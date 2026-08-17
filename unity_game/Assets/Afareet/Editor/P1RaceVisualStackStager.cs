using System;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Operator-facing staging orchestrator for the current P1 race visual stack.
    /// It reuses the existing fail-closed UART stagers and deliberately keeps the
    /// Afareet King handoff classified as a refinement candidate. Successful staging
    /// is not production acceptance, owner visual approval, device proof, or P1 verification.
    /// </summary>
    public static class P1RaceVisualStackStager
    {
        private const string MenuPath = "Afareet/P1/Stage Full Race Visual Stack";

        [MenuItem(MenuPath)]
        public static void StageFullRaceVisualStack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "P1 visual stack staging must run outside Play Mode. Stop Play Mode and retry.");

            Debug.Log(
                "AFAREET_P1_VISUAL_STACK_STAGE_BEGIN " +
                "hero=refinement-candidate productionGate=false ownerAcceptance=false deviceProof=false");

            RunStage("UART-005 Cairo production sources", P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow);
            RunStage("UART-006 Cairo landmark sources", P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow);
            RunStage("UART-007 Cairo track dressing sources", P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow);
            RunStage("UART-004 rival production prefabs", RivalProductionPrefabStager.StageAndBindAll);
            RunStage("UART-003 Hero refinement candidate", HeroCarRefinementCandidateStager.StageCurrentCandidate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(
                "AFAREET_P1_VISUAL_STACK_STAGE_OK " +
                "uart004=staged uart005=staged uart006=staged uart007=staged " +
                "hero=refinement-candidate productionGate=false ownerAcceptance=false deviceProof=false p1Verified=false");
        }

        private static void RunStage(string label, Action stage)
        {
            Debug.Log($"AFAREET_P1_VISUAL_STACK_STAGE_STEP_BEGIN step={label}");
            try
            {
                stage();
                Debug.Log($"AFAREET_P1_VISUAL_STACK_STAGE_STEP_OK step={label}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"AFAREET_P1_VISUAL_STACK_STAGE_STEP_BLOCKED step={label} error={ex.Message}");
                throw;
            }
        }
    }
}
