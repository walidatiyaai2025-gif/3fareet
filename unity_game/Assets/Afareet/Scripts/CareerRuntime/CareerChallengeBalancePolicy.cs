using System;
using Afareet.Progression;
using Afareet.Race;

namespace Afareet.CareerRuntime
{
    public static class CareerChallengeBalancePolicy
    {
        public static RaceChallengeConfiguration Resolve(CareerRaceNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            switch (node.Mode)
            {
                case CareerRaceMode.Circuit:
                    return new RaceChallengeConfiguration(
                        activeRivalCount: 3,
                        aiDifficulty: new AiDifficultyTuning(.88f, .90f));

                case CareerRaceMode.TimeTrial:
                    return new RaceChallengeConfiguration(
                        activeRivalCount: 0,
                        aiDifficulty: AiDifficultyTuning.Standard);

                case CareerRaceMode.Elimination:
                    return new RaceChallengeConfiguration(
                        activeRivalCount: 3,
                        aiDifficulty: new AiDifficultyTuning(.98f, 1.04f),
                        eliminationEnabled: true);

                case CareerRaceMode.DriftChallenge:
                    return new RaceChallengeConfiguration(
                        activeRivalCount: 0,
                        aiDifficulty: AiDifficultyTuning.Standard);

                case CareerRaceMode.Boss:
                    return new RaceChallengeConfiguration(
                        activeRivalCount: 1,
                        aiDifficulty: new AiDifficultyTuning(1.08f, 1.14f));

                default:
                    throw new InvalidOperationException($"Unsupported Career race mode '{node.Mode}'.");
            }
        }
    }
}
