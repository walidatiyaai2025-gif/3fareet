using Afareet.Race;
using UnityEngine;

namespace Afareet.UI
{
    public sealed class RaceStartPulse : MonoBehaviour
    {
        private RaceDirector race;
        private float flash;
        private bool goFlash;
        private string lastText = string.Empty;
        private Texture2D white;
        private GUIStyle label;
        private GUIStyle subLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("RACE START PULSE");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<RaceStartPulse>();
        }

        private void Awake()
        {
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        private void Update()
        {
            if (race == null) race = FindFirstObjectByType<RaceDirector>();
            if (race == null) return;

            var text = race.CountdownText;
            if (!string.IsNullOrEmpty(text) && text != lastText)
            {
                goFlash = text == "GO!";
                flash = goFlash ? 1f : .48f;
                lastText = text;
            }
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * (goFlash ? 2.2f : 3.1f));
        }

        private void OnGUI()
        {
            if (race == null || flash <= 0f) return;
            label ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 72,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subLabel ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.9f, .94f, 1f) }
            };

            var a = Mathf.Clamp01(flash * (goFlash ? .42f : .3f));
            var pulseColor = goFlash ? new Color(1f, .48f, .04f, a) : new Color(.5f, .03f, 1f, a);
            GUI.color = pulseColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);

            var barWidth = Screen.width * (goFlash ? .42f : .28f);
            var centerX = Screen.width * .5f;
            var centerY = Screen.height * .18f;
            GUI.color = goFlash ? new Color(.03f, .82f, 1f, Mathf.Clamp01(flash)) : new Color(.7f, .08f, 1f, Mathf.Clamp01(flash));
            GUI.DrawTexture(new Rect(centerX - barWidth * .5f, centerY + 84f, barWidth, 5f), white);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(race.CountdownText))
            {
                GUI.Label(new Rect(0, centerY, Screen.width, 90), race.CountdownText, label);
                GUI.Label(new Rect(0, centerY + 92f, Screen.width, 32), goFlash ? "SPIRIT RELEASE" : "LOCK IN // HOLD THE LINE", subLabel);
            }
        }
    }
}
