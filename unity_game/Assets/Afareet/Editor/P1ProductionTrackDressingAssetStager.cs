using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    public static class P1ProductionTrackDressingAssetStager
    {
        private const string SourceRoot = "docs/assets/02_tracks_environments/cairo_track_dressing/source";
        private const string GeneratedAssetRoot = "Assets/Afareet/Resources/Art/TracksEnvironments/CairoTrackDressing/Generated";
        private const string ResourceRoot = "Art/TracksEnvironments/CairoTrackDressing/Generated";
        private static readonly string[] Models =
        {
            "SM_Track_FinishGate_A.obj",
            "SM_Track_SpiritRune_A.obj",
            "SM_Track_DesertGround_A.obj",
            "SM_Track_SectorBeacon_A.obj"
        };

        [MenuItem("Afareet/P1/Stage Cairo Track Dressing Sources")]
        public static void StageMenu() => StageTrackedSourcesOrThrow();

        [InitializeOnLoadMethod]
        private static void ScheduleEditorStage()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                try { StageTrackedSourcesOrThrow(); }
                catch (Exception ex) { Debug.LogError($"AFAREET_UART007_STAGE_FAILED {ex}"); }
            };
        }

        public static void StageTrackedSourcesOrThrow()
        {
            var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var sourceRoot = Path.Combine(repo, SourceRoot.Replace('/', Path.DirectorySeparatorChar));
            var generated = Path.Combine(repo, "unity_game", GeneratedAssetRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(sourceRoot)) throw new InvalidOperationException($"UART-007 source root missing: {SourceRoot}");

            foreach (var model in Models)
            {
                if (!File.Exists(Path.Combine(sourceRoot, model)))
                    throw new InvalidOperationException($"UART-007 source missing: {model}");
            }

            Directory.CreateDirectory(generated);
            RemoveStaleStageableFiles(sourceRoot, generated);

            var changed = 0;
            var staged = 0;
            foreach (var source in Directory.GetFiles(sourceRoot, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsStageable(Path.GetExtension(source))) continue;
                staged++;
                var destination = Path.Combine(generated, Path.GetFileName(source));
                var bytes = File.ReadAllBytes(source);
                if (!File.Exists(destination) || !Same(bytes, File.ReadAllBytes(destination)))
                {
                    File.WriteAllBytes(destination, bytes);
                    changed++;
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var model in Models)
            {
                var path = $"{ResourceRoot}/{Path.GetFileNameWithoutExtension(model)}";
                if (Resources.Load<GameObject>(path) == null) throw new InvalidOperationException($"UART-007 Unity import failed: {path}");
            }
            Debug.Log(
                $"AFAREET_UART007_STAGE_OK models={Models.Length} stagedFiles={staged} changed={changed} " +
                "companions=mtl-textures source=tracked-docs generated=ignored-resources");
        }

        private static void RemoveStaleStageableFiles(string sourceRoot, string generatedRoot)
        {
            foreach (var destination in Directory.GetFiles(generatedRoot, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsStageable(Path.GetExtension(destination))) continue;
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

        private static bool Same(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
