using System.Windows.Forms;
using OppStreamer.Core;

namespace OppStreamer.Diagnostics;

/// <summary>
/// The actual window (design doc §5.7): a running/stopped indicator plus a stacked real-time plot,
/// one row per channel <see cref="WaveformMonitor"/> reports. Internal — <see cref="DiagnosticsHost"/>
/// is this project's only public surface; see its doc comment for why.
///
/// Deliberately generic over whatever channels the supplied <see cref="WaveformMonitor"/> has
/// (read from <see cref="WaveformMonitor.ChannelNames"/> the first time one becomes available)
/// rather than hardcoding OppStreamer.Hardware's specific six-channel list — keeps this project
/// referencing only OppStreamer.Core, no dependency on Hardware at all.
///
/// <para><b>2026-08-18 — plotting switched from ScottPlot to hand-rolled GDI+.</b> The window and
/// the streaming indicator both worked correctly under MATLAB, but the first redraw tick that
/// touched ScottPlot threw <c>TypeInitializationException: The type initializer for
/// 'ScottPlot.Fonts' threw an exception</c>, with a <c>FileNotFoundException</c> for
/// <c>System.Runtime.CompilerServices.Unsafe</c> at the bottom of the chain — and kept re-throwing
/// it every tick (expected .NET behavior: a failed static constructor is cached and rethrown for
/// the process's lifetime). Neither a <c>dotnet publish</c> output folder nor switching MATLAB to
/// <c>dotnetenv("core")</c> fixed it, and switching to <c>"core"</c> isn't an option here anyway —
/// MATLAB is deliberately hosted under the .NET Framework CLR on this project, to avoid separate
/// issues that came up trying to pin a <c>dotnetenv("core")</c> version. That's the actual root
/// cause: ScottPlot 5.x depends on SkiaSharp, which pulls in modern-.NET-only BCL pieces that plain
/// .NET Framework hosting cannot resolve — not a missing file, a runtime mismatch no amount of
/// republishing can fix. So this class no longer uses ScottPlot at all: <see cref="WaveformPanel"/>
/// below draws the min/max envelope itself with plain GDI+ (<c>System.Drawing</c>), which is part
/// of the BCL under both .NET Framework and .NET Core and therefore doesn't care which one MATLAB
/// is hosting.</para>
///
/// <para><b>2026-08-18 (later still) — collapsible plot, matching the old LabVIEW streamer window.</b>
/// The Tester shouldn't be able to see the waveforms in normal operation — the plot visibly shows
/// when a probe is present, which is exactly the thing the Tester isn't supposed to know ahead of
/// time. The old LabVIEW window handled this by starting small (indicator + a toggle button only)
/// and only growing to reveal the plot when explicitly expanded. This window now does the same:
/// <see cref="SetPlotVisible"/> toggles both the window size (<see cref="CollapsedSize"/> /
/// <see cref="ExpandedSize"/>) and whether <see cref="Redraw"/> does any plotting work at all — the
/// point isn't just hiding the plot, it's not paying for it while it's hidden, same as the original.</para>
/// </summary>
internal sealed class DiagnosticsView : Form
{
    private const int TargetRedrawHz = 15;

    private static readonly System.Drawing.Size CollapsedSize = new(260, 40);
    private static readonly System.Drawing.Size ExpandedSize = new(500, 500);

    private readonly Func<bool> _isStreaming;
    private readonly Func<WaveformMonitor?> _waveforms;
    private readonly System.Windows.Forms.Timer _redrawTimer;

    private readonly Panel _statusDot;
    private readonly Label _statusLabel;
    private readonly Button _toggleButton;
    private readonly TableLayoutPanel _channelsPanel;
    private readonly Label _placeholderLabel;

    private bool _plotVisible;

    private WaveformPanel[]? _plots;
    private string[]? _plotChannelNames;

    // Belt-and-suspenders: nothing in WaveformPanel's own Paint handler should throw (it's plain
    // GDI+ over arrays we control), but if it ever does, disable it once rather than let a WinForms
    // Paint exception potentially recur every redraw tick / repaint. See DisablePlotting.
    private bool _plottingDisabled;

    public DiagnosticsView(Func<bool> isStreaming, Func<WaveformMonitor?> waveforms)
    {
        _isStreaming = isStreaming ?? throw new ArgumentNullException(nameof(isStreaming));
        _waveforms = waveforms ?? throw new ArgumentNullException(nameof(waveforms));

        Text = "Streamer";
        RestoreLastPosition();
        // Fixed, non-maximizable: the whole point of starting collapsed is that the Tester can't
        // see the plot by default — don't let them get there by dragging the window bigger either.
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ShowIcon = false;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);

        // --- Row 0: running/stopped indicator (left) + show/hide plot toggle (right). Always
        // visible, at both window sizes — this is the whole point of the collapsed state. ---
        var statusBar = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var statusFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(8, 6, 0, 0) };
        _statusDot = new Panel { Width = 14, Height = 14, Margin = new Padding(0, 2, 6, 0) };
        _statusDot.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(_statusDot.BackColor);
            e.Graphics.FillEllipse(brush, 0, 0, _statusDot.Width - 1, _statusDot.Height - 1);
        };
        _statusLabel = new Label { AutoSize = true, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) };
        statusFlow.Controls.Add(_statusDot);
        statusFlow.Controls.Add(_statusLabel);
        statusBar.Controls.Add(statusFlow, 0, 0);

        _toggleButton = new Button
        {
            Text = "Show Plot",
            AutoSize = false,
            Width = 90,
            Height = 26,
            Margin = new Padding(0, 4, 6, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _toggleButton.Click += (_, _) => SetPlotVisible(!_plotVisible);
        statusBar.Controls.Add(_toggleButton, 1, 0);

        root.Controls.Add(statusBar, 0, 0);

        // --- Row 1: stacked plots, one per channel — populated lazily once a WaveformMonitor is
        // available (its channel names aren't known before that). Starts showing a placeholder.
        // Only ever visible/drawn when the Tester has explicitly expanded the window — see
        // SetPlotVisible and Redraw. ---
        _channelsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        _placeholderLabel = new Label
        {
            Text = "Waiting for SetConfig()/Start() — no waveform data yet.",
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            ForeColor = System.Drawing.Color.Gray,
        };
        _channelsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        _channelsPanel.Controls.Add(_placeholderLabel, 0, 0);
        root.Controls.Add(_channelsPanel, 0, 1);

        SetStreamingIndicator(streaming: false); // initial state before the first tick
        SetPlotVisible(false); // starts collapsed — indicator + toggle only, same as the old LabVIEW window

        _redrawTimer = new System.Windows.Forms.Timer { Interval = 1000 / TargetRedrawHz };
        _redrawTimer.Tick += (_, _) => Redraw();
        _redrawTimer.Start();

        this.FormClosing += DiagnosticsView_FormClosing;
    }

    private void DiagnosticsView_FormClosing(object sender, FormClosingEventArgs e)
    {
        Settings.LastPosition = new Rectangle(Location, Size);
    }

    private void RestoreLastPosition()
    {
        if (!Settings.LastPosition.IsEmpty)
        {
            // Validate that the saved position is still visible on screen
            Rectangle savedBounds = Settings.LastPosition;
            bool isVisible = false;

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(savedBounds))
                {
                    isVisible = true;
                    break;
                }
            }

            if (isVisible)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(savedBounds.X, savedBounds.Y);
            }
            else
            {
                // Position is off-screen, use default positioning
                StartPosition = FormStartPosition.CenterScreen;
                // Optionally clear the invalid position
                Settings.LastPosition = Rectangle.Empty;
            }
        }
    }

    /// <summary>
    /// Shows or hides the plot, resizing the window to match (<see cref="CollapsedSize"/> /
    /// <see cref="ExpandedSize"/>) — the collapsed state is the default the Tester sees; expanding
    /// is an explicit action, never automatic. <see cref="Redraw"/> skips all plotting work
    /// entirely while collapsed, so there's no drawing overhead paid while the plot isn't shown.
    /// </summary>
    private void SetPlotVisible(bool visible)
    {
        _plotVisible = visible;
        _channelsPanel.Visible = visible;
        _toggleButton.Text = visible ? "Hide Plot" : "Show Plot";
        ClientSize = visible ? ExpandedSize : CollapsedSize;
    }

    private void Redraw()
    {
        // No dependency on the plotting path below — keeps working every tick regardless, even
        // while the plot is collapsed/hidden.
        SetStreamingIndicator(_isStreaming());

        if (!_plotVisible || _plottingDisabled)
            return; // collapsed — skip all plotting work below, same as the old LabVIEW window did.

        var monitor = _waveforms();
        if (monitor is null)
            return; // stays on the placeholder — nothing configured/started yet.

        try
        {
            EnsurePlotsBuiltFor(monitor);

            for (int ch = 0; ch < monitor.ChannelNames.Count; ch++)
            {
                monitor.GetSnapshot(ch, out var min, out var max);
                var panel = _plots![ch];
                panel.SetData(min, max);
            }
        }
        catch (Exception ex)
        {
            DisablePlotting(ex);
        }
    }

    /// <summary>
    /// Called once, the first time anything in the plotting path throws. A static constructor
    /// failure in a dependency (the original ScottPlot situation this guard was written for) is
    /// cached by .NET and rethrown on every later access for the rest of the process's lifetime —
    /// so without a one-shot guard, the redraw timer would keep re-throwing it at TargetRedrawHz
    /// forever. Now that plotting is plain GDI+ with no external dependency, this is unlikely to
    /// ever fire, but it's cheap insurance and gives a diagnosable in-window message instead of a
    /// silent or repeating failure either way. The streaming indicator is unaffected — see
    /// <see cref="Redraw"/>.
    /// </summary>
    private void DisablePlotting(Exception ex)
    {
        _plottingDisabled = true;
        _plots = null;
        _plotChannelNames = null;

        _channelsPanel.Controls.Clear();
        _channelsPanel.RowStyles.Clear();
        _channelsPanel.RowCount = 1;
        _channelsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var errorLabel = new Label
        {
            Text = "Waveform plotting failed to start and has been disabled for this session:\r\n\r\n"
                 + DescribeExceptionChain(ex)
                 + "\r\n\r\n(The streaming indicator above is unaffected.)",
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            ForeColor = System.Drawing.Color.Firebrick,
            Padding = new Padding(16),
        };
        _channelsPanel.Controls.Add(errorLabel, 0, 0);
    }

    /// <summary>Flattens an exception's InnerException chain into one readable message.</summary>
    private static string DescribeExceptionChain(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (sb.Length > 0) sb.Append("\r\n --> ");
            sb.Append(e.GetType().Name).Append(": ").Append(e.Message);
        }
        return sb.ToString();
    }

    private void EnsurePlotsBuiltFor(WaveformMonitor monitor)
    {
        var names = monitor.ChannelNames;
        if (_plots is not null && _plotChannelNames is not null && _plotChannelNames.SequenceEqual(names))
            return; // already built for this exact channel set — nothing to do.

        _channelsPanel.Controls.Clear();
        _channelsPanel.RowStyles.Clear();
        _channelsPanel.RowCount = names.Count;

        _plots = new WaveformPanel[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            _channelsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / names.Count));

            var container = new Panel { Dock = DockStyle.Fill };
            var label = new Label { Text = names[i], Dock = DockStyle.Top, Height = 18, Padding = new Padding(4, 0, 0, 0) };
            var plot = new WaveformPanel { Dock = DockStyle.Fill };

            container.Controls.Add(plot);
            container.Controls.Add(label);
            _channelsPanel.Controls.Add(container, 0, i);

            _plots[i] = plot;
        }

        _plotChannelNames = names.ToArray();
    }

    private void SetStreamingIndicator(bool streaming)
    {
        _statusDot.BackColor = streaming ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Gray;
        _statusDot.Invalidate();
        _statusLabel.Text = streaming ? "Streaming" : "Stopped";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _redrawTimer.Stop();
        _redrawTimer.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// A single channel's stacked plot: the min/max envelope from <see cref="WaveformMonitor"/>,
    /// drawn as a filled band plus outline, entirely with plain GDI+ (<c>System.Drawing</c>) — no
    /// third-party plotting library, no dependency that could pull in modern-.NET-only pieces of
    /// the BCL (see this file's class-level doc comment for why that matters under MATLAB's
    /// .NET Framework hosting). Deliberately simple: this isn't trying to be a general-purpose
    /// charting control, just enough to see "is there signal, and does it look sane" at a glance.
    /// </summary>
    private sealed class WaveformPanel : Panel
    {
        private float[] _min = Array.Empty<float>();
        private float[] _max = Array.Empty<float>();

        public WaveformPanel()
        {
            DoubleBuffered = true; // avoid flicker while redrawing at TargetRedrawHz
            BackColor = System.Drawing.Color.Black;
        }

        /// <summary>Same-length arrays, oldest-first, as returned by <see cref="WaveformMonitor.GetSnapshot"/>.</summary>
        public void SetData(float[] min, float[] max)
        {
            _min = min;
            _max = max;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int n = _min.Length;
            int w = ClientSize.Width, h = ClientSize.Height;
            if (n == 0 || w <= 0 || h <= 0)
                return;

            float loY = float.PositiveInfinity, hiY = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if (_min[i] < loY) loY = _min[i];
                if (_max[i] > hiY) hiY = _max[i];
            }

            // Headroom so the trace doesn't hug the panel edges — purely proportional to the
            // signal's own observed span (10% top and bottom). Deliberately NOT a fixed absolute
            // floor (an earlier version used Math.Max(0.05f, ...) here): a fixed floor swamps any
            // signal whose true amplitude is small relative to 0.05 — e.g. a tone pip at a real,
            // legitimate amplitude of ~0.02 would get padded out to a ~0.12 total range, filling
            // under a fifth of the panel regardless of how tall the panel actually is. Purely
            // proportional padding instead keeps the trace filling ~80% of the panel no matter the
            // absolute amplitude, which is the actual point of autoscaling.
            //
            // 2026-08-20 fix: a flat/constant window (loY == hiY exactly — e.g. TTS's literal 0f
            // silence once its queue runs dry, or Subject sitting on a constant Background buffer
            // after a trial ends) has zero span, so 10% of it is also zero — there's nothing for
            // the proportional padding above to work with, and an EARLIER version of this fallback
            // (`hiY = loY + 1f`) pinned the flat value to the BOTTOM of that 1-unit window. Fed into
            // MapY, that put the entire trace at pixel row `height` — one row past the last visible
            // row of the panel, clipped and invisible. That's the actual mechanism behind "the trace
            // goes blank when the stimulus/speech stops": not NaN/Infinity anywhere (every value
            // here is an ordinary finite float — 0f is exactly representable, and nothing in this
            // averaging/comparison chain divides by anything), just a flat value silently drawn
            // just offscreen. Centering the fallback window on the constant value instead — rather
            // than starting the window AT that value — draws a flat trace as a visible line down
            // the panel's middle, matching what you'd expect to see for a genuinely constant signal.
            float span = hiY - loY;
            if (span <= 0f)
            {
                loY -= 0.5f;
                hiY += 0.5f;
            }
            else
            {
                float pad = span * 0.1f;
                loY -= pad;
                hiY += pad;
            }

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Zero line, for a visual reference point.
            float zeroY = MapY(0f, loY, hiY, h);
            if (zeroY >= 0 && zeroY <= h)
            {
                using var zeroPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, System.Drawing.Color.White));
                g.DrawLine(zeroPen, 0, zeroY, w, zeroY);
            }

            var upper = new System.Drawing.PointF[n];
            var lower = new System.Drawing.PointF[n];
            for (int i = 0; i < n; i++)
            {
                float x = n <= 1 ? 0f : i * (w - 1f) / (n - 1);
                upper[i] = new System.Drawing.PointF(x, MapY(_max[i], loY, hiY, h));
                lower[i] = new System.Drawing.PointF(x, MapY(_min[i], loY, hiY, h));
            }

            if (n >= 2)
            {
                var envelope = new System.Drawing.PointF[n * 2];
                Array.Copy(upper, envelope, n);
                for (int i = 0; i < n; i++) envelope[n + i] = lower[n - 1 - i];

                using var fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(140, 80, 200, 255));
                g.FillPolygon(fillBrush, envelope);
            }

            using var linePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(230, 120, 220, 255), 1f);
            if (n >= 2)
            {
                g.DrawLines(linePen, upper);
                g.DrawLines(linePen, lower);
            }
            else
            {
                // A single sample: nothing to draw a line between — just mark the point.
                g.DrawLine(linePen, upper[0].X - 2, upper[0].Y, upper[0].X + 2, upper[0].Y);
                g.DrawLine(linePen, lower[0].X - 2, lower[0].Y, lower[0].X + 2, lower[0].Y);
            }
        }

        private static float MapY(float value, float loY, float hiY, int height) =>
            height - (value - loY) / (hiY - loY) * height;
    }
}
