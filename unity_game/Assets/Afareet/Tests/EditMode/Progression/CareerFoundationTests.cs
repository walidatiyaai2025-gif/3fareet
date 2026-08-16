using System;
using System.Collections.Generic;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerFoundationTests
    {
        [Test]
        public void ChapterOne_ReproducesRetainedLegacyFoundation()
        {
            var chapter = ChapterOneCareerContent.CreateFoundation();
            CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);

            Assert.That(chapter.Id, Is.EqualTo("chapter_01_cairo_after_dark"));
            Assert.That(chapter.Title, Is.EqualTo("Cairo After Dark"));
            Assert.That(chapter.Order, Is.EqualTo(1));
            Assert.That(chapter.Nodes.Count, Is.EqualTo(5));

            AssertNode(chapter.Nodes[0], "c01_r01", "Corniche Run", CareerRaceMode.Circuit, "cairo_corniche_night", 0);
            AssertNode(chapter.Nodes[1], "c01_r02", "Clock of Khan", CareerRaceMode.TimeTrial, "khan_el_khalili_sprint", 2);
            Assert.That(chapter.Nodes[1].TargetTimeSeconds, Is.EqualTo(92d));
            AssertNode(chapter.Nodes[2], "c01_r03", "Last Car Standing", CareerRaceMode.Elimination, "ring_road_midnight", 4);
            AssertNode(chapter.Nodes[3], "c01_r04", "Spirit Drift", CareerRaceMode.DriftChallenge, "citadel_drift", 6);
            Assert.That(chapter.Nodes[3].TargetDriftScore, Is.EqualTo(12000));
            AssertNode(chapter.Nodes[4], "c01_boss", "Djinn of the Asphalt", CareerRaceMode.Boss, "pyramids_spirit_run", 9);
            Assert.That(chapter.Nodes[4].BossVehicleId, Is.EqualTo("djinn_spirit"));
        }

        [Test]
        public void ModeSpecificValidation_FailsClosed()
        {
            Assert.Throws<ArgumentException>(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
                Node("tt", CareerRaceMode.TimeTrial)));
            Assert.Throws<ArgumentException>(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
                Node("drift", CareerRaceMode.DriftChallenge)));
            Assert.Throws<ArgumentException>(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
                Node("boss", CareerRaceMode.Boss)));
            Assert.Throws<ArgumentException>(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
                new CareerRaceNode("bad", "Bad", CareerRaceMode.TimeTrial, "track", 0, double.NaN)));
        }

        [Test]
        public void ChapterValidation_RejectsDuplicateNodeIds()
        {
            var chapter = new CareerChapter(
                "chapter",
                "Chapter",
                1,
                new[]
                {
                    Node("same", CareerRaceMode.Circuit),
                    Node("same", CareerRaceMode.Elimination)
                });

            Assert.Throws<ArgumentException>(() => CareerDefinitionPolicy.ValidateChapterOrThrow(chapter));
        }

        [Test]
        public void CareerMap_SortsDeterministicallyAndRejectsDuplicateChapterIds()
        {
            var map = new CareerMap(new[]
            {
                Chapter("chapter_b", 2),
                Chapter("chapter_c", 1),
                Chapter("chapter_a", 1)
            });

            Assert.That(map.Chapters[0].Id, Is.EqualTo("chapter_a"));
            Assert.That(map.Chapters[1].Id, Is.EqualTo("chapter_c"));
            Assert.That(map.Chapters[2].Id, Is.EqualTo("chapter_b"));
            Assert.That(map.ChapterById("chapter_b"), Is.SameAs(map.Chapters[2]));

            Assert.Throws<ArgumentException>(() => new CareerMap(new[]
            {
                Chapter("duplicate", 1),
                Chapter("duplicate", 2)
            }));
        }

        [Test]
        public void NodeState_CompletedWinsOtherwiseUsesStarGate()
        {
            var map = new CareerMap(new[] { ChapterOneCareerContent.CreateFoundation() });
            var node = map.Chapters[0].Nodes[3];

            Assert.That(map.NodeState(node, 5, new HashSet<string>()), Is.EqualTo(CareerNodeState.Locked));
            Assert.That(map.NodeState(node, 6, new HashSet<string>()), Is.EqualTo(CareerNodeState.Available));
            Assert.That(
                map.NodeState(node, 0, new HashSet<string>(StringComparer.Ordinal) { node.Id }),
                Is.EqualTo(CareerNodeState.Completed));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.NodeState(node, -1, new HashSet<string>()));
        }

        private static CareerRaceNode Node(string id, CareerRaceMode mode)
        {
            return new CareerRaceNode(id, id, mode, "track", 0);
        }

        private static CareerChapter Chapter(string id, int order)
        {
            return new CareerChapter(id, id, order, new[] { Node(id + "_race", CareerRaceMode.Circuit) });
        }

        private static void AssertNode(
            CareerRaceNode node,
            string id,
            string title,
            CareerRaceMode mode,
            string trackId,
            int requiredStars)
        {
            Assert.That(node.Id, Is.EqualTo(id));
            Assert.That(node.Title, Is.EqualTo(title));
            Assert.That(node.Mode, Is.EqualTo(mode));
            Assert.That(node.TrackId, Is.EqualTo(trackId));
            Assert.That(node.RequiredStars, Is.EqualTo(requiredStars));
        }
    }
}
