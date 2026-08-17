using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Explicit authoring provenance attached to the real UART-003 production prefab.
    /// Generated, preview, refinement and blockout sources intentionally cannot satisfy
    /// this metadata contract. Human/owner visual acceptance is still required separately
    /// by UPER-009.
    /// </summary>
    public sealed class HeroCarProductionAssetMetadata : MonoBehaviour
    {
        [SerializeField] private bool authoredExternalSource;
        [SerializeField] private bool uv0Authored;
        [SerializeField] private bool normalsAuthored;
        [SerializeField] private bool textureMappedMaterials;
        [SerializeField] private string sourceAssetId = string.Empty;
        [SerializeField] private string assetVersion = string.Empty;
        [SerializeField] private string sourceGuid = string.Empty;
        [SerializeField] private string sourceDependencyHash = string.Empty;

        public bool AuthoredExternalSource => authoredExternalSource;
        public bool Uv0Authored => uv0Authored;
        public bool NormalsAuthored => normalsAuthored;
        public bool TextureMappedMaterials => textureMappedMaterials;
        public string SourceAssetId => sourceAssetId;
        public string AssetVersion => assetVersion;
        public string SourceGuid => sourceGuid;
        public string SourceDependencyHash => sourceDependencyHash;

        public bool DeclaresProductionAuthoring =>
            authoredExternalSource &&
            uv0Authored &&
            normalsAuthored &&
            textureMappedMaterials &&
            IsSupportedExternalModelSource(sourceAssetId) &&
            !IsNonProductionSourcePath(sourceAssetId) &&
            !string.IsNullOrWhiteSpace(assetVersion) &&
            !string.IsNullOrWhiteSpace(sourceGuid) &&
            !string.IsNullOrWhiteSpace(sourceDependencyHash);

        public static bool IsSupportedExternalModelSource(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return false;

            foreach (var extension in new[] { ".fbx", ".obj", ".blend", ".glb", ".gltf" })
            {
                if (assetPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsNonProductionSourcePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return true;

            var normalized = assetPath.Replace('\\', '/');
            foreach (var segment in new[]
                     {
                         "/Generated/",
                         "/Preview/",
                         "/Previews/",
                         "/Refinement/",
                         "/RefinementCandidates/",
                         "/Blockout/",
                         "/Blockouts/"
                     })
            {
                if (normalized.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        public void Configure(
            bool externalSource,
            bool authoredUv0,
            bool authoredNormals,
            bool mappedMaterials,
            string sourceId,
            string version,
            string guid,
            string dependencyHash)
        {
            authoredExternalSource = externalSource;
            uv0Authored = authoredUv0;
            normalsAuthored = authoredNormals;
            textureMappedMaterials = mappedMaterials;
            sourceAssetId = sourceId ?? string.Empty;
            assetVersion = version ?? string.Empty;
            sourceGuid = guid ?? string.Empty;
            sourceDependencyHash = dependencyHash ?? string.Empty;
        }
    }
}
