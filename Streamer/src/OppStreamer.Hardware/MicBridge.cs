using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OppStreamer.Core;

namespace OppStreamer.Hardware;

/// <summary>
/// Captures one WASAPI input device (a USB mic — Tester or Booth) and feeds it into a
/// <see cref="DriftCompensatedRingBuffer"/> (Core) that the render callback drains from on channel
/// 6 or 7. See design doc §5.5 and README "Development without the MOTU".
///
/// Capture and render run on independent, unsynchronized clocks — separate USB devices/drivers,
/// separate hardware crystals, and (in the WASAPI-transport case) a separate device entirely from
/// the one being rendered to. The point of DriftCompensatedRingBuffer is to absorb that drift
/// rather than accumulate latency or starve. This class's job is just: get whatever bytes WASAPI
/// hands us on its own capture thread into mono float @ 48kHz and hand them to the ring buffer's
/// Write() — the ring buffer does the actual drift correction, entirely on the Read() side.
/// </summary>
public sealed class MicBridge : IDisposable
{
    private const int TargetSampleRate = 48_000;

    // ~500ms at 48kHz — generous headroom for drift correction to work with. DriftCompensatedRing-
    // Buffer's default targetFillFraction (0.5) keeps the render side reading from roughly the
    // middle of this, so there's ~250ms of slack on both the underrun and overflow side before
    // either one is a real risk — comfortably more than any realistic USB-clock drift or capture
    // callback jitter needs.
    private const int RingBufferCapacitySamples = TargetSampleRate / 2;

    // Native-rate frames of bufferedInput's backlog that OnDataAvailable always leaves untouched
    // before computing how much to pull from the resampler/converter chain. ~0.7-1.5ms depending
    // on native rate - negligible next to the ring buffer's own ~250ms of latency, but essential:
    // see the long comment in OnDataAvailable for why this exists and what happens without it.
    private const int SafetyMarginNativeFrames = 64;

    private readonly DriftCompensatedRingBuffer _ring = new(RingBufferCapacitySamples);

    private WasapiCapture? _capture;
    private MMDevice? _device;
    private BufferedWaveProvider? _bufferedInput;
    private ISampleProvider? _monoResampled;
    private float[] _pullBuffer = Array.Empty<float>();

    public bool IsRunning => _capture is not null;

    /// <summary>Diagnostics passthrough — see <see cref="DriftCompensatedRingBuffer"/>.</summary>
    public long UnderrunSampleCount => _ring.UnderrunSampleCount;

    /// <summary>Diagnostics passthrough — see <see cref="DriftCompensatedRingBuffer"/>.</summary>
    public long OverflowSampleCount => _ring.OverflowSampleCount;

    /// <summary>Diagnostics passthrough — see <see cref="DriftCompensatedRingBuffer"/>.</summary>
    public double CurrentFillLevel => _ring.CurrentFillLevel;

    /// <summary>Friendly names of active WASAPI capture devices currently visible to Windows.</summary>
    public static IReadOnlyList<string> EnumerateDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => d.FriendlyName)
            .ToList();
    }

    public void Start(string deviceName)
    {
        if (_capture is not null)
            throw new InvalidOperationException("Already running — call Stop() before starting again with a different device.");

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .FirstOrDefault(d => d.FriendlyName == deviceName)
            ?? throw new ArgumentException($"No active WASAPI capture device named '{deviceName}'. " +
                $"Available: {string.Join(", ", EnumerateDevices())}", nameof(deviceName));

        WasapiCapture? capture = null;
        try
        {
            capture = new WasapiCapture(device, useEventSync: true, audioBufferMillisecondsLength: 50);

            // BufferedWaveProvider's WaveFormat must describe the actual byte layout DataAvailable
            // hands us, which is the capture's native mix format. capture.WaveFormat (the public
            // getter) already converts a WaveFormatExtensible mix format down to a plain
            // PCM/IeeeFloat WaveFormat where possible — see NAudio's WaveFormatExtensible.
            // ToStandardWaveFormat, which is what that getter calls internally — because
            // ToSampleProvider() below only accepts a plain-encoded source. Same
            // Encoding-must-be-plain requirement StreamerSampleProvider's comment describes on the
            // output side; this is the capture-side mirror of it.
            var bufferedInput = new BufferedWaveProvider(capture.WaveFormat)
            {
                // The capture callback thread must never block, so this must never throw — drop
                // oldest audio instead. A full second of buffer is far more slack than the
                // DataAvailable handler below should ever need to drain it.
                DiscardOnBufferOverflow = true,

                // Belt-and-suspenders, not the actual fix (see OnDataAvailable's long comment for
                // that): OnDataAvailable is now written to never ask for more output than
                // SafetyMarginNativeFrames' worth of confirmed backlog can supply, so a short read
                // shouldn't happen in normal operation. If one ever did anyway (e.g. a real
                // WASAPI-side hiccup this margin didn't anticipate), false is still the right
                // setting: it returns genuinely fewer samples instead of zero-padding, which is the
                // more honest failure mode even though — as the investigation into the original bug
                // report found the hard way — a short read into WdlResamplingSampleProvider isn't
                // actually "safe" either way (see below). The real protection is not triggering one.
                ReadFully = false,
                BufferDuration = TimeSpan.FromSeconds(1),
            };

            ISampleProvider mono = ToMono(bufferedInput.ToSampleProvider());
            ISampleProvider monoResampled = mono.WaveFormat.SampleRate == TargetSampleRate
                ? mono
                : new WdlResamplingSampleProvider(mono, TargetSampleRate);

            // Hooked before StartRecording() since that call is what spins up the capture thread
            // that will actually invoke these — but the fields OnDataAvailable reads are assigned
            // only after StartRecording() succeeds below, so a failure there can't leave this
            // object looking "running" (IsRunning true) with an already-disposed capture. Any
            // packet that arrives in that narrow window is simply dropped by OnDataAvailable's own
            // null-guard — harmless, same as an ordinary startup underrun.
            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += OnRecordingStopped;

            capture.StartRecording();

            _capture = capture;
            _device = device;
            _bufferedInput = bufferedInput;
            _monoResampled = monoResampled;
        }
        catch
        {
            // capture may or may not have been constructed yet (e.g. ToSampleProvider() can throw
            // for an unsupported capture encoding before StartRecording() is ever reached) — only
            // dispose it if it exists, but always dispose the device either way, same leak-safety
            // pattern as AsioStreamerOutput/WasapiStreamerOutput's Start().
            if (capture is not null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
            }
            device.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Runs on WasapiCapture's own dedicated capture thread (see NAudio's WasapiCapture.
    /// StartRecording), never the audio render thread — <see cref="DriftCompensatedRingBuffer.Write"/>
    /// is the only thing here that has to be safe to call concurrently with the render thread's
    /// Read(), and its internal lock guarantees that.
    /// </summary>
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var capture = _capture;
        var bufferedInput = _bufferedInput;
        var monoResampled = _monoResampled;
        if (capture is null || bufferedInput is null || monoResampled is null)
            return; // Stop() raced us; drop this packet.

        bufferedInput.AddSamples(e.Buffer, 0, e.BytesRecorded);

        // How much to pull out (at the *output* sample rate) this callback. An earlier version of
        // this method estimated "roughly what was just added" (nativeFrames * rate, rounded up
        // with a small margin) and asked WdlResamplingSampleProvider for that — which sounds
        // reasonable, but turned out to be a real bug, confirmed by reproducing it in a standalone
        // harness against the real NAudio source and reading WdlResampler's own doc comment: a
        // short read (asking for more than is genuinely available) doesn't just return fewer
        // samples cleanly — NAudio's own ResampleOut docs say a short read triggers an internal
        // "flush", which is meant for *end of stream*, not for "keep going normally next call". Bad
        // filter-history samples were then interpolated across, producing several samples of
        // audible garbage (in the reproduction, some even exceeded the input signal's own range) —
        // exactly the "discontinuities" symptom this was chasing, once every capture callback,
        // because the old margin's over-ask made a short read the NORMAL case, not a rare edge case.
        //
        // The fix: never ask for more than a confirmed, comfortably-margined amount of REAL backlog
        // already sitting in bufferedInput — not an estimate of what should be there. Reading
        // bufferedInput.BufferedBytes directly (not recomputing our own running estimate) avoids
        // any chance of the two ever disagreeing by even one sample. SafetyMarginNativeFrames of
        // backlog is deliberately always left untouched as a cushion. Verified glitch-free against
        // the real NAudio resampler over a 5-minute simulated capture/render run, with capture
        // packet timing jittered well beyond anything real WASAPI hardware should ever produce.
        int availableNativeFrames = bufferedInput.BufferedBytes / capture.WaveFormat.BlockAlign;
        int safeNativeFrames = Math.Max(0, availableNativeFrames - SafetyMarginNativeFrames);
        int outputFrames = (int)Math.Floor(safeNativeFrames * (double)TargetSampleRate / capture.WaveFormat.SampleRate);
        if (outputFrames == 0)
            return; // not enough backlog yet to safely pull anything this callback

        if (_pullBuffer.Length < outputFrames)
            _pullBuffer = new float[outputFrames];

        int read = monoResampled.Read(_pullBuffer, 0, outputFrames);
        if (read > 0)
            _ring.Write(_pullBuffer.AsSpan(0, read));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // NAudio raises this on capture-thread teardown, including unexpected device loss (e.g.
        // unplugged mid-session) — nothing to do here today beyond not throwing. Stop()/Dispose()
        // handle the orderly-shutdown path. e.Exception is non-null on the device-loss path, if a
        // future stage wants to surface that to ConfigApi/DiagnosticsView.
    }

    /// <summary>Drains drift-corrected mono float samples for the render callback (channel 6 or 7).</summary>
    public void Read(Span<float> destination) => _ring.Read(destination);

    public void Stop()
    {
        if (_capture is null) return;

        _capture.DataAvailable -= OnDataAvailable;
        _capture.RecordingStopped -= OnRecordingStopped;
        _capture.StopRecording();
        _capture.Dispose();
        _capture = null;

        _bufferedInput = null;
        _monoResampled = null;

        _device?.Dispose();
        _device = null;
    }

    public void Dispose() => Stop();

    /// <summary>
    /// Averages however many channels the capture device natively has down to mono — a generic
    /// N-channel version of NAudio's own StereoToMonoSampleProvider (which only accepts exactly 2
    /// channels). Most USB mics are already mono (this is then just a passthrough) or stereo; this
    /// also covers the unusual case of a multichannel capture device without a special case.
    /// </summary>
    private static ISampleProvider ToMono(ISampleProvider source)
        => source.WaveFormat.Channels == 1 ? source : new DownmixToMonoSampleProvider(source);

    private sealed class DownmixToMonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _channels;
        private float[] _sourceBuffer = Array.Empty<float>();

        public DownmixToMonoSampleProvider(ISampleProvider source)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int needed = count * _channels;
            if (_sourceBuffer.Length < needed) _sourceBuffer = new float[needed];

            int sourceRead = _source.Read(_sourceBuffer, 0, needed);
            int frames = sourceRead / _channels;

            for (int frame = 0; frame < frames; frame++)
            {
                float sum = 0f;
                int baseIndex = frame * _channels;
                for (int ch = 0; ch < _channels; ch++) sum += _sourceBuffer[baseIndex + ch];
                buffer[offset + frame] = sum / _channels;
            }

            return frames;
        }
    }
}
