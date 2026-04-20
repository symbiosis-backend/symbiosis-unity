using UnityEngine;

namespace VoidSurvivor
{
    public sealed class PooledObject : MonoBehaviour
    {
        private ObjectPoolManager pool;
        private GameObject prefab;

        public void Bind(ObjectPoolManager owner, GameObject sourcePrefab)
        {
            pool = owner;
            prefab = sourcePrefab;
        }

        public void Despawn()
        {
            if (pool != null)
                pool.Despawn(gameObject, prefab);
            else
                Destroy(gameObject);
        }
    }
}
