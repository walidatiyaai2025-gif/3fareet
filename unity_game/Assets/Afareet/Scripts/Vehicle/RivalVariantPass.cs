using Afareet.World;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// UART-004 runtime installer. Player builds accept only authored production rival prefabs.
    /// Historical primitive/material treatment remains Editor-only for gameplay work.
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
            foreach (var renderer in primitiveRenderers)
                if (renderer != null) renderer.enabled = true;
            ApplyEditorBlockoutVariant(rival, index);
            Debug.LogWarning($"AFAREET_UART004_EDITOR_BLOCKOUT_RIVAL_ACTIVE variant={index + 1} reason={reason} production=false");
#else
            Debug.LogError(
                $"AFAREET_UART004_PRODUCTION_RIVAL_REQUIRED variant={index + 1} reason={reason} " +
                "primitive-fallback-disabled physicsRootPreserved=true");
#endif
        }

#if UNITY_EDITOR
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