using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Afareet.Editor
{
    public sealed class HeroCarProductionBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            HeroCarProductionAssetBuilder.BuildOrThrow();
        }
    }
}
