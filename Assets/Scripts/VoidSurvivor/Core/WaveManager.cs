using System.Collections;
using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;

        private Coroutine routine;

        public bool IsRunning => routine != null;

        public void Bind(EnemySpawner enemySpawner)
        {
            spawner = enemySpawner;
        }

        public void PlayLevel(VoidLevelConfig level, System.Action completed)
        {
            Stop();
            routine = StartCoroutine(LevelRoutine(level, completed));
        }

        public void Stop()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private IEnumerator LevelRoutine(VoidLevelConfig level, System.Action completed)
        {
            if (level == null)
            {
                routine = null;
                completed?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(level.StartDelay);

            WaveDefinition[] waves = level.Waves;
            for (int i = 0; i < waves.Length; i++)
                yield return SpawnWave(waves[i]);

            while (FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude).Length > 0)
                yield return null;

            routine = null;
            completed?.Invoke();
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            if (wave == null)
                yield break;

            yield return new WaitForSeconds(wave.PreDelay);

            EnemySpawnEntry[] entries = wave.Enemies;
            for (int i = 0; i < entries.Length; i++)
            {
                EnemySpawnEntry entry = entries[i];
                if (entry == null || entry.EnemyConfig == null)
                    continue;

                for (int count = 0; count < entry.Count; count++)
                {
                    spawner.Spawn(entry.EnemyConfig, entry.SpeedMultiplier, entry.HealthMultiplier);
                    yield return new WaitForSeconds(wave.SpawnInterval);
                }
            }
        }
    }
}
