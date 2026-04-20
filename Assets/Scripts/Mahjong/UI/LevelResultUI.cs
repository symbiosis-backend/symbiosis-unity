using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class LevelResultUI : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private TileStore tileStore;
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

        private AudioSource audioSource;
        private CanvasGroup panelCanvasGroup;
        private bool shown;

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

            if (resultImage != null)
            {
                Sprite spriteToUse = isWin ? winSprite : loseSprite;
                if (spriteToUse != null)
                    resultImage.sprite = spriteToUse;
            }

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
                        MahjongMatchService.I.LastProcessedResult.GrantedAltin,
                        GameLocalization.Text("common.oz_altin"));
                }
                else
                {
                    rewardText.gameObject.SetActive(false);
                }
            }

            if (btnMenu != null)
                btnMenu.gameObject.SetActive(true);

            bool hasNext = false;
            if (isWin && tileStore != null && MahjongSession.LaunchMode == MahjongLaunchMode.Story)
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

            PlaySound(isWin ? winClip : loseClip);

            Debug.Log($"[LevelResultUI] Show | isWin={isWin} | level={MahjongSession.StoryLevel} | stage={MahjongSession.StoryStage} | hasNext={hasNext}");
        }

        private void HideImmediate()
        {
            shown = false;

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
        }

        private void OnClickMenu()
        {
            LoadSceneWithDoor(lobbySceneName);
        }

        private void OnClickRetry()
        {
            LoadSceneWithDoor(gameSceneName);
        }

        private void OnClickNext()
        {
            AutoResolveReferences();

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

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[LevelResultUI] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip, audioVolume);
        }
    }
}
