using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Explicit authoring provenance attached to the real UART-003 production prefab.
    /// The generated/refinement/review Editor paths intentionally cannot satisfy production
    /// source authority. Human/owner visual acceptance is still required separately by UPER-009.
    /// </summary>
    public sealed class HeroCarProductionAssetMetadata : MonoBehaviour
    {
        private static readonly string[] SupportedExternalModelSuffixes =
        {
            ".fbx", ".obj", ".blend", ".glb", ".gltf"
        };

        private static readonly string[] NonProductionSourceMarkers =
        {
            "/Generated/",
            "/Placeholder/",
            "/LegacyProcedural/",
            "/Preview/",
            "/Refinement/",
            "/RefinementCandidates/",
            "/Blockout/",
            "/Review/",
            "/ReviewPackaging/"
        };

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
            !string.IsNullOrWhiteSpace(assetVersion) &&
            !string.IsNullOrWhiteSpace(sourceGuid) &&
            !string.IsNullOrWhiteSpace(sourceDependencyHash);

        public static bool IsSupportedExternalModelSource(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return false;

            var normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal)) return false;
            if (normalized.IndexOf("../", StringComparison.Ordinal) >= 0) return false;

            var suffixSupported = false;
            foreach (var extension in SupportedExternalModelSuffixes)
            {
                if (!normalized.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
                suffixSupported = true;
                break;
            }
            if (!suffixSupported) return false;

            foreach (var marker in NonProductionSourceMarkers)
            {
                if (normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            return true;
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
