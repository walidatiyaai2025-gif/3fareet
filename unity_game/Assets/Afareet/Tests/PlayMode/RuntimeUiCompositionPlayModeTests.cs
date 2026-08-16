using System.Collections;
using Afareet.CareerRuntime;
using Afareet.Race;
using Afareet.UI;
using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Afareet.Tests.PlayMode
{
    public sealed class RuntimeUiCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProductionUiInstallers_AreIdempotent_AndBindExplicitRuntimeReferences()
        {
            DestroyExistingUi();
            yield return null;

            var runtimeRoot = new GameObject("TEST AFAREET RUNTIME");
            var playerHost = new GameObject("TEST PLAYER");
            playerHost.AddComponent<Rigidbody>();
            var player = playerHost.AddComponent<ArcadeCarController>();
            var race = runtimeRoot.AddComponent<RaceDirector>();
            var career = runtimeRoot.AddComponent<CareerGameSession>();
            var input = runtimeRoot.AddComponent<ProductionRaceInputController>();
            input.Configure(player, race);

            var firstHud = ProductionRaceHud.EnsureInstalled(runtimeRoot.transform);
            var secondHud = ProductionRaceHud.EnsureInstalled(runtimeRoot.transform);
            var firstOverlay = ProductionRaceFlowOverlay.EnsureInstalled(runtimeRoot.transform);
            var secondOverlay = ProductionRaceFlowOverlay.EnsureInstalled(runtimeRoot.transform);
            var firstControls = ProductionRaceControlsOverlay.EnsureInstalled(runtimeRoot.transform);
            var secondControls = ProductionRaceControlsOverlay.EnsureInstalled(runtimeRoot.transform);
            var firstBriefing = ProductionCareerBriefingOverlay.EnsureInstalled(runtimeRoot.transform);
            var secondBriefing = ProductionCareerBriefingOverlay.EnsureInstalled(runtimeRoot.transform);

            firstHud.Configure(player, race, career);
            firstOverlay.Configure(race, career);
            firstControls.Configure(race, input);
            firstBriefing.Configure(career, race);

            Assert.That(secondHud, Is.SameAs(firstHud));
            Assert.That(secondOverlay, Is.SameAs(firstOverlay));
            Assert.That(secondControls, Is.SameAs(firstControls));
            Assert.That(secondBriefing, Is.SameAs(firstBriefing));
            Assert.That(firstHud.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstOverlay.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstControls.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstBriefing.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstHud.HasRuntimeBinding, Is.True);
            Assert.That(firstOverlay.HasRuntimeBinding, Is.True);
            Assert.That(runtimeRoot.GetComponent<ProductionRaceInputController>(), Is.SameAs(input));
            Assert.That(runtimeRoot.GetComponent<PrototypeHud>(), Is.Null);
            Assert.That(Object.FindObjectsByType<ProductionRaceHud>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ProductionRaceFlowOverlay>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ProductionRaceControlsOverlay>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ProductionCareerBriefingOverlay>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            Object.Destroy(runtimeRoot);
            Object.Destroy(playerHost);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            DestroyExistingUi();
            var testRoot = GameObject.Find("TEST AFAREET RUNTIME");
            if (testRoot != null) Object.DestroyImmediate(testRoot);
            var testPlayer = GameObject.Find("TEST PLAYER");
            if (testPlayer != null) Object.DestroyImmediate(testPlayer);
        }

        private static void DestroyExistingUi()
        {
            var huds = Object.FindObjectsByType<ProductionRaceHud>(FindObjectsSortMode.None);
            for (var i = 0; i < huds.Length; i++)
                if (huds[i] != null) Object.DestroyImmediate(huds[i].gameObject);

            var overlays = Object.FindObjectsByType<ProductionRaceFlowOverlay>(FindObjectsSortMode.None);
            for (var i = 0; i < overlays.Length; i++)
                if (overlays[i] != null) Object.DestroyImmediate(overlays[i].gameObject);

            var controls = Object.FindObjectsByType<ProductionRaceControlsOverlay>(FindObjectsSortMode.None);
            for (var i = 0; i < controls.Length; i++)
                if (controls[i] != null) Object.DestroyImmediate(controls[i].gameObject);

            var briefings = Object.FindObjectsByType<ProductionCareerBriefingOverlay>(FindObjectsSortMode.None);
            for (var i = 0; i < briefings.Length; i++)
                if (briefings[i] != null) Object.DestroyImmediate(briefings[i].gameObject);
        }
    }
}
