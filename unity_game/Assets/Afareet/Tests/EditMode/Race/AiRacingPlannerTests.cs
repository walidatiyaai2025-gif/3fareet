using System.Collections.Generic;
using Afareet.Race;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class AiRacingPlannerTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created) if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        [Test]
        public void StraightHasNoCornerSeverityOrBrake()
        {
            var severity = CornerSpeedPolicy.Severity(Vector3.zero, Vector3.forward * 10f, Vector3.forward * 20f);
            var plan = CornerSpeedPolicy.Plan(severity, 100f);
            Assert.That(severity, Is.EqualTo(0f).Within(.001f));
            Assert.That(plan.TargetSpeedKph, Is.EqualTo(125f).Within(.001f));
            Assert.That(plan.Brake01, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void NinetyDegreeCornerReducesTargetAndRequestsBrake()
        {
            var severity = CornerSpeedPolicy.Severity(Vector3.zero, Vector3.forward * 10f, new Vector3(10f, 0f, 10f));
            var plan = CornerSpeedPolicy.Plan(severity, 120f);
            Assert.That(severity, Is.GreaterThan(.8f));
            Assert.That(plan.TargetSpeedKph, Is.LessThan(70f));
            Assert.That(plan.Brake01, Is.GreaterThan(.5f));
        }

        [Test]
        public void StraightLookaheadAimsFartherAndAllowsNitro()
        {
            var line = Line(6);
            var plan = RacingLineLookahead.Plan(line, 2, 90f, 1);
            Assert.That(plan.AimWaypointIndex, Is.EqualTo(4));
            Assert.That(plan.UseNitro, Is.True);
        }

        [Test]
        public void UpcomingCornerCreatesBrakingZoneBeforeTurn()
        {
            var path = new List<Transform>
            {
                Point("W0", 0f, 0f), Point("W1", 0f, 10f), Point("W2", 10f, 10f),
                Point("W3", 20f, 10f), Point("W4", 30f, 10f)
            };
            var plan = RacingLineLookahead.Plan(path, 1, 120f, 2);
            Assert.That(plan.SpeedPlan.Severity01, Is.GreaterThan(.5f));
            Assert.That(plan.SpeedPlan.Brake01, Is.GreaterThan(0f));
            Assert.That(plan.UseNitro, Is.False);
        }

        [Test]
        public void SharpCornerAimsAtImmediateWaypoint()
        {
            var path = new List<Transform>
            {
                Point("W0", 0f, 0f), Point("W1", 0f, 10f),
                Point("W2", 10f, 10f), Point("W3", 20f, 10f)
            };
            var plan = RacingLineLookahead.Plan(path, 1, 110f, 1);
            Assert.That(plan.AimWaypointIndex, Is.EqualTo(1));
        }

        private List<Transform> Line(int count)
        {
            var result = new List<Transform>();
            for (var i = 0; i < count; i++) result.Add(Point($"W{i}", 0f, i * 10f));
            return result;
        }

        private Transform Point(string name, float x, float z)
        {
            var obj = new GameObject(name);
            obj.transform.position = new Vector3(x, 0f, z);
            created.Add(obj);
            return obj.transform;
        }
    }
}
