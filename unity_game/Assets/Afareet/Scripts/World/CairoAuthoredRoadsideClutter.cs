using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Places tracked UART-005 roadside-clutter Resources around authored Cairo buildings.
    /// This adapter never creates Mesh data or primitives; it only instantiates Unity-imported
    /// source models staged from the tracked Cairo street-kit source directory.
    /// </summary>
    public static class CairoAuthoredRoadsideClutter
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private static readonly string[] ClutterPaths =
        {
            ResourceRoot + "/SM_Prop_CairoPlanter_A",
            ResourceRoot + "/SM_Prop_CairoCrateStack_A",
            ResourceRoot + "/SM_Prop_CairoCafeTable_A"
        };

        private static readonly HashSet<string> MissingLogged = new(StringComparer.Ordinal);
        private static bool activationLogged;

        public static bool TryDecorateBuilding(Transform buildingRoot)
        {
            if (buildingRoot == null) throw new ArgumentNullException(nameof(buildingRoot));

            var frontFacade = FindFrontFacade(buildingRoot);
            if (frontFacade == null)
                return false;

            var width = Mathf.Max(5f, Mathf.Abs(frontFacade.localPosition.z) * 2f);
            var primaryVariant = StableVariantIndex(buildingRoot.position, width, ClutterPaths.Length, 73);
            var primarySource = Resources.Load<GameObject>(ClutterPaths[primaryVariant]);
            if (primarySource == null)
            {
                Missing(ClutterPaths[primaryVariant]);
                return false;
            }

            var frontZ = -width * .5f;
            var primary = UnityEngine.Object.Instantiate(primarySource, buildingRoot, false);
            primary.name = $"Authored Cairo Roadside Clutter V{primaryVariant + 1}";
            primary.transform.localPosition = PrimaryPosition(primaryVariant, width, frontZ);
            primary.transform.localRotation = PrimaryRotation(primaryVariant);
            primary.transform.localScale = Vector3.one;

            // Wider shopfronts receive one planter on the opposite side unless the planter is
            // already the primary selection. This remains deterministic and source-backed.
            if (width >= 7.5f && primaryVariant != 0)
            {
                var planterSource = Resources.Load<GameObject>(ClutterPaths[0]);
                if (planterSource == null)
                {
                    Missing(ClutterPaths[0]);
                    return false;
                }

                var planter = UnityEngine.Object.Instantiate(planterSource, buildingRoot, false);
                planter.name = "Authored Cairo Roadside Planter Secondary";
                planter.transform.localPosition = new Vector3(-width * .30f, 0f, frontZ - .72f);
                planter.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                planter.transform.localScale = Vector3.one * .92f;
            }

            if (!activationLogged)
            {
                activationLogged = true;
                Debug.Log(
                    "AFAREET_UART005_ROADSIDE_CLUTTER_ACTIVE sources=3 " +
                    "selection=stable-building-hash geometry=tracked-obj playerMaterials=source-authored primitives=false");
            }

            return true;
        }

        private static Transform FindFrontFacade(Transform buildingRoot)
        {
            foreach (var child in buildingRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name.StartsWith("Facade Front V", StringComparison.Ordinal))
                    return child;
            }
            return null;
        }

        private static Vector3 PrimaryPosition(int variant, float width, float frontZ)
        {
            switch (variant)
            {
                case 0:
                    return new Vector3(width * .30f, 0f, frontZ - .72f);
                case 1:
                    return new Vector3(width * .27f, 0f, frontZ - .70f);
                default:
                    return new Vector3(0f, 0f, frontZ - 1.05f);
            }
        }

        private static Quaternion PrimaryRotation(int variant)
        {
            return variant == 2
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.Euler(0f, 180f, 0f);
        }

        private static int StableVariantIndex(Vector3 position, float width, int count, int salt)
        {
            if (count <= 1) return 0;
            unchecked
            {
                var x = Mathf.RoundToInt(position.x * 2f);
                var z = Mathf.RoundToInt(position.z * 2f);
                var w = Mathf.RoundToInt(width * 10f);
                var hash = (x * 73856093) ^ (z * 19349663) ^ (w * 83492791) ^ salt;
                return (hash & int.MaxValue) % count;
            }
        }

        private static void Missing(string path)
        {
            if (!MissingLogged.Add(path)) return;
            Debug.LogError($"AFAREET_UART005_ROADSIDE_CLUTTER_RESOURCE_MISSING path={path}");
        }
    }
}
