using Afareet.Race;

namespace Afareet.UI
{
    public enum RaceOverlayMode
    {
        Hidden,
        Pause,
        Results
    }

    public static class RaceUiPresentationPolicy
    {
        public static RaceOverlayMode Resolve(RaceRoundPhase phase, bool isPaused)
        {
            if (phase == RaceRoundPhase.Results)
                return RaceOverlayMode.Results;
            if (phase == RaceRoundPhase.Racing && isPaused)
                return RaceOverlayMode.Pause;
            return RaceOverlayMode.Hidden;
        }

        public static bool CanPause(RaceRoundPhase phase, bool isPaused) =>
            phase == RaceRoundPhase.Racing && !isPaused;

        public static bool CanResume(RaceRoundPhase phase, bool isPaused) =>
            phase == RaceRoundPhase.Racing && isPaused;

        public static bool CanRestart(RaceRoundPhase phase) =>
            phase == RaceRoundPhase.Results;
    }
}
