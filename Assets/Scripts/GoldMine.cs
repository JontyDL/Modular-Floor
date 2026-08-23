using System.Collections;
using UnityEngine;

public class GoldMine : MonoBehaviour
{
    [SerializeField] private float Delay = 2.0f;
    [SerializeField] private int GoldFound = 1;

    public void Placed()
    {
        StartCoroutine(GoldMineRoutine());
    }

    private IEnumerator GoldMineRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Delay);

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
