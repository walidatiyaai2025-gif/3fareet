using System;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Binds the fixed UART-003 production prefab to one selected external model source.
    /// The binder records Unity's GUID/dependency hash and refuses to bind unless every
    /// production LOD mesh is actually backed by that source model.
    /// </summary>
    public static class HeroCarProductionSourceBinder
    {
        private const string MenuPath = "Afareet/Bind UART-003 Production Hero Source";

        [MenuItem(MenuPath, true)]
        private static bool CanBind()
        {
            var sourcePath = AssetDatabase.GetAssetPath(Selection.activeObject).Replace('\\', '/');
            return HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath) &&
                   AssetDatabase.LoadAssetAtPath<GameObject>(HeroCarLodPolicy.ProductionAssetPath) != null;
        }

        [MenuItem(MenuPath)]
        private static void BindSelectedSource()
        {
            var sourcePath = AssetDatabase.GetAssetPath(Selection.activeObject).Replace('\\', '/');
            if (!HeroCarProductionAssetMetadata.IsSupportedExternalModelSource(sourcePath))
                throw new InvalidOperationException($"UART-003 selected asset is not a supported external 3D model: {sourcePath}");
            if (sourcePath.Contains("/Generated/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"UART-003 generated source cannot be bound as production art: {sourcePath}");
            if (AssetImporter.GetAtPath(sourcePath) == null || AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                throw new InvalidOperationException($"UART-003 source is not importable by Unity: {sourcePath}");

            var root = PrefabUtility.LoadPrefabContents(HeroCarLodPolicy.ProductionAssetPath);
            if (root == null)
                throw new InvalidOperationException($"UART-003 production prefab is missing: {HeroCarLodPolicy.ProductionAssetPath}");

            try
            {
                var group = root.GetComponent<LODGroup>();
                if (group == null)
                    throw new InvalidOperationException("UART-003 production prefab must contain a root LODGroup before source binding.");

                var lods = group.GetLODs();
                if (lods == null || lods.Length != 3)
                    throw new InvalidOperationException($"UART-003 production prefab must contain exactly three LODs; found {lods?.Length ?? 0}.");

                var allUv0 = true;
                var allNormals = true;
                var allRenderersTextureMapped = true;

                for (var lod = 0; lod < lods.Length; lod++)
                {
                    if (lods[lod].renderers == null || lods[lod].renderers.Length == 0)
                        throw new InvalidOperationException($"UART-003 LOD{lod} has no renderer.");

                    foreach (var renderer in lods[lod].renderers)
                    {
                        if (renderer == null)
                            throw new InvalidOperationException($"UART-003 LOD{lod} contains a null renderer.");

                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null)
                            throw new InvalidOperationException($"UART-003 LOD{lod} renderer is missing a MeshFilter/sharedMesh.");

                        var meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh).Replace('\\', '/');
                        if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                $"UART-003 LOD{lod} mesh is not backed by the selected source. mesh={meshPath} source={sourcePath}");

                        var mesh = filter.sharedMesh;
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
                        allRenderersTextureMapped &= rendererHasTexture;
                    }
                }

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
                if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(dependencyHash))
                    throw new InvalidOperationException($"UART-003 could not fingerprint source: {sourcePath}");

                var metadata = root.GetComponent<HeroCarProductionAssetMetadata>() ??
                               root.AddComponent<HeroCarProductionAssetMetadata>();
                metadata.Configure(
                    true,
                    allUv0,
                    allNormals,
                    allRenderersTextureMapped,
                    sourcePath,
                    Path.GetFileNameWithoutExtension(sourcePath),
                    guid,
                    dependencyHash);

                if (!metadata.DeclaresProductionAuthoring)
                    throw new InvalidOperationException(
                        $"UART-003 source binding does not meet production surface/provenance requirements: " +
                        $"uv0={allUv0} normals={allNormals} textures={allRenderersTextureMapped} source={sourcePath}");

                PrefabUtility.SaveAsPrefabAsset(root, HeroCarLodPolicy.ProductionAssetPath);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"AFAREET_UART003_SOURCE_BIND_OK source={sourcePath} version={metadata.AssetVersion} " +
                    $"guid={guid} dependencyHash={dependencyHash}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
