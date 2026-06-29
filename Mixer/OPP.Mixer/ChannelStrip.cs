using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPP.Mixer
{
    using System;

    public partial class ChannelStrip : UserControl
    {
        public string Title { 
            get
            {
                return label.Text;
            }
            set
            {
                label.Text = value;
            }
        }

        public string ChannelId { get; set; }          // opaque tag, e.g. "Talkback" — NOT a URL

        public event EventHandler<ChannelLevelEventArgs> LevelChanged;  // user moved fader
        public event EventHandler<ChannelMuteEventArgs> MuteToggled;   // user hit mute

        bool _setSilently = false;

        public ChannelStrip()
        {
            InitializeComponent();
            Clear();
        }

        public void SetLevelSilently(float level)
        {
            _setSilently = true;
            trackBar.Value = (int)level;
            numericBox.FloatValue = level;
            _setSilently = false;
        }

        public void SetMuteSilently(bool muted)
        {
            _setSilently = true;
            muteButton.Checked = muted;
            _setSilently = false;
        }

        public void Activate()
        {
            trackBar.Enabled = true;
            numericBox.Enabled = true;
            muteButton.Enabled = true;
        }

        public void MuteAndDisable(bool mute)
        {
            SetMuteSilently(mute);
            muteButton.Enabled = !mute;
        }

        private void Clear()
        {
            SetLevelSilently(trackBar.Minimum);
            trackBar.Enabled = false;
            numericBox.Enabled = false;
            muteButton.Enabled = false;
        }

        private void muteButton_CheckedChanged(object sender, EventArgs e)
        {
            muteButton.BackColor = muteButton.Checked ? Color.IndianRed : Color.LightGreen;
            if (_setSilently) return;

            MuteToggled?.Invoke(this, new ChannelMuteEventArgs(ChannelId, muteButton.Checked));
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            if (_setSilently) return;

            numericBox.FloatValue = trackBar.Value;
            LevelChanged?.Invoke(this, new ChannelLevelEventArgs(ChannelId, trackBar.Value));
        }

        private void numericBox_ValueChanged(object sender, EventArgs e)
        {
            _setSilently = true;
            trackBar.Value = (int) numericBox.FloatValue;
            _setSilently = false;

            LevelChanged?.Invoke(this, new ChannelLevelEventArgs(ChannelId, numericBox.FloatValue));
        }

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            if (_setSilently) return;

            numericBox.FloatValue = trackBar.Value;
        }
    }
}

public sealed class ChannelLevelEventArgs : EventArgs
{
    public string ChannelId { get; }
    public float Level { get; }

    public ChannelLevelEventArgs(string channelId, float level)
    {
        ChannelId = channelId;
        Level = level;
    }
}

public sealed class ChannelMuteEventArgs : EventArgs
{
    public string ChannelId { get; }
    public bool Muted { get; }

    public ChannelMuteEventArgs(string channelId, bool muted)
    {
        ChannelId = channelId;
        Muted = muted;
    }
}

