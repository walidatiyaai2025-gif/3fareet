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
        private GUIStyle micro;
        private Texture2D cyan;
        private Texture2D purple;
        private Texture2D gold;
        private Texture2D panel;
        private Texture2D darkOverlay;
        private float canvasScale;
        private float canvasWidth;
        private float canvasHeight;
        private Vector3 motionBaseline;
        private bool hasMotionBaseline;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;
        private bool brakeInput;

        public void Configure(ArcadeCarController playerCar, RaceDirector director)
        {
            player = playerCar;
            race = director;
        }

        private void Awake()
        {
            cyan = Solid(new Color(.02f, .78f, 1f, .92f));
            purple = Solid(new Color(.48f, .035f, .9f, .94f));
            gold = Solid(new Color(1f, .55f, .05f, .96f));
            panel = Solid(new Color(.018f, .012f, .045f, .82f));
            darkOverlay = Solid(new Color(0f, 0f, .02f, .72f));
        }

        private void Update()
        {
            if (player == null) return;
            UpdateCanvasMetrics();
            ResetInput();
            if (!race.IsStarted)
            {
                player.SetPlayerInput(0f, 0f, false, false, true);
                ReadStartInput();
                return;
            }
            ApplyMotionControls();
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                ApplyPointer(ToCanvas(touch.position));
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0)) ApplyPointer(ToCanvas(Input.mousePosition));
#endif
            player.SetPlayerInput(throttleInput, steerInput, driftInput, nitroInput, brakeInput);
        }

        private void ReadStartInput()
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && StartRect(canvasWidth, canvasHeight).Contains(ToCanvas(touch.position))) StartRace();
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && StartRect(canvasWidth, canvasHeight).Contains(ToCanvas(Input.mousePosition))) StartRace();
#endif
        }

        private void StartRace()
        {
            motionBaseline = Input.acceleration;
            hasMotionBaseline = true;
            ResetInput();
            race.StartRace();
        }

        private void ApplyMotionControls()
        {
            if (!Application.isMobilePlatform || !hasMotionBaseline) return;
            var acceleration = Input.acceleration - motionBaseline;
            var landscapeLeft = Screen.orientation != ScreenOrientation.LandscapeRight;
            var steeringTilt = landscapeLeft ? -acceleration.y : acceleration.y;
            var forwardTilt = landscapeLeft ? -acceleration.x : acceleration.x;
            const float deadZone = .08f;
            steerInput = Mathf.Abs(steeringTilt) <= deadZone ? 0f : Mathf.Clamp(steeringTilt * 2.4f, -1f, 1f);
            throttleInput = 0f;
            nitroInput = forwardTilt > .32f;
            brakeInput = forwardTilt < -.32f;
        }

        private void OnGUI()
        {
            if (player == null || race == null) return;
            EnsureStyles();
            UpdateCanvasMetrics();
            GUI.matrix = Matrix4x4.Scale(Vector3.one * canvasScale);
            var w = canvasWidth;
            var h = canvasHeight;

            DrawPanel(new Rect(20, 16, 218, 62));
            GUI.Label(new Rect(30, 20, 198, 54), $"POS  {race.Position}/4", chip);
            DrawPanel(new Rect(w - 242, 16, 222, 62));
            GUI.Label(new Rect(w - 232, 20, 202, 54), $"{race.RaceTime:0.0} s", chip);

            DrawPanel(new Rect(w - 258, h - 190, 238, 92));
            GUI.Label(new Rect(w - 248, h - 182, 218, 72), $"{Mathf.Abs(player.SpeedKph):000}\nKM/H", title);

            DrawPanel(new Rect(20, h - 190, 260, 64));
            GUI.Label(new Rect(28, h - 184, 244, 26), "SPIRIT NITRO", micro);
            GUI.DrawTexture(new Rect(32, h - 148, 232, 16), panel);
            GUI.DrawTexture(new Rect(32, h - 148, 232 * player.NitroEnergy, 16), player.NitroEnergy > .78f ? gold : purple);
            GUI.DrawTexture(new Rect(32, h - 148, 4, 16), cyan);

            if (!race.IsStarted)
            {
                GUI.DrawTexture(new Rect(0f, 0f, w, h), darkOverlay);
                GUI.Label(new Rect(w * .5f - 240, h * .5f - 148, 480, 54), "3FAREET // CAIRO NIGHT RUN", title);
                GUI.Box(StartRect(w, h), "START RACE", activeButton);
                GUI.Label(new Rect(w * .5f - 280, h * .5f + 66, 560, 38), "HOLD PHONE COMFORTABLY, THEN START", micro);
                return;
            }

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(w * .5f - 130, h * .25f, 260, 120), race.CountdownText, title);

            DrawTouchControls(w, h);
        }

        private void DrawPanel(Rect rect)
        {
            GUI.DrawTexture(rect, panel);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), purple);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), gold);
        }

        private void DrawTouchControls(float w, float h)
        {
            GUI.Box(LeftRect(h), "<", steerInput < 0f ? activeButton : button);
            GUI.Box(RightRect(h), ">", steerInput > 0f ? activeButton : button);
            GUI.Box(DriftRect(w, h), "DRIFT", driftInput ? activeButton : button);
            GUI.Box(NitroRect(w, h), "SPIRIT", nitroInput ? activeButton : button);
            GUI.Box(ThrottleRect(w, h), "GO", throttleInput > 0f ? activeButton : button);
        }

        private void ApplyPointer(Vector2 point)
        {
            if (LeftRect(canvasHeight).Contains(point)) steerInput = -1f;
            if (RightRect(canvasHeight).Contains(point)) steerInput = 1f;
            if (DriftRect(canvasWidth, canvasHeight).Contains(point)) driftInput = true;
            if (NitroRect(canvasWidth, canvasHeight).Contains(point)) nitroInput = true;
            if (ThrottleRect(canvasWidth, canvasHeight).Contains(point)) throttleInput = 1f;
        }

        private void ResetInput() { steerInput = 0f; throttleInput = 0f; driftInput = false; nitroInput = false; brakeInput = false; }
        private Vector2 ToCanvas(Vector2 screenPoint) => new(screenPoint.x / canvasScale, (Screen.height - screenPoint.y) / canvasScale);
        private void UpdateCanvasMetrics() { canvasScale = Mathf.Max(.65f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f)); canvasWidth = Screen.width / canvasScale; canvasHeight = Screen.height / canvasScale; }
        private static Rect LeftRect(float h) => new(24, h - 96, 92, 76);
        private static Rect RightRect(float h) => new(128, h - 96, 92, 76);
        private static Rect DriftRect(float w, float h) => new(w - 420, h - 96, 112, 76);
        private static Rect NitroRect(float w, float h) => new(w - 296, h - 96, 112, 76);
        private static Rect ThrottleRect(float w, float h) => new(w - 172, h - 96, 148, 76);
        private static Rect StartRect(float w, float h) => new(w * .5f - 170, h * .5f - 54, 340, 108);

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            chip = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            micro = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.86f, .92f, 1f) } };
            button = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = purple } };
            activeButton = new GUIStyle(button) { normal = { textColor = Color.black, background = gold } };
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
