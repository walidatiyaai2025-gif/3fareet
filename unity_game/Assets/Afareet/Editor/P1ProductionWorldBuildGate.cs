using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Fail-closed build guard for the owner-rejected Cairo blockout path (#128).
    /// Development/Editor play remains possible, but Android candidate/release builds
    /// must not silently package the known primitive/blockout world as production art.
    /// </summary>
    public static class P1ProductionWorldBuildGate
    {
        private const string ManifestRelativePath = "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json";
        private const string GeneratedAssetRoot = "Assets/Afareet/Resources/Art/TracksEnvironments/CairoStreetKit/Generated";
        private const string GeneratedResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private const string ProductionReadyState = "PRODUCTION_READY";
        private const string ProductionQuality = "authored-production";

        [Serializable]
        private sealed class Manifest
        {
            public string taskId;
            public string reviewState;
            public string sourceQuality;
            public bool runtimeIntegrated;
            public bool proceduralFallbackAllowedInCandidate;
            public string sourceRoot;
            public string targetRuntimePath;
            public Module[] modules;
        }

        [Serializable]
        private sealed class Module
        {
            public string model;
            public int productionMinVertices;
            public int productionMinTriangles;
        }

        private readonly struct ObjStats
        {
            public readonly int Vertices;
            public readonly int Triangles;

            public ObjStats(int vertices, int triangles)
            {
                Vertices = vertices;
                Triangles = triangles;
            }
        }

        public static void ValidateAndroidCandidateOrThrow()
        {
            var repositoryRoot = RepositoryRoot();
            var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(manifestPath))
                Fail($"UART-005 production manifest is missing: {ManifestRelativePath}");

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null)
                Fail("UART-005 production manifest could not be parsed.");
            if (!string.Equals(manifest.taskId, "UART-005", StringComparison.Ordinal))
                Fail($"Unexpected Cairo asset task id: {manifest.taskId ?? "<null>"}");
            if (!string.Equals(manifest.reviewState, ProductionReadyState, StringComparison.Ordinal))
                Fail($"UART-005 is {manifest.reviewState ?? "<null>"}; expected {ProductionReadyState} before an Android candidate can be built.");
            if (!string.Equals(manifest.sourceQuality, ProductionQuality, StringComparison.Ordinal))
                Fail($"UART-005 source quality is {manifest.sourceQuality ?? "<null>"}; expected {ProductionQuality}.");
            if (!manifest.runtimeIntegrated)
                Fail("UART-005 production assets are not marked runtimeIntegrated=true.");
            if (manifest.proceduralFallbackAllowedInCandidate)
                Fail("UART-005 manifest allows procedural fallback in a candidate build.");
            if (manifest.modules == null || manifest.modules.Length < 4)
                Fail("UART-005 production manifest must define at least four required Cairo modules.");
            if (string.IsNullOrWhiteSpace(manifest.sourceRoot) || string.IsNullOrWhiteSpace(manifest.targetRuntimePath))
                Fail("UART-005 source/runtime roots are missing.");

            foreach (var module in manifest.modules)
                ValidateModule(repositoryRoot, manifest, module);

            Debug.Log($"AFAREET_P1_PRODUCTION_WORLD_GATE_OK task=UART-005 modules={manifest.modules.Length} runtime=staged-authored-resources");
        }

        private static void ValidateModule(string repositoryRoot, Manifest manifest, Module module)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.model))
                Fail("UART-005 contains an incomplete module record.");
            if (module.productionMinVertices <= 0 || module.productionMinTriangles <= 0)
                Fail($"UART-005 {module.model} is missing positive anti-blockout geometry floors.");

            var sourceRelative = CombineForwardSlashes(manifest.sourceRoot, module.model);
            var sourceAbsolute = Path.Combine(repositoryRoot, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourceAbsolute))
                Fail($"UART-005 authored source is missing: {sourceRelative}");

            var sourceStats = ReadObjStats(sourceAbsolute);
            if (sourceStats.Vertices < module.productionMinVertices || sourceStats.Triangles < module.productionMinTriangles)
                Fail(
                    $"UART-005 blockout rejected: {module.model} has {sourceStats.Vertices} vertices/{sourceStats.Triangles} triangles; " +
                    $"requires at least {module.productionMinVertices}/{module.productionMinTriangles}."
                );

            var stagedAssetPath = CombineForwardSlashes(GeneratedAssetRoot, module.model);
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(stagedAssetPath);
            if (imported == null)
                Fail($"UART-005 staged Unity model is missing or failed import: {stagedAssetPath}");

            var filters = imported.GetComponentsInChildren<MeshFilter>(true);
            if (filters == null || filters.Length == 0)
                Fail($"UART-005 staged Unity model has no MeshFilter: {stagedAssetPath}");

            var importedVertices = 0;
            var importedTriangles = 0;
            var packagedMeshCount = 0;
            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null) continue;
                var meshAssetPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (!string.Equals(meshAssetPath, stagedAssetPath, StringComparison.Ordinal))
                    continue;

                packagedMeshCount++;
                importedVertices += filter.sharedMesh.vertexCount;
                for (var sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                    importedTriangles += (int)filter.sharedMesh.GetIndexCount(sub) / 3;
            }

            if (packagedMeshCount == 0)
                Fail($"UART-005 imported resource does not resolve to packaged authored mesh data: {stagedAssetPath}");
            if (importedVertices < module.productionMinVertices || importedTriangles < module.productionMinTriangles)
                Fail(
                    $"UART-005 Unity import degraded below the production floor: {module.model} " +
                    $"imported={importedVertices}v/{importedTriangles}t required={module.productionMinVertices}v/{module.productionMinTriangles}t."
                );

            var resourcePath = CombineForwardSlashes(GeneratedResourceRoot, Path.GetFileNameWithoutExtension(module.model));
            if (Resources.Load<GameObject>(resourcePath) == null)
                Fail($"UART-005 authored model is not addressable through Resources: {resourcePath}");
        }

        private static ObjStats ReadObjStats(string path)
        {
            var vertices = 0;
            var triangles = 0;
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.StartsWith("v ", StringComparison.Ordinal))
                {
                    vertices++;
                    continue;
                }

                if (!line.StartsWith("f ", StringComparison.Ordinal)) continue;
                var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var polygonVertexCount = tokens.Length - 1;
                if (polygonVertexCount >= 3)
                    triangles += polygonVertexCount - 2;
            }
            return new ObjStats(vertices, triangles);
        }

        private static string RepositoryRoot()
        {
            // Application.dataPath => <repo>/unity_game/Assets
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }

        private static string CombineForwardSlashes(params string[] parts)
        {
            return string.Join("/", parts).Replace("//", "/");
        }

        private static void Fail(string reason)
        {
            throw new InvalidOperationException($"AFAREET_P1_PRODUCTION_WORLD_GATE_BLOCKED {reason}");
        }
    }
}
