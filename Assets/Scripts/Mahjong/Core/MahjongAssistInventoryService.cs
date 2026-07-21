using UnityEngine;

namespace MahjongGame
{
    public enum MahjongAssistBooster
    {
        HintPair,
        Shuffle,
        Undo
    }

    public static class MahjongAssistInventoryService
    {
        public const int InitialStock = 3;
        public const int RewardedGrantAmount = 1;

        private const string InitializedKey = "Mahjong_AssistBoosters_Initialized";
        private const string HintCountKey = "Mahjong_AssistBoosters_HintPair";
        private const string ShuffleCountKey = "Mahjong_AssistBoosters_Shuffle";
        private const string UndoCountKey = "Mahjong_AssistBoosters_Undo";

        public static void EnsureInitialized()
        {
            if (PlayerPrefs.GetInt(InitializedKey, 0) == 1)
                return;

            PlayerPrefs.SetInt(HintCountKey, InitialStock);
            PlayerPrefs.SetInt(ShuffleCountKey, InitialStock);
            PlayerPrefs.SetInt(UndoCountKey, InitialStock);
            PlayerPrefs.SetInt(InitializedKey, 1);
            PlayerPrefs.Save();
        }

        public static int GetCount(MahjongAssistBooster booster)
        {
            EnsureInitialized();
            return Mathf.Max(0, PlayerPrefs.GetInt(GetKey(booster), 0));
        }

        public static bool TryConsume(MahjongAssistBooster booster)
        {
            EnsureInitialized();

            string key = GetKey(booster);
            int count = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
            if (count <= 0)
                return false;

            PlayerPrefs.SetInt(key, count - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void Grant(MahjongAssistBooster booster, int amount)
        {
            if (amount <= 0)
                return;

            EnsureInitialized();

            string key = GetKey(booster);
            int count = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
            PlayerPrefs.SetInt(key, count + amount);
            PlayerPrefs.Save();
        }

        private static string GetKey(MahjongAssistBooster booster)
        {
            switch (booster)
            {
                case MahjongAssistBooster.Shuffle:
                    return ShuffleCountKey;
                case MahjongAssistBooster.Undo:
                    return UndoCountKey;
                case MahjongAssistBooster.HintPair:
                default:
                    return HintCountKey;
            }
        }
    }
}
