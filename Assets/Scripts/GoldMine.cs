using System.Collections;
using UnityEngine;

public class GoldMine : MonoBehaviour
{
    [SerializeField] private float Delay = 2.0f;
    [SerializeField] private int GoldFound = 1;

    private WaitForSeconds wait;

    // Public accessors so the upgrade system can change these.
    public float MineDelay
    {
        get => Delay;
        set
        {
            Delay = Mathf.Max(0.05f, value);
            wait = new WaitForSeconds(Delay);   // the routine re-reads this field each cycle, so the new delay applies from the next tick
        }
    }

    public int GoldAmount
    {
        get => GoldFound;
        set => GoldFound = Mathf.Max(0, value);
    }

    public void Placed()
    {
        StartCoroutine(GoldMineRoutine());
    }

    private IEnumerator GoldMineRoutine()
    {
        wait = new WaitForSeconds(Delay);

        while (true)
        {
            yield return wait;
            MineGold();
        }
    }

    private void MineGold()
    {
        MoneyManager.Instance.TransactGold(GoldFound);
    }
}
