using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ObjectPoolManager pool;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireInterval = 0.16f;
        [SerializeField] private float targetRange = 14f;
        [SerializeField] private float bulletDamage = 6f;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private bool autoFire = true;

        private float timer;

        private void Awake()
        {
            if (pool == null)
                pool = FindAnyObjectByType<ObjectPoolManager>();

            if (firePoint == null)
                firePoint = transform;
        }

        private void Update()
        {
            if (!autoFire)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            EnemyHealth target = FindNearestEnemy();
            if (target == null)
                return;

            FireAt(target.transform.position);
            timer = fireInterval;
        }

        public void FireAt(Vector2 targetPosition)
        {
            if (bulletPrefab == null)
                return;

            Vector2 origin = firePoint.position;
            Vector2 direction = (targetPosition - origin).normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;

            GameObject bulletObject = pool != null
                ? pool.Spawn(bulletPrefab, origin, Quaternion.identity)
                : Instantiate(bulletPrefab, origin, Quaternion.identity);

            Bullet bullet = bulletObject.GetComponent<Bullet>();
            if (bullet != null)
                bullet.Launch(direction, bulletSpeed, bulletDamage, gameObject);
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
            EnemyHealth nearest = null;
            float bestDistance = targetRange * targetRange;
            Vector2 origin = transform.position;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || !enemies[i].IsAlive)
                    continue;

                float distance = ((Vector2)enemies[i].transform.position - origin).sqrMagnitude;
                if (distance > bestDistance)
                    continue;

                bestDistance = distance;
                nearest = enemies[i];
            }

            return nearest;
        }
    }
}
