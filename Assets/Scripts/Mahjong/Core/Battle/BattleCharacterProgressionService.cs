using System;
using UnityEngine;

namespace MahjongGame
{
    public static class BattleCharacterProgressionService
    {
        private const int MaxHpPerLevel = 8;
        private const int AttackPerLevel = 1;
        private const float ArmorPerLevel = 0.002f;
        private const float ParryPerLevel = 0.002f;
        private const float CritPerLevel = 0.002f;
        private const float CritDamagePerLevel = 0.01f;

        private const int MaxHpPerUpgrade = 18;
        private const int AttackPerUpgrade = 2;
        private const float ArmorPerUpgrade = 0.005f;
        private const float ParryPerUpgrade = 0.005f;
        private const float CritPerUpgrade = 0.005f;
        private const float CritDamagePerUpgrade = 0.025f;

        public static MahjongBattleCharacterProgressData GetOrCreateProgress(PlayerProfile profile, string characterId)
        {
            if (profile == null || string.IsNullOrWhiteSpace(characterId))
                return null;

            profile.EnsureData();
            profile.Mahjong.Battle.EnsureValid();

            string id = characterId.Trim();
            for (int i = 0; i < profile.Mahjong.Battle.CharacterProgression.Count; i++)
            {
                MahjongBattleCharacterProgressData item = profile.Mahjong.Battle.CharacterProgression[i];
                if (item != null && string.Equals(item.CharacterId, id, StringComparison.Ordinal))
                    return item;
            }

            MahjongBattleCharacterProgressData created = new MahjongBattleCharacterProgressData
            {
                CharacterId = id
            };
            profile.Mahjong.Battle.CharacterProgression.Add(created);
            return created;
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyProgression(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            MahjongBattleCharacterProgressData progress)
        {
            if (progress == null)
                return baseStats;

            progress.EnsureValid();
            int levelBonus = Mathf.Max(0, progress.Level - 1);
            return new BattleStatsHub.BattleStatsSnapshot(
                baseStats.MaxHp + levelBonus * MaxHpPerLevel + progress.MaxHpUpgrades * MaxHpPerUpgrade,
                baseStats.Attack + levelBonus * AttackPerLevel + progress.AttackUpgrades * AttackPerUpgrade,
                baseStats.Armor + levelBonus * ArmorPerLevel + progress.ArmorUpgrades * ArmorPerUpgrade,
                baseStats.ParryChance + levelBonus * ParryPerLevel + progress.ParryUpgrades * ParryPerUpgrade,
                baseStats.CritChance + levelBonus * CritPerLevel + progress.CritUpgrades * CritPerUpgrade,
                baseStats.CritDamageMultiplier + levelBonus * CritDamagePerLevel + progress.CritDamageUpgrades * CritDamagePerUpgrade);
        }

        public static BattleStatsHub.BattleStatsSnapshot GetProgressionBonus(MahjongBattleCharacterProgressData progress)
        {
            if (progress == null)
                return new BattleStatsHub.BattleStatsSnapshot(1, 0, 0f, 0f, 0f, 1f);

            progress.EnsureValid();
            int levelBonus = Mathf.Max(0, progress.Level - 1);
            return new BattleStatsHub.BattleStatsSnapshot(
                levelBonus * MaxHpPerLevel + progress.MaxHpUpgrades * MaxHpPerUpgrade,
                levelBonus * AttackPerLevel + progress.AttackUpgrades * AttackPerUpgrade,
                levelBonus * ArmorPerLevel + progress.ArmorUpgrades * ArmorPerUpgrade,
                levelBonus * ParryPerLevel + progress.ParryUpgrades * ParryPerUpgrade,
                levelBonus * CritPerLevel + progress.CritUpgrades * CritPerUpgrade,
                1f + levelBonus * CritDamagePerLevel + progress.CritDamageUpgrades * CritDamagePerUpgrade);
        }

        public static void AddExperience(MahjongBattleCharacterProgressData progress, int amount)
        {
            if (progress == null || amount <= 0)
                return;

            progress.EnsureValid();
            progress.Experience += amount;
            int required = GetExperienceRequiredForNextLevel(progress.Level);
            while (progress.Experience >= required)
            {
                progress.Experience -= required;
                progress.Level++;
                required = GetExperienceRequiredForNextLevel(progress.Level);
            }
        }

        public static int GetExperienceRequiredForNextLevel(int level)
        {
            return 120 + Mathf.Max(0, level - 1) * 60;
        }
    }
}
