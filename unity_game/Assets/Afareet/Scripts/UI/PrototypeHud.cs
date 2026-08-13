using Afareet.Race;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ArcadeCarController player;
        private RaceDirector race;
        private GUIStyle title;
        private GUIStyle chip;
        private GUIStyle button;
        private Texture2D cyan;
        private Texture2D purple;

        public void Configure(ArcadeCarController playerCar, RaceDirector director)
        {
            player = playerCar;
            race = director;
        }

        private void Awake()
        {
            cyan = Solid(new Color(0f, .75f, 1f, .85f));
            purple = Solid(new Color(.45f, .08f, .72f, .82f));
        }

        private void OnGUI()
        {
            if (player == null || race == null) return;
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(.65f, scale));
            var w = Screen.width / Mathf.Max(.65f, scale);
            var h = Screen.height / Mathf.Max(.65f, scale);

            GUI.Label(new Rect(24, 20, 210, 54), $"POS  {race.Position}/4", chip);
            GUI.Label(new Rect(w - 245, 20, 220, 54), $"{race.RaceTime:0.0} s", chip);
            GUI.Label(new Rect(w - 250, h - 150, 220, 70), $"{Mathf.Abs(player.SpeedKph):000}\nKM/H", title);
            GUI.Label(new Rect(24, h - 150, 240, 30), "SPIRIT NITRO", chip);
            GUI.DrawTexture(new Rect(24, h - 108, 220 * player.NitroEnergy, 13), purple);

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(w * .5f - 130, h * .25f, 260, 120), race.CountdownText, title);

            DrawTouchControls(w, h);
        }

        private void DrawTouchControls(float w, float h)
        {
            MobileInput.Reset();
            if (GUI.RepeatButton(new Rect(24, h - 82, 82, 62), "◀", button)) MobileInput.Steer = -1f;
            if (GUI.RepeatButton(new Rect(116, h - 82, 82, 62), "▶", button)) MobileInput.Steer = 1f;
            if (GUI.RepeatButton(new Rect(w - 390, h - 82, 105, 62), "DRIFT", button)) MobileInput.Drift = true;
            if (GUI.RepeatButton(new Rect(w - 275, h - 82, 105, 62), "NITRO", button)) MobileInput.Nitro = true;
            if (GUI.RepeatButton(new Rect(w - 160, h - 82, 136, 62), "GO", button)) MobileInput.Throttle = 1f;
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            chip = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = cyan } };
            button = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = purple }, active = { textColor = Color.yellow, background = cyan } };
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
