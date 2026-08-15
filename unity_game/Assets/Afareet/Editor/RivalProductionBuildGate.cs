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
    /// model-backed prefabs must already exist and pass the runtime production validator.
    /// Their metadata must also resolve to real imported Unity model assets.
    /// </summary>
    public sealed class RivalProductionBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            RivalProductionPolicy.ValidateContract();

            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
            {
                var path = RivalProductionPolicy.AssetPath(variant);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    throw new BuildFailedException(
                        $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} " +
                        $"reason=missing-external-authored-prefab path={path} " +
                        "handoff=docs/assets/01_vehicles/rival_cars_production/README.md");

                if (!RivalProductionPolicy.ValidateProductionPrefab(prefab, variant, out var reason))
                    throw new BuildFailedException(
                        $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} reason={reason} path={path}");

                var metadata = prefab.GetComponent<RivalProductionAssetMetadata>();
                var sourcePath = metadata == null ? string.Empty : metadata.SourceAssetId;
                var importedSource = string.IsNullOrWhiteSpace(sourcePath)
                    ? null
                    : AssetDatabase.LoadMainAssetAtPath(sourcePath);
                if (importedSource == null)
                    throw new BuildFailedException(
                        $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} " +
                        $"reason=missing-imported-authored-source source={sourcePath} prefab={path} " +
                        "expected=tracked-Unity-Assets-model(.fbx|.obj|.blend|.glb|.gltf)");

                var importedPath = AssetDatabase.GetAssetPath(importedSource);
                if (importedPath != sourcePath)
                    throw new BuildFailedException(
                        $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} " +
                        $"reason=authored-source-path-mismatch metadata={sourcePath} imported={importedPath} prefab={path}");
            }

            Debug.Log("AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK variants=3 source=external-authored-3d importedSources=3 primitiveFallback=false");
        }
    }
}
