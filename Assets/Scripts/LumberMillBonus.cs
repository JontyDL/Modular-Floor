using UnityEngine;

public class LumberMillBonus : MonoBehaviour
{
    public int BonusAmount = 1;

    private bool Registered = false;    // only true once Placed() has added our bonus to the manager

    public void Placed()
    {
        MoneyManager.Instance.AddEnemyBonus(BonusAmount);
        Registered = true;
    }

    // Used by the upgrade system: swaps the old bonus for the new one so the manager's total stays correct.
    public void SetBonus(int NewAmount)
    {
        NewAmount = Mathf.Max(0, NewAmount);
        if (NewAmount == BonusAmount) return;

        if (Registered)
        {
            MoneyManager.Instance.AddEnemyBonus(-BonusAmount);
            MoneyManager.Instance.AddEnemyBonus(NewAmount);
        }

        BonusAmount = NewAmount;
    }

    private void OnDestroy()
    {
        // Only undo the bonus if it was actually added (a ghost destroyed before being placed never added one).
        if (Registered && MoneyManager.Instance != null)
            MoneyManager.Instance.AddEnemyBonus(-BonusAmount);
    }
}
