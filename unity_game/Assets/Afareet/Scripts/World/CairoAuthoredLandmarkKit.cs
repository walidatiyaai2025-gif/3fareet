using System;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Runtime adapter for tracked UART-006 authored Cairo landmark OBJ sources.
    /// No mesh is procedurally constructed here.
    /// </summary>
    public static class CairoAuthoredLandmarkKit
    {
        private const string ResourceRoot = "Art/Architecture/CairoLandmarks/Generated";
        private const string PyramidPath = ResourceRoot + "/SM_Landmark_GizaPyramid_A";
        private const string MinaretPath = ResourceRoot + "/SM_Landmark_Minaret_A";
        private const string DomeGatePath = ResourceRoot + "/SM_Landmark_DomeGate_A";
        private const string BridgePath = ResourceRoot + "/SM_Landmark_BridgeGantry_A";
        private static bool activationLogged;

        public static bool TryBuildMinarets(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var source = Resources.Load<GameObject>(MinaretPath);
            if (source == null) return Missing(MinaretPath);

            var root = Root("AUTHORED Spirit Minaret Cluster", anchor, -anchor.right * 26f + Vector3.up * .2f);
            Create(source, root, "Authored Minaret Left", new Vector3(-6f, 0f, 0f), new Vector3(.82f, .82f, .82f), dark, purple, gold);
            Create(source, root, "Authored Minaret Center", Vector3.zero, new Vector3(1.02f, 1.08f, 1.02f), dark, cyan, gold);
            Create(source, root, "Authored Minaret Right", new Vector3(6f, 0f, 0f), new Vector3(.72f, .68f, .72f), dark, purple, gold);
            LogActivation();
            return true;
        }

        public static bool TryBuildDomeGate(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var source = Resources.Load<GameObject>(DomeGatePath);
            if (source == null) return Missing(DomeGatePath);
            var root = Root("AUTHORED Neon Dome Gate", anchor, anchor.right * 24f);
            Create(source, root, "Authored Dome Gate", Vector3.zero, Vector3.one, dark, purple, gold, cyan);
            LogActivation();
            return true;
        }

        public static bool TryBuildPyramidPair(Transform anchor, Material dark, Material purple, Material gold)
        {
            var source = Resources.Load<GameObject>(PyramidPath);
            if (source == null) return Missing(PyramidPath);
            var root = Root("AUTHORED Pyramid Horizon Pair", anchor, -anchor.right * 40f);
            Create(source, root, "Authored Pyramid Major", new Vector3(-9f, 0f, 1.5f), Vector3.one, dark, purple, gold);
            Create(source, root, "Authored Pyramid Minor", new Vector3(8f, 0f, -2f), new Vector3(.71f, .71f, .71f), dark, purple, gold);
            LogActivation();
            return true;
        }

        public static bool TryBuildBridgeGantry(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var source = Resources.Load<GameObject>(BridgePath);
            if (source == null) return Missing(BridgePath);
            var root = Root("AUTHORED Cairo Bridge Gantry", anchor, Vector3.zero);
            Create(source, root, "Authored Bridge Gantry", Vector3.zero, Vector3.one, dark, purple, gold, cyan);
            LogActivation();
            return true;
        }

        private static Transform Root(string name, Transform anchor, Vector3 offset)
        {
            var root = new GameObject(name).transform;
            root.position = anchor.position + offset;
            root.rotation = anchor.rotation;
            return root;
        }

        private static void Create(
            GameObject source,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material baseMaterial,
            Material spiritMaterial,
            Material goldMaterial,
            Material cyanMaterial = null)
        {
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;
            ApplyMaterials(instance, baseMaterial, spiritMaterial, goldMaterial, cyanMaterial);
        }

        private static void ApplyMaterials(GameObject instance, Material baseMaterial, Material spiritMaterial, Material goldMaterial, Material cyanMaterial)
        {
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var n = renderer.gameObject.name ?? string.Empty;
                var selected = baseMaterial;
                if (Contains(n, "Spire") || Contains(n, "Crest") || Contains(n, "Trim") || Contains(n, "Crown"))
                    selected = goldMaterial ?? spiritMaterial ?? baseMaterial;
                else if (Contains(n, "Dome") || Contains(n, "Neon") || Contains(n, "Arch"))
                    selected = spiritMaterial ?? baseMaterial;
                else if (cyanMaterial != null && (Contains(n, "Ring") || Contains(n, "Rail")))
                    selected = cyanMaterial;

                if (selected == null) continue;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = selected;
                renderer.sharedMaterials = bindings;
            }
        }

        private static bool Contains(string value, string token) =>
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool Missing(string path)
        {
            Debug.LogError($"AFAREET_UART006_AUTHORED_RESOURCE_MISSING path={path}");
            return false;
        }

        private static void LogActivation()
        {
            if (activationLogged) return;
            activationLogged = true;
            Debug.Log("AFAREET_UART006_AUTHORED_LANDMARKS_ACTIVE geometry=tracked-obj resources=staged");
        }
    }
}
