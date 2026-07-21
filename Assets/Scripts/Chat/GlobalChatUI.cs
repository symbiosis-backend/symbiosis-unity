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
    public sealed class GlobalChatUI : MonoBehaviour, IPointerClickHandler
    {
        private const int RootCanvasSortingOrder = 30029;
        private const int RoundedSurfaceTextureSize = 72;
        private const int RoundedSurfaceRadius = 22;
        private static Sprite roundedSurfaceSprite;

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelRootRect;
        [SerializeField] private Image panelImage;
        [SerializeField] private RectTransform panelBackgroundRect;
        [SerializeField] private Image panelBackgroundImage;
        [SerializeField] private RectTransform panelFrameRect;
        [SerializeField] private Image panelFrameImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button globalChannelButton;
        [SerializeField] private Button mahjongChannelButton;
        [SerializeField] private Button developerSupportChannelButton;
        [SerializeField] private Button autoTranslateButton;
        [SerializeField] private Image toggleUnreadDot;
        [SerializeField] private Image developerSupportUnreadDot;
        [SerializeField] private TMP_Text messagesText;
        [SerializeField] private RectTransform developerSupportContentRect;
        [SerializeField] private TMP_Text developerSupportEmptyText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button reportButton;
        [SerializeField] private Button blockButton;
        [SerializeField] private Button cancelActionButton;
        [SerializeField] private GameObject actionMenuRoot;
        [SerializeField] private RectTransform actionMenuRect;
        [SerializeField] private TMP_Text actionMenuTitleText;
        [SerializeField] private TMP_Text actionMenuProfileText;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float refreshSeconds = 2.5f;
        private ChatFirstVisitDialogueUI firstVisitDialogue;

        [SerializeField] private GameObject supportActionMenuRoot;
        [SerializeField] private RectTransform supportActionMenuRect;
        [SerializeField] private TMP_Text supportActionTitleText;
        [SerializeField] private Button supportConfirmedButton;
        [SerializeField] private Button supportUnderReviewButton;
        [SerializeField] private Button supportRejectedButton;
        [SerializeField] private Button supportClosedButton;
        [SerializeField] private Button supportVotingButton;
        [SerializeField] private TMP_InputField supportCommentInput;
        [SerializeField] private Button supportCommentSendButton;
        [SerializeField] private Button supportActionCloseButton;

        private RectTransform messagesViewportRect;
        private RectTransform messagesContentRect;
        private Coroutine refreshRoutine;
        private GlobalChatService.GlobalChatMessage selectedActionMessage;
        private Vector2 actionMenuAnchoredPosition;
        private bool hasActionMenuAnchoredPosition;
        private bool sending;
        private long lastSupportTapMessageId;
        private float lastSupportTapTime = -10f;
        private int lastSupportTapPointerId = int.MinValue;
        private Vector2 lastSupportTapPosition;
        private bool loadingOlderSupport;
        private readonly Dictionary<long, DeveloperSupportRequestCardUI> developerSupportCards = new Dictionary<long, DeveloperSupportRequestCardUI>();
        private readonly Stack<DeveloperSupportRequestCardUI> developerSupportCardPool = new Stack<DeveloperSupportRequestCardUI>();
        private readonly HashSet<long> developerSupportSeenIds = new HashSet<long>();
        private readonly HashSet<long> developerSupportVotePendingIds = new HashSet<long>();
        private readonly List<long> developerSupportRemovalBuffer = new List<long>();

        private const float SupportDoubleTapSeconds = 0.45f;

        public bool AutoTranslationEnabled => AppSettings.I == null || AppSettings.I.ChatAutoTranslateEnabled;

        private void Awake()
        {
            if (toggleButton == null || panelRoot == null)
                Build(transform);
        }

        private void OnRectTransformDimensionsChange()
        {
            LayoutChatPanel();
            if (isActiveAndEnabled)
                RefreshMessages();
        }

        public static GlobalChatUI CreateInScene()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();

            GameObject root = new GameObject("GlobalChatUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ConfigureRootCanvas(root);

            GlobalChatUI ui = root.AddComponent<GlobalChatUI>();
            if (ui.toggleButton == null || ui.panelRoot == null)
                ui.Build(root.transform);

            return ui;
        }

        private void OnEnable()
        {
            EnsureRootCanvas();
            EnsurePanelReferences();
            Bind();
            LayoutToggleButton();
            LayoutChatPanel();
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            AppSettings.OnChatAutoTranslateChanged += OnChatAutoTranslateChanged;

            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged += RefreshMessages;
                GlobalChatService.I.ErrorChanged += RefreshStatus;
                GlobalChatService.I.DeveloperSupportUnreadChanged += OnDeveloperSupportUnreadChanged;
            }

            EnsureDeveloperSupportNotificationDots();
            RefreshLocalization();
            RefreshMessages();
            RefreshStatus(GlobalChatService.I != null ? GlobalChatService.I.LastError : string.Empty);
            RefreshDeveloperSupportNotificationDots();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            AppSettings.OnChatAutoTranslateChanged -= OnChatAutoTranslateChanged;

            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged -= RefreshMessages;
                GlobalChatService.I.ErrorChanged -= RefreshStatus;
                GlobalChatService.I.DeveloperSupportUnreadChanged -= OnDeveloperSupportUnreadChanged;
            }

            StopRefreshing();
            if (firstVisitDialogue != null)
                firstVisitDialogue.HideWithoutCompleting();
            HideActionMenu();
            HideSupportActionMenu();
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainGameLaunchBootstrap.RefreshVisibilityNow();
            Unbind();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalization();
            RefreshMessages();
            if (GlobalChatService.I != null && isActiveAndEnabled)
                StartCoroutine(GlobalChatService.I.RefreshForLanguageChange());
        }

        private void OnChatAutoTranslateChanged(bool enabled)
        {
            RefreshAutoTranslateButton();
            RefreshMessages();
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

        private void EnsurePanelReferences()
        {
            if (panelRoot == null)
                return;

            if (panelRootRect == null)
                panelRootRect = panelRoot.transform as RectTransform;

            if (panelImage == null)
                panelImage = panelRoot.GetComponent<Image>();

            ConfigurePanelRootImage();

            EnsurePanelBackground();
            EnsurePanelFrame();

            Transform viewport = panelRoot.transform.Find("MessagesViewport");
            if (messagesViewportRect == null && viewport != null)
                messagesViewportRect = viewport as RectTransform;

            if (scrollRect == null && viewport != null)
                scrollRect = viewport.GetComponent<ScrollRect>();

            if (messagesContentRect == null && scrollRect != null)
                messagesContentRect = scrollRect.content;

            if (messagesContentRect == null && viewport != null)
            {
                Transform content = viewport.Find("MessagesContent");
                if (content != null)
                    messagesContentRect = content as RectTransform;
            }

            EnsureDeveloperSupportContent(viewport);
            EnsureAutoTranslateButton();
        }

        private void EnsureAutoTranslateButton()
        {
            if (panelRoot == null)
                return;

            if (autoTranslateButton == null)
            {
                Transform existing = panelRoot.transform.Find("AutoTranslateButton");
                if (existing != null)
                    autoTranslateButton = existing.GetComponent<Button>();
            }

            if (autoTranslateButton == null)
            {
                autoTranslateButton = CreateButton(
                    panelRoot.transform,
                    "AutoTranslateButton",
                    string.Empty,
                    new Vector2(1f, 1f),
                    new Vector2(-250f, -92f),
                    new Vector2(260f, 58f));
            }

            RefreshAutoTranslateButton();
        }

        private void EnsureDeveloperSupportContent(Transform viewport)
        {
            if (viewport == null)
                return;

            if (developerSupportContentRect == null)
            {
                Transform existing = viewport.Find("DeveloperSupportContent");
                if (existing != null)
                    developerSupportContentRect = existing as RectTransform;
            }

            if (developerSupportContentRect == null)
            {
                GameObject content = new GameObject(
                    "DeveloperSupportContent",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter));
                content.transform.SetParent(viewport, false);
                developerSupportContentRect = content.transform as RectTransform;
                developerSupportContentRect.anchorMin = new Vector2(0f, 1f);
                developerSupportContentRect.anchorMax = new Vector2(1f, 1f);
                developerSupportContentRect.pivot = new Vector2(0.5f, 1f);
                developerSupportContentRect.anchoredPosition = new Vector2(0f, -18f);
                developerSupportContentRect.sizeDelta = new Vector2(-36f, 0f);

                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(2, 2, 2, 20);
                layout.spacing = 16f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (developerSupportEmptyText == null)
            {
                Transform existingEmpty = developerSupportContentRect.Find("EmptyText");
                if (existingEmpty != null)
                    developerSupportEmptyText = existingEmpty.GetComponent<TMP_Text>();
            }

            if (developerSupportEmptyText == null)
            {
                developerSupportEmptyText = CreateText(
                    developerSupportContentRect,
                    "EmptyText",
                    GameLocalization.Text("chat.support.empty"),
                    22f,
                    TextAlignmentOptions.Center);
                developerSupportEmptyText.color = new Color(0.62f, 0.74f, 0.82f, 1f);
                developerSupportEmptyText.raycastTarget = false;
                LayoutElement emptyLayout = developerSupportEmptyText.gameObject.AddComponent<LayoutElement>();
                emptyLayout.minHeight = 84f;
                emptyLayout.preferredHeight = 84f;
            }

        }

        private void Build(Transform parent)
        {
            RectTransform rootRect = parent as RectTransform;
            parent.SetAsLastSibling();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(parent, "ChatButton", GameLocalization.Text("chat.title"), new Vector2(1f, 0f), new Vector2(-210f, 76f), new Vector2(330f, 93f));

            panelRoot = new GameObject("ChatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            panelRootRect = panelRoot.transform as RectTransform;

            panelImage = panelRoot.GetComponent<Image>();
            ConfigurePanelRootImage();

            EnsurePanelBackground();
            EnsurePanelFrame();

            titleText = CreateText(panelRoot.transform, "Title", GameLocalization.Text("chat.title"), 28f, TextAlignmentOptions.Left);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(22f, -58f);
            titleRect.offsetMax = new Vector2(-82f, -14f);

            closeButton = CreateButton(panelRoot.transform, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(48f, 48f));
            globalChannelButton = CreateButton(panelRoot.transform, "GlobalChannelButton", "Global", new Vector2(0f, 1f), new Vector2(78f, -92f), new Vector2(118f, 42f));
            mahjongChannelButton = CreateButton(panelRoot.transform, "MahjongChannelButton", "Mahjong", new Vector2(0f, 1f), new Vector2(208f, -92f), new Vector2(136f, 42f));
            developerSupportChannelButton = CreateButton(panelRoot.transform, "DeveloperSupportChannelButton", "Support", new Vector2(0f, 1f), new Vector2(370f, -92f), new Vector2(170f, 42f));
            autoTranslateButton = CreateButton(panelRoot.transform, "AutoTranslateButton", string.Empty, new Vector2(1f, 1f), new Vector2(-250f, -92f), new Vector2(260f, 58f));
            EnsureDeveloperSupportNotificationDots();

            GameObject viewport = new GameObject("MessagesViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(panelRoot.transform, false);
            messagesViewportRect = viewport.transform as RectTransform;
            messagesViewportRect.anchorMin = new Vector2(0f, 0f);
            messagesViewportRect.anchorMax = new Vector2(1f, 1f);
            messagesViewportRect.offsetMin = new Vector2(18f, 104f);
            messagesViewportRect.offsetMax = new Vector2(-18f, -122f);
            viewport.GetComponent<Image>().color = new Color(0.01f, 0.018f, 0.032f, 0.78f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;

            GameObject content = new GameObject("MessagesContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            messagesContentRect = content.transform as RectTransform;
            messagesContentRect.anchorMin = new Vector2(0f, 0f);
            messagesContentRect.anchorMax = new Vector2(1f, 1f);
            messagesContentRect.offsetMin = new Vector2(12f, 12f);
            messagesContentRect.offsetMax = new Vector2(-12f, -12f);

            messagesText = CreateText(content.transform, "MessagesText", "", 19f, TextAlignmentOptions.BottomLeft);
            RectTransform messagesRect = messagesText.rectTransform;
            messagesRect.anchorMin = Vector2.zero;
            messagesRect.anchorMax = Vector2.one;
            messagesRect.offsetMin = Vector2.zero;
            messagesRect.offsetMax = Vector2.zero;
            messagesText.enableAutoSizing = true;
            messagesText.fontSizeMin = 13f;
            messagesText.fontSizeMax = 20f;
            messagesText.textWrappingMode = TextWrappingModes.Normal;
            messagesText.overflowMode = TextOverflowModes.Truncate;
            messagesText.raycastTarget = true;

            EnsureDeveloperSupportContent(viewport.transform);
            developerSupportContentRect.gameObject.SetActive(false);

            scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = messagesViewportRect;
            scrollRect.content = messagesContentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            inputField = CreateInput(panelRoot.transform);
            sendButton = CreateButton(panelRoot.transform, "SendButton", GameLocalization.Text("chat.send"), new Vector2(1f, 0f), new Vector2(-62f, 52f), new Vector2(104f, 48f));
            CreateActionMenu(panelRoot.transform);
            CreateSupportActionMenu(panelRoot.transform);

            statusText = CreateText(panelRoot.transform, "StatusText", "", 15f, TextAlignmentOptions.Left);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.offsetMin = new Vector2(22f, 8f);
            statusRect.offsetMax = new Vector2(-22f, 32f);
            statusText.color = new Color(1f, 0.68f, 0.34f, 1f);

            LayoutChatPanel();
            panelRoot.SetActive(false);
            HideActionMenu();
            HideSupportActionMenu();
            Bind();
            RefreshLocalization();
        }

        private void CreateActionMenu(Transform parent)
        {
            actionMenuRoot = new GameObject("ChatActionMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            actionMenuRoot.transform.SetParent(parent, false);
            actionMenuRect = actionMenuRoot.transform as RectTransform;

            Image background = actionMenuRoot.GetComponent<Image>();
            background.color = new Color(0.012f, 0.034f, 0.064f, 0.97f);
            Outline outline = actionMenuRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.66f, 0.90f, 0.86f);
            outline.effectDistance = new Vector2(1f, -1f);

            actionMenuTitleText = CreateText(actionMenuRoot.transform, "ActionTitle", "", 19f, TextAlignmentOptions.Left);
            SetTopStretchRect(actionMenuTitleText.rectTransform, 30f, 14f, -30f, 46f);

            actionMenuProfileText = CreateText(actionMenuRoot.transform, "ProfileInfo", "", 17f, TextAlignmentOptions.Left);
            SetTopStretchRect(actionMenuProfileText.rectTransform, 30f, 66f, -30f, 58f);

            reportButton = CreateButton(actionMenuRoot.transform, "ReportButton", GameLocalization.Text("chat.report"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 70f));
            blockButton = CreateButton(actionMenuRoot.transform, "BlockButton", GameLocalization.Text("chat.block"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 70f));
            cancelActionButton = CreateButton(actionMenuRoot.transform, "CancelButton", GameLocalization.Text("settings.close"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 70f));
        }

        private void CreateSupportActionMenu(Transform parent)
        {
            supportActionMenuRoot = new GameObject("DeveloperSupportActionMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            supportActionMenuRoot.transform.SetParent(parent, false);
            supportActionMenuRect = supportActionMenuRoot.transform as RectTransform;

            Image background = supportActionMenuRoot.GetComponent<Image>();
            background.color = new Color(0.012f, 0.034f, 0.064f, 0.99f);
            ApplyRoundedSurface(background);
            ConfigureOutline(supportActionMenuRoot, new Color(0.55f, 0.78f, 1f, 0.92f), new Vector2(2f, -2f));

            supportActionTitleText = CreateText(supportActionMenuRoot.transform, "SupportActionTitle", "", 24f, TextAlignmentOptions.Left);
            supportActionTitleText.textWrappingMode = TextWrappingModes.Normal;
            supportActionTitleText.overflowMode = TextOverflowModes.Ellipsis;

            supportConfirmedButton = CreateButton(supportActionMenuRoot.transform, "ConfirmedButton", "", Vector2.zero, Vector2.zero, new Vector2(220f, 64f));
            supportUnderReviewButton = CreateButton(supportActionMenuRoot.transform, "UnderReviewButton", "", Vector2.zero, Vector2.zero, new Vector2(220f, 64f));
            supportRejectedButton = CreateButton(supportActionMenuRoot.transform, "RejectedButton", "", Vector2.zero, Vector2.zero, new Vector2(220f, 64f));
            supportClosedButton = CreateButton(supportActionMenuRoot.transform, "ClosedButton", "", Vector2.zero, Vector2.zero, new Vector2(220f, 64f));
            supportVotingButton = CreateButton(supportActionMenuRoot.transform, "VotingButton", "", Vector2.zero, Vector2.zero, new Vector2(220f, 64f));

            supportCommentInput = CreateInput(supportActionMenuRoot.transform);
            supportCommentInput.gameObject.name = "DeveloperCommentInput";
            supportCommentInput.characterLimit = 800;
            supportCommentInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            supportCommentSendButton = CreateButton(supportActionMenuRoot.transform, "DeveloperCommentSendButton", "", Vector2.zero, Vector2.zero, new Vector2(190f, 84f));
            supportActionCloseButton = CreateButton(supportActionMenuRoot.transform, "SupportActionCloseButton", "X", Vector2.zero, Vector2.zero, new Vector2(58f, 58f));
            MainLobbyButtonStyle.ApplyCloseIconButton(supportActionCloseButton);
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
                background.transform.SetAsFirstSibling();
                panelBackgroundRect = background.transform as RectTransform;
                panelBackgroundImage = background.GetComponent<Image>();
            }

            if (panelBackgroundImage == null && panelBackgroundRect != null)
                panelBackgroundImage = panelBackgroundRect.GetComponent<Image>();

            if (panelBackgroundImage != null)
            {
                ApplyRoundedSurface(panelBackgroundImage);
                panelBackgroundImage.color = new Color(0.008f, 0.018f, 0.032f, 1f);
                panelBackgroundImage.raycastTarget = false;
            }

            if (panelBackgroundRect != null)
            {
                panelBackgroundRect.gameObject.SetActive(true);
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

            if (panelFrameRect != null)
                panelFrameRect.SetAsFirstSibling();
        }

        private void ConfigurePanelRootImage()
        {
            if (panelImage == null)
                return;

            panelImage.sprite = null;
            panelImage.color = Color.black;
            panelImage.raycastTarget = true;
        }

        private void LayoutChatPanel()
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

            float rootWidth = Mathf.Max(960f, rootRect.rect.width);
            float rootHeight = Mathf.Max(540f, rootRect.rect.height);
            float panelWidth = rootWidth;
            float panelHeight = rootHeight;

            panelRootRect.anchorMin = Vector2.zero;
            panelRootRect.anchorMax = Vector2.one;
            panelRootRect.pivot = new Vector2(0.5f, 0.5f);
            panelRootRect.anchoredPosition = Vector2.zero;
            panelRootRect.sizeDelta = Vector2.zero;
            panelRootRect.offsetMin = Vector2.zero;
            panelRootRect.offsetMax = Vector2.zero;

            EnsurePanelBackground();
            EnsurePanelFrame();

            float frameMarginX = Mathf.Clamp(panelWidth * 0.012f, 12f, 28f);
            float frameMarginY = Mathf.Clamp(panelHeight * 0.018f, 10f, 22f);
            float insetX = Mathf.Clamp(panelWidth * 0.068f, 74f, 132f);
            float insetTop = Mathf.Clamp(panelHeight * 0.13f, 84f, 132f);
            float insetBottom = Mathf.Clamp(panelHeight * 0.105f, 66f, 108f);
            Vector4 safeInsets = GetSafeAreaInsets(rootRect);
            float contentLeft = Mathf.Max(insetX, safeInsets.x + 28f);
            float contentRight = Mathf.Max(insetX, safeInsets.z + 28f);
            insetTop = Mathf.Max(insetTop, safeInsets.w + 24f);
            insetBottom = Mathf.Max(insetBottom, safeInsets.y + 24f);
            float headerHeight = Mathf.Clamp(panelHeight * 0.09f, 58f, 90f);
            float inputHeight = Mathf.Clamp(panelHeight * 0.095f, 64f, 96f);
            float channelWidth = Mathf.Clamp(panelWidth * 0.13f, 178f, 250f);
            float channelGap = Mathf.Clamp(panelWidth * 0.014f, 18f, 28f);
            float autoTranslateWidth = Mathf.Clamp(panelWidth * 0.16f, 200f, 310f);
            float channelY = -insetTop - headerHeight * 0.5f;
            float messageTop = insetTop + headerHeight + 20f;
            float inputBottom = insetBottom + 18f;
            float messageBottom = inputBottom + inputHeight + 34f;

            SetStretchRect(panelFrameRect, frameMarginX, frameMarginY, -frameMarginX, -frameMarginY);
            SetStretchRect(panelBackgroundRect, contentLeft - 18f, insetBottom - 4f, -contentRight + 18f, -insetTop + 10f);

            if (titleText != null)
            {
                titleText.text = GameLocalization.Text("chat.title");
                titleText.gameObject.SetActive(true);
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(0.88f, 0.96f, 1f, 1f);
                titleText.characterSpacing = 1.5f;
                float titleTop = Mathf.Max(frameMarginY + 8f, safeInsets.w + 8f);
                SetTopStretchRect(titleText.rectTransform, panelWidth * 0.30f, titleTop, -panelWidth * 0.30f, headerHeight - 8f);
                ConfigureTextSize(titleText, 40f, 24f);
            }

            float closeSize = Mathf.Clamp(headerHeight * 0.88f, 58f, 78f);
            float closeMarginX = Mathf.Max(frameMarginX + 16f, safeInsets.z + 18f);
            float closeMarginY = Mathf.Max(frameMarginY + 14f, safeInsets.w + 18f);
            SetAnchoredRect(
                closeButton != null ? closeButton.transform as RectTransform : null,
                new Vector2(1f, 1f),
                new Vector2(-closeMarginX - closeSize * 0.5f, -closeMarginY - closeSize * 0.5f),
                new Vector2(closeSize, closeSize));
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            SetAnchoredRect(globalChannelButton != null ? globalChannelButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(contentLeft + channelWidth * 0.5f, channelY), new Vector2(channelWidth, headerHeight));
            SetAnchoredRect(mahjongChannelButton != null ? mahjongChannelButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(contentLeft + channelWidth * 1.5f + channelGap, channelY), new Vector2(channelWidth, headerHeight));
            SetAnchoredRect(developerSupportChannelButton != null ? developerSupportChannelButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(contentLeft + channelWidth * 2.5f + channelGap * 2f, channelY), new Vector2(channelWidth, headerHeight));
            SetAnchoredRect(autoTranslateButton != null ? autoTranslateButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-contentRight - autoTranslateWidth * 0.5f, channelY), new Vector2(autoTranslateWidth, headerHeight));
            ConfigureButtonLabel(globalChannelButton, 30f, 18f);
            ConfigureButtonLabel(mahjongChannelButton, 30f, 18f);
            ConfigureButtonLabel(developerSupportChannelButton, 28f, 16f);
            ConfigureButtonLabel(autoTranslateButton, 26f, 15f);
            LayoutActionMenu(Mathf.Max(contentLeft, contentRight), messageTop, panelWidth, panelHeight);
            LayoutSupportActionMenu(panelWidth, panelHeight);

            SetStretchRect(messagesViewportRect, contentLeft, messageBottom, -contentRight, -messageTop);
            Image viewportImage = messagesViewportRect != null ? messagesViewportRect.GetComponent<Image>() : null;
            if (viewportImage != null)
            {
                ApplyRoundedSurface(viewportImage);
                viewportImage.color = new Color(0.009f, 0.027f, 0.046f, 1f);
                ConfigureOutline(viewportImage.gameObject, new Color(0.22f, 0.60f, 0.82f, 0.46f), new Vector2(1.5f, -1.5f));
            }
            ConfigureTextSize(messagesText, 35f, 23f);
            if (messagesText != null)
            {
                messagesText.alignment = TextAlignmentOptions.TopLeft;
                messagesText.lineSpacing = 13f;
                messagesText.color = new Color(0.90f, 0.95f, 0.98f, 1f);
            }

            RectTransform inputRect = inputField != null ? inputField.transform as RectTransform : null;
            float sendWidth = Mathf.Clamp(panelWidth * 0.13f, 190f, 250f);
            float composeGap = Mathf.Clamp(panelWidth * 0.015f, 18f, 30f);
            SetBottomStretchRect(inputRect, contentLeft, inputBottom, -contentRight - sendWidth - composeGap, inputHeight);
            Image inputImage = inputField != null ? inputField.GetComponent<Image>() : null;
            if (inputImage != null)
            {
                ApplyRoundedSurface(inputImage);
                inputImage.color = new Color(0.025f, 0.090f, 0.145f, 1f);
                ConfigureOutline(inputImage.gameObject, new Color(0.30f, 0.72f, 0.96f, 0.92f), new Vector2(2f, -2f));
            }
            ConfigureInputText(inputField, 32f, 21f);
            if (inputField != null)
            {
                inputField.customCaretColor = true;
                inputField.caretColor = new Color(0.48f, 0.86f, 1f, 1f);
                inputField.selectionColor = new Color(0.20f, 0.58f, 0.82f, 0.55f);
                if (inputField.placeholder is TMP_Text placeholder)
                    placeholder.color = new Color(0.76f, 0.86f, 0.93f, 0.92f);
            }

            SetAnchoredRect(sendButton != null ? sendButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-contentRight - sendWidth * 0.5f, inputBottom + inputHeight * 0.5f), new Vector2(sendWidth, inputHeight));
            ConfigureButtonLabel(sendButton, 30f, 19f);
            if (sendButton != null)
                ConfigureOutline(sendButton.gameObject, new Color(0.26f, 0.68f, 0.92f, 0.56f), new Vector2(1.5f, -1.5f));
            SetBottomStretchRect(statusText != null ? statusText.rectTransform : null, contentLeft, insetBottom - 26f, -contentRight, 34f);
            ConfigureTextSize(statusText, 22f, 14f);

            if (inputField != null)
                inputField.transform.SetAsLastSibling();
            if (sendButton != null)
                sendButton.transform.SetAsLastSibling();
            if (actionMenuRoot != null)
                actionMenuRoot.transform.SetAsLastSibling();
            if (supportActionMenuRoot != null)
                supportActionMenuRoot.transform.SetAsLastSibling();
            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        private void LayoutActionMenu(float insetX, float messageTop, float panelWidth, float panelHeight)
        {
            if (actionMenuRect == null)
                return;

            Vector2 menuSize = new Vector2(760f, 286f);
            Vector2 fallback = new Vector2(panelWidth * 0.5f - insetX - menuSize.x * 0.5f, panelHeight * 0.5f - messageTop - menuSize.y * 0.5f - 28f);
            Vector2 anchored = hasActionMenuAnchoredPosition ? actionMenuAnchoredPosition : fallback;
            float minX = -panelWidth * 0.5f + insetX + menuSize.x * 0.5f;
            float maxX = panelWidth * 0.5f - insetX - menuSize.x * 0.5f;
            float minY = -panelHeight * 0.5f + 178f + menuSize.y * 0.5f;
            float maxY = panelHeight * 0.5f - messageTop - menuSize.y * 0.5f - 12f;
            anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
            anchored.y = Mathf.Clamp(anchored.y, minY, maxY);

            SetAnchoredRect(actionMenuRect, new Vector2(0.5f, 0.5f), anchored, menuSize);
            SetTopStretchRect(actionMenuTitleText != null ? actionMenuTitleText.rectTransform : null, 34f, 18f, -34f, 50f);
            SetTopStretchRect(actionMenuProfileText != null ? actionMenuProfileText.rectTransform : null, 34f, 76f, -34f, 66f);

            RectTransform reportRect = reportButton != null ? reportButton.transform as RectTransform : null;
            RectTransform blockRect = blockButton != null ? blockButton.transform as RectTransform : null;
            RectTransform cancelRect = cancelActionButton != null ? cancelActionButton.transform as RectTransform : null;
            SetAnchoredRect(reportRect, new Vector2(0.5f, 0f), new Vector2(-248f, 54f), new Vector2(224f, 82f));
            SetAnchoredRect(blockRect, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(224f, 82f));
            SetAnchoredRect(cancelRect, new Vector2(0.5f, 0f), new Vector2(248f, 54f), new Vector2(224f, 82f));

            ConfigureTextSize(actionMenuTitleText, 36f, 24f);
            ConfigureTextSize(actionMenuProfileText, 28f, 20f);
            ConfigureButtonLabel(reportButton, 32f, 22f);
            ConfigureButtonLabel(blockButton, 32f, 22f);
            ConfigureButtonLabel(cancelActionButton, 32f, 22f);
        }

        private void LayoutSupportActionMenu(float panelWidth, float panelHeight)
        {
            if (supportActionMenuRect == null)
                return;

            float width = Mathf.Clamp(panelWidth - 120f, 760f, 920f);
            float height = Mathf.Clamp(panelHeight - 60f, 500f, 590f);
            SetAnchoredRect(supportActionMenuRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            SetTopStretchRect(supportActionTitleText != null ? supportActionTitleText.rectTransform : null, 32f, 22f, -92f, 62f);
            SetAnchoredRect(supportActionCloseButton != null ? supportActionCloseButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(58f, 58f));

            float statusGap = 12f;
            float statusWidth = Mathf.Clamp((width - 60f - statusGap * 2f) / 3f, 220f, 280f);
            float statusHeight = 64f;
            float statusStep = statusWidth + statusGap;
            float topRowY = height * 0.5f - 128f;
            float bottomRowY = topRowY - 76f;
            SetAnchoredRect(supportConfirmedButton != null ? supportConfirmedButton.transform as RectTransform : null, new Vector2(0.5f, 0.5f), new Vector2(-statusStep, topRowY), new Vector2(statusWidth, statusHeight));
            SetAnchoredRect(supportUnderReviewButton != null ? supportUnderReviewButton.transform as RectTransform : null, new Vector2(0.5f, 0.5f), new Vector2(0f, topRowY), new Vector2(statusWidth, statusHeight));
            SetAnchoredRect(supportRejectedButton != null ? supportRejectedButton.transform as RectTransform : null, new Vector2(0.5f, 0.5f), new Vector2(statusStep, topRowY), new Vector2(statusWidth, statusHeight));
            SetAnchoredRect(supportClosedButton != null ? supportClosedButton.transform as RectTransform : null, new Vector2(0.5f, 0.5f), new Vector2(-statusStep * 0.5f, bottomRowY), new Vector2(statusWidth, statusHeight));
            SetAnchoredRect(supportVotingButton != null ? supportVotingButton.transform as RectTransform : null, new Vector2(0.5f, 0.5f), new Vector2(statusStep * 0.5f, bottomRowY), new Vector2(statusWidth, statusHeight));

            float commentHeight = Mathf.Clamp(height * 0.19f, 82f, 104f);
            float sendWidth = 190f;
            SetBottomStretchRect(supportCommentInput != null ? supportCommentInput.transform as RectTransform : null, 30f, 28f, -sendWidth - 54f, commentHeight);
            SetAnchoredRect(supportCommentSendButton != null ? supportCommentSendButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-sendWidth * 0.5f - 28f, 28f + commentHeight * 0.5f), new Vector2(sendWidth, commentHeight));

            ConfigureTextSize(supportActionTitleText, 30f, 19f);
            ConfigureInputText(supportCommentInput, 25f, 17f);
            ConfigureButtonLabel(supportConfirmedButton, 25f, 16f);
            ConfigureButtonLabel(supportUnderReviewButton, 25f, 16f);
            ConfigureButtonLabel(supportRejectedButton, 25f, 16f);
            ConfigureButtonLabel(supportClosedButton, 25f, 16f);
            ConfigureButtonLabel(supportVotingButton, 25f, 16f);
            ConfigureButtonLabel(supportCommentSendButton, 24f, 16f);
        }

        public void LayoutToggleButton()
        {
            MainLobbyUiCoordinator.LayoutRightStackButton(toggleButton, MainLobbySideButtonSlot.Chat);
            ConfigureButtonLabel(toggleButton, 30f, 18f);
            MainInfoHintTarget.Detach(toggleButton);
        }

        private void Bind()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePanel);
                toggleButton.onClick.AddListener(TogglePanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(Send);
                sendButton.onClick.AddListener(Send);
            }

            if (reportButton != null)
            {
                reportButton.onClick.RemoveListener(ReportSelectedMessage);
                reportButton.onClick.AddListener(ReportSelectedMessage);
            }

            if (blockButton != null)
            {
                blockButton.onClick.RemoveListener(BlockSelectedUser);
                blockButton.onClick.AddListener(BlockSelectedUser);
            }

            if (cancelActionButton != null)
            {
                cancelActionButton.onClick.RemoveListener(HideActionMenu);
                cancelActionButton.onClick.AddListener(HideActionMenu);
            }

            if (globalChannelButton != null)
            {
                globalChannelButton.onClick.RemoveListener(SelectGlobalChannel);
                globalChannelButton.onClick.AddListener(SelectGlobalChannel);
            }

            if (mahjongChannelButton != null)
            {
                mahjongChannelButton.onClick.RemoveListener(SelectMahjongChannel);
                mahjongChannelButton.onClick.AddListener(SelectMahjongChannel);
            }

            if (developerSupportChannelButton != null)
            {
                developerSupportChannelButton.onClick.RemoveListener(SelectDeveloperSupportChannel);
                developerSupportChannelButton.onClick.AddListener(SelectDeveloperSupportChannel);
            }

            if (autoTranslateButton != null)
            {
                autoTranslateButton.onClick.RemoveListener(ToggleAutoTranslation);
                autoTranslateButton.onClick.AddListener(ToggleAutoTranslation);
            }

            if (supportConfirmedButton != null)
            {
                supportConfirmedButton.onClick.RemoveListener(SetSelectedSupportConfirmed);
                supportConfirmedButton.onClick.AddListener(SetSelectedSupportConfirmed);
            }
            if (supportUnderReviewButton != null)
            {
                supportUnderReviewButton.onClick.RemoveListener(SetSelectedSupportUnderReview);
                supportUnderReviewButton.onClick.AddListener(SetSelectedSupportUnderReview);
            }
            if (supportRejectedButton != null)
            {
                supportRejectedButton.onClick.RemoveListener(SetSelectedSupportRejected);
                supportRejectedButton.onClick.AddListener(SetSelectedSupportRejected);
            }
            if (supportClosedButton != null)
            {
                supportClosedButton.onClick.RemoveListener(SetSelectedSupportClosed);
                supportClosedButton.onClick.AddListener(SetSelectedSupportClosed);
            }
            if (supportVotingButton != null)
            {
                supportVotingButton.onClick.RemoveListener(SetSelectedSupportVoting);
                supportVotingButton.onClick.AddListener(SetSelectedSupportVoting);
            }
            if (supportCommentSendButton != null)
            {
                supportCommentSendButton.onClick.RemoveListener(SendSelectedSupportComment);
                supportCommentSendButton.onClick.AddListener(SendSelectedSupportComment);
            }
            if (supportActionCloseButton != null)
            {
                supportActionCloseButton.onClick.RemoveListener(HideSupportActionMenu);
                supportActionCloseButton.onClick.AddListener(HideSupportActionMenu);
            }

            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(SendFromSubmit);
                inputField.onSubmit.AddListener(SendFromSubmit);
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnChatScrolled);
                scrollRect.onValueChanged.AddListener(OnChatScrolled);
            }
        }

        private void Unbind()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(TogglePanel);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClosePanel);

            if (sendButton != null)
                sendButton.onClick.RemoveListener(Send);

            if (reportButton != null)
                reportButton.onClick.RemoveListener(ReportSelectedMessage);

            if (blockButton != null)
                blockButton.onClick.RemoveListener(BlockSelectedUser);

            if (cancelActionButton != null)
                cancelActionButton.onClick.RemoveListener(HideActionMenu);

            if (globalChannelButton != null)
                globalChannelButton.onClick.RemoveListener(SelectGlobalChannel);

            if (mahjongChannelButton != null)
                mahjongChannelButton.onClick.RemoveListener(SelectMahjongChannel);

            if (developerSupportChannelButton != null)
                developerSupportChannelButton.onClick.RemoveListener(SelectDeveloperSupportChannel);

            if (autoTranslateButton != null)
                autoTranslateButton.onClick.RemoveListener(ToggleAutoTranslation);

            if (supportConfirmedButton != null)
                supportConfirmedButton.onClick.RemoveListener(SetSelectedSupportConfirmed);
            if (supportUnderReviewButton != null)
                supportUnderReviewButton.onClick.RemoveListener(SetSelectedSupportUnderReview);
            if (supportRejectedButton != null)
                supportRejectedButton.onClick.RemoveListener(SetSelectedSupportRejected);
            if (supportClosedButton != null)
                supportClosedButton.onClick.RemoveListener(SetSelectedSupportClosed);
            if (supportVotingButton != null)
                supportVotingButton.onClick.RemoveListener(SetSelectedSupportVoting);
            if (supportCommentSendButton != null)
                supportCommentSendButton.onClick.RemoveListener(SendSelectedSupportComment);
            if (supportActionCloseButton != null)
                supportActionCloseButton.onClick.RemoveListener(HideSupportActionMenu);

            if (inputField != null)
                inputField.onSubmit.RemoveListener(SendFromSubmit);

            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnChatScrolled);
        }

        private void OnChatScrolled(Vector2 normalizedPosition)
        {
            ResetSupportTap();
            if (loadingOlderSupport || normalizedPosition.y < 0.96f || GlobalChatService.I == null)
                return;
            if (GlobalChatService.I.CurrentChannel != GlobalChatService.ChannelDeveloperSupport || !GlobalChatService.I.CanLoadOlderDeveloperSupport)
                return;

            StartCoroutine(LoadOlderSupportRoutine());
        }

        private IEnumerator LoadOlderSupportRoutine()
        {
            loadingOlderSupport = true;
            yield return GlobalChatService.I.LoadOlderDeveloperSupport(50);
            loadingOlderSupport = false;
        }

        private void SelectGlobalChannel()
        {
            SelectChannel(GlobalChatService.ChannelGlobal);
        }

        private void SelectMahjongChannel()
        {
            SelectChannel(GlobalChatService.ChannelMahjong);
        }

        private void SelectDeveloperSupportChannel()
        {
            SelectChannel(GlobalChatService.ChannelDeveloperSupport);
        }

        private void SelectChannel(string channel)
        {
            if (GlobalChatService.I == null)
                return;

            HideActionMenu();
            HideSupportActionMenu();
            GlobalChatService.I.SetChannel(channel);
            RefreshChannelChrome();
            RefreshMessages();

            if (panelRoot != null && panelRoot.activeSelf)
                StartCoroutine(GlobalChatService.I.Refresh());
        }

        private void ToggleAutoTranslation()
        {
            if (AppSettings.I != null)
                AppSettings.I.SetChatAutoTranslateEnabled(!AppSettings.I.ChatAutoTranslateEnabled);
        }

        private void RefreshAutoTranslateButton()
        {
            if (autoTranslateButton == null)
                return;

            SetButtonText(
                autoTranslateButton,
                GameLocalization.Text(AutoTranslationEnabled
                    ? "chat.translation.auto_on"
                    : "chat.translation.auto_off"));

            Image image = autoTranslateButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = AutoTranslationEnabled
                    ? new Color(0.17f, 0.68f, 0.52f, 1f)
                    : new Color(0.42f, 0.47f, 0.54f, 0.94f);
            }
        }

        private void TogglePanel()
        {
            if (panelRoot == null)
                return;

            bool show = !panelRoot.activeSelf;
            if (show && !MainHubStateController.CanOpenMainWindow("GlobalChat"))
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
                StartRefreshing();
                RefreshMessages();
                firstVisitDialogue = ChatFirstVisitDialogueUI.Ensure(transform);
                if (firstVisitDialogue != null)
                    firstVisitDialogue.TryShowForCurrentProfile();
            }
            else
            {
                HideActionMenu();
                HideSupportActionMenu();
                StopRefreshing();
            }
        }

        private void ClosePanel()
        {
            if (firstVisitDialogue != null)
                firstVisitDialogue.HideWithoutCompleting();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainHubStateController.NotifyMainWindowClosed();
            MainGameLaunchBootstrap.RefreshVisibilityNow();
            HideActionMenu();
            HideSupportActionMenu();
            StopRefreshing();
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
                if (GlobalChatService.I != null)
                    yield return GlobalChatService.I.Refresh();

                yield return new WaitForSecondsRealtime(Mathf.Max(1f, refreshSeconds));
            }
        }

        private void SendFromSubmit(string _)
        {
            Send();
        }

        private void Send()
        {
            if (sending || inputField == null || GlobalChatService.I == null)
                return;

            string text = inputField.text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            StartCoroutine(SendRoutine(text));
        }

        private IEnumerator SendRoutine(string text)
        {
            sending = true;
            if (sendButton != null)
                sendButton.interactable = false;

            bool ok = false;
            string error = string.Empty;
            yield return GlobalChatService.I.Send(text, (success, message) =>
            {
                ok = success;
                error = message;
            });

            if (ok && inputField != null)
            {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                RefreshStatus(error);
            }

            if (sendButton != null)
                sendButton.interactable = true;

            sending = false;
        }

        private void ReportSelectedMessage()
        {
            if (sending || GlobalChatService.I == null)
                return;

            GlobalChatService.GlobalChatMessage message = selectedActionMessage;
            if (message == null)
            {
                RefreshStatus(GameLocalization.Text("chat.no_report_target"));
                return;
            }

            StartCoroutine(ReportRoutine(message));
        }

        private IEnumerator ReportRoutine(GlobalChatService.GlobalChatMessage message)
        {
            sending = true;
            SetSafetyButtonsInteractable(false);
            yield return GlobalChatService.I.ReportMessage(message, (success, response) => RefreshStatus(response));
            SetSafetyButtonsInteractable(true);
            HideActionMenu();
            sending = false;
        }

        private void BlockSelectedUser()
        {
            if (sending || GlobalChatService.I == null)
                return;

            GlobalChatService.GlobalChatMessage message = selectedActionMessage;
            if (message == null)
            {
                RefreshStatus(GameLocalization.Text("chat.no_block_target"));
                return;
            }

            StartCoroutine(BlockRoutine(message));
        }

        private IEnumerator BlockRoutine(GlobalChatService.GlobalChatMessage message)
        {
            sending = true;
            SetSafetyButtonsInteractable(false);
            yield return GlobalChatService.I.BlockUser(message, (success, response) => RefreshStatus(response));
            SetSafetyButtonsInteractable(true);
            HideActionMenu();
            sending = false;
        }

        private void SetSafetyButtonsInteractable(bool interactable)
        {
            if (reportButton != null)
                reportButton.interactable = interactable;
            if (blockButton != null)
                blockButton.interactable = interactable;
            if (cancelActionButton != null)
                cancelActionButton.interactable = interactable;
        }

        private void SetSelectedSupportConfirmed()
        {
            SetSelectedSupportStatus("confirmed");
        }

        private void SetSelectedSupportUnderReview()
        {
            SetSelectedSupportStatus("under_review");
        }

        private void SetSelectedSupportRejected()
        {
            SetSelectedSupportStatus("rejected");
        }

        private void SetSelectedSupportClosed()
        {
            SetSelectedSupportStatus("closed");
        }

        private void SetSelectedSupportVoting()
        {
            SetSelectedSupportStatus("voting");
        }

        private void SetSelectedSupportStatus(string status)
        {
            if (sending || GlobalChatService.I == null || selectedActionMessage == null)
                return;

            StartCoroutine(SetSelectedSupportStatusRoutine(selectedActionMessage, status, true, true));
        }

        private IEnumerator SetSelectedSupportStatusRoutine(
            GlobalChatService.GlobalChatMessage message,
            string status,
            bool active,
            bool closeMenuAfterSuccess)
        {
            sending = true;
            SetSupportButtonsInteractable(false);
            bool success = false;
            yield return GlobalChatService.I.SetDeveloperStatus(message, status, active, (ok, response) =>
            {
                success = ok;
                RefreshStatus(response);
            });
            SetSupportButtonsInteractable(true);
            sending = false;

            if (success && closeMenuAfterSuccess)
                HideSupportActionMenu();
        }

        private void SendSelectedSupportComment()
        {
            if (sending || GlobalChatService.I == null || selectedActionMessage == null || supportCommentInput == null)
                return;

            string text = supportCommentInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                RefreshStatus(GameLocalization.Text("chat.support.comment_empty"));
                return;
            }

            StartCoroutine(SendSelectedSupportCommentRoutine(selectedActionMessage, text));
        }

        private IEnumerator SendSelectedSupportCommentRoutine(GlobalChatService.GlobalChatMessage message, string text)
        {
            sending = true;
            SetSupportButtonsInteractable(false);
            bool success = false;
            yield return GlobalChatService.I.AddDeveloperComment(message, text, (ok, response) =>
            {
                success = ok;
                RefreshStatus(response);
            });
            SetSupportButtonsInteractable(true);
            sending = false;

            if (!success)
                yield break;

            supportCommentInput.text = string.Empty;
            selectedActionMessage = FindMessageById(message.id);
            if (supportCommentInput.gameObject.activeInHierarchy)
                supportCommentInput.ActivateInputField();
        }

        private void SetSupportButtonsInteractable(bool interactable)
        {
            if (supportConfirmedButton != null)
                supportConfirmedButton.interactable = interactable;
            if (supportUnderReviewButton != null)
                supportUnderReviewButton.interactable = interactable;
            if (supportRejectedButton != null)
                supportRejectedButton.interactable = interactable;
            if (supportClosedButton != null)
                supportClosedButton.interactable = interactable;
            if (supportVotingButton != null)
                supportVotingButton.interactable = interactable;
            if (supportCommentSendButton != null)
                supportCommentSendButton.interactable = interactable;
            if (supportActionCloseButton != null)
                supportActionCloseButton.interactable = interactable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (messagesText == null || GlobalChatService.I == null || eventData == null)
                return;

            if (eventData.dragging)
                return;

            if (actionMenuRoot != null && actionMenuRoot.activeSelf && IsPointerFromActionMenu(eventData))
                return;
            if (supportActionMenuRoot != null && supportActionMenuRoot.activeSelf && IsChildOfSupportActionMenu(eventData.pointerPress))
                return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(messagesText, eventData.position, eventData.pressEventCamera);
            if (linkIndex < 0 || linkIndex >= messagesText.textInfo.linkCount)
            {
                ResetSupportTap();
                if (actionMenuRoot != null && actionMenuRoot.activeSelf && !IsPointerInsideActionMenu(eventData))
                    HideActionMenu();
                return;
            }

            TMP_LinkInfo link = messagesText.textInfo.linkInfo[linkIndex];
            string id = link.GetLinkID();
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (id.StartsWith("support:", System.StringComparison.Ordinal))
            {
                if (!long.TryParse(id.Substring(8), out long supportMessageId))
                    return;

                GlobalChatService.GlobalChatMessage supportMessage = FindMessageById(supportMessageId);
                if (supportMessage == null || !CanManageDeveloperSupport())
                    return;

                float now = Time.unscaledTime;
                bool isDoubleTap = lastSupportTapMessageId == supportMessageId &&
                                   lastSupportTapPointerId == eventData.pointerId &&
                                   now - lastSupportTapTime <= SupportDoubleTapSeconds &&
                                   (lastSupportTapPosition - eventData.position).sqrMagnitude <= 96f * 96f;
                lastSupportTapMessageId = supportMessageId;
                lastSupportTapTime = now;
                lastSupportTapPointerId = eventData.pointerId;
                lastSupportTapPosition = eventData.position;
                if (!isDoubleTap)
                    return;

                ResetSupportTap();
                ShowSupportActionMenu(supportMessage);
                return;
            }

            ResetSupportTap();
            if (!id.StartsWith("chat:", System.StringComparison.Ordinal))
                return;

            if (!long.TryParse(id.Substring(5), out long messageId))
                return;

            GlobalChatService.GlobalChatMessage message = FindMessageById(messageId);
            if (message == null)
                return;

            ShowActionMenu(message, eventData);
        }

        private bool CanManageDeveloperSupport()
        {
            return ProfileService.I != null &&
                   ProfileService.I.Current != null &&
                   ProfileService.I.Current.IsDeveloper &&
                   GlobalChatService.I != null &&
                   GlobalChatService.I.CanManageDeveloperSupport;
        }

        private bool IsChildOfSupportActionMenu(GameObject target)
        {
            if (target == null || supportActionMenuRoot == null)
                return false;

            Transform current = target.transform;
            Transform menu = supportActionMenuRoot.transform;
            while (current != null)
            {
                if (current == menu)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private void ShowSupportActionMenu(GlobalChatService.GlobalChatMessage message)
        {
            HideActionMenu();
            selectedActionMessage = message;
            if (supportActionTitleText != null)
            {
                string name = string.IsNullOrWhiteSpace(message.nickname) ? GameLocalization.Text("common.player") : message.nickname.Trim();
                supportActionTitleText.text = GameLocalization.Format(
                    "chat.support.manage_title",
                    Escape(name) + GetOwnerBadgeRichText(message.isDeveloper),
                    Escape(GetSupportStatusSummary(message)));
            }
            RefreshSupportStatusButtons(message);
            if (supportCommentInput != null)
                supportCommentInput.text = string.Empty;
            if (supportActionMenuRoot != null)
            {
                supportActionMenuRoot.SetActive(true);
                supportActionMenuRoot.transform.SetAsLastSibling();
            }
        }

        private void HideSupportActionMenu()
        {
            ResetSupportTap();
            if (supportActionMenuRoot != null)
                supportActionMenuRoot.SetActive(false);
            if (supportCommentInput != null)
                supportCommentInput.text = string.Empty;
            selectedActionMessage = null;
        }

        private void ResetSupportTap()
        {
            lastSupportTapMessageId = 0L;
            lastSupportTapTime = -10f;
            lastSupportTapPointerId = int.MinValue;
            lastSupportTapPosition = Vector2.zero;
        }

        private bool IsPointerInsideActionMenu(PointerEventData eventData)
        {
            if (actionMenuRect == null || eventData == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(actionMenuRect, eventData.position, eventData.pressEventCamera);
        }

        private bool IsPointerFromActionMenu(PointerEventData eventData)
        {
            if (actionMenuRoot == null || eventData == null)
                return false;

            return IsChildOfActionMenu(eventData.pointerPress) || IsChildOfActionMenu(eventData.pointerCurrentRaycast.gameObject);
        }

        private bool IsChildOfActionMenu(GameObject target)
        {
            if (target == null || actionMenuRoot == null)
                return false;

            Transform current = target.transform;
            Transform menu = actionMenuRoot.transform;
            while (current != null)
            {
                if (current == menu)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private GlobalChatService.GlobalChatMessage FindMessageById(long messageId)
        {
            IReadOnlyList<GlobalChatService.GlobalChatMessage> messages = GlobalChatService.I != null
                ? GlobalChatService.I.Messages
                : null;
            if (messages == null)
                return null;

            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatService.GlobalChatMessage message = messages[i];
                if (message != null && message.id == messageId)
                    return message;
            }

            return null;
        }

        private void ShowActionMenu(GlobalChatService.GlobalChatMessage message, PointerEventData eventData)
        {
            HideSupportActionMenu();
            selectedActionMessage = message;
            bool actionable = message.userId > 0;
            bool profilePublic = IsPublicProfileMessage(message);
            CaptureActionMenuPosition(eventData);
            LayoutChatPanel();
            if (actionMenuTitleText != null)
            {
                string name = AllianceIdentityFormatter.FormatName(
                    string.IsNullOrWhiteSpace(message.nickname) ? GameLocalization.Text("common.player") : message.nickname.Trim(),
                    message.allianceTag);
                actionMenuTitleText.text = Escape(name) + GetOwnerBadgeRichText(message.isDeveloper);
            }

            if (actionMenuProfileText != null)
                actionMenuProfileText.text = BuildProfileCardText(message, profilePublic);

            if (reportButton != null)
                reportButton.gameObject.SetActive(actionable);
            if (blockButton != null)
                blockButton.gameObject.SetActive(actionable);

            if (actionMenuRoot != null)
            {
                actionMenuRoot.SetActive(true);
                actionMenuRoot.transform.SetAsLastSibling();
            }
        }

        private void CaptureActionMenuPosition(PointerEventData eventData)
        {
            hasActionMenuAnchoredPosition = false;
            if (panelRootRect == null || eventData == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRootRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            actionMenuAnchoredPosition = localPoint + new Vector2(390f, -130f);
            hasActionMenuAnchoredPosition = true;
        }

        private void HideActionMenu()
        {
            selectedActionMessage = null;
            if (actionMenuRoot != null)
                actionMenuRoot.SetActive(false);
        }

        private string BuildProfileCardText(GlobalChatService.GlobalChatMessage message, bool profilePublic)
        {
            if (message == null)
                return string.Empty;

            if (!profilePublic)
                return Escape(GameLocalization.Text("profile.privacy.closed_card"));

            string id = string.IsNullOrWhiteSpace(message.publicPlayerId)
                ? "-"
                : message.publicPlayerId.Trim();
            string alliance = string.IsNullOrWhiteSpace(message.allianceTag)
                ? "-"
                : "#" + message.allianceTag.Trim();

            return Escape(GameLocalization.Format("profile.chat_card", id, alliance));
        }

        private static bool IsPublicProfileMessage(GlobalChatService.GlobalChatMessage message)
        {
            if (message == null)
                return false;

            if (IsChatBotMessage(message))
                return false;

            return message.isProfilePublic;
        }

        private static bool IsChatBotMessage(GlobalChatService.GlobalChatMessage message)
        {
            if (message == null)
                return true;

            if (message.userId <= 0)
                return true;

            return !string.IsNullOrWhiteSpace(message.publicPlayerId) &&
                   message.publicPlayerId.Trim().StartsWith("MB-BOT", System.StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshMessages()
        {
            if (messagesText == null || GlobalChatService.I == null)
                return;

            bool developerSupport = GlobalChatService.I.CurrentChannel == GlobalChatService.ChannelDeveloperSupport;
            EnsureDeveloperSupportContent(messagesViewportRect);
            RectTransform activeContent = developerSupport ? developerSupportContentRect : messagesContentRect;
            float previousContentHeight = activeContent != null ? activeContent.rect.height : 0f;
            Vector2 previousContentPosition = activeContent != null ? activeContent.anchoredPosition : Vector2.zero;
            bool preserveHistoricalScroll = loadingOlderSupport;
            bool preserveCurrentScroll = developerSupport && scrollRect != null && scrollRect.verticalNormalizedPosition > 0.02f;
            RefreshChannelChrome();
            RefreshDeveloperSupportNotificationDots();
            IReadOnlyList<GlobalChatService.GlobalChatMessage> messages = GlobalChatService.I.Messages;

            if (developerSupport)
            {
                RefreshDeveloperSupportCards(messages);
                RestoreDeveloperSupportScroll(previousContentHeight, previousContentPosition, preserveHistoricalScroll, preserveCurrentScroll);
                if (panelRoot != null && panelRoot.activeInHierarchy)
                    GlobalChatService.I.MarkDeveloperSupportReactionsSeen();
                return;
            }

            if (developerSupportContentRect != null)
                developerSupportContentRect.gameObject.SetActive(false);
            messagesText.gameObject.SetActive(true);
            if (scrollRect != null)
                scrollRect.content = messagesContentRect;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatService.GlobalChatMessage message = messages[i];
                if (message == null)
                    continue;

                string name = AllianceIdentityFormatter.FormatName(
                    string.IsNullOrWhiteSpace(message.nickname) ? GameLocalization.Text("common.player") : message.nickname.Trim(),
                    message.allianceTag);
                string originalText = string.IsNullOrWhiteSpace(message.text) ? string.Empty : message.text.Trim();
                bool showTranslation = AutoTranslationEnabled &&
                                       message.isTranslated &&
                                       GlobalChatService.IsTranslationForCurrentLanguage(message.translatedLanguage) &&
                                       !string.IsNullOrWhiteSpace(message.translatedText);
                string text = showTranslation ? message.translatedText.Trim() : originalText;
                if (developerSupport)
                {
                    builder
                        .Append("<link=\"support:")
                        .Append(message.id)
                        .Append("\"><color=#39A9D8>|</color>  <color=#73D7FF><b>")
                        .Append(Escape(name))
                        .Append("</b></color>")
                        .Append(GetOwnerBadgeRichText(message.isDeveloper))
                        .Append("<color=#7897AA>  ·  </color><color=#E9F2F7>")
                        .Append(Escape(text))
                        .Append("</color>\n  <color=")
                        .Append(GetSupportStatusColor(message.status))
                        .Append("><b>")
                        .Append(Escape(GameLocalization.Format("chat.support.status_line", GetSupportStatusLabel(message.status))))
                        .Append("</b></color>");

                    if (message.comments != null)
                    {
                        for (int commentIndex = 0; commentIndex < message.comments.Length; commentIndex++)
                        {
                            GlobalChatService.DeveloperSupportComment comment = message.comments[commentIndex];
                            if (comment == null || string.IsNullOrWhiteSpace(comment.text))
                                continue;

                            string developerName = string.IsNullOrWhiteSpace(comment.developerNickname)
                                ? "Ozkullar"
                                : comment.developerNickname.Trim();
                            builder
                                .Append("\n  <color=#B9A7FF>↳ <b>")
                                .Append(Escape(developerName))
                                .Append("</b></color>")
                                .Append(GetOwnerBadgeRichText(comment.isDeveloper))
                                .Append("<color=#B9A7FF><b>:</b></color> <color=#E8E2FF>")
                                .Append(Escape(comment.text.Trim()))
                                .Append("</color>");
                        }
                    }

                    builder.Append("</link>\n\n");
                    continue;
                }

                if (message.id != 0)
                {
                    builder
                        .Append("<color=#39A9D8>|</color>  <link=\"chat:")
                        .Append(message.id)
                        .Append("\"><color=#73D7FF><b>")
                        .Append(Escape(name))
                        .Append("</b></color>")
                        .Append(GetOwnerBadgeRichText(message.isDeveloper))
                        .Append("</link><color=#7897AA>  ·  </color>");
                }
                else
                {
                    builder
                        .Append("<color=#39A9D8>|</color>  <color=#73D7FF><b>")
                        .Append(Escape(name))
                        .Append("</b></color>")
                        .Append(GetOwnerBadgeRichText(message.isDeveloper))
                        .Append("<color=#7897AA>  ·  </color>");
                }

                builder.Append("<color=#E9F2F7>").Append(Escape(text)).Append("</color>");
                builder.AppendLine();
            }

            messagesText.text = builder.Length == 0
                ? GameLocalization.Text(developerSupport ? "chat.support.empty" : "chat.empty")
                : builder.ToString();
            ResizeMessagesContent();

            if (preserveHistoricalScroll && messagesContentRect != null)
            {
                float addedHeight = Mathf.Max(0f, messagesContentRect.rect.height - previousContentHeight);
                messagesContentRect.anchoredPosition = previousContentPosition + new Vector2(0f, addedHeight);
            }
            else if (preserveCurrentScroll && messagesContentRect != null)
            {
                messagesContentRect.anchoredPosition = previousContentPosition;
            }
            else if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void RefreshDeveloperSupportCards(IReadOnlyList<GlobalChatService.GlobalChatMessage> messages)
        {
            if (developerSupportContentRect == null)
                return;

            messagesText.gameObject.SetActive(false);
            developerSupportContentRect.gameObject.SetActive(true);
            if (scrollRect != null)
                scrollRect.content = developerSupportContentRect;

            float availableWidth = messagesViewportRect != null
                ? Mathf.Max(360f, messagesViewportRect.rect.width - 40f)
                : 900f;
            bool canManage = CanManageDeveloperSupport();
            developerSupportSeenIds.Clear();
            int visibleIndex = 0;
            bool layoutChanged = false;

            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatService.GlobalChatMessage message = messages[i];
                if (message == null || message.id <= 0L || !developerSupportSeenIds.Add(message.id))
                    continue;

                if (!developerSupportCards.TryGetValue(message.id, out DeveloperSupportRequestCardUI card) || card == null)
                {
                    card = AcquireDeveloperSupportCard();
                    developerSupportCards[message.id] = card;
                    layoutChanged = true;
                }

                if (card.transform.GetSiblingIndex() != visibleIndex)
                {
                    card.transform.SetSiblingIndex(visibleIndex);
                    layoutChanged = true;
                }
                visibleIndex++;
                layoutChanged |= card.Bind(this, message, availableWidth, canManage);
            }

            developerSupportRemovalBuffer.Clear();
            foreach (KeyValuePair<long, DeveloperSupportRequestCardUI> entry in developerSupportCards)
            {
                if (!developerSupportSeenIds.Contains(entry.Key))
                    developerSupportRemovalBuffer.Add(entry.Key);
            }

            for (int i = 0; i < developerSupportRemovalBuffer.Count; i++)
            {
                long messageId = developerSupportRemovalBuffer[i];
                DeveloperSupportRequestCardUI card = developerSupportCards[messageId];
                developerSupportCards.Remove(messageId);
                if (card != null)
                {
                    card.Release();
                    developerSupportCardPool.Push(card);
                }
                layoutChanged = true;
            }

            if (developerSupportEmptyText != null)
            {
                string emptyLabel = GameLocalization.Text("chat.support.empty");
                if (!string.Equals(developerSupportEmptyText.text, emptyLabel, System.StringComparison.Ordinal))
                {
                    developerSupportEmptyText.text = emptyLabel;
                    layoutChanged = true;
                }

                bool showEmpty = visibleIndex == 0;
                if (developerSupportEmptyText.gameObject.activeSelf != showEmpty)
                {
                    developerSupportEmptyText.gameObject.SetActive(showEmpty);
                    layoutChanged = true;
                }
                if (developerSupportEmptyText.transform.GetSiblingIndex() != visibleIndex)
                {
                    developerSupportEmptyText.transform.SetSiblingIndex(visibleIndex);
                    layoutChanged = true;
                }
            }

            if (layoutChanged)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(developerSupportContentRect);
                Canvas.ForceUpdateCanvases();
            }
        }

        private DeveloperSupportRequestCardUI AcquireDeveloperSupportCard()
        {
            while (developerSupportCardPool.Count > 0)
            {
                DeveloperSupportRequestCardUI pooled = developerSupportCardPool.Pop();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            return DeveloperSupportRequestCardUI.Create(developerSupportContentRect);
        }

        private void RestoreDeveloperSupportScroll(
            float previousContentHeight,
            Vector2 previousContentPosition,
            bool preserveHistoricalScroll,
            bool preserveCurrentScroll)
        {
            if (developerSupportContentRect == null)
                return;

            if (preserveHistoricalScroll)
            {
                float addedHeight = Mathf.Max(0f, developerSupportContentRect.rect.height - previousContentHeight);
                developerSupportContentRect.anchoredPosition = previousContentPosition + new Vector2(0f, addedHeight);
            }
            else if (preserveCurrentScroll)
            {
                developerSupportContentRect.anchoredPosition = previousContentPosition;
            }
            else if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        internal void HandleSupportCardDoubleTap(long supportMessageId)
        {
            if (!CanManageDeveloperSupport())
                return;

            GlobalChatService.GlobalChatMessage supportMessage = FindMessageById(supportMessageId);
            if (supportMessage != null)
                ShowSupportActionMenu(supportMessage);
        }

        internal void HandleSupportStatusRemoval(long supportMessageId, string status)
        {
            if (sending || !CanManageDeveloperSupport() || GlobalChatService.I == null)
                return;

            GlobalChatService.GlobalChatMessage message = FindMessageById(supportMessageId);
            if (message == null || !GlobalChatService.HasDeveloperSupportStatus(message, status))
                return;

            StartCoroutine(SetSelectedSupportStatusRoutine(message, status, false, false));
        }

        internal bool IsSupportVotePending(long supportMessageId)
        {
            return developerSupportVotePendingIds.Contains(supportMessageId);
        }

        internal void HandleSupportVote(long supportMessageId, int vote)
        {
            if (GlobalChatService.I == null || developerSupportVotePendingIds.Contains(supportMessageId))
                return;

            GlobalChatService.GlobalChatMessage message = FindMessageById(supportMessageId);
            if (message == null || !GlobalChatService.HasDeveloperSupportStatus(message, "voting"))
            {
                RefreshStatus(GameLocalization.Text("chat.support.vote.inactive"));
                return;
            }

            int nextVote = message.myVote == vote ? 0 : vote;
            StartCoroutine(VoteSupportRoutine(message, nextVote));
        }

        private IEnumerator VoteSupportRoutine(GlobalChatService.GlobalChatMessage message, int vote)
        {
            developerSupportVotePendingIds.Add(message.id);
            RefreshMessages();
            yield return GlobalChatService.I.VoteDeveloperSupport(message, vote, (success, response) => RefreshStatus(response));
            developerSupportVotePendingIds.Remove(message.id);
            RefreshMessages();
        }

        private void ResizeMessagesContent()
        {
            if (messagesContentRect == null || messagesViewportRect == null || messagesText == null)
                return;

            messagesText.enableAutoSizing = false;
            messagesText.fontSize = Mathf.Clamp(messagesViewportRect.rect.height * 0.055f, 20f, 30f);
            messagesText.overflowMode = TextOverflowModes.Overflow;
            messagesText.ForceMeshUpdate();
            float viewportHeight = Mathf.Max(1f, messagesViewportRect.rect.height - 44f);
            float contentHeight = Mathf.Max(viewportHeight, messagesText.preferredHeight + 28f);

            messagesContentRect.anchorMin = new Vector2(0f, 1f);
            messagesContentRect.anchorMax = new Vector2(1f, 1f);
            messagesContentRect.pivot = new Vector2(0.5f, 1f);
            messagesContentRect.anchoredPosition = new Vector2(0f, -22f);
            messagesContentRect.sizeDelta = new Vector2(-56f, contentHeight);

            RectTransform textRect = messagesText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        internal static string GetSupportStatusLabel(string status)
        {
            switch (status)
            {
                case "voting": return GameLocalization.Text("chat.support.status.voting");
                case "confirmed": return GameLocalization.Text("chat.support.status.confirmed");
                case "under_review": return GameLocalization.Text("chat.support.status.under_review");
                case "rejected": return GameLocalization.Text("chat.support.status.rejected");
                case "closed": return GameLocalization.Text("chat.support.status.closed");
                default: return GameLocalization.Text("chat.support.status.submitted");
            }
        }

        private static string GetSupportStatusSummary(GlobalChatService.GlobalChatMessage message)
        {
            if (message == null)
                return GetSupportStatusLabel(string.Empty);

            string[] statuses = message.statuses;
            if (statuses == null || statuses.Length == 0)
                return GetSupportStatusLabel(message.status);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < statuses.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(statuses[i]))
                    continue;
                if (builder.Length > 0)
                    builder.Append(", ");
                builder.Append(GetSupportStatusLabel(statuses[i]));
            }
            return builder.Length > 0 ? builder.ToString() : GetSupportStatusLabel(string.Empty);
        }

        private void RefreshSupportStatusButtons(GlobalChatService.GlobalChatMessage message)
        {
            ConfigureSupportStatusButton(supportConfirmedButton, message, "confirmed");
            ConfigureSupportStatusButton(supportUnderReviewButton, message, "under_review");
            ConfigureSupportStatusButton(supportRejectedButton, message, "rejected");
            ConfigureSupportStatusButton(supportClosedButton, message, "closed");
            ConfigureSupportStatusButton(supportVotingButton, message, "voting");
        }

        private static void ConfigureSupportStatusButton(
            Button button,
            GlobalChatService.GlobalChatMessage message,
            string status)
        {
            if (button == null)
                return;
            bool active = GlobalChatService.HasDeveloperSupportStatus(message, status);
            SetButtonText(button, GetSupportStatusLabel(status));
            button.interactable = !active;
        }

        private static string GetSupportStatusColor(string status)
        {
            switch (status)
            {
                case "voting": return "#C58CFF";
                case "confirmed": return "#72E6A1";
                case "under_review": return "#FFD56A";
                case "rejected": return "#FF8C8C";
                case "closed": return "#AAB8C5";
                default: return "#79CFFF";
            }
        }

        internal static Color GetSupportStatusColorValue(string status)
        {
            switch (status)
            {
                case "voting": return new Color(0.77f, 0.55f, 1f, 1f);
                case "confirmed": return new Color(0.45f, 0.90f, 0.63f, 1f);
                case "under_review": return new Color(1f, 0.77f, 0.31f, 1f);
                case "rejected": return new Color(1f, 0.43f, 0.46f, 1f);
                case "closed": return new Color(0.63f, 0.69f, 0.76f, 1f);
                default: return new Color(0.43f, 0.79f, 1f, 1f);
            }
        }

        private static string GetOwnerBadgeRichText(bool isDeveloper)
        {
            return isDeveloper
                ? " <color=#FFD45E><b>◆ " + Escape(GameLocalization.Text("chat.role.owner")) + "</b></color>"
                : string.Empty;
        }

        private void OnDeveloperSupportUnreadChanged(bool unread)
        {
            RefreshDeveloperSupportNotificationDots();
        }

        private void EnsureDeveloperSupportNotificationDots()
        {
            toggleUnreadDot = EnsureNotificationDot(toggleButton, toggleUnreadDot, "DeveloperSupportUnreadDot", 28f);
            developerSupportUnreadDot = EnsureNotificationDot(
                developerSupportChannelButton,
                developerSupportUnreadDot,
                "UnreadReactionDot",
                24f);
        }

        private static Image EnsureNotificationDot(Button button, Image current, string objectName, float size)
        {
            if (button == null)
                return current;

            if (current == null)
            {
                Transform existing = button.transform.Find(objectName);
                if (existing != null)
                    current = existing.GetComponent<Image>();
            }

            if (current == null)
            {
                GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                root.transform.SetParent(button.transform, false);
                current = root.GetComponent<Image>();
                ApplyRoundedSurface(current);
                current.color = new Color(1f, 0.18f, 0.22f, 1f);
                current.raycastTarget = false;
                Outline outline = root.GetComponent<Outline>();
                outline.effectColor = new Color(0.35f, 0.01f, 0.03f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            RectTransform rect = current.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f, -8f);
            rect.sizeDelta = new Vector2(size, size);
            rect.SetAsLastSibling();
            return current;
        }

        private void RefreshDeveloperSupportNotificationDots()
        {
            EnsureDeveloperSupportNotificationDots();
            bool unread = GlobalChatService.I != null && GlobalChatService.I.HasUnreadDeveloperSupportReaction;
            if (toggleUnreadDot != null)
                toggleUnreadDot.gameObject.SetActive(unread);
            if (developerSupportUnreadDot != null)
                developerSupportUnreadDot.gameObject.SetActive(unread);
        }

        private void RefreshChannelChrome()
        {
            if (titleText != null)
            {
                titleText.text = GameLocalization.Text("chat.title");
                titleText.gameObject.SetActive(true);
            }

            SetButtonText(globalChannelButton, GameLocalization.Text("chat.channel.global"));
            SetButtonText(mahjongChannelButton, GameLocalization.Text("chat.channel.mahjong"));
            SetButtonText(developerSupportChannelButton, GameLocalization.Text("chat.channel.developer_support"));
            RefreshAutoTranslateButton();

            string currentChannel = GlobalChatService.I != null ? GlobalChatService.I.CurrentChannel : GlobalChatService.ChannelGlobal;
            ApplyChannelButton(globalChannelButton, currentChannel == GlobalChatService.ChannelGlobal);
            ApplyChannelButton(mahjongChannelButton, currentChannel == GlobalChatService.ChannelMahjong);
            ApplyChannelButton(developerSupportChannelButton, currentChannel == GlobalChatService.ChannelDeveloperSupport);
            ConfigureComposerForChannel(currentChannel);
        }

        private void ConfigureComposerForChannel(string channel)
        {
            if (inputField == null)
                return;

            bool developerSupport = channel == GlobalChatService.ChannelDeveloperSupport;
            inputField.characterLimit = developerSupport ? 1000 : 240;
            inputField.lineType = developerSupport ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            if (inputField.placeholder is TMP_Text placeholder)
            {
                placeholder.text = GameLocalization.Text(developerSupport ? "chat.support.placeholder" : "chat.placeholder") + "...";
            }
        }

        private void RefreshLocalization()
        {
            SetButtonText(toggleButton, GameLocalization.Text("chat.title"));
            SetButtonText(sendButton, GameLocalization.Text("chat.send"));
            SetButtonText(reportButton, GameLocalization.Text("chat.report"));
            SetButtonText(blockButton, GameLocalization.Text("chat.block"));
            SetButtonText(cancelActionButton, GameLocalization.Text("settings.close"));
            SetButtonText(supportConfirmedButton, GameLocalization.Text("chat.support.status.confirmed"));
            SetButtonText(supportUnderReviewButton, GameLocalization.Text("chat.support.status.under_review"));
            SetButtonText(supportRejectedButton, GameLocalization.Text("chat.support.status.rejected"));
            SetButtonText(supportClosedButton, GameLocalization.Text("chat.support.status.closed"));
            SetButtonText(supportVotingButton, GameLocalization.Text("chat.support.status.voting"));
            SetButtonText(supportCommentSendButton, GameLocalization.Text("chat.support.comment_send"));
            RefreshAutoTranslateButton();
            if (selectedActionMessage != null)
                RefreshSupportStatusButtons(selectedActionMessage);
            RefreshChannelChrome();

            if (supportCommentInput != null && supportCommentInput.placeholder is TMP_Text supportPlaceholder)
                supportPlaceholder.text = GameLocalization.Text("chat.support.comment_placeholder") + "...";
        }

        private void RefreshStatus(string value)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        private static TMP_InputField CreateInput(Transform parent)
        {
            GameObject root = new GameObject("MessageInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(18f, 42f);
            rect.offsetMax = new Vector2(-128f, 92f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.015f, 0.024f, 0.04f, 0.92f);

            GameObject viewport = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.transform as RectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(20f, 6f);
            viewportRect.offsetMax = new Vector2(-20f, -6f);

            TMP_Text text = CreateText(viewport.transform, "Text", "", 20f, TextAlignmentOptions.Left);
            text.color = Color.white;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);

            TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", GameLocalization.Text("chat.placeholder") + "...", 20f, TextAlignmentOptions.Left);
            placeholder.color = new Color(0.76f, 0.86f, 0.93f, 0.92f);
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(8f, 2f);
            placeholderRect.offsetMax = new Vector2(-8f, -2f);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.textViewport = viewportRect;
            input.characterLimit = 240;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = new Color(0.48f, 0.86f, 1f, 1f);
            input.selectionColor = new Color(0.20f, 0.58f, 0.82f, 0.55f);
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

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.13f, 0.42f, 0.56f, 0.96f);

            TMP_Text text = CreateText(root.transform, "Label", label, 22f, TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 3f);
            textRect.offsetMax = new Vector2(-10f, -4f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 20f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.margin = Vector4.zero;

            Button button = root.GetComponent<Button>();
            MainLobbyButtonStyle.Apply(button);
            return button;
        }

        private static void ApplyChannelButton(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active
                    ? Color.white
                    : new Color(0.46f, 0.60f, 0.72f, 0.76f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = active
                    ? Color.white
                    : new Color(0.72f, 0.80f, 0.88f, 0.9f);
        }

        private static void ConfigureOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
                return;

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = target.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        internal static void ApplyRoundedSurface(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetRoundedSurfaceSprite();
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
        }

        private static Sprite GetRoundedSurfaceSprite()
        {
            if (roundedSurfaceSprite != null)
                return roundedSurfaceSprite;

            const int size = RoundedSurfaceTextureSize;
            const int radius = RoundedSurfaceRadius;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GlobalChatRoundedSurface",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float coverage = 0f;
                    coverage += IsInsideRoundedRect(x + 0.25f, y + 0.25f, size, radius) ? 0.25f : 0f;
                    coverage += IsInsideRoundedRect(x + 0.75f, y + 0.25f, size, radius) ? 0.25f : 0f;
                    coverage += IsInsideRoundedRect(x + 0.25f, y + 0.75f, size, radius) ? 0.25f : 0f;
                    coverage += IsInsideRoundedRect(x + 0.75f, y + 0.75f, size, radius) ? 0.25f : 0f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedSurfaceSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedSurfaceSprite.name = "GlobalChatRoundedSurface";
            roundedSurfaceSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedSurfaceSprite;
        }

        private static bool IsInsideRoundedRect(float x, float y, float size, float radius)
        {
            float nearestX = Mathf.Clamp(x, radius, size - radius);
            float nearestY = Mathf.Clamp(y, radius, size - radius);
            float deltaX = x - nearestX;
            float deltaY = y - nearestY;
            return deltaX * deltaX + deltaY * deltaY <= radius * radius;
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
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

        private static Vector4 GetSafeAreaInsets(RectTransform rootRect)
        {
            if (rootRect == null || Screen.width <= 0 || Screen.height <= 0)
                return Vector4.zero;

            Rect safeArea = Screen.safeArea;
            float scaleX = rootRect.rect.width / Screen.width;
            float scaleY = rootRect.rect.height / Screen.height;
            if (scaleX <= 0f || scaleY <= 0f)
                return Vector4.zero;

            return new Vector4(
                safeArea.xMin * scaleX,
                safeArea.yMin * scaleY,
                (Screen.width - safeArea.xMax) * scaleX,
                (Screen.height - safeArea.yMax) * scaleY);
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
