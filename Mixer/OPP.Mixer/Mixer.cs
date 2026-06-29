using System;
using System.Collections.Generic;
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
        private string _lastErrorMessage;

        public void Open()
        {
            _mixerPanel = new MixerPanel();
            _mixerPanel.Show();

            foreach (var strip in _mixerPanel.ChannelStrips)
            {
                strip.LevelChanged += HandleLevelChanged;
                strip.MuteToggled += HandleMuteChanged;
            }
        }

        public void Close()
        {
            if (_mixerPanel != null)
            {
                foreach (var strip in _mixerPanel.ChannelStrips)
                {
                    strip.LevelChanged -= HandleLevelChanged;
                    strip.MuteToggled -= HandleMuteChanged;
                }

                _mixerPanel.Close();
                _mixerPanel = null;
            }

            _motu?.Dispose();
        }

        //public bool Initialize(HttpClient httpClient)
        //{
        //    _motu = new MotuClient(httpClient);
        //    return DoInitialization();
        //}

        public bool Initialize(string motuUrl)
        {
            _motu = new MotuClient(motuUrl);
            return DoInitialization();
        }

        public string LastErrorMessage => _lastErrorMessage;

        private bool DoInitialization()
        {
            if (!TestConnection())
            {
                Debug.WriteLine("MOTU device not reachable.");
                return false;
            }

            string waypoint = "";
            try
            {
                waypoint = "SetParticipantLevelToZero";
                SetParticipantLevelToZero();
                waypoint = "MuteTalkback";
                MuteTalkback();
                waypoint = "ReadMotuState";
                ReadMotuState();
                waypoint = "ActivateStrips";
                ActivateStrips();
                waypoint = "MuteAudioStream";
                MuteAudioStream(true);
                waypoint = "";
            }
            catch (Exception ex)
            {
                _lastErrorMessage = $"MOTU initialization error ({waypoint}): {ex.Message}";
                if (ex.InnerException != null)
                {
                    _lastErrorMessage += $"{Environment.NewLine}Inner exception: {ex.InnerException.Message}";
                }
                return false;
            }

            return true;
        }

        public void MuteAudioStream(bool mute)
        {
            var stimStrips = _mixerPanel.ChannelStrips.FindAll(strip => strip.Title == "Stimulus");
            foreach (var strip in stimStrips)
            {
                strip.MuteAndDisable(mute);
            }
        }

        public void ConnectMonitor(int signal)
        {
            //_motu.Write($"datastore/ext/obank/7/ch/0/src", $"16:{signal}");
        }

        public void ToggleTalkback()
        {
            int talkbackIndex = _mixerPanel.ChannelStrips.FindIndex(strip => strip.ChannelId == "WaverTalkback");
            if (talkbackIndex >= 0)
            {
                var muteValue = ReadMuteValue(talkbackIndex);
                WriteMuteValue(talkbackIndex, !muteValue);
                _mixerPanel.ChannelStrips[talkbackIndex].SetMuteSilently(!muteValue);
            }
        }

        private void MuteTalkback()
        {
            int talkbackIndex = _mixerPanel.ChannelStrips.FindIndex(strip => strip.ChannelId == "WaverTalkback");
            if (talkbackIndex >= 0)
            {
                WriteMuteValue(talkbackIndex, true);
                _mixerPanel.ChannelStrips[talkbackIndex].SetMuteSilently(true);
            }
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
            catch (HttpRequestException ex)   // connection refused, wrong IP, non-2xx status
            {
                _lastErrorMessage = $"MOTU connection error: {ex.Message}";
                return false;
            }
            catch (TaskCanceledException)  // timed out — host not answering
            {
                _lastErrorMessage = "MOTU connection timed out.";
                return false;
            }
        }

        private void ActivateStrips()
        {
            foreach (var item in _mixerPanel.ChannelStrips)
            {
                if (item.ChannelId != "ParticipantStim")
                {
                    item.Activate();
                }
            }
        }

        private void SetParticipantLevelToZero()
        {
            int participantStimIndex = _mixerPanel.ChannelStrips.FindIndex(strip => strip.ChannelId == "ParticipantStim");
            if (participantStimIndex >= 0)
            {
                WriteFaderValue(participantStimIndex, 0);
            }
        }

        private void ReadMotuState()
        {
            for (int k=0; k < _mixerPanel.ChannelStrips.Count; k++)
            {
                var faderValue = ReadFaderValue(k);
                _mixerPanel.ChannelStrips[k].SetLevelSilently(faderValue);

                var muteValue = ReadMuteValue(k);
                _mixerPanel.ChannelStrips[k].SetMuteSilently(muteValue);
            }
        }

        private void HandleLevelChanged(object sender, ChannelLevelEventArgs e)
        {
            int channelIndex = _mixerPanel.ChannelStrips.FindIndex(strip => strip == sender);
            if (channelIndex >= 0)
            {
                WriteFaderValue(channelIndex, e.Level);
            }
        }

        private void HandleMuteChanged(object sender, ChannelMuteEventArgs e)
        {
            int channelIndex = _mixerPanel.ChannelStrips.FindIndex(strip => strip == sender);
            if (channelIndex >= 0)
            {
                WriteMuteValue(channelIndex, e.Muted);
            }
        }

        private float ReadFaderValue(int channelIndex)
        {
            DatastoreValue fader = _motu.Get<DatastoreValue>($"datastore/mix/chan/{channelIndex}/matrix/fader");
            return 20f * (float)Math.Log10(fader.value);
        }

        private void WriteFaderValue(int channelIndex, float levelDB)
        {
            float linearValue = (float)Math.Pow(10, levelDB / 20.0);
            _motu.Write($"datastore/mix/chan/{channelIndex}/matrix/fader", linearValue);
        }   

        private bool ReadMuteValue(int channelIndex)
        {
            DatastoreValue mute = _motu.Get<DatastoreValue>($"datastore/mix/chan/{channelIndex}/matrix/mute");
            return mute.value > 0;
        }

        private void WriteMuteValue(int channelIndex, bool muted)
        {
            _motu.Write($"datastore/mix/chan/{channelIndex}/matrix/mute", muted ? (int)1 : (int)0);
        }
    }
}
