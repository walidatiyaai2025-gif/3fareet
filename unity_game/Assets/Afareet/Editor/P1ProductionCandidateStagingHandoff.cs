using System;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Licensed-Unity handoff for the tracked production-art inputs that must exist before
    /// a clean exact-SHA candidate can be tested or built. This method deliberately stops
    /// after staging/binding. It never runs tests, builds a Player, changes acceptance state,
    /// or marks any artifact VERIFIED.
    /// </summary>
    public static class P1ProductionCandidateStagingHandoff
    {
        private const string HeroSourceArgument = "-afareetHeroSource";
        private const string ReportRelativePath = "artifacts/production-staging/p1-staging-handoff.json";

        [Serializable]
        private sealed class HandoffReport
        {
            public int schemaVersion = 1;
            public string state = "STAGED_FOR_COMMIT_NOT_CANDIDATE";
            public bool verified = false;
            public bool publicationEligible = false;
            public bool candidateBuildStarted = false;
            public bool trackedCommitRequired = true;
            public string heroSource;
            public string unityVersion;
            public string utc;
            public string[] stagedTasks = { "UART-003", "UART-004", "UART-005", "UART-006", "UART-007" };
            public string nextAction = "Review Git changes, commit source/import metadata/prefabs, then run the clean exact-SHA candidate pipeline.";
        }

        /// <summary>
        /// Unity batch-mode entry point. Invoke with:
        /// -executeMethod Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit
        /// -afareetHeroSource Assets/.../AfareetKing.<fbx|obj|blend|glb|gltf>
        /// </summary>
        public static void StageForCommit()
        {
            StageForCommit(RequiredArgument(HeroSourceArgument));
        }

        internal static void StageForCommit(string heroSourcePath)
        {
            heroSourcePath = NormalizeAssetPath(heroSourcePath);
            ValidateHeroSourceBeforeMutation(heroSourcePath);

            Debug.Log($"AFAREET_P1_STAGING_HANDOFF_START heroSource={heroSourcePath} verified=false");

            // Ignored Resources staging is deterministic packaging of tracked source bytes.
            // It is included here to prove licensed Unity importability on the handoff machine.
            P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();
            P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow();
            P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow();

            // These two operations create/update tracked production prefab/import provenance.
            // They must be reviewed and committed before the exact-SHA candidate pipeline runs.
            RivalProductionPrefabStager.StageAndBindAll();
            HeroCarProductionPrefabStager.StageAndBind(heroSourcePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            WriteHandoffReport(heroSourcePath);
            Debug.Log(
                "AFAREET_P1_STAGING_HANDOFF_OK tasks=UART-003,UART-004,UART-005,UART-006,UART-007 " +
                "state=STAGED_FOR_COMMIT_NOT_CANDIDATE trackedCommitRequired=true " +
                "candidateBuildStarted=false publicationEligible=false verified=false");
        }

        private static void ValidateHeroSourceBeforeMutation(string sourcePath)
        {
            if (!sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException($"UART-003 batch handoff requires a Unity Assets/ path: {sourcePath}");
            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                throw new InvalidOperationException($"UART-003 batch handoff requires a supported external model: {sourcePath}");

            var lowered = sourcePath.ToLowerInvariant();
            foreach (var forbidden in new[] { "/generated/", "/preview/", "/blockout/" })
            {
                if (lowered.Contains(forbidden))
                    throw new InvalidOperationException($"UART-003 batch handoff rejects non-production source path segment {forbidden}: {sourcePath}");
            }

            if (AssetImporter.GetAtPath(sourcePath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
                throw new InvalidOperationException($"UART-003 Hero source has not been imported by licensed Unity: {sourcePath}");
        }

        private static string RequiredArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (!string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) continue;
                var value = args[index + 1];
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            throw new InvalidOperationException($"Required Unity batch argument is missing: {name}");
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Trim().Trim('"').Replace('\\', '/');
        }

        private static void WriteHandoffReport(string heroSourcePath)
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var reportPath = Path.Combine(repositoryRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(reportPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("P1 staging handoff report path has no directory.");

            Directory.CreateDirectory(directory);
            var report = new HandoffReport
            {
                heroSource = heroSourcePath,
                unityVersion = Application.unityVersion,
                utc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + Environment.NewLine);
        }
    }
}
