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
        private GUIStyle speedUnit;
        private Texture2D cyan;
        private Texture2D purple;
        private Texture2D gold;
        private Texture2D panel;
        private Texture2D darkOverlay;
        private float canvasScale;
        private float canvasWidth;
        private float canvasHeight;
        private float safeLeft;
        private float safeRight;
        private float safeTop;
        private float safeBottom;
        private Vector3 motionBaseline;
        private bool hasMotionBaseline;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;
        private bool brakeInput;
        private bool brakeReverseInput;

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
            if (player == null || race == null) return;
            UpdateCanvasMetrics();
            ResetInput();
            if (!race.IsStarted)
            {
                player.SetPlayerInput(0f, 0f, false, false, true);
                ReadStartInput();
                return;
            }

            if (race.IsPaused || race.Phase == RaceRoundPhase.Results)
            {
                player.SetPlayerInput(0f, 0f, false, false, true);
                return;
            }

            ApplyMotionControls();
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;

                var point = ToCanvas(touch.position);
                if (touch.phase == TouchPhase.Began && RecoverRect().Contains(point))
                {
                    RecoverPlayer();
                    continue;
                }

                ApplyPointer(point);
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            var mousePoint = ToCanvas(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && RecoverRect().Contains(mousePoint))
                RecoverPlayer();
            else if (Input.GetMouseButton(0))
                ApplyPointer(mousePoint);
#endif
            if (brakeReverseInput)
                MobileDriveInputPolicy.ResolveBrakeReverse(player.SpeedKph, out throttleInput, out brakeInput);

            player.SetPlayerInput(throttleInput, steerInput, driftInput, nitroInput, brakeInput);
        }

        private void ReadStartInput()
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && StartRect().Contains(ToCanvas(touch.position))) StartRace();
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && StartRect().Contains(ToCanvas(Input.mousePosition))) StartRace();
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
            steerInput = MobileDriveInputPolicy.ResolveTiltSteer(steeringTilt);
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
            var left = safeLeft;
            var right = w - safeRight;
            var top = safeTop;
            var bottom = h - safeBottom;
            var centerX = left + (right - left) * .5f;
            var centerY = top + (bottom - top) * .5f;

            GUI.Label(new Rect(centerX - 190, top + 17, 380, 24), "CAIRO NIGHT // SPIRIT CIRCUIT", micro);
            GUI.DrawTexture(new Rect(centerX - 116, top + 45, 232, 3), purple);
            GUI.DrawTexture(new Rect(centerX - 42, top + 45, 84, 3), gold);

            DrawPanel(new Rect(left + 20, top + 16, 218, 62));
            GUI.Label(new Rect(left + 30, top + 20, 198, 54), $"POS  {race.Position}/4", chip);
            DrawPanel(new Rect(right - 242, top + 16, 222, 62));
            GUI.Label(new Rect(right - 232, top + 20, 202, 54), $"{race.RaceTime:0.0} s", chip);

            DrawPanel(new Rect(right - 258, bottom - 202, 238, 104));
            GUI.Label(new Rect(right - 248, bottom - 196, 218, 58), $"{Mathf.Abs(player.SpeedKph):000}", title);
            GUI.Label(new Rect(right - 248, bottom - 144, 218, 30), "KM/H", speedUnit);
            GUI.DrawTexture(new Rect(right - 228, bottom - 110, 178, 3), player.NitroActive ? cyan : gold);

            DrawPanel(new Rect(left + 20, bottom - 202, 272, 76));
            GUI.Label(new Rect(left + 28, bottom - 196, 164, 26), "SPIRIT NITRO", micro);
            GUI.Label(new Rect(left + 194, bottom - 196, 84, 26), $"{Mathf.RoundToInt(player.NitroEnergy * 100f)}%", micro);
            GUI.DrawTexture(new Rect(left + 32, bottom - 156, 244, 18), panel);
            GUI.DrawTexture(new Rect(left + 32, bottom - 156, 244 * player.NitroEnergy, 18), player.NitroEnergy > .78f ? gold : purple);
            GUI.DrawTexture(new Rect(left + 32, bottom - 156, 4, 18), cyan);

            if (!race.IsStarted)
            {
                GUI.DrawTexture(new Rect(0f, 0f, w, h), darkOverlay);
                GUI.DrawTexture(new Rect(centerX - 190, centerY - 174, 380, 4), purple);
                GUI.DrawTexture(new Rect(centerX - 68, centerY - 174, 136, 4), gold);
                GUI.Label(new Rect(centerX - 260, centerY - 148, 520, 54), "3FAREET // CAIRO NIGHT RUN", title);
                GUI.Label(new Rect(centerX - 210, centerY - 102, 420, 28), "THE KING ENTERS THE SPIRIT CIRCUIT", micro);
                GUI.Box(StartRect(), "START RACE", activeButton);
                GUI.Label(new Rect(centerX - 280, centerY + 66, 560, 38), "HOLD PHONE COMFORTABLY, THEN START", micro);
                return;
            }

            if (!string.IsNullOrEmpty(race.CountdownText))
                GUI.Label(new Rect(centerX - 130, top + (bottom - top) * .25f, 260, 120), race.CountdownText, title);

            if (race.IsPaused || race.Phase == RaceRoundPhase.Results) return;
            DrawTouchControls();
        }

        private void DrawPanel(Rect rect)
        {
            GUI.DrawTexture(rect, panel);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), purple);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), gold);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 3, rect.y + 7, 3, rect.height - 14), cyan);
        }

        private void DrawTouchControls()
        {
            GUI.Box(LeftRect(), "<", steerInput < 0f ? activeButton : button);
            GUI.Box(RightRect(), ">", steerInput > 0f ? activeButton : button);
            GUI.Box(BrakeReverseRect(), "BRAKE / REV", brakeReverseInput ? activeButton : button);
            GUI.Box(RecoverRect(), "RECOVER", button);
            GUI.Box(DriftRect(), "DRIFT", driftInput ? activeButton : button);
            GUI.Box(NitroRect(), "SPIRIT", nitroInput ? activeButton : button);
            GUI.Box(ThrottleRect(), "GO", throttleInput > 0f ? activeButton : button);
        }

        private void ApplyPointer(Vector2 point)
        {
            if (LeftRect().Contains(point)) steerInput = MobileDriveInputPolicy.ResolveTouchSteer(-1f);
            if (RightRect().Contains(point)) steerInput = MobileDriveInputPolicy.ResolveTouchSteer(1f);
            if (BrakeReverseRect().Contains(point)) brakeReverseInput = true;
            if (DriftRect().Contains(point)) driftInput = true;
            if (NitroRect().Contains(point)) nitroInput = true;
            if (ThrottleRect().Contains(point)) throttleInput = 1f;
        }

        private void RecoverPlayer()
        {
            ResetInput();
            player.SetPlayerInput(0f, 0f, false, false, false);
            player.ResetToSpawn();
        }

        private void ResetInput()
        {
            steerInput = 0f;
            throttleInput = 0f;
            driftInput = false;
            nitroInput = false;
            brakeInput = false;
            brakeReverseInput = false;
        }

        private Vector2 ToCanvas(Vector2 screenPoint) => new(screenPoint.x / canvasScale, (Screen.height - screenPoint.y) / canvasScale);

        private void UpdateCanvasMetrics()
        {
            canvasScale = Mathf.Max(.65f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            canvasWidth = Screen.width / canvasScale;
            canvasHeight = Screen.height / canvasScale;

            var safe = Screen.safeArea;
            safeLeft = safe.xMin / canvasScale;
            safeRight = (Screen.width - safe.xMax) / canvasScale;
            safeTop = (Screen.height - safe.yMax) / canvasScale;
            safeBottom = safe.yMin / canvasScale;
        }

        private Rect LeftRect() => new(safeLeft + 24, canvasHeight - safeBottom - 96, 92, 76);
        private Rect RightRect() => new(safeLeft + 128, canvasHeight - safeBottom - 96, 92, 76);
        private Rect BrakeReverseRect() => new(safeLeft + 232, canvasHeight - safeBottom - 96, 132, 76);
        private Rect RecoverRect() => new(safeLeft + 376, canvasHeight - safeBottom - 96, 132, 76);
        private Rect DriftRect() => new(canvasWidth - safeRight - 420, canvasHeight - safeBottom - 96, 112, 76);
        private Rect NitroRect() => new(canvasWidth - safeRight - 296, canvasHeight - safeBottom - 96, 112, 76);
        private Rect ThrottleRect() => new(canvasWidth - safeRight - 172, canvasHeight - safeBottom - 96, 148, 76);

        private Rect StartRect()
        {
            var usableWidth = canvasWidth - safeLeft - safeRight;
            var usableHeight = canvasHeight - safeTop - safeBottom;
            return new Rect(safeLeft + usableWidth * .5f - 170, safeTop + usableHeight * .5f - 54, 340, 108);
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            chip = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            micro = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.86f, .92f, 1f) } };
            speedUnit = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.08f, .82f, 1f) } };
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
