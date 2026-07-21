using System;
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

	private const string WindowSpritePath = "Mahjong/Sprites/BattleLobbyUI/TournamentWindow";

	private const string InfoPanelSpritePath = "Mahjong/Sprites/BattleLobbyUI/InfoPanel";

	private const string ButtonSpritePath = "Mahjong/Sprites/BattleLobbyUI/Battlebutton";

	private const string DividerSpritePath = "Mahjong/Sprites/BattleLobbyUI/Divider";

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

	private readonly List<Button> gameButtons = new List<Button>();

	private static Sprite windowSprite;

	private static Sprite infoPanelSprite;

	private static Sprite buttonSprite;

	private static Sprite dividerSprite;

	public static LocalWifiBattleLobbyUI Show(string battleSceneName)
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			return null;
		}
		LocalWifiBattleLobbyUI localWifiBattleLobbyUI = UnityEngine.Object.FindAnyObjectByType<LocalWifiBattleLobbyUI>(FindObjectsInactive.Include);
		if (localWifiBattleLobbyUI != null)
		{
			localWifiBattleLobbyUI.Configure(battleSceneName);
			localWifiBattleLobbyUI.Open();
			return localWifiBattleLobbyUI;
		}
		LocalWifiBattleLobbyUI localWifiBattleLobbyUI2 = new GameObject("LocalWifiBattleLobbyUI").AddComponent<LocalWifiBattleLobbyUI>();
		localWifiBattleLobbyUI2.Configure(battleSceneName);
		localWifiBattleLobbyUI2.Open();
		return localWifiBattleLobbyUI2;
	}

	private void Configure(string battleSceneName)
	{
		if (!string.IsNullOrWhiteSpace(battleSceneName))
		{
			battleGameSceneName = battleSceneName;
		}
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
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.WifiMatch);
		UnbindNetwork();
	}

	public void Open()
	{
		BuildUi();
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.WifiMatch);
		root.SetActive(value: true);
		root.transform.SetAsLastSibling();
		RefreshStatus(LocalWifiBattleNetwork.EnsureInstance().Status);
	}

	private void Close()
	{
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.WifiMatch);
		if (root != null)
		{
			root.SetActive(value: false);
		}
	}

	private void CloseForMatchLoad()
	{
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.WifiMatch);
		UnbindNetwork();
		if (root != null)
		{
			root.SetActive(value: false);
			UnityEngine.Object.Destroy(root);
			root = null;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void BindNetwork()
	{
		LocalWifiBattleNetwork localWifiBattleNetwork = LocalWifiBattleNetwork.EnsureInstance();
		localWifiBattleNetwork.DiscoveryChanged -= HandleDiscoveryChanged;
		localWifiBattleNetwork.StatusChanged -= RefreshStatus;
		localWifiBattleNetwork.PeerInfoChanged -= HandlePeerInfoChanged;
		localWifiBattleNetwork.ConnectionStateChanged -= HandleConnectionStateChanged;
		localWifiBattleNetwork.MatchStartRequested -= HandleMatchStartRequested;
		localWifiBattleNetwork.ConnectionClosed -= HandleConnectionClosed;
		localWifiBattleNetwork.DiscoveryChanged += HandleDiscoveryChanged;
		localWifiBattleNetwork.StatusChanged += RefreshStatus;
		localWifiBattleNetwork.PeerInfoChanged += HandlePeerInfoChanged;
		localWifiBattleNetwork.ConnectionStateChanged += HandleConnectionStateChanged;
		localWifiBattleNetwork.MatchStartRequested += HandleMatchStartRequested;
		localWifiBattleNetwork.ConnectionClosed += HandleConnectionClosed;
	}

	private void UnbindNetwork()
	{
		if (!(LocalWifiBattleNetwork.I == null))
		{
			LocalWifiBattleNetwork.I.DiscoveryChanged -= HandleDiscoveryChanged;
			LocalWifiBattleNetwork.I.StatusChanged -= RefreshStatus;
			LocalWifiBattleNetwork.I.PeerInfoChanged -= HandlePeerInfoChanged;
			LocalWifiBattleNetwork.I.ConnectionStateChanged -= HandleConnectionStateChanged;
			LocalWifiBattleNetwork.I.MatchStartRequested -= HandleMatchStartRequested;
			LocalWifiBattleNetwork.I.ConnectionClosed -= HandleConnectionClosed;
		}
	}

	private void HandleMatchStartRequested(int seed)
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			return;
		}
		LocalWifiBattleNetwork.RemotePlayerInfo remotePlayer = LocalWifiBattleNetwork.EnsureInstance().RemotePlayer;
		BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
		if (remotePlayer?.Loadout == null || !remotePlayer.Loadout.IsCompleteForStore(store))
		{
			if (statusText != null)
				statusText.text = LocalizeOpponentLoadoutStatus(waiting: false);
			return;
		}
		if (!EnergyService.TrySpendForMatch())
		{
			if (statusText != null)
			{
				statusText.text = GameLocalization.Format("battle.energy.not_enough", 10);
			}
			return;
		}
		MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
		{
			Id = "local_wifi_peer",
			DisplayName = ((remotePlayer != null) ? remotePlayer.DisplayName : GameLocalization.Text("battle.wifi.player")),
			RankTier = ((remotePlayer != null) ? LocalizeRankTier(remotePlayer.RankTier) : GameLocalization.Text("battle.rank.unranked")),
			RankPoints = (remotePlayer?.RankPoints ?? 0),
			Level = ((remotePlayer == null) ? 1 : Mathf.Max(1, 1 + Mathf.Max(0, remotePlayer.RankPoints) / 100)),
			AvatarId = 0,
			IsBot = false,
			Loadout = remotePlayer.Loadout.Clone()
		};
		MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.LocalWifiMatch);
		MahjongSession.StartBattle(opponent, 0, seed);
		CloseForMatchLoad();
		SceneManager.LoadScene(battleGameSceneName);
	}

	private void OnCreateClicked()
	{
		LocalWifiBattleNetwork.EnsureInstance().StartHost(CreateLocalPlayerInfo());
		ShowRoom(asHost: true);
	}

	private void OnSearchClicked()
	{
		ShowActions();
		LocalWifiBattleNetwork.EnsureInstance().StartDiscovery();
	}

	private void OnStartClicked()
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
			return;

		BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
		LocalWifiBattleNetwork.RemotePlayerInfo remotePlayer = LocalWifiBattleNetwork.EnsureInstance().RemotePlayer;
		if (remotePlayer?.Loadout == null || !remotePlayer.Loadout.IsCompleteForStore(store))
		{
			if (statusText != null)
				statusText.text = LocalizeOpponentLoadoutStatus(waiting: true);
			return;
		}

		LocalWifiBattleNetwork.EnsureInstance().StartHostedMatch();
		RefreshRoom();
	}

	private void OnLeaveRoomClicked()
	{
		LocalWifiBattleNetwork.EnsureInstance().StopAllNetworking();
		ShowActions();
		CreateInfoRow(GameLocalization.Text("battle.wifi.info"));
	}

	private void HandleDiscoveryChanged(IReadOnlyList<LocalWifiBattleNetwork.DiscoveredGame> games)
	{
		if (LocalWifiBattleNetwork.I != null && LocalWifiBattleNetwork.I.IsHost)
		{
			return;
		}
		ClearGameButtons();
		if (games == null || games.Count == 0)
		{
			CreateInfoRow(GameLocalization.Text("battle.wifi.none"));
			return;
		}
		for (int i = 0; i < games.Count; i++)
		{
			LocalWifiBattleNetwork.DiscoveredGame game = games[i];
			Button button = CreateButton(gamesRoot, $"JoinGame_{i}", GameLocalization.Format("battle.wifi.join", game.HostName, game.Address), new Vector2(0f, (float)(-i) * 64f), new Vector2(560f, 54f));
			button.onClick.AddListener(delegate
			{
				LocalWifiBattleNetwork.EnsureInstance().ConnectTo(game, CreateLocalPlayerInfo());
				ShowRoom(asHost: false);
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
		{
			statusText.text = (string.IsNullOrWhiteSpace(value) ? GameLocalization.Text("battle.wifi.choose") : LocalizeNetworkStatus(value));
		}
		RefreshRoom();
	}

	private LocalWifiBattleNetwork.LocalPlayerInfo CreateLocalPlayerInfo()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		if (playerProfile == null)
		{
			LocalWifiBattleNetwork.LocalPlayerInfo localPlayerInfo = LocalWifiBattleNetwork.LocalPlayerInfo.CreateFallback();
			localPlayerInfo.CharacterId = ResolveSelectedCharacterId();
			return localPlayerInfo;
		}
		playerProfile.EnsureData();
		BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
		BattleLoadoutSnapshot.TryCreateFromProfile(playerProfile, store, out BattleLoadoutSnapshot loadout);
		string rankTier = GameLocalization.Text("battle.rank.unranked");
		int rankPoints = 0;
		if (playerProfile.Mahjong != null && playerProfile.Mahjong.Battle != null)
		{
			rankTier = (string.IsNullOrWhiteSpace(playerProfile.Mahjong.Battle.RankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(playerProfile.Mahjong.Battle.RankTier));
			rankPoints = Mathf.Max(0, playerProfile.Mahjong.Battle.RankPoints);
		}
		return new LocalWifiBattleNetwork.LocalPlayerInfo
		{
			DisplayName = (string.IsNullOrWhiteSpace(playerProfile.DisplayName) ? GameLocalization.Text("battle.common.player") : playerProfile.DisplayName.Trim()),
			RankTier = rankTier,
			RankPoints = rankPoints,
			CharacterId = ResolveSelectedCharacterId(),
			Loadout = loadout
		};
	}

	private static string ResolveSelectedCharacterId()
	{
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return string.Empty;
		}
		return BattleCharacterSelectionService.Instance.SelectedCharacterId;
	}

	private static string LocalizeOpponentLoadoutStatus(bool waiting)
	{
		GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
		return language switch
		{
			GameLanguage.English => waiting ? "Waiting for the opponent's complete 18-tile loadout." : "The opponent does not have a complete 18-tile loadout.",
			GameLanguage.Turkish => waiting ? "Rakibin 18 taşlık tam dizilimi bekleniyor." : "Rakibin 18 taşlık tam dizilimi yok.",
			GameLanguage.German => waiting ? "Warte auf das vollständige 18-Steine-Set des Gegners." : "Der Gegner hat kein vollständiges 18-Steine-Set.",
			_ => waiting ? "Ожидаем полный активный набор соперника: 18 камней." : "У соперника не собран полный активный набор из 18 камней."
		};
	}

	private void BuildUi()
	{
		if (!(root != null))
		{
			Canvas orCreatePopupCanvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
			root = new GameObject("LocalWifiBattleLobbyOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			root.transform.SetParent(orCreatePopupCanvas.transform, worldPositionStays: false);
			RectTransform component = root.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = root.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.48f);
			component2.raycastTarget = true;
			GameObject gameObject = new GameObject("LocalWifiBattlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(root.transform, worldPositionStays: false);
			RectTransform component3 = gameObject.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0.5f, 0.5f);
			component3.anchorMax = new Vector2(0.5f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			component3.anchoredPosition = new Vector2(0f, 18f);
			component3.sizeDelta = new Vector2(1540f, 760f);
			FitPanelInsideCanvas(component3, orCreatePopupCanvas, 44f);
			BattlePopupStyle.ApplyWindow(gameObject.GetComponent<Image>());
			titleText = CreateText(gameObject.transform, "Title", GameLocalization.Text("battle.wifi.title"), new Vector2(0f, 276f), new Vector2(1040f, 76f), 56f);
			statusText = CreateText(gameObject.transform, "Status", GameLocalization.Text("battle.wifi.choose"), new Vector2(0f, 214f), new Vector2(980f, 48f), 34f);
			CreateDivider(gameObject.transform, "TopDivider", new Vector2(0f, 174f), new Vector2(1130f, 8f));
			actionsRoot = new GameObject("ActionsRoot", typeof(RectTransform));
			actionsRoot.transform.SetParent(gameObject.transform, worldPositionStays: false);
			RectTransform component4 = actionsRoot.GetComponent<RectTransform>();
			component4.anchorMin = new Vector2(0.5f, 0.5f);
			component4.anchorMax = new Vector2(0.5f, 0.5f);
			component4.pivot = new Vector2(0.5f, 0.5f);
			component4.anchoredPosition = new Vector2(0f, 0f);
			component4.sizeDelta = new Vector2(1120f, 390f);
			CreateFrontPanel(actionsRoot.transform, "ActionsFront", Vector2.zero, new Vector2(1120f, 390f));
			createButton = CreateButton(actionsRoot.transform, "CreateButton", GameLocalization.Text("battle.wifi.create"), new Vector2(-300f, 120f), new Vector2(420f, 92f));
			searchButton = CreateButton(actionsRoot.transform, "SearchButton", GameLocalization.Text("battle.wifi.search"), new Vector2(300f, 120f), new Vector2(420f, 92f));
			CreateDivider(gameObject.transform, "BottomDivider", new Vector2(0f, -206f), new Vector2(860f, 8f));
			closeButton = CreateButton(gameObject.transform, "CloseButton", GameLocalization.Text("battle.common.close"), new Vector2(0f, -282f), new Vector2(440f, 80f));
			createButton.onClick.AddListener(OnCreateClicked);
			searchButton.onClick.AddListener(OnSearchClicked);
			closeButton.onClick.AddListener(Close);
			GameObject gameObject2 = new GameObject("DiscoveredGames", typeof(RectTransform));
			gameObject2.transform.SetParent(actionsRoot.transform, worldPositionStays: false);
			gamesRoot = gameObject2.GetComponent<RectTransform>();
			gamesRoot.anchorMin = new Vector2(0.5f, 0.5f);
			gamesRoot.anchorMax = new Vector2(0.5f, 0.5f);
			gamesRoot.pivot = new Vector2(0.5f, 1f);
			gamesRoot.anchoredPosition = new Vector2(0f, 20f);
			gamesRoot.sizeDelta = new Vector2(940f, 210f);
			BuildRoomUi(gameObject.transform);
			ShowActions();
			CreateInfoRow(GameLocalization.Text("battle.wifi.info"));
		}
	}

	private void BuildRoomUi(Transform parent)
	{
		roomRoot = new GameObject("RoomRoot", typeof(RectTransform));
		roomRoot.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = roomRoot.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = new Vector2(0f, 0f);
		component.sizeDelta = new Vector2(1120f, 390f);
		CreateFrontPanel(roomRoot.transform, "RoomFront", Vector2.zero, new Vector2(1120f, 390f));
		roomTitleText = CreateText(roomRoot.transform, "RoomTitle", GameLocalization.Text("battle.wifi.room_created"), new Vector2(0f, 134f), new Vector2(900f, 52f), 38f);
		roomLocalText = CreateText(roomRoot.transform, "LocalPlayer", GameLocalization.Text("battle.wifi.you_active"), new Vector2(0f, 72f), new Vector2(900f, 42f), 30f);
		roomPeerText = CreateText(roomRoot.transform, "PeerPlayer", GameLocalization.Text("battle.wifi.wait_second"), new Vector2(0f, 20f), new Vector2(900f, 42f), 30f);
		roomHintText = CreateText(roomRoot.transform, "RoomHint", GameLocalization.Text("battle.wifi.host_hint"), new Vector2(0f, -48f), new Vector2(920f, 72f), 28f);
		startButton = CreateButton(roomRoot.transform, "StartButton", GameLocalization.Text("battle.common.start"), new Vector2(-260f, -142f), new Vector2(400f, 86f));
		leaveRoomButton = CreateButton(roomRoot.transform, "LeaveRoomButton", GameLocalization.Text("battle.common.leave"), new Vector2(260f, -142f), new Vector2(400f, 86f));
		startButton.onClick.AddListener(OnStartClicked);
		leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
	}

	private void ShowActions()
	{
		if (actionsRoot != null)
		{
			actionsRoot.SetActive(value: true);
		}
		if (roomRoot != null)
		{
			roomRoot.SetActive(value: false);
		}
		if (titleText != null)
		{
			titleText.text = GameLocalization.Text("battle.wifi.title");
		}
		RefreshStatus(LocalWifiBattleNetwork.EnsureInstance().Status);
	}

	private void ShowRoom(bool asHost)
	{
		if (actionsRoot != null)
		{
			actionsRoot.SetActive(value: false);
		}
		if (roomRoot != null)
		{
			roomRoot.SetActive(value: true);
		}
		if (titleText != null)
		{
			titleText.text = (asHost ? GameLocalization.Text("battle.wifi.room") : GameLocalization.Text("battle.wifi.joining"));
		}
		RefreshRoom();
	}

	private void RefreshRoom()
	{
		if (!(roomRoot == null) && roomRoot.activeSelf)
		{
			LocalWifiBattleNetwork localWifiBattleNetwork = LocalWifiBattleNetwork.EnsureInstance();
			LocalWifiBattleNetwork.RemotePlayerInfo remotePlayer = localWifiBattleNetwork.RemotePlayer;
			bool isHost = localWifiBattleNetwork.IsHost;
			bool isConnected = localWifiBattleNetwork.IsConnected;
			bool flag = isConnected && remotePlayer != null;
			if (roomTitleText != null)
			{
				roomTitleText.text = (isHost ? GameLocalization.Text("battle.wifi.room_created") : GameLocalization.Text("battle.wifi.connected_room"));
			}
			if (roomLocalText != null)
			{
				roomLocalText.text = GameLocalization.Text("battle.wifi.you_active");
			}
			if (roomPeerText != null)
			{
				roomPeerText.text = (flag ? GameLocalization.Format("battle.wifi.second_player", remotePlayer.DisplayName) : (isConnected ? GameLocalization.Text("battle.wifi.second_connected") : GameLocalization.Text("battle.wifi.wait_second")));
			}
			if (roomHintText != null)
			{
				roomHintText.text = ((!isHost) ? GameLocalization.Text("battle.wifi.wait_host") : (isConnected ? GameLocalization.Text("battle.wifi.joined_hint") : GameLocalization.Text("battle.wifi.visible_hint")));
			}
			if (startButton != null)
			{
				startButton.gameObject.SetActive(isHost);
				startButton.interactable = isConnected && !localWifiBattleNetwork.MatchStarted;
			}
		}
	}

	private static Image CreateFrontPanel(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.transform.SetAsFirstSibling();
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		ApplySimpleSprite(component2, LoadInfoPanelSprite(), raycastTarget: false);
		return component2;
	}

	private static void CreateDivider(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		ApplySimpleSprite(obj.GetComponent<Image>(), LoadDividerSprite(), raycastTarget: false);
	}

	private static void FitPanelInsideCanvas(RectTransform panel, Canvas canvas, float padding)
	{
		RectTransform rectTransform = ((canvas != null) ? (canvas.transform as RectTransform) : null);
		if (!(panel == null) && !(rectTransform == null))
		{
			Vector2 vector = rectTransform.rect.size - Vector2.one * Mathf.Max(0f, padding * 2f);
			if (!(vector.x <= 1f) && !(vector.y <= 1f))
			{
				Vector2 sizeDelta = panel.sizeDelta;
				float num = Mathf.Min(1f, vector.x / Mathf.Max(1f, sizeDelta.x), vector.y / Mathf.Max(1f, sizeDelta.y));
				panel.localScale = Vector3.one * num;
			}
		}
	}

	private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		TextMeshProUGUI component2 = obj.GetComponent<TextMeshProUGUI>();
		component2.text = value;
		component2.fontSize = fontSize;
		component2.enableAutoSizing = true;
		component2.fontSizeMin = Mathf.Max(18f, fontSize * 0.64f);
		component2.fontSizeMax = fontSize;
		component2.alignment = TextAlignmentOptions.Center;
		component2.color = Color.white;
		component2.raycastTarget = false;
		BattlePopupStyle.ApplyText(component2, silver: true);
		return component2;
	}

	private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.16f, 0.24f, 0.28f, 0.96f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		CreateText(gameObject.transform, "Label", label, Vector2.zero, size - new Vector2(72f, 20f), 34f);
		BattlePopupStyle.ApplyButton(component3);
		BattlePopupStyle.ApplyButtonLabel(component3, 34f);
		return component3;
	}

	private static void ApplySimpleSprite(Image image, Sprite sprite, bool raycastTarget)
	{
		if (!(image == null))
		{
			image.sprite = sprite;
			image.type = Image.Type.Simple;
			image.preserveAspect = false;
			image.color = Color.white;
			image.raycastTarget = raycastTarget;
		}
	}

	private static Sprite LoadWindowSprite()
	{
		if (windowSprite == null)
		{
			windowSprite = LoadBattleLobbySprite("Mahjong/Sprites/BattleLobbyUI/TournamentWindow");
		}
		return windowSprite;
	}

	private static Sprite LoadInfoPanelSprite()
	{
		if (infoPanelSprite == null)
		{
			infoPanelSprite = LoadBattleLobbySprite("Mahjong/Sprites/BattleLobbyUI/InfoPanel");
		}
		return infoPanelSprite;
	}

	private static Sprite LoadButtonSprite()
	{
		if (buttonSprite == null)
		{
			buttonSprite = LoadBattleLobbySprite("Mahjong/Sprites/BattleLobbyUI/Battlebutton");
		}
		return buttonSprite;
	}

	private static Sprite LoadDividerSprite()
	{
		if (dividerSprite == null)
		{
			dividerSprite = LoadBattleLobbySprite("Mahjong/Sprites/BattleLobbyUI/Divider");
		}
		return dividerSprite;
	}

	private static Sprite LoadBattleLobbySprite(string resourcePath)
	{
		Texture2D texture2D = Resources.Load<Texture2D>(resourcePath);
		if (texture2D != null)
		{
			return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		}
		Sprite sprite = Resources.Load<Sprite>(resourcePath);
		if (sprite != null)
		{
			return sprite;
		}
		Sprite[] array = Resources.LoadAll<Sprite>(resourcePath);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		return array[0];
	}

	private void CreateInfoRow(string message)
	{
		ClearGameButtons();
		Button button = CreateButton(gamesRoot, "InfoRow", message, Vector2.zero, new Vector2(900f, 82f));
		button.interactable = false;
		gameButtons.Add(button);
	}

	private void ClearGameButtons()
	{
		for (int i = 0; i < gameButtons.Count; i++)
		{
			if (gameButtons[i] != null)
			{
				UnityEngine.Object.Destroy(gameButtons[i].gameObject);
			}
		}
		gameButtons.Clear();
	}

	private static string LocalizeRankTier(string tier)
	{
		if (string.IsNullOrWhiteSpace(tier))
		{
			return GameLocalization.Text("battle.rank.unranked");
		}
		string text = tier.Trim().ToLowerInvariant();
		if (text.Contains("master"))
		{
			return GameLocalization.Text("battle.rank.master");
		}
		if (text.Contains("platinum"))
		{
			return GameLocalization.Text("battle.rank.platinum");
		}
		if (text.Contains("gold"))
		{
			return GameLocalization.Text("battle.rank.gold");
		}
		if (text.Contains("silver"))
		{
			return GameLocalization.Text("battle.rank.silver");
		}
		if (text.Contains("bronze"))
		{
			return GameLocalization.Text("battle.rank.bronze");
		}
		if (text.Contains("unranked"))
		{
			return GameLocalization.Text("battle.rank.unranked");
		}
		return tier.Trim();
	}

	private static string LocalizeNetworkStatus(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return GameLocalization.Text("battle.wifi.choose");
		}
		string text = value.Trim();
		if (text.Contains("Host", StringComparison.OrdinalIgnoreCase) || text.Contains("Search", StringComparison.OrdinalIgnoreCase))
		{
			return GameLocalization.Text("battle.wifi.choose");
		}
		return text;
	}
}
}
