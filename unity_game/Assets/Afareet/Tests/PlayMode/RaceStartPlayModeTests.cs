using System.Collections;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Afareet.Tests.PlayMode
{
    public sealed class RaceStartPlayModeTests
    {
        private GameObject directorObject;
        private GameObject playerObject;
        private GameObject rivalObject;
        private RaceDirector director;
        private ArcadeCarController player;
        private ArcadeCarController rival;
        private AiRacer rivalAi;
        private Rigidbody playerBody;
        private Rigidbody rivalBody;
        private TrackRuntime track;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            directorObject = new GameObject("RaceDirector-PlayModeTest");
            director = directorObject.AddComponent<RaceDirector>();

            playerObject = CreateCar("Player-PlayModeTest", out playerBody, out player);
            rivalObject = CreateCar("Rival-PlayModeTest", out rivalBody, out rival);
            rivalAi = rivalObject.AddComponent<AiRacer>();
            track = BuildTrack();
            rivalAi.Configure(track.Waypoints, 1);

            director.Configure(player, track);
            director.RegisterRival(rival);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (directorObject != null) Object.Destroy(directorObject);
            if (playerObject != null) Object.Destroy(playerObject);
            if (rivalObject != null) Object.Destroy(rivalObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_FreezesPlayerAndRivalBeforeStart()
        {
            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Ready));
            Assert.That(director.IsStarted, Is.False);
            Assert.That(player.AcceptsPlayerInput, Is.False);
            Assert.That(playerBody.isKinematic, Is.True);
            Assert.That(rivalBody.isKinematic, Is.True);
            Assert.That(rivalAi.enabled, Is.False);

            var playerStart = playerObject.transform.position;
            var rivalStart = rivalObject.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.That(playerObject.transform.position, Is.EqualTo(playerStart));
            Assert.That(rivalObject.transform.position, Is.EqualTo(rivalStart));
            Assert.That(director.RaceTime, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator StartRace_HoldsRacersDuringCountdown()
        {
            director.StartRace();

            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(director.IsStarted, Is.True);
            Assert.That(director.CountdownText, Is.EqualTo("3"));
            Assert.That(playerBody.isKinematic, Is.True);
            Assert.That(rivalBody.isKinematic, Is.True);

            yield return new WaitForSeconds(1f);

            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(director.RaceTime, Is.EqualTo(0f));
            Assert.That(player.AcceptsPlayerInput, Is.False);
            Assert.That(playerBody.isKinematic, Is.True);
            Assert.That(rivalBody.isKinematic, Is.True);
            Assert.That(rivalAi.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator StartRace_ReleasesRacersAtGoAndStartsClockAfterCountdown()
        {
            director.StartRace();

            yield return new WaitForSeconds(3.15f);

            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Racing));
            Assert.That(playerBody.isKinematic, Is.False);
            Assert.That(player.AcceptsPlayerInput, Is.True);
            Assert.That(rivalBody.isKinematic, Is.False);
            Assert.That(rivalAi.enabled, Is.True);
            Assert.That(director.CountdownText, Is.Empty);

            var timeAfterGo = director.RaceTime;
            Assert.That(timeAfterGo, Is.GreaterThanOrEqualTo(0f));

            yield return new WaitForSeconds(.5f);

            Assert.That(director.RaceTime, Is.GreaterThan(timeAfterGo));
        }

        private TrackRuntime BuildTrack()
        {
            var runtime = new TrackRuntime();
            var points = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint-PlayModeTest-{i}");
                waypoint.transform.SetParent(directorObject.transform, false);
                waypoint.transform.position = points[i];
                runtime.Waypoints.Add(waypoint.transform);
            }

            for (var i = 0; i < runtime.Waypoints.Count; i++)
            {
                var current = runtime.Waypoints[i];
                var next = runtime.Waypoints[(i + 1) % runtime.Waypoints.Count];
                current.rotation = Quaternion.LookRotation((next.position - current.position).normalized);
            }

            return runtime;
        }

        private static GameObject CreateCar(string name, out Rigidbody body, out ArcadeCarController controller)
        {
            var gameObject = new GameObject(name);
            body = gameObject.AddComponent<Rigidbody>();
            controller = gameObject.AddComponent<ArcadeCarController>();
            return gameObject;
        }
    }
}
