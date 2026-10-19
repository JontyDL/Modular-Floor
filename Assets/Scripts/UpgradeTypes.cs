using UnityEngine;
using System;

[Serializable]
public class UpgradeStat
{
    [Tooltip("Added to the stat on each purchase. Use a negative number for stats where lower is better (e.g. gold mine delay).")]
    public float Step = 5f;
    public int BaseCost = 50;
    [Tooltip("Cost of the next purchase = BaseCost * CostGrowth ^ Level")]
    public float CostGrowth = 1.5f;
    public int MaxLevel = 5;

    [HideInInspector] public int Level;

    public int CurrentCost => Mathf.RoundToInt(BaseCost * Mathf.Pow(CostGrowth, Level));
    public bool IsMaxed => Level >= MaxLevel;
}

public class UpgradeOption
{
    public string Label { get; }

    private readonly UpgradeStat Stat;
    private readonly Func<string> CurrentText;
    private readonly Func<string> NextText;
    private readonly Action ApplyStep;

    public UpgradeOption(string Label, UpgradeStat Stat, Func<string> CurrentText, Func<string> NextText, Action ApplyStep)
    {
        this.Label = Label;
        this.Stat = Stat;
        this.CurrentText = CurrentText;
        this.NextText = NextText;
        this.ApplyStep = ApplyStep;
    }

    public string CurrentValue => CurrentText();
    public string NextValue => IsMaxed ? "MAX" : NextText();
    public int Cost => Stat.CurrentCost;
    public bool IsMaxed => Stat.IsMaxed;
    public bool CanAfford => !IsMaxed && MoneyManager.Instance.HasEnoughGold(Cost);

    // Charges the player and applies the upgrade. Returns false if maxed or the player can't afford it.
    public bool TryPurchase()
    {
        if (!CanAfford) return false;

        MoneyManager.Instance.TransactGold(-Cost);   // cost is read BEFORE the level goes up
        ApplyStep();
        Stat.Level++;
        return true;
    }
}
