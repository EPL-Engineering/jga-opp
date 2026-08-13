namespace OppStreamer.Core;

/// <summary>
/// The three continuously-looping stimulus channels the streamer feeds to the mixer
/// (MOTU channels 2, 3, and 4). Text-to-speech (channel 5) and the two mic pass-throughs
/// (channels 6/7) are not part of the shared masker loop and are handled by separate
/// components (see design doc §5.6, §5.5) — they are deliberately not represented here.
/// </summary>
public enum Participant
{
    Caregiver,
    Waver,
    Subject,
}
