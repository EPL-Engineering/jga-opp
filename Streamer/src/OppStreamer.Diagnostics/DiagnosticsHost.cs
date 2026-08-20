using System.Windows.Forms;
using OppStreamer.Core;

namespace OppStreamer.Diagnostics;

/// <summary>
/// Owns <see cref="DiagnosticsView"/>'s lifecycle: constructs it and shows it, on whatever thread
/// calls the constructor; closes it on <see cref="Dispose"/>, same thread.
///
/// <para><b>2026-08-18 — simplified from an earlier, over-cautious design.</b> The original
/// version of this class ran <see cref="DiagnosticsView"/> on its own dedicated STA thread with
/// its own <c>Application.Run</c> message loop, on the theory that MATLAB's own calling thread
/// might not pump Windows messages for a window it didn't create. That theory turned out to be
/// wrong for this environment, and the evidence is decisive: OPP's own existing
/// <c>OPP.Mixer.Mixer.Open()</c> does exactly <c>_mixerPanel = new MixerPanel(); _mixerPanel.Show();</c>
/// — no thread, no <c>Application.Run</c> — and that panel is fully live and interactive from
/// MATLAB (sliders drag, mutes toggle). That's proof MATLAB's calling thread already runs a
/// message loop adequate for a WinForms window shown directly on it, timers included. The
/// dedicated-thread version, meanwhile, never worked under MATLAB at all — not even a plain
/// <c>MessageBox.Show()</c> called from that separate thread ever appeared, while the exact same
/// call succeeds trivially on MATLAB's own thread. So the extra thread wasn't just unnecessary,
/// it was actively the problem: something about a *new* thread this code spins up itself doesn't
/// get serviced under MATLAB's hosting, even though thread creation itself doesn't error. This
/// version just does what <c>MixerPanel</c> already does, matching working, in-house prior art
/// instead of general-purpose caution that didn't hold up here.</para>
///
/// This is still the entire public surface of this project — <c>ConfigApi.cs</c> itself never
/// references <c>System.Windows.Forms</c> or ScottPlot directly, so it stays compilable and
/// testable against plain fakes the same way it already is for <c>OppStreamer.Hardware</c> (see
/// the project's fake-hardware verification harness in the README).
/// </summary>
public sealed class DiagnosticsHost : IDisposable
{
    private readonly DiagnosticsView _view;
    private bool _disposed;

    /// <param name="isStreaming">Polled each redraw tick for the running/stopped indicator — pass e.g. <c>() =&gt; configApi.IsStreaming</c>.</param>
    /// <param name="waveforms">
    /// Polled each redraw tick for the data to plot — pass e.g. <c>() =&gt; currentOutput?.Waveforms</c>.
    /// May return null (nothing configured/started yet); the view shows a placeholder until it
    /// first returns non-null.
    /// </param>
    public DiagnosticsHost(Func<bool> isStreaming, Func<WaveformMonitor?> waveforms)
    {
        if (isStreaming is null) throw new ArgumentNullException(nameof(isStreaming));
        if (waveforms is null) throw new ArgumentNullException(nameof(waveforms));

        _view = new DiagnosticsView(isStreaming, waveforms);
        _view.Show();
    }

    /// <summary>Closes the window. Safe to call more than once. Must be called from the same thread that constructed this.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_view.IsDisposed)
            _view.Close();
    }
}
