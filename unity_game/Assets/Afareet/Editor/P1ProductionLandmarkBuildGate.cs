using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Fail-closed UART-006 Android gate. Staging authored source is not enough:
    /// the task must be explicitly promoted to production-ready, runtime-verified,
    /// and free of candidate procedural fallback before an Android build may proceed.
    /// </summary>
    public sealed class P1ProductionLandmarkBuildGate : IPreprocessBuildWithReport
    {
        private const string ManifestRelativePath = "docs/assets/03_props_architecture/cairo_landmarks/ASSET_MANIFEST.json";
        private const string ProductionReadyState = "PRODUCTION_READY";
        private const string ProductionQuality = "authored-production";
        private const string ResourceRoot = "Art/Architecture/CairoLandmarks/Generated";

        public int callbackOrder => -850;

        [Serializable]
        private sealed class Manifest
        {
            public string taskId;
            public string reviewState;
            public string sourceQuality;
            public bool runtimeIntegrated;
            public bool runtimeIntegrationVerified;
            public bool proceduralFallbackAllowedInCandidate;
            public string sourceRoot;
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

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            ValidateAndroidCandidateOrThrow();
        }

        [MenuItem("Afareet/P1/Validate Cairo Landmark Android Gate")]
        public static void ValidateMenu()
        {
            ValidateAndroidCandidateOrThrow();
        }

        public static void ValidateAndroidCandidateOrThrow()
        {
            var repositoryRoot = RepositoryRoot();
            var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(manifestPath))
                Fail($"UART-006 production manifest is missing: {ManifestRelativePath}");

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null)
                Fail("UART-006 production manifest could not be parsed.");
            if (!string.Equals(manifest.taskId, "UART-006", StringComparison.Ordinal))
                Fail($"Unexpected landmark task id: {manifest.taskId ?? "<null>"}");
            if (!string.Equals(manifest.reviewState, ProductionReadyState, StringComparison.Ordinal))
                Fail($"UART-006 is {manifest.reviewState ?? "<null>"}; expected {ProductionReadyState} before Android candidate build.");
            if (!string.Equals(manifest.sourceQuality, ProductionQuality, StringComparison.Ordinal))
                Fail($"UART-006 source quality is {manifest.sourceQuality ?? "<null>"}; expected {ProductionQuality}.");
            if (!manifest.runtimeIntegrated || !manifest.runtimeIntegrationVerified)
                Fail("UART-006 authored landmark runtime integration is not verified.");
            if (manifest.proceduralFallbackAllowedInCandidate)
                Fail("UART-006 manifest allows procedural landmark fallback in candidate builds.");
            if (manifest.modules == null || manifest.modules.Length < 4)
                Fail("UART-006 manifest must define the four required landmark modules.");
            if (string.IsNullOrWhiteSpace(manifest.sourceRoot))
                Fail("UART-006 source root is missing.");

            foreach (var module in manifest.modules)
                ValidateSource(repositoryRoot, manifest.sourceRoot, module);

            foreach (var module in manifest.modules)
            {
                var resourceName = Path.GetFileNameWithoutExtension(module.model);
                var resourcePath = $"{ResourceRoot}/{resourceName}";
                var imported = Resources.Load<GameObject>(resourcePath);
                if (imported == null)
                    Fail($"UART-006 staged Unity resource is missing: {resourcePath}");
                var filters = imported.GetComponentsInChildren<MeshFilter>(true);
                if (filters == null || filters.Length == 0)
                    Fail($"UART-006 imported resource has no authored mesh: {resourcePath}");
            }

            Debug.Log($"AFAREET_P1_PRODUCTION_LANDMARK_GATE_OK task=UART-006 modules={manifest.modules.Length}");
        }

        private static void ValidateSource(string repositoryRoot, string sourceRoot, Module module)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.model))
                Fail("UART-006 contains an incomplete module record.");
            if (module.productionMinVertices <= 0 || module.productionMinTriangles <= 0)
                Fail($"UART-006 {module.model} is missing anti-blockout geometry floors.");

            var relative = $"{sourceRoot.TrimEnd('/')}/{module.model}";
            var absolute = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
                Fail($"UART-006 authored source is missing: {relative}");

            var stats = ReadObjStats(absolute);
            if (stats.Vertices < module.productionMinVertices || stats.Triangles < module.productionMinTriangles)
                Fail($"UART-006 blockout rejected: {module.model} has {stats.Vertices} vertices/{stats.Triangles} triangles; requires at least {module.productionMinVertices}/{module.productionMinTriangles}.");
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
                var corners = tokens.Length - 1;
                if (corners >= 3) triangles += corners - 2;
            }
            return new ObjStats(vertices, triangles);
        }

        private static string RepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }

        private static void Fail(string reason)
        {
            throw new BuildFailedException($"AFAREET_P1_PRODUCTION_LANDMARK_GATE_BLOCKED {reason}");
        }
    }
}
