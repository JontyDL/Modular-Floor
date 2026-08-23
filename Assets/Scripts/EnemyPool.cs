using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [SerializeField] private PathFollower EnemyPrefab;
    [SerializeField] private int PreWarmCount;

    private readonly Stack<PathFollower> Available = new Stack<PathFollower>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }

        for (int i = 0; i < PreWarmCount; i++)
            Available.Push(CreateInstance());
    }
    
    private PathFollower CreateInstance()
    {
        PathFollower Ins = Instantiate(EnemyPrefab, transform);
        Ins.gameObject.SetActive(false);
        return Ins;
    }

    public PathFollower Get(Vector3 pos)
    {
        PathFollower enemy = Available.Count > 0 ? Available.Pop() : CreateInstance();

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

        enemy.gameObject.SetActive(false);
        enemy.transform.SetParent(transform);
        Available.Push(enemy);
    }
}
