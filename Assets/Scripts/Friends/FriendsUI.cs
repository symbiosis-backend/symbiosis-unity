using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class FriendsUI : MonoBehaviour
    {
        private const int RootCanvasSortingOrder = 30030;
        private const string FriendsPanelFrameResourcePath = "Mahjong/Sprites/Friends/FriendsPanelFrameGenerated";
        private const string FriendsButtonResourcePath = "Mahjong/Sprites/Friends/FriendsButtonGenerated";
        private const string FriendsInputResourcePath = "Mahjong/Sprites/Friends/FriendsInputGenerated";

        private static Sprite friendsPanelFrameSprite;
        private static Sprite friendsButtonSprite;
        private static Sprite friendsInputSprite;

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject screenBackdropRoot;
        [SerializeField] private RectTransform screenBackdropRect;
        [SerializeField] private Image screenBackdropImage;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelRootRect;
        [SerializeField] private Image panelImage;
        [SerializeField] private RectTransform panelBackgroundRect;
        [SerializeField] private Image panelBackgroundImage;
        [SerializeField] private RectTransform panelFrameRect;
        [SerializeField] private Image panelFrameImage;
        [SerializeField] private RectTransform headerRect;
        [SerializeField] private RectTransform addFriendCardRect;
        [SerializeField] private RectTransform friendsCardRect;
        [SerializeField] private RectTransform requestsCardRect;
        [SerializeField] private RectTransform friendsScrollRect;
        [SerializeField] private RectTransform requestsScrollRect;
        [SerializeField] private ScrollRect friendsScroll;
        [SerializeField] private ScrollRect requestsScroll;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button addButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text friendsText;
        [SerializeField] private TMP_Text requestsText;
        [SerializeField] private TMP_Text searchText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text friendsSectionTitleText;
        [SerializeField] private TMP_Text friendsCountText;
        [SerializeField] private TMP_Text requestsSectionTitleText;
        [SerializeField] private TMP_Text requestsCountText;
        [SerializeField] private float refreshSeconds = 6f;
        [SerializeField] private float badgePulseSpeed = 5.5f;
        [SerializeField] private float badgePulseScale = 0.16f;
        [SerializeField] private Color badgeColor = new Color(1f, 0.18f, 0.22f, 1f);

        private static Sprite notificationBadgeSprite;
        private RectTransform notificationBadgeRect;
        private Image notificationBadgeImage;
        private Coroutine refreshRoutine;
        private Coroutine searchCoroutine;
        private int searchVersion;
        private bool refreshInFlight;
        private bool friendsContentInitialized;
        private bool requestsContentInitialized;
        private bool friendsWasEmpty = true;
        private bool requestsWereEmpty = true;
        private bool busy;

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                return;

            LayoutPanel();
        }

        private void Awake()
        {
            if (toggleButton == null || panelRoot == null)
                Build(transform);
        }

        private void Update()
        {
            AnimateNotificationBadge();
        }

        public static FriendsUI CreateInScene()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();

            GameObject root = new GameObject("FriendsUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ConfigureRootCanvas(root);

            FriendsUI ui = root.AddComponent<FriendsUI>();
            if (ui.toggleButton == null || ui.panelRoot == null)
                ui.Build(root.transform);

            return ui;
        }

        private void OnEnable()
        {
            EnsureRootCanvas();
            Bind();
            EnsurePanelReferences();
            LayoutToggleButton();
            EnsureToggleBadge();
            LayoutPanel();

            if (FriendsService.I != null)
            {
                FriendsService.I.FriendsChanged += RefreshView;
                FriendsService.I.ErrorChanged += RefreshStatus;
            }

            AppSettings.OnLanguageChanged += OnLanguageChanged;
            RefreshView();
            RefreshStatus(FriendsService.I != null ? FriendsService.I.LastError : string.Empty);
            StartRefreshing();
        }

        private void EnsureRootCanvas()
        {
            ConfigureRootCanvas(gameObject);
        }

        private static void ConfigureRootCanvas(GameObject rootObject)
        {
            if (rootObject == null)
                return;

            RectTransform rect = rootObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = rootObject.AddComponent<RectTransform>();

            if (rect.parent != null)
                rect.SetParent(null, false);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                rootObject.layer = uiLayer;

            Canvas canvas = rootObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = rootObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = RootCanvasSortingOrder;

            CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = rootObject.AddComponent<CanvasScaler>();

            MainLobbyUiCoordinator.ConfigureOverlayScaler(scaler);

            if (rootObject.GetComponent<GraphicRaycaster>() == null)
                rootObject.AddComponent<GraphicRaycaster>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            if (FriendsService.I != null)
            {
                FriendsService.I.FriendsChanged -= RefreshView;
                FriendsService.I.ErrorChanged -= RefreshStatus;
            }

            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            StopRefreshing();
            refreshInFlight = false;
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            if (screenBackdropRoot != null)
                screenBackdropRoot.SetActive(false);
            MainGameLaunchBootstrap.RefreshVisibilityNow();
            Unbind();
        }

        private void Build(Transform parent)
        {
            RectTransform rootRect = parent as RectTransform;
            parent.SetAsLastSibling();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(parent, "FriendsButton", GameLocalization.Text("friends.title"), new Vector2(1f, 0f), new Vector2(-210f, 188f), new Vector2(330f, 93f));

            panelRoot = new GameObject("FriendsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            panelRootRect = panelRoot.transform as RectTransform;
            panelImage = panelRoot.GetComponent<Image>();
            ConfigurePanelRootImage();
            EnsureScreenBackdrop();
            EnsurePanelBackground();
            EnsurePanelFrame();

            headerRect = CreateContainer(panelRoot.transform, "Header");
            titleText = CreateText(headerRect, "Title", GameLocalization.Text("friends.title"), 52f, TextAlignmentOptions.Left);
            titleText.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplySilverTextEffect(titleText);
            subtitleText = CreateText(headerRect, "Subtitle", GameLocalization.Text("friends.subtitle"), 22f, TextAlignmentOptions.Left);
            subtitleText.color = new Color(0.56f, 0.72f, 0.86f, 1f);

            addFriendCardRect = CreateSurface(panelRoot.transform, "AddFriendCard", new Color(0.025f, 0.075f, 0.125f, 0.9f));
            friendsCardRect = CreateSurface(panelRoot.transform, "FriendsCard", new Color(0.018f, 0.055f, 0.098f, 0.88f));
            requestsCardRect = CreateSurface(panelRoot.transform, "RequestsCard", new Color(0.025f, 0.065f, 0.11f, 0.92f));

            closeButton = CreateButton(panelRoot.transform, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(48f, 48f));
            nicknameInput = CreateInput(addFriendCardRect, "NicknameInput", GameLocalization.Text("friends.nickname"), Vector2.zero, Vector2.zero);
            addButton = CreateButton(addFriendCardRect, "AddButton", GameLocalization.Text("friends.add"), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(150f, 50f));
            refreshButton = CreateButton(headerRect, "RefreshButton", GameLocalization.Text("friends.refresh"), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(150f, 50f));

            friendsSectionTitleText = CreateText(friendsCardRect, "SectionTitle", GameLocalization.Text("friends.my_friends"), 31f, TextAlignmentOptions.Left);
            friendsSectionTitleText.fontStyle = FontStyles.Bold;
            friendsSectionTitleText.color = new Color(0.87f, 0.95f, 1f, 1f);
            friendsCountText = CreateText(friendsCardRect, "Count", string.Empty, 20f, TextAlignmentOptions.Right);
            friendsCountText.color = new Color(0.35f, 0.9f, 0.75f, 1f);
            friendsScrollRect = CreateTextScrollView(friendsCardRect, "FriendsScroll", out friendsScroll, out friendsText);

            requestsSectionTitleText = CreateText(requestsCardRect, "SectionTitle", GameLocalization.Text("friends.requests"), 31f, TextAlignmentOptions.Left);
            requestsSectionTitleText.fontStyle = FontStyles.Bold;
            requestsSectionTitleText.color = new Color(0.87f, 0.95f, 1f, 1f);
            requestsCountText = CreateText(requestsCardRect, "Count", string.Empty, 20f, TextAlignmentOptions.Left);
            requestsCountText.color = new Color(0.42f, 0.72f, 1f, 1f);
            requestsScrollRect = CreateTextScrollView(requestsCardRect, "RequestsScroll", out requestsScroll, out requestsText);

            acceptButton = CreateButton(requestsCardRect, "AcceptButton", GameLocalization.Text("friends.accept"), new Vector2(0f, 0f), Vector2.zero, new Vector2(145f, 44f));
            declineButton = CreateButton(requestsCardRect, "DeclineButton", GameLocalization.Text("friends.decline"), new Vector2(0f, 0f), Vector2.zero, new Vector2(145f, 44f));
            ApplyFriendsButtonArt(addButton);
            ApplyFriendsButtonArt(refreshButton);
            ApplyFriendsButtonArt(acceptButton);
            ApplyFriendsButtonArt(declineButton);
            ConfigureButtonTint(addButton, new Color(0.78f, 0.94f, 1f, 1f));
            ConfigureButtonTint(refreshButton, new Color(0.72f, 0.84f, 0.94f, 1f));
            ConfigureButtonTint(acceptButton, new Color(0.72f, 1f, 0.9f, 1f));
            ConfigureButtonTint(declineButton, new Color(0.82f, 0.84f, 0.9f, 1f));

            searchText = CreateText(addFriendCardRect, "SearchText", "", 17f, TextAlignmentOptions.Left);
            searchText.color = new Color(0.82f, 0.9f, 1f, 1f);
            searchText.textWrappingMode = TextWrappingModes.NoWrap;
            searchText.overflowMode = TextOverflowModes.Ellipsis;

            statusText = CreateText(panelRoot.transform, "StatusText", "", 15f, TextAlignmentOptions.Left);
            statusText.color = new Color(1f, 0.72f, 0.38f, 1f);

            LayoutPanel();
            panelRoot.SetActive(false);
            if (screenBackdropRoot != null)
                screenBackdropRoot.SetActive(false);
            Bind();
            LayoutToggleButton();
        }

        private void EnsurePanelReferences()
        {
            if (panelRoot == null)
                return;

            if (panelRootRect == null)
                panelRootRect = panelRoot.transform as RectTransform;

            if (panelImage == null)
                panelImage = panelRoot.GetComponent<Image>();

            ConfigurePanelRootImage();
            EnsureScreenBackdrop();
            EnsurePanelBackground();
            EnsurePanelFrame();
        }

        private void EnsureScreenBackdrop()
        {
            if (panelRoot == null || panelRoot.transform.parent == null)
                return;

            Transform parent = panelRoot.transform.parent;
            if (screenBackdropRoot == null)
            {
                Transform existing = parent.Find("FriendsScreenBackdrop");
                if (existing != null)
                    screenBackdropRoot = existing.gameObject;
            }

            if (screenBackdropRoot == null)
            {
                screenBackdropRoot = new GameObject(
                    "FriendsScreenBackdrop",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                screenBackdropRoot.transform.SetParent(parent, false);
            }

            if (screenBackdropRect == null)
                screenBackdropRect = screenBackdropRoot.transform as RectTransform;
            if (screenBackdropImage == null)
                screenBackdropImage = screenBackdropRoot.GetComponent<Image>();

            SetStretchRect(screenBackdropRect, 0f, 0f, 0f, 0f);
            if (screenBackdropImage != null)
            {
                screenBackdropImage.sprite = null;
                screenBackdropImage.color = Color.black;
                screenBackdropImage.raycastTarget = true;
            }

            screenBackdropRoot.SetActive(panelRoot.activeSelf);
        }

        private void ConfigurePanelRootImage()
        {
            if (panelImage == null)
                return;

            panelImage.sprite = null;
            panelImage.color = new Color(0f, 0f, 0f, 0f);
            panelImage.raycastTarget = true;
        }

        private void EnsurePanelBackground()
        {
            if (panelRoot == null)
                return;

            if (panelBackgroundRect == null)
            {
                Transform existing = panelRoot.transform.Find("PanelBackground");
                if (existing != null)
                {
                    panelBackgroundRect = existing as RectTransform;
                    panelBackgroundImage = existing.GetComponent<Image>();
                }
            }

            if (panelBackgroundRect == null)
            {
                GameObject background = new GameObject("PanelBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.transform.SetParent(panelRoot.transform, false);
                panelBackgroundRect = background.transform as RectTransform;
                panelBackgroundImage = background.GetComponent<Image>();
            }

            if (panelBackgroundImage == null && panelBackgroundRect != null)
                panelBackgroundImage = panelBackgroundRect.GetComponent<Image>();

            if (panelBackgroundImage != null)
            {
                panelBackgroundImage.color = new Color(0f, 0f, 0f, 0f);
                panelBackgroundImage.raycastTarget = false;
            }

            if (panelBackgroundRect != null)
            {
                panelBackgroundRect.gameObject.SetActive(false);
                panelBackgroundRect.SetAsFirstSibling();
            }
        }

        private void EnsurePanelFrame()
        {
            if (panelRoot == null)
                return;

            if (panelFrameRect == null)
            {
                Transform existing = panelRoot.transform.Find("PanelFrame");
                if (existing != null)
                {
                    panelFrameRect = existing as RectTransform;
                    panelFrameImage = existing.GetComponent<Image>();
                }
            }

            if (panelFrameRect == null)
            {
                GameObject frame = new GameObject("PanelFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frame.transform.SetParent(panelRoot.transform, false);
                panelFrameRect = frame.transform as RectTransform;
                panelFrameImage = frame.GetComponent<Image>();
            }

            if (panelFrameImage == null && panelFrameRect != null)
                panelFrameImage = panelFrameRect.GetComponent<Image>();

            if (panelFrameImage != null)
            {
                MainLobbyButtonStyle.ApplyDlsWindow(panelFrameImage);
                panelFrameImage.raycastTarget = false;
            }
        }

        public void LayoutToggleButton()
        {
            MainLobbyUiCoordinator.LayoutRightStackButton(toggleButton, MainLobbySideButtonSlot.Friends);

            ConfigureButtonLabel(toggleButton, 30f, 18f);
            EnsureToggleBadge();
            RefreshToggleBadge();
            MainInfoHintTarget.Detach(toggleButton);
        }

        private void EnsureToggleBadge()
        {
            if (toggleButton == null)
                return;

            if (notificationBadgeRect == null)
            {
                Transform existing = toggleButton.transform.Find("NotificationBadge");
                if (existing != null)
                {
                    notificationBadgeRect = existing as RectTransform;
                    notificationBadgeImage = existing.GetComponent<Image>();
                }
            }

            if (notificationBadgeRect == null)
            {
                GameObject badge = new GameObject("NotificationBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badge.transform.SetParent(toggleButton.transform, false);
                notificationBadgeRect = badge.transform as RectTransform;
                notificationBadgeImage = badge.GetComponent<Image>();
            }

            if (notificationBadgeImage == null && notificationBadgeRect != null)
                notificationBadgeImage = notificationBadgeRect.GetComponent<Image>();

            if (notificationBadgeRect != null)
            {
                notificationBadgeRect.anchorMin = new Vector2(1f, 1f);
                notificationBadgeRect.anchorMax = new Vector2(1f, 1f);
                notificationBadgeRect.pivot = new Vector2(0.5f, 0.5f);
                notificationBadgeRect.anchoredPosition = new Vector2(-24f, -16f);
                notificationBadgeRect.sizeDelta = new Vector2(28f, 28f);
                notificationBadgeRect.SetAsLastSibling();
            }

            if (notificationBadgeImage != null)
            {
                notificationBadgeImage.sprite = GetNotificationBadgeSprite();
                notificationBadgeImage.color = badgeColor;
                notificationBadgeImage.raycastTarget = false;
                notificationBadgeImage.preserveAspect = true;
            }
        }

        private void RefreshToggleBadge()
        {
            EnsureToggleBadge();

            bool hasIncoming = FriendsService.I != null && FriendsService.I.IncomingRequests.Count > 0;
            if (notificationBadgeRect != null)
            {
                notificationBadgeRect.gameObject.SetActive(hasIncoming);
                if (!hasIncoming)
                    notificationBadgeRect.localScale = Vector3.one;
            }

            if (!hasIncoming || notificationBadgeImage == null)
                return;

            notificationBadgeImage.color = badgeColor;
        }

        private void AnimateNotificationBadge()
        {
            if (notificationBadgeRect == null || notificationBadgeImage == null || !notificationBadgeRect.gameObject.activeInHierarchy)
                return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.1f, badgePulseSpeed));
            float scale = 1f + Mathf.Max(0f, badgePulseScale) * pulse;
            notificationBadgeRect.localScale = new Vector3(scale, scale, 1f);

            Color color = badgeColor;
            color.a = Mathf.Lerp(0.45f, 1f, pulse);
            notificationBadgeImage.color = color;
        }

        private static Sprite GetNotificationBadgeSprite()
        {
            if (notificationBadgeSprite != null)
                return notificationBadgeSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "FriendsNotificationBadge";
            texture.hideFlags = HideFlags.HideAndDontSave;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f - 2f;
            float feather = 4f;
            Color transparent = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - distance) / feather);
                    texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : transparent);
                }
            }

            texture.Apply();
            notificationBadgeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            notificationBadgeSprite.hideFlags = HideFlags.HideAndDontSave;
            return notificationBadgeSprite;
        }

        private void LayoutPanel()
        {
            if (panelRoot == null)
                return;

            if (panelRootRect == null)
                panelRootRect = panelRoot.transform as RectTransform;
            if (panelRootRect == null)
                return;

            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null)
                rootRect = panelRootRect.parent as RectTransform;
            if (rootRect == null)
                return;

            float rootWidth = Mathf.Max(480f, rootRect.rect.width);
            float rootHeight = Mathf.Max(360f, rootRect.rect.height);
            Rect safeArea = Screen.safeArea;
            float safeWidthRatio = Screen.width > 0 ? Mathf.Clamp01(safeArea.width / Screen.width) : 1f;
            float safeHeightRatio = Screen.height > 0 ? Mathf.Clamp01(safeArea.height / Screen.height) : 1f;
            float safeCenterRatioX = Screen.width > 0 ? safeArea.center.x / Screen.width - 0.5f : 0f;
            float safeCenterRatioY = Screen.height > 0 ? safeArea.center.y / Screen.height - 0.5f : 0f;
            if (safeWidthRatio < 0.5f || safeHeightRatio < 0.5f)
            {
                safeWidthRatio = 1f;
                safeHeightRatio = 1f;
                safeCenterRatioX = 0f;
                safeCenterRatioY = 0f;
            }
            float panelWidth = rootWidth * safeWidthRatio;
            float panelHeight = rootHeight * safeHeightRatio;

            panelRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRootRect.pivot = new Vector2(0.5f, 0.5f);
            panelRootRect.anchoredPosition = new Vector2(rootWidth * safeCenterRatioX, rootHeight * safeCenterRatioY);
            panelRootRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            EnsurePanelBackground();
            EnsurePanelFrame();
            EnsureScreenBackdrop();

            float insetX = Mathf.Clamp(panelWidth * 0.045f, 52f, 96f);
            float insetTop = Mathf.Clamp(panelHeight * 0.065f, 52f, 72f);
            float insetBottom = Mathf.Clamp(panelHeight * 0.055f, 44f, 64f);
            float contentLeft = insetX + 12f;
            float contentRight = insetX + 12f;
            float contentWidth = panelWidth - contentLeft - contentRight;
            float sectionGap = Mathf.Clamp(panelWidth * 0.01f, 18f, 24f);

            SetStretchRect(panelFrameRect, 0f, 0f, 0f, 0f);
            SetStretchRect(panelBackgroundRect, insetX, insetBottom, -insetX, -insetTop);
            if (panelBackgroundRect != null)
                panelBackgroundRect.gameObject.SetActive(false);

            SetTopStretchRect(headerRect, contentLeft, insetTop + 2f, -contentRight - 122f, 112f);
            SetTopLeftRect(titleText != null ? titleText.rectTransform : null, 0f, 0f, Mathf.Max(420f, contentWidth * 0.5f), 68f);
            SetTopLeftRect(subtitleText != null ? subtitleText.rectTransform : null, 2f, -68f, Mathf.Max(520f, contentWidth * 0.6f), 38f);
            ConfigureTextSize(titleText, 72f, 48f);
            ConfigureTextSize(subtitleText, 34f, 24f);

            SetAnchoredRect(refreshButton != null ? refreshButton.transform as RectTransform : null, new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(380f, 98f));
            ConfigureButtonLabel(refreshButton, 38f, 26f);

            SetAnchoredRect(closeButton != null ? closeButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-66f, -58f), new Vector2(108f, 108f));
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);

            float addCardTop = insetTop + 132f;
            float addCardHeight = 164f;
            SetTopStretchRect(addFriendCardRect, contentLeft, addCardTop, -contentRight, addCardHeight);
            SetTopStretchRect(nicknameInput != null ? nicknameInput.transform as RectTransform : null, 24f, 20f, -424f, 96f);
            ConfigureInputText(nicknameInput, 42f, 28f);
            Image inputImage = nicknameInput != null ? nicknameInput.GetComponent<Image>() : null;
            if (inputImage != null)
                inputImage.color = inputImage.sprite != null ? Color.white : new Color(0.02f, 0.055f, 0.095f, 0.98f);

            SetAnchoredRect(addButton != null ? addButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-202f, -68f), new Vector2(380f, 100f));
            ConfigureButtonLabel(addButton, 40f, 28f);
            SetBottomStretchRect(searchText != null ? searchText.rectTransform : null, 32f, 8f, -424f, 36f);
            ConfigureTextSize(searchText, 28f, 20f);

            float bodyTop = addCardTop + addCardHeight + 24f;
            float statusHeight = 46f;
            float bodyBottom = insetBottom + statusHeight + 10f;
            float bodyHeight = Mathf.Max(240f, panelHeight - bodyTop - bodyBottom);
            float requestsWidth = Mathf.Clamp(contentWidth * 0.39f, 430f, 860f);
            float friendsWidth = contentWidth - requestsWidth - sectionGap;

            SetTopLeftRect(friendsCardRect, contentLeft, -bodyTop, friendsWidth, bodyHeight);
            SetTopLeftRect(requestsCardRect, contentLeft + friendsWidth + sectionGap, -bodyTop, requestsWidth, bodyHeight);

            if (friendsCountText != null)
                friendsCountText.alignment = TextAlignmentOptions.Left;
            SetTopStretchRect(friendsSectionTitleText != null ? friendsSectionTitleText.rectTransform : null, 58f, 34f, -52f, 54f);
            SetTopStretchRect(friendsCountText != null ? friendsCountText.rectTransform : null, 60f, 88f, -52f, 34f);
            SetStretchRect(friendsScrollRect, 56f, 40f, -48f, -136f);
            ConfigureTextSize(friendsSectionTitleText, 46f, 32f);
            ConfigureTextSize(friendsCountText, 31f, 22f);
            ConfigureTextSize(friendsText, 42f, 30f);

            bool hasIncomingRequest = FriendsService.I != null && FriendsService.I.IncomingRequests.Count > 0;
            SetTopStretchRect(requestsSectionTitleText != null ? requestsSectionTitleText.rectTransform : null, 56f, 34f, -48f, 54f);
            SetTopStretchRect(requestsCountText != null ? requestsCountText.rectTransform : null, 58f, 88f, -48f, 34f);
            SetStretchRect(requestsScrollRect, 54f, hasIncomingRequest ? 146f : 40f, -46f, -136f);
            ConfigureTextSize(requestsSectionTitleText, 46f, 32f);
            ConfigureTextSize(requestsCountText, 31f, 22f);
            ConfigureTextSize(requestsText, 40f, 28f);

            float requestButtonWidth = Mathf.Max(180f, (requestsWidth - 78f) * 0.5f);
            SetAnchoredRect(acceptButton != null ? acceptButton.transform as RectTransform : null, new Vector2(0f, 0f), new Vector2(28f + requestButtonWidth * 0.5f, 76f), new Vector2(requestButtonWidth, 92f));
            SetAnchoredRect(declineButton != null ? declineButton.transform as RectTransform : null, new Vector2(0f, 0f), new Vector2(50f + requestButtonWidth * 1.5f, 76f), new Vector2(requestButtonWidth, 92f));
            ConfigureButtonLabel(acceptButton, 34f, 24f);
            ConfigureButtonLabel(declineButton, 34f, 24f);

            SetBottomStretchRect(statusText != null ? statusText.rectTransform : null, contentLeft + 8f, insetBottom - 2f, -contentRight - 8f, statusHeight);
            ConfigureTextSize(statusText, 30f, 22f);

            if (panelFrameRect != null)
                panelFrameRect.SetAsFirstSibling();
            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        private void Bind()
        {
            AddListener(toggleButton, TogglePanel);
            AddListener(closeButton, ClosePanel);
            AddListener(addButton, AddByNickname);
            AddListener(refreshButton, RefreshNow);
            AddListener(acceptButton, AcceptFirstRequest);
            AddListener(declineButton, DeclineFirstRequest);

            if (nicknameInput != null)
            {
                nicknameInput.onSubmit.RemoveListener(AddBySubmit);
                nicknameInput.onSubmit.AddListener(AddBySubmit);
                nicknameInput.onValueChanged.RemoveListener(SearchByNickname);
                nicknameInput.onValueChanged.AddListener(SearchByNickname);
            }
        }

        private void Unbind()
        {
            RemoveListener(toggleButton, TogglePanel);
            RemoveListener(closeButton, ClosePanel);
            RemoveListener(addButton, AddByNickname);
            RemoveListener(refreshButton, RefreshNow);
            RemoveListener(acceptButton, AcceptFirstRequest);
            RemoveListener(declineButton, DeclineFirstRequest);

            if (nicknameInput != null)
            {
                nicknameInput.onSubmit.RemoveListener(AddBySubmit);
                nicknameInput.onValueChanged.RemoveListener(SearchByNickname);
            }
        }

        private void TogglePanel()
        {
            if (panelRoot == null)
                return;

            bool show = !panelRoot.activeSelf;
            if (show && !MainHubStateController.CanOpenMainWindow("Friends"))
            {
                ClosePanel();
                return;
            }

            if (show)
            {
                SettingsMenuUI.ForceCloseAllSettingsMenus();
                MainLobbyUiCoordinator.SetRightStackSuppressed(true);
                SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            }
            transform.SetAsLastSibling();
            EnsureScreenBackdrop();
            if (screenBackdropRoot != null)
                screenBackdropRoot.SetActive(show);
            panelRoot.SetActive(show);
            if (!show)
            {
                MainLobbyUiCoordinator.SetRightStackSuppressed(false);
                SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            }
            MainGameLaunchBootstrap.RefreshVisibilityNow();

            if (show)
            {
                panelRoot.transform.SetAsLastSibling();
                EnsurePanelReferences();
                LayoutPanel();
                RefreshView();
                RefreshNow();

                Transform introParent = screenBackdropRoot != null ? screenBackdropRoot.transform : transform;
                ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(introParent);
                if (intro != null)
                {
                    intro.TryShowForCurrentProfile(
                        "friends",
                        "main.info.friends.title",
                        "main.info.friends.body",
                        "main.intro.friends.white");
                }
            }
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (screenBackdropRoot != null)
                screenBackdropRoot.SetActive(false);
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainHubStateController.NotifyMainWindowClosed();
            MainGameLaunchBootstrap.RefreshVisibilityNow();
        }

        private void StartRefreshing()
        {
            if (refreshRoutine != null)
                StopCoroutine(refreshRoutine);

            refreshRoutine = StartCoroutine(RefreshLoop());
        }

        private void StopRefreshing()
        {
            if (refreshRoutine == null)
                return;

            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                if (FriendsService.I != null)
                    yield return RefreshFromUi();

                yield return new WaitForSecondsRealtime(Mathf.Max(2f, refreshSeconds));
            }
        }

        private void RefreshNow()
        {
            if (FriendsService.I != null && !refreshInFlight)
                StartCoroutine(RefreshFromUi());
        }

        private IEnumerator RefreshFromUi()
        {
            if (refreshInFlight || FriendsService.I == null)
                yield break;

            refreshInFlight = true;
            yield return FriendsService.I.Refresh();
            refreshInFlight = false;
        }

        private void AddBySubmit(string _)
        {
            AddByNickname();
        }

        private void AddByNickname()
        {
            if (busy || FriendsService.I == null || nicknameInput == null)
                return;

            string nickname = nicknameInput.text;
            if (string.IsNullOrWhiteSpace(nickname))
                return;

            StartCoroutine(AddRoutine(nickname));
        }

        private IEnumerator AddRoutine(string nickname)
        {
            busy = true;
            SetButtonsInteractable(false);

            bool ok = false;
            string message = string.Empty;
            yield return FriendsService.I.SendRequestByNickname(nickname, (success, text) =>
            {
                ok = success;
                message = text;
            });

            RefreshStatus(ok ? (string.IsNullOrWhiteSpace(message) ? GameLocalization.Text("friends.request_sent") : message) : message);
            if (ok && nicknameInput != null)
                nicknameInput.text = string.Empty;

            SetButtonsInteractable(true);
            busy = false;
        }

        private void SearchByNickname(string value)
        {
            searchVersion++;
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }

            if (FriendsService.I == null || string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
            {
                if (searchText != null)
                    searchText.text = string.Empty;
                return;
            }

            searchCoroutine = StartCoroutine(SearchRoutine(value.Trim(), searchVersion));
        }

        private IEnumerator SearchRoutine(string nickname, int version)
        {
            yield return new WaitForSecondsRealtime(0.28f);

            FriendsService service = FriendsService.I;
            if (service == null || version != searchVersion)
                yield break;

            bool ok = false;
            FriendsService.FriendUser[] users = null;
            string error = string.Empty;

            yield return service.Search(nickname, (success, message, result) =>
            {
                ok = success;
                error = message;
                users = result;
            });

            if (version != searchVersion || service != FriendsService.I)
                yield break;

            searchCoroutine = null;

            if (!ok)
            {
                if (searchText != null)
                    searchText.text = error;
                yield break;
            }

            StringBuilder builder = new StringBuilder();
            if (users != null)
            {
                int count = Mathf.Min(users.Length, 1);
                for (int i = 0; i < count; i++)
                {
                    FriendsService.FriendUser user = users[i];
                    if (user == null)
                        continue;

                    builder.Append(Escape(AllianceIdentityFormatter.FormatName(user.nickname, user.allianceTag)));
                    if (user.isFriend)
                        builder.Append("  <color=#51D7AA>").Append(GameLocalization.Text("friends.search_friend")).Append("</color>");
                    else if (user.hasPendingOutgoing)
                        builder.Append("  <color=#79A8D8>").Append(GameLocalization.Text("friends.search_requested")).Append("</color>");
                    else if (user.hasPendingIncoming)
                        builder.Append("  <color=#66C8FF>").Append(GameLocalization.Text("friends.search_incoming")).Append("</color>");

                    if (i + 1 < count)
                        builder.Append("    •    ");
                }
            }

            if (searchText != null)
                searchText.text = builder.Length == 0 ? GameLocalization.Text("friends.search_empty") : builder.ToString();
        }

        private void AcceptFirstRequest()
        {
            if (FriendsService.I == null || FriendsService.I.IncomingRequests.Count == 0)
                return;

            StartCoroutine(FriendsService.I.Accept(FriendsService.I.IncomingRequests[0].id, (_, message) => RefreshStatus(message)));
        }

        private void DeclineFirstRequest()
        {
            if (FriendsService.I == null || FriendsService.I.IncomingRequests.Count == 0)
                return;

            StartCoroutine(FriendsService.I.Decline(FriendsService.I.IncomingRequests[0].id, (_, message) => RefreshStatus(message)));
        }

        private void RefreshView()
        {
            if (FriendsService.I == null)
            {
                RefreshToggleBadge();
                return;
            }

            if (panelRoot != null && !panelRoot.activeSelf)
            {
                RefreshToggleBadge();
                return;
            }

            RefreshRequests();
            RefreshFriends();
            RefreshToggleBadge();
        }

        private void RefreshRequests()
        {
            IReadOnlyList<FriendsService.IncomingFriendRequest> incoming = FriendsService.I.IncomingRequests;
            IReadOnlyList<FriendsService.OutgoingFriendRequest> outgoing = FriendsService.I.OutgoingRequests;
            StringBuilder builder = new StringBuilder();
            bool isEmpty = incoming.Count == 0 && outgoing.Count == 0;

            if (isEmpty)
            {
                builder.Append("<b><color=#BFD8EA>")
                    .Append(GameLocalization.Text("friends.requests_empty"))
                    .Append("</color></b>\n<size=78%><color=#718AA2>")
                    .Append(GameLocalization.Text("friends.requests_empty_hint"))
                    .Append("</color></size>");
            }
            else
            {
                for (int i = 0; i < incoming.Count; i++)
                {
                    builder.Append("<color=#55D9B0>●</color>  <b>")
                        .Append(Escape(incoming[i].senderNickname))
                        .Append("</b>\n<size=76%><color=#7FA9C8>")
                        .Append(GameLocalization.Text("friends.request_incoming"))
                        .Append("</color></size>\n\n");
                }

                for (int i = 0; i < outgoing.Count; i++)
                {
                    builder.Append("<color=#559ED9>●</color>  <b>")
                        .Append(Escape(outgoing[i].receiverNickname))
                        .Append("</b>\n<size=76%><color=#718AA2>")
                        .Append(GameLocalization.Text("friends.request_outgoing"))
                        .Append("</color></size>\n\n");
                }
            }

            if (requestsText != null)
            {
                string content = builder.ToString();
                bool stateChanged = !requestsContentInitialized || requestsWereEmpty != isEmpty;
                if (!string.Equals(requestsText.text, content, System.StringComparison.Ordinal))
                    requestsText.text = content;
                if (stateChanged)
                    ConfigureScrollContent(requestsText, isEmpty);
                if (stateChanged && requestsScroll != null)
                    requestsScroll.verticalNormalizedPosition = 1f;
                requestsContentInitialized = true;
                requestsWereEmpty = isEmpty;
            }

            if (requestsCountText != null)
            {
                string countText = string.Format(
                    GameLocalization.Text("friends.requests_count"),
                    incoming.Count,
                    outgoing.Count);
                if (!string.Equals(requestsCountText.text, countText, System.StringComparison.Ordinal))
                    requestsCountText.text = countText;
            }

            bool hasIncoming = incoming.Count > 0;
            if (acceptButton != null)
                acceptButton.gameObject.SetActive(hasIncoming);
            if (declineButton != null)
                declineButton.gameObject.SetActive(hasIncoming);
            SetStretchRect(requestsScrollRect, 54f, hasIncoming ? 146f : 40f, -46f, -136f);
        }

        private void RefreshFriends()
        {
            IReadOnlyList<FriendsService.FriendUser> list = FriendsService.I.Friends;
            StringBuilder output = new StringBuilder();
            StringBuilder onlineRows = new StringBuilder();
            StringBuilder offlineRows = new StringBuilder();
            int onlineCount = 0;
            int visibleCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                FriendsService.FriendUser friend = list[i];
                if (friend == null)
                    continue;

                visibleCount++;
                if (friend.online)
                    onlineCount++;

                AppendFriendRow(friend.online ? onlineRows : offlineRows, friend);
            }

            output.Append(onlineRows);
            output.Append(offlineRows);

            if (output.Length == 0)
            {
                output.Append("<b><color=#BFD8EA>")
                    .Append(GameLocalization.Text("friends.empty_title"))
                    .Append("</color></b>\n<size=78%><color=#718AA2>")
                    .Append(GameLocalization.Text("friends.empty_hint"))
                    .Append("</color></size>");
            }

            if (friendsText != null)
            {
                bool isEmpty = visibleCount == 0;
                string content = output.ToString();
                bool stateChanged = !friendsContentInitialized || friendsWasEmpty != isEmpty;
                if (!string.Equals(friendsText.text, content, System.StringComparison.Ordinal))
                    friendsText.text = content;
                if (stateChanged)
                    ConfigureScrollContent(friendsText, isEmpty);
                if (stateChanged && friendsScroll != null)
                    friendsScroll.verticalNormalizedPosition = 1f;
                friendsContentInitialized = true;
                friendsWasEmpty = isEmpty;
            }

            if (friendsCountText != null)
            {
                string countText = string.Format(
                    GameLocalization.Text("friends.count"),
                    onlineCount,
                    visibleCount);
                if (!string.Equals(friendsCountText.text, countText, System.StringComparison.Ordinal))
                    friendsCountText.text = countText;
            }
        }

        private static void AppendFriendRow(StringBuilder target, FriendsService.FriendUser friend)
        {
            if (target == null || friend == null)
                return;

            string displayName = AllianceIdentityFormatter.FormatName(
                string.IsNullOrWhiteSpace(friend.nickname) ? GameLocalization.Text("common.player") : friend.nickname,
                friend.allianceTag);
            target.Append(friend.online ? "<color=#4FE2AF>●</color>  " : "<color=#526A80>●</color>  ")
                .Append("<b>")
                .Append(Escape(displayName))
                .Append("</b>\n<size=72%><color=")
                .Append(friend.online ? "#62CFAF>" : "#718398>")
                .Append(friend.online ? GameLocalization.Text("friends.online") : GameLocalization.Text("friends.offline"));
            if (!string.IsNullOrWhiteSpace(friend.publicPlayerId))
                target.Append("  •  ID ").Append(Escape(friend.publicPlayerId));
            target.Append("</color></size>\n\n");
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            SetButtonText(toggleButton, GameLocalization.Text("friends.title"));
            SetButtonText(addButton, GameLocalization.Text("friends.add"));
            SetButtonText(refreshButton, GameLocalization.Text("friends.refresh"));
            SetButtonText(acceptButton, GameLocalization.Text("friends.accept"));
            SetButtonText(declineButton, GameLocalization.Text("friends.decline"));

            if (titleText != null)
                titleText.text = GameLocalization.Text("friends.title");
            if (subtitleText != null)
                subtitleText.text = GameLocalization.Text("friends.subtitle");
            if (friendsSectionTitleText != null)
                friendsSectionTitleText.text = GameLocalization.Text("friends.my_friends");
            if (requestsSectionTitleText != null)
                requestsSectionTitleText.text = GameLocalization.Text("friends.requests");

            if (nicknameInput != null && nicknameInput.placeholder is TMP_Text placeholder)
                placeholder.text = GameLocalization.Text("friends.nickname");

            SearchByNickname(string.Empty);

            RefreshView();
            LayoutPanel();
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }

        private static void ConfigureButtonLabel(Button button, float maxSize, float minSize)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            label.fontSize = maxSize;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = minSize;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.margin = new Vector4(20f, 4f, 20f, 6f);
        }

        private static void ConfigureTextSize(TMP_Text text, float maxSize, float minSize)
        {
            if (text == null)
                return;

            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
        }

        private static void ConfigureInputText(TMP_InputField input, float maxSize, float minSize)
        {
            if (input == null)
                return;

            ConfigureTextSize(input.textComponent, maxSize, minSize);
            if (input.placeholder is TMP_Text placeholder)
                ConfigureTextSize(placeholder, maxSize, minSize);
        }

        private void RefreshStatus(string value)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        private void SetButtonsInteractable(bool value)
        {
            if (addButton != null)
                addButton.interactable = value;
            if (refreshButton != null)
                refreshButton.interactable = value;
        }

        private static RectTransform CreateContainer(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.transform as RectTransform;
        }

        private static RectTransform CreateSurface(Transform parent, string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);

            Image image = root.GetComponent<Image>();
            bool isAddFriendBar = string.Equals(name, "AddFriendCard", System.StringComparison.Ordinal);
            image.color = isAddFriendBar ? Color.clear : color;
            image.raycastTarget = false;

            if (isAddFriendBar)
                return root.transform as RectTransform;

            GameObject frame = new GameObject("ArtFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            RectTransform frameRect = frame.transform as RectTransform;
            SetStretchRect(frameRect, -5f, -5f, 5f, 5f);
            Image frameImage = frame.GetComponent<Image>();
            frameImage.sprite = LoadFriendsPanelFrameSprite();
            frameImage.type = frameImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frameImage.preserveAspect = false;
            frameImage.pixelsPerUnitMultiplier = 1f;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;

            return root.transform as RectTransform;
        }

        private static RectTransform CreateTextScrollView(Transform parent, string name, out ScrollRect scroll, out TMP_Text text)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            root.transform.SetParent(parent, false);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.transform as RectTransform;
            SetStretchRect(viewportRect, 0f, 0f, 0f, 0f);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            text = CreateText(viewport.transform, "Content", string.Empty, 25f, TextAlignmentOptions.TopLeft);
            RectTransform contentRect = text.rectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(4f, 0f);
            contentRect.offsetMax = new Vector2(-14f, 0f);
            contentRect.sizeDelta = new Vector2(-18f, 0f);
            text.margin = new Vector4(4f, 8f, 12f, 12f);
            text.overflowMode = TextOverflowModes.Overflow;

            ContentSizeFitter fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = root.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.14f;
            scroll.scrollSensitivity = 38f;

            return root.transform as RectTransform;
        }

        private static void ConfigureScrollContent(TMP_Text text, bool empty)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = !empty;

            if (empty)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(10f, 10f);
                rect.offsetMax = new Vector2(-10f, -10f);
                text.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(4f, 0f);
                rect.offsetMax = new Vector2(-14f, 0f);
                rect.sizeDelta = new Vector2(-18f, 0f);
                text.alignment = TextAlignmentOptions.TopLeft;
            }

            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        private static void ConfigureButtonTint(Button button, Color normalColor)
        {
            if (button == null)
                return;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.28f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.22f);
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.38f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void ApplyFriendsButtonArt(Button button)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = LoadFriendsButtonSprite();
            if (sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Sliced;
            button.image.preserveAspect = false;
            button.image.pixelsPerUnitMultiplier = 1f;
            button.image.color = Color.white;
        }

        private static Sprite LoadFriendsPanelFrameSprite()
        {
            if (friendsPanelFrameSprite == null)
                friendsPanelFrameSprite = Resources.Load<Sprite>(FriendsPanelFrameResourcePath);
            return friendsPanelFrameSprite;
        }

        private static Sprite LoadFriendsButtonSprite()
        {
            if (friendsButtonSprite == null)
                friendsButtonSprite = Resources.Load<Sprite>(FriendsButtonResourcePath);
            return friendsButtonSprite;
        }

        private static Sprite LoadFriendsInputSprite()
        {
            if (friendsInputSprite == null)
                friendsInputSprite = Resources.Load<Sprite>(FriendsInputResourcePath);
            return friendsInputSprite;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholderText, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            SetOffsets(rect, offsetMin, offsetMax, new Vector2(0f, 1f), new Vector2(1f, 1f));
            Image image = root.GetComponent<Image>();
            image.sprite = LoadFriendsInputSprite();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = image.sprite != null ? Color.white : new Color(0.055f, 0.08f, 0.14f, 0.97f);

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(68f, 12f);
            textAreaRect.offsetMax = new Vector2(-68f, -12f);

            TMP_Text text = CreateText(textArea.transform, "Text", "", 20f, TextAlignmentOptions.Left);
            SetOffsets(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_Text placeholder = CreateText(textArea.transform, "Placeholder", placeholderText, 20f, TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            SetOffsets(placeholder.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 32;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.54f, 0.96f);

            TMP_Text text = CreateText(root.transform, "Label", label, 21f, TextAlignmentOptions.Center);
            SetOffsets(text.rectTransform, new Vector2(10f, 3f), new Vector2(-10f, -4f), Vector2.zero, Vector2.one);
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 19f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.margin = Vector4.zero;
            Button button = root.GetComponent<Button>();
            MainLobbyButtonStyle.Apply(button);
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);

            TMP_Text text = root.GetComponent<TMP_Text>();
            text.text = value;
            MainLobbyButtonStyle.ApplyFont(text);
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(10f, fontSize * 0.6f);
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetTopLeftRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetStretchRect(RectTransform rect, float left, float bottom, float right, float top)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void SetTopStretchRect(RectTransform rect, float left, float top, float right, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(right, -top);
        }

        private static void SetBottomStretchRect(RectTransform rect, float left, float bottom, float right, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, bottom + height);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
