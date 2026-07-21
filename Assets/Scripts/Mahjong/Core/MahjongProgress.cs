using UnityEngine;

namespace MahjongGame
{
    public static class MahjongProgress
    {
        private const string TutorialCompletedKey = "mahjong_tutorial_completed";
        private const string UnlockedLevelKey = "mahjong_unlocked_level";
        private const string HardcoreStagePrefix = "mahjong_story_hardcore_stage_";
        private const string StoryStageCompletedPrefix = "mahjong_story_stage_completed_";
        private const string StoryStageBestScorePrefix = "mahjong_story_stage_best_score_";

        public static bool TutorialCompleted
        {
            get => PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TutorialCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int UnlockedLevel
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelKey, 1));
            set
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(1, value));
                PlayerPrefs.Save();
            }
        }

        public static void CompleteTutorial()
        {
            TutorialCompleted = true;
            if (UnlockedLevel < 2)
                UnlockedLevel = 2;
        }

        public static void UnlockNextLevel(int currentLevel)
        {
            int next = Mathf.Max(1, currentLevel + 1);
            if (next > UnlockedLevel)
                UnlockedLevel = next;
        }

        public static int GetHardcoreUnlockedStage(int level)
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(HardcoreStagePrefix + Mathf.Max(1, level), 1));
        }

        public static void AdvanceHardcoreStage(int level, int completedStage, int stageCount)
        {
            int safeLevel = Mathf.Max(1, level);
            int nextStage = Mathf.Clamp(completedStage + 1, 1, Mathf.Max(1, stageCount));
            if (completedStage >= stageCount)
                nextStage = Mathf.Max(1, stageCount);

            if (nextStage > GetHardcoreUnlockedStage(safeLevel))
            {
                PlayerPrefs.SetInt(HardcoreStagePrefix + safeLevel, nextStage);
                PlayerPrefs.Save();
            }
        }

        public static void ResetHardcoreRun(int level)
        {
            PlayerPrefs.SetInt(HardcoreStagePrefix + Mathf.Max(1, level), 1);
            PlayerPrefs.Save();
        }

        public static bool IsStoryStageCompleted(MahjongStoryDifficulty difficulty, int level, int stage)
        {
            return PlayerPrefs.GetInt(BuildStoryStageKey(StoryStageCompletedPrefix, difficulty, level, stage), 0) == 1;
        }

        public static int GetStoryStageBestScore(MahjongStoryDifficulty difficulty, int level, int stage)
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(BuildStoryStageKey(StoryStageBestScorePrefix, difficulty, level, stage), 0));
        }

        public static void RecordStoryStageResult(MahjongStoryDifficulty difficulty, int level, int stage, int score)
        {
            MahjongStoryDifficulty safeDifficulty = NormalizeDifficulty(difficulty);
            int safeLevel = Mathf.Max(1, level);
            int safeStage = Mathf.Max(1, stage);
            int safeScore = Mathf.Max(0, score);

            PlayerPrefs.SetInt(BuildStoryStageKey(StoryStageCompletedPrefix, safeDifficulty, safeLevel, safeStage), 1);

            string scoreKey = BuildStoryStageKey(StoryStageBestScorePrefix, safeDifficulty, safeLevel, safeStage);
            if (safeScore > PlayerPrefs.GetInt(scoreKey, 0))
                PlayerPrefs.SetInt(scoreKey, safeScore);

            PlayerPrefs.Save();
        }

        public static int GetStoryLevelBestScoreTotal(MahjongStoryDifficulty difficulty, int level, int stageCount)
        {
            int total = 0;
            int safeStageCount = Mathf.Max(0, stageCount);
            for (int stage = 1; stage <= safeStageCount; stage++)
                total += GetStoryStageBestScore(difficulty, level, stage);

            return total;
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(TutorialCompletedKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            for (int level = 1; level <= 128; level++)
                PlayerPrefs.DeleteKey(HardcoreStagePrefix + level);
            DeleteStoryStageKeys(128, 128);
            PlayerPrefs.Save();
        }

        private static string BuildStoryStageKey(string prefix, MahjongStoryDifficulty difficulty, int level, int stage)
        {
            return prefix + NormalizeDifficulty(difficulty).ToString().ToLowerInvariant() + "_" + Mathf.Max(1, level) + "_" + Mathf.Max(1, stage);
        }

        private static MahjongStoryDifficulty NormalizeDifficulty(MahjongStoryDifficulty difficulty)
        {
            return difficulty == MahjongStoryDifficulty.Unset ? MahjongStoryDifficulty.Medium : difficulty;
        }

        private static void DeleteStoryStageKeys(int maxLevel, int maxStage)
        {
            MahjongStoryDifficulty[] difficulties =
            {
                MahjongStoryDifficulty.Easy,
                MahjongStoryDifficulty.Medium,
                MahjongStoryDifficulty.Hardcore
            };

            for (int i = 0; i < difficulties.Length; i++)
            {
                for (int level = 1; level <= maxLevel; level++)
                {
                    for (int stage = 1; stage <= maxStage; stage++)
                    {
                        PlayerPrefs.DeleteKey(BuildStoryStageKey(StoryStageCompletedPrefix, difficulties[i], level, stage));
                        PlayerPrefs.DeleteKey(BuildStoryStageKey(StoryStageBestScorePrefix, difficulties[i], level, stage));
                    }
                }
            }
        }
    }
}
