using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenuUI : MonoBehaviour
    {
        private static SettingsMenuUI instance;
        private static GameObject persistentRoot;

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

        [Header("Setting Buttons")]
        [SerializeField] private Button soundButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button russianLanguageButton;
        [SerializeField] private Button englishLanguageButton;
        [SerializeField] private Button turkishLanguageButton;

        [Header("Colors")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = Color.gray;

        [Header("Game Only")]
        [SerializeField] private GameObject gameButtonsRoot;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button restartButton;

        [Header("Scene Rules")]
        [SerializeField] private string gameplaySceneName = "GameMahjong";
        [SerializeField] private bool pauseGameWhenOpened = true;

        [Header("Visibility Rules")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private bool hideOpenButtonWhileIntroPanelActive = true;

        [Header("Visual Overrides Per Scene")]
        [SerializeField] private bool applyVisualOverrides = true;
        [SerializeField] private SettingsSceneVisualStyle[] sceneVisualStyles;

        private float cachedTimeScale = 1f;

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

        private bool IsGameScene => SceneManager.GetActiveScene().name == gameplaySceneName;

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
            AutoResolveVisualTargets();
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
                panelRoot.SetActive(true);

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

            if (returnToMenuButton != null)
                returnToMenuButton.onClick.RemoveListener(ReturnToMenu);

            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartScene);
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

        private void RefreshButtons()
        {
            if (AppSettings.I == null)
                return;

            ApplyButtonColor(soundButton, AppSettings.I.SoundEnabled);
            ApplyButtonColor(musicButton, AppSettings.I.MusicEnabled);
            ApplyButtonColor(vibrationButton, AppSettings.I.VibrationEnabled);
            ApplyButtonColor(russianLanguageButton, AppSettings.I.Language == GameLanguage.Russian);
            ApplyButtonColor(englishLanguageButton, AppSettings.I.Language == GameLanguage.English);
            ApplyButtonColor(turkishLanguageButton, AppSettings.I.Language == GameLanguage.Turkish);
        }

        private void ApplyButtonColor(Button button, bool isEnabled)
        {
            if (button == null || button.image == null)
                return;

            button.image.color = isEnabled ? enabledColor : disabledColor;
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
        }

        private void EnsureDefaultVisualStyles()
        {
            if (sceneVisualStyles != null && sceneVisualStyles.Length > 0)
                return;

            sceneVisualStyles = new[]
            {
                new SettingsSceneVisualStyle
                {
                    SceneName = "GameMahjongBattle",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(0.5f, 1f),
                    OpenButtonAnchorMax = new Vector2(0.5f, 1f),
                    OpenButtonPivot = new Vector2(0.5f, 1f),
                    OpenButtonPosition = new Vector2(0f, -28f),
                    OpenButtonSize = new Vector2(90f, 90f)
                },
                new SettingsSceneVisualStyle
                {
                    SceneName = "LobbyMahjongBattle",
                    ApplyOpenButtonRect = true,
                    OpenButtonAnchorMin = new Vector2(1f, 1f),
                    OpenButtonAnchorMax = new Vector2(1f, 1f),
                    OpenButtonPivot = new Vector2(1f, 1f),
                    OpenButtonPosition = new Vector2(-58f, -42f),
                    OpenButtonSize = new Vector2(90f, 90f)
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
            if (!applyVisualOverrides)
                return;

            AutoResolveVisualTargets();

            SettingsSceneVisualStyle style = ResolveSceneVisualStyle(SceneManager.GetActiveScene().name);
            if (style == null)
                return;

            if (style.ApplyOpenButtonRect)
                ApplyRect(openButtonRect, style.OpenButtonAnchorMin, style.OpenButtonAnchorMax, style.OpenButtonPivot, style.OpenButtonPosition, style.OpenButtonSize);

            if ((style.ApplyOpenButtonGraphic || style.OpenButtonSprite != null) && openButton != null && openButton.image != null)
                ApplyGraphic(openButton.image, style.OpenButtonSprite, style.OpenButtonColor);

            if (style.ApplyPanelRect)
                ApplyRect(panelRootRect, style.PanelAnchorMin, style.PanelAnchorMax, style.PanelPivot, style.PanelPosition, style.PanelSize);

            if (style.ApplyPanelColor && panelBackgroundImage != null)
                panelBackgroundImage.color = style.PanelColor;

            if ((style.ApplyPanelGraphic || style.PanelSprite != null) && panelBackgroundImage != null)
                ApplyGraphic(panelBackgroundImage, style.PanelSprite, style.PanelSpriteColor);

            if (style.ApplyWindowRect)
                ApplyRect(windowRect, style.WindowAnchorMin, style.WindowAnchorMax, style.WindowPivot, style.WindowPosition, style.WindowSize);

            if ((style.ApplyWindowGraphic || style.WindowSprite != null) && windowImage != null)
                ApplyGraphic(windowImage, style.WindowSprite, style.WindowColor);

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
                image.sprite = sprite;

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

            string sceneName = AppSettings.I != null ? AppSettings.I.MainMenuSceneName : "LobbyMahjong";
            LoadSceneWithDoor(sceneName);
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

            if (gameButtonsRoot != null)
                gameButtonsRoot.SetActive(showGameButtons);

            if (returnToMenuButton != null)
                returnToMenuButton.gameObject.SetActive(showGameButtons);

            if (restartButton != null)
                restartButton.gameObject.SetActive(showGameButtons);
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
    }
}
