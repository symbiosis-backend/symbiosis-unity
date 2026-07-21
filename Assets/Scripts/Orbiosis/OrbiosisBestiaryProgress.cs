using UnityEngine;

namespace MahjongGame.Orbiosis
{
    public static class OrbiosisBestiaryProgress
    {
        private const string KeyPrefix = "Orbiosis.Bestiary.Unlocked.";

        public static bool IsUnlocked(string enemyId)
        {
            return !string.IsNullOrEmpty(enemyId) && PlayerPrefs.GetInt(KeyPrefix + enemyId, 0) != 0;
        }

        public static bool MarkEncountered(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId) || OrbiosisBestiaryLibrary.Find(enemyId) == null || IsUnlocked(enemyId))
                return false;

            PlayerPrefs.SetInt(KeyPrefix + enemyId, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int UnlockedCount()
        {
            OrbiosisBestiaryEntry[] entries = OrbiosisBestiaryLibrary.All();
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (IsUnlocked(entries[i].Id))
                    count++;
            }

            return count;
        }

        public static void ResetAll()
        {
            OrbiosisBestiaryEntry[] entries = OrbiosisBestiaryLibrary.All();
            for (int i = 0; i < entries.Length; i++)
                PlayerPrefs.DeleteKey(KeyPrefix + entries[i].Id);

            PlayerPrefs.Save();
        }
    }
}
