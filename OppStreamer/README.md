# OppStreamer — Stage 3

Stage 3 of the .NET/NAudio rewrite of the OPP audio streamer. See `OPP_Streamer_Design.md`
(delivered separately) for the full architecture and rationale.

## What's here

- **`src/OppStreamer.Core`** — the hardware-independent playback logic: `StimulusStore`,
  `PendingChangeQueue`, `TrialStateMachine`, `StreamerEngine` (the composition root),
  `DriftCompensatedRingBuffer` (the mic bridge's clock-drift correction, see below), and now
  `TtsPlayer` (channel 5, see "Text-to-speech player" below). No external dependencies at all.
  `StreamerEngine`'s boundary-latch mechanism remains the central bet of the whole redesign — that
  Trigger, TrainTest, and the live training-stimulus swap can be handled by one generalized
  mechanism instead of hand-wired per feature — and it's fully built and tested.
- **`test/OppStreamer.Core.Tests`** — 25 tests: the original 8 exercising the boundary-latch
  mechanism (no audible glitch mid-loop, atomic multi-participant swaps, sample-accurate behavior
  across multiple loop wraps, and a couple of edge cases that came up while writing them), 7 for
  `DriftCompensatedRingBuffer` — constant-signal fidelity, fill-level convergence under matched
  and mismatched write/read rates, overflow/underrun bookkeeping, and a direct check that the
  read-rate correction never exceeds its configured bound — 9 for `TtsPlayer` — silence when
  empty, exact playback, cross-buffer continuity within a single `Read()`, append-not-interrupt
  semantics, and `IsPlaying`/`QueuedSampleCount` bookkeeping — plus 1 confirming
  `StreamerEngine.SendTts`/`RenderTts` genuinely reach a live `TtsPlayer` and stay independent of
  the loop-boundary latch. **All 25 pass.**
- **`src/OppStreamer.Hardware`** — `StreamerSampleProvider` (feeds `StreamerEngine`'s and the mic
  bridges' output into NAudio as plain float), `MicBridge` (wraps NAudio `WasapiCapture`, one per
  mic — see "Mic bridge (channels 6/7)" below), and two interchangeable output transports behind a
  common `IStreamerAudioOutput` interface: `AsioStreamerOutput` (the MOTU, in production) and
  `WasapiStreamerOutput` (any ordinary Windows audio device — see "Development without the MOTU"
  below). Real source, not a stub — but see the note below on what "verified" means for this
  project. Confirmed by you: the Stage 1 slice of this project builds and runs cleanly on a real
  machine, with genuinely verified 8-channel routing.

## What's verified, and what isn't

This solution was drafted in a cloud sandbox with **no network access to nuget.org** (only a
narrow allowlist of package registries — npm, PyPI, a couple others — is reachable there; that's
a property of the sandbox, not something you'll hit normally). Practical effect:

- `OppStreamer.Core` and `OppStreamer.Core.Tests` have zero external package dependencies, so they
  restore, build, and run right there in the sandbox — genuinely verified, not just written.
- `OppStreamer.Hardware` depends on the NAudio package and could not be restored or compiled in
  that sandbox at all (confirmed: it fails cleanly at the restore step, `NU1100`, not some deeper
  problem) — written against NAudio 2.2.1's real API (verified against the actual NAudio source,
  not from memory), but until it was tried on a real machine, unbuilt and unrun. **Update:**
  confirmed building and running on a real Windows machine, and confirmed producing audible output
  through `WasapiStreamerOutput` against a real device (see "Development without the MOTU" below
  for a real bug that first test surfaced and how it's fixed). Still not tested against the actual
  MOTU/ASIO path — that's the next real-hardware milestone.
- **`MicBridge` has not yet been run against a real capture device at all** — same situation
  `WasapiStreamerOutput` was in before its first real test: written carefully against verified
  NAudio source (`WasapiCapture`, `BufferedWaveProvider`, `WdlResamplingSampleProvider`, all
  checked against the actual v2.2.1 source, not from memory), but this is real, untested code
  until it's tried against an actual mic. Worth treating its first run the same way — expect to
  possibly find a real bug or two, the same way the WASAPI output path did. **Update:** your report
  of an audible artifact ("discontinuities... buffering is off") turned out to be a real bug, found
  and fixed by simulating `MicBridge`'s exact pipeline in-sandbox against the real (vendored)
  NAudio v2.2.1 source with a synthetic capture thread standing in for `WasapiCapture` — see
  "Mic bridge (channels 6/7)" below for what the bug actually was.
- **`TtsPlayer` (new this stage) is pure Core logic with no hardware dependency at all** — unlike
  `MicBridge`, there's nothing here that needs a real device to exercise; the 9 tests above are
  genuine, complete coverage, not a placeholder pending real-hardware confirmation. It has not yet
  been exercised end-to-end through `StreamerSampleProvider` on real hardware (that still needs
  Windows + NAudio), but the wiring itself is simple enough (a `Span<float>` fill, same shape as
  the mic channels) that this is a low-risk gap.

## Building on your machine

```
dotnet restore
dotnet build
dotnet run --project test/OppStreamer.Core.Tests
```

The test project isn't xUnit — see the comment in its `.csproj` for why (same nuget.org
constraint). It's a ~30-line hand-rolled runner (`TestRunner.cs`) with plain `Check.*` assertions.
Swap it for real xUnit whenever convenient; the test bodies don't depend on the runner itself.

`OppStreamer.Hardware` targets `net8.0-windows` and will only actually build on Windows (or at
least restore/compile against Windows reference assemblies) — that's expected, not a bug.

## Development without the MOTU

`StreamerAudioOutputFactory.Create(AudioBackend.Wasapi, engine)` gets you a working
`IStreamerAudioOutput` against any ordinary Windows audio device — e.g. your StarTech USB 7.1
card, which happens to expose 8 discrete channels, matching the streamer's channel count. This is
a genuine, load-bearing part of the design now, not a hack bolted on for convenience: it only
works because `StreamerSampleProvider` was already written to know nothing about ASIO or WASAPI —
it just produces float samples for `StreamerEngine`, and each transport decides how to get those
onto real hardware.

`StreamerAudioOutputFactory.EnumerateDevices(AudioBackend.Wasapi)` lists active Windows render
devices by friendly name; `WasapiStreamerOutput.Start(deviceName)` tries exclusive mode (float,
then 16-bit PCM — consumer devices are picky about exact exclusive-mode formats) before falling
back to shared mode. If it can't get 8 channels working, the exception message points at Windows
Sound Control Panel → your device → Configure → 7.1 Surround as the likely fix.

**Gotcha found during your first real test:** audio "playing" doesn't by itself mean 8-channel
routing is actually working. NAudio's exclusive-mode `Init()` doesn't reliably throw when a device
rejects your exact format — it can silently substitute whatever format the driver reports as
"closest" and wrap your audio in an internal resampler to match it, with no exception. For a
consumer card, the closest match to 8-channel float is quite plausibly stereo, which would
silently collapse all your per-channel routing before it ever reaches the device — the exact
symptom of "it plays, but channel assignment doesn't seem to be happening." `WasapiStreamerOutput`
now explicitly checks the negotiated channel count after every attempt (and, for shared mode,
checks the device's actual current mix format directly, since NAudio doesn't reliably surface a
shared-mode downmix either) and only accepts an attempt that's genuinely 8-channel — falling
through to the next option, or failing loudly with a diagnostic message, rather than accepting a
silent substitution.

After `Start()` succeeds, check `ActiveShareMode` and `ActiveFormat` (both public properties) in
your test harness to confirm what's actually running — `ActiveFormat.Channels` should read 8. To
verify the *right* content is on the *right* channel (not just that 8 channels exist), the
straightforward approach is one channel at a time: set a distinct tone burst on just one of
Caregiver/Waver/Subject, leave the others silent, and confirm it's audible only on the
corresponding jack of the 7.1 breakout — repeat per channel.

What this validates: the OPP↔streamer API surface, the full latch/continuity state machine
end-to-end (not just the in-process unit tests), device open/close lifecycle, and now genuinely
verified 8-channel routing. What it doesn't validate: actual ASIO driver behavior or real hardware
timing — that still needs the MOTU, whenever you're at the shop.

## Mic bridge (channels 6/7)

`MicBridge` wraps a single WASAPI capture device (`WasapiCapture`), converts whatever it hands
back to mono float at 48kHz (downmixing if the device isn't already mono, resampling if it isn't
already 48kHz), and feeds a `DriftCompensatedRingBuffer` (Core — no hardware dependency, fully
unit tested). `AsioStreamerOutput` and `WasapiStreamerOutput` each own two `MicBridge` instances
(Tester → channel 6, Booth → channel 7) and open/close them together with the main output stream,
per the design doc's "open once, keep open" lifecycle (§6) — the same rule Stage 1 already applies
to the output device itself.

Both mics are independent USB devices on their own clocks — separate from each other and from
whichever output device is in use — so their capture callbacks and the render callback are never
in lockstep. `DriftCompensatedRingBuffer` absorbs that: the render side's `Read()` tracks how full
the buffer is and nudges its own read rate by a fraction of a percent (bounded, via linear
interpolation) to steer back toward a target fill level, rather than ever hard-inserting or
dropping a sample. See the class's own doc comment and
`test/OppStreamer.Core.Tests/DriftCompensatedRingBufferTests.cs` for the details and what's tested
(constant-signal fidelity, convergence under a sustained producer/consumer rate mismatch,
overflow/underrun bookkeeping).

**Bug found and fixed after your first real-mic test:** the audible "discontinuities" you
reported traced to `WdlResamplingSampleProvider` (NAudio's resampler, used whenever the capture
device's native rate isn't already 48kHz) — its own doc comment confirms that feeding it a read
request for fewer samples than it's prepared to produce triggers an internal "flush" meant for
end-of-stream, not a safe repeatable "return less, keep going" behavior; done mid-stream, it
corrupts several samples of output before self-recovering. The original code could trigger this on
essentially every capture callback. The fix: `MicBridge.OnDataAvailable` now reads
`BufferedWaveProvider.BufferedBytes` directly (ground truth, not an estimate) and subtracts an
explicit `SafetyMarginNativeFrames` cushion before deciding how many output-rate frames to pull, so
a short read is never presented to the resampler. Verified by simulating the exact real pipeline
(vendored NAudio v2.2.1 source, not reimplemented) against a continuous test tone over a simulated
300-second capture session with jittered callback timing — zero glitches, zero overflow, underrun
confined to startup only, at safety margins from 4 to 200 native frames.

`IStreamerAudioOutput.Start(deviceName, testerMicDeviceName, boothMicDeviceName)` — the two mic
parameters are optional and independent of each other and of the output device: omit either (or
both) to leave that channel silent, e.g. while testing the output path alone without mic hardware
at hand. `StreamerAudioOutputFactory.EnumerateMicDevices()` (or `MicBridge.EnumerateDevices()`
directly) lists active WASAPI capture device names by friendly name, same pattern as
`EnumerateDevices(AudioBackend)` for output devices.

For diagnostics, each transport exposes its two bridges directly —
`asioOrWasapiOutput.TesterMic`/`.BoothMic` — with `UnderrunSampleCount`, `OverflowSampleCount`, and
`CurrentFillLevel` on each, the same idea as `ActiveShareMode`/`ActiveFormat` on
`WasapiStreamerOutput`. Underrun/overflow should both settle near zero within the first second or
two after Start() and stay there; steady climbing in either one during a session means the
`DriftCompensatedRingBuffer` constructor's default gain/capacity need revisiting for the actual
drift your hardware exhibits (unlikely, but this is the first real-hardware run for this
component — see the note above).

## Text-to-speech player (channel 5)

`TtsPlayer` (Core, zero dependencies) is deliberately the simplest component in the system (design
doc §5.6): presynthesized audio arrives at irregular times and doesn't need to be phase-locked to
anything else — it's just a FIFO of buffers. `StreamerEngine.SendTts(float[] signal)` appends a new
buffer after whatever's already queued or playing (it does **not** interrupt in-progress playback);
`StreamerEngine.RenderTts(Span<float> destination)` drains continuously, crossing seamlessly from
one queued buffer into the next with no gap, and produces silence once the queue runs dry.

Unlike Caregiver/Waver/Subject, none of `StimulusStore`'s loop-boundary latch machinery applies
here — that's specifically why `TtsPlayer` is a separate class owned directly by `StreamerEngine`
rather than folded into `RenderFrame`'s shared-cursor rendering. `StreamerSampleProvider` pulls
from it unconditionally every callback, same pattern as the two mic channels: nothing queued just
means silence on channel 5, no null-checking needed.

`IsPlaying` and `QueuedSampleCount` are exposed for diagnostics — `QueuedSampleCount` is the total
samples of unplayed TTS audio outstanding (current buffer's remainder plus everything queued
behind it).

## Known gaps / next stages

Per the design doc's staged build plan:

- The MATLAB-facing `ConfigApi` surface (string/double[] calls translating into the enum-based
  calls `StreamerEngine` exposes today) — note `SendTts` currently takes `float[]`, matching every
  other Core API; converting from MATLAB's `double[]` is explicitly ConfigApi's job, not
  `TtsPlayer`'s.
- Diagnostics window with the real-time stacked plot

## A design decision worth double-checking

`TrialStateMachine.Trigger()` is a no-op if called while a trial is already active (dropped, not
queued to fire immediately after). The original spec doesn't say what should happen here — this
was a judgment call made explicit in code and covered by a test
(`RetriggerDuringActiveTrialIsDropped`). Flag it if you'd rather a mid-trial Trigger() queue up
instead of being discarded.
