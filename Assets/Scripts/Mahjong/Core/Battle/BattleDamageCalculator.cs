using System;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleDamageCalculator : MonoBehaviour
    {
        public static BattleDamageCalculator Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Serializable]
        public struct DamageResult
        {
            public int FinalDamage;
            public bool IsCritical;
            public bool IsParried;

            public DamageResult(int damage, bool crit, bool parry)
            {
                FinalDamage = damage;
                IsCritical = crit;
                IsParried = parry;
            }
        }

        public event Action<DamageResult> DamageCalculated;

        [SerializeField] private BattleStatsHub statsHub;
        [SerializeField] private bool roundUp = true;
        [SerializeField] private int minimumDamage = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);

            if (statsHub == null)
                statsHub = FindAnyObjectByType<BattleStatsHub>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public DamageResult CalculateFromHub(float targetArmor, float targetParryChance)
        {
            if (statsHub == null)
                return new DamageResult(0, false, false);

            return Calculate(
                statsHub.Attack,
                statsHub.CritChance,
                statsHub.CritDamageMultiplier,
                targetArmor,
                targetParryChance
            );
        }

        public DamageResult Calculate(
            int attack,
            float critChance,
            float critDamageMultiplier,
            float targetArmor,
            float targetParryChance)
        {
            attack = Mathf.Max(0, attack);
            critChance = Mathf.Clamp01(critChance);
            critDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
            targetArmor = Mathf.Clamp01(targetArmor);
            targetParryChance = Mathf.Clamp01(targetParryChance);

            bool parried = Roll(targetParryChance);

            if (parried)
            {
                DamageResult parryResult = new DamageResult(0, false, true);
                DamageCalculated?.Invoke(parryResult);
                return parryResult;
            }

            bool crit = Roll(critChance);

            float damage = attack;

            if (crit)
                damage *= critDamageMultiplier;

            damage *= (1f - targetArmor);

            int finalDamage = roundUp
                ? Mathf.CeilToInt(damage)
                : Mathf.RoundToInt(damage);

            if (finalDamage < minimumDamage && attack > 0)
                finalDamage = minimumDamage;

            DamageResult result = new DamageResult(finalDamage, crit, false);

            DamageCalculated?.Invoke(result);
            return result;
        }

        public int CalculateDamageOnly(float targetArmor, float targetParryChance)
        {
            return CalculateFromHub(targetArmor, targetParryChance).FinalDamage;
        }

        private bool Roll(float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;

            return UnityEngine.Random.value <= chance;
        }
    }
}
