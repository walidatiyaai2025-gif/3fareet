using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Runtime adapter for UART-006 tracked Cairo landmark OBJ sources.
    /// It instantiates Unity-imported authored resources and applies identity materials;
    /// it never constructs production landmark geometry from primitives.
    /// </summary>
    public static class CairoAuthoredLandmarks
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoLandmarks/Generated";
        private const string PyramidPath = ResourceRoot + "/SM_Landmark_GizaSpiritPyramid_A";
        private const string MinaretPath = ResourceRoot + "/SM_Landmark_CairoMinaret_A";
        private const string DomeGatePath = ResourceRoot + "/SM_Landmark_CairoDomeGate_A";
        private const string BridgePath = ResourceRoot + "/SM_Landmark_CairoBridgeGantry_A";

        private static bool activationLogged;

        public static bool TryCreateMinaretCluster(
            Transform anchor,
            Material dark,
            Material purple,
            Material cyan,
            Material gold)
        {
            var source = Resources.Load<GameObject>(MinaretPath);
            if (source == null)
            {
                Missing(MinaretPath);
                return false;
            }

            var root = Root("AUTHORED SPIRIT MINARET CLUSTER", anchor, -anchor.right * 26f + Vector3.up * .2f);
            CreateMinaret(source, root, new Vector3(-6f, 0f, 0f), .82f, dark, purple, gold);
            CreateMinaret(source, root, Vector3.zero, 1.05f, dark, cyan, gold);
            CreateMinaret(source, root, new Vector3(6f, 0f, 0f), .72f, dark, purple, gold);
            LogActivation("minarets");
            return true;
        }

        public static bool TryCreateDomeGate(
            Transform anchor,
            Material dark,
            Material purple,
            Material cyan,
            Material gold)
        {
            var source = Resources.Load<GameObject>(DomeGatePath);
            if (source == null)
            {
                Missing(DomeGatePath);
                return false;
            }

            var root = Root("AUTHORED NEON DOME GATE", anchor, anchor.right * 24f);
            var instance = Object.Instantiate(source, root, false);
            instance.name = "Authored Cairo Dome Gate";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyIdentityMaterials(instance, dark, purple, cyan, gold);
            LogActivation("dome-gate");
            return true;
        }

        public static bool TryCreatePyramidPair(
            Transform anchor,
            Material dark,
            Material purple,
            Material gold)
        {
            var source = Resources.Load<GameObject>(PyramidPath);
            if (source == null)
            {
                Missing(PyramidPath);
                return false;
            }

            var root = Root("AUTHORED PYRAMID HORIZON PAIR", anchor, -anchor.right * 40f);
            CreatePyramid(source, root, new Vector3(-9f, 0f, 1.5f), .92f, dark, purple, gold);
            CreatePyramid(source, root, new Vector3(8f, 0f, -2f), .64f, dark, purple, gold);
            LogActivation("pyramids");
            return true;
        }

        public static bool TryCreateTrackPyramid(
            Transform parent,
            Vector3 worldPosition,
            float scale,
            Material dark,
            Material purple,
            Material gold)
        {
            var source = Resources.Load<GameObject>(PyramidPath);
            if (source == null)
            {
                Missing(PyramidPath);
                return false;
            }

            var instance = Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED GIZA SPIRIT PYRAMID";
            instance.transform.position = worldPosition;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * Mathf.Max(.2f, scale);
            ApplyIdentityMaterials(instance, dark, purple, purple, gold);
            LogActivation("track-pyramid");
            return true;
        }

        public static bool TryCreateBridgeGantry(
            Transform anchor,
            Material dark,
            Material purple,
            Material cyan,
            Material gold)
        {
            var source = Resources.Load<GameObject>(BridgePath);
            if (source == null)
            {
                Missing(BridgePath);
                return false;
            }

            var root = Root("AUTHORED CAIRO BRIDGE GANTRY", anchor, Vector3.zero);
            var instance = Object.Instantiate(source, root, false);
            instance.name = "Authored Cairo Bridge Gantry";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyIdentityMaterials(instance, dark, purple, cyan, gold);
            LogActivation("bridge-gantry");
            return true;
        }

        private static void CreateMinaret(
            GameObject source,
            Transform parent,
            Vector3 localPosition,
            float scale,
            Material dark,
            Material glow,
            Material gold)
        {
            var instance = Object.Instantiate(source, parent, false);
            instance.name = "Authored Cairo Minaret";
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;
            ApplyIdentityMaterials(instance, dark, glow, glow, gold);
        }

        private static void CreatePyramid(
            GameObject source,
            Transform parent,
            Vector3 localPosition,
            float scale,
            Material dark,
            Material glow,
            Material gold)
        {
            var instance = Object.Instantiate(source, parent, false);
            instance.name = "Authored Giza Spirit Pyramid";
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;
            ApplyIdentityMaterials(instance, dark, glow, glow, gold);
        }

        private static Transform Root(string name, Transform anchor, Vector3 offset)
        {
            var root = new GameObject(name).transform;
            root.position = anchor.position + offset;
            root.rotation = anchor.rotation;
            return root;
        }

        private static void ApplyIdentityMaterials(
            GameObject instance,
            Material dark,
            Material purple,
            Material cyan,
            Material gold)
        {
            if (instance == null) return;
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var n = renderer.gameObject.name;
                var material = dark;
                if (ContainsAny(n, "Gold", "Spire", "Crest", "Apex")) material = gold;
                else if (ContainsAny(n, "Cyan")) material = cyan;
                else if (ContainsAny(n, "Spirit", "Neon", "Dome", "Balcony", "Crown", "Purple")) material = purple;
                if (material == null) continue;

                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = material;
                renderer.sharedMaterials = bindings;
            }
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            if (string.IsNullOrEmpty(value) || tokens == null) return false;
            foreach (var token in tokens)
                if (!string.IsNullOrEmpty(token) && value.Contains(token)) return true;
            return false;
        }

        private static void Missing(string path)
        {
            Debug.LogError($"AFAREET_UART006_AUTHORED_RESOURCE_MISSING path={path}");
        }

        private static void LogActivation(string kind)
        {
            if (!activationLogged)
            {
                activationLogged = true;
                Debug.Log("AFAREET_UART006_AUTHORED_LANDMARK_RUNTIME_ACTIVE geometry=tracked-obj resources=staged");
            }
            Debug.Log($"AFAREET_UART006_AUTHORED_LANDMARK_ACTIVE kind={kind}");
        }
    }
}
