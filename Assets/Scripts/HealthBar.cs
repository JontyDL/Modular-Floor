using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image FillBar;

    public void UpdateHealthBar(float MaxHealth, float NewHealth)
    {
        if (MaxHealth == 0) return;

        FillBar.fillAmount = NewHealth / MaxHealth;
    }
}
