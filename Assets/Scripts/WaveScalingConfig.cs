using System;
using UnityEngine;

[Serializable]
public class WaveScalingConfig
{
    [Header("Enemy Count")]
    [SerializeField] private int BaseEnemyCount = 5;
    [SerializeField] private float EnemyCountGrowthPerWave = 1.5f;
    [SerializeField] private int MaxEnemyCount = 75;

    [Header("Health")]
    [SerializeField] private float BaseHealth = 20f;
    [SerializeField] private float HealthGrowthPerWave = 4f;
    [SerializeField] private float MaxHealth = 500f;

    [Header("Attack Damage")]
    [SerializeField] private float BaseDamage = 5f;
    [SerializeField] private float DamageGrowthPerWave = 0.35f;
    [SerializeField] private float MaxDamage = 40f;

    [Header("Move Speed")]
    [SerializeField] private float BaseSpeed = 3.5f;
    [SerializeField] private float SpeedGrowthPerWave = 0.05f;
    [SerializeField] private float MaxSpeed = 7f;

    [Header("Attack Rate")]
    [SerializeField] private float BaseAttackRate = 1f;
    [SerializeField] private float AttackRateGrowthPerWave = 0.02f;
    [SerializeField] private float MaxAttackRate = 2f;

    public int GetEnemyCount(int waveNumber)
    {
        int w = Mathf.Max(0, waveNumber - 1);
        float raw = BaseEnemyCount + w * EnemyCountGrowthPerWave;
        return Mathf.Clamp(Mathf.RoundToInt(raw), BaseEnemyCount, MaxEnemyCount);
    }

    public EnemyStats GetStatsForWave(int waveNumber)
    {
        int w = Mathf.Max(0, waveNumber - 1);

        return new EnemyStats
        {
            MaxHealth = Mathf.Clamp(BaseHealth + w * HealthGrowthPerWave, BaseHealth, MaxHealth),
            MoveSpeed = Mathf.Clamp(BaseSpeed + w * SpeedGrowthPerWave, BaseSpeed, MaxSpeed),
            AttackDamage = Mathf.Clamp(BaseDamage + w * DamageGrowthPerWave, BaseDamage, MaxDamage),
            AttackRate = Mathf.Clamp(BaseAttackRate + w * AttackRateGrowthPerWave, BaseAttackRate, MaxAttackRate)
        };
    }
}
