using System;
using System.Collections.Generic;
using Afareet.CareerRuntime;
using Afareet.GarageRuntime;
using Afareet.Race;
using Afareet.Vehicle;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Afareet.Core
{
    public sealed class CareerBossVehicleRuntimeController : ICareerBossVehicleRuntime
    {
        private const float MinimumStatMultiplier = .90f;
        private const float MaximumStatMultiplier = 1.10f;
        private const string MissingProductionAssetRequest = "EXT-ASSET-002";

        private readonly GarageCatalog catalog;
        private readonly RaceDirector race;
        private readonly ArcadeCarController activeBossRival;
        private readonly List<GameObject> proceduralVisualRoots = new List<GameObject>();
        private GameObject productionVisualRoot;

        public string ActiveBossVehicleId { get; private set; }
        public bool UsingProductionVisual => productionVisualRoot != null;

        public CareerBossVehicleRuntimeController(
            GarageCatalog garageCatalog,
            RaceDirector raceDirector,
            IReadOnlyList<ArcadeCarController> registeredRivals)
        {
            catalog = garageCatalog ?? throw new ArgumentNullException(nameof(garageCatalog));
            race = raceDirector != null ? raceDirector : throw new ArgumentNullException(nameof(raceDirector));
            if (registeredRivals == null) throw new ArgumentNullException(nameof(registeredRivals));
            if (registeredRivals.Count == 0 || registeredRivals[0] == null)
                throw new ArgumentException("Boss runtime requires at least one registered rival.", nameof(registeredRivals));

            // Boss challenge configuration activates exactly one rival. Reuse the first registered
            // physics/AI root and project the stable boss identity onto it; this preserves the
            // generic rival ordering used by Circuit/Elimination and avoids mutating Race internals.
            activeBossRival = registeredRivals[0];
            for (var index = 0; index < activeBossRival.transform.childCount; index++)
            {
                var child = activeBossRival.transform.GetChild(index);
                if (child != null)
                    proceduralVisualRoots.Add(child.gameObject);
            }
        }

        public bool ApplyBossVehicle(string bossVehicleId)
        {
            if (string.IsNullOrWhiteSpace(bossVehicleId))
                throw new ArgumentException("Career boss vehicle id is required.", nameof(bossVehicleId));
            EnsureSafePhase();

            var definition = catalog.GetRequired(bossVehicleId);
            if (string.IsNullOrWhiteSpace(definition.PreviewResourcePath))
                throw new InvalidOperationException($"Boss vehicle '{bossVehicleId}' has no production visual resource binding.");
            var normalized = catalog.NormalizeStats(bossVehicleId);
            var profile = ProjectPerformance(normalized);
            var productionPrefab = Resources.Load<GameObject>(definition.PreviewResourcePath);

            if (StringComparer.Ordinal.Equals(ActiveBossVehicleId, bossVehicleId))
            {
                activeBossRival.SetVehiclePerformanceProfile(profile);
                return false;
            }

            ClearRuntimeState();
            activeBossRival.SetVehiclePerformanceProfile(profile);
            ActiveBossVehicleId = definition.Id;

            if (productionPrefab != null)
            {
                SetProceduralVisualsActive(false);
                productionVisualRoot = Object.Instantiate(productionPrefab, activeBossRival.transform, false);
                productionVisualRoot.name = $"PRODUCTION BOSS VISUAL // {definition.Id}";
                Debug.Log(
                    $"AFAREET_CAREER_BOSS_VEHICLE_APPLIED id={definition.Id} visual=production " +
                    $"resource={definition.PreviewResourcePath}");
            }
            else
            {
                SetProceduralVisualsActive(true);
                Debug.LogWarning(
                    $"AFAREET_CAREER_BOSS_VEHICLE_FALLBACK id={definition.Id} visual=procedural " +
                    $"missingResource={definition.PreviewResourcePath} request={MissingProductionAssetRequest}");
            }

            return true;
        }

        public bool ClearBossVehicle()
        {
            EnsureSafePhase();
            if (ActiveBossVehicleId == null && productionVisualRoot == null)
                return false;
            ClearRuntimeState();
            return true;
        }

        private void ClearRuntimeState()
        {
            activeBossRival.ResetVehiclePerformanceProfile();
            if (productionVisualRoot != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(productionVisualRoot);
                else
                    Object.DestroyImmediate(productionVisualRoot);
                productionVisualRoot = null;
            }
            SetProceduralVisualsActive(true);
            ActiveBossVehicleId = null;
        }

        private static VehiclePerformanceProfile ProjectPerformance(GarageNormalizedStats stats)
        {
            return new VehiclePerformanceProfile(
                Scale(stats.Acceleration),
                Scale(stats.TopSpeed),
                Scale(stats.Handling),
                Scale(stats.Handling),
                Scale(stats.Drift));
        }

        private static double Scale(float normalized)
        {
            if (float.IsNaN(normalized) || float.IsInfinity(normalized) || normalized < 0f || normalized > 1f)
                throw new ArgumentOutOfRangeException(nameof(normalized));
            return Mathf.Lerp(MinimumStatMultiplier, MaximumStatMultiplier, normalized);
        }

        private void SetProceduralVisualsActive(bool active)
        {
            for (var index = 0; index < proceduralVisualRoots.Count; index++)
            {
                var visual = proceduralVisualRoots[index];
                if (visual != null)
                    visual.SetActive(active);
            }
        }

        private void EnsureSafePhase()
        {
            if (race.Phase == RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing)
                throw new InvalidOperationException("Career BossVehicleId cannot change during countdown or active racing.");
        }
    }
}
