using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class AllianceUI : MonoBehaviour
    {
        private const int RootCanvasSortingOrder = 30120;
        private const string MainSceneName = "Main";
        private const string AllianceSceneName = "Alliance";
        private const string AllianceDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const string AllianceBridgeLandscapeSpritePath = "Mahjong/Sprites/Alliance/AllianceBridgeLandscapeBG";
        private const string AllianceWindowSpritePath = "Mahjong/Sprites/Alliance/WindowDLS";
        private const string MemberActionGearSpritePath = "Mahjong/Sprites/MainSettings/SettingsButtonMain";
        private const string MembersIconSpritePath = "Mahjong/Sprites/Alliance/UyeIcon";
        private static readonly Color WindowFillColor = new Color(0.012f, 0.035f, 0.075f, 1f);
        private static readonly Color WindowBorderColor = new Color(0.78f, 0.86f, 0.94f, 1f);
        private static readonly Color CardFillColor = new Color(0.018f, 0.055f, 0.105f, 0.96f);
        private static readonly Color CardAltFillColor = new Color(0.028f, 0.075f, 0.135f, 0.98f);
        private static readonly Color MutedTextColor = new Color(0.66f, 0.78f, 0.92f, 1f);
        private static readonly Color AccentTextColor = new Color(0.82f, 0.93f, 1f, 1f);
        private static readonly Color ButtonFillColor = new Color(0.035f, 0.075f, 0.13f, 1f);
        private static readonly Color ButtonBorderColor = new Color(0.70f, 0.82f, 0.96f, 1f);
        private static readonly Color PrimaryButtonFillColor = new Color(0.08f, 0.20f, 0.34f, 1f);
        private static readonly Color InputFillColor = new Color(0.006f, 0.014f, 0.032f, 1f);
        private static readonly Vector2 PanelSafeMin = new Vector2(0.055f, 0.065f);
        private static readonly Vector2 PanelSafeMax = new Vector2(0.945f, 0.895f);
        private static readonly Vector2 AllianceInnerMinLandscape = new Vector2(0.072f, 0.125f);
        private static readonly Vector2 AllianceInnerMaxLandscape = new Vector2(0.948f, 0.835f);
        private static readonly Vector2 AllianceInnerMinPortrait = new Vector2(0.075f, 0.125f);
        private static readonly Vector2 AllianceInnerMaxPortrait = new Vector2(0.925f, 0.825f);
        private static readonly Vector2 AllianceContentMinLandscape = new Vector2(0.212f, 0.105f);
        private static Sprite cachedMemberActionGearSprite;
        private static Sprite cachedMembersIconSprite;

        private enum Tab
        {
            Info,
            Members,
            Chat,
            Rewards,
            Treasury,
            Tournaments,
            Events,
            Manage
        }

        private RectTransform rootRect;
        private Button toggleButton;
        private GameObject backdropRoot;
        private GameObject panelRoot;
        private RectTransform contentRect;
        private ScrollRect contentScroll;
        private RectTransform listContent;
        private TMP_Text scrollHintText;
        private TMP_Text titleText;
        private TMP_Text bodyText;
        private TMP_Text statusText;
        private TMP_InputField nameInput;
        private TMP_InputField tagInput;
        private TMP_InputField searchInput;
        private TMP_InputField chatInput;
        private TMP_InputField inviteInput;
        private TMP_InputField announcementInput;
        private TMP_InputField donateOzTileInput;
        private TMP_InputField donateOzGoldInput;
        private RectTransform manageInviteGroup;
        private RectTransform manageFocusGroup;
        private RectTransform memberActionPopup;
        private RectTransform announcementPopup;
        private TMP_Text manageInviteTitle;
        private TMP_Text manageFocusTitle;
        private Button createButton;
        private Button searchButton;
        private Button joinFirstButton;
        private Button acceptInviteButton;
        private Button sendChatButton;
        private Button inviteButton;
        private Button updateButton;
        private Button acceptRequestButton;
        private Button claimChestButton;
        private Button selectChampionButton;
        private Button leaveButton;
        private readonly Button[] tabButtons = new Button[8];
        private readonly Button[] focusButtons = new Button[5];
        private readonly string[] focusKeys = { "any", "mahjong_ranked", "mahjong_duel", "daily_checkin", "mahjong_random_online" };
        private string selectedWeeklyFocus = "any";
        private int selectedMemberUserId;
        private Tab currentTab;
        private Coroutine refreshRoutine;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public static AllianceUI CreateInScene()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();

            GameObject root = new GameObject("AllianceUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ConfigureRootCanvas(root);
            AllianceUI ui = root.AddComponent<AllianceUI>();
            ui.Build();
            return ui;
        }

        private void Awake()
        {
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ConfigureRootCanvas(gameObject);
            Subscribe();
            if (AllianceService.I != null)
            {
                StartCoroutine(AllianceService.I.Refresh());
                StartCoroutine(AllianceService.I.RefreshLeaderboard());
            }
            refreshRoutine = StartCoroutine(RefreshLoop());
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
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDisable()
        {
            Unsubscribe();
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainGameLaunchBootstrap.RefreshVisibilityNow();
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        public void LayoutToggleButton()
        {
            if (toggleButton == null)
                return;

            MainLobbyUiCoordinator.LayoutRightStackButton(toggleButton, MainLobbySideButtonSlot.Alliance);
            RectTransform rect = toggleButton.GetComponent<RectTransform>();
            ApplyMainButtonStyle(toggleButton, toggleButton.GetComponentInChildren<TMP_Text>(true), rect.sizeDelta);
            ApplyUnavailableToggleLabel();
            MainInfoHintTarget.Detach(toggleButton);
        }

        private void ApplyUnavailableToggleLabel()
        {
            TMP_Text label = toggleButton != null ? toggleButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;

            label.richText = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 28f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = GameLocalization.Text("alliance.title") +
                         "\n<size=72%><color=#FFD75A>" + GameLocalization.Text("main.feature_unavailable.status") + "</color></size>";
        }

        private void Build()
        {
            rootRect = GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(transform, "AllianceButton", GameLocalization.Text("alliance.title"), new Vector2(1f, 0f), new Vector2(-210f, 292f), new Vector2(330f, 93f));
            if (IsStandaloneScene())
                toggleButton.gameObject.SetActive(false);
            else
                toggleButton.onClick.AddListener(ShowUnavailableNotice);

            backdropRoot = new GameObject("AllianceBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropRoot.transform.SetParent(transform, false);
            Image backdropImage = backdropRoot.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.72f);
            backdropImage.raycastTarget = true;
            RectTransform backdropRect = backdropRoot.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdropRoot.SetActive(false);

            panelRoot = new GameObject("AlliancePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(transform, false);
            Image panelImage = panelRoot.GetComponent<Image>();
            ApplyAllianceWindowStyle(panelImage);
            panelImage.raycastTarget = true;
            RectTransform panel = panelRoot.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.02f, 0.035f);
            panel.anchorMax = new Vector2(0.98f, 0.965f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            titleText = CreateText(panelRoot.transform, "Title", GameLocalization.Text("alliance.title"), 42f, TextAlignmentOptions.Center);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -38f);
            titleRect.sizeDelta = new Vector2(360f, 62f);

            Button close = CreateButton(panelRoot.transform, "Close", "X", new Vector2(1f, 1f), new Vector2(-72f, -64f), new Vector2(66f, 60f));
            MainLobbyButtonStyle.ApplyCloseIconButton(close);
            close.onClick.AddListener(OnClosePressed);

            CreateTabButton("InfoTab", "alliance.info", Tab.Info, -112f);
            CreateTabButton("MembersTab", "alliance.members", Tab.Members, -112f);
            CreateTabButton("ChatTab", "alliance.chat", Tab.Chat, -112f);
            CreateTabButton("RewardsTab", "alliance.rewards", Tab.Rewards, -112f);
            CreateTabButton("TreasuryTab", "alliance.treasury", Tab.Treasury, -112f);
            CreateTabButton("TournamentsTab", "alliance.tournaments", Tab.Tournaments, -112f);
            CreateTabButton("EventsTab", "alliance.events", Tab.Events, -112f);
            CreateTabButton("ManageTab", "alliance.manage", Tab.Manage, -112f);

            GameObject content = new GameObject("ContentCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            content.transform.SetParent(panelRoot.transform, false);
            ApplyRoundedPanel(content, new Color(0.010f, 0.035f, 0.070f, 0.58f), 22f);
            contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.075f, 0.22f);
            contentRect.anchorMax = new Vector2(0.925f, 0.75f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            manageInviteGroup = CreateManageGroup(panelRoot.transform, "ManageInviteGroup", out manageInviteTitle);
            manageFocusGroup = CreateManageGroup(panelRoot.transform, "ManageFocusGroup", out manageFocusTitle);

            nameInput = CreateInput(panelRoot.transform, "NameInput", GameLocalization.Text("alliance.name"), new Vector2(0.07f, 1f), new Vector2(0.42f, 1f), new Vector2(0f, -474f), new Vector2(0f, 70f));
            tagInput = CreateInput(panelRoot.transform, "TagInput", GameLocalization.Text("alliance.tag"), new Vector2(0.44f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -474f), new Vector2(0f, 70f));
            createButton = CreateButton(panelRoot.transform, "CreateButton", GameLocalization.Text("alliance.create"), new Vector2(0.82f, 1f), new Vector2(0f, -474f), new Vector2(250f, 70f));
            createButton.onClick.AddListener(OnCreate);

            searchInput = CreateInput(panelRoot.transform, "SearchInput", GameLocalization.Text("alliance.search"), new Vector2(0.07f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -558f), new Vector2(0f, 70f));
            searchButton = CreateButton(panelRoot.transform, "SearchButton", GameLocalization.Text("alliance.search"), new Vector2(0.82f, 1f), new Vector2(0f, -558f), new Vector2(250f, 70f));
            searchButton.onClick.AddListener(OnSearch);
            joinFirstButton = CreateButton(panelRoot.transform, "JoinFirstButton", GameLocalization.Text("alliance.join"), new Vector2(0.82f, 1f), new Vector2(0f, -642f), new Vector2(250f, 70f));
            joinFirstButton.onClick.AddListener(OnJoinFirst);
            acceptInviteButton = CreateButton(panelRoot.transform, "AcceptInviteButton", GameLocalization.Text("friends.accept"), new Vector2(0.62f, 1f), new Vector2(0f, -642f), new Vector2(230f, 70f));
            acceptInviteButton.onClick.AddListener(OnAcceptFirstInvite);

            chatInput = CreateInput(panelRoot.transform, "ChatInput", GameLocalization.Text("chat.placeholder"), new Vector2(0.04f, 0f), new Vector2(0.76f, 0f), new Vector2(0f, 52f), new Vector2(0f, 68f));
            sendChatButton = CreateButton(panelRoot.transform, "SendChat", GameLocalization.Text("chat.send"), new Vector2(0.88f, 0f), new Vector2(0f, 52f), new Vector2(190f, 68f));
            sendChatButton.onClick.AddListener(OnSendChat);

            inviteInput = CreateInput(manageInviteGroup, "InviteInput", GameLocalization.Text("battle.duel.nickname"), new Vector2(0.04f, 1f), new Vector2(0.66f, 1f), new Vector2(0f, -52f), new Vector2(0f, 52f));
            inviteButton = CreateButton(manageInviteGroup, "InviteButton", GameLocalization.Text("alliance.invite"), new Vector2(0.78f, 1f), new Vector2(0f, -52f), new Vector2(168f, 52f));
            inviteButton.onClick.AddListener(OnInvite);

            announcementInput = CreateInput(manageFocusGroup, "AnnouncementInput", GameLocalization.Text("alliance.announcement"), new Vector2(0.04f, 1f), new Vector2(0.66f, 1f), new Vector2(0f, -52f), new Vector2(0f, 52f));
            updateButton = CreateButton(manageFocusGroup, "UpdateButton", GameLocalization.Text("alliance.update"), new Vector2(0.78f, 1f), new Vector2(0f, -52f), new Vector2(168f, 52f));
            updateButton.onClick.AddListener(OnUpdate);
            CreateFocusButtons();
            acceptRequestButton = CreateButton(manageInviteGroup, "AcceptRequestButton", GameLocalization.Text("alliance.accept_first"), new Vector2(0.78f, 1f), new Vector2(0f, -110f), new Vector2(168f, 52f));
            acceptRequestButton.onClick.AddListener(OnAcceptFirstRequest);
            claimChestButton = CreateButton(panelRoot.transform, "ClaimChestButton", GameLocalization.Text("alliance.claim_chest"), new Vector2(0.48f, 0f), new Vector2(0f, 278f), new Vector2(230f, 62f));
            claimChestButton.onClick.AddListener(OnClaimChest);
            selectChampionButton = CreateButton(panelRoot.transform, "SelectChampionButton", GameLocalization.Text("alliance.select_champion"), new Vector2(0.50f, 1f), new Vector2(0f, -58f), new Vector2(230f, 52f));
            selectChampionButton.onClick.AddListener(OnSelectChampion);
            leaveButton = CreateButton(panelRoot.transform, "LeaveButton", GameLocalization.Text("battle.common.leave"), new Vector2(0.50f, 1f), new Vector2(0f, -116f), new Vector2(230f, 52f));
            leaveButton.onClick.AddListener(OnLeave);

            CreateContentScroll(content.transform);
            CreateScrollHint(content.transform);

            statusText = CreateText(panelRoot.transform, "Status", "", 22f, TextAlignmentOptions.Left);
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(PanelSafeMin.x, 0f);
            statusRect.anchorMax = new Vector2(PanelSafeMax.x, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 28f);
            statusRect.sizeDelta = new Vector2(0f, 44f);

            LayoutToggleButton();
            panelRoot.SetActive(false);
            if (backdropRoot != null)
                backdropRoot.SetActive(false);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            if (IsStandaloneScene())
                SetPanelVisible(true);
            RefreshView();
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                UpdateScrollHint();
                ClosePopupsOnFreeClick();
            }

            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
                return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            LayoutToggleButton();
            if (panelRoot != null && panelRoot.activeSelf)
                RefreshView();
        }

        private void Subscribe()
        {
            if (AllianceService.I == null)
                return;

            AllianceService.I.AllianceChanged += RefreshView;
            AllianceService.I.ChatChanged += RefreshView;
            AllianceService.I.SearchChanged += RefreshView;
            AllianceService.I.LeaderboardChanged += RefreshView;
            AllianceService.I.ErrorChanged += SetStatus;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void Unsubscribe()
        {
            if (AllianceService.I != null)
            {
                AllianceService.I.AllianceChanged -= RefreshView;
                AllianceService.I.ChatChanged -= RefreshView;
                AllianceService.I.SearchChanged -= RefreshView;
                AllianceService.I.LeaderboardChanged -= RefreshView;
                AllianceService.I.ErrorChanged -= SetStatus;
            }
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                if (panelRoot != null && panelRoot.activeSelf && AllianceService.I != null)
                {
                    if (AllianceService.I.HasAlliance)
                        yield return AllianceService.I.RefreshChat();
                    yield return AllianceService.I.RefreshLeaderboard();
                }
                yield return new WaitForSecondsRealtime(12f);
            }
        }

        private void TogglePanel()
        {
            if (IsStandaloneScene())
            {
                SetPanelVisible(true);
                return;
            }

            bool next = panelRoot != null && !panelRoot.activeSelf;
            SetPanelVisible(next);
            if (next && AllianceService.I != null)
            {
                StartCoroutine(AllianceService.I.Refresh());
                StartCoroutine(AllianceService.I.RefreshLeaderboard());
            }
        }

        private void ShowUnavailableNotice()
        {
            ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(transform);
            if (intro != null && intro.TryShowForCurrentProfile(
                    "alliance",
                    "main.info.alliance.title",
                    "main.intro.alliance.unavailable.black",
                    "main.intro.alliance.unavailable.white",
                    onCompleted: ShowAllianceUnavailableNotice))
            {
                return;
            }

            ShowAllianceUnavailableNotice();
        }

        private static void ShowAllianceUnavailableNotice()
        {
            MainSceneResponsiveLayout.ShowDevelopmentNotice("alliance.unavailable.title", "alliance.unavailable.body");
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot == null)
                return;

            if (visible && !IsStandaloneScene())
            {
                ShowUnavailableNotice();
                return;
            }

            if (!visible && IsStandaloneScene())
            {
                LoadMainScene();
                return;
            }

            panelRoot.SetActive(visible);
            if (backdropRoot != null)
                backdropRoot.SetActive(visible);
            if (!visible)
                MainHubStateController.NotifyMainWindowClosed();
            MainLobbyUiCoordinator.SetRightStackSuppressed(visible);
            if (!visible)
            {
                ClearMemberActionPopup();
                ClearAnnouncementPopup();
            }
            if (visible)
            {
                SettingsMenuUI.ForceCloseAllSettingsMenus();
                if (backdropRoot != null)
                    backdropRoot.transform.SetAsLastSibling();
                panelRoot.transform.SetAsLastSibling();
                LayoutTabButtons();
                RefreshView();

                Transform introParent = backdropRoot != null ? backdropRoot.transform : panelRoot.transform;
                ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(introParent);
                if (intro != null)
                {
                    intro.TryShowForCurrentProfile(
                        "alliance",
                        "main.info.alliance.title",
                        "main.info.alliance.body",
                        "main.intro.alliance.white");
                }
            }
        }

        private void OnClosePressed()
        {
            if (IsStandaloneScene())
                LoadMainScene();
            else
                SetPanelVisible(false);
        }

        private static bool IsStandaloneScene()
        {
            return string.Equals(SceneManager.GetActiveScene().name, AllianceSceneName, System.StringComparison.Ordinal);
        }

        private static void LoadAllianceScene()
        {
            LoadScene(AllianceSceneName);
        }

        private static void LoadMainScene()
        {
            LoadScene(MainSceneName);
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("[AllianceUI] Scene is not available in build settings: " + sceneName);
                return;
            }

            DoorFx doorFx = DoorFx.EnsureRuntime();
            if (doorFx != null && doorFx.IsReady())
                doorFx.LoadScene(sceneName, AllianceDoorSpriteResourcePath);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void CreateTabButton(string name, string key, Tab tab, float y)
        {
            Button button = CreateButton(panelRoot.transform, name, GameLocalization.Text(key), new Vector2(0.075f, 1f), new Vector2(0f, y), new Vector2(260f, 64f));
            int index = (int)tab;
            tabButtons[index] = button;
            button.onClick.AddListener(() =>
            {
                currentTab = tab;
                RefreshView();
            });
            LayoutTabButtons();
            RefreshTabButtons();
        }

        private void LayoutTabButtons()
        {
            bool landscape = IsLandscapeLayout();
            Vector2 innerMin = landscape ? AllianceInnerMinLandscape : AllianceInnerMinPortrait;
            Vector2 anchor = landscape ? new Vector2(0.026f, 1f) : new Vector2(innerMin.x, 1f);
            Vector2 size = landscape ? new Vector2(304f, 74f) : new Vector2(272f, 70f);
            float xStep = landscape ? 0f : 300f;
            float yStep = landscape ? -78f : -82f;
            float topY = landscape ? -72f : -132f;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                    continue;

                int column = landscape ? 0 : i % 3;
                int row = landscape ? i : i / 3;
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.anchoredPosition = new Vector2(column * xStep, topY + row * yStep);
                rect.sizeDelta = size;
                ApplyMainButtonStyle(button, button.GetComponentInChildren<TMP_Text>(true), size);
            }
        }

        private void RefreshTabButtons()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                    continue;

                SetButtonText(button, GameLocalization.Text(GetTabKey((Tab)i)));
                Image image = button.GetComponent<Image>();
                if (image != null)
                    image.color = i == (int)currentTab ? new Color(0.70f, 0.84f, 1f, 1f) : new Color(0.92f, 0.96f, 1f, 1f);
                ApplyTabSelectedStyle(button, i == (int)currentTab);
            }
        }

        private static string GetTabKey(Tab tab)
        {
            if (tab == Tab.Members)
                return "alliance.members";
            if (tab == Tab.Chat)
                return "alliance.chat";
            if (tab == Tab.Rewards)
                return "alliance.rewards";
            if (tab == Tab.Treasury)
                return "alliance.treasury";
            if (tab == Tab.Tournaments)
                return "alliance.tournaments";
            if (tab == Tab.Events)
                return "alliance.events";
            if (tab == Tab.Manage)
                return "alliance.manage";
            return "alliance.info";
        }

        private static bool IsLandscapeLayout()
        {
            return Screen.width > Screen.height && Screen.height > 0;
        }

        private void CreateFocusButtons()
        {
            string[] labels =
            {
                GameLocalization.Text("alliance.focus.any"),
                GameLocalization.Text("alliance.focus.ranked"),
                GameLocalization.Text("alliance.focus.duel"),
                GameLocalization.Text("alliance.focus.daily"),
                GameLocalization.Text("alliance.focus.random")
            };
            for (int i = 0; i < focusButtons.Length; i++)
            {
                int index = i;
                Button button = CreateButton(manageFocusGroup, "Focus" + i, labels[i], new Vector2(0.04f, 1f), new Vector2(i * 122f, -112f), new Vector2(114f, 46f));
                button.onClick.AddListener(() =>
                {
                    selectedWeeklyFocus = focusKeys[index];
                    RefreshFocusButtons();
                });
                focusButtons[i] = button;
            }
            RefreshFocusButtons();
        }

        private void RefreshFocusButtons()
        {
            for (int i = 0; i < focusButtons.Length; i++)
            {
                if (focusButtons[i] == null)
                    continue;

                bool selected = focusKeys[i] == selectedWeeklyFocus;
                Image image = focusButtons[i].GetComponent<Image>();
                if (image != null)
                    image.color = selected ? new Color(0.82f, 0.94f, 1f, 1f) : new Color(0.88f, 0.94f, 1f, 0.92f);

                TMP_Text label = focusButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = selected ? "> " + GameLocalization.Text(GetFocusLocalizationKey(focusKeys[i])) : GameLocalization.Text(GetFocusLocalizationKey(focusKeys[i]));
                    label.color = selected ? new Color(1f, 0.95f, 0.70f, 1f) : new Color(0.92f, 0.96f, 1f, 1f);
                    label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                    label.fontSize = selected ? 31f : 28f;
                    label.fontSizeMax = label.fontSize;
                    label.fontSizeMin = 20f;
                    label.enableAutoSizing = true;
                }

                Outline outline = focusButtons[i].GetComponent<Outline>();
                if (outline == null)
                    outline = focusButtons[i].gameObject.AddComponent<Outline>();
                outline.enabled = selected;
                outline.effectColor = new Color(0.95f, 0.85f, 0.45f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        private static string GetFocusLocalizationKey(string focus)
        {
            if (focus == "mahjong_ranked") return "alliance.focus.ranked";
            if (focus == "mahjong_duel") return "alliance.focus.duel";
            if (focus == "daily_checkin") return "alliance.focus.daily";
            if (focus == "mahjong_random_online") return "alliance.focus.random";
            return "alliance.focus.any";
        }

        private void CreateContentScroll(Transform parent)
        {
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(parent, false);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(18f, 18f);
            viewportRect.offsetMax = new Vector2(-18f, -18f);

            GameObject content = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            listContent = content.GetComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            contentScroll = parent.gameObject.AddComponent<ScrollRect>();
            contentScroll.viewport = viewportRect;
            contentScroll.content = listContent;
            contentScroll.horizontal = false;
            contentScroll.vertical = true;
            contentScroll.movementType = ScrollRect.MovementType.Clamped;
            contentScroll.scrollSensitivity = 36f;
        }

        private void CreateScrollHint(Transform parent)
        {
            scrollHintText = CreateText(parent, "ScrollHint", "▼", 34f, TextAlignmentOptions.Center);
            scrollHintText.raycastTarget = false;
            scrollHintText.color = new Color(0.72f, 0.90f, 1f, 0f);
            RectTransform rect = scrollHintText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(90f, 42f);
            scrollHintText.gameObject.SetActive(false);
        }

        private void UpdateScrollHint()
        {
            if (scrollHintText == null || contentScroll == null || contentScroll.viewport == null || listContent == null)
                return;

            bool scrollable = listContent.rect.height > contentScroll.viewport.rect.height + 12f;
            bool hasMoreBelow = contentScroll.verticalNormalizedPosition > 0.025f;
            bool visible = panelRoot != null && panelRoot.activeSelf && scrollable && hasMoreBelow;
            scrollHintText.gameObject.SetActive(visible);
            if (!visible)
                return;

            float pulse = 0.34f + 0.54f * Mathf.PingPong(Time.unscaledTime * 1.65f, 1f);
            scrollHintText.color = new Color(0.72f, 0.90f, 1f, pulse);
        }

        private void ClosePopupsOnFreeClick()
        {
            if (memberActionPopup == null && announcementPopup == null)
                return;

            bool pressed = Input.GetMouseButtonDown(0);
            Vector2 position = Input.mousePosition;
            if (!pressed && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pressed = true;
                position = Input.GetTouch(0).position;
            }
            if (!pressed)
                return;

            if (IsScreenPointInside(memberActionPopup, position) || IsScreenPointInside(announcementPopup, position))
                return;

            ClearMemberActionPopup();
            ClearAnnouncementPopup();
        }

        private static bool IsScreenPointInside(RectTransform rect, Vector2 screenPoint)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        private void RefreshView()
        {
            if (listContent == null)
                return;

            AllianceService service = AllianceService.I;
            if (service == null)
            {
                ClearRows();
                AddTextCard(GameLocalization.Text("alliance.error_profile"), 30f, AccentTextColor, TextAlignmentOptions.Center);
                return;
            }

            SetStatus(service.LastError);
            bool hasAlliance = service.HasAlliance;
            bool noAlliance = !hasAlliance;
            bool chatTab = hasAlliance && currentTab == Tab.Chat;
            bool manageTab = hasAlliance && currentTab == Tab.Manage;
            bool rewardsTab = hasAlliance && currentTab == Tab.Rewards;
            ApplyPanelModeLayout(hasAlliance, chatTab, manageTab, rewardsTab);
            if (contentRect != null)
                contentRect.gameObject.SetActive(!(manageTab && service.PendingRequests.Count == 0));
            LayoutTabButtons();
            RefreshTabButtons();
            RefreshHeaderTitle(hasAlliance);
            nameInput.gameObject.SetActive(!hasAlliance);
            tagInput.gameObject.SetActive(!hasAlliance);
            searchInput.gameObject.SetActive(!hasAlliance);
            chatInput.gameObject.SetActive(chatTab);
            inviteInput.gameObject.SetActive(manageTab && service.CanManage);
            announcementInput.gameObject.SetActive(false);
            for (int i = 0; i < focusButtons.Length; i++)
                SetActive(focusButtons[i], manageTab && service.IsLeader);
            SetActive(createButton, noAlliance);
            SetActive(searchButton, noAlliance);
            SetActive(joinFirstButton, noAlliance && service.SearchResults.Count > 0);
            SetActive(acceptInviteButton, noAlliance && service.IncomingInvites.Count > 0);
            SetActive(sendChatButton, chatTab);
            SetActive(inviteButton, manageTab && service.CanManage);
            SetActive(updateButton, manageTab && service.IsLeader);
            SetActive(acceptRequestButton, manageTab && service.CanManage && service.PendingRequests.Count > 0);
            SetActive(claimChestButton, hasAlliance && currentTab == Tab.Rewards);
            ConfigureClaimChestButton(service);
            SetActive(selectChampionButton, false);
            SetActive(leaveButton, false);
            SetActive(manageInviteGroup, manageTab && service.CanManage);
            SetActive(manageFocusGroup, manageTab && service.IsLeader);
            RefreshManageGroupTitles(service);

            if (!hasAlliance)
            {
                RenderNoAlliance(service);
                return;
            }

            if (service.Current != null && !string.IsNullOrWhiteSpace(service.Current.weeklyFocus) && currentTab != Tab.Manage)
            {
                selectedWeeklyFocus = service.Current.weeklyFocus;
                RefreshFocusButtons();
            }

            if (currentTab == Tab.Info) RenderInfo(service);
            else if (currentTab == Tab.Members) RenderMembers(service);
            else if (currentTab == Tab.Chat) RenderChat(service);
            else if (currentTab == Tab.Rewards) RenderRewards(service);
            else if (currentTab == Tab.Treasury) RenderTreasury(service);
            else if (currentTab == Tab.Tournaments) RenderTournaments(service);
            else if (currentTab == Tab.Events) RenderEvents(service);
            else if (currentTab == Tab.Manage) RenderManage(service);

            Canvas.ForceUpdateCanvases();
            UpdateScrollHint();
        }

        private void ApplyPanelModeLayout(bool hasAlliance, bool chatTab, bool manageTab, bool rewardsTab)
        {
            bool landscape = IsLandscapeLayout();
            Vector2 innerMin = landscape ? AllianceInnerMinLandscape : AllianceInnerMinPortrait;
            Vector2 innerMax = landscape ? AllianceInnerMaxLandscape : AllianceInnerMaxPortrait;
            Vector2 contentMin = landscape ? AllianceContentMinLandscape : innerMin;
            RectTransform panel = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
            if (panel != null)
            {
                panel.anchorMin = landscape ? new Vector2(0.025f, 0.06f) : new Vector2(0.02f, 0.035f);
                panel.anchorMax = landscape ? new Vector2(0.975f, 0.94f) : new Vector2(0.98f, 0.965f);
                panel.offsetMin = Vector2.zero;
                panel.offsetMax = Vector2.zero;
            }

            if (contentRect != null)
            {
                if (landscape)
                {
                    contentRect.anchorMin = manageTab ? new Vector2(contentMin.x, 0.535f) : chatTab ? new Vector2(contentMin.x, 0.235f) : hasAlliance ? new Vector2(contentMin.x, 0.145f) : new Vector2(contentMin.x, 0.145f);
                    contentRect.anchorMax = manageTab ? new Vector2(innerMax.x, 0.775f) : chatTab ? new Vector2(innerMax.x, 0.805f) : hasAlliance ? new Vector2(innerMax.x, 0.775f) : new Vector2(innerMax.x, 0.530f);
                }
                else
                {
                    contentRect.anchorMin = hasAlliance ? new Vector2(innerMin.x, 0.18f) : new Vector2(innerMin.x, 0.20f);
                    contentRect.anchorMax = hasAlliance ? new Vector2(innerMax.x, 0.68f) : new Vector2(innerMax.x, 0.56f);
                }
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;
            }

            if (landscape)
            {
                SetInputRect(nameInput, new Vector2(contentMin.x, 1f), new Vector2(0.45f, 1f), new Vector2(0f, -230f), new Vector2(0f, 60f));
                SetInputRect(tagInput, new Vector2(0.47f, 1f), new Vector2(0.59f, 1f), new Vector2(0f, -230f), new Vector2(0f, 60f));
                SetButtonRect(createButton, new Vector2(0.70f, 1f), new Vector2(0f, -230f), new Vector2(218f, 60f));
                SetInputRect(searchInput, new Vector2(contentMin.x, 1f), new Vector2(0.59f, 1f), new Vector2(0f, -300f), new Vector2(0f, 60f));
                SetButtonRect(searchButton, new Vector2(0.70f, 1f), new Vector2(0f, -300f), new Vector2(218f, 60f));
                SetButtonRect(joinFirstButton, new Vector2(0.84f, 1f), new Vector2(0f, -300f), new Vector2(218f, 60f));
                SetButtonRect(acceptInviteButton, new Vector2(0.84f, 1f), new Vector2(0f, -230f), new Vector2(218f, 60f));
            }
            else
            {
                SetInputRect(nameInput, new Vector2(innerMin.x, 1f), new Vector2(0.42f, 1f), new Vector2(0f, -494f), new Vector2(0f, 70f));
                SetInputRect(tagInput, new Vector2(0.44f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -494f), new Vector2(0f, 70f));
                SetButtonRect(createButton, new Vector2(0.82f, 1f), new Vector2(0f, -494f), new Vector2(250f, 70f));
                SetInputRect(searchInput, new Vector2(innerMin.x, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -584f), new Vector2(0f, 70f));
                SetButtonRect(searchButton, new Vector2(0.82f, 1f), new Vector2(0f, -584f), new Vector2(250f, 70f));
                SetButtonRect(joinFirstButton, new Vector2(0.82f, 1f), new Vector2(0f, -674f), new Vector2(250f, 70f));
                SetButtonRect(acceptInviteButton, new Vector2(0.62f, 1f), new Vector2(0f, -674f), new Vector2(230f, 70f));
            }

            if (chatTab)
            {
                if (landscape)
                {
                    SetInputRect(chatInput, new Vector2(contentMin.x, 0f), new Vector2(0.762f, 0f), new Vector2(0f, 98f), new Vector2(0f, 62f));
                    SetButtonRectWithPivot(sendChatButton, new Vector2(0.762f, 0f), new Vector2(0f, 1f), new Vector2(12f, 98f), new Vector2(184f, 62f));
                }
                else
                {
                    SetInputRect(chatInput, new Vector2(innerMin.x, 0f), new Vector2(0.725f, 0f), new Vector2(0f, 92f), new Vector2(0f, 62f));
                    SetButtonRectWithPivot(sendChatButton, new Vector2(0.725f, 0f), new Vector2(0f, 1f), new Vector2(12f, 92f), new Vector2(168f, 62f));
                }
            }

            if (manageTab)
            {
                if (landscape)
                {
                    SetManageGroupRect(manageInviteGroup, contentMin.x, 0.505f, 0.735f, 0.170f);
                    SetManageGroupRect(manageFocusGroup, contentMin.x, 0.190f, 0.735f, 0.255f);
                    SetInputRect(inviteInput, new Vector2(0.035f, 1f), new Vector2(0.690f, 1f), new Vector2(0f, -74f), new Vector2(0f, 72f));
                    SetButtonRect(inviteButton, new Vector2(0.825f, 1f), new Vector2(0f, -74f), new Vector2(210f, 72f));
                    SetButtonRect(acceptRequestButton, new Vector2(0.825f, 1f), new Vector2(0f, -148f), new Vector2(210f, 64f));
                    SetInputRect(announcementInput, new Vector2(0.035f, 1f), new Vector2(0.690f, 1f), new Vector2(0f, -74f), new Vector2(0f, 72f));
                    SetButtonRect(updateButton, new Vector2(0.825f, 1f), new Vector2(0f, -74f), new Vector2(210f, 72f));
                    SetButtonRect(selectChampionButton, new Vector2(0.50f, 1f), new Vector2(0f, -74f), new Vector2(230f, 64f));
                    SetButtonRect(leaveButton, new Vector2(0.50f, 1f), new Vector2(0f, -146f), new Vector2(230f, 64f));
                    for (int i = 0; i < focusButtons.Length; i++)
                    {
                        int row = i / 3;
                        int column = i % 3;
                        SetButtonRect(focusButtons[i], new Vector2(0.035f, 1f), new Vector2(column * 204f, -110f - row * 76f), new Vector2(192f, 66f));
                    }
                }
                else
                {
                    SetManageGroupRect(manageInviteGroup, innerMin.x, 0.48f, 0.40f, 0.14f);
                    SetManageGroupRect(manageFocusGroup, innerMin.x, 0.28f, 0.82f, 0.17f);
                    SetInputRect(inviteInput, new Vector2(0.04f, 1f), new Vector2(0.60f, 1f), new Vector2(0f, -58f), new Vector2(0f, 56f));
                    SetButtonRect(inviteButton, new Vector2(0.78f, 1f), new Vector2(0f, -58f), new Vector2(210f, 56f));
                    SetButtonRect(acceptRequestButton, new Vector2(0.78f, 1f), new Vector2(0f, -120f), new Vector2(210f, 56f));
                    SetInputRect(announcementInput, new Vector2(0.04f, 1f), new Vector2(0.60f, 1f), new Vector2(0f, -58f), new Vector2(0f, 56f));
                    SetButtonRect(updateButton, new Vector2(0.78f, 1f), new Vector2(0f, -58f), new Vector2(210f, 56f));
                    SetButtonRect(selectChampionButton, new Vector2(0.50f, 1f), new Vector2(0f, -64f), new Vector2(220f, 56f));
                    SetButtonRect(leaveButton, new Vector2(0.50f, 1f), new Vector2(0f, -126f), new Vector2(220f, 56f));
                    for (int i = 0; i < focusButtons.Length; i++)
                    {
                        int row = i / 3;
                        int column = i % 3;
                        SetButtonRect(focusButtons[i], new Vector2(0.04f, 1f), new Vector2(column * 132f, -78f - row * 52f), new Vector2(124f, 46f));
                    }
                }
            }

            if (rewardsTab)
            {
                if (landscape)
                    SetButtonRect(claimChestButton, new Vector2(innerMax.x, 0.775f), new Vector2(-28f, -48f), new Vector2(230f, 58f));
                else
                    SetButtonRect(claimChestButton, new Vector2(innerMax.x, 0.68f), new Vector2(-28f, -52f), new Vector2(230f, 62f));
            }
        }

        private void RenderNoAlliance(AllianceService service)
        {
            ClearRows();
            AddHeroCard(GameLocalization.Text("alliance.no_alliance"), GameLocalization.Text("alliance.hint_select"));
            AddSectionTitle(GameLocalization.Text("alliance.create"));
            AddActionGuideCard();
            AddSectionTitle(GameLocalization.Text("alliance.invites"));
            if (service.IncomingInvites.Count == 0)
                AddCompactEmptyRow(GameLocalization.Text("alliance.empty_invites"));
            else for (int i = 0; i < service.IncomingInvites.Count; i++)
            {
                AllianceInvite invite = service.IncomingInvites[i];
                AddAllianceResultRow("[" + invite.allianceTag + "] " + invite.allianceName, FormatLevel(invite.allianceLevel), LocalizeVisibility(invite.status));
            }
            AddSectionTitle(GameLocalization.Text("alliance.search_results"));
            if (service.SearchResults.Count == 0)
                AddCompactEmptyRow(GameLocalization.Text("alliance.search_hint"));
            else for (int i = 0; i < service.SearchResults.Count; i++)
            {
                AllianceSummary alliance = service.SearchResults[i];
                AddAllianceResultRow("[" + alliance.tag + "] " + alliance.name, FormatLevel(alliance.level) + "  " + alliance.memberCount + "/" + alliance.maxMembers, LocalizeVisibility(alliance.visibility));
            }
        }

        private void RenderInfo(AllianceService service)
        {
            AllianceSummary a = service.Current;
            ClearRows();
            float progress = a.nextLevelXp > 0 ? Mathf.Clamp01((float)a.xp / a.nextLevelXp) : 1f;
            AddInfoTopRow(service, a);
            AddProgressCard("XP", progress, a.xp + " / " + a.nextLevelXp);
            AddStatsGrid(
                GameLocalization.Text("alliance.level"), a.level.ToString(),
                GameLocalization.Text("alliance.weekly_focus"), LocalizeFocus(a.weeklyFocus),
                GameLocalization.Text("alliance.role"), LocalizeRole(a.viewerRole),
                GameLocalization.Text("alliance.weekly"), a.weeklyPoints + " " + GameLocalization.Text("alliance.points_short"));
            AddSectionTitle(GameLocalization.Text("alliance.recent_activity"));
            int recentCount = Mathf.Min(3, service.Activity.Count);
            if (recentCount == 0)
                AddEmptyRow();
            else for (int i = 0; i < recentCount; i++)
            {
                AddActivityRow(service.Activity[i]);
            }
        }

        private void RenderMembers(AllianceService service)
        {
            ClearRows();
            AllianceSummary alliance = service.Current;
            AddHeaderCard(
                GameLocalization.Text("alliance.members"),
                alliance != null ? alliance.memberCount + "/" + alliance.maxMembers + "  |  " + GameLocalization.Text("alliance.weekly") : string.Empty,
                LoadMembersIconSprite());
            AddTableHeader(GameLocalization.Text("common.player"), GameLocalization.Text("alliance.role"), GameLocalization.Text("alliance.weekly"), GameLocalization.Text("alliance.rank"));
            for (int i = 0; i < service.Members.Count; i++)
            {
                AllianceMember member = service.Members[i];
                AddMemberRow(member, service);
            }
            if (service.Members.Count == 0)
                AddEmptyRow();
        }

        private void RenderChat(AllianceService service)
        {
            ClearRows();
            for (int i = 0; i < service.ChatMessages.Count; i++)
            {
                AllianceChatMessage message = service.ChatMessages[i];
                AddChatRow(message);
            }
            if (service.ChatMessages.Count == 0)
                AddChatEmptyCard(GameLocalization.Text("chat.empty"));
            Canvas.ForceUpdateCanvases();
            if (contentScroll != null)
                contentScroll.verticalNormalizedPosition = 0f;
        }

        private void RenderRewards(AllianceService service)
        {
            AllianceSummary a = service.Current;
            ClearRows();
            int nextTierPoints = GetNextChestTierPoints(service, a.weeklyPoints);
            AllianceMember ownMember = FindOwnMember(service);
            int ownWeekly = service.Chest != null ? Mathf.Max(0, service.Chest.playerContribution) : ownMember != null ? Mathf.Max(0, ownMember.weeklyContributionPoints) : 0;
            int requiredPersonal = GetWeeklyChestMinContribution(service);
            int missingPersonal = Mathf.Max(0, requiredPersonal - ownWeekly);
            string tierText = "Tier " + a.weeklyChestTier;
            AddRewardHero(tierText, GetChestStatusText(service, missingPersonal), missingPersonal);
            AddProgressCard(GameLocalization.Text("alliance.my_contribution"), Mathf.Clamp01((float)ownWeekly / requiredPersonal), ownWeekly + " / " + requiredPersonal);
            if (nextTierPoints > 0)
                AddProgressCard(GameLocalization.Text("alliance.weekly"), Mathf.Clamp01((float)a.weeklyPoints / nextTierPoints), a.weeklyPoints + " / " + nextTierPoints);
            else
                AddProgressCard(GameLocalization.Text("alliance.weekly"), 1f, a.weeklyPoints + " " + GameLocalization.Text("alliance.points_short"));
            AddStatsGrid(
                GameLocalization.Text("alliance.chest"), tierText,
                GameLocalization.Text("alliance.my_contribution"), ownWeekly.ToString(),
                GameLocalization.Text("alliance.next_chest"), nextTierPoints > 0 ? nextTierPoints.ToString() : GameLocalization.Text("alliance.max_tier"),
                GameLocalization.Text("alliance.need_more"), missingPersonal > 0 ? missingPersonal.ToString() : GameLocalization.Text("alliance.ready"));
        }

        private void RenderTreasury(AllianceService service)
        {
            AllianceSummary a = service.Current;
            ClearRows();
            if (a == null)
            {
                AddEmptyRow();
                return;
            }

            AllianceTournamentState tournament = service.Tournament;
            AllianceMember ownMember = FindOwnMember(service);
            int ownWeekly = service.Chest != null ? Mathf.Max(0, service.Chest.playerContribution) : ownMember != null ? Mathf.Max(0, ownMember.weeklyContributionPoints) : 0;
            int requiredPersonal = GetWeeklyChestMinContribution(service);
            int nextTierPoints = GetNextChestTierPoints(service, a.weeklyPoints);
            int fundOzTile = tournament != null ? Mathf.Max(0, tournament.fundOzTile) : 0;
            int fundOzGold = tournament != null ? Mathf.Max(0, tournament.fundOzGold) : 0;
            string tierText = "Tier " + a.weeklyChestTier;
            string nextChest = nextTierPoints > 0 ? nextTierPoints + " " + GameLocalization.Text("alliance.points_short") : GameLocalization.Text("alliance.max_tier");

            AddTreasuryTopRow(fundOzTile, fundOzGold, a.weeklyPoints);
            AddStatsGrid(
                GameLocalization.Text("alliance.lifetime_points"), a.lifetimePoints + " " + GameLocalization.Text("alliance.points_short"),
                GameLocalization.Text("alliance.chest"), tierText,
                GameLocalization.Text("alliance.next_chest"), nextChest,
                GameLocalization.Text("alliance.my_contribution"), ownWeekly.ToString());
            AddProgressCard(GameLocalization.Text("alliance.weekly"), nextTierPoints > 0 ? Mathf.Clamp01((float)a.weeklyPoints / nextTierPoints) : 1f, nextTierPoints > 0 ? a.weeklyPoints + " / " + nextTierPoints : a.weeklyPoints + " " + GameLocalization.Text("alliance.points_short"));
            AddProgressCard(GameLocalization.Text("alliance.my_contribution"), Mathf.Clamp01((float)ownWeekly / requiredPersonal), ownWeekly + " / " + requiredPersonal);
            AddInfoRow(GameLocalization.Text("alliance.next_chest"), nextChest, GetChestStatusText(service, Mathf.Max(0, requiredPersonal - ownWeekly)));

            AddSectionTitle(GameLocalization.Text("alliance.breakdown"));
            if (service.ContributionBreakdown.Count == 0)
                AddEmptyRow();
            else for (int i = 0; i < service.ContributionBreakdown.Count; i++)
            {
                AllianceContributionBreakdown item = service.ContributionBreakdown[i];
                AddInfoRow(LocalizeGameKey(item.gameKey), item.weeklyPoints + " " + GameLocalization.Text("alliance.points_short"), item.xp + " XP");
            }

            AddSectionTitle(GameLocalization.Text("alliance.top_contributors"));
            int count = Mathf.Min(5, service.Members.Count);
            if (count == 0)
            {
                AddEmptyRow();
            }
            else
            {
                bool[] used = new bool[service.Members.Count];
                for (int i = 0; i < count; i++)
                {
                    int bestIndex = -1;
                    for (int j = 0; j < service.Members.Count; j++)
                    {
                        if (used[j])
                            continue;

                        if (bestIndex < 0 || service.Members[j].weeklyContributionPoints > service.Members[bestIndex].weeklyContributionPoints)
                            bestIndex = j;
                    }

                    if (bestIndex < 0)
                        break;

                    used[bestIndex] = true;
                    AllianceMember member = service.Members[bestIndex];
                    AddInfoRow(member.nickname, member.weeklyContributionPoints + " " + GameLocalization.Text("alliance.points_short"), LocalizeRole(member.role));
                }
            }
        }

        private void RenderTournaments(AllianceService service)
        {
            ClearRows();
            AllianceTournamentState tournament = service.Tournament;
            AddHeaderCard(GameLocalization.Text("alliance.tournaments"), GameLocalization.Text("alliance.tournaments_hint"));
            if (tournament == null)
            {
                AddEmptyRow();
                return;
            }

            AddTournamentCard(service);
            AddStatsGrid(
                GameLocalization.Text("alliance.tournament_fund"), tournament.fundOzTile + " OzTile",
                GameLocalization.Text("alliance.clan_balance"), tournament.fundOzGold + " OzGold",
                GameLocalization.Text("alliance.level"), tournament.allianceLevel.ToString(),
                GameLocalization.Text("alliance.champion_split"), GetTournamentSplit(tournament));
            AddInfoRow(
                GameLocalization.Text("alliance.champion"),
                tournament.champion != null ? tournament.champion.nickname : GameLocalization.Text("alliance.no_champion"),
                tournament.eligible ? GameLocalization.Text("alliance.ready") : GameLocalization.Text("alliance.tournament_unlock") + " " + Mathf.Max(1, tournament.minAllianceLevel));
        }

        private void RenderEvents(AllianceService service)
        {
            ClearRows();
            AddHeaderCard(GameLocalization.Text("alliance.events"), GameLocalization.Text("alliance.activity") + "  |  " + GameLocalization.Text("alliance.leaderboard"));
            AddSectionTitle(GameLocalization.Text("alliance.activity"));
            if (service.Activity.Count == 0)
                AddEmptyRow();
            else for (int i = 0; i < service.Activity.Count; i++)
            {
                AddActivityRow(service.Activity[i]);
            }
            AddSectionTitle(GameLocalization.Text("alliance.leaderboard"));
            AddTableHeader("#", GameLocalization.Text("alliance.title"), GameLocalization.Text("alliance.level_short"), GameLocalization.Text("alliance.weekly"));
            int count = Mathf.Min(10, service.Leaderboard.Count);
            if (count == 0)
                AddEmptyRow();
            else for (int i = 0; i < count; i++)
            {
                AllianceSummary alliance = service.Leaderboard[i];
                AddLeaderboardRow(i + 1, alliance);
            }
        }

        private void RenderManage(AllianceService service)
        {
            ClearRows();
            if (!service.CanManage)
            {
                AddTextCard(GameLocalization.Text("alliance.no_permission"), 30f, AccentTextColor, TextAlignmentOptions.Center);
                return;
            }

            if (service.Current != null && !string.IsNullOrWhiteSpace(service.Current.weeklyFocus) && selectedWeeklyFocus == "any")
                selectedWeeklyFocus = service.Current.weeklyFocus;
            RefreshFocusButtons();
            if (service.PendingRequests.Count == 0)
                return;

            AddSectionTitle(GameLocalization.Text("alliance.requests"));
            for (int i = 0; i < service.PendingRequests.Count; i++)
            {
                AllianceJoinRequest request = service.PendingRequests[i];
                AddInfoRow(request.nickname, request.publicPlayerId, "#" + request.id);
            }
        }

        private void RenderLeaderboard(AllianceService service)
        {
            ClearRows();
            AddTableHeader("#", GameLocalization.Text("alliance.title"), GameLocalization.Text("alliance.level_short"), GameLocalization.Text("alliance.weekly"));
            for (int i = 0; i < service.Leaderboard.Count; i++)
            {
                AllianceSummary alliance = service.Leaderboard[i];
                AddLeaderboardRow(i + 1, alliance);
            }
            if (service.Leaderboard.Count == 0)
                AddEmptyRow();
        }

        private void ClearRows()
        {
            ClearMemberActionPopup();
            if (listContent == null)
                return;

            for (int i = listContent.childCount - 1; i >= 0; i--)
                DestroyRowObject(listContent.GetChild(i).gameObject);

            if (contentScroll != null)
                contentScroll.verticalNormalizedPosition = 1f;
        }

        private static void DestroyRowObject(GameObject row)
        {
            if (row == null)
                return;

            if (Application.isPlaying)
                Destroy(row);
            else
                DestroyImmediate(row);
        }

        private GameObject CreateRow(string name, float height, Color color)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(LayoutElement));
            row.transform.SetParent(listContent, false);
            ApplyRoundedPanel(row, color, 18f);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
            return row;
        }

        private static AllianceRoundedGraphic ApplyRoundedPanel(GameObject target, Color color, float radius)
        {
            if (target == null)
                return null;

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = false;
            }

            AllianceRoundedGraphic graphic = target.GetComponent<AllianceRoundedGraphic>();
            if (graphic == null)
                graphic = target.AddComponent<AllianceRoundedGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            graphic.raycastTarget = false;
            return graphic;
        }

        private TMP_Text AddText(GameObject parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            TMP_Text text = CreateText(parent.transform, name, value, size, alignment);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 10f);
            rect.offsetMax = new Vector2(-22f, -10f);
            text.color = color;
            text.richText = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private void AddHeaderCard(string title, string subtitle, Sprite icon = null)
        {
            GameObject row = CreateRow("HeaderCard", 128f, new Color(0.03f, 0.09f, 0.16f, 0.98f));
            if (icon != null)
            {
                HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(22, 24, 14, 14);
                layout.spacing = 18f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                GameObject iconObject = new GameObject("MembersIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(row.transform, false);
                Image image = iconObject.GetComponent<Image>();
                image.sprite = icon;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.preferredWidth = 96f;
                iconLayout.preferredHeight = 88f;

                TMP_Text text = CreateText(row.transform, "Header", "<size=40><b>" + title + "</b></size>\n<color=#A8C7EA>" + subtitle + "</color>", 29f, TextAlignmentOptions.Left);
                text.color = Color.white;
                text.richText = true;
                text.overflowMode = TextOverflowModes.Ellipsis;
                LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
                textLayout.flexibleWidth = 1f;
                textLayout.preferredWidth = 560f;
            }
            else
            {
                AddText(row, "Header", "<size=40><b>" + title + "</b></size>\n<color=#A8C7EA>" + subtitle + "</color>", 29f, Color.white, TextAlignmentOptions.Left);
            }
            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.55f, 0.75f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void AddClanBalanceCard(int fundOzTile, int weeklyPoints)
        {
            GameObject row = CreateRow("ClanBalanceCard", 96f, new Color(0.010f, 0.034f, 0.070f, 0.94f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 12, 12);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            AddCell(row.transform, "<b>" + GameLocalization.Text("alliance.clan_balance") + "</b>", 29f, Color.white, TextAlignmentOptions.Left, 0.38f);
            AddCell(row.transform, fundOzTile + " OzTile", 30f, AccentTextColor, TextAlignmentOptions.Center, 0.28f);
            AddCell(row.transform, GameLocalization.Text("alliance.weekly") + ": " + weeklyPoints + " " + GameLocalization.Text("alliance.points_short"), 27f, MutedTextColor, TextAlignmentOptions.Right, 0.34f);
        }

        private void AddTreasuryTopRow(int fundOzTile, int fundOzGold, int weeklyPoints)
        {
            GameObject row = CreateRow("TreasuryTopRow", 224f, new Color(0f, 0f, 0f, 0f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddDonationPanel(row.transform);
            AddClanBalancePanel(row.transform, fundOzTile, fundOzGold, weeklyPoints);
        }

        private void AddDonationPanel(Transform parent)
        {
            GameObject panel = CreateInlinePanel(parent, "DonationPanel", 0.46f, new Color(0.012f, 0.045f, 0.086f, 0.94f));
            VerticalLayoutGroup vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(20, 20, 18, 18);
            vertical.spacing = 14f;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            TMP_Text title = CreateText(panel.transform, "DonationTitle", "<b>" + GameLocalization.Text("alliance.donations") + "</b>", 32f, TextAlignmentOptions.Left);
            title.richText = true;
            title.color = Color.white;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            AddDonationLine(panel.transform, "OzTile", out donateOzTileInput);
            AddDonationLine(panel.transform, "OzGold", out donateOzGoldInput);
        }

        private void AddDonationLine(Transform parent, string currency, out TMP_InputField input)
        {
            GameObject line = new GameObject(currency + "DonationLine", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            line.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            LayoutElement lineLayout = line.GetComponent<LayoutElement>();
            lineLayout.preferredHeight = 58f;
            lineLayout.flexibleWidth = 1f;

            AddCell(line.transform, currency, 27f, AccentTextColor, TextAlignmentOptions.Left, 0.25f);
            input = CreateInlineInput(line.transform, currency + "DonationInput", GameLocalization.Text("alliance.donation_amount"));
            AddSmallActionButton(line.transform, GameLocalization.Text("alliance.donate"), 0.25f, () => OnDonate(currency));
        }

        private void AddClanBalancePanel(Transform parent, int fundOzTile, int fundOzGold, int weeklyPoints)
        {
            GameObject panel = CreateInlinePanel(parent, "ClanBalancePanel", 0.54f, new Color(0.010f, 0.034f, 0.070f, 0.94f));
            VerticalLayoutGroup vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(22, 22, 18, 18);
            vertical.spacing = 14f;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            TMP_Text title = CreateText(panel.transform, "BalanceTitle", "<b>" + GameLocalization.Text("alliance.clan_balance") + "</b>", 32f, TextAlignmentOptions.Left);
            title.richText = true;
            title.color = Color.white;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            AddInfoLine(panel.transform, "OzTile", fundOzTile.ToString());
            AddInfoLine(panel.transform, "OzGold", fundOzGold.ToString());
            AddInfoLine(panel.transform, GameLocalization.Text("alliance.weekly"), weeklyPoints + " " + GameLocalization.Text("alliance.points_short"));
        }

        private GameObject CreateInlinePanel(Transform parent, string name, float flexibleWidth, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);
            ApplyRoundedPanel(panel, color, 18f);
            LayoutElement layout = panel.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredHeight = 216f;
            return panel;
        }

        private void AddInfoLine(Transform parent, string label, string value)
        {
            GameObject line = new GameObject("InfoLine", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            line.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            LayoutElement lineLayout = line.GetComponent<LayoutElement>();
            lineLayout.preferredHeight = 38f;
            lineLayout.flexibleWidth = 1f;
            AddCell(line.transform, label, 27f, MutedTextColor, TextAlignmentOptions.Left, 0.5f);
            AddCell(line.transform, value, 28f, Color.white, TextAlignmentOptions.Right, 0.5f);
        }

        private TMP_InputField CreateInlineInput(Transform parent, string name, string placeholder)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            ApplyRoundedInput(obj.GetComponent<Image>());
            TMP_InputField input = obj.GetComponent<TMP_InputField>();
            input.targetGraphic = obj.GetComponent<Image>();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = new Color(0.92f, 0.96f, 1f, 1f);
            input.selectionColor = new Color(0.20f, 0.45f, 0.75f, 0.55f);

            TMP_Text text = CreateText(obj.transform, "Text", "", 24f, TextAlignmentOptions.Left);
            TMP_Text place = CreateText(obj.transform, "Placeholder", placeholder, 24f, TextAlignmentOptions.Left);
            text.raycastTarget = false;
            place.raycastTarget = false;
            place.color = new Color(1f, 1f, 1f, 0.35f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            RectTransform placeRect = place.GetComponent<RectTransform>();
            placeRect.anchorMin = Vector2.zero;
            placeRect.anchorMax = Vector2.one;
            placeRect.offsetMin = new Vector2(12f, 0f);
            placeRect.offsetMax = new Vector2(-12f, 0f);
            input.textComponent = text;
            input.placeholder = place;

            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.flexibleWidth = 0.42f;
            layout.preferredHeight = 54f;
            layout.preferredWidth = 168f;
            return input;
        }

        private static void AddSmallActionButton(Transform parent, string label, float flexibleWidth, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject("SmallActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            ApplyRoundedButton(image);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredWidth = 136f;
            layout.preferredHeight = 54f;
            TMP_Text text = CreateText(obj.transform, "Label", label, 24f, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ApplyMainButtonStyle(button, text, new Vector2(136f, 54f));
        }

        private void AddAllianceInfoHero(AllianceSummary alliance)
        {
            string title = "[" + alliance.tag + "] " + alliance.name;
            string subtitle = GameLocalization.Text("alliance.level") + " " + alliance.level
                + "  |  XP " + alliance.xp + "/" + alliance.nextLevelXp
                + "  |  " + GameLocalization.Text("alliance.members") + " " + alliance.memberCount + "/" + alliance.maxMembers;
            GameObject row = CreateRow("AllianceInfoHero", 138f, new Color(0.018f, 0.052f, 0.100f, 0.94f));
            AddText(row, "Hero", "<size=42><b>" + title + "</b></size>\n<color=#A8C7EA>" + subtitle + "</color>", 30f, Color.white, TextAlignmentOptions.Left);
            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.62f, 0.92f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void AddInfoTopRow(AllianceService service, AllianceSummary alliance)
        {
            GameObject row = CreateRow("InfoTopRow", 220f, new Color(0f, 0f, 0f, 0f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddAllianceIdentityCard(row.transform, service, alliance);
            AddAnnouncementCard(row.transform, service, alliance);
        }

        private void AddAllianceIdentityCard(Transform parent, AllianceService service, AllianceSummary alliance)
        {
            GameObject card = CreateInfoTopCard(parent, "AllianceIdentityCard", 0.48f);
            AllianceMember leader = FindMemberByUserId(service, alliance != null ? alliance.leaderUserId : 0);
            AllianceChampionInfo champion = service != null && service.Tournament != null ? service.Tournament.champion : null;
            string leaderName = leader != null && !string.IsNullOrWhiteSpace(leader.nickname) ? leader.nickname : "-";
            string leaderId = leader != null && !string.IsNullOrWhiteSpace(leader.publicPlayerId) ? "  <color=#6F8AA8>" + leader.publicPlayerId + "</color>" : "";
            string championName = champion != null && !string.IsNullOrWhiteSpace(champion.nickname) ? champion.nickname : GameLocalization.Text("alliance.no_champion");
            string championId = champion != null && !string.IsNullOrWhiteSpace(champion.publicPlayerId) ? "  <color=#6F8AA8>" + champion.publicPlayerId + "</color>" : "";
            string text =
                "<size=40><b>" + alliance.name + "</b></size>  <color=#A8C7EA>" + alliance.tag + "</color>\n" +
                "<b>" + GameLocalization.Text("alliance.role.leader") + ":</b>  <color=#A8C7EA>" + leaderName + leaderId + "</color>\n" +
                "<b>" + GameLocalization.Text("alliance.role.champion") + ":</b>  <color=#A8C7EA>" + championName + championId + "</color>\n" +
                "<b>" + GameLocalization.Text("alliance.members") + ":</b>  <color=#A8C7EA>" + alliance.memberCount + "/" + alliance.maxMembers + "</color>";
            AddText(card, "Identity", text, 30f, Color.white, TextAlignmentOptions.Left);
        }

        private void AddAnnouncementCard(Transform parent, AllianceService service, AllianceSummary alliance)
        {
            GameObject card = CreateInfoTopCard(parent, "AnnouncementCard", 0.52f);
            string announcement = string.IsNullOrWhiteSpace(alliance.announcement) ? "-" : alliance.announcement;
            string text = "<size=34><b>" + GameLocalization.Text("alliance.announcement") + "</b></size>\n<color=#A8C7EA>" + announcement + "</color>";
            AddText(card, "Text", text, 30f, AccentTextColor, TextAlignmentOptions.Left);
            if (service != null && service.IsLeader)
                AddAnnouncementGearButton(card.transform, alliance);
        }

        private GameObject CreateInfoTopCard(Transform parent, string name, float flexibleWidth)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            ApplyRoundedPanel(card, new Color(0.018f, 0.052f, 0.100f, 0.94f), 18f);
            LayoutElement layout = card.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredHeight = 210f;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.62f, 0.92f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);
            return card;
        }

        private void AddAnnouncementGearButton(Transform parent, AllianceSummary alliance)
        {
            GameObject obj = new GameObject("AnnouncementGear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = LoadMemberActionGearSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(0.90f, 0.96f, 1f, 0.92f);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -14f);
            rect.sizeDelta = new Vector2(32f, 32f);

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => ShowAnnouncementPopup(alliance, rect));
        }

        private void ShowAnnouncementPopup(AllianceSummary alliance, RectTransform sourceRect)
        {
            ClearAnnouncementPopup();
            if (panelRoot == null || AllianceService.I == null || !AllianceService.I.IsLeader)
                return;

            GameObject popup = new GameObject("AnnouncementPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(VerticalLayoutGroup));
            popup.transform.SetParent(panelRoot.transform, false);
            popup.transform.SetAsLastSibling();
            ApplyRoundedPanel(popup, new Color(0.010f, 0.035f, 0.070f, 0.98f), 20f);

            VerticalLayoutGroup layout = popup.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            announcementPopup = popup.GetComponent<RectTransform>();
            announcementPopup.anchorMin = new Vector2(0.5f, 0.5f);
            announcementPopup.anchorMax = new Vector2(0.5f, 0.5f);
            announcementPopup.pivot = new Vector2(1f, 1f);
            announcementPopup.sizeDelta = new Vector2(440f, 156f);

            TMP_Text title = CreateText(popup.transform, "Title", "<b>" + GameLocalization.Text("alliance.announcement") + "</b>", 24f, TextAlignmentOptions.Left);
            title.richText = true;
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 30f;

            TMP_InputField input = CreateInlineInput(popup.transform, "AnnouncementEditInput", GameLocalization.Text("alliance.announcement"));
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterLimit = 180;
            input.text = alliance != null && !string.IsNullOrWhiteSpace(alliance.announcement) ? alliance.announcement : string.Empty;

            Button save = AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.update"), () =>
            {
                string focus = AllianceService.I != null && AllianceService.I.Current != null ? AllianceService.I.Current.weeklyFocus : selectedWeeklyFocus;
                StartCoroutine(AllianceService.I.UpdateSettings(input.text, focus, (_, message) =>
                {
                    SetStatus(message);
                    ClearAnnouncementPopup();
                }));
            });
            LayoutElement saveLayout = save.GetComponent<LayoutElement>();
            if (saveLayout != null)
                saveLayout.preferredHeight = 42f;

            PlaceAnnouncementPopup(sourceRect, announcementPopup.sizeDelta.x, announcementPopup.sizeDelta.y);
        }

        private void PlaceAnnouncementPopup(RectTransform sourceRect, float width, float height)
        {
            if (announcementPopup == null || sourceRect == null || panelRoot == null)
                return;

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            sourceRect.GetWorldCorners(corners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPoint, null, out Vector2 local))
                local = Vector2.zero;

            Rect rect = panelRect.rect;
            Vector2 position = local + new Vector2(-12f, -12f);
            position.x = Mathf.Clamp(position.x, rect.xMin + width + 24f, rect.xMax - 24f);
            position.y = Mathf.Clamp(position.y, rect.yMin + height + 24f, rect.yMax - 24f);
            announcementPopup.anchoredPosition = position;
        }

        private void ClearAnnouncementPopup()
        {
            if (announcementPopup == null)
                return;

            Destroy(announcementPopup.gameObject);
            announcementPopup = null;
        }

        private void AddAnnouncementCard(AllianceSummary alliance)
        {
            string announcement = string.IsNullOrWhiteSpace(alliance.announcement) ? "-" : alliance.announcement;
            string text = "<b>" + GameLocalization.Text("alliance.announcement") + "</b>\n<color=#A8C7EA>" + announcement + "</color>";
            GameObject row = CreateRow("AnnouncementCard", 104f, new Color(0.010f, 0.034f, 0.070f, 0.82f));
            AddText(row, "Text", text, 27f, AccentTextColor, TextAlignmentOptions.Left);
        }

        private void AddRewardHero(string tierText, string statusTextValue, int missingPersonal)
        {
            string subtitle = GameLocalization.Text("alliance.chest") + " " + tierText + "  |  " + statusTextValue;
            if (missingPersonal > 0)
                subtitle += "  |  " + GameLocalization.Text("alliance.need_more") + ": " + missingPersonal;

            GameObject row = CreateRow("RewardHero", 132f, new Color(0.018f, 0.052f, 0.100f, 0.94f));
            AddText(row, "Hero", "<size=40><b>" + GameLocalization.Text("alliance.rewards") + "</b></size>\n<color=#A8C7EA>" + subtitle + "</color>", 29f, Color.white, TextAlignmentOptions.Left);
            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.62f, 0.92f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void AddHeroCard(string title, string subtitle)
        {
            GameObject row = CreateRow("HeroCard", 150f, CardAltFillColor);
            AddText(row, "Hero", "<size=42><b>" + title + "</b></size>\n<color=#A8C7EA>" + subtitle + "</color>", 30f, Color.white, TextAlignmentOptions.Left);
            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.68f, 0.95f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void AddActionGuideCard()
        {
            string create = GameLocalization.Text("alliance.create");
            string search = GameLocalization.Text("alliance.search");
            string name = GameLocalization.Text("alliance.name");
            string tag = GameLocalization.Text("alliance.tag");
            string text = "<b>" + create + "</b>  <color=#A8C7EA>" + name + " + " + tag + "</color>\n"
                        + "<b>" + search + "</b>  <color=#A8C7EA>" + GameLocalization.Text("alliance.search_results") + "</color>";
            GameObject row = CreateRow("ActionGuide", 102f, new Color(0.014f, 0.045f, 0.086f, 0.92f));
            AddText(row, "Text", text, 28f, AccentTextColor, TextAlignmentOptions.Left);
        }

        private void AddTournamentCard(AllianceService service)
        {
            AllianceTournamentState tournament = service != null ? service.Tournament : null;
            if (tournament == null)
                return;

            AllianceChampionInfo champion = tournament.champion;
            string championName = champion != null && !string.IsNullOrWhiteSpace(champion.nickname)
                ? champion.nickname
                : GameLocalization.Text("alliance.no_champion");
            string split = GetTournamentSplit(tournament);
            string text = "<b>" + GameLocalization.Text("alliance.tournament_fund") + "</b>  <color=#A8C7EA>" + tournament.fundOzTile + " OzTile</color>\n"
                        + "<b>" + GameLocalization.Text("alliance.champion") + "</b>  <color=#A8C7EA>" + championName + "</color>"
                        + "  <color=#6F8AA8>" + GameLocalization.Text("alliance.champion_split") + " " + split + "</color>";
            if (!tournament.eligible)
                text += "\n<color=#E7B56A>" + GameLocalization.Text("alliance.tournament_unlock") + " " + Mathf.Max(2, tournament.minAllianceLevel) + "</color>";
            GameObject row = CreateRow("TournamentCard", 118f, new Color(0.026f, 0.070f, 0.118f, 0.96f));
            AddText(row, "Text", text, 27f, Color.white, TextAlignmentOptions.Left);
        }

        private static string GetTournamentSplit(AllianceTournamentState tournament)
        {
            if (tournament != null && tournament.rules != null)
                return tournament.rules.rewardToAlliancePercent + "/" + tournament.rules.rewardToChampionPercent;
            return "70/30";
        }

        private void AddSectionTitle(string title)
        {
            GameObject row = CreateRow("SectionTitle", 58f, new Color(0f, 0f, 0f, 0f));
            AddText(row, "Title", "<b>" + title + "</b>", 32f, AccentTextColor, TextAlignmentOptions.Left);
        }

        private void AddTextCard(string text, float size, Color color, TextAlignmentOptions alignment)
        {
            GameObject row = CreateRow("TextCard", 96f, new Color(0.018f, 0.055f, 0.105f, 0.85f));
            AddText(row, "Text", text, size, color, alignment);
        }

        private void AddChatEmptyCard(string text)
        {
            GameObject row = CreateRow("ChatEmptyCard", 160f, new Color(0.010f, 0.034f, 0.070f, 0.72f));
            AddText(row, "Text", text, 30f, MutedTextColor, TextAlignmentOptions.Center);
        }

        private void AddEmptyRow()
        {
            AddTextCard("-", 26f, MutedTextColor, TextAlignmentOptions.Center);
        }

        private void AddCompactEmptyRow(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "alliance.empty_invites" || text == "alliance.search_hint")
                text = "-";

            GameObject row = CreateRow("EmptyRow", 70f, new Color(0.012f, 0.035f, 0.075f, 0.82f));
            AddText(row, "Text", text, 26f, MutedTextColor, TextAlignmentOptions.Center);
        }

        private void AddInfoRow(string left, string middle, string right)
        {
            GameObject row = CreateRow("InfoRow", 96f, new Color(0.022f, 0.066f, 0.12f, 0.94f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 8, 8);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            AddCell(row.transform, left, 31f, Color.white, TextAlignmentOptions.Left, 0.52f);
            AddCell(row.transform, middle, 28f, AccentTextColor, TextAlignmentOptions.Center, 0.25f);
            AddCell(row.transform, right, 27f, MutedTextColor, TextAlignmentOptions.Right, 0.23f);
        }

        private void AddAllianceResultRow(string left, string middle, string right)
        {
            GameObject row = CreateRow("AllianceResultRow", 92f, new Color(0.018f, 0.052f, 0.096f, 0.96f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 7, 7);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            AddCell(row.transform, "<b>" + left + "</b>", 30f, Color.white, TextAlignmentOptions.Left, 0.54f);
            AddCell(row.transform, middle, 27f, AccentTextColor, TextAlignmentOptions.Center, 0.25f);
            AddCell(row.transform, right, 25f, MutedTextColor, TextAlignmentOptions.Right, 0.21f);
        }

        private void AddTableHeader(string a, string b, string c, string d)
        {
            GameObject row = CreateRow("TableHeader", 72f, new Color(0.026f, 0.078f, 0.140f, 0.96f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 5, 5);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddCell(row.transform, a, 27f, MutedTextColor, TextAlignmentOptions.Left, 0.44f);
            AddCell(row.transform, b, 27f, MutedTextColor, TextAlignmentOptions.Center, 0.20f);
            AddCell(row.transform, c, 27f, MutedTextColor, TextAlignmentOptions.Center, 0.18f);
            AddCell(row.transform, d, 27f, MutedTextColor, TextAlignmentOptions.Right, 0.18f);
        }

        private void AddMemberRow(AllianceMember member, AllianceService service)
        {
            bool selected = member != null && member.userId == selectedMemberUserId;
            GameObject row = CreateRow("MemberRow", 104f, selected ? new Color(0.035f, 0.115f, 0.195f, 0.98f) : new Color(0.014f, 0.046f, 0.090f, 0.92f));
            int memberUserId = member != null ? member.userId : 0;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 8, 8);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddMemberIdentityCell(row.transform, member, service, memberUserId, 0.44f);
            AddCell(row.transform, LocalizeRole(member.role), 28f, AccentTextColor, TextAlignmentOptions.Center, 0.20f);
            AddCell(row.transform, member.weeklyContributionPoints.ToString(), 28f, Color.white, TextAlignmentOptions.Center, 0.18f);
            AddCell(row.transform, string.IsNullOrWhiteSpace(member.battleRankTier) ? "-" : member.battleRankTier, 27f, MutedTextColor, TextAlignmentOptions.Right, 0.18f);
        }

        private void AddMemberIdentityCell(Transform parent, AllianceMember member, AllianceService service, int memberUserId, float flexibleWidth)
        {
            GameObject cell = new GameObject("MemberIdentityCell", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            cell.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = cell.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            LayoutElement cellLayout = cell.GetComponent<LayoutElement>();
            cellLayout.flexibleWidth = flexibleWidth;
            cellLayout.preferredWidth = Mathf.Max(260f, flexibleWidth * 1000f);

            string online = member.online ? "<color=#7CFFB2>●</color>" : "<color=#6F8AA8>●</color>";
            Button actionButton = AddMemberActionGearButton(cell.transform, CanInteractWithMember(service, member));
            RectTransform actionRect = actionButton.GetComponent<RectTransform>();
            actionButton.onClick.AddListener(() =>
            {
                selectedMemberUserId = memberUserId;
                ShowMemberActionPopup(service, member, actionRect);
            });

            TMP_Text label = CreateText(cell.transform, "MemberName", online + "  <b>" + member.nickname + "</b>\n<color=#6F8AA8>" + member.publicPlayerId + "</color>", 29f, TextAlignmentOptions.Left);
            label.color = Color.white;
            label.richText = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.Normal;
            MainLobbyButtonStyle.ApplyFont(label);
            LayoutElement textLayout = label.gameObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.preferredWidth = 260f;

        }

        private static Button AddMemberActionGearButton(Transform parent, bool interactable)
        {
            GameObject obj = new GameObject("MemberActionGear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = LoadMemberActionGearSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = interactable;
            image.color = interactable ? new Color(0.92f, 0.96f, 1f, 1f) : new Color(0.92f, 0.96f, 1f, 0f);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.localScale = new Vector3(0.44f, 0.44f, 1f);

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
            colors.highlightedColor = new Color(0.78f, 0.92f, 1f, 1f);
            colors.pressedColor = new Color(0.58f, 0.78f, 1f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
            button.colors = colors;

            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.preferredWidth = 18f;
            layout.preferredHeight = 18f;
            layout.minWidth = 18f;
            layout.minHeight = 18f;
            layout.flexibleWidth = 0f;
            return button;
        }

        private void AddMemberActionsCard(AllianceService service, AllianceMember member)
        {
            GameObject row = CreateRow("MemberActions", 126f, new Color(0.018f, 0.064f, 0.120f, 0.96f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            AddCell(row.transform, "<b>" + GameLocalization.Text("alliance.selected_member") + "</b>\n<color=#A8C7EA>" + member.nickname + "</color>", 27f, Color.white, TextAlignmentOptions.Left, 0.38f);
            bool targetIsLeader = string.Equals(member.role, "leader", System.StringComparison.OrdinalIgnoreCase);
            bool targetIsOfficer = string.Equals(member.role, "officer", System.StringComparison.OrdinalIgnoreCase);
            bool actorIsLeader = service != null && service.IsLeader;
            bool actorCanKick = actorIsLeader ? !targetIsLeader : !targetIsLeader && !targetIsOfficer;
            if (actorCanKick)
                AddInlineActionButton(row.transform, GameLocalization.Text("alliance.kick_member"), 0.17f, () => OnKickSelectedMember(member.userId));
            if (actorIsLeader && targetIsOfficer)
                AddInlineActionButton(row.transform, GameLocalization.Text("alliance.demote_member"), 0.17f, () => OnDemoteSelectedMember(member.userId));
            else if (actorIsLeader && !targetIsLeader)
                AddInlineActionButton(row.transform, GameLocalization.Text("alliance.promote_member"), 0.17f, () => OnPromoteSelectedMember(member.userId));

            AllianceChampionInfo currentChampion = service != null && service.Tournament != null ? service.Tournament.champion : null;
            bool canChampion = currentChampion == null || currentChampion.userId != member.userId;
            if (service != null && service.IsLeader && canChampion && !targetIsLeader)
                AddInlineActionButton(row.transform, GameLocalization.Text("alliance.make_champion"), 0.22f, () => OnMakeChampion(member.userId));
        }

        private static bool CanInteractWithMember(AllianceService service, AllianceMember member)
        {
            if (service == null || member == null || member.userId <= 0)
                return false;

            if (IsOwnMember(service, member))
                return true;

            if (!service.CanManage)
                return false;

            bool targetIsLeader = string.Equals(member.role, "leader", System.StringComparison.OrdinalIgnoreCase);
            bool targetIsOfficer = string.Equals(member.role, "officer", System.StringComparison.OrdinalIgnoreCase);
            if (service.IsLeader)
                return !targetIsLeader;

            return !targetIsLeader && !targetIsOfficer;
        }

        private static bool IsOwnMember(AllianceService service, AllianceMember member)
        {
            AllianceMember own = FindOwnMember(service);
            return own != null && member != null && own.userId == member.userId;
        }

        private void ShowMemberActionPopup(AllianceService service, AllianceMember member, RectTransform sourceRect)
        {
            ClearMemberActionPopup();
            if (!CanInteractWithMember(service, member) || panelRoot == null)
                return;

            bool isOwn = IsOwnMember(service, member);
            bool targetIsLeader = string.Equals(member.role, "leader", System.StringComparison.OrdinalIgnoreCase);
            bool targetIsOfficer = string.Equals(member.role, "officer", System.StringComparison.OrdinalIgnoreCase);
            bool actorIsLeader = service != null && service.IsLeader;
            bool actorCanKick = !isOwn && (actorIsLeader ? !targetIsLeader : !targetIsLeader && !targetIsOfficer);
            bool canTransferLeadership = !isOwn && actorIsLeader && !targetIsLeader;
            AllianceChampionInfo currentChampion = service != null && service.Tournament != null ? service.Tournament.champion : null;
            bool canChampion = !isOwn && actorIsLeader && service != null && service.Tournament != null && (currentChampion == null || currentChampion.userId != member.userId) && !targetIsLeader;

            GameObject popup = new GameObject("MemberActionPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(VerticalLayoutGroup));
            popup.transform.SetParent(panelRoot.transform, false);
            popup.transform.SetAsLastSibling();
            ApplyRoundedPanel(popup, new Color(0.010f, 0.035f, 0.070f, 0.98f), 20f);

            VerticalLayoutGroup layout = popup.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            memberActionPopup = popup.GetComponent<RectTransform>();
            memberActionPopup.anchorMin = new Vector2(0.5f, 0.5f);
            memberActionPopup.anchorMax = new Vector2(0.5f, 0.5f);
            memberActionPopup.pivot = new Vector2(0f, 1f);

            int actionCount = 0;
            if (isOwn) actionCount++;
            if (actorCanKick) actionCount++;
            if (canTransferLeadership) actionCount++;
            if (!isOwn && actorIsLeader && targetIsOfficer) actionCount++;
            else if (!isOwn && actorIsLeader && !targetIsLeader) actionCount++;
            if (canChampion) actionCount++;

            float width = 330f;
            float height = 72f + Mathf.Max(1, actionCount) * 56f;
            memberActionPopup.sizeDelta = new Vector2(width, height);

            TMP_Text title = CreateText(popup.transform, "PopupTitle", "<b>" + member.nickname + "</b>\n<color=#6F8AA8>" + LocalizeRole(member.role) + "  " + member.publicPlayerId + "</color>", 24f, TextAlignmentOptions.Left);
            title.richText = true;
            title.color = Color.white;
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 54f;

            if (isOwn)
                AddPopupActionButton(popup.transform, GameLocalization.Text("battle.common.leave"), () => { ClearMemberActionPopup(); OnLeave(); });
            if (actorCanKick)
                AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.kick_member"), () => { ClearMemberActionPopup(); OnKickSelectedMember(member.userId); });
            if (canTransferLeadership)
                AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.transfer_leadership"), () => { ClearMemberActionPopup(); OnTransferLeadership(member.userId); });
            if (!isOwn && actorIsLeader && targetIsOfficer)
                AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.demote_member"), () => { ClearMemberActionPopup(); OnDemoteSelectedMember(member.userId); });
            else if (!isOwn && actorIsLeader && !targetIsLeader)
                AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.promote_member"), () => { ClearMemberActionPopup(); OnPromoteSelectedMember(member.userId); });
            if (canChampion)
                AddPopupActionButton(popup.transform, GameLocalization.Text("alliance.make_champion"), () => { ClearMemberActionPopup(); OnMakeChampion(member.userId); });

            PlaceMemberActionPopup(sourceRect, width, height);
        }

        private void PlaceMemberActionPopup(RectTransform sourceRect, float width, float height)
        {
            if (memberActionPopup == null || sourceRect == null || panelRoot == null)
                return;

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            sourceRect.GetWorldCorners(corners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPoint, null, out Vector2 local))
                local = Vector2.zero;

            Rect rect = panelRect.rect;
            Vector2 position = local + new Vector2(18f, 12f);
            if (position.x + width > rect.xMax - 24f)
                position.x = local.x - width - 18f;
            position.x = Mathf.Clamp(position.x, rect.xMin + 24f, rect.xMax - width - 24f);
            position.y = Mathf.Clamp(position.y, rect.yMin + height + 24f, rect.yMax - 24f);
            memberActionPopup.anchoredPosition = position;
        }

        private void ClearMemberActionPopup()
        {
            if (memberActionPopup == null)
                return;

            Destroy(memberActionPopup.gameObject);
            memberActionPopup = null;
        }

        private static Button AddPopupActionButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject("PopupActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            ApplyRoundedButton(image);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);

            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.preferredHeight = 48f;
            layout.minHeight = 46f;
            layout.flexibleWidth = 1f;

            TMP_Text text = CreateText(obj.transform, "Label", label, 22f, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ApplyMainButtonStyle(button, text, new Vector2(240f, 48f));
            return button;
        }

        private static Button AddInlineActionButton(Transform parent, string label, float flexibleWidth, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject("MemberActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            ApplyRoundedButton(image);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredWidth = Mathf.Max(140f, flexibleWidth * 900f);
            layout.preferredHeight = 58f;
            TMP_Text text = CreateText(obj.transform, "Label", label, 22f, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ApplyMainButtonStyle(button, text, new Vector2(layout.preferredWidth, layout.preferredHeight));
            return button;
        }

        private void AddChatRow(AllianceChatMessage message)
        {
            GameObject row = CreateRow("ChatRow", 104f, new Color(0.018f, 0.056f, 0.108f, 0.92f));
            string role = GetChatRoleLabel(message, AllianceService.I);
            string roleText = string.IsNullOrWhiteSpace(role) ? "" : "  <color=#FFEFA8>" + role + "</color>";
            string ownerText = message.isDeveloper
                ? "  <color=#FFD45E><b>◆ " + GameLocalization.Text("chat.role.owner") + "</b></color>"
                : "";
            string text = "<color=#A8C7EA><b>" + EscapeChatRichText(message.nickname) + "</b></color>" + ownerText + roleText + "\n" + EscapeChatRichText(message.text);
            AddText(row, "Message", text, 29f, Color.white, TextAlignmentOptions.Left);
        }

        private static string EscapeChatRichText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string GetChatRoleLabel(AllianceChatMessage message, AllianceService service)
        {
            if (message == null)
                return "";
            string role = string.IsNullOrWhiteSpace(message.role) ? "" : message.role.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(role) && service != null)
            {
                AllianceMember member = FindMemberByUserId(service, message.userId);
                if (member != null && !string.IsNullOrWhiteSpace(member.role))
                    role = member.role.Trim().ToLowerInvariant();
            }

            bool isChampion = service != null
                && service.Tournament != null
                && service.Tournament.champion != null
                && service.Tournament.champion.userId == message.userId;
            string roleLabel = role == "leader" || role == "officer" ? LocalizeRole(role) : "";
            string championLabel = isChampion ? GameLocalization.Text("alliance.role.champion") : "";
            if (!string.IsNullOrWhiteSpace(roleLabel) && !string.IsNullOrWhiteSpace(championLabel))
                return roleLabel + " · " + championLabel;
            if (!string.IsNullOrWhiteSpace(roleLabel))
                return roleLabel;
            return championLabel;
        }

        private void AddActivityRow(AllianceActivity activity)
        {
            string actorName = ResolveActivityActorName(activity);
            string targetName = ResolveActivityTargetName(activity);
            string title = string.IsNullOrWhiteSpace(actorName)
                ? LocalizeActivityType(activity != null ? activity.type : string.Empty)
                : actorName;
            string middle = BuildActivitySummary(activity, targetName);
            string right = activity != null && activity.points > 0
                ? "+" + activity.points + " " + GameLocalization.Text("alliance.points_short")
                : LocalizeGameKey(activity != null ? activity.gameKey : string.Empty);
            AddInfoRow(title, middle, right);
        }

        private void AddLeaderboardRow(int rank, AllianceSummary alliance)
        {
            GameObject row = CreateRow("LeaderboardRow", 84f, new Color(0.022f, 0.066f, 0.12f, 0.94f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddCell(row.transform, rank.ToString(), 28f, AccentTextColor, TextAlignmentOptions.Left, 0.14f);
            AddCell(row.transform, "[" + alliance.tag + "] <b>" + alliance.name + "</b>", 28f, Color.white, TextAlignmentOptions.Left, 0.46f);
            AddCell(row.transform, FormatLevel(alliance.level), 28f, AccentTextColor, TextAlignmentOptions.Center, 0.20f);
            AddCell(row.transform, alliance.weeklyPoints.ToString(), 28f, Color.white, TextAlignmentOptions.Right, 0.20f);
        }

        private void AddStatsGrid(string a, string av, string b, string bv, string c, string cv, string d, string dv)
        {
            if (IsLandscapeLayout())
            {
                AddInfoRow(a, av, b + ": " + bv);
                AddInfoRow(c, cv, d + ": " + dv);
                return;
            }

            GameObject row = CreateRow("StatsGrid", 152f, new Color(0.022f, 0.066f, 0.12f, 0.94f));
            GridLayoutGroup grid = row.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 14, 14);
            grid.spacing = new Vector2(14f, 12f);
            grid.cellSize = new Vector2(300f, 56f);
            AddStatCell(row.transform, a, av);
            AddStatCell(row.transform, b, bv);
            AddStatCell(row.transform, c, cv);
            AddStatCell(row.transform, d, dv);
        }

        private void AddProgressCard(string label, float normalized, string value)
        {
            GameObject row = CreateRow("ProgressCard", 128f, new Color(0.022f, 0.066f, 0.12f, 0.94f));
            AddText(row, "Label", "<b>" + label + "</b>  <color=#A8C7EA>" + value + "</color>", 32f, Color.white, TextAlignmentOptions.Left);
            GameObject track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            track.transform.SetParent(row.transform, false);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.offsetMin = new Vector2(22f, 16f);
            trackRect.offsetMax = new Vector2(-22f, 44f);
            ApplyRoundedPanel(track, new Color(0.05f, 0.11f, 0.18f, 1f), 10f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            fill.transform.SetParent(track.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            ApplyRoundedPanel(fill, new Color(0.42f, 0.72f, 1f, 1f), 10f);
        }

        private void AddStatCell(Transform parent, string label, string value)
        {
            GameObject cell = new GameObject("Stat", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            cell.transform.SetParent(parent, false);
            ApplyRoundedPanel(cell, new Color(0.012f, 0.035f, 0.075f, 0.85f), 14f);
            AddText(cell, "Text", "<color=#A8C7EA>" + label + "</color>  <b>" + value + "</b>", 25f, Color.white, TextAlignmentOptions.Center);
        }

        private TMP_Text AddCell(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment, float flexibleWidth)
        {
            TMP_Text label = CreateText(parent, "Cell", text, size, alignment);
            label.color = color;
            label.richText = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.Normal;
            MainLobbyButtonStyle.ApplyFont(label);
            LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.preferredWidth = Mathf.Max(80f, flexibleWidth * 1000f);
            return label;
        }

        private static string FormatLevel(int level)
        {
            return GameLocalization.Text("alliance.level_short") + "." + Mathf.Max(1, level);
        }

        private static string LocalizeRole(string role)
        {
            string value = string.IsNullOrWhiteSpace(role) ? "member" : role.Trim().ToLowerInvariant();
            if (value == "leader")
                return GameLocalization.Text("alliance.role.leader");
            if (value == "officer")
                return GameLocalization.Text("alliance.role.officer");
            return GameLocalization.Text("alliance.role.member");
        }

        private static string LocalizeVisibility(string visibility)
        {
            string value = string.IsNullOrWhiteSpace(visibility) ? string.Empty : visibility.Trim().ToLowerInvariant();
            if (value == "open")
                return GameLocalization.Text("alliance.visibility.open");
            if (value == "invite_only")
                return GameLocalization.Text("alliance.visibility.invite_only");
            if (value == "closed")
                return GameLocalization.Text("alliance.visibility.closed");
            return visibility ?? string.Empty;
        }

        private static string LocalizeFocus(string focus)
        {
            string value = string.IsNullOrWhiteSpace(focus) ? "any" : focus.Trim().ToLowerInvariant();
            if (value == "mahjong_ranked" || value == "ranked")
                return GameLocalization.Text("alliance.focus.ranked");
            if (value == "mahjong_duel" || value == "duel")
                return GameLocalization.Text("alliance.focus.duel");
            if (value == "daily_checkin" || value == "daily")
                return GameLocalization.Text("alliance.focus.daily");
            if (value == "mahjong_random_online" || value == "random")
                return GameLocalization.Text("alliance.focus.random");
            return GameLocalization.Text("alliance.focus.any");
        }

        private static string LocalizeGameKey(string gameKey)
        {
            string value = string.IsNullOrWhiteSpace(gameKey) ? "any" : gameKey.Trim().ToLowerInvariant();
            if (value == "mahjong_ranked")
                return GameLocalization.Text("alliance.focus.ranked");
            if (value == "mahjong_duel")
                return GameLocalization.Text("alliance.focus.duel");
            if (value == "daily_checkin")
                return GameLocalization.Text("alliance.focus.daily");
            if (value == "mahjong_random_online")
                return GameLocalization.Text("alliance.focus.random");
            return string.IsNullOrWhiteSpace(gameKey) ? "-" : gameKey;
        }

        private static string LocalizeActivityType(string type)
        {
            string value = string.IsNullOrWhiteSpace(type) ? "event" : type.Trim().ToLowerInvariant();
            if (value == "created")
                return GameLocalization.Text("alliance.activity.created");
            if (value == "joined")
                return GameLocalization.Text("alliance.activity.joined");
            if (value == "left")
                return GameLocalization.Text("alliance.activity.left");
            if (value == "kicked")
                return GameLocalization.Text("alliance.activity.kicked");
            if (value == "promoted")
                return GameLocalization.Text("alliance.activity.promoted");
            if (value == "demoted")
                return GameLocalization.Text("alliance.activity.demoted");
            if (value == "contribution")
                return GameLocalization.Text("alliance.activity.contribution");
            if (value == "fund_donation")
                return GameLocalization.Text("alliance.activity.fund_donation");
            if (value == "test_bots_added")
                return GameLocalization.Text("alliance.activity.test_bots_added");
            if (value == "level_up")
                return GameLocalization.Text("alliance.activity.level_up");
            if (value == "champion_selected")
                return GameLocalization.Text("alliance.activity.champion_selected");
            if (value == "request_accepted")
                return GameLocalization.Text("alliance.activity.request_accepted");
            if (value == "invite_accepted")
                return GameLocalization.Text("alliance.activity.invite_accepted");
            if (value == "leadership_transferred")
                return GameLocalization.Text("alliance.activity.leadership_transferred");
            return type ?? "";
        }

        private static string BuildActivitySummary(AllianceActivity activity, string targetName)
        {
            if (activity == null)
                return "";

            string type = string.IsNullOrWhiteSpace(activity.type) ? "event" : activity.type.Trim().ToLowerInvariant();
            if (type == "joined")
                return GameLocalization.Text("alliance.activity.joined");
            if (type == "left")
                return GameLocalization.Text("alliance.activity.left");
            if (type == "created")
                return GameLocalization.Text("alliance.activity.created");
            if (type == "level_up")
                return activity.points > 0 ? GameLocalization.Text("alliance.activity.level_up") + " " + activity.points : GameLocalization.Text("alliance.activity.level_up");
            if (type == "contribution")
                return string.IsNullOrWhiteSpace(activity.gameKey)
                    ? GameLocalization.Text("alliance.activity.contribution")
                    : GameLocalization.Text("alliance.activity.contribution") + ": " + LocalizeGameKey(activity.gameKey);
            if (type == "fund_donation")
                return GameLocalization.Text("alliance.activity.fund_donation");
            if (type == "test_bots_added")
                return GameLocalization.Text("alliance.activity.test_bots_added");
            if ((type == "promoted" || type == "demoted" || type == "kicked" || type == "request_accepted" || type == "invite_accepted" || type == "champion_selected" || type == "leadership_transferred")
                && !string.IsNullOrWhiteSpace(targetName))
            {
                return LocalizeActivityType(type) + ": " + targetName;
            }

            return LocalizeActivityType(type);
        }

        private static string ResolveActivityActorName(AllianceActivity activity)
        {
            if (activity == null)
                return "";
            if (!string.IsNullOrWhiteSpace(activity.actorNickname))
                return activity.actorNickname;
            return activity.actorUserId > 0 ? "#" + activity.actorUserId : "";
        }

        private static string ResolveActivityTargetName(AllianceActivity activity)
        {
            if (activity == null)
                return "";
            if (!string.IsNullOrWhiteSpace(activity.targetNickname))
                return activity.targetNickname;
            return activity.targetUserId > 0 ? "#" + activity.targetUserId : "";
        }

        private static AllianceMember FindOwnMember(AllianceService service)
        {
            if (service == null || ProfileService.I == null || ProfileService.I.Current == null)
                return null;

            string onlineId = ProfileService.I.Current.OnlinePlayerId;
            int userId = 0;
            if (string.IsNullOrWhiteSpace(onlineId) || !int.TryParse(onlineId, out userId) || userId <= 0)
                return null;

            for (int i = 0; i < service.Members.Count; i++)
            {
                AllianceMember member = service.Members[i];
                if (member != null && member.userId == userId)
                    return member;
            }

            return null;
        }

        private static AllianceMember FindMemberByUserId(AllianceService service, int userId)
        {
            if (service == null || userId <= 0)
                return null;

            for (int i = 0; i < service.Members.Count; i++)
            {
                AllianceMember member = service.Members[i];
                if (member != null && member.userId == userId)
                    return member;
            }

            return null;
        }

        private static bool IsChestClaimAvailable(AllianceService service)
        {
            if (service == null || service.Current == null)
                return false;

            AllianceChestState chest = service.Chest;
            if (chest != null)
                return chest.ready && !chest.claimed && chest.playerContribution >= GetWeeklyChestMinContribution(service);

            AllianceMember own = FindOwnMember(service);
            int ownWeekly = own != null ? own.weeklyContributionPoints : 0;
            return service.Current.weeklyChestTier > 0 && ownWeekly >= GetWeeklyChestMinContribution(service);
        }

        private void ConfigureClaimChestButton(AllianceService service)
        {
            if (claimChestButton == null || service == null)
                return;

            AllianceMember ownMember = FindOwnMember(service);
            int ownWeekly = service.Chest != null ? Mathf.Max(0, service.Chest.playerContribution) : ownMember != null ? Mathf.Max(0, ownMember.weeklyContributionPoints) : 0;
            int missingPersonal = Mathf.Max(0, GetWeeklyChestMinContribution(service) - ownWeekly);
            string label = GameLocalization.Text("alliance.claim_chest");
            bool interactable = IsChestClaimAvailable(service);

            if (service.Chest != null && service.Chest.claimed)
                label = GameLocalization.Text("alliance.claimed");
            else if (service.Chest != null && !service.Chest.ready)
                label = GameLocalization.Text("alliance.not_ready");
            else if (missingPersonal > 0)
                label = GameLocalization.Text("alliance.need_more") + " " + missingPersonal;

            claimChestButton.interactable = interactable;
            SetButtonText(claimChestButton, label);
            Image image = claimChestButton.GetComponent<Image>();
            if (image != null)
                image.color = interactable ? new Color(0.70f, 0.84f, 1f, 1f) : new Color(0.52f, 0.62f, 0.74f, 0.82f);
        }

        private static string GetChestStatusText(AllianceService service, int missingPersonal)
        {
            AllianceChestState chest = service != null ? service.Chest : null;
            if (chest != null && chest.claimed)
                return GameLocalization.Text("alliance.claimed");
            if (chest != null && !chest.ready)
                return GameLocalization.Text("alliance.not_ready");
            if (missingPersonal > 0)
                return GameLocalization.Text("alliance.need_more") + " " + missingPersonal;
            return GameLocalization.Text("alliance.ready");
        }

        private static int GetWeeklyChestMinContribution(AllianceService service)
        {
            if (service != null)
            {
                if (service.Chest != null && service.Chest.minContribution > 0)
                    return service.Chest.minContribution;
                if (service.Rules != null && service.Rules.weeklyChestMinContribution > 0)
                    return service.Rules.weeklyChestMinContribution;
            }
            return 50;
        }

        private static int GetNextChestTierPoints(AllianceService service, int weeklyPoints)
        {
            if (service != null && service.Rules != null && service.Rules.chestTiers != null && service.Rules.chestTiers.Length > 0)
            {
                int best = 0;
                for (int i = 0; i < service.Rules.chestTiers.Length; i++)
                {
                    int points = service.Rules.chestTiers[i] != null ? service.Rules.chestTiers[i].points : 0;
                    if (points > weeklyPoints && (best == 0 || points < best))
                        best = points;
                }
                return best;
            }

            if (weeklyPoints < 1000)
                return 1000;
            if (weeklyPoints < 4000)
                return 4000;
            if (weeklyPoints < 10000)
                return 10000;
            return 0;
        }

        private void OnCreate()
        {
            if (AllianceService.I != null)
                StartCoroutine(AllianceService.I.Create(nameInput.text, tagInput.text, string.Empty, (_, message) => SetStatus(message)));
        }

        private void OnSearch()
        {
            if (AllianceService.I != null)
                StartCoroutine(AllianceService.I.Search(searchInput.text));
        }

        private void OnJoinFirst()
        {
            if (AllianceService.I == null || AllianceService.I.SearchResults.Count == 0)
                return;

            AllianceSummary alliance = AllianceService.I.SearchResults[0];
            bool requestOnly = alliance.visibility != "open";
            StartCoroutine(AllianceService.I.Join(alliance.id, requestOnly, (_, message) => SetStatus(message)));
        }

        private void OnAcceptFirstInvite()
        {
            if (AllianceService.I == null || AllianceService.I.IncomingInvites.Count == 0)
                return;

            StartCoroutine(AllianceService.I.RespondInvite(AllianceService.I.IncomingInvites[0].id, true, (_, message) => SetStatus(message)));
        }

        private void OnSendChat()
        {
            if (AllianceService.I == null || string.IsNullOrWhiteSpace(chatInput.text))
                return;
            string text = chatInput.text;
            chatInput.text = string.Empty;
            StartCoroutine(AllianceService.I.SendChat(text, (_, message) => SetStatus(message)));
        }

        private void OnInvite()
        {
            if (AllianceService.I != null && !string.IsNullOrWhiteSpace(inviteInput.text))
                StartCoroutine(AllianceService.I.Invite(inviteInput.text, (_, message) => SetStatus(message)));
        }

        private void OnUpdate()
        {
            if (AllianceService.I != null)
            {
                string announcement = AllianceService.I.Current != null ? AllianceService.I.Current.announcement : string.Empty;
                StartCoroutine(AllianceService.I.UpdateSettings(announcement, selectedWeeklyFocus, (_, message) => SetStatus(message)));
            }
        }

        private void OnAcceptFirstRequest()
        {
            if (AllianceService.I == null || AllianceService.I.PendingRequests.Count == 0)
                return;

            StartCoroutine(AllianceService.I.RespondRequest(AllianceService.I.PendingRequests[0].id, true, (_, message) => SetStatus(message)));
        }

        private void OnClaimChest()
        {
            if (AllianceService.I != null)
                StartCoroutine(AllianceService.I.ClaimChest((_, message) => SetStatus(message)));
        }

        private void OnSelectChampion()
        {
            if (AllianceService.I == null || !AllianceService.I.CanManage || AllianceService.I.Members.Count == 0)
                return;

            AllianceMember best = AllianceService.I.Members[0];
            for (int i = 1; i < AllianceService.I.Members.Count; i++)
            {
                AllianceMember candidate = AllianceService.I.Members[i];
                if (candidate.weeklyContributionPoints > best.weeklyContributionPoints)
                    best = candidate;
            }
            StartCoroutine(AllianceService.I.SelectChampion(best.userId, (_, message) => SetStatus(message)));
        }

        private void OnKickSelectedMember(int userId)
        {
            if (AllianceService.I == null || !AllianceService.I.CanManage || userId <= 0)
                return;

            StartCoroutine(AllianceService.I.Kick(userId, (_, message) =>
            {
                selectedMemberUserId = 0;
                SetStatus(message);
            }));
        }

        private void OnPromoteSelectedMember(int userId)
        {
            if (AllianceService.I == null || !AllianceService.I.IsLeader || userId <= 0)
                return;

            StartCoroutine(AllianceService.I.Promote(userId, (_, message) => SetStatus(message)));
        }

        private void OnDemoteSelectedMember(int userId)
        {
            if (AllianceService.I == null || !AllianceService.I.IsLeader || userId <= 0)
                return;

            StartCoroutine(AllianceService.I.Demote(userId, (_, message) => SetStatus(message)));
        }

        private void OnMakeChampion(int userId)
        {
            if (AllianceService.I == null || !AllianceService.I.IsLeader || userId <= 0)
                return;

            StartCoroutine(AllianceService.I.SelectChampion(userId, (_, message) => SetStatus(message)));
        }

        private void OnDonate(string currency)
        {
            TMP_InputField input = string.Equals(currency, "OzGold", System.StringComparison.OrdinalIgnoreCase) ? donateOzGoldInput : donateOzTileInput;
            int amount = 0;
            if (input == null || !int.TryParse(input.text, out amount) || amount <= 0)
            {
                SetStatus(GameLocalization.Text("alliance.donation_amount"));
                return;
            }

            if (CurrencyService.I == null || AllianceService.I == null)
            {
                SetStatus(GameLocalization.Text("alliance.error_profile"));
                return;
            }

            bool spent = string.Equals(currency, "OzGold", System.StringComparison.OrdinalIgnoreCase)
                ? CurrencyService.I.SpendOzAltin(amount)
                : CurrencyService.I.SpendOzTile(amount);
            if (!spent)
            {
                SetStatus(GameLocalization.Text("alliance.not_enough_currency"));
                return;
            }

            input.text = string.Empty;
            StartCoroutine(AllianceService.I.Donate(currency, amount, (ok, message) =>
            {
                if (!ok)
                {
                    if (string.Equals(currency, "OzGold", System.StringComparison.OrdinalIgnoreCase))
                        CurrencyService.I.AddOzAltin(amount);
                    else
                        CurrencyService.I.AddOzTile(amount);
                }

                SetStatus(ok ? GameLocalization.Text("alliance.donation_sent") : message);
            }));
        }

        private void OnTransferLeadership(int userId)
        {
            if (AllianceService.I == null || !AllianceService.I.IsLeader || userId <= 0)
                return;

            StartCoroutine(AllianceService.I.TransferLeadership(userId, (_, message) => SetStatus(message)));
        }

        private void OnLeave()
        {
            if (AllianceService.I != null)
                StartCoroutine(AllianceService.I.Leave((_, message) => SetStatus(message)));
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value ?? string.Empty;
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            ApplyUnavailableToggleLabel();
            RefreshHeaderTitle(AllianceService.I != null && AllianceService.I.HasAlliance);
            RefreshTabButtons();
            RefreshView();
        }

        private void RefreshHeaderTitle(bool hasAlliance)
        {
            if (titleText == null)
                return;

            titleText.text = hasAlliance ? GameLocalization.Text(GetTabKey(currentTab)) : GameLocalization.Text("alliance.title");
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            ApplyRoundedButton(image);
            Button button = obj.GetComponent<Button>();
            TMP_Text text = CreateText(obj.transform, "Label", label, 24f, TextAlignmentOptions.Center);
            text.color = new Color(0.92f, 0.96f, 1f, 1f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ApplyMainButtonStyle(button, text, size);
            return button;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            ApplyRoundedInput(obj.GetComponent<Image>());
            TMP_InputField input = obj.GetComponent<TMP_InputField>();
            input.targetGraphic = obj.GetComponent<Image>();
            input.interactable = true;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = new Color(0.92f, 0.96f, 1f, 1f);
            input.selectionColor = new Color(0.20f, 0.45f, 0.75f, 0.55f);
            TMP_Text text = CreateText(obj.transform, "Text", "", 24f, TextAlignmentOptions.Left);
            TMP_Text place = CreateText(obj.transform, "Placeholder", placeholder, 24f, TextAlignmentOptions.Left);
            MainLobbyButtonStyle.ApplyFont(text);
            MainLobbyButtonStyle.ApplyFont(place);
            text.raycastTarget = false;
            place.raycastTarget = false;
            place.color = new Color(1f, 1f, 1f, 0.35f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 0f);
            textRect.offsetMax = new Vector2(-18f, 0f);
            RectTransform placeRect = place.GetComponent<RectTransform>();
            placeRect.anchorMin = Vector2.zero;
            placeRect.anchorMax = Vector2.one;
            placeRect.offsetMin = new Vector2(18f, 0f);
            placeRect.offsetMax = new Vector2(-18f, 0f);
            input.textComponent = text;
            input.placeholder = place;
            return input;
        }

        private static RectTransform CreateManageGroup(Transform parent, string name, out TMP_Text title)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            obj.transform.SetParent(parent, false);
            ApplyRoundedPanel(obj, new Color(0.012f, 0.052f, 0.095f, 0.84f), 20f);

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.58f, 0.86f, 0.72f);
            outline.effectDistance = new Vector2(1f, -1f);

            title = CreateText(obj.transform, "Title", string.Empty, 22f, TextAlignmentOptions.Left);
            title.color = new Color(0.74f, 0.88f, 1f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.fontSize = 30f;
            title.fontSizeMax = 30f;
            title.fontSizeMin = 22f;
            title.enableAutoSizing = true;
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(28f, -16f);
            titleRect.sizeDelta = new Vector2(-56f, 40f);

            obj.SetActive(false);
            return obj.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TMP_Text text = obj.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
            if (label != null)
            {
                label.text = value;
                MainLobbyButtonStyle.ApplySilverTextEffect(label);
            }
        }

        private static void ApplyTabSelectedStyle(Button button, bool selected)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = selected ? "> " + label.text.TrimStart('>', ' ') : label.text.TrimStart('>', ' ');
                label.color = selected ? new Color(1f, 0.95f, 0.70f, 1f) : new Color(0.92f, 0.96f, 1f, 1f);
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                label.fontSize = selected ? 34f : 30f;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = 18f;
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();
            outline.enabled = selected;
            outline.effectColor = new Color(0.55f, 0.90f, 1f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void SetActive(Selectable selectable, bool active)
        {
            if (selectable != null)
                selectable.gameObject.SetActive(active);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private void RefreshManageGroupTitles(AllianceService service)
        {
            if (manageInviteTitle != null)
                manageInviteTitle.text = GameLocalization.Text("alliance.recruitment");
            if (manageFocusTitle != null)
                manageFocusTitle.text = GameLocalization.Text("alliance.settings");
        }

        private static void ApplyRoundedButton(Image image)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = ButtonFillColor;
            image.raycastTarget = true;
            if (image.gameObject.name == "CreateButton" || image.gameObject.name == "JoinFirstButton" || image.gameObject.name == "SearchButton")
                image.color = PrimaryButtonFillColor;
            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = ButtonBorderColor;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void ApplyAllianceWindowStyle(Image image)
        {
            if (image == null)
                return;

            Sprite bridgeSprite = IsLandscapeLayout() ? Resources.Load<Sprite>(AllianceBridgeLandscapeSpritePath) : null;
            if (bridgeSprite != null)
            {
                image.sprite = bridgeSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                Sprite sprite = Resources.Load<Sprite>(AllianceWindowSpritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Sliced;
                    image.preserveAspect = false;
                    image.color = Color.white;
                }
                else
                {
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                    image.color = WindowFillColor;
                }
            }

            Outline outline = image.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        private static Sprite LoadMemberActionGearSprite()
        {
            if (cachedMemberActionGearSprite != null)
                return cachedMemberActionGearSprite;

            cachedMemberActionGearSprite = Resources.Load<Sprite>(MemberActionGearSpritePath);
            if (cachedMemberActionGearSprite != null)
                return cachedMemberActionGearSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(MemberActionGearSpritePath);
            if (sprites != null && sprites.Length > 0)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && sprites[i].name == "SettingsButtonMain_0")
                    {
                        cachedMemberActionGearSprite = sprites[i];
                        return cachedMemberActionGearSprite;
                    }
                }

                cachedMemberActionGearSprite = sprites[0];
            }

            return cachedMemberActionGearSprite;
        }

        private static Sprite LoadMembersIconSprite()
        {
            if (cachedMembersIconSprite != null)
                return cachedMembersIconSprite;

            cachedMembersIconSprite = Resources.Load<Sprite>(MembersIconSpritePath);
            if (cachedMembersIconSprite != null)
                return cachedMembersIconSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(MembersIconSpritePath);
            if (sprites != null && sprites.Length > 0)
            {
                cachedMembersIconSprite = sprites[0];
                return cachedMembersIconSprite;
            }

            return cachedMembersIconSprite;
        }

        private static void ApplyMainButtonStyle(Button button, TMP_Text label, Vector2 size)
        {
            if (button == null)
                return;

            bool hasMainSprite = MainLobbyButtonStyle.ButtonSprite != null;
            if (hasMainSprite)
            {
                Outline outline = button.GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = false;

                MainLobbyButtonStyle.Apply(button);
                if (button.image != null)
                {
                    button.image.preserveAspect = false;
                    button.image.color = Color.white;
                }
            }

            if (label == null)
                label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            float fontSize = size.y <= 54f ? 22f : size.y <= 62f ? 24f : 28f;
            if (button.gameObject.name == "AllianceButton")
                fontSize = 30f;
            else if (button.gameObject.name == "Close")
                fontSize = 26f;
            else if (button.gameObject.name.EndsWith("Tab", System.StringComparison.Ordinal))
                fontSize = size.y >= 76f ? 34f : size.y >= 66f ? 30f : 24f;

            MainLobbyButtonStyle.ApplyFont(label);
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
        }

        private static void ApplyRoundedInput(Image image)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = InputFillColor;
            image.raycastTarget = true;
            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.56f, 0.72f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetInputRect(TMP_InputField input, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = input != null ? input.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            ConfigureInputText(input, size.y >= 64f ? 30f : 24f, size.y >= 64f ? 20f : 16f);
        }

        private static void SetButtonRect(Button button, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            ApplyMainButtonStyle(button, button.GetComponentInChildren<TMP_Text>(true), size);
        }

        private static void SetButtonRectWithPivot(Button button, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            ApplyMainButtonStyle(button, button.GetComponentInChildren<TMP_Text>(true), size);
        }

        private static void ConfigureInputText(TMP_InputField input, float maxSize, float minSize)
        {
            if (input == null)
                return;

            TMP_Text text = input.textComponent;
            if (text != null)
            {
                MainLobbyButtonStyle.ApplyFont(text);
                text.fontSize = maxSize;
                text.fontSizeMax = maxSize;
                text.fontSizeMin = minSize;
                text.enableAutoSizing = true;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.overflowMode = TextOverflowModes.Truncate;
            }

            TMP_Text placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
            {
                MainLobbyButtonStyle.ApplyFont(placeholder);
                placeholder.fontSize = maxSize;
                placeholder.fontSizeMax = maxSize;
                placeholder.fontSizeMin = minSize;
                placeholder.enableAutoSizing = true;
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
                placeholder.overflowMode = TextOverflowModes.Truncate;
            }
        }

        private static void SetManageGroupRect(RectTransform rect, float xMin, float yMin, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMin + width, yMin + height);
            rect.pivot = new Vector2(0f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
