using System;
using System.Collections.Generic;

namespace OppStreamer.Core;

/// <summary>
/// Owns the Background/Signal stimulus buffers, the current mode/trial-signal selection, and the
/// single shared playback cursor that Caregiver and Subject both advance through in lockstep.
///
/// A core invariant taken directly from the OPP spec: every stimulus buffer for a phase is
/// exactly one masker interval (I) long, and both participants loop through their buffer
/// together, wrapping at the same instant. That shared wrap is the one and only "safe boundary"
/// at which latched changes (mode switches, trial triggers, hot-swapped training stimuli) are
/// allowed to become audible — never mid-loop.
///
/// <b>2026-08-22 — redesigned around Background/Signal, Waver removed.</b> Earlier, each of
/// Caregiver/Waver had its own independently-settable buffer per mode, and Subject had separate
/// Background/Signal buffers per mode — four named buffers per mode in total, three of which (in
/// practice) always held the same content, since Caregiver and Waver were always set to whatever
/// Subject's Signal buffer held. Reframed per Ken's 2026-08-22 review: there are really only two
/// signals that matter per mode — Background (masker alone) and Signal (masker + probe) — so this
/// class now owns exactly two buffers per mode, not four. Caregiver's audio is simply "the current
/// mode's Signal buffer," continuously, with no separate identity of its own. Subject's audio is
/// "Background until a trial's Signal window is active, then Signal" — same behavior as before,
/// just resolved against the shared Background/Signal buffers rather than Subject-private ones.
/// Waver is gone entirely — its old job (mirroring Caregiver in Test mode, Subject in Training
/// mode) is now done by the MOTU's own hardware mixer tapping the appropriate physical channel, so
/// there's nothing left for software to synthesize or keep in sync.
/// </summary>
public sealed class StimulusStore
{
    // The two named buffers, one per mode (Test/Training). Caregiver always plays _signal[mode];
    // Subject plays _background[mode] or _signal[mode] depending on RequestSubjectSignal.
    private readonly Dictionary<OperatingMode, float[]> _background = new();
    private readonly Dictionary<OperatingMode, float[]> _signal = new();

    // What's currently audible for each participant. Null until the first bootstrap write.
    private readonly Dictionary<Participant, float[]?> _active = new()
    {
        [Participant.Caregiver] = null,
        [Participant.Subject] = null,
    };

    private readonly PendingChangeQueue _pending = new();

    private OperatingMode _mode = OperatingMode.Test;
    private bool _subjectSignalActive;
    private int _cursor;

    /// <summary>The fixed length, in samples, of every stimulus buffer for the current phase.</summary>
    public int? LoopLengthSamples { get; private set; }

    /// <summary>Current mode, as most recently requested (may not yet be audible — see remarks on RequestModeChange).</summary>
    public OperatingMode CurrentMode => _mode;

    /// <summary>Whether the Subject's Signal (vs. Background) buffer is the most recently requested selection.</summary>
    public bool SubjectSignalRequested => _subjectSignalActive;

    /// <summary>
    /// Clears all state and fixes the loop length for a new phase. Call this once per phase,
    /// before rendering begins — writes made after Reset (and before the first render) apply
    /// immediately, since nothing is playing yet to glitch.
    /// </summary>
    public void Reset(int loopLengthSamples)
    {
        if (loopLengthSamples <= 0)
            throw new ArgumentOutOfRangeException(nameof(loopLengthSamples), "Loop length must be a positive sample count.");

        _background.Clear();
        _signal.Clear();
        _active[Participant.Caregiver] = null;
        _active[Participant.Subject] = null;
        _mode = OperatingMode.Test;
        _subjectSignalActive = false;
        _cursor = 0;
        _pending.DrainAll();
        LoopLengthSamples = loopLengthSamples;
    }

    // ----------------------------------------------------------------------------------
    // Configuration — called from the MATLAB-facing API thread.
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Sets a mode's Background buffer — what Subject hears until a trial's Signal window opens.
    /// Boundary-gated (queued for the next loop wrap) when it affects the currently-selected
    /// mode/selection combination, exactly like every other setter here — never applied mid-loop.
    ///
    /// CAVEAT if you're setting both Background and Signal for the currently-selected mode as a
    /// pair (e.g. one call each): each call here queues independently, and the audio thread can
    /// drain and apply whatever's queued so far at ANY boundary crossing — including one that lands
    /// in between your two calls. That produces one torn loop pass (part of the pair already
    /// updated, part still on the old buffer) before the rest catches up on the following boundary.
    /// If both need to land on the very same boundary, use <see cref="SetStimulusSet"/> instead —
    /// it queues the pair as one atomic unit.
    /// </summary>
    public void SetBackground(OperatingMode mode, float[] data)
    {
        ValidateLength(data);
        _background[mode] = data;

        if (mode == _mode && !_subjectSignalActive)
            ApplyBatch(new Dictionary<Participant, float[]> { [Participant.Subject] = data });
    }

    /// <summary>
    /// Sets a mode's Signal buffer — what Caregiver always hears, and what Subject hears once a
    /// trial's Signal window is active. Boundary-gated exactly like <see cref="SetBackground"/>
    /// when it affects the currently-selected mode — see that method's doc comment for the same
    /// torn-update caveat when setting Background and Signal as a pair, one call at a time.
    /// </summary>
    public void SetSignal(OperatingMode mode, float[] data)
    {
        ValidateLength(data);
        _signal[mode] = data;

        if (mode != _mode)
            return;

        var updates = new Dictionary<Participant, float[]> { [Participant.Caregiver] = data };
        if (_subjectSignalActive)
            updates[Participant.Subject] = data;
        ApplyBatch(updates);
    }

    /// <summary>
    /// Atomically updates both of a mode's buffers (Background, Signal) as a single group.
    /// Whichever of Caregiver/Subject are currently selected for playback change together, on the
    /// same loop boundary — never staggered across separate calls landing on different wraps,
    /// unlike calling <see cref="SetBackground"/>/<see cref="SetSignal"/> one at a time for the
    /// same mode (see their doc comments' "torn update" hazard — a real audio-thread boundary can
    /// land in between two of those calls and apply only part of the pair).
    ///
    /// Originally built (2026-08-17) Training-only, as <c>SetTrainingStimulusSet</c>, for the "vary
    /// the training masker/probe combination on the fly" feature; generalized (2026-08-20) to take
    /// an explicit mode; reshaped again (2026-08-22) from four buffers (Caregiver, Waver, Subject
    /// Background, Subject Signal) down to these two (Background, Signal) as part of removing Waver
    /// and collapsing Caregiver/Subject onto the same shared pair — see this class's doc comment.
    /// </summary>
    public void SetStimulusSet(OperatingMode mode, float[] background, float[] signal)
    {
        ValidateLength(background);
        ValidateLength(signal);

        _background[mode] = background;
        _signal[mode] = signal;

        if (_mode != mode)
            return; // not currently selected for anyone — stored for later, nothing to queue.

        var updates = new Dictionary<Participant, float[]>
        {
            [Participant.Caregiver] = signal,
            [Participant.Subject] = _subjectSignalActive ? signal : background,
        };
        ApplyBatch(updates);
    }

    /// <summary>Backward-compatible alias for <c>SetStimulusSet(OperatingMode.Training, ...)</c> — kept for existing Training-specific call sites.</summary>
    public void SetTrainingStimulusSet(float[] background, float[] signal)
        => SetStimulusSet(OperatingMode.Training, background, signal);

    // ----------------------------------------------------------------------------------
    // Requests — latched, boundary-gated selection changes.
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Requests a switch between Test and Training mode. Bookkeeping (<see cref="CurrentMode"/>)
    /// updates immediately so subsequent Set calls route correctly; the audible effect on both
    /// participants is deferred to, and applied together at, the next loop boundary.
    /// </summary>
    public void RequestModeChange(OperatingMode mode)
    {
        _mode = mode;

        var updates = new Dictionary<Participant, float[]>();
        if (_signal.TryGetValue(mode, out var signal))
            updates[Participant.Caregiver] = signal;

        var subjectSource = _subjectSignalActive ? _signal : _background;
        if (subjectSource.TryGetValue(mode, out var subject))
            updates[Participant.Subject] = subject;

        ApplyBatch(updates);
    }

    /// <summary>
    /// Requests the Subject's Background/Signal selection change (driven by
    /// <see cref="TrialStateMachine"/>). Boundary-gated like everything else here.
    /// </summary>
    public void RequestSubjectSignal(bool active)
    {
        _subjectSignalActive = active;

        var source = active ? _signal : _background;
        if (source.TryGetValue(_mode, out var subject))
            ApplyBatch(new Dictionary<Participant, float[]> { [Participant.Subject] = subject });
    }

    /// <summary>
    /// Requests that Caregiver and Subject both go silent together at the next loop boundary — the
    /// click-free building block behind ConfigApi's graceful Stop(). Deliberately reuses the exact
    /// same boundary latch as every other stimulus change above (<see cref="ApplyBatch"/> /
    /// <see cref="PendingChangeQueue"/>): the silence buffers are queued as one atomic group, so
    /// playback finishes the loop pass already in progress and only goes quiet exactly at the wrap
    /// — never mid-waveform — same guarantee as a mode switch or a hot-swapped training buffer.
    /// </summary>
    /// <returns>
    /// True if a silence swap was actually queued (or applied — see remarks). False if there's
    /// nothing configured yet (<see cref="LoopLengthSamples"/> is null, e.g. Stop() called before
    /// any phase was ever started) — there's no loop to gate against, so there's nothing to do.
    /// </returns>
    public bool RequestSilence()
    {
        if (LoopLengthSamples is not int loopLen) return false;

        // A fresh, zero-initialized buffer per call (not a single shared static) — ApplyBatch may
        // store a reference to this into _active directly (for a participant that, unusually,
        // isn't active yet), and separate buffer identities per participant keep that case as
        // unsurprising as every other SetSignal-style call.
        var updates = new Dictionary<Participant, float[]>
        {
            [Participant.Caregiver] = new float[loopLen],
            [Participant.Subject] = new float[loopLen],
        };
        ApplyBatch(updates);
        return true;
    }

    // ----------------------------------------------------------------------------------
    // Playback — called from the audio thread.
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Advances the shared cursor by <paramref name="count"/> samples, filling both output spans.
    /// Any changes queued via the requests/setters above are applied exactly when the cursor wraps
    /// back to zero — never mid-loop. If the configured loop is shorter than <paramref name="count"/>,
    /// multiple wraps (and boundary applications) can happen within a single call; each is handled
    /// individually, in order.
    /// </summary>
    /// <param name="onBoundary">
    /// Invoked synchronously at each wrap, before that wrap's pending changes are drained and
    /// applied — giving the caller (typically <see cref="TrialStateMachine"/>) a chance to queue
    /// its own changes (e.g. ending a trial, or firing the Beacon at a trial's start) so they land
    /// on this same boundary.
    /// </param>
    /// <returns>The number of loop-boundary wraps that occurred during this call.</returns>
    public int Advance(int count, Span<float> caregiverOut, Span<float> subjectOut, Action? onBoundary = null)
    {
        if (LoopLengthSamples is not int loopLen)
            throw new InvalidOperationException("StimulusStore has not been reset/initialized for a phase yet.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (caregiverOut.Length < count || subjectOut.Length < count)
            throw new ArgumentException("Output spans must each be at least 'count' samples long.");

        var caregiver = _active[Participant.Caregiver] ?? throw new InvalidOperationException("Caregiver stimulus has not been set.");
        var subject = _active[Participant.Subject] ?? throw new InvalidOperationException("Subject stimulus has not been set.");

        int wraps = 0;
        for (int i = 0; i < count; i++)
        {
            caregiverOut[i] = caregiver[_cursor];
            subjectOut[i] = subject[_cursor];

            _cursor++;
            if (_cursor >= loopLen)
            {
                _cursor = 0;
                wraps++;

                onBoundary?.Invoke();

                var (c, s) = _pending.DrainAll();
                if (c is not null) caregiver = c;
                if (s is not null) subject = s;
            }
        }

        _active[Participant.Caregiver] = caregiver;
        _active[Participant.Subject] = subject;

        return wraps;
    }

    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Commits or queues a group of participant->buffer updates as a single atomic operation:
    /// a participant that has never gone active yet (still bootstrapping, nothing playing)
    /// commits immediately; everything else queues together so the whole group lands on the
    /// same boundary.
    /// </summary>
    private void ApplyBatch(Dictionary<Participant, float[]> updates)
    {
        if (updates.Count == 0) return;

        Dictionary<Participant, float[]>? toQueue = null;
        foreach (var (participant, data) in updates)
        {
            if (_active[participant] is null)
            {
                _active[participant] = data;
            }
            else
            {
                toQueue ??= new Dictionary<Participant, float[]>();
                toQueue[participant] = data;
            }
        }

        if (toQueue is not null)
            _pending.SetBatch(toQueue);
    }

    private void ValidateLength(float[] data)
    {
        if (LoopLengthSamples is null)
            throw new InvalidOperationException("Call Reset(loopLengthSamples) before setting stimulus buffers.");
        if (data.Length != LoopLengthSamples)
            throw new ArgumentException(
                $"Buffer is {data.Length} samples; every stimulus buffer for this phase must be exactly " +
                $"{LoopLengthSamples} samples (one masker interval).", nameof(data));
    }
}
