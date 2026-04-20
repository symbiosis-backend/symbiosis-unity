using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform player;
        [SerializeField] private ObjectPoolManager pool;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnMargin = 1.5f;

        public void Bind(Camera cameraRef, Transform playerRef, ObjectPoolManager poolRef, GameObject prefab)
        {
            worldCamera = cameraRef;
            player = playerRef;
            pool = poolRef;
            enemyPrefab = prefab;
        }

        public EnemyBase Spawn(VoidEnemyConfig config, float speedMultiplier, float healthMultiplier)
        {
            if (enemyPrefab == null)
                return null;

            Vector3 position = GetSpawnPosition();
            GameObject enemyObject = pool != null
                ? pool.Spawn(enemyPrefab, position, Quaternion.identity)
                : Instantiate(enemyPrefab, position, Quaternion.identity);

            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy != null)
                enemy.Initialize(config, player, pool, speedMultiplier, healthMultiplier);

            return enemy;
        }

        private Vector3 GetSpawnPosition()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return Random.insideUnitCircle.normalized * 9f;

            float halfHeight = worldCamera.orthographicSize;
            float halfWidth = halfHeight * worldCamera.aspect;
            Vector3 center = worldCamera.transform.position;
            int side = Random.Range(0, 4);

            return side switch
            {
                0 => new Vector3(center.x - halfWidth - spawnMargin, Random.Range(center.y - halfHeight, center.y + halfHeight), 0f),
                1 => new Vector3(center.x + halfWidth + spawnMargin, Random.Range(center.y - halfHeight, center.y + halfHeight), 0f),
                2 => new Vector3(Random.Range(center.x - halfWidth, center.x + halfWidth), center.y + halfHeight + spawnMargin, 0f),
                _ => new Vector3(Random.Range(center.x - halfWidth, center.x + halfWidth), center.y - halfHeight - spawnMargin, 0f)
            };
        }
    }
}
