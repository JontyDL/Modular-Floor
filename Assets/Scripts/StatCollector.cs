using System;
using UnityEngine;

public struct GameStats
{
    public int AmountSpent { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int WavesSurvived { get; private set; }

    public GameStats(int GoldCount, int KillCount, int WaveCount)
    {
        AmountSpent = GoldCount;
        EnemiesKilled = KillCount;
        WavesSurvived = WaveCount;
    }
}

public class StatCollector : MonoBehaviour
{
    public static StatCollector Instance {  get; private set; }

    private int TotalGoldSpent;
    private int TotalEnemiesKilled;
    private int TotalWavesSurvived;
    private GameStats Final;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }

        TotalGoldSpent = 0;
        TotalEnemiesKilled = 0;
        TotalWavesSurvived = 0;
    }

    public void SpendGold(int amountSpent)
    {
        TotalGoldSpent += amountSpent;
    }

    public void EnemyKilled()
    {
        ++TotalEnemiesKilled;
    }

    public void ResetKillCount()    // the enemy pooling system is adding kills that shouldn't count
    {
        TotalEnemiesKilled = 0;
    }

    public void WaveCompleted(int Wave)
    {
        TotalWavesSurvived = Wave;
    }

    public GameStats GetFinalStats()
    {
        return Final = new GameStats(Math.Abs(TotalGoldSpent), TotalEnemiesKilled, TotalWavesSurvived);
    }
}
