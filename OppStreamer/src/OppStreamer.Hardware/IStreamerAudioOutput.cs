namespace OppStreamer.Hardware;

/// <summary>
/// A transport that can play the streamer's 8-channel output to some real audio device — ASIO
/// (the MOTU, in production) or WASAPI (a consumer device, for development/testing when the MOTU
/// isn't at hand). ConfigApi (a later stage) targets this interface, not either concrete backend,
/// so which transport is in use is a runtime choice, not a build-time one — see
/// <see cref="StreamerAudioOutputFactory"/>.
/// </summary>
public interface IStreamerAudioOutput : IDisposable
{
    bool IsRunning { get; }

    /// <summary>
    /// Opens the device and begins streaming. Deliberately not tied to Initialize() — see design
    /// doc §6: devices open on Start(), and the connection stays open across SetConfig/phase
    /// changes that don't touch device identity, closing only on Stop() or an actual device
    /// change. This is the fix for the known G-Audio reinit-on-every-open fragility, and it
    /// applies the same way regardless of which transport is in use.
    ///
    /// <paramref name="testerMicDeviceName"/> and <paramref name="boothMicDeviceName"/> are WASAPI
    /// capture device names for channels 6/7 (see design doc §5.5) — independent USB mics on their
    /// own clocks, bridged in via a drift-compensated ring buffer regardless of which output
    /// transport is active. Either (or both) may be omitted, e.g. while developing against the
    /// output path alone without mic hardware at hand: an omitted mic's channel is simply silent,
    /// exactly like an unwired channel, rather than the call failing.
    /// </summary>
    void Start(string deviceName, string? testerMicDeviceName = null, string? boothMicDeviceName = null);

    void Stop();
}
