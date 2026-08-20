namespace OppStreamer.Core.Tests;

/// <summary>
/// Exercises <see cref="WaveformMonitor"/> — DiagnosticsView's data source. Focus: bucket commit
/// timing (exactly every samplesPerBucket samples, not before), correct min/max per bucket,
/// multiple commits within one Accumulate call, the "ramp up from 0, then wrap" history length
/// behavior, wraparound ordering staying oldest-first, and channel independence.
/// </summary>
public static class WaveformMonitorTests
{
    public static void Register(TestRunner runner)
    {
        runner.Test("A partial bucket (fewer than samplesPerBucket samples) commits nothing yet", PartialBucketCommitsNothing);
        runner.Test("Exactly samplesPerBucket samples commits one bucket with the correct min/max", ExactBucketCommitsCorrectMinMax);
        runner.Test("A call spanning multiple buckets commits each one in order", MultiBucketCallCommitsEachInOrder);
        runner.Test("History length ramps up from 0 to BucketsPerChannel, then stays there", HistoryLengthRampsThenCaps);
        runner.Test("Once full, the oldest bucket is evicted and history stays oldest-first", WraparoundStaysOldestFirst);
        runner.Test("Channels accumulate independently — one channel's data never bleeds into another", ChannelsAreIndependent);
        runner.Test("A leftover partial bucket persists across calls until it's completed", PartialBucketCarriesOverBetweenCalls);
        runner.Test("Constructor and accessor argument validation", ArgumentValidation);
    }

    private static void PartialBucketCommitsNothing()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 4, bucketsPerChannel: 10);
        monitor.Accumulate(0, new float[] { 1f, 2f, 3f }); // only 3 of 4 needed
        monitor.GetSnapshot(0, out var min, out var max);
        Check.Equal(0, min.Length, "No bucket should have committed yet");
        Check.Equal(0, max.Length, "No bucket should have committed yet");
    }

    private static void ExactBucketCommitsCorrectMinMax()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 4, bucketsPerChannel: 10);
        monitor.Accumulate(0, new float[] { 3f, -1f, 5f, 0f });
        monitor.GetSnapshot(0, out var min, out var max);
        Check.Equal(new float[] { -1f }, min.AsSpan(), "Bucket min should be the smallest sample seen");
        Check.Equal(new float[] { 5f }, max.AsSpan(), "Bucket max should be the largest sample seen");
    }

    private static void MultiBucketCallCommitsEachInOrder()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 2, bucketsPerChannel: 10);
        // Three full buckets' worth in a single call: (1,2) (3,4) (5,6).
        monitor.Accumulate(0, new float[] { 1f, 2f, 3f, 4f, 5f, 6f });
        monitor.GetSnapshot(0, out var min, out var max);
        Check.Equal(new float[] { 1f, 3f, 5f }, min.AsSpan(), "Each bucket's min, oldest first");
        Check.Equal(new float[] { 2f, 4f, 6f }, max.AsSpan(), "Each bucket's max, oldest first");
    }

    private static void HistoryLengthRampsThenCaps()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 1, bucketsPerChannel: 3);

        monitor.Accumulate(0, new float[] { 1f });
        monitor.GetSnapshot(0, out var min1, out _);
        Check.Equal(1, min1.Length, "One sample committed = history length 1");

        monitor.Accumulate(0, new float[] { 2f });
        monitor.GetSnapshot(0, out var min2, out _);
        Check.Equal(2, min2.Length, "Two samples committed = history length 2");

        monitor.Accumulate(0, new float[] { 3f });
        monitor.GetSnapshot(0, out var min3, out _);
        Check.Equal(3, min3.Length, "Buffer now full (3 buckets)");

        monitor.Accumulate(0, new float[] { 4f }); // one more than capacity — should evict the oldest, not grow
        monitor.GetSnapshot(0, out var min4, out _);
        Check.Equal(3, min4.Length, "History length caps at BucketsPerChannel rather than growing further");
    }

    private static void WraparoundStaysOldestFirst()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 1, bucketsPerChannel: 3);

        // Fill the 3-slot buffer, then push two more — samples 1,2,3 should be evicted in order,
        // leaving 3,4,5 (oldest-first) after two evictions... actually 1 gets evicted first (by
        // sample 4), then 2 (by sample 5), leaving [3, 4, 5].
        monitor.Accumulate(0, new float[] { 1f, 2f, 3f, 4f, 5f });
        monitor.GetSnapshot(0, out var min, out _);
        Check.Equal(new float[] { 3f, 4f, 5f }, min.AsSpan(), "After wrapping past capacity, history should read oldest-to-newest of what remains");
    }

    private static void ChannelsAreIndependent()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0", "Ch1" }, samplesPerBucket: 2, bucketsPerChannel: 5);

        monitor.Accumulate(0, new float[] { 10f, 20f });
        monitor.Accumulate(1, new float[] { -5f, -1f });

        monitor.GetSnapshot(0, out var min0, out var max0);
        monitor.GetSnapshot(1, out var min1, out var max1);

        Check.Equal(new float[] { 10f }, min0.AsSpan(), "Channel 0's data must not be affected by channel 1's Accumulate calls");
        Check.Equal(new float[] { 20f }, max0.AsSpan(), "Channel 0's data must not be affected by channel 1's Accumulate calls");
        Check.Equal(new float[] { -5f }, min1.AsSpan(), "Channel 1's data must not be affected by channel 0's Accumulate calls");
        Check.Equal(new float[] { -1f }, max1.AsSpan(), "Channel 1's data must not be affected by channel 0's Accumulate calls");
    }

    private static void PartialBucketCarriesOverBetweenCalls()
    {
        var monitor = new WaveformMonitor(new[] { "Ch0" }, samplesPerBucket: 3, bucketsPerChannel: 10);

        monitor.Accumulate(0, new float[] { 5f, 1f }); // 2 of 3 — no commit yet
        monitor.GetSnapshot(0, out var minBefore, out _);
        Check.Equal(0, minBefore.Length, "Sanity: nothing committed yet");

        monitor.Accumulate(0, new float[] { 9f }); // completes the bucket: {5, 1, 9}
        monitor.GetSnapshot(0, out var minAfter, out var maxAfter);
        Check.Equal(new float[] { 1f }, minAfter.AsSpan(), "The completed bucket should reflect all 3 samples across both calls");
        Check.Equal(new float[] { 9f }, maxAfter.AsSpan(), "The completed bucket should reflect all 3 samples across both calls");
    }

    private static void ArgumentValidation()
    {
        Check.Throws<ArgumentException>(() => new WaveformMonitor(Array.Empty<string>(), 1, 1), "At least one channel name is required");
        Check.Throws<ArgumentOutOfRangeException>(() => new WaveformMonitor(new[] { "Ch0" }, 0, 1), "samplesPerBucket must be positive");
        Check.Throws<ArgumentOutOfRangeException>(() => new WaveformMonitor(new[] { "Ch0" }, 1, 0), "bucketsPerChannel must be positive");

        var monitor = new WaveformMonitor(new[] { "Ch0" }, 1, 1);
        Check.Throws<ArgumentOutOfRangeException>(() => monitor.Accumulate(1, new float[] { 1f }), "Accumulate should reject an out-of-range channel index");
        Check.Throws<ArgumentOutOfRangeException>(() => monitor.GetSnapshot(-1, out _, out _), "GetSnapshot should reject an out-of-range channel index");
    }
}
