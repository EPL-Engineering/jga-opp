using NAudio.Wave;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// Owns the ASIO connection to the MOTU Monitor 8 and drives <see cref="StreamerEngine"/> from
/// its callback via <see cref="StreamerWaveProvider"/>.
///
/// Per design doc §6: devices open on <see cref="Start"/>, not at construction/Initialize — this
/// is the deliberate fix for the known G-Audio error -2 history (reinitializing hardware on every
/// open). The ASIO stream stays open across <c>SetConfig</c>/phase changes that don't touch device
/// identity; only <see cref="Stop"/> (or a genuine device change, handled by calling Stop then
/// Start again with a different driver name) tears it down.
/// </summary>
public sealed class MotuOutputEngine : IDisposable
{
    private const int SampleRate = 48_000;

    private readonly StreamerEngine _engine;
    private AsioOut? _asioOut;

    public MotuOutputEngine(StreamerEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    public bool IsRunning => _asioOut is not null;

    /// <summary>ASIO driver names currently visible to Windows — surface this via EnumerateOutputDevices() in ConfigApi.</summary>
    public static IReadOnlyList<string> EnumerateAsioDriverNames() => AsioOut.GetDriverNames();

    public void Start(string asioDriverName)
    {
        if (_asioOut is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        var asioOut = new AsioOut(asioDriverName);
        try
        {
            var waveProvider = new StreamerWaveProvider(_engine, SampleRate);
            asioOut.Init(waveProvider);
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
