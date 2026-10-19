using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeRow : MonoBehaviour
{
    [SerializeField] private TMP_Text LabelText;
    [SerializeField] private TMP_Text ValueText;     // "10 -> 15"
    [SerializeField] private TMP_Text CostText;
    [SerializeField] private Button BuyButton;

    private UpgradeOption Option;

    public void Bind(UpgradeOption NewOption, Action<UpgradeOption> OnBuy)
    {
        Option = NewOption;

        BuyButton.onClick.RemoveAllListeners();
        BuyButton.onClick.AddListener(() => OnBuy(Option));

        Refresh();
    }

    // Updates text and button state. Call after a purchase
    public void Refresh()
    {
        LabelText.text = Option.Label;

        if (Option.IsMaxed)
        {
            ValueText.text = $"{Option.CurrentValue} (MAX)";
            CostText.text = "";
        }
        else
        {
            ValueText.text = $"{Option.CurrentValue} -> {Option.NextValue}";
            CostText.text = Option.Cost.ToString() + "G";
        }

        RefreshAffordability();
    }

    // Cheap check (no text changes) so the button greys out as the player's gold changes
    public void RefreshAffordability()
    {
        if (Option.CanAfford)
        {
            CostText.color = Color.yellow;
        } else
        {
            CostText.color = Color.red;
        }

        BuyButton.interactable = Option.CanAfford;
    }

    // Stops the row being clicked while the panel slides away.
    public void Lock()
    {
        BuyButton.interactable = false;
    }
}
