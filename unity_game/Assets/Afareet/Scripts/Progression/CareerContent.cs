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
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Career objective id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException($"Career objective '{id}' requires a description.", nameof(description));
            if (double.IsNaN(target) || double.IsInfinity(target) || target <= 0d)
                throw new ArgumentOutOfRangeException(nameof(target), "Career objective target must be finite and positive.");

            Id = id;
            Description = description;
            Target = target;
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
            if (coins < 0)
                throw new ArgumentOutOfRangeException(nameof(coins));
            if (spirit < 0)
                throw new ArgumentOutOfRangeException(nameof(spirit));
            if (unlockVehicleId != null && string.IsNullOrWhiteSpace(unlockVehicleId))
                throw new ArgumentException("Career reward vehicle unlock id must be non-blank when supplied.", nameof(unlockVehicleId));
            if (coins == 0 && spirit == 0 && unlockVehicleId == null)
                throw new ArgumentException("Career reward must contain at least one payload.");

            Coins = coins;
            Spirit = spirit;
            UnlockVehicleId = unlockVehicleId;
        }
    }

    public sealed class CareerNodeDefinition
    {
        private readonly IReadOnlyList<CareerObjective> objectives;
        private readonly IReadOnlyList<CareerReward> rewards;

        public CareerRaceNode Node { get; }
        public IReadOnlyList<CareerObjective> Objectives => objectives;
        public IReadOnlyList<CareerReward> Rewards => rewards;

        public CareerNodeDefinition(
            CareerRaceNode node,
            IEnumerable<CareerObjective> objectives,
            IEnumerable<CareerReward> rewards)
        {
            CareerDefinitionPolicy.ValidateNodeOrThrow(node);
            if (objectives == null)
                throw new ArgumentNullException(nameof(objectives));
            if (rewards == null)
                throw new ArgumentNullException(nameof(rewards));

            var objectiveList = new List<CareerObjective>(objectives);
            if (objectiveList.Count == 0)
                throw new ArgumentException($"Career node '{node.Id}' requires at least one objective.", nameof(objectives));

            var objectiveIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < objectiveList.Count; index++)
            {
                var objective = objectiveList[index];
                if (objective == null)
                    throw new ArgumentException($"Career node '{node.Id}' contains a null objective.", nameof(objectives));
                if (!objectiveIds.Add(objective.Id))
                    throw new ArgumentException(
                        $"Career node '{node.Id}' contains duplicate objective id '{objective.Id}'.",
                        nameof(objectives));
            }

            var rewardList = new List<CareerReward>(rewards);
            if (rewardList.Count == 0)
                throw new ArgumentException($"Career node '{node.Id}' requires at least one reward payload.", nameof(rewards));
            for (var index = 0; index < rewardList.Count; index++)
                if (rewardList[index] == null)
                    throw new ArgumentException($"Career node '{node.Id}' contains a null reward payload.", nameof(rewards));

            Node = node;
            this.objectives = objectiveList.AsReadOnly();
            this.rewards = rewardList.AsReadOnly();
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
                var objectives = new List<CareerObjective>
                {
                    new CareerObjective(
                        id: $"finish_{node.Id}",
                        description: "Finish the event",
                        target: 1d)
                };

                if (index > 0)
                {
                    objectives.Add(new CareerObjective(
                        id: $"clean_{node.Id}",
                        description: "Finish without restart",
                        target: 1d));
                }

                var rewards = new List<CareerReward>
                {
                    new CareerReward(
                        coins: checked(250 + index * 100),
                        spirit: checked(5 + index))
                };

                if (node.Mode == CareerRaceMode.Boss)
                    rewards.Add(new CareerReward(unlockVehicleId: "djinn_spirit"));

                definitions.Add(new CareerNodeDefinition(node, objectives, rewards));
            }

            return definitions.AsReadOnly();
        }
    }
}
