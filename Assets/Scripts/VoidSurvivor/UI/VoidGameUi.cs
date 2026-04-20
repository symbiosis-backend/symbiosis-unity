using UnityEngine;
using UnityEngine.UI;
using MahjongGame;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class VoidGameUi : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject victoryRoot;
        [SerializeField] private GameObject defeatRoot;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private GameObject levelSelectRoot;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject levelCompleteRoot;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button retryLevelButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button defeatLevelSelectButton;
        [SerializeField] private Button[] levelButtons;

        private float currentHealth;
        private float maxHealth = 100f;
        private int currentLevel = 1;
        private int currentScore;

        private void OnEnable()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            RefreshHudText();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        public void Bind(Text hp, Text level, Text score, GameObject victory, GameObject defeat, Button restart)
        {
            healthText = hp;
            levelText = level;
            scoreText = score;
            victoryRoot = victory;
            defeatRoot = defeat;
            restartButton = restart;

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => GameManager.I?.RestartGame());
            }
        }

        public void BindFlow(
            GameObject mainMenu,
            GameObject levelSelect,
            GameObject gameplay,
            GameObject levelComplete,
            Button start,
            Button retry,
            Button next,
            Button select,
            Button defeatSelect,
            Button[] levels)
        {
            mainMenuRoot = mainMenu;
            levelSelectRoot = levelSelect;
            gameplayRoot = gameplay;
            levelCompleteRoot = levelComplete;
            startButton = start;
            retryLevelButton = retry;
            nextLevelButton = next;
            levelSelectButton = select;
            defeatLevelSelectButton = defeatSelect;
            levelButtons = levels;

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => GameManager.I?.ShowLevelSelect());
            }

            if (retryLevelButton != null)
            {
                retryLevelButton.onClick.RemoveAllListeners();
                retryLevelButton.onClick.AddListener(() => GameManager.I?.RestartGame());
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => GameManager.I?.StartNextLevel());
            }

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.RemoveAllListeners();
                levelSelectButton.onClick.AddListener(() => GameManager.I?.ShowLevelSelect());
            }

            if (defeatLevelSelectButton != null)
            {
                defeatLevelSelectButton.onClick.RemoveAllListeners();
                defeatLevelSelectButton.onClick.AddListener(() => GameManager.I?.ShowLevelSelect());
            }

            if (levelButtons == null)
                return;

            for (int i = 0; i < levelButtons.Length; i++)
            {
                int levelIndex = i;
                if (levelButtons[i] != null)
                {
                    levelButtons[i].onClick.RemoveAllListeners();
                    levelButtons[i].onClick.AddListener(() => GameManager.I?.StartLevel(levelIndex));
                }
            }
        }

        public void SetHealth(float current, float max)
        {
            currentHealth = current;
            maxHealth = max;
            RefreshHealthText();
        }

        public void SetLevel(int level)
        {
            currentLevel = level;
            RefreshLevelText();
        }

        public void SetScore(int score)
        {
            currentScore = score;
            RefreshScoreText();
        }

        public void ShowVictory()
        {
            HideAllScreens();
            if (victoryRoot != null)
                victoryRoot.SetActive(true);
        }

        public void ShowDefeat()
        {
            HideAllScreens();
            if (defeatRoot != null)
                defeatRoot.SetActive(true);
        }

        public void ShowMainMenu()
        {
            HideAllScreens();
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            HideAllScreens();
            if (levelSelectRoot != null)
                levelSelectRoot.SetActive(true);
        }

        public void ShowGameplay()
        {
            HideAllScreens();
            SetHudVisible(true);
            if (gameplayRoot != null)
                gameplayRoot.SetActive(true);
        }

        public void ShowLevelComplete(bool hasNextLevel)
        {
            HideAllScreens();
            if (levelCompleteRoot != null)
                levelCompleteRoot.SetActive(true);

            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(hasNextLevel);
        }

        public void HideEndScreens()
        {
            if (victoryRoot != null)
                victoryRoot.SetActive(false);

            if (defeatRoot != null)
                defeatRoot.SetActive(false);
        }

        private void HideAllScreens()
        {
            SetHudVisible(false);

            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);

            if (levelSelectRoot != null)
                levelSelectRoot.SetActive(false);

            if (gameplayRoot != null)
                gameplayRoot.SetActive(false);

            if (levelCompleteRoot != null)
                levelCompleteRoot.SetActive(false);

            HideEndScreens();
        }

        private void SetHudVisible(bool visible)
        {
            if (healthText != null)
                healthText.gameObject.SetActive(visible);

            if (levelText != null)
                levelText.gameObject.SetActive(visible);

            if (scoreText != null)
                scoreText.gameObject.SetActive(visible);
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshHudText();
        }

        private void RefreshHudText()
        {
            RefreshHealthText();
            RefreshLevelText();
            RefreshScoreText();
        }

        private void RefreshHealthText()
        {
            if (healthText != null)
                healthText.text = GameLocalization.Format("void.hp", Mathf.CeilToInt(currentHealth), Mathf.CeilToInt(maxHealth));
        }

        private void RefreshLevelText()
        {
            if (levelText != null)
                levelText.text = GameLocalization.Format("void.level", currentLevel);
        }

        private void RefreshScoreText()
        {
            if (scoreText != null)
                scoreText.text = GameLocalization.Format("void.score", currentScore);
        }
    }
}
