using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Copies the tracked authored UART-005 OBJ sources into an ignored Unity Resources
    /// staging directory. This keeps exact-SHA source trees clean while allowing Unity's
    /// native model importer to package the tracked geometry into editor/player builds.
    /// </summary>
    public static class P1ProductionWorldAssetStager
    {
        private const string SourceRoot = "docs/assets/02_tracks_environments/cairo_street_kit/source";
        private const string GeneratedAssetRoot = "Assets/Afareet/Resources/Art/TracksEnvironments/CairoStreetKit/Generated";
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";

        private static readonly string[] Models =
        {
            "SM_Env_CairoFacade_A.obj",
            "SM_Env_CairoAwning_A.obj",
            "SM_Prop_CairoLamp_A.obj",
            "SM_Prop_CairoBarrier_A.obj"
        };

        [MenuItem("Afareet/P1/Stage Cairo Production Sources")]
        public static void StageMenu()
        {
            StageTrackedSourcesOrThrow();
            Debug.Log("AFAREET_UART005_STAGE_MENU_OK");
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
                    Debug.LogError($"AFAREET_UART005_STAGE_FAILED {ex}");
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
                throw new InvalidOperationException($"UART-005 source root is missing: {SourceRoot}");

            Directory.CreateDirectory(generatedAbsolute);
            var changed = 0;

            foreach (var model in Models)
            {
                var source = Path.Combine(sourceRoot, model);
                var destination = Path.Combine(generatedAbsolute, model);
                if (!File.Exists(source))
                    throw new InvalidOperationException($"UART-005 tracked source is missing: {SourceRoot}/{model}");

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
                var imported = Resources.Load<GameObject>(resourcePath);
                if (imported == null)
                    throw new InvalidOperationException($"UART-005 staged model failed Unity import: {resourcePath}");
            }

            Debug.Log($"AFAREET_UART005_STAGE_OK models={Models.Length} changed={changed} source=tracked-docs generated=ignored-resources");
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
            // Application.dataPath => <repo>/unity_game/Assets
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }
    }
}
