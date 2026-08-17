using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Marks a UART-004 authored-source review prefab that is safe for local Editor visual
    /// inspection but cannot satisfy the production gate. Geometry is source-derived from the
    /// tracked rival OBJ and only repackaged into one Unity-importable file per authored LOD.
    /// </summary>
    public sealed class RivalAuthoredReviewCandidateMarker : MonoBehaviour
    {
        public const string ExpectedClassification = "AUTHORED_REVIEW_CANDIDATE";

        [SerializeField] private string classification = ExpectedClassification;
        [SerializeField] private int variantIndex;
        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string sourceGuid = string.Empty;
        [SerializeField] private string sourceDependencyHash = string.Empty;
        [SerializeField] private string sourceTriangleSignature = string.Empty;

        public string Classification => classification;
        public int VariantIndex => variantIndex;
        public string SourceAssetPath => sourceAssetPath;
        public string SourceGuid => sourceGuid;
        public string SourceDependencyHash => sourceDependencyHash;
        public string SourceTriangleSignature => sourceTriangleSignature;
        public bool CanSatisfyProductionGate => false;

        public void Configure(
            int variant,
            string sourcePath,
            string guid,
            string dependencyHash,
            string triangleSignature)
        {
            classification = ExpectedClassification;
            variantIndex = variant;
            sourceAssetPath = sourcePath ?? string.Empty;
            sourceGuid = guid ?? string.Empty;
            sourceDependencyHash = dependencyHash ?? string.Empty;
            sourceTriangleSignature = triangleSignature ?? string.Empty;
        }
    }
}
