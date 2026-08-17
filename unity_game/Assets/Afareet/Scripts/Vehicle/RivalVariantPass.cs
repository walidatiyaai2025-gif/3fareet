using Afareet.World;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// UART-004 runtime installer. Player builds accept only authored production rival prefabs.
    /// Editor may additionally display a source-exact authored review candidate so the team can
    /// inspect tracked OBJ art even when the installed Unity OBJ importer flattens LOD objects.
    /// Historical primitive/material treatment remains the final Editor-only fallback.
    /// </summary>
    public sealed class RivalVariantPass : MonoBehaviour
    {
#if UNITY_EDITOR
        private static readonly Color[] Primary =
        {
            new(1f, .12f, .52f), new(1f, .52f, .04f), new(.08f, .9f, .42f)
        };
        private static readonly Color[] Secondary =
        {
            new(.18f, .72f, 1f), new(.6f, .06f, 1f), new(1f, .48f, .04f)
        };
        private static readonly string[] ReviewResourcePaths =
        {
            "Art/Vehicles/Rivals/Review/PF_Rival_01_AuthoredReview",
            "Art/Vehicles/Rivals/Review/PF_Rival_02_AuthoredReview",
            "Art/Vehicles/Rivals/Review/PF_Rival_03_AuthoredReview"
        };
#endif

        private readonly bool[] processed = new bool[RivalProductionPolicy.VariantCount];
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<RivalVariantPass>() != null) return;
            var host = new GameObject("AFAREET RIVAL PRODUCTION VISUAL PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<RivalVariantPass>();
        }

        private void Update()
        {
            if (built) return;
            var ready = 0;
            for (var i = 0; i < RivalProductionPolicy.VariantCount; i++)
            {
                if (processed[i]) { ready++; continue; }
                var rival = GameObject.Find($"RIVAL {i + 1}");
                if (rival == null) continue;
                InstallProductionOrFallback(rival.transform, i);
                processed[i] = true;
                ready++;
            }
            built = ready == RivalProductionPolicy.VariantCount;
            if (built) Debug.Log("AFAREET_UART004_RIVAL_PRODUCTION_PASS_COMPLETE variants=3");
        }

        private static void InstallProductionOrFallback(Transform rival, int index)
        {
            var primitiveRenderers = rival.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in primitiveRenderers) renderer.enabled = false;

            var path = RivalProductionPolicy.ResourcePath(index);
            var prefab = Resources.Load<GameObject>(path);
            var reason = "missing-production-prefab";
            if (prefab != null && RivalProductionPolicy.ValidateProductionPrefab(prefab, index, out reason))
            {
                var metadata = prefab.GetComponent<RivalProductionAssetMetadata>();
                var instance = Instantiate(prefab, rival, false);
                instance.name = "Rival Authored Production Visual";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                Debug.Log(
                    $"AFAREET_UART004_AUTHORED_RIVAL_ACTIVE variant={index + 1} source={metadata.SourceAssetId} " +
                    $"version={metadata.AssetVersion} fingerprint={metadata.SourceFingerprint} path={path} " +
                    $"hiddenBlockoutRenderers={primitiveRenderers.Length} physicsRootPreserved=true");
                return;
            }

#if UNITY_EDITOR
            var reviewPath = ReviewResourcePaths[index];
            var reviewPrefab = Resources.Load<GameObject>(reviewPath);
            if (TryValidateAuthoredReview(reviewPrefab, index, out var reviewMarker, out var reviewReason))
            {
                var instance = Instantiate(reviewPrefab, rival, false);
                instance.name = "Rival Authored Review Visual";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                Debug.Log(
                    $"AFAREET_UART004_AUTHORED_REVIEW_RIVAL_ACTIVE variant={index + 1} " +
                    $"source={reviewMarker.SourceAssetPath} signature={reviewMarker.SourceTriangleSignature} " +
                    $"path={reviewPath} hiddenBlockoutRenderers={primitiveRenderers.Length} " +
                    "physicsRootPreserved=true production=false p1Gate=false");
                return;
            }

            foreach (var renderer in primitiveRenderers)
                if (renderer != null) renderer.enabled = true;
            ApplyEditorBlockoutVariant(rival, index);
            Debug.LogWarning(
                $"AFAREET_UART004_EDITOR_BLOCKOUT_RIVAL_ACTIVE variant={index + 1} " +
                $"reason={reason} reviewReason={reviewReason} production=false");
#else
            Debug.LogError(
                $"AFAREET_UART004_PRODUCTION_RIVAL_REQUIRED variant={index + 1} reason={reason} " +
                "primitive-fallback-disabled physicsRootPreserved=true");
#endif
        }

#if UNITY_EDITOR
        private static bool TryValidateAuthoredReview(
            GameObject prefab,
            int index,
            out RivalAuthoredReviewCandidateMarker marker,
            out string reason)
        {
            marker = null;
            if (prefab == null)
            {
                reason = "missing-authored-review-prefab";
                return false;
            }

            marker = prefab.GetComponent<RivalAuthoredReviewCandidateMarker>();
            if (marker == null ||
                marker.Classification != RivalAuthoredReviewCandidateMarker.ExpectedClassification ||
                marker.VariantIndex != index ||
                marker.CanSatisfyProductionGate)
            {
                reason = "invalid-authored-review-marker";
                return false;
            }

            if (prefab.GetComponent<RivalProductionAssetMetadata>() != null)
            {
                reason = "authored-review-carries-production-metadata";
                return false;
            }

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
            {
                reason = "authored-review-missing-lod-group";
                return false;
            }

            var lods = group.GetLODs();
            if (lods == null || lods.Length != 3)
            {
                reason = $"authored-review-lod-count-{(lods == null ? 0 : lods.Length)}";
                return false;
            }

            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (lods[lod].renderers == null || lods[lod].renderers.Length == 0)
                {
                    reason = $"authored-review-lod{lod}-no-renderers";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static void ApplyEditorBlockoutVariant(Transform rival, int index)
        {
            if (rival.Find("Rival Variant Stripe") != null) return;
            var primary = Primary[index % Primary.Length];
            var secondary = Secondary[index % Secondary.Length];
            var bodyMaterial = RuntimeMaterials.Lit(primary, .55f, .82f);
            var spiritMaterial = RuntimeMaterials.Lit(secondary, .18f, .92f, 2.8f);
            foreach (var renderer in rival.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.name == "Body") renderer.material = bodyMaterial;
                else if (renderer.gameObject.name == "Spirit Hood") renderer.material = spiritMaterial;
            }
            var underglow = rival.Find("Spirit Underglow")?.GetComponent<Light>();
            if (underglow != null) underglow.color = secondary;

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Rival Variant Stripe";
            Destroy(stripe.GetComponent<Collider>());
            stripe.transform.SetParent(rival, false);
            stripe.transform.localPosition = new Vector3(0f, .88f, .22f);
            stripe.transform.localScale = new Vector3(.16f + index * .05f, .035f, 3.2f);
            stripe.GetComponent<Renderer>().material = spiritMaterial;

            var fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "Rival Variant Fin";
            Destroy(fin.GetComponent<Collider>());
            fin.transform.SetParent(rival, false);
            fin.transform.localPosition = new Vector3(index == 1 ? .68f : -.68f, 1.18f, -1.32f);
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, index == 2 ? -18f : 18f);
            fin.transform.localScale = new Vector3(.12f, .48f + index * .08f, .7f);
            fin.GetComponent<Renderer>().material = spiritMaterial;
        }
#endif
    }
}
