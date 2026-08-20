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
/// references <c>System.Windows.Forms</c> directly, so it stays compilable and testable against
/// plain fakes the same way it already is for <c>OppStreamer.Hardware</c> (see the project's
/// fake-hardware verification harness in the README).
///
/// <para><b>2026-08-19 — <c>Application.OleRequired()</c> added, to try to help the "Save Settings"
/// freeze.</b> With this window (or Mixer's) open, choosing "Save Settings" in the OPP MATLAB app —
/// which pops MATLAB's native Windows save dialog — hangs MATLAB hard enough to require force-quit.
/// Per Microsoft's own docs on hosting WinForms inside an unmanaged application's message loop
/// (which is exactly what showing this <c>Form</c> directly on MATLAB's calling thread is, per the
/// entry above): "the message loop provided by the [host] application is fundamentally different
/// from the Windows Forms message loop," and their two official mitigations — <c>ShowDialog()</c>
/// (blocks MATLAB's own UI the whole time this window is open — not acceptable, defeats the point
/// of a live companion window) and a dedicated thread (already proven dead under MATLAB's hosting,
/// see above) — don't work for us. <c>Application.OleRequired()</c> is the supported API for
/// ensuring a thread hosting WinForms UI is properly OLE/COM-initialized without needing
/// <c>Application.Run</c>; it's cheap and idempotent (checks the thread's current state before
/// doing anything), so there's no real downside to calling it here (left in place below). <b>Result:
/// confirmed NOT to fix the freeze</b> — see the 2026-08-19 entry after <see cref="SetVisible"/>
/// below for what was tried next and what actually fixed it, and the README's "Summary: the Save
/// Settings freeze" for the full, consolidated writeup.</para>
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

        // See the class doc comment's 2026-08-19 entry — cheap, idempotent, and the documented API
        // for making sure a thread hosting WinForms UI without Application.Run is properly
        // OLE/COM-initialized. Must happen before the Form below is constructed.
        Application.OleRequired();

        _view = new DiagnosticsView(isStreaming, waveforms);
        _view.Show();
    }

    /// <summary>
    /// Hides or re-shows the window without disposing it — everything (built plots, expanded/
    /// collapsed state) is preserved either way; showing again just makes it visible, no
    /// reconstruction. Added 2026-08-19 as the fallback for the Save Settings freeze once
    /// <c>Application.OleRequired()</c> alone didn't clear it: call <c>SetVisible(false)</c> right
    /// before MATLAB's native save dialog opens, and <c>SetVisible(true)</c> after it closes, so
    /// this window doesn't exist on-screen at the same moment the dialog does. Safe to call after
    /// Dispose() (a no-op) and from the same thread that constructed this, same as everything else
    /// here.
    ///
    /// <b>2026-08-19 — confirmed NOT sufficient for the Save Settings freeze.</b> Tested directly:
    /// hiding this window first did not stop MATLAB from freezing on the native save dialog. Calling
    /// <see cref="Dispose"/> first (full teardown, not just hiding) DID stop it — so the trigger is
    /// specifically a WinForms <c>Form</c>'s window handle existing on MATLAB's thread, not whether
    /// it's on-screen. This method is still real and still useful for "hide this window without
    /// losing its state" in any context that ISN'T "a native modal dialog is about to open" — just
    /// don't reach for it for that specific problem. See the README's "Summary: the Save Settings
    /// freeze" for the full writeup and what the actual fix ended up being (eliminating the Save
    /// Settings flow itself, per the decision made once this was understood).
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_disposed || _view.IsDisposed) return;
        _view.Visible = visible;
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
