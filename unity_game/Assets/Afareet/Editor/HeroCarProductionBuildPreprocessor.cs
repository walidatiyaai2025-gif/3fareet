using System;
using Afareet.Vehicle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// UART-003 Android production-art gate. Player builds may never generate or silently
    /// substitute the deterministic preview mesh. A real externally-authored production
    /// prefab must already exist and remain traceable to its source model asset.
    ///
    /// The only exception is the explicit dedicated experimental APK build, whose identity
    /// is validated centrally by AfareetBuildContext.
    /// </summary>
    public sealed class HeroCarProductionBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;

            if (AfareetBuildContext.IsDedicatedExperimentalAndroidBuild(report))
            {
                Debug.LogWarning(
                    "AFAREET_UART003_EXPERIMENTAL_GATE_BYPASS " +
                    $"productionEvidence=false fallback=procedural-hero output={report.summary.outputPath}"
                );
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCarLodPolicy.ProductionAssetPath);
            if (prefab == null)
                Fail($"missing-authored-prefab path={HeroCarLodPolicy.ProductionAssetPath}");

            if (!HeroCarProductionVisual.ValidateProductionPrefab(prefab, out var reason))
                Fail($"reason={reason} path={HeroCarLodPolicy.ProductionAssetPath}");

            ValidateExternalSourceProvenanceOrThrow(prefab);

            var metadata = prefab.GetComponent<HeroCarProductionAssetMetadata>();
            Debug.Log(
                $"AFAREET_UART003_PRODUCTION_GATE_OK path={HeroCarLodPolicy.ProductionAssetPath} " +
                $"source={metadata.SourceAssetId} version={metadata.AssetVersion} " +
                $"guid={metadata.SourceGuid} dependencyHash={metadata.SourceDependencyHash}");
        }

        private static void ValidateExternalSourceProvenanceOrThrow(GameObject prefab)
        {
            var metadata = prefab.GetComponent<HeroCarProductionAssetMetadata>();
            if (metadata == null)
                Fail("missing-production-metadata");

            var sourcePath = (metadata.SourceAssetId ?? string.Empty).Replace('\\', '/');
            if (!sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
                Fail($"source-must-be-project-asset path={sourcePath}");
            if (sourcePath.IndexOf("/Generated/", StringComparison.OrdinalIgnoreCase) >= 0)
                Fail($"generated-source-is-not-production path={sourcePath}");
            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                Fail($"unsupported-external-model-source path={sourcePath}");

            var importer = AssetImporter.GetAtPath(sourcePath);
            if (importer == null || AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                Fail($"external-model-source-not-imported path={sourcePath}");

            var currentGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(currentGuid) ||
                !string.Equals(currentGuid, metadata.SourceGuid, StringComparison.OrdinalIgnoreCase))
                Fail($"source-guid-mismatch expected={metadata.SourceGuid} actual={currentGuid} path={sourcePath}");

            var currentDependencyHash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
            if (string.IsNullOrWhiteSpace(currentDependencyHash) ||
                !string.Equals(currentDependencyHash, metadata.SourceDependencyHash, StringComparison.OrdinalIgnoreCase))
                Fail(
                    $"source-dependency-hash-mismatch expected={metadata.SourceDependencyHash} " +
                    $"actual={currentDependencyHash} path={sourcePath}");

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
                Fail("missing-lod-group-for-provenance-check");

            var lods = group.GetLODs();
            for (var lod = 0; lod < lods.Length; lod++)
            {
                foreach (var renderer in lods[lod].renderers)
                {
                    if (renderer == null) Fail($"lod{lod}-null-renderer");
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                        Fail($"lod{lod}-missing-source-mesh");

                    var meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh).Replace('\\', '/');
                    if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                        Fail($"lod{lod}-mesh-not-backed-by-source mesh={meshPath} source={sourcePath}");
                }
            }
        }

        private static void Fail(string reason)
        {
            throw new BuildFailedException($"AFAREET_UART003_PRODUCTION_GATE_BLOCKED {reason}");
        }
    }
}
