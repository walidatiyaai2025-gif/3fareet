using Afareet.Vehicle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Fail-closed UART-004 Android gate. All three authored rival production prefabs must
    /// already be imported and pass geometry/surface-authoring validation before an APK builds.
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
                        $"reason=missing-authored-prefab path={path}");

                if (!RivalProductionPolicy.ValidateProductionPrefab(prefab, variant, out var reason))
                    throw new BuildFailedException(
                        $"AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED variant={variant + 1} " +
                        $"reason={reason} path={path}");
            }

            Debug.Log("AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK variants=3");
        }
    }
}
