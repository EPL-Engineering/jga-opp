# OppStreamer — Stage 5

Stage 5 of the .NET/NAudio rewrite of the OPP audio streamer. See `OPP_Streamer_Design.md`
(delivered separately) for the full architecture and rationale.

## What's here

- **`src/OppStreamer.Core`** — the hardware-independent playback logic: `StimulusStore`,
  `PendingChangeQueue`, `TrialStateMachine`, `StreamerEngine` (the composition root),
  `DriftCompensatedRingBuffer` (the mic bridge's clock-drift correction, see below), `TtsPlayer`
  (channel 5, see "Text-to-speech player" below), and now `WaveformMonitor` (DiagnosticsView's data
  source, see "DiagnosticsView" below). No external dependencies at all. `StreamerEngine`'s
  boundary-latch mechanism remains the central bet of the whole redesign — that Trigger, TrainTest,
  the live training-stimulus swap, and now the click-free `Stop()` (see below) can all be handled
  by one generalized mechanism instead of hand-wired per feature — and it's fully built and tested.
- **`test/OppStreamer.Core.Tests`** — 38 tests, all passing: the original 8 exercising the
  boundary-latch mechanism, 7 for `DriftCompensatedRingBuffer`, 9 for `TtsPlayer` plus 1 confirming
  `StreamerEngine.SendTts`/`RenderTts` are wired through, 5 for the click-free `Stop()` mechanism
  (`StopBoundaryTests.cs` — silence never cuts a loop mid-pass, lands on all three participants
  together exactly at the boundary, the wait signal fires once per request), and 8 for
  `WaveformMonitor` (`WaveformMonitorTests.cs` — bucket commit timing, multi-bucket-in-one-call,
  history ramping from 0 up to its cap then wrapping oldest-first, per-channel independence,
  argument validation).
- **`src/OppStreamer.Hardware`** — `StreamerSampleProvider` (feeds `StreamerEngine`'s and the mic
  bridges' output into NAudio as plain float, and now also feeds `WaveformMonitor` — see
  "DiagnosticsView" below), `MicBridge` (wraps NAudio `WasapiCapture`, one per mic — see "Mic
  bridge (channels 6/7)" below), and two interchangeable output transports behind a common
  `IStreamerAudioOutput` interface: `AsioStreamerOutput` (the MOTU, in production) and
  `WasapiStreamerOutput` (any ordinary Windows audio device — see "Development without the MOTU"
  below). Real source, not a stub — but see the note below on what "verified" means for this
  project. Confirmed by you: the Stage 1 slice of this project builds and runs cleanly on a real
  machine, with genuinely verified 8-channel routing.
- **`src/OppStreamer.ConfigApi`** — `ConfigApi`, the one class MATLAB actually calls via
  `NET.addAssembly` (see "ConfigApi (MATLAB-facing surface)" below). Thin by design —
  validates/parses/converts, then delegates straight into `StreamerEngine`, an
  `IStreamerAudioOutput`, or (new this stage) `OppStreamer.Diagnostics.DiagnosticsHost`.
- **`src/OppStreamer.Diagnostics`** — new this stage: `DiagnosticsView` (design doc §5.7 — a
  WinForms window with a running/stopped indicator and a stacked real-time plot, one row per
  channel, via ScottPlot) and `DiagnosticsHost` (owns its hosting thread/lifecycle — see
  "DiagnosticsView" below for why it's a separate class). References only `OppStreamer.Core`, not
  `OppStreamer.Hardware` — it's generic over whatever channels the `WaveformMonitor` it's given
  reports, no hardcoded channel list.

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
- **`ConfigApi` (new this stage) can't build in-sandbox either** — same NU1100 restore failure as
  `OppStreamer.Hardware` (it references that project, which references NAudio). But everything in
  it that ISN'T literal device I/O — lifecycle guards, device/argument validation, the Start/Stop
  state machine, participant/mode string parsing, `double[]`→`float[]` conversion, and (this is the
  part most worth trusting) that every method genuinely routes to the *correct* underlying
  `StreamerEngine` call, not just "doesn't throw" — **was verified for real**, in-sandbox: the
  actual, unmodified `ConfigApi.cs` file was compiled into a throwaway project against hand-written
  fake `IStreamerAudioOutput`/`StreamerAudioOutputFactory` stand-ins (same names/signatures as the
  real Hardware types, swapped in instead of referencing the real Hardware project), and run through
  27 scenarios — including reaching into the private `StreamerEngine` field via reflection to
  confirm things like "does `SetTrainer("Subject", ..., isSubjectProbe: true)` actually land on the
  Signal buffer, not Background" rather than trusting the one-line delegation by eye. Now 35
  scenarios as of this stage (see below). What's NOT covered by this: real MATLAB→`NET.addAssembly`
  marshaling of these exact types (double arrays, nullable strings, etc.) and, obviously, real
  device I/O — both need your machine.
- **`OppStreamer.Diagnostics` (new this stage) splits cleanly into a verified half and an
  unverified half.** `WaveformMonitor` (Core — the actual decimation/history logic DiagnosticsView
  plots) has zero UI dependency at all, so it's genuinely, fully unit tested (8 tests, see above) —
  not a placeholder. `DiagnosticsView`/`DiagnosticsHost` themselves (the WinForms window, the
  ScottPlot calls) could not be built OR verified in this sandbox at all — this needs the
  `Microsoft.WindowsDesktop.App` reference pack (Windows-only) AND the ScottPlot NuGet package
  (needs network), neither available here. What COULD be verified without either: `ConfigApi`'s
  Initialize()/Close() wiring to `DiagnosticsHost` — that it's constructed exactly once per
  `Initialize()`, disposed exactly once per `Close()`, a construction failure propagates and
  leaves `IsOpen()` false rather than leaving things half-open, and the `isStreaming`/`waveforms`
  delegates it's given genuinely stay live (not stale snapshots) as `Start()`/`Stop()` change state
  — verified the same way as everything else `ConfigApi` does (compiling the real, unmodified
  `ConfigApi.cs` against a fake `DiagnosticsHost`, alongside the existing fake Hardware stand-ins;
  8 scenarios, bringing the fake-hardware harness to 35 total, all passing). **Real-machine
  testing already found and fixed one thing** — the original STA-thread hosting approach never
  actually worked under MATLAB; see the 2026-08-18 entry in "DiagnosticsView" below for the fix,
  which turned out to be a simplification (no dedicated thread needed at all), not a workaround.
  **What's still genuinely unverified:** whether the ScottPlot v5 API calls used are correct for
  whatever version actually restores (see "DiagnosticsView" below).

## Building on your machine

```
dotnet restore
dotnet build
dotnet run --project test/OppStreamer.Core.Tests
```

The test project isn't xUnit — see the comment in its `.csproj` for why (same nuget.org
constraint). It's a ~30-line hand-rolled runner (`TestRunner.cs`) with plain `Check.*` assertions.
Swap it for real xUnit whenever convenient; the test bodies don't depend on the runner itself.

`OppStreamer.Hardware`, `OppStreamer.ConfigApi`, and (new this stage) `OppStreamer.Diagnostics` all
target `net8.0-windows` and will only actually build on Windows (or at least restore/compile
against Windows reference assemblies) — that's expected, not a bug. `OppStreamer.Diagnostics`
additionally needs the ScottPlot.WinForms package to restore from nuget.org.

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

## ConfigApi (MATLAB-facing surface)

`ConfigApi` (new project, `OppStreamer.ConfigApi`) is the one class OPP actually calls via
`NET.addAssembly` — everything built in Stages 1–3 (`StreamerEngine`, `IStreamerAudioOutput`,
`MicBridge`, `TtsPlayer`) sits behind it. It mirrors the method-name list design doc §5.8 specifies
as unchanged (`Initialize`, `Close`, `IsOpen`, `EnumerateMicrophones`, `EnumerateOutputDevices`,
`IsMicDeviceValid`, `IsOutputDeviceValid`, `SetConfig`, `SetNumReps`, `SetSignal`, `SetTrainer`,
`TrainTest`, `Start`, `Stop`, `Trigger`) plus the two new ones (`SetTrainingStimulusSet`,
`SendTTS`).

**Please read this before wiring it up to real MATLAB code.** §5.8 gives method *names* to mirror,
not full signatures — the original LabVIEW-compiled assembly wasn't available to inspect while
building this (I asked; you confirmed designing from the doc + established conventions was the
way to go, with you sanity-checking after). Every place I had to invent a parameter list rather
than carry one over from an already-built, already-tested Core method has an `ASSUMPTION:` comment
directly on it in `ConfigApi.cs`, plus these are the ones most likely to need adjusting:

- **`SetConfig(outputDeviceName, loopLengthSamples, testerMicDeviceName?, boothMicDeviceName?)`** —
  §5.8 lists just the name. This is what SetConfig configures in this build: which output device
  (and optional mic devices) to use, and the current phase's loop length (one masker interval, in
  samples — everything else already has its own setter). If the real call site passes something
  shaped differently (a config struct, or separate device-selection vs. phase-length calls), this
  is the method to reshape.
- **`SetSignal(participant, mode, signal, isSubjectProbe = false)` and
  `SetTrainer(participant, signal, isSubjectProbe = false)`** — Core's underlying methods only
  cover Caregiver/Waver directly; Subject has separate Background/Signal buffers
  (`StreamerEngine.SetSubjectSignal`). Since §5.8 lists just one `SetSignal` name covering *all*
  participants including Subject, `isSubjectProbe` selects Signal (true) vs. Background (false) for
  Subject specifically, and it's an error to pass it non-default for Caregiver/Waver. If the real
  API instead has separate methods for Subject's two buffers (e.g. `SetBackground`/`SetProbe`),
  this is the assumption to correct.
- **`SetTrainingStimulusSet(caregiver, waver, subjectBackground, subjectSignal)`** — takes four
  positional `double[]` arrays, mirroring the already-built
  `StreamerEngine.SetTrainingStimulusSet`. Design doc §5.3 originally sketched this as
  `Dictionary<string, double[]>`; that was superseded once Core was actually built (four positional
  buffers give compile-time confidence all four always arrive together), and I've updated §5.3's
  text to match.
- Participant (`"Caregiver"`/`"Waver"`/`"Subject"`) and mode (`"Test"`/`"Training"`) are passed as
  strings, case-insensitive, with a clear exception listing valid values on a typo — this felt like
  the most MATLAB/LabVIEW-natural choice, but if the real assembly used something else (integer
  codes, actual `.NET` enum references via `NET.addAssembly`), swapping the parsing helpers
  (`ParseParticipant`/`ParseMode`) is a small, contained change.

**A real thread-safety gap this surfaced, not yet fixed:** design doc §6 says the device connection
stays open across `SetConfig` calls/phase transitions — implying `SetConfig` can be called again
while actively streaming. But `StreamerEngine.Reset()` (which a new loop length triggers) mutates
`StimulusStore`'s active buffers directly, with **no synchronization** against the audio thread's
concurrent `Advance()` calls — it was written for the "nothing is playing yet" case. Calling it
while the render callback is live is a genuine data race, not just a rough edge. `SetConfig` in
this build sidesteps it rather than papering over it: re-calling `SetConfig` with the *same* loop
length while streaming is fine (skips `Reset()` entirely, since nothing needs resetting), but a
*different* loop length while streaming throws, telling the caller to `Stop()` first. If OPP
actually needs to change the loop length mid-session without a `Stop()`/`Start()` blip, that needs
a real fix to `StimulusStore` (routing `Reset()` through the same boundary-gate as everything else,
or a lock) — flagging this now rather than guessing at a fix nobody's asked for yet.

Two small additions beyond the mirrored surface, both easy to ignore if not needed:
`IsStreaming` (true between `Start()`/`Stop()`) and `LastError` (the message of the most recent
`Start()` failure, for MATLAB-side logging).

**Bug found from your first real MATLAB test (2026-08-16): `SetConfig()` could crash the whole
MATLAB process instead of throwing a normal, catchable error.** Your crash log's key line —
`System.Runtime.InteropServices.SEHException (0x80004005): External component has thrown an
exception`, reaching `AppDomain`'s unhandled-exception path instead of MATLAB's normal .NET-error
marshaling — is the signature of a native fault, not an ordinary managed exception. That distinction
matters: an ordinary exception (like the `ArgumentException`s `ConfigApi` throws for bad input)
always surfaces in MATLAB as a normal, catchable error, exactly like the validation errors this
project already relies on elsewhere — but a genuine native-level fault bypasses that machinery
entirely, at a level no `try`/`catch` in `ConfigApi.cs`, or anywhere else in managed code, can
intercept. (For what it's worth: .NET Core/5+, which this targets, removed the ability to catch
these — even the old `[HandleProcessCorruptedStateExceptions]` escape hatch .NET Framework had
doesn't work anymore. If it's truly this class of fault, no code change in this repo can make it
catchable — only avoiding triggering it, or isolating it in a separate process, actually helps.)

The leading suspect: `ConfigApi.SetConfig()` → its private `ResolveBackend()` → previously checked
`AsioStreamerOutput.EnumerateDrivers()` (NAudio's `AsioOut.GetDriverNames()`) **first**, before ever
checking whether the name you passed was a WASAPI device — meaning every `SetConfig()` call,
regardless of which device it actually named, enumerated every ASIO driver registered on your
machine. ASIO driver enumeration querying third-party driver code is a well-known source of exactly
this kind of instability — a single misbehaving or orphaned driver registration (leftover from some
other audio software, a broken install, etc.) can crash enumeration for everything, not just itself.

Two fixes, shipped now:

1. **`ResolveBackend()` now checks WASAPI first, ASIO second.** A WASAPI device name (the likely
   case on your current dev machine, away from the MOTU) now never touches ASIO enumeration at all.
   This does NOT remove the risk for a name that genuinely needs ASIO — if your eventual MOTU
   machine also has a misbehaving ASIO driver registered, `SetConfig()` with the MOTU's name would
   still reach, and could still crash on, that same call. Verified in the sandbox harness (now 29
   scenarios) with a fake that "crashes" on ASIO enumeration: confirmed a WASAPI device name never
   reaches it, and confirmed a name that genuinely isn't found via WASAPI still does reach (and
   surface a failure from) the ASIO check — this is "skip when unnecessary," not "silently swallow
   ASIO problems."
2. **`AsioStreamerOutput.EnumerateDrivers()` now wraps `GetDriverNames()` in a try/catch**, returning
   an empty list on failure instead of letting an exception propagate uncontrolled. This only helps
   for *ordinary* (catchable) exceptions some ASIO drivers throw — real insurance, but not a fix for
   the corrupted-state-fault scenario above.

**What I'd suggest you do next, whichever is easiest:** the fastest real fix, if this is what
happened, is usually just checking Windows for a broken/orphaned ASIO driver registration (Control
Panel / registry `HKLM\SOFTWARE\ASIO`) and removing it — no code change needed, and it protects the
eventual MOTU machine too, which fix #1 above doesn't. To confirm the diagnosis independent of the
above fixes, try calling `obj.EnumerateOutputDevices()` directly (right after `Initialize()`,
before any `SetConfig()` call) — if that alone reproduces the crash, it's a strong confirmation this
is ASIO enumeration, not something else in `SetConfig()`. If you can share which device name you
passed to `SetConfig()`, that'll also help confirm (a WASAPI-sounding name, e.g. "Speakers
(Realtek...)", would make this diagnosis very likely correct; an ASIO/MOTU name would still be
consistent but less conclusive on its own, since the crash happened before your requested device
was ever actually checked). If the driver-cleanup route doesn't pan out, isolating ASIO enumeration
(and device open) into a short-lived child process — the standard mitigation professional DAWs use
for exactly this class of driver instability — is the real, robust fix; happy to build that next if
needed.

**Click-free `Stop()` (2026-08-17):** `Stop()` no longer cuts Caregiver/Waver/Subject off
mid-waveform. It now requests silence at the *next* loop boundary — reusing the exact same
boundary latch every other stimulus change goes through (`StimulusStore.RequestSilence` →
`PendingChangeQueue`) — so the loop pass already in progress finishes normally, and playback only
goes quiet exactly at the wrap, same as a mode switch or a hot-swapped training buffer. The
physical device (WASAPI/ASIO stream, mic captures) is only torn down once that boundary has
actually been reached; by then the channel is already silent, so there's nothing left to click on.

`Stop()` still returns immediately — it's a deliberate exception to this class's general
non-blocking rule, not a violation of it: the *call* doesn't block, but its effect (the actual
device teardown) now finishes shortly after, on a background thread, rather than synchronously
before the call returns. `IsStreaming` correctly stays `true` for that brief window — audio really
is still playing (now silence). If the boundary never arrives (a stalled device, not just a long
masker interval), a timeout — one full loop length at 48kHz, doubled, floored at 0.5s — falls back
to the old immediate hard stop, so `Stop()` can never hang forever.

**Known limitation, not yet guarded against:** calling `Start()` again before that background
teardown finishes is unsafe. `Start()` sees `IsStreaming` still `true` (correctly, per above) and
no-ops, so the in-flight graceful stop goes on to silence and tear down the device out from under
that "restart." If OPP ever needs `Stop()` immediately followed by `Start()` (e.g. switching output
devices quickly), poll `IsStreaming` down to `false` first. `Close()` is unaffected — it
deliberately stayed the old immediate/hard stop (a "tear everything down now" cleanup path should
actually finish before returning, not hand off to a background thread), so it's still safe to call
right after `Stop()` without waiting.

Verified in the Core test suite (`StopBoundaryTests.cs`: silence doesn't cut a loop mid-pass,
lands on all three participants together at the boundary, the wait signal fires exactly once per
request) and in the ConfigApi fake-hardware harness (the transport isn't torn down until a real
boundary is driven through the shared `StreamerEngine`, and does so promptly rather than waiting
out the timeout fallback).

## DiagnosticsView

Design doc §5.7, build plan item 5: a window, owned by the streamer process, with a running/
stopped indicator and a stacked real-time plot of the six content channels (Caregiver, Waver,
Subject, TTS, Tester Mic, Booth Mic — channels 0-1 are reserved/silent, not worth plotting).

**Architecture, in three pieces:**

- **`WaveformMonitor` (Core)** — the actual data source. Per channel, incrementally decimates the
  incoming sample stream into a scrolling history of (min, max) buckets: 10ms buckets (480 samples
  at 48kHz) × 500 buckets = 5 seconds of visible history, both constants set where
  `WasapiStreamerOutput`/`AsioStreamerOutput` construct it (tied to their own `SampleRate`
  constant, not a disconnected magic number). `StreamerSampleProvider.Read()` feeds it every
  callback for all six channels — cheap (running min/max, no allocation on that hot path). One
  deliberate simplification vs. §5.7's literal "double-buffered handoff" wording: this uses a
  short lock instead, held only once per finished bucket (~every 10ms) and once per UI redraw tick
  (~every 60-100ms) — never on the per-sample path — the same shape `PendingChangeQueue` already
  uses elsewhere in Core. See the class's own doc comment for the full reasoning; a real lock-free
  double buffer would also work but isn't buying anything at these rates. Fully unit tested (8
  tests, all passing) — this piece needed no hardware or UI to verify for real.
- **`DiagnosticsView` (Diagnostics, internal)** — the actual WinForms `Form`: a colored dot +
  label for the running/stopped indicator, a `TableLayoutPanel` of stacked `ScottPlot.WinForms.
  FormsPlot` controls (one per channel, built lazily once a `WaveformMonitor`'s channel names are
  known — so this project references only `OppStreamer.Core`, not `OppStreamer.Hardware`, no
  hardcoded six-channel list), redrawn on a ~15Hz `System.Windows.Forms.Timer`. The
  ScottPlot-touching part of every redraw is wrapped in a try/catch that permanently disables
  plotting (with a visible error label) after its first failure, while the indicator keeps
  updating regardless — see the second 2026-08-18 entry below for why that matters.
- **`DiagnosticsHost` (Diagnostics, public)** — deliberately the ONLY type this project exposes
  that `ConfigApi.cs` touches. `ConfigApi.cs` itself never references `System.Windows.Forms` or
  ScottPlot directly — it just does `new DiagnosticsHost(() => IsStreaming, () =>
  _output?.Waveforms)` in `Initialize()` and `.Dispose()` in `Close()`. Two reasons for this split:
  it keeps `ConfigApi.cs` compilable/testable against plain fakes the same way it already is for
  `OppStreamer.Hardware` (see below), and it isolates the hosting approach into one well-documented
  class that can change internally without touching `ConfigApi.cs` at all — which is exactly what
  happened, see the 2026-08-18 entry right below.

**2026-08-18: window didn't appear under MATLAB (DevShell was fine) — root cause found, via your
own prior art.** The original `DiagnosticsHost` ran `DiagnosticsView` on its own dedicated STA
thread with its own `Application.Run` message loop, on the theory that MATLAB's calling thread
might not pump Windows messages for a window it didn't create itself — the textbook-cautious
choice, but wrong for this environment. Your `OPP.Mixer.Mixer.Open()` was the proof: it does
exactly `_mixerPanel = new MixerPanel(); _mixerPanel.Show();` — no thread, no `Application.Run` —
and that panel is fully live and interactive from MATLAB today. That's decisive: MATLAB's own
calling thread already pumps a message loop adequate for a WinForms window shown directly on it.
The dedicated-thread version, meanwhile, never worked under MATLAB at all — not even a bare
`MessageBox.Show()` called from that separate thread ever appeared, while the identical call
succeeds trivially on MATLAB's own thread. So the extra thread wasn't just unnecessary, it was
actively the failure: something about a *new* thread this code spun up itself never got serviced
under MATLAB's hosting, even though creating the thread itself never errored.

**Fixed:** `DiagnosticsHost` now does exactly what `MixerPanel` does — constructs `DiagnosticsView`
and calls `.Show()` directly on whichever thread calls it, no separate thread, no
`Application.Run`, no `Invoke` marshaling needed (everything happens on the one thread). `Dispose()`
is just `_view.Close()`, same thread. This is a substantial simplification, not just a fix — the
whole `ManualResetEventSlim`/thread-join/exception-relay machinery from the two earlier iterations
is gone, because it was solving a problem (MATLAB not pumping messages for a window it didn't
create) that, per `Mixer`, doesn't actually exist here. `ConfigApi.cs` itself needed zero changes
for this — exactly the point of keeping `DiagnosticsHost` as the one thing it touches.

**2026-08-18 (later same day): window + indicator confirmed working, but plotting throws —
`'ScottPlot.Fonts' threw an exception`, repeatedly.** With the threading fix above in place, the
window now shows and the streaming indicator lights up correctly under MATLAB. But the first redraw
tick that touches ScottPlot throws a `TypeInitializationException` from `ScottPlot.Fonts`'s static
constructor — and it kept recurring on every tick "until MATLAB is closed." That repeat is expected
.NET behavior, not a new bug: once a type's static constructor throws, the CLR caches the failure
and rethrows the *same* exception on every later access to that type, for the lifetime of the
process — so at a 15Hz redraw timer, `DiagnosticsView` was re-throwing roughly 15 times a second
until the MATLAB process itself was closed.

**Fixed — "fail once and give up," exactly as asked:** `Redraw()` now wraps the entire
ScottPlot-touching block (`EnsurePlotsBuiltFor`, including its first `FormsPlot` construction where
a `Fonts` static-init failure actually surfaces, plus the per-channel `Scatter`/axis/`Refresh`
calls) in one `try`/`catch`. The first exception trips a `_plottingDisabled` flag and calls the new
`DisablePlotting(ex)`, which permanently skips all ScottPlot code from then on, replaces the plots
panel with a visible error label (the flattened exception chain — outer `TypeInitializationException`
plus every `InnerException` down the chain, since the outer message alone doesn't say why), and
leaves it there for the rest of the session. `SetStreamingIndicator` has no ScottPlot dependency and
is called before the try block on every tick regardless, so the running/stopped dot keeps working
even with plotting disabled.

**Still open — the actual root cause of the `ScottPlot.Fonts` failure itself.** The one-shot guard
above stops the symptom (infinite re-throw) but doesn't fix why the static initializer fails in the
first place; once the real message appears in the window's error label (or via the debugger), it'll
be the `InnerException` text that matters, not the outer `TypeInitializationException` wrapper. The
leading hypothesis: ScottPlot 5.x renders and measures text via SkiaSharp, which ships its actual
work in native (non-.NET) libraries — `runtimes/win-x64/native/libSkiaSharp.dll` and possibly
`libHarfBuzzSharp.dll` — that a normal `dotnet build`/`publish` output folder gets copied into
automatically, but a folder MATLAB loads `OppStreamer.Diagnostics.dll` from via `NET.addAssembly`
might not, if that folder isn't a real publish output. Worth checking: does that `runtimes\win-x64\
native\` subfolder (with those two DLLs in it) actually exist next to `OppStreamer.Diagnostics.dll`
in whatever folder MATLAB is pointed at? If not, copying it there (or pointing MATLAB at a proper
`dotnet publish` output instead of a bare build folder) is the likely fix.

**Still unverified, lower-risk:** the exact ScottPlot API calls (`Plot.Add.Scatter`,
`Plot.Axes.SetLimitsX/Y`) target ScottPlot 5.x, matching the `Version="5.*"` package reference in
`OppStreamer.Diagnostics.csproj`. If your restore resolves ScottPlot 4.x instead (e.g. if 5.x isn't
actually released/stable by the time you build this — worth double-checking on nuget.org), these
calls need adjusting to the older `AddScatter`-style API — a contained, mechanical change confined
to `DiagnosticsView.cs`.

**Verified, via the same fake-hardware harness approach used for `ConfigApi` all along:** the real,
unmodified `ConfigApi.cs` compiles and behaves correctly against a fake `DiagnosticsHost` (plus the
existing fake Hardware stand-ins) — `Initialize()` constructs it exactly once, `Close()` disposes
it exactly once, a construction failure propagates cleanly rather than leaving `IsOpen()` in a
half-open state, and the `isStreaming`/`waveforms` delegates `ConfigApi` hands it stay live (not
stale) as `Start()`/`Stop()` run. 8 scenarios, all passing, unaffected by the 2026-08-18 rewrite
since `DiagnosticsHost`'s public surface (constructor signature, `IDisposable`) didn't change.

## Known gaps / next stages

Per the design doc's staged build plan:

- Validation against the real LabVIEW streamer, and packaging/rollout decisions (design doc §7,
  items 6–7)
- DiagnosticsView's WinForms/ScottPlot pieces need real-machine verification — see "DiagnosticsView"
  above for exactly what to check.

## A design decision worth double-checking

`TrialStateMachine.Trigger()` is a no-op if called while a trial is already active (dropped, not
queued to fire immediately after). The original spec doesn't say what should happen here — this
was a judgment call made explicit in code and covered by a test
(`RetriggerDuringActiveTrialIsDropped`). Flag it if you'd rather a mid-trial Trigger() queue up
instead of being discarded.
