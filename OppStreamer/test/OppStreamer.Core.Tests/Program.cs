using OppStreamer.Core.Tests;

var runner = new TestRunner();
StreamerEngineTests.Register(runner);
DriftCompensatedRingBufferTests.Register(runner);
TtsPlayerTests.Register(runner);
return runner.ReportAndGetExitCode();
