using System.Collections.Generic;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class ProductionRaceIntegrationTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        [Test]
        public void ConfigureBuildsSolidTrackEdgesAndPlayerOffRoadMonitor()
        {
            var track = BuildSquareTrack();
            var player = CreateCar("Player");
            var directorObject = NewObject("Race Director");
            var director = directorObject.AddComponent<RaceDirector>();

            director.Configure(player, track);

            var boundaryRoot = directorObject.transform.Find("TRACK BOUNDARY EDGES");
            Assert.That(boundaryRoot, Is.Not.Null);
            Assert.That(boundaryRoot.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(track.Waypoints.Count * 2));
            Assert.That(player.GetComponent<TrackBoundaryMonitor>(), Is.Not.Null);
        }

        [Test]
        public void RegisterRivalConfiguresRecoveryAndOffRoadMonitoring()
        {
            var track = BuildSquareTrack();
            var player = CreateCar("Player");
            var rival = CreateCar("Rival");
            rival.gameObject.AddComponent<AiRacer>();
            var director = NewObject("Race Director").AddComponent<RaceDirector>();
            director.Configure(player, track);

            director.RegisterRival(rival);

            Assert.That(rival.GetComponent<TrackBoundaryMonitor>(), Is.Not.Null);
            var reset = rival.GetComponent<RivalResetController>();
            Assert.That(reset, Is.Not.Null);
            Assert.DoesNotThrow(() => reset.SetActive(true));
            Assert.That(reset.Active, Is.True);
            reset.SetActive(false);
            Assert.That(reset.Active, Is.False);
        }

        [Test]
        public void AiInputCarriesExplicitBrakeState()
        {
            var car = CreateCar("Brake Contract");
            car.SetAiInput(.6f, .2f, false, false, true);
            Assert.That(car.CurrentBrakeInput, Is.True);

            car.SetAiInput(.6f, .2f, false, false);
            Assert.That(car.CurrentBrakeInput, Is.False);
        }

        [Test]
        public void LookaheadChoosesBrakingForFastSharpCorner()
        {
            var waypoints = new List<Transform>
            {
                Waypoint("A", new Vector3(0f, 0f, 0f)),
                Waypoint("B", new Vector3(20f, 0f, 0f)),
                Waypoint("C", new Vector3(20f, 0f, 20f)),
                Waypoint("D", new Vector3(0f, 0f, 20f))
            };

            var plan = RacingLineLookahead.Plan(waypoints, 1, 130f, 2);

            Assert.That(plan.SpeedPlan.Severity01, Is.GreaterThan(.5f));
            Assert.That(plan.SpeedPlan.Brake01, Is.GreaterThan(0f));
            Assert.That(plan.UseNitro, Is.False);
        }

        private TrackRuntime BuildSquareTrack()
        {
            var track = new TrackRuntime();
            track.Waypoints.Add(Waypoint("W0", new Vector3(0f, 0f, 0f)));
            track.Waypoints.Add(Waypoint("W1", new Vector3(20f, 0f, 0f)));
            track.Waypoints.Add(Waypoint("W2", new Vector3(20f, 0f, 20f)));
            track.Waypoints.Add(Waypoint("W3", new Vector3(0f, 0f, 20f)));
            for (var i = 0; i < track.Waypoints.Count; i++)
            {
                var next = track.Waypoints[(i + 1) % track.Waypoints.Count];
                track.Waypoints[i].rotation = Quaternion.LookRotation((next.position - track.Waypoints[i].position).normalized);
            }
            return track;
        }

        private ArcadeCarController CreateCar(string name)
        {
            var obj = NewObject(name);
            obj.AddComponent<Rigidbody>();
            return obj.AddComponent<ArcadeCarController>();
        }

        private Transform Waypoint(string name, Vector3 position)
        {
            var obj = NewObject(name);
            obj.transform.position = position;
            return obj.transform;
        }

        private GameObject NewObject(string name)
        {
            var obj = new GameObject(name);
            created.Add(obj);
            return obj;
        }
    }
}
