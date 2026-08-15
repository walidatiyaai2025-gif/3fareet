using UnityEngine;

namespace Afareet.World
{
    public sealed class MobileVisualDensityPass : MonoBehaviour
    {
        private bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            var host = new GameObject("AFAREET MOBILE VISUAL DENSITY PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<MobileVisualDensityPass>();
        }

        private void Update()
        {
            if (applied) return;
            var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var clusters = 0;
            foreach (var t in transforms)
            {
                if (t.name != "Cairo Skyline Cluster" || t.childCount < 10) continue;
                DisablePair(t, 0);
                DisablePair(t, t.childCount - 2);
                clusters++;
            }

            if (clusters >= 3) applied = true;
        }

        private static void DisablePair(Transform root, int index)
        {
            if (index < 0 || index + 1 >= root.childCount) return;
            root.GetChild(index).gameObject.SetActive(false);
            root.GetChild(index + 1).gameObject.SetActive(false);
        }
    }
}
