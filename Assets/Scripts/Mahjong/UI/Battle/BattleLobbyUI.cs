using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class BattleLobbyUI : MonoBehaviour
{
	private sealed class BattleTileInventorySlotInteraction : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private const float TileDragHoldSeconds = 0.18f;

		private const float TileClickMoveTolerancePixels = 18f;

		private const float TileScrollVerticalBias = 1.15f;

		private BattleLobbyUI owner;

		private string tileId;

		private int upgradeLevel;

		private bool activePocket;

		private RectTransform rect;

		private ScrollRect parentScrollRect;

		private Vector2 pointerDownPosition;

		private Vector2 originalAnchoredPosition;

		private Transform originalParent;

		private int originalSiblingIndex;

		private CanvasGroup canvasGroup;

		private GameObject dragGhost;

		private RectTransform dragGhostRect;

		private float pointerDownTime;

		private bool dragging;

		private bool forwardingScrollDrag;

		private bool suppressNextClick;

		public void Configure(BattleLobbyUI newOwner, string newTileId, bool newActivePocket, int newUpgradeLevel)
		{
			owner = newOwner;
			tileId = newTileId;
			activePocket = newActivePocket;
			upgradeLevel = Mathf.Max(0, newUpgradeLevel);
			rect = base.transform as RectTransform;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			pointerDownTime = Time.unscaledTime;
			pointerDownPosition = eventData?.position ?? Vector2.zero;
			forwardingScrollDrag = false;
			parentScrollRect = GetComponentInParent<ScrollRect>();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData != null && HasPointerMovedForScroll(eventData.position))
			{
				suppressNextClick = true;
			}
			forwardingScrollDrag = false;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (suppressNextClick)
			{
				suppressNextClick = false;
			}
			else if (!dragging && !(owner == null) && (eventData == null || !HasPointerMovedForScroll(eventData.position)))
			{
				owner.OnBattleTileSlotClick(tileId, activePocket, upgradeLevel, eventData?.clickCount ?? 1);
			}
		}

		private bool HasPointerMovedForScroll(Vector2 currentPosition)
		{
			return (currentPosition - pointerDownPosition).sqrMagnitude > 324f;
		}

		private bool IsMostlyVerticalPointerMove(PointerEventData eventData)
		{
			if (eventData == null)
			{
				return false;
			}
			Vector2 vector = eventData.position - pointerDownPosition;
			return Mathf.Abs(vector.y) > Mathf.Abs(vector.x) * 1.15f;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (owner == null || rect == null)
			{
				return;
			}
			if (parentScrollRect != null && Time.unscaledTime - pointerDownTime < 0.18f && IsMostlyVerticalPointerMove(eventData))
			{
				forwardingScrollDrag = true;
				ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
				return;
			}
			dragging = true;
			owner.OnBattleTileSlotDragStarted();
			originalAnchoredPosition = rect.anchoredPosition;
			originalParent = base.transform.parent;
			originalSiblingIndex = base.transform.GetSiblingIndex();
			Transform parent = ((owner.battleTileInventoryRoot != null) ? owner.battleTileInventoryRoot.transform : base.transform.parent);
			dragGhost = new GameObject(base.gameObject.name + "_StoneDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
			dragGhost.transform.SetParent(parent, worldPositionStays: false);
			dragGhostRect = dragGhost.transform as RectTransform;
			Image image = ((base.transform.Find("Face") != null) ? base.transform.Find("Face").GetComponent<Image>() : null);
			Image component = dragGhost.GetComponent<Image>();
			component.sprite = ((image != null) ? image.sprite : null);
			component.enabled = component.sprite != null;
			component.color = Color.white;
			component.preserveAspect = true;
			component.raycastTarget = false;
			if (dragGhostRect != null)
			{
				dragGhostRect.position = rect.position;
				dragGhostRect.sizeDelta = (activePocket ? (rect.sizeDelta * 0.86f) : new Vector2(132f, 168f));
				dragGhost.transform.SetAsLastSibling();
				BattleTileUpgradeVisual.Apply(dragGhost.transform, Vector2.zero, dragGhostRect.sizeDelta, upgradeLevel);
			}
			CanvasGroup component2 = dragGhost.GetComponent<CanvasGroup>();
			component2.blocksRaycasts = false;
			component2.alpha = 0.86f;
			canvasGroup = GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
			}
			canvasGroup.blocksRaycasts = true;
			canvasGroup.alpha = 0.42f;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (forwardingScrollDrag)
			{
				if (parentScrollRect != null)
				{
					ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
				}
			}
			else if (dragging && !(rect == null) && eventData != null && dragGhostRect != null)
			{
				dragGhostRect.position = eventData.position;
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (forwardingScrollDrag)
			{
				if (parentScrollRect != null)
				{
					ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
				}
				forwardingScrollDrag = false;
				suppressNextClick = true;
			}
			else
			{
				if (!dragging)
				{
					return;
				}
				dragging = false;
				if (canvasGroup != null)
				{
					canvasGroup.blocksRaycasts = true;
					canvasGroup.alpha = 1f;
				}
				if (dragGhost != null)
				{
					UnityEngine.Object.Destroy(dragGhost);
				}
				dragGhost = null;
				dragGhostRect = null;
				suppressNextClick = true;
				if ((!(owner != null) || eventData == null || !owner.TryDropBattleTile(tileId, activePocket, eventData.position, eventData.pressEventCamera)) && rect != null)
				{
					if (originalParent != null && base.transform.parent != originalParent)
					{
						base.transform.SetParent(originalParent, worldPositionStays: false);
					}
					rect.anchoredPosition = originalAnchoredPosition;
					int num = ((base.transform.parent != null) ? base.transform.parent.childCount : 0);
					base.transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, Mathf.Max(0, num - 1)));
				}
			}
		}
	}

	private static readonly Rect BattleLobbyMatchButtonSpriteRect = Rect.zero;

	private static readonly Rect BattleLobbyTopBarSpriteRect = new Rect(10f, 3f, 1432f, 343f);

	private static readonly Vector4 BattleLobbyMatchButtonLabelMargin = new Vector4(112f, 22f, 112f, 26f);

	private static readonly Vector4 BattleLobbyUtilityButtonLabelMargin = new Vector4(78f, 14f, 78f, 16f);

	private static readonly Vector2 ForcedWeeklyRewardButtonPosition = new Vector2(-230f, -472f);

	private static readonly Vector2 ForcedCharacterButtonPosition = new Vector2(-230f, -472f);

	private static readonly Vector2 ForcedCharacterButtonSize = new Vector2(390f, 100f);

	private static readonly Vector2 ForcedReturnButtonPosition = new Vector2(-690f, -472f);

	private static readonly Vector2 ForcedInventoryButtonPosition = new Vector2(230f, -472f);

	private static readonly Vector2 ForcedShopButtonPosition = new Vector2(690f, -472f);

	private static readonly Vector2 ForcedBottomActionButtonSize = new Vector2(390f, 100f);

	private const int BattleLobbyHudSortingOrder = 30000;

	private const int BattleLobbyOverlaySortingOrder = 30020;

	private const string BattleLobbyRuntimeCanvasName = "BattleLobbyRuntimeHudCanvas";

	private const float BattleLobbyStatsPanelBottomExtension = 18f;

	private const float BattleLobbyCurrencyRowY = 358f;

	private const float BattleLobbyCurrencyDividerY = 316f;

	private const float BattleLobbyNicknameDividerY = 244f;

	private const float BattleLobbyBattleRecordDividerY = 190f;

	private const float BattleLobbyRankDividerY = 126f;

	private const string WeeklyRewardSpriteSheetResourcePath = "Mahjong/Sprites/Rewards/Rewards";

	private const string WeeklyRewardPanelWindowResourcePath = "Mahjong/Sprites/Rewards/RewardWindowWeekly";

	private const string WeeklyRewardWindowResourcePath = "Mahjong/Sprites/Rewards/WeeklyWindow";

	private const string BattleLobbyDownBarPanelResourcePath = "Mahjong/Sprites/BattleLobby/downbarpanel";

	private const string BattleLobbyEnergyIconResourcePath = "Mahjong/Sprites/BattleLobby/EnergyIconTopBar";

	private const string BattleLobbyAmetistIconResourcePath = "Mahjong/Sprites/Money/OzAmetist";

	private const string BattleLobbyGoldIconResourcePath = "Mahjong/Sprites/Money/OzAlt\u0131n";

	private const string BattleLobbyExpIconResourcePath = "Mahjong/Sprites/BattleLobby/ExpIconTopBar";

	private const string BattleLobbyRpIconResourcePath = "Mahjong/Sprites/BattleLobby/RPIconTopBar";

	private const string BattleLobbyOzTileIconResourcePath = "Mahjong/Sprites/BattleLobby/OzTileTopBar";

	private const string BattleLobbyProButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2";

	private const string BattleTileInventoryButtonName = "ButtonBattleTileInventory";

	private const string BattleTileInventoryRootName = "BattleTileInventoryWindow";

	private const string DailyHeroBonusSeenDateKey = "MahjongGame.Battle.DailyHeroBonusSeenDate";

	private static readonly Rect BattleLobbyProButtonUsefulRect = Rect.zero;

	private static readonly Vector4 BattleLobbyProButtonBorder = new Vector4(150f, 78f, 150f, 78f);

	private static readonly bool RouteLocalWifiSlotToTournament = true;

	private const string BattleLobbyTopBarObjectName = "TopBar";

	private const string BattleLobbyTopBarGraphicPath = "Bar/Image";

	private const string BattleLobbyTopBarGraphicAlternatePath = "Bar/UI";

	private const string BattleLobbyTopTabsRootName = "BattleLobbyTopTabsRoot";

	private static readonly Vector2 BattleLobbyDownBarPanelSize = new Vector2(2850f, 330f);

	private static readonly Vector2 BattleLobbyDownBarPanelPosition = new Vector2(0f, -165f);

	private static readonly Vector2 BattleLobbyTopMirrorBarPanelSize = new Vector2(2850f, 330f);

	private static readonly Vector2 BattleLobbyTopMirrorBarPanelPosition = new Vector2(0f, 0f);

	[Header("Scene Names")]
	[SerializeField]
	private string battleLobbySceneName = "LobbyMahjongBattle";

	[SerializeField]
	private string battleGameSceneName = "GameMahjongBattle";

	[SerializeField]
	private string mainLobbySceneName = "Main";

	[SerializeField]
	private string friendLobbySceneName = "";

	[SerializeField]
	private string localWifiSceneName = "";

	[SerializeField]
	private bool randomMatchUsesOnlineRanked;

	[Header("Random Match Button")]
	[SerializeField]
	private Button randomMatchButton;

	[SerializeField]
	private bool autoCreateRandomMatchButton = true;

	[SerializeField]
	private string randomMatchButtonText = "Random Match";

	[SerializeField]
	private Vector2 randomMatchButtonPosition = new Vector2(-360f, 0f);

	[SerializeField]
	private Vector2 randomMatchButtonSize = new Vector2(760f, 184f);

	[Header("Wi-Fi Battle Button")]
	[SerializeField]
	private Button localWifiBattleButton;

	[SerializeField]
	private bool autoCreateLocalWifiBattleButton = true;

	[SerializeField]
	private string localWifiBattleButtonText = "Wi-Fi Battle";

	[SerializeField]
	private Vector2 localWifiBattleButtonPosition = new Vector2(360f, 0f);

	[SerializeField]
	private Vector2 localWifiBattleButtonSize = new Vector2(760f, 184f);

	[Header("Ranked Online Button")]
	[SerializeField]
	private Button rankedBattleButton;

	[SerializeField]
	private bool autoCreateRankedBattleButton = true;

	[SerializeField]
	private string rankedBattleButtonText = "Ranked Match";

	[SerializeField]
	private Vector2 rankedBattleButtonPosition = new Vector2(0f, 170f);

	[SerializeField]
	private Vector2 rankedBattleButtonSize = new Vector2(760f, 184f);

	[Header("Duel Challenge Button")]
	[SerializeField]
	private Button duelChallengeButton;

	[SerializeField]
	private bool autoCreateDuelChallengeButton = true;

	[SerializeField]
	private string duelChallengeButtonText = "Duel Challenge";

	[SerializeField]
	private Vector2 duelChallengeButtonPosition = new Vector2(0f, -170f);

	[SerializeField]
	private Vector2 duelChallengeButtonSize = new Vector2(760f, 184f);

	[Header("Tournament Button")]
	[SerializeField]
	private Button tournamentButton;

	[SerializeField]
	private bool autoCreateTournamentButton = true;

	[SerializeField]
	private string tournamentButtonText = "Tournament";

	[SerializeField]
	private Vector2 tournamentButtonPosition = new Vector2(0f, -300f);

	[SerializeField]
	private Vector2 tournamentButtonSize = new Vector2(700f, 164f);

	[Header("Battle Lobby Visuals")]
	[SerializeField]
	private Sprite battleLobbyButtonSprite;

	[SerializeField]
	private Sprite battleLobbyTopBarSprite;

	[SerializeField]
	private TMP_FontAsset battleLobbyMainFont;

	[Header("Return Button")]
	[SerializeField]
	private Button returnToLobbyButton;

	[SerializeField]
	private bool autoCreateReturnButton = true;

	[SerializeField]
	private string returnButtonText = "Back";

	[SerializeField]
	private Vector2 returnButtonPosition = new Vector2(-690f, -472f);

	[SerializeField]
	private Vector2 returnButtonSize = new Vector2(430f, 112f);

	[Header("Battle Shop")]
	[SerializeField]
	private Button battleShopButton;

	[SerializeField]
	private bool autoCreateBattleShopButton = true;

	[SerializeField]
	private string battleShopButtonText = "Shop";

	[SerializeField]
	private Vector2 battleShopButtonPosition = new Vector2(690f, -472f);

	[SerializeField]
	private Vector2 battleShopButtonSize = new Vector2(430f, 112f);

	[SerializeField]
	[Min(1f)]
	private int shopEnergyAmount = 50;

	[SerializeField]
	[Min(1f)]
	private int shopEnergyAmetistPrice = 5;

	[SerializeField]
	[Min(1f)]
	private int shopDragonAmetistPrice = 300;

	[Header("Battle Tile Inventory")]
	[SerializeField]
	private Button battleTileInventoryButton;

	[SerializeField]
	private bool autoCreateBattleTileInventoryButton = true;

	[SerializeField]
	private string battleTileInventoryButtonText = "Bag";

	[SerializeField]
	private Vector2 battleTileInventoryButtonPosition = new Vector2(230f, -472f);

	[SerializeField]
	private Vector2 battleTileInventoryButtonSize = new Vector2(430f, 112f);

	[Header("Weekly Rewards")]
	[SerializeField]
	private Button weeklyRewardButton;

	[SerializeField]
	private bool autoCreateWeeklyRewardButton;

	[SerializeField]
	private string weeklyRewardButtonText = "Rewards";

	[SerializeField]
	private Vector2 weeklyRewardButtonPosition = new Vector2(-230f, -472f);

	[SerializeField]
	private Vector2 weeklyRewardButtonSize = new Vector2(430f, 112f);

	[Header("Daily Hero Bonus")]
	[SerializeField]
	private Button dailyHeroBonusButton;

	[SerializeField]
	private bool autoCreateDailyHeroBonusButton = true;

	[SerializeField]
	private string dailyHeroBonusButtonText = "Бонус дня";

	[Header("Battle Progress")]
	[SerializeField]
	private GameObject battleProgressRoot;

	[SerializeField]
	private TMP_Text battleLevelText;

	[SerializeField]
	private TMP_Text battleExpText;

	[SerializeField]
	private TMP_Text battleStatsText;

	[SerializeField]
	private TMP_Text energyText;

	[SerializeField]
	private TMP_Text energyHintText;

	[SerializeField]
	private Button energyAdButton;

	[SerializeField]
	private bool autoCreateBattleProgressUi = true;

	[SerializeField]
	private Vector2 battleProgressPosition = new Vector2(-42f, -42f);

	[SerializeField]
	private Vector2 battleProgressSize = new Vector2(440f, 278f);

	[Header("Character Selection")]
	[SerializeField]
	private GameObject characterCarouselRoot;

	[SerializeField]
	private Button openCharacterCarouselButton;

	[SerializeField]
	private string characterCarouselObjectName = "CharasterCarousel";

	[SerializeField]
	private string openCharacterButtonObjectName = "LobbyCharacterImage";

	[SerializeField]
	private bool closeCharacterCarouselOnEnter;

	[SerializeField]
	private bool autoBindOpenCharacterButton = true;

	[SerializeField]
	private bool createOpenCharacterButtonIfMissing;

	[SerializeField]
	private string openCharacterButtonText = "Character";

	[SerializeField]
	private Vector2 openCharacterButtonPosition = new Vector2(-230f, -472f);

	[SerializeField]
	private Vector2 openCharacterButtonSize = new Vector2(300f, 86f);

	[SerializeField]
	private bool openCharacterCarouselWhenNoCharacterSelected = true;

	[SerializeField]
	private int autoOpenCharacterCarouselDelayFrames = 3;

	[SerializeField]
	private float autoOpenCharacterCarouselMaxWaitSeconds = 0.5f;

	[Header("Debug")]
	[SerializeField]
	private bool debugLogs = true;

	private Coroutine autoOpenCharacterCarouselRoutine;

	private Coroutine restoreLobbyAfterCharacterCarouselRoutine;

	private Coroutine energyRefreshRoutine;

	private GameObject battleShopRoot;

	private GameObject downBarPanelRoot;

	private GameObject topMirrorStatsRoot;

	private TMP_Text battleShopBalanceText;

	private TMP_Text battleShopOzTileBalanceText;

	private TMP_Text battleShopAmetistBalanceText;

	private TMP_Text battleShopEnergyBalanceText;

	private TMP_Text battleShopStatusText;

	private GameObject battleTilePackResultRoot;

	private Coroutine battleTilePackResultRevealRoutine;

	private static BattleTilePackResult pendingBattleTilePackResult;

	private static string pendingBattleTilePackProfileKey = string.Empty;

	private Image battleLobbyRankIcon;

	private Image battleLobbyRpIcon;

	private Image battleLobbyExpIcon;

	private Image battleLobbyEnergyIcon;

	private Image battleLobbyAmetistIcon;

	private Image battleLobbyOzTileIcon;

	private Image battleLobbyGoldIcon;

	private Image topBarTooltipBackground;

	private TMP_Text topBarTooltipText;

	private Button shopEnergyTabButton;

	private Button shopCharactersTabButton;

	private Button shopSkinsTabButton;

	private Button shopBattleTilesTabButton;

	private Button shopBuyEnergyButton;

	private Button shopRewardedEnergyButton;

	private Button shopBuyDragonMaleButton;

	private Button shopBuyDragonFemaleButton;

	private Button shopAmetistSmallButton;

	private Button shopAmetistMediumButton;

	private Button shopAmetistBigButton;

	private Button shopAmetistLegendButton;

	private Button shopBattleTileDailyAdButton;

	private Button shopBattleTileMediumButton;

	private Button shopBattleTileHighButton;

	private Button shopBattleTileAmetistButton;

	private GameObject weeklyRewardRoot;

	private GameObject battleTileInventoryRoot;

	private GameObject tutorialGateRoot;

	private GameObject tournamentComingSoonRoot;

	private GameObject shopBattleTilesSection;

	private GameObject battleTileProfileRoot;

	private Transform activeTileInventoryContent;

	private Transform reserveTileInventoryContent;

	private Transform totemTileInventoryContent;

	private RectTransform activeTileInventoryPocketRect;

	private RectTransform reserveTileInventoryPocketRect;

	private RectTransform totemTileInventoryPocketRect;

	private TMP_Text battleTileInventoryStatusText;

	private TMP_Text battleTileInventoryActiveCountText;

	private TMP_Text battleTileInventoryReserveCountText;

	private TMP_Text battleTileInventoryHeroNameText;

	private TMP_Text battleTileInventoryHeroDescriptionText;

	private TMP_Text battleTileInventoryHeroStatsText;

	private TMP_Text battleTileInventoryHeroSkillText;

	private TMP_Text battleTileInventoryTotemCountText;

	private Image battleTileInventoryHeroPortraitImage;

	private Coroutine battleTileProfileClickRoutine;

	private GameObject dailyHeroBonusRoot;

	private GameObject dailyHeroBonusNotificationBadge;

	private Button dailyHeroBoostButton;

	private TMP_Text dailyHeroBoostStatusText;

	private Coroutine dailyHeroAttentionRoutine;

	private bool dailyHeroBoostAdRequestInProgress;

	private bool battleTilePackAdRequestInProgress;

	private TMP_Text weeklyRewardStatusText;

	private TMP_Text weeklyRewardTodayText;

	private TMP_Text weeklyRewardFreeButtonText;

	private TMP_Text weeklyRewardAdButtonText;

	private Button weeklyRewardFreeButton;

	private Button weeklyRewardAdButton;

	private Image[] weeklyRewardSlotImages;

	private Image[] weeklyRewardIconImages;

	private TMP_Text[] weeklyRewardSlotDayTexts;

	private TMP_Text[] weeklyRewardSlotStateTexts;

	private TMP_Text[] weeklyRewardSlotAmountTexts;

	private Image weeklyRewardFreePreviewIcon;

	private Image weeklyRewardAdPreviewIcon;

	private Coroutine weeklyRewardAdRefreshRoutine;

	private bool weeklyRewardAdRequestInProgress;

	private GameObject shopEnergySection;

	private GameObject topMirrorBarPanelRoot;

	private GameObject shopCharactersSection;

	private GameObject shopSkinsSection;

	private Sprite cachedBattleLobbyButtonSprite;

	private Sprite cachedBattleLobbyTopBarSprite;

	private static Sprite cachedBattleLobbyProButtonSprite;

	private static Sprite cachedDailyHeroBadgeSprite;

	private static Sprite cachedBattleLobbyEnergyIconSprite;

	private static Sprite cachedBattleLobbyAmetistIconSprite;

	private static Sprite cachedBattleLobbyGoldIconSprite;

	private static Sprite cachedBattleLobbyExpIconSprite;

	private static Sprite cachedBattleLobbyRpIconSprite;

	private static Sprite cachedBattleLobbyOzTileIconSprite;

	private static Sprite cachedBattleLobbyStatsWindowSprite;

	private static Sprite cachedBattleShopDividerSprite;

	private static Sprite cachedWeeklyRewardPanelWindowSprite;

	private static Sprite cachedWeeklyRewardWindowSprite;

	private static Sprite[] cachedWeeklyRewardSprites;

	private bool matchButtonsSuppressedBySettings;

	private bool battleCharacterSelectionSubscribed;

	private bool topBarTooltipVisible;

	private string topBarTooltipValue;

	private Vector2 topBarTooltipPosition;

	private Coroutine restoreLobbyLayoutRoutine;

	public static bool TryOpenBattleTileInventoryForTutorial()
	{
		BattleLobbyUI battleLobbyUI = UnityEngine.Object.FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include);
		if (battleLobbyUI == null)
		{
			return false;
		}
		battleLobbyUI.OnClickOpenBattleTileInventory();
		return true;
	}

	public static void CloseBattleTileInventoryForTutorial()
	{
		BattleLobbyUI battleLobbyUI = UnityEngine.Object.FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include);
		if (!(battleLobbyUI == null) && !(battleLobbyUI.battleTileInventoryRoot == null))
		{
			battleLobbyUI.battleTileInventoryRoot.SetActive(value: false);
			battleLobbyUI.SetMatchButtonsSuppressedBySettings(suppressed: false);
			BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Shop);
		}
	}

	private void Awake()
	{
		AutoResolveCharacterSelectionLinks();
		BindCharacterSelectionButton();
		EnsureAndBindLobbyButtonsIfNeeded();
		EnsureBattleProgressUi();
		RefreshBattleProgressUi();
		ApplyBattleLobbyVisuals();
		CloseCharacterCarousel();
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += HandleActiveSceneChanged;
		ProfileService.ProfileChanged += RefreshBattleProgressUi;
		ProfileService.ProfileChanged += RefreshBattleTileInventoryUi;
		ProfileService.ProfileChanged += RefreshBattleLobbyTopBarValues;
		AppSettings.OnLanguageChanged += OnLanguageChanged;
		EnergyService.EnergyChanged += RefreshBattleProgressUi;
		EnergyService.EnergyChanged += RefreshBattleLobbyTopBarValues;
		CurrencyService.CurrencyChanged += RefreshBattleShopUi;
		CurrencyService.CurrencyChanged += RefreshBattleLobbyTopBarValues;
		AllianceBootstrap.EnsureForCurrentScene();
		if (AllianceService.I != null)
		{
			AllianceService.I.AllianceChanged += RefreshBattleLobbyTopBarValues;
		}
		if (AllianceService.I != null && AllianceService.I.Current == null)
		{
			StartCoroutine(AllianceService.I.Refresh());
		}
		SubscribeBattleCharacterSelection();
		if (ShouldShowLobbyButtons())
		{
			BattleLobbyUiCoordinator.ResetForLobbyEntry();
			SettingsMenuUI.ForceCloseAllSettingsMenus();
			matchButtonsSuppressedBySettings = false;
			CleanupStaleBattleLobbyOverlays();
		}
		EnsureAndBindLobbyButtonsIfNeeded();
		RefreshBattleProgressUi();
		RefreshBattleShopUi();
		ApplyBattleLobbyVisuals();
		StartEnergyRefreshRoutine();
		CloseCharacterCarousel();
		if (ShouldShowLobbyButtons())
		{
			QueueAutoOpenCharacterCarouselIfNeeded();
		}
		if (ShouldShowLobbyButtons() && MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.TournamentMatch)
		{
			StartCoroutine(OpenTournamentOverlayAfterLobbyRestore());
		}
	}

	private void OnDisable()
	{
		StopRestoreLobbyLayoutRoutine();
		CloseBattleTilePackResult();
		SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
		UnsubscribeBattleCharacterSelection();
		ProfileService.ProfileChanged -= RefreshBattleProgressUi;
		ProfileService.ProfileChanged -= RefreshBattleTileInventoryUi;
		ProfileService.ProfileChanged -= RefreshBattleLobbyTopBarValues;
		AppSettings.OnLanguageChanged -= OnLanguageChanged;
		EnergyService.EnergyChanged -= RefreshBattleProgressUi;
		EnergyService.EnergyChanged -= RefreshBattleLobbyTopBarValues;
		CurrencyService.CurrencyChanged -= RefreshBattleShopUi;
		CurrencyService.CurrencyChanged -= RefreshBattleLobbyTopBarValues;
		if (AllianceService.I != null)
		{
			AllianceService.I.AllianceChanged -= RefreshBattleLobbyTopBarValues;
		}
		if (autoOpenCharacterCarouselRoutine != null)
		{
			StopCoroutine(autoOpenCharacterCarouselRoutine);
			autoOpenCharacterCarouselRoutine = null;
		}
		if (energyRefreshRoutine != null)
		{
			StopCoroutine(energyRefreshRoutine);
			energyRefreshRoutine = null;
		}
		StopDailyHeroAttentionRoutine();
	}

	private void LateUpdate()
	{
		if (ShouldShowLobbyButtons())
		{
			ApplyBattleLobbyTopTabButtonsLayout(GetBattleLobbyCanvasSize());
		}
	}

	private void Update()
	{
		if (!battleCharacterSelectionSubscribed)
			SubscribeBattleCharacterSelection();
	}

	private void SubscribeBattleCharacterSelection()
	{
		if (battleCharacterSelectionSubscribed || !BattleCharacterSelectionService.HasInstance)
		{
			return;
		}
		BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnBattleCharacterSelectionChanged;
		BattleCharacterSelectionService.Instance.SelectionStateChanged += OnBattleCharacterSelectionStateChanged;
		battleCharacterSelectionSubscribed = true;
	}

	private void UnsubscribeBattleCharacterSelection()
	{
		if (!battleCharacterSelectionSubscribed)
		{
			return;
		}
		if (BattleCharacterSelectionService.HasInstance)
		{
			BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnBattleCharacterSelectionChanged;
			BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnBattleCharacterSelectionStateChanged;
		}
		battleCharacterSelectionSubscribed = false;
	}

	private void OnBattleCharacterSelectionChanged(string _)
	{
		RefreshSelectedBattleCharacterViews();
	}

	private void OnBattleCharacterSelectionStateChanged()
	{
		RefreshSelectedBattleCharacterViews();
	}

	public void RefreshSelectedBattleCharacterViews()
	{
		RefreshBattleLobbyTopBarValues();
		BattleLobbyChar battleLobbyChar = UnityEngine.Object.FindAnyObjectByType<BattleLobbyChar>(FindObjectsInactive.Include);
		if (battleLobbyChar != null)
		{
			battleLobbyChar.ConfirmAndRefresh();
		}
		MainLobbySelectedCharacterView mainLobbyView = UnityEngine.Object.FindAnyObjectByType<MainLobbySelectedCharacterView>(FindObjectsInactive.Include);
		if (mainLobbyView != null)
		{
			mainLobbyView.Refresh();
		}
	}

	private void OnDestroy()
	{
		StopBattleTilePackResultRevealRoutine();
		battleTilePackResultRoot = null;
		SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
		if (openCharacterCarouselButton != null)
		{
			openCharacterCarouselButton.onClick.RemoveListener(OnClickOpenCharacterCarousel);
		}
		if (returnToLobbyButton != null)
		{
			returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);
		}
		if (battleShopButton != null)
		{
			battleShopButton.onClick.RemoveListener(OnClickOpenBattleShop);
		}
		if (battleTileInventoryButton != null)
		{
			battleTileInventoryButton.onClick.RemoveListener(OnClickOpenBattleTileInventory);
		}
		if (weeklyRewardButton != null)
		{
			weeklyRewardButton.onClick.RemoveListener(OnClickOpenWeeklyRewards);
		}
		if (dailyHeroBonusButton != null)
		{
			dailyHeroBonusButton.onClick.RemoveListener(OnClickOpenDailyHeroBonus);
		}
		StopDailyHeroAttentionRoutine();
		if (dailyHeroBoostButton != null)
		{
			dailyHeroBoostButton.onClick.RemoveListener(OnClickDailyHeroBoostAd);
		}
		if (randomMatchButton != null)
		{
			randomMatchButton.onClick.RemoveListener(OnClickRandomMatch);
		}
		if (localWifiBattleButton != null)
		{
			localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);
		}
		if (rankedBattleButton != null)
		{
			rankedBattleButton.onClick.RemoveListener(OnClickRankedMatch);
		}
		if (duelChallengeButton != null)
		{
			duelChallengeButton.onClick.RemoveListener(OnClickDuelChallenge);
		}
		if (tournamentButton != null)
		{
			tournamentButton.onClick.RemoveListener(OnClickTournament);
		}
		if (energyAdButton != null)
		{
			energyAdButton.onClick.RemoveListener(OnClickRewardedEnergyAd);
		}
		if (shopEnergyTabButton != null)
		{
			shopEnergyTabButton.onClick.RemoveListener(ShowBattleShopEnergy);
		}
		if (shopCharactersTabButton != null)
		{
			shopCharactersTabButton.onClick.RemoveListener(ShowBattleShopCharacters);
		}
		if (shopSkinsTabButton != null)
		{
			shopSkinsTabButton.onClick.RemoveListener(ShowBattleShopSkins);
		}
		if (shopBattleTilesTabButton != null)
		{
			shopBattleTilesTabButton.onClick.RemoveListener(ShowBattleShopBattleTiles);
		}
		if (shopBuyEnergyButton != null)
		{
			shopBuyEnergyButton.onClick.RemoveListener(OnClickBuyEnergyWithAmetist);
		}
		if (shopRewardedEnergyButton != null)
		{
			shopRewardedEnergyButton.onClick.RemoveListener(OnClickRewardedEnergyAd);
		}
		if (shopBuyDragonMaleButton != null)
		{
			shopBuyDragonMaleButton.onClick.RemoveListener(OnClickBuyDragonMale);
		}
		if (shopBuyDragonFemaleButton != null)
		{
			shopBuyDragonFemaleButton.onClick.RemoveListener(OnClickBuyDragonFemale);
		}
		if (shopAmetistSmallButton != null)
		{
			shopAmetistSmallButton.onClick.RemoveListener(OnClickBuyAmetistSmall);
		}
		if (shopAmetistMediumButton != null)
		{
			shopAmetistMediumButton.onClick.RemoveListener(OnClickBuyAmetistMedium);
		}
		if (shopAmetistBigButton != null)
		{
			shopAmetistBigButton.onClick.RemoveListener(OnClickBuyAmetistBig);
		}
		if (shopAmetistLegendButton != null)
		{
			shopAmetistLegendButton.onClick.RemoveListener(OnClickBuyAmetistLegend);
		}
		if (shopBattleTileDailyAdButton != null)
		{
			shopBattleTileDailyAdButton.onClick.RemoveListener(OnClickOpenDailyBattleTilePack);
		}
		if (shopBattleTileMediumButton != null)
		{
			shopBattleTileMediumButton.onClick.RemoveListener(OnClickOpenMediumBattleTilePack);
		}
		if (shopBattleTileHighButton != null)
		{
			shopBattleTileHighButton.onClick.RemoveListener(OnClickOpenHighBattleTilePack);
		}
		if (shopBattleTileAmetistButton != null)
		{
			shopBattleTileAmetistButton.onClick.RemoveListener(OnClickOpenAmetistBattleTilePack);
		}
		EnergyService.EnergyChanged -= RefreshBattleProgressUi;
		ProfileService.ProfileChanged -= RefreshBattleProgressUi;
		ProfileService.ProfileChanged -= RefreshBattleTileInventoryUi;
		AppSettings.OnLanguageChanged -= OnLanguageChanged;
		CurrencyService.CurrencyChanged -= RefreshBattleShopUi;
		DestroyBattleLobbyRuntimeHudObjects(force: true);
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		ApplyBattleLobbyVisuals();
		RefreshBattleProgressUi();
		RefreshBattleTileInventoryUi();
		RefreshEnergyUi();
		RebuildBattleShopForLanguage();
	}

	private void HandleActiveSceneChanged(Scene previous, Scene current)
	{
		if (!string.Equals(current.name, battleLobbySceneName, StringComparison.Ordinal))
		{
			SetLobbyButtonsVisible(visible: false);
			SetCharacterEntryPointVisible(visible: false);
			DestroyBattleLobbyRuntimeHudObjects(force: true, immediate: true);
			return;
		}
		SettingsMenuUI.ForceCloseAllSettingsMenus();
		BattleLobbyUiCoordinator.ResetForLobbyEntry();
		matchButtonsSuppressedBySettings = false;
		CleanupStaleBattleLobbyOverlays();
		EnsureAndBindLobbyButtonsIfNeeded();
		EnsureBattleProgressUi();
		RefreshBattleProgressUi();
		ApplyBattleLobbyVisuals();
	}

	public void RefreshBattleProgressUi()
	{
		EnsureBattleProgressUi();
		RefreshBattleLobbyTopBarValues();
		if (!ShouldShowBattleProgressUi())
		{
			return;
		}
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		if (playerProfile == null)
		{
			ApplyBattleProgressFallback();
			return;
		}
		playerProfile.EnsureData();
		MahjongBattleData mahjongBattleData = ((playerProfile.Mahjong != null) ? playerProfile.Mahjong.Battle : null);
		int num = ((mahjongBattleData == null) ? 1 : Mathf.Max(1, mahjongBattleData.Level));
		int num2 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Experience) : 0);
		int num3 = ((mahjongBattleData != null) ? Mathf.Max(1, mahjongBattleData.GetExperienceRequiredForNextLevel()) : 100);
		int num4 = Mathf.Max(0, num3 - num2);
		int num5 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Wins) : 0);
		int num6 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Losses) : 0);
		int num7 = ((mahjongBattleData != null) ? Mathf.Clamp(mahjongBattleData.MvpRatePercent, 0, 100) : 0);
		if (battleLevelText != null)
		{
			battleLevelText.text = GameLocalization.Format("battle.lobby.level", num);
		}
		if (battleExpText != null)
		{
			battleExpText.text = GameLocalization.Format("battle.lobby.exp", num2, num3, num4);
		}
		if (battleStatsText != null)
		{
			battleStatsText.text = GameLocalization.Format("battle.lobby.stats", num5, num6, num7);
		}
		RefreshEnergyUi();
	}

	public void OnClickRandomMatch()
	{
		if (!TryShowTutorialGate())
		{
			if (!BattleTotemRequirementUI.EnsureBattleReady())
			{
				return;
			}
			if (randomMatchUsesOnlineRanked)
			{
				RankedLeagueSelectUI.Show(battleGameSceneName);
				Log("RandomMatch selected, routed to ranked league selection.");
			}
			else if (HasMatchEnergy())
			{
				RandomBattleLobbyUI.Show(battleGameSceneName);
				Log("RandomMatch selected, online search opened with bot fallback.");
			}
		}
	}

	public void OnClickStart()
	{
		OnClickRandomMatch();
	}

	public void OnClickOpenCharacterCarousel()
	{
		OpenCharacterCarousel();
	}

	public void OnClickReturnToLobby()
	{
		if (string.IsNullOrWhiteSpace(mainLobbySceneName))
		{
			Log("mainLobbySceneName is empty.");
		}
		else
		{
			LoadScene(mainLobbySceneName);
		}
	}

	public void OnClickOpenBattleShop()
	{
		EnsureCurrencyService();
		EnsureBattleShopUi();
		ShowBattleShopEnergy();
		if (battleShopRoot != null)
		{
			BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Shop);
			battleShopRoot.SetActive(value: true);
			battleShopRoot.transform.SetAsLastSibling();
			SetMatchButtonsSuppressedBySettings(suppressed: true);
			ShowPendingBattleTilePackResult();
		}
	}

	public void OnClickOpenBattleTileInventory()
	{
		EnsureBattleTileInventoryUi();
		RefreshBattleTileInventoryUi();
		if (battleTileInventoryRoot != null)
		{
			BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Shop);
			battleTileInventoryRoot.SetActive(value: true);
			battleTileInventoryRoot.transform.SetAsLastSibling();
			SetMatchButtonsSuppressedBySettings(suppressed: true);
		}
	}

	public void OnClickOpenWeeklyRewards()
	{
	}

	public void OnClickOpenDailyHeroBonus()
	{
		MarkDailyHeroBonusSeenToday();
		UpdateDailyHeroBonusNotification();
		EnsureDailyHeroBonusUi();
		if (dailyHeroBonusRoot != null)
		{
			BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.DailyHeroBonus);
			dailyHeroBonusRoot.SetActive(value: true);
			dailyHeroBonusRoot.transform.SetAsLastSibling();
			SetDailyHeroBonusModalActive(active: true);
		}
	}

	private void CloseBattleShop()
	{
		CloseBattleTilePackResult();
		if (battleShopRoot != null)
		{
			SetGameObjectActiveSafe(battleShopRoot, active: false);
		}
		SetMatchButtonsSuppressedBySettings(suppressed: false);
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Shop);
	}

	private void CloseBattleTileInventory()
	{
		CloseBattleTileProfile();
		if (battleTileInventoryRoot != null)
		{
			SetGameObjectActiveSafe(battleTileInventoryRoot, active: false);
		}
		SetMatchButtonsSuppressedBySettings(suppressed: false);
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Shop);
	}

	private void CloseWeeklyRewards()
	{
		if (weeklyRewardRoot != null)
		{
			SetGameObjectActiveSafe(weeklyRewardRoot, active: false);
		}
		SetWeeklyRewardModalActive(active: false);
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Rewards);
		RefreshWeeklyRewardUi();
	}

	private void CloseDailyHeroBonus()
	{
		if (dailyHeroBonusRoot != null)
		{
			SetGameObjectActiveSafe(dailyHeroBonusRoot, active: false);
		}
		SetDailyHeroBonusModalActive(active: false);
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.DailyHeroBonus);
	}

	private void SetWeeklyRewardModalActive(bool active)
	{
		SetDownBarPanelVisible(!active);
		SetTopMirrorBarPanelVisible(!active);
		SetButtonVisible(returnToLobbyButton, !active);
		SetButtonVisible(battleShopButton, !active);
		SetButtonVisible(weeklyRewardButton, visible: false);
		SetButtonVisible(dailyHeroBonusButton, !active);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			SetButtonVisible(openCharacterCarouselButton, !active);
		}
		SetMatchButtonsVisible(!active && !matchButtonsSuppressedBySettings);
		SetMatchButtonsSuppressedBySettings(active);
	}

	private void SetDailyHeroBonusModalActive(bool active)
	{
		SetDownBarPanelVisible(!active);
		SetTopMirrorBarPanelVisible(!active);
		SetButtonVisible(returnToLobbyButton, !active);
		SetButtonVisible(battleShopButton, !active);
		SetButtonVisible(weeklyRewardButton, visible: false);
		SetButtonVisible(dailyHeroBonusButton, !active);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			SetButtonVisible(openCharacterCarouselButton, !active);
		}
		SetMatchButtonsVisible(!active && !matchButtonsSuppressedBySettings);
		SetMatchButtonsSuppressedBySettings(active);
	}

	public void OpenCharacterCarousel()
	{
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.CharacterCarousel);
		AutoResolveCharacterSelectionLinks();
		if (characterCarouselRoot != null)
		{
			SetLobbyHudVisibleWhileCarousel(visible: false);
			SetBattleLobbyCharacterPreviewSuppressed(suppressed: true);
			SetCharacterCarouselSceneEffectsSuppressed(suppressed: true);
			ApplyCharacterCarouselWindowChrome(characterCarouselRoot);
			characterCarouselRoot.SetActive(value: true);
			characterCarouselRoot.transform.SetAsLastSibling();
			BattleCharacterCircularCarousel componentInChildren = characterCarouselRoot.GetComponentInChildren<BattleCharacterCircularCarousel>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.RefreshButtons();
			}
			Log("Character carousel opened.");
		}
	}

	private static void ApplyCharacterCarouselWindowChrome(GameObject rootObject)
	{
		if (rootObject == null)
		{
			return;
		}
		RectTransform rectTransform = rootObject.transform as RectTransform;
		if (rectTransform != null)
		{
			StretchToFullscreen(rectTransform);
		}
		Image image = rootObject.GetComponent<Image>() ?? FindLargestBackgroundImage(rootObject.transform);
		if (image == null && rootObject.transform is RectTransform)
		{
			image = rootObject.AddComponent<Image>();
		}
		if (!(image == null))
		{
			BattlePopupStyle.ApplyWindow(image);
			image.color = Color.white;
			image.preserveAspect = false;
			image.transform.SetAsFirstSibling();
			RemoveCharacterCarouselBlockingPanels(rootObject.transform, image);
			RectTransform rectTransform2 = image.rectTransform;
			if (rectTransform2 != null && image.transform == rootObject.transform)
			{
				StretchToFullscreen(rectTransform2);
			}
		}
	}

	private static void RemoveCharacterCarouselBlockingPanels(Transform root, Image windowImage)
	{
		if (root == null)
		{
			return;
		}
		RectTransform rectTransform = root as RectTransform;
		Vector2 vector = ((rectTransform != null) ? rectTransform.rect.size : Vector2.zero);
		float num = Mathf.Abs(vector.x * vector.y);
		Image[] componentsInChildren = root.GetComponentsInChildren<Image>(includeInactive: true);
		foreach (Image image in componentsInChildren)
		{
			if (image == null || image == windowImage || image.GetComponent<Button>() != null)
			{
				continue;
			}
			string text = image.gameObject.name;
			if (string.IsNullOrEmpty(text) || (text.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Portrait", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Coin", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Button", StringComparison.OrdinalIgnoreCase) < 0))
			{
				RectTransform rectTransform2 = image.rectTransform;
				Vector2 vector2 = ((rectTransform2 != null) ? rectTransform2.rect.size : Vector2.zero);
				float num2 = Mathf.Abs(vector2.x * vector2.y);
				bool num3 = num > 1f && num2 >= num * 0.42f;
				bool flag = image.sprite == null || image.type == Image.Type.Simple;
				if (num3 && flag)
				{
					image.enabled = false;
					image.sprite = null;
					image.raycastTarget = false;
				}
			}
		}
	}

	private static Image FindLargestBackgroundImage(Transform root)
	{
		if (root == null)
		{
			return null;
		}
		Image[] componentsInChildren = root.GetComponentsInChildren<Image>(includeInactive: true);
		Image image = null;
		float num = -1f;
		foreach (Image image2 in componentsInChildren)
		{
			if (image2 == null || image2.GetComponent<Button>() != null)
			{
				continue;
			}
			string text = image2.gameObject.name;
			if (string.IsNullOrEmpty(text) || (text.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Portrait", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Coin", StringComparison.OrdinalIgnoreCase) < 0))
			{
				RectTransform rectTransform = image2.rectTransform;
				Vector2 vector = ((rectTransform != null) ? rectTransform.rect.size : Vector2.zero);
				float num2 = Mathf.Abs(vector.x * vector.y);
				if (image == null || num2 > num)
				{
					image = image2;
					num = num2;
				}
			}
		}
		return image;
	}

	private static void StretchToFullscreen(RectTransform rect)
	{
		if (!(rect == null))
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.localScale = Vector3.one;
		}
	}

	private void QueueAutoOpenCharacterCarouselIfNeeded()
	{
		if (openCharacterCarouselWhenNoCharacterSelected && !HasSelectedBattleCharacter())
		{
			if (autoOpenCharacterCarouselRoutine != null)
			{
				StopCoroutine(autoOpenCharacterCarouselRoutine);
			}
			autoOpenCharacterCarouselRoutine = StartCoroutine(AutoOpenCharacterCarouselIfNoSelectionRoutine());
		}
	}

	private IEnumerator AutoOpenCharacterCarouselIfNoSelectionRoutine()
	{
		int delayFrames = Mathf.Max(0, autoOpenCharacterCarouselDelayFrames);
		for (int i = 0; i < delayFrames; i++)
		{
			yield return null;
		}
		float deadline = Time.unscaledTime + Mathf.Max(0f, autoOpenCharacterCarouselMaxWaitSeconds);
		while (!BattleCharacterSelectionService.HasInstance && Time.unscaledTime < deadline)
		{
			yield return null;
		}
		autoOpenCharacterCarouselRoutine = null;
		if (!HasSelectedBattleCharacter())
		{
			OpenCharacterCarousel();
			Log("Auto-opened character carousel because no battle character is selected.");
		}
	}

	private static bool HasSelectedBattleCharacter()
	{
		return BattleTotemRequirementUI.HasSelectedBattleCharacter();
	}

	public void CloseCharacterCarousel()
	{
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.CharacterCarousel);
		AutoResolveCharacterSelectionLinks();
		if (characterCarouselRoot != null)
		{
			SetGameObjectActiveSafe(characterCarouselRoot, active: false);
		}
		SetBattleLobbyCharacterPreviewSuppressed(suppressed: false);
		SetCharacterCarouselSceneEffectsSuppressed(suppressed: false);
		SetLobbyHudVisibleWhileCarousel(visible: true);
	}

	public void RestoreLobbyHudAfterCharacterCarouselClosed()
	{
		BattleLobbyUiCoordinator.CloseAllModals();
		matchButtonsSuppressedBySettings = false;
		EnsureBattleLobbyRuntimeCanvasActive();
		AutoResolveCharacterSelectionLinks();
		if (characterCarouselRoot != null)
		{
			SetGameObjectActiveSafe(characterCarouselRoot, active: false);
		}
		EnsureAndBindLobbyButtonsIfNeeded();
		SetTutorialUtilityButtonsVisible(visible: true);
		SetBattleLobbyCharacterPreviewSuppressed(suppressed: false);
		SetCharacterCarouselSceneEffectsSuppressed(suppressed: false);
		SetLobbyHudVisibleWhileCarousel(visible: true);
		RefreshBattleProgressUi();
		RefreshEnergyUi();
		RefreshSelectedBattleCharacterViews();
		SetButtonLabelText(openCharacterCarouselButton, ResolveCharacterButtonText());
		ApplyBattleLobbyVisuals();
		ApplyBattleLobbyButtonLayout();
		SetLobbyButtonsVisible(visible: true);
		SetCharacterEntryButtonVisible(visible: true);
	}

	public void RequestRestoreLobbyHudAfterCharacterCarouselClosed()
	{
		BattleLobbyUiCoordinator.CloseAllModals();
		matchButtonsSuppressedBySettings = false;
		AutoResolveCharacterSelectionLinks();
		if (characterCarouselRoot != null)
		{
			SetGameObjectActiveSafe(characterCarouselRoot, active: false);
		}
		SetBattleLobbyCharacterPreviewSuppressed(suppressed: false);
		SetCharacterCarouselSceneEffectsSuppressed(suppressed: false);
		if (restoreLobbyAfterCharacterCarouselRoutine != null)
		{
			StopCoroutine(restoreLobbyAfterCharacterCarouselRoutine);
		}
		restoreLobbyAfterCharacterCarouselRoutine = StartCoroutine(RestoreLobbyHudAfterCharacterCarouselClosedRoutine());
	}

	private IEnumerator RestoreLobbyHudAfterCharacterCarouselClosedRoutine()
	{
		yield return null;
		yield return null;
		restoreLobbyAfterCharacterCarouselRoutine = null;
		RestoreLobbyHudAfterCharacterCarouselClosed();
	}

	private bool TryShowTutorialGate()
	{
		if (!BattleTotemRequirementUI.EnsureBattleCharacterReady())
		{
			return true;
		}

		if (BattleLoreTutorialSession.IsTrainingComplete)
		{
			return false;
		}
		ShowTutorialGate();
		return true;
	}

	private void ShowTutorialGate()
	{
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.LoreTutorial);
		EnsureTutorialGateUi();
		if (!(tutorialGateRoot == null))
		{
			tutorialGateRoot.SetActive(value: true);
			tutorialGateRoot.transform.SetAsLastSibling();
			SetMatchButtonsSuppressedBySettings(suppressed: true);
		}
	}

	private void CloseTutorialGate()
	{
		if (tutorialGateRoot != null)
		{
			SetGameObjectActiveSafe(tutorialGateRoot, active: false);
		}
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.LoreTutorial);
		SetMatchButtonsSuppressedBySettings(suppressed: false);
	}

	private void EnsureTutorialGateUi()
	{
		if (!(tutorialGateRoot != null))
		{
			Canvas canvas = FindActiveSceneCanvas();
			if (!(canvas == null))
			{
				tutorialGateRoot = new GameObject("BattleTutorialGateOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
				tutorialGateRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
				ConfigureOverlayCanvas(tutorialGateRoot);
				RectTransform component = tutorialGateRoot.GetComponent<RectTransform>();
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.offsetMin = Vector2.zero;
				component.offsetMax = Vector2.zero;
				Image component2 = tutorialGateRoot.GetComponent<Image>();
				component2.color = Color.black;
				component2.raycastTarget = true;
				GameObject obj = CreateShopPanel(tutorialGateRoot.transform, "TutorialGatePanel", new Vector2(1120f, 600f), Vector2.zero, Color.white);
				FitPanelInsideCanvas(obj.transform as RectTransform, canvas, 24f);
				CreateShopText(obj.transform, "Title", BattleLobbyText("Сначала обучение", "Training first", "Önce eğitim", "Zuerst Training"), new Vector2(0f, 180f), new Vector2(820f, 72f), 54f, TextAlignmentOptions.Center, Color.white);
				CreateShopText(obj.transform, "Body", BattleLobbyText("Боевые режимы откроются после прохождения обучения.\nЗаверши все уроки в окне обучения, затем возвращайся к матчам.", "Battle modes unlock after training. Finish all lessons in the training window, then return to matches.", "Savaş modları eğitimden sonra açılır. Eğitim penceresindeki tüm dersleri bitir, sonra maçlara dön.", "Kampfmodi werden nach dem Training freigeschaltet. Beende alle Lektionen und kehre dann zu den Matches zurueck."), new Vector2(0f, 28f), new Vector2(860f, 190f), 34f, TextAlignmentOptions.Center, Color.white);
				CreateShopButton(obj.transform, "ButtonTutorialGateOk", BattleLobbyText("Понятно", "OK", "Tamam", "OK"), new Vector2(0f, -192f), new Vector2(420f, 90f), Color.white, 38f).onClick.AddListener(CloseTutorialGate);
				SetGameObjectActiveSafe(tutorialGateRoot, active: false);
			}
		}
	}

	private static void SetTutorialUtilityButtonsVisible(bool visible)
	{
		SetSceneObjectsActiveByName("ButtonBattleStoneAuction", visible);
		SetSceneObjectsActiveByName("ButtonBattleStoneForge", visible);
		SetSceneObjectsActiveByName("ButtonBattleLoreTutorial", visible && !BattleLoreTutorialSession.IsTrainingComplete);
	}

	public void OnClickRankedMatch()
	{
		if (!TryShowTutorialGate())
		{
			if (!BattleTotemRequirementUI.EnsureBattleReady())
			{
				return;
			}
			RankedLeagueSelectUI.Show(battleGameSceneName);
			Log("RankedMatch selected, league selection opened.");
		}
	}

	public void OnClickDuelChallenge()
	{
		if (!TryShowTutorialGate())
		{
			if (!BattleTotemRequirementUI.EnsureBattleReady())
			{
				return;
			}
			DuelChallengeLobbyUI.Show(battleGameSceneName);
			Log("DuelChallenge selected, challenge window opened.");
		}
	}

	public void OnClickTournament()
	{
		if (!TryShowTutorialGate())
		{
			ShowTournamentComingSoon();
			Log("Tournament selected, coming-soon window opened.");
		}
	}

	private void ShowTournamentComingSoon()
	{
		EnsureTournamentComingSoonUi();
		if (tournamentComingSoonRoot != null)
		{
			BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Tournament);
			tournamentComingSoonRoot.SetActive(value: true);
			tournamentComingSoonRoot.transform.SetAsLastSibling();
		}
	}

	private void CloseTournamentComingSoon()
	{
		if (tournamentComingSoonRoot != null)
		{
			SetGameObjectActiveSafe(tournamentComingSoonRoot, active: false);
		}
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Tournament);
	}

	private void EnsureTournamentComingSoonUi()
	{
		if (tournamentComingSoonRoot != null)
		{
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return;
		}
		tournamentComingSoonRoot = new GameObject("BattleTournamentComingSoonOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		tournamentComingSoonRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
		ConfigureOverlayCanvas(tournamentComingSoonRoot);
		RectTransform component = tournamentComingSoonRoot.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = tournamentComingSoonRoot.GetComponent<Image>();
		component2.color = Color.black;
		component2.raycastTarget = true;
		GameObject obj = CreateShopPanel(tournamentComingSoonRoot.transform, "TournamentComingSoonPanel", new Vector2(1120f, 600f), Vector2.zero, Color.white);
		FitPanelInsideCanvas(obj.transform as RectTransform, canvas, 24f);
		CreateShopText(obj.transform, "Title", BattleLobbyText("Турниры", "Tournaments", "Turnuvalar", "Turniere"), new Vector2(0f, 174f), new Vector2(820f, 72f), 54f, TextAlignmentOptions.Center, Color.white);
		CreateShopText(obj.transform, "Body", BattleLobbyText("Турниры сейчас в разработке.", "Tournaments are currently in development.", "Turnuvalar şu anda geliştirme aşamasında.", "Turniere sind derzeit in Entwicklung."), new Vector2(0f, 28f), new Vector2(860f, 150f), 38f, TextAlignmentOptions.Center, Color.white);
		CreateShopButton(obj.transform, "ButtonTournamentComingSoonOk", BattleLobbyText("Понял", "Got it", "Anladım", "Verstanden"), new Vector2(0f, -178f), new Vector2(420f, 90f), Color.white, 38f).onClick.AddListener(CloseTournamentComingSoon);
		SetGameObjectActiveSafe(tournamentComingSoonRoot, active: false);
	}

	public void OnClickFriendMatch()
	{
		if (!TryShowTutorialGate())
		{
			MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.FriendMatch);
			if (!string.IsNullOrWhiteSpace(friendLobbySceneName))
			{
				LoadScene(friendLobbySceneName);
			}
			else
			{
				Log("FriendMatch selected, but friendLobbySceneName is empty.");
			}
		}
	}

	public void OnClickLocalWifiMatch()
	{
		if (TryShowTutorialGate())
		{
			return;
		}
		if (!BattleTotemRequirementUI.EnsureBattleReady())
		{
			return;
		}
		if (RouteLocalWifiSlotToTournament)
		{
			OnClickTournament();
			Log("LocalWifiMatch slot selected, routed to Tournament while Wi-Fi is disabled.");
		}
		else if (HasMatchEnergy())
		{
			MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.LocalWifiMatch);
			if (!string.IsNullOrWhiteSpace(localWifiSceneName))
			{
				LoadScene(localWifiSceneName);
				return;
			}
			LocalWifiBattleLobbyUI.Show(battleGameSceneName);
			Log("LocalWifiMatch selected, runtime Wi-Fi lobby opened.");
		}
	}

	private void OpenBattleMode(MahjongBattleLobbyMode mode)
	{
		if (!TryShowTutorialGate())
		{
			if (!BattleTotemRequirementUI.EnsureBattleReady())
			{
				return;
			}
			MahjongBattleLobbySession.SetMode(mode);
			PrepareBotOpponent(mode);
			if (string.IsNullOrWhiteSpace(battleGameSceneName))
			{
				Log("battleGameSceneName is empty.");
			}
			else
			{
				LoadScene(battleGameSceneName);
			}
		}
	}

	private void LoadScene(string sceneName)
	{
		if (closeCharacterCarouselOnEnter)
		{
			CloseCharacterCarousel();
		}
		if (DoorFx.I != null && DoorFx.I.IsReady())
		{
			DoorFx.I.LoadScene(sceneName);
		}
		else
		{
			SceneManager.LoadScene(sceneName);
		}
		Log($"LoadScene -> {sceneName} | Mode={MahjongBattleLobbySession.SelectedMode}");
	}

	private void PrepareBotOpponent(MahjongBattleLobbyMode mode)
	{
		MahjongBattleBotService mahjongBattleBotService = MahjongBattleBotService.I;
		if (mahjongBattleBotService == null)
		{
			mahjongBattleBotService = new GameObject("MahjongBattleBotService").AddComponent<MahjongBattleBotService>();
		}
		int playerRankPoints = ResolvePlayerBattleRankPoints();
		MahjongBattleOpponentData mahjongBattleOpponentData = mahjongBattleBotService.CreateOpponent(mode, playerRankPoints);
		MahjongSession.StartBattle(mahjongBattleOpponentData);
		Log("Bot opponent prepared | Name=" + mahjongBattleOpponentData.DisplayName + " | " + $"Rank={mahjongBattleOpponentData.RankTier} {mahjongBattleOpponentData.RankPoints} | " + $"W/L={mahjongBattleOpponentData.Wins}/{mahjongBattleOpponentData.Losses}");
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

	private void AutoResolveCharacterSelectionLinks()
	{
		if (characterCarouselRoot == null && !string.IsNullOrWhiteSpace(characterCarouselObjectName))
		{
			GameObject gameObject = FindObjectByName(characterCarouselObjectName);
			if (gameObject != null)
			{
				characterCarouselRoot = gameObject;
			}
		}
		if (!autoBindOpenCharacterButton)
		{
			return;
		}
		if (ShouldShowLobbyButtons())
		{
			if (openCharacterCarouselButton == null || openCharacterCarouselButton.gameObject == null || !string.Equals(openCharacterCarouselButton.gameObject.name, "OpenCharacterCarouselButton", StringComparison.Ordinal))
			{
				openCharacterCarouselButton = FindRuntimeButtonByName("OpenCharacterCarouselButton");
				if (openCharacterCarouselButton == null)
				{
					openCharacterCarouselButton = CreateOpenCharacterButton();
				}
			}
		}
		else
		{
			if (openCharacterCarouselButton != null)
			{
				return;
			}
			GameObject gameObject2 = ResolveOpenCharacterButtonObject();
			if (gameObject2 == null)
			{
				if (createOpenCharacterButtonIfMissing)
				{
					openCharacterCarouselButton = CreateOpenCharacterButton();
				}
			}
			else
			{
				openCharacterCarouselButton = EnsureOpenCharacterHitButton(gameObject2);
			}
		}
	}

	private GameObject ResolveOpenCharacterButtonObject()
	{
		GameObject gameObject = ((!string.IsNullOrWhiteSpace(openCharacterButtonObjectName)) ? FindObjectByName(openCharacterButtonObjectName) : null);
		if (gameObject != null)
		{
			return gameObject;
		}
		BattleLobbyChar battleLobbyChar = UnityEngine.Object.FindAnyObjectByType<BattleLobbyChar>(FindObjectsInactive.Include);
		if (battleLobbyChar != null)
		{
			return battleLobbyChar.gameObject;
		}
		Image image = FindImageByName("PreviewLobbyImage");
		if (image != null)
		{
			return image.gameObject;
		}
		BattleCharacterModelView battleCharacterModelView = UnityEngine.Object.FindAnyObjectByType<BattleCharacterModelView>(FindObjectsInactive.Include);
		if (!(battleCharacterModelView != null))
		{
			return null;
		}
		return battleCharacterModelView.gameObject;
	}

	private Button EnsureOpenCharacterHitButton(GameObject targetObject)
	{
		if (targetObject == null)
		{
			return null;
		}
		RectTransform rectTransform = targetObject.transform as RectTransform;
		if (rectTransform == null)
		{
			Button button = targetObject.GetComponent<Button>();
			if (button == null)
			{
				button = targetObject.AddComponent<Button>();
			}
			Graphic component = targetObject.GetComponent<Graphic>();
			if (component != null)
			{
				component.raycastTarget = true;
				button.targetGraphic = component;
			}
			return button;
		}
		Transform transform = rectTransform.Find("OpenCharacterHitArea");
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject("OpenCharacterHitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)));
		if (gameObject.transform.parent != rectTransform)
		{
			gameObject.transform.SetParent(rectTransform, worldPositionStays: false);
		}
		RectTransform obj = gameObject.transform as RectTransform;
		obj.anchorMin = Vector2.zero;
		obj.anchorMax = Vector2.one;
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		obj.pivot = new Vector2(0.5f, 0.5f);
		obj.localScale = Vector3.one;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(1f, 1f, 1f, 0.001f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.transition = Selectable.Transition.None;
		component3.targetGraphic = component2;
		component3.interactable = true;
		gameObject.transform.SetAsLastSibling();
		return component3;
	}

	private static Image FindImageByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		Image[] array = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
		foreach (Image image in array)
		{
			if (image != null && string.Equals(image.name, objectName, StringComparison.Ordinal))
			{
				return image;
			}
		}
		foreach (Image image2 in array)
		{
			if (image2 != null && image2.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return image2;
			}
		}
		return null;
	}

	private static GameObject FindObjectByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		Transform[] array = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
		foreach (Transform transform in array)
		{
			if (transform != null && string.Equals(transform.name, objectName, StringComparison.Ordinal))
			{
				return transform.gameObject;
			}
		}
		return null;
	}

	private Button CreateOpenCharacterButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("OpenCharacterCarouselButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = ResolveCharacterButtonPosition();
		component.sizeDelta = ResolveCharacterButtonSize();
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.09f, 0.1f, 0.12f, 0.88f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyBattleLobbyUtilityButton(component3);
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = ResolveCharacterButtonText();
		component5.fontSize = 28f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 34f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private void EnsureReturnButton()
	{
		if (!(returnToLobbyButton != null) && autoCreateReturnButton)
		{
			returnToLobbyButton = CreateReturnToLobbyButton();
		}
	}

	private void EnsureAndBindLobbyButtonsIfNeeded()
	{
		EnsureEventSystem();
		bool flag = ShouldShowLobbyButtons();
		if (flag)
		{
			ResolveLobbyButtonReferences();
			EnsureReturnButton();
			BindReturnButton();
			EnsureBattleShopButton();
			BindBattleShopButton();
			EnsureBattleTileInventoryButton();
			BindBattleTileInventoryButton();
			SetButtonVisible(weeklyRewardButton, visible: false);
			EnsureDailyHeroBonusButton();
			BindDailyHeroBonusButton();
			EnsureDownBarPanel();
			EnsureTopMirrorBarPanel();
			EnsureRandomMatchButton();
			BindRandomMatchButton();
			EnsureRankedBattleButton();
			BindRankedBattleButton();
			EnsureDuelChallengeButton();
			BindDuelChallengeButton();
			EnsureLocalWifiBattleButton();
			BindLocalWifiBattleButton();
			if (!RouteLocalWifiSlotToTournament)
			{
				EnsureTournamentButton();
				BindTournamentButton();
			}
			else
			{
				ResolveLobbyButtonReferences();
				SetButtonVisible(tournamentButton, visible: false);
			}
			DuelChallengeLobbyUI.Ensure(battleGameSceneName);
			TournamentLobbyUI.Ensure(battleGameSceneName);
			ApplyBattleLobbyButtonLayout();
			UpdateDailyHeroBonusNotification();
			SetLobbyButtonsVisible(flag);
			SetDownBarPanelVisible(flag);
			SetTopMirrorBarPanelVisible(flag);
			SetCharacterEntryPointVisible(flag);
			if (flag)
			{
				ApplyBattleLobbyVisuals();
			}
			UpdateDailyHeroBonusNotification();
		}
		else
		{
			ResolveLobbyButtonReferences();
			DestroyBattleLobbyRuntimeHudObjects(force: false);
		}
	}

	private bool ShouldShowLobbyButtons()
	{
		return string.Equals(SceneManager.GetActiveScene().name, battleLobbySceneName, StringComparison.Ordinal);
	}

	private static bool IsBattleTrainingLocked()
	{
		return !BattleLoreTutorialSession.IsTrainingComplete;
	}

	private IEnumerator OpenTournamentOverlayAfterLobbyRestore()
	{
		yield return null;
		TournamentLobbyUI.Show(battleGameSceneName);
	}

	private void SetLobbyButtonsVisible(bool visible)
	{
		SetButtonVisible(returnToLobbyButton, visible);
		SetButtonVisible(battleShopButton, visible);
		SetButtonVisible(battleTileInventoryButton, visible);
		SetButtonVisible(weeklyRewardButton, visible: false);
		SetButtonVisible(dailyHeroBonusButton, visible);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			SetButtonVisible(openCharacterCarouselButton, visible);
		}
		SetDownBarPanelVisible(visible);
		SetTopMirrorBarPanelVisible(visible);
		SetMatchButtonsVisible(visible && !matchButtonsSuppressedBySettings);
		if (!visible && battleShopRoot != null)
		{
			SetGameObjectActiveSafe(battleShopRoot, active: false);
		}
		if (!visible && battleTileInventoryRoot != null)
		{
			SetGameObjectActiveSafe(battleTileInventoryRoot, active: false);
		}
		if (!visible && weeklyRewardRoot != null)
		{
			SetGameObjectActiveSafe(weeklyRewardRoot, active: false);
		}
		if (!visible && dailyHeroBonusRoot != null)
		{
			SetGameObjectActiveSafe(dailyHeroBonusRoot, active: false);
		}
		if (visible)
		{
			RestoreLobbyButtonInteractableState();
		}
		UpdateDailyHeroBonusNotification();
	}

	private void RestoreLobbyButtonInteractableState()
	{
		if (returnToLobbyButton != null)
		{
			returnToLobbyButton.interactable = true;
		}
		if (battleShopButton != null)
		{
			battleShopButton.interactable = true;
		}
		if (battleTileInventoryButton != null)
		{
			battleTileInventoryButton.interactable = true;
		}
		if (weeklyRewardButton != null)
		{
			weeklyRewardButton.interactable = true;
		}
		if (dailyHeroBonusButton != null)
		{
			dailyHeroBonusButton.interactable = true;
		}
		if (openCharacterCarouselButton != null)
		{
			openCharacterCarouselButton.interactable = true;
		}
		SetMatchButtonInteractable(randomMatchButton, interactable: true);
		SetMatchButtonInteractable(rankedBattleButton, interactable: true);
		SetMatchButtonInteractable(duelChallengeButton, interactable: true);
		SetMatchButtonInteractable(localWifiBattleButton, interactable: true);
		SetMatchButtonInteractable(tournamentButton, !RouteLocalWifiSlotToTournament);
	}

	public void SetMatchButtonsSuppressedBySettings(bool suppressed)
	{
		matchButtonsSuppressedBySettings = suppressed;
		if (ShouldShowLobbyButtons())
		{
			SetButtonVisible(weeklyRewardButton, visible: false);
			UpdateDailyHeroBonusNotification();
			SetMatchButtonsVisible(!suppressed);
			if (!suppressed && !BattleLobbyUiCoordinator.HasModalOpen)
			{
				RequestRestoreLobbyLayoutAfterModal();
			}
		}
	}

	private void RequestRestoreLobbyLayoutAfterModal()
	{
		RestoreLobbyLayoutAfterModal();
		if (!isActiveAndEnabled)
		{
			return;
		}
		StopRestoreLobbyLayoutRoutine();
		restoreLobbyLayoutRoutine = StartCoroutine(RestoreLobbyLayoutOnNextFrame());
	}

	private IEnumerator RestoreLobbyLayoutOnNextFrame()
	{
		yield return null;
		Canvas.ForceUpdateCanvases();
		RestoreLobbyLayoutAfterModal();
		restoreLobbyLayoutRoutine = null;
	}

	private void RestoreLobbyLayoutAfterModal()
	{
		if (!ShouldShowLobbyButtons() || BattleLobbyUiCoordinator.HasModalOpen)
		{
			return;
		}
		EnsureBattleLobbyRuntimeCanvasActive();
		ApplyBattleLobbyButtonLayout();
		MahjongTileExchangeUI.RefreshBattleLobbyOpenButtonLayout();
		SetLobbyButtonsVisible(visible: true);
	}

	private void StopRestoreLobbyLayoutRoutine()
	{
		if (restoreLobbyLayoutRoutine == null)
		{
			return;
		}
		StopCoroutine(restoreLobbyLayoutRoutine);
		restoreLobbyLayoutRoutine = null;
	}

	private void SetMatchButtonsVisible(bool visible)
	{
		bool effectiveVisible = visible && !BattleLobbyUiCoordinator.HasModalOpen && !IsBattleTrainingLocked();
		SetButtonVisible(randomMatchButton, effectiveVisible);
		SetButtonVisible(rankedBattleButton, effectiveVisible);
		SetButtonVisible(duelChallengeButton, effectiveVisible);
		SetButtonVisible(localWifiBattleButton, effectiveVisible);
		SetButtonVisible(tournamentButton, effectiveVisible && !RouteLocalWifiSlotToTournament);
		SetMatchButtonInteractable(randomMatchButton, effectiveVisible);
		SetMatchButtonInteractable(rankedBattleButton, effectiveVisible);
		SetMatchButtonInteractable(duelChallengeButton, effectiveVisible);
		SetMatchButtonInteractable(localWifiBattleButton, effectiveVisible);
		SetMatchButtonInteractable(tournamentButton, effectiveVisible && !RouteLocalWifiSlotToTournament);
	}

	private void SetDownBarPanelVisible(bool visible)
	{
		if (downBarPanelRoot != null && downBarPanelRoot.activeSelf != visible)
		{
			downBarPanelRoot.SetActive(visible);
		}
	}

	private void SetTopMirrorBarPanelVisible(bool visible)
	{
		if (topMirrorBarPanelRoot != null && topMirrorBarPanelRoot.activeSelf != visible)
		{
			topMirrorBarPanelRoot.SetActive(visible);
		}
		if (topMirrorStatsRoot != null && topMirrorStatsRoot.activeSelf != visible)
		{
			topMirrorStatsRoot.SetActive(visible);
		}
		if (battleLobbyRankIcon != null && battleLobbyRankIcon.gameObject.activeSelf != visible)
		{
			battleLobbyRankIcon.gameObject.SetActive(visible);
		}
		if (battleLobbyRpIcon != null)
		{
			battleLobbyRpIcon.gameObject.SetActive(value: false);
		}
		if (battleLobbyExpIcon != null)
		{
			battleLobbyExpIcon.gameObject.SetActive(value: false);
		}
		if (battleLobbyEnergyIcon != null && battleLobbyEnergyIcon.gameObject.activeSelf != visible)
		{
			battleLobbyEnergyIcon.gameObject.SetActive(visible);
		}
		if (battleLobbyOzTileIcon != null && battleLobbyOzTileIcon.gameObject.activeSelf != visible)
		{
			battleLobbyOzTileIcon.gameObject.SetActive(visible);
		}
		if (battleLobbyAmetistIcon != null && battleLobbyAmetistIcon.gameObject.activeSelf != visible)
		{
			battleLobbyAmetistIcon.gameObject.SetActive(visible);
		}
		if (battleLobbyGoldIcon != null && battleLobbyGoldIcon.gameObject.activeSelf != visible)
		{
			battleLobbyGoldIcon.gameObject.SetActive(visible);
		}
	}

	private void ApplyBattleLobbyButtonLayout()
	{
		ApplyDownBarPanelLayout();
		ApplyTopMirrorBarPanelLayout();
		Vector2 battleLobbyCanvasSize = GetBattleLobbyCanvasSize();
		RectTransform battleLobbyCanvasRoot = FindActiveSceneCanvas()?.transform as RectTransform;
		EnsureBattleLobbyBottomButtonParent(returnToLobbyButton, battleLobbyCanvasRoot);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			EnsureBattleLobbyBottomButtonParent(openCharacterCarouselButton, battleLobbyCanvasRoot);
		}
		EnsureBattleLobbyBottomButtonParent(battleTileInventoryButton, battleLobbyCanvasRoot);
		EnsureBattleLobbyBottomButtonParent(battleShopButton, battleLobbyCanvasRoot);
		ApplyBattleLobbyBottomButton(returnToLobbyButton, BattleLobbyBottomButtonSlot.Return, battleLobbyCanvasSize);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			ApplyBattleLobbyBottomButton(openCharacterCarouselButton, BattleLobbyBottomButtonSlot.Character, battleLobbyCanvasSize);
		}
		ApplyBattleLobbyBottomButton(battleTileInventoryButton, BattleLobbyBottomButtonSlot.Inventory, battleLobbyCanvasSize);
		ApplyBattleLobbyBottomButton(battleShopButton, BattleLobbyBottomButtonSlot.Shop, battleLobbyCanvasSize);
		ApplyBattleLobbyTopTabButtonsLayout(battleLobbyCanvasSize);
		ApplyBattleLobbyMatchButtonLayout();
		SetButtonLabelText(returnToLobbyButton, BattleLobbyText("Платформа", "Platform", "Platform", "Plattform"));
		SetButtonLabelText(battleShopButton, BattleLobbyText("Магазин", battleShopButtonText, "Mağaza", "Shop"));
		SetButtonLabelText(battleTileInventoryButton, BattleLobbyText("Сумка", battleTileInventoryButtonText, "Çanta", "Taşche"));
		SetButtonLabelText(weeklyRewardButton, string.Empty);
		SetButtonLabelText(dailyHeroBonusButton, GameLocalization.Text("battle.daily.button"));
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			SetButtonLabelText(openCharacterCarouselButton, ResolveCharacterButtonText());
		}
		SetButtonLabelText(randomMatchButton, BattleLobbyText("Случайный бой", randomMatchButtonText, "Rastgele Maç", "Zufallskampf"));
		SetButtonLabelText(rankedBattleButton, BattleLobbyText("Ранговый бой", rankedBattleButtonText, "Rank Maçı", "Rangkampf"));
		SetButtonLabelText(duelChallengeButton, GameLocalization.Text("battle.duel.button"));
		SetButtonLabelText(localWifiBattleButton, RouteLocalWifiSlotToTournament ? BattleLobbyText("Турнир", tournamentButtonText, "Turnuva", "Turnier") : BattleLobbyText("Wi-Fi бой", localWifiBattleButtonText, "Wi-Fi Maçı", "Wi-Fi Kampf"));
		SetButtonLabelText(tournamentButton, BattleLobbyText("Турнир", tournamentButtonText, "Turnuva", "Turnier"));
		EnsureButtonTopCanvas(returnToLobbyButton);
		EnsureButtonTopCanvas(battleShopButton);
		EnsureButtonTopCanvas(battleTileInventoryButton);
		EnsureButtonTopCanvas(dailyHeroBonusButton);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			EnsureButtonTopCanvas(openCharacterCarouselButton);
		}
		if (topMirrorStatsRoot != null)
		{
			PlaceBattleLobbyStatsUnderBars(topMirrorStatsRoot.transform, null);
		}
	}

	private void ApplyBattleLobbyTopTabButtonsLayout(Vector2 canvasSize)
	{
		RectTransform topTabsRoot = EnsureBattleLobbyTopTabsRoot();
		ApplyBattleLobbyTopTabButton(dailyHeroBonusButton, 0, canvasSize, topTabsRoot);
		ApplyBattleLobbyTopTabButtonsByName("ButtonDailyHeroBonus", 0, canvasSize, topTabsRoot);
		ApplyBattleLobbyTopTabButtonsByName("ButtonBattleStoneAuction", 1, canvasSize, topTabsRoot);
		ApplyBattleLobbyTopTabButtonsByName("ButtonBattleStoneForge", 2, canvasSize, topTabsRoot);
		ApplyBattleLobbyTopTabButtonsByName("BtnOpenSettings", 3, canvasSize, topTabsRoot);
		ApplyDailyHeroNotificationBadgeLayout();
	}

	private void ApplyBattleLobbyTopTabButtonsByName(string objectName, int slotIndex, Vector2 canvasSize, RectTransform topTabsRoot)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return;
		}
		Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
		foreach (Button button in buttons)
		{
			if (button != null && button.gameObject != null && string.Equals(button.gameObject.name, objectName, StringComparison.Ordinal))
			{
				ApplyBattleLobbyTopTabButton(button, slotIndex, canvasSize, topTabsRoot);
			}
		}
	}

	private static void EnsureBattleLobbyBottomButtonParent(Button button, RectTransform canvasRoot)
	{
		RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
		if (buttonRect == null || canvasRoot == null || buttonRect.parent == canvasRoot)
		{
			return;
		}
		buttonRect.SetParent(canvasRoot, worldPositionStays: false);
		buttonRect.localScale = Vector3.one;
		buttonRect.localRotation = Quaternion.identity;
	}

	private void ApplyBattleLobbyTopTabButton(Button button, int slotIndex, Vector2 canvasSize, RectTransform topTabsRoot)
	{
		if (!(button == null))
		{
			RectTransform rectTransform = button.transform as RectTransform;
			if (!(rectTransform == null))
			{
				if (topTabsRoot != null && rectTransform.parent != topTabsRoot)
				{
					rectTransform.SetParent(topTabsRoot, worldPositionStays: false);
				}
				MainLobbyUiCoordinator.LayoutBattleLobbyTopTabButton(button, slotIndex, 4, canvasSize);
				EnsureButtonTopCanvas(button);
			}
		}
	}

	private static RectTransform EnsureBattleLobbyTopTabsRoot()
	{
		Canvas canvas = FindActiveSceneCanvas();
		RectTransform canvasRect = (canvas != null) ? (canvas.transform as RectTransform) : null;
		if (canvasRect == null)
		{
			return null;
		}
		Transform existing = canvasRect.Find(BattleLobbyTopTabsRootName);
		RectTransform root = existing as RectTransform;
		if (root == null)
		{
			GameObject obj = new GameObject(BattleLobbyTopTabsRootName, typeof(RectTransform));
			root = obj.GetComponent<RectTransform>();
			root.SetParent(canvasRect, worldPositionStays: false);
		}
		root.anchorMin = Vector2.zero;
		root.anchorMax = Vector2.one;
		root.pivot = new Vector2(0.5f, 0.5f);
		root.offsetMin = Vector2.zero;
		root.offsetMax = Vector2.zero;
		root.localScale = Vector3.one;
		root.SetAsLastSibling();
		return root;
	}

	private void ApplyDownBarPanelLayout()
	{
		RectTransform rectTransform = ((downBarPanelRoot != null) ? (downBarPanelRoot.transform as RectTransform) : null);
		if (!(rectTransform == null))
		{
			MainLobbyUiCoordinator.LayoutBattleLobbyPanel(rectTransform, topMirror: false);
		}
	}

	private void ApplyTopMirrorBarPanelLayout()
	{
		RectTransform rectTransform = ((topMirrorBarPanelRoot != null) ? (topMirrorBarPanelRoot.transform as RectTransform) : null);
		if (!(rectTransform == null))
		{
			MainLobbyUiCoordinator.LayoutBattleLobbyPanel(rectTransform, topMirror: true);
		}
	}

	private static void ApplyBattleLobbyButtonRect(Button button, Vector2 position, Vector2 size)
	{
		RectTransform rectTransform = ((button != null) ? button.GetComponent<RectTransform>() : null);
		if (!(rectTransform == null))
		{
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = position;
			rectTransform.sizeDelta = size;
		}
	}

	private void ApplyBattleLobbyMatchButtonLayout()
	{
		Vector2 battleLobbyCanvasSize = GetBattleLobbyCanvasSize();
		MainLobbyUiCoordinator.LayoutBattleLobbyMatchButton(rankedBattleButton, BattleLobbyMatchButtonSlot.Ranked, battleLobbyCanvasSize, rankedBattleButtonSize);
		MainLobbyUiCoordinator.LayoutBattleLobbyMatchButton(randomMatchButton, BattleLobbyMatchButtonSlot.Random, battleLobbyCanvasSize, randomMatchButtonSize);
		MainLobbyUiCoordinator.LayoutBattleLobbyMatchButton(duelChallengeButton, BattleLobbyMatchButtonSlot.Duel, battleLobbyCanvasSize, duelChallengeButtonSize);
		MainLobbyUiCoordinator.LayoutBattleLobbyMatchButton(localWifiBattleButton, BattleLobbyMatchButtonSlot.LocalWifi, battleLobbyCanvasSize, localWifiBattleButtonSize);
		if (!RouteLocalWifiSlotToTournament)
		{
			MainLobbyUiCoordinator.LayoutBattleLobbyMatchButton(tournamentButton, BattleLobbyMatchButtonSlot.Tournament, battleLobbyCanvasSize, tournamentButtonSize);
		}
		else
		{
			SetButtonVisible(tournamentButton, visible: false);
		}
	}

	private static Vector2 GetBattleLobbyCanvasSize()
	{
		Canvas canvas = FindActiveSceneCanvas();
		RectTransform rectTransform = ((canvas != null) ? (canvas.transform as RectTransform) : null);
		if (rectTransform != null && rectTransform.rect.width > 1f && rectTransform.rect.height > 1f)
		{
			return rectTransform.rect.size;
		}
		return new Vector2(2400f, 1080f);
	}

	private void SetLobbyHudVisibleWhileCarousel(bool visible)
	{
		bool flag = visible && ShouldShowLobbyButtons();
		SetLobbyButtonsVisible(flag);
		SetCharacterEntryButtonVisible(flag);
		if (battleProgressRoot != null)
		{
			SetGameObjectActiveSafe(battleProgressRoot, active: false);
		}
		if (battleShopRoot != null && !flag)
		{
			SetGameObjectActiveSafe(battleShopRoot, active: false);
		}
		if (weeklyRewardRoot != null && !flag)
		{
			SetGameObjectActiveSafe(weeklyRewardRoot, active: false);
		}
		if (dailyHeroBonusRoot != null && !flag)
		{
			SetGameObjectActiveSafe(dailyHeroBonusRoot, active: false);
		}
		SetSceneObjectsActiveByName("OzTileExchangeButton", flag);
		if (!flag)
		{
			SetSceneObjectsActiveByName("OzTileExchangeOverlay", active: false);
		}
	}

	private static void SetBattleLobbyCharacterPreviewSuppressed(bool suppressed)
	{
		BattleLobbyChar battleLobbyChar = UnityEngine.Object.FindAnyObjectByType<BattleLobbyChar>(FindObjectsInactive.Include);
		if (battleLobbyChar != null)
		{
			battleLobbyChar.SetSuppressedByCharacterCarousel(suppressed);
		}
	}

	private static void SetCharacterCarouselSceneEffectsSuppressed(bool suppressed)
	{
		SetSceneObjectsActiveByName("FogLayer", !suppressed);
		SetSceneBehavioursEnabledByTypeName("ProceduralFogUI", !suppressed);
		SetSceneBehavioursEnabledByTypeName("FallingFX", !suppressed);
	}

	private static void SetSceneObjectsActiveByName(string objectName, bool active)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return;
		}
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (!(gameObject == null) && gameObject.scene.IsValid() && string.Equals(gameObject.name, objectName, StringComparison.Ordinal))
			{
				SetGameObjectActiveSafe(gameObject, active);
			}
		}
	}

	private static void SetSceneBehavioursEnabledByTypeName(string typeName, bool enabled)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return;
		}
		Behaviour[] array = Resources.FindObjectsOfTypeAll<Behaviour>();
		foreach (Behaviour behaviour in array)
		{
			if (!(behaviour == null) && !(behaviour.gameObject == null) && behaviour.gameObject.scene.IsValid() && string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
			{
				behaviour.enabled = enabled;
			}
		}
	}

	private void ResolveLobbyButtonReferences()
	{
		if (returnToLobbyButton == null)
		{
			returnToLobbyButton = FindButtonByName("ButtonReturnToLobby");
		}
		if (battleShopButton == null)
		{
			battleShopButton = FindButtonByName("ButtonBattleShop");
		}
		if (battleTileInventoryButton == null)
		{
			battleTileInventoryButton = FindButtonByName("ButtonBattleTileInventory");
		}
		if (weeklyRewardButton == null)
		{
			weeklyRewardButton = FindButtonByName("ButtonWeeklyRewards");
		}
		if (randomMatchButton == null)
		{
			randomMatchButton = FindButtonByName("ButtonRandomMatch");
		}
		if (rankedBattleButton == null)
		{
			rankedBattleButton = FindButtonByName("ButtonRankedBattle");
		}
		if (duelChallengeButton == null)
		{
			duelChallengeButton = FindButtonByName("ButtonDuelChallenge");
		}
		if (localWifiBattleButton == null)
		{
			localWifiBattleButton = FindButtonByName("ButtonLocalWifiBattle");
		}
		if (tournamentButton == null)
		{
			tournamentButton = FindButtonByName("ButtonTournament");
		}
	}

	private static void CleanupStaleBattleLobbyOverlays()
	{
		CleanupObjectsByName("RandomBattleLobbyOverlay");
		CleanupObjectsByName("RankedLeagueSelectOverlay");
		CleanupObjectsByName("OnlineRankedBattleLobbyOverlay");
		CleanupObjectsByName("LocalWifiBattleLobbyOverlay");
		CleanupObjectsByName("DuelChallengeOverlay");
		CleanupObjectsByName("TournamentLobbyOverlay");
		CleanupObjectsByName("BattleTutorialGateOverlay");
		CleanupObjectsByName("DuelIncomingIndicator");
		CleanupObjectsByName("RandomBattleCanvas");
		CleanupObjectsByName("OnlineRankedCanvas");
		CleanupObjectsByName("LocalWifiCanvas");
		CleanupObjectsByName("BattleShopOverlay");
		CleanupObjectsByName("BattleTileInventoryWindow");
		CleanupObjectsByName("WeeklyRewardOverlay");
		CleanupObjectsByName("BattleResultPanel");
		CleanupObjectsByName("BattleResultWindow");
		CleanupObjectsByName("OzTileExchangeOverlay");
		DestroyComponentsByType<RandomBattleLobbyUI>();
		DestroyComponentsByType<RankedLeagueSelectUI>();
		DestroyComponentsByType<OnlineRankedBattleLobbyUI>();
		DestroyComponentsByType<LocalWifiBattleLobbyUI>();
		DestroyComponentsByType<DuelChallengeLobbyUI>();
		DestroyComponentsByType<TournamentLobbyUI>();
	}

	private static void CleanupObjectsByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return;
		}
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (!(gameObject == null) && string.Equals(gameObject.name, objectName, StringComparison.Ordinal) && gameObject.scene.IsValid())
			{
				gameObject.SetActive(value: false);
				DestroyGameObject(gameObject, immediate: true);
			}
		}
	}

	private static void DestroyComponentsByType<T>() where T : Component
	{
		T[] array = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
		foreach (T val in array)
		{
			if (!(val == null) && !(val.gameObject == null))
			{
				val.gameObject.SetActive(value: false);
				DestroyGameObject(val.gameObject, immediate: true);
			}
		}
	}

	private void SetCharacterEntryPointVisible(bool visible)
	{
		SetCharacterEntryButtonVisible(visible);
		if (characterCarouselRoot != null)
		{
			SetGameObjectActiveSafe(characterCarouselRoot, active: false);
		}
	}

	private void SetCharacterEntryButtonVisible(bool visible)
	{
		if (openCharacterCarouselButton != null && openCharacterCarouselButton.gameObject != null)
		{
			SetButtonVisible(openCharacterCarouselButton, visible);
		}
		if (!IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			HideGeneratedOpenCharacterButton();
		}
	}

	private static bool IsExplicitCharacterButton(Button button)
	{
		if (button != null && button.gameObject != null)
		{
			return string.Equals(button.gameObject.name, "OpenCharacterCarouselButton", StringComparison.Ordinal);
		}
		return false;
	}

	private static void HideGeneratedOpenCharacterButton()
	{
		GameObject gameObject = FindObjectByName("OpenCharacterCarouselButton");
		if (!(gameObject == null))
		{
			Button component = gameObject.GetComponent<Button>();
			if (component != null)
			{
				SetButtonVisible(component, visible: false);
			}
			else if (gameObject.activeSelf)
			{
				SetGameObjectActiveSafe(gameObject, active: false);
			}
		}
	}

	private static void SetButtonVisible(Button button, bool visible)
	{
		if (!(button == null) && !(button.gameObject == null))
		{
			if (!button.gameObject.activeSelf)
			{
				button.gameObject.SetActive(value: true);
			}
			CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
			}
			canvasGroup.alpha = (visible ? 1f : 0f);
			canvasGroup.interactable = visible;
			canvasGroup.blocksRaycasts = visible;
			button.interactable = visible;
		}
	}

	private static void SetGameObjectActiveSafe(GameObject target, bool active)
	{
		if (!(target == null))
		{
			if (!active)
			{
				ClearSelectedUiInside(target.transform);
			}
			if (target.activeSelf != active)
			{
				target.SetActive(active);
			}
		}
	}

	private static void ClearSelectedUiInside(Transform root)
	{
		EventSystem current = EventSystem.current;
		if (!(current == null) && !(root == null))
		{
			GameObject currentSelectedGameObject = current.currentSelectedGameObject;
			if (currentSelectedGameObject != null && currentSelectedGameObject.transform != null && currentSelectedGameObject.transform.IsChildOf(root))
			{
				current.SetSelectedGameObject(null);
			}
		}
	}

	private static Canvas FindActiveSceneCanvas()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		Canvas orCreateBattleLobbyRuntimeCanvas = GetOrCreateBattleLobbyRuntimeCanvas(activeScene);
		if (orCreateBattleLobbyRuntimeCanvas != null)
		{
			SetGameObjectActiveSafe(orCreateBattleLobbyRuntimeCanvas.gameObject, active: true);
			return orCreateBattleLobbyRuntimeCanvas;
		}
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (canvas != null && canvas.gameObject.scene == activeScene)
			{
				SetGameObjectActiveSafe(canvas.gameObject, active: true);
				return canvas;
			}
		}
		return null;
	}

	private static void EnsureBattleLobbyRuntimeCanvasActive()
	{
		Canvas canvas = FindBattleLobbyRuntimeCanvas(SceneManager.GetActiveScene());
		if (canvas != null)
		{
			SetGameObjectActiveSafe(canvas.gameObject, active: true);
		}
	}

	private static void ApplyBattleLobbyBottomButton(Button button, BattleLobbyBottomButtonSlot slot, Vector2 canvasSize)
	{
		MainLobbyUiCoordinator.LayoutBattleLobbyBottomButton(button, slot, canvasSize, ForcedBottomActionButtonSize);
	}

	private static Canvas GetOrCreateBattleLobbyRuntimeCanvas(Scene scene)
	{
		Canvas canvas = FindBattleLobbyRuntimeCanvas(scene);
		if (canvas != null)
		{
			return canvas;
		}
		GameObject obj = new GameObject("BattleLobbyRuntimeHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		SceneManager.MoveGameObjectToScene(obj, scene);
		Canvas component = obj.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.overrideSorting = true;
		component.sortingOrder = 29999;
		CanvasScaler component2 = obj.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(2400f, 1080f);
		component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		component2.matchWidthOrHeight = 0.5f;
		RectTransform component3 = obj.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		return component;
	}

	private static Canvas FindBattleLobbyRuntimeCanvas(Scene scene)
	{
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (canvas != null && canvas.gameObject.scene == scene && string.Equals(canvas.gameObject.name, "BattleLobbyRuntimeHudCanvas", StringComparison.Ordinal))
			{
				return canvas;
			}
		}
		return null;
	}

	private void SetButtonLabelText(Button button, string text)
	{
		TMP_Text tMP_Text = ((button != null) ? button.GetComponentInChildren<TMP_Text>(includeInactive: true) : null);
		if (tMP_Text != null)
		{
			tMP_Text.text = text;
			ApplyBattleLobbyFontToText(tMP_Text);
		}
	}

	private static Button FindRuntimeButtonByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		Canvas canvas = FindBattleLobbyRuntimeCanvas(SceneManager.GetActiveScene());
		if (canvas == null)
		{
			return null;
		}
		Button[] componentsInChildren = canvas.GetComponentsInChildren<Button>(includeInactive: true);
		foreach (Button button in componentsInChildren)
		{
			if (button != null && string.Equals(button.gameObject.name, objectName, StringComparison.Ordinal))
			{
				return button;
			}
		}
		return null;
	}

	private string ResolveCharacterButtonText()
	{
		return BattleLobbyText("Персонаж", openCharacterButtonText, "Karakter", "Charakter");
	}

	private static string BattleLobbyText(string russian, string english, string turkish, string german = null)
	{
		switch ((!(AppSettings.I != null)) ? GameLanguage.Turkish : AppSettings.I.Language)
		{
		case GameLanguage.Russian:
			if (!string.IsNullOrWhiteSpace(russian))
			{
				return russian;
			}
			return english;
		case GameLanguage.Turkish:
			if (!string.IsNullOrWhiteSpace(turkish))
			{
				return turkish;
			}
			return english;
		case GameLanguage.German:
			if (!string.IsNullOrWhiteSpace(german))
			{
				return german;
			}
			return english;
		default:
			if (!string.IsNullOrWhiteSpace(english))
			{
				return english;
			}
			return russian;
		}
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

	private Vector2 ResolveWeeklyRewardButtonPosition()
	{
		return ForcedWeeklyRewardButtonPosition;
	}

	private Vector2 ResolveCharacterButtonPosition()
	{
		return ForcedCharacterButtonPosition;
	}

	private Vector2 ResolveCharacterButtonSize()
	{
		return ForcedCharacterButtonSize;
	}

	private void EnsureDownBarPanel()
	{
		if (downBarPanelRoot != null)
		{
			ApplyDownBarPanelLayout();
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (!(canvas == null))
		{
			GameObject gameObject = new GameObject("DownBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
			downBarPanelRoot = gameObject;
			Image component = gameObject.GetComponent<Image>();
			component.sprite = LoadDownBarPanelSprite();
			component.type = Image.Type.Simple;
			component.preserveAspect = false;
			component.color = Color.white;
			component.raycastTarget = false;
			ApplyDownBarPanelLayout();
		}
	}

	private void EnsureTopMirrorBarPanel()
	{
		if (topMirrorBarPanelRoot != null)
		{
			ApplyTopMirrorBarPanelLayout();
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (!(canvas == null))
		{
			GameObject gameObject = new GameObject("TopMirrorBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
			topMirrorBarPanelRoot = gameObject;
			Image component = gameObject.GetComponent<Image>();
			component.sprite = LoadDownBarPanelSprite();
			component.type = Image.Type.Simple;
			component.preserveAspect = false;
			component.color = Color.white;
			component.raycastTarget = false;
			ApplyTopMirrorBarPanelLayout();
		}
	}

	private static Sprite LoadDownBarPanelSprite()
	{
		return LoadResourceSprite("Mahjong/Sprites/BattleLobby/downbarpanel");
	}

	private static Sprite LoadBattleLobbyEnergyIcon()
	{
		if (cachedBattleLobbyEnergyIconSprite != null)
		{
			return cachedBattleLobbyEnergyIconSprite;
		}
		cachedBattleLobbyEnergyIconSprite = LoadResourceSprite("Mahjong/Sprites/BattleLobby/EnergyIconTopBar");
		return cachedBattleLobbyEnergyIconSprite;
	}

	private static Sprite LoadBattleLobbyAmetistIcon()
	{
		if (cachedBattleLobbyAmetistIconSprite != null)
		{
			return cachedBattleLobbyAmetistIconSprite;
		}
		cachedBattleLobbyAmetistIconSprite = LoadResourceSprite("Mahjong/Sprites/Money/OzAmetist");
		return cachedBattleLobbyAmetistIconSprite;
	}

	private static Sprite LoadBattleLobbyGoldIcon()
	{
		if (cachedBattleLobbyGoldIconSprite != null)
		{
			return cachedBattleLobbyGoldIconSprite;
		}
		cachedBattleLobbyGoldIconSprite = LoadResourceSprite(BattleLobbyGoldIconResourcePath);
		return cachedBattleLobbyGoldIconSprite;
	}

	private static Sprite LoadBattleLobbyExpIcon()
	{
		if (cachedBattleLobbyExpIconSprite != null)
		{
			return cachedBattleLobbyExpIconSprite;
		}
		cachedBattleLobbyExpIconSprite = LoadResourceSprite("Mahjong/Sprites/BattleLobby/ExpIconTopBar");
		return cachedBattleLobbyExpIconSprite;
	}

	private static Sprite LoadBattleLobbyRpIcon()
	{
		if (cachedBattleLobbyRpIconSprite != null)
		{
			return cachedBattleLobbyRpIconSprite;
		}
		cachedBattleLobbyRpIconSprite = LoadResourceSprite("Mahjong/Sprites/BattleLobby/RPIconTopBar");
		return cachedBattleLobbyRpIconSprite;
	}

	private static Sprite LoadBattleLobbyOzTileIcon()
	{
		if (cachedBattleLobbyOzTileIconSprite != null)
		{
			return cachedBattleLobbyOzTileIconSprite;
		}
		cachedBattleLobbyOzTileIconSprite = LoadResourceSprite("Mahjong/Sprites/BattleLobby/OzTileTopBar");
		return cachedBattleLobbyOzTileIconSprite;
	}

	private static Sprite LoadResourceSprite(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}
		Sprite sprite = Resources.Load<Sprite>(resourcePath);
		if (sprite != null)
		{
			return sprite;
		}
		Sprite[] array = Resources.LoadAll<Sprite>(resourcePath);
		if (array != null && array.Length != 0)
		{
			return array[0];
		}
		Texture2D texture2D = Resources.Load<Texture2D>(resourcePath);
		if (texture2D == null)
		{
			return null;
		}
		return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
	}

	private static Sprite LoadBattleLobbyStatsWindowSprite()
	{
		if (cachedBattleLobbyStatsWindowSprite != null)
		{
			return cachedBattleLobbyStatsWindowSprite;
		}
		const string statsWindowPath = "Mahjong/Sprites/BattleLobbyUI/BattleWindow";
		Texture2D texture2D = Resources.Load<Texture2D>(statsWindowPath);
		if (texture2D == null)
		{
			cachedBattleLobbyStatsWindowSprite = LoadResourceSprite(statsWindowPath);
			return cachedBattleLobbyStatsWindowSprite;
		}
		Rect rect = new Rect(8f, 8f, Mathf.Max(1f, texture2D.width - 16f), Mathf.Max(1f, texture2D.height - 16f));
		cachedBattleLobbyStatsWindowSprite = Sprite.Create(texture2D, rect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		return cachedBattleLobbyStatsWindowSprite;
	}

	private static void EnsureButtonTopCanvas(Button button)
	{
		if (!(button == null) && !(button.gameObject == null))
		{
			Canvas canvas = button.GetComponent<Canvas>();
			if (canvas == null)
			{
				canvas = button.gameObject.AddComponent<Canvas>();
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = 30000;
			if (button.GetComponent<GraphicRaycaster>() == null)
			{
				button.gameObject.AddComponent<GraphicRaycaster>();
			}
		}
	}

	private static void EnsureEventSystem()
	{
		BattleLobbyUiCoordinator.EnsureInputReady();
		EventSystemInputModeGuard.EnsureCompatibleEventSystems();
	}

	private void ApplyBattleLobbyVisuals()
	{
		if (ShouldShowLobbyButtons())
		{
			EnsureTopMirrorBarPanel();
			ApplyBattleLobbyTopBarVisual();
			ApplyBattleLobbyTypography();
			ApplyBattleLobbyMatchButtonVisual(randomMatchButton);
			ApplyBattleLobbyMatchButtonVisual(rankedBattleButton);
			ApplyBattleLobbyMatchButtonVisual(duelChallengeButton);
			ApplyBattleLobbyMatchButtonVisual(localWifiBattleButton);
			ApplyBattleLobbyMatchButtonVisual(tournamentButton);
			ApplyBattleLobbyUtilityButtonVisual(returnToLobbyButton);
			ApplyBattleLobbyUtilityButtonVisual(battleShopButton);
			ApplyBattleLobbyUtilityButtonVisual(battleTileInventoryButton);
			ApplyBattleLobbyUtilityButtonVisual(weeklyRewardButton);
			ApplyBattleLobbyRightStackUtilityButtonVisual(dailyHeroBonusButton);
			if (IsExplicitCharacterButton(openCharacterCarouselButton))
			{
				ApplyBattleLobbyUtilityButtonVisual(openCharacterCarouselButton);
			}
			ApplyBattleLobbyFontToActiveSceneCanvas();
		}
	}

	private void ApplyBattleLobbyTopBarVisual()
	{
		GameObject gameObject = FindObjectByName("TopBar");
		if (!(gameObject == null))
		{
			ApplyBattleLobbyFontToDescendantTexts(gameObject.transform);
			Image image = ResolveBattleLobbyTopBarImage(gameObject.transform);
			if (image != null)
			{
				image.enabled = false;
				image.raycastTarget = false;
			}
			ApplyBattleLobbyTopBarTextLayout(gameObject.transform);
			RefreshBattleLobbyTopBarValues();
		}
	}

	private void RefreshBattleLobbyTopBarValues()
	{
		GameObject gameObject = FindObjectByName("TopBar");
		if (gameObject == null)
		{
			return;
		}
		TMP_Text[] array = EnsureBattleLobbyTopBarTexts(gameObject.transform);
		if (array == null || array.Length == 0)
		{
			return;
		}
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		playerProfile?.EnsureData();
		MahjongBattleData mahjongBattleData = ((playerProfile != null && playerProfile.Mahjong != null) ? playerProfile.Mahjong.Battle : null);
		RefreshBattleLobbyRankIcon(gameObject.transform, mahjongBattleData);
		string text = AllianceIdentityFormatter.FormatOwnName(playerProfile, GameLocalization.Text("battle.common.player"));
		string arg = ((mahjongBattleData != null && !string.IsNullOrWhiteSpace(mahjongBattleData.RankTier)) ? LocalizeRankTier(mahjongBattleData.RankTier) : GameLocalization.Text("battle.rank.bronze"));
		int num = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.RankPoints) : 0);
		int num2 = ((mahjongBattleData == null) ? 1 : Mathf.Max(1, mahjongBattleData.Level));
		int num3 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Experience) : 0);
		int num4 = ((mahjongBattleData != null) ? Mathf.Max(1, mahjongBattleData.GetExperienceRequiredForNextLevel()) : 100);
		int num5 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Wins) : 0);
		int num6 = ((mahjongBattleData != null) ? Mathf.Max(0, mahjongBattleData.Losses) : 0);
		int num7 = ((mahjongBattleData != null) ? Mathf.Clamp(mahjongBattleData.WinRatePercent, 0, 100) : 0);
		string text2 = (EnergyService.HasInfiniteEnergy() ? "INF" : $"{EnergyService.CurrentEnergy}/{EnergyService.CurrentMaxEnergy}");
		int num8 = ((CurrencyService.I != null) ? CurrencyService.I.GetOzTile() : ((playerProfile != null && playerProfile.Currencies != null) ? Mathf.Max(0, playerProfile.Currencies.OzTile) : 0));
		int num9 = ((CurrencyService.I != null) ? CurrencyService.I.GetOzAmetist() : ((playerProfile != null && playerProfile.Currencies != null) ? Mathf.Max(0, playerProfile.Currencies.OzAmetist) : 0));
		int num10 = ((CurrencyService.I != null) ? CurrencyService.I.GetOzAltin() : ((playerProfile != null && playerProfile.Currencies != null) ? Mathf.Max(0, playerProfile.Currencies.OzAltin) : 0));
		BattleCharacterDatabase.BattleCharacterData battleCharacterData = (BattleCharacterSelectionService.HasInstance ? BattleCharacterSelectionService.Instance.GetSelectedCharacter() : null);
		string[] array2 = new string[12]
		{
			CompactNumberFormatter.FormatCurrency(num10),
			CompactNumberFormatter.FormatCurrency(num9),
			CompactNumberFormatter.FormatCurrency(num8),
			text,
			$"{arg} RP {num}",
			GameLocalization.Format("battle.lobby.top_level", num2),
			$"EXP {num3}/{num4}",
			GameLocalization.Format("battle.lobby.top_wl", num5, num6) + "   " + GameLocalization.Format("battle.lobby.top_winrate", num7),
			string.Empty,
			text2,
			BuildBattleLobbyCharacterClassLine(battleCharacterData),
			BuildBattleLobbyCharacterStatsLine(battleCharacterData)
		};
		for (int i = 0; i < array.Length; i++)
		{
			TMP_Text tMP_Text = array[i];
			if (!(tMP_Text == null))
			{
				if (i < array2.Length)
				{
					tMP_Text.text = array2[i];
					tMP_Text.gameObject.SetActive(value: true);
				}
				else
				{
					tMP_Text.gameObject.SetActive(value: false);
				}
			}
		}
		ApplyBattleLobbyTopBarTextLayout(gameObject.transform);
		BindBattleLobbyTopBarTooltips(gameObject.transform, array);
	}

	private void RefreshBattleLobbyRankIcon(Transform topBarTransform, MahjongBattleData battle)
	{
		Image image = EnsureBattleLobbyRankIcon(topBarTransform);
		if (!(image == null))
		{
			string rankTier = ((battle != null) ? battle.RankTier : "Bronze");
			int rankPoints = ((battle != null) ? Mathf.Max(0, battle.RankPoints) : 0);
			Sprite sprite = (image.sprite = RankedLeagueVisuals.LoadLeagueIcon(RankedLeagueVisuals.ResolveLeagueId(rankTier, rankPoints)));
			image.enabled = sprite != null;
			image.color = Color.white;
			image.preserveAspect = true;
			image.gameObject.SetActive(topMirrorStatsRoot == null || topMirrorStatsRoot.activeSelf);
			RefreshBattleLobbyResourceIcons(topBarTransform);
		}
	}

	private void RefreshBattleLobbyResourceIcons(Transform topBarTransform)
	{
		Transform transform = EnsureTopMirrorStatsRoot(topBarTransform);
		if (!(transform == null))
		{
			DisableTopBarIcon(transform, "BattleTopBarRpIcon", ref battleLobbyRpIcon);
			DisableTopBarIcon(transform, "BattleTopBarExpIcon", ref battleLobbyExpIcon);
			float scale = ResolveBattleLobbyStatsPanelScale();
			battleLobbyGoldIcon = EnsureTopBarIcon(transform, battleLobbyGoldIcon, "BattleTopBarGoldIcon", LoadBattleLobbyGoldIcon(), new Vector2(-210f, BattleLobbyCurrencyRowY) * scale, new Vector2(48f, 48f) * scale);
			battleLobbyAmetistIcon = EnsureTopBarIcon(transform, battleLobbyAmetistIcon, "BattleTopBarAmetistIcon", LoadBattleLobbyAmetistIcon(), new Vector2(-50f, BattleLobbyCurrencyRowY) * scale, new Vector2(48f, 48f) * scale);
			battleLobbyOzTileIcon = EnsureTopBarIcon(transform, battleLobbyOzTileIcon, "BattleTopBarOzTileIcon", LoadBattleLobbyOzTileIcon(), new Vector2(110f, BattleLobbyCurrencyRowY) * scale, new Vector2(48f, 48f) * scale);
			EnsureBattleLobbyCurrencyDivider(transform, scale);
			battleLobbyEnergyIcon = EnsureTopBarIcon(transform, battleLobbyEnergyIcon, "BattleTopBarEnergyIcon", LoadBattleLobbyEnergyIcon(), new Vector2(-74f, -342f) * scale, new Vector2(72f, 72f) * scale);
			bool flag = topMirrorStatsRoot == null || topMirrorStatsRoot.activeSelf;
			if (battleLobbyRpIcon != null)
			{
				battleLobbyRpIcon.gameObject.SetActive(value: false);
			}
			if (battleLobbyExpIcon != null)
			{
				battleLobbyExpIcon.gameObject.SetActive(value: false);
			}
			if (battleLobbyEnergyIcon != null)
			{
				battleLobbyEnergyIcon.gameObject.SetActive(flag);
			}
			if (!flag)
			{
				SetTopBarTooltipVisible(visible: false, null, Vector2.zero);
			}
			if (battleLobbyOzTileIcon != null)
			{
				battleLobbyOzTileIcon.gameObject.SetActive(flag);
			}
			if (battleLobbyAmetistIcon != null)
			{
				battleLobbyAmetistIcon.gameObject.SetActive(flag);
			}
			if (battleLobbyGoldIcon != null)
			{
				battleLobbyGoldIcon.gameObject.SetActive(flag);
			}
		}
	}

	private void DisableTopBarIcon(Transform parent, string objectName, ref Image icon)
	{
		if (icon == null && parent != null)
		{
			Transform transform = parent.Find(objectName);
			icon = ((transform != null) ? transform.GetComponent<Image>() : null);
		}
		if (icon != null)
		{
			icon.raycastTarget = false;
			icon.gameObject.SetActive(value: false);
		}
	}

	private Image EnsureTopBarIcon(Transform parent, Image current, string objectName, Sprite sprite, Vector2 position, Vector2 size)
	{
		if (parent == null)
		{
			return current;
		}
		if (current == null)
		{
			Transform transform = parent.Find(objectName);
			current = ((transform != null) ? transform.GetComponent<Image>() : null);
		}
		if (current == null)
		{
			GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			obj.layer = parent.gameObject.layer;
			obj.transform.SetParent(parent, worldPositionStays: false);
			current = obj.GetComponent<Image>();
		}
		else if (current.transform.parent != parent)
		{
			current.transform.SetParent(parent, worldPositionStays: false);
		}
		RectTransform rectTransform = current.rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = position;
		rectTransform.sizeDelta = size;
		rectTransform.localScale = Vector3.one;
		current.sprite = sprite;
		current.enabled = sprite != null;
		current.color = Color.white;
		current.preserveAspect = true;
		current.raycastTarget = false;
		current.transform.SetAsLastSibling();
		return current;
	}

	private static void EnsureBattleLobbyCurrencyDivider(Transform parent, float scale)
	{
		if (parent == null)
		{
			return;
		}
		EnsureBattleLobbyStatsDivider(parent, "BattleTopBarCurrencyDivider", BattleLobbyCurrencyDividerY, scale);
		EnsureBattleLobbyStatsDivider(parent, "BattleTopBarNicknameDivider", BattleLobbyNicknameDividerY, scale);
		EnsureBattleLobbyStatsDivider(parent, "BattleTopBarBattleRecordDivider", BattleLobbyBattleRecordDividerY, scale);
		EnsureBattleLobbyStatsDivider(parent, "BattleTopBarRankDivider", BattleLobbyRankDividerY, scale);
	}

	private static void EnsureBattleLobbyStatsDivider(Transform parent, string objectName, float y, float scale)
	{
		Transform existing = parent.Find(objectName);
		Image divider = existing != null ? existing.GetComponent<Image>() : null;
		if (divider == null)
		{
			GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			obj.layer = parent.gameObject.layer;
			obj.transform.SetParent(parent, worldPositionStays: false);
			divider = obj.GetComponent<Image>();
		}
		if (cachedBattleShopDividerSprite == null)
		{
			const string resourcePath = "Mahjong/Sprites/BattleLobbyUI/Divider";
			Texture2D texture = Resources.Load<Texture2D>(resourcePath);
			cachedBattleShopDividerSprite = texture != null
				? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect)
				: LoadResourceSprite(resourcePath);
		}
		RectTransform rect = divider.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(0f, y) * scale;
		float dividerHeight = cachedBattleShopDividerSprite != null && cachedBattleShopDividerSprite.rect.width > 1f
			? 430f * (cachedBattleShopDividerSprite.rect.height / cachedBattleShopDividerSprite.rect.width)
			: 27f;
		rect.sizeDelta = new Vector2(430f, dividerHeight) * scale;
		rect.localScale = Vector3.one;
		divider.sprite = cachedBattleShopDividerSprite;
		divider.type = Image.Type.Simple;
		divider.preserveAspect = true;
		divider.color = Color.white;
		divider.raycastTarget = false;
		divider.gameObject.SetActive(value: true);
		divider.transform.SetAsFirstSibling();

		Transform highlightTransform = divider.transform.Find("BrightnessHighlight");
		if (highlightTransform != null)
		{
			highlightTransform.gameObject.SetActive(value: false);
		}
		string[] obsoleteCoreLineNames = { "CoreLineLeft", "CoreLineRight" };
		for (int i = 0; i < obsoleteCoreLineNames.Length; i++)
		{
			Transform obsoleteCoreLine = divider.transform.Find(obsoleteCoreLineNames[i]);
			if (obsoleteCoreLine != null)
			{
				obsoleteCoreLine.gameObject.SetActive(value: false);
			}
		}
	}

	private void BindBattleLobbyTopBarTooltips(Transform topBarTransform, TMP_Text[] texts)
	{
		Transform transform = EnsureTopMirrorStatsRoot(topBarTransform);
		if (!(transform == null))
		{
			EnsureTopBarTooltip(transform);
			if (texts != null)
			{
				BindTopBarTooltip(texts, 0, BuildTopBarTooltipText(0), new Vector2(-180f, BattleLobbyCurrencyRowY));
				BindTopBarTooltip(texts, 1, BuildTopBarTooltipText(1), new Vector2(-20f, BattleLobbyCurrencyRowY));
				BindTopBarTooltip(texts, 2, BuildTopBarTooltipText(2), new Vector2(140f, BattleLobbyCurrencyRowY));
				BindTopBarTooltip(texts, 3, BuildTopBarTooltipText(3), new Vector2(0f, 282f));
				BindTopBarTooltip(texts, 4, BuildTopBarTooltipText(4), new Vector2(0f, 158f));
				BindTopBarTooltip(texts, 5, BuildTopBarTooltipText(5), new Vector2(134f, 96f));
				BindTopBarTooltip(texts, 6, BuildTopBarTooltipText(6), new Vector2(0f, 46f));
				BindTopBarTooltip(texts, 7, BuildTopBarTooltipText(7), new Vector2(0f, 216f));
				BindTopBarTooltip(texts, 9, BuildTopBarTooltipText(9), new Vector2(0f, -342f));
				BindTopBarTooltip(texts, 10, BuildTopBarTooltipText(10), new Vector2(0f, 96f));
				BindTopBarTooltip(texts, 11, BuildTopBarTooltipText(11), new Vector2(0f, -118f));
			}
			BindTopBarTooltip(battleLobbyGoldIcon, BuildTopBarTooltipText(0), new Vector2(-180f, BattleLobbyCurrencyRowY));
			BindTopBarTooltip(battleLobbyAmetistIcon, BuildTopBarTooltipText(1), new Vector2(-20f, BattleLobbyCurrencyRowY));
			BindTopBarTooltip(battleLobbyOzTileIcon, BuildTopBarTooltipText(2), new Vector2(140f, BattleLobbyCurrencyRowY));
			BindTopBarTooltip(battleLobbyRankIcon, BuildTopBarTooltipText(4), new Vector2(-155f, 282f));
			BindTopBarTooltip(battleLobbyEnergyIcon, BuildTopBarTooltipText(9), new Vector2(0f, -342f));
		}
	}

	private void BindTopBarTooltip(TMP_Text[] texts, int index, string tooltip, Vector2 position)
	{
		if (texts != null && index >= 0 && index < texts.Length)
		{
			BindTopBarTooltip(texts[index], tooltip, position);
		}
	}

	private void BindTopBarTooltip(Graphic target, string tooltip, Vector2 position)
	{
		if (!(target == null) && !string.IsNullOrWhiteSpace(tooltip))
		{
			target.raycastTarget = true;
			EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
			if (eventTrigger == null)
			{
				eventTrigger = target.gameObject.AddComponent<EventTrigger>();
			}
			eventTrigger.triggers.Clear();
			AddTopBarTooltipTrigger(eventTrigger, EventTriggerType.PointerDown, delegate
			{
				SetTopBarTooltipVisible(visible: true, tooltip, position);
			});
			AddTopBarTooltipTrigger(eventTrigger, EventTriggerType.PointerClick, delegate
			{
				SetTopBarTooltipVisible(visible: true, tooltip, position);
			});
			AddTopBarTooltipTrigger(eventTrigger, EventTriggerType.PointerExit, delegate
			{
				SetTopBarTooltipVisible(visible: false, null, Vector2.zero);
			});
		}
	}

	private static void AddTopBarTooltipTrigger(EventTrigger trigger, EventTriggerType type, Action action)
	{
		if (!(trigger == null) && action != null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry
			{
				eventID = type
			};
			entry.callback.AddListener(delegate
			{
				action();
			});
			trigger.triggers.Add(entry);
		}
	}

	private void EnsureTopBarTooltip(Transform parent)
	{
		if (!(parent == null))
		{
			if (topBarTooltipBackground == null)
			{
				Transform transform = parent.Find("BattleTopBarTooltip");
				topBarTooltipBackground = ((transform != null) ? transform.GetComponent<Image>() : null);
			}
			if (topBarTooltipBackground == null)
			{
				GameObject gameObject = new GameObject("BattleTopBarTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
				gameObject.layer = parent.gameObject.layer;
				gameObject.transform.SetParent(parent, worldPositionStays: false);
				topBarTooltipBackground = gameObject.GetComponent<Image>();
			}
			else if (topBarTooltipBackground.transform.parent != parent)
			{
				topBarTooltipBackground.transform.SetParent(parent, worldPositionStays: false);
			}
			RectTransform rectTransform = topBarTooltipBackground.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 1f);
			rectTransform.anchoredPosition = ((topBarTooltipPosition == Vector2.zero) ? new Vector2(140f, -48f) : topBarTooltipPosition);
			rectTransform.sizeDelta = new Vector2(470f, 76f);
			rectTransform.localScale = Vector3.one;
			topBarTooltipBackground.color = new Color(0.05f, 0.035f, 0.02f, 0.94f);
			topBarTooltipBackground.raycastTarget = false;
			if (topBarTooltipText == null)
			{
				Transform transform2 = topBarTooltipBackground.transform.Find("Label");
				topBarTooltipText = ((transform2 != null) ? transform2.GetComponent<TMP_Text>() : null);
			}
			if (topBarTooltipText == null)
			{
				GameObject gameObject2 = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
				gameObject2.layer = parent.gameObject.layer;
				gameObject2.transform.SetParent(topBarTooltipBackground.transform, worldPositionStays: false);
				topBarTooltipText = gameObject2.GetComponent<TextMeshProUGUI>();
			}
			RectTransform rectTransform2 = topBarTooltipText.rectTransform;
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.offsetMin = new Vector2(18f, 8f);
			rectTransform2.offsetMax = new Vector2(-18f, -8f);
			rectTransform2.localScale = Vector3.one;
			ApplyBattleLobbyFontToText(topBarTooltipText);
			topBarTooltipText.alignment = TextAlignmentOptions.Center;
			topBarTooltipText.enableAutoSizing = true;
			topBarTooltipText.fontSize = 30f;
			topBarTooltipText.fontSizeMin = 17f;
			topBarTooltipText.fontSizeMax = 30f;
			topBarTooltipText.fontStyle = FontStyles.Bold;
			topBarTooltipText.textWrappingMode = TextWrappingModes.NoWrap;
			topBarTooltipText.overflowMode = TextOverflowModes.Truncate;
			topBarTooltipText.color = new Color(1f, 0.91f, 0.62f, 1f);
			topBarTooltipText.raycastTarget = false;
			topBarTooltipBackground.transform.SetAsLastSibling();
			topBarTooltipBackground.gameObject.SetActive(topBarTooltipVisible);
		}
	}

	private void SetTopBarTooltipVisible(bool visible, string text, Vector2 position)
	{
		topBarTooltipVisible = visible && !string.IsNullOrWhiteSpace(text);
		topBarTooltipValue = (topBarTooltipVisible ? text : string.Empty);
		topBarTooltipPosition = position;
		if (topBarTooltipBackground != null)
		{
			RectTransform rectTransform = topBarTooltipBackground.rectTransform;
			if (rectTransform != null && topBarTooltipVisible)
			{
				rectTransform.anchoredPosition = topBarTooltipPosition;
			}
			topBarTooltipBackground.gameObject.SetActive(topBarTooltipVisible);
			if (topBarTooltipVisible)
			{
				topBarTooltipBackground.transform.SetAsLastSibling();
			}
		}
		if (topBarTooltipText != null)
		{
			topBarTooltipText.text = topBarTooltipValue;
		}
	}

	private string BuildTopBarTooltipText(int index)
	{
		return index switch
		{
			0 => BattleLobbyText("Оз Алтын: золото игрока", "Oz Gold: player gold", "Oz Altın: oyuncu altını", "Oz-Gold: Spielergold"), 
			1 => BattleLobbyText("Оз-Аметист: премиальная валюта", "Oz Ametist: premium currency", "Oz Ametist: premium para", "Oz-Ametist: Premiumwaehrung"), 
			2 => BattleLobbyText("Оз-тайлы: валюта боевых режимов", "Oz Tiles: battle-mode currency", "Oz Taşlari: savaş modu parasi", "Oz-Kacheln: Waehrung fuer Kampfmodi"), 
			3 => BattleLobbyText("Имя игрока и тег альянса", "Player name and alliance tag", "Oyuncu adi ve ittifak etiketi", "Spielername und Allianz-Tag"), 
			4 => BattleLobbyText("RP: очки ранга для лиг и матчмейкинга", "RP: rank points for leagues and matchmaking", "RP: lig ve eslesme puani", "RP: Rangpunkte fuer Ligen und Matchmaking"), 
			5 => BattleLobbyText("Уровень боевого профиля Mahjong", "Mahjong battle profile level", "Mahjong savaş profili seviyesi", "Mahjong-Kampfprofilstufe"), 
			6 => BattleLobbyText("EXP: опыт до следующего уровня", "EXP: progress to the next level", "EXP: sonraki seviyeye ilerleme", "EXP: Fortschritt zur naechsten Stufe"), 
			7 => BattleLobbyText("Победы и поражения в Mahjong Battle", "Wins and losses in Mahjong Battle", "Mahjong Battle galibiyet ve yenilgileri", "Siege und Niederlagen in Mahjong Battle"), 
			8 => BattleLobbyText("Процент побед в боях", "Battle win rate", "Savaş kazanma orani", "Kampf-Siegquote"), 
			9 => BuildEnergyIconTimerTooltipText(), 
			10 => BattleLobbyText("Класс выбранного персонажа", "Selected character class", "Secili karakter sinifi", "Klasse des gewaehlten Charakters"), 
			11 => BattleLobbyText("Основные боевые статы выбранного персонажа", "Selected character battle stats", "Secili karakter savas degerleri", "Kampfwerte des gewaehlten Charakters"), 
			_ => string.Empty, 
		};
	}

	private string BuildBattleLobbyCharacterClassLine(BattleCharacterDatabase.BattleCharacterData character)
	{
		if (character == null)
		{
			return BattleLobbyText("Класс: не выбран", "Class: none", "Sinif: yok", "Klasse: keiner");
		}
		return BattleLobbyText("Класс: ", "Class: ", "Sinif: ", "Klasse: ") + ResolveBattleLobbyCharacterClassName(character);
	}

	private string ResolveBattleLobbyCharacterClassName(BattleCharacterDatabase.BattleCharacterData character)
	{
		if (character == null)
		{
			return string.Empty;
		}
		return character.AnimalType switch
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger => BattleLobbyText("Авангард", "Vanguard", "Oncu", "Vorhut"),
			BattleCharacterDatabase.CharacterAnimalType.Fox => BattleLobbyText("Разведчик", "Scout", "Izci", "Spaher"),
			BattleCharacterDatabase.CharacterAnimalType.Wolf => BattleLobbyText("Дуэлянт", "Duelist", "Duellocu", "Duellant"),
			BattleCharacterDatabase.CharacterAnimalType.Bear => BattleLobbyText("Страж", "Sentinel", "Nobetci", "Wachter"),
			BattleCharacterDatabase.CharacterAnimalType.Dragon => BattleLobbyText("Арканист", "Arcanist", "Arkanist", "Arkanist"),
			BattleCharacterDatabase.CharacterAnimalType.Dog => BattleLobbyText("Следопыт", "Tracker", "Iz Surucu", "Faehrtenleser"),
			_ => character.AnimalType.ToString(),
		};
	}

	private string BuildBattleLobbyCharacterStatsLine(BattleCharacterDatabase.BattleCharacterData character)
	{
		if (character == null)
		{
			return BattleLobbyText("HP -           Атака -\nБроня -           Крит -", "HP -           Attack -\nArmor -           Crit -", "HP -           Saldiri -\nZirh -           Kritik -", "HP -           Angriff -\nRuestung -           Krit -");
		}
		BattleCharacterDatabase.BattleCharacterStats stats = character.Stats;
		stats.Sanitize();
		return BattleLobbyText("HP", "HP", "HP", "HP") + $" {stats.MaxHp}           " +
		       BattleLobbyText("Атака", "Attack", "Saldiri", "Angriff") + $" {stats.Attack}\n" +
		       BattleLobbyText("Броня", "Armor", "Zirh", "Ruestung") + $" {Mathf.RoundToInt(stats.Armor * 100f)}%           " +
		       BattleLobbyText("Крит", "Crit", "Kritik", "Krit") + $" {Mathf.RoundToInt(stats.CritChance * 100f)}%";
	}

	private string BuildEnergyIconTimerTooltipText()
	{
		if (EnergyService.HasInfiniteEnergy())
		{
			return BattleLobbyText("Энергия бесконечна", "Energy infinite", "Enerji sonsuz", "Energie unbegrenzt");
		}
		int currentEnergy = EnergyService.CurrentEnergy;
		int currentMaxEnergy = EnergyService.CurrentMaxEnergy;
		if (currentEnergy >= currentMaxEnergy)
		{
			return BattleLobbyText("Энергия полная", "Energy full", "Enerji dolu", "Energie voll");
		}
		return BattleLobbyText("+1 через " + EnergyService.FormatTimeUntilNextEnergy(), "+1 in " + EnergyService.FormatTimeUntilNextEnergy(), "+1: " + EnergyService.FormatTimeUntilNextEnergy(), "+1 in " + EnergyService.FormatTimeUntilNextEnergy());
	}

	private TMP_Text[] EnsureBattleLobbyTopBarTexts(Transform topBarTransform)
	{
		if (topBarTransform == null)
		{
			return Array.Empty<TMP_Text>();
		}
		Transform transform = EnsureTopMirrorStatsRoot(topBarTransform);
		if (transform == null)
		{
			return Array.Empty<TMP_Text>();
		}
		RectTransform rectTransform = transform as RectTransform;
		if (rectTransform != null)
		{
			Vector2 panelSize = ResolveBattleLobbyStatsPanelSize();
			rectTransform.anchorMin = new Vector2(1f, 0.5f);
			rectTransform.anchorMax = new Vector2(1f, 0.5f);
			rectTransform.pivot = new Vector2(1f, 0.5f);
			rectTransform.anchoredPosition = ResolveBattleLobbyStatsPanelPosition(panelSize);
			rectTransform.sizeDelta = panelSize;
			rectTransform.localScale = Vector3.one;
		}
		TMP_Text[] componentsInChildren = topBarTransform.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren)
		{
			if (!(tMP_Text == null) && !tMP_Text.transform.IsChildOf(transform))
			{
				tMP_Text.gameObject.SetActive(value: false);
			}
		}
		string[] array = new string[12] { "BattleTopBarGold", "BattleTopBarAmetist", "BattleTopBarOzTile", "BattleTopBarPlayerName", "BattleTopBarRank", "BattleTopBarLevel", "BattleTopBarExp", "BattleTopBarWins", "BattleTopBarWinRate", "BattleTopBarEnergy", "BattleTopBarCharacterClass", "BattleTopBarCharacterStats" };
		TMP_Text[] array2 = new TMP_Text[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			Transform transform2 = transform.Find(array[j]);
			array2[j] = ((transform2 != null) ? transform2.GetComponent<TMP_Text>() : null);
			if (array2[j] == null)
			{
				array2[j] = CreateBattleLobbyTopBarText(transform, array[j]);
			}
			if (array2[j] != null)
			{
				array2[j].name = array[j];
				array2[j].gameObject.SetActive(value: true);
			}
		}
		PlaceBattleLobbyStatsUnderBars(transform, topBarTransform);
		return array2;
	}

	private Image EnsureBattleLobbyRankIcon(Transform topBarTransform)
	{
		if (topBarTransform == null)
		{
			return null;
		}
		Transform transform = EnsureTopMirrorStatsRoot(topBarTransform);
		if (transform == null)
		{
			return null;
		}
		if (battleLobbyRankIcon == null)
		{
			Transform transform2 = transform.Find("BattleTopBarRankIcon");
			battleLobbyRankIcon = ((transform2 != null) ? transform2.GetComponent<Image>() : null);
		}
		if (battleLobbyRankIcon == null)
		{
			GameObject gameObject = new GameObject("BattleTopBarRankIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.layer = transform.gameObject.layer;
			gameObject.transform.SetParent(transform, worldPositionStays: false);
			battleLobbyRankIcon = gameObject.GetComponent<Image>();
			battleLobbyRankIcon.raycastTarget = false;
		}
		RectTransform rectTransform = battleLobbyRankIcon.rectTransform;
		if (rectTransform != null)
		{
			float scale = ResolveBattleLobbyStatsPanelScale();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(-118f, 282f) * scale;
			rectTransform.sizeDelta = new Vector2(62f, 62f) * scale;
			rectTransform.localScale = Vector3.one;
		}
		battleLobbyRankIcon.transform.SetAsFirstSibling();
		battleLobbyRankIcon.gameObject.SetActive(value: true);
		return battleLobbyRankIcon;
	}

	private Transform EnsureTopMirrorStatsRoot(Transform topBarTransform)
	{
		if (topMirrorStatsRoot == null)
		{
			Canvas canvas = FindActiveSceneCanvas();
			if (canvas == null)
			{
				return null;
			}
			topMirrorStatsRoot = new GameObject("BattleTopBarRuntimeTextRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			topMirrorStatsRoot.layer = topBarTransform.gameObject.layer;
			topMirrorStatsRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
		}
		RectTransform rectTransform = topMirrorStatsRoot.transform as RectTransform;
		if (rectTransform != null)
		{
			Vector2 panelSize = ResolveBattleLobbyStatsPanelSize();
			rectTransform.anchorMin = new Vector2(1f, 0.5f);
			rectTransform.anchorMax = new Vector2(1f, 0.5f);
			rectTransform.pivot = new Vector2(1f, 0.5f);
			rectTransform.anchoredPosition = ResolveBattleLobbyStatsPanelPosition(panelSize);
			rectTransform.sizeDelta = panelSize;
			rectTransform.localScale = Vector3.one;
		}
		Image image = topMirrorStatsRoot.GetComponent<Image>();
		if (image == null)
		{
			image = topMirrorStatsRoot.AddComponent<Image>();
		}
		if (image != null)
		{
			image.sprite = LoadBattleLobbyStatsWindowSprite();
			image.type = Image.Type.Simple;
			image.preserveAspect = false;
			image.color = Color.white;
			image.raycastTarget = false;
		}
		PlaceBattleLobbyStatsUnderBars(topMirrorStatsRoot.transform, topBarTransform);
		return topMirrorStatsRoot.transform;
	}

	private Vector2 ResolveBattleLobbyStatsPanelSize()
	{
		Vector2 canvasSize = GetBattleLobbyCanvasSize();
		float width = Mathf.Max(1f, canvasSize.x);
		float height = Mathf.Max(1f, canvasSize.y);
		float topReserve = Mathf.Clamp(height * 0.105f, 78f, 116f);
		float bottomReserve = Mathf.Clamp(height * 0.112f, 88f, 126f);
		float verticalPadding = Mathf.Clamp(height * 0.008f, 8f, 14f);
		float availableHeight = Mathf.Max(240f, height - topReserve - bottomReserve - verticalPadding);
		float panelHeight = Mathf.Clamp(availableHeight * 1.085f, 460f, 980f) + BattleLobbyStatsPanelBottomExtension;
		float panelWidth = panelHeight * (520f / 620f);
		float maxWidth = Mathf.Clamp(width * 0.34f, 300f, 650f);
		if (panelWidth > maxWidth)
		{
			panelWidth = maxWidth;
		}
		return new Vector2(panelWidth, panelHeight);
	}

	private static void PlaceBattleLobbyStatsUnderBars(Transform statsRoot, Transform topBarTransform)
	{
		if (statsRoot == null)
		{
			return;
		}
		if (topBarTransform == null || statsRoot.parent != topBarTransform.parent)
		{
			statsRoot.SetAsFirstSibling();
			return;
		}
		statsRoot.SetSiblingIndex(Mathf.Max(0, topBarTransform.GetSiblingIndex()));
	}

	private Vector2 ResolveBattleLobbyStatsPanelPosition(Vector2 panelSize)
	{
		Vector2 canvasSize = GetBattleLobbyCanvasSize();
		float width = Mathf.Max(1f, canvasSize.x);
		float height = Mathf.Max(1f, canvasSize.y);
		float topReserve = Mathf.Clamp(height * 0.105f, 78f, 116f);
		float bottomReserve = Mathf.Clamp(height * 0.112f, 88f, 126f);
		float rightMargin = -Mathf.Clamp(width * 0.022f, 38f, 58f);
		float centerY = (bottomReserve - topReserve) * 0.5f;
		float maxY = height * 0.5f - topReserve - panelSize.y * 0.5f;
		float minY = -height * 0.5f + bottomReserve + panelSize.y * 0.5f;
		if (minY <= maxY)
		{
			centerY = Mathf.Clamp(centerY, minY, maxY);
		}
		centerY -= BattleLobbyStatsPanelBottomExtension * 0.5f;
		return new Vector2(-rightMargin, centerY);
	}

	private float ResolveBattleLobbyStatsPanelScale()
	{
		return Mathf.Clamp(ResolveBattleLobbyStatsPanelSize().y / 620f, 0.46f, 1f);
	}

	private TMP_Text CreateBattleLobbyTopBarText(Transform parent, string objectName)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.layer = parent.gameObject.layer;
		obj.transform.SetParent(parent, worldPositionStays: false);
		TextMeshProUGUI component = obj.GetComponent<TextMeshProUGUI>();
		component.raycastTarget = false;
		component.textWrappingMode = TextWrappingModes.NoWrap;
		component.overflowMode = TextOverflowModes.Truncate;
		ApplyBattleLobbyFontToText(component);
		return component;
	}

	private void ApplyBattleLobbyTopBarTextLayout(Transform topBarTransform)
	{
		if (topBarTransform == null)
		{
			return;
		}
		TMP_Text[] array = EnsureBattleLobbyTopBarTexts(topBarTransform);
		if (array == null || array.Length == 0)
		{
			return;
		}
		float scale = ResolveBattleLobbyStatsPanelScale();
		Vector2[] array2 = new Vector2[12]
		{
			new Vector2(-180f, BattleLobbyCurrencyRowY),
			new Vector2(-20f, BattleLobbyCurrencyRowY),
			new Vector2(140f, BattleLobbyCurrencyRowY),
			new Vector2(32f, 282f),
			new Vector2(0f, 158f),
			new Vector2(134f, 96f),
			new Vector2(0f, 46f),
			new Vector2(0f, 216f),
			new Vector2(0f, -258f),
			new Vector2(28f, -342f),
			new Vector2(-64f, 96f),
			new Vector2(0f, -118f)
		};
		float[] array3 = new float[12] { 110f, 110f, 110f, 350f, 430f, 150f, 300f, 430f, 388f, 180f, 270f, 500f };
		float[] array4 = new float[12] { 42f, 42f, 42f, 60f, 48f, 42f, 42f, 36f, 42f, 62f, 42f, 154f };
		TextAlignmentOptions[] array5 = new TextAlignmentOptions[12]
		{
			TextAlignmentOptions.MidlineLeft,
			TextAlignmentOptions.MidlineLeft,
			TextAlignmentOptions.MidlineLeft,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.MidlineLeft,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.MidlineLeft,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.Center,
			TextAlignmentOptions.Center
		};
		for (int i = 0; i < array.Length && i < array2.Length; i++)
		{
			TMP_Text tMP_Text = array[i];
			if (!(tMP_Text == null))
			{
				RectTransform rectTransform = tMP_Text.rectTransform;
				if (rectTransform != null)
				{
					rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
					rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
					rectTransform.pivot = ((array5[i] == TextAlignmentOptions.Center) ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f));
					rectTransform.anchoredPosition = array2[i] * scale;
					rectTransform.sizeDelta = new Vector2(array3[i] * scale, array4[i] * scale);
					rectTransform.localScale = Vector3.one;
				}
				ApplyBattleLobbyFontToText(tMP_Text);
				tMP_Text.alignment = array5[i];
				tMP_Text.enableAutoSizing = true;
				tMP_Text.fontSize = ((i == 3) ? 50f : ((i == 4) ? 39f : ((i == 9) ? 41f : ((i == 10) ? 34f : ((i == 11) ? 31f : ((i == 7) ? 33f : ((i == 8) ? 31f : 33f))))))) * scale;
				tMP_Text.fontSizeMin = Mathf.Max(10f, ((i == 3) ? 22f : ((i == 4) ? 18f : ((i == 9) ? 20f : ((i == 10 || i == 11) ? 14f : 15f)))) * scale);
				tMP_Text.fontSizeMax = ((i == 3) ? 54f : ((i == 4) ? 41f : ((i == 9) ? 44f : ((i == 10) ? 36f : ((i == 11) ? 33f : ((i == 0) ? 38f : ((i >= 7) ? 32f : 34f))))))) * scale;
				tMP_Text.fontStyle = FontStyles.Bold;
				tMP_Text.lineSpacing = i == 11 ? 58f : 0f;
				tMP_Text.textWrappingMode = i == 11 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
				tMP_Text.overflowMode = TextOverflowModes.Truncate;
				tMP_Text.color = Color.white;
				ApplyBattleLobbyTopBarTextEffect(tMP_Text, i);
				ApplyBattleLobbyFontToText(tMP_Text);
					tMP_Text.gameObject.SetActive(!string.IsNullOrWhiteSpace(tMP_Text.text));
				tMP_Text.transform.SetAsLastSibling();
			}
		}
	}

	private float ResolveBattleLobbyTopBarContentY()
	{
		float num = Mathf.Max(1f, GetBattleLobbyCanvasSize().y);
		float num2 = ResolveBattleLobbyTopBarRootHeight();
		float num3 = Mathf.Clamp(num * 0.016f, 12f, 20f);
		return num2 * 0.5f - num3 - 21f;
	}

	private float ResolveBattleLobbyTopBarRootHeight()
	{
		return Mathf.Clamp(Mathf.Max(1f, GetBattleLobbyCanvasSize().y) * 0.095f, 78f, 110f);
	}

	private float ResolveBattleLobbyTopBarScale(Vector2 canvasSize)
	{
		return Mathf.Clamp(Mathf.Max(1f, canvasSize.x) / MainLobbyUiCoordinator.OverlayReferenceResolution.x, 0.42f, 1f);
	}

	private Vector2 ResolveBattleLobbyTopBarIconSize(Vector2 referenceSize)
	{
		float value = ResolveBattleLobbyTopBarScale(GetBattleLobbyCanvasSize());
		return referenceSize * Mathf.Clamp(value, 0.55f, 0.82f);
	}

	private static void ApplyBattleLobbyTopBarTextEffect(TMP_Text text, int index)
	{
		if (!(text == null))
		{
			text.enableVertexGradient = true;
			text.colorGradient = index == 3
				? new VertexGradient(new Color(1f, 0.97f, 0.78f, 1f), new Color(1f, 0.88f, 0.48f, 1f), new Color(0.84f, 0.52f, 0.18f, 1f), new Color(1f, 0.72f, 0.24f, 1f))
				: new VertexGradient(new Color(1f, 0.98f, 0.82f, 1f), new Color(0.96f, 0.88f, 0.64f, 1f), new Color(0.72f, 0.5f, 0.24f, 1f), new Color(1f, 0.78f, 0.34f, 1f));
			text.outlineWidth = index == 3 ? 0.24f : ((index <= 1) ? 0.14f : 0.16f);
			text.outlineColor = new Color(0.04f, 0.018f, 0.006f, 1f);
			if (index == 3)
			{
				Shadow shadow = text.GetComponent<Shadow>();
				if (shadow == null)
				{
					shadow = text.gameObject.AddComponent<Shadow>();
				}
				shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
				shadow.effectDistance = new Vector2(2.5f, -2.5f);
				shadow.useGraphicAlpha = true;
			}
			text.fontSharedMaterial = text.fontMaterial;
		}
	}

	private void ApplyBattleLobbyTypography()
	{
		ApplyBattleLobbyFontToButton(returnToLobbyButton);
		ApplyBattleLobbyFontToButton(battleShopButton);
		ApplyBattleLobbyFontToButton(battleTileInventoryButton);
		ApplyBattleLobbyFontToButton(weeklyRewardButton);
		if (IsExplicitCharacterButton(openCharacterCarouselButton))
		{
			ApplyBattleLobbyFontToButton(openCharacterCarouselButton);
		}
		ApplyBattleLobbyFontToButton(energyAdButton);
		ApplyBattleLobbyFontToDescendantTexts((battleProgressRoot != null) ? battleProgressRoot.transform : null);
		ApplyBattleLobbyFontToDescendantTexts((battleShopRoot != null) ? battleShopRoot.transform : null);
		ApplyBattleLobbyFontToDescendantTexts((battleTileInventoryRoot != null) ? battleTileInventoryRoot.transform : null);
		ApplyBattleLobbyFontToDescendantTexts((weeklyRewardRoot != null) ? weeklyRewardRoot.transform : null);
		ApplyBattleLobbyFontToActiveSceneCanvas();
	}

	private void ApplyBattleLobbyMatchButtonVisual(Button button)
	{
		if (!(button == null) && !(button.image == null))
		{
			ApplyBattleLobbyProButtonSprite(button);
			TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (!(componentInChildren == null))
			{
				ApplyBattleLobbyFontToText(componentInChildren);
				MainLobbyButtonStyle.ApplySilverTextEffect(componentInChildren);
				ApplyBattleLobbyFontToText(componentInChildren);
				componentInChildren.alignment = TextAlignmentOptions.Center;
				componentInChildren.fontSize = 46f;
				componentInChildren.enableAutoSizing = true;
				componentInChildren.fontSizeMin = 28f;
				componentInChildren.fontSizeMax = 50f;
				componentInChildren.textWrappingMode = TextWrappingModes.NoWrap;
				componentInChildren.overflowMode = TextOverflowModes.Truncate;
				componentInChildren.margin = BattleLobbyMatchButtonLabelMargin;
			}
		}
	}

	private void ApplyBattleLobbyUtilityButtonVisual(Button button)
	{
		if (!(button == null) && !(button.image == null))
		{
			ApplyBattleLobbyProButtonSprite(button);
			TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (!(componentInChildren == null))
			{
				ApplyBattleLobbyFontToText(componentInChildren);
				MainLobbyButtonStyle.ApplySilverTextEffect(componentInChildren);
				ApplyBattleLobbyFontToText(componentInChildren);
				componentInChildren.alignment = TextAlignmentOptions.Center;
				componentInChildren.fontSize = 46f;
				componentInChildren.enableAutoSizing = true;
				componentInChildren.fontSizeMin = 30f;
				componentInChildren.fontSizeMax = 50f;
				componentInChildren.textWrappingMode = TextWrappingModes.NoWrap;
				componentInChildren.overflowMode = TextOverflowModes.Truncate;
				componentInChildren.margin = BattleLobbyUtilityButtonLabelMargin;
			}
		}
	}

	private void ApplyBattleLobbyRightStackUtilityButtonVisual(Button button)
	{
		if (!(button == null))
		{
			BattlePopupStyle.ApplyBattleLobbyUtilityButton(button);
			ApplyBattleLobbyFontToButton(button);
		}
	}

	private static void ApplyBattleLobbyProButtonSprite(Button button)
	{
		if (!(button == null) && !(button.image == null))
		{
			Sprite sprite = LoadBattleLobbyProButtonSprite();
			if (sprite == null)
			{
				BattlePopupStyle.ApplyButton(button);
				return;
			}
			button.image.sprite = sprite;
			button.image.type = Image.Type.Simple;
			button.image.preserveAspect = false;
			button.image.color = Color.white;
			button.image.raycastTarget = true;
			button.targetGraphic = button.image;
			button.transition = Selectable.Transition.ColorTint;
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1.08f, 1.05f, 0.92f, 1f);
			colors.pressedColor = new Color(0.82f, 0.76f, 0.64f, 1f);
			colors.selectedColor = new Color(1.04f, 1.02f, 0.9f, 1f);
			colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.82f);
			colors.colorMultiplier = 1f;
			button.colors = colors;
		}
	}

	private void ApplyBattleLobbyFontToButton(Button button)
	{
		if (!(button == null))
		{
			TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			ApplyBattleLobbyFontToText(componentInChildren);
		}
	}

	private void ApplyBattleLobbyFontToDescendantTexts(Transform root)
	{
		if (!(root == null))
		{
			TMP_Text[] componentsInChildren = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ApplyBattleLobbyFontToText(componentsInChildren[i]);
			}
		}
	}

	private void ApplyBattleLobbyFontToActiveSceneCanvas()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (!(canvas == null) && !(canvas.gameObject.scene != activeScene))
			{
				ApplyBattleLobbyFontToDescendantTexts(canvas.transform);
			}
		}
	}

	private void ApplyBattleLobbyFontToText(TMP_Text text)
	{
		if (!(text == null))
		{
			TMP_FontAsset tMP_FontAsset = ((BattlePopupStyle.Font != null) ? BattlePopupStyle.Font : battleLobbyMainFont);
			if (tMP_FontAsset == null)
			{
				tMP_FontAsset = TMP_Settings.defaultFontAsset;
			}
			if (!(tMP_FontAsset == null))
			{
				text.font = tMP_FontAsset;
				text.fontSharedMaterial = tMP_FontAsset.material;
			}
		}
	}

	private Sprite GetBattleLobbyMatchButtonSprite()
	{
		if (battleLobbyButtonSprite == null)
		{
			return null;
		}
		if (cachedBattleLobbyButtonSprite != null)
		{
			return cachedBattleLobbyButtonSprite;
		}
		cachedBattleLobbyButtonSprite = CreateRuntimeSpriteVariant(battleLobbyButtonSprite, BattleLobbyMatchButtonSpriteRect);
		return cachedBattleLobbyButtonSprite;
	}

	private static Sprite LoadBattleLobbyProButtonSprite()
	{
		if (cachedBattleLobbyProButtonSprite != null)
		{
			return cachedBattleLobbyProButtonSprite;
		}
		Sprite sprite = Resources.Load<Sprite>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
		if (sprite == null)
		{
			Sprite[] array = Resources.LoadAll<Sprite>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
			if (array != null && array.Length != 0)
			{
				sprite = array[0];
			}
		}
		if (sprite == null || sprite.texture == null)
		{
			return null;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2");
		if (texture2D != null)
		{
			cachedBattleLobbyProButtonSprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, BattleLobbyProButtonBorder);
			return cachedBattleLobbyProButtonSprite;
		}
		Rect rect = ((BattleLobbyProButtonUsefulRect.width <= 0.5f || BattleLobbyProButtonUsefulRect.height <= 0.5f) ? sprite.rect : ClampRectToBounds(BattleLobbyProButtonUsefulRect, sprite.textureRect));
		cachedBattleLobbyProButtonSprite = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0u, SpriteMeshType.FullRect, BattleLobbyProButtonBorder);
		return cachedBattleLobbyProButtonSprite;
	}

	private Sprite GetBattleLobbyTopBarSprite()
	{
		if (battleLobbyTopBarSprite == null)
		{
			return null;
		}
		if (cachedBattleLobbyTopBarSprite != null)
		{
			return cachedBattleLobbyTopBarSprite;
		}
		cachedBattleLobbyTopBarSprite = CreateRuntimeSpriteVariant(battleLobbyTopBarSprite, BattleLobbyTopBarSpriteRect);
		return cachedBattleLobbyTopBarSprite;
	}

	private static Sprite CreateRuntimeSpriteVariant(Sprite source, Rect targetRect)
	{
		if (source == null || source.texture == null)
		{
			return source;
		}
		if (targetRect.width <= 0.5f || targetRect.height <= 0.5f)
		{
			return source;
		}
		Rect textureRect = source.textureRect;
		Rect rect = ClampRectToBounds(targetRect, textureRect);
		if (Mathf.Approximately(rect.x, textureRect.x) && Mathf.Approximately(rect.y, textureRect.y) && Mathf.Approximately(rect.width, textureRect.width) && Mathf.Approximately(rect.height, textureRect.height))
		{
			return source;
		}
		return Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0u, SpriteMeshType.FullRect);
	}

	private static Rect ClampRectToBounds(Rect targetRect, Rect bounds)
	{
		float num = Mathf.Clamp(targetRect.x, bounds.xMin, bounds.xMax - 1f);
		float num2 = Mathf.Clamp(targetRect.y, bounds.yMin, bounds.yMax - 1f);
		float width = Mathf.Clamp(targetRect.width, 1f, bounds.xMax - num);
		float height = Mathf.Clamp(targetRect.height, 1f, bounds.yMax - num2);
		return new Rect(num, num2, width, height);
	}

	private static Image ResolveBattleLobbyTopBarImage(Transform topBarTransform)
	{
		if (topBarTransform == null)
		{
			return null;
		}
		Transform transform = topBarTransform.Find("Bar/Image");
		if (transform != null)
		{
			Image component = transform.GetComponent<Image>();
			if (component != null)
			{
				return component;
			}
		}
		Transform transform2 = topBarTransform.Find("Bar/UI");
		if (transform2 != null)
		{
			Image component2 = transform2.GetComponent<Image>();
			if (component2 != null)
			{
				return component2;
			}
		}
		Image[] componentsInChildren = topBarTransform.GetComponentsInChildren<Image>(includeInactive: true);
		Image result = null;
		float num = float.MinValue;
		foreach (Image image in componentsInChildren)
		{
			if (image == null)
			{
				continue;
			}
			RectTransform rectTransform = image.rectTransform;
			if (!(rectTransform == null))
			{
				float num2 = Mathf.Abs(rectTransform.rect.width * rectTransform.rect.height);
				if (image.sprite != null)
				{
					num2 += 1000000f;
				}
				if (image.color.a > 0.01f)
				{
					num2 += 100000f;
				}
				if (image.raycastTarget)
				{
					num2 -= 10000f;
				}
				if (num2 > num)
				{
					num = num2;
					result = image;
				}
			}
		}
		return result;
	}

	private void EnsureRandomMatchButton()
	{
		if (!(randomMatchButton != null) && autoCreateRandomMatchButton)
		{
			randomMatchButton = FindButtonByName("ButtonRandomMatch");
			if (randomMatchButton == null)
			{
				randomMatchButton = CreateRandomMatchButton();
			}
		}
	}

	private void EnsureBattleShopButton()
	{
		if (!(battleShopButton != null) && autoCreateBattleShopButton)
		{
			battleShopButton = FindRuntimeButtonByName("ButtonBattleShop");
			if (battleShopButton == null)
			{
				battleShopButton = CreateBattleShopButton();
			}
		}
	}

	private void EnsureBattleTileInventoryButton()
	{
		if (!(battleTileInventoryButton != null) && autoCreateBattleTileInventoryButton)
		{
			battleTileInventoryButton = FindRuntimeButtonByName("ButtonBattleTileInventory");
			if (battleTileInventoryButton == null)
			{
				battleTileInventoryButton = CreateBattleTileInventoryButton();
			}
		}
	}

	private void EnsureWeeklyRewardButton()
	{
		if (!(weeklyRewardButton != null) && autoCreateWeeklyRewardButton)
		{
			weeklyRewardButton = FindRuntimeButtonByName("ButtonWeeklyRewards");
			if (weeklyRewardButton == null)
			{
				weeklyRewardButton = CreateWeeklyRewardButton();
			}
		}
	}

	private void EnsureDailyHeroBonusButton()
	{
		if (!(dailyHeroBonusButton != null) && autoCreateDailyHeroBonusButton)
		{
			dailyHeroBonusButton = FindRuntimeButtonByName("ButtonDailyHeroBonus");
			if (dailyHeroBonusButton == null)
			{
				dailyHeroBonusButton = CreateDailyHeroBonusButton();
			}
		}
	}

	private void EnsureLocalWifiBattleButton()
	{
		if (!(localWifiBattleButton != null) && autoCreateLocalWifiBattleButton)
		{
			localWifiBattleButton = FindButtonByName("ButtonLocalWifiBattle");
			if (localWifiBattleButton == null)
			{
				localWifiBattleButton = CreateLocalWifiBattleButton();
			}
		}
	}

	private void EnsureRankedBattleButton()
	{
		if (!(rankedBattleButton != null) && autoCreateRankedBattleButton)
		{
			rankedBattleButton = FindButtonByName("ButtonRankedBattle");
			if (rankedBattleButton == null)
			{
				rankedBattleButton = CreateRankedBattleButton();
			}
		}
	}

	private void EnsureDuelChallengeButton()
	{
		if (!(duelChallengeButton != null) && autoCreateDuelChallengeButton)
		{
			duelChallengeButton = FindButtonByName("ButtonDuelChallenge");
			if (duelChallengeButton == null)
			{
				duelChallengeButton = CreateDuelChallengeButton();
			}
		}
	}

	private void EnsureTournamentButton()
	{
		if (!(tournamentButton != null) && autoCreateTournamentButton)
		{
			tournamentButton = FindButtonByName("ButtonTournament");
			if (tournamentButton == null)
			{
				tournamentButton = CreateTournamentButton();
			}
		}
	}

	private Button CreateRandomMatchButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonRandomMatch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = randomMatchButtonPosition;
		component.sizeDelta = randomMatchButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.14f, 0.24f, 0.19f, 0.96f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyBattleLobbyUtilityButton(component3);
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = new Vector2(16f, 8f);
		component4.offsetMax = new Vector2(-16f, -8f);
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = randomMatchButtonText;
		component5.fontSize = 30f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 32f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateLocalWifiBattleButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonLocalWifiBattle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = localWifiBattleButtonPosition;
		component.sizeDelta = localWifiBattleButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.12f, 0.18f, 0.2f, 0.94f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyBattleLobbyUtilityButton(component3);
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = new Vector2(16f, 8f);
		component4.offsetMax = new Vector2(-16f, -8f);
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = localWifiBattleButtonText;
		component5.fontSize = 30f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 32f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateRankedBattleButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonRankedBattle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = rankedBattleButtonPosition;
		component.sizeDelta = rankedBattleButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.18f, 0.16f, 0.28f, 0.96f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyBattleLobbyUtilityButton(component3);
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = new Vector2(16f, 8f);
		component4.offsetMax = new Vector2(-16f, -8f);
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = rankedBattleButtonText;
		component5.fontSize = 30f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 32f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateDuelChallengeButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonDuelChallenge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = duelChallengeButtonPosition;
		component.sizeDelta = duelChallengeButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.24f, 0.17f, 0.12f, 0.96f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyBattleLobbyUtilityButton(component3);
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = new Vector2(16f, 8f);
		component4.offsetMax = new Vector2(-16f, -8f);
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = duelChallengeButtonText;
		component5.fontSize = 30f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 32f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateTournamentButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonTournament", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = tournamentButtonPosition;
		component.sizeDelta = tournamentButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.13f, 0.2f, 0.27f, 0.96f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = new Vector2(16f, 8f);
		component4.offsetMax = new Vector2(-16f, -8f);
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = tournamentButtonText;
		component5.fontSize = 30f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 32f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateBattleShopButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonBattleShop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = battleShopButtonPosition;
		component.sizeDelta = battleShopButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.13f, 0.12f, 0.2f, 0.92f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = battleShopButtonText;
		component5.fontSize = 28f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 34f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateBattleTileInventoryButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonBattleTileInventory", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = battleTileInventoryButtonPosition;
		component.sizeDelta = battleTileInventoryButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.11f, 0.16f, 0.18f, 0.94f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = battleTileInventoryButtonText;
		component5.fontSize = 28f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 34f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateWeeklyRewardButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonWeeklyRewards", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = ResolveWeeklyRewardButtonPosition();
		component.sizeDelta = weeklyRewardButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.16f, 0.13f, 0.2f, 0.92f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = weeklyRewardButtonText;
		component5.fontSize = 28f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 34f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private Button CreateDailyHeroBonusButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonDailyHeroBonus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		MainLobbyUiCoordinator.LayoutBattleLobbyTopTabButton(gameObject.GetComponent<Button>(), 0, 4, GetBattleLobbyCanvasSize());
		Image component = gameObject.GetComponent<Image>();
		component.color = new Color(0.19f, 0.13f, 0.08f, 0.94f);
		component.raycastTarget = true;
		Button component2 = gameObject.GetComponent<Button>();
		component2.targetGraphic = component;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component3 = obj.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		TextMeshProUGUI component4 = obj.GetComponent<TextMeshProUGUI>();
		component4.text = dailyHeroBonusButtonText;
		component4.fontSize = 28f;
		component4.enableAutoSizing = true;
		component4.fontSizeMin = 16f;
		component4.fontSizeMax = 34f;
		component4.alignment = TextAlignmentOptions.Center;
		component4.color = Color.white;
		component4.raycastTarget = false;
		ApplyBattleLobbyRightStackUtilityButtonVisual(component2);
		return component2;
	}

	private Button CreateReturnToLobbyButton()
	{
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("ButtonReturnToLobby", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 1f);
		component.anchorMax = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = returnButtonPosition;
		component.sizeDelta = returnButtonSize;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.08f, 0.1f, 0.13f, 0.9f);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		TextMeshProUGUI component5 = obj.GetComponent<TextMeshProUGUI>();
		component5.text = returnButtonText;
		component5.fontSize = 28f;
		component5.enableAutoSizing = true;
		component5.fontSizeMin = 18f;
		component5.fontSizeMax = 34f;
		component5.alignment = TextAlignmentOptions.Center;
		component5.color = Color.white;
		component5.raycastTarget = false;
		return component3;
	}

	private void EnsureBattleProgressUi()
	{
		if (!ShouldShowBattleProgressUi())
		{
			if (battleProgressRoot == null)
			{
				battleProgressRoot = FindObjectByName("BattleProgressPanel");
			}
			if (battleProgressRoot != null)
			{
				SetGameObjectActiveSafe(battleProgressRoot, active: false);
			}
		}
		else
		{
			if (!autoCreateBattleProgressUi)
			{
				return;
			}
			Canvas canvas = FindActiveSceneCanvas();
			if (!(canvas == null))
			{
				if (battleProgressRoot == null)
				{
					battleProgressRoot = FindObjectByName("BattleProgressPanel");
				}
				if (battleProgressRoot == null)
				{
					battleProgressRoot = new GameObject("BattleProgressPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
					battleProgressRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
				}
				battleProgressRoot.SetActive(value: true);
				RectTransform rectTransform = battleProgressRoot.transform as RectTransform;
				if (rectTransform != null)
				{
					rectTransform.anchorMin = new Vector2(1f, 1f);
					rectTransform.anchorMax = new Vector2(1f, 1f);
					rectTransform.pivot = new Vector2(1f, 1f);
					rectTransform.anchoredPosition = battleProgressPosition;
					rectTransform.sizeDelta = battleProgressSize;
					rectTransform.localScale = Vector3.one;
				}
				Image component = battleProgressRoot.GetComponent<Image>();
				if (component != null)
				{
					component.color = new Color(0.05f, 0.07f, 0.09f, 0.86f);
					component.raycastTarget = false;
				}
				battleLevelText = EnsureProgressText(battleProgressRoot.transform, battleLevelText, "BattleLevelText", new Vector2(22f, -18f), new Vector2(-44f, 42f), 32f, TextAlignmentOptions.Left);
				battleExpText = EnsureProgressText(battleProgressRoot.transform, battleExpText, "BattleExpText", new Vector2(22f, -62f), new Vector2(-44f, 34f), 22f, TextAlignmentOptions.Left);
				battleStatsText = EnsureProgressText(battleProgressRoot.transform, battleStatsText, "BattleStatsText", new Vector2(22f, -102f), new Vector2(-44f, 34f), 22f, TextAlignmentOptions.Left);
				energyText = EnsureProgressText(battleProgressRoot.transform, energyText, "BattleEnergyText", new Vector2(22f, -142f), new Vector2(-44f, 34f), 24f, TextAlignmentOptions.Left);
				energyHintText = EnsureProgressText(battleProgressRoot.transform, energyHintText, "BattleEnergyHintText", new Vector2(22f, -176f), new Vector2(-44f, 30f), 18f, TextAlignmentOptions.Left);
				energyAdButton = EnsureEnergyAdButton(battleProgressRoot.transform, energyAdButton, "BattleEnergyAdButton", new Vector2(22f, -218f), new Vector2(-44f, 46f));
				battleProgressRoot.transform.SetAsLastSibling();
				ApplyBattleLobbyTypography();
			}
		}
	}

	private bool ShouldShowBattleProgressUi()
	{
		return false;
	}

	private void DestroyBattleLobbyRuntimeHudObjects(bool force)
	{
		DestroyBattleLobbyRuntimeHudObjects(force, immediate: false);
	}

	private void DestroyBattleLobbyRuntimeHudObjects(bool force, bool immediate)
	{
		if (force || !ShouldShowLobbyButtons())
		{
			DestroyButtonObject(ref returnToLobbyButton, immediate);
			DestroyButtonObject(ref battleShopButton, immediate);
			DestroyButtonObject(ref battleTileInventoryButton, immediate);
			DestroyButtonObject(ref weeklyRewardButton, immediate);
			DestroyButtonObject(ref dailyHeroBonusButton, immediate);
			DestroyButtonObject(ref randomMatchButton, immediate);
			DestroyButtonObject(ref rankedBattleButton, immediate);
			DestroyButtonObject(ref duelChallengeButton, immediate);
			DestroyButtonObject(ref localWifiBattleButton, immediate);
			DestroyButtonObject(ref tournamentButton, immediate);
			DestroyObjectIfAlive(ref downBarPanelRoot, immediate);
			DestroyObjectIfAlive(ref topMirrorBarPanelRoot, immediate);
			DestroyObjectIfAlive(ref topMirrorStatsRoot, immediate);
			DestroyObjectIfAlive(ref battleShopRoot, immediate);
			DestroyObjectIfAlive(ref battleTileProfileRoot, immediate);
			DestroyObjectIfAlive(ref battleTileInventoryRoot, immediate);
			DestroyObjectIfAlive(ref tutorialGateRoot, immediate);
			DestroyObjectIfAlive(ref tournamentComingSoonRoot, immediate);
			DestroyObjectIfAlive(ref weeklyRewardRoot, immediate);
			DestroyObjectIfAlive(ref dailyHeroBonusRoot, immediate);
			DestroyObjectIfAlive(ref dailyHeroBonusNotificationBadge, immediate);
			DestroyObjectIfAlive(ref battleProgressRoot, immediate);
			weeklyRewardStatusText = null;
			battleTileInventoryStatusText = null;
			battleTileInventoryActiveCountText = null;
			battleTileInventoryReserveCountText = null;
			battleTileInventoryHeroNameText = null;
			battleTileInventoryHeroDescriptionText = null;
			battleTileInventoryHeroStatsText = null;
			battleTileInventoryHeroSkillText = null;
			battleTileInventoryTotemCountText = null;
			battleTileInventoryHeroPortraitImage = null;
			activeTileInventoryContent = null;
			reserveTileInventoryContent = null;
			totemTileInventoryContent = null;
			activeTileInventoryPocketRect = null;
			reserveTileInventoryPocketRect = null;
			totemTileInventoryPocketRect = null;
			battleTileProfileClickRoutine = null;
			weeklyRewardTodayText = null;
			weeklyRewardFreeButtonText = null;
			weeklyRewardAdButtonText = null;
			weeklyRewardFreeButton = null;
			weeklyRewardAdButton = null;
			dailyHeroBoostButton = null;
			dailyHeroBoostStatusText = null;
			StopDailyHeroAttentionRoutine();
			dailyHeroBoostAdRequestInProgress = false;
			weeklyRewardSlotImages = null;
			weeklyRewardIconImages = null;
			weeklyRewardSlotDayTexts = null;
			weeklyRewardSlotStateTexts = null;
			weeklyRewardSlotAmountTexts = null;
			weeklyRewardFreePreviewIcon = null;
			weeklyRewardAdPreviewIcon = null;
			battleLevelText = null;
			battleExpText = null;
			battleStatsText = null;
			energyText = null;
			energyHintText = null;
			energyAdButton = null;
			battleLobbyRankIcon = null;
			battleLobbyRpIcon = null;
			battleLobbyExpIcon = null;
			battleLobbyEnergyIcon = null;
			battleLobbyAmetistIcon = null;
			battleLobbyOzTileIcon = null;
			battleLobbyGoldIcon = null;
			shopBattleTilesSection = null;
			shopBattleTilesTabButton = null;
			shopBattleTileDailyAdButton = null;
			shopBattleTileMediumButton = null;
			shopBattleTileHighButton = null;
			shopBattleTileAmetistButton = null;
			battleTilePackAdRequestInProgress = false;
			topBarTooltipBackground = null;
			topBarTooltipText = null;
			topBarTooltipVisible = false;
			topBarTooltipValue = string.Empty;
			topBarTooltipPosition = Vector2.zero;
		}
	}

	private void RebuildBattleShopForLanguage()
	{
		CloseBattleTilePackResult();
		bool num = battleShopRoot != null && battleShopRoot.activeSelf;
		bool flag = shopCharactersSection != null && shopCharactersSection.activeSelf;
		bool flag2 = shopBattleTilesSection != null && shopBattleTilesSection.activeSelf;
		bool flag3 = shopSkinsSection != null && shopSkinsSection.activeSelf;
		DestroyObjectIfAlive(ref battleShopRoot, immediate: false);
		shopEnergyTabButton = null;
		shopCharactersTabButton = null;
		shopBattleTilesTabButton = null;
		shopSkinsTabButton = null;
		shopBuyEnergyButton = null;
		shopRewardedEnergyButton = null;
		shopBuyDragonMaleButton = null;
		shopBuyDragonFemaleButton = null;
		shopAmetistSmallButton = null;
		shopAmetistMediumButton = null;
		shopAmetistBigButton = null;
		shopAmetistLegendButton = null;
		shopBattleTileDailyAdButton = null;
		shopBattleTileMediumButton = null;
		shopBattleTileHighButton = null;
		shopBattleTileAmetistButton = null;
		shopEnergySection = null;
		shopCharactersSection = null;
		shopBattleTilesSection = null;
		shopSkinsSection = null;
		battleShopBalanceText = null;
		battleShopOzTileBalanceText = null;
		battleShopAmetistBalanceText = null;
		battleShopEnergyBalanceText = null;
		battleShopStatusText = null;
		if (!num)
		{
			return;
		}
		EnsureBattleShopUi();
		if (!(battleShopRoot == null))
		{
			battleShopRoot.SetActive(value: true);
			battleShopRoot.transform.SetAsLastSibling();
			if (flag)
			{
				ShowBattleShopCharacters();
			}
			else if (flag2)
			{
				ShowBattleShopBattleTiles();
			}
			else if (flag3)
			{
				ShowBattleShopSkins();
			}
			else
			{
				ShowBattleShopEnergy();
			}
			ShowPendingBattleTilePackResult();
		}
	}

	private void RebuildWeeklyRewardsForLanguage()
	{
		bool num = weeklyRewardRoot != null && weeklyRewardRoot.activeSelf;
		DestroyObjectIfAlive(ref weeklyRewardRoot, immediate: false);
		weeklyRewardStatusText = null;
		weeklyRewardTodayText = null;
		weeklyRewardFreeButtonText = null;
		weeklyRewardAdButtonText = null;
		weeklyRewardFreeButton = null;
		weeklyRewardAdButton = null;
		weeklyRewardSlotImages = null;
		weeklyRewardIconImages = null;
		weeklyRewardSlotDayTexts = null;
		weeklyRewardSlotStateTexts = null;
		weeklyRewardSlotAmountTexts = null;
		weeklyRewardFreePreviewIcon = null;
		weeklyRewardAdPreviewIcon = null;
		if (!num)
		{
			RefreshWeeklyRewardUi();
			return;
		}
		EnsureWeeklyRewardUi();
		RefreshWeeklyRewardUi();
		if (weeklyRewardRoot != null)
		{
			weeklyRewardRoot.SetActive(value: true);
			weeklyRewardRoot.transform.SetAsLastSibling();
		}
	}

	private static void DestroyButtonObject(ref Button button, bool immediate)
	{
		if (!(button == null))
		{
			GameObject target = button.gameObject;
			button = null;
			DestroyGameObject(target, immediate);
		}
	}

	private static void DestroyObjectIfAlive(ref GameObject target, bool immediate)
	{
		if (!(target == null))
		{
			GameObject target2 = target;
			target = null;
			DestroyGameObject(target2, immediate);
		}
	}

	private static void DestroyGameObject(GameObject target, bool immediate)
	{
		if (!(target == null))
		{
			if (immediate && !Application.isPlaying)
			{
				UnityEngine.Object.DestroyImmediate(target);
			}
			else
			{
				UnityEngine.Object.Destroy(target);
			}
		}
	}

	private bool IsCharacterCarouselOpen()
	{
		if (characterCarouselRoot != null)
		{
			return characterCarouselRoot.activeInHierarchy;
		}
		return false;
	}

	private TMP_Text EnsureProgressText(Transform parent, TMP_Text current, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TextAlignmentOptions alignment)
	{
		if (current != null)
		{
			return current;
		}
		Transform transform = ((parent != null) ? parent.Find(objectName) : null);
		if (transform != null)
		{
			TMP_Text component = transform.GetComponent<TMP_Text>();
			if (component != null)
			{
				return component;
			}
		}
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component2 = obj.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0f, 1f);
		component2.anchoredPosition = anchoredPosition;
		component2.sizeDelta = sizeDelta;
		TextMeshProUGUI component3 = obj.GetComponent<TextMeshProUGUI>();
		component3.text = string.Empty;
		component3.fontSize = fontSize;
		component3.enableAutoSizing = true;
		component3.fontSizeMin = 14f;
		component3.fontSizeMax = fontSize;
		component3.alignment = alignment;
		component3.color = new Color(0.9f, 0.96f, 1f, 1f);
		component3.raycastTarget = false;
		return component3;
	}

	private Button EnsureEnergyAdButton(Transform parent, Button current, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta)
	{
		if (current != null)
		{
			current.onClick.RemoveListener(OnClickRewardedEnergyAd);
			current.onClick.AddListener(OnClickRewardedEnergyAd);
			return current;
		}
		Transform transform = ((parent != null) ? parent.Find(objectName) : null);
		if (transform != null)
		{
			Button component = transform.GetComponent<Button>();
			if (component != null)
			{
				component.onClick.RemoveListener(OnClickRewardedEnergyAd);
				component.onClick.AddListener(OnClickRewardedEnergyAd);
				return component;
			}
		}
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component2 = gameObject.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0f, 1f);
		component2.anchoredPosition = anchoredPosition;
		component2.sizeDelta = sizeDelta;
		Image component3 = gameObject.GetComponent<Image>();
		component3.color = new Color(0.18f, 0.24f, 0.16f, 0.96f);
		component3.raycastTarget = true;
		Button component4 = gameObject.GetComponent<Button>();
		component4.targetGraphic = component3;
		component4.onClick.AddListener(OnClickRewardedEnergyAd);
		TMP_Text tMP_Text = EnsureProgressText(gameObject.transform, null, "Label", Vector2.zero, new Vector2(-20f, 36f), 19f, TextAlignmentOptions.Center);
		tMP_Text.rectTransform.anchorMin = Vector2.zero;
		tMP_Text.rectTransform.anchorMax = Vector2.one;
		tMP_Text.rectTransform.offsetMin = new Vector2(10f, 4f);
		tMP_Text.rectTransform.offsetMax = new Vector2(-10f, -4f);
		tMP_Text.color = Color.white;
		return component4;
	}

	private void EnsureBattleShopUi()
	{
		if (!(battleShopRoot != null))
		{
			Canvas canvas = FindActiveSceneCanvas();
			if (!(canvas == null))
			{
				battleShopRoot = new GameObject("BattleShopOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
				battleShopRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
				ConfigureOverlayCanvas(battleShopRoot);
				RectTransform component = battleShopRoot.GetComponent<RectTransform>();
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.offsetMin = Vector2.zero;
				component.offsetMax = Vector2.zero;
				Image component2 = battleShopRoot.GetComponent<Image>();
				component2.color = new Color(0f, 0f, 0f, 0.68f);
				component2.raycastTarget = true;
				GameObject gameObject = CreateShopPanel(battleShopRoot.transform, "BattleShopPanel", new Vector2(1740f, 980f), new Vector2(0f, 0f), Color.white);
				RectTransform shopPanelRect = gameObject.transform as RectTransform;
				shopPanelRect.anchorMin = Vector2.zero;
				shopPanelRect.anchorMax = Vector2.one;
				shopPanelRect.pivot = new Vector2(0.5f, 0.5f);
				shopPanelRect.anchoredPosition = Vector2.zero;
				shopPanelRect.offsetMin = Vector2.zero;
				shopPanelRect.offsetMax = Vector2.zero;
				shopPanelRect.localScale = Vector3.one;
				TMP_Text shopTitleText = CreateShopText(gameObject.transform, "Title", BattleLobbyText("Боевой магазин", "Battle Shop", "Savaş Mağazası", "Battle Shop"), new Vector2(0f, 374f), new Vector2(1260f, 82f), 70f, TextAlignmentOptions.Center, Color.white);
				RectTransform shopTitleRect = shopTitleText.rectTransform;
				shopTitleRect.anchorMin = new Vector2(0.5f, 1f);
				shopTitleRect.anchorMax = new Vector2(0.5f, 1f);
				shopTitleRect.pivot = new Vector2(0.5f, 1f);
				shopTitleRect.anchoredPosition = new Vector2(0f, -34f);
				battleShopStatusText = CreateShopText(gameObject.transform, "Status", string.Empty, new Vector2(0f, -505f), new Vector2(1260f, 42f), 26f, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.86f, 1f));
				shopEnergyTabButton = CreateShopButton(gameObject.transform, "TabEnergy", BattleLobbyText("Энергия", "Energy", "Enerji", "Energie"), new Vector2(-910f, 168f), new Vector2(350f, 94f), Color.white, 38f);
				shopCharactersTabButton = CreateShopButton(gameObject.transform, "TabCharacters", BattleLobbyText("Герои", "Heroes", "Kahraman", "Helden"), new Vector2(-910f, 56f), new Vector2(350f, 94f), Color.white, 38f);
				shopBattleTilesTabButton = CreateShopButton(gameObject.transform, "TabBattleTiles", BattleLobbyText("Боевые камни", "Battle Tiles", "Savaş Taşları", "Kampfsteine"), new Vector2(-910f, -56f), new Vector2(350f, 94f), Color.white, 38f);
				if (MonetizationService.ArePurchasesSupported)
					shopSkinsTabButton = CreateShopButton(gameObject.transform, "TabAmetist", BattleLobbyText("Аметист", "Ametist", "Ametist", "Ametist"), new Vector2(-910f, -168f), new Vector2(350f, 94f), Color.white, 38f);
				shopEnergyTabButton.onClick.AddListener(ShowBattleShopEnergy);
				shopCharactersTabButton.onClick.AddListener(ShowBattleShopCharacters);
				shopBattleTilesTabButton.onClick.AddListener(ShowBattleShopBattleTiles);
				if (shopSkinsTabButton != null)
					shopSkinsTabButton.onClick.AddListener(ShowBattleShopSkins);
				shopEnergySection = CreateShopPanel(gameObject.transform, "EnergySection", new Vector2(1500f, 720f), new Vector2(210f, -96f), Color.white);
				CreateEnergyShopSectionHeader(shopEnergySection.transform);
				shopBuyEnergyButton = CreateEnergyShopButton(shopEnergySection.transform, "ButtonBuyEnergy", new Vector2(-365f, -45f), BattleLobbyText("Энергия за Аметист", "Ametist Energy", "Ametist Enerjisi", "Ametist-Energie"), shopEnergyAmetistPrice.ToString(), shopEnergyAmount.ToString(), LoadBattleLobbyAmetistIcon(), LoadBattleLobbyEnergyIcon());
				shopBuyEnergyButton.onClick.AddListener(OnClickBuyEnergyWithAmetist);
				shopRewardedEnergyButton = CreateEnergyShopButton(shopEnergySection.transform, "ButtonAdEnergy", new Vector2(365f, -45f), BattleLobbyText("Энергия за рекламу", "Ad Energy", "Reklam Enerjisi", "Werbe-Energie"), BattleLobbyText("РЕКЛАМА", "AD", "REKLAM", "ANZEIGE"), "+" + 20, null, LoadBattleLobbyEnergyIcon());
				shopRewardedEnergyButton.onClick.AddListener(OnClickRewardedEnergyAd);
				shopCharactersSection = CreateShopPanel(gameObject.transform, "CharactersSection", new Vector2(1500f, 720f), new Vector2(210f, -96f), Color.white);
				CreateDragonShopCard(shopCharactersSection.transform, "DragonMaleCard", BattleCharacterDatabase.GetLocalizedDisplayName("Dragon_Male", "Древний"), "Dragon_Male", new Vector2(-365f, -35f));
				CreateDragonShopCard(shopCharactersSection.transform, "DragonFemaleCard", BattleCharacterDatabase.GetLocalizedDisplayName("Dragon_Female", "Древняя"), "Dragon_Female", new Vector2(365f, -35f));
				shopBattleTilesSection = CreateShopPanel(gameObject.transform, "BattleTilesSection", new Vector2(1540f, 540f), new Vector2(210f, -46f), Color.white);
				shopBattleTileDailyAdButton = CreateBattleTilePackCard(shopBattleTilesSection.transform, BattleTilePackId.DailyAd, new Vector2(-570f, -55f));
				shopBattleTileMediumButton = CreateBattleTilePackCard(shopBattleTilesSection.transform, BattleTilePackId.OzTileMedium, new Vector2(-190f, -55f));
				shopBattleTileHighButton = CreateBattleTilePackCard(shopBattleTilesSection.transform, BattleTilePackId.OzTileHigh, new Vector2(190f, -55f));
				shopBattleTileAmetistButton = CreateBattleTilePackCard(shopBattleTilesSection.transform, BattleTilePackId.AmetistPremium, new Vector2(570f, -55f));
				shopBattleTileDailyAdButton.onClick.AddListener(OnClickOpenDailyBattleTilePack);
				shopBattleTileMediumButton.onClick.AddListener(OnClickOpenMediumBattleTilePack);
				shopBattleTileHighButton.onClick.AddListener(OnClickOpenHighBattleTilePack);
				shopBattleTileAmetistButton.onClick.AddListener(OnClickOpenAmetistBattleTilePack);
				if (MonetizationService.ArePurchasesSupported)
				{
					shopSkinsSection = CreateShopPanel(gameObject.transform, "AmetistSection", new Vector2(1500f, 540f), new Vector2(210f, -46f), Color.white);
					CreateShopText(shopSkinsSection.transform, "AmetistTitle", BattleLobbyText("Сокровищница Аметиста", "Ametist Treasury", "Ametist Hazinesi", "Ametist-Schatzkammer"), new Vector2(0f, 222f), new Vector2(960f, 58f), 50f, TextAlignmentOptions.Center, Color.white);
					shopAmetistSmallButton = CreateAmetistPackageButton(shopSkinsSection.transform, "AmetistSmall", "oz_ametist_small", new Vector2(-525f, -40f));
					shopAmetistMediumButton = CreateAmetistPackageButton(shopSkinsSection.transform, "AmetistMedium", "oz_ametist_medium", new Vector2(-175f, -40f));
					shopAmetistBigButton = CreateAmetistPackageButton(shopSkinsSection.transform, "AmetistBig", "oz_ametist_big", new Vector2(175f, -40f));
					shopAmetistLegendButton = CreateAmetistPackageButton(shopSkinsSection.transform, "AmetistLegend", "oz_ametist_legend", new Vector2(525f, -40f));
					shopAmetistSmallButton.onClick.AddListener(OnClickBuyAmetistSmall);
					shopAmetistMediumButton.onClick.AddListener(OnClickBuyAmetistMedium);
					shopAmetistBigButton.onClick.AddListener(OnClickBuyAmetistBig);
					shopAmetistLegendButton.onClick.AddListener(OnClickBuyAmetistLegend);
				}
				Button closeShopButton = CreateShopButton(gameObject.transform, "ButtonCloseShop", BattleLobbyText("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(785f, 392f), new Vector2(92f, 92f), Color.white, 40f);
				RectTransform closeShopRect = closeShopButton.transform as RectTransform;
				closeShopRect.anchorMin = new Vector2(1f, 1f);
				closeShopRect.anchorMax = new Vector2(1f, 1f);
				closeShopRect.pivot = new Vector2(1f, 1f);
				closeShopRect.anchoredPosition = new Vector2(-42f, -36f);
				closeShopButton.transform.SetAsLastSibling();
				closeShopButton.onClick.AddListener(CloseBattleShop);
				SetGameObjectActiveSafe(battleShopRoot, active: false);
				ApplyBattleLobbyTypography();
			}
		}
		Transform shopPanel = battleShopRoot != null ? battleShopRoot.transform.Find("BattleShopPanel") : null;
		if (shopPanel != null)
		{
			EnsureShopCurrencyRow(shopPanel);
			EnsureShopTopDivider(shopPanel);
			ApplyBattleShopTitleStyle(shopPanel);
			ApplyBattleShopSectionContainerStyle(shopEnergySection);
			ApplyBattleShopSectionContainerStyle(shopCharactersSection);
			ApplyBattleShopSectionContainerStyle(shopBattleTilesSection);
			ApplyBattleShopSectionContainerStyle(shopSkinsSection);
			ApplyDragonShopCardPresentation(shopCharactersSection != null ? shopCharactersSection.transform.Find("DragonMaleCard")?.gameObject : null, "Dragon_Male");
			ApplyDragonShopCardPresentation(shopCharactersSection != null ? shopCharactersSection.transform.Find("DragonFemaleCard")?.gameObject : null, "Dragon_Female");
		}
	}

	private static void ApplyBattleShopSectionContainerStyle(GameObject section)
	{
		if (section == null)
		{
			return;
		}
		Image image = section.GetComponent<Image>();
		if (image == null)
		{
			return;
		}
		image.sprite = null;
		image.color = Color.clear;
		image.type = Image.Type.Simple;
		image.preserveAspect = false;
		image.raycastTarget = false;
	}

	private void EnsureShopCurrencyRow(Transform parent)
	{
		Transform ozTileBlock = EnsureShopCurrencyBlock(parent, "ShopOzTileBalanceBlock", new Vector2(-520f, -180f));
		Transform ametistBlock = EnsureShopCurrencyBlock(parent, "ShopAmetistBalanceBlock", new Vector2(0f, -180f));
		Transform energyBlock = EnsureShopCurrencyBlock(parent, "ShopEnergyBalanceBlock", new Vector2(520f, -180f));

		EnsureShopCurrencyIcon(parent, ozTileBlock, "ShopOzTileIcon", LoadBattleLobbyOzTileIcon());
		battleShopOzTileBalanceText = EnsureShopCurrencyValue(parent, ozTileBlock, "ShopOzTileValue", 230f);
		EnsureShopCurrencyIcon(parent, ametistBlock, "ShopAmetistIcon", LoadBattleLobbyAmetistIcon());
		battleShopAmetistBalanceText = EnsureShopCurrencyValue(parent, ametistBlock, "ShopAmetistValue", 230f);
		EnsureShopCurrencyIcon(parent, energyBlock, "ShopEnergyIcon", LoadBattleLobbyEnergyIcon());
		battleShopEnergyBalanceText = EnsureShopCurrencyValue(parent, energyBlock, "ShopEnergyValue", 270f);
	}

	private static Transform EnsureShopCurrencyBlock(Transform parent, string objectName, Vector2 topPosition)
	{
		Transform block = parent.Find(objectName);
		if (block == null)
		{
			GameObject blockObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			blockObject.transform.SetParent(parent, worldPositionStays: false);
			block = blockObject.transform;
		}
		RectTransform rect = block as RectTransform;
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = topPosition;
		rect.sizeDelta = new Vector2(430f, 96f);
		Image image = block.GetComponent<Image>();
		if (image != null)
		{
			BattlePopupStyle.ApplyWindow(image, false);
			image.raycastTarget = false;
		}
		return block;
	}

	private static void EnsureShopTopDivider(Transform parent)
	{
		Transform existing = parent.Find("ShopTopDivider");
		Image divider = existing != null ? existing.GetComponent<Image>() : null;
		if (divider == null)
		{
			divider = CreateShopImage(parent, "ShopTopDivider", Vector2.zero, new Vector2(1840f, 12f), raycastTarget: false);
		}
		if (cachedBattleShopDividerSprite == null)
		{
			const string resourcePath = "Mahjong/Sprites/BattleLobbyUI/Divider";
			Texture2D texture = Resources.Load<Texture2D>(resourcePath);
			cachedBattleShopDividerSprite = texture != null
				? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect)
				: LoadResourceSprite(resourcePath);
		}
		divider.sprite = cachedBattleShopDividerSprite;
		divider.type = Image.Type.Simple;
		divider.preserveAspect = false;
		divider.color = Color.white;
		divider.raycastTarget = false;
		RectTransform rect = divider.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -116f);
		rect.sizeDelta = new Vector2(1840f, 12f);
		divider.transform.SetAsLastSibling();
	}

	private void ApplyBattleShopTitleStyle(Transform parent)
	{
		TMP_Text title = parent.Find("Title")?.GetComponent<TMP_Text>();
		if (title == null)
		{
			return;
		}
		ApplyBattleLobbyFontToText(title);
		title.fontStyle = FontStyles.Bold;
		title.enableVertexGradient = true;
		title.colorGradient = new VertexGradient(
			new Color(1f, 0.98f, 0.82f, 1f),
			new Color(1f, 0.9f, 0.55f, 1f),
			new Color(0.82f, 0.5f, 0.16f, 1f),
			new Color(1f, 0.72f, 0.28f, 1f));
		title.outlineWidth = 0.14f;
		title.outlineColor = new Color(0.04f, 0.018f, 0.006f, 1f);
		Shadow shadow = title.GetComponent<Shadow>();
		if (shadow == null)
		{
			shadow = title.gameObject.AddComponent<Shadow>();
		}
		shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
		shadow.effectDistance = new Vector2(2f, -2f);
		shadow.useGraphicAlpha = true;
		title.fontSharedMaterial = title.fontMaterial;
	}

	private static Image EnsureShopCurrencyIcon(Transform oldParent, Transform block, string objectName, Sprite sprite)
	{
		Transform iconTransform = block.Find(objectName) ?? oldParent.Find(objectName);
		Image image = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
		if (image == null)
		{
			image = CreateShopImage(block, objectName, Vector2.zero, new Vector2(58f, 58f), raycastTarget: false);
		}
		else
		{
			image.transform.SetParent(block, worldPositionStays: false);
		}
		image.sprite = sprite;
		RectTransform rect = image.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(-125f, 0f);
		rect.sizeDelta = new Vector2(58f, 58f);
		image.preserveAspect = true;
		image.raycastTarget = false;
		return image;
	}

	private static TMP_Text EnsureShopCurrencyValue(Transform oldParent, Transform block, string objectName, float width)
	{
		Transform textTransform = block.Find(objectName) ?? oldParent.Find(objectName);
		TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
		if (text == null)
		{
			text = CreateShopText(block, objectName, string.Empty, Vector2.zero, new Vector2(width, 62f), 44f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.58f, 1f));
		}
		else
		{
			text.transform.SetParent(block, worldPositionStays: false);
		}
		RectTransform rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.anchoredPosition = new Vector2(-76f, 0f);
		rect.sizeDelta = new Vector2(width, 62f);
		text.fontSize = 44f;
		text.fontSizeMax = 44f;
		text.alignment = TextAlignmentOptions.MidlineLeft;
		text.color = new Color(1f, 0.9f, 0.58f, 1f);
		return text;
	}

	private static void ApplyHeroBagTitleEffect(TMP_Text title)
	{
		if (title == null)
		{
			return;
		}
		BattlePopupStyle.ApplyFontOnly(title);
		title.enableVertexGradient = true;
		title.colorGradient = new VertexGradient(
			new Color(1f, 0.98f, 0.84f, 1f),
			new Color(1f, 0.9f, 0.56f, 1f),
			new Color(0.72f, 0.43f, 0.14f, 1f),
			new Color(0.96f, 0.72f, 0.28f, 1f));
		title.outlineWidth = 0.18f;
		title.outlineColor = new Color(0.05f, 0.018f, 0.004f, 0.98f);
		Shadow shadow = title.GetComponent<Shadow>();
		if (shadow == null)
		{
			shadow = title.gameObject.AddComponent<Shadow>();
		}
		shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
		shadow.effectDistance = new Vector2(3f, -3f);
		shadow.useGraphicAlpha = true;
	}

	private void EnsureBattleTileInventoryUi()
	{
		if (battleTileInventoryRoot != null)
		{
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (!(canvas == null))
		{
			battleTileInventoryRoot = new GameObject("BattleTileInventoryWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			battleTileInventoryRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
			ConfigureOverlayCanvas(battleTileInventoryRoot);
			RectTransform component = battleTileInventoryRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = battleTileInventoryRoot.GetComponent<Image>();
			component2.color = Color.black;
			component2.raycastTarget = true;
			GameObject gameObject = CreateShopPanel(battleTileInventoryRoot.transform, "BattleTileInventoryPanel", new Vector2(2140f, 1080f), Vector2.zero, Color.white);
			FitPanelInsideCanvas(gameObject.transform as RectTransform, canvas, 24f);
			TMP_Text heroBagTitle = CreateShopText(gameObject.transform, "Title", BattleLobbyText("Сумка героя", "Hero Bag", "Kahraman Çantası", "Heldentaşche"), new Vector2(0f, 446f), new Vector2(1520f, 82f), 62f, TextAlignmentOptions.Center, Color.white);
			ApplyHeroBagTitleEffect(heroBagTitle);
			battleTileInventoryStatusText = CreateShopText(gameObject.transform, "Status", string.Empty, new Vector2(300f, -474f), new Vector2(1260f, 48f), 30f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.58f, 1f));
			GameObject gameObject2 = CreateShopPanel(gameObject.transform, "HeroProfilePanel", new Vector2(650f, 900f), new Vector2(-700f, -16f), Color.white);
			battleTileInventoryHeroNameText = null;
			battleTileInventoryHeroPortraitImage = null;
			battleTileInventoryHeroDescriptionText = null;
			Image heroStatsPlate = CreateShopImage(gameObject2.transform, "TotemStatsPlate", new Vector2(0f, -70f), new Vector2(560f, 278f), raycastTarget: false);
			BattlePopupStyle.ApplyWindow(heroStatsPlate, false);
			TMP_Text heroStatsTitle = CreateShopText(gameObject2.transform, "TotemStatsTitle", BattleLobbyText("РЕЗОНАНС ТОТЕМА", "TOTEM RESONANCE", "TOTEM REZONANSI", "TOTEM-RESONANZ"), new Vector2(0f, 38f), new Vector2(470f, 32f), 25f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.42f, 1f));
			BattlePopupStyle.ApplyText(heroStatsTitle);
			heroStatsTitle.color = new Color(1f, 0.84f, 0.42f, 1f);
			battleTileInventoryHeroStatsText = CreateShopText(gameObject2.transform, "HeroStats", string.Empty, new Vector2(0f, -82f), new Vector2(510f, 190f), 25f, TextAlignmentOptions.Center, new Color(0.96f, 0.92f, 0.82f, 1f));
			battleTileInventoryHeroStatsText.richText = true;
			battleTileInventoryHeroStatsText.lineSpacing = 7f;
			battleTileInventoryHeroStatsText.enableAutoSizing = true;
			battleTileInventoryHeroStatsText.fontSizeMin = 20f;
			battleTileInventoryHeroStatsText.fontSizeMax = 25f;
			Image heroSkillPlate = CreateShopImage(gameObject2.transform, "TotemSkillPlate", new Vector2(0f, -326f), new Vector2(560f, 126f), raycastTarget: false);
			heroSkillPlate.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWideAlt");
			heroSkillPlate.enabled = heroSkillPlate.sprite != null;
			heroSkillPlate.type = Image.Type.Simple;
			heroSkillPlate.preserveAspect = false;
			heroSkillPlate.color = Color.white;
			TMP_Text heroSkillTitle = CreateShopText(gameObject2.transform, "TotemSkillTitle", BattleLobbyText("ДАР ТОТЕМА", "TOTEM GIFT", "TOTEM ARMAĞANI", "GABE DES TOTEMS"), new Vector2(0f, -300f), new Vector2(450f, 26f), 20f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.38f, 1f));
			BattlePopupStyle.ApplyText(heroSkillTitle);
			heroSkillTitle.color = new Color(1f, 0.82f, 0.38f, 1f);
			battleTileInventoryHeroSkillText = CreateShopText(gameObject2.transform, "HeroSkill", string.Empty, new Vector2(0f, -348f), new Vector2(480f, 48f), 22f, TextAlignmentOptions.Center, new Color(0.72f, 0.95f, 1f, 1f));
			battleTileInventoryHeroSkillText.textWrappingMode = TextWrappingModes.Normal;
			battleTileInventoryHeroSkillText.overflowMode = TextOverflowModes.Ellipsis;
			battleTileInventoryHeroSkillText.enableAutoSizing = true;
			battleTileInventoryHeroSkillText.fontSizeMin = 18f;
			battleTileInventoryHeroSkillText.fontSizeMax = 22f;
			GameObject gameObject3 = new GameObject("TotemPocket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			RectTransform component3 = gameObject3.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0.5f, 0.5f);
			component3.anchorMax = new Vector2(0.5f, 0.5f);
			component3.pivot = new Vector2(0.5f, 0.5f);
			component3.anchoredPosition = new Vector2(0f, 236f);
			component3.sizeDelta = new Vector2(230f, 285f);
			Image component4 = gameObject3.GetComponent<Image>();
			component4.color = Color.clear;
			component4.raycastTarget = true;
			totemTileInventoryPocketRect = gameObject3.transform as RectTransform;
			TMP_Text totemTitle = CreateShopText(gameObject3.transform, "Title", BattleLobbyText("ТОТЕМ ГЕРОЯ", "HERO TOTEM", "KAHRAMAN TOTEMİ", "HELDENTOTEM"), new Vector2(0f, 112f), new Vector2(220f, 34f), 27f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.62f, 1f));
			BattlePopupStyle.ApplyText(totemTitle);
			totemTitle.color = new Color(1f, 0.9f, 0.62f, 1f);
			battleTileInventoryTotemCountText = CreateShopText(gameObject3.transform, "Count", string.Empty, new Vector2(0f, 76f), new Vector2(210f, 24f), 18f, TextAlignmentOptions.Center, new Color(0.72f, 0.95f, 1f, 1f));
			Image totemSlotFrame = CreateShopImage(gameObject3.transform, "TotemSlotFrame", new Vector2(0f, -46f), new Vector2(210f, 206f), raycastTarget: false);
			if (!BattlePopupStyle.ApplyWindow(totemSlotFrame, false))
			{
				totemSlotFrame.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby");
				totemSlotFrame.color = Color.white;
				totemSlotFrame.preserveAspect = false;
			}
			totemTileInventoryContent = CreateTileInventoryContent(gameObject3.transform, "TotemTileSlot");
			ConfigureTileInventoryContentRect(totemTileInventoryContent as RectTransform, new Vector2(0f, -46f), new Vector2(156f, 172f));
			GameObject gameObject4 = CreateShopPanel(gameObject.transform, "ActivePocket", new Vector2(650f, 900f), new Vector2(-35f, -16f), Color.white);
			GameObject gameObject5 = CreateShopPanel(gameObject.transform, "ReservePocket", new Vector2(650f, 900f), new Vector2(630f, -16f), Color.white);
			activeTileInventoryPocketRect = gameObject4.transform as RectTransform;
			reserveTileInventoryPocketRect = gameObject5.transform as RectTransform;
			CreateShopText(gameObject4.transform, "Title", BattleLobbyText("Активные", "Active Deck", "Aktif Deste", "Aktives Deck"), new Vector2(0f, 382f), new Vector2(560f, 44f), 40f, TextAlignmentOptions.Center, Color.white);
			battleTileInventoryActiveCountText = CreateShopText(gameObject4.transform, "Count", string.Empty, new Vector2(0f, 344f), new Vector2(560f, 32f), 26f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.42f, 1f));
			CreateShopText(gameObject5.transform, "Title", BattleLobbyText("Запас", "Reserve", "Yedek", "Reserve"), new Vector2(0f, 382f), new Vector2(560f, 44f), 40f, TextAlignmentOptions.Center, Color.white);
			battleTileInventoryReserveCountText = CreateShopText(gameObject5.transform, "Count", string.Empty, new Vector2(0f, 344f), new Vector2(560f, 32f), 26f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.42f, 1f));
			activeTileInventoryContent = CreateTileInventoryScrollContent(gameObject4.transform, "ActiveTileGrid", new Vector2(0f, -22f), new Vector2(560f, 712f));
			reserveTileInventoryContent = CreateTileInventoryScrollContent(gameObject5.transform, "ReserveTileGrid", new Vector2(0f, -22f), new Vector2(560f, 712f));
			CreateShopButton(gameObject.transform, "ButtonCloseTileInventory", BattleLobbyText("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(978f, 456f), new Vector2(92f, 92f), Color.white, 32f).onClick.AddListener(CloseBattleTileInventory);
			SetGameObjectActiveSafe(battleTileInventoryRoot, active: false);
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.gameObject.SetActive(value: false);
			}
			ApplyBattleLobbyTypography();
		}
	}

	private static Transform CreateTileInventoryContent(Transform parent, string objectName)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = new Vector2(0f, -50f);
		component.sizeDelta = new Vector2(650f, 460f);
		return obj.transform;
	}

	private static void ConfigureTileInventoryContentRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		if (!(rect == null))
		{
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
		}
	}

	private static Transform CreateTileInventoryScrollContent(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject gameObject = new GameObject(objectName + "Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0f, 0f, 0f, 0f);
		component2.raycastTarget = true;
		GameObject gameObject2 = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component3 = gameObject2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.pivot = new Vector2(0.5f, 0.5f);
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		GameObject obj = new GameObject(objectName, typeof(RectTransform));
		obj.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = new Vector2(0.5f, 1f);
		component4.anchorMax = new Vector2(0.5f, 1f);
		component4.pivot = new Vector2(0.5f, 1f);
		component4.anchoredPosition = Vector2.zero;
		component4.sizeDelta = size;
		ScrollRect component5 = gameObject.GetComponent<ScrollRect>();
		component5.viewport = component3;
		component5.content = component4;
		component5.horizontal = false;
		component5.vertical = true;
		component5.movementType = ScrollRect.MovementType.Clamped;
		component5.inertia = true;
		component5.scrollSensitivity = 36f;
		return obj.transform;
	}

	private void RefreshBattleTileInventoryUi()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile == null)
		{
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.text = BattleLobbyText("Профиль еще загружается.", "Profile is still loading.", "Profil yukleniyor.", "Profil laedt noch.");
			}
			return;
		}
		bool flag = BattleTileInventoryService.EnsureDefaultInventory(playerProfile);
		if (battleTileStore != null)
		{
			BattleTileInventoryService.EnsureInventoryForStore(playerProfile, battleTileStore);
		}
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(playerProfile);
		if (orCreateInventory != null)
		{
			List<MahjongBattleTileStackData> reserveTileStacks = BattleTileInventoryService.GetReserveTileStacks(orCreateInventory);
			RebuildBattleTileInventoryPocket(activeTileInventoryContent, orCreateInventory.ActiveTileIds, battleTileStore, activePocket: true);
			RebuildReserveBattleTileCards(reserveTileInventoryContent, reserveTileStacks, battleTileStore);
			RebuildBattleTileTotemSlot(orCreateInventory, battleTileStore);
			RefreshBattleTileInventoryHeroPanel(playerProfile, battleTileStore);
			if (battleTileInventoryActiveCountText != null)
			{
				battleTileInventoryActiveCountText.text = $"{orCreateInventory.ActiveTileIds.Count}/{18}";
			}
			if (battleTileInventoryReserveCountText != null)
			{
				battleTileInventoryReserveCountText.text = reserveTileStacks.Count.ToString();
			}
			if (battleTileInventoryTotemCountText != null)
			{
				battleTileInventoryTotemCountText.text = string.IsNullOrWhiteSpace(orCreateInventory.TotemTileId)
					? BattleLobbyText("Слот свободен", "Slot available", "Yuva boş", "Slot frei")
					: BattleLobbyText("Камень установлен", "Tile attuned", "Taş yerleştirildi", "Stein eingesetzt");
			}
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.text = (flag ? BattleLobbyText("18 базовых battle-камней добавлены в активную колоду. Базовые камни не дают бонусов.", "18 base battle tiles were added to the active deck. Base tiles do not give bonuses.", "18 temel savaş taşı aktif desteye eklendi. Temel taşlar bonus vermez.", "18 Basis-Battle-Steine wurden dem aktiven Deck hinzugefuegt. Basis-Steine geben keine Boni.") : BattleLobbyText("Базовые камни не дают бонусов. Запас нужен для будущих особых камней.", "Base tiles give no bonuses. Reserve is for future special tiles.", "Temel taşlar bonus vermez. Yedek gelecekteki özel taşlar içindir.", "Basis-Steine geben keine Boni. Reserve ist fuer spaetere Spezial-Steine."));
			}
			if (flag && ProfileService.I != null)
			{
				ProfileService.I.Save();
				ProfileService.I.NotifyProfileChanged();
			}
		}
	}

	private void RebuildBattleTileInventoryPocket(Transform parent, List<string> tileIds, BattleTileStore store, bool activePocket)
	{
		if (!(parent == null))
		{
			for (int num = parent.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(parent.GetChild(num).gameObject);
			}
			if (tileIds == null || tileIds.Count == 0)
			{
				CreateShopText(parent, "Empty", BattleLobbyText("Пусто", "Empty", "Bos", "Leer"), Vector2.zero, new Vector2(560f, 80f), 42f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f, 1f));
			}
			else if (activePocket)
			{
				RebuildActiveBattleTileGrid(parent, tileIds, store);
			}
		}
	}

	private void RebuildActiveBattleTileGrid(Transform parent, List<string> tileIds, BattleTileStore store)
	{
		Vector2 size = new Vector2(128f, 164f);
		Vector2 vector = new Vector2(136f, -132f);
		float num = -204f;
		float num2 = -10f;
		int num3 = Mathf.CeilToInt((float)tileIds.Count / 4f);
		float num4 = Mathf.Max(712f, (float)num3 * Mathf.Abs(vector.y) + 64f);
		RectTransform rectTransform = parent as RectTransform;
		if (rectTransform != null)
		{
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, num4);
			rectTransform.anchoredPosition = Vector2.zero;
		}
		float num5 = num4 * 0.5f - 78f;
		for (int i = 0; i < tileIds.Count; i++)
		{
			string text = tileIds[i];
			BattleTileData data = null;
			if (store != null)
			{
				store.TryGetTileDataById(text, out data);
			}
			int num6 = i % 4;
			int num7 = i / 4;
			CreateTileInventorySlot(position: new Vector2(num + (float)num6 * vector.x, num5 + num2 + (float)num7 * vector.y), parent: parent, tileId: text, data: data, size: size, activePocket: true);
		}
	}

	private void RebuildReserveBattleTileCards(Transform parent, List<MahjongBattleTileStackData> tileStacks, BattleTileStore store)
	{
		if (parent == null)
		{
			return;
		}
		for (int i = parent.childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
		}
		if (tileStacks == null || tileStacks.Count == 0)
		{
			CreateShopText(parent, "Empty", BattleLobbyText("Пусто", "Empty", "Bos", "Leer"), Vector2.zero, new Vector2(560f, 80f), 42f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f, 1f));
			return;
		}
		Vector2 size = new Vector2(258f, 218f);
		Vector2 vector = new Vector2(272f, -228f);
		float num = -136f;
		float num2 = -8f;
		int num3 = Mathf.CeilToInt((float)tileStacks.Count / 2f);
		float num4 = Mathf.Max(712f, (float)num3 * Mathf.Abs(vector.y) + 36f);
		RectTransform rectTransform = parent as RectTransform;
		if (rectTransform != null)
		{
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, num4);
			rectTransform.anchoredPosition = Vector2.zero;
		}
		float num5 = num4 * 0.5f - 86f;
		for (int i = 0; i < tileStacks.Count; i++)
		{
			MahjongBattleTileStackData stack = tileStacks[i];
			string text = stack?.TileId ?? string.Empty;
			BattleTileData data = null;
			if (store != null)
			{
				store.TryGetTileDataById(text, out data);
			}
			int num6 = i % 2;
			int num7 = i / 2;
			CreateReserveTileInventoryCard(position: new Vector2(num + (float)num6 * vector.x, num5 + num2 + (float)num7 * vector.y), parent: parent, tileId: text, data: data, size: size, count: stack?.Count ?? 0, upgradeLevel: stack?.UpgradeLevel ?? 0);
		}
	}

	private void RebuildBattleTileTotemSlot(MahjongBattleTileInventoryData inventory, BattleTileStore store)
	{
		if (totemTileInventoryContent == null)
		{
			return;
		}
		for (int num = totemTileInventoryContent.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(totemTileInventoryContent.GetChild(num).gameObject);
		}
		string text = ((inventory != null) ? inventory.TotemTileId : string.Empty);
		if (string.IsNullOrWhiteSpace(text))
		{
			TMP_Text emptyTotem = CreateShopText(totemTileInventoryContent, "EmptyTotem", BattleLobbyText("ПЕРЕТАЩИ\nКАМЕНЬ", "DRAG A\nTILE", "TAŞI\nSÜRÜKLE", "STEIN\nZIEHEN"), Vector2.zero, new Vector2(164f, 78f), 22f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.42f, 1f));
			BattlePopupStyle.ApplyText(emptyTotem);
			emptyTotem.color = new Color(1f, 0.84f, 0.42f, 1f);
			return;
		}
		BattleTileData data = null;
		if (store != null)
		{
			store.TryGetTileDataById(text, out data);
		}
		CreateTileInventorySlot(totemTileInventoryContent, text, data, Vector2.zero, new Vector2(142f, 170f), activePocket: false);
	}

	private void RefreshBattleTileInventoryHeroPanel(PlayerProfile profile, BattleTileStore store)
	{
		BattleCharacterDatabase.BattleCharacterData battleCharacterData = (BattleCharacterSelectionService.HasInstance ? BattleCharacterSelectionService.Instance.GetSelectedCharacter() : null);
		if (battleTileInventoryHeroNameText != null)
		{
			battleTileInventoryHeroNameText.text = ((battleCharacterData != null) ? battleCharacterData.LocalizedDisplayName : BattleLobbyText("Герой не выбран", "No hero selected", "Kahraman yok", "Kein Held"));
		}
		if (battleTileInventoryHeroDescriptionText != null)
		{
			battleTileInventoryHeroDescriptionText.text = ((battleCharacterData != null) ? $"{battleCharacterData.AnimalType} {battleCharacterData.Gender}" : BattleLobbyText("Выбери героя, чтобы увидеть профиль.", "Select a hero to see the profile.", "Profili görmek için kahraman seç.", "Waehle einen Helden fuer das Profil."));
		}
		if (battleTileInventoryHeroPortraitImage != null)
		{
			Sprite sprite = ResolveDailyHeroPortrait(battleCharacterData);
			battleTileInventoryHeroPortraitImage.sprite = sprite;
			battleTileInventoryHeroPortraitImage.enabled = sprite != null;
		}
		BattleStatsHub.BattleStatsSnapshot baseStats = ((battleCharacterData != null) ? new BattleStatsHub.BattleStatsSnapshot(battleCharacterData.Stats.MaxHp, battleCharacterData.Stats.Attack, battleCharacterData.Stats.Armor, battleCharacterData.Stats.ParryChance, battleCharacterData.Stats.CritChance, battleCharacterData.Stats.CritDamageMultiplier) : new BattleStatsHub.BattleStatsSnapshot(1, 0, 0f, 0f, 0f, 1f));
		MahjongBattleCharacterProgressData mahjongBattleCharacterProgressData = ((battleCharacterData != null) ? BattleCharacterProgressionService.GetOrCreateProgress(profile, battleCharacterData.Id) : null);
		BattleStatsHub.BattleStatsSnapshot finalStats = BattleTileInventoryService.ApplyActiveTileBonuses(BattleCharacterProgressionService.ApplyProgression(baseStats, mahjongBattleCharacterProgressData), profile, store, battleCharacterData);
		if (battleTileInventoryHeroStatsText != null)
		{
			int num = mahjongBattleCharacterProgressData?.Level ?? 1;
			int num2 = mahjongBattleCharacterProgressData?.Experience ?? 0;
			int experienceRequiredForNextLevel = BattleCharacterProgressionService.GetExperienceRequiredForNextLevel(num);
			battleTileInventoryHeroStatsText.text = FormatTotemStatsBlock(num, num2, experienceRequiredForNextLevel, baseStats, finalStats);
		}
		if (battleTileInventoryHeroSkillText != null)
		{
			battleTileInventoryHeroSkillText.text = ResolveBattleTileInventorySkillText(profile, store);
		}
	}

	private void CreateTileInventorySlot(Transform parent, string tileId, BattleTileData data, Vector2 position, Vector2 size, bool activePocket)
	{
		if (!string.IsNullOrWhiteSpace(tileId))
		{
			Button button = CreateShopButton(parent, "Tile_" + tileId, string.Empty, position, size, Color.white, 1f);
			button.transition = Selectable.Transition.None;
			Image component = button.GetComponent<Image>();
			if (component != null)
			{
				component.sprite = null;
				component.color = Color.clear;
				component.raycastTarget = true;
			}
			Image image = CreateShopImage(button.transform, "Face", Vector2.zero, size * 0.86f, raycastTarget: false);
			image.sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
			image.enabled = image.sprite != null;
			if (!image.enabled)
			{
				CreateShopText(button.transform, "FaceFallback", ResolveBattleTileInventoryDisplayName(tileId), Vector2.zero, new Vector2(size.x * 0.78f, size.y * 0.56f), activePocket ? 34f : 46f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.48f, 1f));
			}
			PlayerProfile profile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
			int upgradeLevel = BattleTileInventoryService.GetUpgradeLevel(profile, tileId);
			CreateBattleTileUpgradePips(button.transform, Vector2.zero, image.rectTransform.sizeDelta, upgradeLevel);
			button.gameObject.AddComponent<BattleTileInventorySlotInteraction>().Configure(this, tileId, activePocket, upgradeLevel);
		}
	}

	private void CreateReserveTileInventoryCard(Transform parent, string tileId, BattleTileData data, Vector2 position, Vector2 size, int count, int upgradeLevel)
	{
		if (!string.IsNullOrWhiteSpace(tileId))
		{
			Button button = CreateShopButton(parent, "ReserveTileCard_" + tileId + "_L" + upgradeLevel, string.Empty, position, size, Color.white, 1f);
			button.transition = Selectable.Transition.ColorTint;
			Image image = CreateShopImage(button.transform, "Face", new Vector2(-72f, 22f), new Vector2(108f, 138f), raycastTarget: false);
			image.sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
			image.enabled = image.sprite != null;
			if (!image.enabled)
			{
				CreateShopText(button.transform, "FaceFallback", ResolveBattleTileInventoryDisplayName(tileId), new Vector2(-72f, 22f), new Vector2(104f, 92f), 28f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.48f, 1f));
			}
			BattleTileRarity battleTileRarity = ResolveBattleTileRarity(data);
			if (battleTileRarity != BattleTileRarity.Standard)
			{
				string value = ResolveBattleTileLocalizedName(tileId, data);
				TMP_Text tMP_Text = CreateShopText(button.transform, "Name", value, new Vector2(50f, 40f), new Vector2(132f, 76f), 28f, TextAlignmentOptions.Center, Color.white);
				tMP_Text.fontSizeMin = 16f;
				tMP_Text.fontSizeMax = 28f;
				tMP_Text.textWrappingMode = TextWrappingModes.Normal;
				tMP_Text.overflowMode = TextOverflowModes.Ellipsis;
			}
			Vector2 position2 = ((battleTileRarity == BattleTileRarity.Standard) ? new Vector2(50f, 0f) : new Vector2(50f, -34f));
			TMP_Text tMP_Text2 = CreateShopText(button.transform, "Rarity", ResolveBattleTileRarityDisplayName(battleTileRarity), position2, new Vector2(132f, 34f), 24f, TextAlignmentOptions.Center, ResolveBattleTileRarityColor(battleTileRarity));
			tMP_Text2.fontSizeMin = 15f;
			tMP_Text2.fontSizeMax = 24f;
			tMP_Text2.textWrappingMode = TextWrappingModes.NoWrap;
			tMP_Text2.overflowMode = TextOverflowModes.Ellipsis;
			CreateBattleTileUpgradePips(button.transform, new Vector2(-72f, 22f), image.rectTransform.sizeDelta, upgradeLevel);
			if (count > 1)
			{
				CreateShopText(button.transform, "StackCount", "x" + count, new Vector2(70f, -56f), new Vector2(84f, 34f), 22f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.42f, 1f));
			}
			button.gameObject.AddComponent<BattleTileInventorySlotInteraction>().Configure(this, tileId, newActivePocket: false, newUpgradeLevel: upgradeLevel);
		}
	}

	private static string ResolveBattleTileInventoryDisplayName(string tileId)
	{
		if (string.IsNullOrWhiteSpace(tileId))
		{
			return "?";
		}
		string text = tileId.Trim();
		if (text.StartsWith("battle_tile_", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring("battle_tile_".Length);
		}
		return text;
	}

	private static string NormalizeBattleTileId(string tileId)
	{
		if (string.IsNullOrWhiteSpace(tileId))
		{
			return string.Empty;
		}
		string text = tileId.Trim();
		if (!text.StartsWith("battle_tile_", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return text.Substring("battle_tile_".Length);
	}

	private string ResolveBattleTileLocalizedName(string tileId, BattleTileData data)
	{
		string text = ((!string.IsNullOrWhiteSpace(data?.DisplayName)) ? data.DisplayName.Trim() : ResolveBattleTileInventoryDisplayName(tileId));
		return NormalizeBattleTileId(tileId) switch
		{
			"29" => BattleLobbyText("Рубиновый гребень феникса", text, "Yakut Anka Armasi", "Rubin-Phonixwappen"), 
			"30" => BattleLobbyText("Сапфировая ледяная корона", text, "Safir Buz Taci", "Saphir-Frostkrone"), 
			"31" => BattleLobbyText("Изумрудный щит змея", text, "Zumrut Yilan Kalkanı", "Smaragd-Schlangenschild"), 
			"32" => BattleLobbyText("Аметистовое око пустоты", text, "Ametist Bosluk Gozu", "Amethyst-Auge der Leere"), 
			"33" => BattleLobbyText("Золотая маска дракона", text, "Altın Ejder Maskesi", "Goldene Drachenmaske"), 
			"34" => BattleLobbyText("Багровая маска демона", text, "Kızıl Iblis Maskesi", "Purpurne Daemonenmaske"), 
			"35" => BattleLobbyText("Циановый кристальный меч", text, "Camgobegi Kristal Kılıç", "Cyan-Kristallschwert"), 
			"37" => BattleLobbyText("Золотое солнце льва", text, "Altın Aslan Güneşi", "Goldene Loewensonne"), 
			"38" => BattleLobbyText("Бирюзовый якорь кракена", text, "Turkuaz Kraken Capasi", "Tuerkiser Krakenanker"), 
			"39" => BattleLobbyText("Белое святое крылатое копье", text, "Beyaz Kutsal Kanat Mizragi", "Weisser heiliger Fluegelspeer"), 
			"40" => BattleLobbyText("Зеленый фонарь некроманта", text, "Yeşil Nekromant Feneri", "Gruene Nekromantenlaterne"), 
			"41" => BattleLobbyText("Синий громовой молот", text, "Mavi Gökgurultusu Cekici", "Blauer Donnerhammer"), 
			"42" => BattleLobbyText("Реликвия розового кинжала", text, "Gul Hançer Yadigari", "Rosendolch-Relikt"), 
			"43" => BattleLobbyText("Щит грифона", text, "Grifon Kalkanı", "Gryphonschild"), 
			"44" => BattleLobbyText("Фиолетовый ключ портала", text, "Mor Portal Anahtari", "Violetter Portalschluessel"), 
			"45" => BattleLobbyText("Нефритовый лотосовый череп", text, "Yesim Lotus Kafataşı", "Jade-Lotusschaedel"), 
			"46" => BattleLobbyText("Призматическая кристальная реликвия", text, "Prizmatik Kristal Yadigar", "Prismatisches Kristallrelikt"), 
			"47" => BattleLobbyText("Небесные рога оленя", text, "Göksel Geyik Boynuzlari", "Himmlisches Hirschgeweih"), 
			"48" => BattleLobbyText("Лавовая рукавица титана", text, "Magma Titan Eldiveni", "Magma-Titanenhandschuh"), 
			"49" => BattleLobbyText("Коготь штормового орла", text, "Firtina Kartali Pencesi", "Sturm-Adlerkralle"), 
			"50" => BattleLobbyText("Теневая коса песочных часов", text, "Gölge Orak Kumsaati", "Schatten-Sensenstundenglas"), 
			"51" => BattleLobbyText("Коралловый трезубец раковины", text, "Mercan Deniz Kabugu Uclu Mizragi", "Korallenmuschel-Dreizack"), 
			"52" => BattleLobbyText("Нефритовая маска самурая", text, "Yesim Samuray Maskesi", "Jade-Samuraimaske"), 
			"53" => BattleLobbyText("Бронзовый топор минотавра", text, "Bronz Minotor Baltaşı", "Bronzene Minotaurenaxt"), 
			"54" => BattleLobbyText("Серебряный паучий кинжал", text, "Gümüş Örümcek Hançeri", "Silberner Spinnendolch"), 
			"55" => BattleLobbyText("Лотосовый лунный посох", text, "Lotus Ay Asası", "Lotus-Mondstab"), 
			"56" => BattleLobbyText("Лук кровавой луны", text, "Kanli Ay Yayı", "Blutmondbogen"), 
			"57" => BattleLobbyText("Маска чумного ворона", text, "Veba Kuzgun Maskesi", "Pestdoktor-Rabenmaske"), 
			"58" => BattleLobbyText("Солнечный жреческий анкх", text, "Güneş Rahibi Ankh", "Sonnenpriester-Ankh"), 
			"59" => BattleLobbyText("Топор ледяного волка", text, "Buz Kurt Baltaşı", "Frostwolf-Axt"), 
			"60" => BattleLobbyText("Ониксовый гробовой щит", text, "Oniks Tabut Kalkanı", "Onyx-Sargschild"), 
			"61" => BattleLobbyText("Изумрудный рог друида", text, "Zumrut Druid Boynuzu", "Smaragd-Druidenhorn"), 
			"62" => BattleLobbyText("Наконечник кометного копья", text, "Kuyruklu Yıldız Mızrak Ucu", "Kometen-Speerspitze"), 
			"63" => BattleLobbyText("Королевское копье шахматного коня", text, "Kraliyet Satranç Atı Mızrağı", "Koenigliche Schachritterlanze"), 
			"64" => BattleLobbyText("Багровая чаша вампира", text, "Kızıl Vampir Kadehi", "Purpurner Vampirkelch"), 
			_ => text, 
		};
	}

	private string ResolveBattleTileLocalizedDescription(string tileId, BattleTileData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.Description))
		{
			return string.Empty;
		}
		string text = ResolveBattleTileLocalizedName(tileId, data);
		return BattleLobbyText("Особый battle-камень «" + text + "». Его сила раскрывается в пассивном бонусе, активном ударе пары и симбиозе с подходящей природой героя.", data.Description.Trim(), text + " özel bir savaş taşıdır. Gücü pasif bonusta, eşleşme vuruşunda ve kahramanın doğasıyla kurulan simbiyozda açılır.", text + " ist ein besonderer Battle-Stein. Seine Kraft wirkt als passiver Bonus, beim Paar-Treffer und in Symbiose mit der passenden Heldennatur.");
	}

	private string ResolveBattleTileLocalizedSkillName(string tileId, BattleTileData data)
	{
		string english = ((!string.IsNullOrWhiteSpace(data?.Skill?.Name)) ? data.Skill.Name.Trim() : ResolveBattleTileLocalizedName(tileId, data));
		return BattleLobbyText("Умение " + ResolveBattleTileLocalizedName(tileId, data), english, ResolveBattleTileLocalizedName(tileId, data) + " yetenegi", ResolveBattleTileLocalizedName(tileId, data) + " Skill");
	}

	private string ResolveBattleTileLocalizedSkillDescription(string tileId, BattleTileData data)
	{
		string text = ((!string.IsNullOrWhiteSpace(data?.Skill?.Description)) ? data.Skill.Description.Trim() : string.Empty);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string text2 = ResolveBattleTileLocalizedName(tileId, data);
		return BattleLobbyText("Когда этот камень ведет комбинацию, «" + text2 + "» усиливает активный удар и раскрывает свой особый ритм боя.", text, "Bu taş kombinasyonu yönettiğinde " + text2 + " aktif vuruşunu güçlendirir ve kendi savaş ritmini açar.", "Wenn dieser Stein die Kombination fuehrt, staerkt " + text2 + " den aktiven Schlag und oeffnet seinen eigenen Kampfrhythmus.");
	}

	private static int CountBattleTilesByRarity(List<string> tileIds, BattleTileStore store, BattleTileRarity rarity)
	{
		if (tileIds == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < tileIds.Count; i++)
		{
			BattleTileData data = null;
			if (store != null)
			{
				store.TryGetTileDataById(tileIds[i], out data);
			}
			if (ResolveBattleTileRarity(data) == rarity)
			{
				num++;
			}
		}
		return num;
	}

	private static BattleTileRarity ResolveBattleTileRarity(BattleTileData data)
	{
		return data?.Rarity ?? BattleTileRarity.Standard;
	}

	private string ResolveBattleTileRarityDisplayName(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Common => BattleLobbyText("Обычные", "Common", "Siradan", "Gewoehnlich"), 
			BattleTileRarity.Rare => BattleLobbyText("Редкие", "Rare", "Nadir", "Selten"), 
			BattleTileRarity.Epic => BattleLobbyText("Эпические", "Epic", "Epik", "Episch"), 
			BattleTileRarity.Legendary => BattleLobbyText("Легендарные", "Legendary", "Efsanevi", "Legendaer"), 
			BattleTileRarity.Mythic => BattleLobbyText("Мифические", "Mythic", "Mitik", "Mythisch"), 
			_ => BattleLobbyText("Базовый камень", "Base tile", "Basit taş", "Basisstein"), 
		};
	}

	private static Color ResolveBattleTileRarityColor(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Common => new Color(0.84f, 0.88f, 0.92f, 1f), 
			BattleTileRarity.Rare => new Color(0.38f, 0.74f, 1f, 1f), 
			BattleTileRarity.Epic => new Color(0.72f, 0.43f, 1f, 1f), 
			BattleTileRarity.Legendary => new Color(1f, 0.72f, 0.22f, 1f), 
			BattleTileRarity.Mythic => new Color(1f, 0.28f, 0.34f, 1f), 
			_ => new Color(1f, 0.86f, 0.42f, 1f), 
		};
	}

	private string FormatStatsBlock(BattleStatsHub.BattleStatsSnapshot stats)
	{
		return BattleStatIconProvider.ValueWithIconGap($"HP: {stats.MaxHp}") + "   " + BattleStatIconProvider.ValueWithIconGap($"ATK: {stats.Attack}") + "\n" + BattleStatIconProvider.ValueWithIconGap($"Armor: {Mathf.RoundToInt(stats.Armor * 100f)}%") + "   " + BattleStatIconProvider.ValueWithIconGap($"Crit: {Mathf.RoundToInt(stats.CritChance * 100f)}%") + "\n" + BattleStatIconProvider.ValueWithIconGap($"Crit Power: x{stats.CritDamageMultiplier:0.##}");
	}

	private string FormatCompactStatsBlock(BattleStatsHub.BattleStatsSnapshot stats)
	{
		return $"HP {stats.MaxHp}  ATK {stats.Attack}  Armor {Mathf.RoundToInt(stats.Armor * 100f)}%\n" + $"Crit {Mathf.RoundToInt(stats.CritChance * 100f)}%  CP x{stats.CritDamageMultiplier:0.##}";
	}

	private string FormatCombinedStatsBlock(BattleStatsHub.BattleStatsSnapshot baseStats, BattleStatsHub.BattleStatsSnapshot finalStats)
	{
		int num = Mathf.Max(0, finalStats.MaxHp - baseStats.MaxHp);
		int num2 = Mathf.Max(0, finalStats.Attack - baseStats.Attack);
		int num3 = Mathf.RoundToInt(Mathf.Max(0f, finalStats.Armor - baseStats.Armor) * 100f);
		int num5 = Mathf.RoundToInt(Mathf.Max(0f, finalStats.CritChance - baseStats.CritChance) * 100f);
		float num6 = Mathf.Max(0f, finalStats.CritDamageMultiplier - baseStats.CritDamageMultiplier);
		return $"HP: {finalStats.MaxHp} (+{num})\n" + $"ATK: {finalStats.Attack} (+{num2})\n" + $"Armor: {Mathf.RoundToInt(finalStats.Armor * 100f)}% (+{num3}%)\n" + $"Crit: {Mathf.RoundToInt(finalStats.CritChance * 100f)}% (+{num5}%)\n" + $"CP: x{finalStats.CritDamageMultiplier:0.##} (+{num6:0.##})";
	}

	private string FormatTotemStatsBlock(int level, int experience, int experienceRequired, BattleStatsHub.BattleStatsSnapshot baseStats, BattleStatsHub.BattleStatsSnapshot finalStats)
	{
		int hpBonus = Mathf.Max(0, finalStats.MaxHp - baseStats.MaxHp);
		int attackBonus = Mathf.Max(0, finalStats.Attack - baseStats.Attack);
		int armorBonus = Mathf.RoundToInt(Mathf.Max(0f, finalStats.Armor - baseStats.Armor) * 100f);
		int critBonus = Mathf.RoundToInt(Mathf.Max(0f, finalStats.CritChance - baseStats.CritChance) * 100f);
		float critPowerBonus = Mathf.Max(0f, finalStats.CritDamageMultiplier - baseStats.CritDamageMultiplier);
		string levelLabel = BattleLobbyText("Уровень", "Level", "Seviye", "Stufe");
		string experienceLabel = BattleLobbyText("Опыт", "Experience", "Deneyim", "Erfahrung");
		string healthLabel = BattleLobbyText("Здоровье", "Health", "Sağlık", "Gesundheit");
		string attackLabel = BattleLobbyText("Атака", "Attack", "Saldırı", "Angriff");
		string armorLabel = BattleLobbyText("Броня", "Armor", "Zırh", "Rüstung");
		string critLabel = BattleLobbyText("Крит. шанс", "Crit chance", "Kritik şansı", "Krit-Chance");
		string critPowerLabel = BattleLobbyText("Сила крита", "Crit power", "Kritik gücü", "Krit-Kraft");
		return $"<color=#F4D36F>{levelLabel} {level}</color>   <color=#91A9BD>•</color>   {experienceLabel} {experience}/{experienceRequired}\n"
			+ $"{healthLabel} <b>{finalStats.MaxHp}</b>{FormatTotemBonus(hpBonus)}   <color=#8B7555>•</color>   {attackLabel} <b>{finalStats.Attack}</b>{FormatTotemBonus(attackBonus)}\n"
			+ $"{armorLabel} <b>{Mathf.RoundToInt(finalStats.Armor * 100f)}%</b>{FormatTotemBonus(armorBonus, "%")}   <color=#8B7555>•</color>   {critLabel} <b>{Mathf.RoundToInt(finalStats.CritChance * 100f)}%</b>{FormatTotemBonus(critBonus, "%")}\n"
			+ $"{critPowerLabel} <b>×{finalStats.CritDamageMultiplier:0.##}</b>{FormatTotemBonus(critPowerBonus)}";
	}

	private static string FormatTotemBonus(int value, string suffix = "")
	{
		return value > 0 ? $" <color=#73DFA0>+{value}{suffix}</color>" : string.Empty;
	}

	private static string FormatTotemBonus(float value)
	{
		return value > 0.001f ? $" <color=#73DFA0>+{value:0.##}</color>" : string.Empty;
	}

	private string FormatStatsDeltaBlock(BattleStatsHub.BattleStatsSnapshot baseStats, BattleStatsHub.BattleStatsSnapshot finalStats)
	{
		return BattleStatIconProvider.ValueWithIconGap($"+HP: {Mathf.Max(0, finalStats.MaxHp - baseStats.MaxHp)}") + "   " + BattleStatIconProvider.ValueWithIconGap($"+ATK: {Mathf.Max(0, finalStats.Attack - baseStats.Attack)}") + "\n" + BattleStatIconProvider.ValueWithIconGap($"+Armor: {Mathf.RoundToInt(Mathf.Max(0f, finalStats.Armor - baseStats.Armor) * 100f)}%") + "   " + BattleStatIconProvider.ValueWithIconGap($"+Crit: {Mathf.RoundToInt(Mathf.Max(0f, finalStats.CritChance - baseStats.CritChance) * 100f)}%") + "\n" + BattleStatIconProvider.ValueWithIconGap($"+Crit Power: {Mathf.Max(0f, finalStats.CritDamageMultiplier - baseStats.CritDamageMultiplier):0.##}");
	}

	private void ApplyInventoryHeroStatsIcons(TMP_Text text, bool hasHeaderLine)
	{
		RectTransform rectTransform = ((text != null) ? text.rectTransform : null);
		if (!(rectTransform == null))
		{
			float num = (hasHeaderLine ? 30f : 54f);
			Vector2 size = new Vector2(24f, 24f);
			BattleStatIconProvider.ShowIcon(rectTransform, "HeroHpIcon", BattleStatIconKind.Hp, new Vector2(-254f, num), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "HeroAttackIcon", BattleStatIconKind.Attack, new Vector2(18f, num), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "HeroArmorIcon", BattleStatIconKind.Armor, new Vector2(-254f, num - 30f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "HeroCriticalIcon", BattleStatIconKind.Critical, new Vector2(18f, num - 30f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "HeroCriticalPowerIcon", BattleStatIconKind.CriticalPower, new Vector2(-254f, num - 60f), size);
		}
	}

	private string ResolveBattleTileInventorySkillText(PlayerProfile profile, BattleTileStore store)
	{
		BattleTileData totemTileData = BattleTileInventoryService.GetTotemTileData(profile, store);
		if (totemTileData?.Skill == null || !totemTileData.Skill.HasSkill())
		{
			return BattleLobbyText("Способность пока не пробуждена", "The gift is not awakened yet", "Yetenek henüz uyanmadı", "Die Gabe ist noch nicht erwacht");
		}
		return ResolveBattleTileLocalizedSkillName(totemTileData.Id, totemTileData);
	}

	private void OnBattleTileSlotClick(string tileId, bool fromActivePocket, int upgradeLevel, int clickCount)
	{
		if (clickCount >= 2)
		{
			CancelBattleTileProfileClick();
			OnClickMoveBattleTile(tileId, fromActivePocket);
		}
		else
		{
			CancelBattleTileProfileClick();
			battleTileProfileClickRoutine = StartCoroutine(OpenBattleTileProfileDelayed(tileId, upgradeLevel));
		}
	}

	private IEnumerator OpenBattleTileProfileDelayed(string tileId, int upgradeLevel)
	{
		yield return new WaitForSecondsRealtime(0.32f);
		battleTileProfileClickRoutine = null;
		OpenBattleTileProfile(tileId, upgradeLevel);
	}

	private void CancelBattleTileProfileClick()
	{
		if (battleTileProfileClickRoutine != null)
		{
			StopCoroutine(battleTileProfileClickRoutine);
			battleTileProfileClickRoutine = null;
		}
	}

	private void OnBattleTileSlotDragStarted()
	{
		CancelBattleTileProfileClick();
		CloseBattleTileProfile();
	}

	private bool TryDropBattleTile(string tileId, bool fromActivePocket, Vector2 screenPosition, Camera eventCamera)
	{
		if (totemTileInventoryPocketRect != null && RectTransformUtility.RectangleContainsScreenPoint(totemTileInventoryPocketRect, screenPosition, eventCamera))
		{
			OnDropBattleTileToTotem(tileId);
			return true;
		}
		RectTransform rectTransform = ((!fromActivePocket) ? ((activeTileInventoryPocketRect != null) ? activeTileInventoryPocketRect : (activeTileInventoryContent as RectTransform)) : ((reserveTileInventoryPocketRect != null) ? reserveTileInventoryPocketRect : (reserveTileInventoryContent as RectTransform)));
		if (rectTransform == null || !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera))
		{
			return false;
		}
		OnClickMoveBattleTile(tileId, fromActivePocket);
		return true;
	}

	private void OnDropBattleTileToTotem(string tileId)
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile != null && !(battleTileStore == null) && !string.IsNullOrWhiteSpace(tileId))
		{
			string reason;
			bool flag = BattleTileInventoryService.TrySetTotemTile(playerProfile, battleTileStore, tileId, out reason);
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.text = (flag ? BattleLobbyText("Камень установлен в слот тотема.", "Tile equipped in the totem slot.", "Taş totem slotuna takildi.", "Stein im Totem-Slot ausgeruestet.") : ResolveBattleTileInventoryReason(reason));
			}
			if (flag && ProfileService.I != null)
			{
				ProfileService.I.Save();
				ProfileService.I.NotifyProfileChanged();
				RefreshBattleTileInventoryUi();
				BattleLoreTutorialUI.NotifyTotemEquippedFromLobby(tileId);
			}
		}
	}

	private void OpenBattleTileProfile(string tileId, int upgradeLevel)
	{
		CloseBattleTileProfile();
		if (string.IsNullOrWhiteSpace(tileId))
		{
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		Transform transform = ((battleTileInventoryRoot != null) ? battleTileInventoryRoot.transform : ((canvas != null) ? canvas.transform : null));
		if (!(transform == null))
		{
			BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
			BattleTileData data = null;
			if (battleTileStore != null)
			{
				battleTileStore.TryGetTileDataById(tileId, out data);
			}
			battleTileProfileRoot = new GameObject("BattleTileProfileWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			battleTileProfileRoot.transform.SetParent(transform, worldPositionStays: false);
			RectTransform component = battleTileProfileRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = battleTileProfileRoot.GetComponent<Image>();
			component2.color = Color.black;
			component2.raycastTarget = true;
			GameObject gameObject = CreateShopPanel(battleTileProfileRoot.transform, "BattleTileProfilePanel", new Vector2(2140f, 1080f), Vector2.zero, Color.white);
			if (canvas != null)
			{
				FitPanelInsideCanvas(gameObject.transform as RectTransform, canvas, 24f);
			}
			TMP_Text tMP_Text = CreateShopText(gameObject.transform, "Title", ResolveBattleTileProfileTitle(tileId, data), new Vector2(0f, 404f), new Vector2(1640f, 108f), 78f, TextAlignmentOptions.Center, Color.white);
			tMP_Text.enableAutoSizing = true;
			tMP_Text.fontSizeMin = 46f;
			tMP_Text.fontSizeMax = 82f;
			tMP_Text.textWrappingMode = TextWrappingModes.NoWrap;
			tMP_Text.overflowMode = TextOverflowModes.Ellipsis;
			Image image = CreateShopImage(gameObject.transform, "BigTileFace", new Vector2(-620f, 88f), new Vector2(420f, 540f), raycastTarget: false);
			image.sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
			image.enabled = image.sprite != null;
			int displayedUpgradeLevel = Mathf.Max(0, upgradeLevel);
			CreateBattleTileUpgradePips(gameObject.transform, new Vector2(-620f, 88f), image.rectTransform.sizeDelta, displayedUpgradeLevel);
			if (!image.enabled)
			{
				CreateShopText(gameObject.transform, "BigTileFallback", ResolveBattleTileInventoryDisplayName(tileId), new Vector2(-620f, 88f), new Vector2(420f, 300f), 104f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.48f, 1f));
			}
			TMP_Text tMP_Text2 = CreateShopText(gameObject.transform, "Description", ResolveBattleTileProfileDescription(tileId, data, displayedUpgradeLevel), new Vector2(280f, -4f), new Vector2(1120f, 620f), 52f, TextAlignmentOptions.Left, new Color(0.95f, 0.92f, 0.84f, 1f));
			tMP_Text2.enableAutoSizing = true;
			tMP_Text2.fontSizeMin = 36f;
			tMP_Text2.fontSizeMax = 56f;
			tMP_Text2.textWrappingMode = TextWrappingModes.Normal;
			tMP_Text2.overflowMode = TextOverflowModes.Ellipsis;
			tMP_Text2.lineSpacing = 4f;
			CreateShopButton(gameObject.transform, "ButtonCloseTileProfile", BattleLobbyText("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(976f, 434f), new Vector2(118f, 118f), Color.white, 34f).onClick.AddListener(CloseBattleTileProfile);
			battleTileProfileRoot.transform.SetAsLastSibling();
			ApplyBattleLobbyTypography();
		}
	}

	private string ResolveBattleTileProfileTitle(string tileId, BattleTileData data)
	{
		string text = ResolveBattleTileLocalizedName(tileId, data);
		if (!string.IsNullOrWhiteSpace(text))
		{
			BattleTileRarity battleTileRarity = data?.Rarity ?? BattleTileRarity.Standard;
			string text2 = ((battleTileRarity == BattleTileRarity.Standard) ? string.Empty : (" [" + ResolveBattleTileRarityDisplayName(battleTileRarity) + "]"));
			return text.Trim() + text2;
		}
		return ResolveBattleTileInventoryDisplayName(tileId);
	}

	private string ResolveBattleTileProfileDescription(string tileId, BattleTileData data, int upgradeLevel)
	{
		string text = ResolveBattleTileLocalizedDescription(tileId, data);
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.Replace("battle-камень", "боевой камень").Replace("Battle-камень", "Боевой камень");
		}
		if (data != null && !string.IsNullOrWhiteSpace(text))
		{
			PlayerProfile profile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
			upgradeLevel = Mathf.Max(0, upgradeLevel);
			int ownedCount = BattleTileInventoryService.GetOwnedCount(profile, tileId, upgradeLevel);
			float forgeMultiplier = 1f + (float)upgradeLevel * BattleTileInventoryService.ForgeBonusGrowthPerLevel;
			float powerMultiplier = BattleTileInventoryService.GetRarityPowerMultiplier(data.Rarity) * forgeMultiplier;
			string text2 = ResolveBattleTileBonusText(data.PassiveBonus, powerMultiplier);
			string text3 = ResolveBattleTileActiveBonusText(data.ActiveBonus, powerMultiplier);
			string text4 = ResolveBattleTileBonusText(data.SymbiosisBonus, powerMultiplier);
			string text5 = ResolveBattleTileSymbiosisBreedsText(data);
			List<string> sections = new List<string>();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				sections.Add("<color=#F4D36F>" + BattleLobbyText("ПАССИВНЫЙ ЭФФЕКТ", "PASSIVE EFFECT", "PASİF ETKİ", "PASSIVER EFFEKT") + "</color>\n" + text2);
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				sections.Add("<color=#F4D36F>" + BattleLobbyText("ЭФФЕКТ ПАРЫ", "MATCH EFFECT", "EŞLEŞME ETKİSİ", "PAAR-EFFEKT") + "</color>\n" + text3);
			}
			sections.Add("<color=#F4D36F>" + BattleLobbyText("УЛУЧШЕНИЕ", "UPGRADE", "GELİŞTİRME", "VERBESSERUNG") + "</color>\n"
				+ BattleLobbyText("Уровень", "Level", "Seviye", "Stufe") + $" +{upgradeLevel}  •  "
				+ BattleLobbyText("Копии этого уровня", "Copies at this level", "Bu seviyedeki kopyalar", "Kopien dieser Stufe") + $" {ownedCount}/{BattleTileInventoryService.ForgeRequiredCopies}\n"
				+ BattleLobbyText("Множитель силы", "Power multiplier", "Güç çarpanı", "Kraftmultiplikator") + $": x{powerMultiplier:0.00}  •  "
				+ BattleLobbyText("Бонус кузницы", "Forge bonus", "Dövme bonusu", "Schmiedebonus") + $": +{upgradeLevel * 10}%");
			if (!string.IsNullOrWhiteSpace(text4))
			{
				sections.Add("<color=#F4D36F>" + BattleLobbyText("СИМБИОЗ", "SYMBIOSIS", "SİMBİYOZ", "SYMBIOSE") + " — " + text5 + "</color>\n" + text4);
			}
			return (text.Trim() + "\n\n" + string.Join("\n\n", sections)).Trim();
		}
		if (BattleTileInventoryService.IsBaseBattleTile(tileId))
		{
			return BattleLobbyText("Базовый battle-камень. Используется в битве и не дает бонусов.", "Base battle tile. Used in battle and gives no bonuses.", "Temel savaş taşı. Savaş için kullanılır ve bonus vermez.", "Basis-Battle-Stein. Wird im Kampf benutzt und gibt keine Boni.");
		}
		return BattleLobbyText("Особый battle-камень. Его бонус будет показан здесь.", "Special battle tile. Its bonus will be shown here.", "Özel savaş taşı. Bonusu burada gösterilecek.", "Spezial-Battle-Stein. Sein Bonus wird hier angezeigt.");
	}

	private string ResolveBattleTileBonusText(BattleTileBonusData bonus, float powerMultiplier)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		powerMultiplier = Mathf.Max(0f, powerMultiplier);
		if (bonus.MaxHp > 0)
		{
			list.Add($"+{Mathf.RoundToInt((float)bonus.MaxHp * powerMultiplier)} " + BattleLobbyText("здоровья", "health", "sağlık", "Gesundheit"));
		}
		if (bonus.Attack > 0)
		{
			list.Add($"+{Mathf.RoundToInt((float)bonus.Attack * powerMultiplier)} " + BattleLobbyText("атаки", "attack", "saldırı", "Angriff"));
		}
		if (bonus.Armor > 0f)
		{
			list.Add($"+{Mathf.RoundToInt(bonus.Armor * powerMultiplier * 100f)}% " + BattleLobbyText("брони", "armor", "zırh", "Rüstung"));
		}
		if (bonus.CritChance > 0f)
		{
			list.Add($"+{Mathf.RoundToInt(bonus.CritChance * powerMultiplier * 100f)}% " + BattleLobbyText("шанса крита", "critical chance", "kritik şansı", "Krit-Chance"));
		}
		if (bonus.CritDamageMultiplier > 1f)
		{
			list.Add($"+{Mathf.RoundToInt((bonus.CritDamageMultiplier - 1f) * powerMultiplier * 100f)}% " + BattleLobbyText("силы крита", "critical power", "kritik gücü", "Krit-Kraft"));
		}
		return string.Join("  •  ", list);
	}

	private string ResolveBattleTileSymbiosisBreedsText(BattleTileData data)
	{
		if (data?.SymbiosisAnimalTypes == null || data.SymbiosisAnimalTypes.Count == 0)
		{
			return BattleLobbyText("нет породы", "no breed", "irk yok", "keine Art");
		}
		List<string> list = new List<string>();
		for (int i = 0; i < data.SymbiosisAnimalTypes.Count; i++)
		{
			list.Add(ResolveBattleTileAnimalTypeName(data.SymbiosisAnimalTypes[i]));
		}
		return string.Join(", ", list);
	}

	private string ResolveBattleTileAnimalTypeName(BattleCharacterDatabase.CharacterAnimalType animalType)
	{
		return animalType switch
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger => BattleLobbyText("Тигр", "Tiger", "Kaplan", "Tiger"),
			BattleCharacterDatabase.CharacterAnimalType.Fox => BattleLobbyText("Лиса", "Fox", "Tilki", "Fuchs"),
			BattleCharacterDatabase.CharacterAnimalType.Wolf => BattleLobbyText("Волк", "Wolf", "Kurt", "Wolf"),
			BattleCharacterDatabase.CharacterAnimalType.Bear => BattleLobbyText("Медведь", "Bear", "Ayı", "Bär"),
			BattleCharacterDatabase.CharacterAnimalType.Dragon => BattleLobbyText("Дракон", "Dragon", "Ejderha", "Drache"),
			BattleCharacterDatabase.CharacterAnimalType.Dog => BattleLobbyText("Собака", "Dog", "Köpek", "Hund"),
			_ => animalType.ToString()
		};
	}

	private string ResolveBattleTileActiveBonusText(BattleTileActiveBonusData bonus, float powerMultiplier)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		powerMultiplier = Mathf.Max(0f, powerMultiplier);
		if (bonus.Attack > 0)
		{
			list.Add($"+{Mathf.RoundToInt((float)bonus.Attack * powerMultiplier)} " + BattleLobbyText("атаки", "attack", "saldırı", "Angriff"));
		}
		if (bonus.CritChance > 0f)
		{
			list.Add($"+{Mathf.RoundToInt(bonus.CritChance * powerMultiplier * 100f)}% " + BattleLobbyText("шанса крита", "critical chance", "kritik şansı", "Krit-Chance"));
		}
		if (bonus.CritDamageMultiplier > 1f)
		{
			list.Add($"+{Mathf.RoundToInt((bonus.CritDamageMultiplier - 1f) * powerMultiplier * 100f)}% " + BattleLobbyText("силы крита", "critical power", "kritik gücü", "Krit-Kraft"));
		}
		if (bonus.HealSelf > 0)
		{
			list.Add($"+{Mathf.RoundToInt((float)bonus.HealSelf * powerMultiplier)} " + BattleLobbyText("восстановления", "healing", "iyileştirme", "Heilung"));
		}
		return string.Join("  •  ", list);
	}

	private void CloseBattleTileProfile()
	{
		CancelBattleTileProfileClick();
		if (!(battleTileProfileRoot == null))
		{
			UnityEngine.Object.Destroy(battleTileProfileRoot);
			battleTileProfileRoot = null;
		}
	}

	private void OnClickMoveBattleTile(string tileId, bool fromActivePocket)
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile == null)
		{
			return;
		}
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(playerProfile);
		string reason;
		if (orCreateInventory != null && string.Equals(orCreateInventory.TotemTileId, tileId, StringComparison.Ordinal))
		{
			bool flag = BattleTileInventoryService.TryClearTotemTile(playerProfile, store, out reason);
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.text = (flag ? BattleLobbyText("Роль тотема снята. Камень остался в активном наборе.", "Totem role removed. The stone remains in the active loadout.", "Totem rolü kaldırıldı. Taş aktif destede kaldı.", "Die Totemrolle wurde entfernt. Der Stein bleibt im aktiven Set.") : ResolveBattleTileInventoryReason(reason));
			}
			if (flag && ProfileService.I != null)
			{
				ProfileService.I.Save();
				ProfileService.I.NotifyProfileChanged();
				RefreshBattleTileInventoryUi();
			}
		}
		else
		{
			bool flag2 = (fromActivePocket ? BattleTileInventoryService.TryReserveTile(playerProfile, store, tileId, out reason) : BattleTileInventoryService.TryActivateTile(playerProfile, store, tileId, out reason));
			if (battleTileInventoryStatusText != null)
			{
				battleTileInventoryStatusText.text = (flag2 ? BattleLobbyText("Колода обновлена.", "Deck updated.", "Deste güncellendi.", "Deck aktualisiert.") : ResolveBattleTileInventoryReason(reason));
			}
			if (flag2)
			{
				ProfileService.I.Save();
				ProfileService.I.NotifyProfileChanged();
				RefreshBattleTileInventoryUi();
			}
		}
	}

	private string ResolveBattleTileInventoryReason(string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return BattleLobbyText("Нельзя переложить этот камень.", "This tile cannot be moved.", "Bu taş taşınamaz.", "Dieser Stein kann nicht bewegt werden.");
		}
		if (reason.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("Активная колода заполнена.", "Active deck is full.", "Aktif deste dolu.", "Aktives Deck ist voll.");
		}
		if (reason.IndexOf("least", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("В активной колоде должно остаться минимум 2 камня.", "Keep at least 2 active tiles.", "Aktif destede en az 2 taş kalmali.", "Mindestens 2 aktive Steine behalten.");
		}
		if (reason.IndexOf("already active", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("Такой тип камня уже взят в бой.", "This tile type is already active.", "Bu taş türü zaten aktif.", "Dieser Steintyp ist bereits aktiv.");
		}
		if (reason.IndexOf("Totem must be selected from the active deck", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("Тотем можно выбрать только среди 18 активных камней.", "The totem must be one of the 18 active stones.", "Totem, 18 aktif taştan biri olmalıdır.", "Der Totemstein muss einer der 18 aktiven Steine sein.");
		}
		if (reason.IndexOf("Select another active totem", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("Сначала назначьте тотемом другой активный камень.", "Select another active totem first.", "Önce başka bir aktif totem seçin.", "Wähle zuerst einen anderen aktiven Totemstein.");
		}
		if (reason.IndexOf("totem", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("Этот тип камня уже установлен как тотем.", "This tile type is already assigned as the totem.", "Bu taş türü zaten totem olarak takılı.", "Dieser Steintyp ist bereits als Totem eingesetzt.");
		}
		if (reason.IndexOf("reserve copies", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return BattleLobbyText("В запасе нет свободной копии этого камня.", "No spare copy of this tile is available.", "Bu taşın yedek kopyası yok.", "Keine Reservekopie dieses Steins verfügbar.");
		}
		return reason;
	}

	private void CreateEnergyShopSectionHeader(Transform parent)
	{
		Image headerPlate = CreateShopImage(parent, "EnergySectionHeaderPlate", new Vector2(0f, 300f), new Vector2(1050f, 92f), raycastTarget: false);
		headerPlate.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWideAlt");
		headerPlate.enabled = headerPlate.sprite != null;
		headerPlate.type = Image.Type.Simple;
		headerPlate.preserveAspect = false;
		headerPlate.color = new Color(0.92f, 0.86f, 0.7f, 0.94f);

		TMP_Text sectionTitle = CreateShopText(parent, "EnergySectionTitle", BattleLobbyText("Пополнить энергию", "Recharge Energy", "Enerjiyi Yenile", "Energie aufladen"), new Vector2(0f, 300f), new Vector2(850f, 46f), 35f, TextAlignmentOptions.Center, new Color(1f, 0.91f, 0.58f, 1f));
		ConfigureEnergyShopText(sectionTitle, 26f, 35f);
	}

	private Button CreateEnergyShopButton(Transform parent, string objectName, Vector2 position, string title, string leftValue, string rightValue, Sprite leftIcon, Sprite rightIcon)
	{
		bool isRewardedAd = leftIcon == null;
		Image cardFrame = CreateShopImage(parent, objectName, position, new Vector2(700f, 570f), raycastTarget: false);
		if (!BattlePopupStyle.ApplyWindow(cardFrame, false))
		{
			cardFrame.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWide");
			cardFrame.color = Color.white;
			cardFrame.preserveAspect = false;
		}
		Transform card = cardFrame.transform;

		Image offerPlate = CreateShopImage(card, "OfferPlate", new Vector2(0f, 40f), new Vector2(630f, 180f), raycastTarget: false);
		offerPlate.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWideAlt");
		offerPlate.enabled = offerPlate.sprite != null;
		offerPlate.type = Image.Type.Simple;
		offerPlate.preserveAspect = false;
		offerPlate.color = isRewardedAd
			? new Color(0.82f, 0.9f, 0.72f, 0.86f)
			: new Color(0.78f, 0.72f, 0.94f, 0.86f);

		TMP_Text cardTitle = CreateShopText(card, "Title", title, new Vector2(0f, 215f), new Vector2(610f, 58f), 42f, TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.76f, 1f));
		ConfigureEnergyShopText(cardTitle, 28f, 42f);
		TMP_Text leftCaption = CreateShopText(card, "LeftCaption", isRewardedAd
			? BattleLobbyText("УСЛОВИЕ", "REQUIREMENT", "KOŞUL", "BEDINGUNG")
			: BattleLobbyText("ЦЕНА", "PRICE", "FİYAT", "PREIS"), new Vector2(-165f, 155f), new Vector2(250f, 32f), 23f, TextAlignmentOptions.Center, new Color(0.82f, 0.8f, 0.74f, 1f));
		ConfigureEnergyShopText(leftCaption, 16f, 23f);
		TMP_Text rightCaption = CreateShopText(card, "RightCaption", BattleLobbyText("НАГРАДА", "REWARD", "ÖDÜL", "BELOHNUNG"), new Vector2(165f, 155f), new Vector2(250f, 32f), 23f, TextAlignmentOptions.Center, new Color(0.64f, 0.9f, 1f, 1f));
		ConfigureEnergyShopText(rightCaption, 16f, 23f);

		Image contentArea = CreateShopImage(card, "OfferContent", new Vector2(0f, 40f), new Vector2(560f, 112f), raycastTarget: false);
		contentArea.sprite = null;
		contentArea.color = Color.clear;
		contentArea.gameObject.AddComponent<RectMask2D>();

		Image leftGroup = CreateShopImage(contentArea.transform, "LeftOfferGroup", new Vector2(-165f, 0f), new Vector2(270f, 112f), raycastTarget: false);
		leftGroup.sprite = null;
		leftGroup.color = Color.clear;
		Image rightGroup = CreateShopImage(contentArea.transform, "RightOfferGroup", new Vector2(165f, 0f), new Vector2(270f, 112f), raycastTarget: false);
		rightGroup.sprite = null;
		rightGroup.color = Color.clear;

		Image leftImage = CreateShopImage(leftGroup.transform, "LeftIcon", new Vector2(-50f, 0f), new Vector2(100f, 100f), raycastTarget: false);
		leftImage.sprite = leftIcon;
		leftImage.enabled = leftIcon != null;
		leftImage.preserveAspect = true;
		if (isRewardedAd)
		{
			TMP_Text adMark = CreateShopText(leftGroup.transform, "AdMark", leftValue, Vector2.zero, new Vector2(220f, 64f), 36f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.34f, 1f));
			ConfigureEnergyShopText(adMark, 24f, 36f);
		}
		else
		{
			TMP_Text priceText = CreateShopText(leftGroup.transform, "LeftValue", leftValue, new Vector2(50f, 0f), new Vector2(80f, 64f), 44f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.44f, 1f));
			ConfigureEnergyShopText(priceText, 32f, 44f);
		}

		TMP_Text equalsText = CreateShopText(contentArea.transform, "Equals", "=", Vector2.zero, new Vector2(64f, 64f), 44f, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.76f, 1f));
		ConfigureEnergyShopText(equalsText, 32f, 44f);
		Image rightImage = CreateShopImage(rightGroup.transform, "RightIcon", new Vector2(-50f, 0f), new Vector2(108f, 108f), raycastTarget: false);
		rightImage.sprite = rightIcon;
		rightImage.enabled = rightIcon != null;
		rightImage.preserveAspect = true;
		string formattedReward = !string.IsNullOrEmpty(rightValue) && rightValue[0] == '+' ? rightValue : "+" + rightValue;
		TMP_Text rewardText = CreateShopText(rightGroup.transform, "RightValue", formattedReward, new Vector2(55f, 0f), new Vector2(110f, 66f), 46f, TextAlignmentOptions.Center, new Color(0.5f, 0.9f, 1f, 1f));
		ConfigureEnergyShopText(rewardText, 33f, 46f);

		TMP_Text benefitText = CreateShopText(card, "Benefit", isRewardedAd
			? BattleLobbyText("Смотри — и снова в бой", "Watch and return to battle", "İzle, savaşa geri dön", "Ansehen und zurück in den Kampf")
			: BattleLobbyText("В бой без ожидания", "Back to battle instantly", "Beklemeden savaşa dön", "Sofort zurück in den Kampf"), new Vector2(0f, -92f), new Vector2(600f, 40f), 27f, TextAlignmentOptions.Center, isRewardedAd ? new Color(0.68f, 0.92f, 0.58f, 1f) : new Color(0.74f, 0.72f, 1f, 1f));
		ConfigureEnergyShopText(benefitText, 19f, 27f);

		string actionText = isRewardedAd
			? BattleLobbyText("СМОТРЕТЬ", "WATCH", "İZLE", "ANSEHEN")
			: BattleLobbyText("КУПИТЬ", "BUY", "SATIN AL", "KAUFEN");
		Button actionButton = CreateShopButton(card, "Action" + objectName, actionText, new Vector2(0f, -207f), new Vector2(540f, 98f), Color.white, 34f);
		TMP_Text actionLabel = actionButton.transform.Find("Label")?.GetComponent<TMP_Text>();
		ConfigureEnergyShopText(actionLabel, 23f, 34f);
		return actionButton;
	}

	private static void ConfigureEnergyShopText(TMP_Text text, float minFontSize, float maxFontSize)
	{
		if (text == null)
		{
			return;
		}
		text.enableAutoSizing = true;
		text.fontSizeMin = minFontSize;
		text.fontSizeMax = maxFontSize;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Ellipsis;
		text.margin = new Vector4(8f, 0f, 8f, 0f);
	}

	private Button CreateBattleTilePackCard(Transform parent, BattleTilePackId packId, Vector2 position)
	{
		BattleTilePackDefinition definition = BattleTilePackShopService.GetDefinition(packId);
		Image block = CreateShopImage(parent, "BattleTilePackBlock" + packId, position, new Vector2(380f, 760f), raycastTarget: false);
		block.sprite = null;
		block.color = Color.clear;

		Image thinFrame = CreateShopImage(block.transform, "ThinFrame", Vector2.zero, new Vector2(760f, 380f), raycastTarget: false);
		thinFrame.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWide");
		if (thinFrame.sprite != null)
		{
			thinFrame.type = Image.Type.Simple;
			thinFrame.preserveAspect = false;
			thinFrame.color = Color.white;
			thinFrame.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
		}
		else
		{
			thinFrame.color = new Color(0.025f, 0.018f, 0.014f, 0.72f);
		}

		Image preview = CreateShopImage(block.transform, "Preview", new Vector2(0f, 195f), new Vector2(340f, 340f), raycastTarget: false);
		preview.sprite = ResolveBattleTilePackPreviewSprite(packId);
		preview.enabled = preview.sprite != null;
		preview.preserveAspect = true;

		Image packFrame = CreateShopImage(block.transform, "PackFrame", new Vector2(0f, 195f), new Vector2(380f, 380f), raycastTarget: false);
		packFrame.sprite = ResolveBattleTilePackFrameSprite(packId);
		packFrame.enabled = packFrame.sprite != null;
		packFrame.preserveAspect = false;

		CreateShopText(block.transform, "Title", GetBattleTilePackTitle(packId), new Vector2(0f, -40f), new Vector2(280f, 52f), 40f, TextAlignmentOptions.Center, Color.white);
		CreateBattleTilePackCost(block.transform, definition);
		CreateShopText(block.transform, "Rolls", GetBattleTilePackRollText(definition), new Vector2(0f, -143f), new Vector2(310f, 44f), 30f, TextAlignmentOptions.Center, new Color(0.86f, 0.94f, 1f, 1f));
		CreateShopText(block.transform, "Rewards", GetBattleTilePackRewardText(packId, definition), new Vector2(0f, -218f), new Vector2(290f, 126f), 24f, TextAlignmentOptions.Center, new Color(0.92f, 0.88f, 0.78f, 1f));

		Button actionButton = CreateShopButton(block.transform, "ButtonBattleTilePack" + packId, GetBattleTilePackActionText(packId), new Vector2(0f, -340f), new Vector2(360f, 90f), Color.white, 36f);
		SetBattleTilePackButtonLabel(actionButton, GetBattleTilePackActionText(packId));
		return actionButton;
	}

	private string GetBattleTilePackActionText(BattleTilePackId packId)
	{
		return packId == BattleTilePackId.DailyAd
			? BattleLobbyText("Смотреть рекламу", "Watch Ad", "Reklam İzle", "Werbung ansehen")
			: BattleLobbyText("Купить", "Buy", "Satın Al", "Kaufen");
	}

	private string GetBattleTilePackTitle(BattleTilePackId packId)
	{
		return packId switch
		{
			BattleTilePackId.DailyAd => BattleLobbyText("Дневной пак", "Daily Pack", "Günlük Paket", "Tagespaket"), 
			BattleTilePackId.OzTileMedium => BattleLobbyText("Боевой пак", "Battle Pack", "Savaş Paketi", "Kampf-Paket"), 
			BattleTilePackId.OzTileHigh => BattleLobbyText("Элитный пак", "Elite Pack", "Elit Paket", "Elite-Paket"), 
			BattleTilePackId.AmetistPremium => BattleLobbyText("Аметистовый пак", "Ametist Pack", "Ametist Paket", "Ametist-Paket"), 
			_ => BattleLobbyText("Пак", "Pack", "Paket", "Paket"), 
		};
	}

	private string GetBattleTilePackCostText(BattleTilePackDefinition definition)
	{
		if (definition == null)
		{
			return string.Empty;
		}
		if (definition.RequiresRewardedAd)
		{
			return BattleLobbyText("1 раз в день / реклама", "Daily / Ad", "Günlük / Reklam", "Taeglich / Werbung");
		}
		if (definition.OzTileCost > 0)
		{
			return definition.OzTileCost + " OzTile";
		}
		if (definition.AmetistCost > 0)
		{
			return definition.AmetistCost + " " + BattleLobbyText("Аметист", "Ametist", "Ametist", "Ametist");
		}
		return BattleLobbyText("Бесплатно", "Free", "Ücretsiz", "Kostenlos");
	}

	private void CreateBattleTilePackCost(Transform parent, BattleTilePackDefinition definition)
	{
		if (definition == null)
		{
			return;
		}
		Sprite currencyIcon = null;
		int amount = 0;
		if (definition.OzTileCost > 0)
		{
			currencyIcon = LoadBattleLobbyOzTileIcon();
			amount = definition.OzTileCost;
		}
		else if (definition.AmetistCost > 0)
		{
			currencyIcon = LoadBattleLobbyAmetistIcon();
			amount = definition.AmetistCost;
		}
		if (currencyIcon == null || amount <= 0)
		{
			CreateShopText(parent, "Cost", GetBattleTilePackCostText(definition), new Vector2(0f, -92f), new Vector2(310f, 44f), 34f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.42f, 1f));
			return;
		}
		Image icon = CreateShopImage(parent, "CostIcon", new Vector2(-54f, -92f), new Vector2(44f, 44f), raycastTarget: false);
		icon.sprite = currencyIcon;
		icon.preserveAspect = true;
		CreateShopText(parent, "Cost", amount.ToString(), new Vector2(24f, -92f), new Vector2(108f, 44f), 36f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.42f, 1f));
	}

	private string GetBattleTilePackRollText(BattleTilePackDefinition definition)
	{
		if (definition == null)
		{
			return string.Empty;
		}
		if (!definition.IsRandomized)
		{
			return BattleLobbyText("Особый набор", "Special Bundle", "Özel Paket", "Spezialpaket");
		}
		return definition.RollCount <= 1
			? BattleLobbyText("1 камень", "1 tile", "1 taş", "1 Stein")
			: BattleLobbyText($"{definition.RollCount} камней", $"{definition.RollCount} tiles", $"{definition.RollCount} taş", $"{definition.RollCount} Steine");
	}

	private string GetBattleTilePackRewardText(BattleTilePackId packId, BattleTilePackDefinition definition)
	{
		if (definition == null)
		{
			return string.Empty;
		}
		if (!definition.IsRandomized)
		{
			List<string> fixedRewards = new List<string>();
			if (definition.FixedRewards != null)
			{
				for (int i = 0; i < definition.FixedRewards.Length; i++)
				{
					BattleTileFixedReward reward = definition.FixedRewards[i];
					if (reward != null && reward.Count > 0)
					{
						fixedRewards.Add($"{reward.Count}x {GetLocalizedBattleTileRarity(reward.Rarity)}");
					}
				}
			}
			return string.Join("\n", fixedRewards);
		}
		if (definition.Weights == null || definition.Weights.Length == 0)
		{
			return string.Empty;
		}
		int totalWeight = 0;
		for (int i = 0; i < definition.Weights.Length; i++)
		{
			totalWeight += Mathf.Max(0, definition.Weights[i].weight);
		}
		if (totalWeight <= 0)
		{
			return string.Empty;
		}
		List<string> odds = new List<string>();
		for (int i = 0; i < definition.Weights.Length; i++)
		{
			int weight = Mathf.Max(0, definition.Weights[i].weight);
			if (weight <= 0)
			{
				continue;
			}
			float percent = weight * 100f / totalWeight;
			float roundedPercent = Mathf.Round(percent);
			string percentText = Mathf.Approximately(percent, roundedPercent)
				? Mathf.RoundToInt(percent) + "%"
				: percent.ToString("0.#") + "%";
			odds.Add(GetLocalizedBattleTileRarity(definition.Weights[i].rarity) + " " + percentText);
		}
		if (definition.GuaranteedMinimumCount > 0)
		{
			odds.Add(BattleLobbyText(
				$"Гарантия: {definition.GuaranteedMinimumCount}+ {GetLocalizedBattleTileRarity(definition.GuaranteedMinimum).ToLowerInvariant()}",
				$"Guaranteed: {definition.GuaranteedMinimumCount}+ {GetLocalizedBattleTileRarity(definition.GuaranteedMinimum)}",
				$"Garanti: {definition.GuaranteedMinimumCount}+ {GetLocalizedBattleTileRarity(definition.GuaranteedMinimum)}",
				$"Garantiert: {definition.GuaranteedMinimumCount}+ {GetLocalizedBattleTileRarity(definition.GuaranteedMinimum)}"));
		}
		else if (definition.PityPackLimit > 0)
		{
			odds.Add(BattleLobbyText(
				$"Гарантия {GetLocalizedBattleTileRarity(definition.PityRarity).ToLowerInvariant()}: {definition.PityPackLimit}-й пак",
				$"{GetLocalizedBattleTileRarity(definition.PityRarity)} guarantee: pack {definition.PityPackLimit}",
				$"{GetLocalizedBattleTileRarity(definition.PityRarity)} garanti: {definition.PityPackLimit}. paket",
				$"{GetLocalizedBattleTileRarity(definition.PityRarity)} garantiert: Paket {definition.PityPackLimit}"));
		}
		return string.Join("\n", odds);
	}

	private string GetLocalizedBattleTileRarity(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Standard => BattleLobbyText("Стандартный", "Standard", "Standart", "Standard"),
			BattleTileRarity.Common => BattleLobbyText("Обычный", "Common", "Yaygın", "Gewöhnlich"),
			BattleTileRarity.Rare => BattleLobbyText("Редкий", "Rare", "Nadir", "Selten"),
			BattleTileRarity.Epic => BattleLobbyText("Эпический", "Epic", "Destansı", "Episch"),
			BattleTileRarity.Legendary => BattleLobbyText("Легендарный", "Legendary", "Efsanevi", "Legendär"),
			BattleTileRarity.Mythic => BattleLobbyText("Мифический", "Mythic", "Mitik", "Mythisch"),
			_ => rarity.ToString(),
		};
	}

	private Sprite ResolveBattleTilePackPreviewSprite(BattleTilePackId packId)
	{
		string resourcePath = packId switch
		{
			BattleTilePackId.DailyAd => "Mahjong/Sprites/BattleShopPacks/BattleTilePackDailyAd",
			BattleTilePackId.OzTileMedium => "Mahjong/Sprites/BattleShopPacks/BattleTilePackMedium",
			BattleTilePackId.OzTileHigh => "Mahjong/Sprites/BattleShopPacks/BattleTilePackHigh",
			BattleTilePackId.AmetistPremium => "Mahjong/Sprites/BattleShopPacks/BattleTilePackAmetist",
			_ => string.Empty,
		};
		Sprite packSprite = LoadResourceSprite(resourcePath);
		if (packSprite != null)
		{
			return packSprite;
		}

		BattleTileRarity guaranteedMinimum = BattleTilePackShopService.GetDefinition(packId).GuaranteedMinimum;
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		IReadOnlyList<BattleTileData> readOnlyList = ((battleTileStore != null) ? battleTileStore.BattleTiles : null);
		if (readOnlyList == null)
		{
			return null;
		}
		BattleTileData battleTileData = null;
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			BattleTileData battleTileData2 = readOnlyList[i];
			if (battleTileData2 != null && !(battleTileData2.Prefab == null) && battleTileData2.IsDonate)
			{
				if (battleTileData == null)
				{
					battleTileData = battleTileData2;
				}
				if (battleTileData2.Rarity >= guaranteedMinimum)
				{
					return battleTileData2.Prefab.FaceSprite;
				}
			}
		}
		if (!(battleTileData?.Prefab != null))
		{
			return null;
		}
		return battleTileData.Prefab.FaceSprite;
	}

	private static Sprite ResolveBattleTilePackFrameSprite(BattleTilePackId packId)
	{
		string resourcePath = packId switch
		{
			BattleTilePackId.DailyAd => "Mahjong/Sprites/BattleShopPacks/BattleTilePackFrameDailyAd",
			BattleTilePackId.OzTileMedium => "Mahjong/Sprites/BattleShopPacks/BattleTilePackFrameMedium",
			BattleTilePackId.OzTileHigh => "Mahjong/Sprites/BattleShopPacks/BattleTilePackFrameHigh",
			BattleTilePackId.AmetistPremium => "Mahjong/Sprites/BattleShopPacks/BattleTilePackFrameAmetist",
			_ => string.Empty,
		};
		return LoadResourceSprite(resourcePath);
	}

	private void CreateDragonShopCard(Transform parent, string objectName, string title, string characterId, Vector2 position)
	{
		GameObject obj = CreateShopPanel(parent, objectName, new Vector2(680f, 600f), position, Color.white);
		Image image = CreateShopImage(obj.transform, "Portrait", new Vector2(-175f, 30f), new Vector2(245f, 360f), raycastTarget: false);
		image.sprite = ResolveBattleCharacterShopSprite(characterId);
		image.enabled = image.sprite != null;
		CreateShopText(obj.transform, "Title", title, new Vector2(0f, 225f), new Vector2(560f, 58f), 45f, TextAlignmentOptions.Center, Color.white);
		CreateShopText(obj.transform, "Price", shopDragonAmetistPrice + " " + BattleLobbyText("Аметист", "Ametist", "Ametist", "Ametist"), new Vector2(145f, -150f), new Vector2(320f, 44f), 30f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.42f, 1f));
		Button button = CreateShopButton(obj.transform, "ButtonBuy" + characterId, BattleLobbyText("Купить", "Buy", "Satın Al", "Kaufen"), new Vector2(0f, -235f), new Vector2(590f, 88f), Color.white, 34f);
		if (string.Equals(characterId, "Dragon_Male", StringComparison.OrdinalIgnoreCase))
		{
			shopBuyDragonMaleButton = button;
			shopBuyDragonMaleButton.onClick.AddListener(OnClickBuyDragonMale);
		}
		else if (string.Equals(characterId, "Dragon_Female", StringComparison.OrdinalIgnoreCase))
		{
			shopBuyDragonFemaleButton = button;
			shopBuyDragonFemaleButton.onClick.AddListener(OnClickBuyDragonFemale);
		}
		ApplyDragonShopCardPresentation(obj, characterId);
	}

	private void ApplyDragonShopCardPresentation(GameObject card, string characterId)
	{
		if (card == null)
		{
			return;
		}
		RectTransform cardRect = card.transform as RectTransform;
		if (cardRect != null)
		{
			cardRect.sizeDelta = new Vector2(680f, 600f);
		}
		Transform portraitTransform = card.transform.Find("Portrait");
		Image portrait = portraitTransform != null ? portraitTransform.GetComponent<Image>() : null;
		if (portrait != null)
		{
			portrait.sprite = ResolveBattleCharacterShopSprite(characterId);
			portrait.enabled = portrait.sprite != null;
			portrait.preserveAspect = true;
			RectTransform portraitRect = portrait.rectTransform;
			portraitRect.anchoredPosition = new Vector2(-175f, 30f);
			portraitRect.sizeDelta = new Vector2(245f, 360f);
		}
		Transform frameTransform = card.transform.Find("PortraitFrame");
		Image frame = frameTransform != null ? frameTransform.GetComponent<Image>() : null;
		if (frame == null)
		{
			frame = CreateShopImage(card.transform, "PortraitFrame", new Vector2(-175f, 30f), new Vector2(260f, 380f), raycastTarget: false);
		}
		frame.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated");
		frame.enabled = frame.sprite != null;
		frame.preserveAspect = true;
		frame.raycastTarget = false;
		frame.rectTransform.anchoredPosition = new Vector2(-175f, 30f);
		frame.rectTransform.sizeDelta = new Vector2(260f, 380f);
		if (portraitTransform != null)
		{
			frame.transform.SetSiblingIndex(portraitTransform.GetSiblingIndex() + 1);
		}

		Transform kickerTransform = card.transform.Find("Kicker");
		if (kickerTransform != null)
		{
			kickerTransform.gameObject.SetActive(false);
		}
		ConfigureDragonShopText(card.transform.Find("Title")?.GetComponent<TMP_Text>(), new Vector2(0f, 225f), new Vector2(560f, 58f), 45f, TextAlignmentOptions.Center);
		BattleCharacterDatabase.BattleCharacterData character = BattleCharacterDatabase.HasInstance ? BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId) : null;
		Transform statsPlateTransform = card.transform.Find("StatsPlate");
		if (statsPlateTransform != null)
		{
			statsPlateTransform.gameObject.SetActive(false);
		}
		Transform detailsTransform = card.transform.Find("Details");
		TMP_Text details = detailsTransform != null ? detailsTransform.GetComponent<TMP_Text>() : null;
		if (details == null)
		{
			details = CreateShopText(card.transform, "Details", string.Empty, new Vector2(145f, 25f), new Vector2(320f, 290f), 30f, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.91f, 0.78f, 1f));
		}
		details.text = BuildDragonShopDetails(character);
		ConfigureDragonShopText(details, new Vector2(145f, 25f), new Vector2(320f, 290f), 30f, TextAlignmentOptions.TopLeft);
		details.color = new Color(0.96f, 0.91f, 0.78f, 1f);
		details.textWrappingMode = TextWrappingModes.NoWrap;
		details.overflowMode = TextOverflowModes.Ellipsis;
		details.lineSpacing = 10f;
		ConfigureDragonShopText(card.transform.Find("Price")?.GetComponent<TMP_Text>(), new Vector2(145f, -150f), new Vector2(320f, 44f), 30f, TextAlignmentOptions.Center);

		Transform buttonTransform = card.transform.Find("ButtonBuy" + characterId);
		RectTransform buttonRect = buttonTransform as RectTransform;
		if (buttonRect != null)
		{
			buttonRect.anchoredPosition = new Vector2(0f, -235f);
			buttonRect.sizeDelta = new Vector2(590f, 88f);
		}
		ConfigureDragonShopText(buttonTransform?.Find("Label")?.GetComponent<TMP_Text>(), Vector2.zero, new Vector2(540f, 62f), 34f, TextAlignmentOptions.Center);
	}

	private static void ConfigureDragonShopText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
	{
		if (text == null)
		{
			return;
		}
		RectTransform rect = text.rectTransform;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		text.fontSize = fontSize;
		text.enableAutoSizing = true;
		text.fontSizeMin = Mathf.Max(16f, fontSize - 8f);
		text.fontSizeMax = fontSize;
		text.alignment = alignment;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Ellipsis;
		text.margin = new Vector4(6f, 2f, 6f, 2f);
	}

	private string BuildDragonShopDetails(BattleCharacterDatabase.BattleCharacterData character)
	{
		string className = ResolveBattleLobbyCharacterClassName(character);
		if (character == null)
		{
			return BattleLobbyText("Класс", "Class", "Sınıf", "Klasse") + ": " + className;
		}
		BattleCharacterDatabase.BattleCharacterStats stats = character.Stats;
		return BattleLobbyText("Класс", "Class", "Sınıf", "Klasse") + ": " + className + "\n"
			+ BattleLobbyText("Здоровье", "Health", "Sağlık", "Gesundheit") + ": " + stats.MaxHp + "\n"
			+ BattleLobbyText("Атака", "Attack", "Saldırı", "Angriff") + ": " + stats.Attack + "\n"
			+ BattleLobbyText("Броня", "Armor", "Zırh", "Rüstung") + ": " + Mathf.RoundToInt(stats.Armor * 100f) + "%\n"
			+ BattleLobbyText("Крит", "Crit", "Kritik", "Krit") + ": " + Mathf.RoundToInt(stats.CritChance * 100f) + "%\n"
			+ BattleLobbyText("Крит. урон", "Crit damage", "Kritik hasar", "Krit-Schaden") + ": x" + stats.CritDamageMultiplier.ToString("0.##");
	}

	private static Sprite ResolveBattleCharacterShopSprite(string characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId) || !BattleCharacterDatabase.HasInstance)
		{
			return null;
		}
		BattleCharacterDatabase.BattleCharacterData characterOrNull = BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId);
		if (characterOrNull == null)
		{
			return null;
		}
		if (!(characterOrNull.ProfileSprite != null))
		{
			if (!(characterOrNull.LobbySprite != null))
			{
				return characterOrNull.BattleSprite;
			}
			return characterOrNull.LobbySprite;
		}
		return characterOrNull.ProfileSprite;
	}

	private Button CreateAmetistPackageButton(Transform parent, string objectName, string productId, Vector2 position)
	{
		MonetizationProduct product = OzAmetistShopService.GetProduct(productId);
		string value = ((product != null) ? product.OzAmetistAmount.ToString() : "?");
		string value2 = ((product != null) ? product.LocalPrice : string.Empty);
		GameObject obj = CreateShopPanel(parent, objectName + "Card", new Vector2(320f, 430f), position, Color.white);
		Image packageImage = CreateShopImage(obj.transform, "Package", new Vector2(0f, 68f), new Vector2(230f, 230f), raycastTarget: false);
		packageImage.sprite = ResolveAmetistPackageSprite(productId);
		packageImage.enabled = packageImage.sprite != null;
		packageImage.preserveAspect = true;
		CreateShopText(obj.transform, "Amount", value, new Vector2(0f, -76f), new Vector2(260f, 56f), 46f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.46f, 1f));
		CreateShopText(obj.transform, "Price", value2, new Vector2(0f, -124f), new Vector2(260f, 42f), 31f, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.86f, 1f));
		return CreateShopButton(obj.transform, objectName, BattleLobbyText("Купить", "Buy", "Satın al", "Kaufen"), new Vector2(0f, -182f), new Vector2(260f, 72f), Color.white, 34f);
	}

	private Sprite ResolveAmetistPackageSprite(string productId)
	{
		string resourcePath = productId switch
		{
			"oz_ametist_small" => "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_50",
			"oz_ametist_medium" => "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_120",
			"oz_ametist_big" => "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_300",
			"oz_ametist_legend" => "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_700",
			_ => string.Empty,
		};
		return LoadResourceSprite(resourcePath);
	}

	private void EnsureWeeklyRewardUi()
	{
		if (weeklyRewardRoot != null)
		{
			return;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (!(canvas == null))
		{
			weeklyRewardRoot = new GameObject("WeeklyRewardOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			weeklyRewardRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
			ConfigureOverlayCanvas(weeklyRewardRoot);
			RectTransform component = weeklyRewardRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = weeklyRewardRoot.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.72f);
			component2.raycastTarget = true;
			GameObject gameObject = CreateShopPanel(weeklyRewardRoot.transform, "WeeklyRewardPanel", new Vector2(1780f, 930f), Vector2.zero, Color.white);
			FitPanelInsideCanvas(gameObject.transform as RectTransform, canvas, 32f);
			CreateShopText(gameObject.transform, "Title", BattleLobbyText("Еженедельные награды", "Weekly Rewards", "Haftalik Ödüller", "Woechentliche Belohnungen"), new Vector2(0f, 374f), new Vector2(1280f, 82f), 66f, TextAlignmentOptions.Center, Color.white);
			weeklyRewardTodayText = CreateShopText(gameObject.transform, "TodayReward", string.Empty, new Vector2(0f, 312f), new Vector2(1280f, 52f), 38f, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.86f, 1f));
			weeklyRewardSlotImages = new Image[7];
			weeklyRewardIconImages = new Image[7];
			weeklyRewardSlotDayTexts = new TMP_Text[7];
			weeklyRewardSlotStateTexts = new TMP_Text[7];
			weeklyRewardSlotAmountTexts = new TMP_Text[7];
			for (int i = 0; i < 7; i++)
			{
				Vector2 weeklyRewardSlotPosition = GetWeeklyRewardSlotPosition(i);
				GameObject gameObject2 = CreateShopPanel(gameObject.transform, $"RewardDay{i + 1}", new Vector2(370f, 260f), weeklyRewardSlotPosition, Color.white);
				weeklyRewardSlotImages[i] = gameObject2.GetComponent<Image>();
				weeklyRewardSlotDayTexts[i] = CreateShopText(gameObject2.transform, "Day", string.Format("{0} {1}", BattleLobbyText("День", "Day", "Gun", "Tag"), i + 1), new Vector2(0f, 104f), new Vector2(320f, 42f), 34f, TextAlignmentOptions.Center, Color.white);
				weeklyRewardIconImages[i] = CreateShopImage(gameObject2.transform, "RewardIcon", new Vector2(0f, 12f), new Vector2(326f, 166f), raycastTarget: false);
				weeklyRewardSlotAmountTexts[i] = CreateShopText(gameObject2.transform, "Amount", string.Empty, new Vector2(0f, -86f), new Vector2(320f, 42f), 34f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.52f, 1f));
				weeklyRewardSlotStateTexts[i] = CreateShopText(gameObject2.transform, "State", string.Empty, new Vector2(0f, -118f), new Vector2(320f, 34f), 27f, TextAlignmentOptions.Center, Color.white);
			}
			weeklyRewardStatusText = CreateShopText(gameObject.transform, "Status", string.Empty, new Vector2(0f, -326f), new Vector2(1280f, 54f), 36f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.68f, 1f));
			weeklyRewardFreeButton = CreateShopButton(gameObject.transform, "ButtonWeeklyFree", BattleLobbyText("Забрать", "Claim Free", "Al", "Gratis abholen"), new Vector2(-520f, -410f), new Vector2(560f, 96f), Color.white, 38f);
			weeklyRewardAdButton = CreateShopButton(gameObject.transform, "ButtonWeeklyAd", BattleLobbyText("Реклама", "Watch Ad", "Reklam", "Werbung ansehen"), new Vector2(520f, -410f), new Vector2(660f, 96f), Color.white, 36f);
			weeklyRewardFreeButtonText = weeklyRewardFreeButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			weeklyRewardAdButtonText = weeklyRewardAdButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			weeklyRewardFreeButton.onClick.AddListener(OnClickClaimWeeklyFree);
			weeklyRewardAdButton.onClick.AddListener(OnClickClaimWeeklyAd);
			CreateShopButton(gameObject.transform, "ButtonCloseWeeklyRewards", BattleLobbyText("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(740f, 374f), new Vector2(92f, 92f), Color.white, 28f).onClick.AddListener(CloseWeeklyRewards);
			SetGameObjectActiveSafe(weeklyRewardRoot, active: false);
			ApplyBattleLobbyTypography();
		}
	}

	private void EnsureDailyHeroBonusUi()
	{
		if (dailyHeroBonusRoot != null)
		{
			UnityEngine.Object.Destroy(dailyHeroBonusRoot);
			dailyHeroBonusRoot = null;
		}
		Canvas canvas = FindActiveSceneCanvas();
		if (canvas == null)
		{
			return;
		}
		dailyHeroBonusRoot = new GameObject("DailyHeroBonusOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		dailyHeroBonusRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
		ConfigureOverlayCanvas(dailyHeroBonusRoot);
		RectTransform component = dailyHeroBonusRoot.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = dailyHeroBonusRoot.GetComponent<Image>();
		component2.color = Color.black;
		component2.raycastTarget = true;
		GameObject gameObject = CreateShopPanel(dailyHeroBonusRoot.transform, "DailyHeroBonusPanel", Vector2.zero, Vector2.zero, Color.white);
		RectTransform rectTransform = gameObject.transform as RectTransform;
		StretchToFullscreen(rectTransform);
		Image component3 = gameObject.GetComponent<Image>();
		if (component3 != null && !BattlePopupStyle.ApplyWindow(component3))
		{
			component3.sprite = null;
			component3.type = Image.Type.Simple;
			component3.color = new Color(0.055f, 0.035f, 0.02f, 1f);
			component3.raycastTarget = true;
		}
		GameObject gameObject2 = new GameObject("DailyHeroBonusContent", typeof(RectTransform));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject2.GetComponent<RectTransform>();
		rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform2.pivot = new Vector2(0.5f, 0.5f);
		rectTransform2.anchoredPosition = Vector2.zero;
		rectTransform2.sizeDelta = new Vector2(2140f, 1080f);
		FitPanelInsideCanvas(rectTransform2, canvas, 36f);
		if (!BattleDailyHeroBonusService.TryGetTodayBonus(out var bonus))
		{
			CreateShopText(gameObject2.transform, "Title", GameLocalization.Text("battle.daily.title"), new Vector2(0f, 120f), new Vector2(1560f, 100f), 82f, TextAlignmentOptions.Center, Color.white);
			CreateShopText(gameObject2.transform, "Empty", GameLocalization.Text("battle.daily.empty"), new Vector2(0f, -20f), new Vector2(1500f, 260f), 58f, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.84f, 1f));
		}
		else
		{
			CreateShopText(gameObject2.transform, "Title", bonus.Title, new Vector2(360f, 402f), new Vector2(1200f, 86f), 70f, TextAlignmentOptions.Left, Color.white);
			CreateShopText(gameObject2.transform, "Subtitle", bonus.Subtitle, new Vector2(360f, 326f), new Vector2(1200f, 68f), 50f, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.42f, 1f));
			Image image = CreateShopImage(gameObject2.transform, "DailyHeroDivider", new Vector2(360f, 282f), new Vector2(1160f, 3f), raycastTarget: false);
			image.color = new Color(0.72f, 0.48f, 0.16f, 0.78f);
			image.preserveAspect = false;
			Image image2 = CreateShopImage(gameObject2.transform, "DailyHeroPortrait", new Vector2(-710f, 150f), new Vector2(540f, 620f), raycastTarget: false);
			Sprite sprite = (image2.sprite = ResolveDailyHeroPortrait(bonus.Character));
			image2.enabled = sprite != null;
			Image image3 = CreateShopImage(gameObject2.transform, "DailyHeroPortraitFrame", new Vector2(-710f, 150f), new Vector2(660f, 720f), raycastTarget: false);
			image3.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated");
			image3.enabled = image3.sprite != null;
			image3.preserveAspect = true;
			image3.transform.SetAsLastSibling();
			GameObject obj = CreateShopPanel(gameObject2.transform, "DailyHeroBonusCard", new Vector2(580f, 326f), new Vector2(-710f, -314f), Color.white);
			Image component4 = obj.GetComponent<Image>();
			if (component4 != null)
			{
				BattlePopupStyle.ApplyFront(component4);
			}
			CreateShopText(obj.transform, "BonusText", bonus.BonusText, Vector2.zero, new Vector2(470f, 230f), 42f, TextAlignmentOptions.Center, Color.white);
			TMP_Text tMP_Text = CreateShopText(gameObject2.transform, "Lore", bonus.LoreText, new Vector2(360f, 82f), new Vector2(1200f, 350f), 50f, TextAlignmentOptions.Left, new Color(0.95f, 0.92f, 0.84f, 1f));
			tMP_Text.textWrappingMode = TextWrappingModes.Normal;
			tMP_Text.overflowMode = TextOverflowModes.Ellipsis;
			string text = BattleDailyHeroBonusService.FormatTimeLeft(bonus.TimeLeft);
			CreateShopText(gameObject2.transform, "TimeLeft", GameLocalization.Format("battle.daily.time_left", text), new Vector2(360f, -194f), new Vector2(1200f, 64f), 44f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.42f, 1f));
			dailyHeroBoostStatusText = CreateShopText(gameObject2.transform, "BoostStatus", ResolveDailyHeroBoostStatusText(bonus), new Vector2(360f, -266f), new Vector2(1200f, 60f), 38f, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.84f, 1f));
			dailyHeroBoostButton = CreateShopButton(gameObject2.transform, "ButtonDailyHeroBoost", GameLocalization.Text("battle.daily.boost_button"), new Vector2(360f, -392f), new Vector2(1080f, 146f), Color.white, 58f);
			dailyHeroBoostButton.onClick.AddListener(OnClickDailyHeroBoostAd);
			RefreshDailyHeroBoostButtonState();
		}
		Button button = CreateShopButton(gameObject.transform, "ButtonCloseDailyHeroBonus", GameLocalization.Text("settings.close"), Vector2.zero, new Vector2(112f, 112f), Color.white, 34f);
		RectTransform rectTransform3 = button.transform as RectTransform;
		if (rectTransform3 != null)
		{
			rectTransform3.anchorMin = Vector2.one;
			rectTransform3.anchorMax = Vector2.one;
			rectTransform3.pivot = Vector2.one;
			rectTransform3.anchoredPosition = new Vector2(-58f, -52f);
		}
		button.onClick.AddListener(CloseDailyHeroBonus);
		SetGameObjectActiveSafe(dailyHeroBonusRoot, active: false);
		ApplyBattleLobbyTypography();
	}

	private static Sprite ResolveDailyHeroPortrait(BattleCharacterDatabase.BattleCharacterData character)
	{
		if (character == null)
		{
			return null;
		}
		if (character.ProfileSprite != null)
		{
			return character.ProfileSprite;
		}
		if (character.LobbySprite != null)
		{
			return character.LobbySprite;
		}
		return character.BattleSprite;
	}

	private void EnsureDailyHeroBonusNotificationBadge()
	{
		if (!(dailyHeroBonusButton == null))
		{
			if (dailyHeroBonusNotificationBadge != null)
			{
				ApplyDailyHeroNotificationBadgeLayout();
				return;
			}
			dailyHeroBonusNotificationBadge = new GameObject("DailyHeroBonusNotificationBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			dailyHeroBonusNotificationBadge.transform.SetParent(dailyHeroBonusButton.transform, worldPositionStays: false);
			Image component = dailyHeroBonusNotificationBadge.GetComponent<Image>();
			component.sprite = GetDailyHeroBadgeSprite();
			component.color = new Color(1f, 0.07f, 0.04f, 1f);
			component.raycastTarget = false;
			component.preserveAspect = true;
			ApplyDailyHeroNotificationBadgeLayout();
		}
	}

	private void ApplyDailyHeroNotificationBadgeLayout()
	{
		if (!(dailyHeroBonusNotificationBadge == null))
		{
			RectTransform rectTransform = dailyHeroBonusNotificationBadge.transform as RectTransform;
			if (!(rectTransform == null))
			{
				rectTransform.anchorMin = new Vector2(1f, 1f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.anchoredPosition = new Vector2(-28f, -26f);
				rectTransform.sizeDelta = new Vector2(32f, 32f);
				dailyHeroBonusNotificationBadge.transform.SetAsLastSibling();
			}
		}
	}

	private void UpdateDailyHeroBonusNotification()
	{
		if (dailyHeroBonusButton == null)
		{
			StopDailyHeroAttentionRoutine();
			return;
		}
		EnsureDailyHeroBonusNotificationBadge();
		BattleDailyHeroBonusService.DailyHeroBonus bonus;
		bool flag = ShouldShowLobbyButtons() && IsButtonVisible(dailyHeroBonusButton) && !HasSeenDailyHeroBonusToday() && BattleDailyHeroBonusService.TryGetTodayBonus(out bonus);
		SetGameObjectActiveSafe(dailyHeroBonusNotificationBadge, flag);
		if (flag)
		{
			if (dailyHeroAttentionRoutine == null && base.isActiveAndEnabled)
			{
				dailyHeroAttentionRoutine = StartCoroutine(DailyHeroAttentionRoutine());
			}
		}
		else
		{
			StopDailyHeroAttentionRoutine();
		}
	}

	private IEnumerator DailyHeroAttentionRoutine()
	{
		RectTransform rect = ((dailyHeroBonusButton != null) ? (dailyHeroBonusButton.transform as RectTransform) : null);
		if (rect == null)
		{
			dailyHeroAttentionRoutine = null;
			yield break;
		}
		Quaternion baseRotation = rect.localRotation;
		Vector3 baseScale = rect.localScale;
		WaitForSecondsRealtime pause = new WaitForSecondsRealtime(3.4f);
		while (dailyHeroBonusButton != null && dailyHeroBonusNotificationBadge != null && dailyHeroBonusNotificationBadge.activeSelf && !HasSeenDailyHeroBonusToday())
		{
			float duration = 0.58f;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float num = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * MathF.PI);
				if (dailyHeroBonusNotificationBadge != null)
				{
					dailyHeroBonusNotificationBadge.transform.localScale = Vector3.one * (1f + num * 0.1f);
				}
				yield return null;
			}
			rect.localRotation = baseRotation;
			rect.localScale = baseScale;
			if (dailyHeroBonusNotificationBadge != null)
			{
				dailyHeroBonusNotificationBadge.transform.localScale = Vector3.one;
			}
			yield return pause;
		}
		rect.localRotation = baseRotation;
		rect.localScale = baseScale;
		if (dailyHeroBonusNotificationBadge != null)
		{
			dailyHeroBonusNotificationBadge.transform.localScale = Vector3.one;
		}
		dailyHeroAttentionRoutine = null;
	}

	private void StopDailyHeroAttentionRoutine()
	{
		if (dailyHeroAttentionRoutine != null)
		{
			StopCoroutine(dailyHeroAttentionRoutine);
			dailyHeroAttentionRoutine = null;
		}
		if (dailyHeroBonusButton != null)
		{
			Transform obj = dailyHeroBonusButton.transform;
			obj.localRotation = Quaternion.identity;
			obj.localScale = Vector3.one;
		}
		if (dailyHeroBonusNotificationBadge != null)
		{
			dailyHeroBonusNotificationBadge.transform.localScale = Vector3.one;
		}
	}

	private static bool IsButtonVisible(Button button)
	{
		if (button == null || button.gameObject == null || !button.gameObject.activeInHierarchy)
		{
			return false;
		}
		CanvasGroup component = button.GetComponent<CanvasGroup>();
		if (!(component == null))
		{
			return component.alpha > 0.01f;
		}
		return true;
	}

	private static bool HasSeenDailyHeroBonusToday()
	{
		return string.Equals(PlayerPrefs.GetString("MahjongGame.Battle.DailyHeroBonusSeenDate", string.Empty), DateTime.Now.ToString("yyyyMMdd"), StringComparison.Ordinal);
	}

	private static void MarkDailyHeroBonusSeenToday()
	{
		PlayerPrefs.SetString("MahjongGame.Battle.DailyHeroBonusSeenDate", DateTime.Now.ToString("yyyyMMdd"));
		PlayerPrefs.Save();
	}

	private static Sprite GetDailyHeroBadgeSprite()
	{
		if (cachedDailyHeroBadgeSprite != null)
		{
			return cachedDailyHeroBadgeSprite;
		}
		Texture2D texture2D = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: false)
		{
			name = "DailyHeroBadgeCircle"
		};
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Vector2 b = new Vector2(31.5f, 31.5f);
		float num = 29.44f;
		float num2 = 2.2f;
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				float num3 = Vector2.Distance(new Vector2(j, i), b);
				float a = Mathf.Clamp01((num - num3) / num2);
				Color color = Color.Lerp(new Color(0.72f, 0f, 0f, 1f), new Color(1f, 0.12f, 0.06f, 1f), Mathf.Clamp01(1f - num3 / num));
				color.a = a;
				texture2D.SetPixel(j, i, color);
			}
		}
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		cachedDailyHeroBadgeSprite = Sprite.Create(texture2D, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
		cachedDailyHeroBadgeSprite.name = "DailyHeroBadgeCircle";
		return cachedDailyHeroBadgeSprite;
	}

	private string ResolveDailyHeroBoostStatusText(BattleDailyHeroBonusService.DailyHeroBonus bonus)
	{
		if (bonus.Character == null || !BattleDailyHeroBonusService.CanUseRewardedBoostForToday())
		{
			return GameLocalization.Text("battle.daily.boost_locked_status");
		}
		if (bonus.IsBoostActive)
		{
			return GameLocalization.Text("battle.daily.boost_active") + ": " + BattleDailyHeroBonusService.FormatTimeLeft(bonus.BoostTimeLeft);
		}
		return GameLocalization.Text("battle.daily.boost_hint");
	}

	private void RefreshDailyHeroBoostButtonState()
	{
		if (!(dailyHeroBoostButton == null))
		{
			TimeSpan timeLeft;
			bool flag = BattleDailyHeroBonusService.IsRewardedBoostActive(out timeLeft);
			RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("daily_hero_boost_rewarded");
			bool flag2 = BattleDailyHeroBonusService.CanUseRewardedBoostForToday();
			dailyHeroBoostButton.interactable = flag2 && !flag && rewardedAdAvailability.IsReady && !dailyHeroBoostAdRequestInProgress;
			TMP_Text componentInChildren = dailyHeroBoostButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.text = ((!flag2) ? GameLocalization.Text("battle.daily.boost_locked") : (flag ? (GameLocalization.Text("battle.daily.boost_active") + "\n" + BattleDailyHeroBonusService.FormatTimeLeft(timeLeft)) : GameLocalization.Text("battle.daily.boost_button")));
			}
		}
	}

	private void OnClickDailyHeroBoostAd()
	{
		if (dailyHeroBoostAdRequestInProgress || BattleDailyHeroBonusService.IsRewardedBoostActive())
		{
			RefreshDailyHeroBoostButtonState();
			return;
		}
		if (!BattleDailyHeroBonusService.CanUseRewardedBoostForToday())
		{
			if (dailyHeroBoostStatusText != null)
			{
				dailyHeroBoostStatusText.text = GameLocalization.Text("battle.daily.boost_unlock_first");
			}
			RefreshDailyHeroBoostButtonState();
			return;
		}
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("daily_hero_boost_rewarded");
		if (!rewardedAdAvailability.IsReady)
		{
			if (dailyHeroBoostStatusText != null)
			{
				dailyHeroBoostStatusText.text = ResolveBattleLobbyStatusMessage(rewardedAdAvailability.Message);
			}
			RefreshDailyHeroBoostButtonState();
			return;
		}
		dailyHeroBoostAdRequestInProgress = true;
		if (dailyHeroBoostStatusText != null)
		{
			dailyHeroBoostStatusText.text = GameLocalization.Text("shop.ad_loading");
		}
		RefreshDailyHeroBoostButtonState();
		MonetizationService.Ensure().ShowRewardedAd("daily_hero_boost_rewarded", delegate(RewardedAdResult result)
		{
			dailyHeroBoostAdRequestInProgress = false;
			if (!result.IsCompleted)
			{
				if (dailyHeroBoostStatusText != null)
				{
					dailyHeroBoostStatusText.text = ResolveBattleLobbyStatusMessage(string.IsNullOrWhiteSpace(result.Message) ? "shop.ad_not_ready" : result.Message);
				}
				RefreshDailyHeroBoostButtonState();
			}
			else
			{
				BattleDailyHeroBonusService.ActivateRewardedBoostForOneHour();
				EnsureDailyHeroBonusUi();
				if (dailyHeroBonusRoot != null)
				{
					dailyHeroBonusRoot.SetActive(value: true);
					dailyHeroBonusRoot.transform.SetAsLastSibling();
					SetDailyHeroBonusModalActive(active: true);
				}
			}
		});
	}

	private void ShowBattleShopEnergy()
	{
		SetBattleShopSection(shopEnergySection);
		RefreshBattleShopUi();
	}

	private void ShowBattleShopCharacters()
	{
		SetBattleShopSection(shopCharactersSection);
		RefreshBattleShopUi();
	}

	private void ShowBattleShopSkins()
	{
		if (!MonetizationService.ArePurchasesSupported)
		{
			ShowBattleShopEnergy();
			return;
		}

		SetBattleShopSection(shopSkinsSection);
		RefreshBattleShopUi();
	}

	private void ShowBattleShopBattleTiles()
	{
		SetBattleShopSection(shopBattleTilesSection);
		RefreshBattleShopUi();
	}

	private void SetBattleShopSection(GameObject activeSection)
	{
		if (shopEnergySection != null)
		{
			shopEnergySection.SetActive(shopEnergySection == activeSection);
		}
		if (shopCharactersSection != null)
		{
			shopCharactersSection.SetActive(shopCharactersSection == activeSection);
		}
		if (shopBattleTilesSection != null)
		{
			shopBattleTilesSection.SetActive(shopBattleTilesSection == activeSection);
		}
		if (shopSkinsSection != null)
		{
			shopSkinsSection.SetActive(shopSkinsSection == activeSection);
		}
		SetShopTabColor(shopEnergyTabButton, shopEnergySection == activeSection);
		SetShopTabColor(shopCharactersTabButton, shopCharactersSection == activeSection);
		SetShopTabColor(shopBattleTilesTabButton, shopBattleTilesSection == activeSection);
		SetShopTabColor(shopSkinsTabButton, shopSkinsSection == activeSection);
	}

	private void OnClickBuyEnergyWithAmetist()
	{
		EnsureCurrencyService();
		if (CurrencyService.I == null || !CurrencyService.I.CanSpendOzAmetist(shopEnergyAmetistPrice))
		{
			SetBattleShopStatus(GameLocalization.Text("battle.shop.not_enough_ametist"));
			RefreshBattleShopUi();
		}
		else if (!CurrencyService.I.SpendOzAmetist(shopEnergyAmetistPrice))
		{
			SetBattleShopStatus(GameLocalization.Text("battle.shop.purchase_failed"));
			RefreshBattleShopUi();
		}
		else if (!EnergyService.AddEnergy(shopEnergyAmount))
		{
			CurrencyService.I.AddOzAmetist(shopEnergyAmetistPrice);
			SetBattleShopStatus(GameLocalization.Text("battle.shop.purchase_failed"));
			RefreshBattleShopUi();
		}
		else
		{
			SetBattleShopStatus(GameLocalization.Format("battle.shop.energy_purchased", shopEnergyAmount));
			RefreshEnergyUi();
			RefreshBattleShopUi();
		}
	}

	private void OnClickBuyDragonMale()
	{
		TryBuyDragonCharacter("Dragon_Male", BattleCharacterDatabase.GetLocalizedDisplayName("Dragon_Male", "Древний"));
	}

	private void OnClickBuyDragonFemale()
	{
		TryBuyDragonCharacter("Dragon_Female", BattleCharacterDatabase.GetLocalizedDisplayName("Dragon_Female", "Древняя"));
	}

	private void TryBuyDragonCharacter(string characterId, string displayName)
	{
		EnsureCurrencyService();
		if (!BattleCharacterSelectionService.HasInstance)
		{
			SetBattleShopStatus(GameLocalization.Text("battle.shop.character_loading"));
			RefreshBattleShopUi();
			return;
		}
		bool flag = BattleCharacterSelectionService.Instance.IsPersistentlyUnlocked(characterId);
		if (flag)
		{
			SetBattleShopStatus(BattleLobbyText("Персонаж уже куплен.", "Character already purchased.", "Karakter zaten satin alindi.", "Charakter bereits gekauft."));
			RefreshBattleShopUi();
		}
		else if (!flag && (CurrencyService.I == null || !CurrencyService.I.CanSpendOzAmetist(shopDragonAmetistPrice)))
		{
			SetBattleShopStatus(GameLocalization.Text("battle.shop.not_enough_ametist"));
			RefreshBattleShopUi();
		}
		else if (!BattleCharacterSelectionService.Instance.TryUnlockCharacterWithAmetist(characterId, shopDragonAmetistPrice))
		{
			SetBattleShopStatus(GameLocalization.Text("battle.shop.character_failed"));
			RefreshBattleShopUi();
		}
		else
		{
			SetBattleShopStatus(GameLocalization.Format("battle.shop.character_unlocked", displayName));
			RefreshBattleShopUi();
		}
	}

	private void OnClickBuyAmetistSmall()
	{
		BuyAmetistPackage("oz_ametist_small");
	}

	private void OnClickBuyAmetistMedium()
	{
		BuyAmetistPackage("oz_ametist_medium");
	}

	private void OnClickBuyAmetistBig()
	{
		BuyAmetistPackage("oz_ametist_big");
	}

	private void OnClickBuyAmetistLegend()
	{
		BuyAmetistPackage("oz_ametist_legend");
	}

	private void BuyAmetistPackage(string productId)
	{
		if (!MonetizationService.ArePurchasesSupported)
		{
			SetBattleShopStatus(GameLocalization.Text("shop.purchase_not_ready"));
			return;
		}

		SetBattleShopStatus(GameLocalization.Text("battle.shop.opening_purchase"));
		OzAmetistShopService.TryPurchaseAmetistPackage(productId, delegate(bool success, int amount, string message)
		{
			SetBattleShopStatus(success ? GameLocalization.Format("battle.shop.ametist_added", amount) : ResolveBattleShopPurchaseMessage(message));
			RefreshBattleShopUi();
		});
	}

	private void OnClickOpenDailyBattleTilePack()
	{
		if (battleTilePackAdRequestInProgress)
		{
			return;
		}
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile == null || battleTileStore == null)
		{
			SetBattleShopStatus(BattleLobbyText("Профиль или камни еще загружаются.", "Profile or tiles are still loading.", "Profil veya taşlar yukleniyor.", "Profil oder Steine laden noch."));
			RefreshBattleShopUi();
			return;
		}
		if (!BattleTilePackShopService.CanClaimDailyAd(playerProfile))
		{
			SetBattleShopStatus(BattleLobbyText("Дневной рекламный пак уже открыт.", "Daily ad pack already opened.", "Günlük reklam paketi acildi.", "Taegliches Werbepaket bereits geoeffnet."));
			RefreshBattleShopUi();
			return;
		}
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("battle_tile_pack_rewarded");
		if (!rewardedAdAvailability.IsReady)
		{
			SetBattleShopStatus(ResolveBattleShopPurchaseMessage(rewardedAdAvailability.Message));
			RefreshBattleShopUi();
			return;
		}
		battleTilePackAdRequestInProgress = true;
		SetBattleShopStatus(BattleLobbyText("Открываем рекламу...", "Opening ad...", "Reklam aciliyor...", "Werbung wird geoeffnet..."));
		BattleTilePackShopService.TryOpenRewardedDailyPack(playerProfile, battleTileStore, delegate(BattleTilePackResult result)
		{
			battleTilePackAdRequestInProgress = false;
			HandleBattleTilePackOpenResult(result);
		});
	}

	private void OnClickOpenMediumBattleTilePack()
	{
		OpenPaidBattleTilePack(BattleTilePackId.OzTileMedium);
	}

	private void OnClickOpenHighBattleTilePack()
	{
		OpenPaidBattleTilePack(BattleTilePackId.OzTileHigh);
	}

	private void OnClickOpenAmetistBattleTilePack()
	{
		OpenPaidBattleTilePack(BattleTilePackId.AmetistPremium);
	}

	private void OpenPaidBattleTilePack(BattleTilePackId packId)
	{
		PlayerProfile profile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		BattleTilePackResult result = BattleTilePackShopService.TryOpenPaidPack(profile, store, packId);
		HandleBattleTilePackOpenResult(result);
	}

	private void HandleBattleTilePackOpenResult(BattleTilePackResult result)
	{
		RefreshBattleShopUi();
		RefreshBattleTileInventoryUi();
		if (result == null || !result.Success || result.Rolls == null || result.Rolls.Count == 0)
		{
			SetBattleShopStatus(FormatBattleTilePackResult(result));
			return;
		}
		SetBattleShopStatus(string.Empty);
		pendingBattleTilePackResult = result;
		pendingBattleTilePackProfileKey = GetCurrentBattleTilePackProfileKey();
		ShowBattleTilePackResult(result);
	}

	private void ShowPendingBattleTilePackResult()
	{
		if (pendingBattleTilePackResult == null)
		{
			return;
		}
		string currentProfileKey = GetCurrentBattleTilePackProfileKey();
		if (!string.Equals(pendingBattleTilePackProfileKey, currentProfileKey, StringComparison.Ordinal))
		{
			return;
		}
		SetBattleShopStatus(string.Empty);
		ShowBattleTilePackResult(pendingBattleTilePackResult);
	}

	private static string GetCurrentBattleTilePackProfileKey()
	{
		PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
		if (profile == null)
		{
			return "default";
		}
		profile.EnsureData();
		return string.IsNullOrWhiteSpace(profile.LocalProfileId) ? "default" : profile.LocalProfileId.Trim();
	}

	private void ShowBattleTilePackResult(BattleTilePackResult result)
	{
		if (battleShopRoot == null || result?.Rolls == null || result.Rolls.Count == 0)
		{
			return;
		}
		CloseBattleTilePackResult();
		battleTilePackResultRoot = new GameObject("BattleTilePackResultOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
		battleTilePackResultRoot.transform.SetParent(battleShopRoot.transform, worldPositionStays: false);
		RectTransform overlayRect = battleTilePackResultRoot.GetComponent<RectTransform>();
		overlayRect.anchorMin = Vector2.zero;
		overlayRect.anchorMax = Vector2.one;
		overlayRect.offsetMin = Vector2.zero;
		overlayRect.offsetMax = Vector2.zero;
		Image overlayImage = battleTilePackResultRoot.GetComponent<Image>();
		overlayImage.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby");
		overlayImage.type = Image.Type.Simple;
		overlayImage.preserveAspect = false;
		overlayImage.color = overlayImage.sprite != null ? Color.white : new Color(0.025f, 0.018f, 0.014f, 0.98f);
		overlayImage.raycastTarget = true;
		CanvasGroup overlayGroup = battleTilePackResultRoot.GetComponent<CanvasGroup>();
		overlayGroup.alpha = 0f;
		overlayGroup.interactable = true;
		overlayGroup.blocksRaycasts = true;

		TMP_Text title = CreateShopText(battleTilePackResultRoot.transform, "Title", BattleLobbyText("ПАК ОТКРЫТ", "PACK OPENED", "PAKET AÇILDI", "PAKET GEÖFFNET"), new Vector2(0f, 410f), new Vector2(1180f, 84f), 66f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.34f, 1f));
		title.fontStyle = FontStyles.Bold;
		title.outlineWidth = 0.18f;
		title.outlineColor = new Color32(55, 18, 4, 255);

		int burnedCount = 0;
		List<BattleTilePackRoll> visibleRolls = new List<BattleTilePackRoll>();
		for (int i = 0; i < result.Rolls.Count; i++)
		{
			BattleTilePackRoll roll = result.Rolls[i];
			if (roll == null || roll.Tile == null)
			{
				continue;
			}
			if (roll.AutoSold)
			{
				burnedCount++;
			}
			else
			{
				visibleRolls.Add(roll);
			}
		}

		Image burnPanel = CreateShopImage(battleTilePackResultRoot.transform, "BurnSummary", new Vector2(0f, 320f), new Vector2(820f, 78f), raycastTarget: false);
		burnPanel.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWideAlt");
		burnPanel.type = Image.Type.Simple;
		burnPanel.preserveAspect = false;
		burnPanel.color = Color.white;
		CreateShopText(burnPanel.transform, "Burned", BattleLobbyText("Сгорело стандартных:", "Standard burned:", "Yanan standart:", "Standard verbrannt:") + " " + burnedCount, new Vector2(-130f, 0f), new Vector2(500f, 52f), 34f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.34f, 1f));
		Image ozTileIcon = CreateShopImage(burnPanel.transform, "OzTileIcon", new Vector2(190f, 0f), new Vector2(50f, 50f), raycastTarget: false);
		ozTileIcon.sprite = LoadBattleLobbyOzTileIcon();
		ozTileIcon.enabled = ozTileIcon.sprite != null;
		CreateShopText(burnPanel.transform, "OzTileValue", "+" + Mathf.Max(0, result.AutoSoldOzTile), new Vector2(286f, 0f), new Vector2(130f, 52f), 38f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.42f, 1f));

		List<CanvasGroup> rewardGroups = CreateBattleTilePackResultGrid(battleTilePackResultRoot.transform, visibleRolls);
		Button confirmButton = CreateShopButton(battleTilePackResultRoot.transform, "ButtonConfirmBattleTilePackResult", BattleLobbyText("ОК", "OK", "Tamam", "OK"), new Vector2(0f, -430f), new Vector2(360f, 90f), Color.white, 40f);
		confirmButton.onClick.AddListener(ConfirmBattleTilePackResult);
		confirmButton.transform.SetAsLastSibling();
		battleTilePackResultRoot.transform.SetAsLastSibling();
		battleTilePackResultRevealRoutine = StartCoroutine(AnimateBattleTilePackResult(overlayGroup, rewardGroups));
	}

	private List<CanvasGroup> CreateBattleTilePackResultGrid(Transform parent, List<BattleTilePackRoll> rolls)
	{
		const float viewportWidth = 1660f;
		const float viewportHeight = 590f;
		const int columns = 8;
		const float columnStep = 190f;
		const float rowStep = 230f;
		int rewardCount = rolls?.Count ?? 0;
		int rows = Mathf.Max(1, Mathf.CeilToInt(rewardCount / (float)columns));
		float contentHeight = Mathf.Max(viewportHeight, rows * rowStep + 20f);

		GameObject viewportObject = new GameObject("RewardsViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
		viewportObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
		viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
		viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
		viewportRect.pivot = new Vector2(0.5f, 0.5f);
		viewportRect.anchoredPosition = new Vector2(0f, -45f);
		viewportRect.sizeDelta = new Vector2(viewportWidth, viewportHeight);
		Image viewportImage = viewportObject.GetComponent<Image>();
		viewportImage.sprite = null;
		viewportImage.color = new Color(0f, 0f, 0f, 0.08f);
		viewportImage.raycastTarget = true;

		GameObject contentObject = new GameObject("Content", typeof(RectTransform));
		contentObject.transform.SetParent(viewportObject.transform, worldPositionStays: false);
		RectTransform contentRect = contentObject.GetComponent<RectTransform>();
		contentRect.anchorMin = new Vector2(0.5f, 1f);
		contentRect.anchorMax = new Vector2(0.5f, 1f);
		contentRect.pivot = new Vector2(0.5f, 0.5f);
		contentRect.anchoredPosition = new Vector2(0f, contentHeight * -0.5f);
		contentRect.sizeDelta = new Vector2(1560f, contentHeight);

		ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
		scrollRect.content = contentRect;
		scrollRect.viewport = viewportRect;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.inertia = true;
		scrollRect.scrollSensitivity = 42f;
		scrollRect.verticalNormalizedPosition = 1f;

		List<CanvasGroup> groups = new List<CanvasGroup>();
		if (rewardCount <= 0)
		{
			CreateShopText(contentRect, "EmptyRewards", BattleLobbyText("Все камни сгорели и превратились в OzTile", "All tiles burned into OzTile", "Tüm taşlar yandı ve OzTile oldu", "Alle Steine wurden zu OzTile verbrannt"), Vector2.zero, new Vector2(1100f, 100f), 42f, TextAlignmentOptions.Center, new Color(1f, 0.72f, 0.38f, 1f));
			return groups;
		}

		for (int i = 0; i < rewardCount; i++)
		{
			int row = i / columns;
			int column = i % columns;
			int itemsInRow = Mathf.Min(columns, rewardCount - row * columns);
			float rowStartX = (itemsInRow - 1) * columnStep * -0.5f;
			float x = rowStartX + column * columnStep;
			float y = contentHeight * 0.5f - 120f - row * rowStep;
			CanvasGroup cardGroup = CreateBattleTilePackResultCard(contentRect, rolls[i], i, new Vector2(x, y));
			if (cardGroup != null)
			{
				groups.Add(cardGroup);
			}
		}
		return groups;
	}

	private CanvasGroup CreateBattleTilePackResultCard(Transform parent, BattleTilePackRoll roll, int index, Vector2 position)
	{
		if (roll?.Tile == null)
		{
			return null;
		}
		Image card = CreateShopImage(parent, "Reward_" + index, position, new Vector2(180f, 220f), raycastTarget: false);
		card.sprite = null;
		card.color = Color.clear;
		CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
		group.alpha = 0f;
		group.interactable = false;
		group.blocksRaycasts = false;
		card.rectTransform.localScale = new Vector3(0.82f, 0.82f, 1f);

		Image frame = CreateShopImage(card.transform, "Frame", Vector2.zero, new Vector2(220f, 180f), raycastTarget: false);
		frame.sprite = LoadResourceSprite("Mahjong/Sprites/BattleLobbyParts/PartWide");
		frame.type = Image.Type.Simple;
		frame.preserveAspect = false;
		frame.color = Color.white;
		frame.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);

		Sprite faceSprite = roll.Tile.Prefab != null ? (roll.Tile.Prefab.FaceSprite != null ? roll.Tile.Prefab.FaceSprite : roll.Tile.Prefab.BackSprite) : null;
		Image face = CreateShopImage(card.transform, "Face", new Vector2(0f, 31f), new Vector2(110f, 136f), raycastTarget: false);
		face.sprite = faceSprite;
		face.enabled = face.sprite != null;
		if (!face.enabled)
		{
			CreateShopText(card.transform, "FaceFallback", "?", new Vector2(0f, 31f), new Vector2(110f, 120f), 62f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.42f, 1f));
		}

		TMP_Text nameText = CreateShopText(card.transform, "Name", ResolveBattleTileLocalizedName(roll.Tile.Id, roll.Tile), new Vector2(0f, -50f), new Vector2(150f, 36f), 24f, TextAlignmentOptions.Center, Color.white);
		nameText.fontSizeMin = 15f;
		nameText.textWrappingMode = TextWrappingModes.Normal;
		nameText.overflowMode = TextOverflowModes.Ellipsis;
		CreateShopText(card.transform, "Rarity", ResolveBattleTileRarityDisplayName(roll.Rarity), new Vector2(0f, -80f), new Vector2(150f, 28f), 21f, TextAlignmentOptions.Center, ResolveBattleTileRarityColor(roll.Rarity));
		string stateLabel = roll.IsNew
			? BattleLobbyText("НОВЫЙ", "NEW", "YENİ", "NEU")
			: BattleLobbyText("КОПИЯ", "COPY", "KOPYA", "KOPIE");
		CreateShopText(card.transform, "State", stateLabel, new Vector2(0f, -101f), new Vector2(142f, 22f), 18f, TextAlignmentOptions.Center, roll.IsNew ? new Color(0.62f, 1f, 0.65f, 1f) : new Color(0.82f, 0.78f, 0.7f, 1f));
		return group;
	}

	private IEnumerator AnimateBattleTilePackResult(CanvasGroup overlayGroup, List<CanvasGroup> rewardGroups)
	{
		float elapsed = 0f;
		const float fadeDuration = 0.18f;
		while (elapsed < fadeDuration && overlayGroup != null)
		{
			elapsed += Time.unscaledDeltaTime;
			overlayGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
			yield return null;
		}
		if (overlayGroup != null)
		{
			overlayGroup.alpha = 1f;
		}
		if (rewardGroups != null)
		{
			for (int i = 0; i < rewardGroups.Count; i++)
			{
				CanvasGroup group = rewardGroups[i];
				if (group == null)
				{
					continue;
				}
				group.alpha = 1f;
				group.transform.localScale = Vector3.one;
				yield return new WaitForSecondsRealtime(0.035f);
			}
		}
		battleTilePackResultRevealRoutine = null;
	}

	private void CloseBattleTilePackResult()
	{
		StopBattleTilePackResultRevealRoutine();
		if (battleTilePackResultRoot != null)
		{
			UnityEngine.Object.Destroy(battleTilePackResultRoot);
			battleTilePackResultRoot = null;
		}
	}

	private void CreateBattleTileUpgradePips(Transform parent, Vector2 facePosition, Vector2 faceSize, int upgradeLevel)
	{
		BattleTileUpgradeVisual.Apply(parent, facePosition, faceSize, upgradeLevel);
	}

	private void ConfirmBattleTilePackResult()
	{
		pendingBattleTilePackResult = null;
		pendingBattleTilePackProfileKey = string.Empty;
		CloseBattleTilePackResult();
	}

	private void StopBattleTilePackResultRevealRoutine()
	{
		if (battleTilePackResultRevealRoutine != null)
		{
			StopCoroutine(battleTilePackResultRevealRoutine);
			battleTilePackResultRevealRoutine = null;
		}
	}

	private string FormatBattleTilePackResult(BattleTilePackResult result)
	{
		if (result == null)
		{
			return BattleLobbyText("Пак не открылся.", "Pack did not open.", "Paket acilmadi.", "Paket wurde nicht geoeffnet.");
		}
		if (!result.Success)
		{
			if (!string.IsNullOrWhiteSpace(result.Message) && result.Message.IndexOf("currency", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return BattleLobbyText("Не хватает валюты.", "Not enough currency.", "Para yeterli değil.", "Nicht genug Waehrung.");
			}
			return ResolveBattleShopPurchaseMessage(result.Message);
		}
		if (result.Rolls == null || result.Rolls.Count == 0)
		{
			return BattleLobbyText("Пак пуст.", "Pack is empty.", "Paket bos.", "Paket ist leer.");
		}
		BattleTilePackRoll battleTilePackRoll = null;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < result.Rolls.Count; i++)
		{
			BattleTilePackRoll battleTilePackRoll2 = result.Rolls[i];
			if (battleTilePackRoll2 != null && battleTilePackRoll2.Tile != null)
			{
				if (!battleTilePackRoll2.AutoSold)
				{
					num2++;
				}
				if (battleTilePackRoll2.IsNew)
				{
					num++;
				}
				if (battleTilePackRoll2.Guaranteed)
				{
					num3++;
				}
				if (battleTilePackRoll2.Pity)
				{
					num4++;
				}
				if (!battleTilePackRoll2.AutoSold && (battleTilePackRoll == null || battleTilePackRoll2.Rarity > battleTilePackRoll.Rarity))
				{
					battleTilePackRoll = battleTilePackRoll2;
				}
			}
		}
		if (battleTilePackRoll == null || battleTilePackRoll.Tile == null)
		{
			return BattleLobbyText("Пак открыт: ", "Pack opened: ", "Paket acildi: ", "Paket geoeffnet: ") + result.Rolls.Count + BattleLobbyText(" кам., авто-продажа: +", " stones, auto-sold: +", " taş, oto satis: +", " Steine, Auto-Verkauf: +") + result.AutoSoldOzTile + " OzTile";
		}
		string text = ResolveBattleTileLocalizedName(battleTilePackRoll.Tile.Id, battleTilePackRoll.Tile);
		string text2 = ResolveBattleTileRarityDisplayName(battleTilePackRoll.Rarity);
		return BattleLobbyText("Пак открыт: ", "Pack opened: ", "Paket acildi: ", "Paket geoeffnet: ") + result.Rolls.Count + BattleLobbyText(" кам., в сумку: ", " stones, stored: ", " taş, canta: ", " Steine, Taşche: ") + num2 + BattleLobbyText(", новых: ", ", new: ", ", yeni: ", ", neu: ") + num + BattleLobbyText(", авто: +", ", auto: +", ", oto: +", ", auto: +") + result.AutoSoldOzTile + " OzTile / " + text + " [" + text2 + "]" + FormatBattleTilePackSafetyText(num3, num4);
	}

	private string FormatBattleTilePackSafetyText(int guaranteedCount, int pityCount)
	{
		string text = string.Empty;
		if (guaranteedCount > 0)
		{
			text = text + BattleLobbyText(" / гарантия: ", " / guarantee: ", " / garanti: ", " / Garantie: ") + guaranteedCount;
		}
		if (pityCount > 0)
		{
			text = text + BattleLobbyText(" / pity: ", " / pity: ", " / pity: ", " / Pity: ") + pityCount;
		}
		return text;
	}

	private static string ResolveBattleShopPurchaseMessage(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return GameLocalization.Text("shop.purchase_not_ready");
		}
		string text = GameLocalization.Text(message);
		if (!(text == message))
		{
			return text;
		}
		return message;
	}

	private void RefreshBattleShopUi()
	{
		if (battleShopRoot == null)
		{
			return;
		}
		EnsureCurrencyService();
		int num = ((CurrencyService.I != null) ? CurrencyService.I.GetOzAmetist() : 0);
		int num2 = ((CurrencyService.I != null) ? CurrencyService.I.GetOzTile() : 0);
		int currentEnergy = EnergyService.CurrentEnergy;
		int currentMaxEnergy = EnergyService.CurrentMaxEnergy;
		if (battleShopStatusText != null && string.Equals(battleShopStatusText.text, GameLocalization.Text("shop.ad_ready"), StringComparison.Ordinal))
		{
			battleShopStatusText.text = string.Empty;
		}
		if (battleShopBalanceText != null)
		{
			battleShopBalanceText.text = string.Empty;
		}
		if (battleShopOzTileBalanceText != null)
			battleShopOzTileBalanceText.text = CompactNumberFormatter.FormatCurrency(num2);
		if (battleShopAmetistBalanceText != null)
			battleShopAmetistBalanceText.text = CompactNumberFormatter.FormatCurrency(num);
		if (battleShopEnergyBalanceText != null)
			battleShopEnergyBalanceText.text = CompactNumberFormatter.FormatCurrency(currentEnergy) + "/" + CompactNumberFormatter.FormatCurrency(currentMaxEnergy);
		CenterShopCurrencyContent(battleShopOzTileBalanceText, "ShopOzTileIcon");
		CenterShopCurrencyContent(battleShopAmetistBalanceText, "ShopAmetistIcon");
		CenterShopCurrencyContent(battleShopEnergyBalanceText, "ShopEnergyIcon");
		if (shopBuyEnergyButton != null)
		{
			shopBuyEnergyButton.interactable = CurrencyService.I != null && CurrencyService.I.CanSpendOzAmetist(shopEnergyAmetistPrice);
		}
		if (shopRewardedEnergyButton != null)
		{
			RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("battle_energy_rewarded");
			shopRewardedEnergyButton.interactable = EnergyService.CanClaimRewardedAdEnergy() && rewardedAdAvailability.IsReady;
			if (battleShopStatusText != null && string.IsNullOrWhiteSpace(battleShopStatusText.text) && EnergyService.CanClaimRewardedAdEnergy() && !rewardedAdAvailability.IsReady)
			{
				battleShopStatusText.text = ResolveBattleShopPurchaseMessage(rewardedAdAvailability.Message);
			}
		}
		RefreshDragonShopButton(shopBuyDragonMaleButton, "Dragon_Male");
		RefreshDragonShopButton(shopBuyDragonFemaleButton, "Dragon_Female");
		RefreshBattleTilePackButtons();
	}

	private static void CenterShopCurrencyContent(TMP_Text valueText, string iconObjectName)
	{
		if (valueText == null || valueText.transform.parent == null)
		{
			return;
		}
		Transform iconTransform = valueText.transform.parent.Find(iconObjectName);
		RectTransform iconRect = iconTransform as RectTransform;
		RectTransform textRect = valueText.rectTransform;
		if (iconRect == null)
		{
			return;
		}
		const float gap = 18f;
		float iconWidth = iconRect.sizeDelta.x;
		float textWidth = Mathf.Min(valueText.GetPreferredValues(valueText.text).x, textRect.sizeDelta.x);
		float left = (iconWidth + gap + textWidth) * -0.5f;
		iconRect.anchoredPosition = new Vector2(left + iconWidth * 0.5f, 0f);
		textRect.anchoredPosition = new Vector2(left + iconWidth + gap, 0f);
	}

	private void RefreshBattleTilePackButtons()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("battle_tile_pack_rewarded");
		bool dailyPackClaimed = playerProfile != null && BattleTilePackShopService.HasClaimedDailyAd(playerProfile);
		SetBattleTilePackButtonState(shopBattleTileDailyAdButton, playerProfile != null && BattleTilePackShopService.CanClaimDailyAd(playerProfile) && rewardedAdAvailability.IsReady && !battleTilePackAdRequestInProgress, dailyPackClaimed);
		SetBattleTilePackButtonState(shopBattleTileMediumButton, CurrencyService.I != null && CurrencyService.I.CanSpendOzTile(BattleTilePackShopService.MediumOzTileCost));
		SetBattleTilePackButtonState(shopBattleTileHighButton, CurrencyService.I != null && CurrencyService.I.CanSpendOzTile(BattleTilePackShopService.HighOzTileCost));
		SetBattleTilePackButtonState(shopBattleTileAmetistButton, CurrencyService.I != null && CurrencyService.I.CanSpendOzAmetist(BattleTilePackShopService.PremiumAmetistCost));
		SetBattleTilePackButtonLabel(shopBattleTileDailyAdButton, dailyPackClaimed
			? BattleLobbyText("Получено", "Claimed", "Alındı", "Erhalten")
			: GetBattleTilePackActionText(BattleTilePackId.DailyAd));
		SetBattleTilePackButtonLabel(shopBattleTileMediumButton, GetBattleTilePackActionText(BattleTilePackId.OzTileMedium));
		SetBattleTilePackButtonLabel(shopBattleTileHighButton, GetBattleTilePackActionText(BattleTilePackId.OzTileHigh));
		SetBattleTilePackButtonLabel(shopBattleTileAmetistButton, GetBattleTilePackActionText(BattleTilePackId.AmetistPremium));
		if (shopBattleTilesSection != null && shopBattleTilesSection.activeSelf && battleShopStatusText != null && string.IsNullOrWhiteSpace(battleShopStatusText.text) && playerProfile != null && BattleTilePackShopService.CanClaimDailyAd(playerProfile) && !rewardedAdAvailability.IsReady)
		{
			battleShopStatusText.text = ResolveBattleShopPurchaseMessage(rewardedAdAvailability.Message);
		}
	}

	private static void SetBattleTilePackButtonState(Button button, bool interactable, bool keepFullVisualWhenDisabled = false)
	{
		if (!(button == null))
		{
			ColorBlock colors = button.colors;
			colors.disabledColor = Color.white;
			button.colors = colors;
			button.interactable = interactable;
			Image component = button.GetComponent<Image>();
			if (component != null)
			{
				component.color = (interactable || keepFullVisualWhenDisabled) ? Color.white : new Color(0.45f, 0.45f, 0.48f, 1f);
			}
		}
	}

	private void RefreshDragonShopButton(Button button, string characterId)
	{
		if (!(button == null))
		{
			bool serviceReady = BattleCharacterSelectionService.HasInstance;
			bool isOwned = serviceReady && BattleCharacterSelectionService.Instance.IsPersistentlyUnlocked(characterId);
			bool canBuy = serviceReady && !isOwned && CurrencyService.I != null && CurrencyService.I.CanSpendOzAmetist(shopDragonAmetistPrice);
			button.interactable = canBuy;
			TMP_Text buttonLabel = button.transform.Find("Label")?.GetComponent<TMP_Text>();
			if (buttonLabel != null)
			{
				buttonLabel.text = isOwned
					? BattleLobbyText("В КОЛЛЕКЦИИ", "OWNED", "KOLEKSİYONDA", "IN SAMMLUNG")
					: BattleLobbyText("КУПИТЬ", "BUY", "SATIN AL", "KAUFEN");
				buttonLabel.color = isOwned ? new Color(0.78f, 0.94f, 0.72f, 1f) : (canBuy ? Color.white : new Color(0.68f, 0.68f, 0.68f, 1f));
			}
			ColorBlock colors = button.colors;
			colors.disabledColor = Color.white;
			button.colors = colors;
			Image buttonImage = button.GetComponent<Image>();
			if (buttonImage != null)
			{
				buttonImage.color = isOwned ? new Color(0.58f, 0.66f, 0.54f, 0.96f) : (canBuy ? Color.white : new Color(0.48f, 0.48f, 0.48f, 0.9f));
			}
			Image cardImage = ((button.transform.parent != null) ? button.transform.parent.GetComponent<Image>() : null);
			if (cardImage != null)
			{
				cardImage.color = Color.white;
			}
			Transform priceTransform = ((button.transform.parent != null) ? button.transform.parent.Find("Price") : null);
			TMP_Text priceText = ((priceTransform != null) ? priceTransform.GetComponent<TMP_Text>() : null);
			if (priceText != null)
			{
				priceText.gameObject.SetActive(!isOwned);
				priceText.text = shopDragonAmetistPrice + " " + BattleLobbyText("Аметист", "Ametist", "Ametist", "Ametist");
			}
		}
	}

	private void SetBattleShopStatus(string message)
	{
		if (battleShopStatusText != null)
		{
			battleShopStatusText.text = message ?? string.Empty;
		}
	}

	private static void EnsureCurrencyService()
	{
		if (!(CurrencyService.I != null))
		{
			new GameObject("CurrencyService").AddComponent<CurrencyService>();
		}
	}

	private static GameObject CreateShopPanel(Transform parent, string objectName, Vector2 size, Vector2 position, Color color)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		component2.color = color;
		component2.raycastTarget = true;
		if (string.Equals(objectName, "WeeklyRewardPanel", StringComparison.Ordinal))
		{
			ApplyWeeklyRewardPanelWindow(component2);
			return obj;
		}
		if (string.Equals(objectName, "BattleShopPanel", StringComparison.Ordinal) || string.Equals(objectName, "BattleTileInventoryPanel", StringComparison.Ordinal) || string.Equals(objectName, "BattleTileProfilePanel", StringComparison.Ordinal) || string.Equals(objectName, "TutorialGatePanel", StringComparison.Ordinal) || string.Equals(objectName, "TournamentComingSoonPanel", StringComparison.Ordinal))
		{
			if (!BattlePopupStyle.ApplyWindow(component2))
			{
				component2.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);
				component2.raycastTarget = true;
			}
			return obj;
		}
		if (IsBattleTileInventoryMajorBlock(objectName))
		{
			ApplyBattleTileInventoryBlockPanel(component2);
			return obj;
		}
		if (IsBattleShopSectionContainer(objectName))
		{
			component2.sprite = null;
			component2.color = Color.clear;
			component2.raycastTarget = false;
			return obj;
		}
		if (IsBattleShopLightPanel(objectName))
		{
			if (!BattlePopupStyle.ApplyWindow(component2, true))
			{
				component2.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);
				component2.raycastTarget = true;
			}
			return obj;
		}
		if (string.Equals(objectName, "HeroProfilePanel", StringComparison.Ordinal))
		{
			BattlePopupStyle.ApplyFront(component2);
			return obj;
		}
		if (objectName != null && objectName.StartsWith("RewardDay", StringComparison.Ordinal))
		{
			ApplyWeeklyRewardWindow(component2);
			return obj;
		}
		if (objectName != null && (objectName.EndsWith("Section", StringComparison.Ordinal) || objectName.EndsWith("Card", StringComparison.Ordinal) || objectName.EndsWith("Pocket", StringComparison.Ordinal)))
		{
			BattlePopupStyle.ApplyFront(component2);
		}
		return obj;
	}

	private static bool IsBattleShopLightPanel(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return false;
		}
		return string.Equals(objectName, "DragonMaleCard", StringComparison.Ordinal)
			|| string.Equals(objectName, "DragonFemaleCard", StringComparison.Ordinal)
			|| string.Equals(objectName, "AmetistSmallCard", StringComparison.Ordinal)
			|| string.Equals(objectName, "AmetistMediumCard", StringComparison.Ordinal)
			|| string.Equals(objectName, "AmetistBigCard", StringComparison.Ordinal)
			|| string.Equals(objectName, "AmetistLegendCard", StringComparison.Ordinal);
	}

	private static bool IsBattleShopSectionContainer(string objectName)
	{
		return string.Equals(objectName, "EnergySection", StringComparison.Ordinal)
			|| string.Equals(objectName, "CharactersSection", StringComparison.Ordinal)
			|| string.Equals(objectName, "BattleTilesSection", StringComparison.Ordinal)
			|| string.Equals(objectName, "AmetistSection", StringComparison.Ordinal);
	}

	private static bool IsBattleTileInventoryMajorBlock(string objectName)
	{
		if (!string.Equals(objectName, "HeroProfilePanel", StringComparison.Ordinal) && !string.Equals(objectName, "ActivePocket", StringComparison.Ordinal))
		{
			return string.Equals(objectName, "ReservePocket", StringComparison.Ordinal);
		}
		return true;
	}

	private static void ApplyBattleTileInventoryBlockPanel(Image image)
	{
		if (!(image == null))
		{
			if (!BattlePopupStyle.ApplyWindow(image))
			{
				BattlePopupStyle.ApplyFront(image);
			}
			image.raycastTarget = true;
		}
	}

	private static void ApplyWeeklyRewardPanelWindow(Image image)
	{
		BattlePopupStyle.ApplyWindow(image);
	}

	private static Sprite LoadWeeklyRewardPanelWindowSprite()
	{
		if (cachedWeeklyRewardPanelWindowSprite != null)
		{
			return cachedWeeklyRewardPanelWindowSprite;
		}
		cachedWeeklyRewardPanelWindowSprite = Resources.Load<Sprite>("Mahjong/Sprites/Rewards/RewardWindowWeekly");
		if (cachedWeeklyRewardPanelWindowSprite != null)
		{
			return cachedWeeklyRewardPanelWindowSprite;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/Rewards/RewardWindowWeekly");
		if (texture2D != null)
		{
			Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
			cachedWeeklyRewardPanelWindowSprite = Sprite.Create(texture2D, rect, new Vector2(0.5f, 0.5f), 100f);
		}
		return cachedWeeklyRewardPanelWindowSprite;
	}

	private static void ApplyWeeklyRewardWindow(Image image)
	{
		BattlePopupStyle.ApplyFront(image);
	}

	private static Sprite LoadWeeklyRewardWindowSprite()
	{
		if (cachedWeeklyRewardWindowSprite != null)
		{
			return cachedWeeklyRewardWindowSprite;
		}
		cachedWeeklyRewardWindowSprite = Resources.Load<Sprite>("Mahjong/Sprites/Rewards/WeeklyWindow");
		if (cachedWeeklyRewardWindowSprite != null)
		{
			return cachedWeeklyRewardWindowSprite;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/Rewards/WeeklyWindow");
		if (texture2D != null)
		{
			Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
			cachedWeeklyRewardWindowSprite = Sprite.Create(texture2D, rect, new Vector2(0.5f, 0.5f), 100f);
		}
		return cachedWeeklyRewardWindowSprite;
	}

	private static void ConfigureOverlayCanvas(GameObject target)
	{
		if (!(target == null))
		{
			Canvas canvas = target.GetComponent<Canvas>();
			if (canvas == null)
			{
				canvas = target.AddComponent<Canvas>();
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = 30020;
			if (target.GetComponent<GraphicRaycaster>() == null)
			{
				target.AddComponent<GraphicRaycaster>();
			}
		}
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
				panel.anchoredPosition = Vector2.zero;
			}
		}
	}

	private static void FitPanelInsideCanvasPercent(RectTransform panel, Canvas canvas, float percent, float padding)
	{
		RectTransform rectTransform = ((canvas != null) ? (canvas.transform as RectTransform) : null);
		if (panel == null || rectTransform == null)
		{
			return;
		}
		Vector2 size = rectTransform.rect.size;
		if (!(size.x <= 1f) && !(size.y <= 1f))
		{
			percent = Mathf.Clamp(percent, 0.5f, 0.98f);
			Vector2 vector = size * percent - Vector2.one * Mathf.Max(0f, padding * 2f);
			if (!(vector.x <= 1f) && !(vector.y <= 1f))
			{
				Vector2 sizeDelta = panel.sizeDelta;
				float num = Mathf.Min(vector.x / Mathf.Max(1f, sizeDelta.x), vector.y / Mathf.Max(1f, sizeDelta.y));
				panel.localScale = Vector3.one * num;
				panel.anchoredPosition = Vector2.zero;
			}
		}
	}

	private static TMP_Text CreateShopText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
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
		component2.fontSizeMin = 13f;
		component2.fontSizeMax = fontSize;
		component2.alignment = alignment;
		component2.color = color;
		component2.raycastTarget = false;
		BattlePopupStyle.ApplyText(component2);
		component2.color = color;
		return component2;
	}

	private static Image CreateShopImage(Transform parent, string objectName, Vector2 position, Vector2 size, bool raycastTarget)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		component2.color = Color.white;
		component2.preserveAspect = true;
		component2.raycastTarget = raycastTarget;
		return component2;
	}

	private static Sprite GetWeeklyRewardSprite(int index)
	{
		if (cachedWeeklyRewardSprites == null)
		{
			cachedWeeklyRewardSprites = LoadWeeklyRewardSprites();
		}
		if (cachedWeeklyRewardSprites == null || cachedWeeklyRewardSprites.Length == 0)
		{
			return null;
		}
		index = Mathf.Clamp(index, 0, cachedWeeklyRewardSprites.Length - 1);
		return cachedWeeklyRewardSprites[index];
	}

	private static Vector2 GetWeeklyRewardSlotPosition(int dayIndex)
	{
		if (dayIndex < 4)
		{
			return new Vector2(-630f + (float)dayIndex * 420f, 128f);
		}
		return new Vector2(-420f + (float)(dayIndex - 4) * 420f, -160f);
	}

	private static int GetWeeklyRewardSpriteIndexForDay(int dayIndex)
	{
		return dayIndex switch
		{
			2 => 4, 
			3 => 2, 
			4 => 5, 
			5 => 3, 
			_ => dayIndex, 
		};
	}

	private static Sprite[] LoadWeeklyRewardSprites()
	{
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/Rewards/Rewards");
		if (texture2D == null)
		{
			return new Sprite[0];
		}
		int num = 8;
		float cellWidth = (float)texture2D.width / 4f;
		float cellHeight = (float)texture2D.height / 2f;
		Sprite[] array = new Sprite[num];
		for (int i = 0; i < num; i++)
		{
			int column = i % 4;
			int row = i / 4;
			Rect weeklyRewardSpriteRect = GetWeeklyRewardSpriteRect(texture2D.height, cellWidth, cellHeight, column, row, i);
			array[i] = Sprite.Create(texture2D, weeklyRewardSpriteRect, new Vector2(0.5f, 0.5f), 100f);
		}
		return array;
	}

	private static Rect GetWeeklyRewardSpriteRect(float textureHeight, float cellWidth, float cellHeight, int column, int row, int spriteIndex)
	{
		Vector4 vector = spriteIndex switch
		{
			0 => new Vector4(76f, 218f, 308f, 250f), 
			1 => new Vector4(30f, 180f, 348f, 292f), 
			2 => new Vector4(0f, 150f, 384f, 326f), 
			3 => new Vector4(0f, 150f, 352f, 342f), 
			4 => new Vector4(42f, 44f, 342f, 276f), 
			5 => new Vector4(14f, 0f, 370f, 334f), 
			6 => new Vector4(0f, 0f, 384f, 338f), 
			7 => new Vector4(0f, 0f, 358f, 342f), 
			_ => new Vector4(cellWidth * 0.05f, cellHeight * 0.1f, cellWidth * 0.9f, cellHeight * 0.7f), 
		};
		float x = (float)column * cellWidth + vector.x;
		float y = textureHeight - (float)(row + 1) * cellHeight + cellHeight - vector.y - vector.w;
		return new Rect(x, y, vector.z, vector.w);
	}

	private static Button CreateShopButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size, Color color, float fontSize)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		component2.color = color;
		component2.raycastTarget = true;
		Button component3 = obj.GetComponent<Button>();
		component3.targetGraphic = component2;
		BattlePopupStyle.ApplyButton(component3);
		CreateShopText(obj.transform, "Label", label, Vector2.zero, size - new Vector2(22f, 10f), fontSize, TextAlignmentOptions.Center, Color.white);
		BattlePopupStyle.ApplyButtonLabel(component3, fontSize);
		if (!string.IsNullOrWhiteSpace(objectName) && objectName.Contains("Close"))
		{
			BattlePopupStyle.ApplyCloseIconButton(component3);
		}
		return component3;
	}

	private static void SetShopTabColor(Button button, bool active)
	{
		if (!(button == null))
		{
			RectTransform rect = button.transform as RectTransform;
			if (rect != null)
			{
				float y = rect.anchoredPosition.y;
				rect.pivot = new Vector2(0f, 0.5f);
				rect.anchoredPosition = new Vector2(-1085f, y);
				rect.sizeDelta = active ? new Vector2(382f, 104f) : new Vector2(350f, 94f);
			}
			Image component = button.GetComponent<Image>();
			if (component != null)
			{
				component.color = active ? new Color(1f, 0.92f, 0.72f, 1f) : Color.white;
			}
			TMP_Text label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (label != null)
			{
				label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
				label.fontSize = active ? 42f : 38f;
				label.fontSizeMax = active ? 42f : 38f;
				label.color = active ? new Color(1f, 0.88f, 0.52f, 1f) : Color.white;
			}
		}
	}

	private static void SetBattleTilePackButtonLabel(Button button, string label)
	{
		if (button == null)
		{
			return;
		}
		TMP_Text buttonLabel = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (buttonLabel == null)
		{
			return;
		}
		buttonLabel.text = label;
		buttonLabel.gameObject.SetActive(true);
		buttonLabel.transform.SetAsLastSibling();
		buttonLabel.enableAutoSizing = false;
		buttonLabel.fontSize = 36f;
		buttonLabel.fontStyle = FontStyles.Bold;
		buttonLabel.alignment = TextAlignmentOptions.Center;
		buttonLabel.overflowMode = TextOverflowModes.Overflow;
		buttonLabel.color = new Color(1f, 0.9f, 0.58f, 1f);
		buttonLabel.raycastTarget = false;
		RectTransform labelRect = buttonLabel.rectTransform;
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.pivot = new Vector2(0.5f, 0.5f);
		labelRect.anchoredPosition = Vector2.zero;
		labelRect.sizeDelta = new Vector2(-22f, -10f);
	}

	private void RefreshWeeklyRewardUi()
	{
		PlayerProfile weeklyRewardProfile = GetWeeklyRewardProfile();
		if (weeklyRewardProfile == null)
		{
			SetWeeklyRewardButtonLabel(BattleLobbyText("Награды", weeklyRewardButtonText, "Ödüller", "Belohnungen"));
			if (weeklyRewardStatusText != null)
			{
				weeklyRewardStatusText.text = GameLocalization.Text("battle.shop.profile_loading");
			}
			return;
		}
		WeeklyRewardService.EnsureInitialized(weeklyRewardProfile);
		bool flag = WeeklyRewardService.IsTimeBlocked(weeklyRewardProfile);
		bool flag2 = WeeklyRewardService.CanClaimToday(weeklyRewardProfile);
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("weekly_rewarded");
		int currentDayNumber = WeeklyRewardService.GetCurrentDayNumber(weeklyRewardProfile);
		int freeAltin = WeeklyRewardService.GetFreeAltin(weeklyRewardProfile);
		int adAltin = WeeklyRewardService.GetAdAltin(weeklyRewardProfile);
		int adAmetist = WeeklyRewardService.GetAdAmetist(weeklyRewardProfile);
		SetWeeklyRewardButtonLabel(BattleLobbyText("Награды", weeklyRewardButtonText, "Ödüller", "Belohnungen"));
		if (weeklyRewardTodayText != null)
		{
			weeklyRewardTodayText.text = string.Format("{0} {1}", BattleLobbyText("День", "Day", "Gun", "Tag"), currentDayNumber);
		}
		if (weeklyRewardFreeButtonText != null)
		{
			weeklyRewardFreeButtonText.text = string.Format("{0} {1} Altın", BattleLobbyText("Забрать", "Claim", "Al", "Abholen"), freeAltin);
		}
		if (weeklyRewardAdButtonText != null)
		{
			weeklyRewardAdButtonText.text = string.Format("{0}: {1} Altın + {2} Ametist\n{3}", BattleLobbyText("Реклама", "Watch Ad", "Reklam", "Werbung ansehen"), adAltin, adAmetist, ResolveBattleLobbyStatusMessage(rewardedAdAvailability.Message));
		}
		if (weeklyRewardFreePreviewIcon != null)
		{
			weeklyRewardFreePreviewIcon.sprite = GetWeeklyRewardSprite(GetWeeklyRewardSpriteIndexForDay(currentDayNumber - 1));
		}
		if (weeklyRewardAdPreviewIcon != null)
		{
			weeklyRewardAdPreviewIcon.sprite = GetWeeklyRewardSprite(GetWeeklyRewardSpriteIndexForDay(Mathf.Min(6, currentDayNumber + 1)));
		}
		if (weeklyRewardFreeButton != null)
		{
			weeklyRewardFreeButton.interactable = !flag && flag2;
		}
		if (weeklyRewardAdButton != null)
		{
			weeklyRewardAdButton.interactable = !flag && flag2 && rewardedAdAvailability.IsReady && !weeklyRewardAdRequestInProgress;
		}
		if (weeklyRewardStatusText != null)
		{
			if (flag)
			{
				weeklyRewardStatusText.text = BattleLobbyText("Ошибка времени.", "Time error detected.", "Zaman hataşı.", "Zeitfehler erkannt.");
			}
			else if (flag2 && !rewardedAdAvailability.IsReady)
			{
				weeklyRewardStatusText.text = ResolveBattleLobbyStatusMessage(rewardedAdAvailability.Message);
			}
			else if (flag2)
			{
				weeklyRewardStatusText.text = BattleLobbyText("Выберите награду: бесплатно или больше за рекламу.", "Choose one reward: free claim or boosted reward for ad.", "Bir ödül seç: ücretsiz veya reklamla daha fazla.", "Waehle eine Belohnung: gratis oder mehr mit Werbung.");
			}
			else
			{
				weeklyRewardStatusText.text = BattleLobbyText("Сегодняшняя награда получена.", "Today reward claimed. Come back tomorrow.", "Bugünku ödül alindi.", "Heutige Belohnung abgeholt. Komm morgen wieder.");
			}
		}
		RefreshWeeklyRewardSlots(weeklyRewardProfile);
	}

	private void RefreshWeeklyRewardSlots(PlayerProfile profile)
	{
		if (profile == null || weeklyRewardSlotImages == null)
		{
			return;
		}
		for (int i = 0; i < weeklyRewardSlotImages.Length; i++)
		{
			bool flag = WeeklyRewardService.IsDayClaimed(profile, i);
			bool flag2 = WeeklyRewardService.IsDayCurrent(profile, i);
			bool flag3 = WeeklyRewardService.IsDayLocked(profile, i);
			WeeklyRewardClaimType dayClaimType = WeeklyRewardService.GetDayClaimType(profile, i);
			Image image = weeklyRewardSlotImages[i];
			if (image != null)
			{
				image.color = (flag ? new Color(0.68f, 0.95f, 0.68f, 1f) : (flag3 ? new Color(0.33f, 0.33f, 0.37f, 1f) : (flag2 ? new Color(1f, 0.88f, 0.46f, 1f) : Color.white)));
			}
			if (weeklyRewardIconImages != null && i < weeklyRewardIconImages.Length && weeklyRewardIconImages[i] != null)
			{
				Image obj = weeklyRewardIconImages[i];
				obj.sprite = GetWeeklyRewardSprite(GetWeeklyRewardSpriteIndexForDay(i));
				obj.color = (flag3 ? new Color(0.42f, 0.42f, 0.46f, 0.72f) : Color.white);
			}
			if (weeklyRewardSlotDayTexts != null && i < weeklyRewardSlotDayTexts.Length && weeklyRewardSlotDayTexts[i] != null)
			{
				weeklyRewardSlotDayTexts[i].text = string.Format("{0} {1}", BattleLobbyText("День", "Day", "Gun", "Tag"), i + 1);
			}
			if (weeklyRewardSlotAmountTexts != null && i < weeklyRewardSlotAmountTexts.Length && weeklyRewardSlotAmountTexts[i] != null)
			{
				weeklyRewardSlotAmountTexts[i].text = $"{WeeklyRewardService.GetFreeAltinForDay(i)}";
			}
			if (weeklyRewardSlotStateTexts != null && i < weeklyRewardSlotStateTexts.Length && weeklyRewardSlotStateTexts[i] != null)
			{
				weeklyRewardSlotStateTexts[i].text = ((!flag) ? (flag3 ? BattleLobbyText("Закрыто", "Locked", "Kilitli", "Gesperrt") : ((flag2 && WeeklyRewardService.CanClaimToday(profile)) ? BattleLobbyText("Готово", "Ready", "Hazır", "Bereit") : BattleLobbyText("Далее", "Next", "Sonraki", "Weiter"))) : ((dayClaimType == WeeklyRewardClaimType.Ad) ? BattleLobbyText("Реклама", "Claimed Ad", "Reklam", "Werbung") : BattleLobbyText("Получено", "Claimed", "Alindi", "Abgeholt")));
			}
		}
	}

	private void OnClickClaimWeeklyFree()
	{
		PlayerProfile weeklyRewardProfile = GetWeeklyRewardProfile();
		if (weeklyRewardProfile != null)
		{
			if (WeeklyRewardService.ClaimFree(weeklyRewardProfile))
			{
				SaveWeeklyRewardClaim();
			}
			RefreshWeeklyRewardUi();
		}
	}

	private void OnClickClaimWeeklyAd()
	{
		PlayerProfile profile = GetWeeklyRewardProfile();
		if (profile == null || weeklyRewardAdRequestInProgress)
		{
			return;
		}
		WeeklyRewardService.EnsureInitialized(profile);
		if (WeeklyRewardService.IsTimeBlocked(profile) || !WeeklyRewardService.CanClaimToday(profile))
		{
			RefreshWeeklyRewardUi();
			return;
		}
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("weekly_rewarded");
		if (!rewardedAdAvailability.IsReady)
		{
			RefreshWeeklyRewardUi();
			if (weeklyRewardStatusText != null)
			{
				weeklyRewardStatusText.text = ResolveBattleLobbyStatusMessage(rewardedAdAvailability.Message);
			}
			return;
		}
		weeklyRewardAdRequestInProgress = true;
		RefreshWeeklyRewardUi();
		if (weeklyRewardStatusText != null)
		{
			weeklyRewardStatusText.text = GameLocalization.Text("shop.ad_loading");
		}
		MonetizationService.Ensure().ShowRewardedAd("weekly_rewarded", delegate(RewardedAdResult result)
		{
			weeklyRewardAdRequestInProgress = false;
			if (!result.IsCompleted)
			{
				RefreshWeeklyRewardUi();
				if (weeklyRewardStatusText != null)
				{
					weeklyRewardStatusText.text = ResolveBattleLobbyStatusMessage(string.IsNullOrWhiteSpace(result.Message) ? "shop.ad_not_ready" : result.Message);
				}
				if (string.Equals(result.Message, "shop.ad_not_ready", StringComparison.Ordinal))
				{
					ScheduleWeeklyRewardAdButtonRefresh();
				}
			}
			else
			{
				if (WeeklyRewardService.ClaimAd(profile))
				{
					SaveWeeklyRewardClaim();
				}
				RefreshWeeklyRewardUi();
			}
		});
	}

	private void ScheduleWeeklyRewardAdButtonRefresh()
	{
		if (weeklyRewardAdRefreshRoutine != null)
		{
			StopCoroutine(weeklyRewardAdRefreshRoutine);
		}
		weeklyRewardAdRefreshRoutine = StartCoroutine(RefreshWeeklyRewardAdButtonAfterDelay());
	}

	private IEnumerator RefreshWeeklyRewardAdButtonAfterDelay()
	{
		yield return new WaitForSecondsRealtime(2f);
		if (weeklyRewardRoot != null && weeklyRewardRoot.activeInHierarchy)
		{
			RefreshWeeklyRewardUi();
		}
		weeklyRewardAdRefreshRoutine = null;
	}

	private static string ResolveBattleLobbyStatusMessage(string messageOrKey)
	{
		if (string.IsNullOrWhiteSpace(messageOrKey))
		{
			return string.Empty;
		}
		string text = GameLocalization.Text(messageOrKey);
		if (!(text == messageOrKey))
		{
			return text;
		}
		return messageOrKey;
	}

	private static PlayerProfile GetWeeklyRewardProfile()
	{
		if (ProfileService.I == null)
		{
			ProfileRuntimeBootstrap.EnsureServices();
		}
		if (ProfileService.I == null)
		{
			return null;
		}
		PlayerProfile current = ProfileService.I.Current;
		if (current == null)
		{
			ProfileRuntimeBootstrap.TryLoadCachedProfile();
			current = ProfileService.I.Current;
		}
		current?.EnsureData();
		return current;
	}

	private void SaveWeeklyRewardClaim()
	{
		if (ProfileService.I != null)
		{
			ProfileService.I.Save();
			ProfileService.I.NotifyProfileChanged();
		}
		RefreshBattleShopUi();
		RefreshBattleLobbyTopBarValues();
	}

	private void SetWeeklyRewardButtonLabel(string label)
	{
		TMP_Text tMP_Text = ((weeklyRewardButton != null) ? weeklyRewardButton.GetComponentInChildren<TMP_Text>(includeInactive: true) : null);
		if (tMP_Text != null)
		{
			tMP_Text.text = label;
		}
	}

	private void ApplyBattleProgressFallback()
	{
		if (battleLevelText != null)
		{
			battleLevelText.text = GameLocalization.Format("battle.lobby.level", 1);
		}
		if (battleExpText != null)
		{
			battleExpText.text = GameLocalization.Format("battle.lobby.exp", 0, 100, 100);
		}
		if (battleStatsText != null)
		{
			battleStatsText.text = GameLocalization.Format("battle.lobby.stats", 0, 0, 0);
		}
		RefreshEnergyUi();
	}

	private void StartEnergyRefreshRoutine()
	{
		if (energyRefreshRoutine != null)
		{
			StopCoroutine(energyRefreshRoutine);
		}
		energyRefreshRoutine = StartCoroutine(EnergyRefreshRoutine());
	}

	private IEnumerator EnergyRefreshRoutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		while (base.isActiveAndEnabled)
		{
			RefreshEnergyUi();
			UpdateDailyHeroBonusNotification();
			yield return wait;
		}
	}

	private bool TrySpendMatchEnergy()
	{
		if (EnergyService.TrySpendForMatch())
		{
			RefreshEnergyUi();
			return true;
		}
		RefreshEnergyUi();
		Log($"Not enough energy. Need {10}, have {EnergyService.CurrentEnergy}.");
		return false;
	}

	private bool HasMatchEnergy()
	{
		if (EnergyService.CanStartMatch())
		{
			return true;
		}
		RefreshEnergyUi();
		Log($"Not enough energy. Need {10}, have {EnergyService.CurrentEnergy}.");
		return false;
	}

	private void RefreshEnergyUi()
	{
		int currentEnergy = EnergyService.CurrentEnergy;
		int currentMaxEnergy = EnergyService.CurrentMaxEnergy;
		bool interactable = EnergyService.CanStartRewardedAdEnergy();
		bool flag = EnergyService.HasInfiniteEnergy();
		if (energyText != null)
		{
			energyText.text = (flag ? (BattleLobbyText("Энергия", "Energy", "Enerji", "Energie") + " ∞") : GameLocalization.Format("battle.lobby.energy", currentEnergy, currentMaxEnergy));
		}
		if (energyHintText != null)
		{
			energyHintText.text = (flag ? GameLocalization.Text("battle.energy.full_admin") : ((currentEnergy >= currentMaxEnergy) ? GameLocalization.Format("battle.lobby.energy_ready", 10) : GameLocalization.Format("battle.lobby.energy_refill", 10, EnergyService.FormatTimeUntilNextEnergy())));
		}
		SetMatchButtonInteractable(randomMatchButton, interactable: true);
		SetMatchButtonInteractable(rankedBattleButton, interactable: true);
		SetMatchButtonInteractable(duelChallengeButton, interactable: true);
		SetMatchButtonInteractable(localWifiBattleButton, interactable: true);
		SetMatchButtonInteractable(tournamentButton, !RouteLocalWifiSlotToTournament);
		if (energyAdButton != null)
		{
			energyAdButton.interactable = interactable;
			TMP_Text componentInChildren = energyAdButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.text = GameLocalization.Format("battle.lobby.energy_ad", 20);
			}
		}
		if (topBarTooltipVisible)
		{
			SetTopBarTooltipVisible(visible: false, null, Vector2.zero);
		}
		RefreshBattleShopUi();
	}

	private void OnClickRewardedEnergyAd()
	{
		RewardedAdAvailability rewardedAdAvailability = MonetizationService.Ensure().GetRewardedAdAvailability("battle_energy_rewarded");
		if (!rewardedAdAvailability.IsReady)
		{
			RefreshEnergyUi();
			if (battleShopRoot != null && battleShopRoot.activeInHierarchy && battleShopStatusText != null)
			{
				battleShopStatusText.text = ResolveBattleShopPurchaseMessage(rewardedAdAvailability.Message);
			}
			return;
		}
		if (battleShopRoot != null && battleShopRoot.activeInHierarchy && battleShopStatusText != null)
		{
			battleShopStatusText.text = GameLocalization.Text("shop.ad_loading");
		}
		EnergyService.TryClaimRewardedAdEnergy(delegate(bool success, string message)
		{
			RefreshEnergyUi();
			if (battleShopRoot != null && battleShopRoot.activeInHierarchy && battleShopStatusText != null)
			{
				battleShopStatusText.text = (success ? string.Format("+{0} {1}", 20, BattleLobbyText("энергии", "Energy", "Enerji", "Energie")) : ResolveBattleShopPurchaseMessage(message));
			}
			Log(success ? $"Rewarded energy ad claimed: +{20}." : (string.IsNullOrWhiteSpace(message) ? "Rewarded energy ad is unavailable." : message));
		});
	}

	private static void SetMatchButtonInteractable(Button button, bool interactable)
	{
		if (!(button == null))
		{
			button.interactable = interactable;
		}
	}

	private void BindCharacterSelectionButton()
	{
		if (autoBindOpenCharacterButton)
		{
			AutoResolveCharacterSelectionLinks();
			if (!(openCharacterCarouselButton == null))
			{
				openCharacterCarouselButton.onClick.RemoveListener(OnClickOpenCharacterCarousel);
				openCharacterCarouselButton.onClick.AddListener(OnClickOpenCharacterCarousel);
				openCharacterCarouselButton.interactable = true;
			}
		}
	}

	private void BindReturnButton()
	{
		if (!(returnToLobbyButton == null))
		{
			returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);
			returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
		}
	}

	private void BindBattleShopButton()
	{
		if (!(battleShopButton == null))
		{
			battleShopButton.onClick.RemoveListener(OnClickOpenBattleShop);
			battleShopButton.onClick.AddListener(OnClickOpenBattleShop);
			battleShopButton.interactable = true;
		}
	}

	private void BindBattleTileInventoryButton()
	{
		if (!(battleTileInventoryButton == null))
		{
			battleTileInventoryButton.onClick.RemoveListener(OnClickOpenBattleTileInventory);
			battleTileInventoryButton.onClick.AddListener(OnClickOpenBattleTileInventory);
			battleTileInventoryButton.interactable = true;
		}
	}

	private void BindWeeklyRewardButton()
	{
		if (!(weeklyRewardButton == null))
		{
			weeklyRewardButton.onClick.RemoveListener(OnClickOpenWeeklyRewards);
			weeklyRewardButton.onClick.AddListener(OnClickOpenWeeklyRewards);
			weeklyRewardButton.interactable = true;
		}
	}

	private void BindDailyHeroBonusButton()
	{
		if (!(dailyHeroBonusButton == null))
		{
			dailyHeroBonusButton.onClick.RemoveListener(OnClickOpenDailyHeroBonus);
			dailyHeroBonusButton.onClick.AddListener(OnClickOpenDailyHeroBonus);
			dailyHeroBonusButton.interactable = true;
			EnsureDailyHeroBonusNotificationBadge();
			UpdateDailyHeroBonusNotification();
		}
	}

	private void BindLocalWifiBattleButton()
	{
		if (!(localWifiBattleButton == null))
		{
			localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);
			localWifiBattleButton.onClick.RemoveListener(OnClickTournament);
			if (RouteLocalWifiSlotToTournament)
			{
				localWifiBattleButton.onClick.AddListener(OnClickTournament);
			}
			else
			{
				localWifiBattleButton.onClick.AddListener(OnClickLocalWifiMatch);
			}
			localWifiBattleButton.interactable = true;
		}
	}

	private void BindRandomMatchButton()
	{
		if (!(randomMatchButton == null))
		{
			randomMatchButton.onClick.RemoveListener(OnClickRandomMatch);
			randomMatchButton.onClick.AddListener(OnClickRandomMatch);
			randomMatchButton.interactable = true;
		}
	}

	private void BindRankedBattleButton()
	{
		if (!(rankedBattleButton == null))
		{
			rankedBattleButton.onClick.RemoveListener(OnClickRankedMatch);
			rankedBattleButton.onClick.AddListener(OnClickRankedMatch);
			rankedBattleButton.interactable = true;
		}
	}

	private void BindDuelChallengeButton()
	{
		if (!(duelChallengeButton == null))
		{
			duelChallengeButton.onClick.RemoveListener(OnClickDuelChallenge);
			duelChallengeButton.onClick.AddListener(OnClickDuelChallenge);
			duelChallengeButton.interactable = true;
		}
	}

	private void BindTournamentButton()
	{
		if (!(tournamentButton == null))
		{
			tournamentButton.onClick.RemoveListener(OnClickTournament);
			tournamentButton.onClick.AddListener(OnClickTournament);
			tournamentButton.interactable = true;
		}
	}

	private static Button FindButtonByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		Button[] array = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
		foreach (Button button in array)
		{
			if (button != null && string.Equals(button.gameObject.name, objectName, StringComparison.Ordinal))
			{
				return button;
			}
		}
		return null;
	}

	private void Log(string message)
	{
		if (debugLogs)
		{
			Debug.Log("[BattleLobbyUI] " + message, this);
		}
	}
}
}
