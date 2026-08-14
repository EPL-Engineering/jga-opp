namespace OppStreamer.Core;

/// <summary>
/// A circular float buffer between two independently-clocked audio streams — here, mic capture
/// (producer, running on WASAPI's own capture thread) and the render callback (consumer, pulling
/// audio for the MOTU/output device's channels 6/7). See design doc §5.5.
///
/// Two USB devices (or a USB device and the MOTU) don't share a clock: even both nominally running
/// at 48kHz, their actual sample clocks drift relative to each other by some small fraction of a
/// percent — not fast enough to matter over a second or two, but easily enough to underrun or
/// overflow a plain fixed-size buffer over the course of a multi-minute session. Rather than
/// resample explicitly against a wall clock, this buffer corrects drift implicitly: Read() tracks
/// how full the buffer currently is and nudges its own read *rate* up or down by a tiny amount (via
/// linear interpolation between samples) to steer the fill level back toward a target. The
/// producer's and consumer's actual clock rates never need to be measured or reconciled directly —
/// only the buffer's occupancy, which is cheap and purely local.
///
/// Zero external dependencies (matches every other OppStreamer.Core type) — this can be, and is,
/// fully unit tested without any real audio hardware; see DriftCompensatedRingBufferTests.
/// </summary>
public sealed class DriftCompensatedRingBuffer
{
    private readonly float[] _buffer;
    private readonly int _capacity;
    private readonly double _targetFill;
    private readonly double _correctionGain;
    private readonly double _maxRateAdjustment;
    private readonly object _gate = new();

    // The buffer's whole history is tracked as two monotonically increasing positions on one
    // timeline, rather than as head/tail indices into the array directly — that's what makes the
    // fractional (sub-sample) read position needed for interpolation straightforward. _readPosition
    // just advances by "rate" (~1.0) each output sample; the actual array slot is only computed
    // (mod _capacity) at the point of writing to or reading from _buffer.
    private long _totalWritten;
    private double _readPosition;

    private float _lastOutputSample;
    private bool _hasOutputSample;

    /// <summary>
    /// Samples silently dropped because the buffer was already full when Write() was called — the
    /// consumer isn't draining fast enough, or hasn't started yet. Should stay at 0 in normal
    /// steady-state operation; a steadily climbing count means the correction gain, target fill, or
    /// capacity need revisiting.
    /// </summary>
    public long OverflowSampleCount { get; private set; }

    /// <summary>
    /// Samples the consumer asked for that weren't available yet — sample-and-held (or silence, if
    /// nothing has ever been written) rather than skipped, since the render callback has a fixed
    /// frame count it must hand back regardless. Expected for the first few callbacks at startup
    /// while the buffer fills toward its target; should not grow during steady-state operation.
    /// </summary>
    public long UnderrunSampleCount { get; private set; }

    /// <summary>
    /// How many samples are currently buffered — write position minus read position, in samples,
    /// not a 0..1 fraction. Mainly useful for diagnostics (a future DiagnosticsView plot).
    /// </summary>
    public double CurrentFillLevel { get { lock (_gate) return _totalWritten - _readPosition; } }

    /// <param name="capacitySamples">
    /// Ring buffer size. Should comfortably exceed the worst-case producer/consumer callback
    /// jitter expected in practice — see MicBridge for the value used there and why.
    /// </param>
    /// <param name="targetFillFraction">
    /// Where in the buffer (as a fraction of capacity) Read() steers the fill level toward. 0.5
    /// (the default) gives equal headroom against both underrun and overflow, which is the right
    /// choice unless one side is known to be much burstier than the other.
    /// </param>
    /// <param name="correctionGain">
    /// How aggressively Read() adjusts its rate per sample of fill-level error. Deliberately small
    /// — see maxRateAdjustment.
    /// </param>
    /// <param name="maxRateAdjustment">
    /// Hard cap on how far the read rate can be nudged away from 1.0 (e.g. 0.02 = never more than
    /// 2% fast or slow). Bounds the worst-case pitch/timing distortion correction can introduce, at
    /// the cost of how quickly it can converge after a large fill-level error.
    /// </param>
    public DriftCompensatedRingBuffer(
        int capacitySamples,
        double targetFillFraction = 0.5,
        double correctionGain = 0.002,
        double maxRateAdjustment = 0.02)
    {
        if (capacitySamples < 4)
            throw new ArgumentOutOfRangeException(nameof(capacitySamples), "Capacity must be large enough to hold at least a couple of samples of slack.");
        if (targetFillFraction is <= 0.0 or >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(targetFillFraction), "Must be strictly between 0 and 1 — the target needs headroom on both sides to correct against.");

        _capacity = capacitySamples;
        _buffer = new float[_capacity];
        _targetFill = targetFillFraction * _capacity;
        _correctionGain = correctionGain;
        _maxRateAdjustment = maxRateAdjustment;
    }

    /// <summary>
    /// Producer side — called from MicBridge's WASAPI capture callback. Drops (and counts) any
    /// samples that don't fit; never blocks and never throws on a full buffer, since a capture
    /// callback thread has nowhere to put backpressure.
    /// </summary>
    public void Write(ReadOnlySpan<float> samples)
    {
        lock (_gate)
        {
            foreach (float sample in samples)
            {
                double fillLevel = _totalWritten - _readPosition;

                // Leave a 1-sample margin: Read()'s interpolation looks one sample ahead of
                // _readPosition, so that slot needs to already hold genuinely-written data, not the
                // stale tail end of a previous lap around the ring that's about to be overwritten.
                if (fillLevel >= _capacity - 1)
                {
                    OverflowSampleCount++;
                    continue;
                }

                _buffer[(int)(_totalWritten % _capacity)] = sample;
                _totalWritten++;
            }
        }
    }

    /// <summary>
    /// Consumer side — called from the render callback (StreamerSampleProvider) once per channel
    /// per audio callback. Always fills the entire destination span: sample-and-holds through any
    /// underrun (or produces silence, before anything has ever been written) rather than returning
    /// a short read, since the render callback has a fixed frame count to hand back to WASAPI/ASIO
    /// regardless of whether the mic side kept up.
    /// </summary>
    public void Read(Span<float> destination)
    {
        lock (_gate)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                double fillLevel = _totalWritten - _readPosition;

                // Need the sample at floor(_readPosition)+1 to already exist to interpolate safely
                // — i.e. fillLevel must reach at least 1.0. Below that, hold rather than guess.
                if (fillLevel < 1.0)
                {
                    UnderrunSampleCount++;
                    destination[i] = _hasOutputSample ? _lastOutputSample : 0f;
                    continue;
                }

                float sample = InterpolatedSample(_readPosition);
                _lastOutputSample = sample;
                _hasOutputSample = true;
                destination[i] = sample;

                double error = fillLevel - _targetFill;
                double rate = 1.0 + Math.Clamp(error * _correctionGain, -_maxRateAdjustment, _maxRateAdjustment);
                _readPosition += rate;
            }
        }
    }

    private float InterpolatedSample(double position)
    {
        long i0 = (long)Math.Floor(position);
        double frac = position - i0;

        float s0 = _buffer[Mod(i0)];
        float s1 = _buffer[Mod(i0 + 1)];
        return (float)(s0 + (s1 - s0) * frac);
    }

    private int Mod(long index)
    {
        int m = (int)(index % _capacity);
        return m < 0 ? m + _capacity : m;
    }
}
