using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class VoidAutoDespawn : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.35f;

        private float timer;
        private PooledObject pooledObject;

        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
        }

        private void OnEnable()
        {
            timer = lifetime;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            if (pooledObject != null)
                pooledObject.Despawn();
            else
                Destroy(gameObject);
        }

        public void SetLifetime(float seconds)
        {
            lifetime = Mathf.Max(0.05f, seconds);
            timer = lifetime;
        }
    }
}
