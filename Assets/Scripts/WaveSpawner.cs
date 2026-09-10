using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Rates")]
    [SerializeField] private WaveCountConfig CountScaling;

    [Header("Timing")]
    [SerializeField] private float InitialDelay = 5f;          // before the first wave
    [SerializeField] private float SpawnInterval = 0.3f;        // time between each spawn
    [SerializeField] private float TimeBetweenWaves = 10f;      // after a wave is cleared, time before the next one

    public int CurrentWave {  get; private set; }
    public int ActiveEnemyCount { get; private set; }

    private readonly List<EnemyDefinition> EligibleBuffer = new List<EnemyDefinition>();

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
            StatCollector.Instance.WaveCompleted(CurrentWave);

            yield return new WaitForSeconds(TimeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(int WaveNumber)
    {
        int count = CountScaling.GetEnemyCount(WaveNumber);

        for (int i = 0; i < count; ++i)
        {
            EnemyDefinition def = PickEnemyType(WaveNumber);
            if (def != null)
            {
                SpawnEnemy(def, WaveNumber);
            }

            yield return new WaitForSeconds(SpawnInterval);
        }
    }

    private EnemyDefinition PickEnemyType(int waveNumber)
    {
        IReadOnlyList<EnemyDefinition> all = EnemyPool.Instance.Definitions;

        EligibleBuffer.Clear();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].UnlockWave <= waveNumber)
                EligibleBuffer.Add(all[i]);
        }

        IReadOnlyList<EnemyDefinition> pool = EligibleBuffer.Count > 0 ? EligibleBuffer : all;
        if (pool.Count == 0)
        {
            Debug.LogError("WaveSpawner: no enemy definitions available to spawn.");
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += Mathf.Max(0f, pool[i].SpawnWeight);

        if (totalWeight <= 0f)
            return pool[Random.Range(0, pool.Count)];

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += Mathf.Max(0f, pool[i].SpawnWeight);
            if (roll <= cumulative)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    private void SpawnEnemy(EnemyDefinition definition, int waveNumber)
    {
        if (!ProceduralFloor.Instance.TryGetRandomEdgeSpawnPosition(out Vector3 spawnPos))
        {
            Debug.LogWarning("WaveSpawner: Unlucky enemy, no valid edge spawn point found, skipping this enemy.");
            return;
        }

        EnemyStats stats = definition.Scaling.GetStatsForWave(waveNumber);

        PathFollower enemy = EnemyPool.Instance.Get(definition.EnemyID, spawnPos);
        if (enemy == null)
            return;

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
