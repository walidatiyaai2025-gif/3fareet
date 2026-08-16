using System;
using Afareet.CareerRuntime;
using Afareet.Race;
using Afareet.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Afareet.UI
{
    public sealed class ProductionRaceHud : MonoBehaviour
    {
        private const string HostName = "AFAREET PRODUCTION RACE HUD";

        private ArcadeCarController player;
        private RaceDirector race;
        private CareerGameSession career;
        private RectTransform safeRoot;
        private Text positionText;
        private Text timeText;
        private Text speedText;
        private Text spiritText;
        private Text careerText;
        private Image spiritFill;
        private Rect lastSafeArea;

        public bool HasRuntimeBinding => player != null && race != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            EnsureInstalled();
        }

        public static ProductionRaceHud EnsureInstalled(Transform parent = null)
        {
            var existing = FindFirstObjectByType<ProductionRaceHud>();
            if (existing != null)
            {
                if (parent != null && existing.transform.parent != parent)
                    existing.transform.SetParent(parent, false);
                return existing;
            }

            var host = new GameObject(HostName);
            if (parent != null)
                host.transform.SetParent(parent, false);

            return host.AddComponent<ProductionRaceHud>();
        }

        public void Configure(
            ArcadeCarController playerCar,
            RaceDirector director,
            CareerGameSession careerSession = null)
        {
            player = playerCar ?? throw new ArgumentNullException(nameof(playerCar));
            race = director ?? throw new ArgumentNullException(nameof(director));
            career = careerSession;
        }

        private void Update()
        {
            if (!ResolveRuntime()) return;
            if (safeRoot == null) BuildHud();
            ApplySafeArea();

            positionText.text = $"POS  {race.Position}/4";
            timeText.text = $"{race.RaceTime:0.0} s";
            speedText.text = $"{Mathf.Abs(player.SpeedKph):000}\nKM/H";
            spiritText.text = $"SPIRIT  {Mathf.RoundToInt(player.NitroEnergy * 100f)}%";
            spiritFill.fillAmount = Mathf.Clamp01(player.NitroEnergy);
            RefreshCareer();
        }

        private void RefreshCareer()
        {
            if (careerText == null)
                return;
            if (career == null)
            {
                careerText.text = "AFAREET CAREER";
                return;
            }
            if (career.CampaignComplete)
            {
                careerText.text = $"CHAPTER 1 COMPLETE  •  {career.Progress.Stars} STARS";
                return;
            }
            if (career.ActiveDefinition == null)
            {
                careerText.text = $"CAREER  •  {career.Progress.Stars} STARS";
                return;
            }

            var node = career.ActiveDefinition.Node;
            careerText.text = $"{node.Id.ToUpperInvariant()}  •  {ModeLabel(node.Mode)}  •  {career.Progress.Stars} STARS";
        }

        private bool ResolveRuntime()
        {
            if (player == null)
            {
                var hero = GameObject.Find("PLAYER HERO — AFAREET");
                if (hero != null) player = hero.GetComponent<ArcadeCarController>();
            }

            if (race == null) race = FindFirstObjectByType<RaceDirector>();
            if (career == null) career = FindFirstObjectByType<CareerGameSession>();
            return player != null && race != null;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Production HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            positionText = Panel("Position", new Vector2(24, -24), new Vector2(210, 58), TextAnchor.MiddleCenter, true);
            timeText = Panel("Time", new Vector2(-24, -24), new Vector2(210, 58), TextAnchor.MiddleCenter, false);
            speedText = Panel("Speed", new Vector2(-24, 24), new Vector2(230, 105), TextAnchor.MiddleCenter, false, true);
            spiritText = Panel("Spirit", new Vector2(24, 24), new Vector2(275, 74), TextAnchor.UpperCenter, true, true);
            careerText = CenterTopPanel("Career", new Vector2(0f, -24f), new Vector2(440f, 58f));

            var fillBg = CreateImage("Spirit Bar BG", spiritText.transform.parent, new Color(.03f, .02f, .08f, .94f));
            SetAnchored(fillBg.rectTransform, new Vector2(16, 12), new Vector2(243, 16), new Vector2(0, 0), new Vector2(0, 0));
            spiritFill = CreateImage("Spirit Bar Fill", fillBg.transform, new Color(.54f, .05f, 1f, 1f));
            spiritFill.type = Image.Type.Filled;
            spiritFill.fillMethod = Image.FillMethod.Horizontal;
            spiritFill.fillOrigin = 0;
            spiritFill.rectTransform.anchorMin = Vector2.zero;
            spiritFill.rectTransform.anchorMax = Vector2.one;
            spiritFill.rectTransform.offsetMin = spiritFill.rectTransform.offsetMax = Vector2.zero;
        }

        private Text CenterTopPanel(string panelName, Vector2 offset, Vector2 size)
        {
            var image = CreateImage(panelName, safeRoot, new Color(.018f, .012f, .045f, .88f));
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;

            var text = new GameObject(panelName + " Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(image.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 6f);
            text.rectTransform.offsetMax = new Vector2(-8f, -6f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private Text Panel(string panelName, Vector2 offset, Vector2 size, TextAnchor alignment, bool left, bool bottom = false)
        {
            var image = CreateImage(panelName, safeRoot, new Color(.018f, .012f, .045f, .88f));
            var anchor = new Vector2(left ? 0f : 1f, bottom ? 0f : 1f);
            SetAnchored(image.rectTransform, offset, size, anchor, anchor);

            var text = new GameObject(panelName + " Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(image.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8, 6);
            text.rectTransform.offsetMax = new Vector2(-8, -6);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = bottom ? 26 : 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            var image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetAnchored(RectTransform rect, Vector2 offset, Vector2 size, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(min.x, min.y);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        private static string ModeLabel(Afareet.Progression.CareerRaceMode mode)
        {
            switch (mode)
            {
                case Afareet.Progression.CareerRaceMode.Circuit: return "CIRCUIT";
                case Afareet.Progression.CareerRaceMode.TimeTrial: return "TIME TRIAL";
                case Afareet.Progression.CareerRaceMode.Elimination: return "ELIMINATION";
                case Afareet.Progression.CareerRaceMode.DriftChallenge: return "DRIFT";
                case Afareet.Progression.CareerRaceMode.Boss: return "BOSS";
                default: return mode.ToString().ToUpperInvariant();
            }
        }

        private void ApplySafeArea()
        {
            var safe = Screen.safeArea;
            if (safe == lastSafeArea) return;
            lastSafeArea = safe;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
    }
}
