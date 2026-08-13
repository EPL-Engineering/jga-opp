using OppStreamer.Core;

namespace OppStreamer.Core.Tests;

/// <summary>
/// Exercises the central bet of the whole redesign: that Trigger, TrainTest, and hot-swapped
/// training stimuli all apply exactly at loop boundaries — never mid-loop — including when
/// several of them need to land on the very same boundary together.
///
/// Buffers below use a distinct constant "marker" value per named buffer (e.g. 1.0f for Caregiver
/// Test, 10.0f for Caregiver Training) purely so tests can identify which buffer is currently
/// playing just by reading a sample.
/// </summary>
public static class StreamerEngineTests
{
    private static float[] Marker(int loopLength, float value) => Enumerable.Repeat(value, loopLength).ToArray();

    public static void Register(TestRunner runner)
    {
        runner.Test("Trigger applies only at the next loop boundary, not mid-loop", TriggerAppliesAtBoundaryOnly);
        runner.Test("Trigger(containsProbe: false) opens the trial window without changing Subject audio", NoProbeTrialOpensWindowSilently);
        runner.Test("TrainTest switches all three participants together, at the same boundary", TrainTestSwitchesAllThreeTogether);
        runner.Test("SetTrainingStimulusSet applies to all four training buffers atomically", TrainingStimulusSetIsAtomic);
        runner.Test("Mismatched buffer length is rejected", MismatchedLengthThrows);
        runner.Test("Multiple loop wraps within one frame are each handled in order (sample-accurate)", MultipleWrapsWithinOneFrame);
        runner.Test("A second Trigger() during an active trial is dropped, not queued", RetriggerDuringActiveTrialIsDropped);
        runner.Test("Writing to a buffer that isn't currently selected doesn't affect playback", InactiveBufferWriteIsSilent);
    }

    private static void TriggerAppliesAtBoundaryOnly()
    {
        const int loopLen = 10;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(1);

        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f)); // Background
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: true, Marker(loopLen, 4f));  // Signal

        Span<float> c = stackalloc float[10], w = stackalloc float[10], s = stackalloc float[10];

        // First half of the loop: plain background, nothing triggered yet.
        engine.RenderFrame(5, c[..5], w[..5], s[..5]);
        Check.Equal(Marker(5, 3f), s[..5], "Subject should be on Background before any trigger");
        Check.True(!engine.TrialActiveWindowOpen, "Trial window should not be open yet");

        engine.Trigger(containsProbe: true);
        Check.True(!engine.TrialActiveWindowOpen, "Trigger() alone must not open the window immediately — only at the boundary");

        // Second half of the SAME loop: must still be background — the boundary hasn't happened yet.
        engine.RenderFrame(5, c[..5], w[..5], s[..5]);
        Check.Equal(Marker(5, 3f), s[..5], "Subject must stay on Background for the rest of the in-progress loop");
        Check.True(engine.TrialActiveWindowOpen, "Boundary at the end of that call should have opened the trial window");

        // Next full loop: now Signal, for exactly one rep (SetNumReps(1)).
        engine.RenderFrame(10, c, w, s);
        Check.Equal(Marker(10, 4f), s, "Subject should be on Signal for the triggered loop");
        Check.True(!engine.TrialActiveWindowOpen, "Window should close at the end of the single requested repetition");

        // Reverts to Background afterwards.
        engine.RenderFrame(10, c, w, s);
        Check.Equal(Marker(10, 3f), s, "Subject should revert to Background after the trial ends");
        Check.Equal(Marker(10, 1f), c, "Caregiver should be unaffected by Subject trial state throughout");
        Check.Equal(Marker(10, 2f), w, "Waver should be unaffected by Subject trial state throughout");
    }

    private static void NoProbeTrialOpensWindowSilently()
    {
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(2);
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: true, Marker(loopLen, 4f));
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));

        Span<float> c = stackalloc float[4], w = stackalloc float[4], s = stackalloc float[4];

        engine.Trigger(containsProbe: false);
        engine.RenderFrame(4, c, w, s); // crosses the boundary that starts the trial: remainingReps = 2
        Check.True(engine.TrialActiveWindowOpen, "No-probe trial should still open the trial-active window");
        Check.Equal(Marker(4, 3f), s, "No-probe trial must not change what Subject actually hears");

        engine.RenderFrame(4, c, w, s); // rep 1 of 2 consumed: remainingReps = 1, still active
        Check.True(engine.TrialActiveWindowOpen, "Window should still be open after only 1 of 2 reps");
        Check.Equal(Marker(4, 3f), s, "Subject audio should remain Background throughout a no-probe trial");

        engine.RenderFrame(4, c, w, s); // rep 2 of 2 consumed: remainingReps = 0, trial ends
        Check.True(!engine.TrialActiveWindowOpen, "Window should close after the configured number of reps even with no probe");
        Check.Equal(Marker(4, 3f), s, "Subject audio should remain Background throughout a no-probe trial");
    }

    private static void TrainTestSwitchesAllThreeTogether()
    {
        const int loopLen = 6;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));

        engine.SetTrainer(Participant.Caregiver, Marker(loopLen, 10f));
        engine.SetTrainer(Participant.Waver, Marker(loopLen, 20f));
        engine.SetSubjectSignal(OperatingMode.Training, isSignal: false, Marker(loopLen, 30f));
        engine.SetSubjectSignal(OperatingMode.Training, isSignal: true, Marker(loopLen, 40f));

        Span<float> c = stackalloc float[6], w = stackalloc float[6], s = stackalloc float[6];

        engine.RenderFrame(3, c[..3], w[..3], s[..3]); // partway through the Test loop
        engine.TrainTest(isTrainer: true);

        // Rest of the in-progress Test loop must be unaffected.
        engine.RenderFrame(3, c[..3], w[..3], s[..3]);
        Check.Equal(Marker(3, 1f), c[..3], "Caregiver must finish the in-progress loop on Test");
        Check.Equal(Marker(3, 2f), w[..3], "Waver must finish the in-progress loop on Test");
        Check.Equal(Marker(3, 3f), s[..3], "Subject must finish the in-progress loop on Test");

        // Next loop: all three switch to Training together, in the same call.
        engine.RenderFrame(6, c, w, s);
        Check.Equal(Marker(6, 10f), c, "Caregiver should be on Training after the boundary");
        Check.Equal(Marker(6, 20f), w, "Waver should be on Training after the boundary");
        Check.Equal(Marker(6, 30f), s, "Subject should be on Training Background after the boundary");
    }

    private static void TrainingStimulusSetIsAtomic()
    {
        const int loopLen = 5;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);

        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetTrainer(Participant.Caregiver, Marker(loopLen, 10f));
        engine.SetTrainer(Participant.Waver, Marker(loopLen, 20f));
        engine.SetSubjectSignal(OperatingMode.Training, isSignal: false, Marker(loopLen, 30f));
        engine.SetSubjectSignal(OperatingMode.Training, isSignal: true, Marker(loopLen, 40f));

        Span<float> c = stackalloc float[5], w = stackalloc float[5], s = stackalloc float[5];

        engine.TrainTest(isTrainer: true);
        engine.RenderFrame(5, c, w, s); // cross into Training

        // Now hot-swap the training set mid-loop.
        engine.RenderFrame(2, c[..2], w[..2], s[..2]);
        engine.SetTrainingStimulusSet(
            caregiver: Marker(loopLen, 100f),
            waver: Marker(loopLen, 200f),
            subjectBackground: Marker(loopLen, 300f),
            subjectSignal: Marker(loopLen, 400f));

        // Rest of the in-progress loop must still show the OLD training content.
        engine.RenderFrame(3, c[..3], w[..3], s[..3]);
        Check.Equal(Marker(3, 10f), c[..3], "Caregiver must finish the loop on the old training buffer");
        Check.Equal(Marker(3, 20f), w[..3], "Waver must finish the loop on the old training buffer");
        Check.Equal(Marker(3, 30f), s[..3], "Subject must finish the loop on the old training buffer");

        // Next loop: all three land on the NEW content simultaneously, in one call.
        engine.RenderFrame(5, c, w, s);
        Check.Equal(Marker(5, 100f), c, "Caregiver should reflect the new training set");
        Check.Equal(Marker(5, 200f), w, "Waver should reflect the new training set");
        Check.Equal(Marker(5, 300f), s, "Subject should reflect the new training set");
    }

    private static void MismatchedLengthThrows()
    {
        var engine = new StreamerEngine();
        engine.Reset(10);
        Check.Throws<ArgumentException>(
            () => engine.SetSignal(Participant.Caregiver, OperatingMode.Test, new float[5]),
            "A 5-sample buffer should be rejected when the phase's loop length is 10 samples");
    }

    private static void MultipleWrapsWithinOneFrame()
    {
        const int loopLen = 3;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(2);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: true, Marker(loopLen, 4f));

        engine.Trigger(containsProbe: true);

        Span<float> c = stackalloc float[10], w = stackalloc float[10], s = stackalloc float[10];
        engine.RenderFrame(10, c, w, s); // spans three wraps of the 3-sample loop in one call

        // One full background loop (trigger hasn't hit a boundary yet), then two full signal loops
        // (SetNumReps(2)), then back to background — all within this single 10-sample call.
        float[] expected = { 3f, 3f, 3f, 4f, 4f, 4f, 4f, 4f, 4f, 3f };
        Check.Equal(expected, s, "Subject samples should show exactly one background loop, two signal loops, then background again");
        Check.Equal(Marker(10, 1f), c, "Caregiver should be unaffected by the Subject trial");
        Check.Equal(Marker(10, 2f), w, "Waver should be unaffected by the Subject trial");
    }

    private static void RetriggerDuringActiveTrialIsDropped()
    {
        const int loopLen = 2;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetNumReps(3);
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: true, Marker(loopLen, 4f));
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));

        Span<float> c = stackalloc float[2], w = stackalloc float[2], s = stackalloc float[2];

        engine.Trigger(containsProbe: true);
        engine.RenderFrame(2, c, w, s); // crosses the boundary that starts the trial: remainingReps = 3

        // Retriggering mid-trial should be a no-op — it must not extend or restart the countdown.
        engine.Trigger(containsProbe: true);
        engine.Trigger(containsProbe: false);

        engine.RenderFrame(2, c, w, s); // rep 1 of 3 consumed: remainingReps = 2
        Check.True(engine.TrialActiveWindowOpen, "Still mid-trial after 1 of 3 reps");
        engine.RenderFrame(2, c, w, s); // rep 2 of 3 consumed: remainingReps = 1
        Check.True(engine.TrialActiveWindowOpen, "Still mid-trial after 2 of 3 reps");
        engine.RenderFrame(2, c, w, s); // rep 3 of 3 consumed: remainingReps = 0 — should end here
        Check.True(!engine.TrialActiveWindowOpen, "Trial should end after exactly the original 3 reps, unaffected by the dropped retriggers");

        engine.RenderFrame(2, c, w, s);
        Check.Equal(Marker(2, 3f), s, "Subject should be back on Background with no further trial pending");
        Check.True(!engine.TrialActiveWindowOpen, "No queued retrigger should have started a new trial");
    }

    private static void InactiveBufferWriteIsSilent()
    {
        const int loopLen = 4;
        var engine = new StreamerEngine();
        engine.Reset(loopLen);
        engine.SetSignal(Participant.Caregiver, OperatingMode.Test, Marker(loopLen, 1f));
        engine.SetSignal(Participant.Waver, OperatingMode.Test, Marker(loopLen, 2f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, Marker(loopLen, 3f));
        engine.SetSubjectSignal(OperatingMode.Test, isSignal: true, Marker(loopLen, 4f));

        Span<float> c = stackalloc float[4], w = stackalloc float[4], s = stackalloc float[4];
        engine.RenderFrame(4, c, w, s);
        Check.Equal(Marker(4, 1f), c, "Sanity check before the inactive write");

        // Training isn't selected — writing to it should have zero effect on current playback,
        // immediately or ever, until something actually switches into Training.
        engine.SetTrainer(Participant.Caregiver, Marker(loopLen, 999f));

        engine.RenderFrame(4, c, w, s);
        Check.Equal(Marker(4, 1f), c, "Writing an unselected buffer must not affect current (Test-mode) playback");
    }
}
