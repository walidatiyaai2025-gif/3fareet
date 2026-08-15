using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Copies tracked UART-005 model sources plus material/texture companions into an
    /// ignored Unity Resources staging directory. Production source remains under docs;
    /// staged files are only Unity-importable packaging of that exact tracked source set.
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
            "SM_Prop_CairoBarrier_A.obj",
            "SM_Track_CairoRoad_A.obj",
            "SM_Track_CairoCurb_A.obj"
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

            foreach (var model in Models)
            {
                if (!File.Exists(Path.Combine(sourceRoot, model)))
                    throw new InvalidOperationException($"UART-005 tracked source is missing: {SourceRoot}/{model}");
            }

            Directory.CreateDirectory(generatedAbsolute);
            RemoveStaleStageableFiles(sourceRoot, generatedAbsolute);

            var changed = 0;
            var staged = 0;
            foreach (var source in Directory.GetFiles(sourceRoot, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsStageable(Path.GetExtension(source))) continue;
                staged++;

                var destination = Path.Combine(generatedAbsolute, Path.GetFileName(source));
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

            Debug.Log(
                $"AFAREET_UART005_STAGE_OK models={Models.Length} stagedFiles={staged} changed={changed} " +
                "companions=mtl-textures source=tracked-docs generated=ignored-resources");
        }

        private static void RemoveStaleStageableFiles(string sourceRoot, string generatedRoot)
        {
            foreach (var destination in Directory.GetFiles(generatedRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var extension = Path.GetExtension(destination);
                if (!IsStageable(extension)) continue;
                var source = Path.Combine(sourceRoot, Path.GetFileName(destination));
                if (!File.Exists(source)) File.Delete(destination);
            }
        }

        private static bool IsStageable(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".obj":
                case ".mtl":
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".bmp":
                case ".exr":
                case ".psd":
                    return true;
                default:
                    return false;
            }
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
