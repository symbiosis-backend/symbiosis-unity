using System;
using System.Collections;
using MahjongGame.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongMenuUI : MonoBehaviour
    {
        private const string StoryEndlessDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject levelSelectPanel;

        [Header("Buttons Root")]
        [SerializeField] private GameObject storyButtonRoot;
        [SerializeField] private GameObject battleButtonRoot;
        [SerializeField] private GameObject resetProgressButtonRoot;

        [Header("Buttons")]
        [SerializeField] private Button storyButton;
        [SerializeField] private Button endlessButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private StoryModeCoordinator storyModeCoordinator;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameMahjong";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";

        [Header("Debug")]
        [SerializeField] private bool enableStoryMode = true;
        [SerializeField] private bool showResetProgressButton = false;
        [SerializeField] private bool debugLogs = true;

        [Header("Endless Lobby Polish")]
        [SerializeField] private bool hideShopInEndlessLobby = true;
        [SerializeField] private bool hideBattleCharacterPreviewInEndlessLobby = true;
        [SerializeField] private bool useSimpleBattleLobbyButton = true;
        [SerializeField] private string storyModeButtonText = "STORY MODE";
        [SerializeField] private string endlessButtonText = "ENDLESS";
        [SerializeField] private string battleLobbyButtonText = "BATTLE LOBBY";
        [SerializeField] private string mainBackButtonText = "BACK";
        [SerializeField] private string endlessLobbyBarResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_WidePanel";
        [SerializeField] private string endlessLobbyButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_LongButton";
        [SerializeField] private string endlessLobbyTopBarResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_TopStatusBar";
        [SerializeField] private string endlessLobbyBottomBarResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_BottomNavigationRail";
        [SerializeField] private string endlessLobbyModeTabsResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_ModeTabsTwoSegments";
        [SerializeField] private string endlessLobbyCurrencyBadgeResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_CurrencyBadgePill";
        [SerializeField] private string endlessLobbyPopupWindowResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_PopupWindowPanel";
        [SerializeField] private string endlessLobbySquareIconResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_SquareIconButton";
        [SerializeField] private string endlessLobbyStoryLevelCardResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_StoryLevelCard";
        [SerializeField] private string endlessLobbySettingsGearFrameResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_SettingsGearFrame";
        [SerializeField] private string endlessLobbyCloseButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_CloseButtonX";
        [SerializeField] private string endlessLobbyOzTileIconResourcePath = "Mahjong/Sprites/BattleTiles/OzTile";
        [SerializeField] private string boosterBagButtonText = "ÇANTA";
        [SerializeField] private string boosterBagHintIconResourcePath = "Mahjong/Sprites/Assist/NaytiParuIcon";
        [SerializeField] private string boosterBagShuffleIconResourcePath = "Mahjong/Sprites/Assist/PeremeshatiIcon";
        [SerializeField] private string boosterBagUndoIconResourcePath = "Mahjong/Sprites/Assist/HodnazadIcon";

        [Header("Cloud Effect")]
        [SerializeField] private bool useCloudEffect = true;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Sprite cloudSprite;

        [Header("Cloud Visual")]
        [SerializeField] private Vector2 cloudStartSize = new Vector2(260f, 140f);
        [SerializeField] private Vector2 cloudAbsorbSize = new Vector2(420f, 220f);

        [Header("Cloud Path")]
        [SerializeField] private float cloudStartOffsetX = 220f;
        [SerializeField] private float cloudExitOffsetX = 900f;
        [SerializeField] private float cloudRevealStartOffsetX = 900f;
        [SerializeField] private float cloudRevealExitOffsetX = 220f;
        [SerializeField] private float cloudYOffset = 0f;

        [Header("Cloud Timing")]
        [SerializeField] private float cloudFlyInTime = 0.28f;
        [SerializeField] private float absorbTime = 0.18f;
        [SerializeField] private float cloudHoldTime = 0.04f;
        [SerializeField] private float cloudFlyOutTime = 0.55f;
        [SerializeField] private float revealFlyInTime = 0.32f;
        [SerializeField] private float revealTime = 0.20f;
        [SerializeField] private float revealFlyOutTime = 0.38f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Cloud Rotation")]
        [SerializeField] private bool rotateCloudWhileMoving = true;
        [SerializeField] private float cloudRotateSpeed = 360f;
        [SerializeField] private bool reverseRotationOnBackFlight = false;

        [Header("Overlay")]
        [SerializeField] private string overlayName = "CloudEffectOverlay";
        [SerializeField] private int overlaySortingOrder = 9999;
        [SerializeField] private string overlaySortingLayerName = "UI";

        private Canvas overlayCanvas;
        private RectTransform overlayRoot;
        private CloudFxRunner fxRunner;
        private bool transitionPlaying;
        private Image endlessBottomBarImage;
        private Image endlessTopBarImage;
        private Image endlessModeTabsImage;
        private RectTransform endlessTopBarStatsRoot;
        private TMP_Text endlessOzTileText;
        private Image endlessOzTileIcon;
        private Button boosterBagButton;
        private RectTransform settingsGearRect;
        private GameObject boosterBagOverlay;
        private CanvasGroup boosterBagCanvasGroup;
        private TMP_Text boosterBagHintCountText;
        private TMP_Text boosterBagShuffleCountText;
        private TMP_Text boosterBagUndoCountText;
        private TMP_Text boosterBagStatusText;
        private Sprite cachedEndlessLobbyBarSprite;
        private Sprite cachedEndlessLobbyButtonSprite;
        private Sprite cachedEndlessLobbyTopBarSprite;
        private Sprite cachedEndlessLobbyBottomBarSprite;
        private Sprite cachedEndlessLobbyModeTabsSprite;
        private Sprite cachedEndlessLobbyCurrencyBadgeSprite;
        private Sprite cachedEndlessLobbyPopupWindowSprite;
        private Sprite cachedEndlessLobbySquareIconSprite;
        private Sprite cachedEndlessLobbyStoryLevelCardSprite;
        private Sprite cachedEndlessLobbySettingsGearFrameSprite;
        private Sprite cachedEndlessLobbyCloseButtonSprite;
        private Sprite cachedEndlessOzTileIconSprite;
        private Sprite cachedBoosterBagHintIconSprite;
        private Sprite cachedBoosterBagShuffleIconSprite;
        private Sprite cachedBoosterBagUndoIconSprite;
        private bool boosterBagRewardedAdInProgress;
        private bool boosterBagPurchaseInProgress;
        private bool storyLevelSelectOpen;
        private Vector2 lastResponsiveLobbyScreenSize = new Vector2(-1f, -1f);
        private bool lastResponsiveLobbyPortrait;
        private const float SettingsGearRotateSpeed = 28f;

        private static readonly Color LobbyButtonNormalColor = new Color(0.02f, 0.11f, 0.075f, 0.92f);
        private static readonly Color LobbyButtonHighlightedColor = new Color(0.06f, 0.23f, 0.15f, 0.96f);
        private static readonly Color LobbyButtonPressedColor = new Color(0.01f, 0.07f, 0.05f, 0.98f);
        private static readonly Color LobbyButtonTextColor = new Color(1f, 0.9f, 0.46f, 1f);
        private static readonly Color LobbyButtonOutlineColor = new Color(0.96f, 0.72f, 0.22f, 0.88f);
        private static readonly Vector2 EndlessLobbyBottomBarSize = new Vector2(0f, 108f);
        private static readonly Vector2 EndlessLobbyTopBarSize = new Vector2(0f, 133f);
        private static readonly Vector2 EndlessLobbyModeTabsSize = new Vector2(900f, 300f);
        private static readonly Vector2 EndlessLobbyBottomBarPosition = new Vector2(0f, -34f);
        private static readonly Vector2 EndlessLobbyTopBarPosition = new Vector2(0f, 22f);
        private static readonly Vector2 EndlessLobbyModeTabsPosition = new Vector2(0f, -72f);
        private static readonly Vector2 EndlessLobbyBattleButtonPosition = new Vector2(0f, -452f);
        private static readonly Vector2 EndlessLobbyBagButtonPosition = new Vector2(-690f, -452f);
        private static readonly Vector2 EndlessLobbyBackButtonPosition = new Vector2(690f, -452f);
        private static readonly Vector2 EndlessLobbyStoryButtonPosition = new Vector2(-374f, -72f);
        private static readonly Vector2 EndlessLobbyEndlessButtonPosition = new Vector2(374f, -72f);
        private static readonly Vector2 EndlessLobbyModeButtonSize = new Vector2(690f, 248f);
        private static readonly Vector2 EndlessLobbyPrimaryButtonSize = new Vector2(420f, 104f);
        private static readonly Vector2 EndlessLobbyUtilityButtonSize = new Vector2(300f, 92f);
        private static readonly Vector2 EndlessLobbyLevelButtonSize = new Vector2(320f, 86f);
        private static readonly Vector2 BoosterBagWindowSize = new Vector2(1500f, 840f);
        private static readonly Vector2 BoosterBagCardSize = new Vector2(360f, 390f);
        private const string BoosterBagPackProductId = "mahjong_booster_pack";

        private struct EndlessLobbyLayout
        {
            public bool Portrait;
            public Vector2 BottomBarSize;
            public Vector2 TopBarSize;
            public Vector2 ModeTabsSize;
            public Vector2 BottomBarPosition;
            public Vector2 TopBarPosition;
            public Vector2 ModeTabsPosition;
            public Vector2 StoryButtonPosition;
            public Vector2 EndlessButtonPosition;
            public Vector2 BattleButtonPosition;
            public Vector2 BagButtonPosition;
            public Vector2 BackButtonPosition;
            public Vector2 ModeButtonSize;
            public Vector2 PrimaryButtonSize;
            public Vector2 UtilityButtonSize;
            public Vector2 TopBarStatsPosition;
            public Vector2 TopBarStatsSize;
            public Vector2 LevelBadgePosition;
            public Vector2 ExpBadgePosition;
            public Vector2 EnergyBadgePosition;
            public Vector2 OzTileBadgePosition;
            public Vector2 LevelBadgeSize;
            public Vector2 ExpBadgeSize;
            public Vector2 EnergyBadgeSize;
            public Vector2 OzTileBadgeSize;
            public Vector2 LevelTextPosition;
            public Vector2 ExpTextPosition;
            public Vector2 EnergyTextPosition;
            public Vector2 OzTileTextPosition;
            public Vector2 OzTileIconPosition;
            public Vector2 StatsTextSize;
            public Vector2 OzTileTextSize;
            public Vector2 OzTileIconSize;
        }

        private void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            EnsureOverlay();
            EnsureStoryModeCoordinator();
        }

        private void OnEnable()
        {
            CurrencyService.CurrencyChanged -= RefreshEndlessTopBarValues;
            CurrencyService.CurrencyChanged += RefreshEndlessTopBarValues;
            ProfileService.ProfileChanged -= RefreshEndlessTopBarValues;
            ProfileService.ProfileChanged += RefreshEndlessTopBarValues;
            EnergyService.EnergyChanged -= RefreshEndlessTopBarValues;
            EnergyService.EnergyChanged += RefreshEndlessTopBarValues;
            RefreshButtons();
            ShowMainPanelImmediate();
            ApplyEndlessLobbyPolish();
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= RefreshEndlessTopBarValues;
            ProfileService.ProfileChanged -= RefreshEndlessTopBarValues;
            EnergyService.EnergyChanged -= RefreshEndlessTopBarValues;
        }

        private void Update()
        {
            if (settingsGearRect != null && settingsGearRect.gameObject.activeInHierarchy)
                settingsGearRect.Rotate(0f, 0f, -SettingsGearRotateSpeed * Time.unscaledDeltaTime);

            RefreshResponsiveLobbyLayoutIfNeeded();
        }

        private void Start()
        {
            RefreshButtons();
            ShowMainPanelImmediate();
            ApplyEndlessLobbyPolish();
            StartCoroutine(ApplyEndlessLobbyPolishNextFrame());
        }

        public void RefreshButtons()
        {
            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(enableStoryMode);

            if (battleButtonRoot != null)
                battleButtonRoot.SetActive(true);

            if (resetProgressButtonRoot != null)
                resetProgressButtonRoot.SetActive(showResetProgressButton);

            RestoreButtonsInTree(mainPanel);
            RestoreButtonsInTree(levelSelectPanel);

            RestoreButtonVisual(storyButton);
            RestoreButtonVisual(endlessButton);
            RestoreButtonVisual(battleButton);
            RestoreButtonVisual(backButton);
            RestoreButtonVisual(resetButton);

            if (!enableStoryMode)
                HideStoryMode();

            if (!showResetProgressButton)
                HideResetProgressButton();

            ApplyEndlessLobbyPolish();
            Canvas.ForceUpdateCanvases();
            Log($"Menu ready | StoryScene={gameSceneName} | BattleLobbyScene={battleLobbySceneName}");
        }

        public void OnClickStory()
        {
            Log("Story button clicked");
            if (!enableStoryMode)
                return;

            if (transitionPlaying)
                return;

            if (useCloudEffect && storyButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenReveal(storyButton, () =>
                {
                    ShowLevelSelectImmediate();
                });
                return;
            }

            ShowLevelSelectImmediate();
        }

        public void OnClickBattle()
        {
            Log("Battle button clicked");
            if (transitionPlaying)
                return;

            MahjongSession.Clear();

            if (useCloudEffect && battleButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenComplete(battleButton, () => LoadSceneWithDoor(battleLobbySceneName));
                return;
            }

            LoadSceneWithDoor(battleLobbySceneName);
        }

        public void OnClickEndless()
        {
            Log("Endless button clicked");
            if (transitionPlaying)
                return;

            MahjongSession.StartEndless(1);

            Button clickedButton = endlessButton != null ? endlessButton : GetCurrentSelectedButton();
            if (useCloudEffect && clickedButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenComplete(clickedButton, () => LoadSceneWithDoor(gameSceneName));
                return;
            }

            LoadSceneWithDoor(gameSceneName);
        }

        public void OnClickBackFromLevels()
        {
            Log("Back button clicked");
            if (transitionPlaying)
                return;

            if (storyLevelSelectOpen && storyModeCoordinator != null && storyModeCoordinator.TryNavigateBack())
                return;

            if (useCloudEffect && storyButton != null && cloudSprite != null)
            {
                PlayReturnToMainWithStoryReveal();
                return;
            }

            ShowMainPanelImmediate();
        }

        public void OnClickLevel(int level)
        {
            Log($"Level button clicked: {level}");
            if (transitionPlaying)
                return;

            LaunchStoryStage(level, 1, GetCurrentSelectedButton());
        }

        public bool OnClickStoryStage(int level, int stage, Button clickedButton, MahjongStoryDifficulty difficulty = MahjongStoryDifficulty.Medium)
        {
            Log($"Story stage clicked: level={level} stage={stage} difficulty={difficulty}");
            if (transitionPlaying)
                return false;

            LaunchStoryStage(level, stage, clickedButton, difficulty);
            return true;
        }

        private void LaunchStoryStage(int level, int stage, Button clickedButton, MahjongStoryDifficulty difficulty = MahjongStoryDifficulty.Medium)
        {
            MahjongSession.StartStory(level, stage, difficulty);

            if (clickedButton == null)
                clickedButton = GetCurrentSelectedButton();

            if (useCloudEffect && clickedButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenComplete(clickedButton, () => LoadSceneWithDoor(gameSceneName));
                return;
            }

            LoadSceneWithDoor(gameSceneName);
        }

        public void OnClickResetProgress()
        {
            Log("Reset button clicked");
            if (transitionPlaying)
                return;

            if (useCloudEffect && resetButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenReveal(resetButton, ResetProgressAndRefresh);
                return;
            }

            ResetProgressAndRefresh();
        }

        private void ResetProgressAndRefresh()
        {
            MahjongProgress.ResetAll();
            MahjongSession.Clear();
            RefreshButtons();
            ShowMainPanelImmediate();
        }

        private void ShowMainPanelImmediate()
        {
            storyLevelSelectOpen = false;
            RestoreButtonsInTree(mainPanel);

            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(enableStoryMode);

            if (battleButtonRoot != null)
                battleButtonRoot.SetActive(true);

            if (resetProgressButtonRoot != null)
                resetProgressButtonRoot.SetActive(showResetProgressButton);

            SetPanelSafe(mainPanel, true);
            SetPanel(levelSelectPanel, false);
            SetMainLobbyControlsVisible(true);

            RestoreButtonVisual(storyButton);
            RestoreButtonVisual(endlessButton);
            RestoreButtonVisual(battleButton);
            RestoreButtonVisual(backButton);
            RestoreButtonVisual(resetButton);

            if (!enableStoryMode)
                HideStoryMode();

            if (!showResetProgressButton)
                HideResetProgressButton();

            ApplyEndlessLobbyPolish();
        }

        private void ShowLevelSelectImmediate()
        {
            storyLevelSelectOpen = true;
            SetPanelSafe(mainPanel, true);
            SetMainLobbyControlsVisible(false);
            RestoreButtonsInTree(levelSelectPanel);
            SetPanel(levelSelectPanel, true);

            if (levelSelectPanel != null)
                levelSelectPanel.transform.SetAsLastSibling();

            ShowStoryModeCoordinator();
            ApplyEndlessLobbyPolish();
            Canvas.ForceUpdateCanvases();
        }

        private void HideStoryMode()
        {
            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(false);

            if (storyButton != null)
                storyButton.gameObject.SetActive(false);

            SetPanel(levelSelectPanel, false);
        }

        private void HideResetProgressButton()
        {
            if (resetProgressButtonRoot != null)
                resetProgressButtonRoot.SetActive(false);

            if (resetButton != null)
                resetButton.gameObject.SetActive(false);
        }

        private IEnumerator ApplyEndlessLobbyPolishNextFrame()
        {
            yield return null;
            ApplyEndlessLobbyPolish();
        }

        private void ApplyEndlessLobbyPolish()
        {
            ConfigureResponsiveLobbyCanvas();
            EndlessLobbyLayout layout = ResolveEndlessLobbyLayout();
            HideEndlessModeTabs();

            if (hideShopInEndlessLobby)
                HideShopUi();

            if (hideBattleCharacterPreviewInEndlessLobby)
                HideBattleCharacterPreview();

            HideOldEndlessTopBar();
            EnsureEndlessLobbyBars(layout);
            RefreshEndlessTopBarValues();
            EnsureEndlessButton();
            ApplyEndlessSettingsGearFrame();
            EnsureBoosterBagUi();
            RefreshBoosterBagCounts();

            if (storyLevelSelectOpen)
            {
                SetEndlessLobbyChromeVisible(false);
                SetMainLobbyControlsVisible(false);
                return;
            }

            SetEndlessLobbyChromeVisible(true);

            if (enableStoryMode)
                ApplyLobbyButtonStyle(storyButton, storyModeButtonText, layout.ModeButtonSize, layout.StoryButtonPosition, true);

            ApplyLobbyButtonStyle(endlessButton, endlessButtonText, layout.ModeButtonSize, layout.EndlessButtonPosition, true);
            AttachEndlessLobbyInfoHints();

            if (useSimpleBattleLobbyButton)
                ApplyLobbyButtonStyle(battleButton, battleLobbyButtonText, layout.PrimaryButtonSize, layout.BattleButtonPosition, true);

            ApplyLobbyButtonStyle(boosterBagButton, boosterBagButtonText, layout.UtilityButtonSize, layout.BagButtonPosition, false);

            Button mainBackButton = FindButtonByName("Btn_Back");
            ApplyLobbyButtonStyle(mainBackButton, mainBackButtonText, layout.UtilityButtonSize, layout.BackButtonPosition, false);

            ApplyLobbyButtonStyle(backButton, mainBackButtonText, layout.UtilityButtonSize, layout.BackButtonPosition, false);
            ArrangeStoryLevelButtons();
        }

        private void ConfigureResponsiveLobbyCanvas()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
                return;

            CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                MainLobbyUiCoordinator.ConfigureResponsiveLobbyScaler(scaler);

            lastResponsiveLobbyScreenSize = MainLobbyUiCoordinator.ResolveScreenSize();
            lastResponsiveLobbyPortrait = MainLobbyUiCoordinator.IsPortraitLayout(lastResponsiveLobbyScreenSize);
        }

        private void RefreshResponsiveLobbyLayoutIfNeeded()
        {
            Vector2 screenSize = MainLobbyUiCoordinator.ResolveScreenSize();
            bool portrait = MainLobbyUiCoordinator.IsPortraitLayout(screenSize);
            if (screenSize == lastResponsiveLobbyScreenSize && portrait == lastResponsiveLobbyPortrait)
                return;

            ApplyEndlessLobbyPolish();
            Canvas.ForceUpdateCanvases();
        }

        private EndlessLobbyLayout ResolveEndlessLobbyLayout()
        {
            Vector2 screenSize = MainLobbyUiCoordinator.ResolveScreenSize();
            bool portrait = MainLobbyUiCoordinator.IsPortraitLayout(screenSize);

            if (!portrait)
            {
                return new EndlessLobbyLayout
                {
                    Portrait = false,
                    BottomBarSize = EndlessLobbyBottomBarSize,
                    TopBarSize = EndlessLobbyTopBarSize,
                    ModeTabsSize = EndlessLobbyModeTabsSize,
                    BottomBarPosition = EndlessLobbyBottomBarPosition,
                    TopBarPosition = EndlessLobbyTopBarPosition,
                    ModeTabsPosition = EndlessLobbyModeTabsPosition,
                    StoryButtonPosition = EndlessLobbyStoryButtonPosition,
                    EndlessButtonPosition = EndlessLobbyEndlessButtonPosition,
                    BattleButtonPosition = EndlessLobbyBattleButtonPosition,
                    BagButtonPosition = EndlessLobbyBagButtonPosition,
                    BackButtonPosition = EndlessLobbyBackButtonPosition,
                    ModeButtonSize = EndlessLobbyModeButtonSize,
                    PrimaryButtonSize = EndlessLobbyPrimaryButtonSize,
                    UtilityButtonSize = EndlessLobbyUtilityButtonSize,
                    TopBarStatsPosition = new Vector2(0f, -74f),
                    TopBarStatsSize = new Vector2(360f, 86f),
                    LevelBadgePosition = new Vector2(-430f, 0f),
                    ExpBadgePosition = new Vector2(-145f, 0f),
                    EnergyBadgePosition = new Vector2(165f, 0f),
                    OzTileBadgePosition = Vector2.zero,
                    LevelBadgeSize = new Vector2(238f, 70f),
                    ExpBadgeSize = new Vector2(288f, 70f),
                    EnergyBadgeSize = new Vector2(286f, 70f),
                    OzTileBadgeSize = new Vector2(270f, 70f),
                    LevelTextPosition = new Vector2(-430f, 0f),
                    ExpTextPosition = new Vector2(-145f, 0f),
                    EnergyTextPosition = new Vector2(165f, 0f),
                    OzTileTextPosition = new Vector2(34f, 0f),
                    OzTileIconPosition = new Vector2(-92f, 0f),
                    StatsTextSize = new Vector2(230f, 58f),
                    OzTileTextSize = new Vector2(190f, 58f),
                    OzTileIconSize = new Vector2(54f, 54f)
                };
            }

            return new EndlessLobbyLayout
            {
                Portrait = true,
                BottomBarSize = new Vector2(0f, 150f),
                TopBarSize = new Vector2(0f, 180f),
                ModeTabsSize = new Vector2(930f, 620f),
                BottomBarPosition = new Vector2(0f, -34f),
                TopBarPosition = new Vector2(0f, 18f),
                ModeTabsPosition = new Vector2(0f, 250f),
                StoryButtonPosition = new Vector2(0f, 405f),
                EndlessButtonPosition = new Vector2(0f, 120f),
                BattleButtonPosition = new Vector2(0f, -210f),
                BagButtonPosition = new Vector2(-250f, -970f),
                BackButtonPosition = new Vector2(250f, -970f),
                ModeButtonSize = new Vector2(760f, 250f),
                PrimaryButtonSize = new Vector2(560f, 132f),
                UtilityButtonSize = new Vector2(390f, 116f),
                TopBarStatsPosition = new Vector2(0f, -94f),
                TopBarStatsSize = new Vector2(420f, 86f),
                LevelBadgePosition = new Vector2(-252f, 32f),
                ExpBadgePosition = new Vector2(252f, 32f),
                EnergyBadgePosition = new Vector2(-252f, -40f),
                OzTileBadgePosition = Vector2.zero,
                LevelBadgeSize = new Vector2(330f, 66f),
                ExpBadgeSize = new Vector2(410f, 66f),
                EnergyBadgeSize = new Vector2(410f, 66f),
                OzTileBadgeSize = new Vector2(330f, 66f),
                LevelTextPosition = new Vector2(-252f, 32f),
                ExpTextPosition = new Vector2(252f, 32f),
                EnergyTextPosition = new Vector2(-252f, -40f),
                OzTileTextPosition = new Vector2(44f, 0f),
                OzTileIconPosition = new Vector2(-106f, 0f),
                StatsTextSize = new Vector2(390f, 54f),
                OzTileTextSize = new Vector2(250f, 54f),
                OzTileIconSize = new Vector2(48f, 48f)
            };
        }

        private void SetMainLobbyControlsVisible(bool visible)
        {
            SetButtonVisible(storyButton, visible && enableStoryMode);
            SetButtonVisible(endlessButton, visible);
            SetButtonVisible(battleButton, visible);
            SetButtonVisible(boosterBagButton, visible);
            SetButtonVisible(backButton, visible);

            Button mainBackButton = FindButtonByName("Btn_Back");
            SetButtonVisible(mainBackButton, visible);
        }

        private void SetEndlessLobbyChromeVisible(bool visible)
        {
            if (endlessBottomBarImage != null)
                endlessBottomBarImage.gameObject.SetActive(false);

            if (endlessTopBarImage != null)
                endlessTopBarImage.gameObject.SetActive(false);

            if (endlessTopBarStatsRoot != null)
                endlessTopBarStatsRoot.gameObject.SetActive(visible);

            if (boosterBagButton != null)
                boosterBagButton.gameObject.SetActive(visible);

            if (settingsGearRect != null)
                settingsGearRect.gameObject.SetActive(visible);
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        private void EnsureStoryModeCoordinator()
        {
            if (storyModeCoordinator == null && levelSelectPanel != null)
                storyModeCoordinator = levelSelectPanel.GetComponent<StoryModeCoordinator>();

            if (storyModeCoordinator == null && levelSelectPanel != null)
                storyModeCoordinator = levelSelectPanel.AddComponent<StoryModeCoordinator>();

            if (storyModeCoordinator != null)
                storyModeCoordinator.Initialize(this, levelSelectPanel, rootCanvas);
        }

        private void ShowStoryModeCoordinator()
        {
            EnsureStoryModeCoordinator();
            if (storyModeCoordinator != null)
                storyModeCoordinator.ShowChapters();
        }

        private void AttachEndlessLobbyInfoHints()
        {
            if (enableStoryMode && storyButton != null)
                MainInfoHintTarget.Attach(storyButton, "mahjong.lobby.story.title", "mahjong.lobby.story.body");

            if (endlessButton != null)
                MainInfoHintTarget.Attach(endlessButton, "mahjong.lobby.endless.title", "mahjong.lobby.endless.body");
        }

        private void ApplyEndlessSettingsGearFrame()
        {
            Button settingsButton = FindButtonByName("BtnOpenSettings");
            Sprite sprite = LoadEndlessLobbySettingsGearFrameSprite();
            if (settingsButton == null || sprite == null)
                return;

            Image image = settingsButton.image;
            if (image == null)
                image = settingsButton.GetComponent<Image>();

            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            settingsGearRect = settingsButton.transform as RectTransform;
            if (settingsGearRect != null)
            {
                bool portrait = MainLobbyUiCoordinator.IsPortraitLayout(MainLobbyUiCoordinator.ResolveScreenSize());
                Vector2 anchoredPosition = settingsGearRect.anchoredPosition;
                if (portrait)
                {
                    settingsGearRect.anchorMin = new Vector2(1f, 1f);
                    settingsGearRect.anchorMax = new Vector2(1f, 1f);
                    settingsGearRect.sizeDelta = new Vector2(132f, 132f);
                    anchoredPosition = new Vector2(-92f, -96f);
                }

                settingsGearRect.pivot = new Vector2(0.5f, 0.5f);
                settingsGearRect.anchoredPosition = anchoredPosition;
            }
        }

        private void HideOldEndlessTopBar()
        {
            SetObjectActiveByName("TopBar", false);
        }

        private void EnsureEndlessLobbyBars(EndlessLobbyLayout layout)
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
                return;

            SetObjectActiveByName("EndlessBottomBar", false);
            SetObjectActiveByName("EndlessTopBar", false);

            if (endlessBottomBarImage == null)
            {
                GameObject existingBottom = FindSceneObjectByName("EndlessBottomBar");
                endlessBottomBarImage = existingBottom != null ? existingBottom.GetComponent<Image>() : null;
            }

            if (endlessTopBarImage == null)
            {
                GameObject existingTop = FindSceneObjectByName("EndlessTopBar");
                endlessTopBarImage = existingTop != null ? existingTop.GetComponent<Image>() : null;
            }

            if (endlessBottomBarImage != null)
                endlessBottomBarImage.gameObject.SetActive(false);

            if (endlessTopBarImage != null)
                endlessTopBarImage.gameObject.SetActive(false);

            HideEndlessModeTabs();
            EnsureEndlessTopBarStats(layout);
        }

        private Image EnsureEndlessLobbyBar(string objectName, bool topBar, Image current, Sprite sprite, Vector2 size, Vector2 position)
        {
            if (current == null)
            {
                GameObject existing = FindSceneObjectByName(objectName);
                current = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (current == null)
            {
                GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(rootCanvas.transform, false);
                panel.layer = rootCanvas.gameObject.layer;
                current = panel.GetComponent<Image>();
            }

            current.sprite = sprite;
            current.type = Image.Type.Simple;
            current.preserveAspect = false;
            current.color = Color.white;
            current.raycastTarget = false;
            current.gameObject.SetActive(true);

            RectTransform rect = current.rectTransform;
            rect.anchorMin = topBar ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            rect.anchorMax = topBar ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            rect.pivot = topBar ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, position.y);
            rect.sizeDelta = new Vector2(0f, size.y);
            rect.localScale = Vector3.one;
            PlaceEndlessBarAboveBackground(rect);
            return current;
        }

        private void EnsureEndlessModeTabs()
        {
            Sprite tabsSprite = LoadEndlessLobbyModeTabsSprite();
            if (tabsSprite == null || rootCanvas == null)
                return;

            if (endlessModeTabsImage == null)
            {
                GameObject existing = FindSceneObjectByName("EndlessModeTabsPanel");
                endlessModeTabsImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (endlessModeTabsImage == null)
            {
                GameObject panel = new GameObject("EndlessModeTabsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(rootCanvas.transform, false);
                panel.layer = rootCanvas.gameObject.layer;
                endlessModeTabsImage = panel.GetComponent<Image>();
            }

            endlessModeTabsImage.sprite = tabsSprite;
            endlessModeTabsImage.type = Image.Type.Simple;
            endlessModeTabsImage.preserveAspect = false;
            endlessModeTabsImage.color = Color.white;
            endlessModeTabsImage.raycastTarget = false;
            endlessModeTabsImage.gameObject.SetActive(true);

            RectTransform rect = endlessModeTabsImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = EndlessLobbyModeTabsPosition;
            rect.sizeDelta = EndlessLobbyModeTabsSize;
            rect.localScale = Vector3.one;
            PlaceEndlessBarAboveBackground(rect);
            PlaceModeTabsBehindModeButtons(rect);
        }

        private void HideEndlessModeTabs()
        {
            SetObjectActiveByName("EndlessModeTabsPanel", false);

            if (endlessModeTabsImage == null)
            {
                GameObject existing = FindSceneObjectByName("EndlessModeTabsPanel");
                endlessModeTabsImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (endlessModeTabsImage != null)
                endlessModeTabsImage.gameObject.SetActive(false);

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && string.Equals(candidate.name, "EndlessModeTabsPanel", StringComparison.Ordinal))
                    candidate.gameObject.SetActive(false);
            }
        }

        private void PlaceModeTabsBehindModeButtons(RectTransform tabsRect)
        {
            if (tabsRect == null || rootCanvas == null)
                return;

            int targetIndex = rootCanvas.transform.childCount - 1;
            RectTransform storyRect = storyButton != null ? storyButton.transform as RectTransform : null;
            RectTransform endlessRect = endlessButton != null ? endlessButton.transform as RectTransform : null;
            if ((storyRect == null || storyRect.parent != rootCanvas.transform) &&
                (endlessRect == null || endlessRect.parent != rootCanvas.transform))
            {
                return;
            }

            if (storyRect != null && storyRect.parent == rootCanvas.transform)
                targetIndex = Mathf.Min(targetIndex, storyRect.GetSiblingIndex());
            if (endlessRect != null && endlessRect.parent == rootCanvas.transform)
                targetIndex = Mathf.Min(targetIndex, endlessRect.GetSiblingIndex());

            tabsRect.SetSiblingIndex(Mathf.Max(0, targetIndex));
        }

        private void PlaceEndlessBarAboveBackground(RectTransform barRect)
        {
            if (barRect == null || rootCanvas == null)
                return;

            Transform canvasTransform = rootCanvas.transform;
            int backgroundIndex = -1;

            for (int i = 0; i < canvasTransform.childCount; i++)
            {
                Transform child = canvasTransform.GetChild(i);
                if (child == null || child == barRect || child == endlessTopBarStatsRoot)
                    continue;

                if (string.Equals(child.name, "EndlessTopBar", StringComparison.Ordinal) ||
                    string.Equals(child.name, "EndlessBottomBar", StringComparison.Ordinal))
                    continue;

                RectTransform childRect = child as RectTransform;
                Image childImage = child.GetComponent<Image>();
                if (childRect == null || childImage == null || childImage.sprite == null)
                    continue;

                bool stretched = childRect.anchorMin == Vector2.zero && childRect.anchorMax == Vector2.one;
                bool large = childRect.rect.width >= rootCanvas.pixelRect.width * 0.75f ||
                             childRect.rect.height >= rootCanvas.pixelRect.height * 0.75f;

                if (stretched || large)
                    backgroundIndex = Mathf.Max(backgroundIndex, i);
            }

            int targetIndex = backgroundIndex >= 0 ? backgroundIndex + 1 : 1;
            targetIndex = Mathf.Clamp(targetIndex, 0, canvasTransform.childCount - 1);
            barRect.SetSiblingIndex(targetIndex);
        }

        private void EnsureEndlessTopBarStats(EndlessLobbyLayout layout)
        {
            if (rootCanvas == null)
                return;

            if (endlessTopBarStatsRoot == null)
            {
                GameObject existing = FindSceneObjectByName("EndlessTopBarRuntimeTextRoot");
                endlessTopBarStatsRoot = existing != null ? existing.transform as RectTransform : null;
            }

            if (endlessTopBarStatsRoot == null)
            {
                GameObject root = new GameObject("EndlessTopBarRuntimeTextRoot", typeof(RectTransform));
                root.transform.SetParent(rootCanvas.transform, false);
                root.layer = rootCanvas.gameObject.layer;
                endlessTopBarStatsRoot = root.GetComponent<RectTransform>();
            }

            endlessTopBarStatsRoot.gameObject.SetActive(true);
            endlessTopBarStatsRoot.anchorMin = new Vector2(0.5f, 1f);
            endlessTopBarStatsRoot.anchorMax = new Vector2(0.5f, 1f);
            endlessTopBarStatsRoot.pivot = new Vector2(0.5f, 0.5f);
            endlessTopBarStatsRoot.anchoredPosition = layout.TopBarStatsPosition;
            endlessTopBarStatsRoot.sizeDelta = layout.TopBarStatsSize;
            endlessTopBarStatsRoot.localScale = Vector3.one;
            endlessTopBarStatsRoot.SetAsLastSibling();

            Sprite badgeSprite = LoadEndlessLobbyCurrencyBadgeSprite();
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessLevelBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessExpBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessEnergyBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessLevelText", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessExpText", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessEnergyText", false);

            EnsureEndlessTopBarBadge(endlessTopBarStatsRoot, "EndlessOzTileBadge", layout.OzTileBadgePosition, layout.OzTileBadgeSize, badgeSprite);

            endlessOzTileText = EnsureEndlessTopBarText(endlessTopBarStatsRoot, endlessOzTileText, "EndlessOzTileText", layout.OzTileTextPosition, layout.OzTileTextSize);
            endlessOzTileIcon = EnsureEndlessTopBarIcon(endlessTopBarStatsRoot, endlessOzTileIcon, "EndlessOzTileIcon", layout.OzTileIconPosition, layout.OzTileIconSize, LoadEndlessOzTileIconSprite());
        }

        private static void SetEndlessTopBarElementVisible(RectTransform parent, string objectName, bool visible)
        {
            Transform existing = parent != null ? parent.Find(objectName) : null;
            if (existing != null)
                existing.gameObject.SetActive(visible);
        }

        private static Image EnsureEndlessTopBarBadge(RectTransform parent, string objectName, Vector2 position, Vector2 size, Sprite sprite)
        {
            if (parent == null || sprite == null)
                return null;

            Transform existing = parent.Find(objectName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject badgeObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObject.transform.SetParent(parent, false);
                badgeObject.layer = parent.gameObject.layer;
                image = badgeObject.GetComponent<Image>();
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            image.gameObject.SetActive(true);
            image.transform.SetAsFirstSibling();
            return image;
        }

        private TMP_Text EnsureEndlessTopBarText(RectTransform parent, TMP_Text current, string objectName, Vector2 position, Vector2 size)
        {
            if (current == null)
            {
                Transform existing = parent.Find(objectName);
                current = existing != null ? existing.GetComponent<TMP_Text>() : null;
            }

            if (current == null)
            {
                GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                textObject.layer = parent.gameObject.layer;
                current = textObject.GetComponent<TextMeshProUGUI>();
                current.raycastTarget = false;
            }

            RectTransform rect = current.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            MainLobbyButtonStyle.ApplyFont(current);
            current.alignment = TextAlignmentOptions.Center;
            current.enableAutoSizing = true;
            current.fontSize = 38f;
            current.fontSizeMin = 22f;
            current.fontSizeMax = 42f;
            current.fontStyle = FontStyles.Bold;
            current.textWrappingMode = TextWrappingModes.NoWrap;
            current.overflowMode = TextOverflowModes.Truncate;
            current.color = Color.white;
            current.enableVertexGradient = true;
            current.colorGradient = new VertexGradient(
                new Color(1f, 0.98f, 0.72f, 1f),
                new Color(1f, 0.93f, 0.35f, 1f),
                new Color(0.76f, 0.54f, 0.08f, 1f),
                new Color(1f, 0.82f, 0.16f, 1f));
            current.outlineWidth = 0.18f;
            current.outlineColor = new Color(0.02f, 0.035f, 0f, 1f);
            current.gameObject.SetActive(true);
            current.transform.SetAsLastSibling();
            return current;
        }

        private Image EnsureEndlessTopBarIcon(RectTransform parent, Image current, string objectName, Vector2 position, Vector2 size, Sprite sprite)
        {
            if (current == null)
            {
                Transform existing = parent.Find(objectName);
                current = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (current == null)
            {
                GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(parent, false);
                iconObject.layer = parent.gameObject.layer;
                current = iconObject.GetComponent<Image>();
            }

            RectTransform rect = current.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            current.sprite = sprite;
            current.type = Image.Type.Simple;
            current.preserveAspect = true;
            current.color = Color.white;
            current.raycastTarget = false;
            current.enabled = sprite != null;
            current.gameObject.SetActive(true);
            current.transform.SetAsLastSibling();
            return current;
        }

        private void RefreshEndlessTopBarValues()
        {
            if (endlessTopBarStatsRoot == null)
                return;

            int ozTile = CurrencyService.I != null ? CurrencyService.I.GetOzTile() : 0;

            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessLevelBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessExpBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessEnergyBadge", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessLevelText", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessExpText", false);
            SetEndlessTopBarElementVisible(endlessTopBarStatsRoot, "EndlessEnergyText", false);

            if (endlessOzTileText != null)
                endlessOzTileText.text = ozTile.ToString();

            if (endlessOzTileIcon != null)
            {
                if (endlessOzTileIcon.sprite == null)
                    endlessOzTileIcon.sprite = LoadEndlessOzTileIconSprite();

                endlessOzTileIcon.enabled = endlessOzTileIcon.sprite != null;
            }
        }

        private Sprite LoadEndlessLobbyBarSprite()
        {
            if (cachedEndlessLobbyBarSprite != null)
                return cachedEndlessLobbyBarSprite;

            cachedEndlessLobbyBarSprite = LoadResourceSprite(endlessLobbyBarResourcePath);
            return cachedEndlessLobbyBarSprite;
        }

        private Sprite LoadEndlessLobbyTopBarSprite()
        {
            if (cachedEndlessLobbyTopBarSprite != null)
                return cachedEndlessLobbyTopBarSprite;

            cachedEndlessLobbyTopBarSprite = LoadResourceSprite(endlessLobbyTopBarResourcePath);
            return cachedEndlessLobbyTopBarSprite;
        }

        private Sprite LoadEndlessLobbyBottomBarSprite()
        {
            if (cachedEndlessLobbyBottomBarSprite != null)
                return cachedEndlessLobbyBottomBarSprite;

            cachedEndlessLobbyBottomBarSprite = LoadResourceSprite(endlessLobbyBottomBarResourcePath);
            return cachedEndlessLobbyBottomBarSprite;
        }

        private Sprite LoadEndlessLobbyModeTabsSprite()
        {
            if (cachedEndlessLobbyModeTabsSprite != null)
                return cachedEndlessLobbyModeTabsSprite;

            cachedEndlessLobbyModeTabsSprite = LoadResourceSprite(endlessLobbyModeTabsResourcePath);
            return cachedEndlessLobbyModeTabsSprite;
        }

        private Sprite LoadEndlessLobbyCurrencyBadgeSprite()
        {
            if (cachedEndlessLobbyCurrencyBadgeSprite != null)
                return cachedEndlessLobbyCurrencyBadgeSprite;

            cachedEndlessLobbyCurrencyBadgeSprite = LoadResourceSprite(endlessLobbyCurrencyBadgeResourcePath);
            return cachedEndlessLobbyCurrencyBadgeSprite;
        }

        private Sprite LoadEndlessLobbyPopupWindowSprite()
        {
            if (cachedEndlessLobbyPopupWindowSprite != null)
                return cachedEndlessLobbyPopupWindowSprite;

            cachedEndlessLobbyPopupWindowSprite = LoadResourceSprite(endlessLobbyPopupWindowResourcePath);
            return cachedEndlessLobbyPopupWindowSprite;
        }

        private Sprite LoadEndlessLobbySquareIconSprite()
        {
            if (cachedEndlessLobbySquareIconSprite != null)
                return cachedEndlessLobbySquareIconSprite;

            cachedEndlessLobbySquareIconSprite = LoadResourceSprite(endlessLobbySquareIconResourcePath);
            return cachedEndlessLobbySquareIconSprite;
        }

        private Sprite LoadEndlessLobbyStoryLevelCardSprite()
        {
            if (cachedEndlessLobbyStoryLevelCardSprite != null)
                return cachedEndlessLobbyStoryLevelCardSprite;

            cachedEndlessLobbyStoryLevelCardSprite = LoadResourceSprite(endlessLobbyStoryLevelCardResourcePath);
            return cachedEndlessLobbyStoryLevelCardSprite;
        }

        private Sprite LoadEndlessLobbySettingsGearFrameSprite()
        {
            if (cachedEndlessLobbySettingsGearFrameSprite != null)
                return cachedEndlessLobbySettingsGearFrameSprite;

            cachedEndlessLobbySettingsGearFrameSprite = LoadResourceSprite(endlessLobbySettingsGearFrameResourcePath);
            return cachedEndlessLobbySettingsGearFrameSprite;
        }

        private Sprite LoadEndlessLobbyCloseButtonSprite()
        {
            if (cachedEndlessLobbyCloseButtonSprite != null)
                return cachedEndlessLobbyCloseButtonSprite;

            cachedEndlessLobbyCloseButtonSprite = LoadResourceSprite(endlessLobbyCloseButtonResourcePath);
            return cachedEndlessLobbyCloseButtonSprite;
        }

        private Sprite LoadEndlessLobbyButtonSprite()
        {
            if (cachedEndlessLobbyButtonSprite != null)
                return cachedEndlessLobbyButtonSprite;

            cachedEndlessLobbyButtonSprite = LoadResourceSprite(endlessLobbyButtonResourcePath);
            if (cachedEndlessLobbyButtonSprite == null)
                cachedEndlessLobbyButtonSprite = Resources.Load<Sprite>(endlessLobbyButtonResourcePath + "_0");

            return cachedEndlessLobbyButtonSprite;
        }

        private Sprite LoadEndlessOzTileIconSprite()
        {
            if (cachedEndlessOzTileIconSprite != null)
                return cachedEndlessOzTileIconSprite;

            cachedEndlessOzTileIconSprite = LoadResourceSprite(endlessLobbyOzTileIconResourcePath);
            return cachedEndlessOzTileIconSprite;
        }

        private Sprite LoadBoosterBagHintIconSprite()
        {
            if (cachedBoosterBagHintIconSprite != null)
                return cachedBoosterBagHintIconSprite;

            cachedBoosterBagHintIconSprite = LoadResourceSprite(boosterBagHintIconResourcePath);
            return cachedBoosterBagHintIconSprite;
        }

        private Sprite LoadBoosterBagShuffleIconSprite()
        {
            if (cachedBoosterBagShuffleIconSprite != null)
                return cachedBoosterBagShuffleIconSprite;

            cachedBoosterBagShuffleIconSprite = LoadResourceSprite(boosterBagShuffleIconResourcePath);
            return cachedBoosterBagShuffleIconSprite;
        }

        private Sprite LoadBoosterBagUndoIconSprite()
        {
            if (cachedBoosterBagUndoIconSprite != null)
                return cachedBoosterBagUndoIconSprite;

            cachedBoosterBagUndoIconSprite = LoadResourceSprite(boosterBagUndoIconResourcePath);
            return cachedBoosterBagUndoIconSprite;
        }

        private static Sprite LoadResourceSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private void HideShopUi()
        {
            MainShopUI[] shops = FindObjectsByType<MainShopUI>(FindObjectsInactive.Include);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null)
                    shops[i].gameObject.SetActive(false);
            }

            SetObjectActiveByName("MainShopUI", false);
            SetObjectActiveByName("ButtonOpenShop", false);
            SetObjectActiveByName("ShopOverlay", false);
        }

        private void HideBattleCharacterPreview()
        {
            SetObjectActiveByName("AnimalLobbyAvatar", false);
            SetObjectActiveByName("CharPanel", false);
            SetObjectActiveByName("Change", false);
            SetObjectActiveByName("Selected", false);

            MainLobbySelectedCharacterView[] views = FindObjectsByType<MainLobbySelectedCharacterView>(FindObjectsInactive.Include);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null)
                    views[i].enabled = false;
            }
        }

        private void EnsureEndlessButton()
        {
            if (endlessButton == null)
                endlessButton = FindButtonByName("ButtonEndlessMode");

            if (endlessButton != null)
            {
                endlessButton.onClick.RemoveListener(OnClickEndless);
                endlessButton.onClick.AddListener(OnClickEndless);
                return;
            }

            Transform parent = null;
            if (mainPanel != null)
                parent = mainPanel.transform;
            else if (battleButton != null)
                parent = battleButton.transform.parent;
            else if (rootCanvas != null)
                parent = rootCanvas.transform;

            if (parent == null)
                return;

            GameObject buttonObject = new GameObject("ButtonEndlessMode", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = parent.gameObject.layer;

            endlessButton = buttonObject.GetComponent<Button>();
            endlessButton.targetGraphic = buttonObject.GetComponent<Image>();
            endlessButton.onClick.AddListener(OnClickEndless);
        }

        private void EnsureBoosterBagUi()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            EnsureBoosterBagButton();
            EnsureBoosterBagOverlay();
        }

        private void EnsureBoosterBagButton()
        {
            if (boosterBagButton == null)
                boosterBagButton = FindButtonByName("ButtonBoosterBag");

            if (boosterBagButton == null)
            {
                Transform parent = null;
                if (mainPanel != null)
                    parent = mainPanel.transform;
                else if (battleButton != null)
                    parent = battleButton.transform.parent;
                else if (rootCanvas != null)
                    parent = rootCanvas.transform;

                if (parent == null)
                    return;

                GameObject buttonObject = new GameObject("ButtonBoosterBag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                buttonObject.layer = parent.gameObject.layer;
                boosterBagButton = buttonObject.GetComponent<Button>();
                boosterBagButton.targetGraphic = buttonObject.GetComponent<Image>();
            }

            boosterBagButton.onClick.RemoveListener(OpenBoosterBag);
            boosterBagButton.onClick.AddListener(OpenBoosterBag);
        }

        private void EnsureBoosterBagOverlay()
        {
            if (boosterBagOverlay != null || rootCanvas == null)
                return;

            GameObject overlay = new GameObject("MahjongBoosterBagOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(rootCanvas.transform, false);
            overlay.layer = rootCanvas.gameObject.layer;
            boosterBagOverlay = overlay;
            boosterBagCanvasGroup = overlay.GetComponent<CanvasGroup>();

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.01f, 0.025f, 0.015f, 0.72f);
            dim.raycastTarget = true;

            GameObject window = new GameObject("BagWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
            window.transform.SetParent(overlay.transform, false);
            window.layer = overlay.layer;

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = BoosterBagWindowSize;

            Image windowImage = window.GetComponent<Image>();
            Sprite popupSprite = LoadEndlessLobbyPopupWindowSprite();
            windowImage.sprite = popupSprite;
            windowImage.type = popupSprite != null ? Image.Type.Simple : Image.Type.Simple;
            windowImage.preserveAspect = false;
            windowImage.color = popupSprite != null ? Color.white : new Color(0.035f, 0.095f, 0.055f, 0.97f);
            windowImage.raycastTarget = true;

            Outline windowOutline = window.GetComponent<Outline>();
            windowOutline.effectColor = new Color(0.92f, 0.72f, 0.24f, 0.95f);
            windowOutline.effectDistance = new Vector2(5f, -5f);
            windowOutline.useGraphicAlpha = false;

            Shadow windowShadow = window.GetComponent<Shadow>();
            windowShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            windowShadow.effectDistance = new Vector2(0f, -10f);
            windowShadow.useGraphicAlpha = false;

            CreateBagText(window.transform, "Title", GameLocalization.Text("mahjong.bag.title"), new Vector2(0f, 320f), new Vector2(900f, 86f), 58f, LobbyButtonTextColor, TextAlignmentOptions.Center, true);
            CreateBagText(window.transform, "Subtitle", GameLocalization.Text("mahjong.bag.subtitle"), new Vector2(0f, 258f), new Vector2(1060f, 56f), 28f, new Color(0.88f, 0.95f, 0.72f, 1f), TextAlignmentOptions.Center, false);

            boosterBagHintCountText = CreateBoosterBagCard(
                window.transform,
                "HintPairCard",
                GameLocalization.Text("mahjong.bag.hint"),
                LoadBoosterBagHintIconSprite(),
                MahjongAssistBooster.HintPair,
                new Vector2(-430f, 35f));

            boosterBagShuffleCountText = CreateBoosterBagCard(
                window.transform,
                "ShuffleCard",
                GameLocalization.Text("mahjong.bag.shuffle"),
                LoadBoosterBagShuffleIconSprite(),
                MahjongAssistBooster.Shuffle,
                new Vector2(0f, 35f));

            boosterBagUndoCountText = CreateBoosterBagCard(
                window.transform,
                "UndoCard",
                GameLocalization.Text("mahjong.bag.undo"),
                LoadBoosterBagUndoIconSprite(),
                MahjongAssistBooster.Undo,
                new Vector2(430f, 35f));

            float adButtonX = MonetizationService.ArePurchasesSupported ? -270f : 0f;
            Button adButton = CreateBoosterBagActionButton(window.transform, "BtnBagRewardedAd", GameLocalization.Text("mahjong.bag.ad"), new Vector2(adButtonX, -315f), new Vector2(460f, 104f));
            adButton.onClick.AddListener(OnClickBoosterBagRewardedAd);

            if (MonetizationService.ArePurchasesSupported)
            {
                Button packButton = CreateBoosterBagActionButton(window.transform, "BtnBagPack", GameLocalization.Text("mahjong.bag.pack"), new Vector2(270f, -315f), new Vector2(460f, 104f));
                packButton.onClick.AddListener(OnClickBoosterBagPack);
            }

            boosterBagStatusText = CreateBagText(window.transform, "BagStatus", string.Empty, new Vector2(0f, -230f), new Vector2(900f, 42f), 24f, new Color(0.92f, 0.98f, 0.78f, 1f), TextAlignmentOptions.Center, false);

            Button closeButton = CreateBoosterBagActionButton(window.transform, "BtnBagClose", string.Empty, new Vector2(690f, 342f), new Vector2(78f, 70f));
            Image closeImage = closeButton.GetComponent<Image>();
            Sprite closeSprite = LoadEndlessLobbyCloseButtonSprite();
            if (closeImage != null && closeSprite != null)
            {
                closeImage.sprite = closeSprite;
                closeImage.type = Image.Type.Simple;
                closeImage.preserveAspect = true;
                closeImage.color = Color.white;
            }
            closeButton.onClick.AddListener(CloseBoosterBag);

            overlay.SetActive(false);
        }

        private TMP_Text CreateBoosterBagCard(Transform parent, string objectName, string title, Sprite icon, MahjongAssistBooster booster, Vector2 position)
        {
            GameObject card = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);
            card.layer = parent.gameObject.layer;

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = BoosterBagCardSize;

            Image cardImage = card.GetComponent<Image>();
            Sprite cardSprite = LoadEndlessLobbyStoryLevelCardSprite();
            cardImage.sprite = cardSprite;
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = false;
            cardImage.color = cardSprite != null ? Color.white : new Color(0.015f, 0.055f, 0.032f, 0.92f);
            cardImage.raycastTarget = false;

            Outline outline = card.GetComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.57f, 0.18f, 0.88f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;

            GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            iconFrame.transform.SetParent(card.transform, false);
            iconFrame.layer = card.layer;

            RectTransform iconFrameRect = iconFrame.GetComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconFrameRect.pivot = new Vector2(0.5f, 0.5f);
            iconFrameRect.anchoredPosition = new Vector2(0f, 68f);
            iconFrameRect.sizeDelta = new Vector2(188f, 188f);

            Image iconFrameImage = iconFrame.GetComponent<Image>();
            Sprite iconFrameSprite = LoadEndlessLobbySquareIconSprite();
            iconFrameImage.sprite = iconFrameSprite;
            iconFrameImage.type = Image.Type.Simple;
            iconFrameImage.preserveAspect = false;
            iconFrameImage.color = iconFrameSprite != null ? Color.white : new Color(0.82f, 0.78f, 0.52f, 0.94f);
            iconFrameImage.raycastTarget = false;

            Outline iconFrameOutline = iconFrame.GetComponent<Outline>();
            iconFrameOutline.effectColor = new Color(0.12f, 0.2f, 0.06f, 0.9f);
            iconFrameOutline.effectDistance = new Vector2(2f, -2f);
            iconFrameOutline.useGraphicAlpha = false;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(iconFrame.transform, false);
            iconObject.layer = card.layer;

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(160f, 160f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            CreateBagText(card.transform, "Name", title, new Vector2(0f, -92f), new Vector2(300f, 78f), 32f, new Color(0.98f, 0.92f, 0.56f, 1f), TextAlignmentOptions.Center, true);
            TMP_Text count = CreateBagText(card.transform, "Count", string.Empty, new Vector2(0f, -162f), new Vector2(250f, 56f), 36f, Color.white, TextAlignmentOptions.Center, true);
            SetBoosterBagCountText(count, MahjongAssistInventoryService.GetCount(booster));
            return count;
        }

        private Button CreateBoosterBagActionButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = parent.gameObject.layer;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            ApplyLobbyButtonStyle(button, label, size, position, false);
            return button;
        }

        private TMP_Text CreateBagText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment, bool bold)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Outline));
            textObject.transform.SetParent(parent, false);
            textObject.layer = parent.gameObject.layer;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.color = color;
            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Max(14f, fontSize * 0.52f);
            text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(text);

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            outline.useGraphicAlpha = false;
            return text;
        }

        private void OpenBoosterBag()
        {
            EnsureBoosterBagUi();
            RefreshBoosterBagCounts();
            SetBoosterBagStatus(string.Empty);

            if (boosterBagOverlay != null)
            {
                boosterBagOverlay.transform.SetAsLastSibling();
                boosterBagOverlay.SetActive(true);
            }

            if (boosterBagCanvasGroup != null)
            {
                boosterBagCanvasGroup.alpha = 1f;
                boosterBagCanvasGroup.blocksRaycasts = true;
                boosterBagCanvasGroup.interactable = true;
            }
        }

        private void CloseBoosterBag()
        {
            if (boosterBagOverlay != null)
                boosterBagOverlay.SetActive(false);
        }

        private void OnClickBoosterBagRewardedAd()
        {
            if (boosterBagRewardedAdInProgress)
                return;

            RewardedAdAvailability availability = MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.MahjongAssistRewardedPlacementId);
            if (!availability.IsReady)
            {
                SetBoosterBagStatus(GameLocalization.Text(availability.IsLoading ? "mahjong.bag.ad_loading" : "mahjong.bag.ad_unavailable"));
                return;
            }

            boosterBagRewardedAdInProgress = true;
            SetBoosterBagStatus(GameLocalization.Text("mahjong.bag.ad_loading"));

            MonetizationService.Ensure().ShowRewardedAd(MonetizationService.MahjongAssistRewardedPlacementId, result =>
            {
                boosterBagRewardedAdInProgress = false;

                if (!result.IsCompleted)
                {
                    SetBoosterBagStatus(GameLocalization.Text("mahjong.bag.ad_unavailable"));
                    RefreshBoosterBagCounts();
                    return;
                }

                GrantBoosterBagPack(MahjongAssistInventoryService.RewardedGrantAmount);
                RefreshBoosterBagCounts();
                SetBoosterBagStatus(GameLocalization.Format("mahjong.bag.added", MahjongAssistInventoryService.RewardedGrantAmount));
            });
        }

        private void OnClickBoosterBagPack()
        {
            if (!MonetizationService.ArePurchasesSupported || boosterBagPurchaseInProgress)
                return;

            MonetizationService service = MonetizationService.Ensure();
            if (!service.CanPurchase(BoosterBagPackProductId))
            {
                SetBoosterBagStatus(GameLocalization.Text("mahjong.bag.purchase_unavailable"));
                return;
            }

            boosterBagPurchaseInProgress = true;
            SetBoosterBagStatus(GameLocalization.Text("battle.shop.opening_purchase"));

            service.Purchase(BoosterBagPackProductId, result =>
            {
                boosterBagPurchaseInProgress = false;

                if (!result.IsPurchased)
                {
                    SetBoosterBagStatus(GameLocalization.Text("battle.shop.purchase_failed"));
                    return;
                }

                GrantBoosterBagPack(5);
                RefreshBoosterBagCounts();
                SetBoosterBagStatus(GameLocalization.Format("mahjong.bag.pack_added", 5));
            });
        }

        private void GrantBoosterBagPack(int amountPerBooster)
        {
            MahjongAssistInventoryService.Grant(MahjongAssistBooster.HintPair, amountPerBooster);
            MahjongAssistInventoryService.Grant(MahjongAssistBooster.Shuffle, amountPerBooster);
            MahjongAssistInventoryService.Grant(MahjongAssistBooster.Undo, amountPerBooster);
        }

        private void RefreshBoosterBagCounts()
        {
            SetBoosterBagCountText(boosterBagHintCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.HintPair));
            SetBoosterBagCountText(boosterBagShuffleCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.Shuffle));
            SetBoosterBagCountText(boosterBagUndoCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.Undo));
        }

        private static void SetBoosterBagCountText(TMP_Text text, int count)
        {
            if (text != null)
                text.text = count.ToString();
        }

        private void SetBoosterBagStatus(string value)
        {
            if (boosterBagStatusText != null)
                boosterBagStatusText.text = value;
        }

        private void ApplyLobbyButtonStyle(Button button, string labelText, Vector2 size, Vector2? anchoredPosition, bool largeLabel)
        {
            if (button == null)
                return;

            Sprite buttonSprite = LoadEndlessLobbyButtonSprite();
            button.gameObject.SetActive(true);
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            if (buttonSprite != null)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 0.82f, 1f);
                colors.pressedColor = new Color(0.82f, 0.92f, 0.52f, 1f);
                colors.selectedColor = new Color(1f, 1f, 0.82f, 1f);
                colors.disabledColor = new Color(0.55f, 0.62f, 0.35f, 0.72f);
            }
            else
            {
                colors.normalColor = LobbyButtonNormalColor;
                colors.highlightedColor = LobbyButtonHighlightedColor;
                colors.pressedColor = LobbyButtonPressedColor;
                colors.selectedColor = LobbyButtonHighlightedColor;
                colors.disabledColor = new Color(0.06f, 0.08f, 0.07f, 0.58f);
            }

            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Image image = button.image;
            if (image == null)
                image = button.gameObject.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = buttonSprite != null ? Color.white : LobbyButtonNormalColor;
                image.raycastTarget = true;

                Outline imageOutline = image.GetComponent<Outline>();
                if (buttonSprite != null && imageOutline != null)
                    imageOutline.enabled = false;
                else if (buttonSprite == null && imageOutline == null)
                    imageOutline = image.gameObject.AddComponent<Outline>();

                if (imageOutline != null)
                {
                    imageOutline.enabled = buttonSprite == null;
                    imageOutline.effectColor = LobbyButtonOutlineColor;
                    imageOutline.effectDistance = new Vector2(2f, -2f);
                    imageOutline.useGraphicAlpha = false;
                }
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.sizeDelta = size;

                if (anchoredPosition.HasValue)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = anchoredPosition.Value;
                }
            }

            TMP_Text label = EnsureButtonLabel(button);
            if (label == null)
                return;

            label.text = labelText;
            label.gameObject.SetActive(true);
            label.color = LobbyButtonTextColor;
            bool heroLabel = largeLabel && size.y >= 150f;
            label.fontSize = heroLabel ? 58f : largeLabel ? 34f : 28f;
            label.fontSizeMin = heroLabel ? 30f : largeLabel ? 20f : 16f;
            label.fontSizeMax = heroLabel ? 66f : largeLabel ? 38f : 30f;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(label);

            Outline labelOutline = label.GetComponent<Outline>();
            if (labelOutline == null)
                labelOutline = label.gameObject.AddComponent<Outline>();

            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.86f);
            labelOutline.effectDistance = new Vector2(1.25f, -1.25f);
            labelOutline.useGraphicAlpha = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = new Vector2(18f, 6f);
            labelRect.offsetMax = new Vector2(-18f, -7f);
            labelRect.localScale = Vector3.one;
        }

        private void ArrangeStoryLevelButtons()
        {
            if (levelSelectPanel == null)
                return;

            if (storyModeCoordinator != null)
            {
                SetLegacyStoryLevelButtonsVisible(false);
                return;
            }

            Button[] buttons = levelSelectPanel.GetComponentsInChildren<Button>(true);
            int levelIndex = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (!IsStoryLevelButton(button))
                    continue;

                Vector2 position = ResolveStoryLevelButtonPosition(levelIndex);
                ApplyLobbyButtonStyle(button, ResolveStoryLevelButtonText(button, levelIndex), EndlessLobbyLevelButtonSize, position, false);
                levelIndex++;
            }
        }

        private void SetLegacyStoryLevelButtonsVisible(bool visible)
        {
            if (levelSelectPanel == null)
                return;

            Button[] buttons = levelSelectPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || (storyModeCoordinator != null && storyModeCoordinator.HandlesButton(button)))
                    continue;

                if (IsStoryLevelButton(button))
                    button.gameObject.SetActive(visible);
            }
        }

        private static Vector2 ResolveStoryLevelButtonPosition(int index)
        {
            const int columns = 4;
            const float xStep = 360f;
            const float yStep = 112f;
            int column = index % columns;
            int row = index / columns;
            float startX = -((columns - 1) * xStep) * 0.5f;
            return new Vector2(startX + column * xStep, 170f - row * yStep);
        }

        private bool IsStoryLevelButton(Button button)
        {
            if (button == null)
                return false;

            if (button == storyButton || button == endlessButton || button == battleButton || button == backButton || button == resetButton)
                return false;

            string name = button.gameObject.name;
            if (!string.IsNullOrWhiteSpace(name) && name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            return label != null && !string.IsNullOrWhiteSpace(label.text) && label.text.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveStoryLevelButtonText(Button button, int index)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null || string.IsNullOrWhiteSpace(label.text))
                return $"Level {index + 1}";

            string text = label.text.Replace("\r", " ").Replace("\n", " ").Trim();
            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            return text;
        }

        private TMP_Text EnsureButtonLabel(Button button)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                return label;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button.transform, false);
            labelObject.layer = button.gameObject.layer;
            return labelObject.GetComponent<TextMeshProUGUI>();
        }

        private Button FindButtonByName(string objectName)
        {
            GameObject target = FindSceneObjectByName(objectName);
            return target != null ? target.GetComponent<Button>() : null;
        }

        private void SetObjectActiveByName(string objectName, bool active)
        {
            GameObject target = FindSceneObjectByName(objectName);
            if (target != null)
                target.SetActive(active);
        }

        private GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                    return candidate.gameObject;
            }

            return null;
        }

        private void PlayReturnToMainWithStoryReveal()
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                ShowMainPanelImmediate();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(ReturnToMainWithStoryRevealRoutine());
        }

        private IEnumerator ReturnToMainWithStoryRevealRoutine()
        {
            ShowMainPanelImmediate();

            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(enableStoryMode);

            if (storyButton != null)
            {
                storyButton.gameObject.SetActive(true);

                RectTransform storyRect = storyButton.transform as RectTransform;
                if (storyRect != null)
                    storyRect.localScale = Vector3.zero;

                CanvasGroup storyGroup = storyButton.GetComponent<CanvasGroup>();
                if (storyGroup == null)
                    storyGroup = storyButton.gameObject.AddComponent<CanvasGroup>();

                storyGroup.alpha = 1f;
                storyGroup.interactable = false;
                storyGroup.blocksRaycasts = false;

                Graphic[] graphics = storyButton.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] == null)
                        continue;

                    Color c = graphics[i].color;
                    c.a = 1f;
                    graphics[i].color = c;
                }
            }

            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform targetRect = storyButton != null ? storyButton.transform as RectTransform : null;
            if (targetRect == null || overlayRoot == null || cloudSprite == null)
            {
                RestoreButtonVisual(storyButton);
                transitionPlaying = false;
                yield break;
            }

            RectTransform cloud = CreateCloud("RevealCloud", cloudSprite, cloudAbsorbSize);
            if (cloud == null)
            {
                RestoreButtonVisual(storyButton);
                transitionPlaying = false;
                yield break;
            }

            Vector2 center = GetTargetLocalPosition(targetRect);
            center.y += cloudYOffset;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(halfWidth + cloudAbsorbSize.x + cloudRevealStartOffsetX, center.y);
            Vector2 revealPos = center;
            Vector2 exitPos = new Vector2(-halfWidth - cloudStartSize.x - cloudRevealExitOffsetX, center.y);

            cloud.anchoredPosition = startPos;
            cloud.sizeDelta = cloudAbsorbSize;
            cloud.localScale = Vector3.one;
            cloud.SetAsLastSibling();

            Image cloudImage = cloud.GetComponent<Image>();
            if (cloudImage != null)
            {
                cloudImage.enabled = true;
                cloudImage.color = Color.white;
            }

            yield return AnimateCloudMove(cloud, startPos, revealPos, revealFlyInTime, reverseRotationOnBackFlight ? -1f : 1f);
            yield return AnimateReveal(cloud, targetRect, storyButton);
            yield return AnimateCloudMoveAndResize(cloud, revealPos, exitPos, cloudAbsorbSize, cloudStartSize, revealFlyOutTime, reverseRotationOnBackFlight ? -1f : 1f);

            if (cloud != null)
                Destroy(cloud.gameObject);

            RestoreButtonVisual(storyButton);
            transitionPlaying = false;
        }

        private void PlayAbsorbCloudThenReveal(Button targetButton, Action onReveal)
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                onReveal?.Invoke();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(AbsorbCloudRoutine(targetButton, onReveal, null));
        }

        private void PlayAbsorbCloudThenComplete(Button targetButton, Action onComplete)
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                onComplete?.Invoke();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(AbsorbCloudRoutine(targetButton, null, onComplete));
        }

        private IEnumerator AbsorbCloudRoutine(Button targetButton, Action onReveal, Action onComplete)
        {
            if (targetButton == null)
            {
                transitionPlaying = false;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            RectTransform targetRect = targetButton.transform as RectTransform;
            if (targetRect == null || overlayRoot == null || cloudSprite == null)
            {
                transitionPlaying = false;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            CanvasGroup targetGroup = targetButton.GetComponent<CanvasGroup>();
            if (targetGroup == null)
                targetGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

            targetGroup.blocksRaycasts = false;

            Graphic[] targetGraphics = targetButton.GetComponentsInChildren<Graphic>(true);
            Color[] originalColors = CacheGraphicColors(targetGraphics);

            RectTransform cloud = CreateCloud("AbsorbCloud", cloudSprite, cloudStartSize);
            if (cloud == null)
            {
                transitionPlaying = false;
                targetGroup.blocksRaycasts = true;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            Vector2 center = GetTargetLocalPosition(targetRect);
            center.y += cloudYOffset;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(-halfWidth - cloudStartSize.x - cloudStartOffsetX, center.y);
            Vector2 absorbPos = center;
            Vector2 exitPos = new Vector2(halfWidth + cloudAbsorbSize.x + cloudExitOffsetX, center.y);

            cloud.anchoredPosition = startPos;
            cloud.sizeDelta = cloudStartSize;
            cloud.localScale = Vector3.one;
            cloud.SetAsLastSibling();

            Image cloudImage = cloud.GetComponent<Image>();
            if (cloudImage != null)
            {
                cloudImage.enabled = true;
                cloudImage.color = Color.white;
            }

            yield return AnimateCloudMove(cloud, startPos, absorbPos, cloudFlyInTime, 1f);
            yield return AnimateAbsorb(cloud, targetRect, targetGroup, targetGraphics, originalColors);

            if (cloudHoldTime > 0f)
                yield return Wait(cloudHoldTime);

            targetButton.gameObject.SetActive(false);

            onReveal?.Invoke();

            if (cloud != null)
            {
                cloud.gameObject.SetActive(true);
                cloud.SetAsLastSibling();

                Image img = cloud.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.color = Color.white;
                }
            }

            yield return AnimateCloudMove(cloud, absorbPos, exitPos, cloudFlyOutTime, 1f);

            if (cloud != null)
                Destroy(cloud.gameObject);

            transitionPlaying = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateAbsorb(RectTransform cloud, RectTransform targetRect, CanvasGroup targetGroup, Graphic[] targetGraphics, Color[] originalColors)
        {
            if (cloud == null || targetRect == null || targetGroup == null)
                yield break;

            Vector2 startSize = cloud.sizeDelta;
            Vector2 endSize = cloudAbsorbSize;
            Vector3 startButtonScale = targetRect.localScale;

            float t = 0f;
            float duration = Mathf.Max(0.0001f, absorbTime);

            while (t < duration)
            {
                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, eased);
                targetRect.localScale = Vector3.LerpUnclamped(startButtonScale, Vector3.zero, eased);
                RestoreGraphicColors(targetGraphics, originalColors);

                yield return null;
            }

            cloud.sizeDelta = endSize;
            targetRect.localScale = Vector3.zero;
            RestoreGraphicColors(targetGraphics, originalColors);

            targetGroup.interactable = false;
            targetGroup.blocksRaycasts = false;
        }

        private IEnumerator AnimateReveal(RectTransform cloud, RectTransform targetRect, Button targetButton)
        {
            if (cloud == null || targetRect == null || targetButton == null)
                yield break;

            CanvasGroup targetGroup = targetButton.GetComponent<CanvasGroup>();
            if (targetGroup == null)
                targetGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

            Graphic[] targetGraphics = targetButton.GetComponentsInChildren<Graphic>(true);
            Color[] originalColors = CacheGraphicColors(targetGraphics);

            Vector2 startSize = cloud.sizeDelta;
            Vector2 endSize = cloudStartSize;
            Vector3 startButtonScale = Vector3.zero;
            Vector3 endButtonScale = Vector3.one;

            targetRect.localScale = Vector3.zero;
            targetGroup.alpha = 1f;

            float t = 0f;
            float duration = Mathf.Max(0.0001f, revealTime);

            while (t < duration)
            {
                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, eased);
                targetRect.localScale = Vector3.LerpUnclamped(startButtonScale, endButtonScale, eased);
                RestoreGraphicColors(targetGraphics, originalColors);

                yield return null;
            }

            cloud.sizeDelta = endSize;
            targetRect.localScale = Vector3.one;
            RestoreGraphicColors(targetGraphics, originalColors);

            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        private IEnumerator AnimateCloudMove(RectTransform cloud, Vector2 from, Vector2 to, float duration, float rotationDirection)
        {
            if (cloud == null)
                yield break;

            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);
            float startZ = cloud.localEulerAngles.z;

            while (t < duration)
            {
                if (cloud == null)
                    yield break;

                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);

                if (rotateCloudWhileMoving)
                {
                    float z = startZ - (cloudRotateSpeed * rotationDirection * t);
                    cloud.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (cloud != null)
                cloud.anchoredPosition = to;
        }

        private IEnumerator AnimateCloudMoveAndResize(RectTransform cloud, Vector2 fromPos, Vector2 toPos, Vector2 fromSize, Vector2 toSize, float duration, float rotationDirection)
        {
            if (cloud == null)
                yield break;

            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);
            float startZ = cloud.localEulerAngles.z;

            while (t < duration)
            {
                if (cloud == null)
                    yield break;

                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, eased);
                cloud.sizeDelta = Vector2.LerpUnclamped(fromSize, toSize, eased);

                if (rotateCloudWhileMoving)
                {
                    float z = startZ - (cloudRotateSpeed * rotationDirection * t);
                    cloud.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (cloud != null)
            {
                cloud.anchoredPosition = toPos;
                cloud.sizeDelta = toSize;
            }
        }

        private void EnsureOverlay()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
            {
                Log("Root canvas not found");
                return;
            }

            Transform existing = rootCanvas.transform.Find(overlayName);
            if (existing != null)
            {
                overlayRoot = existing as RectTransform;
                overlayCanvas = existing.GetComponent<Canvas>();
            }

            if (overlayRoot == null || overlayCanvas == null)
            {
                GameObject go = new GameObject(
                    overlayName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(CloudFxRunner));

                go.transform.SetParent(rootCanvas.transform, false);

                overlayRoot = go.GetComponent<RectTransform>();
                overlayCanvas = go.GetComponent<Canvas>();
                fxRunner = go.GetComponent<CloudFxRunner>();

                overlayRoot.anchorMin = Vector2.zero;
                overlayRoot.anchorMax = Vector2.one;
                overlayRoot.offsetMin = Vector2.zero;
                overlayRoot.offsetMax = Vector2.zero;

                GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
                raycaster.enabled = false;
            }
            else
            {
                fxRunner = overlayCanvas.GetComponent<CloudFxRunner>();
                if (fxRunner == null)
                    fxRunner = overlayCanvas.gameObject.AddComponent<CloudFxRunner>();
            }

            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = overlaySortingOrder;
            overlayCanvas.sortingLayerName = overlaySortingLayerName;

            overlayRoot.SetAsLastSibling();
            overlayCanvas.gameObject.SetActive(true);
        }

        private RectTransform CreateCloud(string objectName, Sprite sprite, Vector2 size)
        {
            if (sprite == null || overlayRoot == null)
                return null;

            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(overlayRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            img.enabled = true;

            rt.SetAsLastSibling();
            return rt;
        }

        private Vector2 GetTargetLocalPosition(RectTransform target)
        {
            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, cam, out Vector2 localPoint);
            return localPoint;
        }

        private Button GetCurrentSelectedButton()
        {
            GameObject selected = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
                : null;

            if (selected == null)
                return null;

            return selected.GetComponent<Button>();
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[MahjongMenuUI] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName, StoryEndlessDoorSpriteResourcePath, false);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void SetPanel(GameObject panel, bool value)
        {
            if (panel != null)
                panel.SetActive(value);
        }

        private void SetPanelSafe(GameObject panel, bool value)
        {
            if (panel == null)
                return;

            if (panel == gameObject && !value)
                return;

            panel.SetActive(value);
        }

        private void RestoreButtonVisual(Button targetButton)
        {
            if (targetButton == null)
                return;

            targetButton.gameObject.SetActive(true);

            RectTransform rt = targetButton.transform as RectTransform;
            if (rt != null)
                rt.localScale = Vector3.one;

            CanvasGroup cg = targetButton.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            Graphic[] graphics = targetButton.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;
                Color c = graphics[i].color;
                c.a = 1f;
                graphics[i].color = c;
            }

            targetButton.interactable = true;
        }

        private void RestoreButtonsInTree(GameObject root)
        {
            if (root == null)
                return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                RestoreButtonVisual(buttons[i]);
        }

        private Color[] CacheGraphicColors(Graphic[] graphics)
        {
            if (graphics == null)
                return Array.Empty<Color>();

            Color[] colors = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                colors[i] = graphics[i] != null ? graphics[i].color : Color.white;

            return colors;
        }

        private void RestoreGraphicColors(Graphic[] graphics, Color[] colors)
        {
            if (graphics == null || colors == null)
                return;

            int count = Mathf.Min(graphics.Length, colors.Length);
            for (int i = 0; i < count; i++)
            {
                if (graphics[i] == null)
                    continue;

                graphics[i].color = colors[i];
            }
        }

        private float DeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private object Wait(float time)
        {
            if (useUnscaledTime)
                return new WaitForSecondsRealtime(time);

            return new WaitForSeconds(time);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[MahjongMenuUI] {message}", this);
        }
    }

}
