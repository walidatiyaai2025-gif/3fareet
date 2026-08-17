using System;
using System.Collections.Generic;
using Afareet.Race;
using Afareet.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Afareet.UI
{
    public sealed class ProductionRaceControlsOverlay : MonoBehaviour
    {
        private static readonly PowerUpKind[] PowerUpOrder =
        {
            PowerUpKind.AsphaltShard,
            PowerUpKind.NitroSpirit,
            PowerUpKind.TrafficCurse,
            PowerUpKind.EnchantedPound,
            PowerUpKind.EyeShield
        };

        private RaceDirector race;
        private ProductionRaceInputController input;
        private RectTransform safeRoot;
        private RectTransform startRect;
        private RectTransform leftRect;
        private RectTransform rightRect;
        private RectTransform brakeRect;
        private RectTransform recoverRect;
        private RectTransform driftRect;
        private RectTransform nitroRect;
        private RectTransform throttleRect;
        private readonly RectTransform[] powerRects = new RectTransform[5];
        private readonly Text[] powerLabels = new Text[5];
        private Text feedbackText;
        private Rect lastSafeArea;

        public static ProductionRaceControlsOverlay EnsureInstalled(Transform owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var existing = FindFirstObjectByType<ProductionRaceControlsOverlay>();
            if (existing != null)
            {
                if (existing.transform.parent != owner)
                    existing.transform.SetParent(owner, false);
                return existing;
            }

            var host = new GameObject("AFAREET PRODUCTION RACE CONTROLS");
            host.transform.SetParent(owner, false);
            return host.AddComponent<ProductionRaceControlsOverlay>();
        }

        public void Configure(RaceDirector director, ProductionRaceInputController inputController)
        {
            race = director != null ? director : throw new ArgumentNullException(nameof(director));
            input = inputController != null ? inputController : throw new ArgumentNullException(nameof(inputController));
        }

        private void Update()
        {
            if (race == null || input == null)
                return;
            if (safeRoot == null)
                BuildUi();

            ApplySafeArea();
            HandleKeyboardPowerUps();
            HandlePointerInput();
            RefreshPresentation();
        }

        private void HandlePointerInput()
        {
            var steer = 0f;
            var throttle = 0f;
            var drift = false;
            var nitro = false;
            var brakeReverse = false;

            for (var index = 0; index < Input.touchCount; index++)
            {
                var touch = Input.GetTouch(index);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    continue;
                if (touch.phase == TouchPhase.Began)
                    HandleDiscretePoint(touch.position);
                ApplyContinuousPoint(touch.position, ref steer, ref throttle, ref drift, ref nitro, ref brakeReverse);
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
                HandleDiscretePoint(Input.mousePosition);
            if (Input.GetMouseButton(0))
                ApplyContinuousPoint(Input.mousePosition, ref steer, ref throttle, ref drift, ref nitro, ref brakeReverse);
#endif

            input.ApplyDriveFrame(steer, throttle, drift, nitro, brakeReverse);
        }

        private void HandleDiscretePoint(Vector2 point)
        {
            if (race.Phase == RaceRoundPhase.Ready && Contains(startRect, point))
            {
                input.StartRace();
                return;
            }

            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
                return;

            if (Contains(recoverRect, point))
            {
                input.RecoverPlayer();
                SetFeedback("RECOVERED");
                return;
            }

            for (var index = 0; index < powerRects.Length; index++)
            {
                if (!Contains(powerRects[index], point))
                    continue;
                UsePowerUp(PowerUpOrder[index]);
                return;
            }
        }

        private void ApplyContinuousPoint(
            Vector2 point,
            ref float steer,
            ref float throttle,
            ref bool drift,
            ref bool nitro,
            ref bool brakeReverse)
        {
            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
                return;

            if (Contains(leftRect, point)) steer = MobileDriveInputPolicy.ResolveTouchSteer(-1f);
            if (Contains(rightRect, point)) steer = MobileDriveInputPolicy.ResolveTouchSteer(1f);
            if (Contains(brakeRect, point)) brakeReverse = true;
            if (Contains(driftRect, point)) drift = true;
            if (Contains(nitroRect, point)) nitro = true;
            if (Contains(throttleRect, point)) throttle = 1f;
        }

        private void HandleKeyboardPowerUps()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) UsePowerUp(PowerUpOrder[0]);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UsePowerUp(PowerUpOrder[1]);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UsePowerUp(PowerUpOrder[2]);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UsePowerUp(PowerUpOrder[3]);
            if (Input.GetKeyDown(KeyCode.Alpha5)) UsePowerUp(PowerUpOrder[4]);
#endif
        }

        private void UsePowerUp(PowerUpKind kind)
        {
            var result = race.TryUsePlayerPowerUp(kind);
            if (result == null)
            {
                SetFeedback("POWER-UP UNAVAILABLE");
                return;
            }

            switch (result.Status)
            {
                case PowerUpRuntimeUseStatus.Used:
                    SetFeedback($"{ShortName(kind)} USED");
                    break;
                case PowerUpRuntimeUseStatus.BlockedByEyeShield:
                    SetFeedback("BLOCKED BY SHIELD");
                    break;
                case PowerUpRuntimeUseStatus.NoCharges:
                    SetFeedback("NO CHARGES");
                    break;
                case PowerUpRuntimeUseStatus.CooldownActive:
                    SetFeedback($"COOLDOWN {result.CooldownRemainingSeconds:0.0}s");
                    break;
                case PowerUpRuntimeUseStatus.MissingTarget:
                    SetFeedback("NO VALID TARGET");
                    break;
                default:
                    SetFeedback(result.Status.ToString().ToUpperInvariant());
                    break;
            }
        }

        private void RefreshPresentation()
        {
            var ready = race.Phase == RaceRoundPhase.Ready;
            var driving = race.Phase == RaceRoundPhase.Racing && !race.IsPaused;
            startRect.gameObject.SetActive(ready);

            SetActive(leftRect, driving);
            SetActive(rightRect, driving);
            SetActive(brakeRect, driving);
            SetActive(recoverRect, driving);
            SetActive(driftRect, driving);
            SetActive(nitroRect, driving);
            SetActive(throttleRect, driving);

            var inventory = race.GetPlayerPowerUpInventory();
            for (var index = 0; index < powerRects.Length; index++)
            {
                powerRects[index].gameObject.SetActive(driving);
                var snapshot = FindInventory(inventory, PowerUpOrder[index]);
                if (snapshot == null)
                {
                    powerLabels[index].text = $"{index + 1}  {ShortName(PowerUpOrder[index])}\n--";
                    continue;
                }

                var cooldown = snapshot.CooldownRemainingSeconds > .05d
                    ? $"  {snapshot.CooldownRemainingSeconds:0.0}s"
                    : string.Empty;
                powerLabels[index].text = $"{index + 1}  {ShortName(snapshot.Kind)}\nx{snapshot.Charges}{cooldown}";
                powerLabels[index].color = snapshot.IsUsable ? Color.white : new Color(.62f, .66f, .75f);
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Production Race Controls Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            startRect = CreateControl("START RACE", new Vector2(.5f, .5f), Vector2.zero, new Vector2(320f, 92f), out _);

            leftRect = CreateControl("◀", new Vector2(0f, 0f), new Vector2(24f, 22f), new Vector2(88f, 70f), out _);
            rightRect = CreateControl("▶", new Vector2(0f, 0f), new Vector2(122f, 22f), new Vector2(88f, 70f), out _);
            brakeRect = CreateControl("BRAKE / REV", new Vector2(0f, 0f), new Vector2(220f, 22f), new Vector2(126f, 70f), out _);
            recoverRect = CreateControl("RECOVER", new Vector2(0f, 0f), new Vector2(356f, 22f), new Vector2(112f, 70f), out _);

            driftRect = CreateControl("DRIFT", new Vector2(1f, 0f), new Vector2(-386f, 22f), new Vector2(104f, 70f), out _);
            nitroRect = CreateControl("SPIRIT", new Vector2(1f, 0f), new Vector2(-272f, 22f), new Vector2(104f, 70f), out _);
            throttleRect = CreateControl("GO", new Vector2(1f, 0f), new Vector2(-158f, 22f), new Vector2(134f, 70f), out _);

            var startX = -236f;
            for (var index = 0; index < powerRects.Length; index++)
            {
                powerRects[index] = CreateControl(
                    ShortName(PowerUpOrder[index]),
                    new Vector2(.5f, 0f),
                    new Vector2(startX + index * 118f, 108f),
                    new Vector2(108f, 58f),
                    out powerLabels[index]);
                powerLabels[index].fontSize = 14;
            }

            feedbackText = CreateText(safeRoot, "Power Feedback", new Vector2(0f, 176f), new Vector2(420f, 34f), 17, FontStyle.Bold);
            feedbackText.color = new Color(.94f, .82f, 1f);
        }

        private RectTransform CreateControl(
            string labelText,
            Vector2 anchor,
            Vector2 offset,
            Vector2 size,
            out Text label)
        {
            var image = new GameObject(labelText + " Control", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(safeRoot, false);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            image.color = new Color(.17f, .035f, .30f, .90f);
            image.raycastTarget = false;

            label = CreateText(image.transform, labelText + " Label", Vector2.zero, size - new Vector2(8f, 8f), 18, FontStyle.Bold);
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            FontStyle style)
        {
            var text = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
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

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message ?? string.Empty;
        }

        private static PowerUpInventorySnapshot FindInventory(
            IReadOnlyList<PowerUpInventorySnapshot> inventory,
            PowerUpKind kind)
        {
            for (var index = 0; index < inventory.Count; index++)
                if (inventory[index].Kind == kind)
                    return inventory[index];
            return null;
        }

        private static string ShortName(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.AsphaltShard: return "SHARD";
                case PowerUpKind.NitroSpirit: return "NITRO";
                case PowerUpKind.TrafficCurse: return "CURSE";
                case PowerUpKind.EnchantedPound: return "POUND";
                case PowerUpKind.EyeShield: return "SHIELD";
                default: return kind.ToString().ToUpperInvariant();
            }
        }

        private static bool Contains(RectTransform rect, Vector2 point) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, point, null);

        private static void SetActive(RectTransform rect, bool active)
        {
            if (rect != null && rect.gameObject.activeSelf != active)
                rect.gameObject.SetActive(active);
        }

        private void ApplySafeArea()
        {
            var safe = Screen.safeArea;
            if (safe == lastSafeArea)
                return;
            lastSafeArea = safe;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
    }
}
