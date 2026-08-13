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
            cyan = Solid(new Color(0f, .75f, 1f, .85f));
            purple = Solid(new Color(.45f, .08f, .72f, .82f));
            darkOverlay = Solid(new Color(0f, 0f, 0f, .62f));
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
                if (touch.phase == TouchPhase.Began && StartRect(canvasWidth, canvasHeight).Contains(ToCanvas(touch.position)))
                    StartRace();
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && StartRect(canvasWidth, canvasHeight).Contains(ToCanvas(Input.mousePosition)))
                StartRace();
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
            steerInput = Mathf.Abs(steeringTilt) <= deadZone
                ? 0f
                : Mathf.Clamp(steeringTilt * 2.4f, -1f, 1f);

            // Pitching the phone forward accelerates with nitro; pulling it
            // back applies the brake. Neutral leaves the GO button in control.
            // Acceleration is touch-only so sensor noise can never launch the
            // car. Phone pitch controls nitro/brake as a separate gesture.
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

            GUI.Label(new Rect(24, 20, 210, 54), $"POS  {race.Position}/4", chip);
            GUI.Label(new Rect(w - 245, 20, 220, 54), $"{race.RaceTime:0.0} s", chip);
            GUI.Label(new Rect(w - 250, h - 176, 220, 70), $"{Mathf.Abs(player.SpeedKph):000}\nKM/H", title);
            GUI.Label(new Rect(24, h - 176, 240, 30), "SPIRIT NITRO", chip);
            GUI.DrawTexture(new Rect(24, h - 138, 220 * player.NitroEnergy, 13), purple);

            if (!race.IsStarted)
            {
                GUI.DrawTexture(new Rect(0f, 0f, w, h), darkOverlay);
                GUI.Box(StartRect(w, h), "START RACE", activeButton);
                GUI.Label(new Rect(w * .5f - 260, h * .5f + 58, 520, 44), "HOLD PHONE COMFORTABLY, THEN START", chip);
                return;
            }

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(w * .5f - 130, h * .25f, 260, 120), race.CountdownText, title);

            GUI.Label(new Rect(w * .5f - 210, 18, 420, 42),
                $"STEER {steerInput:+0.00;-0.00;0.00}   GAS {throttleInput:0.00}   BRAKE {(brakeInput ? "ON" : "OFF")}", chip);

            DrawTouchControls(w, h);
        }

        private void DrawTouchControls(float w, float h)
        {
            GUI.Box(LeftRect(h), "<", steerInput < 0f ? activeButton : button);
            GUI.Box(RightRect(h), ">", steerInput > 0f ? activeButton : button);
            GUI.Box(DriftRect(w, h), "DRIFT", driftInput ? activeButton : button);
            GUI.Box(NitroRect(w, h), "NITRO", nitroInput ? activeButton : button);
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

        private void ResetInput()
        {
            steerInput = 0f;
            throttleInput = 0f;
            driftInput = false;
            nitroInput = false;
            brakeInput = false;
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
        private static Rect StartRect(float w, float h) => new(w * .5f - 170, h * .5f - 54, 340, 108);

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
