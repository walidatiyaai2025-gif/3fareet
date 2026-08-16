using System;
using Afareet.CareerRuntime;
using Afareet.Race;
using UnityEngine;
using UnityEngine.UI;

namespace Afareet.UI
{
    public sealed class ProductionRaceFlowOverlay : MonoBehaviour
    {
        private const string HostName = "AFAREET RACE FLOW OVERLAY";

        private RaceDirector race;
        private CareerGameSession career;
        private RectTransform safeRoot;
        private RectTransform pauseButtonRect;
        private RectTransform resumeButtonRect;
        private RectTransform restartButtonRect;
        private RectTransform nextButtonRect;
        private GameObject pausePanel;
        private GameObject resultsPanel;
        private Text pauseButtonLabel;
        private Text pauseTitle;
        private Text resumeLabel;
        private Text resultsTitle;
        private Text positionLabel;
        private Text timeLabel;
        private Text settlementLabel;
        private Text restartLabel;
        private Text nextLabel;
        private Rect lastSafeArea;

        public bool HasRuntimeBinding => race != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            EnsureInstalled();
        }

        public static ProductionRaceFlowOverlay EnsureInstalled(Transform parent = null)
        {
            var existing = FindFirstObjectByType<ProductionRaceFlowOverlay>();
            if (existing != null)
            {
                if (parent != null && existing.transform.parent != parent)
                    existing.transform.SetParent(parent, false);
                return existing;
            }

            var host = new GameObject(HostName);
            if (parent != null)
                host.transform.SetParent(parent, false);

            return host.AddComponent<ProductionRaceFlowOverlay>();
        }

        public void Configure(RaceDirector director, CareerGameSession careerSession = null)
        {
            race = director ?? throw new ArgumentNullException(nameof(director));
            career = careerSession;
        }

        private void Update()
        {
            if (!ResolveRuntime()) return;
            if (safeRoot == null) BuildUi();

            ApplySafeArea();
            HandleKeyboard();
            HandlePointerInput();
            RefreshPresentation();
        }

        private bool ResolveRuntime()
        {
            if (race == null) race = FindFirstObjectByType<RaceDirector>();
            if (career == null) career = FindFirstObjectByType<CareerGameSession>();
            return race != null;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Race Flow Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            pauseButtonRect = CreateButtonVisual(
                safeRoot,
                "Pause Button",
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(150f, 56f),
                out pauseButtonLabel);

            pausePanel = CreateCenteredPanel("Pause Panel", new Vector2(520f, 300f), out var pauseRoot);
            pauseTitle = CreateText(pauseRoot, "Pause Title", new Vector2(0f, 78f), new Vector2(440f, 60f), 34, FontStyle.Bold);
            resumeButtonRect = CreateButtonVisual(
                pauseRoot,
                "Resume Button",
                new Vector2(.5f, .5f),
                new Vector2(0f, -42f),
                new Vector2(280f, 72f),
                out resumeLabel);

            resultsPanel = CreateCenteredPanel("Results Panel", new Vector2(650f, 470f), out var resultsRoot);
            resultsTitle = CreateText(resultsRoot, "Results Title", new Vector2(0f, 174f), new Vector2(560f, 58f), 32, FontStyle.Bold);
            positionLabel = CreateText(resultsRoot, "Position", new Vector2(0f, 102f), new Vector2(520f, 42f), 22, FontStyle.Bold);
            timeLabel = CreateText(resultsRoot, "Finish Time", new Vector2(0f, 58f), new Vector2(520f, 42f), 22, FontStyle.Bold);
            settlementLabel = CreateText(resultsRoot, "Career Settlement", new Vector2(0f, -4f), new Vector2(560f, 72f), 19, FontStyle.Bold);

            restartButtonRect = CreateButtonVisual(
                resultsRoot,
                "Restart Button",
                new Vector2(.5f, .5f),
                new Vector2(-155f, -154f),
                new Vector2(270f, 70f),
                out restartLabel);
            nextButtonRect = CreateButtonVisual(
                resultsRoot,
                "Next Event Button",
                new Vector2(.5f, .5f),
                new Vector2(155f, -154f),
                new Vector2(270f, 70f),
                out nextLabel);

            pausePanel.SetActive(false);
            resultsPanel.SetActive(false);
        }

        private GameObject CreateCenteredPanel(string name, Vector2 size, out RectTransform root)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(safeRoot, false);
            root = panel.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, .5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = size;

            var image = panel.GetComponent<Image>();
            image.color = new Color(.012f, .008f, .035f, .96f);
            image.raycastTarget = false;

            var accent = new GameObject("Spirit Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            accent.transform.SetParent(root, false);
            accent.rectTransform.anchorMin = new Vector2(0f, 1f);
            accent.rectTransform.anchorMax = new Vector2(1f, 1f);
            accent.rectTransform.pivot = new Vector2(.5f, 1f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(0f, 6f);
            accent.color = new Color(.52f, .03f, 1f, 1f);
            accent.raycastTarget = false;
            return panel;
        }

        private static RectTransform CreateButtonVisual(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 offset,
            Vector2 size,
            out Text label)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            image.rectTransform.pivot = anchor;
            image.rectTransform.anchoredPosition = offset;
            image.rectTransform.sizeDelta = size;
            image.color = new Color(.42f, .025f, .82f, .96f);
            image.raycastTarget = false;

            label = CreateText(image.transform, name + " Label", Vector2.zero, size - new Vector2(18f, 12f), 22, FontStyle.Bold);
            return image.rectTransform;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            FontStyle style)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.rectTransform.anchoredPosition = anchoredPosition;
            text.rectTransform.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void HandleKeyboard()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (RaceUiPresentationPolicy.CanPause(race.Phase, race.IsPaused))
                    race.SetPaused(true);
                else if (RaceUiPresentationPolicy.CanResume(race.Phase, race.IsPaused))
                    race.SetPaused(false);
            }

            if (race.Phase == RaceRoundPhase.Results && Input.GetKeyDown(KeyCode.R))
                RestartRace();
            if (race.Phase == RaceRoundPhase.Results && Input.GetKeyDown(KeyCode.N) && CanAdvanceCareer())
                career.TryAdvanceToNextEvent();
#endif
        }

        private void HandlePointerInput()
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began) HandlePoint(touch.position);
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) HandlePoint(Input.mousePosition);
#endif
        }

        private void HandlePoint(Vector2 screenPoint)
        {
            if (RaceUiPresentationPolicy.CanPause(race.Phase, race.IsPaused) && Contains(pauseButtonRect, screenPoint))
            {
                race.SetPaused(true);
                return;
            }

            if (RaceUiPresentationPolicy.CanResume(race.Phase, race.IsPaused) && Contains(resumeButtonRect, screenPoint))
            {
                race.SetPaused(false);
                return;
            }

            if (RaceUiPresentationPolicy.CanRestart(race.Phase) && Contains(restartButtonRect, screenPoint))
            {
                RestartRace();
                return;
            }

            if (CanAdvanceCareer() && Contains(nextButtonRect, screenPoint))
                career.TryAdvanceToNextEvent();
        }

        private void RestartRace()
        {
            if (career != null && career.HasActiveEvent)
                career.RestartCurrentEvent();
            else
                race.RestartRace();
        }

        private void RefreshPresentation()
        {
            var mode = RaceUiPresentationPolicy.Resolve(race.Phase, race.IsPaused);
            pauseButtonRect.gameObject.SetActive(RaceUiPresentationPolicy.CanPause(race.Phase, race.IsPaused));
            pausePanel.SetActive(mode == RaceOverlayMode.Pause);
            resultsPanel.SetActive(mode == RaceOverlayMode.Results);

            pauseButtonLabel.text = RuntimeLocalization.Text("pause");
            pauseTitle.text = RuntimeLocalization.Text("pause");
            resumeLabel.text = RuntimeLocalization.Text("resume");
            restartLabel.text = "RETRY";
            nextLabel.text = "NEXT EVENT";
            positionLabel.text = $"{RuntimeLocalization.Text("position")}  {race.Position}/4";
            timeLabel.text = $"{RuntimeLocalization.Text("time")}  {Mathf.Max(0f, race.FinishTime):0.00} s";

            if (career == null || career.LastSettlement == null)
            {
                resultsTitle.text = RuntimeLocalization.Text("results");
                settlementLabel.text = string.Empty;
                nextButtonRect.gameObject.SetActive(false);
                return;
            }

            var settlement = career.LastSettlement;
            var passed = career.ActiveDefinition != null &&
                         career.Progress.IsNodeCompleted(career.ActiveDefinition.Node.Id);
            resultsTitle.text = passed ? "EVENT CLEARED" : "EVENT FAILED";

            if (passed)
            {
                var reward = settlement.GrantedAnyReward
                    ? $"  •  +{settlement.CoinsGranted} COINS  +{settlement.SpiritGranted} SPIRIT"
                    : string.Empty;
                var stars = settlement.StarsEarned > 0
                    ? $"+{settlement.StarsEarned} STARS"
                    : "COMPLETED";
                settlementLabel.text = $"{stars}{reward}";
            }
            else
            {
                settlementLabel.text = $"OBJECTIVES  {settlement.Evaluation.CompletedCount}/{settlement.Evaluation.Entries.Count}  •  RETRY TO ADVANCE";
            }

            nextButtonRect.gameObject.SetActive(CanAdvanceCareer());
        }

        private bool CanAdvanceCareer()
        {
            return career != null &&
                   race.Phase == RaceRoundPhase.Results &&
                   career.ActiveDefinition != null &&
                   career.Progress.IsNodeCompleted(career.ActiveDefinition.Node.Id) &&
                   !career.CampaignComplete;
        }

        private static bool Contains(RectTransform rect, Vector2 screenPoint) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);

        private void ApplySafeArea()
        {
            var safe = Screen.safeArea;
            if (safe == lastSafeArea) return;
            lastSafeArea = safe;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (race != null && race.IsPaused) race.SetPaused(false);
        }
    }
}
