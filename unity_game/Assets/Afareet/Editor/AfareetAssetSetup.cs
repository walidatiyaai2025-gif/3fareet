using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    public static class AfareetAssetSetup
    {
        private const string ConfigDirectory = "Assets/Afareet/Resources/Config";

        [MenuItem("Afareet/Setup/Ensure Production Config Assets")]
        public static void EnsureConfigAssets()
        {
            Directory.CreateDirectory(ConfigDirectory);
            EnsureAsset<ArcadeCarConfig>($"{ConfigDirectory}/ArcadeCarConfig.asset");
            EnsureAsset<ChaseCameraConfig>($"{ConfigDirectory}/ChaseCameraConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AFAREET_CONFIG_ASSETS_READY");
        }

        private static void EnsureAsset<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
