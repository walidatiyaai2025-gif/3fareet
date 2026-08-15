using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Explicit authoring provenance attached to the real UART-003 production prefab.
    /// The generated Editor preview intentionally does not carry this component.
    /// Human/owner visual acceptance is still required separately by UPER-009.
    /// </summary>
    public sealed class HeroCarProductionAssetMetadata : MonoBehaviour
    {
        [SerializeField] private bool authoredExternalSource;
        [SerializeField] private bool uv0Authored;
        [SerializeField] private bool normalsAuthored;
        [SerializeField] private bool textureMappedMaterials;
        [SerializeField] private string sourceAssetId = string.Empty;

        public bool AuthoredExternalSource => authoredExternalSource;
        public bool Uv0Authored => uv0Authored;
        public bool NormalsAuthored => normalsAuthored;
        public bool TextureMappedMaterials => textureMappedMaterials;
        public string SourceAssetId => sourceAssetId;

        public bool DeclaresProductionAuthoring =>
            authoredExternalSource &&
            uv0Authored &&
            normalsAuthored &&
            textureMappedMaterials &&
            !string.IsNullOrWhiteSpace(sourceAssetId);
    }
}
