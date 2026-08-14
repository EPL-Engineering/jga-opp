using OppStreamer.Core;

namespace OppStreamer.Core.Tests;

/// <summary>
/// Exercises DriftCompensatedRingBuffer without any real audio hardware — mic capture and the
/// render callback are both just simulated as Write()/Read() call sequences with a chosen
/// samples-per-call ratio, which is exactly what "drift" reduces to from the buffer's point of
/// view (see the class's own doc comment).
/// </summary>
public static class DriftCompensatedRingBufferTests
{
    public static void Register(TestRunner runner)
    {
        runner.Test("A constant signal passes through unchanged under matched write/read rates", ConstantSignalPassesThroughUnchanged);
        runner.Test("Fill level converges toward target and stays bounded under matched write/read rates", FillLevelStaysNearTargetUnderMatchedRates);
        runner.Test("A faster producer converges without overflowing", FasterProducerConvergesWithoutOverflow);
        runner.Test("A faster consumer converges without sustained underrun", FasterConsumerConvergesWithoutSustainedUnderrun);
        runner.Test("A full buffer drops (and counts) excess samples instead of overwriting unread data", OverflowDropsExcessSamplesAndCounts);
        runner.Test("Reading before anything is written produces silence and counts the underrun", UnderrunOnEmptyBufferProducesSilence);
        runner.Test("A ramp signal's sample-to-sample step never exceeds the configured max rate adjustment", RampStepNeverExceedsMaxRateAdjustment);
    }

    private static void ConstantSignalPassesThroughUnchanged()
    {
        var ring = new DriftCompensatedRingBuffer(capacitySamples: 200, targetFillFraction: 0.5);
        var writeBuf = new float[10];
        Array.Fill(writeBuf, 0.5f);
        var readBuf = new float[10];

        for (int iter = 0; iter < 50; iter++)
        {
            ring.Write(writeBuf);
            ring.Read(readBuf);

            foreach (float sample in readBuf)
                Check.Approximately(0.5, sample, 1e-6, "interpolating a constant signal must reproduce it exactly, regardless of read rate");
        }

        Check.Equal(0L, ring.UnderrunSampleCount, "writing before every read in this test should never leave the buffer empty");
    }

    private static void FillLevelStaysNearTargetUnderMatchedRates()
    {
        const int capacity = 2000;
        const double targetFraction = 0.5;
        var ring = new DriftCompensatedRingBuffer(capacity, targetFraction);

        var writeBuf = new float[37];
        var readBuf = new float[37];
        long sampleCounter = 0;

        for (int iter = 0; iter < 500; iter++)
        {
            for (int i = 0; i < writeBuf.Length; i++) writeBuf[i] = sampleCounter++;
            ring.Write(writeBuf);
            ring.Read(readBuf);
        }

        Check.Equal(0L, ring.OverflowSampleCount, "matched write/read call sizes should never fill a 2000-sample buffer from 37-sample chunks");
        Check.Equal(0L, ring.UnderrunSampleCount, "writing before every read in this test should never leave the buffer empty");

        double target = targetFraction * capacity;
        Check.True(ring.CurrentFillLevel > 0 && ring.CurrentFillLevel < capacity,
            $"fill level ({ring.CurrentFillLevel}) should be strictly within the buffer, not pinned at either end");
        Check.Approximately(target, ring.CurrentFillLevel, target, // generous: within a target-width of the target itself
            $"fill level should have converged toward the target ({target}) rather than drifting to an extreme, got {ring.CurrentFillLevel}");
    }

    private static void FasterProducerConvergesWithoutOverflow()
    {
        // Producer writes 1001 samples for every 1000 the consumer reads — a ~0.1% rate mismatch,
        // comfortably inside the default 2% maxRateAdjustment headroom, so correction should be
        // able to fully absorb it rather than just slowing the inevitable overflow down.
        const int capacity = 4000;
        var ring = new DriftCompensatedRingBuffer(capacity, targetFillFraction: 0.5);
        var writeBuf = new float[1001];
        var readBuf = new float[1000];

        double[] fillHistory = new double[5000];
        for (int iter = 0; iter < fillHistory.Length; iter++)
        {
            ring.Write(writeBuf);
            ring.Read(readBuf);
            fillHistory[iter] = ring.CurrentFillLevel;
        }

        Check.Equal(0L, ring.OverflowSampleCount, "a 0.1% producer-side excess is well within maxRateAdjustment and should never overflow a 4000-sample buffer");

        // Convergence, not just "didn't overflow yet": the back half of the run should be settled
        // near the target, not still climbing toward capacity.
        double target = 0.5 * capacity;
        double lateAverage = fillHistory.Skip(4000).Average();
        Check.Approximately(target, lateAverage, target * 0.6,
            $"fill level should have settled near the target ({target}) by the back half of the run, got average {lateAverage}");
    }

    private static void FasterConsumerConvergesWithoutSustainedUnderrun()
    {
        // The mirror image: consumer asks for 1001 samples for every 1000 the producer supplies.
        const int capacity = 4000;
        var ring = new DriftCompensatedRingBuffer(capacity, targetFillFraction: 0.5);
        var writeBuf = new float[1000];
        var readBuf = new float[1001];

        for (int iter = 0; iter < 5000; iter++)
        {
            ring.Write(writeBuf);
            ring.Read(readBuf);
        }

        // Underrun is expected only in the startup transient before the buffer reaches its target
        // fill for the first time — a few thousand samples' worth, not a meaningful fraction of the
        // ~5,000,500 samples read over the whole run.
        long totalRead = 5000L * readBuf.Length;
        Check.True(ring.UnderrunSampleCount < totalRead / 100,
            $"underrun ({ring.UnderrunSampleCount} samples) should be confined to startup, not a sustained fraction of {totalRead} samples read");

        double target = 0.5 * capacity;
        Check.True(ring.CurrentFillLevel > 0,
            "fill level should have stabilized above zero, not drained to empty, by the end of the run");
        Check.Approximately(target, ring.CurrentFillLevel, target * 0.6,
            $"fill level should have settled near the target ({target}), got {ring.CurrentFillLevel}");
    }

    private static void OverflowDropsExcessSamplesAndCounts()
    {
        const int capacity = 16;
        var ring = new DriftCompensatedRingBuffer(capacity, targetFillFraction: 0.5);
        var writeBuf = new float[1000];
        for (int i = 0; i < writeBuf.Length; i++) writeBuf[i] = i;

        ring.Write(writeBuf); // one big write, no reads at all — the buffer fills and the rest must drop

        // Read() reserves a 1-sample margin so interpolation always has a valid "next" sample to
        // read, so only capacity-1 samples are ever actually accepted.
        Check.Equal((long)(writeBuf.Length - (capacity - 1)), ring.OverflowSampleCount,
            "every sample beyond the buffer's usable capacity (capacity - 1, for the interpolation margin) should be dropped and counted");
        Check.Equal((double)(capacity - 1), ring.CurrentFillLevel, "fill level should be pinned at the usable capacity, not overrun it");
    }

    private static void UnderrunOnEmptyBufferProducesSilence()
    {
        var ring = new DriftCompensatedRingBuffer(capacitySamples: 100, targetFillFraction: 0.5);
        var readBuf = new float[25];
        Array.Fill(readBuf, -1f); // sentinel, so a bug that leaves entries untouched is visible

        ring.Read(readBuf);

        Check.Equal(25L, ring.UnderrunSampleCount, "every sample of a read against a totally empty buffer should count as an underrun");
        foreach (float sample in readBuf)
            Check.Equal(0f, sample, "an underrun with no prior output sample yet should produce silence, not garbage or the sentinel");
    }

    private static void RampStepNeverExceedsMaxRateAdjustment()
    {
        // A true linear ramp's interpolated value at any fractional position equals the position
        // itself, so the difference between consecutive output samples is exactly the read *rate*
        // used to produce them — this is a direct, always-true check on the rate-clamping logic
        // itself, not just a statistical sanity check, and needs no warm-up period to be valid.
        const double maxRateAdjustment = 0.02;
        var ring = new DriftCompensatedRingBuffer(capacitySamples: 2000, targetFillFraction: 0.5, maxRateAdjustment: maxRateAdjustment);

        var writeBuf = new float[50];
        var readBuf = new float[50];
        long sampleCounter = 0;
        float? previous = null;

        for (int iter = 0; iter < 300; iter++)
        {
            for (int i = 0; i < writeBuf.Length; i++) writeBuf[i] = sampleCounter++;
            ring.Write(writeBuf);
            ring.Read(readBuf);

            foreach (float sample in readBuf)
            {
                if (previous is not null)
                {
                    double step = sample - previous.Value;
                    // Tolerance is wider than you'd expect for an exact-arithmetic check: ramp
                    // values climb into the thousands over 300 iterations, and float32 (the
                    // buffer's storage type, per the design doc's §5.9 "float end-to-end" pipeline)
                    // only carries ~7 significant decimal digits — rounding at that magnitude is a
                    // couple thousandths, not a bug.
                    Check.True(Math.Abs(step - 1.0) <= maxRateAdjustment + 1e-3,
                        $"ramp step of {step} implies a read rate outside the configured +/-{maxRateAdjustment} band");
                }
                previous = sample;
            }
        }

        Check.Equal(0L, ring.UnderrunSampleCount, "writing before every read in this test should never leave the buffer empty");
    }
}
