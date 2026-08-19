using System;
using System.Collections.Generic;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Editor
{
    /// <summary>
    /// Generates the deterministic UART-003 V2 geometry for Editor visual/shape iteration only.
    /// This output is deliberately named PreviewV2 and is never the production resource path.
    /// </summary>
    public static class HeroCarProductionAssetBuilder
    {
        private const string OutputRoot = "Assets/Afareet/Resources/Art/Vehicles/HeroCar/Generated";
        private const string PrefabPath = OutputRoot + "/PF_Vehicle_AfareetKing_PreviewV2.prefab";

        private static readonly string[] GroupNames =
        {
            "Body", "Glass", "Wheel", "GoldTrim", "Black", "Spirit"
        };

        [MenuItem("Afareet/Build Hero Car Generated Preview V2")]
        public static void BuildMenu() => BuildOrThrow();

        [MenuItem("Afareet/Validate Hero Car Generated Preview V2")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("AFAREET_HERO_GENERATED_PREVIEW_VALIDATION_OK");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleInitialGeneration()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
                try
                {
                    BuildOrThrow();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AFAREET_HERO_PREVIEW_GENERATION_FAILED {ex}");
                }
            };
        }

        public static void BuildOrThrow()
        {
            HeroCarLodPolicy.ValidateContract();

            var authored = new HeroCarProductionMeshAuthoring.MeshData[3];
            for (var lod = 0; lod < authored.Length; lod++)
            {
                authored[lod] = HeroCarProductionMeshAuthoring.Build(lod);
                ValidatePreviewData(lod, authored[lod]);
            }

            if (AssetDatabase.IsValidFolder(OutputRoot))
                AssetDatabase.DeleteAsset(OutputRoot);

            Directory.CreateDirectory(OutputRoot);
            AssetDatabase.Refresh();

            var materials = CreatePreviewMaterials();
            var meshes = new Mesh[3];
            for (var lod = 0; lod < meshes.Length; lod++)
            {
                meshes[lod] = CreatePreviewMesh(lod, authored[lod]);
                AssetDatabase.CreateAsset(meshes[lod], $"{OutputRoot}/SM_Vehicle_AfareetKing_PreviewV2_LOD{lod}.asset");
            }

            BuildPreviewPrefab(meshes, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateOrThrow();

            Debug.Log(
                "AFAREET_HERO_GENERATED_PREVIEW_V2_BUILD_OK " +
                $"lod0={authored[0].Vertices.Count}v/{authored[0].TriangleCount}t " +
                $"lod1={authored[1].Vertices.Count}v/{authored[1].TriangleCount}t " +
                $"lod2={authored[2].Vertices.Count}v/{authored[2].TriangleCount}t " +
                $"prefab={PrefabPath} production=false");
        }

        public static void ValidateOrThrow()
        {
            HeroCarLodPolicy.ValidateContract();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Generated Hero preview prefab is missing at {PrefabPath}.");

            if (prefab.GetComponent<HeroCarProductionAssetMetadata>() != null)
                throw new InvalidOperationException("Generated Hero preview must never carry production-authoring metadata.");

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
                throw new InvalidOperationException("Generated Hero preview is missing its LODGroup.");

            var lods = group.GetLODs();
            if (lods.Length != 3)
                throw new InvalidOperationException($"Hero preview must have exactly 3 LOD levels, got {lods.Length}.");

            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (Mathf.Abs(lods[lod].screenRelativeTransitionHeight - HeroCarLodPolicy.TransitionFor(lod)) > .0001f)
                    throw new InvalidOperationException($"Hero preview LOD{lod} transition height does not match policy.");
                if (lods[lod].renderers == null || lods[lod].renderers.Length != 1 || lods[lod].renderers[0] == null)
                    throw new InvalidOperationException($"Hero preview LOD{lod} must bind exactly one renderer.");

                var renderer = lods[lod].renderers[0] as MeshRenderer;
                if (renderer == null)
                    throw new InvalidOperationException($"Hero preview LOD{lod} must use a MeshRenderer.");

                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException($"Hero preview LOD{lod} renderer is missing its mesh.");

                var mesh = filter.sharedMesh;
                var triangleCount = TriangleCount(mesh);
                if (!HeroCarLodPolicy.IsWithinBudget(lod, mesh.vertexCount, triangleCount))
                    throw new InvalidOperationException(
                        $"Hero preview LOD{lod} violates geometry policy: {mesh.vertexCount} vertices / {triangleCount} triangles.");

                if (mesh.subMeshCount != GroupNames.Length)
                    throw new InvalidOperationException($"Hero preview LOD{lod} must preserve all {GroupNames.Length} material groups.");
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length != GroupNames.Length)
                    throw new InvalidOperationException($"Hero preview LOD{lod} material binding count is invalid.");

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                        throw new InvalidOperationException($"Hero preview LOD{lod} has a null material binding.");
                    if (material.shader == null || material.shader.name != "Afareet/RuntimeLit")
                        throw new InvalidOperationException($"Hero preview LOD{lod} must use Afareet/RuntimeLit materials.");
                }
            }
        }

        private static void ValidatePreviewData(int lod, HeroCarProductionMeshAuthoring.MeshData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Triangles == null || data.Triangles.Length != GroupNames.Length)
                throw new InvalidOperationException("UART-003 preview mesh must preserve six material groups.");
            if (!HeroCarLodPolicy.IsWithinBudget(lod, data.Vertices.Count, data.TriangleCount))
                throw new InvalidOperationException(
                    $"UART-003 preview LOD{lod} violates geometry policy: {data.Vertices.Count} vertices / {data.TriangleCount} triangles.");

            for (var group = 0; group < data.Triangles.Length; group++)
            {
                if (data.Triangles[group].Count < 3 || data.Triangles[group].Count % 3 != 0)
                    throw new InvalidOperationException($"UART-003 preview group {GroupNames[group]} has invalid triangle data.");
            }
        }

        private static Mesh CreatePreviewMesh(int lod, HeroCarProductionMeshAuthoring.MeshData data)
        {
            var mesh = new Mesh
            {
                name = $"SM_Vehicle_AfareetKing_PreviewV2_LOD{lod}",
                indexFormat = data.Vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(data.Vertices);
            mesh.subMeshCount = GroupNames.Length;
            for (var sub = 0; sub < GroupNames.Length; sub++)
                mesh.SetTriangles(data.Triangles[sub], sub, true);

            // Preview-only normals: production acceptance explicitly requires authored normals.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Dictionary<string, Material> CreatePreviewMaterials()
        {
            var shader = Shader.Find("Afareet/RuntimeLit");
            if (shader == null) throw new InvalidOperationException("Required shader Afareet/RuntimeLit is unavailable.");

            return new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                ["Body"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Body", new Color(.022f, .006f, .05f), .82f, .86f, new Color(.012f, .002f, .025f)),
                ["Glass"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Glass", new Color(.008f, .055f, .09f), .42f, .94f, new Color(0f, .018f, .045f)),
                ["Wheel"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Wheel", new Color(.006f, .007f, .009f), .15f, .24f, Color.black),
                ["GoldTrim"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Gold", new Color(1f, .40f, .018f), .92f, .86f, new Color(.30f, .07f, 0f)),
                ["Black"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Black", new Color(.003f, .004f, .008f), .34f, .48f, Color.black),
                ["Spirit"] = PreviewMaterial(shader, "M_Vehicle_AfareetKing_Preview_Spirit", new Color(.43f, .008f, .98f), .34f, .90f, new Color(.95f, .025f, 1.60f))
            };
        }

        private static Material PreviewMaterial(Shader shader, string name, Color color, float metallic, float gloss, Color emission)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(material, $"{OutputRoot}/{name}.mat");
            return material;
        }

        private static void BuildPreviewPrefab(Mesh[] meshes, IReadOnlyDictionary<string, Material> materials)
        {
            var root = new GameObject("PF_Vehicle_AfareetKing_PreviewV2");
            var renderers = new Renderer[3];

            for (var lod = 0; lod < 3; lod++)
            {
                var child = new GameObject($"SM_Vehicle_AfareetKing_PreviewV2_LOD{lod}");
                child.transform.SetParent(root.transform, false);
                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = meshes[lod];
                var renderer = child.AddComponent<MeshRenderer>();
                var bindings = new Material[GroupNames.Length];
                for (var group = 0; group < GroupNames.Length; group++)
                    bindings[group] = materials[GroupNames[group]];
                renderer.sharedMaterials = bindings;
                renderer.shadowCastingMode = lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = lod < 2;
                renderers[lod] = renderer;
            }

            var groupComponent = root.AddComponent<LODGroup>();
            groupComponent.fadeMode = LODFadeMode.None;
            groupComponent.animateCrossFading = false;
            groupComponent.SetLODs(new[]
            {
                new LOD(HeroCarLodPolicy.Lod0Transition, new[] { renderers[0] }),
                new LOD(HeroCarLodPolicy.Lod1Transition, new[] { renderers[1] }),
                new LOD(HeroCarLodPolicy.Lod2Transition, new[] { renderers[2] })
            });
            groupComponent.RecalculateBounds();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }
    }
}
