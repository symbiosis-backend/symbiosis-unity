using System.Collections;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileBootstrap : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject languageSelectionPanel;
        [SerializeField] private GameObject profileSetupPanel;
        [SerializeField] private CanvasGroup fadeGroup;

        [Header("Language Buttons")]
        [SerializeField] private Button russianLanguageButton;
        [SerializeField] private Button englishLanguageButton;
        [SerializeField] private Button turkishLanguageButton;

        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "Main";

        [Header("Transition")]
        [SerializeField, Min(0f)] private float startDelay = 0.15f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
        [SerializeField, Min(0)] private int inputSettleFramesBeforeSceneLoad = 3;
        [SerializeField] private bool unloadUnusedAssetsBeforeLobby = true;

        [Header("Entry Intro")]
        [SerializeField] private bool playEntryIntro = true;
        [SerializeField] private bool playEntryIntroOnMobile = true;
        [SerializeField] private EntryCinematicIntro entryIntro;

        [Header("Generated UI")]
        [SerializeField] private bool autoCreateLanguagePanel = true;
        [SerializeField] private bool autoCreateProfileService = true;
        [SerializeField] private bool requireServerProfile = true;

        private bool started;
        private bool loadingNextScene;
        private bool resolvingServerProfile;

        private void Awake()
        {
            LogRuntime("ProfileBootstrap Awake begin");
            EnsureLanguagePanel();
            LogRuntime("ProfileBootstrap language panel ensured");
            BindLanguageButtons();
            LogRuntime("ProfileBootstrap Awake done");
        }

        private void OnDestroy()
        {
            ClearEditorSelection();
            UnbindLanguageButtons();
        }

        private void Start()
        {
            LogRuntime("ProfileBootstrap Start");
            if (started)
                return;

            started = true;
            StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            LogRuntime("BootRoutine begin. ActiveScene=" + SceneManager.GetActiveScene().name);
            SetLoadingVisible(false);
            SetLanguageSelectionVisible(false);
            SetProfileSetupVisible(false);
            SetFade(0f);

            yield return PlayEntryIntroIfNeeded();
            ReleaseEntryIntroResources();
            LogRuntime("Entry intro finished");

            SetLoadingVisible(true);

            yield return null;
            yield return new WaitForSeconds(startDelay);

            yield return EnsureProfileServiceReady();
            LogRuntime("Profile service ready=" + (ProfileService.I != null));

            if (ProfileService.I == null)
            {
                Debug.LogError("[ProfileBootstrap] ProfileService not found.");
                SetLoadingVisible(false);
                SetProfileSetupVisible(true);
                yield break;
            }

            SetLoadingVisible(false);

            if (ShouldShowLanguageSelection())
            {
                LogRuntime("Showing language selection");
                SetLanguageSelectionVisible(true);
                SetProfileSetupVisible(false);
                yield break;
            }

            LogRuntime("Language already selected, continuing");
            ContinueAfterLanguageSelection();
        }

        public void ContinueAfterLanguageSelection()
        {
            if (loadingNextScene || resolvingServerProfile)
                return;

            StartCoroutine(ContinueAfterLanguageSelectionRoutine());
        }

        private IEnumerator ContinueAfterLanguageSelectionRoutine()
        {
            resolvingServerProfile = true;
            SetLanguageSelectionVisible(false);

            if (ProfileService.I == null)
            {
                Debug.LogError("[ProfileBootstrap] ProfileService not found while continuing after language selection.");
                SetProfileSetupVisible(true);
                resolvingServerProfile = false;
                yield break;
            }

            SetLoadingVisible(true);

            if (ProfileService.I.HasRememberedAccount)
            {
                LogRuntime("Remembered account found; showing slot picker");
                SetLoadingVisible(false);
                SetProfileSetupVisible(true);
                resolvingServerProfile = false;
                yield break;
            }

            if (!ProfileService.I.CanAutoLoadProfile)
            {
                LogRuntime("No remembered profile; showing registration/login setup");
                SetLoadingVisible(false);
                SetProfileSetupVisible(true);
                resolvingServerProfile = false;
                yield break;
            }

            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            yield return ProfileService.I.LoadOrCreateServerProfile(language);

            SetLoadingVisible(false);

            if (ProfileService.I.Current == null && ProfileService.I.HasProfile() && !requireServerProfile)
            {
                LogRuntime("Loading cached profile after server profile fallback");
                ProfileService.I.LoadProfile();
            }

            if (ProfileService.I.Current == null)
            {
                Debug.LogWarning("[ProfileBootstrap] Server profile was not loaded; showing registration/login setup. " + ProfileService.I.LastServerError);
                SetProfileSetupVisible(true);
                resolvingServerProfile = false;
                yield break;
            }

            PlayerProfile profile = ProfileService.I.Current;
            bool shouldShowSetup = profile == null || !profile.IsProfileCompleted;
            LogRuntime("ContinueAfterLanguageSelection. ShowSetup=" + shouldShowSetup);

            SetProfileSetupVisible(shouldShowSetup);

            if (!shouldShowSetup)
                StartCoroutine(LoadLobbyRoutine());

            resolvingServerProfile = false;
        }

        public void ContinueAfterProfileSetup()
        {
            if (loadingNextScene)
                return;

            LogRuntime("ContinueAfterProfileSetup");
            StartCoroutine(LoadLobbyRoutine());
        }

        public void SelectRussianLanguage()
        {
            SelectLanguage(GameLanguage.Russian);
        }

        public void SelectEnglishLanguage()
        {
            SelectLanguage(GameLanguage.English);
        }

        public void SelectTurkishLanguage()
        {
            SelectLanguage(GameLanguage.Turkish);
        }

        private void SelectLanguage(GameLanguage language)
        {
            if (AppSettings.I != null)
                AppSettings.I.SetLanguage(language);
            else
                Debug.LogWarning("[ProfileBootstrap] AppSettings not found. Language will use saved/default value.");

            ContinueAfterLanguageSelection();
        }

        private IEnumerator LoadLobbyRoutine()
        {
            loadingNextScene = true;

            LogRuntime("LoadLobbyRoutine begin. Target=" + lobbySceneName);
            ClearEditorSelection();
            ReleaseInputFocus();
            SetLanguageSelectionVisible(false);
            SetProfileSetupVisible(false);
            SetLoadingVisible(true);

            for (int i = 0; i < inputSettleFramesBeforeSceneLoad; i++)
            {
                ReleaseInputFocus();
                yield return null;
            }

            if (fadeGroup != null)
            {
                float time = 0f;
                while (time < fadeDuration)
                {
                    time += Time.deltaTime;
                    float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(time / fadeDuration);
                    fadeGroup.alpha = t;
                    yield return null;
                }

                fadeGroup.alpha = 1f;
            }

            ClearEditorSelection();

            yield return null;

            if (unloadUnusedAssetsBeforeLobby && !Application.isMobilePlatform)
            {
                LogRuntime("UnloadUnusedAssets before lobby");
                yield return Resources.UnloadUnusedAssets();
            }

            if (!IsSceneInBuild(lobbySceneName))
            {
                LogRuntime("Scene not found in build settings: " + lobbySceneName);
                Debug.LogError("[ProfileBootstrap] Scene not found in build settings: " + lobbySceneName);
                SetLoadingVisible(false);
                loadingNextScene = false;
                yield break;
            }

            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(lobbySceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                LogRuntime("LoadSceneAsync exception: " + ex);
                Debug.LogError("[ProfileBootstrap] LoadSceneAsync failed: " + ex);
            }

            if (operation == null)
            {
                LogRuntime("LoadSceneAsync returned null for scene " + lobbySceneName);
                SetLoadingVisible(false);
                loadingNextScene = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            GlobalChatBootstrap.EnsureForCurrentScene();
            FriendsBootstrap.EnsureForCurrentScene();
            LogRuntime("LoadLobbyRoutine done. Target=" + lobbySceneName);
        }

        private IEnumerator PlayEntryIntroIfNeeded()
        {
            if (!playEntryIntro)
                yield break;

            if (Application.isMobilePlatform && !playEntryIntroOnMobile)
            {
                LogRuntime("Entry intro skipped on mobile");
                yield break;
            }

            if (entryIntro == null)
                entryIntro = FindAnyObjectByType<EntryCinematicIntro>(FindObjectsInactive.Include);

            if (entryIntro == null)
                entryIntro = gameObject.AddComponent<EntryCinematicIntro>();

            yield return entryIntro.Play();
        }

        private void ReleaseEntryIntroResources()
        {
            if (entryIntro != null)
                entryIntro.ReleaseHeavyReferences();
        }

        private bool ShouldShowLanguageSelection()
        {
            return AppSettings.I == null || !AppSettings.I.HasLanguagePreference;
        }

        private IEnumerator EnsureProfileServiceReady()
        {
            const int maxWaitFrames = 12;

            for (int i = 0; i < maxWaitFrames && ProfileService.I == null; i++)
            {
                ProfileService existing = FindAnyObjectByType<ProfileService>(FindObjectsInactive.Include);
                if (existing != null)
                {
                    if (!existing.gameObject.activeSelf)
                        existing.gameObject.SetActive(true);

                    yield break;
                }

                yield return null;
            }

            if (ProfileService.I != null || !autoCreateProfileService)
                yield break;

            GameObject serviceObject = new GameObject("ProfileService");
            serviceObject.AddComponent<ProfileService>();

            yield return null;
        }

        private void BindLanguageButtons()
        {
            if (russianLanguageButton != null)
                russianLanguageButton.onClick.AddListener(SelectRussianLanguage);

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.AddListener(SelectEnglishLanguage);

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.AddListener(SelectTurkishLanguage);
        }

        private void UnbindLanguageButtons()
        {
            if (russianLanguageButton != null)
                russianLanguageButton.onClick.RemoveListener(SelectRussianLanguage);

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.RemoveListener(SelectEnglishLanguage);

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.RemoveListener(SelectTurkishLanguage);
        }

        private void EnsureLanguagePanel()
        {
            if (languageSelectionPanel != null || !autoCreateLanguagePanel)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            GameObject overlay = new GameObject("LanguageSelectionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic));
            overlay.transform.SetParent(canvas.transform, false);
            overlay.layer = canvas.gameObject.layer;
            overlay.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            SolidRuntimeGraphic overlayGraphic = overlay.GetComponent<SolidRuntimeGraphic>();
            overlayGraphic.color = new Color(0.035f, 0.05f, 0.075f, 0.96f);

            GameObject window = new GameObject("LanguageWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            window.transform.SetParent(overlay.transform, false);
            window.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(620f, 460f);

            SolidRuntimeGraphic windowGraphic = window.GetComponent<SolidRuntimeGraphic>();
            windowGraphic.color = new Color(0.09f, 0.12f, 0.17f, 0.98f);

            VerticalLayoutGroup layout = window.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 42, 38, 38);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = window.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(window.transform, "Title", GameLocalization.Text("language.title"), 42f, FontStyles.Bold, Color.white, 72f);
            CreateText(window.transform, "Subtitle", GameLocalization.Text("language.subtitle"), 22f, FontStyles.Normal, new Color(0.78f, 0.84f, 0.94f, 1f), 58f);

            russianLanguageButton = CreateLanguageButton(window.transform, "RU", "RU  Русский");
            englishLanguageButton = CreateLanguageButton(window.transform, "EN", "EN  English");
            turkishLanguageButton = CreateLanguageButton(window.transform, "TR", "TR  Turkce");

            languageSelectionPanel = overlay;
            languageSelectionPanel.SetActive(false);
        }

        private Button CreateLanguageButton(Transform parent, string code, string label)
        {
            GameObject buttonObject = new GameObject("ButtonLanguage" + code, typeof(RectTransform), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 68f);

            SolidRuntimeGraphic graphic = buttonObject.GetComponent<SolidRuntimeGraphic>();
            graphic.color = new Color(0.14f, 0.18f, 0.24f, 1f);

            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = 68f;
            element.minHeight = 58f;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = graphic;

            CreateText(buttonObject.transform, "Label", label, 28f, FontStyles.Bold, Color.white, 68f);
            return button;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, FontStyles style, Color color, float preferredHeight)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            textObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, preferredHeight);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;

            LayoutElement element = textObject.GetComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;

            return label;
        }

        private static void ClearEditorSelection()
        {
#if UNITY_EDITOR
            if (Selection.activeObject != null)
                Selection.activeObject = null;
#endif
        }

        public static void LogRuntime(string message)
        {
            RuntimeFileLogger.Write("[ProfileFlow] " + message);

            try
            {
                string path = Path.Combine(Application.persistentDataPath, "profile_flow.log");
                string line = DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine;
                File.AppendAllText(path, line);
            }
            catch
            {
            }
        }

        private static bool IsSceneInBuild(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0)
                return true;

            return SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") >= 0;
        }

        private static void ReleaseInputFocus()
        {
            TMPro.TMP_InputField[] inputs = FindObjectsByType<TMPro.TMP_InputField>(FindObjectsInactive.Include);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null)
                    inputs[i].DeactivateInputField();
            }

            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        private void SetLoadingVisible(bool value)
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(value);
        }

        private void SetLanguageSelectionVisible(bool value)
        {
            if (languageSelectionPanel != null)
                languageSelectionPanel.SetActive(value);
        }

        private void SetProfileSetupVisible(bool value)
        {
            if (profileSetupPanel != null)
                profileSetupPanel.SetActive(value);
        }

        private void SetFade(float alpha)
        {
            if (fadeGroup == null)
                return;

            fadeGroup.alpha = alpha;
            fadeGroup.blocksRaycasts = alpha > 0.001f;
            fadeGroup.interactable = false;
        }
    }
}
