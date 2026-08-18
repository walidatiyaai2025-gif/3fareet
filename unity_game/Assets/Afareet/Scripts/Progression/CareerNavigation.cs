using System;
using System.Collections.Generic;

namespace Afareet.Progression
{
    public sealed class CareerNavigationNodeSnapshot
    {
        public CareerChapter Chapter { get; }
        public CareerRaceNode Node { get; }
        public CareerNodeState State { get; }
        public int ChapterIndex { get; }
        public int NodeIndex { get; }
        public int FlatIndex { get; }
        public bool IsSelected { get; }
        public bool IsLocked => State == CareerNodeState.Locked;
        public bool IsAvailable => State == CareerNodeState.Available;
        public bool IsCompleted => State == CareerNodeState.Completed;

        public CareerNavigationNodeSnapshot(
            CareerChapter chapter,
            CareerRaceNode node,
            CareerNodeState state,
            int chapterIndex,
            int nodeIndex,
            int flatIndex,
            bool isSelected)
        {
            CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);
            CareerDefinitionPolicy.ValidateNodeOrThrow(node);
            if (!Enum.IsDefined(typeof(CareerNodeState), state))
                throw new ArgumentOutOfRangeException(nameof(state));
            if (chapterIndex < 0) throw new ArgumentOutOfRangeException(nameof(chapterIndex));
            if (nodeIndex < 0) throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            if (flatIndex < 0) throw new ArgumentOutOfRangeException(nameof(flatIndex));

            Chapter = chapter;
            Node = node;
            State = state;
            ChapterIndex = chapterIndex;
            NodeIndex = nodeIndex;
            FlatIndex = flatIndex;
            IsSelected = isSelected;
        }
    }

    public sealed class CareerNavigationSnapshot
    {
        private readonly IReadOnlyList<CareerNavigationNodeSnapshot> nodes;

        public IReadOnlyList<CareerNavigationNodeSnapshot> Nodes => nodes;
        public string SelectedNodeId { get; }
        public int SelectedIndex { get; }
        public CareerNavigationNodeSnapshot SelectedNode => nodes[SelectedIndex];

        internal CareerNavigationSnapshot(
            IEnumerable<CareerNavigationNodeSnapshot> nodes,
            int selectedIndex)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            var copy = new List<CareerNavigationNodeSnapshot>(nodes);
            if (copy.Count == 0)
                throw new ArgumentException("Career navigation must contain at least one node.", nameof(nodes));
            if (selectedIndex < 0 || selectedIndex >= copy.Count)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            for (var index = 0; index < copy.Count; index++)
            {
                if (copy[index] == null)
                    throw new ArgumentException("Career navigation cannot contain null nodes.", nameof(nodes));
                if (copy[index].FlatIndex != index)
                    throw new ArgumentException("Career navigation flat indices must be contiguous and deterministic.", nameof(nodes));
                if (copy[index].IsSelected != (index == selectedIndex))
                    throw new ArgumentException("Career navigation selected flags must match the selected index.", nameof(nodes));
            }

            this.nodes = copy.AsReadOnly();
            SelectedIndex = selectedIndex;
            SelectedNodeId = copy[selectedIndex].Node.Id;
        }

        public bool TryGetNode(string nodeId, out CareerNavigationNodeSnapshot node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(nodes[index].Node.Id, nodeId))
                {
                    node = nodes[index];
                    return true;
                }
            }

            node = null;
            return false;
        }
    }

    public sealed class CareerNavigationService
    {
        private sealed class NodeSeed
        {
            public CareerChapter Chapter { get; }
            public CareerRaceNode Node { get; }
            public CareerNodeState State { get; }
            public int ChapterIndex { get; }
            public int NodeIndex { get; }
            public int FlatIndex { get; }

            public NodeSeed(
                CareerChapter chapter,
                CareerRaceNode node,
                CareerNodeState state,
                int chapterIndex,
                int nodeIndex,
                int flatIndex)
            {
                Chapter = chapter;
                Node = node;
                State = state;
                ChapterIndex = chapterIndex;
                NodeIndex = nodeIndex;
                FlatIndex = flatIndex;
            }
        }

        public CareerNavigationSnapshot Build(
            CareerMap map,
            CareerProgress progress,
            string preferredNodeId = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (preferredNodeId != null && string.IsNullOrWhiteSpace(preferredNodeId))
                throw new ArgumentException("Preferred Career node id must be null or non-blank.", nameof(preferredNodeId));

            var seeds = new List<NodeSeed>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var completed = new HashSet<string>(progress.CompletedNodeIds, StringComparer.Ordinal);

            for (var chapterIndex = 0; chapterIndex < map.Chapters.Count; chapterIndex++)
            {
                var chapter = map.Chapters[chapterIndex];
                CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);
                var chapterUnlocked = progress.Stars >= chapter.RequiredStars;

                for (var nodeIndex = 0; nodeIndex < chapter.Nodes.Count; nodeIndex++)
                {
                    var node = chapter.Nodes[nodeIndex];
                    if (!ids.Add(node.Id))
                        throw new ArgumentException($"Career navigation contains duplicate node id '{node.Id}'.", nameof(map));

                    CareerNodeState state;
                    if (completed.Contains(node.Id))
                        state = CareerNodeState.Completed;
                    else if (!chapterUnlocked)
                        state = CareerNodeState.Locked;
                    else
                        state = map.NodeState(node, progress.Stars, completed);

                    seeds.Add(new NodeSeed(
                        chapter,
                        node,
                        state,
                        chapterIndex,
                        nodeIndex,
                        seeds.Count));
                }
            }

            if (seeds.Count == 0)
                throw new ArgumentException("Career navigation map does not contain any nodes.", nameof(map));

            var selectedIndex = preferredNodeId == null
                ? ResolveDefaultSelection(seeds)
                : FindRequiredIndex(seeds, preferredNodeId, nameof(preferredNodeId));
            return CreateSnapshot(seeds, selectedIndex);
        }

        public CareerNavigationSnapshot Select(CareerNavigationSnapshot snapshot, string nodeId)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Career navigation node id is required.", nameof(nodeId));

            var selectedIndex = -1;
            for (var index = 0; index < snapshot.Nodes.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(snapshot.Nodes[index].Node.Id, nodeId))
                {
                    selectedIndex = index;
                    break;
                }
            }

            if (selectedIndex < 0)
                throw new ArgumentException($"Unknown Career navigation node '{nodeId}'.", nameof(nodeId));
            if (selectedIndex == snapshot.SelectedIndex)
                return snapshot;
            return Rebuild(snapshot, selectedIndex);
        }

        public CareerNavigationSnapshot Move(CareerNavigationSnapshot snapshot, int delta)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (delta == 0) return snapshot;

            var count = snapshot.Nodes.Count;
            var raw = (long)snapshot.SelectedIndex + delta;
            var wrapped = (int)(raw % count);
            if (wrapped < 0) wrapped += count;
            return Rebuild(snapshot, wrapped);
        }

        private static CareerNavigationSnapshot Rebuild(CareerNavigationSnapshot snapshot, int selectedIndex)
        {
            var nodes = new List<CareerNavigationNodeSnapshot>(snapshot.Nodes.Count);
            for (var index = 0; index < snapshot.Nodes.Count; index++)
            {
                var current = snapshot.Nodes[index];
                nodes.Add(new CareerNavigationNodeSnapshot(
                    current.Chapter,
                    current.Node,
                    current.State,
                    current.ChapterIndex,
                    current.NodeIndex,
                    current.FlatIndex,
                    index == selectedIndex));
            }
            return new CareerNavigationSnapshot(nodes, selectedIndex);
        }

        private static CareerNavigationSnapshot CreateSnapshot(IReadOnlyList<NodeSeed> seeds, int selectedIndex)
        {
            var nodes = new List<CareerNavigationNodeSnapshot>(seeds.Count);
            for (var index = 0; index < seeds.Count; index++)
            {
                var seed = seeds[index];
                nodes.Add(new CareerNavigationNodeSnapshot(
                    seed.Chapter,
                    seed.Node,
                    seed.State,
                    seed.ChapterIndex,
                    seed.NodeIndex,
                    seed.FlatIndex,
                    index == selectedIndex));
            }
            return new CareerNavigationSnapshot(nodes, selectedIndex);
        }

        private static int ResolveDefaultSelection(IReadOnlyList<NodeSeed> seeds)
        {
            for (var index = 0; index < seeds.Count; index++)
                if (seeds[index].State == CareerNodeState.Available)
                    return index;
            for (var index = 0; index < seeds.Count; index++)
                if (seeds[index].State == CareerNodeState.Completed)
                    return index;
            return 0;
        }

        private static int FindRequiredIndex(IReadOnlyList<NodeSeed> seeds, string nodeId, string parameterName)
        {
            for (var index = 0; index < seeds.Count; index++)
                if (StringComparer.Ordinal.Equals(seeds[index].Node.Id, nodeId))
                    return index;
            throw new ArgumentException($"Unknown Career navigation node '{nodeId}'.", parameterName);
        }
    }
}