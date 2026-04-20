using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyBase : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject deathFxPrefab;

        private VoidEnemyConfig config;
        private EnemyHealth health;
        private ObjectPoolManager pool;
        private PooledObject pooledObject;
        private Transform target;
        private int scoreValue;

        public bool IsAlive => health != null && health.IsAlive;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            pooledObject = GetComponent<PooledObject>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(VoidEnemyConfig enemyConfig, Transform player, ObjectPoolManager objectPool, float speedMultiplier, float healthMultiplier)
        {
            config = enemyConfig;
            target = player;
            pool = objectPool;
            scoreValue = config != null ? config.ScoreValue : 10;

            if (spriteRenderer != null && config != null)
            {
                if (config.Sprite != null)
                    spriteRenderer.sprite = config.Sprite;

                spriteRenderer.color = config.Color;
                transform.localScale = config.Size;
            }

            health.Initialize((config != null ? config.MaxHealth : 10f) * Mathf.Max(0.1f, healthMultiplier));
            ApplyMovement(config != null ? config.MoveKind : EnemyMoveKind.Chase, speedMultiplier);
        }

        public void SetDeathFxPrefab(GameObject prefab)
        {
            deathFxPrefab = prefab;
        }

        public void Kill(bool awardScore)
        {
            if (awardScore && GameManager.I != null)
                GameManager.I.AddScore(scoreValue);

            if (deathFxPrefab != null)
            {
                if (pool != null)
                    pool.Spawn(deathFxPrefab, transform.position, Quaternion.identity);
                else
                    Instantiate(deathFxPrefab, transform.position, Quaternion.identity);
            }

            if (pooledObject == null)
                pooledObject = GetComponent<PooledObject>();

            if (pooledObject != null)
                pooledObject.Despawn();
            else
                Destroy(gameObject);
        }

        private void ApplyMovement(EnemyMoveKind kind, float speedMultiplier)
        {
            EnemyMoveBase[] existing = GetComponents<EnemyMoveBase>();
            for (int i = 0; i < existing.Length; i++)
            {
                existing[i].enabled = false;
                Destroy(existing[i]);
            }

            EnemyMoveBase movement = kind switch
            {
                EnemyMoveKind.Wave => gameObject.AddComponent<EnemyMoveWave>(),
                EnemyMoveKind.ZigZag => gameObject.AddComponent<EnemyMoveZigZag>(),
                EnemyMoveKind.Orbit => gameObject.AddComponent<EnemyMoveOrbit>(),
                EnemyMoveKind.Dash => gameObject.AddComponent<EnemyMoveDash>(),
                EnemyMoveKind.DelayThenChase => gameObject.AddComponent<EnemyMoveDelayThenChase>(),
                EnemyMoveKind.Down => gameObject.AddComponent<EnemyMoveDown>(),
                EnemyMoveKind.EdgeCircle => gameObject.AddComponent<EnemyMoveEdgeCircle>(),
                _ => gameObject.AddComponent<EnemyMoveChase>()
            };

            movement.Initialize(target, config, speedMultiplier);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || !playerHealth.IsAlive)
                return;

            float damage = config != null ? config.ContactDamage : 10f;
            playerHealth.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
            Kill(false);
        }
    }
}
