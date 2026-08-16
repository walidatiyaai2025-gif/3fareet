using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Fail-closed UART-004 Android gate. Production builds may never synthesize rival
    /// production assets from code/design profiles at build time. Three externally authored
    /// model-backed prefabs must already exist, pass production validation and remain bound
    /// to the exact imported source model GUID/dependency hash used by every LOD mesh.
    /// Each rival must also use a distinct authored model source.
    /// </summary>
    public sealed class RivalProductionBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            if (AfareetBuildContext.IsDedicatedExperimentalAndroidBuild(report))
            {
                Debug.LogWarning(
                    "AFAREET_UART004_EXPERIMENTAL_GATE_BYPASS " +
                    "productionEvidence=false fallback=procedural-rivals");
                return;
            }

            RivalProductionPolicy.ValidateContract();
            var usedSourceGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
            {
                var path = RivalProductionPolicy.AssetPath(variant);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    Fail(variant, $"missing-external-authored-prefab path={path}");

                if (!RivalProductionPolicy.ValidateProductionPrefab(prefab, variant, out var reason))
                    Fail(variant, $"reason={reason} path={path}");

                var sourceGuid = ValidateExternalSourceProvenanceOrThrow(prefab, variant, path);
                if (!usedSourceGuids.Add(sourceGuid))
                    Fail(
                        variant,
                        $"reason=duplicate-authored-source-guid guid={sourceGuid} path={path} " +
                        "expected=three-distinct-rival-model-sources");
            }

            Debug.Log(
                "AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK variants=3 source=external-authored-3d " +
                "importedSources=3 distinctSources=3 guidHashBound=true meshSourceBound=true primitiveFallback=false");
        }

        private static string ValidateExternalSourceProvenanceOrThrow(GameObject prefab, int variant, string prefabPath)
        {
            var metadata = prefab.GetComponent<RivalProductionAssetMetadata>();
            if (metadata == null)
                Fail(variant, $"reason=missing-production-metadata prefab={prefabPath}");

            var sourcePath = (metadata.SourceAssetId ?? string.Empty).Replace('\\', '/');
            if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                Fail(variant, $"reason=unsupported-authored-model-source source={sourcePath} prefab={prefabPath}");

            var importedSource = AssetDatabase.LoadMainAssetAtPath(sourcePath);
            if (importedSource == null || AssetImporter.GetAtPath(sourcePath) == null)
                Fail(
                    variant,
                    $"reason=missing-imported-authored-source source={sourcePath} prefab={prefabPath} " +
                    "expected=tracked-Unity-Assets-model(.fbx|.obj|.blend|.glb|.gltf)");

            var importedPath = AssetDatabase.GetAssetPath(importedSource).Replace('\\', '/');
            if (!string.Equals(importedPath, sourcePath, StringComparison.Ordinal))
                Fail(variant, $"reason=authored-source-path-mismatch metadata={sourcePath} imported={importedPath} prefab={prefabPath}");

            var currentGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(currentGuid) ||
                !string.Equals(currentGuid, metadata.SourceGuid, StringComparison.OrdinalIgnoreCase))
                Fail(
                    variant,
                    $"reason=source-guid-mismatch expected={metadata.SourceGuid} actual={currentGuid} source={sourcePath} prefab={prefabPath}");

            var currentDependencyHash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
            if (string.IsNullOrWhiteSpace(currentDependencyHash) ||
                !string.Equals(currentDependencyHash, metadata.SourceDependencyHash, StringComparison.OrdinalIgnoreCase))
                Fail(
                    variant,
                    $"reason=source-dependency-hash-mismatch expected={metadata.SourceDependencyHash} " +
                    $"actual={currentDependencyHash} source={sourcePath} prefab={prefabPath}");

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
                Fail(variant, $"reason=missing-lod-group prefab={prefabPath}");

            var lods = group.GetLODs();
            for (var lod = 0; lod < lods.Length; lod++)
            {
                foreach (var renderer in lods[lod].renderers)
                {
                    if (renderer == null)
                        Fail(variant, $"reason=lod{lod}-null-renderer prefab={prefabPath}");

                    var mesh = RivalProductionPolicy.MeshFor(renderer);
                    if (mesh == null)
                        Fail(variant, $"reason=lod{lod}-missing-source-mesh prefab={prefabPath}");

                    var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                    if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                        Fail(
                            variant,
                            $"reason=lod{lod}-mesh-not-backed-by-source mesh={meshPath} source={sourcePath} prefab={prefabPath}");
                }
            }

            return currentGuid;
        }

        private static void Fail(int variant, string detail)
        {
            throw new BuildFailedException(
                $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} {detail} " +
                "handoff=docs/assets/01_vehicles/rival_cars_production/README.md");
        }
    }
}
