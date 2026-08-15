using System;
using UnityEngine;

namespace Afareet.World
{
    public static class CairoAuthoredTrackDressing
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoTrackDressing/Generated";
        private const string FinishGatePath = ResourceRoot + "/SM_Track_FinishGate_A";
        private const string RunePath = ResourceRoot + "/SM_Track_SpiritRune_A";
        private static bool activationLogged;

        public static bool TryCreateRoadRune(Transform parent, Vector3 position, Quaternion rotation, Material primary, Material secondary)
        {
            var source = Resources.Load<GameObject>(RunePath);
            if (source == null) return Missing(RunePath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Asphalt Spirit Rune";
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = Vector3.one;
            ApplyByName(instance, primary, secondary, secondary, primary);
            LogActivation();
            return true;
        }

        public static bool TryCreateFinishGate(Transform parent, Transform start, Material cyan, Material purple, Material gold)
        {
            var source = Resources.Load<GameObject>(FinishGatePath);
            if (source == null) return Missing(FinishGatePath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Cairo Finish Gate";
            instance.transform.SetPositionAndRotation(start.position, start.rotation);
            instance.transform.localScale = Vector3.one;
            ApplyByName(instance, cyan, purple, gold, cyan);
            LogActivation();
            return true;
        }

        private static void ApplyByName(GameObject instance, Material baseMaterial, Material spirit, Material gold, Material cyan)
        {
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var name = renderer.gameObject.name ?? string.Empty;
                var selected = baseMaterial;
                if (Contains(name, "Gold") || Contains(name, "Crest") || Contains(name, "Crown")) selected = gold ?? baseMaterial;
                else if (Contains(name, "Spirit") || Contains(name, "Rune")) selected = spirit ?? baseMaterial;
                else if (Contains(name, "Arch") || Contains(name, "Pylon") || Contains(name, "Edge")) selected = cyan ?? baseMaterial;
                if (selected == null) continue;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (var i = 0; i < materials.Length; i++) materials[i] = selected;
                renderer.sharedMaterials = materials;
            }
        }

        private static bool Contains(string value, string token) => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool Missing(string path)
        {
            Debug.LogError($"AFAREET_UART007_AUTHORED_RESOURCE_MISSING path={path}");
            return false;
        }

        private static void LogActivation()
        {
            if (activationLogged) return;
            activationLogged = true;
            Debug.Log("AFAREET_UART007_AUTHORED_TRACK_DRESSING_ACTIVE geometry=tracked-obj resources=staged");
        }
    }
}
