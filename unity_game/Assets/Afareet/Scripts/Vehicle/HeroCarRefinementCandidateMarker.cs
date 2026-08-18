using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Marks an externally imported Hero mesh that is useful for visual refinement only.
    /// It must never be interpreted as the authoritative UART-003 production artifact.
    /// </summary>
    public sealed class HeroCarRefinementCandidateMarker : MonoBehaviour
    {
        public const string ExpectedClassification = "REFINEMENT_CANDIDATE";

        [SerializeField] private string classification = ExpectedClassification;
        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string sourceSha256 = string.Empty;
        [SerializeField] private bool mobileBudgetReady;

        public string Classification => classification;
        public string SourceAssetPath => sourceAssetPath;
        public string SourceSha256 => sourceSha256;
        public bool MobileBudgetReady => mobileBudgetReady;
        public bool CanSatisfyProductionGate => false;

        public void Configure(string sourcePath, string sha256, bool budgetReady)
        {
            classification = ExpectedClassification;
            sourceAssetPath = sourcePath ?? string.Empty;
            sourceSha256 = sha256 ?? string.Empty;
            mobileBudgetReady = budgetReady;
        }
    }
}
