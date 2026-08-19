using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Explicit authoring provenance attached to the real UART-003 production prefab.
    /// Generated/refinement/review or non-Hero vehicle roles intentionally cannot satisfy
    /// production source authority. Human/owner visual acceptance remains a separate UPER-009 gate.
    /// </summary>
    public sealed class HeroCarProductionAssetMetadata : MonoBehaviour
    {
        private const string VehiclePathMarker = "/Vehicles/";

        private static readonly string[] SupportedExternalModelSuffixes =
        {
            ".fbx", ".obj", ".blend", ".glb", ".gltf"
        };

        // This remains the central fail-closed marker set used by every Hero production entry point.
        // `/Rivals/` is production-valid for UART-004, but it is intentionally non-production as a
        // UART-003 Hero source role and therefore belongs in this Hero-specific rejection set.
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
            "/ReviewPackaging/",
            "/Rivals/"
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
            if (normalized.IndexOf(VehiclePathMarker, StringComparison.OrdinalIgnoreCase) < 0) return false;

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
