using System;
using System.Text;
using Afareet.CareerRuntime;
using Afareet.Race;
using UnityEngine;
using UnityEngine.UI;

namespace Afareet.UI
{
    public sealed class ProductionCareerBriefingOverlay : MonoBehaviour
    {
        private CareerGameSession career;
        private RaceDirector race;
        private RectTransform safeRoot;
        private GameObject briefingPanel;
        private Text titleText;
        private Text objectivesText;
        private Text profileText;
        private Text recoveryText;
        private Rect lastSafeArea;

        public static ProductionCareerBriefingOverlay EnsureInstalled(Transform owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var existing = FindFirstObjectByType<ProductionCareerBriefingOverlay>();
            if (existing != null)
            {
                if (existing.transform.parent != owner)
                    existing.transform.SetParent(owner, false);
                return existing;
            }

            var host = new GameObject("AFAREET CAREER BRIEFING");
            host.transform.SetParent(owner, false);
            return host.AddComponent<ProductionCareerBriefingOverlay>();
        }

        public void Configure(CareerGameSession session, RaceDirector director)
        {
            career = session != null ? session : throw new ArgumentNullException(nameof(session));
            race = director != null ? director : throw new ArgumentNullException(nameof(director));
        }

        private void Update()
        {
            if (career == null || race == null)
                return;
            if (safeRoot == null)
                BuildUi();

            ApplySafeArea();
            var visible = race.Phase == RaceRoundPhase.Ready;
            briefingPanel.SetActive(visible);
            if (visible)
                RefreshBriefing();
        }

        private void RefreshBriefing()
        {
            if (career.CampaignComplete)
            {
                titleText.text = "CHAPTER 1 COMPLETE";
                objectivesText.text = "All Chapter 1 events are complete.";
            }
            else if (career.ActiveDefinition != null)
            {
                var definition = career.ActiveDefinition;
                titleText.text = $"{definition.Node.Id.ToUpperInvariant()}  •  {ModeLabel(definition.Node.Mode)}";
                var builder = new StringBuilder();
                for (var index = 0; index < definition.Objectives.Count; index++)
                {
                    if (index > 0) builder.AppendLine();
                    builder.Append("• ");
                    builder.Append(definition.Objectives[index].Description);
                }
                objectivesText.text = builder.ToString();
            }
            else
            {
                titleText.text = "AFAREET CAREER";
                objectivesText.text = "No playable event is currently available.";
            }

            profileText.text = career.Profile == null
                ? string.Empty
                : $"{career.Progress.Stars} STARS   •   {career.Profile.Coins} COINS   •   {career.Profile.Spirit} SPIRIT";
            recoveryText.text = career.RecoveredInvalidSave
                ? "SAVE RECOVERY MODE — invalid stored profile preserved for diagnosis"
                : string.Empty;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Career Briefing Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            briefingPanel = new GameObject("Career Briefing Panel", typeof(RectTransform), typeof(Image));
            briefingPanel.transform.SetParent(safeRoot, false);
            var panelRect = briefingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(430f, 190f);
            panelRect.anchoredPosition = new Vector2(20f, -88f);
            var image = briefingPanel.GetComponent<Image>();
            image.color = new Color(.012f, .008f, .035f, .82f);
            image.raycastTarget = false;

            titleText = CreateText(panelRect, "Event Title", new Vector2(0f, 62f), new Vector2(390f, 36f), 23, FontStyle.Bold);
            titleText.alignment = TextAnchor.MiddleLeft;

            objectivesText = CreateText(panelRect, "Objectives", new Vector2(0f, 12f), new Vector2(390f, 78f), 15, FontStyle.Normal);
            objectivesText.alignment = TextAnchor.UpperLeft;

            profileText = CreateText(panelRect, "Profile", new Vector2(0f, -56f), new Vector2(390f, 30f), 15, FontStyle.Bold);
            profileText.alignment = TextAnchor.MiddleLeft;

            recoveryText = CreateText(panelRect, "Recovery", new Vector2(0f, -80f), new Vector2(390f, 24f), 11, FontStyle.Bold);
            recoveryText.alignment = TextAnchor.MiddleLeft;
            recoveryText.color = new Color(1f, .72f, .25f);
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size,
            int fontSize,
            FontStyle style)
        {
            var text = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static string ModeLabel(Afareet.Progression.CareerRaceMode mode)
        {
            switch (mode)
            {
                case Afareet.Progression.CareerRaceMode.Circuit: return "CIRCUIT";
                case Afareet.Progression.CareerRaceMode.TimeTrial: return "TIME TRIAL";
                case Afareet.Progression.CareerRaceMode.Elimination: return "ELIMINATION";
                case Afareet.Progression.CareerRaceMode.DriftChallenge: return "DRIFT CHALLENGE";
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
