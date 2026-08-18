using System;
using System.Collections.Generic;

namespace OppStreamer.Core
{
    internal static class KeyValuePairExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }

    /// <summary>
    /// Thread-safe latch that every mutating operation on <see cref="StimulusStore"/> funnels
    /// through. Values queued here are applied atomically, as a group, the next time the shared
    /// playback loop wraps (see <see cref="StimulusStore.Advance"/>).
    ///
    /// The group-atomicity is the whole point: <see cref="SetBatch"/> takes every participant that
    /// needs to change together (e.g. all three participants for a mode switch, or the subset
    /// touched by SetTrainingStimulusSet) and applies them in one lock acquisition, so a concurrent
    /// drain on the audio thread can never observe a partially-applied group.
    /// </summary>
    internal sealed class PendingChangeQueue
    {
        private readonly object _gate = new object();
        private float[] _caregiver;
        private float[] _waver;
        private float[] _subject;

        public void SetBatch(IReadOnlyDictionary<Participant, float[]> updates)
        {
            if (updates.Count == 0) return;

            lock (_gate)
            {
                foreach (var (participant, buffer) in updates)
                {
                    switch (participant)
                    {
                        case Participant.Caregiver: _caregiver = buffer; break;
                        case Participant.Waver: _waver = buffer; break;
                        case Participant.Subject: _subject = buffer; break;
                        default: throw new ArgumentOutOfRangeException(nameof(updates));
                    }
                }
            }
        }

        /// <summary>
        /// Atomically takes and clears whatever is currently queued. Called from the audio thread
        /// exactly at a loop-boundary wrap.
        /// </summary>
        public (float[] Caregiver, float[] Waver, float[] Subject) DrainAll()
        {
            lock (_gate)
            {
                var result = (_caregiver, _waver, _subject);
                _caregiver = _waver = _subject = null;
                return result;
            }
        }
    }
}