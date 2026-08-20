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
  channel, drawn with plain GDI+ — see the 2026-08-18 entry in "DiagnosticsView" below for why this
  isn't ScottPlot anymore) and `DiagnosticsHost` (owns its hosting thread/lifecycle — see
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
  plotting) could not be built OR verified in this sandbox at all — this needs the
  `Microsoft.WindowsDesktop.App` reference pack (Windows-only), not available here. What COULD be
  verified without it: `ConfigApi`'s Initialize()/Close() wiring to `DiagnosticsHost` — that it's
  constructed exactly once per `Initialize()`, disposed exactly once per `Close()`, a construction
  failure propagates and leaves `IsOpen()` false rather than leaving things half-open, and the
  `isStreaming`/`waveforms` delegates it's given genuinely stay live (not stale snapshots) as
  `Start()`/`Stop()` change state — verified the same way as everything else `ConfigApi` does
  (compiling the real, unmodified `ConfigApi.cs` against a fake `DiagnosticsHost`, alongside the
  existing fake Hardware stand-ins; 8 scenarios, bringing the fake-hardware harness to 35 total,
  all passing). **Real-machine testing found and fixed two real things**, both in "DiagnosticsView"
  below: the original STA-thread hosting approach never actually worked under MATLAB (fixed by a
  simplification, no dedicated thread needed at all), and ScottPlot turned out to be fundamentally
  incompatible with how MATLAB hosts this project's .NET code (fixed by dropping it for hand-rolled
  GDI+, which has no such incompatibility). Plotting now depends on nothing beyond the .NET BCL, so
  the remaining unverified surface is just "does it look right on your screen," not "does it load
  at all."

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
against Windows reference assemblies) — that's expected, not a bug. `OppStreamer.Diagnostics` has
no third-party package dependency (see the 2026-08-18 entry in "DiagnosticsView" below for why it
no longer uses ScottPlot), so there's nothing extra to restore for it beyond the reference pack.

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
`TrainTest`, `Start`, `Stop`, `Trigger`) plus the new additions beyond it (`SetTrainingStimulusSet`,
`SendTTS`, `WaitForLatch`, `SetDiagnosticsVisible`, `SetStimulusSet`, `IsTrialActive` — each
explained further down, where and why it was added).

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

**`EnumerateOutputDevices()` now returns ASIO and WASAPI devices combined (2026-08-19),** instead of
the previous "ASIO if any exist, else WASAPI" either/or behavior. You asked whether there was a
reason to keep them separate — there isn't: nothing downstream treats the two lists differently
(`SetConfig` resolves whichever name it's given, WASAPI-first, per the crash fix above), so
combining them just gives MATLAB one full picker instead of silently hiding WASAPI devices whenever
any ASIO driver happens to be registered. No new risk introduced — the ASIO-enumeration-crash
mitigations above (WASAPI-first resolution in `SetConfig`, the try/catch in `EnumerateDrivers()`)
are unaffected, since this only changes what `EnumerateOutputDevices()` itself returns, not how
`SetConfig()` resolves a name.

**`WaitForLatch` (2026-08-19): replaces the old stop/start-around-every-change pattern for live
stimulus changes.** You laid out OPP's three actual status-check scenarios from the LabVIEW code:

1. `SetAudioStream` stops the stream, waits a finite time for it to report stopped, checks for an
   error if it didn't.
2. Later in `SetAudioStream`, after the new waveforms are set, starts the stream, waits a finite
   time for it to report started, checks for an error if it didn't.
3. Closing OPP stops the stream, waits a finite time for it to report stopped, then closes
   regardless and moves on.

Your key realization: in this build, Caregiver/Waver/Subject/Training content can already change
live, without stopping anything — every `Set*`/`TrainTest`/`Trigger` call goes through the same
boundary latch `Stop()` itself uses (`PendingChangeQueue`/`ApplyBatch`), so a new stimulus is simply
picked up whole at the next loop wrap. Scenarios 1 and 2 above exist in the LabVIEW code to
guarantee a clean handoff around a stimulus change — but with boundary-latching, that guarantee
already holds without stopping the stream at all. What's actually still needed is just a way to
confirm "has what I just set actually taken effect yet" — which is exactly what `WaitForLatch` is
for, and nothing more: it does **not** cover changing the loop length / masker-interval timing,
which (per your clarification) can only change via a full OPP restart that kills and rebuilds the
stream — `SetConfig`'s existing "throws on a different loop length while streaming" guard (see
above) is correct and untouched.

```
public bool WaitForLatch(double timeoutSeconds)
```

Blocks the calling thread (never the audio thread) until at least one loop boundary has been
crossed since the most recent mutating call — `SetSignal`, `SetTrainer`, `SetTrainingStimulusSet`,
`TrainTest`, or `Trigger` — or the timeout elapses first. Because every one of those calls shares
the exact same boundary-latch machinery, `WaitForLatch` doesn't need to know *which* change it's
confirming — any boundary after the most recent mutating call means whatever was queued has now
applied. Returns `false` on timeout, meaning the same thing a `Stop()`/`Start()` timeout would have
meant in the old model: most likely audio genuinely isn't being rendered right now (never started,
or the device stalled), not just a long masker interval. Throws `ArgumentOutOfRangeException` for a
negative timeout, and the usual `RequireOpen()` guard (`InvalidOperationException`) if called before
`Initialize()`.

Mapping your three scenarios onto the new API:

- **Scenarios 1 & 2 (the stimulus-change halves of `SetAudioStream`)** collapse into one step each,
  with no `Stop()`/`Start()` at all: call whichever `Set*` method matches the change (`SetSignal`,
  `SetTrainer`, `SetTrainingStimulusSet`, or `TrainTest`/`Trigger` for mode/trial changes), then call
  `WaitForLatch(timeoutSeconds)`. A `true` means the new stimulus is now playing; a `false` means it
  hasn't landed within your timeout, and — same as before — you'd check `LastError` for anything
  useful, keeping in mind the caveat below.
- **Scenario 3 (closing OPP)** needs no new method at all: the existing `Stop()` + poll `IsStreaming`
  down to `false` (with your own finite timeout) + call `Close()` regardless already covers it
  exactly as before. `WaitForLatch` has nothing to add here since you're tearing the stream down,
  not confirming a change took effect.

**The same caveat `LastError`'s doc comment already carries applies here too:** a `WaitForLatch`
timeout doesn't distinguish "the device silently died mid-session" from "audio was simply never
started" — there's still no NAudio callback wired up to catch a mid-stream driver failure (see
"Known gaps" below). That said, a `WaitForLatch` timeout is a meaningfully stronger signal of real
trouble than `IsStreaming` alone ever was: `IsStreaming` only tells you whether `Start()` was called
and `Stop()`/`Close()` wasn't, not whether frames are actually being rendered, whereas a
`WaitForLatch` timeout means no loop boundary — i.e. no actual audio callback activity — happened
within your wait window.

Verified in the Core test suite (`WaitForLatchTests.cs`, 7 scenarios: false before any boundary,
true once one is crossed, no `RequestStop()`-style matching request needed, times out when nothing
is configured, stays true across later unrelated boundaries rather than being one-shot, a fresh
mutating call correctly resets it, and — added 2026-08-20, see below — `TrainTest` specifically)
and in the ConfigApi fake-hardware harness (delegates through to the same
`StreamerEngine.WaitForLatch`, rejects a negative timeout, and throws the standard `RequireOpen()`
guard before `Initialize()`).

**2026-08-20: confirmed `WaitForLatch` also replaces the old LabVIEW `IsTrainer` polling.** You
asked whether it's the right fit for OPP's Test/Training toggle — the old LabVIEW code polled an
`IsTrainer` flag to find out when a mode switch had actually taken effect at the loop boundary,
which is exactly what `WaitForLatch` reports for any mutating call, `TrainTest` included. Yes: call
`configApi.TrainTest(isTrainer)`, then `WaitForLatch(timeoutSeconds)` — `true` means the switch has
audibly happened (all three of Caregiver/Waver/Subject, together, since `RequestModeChange` already
batches them atomically the same way `SetStimulusSet` does). One nuance worth knowing: `TrainTest`
updates `StreamerEngine`'s notion of the current mode immediately, as bookkeeping, well before the
boundary — so if you ever need "what mode is currently requested" versus "what mode is currently
*audible*", those are two different moments, and `WaitForLatch` specifically tracks the latter (the
one `IsTrainer` was actually reporting). Added a dedicated test for this exact case
(`TrainTestBoundaryMapsOntoWaitForLatch`) rather than relying only on the general-purpose coverage
above — it confirms `WaitForLatch` reports `false` right after `TrainTest()`, stays `false` through
the rest of the in-progress loop (which correctly finishes on the OLD mode), and only goes `true`
at, and audibly reflects Training from, the boundary where the switch actually took effect.

**Also added, same day: `SetDiagnosticsVisible(bool)`.** Originally built as an attempted fix for
the Save Settings freeze (see the freeze summary below) — hide the diagnostics window before
`uiputfile`, show it again after. That specific fix didn't work (hiding alone doesn't prevent the
freeze; see the summary), but the method itself is real, tested, and still useful any time OPP wants
to hide/show the diagnostics window programmatically outside that context — it's a thin pass-through
to `DiagnosticsHost.SetVisible`, a safe no-op if called before `Initialize()`.

**`SetStimulusSet(mode, ...)` (2026-08-20): closes a real torn-update gap in the one-at-a-time
`SetSignal` pattern.** You asked whether OPP's existing code — which sets the currently-selected
phase's stimuli by calling `SetSignal` once each for Caregiver, Waver, Subject Background, and
Subject Signal — could conflict with the boundary-latch mechanism. Short answer: not by throwing or
corrupting anything, but yes, there's a real (if narrow, timing-dependent) hazard. Each `SetSignal`
call queues its one buffer independently into the same shared pending-change slot set
(`PendingChangeQueue`); the audio thread drains and applies whatever's queued so far the instant it
crosses a loop boundary, regardless of how many of your calls have completed. If that boundary
happens to land between two of your four calls, it applies only the ones queued so far — one loop
pass then plays a *mix* of new and old buffers before the rest catches up on the following boundary.
No crash, no permanent corruption, just one masker interval's worth of audio that isn't the coherent
set you intended.

This is exactly the problem `SetTrainingStimulusSet` already solved for Training — it just only
covered Training. `SetStimulusSet` generalizes that same atomic-batch mechanism to take an explicit
mode:

```
public void SetStimulusSet(string mode, double[] caregiver, double[] waver, double[] subjectBackground, double[] subjectSignal)
```

Queues Caregiver, Waver, and both of Subject's buffers as a single atomic group in
`StimulusStore` — guaranteed to land on exactly one loop boundary, never staggered. `SetSignal`
called four separate times for the same group is still there and still works (nothing about it
changed), it's just no longer the recommended way to update a whole phase's stimuli together; use
`SetStimulusSet` for that instead. `SetTrainingStimulusSet` itself is now a thin alias for
`SetStimulusSet("Training", ...)`, kept so existing Training call sites need no change.

Verified in the Core test suite (`StreamerEngineTests.cs`, two new scenarios: `SetStimulusSet`
applied to an explicit mode lands atomically, same shape as the existing Training test; and a
side-by-side comparison — one engine updated one-at-a-time with a boundary deliberately interleaved
between calls, which visibly tears for one loop pass, versus an identical change made via
`SetStimulusSet`, which never tears) and in the ConfigApi fake-hardware harness (routes through with
the same atomicity, and rejects an unrecognized mode string the same way `SetSignal` does).

**`IsTrialActive` (2026-08-20, added by Ken): exposes the trial-active-window flag to MATLAB.** You
asked whether the API surfaced a way to know when a trial's signal window is open — a `Trigger()`
has taken effect and is counting down reps — independent of whether that trial actually contains a
probe. It already existed one layer down (`StreamerEngine.TrialActiveWindowOpen`, driven by
`TrialStateMachine.OnBoundary`): true from the loop boundary a pending `Trigger()` takes effect,
through the full repeat count configured via `SetNumReps`, until that countdown reaches zero —
regardless of `containsProbe`, since a no-probe trial still opens the window and counts down reps
exactly like a probe trial, it just never switches Subject off the Background buffer while doing so.
It just hadn't been threaded through to `ConfigApi` yet. Added as a thin read-only pass-through,
named to match the existing `IsStreaming`/`IsOpen()` convention:

```csharp
public bool IsTrialActive => _engine.TrialActiveWindowOpen;
```

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
  label for the running/stopped indicator, a show/hide toggle button, and a `TableLayoutPanel` of
  stacked `WaveformPanel` controls (a small private `Panel` subclass that draws its channel's
  min/max envelope with plain GDI+ — one per channel, built lazily once a `WaveformMonitor`'s
  channel names are known — so this project references only `OppStreamer.Core`, not
  `OppStreamer.Hardware`, no hardcoded six-channel list, and no third-party plotting package either
  — see the second 2026-08-18 entry below for why that last part matters), redrawn on a ~15Hz
  `System.Windows.Forms.Timer`. The plotting part of every redraw is still wrapped in a try/catch
  that permanently disables plotting (with a visible error label) after any failure, while the
  indicator keeps updating regardless — cheap insurance, now unlikely to ever actually trip.
  **Collapsible by default**, matching the old LabVIEW streamer window — see the third 2026-08-18
  entry below.
- **`DiagnosticsHost` (Diagnostics, public)** — deliberately the ONLY type this project exposes
  that `ConfigApi.cs` touches. `ConfigApi.cs` itself never references `System.Windows.Forms`
  directly — it just does `new DiagnosticsHost(() => IsStreaming, () =>
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
until the MATLAB process itself was closed. First fix (still true, and kept): wrap the plotting
part of `Redraw()` in a try/catch that disables plotting permanently after its first failure and
shows the error in the window, while the indicator keeps working regardless — that part didn't
change. What changed is *why* it was failing, and the real fix for that:

**Root cause, from the window's own error label:** the bottom of the exception chain was a
`FileNotFoundException` for `System.Runtime.CompilerServices.Unsafe, Version=4.0.4.1...`. Trying a
`dotnet publish` output folder instead of a `dotnet build` one didn't change anything — which was
the tell that this was never a "which files are in the folder" problem. The actual cause: this
project's MATLAB integration deliberately hosts under the **.NET Framework** CLR, not .NET Core —
`dotnetenv("core")` was tried earlier and caused its own separate problems pinning a version, so
Framework hosting (MATLAB's Windows default) is the one that's actually being used, restarting
MATLAB clean before every attempt. ScottPlot 5.x depends on SkiaSharp, and that dependency chain
needs pieces of the modern (.NET 5+/Core) BCL that simply don't exist under .NET Framework, no
matter what's sitting in the output folder — not a missing/mismatched file, a different runtime
that can't run that code at all. (For what it's worth, one other MATLAB user hit close to this
exact error — `System.Runtime, Version=8.0.0.0` not found — for the identical reason.)

**Fixed for real this time: dropped ScottPlot entirely.** `OppStreamer.Diagnostics.csproj` no
longer references `ScottPlot.WinForms` at all — zero third-party packages in this project now.
`DiagnosticsView` draws each channel's min/max envelope itself with a small private `WaveformPanel`
(a `Panel` subclass overriding `OnPaint`, using plain `System.Drawing` — `Graphics.FillPolygon` for
the filled band, `DrawLines` for the outline). `System.Drawing`/`System.Windows.Forms` are part of
the .NET BCL under *both* .NET Framework and .NET Core, so this doesn't care which one MATLAB is
hosting — the whole class of problem above can't recur. The try/catch + `DisablePlotting` one-shot
guard from the first fix is still there around the plotting call, now just as cheap insurance
rather than a load-bearing fix for a real, recurring failure. **Confirmed working under real MATLAB**:
window, indicator, and plot all come up cleanly, no exception.

**2026-08-18 (later still): collapsible by default, matching the old LabVIEW streamer window.** The
old LabVIEW streamer had a top-right toggle button; the window started small (indicator + toggle
only) and only grew to reveal the plot when explicitly expanded — deliberately, since the Tester
shouldn't be able to see the waveforms in normal operation (the plot visibly shows when a probe is
present, which is exactly the thing the Tester isn't supposed to know ahead of time). It also only
did the plotting work while the plot was actually visible, to avoid paying that overhead the rest
of the time. `DiagnosticsView` now matches this: a `Button` next to the status indicator toggles
`SetPlotVisible`, which resizes the window between a small collapsed `ClientSize` (260×40 — just
the indicator row) and an expanded one (600×600, per your size request) and shows/hides the
channels panel to match. `Redraw()` gates all plotting work — `EnsurePlotsBuiltFor`, every channel's
`GetSnapshot`/`SetData` — behind `_plotVisible`, so none of it runs while collapsed; only
`SetStreamingIndicator` (essentially free) still runs every tick regardless. The window is also
`FormBorderStyle.FixedSingle` with `MaximizeBox = false` now, so the Tester can't drag/maximize
their way to a bigger window and expose the hidden plot area that way. Note `WaveformMonitor` itself
keeps recording in the background the whole time regardless of window state (it's fed straight from
the audio thread, with no idea whether the window is collapsed) — so nothing is lost while
collapsed; the plot just doesn't spend time drawing it, and shows the last ~5 seconds of real
history the moment it's expanded.

**Verified, via the same fake-hardware harness approach used for `ConfigApi` all along:** the real,
unmodified `ConfigApi.cs` compiles and behaves correctly against a fake `DiagnosticsHost` (plus the
existing fake Hardware stand-ins) — `Initialize()` constructs it exactly once, `Close()` disposes
it exactly once, a construction failure propagates cleanly rather than leaving `IsOpen()` in a
half-open state, and the `isStreaming`/`waveforms` delegates `ConfigApi` hands it stay live (not
stale) as `Start()`/`Stop()` run. 8 scenarios, all passing, unaffected by the 2026-08-18 rewrite
since `DiagnosticsHost`'s public surface (constructor signature, `IDisposable`) didn't change.

**2026-08-20: fixed a real autoscaling bug — small-amplitude traces (tone pips) were rendering only
a few pixels tall.** You flagged this after seeing it on real hardware. The vertical autoscale in
`WaveformPanel.OnPaint` was already recomputing each channel's min/max range from its own visible
data every redraw (so it was never a "no autoscaling at all" problem, and this was never a
per-channel thing — every channel shares the identical drawing code) — the actual bug was the
headroom calculation: `Math.Max(0.05f, (hiY - loY) * 0.1f)` padded every trace by *at least* an
absolute 0.05, regardless of how small the signal's real amplitude was. For a full-scale signal
(span ~1.0-2.0) that's negligible. For something like a calibrated tone pip with a real span of
~0.02, that fixed floor swamped the actual data — total plotted range came out to ~0.12, so the
trace filled under 20% of the panel height, worse the smaller the amplitude, which matches "only a
few pixels tall" exactly.

Fixed by making the padding purely proportional to the trace's own span (10% top and bottom, no
absolute floor) — the existing `if (hiY <= loY) hiY = loY + 1f` fallback right after it still
handles the one case proportional padding can't (a genuinely constant/silent signal, where 10% of a
zero span is still zero). Checked the arithmetic directly (this file can't build in-sandbox — no
Windows Desktop SDK here, same limitation as `ConfigApi`/Hardware — so this is a plain numeric
check of the same formula, not a build/run): across signal spans from 0.002 to 2.0, the old formula
filled anywhere from 2% to 83% of the panel depending on amplitude (worse for exactly the small,
legitimate signals you were seeing); the new formula fills a steady ~83% regardless of amplitude —
same generous headroom a full-scale signal already got, now given to every signal equally.

**2026-08-20 (later): found and fixed the real bug behind Subject/TTS going blank once the
stimulus/speech stops.** Not NaN or Infinity anywhere — traced the whole data path
(`WaveformMonitor.Accumulate`/`Commit`, `TtsPlayer.Read`'s silence padding, `StimulusStore`'s
buffers) and every value flowing through it is an ordinary finite float; nothing here divides by
anything on the way in. The actual bug was in the very fallback the last fix above relies on. When a
channel's visible window is perfectly flat — `loY == hiY` exactly, which is exactly what happens
once TTS's queue runs dry (`TtsPlayer` pads with literal `0f`, forever, until something new is
queued) or Subject settles onto a constant Background buffer between trials — the proportional
padding computes to zero (10% of a zero span is still zero), so it fell through to
`if (hiY <= loY) hiY = loY + 1f`. That builds a 1-unit window *starting at* the flat value rather
than *centered on* it, and fed into `MapY`, every point in a perfectly flat trace lands at pixel row
`height` — one row past the panel's last visible row, clipped by the paint region. Not blank data,
a real trace silently drawn just offscreen.

Fixed by centering that fallback window on the flat value instead of starting from it (`loY -= 0.5f;
hiY += 0.5f;`, rather than `hiY = loY + 1f`) — worked out on paper (again, this project can't build
in this sandbox): a flat trace at any constant value, `0` or otherwise, now maps to pixel row
`height / 2`, dead center, instead of `height`. A genuinely silent/constant channel should now show
as a visible flat line down the middle of its panel rather than disappearing. Please confirm this
one too on your end.

**2026-08-19: MATLAB hangs hard (force-quit required) on "Save Settings" while this window (or
Mixer's) is open.** The OPP MATLAB app's Save Settings menu pops MATLAB's native Windows save
dialog; with the Streamer or Mixer window open, that dialog freezes MATLAB before you can even
click anything in it. This isn't specific to our code — it's a documented class of MATLAB bug
(native file dialog + another top-level window MATLAB doesn't fully control = freeze), and per
Microsoft's own docs on hosting WinForms directly on an unmanaged host's message loop (exactly what
`DiagnosticsHost`/`MixerPanel` both do): "the message loop provided by the [host] application is
fundamentally different from the Windows Forms message loop." Microsoft's two official mitigations
don't work for us — `ShowDialog()` blocks MATLAB's own UI the whole time the window's open (defeats
the point), and a dedicated thread is the exact approach already proven dead under MATLAB's hosting
(see the 2026-08-18 entry above). Forcing MATLAB to use its Java Swing file chooser instead of the
native dialog (`com.mathworks.mwswing.MJFileChooserPerPlatform.setUseSwingDialog(1)`) eliminates it
today, but it's an undocumented internal MathWorks class already flagged for removal with "no
simple replacement" — not safe to depend on long-term, especially with a MATLAB 2023→2025 upgrade
planned for the deployment machine.

**Tried first: `Application.OleRequired()`.** `DiagnosticsHost`'s constructor calls it before
creating the `Form` — the documented API for making sure a thread hosting WinForms UI without
`Application.Run` is properly OLE/COM initialized. **Confirmed: did not fix it.** The freeze
persists with this in place, so whatever's actually deadlocking isn't (purely) a COM-apartment
initialization gap on this thread.

**Fallback, now built: `SetDiagnosticsVisible`.** `DiagnosticsHost.SetVisible(bool)` (new) hides or
re-shows the window via `Form.Visible` — no dispose/reconstruct, so expanded/collapsed state and
built plots survive a hide/show cycle. `ConfigApi.SetDiagnosticsVisible(bool)` (new) is the
MATLAB-facing wrapper: a no-op if `Initialize()` hasn't been called or after `Close()`. Intended
call pattern from the OPP MATLAB app's save-settings handler:

```matlab
configApi.SetDiagnosticsVisible(false);
[file, path] = uiputfile(...);
configApi.SetDiagnosticsVisible(true);
```

You'd need the equivalent on `Mixer`/`MixerPanel` too (a small `SetVisible` there, same shape) since
that's a separate class library this project doesn't own — you mentioned you'll handle that side
yourself.

**Caveat, confirmed to matter:** `SetDiagnosticsVisible(false)` (hide, don't dispose) did NOT fix
the freeze — proved by testing. `Close()` (which fully disposes the `Form`) DID fix it. See the
summary right below for what that combination actually tells us.

### Summary: the Save Settings freeze, for future reference

Consolidated here because the investigation above is long and blow-by-blow — this is the part worth
reading if this resurfaces later and nobody wants to re-derive it.

**Symptom:** MATLAB hard-freezes (force-quit required) when the OPP app's native Save Settings
dialog opens while the Streamer or Mixer window exists, even hidden.

**What's confirmed, in the order it was learned:**
1. Not fixed by `Application.OleRequired()` — rules out a simple missing COM/OLE apartment
   initialization on MATLAB's hosting thread as the (sole) cause.
2. Not fixed by hiding the window (`Form.Visible = false`) while leaving the `Form` object and its
   window handle alive on that thread — rules out on-screen *visibility* as the deciding factor.
3. **Is fixed by `Close()`** — fully disposing the `Form`, removing its window handle from that
   thread entirely.

**Current best understanding:** the trigger is the mere *existence* of a WinForms `Form`'s window
handle, registered on MATLAB's own calling thread, at the moment MATLAB opens a native modal common
dialog on that same thread — independent of whether that window is visible. This is consistent with
Microsoft's own documented caveat about hosting WinForms directly on an unmanaged host's message
loop (quoted above): the host's message loop is "fundamentally different" from what WinForms
expects, and apparently that mismatch surfaces specifically around a second top-level window's
handle existing when a native COM-based modal dialog also wants the thread's attention — not around
painting/visibility, which is why hiding didn't help but disposing did.

**Why this matters beyond Save Settings:** this isn't a Save-Settings-specific bug — it's a general
hazard of *any* native modal Windows dialog (a file dialog, `MessageBox`, print dialog, color
picker, anything using the classic common-dialog/COM machinery) opened from MATLAB while the
Streamer or Mixer window exists on that thread, visible or not. If some other feature ever needs to
pop a native dialog while either window might be open, expect the same freeze unless that window is
fully disposed first.

**Decision:** rather than work around this for Save Settings specifically, it's being eliminated in
favor of auto-save — researchers weren't using manual load/save correctly anyway, so this removes
both the freeze risk and a source of confusion in one move. `SetDiagnosticsVisible` stays in the
codebase (harmless, and a real, working API for "hide the window without losing its state" in any
context that doesn't involve a native dialog) but isn't the answer to this specific problem.

**If this resurfaces later** (a different native dialog, a future feature): the known-working fix is
full disposal, not hiding. `ConfigApi.Close()` already does this but also tears down active
streaming, which is usually too blunt for a quick "just get this dialog past this window" need. The
lighter version — disposing and later reconstructing just `DiagnosticsHost` without touching
`_output`/streaming state — was sketched but not built, since Close() answered the diagnostic
question without needing it. Worth building at that point, not before.

## Known gaps / next stages

Per the design doc's staged build plan:

- Validation against the real LabVIEW streamer, and packaging/rollout decisions (design doc §7,
  items 6–7)
- DiagnosticsView's window/indicator and GDI+ plotting are confirmed working under real MATLAB (see
  "DiagnosticsView" above). Still worth a look on your next run: the collapsed/expanded toggle
  sizes itself sensibly on your actual display/DPI settings, and the envelope itself looks sane
  (not flipped, scaled, or empty) once real audio is flowing through it.

## A design decision worth double-checking

`TrialStateMachine.Trigger()` is a no-op if called while a trial is already active (dropped, not
queued to fire immediately after). The original spec doesn't say what should happen here — this
was a judgment call made explicit in code and covered by a test
(`RetriggerDuringActiveTrialIsDropped`). Flag it if you'd rather a mid-trial Trigger() queue up
instead of being discarded.
