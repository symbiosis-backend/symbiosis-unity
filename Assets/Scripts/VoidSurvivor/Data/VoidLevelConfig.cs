using System;
using UnityEngine;

namespace VoidSurvivor
{
    [CreateAssetMenu(menuName = "Void Survivor/Level Config", fileName = "LevelConfig")]
    public sealed class VoidLevelConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private WaveDefinition[] waves = Array.Empty<WaveDefinition>();

        public int LevelNumber => levelNumber;
        public float StartDelay => startDelay;
        public WaveDefinition[] Waves => waves;

        public void SetRuntime(int number, float delay, WaveDefinition[] levelWaves)
        {
            levelNumber = number;
            startDelay = delay;
            waves = levelWaves ?? Array.Empty<WaveDefinition>();
        }
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        [SerializeField] private string waveId = "Wave";
        [SerializeField] private float preDelay = 1f;
        [SerializeField] private float spawnInterval = 0.45f;
        [SerializeField] private EnemySpawnEntry[] enemies = Array.Empty<EnemySpawnEntry>();

        public string WaveId => waveId;
        public float PreDelay => preDelay;
        public float SpawnInterval => spawnInterval;
        public EnemySpawnEntry[] Enemies => enemies;

        public WaveDefinition(string id, float delay, float interval, EnemySpawnEntry[] entries)
        {
            waveId = id;
            preDelay = delay;
            spawnInterval = interval;
            enemies = entries ?? Array.Empty<EnemySpawnEntry>();
        }
    }

    [Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private VoidEnemyConfig enemyConfig;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float healthMultiplier = 1f;

        public VoidEnemyConfig EnemyConfig => enemyConfig;
        public int Count => count;
        public float SpeedMultiplier => speedMultiplier;
        public float HealthMultiplier => healthMultiplier;

        public EnemySpawnEntry(VoidEnemyConfig config, int amount, float speed, float health)
        {
            enemyConfig = config;
            count = Mathf.Max(1, amount);
            speedMultiplier = Mathf.Max(0.1f, speed);
            healthMultiplier = Mathf.Max(0.1f, health);
        }
    }
}
