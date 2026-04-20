using System;
using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
                Died?.Invoke();
        }
    }
}
