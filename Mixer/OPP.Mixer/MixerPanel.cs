using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPP.Mixer
{
    public partial class MixerPanel : Form
    {
        public List<ChannelStrip> ChannelStrips { get; private set; } = new List<ChannelStrip>();

        public MixerPanel()
        {
            InitializeComponent();
            RestoreLastPosition();
        }

        private void MixerPanel_FormClosing(object sender, FormClosingEventArgs e)
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

        private void MixerPanel_Load(object sender, EventArgs e)
        {
            ChannelStrips.Add(caregiverStrip);
        }
    }
}
