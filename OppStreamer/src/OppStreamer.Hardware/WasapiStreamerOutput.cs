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
/// actually driven), falling back progressively if the device won't accept it:
///   1. Exclusive mode, 32-bit float (what the core pipeline produces natively).
///   2. Exclusive mode, 16-bit PCM (many consumer devices only accept this in exclusive mode).
///   3. Shared mode, using the device's own mix format — most forgiving, but only works if
///      Windows has that device configured for 8 channels (Sound Control Panel → Configure →
///      7.1 Surround).
///
/// IMPORTANT, found the hard way: NAudio's exclusive-mode Init() does NOT reliably throw when the
/// device rejects the exact format you asked for. If the driver reports a "closest" format
/// instead, NAudio silently accepts it and wraps your audio in an internal resampler to convert
/// to THAT format instead — no exception. For a consumer card, the "closest" format to 8-channel
/// float is quite plausibly stereo. Left unchecked, that means Init()/Play() both "succeed" while
/// your 8-channel routing gets silently collapsed before it ever reaches the device — audio
/// plays, but per-channel assignment is gone. So every attempt here explicitly checks
/// wasapiOut.OutputWaveFormat.Channels after Init() and treats a channel-count mismatch as a
/// failure of that attempt, same as a thrown exception would be.
///
/// SECOND finding, also from real-hardware testing: WASAPI exclusive mode requires
/// WAVEFORMATEXTENSIBLE for any format with more than 2 channels — a documented Windows
/// requirement, independent of the device. A plain WaveFormat with channels=8 gets rejected in
/// exclusive mode even on a device genuinely configured for 8-channel output. Both exclusive
/// attempts now force WAVEFORMATEXTENSIBLE via ExtensibleFormatOverride below.
///
/// Caveat worth knowing about: WAVEFORMATEXTENSIBLE also carries a channel mask (which physical
/// speaker each channel maps to). The mask this code uses (NAudio's default: channels 0-7 mapped
/// to front-left/right/center/LFE/back-left/right/front-left-of-center/front-right-of-center —
/// the older KSAUDIO_SPEAKER_7POINT1 layout) may not exactly match what Windows' modern "Configure
/// → 7.1 Surround" wizard sets on the device (KSAUDIO_SPEAKER_7POINT1_SURROUND — same first 6
/// channels, but side-left/right instead of front-left/right-of-center for the last two). Most
/// drivers accept it regardless since the mask is speaker-labeling metadata, not something they
/// usually validate strictly — but if you still see a rejection after this fix specifically citing
/// the channel mask, that's the next thing to chase, and NAudio 2.2.1 doesn't expose a public way
/// to set an arbitrary mask, so it'd need a small reflection-based workaround.
/// </summary>
public sealed class WasapiStreamerOutput : IStreamerAudioOutput
{
    private const int ChannelCount = 8;
    private const int SampleRate = 48_000;

    private readonly StreamerEngine _engine;
    private WasapiOut? _wasapiOut;
    private MMDevice? _device;

    // Owned for the lifetime of this transport (design doc §6: "ASIO stream and both mic captures
    // open once at Start()" — applies the same way here) rather than per-Start — MicBridge itself
    // is a reusable Start/Stop object, so there's no need to recreate it across a Stop()/Start()
    // cycle. Note these are always WASAPI captures regardless of which output backend is active
    // (design doc §5.5): even when this class itself is standing in for the MOTU during dev/test,
    // real mic hardware (if any is plugged in) is bridged in the same way.
    private readonly MicBridge _testerMic = new();
    private readonly MicBridge _boothMic = new();

    public WasapiStreamerOutput(StreamerEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    public bool IsRunning => _wasapiOut is not null;

    /// <summary>Tester mic bridge (channel 6) — for diagnostics (UnderrunSampleCount, OverflowSampleCount, CurrentFillLevel).</summary>
    public MicBridge TesterMic => _testerMic;

    /// <summary>Booth mic bridge (channel 7) — for diagnostics (UnderrunSampleCount, OverflowSampleCount, CurrentFillLevel).</summary>
    public MicBridge BoothMic => _boothMic;

    /// <summary>Which share mode actually ended up active. Null until Start() succeeds.</summary>
    public AudioClientShareMode? ActiveShareMode { get; private set; }

    /// <summary>
    /// The format WASAPI actually negotiated — check .Channels == 8 if you want to confirm for
    /// yourself that discrete per-channel routing is really in effect, not silently downmixed.
    /// </summary>
    public WaveFormat? ActiveFormat { get; private set; }

    /// <summary>Friendly names of active WASAPI render devices currently visible to Windows.</summary>
    public static IReadOnlyList<string> EnumerateDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => d.FriendlyName)
            .ToList();
    }

    public void Start(string deviceName, string? testerMicDeviceName = null, string? boothMicDeviceName = null)
    {
        if (_wasapiOut is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .FirstOrDefault(d => d.FriendlyName == deviceName)
            ?? throw new ArgumentException($"No active WASAPI render device named '{deviceName}'. " +
                $"Available: {string.Join(", ", EnumerateDevices())}", nameof(deviceName));

        var sampleProvider = new StreamerSampleProvider(_engine, SampleRate, _testerMic, _boothMic);
        var attemptedFormats = new List<string>();

        // WASAPI requires WAVEFORMATEXTENSIBLE for any format with more than 2 channels — a
        // documented Windows requirement that applies in BOTH exclusive and shared mode, not
        // device-specific and not exclusive-only (an earlier version of this fix only covered
        // exclusive mode, which is why the shared/float32 attempt was still throwing).
        // NAudio's SampleToWaveProvider/SampleToWaveProvider16 both build plain formats internally
        // (and both require their *source* to be plain IeeeFloat, so the override can't be pushed
        // upstream into StreamerSampleProvider either — see its comment). So: convert normally
        // first, then override just the format descriptor on the resulting IWaveProvider before
        // handing it to WASAPI. The raw sample byte layout Read() produces is unaffected either
        // way — this only changes what's used for negotiation.
        //
        // A fresh StreamerSampleProvider/SampleToWaveProvider per attempt (rather than sharing one
        // across attempts) — cheap to construct, and avoids any question of a failed attempt
        // having already pulled a Read() (and so advanced StreamerEngine's playback cursor)
        // before a later attempt gets its turn.
        // _testerMic/_boothMic themselves are NOT recreated per attempt (unlike the
        // StreamerSampleProvider wrapper) — they're this transport's persistent mic connections,
        // same lifecycle rules as _engine, just wrapped fresh each time.
        IWaveProvider Float32Extensible() => new ExtensibleFormatOverride(
            new SampleToWaveProvider(new StreamerSampleProvider(_engine, SampleRate, _testerMic, _boothMic)),
            new WaveFormatExtensible(SampleRate, 32, ChannelCount));
        var pcm16Exclusive = new ExtensibleFormatOverride(
            new SampleToWaveProvider16(sampleProvider),
            new WaveFormatExtensible(SampleRate, 16, ChannelCount));

        try
        {
            var result =
                TryStart(device, Float32Extensible(), AudioClientShareMode.Exclusive, attemptedFormats, "exclusive/float32") ??
                TryStart(device, pcm16Exclusive, AudioClientShareMode.Exclusive, attemptedFormats, "exclusive/16-bit PCM") ??
                TryStart(device, Float32Extensible(), AudioClientShareMode.Shared, attemptedFormats, "shared/float32");

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Could not get true {ChannelCount}-channel output from '{device.FriendlyName}' in any configuration tried " +
                    $"({string.Join("; ", attemptedFormats)}). This usually means the device isn't set to 8-channel output — " +
                    "check Windows Sound Control Panel → this device → Configure → 7.1 Surround. " +
                    "(Note: audio may still have appeared to play during these attempts — WASAPI can silently substitute a " +
                    "different channel count instead of rejecting the format outright, which is exactly the failure mode this check exists to catch.)");
            }

            (_wasapiOut, ActiveShareMode, ActiveFormat) = result.Value;

            // Mic capture is WASAPI regardless of which output backend is active (design doc
            // §5.5) — started only once the output format negotiation above has actually
            // succeeded, so a failed Start() doesn't leave a mic capture running with nothing to
            // pair it with. Either name may be null (no device available yet) and that mic's
            // channel is simply silent — see StreamerSampleProvider.
            if (testerMicDeviceName is not null) _testerMic.Start(testerMicDeviceName);
            if (boothMicDeviceName is not null) _boothMic.Start(boothMicDeviceName);
        }
        catch
        {
            _testerMic.Stop();
            _boothMic.Stop();
            device.Dispose();
            throw;
        }

        // Held for the lifetime of playback (WasapiOut keeps its own reference to it too) and
        // only released in Stop() — disposing it earlier can invalidate the audio client it handed out.
        _device = device;
    }

    private static (WasapiOut Out, AudioClientShareMode ShareMode, WaveFormat Format)? TryStart(
        MMDevice device, IWaveProvider waveProvider, AudioClientShareMode shareMode, List<string> attemptedFormats, string label)
    {
        // In shared mode, NAudio's post-Init() OutputWaveFormat isn't a reliable signal — it just
        // echoes back what we asked for rather than what the device's shared mix engine will
        // actually do with it (that conversion happens inside AutoConvertPcm, invisibly). The
        // device's own current mix format is the real answer, so check that directly, up front.
        if (shareMode == AudioClientShareMode.Shared)
        {
            using var audioClient = device.AudioClient;
            int mixChannels = audioClient.MixFormat.Channels;
            if (mixChannels != ChannelCount)
            {
                attemptedFormats.Add($"{label}: device's current shared mix format is {mixChannels}ch, not {ChannelCount}ch " +
                    "(Windows Sound Control Panel → this device → Configure controls this)");
                return null;
            }
        }

        var wasapiOut = new WasapiOut(device, shareMode, useEventSync: true, 100);
        try
        {
            wasapiOut.Init(waveProvider);

            // The critical check: Init() can "succeed" while having silently negotiated a
            // different channel count than we asked for. Don't trust it without verifying.
            if (wasapiOut.OutputWaveFormat.Channels != ChannelCount)
            {
                attemptedFormats.Add($"{label}: got {wasapiOut.OutputWaveFormat.Channels}ch instead of {ChannelCount}ch " +
                    $"({wasapiOut.OutputWaveFormat})");
                wasapiOut.Dispose();
                return null;
            }

            wasapiOut.Play();
            return (wasapiOut, shareMode, wasapiOut.OutputWaveFormat);
        }
        catch (Exception ex)
        {
            attemptedFormats.Add($"{label}: {ex.GetType().Name} — {ex.Message}");
            wasapiOut.Dispose();
            return null;
        }
    }

    public void Stop()
    {
        if (_wasapiOut is null) return;

        _wasapiOut.Stop();
        _wasapiOut.Dispose();
        _wasapiOut = null;
        ActiveShareMode = null;
        ActiveFormat = null;

        _testerMic.Stop();
        _boothMic.Stop();

        _device?.Dispose();
        _device = null;
    }

    public void Dispose()
    {
        Stop();
        _testerMic.Dispose();
        _boothMic.Dispose();
    }

    /// <summary>
    /// Wraps an <see cref="IWaveProvider"/> to report a different <see cref="WaveFormat"/> than
    /// its own — used to force WAVEFORMATEXTENSIBLE onto providers (like
    /// <see cref="SampleToWaveProvider16"/>) that always build their own plain format internally.
    /// Safe because WAVEFORMATEXTENSIBLE vs. plain WAVEFORMATEX doesn't change the actual
    /// interleaved sample byte layout — only the format descriptor used during device negotiation.
    /// </summary>
    private sealed class ExtensibleFormatOverride : IWaveProvider
    {
        private readonly IWaveProvider _inner;
        public WaveFormat WaveFormat { get; }

        public ExtensibleFormatOverride(IWaveProvider inner, WaveFormat format)
        {
            _inner = inner;
            WaveFormat = format;
        }

        public int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    }
}
