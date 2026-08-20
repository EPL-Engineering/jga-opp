using System;
using System.Threading;

namespace OppStreamer.Core;

/// <summary>
/// Composition root for the hardware-independent "brain" of the streamer: everything that
/// decides what Caregiver/Waver/Subject should be playing, moment to moment, with no dependency
/// on NAudio, ASIO, or any real audio device. A later stage wires this up to a real
/// <c>MotuOutputEngine</c> (NAudio AsioOut) that calls <see cref="RenderFrame"/> from its audio
/// callback; for testing (see the test project) a synthetic driver calls it directly.
///
/// Note this exposes a strongly-typed internal API (enums, not strings/participant names as
/// free text). The eventual MATLAB-facing ConfigApi — a later stage — is what translates
/// NET.addAssembly's string/double[] calls into these enum-based calls; this class deliberately
/// isn't that public surface yet.
/// </summary>
public sealed class StreamerEngine
{
    private readonly StimulusStore _store = new();
    private readonly TrialStateMachine _trial;
    private readonly TtsPlayer _tts = new();

    // Backs RequestStop()/WaitForStopBoundary() — see their doc comments. Starts signaled (no
    // stop is pending, so a wait would return immediately) rather than starting blocked.
    private readonly ManualResetEventSlim _stopBoundaryReached = new(initialState: true);
    private volatile bool _stopPending;

    // Backs WaitForLatch() — see its doc comment. Every mutating call (SetSignal, SetSubjectSignal,
    // SetTrainer, SetStimulusSet/SetTrainingStimulusSet, TrainTest, Trigger) Reset()s this; the next boundary
    // crossing after that — whichever mutating call it was — Sets it. Same shape as
    // _stopBoundaryReached/RequestStop above: the reset happens at the "I just changed something"
    // moment, not inside the wait call itself, so a boundary that lands between the mutating call
    // and WaitForLatch() being called still counts (it doesn't get reset out from under itself).
    private readonly ManualResetEventSlim _latchReached = new(initialState: false);

    public StreamerEngine() => _trial = new TrialStateMachine(_store);

    /// <summary>True while a trial's trial-active-window is open.</summary>
    public bool TrialActiveWindowOpen => _trial.TrialActiveWindowOpen;

    public int? LoopLengthSamples => _store.LoopLengthSamples;

    /// <summary>Begins a new phase: clears all stimulus buffers and fixes the loop length (one masker interval, in samples).</summary>
    public void Reset(int loopLengthSamples) => _store.Reset(loopLengthSamples);

    public void SetNumReps(int numReps) => _trial.SetNumReps(numReps);

    /// <summary>Sets the Caregiver or Waver buffer for a given mode.</summary>
    public void SetSignal(Participant participant, OperatingMode mode, float[] signal)
    {
        if (participant == Participant.Subject)
            throw new ArgumentException("Use SetSubjectSignal for Subject — it has Background/Signal buffers, not one.", nameof(participant));
        _latchReached.Reset();
        _store.SetContinuousStimulus(participant, mode, signal);
    }

    /// <summary>Sets one of the Subject's four buffers (Background/Signal x Test/Training).</summary>
    public void SetSubjectSignal(OperatingMode mode, bool isSignal, float[] signal)
    {
        _latchReached.Reset();
        _store.SetSubjectStimulus(mode, isSignal, signal);
    }

    /// <summary>Sets a participant's Training buffer, regardless of which mode is currently active — matches the original SetTrainer semantics.</summary>
    public void SetTrainer(Participant participant, float[] signal)
    {
        if (participant == Participant.Subject)
            throw new ArgumentException("Use SetSubjectSignal(OperatingMode.Training, ...) for Subject.", nameof(participant));
        _latchReached.Reset();
        _store.SetContinuousStimulus(participant, OperatingMode.Training, signal);
    }

    /// <summary>
    /// Atomically updates all four of a mode's buffers (Caregiver, Waver, Subject Background,
    /// Subject Signal) together, guaranteed to land on the same loop boundary — see
    /// <see cref="StimulusStore.SetStimulusSet"/> for why this matters versus calling
    /// <see cref="SetSignal"/>/<see cref="SetSubjectSignal"/> one at a time for the same group.
    /// </summary>
    public void SetStimulusSet(OperatingMode mode, float[] caregiver, float[] waver, float[] subjectBackground, float[] subjectSignal)
    {
        _latchReached.Reset();
        _store.SetStimulusSet(mode, caregiver, waver, subjectBackground, subjectSignal);
    }

    /// <summary>Backward-compatible alias for <c>SetStimulusSet(OperatingMode.Training, ...)</c> — the capability this redesign was originally undertaken to support cleanly.</summary>
    public void SetTrainingStimulusSet(float[] caregiver, float[] waver, float[] subjectBackground, float[] subjectSignal)
        => SetStimulusSet(OperatingMode.Training, caregiver, waver, subjectBackground, subjectSignal);

    /// <summary>Requests a switch between Test and Training mode, applied at the next loop boundary.</summary>
    public void TrainTest(bool isTrainer)
    {
        _latchReached.Reset();
        _store.RequestModeChange(isTrainer ? OperatingMode.Training : OperatingMode.Test);
    }

    /// <summary>Initiates a trial, applied at the next loop boundary. Works identically in Test and Training mode.</summary>
    public void Trigger(bool containsProbe)
    {
        _latchReached.Reset();
        _trial.Trigger(containsProbe);
    }

    /// <summary>
    /// Requests that Caregiver/Waver/Subject go silent at the next loop boundary rather than being
    /// cut off mid-waveform — the click-free half of a graceful Stop() (see
    /// <see cref="StimulusStore.RequestSilence"/>). Call <see cref="WaitForStopBoundary"/>
    /// afterward, from a thread OTHER than the audio thread, to find out once that boundary has
    /// actually been reached — e.g. before tearing down the physical output device.
    ///
    /// Safe to call even if nothing is currently playing (or nothing has ever been configured):
    /// in that case there's no loop to wait on, so <see cref="WaitForStopBoundary"/> returns
    /// immediately (true) rather than hanging.
    /// </summary>
    public void RequestStop()
    {
        _stopBoundaryReached.Reset();
        if (!_store.RequestSilence())
        {
            _stopBoundaryReached.Set();
            return;
        }
        _stopPending = true;
    }

    /// <summary>
    /// Blocks the CALLING thread — never the audio thread — until the silence requested by
    /// <see cref="RequestStop"/> has actually taken effect, or <paramref name="timeout"/> elapses
    /// first. Returns false on timeout; the caller should still proceed with a hard stop rather
    /// than wait forever (a timeout here most likely means audio isn't actually being rendered
    /// right now — e.g. the device stalled — not that it's merely running long).
    /// </summary>
    public bool WaitForStopBoundary(TimeSpan timeout) => _stopBoundaryReached.Wait(timeout);

    /// <summary>
    /// Blocks the CALLING thread — never the audio thread — until at least one loop boundary has
    /// been crossed SINCE THE MOST RECENT mutating call (SetSignal, SetSubjectSignal, SetTrainer,
    /// SetTrainingStimulusSet, TrainTest, or Trigger — each Reset()s the underlying signal; see the
    /// <c>_latchReached</c> field comment), or <paramref name="timeout"/> elapses first.
    ///
    /// This is the general-purpose replacement for the old LabVIEW-era pattern of stopping and
    /// restarting the stream around every stimulus change: because every mutation goes through the
    /// same PendingChangeQueue/ApplyBatch boundary latch and applies atomically at the very next
    /// boundary, there's no need to track which specific change this call is confirming — ANY
    /// boundary crossing after the most recent mutating call means whatever was queued has now been
    /// applied. Call this right after a Set*/TrainTest/Trigger call whose effect needs confirming
    /// before proceeding. Correctly reports true even if the boundary already happened between the
    /// mutating call returning and this method being called (the reset lives in the mutating call,
    /// not here) — and, deliberately, keeps reporting true on later calls until the NEXT mutating
    /// call resets it again, the same "stays latched, not one-shot" shape as
    /// <see cref="WaitForStopBoundary"/>.
    ///
    /// Returns false on timeout — same interpretation as <see cref="WaitForStopBoundary"/>: most
    /// likely means audio genuinely isn't being rendered right now (never started, or the device
    /// stalled), not merely a long masker interval. Safe to call with nothing configured yet (no
    /// loop length set) — there's nothing to wait on, so this simply times out rather than hanging,
    /// the same outward behavior as a stalled device.
    /// </summary>
    public bool WaitForLatch(TimeSpan timeout) => _latchReached.Wait(timeout);

    /// <summary>
    /// Renders the next <paramref name="count"/> samples for Caregiver/Waver/Subject. Drives the
    /// shared playback clock and applies any latched changes exactly at loop boundaries.
    /// </summary>
    public void RenderFrame(int count, Span<float> caregiverOut, Span<float> waverOut, Span<float> subjectOut)
    {
        int wraps = _store.Advance(count, caregiverOut, waverOut, subjectOut, onBoundary: _trial.OnBoundary);

        if (wraps > 0)
        {
            // See WaitForLatch's doc comment — every boundary counts, not just stop-related ones.
            _latchReached.Set();
        }

        // Fires at most once per RequestStop() call: the moment a boundary is crossed while a
        // stop is pending, the silence RequestStop() queued is now guaranteed to have already been
        // drained and applied by this same Advance() call (DrainAll happens synchronously, inside
        // Advance, immediately after onBoundary) — so it's correct to signal completion right here.
        if (_stopPending && wraps > 0)
        {
            _stopPending = false;
            _stopBoundaryReached.Set();
        }
    }

    /// <summary>
    /// Queues presynthesized TTS audio (channel 5) to play after anything already queued or
    /// playing. Unlike Caregiver/Waver/Subject this has no loop-boundary latch — see
    /// <see cref="TtsPlayer"/>'s doc comment for why it's a separate, simpler FIFO.
    /// </summary>
    public void SendTts(float[] signal) => _tts.Enqueue(signal);

    /// <summary>Renders the next <paramref name="destination"/>.Length samples of TTS audio (channel 5), silence once the queue runs dry.</summary>
    public void RenderTts(Span<float> destination) => _tts.Read(destination);
}
