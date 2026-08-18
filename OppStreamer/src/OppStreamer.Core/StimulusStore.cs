using System;
using System.Collections.Generic;

namespace OppStreamer.Core
{
    /// <summary>
    /// Owns every named stimulus buffer, the current mode/trial-signal selection, and the single
    /// shared playback cursor that Caregiver, Waver, and Subject all advance through in lockstep.
    ///
    /// A core invariant taken directly from the OPP spec: every stimulus buffer for a phase is
    /// exactly one masker interval (I) long, and all three participants loop through their buffer
    /// together, wrapping at the same instant. That shared wrap is the one and only "safe boundary"
    /// at which latched changes (mode switches, trial triggers, hot-swapped training stimuli) are
    /// allowed to become audible — never mid-loop.
    /// </summary>
    public sealed class StimulusStore
    {
        // Named buffers. Caregiver/Waver have one buffer per mode; Subject has Background/Signal per mode.
        private readonly Dictionary<(Participant Participant, OperatingMode Mode), float[]> _continuous = new();
        private readonly Dictionary<(OperatingMode Mode, bool IsSignal), float[]> _subject = new();

        // What's currently audible for each participant. Null until the first bootstrap write.
        private readonly Dictionary<Participant, float[]?> _active = new()
        {
            [Participant.Caregiver] = null,
            [Participant.Waver] = null,
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

            _continuous.Clear();
            _subject.Clear();
            _active[Participant.Caregiver] = null;
            _active[Participant.Waver] = null;
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
        /// Sets the Caregiver or Waver buffer for a given mode. If that buffer is the one currently
        /// selected for playback, the update is boundary-gated (queued for the next loop wrap) rather
        /// than applied immediately, so an in-flight loop is never interrupted mid-sample.
        /// </summary>
        public void SetContinuousStimulus(Participant participant, OperatingMode mode, float[] data)
        {
            if (participant == Participant.Subject)
                throw new ArgumentException("Subject has Background/Signal buffers — use SetSubjectStimulus.", nameof(participant));

            ValidateLength(data);
            _continuous[(participant, mode)] = data;

            if (mode == _mode)
                ApplyBatch(new Dictionary<Participant, float[]> { [participant] = data });
        }

        /// <summary>
        /// Sets one of the Subject's four named buffers (Background/Signal x Test/Training).
        /// Boundary-gated exactly like <see cref="SetContinuousStimulus"/> when it targets the
        /// currently selected combination.
        /// </summary>
        public void SetSubjectStimulus(OperatingMode mode, bool isSignal, float[] data)
        {
            ValidateLength(data);
            _subject[(mode, isSignal)] = data;

            if (mode == _mode && isSignal == _subjectSignalActive)
                ApplyBatch(new Dictionary<Participant, float[]> { [Participant.Subject] = data });
        }

        /// <summary>
        /// Atomically updates all four training buffers (Caregiver, Waver, Subject Background,
        /// Subject Signal) as a single group. This is the entry point for the new "vary the training
        /// masker/probe combination on the fly" feature: whichever of these are currently selected
        /// for playback change together, on the same loop boundary — never staggered across separate
        /// calls landing on different wraps.
        /// </summary>
        public void SetTrainingStimulusSet(float[] caregiver, float[] waver, float[] subjectBackground, float[] subjectSignal)
        {
            ValidateLength(caregiver);
            ValidateLength(waver);
            ValidateLength(subjectBackground);
            ValidateLength(subjectSignal);

            _continuous[(Participant.Caregiver, OperatingMode.Training)] = caregiver;
            _continuous[(Participant.Waver, OperatingMode.Training)] = waver;
            _subject[(OperatingMode.Training, false)] = subjectBackground;
            _subject[(OperatingMode.Training, true)] = subjectSignal;

            if (_mode != OperatingMode.Training)
                return; // not currently selected for anyone — stored for later, nothing to queue.

            var updates = new Dictionary<Participant, float[]>
            {
                [Participant.Caregiver] = caregiver,
                [Participant.Waver] = waver,
                [Participant.Subject] = _subjectSignalActive ? subjectSignal : subjectBackground,
            };
            ApplyBatch(updates);
        }

        // ----------------------------------------------------------------------------------
        // Requests — latched, boundary-gated selection changes.
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Requests a switch between Test and Training mode. Bookkeeping (<see cref="CurrentMode"/>)
        /// updates immediately so subsequent Set calls route correctly; the audible effect on all
        /// three participants is deferred to, and applied together at, the next loop boundary.
        /// </summary>
        public void RequestModeChange(OperatingMode mode)
        {
            _mode = mode;

            var updates = new Dictionary<Participant, float[]>();
            if (_continuous.TryGetValue((Participant.Caregiver, mode), out var caregiver)) updates[Participant.Caregiver] = caregiver;
            if (_continuous.TryGetValue((Participant.Waver, mode), out var waver)) updates[Participant.Waver] = waver;
            if (_subject.TryGetValue((mode, _subjectSignalActive), out var subject)) updates[Participant.Subject] = subject;

            ApplyBatch(updates);
        }

        /// <summary>
        /// Requests the Subject's Background/Signal selection change (driven by
        /// <see cref="TrialStateMachine"/>). Boundary-gated like everything else here.
        /// </summary>
        public void RequestSubjectSignal(bool active)
        {
            _subjectSignalActive = active;

            if (_subject.TryGetValue((_mode, active), out var subject))
                ApplyBatch(new Dictionary<Participant, float[]> { [Participant.Subject] = subject });
        }

        /// <summary>
        /// Requests that Caregiver, Waver, and Subject all go silent together at the next loop
        /// boundary — the click-free building block behind ConfigApi's graceful Stop(). Deliberately
        /// reuses the exact same boundary latch as every other stimulus change above (<see cref="ApplyBatch"/>
        /// / <see cref="PendingChangeQueue"/>): the silence buffers are queued as one atomic group, so
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
                [Participant.Waver] = new float[loopLen],
                [Participant.Subject] = new float[loopLen],
            };
            ApplyBatch(updates);
            return true;
        }

        // ----------------------------------------------------------------------------------
        // Playback — called from the audio thread.
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Advances the shared cursor by <paramref name="count"/> samples, filling the three output
        /// spans. Any changes queued via the requests/setters above are applied exactly when the
        /// cursor wraps back to zero — never mid-loop. If the configured loop is shorter than
        /// <paramref name="count"/>, multiple wraps (and boundary applications) can happen within a
        /// single call; each is handled individually, in order.
        /// </summary>
        /// <param name="onBoundary">
        /// Invoked synchronously at each wrap, before that wrap's pending changes are drained and
        /// applied — giving the caller (typically <see cref="TrialStateMachine"/>) a chance to queue
        /// its own changes (e.g. ending a trial) so they land on this same boundary.
        /// </param>
        /// <returns>The number of loop-boundary wraps that occurred during this call.</returns>
        public int Advance(int count, Span<float> caregiverOut, Span<float> waverOut, Span<float> subjectOut, Action? onBoundary = null)
        {
            if (LoopLengthSamples is not int loopLen)
                throw new InvalidOperationException("StimulusStore has not been reset/initialized for a phase yet.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (caregiverOut.Length < count || waverOut.Length < count || subjectOut.Length < count)
                throw new ArgumentException("Output spans must each be at least 'count' samples long.");

            var caregiver = _active[Participant.Caregiver] ?? throw new InvalidOperationException("Caregiver stimulus has not been set.");
            var waver = _active[Participant.Waver] ?? throw new InvalidOperationException("Waver stimulus has not been set.");
            var subject = _active[Participant.Subject] ?? throw new InvalidOperationException("Subject stimulus has not been set.");

            int wraps = 0;
            for (int i = 0; i < count; i++)
            {
                caregiverOut[i] = caregiver[_cursor];
                waverOut[i] = waver[_cursor];
                subjectOut[i] = subject[_cursor];

                _cursor++;
                if (_cursor >= loopLen)
                {
                    _cursor = 0;
                    wraps++;

                    onBoundary?.Invoke();

                    var (c, w, s) = _pending.DrainAll();
                    if (c is not null) caregiver = c;
                    if (w is not null) waver = w;
                    if (s is not null) subject = s;
                }
            }

            _active[Participant.Caregiver] = caregiver;
            _active[Participant.Waver] = waver;
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
}