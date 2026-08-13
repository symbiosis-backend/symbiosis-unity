using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Monetization;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleMatchController : MonoBehaviour
    {
        private static readonly Rect PlayerHpBarSpriteRect = new Rect(332f, 32f, 240f, 960f);
        private static readonly Rect OpponentHpBarSpriteRect = new Rect(964f, 32f, 240f, 960f);
        private const string CountdownNumbersResourcePath = "Mahjong/Sprites/BattleCountdown/321Numbers";
        private const string CountdownStartResourcePath = "Mahjong/Sprites/BattleCountdown/StartText";
        private const string RoundBadgeResourcePath = "Mahjong/Sprites/BattleCountdown/Round,Raund,Tur";
        private const string BattleCountdownResourceRoot = "Mahjong/Sprites/BattleCountdown/";
        private const string ResultOzTileIconResourcePath = "Mahjong/Sprites/BattleTiles/OzTile";
        private const string BattleBoardPanelResourcePath = "Mahjong/Sprites/BattleLobbyUI/WindowBattle";
        private const string BattleProfileInfoPanelResourcePath = "Mahjong/Sprites/BattleLobbyUI/InfoPanel";
        private const string BattleProfileFrameResourcePath = "Mahjong/Sprites/BattleHUD/BattleProfileFrame_Final";
        private const string BattleProfilePortraitFrameResourcePath = "Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated";
        private const string LegacyBattleProfileFlagPanelResourcePath = "Mahjong/Sprites/FlagPanelBattleProfileCard";
        private const string PlayerBoardFullscreenPrefsKey = "Mahjong_Battle_PlayerBoardFullscreen";

        private static readonly Dictionary<int, Sprite> CountdownNumberSprites = new();
        private static readonly Dictionary<int, Sprite> RoundNumberSprites = new();
        private static readonly Dictionary<GameLanguage, Sprite> StartTextSprites = new();
        private static readonly Dictionary<GameLanguage, Sprite> RoundBadgeSprites = new();

        public event Action<BattleMatchController> MatchStarted;
        public event Action<BattleMatchController, int> RoundStarted;
        public event Action<BattleMatchController, int> RoundFinished;
        public event Action<BattleMatchController, bool> MatchFinished;
        public event Action<BattleMatchController, bool> PlayerBoardFullscreenChanged;
        public event Action<BattleMatchController> MatchStateChanged;

        [Header("Boards")]
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleBotController botController;
        [SerializeField] private BattleCombatSystem combatSystem;

        [Header("Round UI")]
        [SerializeField] private bool showRoundHud = false;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text stateText;

        [Header("Player Profile UI")]
        [SerializeField] private Image playerBattleSpriteImage;
        [SerializeField] private BattleCharacterModelView playerBattleModelView;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerTitleText;
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private Image playerRankIconImage;
        [SerializeField] private Image playerCardPortraitImage;
        [SerializeField] private Image playerTotemImage;
        [SerializeField] private TMP_Text playerTotemText;
        [SerializeField] private string playerRankFormat = "{0} {1} RP";
        [SerializeField] private string fallbackPlayerName = "Player";
        [SerializeField] private string fallbackPlayerRankTier = "Unranked";
        [SerializeField] private bool createPlayerProfileUiIfMissing = true;
        [SerializeField] private Vector2 playerProfileUiSize = new Vector2(420f, 172f);
        [SerializeField] private Vector2 playerProfileUiOffset = new Vector2(-120f, -50f);
        [SerializeField] private Vector2 playerBattleSpriteSize = new Vector2(156f, 156f);
        [SerializeField] private Vector2 playerBattleSpriteOffset = new Vector2(-12f, -8f);
        [SerializeField] private Image playerHpBarFill;
        [SerializeField] private TMP_Text playerHpBarText;
        [SerializeField] private Vector2 playerHpBarOffset = new Vector2(16f, -122f);
        [SerializeField] private Vector2 playerHpBarSize = new Vector2(230f, 18f);
        [SerializeField] private Color playerHpBarBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color playerHpBarFillColor = new Color(0.3f, 0.9f, 0.35f, 1f);
        [SerializeField] private Image opponentHpBarFill;
        [SerializeField] private TMP_Text opponentHpBarText;
        [SerializeField] private Color opponentHpBarFillColor = new Color(1f, 0.32f, 0.24f, 1f);
        [SerializeField, Min(0f)] private float boardHpBarInnerInset = 18f;
        [SerializeField, Min(6f)] private float verticalBoardHpBarWidth = 58f;
        [SerializeField, Min(20f)] private float horizontalBoardHpBarHeight = 72f;
        [SerializeField, Range(0.2f, 1f)] private float verticalBoardHpBarHeightFactor = 0.94f;
        [SerializeField] private bool hideLegacyPlayerHpText = true;

        [Header("Opponent Profile UI")]
        [SerializeField] private Image opponentBattleSpriteImage;
        [SerializeField] private BattleCharacterModelView opponentBattleModelView;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text opponentRankText;
        [SerializeField] private TMP_Text opponentStatsText;
        [SerializeField] private Image opponentRankIconImage;
        [SerializeField] private Image opponentCardPortraitImage;
        [SerializeField] private Image opponentTotemImage;
        [SerializeField] private TMP_Text opponentTotemText;
        [SerializeField] private bool createOpponentProfileUiIfMissing = true;
        [SerializeField] private Vector2 opponentProfileUiSize = new Vector2(420f, 172f);
        [SerializeField] private Vector2 opponentProfileUiOffset = new Vector2(120f, -50f);
        [SerializeField] private Vector2 opponentBattleSpriteSize = new Vector2(156f, 156f);
        [SerializeField] private Vector2 opponentBattleSpriteOffset = new Vector2(12f, -8f);
        [SerializeField] private bool flipOpponentBattleSpriteX = true;

        [Header("Battle Profile Panel")]
        [SerializeField] private Sprite battleProfilePanelSprite;
        [SerializeField] private string battleProfilePanelSpriteResourcePath = BattleProfileInfoPanelResourcePath;
        [SerializeField] private Color battleProfilePanelColor = Color.white;
        [SerializeField] private Image.Type battleProfilePanelImageType = Image.Type.Simple;
        [SerializeField] private bool battleProfilePanelRaycastTarget = false;

        [Header("Battle Board Panel")]
        [SerializeField] private Sprite battleBoardPanelSprite;
        [SerializeField] private string battleBoardPanelSpriteResourcePath = BattleBoardPanelResourcePath;
        [SerializeField] private Color battleBoardPanelColor = Color.white;
        [SerializeField] private Image.Type battleBoardPanelImageType = Image.Type.Simple;
        [SerializeField] private bool battleBoardPanelRaycastTarget = true;

        [Header("Player Board Fullscreen")]
        [SerializeField] private Button playerBoardFullscreenButton;
        [SerializeField] private TMP_Text playerBoardFullscreenButtonText;
        [SerializeField] private bool createPlayerBoardFullscreenButtonIfMissing = true;
        [SerializeField] private Vector2 playerBoardFullscreenButtonSize = new Vector2(128f, 52f);
        [SerializeField] private Vector2 playerBoardFullscreenButtonOffset = new Vector2(-22f, -10f);
        [SerializeField, Min(0f)] private float playerBoardFullscreenSideInset = 18f;
        [SerializeField, Min(0f)] private float playerBoardFullscreenTopInset = 76f;
        [SerializeField, Min(0f)] private float playerBoardFullscreenBottomInset = 18f;
        [SerializeField, Min(1f)] private float playerBoardFullscreenMaxTileScale = 3.85f;
        [SerializeField, Min(0f)] private float playerBoardFullscreenHpBarGap = 12f;
        [SerializeField] private Image battleMenuGearImage;
        [SerializeField] private bool animateBattleMenuGear = false;
        [SerializeField, Min(0.05f)] private float battleMenuGearHalfTurnDuration = 2.8f;

        [Header("Character Action Feedback")]
        [SerializeField, Min(0.01f)] private float characterActionPulseDuration = 0.18f;
        [SerializeField, Min(0f)] private float characterAttackPulseDistance = 18f;
        [SerializeField, Min(0f)] private float characterHitPulseDistance = 12f;

        [Header("Matched Pair Attack FX")]
        [SerializeField] private bool useMatchedPairAttackFx = true;
        [SerializeField, Min(0.01f)] private float matchedPairCrashDuration = 0.12f;
        [SerializeField, Min(0f)] private float matchedPairCrashSpacing = 16f;
        [SerializeField, Min(0.01f)] private float matchedPairFlightDuration = 0.28f;
        [SerializeField, Min(0f)] private float matchedPairFlightArcHeight = 46f;
        [SerializeField, Min(0.1f)] private float matchedPairImpactScale = 1.12f;
        [SerializeField, Range(0f, 1f)] private float matchedPairTargetScale = 0.36f;
        [SerializeField, Range(0f, 1f)] private float matchedPairTargetAlpha = 0.15f;

        [Header("Damage Text FX")]
        [SerializeField] private bool useFloatingDamageText = true;
        [SerializeField, Min(8f)] private float damageTextFontSize = 34f;
        [SerializeField, Min(0.05f)] private float damageTextDuration = 1.05f;
        [SerializeField, Min(0f)] private float damageTextRiseDistance = 72f;
        [SerializeField, Min(0f)] private float damageTextSideOffset = 34f;
        [SerializeField] private Color damageTextColor = new Color(1f, 0.26f, 0.16f, 1f);
        [SerializeField] private Color damageTextCriticalColor = new Color(1f, 0.82f, 0.18f, 1f);
        [SerializeField] private Color damageTextArmorColor = new Color(0.45f, 0.84f, 1f, 1f);
        [SerializeField] private Color damageTextOutlineColor = new Color(0.02f, 0f, 0f, 1f);
        [SerializeField, Min(1f)] private float tutorialDamageTextFontSize = 68f;
        [SerializeField, Min(0.1f)] private float tutorialDamageTextDuration = 3.2f;
        [SerializeField, Min(0f)] private float tutorialDamageTextRiseDistance = 44f;
        [SerializeField, Min(1f)] private float tutorialDamageTextIconSize = 58f;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanelRoot;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultRewardText;
        [SerializeField] private TMP_Text resultExperienceText;
        [SerializeField] private Image resultRewardIcon;
        [SerializeField] private Button resultBattleLobbyButton;
        [SerializeField] private Button resultNewMatchButton;
        [SerializeField] private bool createResultPanelIfMissing = true;
        [SerializeField] private string winResultText = "VICTORY";
        [SerializeField] private string failedResultText = "DEFEAT";
        [SerializeField] private string resultGoldFormat = "+{0} Gold";
        [SerializeField] private string resultNoGoldText = "+0 Gold";
        [SerializeField] private string resultExperienceFormat = "+{0} XP  Level {1}";
        [SerializeField] private string returnToBattleLobbyText = "Menu";
        [SerializeField] private string newMatchText = "New Match";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string battleGameSceneName = "GameMahjongBattle";
        [SerializeField, Min(0)] private int battleWinExperienceReward = 100;
        [SerializeField, Min(0)] private int battleLoseExperienceReward = 35;
        [SerializeField] private Vector2 resultPanelSize = new Vector2(1180f, 680f);
        [SerializeField] private Color resultPanelBackgroundColor = Color.clear;
        [SerializeField] private Color winResultColor = new Color(0.3f, 1f, 0.38f, 1f);
        [SerializeField] private Color failedResultColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Sprite winResultWindowSprite;
        [SerializeField] private Sprite defeatResultWindowSprite;
        [SerializeField] private string winResultWindowResourcePath = "Mahjong/Sprites/BattleResult/WindowWin";
        [SerializeField] private string defeatResultWindowResourcePath = "Mahjong/Sprites/BattleResult/WindowDefeat";
        [SerializeField] private AudioClip resultWinClip;
        [SerializeField] private AudioClip resultLoseClip;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private string resultWinClipResourcePath = "Mahjong/Sounds/game-won";
        [SerializeField] private string resultLoseClipResourcePath = "Mahjong/Sounds/beep-failure";
        [SerializeField] private string impactClipResourcePath = "Mahjong/Sounds/impact-sound";
        [SerializeField, Range(0f, 1f)] private float resultAudioVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float impactAudioVolume = 0.9f;
        [SerializeField] private Texture2D hpBarsTexture;
        [SerializeField] private string hpBarsTextureResourcePath = "Mahjong/Sprites/BattleResult/HPBars";
        [SerializeField] private Image resultPlayerCharacterImage;
        [SerializeField] private Image resultOpponentCharacterImage;
        [SerializeField] private BattleCharacterModelView resultPlayerCharacterModelView;
        [SerializeField] private BattleCharacterModelView resultOpponentCharacterModelView;
        [SerializeField] private Vector2 resultCharacterSize = new Vector2(260f, 220f);
        [SerializeField] private Vector2 resultPlayerCharacterOffset = new Vector2(0f, 260f);

        [Header("Flow")]
        [SerializeField, Min(1)] private int totalCombatRounds = 1;
        [SerializeField] private float nextRoundDelay = 1.25f;
        [SerializeField] private string roundPrefix = "Round ";
        [SerializeField] private string stateFight = "Fight";
        [SerializeField] private string statePlayerBoardCleared = "Player board cleared";
        [SerializeField] private string stateOpponentBoardCleared = "Opponent board cleared";
        [SerializeField] private string statePlayerBoardFailed = "Player board failed";
        [SerializeField] private string stateOpponentBoardFailed = "Opponent board failed";
        [SerializeField] private string stateRoundWin = "Round won";
        [SerializeField] private string stateRoundLose = "Round lost";
        [SerializeField] private string stateMatchWin = "You won the match";
        [SerializeField] private string stateMatchLose = "You lost the match";

        [Header("Countdown")]
        [SerializeField] private bool useStartCountdown = true;
        [SerializeField, Min(1)] private int countdownSeconds = 3;
        [SerializeField, Min(0.05f)] private float countdownInterval = 1f;
        [SerializeField, Min(0.05f)] private float startTextDuration = 0.45f;
        [SerializeField] private string countdownStartText = "Start";
        [SerializeField] private GameObject countdownOverlayRoot;
        [SerializeField] private TMP_Text countdownOverlayText;
        [SerializeField] private Image countdownOverlaySpriteImage;
        [SerializeField] private Image countdownOverlayBackdrop;
        [SerializeField] private Image countdownOverlayTopLine;
        [SerializeField] private Image countdownOverlayBottomLine;
        [SerializeField] private bool useCountdownImageSprites = true;
        [SerializeField] private bool createCountdownOverlayIfMissing = true;
        [SerializeField] private Color countdownOverlayBackdropColor = new Color(0.015f, 0.012f, 0.009f, 0.54f);
        [SerializeField] private Color countdownOverlayFrameColor = new Color(0.95f, 0.68f, 0.24f, 0.22f);
        [SerializeField] private Color countdownOverlayTextColor = new Color(1f, 0.86f, 0.30f, 1f);
        [SerializeField, Min(24f)] private float countdownOverlayNumberFontSize = 330f;
        [SerializeField, Min(24f)] private float countdownOverlayStartFontSize = 210f;
        [SerializeField, Min(0.1f)] private float countdownOverlayStartScale = 1.75f;
        [SerializeField, Min(0.1f)] private float countdownOverlaySettleScale = 0.92f;
        [SerializeField, Min(0.1f)] private float countdownOverlayEndScale = 0.58f;
        [SerializeField, Range(0f, 1f)] private float countdownOverlayHoldPortion = 0.24f;

        [Header("Round Badge Sprites")]
        [SerializeField] private bool useRoundBadgeImageSprites = true;
        [SerializeField] private Image roundBadgeImage;
        [SerializeField] private Image roundBadgeNumberImage;
        [SerializeField] private Vector2 roundBadgeSize = new Vector2(132f, 154f);
        [SerializeField] private Vector2 roundBadgeNumberSize = new Vector2(50f, 66f);
        [SerializeField] private Vector2 roundBadgeOffset = new Vector2(0f, -8f);
        [SerializeField] private Vector2 roundBadgeNumberOffset = new Vector2(0f, -96f);

        [Header("Layout")]
        [SerializeField] private BattleLayoutPresetService battleLayoutPresetService;
        [SerializeField] private bool loopLayouts = true;
        [SerializeField] private bool randomizeBattleLayouts = true;

        [Header("Tiles")]
        [SerializeField] private BattleTileStore battleStore;

        [Header("Fallback")]
        [SerializeField, Min(1)] private int fallbackTileRound = 1;
        [SerializeField, Min(1)] private int fallbackLayoutLevel = 1;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private int currentRoundIndex;
        private int playerLayoutIndex;
        private int opponentLayoutIndex;
        private int playerRoundWins;
        private int opponentRoundWins;

        private bool matchFinished;
        private bool matchRunning;
        private bool roundEnding;
        private bool tutorialDamageTextEmphasis;

        private bool playerBoardRebuilding;
        private bool opponentBoardRebuilding;
        private int lastResultGoldReward;
        private int lastResultExperienceReward;
        private int lastResultAccountLevel;
        private int lastResultBattleWins;
        private int lastResultBattleLosses;
        private bool lastResultWasRanked;
        private bool lastResultWasTutorial;
        private bool lastResultPlayerWon;
        private bool tutorialResultApplied;
        private int lastResultRankPointDelta;
        private int lastResultOzTileDelta;
        private AudioSource resultAudioSource;
        private Sprite playerHpBarSprite;
        private Sprite opponentHpBarSprite;
        private Sprite resultOzTileIconSprite;
        private Coroutine resultRewardFlyRoutine;
        private bool resultMatchEndAdPending;
        private bool resultMatchEndAdInProgress;
        private string pendingResultMatchEndAdSource;

        private Coroutine matchStartRoutine;
        private Coroutine playerBoardRoutine;
        private Coroutine opponentBoardRoutine;
        private Coroutine roundTransitionRoutine;
        private Coroutine playerCharacterActionRoutine;
        private Coroutine opponentCharacterActionRoutine;
        private readonly List<Image> activeMatchedPairFxImages = new();
        private readonly List<Image> activeResultRewardFxImages = new();
        private readonly List<TMP_Text> activeDamageTexts = new();
        private string opponentBattleCharacterId;
        private BattleBoardSide lastPairAttackAnimationSide;
        private float lastPairAttackAnimationRealtime = -100f;
        private BattleBoardsHeightFirstLayout boardsHeightFirstLayout;
        private Vector2 lastPlayerBoardAreaSize;
        private Vector2 lastOpponentBoardAreaSize;
        private Vector2 lastBattleHudCanvasSize;
        private bool playerBoardFullscreen;
        private CanvasGroup opponentBoardFullscreenGroup;
        private CanvasGroup playerProfileFullscreenGroup;
        private CanvasGroup opponentProfileFullscreenGroup;
        private bool hasSavedBoardSiblingIndices;
        private int savedPlayerBoardSiblingIndex;
        private int savedOpponentBoardSiblingIndex;
        private Coroutine restoreBoardsLayoutRoutine;
        private BattleFullscreenSpriteState playerFullscreenSpriteState;
        private BattleFullscreenSpriteState opponentFullscreenSpriteState;
        private Coroutine battleMenuGearSpinRoutine;
        private Sprite battleProfilePortraitFrameSprite;
        private int playerTotemUpgradeLevel;

        private struct BattleFullscreenSpriteState
        {
            public bool HasValue;
            public Transform Parent;
            public int SiblingIndex;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector3 LocalScale;
            public Quaternion LocalRotation;
        }

        public BattleBoard PlayerBoard => playerBoard;
        public BattleBoard OpponentBoard => opponentBoard;
        public BattleBotController BotController => botController;
        public BattleCombatSystem CombatSystem => combatSystem;
        public BattleTileStore BattleStore => battleStore;
        public BattleLayoutPresetService BattleLayoutPresetService => battleLayoutPresetService;

        public int CurrentRoundIndex => currentRoundIndex;
        public int CurrentRoundNumber => Mathf.Max(1, currentRoundIndex + 1);

        public int PlayerLayoutIndex => playerLayoutIndex;
        public int OpponentLayoutIndex => opponentLayoutIndex;

        public int PlayerLayoutNumber => Mathf.Max(1, playerLayoutIndex + 1);
        public int OpponentLayoutNumber => Mathf.Max(1, opponentLayoutIndex + 1);

        public bool IsMatchFinished => matchFinished;
        public bool IsMatchRunning => matchRunning;
        public bool IsPlayerBoardRebuilding => playerBoardRebuilding;
        public bool IsOpponentBoardRebuilding => opponentBoardRebuilding;
        public bool IsPlayerBoardFullscreen => playerBoardFullscreen;

        public void SetTutorialDamageTextEmphasis(bool enabled)
        {
            tutorialDamageTextEmphasis = enabled;
        }
        private Vector2 ResultPanelVisualSize => new Vector2(
            Mathf.Max(1180f, resultPanelSize.x),
            Mathf.Max(680f, resultPanelSize.y));

        private void Awake()
        {
            EnsureResultAudioSource();
            AutoResolveLinks();
            EnsureBattleBoardsLayout();
        }

        private void OnEnable()
        {
            AutoResolveLinks();
            EnsureBattleBoardsLayout();
            BindBoards();
            ProfileService.ProfileChanged += ApplyPlayerProfileUi;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            BindCharacterSelectionService();
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= ApplyPlayerProfileUi;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            UnbindCharacterSelectionService();
            UnbindBoards();
            UnbindCombatSystem();
            StopMatchStartRoutine();
            StopBoardRoutines();
            StopRoundTransitionRoutine();
            StopCharacterActionRoutines();
            StopRestoreBoardsLayoutRoutine();
            StopBattleMenuGearSpin();
        }

        private void Start()
        {
            HideLobbyRuntimeUi();
            EnsureBattleBoardsLayout();
            EnsurePlayerBoardFullscreenButton();
            EnsureBattleOpponentSession();
            EnsureOpponentBattleCharacter();
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            ApplyBattleProfileLayout();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshBattleHpBars();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
            ApplyBattleProfileLayout();
            RefreshHud();
            EnsureBattleMenuGearSpin();
            StartMatch();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshHud();
        }

        private void HideLobbyRuntimeUi()
        {
            SetObjectActiveByName("ButtonRandomMatch", false);
            SetObjectActiveByName("ButtonRankedBattle", false);
            SetObjectActiveByName("ButtonLocalWifiBattle", false);
            SetObjectActiveByName("ButtonBattleShop", false);
            SetObjectActiveByName("ButtonReturnToLobby", false);
            SetObjectActiveByName("BattleProgressPanel", false);
            SetObjectActiveByName("BattleShopOverlay", false);
            SetObjectActiveByName("BattleEnergyAdButton", false);
            SetObjectActiveByName("OpenCharacterCarouselButton", false);
            SetObjectActiveByName("LobbyCharacterImage", false);
            SetObjectActiveByName("CharasterCarousel", false);
            SetObjectActiveByName("RandomBattleLobbyOverlay", false);
            SetObjectActiveByName("OnlineRankedBattleLobbyOverlay", false);
            SetObjectActiveByName("LocalWifiBattleLobbyOverlay", false);
        }

        private static void SetObjectActiveByName(string objectName, bool active)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform item = transforms[i];
                if (item == null || !string.Equals(item.name, objectName, StringComparison.Ordinal))
                    continue;

                if (item.gameObject.activeSelf != active)
                    item.gameObject.SetActive(active);
            }
        }

        private void Update()
        {
            RefitBoardsWhenAreaSizeChanges();
            RefitBattleHudWhenCanvasSizeChanges();
            if (playerBoardFullscreen)
                ApplyPlayerBoardFullscreenLayout();
        }

        public void StartMatch()
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return;

            if (!BattleLoreTutorialSession.IsActive &&
                MahjongSession.LaunchMode == MahjongLaunchMode.Battle &&
                !MahjongSession.EnsureLocalBattleLoadout())
            {
                Debug.LogError("[BattleMatchController] Battle start blocked: the active 18-tile loadout could not be frozen.");
                return;
            }

            if (playerBoardFullscreen)
                SetPlayerBoardFullscreen(false, false);

            StopAllCoroutines();
            ClearMatchedPairFx();
            ClearFloatingDamageTexts();
            ClearResultRewardFlyFx();
            HideCountdownOverlay();
            matchStartRoutine = null;
            resultRewardFlyRoutine = null;
            playerBoardRoutine = null;
            opponentBoardRoutine = null;
            roundTransitionRoutine = null;
            playerCharacterActionRoutine = null;
            opponentCharacterActionRoutine = null;
            battleMenuGearSpinRoutine = null;
            opponentBattleCharacterId = string.Empty;

            currentRoundIndex = 0;
            playerLayoutIndex = 0;
            opponentLayoutIndex = 0;
            playerRoundWins = 0;
            opponentRoundWins = 0;

            matchFinished = false;
            matchRunning = false;
            roundEnding = false;

            playerBoardRebuilding = false;
            opponentBoardRebuilding = false;

            EnsureBattleOpponentSession();
            EnsureOpponentBattleCharacter();
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();
            EnsureBattleBoardsLayout();
            EnsurePlayerBoardFullscreenButton();
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            ApplyBattleProfileLayout();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshBattleHpBars();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
            ApplyBattleProfileLayout();
            RefreshHud();
            EnsureBattleMenuGearSpin();
            NotifyStateChanged();

            matchStartRoutine = StartCoroutine(StartMatchRoutine());
        }

        public void RestartMatch()
        {
            StartMatch();
        }

        public void ForceFinishMatch(bool playerWon)
        {
            ForceFinishMatch(playerWon, false);
        }

        public void ForceForfeitMatch()
        {
            ForceFinishMatch(false, true);
        }

        private void ForceFinishMatch(bool playerWon, bool playerForfeited)
        {
            if (matchFinished)
                return;

            if (playerBoardFullscreen)
                SetPlayerBoardFullscreen(false);

            StopBoardRoutines();
            StopMatchStartRoutine();
            StopRoundTransitionRoutine();
            StopCharacterActionRoutines();
            StopRestoreBoardsLayoutRoutine();

            matchFinished = true;
            matchRunning = false;
            playerBoardRebuilding = false;
            opponentBoardRebuilding = false;
            roundEnding = false;
            RefreshPlayerBoardFullscreenButton();

            if (stateText != null)
                stateText.text = playerWon ? stateMatchWin : stateMatchLose;

            if (botController != null)
                botController.StopBot();

            bool wasOnlineRankedBattle = IsOnlineRankedBattleActive();
            if (wasOnlineRankedBattle)
            {
                if (playerForfeited)
                    OnlineRankedBattleNetwork.I.SendForfeitMatch();
                else
                    OnlineRankedBattleNetwork.I.SendMatchFinished();
            }

            ApplyBattleMatchResult(playerWon, wasOnlineRankedBattle);
            ShowResultPanel(playerWon);
            MatchFinished?.Invoke(this, playerWon);
            NotifyStateChanged();

            Log($"Match finished | PlayerWon={playerWon} | Forfeit={playerForfeited}");
        }

        private IEnumerator StartMatchRoutine()
        {
            yield return PlayStartCountdownRoutine();

            matchStartRoutine = null;

            if (matchFinished)
                yield break;

            BeginMatchAfterCountdown();
        }

        private void BeginMatchAfterCountdown()
        {
            matchRunning = true;
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();

            if (combatSystem != null)
                combatSystem.StartCombat();

            RefreshHud();
            MatchStarted?.Invoke(this);
            NotifyStateChanged();

            Log("Match started");
            BuildInitialBoards();
        }

        public void SetBoards(BattleBoard player, BattleBoard opponent)
        {
            UnbindBoards();

            playerBoard = player;
            opponentBoard = opponent;
            ConfigureBoardInputOwnership();

            if (combatSystem != null)
                combatSystem.SetBoards(playerBoard, opponentBoard);

            BindBoards();
            NotifyStateChanged();
        }

        public void SetBotController(BattleBotController controller)
        {
            botController = controller;
            NotifyStateChanged();
        }

        public void SetCombatSystem(BattleCombatSystem combat)
        {
            UnbindCombatSystem();
            combatSystem = combat;

            if (combatSystem != null)
            {
                combatSystem.SetMatchController(this);
                combatSystem.SetBoards(playerBoard, opponentBoard);
                BindCombatSystem();
            }

            ConfigureBoardInputOwnership();
            NotifyStateChanged();
        }

        public void SetBattleStore(BattleTileStore store)
        {
            battleStore = store;
            NotifyStateChanged();
        }

        public void SetBattleLayoutPresetService(BattleLayoutPresetService service)
        {
            battleLayoutPresetService = service;
            NotifyStateChanged();
        }

        public void SetRoundUi(TMP_Text round, TMP_Text score, TMP_Text state)
        {
            roundText = round;
            scoreText = score;
            stateText = state;
            RefreshHud();
            NotifyStateChanged();
        }

        public string GetRoundText()
        {
            return $"{roundPrefix}{CurrentRoundNumber}/{TotalCombatRounds}";
        }

        public string GetScoreText()
        {
            if (combatSystem != null)
                return $"{combatSystem.PlayerHp} : {combatSystem.OpponentHp}";

            return "- : -";
        }

        public void BuildNextPlayerBoard()
        {
            if (!CanRebuildPlayerBoard())
                return;

            playerLayoutIndex = ResolveNextBattleLayoutIndex(playerLayoutIndex, 11);
            BuildBoardForSide(playerBoard, playerLayoutIndex, restartBotAfterBuild: false);

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            NotifyStateChanged();

            Log($"Player board rebuilt in current round | LayoutIndex={playerLayoutIndex}");
        }

        public void BuildNextOpponentBoard()
        {
            if (!CanRebuildOpponentBoard())
                return;

            opponentLayoutIndex = ResolveNextBattleLayoutIndex(opponentLayoutIndex, 29);
            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: !IsRealtimeOpponentBattleActive());

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            NotifyStateChanged();

            Log($"Opponent board rebuilt in current round | LayoutIndex={opponentLayoutIndex}");
        }

        private void BuildInitialBoards()
        {
            playerLayoutIndex = ResolveInitialBattleLayoutIndex();
            opponentLayoutIndex = playerLayoutIndex;

            BuildBoardForSide(playerBoard, playerLayoutIndex, restartBotAfterBuild: false);
            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: !IsRealtimeOpponentBattleActive());

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            RoundStarted?.Invoke(this, CurrentRoundNumber);
            NotifyStateChanged();

            Log($"Initial boards built | PlayerLayout={playerLayoutIndex} OpponentLayout={opponentLayoutIndex}");
        }

        private void ApplyPreferredFullscreenAfterBoardsBuilt()
        {
        }

        private void BuildBoardForSide(BattleBoard board, int layoutIndex, bool restartBotAfterBuild)
        {
            if (board == null)
            {
                Debug.LogError("[BattleMatchController] Target board is null.");
                return;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log($"BuildBoardForSide skipped: combat finished | Side={board.Side}");
                return;
            }

            int requestedRoundNumber = CurrentRoundNumber;
            int resolvedRoundNumber = requestedRoundNumber;
            int layoutLevel = ResolveLayoutLevel(layoutIndex);
            List<LayoutSlot> slots = ResolveBattleLayoutSlots(layoutLevel);

            if (slots == null || slots.Count == 0)
            {
                Debug.LogError($"[BattleMatchController] Battle layout is empty for level {layoutLevel}.");
                return;
            }

            int requiredBoardTiles = BattleTileInventoryService.RequiredActiveTiles * 2;
            if (!BattleLoreTutorialSession.IsActive && slots.Count != requiredBoardTiles)
            {
                Debug.LogError($"[BattleMatchController] Battle layout must contain exactly {requiredBoardTiles} slots for 18 pairs. Actual={slots.Count}.");
                return;
            }

            IReadOnlyList<BattleTileData> source = ResolveBestTileSource(board.Side, requestedRoundNumber, out resolvedRoundNumber);
            if (source == null || source.Count == 0)
            {
                Debug.LogError($"[BattleMatchController] Tile source is empty. RequestedRound={requestedRoundNumber}");
                return;
            }

            int seed = ResolveBattleBoardSeed(requestedRoundNumber);

            board.Clear();
            board.SetRoundData(resolvedRoundNumber, slots, seed, source);
            EnsureBattleBoardsLayout();
            board.Build();
            RefitBattleBoards();

            if (!board.IsBuilt)
            {
                Debug.LogError($"[BattleMatchController] Board build failed | Side={board.Side} | Round={resolvedRoundNumber}");
                return;
            }

            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();

            if (restartBotAfterBuild && botController != null && !IsRealtimeOpponentBattleActive())
                botController.RestartBot();

            Log(
                $"Board built | Side={board.Side} | RequestedRound={requestedRoundNumber} | " +
                $"ResolvedRound={resolvedRoundNumber} | Layout={layoutLevel} | Seed={seed}");
        }

        private bool CanRebuildPlayerBoard()
        {
            return matchRunning &&
                   !matchFinished &&
                   !roundEnding &&
                   playerBoard != null &&
                   !playerBoardRebuilding &&
                   (combatSystem == null || !combatSystem.IsCombatFinished);
        }

        private int ResolveBattleBoardSeed(int roundNumber)
        {
            int safeRound = Mathf.Max(1, roundNumber);
            if ((MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch ||
                 MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch) &&
                MahjongSession.BattleMatchSeed > 0)
            {
                return MahjongSession.BattleMatchSeed + safeRound * 997;
            }

            return UnityEngine.Random.Range(100000, 999999);
        }

        private bool CanRebuildOpponentBoard()
        {
            return matchRunning &&
                   !matchFinished &&
                   !roundEnding &&
                   opponentBoard != null &&
                   !opponentBoardRebuilding &&
                   (combatSystem == null || !combatSystem.IsCombatFinished);
        }

        private void BindBoards()
        {
            if (playerBoard != null)
            {
                playerBoard.Cleared -= HandleBoardCleared;
                playerBoard.Failed -= HandleBoardFailed;
                playerBoard.Cleared += HandleBoardCleared;
                playerBoard.Failed += HandleBoardFailed;
            }

            if (opponentBoard != null)
            {
                opponentBoard.Cleared -= HandleBoardCleared;
                opponentBoard.Failed -= HandleBoardFailed;
                opponentBoard.Cleared += HandleBoardCleared;
                opponentBoard.Failed += HandleBoardFailed;
            }
        }

        private void UnbindBoards()
        {
            if (playerBoard != null)
            {
                playerBoard.Cleared -= HandleBoardCleared;
                playerBoard.Failed -= HandleBoardFailed;
            }

            if (opponentBoard != null)
            {
                opponentBoard.Cleared -= HandleBoardCleared;
                opponentBoard.Failed -= HandleBoardFailed;
            }
        }

        private List<LayoutSlot> ResolveBattleLayoutSlots(int layoutLevel)
        {
            if (BattleLoreTutorialSession.IsActive)
                return BattleLoreTutorialSession.GetTutorialLayoutSlots(BattleLoreTutorialSession.ActiveStage);

            if (battleLayoutPresetService == null)
            {
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();
            }

            List<LayoutSlot> slots = null;

            if (battleLayoutPresetService != null)
                slots = battleLayoutPresetService.GetLevel(layoutLevel);

            if (slots == null || slots.Count == 0)
                slots = BattleLayoutPresets.GetByLevel(layoutLevel);

            return slots != null ? new List<LayoutSlot>(slots) : null;
        }

        private IReadOnlyList<BattleTileData> ResolveBestTileSource(BattleBoardSide side, int requestedRoundNumber, out int resolvedRoundNumber)
        {
            resolvedRoundNumber = Mathf.Max(1, requestedRoundNumber);

            if (battleStore == null)
                return null;

            IReadOnlyList<BattleTileData> source = GetTileSourceForSide(side, resolvedRoundNumber);
            if (HasTileSource(source))
                return source;

            for (int round = resolvedRoundNumber - 1; round >= 1; round--)
            {
                source = GetTileSourceForSide(side, round);
                if (HasTileSource(source))
                {
                    resolvedRoundNumber = round;
                    Log($"Fallback tile source used | RequestedRound={requestedRoundNumber} -> Round={round}");
                    return source;
                }
            }

            resolvedRoundNumber = Mathf.Max(1, fallbackTileRound);
            source = GetTileSourceForSide(side, resolvedRoundNumber);
            return HasTileSource(source) ? source : null;
        }

        private IReadOnlyList<BattleTileData> GetTileSourceForSide(BattleBoardSide side, int roundNumber)
        {
            if (battleStore == null)
                return null;

            if (BattleLoreTutorialSession.IsActive)
            {
                PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
                return battleStore.GetProfileTilesForRound(roundNumber, profile);
            }

            BattleLoadoutSnapshot loadout = MahjongSession.GetBattleLoadout(side);
            if (loadout != null && loadout.TryResolveActiveTiles(battleStore, out List<BattleTileData> tiles))
                return tiles;

            return null;
        }

        public IReadOnlyList<BattleTileData> GetAdaptiveOpponentTilesForRound(int roundNumber)
        {
            if (battleStore == null)
                return null;

            BattleLoadoutSnapshot loadout = MahjongSession.OpponentBattleLoadout;
            return loadout != null && loadout.TryResolveActiveTiles(battleStore, out List<BattleTileData> result)
                ? result
                : null;
        }

        public BattleTileData GetAdaptiveOpponentTotemTile()
        {
            if (battleStore == null)
                return null;

            BattleLoadoutSnapshot loadout = MahjongSession.OpponentBattleLoadout;
            if (loadout == null || string.IsNullOrWhiteSpace(loadout.TotemTileId))
                return null;

            return battleStore.TryGetTileDataById(loadout.TotemTileId, out BattleTileData totem)
                ? totem
                : null;
        }

        private BattleTileData SelectOpponentTileByRarity(BattleTileRarity rarity, HashSet<string> usedIds, int roundNumber, int salt)
        {
            List<BattleTileData> exact = CollectBattleTilesByRarity(rarity, usedIds);
            if (exact.Count > 0)
                return exact[ResolveDeterministicIndex(exact.Count, roundNumber, salt, rarity)];

            for (int offset = 1; offset <= 5; offset++)
            {
                int lower = (int)rarity - offset;
                if (lower >= (int)BattleTileRarity.Standard)
                {
                    List<BattleTileData> lowerPool = CollectBattleTilesByRarity((BattleTileRarity)lower, usedIds);
                    if (lowerPool.Count > 0)
                        return lowerPool[ResolveDeterministicIndex(lowerPool.Count, roundNumber, salt, (BattleTileRarity)lower)];
                }

                int higher = (int)rarity + offset;
                if (higher <= (int)BattleTileRarity.Mythic)
                {
                    List<BattleTileData> higherPool = CollectBattleTilesByRarity((BattleTileRarity)higher, usedIds);
                    if (higherPool.Count > 0)
                        return higherPool[ResolveDeterministicIndex(higherPool.Count, roundNumber, salt, (BattleTileRarity)higher)];
                }
            }

            List<BattleTileData> any = CollectValidBattleTiles(usedIds);
            if (any.Count > 0)
                return any[ResolveDeterministicIndex(any.Count, roundNumber, salt, BattleTileRarity.Standard)];

            List<BattleTileData> unrestricted = CollectBattleTilesByRarity(rarity, null);
            if (unrestricted.Count > 0)
                return unrestricted[ResolveDeterministicIndex(unrestricted.Count, roundNumber, salt, rarity)];

            unrestricted = CollectValidBattleTiles(null);
            return unrestricted.Count > 0 ? unrestricted[ResolveDeterministicIndex(unrestricted.Count, roundNumber, salt, BattleTileRarity.Standard)] : null;
        }

        private List<BattleTileData> CollectBattleTilesByRarity(BattleTileRarity rarity, HashSet<string> usedIds)
        {
            List<BattleTileData> result = new();
            IReadOnlyList<BattleTileData> tiles = battleStore != null ? battleStore.BattleTiles : null;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData data = tiles[i];
                if (!IsValidOpponentTileCandidate(data, usedIds) || data.Rarity != rarity)
                    continue;

                result.Add(data);
            }

            return result;
        }

        private List<BattleTileData> CollectValidBattleTiles(HashSet<string> usedIds)
        {
            List<BattleTileData> result = new();
            IReadOnlyList<BattleTileData> tiles = battleStore != null ? battleStore.BattleTiles : null;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData data = tiles[i];
                if (IsValidOpponentTileCandidate(data, usedIds))
                    result.Add(data);
            }

            return result;
        }

        private static bool IsValidOpponentTileCandidate(BattleTileData data, HashSet<string> usedIds)
        {
            if (data == null || data.Prefab == null || string.IsNullOrWhiteSpace(data.Id))
                return false;

            return usedIds == null || !usedIds.Contains(data.Id);
        }

        private int ResolveDeterministicIndex(int count, int roundNumber, int salt, BattleTileRarity rarity)
        {
            if (count <= 1)
                return 0;

            string key = $"{MahjongSession.BattleOpponentName}|{MahjongSession.BattleOpponentCharacterId}|{roundNumber}|{salt}|{rarity}";
            return Mathf.Abs(key.GetHashCode() % count);
        }

        private static HashSet<string> BuildTileIdSet(IReadOnlyList<BattleTileData> tiles)
        {
            HashSet<string> result = new(StringComparer.Ordinal);
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                string id = tiles[i] != null ? tiles[i].Id : string.Empty;
                if (!string.IsNullOrWhiteSpace(id))
                    result.Add(id);
            }

            return result;
        }

        private static BattleTileRarity ResolveHighestRarity(IReadOnlyList<BattleTileData> tiles)
        {
            BattleTileRarity result = BattleTileRarity.Standard;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData data = tiles[i];
                if (data != null && data.Rarity > result)
                    result = data.Rarity;
            }

            return result;
        }

        private bool HasTileSource(IReadOnlyList<BattleTileData> source)
        {
            if (BattleLoreTutorialSession.IsActive)
                return source != null && source.Count > 0;

            if (source == null || source.Count != BattleTileInventoryService.RequiredActiveTiles)
                return false;

            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                BattleTileData tile = source[i];
                if (tile?.Prefab == null || string.IsNullOrWhiteSpace(tile.Id) || !ids.Add(tile.Id))
                    return false;
            }

            return true;
        }

        private int ResolveLayoutLevel(int layoutIndex)
        {
            if (BattleLoreTutorialSession.IsActive)
                return BattleLoreTutorialSession.ActiveStage;

            if (battleLayoutPresetService != null)
            {
                int min = battleLayoutPresetService.MinLevel;
                int max = battleLayoutPresetService.MaxLevel;
                int count = Mathf.Max(1, max - min + 1);

                if (loopLayouts)
                    return min + (Mathf.Abs(layoutIndex) % count);

                return Mathf.Clamp(min + layoutIndex, min, max);
            }

            return Mathf.Max(1, fallbackLayoutLevel);
        }

        private int ResolveInitialBattleLayoutIndex()
        {
            if (!randomizeBattleLayouts)
                return currentRoundIndex;

            return ResolveNextBattleLayoutIndex(-1, 3);
        }

        private int ResolveNextBattleLayoutIndex(int previousIndex, int salt)
        {
            if (!randomizeBattleLayouts)
                return previousIndex >= 0 ? previousIndex : currentRoundIndex;

            int count = ResolveBattleLayoutCount();
            if (count <= 1)
                return 0;

            if (IsRealtimeOpponentBattleActive() && MahjongSession.BattleMatchSeed > 0)
            {
                int seed = MahjongSession.BattleMatchSeed + CurrentRoundNumber * 7919 + salt * 104729;
                int deterministic = Mathf.Abs(seed) % count;
                if (deterministic == previousIndex)
                    deterministic = (deterministic + 1) % count;

                return deterministic;
            }

            int next = UnityEngine.Random.Range(0, count);
            if (next == previousIndex)
                next = (next + UnityEngine.Random.Range(1, count)) % count;

            return next;
        }

        private int ResolveBattleLayoutCount()
        {
            if (battleLayoutPresetService != null)
            {
                int min = battleLayoutPresetService.MinLevel;
                int max = battleLayoutPresetService.MaxLevel;
                return Mathf.Max(1, max - min + 1);
            }

            List<int> levels = BattleLayoutPresets.GetAllLevels();
            return Mathf.Max(1, levels != null ? levels.Count : 1);
        }

        private void HandleBoardCleared(BattleBoard board)
        {
            if (matchFinished || board == null)
                return;

            if (stateText != null)
            {
                if (board == playerBoard)
                    stateText.text = statePlayerBoardCleared;
                else if (board == opponentBoard)
                    stateText.text = stateOpponentBoardCleared;
            }

            RefreshHud();
            NotifyStateChanged();

            Log($"Board cleared inside round | Side={board.Side}");

            if (board == playerBoard)
            {
                if (playerBoardRoutine != null)
                    StopCoroutine(playerBoardRoutine);

                playerBoardRoutine = StartCoroutine(RebuildPlayerBoardRoutine());
            }
            else if (board == opponentBoard)
            {
                if (opponentBoardRoutine != null)
                    StopCoroutine(opponentBoardRoutine);

                opponentBoardRoutine = StartCoroutine(RebuildOpponentBoardRoutine());
            }
        }

        private void HandleDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int damage, int hpAfter)
        {
            if (matchFinished || damage <= 0)
                return;

            if (targetSide == BattleBoardSide.Opponent)
            {
                PlayImpactSoundIfNotAlreadyLaunched(BattleBoardSide.Player);
                PlayAttackAnimationIfNotAlreadyLaunched(BattleBoardSide.Player);
            }
            else if (targetSide == BattleBoardSide.Player)
            {
                AppSettings.I?.VibrateMedium();
                PlayImpactSoundIfNotAlreadyLaunched(BattleBoardSide.Opponent);
                PlayAttackAnimationIfNotAlreadyLaunched(BattleBoardSide.Opponent);
            }

            RefreshHud();
            NotifyStateChanged();
        }

        public void BuildCurrentBoardFromFrozenLoadout(BattleBoardSide side)
        {
            if (!matchRunning || matchFinished)
                return;

            if (side == BattleBoardSide.Player)
                BuildBoardForSide(playerBoard, playerLayoutIndex, restartBotAfterBuild: false);
            else
                BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: false);
        }

        private void HandleDamageResultApplied(BattleCombatSystem _, BattleBoardSide targetSide, BattleDamageCalculator.DamageResult result, int hpAfter)
        {
            if (matchFinished || result.FinalDamage <= 0)
                return;

            ShowFloatingDamageText(targetSide, result);
        }

        public IEnumerator PlayMatchedPairAttackSequence(BattleBoardSide attackerSide, BattleTile firstTile, BattleTile secondTile)
        {
            if (!useMatchedPairAttackFx)
                yield break;

            if (firstTile == null || secondTile == null)
                yield break;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
                yield break;

            BattleBoardSide targetSide = attackerSide == BattleBoardSide.Player
                ? BattleBoardSide.Opponent
                : BattleBoardSide.Player;
            RectTransform targetRect = ResolveBattleAvatarRect(targetSide);
            if (targetRect == null)
                yield break;

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Image firstFx = CreateMatchedPairFxImage(canvasRect, firstTile, eventCamera);
            Image secondFx = CreateMatchedPairFxImage(canvasRect, secondTile, eventCamera);
            if (firstFx == null || secondFx == null)
            {
                DestroyMatchedPairFx(firstFx);
                DestroyMatchedPairFx(secondFx);
                yield break;
            }

            Vector2 firstStart = ResolveWorldRectCenterInCanvas(firstTile.Rect, canvasRect, eventCamera);
            Vector2 secondStart = ResolveWorldRectCenterInCanvas(secondTile.Rect, canvasRect, eventCamera);
            Vector2 target = ResolveWorldRectCenterInCanvas(targetRect, canvasRect, eventCamera);
            Vector2 midpoint = (firstStart + secondStart) * 0.5f;
            Vector2 crashOffset = new Vector2(Mathf.Max(0f, matchedPairCrashSpacing) * 0.5f, 0f);
            Vector2 firstCrash = midpoint - crashOffset;
            Vector2 secondCrash = midpoint + crashOffset;

            firstFx.rectTransform.anchoredPosition = firstStart;
            secondFx.rectTransform.anchoredPosition = secondStart;

            Vector3 startScale = Vector3.one;
            Vector3 impactScale = Vector3.one * Mathf.Max(0.1f, matchedPairImpactScale);
            Vector3 targetScale = Vector3.one * Mathf.Max(0.1f, matchedPairTargetScale);

            float crashDuration = Mathf.Max(0.01f, matchedPairCrashDuration);
            for (float t = 0f; t < crashDuration; t += Time.deltaTime)
            {
                float progress = EaseOutCubic(t / crashDuration);
                SetMatchedPairFxState(firstFx, Vector2.LerpUnclamped(firstStart, firstCrash, progress), Vector3.LerpUnclamped(startScale, impactScale, progress), progress * 12f, 1f);
                SetMatchedPairFxState(secondFx, Vector2.LerpUnclamped(secondStart, secondCrash, progress), Vector3.LerpUnclamped(startScale, impactScale, progress), -progress * 12f, 1f);
                yield return null;
            }

            SetMatchedPairFxState(firstFx, firstCrash, impactScale, 12f, 1f);
            SetMatchedPairFxState(secondFx, secondCrash, impactScale, -12f, 1f);

            float flightDuration = Mathf.Max(0.01f, matchedPairFlightDuration);
            float fadeTarget = Mathf.Clamp01(matchedPairTargetAlpha);
            float arcHeight = Mathf.Max(0f, matchedPairFlightArcHeight);
            Vector2 splitOffset = new Vector2(Mathf.Max(0f, matchedPairCrashSpacing) * 0.22f, 0f);

            for (float t = 0f; t < flightDuration; t += Time.deltaTime)
            {
                float progress = EaseInOutCubic(t / flightDuration);
                float arc = Mathf.Sin(progress * Mathf.PI) * arcHeight;
                Vector2 firstTarget = target - splitOffset;
                Vector2 secondTarget = target + splitOffset;
                Vector2 firstPos = Vector2.LerpUnclamped(firstCrash, firstTarget, progress) + new Vector2(0f, arc);
                Vector2 secondPos = Vector2.LerpUnclamped(secondCrash, secondTarget, progress) + new Vector2(0f, arc);
                Vector3 scale = Vector3.LerpUnclamped(impactScale, targetScale, progress);
                float alpha = Mathf.Lerp(1f, fadeTarget, progress);
                SetMatchedPairFxState(firstFx, firstPos, scale, Mathf.Lerp(12f, 0f, progress), alpha);
                SetMatchedPairFxState(secondFx, secondPos, scale, Mathf.Lerp(-12f, 0f, progress), alpha);
                yield return null;
            }

            PlayAttackAnimationForSide(attackerSide);
            PlayImpactSound();

            DestroyMatchedPairFx(firstFx);
            DestroyMatchedPairFx(secondFx);
        }

        private void PlayAttackAnimationForSide(BattleBoardSide side)
        {
            if (side == BattleBoardSide.Player)
                PlayBattleCharacterAction(playerBattleModelView, playerBattleSpriteImage, true, true);
            else
                PlayBattleCharacterAction(opponentBattleModelView, opponentBattleSpriteImage, true, false);

            lastPairAttackAnimationSide = side;
            lastPairAttackAnimationRealtime = Time.realtimeSinceStartup;
        }

        private void PlayAttackAnimationIfNotAlreadyLaunched(BattleBoardSide side)
        {
            bool alreadyLaunched =
                lastPairAttackAnimationSide == side &&
                Time.realtimeSinceStartup - lastPairAttackAnimationRealtime <= 1.5f;

            if (!alreadyLaunched)
                PlayAttackAnimationForSide(side);
        }

        private void PlayImpactSoundIfNotAlreadyLaunched(BattleBoardSide attackerSide)
        {
            bool alreadyLaunched =
                lastPairAttackAnimationSide == attackerSide &&
                Time.realtimeSinceStartup - lastPairAttackAnimationRealtime <= 1.5f;

            if (!alreadyLaunched)
                PlayImpactSound();
        }

        private void PlayBattleCharacterAction(
            BattleCharacterModelView modelView,
            Image spriteImage,
            bool isAttack,
            bool isPlayerSide)
        {
            bool playedModelAnimation = false;
            if (modelView != null)
            {
                playedModelAnimation = isAttack
                    ? modelView.PlayAttackAnimation()
                    : modelView.PlayHitAnimation();

                if (!playedModelAnimation && modelView.HasModel)
                    return;
            }

            if (!playedModelAnimation)
                PlaySpriteActionPulse(spriteImage, isAttack, isPlayerSide);
        }

        private void PlaySpriteActionPulse(Image spriteImage, bool isAttack, bool isPlayerSide)
        {
            if (spriteImage == null || spriteImage.rectTransform == null || !spriteImage.gameObject.activeInHierarchy)
                return;

            Coroutine routine = isPlayerSide ? playerCharacterActionRoutine : opponentCharacterActionRoutine;
            if (routine != null)
                StopCoroutine(routine);

            float distance = isAttack ? characterAttackPulseDistance : characterHitPulseDistance;
            float direction = isPlayerSide
                ? (isAttack ? 1f : -1f)
                : (isAttack ? -1f : 1f);

            routine = StartCoroutine(SpriteActionPulseRoutine(spriteImage.rectTransform, direction * distance, isPlayerSide));

            if (isPlayerSide)
                playerCharacterActionRoutine = routine;
            else
                opponentCharacterActionRoutine = routine;
        }

        private IEnumerator SpriteActionPulseRoutine(RectTransform rect, float localX, bool isPlayerSide)
        {
            Vector2 start = rect.anchoredPosition;
            Vector2 peak = start + new Vector2(localX, 0f);
            float halfDuration = Mathf.Max(0.01f, characterActionPulseDuration * 0.5f);

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (rect == null)
                    yield break;

                rect.anchoredPosition = Vector2.Lerp(start, peak, t / halfDuration);
                yield return null;
            }

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (rect == null)
                    yield break;

                rect.anchoredPosition = Vector2.Lerp(peak, start, t / halfDuration);
                yield return null;
            }

            if (rect != null)
                rect.anchoredPosition = start;

            if (isPlayerSide)
                playerCharacterActionRoutine = null;
            else
                opponentCharacterActionRoutine = null;
        }

        private void HandleBoardFailed(BattleBoard board)
        {
            if (matchFinished || board == null)
                return;

            if (stateText != null)
            {
                if (board == playerBoard)
                    stateText.text = statePlayerBoardFailed;
                else if (board == opponentBoard)
                    stateText.text = stateOpponentBoardFailed;
            }

            RefreshHud();
            NotifyStateChanged();

            Log($"Board failed inside round | Side={board.Side}");

            if (board == playerBoard)
            {
                if (playerBoardRoutine != null)
                    StopCoroutine(playerBoardRoutine);

                playerBoardRoutine = StartCoroutine(RebuildPlayerBoardRoutine());
            }
            else if (board == opponentBoard)
            {
                if (opponentBoardRoutine != null)
                    StopCoroutine(opponentBoardRoutine);

                opponentBoardRoutine = StartCoroutine(RebuildOpponentBoardRoutine());
            }
        }

        private IEnumerator RebuildPlayerBoardRoutine()
        {
            playerBoardRebuilding = true;
            NotifyStateChanged();

            yield return new WaitForSeconds(Mathf.Max(0.05f, nextRoundDelay));

            playerBoardRoutine = null;

            if (matchFinished || !matchRunning || roundEnding)
            {
                playerBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log("RebuildPlayerBoardRoutine stopped: combat finished");
                playerBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            playerBoardRebuilding = false;
            NotifyStateChanged();

            BuildNextPlayerBoard();
        }

        private IEnumerator RebuildOpponentBoardRoutine()
        {
            opponentBoardRebuilding = true;
            NotifyStateChanged();

            yield return new WaitForSeconds(Mathf.Max(0.05f, nextRoundDelay));

            opponentBoardRoutine = null;

            if (matchFinished || !matchRunning || roundEnding)
            {
                opponentBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log("RebuildOpponentBoardRoutine stopped: combat finished");
                opponentBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            opponentBoardRebuilding = false;
            NotifyStateChanged();

            BuildNextOpponentBoard();
        }

        private void StopBoardRoutines()
        {
            if (playerBoardRoutine != null)
            {
                StopCoroutine(playerBoardRoutine);
                playerBoardRoutine = null;
            }

            if (opponentBoardRoutine != null)
            {
                StopCoroutine(opponentBoardRoutine);
                opponentBoardRoutine = null;
            }
        }

        private void StopRoundTransitionRoutine()
        {
            if (roundTransitionRoutine == null)
                return;

            StopCoroutine(roundTransitionRoutine);
            roundTransitionRoutine = null;
        }

        private void StopCharacterActionRoutines()
        {
            if (playerCharacterActionRoutine != null)
            {
                StopCoroutine(playerCharacterActionRoutine);
                playerCharacterActionRoutine = null;
            }

            if (opponentCharacterActionRoutine != null)
            {
                StopCoroutine(opponentCharacterActionRoutine);
                opponentCharacterActionRoutine = null;
            }

            ClearMatchedPairFx();
        }

        private void StopRestoreBoardsLayoutRoutine()
        {
            if (restoreBoardsLayoutRoutine == null)
                return;

            StopCoroutine(restoreBoardsLayoutRoutine);
            restoreBoardsLayoutRoutine = null;
        }

        private RectTransform ResolveBattleAvatarRect(BattleBoardSide side)
        {
            Image spriteImage = side == BattleBoardSide.Player ? playerBattleSpriteImage : opponentBattleSpriteImage;
            if (spriteImage != null && spriteImage.rectTransform != null)
                return spriteImage.rectTransform;

            BattleCharacterModelView modelView = side == BattleBoardSide.Player ? playerBattleModelView : opponentBattleModelView;
            if (modelView != null && modelView.transform is RectTransform modelRect)
                return modelRect;

            BattleBoard board = side == BattleBoardSide.Player ? playerBoard : opponentBoard;
            return board != null ? board.BoardArea : null;
        }

        private Image CreateMatchedPairFxImage(RectTransform parent, BattleTile tile, Camera camera)
        {
            if (parent == null || tile == null || tile.Rect == null)
                return null;

            Sprite sprite = tile.FaceSprite != null ? tile.FaceSprite : tile.BackSprite;
            if (sprite == null)
                return null;

            GameObject fxObject = new GameObject("MatchedPairTileFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fxObject.layer = parent.gameObject.layer;
            fxObject.transform.SetParent(parent, false);
            fxObject.transform.SetAsLastSibling();

            Image image = fxObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = ResolveRectSizeInCanvas(tile.Rect, parent, camera);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            activeMatchedPairFxImages.Add(image);
            return image;
        }

        private static Vector2 ResolveWorldRectCenterInCanvas(RectTransform source, RectTransform canvasRect, Camera camera)
        {
            if (source == null || canvasRect == null)
                return Vector2.zero;

            Vector3 worldCenter = source.TransformPoint(source.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out Vector2 localPoint);
            return localPoint;
        }

        private static Vector2 ResolveRectSizeInCanvas(RectTransform source, RectTransform canvasRect, Camera camera)
        {
            if (source == null || canvasRect == null)
                return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
        }

        private static void SetMatchedPairFxState(Image image, Vector2 anchoredPosition, Vector3 scale, float rotationZ, float alpha)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private void ClearMatchedPairFx()
        {
            for (int i = activeMatchedPairFxImages.Count - 1; i >= 0; i--)
                DestroyMatchedPairFx(activeMatchedPairFxImages[i]);

            activeMatchedPairFxImages.Clear();
        }

        private void DestroyMatchedPairFx(Image image)
        {
            if (image == null)
                return;

            activeMatchedPairFxImages.Remove(image);
            if (image.gameObject != null)
                Destroy(image.gameObject);
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - (inv * inv * inv);
        }

        private static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        private void StopMatchStartRoutine()
        {
            if (matchStartRoutine == null)
                return;

            StopCoroutine(matchStartRoutine);
            matchStartRoutine = null;
            HideCountdownOverlay();
        }

        private IEnumerator PlayCountdownOverlayStep(string text, bool isStartText, float duration)
        {
            EnsureCountdownOverlay();

            if (countdownOverlayRoot == null || (countdownOverlayText == null && countdownOverlaySpriteImage == null))
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            countdownOverlayRoot.SetActive(true);
            countdownOverlayRoot.transform.SetAsLastSibling();

            Sprite countdownSprite = useCountdownImageSprites ? ResolveCountdownOverlaySprite(text, isStartText) : null;
            RectTransform animatedRect = null;
            bool useSprite = countdownSprite != null && countdownOverlaySpriteImage != null;
            if (useSprite)
            {
                countdownOverlaySpriteImage.sprite = countdownSprite;
                countdownOverlaySpriteImage.color = Color.white;
                countdownOverlaySpriteImage.enabled = true;
                countdownOverlaySpriteImage.preserveAspect = true;
                ApplyCountdownOverlaySpriteLayout(isStartText);
                animatedRect = countdownOverlaySpriteImage.rectTransform;

                if (countdownOverlayText != null)
                    countdownOverlayText.alpha = 0f;
            }
            else if (countdownOverlayText != null)
            {
                RectTransform textRect = countdownOverlayText.rectTransform;
                countdownOverlayText.text = text;
                countdownOverlayText.fontSize = isStartText ? countdownOverlayStartFontSize : countdownOverlayNumberFontSize;
                countdownOverlayText.color = countdownOverlayTextColor;
                countdownOverlayText.alpha = 1f;
                countdownOverlayText.enableAutoSizing = true;
                countdownOverlayText.fontSizeMin = Mathf.Min(72f, countdownOverlayText.fontSize);
                countdownOverlayText.fontSizeMax = countdownOverlayText.fontSize;
                countdownOverlayText.enableVertexGradient = true;
                countdownOverlayText.colorGradient = new VertexGradient(
                    new Color(1f, 0.98f, 0.74f, 1f),
                    new Color(1f, 0.82f, 0.22f, 1f),
                    new Color(0.84f, 0.38f, 0.08f, 1f),
                    new Color(1f, 0.66f, 0.16f, 1f));
                animatedRect = textRect;
            }

            if (animatedRect == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            if (countdownOverlayBackdrop != null)
            {
                countdownOverlayBackdrop.color = countdownOverlayBackdropColor;
                countdownOverlayBackdrop.enabled = true;
            }

            float safeDuration = Mathf.Max(0.05f, duration);
            float holdUntil = Mathf.Clamp01(countdownOverlayHoldPortion);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float animatedT = Mathf.InverseLerp(holdUntil, 1f, t);
                float eased = EaseOutCubic(animatedT);
                float settle = EaseOutCubic(Mathf.Clamp01(t / Mathf.Max(0.01f, holdUntil)));
                float entranceScale = Mathf.Lerp(countdownOverlayEndScale, countdownOverlaySettleScale, settle);
                float exitScale = Mathf.Lerp(countdownOverlaySettleScale, countdownOverlayStartScale, eased);
                float scale = animatedT <= 0f ? entranceScale : exitScale;
                float alpha = 1f - eased;
                animatedRect.localScale = Vector3.one * scale;
                if (useSprite)
                {
                    Color spriteColor = Color.white;
                    spriteColor.a = alpha;
                    countdownOverlaySpriteImage.color = spriteColor;
                }
                else if (countdownOverlayText != null)
                {
                    countdownOverlayText.alpha = alpha;
                }

                if (countdownOverlayBackdrop != null)
                {
                    Color backdropColor = countdownOverlayBackdropColor;
                    backdropColor.a *= Mathf.Lerp(1f, 0.15f, eased);
                    countdownOverlayBackdrop.color = backdropColor;
                }

                SetCountdownGraphicState(countdownOverlayTopLine, countdownOverlayFrameColor, 0f, Vector3.one);
                SetCountdownGraphicState(countdownOverlayBottomLine, countdownOverlayFrameColor, 0f, Vector3.one);

                elapsed += Time.deltaTime;
                yield return null;
            }

            animatedRect.localScale = Vector3.one * countdownOverlayStartScale;
            if (useSprite)
            {
                Color spriteColor = Color.white;
                spriteColor.a = 0f;
                countdownOverlaySpriteImage.color = spriteColor;
                countdownOverlaySpriteImage.enabled = false;
            }
            else if (countdownOverlayText != null)
            {
                countdownOverlayText.alpha = 0f;
            }
            SetCountdownGraphicState(countdownOverlayTopLine, countdownOverlayFrameColor, 0f, Vector3.one);
            SetCountdownGraphicState(countdownOverlayBottomLine, countdownOverlayFrameColor, 0f, Vector3.one);
        }

        private void EnsureCountdownOverlay()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            if (countdownOverlayRoot == null && !createCountdownOverlayIfMissing)
                return;

            if (countdownOverlayRoot == null)
            {
                GameObject root = new GameObject("BattleStartCountdownOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                root.layer = canvas.gameObject.layer;
                root.transform.SetParent(canvas.transform, false);
                countdownOverlayRoot = root;

                countdownOverlayBackdrop = root.GetComponent<Image>();
                countdownOverlaySpriteImage = CreateCountdownOverlayImage(root.transform, "BattleStartCountdownSprite");
                countdownOverlayText = CreateCountdownOverlayText(root.transform);
            }

            if (countdownOverlayRoot.transform.parent != canvas.transform)
                countdownOverlayRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = countdownOverlayRoot.transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.localScale = Vector3.one;
            }

            countdownOverlayBackdrop = countdownOverlayBackdrop != null
                ? countdownOverlayBackdrop
                : countdownOverlayRoot.GetComponent<Image>();
            if (countdownOverlayBackdrop != null)
            {
                countdownOverlayBackdrop.raycastTarget = false;
                countdownOverlayBackdrop.color = countdownOverlayBackdropColor;
            }

            countdownOverlayTopLine = FindCountdownOverlayImage("BattleStartCountdownTopLine", countdownOverlayTopLine);
            countdownOverlayBottomLine = FindCountdownOverlayImage("BattleStartCountdownBottomLine", countdownOverlayBottomLine);
            DisableCountdownLine(countdownOverlayTopLine);
            DisableCountdownLine(countdownOverlayBottomLine);
            countdownOverlaySpriteImage = EnsureCountdownOverlayImage("BattleStartCountdownSprite", countdownOverlaySpriteImage);
            if (countdownOverlaySpriteImage != null)
            {
                countdownOverlaySpriteImage.preserveAspect = true;
                countdownOverlaySpriteImage.color = Color.clear;
                countdownOverlaySpriteImage.enabled = false;
            }

            if (countdownOverlayText == null)
                countdownOverlayText = countdownOverlayRoot.GetComponentInChildren<TMP_Text>(true);
            if (countdownOverlayText == null)
                countdownOverlayText = CreateCountdownOverlayText(countdownOverlayRoot.transform);

            if (countdownOverlaySpriteImage != null)
                countdownOverlaySpriteImage.transform.SetSiblingIndex(3);
            countdownOverlayText.transform.SetAsLastSibling();

            ApplyCountdownOverlayGraphicLayout();
            ApplyCountdownOverlayTextLayout();
            ApplyCountdownOverlaySpriteLayout(false);
            countdownOverlayRoot.SetActive(false);
        }

        private Image EnsureCountdownOverlayImage(string objectName, Image image)
        {
            if (countdownOverlayRoot == null)
                return image;

            if (image != null)
                return image;

            Transform existing = countdownOverlayRoot.transform.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
            return image != null ? image : CreateCountdownOverlayImage(countdownOverlayRoot.transform, objectName);
        }

        private Image FindCountdownOverlayImage(string objectName, Image image)
        {
            if (countdownOverlayRoot == null)
                return image;

            if (image != null)
                return image;

            Transform existing = countdownOverlayRoot.transform.Find(objectName);
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        private static void DisableCountdownLine(Image line)
        {
            if (line == null)
                return;

            line.color = Color.clear;
            line.enabled = false;
            line.gameObject.SetActive(false);
        }

        private Image CreateCountdownOverlayImage(Transform parent, string objectName)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.clear;

            return image;
        }

        private TMP_Text CreateCountdownOverlayText(Transform parent)
        {
            GameObject textObject = new GameObject("BattleStartCountdownText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = countdownOverlayTextColor;
            text.fontSize = countdownOverlayNumberFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = 72f;
            text.fontSizeMax = countdownOverlayNumberFontSize;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                if (styleSource.fontSharedMaterial != null)
                    text.fontMaterial = new Material(styleSource.fontSharedMaterial);
            }
            BattlePopupStyle.ApplyFontOnly(text);

            text.outlineWidth = 0.18f;
            text.outlineColor = new Color(0.18f, 0.05f, 0f, 0.98f);
            Material textMaterial = text.fontMaterial;
            if (textMaterial != null)
            {
                textMaterial.EnableKeyword("UNDERLAY_ON");
                if (textMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    textMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.86f));
                if (textMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    textMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.08f);
                if (textMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    textMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.08f);
                if (textMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                    textMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.34f);
                if (textMaterial.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    textMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.12f);
                text.UpdateMeshPadding();
            }

            return text;
        }

        private void ApplyCountdownOverlayGraphicLayout()
        {
            DisableCountdownLine(countdownOverlayTopLine);
            DisableCountdownLine(countdownOverlayBottomLine);
        }

        private static void ApplyCountdownLineLayout(Image line, float y)
        {
            RectTransform rect = line != null ? line.rectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.29f, y);
            rect.anchorMax = new Vector2(0.71f, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 4f);
        }

        private void ApplyCountdownOverlayTextLayout()
        {
            if (countdownOverlayText == null || countdownOverlayText.rectTransform == null)
                return;

            RectTransform rect = countdownOverlayText.rectTransform;
            rect.anchorMin = new Vector2(0.04f, 0.08f);
            rect.anchorMax = new Vector2(0.96f, 0.92f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void ApplyCountdownOverlaySpriteLayout(bool isStartText)
        {
            if (countdownOverlaySpriteImage == null || countdownOverlaySpriteImage.rectTransform == null)
                return;

            RectTransform rect = countdownOverlaySpriteImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = isStartText ? new Vector2(760f, 430f) : new Vector2(500f, 500f);
            rect.localScale = Vector3.one;
        }

        private static void SetCountdownGraphicState(Image image, Color baseColor, float alpha, Vector3 scale)
        {
            if (image == null)
                return;

            Color color = baseColor;
            color.a *= Mathf.Clamp01(alpha);
            image.color = color;
            image.enabled = color.a > 0.001f;
            image.rectTransform.localScale = scale;
        }

        private void HideCountdownOverlay()
        {
            if (countdownOverlayText != null)
            {
                countdownOverlayText.alpha = 0f;
                if (countdownOverlayText.rectTransform != null)
                    countdownOverlayText.rectTransform.localScale = Vector3.one;
            }

            if (countdownOverlaySpriteImage != null)
            {
                countdownOverlaySpriteImage.enabled = false;
                countdownOverlaySpriteImage.color = Color.clear;
                if (countdownOverlaySpriteImage.rectTransform != null)
                    countdownOverlaySpriteImage.rectTransform.localScale = Vector3.one;
            }

            if (countdownOverlayRoot != null)
                countdownOverlayRoot.SetActive(false);
        }

        private IEnumerator PlayStartCountdownRoutine()
        {
            if (!useStartCountdown)
                yield break;

            EnsureCountdownOverlay();

            for (int i = Mathf.Max(1, countdownSeconds); i > 0; i--)
            {
                if (stateText != null)
                    stateText.text = string.Empty;

                NotifyStateChanged();
                yield return PlayCountdownOverlayStep(i.ToString(), false, Mathf.Max(0.05f, countdownInterval));
            }

            string localizedStartText = ResolveCountdownStartText();
            if (stateText != null)
                stateText.text = string.Empty;

            NotifyStateChanged();
            yield return PlayCountdownOverlayStep(localizedStartText, true, Mathf.Max(0.05f, startTextDuration));
            HideCountdownOverlay();
        }

        private string ResolveCountdownStartText()
        {
            string localized = GameLocalization.Text("battle.countdown.start");
            if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, "battle.countdown.start", StringComparison.Ordinal))
                return localized;

            return string.IsNullOrWhiteSpace(countdownStartText) ? "Start" : countdownStartText;
        }

        private Sprite ResolveCountdownOverlaySprite(string text, bool isStartText)
        {
            if (isStartText)
                return GetStartTextSprite(CurrentLanguage());

            return int.TryParse(text, out int number) ? GetCountdownNumberSprite(number) : null;
        }

        private GameLanguage CurrentLanguage()
        {
            return AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
        }

        private static Sprite GetCountdownNumberSprite(int number)
        {
            if (number < 1 || number > 3)
                return null;

            if (CountdownNumberSprites.TryGetValue(number, out Sprite cached) && cached != null)
                return cached;

            Sprite singleSprite = Resources.Load<Sprite>(BattleCountdownResourceRoot + number + "n");
            if (singleSprite != null)
            {
                CountdownNumberSprites[number] = singleSprite;
                return singleSprite;
            }

            Sprite sheetSprite = LoadNamedSprite(
                CountdownNumbersResourcePath,
                "BattleCountdownNumber" + number,
                "CountdownNumber" + number,
                "Number" + number,
                "321Numbers_" + (3 - number));
            if (sheetSprite != null)
            {
                CountdownNumberSprites[number] = sheetSprite;
                return sheetSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(CountdownNumbersResourcePath);
            if (texture == null)
                return null;

            float columnWidth = texture.width / 3f;
            int column = 3 - number;
            Rect rect = new Rect(columnWidth * column, 0f, columnWidth, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "BattleCountdownNumber" + number;
            CountdownNumberSprites[number] = sprite;
            return sprite;
        }

        private static Sprite GetRoundNumberSprite(int number)
        {
            if (number < 1 || number > 3)
                return null;

            if (RoundNumberSprites.TryGetValue(number, out Sprite cached) && cached != null)
                return cached;

            Sprite singleSprite = Resources.Load<Sprite>(BattleCountdownResourceRoot + number + "n");
            if (singleSprite != null)
            {
                RoundNumberSprites[number] = singleSprite;
                return singleSprite;
            }

            Sprite sheetSprite = LoadNamedSprite(
                CountdownNumbersResourcePath,
                "BattleRoundNumber" + number,
                "RoundNumber" + number,
                "Number" + number,
                "321Numbers_" + (3 - number));
            if (sheetSprite != null)
            {
                RoundNumberSprites[number] = sheetSprite;
                return sheetSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(CountdownNumbersResourcePath);
            if (texture == null)
                return null;

            float columnWidth = texture.width / 3f;
            int column = 3 - number;
            Rect rect = new Rect(columnWidth * column, texture.height * 0.12f, columnWidth, texture.height * 0.76f);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "BattleRoundNumber" + number;
            RoundNumberSprites[number] = sprite;
            return sprite;
        }

        private static Sprite GetStartTextSprite(GameLanguage language)
        {
            if (StartTextSprites.TryGetValue(language, out Sprite cached) && cached != null)
                return cached;

            Sprite singleSprite = Resources.Load<Sprite>(BattleCountdownResourceRoot + "StartText" + GetLanguageAssetSuffix(language));
            if (singleSprite != null)
            {
                StartTextSprites[language] = singleSprite;
                return singleSprite;
            }

            Sprite sheetSprite = LoadNamedSprite(
                CountdownStartResourcePath,
                "BattleStart" + language,
                "StartText_" + language,
                "Start_" + language,
                language.ToString());
            if (sheetSprite != null && sheetSprite.rect.width > 220f)
            {
                StartTextSprites[language] = sheetSprite;
                return sheetSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(CountdownStartResourcePath);
            if (texture == null)
                return null;

            float rowHeight = texture.height / 3f;
            int rowFromTop = language switch
            {
                GameLanguage.English => 0,
                GameLanguage.German => 0,
                GameLanguage.Turkish => 2,
                _ => 1
            };
            Rect rect = new Rect(0f, texture.height - rowHeight * (rowFromTop + 1), texture.width, rowHeight);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "BattleCountdownStart" + language;
            StartTextSprites[language] = sprite;
            return sprite;
        }

        private static Sprite GetRoundBadgeSprite(GameLanguage language)
        {
            if (RoundBadgeSprites.TryGetValue(language, out Sprite cached) && cached != null)
                return cached;

            Sprite singleSprite = Resources.Load<Sprite>(BattleCountdownResourceRoot + "Round" + GetLanguageAssetSuffix(language));
            if (singleSprite != null)
            {
                RoundBadgeSprites[language] = singleSprite;
                return singleSprite;
            }

            Sprite sheetSprite = LoadNamedSprite(
                RoundBadgeResourcePath,
                "BattleRoundBadge" + language,
                "RoundBadge_" + language,
                "Round_" + language,
                language.ToString());
            if (sheetSprite != null && sheetSprite.rect.width > 220f)
            {
                RoundBadgeSprites[language] = sheetSprite;
                return sheetSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(RoundBadgeResourcePath);
            if (texture == null)
                return null;

            float columnWidth = texture.width / 3f;
            int column = language switch
            {
                GameLanguage.English => 1,
                GameLanguage.German => 1,
                GameLanguage.Turkish => 2,
                _ => 0
            };
            Rect rect = new Rect(columnWidth * column, 0f, columnWidth, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "BattleRoundBadge" + language;
            RoundBadgeSprites[language] = sprite;
            return sprite;
        }

        private static string GetLanguageAssetSuffix(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.English => "EN",
                GameLanguage.German => "EN",
                GameLanguage.Turkish => "TR",
                _ => "RU"
            };
        }

        private static Sprite LoadNamedSprite(string resourcePath, params string[] names)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) || names == null || names.Length == 0)
                return null;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                string expected = names[i];
                if (string.IsNullOrWhiteSpace(expected))
                    continue;

                for (int j = 0; j < sprites.Length; j++)
                {
                    Sprite sprite = sprites[j];
                    if (sprite != null && string.Equals(sprite.name, expected, StringComparison.OrdinalIgnoreCase))
                        return sprite;
                }
            }

            return null;
        }

        private void RefreshHud()
        {
            if (roundText != null)
            {
                roundText.text = GetRoundText();
                roundText.enabled = showRoundHud;
            }

            if (scoreText != null)
                scoreText.text = GetScoreText();

            RefreshRoundBadge();
            RefreshBattleHpBars();
        }

        private void RefreshRoundBadge()
        {
            if (!showRoundHud)
            {
                SetRoundBadgeVisible(false);
                if (roundText != null)
                    roundText.enabled = false;
                return;
            }

            if (!useRoundBadgeImageSprites)
            {
                SetRoundBadgeVisible(false);
                if (roundText != null)
                    roundText.enabled = true;
                return;
            }

            Sprite badgeSprite = GetRoundBadgeSprite(CurrentLanguage());
            Sprite numberSprite = GetRoundNumberSprite(CurrentRoundNumber);
            if (badgeSprite == null || numberSprite == null)
            {
                SetRoundBadgeVisible(false);
                if (roundText != null)
                    roundText.enabled = true;
                return;
            }

            EnsureRoundBadgeImages();
            if (roundBadgeImage == null || roundBadgeNumberImage == null)
                return;

            roundBadgeImage.sprite = badgeSprite;
            roundBadgeImage.color = Color.white;
            roundBadgeImage.enabled = true;
            roundBadgeImage.preserveAspect = true;
            roundBadgeNumberImage.sprite = numberSprite;
            roundBadgeNumberImage.color = Color.white;
            roundBadgeNumberImage.enabled = true;
            roundBadgeNumberImage.preserveAspect = true;
            roundBadgeImage.transform.SetAsLastSibling();
            roundBadgeNumberImage.transform.SetAsLastSibling();
            if (roundText != null)
                roundText.enabled = false;
            ApplyRoundBadgeLayout();
        }

        private void EnsureBattleOpponentSession()
        {
            bool needsOpponent = MahjongSession.LaunchMode != MahjongLaunchMode.Battle ||
                                 string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName);

            if (!needsOpponent)
                return;

            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            MahjongBattleLobbyMode mode = MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.None
                ? MahjongBattleLobbyMode.RandomMatch
                : MahjongBattleLobbySession.SelectedMode;

            MahjongBattleOpponentData opponent = botService.CreateOpponent(mode, ResolvePlayerBattleRankPoints());
            MahjongSession.StartBattle(opponent);

            Log($"Generated fallback bot profile | {opponent.DisplayName} | {opponent.RankTier} {opponent.RankPoints}");
        }

        private void EnsureOpponentBattleCharacter()
        {
            if (!string.IsNullOrWhiteSpace(opponentBattleCharacterId))
                return;

            if (TryApplyLocalWifiOpponentCharacter())
                return;

            if (TryApplySessionOpponentCharacter())
                return;

            BattleCharacterDatabase database = BattleCharacterDatabase.HasInstance
                ? BattleCharacterDatabase.Instance
                : FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);

            if (database == null)
                return;

            database.RebuildCache();
            List<BattleCharacterDatabase.BattleCharacterData> characters = database.GetEnabledCharacters();
            if (characters == null || characters.Count == 0)
                return;

            int index = Mathf.Abs(MahjongSession.BattleMatchSeed);
            if (index <= 0)
                index = UnityEngine.Random.Range(0, int.MaxValue);

            BattleCharacterDatabase.BattleCharacterData selected = characters[index % characters.Count];
            if (selected == null || string.IsNullOrWhiteSpace(selected.Id))
                return;

            opponentBattleCharacterId = selected.Id;
            Log($"Opponent battle character selected | {opponentBattleCharacterId}");
        }

        private bool TryApplyLocalWifiOpponentCharacter()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.LocalWifiMatch ||
                LocalWifiBattleNetwork.I == null ||
                LocalWifiBattleNetwork.I.RemotePlayer == null)
            {
                return false;
            }

            string characterId = LocalWifiBattleNetwork.I.RemotePlayer.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (ResolveBattleCharacter(characterId) == null)
                return false;

            opponentBattleCharacterId = characterId.Trim();
            Log($"Local Wi-Fi opponent battle character applied | {opponentBattleCharacterId}");
            return true;
        }

        private bool TryApplySessionOpponentCharacter()
        {
            string characterId = MahjongSession.BattleOpponentCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (ResolveBattleCharacter(characterId) == null)
                return false;

            opponentBattleCharacterId = characterId.Trim();
            Log($"Session opponent battle character applied | {opponentBattleCharacterId}");
            return true;
        }

        private int ResolvePlayerBattleRankPoints()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Mahjong != null && profile.Mahjong.Battle != null
                ? Mathf.Max(0, profile.Mahjong.Battle.RankPoints)
                : 0;
        }

        private void AutoResolveOpponentProfileUi()
        {
            if (hideLegacyPlayerHpText)
                HideLegacyHpTexts();

            if (opponentBattleSpriteImage == null)
            {
                opponentBattleSpriteImage = FindImageByObjectName("OpponentBattleSprite");
                if (opponentBattleSpriteImage == null)
                    opponentBattleSpriteImage = FindImageByObjectName("OpponentCharacterBattleSprite");
                if (opponentBattleSpriteImage == null)
                    opponentBattleSpriteImage = FindImageByObjectName("OpponentProfileSprite");
            }

            if (opponentNameText == null)
                opponentNameText = FindTextByObjectName("OpponentName");

            if (opponentRankText == null)
            {
                opponentRankText = FindTextByObjectName("OpponentRank");
                if (opponentRankText == null)
                    opponentRankText = FindTextByObjectName("OpponentRankText");
                if (opponentRankText == null)
                    opponentRankText = FindTextByObjectName("OpponentRankTier");
            }

            if (opponentStatsText == null)
            {
                opponentStatsText = FindTextByObjectName("OpponentStats");
                if (opponentStatsText == null)
                    opponentStatsText = FindTextByObjectName("OpponentStatsText");
            }

            if (opponentHpBarFill == null)
            {
                opponentHpBarFill = FindImageByObjectName("OpponentHpBarFill");
                if (opponentHpBarFill == null)
                    opponentHpBarFill = FindImageByObjectName("OpponentHPBarFill");
            }

            if (opponentHpBarText == null)
            {
                opponentHpBarText = FindTextByObjectName("OpponentHpBarText");
                if (opponentHpBarText == null)
                    opponentHpBarText = FindTextByObjectName("OpponentHPBarText");
            }

            if (opponentHpBarFill == null || opponentHpBarText == null)
            {
                Transform hpParent = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText);
                if (hpParent != null)
                    CreateOpponentHpBar(hpParent);
            }

            if (opponentCardPortraitImage == null)
                opponentCardPortraitImage = FindImageByObjectName("OpponentCardPortrait");

            if (opponentTotemImage == null)
                opponentTotemImage = FindImageByObjectName("OpponentTotemImage");

            if (opponentTotemText == null)
                opponentTotemText = FindTextByObjectName("OpponentTotemText");

            if (createOpponentProfileUiIfMissing &&
                (opponentBattleSpriteImage == null ||
                 opponentNameText == null ||
                 opponentRankText == null ||
                 opponentStatsText == null))
            {
                CreateOpponentProfileUi();
            }

            Transform opponentCardParent = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText, opponentCardPortraitImage, opponentTotemImage);
            if (opponentCardParent != null)
            {
                EnsureProfileCardPortraitUi(opponentCardParent, ref opponentCardPortraitImage, "OpponentCardPortrait");
                EnsureOpponentTotemUi(opponentCardParent);
            }
        }

        private void AutoResolvePlayerProfileUi()
        {
            if (hideLegacyPlayerHpText)
                HideLegacyHpTexts();

            if (playerBattleSpriteImage == null)
            {
                playerBattleSpriteImage = FindImageByObjectName("PlayerBattleSprite");
                if (playerBattleSpriteImage == null)
                    playerBattleSpriteImage = FindImageByObjectName("PlayerCharacterBattleSprite");
                if (playerBattleSpriteImage == null)
                    playerBattleSpriteImage = FindImageByObjectName("PlayerProfileSprite");
            }

            if (playerNameText == null)
            {
                playerNameText = FindTextByObjectName("PlayerName");
                if (playerNameText == null)
                    playerNameText = FindTextByObjectName("PlayerNameText");
            }

            if (playerRankText == null)
            {
                playerRankText = FindTextByObjectName("PlayerRank");
                if (playerRankText == null)
                    playerRankText = FindTextByObjectName("PlayerRankText");
                if (playerRankText == null)
                    playerRankText = FindTextByObjectName("PlayerRankTier");
            }

            if (playerTitleText == null)
            {
                playerTitleText = FindTextByObjectName("PlayerTitle");
                if (playerTitleText == null)
                    playerTitleText = FindTextByObjectName("PlayerTitleText");
            }

            if (playerHpBarFill == null)
            {
                playerHpBarFill = FindImageByObjectName("PlayerHpBarFill");
                if (playerHpBarFill == null)
                    playerHpBarFill = FindImageByObjectName("PlayerHPBarFill");
            }

            if (playerHpBarText == null)
            {
                playerHpBarText = FindTextByObjectName("PlayerHpBarText");
                if (playerHpBarText == null)
                    playerHpBarText = FindTextByObjectName("PlayerHPBarText");
            }

            if (playerHpBarFill == null || playerHpBarText == null)
            {
                Transform hpParent = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerRankText);
                if (hpParent != null)
                    CreatePlayerHpBar(hpParent);
            }

            if (playerCardPortraitImage == null)
                playerCardPortraitImage = FindImageByObjectName("PlayerCardPortrait");

            if (playerTotemImage == null)
            {
                playerTotemImage = FindImageByObjectName("PlayerTotemImage");
                if (playerTotemImage == null)
                    playerTotemImage = FindImageByObjectName("PlayerTotemTile");
            }

            if (playerTotemText == null)
            {
                playerTotemText = FindTextByObjectName("PlayerTotemText");
                if (playerTotemText == null)
                    playerTotemText = FindTextByObjectName("PlayerTotemName");
            }

            if (createPlayerProfileUiIfMissing &&
                (playerBattleSpriteImage == null ||
                 playerNameText == null ||
                 playerRankText == null ||
                 playerTitleText == null))
            {
                CreatePlayerProfileUi();
            }

            Transform totemParent = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerRankText, playerTotemImage);
            if (totemParent != null)
            {
                EnsureProfileCardPortraitUi(totemParent, ref playerCardPortraitImage, "PlayerCardPortrait");
                EnsurePlayerTotemUi(totemParent);
            }
        }

        private void AutoResolveResultPanelUi()
        {
            if (resultPanelRoot == null)
            {
                GameObject foundRoot = GameObject.Find("BattleResultPanel");
                if (foundRoot == null)
                    foundRoot = GameObject.Find("ResultPanel");
                if (foundRoot == null)
                    foundRoot = GameObject.Find("BattleEndPanel");
                if (foundRoot == null)
                    foundRoot = FindInactiveObjectByName("BattleResultPanel");

                resultPanelRoot = foundRoot;
            }

            if (resultTitleText == null)
            {
                resultTitleText = FindTextByObjectName("BattleResultTitle");
                if (resultTitleText == null)
                    resultTitleText = FindTextByObjectName("ResultTitle");
                if (resultTitleText == null)
                    resultTitleText = FindTextByObjectName("ResultTitleText");
            }

            if (resultRewardText == null)
            {
                resultRewardText = FindTextByObjectName("BattleResultReward");
                if (resultRewardText == null)
                    resultRewardText = FindTextByObjectName("ResultRewardText");
            }

            if (resultExperienceText == null)
            {
                resultExperienceText = FindTextByObjectName("BattleResultExperience");
                if (resultExperienceText == null)
                    resultExperienceText = FindTextByObjectName("ResultExperienceText");
            }

            if (resultBattleLobbyButton == null)
            {
                resultBattleLobbyButton = FindButtonByObjectName("BattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("ReturnBattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("BackToBattleLobbyButton");
            }

            if (resultNewMatchButton == null)
            {
                resultNewMatchButton = FindButtonByObjectName("BattleNewMatchButton");
                if (resultNewMatchButton == null)
                    resultNewMatchButton = FindButtonByObjectName("NewMatchButton");
                if (resultNewMatchButton == null)
                    resultNewMatchButton = FindButtonByObjectName("FindNewMatchButton");
            }

            if (createResultPanelIfMissing &&
                (resultPanelRoot == null ||
                 resultTitleText == null ||
                 resultRewardText == null ||
                 resultExperienceText == null ||
                 resultBattleLobbyButton == null ||
                 resultNewMatchButton == null))
            {
                CreateResultPanelUi();
            }

            BindResultPanelButton();
        }

        private TMP_Text FindTextByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.gameObject.name == objectName)
                    return text;
            }

            return null;
        }

        private static GameObject FindInactiveObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject item = objects[i];
                if (item != null &&
                    item.scene.IsValid() &&
                    string.Equals(item.name, objectName, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private Image FindImageByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.gameObject.name == objectName)
                    return image;
            }

            return null;
        }

        private Button FindButtonByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.gameObject.name == objectName)
                    return button;
            }

            return null;
        }

        private void ApplyOpponentProfileUi()
        {
            string opponentName = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName)
                ? "Opponent"
                : MahjongSession.BattleOpponentName;
            opponentName = AllianceIdentityFormatter.FormatName(opponentName, MahjongSession.BattleOpponentAllianceTag);

            string rankTier = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentRankTier)
                ? "Unranked"
                : MahjongSession.BattleOpponentRankTier;

            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            int level = Mathf.Max(1, MahjongSession.BattleOpponentLevel);
            int wins = Mathf.Max(0, MahjongSession.BattleOpponentWins);
            int losses = Mathf.Max(0, MahjongSession.BattleOpponentLosses);
            int total = Mathf.Max(wins + losses, 0);
            int mvpPercent = total > 0 ? Mathf.RoundToInt((float)Mathf.Clamp(MahjongSession.BattleOpponentMvpCount, 0, total) / total * 100f) : 0;
            string rankText = $"{rankTier} {rankPoints} RP   LVL {level}";
            string statsText = FormatBattleProfileStats(wins, losses, mvpPercent, ResolveDisplayOpponentHp(), combatSystem != null ? combatSystem.MaxOpponentHp : 0);
            if (!string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentStatusLine))
                statsText = $"{statsText}\n{MahjongSession.BattleOpponentStatusLine.Trim()}";

            if (opponentNameText != null)
                opponentNameText.text = opponentRankText == null ? $"{opponentName} [{rankText}]" : opponentName;

            if (opponentRankText != null)
                opponentRankText.text = rankText;

            if (opponentStatsText != null)
                opponentStatsText.text = statsText;

            RefreshOpponentCardProfileVisuals();
            RefreshBattleProfileRankIcon(opponentNameText != null ? opponentNameText.transform.parent : null, ref opponentRankIconImage, rankTier, rankPoints, "OpponentRankIcon");

            BattleHudUI hud = FindAnyObjectByType<BattleHudUI>(FindObjectsInactive.Include);
            if (hud != null)
                hud.Refresh();
        }

        private void ApplyOpponentBattleSpriteUi()
        {
            if (opponentBattleSpriteImage == null)
                return;

            BattleCharacterDatabase.BattleCharacterData data = ResolveBattleCharacter(opponentBattleCharacterId);
            if (ApplyBattleModel(data, ref opponentBattleModelView, opponentBattleSpriteImage, true))
                return;

            Sprite battleSprite = ResolveBattleSprite(opponentBattleCharacterId);
            opponentBattleSpriteImage.sprite = battleSprite;
            opponentBattleSpriteImage.enabled = battleSprite != null;
            opponentBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                opponentBattleSpriteImage,
                opponentBattleSpriteOffset,
                opponentBattleSpriteSize,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));
            ApplyImageFlip(opponentBattleSpriteImage, flipOpponentBattleSpriteX);
        }

        private void ApplyPlayerProfileUi()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string playerName = fallbackPlayerName;
            string rankTier = fallbackPlayerRankTier;
            int rankPoints = 0;
            int level = 1;
            int wins = 0;
            int losses = 0;
            int mvpPercent = 0;

            if (profile != null)
            {
                profile.EnsureData();

                if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                    playerName = profile.DisplayName.Trim();
                playerName = AllianceIdentityFormatter.FormatName(playerName, AllianceIdentityFormatter.ResolveOwnTag(profile));

                if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                {
                    level = Mathf.Max(1, profile.Mahjong.Battle.Level);
                    wins = Mathf.Max(0, profile.Mahjong.Battle.Wins);
                    losses = Mathf.Max(0, profile.Mahjong.Battle.Losses);
                    mvpPercent = Mathf.Clamp(profile.Mahjong.Battle.MvpRatePercent, 0, 100);

                    if (!string.IsNullOrWhiteSpace(profile.Mahjong.Battle.RankTier))
                        rankTier = profile.Mahjong.Battle.RankTier.Trim();

                    rankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
                }
            }

            if (playerNameText != null)
                playerNameText.text = playerRankText == null && playerTitleText == null
                    ? $"{playerName} [{string.Format(playerRankFormat, rankTier, rankPoints)}]"
                    : playerName;

            if (playerTitleText != null)
                playerTitleText.text = FormatBattleProfileStats(wins, losses, mvpPercent, ResolveDisplayPlayerHp(), combatSystem != null ? combatSystem.MaxPlayerHp : 0);

            if (playerRankText != null)
                playerRankText.text = $"{rankTier} {rankPoints} RP   LVL {level}";

            RefreshPlayerCardProfileVisuals();
            RefreshBattleProfileRankIcon(playerNameText != null ? playerNameText.transform.parent : null, ref playerRankIconImage, rankTier, rankPoints, "PlayerRankIcon");
        }

        private void RefreshPlayerCardProfileVisuals()
        {
            Transform root = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerCardPortraitImage, playerTotemImage);
            if (root == null)
                return;

            EnsureProfileCardPortraitUi(root, ref playerCardPortraitImage, "PlayerCardPortrait");
            EnsurePlayerTotemUi(root);

            Sprite portrait = ResolveSelectedProfileSprite();
            if (portrait == null && playerBattleSpriteImage != null)
                portrait = playerBattleSpriteImage.sprite;
            ApplyProfileCardSprite(playerCardPortraitImage, portrait);

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            BattleTileStore store = battleStore != null ? battleStore : BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
            BattleTileData totem = BattleTileInventoryService.GetTotemTileData(profile, store);
            Sprite sprite = ResolveBattleTileFaceSprite(totem);
            bool hasTotem = sprite != null;

            if (playerTotemImage != null)
            {
                playerTotemImage.sprite = sprite;
                playerTotemImage.enabled = hasTotem;
                playerTotemImage.gameObject.SetActive(true);
                playerTotemImage.color = Color.white;
                playerTotemImage.preserveAspect = true;
                playerTotemImage.raycastTarget = false;
				playerTotemUpgradeLevel = hasTotem
					? Mathf.Max(0, BattleTileInventoryService.GetUpgradeLevel(profile, totem?.Id))
					: 0;
				RefreshPlayerTotemUpgradeVisual(root);
            }

            if (playerTotemText != null)
            {
                playerTotemText.text = hasTotem ? ResolveBattleTileShortName(totem) : BattleProfileText("Тотем", "Totem", "Totem", "Totem");
                playerTotemText.gameObject.SetActive(false);
            }
        }

        private void RefreshOpponentCardProfileVisuals()
        {
            Transform root = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText, opponentStatsText, opponentCardPortraitImage, opponentTotemImage);
            if (root == null)
                return;

            EnsureProfileCardPortraitUi(root, ref opponentCardPortraitImage, "OpponentCardPortrait");
            EnsureOpponentTotemUi(root);

            Sprite portrait = ResolveProfileSprite(opponentBattleCharacterId);
            if (portrait == null && opponentBattleSpriteImage != null)
                portrait = opponentBattleSpriteImage.sprite;
            ApplyProfileCardSprite(opponentCardPortraitImage, portrait);

            BattleTileData totem = ResolveOpponentTotemData();
            Sprite sprite = ResolveBattleTileFaceSprite(totem);
            bool hasTotem = sprite != null;

            if (opponentTotemImage != null)
            {
                opponentTotemImage.sprite = sprite;
                opponentTotemImage.enabled = hasTotem;
                opponentTotemImage.gameObject.SetActive(true);
                opponentTotemImage.color = Color.white;
                opponentTotemImage.preserveAspect = true;
                opponentTotemImage.raycastTarget = false;
            }

            if (opponentTotemText != null)
            {
                opponentTotemText.text = hasTotem ? ResolveBattleTileShortName(totem) : BattleProfileText("Тотем", "Totem", "Totem", "Totem");
                opponentTotemText.gameObject.SetActive(false);
            }
        }

        private void EnsureProfileCardPortraitUi(Transform root, ref Image image, string objectName)
        {
            if (root == null)
                return;

            if (image == null || image.transform == null || image.transform.parent != root)
            {
                Transform existing = root.Find(objectName);
                image = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (image == null)
            {
                image = CreateProfileImage(
                    root,
                    objectName,
                    Vector2.zero,
                    new Vector2(86f, 100f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));
            }

            EnsureProfileCardPortraitFrame(image, objectName + "Frame");
        }

        private void EnsureProfileCardPortraitFrame(Image portrait, string frameName)
        {
            if (portrait == null)
                return;

            Transform existing = portrait.transform.Find(frameName);
            Image frame = existing != null ? existing.GetComponent<Image>() : null;
            if (frame == null)
            {
                GameObject frameObject = new GameObject(frameName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frameObject.layer = portrait.gameObject.layer;
                frameObject.transform.SetParent(portrait.transform, false);
                frame = frameObject.GetComponent<Image>();
            }

            RectTransform rect = frame.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-7f, -7f);
            rect.offsetMax = new Vector2(7f, 7f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            frame.sprite = ResolveBattleProfilePortraitFrameSprite();
            frame.type = Image.Type.Simple;
            frame.preserveAspect = false;
            frame.color = Color.white;
            frame.raycastTarget = false;
            frame.enabled = frame.sprite != null;
            frame.gameObject.SetActive(frame.sprite != null);
            frame.transform.SetAsLastSibling();
        }

        private Sprite ResolveBattleProfilePortraitFrameSprite()
        {
            if (battleProfilePortraitFrameSprite != null)
                return battleProfilePortraitFrameSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(BattleProfilePortraitFrameResourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                battleProfilePortraitFrameSprite = sprites[0];
                for (int i = 1; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && sprites[i].rect.width * sprites[i].rect.height >
                        battleProfilePortraitFrameSprite.rect.width * battleProfilePortraitFrameSprite.rect.height)
                    {
                        battleProfilePortraitFrameSprite = sprites[i];
                    }
                }
            }

            if (battleProfilePortraitFrameSprite == null)
                battleProfilePortraitFrameSprite = Resources.Load<Sprite>(BattleProfilePortraitFrameResourcePath);

            return battleProfilePortraitFrameSprite;
        }

        private void CleanupOrphanProfilePortraitFrames()
        {
            Transform expectedPlayerFrame = playerCardPortraitImage != null
                ? playerCardPortraitImage.transform.Find("PlayerCardPortraitFrame")
                : null;
            Transform expectedOpponentFrame = opponentCardPortraitImage != null
                ? opponentCardPortraitImage.transform.Find("OpponentCardPortraitFrame")
                : null;

            RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null)
                    continue;

                bool playerFrame = string.Equals(rect.name, "PlayerCardPortraitFrame", StringComparison.Ordinal);
                bool opponentFrame = string.Equals(rect.name, "OpponentCardPortraitFrame", StringComparison.Ordinal);
                if (!playerFrame && !opponentFrame)
                    continue;

                bool expected = playerFrame
                    ? rect.transform == expectedPlayerFrame
                    : rect.transform == expectedOpponentFrame;
                if (!expected && rect.gameObject.activeSelf)
                    rect.gameObject.SetActive(false);
            }
        }

        private void RefreshPlayerTotemUpgradeVisual(Transform legacyProfileRoot = null)
        {
            if (playerTotemImage == null)
                return;

            if (legacyProfileRoot != null && legacyProfileRoot != playerTotemImage.transform)
                BattleTileUpgradeVisual.SetVisible(legacyProfileRoot, false);

            RectTransform rect = playerTotemImage.rectTransform;
            Vector2 size = rect.rect.size;
            if (size.x <= 1f || size.y <= 1f)
                size = rect.sizeDelta;

            bool visible = playerTotemImage.enabled && playerTotemImage.sprite != null;
            BattleTileUpgradeVisual.Apply(
                playerTotemImage.transform,
                Vector2.zero,
                size,
                playerTotemUpgradeLevel,
                visible);
        }

        private static void ApplyProfileCardSprite(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.gameObject.SetActive(true);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void EnsurePlayerTotemUi(Transform root)
        {
            if (root == null)
                return;

            if (playerTotemImage == null || playerTotemImage.transform == null || playerTotemImage.transform.parent != root)
            {
                Transform existing = root.Find("PlayerTotemImage");
                playerTotemImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (playerTotemImage == null)
            {
                playerTotemImage = CreateProfileImage(
                    root,
                    "PlayerTotemImage",
                    Vector2.zero,
                    new Vector2(72f, 96f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));
            }

            if (playerTotemText == null || playerTotemText.transform == null || playerTotemText.transform.parent != root)
            {
                Transform existing = root.Find("PlayerTotemText");
                playerTotemText = existing != null ? existing.GetComponent<TMP_Text>() : null;
            }

            if (playerTotemText == null)
            {
                playerTotemText = CreateProfileText(root, "PlayerTotemText", 18f, Vector2.zero, 5);
                playerTotemText.alignment = TextAlignmentOptions.Center;
            }
            if (playerTotemText != null)
                playerTotemText.gameObject.SetActive(false);
        }

        private void EnsureOpponentTotemUi(Transform root)
        {
            if (root == null)
                return;

            if (opponentTotemImage == null || opponentTotemImage.transform == null || opponentTotemImage.transform.parent != root)
            {
                Transform existing = root.Find("OpponentTotemImage");
                opponentTotemImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (opponentTotemImage == null)
            {
                opponentTotemImage = CreateProfileImage(
                    root,
                    "OpponentTotemImage",
                    Vector2.zero,
                    new Vector2(72f, 96f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f));
            }

            if (opponentTotemText == null || opponentTotemText.transform == null || opponentTotemText.transform.parent != root)
            {
                Transform existing = root.Find("OpponentTotemText");
                opponentTotemText = existing != null ? existing.GetComponent<TMP_Text>() : null;
            }

            if (opponentTotemText == null)
            {
                opponentTotemText = CreateProfileText(root, "OpponentTotemText", 18f, Vector2.zero, 5);
                opponentTotemText.alignment = TextAlignmentOptions.Center;
            }
            if (opponentTotemText != null)
                opponentTotemText.gameObject.SetActive(false);
        }

        private BattleTileData ResolveOpponentTotemData()
        {
            BattleTileStore store = battleStore != null ? battleStore : BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
            if (store == null)
                return null;

            BattleTileData adaptiveTotem = GetAdaptiveOpponentTotemTile();
            if (adaptiveTotem != null)
                return adaptiveTotem;

            IReadOnlyList<BattleTileData> source = store.GetDefaultTilesForRound(Mathf.Max(1, CurrentRoundNumber));
            if (source == null || source.Count == 0)
                return null;

            int hash = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName)
                ? Mathf.Max(0, MahjongSession.BattleOpponentRankPoints)
                : MahjongSession.BattleOpponentName.GetHashCode();

            int index = Mathf.Abs(hash % source.Count);
            return source[index];
        }

        private static Sprite ResolveBattleTileFaceSprite(BattleTileData data)
        {
            if (data?.Prefab == null)
                return null;

            return data.Prefab.FaceSprite != null ? data.Prefab.FaceSprite : data.Prefab.BackSprite;
        }

        private static string ResolveBattleTileShortName(BattleTileData data)
        {
            if (data == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(data.DisplayName))
                return data.DisplayName.Trim();

            return !string.IsNullOrWhiteSpace(data.Id) ? data.Id.Trim() : string.Empty;
        }

        private static string BattleProfileText(string ru, string en, string tr, string de)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            return language switch
            {
                GameLanguage.Russian => ru,
                GameLanguage.English => en,
                GameLanguage.German => de,
                _ => tr
            };
        }

        private string FormatBattleProfileStats(int wins, int losses, int mvpPercent, int hp, int maxHp)
        {
            string hpText = maxHp > 0 ? $"HP {Mathf.Max(0, hp)}/{maxHp}" : "HP ?";
            return $"{hpText}  W {Mathf.Max(0, wins)}/{Mathf.Max(0, losses)}  M {Mathf.Clamp(mvpPercent, 0, 100)}%";
        }

        private void RefreshPlayerHpBar()
        {
            int hp = ResolveDisplayPlayerHp();
            int maxHp = combatSystem != null ? combatSystem.MaxPlayerHp : 0;
            RefreshPlayerHpBar(hp, maxHp);
        }

        private void RefreshOpponentHpBar()
        {
            int hp = ResolveDisplayOpponentHp();
            int maxHp = combatSystem != null ? combatSystem.MaxOpponentHp : 0;
            RefreshOpponentHpBar(hp, maxHp);
        }

        private void RefreshBattleHpBars()
        {
            RefreshPlayerHpBar();
            RefreshOpponentHpBar();
        }

        private float ResolvePlayerHpNormalized()
        {
            return combatSystem != null && combatSystem.MaxPlayerHp > 0
                ? Mathf.Clamp01((float)Mathf.Max(0, ResolveDisplayPlayerHp()) / combatSystem.MaxPlayerHp)
                : 0f;
        }

        private float ResolveOpponentHpNormalized()
        {
            return combatSystem != null && combatSystem.MaxOpponentHp > 0
                ? Mathf.Clamp01((float)Mathf.Max(0, ResolveDisplayOpponentHp()) / combatSystem.MaxOpponentHp)
                : 0f;
        }

        private int ResolveDisplayPlayerHp()
        {
            if (combatSystem == null)
                return 0;

            return combatSystem.IsCombatStarted
                ? combatSystem.PlayerHp
                : combatSystem.MaxPlayerHp;
        }

        private int ResolveDisplayOpponentHp()
        {
            if (combatSystem == null)
                return 0;

            return combatSystem.IsCombatStarted
                ? combatSystem.OpponentHp
                : combatSystem.MaxOpponentHp;
        }

        private void RefreshPlayerHpBar(int hp, int maxHp)
        {
            if (playerHpBarFill != null)
            {
                float value = maxHp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, hp) / maxHp) : 0f;
                ApplyHpBarFillValue(playerHpBarFill, playerHpBarFillColor, value);
            }

            if (playerHpBarText != null)
                playerHpBarText.text = maxHp > 0 ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP";

            ApplyPlayerProfileUi();
            if (playerBoardFullscreen)
                ApplyFullscreenHpBarLayouts();
        }

        private void RefreshOpponentHpBar(int hp, int maxHp)
        {
            if (opponentHpBarFill != null)
            {
                float value = maxHp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, hp) / maxHp) : 0f;
                ApplyHpBarFillValue(opponentHpBarFill, opponentHpBarFillColor, value);
            }

            if (opponentHpBarText != null)
                opponentHpBarText.text = maxHp > 0 ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP";

            ApplyOpponentProfileUi();
            if (playerBoardFullscreen)
                ApplyFullscreenHpBarLayouts();
        }

        private static void ApplyHpBarFillValue(Image fill, Color color, float value)
        {
            if (fill == null)
                return;

            fill.enabled = true;
            fill.gameObject.SetActive(true);
            fill.raycastTarget = false;
            fill.color = fill.sprite != null ? Color.white : color;

            RectTransform rect = fill.rectTransform;
            RectTransform parent = fill.transform.parent as RectTransform;
            bool vertical = parent != null && parent.rect.height > parent.rect.width * 1.2f;

            if (rect == null)
                return;

            value = Mathf.Clamp01(value);
            if (fill.sprite != null && vertical)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = value;
                fill.preserveAspect = false;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            if (fill.sprite != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = value;
                fill.preserveAspect = false;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            fill.type = Image.Type.Simple;
            if (vertical)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(1f, value);
                rect.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(value, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
            }

            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HideLegacyHpTexts()
        {
            TMP_Text legacy = FindTextByObjectName("PlayerHPText");
            if (legacy == null)
                legacy = FindTextByObjectName("PlayerHpText");

            if (legacy != null)
                legacy.gameObject.SetActive(false);

            legacy = FindTextByObjectName("OpponentHPText");
            if (legacy == null)
                legacy = FindTextByObjectName("OpponentHpText");

            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }

        private void ApplyPlayerBattleSpriteUi()
        {
            if (playerBattleSpriteImage == null)
                return;

            BattleCharacterDatabase.BattleCharacterData data = ResolveSelectedBattleCharacter();
            if (ApplyBattleModel(data, ref playerBattleModelView, playerBattleSpriteImage, false))
                return;

            Sprite battleSprite = ResolveSelectedBattleSprite();
            playerBattleSpriteImage.sprite = battleSprite;
            playerBattleSpriteImage.enabled = battleSprite != null;
            playerBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                playerBattleSpriteImage,
                playerBattleSpriteOffset,
                playerBattleSpriteSize,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));
            ApplyImageFlip(playerBattleSpriteImage, false);
        }

        private bool ApplyBattleModel(
            BattleCharacterDatabase.BattleCharacterData data,
            ref BattleCharacterModelView modelView,
            Image anchorImage,
            bool flipX)
        {
            if (anchorImage == null || data == null)
            {
                if (modelView != null)
                    modelView.Hide();

                return false;
            }

            if (modelView == null)
                modelView = anchorImage.GetComponent<BattleCharacterModelView>();

            if (modelView == null)
                modelView = anchorImage.gameObject.AddComponent<BattleCharacterModelView>();

            modelView.ConfigureBattleRenderFrame(anchorToFeet: true, feetBottomMargin: -0.08f);
            bool shown = modelView.Show(data, BattleCharacterModelView.ModelContext.Battle, flipX);
            if (shown)
            {
                anchorImage.enabled = false;
                anchorImage.raycastTarget = false;
            }

            return shown;
        }

        private BattleCharacterDatabase.BattleCharacterData ResolveSelectedBattleCharacter()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            return BattleCharacterSelectionService.Instance.GetSelectedCharacter();
        }

        private BattleCharacterDatabase.BattleCharacterData ResolveBattleCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !BattleCharacterDatabase.HasInstance)
                return null;

            return BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId);
        }

        private Sprite ResolveSelectedBattleSprite()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            BattleCharacterDatabase.BattleCharacterData selected =
                BattleCharacterSelectionService.Instance.GetSelectedCharacter();

            if (selected == null)
                return null;

            if (selected.BattleSprite != null)
                return selected.BattleSprite;

            if (selected.LobbySprite != null)
                return selected.LobbySprite;

            return selected.SelectSprite;
        }

        private Sprite ResolveSelectedProfileSprite()
        {
            BattleCharacterDatabase.BattleCharacterData selected = ResolveSelectedBattleCharacter();
            return ResolveProfileSprite(selected);
        }

        private Sprite ResolveBattleSprite(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !BattleCharacterDatabase.HasInstance)
                return null;

            BattleCharacterDatabase.BattleCharacterData data =
                BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId);

            if (data == null)
                return null;

            if (data.BattleSprite != null)
                return data.BattleSprite;

            if (data.LobbySprite != null)
                return data.LobbySprite;

            return data.SelectSprite;
        }

        private Sprite ResolveProfileSprite(string characterId)
        {
            BattleCharacterDatabase.BattleCharacterData data = ResolveBattleCharacter(characterId);
            return ResolveProfileSprite(data);
        }

        private static Sprite ResolveProfileSprite(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            if (data.ProfileSprite != null)
                return data.ProfileSprite;

            if (data.LobbySprite != null)
                return data.LobbySprite;

            if (data.SelectSprite != null)
                return data.SelectSprite;

            return data.BattleSprite;
        }

        private void CreatePlayerProfileUi()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            Transform rootTransform = playerNameText != null && playerNameText.transform.parent != null
                ? playerNameText.transform.parent
                : null;

            GameObject root = rootTransform != null
                ? rootTransform.gameObject
                : new GameObject("PlayerProfileHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            root.layer = canvas.gameObject.layer;

            if (rootTransform == null)
                root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect != null && rootTransform == null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = playerProfileUiOffset;
                rect.sizeDelta = playerProfileUiSize;
            }

            Image background = root.GetComponent<Image>();
            if (background != null)
            {
                background.enabled = false;
                background.raycastTarget = false;
            }

            if (playerBattleSpriteImage == null)
            {
                playerBattleSpriteImage = CreateProfileImage(
                    root.transform,
                    "PlayerBattleSprite",
                    playerBattleSpriteOffset,
                    playerBattleSpriteSize,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
            }

            if (playerNameText == null)
                playerNameText = CreateProfileText(root.transform, "PlayerName", 30f, new Vector2(20f, -18f), 1);

            if (playerTitleText == null)
                playerTitleText = CreateProfileText(root.transform, "PlayerTitle", 18f, new Vector2(20f, -54f), 2);

            if (playerRankText == null)
                playerRankText = CreateProfileText(root.transform, "PlayerRank", 20f, new Vector2(20f, -82f), 3);

            if (playerHpBarFill == null || playerHpBarText == null)
                CreatePlayerHpBar(root.transform);

            EnsureProfileCardPortraitUi(root.transform, ref playerCardPortraitImage, "PlayerCardPortrait");
            EnsurePlayerTotemUi(root.transform);
        }

        private void CreateOpponentProfileUi()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            GameObject root = new GameObject("OpponentProfileHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.layer = canvas.gameObject.layer;
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = opponentProfileUiOffset;
            rect.sizeDelta = opponentProfileUiSize;

            Image background = root.GetComponent<Image>();
            if (background != null)
            {
                background.enabled = false;
                background.raycastTarget = false;
            }

            if (opponentBattleSpriteImage == null)
            {
                opponentBattleSpriteImage = CreateProfileImage(
                    root.transform,
                    "OpponentBattleSprite",
                    opponentBattleSpriteOffset,
                    opponentBattleSpriteSize,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));
            }

            if (opponentNameText == null)
                opponentNameText = CreateProfileText(root.transform, "OpponentName", 30f, new Vector2(20f, -18f), 1);

            if (opponentRankText == null)
                opponentRankText = CreateProfileText(root.transform, "OpponentRank", 22f, new Vector2(20f, -58f), 2);

            if (opponentStatsText == null)
                opponentStatsText = CreateProfileText(root.transform, "OpponentStats", 18f, new Vector2(20f, -90f), 3);

            if (opponentHpBarFill == null || opponentHpBarText == null)
                CreateOpponentHpBar(root.transform);

            EnsureProfileCardPortraitUi(root.transform, ref opponentCardPortraitImage, "OpponentCardPortrait");
            EnsureOpponentTotemUi(root.transform);
        }

        private void ApplyBattleProfilePanelBackground(Transform root, bool flipX)
        {
            if (root == null)
                return;

            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = false;
                rootImage.raycastTarget = false;
            }

            Transform backgroundTransform = root.Find("BattleProfilePanelBackground");
            Image background = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
            if (background == null)
            {
                GameObject backgroundObject = new GameObject(
                    "BattleProfilePanelBackground",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                backgroundObject.layer = root.gameObject.layer;
                backgroundObject.transform.SetParent(root, false);
                background = backgroundObject.GetComponent<Image>();
            }

            RectTransform rect = background.rectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            background.transform.SetSiblingIndex(0);

            ApplyBattleProfilePanelBackground(background);
        }

        private void ApplyBattleProfilePanelBackground(Image background)
        {
            if (background == null)
                return;

			// This is a dedicated wide HUD frame. Reusing popup or button sprites here
			// makes their borders overlap and leaves the profile looking like a crop.
			Sprite compactProfileFrame = Resources.Load<Sprite>(BattleProfileFrameResourcePath);
			if (compactProfileFrame != null)
            {
				background.sprite = compactProfileFrame;
				background.type = Image.Type.Simple;
                background.color = Color.white;
                background.raycastTarget = false;
                background.preserveAspect = false;
                background.enabled = true;
                return;
            }

            Sprite sprite = ResolveBattleProfilePanelSprite();
            background.sprite = sprite;
            background.color = sprite != null ? battleProfilePanelColor : new Color(0f, 0f, 0f, 0.42f);
            background.type = sprite != null ? battleProfilePanelImageType : Image.Type.Simple;
            background.raycastTarget = battleProfilePanelRaycastTarget;
            background.preserveAspect = false;
            background.enabled = true;
        }

        private Sprite ResolveBattleProfilePanelSprite()
        {
            if (battleProfilePanelSprite != null &&
                battleProfilePanelSprite.name.IndexOf("FlagPanelBattleProfileCard", StringComparison.Ordinal) < 0)
            {
                return battleProfilePanelSprite;
            }

            battleProfilePanelSprite = null;

            if (string.Equals(battleProfilePanelSpriteResourcePath, LegacyBattleProfileFlagPanelResourcePath, StringComparison.Ordinal))
                battleProfilePanelSpriteResourcePath = BattleProfileInfoPanelResourcePath;

            if (string.IsNullOrWhiteSpace(battleProfilePanelSpriteResourcePath))
                return null;

            Sprite[] sprites = Resources.LoadAll<Sprite>(battleProfilePanelSpriteResourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                battleProfilePanelSprite = sprites[0];
                return battleProfilePanelSprite;
            }

            battleProfilePanelSprite = Resources.Load<Sprite>(battleProfilePanelSpriteResourcePath);
            return battleProfilePanelSprite;
        }

        private Image CreateProfileImage(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            imageObject.transform.SetSiblingIndex(0);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;

            return image;
        }

        private void CreatePlayerHpBar(Transform parent)
        {
            if (playerHpBarFill != null)
            {
                if (playerHpBarText == null)
                {
                    playerHpBarText = CreateProfileText(parent, "PlayerHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                    playerHpBarText.alignment = TextAlignmentOptions.Center;
                    playerHpBarText.rectTransform.sizeDelta = playerHpBarSize;
                }

                RefreshPlayerHpBar();
                return;
            }

            GameObject barRoot = new GameObject("PlayerHpBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barRoot.layer = parent.gameObject.layer;
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.SetSiblingIndex(3);

            RectTransform rootRect = barRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = playerHpBarOffset;
            rootRect.sizeDelta = playerHpBarSize;

            Image background = barRoot.GetComponent<Image>();
            background.color = playerHpBarBackgroundColor;
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("PlayerHpBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = parent.gameObject.layer;
            fillObject.transform.SetParent(barRoot.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            playerHpBarFill = fillObject.GetComponent<Image>();
            playerHpBarFill.color = playerHpBarFillColor;
            playerHpBarFill.raycastTarget = false;
            playerHpBarFill.type = Image.Type.Simple;

            if (playerHpBarText == null)
            {
                playerHpBarText = CreateProfileText(parent, "PlayerHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                playerHpBarText.alignment = TextAlignmentOptions.Center;
                playerHpBarText.rectTransform.sizeDelta = playerHpBarSize;
            }

            RefreshPlayerHpBar();
        }

        private void CreateOpponentHpBar(Transform parent)
        {
            if (opponentHpBarFill != null)
            {
                if (opponentHpBarText == null)
                {
                    opponentHpBarText = CreateProfileText(parent, "OpponentHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                    opponentHpBarText.alignment = TextAlignmentOptions.Center;
                    opponentHpBarText.rectTransform.sizeDelta = playerHpBarSize;
                }

                RefreshOpponentHpBar();
                return;
            }

            GameObject barRoot = new GameObject("OpponentHpBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barRoot.layer = parent.gameObject.layer;
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.SetSiblingIndex(3);

            RectTransform rootRect = barRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = playerHpBarOffset;
            rootRect.sizeDelta = playerHpBarSize;

            Image background = barRoot.GetComponent<Image>();
            background.color = playerHpBarBackgroundColor;
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("OpponentHpBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = parent.gameObject.layer;
            fillObject.transform.SetParent(barRoot.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            opponentHpBarFill = fillObject.GetComponent<Image>();
            opponentHpBarFill.color = opponentHpBarFillColor;
            opponentHpBarFill.raycastTarget = false;
            opponentHpBarFill.type = Image.Type.Simple;

            if (opponentHpBarText == null)
            {
                opponentHpBarText = CreateProfileText(parent, "OpponentHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                opponentHpBarText.alignment = TextAlignmentOptions.Center;
                opponentHpBarText.rectTransform.sizeDelta = playerHpBarSize;
            }

            RefreshOpponentHpBar();
        }

        private void ApplyHpBarSpriteVisuals(Image fill, bool playerSide)
        {
            if (fill == null)
                return;

            Sprite sprite = ResolveHpBarSprite(playerSide);
            if (sprite == null)
                return;

            RectTransform rootRect = fill.transform.parent as RectTransform;
            if (rootRect != null && rootRect.rect.width > rootRect.rect.height * 1.2f)
            {
                rootRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
                rootRect.localScale = new Vector3(1f, playerSide ? 1f : -1f, 1f);
                rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.y, rootRect.sizeDelta.x);
            }

            fill.sprite = sprite;
            fill.color = Color.white;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.preserveAspect = false;
            fill.raycastTarget = false;

            Image background = fill.transform.parent != null ? fill.transform.parent.GetComponent<Image>() : null;
            if (background != null)
            {
                background.sprite = sprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
                background.color = new Color(1f, 1f, 1f, 0.08f);
                background.raycastTarget = false;
            }
        }

        private Sprite ResolveHpBarSprite(bool playerSide)
        {
            if (playerSide)
            {
                if (playerHpBarSprite == null)
                    playerHpBarSprite = CreateHpBarSprite(PlayerHpBarSpriteRect, "BattlePlayerHpBar");

                return playerHpBarSprite;
            }

            if (opponentHpBarSprite == null)
                opponentHpBarSprite = CreateHpBarSprite(OpponentHpBarSpriteRect, "BattleOpponentHpBar");

            return opponentHpBarSprite;
        }

        private Sprite CreateHpBarSprite(Rect rect, string spriteName)
        {
            Texture2D texture = hpBarsTexture;
            if (texture == null && !string.IsNullOrWhiteSpace(hpBarsTextureResourcePath))
            {
                texture = Resources.Load<Texture2D>(hpBarsTextureResourcePath);
                if (texture == null)
                {
                    Sprite sourceSprite = Resources.Load<Sprite>(hpBarsTextureResourcePath);
                    texture = sourceSprite != null ? sourceSprite.texture : null;
                }

                hpBarsTexture = texture;
            }

            if (texture == null)
                return null;

            Rect clamped = ClampRectToTexture(rect, texture);
            Sprite sprite = Sprite.Create(texture, clamped, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static Rect ClampRectToTexture(Rect rect, Texture2D texture)
        {
            if (texture == null)
                return rect;

            float x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, texture.width - 1f));
            float y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, texture.height - 1f));
            float width = Mathf.Clamp(rect.width, 1f, texture.width - x);
            float height = Mathf.Clamp(rect.height, 1f, texture.height - y);
            return new Rect(x, y, width, height);
        }

        private void ApplyBattleProfileLayout()
        {
            Transform playerRoot = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerHpBarFill, playerHpBarText, playerCardPortraitImage, playerTotemImage);
            Transform opponentRoot = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText, opponentStatsText, opponentHpBarFill, opponentHpBarText, opponentCardPortraitImage, opponentTotemImage);
            EnsureProfileContentParented(playerRoot, playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerCardPortraitImage, playerTotemImage, playerTotemText);
            EnsureProfileContentParented(opponentRoot, opponentBattleSpriteImage, opponentNameText, opponentStatsText, opponentRankText, opponentCardPortraitImage, opponentTotemImage, opponentTotemText);
            ApplyBattleProfilePanelBackground(playerRoot, true);
            ApplyBattleProfilePanelBackground(opponentRoot, false);
            if (playerRoot != null)
            {
                EnsureProfileCardPortraitUi(playerRoot, ref playerCardPortraitImage, "PlayerCardPortrait");
                EnsurePlayerTotemUi(playerRoot);
            }
            if (opponentRoot != null)
            {
                EnsureProfileCardPortraitUi(opponentRoot, ref opponentCardPortraitImage, "OpponentCardPortrait");
                EnsureOpponentTotemUi(opponentRoot);
            }
            CleanupOrphanProfilePortraitFrames();

            Vector2 canvasSize = ResolveBattleHudCanvasSize();
            lastBattleHudCanvasSize = canvasSize;

            float width = Mathf.Max(1f, canvasSize.x);
            float height = Mathf.Max(1f, canvasSize.y);
            bool compact = width < 900f || height < 720f;
            bool narrow = width < 640f;
            float canvasAspect = width / height;
            bool tabletLikeLayout = canvasAspect < 1.62f && width >= 900f;

            float topSpaceHeight = ResolveFreeHeightAboveBoards(canvasSize);
            float panelGap = compact ? 18f : 24f;
            float portraitMaxByWidth = Mathf.Clamp(width * (narrow ? 0.22f : 0.13f), compact ? 112f : 132f, compact ? 170f : 210f);
            float portraitSize = Mathf.Clamp(topSpaceHeight * 0.58f, compact ? 88f : 110f, portraitMaxByWidth);
            float fighterGap = Mathf.Clamp(portraitSize * 0.95f, compact ? 92f : 112f, compact ? 150f : 178f);
            float rootTop = -Mathf.Clamp(topSpaceHeight * 0.08f, compact ? 10f : 14f, compact ? 24f : 32f);
            float profileWidth = Mathf.Clamp(width * (narrow ? 0.32f : 0.22f), narrow ? 150f : 220f, compact ? 320f : 420f);
            float profileHeight = Mathf.Max(portraitSize + 18f, compact ? 132f : 158f);
            float rootWidth = profileWidth + panelGap + portraitSize * 0.5f;
            Vector2 profileSize = new Vector2(rootWidth, profileHeight);
            Vector2 opponentProfileSize = profileSize;
            Vector2 portraitSizeDelta = new Vector2(portraitSize, portraitSize);
            Vector2 playerRootOffset = new Vector2(-fighterGap * 0.5f, rootTop);
            Vector2 opponentRootOffset = new Vector2(fighterGap * 0.5f, rootTop);
            Vector2 playerPortraitOffset = new Vector2(portraitSize * 0.22f, compact ? -8f : -12f);
            Vector2 opponentPortraitOffset = new Vector2(-portraitSize * 0.22f, compact ? -8f : -12f);
            Vector2 playerPortraitAnchor = new Vector2(1f, 1f);
            Vector2 opponentPortraitAnchor = new Vector2(0f, 1f);
            Vector2 portraitPivot = new Vector2(0.5f, 1f);
            float textTop = compact ? -34f : -42f;
            float rankTop = compact ? -68f : -82f;
            Vector2 profileTextSize = new Vector2(Mathf.Max(120f, profileWidth - 28f), compact ? 26f : 30f);
            Vector2 rankTextSize = new Vector2(Mathf.Max(120f, profileWidth - 28f), compact ? 24f : 28f);
            Vector2 profileTextAnchor = new Vector2(0.5f, 0.62f);
            float profileTextSpacing = compact ? 22f : 26f;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null &&
                TryResolveBoardBoundsInCanvas(playerBoard, canvas, canvasRect, out Rect playerBoardBounds) &&
                TryResolveBoardBoundsInCanvas(opponentBoard, canvas, canvasRect, out Rect opponentBoardBounds))
            {
                float canvasTop = canvasRect.rect.yMax;
                float boardTop = Mathf.Min(playerBoardBounds.yMax, opponentBoardBounds.yMax);
                float topBandHeight = Mathf.Max(1f, canvasTop - boardTop);
                float playerBoardWidth = Mathf.Max(1f, playerBoardBounds.width);
                float opponentBoardWidth = Mathf.Max(1f, opponentBoardBounds.width);
                float aspect = width / height;
                bool tabletLike = tabletLikeLayout;
                float dynamicMinHeight = compact ? 106f : tabletLike ? 118f : 142f;
                float dynamicMaxHeight = compact ? 142f : tabletLike ? 158f : 196f;
                float dynamicHeight = Mathf.Clamp(topBandHeight, dynamicMinHeight, dynamicMaxHeight);
                float flagWidthFactor = aspect > 1.75f ? 0.72f : tabletLike ? 0.70f : 0.82f;
                float dynamicPlayerWidth = Mathf.Clamp(playerBoardWidth * flagWidthFactor, compact ? 220f : 250f, playerBoardWidth * 0.9f);
                float dynamicOpponentWidth = Mathf.Clamp(opponentBoardWidth * flagWidthFactor, compact ? 220f : 250f, opponentBoardWidth * 0.9f);
                float playerFlagSideOffset = dynamicPlayerWidth * (tabletLike ? 0.08f : 0.14f);
                float opponentFlagSideOffset = dynamicOpponentWidth * (tabletLike ? 0.08f : 0.14f);

                profileSize = new Vector2(dynamicPlayerWidth, dynamicHeight);
                opponentProfileSize = new Vector2(dynamicOpponentWidth, dynamicHeight);
                float playerPanelSideInset = Mathf.Clamp(playerBoardWidth * (tabletLike ? 0.02f : 0.03f), 8f, 24f);
                float opponentPanelSideInset = Mathf.Clamp(opponentBoardWidth * (tabletLike ? 0.02f : 0.03f), 8f, 24f);
                playerRootOffset = new Vector2(playerBoardBounds.xMin + playerPanelSideInset, canvasTop);
                opponentRootOffset = new Vector2(opponentBoardBounds.xMax - dynamicOpponentWidth - opponentPanelSideInset, canvasTop);
                float characterAvailableHeight = Mathf.Max(80f, topBandHeight);
                float characterHeight = Mathf.Clamp(
                    characterAvailableHeight * (tabletLike ? 0.94f : 0.96f),
                    compact ? 82f : 96f,
                    characterAvailableHeight);
                float characterWidth = Mathf.Clamp(
                    characterHeight * (tabletLike ? 1.16f : 1.04f),
                    compact ? 82f : 96f,
                    Mathf.Min(dynamicPlayerWidth, dynamicOpponentWidth) * (aspect > 1.75f ? 0.46f : 0.44f));
                portraitSize = characterHeight;
                portraitSizeDelta = new Vector2(characterWidth, characterHeight);
                float standOnBoardOffset = dynamicHeight - topBandHeight;
                float fighterEdgeInset = characterHeight * (tabletLike ? 0.22f : 0.28f);
                float playerFighterX = playerBoardBounds.xMax - fighterEdgeInset;
                float opponentFighterX = opponentBoardBounds.xMin + fighterEdgeInset;
                playerPortraitOffset = new Vector2(playerFighterX - (playerRootOffset.x + dynamicPlayerWidth), standOnBoardOffset);
                opponentPortraitOffset = new Vector2(opponentFighterX - opponentRootOffset.x, standOnBoardOffset);
                playerPortraitAnchor = new Vector2(1f, 0f);
                opponentPortraitAnchor = new Vector2(0f, 0f);
                portraitPivot = new Vector2(0.5f, 0f);
                textTop = -dynamicHeight * 0.38f;
                rankTop = -dynamicHeight * 0.5f;
                float textWidth = Mathf.Clamp(Mathf.Min(dynamicPlayerWidth, dynamicOpponentWidth) - (tabletLike ? 104f : 128f), compact ? 190f : 220f, Mathf.Min(dynamicPlayerWidth, dynamicOpponentWidth) - (tabletLike ? 72f : 92f));
                profileTextSize = new Vector2(textWidth, compact || tabletLike ? 28f : 38f);
                rankTextSize = new Vector2(textWidth, compact || tabletLike ? 24f : 34f);
                profileTextAnchor = new Vector2(0.5f, 0.58f);
                profileTextSpacing = Mathf.Clamp(dynamicHeight * (tabletLike ? 0.18f : 0.22f), compact ? 24f : 28f, compact || tabletLike ? 34f : 48f);
                rootTop = canvasTop;
                fighterGap = Mathf.Abs(opponentRootOffset.x - playerRootOffset.x);

                ApplyProfileRootLayout(
                    playerRoot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 1f),
                    playerRootOffset,
                    profileSize);

                ApplyProfileRootLayout(
                    opponentRoot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 1f),
                    opponentRootOffset,
                    opponentProfileSize);
            }
            else
            {
                ApplyProfileRootLayout(
                    playerRoot,
                    new Vector2(0.5f, 1f),
                    new Vector2(1f, 1f),
                    playerRootOffset,
                    profileSize);

                ApplyProfileRootLayout(
                    opponentRoot,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, 1f),
                    opponentRootOffset,
                    profileSize);
            }

            ApplyImageLayout(playerBattleSpriteImage, playerPortraitOffset, portraitSizeDelta, playerPortraitAnchor, portraitPivot);
            ApplyImageLayout(opponentBattleSpriteImage, opponentPortraitOffset, portraitSizeDelta, opponentPortraitAnchor, portraitPivot);

            Vector2 textAnchor = new Vector2(0.5f, 0.55f);
            Vector2 playerTextOffset = new Vector2(-8f, 0f);
            Vector2 opponentTextOffset = new Vector2(8f, 0f);
            float cardPortraitHeight = Mathf.Clamp(profileSize.y * 0.70f, compact ? 76f : 90f, compact ? 102f : 126f);
            Vector2 cardPortraitSize = new Vector2(cardPortraitHeight * 0.86f, cardPortraitHeight);
            Vector2 playerCardPortraitOffset = new Vector2(profileSize.x * 0.5f - cardPortraitSize.x * 0.5f - (compact ? 16f : 22f), profileSize.y * 0.02f);
            Vector2 opponentCardPortraitOffset = new Vector2(-opponentProfileSize.x * 0.5f + cardPortraitSize.x * 0.5f + (compact ? 16f : 22f), opponentProfileSize.y * 0.02f);
            float totemHeight = Mathf.Clamp(profileSize.y * 0.44f, compact ? 50f : 60f, compact ? 72f : 86f);
            Vector2 totemSize = new Vector2(totemHeight * 0.74f, totemHeight);
            float portraitTotemGap = compact ? 10f : 14f;
            Vector2 totemOffset = new Vector2(playerCardPortraitOffset.x - cardPortraitSize.x * 0.5f - totemSize.x * 0.5f - portraitTotemGap, -profileSize.y * 0.05f);
            Vector2 opponentTotemOffset = new Vector2(opponentCardPortraitOffset.x + cardPortraitSize.x * 0.5f + totemSize.x * 0.5f + portraitTotemGap, -opponentProfileSize.y * 0.05f);
            Vector2 totemLabelSize = new Vector2(Mathf.Clamp(totemSize.x + 44f, 86f, 138f), compact ? 22f : 26f);
            float sharedProfileHeight = Mathf.Min(profileSize.y, opponentProfileSize.y);
            Vector2 iconSize = Vector2.one * Mathf.Clamp(sharedProfileHeight * 0.72f, compact ? 92f : 108f, compact ? 124f : 148f);
            float iconInset = compact ? 28f : 36f;
            float iconCenterFromEdge = iconInset + iconSize.x * 0.5f;
            float iconVerticalOffset = -sharedProfileHeight * 0.01f;
            Vector2 playerIconOffset = new Vector2(-profileSize.x * 0.5f + iconCenterFromEdge, iconVerticalOffset);
            Vector2 opponentIconOffset = new Vector2(opponentProfileSize.x * 0.5f - iconCenterFromEdge, iconVerticalOffset);
            float textLaneWidth = Mathf.Clamp(
                Mathf.Min(profileSize.x, opponentProfileSize.x) - iconSize.x * 0.76f - cardPortraitSize.x - totemSize.x - (compact ? 42f : 56f),
                compact ? 132f : 150f,
                compact ? 210f : 240f);
            profileTextSize = new Vector2(textLaneWidth, profileTextSize.y);
            rankTextSize = new Vector2(textLaneWidth, rankTextSize.y);
            ApplyBattleProfileRankIconLayout(playerRankIconImage, textAnchor, playerIconOffset, iconSize);
            ApplyBattleProfileRankIconLayout(opponentRankIconImage, textAnchor, opponentIconOffset, iconSize);
            ApplyBattleProfileTotemLayout(playerCardPortraitImage, textAnchor, playerCardPortraitOffset, cardPortraitSize);
            ApplyBattleProfileTotemLayout(opponentCardPortraitImage, textAnchor, opponentCardPortraitOffset, cardPortraitSize);
            ApplyBattleProfileTotemLayout(playerTotemImage, textAnchor, totemOffset, totemSize);
            ApplyBattleProfileTotemLayout(opponentTotemImage, textAnchor, opponentTotemOffset, totemSize);
            RefreshPlayerTotemUpgradeVisual(playerRoot);
            SetProfileTextVisible(playerTotemText, false);
            SetProfileTextVisible(opponentTotemText, false);
            HideProfileTotemTextArtifacts();
            ApplyFlagProfileTextLayout(playerNameText, textAnchor, playerTextOffset + new Vector2(0f, profileTextSpacing), profileTextSize, tabletLikeLayout ? 24f : compact ? 30f : 34f);
            ApplyFlagProfileTextLayout(playerTitleText, textAnchor, playerTextOffset, rankTextSize, tabletLikeLayout ? 18f : compact ? 22f : 25f);
            ApplyFlagProfileTextLayout(playerRankText, textAnchor, playerTextOffset + new Vector2(0f, -profileTextSpacing), rankTextSize, tabletLikeLayout ? 19f : compact ? 23f : 26f);
            ApplyFlagProfileTextLayout(opponentNameText, textAnchor, opponentTextOffset + new Vector2(0f, profileTextSpacing), profileTextSize, tabletLikeLayout ? 24f : compact ? 30f : 34f);
            ApplyFlagProfileTextLayout(opponentStatsText, textAnchor, opponentTextOffset, rankTextSize, tabletLikeLayout ? 18f : compact ? 22f : 25f);
            ApplyFlagProfileTextLayout(opponentRankText, textAnchor, opponentTextOffset + new Vector2(0f, -profileTextSpacing), rankTextSize, tabletLikeLayout ? 19f : compact ? 23f : 26f);
            BringBattleProfileContentToFront(playerRoot);
            BringBattleProfileContentToFront(opponentRoot);
            BringBattleFightersAboveBoards(playerRoot, opponentRoot);

            ApplyBoardHpBarLayouts(canvasSize, compact);
            ApplyRoundHudLayout(canvasSize, compact, fighterGap, rootTop, portraitSize);
        }

        private float ResolveFreeHeightAboveBoards(Vector2 canvasSize)
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvas == null || canvasRect == null)
                return Mathf.Clamp(canvasSize.y * 0.26f, 150f, 260f);

            bool resolvedAny = false;
            float boardTop = float.PositiveInfinity;
            resolvedAny |= TryResolveBoardTopInCanvas(playerBoard, canvas, canvasRect, out float playerTop);
            if (resolvedAny)
                boardTop = Mathf.Min(boardTop, playerTop);

            bool opponentResolved = TryResolveBoardTopInCanvas(opponentBoard, canvas, canvasRect, out float opponentTop);
            if (opponentResolved)
            {
                resolvedAny = true;
                boardTop = Mathf.Min(boardTop, opponentTop);
            }

            if (!resolvedAny)
                return Mathf.Clamp(canvasSize.y * 0.26f, 150f, 260f);

            float canvasTop = canvasRect.rect.height * 0.5f;
            return Mathf.Clamp(canvasTop - boardTop, 150f, Mathf.Max(170f, canvasSize.y * 0.34f));
        }

        private bool TryResolveBoardTopInCanvas(BattleBoard board, Canvas canvas, RectTransform canvasRect, out float top)
        {
            top = 0f;

            RectTransform boardArea = board != null ? board.BoardArea : null;
            if (boardArea == null || canvas == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[1]), camera, out Vector2 topLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[2]), camera, out Vector2 topRight))
            {
                return false;
            }

            top = Mathf.Max(topLeft.y, topRight.y);
            return true;
        }

        private bool TryResolveBoardBoundsInCanvas(BattleBoard board, Canvas canvas, RectTransform canvasRect, out Rect bounds)
        {
            bounds = default;

            RectTransform boardArea = board != null ? board.BoardArea : null;
            if (boardArea == null || canvas == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            bool resolvedAny = false;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out Vector2 localPoint))
                    continue;

                resolvedAny = true;
                minX = Mathf.Min(minX, localPoint.x);
                maxX = Mathf.Max(maxX, localPoint.x);
                minY = Mathf.Min(minY, localPoint.y);
                maxY = Mathf.Max(maxY, localPoint.y);
            }

            if (!resolvedAny)
                return false;

            bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private Transform ResolveProfileRoot(params Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.transform == null)
                    continue;

                if (component.transform.parent != null)
                {
                    if (component.transform.parent.GetComponent<Canvas>() != null)
                        continue;

                    return component.transform.parent;
                }
            }

            return null;
        }

        private void EnsureProfileContentParented(Transform root, params Component[] components)
        {
            if (root == null || components == null)
                return;

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.transform == null || component.transform == root)
                    continue;

                if (component.transform.parent != root)
                    component.transform.SetParent(root, false);

                component.gameObject.layer = root.gameObject.layer;
            }
        }

        private void ApplyProfileRootLayout(
            Transform root,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 offset,
            Vector2 size)
        {
            if (root == null)
                return;

            RectTransform rect = root as RectTransform;
            if (rect == null)
                rect = root.GetComponent<RectTransform>();

            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        private void ApplyProfileTextLayout(
            TMP_Text text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TextAlignmentOptions alignment)
        {
            ApplyProfileTextLayout(text, anchoredPosition, sizeDelta, alignment, null);
        }

        private void ApplyProfileTextLayout(
            TMP_Text text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TextAlignmentOptions alignment,
            float? fontSize)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            if (fontSize.HasValue)
                text.fontSize = fontSize.Value;
        }

        private void ApplyCenteredProfileTextLayout(
            TMP_Text text,
            Vector2 offsetFromCenter,
            Vector2 sizeDelta,
            float fontSize)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offsetFromCenter;
            rect.sizeDelta = new Vector2(Mathf.Abs(sizeDelta.x), Mathf.Abs(sizeDelta.y));
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.fontSize = fontSize;
        }

        private void RefreshBattleProfileRankIcon(
            Transform root,
            ref Image image,
            string rankTier,
            int rankPoints,
            string objectName)
        {
            if (root == null)
                return;

            if (image == null || image.transform == null || image.transform.parent != root)
            {
                Transform existing = root.Find(objectName);
                image = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (image == null)
            {
                GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.layer = root.gameObject.layer;
                iconObject.transform.SetParent(root, false);
                image = iconObject.GetComponent<Image>();
            }

            RankedLeagueId leagueId = RankedLeagueVisuals.ResolveLeagueId(rankTier, rankPoints);
            Sprite sprite = RankedLeagueVisuals.LoadLeagueIcon(leagueId);
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(sprite != null);
        }

        private static void ApplyBattleProfileRankIconLayout(
            Image image,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (image == null || image.rectTransform == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            image.transform.SetAsLastSibling();
        }

        private static void ApplyBattleProfileTotemLayout(
            Image image,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (image == null || image.rectTransform == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            image.transform.SetAsLastSibling();
        }

        private void ApplyFlagProfileTextLayout(
            TMP_Text text,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(Mathf.Abs(sizeDelta.x), Mathf.Abs(sizeDelta.y));
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.fontSize = fontSize;
            text.raycastTarget = false;
            BattlePopupStyle.ApplyText(text, true);
        }

        private void ApplyPlayerHpBarLayout()
        {
            ApplyPlayerHpBarLayout(playerHpBarOffset, playerHpBarSize);
        }

        private void ApplyPlayerHpBarLayout(Vector2 offset, Vector2 size)
        {
            RectTransform barRoot = playerHpBarFill != null && playerHpBarFill.transform.parent != null
                ? playerHpBarFill.transform.parent as RectTransform
                : null;

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(true);
                barRoot.anchorMin = new Vector2(0.5f, 0.5f);
                barRoot.anchorMax = new Vector2(0.5f, 0.5f);
                barRoot.pivot = new Vector2(0.5f, 0.5f);
                barRoot.anchoredPosition = offset;
                barRoot.sizeDelta = size;
                barRoot.localRotation = Quaternion.identity;
                barRoot.localScale = Vector3.one;
                barRoot.SetAsLastSibling();

                Image background = barRoot.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;
            }

            ApplyHpBarSpriteVisuals(playerHpBarFill, true);

            if (playerHpBarFill != null)
            {
                ApplyHpBarFillValue(playerHpBarFill, playerHpBarFillColor, ResolvePlayerHpNormalized());
            }

            if (playerHpBarText != null && playerHpBarText.rectTransform != null)
                ApplyHpBarTextLayout(playerHpBarText, offset, size);
        }

        private void ApplyOpponentHpBarLayout(Vector2 offset, Vector2 size)
        {
            RectTransform barRoot = opponentHpBarFill != null && opponentHpBarFill.transform.parent != null
                ? opponentHpBarFill.transform.parent as RectTransform
                : null;

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(true);
                barRoot.anchorMin = new Vector2(0.5f, 0.5f);
                barRoot.anchorMax = new Vector2(0.5f, 0.5f);
                barRoot.pivot = new Vector2(0.5f, 0.5f);
                barRoot.anchoredPosition = offset;
                barRoot.sizeDelta = size;
                barRoot.localRotation = Quaternion.identity;
                barRoot.localScale = Vector3.one;
                barRoot.SetAsLastSibling();

                Image background = barRoot.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;
            }

            ApplyHpBarSpriteVisuals(opponentHpBarFill, false);

            if (opponentHpBarFill != null)
            {
                ApplyHpBarFillValue(opponentHpBarFill, opponentHpBarFillColor, ResolveOpponentHpNormalized());
            }

            if (opponentHpBarText != null && opponentHpBarText.rectTransform != null)
                ApplyHpBarTextLayout(opponentHpBarText, offset, size);
        }

        private void ApplyBoardHpBarLayouts(Vector2 canvasSize, bool compact)
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            Vector2 fallbackSize = new Vector2(Mathf.Clamp(canvasSize.x * 0.2f, 220f, compact ? 360f : 460f), compact ? 60f : 72f);
            float fallbackY = canvasSize.y * 0.22f;

            Vector2 playerOffset;
            Vector2 playerSize;
            if (!TryResolveBoardHpBarLayout(playerBoard, canvas, compact, true, out playerOffset, out playerSize))
            {
                playerOffset = new Vector2(-canvasSize.x * 0.24f, fallbackY);
                playerSize = fallbackSize;
            }

            Vector2 opponentOffset;
            Vector2 opponentSize;
            if (!TryResolveBoardHpBarLayout(opponentBoard, canvas, compact, false, out opponentOffset, out opponentSize))
            {
                opponentOffset = new Vector2(canvasSize.x * 0.24f, fallbackY);
                opponentSize = fallbackSize;
            }

            ReparentHpBarToCanvas(playerHpBarFill, playerHpBarText, canvas);
            ReparentHpBarToCanvas(opponentHpBarFill, opponentHpBarText, canvas);
            ApplyPlayerHpBarLayout(playerOffset, playerSize);
            ApplyOpponentHpBarLayout(opponentOffset, opponentSize);
        }

        private bool TryResolveBoardHpBarLayout(BattleBoard board, Canvas canvas, bool compact, bool playerSide, out Vector2 offset, out Vector2 size)
        {
            offset = Vector2.zero;
            size = Vector2.zero;

            RectTransform boardArea = board != null ? board.BoardArea : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (boardArea == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 bottomLeft;
            Vector2 topLeft;
            Vector2 topRight;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[0]), camera, out bottomLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[1]), camera, out topLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[2]), camera, out topRight))
            {
                return false;
            }

            float boardHeight = Mathf.Abs(topLeft.y - bottomLeft.y);
            float boardWidth = Mathf.Abs(topRight.x - topLeft.x);
            float edgeInset = Mathf.Clamp(boardHpBarInnerInset * 0.45f, 4f, Mathf.Max(4f, boardWidth * 0.04f));
            bool useVerticalBar = !compact && boardHeight > boardWidth * 1.15f;
            if (useVerticalBar)
            {
                float verticalBarWidth = Mathf.Clamp(verticalBoardHpBarWidth, 24f, Mathf.Max(24f, boardWidth * 0.22f));
                float verticalBarHeight = Mathf.Clamp(boardHeight * verticalBoardHpBarHeightFactor, 120f, Mathf.Max(120f, boardHeight - edgeInset * 2f));
                float x = playerSide ? topLeft.x + edgeInset + verticalBarWidth * 0.5f : topRight.x - edgeInset - verticalBarWidth * 0.5f;
                float y = (topLeft.y + bottomLeft.y) * 0.5f;
                offset = new Vector2(x, y);
                size = new Vector2(verticalBarWidth, verticalBarHeight);
                return true;
            }

            float barHeight = compact ? Mathf.Max(60f, horizontalBoardHpBarHeight - 8f) : Mathf.Max(72f, horizontalBoardHpBarHeight);
            barHeight = Mathf.Min(barHeight, Mathf.Max(36f, boardHeight * 0.24f));
            float barWidth = Mathf.Clamp(boardWidth * 0.46f, compact ? 220f : 280f, Mathf.Max(160f, boardWidth - edgeInset * 2f));
            float centerX = (topLeft.x + topRight.x) * 0.5f;
            float topY = topLeft.y - barHeight * 0.08f;
            offset = new Vector2(centerX, topY);
            size = new Vector2(barWidth, barHeight);
            return true;
        }

        private void ReparentHpBarToCanvas(Image fill, TMP_Text text, Canvas canvas)
        {
            if (canvas == null)
                return;

            Transform canvasTransform = canvas.transform;
            RectTransform barRoot = fill != null && fill.transform.parent != null
                ? fill.transform.parent as RectTransform
                : null;

            if (barRoot != null && barRoot.parent != canvasTransform)
                barRoot.SetParent(canvasTransform, false);

            if (text != null && text.transform.parent != canvasTransform)
                text.transform.SetParent(canvasTransform, false);

            if (barRoot != null)
                barRoot.SetAsLastSibling();
            if (text != null)
                text.transform.SetAsLastSibling();
        }

        private void ApplyHpBarTextLayout(TMP_Text text, Vector2 offset, Vector2 size)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform textRect = text.rectTransform;
            text.gameObject.SetActive(true);
            if (size.y > size.x * 2f)
            {
                text.gameObject.SetActive(false);
                return;
            }

            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = offset;
            textRect.sizeDelta = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private void ShowFloatingDamageText(BattleBoardSide targetSide, BattleDamageCalculator.DamageResult result)
        {
            if (!useFloatingDamageText || result.FinalDamage <= 0)
                return;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            TMP_Text text = CreateFloatingDamageText(canvasRect, targetSide, result);
            if (text == null)
                return;

            activeDamageTexts.Add(text);
            StartCoroutine(AnimateFloatingDamageText(text, targetSide, result.IsCritical, tutorialDamageTextEmphasis));
        }

        private TMP_Text CreateFloatingDamageText(RectTransform canvasRect, BattleBoardSide targetSide, BattleDamageCalculator.DamageResult result)
        {
            bool emphasized = tutorialDamageTextEmphasis;
            GameObject obj = emphasized
                ? new GameObject($"FloatingDamageText_{targetSide}", typeof(RectTransform), typeof(CanvasGroup))
                : new GameObject($"FloatingDamageText_{targetSide}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(canvasRect, false);
            obj.transform.SetAsLastSibling();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = emphasized
                ? new Vector2(result.AbsorbedDamage > 0 ? 430f : 340f, 112f)
                : new Vector2(result.IsCritical ? 250f : 190f, 64f);
            rect.anchoredPosition = ResolveFloatingDamagePosition(canvasRect, targetSide);
            rect.localScale = Vector3.one * (result.IsCritical ? 1.12f : 1f);

            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group == null)
                group = obj.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            TMP_Text text = emphasized
                ? CreateFloatingDamageLabel(rect, result)
                : obj.GetComponent<TMP_Text>();
            text.raycastTarget = false;
            text.fontSize = emphasized
                ? (result.IsCritical ? tutorialDamageTextFontSize * 1.08f : tutorialDamageTextFontSize)
                : (result.IsCritical ? damageTextFontSize * 1.14f : damageTextFontSize);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            text.color = result.IsCritical ? damageTextCriticalColor : damageTextColor;
            text.outlineColor = damageTextOutlineColor;
            text.outlineWidth = emphasized ? 0.34f : (result.IsCritical ? 0.28f : 0.22f);
            text.text = FormatFloatingDamageText(result);

            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.86f);
            shadow.effectDistance = emphasized ? new Vector2(4f, -4f) : new Vector2(2.4f, -2.4f);

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = emphasized ? new Vector2(2.2f, -2.2f) : new Vector2(1.3f, -1.3f);

            if (emphasized)
            {
                outline.enabled = false;
                shadow.enabled = false;
                AddFloatingDamageIcons(rect, result);
            }

            return text;
        }

        private TMP_Text CreateFloatingDamageLabel(RectTransform root, BattleDamageCalculator.DamageResult result)
        {
            GameObject labelObject = new GameObject("DamageValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root, false);
            labelObject.transform.SetAsLastSibling();

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = result.AbsorbedDamage > 0 ? new Vector2(-34f, 0f) : new Vector2(56f, 0f);
            labelRect.sizeDelta = result.AbsorbedDamage > 0 ? new Vector2(170f, 96f) : new Vector2(190f, 96f);

            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            text.raycastTarget = false;
            return text;
        }

        private void AddFloatingDamageIcons(RectTransform parent, BattleDamageCalculator.DamageResult result)
        {
            if (parent == null)
                return;

            float iconSize = Mathf.Max(1f, tutorialDamageTextIconSize);
            float x = -174f;
            BattleStatIconKind primary = result.IsCritical ? BattleStatIconKind.CriticalDamage : BattleStatIconKind.Attack;
            BattleStatIconProvider.ShowIcon(parent, "DamagePrimaryStatIcon", primary, new Vector2(x, 0f), new Vector2(iconSize * 1.28f, iconSize * 1.28f));

            if (result.AbsorbedDamage > 0)
                BattleStatIconProvider.ShowIcon(parent, "DamageArmorStatIcon", BattleStatIconKind.Armor, new Vector2(114f, -2f), new Vector2(iconSize * 1.04f, iconSize * 1.04f));
        }

        private string FormatFloatingDamageText(BattleDamageCalculator.DamageResult result)
        {
            string mainColor = ColorUtility.ToHtmlStringRGB(result.IsCritical ? damageTextCriticalColor : damageTextColor);
            string armorColor = ColorUtility.ToHtmlStringRGB(damageTextArmorColor);
            if (tutorialDamageTextEmphasis)
            {
                string emphasized = $"<color=#{mainColor}>-{result.FinalDamage}</color>";
                if (result.AbsorbedDamage > 0)
                    emphasized += $"    <color=#{armorColor}>{result.AbsorbedDamage}</color>";

                return emphasized;
            }

            string prefix = result.IsCritical ? "Crit! " : string.Empty;
            string text = $"<color=#{mainColor}>{prefix}-{result.FinalDamage}</color>";

            if (result.AbsorbedDamage > 0)
                text += $" <color=#{armorColor}>({result.AbsorbedDamage})</color>";

            return text;
        }

        private Vector2 ResolveFloatingDamagePosition(RectTransform canvasRect, BattleBoardSide targetSide)
        {
            Image fill = targetSide == BattleBoardSide.Player ? playerHpBarFill : opponentHpBarFill;
            RectTransform barRoot = fill != null && fill.transform.parent != null
                ? fill.transform.parent as RectTransform
                : null;

            if (barRoot == null)
                return Vector2.zero;

            Vector3 worldCenter = barRoot.TransformPoint(barRoot.rect.center);
            Camera camera = ResolveBattleHudCanvas()?.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : ResolveBattleHudCanvas()?.worldCamera;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(camera, worldCenter),
                camera,
                out localPoint);

            bool vertical = barRoot.rect.height > barRoot.rect.width * 1.2f;
            float xDirection = targetSide == BattleBoardSide.Player ? 1f : -1f;
            Vector2 offset = vertical
                ? new Vector2(xDirection * damageTextSideOffset, 0f)
                : new Vector2(0f, Mathf.Max(34f, barRoot.rect.height * 0.55f));

            return localPoint + offset;
        }

        private IEnumerator AnimateFloatingDamageText(TMP_Text text, BattleBoardSide targetSide, bool critical, bool emphasized)
        {
            if (text == null)
                yield break;

            RectTransform rect = emphasized &&
                                 text.transform.parent != null &&
                                 text.transform.parent.name.StartsWith("FloatingDamageText_", StringComparison.Ordinal)
                ? text.transform.parent as RectTransform
                : text.rectTransform;
            if (rect == null)
                yield break;
            Vector2 start = rect.anchoredPosition;
            CanvasGroup group = emphasized && text.transform.parent != null
                ? text.transform.parent.GetComponent<CanvasGroup>()
                : text.GetComponent<CanvasGroup>();
            float duration = Mathf.Max(0.05f, emphasized ? tutorialDamageTextDuration : damageTextDuration);
            float elapsed = 0f;
            float startScale = emphasized ? (critical ? 1.14f : 1.08f) : (critical ? 1.22f : 1.04f);
            float endScale = emphasized ? 0.98f : (critical ? 0.92f : 0.86f);
            float riseDistance = emphasized ? tutorialDamageTextRiseDistance : damageTextRiseDistance;

            while (elapsed < duration && text != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float pop = Mathf.Sin(Mathf.Clamp01(t * 2.2f) * Mathf.PI) * (emphasized ? 0.06f : (critical ? 0.16f : 0.09f));

                rect.anchoredPosition = start + new Vector2(0f, riseDistance * eased);
                rect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased + pop * 0.2f);
                float alpha = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(emphasized ? 0.72f : 0.45f, 1f, t));
                if (group != null)
                    group.alpha = alpha;
                else
                    text.alpha = alpha;

                yield return null;
            }

            activeDamageTexts.Remove(text);
            if (text != null)
                Destroy(text.transform.parent != null && text.transform.parent.name.StartsWith("FloatingDamageText_", StringComparison.Ordinal)
                    ? text.transform.parent.gameObject
                    : text.gameObject);
        }

        private void ClearFloatingDamageTexts()
        {
            for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
            {
                TMP_Text text = activeDamageTexts[i];
                if (text != null)
                {
                    GameObject root = text.transform.parent != null && text.transform.parent.name.StartsWith("FloatingDamageText_", StringComparison.Ordinal)
                        ? text.transform.parent.gameObject
                        : text.gameObject;
                    Destroy(root);
                }
            }

            activeDamageTexts.Clear();
        }

        private void ApplyRoundHudLayout(Vector2 canvasSize, bool compact, float fighterGap, float rootTop, float portraitSize)
        {
            HideRoundHudBackgroundImages();

            float centerWidth = Mathf.Clamp(fighterGap + portraitSize * 1.35f, 174f, compact ? 250f : 310f);
            float top = rootTop - Mathf.Clamp(portraitSize * 0.58f, compact ? 62f : 72f, compact ? 86f : 104f);

            if (showRoundHud)
            {
                ApplyCenteredHudText(roundText, new Vector2(0f, top), new Vector2(centerWidth, compact ? 24f : 28f), compact ? 18f : 22f);
            }
            else if (roundText != null)
            {
                roundText.enabled = false;
            }

            if (showRoundHud && useRoundBadgeImageSprites)
            {
                RefreshRoundBadge();
                ApplyRoundBadgeLayout(new Vector2(0f, roundBadgeOffset.y));
            }
            else
            {
                SetRoundBadgeVisible(false);
            }

            ApplyCenteredHudText(scoreText, new Vector2(0f, top - (compact ? 24f : 28f)), new Vector2(centerWidth, compact ? 28f : 34f), compact ? 23f : 28f);
            ApplyCenteredHudText(stateText, new Vector2(0f, top - (compact ? 56f : 66f)), new Vector2(Mathf.Min(canvasSize.x - 32f, centerWidth + 110f), compact ? 24f : 28f), compact ? 16f : 19f);
        }

        private void EnsureRoundBadgeImages()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            if (roundBadgeImage == null)
                roundBadgeImage = FindBattleHudImage(canvas.transform, "BattleRoundBadge");
            if (roundBadgeImage == null)
                roundBadgeImage = CreateBattleHudImage(canvas.transform, "BattleRoundBadge");

            if (roundBadgeNumberImage == null)
                roundBadgeNumberImage = FindBattleHudImage(canvas.transform, "BattleRoundBadgeNumber");
            if (roundBadgeNumberImage == null)
                roundBadgeNumberImage = CreateBattleHudImage(canvas.transform, "BattleRoundBadgeNumber");

            roundBadgeImage.transform.SetAsLastSibling();
            roundBadgeNumberImage.transform.SetAsLastSibling();
            roundBadgeImage.raycastTarget = false;
            roundBadgeNumberImage.raycastTarget = false;
        }

        private static Image FindBattleHudImage(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && string.Equals(child.name, objectName, StringComparison.Ordinal))
                    return child.GetComponent<Image>();
            }

            return null;
        }

        private static Image CreateBattleHudImage(Transform parent, string objectName)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private void ApplyRoundBadgeLayout()
        {
            Vector2 anchor = Vector2.zero;
            if (roundText != null && roundText.rectTransform != null)
                anchor = roundText.rectTransform.anchoredPosition + roundBadgeOffset;

            ApplyRoundBadgeLayout(anchor);
        }

        private void ApplyRoundBadgeLayout(Vector2 anchoredPosition)
        {
            if (roundBadgeImage != null && roundBadgeImage.rectTransform != null)
            {
                RectTransform rect = roundBadgeImage.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = roundBadgeSize;
                rect.localScale = Vector3.one;
            }

            if (roundBadgeNumberImage != null && roundBadgeNumberImage.rectTransform != null)
            {
                RectTransform rect = roundBadgeNumberImage.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = anchoredPosition + roundBadgeNumberOffset;
                rect.sizeDelta = roundBadgeNumberSize;
                rect.localScale = Vector3.one;
            }
        }

        private void SetRoundBadgeVisible(bool visible)
        {
            if (roundBadgeImage != null)
                roundBadgeImage.enabled = visible;
            if (roundBadgeNumberImage != null)
                roundBadgeNumberImage.enabled = visible;
        }

        private void HideRoundHudBackgroundImages()
        {
            Transform root = roundText != null && roundText.transform.parent != null
                ? roundText.transform.parent
                : null;

            if (root == null && scoreText != null && scoreText.transform.parent != null)
                root = scoreText.transform.parent;
            if (root == null && stateText != null && stateText.transform.parent != null)
                root = stateText.transform.parent;
            if (root == null)
                return;
            if (root.GetComponent<Canvas>() != null)
                return;
            if (!root.name.Contains("Round") && !root.name.Contains("Score") && !root.name.Contains("HUD"))
                return;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                    continue;

                image.enabled = false;
                image.raycastTarget = false;
            }
        }

        private void ApplyCenteredHudText(TMP_Text text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.fontSize = fontSize;
        }

        private static void SetProfileTextVisible(TMP_Text text, bool visible)
        {
            if (text != null && text.gameObject != null)
                text.gameObject.SetActive(visible);
        }

        private static void HideProfileTotemTextArtifacts()
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                string name = text.gameObject.name;
                if (name == "PlayerTotemText" || name == "PlayerTotemName" || name == "OpponentTotemText" || name == "OpponentTotemName")
                    text.gameObject.SetActive(false);
            }
        }

        private void BringBattleProfileContentToFront(Transform root)
        {
            if (root == null)
                return;

            Transform background = root.Find("BattleProfilePanelBackground");
            if (background != null)
                background.SetSiblingIndex(0);

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.transform.parent != root)
                    continue;

                if (background != null && image.transform == background)
                    continue;

                if (battleProfilePanelSprite != null && image.sprite == battleProfilePanelSprite)
                {
                    image.enabled = false;
                    image.raycastTarget = false;
                    continue;
                }
            }

            if (playerBattleSpriteImage != null && playerBattleSpriteImage.transform.parent == root)
                playerBattleSpriteImage.transform.SetAsLastSibling();

            if (opponentBattleSpriteImage != null && opponentBattleSpriteImage.transform.parent == root)
                opponentBattleSpriteImage.transform.SetAsLastSibling();

            BringProfileImageToFront(playerRankIconImage, root);
            BringProfileImageToFront(opponentRankIconImage, root);
            BringProfileImageToFront(playerCardPortraitImage, root);
            BringProfileImageToFront(opponentCardPortraitImage, root);
            BringProfileImageToFront(playerTotemImage, root);
            BringProfileImageToFront(opponentTotemImage, root);

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].transform.parent == root)
                    texts[i].transform.SetAsLastSibling();
            }
        }

        private static void BringProfileImageToFront(Image image, Transform root)
        {
            if (image != null && image.transform != null && image.transform.parent == root)
                image.transform.SetAsLastSibling();
        }

        private void BringBattleFightersAboveBoards(params Transform[] roots)
        {
            if (roots == null)
                return;

            for (int i = 0; i < roots.Length; i++)
            {
                Transform root = roots[i];
                if (root == null)
                    continue;

                root.SetAsLastSibling();
                BringBattleProfileContentToFront(root);
            }
        }

        private Vector2 ResolveBattleHudCanvasSize()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return canvasRect.rect.size;

            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        private Canvas ResolveBattleHudCanvas()
        {
            Canvas canvas = roundText != null ? roundText.GetComponentInParent<Canvas>() : null;
            if (canvas == null && playerBattleSpriteImage != null)
                canvas = playerBattleSpriteImage.GetComponentInParent<Canvas>();
            if (canvas == null && opponentBattleSpriteImage != null)
                canvas = opponentBattleSpriteImage.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

            return canvas;
        }

        private void EnsureBattleMenuGearSpin()
        {
            Image settingsGear = SettingsMenuUI.EnsureBattleSettingsOpenButton();
            if (settingsGear != null)
                battleMenuGearImage = settingsGear;

            // The button itself must remain static. Rotating its RectTransform also
            // rotates the decorative frame and hit area, which looks corrupted.
            animateBattleMenuGear = false;
            StopBattleMenuGearSpin();

            RectTransform rect = battleMenuGearImage != null ? battleMenuGearImage.rectTransform : null;
            if (rect != null)
            {
                rect.localRotation = Quaternion.identity;
                rect.SetAsLastSibling();
            }
        }

        private void StopBattleMenuGearSpin()
        {
            if (battleMenuGearSpinRoutine != null)
            {
                StopCoroutine(battleMenuGearSpinRoutine);
                battleMenuGearSpinRoutine = null;
            }

            RectTransform rect = battleMenuGearImage != null ? battleMenuGearImage.rectTransform : null;
            if (rect != null)
                rect.localRotation = Quaternion.identity;
        }

        private IEnumerator AnimateBattleMenuGearSpin()
        {
            RectTransform rect = battleMenuGearImage != null ? battleMenuGearImage.rectTransform : null;
            if (rect == null)
            {
                battleMenuGearSpinRoutine = null;
                yield break;
            }

            Quaternion baseRotation = rect.localRotation;
            float duration = Mathf.Max(0.05f, battleMenuGearHalfTurnDuration);

            while (animateBattleMenuGear && rect != null)
            {
                yield return RotateBattleMenuGear(rect, baseRotation, -360f, duration);
                yield return RotateBattleMenuGear(rect, baseRotation, 360f, duration);
            }

            if (rect != null)
                rect.localRotation = baseRotation;
            battleMenuGearSpinRoutine = null;
        }

        private static IEnumerator RotateBattleMenuGear(RectTransform rect, Quaternion baseRotation, float targetAngle, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetAngle, eased));
                yield return null;
            }

            if (rect != null)
                rect.localRotation = baseRotation;
        }

        private Image ResolveBattleMenuGearImage()
        {
            Image settingsGear = SettingsMenuUI.EnsureBattleSettingsOpenButton();
            if (settingsGear != null)
                return settingsGear;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvas == null || canvasRect == null)
                return null;

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            Image bestTopCenter = null;
            float bestTopCenterScore = float.PositiveInfinity;

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                RectTransform rect = image != null ? image.rectTransform : null;
                if (image == null || rect == null || !image.gameObject.activeInHierarchy || image.sprite == null)
                    continue;

                if (image.GetComponentInParent<Canvas>() != canvas)
                    continue;

                if (IsKnownNonGearBattleImage(image))
                    continue;

                if (!TryResolveRectBoundsInCanvas(rect, canvas, canvasRect, out Rect bounds))
                    continue;

                float width = Mathf.Abs(bounds.width);
                float height = Mathf.Abs(bounds.height);
                if (width < 18f || height < 18f || width > 120f || height > 120f)
                    continue;

                float centerBand = Mathf.Max(72f, canvasRect.rect.width * 0.12f);
                if (Mathf.Abs(bounds.center.x) > centerBand)
                    continue;

                if (bounds.center.y < canvasRect.rect.yMax - 140f)
                    continue;

                string itemName = image.name ?? string.Empty;
                float nameBonus =
                    itemName.IndexOf("gear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("settings", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("cog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("menu", StringComparison.OrdinalIgnoreCase) >= 0
                        ? -120f
                        : 0f;
                float score = Mathf.Abs(bounds.center.x) * 2f + Mathf.Abs((canvasRect.rect.yMax - 36f) - bounds.center.y) + nameBonus;
                if (score < bestTopCenterScore)
                {
                    bestTopCenterScore = score;
                    bestTopCenter = image;
                }
            }

            return bestTopCenter;
        }

        private bool IsValidBattleMenuGearImage(Image image)
        {
            if (IsSettingsMenuGearImage(image))
                return image.gameObject.activeInHierarchy && image.sprite != null;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            RectTransform rect = image != null ? image.rectTransform : null;
            if (image == null || rect == null || canvas == null || canvasRect == null || image.sprite == null)
                return false;

            if (image.GetComponentInParent<Canvas>() != canvas || IsKnownNonGearBattleImage(image))
                return false;

            if (!TryResolveRectBoundsInCanvas(rect, canvas, canvasRect, out Rect bounds))
                return false;

            float centerBand = Mathf.Max(72f, canvasRect.rect.width * 0.12f);
            return Mathf.Abs(bounds.center.x) <= centerBand
                   && bounds.center.y >= canvasRect.rect.yMax - 140f
                   && Mathf.Abs(bounds.width) >= 18f
                   && Mathf.Abs(bounds.height) >= 18f
                   && Mathf.Abs(bounds.width) <= 120f
                   && Mathf.Abs(bounds.height) <= 120f;
        }

        private static bool IsSettingsMenuGearImage(Image image)
        {
            if (image == null)
                return false;

            Button button = image.GetComponent<Button>();
            if (button == null)
                button = image.GetComponentInParent<Button>();

            return button != null && string.Equals(button.name, "BtnOpenSettings", StringComparison.Ordinal);
        }

        private bool IsKnownNonGearBattleImage(Image image)
        {
            if (image == null)
                return true;

            return image == playerHpBarFill
                   || image == opponentHpBarFill
                   || image == playerBattleSpriteImage
                   || image == opponentBattleSpriteImage
                   || image == playerCardPortraitImage
                   || image == opponentCardPortraitImage
                   || image == playerRankIconImage
                   || image == opponentRankIconImage
                   || image == playerTotemImage
                   || image == opponentTotemImage
                   || IsProfileChildImage(image)
                   || (playerBoardFullscreenButton != null && image.transform.IsChildOf(playerBoardFullscreenButton.transform));
        }

        private bool IsProfileChildImage(Image image)
        {
            if (image == null)
                return true;

            Transform playerRoot = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerCardPortraitImage, playerTotemImage);
            Transform opponentRoot = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText, opponentStatsText, opponentCardPortraitImage, opponentTotemImage);
            return playerRoot != null && image.transform.IsChildOf(playerRoot)
                   || opponentRoot != null && image.transform.IsChildOf(opponentRoot);
        }

        private static bool TryResolveRectBoundsInCanvas(RectTransform source, Canvas canvas, RectTransform canvasRect, out Rect bounds)
        {
            bounds = default;
            if (source == null || canvas == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            bool resolvedAny = false;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out Vector2 localPoint))
                    continue;

                resolvedAny = true;
                minX = Mathf.Min(minX, localPoint.x);
                maxX = Mathf.Max(maxX, localPoint.x);
                minY = Mathf.Min(minY, localPoint.y);
                maxY = Mathf.Max(maxY, localPoint.y);
            }

            if (!resolvedAny)
                return false;

            bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private void CreateResultPanelUi()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            GameObject root = resultPanelRoot != null
                ? resultPanelRoot
                : new GameObject("BattleResultPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            root.layer = canvas.gameObject.layer;
            if (root.transform.parent != canvas.transform)
                root.transform.SetParent(canvas.transform, false);
            root.SetActive(true);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                rect = root.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = root.GetComponent<Image>();
            if (background == null)
                background = root.AddComponent<Image>();

            background.color = Color.clear;
            background.raycastTarget = true;

            resultPanelRoot = root;

            RectTransform window = EnsureResultWindow(root.transform);

            ReparentResultElement(resultTitleText, window);
            ReparentResultElement(resultRewardText, window);
            ReparentResultElement(resultExperienceText, window);
            ReparentResultElement(resultRewardIcon, window);
            ReparentResultElement(resultBattleLobbyButton, window);
            ReparentResultElement(resultNewMatchButton, window);
            ReparentResultElement(resultPlayerCharacterImage, window);
            ReparentResultElement(resultOpponentCharacterImage, window);

            if (resultTitleText == null)
                resultTitleText = CreateResultText(window);

            if (resultRewardText == null)
                resultRewardText = CreateResultInfoText(window, "BattleResultReward", new Vector2(0f, 34f), 32f);

            if (resultExperienceText == null)
                resultExperienceText = CreateResultInfoText(window, "BattleResultExperience", new Vector2(0f, -8f), 26f);

            resultRewardIcon = EnsureResultRewardIcon(window);

            if (resultBattleLobbyButton == null)
                resultBattleLobbyButton = CreateResultButton(window, "BattleLobbyButton", new Vector2(-142f, -126f), returnToBattleLobbyText);

            if (resultNewMatchButton == null)
                resultNewMatchButton = CreateResultButton(window, "BattleNewMatchButton", new Vector2(142f, -126f), newMatchText);

            EnsureResultCharacterViews(window);

            Vector2 visualSize = ResultPanelVisualSize;
            ApplyResultTextLayout(resultTitleText, new Vector2(0f, 198f), new Vector2(visualSize.x - 220f, 78f), 54f);
            ApplyResultTextLayout(resultRewardText, new Vector2(92f, 52f), new Vector2(340f, 82f), 60f);
            ApplyResultTextLayout(resultExperienceText, new Vector2(0f, -58f), new Vector2(visualSize.x - 330f, 128f), 34f);
            if (resultExperienceText != null)
                resultExperienceText.textWrappingMode = TextWrappingModes.Normal;
            ApplyResultRewardIconLayout(resultRewardIcon, new Vector2(-112f, 52f), new Vector2(98f, 98f));
            ApplyResultRewardTextStyle();
            ApplyResultButtonLayout(resultBattleLobbyButton, new Vector2(0f, -214f), new Vector2(430f, 78f), LocalizedBattleLobbyButtonText());
            if (resultNewMatchButton != null)
                resultNewMatchButton.gameObject.SetActive(false);
            HideResultCharacterModel(resultPlayerCharacterModelView, resultPlayerCharacterImage);
            HideResultCharacterModel(resultOpponentCharacterModelView, resultOpponentCharacterImage);

            root.transform.SetAsLastSibling();
            window.SetAsLastSibling();
        }

        private void EnsureResultCharacterViews(RectTransform window)
        {
            if (window == null)
                return;

            resultPlayerCharacterImage = EnsureResultCharacterImage(
                window,
                resultPlayerCharacterImage,
                "BattleResultPlayerCharacter");
            resultOpponentCharacterImage = EnsureResultCharacterImage(
                window,
                resultOpponentCharacterImage,
                "BattleResultOpponentCharacter");

            resultPlayerCharacterModelView = EnsureResultCharacterModelView(
                resultPlayerCharacterImage,
                resultPlayerCharacterModelView);
            resultOpponentCharacterModelView = EnsureResultCharacterModelView(
                resultOpponentCharacterImage,
                resultOpponentCharacterModelView);
        }

        private Image EnsureResultCharacterImage(RectTransform parent, Image image, string objectName)
        {
            if (image == null)
            {
                Transform existing = parent.Find(objectName);
                image = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (image == null)
            {
                GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.layer = parent.gameObject.layer;
                imageObject.transform.SetParent(parent, false);
                image = imageObject.GetComponent<Image>();
            }
            else if (image.transform.parent != parent)
            {
                image.transform.SetParent(parent, false);
            }

            image.color = Color.white;
            image.raycastTarget = false;
            image.enabled = false;
            image.transform.SetAsFirstSibling();
            return image;
        }

        private BattleCharacterModelView EnsureResultCharacterModelView(
            Image image,
            BattleCharacterModelView modelView)
        {
            if (image == null)
                return modelView;

            if (modelView == null)
                modelView = image.GetComponent<BattleCharacterModelView>();

            if (modelView == null)
                modelView = image.gameObject.AddComponent<BattleCharacterModelView>();

            return modelView;
        }

        private void ApplyResultCharacterLayout(Image image, Vector2 anchoredPosition)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = resultCharacterSize;

            rect.localScale = Vector3.one;
        }

        private Image EnsureResultRewardIcon(RectTransform parent)
        {
            if (parent == null)
                return resultRewardIcon;

            if (resultRewardIcon == null)
            {
                Transform existing = parent.Find("BattleResultRewardIcon");
                resultRewardIcon = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (resultRewardIcon == null)
            {
                GameObject iconObject = new GameObject("BattleResultRewardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.layer = parent.gameObject.layer;
                iconObject.transform.SetParent(parent, false);
                resultRewardIcon = iconObject.GetComponent<Image>();
            }
            else if (resultRewardIcon.transform.parent != parent)
            {
                resultRewardIcon.transform.SetParent(parent, false);
            }

            resultRewardIcon.sprite = LoadResultOzTileIcon();
            resultRewardIcon.enabled = resultRewardIcon.sprite != null;
            resultRewardIcon.color = Color.white;
            resultRewardIcon.preserveAspect = true;
            resultRewardIcon.raycastTarget = false;
            resultRewardIcon.transform.SetAsLastSibling();
            return resultRewardIcon;
        }

        private void ApplyResultRewardIconLayout(Image image, Vector2 anchoredPosition, Vector2 size)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private void ApplyResultRewardTextStyle()
        {
            if (resultRewardText != null)
            {
                resultRewardText.enableVertexGradient = true;
                resultRewardText.colorGradient = new VertexGradient(
                    new Color(1f, 0.98f, 0.72f, 1f),
                    new Color(1f, 0.84f, 0.24f, 1f),
                    new Color(0.82f, 0.38f, 0.06f, 1f),
                    new Color(1f, 0.62f, 0.16f, 1f));
                resultRewardText.fontStyle = FontStyles.Bold;
                resultRewardText.outlineWidth = 0.18f;
                resultRewardText.outlineColor = new Color(0.16f, 0.055f, 0.015f, 1f);
                resultRewardText.fontSizeMax = Mathf.Max(resultRewardText.fontSizeMax, 64f);
            }

            if (resultExperienceText != null)
            {
                resultExperienceText.enableVertexGradient = true;
                resultExperienceText.colorGradient = new VertexGradient(
                    new Color(1f, 0.96f, 0.78f, 1f),
                    new Color(0.93f, 0.86f, 0.66f, 1f),
                    new Color(0.72f, 0.48f, 0.22f, 1f),
                    new Color(0.94f, 0.78f, 0.44f, 1f));
                resultExperienceText.fontStyle = FontStyles.Bold;
                resultExperienceText.outlineWidth = 0.12f;
                resultExperienceText.outlineColor = new Color(0.08f, 0.035f, 0.01f, 1f);
            }
        }

        private Sprite LoadResultOzTileIcon()
        {
            if (resultOzTileIconSprite != null)
                return resultOzTileIconSprite;

            resultOzTileIconSprite = Resources.Load<Sprite>(ResultOzTileIconResourcePath);
            if (resultOzTileIconSprite != null)
                return resultOzTileIconSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(ResultOzTileIconResourcePath);
            if (sprites != null && sprites.Length > 0)
                resultOzTileIconSprite = sprites[0];

            if (resultOzTileIconSprite != null)
                return resultOzTileIconSprite;

            Texture2D texture = Resources.Load<Texture2D>(ResultOzTileIconResourcePath);
            if (texture != null)
            {
                resultOzTileIconSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return resultOzTileIconSprite;
        }

        private RectTransform EnsureResultWindow(Transform overlayRoot)
        {
            Transform existing = overlayRoot.Find("BattleResultWindow");
            GameObject windowObject = existing != null
                ? existing.gameObject
                : new GameObject("BattleResultWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            windowObject.layer = overlayRoot.gameObject.layer;
            windowObject.transform.SetParent(overlayRoot, false);
            windowObject.SetActive(true);

            RectTransform rect = windowObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = windowObject.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = ResultPanelVisualSize;

            Image image = windowObject.GetComponent<Image>();
            if (image == null)
                image = windowObject.AddComponent<Image>();

            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = true;

            return rect;
        }

        private void ApplyResultWindowSprite(bool playerWon)
        {
            if (resultPanelRoot == null)
                return;

            Transform window = resultPanelRoot.transform.Find("BattleResultWindow");
            Image image = window != null ? window.GetComponent<Image>() : null;
            if (image == null)
                return;

            Sprite sprite = ResolveResultWindowSprite(playerWon);
            if (sprite == null)
                return;

            image.enabled = true;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;
        }

        private Sprite ResolveResultWindowSprite(bool playerWon)
        {
            if (playerWon)
            {
                if (winResultWindowSprite == null && !string.IsNullOrWhiteSpace(winResultWindowResourcePath))
                    winResultWindowSprite = Resources.Load<Sprite>(winResultWindowResourcePath);

                return winResultWindowSprite;
            }

            if (defeatResultWindowSprite == null && !string.IsNullOrWhiteSpace(defeatResultWindowResourcePath))
                defeatResultWindowSprite = Resources.Load<Sprite>(defeatResultWindowResourcePath);

            return defeatResultWindowSprite;
        }

        private static void ReparentResultElement(Component component, RectTransform window)
        {
            if (component == null || window == null)
                return;

            component.transform.SetParent(window, false);
            component.transform.SetAsLastSibling();
        }

        private TMP_Text CreateResultText(Transform parent)
        {
            GameObject textObject = new GameObject("BattleResultTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 112f);
            Vector2 visualSize = ResultPanelVisualSize;
            rect.sizeDelta = new Vector2(visualSize.x - 80f, 96f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = 68f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.text = ResolveResultTitle(true);

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            BattlePopupStyle.ApplyFontOnly(text);

            return text;
        }

        private void ApplyResultTextLayout(TMP_Text text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize)
        {
            if (text == null)
                return;

            RectTransform rect = text.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private void ApplyResultButtonLayout(Button button, Vector2 anchoredPosition, Vector2 sizeDelta, string labelText)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = labelText;
                label.fontSize = 28f;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16f;
                label.fontSizeMax = 30f;
                label.margin = new Vector4(34f, 8f, 34f, 10f);
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
            {
                image.enabled = true;
                image.color = Color.white;
                image.raycastTarget = true;
            }

            BattlePopupStyle.ApplyButton(button);
            BattlePopupStyle.ApplyButtonLabel(button, 28f);
        }

        private TMP_Text CreateResultInfoText(Transform parent, string objectName, Vector2 anchoredPosition, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            Vector2 visualSize = ResultPanelVisualSize;
            rect.sizeDelta = new Vector2(visualSize.x - 90f, 40f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            BattlePopupStyle.ApplyFontOnly(text);

            return text;
        }

        private Button CreateResultButton(Transform parent, string objectName, Vector2 anchoredPosition, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(280f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateResultButtonLabel(buttonObject.transform);
            label.text = labelText;

            return button;
        }

        private TMP_Text CreateResultButtonLabel(Transform parent)
        {
            GameObject textObject = new GameObject("BattleLobbyButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = new Color(0.06f, 0.06f, 0.07f, 1f);
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            BattlePopupStyle.ApplyFontOnly(text);

            return text;
        }

        private void ApplyImageLayout(
            Image image,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void ApplyImageFlip(Image image, bool flipX)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            Vector3 scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
            rect.localScale = scale;
        }

        private void EnsureResultAudioSource()
        {
            if (resultAudioSource != null)
                return;

            resultAudioSource = GetComponent<AudioSource>();
            if (resultAudioSource == null)
                resultAudioSource = gameObject.AddComponent<AudioSource>();

            resultAudioSource.playOnAwake = false;
            resultAudioSource.loop = false;
            resultAudioSource.spatialBlend = 0f;
        }

        private void PlayResultSound(bool playerWon)
        {
            EnsureResultAudioSource();

            AudioClip clip = ResolveResultClip(playerWon);
            if (resultAudioSource == null || clip == null)
                return;

            resultAudioSource.PlayOneShot(clip, resultAudioVolume);
        }

        private void PlayResultRewardFlyFx(bool playerWon)
        {
            if (resultRewardFlyRoutine != null)
                StopCoroutine(resultRewardFlyRoutine);

            ClearResultRewardFlyFx();
            if (Mathf.Abs(lastResultOzTileDelta) <= 0 && lastResultGoldReward <= 0)
                return;

            resultRewardFlyRoutine = StartCoroutine(ResultRewardFlyFxRoutine(playerWon));
        }

        private IEnumerator ResultRewardFlyFxRoutine(bool playerWon)
        {
            yield return null;

            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            RectTransform targetRect = resultRewardIcon != null ? resultRewardIcon.rectTransform : null;
            if (canvasRect == null || targetRect == null)
                yield break;

            BattleBoardSide loserSide = playerWon ? BattleBoardSide.Opponent : BattleBoardSide.Player;
            RectTransform sourceRect = ResolveBattleAvatarRect(loserSide);
            if (sourceRect == null)
                sourceRect = targetRect;

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 source = ResolveWorldRectCenterInCanvas(sourceRect, canvasRect, camera);
            Vector2 target = ResolveWorldRectCenterInCanvas(targetRect, canvasRect, camera);
            Sprite sprite = LoadResultOzTileIcon();
            if (sprite == null)
                yield break;

            const int count = 14;
            const float burstDuration = 0.22f;
            const float flightDuration = 0.72f;
            for (int i = 0; i < count; i++)
            {
                Image fx = CreateResultRewardFxImage(canvasRect, sprite, source, i);
                if (fx != null)
                    activeResultRewardFxImages.Add(fx);
            }

            Vector2[] burstTargets = new Vector2[activeResultRewardFxImages.Count];
            for (int i = 0; i < burstTargets.Length; i++)
            {
                float angle = Mathf.Lerp(35f, 145f, (i + 0.5f) / Mathf.Max(1f, burstTargets.Length));
                float radius = 80f + (i % 4) * 18f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                burstTargets[i] = source + dir * radius + new Vector2((i - 6) * 5f, 0f);
            }

            for (float elapsed = 0f; elapsed < burstDuration; elapsed += Time.deltaTime)
            {
                float t = EaseOutCubic(elapsed / burstDuration);
                for (int i = activeResultRewardFxImages.Count - 1; i >= 0; i--)
                {
                    Image fx = activeResultRewardFxImages[i];
                    if (fx == null)
                        continue;

                    RectTransform rect = fx.rectTransform;
                    rect.anchoredPosition = Vector2.LerpUnclamped(source, burstTargets[i], t);
                    rect.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.15f, t);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-35f, 80f, t) + i * 11f);
                    fx.color = Color.white;
                }

                yield return null;
            }

            for (float elapsed = 0f; elapsed < flightDuration; elapsed += Time.deltaTime)
            {
                for (int i = activeResultRewardFxImages.Count - 1; i >= 0; i--)
                {
                    Image fx = activeResultRewardFxImages[i];
                    if (fx == null)
                        continue;

                    float delay = i * 0.028f;
                    float localT = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, flightDuration - delay));
                    float eased = EaseInOutCubic(localT);
                    float wave = Mathf.Sin(eased * Mathf.PI);
                    Vector2 start = i < burstTargets.Length ? burstTargets[i] : source;
                    Vector2 side = new Vector2((i - activeResultRewardFxImages.Count * 0.5f) * 8f, 0f);
                    Vector2 arc = new Vector2(0f, wave * (150f + i * 3f));
                    RectTransform rect = fx.rectTransform;
                    rect.anchoredPosition = Vector2.LerpUnclamped(start, target + side * 0.08f, eased) + arc;
                    rect.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.55f, eased);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(80f + i * 11f, -20f, eased));
                    Color color = Color.white;
                    color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((localT - 0.86f) / 0.14f));
                    fx.color = color;
                }

                yield return null;
            }

            ClearResultRewardFlyFx();
            resultRewardFlyRoutine = null;
        }

        private Image CreateResultRewardFxImage(RectTransform parent, Sprite sprite, Vector2 start, int index)
        {
            GameObject fxObject = new GameObject("ResultOzTileFlyFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fxObject.layer = parent.gameObject.layer;
            fxObject.transform.SetParent(parent, false);
            fxObject.transform.SetAsLastSibling();

            Image image = fxObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(44f, 44f);
            rect.localScale = Vector3.one * 0.25f;
            return image;
        }

        private void ClearResultRewardFlyFx()
        {
            for (int i = activeResultRewardFxImages.Count - 1; i >= 0; i--)
            {
                Image image = activeResultRewardFxImages[i];
                if (image != null && image.gameObject != null)
                    Destroy(image.gameObject);
            }

            activeResultRewardFxImages.Clear();
        }

        private void PlayImpactSound()
        {
            EnsureResultAudioSource();

            AudioClip clip = ResolveImpactClip();
            if (resultAudioSource == null || clip == null)
                return;

            resultAudioSource.PlayOneShot(clip, impactAudioVolume);
        }

        private AudioClip ResolveResultClip(bool playerWon)
        {
            if (playerWon)
            {
                if (resultWinClip == null && !string.IsNullOrWhiteSpace(resultWinClipResourcePath))
                    resultWinClip = Resources.Load<AudioClip>(resultWinClipResourcePath);

                return resultWinClip;
            }

            if (resultLoseClip == null && !string.IsNullOrWhiteSpace(resultLoseClipResourcePath))
                resultLoseClip = Resources.Load<AudioClip>(resultLoseClipResourcePath);

            return resultLoseClip;
        }

        private AudioClip ResolveImpactClip()
        {
            if (impactClip == null && !string.IsNullOrWhiteSpace(impactClipResourcePath))
                impactClip = Resources.Load<AudioClip>(impactClipResourcePath);

            return impactClip;
        }

        private void ApplyBattleMatchResult(bool playerWon, bool wasOnlineRankedBattle)
        {
            lastResultGoldReward = 0;
            lastResultWasRanked = false;
            lastResultWasTutorial = IsTutorialMatchActive();
            lastResultPlayerWon = playerWon;
            tutorialResultApplied = false;
            lastResultRankPointDelta = 0;
            lastResultOzTileDelta = 0;
            lastResultExperienceReward = playerWon
                ? Mathf.Max(0, battleWinExperienceReward)
                : Mathf.Max(0, battleLoseExperienceReward);
            lastResultAccountLevel = 1;
            lastResultBattleWins = 0;
            lastResultBattleLosses = 0;

            if (lastResultWasTutorial)
            {
                lastResultExperienceReward = 0;
                lastResultGoldReward = playerWon
                    ? BattleLoreTutorialSession.GetStageOzTileReward(BattleLoreTutorialSession.ActiveStage)
                    : 0;
                return;
            }

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
                profile.EnsureData();

            int tileBefore = GetProfileTile(profile);
            bool tournamentMatch = MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.TournamentMatch ||
                                   MahjongSession.BattleSource == MahjongBattleSource.Tournament;

            bool rankedPending = MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch &&
                                 RankedBattleService.HasPendingMatch();
            if (rankedPending)
            {
                RankedBattleResult rankedResult = RankedBattleService.ApplyRankedResult(
                    playerWon,
                    syncRankToServer: !wasOnlineRankedBattle);
                if (rankedResult != null && rankedResult.Applied)
                {
                    lastResultWasRanked = true;
                    lastResultGoldReward = playerWon ? Mathf.Max(0, rankedResult.WinRewardOzTile) : 0;
                    lastResultOzTileDelta = rankedResult.OzTileDelta;
                    lastResultRankPointDelta = rankedResult.RankPointDelta;
                    profile = ProfileService.I != null ? ProfileService.I.Current : profile;
                }
            }

            MahjongBattleResult battleResult = playerWon ? MahjongBattleResult.Win : MahjongBattleResult.Lose;
            int score = Mathf.Max(0, playerRoundWins * 100 - opponentRoundWins * 25);
            MahjongMatchResultData result = MahjongMatchResultData.CreateBattleResult(
                battleResult,
                score,
                maxCombo: 0,
                stakePot: MahjongSession.BattleStakePot,
                battleMvp: playerWon);

            if (lastResultWasRanked)
            {
                if (profile != null && profile.Mahjong != null && profile.Mahjong.Battle != null)
                    profile.Mahjong.TotalScoreAllModes += Mathf.Max(0, result.Score);
            }
            else if (tournamentMatch)
            {
                lastResultGoldReward = 0;
                lastResultOzTileDelta = 0;
                lastResultRankPointDelta = 0;
            }
            else if (MahjongMatchService.I != null)
            {
                MahjongMatchProcessResult processed = MahjongMatchService.I.ProcessMatch(result);
                lastResultGoldReward = processed != null ? Mathf.Max(0, processed.GrantedOzTile) : 0;

                if (playerWon && lastResultGoldReward <= 0 && MahjongRewardService.I == null)
                {
                    lastResultGoldReward = 100 + Mathf.Max(0, result.BattleStakePot);
                    GrantDirectBattleTile(lastResultGoldReward);
                }
            }
            else
            {
                ApplyBattleResultFallback(result);
                profile = ProfileService.I != null ? ProfileService.I.Current : profile;
            }

            if (profile == null)
                return;

            profile.EnsureData();
            if (!lastResultWasRanked && !tournamentMatch)
                EnsureBattleTileRewardPersisted(profile, playerWon, result, tileBefore);
            profile.AddAccountExp(lastResultExperienceReward);
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
            {
                profile.Mahjong.Battle.AddExperience(lastResultExperienceReward);
                lastResultAccountLevel = profile.Mahjong.Battle.Level;
                lastResultBattleWins = Mathf.Max(0, profile.Mahjong.Battle.Wins);
                lastResultBattleLosses = Mathf.Max(0, profile.Mahjong.Battle.Losses);
                if (MahjongTitleService.I != null)
                    MahjongTitleService.I.EvaluateBattleTitles(profile);
            }
            else
            {
                lastResultAccountLevel = profile.AccountLevel;
            }

            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }
        }

        private void EnsureBattleTileRewardPersisted(
            PlayerProfile profile,
            bool playerWon,
            MahjongMatchResultData result,
            int tileBefore)
        {
            if (profile == null || !playerWon)
                return;

            int expectedReward = Mathf.Max(0, lastResultGoldReward);
            if (expectedReward <= 0)
            {
                expectedReward = 100 + (result != null ? Mathf.Max(0, result.BattleStakePot) : 0);
                lastResultGoldReward = expectedReward;
            }

            int actualGain = Mathf.Max(0, GetProfileTile(profile) - Mathf.Max(0, tileBefore));
            int missingReward = Mathf.Max(0, expectedReward - actualGain);
            if (missingReward <= 0)
                return;

            profile.AddTile(missingReward);
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                profile.Mahjong.Battle.TotalBattleRewardEarned += missingReward;
        }

        private static int GetProfileTile(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Currencies != null ? Mathf.Max(0, profile.Currencies.OzTile) : 0;
        }

        private void ApplyBattleResultFallback(MahjongMatchResultData result)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || result == null)
                return;

            profile.EnsureData();

            if (result.BattleResult == MahjongBattleResult.Win)
            {
                profile.Mahjong.Battle.AddWin(result.BattleMvp);
                profile.Mahjong.TotalWins++;
                lastResultGoldReward = 100 + Mathf.Max(0, result.BattleStakePot);
                profile.AddTile(lastResultGoldReward);
            }
            else if (result.BattleResult == MahjongBattleResult.Lose)
            {
                profile.Mahjong.Battle.AddLoss(result.BattleMvp);
                profile.Mahjong.TotalLosses++;
            }

            profile.Mahjong.Battle.LastStakeUsed = Mathf.Max(0, result.BattleStakePot);
            profile.Mahjong.Battle.TotalBattleRewardEarned += lastResultGoldReward;
            profile.Mahjong.TotalMatchesPlayed++;
            profile.Mahjong.TotalScoreAllModes += Mathf.Max(0, result.Score);

            if (MahjongTitleService.I != null)
                MahjongTitleService.I.EvaluateBattleTitles(profile);
        }

        private void GrantDirectBattleTile(int amount)
        {
            if (amount <= 0)
                return;

            if (CurrencyService.I != null)
                CurrencyService.I.AddOzTile(amount);
            else if (ProfileService.I != null && ProfileService.I.Current != null)
                ProfileService.I.Current.AddTile(amount);

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return;

            profile.EnsureData();
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                profile.Mahjong.Battle.TotalBattleRewardEarned += amount;
        }

        private void ShowResultPanel(bool playerWon)
        {
            HideLobbyRuntimeUi();
            AutoResolveResultPanelUi();
            CreateResultPanelUi();
            bool tutorialResult = IsTutorialMatchActive();

            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(true);
                resultPanelRoot.transform.SetAsLastSibling();
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = string.Empty;
                resultTitleText.gameObject.SetActive(false);
            }

            if (resultRewardText != null)
                resultRewardText.text = tutorialResult
                    ? ResolveTutorialResultRewardText(playerWon)
                    : ResolveResultRewardText();

            if (resultExperienceText != null)
            {
                if (tutorialResult)
                {
                    resultExperienceText.text = string.Empty;
                    resultExperienceText.gameObject.SetActive(false);
                }
                else
                {
                    resultExperienceText.gameObject.SetActive(true);
                    string experienceText = ResolveResultExperienceText(
                        Mathf.Max(0, lastResultExperienceReward),
                        Mathf.Max(1, lastResultAccountLevel));
                    if (lastResultWasRanked)
                        experienceText += $"\nRP {FormatSigned(lastResultRankPointDelta)}";
                    experienceText += $"\nW {lastResultBattleWins} / L {lastResultBattleLosses}";
                    resultExperienceText.text = experienceText;
                }
            }

            ApplyResultRewardTextStyle();
            if (tutorialResult)
                ApplyTutorialResultTextLayout(playerWon);

            ApplyResultButtonLabel(resultBattleLobbyButton, LocalizedBattleLobbyButtonText());
            if (resultNewMatchButton != null)
                resultNewMatchButton.gameObject.SetActive(false);
            ApplyResultWindowSprite(playerWon);
            ApplyTutorialResultIconState(tutorialResult, playerWon);
            HideResultCharacterModel(resultOpponentCharacterModelView, resultOpponentCharacterImage);
            HideResultCharacterModel(resultPlayerCharacterModelView, resultPlayerCharacterImage);
            PlayResultSound(playerWon);
            PlayResultRewardFlyFx(playerWon);
            QueueResultMatchEndAd(playerWon ? "mahjong_battle_return_lobby_win" : "mahjong_battle_return_lobby_lose");
        }

        private void HideResultPanel()
        {
            if (resultPanelRoot != null)
                resultPanelRoot.SetActive(false);
        }

        private void DestroyResultPanel()
        {
            GameObject root = resultPanelRoot;
            HideResultPanel();

            resultPanelRoot = null;
            resultTitleText = null;
            resultRewardText = null;
            resultExperienceText = null;
            resultBattleLobbyButton = null;
            resultNewMatchButton = null;
            resultPlayerCharacterImage = null;
            resultOpponentCharacterImage = null;
            resultPlayerCharacterModelView = null;
            resultOpponentCharacterModelView = null;
            resultRewardIcon = null;
            resultMatchEndAdPending = false;
            resultMatchEndAdInProgress = false;
            pendingResultMatchEndAdSource = null;
            if (resultRewardFlyRoutine != null)
            {
                StopCoroutine(resultRewardFlyRoutine);
                resultRewardFlyRoutine = null;
            }
            ClearResultRewardFlyFx();

            if (root != null)
                Destroy(root);

            DestroyObjectsByName("BattleResultPanel");
            DestroyObjectsByName("BattleResultWindow");
        }

        private static void DestroyObjectsByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform item = transforms[i];
                if (item == null || !string.Equals(item.name, objectName, StringComparison.Ordinal))
                    continue;

                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
        }

        private void ApplyResultCharacterModels(bool playerWon)
        {
            EnsureResultCharacterViews(resultPanelRoot != null
                ? resultPanelRoot.transform.Find("BattleResultWindow") as RectTransform
                : null);

            HideResultCharacterModel(resultOpponentCharacterModelView, resultOpponentCharacterImage);
            HideResultCharacterModel(resultPlayerCharacterModelView, resultPlayerCharacterImage);

            BattleCharacterDatabase.BattleCharacterData winnerData = playerWon
                ? ResolveSelectedBattleCharacter()
                : ResolveBattleCharacter(opponentBattleCharacterId);

            ApplyResultWinnerCharacterModel(
                winnerData,
                resultPlayerCharacterModelView,
                resultPlayerCharacterImage);
        }

        private void ApplyResultWinnerCharacterModel(
            BattleCharacterDatabase.BattleCharacterData data,
            BattleCharacterModelView modelView,
            Image image)
        {
            if (image == null || modelView == null || data == null)
                return;

            bool shown = modelView.Show(data, BattleCharacterModelView.ModelContext.Battle, false);
            if (!shown)
                return;

            image.enabled = false;
            image.raycastTarget = false;
        }

        private void HideResultCharacterModel(BattleCharacterModelView modelView, Image image)
        {
            if (modelView != null)
                modelView.Hide();

            if (image != null)
            {
                image.enabled = false;
                image.raycastTarget = false;
            }
        }

        private string ResolveResultTitle(bool playerWon)
        {
            string value = playerWon ? winResultText : failedResultText;
            if (string.IsNullOrWhiteSpace(value))
                return playerWon ? LocalizedVictoryText() : LocalizedDefeatText();

            value = value.Trim();
            if (playerWon && string.Equals(value, "WIN", StringComparison.OrdinalIgnoreCase))
                return LocalizedVictoryText();
            if (!playerWon && string.Equals(value, "FAILED", StringComparison.OrdinalIgnoreCase))
                return LocalizedDefeatText();
            if (playerWon && string.Equals(value, "VICTORY", StringComparison.OrdinalIgnoreCase))
                return LocalizedVictoryText();
            if (!playerWon && string.Equals(value, "DEFEAT", StringComparison.OrdinalIgnoreCase))
                return LocalizedDefeatText();

            return value;
        }

        private static bool IsTutorialMatchActive()
        {
            return BattleLoreTutorialSession.IsActive;
        }

        private string ResolveTutorialResultRewardText(bool playerWon)
        {
            int amount = playerWon ? Mathf.Max(0, BattleLoreTutorialSession.GetStageOzTileReward(BattleLoreTutorialSession.ActiveStage)) : 0;
            string reward = BattleUiText("Награда:", "Reward:", "Ödül:", "Belohnung:") + $"  +{amount}";
            if (playerWon && BattleLoreTutorialSession.ActiveStage == 3)
            {
                BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
                string tileName = BattleLoreTutorialSession.GetStageThreeRewardTileName(store);
                reward += "\n" + BattleUiText("+3 редких камня", "+3 rare stones", "+3 nadir tas", "+3 seltene Steine") + $": {tileName}";
            }

            return reward;
        }

        private string TutorialResultRetryText()
        {
            return BattleUiText(
                "Это обучение. Статистика не изменена. Попробуй этап еще раз.",
                "This is training. Stats were not changed. Try the stage again.",
                "Bu egitimdir. Istatistik değişmedi. Asamayi tekrar dene.",
                "Das ist Training. Statistiken wurden nicht geaendert. Versuche die Stufe erneut.");
        }

        private void ApplyTutorialResultIconState(bool tutorialResult, bool playerWon)
        {
            if (resultRewardIcon == null)
                return;

            if (!tutorialResult)
            {
                resultRewardIcon.gameObject.SetActive(true);
                resultRewardIcon.enabled = resultRewardIcon.sprite != null;
                return;
            }

            bool showIcon = playerWon && BattleLoreTutorialSession.GetStageOzTileReward(BattleLoreTutorialSession.ActiveStage) > 0;
            resultRewardIcon.gameObject.SetActive(showIcon);
            resultRewardIcon.enabled = showIcon && resultRewardIcon.sprite != null;
        }

        private void ApplyTutorialResultTextLayout(bool playerWon)
        {
            if (resultTitleText != null)
                resultTitleText.gameObject.SetActive(false);

            if (resultRewardText != null)
            {
                bool hasReward = playerWon && BattleLoreTutorialSession.GetStageOzTileReward(BattleLoreTutorialSession.ActiveStage) > 0;
                bool hasStoneReward = playerWon && BattleLoreTutorialSession.ActiveStage == 3;
                Vector2 rewardPosition = hasReward
                    ? new Vector2(96f, 28f)
                    : new Vector2(0f, 28f);
                ApplyResultTextLayout(resultRewardText, hasStoneReward ? new Vector2(66f, 24f) : rewardPosition, hasStoneReward ? new Vector2(650f, 128f) : new Vector2(520f, 82f), hasStoneReward ? 34f : 42f);
                resultRewardText.textWrappingMode = hasStoneReward ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                resultRewardText.overflowMode = TextOverflowModes.Truncate;
            }

            if (resultExperienceText != null)
                resultExperienceText.gameObject.SetActive(false);

            if (resultRewardIcon != null)
                ApplyResultRewardIconLayout(resultRewardIcon, new Vector2(-190f, 28f), new Vector2(82f, 82f));
        }

        private string ResolveResultRewardText()
        {
            if (lastResultWasRanked)
                return FormatSigned(lastResultOzTileDelta);

            return lastResultGoldReward > 0
                ? $"+{Mathf.Max(0, lastResultGoldReward)}"
                : "+0";
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private string ResolveResultGoldText(int amount)
        {
            if (!string.IsNullOrWhiteSpace(resultGoldFormat) &&
                resultGoldFormat.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return string.Format(resultGoldFormat, amount);
            }

            return $"+{Mathf.Max(0, amount)} {ResultCurrencyLabel()}";
        }

        private string ResolveResultNoGoldText()
        {
            if (!string.IsNullOrWhiteSpace(resultNoGoldText) &&
                resultNoGoldText.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return resultNoGoldText;
            }

            return $"+0 {ResultCurrencyLabel()}";
        }

        private string ResolveResultExperienceText(int experience, int level)
        {
            if (!string.IsNullOrWhiteSpace(resultExperienceFormat) &&
                resultExperienceFormat.IndexOf("Level", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return string.Format(resultExperienceFormat, experience, level);
            }

            return $"+{Mathf.Max(0, experience)} XP  {LocalizedLevelLabel()} {Mathf.Max(1, level)}";
        }

        private static void ApplyResultButtonLabel(Button button, string labelText)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = labelText;
        }

        private string LocalizedVictoryText() => BattleUiText("ПОБЕДА", "VICTORY", "ZAFER", "SIEG");
        private string LocalizedDefeatText() => BattleUiText("ПОРАЖЕНИЕ", "DEFEAT", "YENILGI", "NIEDERLAGE");
        private string LocalizedGoldLabel() => BattleUiText("золото", "Gold", "Altın", "Gold");
        private string LocalizedLevelLabel() => BattleUiText("Уровень", "Level", "Seviye", "Level");
        private string LocalizedWinsLabel() => BattleUiText("Победы", "Wins", "Galibiyet", "Siege");
        private string LocalizedLossesLabel() => BattleUiText("Поражения", "Losses", "Mağlubiyet", "Niederlagen");
        private string LocalizedBattleLobbyButtonText() => BattleUiText("Вернуться в лобби", "Return to Lobby", "Lobiye Don", "Zur Lobby");
        private string LocalizedNewMatchButtonText() => BattleUiText("Новый матч", "New Match", "Yeni Maç", "Neues Match");

        private string ResultCurrencyLabel() => BattleUiText("\u041E\u0437 Tile", "OzTile", "Oz Tile", "OzTile");

        private string BattleUiText(string russian, string english, string turkish, string german = null)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            return language switch
            {
                GameLanguage.English => english,
                GameLanguage.Turkish => turkish,
                GameLanguage.German => string.IsNullOrWhiteSpace(german) ? english : german,
                _ => russian
            };
        }

        private void BindResultPanelButton()
        {
            if (resultBattleLobbyButton != null)
            {
                resultBattleLobbyButton.onClick.RemoveListener(OnClickReturnToBattleLobby);
                resultBattleLobbyButton.onClick.AddListener(OnClickReturnToBattleLobby);
            }

            if (resultNewMatchButton != null)
            {
                resultNewMatchButton.onClick.RemoveListener(OnClickFindNewMatch);
                resultNewMatchButton.onClick.AddListener(OnClickFindNewMatch);
            }
        }

        private void OnClickReturnToBattleLobby()
        {
            RunAfterResultMatchEndAd(ReturnToBattleLobbyAfterResult);
        }

        private void ReturnToBattleLobbyAfterResult()
        {
            ApplyTutorialResultProgressIfNeeded();
            DestroyResultPanel();

            if (botController != null)
                botController.StopBot();

            SceneManager.LoadScene(battleLobbySceneName);
        }

        private void ApplyTutorialResultProgressIfNeeded()
        {
            if (!lastResultWasTutorial || tutorialResultApplied)
                return;

            tutorialResultApplied = true;

            if (!BattleLoreTutorialSession.IsActive)
            {
                if (!lastResultPlayerWon)
                    BattleLoreTutorialSession.RequestOpenOnLobbyReturn();
                return;
            }

            if (lastResultPlayerWon)
            {
                int completedStage = BattleLoreTutorialSession.ActiveStage;
                BattleLoreTutorialSession.GrantStageReward(completedStage);
                BattleLoreTutorialSession.CompleteActiveStage();
                BattleLoreTutorialSession.ClearActive();

                if (completedStage < BattleLoreTutorialSession.StageCount)
                    BattleLoreTutorialSession.RequestOpenOnLobbyReturn();
                return;
            }

            BattleLoreTutorialSession.ClearActive();
            BattleLoreTutorialSession.RequestOpenOnLobbyReturn();
        }

        private void OnClickFindNewMatch()
        {
            FindNewMatchAfterResult();
        }

        private void FindNewMatchAfterResult()
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return;

            DestroyResultPanel();

            if (botController != null)
                botController.StopBot();

            MahjongBattleLobbyMode mode = MahjongBattleLobbySession.SelectedMode;
            if (mode == MahjongBattleLobbyMode.LocalWifiMatch || mode == MahjongBattleLobbyMode.RankedMatch)
            {
                SceneManager.LoadScene(battleLobbySceneName);
                return;
            }

            if (mode == MahjongBattleLobbyMode.None)
            {
                mode = MahjongBattleLobbyMode.RandomMatch;
                MahjongBattleLobbySession.SetMode(mode);
            }

            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            int playerRankPoints = 0;
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
            {
                profile.EnsureData();
                if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                    playerRankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
            }

            MahjongSession.StartBattle(botService.CreateOpponent(mode, playerRankPoints));
            SceneManager.LoadScene(battleGameSceneName);
        }

        private void QueueResultMatchEndAd(string source)
        {
            if (lastResultWasTutorial || BattleLoreTutorialSession.IsActive)
            {
                pendingResultMatchEndAdSource = string.Empty;
                resultMatchEndAdPending = false;
                resultMatchEndAdInProgress = false;
                SetResultButtonsInteractable(true);
                return;
            }

            pendingResultMatchEndAdSource = source;
            resultMatchEndAdPending = true;
            resultMatchEndAdInProgress = false;
            SetResultButtonsInteractable(true);
        }

        private void RunAfterResultMatchEndAd(Action action)
        {
            if (resultMatchEndAdInProgress)
                return;

            if (!resultMatchEndAdPending)
            {
                action?.Invoke();
                return;
            }

            resultMatchEndAdPending = false;
            resultMatchEndAdInProgress = true;
            SetResultButtonsInteractable(false);

            bool continued = false;
            void ContinueAfterAd()
            {
                if (continued)
                    return;

                continued = true;
                resultMatchEndAdInProgress = false;
                SetResultButtonsInteractable(true);
                action?.Invoke();
            }

            bool started = MatchEndAdService.TryShowAfterMatchResult(pendingResultMatchEndAdSource, _ => ContinueAfterAd());
            if (!started)
                ContinueAfterAd();
        }

        private void SetResultButtonsInteractable(bool interactable)
        {
            if (resultBattleLobbyButton != null)
                resultBattleLobbyButton.interactable = interactable;

            if (resultNewMatchButton != null)
                resultNewMatchButton.interactable = interactable;
        }

        private TMP_Text CreateProfileText(
            Transform parent,
            string objectName,
            float fontSize,
            Vector2 anchoredPosition,
            int siblingIndex)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            textObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(-40f, 34f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.text = objectName;

            TMP_Text styleSource = playerNameText != null ? playerNameText : opponentNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            BattlePopupStyle.ApplyFontOnly(text);

            return text;
        }

        private void BindCharacterSelectionService()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged -= ApplyPlayerBattleSpriteUi;
            BattleCharacterSelectionService.Instance.SelectedCharacterChanged += HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged += ApplyPlayerBattleSpriteUi;
        }

        private void UnbindCharacterSelectionService()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged -= ApplyPlayerBattleSpriteUi;
        }

        private void HandleSelectedCharacterChanged(string _)
        {
            ApplyPlayerBattleSpriteUi();
            RefreshPlayerCardProfileVisuals();
            ApplyBattleProfileLayout();
        }

        private void AutoResolveLinks()
        {
            if (battleStore == null)
                battleStore = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();

            if (battleLayoutPresetService == null)
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();

            if (combatSystem == null)
                combatSystem = GetComponent<BattleCombatSystem>();

            if (playerBoard == null || opponentBoard == null)
            {
                BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
                for (int i = 0; i < boards.Length; i++)
                {
                    BattleBoard foundBoard = boards[i];
                    if (foundBoard == null)
                        continue;

                    if (foundBoard.Side == BattleBoardSide.Player && playerBoard == null)
                        playerBoard = foundBoard;
                    else if (foundBoard.Side == BattleBoardSide.Opponent && opponentBoard == null)
                        opponentBoard = foundBoard;
                }
            }

            if (combatSystem != null)
            {
                combatSystem.SetMatchController(this);
                combatSystem.SetBoards(playerBoard, opponentBoard);
                BindCombatSystem();
            }

            ConfigureBoardInputOwnership();
        }

        private void ConfigureBoardInputOwnership()
        {
            if (playerBoard != null)
                playerBoard.SetAllowPlayerInput(true);

            if (opponentBoard != null)
                opponentBoard.SetAllowPlayerInput(false);
        }

        private void EnsureLocalWifiMatchSync()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.LocalWifiMatch)
                return;

            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.I;
            if (network == null || !network.IsConnected)
                return;

            LocalWifiBattleMatchSync sync = GetComponent<LocalWifiBattleMatchSync>();
            if (sync == null)
                sync = gameObject.AddComponent<LocalWifiBattleMatchSync>();

            sync.Configure(this, playerBoard, opponentBoard, botController, combatSystem);

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }
        }

        private void EnsureOnlineRankedMatchSync()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.RankedMatch &&
                MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.RandomMatch)
                return;

            OnlineRankedBattleNetwork network = OnlineRankedBattleNetwork.I;
            if (network == null || !network.IsInMatch)
                return;

            OnlineRankedBattleMatchSync sync = GetComponent<OnlineRankedBattleMatchSync>();
            if (sync == null)
                sync = gameObject.AddComponent<OnlineRankedBattleMatchSync>();

            sync.Configure(this, playerBoard, opponentBoard, botController, combatSystem);

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }
        }

        private static bool IsLocalWifiBattleActive()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch &&
                   LocalWifiBattleNetwork.I != null &&
                   LocalWifiBattleNetwork.I.IsConnected;
        }

        private static bool IsOnlineRankedBattleActive()
        {
            bool onlineBattleMode =
                MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch ||
                MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RandomMatch;

            return onlineBattleMode &&
                   OnlineRankedBattleNetwork.I != null &&
                   OnlineRankedBattleNetwork.I.IsInMatch;
        }

        private static bool IsRealtimeOpponentBattleActive()
        {
            return IsLocalWifiBattleActive() || IsOnlineRankedBattleActive();
        }

        private void EnsureBattleBoardsLayout()
        {
            if (playerBoard == null || opponentBoard == null)
                return;

            RectTransform playerArea = playerBoard.BoardArea;
            RectTransform opponentArea = opponentBoard.BoardArea;
            if (playerArea == null || opponentArea == null)
                return;

            ApplyBattleBoardPanelBackground(playerArea);
            ApplyBattleBoardPanelBackground(opponentArea);

            RectTransform root = playerArea.parent as RectTransform;
            if (root == null)
                root = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include)?.transform as RectTransform;

            if (root == null)
                return;

            if (boardsHeightFirstLayout == null || boardsHeightFirstLayout.transform != root)
                boardsHeightFirstLayout = root.GetComponent<BattleBoardsHeightFirstLayout>();

            if (boardsHeightFirstLayout == null)
                boardsHeightFirstLayout = root.gameObject.AddComponent<BattleBoardsHeightFirstLayout>();

            boardsHeightFirstLayout.Configure(playerArea, opponentArea);
            if (playerBoardFullscreen)
                ApplyPlayerBoardFullscreenLayout();
            else
                RefitBattleBoards();
        }

        private void EnsurePlayerBoardFullscreenButton()
        {
            if (playerBoardFullscreenButton == null)
                playerBoardFullscreenButton = FindButtonByObjectName("PlayerBoardFullscreenButton");

            if (playerBoardFullscreenButton == null && createPlayerBoardFullscreenButtonIfMissing)
                playerBoardFullscreenButton = CreatePlayerBoardFullscreenButton();

            if (playerBoardFullscreenButton == null)
                return;

            RectTransform playerArea = playerBoard != null ? playerBoard.BoardArea : null;
            if (playerArea != null && playerBoardFullscreenButton.transform.parent != playerArea)
                playerBoardFullscreenButton.transform.SetParent(playerArea, false);

            playerBoardFullscreenButton.onClick.RemoveListener(TogglePlayerBoardFullscreen);
            playerBoardFullscreenButton.onClick.AddListener(TogglePlayerBoardFullscreen);
            LayoutPlayerBoardFullscreenButton();
            RefreshPlayerBoardFullscreenButton();
        }

        private Button CreatePlayerBoardFullscreenButton()
        {
            RectTransform parent = playerBoard != null ? playerBoard.BoardArea : null;
            if (parent == null)
            {
                Canvas canvas = ResolveBattleHudCanvas();
                parent = canvas != null ? canvas.transform as RectTransform : null;
            }
            if (parent == null)
                return null;

            GameObject buttonObject = new GameObject("PlayerBoardFullscreenButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.SetAsLastSibling();

            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            BattlePopupStyle.ApplyButton(button);

            playerBoardFullscreenButtonText = CreateResultButtonLabel(buttonObject.transform);
            playerBoardFullscreenButtonText.name = "PlayerBoardFullscreenButtonText";
            playerBoardFullscreenButtonText.fontSize = 24f;
            playerBoardFullscreenButtonText.color = Color.white;
            playerBoardFullscreenButtonText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private void LayoutPlayerBoardFullscreenButton()
        {
            if (playerBoardFullscreenButton == null)
                return;

            RectTransform rect = playerBoardFullscreenButton.transform as RectTransform;
            if (rect == null)
                return;

            RectTransform fullscreenParent = null;
            if (playerBoardFullscreen)
                fullscreenParent = ResolveBattleHudCanvas()?.transform as RectTransform;
            else
                fullscreenParent = playerBoard != null ? playerBoard.BoardArea : null;

            if (fullscreenParent != null && rect.parent != fullscreenParent)
                rect.SetParent(fullscreenParent, false);

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = playerBoardFullscreenButtonOffset;
            rect.sizeDelta = playerBoardFullscreenButtonSize;
            rect.SetAsLastSibling();
        }

        private void TogglePlayerBoardFullscreen()
        {
            SetPlayerBoardFullscreen(!playerBoardFullscreen, true);
        }

        private void SetPlayerBoardFullscreen(bool fullscreen)
        {
            SetPlayerBoardFullscreen(fullscreen, false);
        }

        public void OpenPlayerBoardFullscreenForTutorial()
        {
            SetPlayerBoardFullscreen(true, false);
        }

        public void ClosePlayerBoardFullscreenForTutorial()
        {
            SetPlayerBoardFullscreen(false, false);
        }

        private void SetPlayerBoardFullscreen(bool fullscreen, bool savePreference)
        {
            if (playerBoardFullscreen == fullscreen)
            {
                if (savePreference)
                    SavePlayerBoardFullscreenPreference(fullscreen);
                return;
            }

            playerBoardFullscreen = fullscreen;
            if (savePreference)
                SavePlayerBoardFullscreenPreference(fullscreen);

            StopRestoreBoardsLayoutRoutine();

            if (boardsHeightFirstLayout != null)
                boardsHeightFirstLayout.enabled = !fullscreen;

            ClearUnsafeProfileFullscreenGroups();
            SetCanvasGroupVisible(ResolveOpponentBoardFullscreenGroup(), !fullscreen, false);
            SetCanvasGroupVisible(ResolvePlayerProfileFullscreenGroup(), !fullscreen, false);
            SetCanvasGroupVisible(ResolveOpponentProfileFullscreenGroup(), !fullscreen, false);

            if (fullscreen)
                ApplyPlayerBoardFullscreenLayout();
            else
            {
                RestoreBattleBoardsLayout();
            }

            RefreshPlayerBoardFullscreenButton();
            PlayerBoardFullscreenChanged?.Invoke(this, playerBoardFullscreen);
        }

        private static bool LoadPlayerBoardFullscreenPreference()
        {
            return PlayerPrefs.GetInt(PlayerBoardFullscreenPrefsKey, 0) == 1;
        }

        private static void SavePlayerBoardFullscreenPreference(bool fullscreen)
        {
            PlayerPrefs.SetInt(PlayerBoardFullscreenPrefsKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RestoreBattleBoardsLayout()
        {
            RestoreBattleBoardSiblingIndices();

            if (boardsHeightFirstLayout != null)
            {
                boardsHeightFirstLayout.enabled = true;
                boardsHeightFirstLayout.Apply();
            }

            RefitBattleBoards();
            ApplyBattleProfileLayout();

            restoreBoardsLayoutRoutine = StartCoroutine(RestoreBattleBoardsLayoutNextFrame());
        }

        private IEnumerator RestoreBattleBoardsLayoutNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            RestoreBattleBoardSiblingIndices();

            if (boardsHeightFirstLayout != null)
            {
                boardsHeightFirstLayout.enabled = true;
                boardsHeightFirstLayout.Apply();
            }

            RefitBattleBoards();
            ApplyBattleProfileLayout();
            RefreshBattleHpBars();
            restoreBoardsLayoutRoutine = null;
        }

        private void SaveBattleBoardSiblingIndices()
        {
            if (hasSavedBoardSiblingIndices)
                return;

            RectTransform playerArea = playerBoard != null ? playerBoard.BoardArea : null;
            RectTransform opponentArea = opponentBoard != null ? opponentBoard.BoardArea : null;
            if (playerArea == null || opponentArea == null)
                return;

            savedPlayerBoardSiblingIndex = playerArea.GetSiblingIndex();
            savedOpponentBoardSiblingIndex = opponentArea.GetSiblingIndex();
            hasSavedBoardSiblingIndices = true;
        }

        private void RestoreBattleBoardSiblingIndices()
        {
            if (!hasSavedBoardSiblingIndices)
                return;

            RectTransform playerArea = playerBoard != null ? playerBoard.BoardArea : null;
            RectTransform opponentArea = opponentBoard != null ? opponentBoard.BoardArea : null;
            if (playerArea == null || opponentArea == null)
                return;

            int childCount = playerArea.parent != null ? playerArea.parent.childCount : 0;
            if (childCount <= 0)
                return;

            playerArea.SetSiblingIndex(Mathf.Clamp(savedPlayerBoardSiblingIndex, 0, childCount - 1));
            opponentArea.SetSiblingIndex(Mathf.Clamp(savedOpponentBoardSiblingIndex, 0, childCount - 1));
            hasSavedBoardSiblingIndices = false;
        }

        private void ApplyPlayerBoardFullscreenLayout()
        {
            RectTransform playerArea = playerBoard != null ? playerBoard.BoardArea : null;
            if (playerArea == null)
                return;

            if (playerBoard == null || !playerBoard.IsBuilt)
                return;

            RectTransform root = playerArea.parent as RectTransform;
            if (root == null)
                root = ResolveBattleHudCanvas()?.transform as RectTransform;
            if (root == null)
                return;

            if (boardsHeightFirstLayout != null)
                boardsHeightFirstLayout.enabled = false;

            SaveBattleBoardSiblingIndices();

            playerArea.anchorMin = Vector2.zero;
            playerArea.anchorMax = Vector2.one;
            playerArea.pivot = new Vector2(0.5f, 0.5f);
            playerArea.offsetMin = new Vector2(playerBoardFullscreenSideInset, playerBoardFullscreenBottomInset);
            playerArea.offsetMax = new Vector2(-playerBoardFullscreenSideInset, -playerBoardFullscreenTopInset);
            playerArea.SetAsLastSibling();
            LayoutPlayerBoardFullscreenButton();
            BringPlayerBoardTilesToFront();

            float fullscreenScale = Mathf.Max(playerBoardFullscreenMaxTileScale, 3.15f);
            playerBoard.RefitIntoBoardArea(fullscreenScale, 0f, 0f, 0f, 0f);
            lastPlayerBoardAreaSize = playerArea.rect.size;
            BringPlayerBoardTilesToFront();
            ApplyFullscreenHpBarLayouts();
            LayoutPlayerBoardFullscreenButton();
            BringBattleMenuGearToFront();
        }

        private void BringPlayerBoardTilesToFront()
        {
            RectTransform root = playerBoard != null ? playerBoard.Root : null;
            if (root != null)
                root.SetAsLastSibling();
        }

        private void BringBattleMenuGearToFront()
        {
            RectTransform rect = battleMenuGearImage != null ? battleMenuGearImage.rectTransform : null;
            if (rect != null)
                rect.SetAsLastSibling();
        }

        private void RefreshPlayerBoardFullscreenButton()
        {
            if (playerBoardFullscreenButton == null)
                return;

            playerBoardFullscreenButton.gameObject.SetActive(!matchFinished);
            if (playerBoardFullscreenButtonText == null)
                playerBoardFullscreenButtonText = playerBoardFullscreenButton.GetComponentInChildren<TMP_Text>(true);

            if (playerBoardFullscreenButtonText != null)
                playerBoardFullscreenButtonText.text = playerBoardFullscreen ? "BACK" : "FULL";

            LayoutPlayerBoardFullscreenButton();
        }

        private void ApplyFullscreenHpBarLayouts()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            RectTransform playerArea = playerBoard != null ? playerBoard.BoardArea : null;
            if (canvas == null || canvasRect == null || playerArea == null)
                return;

            ReparentHpBarToCanvas(playerHpBarFill, playerHpBarText, canvas);
            ReparentHpBarToCanvas(opponentHpBarFill, opponentHpBarText, canvas);

            float barHeight = ResolveFullscreenHorizontalHpBarHeight();
            float edgeInset = Mathf.Max(playerBoardFullscreenHpBarGap, barHeight * 0.16f);
            float topInset = Mathf.Max(playerBoardFullscreenHpBarGap * 0.65f, 6f);
            EnsureBattleMenuGearSpin();
            Rect gearBounds = ResolveBattleMenuGearBounds(canvas, canvasRect, barHeight, topInset);
            float centerGap = Mathf.Clamp(Mathf.Max(gearBounds.width, barHeight) + playerBoardFullscreenHpBarGap * 2f, 78f, 170f);
            float barWidth = Mathf.Clamp(
                Mathf.Min(
                    gearBounds.xMin - canvasRect.rect.xMin - edgeInset - playerBoardFullscreenHpBarGap,
                    canvasRect.rect.xMax - gearBounds.xMax - edgeInset - playerBoardFullscreenHpBarGap),
                190f,
                Mathf.Max(430f, canvasRect.rect.width * 0.42f));
            Vector2 size = new Vector2(barWidth, barHeight);
            float y = gearBounds.height > 1f
                ? gearBounds.center.y
                : canvasRect.rect.yMax - topInset - barHeight * 0.5f;

            ApplyFullscreenHorizontalHpBarLayout(
                opponentHpBarFill,
                opponentHpBarText,
                new Vector2(gearBounds.center.x + centerGap * 0.5f + barWidth * 0.5f, y),
                size,
                false);

            ApplyFullscreenHorizontalHpBarLayout(
                playerHpBarFill,
                playerHpBarText,
                new Vector2(gearBounds.center.x - centerGap * 0.5f - barWidth * 0.5f, y),
                size,
                true);
        }

        private Rect ResolveBattleMenuGearBounds(Canvas canvas, RectTransform canvasRect, float fallbackHeight, float topInset)
        {
            if (battleMenuGearImage == null || !IsValidBattleMenuGearImage(battleMenuGearImage))
                battleMenuGearImage = ResolveBattleMenuGearImage();

            RectTransform gearRect = battleMenuGearImage != null ? battleMenuGearImage.rectTransform : null;
            if (gearRect != null && TryResolveRectBoundsInCanvas(gearRect, canvas, canvasRect, out Rect bounds))
                return bounds;

            float size = Mathf.Max(1f, fallbackHeight);
            float y = canvasRect.rect.yMax - topInset - size * 0.5f;
            return new Rect(-size * 0.5f, y - size * 0.5f, size, size);
        }

        private float ResolveFullscreenHorizontalHpBarHeight()
        {
            return Mathf.Clamp(horizontalBoardHpBarHeight * 0.7f, 38f, 52f);
        }

        private void ApplyFullscreenHorizontalHpBarLayout(Image fill, TMP_Text text, Vector2 offset, Vector2 size, bool playerSide)
        {
            RectTransform barRoot = fill != null && fill.transform.parent != null
                ? fill.transform.parent as RectTransform
                : null;

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(true);
                barRoot.anchorMin = new Vector2(0.5f, 0.5f);
                barRoot.anchorMax = new Vector2(0.5f, 0.5f);
                barRoot.pivot = new Vector2(0.5f, 0.5f);
                barRoot.anchoredPosition = offset;
                barRoot.sizeDelta = size;
                barRoot.localRotation = Quaternion.identity;
                barRoot.localScale = Vector3.one;
                barRoot.SetAsLastSibling();

                Image background = barRoot.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;
            }

            ApplyHpBarSpriteVisuals(fill, playerSide);
            ApplyHpBarFillValue(fill, playerSide ? playerHpBarFillColor : opponentHpBarFillColor, playerSide ? ResolvePlayerHpNormalized() : ResolveOpponentHpNormalized());

            if (text != null)
                ApplyHpBarTextLayout(text, offset, size);
        }

        private void PrepareFullscreenCharacterSprites()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            PrepareFullscreenCharacterSprite(playerBattleSpriteImage, ref playerFullscreenSpriteState, canvasRect);
            PrepareFullscreenCharacterSprite(opponentBattleSpriteImage, ref opponentFullscreenSpriteState, canvasRect);
        }

        private static void PrepareFullscreenCharacterSprite(Image image, ref BattleFullscreenSpriteState state, RectTransform canvasRect)
        {
            RectTransform rect = image != null ? image.rectTransform : null;
            if (rect == null || canvasRect == null)
                return;

            if (!state.HasValue)
            {
                state.Parent = rect.parent;
                state.SiblingIndex = rect.GetSiblingIndex();
                state.AnchorMin = rect.anchorMin;
                state.AnchorMax = rect.anchorMax;
                state.Pivot = rect.pivot;
                state.AnchoredPosition = rect.anchoredPosition;
                state.SizeDelta = rect.sizeDelta;
                state.LocalScale = rect.localScale;
                state.LocalRotation = rect.localRotation;
                state.HasValue = true;
            }

            if (rect.parent != canvasRect)
                rect.SetParent(canvasRect, false);

            image.gameObject.SetActive(true);
            image.enabled = true;
            rect.SetAsLastSibling();
        }

        private void RestoreFullscreenCharacterSprites()
        {
            RestoreFullscreenCharacterSprite(playerBattleSpriteImage, ref playerFullscreenSpriteState);
            RestoreFullscreenCharacterSprite(opponentBattleSpriteImage, ref opponentFullscreenSpriteState);
        }

        private static void RestoreFullscreenCharacterSprite(Image image, ref BattleFullscreenSpriteState state)
        {
            RectTransform rect = image != null ? image.rectTransform : null;
            if (rect == null || !state.HasValue)
                return;

            if (state.Parent != null)
                rect.SetParent(state.Parent, false);

            rect.anchorMin = state.AnchorMin;
            rect.anchorMax = state.AnchorMax;
            rect.pivot = state.Pivot;
            rect.anchoredPosition = state.AnchoredPosition;
            rect.sizeDelta = state.SizeDelta;
            rect.localScale = state.LocalScale;
            rect.localRotation = state.LocalRotation;

            if (state.Parent != null)
                rect.SetSiblingIndex(Mathf.Clamp(state.SiblingIndex, 0, state.Parent.childCount - 1));

            state = default;
        }

        private void ApplyFullscreenCharacterSpriteLayouts()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvas == null || canvasRect == null)
                return;

            if (!TryResolveBoardBoundsInCanvas(playerBoard, canvas, canvasRect, out Rect bounds))
                return;

            float barHeight = ResolveFullscreenHorizontalHpBarHeight();
            float topSafe = barHeight + playerBoardFullscreenHpBarGap * 1.2f;
            float characterHeight = Mathf.Clamp(bounds.height * 0.48f, 132f, 310f);
            float characterWidth = characterHeight * 0.78f;
            float sideInset = Mathf.Clamp(bounds.width * 0.045f, 34f, 82f);
            float y = Mathf.Clamp(bounds.center.y - bounds.height * 0.08f, bounds.yMin + characterHeight * 0.5f + 16f, bounds.yMax - topSafe - characterHeight * 0.5f);

            ApplyFullscreenCharacterSpriteLayout(
                playerBattleSpriteImage,
                new Vector2(bounds.xMin + sideInset + characterWidth * 0.5f, y),
                new Vector2(characterWidth, characterHeight),
                false);

            ApplyFullscreenCharacterSpriteLayout(
                opponentBattleSpriteImage,
                new Vector2(bounds.xMax - sideInset - characterWidth * 0.5f, y),
                new Vector2(characterWidth, characterHeight),
                true);
        }

        private static void ApplyFullscreenCharacterSpriteLayout(Image image, Vector2 offset, Vector2 size, bool flipX)
        {
            RectTransform rect = image != null ? image.rectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            rect.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();
        }

        private CanvasGroup ResolveOpponentBoardFullscreenGroup()
        {
            if (opponentBoardFullscreenGroup == null && opponentBoard != null && opponentBoard.BoardArea != null)
                opponentBoardFullscreenGroup = GetOrAddCanvasGroup(opponentBoard.BoardArea.gameObject);

            return opponentBoardFullscreenGroup;
        }

        private CanvasGroup ResolvePlayerProfileFullscreenGroup()
        {
            if (playerProfileFullscreenGroup == null)
            {
                Transform root = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerHpBarFill, playerHpBarText, playerCardPortraitImage, playerTotemImage);
                if (root != null && !IsUnsafeProfileFullscreenRoot(root))
                    playerProfileFullscreenGroup = GetOrAddCanvasGroup(root.gameObject);
            }

            return playerProfileFullscreenGroup;
        }

        private CanvasGroup ResolveOpponentProfileFullscreenGroup()
        {
            if (opponentProfileFullscreenGroup == null)
            {
                Transform root = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText, opponentStatsText, opponentHpBarFill, opponentHpBarText, opponentCardPortraitImage, opponentTotemImage);
                if (root != null && !IsUnsafeProfileFullscreenRoot(root))
                    opponentProfileFullscreenGroup = GetOrAddCanvasGroup(root.gameObject);
            }

            return opponentProfileFullscreenGroup;
        }

        private void ClearUnsafeProfileFullscreenGroups()
        {
            if (IsUnsafeProfileFullscreenGroup(playerProfileFullscreenGroup))
            {
                SetCanvasGroupVisible(playerProfileFullscreenGroup, true, false);
                playerProfileFullscreenGroup = null;
            }

            if (IsUnsafeProfileFullscreenGroup(opponentProfileFullscreenGroup))
            {
                SetCanvasGroupVisible(opponentProfileFullscreenGroup, true, false);
                opponentProfileFullscreenGroup = null;
            }
        }

        private bool IsUnsafeProfileFullscreenGroup(CanvasGroup group)
        {
            return group != null && IsUnsafeProfileFullscreenRoot(group.transform);
        }

        private bool IsUnsafeProfileFullscreenRoot(Transform root)
        {
            if (root == null || root.GetComponent<Canvas>() != null)
                return true;

            return IsAncestorOrSelf(root, playerBoard != null ? playerBoard.BoardArea : null)
                   || IsAncestorOrSelf(root, playerBoard != null ? playerBoard.Root : null)
                   || IsAncestorOrSelf(root, opponentBoard != null ? opponentBoard.BoardArea : null)
                   || IsAncestorOrSelf(root, opponentBoard != null ? opponentBoard.Root : null);
        }

        private static bool IsAncestorOrSelf(Transform possibleAncestor, Transform node)
        {
            if (possibleAncestor == null || node == null)
                return false;

            Transform current = node;
            while (current != null)
            {
                if (current == possibleAncestor)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
                return null;

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private static void SetCanvasGroupVisible(CanvasGroup group, bool visible, bool interactableWhenVisible)
        {
            if (group == null)
                return;

            if (group.GetComponent<Canvas>() != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactableWhenVisible;
            group.blocksRaycasts = visible && interactableWhenVisible;
            group.ignoreParentGroups = false;
        }

        private void ApplyBattleBoardPanelBackground(RectTransform boardArea)
        {
            if (boardArea == null)
                return;

            Image image = boardArea.GetComponent<Image>();
            if (image == null)
                return;

            Sprite sprite = ResolveBattleBoardPanelSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.color = battleBoardPanelColor;
            image.type = battleBoardPanelImageType;
            image.raycastTarget = battleBoardPanelRaycastTarget;
            image.preserveAspect = false;
            image.enabled = true;
        }

        private Sprite ResolveBattleBoardPanelSprite()
        {
            if (battleBoardPanelSprite != null)
                return battleBoardPanelSprite;

            if (string.IsNullOrWhiteSpace(battleBoardPanelSpriteResourcePath))
                battleBoardPanelSpriteResourcePath = BattleBoardPanelResourcePath;

            Sprite[] sprites = Resources.LoadAll<Sprite>(battleBoardPanelSpriteResourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                battleBoardPanelSprite = sprites[0];
                return battleBoardPanelSprite;
            }

            battleBoardPanelSprite = Resources.Load<Sprite>(battleBoardPanelSpriteResourcePath);
            return battleBoardPanelSprite;
        }

        private void RefitBattleBoards()
        {
            playerBoard?.RefitIntoBoardArea();
            opponentBoard?.RefitIntoBoardArea();
            lastPlayerBoardAreaSize = playerBoard != null && playerBoard.BoardArea != null
                ? playerBoard.BoardArea.rect.size
                : Vector2.zero;
            lastOpponentBoardAreaSize = opponentBoard != null && opponentBoard.BoardArea != null
                ? opponentBoard.BoardArea.rect.size
                : Vector2.zero;
            LayoutPlayerBoardFullscreenButton();
        }

        private void RefitBoardsWhenAreaSizeChanges()
        {
            if (playerBoardFullscreen)
                return;

            Vector2 playerSize = playerBoard != null && playerBoard.BoardArea != null
                ? playerBoard.BoardArea.rect.size
                : Vector2.zero;
            Vector2 opponentSize = opponentBoard != null && opponentBoard.BoardArea != null
                ? opponentBoard.BoardArea.rect.size
                : Vector2.zero;

            if (playerSize == lastPlayerBoardAreaSize && opponentSize == lastOpponentBoardAreaSize)
                return;

            RefitBattleBoards();
        }

        private void RefitBattleHudWhenCanvasSizeChanges()
        {
            Vector2 canvasSize = ResolveBattleHudCanvasSize();
            if ((canvasSize - lastBattleHudCanvasSize).sqrMagnitude < 1f)
                return;

            ApplyBattleProfileLayout();
        }

        private int TotalCombatRounds => Mathf.Max(1, totalCombatRounds);

        private void BindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged -= HandleOpponentHpChanged;
            combatSystem.DamageApplied -= HandleDamageApplied;
            combatSystem.DamageResultApplied -= HandleDamageResultApplied;
            combatSystem.CombatFinished += HandleCombatFinished;
            combatSystem.PlayerHpChanged += HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged += HandleOpponentHpChanged;
            combatSystem.DamageApplied += HandleDamageApplied;
            combatSystem.DamageResultApplied += HandleDamageResultApplied;
        }

        private void UnbindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged -= HandleOpponentHpChanged;
            combatSystem.DamageApplied -= HandleDamageApplied;
            combatSystem.DamageResultApplied -= HandleDamageResultApplied;
        }

        private void HandlePlayerHpChanged(BattleCombatSystem _, int hp, int maxHp)
        {
            RefreshPlayerHpBar(hp, maxHp);
            if (scoreText != null)
                scoreText.text = GetScoreText();
        }

        private void HandleOpponentHpChanged(BattleCombatSystem _, int hp, int maxHp)
        {
            RefreshOpponentHpBar(hp, maxHp);
            if (scoreText != null)
                scoreText.text = GetScoreText();
        }

        private void HandleCombatFinished(BattleCombatSystem _, BattleBoardSide deadSide)
        {
            FinishRoundByDeadSide(deadSide, notifyLocalWifiPeer: true);
        }

        public void HandleLocalWifiRemoteRoundEnded(int senderRoundNumber, BattleBoardSide senderDeadSide)
        {
            if (senderRoundNumber != CurrentRoundNumber)
            {
                Log($"Ignored remote Wi-Fi round end for round {senderRoundNumber}; current round is {CurrentRoundNumber}");
                return;
            }

            FinishRoundByDeadSide(MapRemoteBattleSide(senderDeadSide), notifyLocalWifiPeer: false);
        }

        private void FinishRoundByDeadSide(BattleBoardSide deadSide, bool notifyLocalWifiPeer)
        {
            if (matchFinished || !matchRunning || roundEnding)
                return;

            roundEnding = true;
            StopBoardRoutines();

            if (botController != null)
                botController.StopBot();

            if (notifyLocalWifiPeer && IsLocalWifiBattleActive())
                LocalWifiBattleNetwork.I.SendRoundEnded(CurrentRoundNumber, deadSide);

            bool playerWonRound = deadSide == BattleBoardSide.Opponent;
            if (playerWonRound)
                playerRoundWins++;
            else
                opponentRoundWins++;

            string resultText = playerWonRound ? stateRoundWin : stateRoundLose;
            if (stateText != null)
                stateText.text = CurrentRoundNumber >= TotalCombatRounds
                    ? resultText
                    : $"{resultText}. Next round...";

            RefreshHud();
            RoundFinished?.Invoke(this, CurrentRoundNumber);
            NotifyStateChanged();

            Log(
                $"Round finished by HP | Round={CurrentRoundNumber}/{TotalCombatRounds} | " +
                $"PlayerWonRound={playerWonRound} | Score={playerRoundWins}:{opponentRoundWins}");

            if (CurrentRoundNumber >= TotalCombatRounds)
            {
                ForceFinishMatch(playerRoundWins >= opponentRoundWins);
                return;
            }

            StopRoundTransitionRoutine();
            roundTransitionRoutine = StartCoroutine(StartNextHpRoundRoutine());
        }

        private static BattleBoardSide MapRemoteBattleSide(BattleBoardSide remoteSide)
        {
            return remoteSide == BattleBoardSide.Player
                ? BattleBoardSide.Opponent
                : BattleBoardSide.Player;
        }

        private IEnumerator StartNextHpRoundRoutine()
        {
            if (botController != null)
                botController.StopBot();

            yield return new WaitForSeconds(ResolveRoundTransitionDelay());

            roundTransitionRoutine = null;

            if (matchFinished || !matchRunning)
            {
                roundEnding = false;
                NotifyStateChanged();
                yield break;
            }

            currentRoundIndex++;
            roundEnding = false;

            yield return PlayStartCountdownRoutine();

            if (matchFinished || !matchRunning)
            {
                NotifyStateChanged();
                yield break;
            }

            if (combatSystem != null)
                combatSystem.StartCombat();

            RefreshHud();
            BuildInitialBoards();

            Log($"Next HP round started | Round={CurrentRoundNumber}/{TotalCombatRounds}");
        }

        private float ResolveRoundTransitionDelay()
        {
            float delay = Mathf.Max(0.05f, nextRoundDelay);
            if (IsRealtimeOpponentBattleActive())
                delay = Mathf.Max(delay, 2.2f);

            return delay;
        }

        private void NotifyStateChanged()
        {
            MatchStateChanged?.Invoke(this);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleMatchController] {message}", this);
        }
    }
}
