using OppStreamer.Core;

namespace OppStreamer.Core.Tests;

/// <summary>
/// Exercises TtsPlayer's FIFO semantics directly — it's pure Core logic with no hardware
/// dependency, so unlike MicBridge it needs no simulation harness to get genuine coverage.
/// </summary>
public static class TtsPlayerTests
{
    public static void Register(TestRunner runner)
    {
        runner.Test("Read() before any Enqueue() produces silence", SilenceWhenEmpty);
        runner.Test("Enqueue() then Read() plays back exactly what was queued", PlaysBackQueuedSamples);
        runner.Test("A Read() that exhausts the current buffer pads the remainder with silence", PadsRemainderWithSilenceWhenQueueRunsDry);
        runner.Test("A later Enqueue() appends after what's already playing, not interrupting it", LaterEnqueueAppendsRatherThanInterrupts);
        runner.Test("A single Read() seamlessly crosses from one queued buffer into the next with no gap", ReadCrossesBufferBoundarySeamlessly);
        runner.Test("Multiple small Read() calls drain one larger queued buffer correctly across calls", MultipleReadsDrainOneBufferAcrossCalls);
        runner.Test("Enqueue() of an empty array is a no-op", EmptyArrayEnqueueIsNoOp);
        runner.Test("Enqueue(null) throws", NullEnqueueThrows);
        runner.Test("IsPlaying and QueuedSampleCount reflect FIFO state accurately as it drains", IsPlayingAndQueuedSampleCountTrackState);
    }

    private static void SilenceWhenEmpty()
    {
        var player = new TtsPlayer();
        var dest = new float[10];
        Array.Fill(dest, -1f); // sentinel

        player.Read(dest);

        foreach (float sample in dest)
            Check.Equal(0f, sample, "reading from an empty TtsPlayer should produce silence, not garbage or the sentinel");
        Check.True(!player.IsPlaying, "IsPlaying should be false when nothing has ever been queued");
        Check.Equal(0L, player.QueuedSampleCount, "QueuedSampleCount should be zero when nothing has ever been queued");
    }

    private static void PlaysBackQueuedSamples()
    {
        var player = new TtsPlayer();
        float[] signal = { 1f, 2f, 3f, 4f, 5f };
        player.Enqueue(signal);

        var dest = new float[5];
        player.Read(dest);

        Check.Equal((ReadOnlySpan<float>)signal, dest, "Read() should reproduce exactly what was Enqueue()d");
    }

    private static void PadsRemainderWithSilenceWhenQueueRunsDry()
    {
        var player = new TtsPlayer();
        player.Enqueue(new float[] { 1f, 2f, 3f });

        var dest = new float[6];
        Array.Fill(dest, -1f);
        player.Read(dest);

        Check.Equal((ReadOnlySpan<float>)new float[] { 1f, 2f, 3f, 0f, 0f, 0f }, dest, "the queue running dry mid-Read should pad the remainder with silence");
        Check.True(!player.IsPlaying, "IsPlaying should be false once the queue has fully drained");
    }

    private static void LaterEnqueueAppendsRatherThanInterrupts()
    {
        var player = new TtsPlayer();
        player.Enqueue(new float[] { 1f, 2f, 3f });

        var first = new float[2];
        player.Read(first); // consume part of the first buffer

        // Enqueue a second buffer mid-playback of the first — it must NOT interrupt what's
        // already playing; it should only be heard after the first buffer is exhausted.
        player.Enqueue(new float[] { 10f, 20f });

        var rest = new float[3];
        player.Read(rest);

        Check.Equal((ReadOnlySpan<float>)new float[] { 1f, 2f }, first, "first Read() should have played the start of the first buffer, unaffected by the later Enqueue");
        Check.Equal((ReadOnlySpan<float>)new float[] { 3f, 10f, 20f }, rest, "the second buffer should only be heard after the first buffer's remaining sample, not interrupt it");
    }

    private static void ReadCrossesBufferBoundarySeamlessly()
    {
        var player = new TtsPlayer();
        player.Enqueue(new float[] { 1f, 2f, 3f });
        player.Enqueue(new float[] { 4f, 5f, 6f, 7f });

        var dest = new float[6]; // spans all of buffer 1 and part of buffer 2 in one Read() call
        player.Read(dest);

        Check.Equal((ReadOnlySpan<float>)new float[] { 1f, 2f, 3f, 4f, 5f, 6f }, dest, "a single Read() spanning two queued buffers should cross the boundary with no gap or reordering");

        var dest2 = new float[2];
        player.Read(dest2);
        Check.Equal((ReadOnlySpan<float>)new float[] { 7f, 0f }, dest2, "the remaining sample of the second buffer should play, then silence once the queue is empty");
    }

    private static void MultipleReadsDrainOneBufferAcrossCalls()
    {
        var player = new TtsPlayer();
        player.Enqueue(new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f });

        var a = new float[3];
        var b = new float[2];
        var c = new float[3]; // over-reads by one sample past the 7 queued -> trailing silence

        player.Read(a);
        player.Read(b);
        player.Read(c);

        Check.Equal((ReadOnlySpan<float>)new float[] { 1f, 2f, 3f }, a, "first chunk should match");
        Check.Equal((ReadOnlySpan<float>)new float[] { 4f, 5f }, b, "second chunk should continue exactly where the first left off");
        Check.Equal((ReadOnlySpan<float>)new float[] { 6f, 7f, 0f }, c, "third chunk should finish the buffer then pad with silence");
    }

    private static void EmptyArrayEnqueueIsNoOp()
    {
        var player = new TtsPlayer();
        player.Enqueue(Array.Empty<float>());

        Check.True(!player.IsPlaying, "enqueuing an empty array should not leave a dead entry that makes IsPlaying report true");
        Check.Equal(0L, player.QueuedSampleCount, "an empty-array enqueue should contribute nothing to QueuedSampleCount");

        // Confirm it doesn't jam the queue for a real buffer enqueued afterward.
        player.Enqueue(new float[] { 9f });
        var dest = new float[1];
        player.Read(dest);
        Check.Equal((ReadOnlySpan<float>)new float[] { 9f }, dest, "a real buffer enqueued after an empty one should still play normally");
    }

    private static void NullEnqueueThrows()
    {
        var player = new TtsPlayer();
        Check.Throws<ArgumentNullException>(() => player.Enqueue(null!), "Enqueue(null) should throw ArgumentNullException, matching every other Core API's null-signal handling");
    }

    private static void IsPlayingAndQueuedSampleCountTrackState()
    {
        var player = new TtsPlayer();
        Check.True(!player.IsPlaying, "should not be playing before anything is queued");

        player.Enqueue(new float[] { 1f, 2f, 3f, 4f });
        Check.True(player.IsPlaying, "should be playing immediately after Enqueue, before any Read");
        Check.Equal(4L, player.QueuedSampleCount, "QueuedSampleCount should reflect the full buffer before any Read");

        player.Enqueue(new float[] { 5f, 6f });
        Check.Equal(6L, player.QueuedSampleCount, "QueuedSampleCount should include both the in-progress buffer and everything queued behind it");

        var dest = new float[3];
        player.Read(dest);
        Check.Equal(3L, player.QueuedSampleCount, "QueuedSampleCount should shrink by exactly what was read");
        Check.True(player.IsPlaying, "should still be playing with samples left in the first buffer plus a queued second buffer");

        var dest2 = new float[3];
        player.Read(dest2); // finishes buffer 1's last sample, then all of buffer 2
        Check.Equal(0L, player.QueuedSampleCount, "QueuedSampleCount should be zero once everything queued has been read");
        Check.True(!player.IsPlaying, "should not be playing once the queue has fully drained");
    }
}
