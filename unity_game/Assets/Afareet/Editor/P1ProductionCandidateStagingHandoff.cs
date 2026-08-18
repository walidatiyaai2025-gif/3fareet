using System;
using System.Collections.Generic;
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
        private const string GitShaArgument = "-afareetGitSha";
        private const string HandoffPacketSha256Argument = "-afareetHandoffPacketSha256";
        private const string NativeHandoffVerificationSha256Argument = "-afareetNativeHandoffVerificationSha256";
        private const string OperatorChainSha256Argument = "-afareetOperatorChainSha256";
        private const string ReportRelativePath = "artifacts/production-staging/p1-staging-handoff.json";
        private const string VerticalSliceLayoutPath = "Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json";

        [Serializable]
        private sealed class TaskEvidence
        {
            public string taskId;
            public string state;
            public string sourceEvidence;
            public string runtimeEvidence;
            public bool verified = false;
            public bool runtimeVerified = false;
            public bool ownerAccepted = false;
        }

        [Serializable]
        private sealed class HandoffReport
        {
            public int schemaVersion = 3;
            public string state = "STAGED_FOR_COMMIT_NOT_CANDIDATE";
            public bool verified = false;
            public bool runtimeVerified = false;
            public bool ownerAccepted = false;
            public bool publicationEligible = false;
            public bool candidateBuildStarted = false;
            public bool trackedCommitRequired = true;
            public string gitSha;
            public string authorizationSourceGitSha;
            public string handoffPacketSha256;
            public string nativeHandoffVerificationSha256;
            public string operatorChainSha256;
            public string heroSource;
            public string heroSourceGuid;
            public string heroPrefabGuid;
            public string unityVersion;
            public string utc;
            public string[] coveredTasks = { "UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011" };
            public TaskEvidence[] taskEvidence;
            public string nextAction = "Review Git changes, commit source/import metadata/prefabs, then run the clean exact-SHA candidate pipeline and collect runtime/device/owner evidence.";
        }

        /// <summary>
        /// Unity batch-mode entry point. Invoke only through the authoritative P1 native
        /// handoff wrapper, which supplies the exact source commit and authorization hashes.
        /// </summary>
        public static void StageForCommit()
        {
            StageForCommit(
                RequiredArgument(HeroSourceArgument),
                RequiredArgument(GitShaArgument),
                RequiredArgument(HandoffPacketSha256Argument),
                RequiredArgument(NativeHandoffVerificationSha256Argument),
                RequiredArgument(OperatorChainSha256Argument));
        }

        internal static void StageForCommit(
            string heroSourcePath,
            string gitSha,
            string handoffPacketSha256,
            string nativeHandoffVerificationSha256,
            string operatorChainSha256)
        {
            heroSourcePath = NormalizeAssetPath(heroSourcePath);
            gitSha = NormalizeGitSha(gitSha);
            handoffPacketSha256 = NormalizeSha256(handoffPacketSha256, "handoff packet");
            nativeHandoffVerificationSha256 = NormalizeSha256(nativeHandoffVerificationSha256, "native handoff verification");
            operatorChainSha256 = NormalizeSha256(operatorChainSha256, "operator chain");

            // Validate every tracked external vehicle input before any world/landmark/dressing
            // stager is allowed to mutate the project. RivalProductionPrefabStager keeps its own
            // preflight as defense-in-depth, but this handoff-level gate prevents a missing Rival
            // production source from being discovered only after unrelated Cairo staging changed files.
            ValidateHeroSourceBeforeMutation(heroSourcePath);
            RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow();
            Debug.Log(
                "AFAREET_P1_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK hero=1 rivals=3 " +
                "trackedInputsValidated=true mutationStarted=false verified=false");

            Debug.Log(
                $"AFAREET_P1_STAGING_HANDOFF_START gitSha={gitSha} heroSource={heroSourcePath} " +
                $"packetSha256={handoffPacketSha256} nativeVerificationSha256={nativeHandoffVerificationSha256} " +
                "verified=false");

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

            var taskEvidence = BuildTaskEvidence(heroSourcePath);
            WriteHandoffReport(
                heroSourcePath,
                gitSha,
                handoffPacketSha256,
                nativeHandoffVerificationSha256,
                operatorChainSha256,
                taskEvidence);
            Debug.Log(
                $"AFAREET_P1_STAGING_HANDOFF_OK gitSha={gitSha} packetSha256={handoffPacketSha256} " +
                "tasks=UART-003,UART-004,UART-005,UART-006,UART-007,URAC-011 " +
                "state=STAGED_FOR_COMMIT_NOT_CANDIDATE trackedCommitRequired=true " +
                "candidateBuildStarted=false publicationEligible=false verified=false");
        }

        private static TaskEvidence[] BuildTaskEvidence(string heroSourcePath)
        {
            var heroSourceGuid = RequiredGuid(heroSourcePath, "UART-003 Hero source");
            var heroPrefabGuid = RequiredGuid(HeroCarLodPolicy.ProductionAssetPath, "UART-003 production prefab");

            var rivalSources = new List<string>();
            var rivalRuntime = new List<string>();
            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
            {
                var prefabPath = RivalProductionPolicy.AssetPath(variant);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (!RivalProductionPolicy.ValidateProductionPrefab(prefab, variant, out var reason))
                    throw new InvalidOperationException($"UART-004 handoff evidence rejects staged rival {variant + 1}: {reason}");

                var metadata = prefab.GetComponent<RivalProductionAssetMetadata>();
                if (metadata == null || string.IsNullOrWhiteSpace(metadata.SourceAssetId) || string.IsNullOrWhiteSpace(metadata.SourceGuid))
                    throw new InvalidOperationException($"UART-004 handoff evidence is missing source provenance for rival {variant + 1}.");

                var actualSourceGuid = RequiredGuid(metadata.SourceAssetId, $"UART-004 rival {variant + 1} source");
                if (!string.Equals(actualSourceGuid, metadata.SourceGuid, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"UART-004 handoff evidence source GUID mismatch for rival {variant + 1}: " +
                        $"metadata={metadata.SourceGuid} actual={actualSourceGuid}");

                rivalSources.Add($"{metadata.SourceAssetId}|guid={actualSourceGuid}");
                rivalRuntime.Add($"{prefabPath}|guid={RequiredGuid(prefabPath, $"UART-004 rival {variant + 1} production prefab")}");
            }

            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(VerticalSliceLayoutPath);
            if (layout == null)
                throw new InvalidOperationException($"URAC-011 authored vertical-slice layout failed licensed Unity import: {VerticalSliceLayoutPath}");
            if (layout.text.IndexOf("\"authoringState\": \"AUTHORED_LAYOUT\"", StringComparison.Ordinal) < 0 ||
                layout.text.IndexOf("\"layoutId\": \"cairo-night-vertical-slice-v1\"", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("URAC-011 imported layout no longer declares the authored vertical-slice identity.");
            var layoutGuid = RequiredGuid(VerticalSliceLayoutPath, "URAC-011 authored layout");

            return new[]
            {
                Evidence(
                    "UART-003",
                    "LICENSED_UNITY_STAGE_AND_BIND_OK",
                    $"{heroSourcePath}|guid={heroSourceGuid}",
                    $"{HeroCarLodPolicy.ProductionAssetPath}|guid={heroPrefabGuid}"),
                Evidence(
                    "UART-004",
                    "LICENSED_UNITY_STAGE_AND_BIND_OK",
                    string.Join(";", rivalSources),
                    string.Join(";", rivalRuntime)),
                Evidence(
                    "UART-005",
                    "LICENSED_UNITY_IMPORT_STAGE_OK",
                    "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow",
                    "CairoStreetKit ignored Resources staging imported successfully"),
                Evidence(
                    "UART-006",
                    "LICENSED_UNITY_IMPORT_STAGE_OK",
                    "P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow",
                    "CairoLandmarks ignored Resources staging imported successfully"),
                Evidence(
                    "UART-007",
                    "LICENSED_UNITY_IMPORT_STAGE_OK",
                    "P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow",
                    "CairoTrackDressing ignored Resources staging imported successfully"),
                Evidence(
                    "URAC-011",
                    "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
                    $"{VerticalSliceLayoutPath}|guid={layoutGuid}",
                    "CairoVerticalSliceLayout + Android build gate remain authoritative; exact Player/device proof still pending")
            };
        }

        private static TaskEvidence Evidence(string taskId, string state, string sourceEvidence, string runtimeEvidence)
        {
            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(state) ||
                string.IsNullOrWhiteSpace(sourceEvidence) || string.IsNullOrWhiteSpace(runtimeEvidence))
                throw new InvalidOperationException("P1 staging task evidence cannot contain blank identity/state/evidence fields.");

            return new TaskEvidence
            {
                taskId = taskId,
                state = state,
                sourceEvidence = sourceEvidence,
                runtimeEvidence = runtimeEvidence
            };
        }

        private static string RequiredGuid(string assetPath, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"{label} has no Unity GUID: {assetPath}");
            return guid;
        }

        private static void ValidateHeroSourceBeforeMutation(string sourcePath)
        {
            if (!sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException($"UART-003 batch handoff requires a Unity Assets/ path: {sourcePath}");
            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                throw new InvalidOperationException($"UART-003 batch handoff requires a supported external model: {sourcePath}");

            var lowered = sourcePath.ToLowerInvariant();
            foreach (var forbidden in new[] { "/generated/", "/preview/", "/blockout/", "/rivals/" })
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

        private static string NormalizeGitSha(string value)
        {
            value = (value ?? string.Empty).Trim().Trim('"').ToLowerInvariant();
            if (value.Length != 40)
                throw new InvalidOperationException($"P1 staging handoff requires a full 40-character Git SHA: {value}");
            for (var index = 0; index < value.Length; index++)
            {
                var c = value[index];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new InvalidOperationException($"P1 staging handoff Git SHA is not hexadecimal: {value}");
            }
            return value;
        }

        private static string NormalizeSha256(string value, string label)
        {
            value = (value ?? string.Empty).Trim().Trim('"').ToLowerInvariant();
            if (value.Length != 64)
                throw new InvalidOperationException($"P1 staging handoff {label} SHA-256 must be 64 hexadecimal characters: {value}");
            for (var index = 0; index < value.Length; index++)
            {
                var c = value[index];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new InvalidOperationException($"P1 staging handoff {label} SHA-256 is not hexadecimal: {value}");
            }
            return value;
        }

        private static void WriteHandoffReport(
            string heroSourcePath,
            string gitSha,
            string handoffPacketSha256,
            string nativeHandoffVerificationSha256,
            string operatorChainSha256,
            TaskEvidence[] taskEvidence)
        {
            if (taskEvidence == null || taskEvidence.Length != 6)
                throw new InvalidOperationException("P1 staging handoff report requires evidence for exactly six visual/runtime tasks.");

            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var reportPath = Path.Combine(repositoryRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(reportPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("P1 staging handoff report path has no directory.");

            Directory.CreateDirectory(directory);
            var report = new HandoffReport
            {
                gitSha = gitSha,
                authorizationSourceGitSha = gitSha,
                handoffPacketSha256 = handoffPacketSha256,
                nativeHandoffVerificationSha256 = nativeHandoffVerificationSha256,
                operatorChainSha256 = operatorChainSha256,
                heroSource = heroSourcePath,
                heroSourceGuid = RequiredGuid(heroSourcePath, "UART-003 Hero source"),
                heroPrefabGuid = RequiredGuid(HeroCarLodPolicy.ProductionAssetPath, "UART-003 production prefab"),
                unityVersion = Application.unityVersion,
                utc = DateTime.UtcNow.ToString("O"),
                taskEvidence = taskEvidence
            };
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + Environment.NewLine);
        }
    }
}