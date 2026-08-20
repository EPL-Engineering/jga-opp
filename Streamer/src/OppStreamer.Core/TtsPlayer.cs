using System;
using System.Collections.Generic;

namespace OppStreamer.Core
{
    /// <summary>
    /// Channel 5 — text-to-speech playback. Deliberately the simplest component in the system (design
    /// doc §5.6): presynthesized audio arrives at irregular times from MATLAB and doesn't need to be
    /// phase-locked to anything else the streamer is doing. It's just a FIFO of buffers — newly
    /// arriving audio queues/appends after whatever's already playing (confirmed: it does NOT
    /// interrupt it), drains continuously into channel 5 as fast as the render callback asks for it,
    /// and produces silence once the queue runs dry. None of <see cref="StimulusStore"/>'s
    /// loop-boundary latch machinery applies here at all — that's specifically why this is a separate
    /// class from <see cref="StreamerEngine"/>'s shared-cursor rendering rather than folded into
    /// <see cref="StreamerEngine.RenderFrame"/>.
    ///
    /// Thread-safety: <see cref="Enqueue"/> is expected to be called from whatever thread ConfigApi's
    /// future SendTTS lands on (the MATLAB interop thread, not the audio thread), while
    /// <see cref="Read"/> is called from the render callback. Both take the same lock, so interleaving
    /// is safe; neither call blocks for long (Enqueue is O(1), Read is a couple of array copies).
    /// </summary>
    public sealed class TtsPlayer
    {
        private readonly object _gate = new object();
        private readonly Queue<float[]> _queued = new Queue<float[]>();

        private float[] _current;
        private int _currentPosition;

        /// <summary>
        /// True while there's audio still to play — either mid-buffer or queued behind it. False
        /// exactly when Read() would currently be producing silence.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                // _current can briefly point at a fully-exhausted buffer (Read() only detects and
                // clears that lazily, at the start of its NEXT call) - guard on position, not just
                // reference, so IsPlaying is accurate immediately after a Read() that lands exactly on
                // a buffer boundary, not just after the following Read().
                lock (_gate) return (_current != null && _currentPosition < _current.Length) || _queued.Count > 0;
            }
        }

        /// <summary>Total samples of unplayed TTS audio outstanding (current buffer's remainder plus everything queued behind it) — for diagnostics.</summary>
        public long QueuedSampleCount
        {
            get
            {
                lock (_gate)
                {
                    long total = _current is null ? 0 : _current.Length - _currentPosition;
                    foreach (var buffer in _queued) total += buffer.Length;
                    return total;
                }
            }
        }

        /// <summary>
        /// Appends a new buffer to play after whatever's currently queued or playing. Matches the
        /// original SendTTS(double[] signal) semantics (design doc §5.8) — ConfigApi, a later stage,
        /// is what will convert MATLAB's double[] to float[] before calling this, same boundary every
        /// other Core API observes (see StreamerEngine's doc comment).
        /// </summary>
        public void Enqueue(float[] signal)
        {
            if (signal is null) throw new ArgumentNullException(nameof(signal));
            if (signal.Length == 0) return; // nothing to play - don't leave a dead entry in the queue

            lock (_gate) _queued.Enqueue(signal);
        }

        /// <summary>
        /// Fills <paramref name="destination"/> with the next samples to play, seamlessly crossing
        /// from one queued buffer into the next with no gap, and filling with silence once nothing is
        /// left queued. Always fills the entire span — the render callback has a fixed frame count to
        /// hand back to the transport regardless of whether TTS audio happens to be available.
        /// </summary>
        public void Read(Span<float> destination)
        {
            lock (_gate)
            {
                int written = 0;
                while (written < destination.Length)
                {
                    if (_current is null || _currentPosition >= _current.Length)
                    {
                        if (_queued.Count == 0)
                        {
                            _current = null;
                            break;
                        }
                        _current = _queued.Dequeue();
                        _currentPosition = 0;
                    }

                    int availableInCurrent = _current.Length - _currentPosition;
                    int toCopy = Math.Min(availableInCurrent, destination.Length - written);
                    _current.AsSpan(_currentPosition, toCopy).CopyTo(destination.Slice(written, toCopy));
                    _currentPosition += toCopy;
                    written += toCopy;
                }

                if (written < destination.Length)
                    destination.Slice(written).Clear(); // queue ran dry - silence for the remainder
            }
        }
    }
}