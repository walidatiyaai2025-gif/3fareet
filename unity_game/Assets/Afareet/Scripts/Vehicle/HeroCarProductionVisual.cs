using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class HeroCarProductionVisual : MonoBehaviour
    {
        public static bool TryAttach(Transform vehicleRoot)
        {
            if (vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.ProductionResourcePath);
            var productionAssetActive = prefab != null;

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"AFAREET_HERO_PRODUCTION_VISUAL_MISSING path={HeroCarLodPolicy.ProductionResourcePath}; " +
                    $"tryingDevelopmentFallback={HeroCarLodPolicy.DevelopmentFallbackResourcePath}");
                prefab = Resources.Load<GameObject>(HeroCarLodPolicy.DevelopmentFallbackResourcePath);
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    "AFAREET_HERO_VISUAL_FALLBACK_MISSING; retaining procedural vehicle visual. " +
                    "UART-003 remains blocked for production-art acceptance.");
                return false;
            }

            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = productionAssetActive ? "Hero Production Visual" : "Hero Development Fallback Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (instance.GetComponent<LODGroup>() == null)
            {
                Object.Destroy(instance);
                Debug.LogError(
                    productionAssetActive
                        ? "AFAREET_HERO_PRODUCTION_VISUAL_INVALID missing LODGroup; refusing production Hero."
                        : "AFAREET_HERO_DEVELOPMENT_FALLBACK_INVALID missing LODGroup; retaining procedural fallback.");
                return false;
            }

            if (productionAssetActive)
            {
                Debug.Log($"AFAREET_HERO_AUTHORED_PRODUCTION_VISUAL_ACTIVE path={HeroCarLodPolicy.ProductionResourcePath}");
            }
            else
            {
                Debug.LogWarning(
                    $"AFAREET_HERO_DEVELOPMENT_FALLBACK_ACTIVE path={HeroCarLodPolicy.DevelopmentFallbackResourcePath}; " +
                    "not eligible for UPER-009 production-art acceptance.");
            }

            return true;
        }
    }
}
