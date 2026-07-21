using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongGame
{
    public enum BattleTileRarity
    {
        Standard = 0,
        Common = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    [System.Serializable]
    public sealed class BattleTileBonusData
    {
        public int MaxHp;
        public int Attack;
        [Range(0f, 1f)] public float Armor;
        [Range(0f, 1f)] public float ParryChance;
        [Range(0f, 1f)] public float CritChance;
        [Min(1f)] public float CritDamageMultiplier = 1f;

        public bool HasAnyBonus()
        {
            return MaxHp > 0
                   || Attack > 0
                   || Armor > 0f
                   || ParryChance > 0f
                   || CritChance > 0f
                   || CritDamageMultiplier > 1f;
        }
    }

    [System.Serializable]
    public sealed class BattleTileActiveBonusData
    {
        public int Attack;
        [Range(0f, 1f)] public float CritChance;
        [Min(1f)] public float CritDamageMultiplier = 1f;
        public int HealSelf;

        public bool HasAnyBonus()
        {
            return Attack > 0
                   || CritChance > 0f
                   || CritDamageMultiplier > 1f
                   || HealSelf > 0;
        }
    }

    [System.Serializable]
    public sealed class BattleTileSkillData
    {
        public string Name;
        [TextArea(2, 5)] public string Description;

        public bool HasSkill()
        {
            return !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Description);
        }
    }

    [Serializable]
    public sealed class BattleTileData
    {
        public string Id;
        public string DisplayName;
        public BattleTile Prefab;
        public BattleTileRarity Rarity = BattleTileRarity.Standard;
        public bool IsDonate;
        public bool IsFree = true;
        [TextArea(2, 8)] public string Description;
        [FormerlySerializedAs("Bonus")] public BattleTileBonusData PassiveBonus = new();
        public BattleTileActiveBonusData ActiveBonus = new();
        public List<BattleCharacterDatabase.CharacterAnimalType> SymbiosisAnimalTypes = new();
        public BattleTileBonusData SymbiosisBonus = new();
        public BattleTileSkillData Skill = new();
    }
}
