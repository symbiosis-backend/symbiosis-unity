using System;
using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        private float maxHealth;
        private float currentHealth;
        private EnemyBase enemy;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        public bool IsAlive => currentHealth > 0f;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
        }

        public void Initialize(float health)
        {
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            Damaged?.Invoke(damage);

            if (currentHealth <= 0f)
            {
                Died?.Invoke(damage);
                if (enemy != null)
                    enemy.Kill(true);
            }
        }
    }
}
