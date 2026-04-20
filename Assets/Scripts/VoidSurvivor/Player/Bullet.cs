using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.2f;
        [SerializeField] private LayerMask hitMask = ~0;

        private Vector2 direction;
        private float speed;
        private float damage;
        private float timer;
        private GameObject owner;
        private PooledObject pooledObject;

        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
        }

        public void Launch(Vector2 shotDirection, float shotSpeed, float shotDamage, GameObject source)
        {
            direction = shotDirection.normalized;
            speed = shotSpeed;
            damage = shotDamage;
            owner = source;
            timer = lifetime;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            timer -= Time.deltaTime;
            if (timer <= 0f)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & hitMask.value) == 0)
                return;

            if (other.gameObject == owner)
                return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return;

            damageable.TakeDamage(new DamageInfo(damage, transform.position, owner));
            Despawn();
        }

        private void Despawn()
        {
            if (pooledObject == null)
                pooledObject = GetComponent<PooledObject>();

            if (pooledObject != null)
                pooledObject.Despawn();
            else
                Destroy(gameObject);
        }
    }
}
