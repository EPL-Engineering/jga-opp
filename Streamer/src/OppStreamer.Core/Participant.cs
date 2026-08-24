namespace OppStreamer.Core
{
    /// <summary>
    /// The two continuously-looping stimulus channels the streamer feeds to the mixer (MOTU
    /// channels 2 and 3). Text-to-speech (channel 4), the Beacon/Alert one-shot (channel 5), and
    /// the two mic pass-throughs (channels 6/7) are not part of the shared masker loop and are
    /// handled by separate components (see <see cref="TtsPlayer"/> and <see cref="StreamerEngine"/>'s
    /// Beacon members) — they are deliberately not represented here.
    ///
    /// <b>2026-08-22 — Waver removed.</b> Waver used to be a third entry here, with its own buffer
    /// that in practice was always set to the same content as whichever of Caregiver/Subject it was
    /// supposed to mirror — a redundancy with no upside. It's now handled entirely by the MOTU's own
    /// hardware mixer, tapping whichever of Caregiver's or Subject's physical output channel the
    /// current mode calls for (Test: Caregiver's channel; Training: Subject's channel) directly at
    /// the mixer, in perfect sync with zero desync risk, since it's the identical physical signal
    /// rather than a resynthesized copy. See the README's 2026-08-22 entry for the full reasoning.
    /// </summary>
    public enum Participant
    {
        Caregiver,
        Subject,
    }
}
