using System.Collections;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class OnlineRankedBattleLobbyUI : MonoBehaviour
{
	private const string RootName = "OnlineRankedBattleLobbyOverlay";

	[SerializeField]
	[Min(1f)]
	private float maxOnlineSearchSeconds = 30f;

	[SerializeField]
	[Min(1f)]
	private float botFallbackSeconds = 45f;

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

	private bool waitingForRankedMatch;

	private bool launching;

	public static OnlineRankedBattleLobbyUI Show(string battleSceneName)
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			return null;
		}
		OnlineRankedBattleLobbyUI onlineRankedBattleLobbyUI = Object.FindAnyObjectByType<OnlineRankedBattleLobbyUI>(FindObjectsInactive.Include);
		if (onlineRankedBattleLobbyUI != null)
		{
			onlineRankedBattleLobbyUI.Configure(battleSceneName);
			onlineRankedBattleLobbyUI.Open();
			return onlineRankedBattleLobbyUI;
		}
		OnlineRankedBattleLobbyUI onlineRankedBattleLobbyUI2 = new GameObject("OnlineRankedBattleLobbyUI").AddComponent<OnlineRankedBattleLobbyUI>();
		onlineRankedBattleLobbyUI2.Configure(battleSceneName);
		onlineRankedBattleLobbyUI2.Open();
		return onlineRankedBattleLobbyUI2;
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
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RankedMatch);
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
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.RankedMatch);
		if (root != null)
		{
			root.SetActive(value: true);
			root.transform.SetAsLastSibling();
		}
		ApplyPlayerProfile();
		ApplyOpponentPlaceholder();
		StartRankedSearch();
	}

	private void Close()
	{
		if (!launching && RankedBattleService.HasPendingMatch())
		{
			RankedBattleService.CancelPendingMatch(refundEntryFee: true);
		}
		waitingForRankedMatch = false;
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RankedMatch);
		if (searchRoutine != null)
		{
			StopCoroutine(searchRoutine);
			searchRoutine = null;
		}
		OnlineRankedBattleNetwork.I?.CancelRankedSearch();
		if (root != null)
		{
			root.SetActive(value: false);
		}
	}

	private void StartRankedSearch()
	{
		if (searchRoutine != null)
		{
			StopCoroutine(searchRoutine);
		}
		RankedPendingMatch pendingMatch = RankedBattleService.GetPendingMatch();
		if (pendingMatch == null || !pendingMatch.Active)
		{
			waitingForRankedMatch = false;
			launching = false;
			if (titleText != null)
			{
				titleText.text = GameLocalization.Text("battle.ranked.title");
			}
			if (statusText != null)
			{
				statusText.text = GameLocalization.Text("battle.ranked.choose_league_first");
			}
			if (cancelButton != null)
			{
				cancelButton.interactable = true;
			}
			return;
		}
		waitingForRankedMatch = true;
		launching = false;
		if (titleText != null)
		{
			titleText.text = GameLocalization.Format("battle.league.confirm_title", LocalizeLeagueName(RankedBattleService.GetLeague(pendingMatch.LeagueId)));
		}
		if (statusText != null)
		{
			statusText.text = GameLocalization.Format("battle.ranked.searching_entry", pendingMatch.EntryFeeOzTile);
		}
		if (cancelButton != null)
		{
			cancelButton.interactable = true;
		}
		ApplyOpponentPlaceholder();
		MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
		OnlineRankedBattleNetwork.EnsureInstance().StartRankedSearch();
		searchRoutine = StartCoroutine(RankedSearchFallbackRoutine());
	}

	private IEnumerator RankedSearchFallbackRoutine()
	{
		float elapsed = 0f;
		float fallbackAt = Mathf.Clamp(botFallbackSeconds, 1f, Mathf.Max(1f, maxOnlineSearchSeconds));
		float maxSearch = Mathf.Max(fallbackAt, maxOnlineSearchSeconds);
		while (elapsed < maxSearch && waitingForRankedMatch && !launching)
		{
			float f = Mathf.Max(0f, fallbackAt - elapsed);
			float f2 = Mathf.Max(0f, maxSearch - elapsed);
			if (statusText != null)
			{
				if (elapsed < fallbackAt)
				{
					statusText.text = GameLocalization.Format("battle.ranked.searching_seconds", Mathf.CeilToInt(f));
				}
				else
				{
					statusText.text = GameLocalization.Format("battle.ranked.extending_seconds", Mathf.CeilToInt(f2));
				}
			}
			yield return null;
			elapsed += Time.unscaledDeltaTime;
			if (elapsed >= fallbackAt && waitingForRankedMatch && !launching)
			{
				break;
			}
		}
		searchRoutine = null;
		if (waitingForRankedMatch && !launching)
		{
			OnlineRankedBattleNetwork.I?.CancelRankedSearch();
			MahjongBattleOpponentData opponent = CreateRankedBotOpponent();
			yield return LaunchFoundMatch(opponent, 0);
		}
	}

	private void HandleMatchFound(OnlineRankedBattleNetwork.RankedMatchInfo match)
	{
		if (waitingForRankedMatch && !launching && match != null && match.opponent != null)
		{
			if (searchRoutine != null)
			{
				StopCoroutine(searchRoutine);
				searchRoutine = null;
			}
			MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
			{
				Id = (string.IsNullOrWhiteSpace(match.opponent.id) ? "ranked_online_peer" : match.opponent.id),
				DisplayName = (string.IsNullOrWhiteSpace(match.opponent.displayName) ? GameLocalization.Text("battle.ranked.online_player") : match.opponent.displayName),
				AllianceTag = match.opponent.allianceTag,
				AllianceLevel = Mathf.Max(0, match.opponent.allianceLevel),
				RankTier = (string.IsNullOrWhiteSpace(match.opponent.rankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(match.opponent.rankTier)),
				RankPoints = Mathf.Max(0, match.opponent.rankPoints),
				Level = Mathf.Max(1, 1 + Mathf.Max(0, match.opponent.rankPoints) / 100),
				AvatarId = Mathf.Max(0, match.opponent.avatarId),
				Gender = MahjongBattleOpponentData.ParseGender(match.opponent.gender),
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
			RankedBattleService.CancelPendingMatch(refundEntryFee: true);
			OnlineRankedBattleNetwork.I?.CancelRankedSearch();
			yield break;
		}
		waitingForRankedMatch = false;
		launching = true;
		if (cancelButton != null)
		{
			cancelButton.interactable = false;
		}
		MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
		RankedPendingMatch pendingMatch = RankedBattleService.GetPendingMatch();
		if (pendingMatch == null || !pendingMatch.Active)
		{
			waitingForRankedMatch = false;
			launching = false;
			if (statusText != null)
			{
				statusText.text = GameLocalization.Text("battle.ranked.entry_expired");
			}
			if (cancelButton != null)
			{
				cancelButton.interactable = true;
			}
			yield break;
		}
		RankedBattleService.MarkPendingMatchStarted();
		MahjongSession.StartBattle(opponent, pendingMatch.EntryFeeOzTile, matchSeed, MahjongBattleSource.Ranked);
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
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RankedMatch);
		SceneManager.LoadScene(battleGameSceneName);
	}

	private MahjongBattleOpponentData CreateRankedBotOpponent()
	{
		MahjongBattleBotService mahjongBattleBotService = MahjongBattleBotService.I;
		if (mahjongBattleBotService == null)
		{
			mahjongBattleBotService = new GameObject("MahjongBattleBotService").AddComponent<MahjongBattleBotService>();
		}
		return mahjongBattleBotService.CreateOpponent(MahjongBattleLobbyMode.RankedMatch, ResolvePlayerBattleRankPoints());
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
		onlineRankedBattleNetwork.MatchFound -= HandleMatchFound;
		onlineRankedBattleNetwork.MatchFound += HandleMatchFound;
	}

	private void UnbindNetwork()
	{
		if (!(OnlineRankedBattleNetwork.I == null))
		{
			OnlineRankedBattleNetwork.I.MatchFound -= HandleMatchFound;
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
			opponentInfoText.text = GameLocalization.Text("battle.ranked.slot");
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
				string text = (string.IsNullOrWhiteSpace(opponent.StatusLine) ? GameLocalization.Text("battle.ranked.ready") : opponent.StatusLine.Trim());
				opponentInfoText.text = GameLocalization.Format("battle.common.opponent_line", string.IsNullOrWhiteSpace(opponent.RankTier) ? GameLocalization.Text("battle.rank.unranked") : LocalizeRankTier(opponent.RankTier), Mathf.Max(0, opponent.RankPoints), Mathf.Max(0, opponent.Wins), Mathf.Max(0, opponent.Losses), text);
			}
		}
	}

	private void BuildUi()
	{
		if (!(root != null))
		{
			Canvas orCreatePopupCanvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
			root = new GameObject("OnlineRankedBattleLobbyOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			root.transform.SetParent(orCreatePopupCanvas.transform, worldPositionStays: false);
			RectTransform component = root.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = root.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.48f);
			component2.raycastTarget = true;
			GameObject gameObject = new GameObject("OnlineRankedBattlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(root.transform, worldPositionStays: false);
			RectTransform component3 = gameObject.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0.5f, 0.5f);
			component3.anchorMax = new Vector2(0.5f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			component3.anchoredPosition = new Vector2(0f, 18f);
			component3.sizeDelta = new Vector2(1540f, 760f);
			FitPanelInsideCanvas(component3, orCreatePopupCanvas, 44f);
			BattlePopupStyle.ApplyWindow(gameObject.GetComponent<Image>());
			titleText = CreateText(gameObject.transform, "Title", GameLocalization.Text("battle.ranked.title"), new Vector2(0f, 276f), new Vector2(1040f, 76f), 56f);
			statusText = CreateText(gameObject.transform, "Status", GameLocalization.Text("battle.random.searching"), new Vector2(0f, 214f), new Vector2(980f, 48f), 34f);
			Transform parent = CreateProfileCard(gameObject.transform, "PlayerCard", new Vector2(-420f, 8f));
			playerNameText = CreateText(parent, "Name", GameLocalization.Text("battle.common.player"), new Vector2(0f, 102f), new Vector2(430f, 56f), 38f);
			playerInfoText = CreateText(parent, "Info", string.Empty, new Vector2(0f, -16f), new Vector2(430f, 184f), 29f);
			Transform parent2 = CreateProfileCard(gameObject.transform, "OpponentCard", new Vector2(420f, 8f));
			opponentNameText = CreateText(parent2, "Name", GameLocalization.Text("battle.common.searching"), new Vector2(0f, 102f), new Vector2(430f, 56f), 38f);
			opponentInfoText = CreateText(parent2, "Info", GameLocalization.Text("battle.ranked.slot"), new Vector2(0f, -16f), new Vector2(430f, 184f), 29f);
			CreateText(gameObject.transform, "Versus", "VS", new Vector2(0f, 8f), new Vector2(150f, 72f), 48f);
			cancelButton = CreateButton(gameObject.transform, "CancelButton", GameLocalization.Text("battle.common.cancel"), new Vector2(0f, -280f), new Vector2(440f, 92f));
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
		component.sizeDelta = new Vector2(520f, 340f);
		BattlePopupStyle.ApplyFront(obj.GetComponent<Image>());
		return obj.transform;
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

	private static string LocalizeLeagueName(RankedLeagueConfig config)
	{
		if (config != null)
		{
			return LocalizeRankTier(config.DisplayName);
		}
		return GameLocalization.Text("battle.rank.bronze");
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
