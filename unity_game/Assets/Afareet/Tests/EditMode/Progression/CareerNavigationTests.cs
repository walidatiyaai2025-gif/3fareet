using System;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerNavigationTests
    {
        [Test]
        public void Build_DefaultsToFirstAvailableAndPreservesDeterministicOrder()
        {
            var service = new CareerNavigationService();
            var map = new CareerMap(new[] { ChapterOneCareerContent.CreateFoundation() });

            var snapshot = service.Build(map, CareerProgress.Empty());

            Assert.That(snapshot.Nodes.Count, Is.EqualTo(5));
            Assert.That(snapshot.SelectedNodeId, Is.EqualTo("c01_r01"));
            Assert.That(snapshot.SelectedIndex, Is.Zero);
            Assert.That(snapshot.Nodes[0].State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(snapshot.Nodes[1].State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(snapshot.Nodes[0].ChapterIndex, Is.Zero);
            Assert.That(snapshot.Nodes[0].NodeIndex, Is.Zero);
            Assert.That(snapshot.Nodes[0].FlatIndex, Is.Zero);
            Assert.That(snapshot.Nodes[0].IsSelected, Is.True);
        }

        [Test]
        public void Build_DerivesCompletedAvailableAndLockedFromAuthoritativeProgress()
        {
            var service = new CareerNavigationService();
            var map = new CareerMap(new[] { ChapterOneCareerContent.CreateFoundation() });
            var progress = new CareerProgress(
                CareerProgress.CurrentVersion,
                6,
                new[] { "c01_r01" },
                Array.Empty<string>());

            var snapshot = service.Build(map, progress, "c01_r03");

            Assert.That(snapshot.SelectedNodeId, Is.EqualTo("c01_r03"));
            Assert.That(snapshot.Nodes[0].State, Is.EqualTo(CareerNodeState.Completed));
            Assert.That(snapshot.Nodes[1].State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(snapshot.Nodes[2].State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(snapshot.Nodes[3].State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(snapshot.Nodes[4].State, Is.EqualTo(CareerNodeState.Locked));
        }

        [Test]
        public void SelectAndMove_UseStableIdsAndWrapBothDirections()
        {
            var service = new CareerNavigationService();
            var map = new CareerMap(new[] { ChapterOneCareerContent.CreateFoundation() });
            var snapshot = service.Build(map, CareerProgress.Empty(), "c01_r03");

            var previous = service.Move(snapshot, -3);
            Assert.That(previous.SelectedNodeId, Is.EqualTo("c01_boss"));
            var next = service.Move(previous, 1);
            Assert.That(next.SelectedNodeId, Is.EqualTo("c01_r01"));
            var selected = service.Select(next, "c01_r04");
            Assert.That(selected.SelectedNodeId, Is.EqualTo("c01_r04"));
            Assert.That(selected.Nodes[3].IsSelected, Is.True);
            Assert.Throws<ArgumentException>(() => service.Select(selected, "missing"));
            Assert.Throws<ArgumentException>(() => service.Build(map, CareerProgress.Empty(), "missing"));
        }

        [Test]
        public void Build_RespectsChapterGateWhileCompletedStateWins()
        {
            var chapter = new CareerChapter(
                "gated",
                "Gated",
                1,
                new[] { new CareerRaceNode("gated_race", "Race", CareerRaceMode.Circuit, "track", 0) },
                requiredStars: 5);
            var map = new CareerMap(new[] { chapter });
            var service = new CareerNavigationService();
            var belowGate = new CareerProgress(
                CareerProgress.CurrentVersion,
                4,
                Array.Empty<string>(),
                Array.Empty<string>());

            Assert.That(service.Build(map, belowGate).Nodes[0].State, Is.EqualTo(CareerNodeState.Locked));

            var alreadyCompleted = new CareerProgress(
                CareerProgress.CurrentVersion,
                0,
                new[] { "gated_race" },
                Array.Empty<string>());
            Assert.That(service.Build(map, alreadyCompleted).Nodes[0].State, Is.EqualTo(CareerNodeState.Completed));
        }

        [Test]
        public void Build_FailsClosedOnDuplicateNodeIdsAcrossChapters()
        {
            var map = new CareerMap(new[]
            {
                new CareerChapter(
                    "one",
                    "One",
                    1,
                    new[] { new CareerRaceNode("shared", "Shared A", CareerRaceMode.Circuit, "a", 0) }),
                new CareerChapter(
                    "two",
                    "Two",
                    2,
                    new[] { new CareerRaceNode("shared", "Shared B", CareerRaceMode.Circuit, "b", 0) })
            });

            Assert.Throws<ArgumentException>(() => new CareerNavigationService().Build(map, CareerProgress.Empty()));
        }
    }
}