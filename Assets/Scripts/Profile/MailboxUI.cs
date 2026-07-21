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
    public sealed class MailboxUI : MonoBehaviour
    {
        private const int RootCanvasSortingOrder = 30031;
        public static bool IsMailboxAccessEnabled => false;

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelRootRect;
        [SerializeField] private Image panelFrameImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inboxTabButton;
        [SerializeField] private Button sentTabButton;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private ScrollRect listScrollRect;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_InputField subjectInput;
        [SerializeField] private TMP_InputField bodyInput;
        [SerializeField] private RectTransform attachmentIconRoot;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button sendButton;

        private readonly List<Button> rowButtons = new List<Button>();
        private readonly List<string> rowMessageIds = new List<string>();
        private readonly List<Color> rowBaseColors = new List<Color>();
        private TMP_Text panelTitleText;
        private TMP_Text listHeaderText;
        private TMP_Text detailSubjectText;
        private TMP_Text detailMetaText;
        private TMP_Text attachmentsTitleText;
        private TMP_Text attachmentStatusText;
        private RectTransform detailViewportRect;
        private ScrollRect detailScrollRect;
        private RectTransform detailPanelRect;
        private Image detailPanelImage;
        private Image detailDividerImage;
        private Image attachmentDividerImage;
        private string selectedMessageId;
        private bool showingSent;

        public static MailboxUI CreateInScene()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();

            GameObject root = new GameObject("MailboxUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ConfigureRootCanvas(root);
            MailboxUI ui = root.AddComponent<MailboxUI>();
            // AddComponent invokes Awake/OnEnable immediately. Awake already builds the
            // runtime hierarchy, so calling Build again here used to create a second
            // MailboxButton and a second MailboxPanel on every Main entry.
            ui.EnsureBuilt(root.transform);
            return ui;
        }

        private void Awake()
        {
            EnsureBuilt(transform);
        }

        private void OnEnable()
        {
            ConfigureRootCanvas(gameObject);
            Bind();
            LayoutToggleButton();
            LayoutPanel();
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            MailboxService.MailboxChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            MailboxService.MailboxChanged -= Refresh;
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            Unbind();
        }

        private void OnRectTransformDimensionsChange()
        {
            LayoutPanel();
        }

        public void LayoutToggleButton()
        {
            EnsureBuilt(transform);
            MainLobbyUiCoordinator.LayoutRightStackButton(toggleButton, MainLobbySideButtonSlot.Mail);
            ConfigureButtonLabel(toggleButton, 30f, 18f);
            RefreshToggleLabel();
        }

        public void RepairVisualHierarchy()
        {
            EnsureBuilt(transform);
            RemoveDuplicateDirectChildren("MailboxButton", toggleButton != null ? toggleButton.gameObject : null);
            RemoveDuplicateDirectChildren("MailboxPanel", panelRoot);
        }

        private void EnsureBuilt(Transform parent)
        {
            if (toggleButton == null || panelRoot == null)
                Build(parent != null ? parent : transform);

            RemoveDuplicateDirectChildren("MailboxButton", toggleButton != null ? toggleButton.gameObject : null);
            RemoveDuplicateDirectChildren("MailboxPanel", panelRoot);
        }

        private void RemoveDuplicateDirectChildren(string objectName, GameObject keep)
        {
            Transform root = transform;
            if (root == null || string.IsNullOrWhiteSpace(objectName))
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null || child.gameObject == keep ||
                    !string.Equals(child.name, objectName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
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
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void Build(Transform parent)
        {
            RectTransform rootRect = parent as RectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(parent, "MailboxButton", GameLocalization.Text("mail.title"), new Vector2(1f, 0f), new Vector2(-210f, 300f), new Vector2(330f, 93f));

            panelRoot = new GameObject("MailboxPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            panelRootRect = panelRoot.transform as RectTransform;
            Image panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.01f);
            panelImage.raycastTarget = true;

            GameObject frame = new GameObject("MailboxFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(panelRoot.transform, false);
            panelFrameImage = frame.GetComponent<Image>();
            MainLobbyButtonStyle.ApplyDlsWindow(panelFrameImage);
            panelFrameImage.raycastTarget = false;

            closeButton = CreateButton(panelRoot.transform, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(-62f, -58f), new Vector2(76f, 76f));
            panelTitleText = CreateText(panelRoot.transform, "MailboxTitle", GameLocalization.Text("mail.title"), 42f, TextAlignmentOptions.Center);
            panelTitleText.fontStyle = FontStyles.Bold;
            inboxTabButton = CreateButton(panelRoot.transform, "InboxTab", GameLocalization.Text("mail.inbox"), new Vector2(0f, 1f), new Vector2(180f, -94f), new Vector2(260f, 78f));
            sentTabButton = CreateButton(panelRoot.transform, "SentTab", GameLocalization.Text("mail.sent"), new Vector2(0f, 1f), new Vector2(470f, -94f), new Vector2(260f, 78f));
            listHeaderText = CreateText(panelRoot.transform, "MessageListHeader", GameLocalization.Text("mail.inbox"), 26f, TextAlignmentOptions.Left);
            listHeaderText.fontStyle = FontStyles.Bold;
            listHeaderText.color = new Color(0.76f, 0.86f, 1f, 1f);

            GameObject listViewport = new GameObject("MessageListViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            listViewport.transform.SetParent(panelRoot.transform, false);
            Image listImage = listViewport.GetComponent<Image>();
            listImage.color = new Color(0.01f, 0.025f, 0.045f, 0.58f);
            listViewport.GetComponent<Mask>().showMaskGraphic = true;

            GameObject listContentObject = new GameObject("MessageListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContentObject.transform.SetParent(listViewport.transform, false);
            listContent = listContentObject.transform as RectTransform;
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = listContentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = listContentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            listScrollRect = listViewport.AddComponent<ScrollRect>();
            listScrollRect.content = listContent;
            listScrollRect.viewport = listViewport.transform as RectTransform;
            listScrollRect.horizontal = false;
            listScrollRect.vertical = true;
            listScrollRect.movementType = ScrollRect.MovementType.Clamped;
            listScrollRect.inertia = true;
            listScrollRect.scrollSensitivity = 34f;

            GameObject detailPanel = new GameObject("MessageDetailPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            detailPanel.transform.SetParent(panelRoot.transform, false);
            detailPanelRect = detailPanel.transform as RectTransform;
            detailPanelImage = detailPanel.GetComponent<Image>();
            detailPanelImage.color = new Color(0.015f, 0.045f, 0.078f, 0.46f);
            detailPanelImage.raycastTarget = false;

            detailSubjectText = CreateText(panelRoot.transform, "DetailSubject", "", 36f, TextAlignmentOptions.Left);
            detailSubjectText.fontStyle = FontStyles.Bold;
            detailMetaText = CreateText(panelRoot.transform, "DetailMeta", "", 23f, TextAlignmentOptions.Left);
            detailMetaText.color = new Color(0.65f, 0.75f, 0.86f, 1f);

            detailDividerImage = CreateSolidImage(panelRoot.transform, "DetailDivider", new Color(0.25f, 0.58f, 0.9f, 0.32f));

            GameObject detailViewport = new GameObject("DetailBodyViewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            detailViewport.transform.SetParent(panelRoot.transform, false);
            detailViewportRect = detailViewport.transform as RectTransform;

            detailText = CreateText(detailViewport.transform, "DetailText", "", 28f, TextAlignmentOptions.TopLeft);
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.overflowMode = TextOverflowModes.Overflow;
            detailText.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailText.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailText.rectTransform.pivot = new Vector2(0.5f, 1f);
            detailText.rectTransform.anchoredPosition = Vector2.zero;
            detailText.rectTransform.sizeDelta = Vector2.zero;
            ContentSizeFitter detailFitter = detailText.gameObject.AddComponent<ContentSizeFitter>();
            detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailScrollRect = detailViewport.GetComponent<ScrollRect>();
            detailScrollRect.content = detailText.rectTransform;
            detailScrollRect.viewport = detailViewportRect;
            detailScrollRect.horizontal = false;
            detailScrollRect.vertical = true;
            detailScrollRect.movementType = ScrollRect.MovementType.Clamped;
            detailScrollRect.inertia = true;
            detailScrollRect.scrollSensitivity = 32f;

            attachmentsTitleText = CreateText(panelRoot.transform, "AttachmentsTitle", GameLocalization.Text("mail.attachments"), 25f, TextAlignmentOptions.Left);
            attachmentsTitleText.fontStyle = FontStyles.Bold;
            attachmentDividerImage = CreateSolidImage(panelRoot.transform, "AttachmentDivider", new Color(0.25f, 0.58f, 0.9f, 0.24f));

            GameObject iconRoot = new GameObject("AttachmentIcons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            iconRoot.transform.SetParent(panelRoot.transform, false);
            attachmentIconRoot = iconRoot.transform as RectTransform;
            HorizontalLayoutGroup iconLayout = iconRoot.GetComponent<HorizontalLayoutGroup>();
            iconLayout.spacing = 12f;
            iconLayout.childAlignment = TextAnchor.MiddleLeft;
            iconLayout.childControlWidth = false;
            iconLayout.childControlHeight = false;
            iconLayout.childForceExpandWidth = false;
            iconLayout.childForceExpandHeight = false;

            attachmentStatusText = CreateText(panelRoot.transform, "AttachmentStatus", "", 22f, TextAlignmentOptions.Left);
            attachmentStatusText.color = new Color(0.72f, 0.86f, 1f, 1f);

            subjectInput = CreateInput(panelRoot.transform, "SubjectInput", GameLocalization.Text("mail.subject_placeholder"), 34f);
            bodyInput = CreateInput(panelRoot.transform, "BodyInput", GameLocalization.Text("mail.body_placeholder"), 30f);
            bodyInput.lineType = TMP_InputField.LineType.MultiLineNewline;

            claimButton = CreateButton(panelRoot.transform, "ClaimButton", GameLocalization.Text("mail.claim"), new Vector2(1f, 0f), new Vector2(-330f, 86f), new Vector2(280f, 86f));
            deleteButton = CreateButton(panelRoot.transform, "DeleteMessageButton", "", new Vector2(1f, 0f), new Vector2(-160f, 86f), new Vector2(86f, 86f));
            BuildTrashIcon(deleteButton);
            sendButton = CreateButton(panelRoot.transform, "SendLetterButton", GameLocalization.Text("mail.send"), new Vector2(1f, 0f), new Vector2(-330f, 86f), new Vector2(280f, 86f));
            statusText = CreateText(panelRoot.transform, "StatusText", "", 22f, TextAlignmentOptions.Left);
            statusText.color = new Color(1f, 0.72f, 0.42f, 1f);

            panelRoot.SetActive(false);
            Bind();
            LayoutPanel();
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
            if (inboxTabButton != null)
            {
                inboxTabButton.onClick.RemoveListener(ShowInbox);
                inboxTabButton.onClick.AddListener(ShowInbox);
            }
            if (sentTabButton != null)
            {
                sentTabButton.onClick.RemoveListener(ShowSent);
                sentTabButton.onClick.AddListener(ShowSent);
            }
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(ClaimSelected);
                claimButton.onClick.AddListener(ClaimSelected);
            }
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(DeleteSelected);
                deleteButton.onClick.AddListener(DeleteSelected);
            }
            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(SendLetter);
                sendButton.onClick.AddListener(SendLetter);
            }
        }

        private void Unbind()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(TogglePanel);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClosePanel);
            if (inboxTabButton != null)
                inboxTabButton.onClick.RemoveListener(ShowInbox);
            if (sentTabButton != null)
                sentTabButton.onClick.RemoveListener(ShowSent);
            if (claimButton != null)
                claimButton.onClick.RemoveListener(ClaimSelected);
            if (deleteButton != null)
                deleteButton.onClick.RemoveListener(DeleteSelected);
            if (sendButton != null)
                sendButton.onClick.RemoveListener(SendLetter);
        }

        private void TogglePanel()
        {
            if (!IsMailboxAccessEnabled)
            {
                ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(transform);
                if (intro != null && intro.TryShowForCurrentProfile(
                        "mail",
                        "main.info.mail.title",
                        "main.intro.mail.unavailable.black",
                        "main.intro.mail.unavailable.white",
                        onCompleted: ShowMailboxUnavailableNotice))
                {
                    return;
                }

                ShowMailboxUnavailableNotice();
                return;
            }

            if (panelRoot == null)
                return;

            bool show = !panelRoot.activeSelf;
            if (show && !MainHubStateController.CanOpenMainWindow("Mailbox"))
            {
                ClosePanel();
                return;
            }

            panelRoot.SetActive(show);
            MainLobbyUiCoordinator.SetRightStackSuppressed(show);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(show);
            if (show)
            {
                transform.SetAsLastSibling();
                if (MailboxService.I != null)
                {
                    MailboxService.I.RepairClaimedClientAttachmentGifts();
                    MailboxService.I.RefreshFromServer();
                }
                Refresh();

                ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(panelRoot.transform);
                if (intro != null)
                {
                    intro.TryShowForCurrentProfile(
                        "mail",
                        "main.info.mail.title",
                        "main.info.mail.body",
                        "main.intro.mail.white");
                }
            }
        }

        private static void ShowMailboxUnavailableNotice()
        {
            MainSceneResponsiveLayout.ShowDevelopmentNotice("mail.title", "mail.unavailable.body");
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void ShowInbox()
        {
            showingSent = false;
            selectedMessageId = null;
            Refresh();
        }

        private void ShowSent()
        {
            showingSent = true;
            selectedMessageId = null;
            Refresh();
        }

        private void Refresh()
        {
            RefreshToggleLabel();
            RefreshRows();
            RefreshDetail();
            RefreshTabs();
        }

        private void RefreshToggleLabel()
        {
            if (toggleButton == null)
                return;

            if (!IsMailboxAccessEnabled)
            {
                string unavailableLabel = GameLocalization.Text("mail.title") +
                                          "\n<size=72%><color=#FFD75A>" + GameLocalization.Text("main.feature_unavailable.status") + "</color></size>";
                TMP_Text text = toggleButton.GetComponentInChildren<TMP_Text>(true);
                if (text != null)
                {
                    text.richText = true;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 17f;
                    text.fontSizeMax = 28f;
                    text.textWrappingMode = TextWrappingModes.Normal;
                }
                SetButtonLabel(toggleButton, unavailableLabel);
                return;
            }

            int unread = MailboxService.I != null ? MailboxService.I.GetUnreadCount() : 0;
            int gifts = MailboxService.I != null ? MailboxService.I.GetClaimableCount() : 0;
            string label = GameLocalization.Text("mail.title");
            if (unread > 0 || gifts > 0)
                label += "\n" + (gifts > 0 ? GameLocalization.Format("mail.badge_gifts", gifts) : unread.ToString());
            SetButtonLabel(toggleButton, label);
        }

        private void RefreshRows()
        {
            ClearRows();
            if (!IsMailboxAccessEnabled || ProfileService.I == null || ProfileService.I.Current == null)
                return;

            MailboxData mailbox = MailboxService.I != null ? MailboxService.I.GetMailbox() : null;
            if (mailbox == null)
                return;

            List<MailboxMessageData> source = showingSent ? mailbox.PlayerLetters : mailbox.Inbox;
            if (listHeaderText != null)
                listHeaderText.text = GameLocalization.Text(showingSent ? "mail.sent" : "mail.inbox") + "  ·  " + source.Count;

            if (source.Count == 0)
            {
                AddRow(null, GameLocalization.Text(showingSent ? "mail.sent_empty" : "mail.inbox_empty"));
                return;
            }

            bool selectionExists = false;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && string.Equals(source[i].Id, selectedMessageId, System.StringComparison.Ordinal))
                {
                    selectionExists = true;
                    break;
                }
            }

            if (!selectionExists)
                selectedMessageId = source[0].Id;

            for (int i = 0; i < source.Count; i++)
            {
                MailboxMessageData message = source[i];
                AddRow(message, null);
            }

            RefreshRowSelectionVisuals();
            if (listScrollRect != null)
                listScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshDetail()
        {
            MailboxMessageData message = GetSelectedMessage();
            bool hasMessage = message != null;
            bool compose = showingSent;

            if (detailSubjectText != null)
            {
                detailSubjectText.gameObject.SetActive(!compose && hasMessage);
                detailSubjectText.text = hasMessage ? message.Subject : string.Empty;
            }

            if (detailMetaText != null)
            {
                detailMetaText.gameObject.SetActive(!compose && hasMessage);
                detailMetaText.text = hasMessage
                    ? GameLocalization.Format("mail.from", message.SenderName) + "   ·   " + FormatDate(message.CreatedAtUtc)
                    : string.Empty;
            }

            if (detailText != null)
            {
                detailText.gameObject.SetActive(!compose);
                detailText.text = hasMessage ? message.Body : GameLocalization.Text("mail.select_message");
                detailText.color = hasMessage ? Color.white : new Color(0.64f, 0.74f, 0.84f, 0.9f);
            }
            if (detailViewportRect != null)
                detailViewportRect.gameObject.SetActive(!compose);

            bool hasAttachments = hasMessage && message.Attachments != null && message.Attachments.Count > 0;
            if (attachmentsTitleText != null)
            {
                attachmentsTitleText.gameObject.SetActive(!compose && hasAttachments);
                attachmentsTitleText.text = GameLocalization.Text("mail.attachments");
            }

            if (detailDividerImage != null)
                detailDividerImage.gameObject.SetActive(!compose && hasMessage);
            if (attachmentDividerImage != null)
                attachmentDividerImage.gameObject.SetActive(!compose && hasAttachments);

            if (attachmentStatusText != null)
            {
                attachmentStatusText.gameObject.SetActive(!compose && hasAttachments);
                attachmentStatusText.text = hasAttachments
                    ? (message.IsClaimed ? GameLocalization.Text("mail.already_claimed") : GameLocalization.Text("mail.ready_to_claim"))
                    : string.Empty;
                attachmentStatusText.color = message != null && message.IsClaimed
                    ? new Color(0.58f, 0.72f, 0.82f, 1f)
                    : new Color(0.48f, 0.9f, 0.72f, 1f);
            }

            if (subjectInput != null)
                subjectInput.gameObject.SetActive(compose);
            if (bodyInput != null)
                bodyInput.gameObject.SetActive(compose);
            if (sendButton != null)
                sendButton.gameObject.SetActive(compose);
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(!compose && hasMessage && message.HasClaimableAttachments);
                claimButton.interactable = hasMessage && message.HasClaimableAttachments;
            }
            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(!compose && hasMessage);
                deleteButton.interactable = hasMessage && !message.HasClaimableAttachments;
            }

            if (!compose && hasMessage && !message.IsRead && MailboxService.I != null)
                MailboxService.I.MarkRead(message.Id);

            RefreshAttachmentIcons(compose ? null : message);
            RefreshRowSelectionVisuals();
            if (detailText != null && detailText.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailText.rectTransform);
            if (detailScrollRect != null)
                detailScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshTabs()
        {
            SetButtonLabel(inboxTabButton, GameLocalization.Text("mail.inbox"));
            SetButtonLabel(sentTabButton, GameLocalization.Text("mail.sent"));
            SetButtonLabel(claimButton, GameLocalization.Text("mail.claim"));
            SetButtonLabel(deleteButton, "");
            SetButtonLabel(sendButton, GameLocalization.Text("mail.send"));
            if (panelTitleText != null)
                panelTitleText.text = GameLocalization.Text("mail.title");

            ApplyTabVisual(inboxTabButton, !showingSent);
            ApplyTabVisual(sentTabButton, showingSent);
        }

        private void ClaimSelected()
        {
            if (MailboxService.I == null || string.IsNullOrWhiteSpace(selectedMessageId))
                return;

            if (MailboxService.I.ClaimAttachments(selectedMessageId, out string result))
                SetStatus(result);
            else
                SetStatus(result);

            Refresh();
        }

        private void DeleteSelected()
        {
            if (MailboxService.I == null || string.IsNullOrWhiteSpace(selectedMessageId))
                return;

            if (MailboxService.I.DeleteInboxMessage(selectedMessageId, out string result))
                selectedMessageId = null;

            SetStatus(result);
            Refresh();
        }

        private void SendLetter()
        {
            if (MailboxService.I == null)
                return;

            string subject = subjectInput != null ? subjectInput.text : string.Empty;
            string body = bodyInput != null ? bodyInput.text : string.Empty;
            if (MailboxService.I.SubmitPlayerLetter(subject, body, out string result))
            {
                if (subjectInput != null)
                    subjectInput.text = string.Empty;
                if (bodyInput != null)
                    bodyInput.text = string.Empty;
            }

            SetStatus(result);
            Refresh();
        }

        private MailboxMessageData GetSelectedMessage()
        {
            if (!IsMailboxAccessEnabled || ProfileService.I == null || ProfileService.I.Current == null)
                return null;

            MailboxData mailbox = MailboxService.I != null ? MailboxService.I.GetMailbox() : null;
            if (mailbox == null)
                return null;

            List<MailboxMessageData> source = showingSent ? mailbox.PlayerLetters : mailbox.Inbox;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && string.Equals(source[i].Id, selectedMessageId, System.StringComparison.Ordinal))
                    return source[i];
            }

            return null;
        }

        private string BuildDetail(MailboxMessageData message)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<b>" + message.Subject + "</b>");
            builder.AppendLine(GameLocalization.Format("mail.from", message.SenderName));
            builder.AppendLine(FormatDate(message.CreatedAtUtc));
            builder.AppendLine();
            builder.AppendLine(message.Body);

            if (message.Attachments != null && message.Attachments.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("<b>" + GameLocalization.Text("mail.attachments") + "</b>");
                for (int i = 0; i < message.Attachments.Count; i++)
                {
                    MailboxAttachmentData attachment = message.Attachments[i];
                    if (attachment != null && attachment.IsValid)
                        builder.AppendLine("+ " + attachment.Amount + " " + attachment.Label);
                }

                builder.AppendLine(message.IsClaimed ? GameLocalization.Text("mail.already_claimed") : GameLocalization.Text("mail.ready_to_claim"));
            }

            return builder.ToString();
        }

        private void AddRow(MailboxMessageData message, string emptyLabel)
        {
            GameObject rowObject = new GameObject("MailboxRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rowObject.transform.SetParent(listContent, false);
            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.sprite = null;
            rowImage.color = new Color(0.035f, 0.085f, 0.135f, 0.86f);
            Button row = rowObject.GetComponent<Button>();
            row.targetGraphic = rowImage;
            ColorBlock colors = row.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.88f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.65f, 0.68f, 0.72f, 0.7f);
            colors.colorMultiplier = 1f;
            row.colors = colors;

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = message != null ? 86f : 76f;
            element.minHeight = message != null ? 78f : 70f;
            rowButtons.Add(row);
            rowMessageIds.Add(message != null ? message.Id : null);
            Color baseColor = message != null && message.HasClaimableAttachments
                ? new Color(0.045f, 0.115f, 0.18f, 0.94f)
                : new Color(0.028f, 0.07f, 0.115f, 0.9f);
            rowBaseColors.Add(baseColor);

            if (message == null)
            {
                TMP_Text emptyText = CreateText(row.transform, "EmptyLabel", emptyLabel ?? string.Empty, 23f, TextAlignmentOptions.Center);
                emptyText.color = new Color(0.58f, 0.68f, 0.78f, 1f);
                SetStretchRect(emptyText.rectTransform, 18f, 8f, -18f, -8f);
                row.interactable = false;
                return;
            }

            Image unreadDot = CreateSolidImage(row.transform, "UnreadDot", message.IsRead
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(0.25f, 0.72f, 1f, 1f));
            SetAnchoredRect(unreadDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(8f, 42f));

            TMP_Text subject = CreateText(row.transform, "Subject", message.Subject, 27f, TextAlignmentOptions.Left);
            subject.fontStyle = message.IsRead ? FontStyles.Normal : FontStyles.Bold;
            subject.textWrappingMode = TextWrappingModes.NoWrap;
            subject.overflowMode = TextOverflowModes.Ellipsis;
            SetStretchRect(subject.rectTransform, 34f, 34f, -26f, -8f);

            TMP_Text meta = CreateText(row.transform, "Meta", message.SenderName + "   ·   " + FormatDate(message.CreatedAtUtc), 18f, TextAlignmentOptions.Left);
            meta.color = new Color(0.56f, 0.67f, 0.78f, 1f);
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            SetStretchRect(meta.rectTransform, 34f, 8f, -26f, -46f);

            Image giftAccent = CreateSolidImage(row.transform, "GiftAccent", message.HasClaimableAttachments
                ? new Color(0.9f, 0.63f, 0.18f, 1f)
                : new Color(0f, 0f, 0f, 0f));
            SetAnchoredRect(giftAccent.rectTransform, new Vector2(1f, 0.5f), new Vector2(-7f, 0f), new Vector2(5f, 52f));

            int index = rowButtons.Count - 1;
            row.onClick.AddListener(() =>
            {
                selectedMessageId = rowMessageIds[index];
                RefreshDetail();
            });
        }

        private void ClearRows()
        {
            for (int i = 0; i < rowButtons.Count; i++)
            {
                if (rowButtons[i] != null)
                    Destroy(rowButtons[i].gameObject);
            }

            rowButtons.Clear();
            rowMessageIds.Clear();
            rowBaseColors.Clear();
        }

        private void LayoutPanel()
        {
            if (panelRootRect == null)
                return;

            RectTransform rootRect = transform as RectTransform;
            float rootWidth = rootRect != null ? Mathf.Max(480f, rootRect.rect.width) : 2400f;
            float rootHeight = rootRect != null ? Mathf.Max(360f, rootRect.rect.height) : 1080f;
            float safeMarginX = Mathf.Clamp(rootWidth * 0.02f, 12f, 42f);
            float safeMarginY = Mathf.Clamp(rootHeight * 0.025f, 10f, 34f);
            float panelWidth = Mathf.Min(2320f, Mathf.Max(420f, rootWidth - safeMarginX * 2f));
            float panelHeight = Mathf.Min(1060f, Mathf.Max(320f, rootHeight - safeMarginY * 2f));

            SetAnchoredRect(panelRootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(panelWidth, panelHeight));
            SetStretchRect(panelFrameImage != null ? panelFrameImage.rectTransform : null, 0f, 0f, 0f, 0f);

            float left = Mathf.Clamp(panelWidth * 0.045f, 44f, 96f);
            float top = Mathf.Clamp(panelHeight * 0.095f, 66f, 96f);
            float bottom = Mathf.Clamp(panelHeight * 0.065f, 44f, 72f);
            float gap = Mathf.Clamp(panelWidth * 0.022f, 24f, 42f);
            float contentWidth = Mathf.Max(320f, panelWidth - left * 2f);
            float desiredListWidth = Mathf.Clamp(contentWidth * 0.35f, 360f, 620f);
            float minimumDetailWidth = Mathf.Min(620f, contentWidth * 0.58f);
            float listWidth = Mathf.Min(desiredListWidth, Mathf.Max(280f, contentWidth - gap - minimumDetailWidth));
            float tabsY = -top + 24f;
            float contentTop = top + 82f;

            SetAnchoredRect(closeButton != null ? closeButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-52f, -50f), new Vector2(64f, 64f));
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            SetAnchoredRect(panelTitleText != null ? panelTitleText.rectTransform : null, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(420f, 54f));
            float tabWidth = Mathf.Clamp(listWidth * 0.47f, 180f, 260f);
            float tabGap = Mathf.Clamp(listWidth * 0.04f, 12f, 22f);
            SetAnchoredRect(inboxTabButton != null ? inboxTabButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(left + tabWidth * 0.5f, tabsY), new Vector2(tabWidth, 66f));
            SetAnchoredRect(sentTabButton != null ? sentTabButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(left + tabWidth * 1.5f + tabGap, tabsY), new Vector2(tabWidth, 66f));
            SetTopStretchRect(listHeaderText != null ? listHeaderText.rectTransform : null, left + 8f, contentTop - 40f, -panelWidth + left + listWidth - 8f, 34f);

            RectTransform listViewport = listContent != null && listContent.parent != null ? listContent.parent as RectTransform : null;
            SetStretchRect(listViewport, left, bottom + 8f, -panelWidth + left + listWidth, -contentTop);
            ConfigureTopStretchContent(listContent);

            float detailLeft = left + listWidth + gap;
            float detailRight = left;
            SetStretchRect(detailPanelRect, detailLeft, bottom + 8f, -detailRight, -contentTop + 2f);

            float detailPadding = Mathf.Clamp(panelWidth * 0.014f, 20f, 32f);
            float detailTextLeft = detailLeft + detailPadding;
            float detailTextRight = -detailRight - detailPadding;
            float detailHeaderTop = contentTop + 18f;
            SetTopStretchRect(detailSubjectText != null ? detailSubjectText.rectTransform : null, detailTextLeft, detailHeaderTop, detailTextRight, 48f);
            SetTopStretchRect(detailMetaText != null ? detailMetaText.rectTransform : null, detailTextLeft, detailHeaderTop + 50f, detailTextRight, 32f);
            SetTopStretchRect(detailDividerImage != null ? detailDividerImage.rectTransform : null, detailTextLeft, detailHeaderTop + 88f, detailTextRight, 2f);

            float attachmentTitleBottom = bottom + 196f;
            SetStretchRect(detailViewportRect, detailTextLeft, attachmentTitleBottom + 44f, detailTextRight, -detailHeaderTop - 108f);
            ConfigureTopStretchContent(detailText != null ? detailText.rectTransform : null);
            SetBottomStretchRect(attachmentsTitleText != null ? attachmentsTitleText.rectTransform : null, detailTextLeft, attachmentTitleBottom, detailTextRight, 32f);
            SetBottomStretchRect(attachmentDividerImage != null ? attachmentDividerImage.rectTransform : null, detailTextLeft, attachmentTitleBottom - 8f, detailTextRight, 2f);
            SetBottomStretchRect(attachmentIconRoot, detailTextLeft, bottom + 92f, detailTextRight, 88f);
            SetBottomStretchRect(attachmentStatusText != null ? attachmentStatusText.rectTransform : null, detailTextLeft, bottom + 48f, -detailRight - 410f, 34f);

            SetTopStretchRect(subjectInput != null ? subjectInput.transform as RectTransform : null, detailTextLeft, detailHeaderTop + 6f, detailTextRight, 74f);
            SetStretchRect(bodyInput != null ? bodyInput.transform as RectTransform : null, detailTextLeft, bottom + 94f, detailTextRight, -detailHeaderTop - 104f);

            SetAnchoredRect(claimButton != null ? claimButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-detailRight - 154f, bottom + 48f), new Vector2(286f, 78f));
            SetAnchoredRect(deleteButton != null ? deleteButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-detailRight - 354f, bottom + 48f), new Vector2(76f, 76f));
            SetAnchoredRect(sendButton != null ? sendButton.transform as RectTransform : null, new Vector2(1f, 0f), new Vector2(-detailRight - 154f, bottom + 48f), new Vector2(286f, 78f));
            SetBottomStretchRect(statusText != null ? statusText.rectTransform : null, detailTextLeft, bottom + 14f, -detailRight - 390f, 54f);

            ConfigureText(panelTitleText, 42f, 28f);
            ConfigureText(listHeaderText, 26f, 19f);
            ConfigureButtonLabel(inboxTabButton, 31f, 21f);
            ConfigureButtonLabel(sentTabButton, 31f, 21f);
            ConfigureButtonLabel(claimButton, 32f, 23f);
            ConfigureButtonLabel(sendButton, 32f, 23f);
            ConfigureText(detailSubjectText, 36f, 25f);
            ConfigureText(detailMetaText, 23f, 17f);
            ConfigureText(detailText, 29f, 21f);
            ConfigureText(attachmentsTitleText, 25f, 18f);
            ConfigureText(attachmentStatusText, 22f, 16f);
            ConfigureText(statusText, 23f, 17f);
            ConfigureInput(subjectInput, 32f, 22f);
            ConfigureInput(bodyInput, 29f, 20f);
        }

        private void RefreshAttachmentIcons(MailboxMessageData message)
        {
            if (attachmentIconRoot == null)
                return;

            for (int i = attachmentIconRoot.childCount - 1; i >= 0; i--)
                Destroy(attachmentIconRoot.GetChild(i).gameObject);

            bool hasAttachments = message != null && message.Attachments != null && message.Attachments.Count > 0;
            attachmentIconRoot.gameObject.SetActive(hasAttachments);
            if (!hasAttachments)
                return;

            for (int i = 0; i < message.Attachments.Count; i++)
            {
                MailboxAttachmentData attachment = message.Attachments[i];
                if (attachment == null || !attachment.IsValid)
                    continue;

                GameObject cardObject = new GameObject("AttachmentCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                cardObject.transform.SetParent(attachmentIconRoot, false);
                Image cardImage = cardObject.GetComponent<Image>();
                cardImage.color = new Color(0.025f, 0.075f, 0.12f, 0.94f);
                cardImage.raycastTarget = false;
                RectTransform cardRect = cardObject.transform as RectTransform;
                cardRect.sizeDelta = new Vector2(272f, 82f);
                LayoutElement cardLayout = cardObject.GetComponent<LayoutElement>();
                cardLayout.preferredWidth = 272f;
                cardLayout.preferredHeight = 82f;

                Color rarityColor = ResolveAttachmentFallbackColor(attachment);
                Image accent = CreateSolidImage(cardObject.transform, "RarityAccent", rarityColor);
                SetAnchoredRect(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(6f, 62f));

                Image icon = CreateSolidImage(cardObject.transform, "Icon", Color.white);
                icon.sprite = ResolveAttachmentSprite(attachment);
                icon.color = icon.sprite != null ? Color.white : rarityColor;
                icon.preserveAspect = true;
                SetAnchoredRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(60f, 60f));

                if (icon.sprite == null)
                {
                    TMP_Text gemMark = CreateText(icon.transform, "GemMark", "◆", 30f, TextAlignmentOptions.Center);
                    gemMark.color = new Color(0.96f, 0.98f, 1f, 0.96f);
                    SetStretchRect(gemMark.rectTransform, 2f, 2f, -2f, -2f);
                }

                TMP_Text itemName = CreateText(cardObject.transform, "ItemName", ResolveAttachmentDisplayName(attachment), 22f, TextAlignmentOptions.Left);
                itemName.fontStyle = FontStyles.Bold;
                itemName.textWrappingMode = TextWrappingModes.NoWrap;
                itemName.overflowMode = TextOverflowModes.Ellipsis;
                SetStretchRect(itemName.rectTransform, 84f, 35f, -16f, -9f);

                TMP_Text count = CreateText(cardObject.transform, "Count", "×" + attachment.Amount, 19f, TextAlignmentOptions.Left);
                count.color = new Color(0.68f, 0.79f, 0.9f, 1f);
                SetStretchRect(count.rectTransform, 84f, 9f, -16f, -49f);
            }
        }

        private static string ResolveAttachmentDisplayName(MailboxAttachmentData attachment)
        {
            if (attachment == null)
                return GameLocalization.Text("mail.attachment_item");

            if (attachment.Kind == MailboxAttachmentKind.Currency)
            {
                string currencyId = CurrencyWalletEntry.NormalizeCurrencyId(attachment.CurrencyId);
                if (currencyId == CurrencyIds.OzAltin)
                    return "Öz Altın";
                if (currencyId == CurrencyIds.OzAmetist)
                    return "Öz Ametist";
            }

            if (attachment.Kind == MailboxAttachmentKind.BattleTile)
            {
                string rarity = ResolveAttachmentRarity(attachment);
                if (rarity == "epic")
                    return GameLocalization.Text("mail.epic_stone");
                if (rarity == "rare")
                    return GameLocalization.Text("mail.rare_stone");
                if (rarity == "legendary")
                    return GameLocalization.Text("mail.legendary_stone");
                if (rarity == "mythic")
                    return GameLocalization.Text("mail.mythic_stone");

                return GameLocalization.Text("mail.attachment_stone");
            }

            return string.IsNullOrWhiteSpace(attachment.Label)
                ? GameLocalization.Text("mail.attachment_item")
                : attachment.Label.Trim();
        }

        private static Sprite ResolveAttachmentSprite(MailboxAttachmentData attachment)
        {
            if (attachment == null)
                return null;

            if (attachment.Kind == MailboxAttachmentKind.BattleTile)
                return ResolveBattleTileSprite(attachment.ItemId);

            if (!string.IsNullOrWhiteSpace(attachment.IconResourcePath))
            {
                Sprite sprite = Resources.Load<Sprite>(attachment.IconResourcePath);
                if (sprite != null)
                    return sprite;

                Sprite[] sprites = Resources.LoadAll<Sprite>(attachment.IconResourcePath);
                if (sprites != null && sprites.Length > 0)
                    return sprites[0];
            }

            if (attachment.Kind == MailboxAttachmentKind.Currency)
            {
                string id = CurrencyWalletEntry.NormalizeCurrencyId(attachment.CurrencyId);
                if (id == CurrencyIds.OzAltin)
                    return MainLobbyButtonStyle.GoldCurrencySprite;
                if (id == CurrencyIds.OzAmetist)
                    return MainLobbyButtonStyle.AmetistCurrencySprite;
            }

            return null;
        }

        private static string ResolveAttachmentRarityLabel(MailboxAttachmentData attachment)
        {
            string rarity = ResolveAttachmentRarity(attachment);
            if (rarity == "epic")
                return "EPIC";
            if (rarity == "rare")
                return "RARE";
            if (rarity == "legendary")
                return "LEG";
            if (rarity == "mythic")
                return "MYTH";
            return "STONE";
        }

        private static Color ResolveAttachmentFallbackColor(MailboxAttachmentData attachment)
        {
            string rarity = ResolveAttachmentRarity(attachment);
            if (rarity == "epic")
                return new Color(0.42f, 0.16f, 0.78f, 0.96f);
            if (rarity == "rare")
                return new Color(0.08f, 0.38f, 0.88f, 0.96f);
            if (rarity == "legendary")
                return new Color(0.92f, 0.52f, 0.08f, 0.96f);
            if (rarity == "mythic")
                return new Color(0.82f, 0.12f, 0.38f, 0.96f);
            return new Color(0.12f, 0.22f, 0.34f, 0.96f);
        }

        private static string ResolveAttachmentRarity(MailboxAttachmentData attachment)
        {
            if (attachment == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(attachment.Rarity))
                return attachment.Rarity.Trim().ToLowerInvariant();

            string itemId = attachment.ItemId ?? string.Empty;
            if (itemId.IndexOf("epic", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "epic";
            if (itemId.IndexOf("rare", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "rare";

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            string resolvedId = MailboxService.ResolveGiftBattleTileId(store, itemId);
            if (store != null && !string.IsNullOrWhiteSpace(resolvedId) && store.TryGetTileDataById(resolvedId, out BattleTileData data))
                return data.Rarity.ToString().ToLowerInvariant();

            return string.Empty;
        }

        private static Sprite ResolveBattleTileSprite(string tileId)
        {
            if (string.IsNullOrWhiteSpace(tileId))
                return null;

            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            string resolvedId = MailboxService.ResolveGiftBattleTileId(store, tileId);
            if (store == null || string.IsNullOrWhiteSpace(resolvedId) || !store.TryGetTileDataById(resolvedId, out BattleTileData data) || data?.Prefab == null)
                return null;

            return data.Prefab.FaceSprite != null ? data.Prefab.FaceSprite : data.Prefab.BackSprite;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Button button = obj.GetComponent<Button>();
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.06f, 0.13f, 0.22f, 0.94f);
            MainLobbyButtonStyle.Apply(button);
            SetAnchoredRect(obj.transform as RectTransform, anchor, position, size);

            TMP_Text text = CreateText(obj.transform, "Label", label, 28f, TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplyButtonLabelLayout(text);
            return button;
        }

        private static Image CreateSolidImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ApplyTabVisual(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = active
                    ? new Color(0.92f, 0.98f, 1f, 1f)
                    : new Color(0.5f, 0.62f, 0.74f, 0.82f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                label.color = active ? Color.white : new Color(0.7f, 0.78f, 0.86f, 0.9f);
            }
        }

        private void RefreshRowSelectionVisuals()
        {
            int count = Mathf.Min(rowButtons.Count, Mathf.Min(rowMessageIds.Count, rowBaseColors.Count));
            for (int i = 0; i < count; i++)
            {
                Button row = rowButtons[i];
                Image image = row != null ? row.targetGraphic as Image : null;
                if (image == null)
                    continue;

                bool selected = !string.IsNullOrWhiteSpace(rowMessageIds[i]) &&
                                string.Equals(rowMessageIds[i], selectedMessageId, System.StringComparison.Ordinal);
                image.color = selected
                    ? new Color(0.07f, 0.23f, 0.37f, 0.98f)
                    : rowBaseColors[i];
            }
        }

        private static void BuildTrashIcon(Button button)
        {
            if (button == null)
                return;

            Transform parent = button.transform;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.gameObject.SetActive(false);

            Color color = new Color(0.82f, 0.92f, 1f, 0.95f);
            CreateIconRect(parent, "TrashLid", new Vector2(0f, 18f), new Vector2(42f, 6f), color);
            CreateIconRect(parent, "TrashHandle", new Vector2(0f, 27f), new Vector2(20f, 5f), color);
            CreateIconRect(parent, "TrashBody", new Vector2(0f, -4f), new Vector2(34f, 40f), new Color(0.18f, 0.28f, 0.42f, 0.72f));
            CreateIconRect(parent, "TrashLineA", new Vector2(-9f, -4f), new Vector2(4f, 28f), color);
            CreateIconRect(parent, "TrashLineB", new Vector2(9f, -4f), new Vector2(4f, 28f), color);
        }

        private static void CreateIconRect(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            SetAnchoredRect(obj.transform as RectTransform, new Vector2(0.5f, 0.5f), position, size);
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TMP_Text text = obj.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, float fontSize)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.015f, 0.024f, 0.04f, 0.92f);

            TMP_InputField input = obj.GetComponent<TMP_InputField>();
            TMP_Text text = CreateText(obj.transform, "Text", "", fontSize, TextAlignmentOptions.Left);
            TMP_Text hint = CreateText(obj.transform, "Placeholder", placeholder, fontSize, TextAlignmentOptions.Left);
            hint.color = new Color(0.62f, 0.72f, 0.82f, 0.75f);
            SetStretchRect(text.rectTransform, 18f, 10f, -18f, -10f);
            SetStretchRect(hint.rectTransform, 18f, 10f, -18f, -10f);
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null)
                text.text = label;
        }

        private static void ConfigureButtonLabel(Button button, float max, float min)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            ConfigureText(text, max, min);
        }

        private static void ConfigureText(TMP_Text text, float max, float min)
        {
            if (text == null)
                return;

            MainLobbyButtonStyle.ApplyFont(text);
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = min;
        }

        private static void ConfigureInput(TMP_InputField input, float max, float min)
        {
            if (input == null)
                return;

            ConfigureText(input.textComponent, max, min);
            ConfigureText(input.placeholder as TMP_Text, max, min);
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text ?? string.Empty;
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();
        }

        private static string FormatDate(string value)
        {
            if (System.DateTime.TryParse(value, out System.DateTime date))
                return date.ToLocalTime().ToString("dd.MM.yyyy  ·  HH:mm");
            return value;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
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
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(right, -top);
        }

        private static void ConfigureTopStretchContent(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
        }

        private static void SetBottomStretchRect(RectTransform rect, float left, float bottom, float right, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, bottom + height);
        }
    }
}
