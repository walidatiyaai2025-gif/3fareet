using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Vehicle
{
    public sealed class RivalProductionAssetMetadata : MonoBehaviour
    {
        [SerializeField] private int variantIndex;
        [SerializeField] private bool authoredExternalSource;
        [SerializeField] private bool uv0Authored;
        [SerializeField] private bool normalsAuthored;
        [SerializeField] private bool textureMappedMaterials;
        [SerializeField] private string sourceAssetId = string.Empty;
        [SerializeField] private string assetVersion = string.Empty;
        [SerializeField] private string sourceFingerprint = string.Empty;
        [SerializeField] private string sourceGuid = string.Empty;
        [SerializeField] private string sourceDependencyHash = string.Empty;

        public int VariantIndex => variantIndex;
        public bool AuthoredExternalSource => authoredExternalSource;
        public bool Uv0Authored => uv0Authored;
        public bool NormalsAuthored => normalsAuthored;
        public bool TextureMappedMaterials => textureMappedMaterials;
        public string SourceAssetId => sourceAssetId;
        public string AssetVersion => assetVersion;
        public string SourceFingerprint => sourceFingerprint;
        public string SourceGuid => sourceGuid;
        public string SourceDependencyHash => sourceDependencyHash;

        public bool DeclaresProductionAuthoring =>
            authoredExternalSource && uv0Authored && normalsAuthored && textureMappedMaterials &&
            RivalProductionPolicy.IsSupportedAuthoredModelSource(sourceAssetId) &&
            !string.IsNullOrWhiteSpace(assetVersion) &&
            !string.IsNullOrWhiteSpace(sourceFingerprint) &&
            !string.IsNullOrWhiteSpace(sourceGuid) &&
            !string.IsNullOrWhiteSpace(sourceDependencyHash);

        public void Configure(
            int index,
            bool externalSource,
            bool authoredUv0,
            bool authoredNormals,
            bool mappedMaterials,
            string sourceId,
            string version,
            string fingerprint,
            string guid,
            string dependencyHash)
        {
            variantIndex = index;
            authoredExternalSource = externalSource;
            uv0Authored = authoredUv0;
            normalsAuthored = authoredNormals;
            textureMappedMaterials = mappedMaterials;
            sourceAssetId = sourceId ?? string.Empty;
            assetVersion = version ?? string.Empty;
            sourceFingerprint = fingerprint ?? string.Empty;
            sourceGuid = guid ?? string.Empty;
            sourceDependencyHash = dependencyHash ?? string.Empty;
        }
    }

    /// <summary>
    /// Fail-closed UART-004 contract for three externally authored rival production vehicles.
    /// Historical CarFactory bodies, Editor stripe/fin primitives and code-generated design-profile
    /// meshes can never satisfy production provenance on their own.
    /// </summary>
    public static class RivalProductionPolicy
    {
        public const int VariantCount = 3;

        private static readonly string[] ResourcePaths =
        {
            "Art/Vehicles/Rivals/Production/PF_Rival_01_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_02_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_03_Production"
        };

        private static readonly string[] AssetPaths =
        {
            "Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab",
            "Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab",
            "Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab"
        };

        private static readonly string[] AuthoredModelSuffixes =
        {
            ".fbx", ".obj", ".blend", ".glb", ".gltf"
        };

        public static readonly int[] MinimumTriangles = { 1800, 800, 350 };
        public static readonly int[] MaximumTriangles = { 16000, 8000, 4000 };

        public static string ResourcePath(int variantIndex)
        {
            ValidateVariantIndex(variantIndex);
            return ResourcePaths[variantIndex];
        }

        public static string AssetPath(int variantIndex)
        {
            ValidateVariantIndex(variantIndex);
            return AssetPaths[variantIndex];
        }

        public static bool IsSupportedAuthoredModelSource(string sourceAssetId)
        {
            if (string.IsNullOrWhiteSpace(sourceAssetId)) return false;

            var normalized = sourceAssetId.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal)) return false;
            if (normalized.IndexOf("../", StringComparison.Ordinal) >= 0) return false;
            if (normalized.IndexOf("/Generated/", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            foreach (var suffix in AuthoredModelSuffixes)
            {
                if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static bool MeetsProductionFloor(int lod, int triangleCount, bool allMeshesHaveUv0, bool allMeshesHaveAuthoredNormals, bool hasTextureMappedMaterial)
        {
            if (lod < 0 || lod >= MinimumTriangles.Length) return false;
            if (triangleCount < MinimumTriangles[lod] || triangleCount > MaximumTriangles[lod]) return false;
            return allMeshesHaveUv0 && allMeshesHaveAuthoredNormals && hasTextureMappedMaterial;
        }

        public static bool ValidateProductionPrefab(GameObject prefab, int variantIndex, out string reason)
        {
            if (prefab == null) { reason = "missing-prefab"; return false; }
            if (variantIndex < 0 || variantIndex >= VariantCount) { reason = "variant-index-out-of-range"; return false; }

            var metadata = prefab.GetComponent<RivalProductionAssetMetadata>();
            if (metadata == null) { reason = "missing-production-metadata"; return false; }
            if (metadata.VariantIndex != variantIndex)
            {
                reason = $"variant-metadata-mismatch-{metadata.VariantIndex}-{variantIndex}";
                return false;
            }
            if (!metadata.AuthoredExternalSource)
            {
                reason = "external-authored-source-required";
                return false;
            }
            if (!IsSupportedAuthoredModelSource(metadata.SourceAssetId))
            {
                reason = $"unsupported-authored-model-source:{metadata.SourceAssetId ?? "<null>"}";
                return false;
            }
            if (!metadata.DeclaresProductionAuthoring) { reason = "production-metadata-incomplete"; return false; }

            var group = prefab.GetComponent<LODGroup>();
            if (group == null) { reason = "missing-lod-group"; return false; }
            var lods = group.GetLODs();
            if (lods == null || lods.Length != 3)
            {
                reason = $"lod-count-{(lods == null ? 0 : lods.Length)}";
                return false;
            }

            for (var lod = 0; lod < lods.Length; lod++)
            {
                var renderers = lods[lod].renderers;
                if (renderers == null || renderers.Length == 0) { reason = $"lod{lod}-no-renderers"; return false; }
                var triangles = 0;
                var allUv0 = true;
                var allNormals = true;
                var hasTexture = false;

                foreach (var renderer in renderers)
                {
                    if (renderer == null) { reason = $"lod{lod}-null-renderer"; return false; }
                    var mesh = MeshFor(renderer);
                    if (mesh == null) { reason = $"lod{lod}-renderer-missing-mesh"; return false; }
                    triangles += TriangleCount(mesh);
                    allUv0 &= mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
                    allNormals &= mesh.HasVertexAttribute(VertexAttribute.Normal);
                    if (renderer.sharedMaterials != null)
                    {
                        foreach (var material in renderer.sharedMaterials)
                            if (HasAssignedTexture(material)) hasTexture = true;
                    }
                }

                if (!MeetsProductionFloor(lod, triangles, allUv0 && metadata.Uv0Authored, allNormals && metadata.NormalsAuthored, hasTexture && metadata.TextureMappedMaterials))
                {
                    reason = $"lod{lod}-production-quality triangles={triangles} uv0={allUv0}/{metadata.Uv0Authored} normals={allNormals}/{metadata.NormalsAuthored} texture={hasTexture}/{metadata.TextureMappedMaterials}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public static void ValidateContract()
        {
            if (ResourcePaths.Length != VariantCount || AssetPaths.Length != VariantCount)
                throw new InvalidOperationException("UART-004 must define exactly three production rival paths.");
            if (MinimumTriangles.Length != 3 || MaximumTriangles.Length != 3)
                throw new InvalidOperationException("UART-004 must define exactly three LOD quality bands.");
            if (AuthoredModelSuffixes.Length < 5)
                throw new InvalidOperationException("UART-004 must retain the supported external 3D source suffix contract.");
            for (var lod = 0; lod < 3; lod++)
            {
                if (MinimumTriangles[lod] <= 0 || MaximumTriangles[lod] <= MinimumTriangles[lod])
                    throw new InvalidOperationException($"UART-004 invalid production triangle band for LOD{lod}.");
            }
            if (!(MinimumTriangles[0] > MinimumTriangles[1] && MinimumTriangles[1] > MinimumTriangles[2]))
                throw new InvalidOperationException("UART-004 production triangle floors must decrease across LODs.");
        }

        public static Mesh MeshFor(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? null : filter.sharedMesh;
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

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++) count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }

        private static void ValidateVariantIndex(int variantIndex)
        {
            if (variantIndex < 0 || variantIndex >= VariantCount)
                throw new ArgumentOutOfRangeException(nameof(variantIndex));
        }
    }
}