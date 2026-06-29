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

        //public float Level { /* set updates slider silently */ }
        //public bool Muted { /* set updates toggle silently */ }
        //public bool Controls Enabled { /* disable interlock from main app */ }

        //public event EventHandler<ChannelLevelEventArgs> LevelChanged;  // user moved fader
        //public event EventHandler<ChannelMuteEventArgs> MuteToggled;   // user hit mute

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

        private void Clear()
        {
            SetLevelSilently(trackBar.Minimum);
            trackBar.Enabled = false;
            numericBox.Enabled = false;
            muteButton.Enabled = false;
        }

        private void muteButton_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            if (_setSilently) return;

            numericBox.FloatValue = trackBar.Value;
        }

        private void numericBox_ValueChanged(object sender, EventArgs e)
        {
            Debug.WriteLine($"numericBox_ValueChanged: {numericBox.FloatValue}");
        }

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            if (_setSilently) return;

            numericBox.FloatValue = trackBar.Value;
            Debug.WriteLine($"trackBar_ValueChanged: {trackBar.Value}");
        }
    }
}
