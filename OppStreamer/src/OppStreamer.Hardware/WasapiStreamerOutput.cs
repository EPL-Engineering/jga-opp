using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// WASAPI transport — for development and testing when a real ASIO device (the MOTU) isn't at
/// hand. A consumer 7.1-surround USB sound card conveniently exposes 8 discrete channels, which
/// happens to match the streamer's channel count, so it's a genuinely useful proxy for verifying
/// the OPP↔streamer API surface, the latch/continuity logic end to end, and basic 8-channel
/// routing — NOT a substitute for validating actual ASIO driver behavior or hardware timing
/// against the MOTU itself (design doc discussion, and see README "Development without the MOTU").
///
/// Tries exclusive mode first (channel-for-channel control, closest in spirit to how the MOTU is
/// actually driven), falling back progressively if the device won't accept it. Consumer audio
/// hardware is often picky about exact exclusive-mode formats, so this is a deliberate cascade
/// rather than a single fixed assumption:
///   1. Exclusive mode, 32-bit float (what the core pipeline produces natively).
///   2. Exclusive mode, 16-bit PCM (many consumer devices only accept this in exclusive mode).
///   3. Shared mode, using the device's own mix format — most forgiving, but only works if
///      Windows has that device configured for 8 channels (Sound Control Panel → Configure →
///      7.1 Surround) and may resample/reformat under the hood.
/// If all three fail, the exception from the shared-mode attempt is the one that surfaces, since
/// it's usually the most diagnostic (e.g. it'll show you the device's actual channel count if
/// that's the mismatch).
/// </summary>
public sealed class WasapiStreamerOutput : IStreamerAudioOutput
{
    private const int SampleRate = 48_000;

    private readonly StreamerEngine _engine;
    private WasapiOut? _wasapiOut;
    private MMDevice? _device;

    public WasapiStreamerOutput(StreamerEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    public bool IsRunning => _wasapiOut is not null;

    /// <summary>Friendly names of active WASAPI render devices currently visible to Windows.</summary>
    public static IReadOnlyList<string> EnumerateDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => d.FriendlyName)
            .ToList();
    }

    public void Start(string deviceName)
    {
        if (_wasapiOut is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .FirstOrDefault(d => d.FriendlyName == deviceName)
            ?? throw new ArgumentException($"No active WASAPI render device named '{deviceName}'. " +
                $"Available: {string.Join(", ", EnumerateDevices())}", nameof(deviceName));

        var sampleProvider = new StreamerSampleProvider(_engine, SampleRate);

        try
        {
            _wasapiOut = TryStart(device, new SampleToWaveProvider(sampleProvider), AudioClientShareMode.Exclusive)
                ?? TryStart(device, new SampleToWaveProvider16(sampleProvider), AudioClientShareMode.Exclusive)
                ?? StartOrThrow(device, new SampleToWaveProvider(sampleProvider), AudioClientShareMode.Shared,
                    "Exclusive mode was rejected in both float32 and 16-bit PCM. This usually means the device " +
                    "isn't set to 8-channel output — check Windows Sound Control Panel → this device → " +
                    "Configure → 7.1 Surround — or the shared-mode mix format doesn't match.");
        }
        catch
        {
            device.Dispose();
            throw;
        }

        // Held for the lifetime of playback (WasapiOut keeps its own reference to it too) and
        // only released in Stop() — disposing it earlier can invalidate the audio client it handed out.
        _device = device;
    }

    private static WasapiOut? TryStart(MMDevice device, IWaveProvider waveProvider, AudioClientShareMode shareMode)
    {
        var wasapiOut = new WasapiOut(device, shareMode, useEventSync: true, 100);
        try
        {
            wasapiOut.Init(waveProvider);
            wasapiOut.Play();
            return wasapiOut;
        }
        catch
        {
            wasapiOut.Dispose();
            return null;
        }
    }

    private static WasapiOut StartOrThrow(MMDevice device, IWaveProvider waveProvider, AudioClientShareMode shareMode, string context)
    {
        var wasapiOut = new WasapiOut(device, shareMode, useEventSync: true, 100);
        try
        {
            wasapiOut.Init(waveProvider);
            wasapiOut.Play();
            return wasapiOut;
        }
        catch (Exception ex)
        {
            wasapiOut.Dispose();
            throw new InvalidOperationException($"Could not open '{device.FriendlyName}' for 8-channel output. {context}", ex);
        }
    }

    public void Stop()
    {
        if (_wasapiOut is null) return;

        _wasapiOut.Stop();
        _wasapiOut.Dispose();
        _wasapiOut = null;

        _device?.Dispose();
        _device = null;
    }

    public void Dispose() => Stop();
}
