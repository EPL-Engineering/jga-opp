using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// ASIO transport — what the MOTU Monitor 8 actually uses in production. See design doc §2 for
/// why ASIO rather than WASAPI is the right choice for the MOTU specifically (professional
/// multichannel interface, single sample clock).
/// </summary>
public sealed class AsioStreamerOutput : IStreamerAudioOutput
{
    private const int SampleRate = 48_000;

    private readonly StreamerEngine _engine;
    private AsioOut? _asioOut;

    // Owned for the lifetime of this transport (design doc §6: "ASIO stream and both mic captures
    // open once at Start()") rather than per-Start — MicBridge itself is a reusable Start/Stop
    // object, same as AsioOut, so there's no need to recreate it across a Stop()/Start() cycle.
    private readonly MicBridge _testerMic = new();
    private readonly MicBridge _boothMic = new();

    // See WasapiStreamerOutput's identical field for the bucketing rationale (10ms x 500 = 5s of
    // DiagnosticsView history, design doc §5.7).
    private readonly WaveformMonitor _waveforms = new(MonitoredChannel.Names, samplesPerBucket: SampleRate / 100, bucketsPerChannel: 500);

    public AsioStreamerOutput(StreamerEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    public bool IsRunning => _asioOut is not null;

    public WaveformMonitor Waveforms => _waveforms;

    /// <summary>Tester mic bridge (channel 6) — for diagnostics (UnderrunSampleCount, OverflowSampleCount, CurrentFillLevel).</summary>
    public MicBridge TesterMic => _testerMic;

    /// <summary>Booth mic bridge (channel 7) — for diagnostics (UnderrunSampleCount, OverflowSampleCount, CurrentFillLevel).</summary>
    public MicBridge BoothMic => _boothMic;

    /// <summary>
    /// ASIO driver names currently visible to Windows. Defensively wrapped: enumerating ASIO
    /// drivers means querying every ASIO driver registered on the machine (including third-party
    /// ones), and a single misbehaving/orphaned driver registration is a known way for that to
    /// throw for ALL drivers, not just the bad one — or, worse, to fault natively rather than raise
    /// an ordinary .NET exception at all (see the 2026-08-16 MATLAB crash: a SEHException that
    /// reached AppDomain.UnhandledException instead of surfacing as a catchable error — .NET Core
    /// cannot catch a true corrupted-state/access-violation-class fault in-process no matter how
    /// this method is written, so this catch is a real but partial mitigation, not a guarantee).
    /// Returning an empty list on failure at least degrades to "no ASIO drivers usable" instead of
    /// throwing somewhere a caller isn't expecting it.
    /// </summary>
    public static IReadOnlyList<string> EnumerateDrivers()
    {
        try
        {
            return AsioOut.GetDriverNames();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    public void Start(string deviceName, string? testerMicDeviceName = null, string? boothMicDeviceName = null)
    {
        if (_asioOut is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        var asioOut = new AsioOut(deviceName);
        try
        {
            var sampleProvider = new StreamerSampleProvider(_engine, SampleRate, _testerMic, _boothMic, _waveforms);
            // NAudio negotiates the driver's actual ASIOSampleType (commonly 24-in-32, sometimes
            // float32) against this format internally — we don't special-case it (design doc §5.9).
            asioOut.Init(new SampleToWaveProvider(sampleProvider));

            // Mic capture is WASAPI regardless of which output transport is active (design doc
            // §5.5: "independent USB microphones, not MOTU input channels") — started here so both
            // mics come up and go down together with the ASIO stream, per §6's "open once, keep
            // open" lifecycle. Either name may be null (no device available yet / not wired up for
            // this test) and that mic's channel is simply silent — see StreamerSampleProvider.
            if (testerMicDeviceName is not null) _testerMic.Start(testerMicDeviceName);
            if (boothMicDeviceName is not null) _boothMic.Start(boothMicDeviceName);

            asioOut.Play();
            _asioOut = asioOut;
        }
        catch
        {
            _testerMic.Stop();
            _boothMic.Stop();
            asioOut.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        if (_asioOut is null) return;

        _asioOut.Stop();
        _asioOut.Dispose();
        _asioOut = null;

        _testerMic.Stop();
        _boothMic.Stop();
    }

    public void Dispose()
    {
        Stop();
        _testerMic.Dispose();
        _boothMic.Dispose();
    }
}
