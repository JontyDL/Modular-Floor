using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance {  get; private set; }

    [SerializeField] private TextMeshProUGUI GoldDisplaytxt;

    [SerializeField] private Int64 Gold = 0;

    [SerializeField] private int EnemyBonus = 0;          // a bonus amount of gold given to the player for every enemy killed, determined by how many lumber mills there are on the map

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateGold();
    }

    public void AddEnemyBonus(int Amount)
    {
        EnemyBonus += Amount;
    }

    public bool HasEnoughGold(int Cost)
    {
        return Gold >= Cost;
    }

    public void TransactGold(int Amount, bool IsEnemy = false)
    {
        if (IsEnemy)
        {
            Gold += Amount + EnemyBonus;
        } else
        {
            Gold += Amount;
        }
        
        UpdateGold();
    }
    
    private void UpdateGold()
    {
        GoldDisplaytxt.text = Gold.ToString("N0", CultureInfo.InvariantCulture);
    }
}
