using UnityEngine;

public class LumberMillBonus : MonoBehaviour
{
    public int BonusAmount = 1;

    public void Placed()
    {
        MoneyManager.Instance.AddEnemyBonus(BonusAmount);
    }

    // this will have to be updated when building upgrading is added, as the logic falls apart, if bonus amount changes

    private void OnDestroy()        
    {
        MoneyManager.Instance.AddEnemyBonus(-BonusAmount);
    }
}
