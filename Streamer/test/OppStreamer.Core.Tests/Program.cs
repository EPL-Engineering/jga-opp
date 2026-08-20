using OppStreamer.Core.Tests;

var runner = new TestRunner();
StreamerEngineTests.Register(runner);
DriftCompensatedRingBufferTests.Register(runner);
TtsPlayerTests.Register(runner);
StopBoundaryTests.Register(runner);
WaveformMonitorTests.Register(runner);
WaitForLatchTests.Register(runner);
return runner.ReportAndGetExitCode();
