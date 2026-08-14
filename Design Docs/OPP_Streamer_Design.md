# OPP Audio Streamer — .NET/NAudio Redesign

## Design Reference Document

---

## 1. Overview

The OPP audio streamer is being rewritten from LabVIEW (compiled to a .NET assembly) to native C#/.NET using NAudio, consumed by the existing MATLAB OPP application exactly as today (via `NET.addAssembly`). The trigger for the rewrite is a specific feature request — letting researchers vary the training masker/probe combination on the fly — but the request is really exposing a structural limitation: the current LabVIEW block diagram hand-wires a separate latch/state-holding mechanism for each thing that must change without audibly glitching the output (`Trigger`, `TrainTest`). Adding another one (live training-stimulus swap) is the point at which that approach stops scaling.

**Goals**

- Preserve exact external behavior and API surface the MATLAB app already depends on, so the OPP-side integration work is minimal.
- Generalize the "latch a change, apply it without an audible glitch" mechanism so it's a single reusable piece of infrastructure, not a per-feature special case.
- Eliminate the LabVIEW Run-Time Engine dependency and its startup lag.
- Fix the known G-Audio reinitialization fragility (error -2) by not tearing down and rebuilding the audio device on every open.
- Land on an architecture that's straightforward for both humans and LLM tooling to reason about and extend.

**Non-goals**

- Changing the MATLAB-side config dialog or OPP's control logic.
- Changing the audio hardware (MOTU Monitor 8) or the channel assignment.
- Adding new experimental capabilities beyond what's described here — this is a faithful, more maintainable re-implementation plus the one requested feature.

---

## 2. System Context

**Participants and what they hear** (unchanged from current system):

| Participant | Hears |
|---|---|
| Subject | Masker continuous, probe inserted intermittently; reward video audio |
| Caregiver | Probe or masker+probe pair, continuously (never knows when Subject gets it) |
| Waver | Same as Caregiver, except during Training (hears what Subject hears); Tester talkback; TTS |
| Tester | Waver, via booth mic |

**Output channel map to the MOTU (unchanged):**

| Channel | Content |
|---|---|
| 0–1 | Silence (reserved for video player audio) |
| 2 | Caregiver stimulus |
| 3 | Waver stimulus |
| 4 | Subject stimulus |
| 5 | Text-to-speech |
| 6 | Tester mic → Waver |
| 7 | Booth mic → Tester |

**Hardware and device topology (clarified during design discussion):**

- **MOTU Monitor 8** — professional multichannel interface, driven via **ASIO** (matching what the existing "G-Audio" LabVIEW layer almost certainly used). NAudio's `AsioOut` is the output engine.
- **Tester mic and Booth mic are independent USB microphones**, not MOTU input channels. This means their capture streams run on their own clocks, separate from the MOTU's ASIO clock — the streamer has to bridge across that clock boundary itself (see §5.5).
- **MATLAB 2025 / Windows 11.** MATLAB has supported non-Framework .NET assemblies since R2022b, but defaults to .NET Framework on Windows unless `dotnetenv("core")` is called — target runtime (.NET Framework 4.8 vs. modern .NET) is a deployment decision, not a blocker either way, and is deferred to the packaging stage.

---

## 3. Key Design Principles

1. **Continuity is the one hard timing requirement.** A trial must start and stop without an audible seam in the ongoing masker pattern — everything else has generous slack ("the uncertain delay is a bonus feature"). The design should spend its precision budget entirely on this property.
2. **Every state change that could audibly interrupt playback goes through one mechanism**, not one mechanism per feature. `Trigger`, `TrainTest`, and the new live training-stimulus swap are all instances of the same thing: "apply this change at the next safe boundary."
3. **The safe boundary is the *active buffer's* loop point, not a fixed clock tick.** The spec is explicit about this: a latched request is applied when the *currently playing* stimulus reaches the end of its own loop and wraps back to the start — not on some fixed 100ms cadence. The 100ms "frame" is a separate, tunable, lower-level processing/acquisition chunk size, orthogonal to the loop-boundary concept. Conflating the two is one of the easier mistakes to make here, so it's called out explicitly.
4. **Open devices once, keep them open.** The known G-Audio failure mode was reinitializing hardware unnecessarily. ASIO device and mic capture streams should be opened on `Initialize()`/`Start()` and stay open across `SetConfig`/phase changes that don't change the underlying devices, closing only on `Close()` or an actual device change.
5. **Nothing on the audio thread can throw across the MATLAB interop boundary.** Errors get logged and surfaced as pollable state, never as exceptions that could crash or hang the calling MATLAB session.

---

## 4. Component Map

```
                         ┌───────────────────┐
   MATLAB (OPP)  ───────▶│     ConfigApi      │  (thin, matches existing surface)
                         └─────────┬──────────┘
                                   │ enqueues changes
                                   ▼
                       ┌────────────────────────┐
                       │   PendingChangeQueue    │  (thread-safe latch)
                       └───────────┬─────────────┘
                                   │ drained at loop boundaries
                                   ▼
        ┌───────────────┐  ┌──────────────────┐
        │ StimulusStore │◀▶│ TrialStateMachine │
        └───────┬───────┘  └────────┬──────────┘
                │                   │
                ▼                   ▼
          ┌─────────────────────────────┐        ┌───────────────┐
          │      MotuOutputEngine        │◀───────│   TtsPlayer   │ (ch5, independent of loop)
          │   (NAudio AsioOut, 8ch out)  │        └───────────────┘
          └──────────┬───────────┬──────┘
                      │           │
             ┌────────▼───┐  ┌────▼────────┐
             │ MicBridge   │  │ MicBridge   │   (ch6, ch7 — independent clocks,
             │  (Tester)   │  │  (Booth)    │    drift-compensated)
             └─────────────┘  └─────────────┘

          ┌───────────────────────────────┐
          │       DiagnosticsView          │  (self-contained window, owned by
          │  (stacked real-time plot)      │   the streamer process — see §5.7)
          └───────────────┬────────────────┘
                           │ reads decimated snapshot
                           ▲
              (published by the active IStreamerAudioOutput —
               AsioStreamerOutput in production, WasapiStreamerOutput for dev/test)
```

---

## 5. Component Details

### 5.1 StimulusStore

Holds every named buffer the system knows about: Subject Background/Signal × Test/Training, Caregiver × Test/Training, Waver × Test/Training. Buffer arrays are set via `SetSignal`/`SetTrainer`/`SetTrainingStimulusSet` at phase-init or, now, at arbitrary times.

**Correction from the original draft of this document:** Caregiver, Waver, and Subject don't each get an independent playback cursor. The spec is explicit that every stimulus buffer for a phase is exactly one masker interval (I) long, so all three participants loop through their buffer **in lockstep on a single shared cursor**, wrapping at the same instant. That single shared wrap is what makes `SetTrainingStimulusSet`'s atomicity guarantee (§5.3) natural rather than something bolted on — there's only ever one boundary event to land on, not three independent ones that happen to coincide.

Writes fall into two categories:

- **Immediate-safe** — updating a buffer that isn't currently the active one for its slot. No glitch risk; can apply as soon as it arrives.
- **Boundary-gated** — anything that changes what's currently audible (switching the active buffer for a slot, or replacing the buffer that's currently playing). These go through the `PendingChangeQueue` and apply only when that slot's cursor wraps.

### 5.2 PendingChangeQueue

A thread-safe mailbox that every mutating API call writes into. At each loop-boundary event (a playback cursor wrapping), the engine drains whatever's currently queued for that slot and applies it atomically. Because MATLAB calls happen essentially instantaneously relative to audio loop timescales, changes issued together (e.g., the three training buffers in a `SetTrainingStimulusSet` call) land in the same drain and take effect together — but see §5.3 for why that call exists as a single atomic entry point rather than relying on three separate calls landing together by coincidence.

### 5.3 TrialStateMachine

Tracks trial state (`Idle` / `TrialActive` with remaining probe-repeat count) independently of which top-level mode (Test/Training) is active. `Trigger(containsProbe)` is latched like everything else: at the next loop boundary, if `containsProbe`, the active Subject slot switches from Background to Signal (whichever mode's Signal buffer is currently selected — Test.Signal or Training.Signal); the trial-active-window flag opens regardless of `containsProbe`, for the configured number of loops, then closes and Subject reverts to Background.

Because `Trigger()` works in both Test and Training mode (confirmed), this state machine deliberately doesn't know or care which mode it's in — it only cares which buffer is currently "the Signal for Subject," which `TrainTest()` and the training-swap calls control independently. This orthogonality is what keeps the two concerns from needing to know about each other.

**New in this design:** `SetTrainingStimulusSet(Dictionary<string, double[]> byParticipant)` — a single call that queues Subject, Caregiver, and Waver training buffer updates together, guaranteeing they apply at the same boundary. This replaces relying on three separate `SetTrainer` calls arriving close enough together to land in the same drain.

### 5.4 Audio Output Transport — `IStreamerAudioOutput`

The channel-filling logic (`StreamerSampleProvider`) only ever talks to `StreamerEngine.RenderFrame` — it has no idea whether the audio it produces ends up on ASIO or WASAPI, exclusive mode or shared. That deliberate separation (float in, format/transport decided at the very edge — see §5.9) turned out to pay for itself sooner than expected: **development doesn't require the MOTU to be physically present.** Two backends implement a common `IStreamerAudioOutput` interface (`Start(deviceName)` / `Stop()`), selected at runtime rather than compiled in:

- **`AsioStreamerOutput`** — wraps NAudio's `AsioOut`. What the MOTU actually uses in production.
- **`WasapiStreamerOutput`** — wraps NAudio's `WasapiOut` against an ordinary Windows audio device. A consumer USB 7.1 sound card conveniently exposes 8 discrete channels, matching the streamer's channel count, which makes it a genuinely useful stand-in for development away from the MOTU — verifying the OPP↔streamer API surface, the latch/continuity logic end to end, and basic 8-channel routing. It tries exclusive mode first (float, then 16-bit PCM, since consumer exclusive-mode support is picky about exact formats), falling back to shared mode as a last resort.

**This does not replace testing against the real MOTU.** It validates everything upstream of the physical driver — the API, the state machine, channel routing — but not actual ASIO driver behavior or real hardware timing. Both matter; they're just not the same test.

Whichever backend is active, each render callback:

1. Pulls the next chunk of samples for Caregiver/Waver/Subject from `StreamerEngine.RenderFrame` (which itself drives `StimulusStore`'s shared cursor and applies the boundary-latch drain — see §5.1's correction above).
2. Writes silence to channels 0–1.
3. Pulls from `TtsPlayer` for channel 5.
4. Pulls from each `MicBridge` for channels 6–7.
5. Publishes a decimated snapshot of the frame for `DiagnosticsView`.

(Steps 3–5 are later-stage additions — see §7. Stage 1 wires up only steps 1–2, with 3–5 stubbed to silence.)

### 5.5 MicBridge (Tester, Booth)

Each mic is captured independently via NAudio's WASAPI capture, on its own clock. Because the render side (MOTU/ASIO) has a different clock, a naive direct handoff will eventually starve or overflow. Each bridge is a small ring buffer with a target fill level; the ASIO callback drains from it each frame, and a lightweight drift corrector nudges the effective read rate up or down slightly when the buffer trends away from its target — inaudible given the loose timing budget, and avoids the clicks/gaps that hard inserts or drops would cause. This runs indefinitely for a session, so it's designed and tested as its own unit (steady-state fill level over a long soak test is the key thing to validate).

**Confirmed:** the Tester mic streams to channel 6 continuously; talkback gating is handled downstream by the mixer app, not the streamer. Both `MicBridge` instances are always-on, with no mute/gating logic needed in this component at all.

### 5.6 TtsPlayer

Simplest component in the system, by design: presynthesized audio arrives at irregular times and does **not** need to be phase-locked to anything. It's a FIFO of PCM buffers — new audio is appended on arrival and drained continuously into channel 5 as fast as it's available, silence when the queue is empty. No loop-boundary gating applies here at all.

**Confirmed:** newly arriving TTS audio queues/appends after whatever's currently playing rather than interrupting it.

### 5.7 DiagnosticsView

Kept in the streamer, self-contained, per your preference — this avoids exactly the cross-thread marshaling pain you were describing with MATLAB-side plotting. It's a window owned by the streamer process, shown/hidden by the existing `Initialize()`/`Close()`/`IsOpen()` lifecycle (which already models the streamer as owning its own window, so this isn't a new concept, just a new thing drawn inside it).

Implementation: `MotuOutputEngine` publishes a cheap decimated snapshot per channel (running min/max per small time bucket, computed incrementally on the audio thread with negligible cost) via a double-buffered handoff; a UI timer (10–20 Hz) redraws from the latest snapshot on the UI thread. No MATLAB involvement, no shared-state locking headaches. Recommend **ScottPlot** (MIT-licensed, built for exactly this kind of real-time streaming waveform display, WinForms/WPF support) over something heavier like OxyPlot.

### 5.8 ConfigApi

Mirrors the existing surface as closely as possible so the MATLAB-side integration is close to a drop-in replacement:

- **Unchanged:** `Initialize`, `Close`, `IsOpen`, `EnumerateMicrophones`, `EnumerateOutputDevices`, `IsMicDeviceValid`, `IsOutputDeviceValid`, `SetConfig`, `SetNumReps`, `SetSignal`, `SetTrainer`, `TrainTest`, `Start`, `Stop`, `Trigger`.
- **New:** `SetTrainingStimulusSet(...)` (§5.3), and `SendTTS(double[] signal)` — matching the naming convention of the existing surface.
- Device enumeration stops reaching into a black-box layer (no more "G-Audio" indirection) and becomes plain NAudio device queries.

All `ConfigApi` methods are non-blocking: they validate and enqueue, then return immediately. Nothing on this surface waits on the audio thread.

### 5.9 Sample Format Handling

The MOTU Monitor 8's analog converters are 24-bit (its internal DSP mixing/monitoring engine runs 32-bit float, but that's downstream of the ASIO boundary this streamer talks to). Rather than hardcoding an assumed wire format, `MotuOutputEngine` queries NAudio's reported `ASIOSampleType` when the ASIO stream opens and adapts at that single boundary — MOTU's Windows ASIO driver commonly exposes 24-bit packed in a 32-bit container, but this can vary by driver mode, so the code shouldn't assume. Internally, the rest of the pipeline (`StimulusStore`, `TtsPlayer`, `MicBridge`) works in 32-bit float throughout, which is a one-line downcast from the `double[]` arrays MATLAB already hands over and keeps every component except the ASIO boundary itself decoupled from hardware sample-format details.

**Confirmed:** every stimulus channel (Subject, Caregiver, Waver, TTS) is mono — the mixer app handles any downstream combining. Sample rate is fixed at 48,000 Hz.

---

## 6. Error Handling & Lifecycle

- ASIO stream and both mic captures open once at `Start()` — deliberately not at `Initialize()`, so there's a window (after the diagnostics window opens, before the device actually locks) to check/adjust settings first. They then stay open across `SetConfig` calls and phase transitions that don't change device identity, and reopen only on an actual device change or explicit `Close()` — directly addressing the G-Audio error -2 history.
- All audio-thread exceptions are caught at the callback boundary, logged, and reflected in a pollable status property rather than thrown — nothing should be able to propagate into MATLAB and crash or hang the session.
- A rolling local log file is included from the start, since this runs unattended on lab machines operated by non-programmers and needs to be diagnosable after the fact.

---

## 7. Build Plan (staged)

1. **Core playback skeleton** — `StimulusStore` + `PendingChangeQueue` + `TrialStateMachine` + `MotuOutputEngine` against real ASIO hardware, driven by synthetic test buffers, no mics/TTS/UI yet. Validates the central claim of the whole redesign: glitch-free boundary-latched swaps, including the new live training-stimulus case.
2. **Mic capture + drift-compensated bridge** for channels 6–7.
3. **TTS player** for channel 5.
4. **ConfigApi** — full surface, ready for MATLAB-side integration and testing.
5. **DiagnosticsView** — real-time stacked plot window.
6. **Validation** — loopback-recorded comparison against the current LabVIEW streamer (continuity at trial start/stop, continuity at training toggle, mic drift over long soak runs), plus a side-by-side field trial before full cutover.
7. **Packaging & rollout** — .NET target decision (Framework vs. modern .NET, pending confirmation of MATLAB versions across lab machines), installer, parallel-run/rollback plan.

---

## 8. Resolved During Design Review

- Talkback: handled by the mixer app downstream, not the streamer (§5.5).
- TTS delivery: queue/append, not interrupt; API is `SendTTS(double[] signal)` (§5.6, §5.8).
- `SetTrainingStimulusSet` name confirmed.
- Sample format: mono, 48,000 Hz, 24-bit MOTU converters negotiated via ASIO at runtime rather than hardcoded (§5.9).
- Device lifecycle: devices open on `Start()`, not `Initialize()` (§6).

All design-level open questions are now resolved. Remaining decisions (e.g., .NET Framework vs. modern .NET target, repo/workflow setup) belong to Stage 1 project scaffolding rather than the architecture itself.
