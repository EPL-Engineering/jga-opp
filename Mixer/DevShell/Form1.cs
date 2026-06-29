using OPP.Mixer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevShell
{
    public partial class Form1 : Form
    {
        private Mixer _mixer;

        private FakeMotuHandler _fake;

        public Form1()
        {
            InitializeComponent();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            _mixer = new Mixer();
            _mixer.Open();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            _mixer.Close();
        }

        private void initButton_Click(object sender, EventArgs e)
        {
            _fake = new FakeMotuHandler();
            _fake.Set("datastore/mix/chan/0/matrix/fader", 0.5);
            var http = new HttpClient(_fake) { BaseAddress = new Uri("http://localhost/") };
            
//            string motuUrl = urlBox.Text;
            _mixer.Initialize(http);
        }
    }
}
