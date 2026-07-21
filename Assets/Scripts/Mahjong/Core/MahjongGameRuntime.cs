using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongGameRuntime : MonoBehaviour
    {
        private const string StoryEndlessDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const string IntroInfoWindowResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_StageCardThin_810x310";
        private const string IntroTitlePlaqueResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_DifficultyTab_Medium";
        private const string IntroStartButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_LongButton";
        private const string IntroStartLocalizationKey = "mahjong.intro.start";

        public static MahjongGameRuntime I { get; private set; }
        public static bool AssistUiAllowed { get; private set; }

        [Header("Core")]
        [SerializeField] private TileStore store;
        [SerializeField] private Board board;
        [SerializeField] private MahjongMusicPlayer musicPlayer;

        [Header("UI")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private TMP_Text introTitleText;
        [SerializeField] private TMP_Text introDescriptionText;
        [SerializeField] private Button continueButton;

        [Header("Optional Intro Objects")]
        [SerializeField] private GameObject[] extraIntroObjects;

        [Header("Scenes")]
        [SerializeField] private string lobbySceneName = "LobbyMahjong";

        [Header("Flow")]
        [SerializeField] private float continueDelay = 5f;
        [SerializeField] private float introFadeDuration = 0.2f;
        [SerializeField] private string endlessIntroDescription = "Mahjong turns observation into knowledge: compare, remember, and open the hidden path.";

        private Image introTitlePlaqueImage;
        private Image introTextWindowImage;
        private TMP_Text introContinueLabel;
        private TMP_Text introTitleShadowText;
        private Sprite introInfoWindowSprite;
        private Sprite introTitlePlaqueSprite;
        private Sprite introStartButtonSprite;

        private int currentLevel = 1;
        private int currentStageIndex = 0;
        private float timer;
        private bool waitingForContinue;
        private bool transitionRunning;
        private bool resultStateReached;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;

            if (introCanvasGroup == null && introPanel != null)
                introCanvasGroup = introPanel.GetComponent<CanvasGroup>();

            if (board == null)
                board = FindAnyObjectByType<Board>();

            EnsureIntroReadableLayout();
        }

        private void OnEnable()
        {
            BindBoardEvents();
        }

        private void Start()
        {
            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (board == null)
                board = FindAnyObjectByType<Board>();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnClickContinue);
                continueButton.onClick.AddListener(OnClickContinue);
            }

            ResolveLaunch();
        }

        private void OnDisable()
        {
            UnbindBoardEvents();
        }

        private void OnDestroy()
        {
            UnbindBoardEvents();
        }

        private void BindBoardEvents()
        {
            if (board == null)
                board = FindAnyObjectByType<Board>();

            if (board == null)
                return;

            board.WinTriggered -= HandleBoardWin;
            board.LoseTriggered -= HandleBoardLose;

            board.WinTriggered += HandleBoardWin;
            board.LoseTriggered += HandleBoardLose;
        }

        private void UnbindBoardEvents()
        {
            if (board == null)
                return;

            board.WinTriggered -= HandleBoardWin;
            board.LoseTriggered -= HandleBoardLose;
        }

        private void Update()
        {
            if (!waitingForContinue || continueButton == null || continueButton.gameObject.activeSelf)
                return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
                continueButton.gameObject.SetActive(true);
        }

        private void ResolveLaunch()
        {
            if (MahjongSession.LaunchMode == MahjongLaunchMode.Story)
            {
                currentLevel = Mathf.Max(1, MahjongSession.StoryLevel);
                currentStageIndex = Mathf.Max(0, MahjongSession.StoryStage - 1);
            }
            else if (MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
            {
                currentLevel = Mathf.Max(1, MahjongSession.EndlessLevel);
                currentStageIndex = 0;
                MahjongSession.SetEndlessLevel(currentLevel);
            }
            else
            {
                currentLevel = 1;
                currentStageIndex = 0;
                MahjongSession.StartStory(currentLevel, currentStageIndex + 1);
            }

            resultStateReached = false;
            transitionRunning = false;
            waitingForContinue = false;

            if (store != null)
                musicPlayer?.PlayLevelMusic(store.GetMusicForLevel(currentLevel));

            ShowStageIntro();
        }

        private void ShowStageIntro()
        {
            if (MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
            {
                EndlessWisdomEntry wisdom = EndlessWisdomLibrary.GetForEndlessLevel(currentLevel);
                string endlessTitle = string.IsNullOrWhiteSpace(wisdom.Title)
                    ? "Endless Thought"
                    : wisdom.Title;

                string description = string.IsNullOrWhiteSpace(wisdom.Body)
                    ? endlessIntroDescription
                    : wisdom.Body;

                ShowIntro(
                    endlessTitle,
                    description,
                    $"[MahjongGameRuntime] Endless intro shown | Level={currentLevel}");
                return;
            }

            if (store == null || !store.TryGetStageContent(currentLevel, currentStageIndex + 1, out LevelStageContent stage))
            {
                Debug.LogError($"[MahjongGameRuntime] Stage not found | Level={currentLevel} | Stage={currentStageIndex + 1}");
                ReturnToLobby();
                return;
            }

            string title = string.IsNullOrWhiteSpace(stage.Title)
                ? $"{store.GetLevelDisplayName(currentLevel)} - Этап {currentStageIndex + 1}"
                : stage.Title;

            ShowIntro(title, stage.Description, $"[MahjongGameRuntime] Intro shown | Level={currentLevel} | Stage={currentStageIndex + 1}");
        }

        private void ShowIntro(string title, string description, string logMessage)
        {
            waitingForContinue = true;
            transitionRunning = false;
            resultStateReached = false;
            AssistUiAllowed = false;
            MahjongAssistUI.SetVisible(false);

            ShowObject(introPanel, true);
            ShowObject(gamePanel, false);
            SetExtraIntroObjectsVisible(true);
            EnsureIntroReadableLayout();

            if (introCanvasGroup != null)
            {
                introCanvasGroup.alpha = 1f;
                introCanvasGroup.interactable = true;
                introCanvasGroup.blocksRaycasts = true;
            }

            if (introTitleText != null)
            {
                introTitleText.text = title;
                StyleIntroTitle();
            }

            if (introDescriptionText != null)
            {
                introDescriptionText.text = description;
                StyleIntroDescription();
            }

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            timer = continueDelay;

            Debug.Log(logMessage);
        }

        private void EnsureIntroReadableLayout()
        {
            RectTransform panelRect = introPanel != null ? introPanel.transform as RectTransform : null;
            if (panelRect == null)
                return;

            introTextWindowImage = EnsureIntroImage(
                panelRect,
                introTextWindowImage,
                "IntroInfoWindow",
                LoadIntroInfoWindowSprite(),
                new Vector2(0f, -32f),
                new Vector2(2220f, 850f));

            introTitlePlaqueImage = EnsureIntroImage(
                panelRect,
                introTitlePlaqueImage,
                "IntroTitlePlaque",
                LoadIntroTitlePlaqueSprite(),
                new Vector2(0f, 352f),
                new Vector2(760f, 230f));
            if (introTitlePlaqueImage != null)
                introTitlePlaqueImage.gameObject.SetActive(false);

            StyleIntroTitle();
            StyleIntroDescription();
            StyleIntroContinueButton();

            Transform background = panelRect.Find("IntroBackground");
            EnsureIntroMovingBackground(background);

            if (background != null)
                background.SetAsFirstSibling();

            if (introTextWindowImage != null)
                introTextWindowImage.transform.SetSiblingIndex(Mathf.Min(background != null ? 1 : 0, panelRect.childCount - 1));

            if (introTitlePlaqueImage != null)
                introTitlePlaqueImage.transform.SetSiblingIndex(Mathf.Min(background != null ? 2 : 1, panelRect.childCount - 1));

            if (introTitleText != null)
                introTitleText.transform.SetAsLastSibling();

            if (introTitleShadowText != null)
                introTitleShadowText.transform.SetSiblingIndex(Mathf.Max(0, introTitleText != null ? introTitleText.transform.GetSiblingIndex() - 1 : panelRect.childCount - 1));

            if (introDescriptionText != null)
                introDescriptionText.transform.SetAsLastSibling();

            if (continueButton != null)
                continueButton.transform.SetAsLastSibling();
        }

        private void EnsureIntroMovingBackground(Transform background)
        {
            if (background == null)
                return;

            Image image = background.GetComponent<Image>();
            if (image == null)
                return;

            image.raycastTarget = false;

            MahjongIntroMovingBackground movingBackground = background.GetComponent<MahjongIntroMovingBackground>();
            if (movingBackground == null)
                movingBackground = background.gameObject.AddComponent<MahjongIntroMovingBackground>();

            movingBackground.RefreshFromSource();
        }

        private Image EnsureIntroImage(RectTransform parent, Image current, string objectName, Sprite sprite, Vector2 position, Vector2 size)
        {
            if (current == null)
            {
                Transform existing = parent.Find(objectName);
                current = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (current == null && string.Equals(objectName, "IntroInfoWindow", System.StringComparison.Ordinal))
            {
                Transform legacyWindow = parent.Find("Image");
                current = legacyWindow != null ? legacyWindow.GetComponent<Image>() : null;
                if (current != null)
                    current.gameObject.name = objectName;
            }

            if (current == null)
            {
                GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(parent, false);
                panel.layer = parent.gameObject.layer;
                current = panel.GetComponent<Image>();
            }

            RectTransform rect = current.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            current.sprite = sprite;
            current.color = Color.white;
            current.type = Image.Type.Simple;
            current.preserveAspect = !string.Equals(objectName, "IntroInfoWindow", System.StringComparison.Ordinal);
            current.pixelsPerUnitMultiplier = 1f;
            current.raycastTarget = false;
            return current;
        }

        private Sprite LoadIntroInfoWindowSprite()
        {
            if (introInfoWindowSprite == null)
                introInfoWindowSprite = Resources.Load<Sprite>(IntroInfoWindowResourcePath);

            return introInfoWindowSprite;
        }

        private Sprite LoadIntroTitlePlaqueSprite()
        {
            if (introTitlePlaqueSprite == null)
                introTitlePlaqueSprite = Resources.Load<Sprite>(IntroTitlePlaqueResourcePath);

            return introTitlePlaqueSprite;
        }

        private Sprite LoadIntroStartButtonSprite()
        {
            if (introStartButtonSprite == null)
                introStartButtonSprite = Resources.Load<Sprite>(IntroStartButtonResourcePath);

            return introStartButtonSprite;
        }

        private void ApplyCenteredRect(RectTransform current, Vector2 position, Vector2 size)
        {
            current.anchorMin = new Vector2(0.5f, 0.5f);
            current.anchorMax = new Vector2(0.5f, 0.5f);
            current.pivot = new Vector2(0.5f, 0.5f);
            current.anchoredPosition = position;
            current.sizeDelta = size;
            current.localScale = Vector3.one;
        }

        private void StyleIntroTitle()
        {
            if (introTitleText == null)
                return;

            RectTransform rect = introTitleText.rectTransform;
            ApplyCenteredRect(rect, new Vector2(0f, 406f), new Vector2(1320f, 126f));

            ApplyIntroFont(introTitleText);
            introTitleText.color = new Color(1f, 0.8f, 0.18f, 1f);
            introTitleText.enableVertexGradient = true;
            introTitleText.colorGradient = new VertexGradient(
                new Color(1f, 0.98f, 0.58f, 1f),
                new Color(1f, 0.88f, 0.2f, 1f),
                new Color(0.78f, 0.36f, 0.04f, 1f),
                new Color(1f, 0.62f, 0.08f, 1f));
            introTitleText.fontSize = 78f;
            introTitleText.fontSizeMin = 42f;
            introTitleText.fontSizeMax = 92f;
            introTitleText.enableAutoSizing = true;
            introTitleText.fontStyle = FontStyles.Bold;
            introTitleText.alignment = TextAlignmentOptions.Center;
            introTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            introTitleText.overflowMode = TextOverflowModes.Ellipsis;
            introTitleText.outlineWidth = 0.38f;
            introTitleText.outlineColor = new Color(0f, 0.025f, 0.006f, 1f);

            Shadow shadow = introTitleText.GetComponent<Shadow>();
            if (shadow == null)
                shadow = introTitleText.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0.04f, 0.012f, 0.95f);
            shadow.effectDistance = new Vector2(0f, -7f);

            StyleIntroTitleShadow();
        }

        private void StyleIntroTitleShadow()
        {
            if (introTitleText == null)
                return;

            if (introTitleShadowText == null)
            {
                GameObject shadowObject = new GameObject("IntroTitleHeavyShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                shadowObject.transform.SetParent(introTitleText.transform.parent, false);
                shadowObject.layer = introTitleText.gameObject.layer;
                introTitleShadowText = shadowObject.GetComponent<TextMeshProUGUI>();
                introTitleShadowText.raycastTarget = false;
            }

            RectTransform shadowRect = introTitleShadowText.rectTransform;
            ApplyCenteredRect(shadowRect, new Vector2(0f, 398f), new Vector2(1320f, 126f));

            ApplyIntroFont(introTitleShadowText);
            introTitleShadowText.text = introTitleText.text;
            introTitleShadowText.color = new Color(0f, 0.075f, 0.02f, 0.92f);
            introTitleShadowText.fontSize = introTitleText.fontSize;
            introTitleShadowText.fontSizeMin = introTitleText.fontSizeMin;
            introTitleShadowText.fontSizeMax = introTitleText.fontSizeMax;
            introTitleShadowText.enableAutoSizing = introTitleText.enableAutoSizing;
            introTitleShadowText.fontStyle = introTitleText.fontStyle;
            introTitleShadowText.alignment = introTitleText.alignment;
            introTitleShadowText.textWrappingMode = introTitleText.textWrappingMode;
            introTitleShadowText.overflowMode = introTitleText.overflowMode;
            introTitleShadowText.enableVertexGradient = false;
            introTitleShadowText.outlineWidth = 0.42f;
            introTitleShadowText.outlineColor = new Color(0f, 0.02f, 0.005f, 1f);
            introTitleShadowText.gameObject.SetActive(introTitleText.gameObject.activeInHierarchy);
        }

        private void StyleIntroDescription()
        {
            if (introDescriptionText == null)
                return;

            RectTransform rect = introDescriptionText.rectTransform;
            ApplyCenteredRect(rect, new Vector2(0f, -22f), new Vector2(1780f, 460f));

            ApplyIntroFont(introDescriptionText);
            introDescriptionText.color = new Color(0.98f, 0.94f, 0.72f, 1f);
            introDescriptionText.fontSize = 58f;
            introDescriptionText.fontSizeMin = 30f;
            introDescriptionText.fontSizeMax = ResolveIntroDescriptionMaxFontSize(introDescriptionText.text);
            introDescriptionText.enableAutoSizing = true;
            introDescriptionText.fontStyle = FontStyles.Bold;
            introDescriptionText.alignment = TextAlignmentOptions.Center;
            introDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            introDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
            introDescriptionText.margin = new Vector4(54f, 28f, 54f, 28f);
            introDescriptionText.lineSpacing = 8f;
            introDescriptionText.outlineWidth = 0.2f;
            introDescriptionText.outlineColor = new Color(0f, 0.018f, 0.006f, 1f);

            Shadow shadow = introDescriptionText.GetComponent<Shadow>();
            if (shadow == null)
                shadow = introDescriptionText.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        private float ResolveIntroDescriptionMaxFontSize(string text)
        {
            int length = string.IsNullOrWhiteSpace(text) ? 0 : text.Trim().Length;

            if (length <= 90)
                return 66f;

            if (length <= 150)
                return 60f;

            return 54f;
        }

        private void StyleIntroContinueButton()
        {
            if (continueButton == null)
                return;

            RectTransform rect = continueButton.transform as RectTransform;
            if (rect != null)
                ApplyCenteredRect(rect, new Vector2(0f, -474f), new Vector2(430f, 176f));

            Image image = continueButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = LoadIntroStartButtonSprite();
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = true;
            }

            TMP_Text label = EnsureIntroContinueLabel();
            if (label == null)
                return;

            ApplyIntroFont(label);
            label.text = ResolveIntroStartText();
            label.color = new Color(1f, 0.9f, 0.42f, 1f);
            label.fontSize = 50f;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 60f;
            label.enableAutoSizing = true;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.outlineWidth = 0.12f;
            label.outlineColor = new Color(0f, 0.045f, 0.015f, 1f);
        }

        private static void ApplyIntroFont(TMP_Text text)
        {
            LocalizedTextStyle.Apply(text);
        }

        private TMP_Text EnsureIntroContinueLabel()
        {
            if (continueButton == null)
                return null;

            if (introContinueLabel == null)
                introContinueLabel = continueButton.GetComponentInChildren<TMP_Text>(true);

            if (introContinueLabel == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizedText));
                labelObject.transform.SetParent(continueButton.transform, false);
                introContinueLabel = labelObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform rect = introContinueLabel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(52f, 36f);
            rect.offsetMax = new Vector2(-52f, -38f);
            rect.localScale = Vector3.one;

            LocalizedText localized = introContinueLabel.GetComponent<LocalizedText>();
            if (localized != null)
                localized.enabled = false;

            return introContinueLabel;
        }

        private static string ResolveIntroStartText()
        {
            return AppSettings.I != null ? GameLocalization.Text(IntroStartLocalizationKey) : "Başla";
        }

        private void OnClickContinue()
        {
            if (!waitingForContinue || transitionRunning || resultStateReached)
                return;

            if (continueButton != null && !continueButton.gameObject.activeSelf)
                return;

            if (DoorFx.I != null && DoorFx.I.IsReady())
            {
                transitionRunning = true;
                DoorFx.I.RunBetweenLevels(() =>
                {
                    StartCoroutine(StartGameplaySmoothRoutine());
                }, StoryEndlessDoorSpriteResourcePath, false);
            }
            else
            {
                StartCoroutine(StartGameplaySmoothRoutine());
            }
        }

        private IEnumerator StartGameplaySmoothRoutine()
        {
            transitionRunning = true;
            waitingForContinue = false;
            resultStateReached = false;

            if (board == null)
                board = FindAnyObjectByType<Board>();

            ShowObject(gamePanel, true);

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (board != null)
            {
                if (MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
                    board.SetEndlessLevel(currentLevel);
                else
                    board.SetStoryStage(currentLevel, currentStageIndex);

                board.Build();
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            if (introCanvasGroup != null)
            {
                float time = 0f;
                float duration = Mathf.Max(0.01f, introFadeDuration);

                introCanvasGroup.interactable = false;
                introCanvasGroup.blocksRaycasts = false;

                while (time < duration)
                {
                    time += Time.deltaTime;
                    introCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
                    yield return null;
                }

                introCanvasGroup.alpha = 0f;
            }

            ShowObject(introPanel, false);
            SetExtraIntroObjectsVisible(false);
            AssistUiAllowed = true;
            MahjongAssistUI.SetVisible(true);

            Canvas.ForceUpdateCanvases();

            transitionRunning = false;

            Debug.Log($"[MahjongGameRuntime] Gameplay started | Level={currentLevel} | Stage={currentStageIndex + 1}");
        }

        private void HandleBoardWin()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;
            AssistUiAllowed = false;
            MahjongAssistUI.SetVisible(false);

            Debug.Log("[MahjongGameRuntime] Board WIN received.");
        }

        private void HandleBoardLose()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;
            AssistUiAllowed = false;
            MahjongAssistUI.SetVisible(false);

            Debug.Log("[MahjongGameRuntime] Board LOSE received.");
        }

        public void OnBoardStageComplete()
        {
            if (resultStateReached)
                return;

            resultStateReached = true;
            waitingForContinue = false;
            transitionRunning = false;

            Debug.Log("[MahjongGameRuntime] OnBoardStageComplete called.");
        }

        public void ReturnToLobbyFromRuntime()
        {
            ReturnToLobby();
        }

        private void ReturnToLobby()
        {
            MahjongSession.Clear();
            LoadSceneWithDoor(lobbySceneName);
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[MahjongGameRuntime] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName, StoryEndlessDoorSpriteResourcePath, false);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void ShowObject(GameObject obj, bool value)
        {
            if (obj == null)
                return;

            obj.SetActive(value);

            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = value ? 1f : 0f;
                group.interactable = value;
                group.blocksRaycasts = value;
            }
        }

        private void SetExtraIntroObjectsVisible(bool value)
        {
            if (extraIntroObjects == null)
                return;

            for (int i = 0; i < extraIntroObjects.Length; i++)
            {
                GameObject obj = extraIntroObjects[i];
                if (obj == null)
                    continue;

                obj.SetActive(value);
            }
        }
    }
}
