# OppStreamer — Stage 1

Stage 1 of the .NET/NAudio rewrite of the OPP audio streamer. See `OPP_Streamer_Design.md`
(delivered separately) for the full architecture and rationale.

## What's here

- **`src/OppStreamer.Core`** — the hardware-independent playback logic: `StimulusStore`,
  `PendingChangeQueue`, `TrialStateMachine`, and `StreamerEngine` (the composition root). No
  external dependencies at all. This is the central bet of the whole redesign — that
  boundary-latched changes (Trigger, TrainTest, and the new live training-stimulus swap) can be
  handled by one generalized mechanism instead of hand-wired per feature — and it's fully built
  and tested.
- **`test/OppStreamer.Core.Tests`** — 8 tests exercising exactly that: no audible glitch mid-loop,
  atomic multi-participant swaps landing on the same boundary, sample-accurate behavior across
  multiple loop wraps within a single audio callback, and a couple of edge cases (buffer-length
  validation, re-triggering mid-trial) that came up while writing the tests. **All 8 pass.**
- **`src/OppStreamer.Hardware`** — `StreamerSampleProvider` (feeds `StreamerEngine`'s output into
  NAudio as plain float) plus two interchangeable transports behind a common
  `IStreamerAudioOutput` interface: `AsioStreamerOutput` (the MOTU, in production) and
  `WasapiStreamerOutput` (any ordinary Windows audio device — see "Development without the MOTU"
  below). Real source, not a stub — but see the note below on what "verified" means for this
  project. Confirmed by you: this project builds cleanly on a real machine.

## What's verified, and what isn't

This solution was drafted in a cloud sandbox with **no network access to nuget.org** (only a
narrow allowlist of package registries — npm, PyPI, a couple others — is reachable there; that's
a property of the sandbox, not something you'll hit normally). Practical effect:

- `OppStreamer.Core` and `OppStreamer.Core.Tests` have zero external package dependencies, so they
  restore, build, and run right there in the sandbox — genuinely verified, not just written.
- `OppStreamer.Hardware` depends on the NAudio package and could not be restored or compiled in
  that sandbox at all (confirmed: it fails cleanly at the restore step, `NU1100`, not some deeper
  problem). It's real code, written against NAudio 2.2.1's actual `AsioOut`/`IWaveProvider` API,
  but **it has not been compiled anywhere, let alone run against real hardware.** Restoring and
  building it — and then testing it against the actual MOTU — is the first thing to do once this
  is open on a normal Windows machine with the MOTU attached.

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

What this validates: the OPP↔streamer API surface, the full latch/continuity state machine
end-to-end (not just the in-process unit tests), device open/close lifecycle, and basic 8-channel
routing. What it doesn't validate: actual ASIO driver behavior or real hardware timing — that
still needs the MOTU, whenever you're at the shop.

## Known gaps / next stages

Per the design doc's staged build plan — none of this is in Stage 1 yet:

- Mic capture + drift-compensated bridge (channels 6/7)
- TTS player (channel 5) — `SendTTS(double[] signal)`, queue/append semantics
- The MATLAB-facing `ConfigApi` surface (string/double[] calls translating into the enum-based
  calls `StreamerEngine` exposes today)
- Diagnostics window with the real-time stacked plot
- The TTS and mic-related channels in `StreamerSampleProvider` are explicitly stubbed to silence
  right now (see the comments in that file) — the channel layout is already correct, so wiring
  them up later shouldn't require reworking anything that's here today.

## A design decision worth double-checking

`TrialStateMachine.Trigger()` is a no-op if called while a trial is already active (dropped, not
queued to fire immediately after). The original spec doesn't say what should happen here — this
was a judgment call made explicit in code and covered by a test
(`RetriggerDuringActiveTrialIsDropped`). Flag it if you'd rather a mid-trial Trigger() queue up
instead of being discarded.
