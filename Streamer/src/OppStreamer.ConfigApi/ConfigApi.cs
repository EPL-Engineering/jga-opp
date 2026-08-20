using OppStreamer.Core;
using OppStreamer.Diagnostics;
using OppStreamer.Hardware;

namespace OppStreamer.ConfigApi;

/// <summary>
/// The MATLAB-facing surface (design doc §5.8) — the one class OPP actually talks to via
/// <c>NET.addAssembly</c>. Thin by design: every method here either does simple validation/parsing
/// and then delegates straight into <see cref="StreamerEngine"/> (Core) or an
/// <see cref="IStreamerAudioOutput"/> (Hardware), never audio-thread logic of its own.
///
/// <para><b>A note on exactness (read before wiring this up to real MATLAB code):</b> this was
/// built from the design doc's method-name list (§5.8: "Mirrors the existing surface as closely
/// as possible") plus the conventions already established in Core, NOT from the original
/// LabVIEW-compiled assembly's actual signatures — that assembly wasn't available to inspect while
/// writing this. Every place a parameter list had to be *invented* rather than just carried over
/// from an existing, tested Core method is called out below with an <c>ASSUMPTION:</c> comment.
/// Please check these against the real MATLAB call sites before wiring this in, and tell me what
/// needs to change — it's a thin layer, safe and cheap to adjust.</para>
///
/// <para><b>Non-blocking, but not non-throwing:</b> per §5.8, "all ConfigApi methods are
/// non-blocking: they validate and enqueue, then return immediately... nothing on this surface
/// waits on the audio thread." That's about not blocking on loop-boundary drains — it does NOT
/// mean these methods swallow bad input. Invalid arguments (an unknown device name, an unrecognized
/// participant string, calling Start() before SetConfig()) throw synchronously, on the calling
/// (MATLAB) thread, same as any normal .NET API — that's how MATLAB finds out about a mistake,
/// since there's no other channel for it at this layer. The "errors get logged and surfaced as
/// pollable state, never thrown" rule in §6 is specifically about the AUDIO callback boundary
/// (a MicBridge hiccup, a device dropout mid-session) — see <see cref="LastError"/>.</para>
/// </summary>
public sealed class ConfigApi : IDisposable
{
    private readonly StreamerEngine _engine = new();

    private IStreamerAudioOutput? _output;
    private AudioBackend? _outputBackend;
    private AudioBackend? _backend;

    private string? _outputDeviceName;
    private string? _testerMicDeviceName;
    private string? _boothMicDeviceName;

    private bool _isOpen;
    private DiagnosticsHost? _diagnosticsHost;

    // ------------------------------------------------------------------------------------------
    // Lifecycle — Initialize/Close/IsOpen. Confirmed by design doc §5.7: this is specifically the
    // window-level lifecycle pair (it's what shows/hides DiagnosticsView), distinct from the
    // device-level Start()/Stop() pair below. IsOpen() tracks Initialize()/Close(), not whether
    // audio is currently streaming — see IsStreaming for that.
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Opens the streamer session. Must be called before any other method except the static-style
    /// enumeration/validity checks (EnumerateMicrophones, EnumerateOutputDevices,
    /// IsMicDeviceValid, IsOutputDeviceValid), which are safe to call any time — a device-selection
    /// dropdown in the MATLAB config dialog plausibly needs them before Initialize() is even
    /// reached.
    ///
    /// Also opens DiagnosticsView (design doc §5.7 — this is the "window-level lifecycle" this
    /// class comment above refers to), shown directly on whichever thread calls Initialize() (see
    /// <see cref="DiagnosticsHost"/>'s doc comment — this matches how <c>OPP.Mixer</c> already
    /// shows its own panel) — a real device isn't needed for the window to appear, only for it to
    /// have anything to plot; it shows a placeholder until SetConfig()/Start() actually produce a
    /// <see cref="StreamerEngine"/>/output to read from.
    /// </summary>
    public void Initialize()
    {
        if (_isOpen)
            throw new InvalidOperationException("Already initialized — call Close() first if you need to restart the session.");

        _diagnosticsHost = new DiagnosticsHost(() => IsStreaming, () => _output?.Waveforms);
        _isOpen = true;
        LastError = null;
    }

    /// <summary>
    /// Closes the streamer session: stops and fully releases the audio device/mic connections (if
    /// running) and clears <see cref="IsOpen"/>. Safe to call even if never Initialize()'d or
    /// already Stop()'d — a defensive "tear everything down" MATLAB can call from a cleanup path
    /// without first checking state itself.
    ///
    /// Deliberately NOT the graceful, boundary-gated stop <see cref="Stop"/> uses — this calls the
    /// transport's Stop() directly, immediately, on the calling thread. A "tear everything down
    /// now" cleanup path should actually finish before returning, not hand off to a background
    /// thread; a possible click here is an acceptable tradeoff for that.
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;

        _output?.Stop();
        _output?.Dispose();
        _output = null;
        _outputBackend = null;

        _diagnosticsHost?.Dispose();
        _diagnosticsHost = null;

        _isOpen = false;

        // Deliberately NOT cleared: _outputDeviceName/_testerMicDeviceName/_boothMicDeviceName and
        // the engine's configured loop length. A subsequent Initialize() + Start() (skipping a
        // fresh SetConfig()) still has a device to reconnect to — matches the general pattern here
        // of "re-calling a setter is required to CHANGE something, not to re-arm a restart."
    }

    /// <summary>True from Initialize() until Close() — see the class-level note on what this does and doesn't track.</summary>
    public bool IsOpen() => _isOpen;

    /// <summary>
    /// ADDITION beyond the mirrored surface (§5.8's "unchanged" list has no equivalent) — whether
    /// the audio device is actually streaming right now, i.e. between Start() and Stop(). Harmless
    /// to ignore if the MATLAB side doesn't need it; included because it's a natural, cheap
    /// diagnostic and every lower layer already tracks it (IStreamerAudioOutput.IsRunning).
    /// </summary>
    public bool IsStreaming => _output?.IsRunning ?? false;

    /// <summary>
    /// ADDITION beyond the mirrored surface — the message of the most recent Start() failure, if
    /// any, for MATLAB-side logging/display. Not the same mechanism as §6's audio-thread pollable
    /// status (that's about failures *during* streaming, e.g. a mic dropping out mid-session, which
    /// this stage doesn't yet surface anywhere — MicBridge/AsioOut swallow-and-log internally today,
    /// same as before this stage). This is specifically "why did my last Start() call fail," kept
    /// around after the exception propagates, in case MATLAB wants to poll it as well as catch it.
    /// </summary>
    public string? LastError { get; private set; }

    // ------------------------------------------------------------------------------------------
    // Device enumeration/validation — "plain NAudio device queries" per §5.8, no more black-box
    // G-Audio indirection. Deliberately NOT gated behind RequireOpen(): a device-selection dropdown
    // in the MATLAB config dialog needs these before Initialize() is necessarily called.
    // ------------------------------------------------------------------------------------------

    /// <summary>WASAPI capture device names available for the Tester/Booth mic bridges (channels 6/7).</summary>
    public string[] EnumerateMicrophones() => StreamerAudioOutputFactory.EnumerateMicDevices().ToArray();

    /// <summary>
    /// Output device names available for SetConfig()'s outputDeviceName. ASSUMPTION: since §5.8's
    /// "unchanged" list has no separate backend-selection parameter anywhere, this implements the
    /// "try ASIO devices first, fall back to WASAPI if none are present" option §5.4 explicitly
    /// floats — if any ASIO driver is present (the MOTU, in production), only ASIO names are
    /// returned; otherwise WASAPI render device names are returned (a dev/test machine without the
    /// MOTU). SetConfig() below resolves whichever name it's given against both lists anyway, so
    /// this only affects what a MATLAB-side dropdown would show by default.
    /// </summary>
    public string[] EnumerateOutputDevices()
    {
        var asio = StreamerAudioOutputFactory.EnumerateDevices(AudioBackend.Asio);
        return (asio.Count > 0 ? asio : StreamerAudioOutputFactory.EnumerateDevices(AudioBackend.Wasapi)).ToArray();
    }

    /// <summary>True if <paramref name="deviceName"/> is a currently active WASAPI capture device.</summary>
    public bool IsMicDeviceValid(string deviceName) => StreamerAudioOutputFactory.EnumerateMicDevices().Contains(deviceName);

    /// <summary>True if <paramref name="deviceName"/> is a currently active ASIO driver or WASAPI render device.</summary>
    public bool IsOutputDeviceValid(string deviceName) => ResolveBackend(deviceName) is not null;

    /// <summary>
    /// WASAPI checked FIRST, deliberately — see the 2026-08-16 finding: <c>AsioOut.GetDriverNames()</c>
    /// (queried to check ASIO) can crash the whole host process on a machine with a misbehaving
    /// ASIO driver registered, in a way .NET cannot catch (see
    /// <see cref="AsioStreamerOutput.EnumerateDrivers"/>'s doc comment). Checking WASAPI first means
    /// resolving a WASAPI device name — the common case on a dev machine without the MOTU attached —
    /// never touches ASIO enumeration at all. This does NOT remove the risk for a name that genuinely
    /// needs ASIO (the MOTU, in production) — if that machine also has a misbehaving ASIO driver
    /// registered, this call still reaches it. See the README for the fuller mitigation discussion.
    /// </summary>
    private static AudioBackend? ResolveBackend(string deviceName)
    {
        if (StreamerAudioOutputFactory.EnumerateDevices(AudioBackend.Wasapi).Contains(deviceName)) return AudioBackend.Wasapi;
        if (StreamerAudioOutputFactory.EnumerateDevices(AudioBackend.Asio).Contains(deviceName)) return AudioBackend.Asio;
        return null;
    }

    // ------------------------------------------------------------------------------------------
    // Config / phase setup.
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Configures which devices to use and fixes the current phase's loop length (one masker
    /// interval, in samples — see <see cref="StreamerEngine.Reset"/>), then begins that phase
    /// (clearing all stimulus buffers, same as a fresh <c>Reset</c>).
    ///
    /// ASSUMPTION: §5.8 lists a single unchanged <c>SetConfig</c> name with no signature given
    /// anywhere in the design doc. This shape — output device, loop length, and the two optional
    /// mic devices — is what this system actually needs configured before a phase can start
    /// (everything else has its own named setter already: SetNumReps, SetSignal, etc.). If the real
    /// MATLAB call site passes something structured differently (e.g. a single config struct, or
    /// separate device-selection vs. phase-length calls), this is the method to reshape.
    ///
    /// <paramref name="testerMicDeviceName"/>/<paramref name="boothMicDeviceName"/> are optional —
    /// see <see cref="IStreamerAudioOutput.Start"/>'s doc comment: an omitted mic just means that
    /// channel stays silent, not a failure.
    ///
    /// Per design doc §6, the device connection (if already running) stays open across a SetConfig
    /// call that doesn't change device identity — e.g. calling this again just to update a mic
    /// device name, or to re-affirm the same loop length, is fine while streaming. Two things are
    /// deliberately NOT allowed while streaming, both throwing InvalidOperationException with a
    /// message telling the caller to Stop() first:
    ///
    /// 1. Changing which device(s) are configured — obviously requires a real reconnect.
    /// 2. Changing the loop length — <see cref="StreamerEngine.Reset"/> clears StimulusStore's
    ///    active buffers directly, with no synchronization against the audio thread's concurrent
    ///    Advance() calls (Reset() was written for the "nothing is playing yet" case — see its own
    ///    doc comment). Calling it while the render callback is live is a genuine data race, not
    ///    just an inconvenience, so this is blocked outright rather than allowed to happen and
    ///    intermittently misbehave. If in practice OPP needs to change the loop length in the
    ///    middle of a streaming session without a Stop()/Start() blip, that needs a real fix to
    ///    StimulusStore itself (routing Reset through the same boundary-gate everything else uses,
    ///    or a lock) — flagging this now rather than guessing at a fix nobody asked for yet.
    /// </summary>
    public void SetConfig(string outputDeviceName, int loopLengthSamples, string? testerMicDeviceName = null, string? boothMicDeviceName = null)
    {
        RequireOpen();
        if (string.IsNullOrWhiteSpace(outputDeviceName))
            throw new ArgumentException("Output device name must not be empty.", nameof(outputDeviceName));

        var backend = ResolveBackend(outputDeviceName)
            ?? throw new ArgumentException(
                $"'{outputDeviceName}' is not a currently active ASIO or WASAPI output device. " +
                $"Available: {string.Join(", ", EnumerateOutputDevices())}", nameof(outputDeviceName));

        bool isStreaming = IsStreaming;

        bool deviceIdentityChanged = _outputDeviceName is not null && (
            outputDeviceName != _outputDeviceName ||
            testerMicDeviceName != _testerMicDeviceName ||
            boothMicDeviceName != _boothMicDeviceName);
        if (isStreaming && deviceIdentityChanged)
            throw new InvalidOperationException(
                "Changing the output or mic device while streaming requires Stop() first, then SetConfig(), then Start() again.");

        bool isNewPhase = _engine.LoopLengthSamples != loopLengthSamples;
        if (isStreaming && isNewPhase)
            throw new InvalidOperationException(
                $"Changing the loop length (from {_engine.LoopLengthSamples?.ToString() ?? "unset"} to {loopLengthSamples} samples) " +
                "while streaming isn't safe with the current engine (see this method's doc comment). Call Stop() before starting a " +
                "new phase with a different loop length, then Start() again. Re-calling SetConfig with the SAME loop length while " +
                "streaming — e.g. just to update a mic device name — is fine.");

        _backend = backend;
        _outputDeviceName = outputDeviceName;
        _testerMicDeviceName = testerMicDeviceName;
        _boothMicDeviceName = boothMicDeviceName;

        if (isNewPhase)
            _engine.Reset(loopLengthSamples);
    }

    // ------------------------------------------------------------------------------------------
    // Start/Stop — device-level lifecycle. Per §6: devices open on Start(), not Initialize(), and
    // stay open across SetConfig calls/phase transitions that don't change device identity.
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Opens the configured output device (and mic devices, if any) and begins streaming.
    /// Idempotent — calling Start() while already streaming is a no-op, matching the "stay open"
    /// lifecycle rather than throwing on a redundant call.
    /// </summary>
    public void Start()
    {
        RequireOpen();
        if (_outputDeviceName is null || _backend is null)
            throw new InvalidOperationException("Call SetConfig(...) before Start() — no output device configured yet.");
        if (_engine.LoopLengthSamples is null)
            throw new InvalidOperationException("Call SetConfig(...) before Start() — no loop length configured yet.");
        if (IsStreaming)
            return;

        if (_output is not null && _outputBackend != _backend)
        {
            // A prior Start() used a different backend (e.g. ASIO then WASAPI across a device
            // change) — that IStreamerAudioOutput instance's own MicBridge pair is now stale;
            // drop it and build a fresh one for the new backend rather than trying to reuse it.
            _output.Dispose();
            _output = null;
        }

        _output ??= StreamerAudioOutputFactory.Create(_backend.Value, _engine);
        _outputBackend = _backend;

        try
        {
            _output.Start(_outputDeviceName, _testerMicDeviceName, _boothMicDeviceName);
            LastError = null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// Time to wait for the loop boundary requested by a graceful <see cref="Stop"/> before
    /// falling back to an immediate hard stop anyway. Derived from one full loop length at the
    /// hardware layer's fixed sample rate (see WasapiStreamerOutput/AsioStreamerOutput's
    /// SampleRate constant — ConfigApi itself has no other way to know it), doubled and floored at
    /// half a second, so under ordinary operation this is never actually what decides when Stop()
    /// finishes — a boundary should always arrive well before it. It only matters as a backstop if
    /// rendering has genuinely stalled (a wedged device, no boundary ever coming), not merely
    /// because a masker interval happens to be a bit long.
    /// </summary>
    private static TimeSpan BoundaryTimeout(int? loopLengthSamples)
    {
        const int assumedSampleRateHz = 48_000;
        double loopSeconds = (loopLengthSamples ?? assumedSampleRateHz) / (double)assumedSampleRateHz;
        return TimeSpan.FromSeconds(Math.Max(0.5, loopSeconds * 2));
    }

    /// <summary>
    /// Stops streaming, if running — gracefully. Rather than cutting Caregiver/Waver/Subject off
    /// mid-waveform (an audible click/transient), this requests silence at the NEXT loop boundary
    /// (<see cref="StreamerEngine.RequestStop"/>, which reuses the exact same boundary latch every
    /// other stimulus change goes through) and only tears down the physical device once that
    /// boundary has actually been reached. Safe to call when already stopped (no-op).
    ///
    /// This is a deliberate, documented EXCEPTION to the class-level "non-blocking" note above:
    /// the call itself still returns immediately (never waits on the audio thread) — but unlike
    /// the boundary-gated setters, its effect isn't just "enqueued for whenever." The boundary
    /// wait and the actual device teardown happen on a background thread instead. <see
    /// cref="IsStreaming"/> correctly stays true for that brief window — audio genuinely is still
    /// playing (now silence) until the device is actually closed. If that boundary doesn't arrive
    /// within <see cref="BoundaryTimeout"/> (audio genuinely stalled, not just a long masker
    /// interval), this falls back to an immediate hard stop rather than hanging forever.
    ///
    /// KNOWN LIMITATION: calling Start() again before this background teardown finishes isn't
    /// currently guarded against — Start() sees IsStreaming still true (correctly, per above) and
    /// no-ops, so the in-flight graceful stop goes on to silence/tear down the device out from
    /// under that "restart," which is almost certainly not what was intended. If OPP ever needs to
    /// Stop()-then-immediately-Start() (e.g. switching devices quickly), poll IsStreaming down to
    /// false first — or say the word and I'll add a proper cancellation path.
    /// </summary>
    public void Stop()
    {
        if (!IsStreaming) return;
        var output = _output!;

        var timeout = BoundaryTimeout(_engine.LoopLengthSamples);
        _engine.RequestStop();
        Task.Run(() =>
        {
            _engine.WaitForStopBoundary(timeout); // ignored: falls through to a hard stop either way
            try
            {
                output.Stop();
                LastError = null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        });
    }

    // ------------------------------------------------------------------------------------------
    // Stimulus mutation — thin validate/convert/delegate wrappers over StreamerEngine.
    // ------------------------------------------------------------------------------------------

    public void SetNumReps(int numReps)
    {
        RequireOpen();
        _engine.SetNumReps(numReps);
    }

    /// <summary>
    /// Sets a Caregiver/Waver/Subject stimulus buffer for the given mode.
    ///
    /// ASSUMPTION: Core's <see cref="StreamerEngine.SetSignal"/> only covers Caregiver/Waver (it
    /// throws for Subject — Subject has separate Background/Signal buffers, see
    /// <see cref="StreamerEngine.SetSubjectSignal"/>), but §5.8 lists just one unchanged
    /// <c>SetSignal</c> name covering all participants including Subject. Rather than inventing a
    /// second public method name not in that list, <paramref name="isSubjectProbe"/> — defaulted
    /// so existing Caregiver/Waver call sites need no change — selects Subject's Signal buffer
    /// (true) vs. Background buffer (false); it's an error to pass it non-default for Caregiver or
    /// Waver, since they don't have that distinction. If the real API instead has separate
    /// SetBackground/SetProbe-style methods for Subject, this is the assumption to correct.
    /// </summary>
    public void SetSignal(string participant, string mode, double[] signal, bool isSubjectProbe = false)
    {
        RequireOpen();
        var p = ParseParticipant(participant);
        var m = ParseMode(mode);
        var floatSignal = ToFloat(signal);

        if (p == Participant.Subject)
        {
            _engine.SetSubjectSignal(m, isSignal: isSubjectProbe, floatSignal);
        }
        else
        {
            if (isSubjectProbe)
                throw new ArgumentException($"isSubjectProbe only applies to participant \"Subject\" (got \"{participant}\").", nameof(isSubjectProbe));
            _engine.SetSignal(p, m, floatSignal);
        }
    }

    /// <summary>
    /// Sets a participant's Training buffer, regardless of which mode is currently active.
    /// Same <paramref name="isSubjectProbe"/> assumption as <see cref="SetSignal"/> above — see its
    /// doc comment.
    /// </summary>
    public void SetTrainer(string participant, double[] signal, bool isSubjectProbe = false)
    {
        RequireOpen();
        var p = ParseParticipant(participant);
        var floatSignal = ToFloat(signal);

        if (p == Participant.Subject)
        {
            _engine.SetSubjectSignal(OperatingMode.Training, isSignal: isSubjectProbe, floatSignal);
        }
        else
        {
            if (isSubjectProbe)
                throw new ArgumentException($"isSubjectProbe only applies to participant \"Subject\" (got \"{participant}\").", nameof(isSubjectProbe));
            _engine.SetTrainer(p, floatSignal);
        }
    }

    /// <summary>
    /// Atomically updates all four Training buffers together (design doc §5.3) — the entry point
    /// for the new "vary the training masker/probe combination on the fly" feature.
    ///
    /// NOTE: §5.3's own early sketch of this method's signature was
    /// <c>SetTrainingStimulusSet(Dictionary&lt;string, double[]&gt; byParticipant)</c> — that was
    /// superseded during Core's actual build: <see cref="StreamerEngine.SetTrainingStimulusSet"/>
    /// (already built, tested, and delivered) takes four positional buffers instead, which is both
    /// simpler and gives compile-time confidence that all four are always provided together. This
    /// mirrors that already-built shape rather than reintroducing the dictionary. I've updated the
    /// design doc's §5.3 text to match.
    /// </summary>
    public void SetTrainingStimulusSet(double[] caregiver, double[] waver, double[] subjectBackground, double[] subjectSignal)
    {
        RequireOpen();
        _engine.SetTrainingStimulusSet(ToFloat(caregiver), ToFloat(waver), ToFloat(subjectBackground), ToFloat(subjectSignal));
    }

    public void TrainTest(bool isTrainer)
    {
        RequireOpen();
        _engine.TrainTest(isTrainer);
    }

    public void Trigger(bool containsProbe)
    {
        RequireOpen();
        _engine.Trigger(containsProbe);
    }

    /// <summary>Queues TTS audio (channel 5) to play after anything already queued or playing — see <see cref="TtsPlayer"/>.</summary>
    public void SendTTS(double[] signal)
    {
        RequireOpen();
        _engine.SendTts(ToFloat(signal));
    }

    // ------------------------------------------------------------------------------------------
    // Helpers.
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// The one-line double[]→float[] downcast design doc §5.9 calls for at exactly this boundary —
    /// deliberately not in Core (see StreamerEngine's own doc comment: "this class deliberately
    /// isn't that public surface yet").
    /// </summary>
    private static float[] ToFloat(double[] signal)
    {
        if (signal is null) throw new ArgumentNullException(nameof(signal));
        var result = new float[signal.Length];
        for (int i = 0; i < signal.Length; i++) result[i] = (float)signal[i];
        return result;
    }

    private static Participant ParseParticipant(string participant)
    {
        if (participant is not null && Enum.TryParse<Participant>(participant, ignoreCase: true, out var result))
            return result;
        throw new ArgumentException(
            $"'{participant}' is not a recognized participant. Valid values: {string.Join(", ", Enum.GetNames(typeof(Participant)))}.",
            nameof(participant));
    }

    private static OperatingMode ParseMode(string mode)
    {
        if (mode is not null && Enum.TryParse<OperatingMode>(mode, ignoreCase: true, out var result))
            return result;
        throw new ArgumentException(
            $"'{mode}' is not a recognized mode. Valid values: {string.Join(", ", Enum.GetNames(typeof(OperatingMode)))}.",
            nameof(mode));
    }

    private void RequireOpen()
    {
        if (!_isOpen)
            throw new InvalidOperationException("Call Initialize() before using the streamer.");
    }

    public void Dispose() => Close();
}
