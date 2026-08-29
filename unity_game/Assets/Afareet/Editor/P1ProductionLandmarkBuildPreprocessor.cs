using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Ensures tracked UART-006 authored landmark sources are imported before any player build.
    /// </summary>
    public sealed class P1ProductionLandmarkBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;

        public void OnPreprocessBuild(BuildReport report)
        {
            P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow();
            Debug.Log("AFAREET_UART006_BUILD_STAGE_OK source=tracked-obj");
        }
    }
}
