using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Deterministically stages tracked UART-006 landmark OBJ sources into an ignored
    /// Resources directory so Unity's native model importer packages the authored meshes.
    /// </summary>
    public static class P1ProductionLandmarkAssetStager
    {
        private const string SourceRoot = "docs/assets/03_props_architecture/cairo_landmarks/source";
        private const string GeneratedAssetRoot = "Assets/Afareet/Resources/Art/Architecture/CairoLandmarks/Generated";
        private const string ResourceRoot = "Art/Architecture/CairoLandmarks/Generated";

        private static readonly string[] Models =
        {
            "SM_Landmark_GizaPyramid_A.obj",
            "SM_Landmark_Minaret_A.obj",
            "SM_Landmark_DomeGate_A.obj",
            "SM_Landmark_BridgeGantry_A.obj"
        };

        [MenuItem("Afareet/P1/Stage Cairo Landmark Sources")]
        public static void StageMenu()
        {
            StageTrackedSourcesOrThrow();
            Debug.Log("AFAREET_UART006_STAGE_MENU_OK");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleEditorStage()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    StageTrackedSourcesOrThrow();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AFAREET_UART006_STAGE_FAILED {ex}");
                }
            };
        }

        public static void StageTrackedSourcesOrThrow()
        {
            var repositoryRoot = RepositoryRoot();
            var sourceRoot = Path.Combine(repositoryRoot, SourceRoot.Replace('/', Path.DirectorySeparatorChar));
            var generatedAbsolute = Path.Combine(
                repositoryRoot,
                "unity_game",
                GeneratedAssetRoot.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(sourceRoot))
                throw new InvalidOperationException($"UART-006 source root is missing: {SourceRoot}");

            Directory.CreateDirectory(generatedAbsolute);
            var changed = 0;
            foreach (var model in Models)
            {
                var source = Path.Combine(sourceRoot, model);
                var destination = Path.Combine(generatedAbsolute, model);
                if (!File.Exists(source))
                    throw new InvalidOperationException($"UART-006 tracked source is missing: {SourceRoot}/{model}");

                var sourceBytes = File.ReadAllBytes(source);
                if (!File.Exists(destination) || !BytesEqual(sourceBytes, File.ReadAllBytes(destination)))
                {
                    File.WriteAllBytes(destination, sourceBytes);
                    changed++;
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var model in Models)
            {
                var resourceName = Path.GetFileNameWithoutExtension(model);
                var resourcePath = $"{ResourceRoot}/{resourceName}";
                if (Resources.Load<GameObject>(resourcePath) == null)
                    throw new InvalidOperationException($"UART-006 staged model failed Unity import: {resourcePath}");
            }

            Debug.Log($"AFAREET_UART006_STAGE_OK models={Models.Length} changed={changed} source=tracked-docs generated=ignored-resources");
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static string RepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }
    }
}
