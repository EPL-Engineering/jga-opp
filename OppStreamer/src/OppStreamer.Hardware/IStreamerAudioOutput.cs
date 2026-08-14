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
    /// </summary>
    void Start(string deviceName);

    void Stop();
}
