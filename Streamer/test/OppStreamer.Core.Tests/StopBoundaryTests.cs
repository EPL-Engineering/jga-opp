namespace OppStreamer.Core.Tests;

/// <summary>
/// Covers <see cref="StreamerEngine.RequestStop"/>/<see cref="StreamerEngine.WaitForStopBoundary"/>
/// and the <see cref="StimulusStore.RequestSilence"/> mechanism underneath — the click-free Stop()
/// feature: playback must finish the loop pass already in progress (never cut mid-waveform) and
/// only go quiet exactly at the next boundary, with the "boundary reached" signal firing at
/// precisely that point, not before and not (indefinitely) after.
/// </summary>
public static class StopBoundaryTests
{
    private static float[] Marker(int loopLength, float value) => Enumerable.Repeat(value, loopLength).ToArray();

    public static void Register(TestRunner runner)
    {
        runner.Test("RequestStop leaves the in-progress loop untouched until the boundary", StopDoesNotCutMidLoop);
        runner.Test("RequestStop's silence lands exactly at the boundary, all three participants together", StopSilencesAllThreeAtBoundary);
        runner.Test("WaitForStopBoundary returns false before the boundary and true once it's reached", WaitReflectsBoundaryState);
        runner.Test("RequestStop with nothing configured yet resolves immediately (nothing to wait on)", StopWithNothingConfiguredIsImmediate);
        runner.Test("A stop request is a one-shot signal — it doesn't refire on later, unrelated boundaries", StopSignalFiresOnlyOnce);
    }

    private static void StopDoesNotCutMidLoop()
    {
        const int loopLen = 10;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[10], w = stackalloc float[10], s = stackalloc float[10];

        // Play through the first half of the loop normally, then request a stop mid-loop.
        engine.RenderFrame(5, c[..5], w[..5], s[..5]);
        Check.Equal(Marker(5, 1f), c[..5], "Sanity check before RequestStop");
        engine.RequestStop();

        // The rest of THIS loop pass must still be the real content — no cutting mid-waveform.
        engine.RenderFrame(5, c[..5], w[..5], s[..5]);
        Check.Equal(Marker(5, 1f), c[..5], "Caregiver must finish the in-progress loop after RequestStop, not go silent mid-loop");
        Check.Equal(Marker(5, 2f), w[..5], "Waver must finish the in-progress loop after RequestStop, not go silent mid-loop");
        Check.Equal(Marker(5, 3f), s[..5], "Subject must finish the in-progress loop after RequestStop, not go silent mid-loop");
    }

    private static void StopSilencesAllThreeAtBoundary()
    {
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[4], w = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(4, c, w, s); // establish a normal loop first (crosses one boundary, lands back at cursor 0)
        engine.RequestStop();

        // This call renders the loop pass that was ALREADY the "current" one the instant
        // RequestStop() was called (cursor was sitting at exactly 0) — same rule as
        // StopDoesNotCutMidLoop: the pending silence isn't drained until THIS call's own
        // boundary, at its end, so it must still show the real content throughout.
        engine.RenderFrame(4, c, w, s);
        Check.Equal(Marker(4, 1f), c, "The loop pass in progress when RequestStop() was called must still be real content");

        // Only the NEXT loop — the first one to start after that drain — should be silent.
        engine.RenderFrame(4, c, w, s);
        Check.Equal(Marker(4, 0f), c, "Caregiver should be silent after the boundary following RequestStop");
        Check.Equal(Marker(4, 0f), w, "Waver should be silent after the boundary following RequestStop");
        Check.Equal(Marker(4, 0f), s, "Subject should be silent after the boundary following RequestStop");
    }

    private static void WaitReflectsBoundaryState()
    {
        const int loopLen = 6;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[6], w = stackalloc float[6], s = stackalloc float[6];
        engine.RenderFrame(3, c[..3], w[..3], s[..3]); // partway through a loop, no boundary yet

        engine.RequestStop();
        Check.True(!engine.WaitForStopBoundary(TimeSpan.Zero), "Boundary hasn't happened yet — a zero-timeout wait must report false");

        engine.RenderFrame(3, c[..3], w[..3], s[..3]); // crosses the boundary that ends this loop
        Check.True(engine.WaitForStopBoundary(TimeSpan.Zero), "Boundary just happened — a zero-timeout wait must now report true");
    }

    private static void StopWithNothingConfiguredIsImmediate()
    {
        var engine = new StreamerEngine();
        // Reset() was never called — LoopLengthSamples is null, nothing is or could be playing.
        engine.RequestStop();
        Check.True(engine.WaitForStopBoundary(TimeSpan.Zero), "With nothing configured yet, there's no loop to gate against — the wait should resolve immediately");
    }

    private static void StopSignalFiresOnlyOnce()
    {
        const int loopLen = 2;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[2], w = stackalloc float[2], s = stackalloc float[2];

        engine.RequestStop();
        engine.RenderFrame(2, c, w, s); // crosses the boundary — signal should fire
        Check.True(engine.WaitForStopBoundary(TimeSpan.Zero), "Boundary reached — should report true");

        // Further loops (no new RequestStop) shouldn't un-signal or otherwise misbehave — once
        // reached, it stays reached until the next RequestStop() call resets it.
        engine.RenderFrame(2, c, w, s);
        engine.RenderFrame(2, c, w, s);
        Check.True(engine.WaitForStopBoundary(TimeSpan.Zero), "Should still report true across later, unrelated boundaries");
    }
}
