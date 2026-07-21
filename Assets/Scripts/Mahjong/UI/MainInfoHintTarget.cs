using System;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainInfoHintTarget : MonoBehaviour
    {
        // Temporary feature gate: keep the implementation intact while hints are disabled.
        public static bool FeatureEnabled => false;

        private const int OverlaySortingOrder = 32760;
        private static readonly Color MarkerColor = new Color(1f, 0.82f, 0.38f, 0.46f);
        private static readonly Color MarkerOutlineColor = new Color(1f, 0.96f, 0.72f, 0.48f);
        private static readonly Color GlowColor = new Color(1f, 0.78f, 0.22f, 0.12f);
        private static readonly Color GlowOutlineColor = new Color(1f, 0.86f, 0.38f, 0.48f);

        private Button targetButton;
        private string titleKey;
        private string bodyKey;
        private Sprite iconSprite;
        private RectTransform markerRect;
        private CanvasGroup markerGroup;
        private RectTransform glowRect;
        private CanvasGroup glowGroup;
        private RectTransform clickCatcherRect;
        private CanvasGroup clickCatcherGroup;
        private Vector2 markerBasePosition;

        public static MainInfoHintTarget Attach(Button button, string titleLocalizationKey, string bodyLocalizationKey, Sprite icon = null)
        {
            if (button == null)
                return null;

            if (!FeatureEnabled)
            {
                Detach(button);
                return null;
            }

            MainInfoHintTarget target = button.GetComponent<MainInfoHintTarget>();
            if (target == null)
                target = button.gameObject.AddComponent<MainInfoHintTarget>();

            target.Configure(button, titleLocalizationKey, bodyLocalizationKey, icon);
            return target;
        }

        public static void Detach(Button button)
        {
            if (button == null)
                return;

            string[] childNames = { "MainInfoPulseMarker", "MainInfoButtonGlow", "MainInfoClickCatcher" };
            for (int i = 0; i < childNames.Length; i++)
            {
                Transform child = button.transform.Find(childNames[i]);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            MainInfoHintTarget target = button.GetComponent<MainInfoHintTarget>();
            if (target != null)
            {
                target.enabled = false;
                Destroy(target);
            }
        }

        public static GameObject ShowModal(
            Canvas rootCanvas,
            string titleLocalizationKey,
            string bodyLocalizationKey,
            Sprite icon = null,
            Action onClosed = null)
        {
            if (rootCanvas == null)
                return null;

            if (!FeatureEnabled)
                return null;

            MainInfoHintOverlayMarker[] existingOverlays = FindObjectsByType<MainInfoHintOverlayMarker>(FindObjectsInactive.Include);
            for (int i = 0; i < existingOverlays.Length; i++)
            {
                if (existingOverlays[i] != null)
                    existingOverlays[i].CloseOverlay();
            }

            GameObject overlay = new GameObject(
                "MainInfoHintOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(MainInfoHintOverlayMarker));
            overlay.transform.SetParent(rootCanvas.transform, false);
            overlay.transform.SetAsLastSibling();
            MainInfoHintOverlayMarker overlayMarker = overlay.GetComponent<MainInfoHintOverlayMarker>();
            overlayMarker.Configure(onClosed);

            RectTransform overlayRoot = overlay.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;

            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingLayerName = rootCanvas.sortingLayerName;
            overlayCanvas.sortingOrder = OverlaySortingOrder;

            Canvas.ForceUpdateCanvases();

            MainInfoCoordinator coordinator = MainInfoCoordinator.Ensure(overlayRoot);
            coordinator.CreateDimBackdrop(overlayRoot);
            Transform dimBackdrop = overlayRoot.Find("MainInfoDimBackdrop");
            MainInfoLayerElement dimElement = dimBackdrop != null ? dimBackdrop.GetComponent<MainInfoLayerElement>() : null;
            if (dimElement != null)
                dimElement.SetVisible(true);

            Vector2 cardSize = ResolveCardSize(overlayRoot.rect.size);
            Vector2 cardPosition = new Vector2(0f, 44f);
            Vector2 buttonSize = new Vector2(370f, 98f);
            Vector2 buttonPosition = new Vector2(0f, cardPosition.y - cardSize.y * 0.5f - 72f);

            MainInfoCard card = coordinator.CreateCard(
                overlayRoot,
                "InfoModalCard",
                cardPosition,
                cardSize,
                titleLocalizationKey,
                bodyLocalizationKey,
                icon);
            if (card != null)
                card.SetVisible(true);

            Button closeButton = coordinator.CreateUnderstoodButton(overlayRoot, buttonPosition, buttonSize, () =>
            {
                if (overlayMarker != null)
                    overlayMarker.CloseOverlay();
            });
            MainInfoLayerElement closeElement = closeButton != null ? closeButton.GetComponent<MainInfoLayerElement>() : null;
            if (closeElement != null)
                closeElement.SetVisible(true);

            return overlay;
        }

        public void Configure(Button button, string titleLocalizationKey, string bodyLocalizationKey, Sprite icon = null)
        {
            targetButton = button;
            titleKey = titleLocalizationKey;
            bodyKey = bodyLocalizationKey;
            iconSprite = icon != null ? icon : ResolveButtonSprite(button);
            EnsureMarker();
            EnsureButtonGlow();
            EnsureClickCatcher();
            if (glowRect != null)
                glowRect.SetAsFirstSibling();
            if (markerRect != null)
                markerRect.SetAsLastSibling();
            if (clickCatcherRect != null)
                clickCatcherRect.SetAsLastSibling();
            RefreshVisibility();
        }

        private void OnEnable()
        {
            AppSettings.OnInfoHintsChanged += OnInfoHintsChanged;
            EnsureClickCatcher();
            RefreshVisibility();
        }

        private void OnDisable()
        {
            AppSettings.OnInfoHintsChanged -= OnInfoHintsChanged;
        }

        private void Update()
        {
            if (!ShouldShowTrainingMarker())
                return;

            float t = Time.unscaledTime * 5.2f;
            float pulse = (Mathf.Sin(t) + 1f) * 0.5f;
            if (markerRect != null && markerGroup != null)
            {
                markerRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.06f, pulse);
                markerRect.anchoredPosition = markerBasePosition;
                markerGroup.alpha = Mathf.Lerp(0.34f, 0.62f, pulse);
            }

            if (glowRect != null && glowGroup != null)
            {
                glowRect.localScale = Vector3.one * Mathf.Lerp(0.985f, 1.035f, pulse);
                glowGroup.alpha = Mathf.Lerp(0.34f, 0.82f, pulse);
            }
        }

        private void EnsureMarker()
        {
            if (markerRect != null)
                return;

            Transform existing = transform.Find("MainInfoPulseMarker");
            if (existing != null)
            {
                markerRect = existing as RectTransform;
                markerGroup = existing.GetComponent<CanvasGroup>();
                return;
            }

            GameObject marker = new GameObject("MainInfoPulseMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(CanvasGroup), typeof(Outline));
            marker.transform.SetParent(transform, false);
            marker.transform.SetAsLastSibling();
            markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(1f, 1f);
            markerRect.anchorMax = new Vector2(1f, 1f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerBasePosition = new Vector2(-18f, -18f);
            markerRect.anchoredPosition = markerBasePosition;
            markerRect.sizeDelta = new Vector2(34f, 34f);

            AllianceRoundedGraphic graphic = marker.GetComponent<AllianceRoundedGraphic>();
            graphic.color = MarkerColor;
            graphic.CornerRadius = 18f;
            graphic.CornerSegments = 16;
            graphic.raycastTarget = false;

            Outline outline = marker.GetComponent<Outline>();
            outline.effectColor = MarkerOutlineColor;
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            markerGroup = marker.GetComponent<CanvasGroup>();
        }

        private void EnsureButtonGlow()
        {
            if (glowRect != null)
                return;

            Transform existing = transform.Find("MainInfoButtonGlow");
            if (existing != null)
            {
                glowRect = existing as RectTransform;
                glowGroup = existing.GetComponent<CanvasGroup>();
                return;
            }

            GameObject glow = new GameObject("MainInfoButtonGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(CanvasGroup), typeof(Outline));
            glow.transform.SetParent(transform, false);
            glow.transform.SetAsFirstSibling();

            glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-8f, -8f);
            glowRect.offsetMax = new Vector2(8f, 8f);

            AllianceRoundedGraphic graphic = glow.GetComponent<AllianceRoundedGraphic>();
            graphic.color = GlowColor;
            graphic.CornerRadius = 36f;
            graphic.CornerSegments = 16;
            graphic.raycastTarget = false;

            Outline outline = glow.GetComponent<Outline>();
            outline.effectColor = GlowOutlineColor;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            glowGroup = glow.GetComponent<CanvasGroup>();
        }

        private void EnsureClickCatcher()
        {
            if (clickCatcherRect != null)
                return;

            Transform existing = transform.Find("MainInfoClickCatcher");
            if (existing != null)
            {
                clickCatcherRect = existing as RectTransform;
                clickCatcherGroup = existing.GetComponent<CanvasGroup>();
                if (clickCatcherRect != null)
                {
                    clickCatcherRect.anchorMin = Vector2.one;
                    clickCatcherRect.anchorMax = Vector2.one;
                    clickCatcherRect.pivot = new Vector2(0.5f, 0.5f);
                    clickCatcherRect.anchoredPosition = new Vector2(-18f, -18f);
                    clickCatcherRect.sizeDelta = new Vector2(52f, 52f);
                }
                return;
            }

            GameObject catcher = new GameObject("MainInfoClickCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
            catcher.transform.SetParent(transform, false);
            catcher.transform.SetAsLastSibling();

            clickCatcherRect = catcher.GetComponent<RectTransform>();
            clickCatcherRect.anchorMin = Vector2.one;
            clickCatcherRect.anchorMax = Vector2.one;
            clickCatcherRect.pivot = new Vector2(0.5f, 0.5f);
            clickCatcherRect.anchoredPosition = new Vector2(-18f, -18f);
            clickCatcherRect.sizeDelta = new Vector2(52f, 52f);

            Image image = catcher.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            clickCatcherGroup = catcher.GetComponent<CanvasGroup>();

            Button button = catcher.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(ShowHint);
        }

        private void ShowHint()
        {
            if (!FeatureEnabled || !MainInfoCoordinator.HintsEnabled || targetButton == null)
                return;

            Canvas rootCanvas = targetButton.GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindAnyObjectByType<Canvas>();

            if (rootCanvas == null)
                return;

            RectTransform targetRect = targetButton.transform as RectTransform;
            if (targetRect == null)
                return;

            if (!MainHubStateController.CanOpenMainWindow("InfoHint"))
                return;

            GameObject overlay = new GameObject(
                "MainInfoHintOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(MainInfoHintOverlayMarker));
            overlay.transform.SetParent(rootCanvas.transform, false);
            overlay.transform.SetAsLastSibling();
            MainInfoHintOverlayMarker overlayMarker = overlay.GetComponent<MainInfoHintOverlayMarker>();
            overlayMarker.Configure(MainHubStateController.NotifyMainWindowClosed);

            RectTransform overlayRoot = overlay.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;

            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingLayerName = rootCanvas.sortingLayerName;
            overlayCanvas.sortingOrder = OverlaySortingOrder;

            Canvas.ForceUpdateCanvases();

            MainInfoCoordinator coordinator = MainInfoCoordinator.Ensure(overlayRoot);
            coordinator.CreateDimBackdrop(overlayRoot);

            Vector2 cardSize = ResolveCardSize(overlayRoot.rect.size);
            Vector2 cardPosition = Vector2.zero;

            Vector2 understoodSize = new Vector2(370f, 98f);
            float buttonGap = 64f;
            Vector2 buttonPosition = new Vector2(0f, cardPosition.y - cardSize.y * 0.5f - buttonGap);
            float minButtonY = -overlayRoot.rect.height * 0.5f + understoodSize.y * 0.5f + 26f;
            if (buttonPosition.y < minButtonY)
            {
                float lift = minButtonY - buttonPosition.y;
                cardPosition.y += lift;
                cardPosition = ClampCardPosition(cardPosition, cardSize, overlayRoot.rect.size);
                buttonPosition.y = Mathf.Max(minButtonY, cardPosition.y - cardSize.y * 0.5f - buttonGap);
            }

            Sprite resolvedIcon = iconSprite != null ? iconSprite : ResolveButtonSprite(targetButton);
            coordinator.CreateCard(overlayRoot, "Info_" + gameObject.name, cardPosition, cardSize, titleKey, bodyKey, resolvedIcon);
            coordinator.CreateUnderstoodButton(overlayRoot, buttonPosition, understoodSize, () =>
            {
                MarkAcknowledged();
                RefreshVisibility();
                if (overlayMarker != null)
                    overlayMarker.CloseOverlay();
            });
        }

        private void OnInfoHintsChanged(bool enabled)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool visible = ShouldShowTrainingMarker();
            if (markerRect != null)
                markerRect.gameObject.SetActive(visible);
            if (markerGroup != null)
            {
                markerGroup.interactable = false;
                markerGroup.blocksRaycasts = false;
            }

            if (glowRect != null)
                glowRect.gameObject.SetActive(visible);
            if (glowGroup != null)
            {
                glowGroup.interactable = false;
                glowGroup.blocksRaycasts = false;
            }

            if (clickCatcherRect != null)
                clickCatcherRect.gameObject.SetActive(visible);
            if (clickCatcherGroup != null)
            {
                clickCatcherGroup.interactable = visible;
                clickCatcherGroup.blocksRaycasts = visible;
            }
        }

        private bool ShouldShowTrainingMarker()
        {
            return MainInfoCoordinator.HintsEnabled && !IsAcknowledged();
        }

        private bool IsAcknowledged()
        {
            return PlayerPrefs.GetInt(SeenPrefsKey, 0) == 1;
        }

        private void MarkAcknowledged()
        {
            PlayerPrefs.SetInt(SeenPrefsKey, 1);
            PlayerPrefs.Save();
        }

        private string SeenPrefsKey => "main_info_seen_" + (string.IsNullOrWhiteSpace(bodyKey) ? gameObject.name : bodyKey);

        private static Vector2 ResolveCardSize(Vector2 rootSize)
        {
            float width = Mathf.Clamp(rootSize.x * 0.62f, 840f, Mathf.Max(840f, rootSize.x - 120f));
            float height = Mathf.Clamp(rootSize.y * 0.26f, 250f, 330f);
            return new Vector2(width, height);
        }

        private static Vector2 ClampCardPosition(Vector2 position, Vector2 cardSize, Vector2 rootSize)
        {
            float halfWidth = rootSize.x * 0.5f;
            float halfHeight = rootSize.y * 0.5f;
            float margin = 26f;
            position.x = Mathf.Clamp(position.x, -halfWidth + cardSize.x * 0.5f + margin, halfWidth - cardSize.x * 0.5f - margin);
            position.y = Mathf.Clamp(position.y, -halfHeight + cardSize.y * 0.5f + margin, halfHeight - cardSize.y * 0.5f - margin);
            return position;
        }

        private static Sprite ResolveButtonSprite(Button button)
        {
            if (button == null)
                return null;

            Image image = button.GetComponent<Image>();
            return image != null ? image.sprite : null;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class MainInfoHintOverlayMarker : MonoBehaviour
    {
        private Action onClosed;
        private bool closeNotified;
        private bool closeRequested;

        public void Configure(Action callback)
        {
            onClosed = callback;
        }

        public void CloseOverlay()
        {
            if (closeRequested)
                return;

            closeRequested = true;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            NotifyClosed();
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            NotifyClosed();

            if (!closeRequested)
            {
                closeRequested = true;
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            NotifyClosed();
        }

        private void NotifyClosed()
        {
            if (closeNotified)
                return;

            closeNotified = true;
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }
    }
}
