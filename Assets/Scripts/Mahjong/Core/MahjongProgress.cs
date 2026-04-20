using UnityEngine;

namespace MahjongGame
{
    public static class MahjongProgress
    {
        private const string TutorialCompletedKey = "mahjong_tutorial_completed";
        private const string UnlockedLevelKey = "mahjong_unlocked_level";

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
            if (UnlockedLevel < 1)
                UnlockedLevel = 1;
        }

        public static void UnlockNextLevel(int currentLevel)
        {
            int next = Mathf.Max(1, currentLevel + 1);
            if (next > UnlockedLevel)
                UnlockedLevel = next;
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(TutorialCompletedKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.Save();
        }
    }
}