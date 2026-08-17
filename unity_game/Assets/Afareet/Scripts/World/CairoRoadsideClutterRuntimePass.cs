using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Finds authored Cairo building roots after track generation and decorates each once with
    /// tracked roadside-clutter Resources. It retries undiscovered/temporarily unstaged buildings
    /// but never falls back to generated geometry.
    /// </summary>
    public sealed class CairoRoadsideClutterRuntimePass : MonoBehaviour
    {
        private const float InitialScanDelaySeconds = .75f;
        private const float RescanSeconds = 1.0f;
        private readonly HashSet<Transform> decoratedBuildings = new();
        private float nextScanAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CairoRoadsideClutterRuntimePass>() != null)
                return;

            var host = new GameObject("AFAREET UART005 ROADSIDE CLUTTER PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoRoadsideClutterRuntimePass>();
        }

        private void Awake()
        {
            nextScanAt = Time.unscaledTime + InitialScanDelaySeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt) return;
            nextScanAt = Time.unscaledTime + RescanSeconds;
            DecoratePendingBuildings();
        }

        private void DecoratePendingBuildings()
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate == null ||
                    !candidate.name.StartsWith("AUTHORED CAIRO BUILDING", StringComparison.Ordinal))
                    continue;

                if (decoratedBuildings.Contains(candidate))
                    continue;

                if (CairoAuthoredRoadsideClutter.TryDecorateBuilding(candidate))
                    decoratedBuildings.Add(candidate);
            }
        }
    }
}
