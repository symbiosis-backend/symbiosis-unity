using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private VoidLevelConfig[] levels;
        [SerializeField] private WaveManager waveManager;

        private int currentIndex = -1;

        public int CurrentLevelNumber => currentIndex >= 0 && currentIndex < levels.Length ? levels[currentIndex].LevelNumber : 0;
        public bool HasMoreLevels => currentIndex + 1 < levels.Length;
        public int CurrentIndex => currentIndex;
        public int LevelCount => levels != null ? levels.Length : 0;

        public void Bind(VoidLevelConfig[] levelConfigs, WaveManager waves)
        {
            levels = levelConfigs;
            waveManager = waves;
        }

        public void StartFirstLevel()
        {
            currentIndex = -1;
            StartNextLevel();
        }

        public void StartNextLevel()
        {
            if (!HasMoreLevels)
            {
                GameManager.I?.WinGame();
                return;
            }

            StartLevel(currentIndex + 1, StartNextLevel);
        }

        public void StartLevel(int levelIndex, System.Action completed)
        {
            if (levels == null || levels.Length == 0)
            {
                completed?.Invoke();
                return;
            }

            currentIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
            GameManager.I?.SetLevel(CurrentLevelNumber);
            waveManager.PlayLevel(levels[currentIndex], completed);
        }

        public int GetLevelNumber(int levelIndex)
        {
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length || levels[levelIndex] == null)
                return levelIndex + 1;

            return levels[levelIndex].LevelNumber;
        }

        public void Stop()
        {
            if (waveManager != null)
                waveManager.Stop();
        }
    }
}
