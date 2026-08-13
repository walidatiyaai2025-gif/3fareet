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
        private GUIStyle activeButton;
        private Texture2D cyan;
        private Texture2D purple;
        private float canvasScale;
        private float canvasWidth;
        private float canvasHeight;

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

        private void Update()
        {
            if (player == null) return;
            UpdateCanvasMetrics();
            MobileInput.Reset();

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                ApplyPointer(ToCanvas(touch.position));
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0)) ApplyPointer(ToCanvas(Input.mousePosition));
#endif
        }

        private void OnGUI()
        {
            if (player == null || race == null) return;
            EnsureStyles();
            UpdateCanvasMetrics();
            GUI.matrix = Matrix4x4.Scale(Vector3.one * canvasScale);
            var w = canvasWidth;
            var h = canvasHeight;

            GUI.Label(new Rect(24, 20, 210, 54), $"POS  {race.Position}/4", chip);
            GUI.Label(new Rect(w - 245, 20, 220, 54), $"{race.RaceTime:0.0} s", chip);
            GUI.Label(new Rect(w - 250, h - 176, 220, 70), $"{Mathf.Abs(player.SpeedKph):000}\nKM/H", title);
            GUI.Label(new Rect(24, h - 176, 240, 30), "SPIRIT NITRO", chip);
            GUI.DrawTexture(new Rect(24, h - 138, 220 * player.NitroEnergy, 13), purple);

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(w * .5f - 130, h * .25f, 260, 120), race.CountdownText, title);

            DrawTouchControls(w, h);
        }

        private void DrawTouchControls(float w, float h)
        {
            GUI.Box(LeftRect(h), "<", MobileInput.Steer < 0f ? activeButton : button);
            GUI.Box(RightRect(h), ">", MobileInput.Steer > 0f ? activeButton : button);
            GUI.Box(DriftRect(w, h), "DRIFT", MobileInput.Drift ? activeButton : button);
            GUI.Box(NitroRect(w, h), "NITRO", MobileInput.Nitro ? activeButton : button);
            GUI.Box(ThrottleRect(w, h), "GO", MobileInput.Throttle > 0f ? activeButton : button);
        }

        private void ApplyPointer(Vector2 point)
        {
            if (LeftRect(canvasHeight).Contains(point)) MobileInput.Steer = -1f;
            if (RightRect(canvasHeight).Contains(point)) MobileInput.Steer = 1f;
            if (DriftRect(canvasWidth, canvasHeight).Contains(point)) MobileInput.Drift = true;
            if (NitroRect(canvasWidth, canvasHeight).Contains(point)) MobileInput.Nitro = true;
            if (ThrottleRect(canvasWidth, canvasHeight).Contains(point)) MobileInput.Throttle = 1f;
        }

        private Vector2 ToCanvas(Vector2 screenPoint) =>
            new(screenPoint.x / canvasScale, (Screen.height - screenPoint.y) / canvasScale);

        private void UpdateCanvasMetrics()
        {
            canvasScale = Mathf.Max(.65f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            canvasWidth = Screen.width / canvasScale;
            canvasHeight = Screen.height / canvasScale;
        }

        private static Rect LeftRect(float h) => new(24, h - 96, 92, 76);
        private static Rect RightRect(float h) => new(128, h - 96, 92, 76);
        private static Rect DriftRect(float w, float h) => new(w - 420, h - 96, 112, 76);
        private static Rect NitroRect(float w, float h) => new(w - 296, h - 96, 112, 76);
        private static Rect ThrottleRect(float w, float h) => new(w - 172, h - 96, 148, 76);

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            chip = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = cyan } };
            button = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = purple } };
            activeButton = new GUIStyle(button) { normal = { textColor = Color.yellow, background = cyan } };
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
