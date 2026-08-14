using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class HeroCarProductionVisual : MonoBehaviour
    {
        public static bool TryAttach(Transform vehicleRoot)
        {
            if (vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"AFAREET_HERO_PRODUCTION_VISUAL_MISSING path={HeroCarLodPolicy.ResourcePath}; using procedural fallback.");
                return false;
            }

            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = "Hero Production Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (instance.GetComponent<LODGroup>() == null)
            {
                Object.Destroy(instance);
                Debug.LogError("AFAREET_HERO_PRODUCTION_VISUAL_INVALID missing LODGroup; using procedural fallback.");
                return false;
            }

            return true;
        }
    }
}
