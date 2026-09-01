using UnityEngine;

public class BuildingSwapper : MonoBehaviour
{
    public void SwitchToTower()
    {
        BuildingGridSystem.Instance.SwitchBuilding(BuildingGridSystem.BuildingType.Tower);
    }

    public void SwitchToMine()
    {
        BuildingGridSystem.Instance.SwitchBuilding(BuildingGridSystem.BuildingType.Mine);
    }

    public void SwitchToMill()
    {
        BuildingGridSystem.Instance.SwitchBuilding(BuildingGridSystem.BuildingType.Mill);
    }
}
