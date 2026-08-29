using System;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Binds one selected externally-authored model source to one UART-004 production rival.
    /// The binder refuses to record provenance unless every LOD mesh is actually backed by
    /// the exact deterministic source assigned to that variant and the imported prefab already
    /// satisfies the production surface contract.
    /// </summary>
    public static class RivalProductionSourceBinder
    {
        private const string MenuRoot = "Afareet/Bind UART-004/";

        [MenuItem(MenuRoot + "Rival 1 Source", true)]
        private static bool CanBindRival1() => CanBind(0);

        [MenuItem(MenuRoot + "Rival 1 Source")]
        private static void BindRival1() => BindSelected(0);

        [MenuItem(MenuRoot + "Rival 2 Source", true)]
        private static bool CanBindRival2() => CanBind(1);

        [MenuItem(MenuRoot + "Rival 2 Source")]
        private static void BindRival2() => BindSelected(1);

        [MenuItem(MenuRoot + "Rival 3 Source", true)]
        private static bool CanBindRival3() => CanBind(2);

        [MenuItem(MenuRoot + "Rival 3 Source")]
        private static void BindRival3() => BindSelected(2);

        private static bool CanBind(int variant)
        {
            var sourcePath = SelectedSourcePath();
            return RivalProductionPolicy.IsExactProductionSourceForVariant(variant, sourcePath) &&
                   AssetDatabase.LoadAssetAtPath<GameObject>(RivalProductionPolicy.AssetPath(variant)) != null;
        }

        private static void BindSelected(int variant)
        {
            BindSource(variant, SelectedSourcePath());
        }

        /// <summary>
        /// Shared fail-closed binding entry used by the explicit menu flow and the UART-004
        /// prefab stager. This method records provenance only; it never creates or modifies
        /// model geometry.
        /// </summary>
        internal static void BindSource(int variant, string sourcePath)
        {
            RivalProductionPolicy.ValidateContract();
            sourcePath = (sourcePath ?? string.Empty).Replace('\\', '/');

            var expectedSourcePath = RivalProductionPolicy.StagingSourcePath(variant);
            if (!RivalProductionPolicy.IsExactProductionSourceForVariant(variant, sourcePath))
                throw new InvalidOperationException(
                    $"UART-004 rival {variant + 1} source must match its deterministic production exchange path. " +
                    $"expected={expectedSourcePath} actual={sourcePath}");
            if (AssetImporter.GetAtPath(sourcePath) == null || AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                throw new InvalidOperationException($"UART-004 source is not importable by Unity: {sourcePath}");

            EnsureSourceIsUniqueAcrossOtherVariants(variant, sourcePath);

            var prefabPath = RivalProductionPolicy.AssetPath(variant);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                throw new InvalidOperationException($"UART-004 production prefab is missing: {prefabPath}");

            try
            {
                var group = root.GetComponent<LODGroup>();
                if (group == null)
                    throw new InvalidOperationException($"UART-004 rival {variant + 1} production prefab has no LODGroup.");

                var lods = group.GetLODs();
                if (lods == null || lods.Length != 3)
                    throw new InvalidOperationException(
                        $"UART-004 rival {variant + 1} must contain exactly three LODs; found {lods?.Length ?? 0}.");

                var allUv0 = true;
                var allNormals = true;
                var allTextureMapped = true;

                for (var lod = 0; lod < lods.Length; lod++)
                {
                    var renderers = lods[lod].renderers;
                    if (renderers == null || renderers.Length == 0)
                        throw new InvalidOperationException($"UART-004 rival {variant + 1} LOD{lod} has no renderers.");

                    foreach (var renderer in renderers)
                    {
                        if (renderer == null)
                            throw new InvalidOperationException($"UART-004 rival {variant + 1} LOD{lod} contains a null renderer.");

                        var mesh = RivalProductionPolicy.MeshFor(renderer);
                        if (mesh == null)
                            throw new InvalidOperationException($"UART-004 rival {variant + 1} LOD{lod} renderer is missing its mesh.");

                        var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                        if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                $"UART-004 rival {variant + 1} LOD{lod} mesh is not backed by selected source. " +
                                $"mesh={meshPath} source={sourcePath}");

                        allUv0 &= mesh.uv != null && mesh.uv.Length == mesh.vertexCount;
                        allNormals &= mesh.normals != null && mesh.normals.Length == mesh.vertexCount;

                        var rendererHasTexture = false;
                        if (renderer.sharedMaterials != null)
                        {
                            foreach (var material in renderer.sharedMaterials)
                            {
                                if (material != null && material.mainTexture != null)
                                {
                                    rendererHasTexture = true;
                                    break;
                                }
                            }
                        }
                        allTextureMapped &= rendererHasTexture;
                    }
                }

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
                if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(dependencyHash))
                    throw new InvalidOperationException($"UART-004 could not fingerprint source: {sourcePath}");

                var metadata = root.GetComponent<RivalProductionAssetMetadata>() ??
                               root.AddComponent<RivalProductionAssetMetadata>();
                metadata.Configure(
                    variant,
                    true,
                    allUv0,
                    allNormals,
                    allTextureMapped,
                    sourcePath,
                    Path.GetFileNameWithoutExtension(sourcePath),
                    $"{guid}:{dependencyHash}",
                    guid,
                    dependencyHash);

                if (!metadata.DeclaresProductionAuthoring)
                    throw new InvalidOperationException(
                        $"UART-004 rival {variant + 1} source binding does not meet production surface/provenance requirements: " +
                        $"uv0={allUv0} normals={allNormals} textures={allTextureMapped} source={sourcePath}");

                if (!RivalProductionPolicy.ValidateProductionPrefab(root, variant, out var reason))
                    throw new InvalidOperationException(
                        $"UART-004 rival {variant + 1} remains invalid after source binding: {reason}");

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"AFAREET_UART004_SOURCE_BIND_OK variant={variant + 1} source={sourcePath} " +
                    $"guid={guid} dependencyHash={dependencyHash} exactVariantSource=true distinctSource=true");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureSourceIsUniqueAcrossOtherVariants(int targetVariant, string sourcePath)
        {
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
            {
                if (variant == targetVariant) continue;

                var other = AssetDatabase.LoadAssetAtPath<GameObject>(RivalProductionPolicy.AssetPath(variant));
                if (other == null) continue;
                var metadata = other.GetComponent<RivalProductionAssetMetadata>();
                if (metadata == null) continue;

                if (string.Equals(metadata.SourceAssetId, sourcePath, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(sourceGuid) &&
                     string.Equals(metadata.SourceGuid, sourceGuid, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"UART-004 rival {targetVariant + 1} cannot reuse rival {variant + 1} authored source: {sourcePath}");
                }
            }
        }

        private static string SelectedSourcePath()
        {
            return (AssetDatabase.GetAssetPath(Selection.activeObject) ?? string.Empty).Replace('\\', '/');
        }
    }
}