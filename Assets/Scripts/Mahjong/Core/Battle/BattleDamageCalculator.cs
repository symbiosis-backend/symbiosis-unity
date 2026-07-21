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
            public int AbsorbedDamage;
            public bool IsCritical;
            public bool IsParried;

            public DamageResult(int damage, bool crit)
                : this(damage, 0, crit)
            {
            }

            public DamageResult(int damage, int absorbedDamage, bool crit)
            {
                FinalDamage = damage;
                AbsorbedDamage = Mathf.Max(0, absorbedDamage);
                IsCritical = crit;
                IsParried = false;
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

        public DamageResult CalculateFromHub(float targetArmor)
        {
            if (statsHub == null)
                return new DamageResult(0, false);

            return Calculate(
                statsHub.Attack,
                statsHub.CritChance,
                statsHub.CritDamageMultiplier,
                targetArmor
            );
        }

        public DamageResult Calculate(
            int attack,
            float critChance,
            float critDamageMultiplier,
            float targetArmor)
        {
            attack = Mathf.Max(0, attack);
            critChance = Mathf.Clamp01(critChance);
            critDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
            targetArmor = Mathf.Clamp01(targetArmor);
            bool crit = Roll(critChance);
            float damage = attack;

            if (crit)
                damage *= critDamageMultiplier;

            float damageBeforeArmor = damage;
            damage *= (1f - targetArmor);

            int finalDamage = roundUp
                ? Mathf.CeilToInt(damage)
                : Mathf.RoundToInt(damage);

            if (finalDamage < minimumDamage && attack > 0)
                finalDamage = minimumDamage;

            int rawDamage = roundUp
                ? Mathf.CeilToInt(damageBeforeArmor)
                : Mathf.RoundToInt(damageBeforeArmor);
            int absorbedDamage = Mathf.Max(0, rawDamage - finalDamage);
            DamageResult result = new DamageResult(finalDamage, absorbedDamage, crit);

            DamageCalculated?.Invoke(result);
            return result;
        }

        public int CalculateDamageOnly(float targetArmor)
        {
            return CalculateFromHub(targetArmor).FinalDamage;
        }

        private bool Roll(float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;

            return UnityEngine.Random.value <= chance;
        }
    }
}
