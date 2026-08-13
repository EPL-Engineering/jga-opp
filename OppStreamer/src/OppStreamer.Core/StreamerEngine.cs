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
        _store.SetContinuousStimulus(participant, mode, signal);
    }

    /// <summary>Sets one of the Subject's four buffers (Background/Signal x Test/Training).</summary>
    public void SetSubjectSignal(OperatingMode mode, bool isSignal, float[] signal) => _store.SetSubjectStimulus(mode, isSignal, signal);

    /// <summary>Sets a participant's Training buffer, regardless of which mode is currently active — matches the original SetTrainer semantics.</summary>
    public void SetTrainer(Participant participant, float[] signal)
    {
        if (participant == Participant.Subject)
            throw new ArgumentException("Use SetSubjectSignal(OperatingMode.Training, ...) for Subject.", nameof(participant));
        _store.SetContinuousStimulus(participant, OperatingMode.Training, signal);
    }

    /// <summary>
    /// Atomically updates all training buffers together — the new capability this redesign was
    /// specifically undertaken to support cleanly.
    /// </summary>
    public void SetTrainingStimulusSet(float[] caregiver, float[] waver, float[] subjectBackground, float[] subjectSignal)
        => _store.SetTrainingStimulusSet(caregiver, waver, subjectBackground, subjectSignal);

    /// <summary>Requests a switch between Test and Training mode, applied at the next loop boundary.</summary>
    public void TrainTest(bool isTrainer) => _store.RequestModeChange(isTrainer ? OperatingMode.Training : OperatingMode.Test);

    /// <summary>Initiates a trial, applied at the next loop boundary. Works identically in Test and Training mode.</summary>
    public void Trigger(bool containsProbe) => _trial.Trigger(containsProbe);

    /// <summary>
    /// Renders the next <paramref name="count"/> samples for Caregiver/Waver/Subject. Drives the
    /// shared playback clock and applies any latched changes exactly at loop boundaries.
    /// </summary>
    public void RenderFrame(int count, Span<float> caregiverOut, Span<float> waverOut, Span<float> subjectOut)
        => _store.Advance(count, caregiverOut, waverOut, subjectOut, onBoundary: _trial.OnBoundary);
}
