using System.Globalization;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Enemies/Enemy Definition")]
public class EnemyDefinition: ScriptableObject
{
    [SerializeField] private string enemyID = "enemy";
    public string EnemyID => enemyID;

    [SerializeField] private PathFollower prefab;
    public PathFollower Prefab => prefab;

    [SerializeField] private WaveScalingConfig scaling;
    public WaveScalingConfig Scaling => scaling;

    [SerializeField] private float spawnWeight;
    public float SpawnWeight => spawnWeight;

    [SerializeField] private int unlockWave;        // this enemy cannot be spawned before this wave has been reached
    public int UnlockWave => unlockWave;

    [SerializeField] private int preWarmCount;
    public int PreWarmCount => preWarmCount;
}
