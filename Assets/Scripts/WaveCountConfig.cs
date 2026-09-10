using System;
using UnityEngine;

[Serializable]
public class WaveCountConfig
{
    [SerializeField] private int BaseEnemyCount;
    [SerializeField] private float EnemyCountGrowthPerWave;
    [SerializeField] private int MaxEnemyCount;

    public int GetEnemyCount(int WaveNumber)
    {
        int W = Mathf.Max(0, WaveNumber - 1);
        float Raw = BaseEnemyCount + W * EnemyCountGrowthPerWave;
        return Mathf.Clamp(Mathf.RoundToInt(Raw), BaseEnemyCount, MaxEnemyCount);
    }
}
