using System;
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

        private static SettingsMenuUI instance;
        private static GameObject persistentRoot;
        private const string BattleSettingsButtonResourcePath = "Mahjong/Sprites/BattleSettingsButton";
        private const string BattleLobbySettingsWindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/SettingsBattleWindow";
        private const string MainButtonStandardResourcePath = "Mahjong/Sprites/MainSettings/BtnMainStandart";
        private const string MainSettingsButtonResourcePath = "Mahjong/Sprites/MainSettings/SettingsButtonMain";
        private const string MainSettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MainSettingsWindow";
        private const string MainSettingsTextButtonResourcePath = MainButtonStandardResourcePath;
        private const string RussianLanguageButtonResourcePath = "Mahjong/Sprites/RuButton";
        private const string EnglishLanguageButtonResourcePath = "Mahjong/Sprites/EngButton";
        private const string TurkishLanguageButtonResourcePath = "Mahjong/Sprites/TrButton";

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
        [SerializeField] private Button russianLanguageButton;
        [SerializeField] private Button englishLanguageButton;
        [SerializeField] private Button turkishLanguageButton;
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
            "GameMahjongBattle",
            "GameOkey",
            "Voider",
            "Tetris"
        };
        [SerializeField] private string battleGameplaySceneName = "GameMahjongBattle";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string okeyLobbySceneName = "LobbyOkey";
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
        private Sprite cachedMainSettingsTextButtonSprite;
        private Sprite cachedRussianLanguageButtonSprite;
        private Sprite cachedEnglishLanguageButtonSprite;
        private Sprite cachedTurkishLanguageButtonSprite;
        private Sprite cachedBattleLobbyWindowSourceSprite;
        private Sprite cachedBattleLobbyWindowSprite;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntimeInstance()
        {
            if (instance != null)
                return;

            SettingsMenuUI existing = FindAnyObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            GameObject go = new GameObject("SettingsMenu", typeof(RectTransform));
            go.AddComponent<SettingsMenuUI>();
        }

        private bool IsBlockedByIntro
        {
            get
            {
                if (!hideOpenButtonWhileIntroPanelActive)
                    return false;

                return introPanel != null && introPanel.activeInHierarchy;
            }
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
            EnsureLogoutButton();
            EnsureChangeProfileButton();
            EnsureSurrenderButton();
            EnsureDefaultVisualStyles();

            if (panelRoot != null)
                panelRoot.SetActive(false);

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
            EnsureSettingsInstance();

            RefreshButtons();
            ApplySceneMode();
            ApplySceneVisualStyle();
            RefreshOpenButtonVisibility();

            if (IsBlockedByIntro)
                return;

            if (AppSettings.I != null)
                AppSettings.I.RefreshAndApplyAudio();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                string activeSceneName = SceneManager.GetActiveScene().name;
                ApplyMainSettingsVisuals(activeSceneName);
                ApplyBattleSettingsVisuals(activeSceneName);
                RefreshButtons();
            }

            if (IsGameScene && pauseGameWhenOpened)
            {
                cachedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            AppSettings.I?.Vibrate();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (IsGameScene && pauseGameWhenOpened)
                Time.timeScale = cachedTimeScale;
        }

        private void CloseInstant()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;
        }

        private void BindUI()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
                openButton.onClick.AddListener(Open);
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

            if (russianLanguageButton != null)
                russianLanguageButton.onClick.RemoveListener(SetRussianLanguage);

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.RemoveListener(SetEnglishLanguage);

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.RemoveListener(SetTurkishLanguage);

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

            ApplyButtonColor(soundButton, AppSettings.I.SoundEnabled);
            ApplyButtonColor(musicButton, AppSettings.I.MusicEnabled);
            ApplyButtonColor(vibrationButton, AppSettings.I.VibrationEnabled);
            ApplyLanguageButtonColor(russianLanguageButton, AppSettings.I.Language == GameLanguage.Russian);
            ApplyLanguageButtonColor(englishLanguageButton, AppSettings.I.Language == GameLanguage.English);
            ApplyLanguageButtonColor(turkishLanguageButton, AppSettings.I.Language == GameLanguage.Turkish);
        }

        private void ApplyButtonColor(Button button, bool isEnabled)
        {
            if (button == null || button.image == null)
                return;

            button.image.color = isEnabled ? enabledColor : disabledColor;
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
                || sprite == cachedTurkishLanguageButtonSprite)
            {
                return true;
            }

            return string.Equals(sprite.name, "RuButton_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "EngButton_0", StringComparison.Ordinal)
                || string.Equals(sprite.name, "TrButton_0", StringComparison.Ordinal);
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
                    OpenButtonPosition = new Vector2(-390f, -42f),
                    OpenButtonSize = new Vector2(90f, 90f)
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
                ApplyBattleSettingsWindowLayout();
                ApplyBattleSettingsVisuals(activeSceneName);
                ApplyBattleOpenButtonSprite(activeSceneName, null);
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
                ApplyBattleSettingsWindowLayout();
                ApplyBattleSettingsVisuals(activeSceneName);
                ApplyBattleOpenButtonSprite(activeSceneName, null);
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
            }

            if (style.ApplySettingButtonColors)
            {
                enabledColor = style.EnabledColor;
                disabledColor = style.DisabledColor;
                RefreshButtons();
            }

            if (style.ApplySettingButtonGraphics || style.ApplyLanguageButtonGraphics || HasSettingButtonSprites(style))
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
                RefreshButtons();
            }

            ApplyLanguageButtonSprites(style);

            ApplyBattleOpenButtonPlacement();
            ApplyMainOpenButtonPlacement();
            ApplyMainSettingsVisuals(activeSceneName);
            ApplyBattleSettingsWindowLayout();
            ApplyBattleSettingsVisuals(activeSceneName);
        }

        private void ApplyOpenButtonGraphic(SettingsSceneVisualStyle style, string sceneName)
        {
            if (style == null)
                return;

            Sprite sprite = style.OpenButtonSprite;
            if (IsBattleSettingsSceneName(sceneName))
                sprite = LoadBattleSettingsButtonSprite() ?? sprite;
            else if (IsMainSettingsSceneName(sceneName))
                sprite = LoadMainSettingsButtonSprite() ?? sprite;

            if ((style.ApplyOpenButtonGraphic || sprite != null) && openButton != null && openButton.image != null)
                ApplyGraphic(openButton.image, sprite, style.OpenButtonColor);

            SetOpenButtonLabelVisible(sprite == null);
        }

        private void ApplyBattleOpenButtonSprite(string sceneName, Sprite fallbackSprite)
        {
            if (!IsBattleSettingsSceneName(sceneName) || openButton == null || openButton.image == null)
                return;

            Sprite sprite = LoadBattleSettingsButtonSprite() ?? fallbackSprite;
            if (sprite == null)
                return;

            ApplyGraphic(openButton.image, sprite, Color.white);
            SetOpenButtonLabelVisible(false);
        }

        private void ApplyMainSettingsVisuals(string sceneName)
        {
            if (!IsMainSettingsSceneName(sceneName))
            {
                SetMainSettingsWindowGraphicVisible(false);
                return;
            }

            Sprite openSprite = LoadMainSettingsButtonSprite();
            if (openSprite != null && openButton != null && openButton.image != null)
            {
                ApplyGraphic(openButton.image, openSprite, Color.white);
                openButton.image.type = Image.Type.Simple;
                openButton.image.preserveAspect = true;
                SetOpenButtonLabelVisible(false);
            }

            ApplyMainSettingsWindowGraphic();

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1100f, 740f));

            SetDecorativeLineVisible("TopAmberLine", false);
            SetDecorativeLineVisible("BottomJadeLine", false);
            ApplyMainTextButtonVisuals();
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

        private void ApplyMainTextButtonVisuals()
        {
            Sprite textButtonSprite = LoadMainSettingsTextButtonSprite();
            if (textButtonSprite == null)
                return;

            ApplyTextButtonGraphic(soundButton, textButtonSprite);
            ApplyTextButtonGraphic(musicButton, textButtonSprite);
            ApplyTextButtonGraphic(vibrationButton, textButtonSprite);
            ApplyTextButtonGraphic(changeProfileButton, textButtonSprite);
            ApplyTextButtonGraphic(logoutButton, textButtonSprite);
            ApplyTextButtonGraphic(closeButton, textButtonSprite);
        }

        private void ApplyMainSettingsWindowLayout(string sceneName)
        {
            Vector2 openButtonPosition = string.Equals(sceneName, "Main", StringComparison.Ordinal)
                ? new Vector2(-58f, -42f)
                : new Vector2(-78f, -42f);

            ApplyRect(
                openButtonRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                openButtonPosition,
                string.Equals(sceneName, "Main", StringComparison.Ordinal) ? new Vector2(82.5f, 82.5f) : new Vector2(110f, 110f));

            ApplyButtonRect(soundButton, new Vector2(-320f, 160f), new Vector2(260f, 90f));
            ApplyButtonRect(musicButton, new Vector2(0f, 160f), new Vector2(260f, 90f));
            ApplyButtonRect(vibrationButton, new Vector2(320f, 160f), new Vector2(260f, 90f));

            ApplyButtonRect(russianLanguageButton, new Vector2(-260f, 36f), new Vector2(150f, 90f));
            ApplyButtonRect(englishLanguageButton, new Vector2(0f, 36f), new Vector2(150f, 90f));
            ApplyButtonRect(turkishLanguageButton, new Vector2(260f, 36f), new Vector2(150f, 90f));

            ApplyButtonRect(changeProfileButton, new Vector2(-205f, -126f), new Vector2(340f, 118f));
            ApplyButtonRect(logoutButton, new Vector2(205f, -126f), new Vector2(340f, 118f));
            ApplyButtonRect(closeButton, new Vector2(0f, -300f), new Vector2(270f, 94f));
        }

        private static void ApplyTextButtonGraphic(Button button, Sprite sprite)
        {
            if (button == null || button.image == null || sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = true;
            button.image.color = Color.white;
            SetButtonLabelVisible(button, true);
            MainLobbyButtonStyle.Apply(button);
        }

        private Sprite LoadBattleSettingsButtonSprite()
        {
            if (cachedBattleSettingsButtonSprite != null)
                return cachedBattleSettingsButtonSprite;

            cachedBattleSettingsButtonSprite = LoadFirstSprite(BattleSettingsButtonResourcePath, null);
            return cachedBattleSettingsButtonSprite;
        }

        private Sprite LoadMainSettingsButtonSprite()
        {
            if (cachedMainSettingsButtonSprite != null)
                return cachedMainSettingsButtonSprite;

            cachedMainSettingsButtonSprite = LoadFirstSprite(MainSettingsButtonResourcePath, "SettingsButtonMain_0");
            return cachedMainSettingsButtonSprite;
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

        private static bool IsUsableSettingsWindowSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            return sprite.rect.width >= 500f && sprite.rect.height >= 300f;
        }

        private void ApplyMainSettingsWindowGraphic()
        {
            if (windowImage == null && windowRect != null)
                windowImage = windowRect.GetComponent<Image>();

            if (windowRect == null)
                return;

            Sprite windowSprite = LoadMainSettingsWindowSprite();
            if (windowSprite == null)
                return;

            Image targetImage = EnsureMainSettingsWindowGraphicImage();
            if (targetImage == null)
                return;

            ApplyMainSettingsWindowImage(windowImage, windowSprite, true);
            ApplyMainSettingsWindowImage(targetImage, windowSprite, false);
            SetMainSettingsWindowGraphicVisible(true);

            targetImage.transform.SetAsFirstSibling();
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

            Sprite sprite = ResolveSceneWindowSprite(sceneName, null);
            if (sprite != null && windowImage != null)
                ApplyGraphic(windowImage, sprite, Color.white);

            ApplyBattleSettingsButtonVisuals();
        }

        private void ApplyBattleSettingsButtonVisuals()
        {
            ApplyBattleSettingToggleButtonVisual(soundButton);
            ApplyBattleSettingToggleButtonVisual(musicButton);
            ApplyBattleSettingToggleButtonVisual(vibrationButton);

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
        }

        private static void ApplyBattleActionButtonVisual(Button button)
        {
            BattlePopupStyle.ApplyButton(button);
        }

        private Sprite LoadMainSettingsTextButtonSprite()
        {
            if (cachedMainSettingsTextButtonSprite != null)
                return cachedMainSettingsTextButtonSprite;

            cachedMainSettingsTextButtonSprite = LoadFirstSprite(MainSettingsTextButtonResourcePath, "BtnMainStandart_0");
            return cachedMainSettingsTextButtonSprite;
        }

        private void ApplyLanguageButtonSprites(SettingsSceneVisualStyle style)
        {
            Sprite russianSprite = LoadRussianLanguageButtonSprite() ?? style?.RussianLanguageSprite;
            Sprite englishSprite = LoadEnglishLanguageButtonSprite() ?? style?.EnglishLanguageSprite;
            Sprite turkishSprite = LoadTurkishLanguageButtonSprite() ?? style?.TurkishLanguageSprite;

            ApplyLanguageButtonSprite(russianLanguageButton, russianSprite);
            ApplyLanguageButtonSprite(englishLanguageButton, englishSprite);
            ApplyLanguageButtonSprite(turkishLanguageButton, turkishSprite);
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

            return sprites[0];
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

        private void SetOpenButtonLabelVisible(bool visible)
        {
            SetButtonLabelVisible(openButton, visible);
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
            if (!IsBattleGameScene)
                return;

            ApplyRect(
                openButtonRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-58f, -42f),
                new Vector2(82f, 82f));
        }

        private void ApplyMainOpenButtonPlacement()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.Ordinal))
                return;

            ApplyRect(
                openButtonRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-58f, -42f),
                new Vector2(82.5f, 82.5f));
        }

        private void ApplyBattleSettingsWindowLayout()
        {
            if (!IsBattleSettingsSceneName(SceneManager.GetActiveScene().name))
                return;

            ApplyRect(
                windowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 620f));

            ApplyButtonRect(soundButton, new Vector2(-230f, 160f), new Vector2(180f, 74f));
            ApplyButtonRect(musicButton, new Vector2(0f, 160f), new Vector2(180f, 74f));
            ApplyButtonRect(vibrationButton, new Vector2(230f, 160f), new Vector2(180f, 74f));

            ApplyButtonRect(russianLanguageButton, new Vector2(-230f, 58f), new Vector2(180f, 70f));
            ApplyButtonRect(englishLanguageButton, new Vector2(0f, 58f), new Vector2(180f, 70f));
            ApplyButtonRect(turkishLanguageButton, new Vector2(230f, 58f), new Vector2(180f, 70f));

            ApplyButtonRect(returnToMenuButton, new Vector2(-190f, -78f), new Vector2(230f, 74f));
            ApplyButtonRect(restartButton, new Vector2(190f, -78f), new Vector2(230f, 74f));
            ApplyButtonRect(surrenderButton, new Vector2(0f, -180f), new Vector2(260f, 76f));
            ApplyButtonRect(closeButton, new Vector2(0f, -272f), new Vector2(210f, 68f));
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
            CloseInstant();

            BattleMatchController battleMatchController = FindAnyObjectByType<BattleMatchController>(FindObjectsInactive.Include);
            if (battleMatchController != null && !battleMatchController.IsMatchFinished)
                battleMatchController.ForceFinishMatch(false);

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
                DoorFx.I.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
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

            ApplyBattleSettingsWindowLayout();
            ApplyBattleSettingsVisuals(SceneManager.GetActiveScene().name);
        }

        private void RefreshOpenButtonVisibility()
        {
            if (openButton == null)
                return;

            bool shouldShow = !IsBlockedByIntro;

            if (openButton.gameObject.activeSelf != shouldShow)
                openButton.gameObject.SetActive(shouldShow);

            if (!shouldShow && panelRoot != null && panelRoot.activeSelf)
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
            canvas.sortingOrder = 9999;

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

            if (string.Equals(activeScene, "GameOkey", StringComparison.Ordinal))
                return okeyLobbySceneName;

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

            openButton = CreateRuntimeIconButton(transform, "BtnOpenSettings", new Vector2(1f, 1f), new Vector2(-58f, -42f), new Vector2(82f, 82f), "⚙");
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

            russianLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageRU", new Vector2(0.5f, 0.5f), new Vector2(-260f, 10f), new Vector2(190f, 76f), "RU", "settings.language_ru", RuntimeButtonStyle.Language);
            englishLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageEN", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(190f, 76f), "EN", "settings.language_en", RuntimeButtonStyle.Language);
            turkishLanguageButton = CreateRuntimeTextButton(window.transform, "BtnLanguageTR", new Vector2(0.5f, 0.5f), new Vector2(260f, 10f), new Vector2(190f, 76f), "TR", "settings.language_tr", RuntimeButtonStyle.Language);
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
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

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
