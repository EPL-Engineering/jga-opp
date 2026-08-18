namespace OppStreamer.Core
{
    /// <summary>
    /// Top-level stimulus set in use. Toggled by TrainTest() and independent of whether a
    /// trial is currently active — Trigger() works the same way in either mode (confirmed
    /// during design review).
    /// </summary>
    public enum OperatingMode
    {
        Test,
        Training,
    }
}