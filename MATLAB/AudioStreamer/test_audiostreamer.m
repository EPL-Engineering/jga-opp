asmInfo = NET.addAssembly(fullfile('D:\Development\Arenberg\OPP\jga_opp\LabVIEW\Build', 'OPP.AudioStreamer.dll'));


Fs = 50000;

info = audiodevinfo;


streamer = OPP.AudioStreamer;
streamer.Initialize(true);
streamer.SetConfig(info.output(2).Name, Fs);
% streamer.SetNumReps(3);
streamer.SetSignal('caregiver', '500-Hz tone', ones(1,3));
streamer.SetSignal('waver', '500-Hz tone', ones(1,3));
streamer.SetSignal('background', '500-Hz tone', ones(1,3));
streamer.SetSignal('signal', 'noise burst', ones(1,3));

AudioStreamerTest(streamer);


streamer.Close();

