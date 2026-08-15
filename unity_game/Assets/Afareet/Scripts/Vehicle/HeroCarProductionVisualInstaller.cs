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

            if (hero.GetComponentInChildren<HeroCarProductionVisual>(true) != null)
            {
                complete = true;
                return;
            }

            var proceduralRenderers = hero.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in proceduralRenderers)
                renderer.enabled = false;

            if (!HeroCarProductionVisual.TryAttach(hero.transform))
            {
                foreach (var renderer in proceduralRenderers)
                    if (renderer != null) renderer.enabled = true;
                return;
            }

            Debug.Log($"AFAREET_HERO_PRODUCTION_VISUAL_ACTIVE hiddenProceduralRenderers={proceduralRenderers.Length}");
            complete = true;
        }
    }
}
