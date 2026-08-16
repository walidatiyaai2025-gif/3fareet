using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerEventSettlement
    {
        private readonly IReadOnlyList<CareerReward> grantedRewards;
        private readonly IReadOnlyList<string> grantedRewardIds;
        private readonly IReadOnlyList<string> unlockedVehicleIds;

        public CareerObjectiveEvaluation Evaluation { get; }
        public CareerProgress Progress { get; }
        public bool NodeCompletedNow { get; }
        public int StarsEarned { get; }
        public IReadOnlyList<CareerReward> GrantedRewards => grantedRewards;
        public IReadOnlyList<string> GrantedRewardIds => grantedRewardIds;
        public IReadOnlyList<string> UnlockedVehicleIds => unlockedVehicleIds;
        public int CoinsGranted { get; }
        public int SpiritGranted { get; }
        public bool GrantedAnyReward => grantedRewards.Count > 0;

        public CareerEventSettlement(
            CareerObjectiveEvaluation evaluation,
            CareerProgress progress,
            bool nodeCompletedNow,
            int starsEarned,
            IEnumerable<CareerReward> grantedRewards,
            IEnumerable<string> grantedRewardIds)
        {
            Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (starsEarned < 0 || starsEarned > 3)
                throw new ArgumentOutOfRangeException(nameof(starsEarned));
            if (grantedRewards == null)
                throw new ArgumentNullException(nameof(grantedRewards));
            if (grantedRewardIds == null)
                throw new ArgumentNullException(nameof(grantedRewardIds));

            var rewards = new List<CareerReward>(grantedRewards);
            var rewardIds = new List<string>(grantedRewardIds);
            if (rewards.Count != rewardIds.Count)
                throw new ArgumentException("Granted reward payloads and ids must have the same count.");

            var vehicleIds = new List<string>();
            var coins = 0;
            var spirit = 0;
            for (var index = 0; index < rewards.Count; index++)
            {
                var reward = rewards[index] ??
                    throw new ArgumentException("Granted rewards cannot contain null entries.", nameof(grantedRewards));
                CareerProgress.ValidateId(rewardIds[index], nameof(grantedRewardIds));
                checked
                {
                    coins += reward.Coins;
                    spirit += reward.Spirit;
                }
                if (reward.HasVehicleUnlock)
                    vehicleIds.Add(reward.UnlockVehicleId);
            }

            NodeCompletedNow = nodeCompletedNow;
            StarsEarned = starsEarned;
            this.grantedRewards = rewards.AsReadOnly();
            this.grantedRewardIds = rewardIds.AsReadOnly();
            unlockedVehicleIds = vehicleIds.AsReadOnly();
            CoinsGranted = coins;
            SpiritGranted = spirit;
        }
    }

    public sealed class CareerEventSettlementService
    {
        private readonly CareerProgressionService progression = new CareerProgressionService();

        public CareerEventSettlement Settle(
            CareerNodeDefinition definition,
            CareerEventOutcome outcome,
            CareerProgress progress)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            var evaluation = CareerObjectiveEvaluationPolicy.Evaluate(definition, outcome);
            var nodeId = definition.Node.Id;
            var alreadyCompleted = progress.IsNodeCompleted(nodeId);

            // A previously completed node may still recover deterministic unclaimed rewards
            // after an interrupted save. A new completion must first satisfy its mode contract.
            if (!alreadyCompleted && !CareerEventCompletionPolicy.CanComplete(definition, outcome))
                return EmptySettlement(evaluation, progress);

            var starsEarned = alreadyCompleted ? 0 : CareerStarAwardPolicy.Resolve(outcome, evaluation);
            var updatedProgress = alreadyCompleted
                ? progress
                : progression.CompleteNode(progress, nodeId, starsEarned);
            var rewards = new List<CareerReward>();
            var rewardIds = new List<string>();

            for (var index = 0; index < definition.Rewards.Count; index++)
            {
                var rewardId = BuildRewardId(nodeId, index);
                if (!progression.CanClaim(rewardId, updatedProgress))
                    continue;

                updatedProgress = progression.Claim(rewardId, updatedProgress);
                rewards.Add(definition.Rewards[index]);
                rewardIds.Add(rewardId);
            }

            return new CareerEventSettlement(
                evaluation,
                updatedProgress,
                nodeCompletedNow: !alreadyCompleted,
                starsEarned,
                rewards,
                rewardIds);
        }

        public static string BuildRewardId(string nodeId, int rewardIndex)
        {
            CareerProgress.ValidateId(nodeId, nameof(nodeId));
            if (rewardIndex < 0) throw new ArgumentOutOfRangeException(nameof(rewardIndex));
            return $"career:{nodeId}:reward:{rewardIndex:00}";
        }

        private static CareerEventSettlement EmptySettlement(
            CareerObjectiveEvaluation evaluation,
            CareerProgress progress)
        {
            return new CareerEventSettlement(
                evaluation,
                progress,
                nodeCompletedNow: false,
                starsEarned: 0,
                Array.Empty<CareerReward>(),
                Array.Empty<string>());
        }
    }

    public static class CareerEventCompletionPolicy
    {
        public static bool CanComplete(CareerNodeDefinition definition, CareerEventOutcome outcome)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (!outcome.Finished) return false;

            var node = definition.Node;
            switch (node.Mode)
            {
                case CareerRaceMode.Circuit:
                    return true;
                case CareerRaceMode.TimeTrial:
                    return node.TargetTimeSeconds.HasValue &&
                           outcome.FinishTimeSeconds.HasValue &&
                           outcome.FinishTimeSeconds.Value <= node.TargetTimeSeconds.Value;
                case CareerRaceMode.Elimination:
                case CareerRaceMode.Boss:
                    return outcome.FinalPosition == 1;
                case CareerRaceMode.DriftChallenge:
                    return node.TargetDriftScore.HasValue && outcome.DriftScore >= node.TargetDriftScore.Value;
                default:
                    throw new InvalidOperationException($"Unsupported Career mode '{node.Mode}'.");
            }
        }
    }

    public static class CareerStarAwardPolicy
    {
        private const int PassStars = 2;
        private const int PerfectStars = 3;

        public static int Resolve(CareerEventOutcome outcome, CareerObjectiveEvaluation evaluation)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            if (!outcome.Finished) return 0;
            return evaluation.AllCompleted ? PerfectStars : PassStars;
        }
    }
}
