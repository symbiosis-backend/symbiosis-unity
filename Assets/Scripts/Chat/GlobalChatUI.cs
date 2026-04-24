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
    public sealed class GlobalChatUI : MonoBehaviour
    {
        private const int RootCanvasSortingOrder = 10020;

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
        [SerializeField] private TMP_Text messagesText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float refreshSeconds = 2.5f;

        private RectTransform messagesViewportRect;
        private RectTransform messagesContentRect;
        private Coroutine refreshRoutine;
        private bool sending;

        private void Awake()
        {
            if (toggleButton == null || panelRoot == null)
                Build(transform);
        }

        private void OnRectTransformDimensionsChange()
        {
            LayoutChatPanel();
        }

        public static GlobalChatUI CreateInScene()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Exclude) == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }
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
            Bind();
            EnsurePanelReferences();
            LayoutToggleButton();
            LayoutChatPanel();
            AppSettings.OnLanguageChanged += OnLanguageChanged;

            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged += RefreshMessages;
                GlobalChatService.I.ErrorChanged += RefreshStatus;
            }

            RefreshLocalization();
            RefreshMessages();
            RefreshStatus(GlobalChatService.I != null ? GlobalChatService.I.LastError : string.Empty);
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;

            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged -= RefreshMessages;
                GlobalChatService.I.ErrorChanged -= RefreshStatus;
            }

            StopRefreshing();
            Unbind();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalization();
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

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;

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

            scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = messagesViewportRect;
            scrollRect.content = messagesContentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            inputField = CreateInput(panelRoot.transform);
            sendButton = CreateButton(panelRoot.transform, "SendButton", GameLocalization.Text("chat.send"), new Vector2(1f, 0f), new Vector2(-62f, 52f), new Vector2(104f, 48f));

            statusText = CreateText(panelRoot.transform, "StatusText", "", 15f, TextAlignmentOptions.Left);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.offsetMin = new Vector2(22f, 8f);
            statusRect.offsetMax = new Vector2(-22f, 32f);
            statusText.color = new Color(1f, 0.68f, 0.34f, 1f);

            LayoutChatPanel();
            panelRoot.SetActive(false);
            Bind();
            RefreshLocalization();
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
                panelBackgroundImage.color = new Color(0.015f, 0.022f, 0.035f, 0.94f);
                panelBackgroundImage.raycastTarget = false;
            }

            if (panelBackgroundRect != null)
                panelBackgroundRect.SetAsFirstSibling();
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
                MainLobbyButtonStyle.ApplyMainFrame(panelFrameImage);
                panelFrameImage.raycastTarget = false;
            }

            if (panelFrameRect != null)
                panelFrameRect.SetAsLastSibling();
        }

        private void ConfigurePanelRootImage()
        {
            if (panelImage == null)
                return;

            panelImage.sprite = null;
            panelImage.color = new Color(0f, 0f, 0f, 0f);
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

            float rootWidth = Mathf.Max(480f, rootRect.rect.width);
            float rootHeight = Mathf.Max(360f, rootRect.rect.height);
            const float frameAspect = 1494f / 1024f;
            float maxWidth = Mathf.Max(360f, rootWidth * 0.8f);
            float maxHeight = Mathf.Max(300f, rootHeight * 0.8f);
            float panelWidth = Mathf.Min(maxWidth, maxHeight * frameAspect);
            float panelHeight = panelWidth / frameAspect;
            panelWidth = Mathf.Clamp(panelWidth, Mathf.Min(720f, maxWidth), maxWidth);
            panelHeight = Mathf.Clamp(panelHeight, Mathf.Min(500f, maxHeight), maxHeight);

            panelRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRootRect.pivot = new Vector2(0.5f, 0.5f);
            panelRootRect.anchoredPosition = Vector2.zero;
            panelRootRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            EnsurePanelBackground();
            EnsurePanelFrame();

            float insetX = Mathf.Max(72f, panelWidth * 0.095f);
            float insetTop = Mathf.Max(58f, panelHeight * 0.09f);
            float insetBottom = Mathf.Max(64f, panelHeight * 0.085f);
            float inputHeight = Mathf.Clamp(panelHeight * 0.08f, 58f, 72f);
            float channelY = -insetTop - 70f;
            float messageTop = insetTop + 140f;
            float messageBottom = insetBottom + inputHeight + 42f;

            SetStretchRect(panelFrameRect, 0f, 0f, 0f, 0f);
            SetStretchRect(panelBackgroundRect, insetX + 12f, insetBottom + 12f, -insetX - 12f, -insetTop - 12f);

            if (titleText != null)
            {
                titleText.text = string.Empty;
                titleText.gameObject.SetActive(false);
            }

            SetAnchoredRect(closeButton != null ? closeButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-insetX - 54f, -insetTop - 34f), new Vector2(112f, 82f));
            ConfigureButtonLabel(closeButton, 42f, 26f);
            SetAnchoredRect(globalChannelButton != null ? globalChannelButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(insetX + 96f, channelY), new Vector2(172f, 54f));
            SetAnchoredRect(mahjongChannelButton != null ? mahjongChannelButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(insetX + 292f, channelY), new Vector2(184f, 54f));
            ConfigureButtonLabel(globalChannelButton, 21f, 12f);
            ConfigureButtonLabel(mahjongChannelButton, 21f, 12f);

            SetStretchRect(messagesViewportRect, insetX + 24f, messageBottom, -insetX - 24f, -messageTop);
            Image viewportImage = messagesViewportRect != null ? messagesViewportRect.GetComponent<Image>() : null;
            if (viewportImage != null)
                viewportImage.color = new Color(0.01f, 0.018f, 0.032f, 0.78f);
            SetStretchRect(messagesContentRect, 22f, 18f, -22f, -18f);
            ConfigureTextSize(messagesText, 36f, 22f);
            if (messagesText != null)
                messagesText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform inputRect = inputField != null ? inputField.transform as RectTransform : null;
            SetBottomStretchRect(inputRect, insetX + 24f, insetBottom + 18f, -insetX - 228f, inputHeight);
            Image inputImage = inputField != null ? inputField.GetComponent<Image>() : null;
            if (inputImage != null)
                inputImage.color = new Color(0.015f, 0.024f, 0.04f, 0.92f);
            ConfigureInputText(inputField, 26f, 17f);

            SetAnchoredRect(sendButton != null ? sendButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-insetX - 102f, insetBottom + 18f + inputHeight * 0.5f), new Vector2(184f, inputHeight));
            ConfigureButtonLabel(sendButton, 22f, 13f);
            SetBottomStretchRect(statusText != null ? statusText.rectTransform : null, insetX + 24f, insetBottom - 20f, -insetX - 24f, 28f);
            ConfigureTextSize(statusText, 18f, 12f);

            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        public void LayoutToggleButton()
        {
            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(true);
                toggleButton.transform.SetAsLastSibling();
            }

            SetAnchoredRect(toggleButton != null ? toggleButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-210f, 76f), new Vector2(330f, 93f));
            ConfigureButtonLabel(toggleButton, 30f, 18f);
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

            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(SendFromSubmit);
                inputField.onSubmit.AddListener(SendFromSubmit);
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

            if (globalChannelButton != null)
                globalChannelButton.onClick.RemoveListener(SelectGlobalChannel);

            if (mahjongChannelButton != null)
                mahjongChannelButton.onClick.RemoveListener(SelectMahjongChannel);

            if (inputField != null)
                inputField.onSubmit.RemoveListener(SendFromSubmit);
        }

        private void SelectGlobalChannel()
        {
            SelectChannel(GlobalChatService.ChannelGlobal);
        }

        private void SelectMahjongChannel()
        {
            SelectChannel(GlobalChatService.ChannelMahjong);
        }

        private void SelectChannel(string channel)
        {
            if (GlobalChatService.I == null)
                return;

            GlobalChatService.I.SetChannel(channel);
            RefreshChannelChrome();
            RefreshMessages();

            if (panelRoot != null && panelRoot.activeSelf)
                StartCoroutine(GlobalChatService.I.Refresh());
        }

        private void TogglePanel()
        {
            if (panelRoot == null)
                return;

            bool show = !panelRoot.activeSelf;
            transform.SetAsLastSibling();
            panelRoot.SetActive(show);

            if (show)
                StartRefreshing();
            else
                StopRefreshing();
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

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

        private void RefreshMessages()
        {
            if (messagesText == null || GlobalChatService.I == null)
                return;

            RefreshChannelChrome();
            StringBuilder builder = new StringBuilder();
            IReadOnlyList<GlobalChatService.GlobalChatMessage> messages = GlobalChatService.I.Messages;
            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatService.GlobalChatMessage message = messages[i];
                if (message == null)
                    continue;

                string name = string.IsNullOrWhiteSpace(message.nickname) ? GameLocalization.Text("common.player") : message.nickname.Trim();
                string text = string.IsNullOrWhiteSpace(message.text) ? string.Empty : message.text.Trim();
                builder.Append("<b>").Append(Escape(name)).Append(":</b> ").Append(Escape(text)).AppendLine();
            }

            messagesText.text = builder.Length == 0 ? GameLocalization.Text("chat.empty") : builder.ToString();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void RefreshChannelChrome()
        {
            if (titleText != null)
            {
                titleText.text = string.Empty;
                titleText.gameObject.SetActive(false);
            }

            SetButtonText(globalChannelButton, GameLocalization.Text("chat.channel.global"));
            SetButtonText(mahjongChannelButton, GameLocalization.Text("chat.channel.mahjong"));

            string currentChannel = GlobalChatService.I != null ? GlobalChatService.I.CurrentChannel : GlobalChatService.ChannelGlobal;
            ApplyChannelButton(globalChannelButton, currentChannel == GlobalChatService.ChannelGlobal);
            ApplyChannelButton(mahjongChannelButton, currentChannel == GlobalChatService.ChannelMahjong);
        }

        private void RefreshLocalization()
        {
            SetButtonText(toggleButton, GameLocalization.Text("chat.title"));
            SetButtonText(sendButton, GameLocalization.Text("chat.send"));
            RefreshChannelChrome();

            if (inputField != null && inputField.placeholder is TMP_Text placeholder)
                placeholder.text = GameLocalization.Text("chat.placeholder");
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

            TMP_Text text = CreateText(root.transform, "Text", "", 20f, TextAlignmentOptions.Left);
            text.color = Color.white;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            TMP_Text placeholder = CreateText(root.transform, "Placeholder", GameLocalization.Text("chat.placeholder"), 20f, TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 4f);
            placeholderRect.offsetMax = new Vector2(-12f, -4f);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 240;
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
            text.overflowMode = TextOverflowModes.Ellipsis;
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
                image.color = Color.white;
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
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(10f, 2f, 10f, 4f);
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
