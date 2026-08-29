using System;
using System.Collections.Generic;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Stages the UART-003 production Hero prefab strictly from one selected, already-imported
    /// external model asset. No Mesh data or primitives are created here; every LOD renderer
    /// must remain backed by the selected model source and pass the existing production policy.
    /// </summary>
    public static class HeroCarProductionPrefabStager
    {
        private const string MenuPath = "Afareet/Stage + Bind UART-003 Production Hero Source";

        [MenuItem(MenuPath, true)]
        private static bool CanStage()
        {
            var sourcePath = SelectedSourcePath();
            return HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath) &&
                   sourcePath.IndexOf("/Generated/", StringComparison.OrdinalIgnoreCase) < 0;
        }

        [MenuItem(MenuPath)]
        private static void StageSelected()
        {
            StageAndBind(SelectedSourcePath());
        }

        internal static void StageAndBind(string sourcePath)
        {
            HeroCarLodPolicy.ValidateContract();
            sourcePath = (sourcePath ?? string.Empty).Replace('\\', '/');

            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                throw new InvalidOperationException($"UART-003 stager requires a supported external model: {sourcePath}");
            if (sourcePath.IndexOf("/Generated/", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException($"UART-003 generated preview/source cannot be staged as production: {sourcePath}");

            var importer = AssetImporter.GetAtPath(sourcePath);
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (importer == null || sourceModel == null)
                throw new InvalidOperationException($"UART-003 selected model has not been imported by Unity: {sourcePath}");

            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceGuid))
                throw new InvalidOperationException($"UART-003 selected model has no Unity GUID: {sourcePath}");

            EnsureProductionDirectory();
            StagePrefabFromImportedSource(sourcePath, sourceModel);
            AssetDatabase.ImportAsset(HeroCarLodPolicy.ProductionAssetPath, ImportAssetOptions.ForceSynchronousImport);

            // The existing binder remains the authority for GUID/hash provenance and surface flags.
            HeroCarProductionSourceBinder.BindSource(sourcePath);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCarLodPolicy.ProductionAssetPath);
            if (!HeroCarProductionVisual.ValidateProductionPrefab(prefab, out var reason))
                throw new InvalidOperationException($"UART-003 staged Hero failed production validation: {reason}");

            Debug.Log(
                $"AFAREET_UART003_PREFAB_STAGE_OK source={sourcePath} guid={sourceGuid} " +
                $"prefab={HeroCarLodPolicy.ProductionAssetPath} geometryGenerated=false primitiveCreated=false");
        }

        private static void StagePrefabFromImportedSource(string sourcePath, GameObject sourceModel)
        {
            GameObject root = null;
            try
            {
                root = new GameObject("PF_Vehicle_AfareetKing_Production");
                var imported = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
                if (imported == null)
                    throw new InvalidOperationException($"UART-003 could not instantiate imported source model: {sourcePath}");

                imported.name = "AfareetKing Imported Production Source";
                imported.transform.SetParent(root.transform, false);
                imported.transform.localPosition = Vector3.zero;
                imported.transform.localRotation = Quaternion.identity;
                imported.transform.localScale = Vector3.one;

                var lodRenderers = new List<MeshRenderer>[3]
                {
                    new List<MeshRenderer>(),
                    new List<MeshRenderer>(),
                    new List<MeshRenderer>()
                };

                foreach (var renderer in imported.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var lod = ResolveLod(renderer.transform, imported.transform);
                    if (lod < 0) continue;
                    ValidateSourceRenderer(renderer, sourcePath, lod);
                    lodRenderers[lod].Add(renderer);
                }

                var lods = new LOD[3];
                for (var lod = 0; lod < lodRenderers.Length; lod++)
                {
                    if (lodRenderers[lod].Count != 1)
                        throw new InvalidOperationException(
                            $"UART-003 imported source must expose exactly one MeshRenderer for LOD{lod}; " +
                            $"found={lodRenderers[lod].Count} source={sourcePath} expectedObjectSuffix=_LOD{lod}");

                    var renderer = lodRenderers[lod][0];
                    var filter = renderer.GetComponent<MeshFilter>();
                    var triangleCount = TriangleCount(filter.sharedMesh);
                    if (!HeroCarLodPolicy.IsWithinBudget(lod, filter.sharedMesh.vertexCount, triangleCount))
                        throw new InvalidOperationException(
                            $"UART-003 imported LOD{lod} violates production geometry policy: " +
                            $"{filter.sharedMesh.vertexCount}v/{triangleCount}t source={sourcePath}");

                    lods[lod] = new LOD(HeroCarLodPolicy.TransitionFor(lod), new Renderer[] { renderer });
                }

                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.animateCrossFading = false;
                group.SetLODs(lods);
                group.RecalculateBounds();

                var saved = PrefabUtility.SaveAsPrefabAsset(root, HeroCarLodPolicy.ProductionAssetPath);
                if (saved == null)
                    throw new InvalidOperationException(
                        $"UART-003 failed to save production prefab: {HeroCarLodPolicy.ProductionAssetPath}");
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static int ResolveLod(Transform rendererTransform, Transform importedRoot)
        {
            for (var current = rendererTransform; current != null; current = current.parent)
            {
                for (var lod = 0; lod < 3; lod++)
                {
                    if (current.name.EndsWith($"_LOD{lod}", StringComparison.OrdinalIgnoreCase))
                        return lod;
                }

                if (current == importedRoot) break;
            }

            return -1;
        }

        private static void ValidateSourceRenderer(MeshRenderer renderer, string sourcePath, int lod)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                throw new InvalidOperationException($"UART-003 imported LOD{lod} renderer is missing MeshFilter/sharedMesh: {renderer.name}");

            var mesh = filter.sharedMesh;
            var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"UART-003 stager refuses non-source mesh at LOD{lod}: mesh={meshPath} source={sourcePath}");

            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                throw new InvalidOperationException($"UART-003 imported LOD{lod} is missing complete UV0: {renderer.name}");
            if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                throw new InvalidOperationException($"UART-003 imported LOD{lod} is missing authored normals: {renderer.name}");

            var textureMapped = false;
            foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
            {
                if (material != null && material.mainTexture != null)
                {
                    textureMapped = true;
                    break;
                }
            }
            if (!textureMapped)
                throw new InvalidOperationException($"UART-003 imported LOD{lod} has no texture-mapped material: {renderer.name}");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }

        private static void EnsureProductionDirectory()
        {
            var directory = Path.GetDirectoryName(HeroCarLodPolicy.ProductionAssetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("UART-003 production prefab path has no directory.");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static string SelectedSourcePath()
        {
            return (AssetDatabase.GetAssetPath(Selection.activeObject) ?? string.Empty).Replace('\\', '/');
        }
    }
}
