using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Fail-closed UART-006 Android gate. Production-ready means more than nontrivial
    /// geometry: tracked OBJ source must carry authored UV/normal references and Unity's
    /// imported runtime renderers must retain texture-mapped materials.
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
            public readonly int TextureCoordinates;
            public readonly int Normals;
            public readonly int Faces;
            public readonly int FacesWithUvAndNormal;

            public ObjStats(int vertices, int triangles, int textureCoordinates, int normals, int faces, int facesWithUvAndNormal)
            {
                Vertices = vertices;
                Triangles = triangles;
                TextureCoordinates = textureCoordinates;
                Normals = normals;
                Faces = faces;
                FacesWithUvAndNormal = facesWithUvAndNormal;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            if (IsDedicatedExperimentalBuild(report))
            {
                Debug.LogWarning("AFAREET_UART006_EXPERIMENTAL_GATE_BYPASS productionEvidence=false");
                return;
            }
            ValidateAndroidCandidateOrThrow();
        }

        private static bool IsDedicatedExperimentalBuild(BuildReport report)
        {
            var output = (report.summary.outputPath ?? string.Empty).Replace('\\', '/');
            return AfareetBuildContext.IsExperimentalAndroidBuild &&
                   (report.summary.options & BuildOptions.Development) != 0 &&
                   output.EndsWith(
                       "Builds/Android/afareet-unity3d-experimental.apk",
                       StringComparison.OrdinalIgnoreCase);
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
                ValidateImportedResource(module);

            Debug.Log(
                $"AFAREET_P1_PRODUCTION_LANDMARK_GATE_OK task=UART-006 modules={manifest.modules.Length} " +
                "surfaces=uv0-authoredNormals-textureMapped");
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

            if (stats.TextureCoordinates <= 0 || stats.Normals <= 0 || stats.Faces <= 0 || stats.FacesWithUvAndNormal != stats.Faces)
                Fail(
                    $"UART-006 authored-surface rejected: {module.model} vt={stats.TextureCoordinates} vn={stats.Normals} " +
                    $"facesWithUvNormal={stats.FacesWithUvAndNormal}/{stats.Faces}. " +
                    "Production landmark OBJ faces must carry authored UV0 and normal references."
                );
        }

        private static void ValidateImportedResource(Module module)
        {
            var resourceName = Path.GetFileNameWithoutExtension(module.model);
            var resourcePath = $"{ResourceRoot}/{resourceName}";
            var imported = Resources.Load<GameObject>(resourcePath);
            if (imported == null)
                Fail($"UART-006 staged Unity resource is missing: {resourcePath}");

            var filters = imported.GetComponentsInChildren<MeshFilter>(true);
            if (filters == null || filters.Length == 0)
                Fail($"UART-006 imported resource has no authored mesh: {resourcePath}");

            var meshCount = 0;
            var allUv0 = true;
            var allNormals = true;
            var allRenderersTextured = true;
            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null) continue;
                meshCount++;
                var mesh = filter.sharedMesh;
                allUv0 &= mesh.uv != null && mesh.uv.Length == mesh.vertexCount;
                allNormals &= mesh.normals != null && mesh.normals.Length == mesh.vertexCount;

                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    allRenderersTextured = false;
                    continue;
                }

                var rendererHasTexture = false;
                var materials = renderer.sharedMaterials;
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        if (material != null && material.mainTexture != null)
                        {
                            rendererHasTexture = true;
                            break;
                        }
                    }
                }
                allRenderersTextured &= rendererHasTexture;
            }

            if (meshCount == 0)
                Fail($"UART-006 imported resource has no usable mesh data: {resourcePath}");
            if (!allUv0 || !allNormals || !allRenderersTextured)
                Fail(
                    $"UART-006 imported production surface rejected: {module.model} " +
                    $"uv0={allUv0} normals={allNormals} textures={allRenderersTextured}."
                );
        }

        private static ObjStats ReadObjStats(string path)
        {
            var vertices = 0;
            var triangles = 0;
            var textureCoordinates = 0;
            var normals = 0;
            var faces = 0;
            var facesWithUvAndNormal = 0;

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.StartsWith("v ", StringComparison.Ordinal))
                {
                    vertices++;
                    continue;
                }
                if (line.StartsWith("vt ", StringComparison.Ordinal))
                {
                    textureCoordinates++;
                    continue;
                }
                if (line.StartsWith("vn ", StringComparison.Ordinal))
                {
                    normals++;
                    continue;
                }
                if (!line.StartsWith("f ", StringComparison.Ordinal)) continue;

                var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var corners = tokens.Length - 1;
                if (corners < 3) continue;
                faces++;
                triangles += corners - 2;

                var faceHasUvAndNormal = true;
                for (var i = 1; i < tokens.Length; i++)
                {
                    var indices = tokens[i].Split('/');
                    if (indices.Length < 3 || string.IsNullOrEmpty(indices[1]) || string.IsNullOrEmpty(indices[2]))
                    {
                        faceHasUvAndNormal = false;
                        break;
                    }
                }
                if (faceHasUvAndNormal) facesWithUvAndNormal++;
            }

            return new ObjStats(vertices, triangles, textureCoordinates, normals, faces, facesWithUvAndNormal);
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
