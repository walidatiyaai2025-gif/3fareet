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
        private const float FinishGateForwardOffset = 10f;
        private const float SectorBeaconLateralOffset = 15f;
        private static bool activationLogged;
        private static bool finishGatePlacementLogged;
        private static bool editorMaterialOverrideLogged;
        private static bool editorTexturePreservationLogged;

        public static bool TryCreateGround(Transform parent, Material ground, Material accent)
        {
            var source = Resources.Load<GameObject>(GroundPath);
            if (source == null) return Missing(GroundPath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Cairo Desert Ground";
            instance.transform.localPosition = new Vector3(0f, -.15f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyByNameForEditorPreview(instance, ground, accent, accent, accent);
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
            ApplyByNameForEditorPreview(instance, primary, secondary, secondary, primary);
            LogActivation();
            return true;
        }

        public static bool TryCreateFinishGate(Transform parent, Transform start, Material cyan, Material purple, Material gold)
        {
            var source = Resources.Load<GameObject>(FinishGatePath);
            if (source == null) return Missing(FinishGatePath);
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = "AUTHORED Cairo Finish Gate";

            // Waypoint 0 is also the front grid anchor. Placing a ~15m wide / 9m tall authored
            // gate directly on that transform leaves the 7.2m chase camera almost inside the
            // landmark at race start. Keep race/grid logic untouched and move only the visual
            // landmark forward along the track so the gate frames the sightline instead of
            // occluding it.
            var visualPosition = start.position + start.forward * FinishGateForwardOffset;
            instance.transform.SetPositionAndRotation(visualPosition, start.rotation);
            instance.transform.localScale = Vector3.one;
            ApplyByNameForEditorPreview(instance, cyan, purple, gold, cyan);
            LogFinishGatePlacement();
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
            instance.transform.SetPositionAndRotation(
                anchor.position + anchor.right * SectorBeaconLateralOffset,
                anchor.rotation);
            instance.transform.localScale = Vector3.one;
            ApplySectorBeaconMaterialsForEditorPreview(instance, primary, secondary, dark, gold);

            LogActivation();
            return true;
        }

        private static void ApplySectorBeaconMaterialsForEditorPreview(
            GameObject instance,
            Material primary,
            Material secondary,
            Material dark,
            Material gold)
        {
            if (!Application.isEditor)
                return;
            if (instance == null)
                return;

            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var name = renderer.gameObject.name ?? string.Empty;
                var selected = dark;
                if (Contains(name, "Primary")) selected = primary ?? dark;
                else if (Contains(name, "Secondary")) selected = secondary ?? dark;
                else if (Contains(name, "Gold") || Contains(name, "Spire")) selected = gold ?? dark;
                else if (Contains(name, "Crown") || Contains(name, "Lantern")) selected = primary ?? dark;
                SetAllMaterialsForEditorPreview(renderer, selected);
            }
        }

        private static void ApplyByNameForEditorPreview(GameObject instance, Material baseMaterial, Material spirit, Material gold, Material cyan)
        {
            if (!Application.isEditor)
                return;
            if (instance == null)
                return;

            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var name = renderer.gameObject.name ?? string.Empty;
                var selected = baseMaterial;
                if (Contains(name, "Gold") || Contains(name, "Crest") || Contains(name, "Crown")) selected = gold ?? baseMaterial;
                else if (Contains(name, "Spirit") || Contains(name, "Rune") || Contains(name, "Dune")) selected = spirit ?? baseMaterial;
                else if (Contains(name, "Arch") || Contains(name, "Pylon") || Contains(name, "Edge")) selected = cyan ?? baseMaterial;
                SetAllMaterialsForEditorPreview(renderer, selected);
            }
        }

        private static void SetAllMaterialsForEditorPreview(Renderer renderer, Material selected)
        {
            if (renderer == null || selected == null) return;
            if (WouldDiscardAuthoredTexture(renderer, selected))
            {
                LogEditorTexturePreservation();
                return;
            }

            LogEditorMaterialOverride();
            var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
            var materials = new Material[count];
            for (var i = 0; i < materials.Length; i++) materials[i] = selected;
            renderer.sharedMaterials = materials;
        }

        private static bool WouldDiscardAuthoredTexture(Renderer renderer, Material previewMaterial) =>
            RendererHasAssignedTexture(renderer) && !MaterialHasAssignedTexture(previewMaterial);

        private static bool RendererHasAssignedTexture(Renderer renderer)
        {
            if (renderer == null) return false;
            foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
            {
                if (MaterialHasAssignedTexture(material))
                    return true;
            }
            return false;
        }

        private static bool MaterialHasAssignedTexture(Material material)
        {
            if (material == null || material.shader == null)
                return false;
            foreach (var propertyName in material.GetTexturePropertyNames())
            {
                if (material.GetTexture(propertyName) != null)
                    return true;
            }
            return false;
        }

        private static void LogFinishGatePlacement()
        {
            if (finishGatePlacementLogged) return;
            finishGatePlacementLogged = true;
            Debug.Log(
                $"AFAREET_UART007_FINISH_GATE_VISUAL_CLEARANCE_ACTIVE forwardOffset={FinishGateForwardOffset:F1}m " +
                "waypoint0Unchanged=true gridLogicUnchanged=true");
        }

        private static void LogEditorMaterialOverride()
        {
            if (editorMaterialOverrideLogged) return;
            editorMaterialOverrideLogged = true;
            Debug.Log("AFAREET_UART007_EDITOR_PREVIEW_MATERIAL_OVERRIDE production=false player-preserves-source-materials=true");
        }

        private static void LogEditorTexturePreservation()
        {
            if (editorTexturePreservationLogged) return;
            editorTexturePreservationLogged = true;
            Debug.Log("AFAREET_UART007_EDITOR_SOURCE_TEXTURE_PRESERVED previewOverrideSkipped=true production=false");
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
            Debug.Log(
                $"AFAREET_UART007_AUTHORED_TRACK_DRESSING_ACTIVE geometry=tracked-obj resources=staged " +
                $"playerMaterials=source-authored finishGateForwardOffset={FinishGateForwardOffset:F1} " +
                $"sectorBeaconOffset={SectorBeaconLateralOffset:F1}");
        }
    }
}
