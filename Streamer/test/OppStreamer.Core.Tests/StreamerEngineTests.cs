using OppStreamer.Core;

namespace OppStreamer.Core.Tests;

/// <summary>
/// Exercises the central bet of the whole redesign: that Trigger, TrainTest, and hot-swapped
/// training stimuli all apply exactly at loop boundaries — never mid-loop — including when
/// several of them need to land on the very same boundary together.
///
/// Buffers below use a distinct constant "marker" value per named buffer (e.g. 1.0f for Test
/// Signal, 10.0f for Training Signal) purely so tests can identify which buffer is currently
/// playing just by reading a sample.
///
/// 2026-08-22: rewritten for the Background/Signal redesign — Waver is gone (RenderFrame now takes
/// two spans, Caregiver/Subject), and the old per-participant SetSignal/SetTrainer are replaced by
/// mode-scoped SetBackground/SetSignal. See StimulusStore's class doc comment for the full reasoning.
/// </summary>
public static class StreamerEngineTests
{
    private static float[] Marker(int loopLength, float value) => Enumerable.Repeat(value, loopLength).ToArray();

    public static void Register(TestRunner runner)
    {
        runner.Test("Trigger applies only at the next loop boundary, not mid-loop", TriggerAppliesAtBoundaryOnly);
        runner.Test("Trigger(containsProbe: false) opens the trial window without changing Subject audio", NoProbeTrialOpensWindowSilently);
        runner.Test("TrainTest switches Caregiver and Subject together, at the same boundary", TrainTestSwitchesBothTogether);
        runner.Test("SetTrainingStimulusSet applies to both training buffers atomically", TrainingStimulusSetIsAtomic);
        runner.Test("SetStimulusSet applies to both buffers of an explicit mode atomically", StimulusSetIsAtomicForExplicitMode);
        runner.Test("SetStimulusSet(Test, ...) avoids the torn update that one-at-a-time SetSignal/SetBackground calls can produce", StimulusSetAvoidsTornUpdate);
        runner.Test("Mismatched buffer length is rejected", MismatchedLengthThrows);
        runner.Test("Multiple loop wraps within one frame are each handled in order (sample-accurate)", MultipleWrapsWithinOneFrame);
        runner.Test("A second Trigger() during an active trial is dropped, not queued", RetriggerDuringActiveTrialIsDropped);
        runner.Test("Writing to a buffer that isn't currently selected doesn't affect playback", InactiveBufferWriteIsSilent);
        runner.Test("SendTts/RenderTts are wired through to a working TtsPlayer, independent of the loop-boundary latch", SendTtsAndRenderTtsAreWiredThrough);
        runner.Test("Caregiver always plays the current mode's Signal buffer, never Background", CaregiverAlwaysPlaysSignal);
    }

    private static void TriggerAppliesAtBoundaryOnly()
    {
        const int loopLen = 10;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(1);

        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[10], s = stackalloc float[10];

        // First half of the loop: plain background, nothing triggered yet.
        engine.RenderFrame(5, c[..5], s[..5]);
        Check.Equal(Marker(5, 3f), s[..5], "Subject should be on Background before any trigger");
        Check.True(!engine.TrialActiveWindowOpen, "Trial window should not be open yet");

        engine.Trigger(containsProbe: true);
        Check.True(!engine.TrialActiveWindowOpen, "Trigger() alone must not open the window immediately — only at the boundary");

        // Second half of the SAME loop: must still be background — the boundary hasn't happened yet.
        engine.RenderFrame(5, c[..5], s[..5]);
        Check.Equal(Marker(5, 3f), s[..5], "Subject must stay on Background for the rest of the in-progress loop");
        Check.True(engine.TrialActiveWindowOpen, "Boundary at the end of that call should have opened the trial window");

        // Next full loop: now Signal, for exactly one rep (SetNumReps(1)).
        engine.RenderFrame(10, c, s);
        Check.Equal(Marker(10, 1f), s, "Subject should be on Signal for the triggered loop — the same buffer Caregiver always plays");
        Check.True(!engine.TrialActiveWindowOpen, "Window should close at the end of the single requested repetition");

        // Reverts to Background afterwards.
        engine.RenderFrame(10, c, s);
        Check.Equal(Marker(10, 3f), s, "Subject should revert to Background after the trial ends");
        Check.Equal(Marker(10, 1f), c, "Caregiver should be unaffected by Subject trial state throughout — it always plays Signal");
    }

    private static void NoProbeTrialOpensWindowSilently()
    {
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(2);
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));

        Span<float> c = stackalloc float[4], s = stackalloc float[4];

        engine.Trigger(containsProbe: false);
        engine.RenderFrame(4, c, s); // crosses the boundary that starts the trial: remainingReps = 2
        Check.True(engine.TrialActiveWindowOpen, "No-probe trial should still open the trial-active window");
        Check.Equal(Marker(4, 3f), s, "No-probe trial must not change what Subject actually hears");

        engine.RenderFrame(4, c, s); // rep 1 of 2 consumed: remainingReps = 1, still active
        Check.True(engine.TrialActiveWindowOpen, "Window should still be open after only 1 of 2 reps");
        Check.Equal(Marker(4, 3f), s, "Subject audio should remain Background throughout a no-probe trial");

        engine.RenderFrame(4, c, s); // rep 2 of 2 consumed: remainingReps = 0, trial ends
        Check.True(!engine.TrialActiveWindowOpen, "Window should close after the configured number of reps even with no probe");
        Check.Equal(Marker(4, 3f), s, "Subject audio should remain Background throughout a no-probe trial");
    }

    private static void TrainTestSwitchesBothTogether()
    {
        const int loopLen = 6;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        engine.SetSignal(OperatingMode.Training, Marker(loopLen, 10f));
        engine.SetBackground(OperatingMode.Training, Marker(loopLen, 30f));

        Span<float> c = stackalloc float[6], s = stackalloc float[6];

        engine.RenderFrame(3, c[..3], s[..3]); // partway through the Test loop
        engine.TrainTest(isTrainer: true);

        // Rest of the in-progress Test loop must be unaffected.
        engine.RenderFrame(3, c[..3], s[..3]);
        Check.Equal(Marker(3, 1f), c[..3], "Caregiver must finish the in-progress loop on Test");
        Check.Equal(Marker(3, 3f), s[..3], "Subject must finish the in-progress loop on Test");

        // Next loop: both switch to Training together, in the same call.
        engine.RenderFrame(6, c, s);
        Check.Equal(Marker(6, 10f), c, "Caregiver should be on Training Signal after the boundary");
        Check.Equal(Marker(6, 30f), s, "Subject should be on Training Background after the boundary");
    }

    private static void TrainingStimulusSetIsAtomic()
    {
        const int loopLen = 5;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));
        engine.SetSignal(OperatingMode.Training, Marker(loopLen, 10f));
        engine.SetBackground(OperatingMode.Training, Marker(loopLen, 30f));

        Span<float> c = stackalloc float[5], s = stackalloc float[5];

        engine.TrainTest(isTrainer: true);
        engine.RenderFrame(5, c, s); // cross into Training

        // Now hot-swap the training set mid-loop.
        engine.RenderFrame(2, c[..2], s[..2]);
        engine.SetTrainingStimulusSet(
            background: Marker(loopLen, 300f),
            signal: Marker(loopLen, 100f));

        // Rest of the in-progress loop must still show the OLD training content.
        engine.RenderFrame(3, c[..3], s[..3]);
        Check.Equal(Marker(3, 10f), c[..3], "Caregiver must finish the loop on the old training buffer");
        Check.Equal(Marker(3, 30f), s[..3], "Subject must finish the loop on the old training buffer");

        // Next loop: both land on the NEW content simultaneously, in one call.
        engine.RenderFrame(5, c, s);
        Check.Equal(Marker(5, 100f), c, "Caregiver should reflect the new training set");
        Check.Equal(Marker(5, 300f), s, "Subject should reflect the new training set");
    }

    private static void StimulusSetIsAtomicForExplicitMode()
    {
        // Mirrors TrainingStimulusSetIsAtomic, but exercises the mode-parameterized entry point
        // directly against Test — the mode OPP's "currently selected Phase" actually plays through,
        // and the case that originally prompted this method (see StimulusSetAvoidsTornUpdate).
        const int loopLen = 5;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[5], s = stackalloc float[5];

        engine.RenderFrame(2, c[..2], s[..2]); // partway through a Test loop
        engine.SetStimulusSet(OperatingMode.Test,
            background: Marker(loopLen, 300f),
            signal: Marker(loopLen, 100f));

        // Rest of the in-progress loop must still show the OLD content.
        engine.RenderFrame(3, c[..3], s[..3]);
        Check.Equal(Marker(3, 1f), c[..3], "Caregiver must finish the loop on the old buffer");
        Check.Equal(Marker(3, 3f), s[..3], "Subject must finish the loop on the old buffer");

        // Next loop: both land on the NEW content simultaneously, in one call.
        engine.RenderFrame(5, c, s);
        Check.Equal(Marker(5, 100f), c, "Caregiver should reflect the new stimulus set");
        Check.Equal(Marker(5, 300f), s, "Subject should reflect the new stimulus set");
    }

    private static void StimulusSetAvoidsTornUpdate()
    {
        // Demonstrates the exact hazard SetStimulusSet was built to close (see its doc comment and
        // ConfigApi.SetStimulusSet's), and confirms it's actually closed. Two identically-bootstrapped
        // engines: one updated via two separate one-at-a-time calls with a loop boundary landing in
        // between (standing in for an audio-thread boundary crossing mid-sequence — the real
        // production race), the other via one SetStimulusSet call. The one-at-a-time engine should
        // show a visibly torn loop pass; the atomic one should not.
        const int loopLen = 4;

        StreamerEngine Bootstrap()
        {
            var engine = new StreamerEngine();
            engine.Reset(loopLen);
            engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
            engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));
            return engine;
        }

        Span<float> c = stackalloc float[4], s = stackalloc float[4];

        // --- One-at-a-time: Signal is changed and queued, then a boundary is crossed before
        // Background is ever set — exactly like an unlucky audio-thread timing in production.
        var tornEngine = Bootstrap();
        tornEngine.SetSignal(OperatingMode.Test, Marker(loopLen, 100f));
        tornEngine.RenderFrame(4, c, s); // crosses a boundary — only Signal's change was queued for it

        tornEngine.SetBackground(OperatingMode.Test, Marker(loopLen, 300f));

        tornEngine.RenderFrame(4, c, s); // this loop pass shows the TORN mix
        Check.Equal(Marker(4, 100f), c, "Caregiver already switched (it was queued before the boundary)...");
        Check.Equal(Marker(4, 3f), s, "...but Subject is torn — stuck on the OLD Background for this whole loop pass, exactly the hazard SetStimulusSet exists to close");

        tornEngine.RenderFrame(4, c, s); // next boundary — the rest finally catches up, one full loop late
        Check.Equal(Marker(4, 300f), s, "Subject only catches up on the FOLLOWING boundary");

        // --- Atomic: the exact same overall change, issued as one SetStimulusSet call — no boundary
        // can land "inside" a single call, so nothing is ever torn.
        var atomicEngine = Bootstrap();
        atomicEngine.SetStimulusSet(OperatingMode.Test,
            background: Marker(loopLen, 300f),
            signal: Marker(loopLen, 100f));
        atomicEngine.RenderFrame(4, c, s); // boundary crossed; both drained together
        atomicEngine.RenderFrame(4, c, s); // observe: both landed on the SAME boundary
        Check.Equal(Marker(4, 100f), c, "Caregiver reflects the new set");
        Check.Equal(Marker(4, 300f), s, "Subject reflects the new set, on the SAME boundary as Caregiver — nothing torn");
    }

    private static void MismatchedLengthThrows()
    {
        var engine = new StreamerEngine();
        engine.Reset(10);
        Check.Throws<ArgumentException>(
            () => engine.SetSignal(OperatingMode.Test, new float[5]),
            "A 5-sample buffer should be rejected when the phase's loop length is 10 samples");
    }

    private static void MultipleWrapsWithinOneFrame()
    {
        const int loopLen = 3;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(2);
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 4f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        engine.Trigger(containsProbe: true);

        Span<float> c = stackalloc float[10], s = stackalloc float[10];
        engine.RenderFrame(10, c, s); // spans three wraps of the 3-sample loop in one call

        // One full background loop (trigger hasn't hit a boundary yet), then two full signal loops
        // (SetNumReps(2)), then back to background — all within this single 10-sample call.
        float[] expected = { 3f, 3f, 3f, 4f, 4f, 4f, 4f, 4f, 4f, 3f };
        Check.Equal(expected, s, "Subject samples should show exactly one background loop, two signal loops, then background again");
        Check.Equal(Marker(10, 4f), c, "Caregiver should be unaffected by the Subject trial — always Signal");
    }

    private static void RetriggerDuringActiveTrialIsDropped()
    {
        const int loopLen = 2;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(3);
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 4f));

        Span<float> c = stackalloc float[2], s = stackalloc float[2];

        engine.Trigger(containsProbe: true);
        engine.RenderFrame(2, c, s); // crosses the boundary that starts the trial: remainingReps = 3

        // Retriggering mid-trial should be a no-op — it must not extend or restart the countdown.
        engine.Trigger(containsProbe: true);
        engine.Trigger(containsProbe: false);

        engine.RenderFrame(2, c, s); // rep 1 of 3 consumed: remainingReps = 2
        Check.True(engine.TrialActiveWindowOpen, "Still mid-trial after 1 of 3 reps");
        engine.RenderFrame(2, c, s); // rep 2 of 3 consumed: remainingReps = 1
        Check.True(engine.TrialActiveWindowOpen, "Still mid-trial after 2 of 3 reps");
        engine.RenderFrame(2, c, s); // rep 3 of 3 consumed: remainingReps = 0 — should end here
        Check.True(!engine.TrialActiveWindowOpen, "Trial should end after exactly the original 3 reps, unaffected by the dropped retriggers");

        engine.RenderFrame(2, c, s);
        Check.Equal(Marker(2, 3f), s, "Subject should be back on Background with no further trial pending");
        Check.True(!engine.TrialActiveWindowOpen, "No queued retrigger should have started a new trial");
    }

    private static void InactiveBufferWriteIsSilent()
    {
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(4, c, s);
        Check.Equal(Marker(4, 1f), c, "Sanity check before the inactive write");

        // Training isn't selected — writing to it should have zero effect on current playback,
        // immediately or ever, until something actually switches into Training.
        engine.SetSignal(OperatingMode.Training, Marker(loopLen, 999f));

        engine.RenderFrame(4, c, s);
        Check.Equal(Marker(4, 1f), c, "Writing an unselected buffer must not affect current (Test-mode) playback");
    }

    private static void SendTtsAndRenderTtsAreWiredThrough()
    {
        // TtsPlayer itself is covered exhaustively by TtsPlayerTests; this just confirms
        // StreamerEngine's SendTts/RenderTts genuinely reach a live TtsPlayer instance, and that
        // TTS playback is untouched by loop-boundary/trial machinery entirely — no Reset(),
        // Trigger(), or RenderFrame() call is needed for it to work.
        var engine = new StreamerEngine();

        Span<float> tts = stackalloc float[4];
        engine.RenderTts(tts);
        Check.Equal(new float[] { 0f, 0f, 0f, 0f }, tts, "RenderTts should produce silence before anything is ever sent");

        engine.SendTts(new float[] { 1f, 2f, 3f });
        engine.RenderTts(tts);
        Check.Equal(new float[] { 1f, 2f, 3f, 0f }, tts, "RenderTts should play back exactly what was sent via SendTts, then pad with silence");

        // Confirm it keeps working interleaved with the loop-boundary-driven Caregiver/Subject
        // path, without either side affecting the other.
        const int loopLen = 4;
        engine.Reset(loopLen);
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        engine.SendTts(new float[] { 9f, 9f });
        Span<float> c = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(4, c, s);
        Check.Equal(Marker(4, 1f), c, "RenderFrame's Caregiver output should be unaffected by TTS being queued");

        engine.RenderTts(tts);
        Check.Equal(new float[] { 9f, 9f, 0f, 0f }, tts, "TTS queued while RenderFrame calls happened should still be there afterward, unaffected by loop boundaries");
    }

    private static void CaregiverAlwaysPlaysSignal()
    {
        // The core simplification of the 2026-08-22 redesign, made explicit as its own test:
        // Caregiver has no buffer of its own anymore — it's always just "the current mode's Signal
        // buffer" — regardless of what Subject is doing (Background, mid-trial Signal, or anything
        // else). This isn't a new behavior (it matches what "SetCaregiver = SetSubjectSignal" always
        // amounted to in practice), just confirming the simplified model preserves it exactly.
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(1);
        engine.SetSignal(OperatingMode.Test, Marker(loopLen, 7f));
        engine.SetBackground(OperatingMode.Test, Marker(loopLen, 3f));

        Span<float> c = stackalloc float[4], s = stackalloc float[4];

        engine.RenderFrame(4, c, s);
        Check.Equal(Marker(4, 7f), c, "Caregiver plays Signal even while Subject is on Background");

        engine.Trigger(containsProbe: true);
        engine.RenderFrame(4, c, s); // crosses the boundary that starts the trial
        engine.RenderFrame(4, c, s); // the triggered loop — Subject now also on Signal
        Check.Equal(Marker(4, 7f), c, "Caregiver plays the exact same Signal buffer while Subject is also on Signal");
        Check.Equal(Marker(4, 7f), s, "Subject's Signal content is identical to Caregiver's — same shared buffer");
    }
}
