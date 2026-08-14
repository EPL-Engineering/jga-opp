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
/// Stage 1 scope: only channels 2/3/4 are wired to real content (via StreamerEngine.RenderFrame).
/// Channels 5-7 (TTS, mic pass-through) are stubbed to silence here and get wired up in the
/// stages that add TtsPlayer and MicBridge — the channel layout is already correct so those
/// stages are additive, not a rework of this class.
/// </summary>
internal sealed class StreamerSampleProvider : ISampleProvider
{
    private const int ChannelCount = 8;
    private const int CaregiverChannel = 2, WaverChannel = 3, SubjectChannel = 4;

    private readonly StreamerEngine _engine;

    // Reused across Read() calls to avoid per-callback allocation on the audio thread.
    private float[] _caregiver = Array.Empty<float>();
    private float[] _waver = Array.Empty<float>();
    private float[] _subject = Array.Empty<float>();

    public StreamerSampleProvider(StreamerEngine engine, int sampleRate)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
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
        }

        _engine.RenderFrame(frameCount, _caregiver.AsSpan(0, frameCount), _waver.AsSpan(0, frameCount), _subject.AsSpan(0, frameCount));

        for (int i = 0; i < frameCount; i++)
        {
            int baseIndex = offset + i * ChannelCount;
            buffer[baseIndex + 0] = 0f; // reserved for the video player's own audio
            buffer[baseIndex + 1] = 0f;
            buffer[baseIndex + CaregiverChannel] = _caregiver[i];
            buffer[baseIndex + WaverChannel] = _waver[i];
            buffer[baseIndex + SubjectChannel] = _subject[i];
            buffer[baseIndex + 5] = 0f; // TTS — wired up in a later stage
            buffer[baseIndex + 6] = 0f; // Tester mic — wired up in a later stage
            buffer[baseIndex + 7] = 0f; // Booth mic — wired up in a later stage
        }

        return frameCount * ChannelCount;
    }
}
