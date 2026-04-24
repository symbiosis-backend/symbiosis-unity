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
        private const int RootCanvasSortingOrder = 10021;

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelRootRect;
        [SerializeField] private Image panelImage;
        [SerializeField] private RectTransform panelBackgroundRect;
        [SerializeField] private Image panelBackgroundImage;
        [SerializeField] private RectTransform panelFrameRect;
        [SerializeField] private Image panelFrameImage;
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
        [SerializeField] private float refreshSeconds = 6f;
        [SerializeField] private float badgePulseSpeed = 5.5f;
        [SerializeField] private float badgePulseScale = 0.16f;
        [SerializeField] private Color badgeColor = new Color(1f, 0.18f, 0.22f, 1f);

        private static Sprite notificationBadgeSprite;
        private RectTransform notificationBadgeRect;
        private Image notificationBadgeImage;
        private Coroutine refreshRoutine;
        private bool busy;

        private void OnRectTransformDimensionsChange()
        {
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

        private void OnDisable()
        {
            if (FriendsService.I != null)
            {
                FriendsService.I.FriendsChanged -= RefreshView;
                FriendsService.I.ErrorChanged -= RefreshStatus;
            }

            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            StopRefreshing();
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
            EnsurePanelBackground();
            EnsurePanelFrame();

            titleText = CreateText(panelRoot.transform, "Title", GameLocalization.Text("friends.title"), 30f, TextAlignmentOptions.Left);
            SetOffsets(titleText.rectTransform, new Vector2(22f, -58f), new Vector2(-82f, -14f), new Vector2(0f, 1f), new Vector2(1f, 1f));

            closeButton = CreateButton(panelRoot.transform, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(48f, 48f));
            nicknameInput = CreateInput(panelRoot.transform, "NicknameInput", GameLocalization.Text("friends.nickname"), new Vector2(18f, -110f), new Vector2(-198f, -60f));
            addButton = CreateButton(panelRoot.transform, "AddButton", GameLocalization.Text("friends.add"), new Vector2(1f, 1f), new Vector2(-111f, -85f), new Vector2(150f, 50f));
            refreshButton = CreateButton(panelRoot.transform, "RefreshButton", GameLocalization.Text("friends.refresh"), new Vector2(1f, 1f), new Vector2(-111f, -145f), new Vector2(150f, 50f));

            requestsText = CreateText(panelRoot.transform, "RequestsText", "", 18f, TextAlignmentOptions.TopLeft);
            SetOffsets(requestsText.rectTransform, new Vector2(22f, 360f), new Vector2(-22f, 492f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            requestsText.enableAutoSizing = true;
            requestsText.fontSizeMin = 13f;
            requestsText.fontSizeMax = 18f;

            acceptButton = CreateButton(panelRoot.transform, "AcceptButton", GameLocalization.Text("friends.accept"), new Vector2(0f, 0f), new Vector2(96f, 336f), new Vector2(145f, 44f));
            declineButton = CreateButton(panelRoot.transform, "DeclineButton", GameLocalization.Text("friends.decline"), new Vector2(0f, 0f), new Vector2(248f, 336f), new Vector2(145f, 44f));

            friendsText = CreateText(panelRoot.transform, "FriendsText", "", 19f, TextAlignmentOptions.TopLeft);
            SetOffsets(friendsText.rectTransform, new Vector2(22f, 92f), new Vector2(-22f, 320f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            friendsText.enableAutoSizing = true;
            friendsText.fontSizeMin = 13f;
            friendsText.fontSizeMax = 19f;

            searchText = CreateText(panelRoot.transform, "SearchText", "", 16f, TextAlignmentOptions.TopLeft);
            SetOffsets(searchText.rectTransform, new Vector2(22f, 48f), new Vector2(-22f, 88f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            searchText.color = new Color(0.82f, 0.9f, 1f, 1f);

            statusText = CreateText(panelRoot.transform, "StatusText", "", 15f, TextAlignmentOptions.Left);
            SetOffsets(statusText.rectTransform, new Vector2(22f, 12f), new Vector2(-22f, 36f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            statusText.color = new Color(1f, 0.72f, 0.38f, 1f);

            LayoutPanel();
            panelRoot.SetActive(false);
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
            EnsurePanelBackground();
            EnsurePanelFrame();
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
                panelBackgroundImage.color = new Color(0.015f, 0.022f, 0.035f, 0.98f);
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
        }

        public void LayoutToggleButton()
        {
            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(true);
                toggleButton.transform.SetAsLastSibling();
            }

            RectTransform rect = toggleButton != null ? toggleButton.transform as RectTransform : null;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-210f, 188f);
                rect.sizeDelta = new Vector2(330f, 93f);
            }

            ConfigureButtonLabel(toggleButton, 30f, 18f);
            EnsureToggleBadge();
            RefreshToggleBadge();
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
            const float frameAspect = 1494f / 1024f;
            float maxWidth = Mathf.Max(760f, rootWidth * 0.82f);
            float maxHeight = Mathf.Max(500f, rootHeight * 0.78f);
            float panelWidth = Mathf.Min(maxWidth, maxHeight * frameAspect);
            float panelHeight = panelWidth / frameAspect;
            panelWidth = Mathf.Clamp(panelWidth, Mathf.Min(940f, maxWidth), maxWidth);
            panelHeight = Mathf.Clamp(panelHeight, Mathf.Min(620f, maxHeight), maxHeight);

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
            float contentLeft = insetX + 28f;
            float contentRight = insetX + 12f;
            float buttonHeight = 70f;
            float inputHeight = 70f;
            float actionWidth = 210f;
            float closeSize = 108f;

            SetStretchRect(panelFrameRect, 0f, 0f, 0f, 0f);
            SetStretchRect(panelBackgroundRect, insetX + 12f, insetBottom + 12f, -insetX - 12f, -insetTop - 12f);

            if (titleText != null)
                titleText.gameObject.SetActive(false);

            SetAnchoredRect(closeButton != null ? closeButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-insetX - 62f, -insetTop - 52f), new Vector2(closeSize, closeSize));
            ConfigureButtonLabel(closeButton, 50f, 30f);

            SetTopStretchRect(nicknameInput != null ? nicknameInput.transform as RectTransform : null, contentLeft, insetTop + 116f, -contentRight - actionWidth - 22f, inputHeight);
            ConfigureInputText(nicknameInput, 30f, 18f);
            Image inputImage = nicknameInput != null ? nicknameInput.GetComponent<Image>() : null;
            if (inputImage != null)
                inputImage.color = new Color(0.055f, 0.08f, 0.14f, 0.97f);

            SetAnchoredRect(addButton != null ? addButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-contentRight - actionWidth * 0.5f, -insetTop - 148f), new Vector2(actionWidth, buttonHeight));
            SetAnchoredRect(refreshButton != null ? refreshButton.transform as RectTransform : null, new Vector2(1f, 1f), new Vector2(-contentRight - actionWidth * 0.5f, -insetTop - 232f), new Vector2(actionWidth, buttonHeight));
            ConfigureButtonLabel(addButton, 26f, 16f);
            ConfigureButtonLabel(refreshButton, 26f, 16f);

            float requestsTop = insetTop + 236f;
            float requestsHeight = Mathf.Clamp(panelHeight * 0.18f, 92f, 130f);
            SetTopStretchRect(requestsText != null ? requestsText.rectTransform : null, contentLeft, requestsTop, -contentRight, requestsHeight);
            ConfigureTextSize(requestsText, 28f, 17f);

            SetAnchoredRect(acceptButton != null ? acceptButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(contentLeft + 120f, -requestsTop - requestsHeight - 40f), new Vector2(210f, 60f));
            SetAnchoredRect(declineButton != null ? declineButton.transform as RectTransform : null, new Vector2(0f, 1f), new Vector2(contentLeft + 350f, -requestsTop - requestsHeight - 40f), new Vector2(210f, 60f));
            ConfigureButtonLabel(acceptButton, 22f, 14f);
            ConfigureButtonLabel(declineButton, 22f, 14f);

            float listTop = requestsTop + requestsHeight + 94f;
            float listBottom = insetBottom + 82f;
            SetStretchRect(friendsText != null ? friendsText.rectTransform : null, contentLeft, listBottom, -contentRight, -listTop);
            ConfigureTextSize(friendsText, 29f, 18f);

            SetBottomStretchRect(searchText != null ? searchText.rectTransform : null, contentLeft, insetBottom + 34f, -contentRight, 32f);
            ConfigureTextSize(searchText, 20f, 13f);
            SetBottomStretchRect(statusText != null ? statusText.rectTransform : null, contentLeft, insetBottom - 2f, -contentRight, 30f);
            ConfigureTextSize(statusText, 20f, 13f);

            if (panelFrameRect != null)
                panelFrameRect.SetAsLastSibling();
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
            transform.SetAsLastSibling();
            panelRoot.SetActive(show);

            if (show)
            {
                EnsurePanelReferences();
                LayoutPanel();
                RefreshNow();
            }
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
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
                    yield return FriendsService.I.Refresh();

                yield return new WaitForSecondsRealtime(Mathf.Max(2f, refreshSeconds));
            }
        }

        private void RefreshNow()
        {
            if (FriendsService.I != null)
                StartCoroutine(FriendsService.I.Refresh());
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

            RefreshStatus(ok ? (string.IsNullOrWhiteSpace(message) ? "Request sent." : message) : message);
            if (ok && nicknameInput != null)
                nicknameInput.text = string.Empty;

            SetButtonsInteractable(true);
            busy = false;
        }

        private void SearchByNickname(string value)
        {
            if (FriendsService.I == null || string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
            {
                if (searchText != null)
                    searchText.text = string.Empty;
                return;
            }

            StartCoroutine(SearchRoutine(value.Trim()));
        }

        private IEnumerator SearchRoutine(string nickname)
        {
            bool ok = false;
            FriendsService.FriendUser[] users = null;
            string error = string.Empty;

            yield return FriendsService.I.Search(nickname, (success, message, result) =>
            {
                ok = success;
                error = message;
                users = result;
            });

            if (!ok)
            {
                if (searchText != null)
                    searchText.text = error;
                yield break;
            }

            StringBuilder builder = new StringBuilder();
            if (users != null)
            {
                int count = Mathf.Min(users.Length, 3);
                for (int i = 0; i < count; i++)
                {
                    FriendsService.FriendUser user = users[i];
                    if (user == null)
                        continue;

                    builder.Append(user.nickname);
                    if (user.isFriend)
                        builder.Append(" - friend");
                    else if (user.hasPendingOutgoing)
                        builder.Append(" - requested");
                    else if (user.hasPendingIncoming)
                        builder.Append(" - wants to add you");

                    if (i + 1 < count)
                        builder.Append("   ");
                }
            }

            if (searchText != null)
                searchText.text = builder.Length == 0 ? "No players found." : builder.ToString();
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

            RefreshRequests();
            RefreshFriends();
            RefreshToggleBadge();
        }

        private void RefreshRequests()
        {
            IReadOnlyList<FriendsService.IncomingFriendRequest> incoming = FriendsService.I.IncomingRequests;
            IReadOnlyList<FriendsService.OutgoingFriendRequest> outgoing = FriendsService.I.OutgoingRequests;
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Requests");
            if (incoming.Count == 0 && outgoing.Count == 0)
            {
                builder.Append("No pending requests.");
            }
            else
            {
                for (int i = 0; i < incoming.Count; i++)
                    builder.Append("Incoming: ").Append(Escape(incoming[i].senderNickname)).AppendLine();

                for (int i = 0; i < outgoing.Count; i++)
                    builder.Append("Sent: ").Append(Escape(outgoing[i].receiverNickname)).AppendLine();
            }

            if (requestsText != null)
                requestsText.text = builder.ToString();

            bool hasIncoming = incoming.Count > 0;
            if (acceptButton != null)
                acceptButton.gameObject.SetActive(hasIncoming);
            if (declineButton != null)
                declineButton.gameObject.SetActive(hasIncoming);
        }

        private void RefreshFriends()
        {
            IReadOnlyList<FriendsService.FriendUser> list = FriendsService.I.Friends;
            StringBuilder online = new StringBuilder();
            StringBuilder offline = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                FriendsService.FriendUser friend = list[i];
                if (friend == null)
                    continue;

                StringBuilder target = friend.online ? online : offline;
                target.Append(friend.online ? "[ON] " : "[OFF] ");
                target.Append(Escape(string.IsNullOrWhiteSpace(friend.nickname) ? GameLocalization.Text("common.player") : friend.nickname));
                if (!string.IsNullOrWhiteSpace(friend.publicPlayerId))
                    target.Append("  ").Append(Escape(friend.publicPlayerId));
                target.AppendLine();
            }

            StringBuilder output = new StringBuilder();
            if (online.Length > 0)
                output.Append(online);
            if (online.Length > 0 && offline.Length > 0)
                output.AppendLine();
            if (offline.Length > 0)
                output.Append(offline);
            if (output.Length == 0)
                output.Append(GameLocalization.Text("friends.empty_online"));

            if (friendsText != null)
                friendsText.text = output.ToString();
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

            if (nicknameInput != null && nicknameInput.placeholder is TMP_Text placeholder)
                placeholder.text = GameLocalization.Text("friends.nickname");

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
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(10f, 2f, 10f, 4f);
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

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholderText, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            SetOffsets(rect, offsetMin, offsetMax, new Vector2(0f, 1f), new Vector2(1f, 1f));
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.055f, 0.08f, 0.14f, 0.97f);

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(18f, 4f);
            textAreaRect.offsetMax = new Vector2(-18f, -4f);

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
            text.overflowMode = TextOverflowModes.Ellipsis;
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
