using System.Collections.Generic;
using UnityEngine;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private UISlide Window;            // the child that slides on/off screen (leave it inactive in the scene)
    [SerializeField] private Transform RowContainer;    // child of Window with a VerticalLayoutGroup (+ ContentSizeFitter if you want the height to adapt)
    [SerializeField] private UpgradeRow RowPrefab;

    private readonly List<UpgradeRow> Rows = new List<UpgradeRow>();
    private UpgradeableObject Current;

    private void OnEnable()
    {
        UpgradeableObject.UpgradePanelRequested += ShowPanel;
        UpgradeableObject.UpgradeSessionEnded += HidePanel;
    }

    private void OnDisable()
    {
        UpgradeableObject.UpgradePanelRequested -= ShowPanel;
        UpgradeableObject.UpgradeSessionEnded -= HidePanel;

        if (Current != null)
            Current.Upgraded -= RefreshRows;
    }

    public void OnGoldChanged()
    {
        foreach (UpgradeRow Row in Rows)
            Row.RefreshAffordability();
    }

    // Hook this to the panel's close button
    public void Close()
    {
        UpgradeableObject.CloseCurrent();   // raises UpgradeSessionEnded, which calls HidePanel
    }

    private void ShowPanel(UpgradeableObject Building)
    {
        if (Current != null)
            Current.Upgraded -= RefreshRows;

        Current = Building;
        Current.Upgraded += RefreshRows;

        BuildRows();        // build before showing, so the slide-in already has the right content
        Window.Show();
    }

    private void HidePanel()
    {
        if (Current != null)
        {
            Current.Upgraded -= RefreshRows;
            Current = null;
        }

        // The building may be gone (destroyed), so don't let anyone click or refresh its rows while we slide out.
        foreach (UpgradeRow Row in Rows)
            Row.Lock();

        if (Window.gameObject.activeSelf)
            Window.Deactivate();
    }

    private void BuildRows()
    {
        foreach (UpgradeRow Row in Rows)
            Destroy(Row.gameObject);
        Rows.Clear();

        foreach (UpgradeOption Option in Current.Options)
        {
            UpgradeRow Row = Instantiate(RowPrefab, RowContainer);
            Row.Bind(Option, OnBuyClicked);
            Rows.Add(Row);
        }
    }

    private void OnBuyClicked(UpgradeOption Option)
    {
        if (Current != null)
            Current.Purchase(Option);   // raises Upgraded, which calls RefreshRows
    }

    private void RefreshRows()
    {
        foreach (UpgradeRow Row in Rows)
            Row.Refresh();
    }
}
