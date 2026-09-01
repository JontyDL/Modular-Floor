using UnityEngine;

public class GiveMoneyOnDeath : MonoBehaviour
{
    [SerializeField] private int GoldAmount;
    [SerializeField] private bool IsEnemy;

    private void OnDisable()
    {
        try
        {               // adding a try catch, just incase scene switching and deletion order would call this/mess this up
            MoneyManager.Instance.TransactGold(GoldAmount, IsEnemy);
            StatCollector.Instance.EnemyKilled();       // this is a script that is only on the enemies, not on the buildings, and the enemy pooling can mess this number up
        }
        catch { }
    }
}
