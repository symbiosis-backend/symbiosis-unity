using System;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleHealthSystem : MonoBehaviour
    {
        public static BattleHealthSystem Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Serializable]
        public struct HealthSnapshot
        {
            public int CurrentHp;
            public int MaxHp;
            public bool IsDead;

            public HealthSnapshot(int currentHp, int maxHp, bool isDead)
            {
                CurrentHp = Mathf.Max(0, currentHp);
                MaxHp = Mathf.Max(1, maxHp);
                IsDead = isDead;
            }
        }

        [Header("Links")]
        [SerializeField] private BattleStatsHub statsHub;

        [Header("Runtime State")]
        [SerializeField] private int currentHp;
        [SerializeField] private int maxHp;
        [SerializeField] private bool isDead;
        [SerializeField] private bool autoInitFromStatsHubOnStart = true;

        public event Action<HealthSnapshot> HealthChanged;
        public event Action<int> DamageTaken;
        public event Action<int> Healed;
        public event Action Died;
        public event Action Revived;
        public event Action Initialized;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public bool IsDead => isDead;
        public float NormalizedHp => maxHp <= 0 ? 0f : (float)currentHp / maxHp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SanitizeLocalState();
        }

        private void Start()
        {
            if (autoInitFromStatsHubOnStart)
                InitializeFromStatsHub();
            else
                PushState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnValidate()
        {
            SanitizeLocalState();
        }

        public void InitializeFromStatsHub(bool fullHeal = true)
        {
            if (statsHub == null)
            {
                Debug.LogWarning("[BattleHealthSystem] BattleStatsHub is not assigned.");
                return;
            }

            int targetMaxHp = ReadMaxHpFromHub();
            maxHp = Mathf.Max(1, targetMaxHp);

            if (fullHeal)
                currentHp = maxHp;
            else
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            isDead = currentHp <= 0;

            Initialized?.Invoke();
            PushState();
        }

        public void SetMaxHp(int value, bool clampCurrentHp = true, bool notify = true)
        {
            maxHp = Mathf.Max(1, value);

            if (clampCurrentHp)
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            isDead = currentHp <= 0;

            if (notify)
                PushState();
        }

        public void SetCurrentHp(int value, bool notify = true)
        {
            currentHp = Mathf.Clamp(value, 0, maxHp <= 0 ? 1 : maxHp);

            bool wasDead = isDead;
            isDead = currentHp <= 0;

            if (!wasDead && isDead)
                Died?.Invoke();
            else if (wasDead && !isDead)
                Revived?.Invoke();

            if (notify)
                PushState();
        }

        public void FullHeal(bool notify = true)
        {
            bool wasDead = isDead;

            currentHp = maxHp;
            isDead = false;

            if (wasDead)
                Revived?.Invoke();

            Healed?.Invoke(maxHp);

            if (notify)
                PushState();
        }

        public void Heal(int amount, bool notify = true)
        {
            if (amount <= 0 || maxHp <= 0)
                return;

            int oldHp = currentHp;
            currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);

            int healedAmount = currentHp - oldHp;
            if (healedAmount <= 0)
                return;

            bool wasDead = isDead;
            isDead = currentHp <= 0;

            Healed?.Invoke(healedAmount);

            if (wasDead && !isDead)
                Revived?.Invoke();

            if (notify)
                PushState();
        }

        public void TakeDamage(int amount, bool notify = true)
        {
            if (amount <= 0 || isDead)
                return;

            int oldHp = currentHp;
            currentHp = Mathf.Clamp(currentHp - amount, 0, maxHp);

            int taken = oldHp - currentHp;
            if (taken <= 0)
                return;

            DamageTaken?.Invoke(taken);

            bool justDied = !isDead && currentHp <= 0;
            isDead = currentHp <= 0;

            if (justDied)
                Died?.Invoke();

            if (notify)
                PushState();
        }

        public void ApplyDamageResult(BattleDamageCalculator.DamageResult result, bool notify = true)
        {
            if (result.IsParried)
                return;

            TakeDamage(result.FinalDamage, notify);
        }

        public HealthSnapshot GetSnapshot()
        {
            return new HealthSnapshot(currentHp, maxHp, isDead);
        }

        public void PushState()
        {
            HealthChanged?.Invoke(GetSnapshot());
        }

        private int ReadMaxHpFromHub()
        {
            if (statsHub == null)
                return 100;

            return statsHub.MaxHp;
        }

        private void SanitizeLocalState()
        {
            maxHp = Mathf.Max(1, maxHp);

            if (currentHp < 0)
                currentHp = 0;

            if (currentHp > maxHp)
                currentHp = maxHp;

            isDead = currentHp <= 0;
        }
    }
}