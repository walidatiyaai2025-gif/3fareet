using Afareet.Race;
using UnityEngine;

namespace Afareet.UI
{
    public sealed class RaceStartPulse : MonoBehaviour
    {
        private RaceDirector race;
        private float flash;
        private string lastText = string.Empty;
        private Texture2D white;
        private GUIStyle label;

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
                flash = text == "GO!" ? 1f : .45f;
                lastText = text;
            }
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.8f);
        }

        private void OnGUI()
        {
            if (race == null || flash <= 0f) return;
            label ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            var a = Mathf.Clamp01(flash * .32f);
            GUI.color = new Color(.5f, .03f, 1f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(0, Screen.height * .18f, Screen.width, 90), race.CountdownText, label);
        }
    }
}
