using System.Collections;
using Afareet.Race;
using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Afareet.Tests.PlayMode
{
    public sealed class VehicleRaceTransientResetPlayModeTests
    {
        private GameObject root;
        private GameObject carObject;
        private ArcadeCarController car;
        private Rigidbody body;
        private ArcadeCarConfig config;
        private OneLapRaceTracker lap;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Vehicle-Race-Transient-Reset-Test");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Asphalt Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);

            carObject = new GameObject("Player-Race-Transient-Reset-Test");
            carObject.transform.SetParent(root.transform, false);
            carObject.transform.position = new Vector3(0f, 0.65f, 0f);

            body = carObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            car = carObject.AddComponent<ArcadeCarController>();

            config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            config.nitroMinimumSpeedKph = 0f;
            car.Configure(config);

            var checkpoints = carObject.AddComponent<RacerCheckpointTracker>();
            checkpoints.Configure(4, 1);
            lap = carObject.AddComponent<OneLapRaceTracker>();
            lap.Configure(4);

            carObject.GetComponent<ArcadeGroundSurfaceSensor>().ProbeNow();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            if (config != null)
                Object.DestroyImmediate(config);
        }

        [UnityTest]
        public IEnumerator LapStart_RefillsNitroAndClearsDriftCooldownAndInputs()
        {
            body.linearVelocity = carObject.transform.forward * 20f + carObject.transform.right * 4f;
            car.SetAiInput(1f, 0.65f, true, true, false);

            yield return new WaitForFixedUpdate();

            Assert.That(car.NitroEnergy, Is.LessThan(1f));
            Assert.That(car.DriftEnergy, Is.GreaterThan(0f));

            car.SetAiInput(0f, 0f, false, false, false);
            yield return new WaitForFixedUpdate();

            Assert.That(car.NitroCooldownRemaining, Is.GreaterThan(0f));

            lap.StartRace();

            Assert.That(car.NitroEnergy, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(car.NitroCooldownRemaining, Is.Zero.Within(0.0001f));
            Assert.That(car.DriftEnergy, Is.Zero.Within(0.0001f));
            Assert.That(car.IsDrifting, Is.False);
            Assert.That(car.CurrentThrottleInput, Is.Zero.Within(0.0001f));
            Assert.That(car.CurrentSteerInput, Is.Zero.Within(0.0001f));
            Assert.That(car.CurrentBrakeInput, Is.False);
        }

        [Test]
        public void LapStart_ClearsRecoveryLockBeforeFreshPlayerInput()
        {
            car.ResetToSpawn();
            Assert.That(car.RecoveryInputLockRemaining, Is.GreaterThan(0f));

            lap.StartRace();

            Assert.That(car.RecoveryInputLockRemaining, Is.Zero.Within(0.0001f));

            car.SetPlayerInput(1f, 0.5f, false, false, false);
            Assert.That(car.CurrentThrottleInput, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(car.CurrentSteerInput, Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
