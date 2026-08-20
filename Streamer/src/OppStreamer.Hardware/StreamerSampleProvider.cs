using NAudio.Wave;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// Bridges <see cref="StreamerEngine"/> (hardware-independent core logic) to NAudio as a plain
/// <see cref="ISampleProvider"/> — the canonical float representation the design doc calls for
/// (§5.9): the core pipeline works in 32-bit float throughout, and each transport backend
/// (<see cref="AsioStreamerOutput"/>, <see cref="WasapiStreamerOutput"/>) adapts that to whatever
/// format its device actually needs, using NAudio's own conversion providers. This class itself
/// doesn't know or care whether it ends up feeding ASIO or WASAPI, exclusive mode or shared,
/// float or 16-bit PCM — that's entirely the transport's concern.
///
/// Produces interleaved 8-channel float, matching the MOTU channel map from the design doc:
/// 0-1 silence, 2 Caregiver, 3 Waver, 4 Subject, 5 TTS, 6 Tester mic, 7 Booth mic.
///
/// All content channels are now wired to real sources: 2/3/4 via StreamerEngine.RenderFrame, 5
/// (TTS) via StreamerEngine.RenderTts (backed by TtsPlayer — no loop-boundary latch, just a
/// FIFO), and 6/7 via the two MicBridge instances passed in. The mic bridges are always read
/// unconditionally, whether or not their underlying device was ever Start()ed: an unstarted (or
/// not-yet-caught-up) MicBridge's ring buffer just reports a permanent underrun and yields
/// silence on Read(), so an omitted mic device degrades to a silent channel — no null-checking
/// needed here. TtsPlayer degrades the same way when nothing's been queued yet.
/// </summary>
/// <summary>
/// Index order (and matching display names) for <see cref="WaveformMonitor"/>'s compact
/// channel numbering used by <see cref="StreamerSampleProvider"/> — deliberately NOT the same
/// numbering as the interleaved 8-channel wire format's channel indices (0-7, with 0-1 reserved):
/// this only covers the six channels that ever carry real content, worth plotting in
/// DiagnosticsView (design doc §5.7). Shared here so both <see cref="StreamerSampleProvider"/>
/// (the writer) and whichever <see cref="IStreamerAudioOutput"/> constructs the
/// <see cref="WaveformMonitor"/> (the names list) agree on the same order.
/// </summary>
internal static class MonitoredChannel
{
    public const int Caregiver = 0, Waver = 1, Subject = 2, Tts = 3, TesterMic = 4, BoothMic = 5;

    public static readonly string[] Names = { "Caregiver", "Waver", "Subject", "TTS", "Tester Mic", "Booth Mic" };
}

internal sealed class StreamerSampleProvider : ISampleProvider
{
    private const int ChannelCount = 8;
    private const int CaregiverChannel = 2, WaverChannel = 3, SubjectChannel = 4;
    private const int TesterMicChannel = 6, BoothMicChannel = 7;

    private readonly StreamerEngine _engine;
    private readonly MicBridge _testerMic;
    private readonly MicBridge _boothMic;
    private readonly WaveformMonitor _waveforms;

    // Reused across Read() calls to avoid per-callback allocation on the audio thread.
    private float[] _caregiver = Array.Empty<float>();
    private float[] _waver = Array.Empty<float>();
    private float[] _subject = Array.Empty<float>();
    private float[] _testerMicFrame = Array.Empty<float>();
    private float[] _boothMicFrame = Array.Empty<float>();
    private float[] _ttsFrame = Array.Empty<float>();

    public StreamerSampleProvider(StreamerEngine engine, int sampleRate, MicBridge testerMic, MicBridge boothMic, WaveformMonitor waveforms)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _testerMic = testerMic ?? throw new ArgumentNullException(nameof(testerMic));
        _boothMic = boothMic ?? throw new ArgumentNullException(nameof(boothMic));
        _waveforms = waveforms ?? throw new ArgumentNullException(nameof(waveforms));

        // Deliberately the plain (non-extensible) IEEE float format, not WaveFormatExtensible —
        // NAudio's SampleToWaveProvider/SampleToWaveProvider16 both require their *source*
        // (this) to report Encoding == IeeeFloat and throw ArgumentException otherwise; a plain
        // WaveFormatExtensible's Encoding is Extensible, not IeeeFloat, since the actual sample
        // type lives in its subFormat field instead. WASAPI exclusive mode's WAVEFORMATEXTENSIBLE
        // requirement for >2 channels is handled downstream, at the transport boundary
        // (WasapiStreamerOutput.ExtensibleFormatOverride) — applied to the already-converted
        // IWaveProvider, not here at the shared source both transports build from.
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, ChannelCount);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        int frameCount = count / ChannelCount;
        if (frameCount == 0) return 0;

        if (_caregiver.Length < frameCount)
        {
            _caregiver = new float[frameCount];
            _waver = new float[frameCount];
            _subject = new float[frameCount];
            _testerMicFrame = new float[frameCount];
            _boothMicFrame = new float[frameCount];
            _ttsFrame = new float[frameCount];
        }

        _engine.RenderFrame(frameCount, _caregiver.AsSpan(0, frameCount), _waver.AsSpan(0, frameCount), _subject.AsSpan(0, frameCount));

        // Independent of RenderFrame's shared-cursor latch mechanism entirely — the mic bridges
        // are always-on passthroughs (design doc §5.5: "no mute/gating logic needed"), not gated by
        // trial state or loop boundaries. TTS (channel 5) is the same: TtsPlayer has no
        // loop-boundary latch either, it just drains its FIFO as fast as this callback asks.
        _testerMic.Read(_testerMicFrame.AsSpan(0, frameCount));
        _boothMic.Read(_boothMicFrame.AsSpan(0, frameCount));
        _engine.RenderTts(_ttsFrame.AsSpan(0, frameCount));

        // Feeds DiagnosticsView (design doc §5.7's "publishes a decimated snapshot of the frame")
        // — cheap running min/max bucketing, see WaveformMonitor's own doc comment for why this is
        // safe to do unconditionally on the audio thread every callback.
        _waveforms.Accumulate(MonitoredChannel.Caregiver, _caregiver.AsSpan(0, frameCount));
        _waveforms.Accumulate(MonitoredChannel.Waver, _waver.AsSpan(0, frameCount));
        _waveforms.Accumulate(MonitoredChannel.Subject, _subject.AsSpan(0, frameCount));
        _waveforms.Accumulate(MonitoredChannel.Tts, _ttsFrame.AsSpan(0, frameCount));
        _waveforms.Accumulate(MonitoredChannel.TesterMic, _testerMicFrame.AsSpan(0, frameCount));
        _waveforms.Accumulate(MonitoredChannel.BoothMic, _boothMicFrame.AsSpan(0, frameCount));

        for (int i = 0; i < frameCount; i++)
        {
            int baseIndex = offset + i * ChannelCount;
            buffer[baseIndex + 0] = 0f; // reserved for the video player's own audio
            buffer[baseIndex + 1] = 0f;
            buffer[baseIndex + CaregiverChannel] = _caregiver[i];
            buffer[baseIndex + WaverChannel] = _waver[i];
            buffer[baseIndex + SubjectChannel] = _subject[i];
            buffer[baseIndex + 5] = _ttsFrame[i];
            buffer[baseIndex + TesterMicChannel] = _testerMicFrame[i];
            buffer[baseIndex + BoothMicChannel] = _boothMicFrame[i];
        }

        return frameCount * ChannelCount;
    }
}
