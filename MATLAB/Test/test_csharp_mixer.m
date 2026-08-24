NET.addAssembly('C:\Development\jga-opp\MATLAB\OPP\DotNet\Mixer\OPP.Mixer.dll');

h = OPP.Mixer.Mixer;
h.Open();
h.Initialize('http://169.254.228.35');