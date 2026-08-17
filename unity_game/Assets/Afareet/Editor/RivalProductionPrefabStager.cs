using System;
using System.Collections.Generic;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Editor
{
    /// <summary>
    /// Assembles UART-004 production prefabs only from already-imported authored model assets.
    /// This stager never creates Mesh data, primitives, or replacement geometry. It groups the
    /// LOD renderers that Unity imported from each tracked source, saves a prefab, then delegates
    /// provenance capture and production validation to RivalProductionSourceBinder.
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
                "resolver=authored-name-or-exact-source-triangle-signature " +
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
            var prefabPath = RivalProductionPolicy.AssetPath(variant);
            EnsureAssetDirectory(prefabPath);
            StagePrefabFromImportedSource(variant, sourcePath, sourceModel, signature, prefabPath);
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);

            RivalProductionSourceBinder.BindSource(variant, sourcePath);

            var staged = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!RivalProductionPolicy.ValidateProductionPrefab(staged, variant, out var reason))
                throw new InvalidOperationException(
                    $"UART-004 staged rival {variant + 1} failed post-bind validation: {reason}");

            Debug.Log(
                $"AFAREET_UART004_PREFAB_STAGE_OK variant={variant + 1} source={sourcePath} " +
                $"guid={sourceGuid} prefab={prefabPath} " +
                $"sourceSignatures={signature.Triangles[0]}/{signature.Triangles[1]}/{signature.Triangles[2]} " +
                "resolver=authored-name-or-exact-source-triangle-signature " +
                "geometryGenerated=false primitiveCreated=false");
        }

        private static void StagePrefabFromImportedSource(
            int variant,
            string sourcePath,
            GameObject sourceModel,
            RivalImportedLodResolver.SourceSignature signature,
            string prefabPath)
        {
            GameObject root = null;
            try
            {
                root = new GameObject($"PF_Rival_{variant + 1:00}_Production");
                var instance = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException(
                        $"UART-004 could not instantiate imported source model: variant={variant + 1} source={sourcePath}");

                instance.name = $"Rival_{variant + 1:00}_ImportedSource";
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var lodRenderers = new List<Renderer>[RivalProductionPolicy.MinimumTriangles.Length];
                for (var lod = 0; lod < lodRenderers.Length; lod++)
                    lodRenderers[lod] = new List<Renderer>();

                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var lod = RivalImportedLodResolver.Resolve(renderer, instance.transform, signature);
                    if (lod < 0) continue;

                    ValidateRendererSource(renderer, sourcePath, variant, lod);
                    lodRenderers[lod].Add(renderer);
                }

                var lods = new LOD[lodRenderers.Length];
                for (var lod = 0; lod < lodRenderers.Length; lod++)
                {
                    if (lodRenderers[lod].Count == 0)
                        throw new InvalidOperationException(
                            $"UART-004 imported source contains no renderer for LOD{lod}: variant={variant + 1} source={sourcePath} " +
                            $"authoredObject={signature.ObjectNames[lod]} sourceTriangles={signature.Triangles[lod]} " +
                            "resolver=transform-or-mesh-name-or-exact-source-triangle-signature");

                    lods[lod] = new LOD(TransitionHeights[lod], lodRenderers[lod].ToArray());
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

        private static void ValidateRendererSource(Renderer renderer, string sourcePath, int variant, int lod)
        {
            var mesh = RivalProductionPolicy.MeshFor(renderer);
            if (mesh == null)
                throw new InvalidOperationException(
                    $"UART-004 imported rival {variant + 1} LOD{lod} renderer has no mesh: {renderer.name}");

            var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"UART-004 stager refuses non-source mesh: variant={variant + 1} LOD{lod} " +
                    $"renderer={renderer.name} mesh={meshPath} source={sourcePath}");

            if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                throw new InvalidOperationException(
                    $"UART-004 imported rival {variant + 1} LOD{lod} mesh is missing UV0: {renderer.name}");
            if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                throw new InvalidOperationException(
                    $"UART-004 imported rival {variant + 1} LOD{lod} mesh is missing authored normals: {renderer.name}");

            var hasMappedTexture = false;
            foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
            {
                if (!HasAssignedTexture(material)) continue;
                hasMappedTexture = true;
                break;
            }
            if (!hasMappedTexture)
                throw new InvalidOperationException(
                    $"UART-004 imported rival {variant + 1} LOD{lod} renderer has no texture-mapped material: {renderer.name}");
        }

        private static bool HasAssignedTexture(Material material)
        {
            if (material == null || material.shader == null)
                return false;

            foreach (var propertyName in material.GetTexturePropertyNames())
            {
                if (material.GetTexture(propertyName) != null)
                    return true;
            }
            return false;
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
