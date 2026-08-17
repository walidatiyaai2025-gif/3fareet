using System;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Operator-facing staging orchestrator for the current P1 race visual-review stack.
    /// Production gates remain fail-closed; local Hero/Rival review candidates are explicitly
    /// non-production and exist only so the licensed Editor can show the authored race scene.
    /// </summary>
    public static class P1RaceVisualStackStager
    {
        private const string MenuPath = "Afareet/P1/Stage Full Race Visual Stack";
        private const string ResumeFromRivalsMenuPath = "Afareet/P1/Resume Visual Stack From Rivals";

        [MenuItem(MenuPath)]
        public static void StageFullRaceVisualStack()
        {
            EnsureOutsidePlayMode();

            // All read-only source checks run before the first visual artifact is staged.
            RunPreflight(
                "UART-003 Hero refinement candidate source",
                HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow);
            RunPreflight(
                "UART-004 rival tracked OBJ review sources",
                RivalAuthoredReviewPrefabStager.ValidateCurrentSourcesOrThrow);

            Debug.Log(
                "AFAREET_P1_VISUAL_STACK_STAGE_BEGIN " +
                "hero=refinement-candidate rivals=authored-review-candidates " +
                "productionGate=false ownerAcceptance=false deviceProof=false");

            RunStage("UART-005 Cairo production sources", P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow);
            RunStage("UART-006 Cairo landmark sources", P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow);
            RunStage("UART-007 Cairo track dressing sources", P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow);
            StageReviewTail("full-stack");
        }

        /// <summary>
        /// Resumes the operator-only visual review after Cairo staging. Some licensed Editor
        /// sessions can return control to Unity after an AssetDatabase refresh before the
        /// remaining menu invocation continues. This entry deliberately stages only the local
        /// UART-004 authored review rivals and UART-003 Hero refinement candidate; it does not
        /// rerun or promote any production gate.
        /// </summary>
        [MenuItem(ResumeFromRivalsMenuPath)]
        public static void ResumeVisualStackFromRivals()
        {
            EnsureOutsidePlayMode();

            RunPreflight(
                "UART-003 Hero refinement candidate source",
                HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow);
            RunPreflight(
                "UART-004 rival tracked OBJ review sources",
                RivalAuthoredReviewPrefabStager.ValidateCurrentSourcesOrThrow);

            Debug.Log(
                "AFAREET_P1_VISUAL_STACK_RESUME_BEGIN from=uart004-authored-review-rivals " +
                "productionGate=false ownerAcceptance=false deviceProof=false");

            StageReviewTail("resume-from-rivals");
        }

        private static void StageReviewTail(string invocation)
        {
            RunStage("UART-004 authored review rivals", RivalAuthoredReviewPrefabStager.StageAll);
            RunStage("UART-003 Hero refinement candidate", HeroCarRefinementCandidateStager.StageCurrentCandidate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(
                "AFAREET_P1_VISUAL_STACK_STAGE_OK " +
                $"invocation={invocation} " +
                "uart004=authored-review-candidates uart005=staged uart006=staged uart007=staged " +
                "hero=refinement-candidate productionGate=false ownerAcceptance=false deviceProof=false p1Verified=false");
        }

        private static void EnsureOutsidePlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "P1 visual stack staging must run outside Play Mode. Stop Play Mode and retry.");
        }

        private static void RunPreflight(string label, Action preflight)
        {
            Debug.Log($"AFAREET_P1_VISUAL_STACK_PREFLIGHT_BEGIN step={label}");
            try
            {
                preflight();
                Debug.Log($"AFAREET_P1_VISUAL_STACK_PREFLIGHT_OK step={label}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"AFAREET_P1_VISUAL_STACK_PREFLIGHT_BLOCKED step={label} error={ex.Message}");
                throw;
            }
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
