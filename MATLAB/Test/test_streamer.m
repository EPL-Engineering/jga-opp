
asm = System.AppDomain.CurrentDomain.GetAssemblies;
for k = 1:asm.Length
   isLoaded = startsWith(char(asm(k).FullName), 'OPPStreamer.ConfigApi');
   if isLoaded, break; end
end

if ~isLoaded
   NET.addAssembly(fullfile(getenv('DEVROOT'), 'Arenberg\jga-opp\OppStreamer\src\OppStreamer.ConfigApi\bin\Debug\net48\OppStreamer.ConfigApi.dll'));
   NET.addAssembly(fullfile(getenv('DEVROOT'), 'Arenberg\jga-opp\MATLAB\OPP', 'KLib.WindowsVoice.dll'));
end

hStream = OppStreamer.ConfigApi.ConfigApi;

outDevice = 'Speakers (USB Sound Device)';
testerMicDevice = 'Microphone (USB Sound Device)';
boothMicDevice = 'Microphone Array on SoundWire Device (13- Cirrus Logic XU)';

Fs = 48000;
T = 1;
npts = round(Fs * T);
t = (0:npts-1) / Fs;
y = 0.25 * sin(2*pi*500*t);
y = epl.signals.cos2window(y, 'Duration', 250, 'Ramp', 20);

hStream.Initialize();
hStream.SetConfig(outDevice, npts, testerMicDevice, boothMicDevice);
hStream.SetNumReps(3);
hStream.SetSignal('Caregiver', 'Test', y);
hStream.SetSignal('Waver', 'Test', y);
hStream.SetSignal('Subject', 'Test', y);

hVoice = KLib.WindowsVoice.WindowsVoice();
