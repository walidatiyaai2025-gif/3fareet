using System.Collections;
using Afareet.Race;
using Afareet.Vehicle;
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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            directorObject = new GameObject("RaceDirector-PlayModeTest");
            director = directorObject.AddComponent<RaceDirector>();

            playerObject = CreateCar("Player-PlayModeTest", out playerBody, out player);
            rivalObject = CreateCar("Rival-PlayModeTest", out rivalBody, out rival);
            rivalAi = rivalObject.AddComponent<AiRacer>();

            director.Configure(player, null);
            director.RegisterRival(rival);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (directorObject != null) Object.Destroy(directorObject);
            if (playerObject != null) Object.Destroy(playerObject);
            if (rivalObject != null) Object.Destroy(rivalObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_FreezesPlayerAndRivalBeforeStart()
        {
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

            Assert.That(director.IsStarted, Is.True);
            Assert.That(director.CountdownText, Is.EqualTo("3"));
            Assert.That(playerBody.isKinematic, Is.True);
            Assert.That(rivalBody.isKinematic, Is.True);

            yield return new WaitForSeconds(1f);

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

            Assert.That(playerBody.isKinematic, Is.False);
            Assert.That(player.AcceptsPlayerInput, Is.True);
            Assert.That(rivalBody.isKinematic, Is.False);
            Assert.That(rivalAi.enabled, Is.True);
            Assert.That(director.CountdownText, Is.EqualTo("GO!"));
            Assert.That(director.RaceTime, Is.EqualTo(0f).Within(0.05f));

            yield return new WaitForSeconds(1f);

            Assert.That(director.CountdownText, Is.Empty);
            Assert.That(director.RaceTime, Is.GreaterThan(0f));
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
