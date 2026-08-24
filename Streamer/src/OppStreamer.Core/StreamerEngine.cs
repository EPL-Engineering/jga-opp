using System;
using System.Threading;

namespace OppStreamer.Core;

/// <summary>
/// Composition root for the hardware-independent "brain" of the streamer: everything that
/// decides what Caregiver/Subject should be playing, moment to moment, with no dependency
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

    // Channel 5 — Beacon/Alert (added 2026-08-22). Reuses TtsPlayer's exact FIFO/one-shot playback
    // shape: presynthesized audio, no loop-boundary latch, drains as fast as the render callback
    // asks, silence once dry. The only thing new here is WHEN it gets enqueued — see
    // FireBeaconIfEnabled and TrialStateMachine's onTrialStart hook.
    private readonly TtsPlayer _beacon = new();
    private float[]? _beaconSound;
    private volatile bool _beaconEnabled;

    // Backs RequestStop()/WaitForStopBoundary() — see their doc comments. Starts signaled (no
    // stop is pending, so a wait would return immediately) rather than starting blocked.
    private readonly ManualResetEventSlim _stopBoundaryReached = new(initialState: true);
    private volatile bool _stopPending;

    // Backs WaitForLatch() — see its doc comment. Every mutating call (SetBackground, SetSignal,
    // SetStimulusSet/SetTrainingStimulusSet, TrainTest, Trigger) Reset()s this; the next boundary
    // crossing after that — whichever mutating call it was — Sets it. Same shape as
    // _stopBoundaryReached/RequestStop above: the reset happens at the "I just changed something"
    // moment, not inside the wait call itself, so a boundary that lands between the mutating call
    // and WaitForLatch() being called still counts (it doesn't get reset out from under itself).
    // Beacon setters (SetBeaconSound/SetBeaconEnabled) deliberately do NOT reset this — they take
    // effect immediately, not at a loop boundary, so there's nothing for WaitForLatch to confirm.
    private readonly ManualResetEventSlim _latchReached = new(initialState: false);

    public StreamerEngine() => _trial = new TrialStateMachine(_store, onTrialStart: FireBeaconIfEnabled);

    /// <summary>True while a trial's trial-active-window is open.</summary>
    public bool TrialActiveWindowOpen => _trial.TrialActiveWindowOpen;

    public int? LoopLengthSamples => _store.LoopLengthSamples;

    /// <summary>Begins a new phase: clears all stimulus buffers and fixes the loop length (one masker interval, in samples).</summary>
    public void Reset(int loopLengthSamples) => _store.Reset(loopLengthSamples);

    public void SetNumReps(int numReps) => _trial.SetNumReps(numReps);

    /// <summary>Sets a mode's Background buffer — what Subject hears until a trial's Signal window is active.</summary>
    public void SetBackground(OperatingMode mode, float[] signal)
    {
        _latchReached.Reset();
        _store.SetBackground(mode, signal);
    }

    /// <summary>Sets a mode's Signal buffer — what Caregiver always hears, and what Subject hears while a trial's Signal window is active.</summary>
    public void SetSignal(OperatingMode mode, float[] signal)
    {
        _latchReached.Reset();
        _store.SetSignal(mode, signal);
    }

    /// <summary>
    /// Atomically updates both of a mode's buffers (Background, Signal) together, guaranteed to
    /// land on the same loop boundary — see <see cref="StimulusStore.SetStimulusSet"/> for why this
    /// matters versus calling <see cref="SetBackground"/>/<see cref="SetSignal"/> one at a time for
    /// the same mode.
    /// </summary>
    public void SetStimulusSet(OperatingMode mode, float[] background, float[] signal)
    {
        _latchReached.Reset();
        _store.SetStimulusSet(mode, background, signal);
    }

    /// <summary>Backward-compatible alias for <c>SetStimulusSet(OperatingMode.Training, ...)</c> — the capability this redesign was originally undertaken to support cleanly.</summary>
    public void SetTrainingStimulusSet(float[] background, float[] signal)
        => SetStimulusSet(OperatingMode.Training, background, signal);

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
    /// Loads the one-shot Beacon/Alert clip (channel 5) — provided once at startup per the
    /// 2026-08-22 spec, though nothing here prevents reloading it later if that's ever useful. Takes
    /// effect immediately (no loop-boundary latch — like <see cref="SendTts"/>, this is a FIFO
    /// one-shot player, not part of the Caregiver/Subject shared-cursor loop) but has no audible
    /// effect until a trial actually starts AND <see cref="SetBeaconEnabled"/> is on — see
    /// <see cref="FireBeaconIfEnabled"/>.
    /// </summary>
    public void SetBeaconSound(float[] sound) => _beaconSound = sound ?? throw new ArgumentNullException(nameof(sound));

    /// <summary>
    /// Enables or disables the Beacon/Alert. Changeable at any time, independent of loading the
    /// sound (design: "two-step — load once at startup, enable can be toggled any time"). Takes
    /// effect immediately, same as <see cref="SetBeaconSound"/> — not loop-boundary-latched.
    /// </summary>
    public void SetBeaconEnabled(bool enabled) => _beaconEnabled = enabled;

    /// <summary>Whether the Beacon/Alert is currently enabled.</summary>
    public bool BeaconEnabled => _beaconEnabled;

    /// <summary>
    /// Wired to <see cref="TrialStateMachine"/>'s onTrialStart callback — see
    /// <see cref="TrialStateMachine"/>'s constructor doc comment for exactly when this fires (once
    /// per trial, at the boundary the trial latches in, regardless of probe presence). A no-op if
    /// disabled or no sound has been loaded yet — silently, not an error, since "Beacon not yet
    /// configured" is a completely normal state during startup before <see cref="SetBeaconSound"/>
    /// has been called.
    /// </summary>
    private void FireBeaconIfEnabled()
    {
        if (_beaconEnabled && _beaconSound is not null)
            _beacon.Enqueue(_beaconSound);
    }

    /// <summary>
    /// Requests that Caregiver/Subject go silent at the next loop boundary rather than being
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
    /// been crossed SINCE THE MOST RECENT mutating call (SetBackground, SetSignal,
    /// SetStimulusSet/SetTrainingStimulusSet, TrainTest, or Trigger — each Reset()s the underlying
    /// signal; see the <c>_latchReached</c> field comment), or <paramref name="timeout"/> elapses
    /// first.
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
    /// Renders the next <paramref name="count"/> samples for Caregiver/Subject. Drives the shared
    /// playback clock and applies any latched changes exactly at loop boundaries.
    /// </summary>
    public void RenderFrame(int count, Span<float> caregiverOut, Span<float> subjectOut)
    {
        int wraps = _store.Advance(count, caregiverOut, subjectOut, onBoundary: _trial.OnBoundary);

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
    /// Queues presynthesized TTS audio (channel 4) to play after anything already queued or
    /// playing. Unlike Caregiver/Subject this has no loop-boundary latch — see
    /// <see cref="TtsPlayer"/>'s doc comment for why it's a separate, simpler FIFO.
    /// </summary>
    public void SendTts(float[] signal) => _tts.Enqueue(signal);

    /// <summary>Renders the next <paramref name="destination"/>.Length samples of TTS audio (channel 4), silence once the queue runs dry.</summary>
    public void RenderTts(Span<float> destination) => _tts.Read(destination);

    /// <summary>Renders the next <paramref name="destination"/>.Length samples of Beacon/Alert audio (channel 5), silence when nothing's queued (disabled, not yet loaded, or already finished playing).</summary>
    public void RenderBeacon(Span<float> destination) => _beacon.Read(destination);
}
