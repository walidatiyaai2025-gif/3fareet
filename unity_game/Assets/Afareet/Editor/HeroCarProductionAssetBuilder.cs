using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Editor
{
    public static class HeroCarProductionAssetBuilder
    {
        private const string OutputRoot = "Assets/Afareet/Resources/Art/Vehicles/HeroCar/Generated";
        private const string PrefabPath = OutputRoot + "/PF_Vehicle_AfareetKing_Production.prefab";
        private static readonly string[] GroupNames = { "Body", "Glass", "Wheel", "GoldTrim", "Black", "Spirit" };

        private sealed class ParsedObj
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<string> GroupOrder = new();
            public readonly Dictionary<string, List<int>> Triangles = new(StringComparer.Ordinal);
            public int TriangleCount { get; set; }
        }

        [MenuItem("Afareet/Build Hero Car Production LODs")]
        public static void BuildMenu() => BuildOrThrow();

        [MenuItem("Afareet/Validate Hero Car Production LODs")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("AFAREET_HERO_LOD_VALIDATION_OK");
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
                    Debug.LogError($"AFAREET_HERO_GENERATION_FAILED {ex}");
                }
            };
        }

        public static void BuildOrThrow()
        {
            HeroCarLodPolicy.ValidateContract();

            var parsed = new ParsedObj[3];
            for (var lod = 0; lod < parsed.Length; lod++)
            {
                parsed[lod] = ParseSource(lod);
                ValidateParsed(lod, parsed[lod]);
            }

            if (AssetDatabase.IsValidFolder(OutputRoot))
                AssetDatabase.DeleteAsset(OutputRoot);

            Directory.CreateDirectory(OutputRoot);
            AssetDatabase.Refresh();

            var materials = CreateMaterials();
            var meshes = new Mesh[3];
            var groupOrders = new string[3][];

            for (var lod = 0; lod < meshes.Length; lod++)
            {
                meshes[lod] = CreateMesh(lod, parsed[lod]);
                groupOrders[lod] = parsed[lod].GroupOrder.ToArray();
                AssetDatabase.CreateAsset(meshes[lod], $"{OutputRoot}/SM_Vehicle_AfareetKing_LOD{lod}.asset");
            }

            BuildPrefab(meshes, groupOrders, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateOrThrow();

            Debug.Log(
                $"AFAREET_HERO_LOD_BUILD_OK prefab={PrefabPath} " +
                $"triangles={HeroCarLodPolicy.ExpectedTriangles[0]}/" +
                $"{HeroCarLodPolicy.ExpectedTriangles[1]}/" +
                $"{HeroCarLodPolicy.ExpectedTriangles[2]} textures=0");
        }

        public static void ValidateOrThrow()
        {
            HeroCarLodPolicy.ValidateContract();

            for (var lod = 0; lod < 3; lod++)
                ValidateParsed(lod, ParseSource(lod));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Generated Hero prefab is missing at {PrefabPath}.");

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
                throw new InvalidOperationException("Generated Hero prefab is missing its LODGroup.");

            var lods = group.GetLODs();
            if (lods.Length != 3)
                throw new InvalidOperationException($"Hero prefab must have exactly 3 LOD levels, got {lods.Length}.");

            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (Mathf.Abs(lods[lod].screenRelativeTransitionHeight - HeroCarLodPolicy.TransitionFor(lod)) > 0.0001f)
                    throw new InvalidOperationException($"Hero LOD{lod} transition height does not match policy.");
                if (lods[lod].renderers == null || lods[lod].renderers.Length != 1 || lods[lod].renderers[0] == null)
                    throw new InvalidOperationException($"Hero LOD{lod} must bind exactly one renderer.");

                var filter = lods[lod].renderers[0].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException($"Hero LOD{lod} renderer is missing its mesh.");

                var triangleCount = 0;
                for (var sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                    triangleCount += (int)filter.sharedMesh.GetIndexCount(sub) / 3;

                if (!HeroCarLodPolicy.IsWithinBudget(lod, filter.sharedMesh.vertexCount, triangleCount))
                    throw new InvalidOperationException($"Generated Hero LOD{lod} does not match its vertex/triangle budget contract.");

                foreach (var material in lods[lod].renderers[0].sharedMaterials)
                {
                    if (material == null) throw new InvalidOperationException($"Hero LOD{lod} has a null material binding.");
                    if (material.mainTexture != null)
                        throw new InvalidOperationException("UART-003 P1 Hero material contract forbids runtime texture maps.");
                }
            }
        }

        private static ParsedObj ParseSource(int lod)
        {
            var path = SourcePath(lod);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Hero LOD source is missing: {path}", path);

            var result = new ParsedObj();
            string currentGroup = null;

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                if (line.StartsWith("v ", StringComparison.Ordinal))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 4) throw new InvalidDataException($"Invalid Hero OBJ vertex line: {line}");
                    result.Vertices.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    continue;
                }

                if (line.StartsWith("g ", StringComparison.Ordinal))
                {
                    currentGroup = line.Substring(2).Trim();
                    if (string.IsNullOrWhiteSpace(currentGroup))
                        throw new InvalidDataException("Hero OBJ contains an empty group name.");
                    if (!result.Triangles.ContainsKey(currentGroup))
                    {
                        result.Triangles[currentGroup] = new List<int>();
                        result.GroupOrder.Add(currentGroup);
                    }
                    continue;
                }

                if (!line.StartsWith("f ", StringComparison.Ordinal)) continue;
                if (currentGroup == null)
                    throw new InvalidDataException("Hero OBJ face appears before a group declaration.");

                var face = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (face.Length != 4)
                    throw new InvalidDataException("UART-003 source must be triangulated before ingestion.");

                for (var i = 1; i <= 3; i++)
                {
                    var token = face[i].Split('/')[0];
                    var sourceIndex = int.Parse(token, CultureInfo.InvariantCulture);
                    var zeroBased = sourceIndex - 1;
                    if (zeroBased < 0 || zeroBased >= result.Vertices.Count)
                        throw new InvalidDataException($"Hero OBJ face index {sourceIndex} is outside the vertex list.");
                    result.Triangles[currentGroup].Add(zeroBased);
                }
                result.TriangleCount++;
            }

            return result;
        }

        private static void ValidateParsed(int lod, ParsedObj parsed)
        {
            if (!HeroCarLodPolicy.IsWithinBudget(lod, parsed.Vertices.Count, parsed.TriangleCount))
                throw new InvalidOperationException(
                    $"Hero LOD{lod} source mismatch: vertices={parsed.Vertices.Count} triangles={parsed.TriangleCount}; " +
                    $"expected={HeroCarLodPolicy.ExpectedVertices[lod]}/{HeroCarLodPolicy.ExpectedTriangles[lod]}.");

            if (!parsed.Triangles.ContainsKey("Body") || !parsed.Triangles.ContainsKey("Wheel"))
                throw new InvalidOperationException($"Hero LOD{lod} must contain Body and Wheel geometry groups.");
            if (lod < 2 && (!parsed.Triangles.ContainsKey("GoldTrim") || !parsed.Triangles.ContainsKey("Spirit")))
                throw new InvalidOperationException($"Hero near LOD{lod} must preserve GoldTrim and Spirit identity groups.");

            foreach (var group in parsed.GroupOrder)
            {
                var known = false;
                foreach (var expected in GroupNames)
                    if (string.Equals(expected, group, StringComparison.Ordinal)) known = true;
                if (!known) throw new InvalidOperationException($"Unsupported Hero material group '{group}'.");
            }
        }

        private static Mesh CreateMesh(int lod, ParsedObj parsed)
        {
            var mesh = new Mesh
            {
                name = $"SM_Vehicle_AfareetKing_LOD{lod}",
                indexFormat = IndexFormat.UInt16
            };
            mesh.SetVertices(parsed.Vertices);
            mesh.subMeshCount = parsed.GroupOrder.Count;
            for (var sub = 0; sub < parsed.GroupOrder.Count; sub++)
                mesh.SetTriangles(parsed.Triangles[parsed.GroupOrder[sub]], sub, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var shader = Shader.Find("Afareet/RuntimeLit");
            if (shader == null) throw new InvalidOperationException("Required shader Afareet/RuntimeLit is unavailable.");

            var result = new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                ["Body"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Body", new Color(.025f, .008f, .055f), .72f, .72f, Color.black),
                ["Glass"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Glass", new Color(.012f, .08f, .13f), .30f, .88f, new Color(0f, .035f, .06f)),
                ["Wheel"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Wheel", new Color(.008f, .009f, .012f), .20f, .28f, Color.black),
                ["GoldTrim"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Gold", new Color(1f, .43f, .025f), .82f, .78f, new Color(.24f, .065f, 0f)),
                ["Black"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Black", new Color(.004f, .005f, .01f), .18f, .32f, Color.black),
                ["Spirit"] = MaterialAsset(shader, "M_Vehicle_AfareetKing_Spirit", new Color(.46f, .015f, .95f), .28f, .82f, new Color(.75f, .03f, 1.25f))
            };
            return result;
        }

        private static Material MaterialAsset(Shader shader, string name, Color color, float metallic, float gloss, Color emission)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(material, $"{OutputRoot}/{name}.mat");
            return material;
        }

        private static void BuildPrefab(Mesh[] meshes, string[][] groupOrders, IReadOnlyDictionary<string, Material> materials)
        {
            var root = new GameObject("PF_Vehicle_AfareetKing_Production");
            root.AddComponent<HeroCarProductionVisual>();
            var renderers = new Renderer[3];

            for (var lod = 0; lod < 3; lod++)
            {
                var child = new GameObject($"SM_Vehicle_AfareetKing_LOD{lod}");
                child.transform.SetParent(root.transform, false);
                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = meshes[lod];
                var renderer = child.AddComponent<MeshRenderer>();
                var bindings = new Material[groupOrders[lod].Length];
                for (var sub = 0; sub < bindings.Length; sub++)
                    bindings[sub] = materials[groupOrders[lod][sub]];
                renderer.sharedMaterials = bindings;
                renderer.shadowCastingMode = lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = lod < 2;
                renderers[lod] = renderer;
            }

            var group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(HeroCarLodPolicy.Lod0Transition, new[] { renderers[0] }),
                new LOD(HeroCarLodPolicy.Lod1Transition, new[] { renderers[1] }),
                new LOD(HeroCarLodPolicy.Lod2Transition, new[] { renderers[2] })
            });
            group.RecalculateBounds();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static string SourcePath(int lod)
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(
                repositoryRoot,
                "docs", "assets", "01_vehicles", "hero_car_production", "source",
                $"SM_Vehicle_AfareetKing_LOD{lod}.obj");
        }
    }
}
