using C462.Shared;
using KLib.Signals;
using ScottPlot;

using OppStreamer.Hardware;
using OppStreamer.Core;
using OppStreamer.ConfigApi;

namespace OppStreamer.DevShell
{
    public partial class MainForm : Form
    {
        float[] _trainingStim;
        double[] _testStim;

        float _sampleRate = 48000f;
        int _loopLen;

        StreamerEngine _engine;
        IStreamerAudioOutput _audioOutput;

        public MainForm()
        {
            InitializeComponent();
            InitSignalGraph();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CreateSignals();
            PlotSignals();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _audioOutput?.Dispose();
        }

        private void InitSignalGraph()
        {
            // Hide axis label and tick
            signalGraph.Plot.Axes.Left.TickLabelStyle.IsVisible = false;
            signalGraph.Plot.Axes.Left.MajorTickStyle.Length = 0;
            signalGraph.Plot.Axes.Left.MinorTickStyle.Length = 0;
            signalGraph.Plot.XLabel("Time (s)");

            // Hide axis edge line
            signalGraph.Plot.Axes.Left.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Right.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Top.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Bottom.MinorTickStyle.Length = 0;

            signalGraph.Plot.Axes.Bottom.Label.Bold = false;
            signalGraph.Plot.Axes.Bottom.Label.FontSize = 12;
            signalGraph.Plot.Axes.Bottom.TickLabelStyle.FontSize = 12;

            signalGraph.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
            var padding = new PixelPadding(
                left: 0,
                right: 0,
                bottom: 50, // keep some bottom padding for x-axis labels
                top: 0);
            signalGraph.Plot.Layout.Fixed(padding);
            signalGraph.Refresh();
        }


        private void CreateSignals()
        {
            SignalContext signalContext = new SignalContext()
            {
                AdapterMap = AdapterMap.Default7point1Map()
            };

            SignalManager signalManager = new SignalManager();

            Channel ch = new Channel()
            {
                Active = true,
                Name = "Training",
                Modality = Modality.Audio,
                Laterality = Laterality.Left,
                Waveform = new Sinusoid() { Frequency_Hz = 1000 },
                Gate = new Gate()
                {
                    Active = true,
                    Width_ms = 200,
                    Ramp_ms = 10
                },
                Level = new Level()
                {
                    Units = LevelUnits.dB_attenuation,
                    Value = "-20"
                }
            };

            _loopLen = (int)(_sampleRate * 0.5); // 0.5 seconds of data

            signalManager.AddChannel(ch);
            signalManager.Initialize(_sampleRate, _loopLen, signalContext);

            ch.Create();
            _trainingStim = new float[_loopLen];
            for (int i = 0; i < _loopLen; i++)
            {
                _trainingStim[i] = ch.Data[i];
            }

        }

        private void PlotSignals()
        {
            signalGraph.Plot.Clear();

            var npts = _trainingStim.Length;
            var time = new double[npts];
            var y = new double[npts];

            int irow = 0;
            var maxVal = _trainingStim.Max();
            double scaleFactor = maxVal > 0 ? 1 / maxVal : 1;

            for (int k = 0; k < npts; k++)
            {
                time[k] = k / _sampleRate;
                y[k] = _trainingStim[k] * scaleFactor + 2 * irow;
            }

            signalGraph.Plot.Add.SignalXY(time, y);
            --irow;

            signalGraph.Plot.Axes.AutoScale();
            signalGraph.Refresh();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;

            _engine = new StreamerEngine();
            _audioOutput = StreamerAudioOutputFactory.Create(AudioBackend.Wasapi, _engine);

            _engine.Reset(_loopLen);
            _engine.SetNumReps(3);

            _engine.SetSignal(Participant.Caregiver, OperatingMode.Test, _trainingStim);
            _engine.SetSignal(Participant.Waver, OperatingMode.Test, _trainingStim);
            _engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, _trainingStim); // Background
            _engine.SetSubjectSignal(OperatingMode.Test, isSignal: false, _trainingStim); // Background

            _audioOutput.Start("Speakers (USB Sound Device)");
            stopButton.Enabled = true;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            stopButton.Enabled = false;

            _audioOutput.Stop();

            startButton.Enabled = true;
        }

        private void speakButton_Click(object sender, EventArgs e)
        {
            var voice = new KLib.WindowsVoice.WindowsVoice();
            var data = voice.Render("Hello, this is a test of the text to speech system.");
            _engine.SendTts(data);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ConfigApi.ConfigApi streamer = new ConfigApi.ConfigApi();
            streamer.Initialize();
        }
    }
}
