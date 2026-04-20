using UnityEngine;

namespace VoidSurvivor
{
    [CreateAssetMenu(menuName = "Void Survivor/Enemy Config", fileName = "EnemyConfig")]
    public sealed class VoidEnemyConfig : ScriptableObject
    {
        [SerializeField] private string enemyId = "void_chaser";
        [SerializeField] private EnemyMoveKind moveKind = EnemyMoveKind.Chase;
        [SerializeField] private float maxHealth = 12f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = new Color(0.75f, 0.15f, 1f, 1f);
        [SerializeField] private Vector2 size = new Vector2(0.55f, 0.55f);
        [SerializeField] private float waveAmplitude = 1.1f;
        [SerializeField] private float waveFrequency = 3.2f;
        [SerializeField] private float dashInterval = 1.5f;
        [SerializeField] private float dashDuration = 0.22f;
        [SerializeField] private float dashMultiplier = 3.5f;
        [SerializeField] private float attackDelay = 0.85f;

        public string EnemyId => enemyId;
        public EnemyMoveKind MoveKind => moveKind;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public int ScoreValue => scoreValue;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public Vector2 Size => size;
        public float WaveAmplitude => waveAmplitude;
        public float WaveFrequency => waveFrequency;
        public float DashInterval => dashInterval;
        public float DashDuration => dashDuration;
        public float DashMultiplier => dashMultiplier;
        public float AttackDelay => attackDelay;

        public void SetRuntime(
            string id,
            EnemyMoveKind kind,
            float health,
            float speed,
            float damage,
            int score,
            Color tint,
            Vector2 scale)
        {
            enemyId = id;
            moveKind = kind;
            maxHealth = health;
            moveSpeed = speed;
            contactDamage = damage;
            scoreValue = score;
            color = tint;
            size = scale;
        }
    }
}
