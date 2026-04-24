using System.Collections.Generic;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class LocalWifiBattleLobbyUI : MonoBehaviour
    {
        private const string RootName = "LocalWifiBattleLobbyOverlay";

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private GameObject actionsRoot;
        private GameObject roomRoot;
        private TMP_Text titleText;
        private TMP_Text statusText;
        private TMP_Text roomTitleText;
        private TMP_Text roomLocalText;
        private TMP_Text roomPeerText;
        private TMP_Text roomHintText;
        private RectTransform gamesRoot;
        private Button createButton;
        private Button searchButton;
        private Button startButton;
        private Button leaveRoomButton;
        private Button closeButton;
        private readonly List<Button> gameButtons = new();

        public static LocalWifiBattleLobbyUI Show(string battleSceneName)
        {
            LocalWifiBattleLobbyUI existing = FindAnyObjectByType<LocalWifiBattleLobbyUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                existing.Open();
                return existing;
            }

            GameObject host = new GameObject("LocalWifiBattleLobbyUI");
            LocalWifiBattleLobbyUI ui = host.AddComponent<LocalWifiBattleLobbyUI>();
            ui.Configure(battleSceneName);
            ui.Open();
            return ui;
        }

        private void Configure(string battleSceneName)
        {
            if (!string.IsNullOrWhiteSpace(battleSceneName))
                battleGameSceneName = battleSceneName;
        }

        private void Awake()
        {
            BuildUi();
            BindNetwork();
        }

        private void OnEnable()
        {
            BindNetwork();
            RefreshStatus(LocalWifiBattleNetwork.EnsureInstance().Status);
        }

        private void OnDestroy()
        {
            UnbindNetwork();
        }

        public void Open()
        {
            BuildUi();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            RefreshStatus(LocalWifiBattleNetwork.EnsureInstance().Status);
        }

        private void Close()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void CloseForMatchLoad()
        {
            UnbindNetwork();

            if (root != null)
            {
                root.SetActive(false);
                Destroy(root);
                root = null;
            }

            Destroy(gameObject);
        }

        private void BindNetwork()
        {
            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.EnsureInstance();
            network.DiscoveryChanged -= HandleDiscoveryChanged;
            network.StatusChanged -= RefreshStatus;
            network.PeerInfoChanged -= HandlePeerInfoChanged;
            network.ConnectionStateChanged -= HandleConnectionStateChanged;
            network.MatchStartRequested -= HandleMatchStartRequested;
            network.ConnectionClosed -= HandleConnectionClosed;
            network.DiscoveryChanged += HandleDiscoveryChanged;
            network.StatusChanged += RefreshStatus;
            network.PeerInfoChanged += HandlePeerInfoChanged;
            network.ConnectionStateChanged += HandleConnectionStateChanged;
            network.MatchStartRequested += HandleMatchStartRequested;
            network.ConnectionClosed += HandleConnectionClosed;
        }

        private void UnbindNetwork()
        {
            if (LocalWifiBattleNetwork.I == null)
                return;

            LocalWifiBattleNetwork.I.DiscoveryChanged -= HandleDiscoveryChanged;
            LocalWifiBattleNetwork.I.StatusChanged -= RefreshStatus;
            LocalWifiBattleNetwork.I.PeerInfoChanged -= HandlePeerInfoChanged;
            LocalWifiBattleNetwork.I.ConnectionStateChanged -= HandleConnectionStateChanged;
            LocalWifiBattleNetwork.I.MatchStartRequested -= HandleMatchStartRequested;
            LocalWifiBattleNetwork.I.ConnectionClosed -= HandleConnectionClosed;
        }

        private void HandleMatchStartRequested(int seed)
        {
            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.EnsureInstance();
            LocalWifiBattleNetwork.RemotePlayerInfo remote = network.RemotePlayer;

            MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
            {
                Id = "local_wifi_peer",
                DisplayName = remote != null ? remote.DisplayName : "Wi-Fi Player",
                RankTier = remote != null ? remote.RankTier : "Unranked",
                RankPoints = remote != null ? remote.RankPoints : 0,
                Level = remote != null ? Mathf.Max(1, 1 + Mathf.Max(0, remote.RankPoints) / 100) : 1,
                AvatarId = 0,
                IsBot = false
            };

            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.LocalWifiMatch);
            MahjongSession.StartBattle(opponent, 0, seed);
            CloseForMatchLoad();
            SceneManager.LoadScene(battleGameSceneName);
        }

        private void OnCreateClicked()
        {
            LocalWifiBattleNetwork.EnsureInstance().StartHost(CreateLocalPlayerInfo());
            ShowRoom(true);
        }

        private void OnSearchClicked()
        {
            ShowActions();
            LocalWifiBattleNetwork.EnsureInstance().StartDiscovery();
        }

        private void OnStartClicked()
        {
            LocalWifiBattleNetwork.EnsureInstance().StartHostedMatch();
            RefreshRoom();
        }

        private void OnLeaveRoomClicked()
        {
            LocalWifiBattleNetwork.EnsureInstance().StopAllNetworking();
            ShowActions();
            CreateInfoRow("Create a game or search in the same Wi-Fi");
        }

        private void HandleDiscoveryChanged(IReadOnlyList<LocalWifiBattleNetwork.DiscoveredGame> games)
        {
            if (LocalWifiBattleNetwork.I != null && LocalWifiBattleNetwork.I.IsHost)
                return;

            ClearGameButtons();

            if (games == null || games.Count == 0)
            {
                CreateInfoRow("No local battles found yet");
                return;
            }

            for (int i = 0; i < games.Count; i++)
            {
                LocalWifiBattleNetwork.DiscoveredGame game = games[i];
                Button button = CreateButton(
                    gamesRoot,
                    $"JoinGame_{i}",
                    $"Join {game.HostName}  {game.Address}",
                    new Vector2(0f, -i * 64f),
                    new Vector2(560f, 54f));
                button.onClick.AddListener(() =>
                {
                    LocalWifiBattleNetwork.EnsureInstance().ConnectTo(game, CreateLocalPlayerInfo());
                    ShowRoom(false);
                });
                gameButtons.Add(button);
            }
        }

        private void HandlePeerInfoChanged(LocalWifiBattleNetwork.RemotePlayerInfo _)
        {
            RefreshRoom();
        }

        private void HandleConnectionStateChanged(bool _)
        {
            RefreshRoom();
        }

        private void HandleConnectionClosed()
        {
            RefreshRoom();
        }

        private void RefreshStatus(string value)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(value) ? "Choose Host or Search" : value;

            RefreshRoom();
        }

        private LocalWifiBattleNetwork.LocalPlayerInfo CreateLocalPlayerInfo()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
            {
                LocalWifiBattleNetwork.LocalPlayerInfo fallback = LocalWifiBattleNetwork.LocalPlayerInfo.CreateFallback();
                fallback.CharacterId = ResolveSelectedCharacterId();
                return fallback;
            }

            profile.EnsureData();

            string rankTier = "Unranked";
            int rankPoints = 0;
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
            {
                rankTier = string.IsNullOrWhiteSpace(profile.Mahjong.Battle.RankTier)
                    ? "Unranked"
                    : profile.Mahjong.Battle.RankTier;
                rankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
            }

            return new LocalWifiBattleNetwork.LocalPlayerInfo
            {
                DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "Player" : profile.DisplayName.Trim(),
                RankTier = rankTier,
                RankPoints = rankPoints,
                CharacterId = ResolveSelectedCharacterId()
            };
        }

        private static string ResolveSelectedCharacterId()
        {
            return BattleCharacterSelectionService.HasInstance
                ? BattleCharacterSelectionService.Instance.SelectedCharacterId
                : string.Empty;
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                canvas = CreateCanvas();

            root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            GameObject panel = new GameObject("LocalWifiBattlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(680f, 560f);

            Image panelImage = panel.GetComponent<Image>();
            BattlePopupStyle.ApplyWindow(panelImage);

            titleText = CreateText(panel.transform, "Title", "Wi-Fi Battle", new Vector2(0f, 214f), new Vector2(600f, 58f), 44f);
            statusText = CreateText(panel.transform, "Status", "Choose Host or Search", new Vector2(0f, 158f), new Vector2(590f, 58f), 22f);

            actionsRoot = new GameObject("ActionsRoot", typeof(RectTransform));
            actionsRoot.transform.SetParent(panel.transform, false);
            RectTransform actionsRect = actionsRoot.GetComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0.5f, 0.5f);
            actionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            actionsRect.pivot = new Vector2(0.5f, 0.5f);
            actionsRect.anchoredPosition = Vector2.zero;
            actionsRect.sizeDelta = new Vector2(620f, 330f);
            CreateFrontPanel(actionsRoot.transform, "ActionsFront", Vector2.zero, new Vector2(620f, 330f));

            createButton = CreateButton(actionsRoot.transform, "CreateButton", "Create Game", new Vector2(-154f, 86f), new Vector2(280f, 62f));
            searchButton = CreateButton(actionsRoot.transform, "SearchButton", "Search", new Vector2(154f, 86f), new Vector2(280f, 62f));
            closeButton = CreateButton(panel.transform, "CloseButton", "Close", new Vector2(0f, -230f), new Vector2(260f, 56f));

            createButton.onClick.AddListener(OnCreateClicked);
            searchButton.onClick.AddListener(OnSearchClicked);
            closeButton.onClick.AddListener(Close);

            GameObject list = new GameObject("DiscoveredGames", typeof(RectTransform));
            list.transform.SetParent(actionsRoot.transform, false);
            gamesRoot = list.GetComponent<RectTransform>();
            gamesRoot.anchorMin = new Vector2(0.5f, 0.5f);
            gamesRoot.anchorMax = new Vector2(0.5f, 0.5f);
            gamesRoot.pivot = new Vector2(0.5f, 1f);
            gamesRoot.anchoredPosition = new Vector2(0f, 24f);
            gamesRoot.sizeDelta = new Vector2(580f, 240f);

            BuildRoomUi(panel.transform);
            ShowActions();
            CreateInfoRow("Create a game or search in the same Wi-Fi");
        }

        private void BuildRoomUi(Transform parent)
        {
            roomRoot = new GameObject("RoomRoot", typeof(RectTransform));
            roomRoot.transform.SetParent(parent, false);

            RectTransform roomRect = roomRoot.GetComponent<RectTransform>();
            roomRect.anchorMin = new Vector2(0.5f, 0.5f);
            roomRect.anchorMax = new Vector2(0.5f, 0.5f);
            roomRect.pivot = new Vector2(0.5f, 0.5f);
            roomRect.anchoredPosition = new Vector2(0f, -16f);
            roomRect.sizeDelta = new Vector2(620f, 330f);
            CreateFrontPanel(roomRoot.transform, "RoomFront", Vector2.zero, new Vector2(620f, 330f));

            roomTitleText = CreateText(roomRoot.transform, "RoomTitle", "Room Created", new Vector2(0f, 116f), new Vector2(580f, 42f), 30f);
            roomLocalText = CreateText(roomRoot.transform, "LocalPlayer", "You: active", new Vector2(0f, 62f), new Vector2(560f, 38f), 24f);
            roomPeerText = CreateText(roomRoot.transform, "PeerPlayer", "Waiting for second player...", new Vector2(0f, 16f), new Vector2(560f, 38f), 24f);
            roomHintText = CreateText(roomRoot.transform, "RoomHint", "Ask the second player to press Search and join this room.", new Vector2(0f, -38f), new Vector2(580f, 56f), 20f);

            startButton = CreateButton(roomRoot.transform, "StartButton", "Start", new Vector2(-150f, -126f), new Vector2(260f, 58f));
            leaveRoomButton = CreateButton(roomRoot.transform, "LeaveRoomButton", "Leave", new Vector2(150f, -126f), new Vector2(260f, 58f));
            startButton.onClick.AddListener(OnStartClicked);
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        }

        private void ShowActions()
        {
            if (actionsRoot != null)
                actionsRoot.SetActive(true);
            if (roomRoot != null)
                roomRoot.SetActive(false);
            if (titleText != null)
                titleText.text = "Wi-Fi Battle";
            RefreshStatus(LocalWifiBattleNetwork.EnsureInstance().Status);
        }

        private void ShowRoom(bool asHost)
        {
            if (actionsRoot != null)
                actionsRoot.SetActive(false);
            if (roomRoot != null)
                roomRoot.SetActive(true);
            if (titleText != null)
                titleText.text = asHost ? "Wi-Fi Room" : "Joining Room";
            RefreshRoom();
        }

        private void RefreshRoom()
        {
            if (roomRoot == null || !roomRoot.activeSelf)
                return;

            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.EnsureInstance();
            LocalWifiBattleNetwork.RemotePlayerInfo remote = network.RemotePlayer;
            bool isHost = network.IsHost;
            bool hasConnection = network.IsConnected;
            bool hasPeer = hasConnection && remote != null;

            if (roomTitleText != null)
                roomTitleText.text = isHost ? "Room Created" : "Connected to Room";

            if (roomLocalText != null)
                roomLocalText.text = "You: active";

            if (roomPeerText != null)
                roomPeerText.text = hasPeer
                    ? $"Second player: {remote.DisplayName}"
                    : (hasConnection ? "Second player: connected" : "Waiting for second player...");

            if (roomHintText != null)
                roomHintText.text = isHost
                    ? (hasConnection ? "Second player joined. Press Start to begin." : "Room is visible. Waiting for another player to join.")
                    : "Waiting for host to start the battle.";

            if (startButton != null)
            {
                startButton.gameObject.SetActive(isHost);
                startButton.interactable = hasConnection && !network.MatchStarted;
            }
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("LocalWifiCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static Image CreateFrontPanel(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.transform.SetAsFirstSibling();

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            BattlePopupStyle.ApplyFront(image);
            return image;
        }

        private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            BattlePopupStyle.ApplyText(text);
            return text;
        }

        private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.24f, 0.28f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(28f, 12f), 24f);
            BattlePopupStyle.ApplyButton(button);
            return button;
        }

        private void CreateInfoRow(string message)
        {
            ClearGameButtons();
            Button row = CreateButton(gamesRoot, "InfoRow", message, Vector2.zero, new Vector2(560f, 54f));
            row.interactable = false;
            gameButtons.Add(row);
        }

        private void ClearGameButtons()
        {
            for (int i = 0; i < gameButtons.Count; i++)
            {
                if (gameButtons[i] != null)
                    Destroy(gameButtons[i].gameObject);
            }

            gameButtons.Clear();
        }
    }
}
