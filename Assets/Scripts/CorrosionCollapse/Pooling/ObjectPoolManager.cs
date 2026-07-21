using System.Collections.Generic;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Pooling
{
    public sealed class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        public void RegisterPool(string key, GameObject prefab, int count, Transform parent)
        {
            if (pools.ContainsKey(key))
            {
                return;
            }

            prefabs[key] = prefab;
            pools[key] = new Queue<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                GameObject item = Instantiate(prefab, parent);
                item.name = $"{key}_{i:00}";
                item.SetActive(false);
                var pooled = item.GetComponent<PooledObject>() ?? item.AddComponent<PooledObject>();
                pooled.poolKey = key;
                pools[key].Enqueue(item);
            }
        }

        public GameObject Get(string key, Transform parent)
        {
            if (!pools.TryGetValue(key, out Queue<GameObject> queue))
            {
                Debug.LogWarning($"Pool not registered: {key}");
                return null;
            }

            if (queue.Count == 0)
            {
                Debug.LogWarning($"Pool exhausted: {key}");
                return null;
            }

            GameObject item = queue.Dequeue();
            item.transform.SetParent(parent, false);
            item.SetActive(true);
            return item;
        }

        public void Release(GameObject item)
        {
            if (item == null || !item.TryGetComponent(out PooledObject pooled) || !pools.ContainsKey(pooled.poolKey))
            {
                return;
            }

            item.SetActive(false);
            pools[pooled.poolKey].Enqueue(item);
        }

        public GameObject GetRegisteredPrefab(string key)
        {
            return prefabs.TryGetValue(key, out GameObject prefab) ? prefab : null;
        }
    }
}
