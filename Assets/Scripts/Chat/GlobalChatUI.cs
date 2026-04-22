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
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
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

        private Coroutine refreshRoutine;
        private bool sending;

        private void Awake()
        {
            if (toggleButton == null || panelRoot == null)
                Build(transform);
        }

        public static GlobalChatUI CreateInScene()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2400f, 1080f);
                scaler.matchWidthOrHeight = 0.65f;
            }

            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Exclude) == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }

            GameObject root = new GameObject("GlobalChatUI", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            GlobalChatUI ui = root.AddComponent<GlobalChatUI>();
            if (ui.toggleButton == null || ui.panelRoot == null)
                ui.Build(root.transform);

            return ui;
        }

        private void OnEnable()
        {
            Bind();

            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged += RefreshMessages;
                GlobalChatService.I.ErrorChanged += RefreshStatus;
            }

            RefreshMessages();
            RefreshStatus(GlobalChatService.I != null ? GlobalChatService.I.LastError : string.Empty);
        }

        private void OnDisable()
        {
            if (GlobalChatService.I != null)
            {
                GlobalChatService.I.MessagesChanged -= RefreshMessages;
                GlobalChatService.I.ErrorChanged -= RefreshStatus;
            }

            StopRefreshing();
            Unbind();
        }

        private void Build(Transform parent)
        {
            RectTransform rootRect = parent as RectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(parent, "ChatButton", GameLocalization.Text("chat.title"), new Vector2(1f, 0f), new Vector2(-138f, 72f), new Vector2(180f, 56f));

            panelRoot = new GameObject("ChatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            RectTransform panelRect = panelRoot.transform as RectTransform;
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-32f, 140f);
            panelRect.sizeDelta = new Vector2(520f, 570f);

            Image panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = new Color(0.045f, 0.05f, 0.065f, 0.94f);

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
            RectTransform viewportRect = viewport.transform as RectTransform;
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(18f, 104f);
            viewportRect.offsetMax = new Vector2(-18f, -122f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;

            GameObject content = new GameObject("MessagesContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.transform as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(12f, 12f);
            contentRect.offsetMax = new Vector2(-12f, -12f);

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
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
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

            panelRoot.SetActive(false);
            Bind();
            RefreshChannelChrome();
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
            if (GlobalChatService.I == null)
                return;

            if (titleText != null)
                titleText.text = GlobalChatService.I.CurrentChannelLabel + " Chat";

            ApplyChannelButton(globalChannelButton, GlobalChatService.I.CurrentChannel == GlobalChatService.ChannelGlobal);
            ApplyChannelButton(mahjongChannelButton, GlobalChatService.I.CurrentChannel == GlobalChatService.ChannelMahjong);
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
            background.color = new Color(1f, 1f, 1f, 0.12f);

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
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return root.GetComponent<Button>();
        }

        private static void ApplyChannelButton(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? new Color(0.22f, 0.58f, 0.62f, 0.98f) : new Color(0.11f, 0.22f, 0.28f, 0.92f);
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);

            TMP_Text text = root.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
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
