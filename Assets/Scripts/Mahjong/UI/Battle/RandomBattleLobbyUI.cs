using System.Collections;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class RandomBattleLobbyUI : MonoBehaviour
{
	private const string RootName = "RandomBattleLobbyOverlay";

	private const string WindowSpritePath = "Mahjong/Sprites/BattleLobbyUI/TournamentWindow";

	private const string InfoPanelSpritePath = "Mahjong/Sprites/BattleLobbyUI/InfoPanel";

	private const string ButtonSpritePath = "Mahjong/Sprites/BattleLobbyUI/Battlebutton";

	private const string DividerSpritePath = "Mahjong/Sprites/BattleLobbyUI/Divider";

	private static readonly Vector2 FullscreenPanelSize = new Vector2(2140f, 980f);

	[SerializeField]
	[Min(0.5f)]
	private float onlineSearchSeconds = 10f;

	[SerializeField]
	[Min(0.1f)]
	private float foundRevealSeconds = 1.4f;

	private string battleGameSceneName = "GameMahjongBattle";

	private GameObject root;

	private TMP_Text titleText;

	private TMP_Text statusText;

	private TMP_Text playerNameText;

	private TMP_Text playerInfoText;

	private TMP_Text opponentNameText;

	private TMP_Text opponentInfoText;

	private Button cancelButton;

	private Coroutine searchRoutine;

	private bool waitingForRandomMatch;

	private bool launching;

	private static Sprite windowSprite;

	private static Sprite infoPanelSprite;

	private static Sprite buttonSprite;

	private static Sprite dividerSprite;

	public static RandomBattleLobbyUI Show(string battleSceneName)
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			return null;
		}
		RandomBattleLobbyUI randomBattleLobbyUI = Object.FindAnyObjectByType<RandomBattleLobbyUI>(FindObjectsInactive.Include);
		if (randomBattleLobbyUI != null)
		{
			randomBattleLobbyUI.Configure(battleSceneName);
			randomBattleLobbyUI.Open();
			return randomBattleLobbyUI;
		}
		RandomBattleLobbyUI randomBattleLobbyUI2 = new GameObject("RandomBattleLobbyUI").AddComponent<RandomBattleLobbyUI>();
		randomBattleLobbyUI2.Configure(battleSceneName);
		randomBattleLobbyUI2.Open();
		return randomBattleLobbyUI2;
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
	}

	private void OnDestroy()
	{
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RandomMatch);
		UnbindNetwork();
		if (cancelButton != null)
		{
			cancelButton.onClick.RemoveListener(Close);
		}
	}

	public void Open()
	{
		BuildUi();
		BindNetwork();
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.RandomMatch);
		if (root != null)
		{
			root.SetActive(value: true);
			root.transform.SetAsLastSibling();
		}
		ApplyPlayerProfile();
		ApplyOpponentPlaceholder();
		StartRandomSearch();
	}

	private void Close()
	{
		waitingForRandomMatch = false;
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RandomMatch);
		if (searchRoutine != null)
		{
			StopCoroutine(searchRoutine);
			searchRoutine = null;
		}
		OnlineRankedBattleNetwork.I?.CancelRandomSearch();
		if (root != null)
		{
			root.SetActive(value: false);
		}
	}

	private void StartRandomSearch()
	{
		if (!launching)
		{
			if (searchRoutine != null)
			{
				StopCoroutine(searchRoutine);
			}
			waitingForRandomMatch = true;
			launching = false;
			titleText.text = GameLocalization.Text("battle.random.title");
			statusText.text = GameLocalization.Text("battle.random.searching");
			if (cancelButton != null)
			{
				cancelButton.interactable = true;
			}
			ApplyOpponentPlaceholder();
			OnlineRankedBattleNetwork.EnsureInstance().StartRandomSearch();
			searchRoutine = StartCoroutine(RandomSearchTimeoutRoutine());
		}
	}

	private IEnumerator RandomSearchTimeoutRoutine()
	{
		float remaining = Mathf.Max(0.5f, onlineSearchSeconds);
		while (remaining > 0f && waitingForRandomMatch)
		{
			if (statusText != null)
			{
				statusText.text = GameLocalization.Format("battle.random.searching_seconds", Mathf.CeilToInt(remaining));
			}
			yield return null;
			remaining -= Time.unscaledDeltaTime;
		}
		searchRoutine = null;
		if (waitingForRandomMatch && !launching)
		{
			OnlineRankedBattleNetwork.I?.CancelRandomSearch();
			MahjongBattleOpponentData opponent = CreateBotOpponent();
			yield return LaunchFoundMatch(opponent, 0);
		}
	}

	private void HandleRandomMatchFound(OnlineRankedBattleNetwork.RankedMatchInfo match)
	{
		if (waitingForRandomMatch && !launching && match != null && match.opponent != null)
		{
			if (searchRoutine != null)
			{
				StopCoroutine(searchRoutine);
				searchRoutine = null;
			}
			MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
			{
				Id = (string.IsNullOrWhiteSpace(match.opponent.id) ? "random_online_peer" : match.opponent.id),
				DisplayName = (string.IsNullOrWhiteSpace(match.opponent.displayName) ? GameLocalization.Text("battle.random.online_player") : match.opponent.displayName),
				AllianceTag = match.opponent.allianceTag,
				AllianceLevel = Mathf.Max(0, match.opponent.allianceLevel),
				AvatarId = Mathf.Max(0, match.opponent.avatarId),
				Gender = MahjongBattleOpponentData.ParseGender(match.opponent.gender),
				RankTier = (string.IsNullOrWhiteSpace(match.opponent.rankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(match.opponent.rankTier)),
				RankPoints = Mathf.Max(0, match.opponent.rankPoints),
				Level = Mathf.Max(1, 1 + Mathf.Max(0, match.opponent.rankPoints) / 100),
				CharacterId = (string.IsNullOrWhiteSpace(match.opponent.characterId) ? string.Empty : match.opponent.characterId.Trim()),
				IsBot = false,
				Loadout = match.opponent.loadout?.Clone()
			};
			StartCoroutine(LaunchFoundMatch(opponent, Mathf.Max(1, match.seed)));
		}
	}

	private IEnumerator LaunchFoundMatch(MahjongBattleOpponentData opponent, int matchSeed)
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			OnlineRankedBattleNetwork.I?.CancelRandomSearch();
			yield break;
		}
		waitingForRandomMatch = false;
		launching = true;
		if (cancelButton != null)
		{
			cancelButton.interactable = false;
		}
		if (!EnergyService.TrySpendForMatch())
		{
			launching = false;
			if (cancelButton != null)
			{
				cancelButton.interactable = true;
			}
			if (statusText != null)
			{
				statusText.text = GameLocalization.Format("battle.energy.not_enough", 10);
			}
			OnlineRankedBattleNetwork.I?.CancelRandomSearch();
			yield break;
		}
		MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RandomMatch);
		MahjongSession.StartBattle(opponent, 0, matchSeed, MahjongBattleSource.Random);
		ApplyOpponentProfile(opponent);
		if (titleText != null)
		{
			titleText.text = GameLocalization.Text("battle.random.player_found");
		}
		if (statusText != null)
		{
			statusText.text = GameLocalization.Text("battle.random.starting");
		}
		yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, foundRevealSeconds));
		UnbindNetwork();
		if (root != null)
		{
			root.SetActive(value: false);
		}
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RandomMatch);
		SceneManager.LoadScene(battleGameSceneName);
	}

	private MahjongBattleOpponentData CreateBotOpponent()
	{
		MahjongBattleBotService mahjongBattleBotService = MahjongBattleBotService.I;
		if (mahjongBattleBotService == null)
		{
			mahjongBattleBotService = new GameObject("MahjongBattleBotService").AddComponent<MahjongBattleBotService>();
		}
		return mahjongBattleBotService.CreateOpponent(MahjongBattleLobbyMode.RandomMatch, ResolvePlayerBattleRankPoints());
	}

	private int ResolvePlayerBattleRankPoints()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		if (playerProfile == null)
		{
			return 0;
		}
		playerProfile.EnsureData();
		if (playerProfile.Mahjong == null || playerProfile.Mahjong.Battle == null)
		{
			return 0;
		}
		return Mathf.Max(0, playerProfile.Mahjong.Battle.RankPoints);
	}

	private void BindNetwork()
	{
		OnlineRankedBattleNetwork onlineRankedBattleNetwork = OnlineRankedBattleNetwork.EnsureInstance();
		onlineRankedBattleNetwork.MatchFound -= HandleRandomMatchFound;
		onlineRankedBattleNetwork.MatchFound += HandleRandomMatchFound;
	}

	private void UnbindNetwork()
	{
		if (!(OnlineRankedBattleNetwork.I == null))
		{
			OnlineRankedBattleNetwork.I.MatchFound -= HandleRandomMatchFound;
		}
	}

	private void ApplyPlayerProfile()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		string text = GameLocalization.Text("battle.common.player");
		string text2 = GameLocalization.Text("battle.rank.unranked");
		int num = 0;
		int num2 = 1;
		int num3 = 0;
		int num4 = 0;
		if (playerProfile != null)
		{
			playerProfile.EnsureData();
			if (!string.IsNullOrWhiteSpace(playerProfile.DisplayName))
			{
				text = playerProfile.DisplayName.Trim();
			}
			text = AllianceIdentityFormatter.FormatName(text, AllianceIdentityFormatter.ResolveOwnTag(playerProfile));
			num2 = Mathf.Max(1, playerProfile.AccountLevel);
			MahjongBattleData mahjongBattleData = ((playerProfile.Mahjong != null) ? playerProfile.Mahjong.Battle : null);
			if (mahjongBattleData != null)
			{
				text2 = (string.IsNullOrWhiteSpace(mahjongBattleData.RankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(mahjongBattleData.RankTier));
				num = Mathf.Max(0, mahjongBattleData.RankPoints);
				num3 = Mathf.Max(0, mahjongBattleData.Wins);
				num4 = Mathf.Max(0, mahjongBattleData.Losses);
			}
		}
		if (playerNameText != null)
		{
			playerNameText.text = text;
		}
		if (playerInfoText != null)
		{
			playerInfoText.text = GameLocalization.Format("battle.common.profile_line", GameLocalization.Format("battle.common.level", num2), text2, num, num3, num4);
		}
	}

	private void ApplyOpponentPlaceholder()
	{
		if (opponentNameText != null)
		{
			opponentNameText.text = GameLocalization.Text("battle.common.searching");
		}
		if (opponentInfoText != null)
		{
			opponentInfoText.text = GameLocalization.Text("battle.random.waiting");
		}
	}

	private void ApplyOpponentProfile(MahjongBattleOpponentData opponent)
	{
		if (opponent != null)
		{
			if (opponentNameText != null)
			{
				opponentNameText.text = AllianceIdentityFormatter.FormatName(string.IsNullOrWhiteSpace(opponent.DisplayName) ? GameLocalization.Text("battle.common.opponent") : opponent.DisplayName, opponent.AllianceTag);
			}
			if (opponentInfoText != null)
			{
				string text = (string.IsNullOrWhiteSpace(opponent.StatusLine) ? GameLocalization.Text("battle.random.ready") : opponent.StatusLine.Trim());
				opponentInfoText.text = GameLocalization.Format("battle.common.opponent_line", string.IsNullOrWhiteSpace(opponent.RankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(opponent.RankTier), Mathf.Max(0, opponent.RankPoints), Mathf.Max(0, opponent.Wins), Mathf.Max(0, opponent.Losses), text);
			}
		}
	}

	private void BuildUi()
	{
		if (!(root != null))
		{
			Canvas orCreatePopupCanvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
			root = new GameObject("RandomBattleLobbyOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			root.transform.SetParent(orCreatePopupCanvas.transform, worldPositionStays: false);
			RectTransform component = root.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = root.GetComponent<Image>();
			component2.color = Color.black;
			component2.raycastTarget = true;
			GameObject gameObject = new GameObject("RandomBattlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(root.transform, worldPositionStays: false);
			RectTransform component3 = gameObject.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0.5f, 0.5f);
			component3.anchorMax = new Vector2(0.5f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			component3.anchoredPosition = Vector2.zero;
			component3.sizeDelta = FullscreenPanelSize;
			FitPanelInsideCanvas(component3, orCreatePopupCanvas, 30f);
			BattlePopupStyle.ApplyWindow(gameObject.GetComponent<Image>());
			titleText = CreateText(gameObject.transform, "Title", GameLocalization.Text("battle.random.title"), new Vector2(0f, 330f), new Vector2(1220f, 90f), 66f);
			statusText = CreateText(gameObject.transform, "Status", GameLocalization.Text("battle.random.searching"), new Vector2(0f, 258f), new Vector2(1180f, 56f), 40f);
			CreateDivider(gameObject.transform, "TopDivider", new Vector2(0f, 194f), new Vector2(1420f, 8f));
			Transform parent = CreateProfileCard(gameObject.transform, "PlayerCard", new Vector2(-580f, -26f));
			playerNameText = CreateText(parent, "Name", GameLocalization.Text("battle.common.player"), new Vector2(0f, 68f), new Vector2(590f, 56f), 40f);
			playerInfoText = CreateText(parent, "Info", string.Empty, new Vector2(0f, -36f), new Vector2(590f, 122f), 30f);
			Transform parent2 = CreateProfileCard(gameObject.transform, "OpponentCard", new Vector2(580f, -26f));
			opponentNameText = CreateText(parent2, "Name", GameLocalization.Text("battle.common.searching"), new Vector2(0f, 68f), new Vector2(590f, 56f), 40f);
			opponentInfoText = CreateText(parent2, "Info", GameLocalization.Text("battle.random.waiting"), new Vector2(0f, -36f), new Vector2(590f, 122f), 30f);
			CreateText(gameObject.transform, "Versus", "VS", new Vector2(0f, -26f), new Vector2(180f, 82f), 58f);
			CreateDivider(gameObject.transform, "BottomDivider", new Vector2(0f, -264f), new Vector2(1340f, 8f));
			cancelButton = CreateButton(gameObject.transform, "CancelButton", GameLocalization.Text("battle.common.cancel"), new Vector2(0f, -362f), new Vector2(560f, 100f));
			cancelButton.onClick.AddListener(Close);
			root.SetActive(value: false);
		}
	}

	private Transform CreateProfileCard(Transform parent, string objectName, Vector2 position)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = new Vector2(690f, 220f);
		ApplySimpleSprite(obj.GetComponent<Image>(), LoadInfoPanelSprite(), raycastTarget: false);
		return obj.transform;
	}

	private void CreateDivider(Transform parent, string objectName, Vector2 position, Vector2 size)
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
		component2.color = new Color(0.16f, 0.23f, 0.22f, 0.96f);
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
}
}
