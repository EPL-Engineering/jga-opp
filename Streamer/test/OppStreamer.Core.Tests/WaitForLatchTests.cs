namespace OppStreamer.Core.Tests;

/// <summary>
/// Covers <see cref="StreamerEngine.WaitForLatch"/> — the general-purpose "has whatever I just
/// queued been applied yet" signal that replaces the old LabVIEW-era stop/start-around-every-change
/// pattern. Every mutating call (SetSignal, SetSubjectSignal, SetTrainer, SetStimulusSet/
/// SetTrainingStimulusSet, TrainTest, Trigger) resets the underlying signal; the next boundary
/// crossing after that — from ANY source, not tied to which specific call queued it — sets it. Same
/// "reset at the request, not at the wait" shape as RequestStop/WaitForStopBoundary, and
/// deliberately not one-shot: once true, it stays true until the next mutating call resets it.
///
/// <see cref="TrainTestBoundaryMapsOntoWaitForLatch"/> covers the concrete case that prompted this
/// question: OPP's old LabVIEW code polled an "IsTrainer" flag to find out when toggling
/// Test/Training had actually taken effect at the loop boundary — that flag was doing exactly what
/// WaitForLatch does here, just LabVIEW-side. TrainTest()/WaitForLatch() together replace it, with
/// no new API needed.
/// </summary>
public static class WaitForLatchTests
{
    private static float[] Marker(int loopLength, float value) => Enumerable.Repeat(value, loopLength).ToArray();

    private static StreamerEngine NewConfiguredEngine(int loopLen)
    {
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        return engine;
    }

    public static void Register(TestRunner runner)
    {
        runner.Test("WaitForLatch returns false before any boundary has been crossed", FalseBeforeBoundary);
        runner.Test("WaitForLatch returns true once a boundary has been crossed", TrueAfterBoundary);
        runner.Test("WaitForLatch needs no matching request — any mutating call's boundary counts", AnyMutationsBoundaryCounts);
        runner.Test("WaitForLatch times out (false) when nothing is configured to produce a boundary", TimesOutWithNothingConfigured);
        runner.Test("Once true, WaitForLatch stays true across later, unrelated boundaries — not one-shot", StaysTrueAcrossLaterBoundaries);
        runner.Test("A new mutating call resets the latch — a subsequent wait needs a fresh boundary", NewMutationResetsTheLatch);
        runner.Test("TrainTest + WaitForLatch replaces the old LabVIEW IsTrainer polling — false until the mode actually switches, true exactly at that boundary", TrainTestBoundaryMapsOntoWaitForLatch);
    }

    private static void FalseBeforeBoundary()
    {
        const int loopLen = 6;
        var engine = NewConfiguredEngine(loopLen);

        Span<float> c = stackalloc float[6], w = stackalloc float[6], s = stackalloc float[6];
        engine.RenderFrame(3, c[..3], w[..3], s[..3]); // partway through a loop, no boundary yet

        Check.True(!engine.WaitForLatch(TimeSpan.Zero), "No boundary has been crossed yet — a zero-timeout wait must report false");
    }

    private static void TrueAfterBoundary()
    {
        const int loopLen = 6;
        var engine = NewConfiguredEngine(loopLen);

        Span<float> c = stackalloc float[6], w = stackalloc float[6], s = stackalloc float[6];
        engine.RenderFrame(6, c, w, s); // crosses exactly one boundary

        Check.True(engine.WaitForLatch(TimeSpan.Zero), "A boundary was just crossed — a zero-timeout wait must now report true");
    }

    private static void AnyMutationsBoundaryCounts()
    {
        // No RequestStop() anywhere in this test — WaitForLatch must not require one. A plain
        // SetTrainer call, followed by ordinary playback crossing a boundary, is enough.
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);

        Span<float> c = stackalloc float[4], w = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(4, c, w, s); // establish a normal loop, crosses one boundary

        engine.SetTrainer(Participant.Caregiver, Marker(loopLen, 9f)); // the "just queued a change" moment — resets the latch
        Check.True(!engine.WaitForLatch(TimeSpan.Zero), "Queuing a change resets the latch — it hasn't applied yet");

        engine.RenderFrame(4, c, w, s); // this call both applies the queued Training buffer AND crosses the boundary
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "The boundary after SetTrainer should satisfy WaitForLatch, with no RequestStop involved");
    }

    private static void TimesOutWithNothingConfigured()
    {
        var engine = new StreamerEngine();
        // Reset() was never called — LoopLengthSamples is null, nothing is or could be playing, so
        // no boundary can ever be crossed. Unlike WaitForStopBoundary (which special-cases this to
        // resolve immediately, since a stop trivially "succeeds" when nothing needs stopping),
        // WaitForLatch has no such special case — a caller asking "has my change applied" when
        // nothing is playing genuinely should time out, since nothing ever will apply.
        Check.True(!engine.WaitForLatch(TimeSpan.Zero), "With nothing configured, no boundary can ever come — must time out, not hang or false-positive");
    }

    private static void StaysTrueAcrossLaterBoundaries()
    {
        const int loopLen = 2;
        var engine = NewConfiguredEngine(loopLen);

        Span<float> c = stackalloc float[2], w = stackalloc float[2], s = stackalloc float[2];
        engine.RenderFrame(2, c, w, s); // crosses the boundary — signal should fire
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "Boundary reached — should report true");

        // Further loops, with no new mutating call in between, shouldn't un-signal it — once
        // reached, it stays reached until the next mutating call resets it.
        engine.RenderFrame(2, c, w, s);
        engine.RenderFrame(2, c, w, s);
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "Should still report true across later, unrelated boundaries");
    }

    private static void NewMutationResetsTheLatch()
    {
        const int loopLen = 3;
        var engine = NewConfiguredEngine(loopLen);

        Span<float> c = stackalloc float[3], w = stackalloc float[3], s = stackalloc float[3];
        engine.RenderFrame(3, c, w, s); // crosses a boundary
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "First boundary should be observed");

        // A fresh mutating call means there's something new to confirm — the latch must go back to
        // "not yet applied" even though an earlier, unrelated change did already latch in.
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 5f));
        Check.True(!engine.WaitForLatch(TimeSpan.Zero), "A new mutating call must reset the latch, even though an earlier boundary had already satisfied it");

        engine.RenderFrame(3, c, w, s); // the boundary that actually applies the new SetSignal
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "The boundary after the new mutating call should satisfy WaitForLatch again");
    }

    private static void TrainTestBoundaryMapsOntoWaitForLatch()
    {
        // Mirrors StreamerEngineTests.TrainTestSwitchesAllThreeTogether's setup, but checks
        // WaitForLatch at each step instead of just the audio content — this is the actual
        // replacement for OPP's old LabVIEW IsTrainer polling loop.
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetTrainer(Participant.Caregiver, Marker(loopLen, 10f));
        engine.SetTrainer(Participant.Waver, Marker(loopLen, 20f));
        engine.SetSubjectSignal(OperatingMode.Training, isSignal: false, Marker(loopLen, 30f));

        Span<float> c = stackalloc float[4], w = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(2, c[..2], w[..2], s[..2]); // partway through a Test loop, no wrap yet

        engine.TrainTest(isTrainer: true); // the moment OPP's toggle would fire
        Check.True(!engine.WaitForLatch(TimeSpan.Zero), "Requested, not yet applied — must report false immediately after TrainTest()");

        // Rest of the in-progress Test loop — must finish on Test, unaffected by the pending switch.
        engine.RenderFrame(2, c[..2], w[..2], s[..2]);
        Check.Equal(Marker(2, 1f), c[..2], "must finish the in-progress loop on Test");
        Check.True(engine.WaitForLatch(TimeSpan.Zero), "The boundary at the end of that call is exactly when Training actually took effect — this is the signal that replaces IsTrainer");

        // Next loop: now actually, audibly on Training — confirms WaitForLatch's "true" wasn't a
        // false positive from mere bookkeeping (CurrentMode updates immediately inside TrainTest,
        // well before this boundary — WaitForLatch tracks the AUDIBLE switch, not that bookkeeping).
        engine.RenderFrame(4, c, w, s);
        Check.Equal(Marker(4, 10f), c, "Caregiver should now be audibly on Training");
        Check.Equal(Marker(4, 20f), w, "Waver should now be audibly on Training");
        Check.Equal(Marker(4, 30f), s, "Subject should now be audibly on Training");
    }
}
