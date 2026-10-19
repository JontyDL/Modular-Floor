using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeableObject : MonoBehaviour, IInteractable
{
    [Header("Upgrade tuning (set per building prefab - unused stats are simply ignored)")]
    [SerializeField] private UpgradeStat HealthUpgrade = new UpgradeStat { Step = 15f, BaseCost = 40, MaxLevel = 3 };
    [SerializeField] private UpgradeStat DamageUpgrade = new UpgradeStat { Step = 5f, BaseCost = 45, MaxLevel = 5 };
    [SerializeField] private UpgradeStat RangeUpgrade = new UpgradeStat { Step = 5f, BaseCost = 70, MaxLevel = 3 };
    [SerializeField] private UpgradeStat AttackRateUpgrade = new UpgradeStat { Step = 0.15f, BaseCost = 90, MaxLevel = 3 };
    [SerializeField] private UpgradeStat KnockbackUpgrade = new UpgradeStat { Step = 0.5f, BaseCost = 40, MaxLevel = 3 };
    [SerializeField] private UpgradeStat LumberBonusUpgrade = new UpgradeStat { Step = 2f, BaseCost = 80, MaxLevel = 3 };
    [SerializeField] private UpgradeStat GoldAmountUpgrade = new UpgradeStat { Step = 1f, BaseCost = 90, MaxLevel = 3 };
    [SerializeField] private UpgradeStat GoldDelayUpgrade = new UpgradeStat { Step = -0.2f, BaseCost = 80, MaxLevel = 4 };
    [SerializeField] private float MinGoldDelay = 0.3f;

    // ---- What the UI hooks into ----

    // Raised when a building is chosen for upgrading. The panel subscribes, shows itself and builds its rows from Options
    public static event Action<UpgradeableObject> UpgradePanelRequested;

    // Raised when the upgrade session ends (panel closed, another building chosen, or the building was destroyed). The panel should hide itself
    public static event Action UpgradeSessionEnded;

    // Raised on this building after a purchase so the panel can refresh values, costs and button states
    public event Action Upgraded;

    public static UpgradeableObject Current { get; private set; }

    public static void CloseCurrent()
    {
        if (Current == null) return;

        Current.EndSession();
        Current = null;
        UpgradeSessionEnded?.Invoke();
    }

    // ---- Components, found when they are needed so Awake/Start ordering doesn't matter ----

    private Health BuildingHealth;
    private Attacker BuildingAttacker;
    private Building BuildingScript;
    private GoldMine Mine;
    private LumberMillBonus Lumber;
    private List<UpgradeOption> options;

    public bool IsUpgradeable => !TryGetComponent<DeadManBomb>(out _) && !TryGetComponent<SpellAOE>(out _);

    public IReadOnlyList<UpgradeOption> Options
    {
        get
        {
            if (options == null) BuildOptions();
            return options;
        }
    }

    public void OnInteract(InteractionMode mode)
    {
        if (mode != InteractionMode.Upgrade) return;
        if (!IsUpgradeable) return;
        if (Current == this) return;

        // Only one building can be under consideration at a time.
        if (Current != null)
            Current.EndSession();

        Current = this;
        BeginSession();
        UpgradePanelRequested?.Invoke(this);
    }

    // Called by the panel's buttons. Returns true if the purchase went through.
    public bool Purchase(UpgradeOption Option)
    {
        if (Option == null || !Option.TryPurchase()) return false;

        Upgraded?.Invoke();
        return true;
    }

    private void BeginSession()
    {
        if (Options.Count > 0 && BuildingScript != null && BuildingScript.HasRange)
            BuildingScript.ShowRangeRing(true);
    }

    private void EndSession()
    {
        if (BuildingScript != null)
            BuildingScript.ShowRangeRing(false);
    }

    private void OnDestroy()
    {
        // If this building is the one on the panel (e.g. an enemy destroyed it), close the panel.
        if (Current == this)
        {
            Current = null;
            UpgradeSessionEnded?.Invoke();
        }
    }

    // ---- Building the option list ----

    private void BuildOptions()
    {
        options = new List<UpgradeOption>();

        TryGetComponent(out BuildingHealth);
        TryGetComponent(out BuildingAttacker);
        TryGetComponent(out BuildingScript);
        TryGetComponent(out Mine);
        TryGetComponent(out Lumber);

        if (BuildingHealth != null)
        {
            // SetMaxHealth refills to full by default, which is how your Health script describes upgrading.
            AddOption("Health", HealthUpgrade,
                () => BuildingHealth.MaxHealth,
                v => BuildingHealth.SetMaxHealth(v));
        }

        if (Lumber != null)
        {
            AddOption("Gold Per Kill", LumberBonusUpgrade,
                () => Lumber.BonusAmount,
                v => Lumber.SetBonus(Mathf.RoundToInt(v)));
        }
        else if (Mine != null)
        {
            AddOption("Gold Per Mine", GoldAmountUpgrade,
                () => Mine.GoldAmount,
                v => Mine.GoldAmount = Mathf.RoundToInt(v));

            AddOption("Mine Rate", GoldDelayUpgrade,
                () => Mine.MineDelay,
                v => Mine.MineDelay = v,
                MinGoldDelay);
        }
        else if (BuildingAttacker != null)
        {
            AddOption("Damage", DamageUpgrade,
                () => BuildingAttacker.Damage,
                v => BuildingAttacker.Damage = v);

            if (BuildingScript != null && BuildingScript.HasRange)
            {
                AddOption("Range", RangeUpgrade,
                    () => BuildingScript.Range,
                    v => BuildingScript.Range = v);
            }

            // The main tower only gets damage (and range) upgrades.
            if (BuildingScript == null || !BuildingScript.IsMainTower)
            {
                AddOption("Attack Rate", AttackRateUpgrade,
                    () => BuildingAttacker.Rate,
                    v => BuildingAttacker.Rate = v);

                AddOption("Knockback", KnockbackUpgrade,
                    () => BuildingAttacker.Knockback,
                    v => BuildingAttacker.Knockback = v);
            }
        }
    }

    private void AddOption(string Label, UpgradeStat Stat, Func<float> Get, Action<float> Set, float Min = 0f)
    {
        options.Add(new UpgradeOption(
            Label,
            Stat,
            () => Get().ToString("0.##"),
            () => Mathf.Max(Get() + Stat.Step, Min).ToString("0.##"),
            () => Set(Mathf.Max(Get() + Stat.Step, Min))));
    }
}
