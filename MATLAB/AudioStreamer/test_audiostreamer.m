dllPath = 'D:\Development\Arenberg\jga_opp\LabVIEW\OPP.AudioStreamer\Build\OPP.AudioStreamer.dll';
asmInfo = NET.addAssembly(dllPath);


Fs = 50000;
F = 500;
T = 1;
duration = 0.25;
ramp = 0.01;

npts = round(Fs * T);
tone = zeros(npts, 1);
noise = tone;

nptsDur = round(Fs * duration);

nptsRamp = floor(Fs * ramp);
hw = hanning(nptsRamp * 2);
win = [hw(1:nptsRamp); ones(nptsDur-length(hw), 1); hw(nptsRamp+1:end)];


t = (0:nptsDur-1) / nptsDur * T;
y = sin(2*pi*F*t(:));
tone(1:nptsDur) = y .* win;
noise(1:nptsDur) = normrnd(0, 1, nptsDur, 1) .* win;

info = audiodevinfo;


streamer = OPP.AudioStreamer;
streamer.Initialize(true);
streamer.SetConfig('Out 1-24 (MOTU Pro Audio)','Microphone (HD Pro Webcam C920)', Fs);
% streamer.SetConfig('asdfadsf', Fs);
streamer.SetNumReps(3);
streamer.SetSignal('caregiver', '500-Hz tone', tone);
streamer.SetSignal('waver', '500-Hz tone', tone);
streamer.SetSignal('background', '500-Hz tone', tone);
streamer.SetSignal('signal', 'noise burst', noise);

h = AudioStreamerTest(streamer, []);
uiwait(h.UIFigure);

streamer.Close();

