using Afareet.World;
using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class RivalVariantPass : MonoBehaviour
    {
        private static readonly Color[] Primary =
        {
            new(1f, .12f, .52f),
            new(1f, .52f, .04f),
            new(.08f, .9f, .42f)
        };

        private static readonly Color[] Secondary =
        {
            new(.18f, .72f, 1f),
            new(.6f, .06f, 1f),
            new(1f, .48f, .04f)
        };

        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<RivalVariantPass>() != null) return;
            var host = new GameObject("AFAREET RIVAL VARIANT PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<RivalVariantPass>();
        }

        private void Update()
        {
            if (built) return;
            var ready = 0;
            for (var i = 0; i < 3; i++)
            {
                var rival = GameObject.Find($"RIVAL {i + 1}");
                if (rival == null) continue;
                ApplyVariant(rival.transform, i);
                ready++;
            }
            built = ready == 3;
        }

        private static void ApplyVariant(Transform rival, int index)
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
    }
}
