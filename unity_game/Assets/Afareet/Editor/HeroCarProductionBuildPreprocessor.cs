using Afareet.Vehicle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// UART-003 Android production-art gate. Player builds may never generate or silently
    /// substitute the deterministic preview mesh. A real authored production prefab must
    /// already exist and satisfy the runtime production-quality validator.
    /// </summary>
    public sealed class HeroCarProductionBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCarLodPolicy.ProductionAssetPath);
            if (prefab == null)
                throw new BuildFailedException(
                    $"AFAREET_UART003_PRODUCTION_GATE_BLOCKED missing-authored-prefab path={HeroCarLodPolicy.ProductionAssetPath}");

            if (!HeroCarProductionVisual.ValidateProductionPrefab(prefab, out var reason))
                throw new BuildFailedException(
                    $"AFAREET_UART003_PRODUCTION_GATE_BLOCKED reason={reason} path={HeroCarLodPolicy.ProductionAssetPath}");

            Debug.Log($"AFAREET_UART003_PRODUCTION_GATE_OK path={HeroCarLodPolicy.ProductionAssetPath}");
        }
    }
}
