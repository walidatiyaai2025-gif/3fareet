using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerObjective
    {
        public string Id { get; }
        public string Description { get; }
        public double Target { get; }

        public CareerObjective(string id, string description, double target)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Career objective id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException($"Career objective '{id}' requires a description.", nameof(description));
            if (double.IsNaN(target) || double.IsInfinity(target) || target <= 0d) throw new ArgumentOutOfRangeException(nameof(target));
            Id = id; Description = description; Target = target;
        }
    }

    public sealed class CareerReward
    {
        public int Coins { get; }
        public int Spirit { get; }
        public string UnlockVehicleId { get; }
        public bool HasVehicleUnlock => UnlockVehicleId != null;

        public CareerReward(int coins = 0, int spirit = 0, string unlockVehicleId = null)
        {
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            if (spirit < 0) throw new ArgumentOutOfRangeException(nameof(spirit));
            if (unlockVehicleId != null && string.IsNullOrWhiteSpace(unlockVehicleId)) throw new ArgumentException("Career reward vehicle unlock id must be non-blank when supplied.", nameof(unlockVehicleId));
            if (coins == 0 && spirit == 0 && unlockVehicleId == null) throw new ArgumentException("Career reward must contain at least one payload.");
            Coins = coins; Spirit = spirit; UnlockVehicleId = unlockVehicleId;
        }
    }

    public sealed class CareerNodeDefinition
    {
        private readonly IReadOnlyList<CareerObjective> objectives;
        private readonly IReadOnlyList<CareerReward> rewards;
        public CareerRaceNode Node { get; }
        public IReadOnlyList<CareerObjective> Objectives => objectives;
        public IReadOnlyList<CareerReward> Rewards => rewards;

        public CareerNodeDefinition(CareerRaceNode node, IEnumerable<CareerObjective> objectives, IEnumerable<CareerReward> rewards)
        {
            CareerDefinitionPolicy.ValidateNodeOrThrow(node);
            if (objectives == null) throw new ArgumentNullException(nameof(objectives));
            if (rewards == null) throw new ArgumentNullException(nameof(rewards));
            var objectiveList = new List<CareerObjective>(objectives);
            if (objectiveList.Count == 0) throw new ArgumentException($"Career node '{node.Id}' requires at least one objective.", nameof(objectives));
            var objectiveIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var objective in objectiveList)
            {
                if (objective == null) throw new ArgumentException($"Career node '{node.Id}' contains a null objective.", nameof(objectives));
                if (!objectiveIds.Add(objective.Id)) throw new ArgumentException($"Career node '{node.Id}' contains duplicate objective id '{objective.Id}'.", nameof(objectives));
            }
            var rewardList = new List<CareerReward>(rewards);
            if (rewardList.Count == 0) throw new ArgumentException($"Career node '{node.Id}' requires at least one reward payload.", nameof(rewards));
            if (rewardList.Contains(null)) throw new ArgumentException($"Career node '{node.Id}' contains a null reward payload.", nameof(rewards));
            Node = node; this.objectives = objectiveList.AsReadOnly(); this.rewards = rewardList.AsReadOnly();
        }
    }

    public static class ChapterOneCareerEventContent
    {
        public static IReadOnlyList<CareerNodeDefinition> CreateDefinitions()
        {
            var chapter = ChapterOneCareerContent.CreateFoundation();
            var definitions = new List<CareerNodeDefinition>(chapter.Nodes.Count);
            for (var index = 0; index < chapter.Nodes.Count; index++)
            {
                var node = chapter.Nodes[index];
                var objectives = new List<CareerObjective> { Binary($"finish_{node.Id}", "Finish the event") };
                if (index > 0) objectives.Add(Binary($"clean_{node.Id}", "Finish without restart"));
                switch (node.Mode)
                {
                    case CareerRaceMode.TimeTrial:
                        objectives.Add(Binary($"time_{node.Id}", $"Finish in {node.TargetTimeSeconds.Value:0.#} seconds or less"));
                        break;
                    case CareerRaceMode.Elimination:
                        objectives.Add(Binary($"win_{node.Id}", "Finish in first place"));
                        break;
                    case CareerRaceMode.DriftChallenge:
                        objectives.Add(Binary($"drift_{node.Id}", $"Reach {node.TargetDriftScore.Value} drift score"));
                        break;
                    case CareerRaceMode.Boss:
                        objectives.Add(Binary($"win_{node.Id}", "Defeat the boss and finish first"));
                        break;
                }
                var rewards = new List<CareerReward> { new CareerReward(checked(250 + index * 100), checked(5 + index)) };
                if (node.Mode == CareerRaceMode.Boss) rewards.Add(new CareerReward(unlockVehicleId: "djinn_spirit"));
                definitions.Add(new CareerNodeDefinition(node, objectives, rewards));
            }
            return definitions.AsReadOnly();
        }

        private static CareerObjective Binary(string id, string description) => new CareerObjective(id, description, 1d);
    }
}
