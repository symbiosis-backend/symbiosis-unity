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
    public sealed class SettingsMenuUI : MonoBehaviour
    {
        private static readonly Vector4 BattleLobbyWindowBorder = new Vector4(84f, 76f, 84f, 76f);
        private static readonly Vector4 MainFullscreenWindowBorder = new Vector4(200f, 100f, 200f, 180f);

        private static SettingsMenuUI instance;
        private static GameObject persistentRoot;
        private static bool mainSettingsButtonSuppressed;
        private const string BattleSettingsButtonResourcePath = "Mahjong/Sprites/BattleSettingsButton";
        private const string BattleLobbySettingsWindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/SettingsBattleWindow";
        private const string BattleLobbyPopupWindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/WindowBattle";
        private const string BattleLobbyPopupButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/Battlebutton";
        private const string BattleLobbyTopTabButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2";
        private const string MainButtonStandardResourcePath = "Mahjong/Sprites/MainSettings/BtnMainStandart";
        private const string MainSettingsButtonResourcePath = "Mahjong/Sprites/MainSettings/SettingsButtonMain";
        private const string MainSettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MainSettingsWindow";
        private const string MainSettingsTextButtonResourcePath = MainButtonStandardResourcePath;
        private const string MahjongLobbySettingsButtonSetResourcePath = "Mahjong/Sprites/MainSettings/MahjongLobbySettingsButtons";
        private const string MahjongLobbySettingsGearResourcePath = "Mahjong/Sprites/MainSettings/MahjongLobbySettingsGear";
        private const string MahjongLobbySettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MahjongLobbySettingsWindow";
        private const string BambooSettingsGearResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_SettingsGearFrame";
        private const string BambooSettingsWindowResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_PopupWindowPanel";
        private const string BambooSettingsMediumButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_MediumButton";
        private const string BambooSettingsLongButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_LongButton";
        private const string BambooSettingsGearIconResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_SettingsGearIcon";
        private const string BattleDoorSpriteResourcePath = "Mahjong/Sprites/BattleUI/BattleLobbyDoorLeaf";
        private const string StoryEndlessDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const string RussianLanguageButtonResourcePath = "Mahjong/Sprites/RuButton";
        private const string EnglishLanguageButtonResourcePath = "Mahjong/Sprites/EngButton";
        private const string TurkishLanguageButtonResourcePath = "Mahjong/Sprites/TrButton";
        private const string GermanLanguageButtonResourcePath = "Mahjong/Sprites/ButtonDE";

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Open / Close")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform openButtonRect;
        [SerializeField] private RectTransform panelRootRect;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private Image panelBackgroundImage;
        [SerializeField] private Image windowImage;
        [SerializeField] private Image mainSettingsWindowGraphicImage;

        [Header("Setting Buttons")]
        [SerializeField] private Button soundButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button infoHintsButton;
        [SerializeField] private Button russianLanguageButton;
        [SerializeField] private Button englishLanguageButton;
        [SerializeField] private Button turkishLanguageButton;
        [SerializeField] private Button germanLanguageButton;
        [SerializeField] private Button changeProfileButton;
        [SerializeField] private Button logoutButton;

        [Header("Colors")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = Color.gray;

        [Header("Game Only")]
        [SerializeField] private GameObject gameButtonsRoot;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button surrenderButton;

        [Header("Scene Rules")]
        [SerializeField] private string gameplaySceneName = "GameMahjong";
        [SerializeField] private string[] gameplaySceneNames =
        {
            "GameMahjong",
            "GameMahjongBattle"
        };
        [SerializeField] private string battleGameplaySceneName = "GameMahjongBattle";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string mahjongLobbySceneName = "LobbyMahjong";
        [SerializeField] private string entrySceneName = "Entry";
        [SerializeField] private bool pauseGameWhenOpened = true;

        [Header("Visibility Rules")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private bool hideOpenButtonWhileIntroPanelActive = true;

        [Header("Visual Overrides Per Scene")]
        [SerializeField] private bool applyVisualOverrides = true;
        [SerializeField] private SettingsSceneVisualStyle[] sceneVisualStyles;

        private float cachedTimeScale = 1f;
        private Sprite cachedBattleSettingsButtonSprite;
        private Sprite cachedMainSettingsButtonSprite;
        private Sprite cachedMainSettingsWindowSprite;
        private Sprite cachedMainFullscreenWindowSprite;
        private Sprite cachedMainSettingsTextButtonSprite;
        private Sprite cachedMahjongLobbySettingsGearSprite;
        private Sprite cachedMahjongLobbySettingsWindowSprite;
        private Sprite cachedMahjongLobbySettingsSmallButtonSprite;
        private Sprite cachedMahjongLobbySettingsLargeButtonSprite;
        private Sprite cachedBambooSettingsGearSprite;
        private Sprite cachedBambooSettingsWindowSprite;
        private Sprite cachedBambooSettingsMediumButtonSprite;
        private Sprite cachedBambooSettingsLongButtonSprite;
        private Sprite cachedBambooSettingsGearIconSprite;
        private Image openButtonInnerGearImage;
        private Coroutine openButtonGearSpinRoutine;
        private Sprite cachedRussianLanguageButtonSprite;
        private Sprite cachedEnglishLanguageButtonSprite;
        private Sprite cachedTurkishLanguageButtonSprite;
        private Sprite cachedGermanLanguageButtonSprite;
        private bool surrenderAdInProgress;
        private Sprite cachedBattleLobbyWindowSourceSprite;
        private Sprite cachedBattleLobbyWindowSprite;
        private Sprite cachedBattleLobbyPopupWindowSprite;
        private Sprite cachedBattleLobbyPopupButtonSprite;
        private float battleOpenButtonReadyAt;
        private int cachedMainLayoutScreenWidth = -1;
        private int cachedMainLayoutScreenHeight = -1;
        private Rect cachedMainLayoutSafeArea = new Rect(-1f, -1f, -1f, -1f);

        [Serializable]
        public sealed class SettingsSceneVisualStyle
        {
            public string SceneName;

            [Header("Open Button")]
            public bool ApplyOpenButtonRect = true;
            public Vector2 OpenButtonAnchorMin = new Vector2(0.5f, 1f);
            public Vector2 OpenButtonAnchorMax = new Vector2(0.5f, 1f);
            public Vector2 OpenButtonPivot = new Vector2(0.5f, 1f);
            public Vector2 OpenButtonPosition = new Vector2(0f, -28f);
            public Vector2 OpenButtonSize = new Vector2(90f, 90f);
            public bool ApplyOpenButtonGraphic;
            public Sprite OpenButtonSprite;
            public Color OpenButtonColor = Color.white;

            [Header("Overlay Panel")]
            public bool ApplyPanelRect;
            public Vector2 PanelAnchorMin = Vector2.zero;
            public Vector2 PanelAnchorMax = Vector2.one;
            public Vector2 PanelPivot = new Vector2(0.5f, 0.5f);
            public Vector2 PanelPosition = Vector2.zero;
            public Vector2 PanelSize = Vector2.zero;
            public bool ApplyPanelColor;
            public Color PanelColor = new Color(0.1f, 0.1f, 0.1f, 0.8627451f);
            public bool ApplyPanelGraphic;
            public Sprite PanelSprite;
            public Color PanelSpriteColor = Color.white;

            [Header("Window")]
            public bool ApplyWindowRect;
            public Vector2 WindowAnchorMin = new Vector2(0.5f, 0.5f);
            public Vector2 WindowAnchorMax = new Vector2(0.5f, 0.5f);
            public Vector2 WindowPivot = new Vector2(0.5f, 0.5f);
            public Vector2 WindowPosition = Vector2.zero;
            public Vector2 WindowSize = new Vector2(1100f, 800f);
            public bool ApplyWindowGraphic;
            public Sprite WindowSprite;
            public Color WindowColor = Color.white;

            [Header("Setting Buttons")]
            public bool ApplySettingButtonSize;
            public Vector2 SettingButtonSize = new Vector2(120f, 120f);
            public bool ApplySettingButtonColors;
            public Color EnabledColor = Color.white;
            public Color DisabledColor = Color.gray;
            public bool ApplySettingButtonGraphics;
            public Sprite SoundButtonSprite;
            public Sprite MusicButtonSprite;
            public Sprite VibrationButtonSprite;
            public Sprite CloseButtonSprite;
            public Sprite ReturnButtonSprite;
            public Sprite RestartButtonSprite;
            public Color ActionButtonColor = Color.white;

            [Header("Language Flags")]
            public bool ApplyLanguageButtonSize;
            public Vector2 LanguageButtonSize = new Vector2(120f, 80f);
            public bool ApplyLanguageButtonGraphics;
            public Sprite RussianLanguageSprite;
            public Sprite EnglishLanguageSprite;
            public Sprite TurkishLanguageSprite;
            public Color LanguageButtonColor = Color.white;
        }

        private bool IsGameScene => IsGameplayScene(SceneManager.GetActiveScene().name);
        private bool IsBattleGameScene => string.Equals(SceneManager.GetActiveScene().name, battleGameplaySceneName, StringComparison.Ordinal);
        private bool IsSettingsAvailableScene => IsSettingsAvailableSceneName(SceneManager.GetActiveScene().name);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntimeInstance()
        {
            if (instance != null)
                return;

            SettingsMenuUI existing = FindAnyObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
            if (existing != null && existing.gameObject.activeInHierarchy)
                return;

            GameObject go = new GameObject("SettingsMenu", typeof(RectTransform));
            go.AddComponent<SettingsMenuUI>();
        }

        public static Image EnsureBattleSettingsOpenButton()
        {
            if (instance == null)
                BootstrapRuntimeInstance();

            SettingsMenuUI menu = instance;
            if (menu == null)
                return null;

            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.Equals(sceneName, menu.battleGameplaySceneName, StringComparison.Ordinal))
                return null;

            mainSettingsButtonSuppressed = false;
            menu.EnsureRuntimeUi();
            menu.BindUI();
            menu.ApplyBattleOpenButtonSprite(sceneName, null);
            menu.ApplyBattleOpenButtonPlacement();
            menu.RefreshOpenButtonVisibility();
            if (Time.unscaledTime < menu.battleOpenButtonReadyAt && menu.panelRoot != null && menu.panelRoot.activeSelf)
                menu.CloseInstant(false);
            return menu.openButton != null ? menu.openButton.image : null;
        }

        private bool IsBlockedByIntro
        {
            get
            {
                if (!hideOpenButtonWhileIntroPanelActive)
                    return false;

                if (introPanel != null && introPanel.activeInHierarchy)
                    return true;

                return IsSceneObjectActive("IntroPanel");
            }
        }

        private static bool IsSceneObjectActive(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return false;

            RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect != null && string.Equals(rect.gameObject.name, objectName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            EnsurePersistentRoot();
            transform.SetParent(persistentRoot.transform, false);

            EnsureSettingsInstance();
            EnsureRuntimeUi();
            AutoResolveVisualTargets();
            EnsureInfoHintsButton();
            EnsureGermanLanguageButton();
            EnsureLogoutButton();
            EnsureChangeProfileButton();
            EnsureSurrenderButton();
            EnsureDefaultVisualStyles();

            if (IsBattleGameScene)
                battleOpenButtonReadyAt = Time.unscaledTime + 0.75f;

            if (panelRoot != null)
                SetPanelRootActive(false);

            BindUI();
            RefreshButtons();
            ApplySceneMode();
            ApplySceneVisualStyle();
            RefreshOpenButtonVisibility();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            RefreshButtons();
            ApplySceneMode();
            ApplySceneVisualStyle();
            RefreshOpenButtonVisibility();

            if (AppSettings.I != null)
                AppSettings.I.RefreshAndApplyAudio();
        }

        private void Update()
        {
            RefreshOpenButtonVisibility();
            RefreshMainFullscreenLayoutIfNeeded();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            UnbindUI();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A lobby or main-menu modal may suppress the persistent settings
            // button. That state must never leak into an active battle.
            if (string.Equals(scene.name, battleGameplaySceneName, StringComparison.Ordinal))
            {
                mainSettingsButtonSuppressed = false;
                battleOpenButtonReadyAt = Time.unscaledTime + 0.75f;
            }

            CloseInstant();
            RefreshButtons();
            ApplySceneMode();
            ApplySceneVisualStyle();
            RefreshOpenButtonVisibility();

            if (AppSettings.I != null)
                AppSettings.I.RefreshAndApplyAudio();
        }

        public void Open()
        {
            if (!IsBattleGameScene && BattleLobbyUiCoordinator.HasModalOpen &&
                BattleLobbyUiCoordinator.ActiveModal != BattleLobbyModalKind.Settings)
            {
                CloseInstant();
                return;
            }

            EnsureSettingsInstance();

            RefreshButtons();
            ApplySceneMode();
            ApplySceneVisualStyle();
            RefreshOpenButtonVisibility();

            if (!IsBattleGameScene && IsBlockedByIntro)
                return;

            if (!IsBattleGameScene && !MainHubStateController.CanOpenMainWindow("Settings"))
                return;

            if (string.Equals(SceneManager.GetActiveScene().name, "Main", System.StringComparison.Ordinal))
            {
                MainLobbyUiCoordinator.SetRightStackSuppressed(true);
                SetMainSettingsButtonSuppressed(true);
            }

            if (AppSettings.I != null)
                AppSettings.I.RefreshAndApplyAudio();

            if (panelRoot != null)
            {
                SetPanelBackdropRaycastActive(true);
                SetPanelRootActive(true);
                string activeSceneName = SceneManager.GetActiveScene().name;
                ApplyMainSettingsVisuals(activeSceneName);
                ApplyBambooMahjongSettingsVisuals(activeSceneName);
                ApplyBattleSettingsVisuals(activeSceneName);
                SetBattleLobbyMatchButtonsSuppressed(activeSceneName, true);
                RefreshButtons();
            }

            if (IsGameScene && pauseGameWhenOpened)
            {
                cachedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            AppSettings.I?.Vibrate();
        }

        private void HandleOpenButtonClick()
        {
            if (IsBattleGameScene && Time.unscaledTime < battleOpenButtonReadyAt)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                return;
            }

            Open();
        }

        public void Close()
        {
            SetBattleLobbyMatchButtonsSuppressed(SceneManager.GetActiveScene().name, false);

            if (panelRoot != null)
                SetPanelRootActive(false);
            SetPanelBackdropRaycastActive(false);

            if (IsGameScene && pauseGameWhenOpened)
                Time.timeScale = cachedTimeScale;

            if (string.Equals(SceneManager.GetActiveScene().name, "Main", System.StringComparison.Ordinal))
                MainHubStateController.NotifyMainWindowClosed();
        }

        public static void ForceCloseAllSettingsMenus()
        {
            SettingsMenuUI[] menus = FindObjectsByType<SettingsMenuUI>(FindObjectsInactive.Include);
            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] != null)
                    menus[i].CloseInstant();
            }

            EnsurePersistentRootRaycasterActive();
        }

        public static void SetMainSettingsButtonSuppressed(bool suppressed)
        {
            mainSettingsButtonSuppressed = suppressed;
            SettingsMenuUI[] menus = FindObjectsByType<SettingsMenuUI>(FindObjectsInactive.Include);
            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] == null)
                    continue;

                if (suppressed)
                    menus[i].CloseInstant(false);
                menus[i].RefreshOpenButtonVisibility();
            }
        }

        public static void ForceRefreshAllForCurrentScene()
        {
            SettingsMenuUI[] menus = FindObjectsByType<SettingsMenuUI>(FindObjectsInactive.Include);
            for (int i = 0; i < menus.Length; i++)
            {
                SettingsMenuUI menu = menus[i];
                if (menu == null)
                    continue;

                menu.CloseInstant();
                menu.RefreshButtons();
                menu.ApplySceneMode();
                menu.ApplySceneVisualStyle();
                menu.RefreshOpenButtonVisibility();
            }
        }

        private static void EnsurePersistentRootRaycasterActive()
        {
            if (persistentRoot == null)
                return;

            GraphicRaycaster raycaster = persistentRoot.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = persistentRoot.AddComponent<GraphicRaycaster>();

            raycaster.enabled = true;
        }

        private void CloseInstant(bool notifyMainHub = true)
        {
            bool wasOpen = panelRoot != null && panelRoot.activeSelf;
            SetBattleLobbyMatchButtonsSuppressed(SceneManager.GetActiveScene().name, false);

            if (panelRoot != null)
                SetPanelRootActive(false);
            SetPanelBackdropRaycastActive(false);

            Time.timeScale = 1f;

            if (notifyMainHub && wasOpen && string.Equals(SceneManager.GetActiveScene().name, "Main", System.StringComparison.Ordinal))
                MainHubStateController.NotifyMainWindowClosed();
        }

        private void SetPanelBackdropRaycastActive(bool active)
        {
            if (panelBackgroundImage != null)
                panelBackgroundImage.raycastTarget = active;
        }

        private void SetPanelRootActive(bool active)
        {
            if (panelRoot == null)
                return;

            if (!active)
                ClearSelectedUiInside(panelRoot.transform);

            if (panelRoot.activeSelf != active)
                panelRoot.SetActive(active);
        }

        private static void ClearSelectedUiInside(Transform root)
        {
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null || root == null)
                return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform != null && selected.transform.IsChildOf(root))
                eventSystem.SetSelectedGameObject(null);
        }

        private void BindUI()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
                openButton.onClick.RemoveListener(HandleOpenButtonClick);
                openButton.onClick.AddListener(HandleOpenButtonClick);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (soundButton != null)
            {
                soundButton.onClick.RemoveListener(ToggleSound);
                soundButton.onClick.AddListener(ToggleSound);
            }

            if (musicButton != null)
            {
                musicButton.onClick.RemoveListener(ToggleMusic);
                musicButton.onClick.AddListener(ToggleMusic);
            }

            if (vibrationButton != null)
            {
                vibrationButton.onClick.RemoveListener(ToggleVibration);
                vibrationButton.onClick.AddListener(ToggleVibration);
            }

            if (infoHintsButton != null)
            {
                infoHintsButton.onClick.RemoveListener(ToggleInfoHints);
                infoHintsButton.onClick.AddListener(ToggleInfoHints);
            }

            if (russianLanguageButton != null)
            {
                russianLanguageButton.onClick.RemoveListener(SetRussianLanguage);
                russianLanguageButton.onClick.AddListener(SetRussianLanguage);
            }

            if (englishLanguageButton != null)
            {
                englishLanguageButton.onClick.RemoveListener(SetEnglishLanguage);
                englishLanguageButton.onClick.AddListener(SetEnglishLanguage);
            }

            if (turkishLanguageButton != null)
            {
                turkishLanguageButton.onClick.RemoveListener(SetTurkishLanguage);
                turkishLanguageButton.onClick.AddListener(SetTurkishLanguage);
            }

            if (germanLanguageButton != null)
            {
                germanLanguageButton.onClick.RemoveListener(SetGermanLanguage);
                germanLanguageButton.onClick.AddListener(SetGermanLanguage);
            }

            if (logoutButton != null)
            {
                logoutButton.onClick.RemoveListener(LogoutProfile);
                logoutButton.onClick.AddListener(LogoutProfile);
            }

            if (changeProfileButton != null)
            {
                changeProfileButton.onClick.RemoveListener(ChangeProfile);
                changeProfileButton.onClick.AddListener(ChangeProfile);
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
                returnToMenuButton.onClick.AddListener(ReturnToMenu);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartScene);
                restartButton.onClick.AddListener(RestartScene);
            }

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveListener(SurrenderBattle);
                surrenderButton.onClick.AddListener(SurrenderBattle);
            }
        }

        private void UnbindUI()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(Open);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (soundButton != null)
                soundButton.onClick.RemoveListener(ToggleSound);

            if (musicButton != null)
                musicButton.onClick.RemoveListener(ToggleMusic);

            if (vibrationButton != null)
                vibrationButton.onClick.RemoveListener(ToggleVibration);

            if (infoHintsButton != null)
                infoHintsButton.onClick.RemoveListener(ToggleInfoHints);

            if (russianLanguageButton != null)
                russianLanguageButton.onClick.RemoveListener(SetRussianLanguage);

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.RemoveListener(SetEnglishLanguage);

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.RemoveListener(SetTurkishLanguage);

            if (germanLanguageButton != null)
                germanLanguageButton.onClick.RemoveListener(SetGermanLanguage);

            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(LogoutProfile);

            if (changeProfileButton != null)
                changeProfileButton.onClick.RemoveListener(ChangeProfile);

            if (returnToMenuButton != null)
                returnToMenuButton.onClick.RemoveListener(ReturnToMenu);

            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartScene);

            if (surrenderButton != null)
                surrenderButton.onClick.RemoveListener(SurrenderBattle);
        }

        private void ToggleSound()
        {
            if (AppSettings.I == null)
                return;

            AppSettings.I.SetSoundEnabled(!AppSettings.I.SoundEnabled);
            RefreshButtons();
            AppSettings.I.Vibrate();
        }

        private void ToggleMusic()
        {
            if (AppSettings.I == null)
                return;

            AppSettings.I.SetMusicEnabled(!AppSettings.I.MusicEnabled);
            RefreshButtons();
            AppSettings.I.Vibrate();
        }

        private void ToggleVibration()
        {
            if (AppSettings.I == null)
                return;

            AppSettings.I.SetVibrationEnabled(!AppSettings.I.VibrationEnabled);
            RefreshButtons();
            AppSettings.I.Vibrate();
        }

        private void ToggleInfoHints()
        {
            if (AppSettings.I == null)
                return;

            AppSettings.I.SetInfoHintsEnabled(!AppSettings.I.InfoHintsEnabled);
            RefreshButtons();
            AppSettings.I.Vibrate();
        }

        private void SetRussianLanguage()
        {
            SetLanguage(GameLanguage.Russian);
        }

        private void SetEnglishLanguage()
        {
            SetLanguage(GameLanguage.English);
        }

        private void SetTurkishLanguage()
        {
            SetLanguage(GameLanguage.Turkish);
        }

        private void SetGermanLanguage()
        {
            SetLanguage(GameLanguage.German);
        }

        private void SetLanguage(GameLanguage language)
        {
            EnsureSettingsInstance();
            AppSettings.I?.SetLanguage(language);
            AppSettings.I?.Vibrate();
            RefreshButtons();
        }

        private void LogoutProfile()
        {
            CloseInstant();

            if (ProfileService.I != null)
                ProfileService.I.Logout();

            if (AppSettings.I != null)
                AppSettings.I.ClearLanguagePreference();

            LoadSceneWithDoor(string.IsNullOrWhiteSpace(entrySceneName) ? "Entry" : entrySceneName);
        }

        private void ChangeProfile()
        {
            CloseInstant();

            if (ProfileService.I != null)
                ProfileService.I.ChangeProfile();

            LoadSceneWithDoor(string.IsNullOrWhiteSpace(entrySceneName) ? "Entry" : entrySceneName);
        }

        private void RefreshButtons()
        {
            if (AppSettings.I == null)
                return;

            RefreshInfoHintsButtonLabel();

            string sceneName = SceneManager.GetActiveScene().name;
            if (IsMahjongLobbySceneName(sceneName) || IsRegularMahjongGameplaySceneName(sceneName))
            {
                ApplyUntintedButtonColor(soundButton);
                ApplyUntintedButtonColor(musicButton);
                ApplyUntintedButtonColor(vibrationButton);
                ApplyUntintedButtonColor(infoHintsButton);
                ApplyUntintedButtonColor(russianLanguageButton);
                ApplyUntintedButtonColor(englishLanguageButton);
                ApplyUntintedButtonColor(turkishLanguageButton);
                ApplyUntintedButtonColor(germanLanguageButton);
                ApplyUntintedButtonColor(returnToMenuButton);
                ApplyUntintedButtonColor(restartButton);
                ApplyUntintedButtonColor(surrenderButton);
                ApplyUntintedButtonColor(changeProfileButton);
                ApplyUntintedButtonColor(logoutButton);
                ApplyUntintedButtonColor(closeButton);
                return;
            }

            ApplyButtonColor(soundButton, AppSettings.I.SoundEnabled);
            ApplyButtonColor(musicButton, AppSettings.I.MusicEnabled);
            ApplyButtonColor(vibrationButton, AppSettings.I.VibrationEnabled);
            ApplyButtonColor(infoHintsButton, AppSettings.I.InfoHintsEnabled);
            RefreshInfoHintsButtonLabel();
            ApplyLanguageButtonColor(russianLanguageButton, AppSettings.I.Language == GameLanguage.Russian);
            ApplyLanguageButtonColor(englishLanguageButton, AppSettings.I.Language == GameLanguage.English);
            ApplyLanguageButtonColor(turkishLanguageButton, AppSettings.I.Language == GameLanguage.Turkish);
            ApplyLanguageButtonColor(germanLanguageButton, AppSettings.I.Language == GameLanguage.German);
        }

        private void RefreshInfoHintsButtonLabel()
        {
            if (infoHintsButton == null || AppSettings.I == null)
                return;

            TMP_Text label = infoHintsButton.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            label.text = GameLocalization.Text(AppSettings.I.InfoHintsEnabled
                ? "settings.info_hints_on"
                : "settings.info_hints_off");
        }

        private void ApplyButtonColor(Button button, bool isEnabled)
        {
            if (button == null || button.image == null)
                return;

            button.image.color = isEnabled ? enabledColor : disabledColor;
        }

        private static void ApplyUntintedButtonColor(Button button)
        {
            if (button == null || button.image == null)
                return;

            button.image.color = Color.white;
            FreezeButtonColors(button);
        }

        private void ApplyLanguageButtonColor(Button button, bool isEnabled)
        {
            if (button == null || button.image == null)
                return;

            if (IsLanguageFlagSprite(button.image.sprite))
            {
                button.image.color = Color.white;
                return;
            }

            button.image.color = isEnabled ? enabledColor : disabledColor;
        }

        private bool IsLanguageFlagSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            if (sprite == cachedRussianLanguageButtonSprite
                || sprite == cachedEnglishLanguageButtonSprite
                || sprite == cachedTurkishLanguageButtonSprite
                || sprite == cachedGermanLanguageButtonSprite)
            {
                return true;
            }

            return string.Equals(sprite.name, "RuButton_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "EngButton_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "TrButton_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "ButtonDE_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "RuButton", StringComparison.Ordinal)
                || string.Equals(sprite.name, "EngButton", StringComparison.Ordinal)
                || string.Equals(sprite.name, "TrButton", StringComparison.Ordinal)
                || string.Equals(sprite.name, "ButtonDE", StringComparison.Ordinal);
        }

        private void AutoResolveVisualTargets()
        {
            if (openButtonRect == null && openButton != null)
                openButtonRect = openButton.GetComponent<RectTransform>();

            if (panelRootRect == null && panelRoot != null)
                panelRootRect = panelRoot.GetComponent<RectTransform>();

            if (panelBackgroundImage == null && panelRoot != null)
                panelBackgroundImage = panelRoot.GetComponent<Image>();

            if (windowRect == null)
            {
                Transform found = FindChildByName(transform, "Window");
                if (found != null)
                    windowRect = found.GetComponent<RectTransform>();
            }

            if (windowImage == null && windowRect != null)
                windowImage = windowRect.GetComponent<Image>();

            if (russianLanguageButton == null)
                russianLanguageButton = FindButtonByName("BtnLanguageRU");

            if (englishLanguageButton == null)
                englishLanguageButton = FindButtonByName("BtnLanguageEN");

            if (turkishLanguageButton == null)
                turkishLanguageButton = FindButtonByName("BtnLanguageTR");

            if (germanLanguageButton == null)
                germanLanguageButton = FindButtonByName("BtnLanguageDE");

            if (infoHintsButton == null)
                infoHintsButton = FindButtonByName("BtnInfoHints");

            if (changeProfileButton == null)
                changeProfileButton = FindButtonByName("BtnChangeProfile");

            if (logoutButton == null)
                logoutButton = FindButtonByName("BtnLogoutProfile");

            if (surrenderButton == null)
                surrenderButton = FindButtonByName("BtnSurrender");
        }

        private void EnsureSurrenderButton()
        {
            if (surrenderButton != null || gameButtonsRoot == null)
                return;

            Button templateButton = returnToMenuButton != null ? returnToMenuButton : restartButton;
            Vector2 size = new Vector2(400f, 120f);
            Image templateImage = null;

            if (templateButton != null)
            {
                RectTransform templateRect = templateButton.GetComponent<RectTransform>();
                if (templateRect != null)
                    size = templateRect.sizeDelta;

                templateImage = templateButton.image;
            }

            surrenderButton = CreateRuntimeTextButton(
                gameButtonsRoot.transform,
                "BtnSurrender",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f),
                size,
                "Surrender",
                "settings.surrender",
                RuntimeButtonStyle.Danger);

            if (templateImage != null && templateImage.sprite != null && surrenderButton.image != null)
            {
                surrenderButton.image.sprite = templateImage.sprite;
                surrenderButton.image.type = templateImage.type;
                surrenderButton.image.preserveAspect = templateImage.preserveAspect;
            }
        }

        private void EnsureInfoHintsButton()
        {
            if (!MainInfoHintTarget.FeatureEnabled)
            {
                if (infoHintsButton != null)
                    infoHintsButton.gameObject.SetActive(false);

                return;
            }

            if (infoHintsButton != null || windowRect == null)
                return;

            infoHintsButton = CreateRuntimeTextButton(
                windowRect,
                "BtnInfoHints",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -258f),
                new Vector2(360f, 82f),
                "Hints: On",
                null,
                RuntimeButtonStyle.Action);
        }

        private void EnsureLogoutButton()
        {
            if (logoutButton != null || windowRect == null)
            {
                AdjustProfileActionButtonLayout();
                return;
            }

            logoutButton = CreateRuntimeTextButton(
                windowRect,
                "BtnLogoutProfile",
                new Vector2(0.5f, 0.5f),
                new Vector2(170f, -95f),
                new Vector2(300f, 76f),
                "Logout",
                "settings.logout",
                RuntimeButtonStyle.Danger);
            AdjustProfileActionButtonLayout();
        }

        private void EnsureChangeProfileButton()
        {
            if (changeProfileButton != null || windowRect == null)
            {
                AdjustProfileActionButtonLayout();
                return;
            }

            changeProfileButton = CreateRuntimeTextButton(
                windowRect,
                "BtnChangeProfile",
                new Vector2(0.5f, 0.5f),
                new Vector2(-170f, -95f),
                new Vector2(300f, 76f),
                "Change Profile",
                "settings.change_profile",
                RuntimeButtonStyle.Action);
            AdjustProfileActionButtonLayout();
        }

        private void EnsureGermanLanguageButton()
        {
            if (germanLanguageButton != null || windowRect == null)
                return;

            germanLanguageButton = CreateRuntimeTextButton(
                windowRect,
                "BtnLanguageDE",
                new Vector2(0.5f, 0.5f),
                new Vector2(315f, 36f),
                new Vector2(140f, 90f),
                "DE",
                "settings.language_de",
                RuntimeButtonStyle.Language);
        }

        private void AdjustProfileActionButtonLayout()
        {
            if (windowRect == null)
                return;

            RectTransform logoutRect = logoutButton != null ? logoutButton.GetComponent<RectTransform>() : null;
            RectTransform changeRect = changeProfileButton != null ? changeProfileButton.GetComponent<RectTransform>() : null;

            if (logoutRect != null && changeRect != null)
            {
                SetCenteredRuntimeRect(changeRect, new Vector2(-170f, -95f), new Vector2(300f, 76f));
                SetCenteredRuntimeRect(logoutRect, new Vector2(170f, -95f), new Vector2(300f, 76f));
            }
            else if (logoutRect != null)
            {
                SetCenteredRuntimeRect(logoutRect, new Vector2(0f, -95f), new Vector2(300f, 76f));
            }
            else if (changeRect != null)
            {
                SetCenteredRuntimeRect(changeRect, new Vector2(0f, -95f), new Vector2(300f, 76f));
            }
        }

        private static void SetCenteredRuntimeRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void EnsureDefaultVisualStyles()
        {
            if (sceneVisualStyles != null && sceneVisualStyles.Length > 0)
                return;

            sceneVisualStyles = new[]
            {
                new SettingsSceneVisualStyle
                {
                    SceneName = "GameMahjong",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-36f, -22f),
                    OpenButtonSize = new Vector2(118f, 118f)
                },
                new SettingsSceneVisualStyle
                {
                    SceneName = "GameMahjongBattle",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-58f, -42f),
                    OpenButtonSize = new Vector2(82f, 82f),
                    ApplyOpenButtonGraphic = true,
                    OpenButtonColor = Color.white
                },
                new SettingsSceneVisualStyle
                {
                    SceneName = "LobbyMahjongBattle",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-58f, -42f),
                    OpenButtonSize = new Vector2(90f, 90f),
                    ApplyOpenButtonGraphic = true,
                    OpenButtonColor = Color.white,
                    ApplyWindowGraphic = true,
                    WindowSprite = LoadBattleLobbySettingsWindowSprite(),
                    WindowColor = Color.white
                },
                new SettingsSceneVisualStyle
                {
                    SceneName = "LobbyMahjong",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-58f, -42f),
                    OpenButtonSize = new Vector2(90f, 90f)
                },
                new SettingsSceneVisualStyle
                {
                    SceneName = "Main",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-58f, -42f),
                    OpenButtonSize = new Vector2(90f, 90f)
                }
            };
        }

        private void ApplySceneVisualStyle()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (!applyVisualOverrides)
            {
                ApplyBattleOpenButtonPlacement();
                ApplyMainOpenButtonPlacement();
                ApplyMainSettingsVisuals(activeSceneName);
                ApplyBambooMahjongSettingsVisuals(activeSceneName);
                ApplyBattleSettingsWindowLayout();
                ApplyBattleSettingsVisuals(activeSceneName);
                ApplyBattleOpenButtonSprite(activeSceneName, null);
                if (!IsMahjongLobbySceneName(activeSceneName))
                    ApplyLanguageButtonSprites(null);
                return;
            }

            AutoResolveVisualTargets();

            SettingsSceneVisualStyle style = ResolveSceneVisualStyle(activeSceneName);
            if (style == null)
            {
                ApplyBattleOpenButtonPlacement();
                ApplyMainOpenButtonPlacement();
                ApplyMainSettingsVisuals(activeSceneName);
                ApplyBambooMahjongSettingsVisuals(activeSceneName);
                ApplyBattleSettingsWindowLayout();
                ApplyBattleSettingsVisuals(activeSceneName);
                ApplyBattleOpenButtonSprite(activeSceneName, null);
                if (!IsMahjongLobbySceneName(activeSceneName))
                    ApplyLanguageButtonSprites(null);
                return;
            }

            if (style.ApplyOpenButtonRect)
                ApplyRect(openButtonRect, style.OpenButtonAnchorMin, style.OpenButtonAnchorMax, style.OpenButtonPivot, style.OpenButtonPosition, style.OpenButtonSize);

            ApplyOpenButtonGraphic(style, activeSceneName);

            if (style.ApplyPanelRect)
                ApplyRect(panelRootRect, style.PanelAnchorMin, style.PanelAnchorMax, style.PanelPivot, style.PanelPosition, style.PanelSize);

            if (style.ApplyPanelColor && panelBackgroundImage != null)
                panelBackgroundImage.color = style.PanelColor;

            if ((style.ApplyPanelGraphic || style.PanelSprite != null) && panelBackgroundImage != null)
                ApplyGraphic(panelBackgroundImage, style.PanelSprite, style.PanelSpriteColor);

            if (style.ApplyWindowRect)
                ApplyRect(windowRect, style.WindowAnchorMin, style.WindowAnchorMax, style.WindowPivot, style.WindowPosition, style.WindowSize);

            Sprite sceneWindowSprite = ResolveSceneWindowSprite(activeSceneName, style.WindowSprite);
            if ((style.ApplyWindowGraphic || sceneWindowSprite != null) && windowImage != null)
                ApplyGraphic(windowImage, sceneWindowSprite, style.WindowColor);

            if (style.ApplySettingButtonSize)
            {
                ApplyButtonSize(soundButton, style.SettingButtonSize);
                ApplyButtonSize(musicButton, style.SettingButtonSize);
                ApplyButtonSize(vibrationButton, style.SettingButtonSize);
            }

            if (style.ApplyLanguageButtonSize)
            {
                ApplyButtonSize(russianLanguageButton, style.LanguageButtonSize);
                ApplyButtonSize(englishLanguageButton, style.LanguageButtonSize);
                ApplyButtonSize(turkishLanguageButton, style.LanguageButtonSize);
                ApplyButtonSize(germanLanguageButton, style.LanguageButtonSize);
            }

            if (style.ApplySettingButtonColors)
            {
                enabledColor = style.EnabledColor;
                disabledColor = style.DisabledColor;
                RefreshButtons();
            }

            if ((style.ApplySettingButtonGraphics || style.ApplyLanguageButtonGraphics || HasSettingButtonSprites(style))
                && !IsMahjongLobbySceneName(activeSceneName))
            {
                ApplyButtonGraphic(soundButton, style.SoundButtonSprite, enabledColor);
                ApplyButtonGraphic(musicButton, style.MusicButtonSprite, enabledColor);
                ApplyButtonGraphic(vibrationButton, style.VibrationButtonSprite, enabledColor);
                ApplyButtonGraphic(closeButton, style.CloseButtonSprite, style.ActionButtonColor);
                ApplyButtonGraphic(returnToMenuButton, style.ReturnButtonSprite, style.ActionButtonColor);
                ApplyButtonGraphic(restartButton, style.RestartButtonSprite, style.ActionButtonColor);
                ApplyButtonGraphic(russianLanguageButton, style.RussianLanguageSprite, style.LanguageButtonColor);
                ApplyButtonGraphic(englishLanguageButton, style.EnglishLanguageSprite, style.LanguageButtonColor);
                ApplyButtonGraphic(turkishLanguageButton, style.TurkishLanguageSprite, style.LanguageButtonColor);
                ApplyButtonGraphic(germanLanguageButton, null, style.LanguageButtonColor);
                RefreshButtons();
            }

            if (!IsMahjongLobbySceneName(activeSceneName))
                ApplyLanguageButtonSprites(style);

            ApplyBattleOpenButtonPlacement();
            ApplyMainOpenButtonPlacement();
            ApplyMainSettingsVisuals(activeSceneName);
            ApplyBambooMahjongSettingsVisuals(activeSceneName);
            ApplyBattleSettingsWindowLayout();
            ApplyBattleSettingsVisuals(activeSceneName);
        }

        private void ApplyOpenButtonGraphic(SettingsSceneVisualStyle style, string sceneName)
        {
            if (style == null)
                return;

            Sprite sprite = style.OpenButtonSprite;
            bool isBattleLobby = string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal);
            if (isBattleLobby)
                sprite = LoadFirstSprite(BattleLobbyTopTabButtonResourcePath, null) ?? sprite;
            else if (IsBattleSettingsSceneName(sceneName))
                sprite = LoadBattleSettingsButtonSprite() ?? sprite;
            else if (IsGameplayScene(sceneName))
                sprite = IsRegularMahjongGameplaySceneName(sceneName)
                    ? LoadBambooSettingsGearIconSprite() ?? LoadMahjongLobbySettingsGearSprite() ?? LoadMainSettingsButtonSprite() ?? sprite
                    : LoadMahjongLobbySettingsGearSprite() ?? LoadMainSettingsButtonSprite() ?? sprite;
            else if (IsMainSettingsSceneName(sceneName))
                sprite = LoadMainSettingsButtonSprite(sceneName) ?? sprite;

            if ((style.ApplyOpenButtonGraphic || sprite != null) && openButton != null && openButton.image != null)
                ApplyGraphic(openButton.image, sprite, style.OpenButtonColor);

            if (isBattleLobby)
            {
                ApplyBattleLobbySettingsOpenButtonStyle();
            }
            else
            {
                SetOpenButtonLabelVisible(sprite == null && !IsGameplayScene(sceneName));
            }
            if (!IsRegularMahjongGameplaySceneName(sceneName) && !IsMahjongLobbySceneName(sceneName))
                EnsureOpenButtonInnerGear(false);
        }

        private void ApplyBattleOpenButtonSprite(string sceneName, Sprite fallbackSprite)
        {
            if (!IsBattleSettingsSceneName(sceneName) || openButton == null || openButton.image == null)
                return;

            bool isBattleLobby = string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal);
            Sprite sprite = isBattleLobby ? (LoadFirstSprite(BattleLobbyTopTabButtonResourcePath, null) ?? fallbackSprite) : (LoadBattleSettingsButtonSprite() ?? fallbackSprite);
            if (sprite == null)
                return;

            ApplyGraphic(openButton.image, sprite, Color.white);
            if (isBattleLobby)
            {
                ApplyBattleLobbySettingsOpenButtonStyle();
            }
            else
            {
                SetOpenButtonLabelVisible(false);
            }
        }

        private void ApplyMainSettingsVisuals(string sceneName)
        {
            if (IsMahjongLobbySceneName(sceneName))
            {
                ApplyBambooMahjongSettingsVisuals(sceneName);
                return;
            }

            if (!IsMainSettingsSceneName(sceneName))
            {
                SetMainSettingsWindowGraphicVisible(false);
                EnsureOpenButtonInnerGear(false);
                return;
            }

            Sprite openSprite = LoadMainSettingsButtonSprite(sceneName);
            if (openSprite != null && openButton != null && openButton.image != null)
            {
                ApplyGraphic(openButton.image, openSprite, Color.white);
                openButton.image.type = Image.Type.Simple;
                openButton.image.preserveAspect = true;
                FreezeButtonColors(openButton);
                SetOpenButtonLabelVisible(false);
                EnsureOpenButtonInnerGear(false);
            }

            ApplyMainSettingsBackdrop(sceneName);
            ApplyMainSettingsWindowGraphic(sceneName);

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                IsMahjongLobbySceneName(sceneName) ? new Vector2(1320f, 645f) : new Vector2(1100f, 740f));

            SetDecorativeLineVisible("TopAmberLine", false);
            SetDecorativeLineVisible("BottomJadeLine", false);
            ApplyMainTextButtonVisuals(sceneName);
            ApplyMainSettingsWindowLayout(sceneName);
            RefreshButtons();
        }

        private void SetDecorativeLineVisible(string objectName, bool visible)
        {
            if (windowRect == null || string.IsNullOrWhiteSpace(objectName))
                return;

            Transform line = FindChildByName(windowRect, objectName);
            if (line != null && line.gameObject.activeSelf != visible)
                line.gameObject.SetActive(visible);
        }

        private void ApplyMainSettingsBackdrop(string sceneName)
        {
            if (panelBackgroundImage == null)
                return;

            if (string.Equals(sceneName, "Main", StringComparison.Ordinal))
            {
                panelBackgroundImage.enabled = true;
                panelBackgroundImage.sprite = null;
                panelBackgroundImage.type = Image.Type.Simple;
                panelBackgroundImage.preserveAspect = false;
                panelBackgroundImage.color = Color.black;
                panelBackgroundImage.raycastTarget = true;
                SetRuntimeChildVisible(panelRoot != null ? panelRoot.transform : null, "WindowShadow", false);
                return;
            }

            if (IsMahjongLobbySceneName(sceneName))
            {
                panelBackgroundImage.enabled = true;
                panelBackgroundImage.color = Color.clear;
                panelBackgroundImage.raycastTarget = true;
            }
        }

        private void ApplyMainTextButtonVisuals(string sceneName)
        {
            Sprite textButtonSprite = LoadMainSettingsTextButtonSprite(sceneName);
            if (textButtonSprite == null)
                return;

            ApplyTextButtonGraphic(soundButton, textButtonSprite);
            ApplyTextButtonGraphic(musicButton, textButtonSprite);
            ApplyTextButtonGraphic(vibrationButton, textButtonSprite);
            ApplyTextButtonGraphic(infoHintsButton, LoadMainSettingsWideTextButtonSprite(sceneName) ?? textButtonSprite);
            if (IsMahjongLobbySceneName(sceneName))
            {
                ApplyTextButtonGraphic(russianLanguageButton, textButtonSprite);
                ApplyTextButtonGraphic(englishLanguageButton, textButtonSprite);
                ApplyTextButtonGraphic(turkishLanguageButton, textButtonSprite);
                ApplyTextButtonGraphic(germanLanguageButton, textButtonSprite);
            }
            ApplyTextButtonGraphic(changeProfileButton, LoadMainSettingsWideTextButtonSprite(sceneName) ?? textButtonSprite);
            ApplyTextButtonGraphic(logoutButton, LoadMainSettingsWideTextButtonSprite(sceneName) ?? textButtonSprite);
            if (string.Equals(sceneName, "Main", StringComparison.Ordinal))
                MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            else
                ApplyTextButtonGraphic(closeButton, textButtonSprite);
        }

        private void ApplyMainSettingsWindowLayout(string sceneName)
        {
            if (string.Equals(sceneName, "Main", StringComparison.Ordinal))
            {
                ApplyMainFullscreenWindowLayout();
                return;
            }

            bool isMahjongLobby = IsMahjongLobbySceneName(sceneName);
            Vector2 openButtonPosition = string.Equals(sceneName, "Main", StringComparison.Ordinal)
                ? new Vector2(-46f, -30f)
                : new Vector2(-48f, -28f);

            ApplyRect(
                openButtonRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                openButtonPosition,
                string.Equals(sceneName, "Main", StringComparison.Ordinal) ? new Vector2(82.5f, 82.5f) : new Vector2(104f, 104f));

            if (isMahjongLobby)
            {
                ApplyButtonRect(soundButton, new Vector2(-380f, 150f), new Vector2(320f, 82f));
                ApplyButtonRect(musicButton, new Vector2(0f, 150f), new Vector2(320f, 82f));
                ApplyButtonRect(vibrationButton, new Vector2(380f, 150f), new Vector2(320f, 82f));

                ApplyButtonRect(changeProfileButton, new Vector2(-245f, -24f), new Vector2(420f, 96f));
                ApplyButtonRect(logoutButton, new Vector2(245f, -24f), new Vector2(420f, 96f));
                ApplyButtonRect(closeButton, new Vector2(0f, -250f), new Vector2(320f, 82f));
                return;
            }

            ApplyButtonRect(soundButton, new Vector2(-320f, 160f), new Vector2(260f, 90f));
            ApplyButtonRect(musicButton, new Vector2(0f, 160f), new Vector2(260f, 90f));
            ApplyButtonRect(vibrationButton, new Vector2(320f, 160f), new Vector2(260f, 90f));

            ApplyButtonRect(russianLanguageButton, new Vector2(-315f, 36f), new Vector2(140f, 90f));
            ApplyButtonRect(englishLanguageButton, new Vector2(-105f, 36f), new Vector2(140f, 90f));
            ApplyButtonRect(turkishLanguageButton, new Vector2(105f, 36f), new Vector2(140f, 90f));
            ApplyButtonRect(germanLanguageButton, new Vector2(315f, 36f), new Vector2(140f, 90f));

            ApplyButtonRect(changeProfileButton, new Vector2(-205f, -126f), new Vector2(340f, 118f));
            ApplyButtonRect(logoutButton, new Vector2(205f, -126f), new Vector2(340f, 118f));
            ApplyButtonRect(infoHintsButton, new Vector2(0f, -258f), new Vector2(360f, 88f));
            ApplyButtonRect(closeButton, new Vector2(0f, -300f), new Vector2(270f, 94f));
        }

        private void ApplyMainFullscreenWindowLayout()
        {
            Vector2 canvasSize = ResolveMainSettingsCanvasSize();
            Vector4 safeInsets = ResolveMainSafeAreaInsets(canvasSize);
            const float horizontalMargin = 28f;
            const float verticalMargin = 24f;

            Vector2 windowSize = new Vector2(
                Mathf.Max(1f, canvasSize.x - safeInsets.x - safeInsets.z - horizontalMargin * 2f),
                Mathf.Max(1f, canvasSize.y - safeInsets.y - safeInsets.w - verticalMargin * 2f));
            Vector2 windowPosition = new Vector2(
                (safeInsets.x - safeInsets.z) * 0.5f,
                (safeInsets.y - safeInsets.w) * 0.5f);

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                windowPosition,
                windowSize);

            float layoutScale = Mathf.Clamp(
                Mathf.Min(windowSize.x / 2200f, windowSize.y / 1000f),
                0.78f,
                1.12f);

            ApplyButtonRect(soundButton, new Vector2(-440f, 245f) * layoutScale, new Vector2(340f, 104f) * layoutScale);
            ApplyButtonRect(musicButton, new Vector2(0f, 245f) * layoutScale, new Vector2(340f, 104f) * layoutScale);
            ApplyButtonRect(vibrationButton, new Vector2(440f, 245f) * layoutScale, new Vector2(340f, 104f) * layoutScale);

            ApplyButtonRect(russianLanguageButton, new Vector2(-330f, 65f) * layoutScale, new Vector2(160f, 104f) * layoutScale);
            ApplyButtonRect(englishLanguageButton, new Vector2(-110f, 65f) * layoutScale, new Vector2(160f, 104f) * layoutScale);
            ApplyButtonRect(turkishLanguageButton, new Vector2(110f, 65f) * layoutScale, new Vector2(160f, 104f) * layoutScale);
            ApplyButtonRect(germanLanguageButton, new Vector2(330f, 65f) * layoutScale, new Vector2(160f, 104f) * layoutScale);

            ApplyButtonRect(changeProfileButton, new Vector2(-250f, -135f) * layoutScale, new Vector2(430f, 112f) * layoutScale);
            ApplyButtonRect(logoutButton, new Vector2(250f, -135f) * layoutScale, new Vector2(430f, 112f) * layoutScale);
            ApplyButtonRect(infoHintsButton, new Vector2(0f, -330f) * layoutScale, new Vector2(420f, 96f) * layoutScale);
            ApplyAnchoredButtonRect(
                closeButton,
                Vector2.one,
                Vector2.one,
                new Vector2(-48f, -40f) * layoutScale,
                new Vector2(92f, 92f) * layoutScale);

            CacheMainLayoutScreenState();
        }

        private Vector2 ResolveMainSettingsCanvasSize()
        {
            RectTransform canvasRect = panelRootRect != null ? panelRootRect : transform as RectTransform;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return canvasRect.rect.size;

            return MainLobbyUiCoordinator.OverlayReferenceResolution;
        }

        private static Vector4 ResolveMainSafeAreaInsets(Vector2 canvasSize)
        {
            Rect safeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
                return Vector4.zero;

            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;
            float left = Mathf.Max(0f, safeArea.xMin) * scaleX;
            float bottom = Mathf.Max(0f, safeArea.yMin) * scaleY;
            float right = Mathf.Max(0f, Screen.width - safeArea.xMax) * scaleX;
            float top = Mathf.Max(0f, Screen.height - safeArea.yMax) * scaleY;
            return new Vector4(left, bottom, right, top);
        }

        private void RefreshMainFullscreenLayoutIfNeeded()
        {
            if (panelRoot == null || !panelRoot.activeSelf ||
                !string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.Ordinal))
                return;

            Rect safeArea = Screen.safeArea;
            if (cachedMainLayoutScreenWidth == Screen.width &&
                cachedMainLayoutScreenHeight == Screen.height &&
                cachedMainLayoutSafeArea == safeArea)
                return;

            ApplyMainSettingsVisuals("Main");
        }

        private void CacheMainLayoutScreenState()
        {
            cachedMainLayoutScreenWidth = Screen.width;
            cachedMainLayoutScreenHeight = Screen.height;
            cachedMainLayoutSafeArea = Screen.safeArea;
        }

        private static void ApplyTextButtonGraphic(Button button, Sprite sprite)
        {
            if (button == null || button.image == null || sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = true;
            button.image.color = Color.white;
            FreezeButtonColors(button);
            SetButtonLabelVisible(button, true);
            ApplyTextButtonLabelStyle(button);
        }

        private static void ApplyTextButtonLabelStyle(Button button)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;

            MainLobbyButtonStyle.ApplyFont(label);
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, label.fontSize * 0.55f);
            label.fontSizeMax = label.fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.margin = new Vector4(8f, 1f, 8f, 3f);
            label.gameObject.SetActive(true);
        }

        private static void FreezeButtonColors(Button button)
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.None;
            if (button.image != null)
                button.targetGraphic = button.image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private Sprite LoadBattleSettingsButtonSprite()
        {
            if (cachedBattleSettingsButtonSprite != null)
                return cachedBattleSettingsButtonSprite;

            cachedBattleSettingsButtonSprite = LoadFirstSprite(BattleSettingsButtonResourcePath, null);
            return cachedBattleSettingsButtonSprite;
        }

        private Sprite LoadMainSettingsButtonSprite(string sceneName)
        {
            if (IsMahjongLobbySceneName(sceneName))
                return LoadMahjongLobbySettingsGearSprite() ?? LoadMainSettingsButtonSprite();

            return LoadMainSettingsButtonSprite();
        }

        private Sprite LoadMainSettingsButtonSprite()
        {
            if (cachedMainSettingsButtonSprite != null)
                return cachedMainSettingsButtonSprite;

            cachedMainSettingsButtonSprite = LoadFirstSprite(MainSettingsButtonResourcePath, "SettingsButtonMain_0");
            return cachedMainSettingsButtonSprite;
        }

        private Sprite LoadMainSettingsWindowSprite(string sceneName)
        {
            if (IsMahjongLobbySceneName(sceneName))
                return LoadMahjongLobbySettingsWindowSprite() ?? LoadMainSettingsWindowSprite();

            return LoadMainSettingsWindowSprite();
        }

        private Sprite LoadMainSettingsWindowSprite()
        {
            if (cachedMainSettingsWindowSprite != null && IsUsableSettingsWindowSprite(cachedMainSettingsWindowSprite))
                return cachedMainSettingsWindowSprite;

            cachedMainSettingsWindowSprite = LoadLargestSprite(MainSettingsWindowResourcePath, "Window_1");
            return cachedMainSettingsWindowSprite;
        }

        private Sprite LoadBattleLobbySettingsWindowSprite()
        {
            if (cachedBattleLobbyWindowSourceSprite != null && IsUsableSettingsWindowSprite(cachedBattleLobbyWindowSourceSprite))
                return cachedBattleLobbyWindowSourceSprite;

            cachedBattleLobbyWindowSourceSprite = LoadLargestSprite(BattleLobbySettingsWindowResourcePath, "SettingsBattleWindow");
            return cachedBattleLobbyWindowSourceSprite;
        }

        private Sprite LoadBattleLobbyPopupWindowSprite()
        {
            if (cachedBattleLobbyPopupWindowSprite != null && IsUsableSettingsWindowSprite(cachedBattleLobbyPopupWindowSprite))
                return cachedBattleLobbyPopupWindowSprite;

            cachedBattleLobbyPopupWindowSprite = LoadLargestSprite(BattleLobbyPopupWindowResourcePath, "WindowBattle");
            return cachedBattleLobbyPopupWindowSprite;
        }

        private Sprite LoadBattleLobbyPopupButtonSprite()
        {
            if (cachedBattleLobbyPopupButtonSprite != null)
                return cachedBattleLobbyPopupButtonSprite;

            cachedBattleLobbyPopupButtonSprite = LoadFirstSprite(BattleLobbyPopupButtonResourcePath, "Battlebutton");
            return cachedBattleLobbyPopupButtonSprite;
        }

        private static bool IsUsableSettingsWindowSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            return sprite.rect.width >= 500f && sprite.rect.height >= 300f;
        }

        private void ApplyMainSettingsWindowGraphic(string sceneName)
        {
            if (windowImage == null && windowRect != null)
                windowImage = windowRect.GetComponent<Image>();

            if (windowRect == null)
                return;

            Sprite windowSprite = LoadMainSettingsWindowSprite(sceneName);
            if (windowSprite == null)
                return;

            Image targetImage = EnsureMainSettingsWindowGraphicImage();
            if (targetImage == null)
                return;

            bool isMainFullscreen = string.Equals(sceneName, "Main", StringComparison.Ordinal);
            ApplyMainSettingsWindowImage(windowImage, windowSprite, true);
            if (isMainFullscreen || IsMahjongLobbySceneName(sceneName))
                ApplyTransparentRaycastImage(windowImage);

            if (isMainFullscreen)
                ApplyMainFullscreenWindowImage(targetImage, windowSprite);
            else
                ApplyMainSettingsWindowImage(targetImage, windowSprite, false);

            SetMainSettingsWindowGraphicVisible(true);

            targetImage.transform.SetAsFirstSibling();
        }

        private void ApplyMainFullscreenWindowImage(Image image, Sprite sourceSprite)
        {
            if (image == null || sourceSprite == null)
                return;

            if (cachedMainFullscreenWindowSprite == null || cachedMainFullscreenWindowSprite.texture != sourceSprite.texture)
            {
                cachedMainFullscreenWindowSprite = Sprite.Create(
                    sourceSprite.texture,
                    sourceSprite.rect,
                    new Vector2(0.5f, 0.5f),
                    sourceSprite.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    MainFullscreenWindowBorder);
                cachedMainFullscreenWindowSprite.name = sourceSprite.name + "_MainFullscreen";
            }

            image.enabled = true;
            image.sprite = cachedMainFullscreenWindowSprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void SetMainSettingsWindowGraphicVisible(bool visible)
        {
            if (mainSettingsWindowGraphicImage != null)
            {
                mainSettingsWindowGraphicImage.enabled = visible;
                mainSettingsWindowGraphicImage.raycastTarget = false;
            }

            if (!visible)
            {
                SetDecorativeLineVisible("TopAmberLine", false);
                SetDecorativeLineVisible("BottomJadeLine", false);
            }
        }

        private static void ApplyMainSettingsWindowImage(Image image, Sprite sprite, bool receiveRaycasts)
        {
            if (image == null || sprite == null)
                return;

            image.enabled = true;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = receiveRaycasts;
        }

        private static void ApplyTransparentRaycastImage(Image image)
        {
            if (image == null)
                return;

            image.enabled = true;
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        private Image EnsureMainSettingsWindowGraphicImage()
        {
            if (mainSettingsWindowGraphicImage != null)
                return mainSettingsWindowGraphicImage;

            if (windowRect == null)
                return null;

            Transform existing = FindChildByName(windowRect, "MainSettingsWindowGraphic");
            if (existing != null)
            {
                mainSettingsWindowGraphicImage = existing.GetComponent<Image>();
                if (mainSettingsWindowGraphicImage != null)
                    return mainSettingsWindowGraphicImage;
            }

            GameObject graphicObject = new GameObject("MainSettingsWindowGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            graphicObject.transform.SetParent(windowRect, false);
            graphicObject.transform.SetAsFirstSibling();

            RectTransform graphicRect = graphicObject.GetComponent<RectTransform>();
            graphicRect.anchorMin = Vector2.zero;
            graphicRect.anchorMax = Vector2.one;
            graphicRect.pivot = new Vector2(0.5f, 0.5f);
            graphicRect.anchoredPosition = Vector2.zero;
            graphicRect.offsetMin = Vector2.zero;
            graphicRect.offsetMax = Vector2.zero;

            mainSettingsWindowGraphicImage = graphicObject.GetComponent<Image>();
            return mainSettingsWindowGraphicImage;
        }

        private Sprite ResolveSceneWindowSprite(string sceneName, Sprite sourceSprite)
        {
            if (!IsBattleSettingsSceneName(sceneName))
                return sourceSprite;

            Sprite battleLobbyWindowSprite = LoadBattleLobbyPopupWindowSprite();
            if (battleLobbyWindowSprite != null)
                return battleLobbyWindowSprite;

            Sprite popupWindowSprite = BattlePopupStyle.WindowSprite;
            if (popupWindowSprite != null)
                return popupWindowSprite;

            sourceSprite ??= LoadBattleLobbySettingsWindowSprite();
            if (sourceSprite == null)
                return null;

            if (cachedBattleLobbyWindowSprite != null && cachedBattleLobbyWindowSprite.texture == sourceSprite.texture)
                return cachedBattleLobbyWindowSprite;

            cachedBattleLobbyWindowSprite = Sprite.Create(
                sourceSprite.texture,
                sourceSprite.rect,
                new Vector2(0.5f, 0.5f),
                sourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                BattleLobbyWindowBorder);

            return cachedBattleLobbyWindowSprite;
        }

        private void ApplyBattleSettingsVisuals(string sceneName)
        {
            if (!IsBattleSettingsSceneName(sceneName))
                return;

            SetMainSettingsWindowGraphicVisible(false);
            ApplyBattleSettingsBackdrop(sceneName);

            Sprite sprite = ResolveSceneWindowSprite(sceneName, null);
            if (sprite != null && windowImage != null)
                ApplyBattleLobbyPopupImage(windowImage, sprite, true);

            ApplyBattleSettingsButtonVisuals();
            ApplyBattleSettingsFullToggleLabels();
            if (IsBattleGameScene)
            {
                SetLanguageButtonsActive(false);
                if (changeProfileButton != null)
                    changeProfileButton.gameObject.SetActive(false);
                if (logoutButton != null)
                    logoutButton.gameObject.SetActive(false);
                if (infoHintsButton != null)
                    infoHintsButton.gameObject.SetActive(false);
            }
        }

        private void ApplyBattleSettingsButtonVisuals()
        {
            ApplyLanguageButtonSprites(null);

            ApplyBattleSettingToggleButtonVisual(soundButton);
            ApplyBattleSettingToggleButtonVisual(musicButton);
            ApplyBattleSettingToggleButtonVisual(vibrationButton);
            ApplyBattleSettingToggleButtonVisual(infoHintsButton);

            ApplyBattleActionButtonVisual(changeProfileButton);
            ApplyBattleActionButtonVisual(logoutButton);
            ApplyBattleActionButtonVisual(closeButton);
            ApplyBattleActionButtonVisual(returnToMenuButton);
            ApplyBattleActionButtonVisual(restartButton);
            ApplyBattleActionButtonVisual(surrenderButton);
        }

        private static void ApplyBattleSettingToggleButtonVisual(Button button)
        {
            BattlePopupStyle.ApplyButton(button, true);
            ApplyBattleLobbyButtonGraphic(button);
            ApplyBattleSettingsButtonLabel(button, 31f);
        }

        private static void ApplyBattleActionButtonVisual(Button button)
        {
            BattlePopupStyle.ApplyButton(button);
            ApplyBattleLobbyButtonGraphic(button);
            ApplyBattleSettingsButtonLabel(button, 30f);
        }

        private static void ApplyBattleLobbyPopupImage(Image image, Sprite sprite, bool raycastTarget)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
        }

        private static void ApplyBattleLobbyButtonGraphic(Button button)
        {
            Image image = button != null ? button.image : null;
            if (image == null)
                return;

            Sprite sprite = instance != null ? instance.LoadBattleLobbyPopupButtonSprite() : null;
            if (sprite == null)
                return;

            ApplyBattleLobbyPopupImage(image, sprite, true);
            button.targetGraphic = image;
        }

        private static void ApplyBattleSettingsButtonLabel(Button button, float fontSize)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;

            label.fontSize = fontSize;
            BattlePopupStyle.ApplyButtonLabel(button, fontSize);
        }

        private static void ApplyBattleSettingsFullToggleLabels()
        {
            ApplyBattleSettingsFullLabel(instance != null ? instance.soundButton : null, "settings.sound");
            ApplyBattleSettingsFullLabel(instance != null ? instance.musicButton : null, "settings.music");
            ApplyBattleSettingsFullLabel(instance != null ? instance.vibrationButton : null, "settings.vibration");
        }

        private static void ApplyBattleSettingsFullLabel(Button button, string localizationKey)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;

            label.text = GameLocalization.Text(localizationKey);
            label.enableAutoSizing = true;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 34f;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
        }

        private void ApplyBattleSettingsBackdrop(string sceneName)
        {
            if (!IsBattleSettingsSceneName(sceneName))
                return;

            if (panelBackgroundImage != null)
            {
                panelBackgroundImage.enabled = true;
                panelBackgroundImage.color = string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal)
                    ? Color.clear
                    : new Color(0f, 0f, 0f, 0.46f);
                panelBackgroundImage.raycastTarget = true;
            }

            SetRuntimeChildVisible(panelRoot != null ? panelRoot.transform : null, "WindowShadow", false);
        }

        private static void SetRuntimeChildVisible(Transform root, string childName, bool visible)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
                return;

            Transform child = root.Find(childName);
            if (child != null && child.gameObject.activeSelf != visible)
                child.gameObject.SetActive(visible);
        }

        private void SetBattleLobbyMatchButtonsSuppressed(string sceneName, bool suppressed)
        {
            if (!string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal))
                return;

            BattleLobbyUI[] lobbies = FindObjectsByType<BattleLobbyUI>(FindObjectsInactive.Include);
            for (int i = 0; i < lobbies.Length; i++)
            {
                if (lobbies[i] != null)
                    lobbies[i].SetMatchButtonsSuppressedBySettings(suppressed);
            }
        }

        private Sprite LoadMainSettingsTextButtonSprite(string sceneName)
        {
            if (IsMahjongLobbySceneName(sceneName))
                return LoadMahjongLobbySettingsSmallButtonSprite() ?? LoadMainSettingsTextButtonSprite();

            return LoadMainSettingsTextButtonSprite();
        }

        private Sprite LoadMainSettingsWideTextButtonSprite(string sceneName)
        {
            if (IsMahjongLobbySceneName(sceneName))
                return LoadMahjongLobbySettingsLargeButtonSprite();

            return null;
        }

        private Sprite LoadMainSettingsTextButtonSprite()
        {
            if (cachedMainSettingsTextButtonSprite != null)
                return cachedMainSettingsTextButtonSprite;

            cachedMainSettingsTextButtonSprite = LoadFirstSprite(MainSettingsTextButtonResourcePath, "BtnMainStandart_0");
            return cachedMainSettingsTextButtonSprite;
        }

        private Sprite LoadMahjongLobbySettingsGearSprite()
        {
            if (cachedMahjongLobbySettingsGearSprite != null)
                return cachedMahjongLobbySettingsGearSprite;

            cachedMahjongLobbySettingsGearSprite = LoadFirstSprite(MahjongLobbySettingsGearResourcePath, "MahjongLobbySettingsGear_0");
            return cachedMahjongLobbySettingsGearSprite;
        }

        private Sprite LoadMahjongLobbySettingsWindowSprite()
        {
            if (cachedMahjongLobbySettingsWindowSprite != null && IsUsableSettingsWindowSprite(cachedMahjongLobbySettingsWindowSprite))
                return cachedMahjongLobbySettingsWindowSprite;

            cachedMahjongLobbySettingsWindowSprite = LoadLargestSprite(MahjongLobbySettingsWindowResourcePath, "MahjongLobbySettingsWindow_0");
            return cachedMahjongLobbySettingsWindowSprite;
        }

        private Sprite LoadMahjongLobbySettingsSmallButtonSprite()
        {
            if (cachedMahjongLobbySettingsSmallButtonSprite != null)
                return cachedMahjongLobbySettingsSmallButtonSprite;

            cachedMahjongLobbySettingsSmallButtonSprite = LoadFirstSprite(MahjongLobbySettingsButtonSetResourcePath, "MahjongLobbySettingsButtonSmall_0");
            return cachedMahjongLobbySettingsSmallButtonSprite;
        }

        private Sprite LoadMahjongLobbySettingsLargeButtonSprite()
        {
            if (cachedMahjongLobbySettingsLargeButtonSprite != null)
                return cachedMahjongLobbySettingsLargeButtonSprite;

            cachedMahjongLobbySettingsLargeButtonSprite = LoadFirstSprite(MahjongLobbySettingsButtonSetResourcePath, "MahjongLobbySettingsButtonLarge_0");
            return cachedMahjongLobbySettingsLargeButtonSprite;
        }

        private Sprite LoadBambooSettingsGearSprite()
        {
            if (cachedBambooSettingsGearSprite != null)
                return cachedBambooSettingsGearSprite;

            cachedBambooSettingsGearSprite = LoadFirstSprite(BambooSettingsGearResourcePath, "Mahjong_Lobby_SettingsGearFrame");
            return cachedBambooSettingsGearSprite;
        }

        private Sprite LoadBambooSettingsWindowSprite()
        {
            if (cachedBambooSettingsWindowSprite != null && IsUsableSettingsWindowSprite(cachedBambooSettingsWindowSprite))
                return cachedBambooSettingsWindowSprite;

            cachedBambooSettingsWindowSprite = LoadLargestSprite(BambooSettingsWindowResourcePath, "Mahjong_Lobby_PopupWindowPanel");
            return cachedBambooSettingsWindowSprite;
        }

        private Sprite LoadBambooSettingsMediumButtonSprite()
        {
            if (cachedBambooSettingsMediumButtonSprite != null)
                return cachedBambooSettingsMediumButtonSprite;

            cachedBambooSettingsMediumButtonSprite = LoadFirstSprite(BambooSettingsMediumButtonResourcePath, "Mahjong_Bamboo_MediumButton");
            return cachedBambooSettingsMediumButtonSprite;
        }

        private Sprite LoadBambooSettingsLongButtonSprite()
        {
            if (cachedBambooSettingsLongButtonSprite != null)
                return cachedBambooSettingsLongButtonSprite;

            cachedBambooSettingsLongButtonSprite = LoadFirstSprite(BambooSettingsLongButtonResourcePath, "Mahjong_Bamboo_LongButton");
            return cachedBambooSettingsLongButtonSprite;
        }

        private Sprite LoadBambooSettingsGearIconSprite()
        {
            if (cachedBambooSettingsGearIconSprite != null)
                return cachedBambooSettingsGearIconSprite;

            cachedBambooSettingsGearIconSprite = LoadFirstSprite(BambooSettingsGearIconResourcePath, "Mahjong_Lobby_SettingsGearIcon");
            return cachedBambooSettingsGearIconSprite;
        }

        private void ApplyLanguageButtonSprites(SettingsSceneVisualStyle style)
        {
            Sprite russianSprite = LoadRussianLanguageButtonSprite() ?? style?.RussianLanguageSprite;
            Sprite englishSprite = LoadEnglishLanguageButtonSprite() ?? style?.EnglishLanguageSprite;
            Sprite turkishSprite = LoadTurkishLanguageButtonSprite() ?? style?.TurkishLanguageSprite;
            Sprite germanSprite = LoadGermanLanguageButtonSprite();

            ApplyLanguageButtonSprite(russianLanguageButton, russianSprite);
            ApplyLanguageButtonSprite(englishLanguageButton, englishSprite);
            ApplyLanguageButtonSprite(turkishLanguageButton, turkishSprite);
            ApplyLanguageButtonSprite(germanLanguageButton, germanSprite);
            RefreshButtons();
        }

        private Sprite LoadRussianLanguageButtonSprite()
        {
            if (cachedRussianLanguageButtonSprite != null)
                return cachedRussianLanguageButtonSprite;

            cachedRussianLanguageButtonSprite = LoadFirstSprite(RussianLanguageButtonResourcePath, null);
            return cachedRussianLanguageButtonSprite;
        }

        private Sprite LoadEnglishLanguageButtonSprite()
        {
            if (cachedEnglishLanguageButtonSprite != null)
                return cachedEnglishLanguageButtonSprite;

            cachedEnglishLanguageButtonSprite = LoadFirstSprite(EnglishLanguageButtonResourcePath, null);
            return cachedEnglishLanguageButtonSprite;
        }

        private Sprite LoadTurkishLanguageButtonSprite()
        {
            if (cachedTurkishLanguageButtonSprite != null)
                return cachedTurkishLanguageButtonSprite;

            cachedTurkishLanguageButtonSprite = LoadFirstSprite(TurkishLanguageButtonResourcePath, null);
            return cachedTurkishLanguageButtonSprite;
        }

        private Sprite LoadGermanLanguageButtonSprite()
        {
            if (cachedGermanLanguageButtonSprite != null)
                return cachedGermanLanguageButtonSprite;

            cachedGermanLanguageButtonSprite = LoadFirstSprite(GermanLanguageButtonResourcePath, null);
            return cachedGermanLanguageButtonSprite;
        }

        private static Sprite LoadFirstSprite(string resourcePath, string preferredSpriteName)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredSpriteName))
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && string.Equals(sprites[i].name, preferredSpriteName, StringComparison.Ordinal))
                        return sprites[i];
                }
            }

            Sprite first = sprites[0];
            if (first != null && first.texture != null)
            {
                Rect rect = first.rect;
                if (rect.xMin >= 0f
                    && rect.yMin >= 0f
                    && rect.xMax <= first.texture.width
                    && rect.yMax <= first.texture.height)
                {
                    return first;
                }
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return texture != null
                ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                : first;
        }

        private static Sprite LoadLargestSprite(string resourcePath, string preferredSpriteName)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(preferredSpriteName))
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] != null && string.Equals(sprites[i].name, preferredSpriteName, StringComparison.Ordinal))
                            return sprites[i];
                    }
                }

                Sprite largest = null;
                float largestArea = 0f;
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite candidate = sprites[i];
                    if (candidate == null)
                        continue;

                    float area = candidate.rect.width * candidate.rect.height;
                    if (largest == null || area > largestArea)
                    {
                        largest = candidate;
                        largestArea = area;
                    }
                }

                if (largest != null)
                    return largest;
            }

            return Resources.Load<Sprite>(resourcePath);
        }

        private void ApplyLanguageButtonSprite(Button button, Sprite sprite)
        {
            if (button == null || button.image == null)
                return;

            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.type = Image.Type.Simple;
                button.image.preserveAspect = true;
                button.image.color = Color.white;
            }

            SetButtonLabelVisible(button, sprite == null);
        }

        private bool IsBattleSettingsSceneName(string sceneName)
        {
            return string.Equals(sceneName, battleGameplaySceneName, StringComparison.Ordinal)
                || string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal);
        }

        private bool IsMainSettingsSceneName(string sceneName)
        {
            return string.Equals(sceneName, "Main", StringComparison.Ordinal)
                || string.Equals(sceneName, mahjongLobbySceneName, StringComparison.Ordinal);
        }

        private bool IsRegularMahjongGameplaySceneName(string sceneName)
        {
            return string.Equals(sceneName, gameplaySceneName, StringComparison.Ordinal);
        }

        private bool IsSettingsAvailableSceneName(string sceneName)
        {
            return IsMainSettingsSceneName(sceneName)
                || IsBattleSettingsSceneName(sceneName)
                || IsGameplayScene(sceneName);
        }

        private bool IsMahjongLobbySceneName(string sceneName)
        {
            return string.Equals(sceneName, mahjongLobbySceneName, StringComparison.Ordinal);
        }

        private static bool IsLanguageSettingsSceneName(string sceneName)
        {
            return string.Equals(sceneName, "Main", StringComparison.Ordinal);
        }

        private void SetOpenButtonLabelVisible(bool visible)
        {
            SetButtonLabelVisible(openButton, visible);
        }

        private void SetOpenButtonLabelText(string text)
        {
            if (openButton == null)
                return;

            TMP_Text label = openButton.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            label.text = string.IsNullOrWhiteSpace(text) ? "Settings" : text;
            label.fontSize = 46f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 50f;
            label.alignment = TextAlignmentOptions.Center;
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
        }

        private void ApplyBattleLobbySettingsOpenButtonStyle()
        {
            if (openButton == null)
                return;

            BattlePopupStyle.ApplyBattleLobbyUtilityButton(openButton, 46f);
            SetOpenButtonLabelText(GameLocalization.Text("main.info.settings.title"));
            SetOpenButtonLabelVisible(true);
            EnsureOpenButtonInnerGear(false);
        }

        private static void SetButtonLabelVisible(Button button, bool visible)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.gameObject.SetActive(visible);
        }

        private static bool HasSettingButtonSprites(SettingsSceneVisualStyle style)
        {
            if (style == null)
                return false;

            return style.SoundButtonSprite != null
                || style.MusicButtonSprite != null
                || style.VibrationButtonSprite != null
                || style.CloseButtonSprite != null
                || style.ReturnButtonSprite != null
                || style.RestartButtonSprite != null
                || style.RussianLanguageSprite != null
                || style.EnglishLanguageSprite != null
                || style.TurkishLanguageSprite != null;
        }

        private SettingsSceneVisualStyle ResolveSceneVisualStyle(string sceneName)
        {
            if (sceneVisualStyles == null)
                return null;

            for (int i = 0; i < sceneVisualStyles.Length; i++)
            {
                SettingsSceneVisualStyle style = sceneVisualStyles[i];
                if (style == null || string.IsNullOrWhiteSpace(style.SceneName))
                    continue;

                if (string.Equals(style.SceneName.Trim(), sceneName, StringComparison.Ordinal))
                    return style;
            }

            return null;
        }

        private static void ApplyRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ApplyGraphic(Image image, Sprite sprite, Color color)
        {
            if (image == null)
                return;

            if (sprite != null)
            {
                image.sprite = sprite;
                bool useSliced = sprite.border.sqrMagnitude > 0.01f;
                image.type = useSliced ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = !useSliced;
            }

            image.color = color;
        }

        private static void ApplyButtonSize(Button button, Vector2 size)
        {
            if (button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = size;
        }

        private static void ApplyButtonGraphic(Button button, Sprite sprite, Color color)
        {
            if (button == null || button.image == null)
                return;

            ApplyGraphic(button.image, sprite, color);
        }

        private void ApplyBattleOpenButtonPlacement()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal))
            {
                MainLobbyUiCoordinator.LayoutBattleLobbyTopTabButton(openButton, 3, 4, ResolveSettingsCanvasSize());
                return;
            }

            if (!IsBattleGameScene)
                return;

            ApplyRect(
                openButtonRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(94f, 94f));
        }

        private Vector2 ResolveSettingsCanvasSize()
        {
            RectTransform basis = null;
            if (openButtonRect != null && openButtonRect.parent is RectTransform parentRect)
                basis = parentRect;
            else
                basis = transform as RectTransform;

            if (basis != null && basis.rect.width > 1f && basis.rect.height > 1f)
                return basis.rect.size;

            return MainLobbyUiCoordinator.OverlayReferenceResolution;
        }

        private void ApplyMainOpenButtonPlacement()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool shouldUseTopRight =
                string.Equals(sceneName, "Main", StringComparison.Ordinal) ||
                IsRegularMahjongGameplaySceneName(sceneName);

            if (!shouldUseTopRight)
                return;

            ApplyRect(
                openButtonRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                IsRegularMahjongGameplaySceneName(sceneName) ? ResolveGameplayOpenButtonPosition() : new Vector2(-36f, -22f),
                IsRegularMahjongGameplaySceneName(sceneName) ? new Vector2(88f, 88f) : new Vector2(118f, 118f));
        }

        private Vector2 ResolveGameplayOpenButtonPosition()
        {
            RectTransform basis = panelRootRect != null ? panelRootRect : transform as RectTransform;
            Vector2 canvasSize = basis != null && basis.rect.width > 0f && basis.rect.height > 0f
                ? basis.rect.size
                : new Vector2(1920f, 1080f);

            Rect safe = Screen.safeArea;
            float safeRight = 0f;
            float safeTop = 0f;

            if (Screen.width > 0 && Screen.height > 0 && safe.width > 0f && safe.height > 0f)
            {
                safeRight = Mathf.Max(0f, Screen.width - safe.xMax) * (canvasSize.x / Screen.width);
                safeTop = Mathf.Max(0f, Screen.height - safe.yMax) * (canvasSize.y / Screen.height);
            }

            float x = -Mathf.Max(82f, safeRight + 58f);
            float y = -Mathf.Max(92f, safeTop + 64f);
            return new Vector2(x, y);
        }

        private void ApplyBambooMahjongSettingsVisuals(string sceneName)
        {
            bool isGameplay = IsRegularMahjongGameplaySceneName(sceneName);
            bool isLobby = IsMahjongLobbySceneName(sceneName);
            if (!isGameplay && !isLobby)
                return;

            float scale = ResolveBambooSettingsScale();

            Sprite openSprite = LoadBambooSettingsGearIconSprite() ?? LoadMahjongLobbySettingsGearSprite() ?? LoadMainSettingsButtonSprite();
            if (openSprite != null && openButton != null && openButton.image != null)
            {
                openButton.image.enabled = true;
                openButton.image.sprite = null;
                openButton.image.type = Image.Type.Simple;
                openButton.image.preserveAspect = false;
                openButton.image.color = Color.clear;
                openButton.image.raycastTarget = true;
                FreezeButtonColors(openButton);
                openButton.image.color = Color.clear;
                SetOpenButtonLabelVisible(false);
                EnsureOpenButtonInnerGear(true);
            }

            if (panelBackgroundImage != null)
            {
                panelBackgroundImage.enabled = true;
                panelBackgroundImage.color = isGameplay ? new Color(0f, 0f, 0f, 0.34f) : Color.clear;
                panelBackgroundImage.raycastTarget = true;
            }

            SetRuntimeChildVisible(panelRoot != null ? panelRoot.transform : null, "WindowShadow", false);

            Sprite windowSprite = LoadBambooSettingsWindowSprite() ?? LoadMahjongLobbySettingsWindowSprite() ?? LoadMainSettingsWindowSprite();
            if (windowSprite != null && windowRect != null)
            {
                Image targetImage = EnsureMainSettingsWindowGraphicImage();
                ApplyTransparentRaycastImage(windowImage);
                ApplyMainSettingsWindowImage(targetImage, windowSprite, false);
                SetMainSettingsWindowGraphicVisible(true);
                if (targetImage != null)
                    targetImage.transform.SetAsFirstSibling();
            }

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1000f, 610f) * scale);

            Sprite smallButtonSprite = LoadBambooSettingsMediumButtonSprite()
                                      ?? LoadBambooSettingsLongButtonSprite()
                                      ?? LoadMahjongLobbySettingsLargeButtonSprite()
                                      ?? LoadMainSettingsTextButtonSprite();
            Sprite wideButtonSprite = LoadBambooSettingsLongButtonSprite() ?? smallButtonSprite;

            ApplyTextButtonGraphic(soundButton, smallButtonSprite);
            ApplyTextButtonGraphic(musicButton, smallButtonSprite);
            ApplyTextButtonGraphic(vibrationButton, smallButtonSprite);
            ApplyTextButtonGraphic(returnToMenuButton, wideButtonSprite);
            ApplyTextButtonGraphic(restartButton, wideButtonSprite);
            ApplyTextButtonGraphic(changeProfileButton, wideButtonSprite);
            ApplyTextButtonGraphic(logoutButton, wideButtonSprite);
            ApplyTextButtonGraphic(closeButton, wideButtonSprite);

            ApplyButtonRect(soundButton, new Vector2(-255f, 112f) * scale, new Vector2(250f, 86f) * scale);
            ApplyButtonRect(musicButton, new Vector2(0f, 112f) * scale, new Vector2(250f, 86f) * scale);
            ApplyButtonRect(vibrationButton, new Vector2(255f, 112f) * scale, new Vector2(250f, 86f) * scale);

            if (isGameplay)
            {
                ApplyTextButtonGraphic(returnToMenuButton, smallButtonSprite);
                ApplyTextButtonGraphic(restartButton, smallButtonSprite);
                ApplyButtonRect(returnToMenuButton, new Vector2(-145f, -34f) * scale, new Vector2(260f, 86f) * scale);
                ApplyButtonRect(restartButton, new Vector2(145f, -34f) * scale, new Vector2(260f, 86f) * scale);
            }
            else
            {
                ApplyButtonRect(changeProfileButton, new Vector2(-190f, -42f) * scale, new Vector2(350f, 92f) * scale);
                ApplyButtonRect(logoutButton, new Vector2(190f, -42f) * scale, new Vector2(350f, 92f) * scale);
            }

            ApplyTextButtonGraphic(closeButton, smallButtonSprite);
            ApplyButtonRect(closeButton, new Vector2(0f, -168f) * scale, new Vector2(250f, 82f) * scale);

            SetDecorativeLineVisible("TopAmberLine", false);
            SetDecorativeLineVisible("BottomJadeLine", false);
            RefreshButtons();
        }

        private void EnsureOpenButtonInnerGear(bool visible)
        {
            if (openButton == null)
                return;

            if (!visible)
            {
                if (openButtonInnerGearImage != null)
                    openButtonInnerGearImage.gameObject.SetActive(false);
                StopOpenButtonGearSpin();
                return;
            }

            Sprite gearSprite = LoadBambooSettingsGearIconSprite() ?? LoadMahjongLobbySettingsGearSprite() ?? LoadMainSettingsButtonSprite();
            if (gearSprite == null)
                return;

            if (openButtonInnerGearImage == null)
            {
                Transform existing = openButton.transform.Find("InnerGear");
                if (existing != null)
                    openButtonInnerGearImage = existing.GetComponent<Image>();

                if (openButtonInnerGearImage == null)
                {
                    GameObject gearObject = new GameObject("InnerGear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    gearObject.transform.SetParent(openButton.transform, false);
                    openButtonInnerGearImage = gearObject.GetComponent<Image>();
                    openButtonInnerGearImage.raycastTarget = false;
                }
            }

            RectTransform rect = openButtonInnerGearImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.sizeDelta = new Vector2(62f, 62f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            openButtonInnerGearImage.sprite = gearSprite;
            openButtonInnerGearImage.type = Image.Type.Simple;
            openButtonInnerGearImage.preserveAspect = true;
            openButtonInnerGearImage.color = Color.white;
            openButtonInnerGearImage.gameObject.SetActive(true);
            openButtonInnerGearImage.transform.SetAsLastSibling();

            if (openButtonGearSpinRoutine == null && gameObject.activeInHierarchy)
                openButtonGearSpinRoutine = StartCoroutine(OpenButtonGearSpinRoutine(rect));
        }

        private IEnumerator OpenButtonGearSpinRoutine(RectTransform gearRect)
        {
            float direction = 1f;
            float elapsed = 0f;
            float angle = 0f;
            const float switchSeconds = 4.25f;
            const float speed = 18f;

            while (gearRect != null && gearRect.gameObject.activeInHierarchy)
            {
                float delta = Time.unscaledDeltaTime;
                elapsed += delta;
                if (elapsed >= switchSeconds)
                {
                    elapsed = 0f;
                    direction *= -1f;
                }

                gearRect.anchoredPosition = Vector2.zero;
                gearRect.localPosition = Vector3.zero;
                angle += direction * speed * delta;
                gearRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            openButtonGearSpinRoutine = null;
        }

        private void StopOpenButtonGearSpin()
        {
            if (openButtonGearSpinRoutine == null)
                return;

            StopCoroutine(openButtonGearSpinRoutine);
            openButtonGearSpinRoutine = null;
        }

        private float ResolveBambooSettingsScale()
        {
            RectTransform basis = panelRootRect != null ? panelRootRect : transform as RectTransform;
            Vector2 size = basis != null && basis.rect.width > 0f && basis.rect.height > 0f
                ? basis.rect.size
                : new Vector2(1920f, 1080f);

            float widthScale = (size.x - 180f) / 1000f;
            float heightScale = (size.y - 190f) / 610f;
            float scale = Mathf.Min(widthScale, heightScale);

            return Mathf.Clamp(scale, 0.74f, 1.04f);
        }

        private void ApplyBattleSettingsWindowLayout()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!IsBattleSettingsSceneName(sceneName))
                return;

            if (string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal))
            {
                ApplyRect(
                    windowRect,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0f),
                    new Vector2(1320f, 760f));

                ApplyButtonRect(soundButton, new Vector2(-390f, 188f), new Vector2(300f, 84f));
                ApplyButtonRect(musicButton, new Vector2(0f, 188f), new Vector2(300f, 84f));
                ApplyButtonRect(vibrationButton, new Vector2(390f, 188f), new Vector2(300f, 84f));

                ApplyButtonRect(changeProfileButton, new Vector2(-310f, 32f), new Vector2(450f, 86f));
                ApplyButtonRect(logoutButton, new Vector2(310f, 32f), new Vector2(450f, 86f));
                ApplyButtonRect(closeButton, new Vector2(0f, -292f), new Vector2(340f, 76f));
                return;
            }

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1320f, 760f));

            ApplyButtonRect(soundButton, new Vector2(-390f, 198f), new Vector2(300f, 90f));
            ApplyButtonRect(musicButton, new Vector2(0f, 198f), new Vector2(300f, 90f));
            ApplyButtonRect(vibrationButton, new Vector2(390f, 198f), new Vector2(300f, 90f));

            ApplyButtonRect(returnToMenuButton, new Vector2(-290f, 42f), new Vector2(380f, 90f));
            ApplyButtonRect(restartButton, new Vector2(290f, 42f), new Vector2(380f, 90f));
            ApplyButtonRect(surrenderButton, new Vector2(0f, -112f), new Vector2(420f, 92f));
            ApplyButtonRect(closeButton, new Vector2(0f, -304f), new Vector2(320f, 80f));
        }

        private static void ApplyButtonRect(Button button, Vector2 position, Vector2 size)
        {
            if (button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ApplyAnchoredButtonRect(Button button, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private Button FindButtonByName(string buttonName)
        {
            Transform found = FindChildByName(transform, buttonName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }

        private void ReturnToMenu()
        {
            CloseInstant();

            string activeScene = SceneManager.GetActiveScene().name;
            string sceneName = ResolveReturnSceneName(activeScene);
            LoadSceneWithDoor(sceneName);
        }

        private void SurrenderBattle()
        {
            if (surrenderAdInProgress)
                return;

            if (BattleLoreTutorialSession.IsActive || NoAdsService.HasActiveNoAds())
            {
                CompleteSurrenderBattle();
                return;
            }

            surrenderAdInProgress = true;
            if (surrenderButton != null)
                surrenderButton.interactable = false;

            StartCoroutine(ShowSurrenderAdThenComplete());
        }

        private IEnumerator ShowSurrenderAdThenComplete()
        {
            MonetizationService service = MonetizationService.Ensure();
            string placementId = MonetizationService.SurrenderInterstitialPlacementId;
            float deadline = Time.unscaledTime + 5f;
            while (Time.unscaledTime < deadline && !service.CanShowInterstitialAd(placementId))
                yield return null;

            service.ShowInterstitialAd(placementId, result =>
            {
                surrenderAdInProgress = false;

                Debug.Log($"[SettingsMenuUI] Surrender interstitial finished. State={result.State} Message={result.Message}");
                CompleteSurrenderBattle();
            });
        }

        private void CompleteSurrenderBattle()
        {
            CloseInstant();
            ForceCloseAllSettingsMenus();

            BattleMatchController battleMatchController = FindAnyObjectByType<BattleMatchController>(FindObjectsInactive.Include);
            if (battleMatchController != null && !battleMatchController.IsMatchFinished)
                battleMatchController.ForceForfeitMatch();

            MahjongSession.Clear();
            LoadSceneWithDoor(battleLobbySceneName);
        }

        private void RestartScene()
        {
            CloseInstant();

            Scene currentScene = SceneManager.GetActiveScene();
            LoadSceneWithDoor(currentScene.name);
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[SettingsMenuUI] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName, ResolveDoorSpriteResourcePath(sceneName), ShouldReverseDoorMirroring(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }

        private string ResolveDoorSpriteResourcePath(string sceneName)
        {
            if (string.Equals(sceneName, battleLobbySceneName, StringComparison.Ordinal) ||
                string.Equals(sceneName, battleGameplaySceneName, StringComparison.Ordinal) ||
                IsBattleGameScene)
                return BattleDoorSpriteResourcePath;

            if (string.Equals(sceneName, mahjongLobbySceneName, StringComparison.Ordinal) ||
                string.Equals(sceneName, gameplaySceneName, StringComparison.Ordinal) ||
                string.Equals(SceneManager.GetActiveScene().name, mahjongLobbySceneName, StringComparison.Ordinal) ||
                string.Equals(SceneManager.GetActiveScene().name, gameplaySceneName, StringComparison.Ordinal))
                return StoryEndlessDoorSpriteResourcePath;

            return null;
        }

        private bool ShouldReverseDoorMirroring(string sceneName)
        {
            return false;
        }

        private void ApplySceneMode()
        {
            bool showGameButtons = IsGameScene;
            bool showSurrender = IsBattleGameScene;
            bool showProfileActions = !IsGameScene;

            if (gameButtonsRoot != null)
                gameButtonsRoot.SetActive(showGameButtons);

            if (changeProfileButton != null)
                changeProfileButton.gameObject.SetActive(showProfileActions);

            if (logoutButton != null)
                logoutButton.gameObject.SetActive(showProfileActions);

            if (returnToMenuButton != null)
                returnToMenuButton.gameObject.SetActive(showGameButtons);

            if (restartButton != null)
                restartButton.gameObject.SetActive(showGameButtons);

            if (surrenderButton != null)
                surrenderButton.gameObject.SetActive(showSurrender);

            if (infoHintsButton != null)
                infoHintsButton.gameObject.SetActive(
                    MainInfoHintTarget.FeatureEnabled &&
                    string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.Ordinal));

            bool showLanguageButtons = IsLanguageSettingsSceneName(SceneManager.GetActiveScene().name);
            SetLanguageButtonsActive(showLanguageButtons);

            ApplyBattleSettingsWindowLayout();
            ApplyBattleSettingsVisuals(SceneManager.GetActiveScene().name);
        }

        private void SetLanguageButtonsActive(bool active)
        {
            if (russianLanguageButton != null)
                russianLanguageButton.gameObject.SetActive(active);

            if (englishLanguageButton != null)
                englishLanguageButton.gameObject.SetActive(active);

            if (turkishLanguageButton != null)
                turkishLanguageButton.gameObject.SetActive(active);

            if (germanLanguageButton != null)
                germanLanguageButton.gameObject.SetActive(active);
        }

        private void RefreshOpenButtonVisibility()
        {
            if (openButton == null)
                return;

            bool battleGameplay = IsBattleGameScene;
            bool blockedByBattleModal = !battleGameplay && BattleLobbyUiCoordinator.HasModalOpen &&
                                        BattleLobbyUiCoordinator.ActiveModal != BattleLobbyModalKind.Settings;
            bool suppressed = !battleGameplay && mainSettingsButtonSuppressed;
            bool blockedByIntro = !battleGameplay && IsBlockedByIntro;
            // Suppression only hides the launcher while a Main overlay (including
            // this settings panel) owns the screen. It must not make an already
            // open settings panel close itself on the next Update.
            bool available = IsSettingsAvailableScene && !blockedByIntro && !blockedByBattleModal;
            bool panelOpen = panelRoot != null && panelRoot.activeSelf;
            bool shouldShow = available && !suppressed && !panelOpen;

            if (openButton.gameObject.activeSelf != shouldShow)
                openButton.gameObject.SetActive(shouldShow);

            if (shouldShow && battleGameplay)
            {
                openButton.transform.SetAsLastSibling();
                openButton.interactable = Time.unscaledTime >= battleOpenButtonReadyAt;
                if (openButton.image != null)
                {
                    openButton.image.enabled = true;
                    openButton.image.raycastTarget = true;
                }
            }
            else
            {
                openButton.interactable = shouldShow;
            }

            if (!available && panelOpen)
                CloseInstant();
        }

        private void EnsureSettingsInstance()
        {
            if (AppSettings.I != null)
                return;

            GameObject go = new GameObject("AppSettings");
            go.AddComponent<AppSettings>();
        }

        private void EnsurePersistentRoot()
        {
            if (persistentRoot != null)
                return;

            persistentRoot = new GameObject("PersistentSettingsUI");

            Canvas canvas = persistentRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30100;

            CanvasScaler scaler = persistentRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            persistentRoot.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(persistentRoot);
        }

        private bool IsGameplayScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (string.Equals(sceneName, gameplaySceneName, StringComparison.Ordinal))
                return true;

            if (gameplaySceneNames == null)
                return false;

            for (int i = 0; i < gameplaySceneNames.Length; i++)
            {
                string candidate = gameplaySceneNames[i];
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (string.Equals(candidate.Trim(), sceneName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private string ResolveReturnSceneName(string activeScene)
        {
            if (string.Equals(activeScene, battleGameplaySceneName, StringComparison.Ordinal))
                return battleLobbySceneName;

            if (!string.IsNullOrWhiteSpace(mahjongLobbySceneName))
                return mahjongLobbySceneName;

            return AppSettings.I != null ? AppSettings.I.MainMenuSceneName : "LobbyMahjong";
        }

        private void EnsureRuntimeUi()
        {
            if (openButton != null && panelRoot != null)
                return;

            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            openButton = CreateRuntimeIconButton(transform, "BtnOpenSettings", new Vector2(1f, 1f), new Vector2(-58f, -42f), new Vector2(82f, 82f), "Menü");
            openButtonRect = openButton.GetComponent<RectTransform>();

            panelRoot = CreateRuntimePanel(transform, "PanelRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.02f, 0.015f, 0.012f, 0.78f));
            panelRootRect = panelRoot.GetComponent<RectTransform>();
            panelBackgroundImage = panelRoot.GetComponent<Image>();

            GameObject shadow = CreateRuntimePanel(panelRoot.transform, "WindowShadow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, -18f), new Vector2(920f, 680f), new Color(0f, 0f, 0f, 0.42f));
            shadow.GetComponent<Image>().raycastTarget = false;

            GameObject window = CreateRuntimePanel(panelRoot.transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 660f), new Color(0.075f, 0.06f, 0.047f, 0.97f));
            windowRect = window.GetComponent<RectTransform>();
            windowImage = window.GetComponent<Image>();
            enabledColor = new Color(0.95f, 0.58f, 0.22f, 0.98f);
            disabledColor = new Color(0.16f, 0.14f, 0.12f, 0.92f);

            soundButton = CreateRuntimeTextButton(window.transform, "BtnSound", new Vector2(0.5f, 0.5f), new Vector2(-260f, 120f), new Vector2(190f, 76f), "Sound", "settings.sound", RuntimeButtonStyle.Setting);
            musicButton = CreateRuntimeTextButton(window.transform, "BtnMusic", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(190f, 76f), "Music", "settings.music", RuntimeButtonStyle.Setting);
            vibrationButton = CreateRuntimeTextButton(window.transform, "BtnVibration", new Vector2(0.5f, 0.5f), new Vector2(260f, 120f), new Vector2(190f, 76f), "Vibration", "settings.vibration", RuntimeButtonStyle.Setting);

            russianLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageRU", new Vector2(0.5f, 0.5f), new Vector2(-315f, 10f), new Vector2(140f, 76f), "RU", "settings.language_ru", RuntimeButtonStyle.Language);
            englishLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageEN", new Vector2(0.5f, 0.5f), new Vector2(-105f, 10f), new Vector2(140f, 76f), "EN", "settings.language_en", RuntimeButtonStyle.Language);
            turkishLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageTR", new Vector2(0.5f, 0.5f), new Vector2(105f, 10f), new Vector2(140f, 76f), "TR", "settings.language_tr", RuntimeButtonStyle.Language);
            germanLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageDE", new Vector2(0.5f, 0.5f), new Vector2(315f, 10f), new Vector2(140f, 76f), "DE", "settings.language_de", RuntimeButtonStyle.Language);
            changeProfileButton = CreateRuntimeTextButton(window.transform, "BtnChangeProfile", new Vector2(0.5f, 0.5f), new Vector2(-170f, -95f), new Vector2(300f, 76f), "Change Profile", "settings.change_profile", RuntimeButtonStyle.Action);
            logoutButton = CreateRuntimeTextButton(window.transform, "BtnLogoutProfile", new Vector2(0.5f, 0.5f), new Vector2(170f, -95f), new Vector2(300f, 76f), "Logout", "settings.logout", RuntimeButtonStyle.Danger);

            gameButtonsRoot = new GameObject("GameButtonsRoot", typeof(RectTransform));
            gameButtonsRoot.transform.SetParent(window.transform, false);

            RectTransform gameButtonsRect = gameButtonsRoot.GetComponent<RectTransform>();
            gameButtonsRect.anchorMin = new Vector2(0.5f, 0.5f);
            gameButtonsRect.anchorMax = new Vector2(0.5f, 0.5f);
            gameButtonsRect.pivot = new Vector2(0.5f, 0.5f);
            gameButtonsRect.anchoredPosition = Vector2.zero;
            gameButtonsRect.sizeDelta = Vector2.zero;

            returnToMenuButton = CreateRuntimeTextButton(gameButtonsRoot.transform, "BtnReturn", new Vector2(0.5f, 0.5f), new Vector2(-225f, -130f), new Vector2(250f, 78f), "Menu", "settings.menu", RuntimeButtonStyle.Action);
            restartButton = CreateRuntimeTextButton(gameButtonsRoot.transform, "BtnRestart", new Vector2(0.5f, 0.5f), new Vector2(225f, -130f), new Vector2(250f, 78f), "Restart", "settings.restart", RuntimeButtonStyle.Action);
            surrenderButton = CreateRuntimeTextButton(gameButtonsRoot.transform, "BtnSurrender", new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(300f, 78f), "Surrender", "settings.surrender", RuntimeButtonStyle.Danger);

            closeButton = CreateRuntimeTextButton(window.transform, "BtnClose", new Vector2(0.5f, 0.5f), new Vector2(0f, -325f), new Vector2(210f, 70f), "Close", "settings.close", RuntimeButtonStyle.Close);
        }

        private static GameObject CreateRuntimePanel(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            return go;
        }

        private static Button CreateRuntimeIconButton(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, string label)
        {
            Button button = CreateRuntimeTextButton(parent, objectName, anchor, position, size, label);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.fontSize = 24f;

            return button;
        }

        private enum RuntimeButtonStyle
        {
            Setting,
            Language,
            Action,
            Danger,
            Close
        }

        private static Button CreateRuntimeTextButton(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, string label, string localizationKey = null, RuntimeButtonStyle style = RuntimeButtonStyle.Setting)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = ResolveRuntimeButtonColor(style);
            image.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = string.IsNullOrWhiteSpace(localizationKey) ? label : GameLocalization.Text(localizationKey);
            MainLobbyButtonStyle.ApplyFont(text);
            text.fontSize = 28f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ResolveRuntimeButtonTextColor(style);
            MainLobbyButtonStyle.ApplySilverTextEffect(text);
            text.raycastTarget = false;

            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedText localizedText = textObject.AddComponent<LocalizedText>();
                localizedText.SetKey(localizationKey);
            }

            go.SetActive(true);
            return button;
        }

        private static Color ResolveRuntimeButtonColor(RuntimeButtonStyle style)
        {
            return style switch
            {
                RuntimeButtonStyle.Language => new Color(0.13f, 0.23f, 0.2f, 0.94f),
                RuntimeButtonStyle.Action => new Color(0.18f, 0.22f, 0.25f, 0.94f),
                RuntimeButtonStyle.Danger => new Color(0.48f, 0.13f, 0.09f, 0.95f),
                RuntimeButtonStyle.Close => new Color(0.68f, 0.38f, 0.13f, 0.95f),
                _ => new Color(0.16f, 0.14f, 0.12f, 0.95f)
            };
        }

        private static Color ResolveRuntimeButtonTextColor(RuntimeButtonStyle style)
        {
            return style == RuntimeButtonStyle.Close
                ? new Color(1f, 0.92f, 0.78f, 1f)
                : new Color(0.96f, 0.92f, 0.84f, 1f);
        }
    }
}
