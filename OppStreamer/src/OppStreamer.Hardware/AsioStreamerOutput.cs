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

    public AsioStreamerOutput(StreamerEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    public bool IsRunning => _asioOut is not null;

    /// <summary>ASIO driver names currently visible to Windows.</summary>
    public static IReadOnlyList<string> EnumerateDrivers() => AsioOut.GetDriverNames();

    public void Start(string deviceName)
    {
        if (_asioOut is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        var asioOut = new AsioOut(deviceName);
        try
        {
            var sampleProvider = new StreamerSampleProvider(_engine, SampleRate);
            // NAudio negotiates the driver's actual ASIOSampleType (commonly 24-in-32, sometimes
            // float32) against this format internally — we don't special-case it (design doc §5.9).
            asioOut.Init(new SampleToWaveProvider(sampleProvider));
            asioOut.Play();
            _asioOut = asioOut;
        }
        catch
        {
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
    }

    public void Dispose() => Stop();
}
