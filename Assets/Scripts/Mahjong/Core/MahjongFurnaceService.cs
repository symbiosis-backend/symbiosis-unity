using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public enum MahjongFurnaceRewardTier
    {
        Booster = 0,
        Legendary = 1,
        Mythic = 2
    }

    [Serializable]
    public sealed class MahjongFurnaceRewardResult
    {
        public MahjongFurnaceRewardTier Tier;
        public string Title;
        public string Description;
        public string TileId;
        public MahjongAssistBooster Booster;
        public int BoosterAmount;
        public bool GrantedTileCopy;
    }

    [Serializable]
    public sealed class MahjongFurnaceFeedResult
    {
        public int Added;
        public int FillBefore;
        public int FillAfter;
        public int Capacity;
        public List<MahjongFurnaceRewardResult> Rewards = new();

        public bool HasRewards => Rewards != null && Rewards.Count > 0;
    }

    public static class MahjongFurnaceService
    {
        public const int Capacity = 12;

        private const string FillKey = "Mahjong_Furnace_Fill";
        private const string LegendaryPityKey = "Mahjong_Furnace_LegendaryPity";
        private const string MythicPityKey = "Mahjong_Furnace_MythicPity";
        private const string LegendaryShardKey = "Mahjong_Furnace_LegendaryShards";
        private const string MythicShardKey = "Mahjong_Furnace_MythicShards";

        private const int LegendaryPityLimit = 5;
        private const int MythicPityLimit = 9;
        private const float LegendaryChance = 0.18f;
        private const float MythicChance = 0.06f;

        public static int CurrentFill => Mathf.Clamp(PlayerPrefs.GetInt(FillKey, 0), 0, Capacity - 1);

        public static int LegendaryShards => Mathf.Max(0, PlayerPrefs.GetInt(LegendaryShardKey, 0));

        public static int MythicShards => Mathf.Max(0, PlayerPrefs.GetInt(MythicShardKey, 0));

        public static int CalculateFeedAmount(bool isWin)
        {
            int score = ScoreSystem.I != null ? Mathf.Max(0, ScoreSystem.I.CurrentLevelScore) : 0;
            int scoreBonus = Mathf.Clamp(score / 1500, 0, 3);
            int modeBonus = MahjongSession.LaunchMode == MahjongLaunchMode.Endless ? 2 : 1;
            return isWin ? 4 + scoreBonus + modeBonus : 2;
        }

        public static MahjongFurnaceFeedResult Feed(int amount)
        {
            MahjongFurnaceFeedResult result = new MahjongFurnaceFeedResult
            {
                Added = Mathf.Max(0, amount),
                Capacity = Capacity,
                FillBefore = CurrentFill
            };

            int fill = result.FillBefore + result.Added;
            while (fill >= Capacity)
            {
                fill -= Capacity;
                result.Rewards.Add(RollReward());
            }

            result.FillAfter = Mathf.Clamp(fill, 0, Capacity - 1);
            PlayerPrefs.SetInt(FillKey, result.FillAfter);
            PlayerPrefs.Save();
            SaveProfileIfReady();
            return result;
        }

        private static MahjongFurnaceRewardResult RollReward()
        {
            int legendaryPity = Mathf.Max(0, PlayerPrefs.GetInt(LegendaryPityKey, 0));
            int mythicPity = Mathf.Max(0, PlayerPrefs.GetInt(MythicPityKey, 0));

            float roll = UnityEngine.Random.value;
            bool mythic = mythicPity + 1 >= MythicPityLimit || roll < MythicChance;
            bool legendary = !mythic && (legendaryPity + 1 >= LegendaryPityLimit || roll < MythicChance + LegendaryChance);

            if (mythic)
            {
                PlayerPrefs.SetInt(MythicPityKey, 0);
                PlayerPrefs.SetInt(LegendaryPityKey, 0);
                return GrantRareTile(BattleTileRarity.Mythic);
            }

            if (legendary)
            {
                PlayerPrefs.SetInt(MythicPityKey, mythicPity + 1);
                PlayerPrefs.SetInt(LegendaryPityKey, 0);
                return GrantRareTile(BattleTileRarity.Legendary);
            }

            PlayerPrefs.SetInt(MythicPityKey, mythicPity + 1);
            PlayerPrefs.SetInt(LegendaryPityKey, legendaryPity + 1);
            return GrantBoosterReward();
        }

        private static MahjongFurnaceRewardResult GrantBoosterReward()
        {
            MahjongAssistBooster booster = (MahjongAssistBooster)UnityEngine.Random.Range(0, 3);
            int amount = UnityEngine.Random.value < 0.22f ? 2 : 1;
            MahjongAssistInventoryService.Grant(booster, amount);

            return new MahjongFurnaceRewardResult
            {
                Tier = MahjongFurnaceRewardTier.Booster,
                Booster = booster,
                BoosterAmount = amount,
                Title = "Сила жерла",
                Description = "+" + amount + " " + BoosterLabel(booster)
            };
        }

        private static MahjongFurnaceRewardResult GrantRareTile(BattleTileRarity rarity)
        {
            MahjongFurnaceRewardResult result = new MahjongFurnaceRewardResult
            {
                Tier = rarity == BattleTileRarity.Mythic ? MahjongFurnaceRewardTier.Mythic : MahjongFurnaceRewardTier.Legendary,
                Title = rarity == BattleTileRarity.Mythic ? "MYTHIC!" : "LEGENDARY!",
                Description = rarity == BattleTileRarity.Mythic ? "Мифический камень пробужден." : "Легендарный камень пробужден."
            };

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>();
            BattleTileData tile = PickRewardTile(store, rarity);
            if (profile != null && store != null && tile != null && !string.IsNullOrWhiteSpace(tile.Id))
            {
                result.TileId = tile.Id;
                result.Description = string.IsNullOrWhiteSpace(tile.DisplayName) ? result.Description : tile.DisplayName;
                result.GrantedTileCopy = BattleTileInventoryService.GrantTileCopy(profile, store, tile.Id, out _);
                return result;
            }

            string key = rarity == BattleTileRarity.Mythic ? MythicShardKey : LegendaryShardKey;
            PlayerPrefs.SetInt(key, Mathf.Max(0, PlayerPrefs.GetInt(key, 0)) + 1);
            result.Description += " Сохранено в запас жерла.";
            return result;
        }

        private static BattleTileData PickRewardTile(BattleTileStore store, BattleTileRarity rarity)
        {
            IReadOnlyList<BattleTileData> tiles = store != null ? store.BattleTiles : null;
            if (tiles == null || tiles.Count == 0)
                return null;

            List<BattleTileData> candidates = new();
            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData tile = tiles[i];
                if (tile != null &&
                    tile.Prefab != null &&
                    tile.Rarity == rarity &&
                    !string.IsNullOrWhiteSpace(tile.Id))
                {
                    candidates.Add(tile);
                }
            }

            return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
        }

        private static string BoosterLabel(MahjongAssistBooster booster)
        {
            switch (booster)
            {
                case MahjongAssistBooster.Shuffle:
                    return "перемешивание";
                case MahjongAssistBooster.Undo:
                    return "отмена хода";
                case MahjongAssistBooster.HintPair:
                default:
                    return "подсказка пары";
            }
        }

        private static void SaveProfileIfReady()
        {
            if (ProfileService.I == null)
                return;

            ProfileService.I.Save();
            ProfileService.I.NotifyProfileChanged();
        }
    }
}
