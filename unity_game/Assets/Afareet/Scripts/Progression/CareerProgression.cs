using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerProgress
    {
        public const int CurrentVersion = 1;

        private readonly IReadOnlyList<string> completedNodeIds;
        private readonly IReadOnlyList<string> claimedRewardIds;
        private readonly HashSet<string> completedNodeLookup;
        private readonly HashSet<string> claimedRewardLookup;

        public int Version { get; }
        public int Stars { get; }
        public IReadOnlyList<string> CompletedNodeIds => completedNodeIds;
        public IReadOnlyList<string> ClaimedRewardIds => claimedRewardIds;

        public CareerProgress(
            int version,
            int stars,
            IEnumerable<string> completedNodeIds,
            IEnumerable<string> claimedRewardIds)
        {
            if (version != CurrentVersion)
                throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported Career progress version {version}; expected {CurrentVersion}.");
            if (stars < 0)
                throw new ArgumentOutOfRangeException(nameof(stars));

            Version = version;
            Stars = stars;

            HashSet<string> completedLookup;
            HashSet<string> claimedLookup;
            this.completedNodeIds = BuildDeterministicIds(completedNodeIds, nameof(completedNodeIds), out completedLookup);
            this.claimedRewardIds = BuildDeterministicIds(claimedRewardIds, nameof(claimedRewardIds), out claimedLookup);
            completedNodeLookup = completedLookup;
            claimedRewardLookup = claimedLookup;
        }

        public static CareerProgress Empty()
        {
            return new CareerProgress(CurrentVersion, 0, Array.Empty<string>(), Array.Empty<string>());
        }

        public bool IsNodeCompleted(string nodeId)
        {
            ValidateId(nodeId, nameof(nodeId));
            return completedNodeLookup.Contains(nodeId);
        }

        public bool IsRewardClaimed(string rewardId)
        {
            ValidateId(rewardId, nameof(rewardId));
            return claimedRewardLookup.Contains(rewardId);
        }

        private static IReadOnlyList<string> BuildDeterministicIds(
            IEnumerable<string> source,
            string parameterName,
            out HashSet<string> lookup)
        {
            if (source == null)
                throw new ArgumentNullException(parameterName);

            lookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in source)
            {
                ValidateId(id, parameterName);
                lookup.Add(id);
            }

            var ordered = new List<string>(lookup);
            ordered.Sort(StringComparer.Ordinal);
            return ordered.AsReadOnly();
        }

        internal static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Career progress ids must be non-blank.", parameterName);
        }
    }

    public sealed class CareerProgressionService
    {
        public bool CanEnter(CareerRaceNode node, CareerProgress progress)
        {
            CareerDefinitionPolicy.ValidateNodeOrThrow(node);
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            return progress.Stars >= node.RequiredStars;
        }

        public CareerProgress CompleteNode(CareerProgress progress, string nodeId, int starsEarned)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            CareerProgress.ValidateId(nodeId, nameof(nodeId));

            if (progress.IsNodeCompleted(nodeId))
                return progress;

            var clampedStars = starsEarned < 0 ? 0 : starsEarned > 3 ? 3 : starsEarned;
            int nextStars;
            checked
            {
                nextStars = progress.Stars + clampedStars;
            }

            var completed = new List<string>(progress.CompletedNodeIds) { nodeId };
            return new CareerProgress(
                CareerProgress.CurrentVersion,
                nextStars,
                completed,
                progress.ClaimedRewardIds);
        }

        public bool CanClaim(string rewardId, CareerProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            CareerProgress.ValidateId(rewardId, nameof(rewardId));
            return !progress.IsRewardClaimed(rewardId);
        }

        public CareerProgress Claim(string rewardId, CareerProgress progress)
        {
            if (!CanClaim(rewardId, progress))
                return progress;

            var claimed = new List<string>(progress.ClaimedRewardIds) { rewardId };
            return new CareerProgress(
                CareerProgress.CurrentVersion,
                progress.Stars,
                progress.CompletedNodeIds,
                claimed);
        }

        public bool ChapterComplete(CareerChapter chapter, CareerProgress progress)
        {
            CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            for (var index = 0; index < chapter.Nodes.Count; index++)
                if (!progress.IsNodeCompleted(chapter.Nodes[index].Id))
                    return false;
            return true;
        }
    }
}
