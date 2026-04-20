using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private VoidGameUi ui;
        [SerializeField] private ScreenShake2D screenShake;

        private int score;
        private int currentLevel;
        private int currentLevelIndex;
        private bool ended;

        public int Score => score;
        public int CurrentLevel => currentLevel;

        private void Awake()
        {
            I = this;
        }

        private void OnDestroy()
        {
            if (I == this)
                I = null;
        }

        public void Bind(PlayerHealth health, LevelManager levels, VoidGameUi gameUi, ScreenShake2D shake)
        {
            playerHealth = health;
            levelManager = levels;
            ui = gameUi;
            screenShake = shake;
        }

        private void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged += OnPlayerHealthChanged;
                playerHealth.Died += LoseGame;
            }

            ShowMainMenu();
        }

        public void RestartGame()
        {
            StartLevel(currentLevelIndex);
        }

        public void ShowMainMenu()
        {
            ended = true;
            ClearEnemies();
            levelManager?.Stop();
            SetPlayerGameplay(false);
            ui?.SetScore(score);
            ui?.SetLevel(currentLevel);
            ui?.ShowMainMenu();
        }

        public void ShowLevelSelect()
        {
            ended = true;
            ClearEnemies();
            levelManager?.Stop();
            SetPlayerGameplay(false);
            ui?.ShowLevelSelect();
        }

        public void StartLevel(int levelIndex)
        {
            ended = false;
            score = 0;
            currentLevelIndex = Mathf.Max(0, levelIndex);
            ClearEnemies();

            if (playerHealth != null)
            {
                playerHealth.transform.position = Vector3.zero;
                playerHealth.ResetHealth();
            }

            SetPlayerGameplay(true);
            ui?.ShowGameplay();
            ui?.SetScore(score);

            levelManager?.StartLevel(currentLevelIndex, CompleteLevel);
        }

        public void StartNextLevel()
        {
            if (levelManager == null)
                return;

            StartLevel(Mathf.Min(currentLevelIndex + 1, levelManager.LevelCount - 1));
        }

        public void CompleteLevel()
        {
            if (ended)
                return;

            ended = true;
            ClearEnemies();
            levelManager?.Stop();
            SetPlayerGameplay(false);
            bool hasNext = levelManager != null && currentLevelIndex + 1 < levelManager.LevelCount;
            ui?.ShowLevelComplete(hasNext);
        }

        public void AddScore(int amount)
        {
            if (ended)
                return;

            score += Mathf.Max(0, amount);
            ui?.SetScore(score);
            screenShake?.Shake(0.04f, 0.035f);
        }

        public void SetLevel(int level)
        {
            currentLevel = level;
            ui?.SetLevel(level);
        }

        public void WinGame()
        {
            if (ended)
                return;

            ended = true;
            levelManager?.Stop();
            SetPlayerGameplay(false);
            ui?.ShowVictory();
        }

        public void LoseGame()
        {
            if (ended)
                return;

            ended = true;
            levelManager?.Stop();
            SetPlayerGameplay(false);
            ui?.ShowDefeat();
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            ui?.SetHealth(current, max);
            screenShake?.Shake(0.09f, 0.08f);
        }

        private void ClearEnemies()
        {
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                    enemies[i].Kill(false);
            }
        }

        private void SetPlayerGameplay(bool enabled)
        {
            if (playerHealth == null)
                return;

            PlayerController controller = playerHealth.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = enabled;

            PlayerShooter shooter = playerHealth.GetComponent<PlayerShooter>();
            if (shooter != null)
                shooter.enabled = enabled;
        }
    }
}
