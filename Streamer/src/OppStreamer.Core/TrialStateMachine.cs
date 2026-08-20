using System;

namespace OppStreamer.Core
{
    /// <summary>
    /// Tracks whether a trial is currently active (probe-repeat countdown and the trial-active-window
    /// flag), independently of which top-level mode (Test/Training) is selected — Trigger() is meant
    /// to work the same way in both modes, so this class never looks at <see cref="OperatingMode"/> at
    /// all. It only ever asks the store "make the Subject's Signal buffer active" or "...Background
    /// buffer active"; whichever mode is currently selected resolves which physical buffer that means.
    /// </summary>
    public sealed class TrialStateMachine
    {
        private readonly StimulusStore _store;

        private int _numReps = 1;
        private int _remainingReps;
        private bool _triggerPending;
        private bool _pendingContainsProbe;
        private bool _trialActive;

        public TrialStateMachine(StimulusStore store) => _store = store ?? throw new ArgumentNullException(nameof(store));

        /// <summary>True while a trial's trial-active-window is open (regardless of whether it contains a probe).</summary>
        public bool TrialActiveWindowOpen { get; private set; }

        public void SetNumReps(int numReps)
        {
            if (numReps < 1)
                throw new ArgumentOutOfRangeException(nameof(numReps), "Must repeat the probe at least once.");
            _numReps = numReps;
        }

        /// <summary>
        /// Latches a trial request. Takes effect at the next loop boundary (via
        /// <see cref="OnBoundary"/>), so it seamlessly joins the ongoing masker pattern rather than
        /// cutting in mid-loop.
        ///
        /// If a trial is already active, this call is a deliberate no-op: it's dropped, not queued.
        /// (The spec doesn't say what should happen if Trigger() fires again mid-trial — this was a
        /// judgment call, made explicit here rather than left as an accident of a single pending-flag
        /// latch. The alternative, queuing it to fire immediately when the current trial ends, risked
        /// an unintended back-to-back trial from something like Tester key bounce. Flag if you'd
        /// rather it queued.)
        /// </summary>
        public void Trigger(bool containsProbe)
        {
            if (_trialActive)
                return;

            _triggerPending = true;
            _pendingContainsProbe = containsProbe;
        }

        /// <summary>
        /// Called by the engine exactly once per completed loop (see
        /// <see cref="StimulusStore.Advance"/>'s onBoundary hook), before that boundary's pending
        /// changes are drained — so any request made here lands on this same boundary.
        /// </summary>
        public void OnBoundary()
        {
            if (_trialActive)
            {
                _remainingReps--;
                if (_remainingReps <= 0)
                {
                    _trialActive = false;
                    TrialActiveWindowOpen = false;
                    _store.RequestSubjectSignal(active: false);
                }
                return;
            }

            if (_triggerPending)
            {
                _triggerPending = false;
                _trialActive = true;
                TrialActiveWindowOpen = true;
                _remainingReps = _numReps;
                _store.RequestSubjectSignal(active: _pendingContainsProbe);
            }
        }
    }
}