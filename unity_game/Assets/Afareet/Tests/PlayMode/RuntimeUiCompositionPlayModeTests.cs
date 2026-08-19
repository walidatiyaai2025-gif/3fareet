using System.Collections;
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
            var player = playerHost.AddComponent<ArcadeCarController>();
            var race = runtimeRoot.AddComponent<RaceDirector>();

            var firstHud = ProductionRaceHud.EnsureInstalled(runtimeRoot.transform);
            var secondHud = ProductionRaceHud.EnsureInstalled(runtimeRoot.transform);
            var firstOverlay = ProductionRaceFlowOverlay.EnsureInstalled(runtimeRoot.transform);
            var secondOverlay = ProductionRaceFlowOverlay.EnsureInstalled(runtimeRoot.transform);

            firstHud.Configure(player, race);
            firstOverlay.Configure(race);

            Assert.That(secondHud, Is.SameAs(firstHud));
            Assert.That(secondOverlay, Is.SameAs(firstOverlay));
            Assert.That(firstHud.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstOverlay.transform.parent, Is.SameAs(runtimeRoot.transform));
            Assert.That(firstHud.HasRuntimeBinding, Is.True);
            Assert.That(firstOverlay.HasRuntimeBinding, Is.True);
            Assert.That(Object.FindObjectsByType<ProductionRaceHud>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ProductionRaceFlowOverlay>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

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
        }
    }
}
