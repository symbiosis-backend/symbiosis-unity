using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor
{
    public sealed class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools.Add(prefab, pool);
            }

            GameObject instance = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
            PooledObject pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
                pooledObject = instance.AddComponent<PooledObject>();

            pooledObject.Bind(this, prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Despawn(GameObject instance, GameObject prefab)
        {
            if (instance == null)
                return;

            instance.SetActive(false);

            if (prefab == null)
                return;

            if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools.Add(prefab, pool);
            }

            pool.Enqueue(instance);
        }
    }

}
