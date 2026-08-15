using System;
using UnityEngine;

namespace Afareet.World
{
    public static class CairoAuthoredTrackDressing
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoTrackDressing/Generated";
        private const string FinishGatePath = ResourceRoot + "/SM_Track_FinishGate_A";
        private const string RunePath = ResourceRoot + "/SM_Track_SpiritRune_A";
        private const string GroundPath = ResourceRoot + "/SM_Track_DesertGround_A";
        private const string SectorBeaconPath = ResourceRoot + "/SM_Track_SectorBeacon_A";
        private static bool activationLogged;

        public static bool TryCreateGround(Transform parent, Material ground, Material accent)
        {
            var source = Resources.Load<GameObject>(GroundPath);
            if (source == null) return Missing(GroundPath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Cairo Desert Ground";
            instance.transform.localPosition = new Vector3(0f, -.15f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyByName(instance, ground, accent, accent, accent);
            LogActivation();
            return true;
        }

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

        public static bool TryCreateSectorBeacon(
            Transform parent,
            Transform anchor,
            Material primary,
            Material secondary,
            Material dark,
            Material gold)
        {
            var source = Resources.Load<GameObject>(SectorBeaconPath);
            if (source == null) return Missing(SectorBeaconPath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Cairo Sector Beacon";
            instance.transform.SetPositionAndRotation(anchor.position + anchor.right * 18f, anchor.rotation);
            instance.transform.localScale = Vector3.one;

            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var name = renderer.gameObject.name ?? string.Empty;
                var selected = dark;
                if (Contains(name, "Primary")) selected = primary ?? dark;
                else if (Contains(name, "Secondary")) selected = secondary ?? dark;
                else if (Contains(name, "Gold") || Contains(name, "Spire")) selected = gold ?? dark;
                else if (Contains(name, "Crown") || Contains(name, "Lantern")) selected = primary ?? dark;
                SetAllMaterials(renderer, selected);
            }

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
                else if (Contains(name, "Spirit") || Contains(name, "Rune") || Contains(name, "Dune")) selected = spirit ?? baseMaterial;
                else if (Contains(name, "Arch") || Contains(name, "Pylon") || Contains(name, "Edge")) selected = cyan ?? baseMaterial;
                SetAllMaterials(renderer, selected);
            }
        }

        private static void SetAllMaterials(Renderer renderer, Material selected)
        {
            if (renderer == null || selected == null) return;
            var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
            var materials = new Material[count];
            for (var i = 0; i < materials.Length; i++) materials[i] = selected;
            renderer.sharedMaterials = materials;
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
