using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public enum CareerRaceMode
    {
        Circuit = 0,
        TimeTrial = 1,
        Elimination = 2,
        DriftChallenge = 3,
        Boss = 4
    }

    public enum CareerNodeState
    {
        Locked = 0,
        Available = 1,
        Completed = 2
    }

    public sealed class CareerRaceNode
    {
        public string Id { get; }
        public string Title { get; }
        public CareerRaceMode Mode { get; }
        public string TrackId { get; }
        public int RequiredStars { get; }
        public double? TargetTimeSeconds { get; }
        public int? TargetDriftScore { get; }
        public string BossVehicleId { get; }

        public CareerRaceNode(
            string id,
            string title,
            CareerRaceMode mode,
            string trackId,
            int requiredStars,
            double? targetTimeSeconds = null,
            int? targetDriftScore = null,
            string bossVehicleId = null)
        {
            Id = id;
            Title = title;
            Mode = mode;
            TrackId = trackId;
            RequiredStars = requiredStars;
            TargetTimeSeconds = targetTimeSeconds;
            TargetDriftScore = targetDriftScore;
            BossVehicleId = bossVehicleId;
        }
    }

    public sealed class CareerChapter
    {
        private readonly IReadOnlyList<CareerRaceNode> nodes;

        public string Id { get; }
        public string Title { get; }
        public int Order { get; }
        public int RequiredStars { get; }
        public IReadOnlyList<CareerRaceNode> Nodes => nodes;

        public CareerChapter(
            string id,
            string title,
            int order,
            IEnumerable<CareerRaceNode> nodes,
            int requiredStars = 0)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            Id = id;
            Title = title;
            Order = order;
            RequiredStars = requiredStars;
            this.nodes = new List<CareerRaceNode>(nodes).AsReadOnly();
        }
    }

    public sealed class CareerMap
    {
        private readonly IReadOnlyList<CareerChapter> chapters;

        public IReadOnlyList<CareerChapter> Chapters => chapters;

        public CareerMap(IEnumerable<CareerChapter> chapters)
        {
            if (chapters == null)
                throw new ArgumentNullException(nameof(chapters));

            var ordered = new List<CareerChapter>(chapters);
            if (ordered.Count == 0)
                throw new ArgumentException("Career map must contain at least one chapter.", nameof(chapters));

            var chapterIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < ordered.Count; index++)
            {
                var chapter = ordered[index];
                CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);
                if (!chapterIds.Add(chapter.Id))
                    throw new ArgumentException($"Duplicate career chapter id '{chapter.Id}'.", nameof(chapters));
            }

            ordered.Sort((left, right) =>
            {
                var order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            this.chapters = ordered.AsReadOnly();
        }

        public CareerChapter ChapterById(string id)
        {
            if (id == null)
                return null;

            for (var index = 0; index < chapters.Count; index++)
                if (StringComparer.Ordinal.Equals(chapters[index].Id, id))
                    return chapters[index];
            return null;
        }

        public CareerNodeState NodeState(
            CareerRaceNode node,
            int earnedStars,
            ISet<string> completedNodeIds)
        {
            CareerDefinitionPolicy.ValidateNodeOrThrow(node);
            if (earnedStars < 0)
                throw new ArgumentOutOfRangeException(nameof(earnedStars));
            if (completedNodeIds == null)
                throw new ArgumentNullException(nameof(completedNodeIds));

            if (completedNodeIds.Contains(node.Id))
                return CareerNodeState.Completed;
            return earnedStars >= node.RequiredStars
                ? CareerNodeState.Available
                : CareerNodeState.Locked;
        }
    }

    public static class CareerDefinitionPolicy
    {
        public static void ValidateNodeOrThrow(CareerRaceNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(node.Id))
                throw new ArgumentException("Career race node id is required.", nameof(node));
            if (string.IsNullOrWhiteSpace(node.Title))
                throw new ArgumentException($"Career race node '{node.Id}' requires a title.", nameof(node));
            if (!Enum.IsDefined(typeof(CareerRaceMode), node.Mode))
                throw new ArgumentException($"Career race node '{node.Id}' has unknown mode {(int)node.Mode}.", nameof(node));
            if (string.IsNullOrWhiteSpace(node.TrackId))
                throw new ArgumentException($"Career race node '{node.Id}' requires a track id.", nameof(node));
            if (node.RequiredStars < 0)
                throw new ArgumentException($"Career race node '{node.Id}' has negative required stars.", nameof(node));

            switch (node.Mode)
            {
                case CareerRaceMode.TimeTrial:
                    if (!node.TargetTimeSeconds.HasValue ||
                        double.IsNaN(node.TargetTimeSeconds.Value) ||
                        double.IsInfinity(node.TargetTimeSeconds.Value) ||
                        node.TargetTimeSeconds.Value <= 0d)
                    {
                        throw new ArgumentException(
                            $"Time-trial node '{node.Id}' requires a finite positive target time.",
                            nameof(node));
                    }
                    break;
                case CareerRaceMode.DriftChallenge:
                    if (!node.TargetDriftScore.HasValue || node.TargetDriftScore.Value <= 0)
                    {
                        throw new ArgumentException(
                            $"Drift-challenge node '{node.Id}' requires a positive drift target.",
                            nameof(node));
                    }
                    break;
                case CareerRaceMode.Boss:
                    if (string.IsNullOrWhiteSpace(node.BossVehicleId))
                    {
                        throw new ArgumentException(
                            $"Boss node '{node.Id}' requires a boss vehicle id.",
                            nameof(node));
                    }
                    break;
            }
        }

        public static void ValidateChapterOrThrow(CareerChapter chapter)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));
            if (string.IsNullOrWhiteSpace(chapter.Id))
                throw new ArgumentException("Career chapter id is required.", nameof(chapter));
            if (string.IsNullOrWhiteSpace(chapter.Title))
                throw new ArgumentException($"Career chapter '{chapter.Id}' requires a title.", nameof(chapter));
            if (chapter.Order < 1)
                throw new ArgumentException($"Career chapter '{chapter.Id}' requires order >= 1.", nameof(chapter));
            if (chapter.RequiredStars < 0)
                throw new ArgumentException($"Career chapter '{chapter.Id}' has negative required stars.", nameof(chapter));
            if (chapter.Nodes.Count == 0)
                throw new ArgumentException($"Career chapter '{chapter.Id}' requires at least one race node.", nameof(chapter));

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < chapter.Nodes.Count; index++)
            {
                var node = chapter.Nodes[index];
                ValidateNodeOrThrow(node);
                if (!nodeIds.Add(node.Id))
                    throw new ArgumentException(
                        $"Career chapter '{chapter.Id}' contains duplicate node id '{node.Id}'.",
                        nameof(chapter));
            }
        }
    }
}
