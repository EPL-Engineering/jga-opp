using System;
using System.Collections.Generic;
using System.Linq;

namespace OppStreamer.Core;

/// <summary>
/// A lightweight, real-time-safe "oscilloscope" recorder for DiagnosticsView (design doc §5.7):
/// per named channel, incrementally decimates an incoming sample stream into a scrolling history
/// of (min, max) buckets — cheap enough to run on the audio thread every callback, with no
/// allocation after construction on that hot path.
///
/// Deliberately hardware/UI-agnostic (same "no dependency on NAudio, ASIO, or any real audio
/// device" charter as the rest of Core) so both the audio-producing side (Hardware, which calls
/// <see cref="Accumulate"/> from inside <c>StreamerSampleProvider.Read</c>) and the UI-consuming
/// side (Diagnostics, which calls <see cref="GetSnapshot"/> from a redraw timer) can share this
/// type without depending on each other.
///
/// <para><b>Threading, and a deliberate simplification vs. the design doc's literal wording:</b>
/// §5.7 describes "a double-buffered handoff" specifically to avoid "shared-state locking
/// headaches." This class does use a lock — but only around <see cref="Commit"/> (once per
/// finished bucket, e.g. every ~10ms, not every sample) and <see cref="GetSnapshot"/> (once per UI
/// redraw tick, ~10-20Hz per §5.7). That's the exact same shape <see cref="PendingChangeQueue"/>
/// already uses elsewhere in Core: a lock held only at infrequent hand-off points, never on the
/// per-sample hot path. A real lock-free double buffer would also work, but at these rates (tens
/// of hertz, not tens of thousands) the extra complexity isn't buying anything a short lock
/// doesn't already give for free — this achieves the same goal (the UI thread and the audio thread
/// never block each other for more than a few array-index writes) more simply.</para>
/// </summary>
public sealed class WaveformMonitor
{
    private readonly object _gate = new();
    private readonly string[] _channelNames;
    private readonly int _bucketsPerChannel;
    private readonly int _samplesPerBucket;

    // Audio-thread-only working state — the bucket currently being accumulated. Never touched
    // from GetSnapshot, so no lock needed here.
    private readonly float[] _liveMin;
    private readonly float[] _liveMax;
    private readonly int[] _liveCount;

    // Shared, lock-protected committed history. Flat [channel * bucketsPerChannel + bucket]
    // layout — avoids a jagged array's extra allocation/indirection per channel.
    private readonly float[] _committedMin;
    private readonly float[] _committedMax;
    private readonly int[] _writeIndex;   // next slot to write, per channel
    private readonly int[] _filledCount;  // how many buckets hold real data so far (ramps up to bucketsPerChannel, then stays there)

    /// <summary>Channel names, in the index order <see cref="Accumulate"/>/<see cref="GetSnapshot"/> expect.</summary>
    public IReadOnlyList<string> ChannelNames => _channelNames;

    /// <summary>The fixed history length (in buckets) every channel scrolls through.</summary>
    public int BucketsPerChannel => _bucketsPerChannel;

    /// <param name="channelNames">Display names, in index order — e.g. ["Caregiver", "Waver", ...].</param>
    /// <param name="samplesPerBucket">
    /// How many raw samples get decimated into one (min, max) bucket — e.g. 480 at a 48kHz sample
    /// rate for a 10ms bucket. Smaller = finer time resolution but more buckets needed to cover
    /// the same wall-clock window; larger = coarser but a longer history fits in the same memory.
    /// </param>
    /// <param name="bucketsPerChannel">
    /// How many buckets of history to keep per channel (the scrolling window's total length) —
    /// e.g. 500 buckets x 10ms/bucket = 5 seconds of visible history.
    /// </param>
    public WaveformMonitor(IReadOnlyList<string> channelNames, int samplesPerBucket, int bucketsPerChannel)
    {
        if (channelNames is null || channelNames.Count == 0)
            throw new ArgumentException("At least one channel name is required.", nameof(channelNames));
        if (samplesPerBucket <= 0)
            throw new ArgumentOutOfRangeException(nameof(samplesPerBucket), "Must be a positive sample count.");
        if (bucketsPerChannel <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketsPerChannel), "Must be a positive bucket count.");

        _channelNames = channelNames.ToArray();
        _samplesPerBucket = samplesPerBucket;
        _bucketsPerChannel = bucketsPerChannel;

        int n = _channelNames.Length;
        _liveMin = new float[n];
        _liveMax = new float[n];
        _liveCount = new int[n];
        for (int i = 0; i < n; i++)
        {
            _liveMin[i] = float.PositiveInfinity;
            _liveMax[i] = float.NegativeInfinity;
        }

        _committedMin = new float[n * bucketsPerChannel];
        _committedMax = new float[n * bucketsPerChannel];
        _writeIndex = new int[n];
        _filledCount = new int[n];
    }

    /// <summary>
    /// Feeds a chunk of samples for one channel — called from the audio thread, once per channel
    /// per render callback. Updates whatever bucket is currently in progress, committing it (and
    /// starting a fresh one) each time <c>samplesPerBucket</c> samples have been seen since the
    /// last commit. If <paramref name="samples"/> is longer than a single bucket's worth, more
    /// than one bucket can be committed within this one call — handled in a loop, same approach
    /// <see cref="StimulusStore.Advance"/> uses for multiple loop wraps in a single render call.
    /// </summary>
    public void Accumulate(int channelIndex, ReadOnlySpan<float> samples)
    {
        CheckChannel(channelIndex);

        float min = _liveMin[channelIndex];
        float max = _liveMax[channelIndex];
        int count = _liveCount[channelIndex];

        foreach (float sample in samples)
        {
            if (sample < min) min = sample;
            if (sample > max) max = sample;
            count++;

            if (count >= _samplesPerBucket)
            {
                Commit(channelIndex, min, max);
                min = float.PositiveInfinity;
                max = float.NegativeInfinity;
                count = 0;
            }
        }

        _liveMin[channelIndex] = min;
        _liveMax[channelIndex] = max;
        _liveCount[channelIndex] = count;
    }

    private void Commit(int channelIndex, float min, float max)
    {
        lock (_gate)
        {
            int slot = channelIndex * _bucketsPerChannel + _writeIndex[channelIndex];
            _committedMin[slot] = min;
            _committedMax[slot] = max;
            _writeIndex[channelIndex] = (_writeIndex[channelIndex] + 1) % _bucketsPerChannel;
            if (_filledCount[channelIndex] < _bucketsPerChannel) _filledCount[channelIndex]++;
        }
    }

    /// <summary>
    /// Returns a copy of one channel's current scrolling history, oldest bucket first — safe to
    /// call from the UI thread at any rate (e.g. a 10-20Hz redraw timer per design doc §5.7).
    /// <paramref name="min"/>/<paramref name="max"/> come back sized to exactly however many
    /// buckets have real data so far — 0 immediately after construction, ramping up to <see
    /// cref="BucketsPerChannel"/> as history accumulates — rather than padded with zeros that
    /// would misleadingly plot as silence before any audio has actually played.
    /// </summary>
    public void GetSnapshot(int channelIndex, out float[] min, out float[] max)
    {
        CheckChannel(channelIndex);

        lock (_gate)
        {
            int filled = _filledCount[channelIndex];
            min = new float[filled];
            max = new float[filled];

            int start = filled < _bucketsPerChannel ? 0 : _writeIndex[channelIndex];
            for (int i = 0; i < filled; i++)
            {
                int slot = channelIndex * _bucketsPerChannel + (start + i) % _bucketsPerChannel;
                min[i] = _committedMin[slot];
                max[i] = _committedMax[slot];
            }
        }
    }

    private void CheckChannel(int channelIndex)
    {
        if ((uint)channelIndex >= (uint)_channelNames.Length)
            throw new ArgumentOutOfRangeException(nameof(channelIndex));
    }
}
