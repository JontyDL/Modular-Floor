using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [SerializeField] private List<EnemyDefinition> EnemyDefinitions;

    public IReadOnlyList<EnemyDefinition> Definitions => EnemyDefinitions;

    private readonly Dictionary<string, EnemyDefinition> DefinitionLookup = new Dictionary<string, EnemyDefinition>();

    private readonly Dictionary<string, Stack<PathFollower>> Available = new Dictionary<string, Stack<PathFollower>>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
            return;
        }

        foreach (EnemyDefinition def in EnemyDefinitions)
        {
            if (def == null) continue;

            if (DefinitionLookup.ContainsKey(def.EnemyID))
            {
                Debug.Log($"duplicate IDs on {def.EnemyID}");
                continue;
            }

            DefinitionLookup.Add(def.EnemyID, def);

            Stack<PathFollower> stack = new Stack<PathFollower>();
            Available.Add(def.EnemyID, stack);

            for (int i = 0; i < def.PreWarmCount; i++)
            {
                stack.Push(CreateInstance(def));
            }

            StatCollector.Instance.ResetKillCount();
        }
    }
    
    private PathFollower CreateInstance(EnemyDefinition def)
    {
        PathFollower Ins = Instantiate(def.Prefab, transform);
        Ins.gameObject.SetActive(false);
        Ins.SetPoolId(def.EnemyID);
        return Ins;
    }

    public PathFollower Get(string enemyID, Vector3 pos)
    {
        if (!DefinitionLookup.TryGetValue(enemyID, out EnemyDefinition def))
        {
            return null;
        }

        Stack<PathFollower> stack = Available[enemyID];
        PathFollower enemy = stack.Count > 0 ? stack.Pop() : CreateInstance(def);

        enemy.ResetForPool();
        enemy.transform.SetPositionAndRotation(pos, Quaternion.identity);
        enemy.gameObject.SetActive(true);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(pos);        // we warp the agent so as to not mess with it's nav mesh calculations
        }

        enemy.Restart();

        return enemy;
    }

    public void Release(PathFollower enemy)
    {
        if (enemy == null)
            return;

        if (!Available.TryGetValue(enemy.EnemyId, out Stack<PathFollower> stack))
        {
            Debug.LogError($"EnemyPool: tried to release enemy with unknown EnemyId '{enemy.EnemyId}'. Destroying instead.");
            Destroy(enemy.gameObject);
            return;
        }

        enemy.gameObject.SetActive(false);
        enemy.transform.SetParent(transform);
        stack.Push(enemy);
    }
}
