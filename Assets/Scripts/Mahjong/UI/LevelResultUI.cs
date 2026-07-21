using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MahjongGame.Monetization;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class LevelResultUI : MonoBehaviour
    {
        private const string StoryEndlessDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const string ResultWindowResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_FullscreenPanel";
        private const string ResultContentCardResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_WidePanel";
        private const string ResultHeaderBarResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_TopStatusBar";
        private const string ResultDividerResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_DecorativeDivider";
        private const string ResultButtonResourcePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_LongButton";

        [Header("Links")]
        [SerializeField] private TileStore tileStore;
        [SerializeField] private Board board;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image darkOverlay;
        [SerializeField] private Image resultImage;

        [Header("Gameplay UI")]
        [SerializeField] private GameObject gameplayUiRoot;

        [Header("Score View")]
        [SerializeField] private TMP_Text scoreText;

        [Header("Reward View")]
        [SerializeField] private TMP_Text rewardText;

        [Header("Buttons")]
        [SerializeField] private Button btnMenu;
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnRetry;

        [Header("Sprites")]
        [SerializeField] private Sprite winSprite;
        [SerializeField] private Sprite loseSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;

        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "LobbyMahjong";
        [SerializeField] private string gameSceneName = "GameMahjong";

        [Header("Modern Result Style")]
        [SerializeField] private bool useModernResultStyle = true;
        [SerializeField] private Vector2 panelSize = new Vector2(1520f, 760f);
        [SerializeField] private Vector2 primaryButtonSize = new Vector2(360f, 92f);
        [SerializeField] private Vector2 secondaryButtonSize = new Vector2(340f, 92f);

        private AudioSource audioSource;
        private CanvasGroup panelCanvasGroup;
        private bool shown;
        private bool matchEndAdPending;
        private bool matchEndAdInProgress;
        private string pendingMatchEndAdSource;
        private RectTransform modernRoot;
        private Image modernPanel;
        private Image modernContentCard;
        private Image modernHeaderBar;
        private Image modernDivider;
        private TMP_Text modernTitleText;
        private TMP_Text modernScoreText;
        private TMP_Text modernRewardText;
        private Button modernMenuButton;
        private Button modernNextButton;
        private Button modernRetryButton;
        private Button modernRescueButton;
        private Button modernFurnaceButton;
        private RectTransform furnaceRoot;
        private Image furnacePanel;
        private TMP_Text furnaceTitleText;
        private TMP_Text furnaceBodyText;
        private Button furnaceCloseButton;
        private Sprite cachedResultWindowSprite;
        private Sprite cachedResultContentCardSprite;
        private Sprite cachedResultHeaderBarSprite;
        private Sprite cachedResultDividerSprite;
        private Sprite cachedResultButtonSprite;
        private float modernPanelWidth = 1520f;
        private float modernPanelHeight = 760f;
        private int pendingFurnaceFeedAmount;
        private bool furnaceFeedClaimed;
        private bool rescueAdInProgress;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color PanelColor = Color.white;
        private static readonly Color PanelOutlineColor = new Color(0.96f, 0.78f, 0.26f, 0.92f);
        private static readonly Color TextPrimaryColor = new Color(1f, 0.93f, 0.58f, 1f);
        private static readonly Color TextAccentColor = new Color(1f, 0.88f, 0.34f, 1f);
        private static readonly Color ButtonNormalColor = new Color(0.04f, 0.17f, 0.11f, 0.96f);
        private static readonly Color ButtonHighlightedColor = new Color(0.08f, 0.27f, 0.17f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.02f, 0.10f, 0.07f, 1f);
        private static readonly Color ButtonTextColor = new Color(1f, 0.91f, 0.5f, 1f);
        private static readonly VertexGradient WinTitleGradient = new VertexGradient(
            new Color32(255, 255, 190, 255),
            new Color32(255, 226, 82, 255),
            new Color32(40, 210, 78, 255),
            new Color32(255, 178, 34, 255));
        private static readonly VertexGradient LoseTitleGradient = new VertexGradient(
            new Color32(255, 238, 164, 255),
            new Color32(255, 166, 52, 255),
            new Color32(196, 42, 18, 255),
            new Color32(255, 114, 38, 255));
        private static readonly VertexGradient GoldTextGradient = new VertexGradient(
            new Color32(255, 255, 202, 255),
            new Color32(255, 227, 95, 255),
            new Color32(255, 164, 31, 255),
            new Color32(255, 213, 74, 255));

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            AutoResolveReferences();
            SetupButtons();
            HideImmediate();
        }

        private void AutoResolveReferences()
        {
            if (tileStore == null)
                tileStore = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (board == null)
                board = FindAnyObjectByType<Board>();

            if (panelRoot == null)
            {
                GameObject found = GameObject.Find("LevelResultPanel");
                if (found != null)
                    panelRoot = found;
            }

            if (gameplayUiRoot == null)
            {
                GameObject found = GameObject.Find("GameplayUIRoot");
                if (found != null)
                    gameplayUiRoot = found;
            }

            if (panelRoot != null && panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                    panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            if (darkOverlay == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("DarkOverlay");
                if (t != null)
                    darkOverlay = t.GetComponent<Image>();
            }

            if (resultImage == null && panelRoot != null)
            {
                Transform bg = panelRoot.transform.Find("BG");
                if (bg != null)
                    resultImage = bg.GetComponent<Image>();
            }

            if (scoreText == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("ScoreText");
                if (t != null)
                    scoreText = t.GetComponent<TMP_Text>();
            }

            if (rewardText == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("RewardText");
                if (t != null)
                    rewardText = t.GetComponent<TMP_Text>();
            }

            if (btnMenu == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("BtnMenu");
                if (t != null)
                    btnMenu = t.GetComponent<Button>();
            }

            if (btnNext == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("BtnNext");
                if (t != null)
                    btnNext = t.GetComponent<Button>();
            }

            if (btnRetry == null && panelRoot != null)
            {
                Transform t = panelRoot.transform.Find("Retry");
                if (t != null)
                    btnRetry = t.GetComponent<Button>();
            }

            if (useModernResultStyle)
                EnsureModernResultRoot();
        }

        private void SetupButtons()
        {
            if (btnMenu != null)
            {
                btnMenu.onClick.RemoveListener(OnClickMenu);
                btnMenu.onClick.AddListener(OnClickMenu);
            }

            if (btnNext != null)
            {
                btnNext.onClick.RemoveListener(OnClickNext);
                btnNext.onClick.AddListener(OnClickNext);
            }

            if (btnRetry != null)
            {
                btnRetry.onClick.RemoveListener(OnClickRetry);
                btnRetry.onClick.AddListener(OnClickRetry);
            }
        }

        public void ShowWin()
        {
            if (shown)
                return;

            shown = true;
            Show(true);
        }

        public void ShowLose()
        {
            if (shown)
                return;

            shown = true;
            Show(false);
        }

        public void ResetState()
        {
            HideImmediate();
        }

        private void Show(bool isWin)
        {
            AutoResolveReferences();

            if (panelRoot == null)
            {
                Debug.LogError("[LevelResultUI] panelRoot is NULL");
                return;
            }

            if (gameplayUiRoot != null)
                gameplayUiRoot.SetActive(false);

            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();

            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.transform.SetAsLastSibling();

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }

            if (darkOverlay != null)
                darkOverlay.gameObject.SetActive(true);

            if (scoreText != null)
            {
                if (isWin && ScoreSystem.I != null)
                {
                    scoreText.gameObject.SetActive(true);
                    scoreText.text = GameLocalization.Format("mahjong.score", ScoreSystem.I.CurrentLevelScore);
                }
                else
                {
                    scoreText.gameObject.SetActive(false);
                }
            }

            if (rewardText != null)
            {
                if (isWin && MahjongMatchService.I != null && MahjongMatchService.I.LastProcessedResult != null)
                {
                    rewardText.gameObject.SetActive(true);
                    rewardText.text = GameLocalization.Format(
                        "mahjong.reward",
                        MahjongMatchService.I.LastProcessedResult.GrantedOzTile,
                        RewardCurrencyLabel());
                }
                else
                {
                    rewardText.gameObject.SetActive(false);
                }
            }

            if (btnMenu != null)
                btnMenu.gameObject.SetActive(true);

            bool hasNext = false;
            if (isWin && MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
            {
                hasNext = true;
            }
            else if (isWin && tileStore != null && MahjongSession.LaunchMode == MahjongLaunchMode.Story)
            {
                int currentLevel = MahjongSession.StoryLevel;
                int currentStage = MahjongSession.StoryStage;
                int stageCount = tileStore.GetStageCount(currentLevel);

                bool hasNextStage = stageCount > 0 && currentStage < stageCount;
                bool hasNextLevel = tileStore.HasNextLevel(currentLevel);

                hasNext = hasNextStage || hasNextLevel;
            }

            if (btnNext != null)
                btnNext.gameObject.SetActive(isWin && hasNext);

            if (btnRetry != null)
                btnRetry.gameObject.SetActive(!isWin);

            if (useModernResultStyle)
                ApplyModernResultStyle(isWin, hasNext);
            else if (resultImage != null)
            {
                Sprite spriteToUse = isWin ? winSprite : loseSprite;
                if (spriteToUse != null)
                    resultImage.sprite = spriteToUse;
            }

            PlaySound(isWin ? winClip : loseClip);
            StartMatchEndAdFlow(isWin);

            Debug.Log($"[LevelResultUI] Show | isWin={isWin} | mode={MahjongSession.LaunchMode} | level={MahjongSession.StoryLevel} | endlessLevel={MahjongSession.EndlessLevel} | stage={MahjongSession.StoryStage} | hasNext={hasNext}");
        }

        private void HideImmediate()
        {
            shown = false;
            matchEndAdPending = false;
            matchEndAdInProgress = false;
            pendingMatchEndAdSource = null;
            rescueAdInProgress = false;

            if (gameplayUiRoot != null)
                gameplayUiRoot.SetActive(true);

            if (darkOverlay != null)
                darkOverlay.gameObject.SetActive(false);

            if (scoreText != null)
                scoreText.gameObject.SetActive(false);

            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            if (btnMenu != null)
                btnMenu.gameObject.SetActive(false);

            if (btnNext != null)
                btnNext.gameObject.SetActive(false);

            if (btnRetry != null)
                btnRetry.gameObject.SetActive(false);

            if (modernRoot != null)
                modernRoot.gameObject.SetActive(false);

            if (furnaceRoot != null)
                furnaceRoot.gameObject.SetActive(false);

            pendingFurnaceFeedAmount = 0;
            furnaceFeedClaimed = false;
        }

        private void StartMatchEndAdFlow(bool isWin)
        {
            pendingMatchEndAdSource = ResolveMatchEndAdSource(isWin);
            matchEndAdPending = true;
            matchEndAdInProgress = false;
            SetResultNavigationInteractable(true);
        }

        private void SetResultNavigationInteractable(bool interactable)
        {
            if (btnMenu != null)
                btnMenu.interactable = interactable;

            if (btnNext != null)
                btnNext.interactable = interactable;

            if (btnRetry != null)
                btnRetry.interactable = interactable;

            if (modernMenuButton != null)
                modernMenuButton.interactable = interactable;

            if (modernNextButton != null)
                modernNextButton.interactable = interactable;

            if (modernRetryButton != null)
                modernRetryButton.interactable = interactable;

            if (modernRescueButton != null)
                modernRescueButton.interactable = interactable && !rescueAdInProgress && CanShowRescueButton();

            if (modernFurnaceButton != null)
            {
                modernFurnaceButton.interactable = false;
                modernFurnaceButton.gameObject.SetActive(false);
            }
        }

        private static string ResolveMatchEndAdSource(bool isWin)
        {
            string mode = MahjongSession.LaunchMode switch
            {
                MahjongLaunchMode.Battle => "battle",
                MahjongLaunchMode.Endless => "endless",
                _ => "story"
            };

            return isWin ? $"mahjong_{mode}_win" : $"mahjong_{mode}_lose";
        }

        private void OnClickMenu()
        {
            RunAfterMatchEndAd(() => LoadSceneWithDoor(lobbySceneName));
        }

        private void OnClickRetry()
        {
            RunAfterMatchEndAd(() => LoadSceneWithDoor(gameSceneName));
        }

        private void OnClickNext()
        {
            RunAfterMatchEndAd(GoToNextAfterResult);
        }

        private void GoToNextAfterResult()
        {
            AutoResolveReferences();

            if (MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
            {
                MahjongSession.StartEndless(MahjongSession.EndlessLevel + 1);
                LoadSceneWithDoor(gameSceneName);
                return;
            }

            if (tileStore == null || MahjongSession.LaunchMode != MahjongLaunchMode.Story)
            {
                LoadSceneWithDoor(lobbySceneName);
                return;
            }

            int currentLevel = MahjongSession.StoryLevel;
            int currentStage = MahjongSession.StoryStage;
            int stageCount = tileStore.GetStageCount(currentLevel);

            if (stageCount > 0 && currentStage < stageCount)
            {
                MahjongSession.StartStory(currentLevel, currentStage + 1);
                LoadSceneWithDoor(gameSceneName);
                return;
            }

            int nextLevel = tileStore.GetNextLevelNumber(currentLevel);
            if (nextLevel > 0)
            {
                MahjongSession.StartStory(nextLevel, 1);
                LoadSceneWithDoor(gameSceneName);
                return;
            }

            LoadSceneWithDoor(lobbySceneName);
        }

        private void RunAfterMatchEndAd(Action action)
        {
            if (matchEndAdInProgress)
                return;

            if (!matchEndAdPending)
            {
                action?.Invoke();
                return;
            }

            matchEndAdPending = false;
            matchEndAdInProgress = true;
            SetResultNavigationInteractable(false);

            bool continued = false;
            void ContinueAfterAd()
            {
                if (continued)
                    return;

                continued = true;
                matchEndAdInProgress = false;
                SetResultNavigationInteractable(true);
                action?.Invoke();
            }

            bool started = MatchEndAdService.TryShowAfterMatchResult(pendingMatchEndAdSource, _ => ContinueAfterAd());
            if (!started)
                ContinueAfterAd();
        }

        private void ApplyModernResultStyle(bool isWin, bool hasNext)
        {
            string scoreValue = scoreText != null && scoreText.gameObject.activeSelf ? scoreText.text : string.Empty;
            string rewardValue = rewardText != null && rewardText.gameObject.activeSelf ? rewardText.text : string.Empty;

            HideLegacyResultVisuals();
            EnsureModernResultRoot();

            if (darkOverlay != null)
            {
                darkOverlay.color = OverlayColor;
                darkOverlay.raycastTarget = true;
                darkOverlay.transform.SetAsFirstSibling();
            }

            if (modernRoot != null)
            {
                modernRoot.gameObject.SetActive(true);
                modernRoot.SetAsLastSibling();
            }

            LayoutModernPanel();
            SetModernTitle(isWin ? "KAZANDIN!" : "TEKRAR DENE", isWin);
            SetModernText(modernScoreText, scoreValue, new Vector2(0f, 84f), TextPrimaryColor, 40f, 110f);
            SetModernText(modernRewardText, rewardValue, new Vector2(0f, -46f), TextPrimaryColor, 32f, 126f);

            pendingFurnaceFeedAmount = 0;
            furnaceFeedClaimed = true;
            bool hasSecondAction = (isWin && hasNext) || !isWin;
            bool showFurnace = false;
            bool showRescue = !isWin && CanShowRescueButton();
            float actionButtonY = -modernPanelHeight * 0.5f + 116f;
            float buttonSpacing = Mathf.Min(440f, modernPanelWidth * 0.23f);
            Vector2 menuPosition = showFurnace
                ? hasSecondAction ? new Vector2(-buttonSpacing, actionButtonY) : new Vector2(-buttonSpacing * 0.5f, actionButtonY)
                : showRescue ? new Vector2(-buttonSpacing, actionButtonY)
                : hasSecondAction ? new Vector2(-buttonSpacing * 0.62f, actionButtonY) : new Vector2(0f, actionButtonY);
            StyleResultButton(modernMenuButton, "LOBBY", menuPosition, secondaryButtonSize);

            if (modernFurnaceButton != null)
            {
                modernFurnaceButton.gameObject.SetActive(showFurnace);
                if (showFurnace)
                {
                    Vector2 furnacePosition = hasSecondAction ? new Vector2(0f, actionButtonY) : new Vector2(buttonSpacing * 0.5f, actionButtonY);
                    StyleResultButton(modernFurnaceButton, "ЖЕРЛО +" + pendingFurnaceFeedAmount, furnacePosition, new Vector2(360f, secondaryButtonSize.y));
                    modernFurnaceButton.interactable = !furnaceFeedClaimed;
                }
            }

            if (isWin && hasNext && modernNextButton != null)
            {
                modernNextButton.gameObject.SetActive(true);
                StyleResultButton(modernNextButton, MahjongSession.LaunchMode == MahjongLaunchMode.Endless ? "NEXT LEVEL" : "NEXT", showFurnace ? new Vector2(buttonSpacing, actionButtonY) : new Vector2(buttonSpacing * 0.62f, actionButtonY), primaryButtonSize);
            }
            else if (modernNextButton != null)
            {
                modernNextButton.gameObject.SetActive(false);
            }

            if (!isWin && modernRetryButton != null)
            {
                modernRetryButton.gameObject.SetActive(true);
                StyleResultButton(modernRetryButton, "RETRY", showRescue ? new Vector2(buttonSpacing, actionButtonY) : showFurnace ? new Vector2(buttonSpacing, actionButtonY) : new Vector2(buttonSpacing * 0.62f, actionButtonY), primaryButtonSize);
            }
            else if (modernRetryButton != null)
            {
                modernRetryButton.gameObject.SetActive(false);
            }

            if (modernRescueButton != null)
            {
                modernRescueButton.gameObject.SetActive(showRescue);
                if (showRescue)
                    StyleResultButton(modernRescueButton, GameLocalization.Text("mahjong.result.rescue"), new Vector2(0f, actionButtonY), new Vector2(400f, secondaryButtonSize.y));
            }
        }

        private void HideLegacyResultVisuals()
        {
            if (resultImage != null)
                resultImage.gameObject.SetActive(false);

            if (scoreText != null)
                scoreText.gameObject.SetActive(false);

            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            if (btnMenu != null)
                btnMenu.gameObject.SetActive(false);

            if (btnNext != null)
                btnNext.gameObject.SetActive(false);

            if (btnRetry != null)
                btnRetry.gameObject.SetActive(false);
        }

        private void EnsureModernResultRoot()
        {
            if (panelRoot == null)
                return;

            if (modernRoot == null)
            {
                Transform found = panelRoot.transform.Find("ModernResultRoot");
                if (found != null)
                    modernRoot = found as RectTransform;
            }

            if (modernRoot == null)
            {
                GameObject rootObject = new GameObject("ModernResultRoot", typeof(RectTransform));
                rootObject.transform.SetParent(panelRoot.transform, false);
                rootObject.layer = panelRoot.layer;
                modernRoot = rootObject.GetComponent<RectTransform>();
            }

            modernRoot.anchorMin = Vector2.zero;
            modernRoot.anchorMax = Vector2.one;
            modernRoot.pivot = new Vector2(0.5f, 0.5f);
            modernRoot.offsetMin = Vector2.zero;
            modernRoot.offsetMax = Vector2.zero;
            modernRoot.localScale = Vector3.one;

            modernPanel = modernPanel != null ? modernPanel : EnsureModernImage("Panel");
            modernContentCard = modernContentCard != null ? modernContentCard : EnsureModernImage("ContentCard");
            modernHeaderBar = modernHeaderBar != null ? modernHeaderBar : EnsureModernImage("HeaderBar");
            modernDivider = modernDivider != null ? modernDivider : EnsureModernImage("Divider");
            modernTitleText = modernTitleText != null ? modernTitleText : EnsureModernText("Title");
            modernScoreText = modernScoreText != null ? modernScoreText : EnsureModernText("Score");
            modernRewardText = modernRewardText != null ? modernRewardText : EnsureModernText("Reward");
            modernMenuButton = modernMenuButton != null ? modernMenuButton : EnsureModernButton("MenuButton", OnClickMenu);
            modernNextButton = modernNextButton != null ? modernNextButton : EnsureModernButton("NextButton", OnClickNext);
            modernRetryButton = modernRetryButton != null ? modernRetryButton : EnsureModernButton("RetryButton", OnClickRetry);
            modernRescueButton = modernRescueButton != null ? modernRescueButton : EnsureModernButton("RescueButton", OnClickRescue);
            Transform obsoleteFurnaceButton = modernRoot.Find("FurnaceButton");
            if (obsoleteFurnaceButton != null)
            {
                modernFurnaceButton = obsoleteFurnaceButton.GetComponent<Button>();
                obsoleteFurnaceButton.gameObject.SetActive(false);
            }

            Transform obsoleteFurnaceRoot = modernRoot.Find("FurnaceRoot");
            if (obsoleteFurnaceRoot != null)
                obsoleteFurnaceRoot.gameObject.SetActive(false);
        }

        private Image EnsureModernImage(string objectName)
        {
            Transform found = modernRoot.Find(objectName);
            if (found != null && found.TryGetComponent(out Image existing))
                return existing;

            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(modernRoot, false);
            imageObject.layer = modernRoot.gameObject.layer;
            return imageObject.GetComponent<Image>();
        }

        private TMP_Text EnsureModernText(string objectName)
        {
            Transform found = modernRoot.Find(objectName);
            if (found != null && found.TryGetComponent(out TMP_Text existing))
                return existing;

            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(modernRoot, false);
            textObject.layer = modernRoot.gameObject.layer;
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private Button EnsureModernButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            Transform found = modernRoot.Find(objectName);
            Button button;
            if (found != null && found.TryGetComponent(out Button existing))
            {
                button = existing;
            }
            else
            {
                GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(modernRoot, false);
                buttonObject.layer = modernRoot.gameObject.layer;
                button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return button;
        }

        private void LayoutModernPanel()
        {
            if (modernPanel == null)
                return;

            Vector2 availableSize = ResolveModernAvailableSize();
            float resolvedPanelWidth = Mathf.Max(availableSize.x - 56f, 1120f);
            float resolvedPanelHeight = Mathf.Max(availableSize.y - 48f, 620f);
            modernPanelWidth = resolvedPanelWidth;
            modernPanelHeight = resolvedPanelHeight;

            modernPanel.gameObject.SetActive(true);
            Sprite panelSprite = LoadResultWindowSprite();
            modernPanel.sprite = panelSprite;
            modernPanel.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            modernPanel.preserveAspect = false;
            modernPanel.color = PanelColor;
            modernPanel.raycastTarget = true;

            RectTransform rect = modernPanel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(resolvedPanelWidth, resolvedPanelHeight);
            rect.localScale = Vector3.one;
            rect.SetAsFirstSibling();

            LayoutModernLobbyDecor(resolvedPanelWidth, resolvedPanelHeight);

            Shadow shadow = modernPanel.GetComponent<Shadow>();
            if (shadow == null)
                shadow = modernPanel.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.56f);
            shadow.effectDistance = new Vector2(10f, -12f);

            Outline outline = modernPanel.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = panelSprite == null;
        }

        private void LayoutModernLobbyDecor(float panelWidth, float panelHeight)
        {
            ConfigureModernDecorImage(
                modernContentCard,
                LoadResultContentCardSprite(),
                new Vector2(0f, 8f),
                new Vector2(panelWidth - 96f, panelHeight - 190f),
                true,
                new Color(1f, 1f, 1f, 0.94f));

            ConfigureModernDecorImage(
                modernHeaderBar,
                LoadResultHeaderBarSprite(),
                new Vector2(0f, panelHeight * 0.5f - 92f),
                new Vector2(panelWidth - 140f, 116f),
                true,
                new Color(1f, 1f, 1f, 0.96f));

            ConfigureModernDecorImage(
                modernDivider,
                LoadResultDividerSprite(),
                new Vector2(0f, -panelHeight * 0.5f + 220f),
                new Vector2(panelWidth - 420f, 48f),
                false,
                new Color(1f, 0.95f, 0.7f, 0.9f));
        }

        private Vector2 ResolveModernAvailableSize()
        {
            if (modernRoot != null)
            {
                Vector2 rootSize = modernRoot.rect.size;
                if (rootSize.x > 100f && rootSize.y > 100f)
                    return rootSize;
            }

            Canvas canvas = panelRoot != null ? panelRoot.GetComponentInParent<Canvas>() : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null)
            {
                Vector2 canvasSize = canvasRect.rect.size;
                if (canvasSize.x > 100f && canvasSize.y > 100f)
                    return canvasSize;
            }

            return new Vector2(Mathf.Max(Screen.width, 1600f), Mathf.Max(Screen.height, 900f));
        }

        private void ConfigureModernDecorImage(Image image, Sprite sprite, Vector2 position, Vector2 size, bool stretch, Color color)
        {
            if (image == null)
                return;

            image.gameObject.SetActive(sprite != null);
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = stretch ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !stretch;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        private void SetModernTitle(string value, bool isWin)
        {
            if (modernTitleText == null)
                return;

            modernTitleText.gameObject.SetActive(true);
            modernTitleText.text = value;
            modernTitleText.color = Color.white;
            modernTitleText.fontSize = 84f;
            modernTitleText.fontSizeMin = 48f;
            modernTitleText.fontSizeMax = 88f;
            modernTitleText.enableAutoSizing = true;
            modernTitleText.alignment = TextAlignmentOptions.Center;
            modernTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            modernTitleText.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(modernTitleText);
            modernTitleText.enableVertexGradient = true;
            modernTitleText.colorGradient = isWin ? WinTitleGradient : LoseTitleGradient;
            modernTitleText.outlineWidth = 0.32f;
            modernTitleText.outlineColor = isWin
                ? new Color(0.02f, 0.18f, 0.04f, 1f)
                : new Color(0.24f, 0.045f, 0.01f, 1f);

            RectTransform rect = modernTitleText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, modernPanelHeight * 0.5f - 98f);
            rect.sizeDelta = new Vector2(Mathf.Max(modernPanelWidth - 260f, 780f), 108f);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        private void SetModernText(TMP_Text text, string value, Vector2 position, Color color, float fontSize)
        {
            SetModernText(text, value, position, color, fontSize, 70f);
        }

        private void SetModernText(TMP_Text text, string value, Vector2 position, Color color, float fontSize, float height)
        {
            if (text == null)
                return;

            bool visible = !string.IsNullOrWhiteSpace(value);
            text.gameObject.SetActive(visible);
            if (!visible)
                return;

            text.text = value;
            text.color = color;
            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Max(14f, fontSize - 8f);
            text.fontSizeMax = fontSize + 4f;
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(text);
            text.enableVertexGradient = true;
            text.colorGradient = GoldTextGradient;
            text.outlineWidth = 0.24f;
            text.outlineColor = new Color(0.01f, 0.03f, 0.005f, 0.94f);

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(Mathf.Max(modernPanelWidth - 360f, 720f), height);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        private void StyleResultButton(Button button, string label, Vector2 position, Vector2 size)
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.ColorTint;

            Image image = button.image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = LoadResultButtonSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = image.sprite != null ? Color.white : ButtonNormalColor;
                image.raycastTarget = true;

                Outline outline = image.GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = image.sprite == null;
            }

            ColorBlock colors = button.colors;
            bool hasButtonSprite = image != null && image.sprite != null;
            colors.normalColor = hasButtonSprite ? Color.white : ButtonNormalColor;
            colors.highlightedColor = hasButtonSprite ? new Color(1.08f, 1.08f, 1.08f, 1f) : ButtonHighlightedColor;
            colors.pressedColor = hasButtonSprite ? new Color(0.82f, 0.82f, 0.82f, 1f) : ButtonPressedColor;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.08f, 0.09f, 0.08f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Graphic[] childGraphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                Graphic graphic = childGraphics[i];
                if (graphic == null || graphic.transform == button.transform || graphic is TMP_Text)
                    continue;

                graphic.gameObject.SetActive(false);
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                Vector2 resolvedSize = new Vector2(Mathf.Max(size.x, 330f), Mathf.Max(size.y, 88f));
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = resolvedSize;
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(button.transform, false);
                labelObject.layer = button.gameObject.layer;
                text = labelObject.GetComponent<TextMeshProUGUI>();
            }

            text.gameObject.SetActive(true);
            text.text = label;
            text.color = ButtonTextColor;
            text.fontSize = 27f;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 34f;
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(text);
            text.enableVertexGradient = true;
            text.colorGradient = GoldTextGradient;
            text.outlineWidth = 0.24f;
            text.outlineColor = new Color(0.01f, 0.025f, 0f, 1f);

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(42f, 12f);
            textRect.offsetMax = new Vector2(-42f, -14f);
            textRect.localScale = Vector3.one;
        }

        private Sprite LoadResultWindowSprite()
        {
            if (cachedResultWindowSprite != null)
                return cachedResultWindowSprite;

            cachedResultWindowSprite = LoadAnyResultSprite(ResultWindowResourcePath);
            return cachedResultWindowSprite;
        }

        private Sprite LoadResultContentCardSprite()
        {
            if (cachedResultContentCardSprite != null)
                return cachedResultContentCardSprite;

            cachedResultContentCardSprite = LoadAnyResultSprite(ResultContentCardResourcePath);
            return cachedResultContentCardSprite;
        }

        private Sprite LoadResultHeaderBarSprite()
        {
            if (cachedResultHeaderBarSprite != null)
                return cachedResultHeaderBarSprite;

            cachedResultHeaderBarSprite = LoadAnyResultSprite(ResultHeaderBarResourcePath);
            return cachedResultHeaderBarSprite;
        }

        private Sprite LoadResultDividerSprite()
        {
            if (cachedResultDividerSprite != null)
                return cachedResultDividerSprite;

            cachedResultDividerSprite = LoadAnyResultSprite(ResultDividerResourcePath);
            return cachedResultDividerSprite;
        }

        private Sprite LoadResultButtonSprite()
        {
            if (cachedResultButtonSprite != null)
                return cachedResultButtonSprite;

            cachedResultButtonSprite = LoadAnyResultSprite(ResultButtonResourcePath);
            return cachedResultButtonSprite;
        }

        private void OnClickFurnace()
        {
            if (pendingFurnaceFeedAmount <= 0)
                return;

            MahjongFurnaceFeedResult result;
            if (furnaceFeedClaimed)
            {
                result = new MahjongFurnaceFeedResult
                {
                    Added = 0,
                    Capacity = MahjongFurnaceService.Capacity,
                    FillBefore = MahjongFurnaceService.CurrentFill,
                    FillAfter = MahjongFurnaceService.CurrentFill
                };
            }
            else
            {
                result = MahjongFurnaceService.Feed(pendingFurnaceFeedAmount);
                furnaceFeedClaimed = true;
                pendingFurnaceFeedAmount = 0;
            }

            if (modernFurnaceButton != null)
            {
                modernFurnaceButton.interactable = false;
                TMP_Text label = modernFurnaceButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = "ЖЕРЛО ✓";
            }

            OpenFurnaceWindow(result);
        }

        private void OnClickRescue()
        {
            if (rescueAdInProgress || !CanShowRescueButton())
                return;

            rescueAdInProgress = true;
            SetResultNavigationInteractable(false);

            MonetizationService.Ensure().ShowRewardedAd(MonetizationService.MahjongAssistRewardedPlacementId, result =>
            {
                rescueAdInProgress = false;

                if (result.IsCompleted && board != null && board.TryRescueAfterLoseUndo())
                {
                    HideImmediate();
                    return;
                }

                SetModernText(
                    modernRewardText,
                    result.IsCompleted
                        ? GameLocalization.Text("mahjong.result.rescue_failed")
                        : ResolveRewardedStatus(result.Message),
                    new Vector2(0f, -18f),
                    TextPrimaryColor,
                    23f);

                SetResultNavigationInteractable(true);
            });
        }

        private bool CanShowRescueButton()
        {
            return MahjongSession.LaunchMode != MahjongLaunchMode.Battle
                && board != null
                && board.CanRescueAfterLoseUndo();
        }

        private static string ResolveRewardedStatus(string fallback)
        {
            return string.IsNullOrWhiteSpace(fallback)
                ? GameLocalization.Text("mahjong.result.rescue_ad_not_ready")
                : fallback;
        }

        private void OpenFurnaceWindow(MahjongFurnaceFeedResult result)
        {
            EnsureFurnaceWindow();
            if (furnaceRoot == null)
                return;

            furnaceRoot.gameObject.SetActive(true);
            furnaceRoot.SetAsLastSibling();

            if (furnaceTitleText != null)
            {
                furnaceTitleText.text = result != null && result.HasRewards ? "ЖЕРЛО ОТКРЫЛОСЬ" : "ЖЕРЛО";
                furnaceTitleText.enableVertexGradient = true;
                furnaceTitleText.colorGradient = result != null && result.HasRewards ? WinTitleGradient : GoldTextGradient;
            }

            if (furnaceBodyText != null)
                furnaceBodyText.text = BuildFurnaceMessage(result);
        }

        private void CloseFurnaceWindow()
        {
            if (furnaceRoot != null)
                furnaceRoot.gameObject.SetActive(false);
        }

        private void EnsureFurnaceWindow()
        {
            EnsureModernResultRoot();
            if (modernRoot == null)
                return;

            if (furnaceRoot == null)
            {
                Transform found = modernRoot.Find("FurnaceRoot");
                if (found != null)
                    furnaceRoot = found as RectTransform;
            }

            if (furnaceRoot == null)
            {
                GameObject rootObject = new GameObject("FurnaceRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rootObject.transform.SetParent(modernRoot, false);
                rootObject.layer = modernRoot.gameObject.layer;
                furnaceRoot = rootObject.GetComponent<RectTransform>();
            }

            furnaceRoot.anchorMin = Vector2.zero;
            furnaceRoot.anchorMax = Vector2.one;
            furnaceRoot.offsetMin = Vector2.zero;
            furnaceRoot.offsetMax = Vector2.zero;
            furnaceRoot.localScale = Vector3.one;

            Image dim = furnaceRoot.GetComponent<Image>();
            if (dim != null)
            {
                dim.color = new Color(0f, 0f, 0f, 0.42f);
                dim.raycastTarget = true;
            }

            furnacePanel = furnacePanel != null ? furnacePanel : EnsureFurnaceImage("Panel");
            Sprite panelSprite = LoadResultWindowSprite();
            furnacePanel.sprite = panelSprite;
            furnacePanel.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            furnacePanel.color = Color.white;
            furnacePanel.raycastTarget = true;
            RectTransform panelRect = furnacePanel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(700f, 430f);
            panelRect.localScale = Vector3.one;

            furnaceTitleText = furnaceTitleText != null ? furnaceTitleText : EnsureFurnaceText("Title");
            ConfigureFurnaceText(furnaceTitleText, new Vector2(0f, 118f), new Vector2(540f, 66f), 48f, TextAlignmentOptions.Center);

            furnaceBodyText = furnaceBodyText != null ? furnaceBodyText : EnsureFurnaceText("Body");
            ConfigureFurnaceText(furnaceBodyText, new Vector2(0f, 8f), new Vector2(560f, 168f), 28f, TextAlignmentOptions.Center);

            furnaceCloseButton = furnaceCloseButton != null ? furnaceCloseButton : EnsureFurnaceButton("CloseButton", CloseFurnaceWindow);
            StyleResultButton(furnaceCloseButton, "OK", new Vector2(0f, -142f), new Vector2(190f, 58f));

            furnaceRoot.gameObject.SetActive(false);
        }

        private Image EnsureFurnaceImage(string objectName)
        {
            Transform found = furnaceRoot.Find(objectName);
            if (found != null && found.TryGetComponent(out Image existing))
                return existing;

            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(furnaceRoot, false);
            imageObject.layer = furnaceRoot.gameObject.layer;
            return imageObject.GetComponent<Image>();
        }

        private TMP_Text EnsureFurnaceText(string objectName)
        {
            Transform found = furnaceRoot.Find(objectName);
            if (found != null && found.TryGetComponent(out TMP_Text existing))
                return existing;

            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(furnaceRoot, false);
            textObject.layer = furnaceRoot.gameObject.layer;
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private Button EnsureFurnaceButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            Transform found = furnaceRoot.Find(objectName);
            Button button;
            if (found != null && found.TryGetComponent(out Button existing))
            {
                button = existing;
            }
            else
            {
                GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(furnaceRoot, false);
                buttonObject.layer = furnaceRoot.gameObject.layer;
                button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return button;
        }

        private void ConfigureFurnaceText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            if (text == null)
                return;

            text.gameObject.SetActive(true);
            text.color = TextPrimaryColor;
            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Max(16f, fontSize - 12f);
            text.fontSizeMax = fontSize + 4f;
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(text);
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color(0.01f, 0.025f, 0f, 1f);

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        private static string BuildFurnaceMessage(MahjongFurnaceFeedResult result)
        {
            if (result == null)
                return string.Empty;

            string progress = $"Камни: {result.FillAfter}/{result.Capacity}";
            if (!result.HasRewards)
                return $"+{result.Added} ушло в жерло.\n{progress}\nШанс Legendary/Mythic растет с каждым заполнением.";

            string message = $"+{result.Added} ушло в жерло.\n";
            for (int i = 0; i < result.Rewards.Count; i++)
            {
                MahjongFurnaceRewardResult reward = result.Rewards[i];
                if (reward == null)
                    continue;

                message += reward.Title + ": " + reward.Description;
                if (i < result.Rewards.Count - 1)
                    message += "\n";
            }

            return message + "\n" + progress;
        }

        private static Sprite LoadAnyResultSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0)
                return null;

            return sprites[0];
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[LevelResultUI] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName, StoryEndlessDoorSpriteResourcePath, false);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip, audioVolume);
        }

        private string RewardCurrencyLabel()
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            return language switch
            {
                GameLanguage.English => "OzTile",
                GameLanguage.Turkish => "Oz Tile",
                GameLanguage.German => "OzTile",
                _ => "\u041E\u0437 Tile"
            };
        }
    }
}
