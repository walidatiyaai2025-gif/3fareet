using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    public sealed class P1ProductionLandmarkMaterialDependencyGate : IPreprocessBuildWithReport
    {
        private const string ManifestRelativePath = "docs/assets/03_props_architecture/cairo_landmarks/ASSET_MANIFEST.json";

        [Serializable]
        private sealed class Manifest
        {
            public string taskId;
            public string reviewState;
            public string sourceQuality;
            public string sourceRoot;
            public Module[] modules;
        }

        [Serializable]
        private sealed class Module
        {
            public string model;
        }

        public int callbackOrder => -855;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            ValidateAndroidCandidateOrThrow();
        }

        public static void ValidateAndroidCandidateOrThrow()
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(manifestPath)) Fail($"production manifest is missing: {ManifestRelativePath}");

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || !string.Equals(manifest.taskId, "UART-006", StringComparison.Ordinal))
                Fail("production manifest is invalid or has an unexpected task id");

            if (!string.Equals(manifest.reviewState, "PRODUCTION_READY", StringComparison.Ordinal) ||
                !string.Equals(manifest.sourceQuality, "authored-production", StringComparison.Ordinal))
                return;

            if (string.IsNullOrWhiteSpace(manifest.sourceRoot) || manifest.modules == null || manifest.modules.Length == 0)
                Fail("production source root/modules are missing");

            var sourceRoot = Path.GetFullPath(Path.Combine(repositoryRoot, manifest.sourceRoot.Replace('/', Path.DirectorySeparatorChar)));
            if (!Directory.Exists(sourceRoot)) Fail($"production source root is missing: {manifest.sourceRoot}");

            foreach (var module in manifest.modules)
            {
                if (module == null || string.IsNullOrWhiteSpace(module.model)) Fail("manifest contains an incomplete module record");
                var objPath = Path.Combine(sourceRoot, module.model.Replace('/', Path.DirectorySeparatorChar));
                ObjProductionMaterialDependencyPolicy.ValidateOrThrow(
                    objPath,
                    sourceRoot,
                    detail => Fail($"module={module.model} {detail}"));
            }

            Debug.Log(
                $"AFAREET_UART006_MATERIAL_DEPENDENCY_GATE_OK modules={manifest.modules.Length} " +
                "provenance=obj-mtllib-usemtl-tracked-textures rootBound=true");
        }

        private static void Fail(string detail)
        {
            throw new BuildFailedException($"AFAREET_UART006_MATERIAL_DEPENDENCY_GATE_BLOCKED {detail}");
        }
    }
}
