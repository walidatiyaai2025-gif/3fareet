using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Stages the Blender-generated Afareet King model strictly as a visual refinement candidate.
    /// This path exists to let the team inspect the uploaded model in Editor/experimental APKs
    /// while preserving the UART-003 production gate for a later genuinely authored and
    /// mobile-optimized asset.
    /// </summary>
    public static class HeroCarRefinementCandidateStager
    {
        private const string MenuPath = "Afareet/P1/Hero/Stage Blender Refinement Candidate";
        private const string ExpectedSourceSha256 = "97b02c87118c451d068c881fc551787d6e468ec8002cce7802db62258cc4cda2";

        [MenuItem(MenuPath)]
        public static void StageCurrentCandidate()
        {
            ValidateCurrentCandidateSourceOrThrow();

            var sourcePath = HeroCarLodPolicy.RefinementCandidateSourcePath;
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var actualSha256 = Sha256ForProjectAsset(sourcePath);

            EnsureAssetFolder(Path.GetDirectoryName(HeroCarLodPolicy.RefinementCandidateAssetPath)?.Replace('\\', '/'));

            var root = new GameObject("AFAREET KING — REFINEMENT CANDIDATE");
            GameObject modelInstance = null;
            try
            {
                modelInstance = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
                if (modelInstance == null)
                    throw new InvalidOperationException("Unity could not instantiate the refinement FBX.");

                // Blender/FBX may already carry LODGroup components derived from authored LOD
                // collections/nodes. The refinement prefab owns one explicit authoritative
                // three-level LODGroup at its root, so imported groups must not survive and
                // register the same MeshRenderers a second time in Unity 6.
                if (PrefabUtility.IsPartOfPrefabInstance(modelInstance))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        modelInstance,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                var importedLodGroups = modelInstance.GetComponentsInChildren<LODGroup>(true);
                var removedImportedLodGroups = importedLodGroups.Length;
                foreach (var importedLodGroup in importedLodGroups)
                {
                    if (importedLodGroup != null)
                        UnityEngine.Object.DestroyImmediate(importedLodGroup);
                }

                modelInstance.name = "AfareetKing_Hero_Source";
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                var rendererBuckets = new[]
                {
                    new List<Renderer>(),
                    new List<Renderer>(),
                    new List<Renderer>()
                };

                foreach (var renderer in modelInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    rendererBuckets[ClassifyLod(renderer.gameObject.name)].Add(renderer);
                }

                for (var lod = 0; lod < 3; lod++)
                {
                    if (rendererBuckets[lod].Count == 0)
                        throw new InvalidOperationException($"Refinement FBX has no renderers classified as LOD{lod}.");
                }

                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(HeroCarLodPolicy.Lod0Transition, rendererBuckets[0].ToArray()),
                    new LOD(HeroCarLodPolicy.Lod1Transition, rendererBuckets[1].ToArray()),
                    new LOD(HeroCarLodPolicy.Lod2Transition, rendererBuckets[2].ToArray())
                });
                lodGroup.RecalculateBounds();

                Debug.Log(
                    $"AFAREET_HERO_REFINEMENT_LOD_AUTHORITY sourceLodGroupsRemoved={removedImportedLodGroups} " +
                    "stagedLodGroups=1 duplicateRendererRegistration=false productionGate=false");

                var withinBudget = true;
                for (var lod = 0; lod < 3; lod++)
                {
                    var vertices = 0;
                    var triangles = 0;
                    foreach (var renderer in rendererBuckets[lod])
                    {
                        var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        if (mesh == null) continue;
                        vertices += mesh.vertexCount;
                        triangles += TriangleCount(mesh);
                    }

                    var thisLodWithinBudget = HeroCarLodPolicy.IsWithinBudget(lod, vertices, triangles);
                    withinBudget &= thisLodWithinBudget;
                    Debug.Log(
                        $"AFAREET_HERO_REFINEMENT_LOD lod={lod} renderers={rendererBuckets[lod].Count} " +
                        $"vertices={vertices} triangles={triangles} productionBudget={thisLodWithinBudget}");
                }

                var marker = root.AddComponent<HeroCarRefinementCandidateMarker>();
                marker.Configure(sourcePath, actualSha256, withinBudget);

                PrefabUtility.SaveAsPrefabAsset(root, HeroCarLodPolicy.RefinementCandidateAssetPath);
                AssetDatabase.SaveAssets();

                var staged = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCarLodPolicy.RefinementCandidateAssetPath);
                if (!HeroCarProductionVisual.ValidateRefinementCandidatePrefab(staged, out var reason))
                    throw new InvalidOperationException($"Staged refinement candidate failed validation: {reason}");

                Debug.Log(
                    $"AFAREET_HERO_REFINEMENT_STAGED path={HeroCarLodPolicy.RefinementCandidateAssetPath} " +
                    $"source={sourcePath} sha256={actualSha256} mobileBudgetReady={withinBudget} " +
                    "productionGate=false");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Non-mutating source intake check used before the full P1 visual stack begins staging.
        /// It deliberately validates only the refinement-candidate identity/import boundary;
        /// it does not imply production acceptance or UART-003 closure.
        /// </summary>
        public static void ValidateCurrentCandidateSourceOrThrow()
        {
            var sourcePath = HeroCarLodPolicy.RefinementCandidateSourcePath;
            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                throw new InvalidOperationException($"Refinement source is not a supported model: {sourcePath}");
            if (!HeroCarProductionAssetMetadata.IsNonProductionSourcePath(sourcePath))
                throw new InvalidOperationException(
                    $"Refinement source must remain under an explicitly non-production path: {sourcePath}");

            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceModel == null)
                throw new InvalidOperationException(
                    $"Refinement source is not imported. Run tools/android/import_hero_refinement_candidate_windows.ps1 first. Expected: {sourcePath}");

            var actualSha256 = Sha256ForProjectAsset(sourcePath);
            if (!string.Equals(actualSha256, ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Refinement source SHA-256 mismatch. expected={ExpectedSourceSha256} actual={actualSha256}");

            Debug.Log(
                $"AFAREET_HERO_REFINEMENT_PREFLIGHT_OK source={sourcePath} sha256={actualSha256} " +
                "classification=REFINEMENT_CANDIDATE productionGate=false");
        }

        private static int ClassifyLod(string objectName)
        {
            var name = objectName ?? string.Empty;
            if (name.IndexOf("_LOD2", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            if (name.IndexOf("_LOD1", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return 0;
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }

        private static string Sha256ForProjectAsset(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Could not resolve Unity project root.");

            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            using var stream = File.OpenRead(fullPath);
            using var sha = SHA256.Create();
            var digest = sha.ComputeHash(stream);
            return BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder) || assetFolder == "Assets") return;
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            var parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetFolder);
            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetFolder))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
