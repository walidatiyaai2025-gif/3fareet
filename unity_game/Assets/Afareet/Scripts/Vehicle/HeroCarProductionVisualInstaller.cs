using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class HeroCarProductionVisualInstaller : MonoBehaviour
    {
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<HeroCarProductionVisualInstaller>() != null) return;
            var host = new GameObject("AFAREET HERO PRODUCTION VISUAL INSTALLER");
            DontDestroyOnLoad(host);
            host.AddComponent<HeroCarProductionVisualInstaller>();
        }

        private void Update()
        {
            if (complete) return;
            var hero = GameObject.Find("PLAYER HERO — AFAREET");
            if (hero == null || hero.GetComponent<ArcadeCarController>() == null) return;

            var proceduralRenderers = hero.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in proceduralRenderers)
                renderer.enabled = false;

            if (HeroCarProductionVisual.TryAttach(hero.transform))
            {
                Debug.Log($"AFAREET_HERO_PRODUCTION_VISUAL_ACTIVE hiddenProceduralRenderers={proceduralRenderers.Length}");
                complete = true;
                return;
            }

            if (Application.isEditor && HeroCarProductionVisual.TryAttachGeneratedPreview(hero.transform))
            {
                Debug.Log($"AFAREET_HERO_EDITOR_GENERATED_PREVIEW_ACTIVE hiddenProceduralRenderers={proceduralRenderers.Length} production=false");
                complete = true;
                return;
            }

            if (Application.isEditor)
            {
                foreach (var renderer in proceduralRenderers)
                    if (renderer != null) renderer.enabled = true;
                Debug.LogWarning("AFAREET_HERO_EDITOR_BLOCKOUT_FALLBACK_ACTIVE");
            }
            else
            {
                Debug.LogError("AFAREET_HERO_PRODUCTION_REQUIRED blockout-fallback-disabled");
            }

            complete = true;
        }
    }
}
