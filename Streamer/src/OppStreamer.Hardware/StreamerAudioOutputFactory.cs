namespace OppStreamer.Hardware
{
    public enum AudioBackend
    {
        /// <summary>The MOTU Monitor 8 in production — see design doc §2.</summary>
        Asio,

        /// <summary>A consumer device (e.g. a USB 7.1 sound card) for development/testing without the MOTU.</summary>
        Wasapi,
    }

    /// <summary>
    /// Picks a transport at runtime. ConfigApi (a later stage) is expected to expose this choice —
    /// directly or indirectly (e.g. "try ASIO devices first, fall back to WASAPI if none are
    /// present") — rather than hardcoding one backend, so the same streamer binary works whether
    /// you're at a lab machine with the MOTU or at the shop with a USB sound card.
    /// </summary>
    public static class StreamerAudioOutputFactory
    {
        public static IStreamerAudioOutput Create(AudioBackend backend, OppStreamer.Core.StreamerEngine engine)
            => backend switch
            {
                AudioBackend.Asio => new AsioStreamerOutput(engine),
                AudioBackend.Wasapi => new WasapiStreamerOutput(engine),
                _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null),
            };

        /// <summary>Device/driver names available for a given backend right now.</summary>
        public static IReadOnlyList<string> EnumerateDevices(AudioBackend backend)
            => backend switch
            {
                AudioBackend.Asio => AsioStreamerOutput.EnumerateDrivers(),
                AudioBackend.Wasapi => WasapiStreamerOutput.EnumerateDevices(),
                _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null),
            };

        /// <summary>
        /// WASAPI capture device names for the Tester/Booth mic bridges (channels 6/7) — independent
        /// of which output backend is active (design doc §5.5). A single call site for the same query
        /// ConfigApi's future EnumerateMicrophones() surface (§5.8) will need.
        /// </summary>
        public static IReadOnlyList<string> EnumerateMicDevices() => MicBridge.EnumerateDevices();
    }
}