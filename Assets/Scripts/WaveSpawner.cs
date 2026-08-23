using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Rates")]
    [SerializeField] private WaveScalingConfig Scaling;

    [Header("Timing")]
    [SerializeField] private float InitialDelay = 5f;          // before the first wave
    [SerializeField] private float SpawnInterval = 0.3f;        // time between each spawn
    [SerializeField] private float TimeBetweenWaves = 10f;      // after a wave is cleared, time before the next one

    public int CurrentWave {  get; private set; }
    public int ActiveEnemyCount { get; private set; }

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(InitialDelay);

        while (true)
        {
            ++CurrentWave;
            Debug.Log($"Wave {CurrentWave} is starting now!");

            yield return SpawnWave(CurrentWave);

            yield return new WaitUntil(() => ActiveEnemyCount <= 0);
            Debug.Log($"Wave {CurrentWave} cleared");

            yield return new WaitForSeconds(TimeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(int WaveNumber)
    {
        int count = Scaling.GetEnemyCount(WaveNumber);
        EnemyStats stats = Scaling.GetStatsForWave(WaveNumber);

        for (int i = 0; i < count; ++i)
        {
            SpawnEnemy(stats);
            yield return new WaitForSeconds(SpawnInterval);
        }
    }

    private void SpawnEnemy(EnemyStats stats)
    {
        if (!ProceduralFloor.Instance.TryGetRandomEdgeSpawnPosition(out Vector3 spawnPos))
        {
            Debug.LogWarning("WaveSpawner: Unlucky enemy, no valid edge spawn point found, skipping this enemy.");
            return;
        }

        PathFollower enemy = EnemyPool.Instance.Get(spawnPos);
        enemy.Initialize(stats);
        ++ActiveEnemyCount;

        Health health = enemy.GetComponent<Health>();
        System.Action onDeath = null;
        onDeath = () =>
        {
            health.OnDeath -= onDeath;
            --ActiveEnemyCount;
            EnemyPool.Instance.Release(enemy);
        };
        health.OnDeath += onDeath;
    }
}
