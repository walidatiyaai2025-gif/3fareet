namespace Afareet.Support
{
    public enum RaceUiPhase { Racing, Paused, Results, RestartRequested }

    public sealed class RaceUiFlow
    {
        public RaceUiPhase Phase { get; private set; } = RaceUiPhase.Racing;

        public bool Pause()
        {
            if (Phase != RaceUiPhase.Racing) return false;
            Phase = RaceUiPhase.Paused;
            return true;
        }

        public bool Resume()
        {
            if (Phase != RaceUiPhase.Paused) return false;
            Phase = RaceUiPhase.Racing;
            return true;
        }

        public bool ShowResults()
        {
            if (Phase != RaceUiPhase.Racing && Phase != RaceUiPhase.Paused) return false;
            Phase = RaceUiPhase.Results;
            return true;
        }

        public bool RequestRestart()
        {
            if (Phase != RaceUiPhase.Results) return false;
            Phase = RaceUiPhase.RestartRequested;
            return true;
        }

        public void BeginRace() { Phase = RaceUiPhase.Racing; }
    }
}
