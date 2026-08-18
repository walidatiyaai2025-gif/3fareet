using System;
using System.Collections;
using Afareet.Core;
using Afareet.GarageRuntime;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Afareet.Tests.PlayMode.GarageRuntime
{
    public sealed class CareerGarageVehicleRuntimePlayModeTests
    {
        private GameObject root;
        private GameObject playerObject;
        private ArcadeCarController player;
        private RaceDirector race;
        private GarageCatalog catalog;
        private CareerGarageVehicleRuntimeController runtime;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("GARAGE LIVE EQUIP TEST ROOT");
            playerObject = new GameObject("GARAGE LIVE EQUIP TEST PLAYER");
            playerObject.transform.SetParent(root.transform, false);
            playerObject.AddComponent<Rigidbody>();
            player = playerObject.AddComponent<ArcadeCarController>();

            race = root.AddComponent<RaceDirector>();
            race.Configure(player, BuildTrack());
            catalog = GarageCatalog.CreateDefault();
            runtime = new CareerGarageVehicleRuntimeController(catalog, race, player);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyEquippedVehicle_ProjectsAuthoritativeStatsAndIsIdempotent()
        {
            var expected = GarageVehiclePerformanceProjection.Project(
                catalog.NormalizeStats(GarageCatalog.StarterVehicleId));

            Assert.That(runtime.ApplyEquippedVehicle(GarageCatalog.StarterVehicleId), Is.True);
            Assert.That(runtime.ActiveVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            AssertProfile(player.VehiclePerformanceProfile, expected);

            Assert.That(runtime.ApplyEquippedVehicle(GarageCatalog.StarterVehicleId), Is.False);
            AssertProfile(player.VehiclePerformanceProfile, expected);

            var wedgeExpected = GarageVehiclePerformanceProjection.Project(catalog.NormalizeStats("wedge_coupe"));
            Assert.That(runtime.ApplyEquippedVehicle("wedge_coupe"), Is.True);
            Assert.That(runtime.ActiveVehicleId, Is.EqualTo("wedge_coupe"));
            AssertProfile(player.VehiclePerformanceProfile, wedgeExpected);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CountdownGuard_FailsBeforeChangingLiveEquippedVehicle()
        {
            Assert.That(runtime.ApplyEquippedVehicle(GarageCatalog.StarterVehicleId), Is.True);
            var before = player.VehiclePerformanceProfile;

            race.StartRace();
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.Throws<InvalidOperationException>(() => runtime.ValidateApply("wedge_coupe"));
            Assert.Throws<InvalidOperationException>(() => runtime.ApplyEquippedVehicle("wedge_coupe"));

            Assert.That(runtime.ActiveVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            AssertProfile(player.VehiclePerformanceProfile, before);
            yield return null;
        }

        [Test]
        public void UnknownVehicle_FailsClosedWithoutChangingLiveState()
        {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => runtime.ValidateApply("missing_vehicle"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => runtime.ApplyEquippedVehicle("missing_vehicle"));
            Assert.That(runtime.ActiveVehicleId, Is.Null);
            AssertProfile(player.VehiclePerformanceProfile, VehiclePerformanceProfile.Identity);
        }

        private TrackRuntime BuildTrack()
        {
            var runtimeTrack = new TrackRuntime();
            var points = new[]
            {
                Vector3.zero,
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = new GameObject($"GarageWaypoint-{index}");
                waypoint.transform.SetParent(root.transform, false);
                waypoint.transform.position = points[index];
                runtimeTrack.Waypoints.Add(waypoint.transform);
            }

            for (var index = 0; index < runtimeTrack.Waypoints.Count; index++)
            {
                var current = runtimeTrack.Waypoints[index];
                var next = runtimeTrack.Waypoints[(index + 1) % runtimeTrack.Waypoints.Count];
                current.rotation = Quaternion.LookRotation((next.position - current.position).normalized);
            }

            return runtimeTrack;
        }

        private static void AssertProfile(VehiclePerformanceProfile actual, VehiclePerformanceProfile expected)
        {
            Assert.That(actual.AccelerationMultiplier, Is.EqualTo(expected.AccelerationMultiplier).Within(.000001d));
            Assert.That(actual.MaxSpeedMultiplier, Is.EqualTo(expected.MaxSpeedMultiplier).Within(.000001d));
            Assert.That(actual.SteeringAuthorityMultiplier, Is.EqualTo(expected.SteeringAuthorityMultiplier).Within(.000001d));
            Assert.That(actual.GripMultiplier, Is.EqualTo(expected.GripMultiplier).Within(.000001d));
            Assert.That(actual.DriftAuthorityMultiplier, Is.EqualTo(expected.DriftAuthorityMultiplier).Within(.000001d));
        }
    }
}
