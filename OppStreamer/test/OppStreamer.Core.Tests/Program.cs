using OppStreamer.Core.Tests;

var runner = new TestRunner();
StreamerEngineTests.Register(runner);
return runner.ReportAndGetExitCode();
