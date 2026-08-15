using System;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Runtime presentation adapter for UART-005 tracked authored geometry.
    /// Geometry comes from Unity-imported OBJ resources staged from tracked sources;
    /// this class only applies authored transforms/materials and never constructs meshes.
    /// </summary>
    public static class CairoAuthoredStreetKit
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private const string FacadePath = ResourceRoot + "/SM_Env_CairoFacade_A";
        private const string AwningPath = ResourceRoot + "/SM_Env_CairoAwning_A";
        private const string LampPath = ResourceRoot + "/SM_Prop_CairoLamp_A";
        private const string BarrierPath = ResourceRoot + "/SM_Prop_CairoBarrier_A";
        private const string RoadPath = ResourceRoot + "/SM_Track_CairoRoad_A";

        private static bool activationLogged;

        public static bool TryCreateRoadSegment(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            float length,
            Material asphaltMaterial,
            Material curbMaterial,
            Material edgeMaterial)
        {
            var source = Resources.Load<GameObject>(RoadPath);
            if (source == null)
            {
                Missing(RoadPath);
                return false;
            }

            var road = Object.Instantiate(source, parent, false);
            road.name = "AUTHORED CAIRO ROAD SEGMENT";
            road.transform.SetPositionAndRotation(position, rotation);
            road.transform.localScale = new Vector3(1f, 1f, Mathf.Max(.25f, length));
            ApplyRoadMaterials(road, asphaltMaterial, curbMaterial, edgeMaterial);
            LogActivation();
            return true;
        }

        public static bool TryCreateBuilding(
            Transform parent,
            Vector3 groundPosition,
            Quaternion rotation,
            float width,
            float height,
            Material facadeMaterial,
            Material accentMaterial)
        {
            var facade = Resources.Load<GameObject>(FacadePath);
            if (facade == null)
            {
                Missing(FacadePath);
                return false;
            }

            var root = new GameObject("AUTHORED CAIRO BUILDING").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(groundPosition, rotation);

            var tierCount = Mathf.Max(1, Mathf.CeilToInt(height / 5f));
            for (var tier = 0; tier < tierCount; tier++)
            {
                var tierBase = tier * 5f;
                var tierHeight = Mathf.Min(5f, height - tierBase);
                if (tierHeight <= .1f) break;

                var scaleY = tierHeight / 5f;
                CreateFacade(facade, root, "Facade Front", new Vector3(-width * .5f, tierBase, -width * .5f), Quaternion.identity, new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, "Facade Back", new Vector3(width * .5f, tierBase, width * .5f), Quaternion.Euler(0f, 180f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, "Facade Left", new Vector3(-width * .5f, tierBase, width * .5f), Quaternion.Euler(0f, -90f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, "Facade Right", new Vector3(width * .5f, tierBase, -width * .5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
            }

            var awning = Resources.Load<GameObject>(AwningPath);
            if (awning != null && width >= 5f)
            {
                var canopy = Object.Instantiate(awning, root, false);
                canopy.name = "Authored Cairo Awning";
                canopy.transform.localPosition = new Vector3(-Mathf.Min(width * .32f, 1.5f), 1.55f, -width * .5f - .06f);
                canopy.transform.localRotation = Quaternion.identity;
                canopy.transform.localScale = new Vector3(Mathf.Min(1f, width / 6f), 1f, .78f);
                ApplyMaterial(canopy, accentMaterial);
            }

            LogActivation();
            return true;
        }

        public static bool TryCreateLamp(Transform parent, Vector3 position, Quaternion rotation, Material material)
        {
            var source = Resources.Load<GameObject>(LampPath);
            if (source == null)
            {
                Missing(LampPath);
                return false;
            }

            var lamp = Object.Instantiate(source, parent, false);
            lamp.name = "AUTHORED CAIRO LAMP";
            lamp.transform.SetPositionAndRotation(position, rotation);
            lamp.transform.localScale = Vector3.one;
            ApplyMaterial(lamp, material);

            var lightHost = new GameObject("Lamp Practical Light");
            lightHost.transform.SetParent(lamp.transform, false);
            lightHost.transform.localPosition = new Vector3(.1f, 2.86f, .1f);
            var light = lightHost.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2.2f;
            light.color = material != null && material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor")
                : new Color(.15f, .75f, 1f);
            light.shadows = LightShadows.None;

            LogActivation();
            return true;
        }

        public static bool TryCreateBarrier(Transform parent, Vector3 position, Quaternion rotation, Material material, float lengthScale = 1f)
        {
            var source = Resources.Load<GameObject>(BarrierPath);
            if (source == null)
            {
                Missing(BarrierPath);
                return false;
            }

            var barrier = Object.Instantiate(source, parent, false);
            barrier.name = "AUTHORED CAIRO BARRIER";
            barrier.transform.SetPositionAndRotation(position, rotation);
            barrier.transform.localScale = new Vector3(Mathf.Max(.5f, lengthScale), 1f, 1f);
            ApplyMaterial(barrier, material);
            LogActivation();
            return true;
        }

        private static void CreateFacade(
            GameObject source,
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            var panel = Object.Instantiate(source, parent, false);
            panel.name = name;
            panel.transform.localPosition = localPosition;
            panel.transform.localRotation = localRotation;
            panel.transform.localScale = localScale;
            ApplyMaterial(panel, material);
        }

        private static void ApplyRoadMaterials(GameObject instance, Material asphaltMaterial, Material curbMaterial, Material edgeMaterial)
        {
            if (instance == null) return;
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var objectName = renderer.gameObject.name ?? string.Empty;
                var target = asphaltMaterial;
                if (objectName.IndexOf("Curb", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("Sidewalk", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = curbMaterial ?? asphaltMaterial;
                }
                else if (objectName.IndexOf("Drain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         objectName.IndexOf("Edge_Blade", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = edgeMaterial ?? curbMaterial ?? asphaltMaterial;
                }

                if (target == null) continue;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = target;
                renderer.sharedMaterials = bindings;
            }
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            if (instance == null || material == null) return;
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = material;
                renderer.sharedMaterials = bindings;
            }
        }

        private static void Missing(string path)
        {
            Debug.LogError($"AFAREET_UART005_AUTHORED_RESOURCE_MISSING path={path}");
        }

        private static void LogActivation()
        {
            if (activationLogged) return;
            activationLogged = true;
            Debug.Log("AFAREET_UART005_AUTHORED_RUNTIME_ACTIVE geometry=tracked-obj resources=staged");
        }
    }
}
