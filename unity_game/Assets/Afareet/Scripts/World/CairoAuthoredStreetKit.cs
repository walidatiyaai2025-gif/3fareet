using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Runtime presentation adapter for UART-005 tracked authored geometry.
    /// Geometry comes from Unity-imported OBJ resources staged from tracked sources.
    /// Editor preview may use temporary runtime materials; Player builds preserve imported
    /// authored source materials so production texture mapping cannot be silently discarded.
    /// </summary>
    public static class CairoAuthoredStreetKit
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private static readonly string[] FacadePaths =
        {
            ResourceRoot + "/SM_Env_CairoFacade_A",
            ResourceRoot + "/SM_Env_CairoFacade_B",
            ResourceRoot + "/SM_Env_CairoFacade_C"
        };
        private static readonly string[] AwningPaths =
        {
            ResourceRoot + "/SM_Env_CairoAwning_A",
            ResourceRoot + "/SM_Env_CairoAwning_B"
        };
        private const string SignPath = ResourceRoot + "/SM_Prop_CairoSign_A";
        private const string LampPath = ResourceRoot + "/SM_Prop_CairoLamp_A";
        private const string BarrierPath = ResourceRoot + "/SM_Prop_CairoBarrier_A";
        private const string RoadPath = ResourceRoot + "/SM_Track_CairoRoad_A";
        private const string CurbPath = ResourceRoot + "/SM_Track_CairoCurb_A";
        private const float AuthoredAwningWidth = 3f;

        private static bool activationLogged;
        private static bool roadActivationLogged;
        private static bool buildingVariantActivationLogged;
        private static bool editorMaterialOverrideLogged;

        public static bool TryCreateRoadSegment(
            Transform parent,
            Vector3 center,
            Quaternion rotation,
            float length,
            float roadWidth,
            Material asphaltMaterial,
            Material curbMaterial,
            Material accentMaterial)
        {
            var roadSource = Resources.Load<GameObject>(RoadPath);
            var curbSource = Resources.Load<GameObject>(CurbPath);
            if (roadSource == null || curbSource == null)
            {
                if (roadSource == null) Missing(RoadPath);
                if (curbSource == null) Missing(CurbPath);
                return false;
            }

            var root = new GameObject("AUTHORED CAIRO ROAD SEGMENT").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(center, rotation);

            var zScale = Mathf.Max(.05f, length / 10f);
            var xScale = Mathf.Max(.05f, roadWidth / 14f);

            var road = Object.Instantiate(roadSource, root, false);
            road.name = "Authored Crowned Asphalt";
            road.transform.localPosition = Vector3.zero;
            road.transform.localRotation = Quaternion.identity;
            road.transform.localScale = new Vector3(xScale, 1f, zScale);
            ApplyNamedMaterialsForEditorPreview(road, asphaltMaterial, accentMaterial, "EdgeSeam", "CenterDetail", "Drainage");

            var curbOffset = roadWidth * .5f + .28f;
            var rightCurb = Object.Instantiate(curbSource, root, false);
            rightCurb.name = "Authored Curb Right";
            rightCurb.transform.localPosition = new Vector3(curbOffset, 0f, 0f);
            rightCurb.transform.localRotation = Quaternion.identity;
            rightCurb.transform.localScale = new Vector3(1f, 1f, zScale);
            ApplyNamedMaterialsForEditorPreview(rightCurb, curbMaterial, accentMaterial, "Reflector", "NeonChannel");

            var leftCurb = Object.Instantiate(curbSource, root, false);
            leftCurb.name = "Authored Curb Left";
            leftCurb.transform.localPosition = new Vector3(-curbOffset, 0f, 0f);
            leftCurb.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            leftCurb.transform.localScale = new Vector3(1f, 1f, zScale);
            ApplyNamedMaterialsForEditorPreview(leftCurb, curbMaterial, accentMaterial, "Reflector", "NeonChannel");

            LogActivation();
            if (!roadActivationLogged)
            {
                roadActivationLogged = true;
                Debug.Log("AFAREET_UART005_AUTHORED_ROAD_ACTIVE source=tracked-obj road=SM_Track_CairoRoad_A curb=SM_Track_CairoCurb_A playerMaterials=source-authored");
            }
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
            var facadeVariant = StableVariantIndex(groundPosition, width, height, FacadePaths.Length, 17);
            var awningVariant = StableVariantIndex(groundPosition, width, height, AwningPaths.Length, 31);
            var facadePath = FacadePaths[facadeVariant];
            var awningPath = AwningPaths[awningVariant];

            var facade = Resources.Load<GameObject>(facadePath);
            var awning = Resources.Load<GameObject>(awningPath);
            var sign = Resources.Load<GameObject>(SignPath);
            if (facade == null || awning == null || sign == null)
            {
                if (facade == null) Missing(facadePath);
                if (awning == null) Missing(awningPath);
                if (sign == null) Missing(SignPath);
                return false;
            }

            var root = new GameObject($"AUTHORED CAIRO BUILDING F{facadeVariant + 1} A{awningVariant + 1}").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(groundPosition, rotation);

            var tierCount = Mathf.Max(1, Mathf.CeilToInt(height / 5f));
            for (var tier = 0; tier < tierCount; tier++)
            {
                var tierBase = tier * 5f;
                var tierHeight = Mathf.Min(5f, height - tierBase);
                if (tierHeight <= .1f) break;

                var scaleY = tierHeight / 5f;
                CreateFacade(facade, root, $"Facade Front V{facadeVariant + 1}", new Vector3(-width * .5f, tierBase, -width * .5f), Quaternion.identity, new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, $"Facade Back V{facadeVariant + 1}", new Vector3(width * .5f, tierBase, width * .5f), Quaternion.Euler(0f, 180f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, $"Facade Left V{facadeVariant + 1}", new Vector3(-width * .5f, tierBase, width * .5f), Quaternion.Euler(0f, -90f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
                CreateFacade(facade, root, $"Facade Right V{facadeVariant + 1}", new Vector3(width * .5f, tierBase, -width * .5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(width / 6f, scaleY, 1f), facadeMaterial);
            }

            if (width >= 5f)
            {
                var canopy = Object.Instantiate(awning, root, false);
                canopy.name = $"Authored Cairo Awning V{awningVariant + 1}";
                var awningScaleX = Mathf.Min(1f, width / 6f);
                var placedAwningWidth = AuthoredAwningWidth * awningScaleX;

                canopy.transform.localPosition = new Vector3(
                    placedAwningWidth * .5f,
                    1.55f,
                    -width * .5f - .06f);
                canopy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                canopy.transform.localScale = new Vector3(awningScaleX, 1f, .78f);
                ApplyMaterialForEditorPreview(canopy, accentMaterial);
            }

            if (width >= 5.5f && height >= 3.25f)
            {
                var hangingSign = Object.Instantiate(sign, root, false);
                hangingSign.name = "Authored Cairo Hanging Sign";
                var signScale = Mathf.Clamp(width / 7f, .72f, 1.08f);
                hangingSign.transform.localPosition = new Vector3(
                    -width * .23f,
                    Mathf.Clamp(height * .42f, 2.35f, 3.65f),
                    -width * .5f - .08f);
                hangingSign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                hangingSign.transform.localScale = Vector3.one * signScale;
                ApplyMaterialForEditorPreview(hangingSign, accentMaterial);
            }

            LogActivation();
            if (!buildingVariantActivationLogged)
            {
                buildingVariantActivationLogged = true;
                Debug.Log(
                    "AFAREET_UART005_BUILDING_VARIANTS_ACTIVE facades=3 awnings=2 signs=1 " +
                    "selection=stable-position-hash playerMaterials=source-authored");
            }
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
            ApplyMaterialForEditorPreview(lamp, material);

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
            ApplyMaterialForEditorPreview(barrier, material);
            LogActivation();
            return true;
        }

        private static int StableVariantIndex(Vector3 position, float width, float height, int count, int salt)
        {
            if (count <= 1) return 0;
            unchecked
            {
                var x = Mathf.RoundToInt(position.x * 2f);
                var z = Mathf.RoundToInt(position.z * 2f);
                var w = Mathf.RoundToInt(width * 10f);
                var h = Mathf.RoundToInt(height * 10f);
                var hash = (x * 73856093) ^ (z * 19349663) ^ (w * 83492791) ^ (h * 297121507) ^ salt;
                return (hash & int.MaxValue) % count;
            }
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
            ApplyMaterialForEditorPreview(panel, material);
        }

        private static void ApplyNamedMaterialsForEditorPreview(GameObject instance, Material baseMaterial, Material accentMaterial, params string[] accentNameTokens)
        {
            if (!Application.isEditor)
                return;

            LogEditorMaterialOverride();
            if (instance == null) return;
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var selected = baseMaterial;
                if (accentMaterial != null && accentNameTokens != null)
                {
                    var rendererName = renderer.gameObject.name;
                    foreach (var token in accentNameTokens)
                    {
                        if (!string.IsNullOrEmpty(token) && rendererName.Contains(token))
                        {
                            selected = accentMaterial;
                            break;
                        }
                    }
                }
                if (selected == null) continue;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = selected;
                renderer.sharedMaterials = bindings;
            }
        }

        private static void ApplyMaterialForEditorPreview(GameObject instance, Material material)
        {
            if (!Application.isEditor)
                return;

            LogEditorMaterialOverride();
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

        private static void LogEditorMaterialOverride()
        {
            if (editorMaterialOverrideLogged) return;
            editorMaterialOverrideLogged = true;
            Debug.Log("AFAREET_UART005_EDITOR_PREVIEW_MATERIAL_OVERRIDE production=false player-preserves-source-materials=true");
        }

        private static void Missing(string path)
        {
            Debug.LogError($"AFAREET_UART005_AUTHORED_RESOURCE_MISSING path={path}");
        }

        private static void LogActivation()
        {
            if (activationLogged) return;
            activationLogged = true;
            Debug.Log("AFAREET_UART005_AUTHORED_RUNTIME_ACTIVE geometry=tracked-obj resources=staged playerMaterials=source-authored");
        }
    }
}
