asm = System.AppDomain.CurrentDomain.GetAssemblies;
for k = 1:asm.Length
   isLoaded = startsWith(char(asm(k).FullName), 'OPP.Mixer');
   if isLoaded, break; end
end

if ~isLoaded
   NET.addAssembly(fullfile(getenv('DEVROOT'), 'Arenberg\jga_opp\LabVIEW\OPP.Mixer\Build\OPP.Mixer.dll'));
   NET.addAssembly(fullfile(getenv('DEVROOT'), 'Arenberg\jga_opp\LabVIEW\OPP.AudioStreamer\Build\OPP.AudioStreamer.dll'));
end
   NET.addAssembly(fullfile(getenv('DEVROOT'), 'Arenberg\jga_opp\LabVIEW\OPP.AudioStreamer\Build\OPP.AudioStreamer.dll'));

hStream = OPP.AudioStreamer;
disp('Initializing streamer: opening...');
t0 = tic;
drawnow();
hStream.Initialize(true);
fprintf('%.1f seconds\n', toc(t0));

hMixer = OPP.Mixer;

disp('Initializing mixer: opening...');
t0 = tic;
drawnow();
hMixer.Open();
fprintf('%.1f seconds\n', toc(t0));
disp('Initializing mixer: connecting...');
drawnow();
t0 = tic;
hMixer.Initialize('test');
hMixer.MuteAudioStream(true);
fprintf('%.1f seconds\n', toc(t0));
disp('Initializing mixer: setting state...');
drawnow();
t0 = tic;
hMixer.SetChannelProperty('participant', 'slider enable', false);
hMixer.ConnectMonitor(-1);
fprintf('%.1f seconds\n', toc(t0));
disp('Initializing mixer: finishing...');
drawnow();
t0 = tic;
success = hMixer.WaitForReady(10);
if ~success
   error('Timed out waiting for mixer to get ready');
end

fprintf('%.1f seconds\n', toc(t0));
disp('Finished.');