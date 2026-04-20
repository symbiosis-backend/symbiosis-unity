using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleStatModifierSystem : MonoBehaviour
    {
        public static BattleStatModifierSystem Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        public enum StatType
        {
            MaxHp = 0,
            Attack = 1,
            Armor = 2,
            ParryChance = 3,
            CritChance = 4,
            CritDamageMultiplier = 5
        }

        public enum ModifierOperation
        {
            Add = 0,
            Multiply = 1,
            Override = 2
        }

        [Serializable]
        public struct StatModifier
        {
            public string Id;
            public StatType StatType;
            public ModifierOperation Operation;
            public float Value;
            public float Duration;
            public bool IsPermanent;

            [NonSerialized] public float TimeLeft;

            public void InitializeRuntime()
            {
                TimeLeft = Mathf.Max(0f, Duration);
            }

            public bool IsExpired => !IsPermanent && TimeLeft <= 0f;
        }

        [Serializable]
        public struct EffectiveStats
        {
            public int MaxHp;
            public int Attack;
            public float Armor;
            public float ParryChance;
            public float CritChance;
            public float CritDamageMultiplier;

            public EffectiveStats(
                int maxHp,
                int attack,
                float armor,
                float parryChance,
                float critChance,
                float critDamageMultiplier)
            {
                MaxHp = Mathf.Max(1, maxHp);
                Attack = Mathf.Max(0, attack);
                Armor = Mathf.Clamp01(armor);
                ParryChance = Mathf.Clamp01(parryChance);
                CritChance = Mathf.Clamp01(critChance);
                CritDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
            }
        }

        [Header("Links")]
        [SerializeField] private BattleStatsHub statsHub;

        [Header("Runtime")]
        [SerializeField] private bool autoPushOnStart = true;
        [SerializeField] private List<StatModifier> activeModifiers = new List<StatModifier>();

        public event Action<EffectiveStats> EffectiveStatsChanged;
        public event Action<StatModifier> ModifierAdded;
        public event Action<StatModifier> ModifierRemoved;
        public event Action ModifiersCleared;

        public IReadOnlyList<StatModifier> ActiveModifiers => activeModifiers;
        public int ActiveModifierCount => activeModifiers.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SanitizeAllModifiers();
        }

        private void Start()
        {
            if (autoPushOnStart)
                PushEffectiveStats();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (activeModifiers.Count == 0)
                return;

            bool changed = false;

            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                StatModifier modifier = activeModifiers[i];

                if (modifier.IsPermanent)
                    continue;

                modifier.TimeLeft -= Time.deltaTime;

                if (modifier.TimeLeft <= 0f)
                {
                    StatModifier expired = modifier;
                    activeModifiers.RemoveAt(i);
                    ModifierRemoved?.Invoke(expired);
                    changed = true;
                    continue;
                }

                activeModifiers[i] = modifier;
            }

            if (changed)
                PushEffectiveStats();
        }

        private void OnValidate()
        {
            SanitizeAllModifiers();
        }

        public EffectiveStats GetEffectiveStats()
        {
            if (statsHub == null)
                return new EffectiveStats(100, 10, 0f, 0f, 0.05f, 1.5f);

            float maxHpValue = statsHub.MaxHp;
            float attackValue = statsHub.Attack;
            float armorValue = statsHub.Armor;
            float parryValue = statsHub.ParryChance;
            float critChanceValue = statsHub.CritChance;
            float critDamageValue = statsHub.CritDamageMultiplier;

            bool hasOverrideMaxHp = false;
            bool hasOverrideAttack = false;
            bool hasOverrideArmor = false;
            bool hasOverrideParry = false;
            bool hasOverrideCritChance = false;
            bool hasOverrideCritDamage = false;

            float overrideMaxHp = maxHpValue;
            float overrideAttack = attackValue;
            float overrideArmor = armorValue;
            float overrideParry = parryValue;
            float overrideCritChance = critChanceValue;
            float overrideCritDamage = critDamageValue;

            float addMaxHp = 0f;
            float addAttack = 0f;
            float addArmor = 0f;
            float addParry = 0f;
            float addCritChance = 0f;
            float addCritDamage = 0f;

            float mulMaxHp = 1f;
            float mulAttack = 1f;
            float mulArmor = 1f;
            float mulParry = 1f;
            float mulCritChance = 1f;
            float mulCritDamage = 1f;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                StatModifier modifier = activeModifiers[i];

                switch (modifier.StatType)
                {
                    case StatType.MaxHp:
                        ApplyModifier(ref hasOverrideMaxHp, ref overrideMaxHp, ref addMaxHp, ref mulMaxHp, modifier);
                        break;

                    case StatType.Attack:
                        ApplyModifier(ref hasOverrideAttack, ref overrideAttack, ref addAttack, ref mulAttack, modifier);
                        break;

                    case StatType.Armor:
                        ApplyModifier(ref hasOverrideArmor, ref overrideArmor, ref addArmor, ref mulArmor, modifier);
                        break;

                    case StatType.ParryChance:
                        ApplyModifier(ref hasOverrideParry, ref overrideParry, ref addParry, ref mulParry, modifier);
                        break;

                    case StatType.CritChance:
                        ApplyModifier(ref hasOverrideCritChance, ref overrideCritChance, ref addCritChance, ref mulCritChance, modifier);
                        break;

                    case StatType.CritDamageMultiplier:
                        ApplyModifier(ref hasOverrideCritDamage, ref overrideCritDamage, ref addCritDamage, ref mulCritDamage, modifier);
                        break;
                }
            }

            maxHpValue = hasOverrideMaxHp ? overrideMaxHp : (maxHpValue + addMaxHp) * mulMaxHp;
            attackValue = hasOverrideAttack ? overrideAttack : (attackValue + addAttack) * mulAttack;
            armorValue = hasOverrideArmor ? overrideArmor : (armorValue + addArmor) * mulArmor;
            parryValue = hasOverrideParry ? overrideParry : (parryValue + addParry) * mulParry;
            critChanceValue = hasOverrideCritChance ? overrideCritChance : (critChanceValue + addCritChance) * mulCritChance;
            critDamageValue = hasOverrideCritDamage ? overrideCritDamage : (critDamageValue + addCritDamage) * mulCritDamage;

            return new EffectiveStats(
                Mathf.RoundToInt(maxHpValue),
                Mathf.RoundToInt(attackValue),
                Mathf.Clamp01(armorValue),
                Mathf.Clamp01(parryValue),
                Mathf.Clamp01(critChanceValue),
                Mathf.Max(1f, critDamageValue));
        }

        public void PushEffectiveStats()
        {
            EffectiveStatsChanged?.Invoke(GetEffectiveStats());
        }

        public string AddModifier(
            StatType statType,
            ModifierOperation operation,
            float value,
            float duration,
            bool isPermanent = false,
            string customId = null,
            bool notify = true)
        {
            StatModifier modifier = new StatModifier
            {
                Id = string.IsNullOrWhiteSpace(customId) ? Guid.NewGuid().ToString("N") : customId,
                StatType = statType,
                Operation = operation,
                Value = value,
                Duration = Mathf.Max(0f, duration),
                IsPermanent = isPermanent
            };

            modifier.InitializeRuntime();
            activeModifiers.Add(modifier);

            ModifierAdded?.Invoke(modifier);

            if (notify)
                PushEffectiveStats();

            return modifier.Id;
        }

        public string AddFlatAttack(int value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.Attack, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string AddFlatMaxHp(int value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.MaxHp, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string AddArmor(float value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.Armor, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string AddParryChance(float value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.ParryChance, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string AddCritChance(float value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.CritChance, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string AddCritDamageMultiplier(float value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.CritDamageMultiplier, ModifierOperation.Add, value, duration, isPermanent, customId, notify);
        }

        public string MultiplyAttack(float multiplier, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.Attack, ModifierOperation.Multiply, multiplier, duration, isPermanent, customId, notify);
        }

        public string MultiplyArmor(float multiplier, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.Armor, ModifierOperation.Multiply, multiplier, duration, isPermanent, customId, notify);
        }

        public string OverrideAttack(float value, float duration, bool isPermanent = false, string customId = null, bool notify = true)
        {
            return AddModifier(StatType.Attack, ModifierOperation.Override, value, duration, isPermanent, customId, notify);
        }

        public bool RemoveModifier(string modifierId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                return false;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                if (!string.Equals(activeModifiers[i].Id, modifierId, StringComparison.Ordinal))
                    continue;

                StatModifier removed = activeModifiers[i];
                activeModifiers.RemoveAt(i);
                ModifierRemoved?.Invoke(removed);

                if (notify)
                    PushEffectiveStats();

                return true;
            }

            return false;
        }

        public int RemoveModifiersByStat(StatType statType, bool notify = true)
        {
            int removedCount = 0;

            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].StatType != statType)
                    continue;

                StatModifier removed = activeModifiers[i];
                activeModifiers.RemoveAt(i);
                ModifierRemoved?.Invoke(removed);
                removedCount++;
            }

            if (removedCount > 0 && notify)
                PushEffectiveStats();

            return removedCount;
        }

        public void ClearAllModifiers(bool notify = true)
        {
            if (activeModifiers.Count == 0)
                return;

            activeModifiers.Clear();
            ModifiersCleared?.Invoke();

            if (notify)
                PushEffectiveStats();
        }

        public float GetTimeLeft(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                return -1f;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                if (!string.Equals(activeModifiers[i].Id, modifierId, StringComparison.Ordinal))
                    continue;

                return activeModifiers[i].IsPermanent ? float.PositiveInfinity : activeModifiers[i].TimeLeft;
            }

            return -1f;
        }

        public bool HasModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                return false;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                if (string.Equals(activeModifiers[i].Id, modifierId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void ApplyModifier(
            ref bool hasOverride,
            ref float overrideValue,
            ref float addValue,
            ref float multiplyValue,
            StatModifier modifier)
        {
            switch (modifier.Operation)
            {
                case ModifierOperation.Add:
                    addValue += modifier.Value;
                    break;

                case ModifierOperation.Multiply:
                    multiplyValue *= modifier.Value;
                    break;

                case ModifierOperation.Override:
                    hasOverride = true;
                    overrideValue = modifier.Value;
                    break;
            }
        }

        private void SanitizeAllModifiers()
        {
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                StatModifier modifier = activeModifiers[i];

                if (string.IsNullOrWhiteSpace(modifier.Id))
                    modifier.Id = Guid.NewGuid().ToString("N");

                modifier.Duration = Mathf.Max(0f, modifier.Duration);

                if (modifier.Operation == ModifierOperation.Multiply && modifier.Value < 0f)
                    modifier.Value = 0f;

                modifier.InitializeRuntime();
                activeModifiers[i] = modifier;
            }
        }
    }
}