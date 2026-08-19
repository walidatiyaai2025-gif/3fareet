using System;
using System.Collections.Generic;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerContentTests
    {
        [Test]
        public void ChapterOneContent_PreservesExactLegacyObjectivesAndRewards()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var expectedNodeIds = new[] { "c01_r01", "c01_r02", "c01_r03", "c01_r04", "c01_boss" };
            var expectedCoins = new[] { 250, 350, 450, 550, 650 };
            var expectedSpirit = new[] { 5, 6, 7, 8, 9 };

            Assert.That(definitions.Count, Is.EqualTo(5));
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                Assert.That(definition.Node.Id, Is.EqualTo(expectedNodeIds[index]));
                Assert.That(definition.Objectives[0].Id, Is.EqualTo($"finish_{expectedNodeIds[index]}"));
                Assert.That(definition.Objectives[0].Description, Is.EqualTo("Finish the event"));
                Assert.That(definition.Objectives[0].Target, Is.EqualTo(1d));
                Assert.That(definition.Rewards[0].Coins, Is.EqualTo(expectedCoins[index]));
                Assert.That(definition.Rewards[0].Spirit, Is.EqualTo(expectedSpirit[index]));

                if (index == 0)
                {
                    Assert.That(definition.Objectives.Count, Is.EqualTo(1));
                }
                else
                {
                    Assert.That(definition.Objectives.Count, Is.EqualTo(2));
                    Assert.That(definition.Objectives[1].Id, Is.EqualTo($"clean_{expectedNodeIds[index]}"));
                    Assert.That(definition.Objectives[1].Description, Is.EqualTo("Finish without restart"));
                    Assert.That(definition.Objectives[1].Target, Is.EqualTo(1d));
                }
            }
        }

        [Test]
        public void BossContent_AddsExactDjinnSpiritUnlockPayload()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var boss = definitions[definitions.Count - 1];

            Assert.That(boss.Node.Mode, Is.EqualTo(CareerRaceMode.Boss));
            Assert.That(boss.Rewards.Count, Is.EqualTo(2));
            Assert.That(boss.Rewards[1].Coins, Is.Zero);
            Assert.That(boss.Rewards[1].Spirit, Is.Zero);
            Assert.That(boss.Rewards[1].UnlockVehicleId, Is.EqualTo("djinn_spirit"));
            Assert.That(boss.Rewards[1].HasVehicleUnlock, Is.True);
        }

        [Test]
        public void ObjectiveAndRewardContracts_FailClosedOnInvalidPayloads()
        {
            Assert.Throws<ArgumentException>(() => new CareerObjective(" ", "Finish", 1d));
            Assert.Throws<ArgumentException>(() => new CareerObjective("finish", " ", 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerObjective("finish", "Finish", 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerObjective("finish", "Finish", double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerReward(coins: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerReward(spirit: -1));
            Assert.Throws<ArgumentException>(() => new CareerReward());
            Assert.Throws<ArgumentException>(() => new CareerReward(unlockVehicleId: " "));
        }

        [Test]
        public void NodeDefinition_RejectsDuplicateObjectivesAndMissingPayloads()
        {
            var node = ChapterOneCareerContent.CreateFoundation().Nodes[0];
            var duplicateObjectives = new[]
            {
                new CareerObjective("same", "A", 1d),
                new CareerObjective("same", "B", 1d)
            };

            Assert.Throws<ArgumentException>(() =>
                new CareerNodeDefinition(node, duplicateObjectives, new[] { new CareerReward(coins: 1) }));
            Assert.Throws<ArgumentException>(() =>
                new CareerNodeDefinition(node, Array.Empty<CareerObjective>(), new[] { new CareerReward(coins: 1) }));
            Assert.Throws<ArgumentException>(() =>
                new CareerNodeDefinition(node, new[] { new CareerObjective("a", "A", 1d) }, Array.Empty<CareerReward>()));
        }

        [Test]
        public void NodeDefinition_DefensivelyCopiesInputLists()
        {
            var node = ChapterOneCareerContent.CreateFoundation().Nodes[0];
            var objectives = new List<CareerObjective> { new CareerObjective("a", "A", 1d) };
            var rewards = new List<CareerReward> { new CareerReward(coins: 1) };
            var definition = new CareerNodeDefinition(node, objectives, rewards);

            objectives.Add(new CareerObjective("b", "B", 1d));
            rewards.Add(new CareerReward(spirit: 1));

            Assert.That(definition.Objectives.Count, Is.EqualTo(1));
            Assert.That(definition.Rewards.Count, Is.EqualTo(1));
        }
    }
}
