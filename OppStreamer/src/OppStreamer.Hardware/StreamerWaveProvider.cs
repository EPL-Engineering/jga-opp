using System.Runtime.InteropServices;
using NAudio.Wave;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// Bridges <see cref="StreamerEngine"/> (hardware-independent core logic) to NAudio's pull-based
/// <see cref="IWaveProvider"/> model, which is what <c>AsioOut</c> calls on every ASIO callback.
///
/// Produces interleaved 8-channel IEEE float, matching the MOTU channel map from the design doc:
/// 0-1 silence, 2 Caregiver, 3 Waver, 4 Subject, 5 TTS, 6 Tester mic, 7 Booth mic.
///
/// Stage 1 scope: only channels 2/3/4 are wired to real content (via StreamerEngine.RenderFrame).
/// Channels 5-7 (TTS, mic pass-through) are stubbed to silence here and get wired up in the
/// stages that add TtsPlayer and MicBridge — the channel layout is already correct so those
/// stages are additive, not a rework of this class.
/// </summary>
internal sealed class StreamerWaveProvider : IWaveProvider
{
    private const int ChannelCount = 8;
    private const int CaregiverChannel = 2, WaverChannel = 3, SubjectChannel = 4;

    private readonly StreamerEngine _engine;

    // Reused across Read() calls to avoid per-callback allocation on the audio thread.
    private float[] _caregiver = Array.Empty<float>();
    private float[] _waver = Array.Empty<float>();
    private float[] _subject = Array.Empty<float>();

    public StreamerWaveProvider(StreamerEngine engine, int sampleRate)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, ChannelCount);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(byte[] buffer, int offset, int count)
    {
        const int bytesPerSample = sizeof(float);
        int frameCount = count / (bytesPerSample * ChannelCount);
        if (frameCount == 0) return 0;

        if (_caregiver.Length < frameCount)
        {
            _caregiver = new float[frameCount];
            _waver = new float[frameCount];
            _subject = new float[frameCount];
        }

        _engine.RenderFrame(frameCount, _caregiver.AsSpan(0, frameCount), _waver.AsSpan(0, frameCount), _subject.AsSpan(0, frameCount));

        var outSamples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(offset, frameCount * ChannelCount * bytesPerSample));
        for (int i = 0; i < frameCount; i++)
        {
            int baseIndex = i * ChannelCount;
            outSamples[baseIndex + 0] = 0f; // reserved for the video player's own audio
            outSamples[baseIndex + 1] = 0f;
            outSamples[baseIndex + CaregiverChannel] = _caregiver[i];
            outSamples[baseIndex + WaverChannel] = _waver[i];
            outSamples[baseIndex + SubjectChannel] = _subject[i];
            outSamples[baseIndex + 5] = 0f; // TTS — wired up in a later stage
            outSamples[baseIndex + 6] = 0f; // Tester mic — wired up in a later stage
            outSamples[baseIndex + 7] = 0f; // Booth mic — wired up in a later stage
        }

        return frameCount * ChannelCount * bytesPerSample;
    }
}
