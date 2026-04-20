using System;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleStatsHub : MonoBehaviour
    {
        public static BattleStatsHub Instance { get; private set; }

        public static bool HasInstance => Instance != null;

        [Serializable]
        public struct BattleStatsSnapshot
        {
            public int MaxHp;
            public int Attack;
            public float Armor;
            public float ParryChance;
            public float CritChance;
            public float CritDamageMultiplier;

            public BattleStatsSnapshot(
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

        [Header("Base Battle Stats")]
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int attack = 10;
        [SerializeField, Range(0f, 1f)] private float armor = 0f;
        [SerializeField, Range(0f, 1f)] private float parryChance = 0f;
        [SerializeField, Range(0f, 1f)] private float critChance = 0.05f;
        [SerializeField, Min(1f)] private float critDamageMultiplier = 1.5f;

        public event Action<BattleStatsSnapshot> SnapshotChanged;
        public event Action<int> MaxHpChanged;
        public event Action<int> AttackChanged;
        public event Action<float> ArmorChanged;
        public event Action<float> ParryChanceChanged;
        public event Action<float> CritChanceChanged;
        public event Action<float> CritDamageMultiplierChanged;

        public int MaxHp => maxHp;
        public int Attack => attack;
        public float Armor => armor;
        public float ParryChance => parryChance;
        public float CritChance => critChance;
        public float CritDamageMultiplier => critDamageMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
            Sanitize();
        }

        private void OnValidate()
        {
            Sanitize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public BattleStatsSnapshot GetSnapshot()
        {
            return new BattleStatsSnapshot(
                maxHp,
                attack,
                armor,
                parryChance,
                critChance,
                critDamageMultiplier);
        }

        public void PushSnapshot()
        {
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetAll(BattleStatsSnapshot snapshot, bool notify = true)
        {
            maxHp = Mathf.Max(1, snapshot.MaxHp);
            attack = Mathf.Max(0, snapshot.Attack);
            armor = Mathf.Clamp01(snapshot.Armor);
            parryChance = Mathf.Clamp01(snapshot.ParryChance);
            critChance = Mathf.Clamp01(snapshot.CritChance);
            critDamageMultiplier = Mathf.Max(1f, snapshot.CritDamageMultiplier);

            if (!notify)
                return;

            NotifyAll();
        }

        public void SetMaxHp(int value, bool notify = true)
        {
            int next = Mathf.Max(1, value);
            if (maxHp == next)
                return;

            maxHp = next;

            if (!notify)
                return;

            MaxHpChanged?.Invoke(maxHp);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetAttack(int value, bool notify = true)
        {
            int next = Mathf.Max(0, value);
            if (attack == next)
                return;

            attack = next;

            if (!notify)
                return;

            AttackChanged?.Invoke(attack);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetArmor(float value, bool notify = true)
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(armor, next))
                return;

            armor = next;

            if (!notify)
                return;

            ArmorChanged?.Invoke(armor);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetParryChance(float value, bool notify = true)
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(parryChance, next))
                return;

            parryChance = next;

            if (!notify)
                return;

            ParryChanceChanged?.Invoke(parryChance);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetCritChance(float value, bool notify = true)
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(critChance, next))
                return;

            critChance = next;

            if (!notify)
                return;

            CritChanceChanged?.Invoke(critChance);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void SetCritDamageMultiplier(float value, bool notify = true)
        {
            float next = Mathf.Max(1f, value);
            if (Mathf.Approximately(critDamageMultiplier, next))
                return;

            critDamageMultiplier = next;

            if (!notify)
                return;

            CritDamageMultiplierChanged?.Invoke(critDamageMultiplier);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void AddMaxHp(int value, bool notify = true)
        {
            SetMaxHp(maxHp + value, notify);
        }

        public void AddAttack(int value, bool notify = true)
        {
            SetAttack(attack + value, notify);
        }

        public void AddArmor(float value, bool notify = true)
        {
            SetArmor(armor + value, notify);
        }

        public void AddParryChance(float value, bool notify = true)
        {
            SetParryChance(parryChance + value, notify);
        }

        public void AddCritChance(float value, bool notify = true)
        {
            SetCritChance(critChance + value, notify);
        }

        public void AddCritDamageMultiplier(float value, bool notify = true)
        {
            SetCritDamageMultiplier(critDamageMultiplier + value, notify);
        }

        public void ResetToDefaults(
            int defaultHp = 100,
            int defaultAttack = 10,
            float defaultArmor = 0f,
            float defaultParryChance = 0f,
            float defaultCritChance = 0.05f,
            float defaultCritDamageMultiplier = 1.5f,
            bool notify = true)
        {
            SetAll(new BattleStatsSnapshot(
                defaultHp,
                defaultAttack,
                defaultArmor,
                defaultParryChance,
                defaultCritChance,
                defaultCritDamageMultiplier), notify);
        }

        private void NotifyAll()
        {
            MaxHpChanged?.Invoke(maxHp);
            AttackChanged?.Invoke(attack);
            ArmorChanged?.Invoke(armor);
            ParryChanceChanged?.Invoke(parryChance);
            CritChanceChanged?.Invoke(critChance);
            CritDamageMultiplierChanged?.Invoke(critDamageMultiplier);
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        private void Sanitize()
        {
            maxHp = Mathf.Max(1, maxHp);
            attack = Mathf.Max(0, attack);
            armor = Mathf.Clamp01(armor);
            parryChance = Mathf.Clamp01(parryChance);
            critChance = Mathf.Clamp01(critChance);
            critDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
        }
    }
}
