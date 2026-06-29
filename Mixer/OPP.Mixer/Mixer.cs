using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace OPP.Mixer
{
    public class Mixer
    {
        private MixerPanel _mixerPanel;
        private string _motuUrl;

        private MotuClient _motu;

        public void Open()
        {
            _mixerPanel = new MixerPanel();
            _mixerPanel.Show();
        }

        public void Close()
        {
            if (_mixerPanel != null)
            {
                _mixerPanel.Close();
                _mixerPanel = null;
            }

            _motu?.Dispose();
        }

        public bool Initialize(HttpClient httpClient)
        {
            _motu = new MotuClient(httpClient);
            return DoInitialization();
        }

        public bool Initialize(string motuUrl)
        {
            _motu = new MotuClient(motuUrl);
            return DoInitialization();
        }

        private bool DoInitialization()
        {
            if (!TestConnection())
            {
                Debug.WriteLine("MOTU device not reachable.");
                return false;
            }

            ReadMotuState();
            return true;
        }

        public void MuteAudioStream(bool mute)
        {
            // Implement logic to mute or unmute the audio stream
            // This could involve interacting with the audio engine or API
        }

        public void SetChannelProperty(string channelId, string propertyName, object value)
        {
            // Implement logic to set a property of a specific channel
            // This could involve finding the channel by its ID and updating the property
        }

        public void ConnectMonitor(int signal)
        {

        }

        public void ToggleTalkback()
        {
            // Implement logic to toggle the talkback feature
            // This could involve changing the state of a talkback button or sending a command to the audio engine
        }

        private bool TestConnection(int timeoutMs = 3000)
        {
            try
            {
                using (var cts = new CancellationTokenSource(timeoutMs))
                using (var resp = _motu.GetAsync("datastore", cts.Token).GetAwaiter().GetResult())
                {
                    resp.EnsureSuccessStatusCode();   // optional — see note below
                    return true;                       // box answered: reachable
                }
            }
            catch (HttpRequestException)   // connection refused, wrong IP, non-2xx status
            {
                return false;
            }
            catch (TaskCanceledException)  // timed out — host not answering
            {
                return false;
            }
        }

        private void ReadMotuState()
        {
            var faderValue = ReadFaderValue(0);
            _mixerPanel.ChannelStrips[0].SetLevelSilently(faderValue);
        }

        private float ReadFaderValue(int channelIndex)
        {
            DatastoreValue fader = _motu.Get<DatastoreValue>($"datastore/mix/chan/{channelIndex}/matrix/fader");
            return 20f * (float) Math.Log10(fader.Value);
        }
    }
}
