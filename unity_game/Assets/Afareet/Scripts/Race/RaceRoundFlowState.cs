using System;

namespace Afareet.Race
{
    public enum RaceRoundPhase { Ready, Countdown, Racing, Results }

    public sealed class RaceRoundFlowState
    {
        public RaceRoundPhase Phase { get; private set; } = RaceRoundPhase.Ready;
        public float CountdownRemaining { get; private set; }
        public float FinishTime { get; private set; } = -1f;
        public int RoundNumber { get; private set; } = 1;

        public void BeginCountdown(float seconds)
        {
            if (seconds <= 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Phase != RaceRoundPhase.Ready) throw new InvalidOperationException();
            CountdownRemaining = seconds;
            Phase = RaceRoundPhase.Countdown;
        }

        public bool TickCountdown(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Phase != RaceRoundPhase.Countdown) return false;
            CountdownRemaining = Math.Max(0f, CountdownRemaining - deltaTime);
            if (CountdownRemaining > 0f) return false;
            Phase = RaceRoundPhase.Racing;
            return true;
        }

        public void Finish(float finishTime)
        {
            if (Phase != RaceRoundPhase.Racing) throw new InvalidOperationException();
            if (finishTime < 0f) throw new ArgumentOutOfRangeException(nameof(finishTime));
            FinishTime = finishTime;
            Phase = RaceRoundPhase.Results;
        }

        public void Restart()
        {
            if (Phase != RaceRoundPhase.Results) throw new InvalidOperationException();
            RoundNumber++;
            CountdownRemaining = 0f;
            FinishTime = -1f;
            Phase = RaceRoundPhase.Ready;
        }
    }
}
