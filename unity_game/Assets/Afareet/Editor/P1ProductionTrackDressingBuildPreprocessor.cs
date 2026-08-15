using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    public sealed class P1ProductionTrackDressingBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -850;
        public void OnPreprocessBuild(BuildReport report)
        {
            P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow();
            Debug.Log("AFAREET_UART007_BUILD_STAGE_OK source=tracked-obj");
        }
    }
}
