using System;
using System.Collections.Generic;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Assembles UART-004 production prefabs only from already-imported authored model assets.
    /// Unity may flatten the OBJ Model Prefab hierarchy, so this stager wraps the exact imported
    /// Mesh/Material sub-assets in LOD renderers. It never creates Mesh data, primitives or
    /// replacement geometry; source provenance remains bound to the tracked OBJ.
    /// </summary>
    public static class RivalProductionPrefabStager
    {
        private const string MenuRoot = "Afareet/Stage UART-004/";

        private static readonly string[] SourcePaths =
        {
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj"
        };

        private static readonly float[] TransitionHeights = { 0.60f, 0.28f, 0.08f };

        [MenuItem(MenuRoot + "Stage + Bind All Rival Prefabs")]
        private static void StageAndBindAllMenu() => StageAndBindAll();

        [MenuItem(MenuRoot + "Stage + Bind Rival 1")]
        private static void StageAndBindRival1() => StageAndBind(0);

        [MenuItem(MenuRoot + "Stage + Bind Rival 2")]
        private static void StageAndBindRival2() => StageAndBind(1);

        [MenuItem(MenuRoot + "Stage + Bind Rival 3")]
        private static void StageAndBindRival3() => StageAndBind(2);

        internal static void StageAndBindAll()
        {
            RivalProductionPolicy.ValidateContract();
            ValidateStaticContract();

            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
                StageAndBind(variant);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AFAREET_UART004_PREFAB_STAGE_ALL_OK variants=3 source=tracked-imported-models " +
                "resolver=imported-mesh-subassets+exact-source-signature " +
                "geometryGenerated=false primitiveCreated=false bindingDelegated=true");
        }

        internal static void StageAndBind(int variant)
        {
            RivalProductionPolicy.ValidateContract();
            ValidateStaticContract();
            if (variant < 0 || variant >= RivalProductionPolicy.VariantCount)
                throw new ArgumentOutOfRangeException(nameof(variant));

            var sourcePath = SourcePaths[variant];
            if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                throw new InvalidOperationException($"UART-004 stager source contract rejected: {sourcePath}");

            var importer = AssetImporter.GetAtPath(sourcePath);
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (importer == null || sourceModel == null)
                throw new InvalidOperationException(
                    $"UART-004 tracked source has not been imported by Unity: variant={variant + 1} source={sourcePath}");

            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceGuid))
                throw new InvalidOperationException(
                    $"UART-004 imported source has no Unity GUID: variant={variant + 1} source={sourcePath}");

            var signature = RivalImportedLodResolver.ParseSourceOrThrow(sourcePath);
            var meshes = RivalImportedLodResolver.ResolveImportedMeshesOrThrow(sourcePath, sourceModel, signature);
            var materials = RivalImportedLodResolver.ResolveImportedMaterialsOrThrow(sourcePath, sourceModel, signature);

            var prefabPath = RivalProductionPolicy.AssetPath(variant);
            EnsureAssetDirectory(prefabPath);
            StagePrefabFromImportedAssets(variant, sourcePath, signature, meshes, materials, prefabPath);
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);

            // Binder remains the single authority for GUID/dependency provenance and final
            // source-backed production validation.
            RivalProductionSourceBinder.BindSource(variant, sourcePath);

            var staged = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!RivalProductionPolicy.ValidateProductionPrefab(staged, variant, out var reason))
                throw new InvalidOperationException(
                    $"UART-004 staged rival {variant + 1} failed post-bind validation: {reason}");

            Debug.Log(
                $"AFAREET_UART004_PREFAB_STAGE_OK variant={variant + 1} source={sourcePath} " +
                $"guid={sourceGuid} prefab={prefabPath} " +
                $"sourceSignatures={signature.Triangles[0]}/{signature.Triangles[1]}/{signature.Triangles[2]} " +
                "resolver=imported-mesh-subassets+exact-source-signature " +
                "geometryGenerated=false primitiveCreated=false");
        }

        private static void StagePrefabFromImportedAssets(
            int variant,
            string sourcePath,
            RivalImportedLodResolver.SourceSignature signature,
            Mesh[] meshes,
            Material[][] materials,
            string prefabPath)
        {
            GameObject root = null;
            try
            {
                root = new GameObject($"PF_Rival_{variant + 1:00}_Production");
                var lods = new LOD[meshes.Length];

                for (var lod = 0; lod < meshes.Length; lod++)
                {
                    var mesh = meshes[lod];
                    var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                    if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"UART-004 stager refuses non-source mesh: variant={variant + 1} LOD{lod} mesh={meshPath}");
                    if (RivalImportedLodResolver.TriangleCount(mesh) != signature.Triangles[lod])
                        throw new InvalidOperationException(
                            $"UART-004 staged source triangle identity mismatch: variant={variant + 1} LOD{lod}");
                    if (materials[lod] == null || materials[lod].Length == 0)
                        throw new InvalidOperationException(
                            $"UART-004 staged source material slots missing: variant={variant + 1} LOD{lod}");
                    if (mesh.subMeshCount != materials[lod].Length)
                        throw new InvalidOperationException(
                            $"UART-004 imported material/submesh contract mismatch: variant={variant + 1} LOD{lod} " +
                            $"subMeshes={mesh.subMeshCount} materials={materials[lod].Length} " +
                            $"sourceMaterials={string.Join(",", signature.MaterialNames[lod])}");

                    var child = new GameObject($"Rival_{variant + 1:00}_LOD{lod}_SourceBacked");
                    child.transform.SetParent(root.transform, false);
                    var filter = child.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = materials[lod];
                    lods[lod] = new LOD(TransitionHeights[lod], new Renderer[] { renderer });
                }

                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = false;
                group.SetLODs(lods);
                group.RecalculateBounds();

                var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (saved == null)
                    throw new InvalidOperationException(
                        $"UART-004 failed to save staged production prefab: variant={variant + 1} path={prefabPath}");
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException($"UART-004 invalid prefab asset path: {assetPath}");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static void ValidateStaticContract()
        {
            if (SourcePaths.Length != RivalProductionPolicy.VariantCount)
                throw new InvalidOperationException("UART-004 stager must define exactly three source paths.");
            if (TransitionHeights.Length != RivalProductionPolicy.MinimumTriangles.Length)
                throw new InvalidOperationException("UART-004 stager must define exactly three LOD transition heights.");

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourcePath in SourcePaths)
            {
                if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                    throw new InvalidOperationException($"UART-004 invalid staged source path: {sourcePath}");
                if (!unique.Add(sourcePath))
                    throw new InvalidOperationException($"UART-004 stager source reuse is forbidden: {sourcePath}");
            }
        }
    }
}
