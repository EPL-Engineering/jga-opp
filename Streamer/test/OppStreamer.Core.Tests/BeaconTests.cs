namespace OppStreamer.Core.Tests;

/// <summary>
/// Covers the Beacon/Alert feature added 2026-08-22: a one-shot sound loaded once (typically at
/// startup), gated by an independently-toggleable enable flag, fired exactly once per trial at the
/// same loop boundary the trial's Signal window latches in — regardless of whether that trial
/// actually contains a probe, and never again for the rest of that trial's configured reps.
///
/// See <see cref="TrialStateMachine"/>'s onTrialStart callback and
/// <see cref="StreamerEngine.FireBeaconIfEnabled"/> for the wiring; this file exercises it end to
/// end through the public StreamerEngine surface.
/// </summary>
public static class BeaconTests
{
    private static float[] Marker(int loopLength, float value) => Enumerable.Repeat(value, loopLength).ToArray();

    private static StreamerEngine NewConfiguredEngine(int loopLen)
    {
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));
        return engine;
    }

    public static void Register(TestRunner runner)
    {
        runner.Test("Beacon does not play if never enabled, even with a sound loaded and a trial triggered", DisabledBeaconStaysSilent);
        runner.Test("Beacon does not play if enabled but no sound has been loaded", EnabledWithNoSoundStaysSilent);
        runner.Test("Beacon fires exactly at the boundary a trial's Signal window latches in", BeaconFiresAtTrialStartBoundary);
        runner.Test("Beacon fires for a no-probe trial too — regardless of probe presence", BeaconFiresRegardlessOfProbe);
        runner.Test("Beacon fires only once per trial, not once per repeat", BeaconFiresOnlyOncePerTrial);
        runner.Test("Disabling the Beacon after loading the sound prevents it from firing on the next trial", DisablingAfterLoadPreventsNextFire);
        runner.Test("RenderBeacon is independent of the loop-boundary latch, like TTS", BeaconPlaybackIsIndependentOfLatch);
        runner.Test("SetBeaconSound(null) throws", NullSoundThrows);
    }

    private static void DisabledBeaconStaysSilent()
    {
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconSound(new float[] { 9f, 9f, 9f });
        // Deliberately never calling SetBeaconEnabled(true).

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // crosses the trial-start boundary

        Span<float> beacon = stackalloc float[3];
        engine.RenderBeacon(beacon);
        Check.Equal(new float[] { 0f, 0f, 0f }, beacon, "Beacon must stay silent when never enabled");
    }

    private static void EnabledWithNoSoundStaysSilent()
    {
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconEnabled(true);
        // Deliberately never calling SetBeaconSound — "not yet configured" must be a safe no-op, not a throw.

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // crosses the trial-start boundary

        Span<float> beacon = stackalloc float[3];
        engine.RenderBeacon(beacon);
        Check.Equal(new float[] { 0f, 0f, 0f }, beacon, "Beacon must stay silent when enabled but no sound was ever loaded");
    }

    private static void BeaconFiresAtTrialStartBoundary()
    {
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconSound(new float[] { 5f, 6f, 7f });
        engine.SetBeaconEnabled(true);

        Span<float> c = stackalloc float[4], s = stackalloc float[4];

        // Before any trigger: no boundary has latched a trial in yet, so nothing should be queued.
        Span<float> beaconEarly = stackalloc float[3];
        engine.RenderBeacon(beaconEarly);
        Check.Equal(new float[] { 0f, 0f, 0f }, beaconEarly, "Beacon must not fire before any trial has been triggered");

        engine.Trigger(containsProbe: true);
        Check.True(!engine.TrialActiveWindowOpen, "Trigger() alone must not open the window immediately — only at the boundary");

        engine.RenderFrame(4, c, s); // crosses the boundary that actually starts the trial
        Check.True(engine.TrialActiveWindowOpen, "Trial should now be active");

        Span<float> beacon = stackalloc float[3];
        engine.RenderBeacon(beacon);
        Check.Equal(new float[] { 5f, 6f, 7f }, beacon, "Beacon should have been enqueued at exactly the trial-start boundary");
    }

    private static void BeaconFiresRegardlessOfProbe()
    {
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconSound(new float[] { 8f });
        engine.SetBeaconEnabled(true);

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.Trigger(containsProbe: false); // no-probe trial
        engine.RenderFrame(4, c, s);

        Span<float> beacon = stackalloc float[1];
        engine.RenderBeacon(beacon);
        Check.Equal(new float[] { 8f }, beacon, "Beacon must fire for a no-probe trial exactly the same as a probe trial — the PI wants to know a trial started, not that it contains a probe");
    }

    private static void BeaconFiresOnlyOncePerTrial()
    {
        const int loopLen = 2;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetNumReps(3);
        engine.SetBeaconSound(new float[] { 1f });
        engine.SetBeaconEnabled(true);

        Span<float> c = stackalloc float[2], s = stackalloc float[2];

        engine.Trigger(containsProbe: true);
        engine.RenderFrame(2, c, s); // boundary 1: trial starts, remainingReps = 3 — Beacon should enqueue here
        engine.RenderFrame(2, c, s); // boundary 2: rep 1 of 3 consumed — trial still active, Beacon must NOT re-fire
        engine.RenderFrame(2, c, s); // boundary 3: rep 2 of 3 consumed — still active, still must NOT re-fire
        engine.RenderFrame(2, c, s); // boundary 4: rep 3 of 3 consumed — trial ends, still must NOT fire again

        // Exactly one sample's worth should have ever been enqueued, across the whole trial.
        Span<float> beacon = stackalloc float[4];
        engine.RenderBeacon(beacon);
        Check.Equal(new float[] { 1f, 0f, 0f, 0f }, beacon, "Beacon should have enqueued its one-sample clip exactly once for the whole 3-rep trial, not once per rep");
    }

    private static void DisablingAfterLoadPreventsNextFire()
    {
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconSound(new float[] { 4f });
        engine.SetBeaconEnabled(true);

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // first trial starts — Beacon fires
        engine.RenderFrame(4, c, s); // trial ends (SetNumReps default 1)

        Span<float> beaconFirst = stackalloc float[1];
        engine.RenderBeacon(beaconFirst);
        Check.Equal(new float[] { 4f }, beaconFirst, "Sanity: Beacon fired for the first trial while enabled");

        // Now disable — the "changeable at any time" part of the spec — and trigger a second trial.
        engine.SetBeaconEnabled(false);
        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // second trial starts — Beacon must stay silent this time

        Span<float> beaconSecond = stackalloc float[1];
        engine.RenderBeacon(beaconSecond);
        Check.Equal(new float[] { 0f }, beaconSecond, "Beacon must not fire for a trial started after being disabled");
    }

    private static void BeaconPlaybackIsIndependentOfLatch()
    {
        // Same shape as SendTtsAndRenderTtsAreWiredThrough in StreamerEngineTests: Beacon audio,
        // once enqueued, drains via RenderBeacon exactly like TtsPlayer — no dependency on
        // WaitForLatch or any further RenderFrame boundary crossings.
        const int loopLen = 4;
        var engine = NewConfiguredEngine(loopLen);
        engine.SetBeaconSound(new float[] { 2f, 3f });
        engine.SetBeaconEnabled(true);

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // fires the Beacon

        // Read it back in small pieces, across multiple calls, same as TTS would drain.
        Span<float> first = stackalloc float[1];
        engine.RenderBeacon(first);
        Check.Equal(new float[] { 2f }, first, "First sample of the Beacon clip");

        Span<float> second = stackalloc float[2];
        engine.RenderBeacon(second);
        Check.Equal(new float[] { 3f, 0f }, second, "Second (last) sample, then silence — no dependency on any further loop boundary");
    }

    private static void NullSoundThrows()
    {
        var engine = new StreamerEngine();
        Check.Throws<ArgumentNullException>(() => engine.SetBeaconSound(null!), "SetBeaconSound(null) should throw, matching SendTts/Enqueue's own null-guard");
    }
}
