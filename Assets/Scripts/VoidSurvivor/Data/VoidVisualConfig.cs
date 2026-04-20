using UnityEngine;

namespace VoidSurvivor
{
    [CreateAssetMenu(menuName = "Void Survivor/Visual Config", fileName = "VoidVisualConfig")]
    public sealed class VoidVisualConfig : ScriptableObject
    {
        [Header("Player")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Color playerColor = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField] private Vector2 playerSize = new Vector2(0.55f, 0.75f);

        [Header("Player Trail")]
        [SerializeField] private bool usePlayerTrail = true;
        [SerializeField] private float trailTime = 0.25f;
        [SerializeField] private float trailStartWidth = 0.28f;
        [SerializeField] private Color trailStartColor = new Color(0.25f, 0.8f, 1f, 0.75f);
        [SerializeField] private Color trailEndColor = new Color(0.25f, 0.8f, 1f, 0f);

        [Header("Bullet")]
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Color bulletColor = new Color(0.6f, 0.95f, 1f, 1f);
        [SerializeField] private Vector2 bulletSize = new Vector2(0.13f, 0.42f);

        [Header("Enemy Fallback")]
        [SerializeField] private Sprite enemyFallbackSprite;
        [SerializeField] private Color enemyFallbackColor = new Color(0.75f, 0.15f, 1f, 1f);
        [SerializeField] private Vector2 enemyFallbackSize = new Vector2(0.55f, 0.55f);

        [Header("Explosion")]
        [SerializeField] private Sprite explosionSprite;
        [SerializeField] private Color explosionColor = new Color(0.9f, 0.35f, 1f, 0.65f);
        [SerializeField] private Vector2 explosionSize = new Vector2(0.8f, 0.8f);
        [SerializeField] private float explosionLifetime = 0.35f;

        [Header("Camera")]
        [SerializeField] private Color backgroundColor = new Color(0.015f, 0.01f, 0.035f, 1f);
        [SerializeField] private float orthographicSize = 6f;

        public Sprite PlayerSprite => playerSprite;
        public Color PlayerColor => playerColor;
        public Vector2 PlayerSize => playerSize;
        public bool UsePlayerTrail => usePlayerTrail;
        public float TrailTime => trailTime;
        public float TrailStartWidth => trailStartWidth;
        public Color TrailStartColor => trailStartColor;
        public Color TrailEndColor => trailEndColor;
        public Sprite BulletSprite => bulletSprite;
        public Color BulletColor => bulletColor;
        public Vector2 BulletSize => bulletSize;
        public Sprite EnemyFallbackSprite => enemyFallbackSprite;
        public Color EnemyFallbackColor => enemyFallbackColor;
        public Vector2 EnemyFallbackSize => enemyFallbackSize;
        public Sprite ExplosionSprite => explosionSprite;
        public Color ExplosionColor => explosionColor;
        public Vector2 ExplosionSize => explosionSize;
        public float ExplosionLifetime => Mathf.Max(0.05f, explosionLifetime);
        public Color BackgroundColor => backgroundColor;
        public float OrthographicSize => Mathf.Max(2f, orthographicSize);
    }
}
