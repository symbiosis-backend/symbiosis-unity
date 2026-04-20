using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem I { get; private set; }

        private const string TotalScoreKey = "Mahjong_TotalScore";

        [Header("Scoring")]
        [SerializeField, Min(1)] private int basePairScore = 100;

        public int CurrentLevelScore { get; private set; }
        public int TotalScore { get; private set; }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            TotalScore = PlayerPrefs.GetInt(TotalScoreKey, 0);
        }

        public void ResetLevelScore()
        {
            CurrentLevelScore = 0;
        }

        public int AddPairScore()
        {
            int comboBonus = ComboSystem.I != null ? ComboSystem.I.GetCurrentBonus() : 0;
            int added = basePairScore + comboBonus;
            CurrentLevelScore += added;
            return added;
        }

        public void CommitLevelScoreToTotal()
        {
            TotalScore += CurrentLevelScore;
            PlayerPrefs.SetInt(TotalScoreKey, TotalScore);
            PlayerPrefs.Save();
        }

        public void SaveBestScore(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return;

            string key = $"Mahjong_BestScore_{levelId}";
            int oldBest = PlayerPrefs.GetInt(key, 0);

            if (CurrentLevelScore > oldBest)
            {
                PlayerPrefs.SetInt(key, CurrentLevelScore);
                PlayerPrefs.Save();
            }
        }

        public int GetBestScore(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return 0;

            return PlayerPrefs.GetInt($"Mahjong_BestScore_{levelId}", 0);
        }

        public int GetTotalScore()
        {
            return TotalScore;
        }
    }
}