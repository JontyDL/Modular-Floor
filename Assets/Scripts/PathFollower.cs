using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]
public class PathFollower : MonoBehaviour
{
    public enum State
    {
        SeekingLine,                // walking towards the closest path
        FollowingLine,              // following the path to the destination
        MovingToDestination,        // finished following the path, walking to the destination
        Arrived,                    // at the destination, can start attacking the tower
        AttackingBuilding           // diverted off the path to deal with a building in range
    }

    [Header("Line Following")]
    [SerializeField] private float LookAhead;
    [SerializeField] private float WanderAmplitude;
    [SerializeField] private float WanderSpeed;
    private float EndOfLineThreshHold = 4f;

    [Header("Destination")]
    [SerializeField] private float DestinationThreshold = 5f;

    [Header("Polling")]
    [SerializeField] private float ArrivalCheckInterval = 0.25f;        // how often, in seconds, we should check if we've reached the destination

    [Header("Building Interception")]
    [SerializeField] private float BuildingArriveDistance = 2f;         // how close to get before stopping at a building
    [SerializeField] private float BuildingCheckInterval = 0.25f;       // how often, in seconds, to check if the current building target is dead/gone

    private NavMeshAgent NMAgent;
    private Pathway CurrentPath;
    public float DistanceAlongPath;                 // creating and exposing this now incase I want to do some cutscene scripting later in the project
    private float NoiseSeed;
    private Coroutine RunRoutine;
    public State CurrentState { get; private set; }

    private Health CombatHealth;
    private Attacker CombatAttacker;

    private readonly List<Transform> NearbyBuildings = new List<Transform>();           // buildings near our trigger
    private Transform CurrentBuildingTarget;

    private void Awake()
    {
        NMAgent = GetComponent<NavMeshAgent>();
        CombatHealth = GetComponent<Health>();
        CombatAttacker = GetComponent<Attacker>();
        NoiseSeed = Random.Range(0, 10000f);
    }

    public void Initialize(EnemyStats Stats)                // call this immediately after initializing the enemy, to provide it's stats (as we scale speed, damage and health as waves progress)
    {
        NMAgent.speed = Stats.MoveSpeed;
        CombatHealth.SetMaxHealth(Stats.MaxHealth);
        CombatAttacker.SetStats(Stats.AttackDamage, Stats.AttackRate);
    }

    private void OnEnable()
    {
        RunRoutine = StartCoroutine(Run());
    }

    private void OnDisable()
    {
        if (RunRoutine != null) StopCoroutine(RunRoutine);
    }
    private void OnDestroy()
    {
        if (RunRoutine != null) StopCoroutine(RunRoutine);
    }

    public void Restart()
    {
        if (RunRoutine != null)
        {
            StopCoroutine(RunRoutine);
            RunRoutine = StartCoroutine(Run());
        }
    }

    private IEnumerator Run()
    {
        CurrentState = State.SeekingLine;
        if (!PathManager.FindClosestPath(transform.position, out CurrentPath, out Vector3 point, out DistanceAlongPath))
        {
            CurrentState = State.Arrived;        // nothing to follow
            Debug.Log("Error, no path to follow");
            yield break;
        }
        NMAgent.SetDestination(point);
        Debug.Log("Walking to path");
        yield return WaitUntilArrived();
        Debug.Log("At path, following it");
        CurrentState = State.FollowingLine;
        while (true)
        {
            CurrentPath.ClosestPoint(transform.position, out _, out DistanceAlongPath, out _);

            if (DistanceAlongPath >= CurrentPath.TotalLength - EndOfLineThreshHold)
                break;

            SetNextWanderTarget();
            yield return WaitUntilArrived();
        }

        // finished following the path, now we walk the final distance
        Debug.Log("Finished following path,  walking to tower");
        CurrentState = State.MovingToDestination;
        Transform dest = CurrentPath.EffectiveDestination;
        if (dest != null)
        {
            NMAgent.SetDestination(dest.position);
            yield return WaitUntilArrived();
        }

        CurrentState = State.Arrived;
        Debug.Log("At the tower!");

        if (dest != null)
        {
            Health DestinationHealth = dest.GetComponent<Health>();
            if (DestinationHealth != null)
            {
                CombatAttacker.SetTarget(DestinationHealth);
            }
        }
    }

    private IEnumerator WaitUntilArrived()
    {
        while (NMAgent.pathPending)
            yield return null;

        WaitForSeconds wait = ArrivalCheckInterval > 0f ? new WaitForSeconds(ArrivalCheckInterval) : null;
        while (NMAgent.remainingDistance > Mathf.Max(NMAgent.stoppingDistance, DestinationThreshold))
        {
            yield return wait;
        }
    }

    private void OnTriggerEnter(Collider Other)
    {
        Transform Building = ResolveRoot(Other);
        if (!Building.CompareTag("Building")) return;

        if (NearbyBuildings.Contains(Building)) return;
        NearbyBuildings.Add(Building);

        // If we're already dealing with a building, just queue this one - don't
        // retarget mid-attack. It'll get picked up once the current one is resolved.
        if (CurrentState != State.AttackingBuilding)
        {
            InterruptForBuilding(Building);
        }
    }

    private void OnTriggerExit(Collider Other)
    {
        Transform Building = ResolveRoot(Other);
        if (!Building.CompareTag("Building")) return;
        NearbyBuildings.Remove(Building);
    }

    private static Transform ResolveRoot(Collider Other)        // in case the collider lives not on the game object, but on a child object
    {
        return Other.attachedRigidbody != null ? Other.attachedRigidbody.transform : Other.transform;
    }

    private void InterruptForBuilding(Transform Building)
    {
        if (RunRoutine != null) StopCoroutine(RunRoutine);
        RunRoutine = StartCoroutine(AttackBuilding(Building));
    }

    private IEnumerator AttackBuilding(Transform Building)
    {
        CurrentState = State.AttackingBuilding;
        CurrentBuildingTarget = Building;
        Debug.Log($"Diverting to attack {Building.name}");

        float OriginalStoppingDistance = NMAgent.stoppingDistance;
        NMAgent.stoppingDistance = BuildingArriveDistance;
        NMAgent.SetDestination(Building.position);

        while (NMAgent.pathPending)
            yield return null;

        WaitForSeconds Wait = BuildingCheckInterval > 0f ? new WaitForSeconds(BuildingCheckInterval) : null;

        // Close the distance before actually swinging at it.
        while (CurrentBuildingTarget != null && NearbyBuildings.Contains(CurrentBuildingTarget)
               && NMAgent.remainingDistance > NMAgent.stoppingDistance)
        {
            yield return Wait;
        }

        // Still valid and in range - start dealing damage, then hold here until it dies or leaves.
        if (CurrentBuildingTarget != null && NearbyBuildings.Contains(CurrentBuildingTarget))
        {
            Health BuildingHealth = Building.GetComponent<Health>();
            if (BuildingHealth != null)
                CombatAttacker.SetTarget(BuildingHealth);

            while (CurrentBuildingTarget != null && NearbyBuildings.Contains(CurrentBuildingTarget))
            {
                yield return Wait;
            }
        }

        CombatAttacker.StopAttacking();
        NMAgent.stoppingDistance = OriginalStoppingDistance;

        bool WasDestroyed = CurrentBuildingTarget == null;
        NearbyBuildings.RemoveAll(T => T == null); // clean up any other stale references
        CurrentBuildingTarget = null;

        if (NearbyBuildings.Count > 0)
        {
            // Something else was overlapping while we were busy - deal with it next.
            Transform Next = NearbyBuildings[0];
            RunRoutine = StartCoroutine(AttackBuilding(Next));
        }
        else
        {
            RunRoutine = StartCoroutine(Run());     // Nothing left to fight - resume normal behaviour.
        }
    }

    private void SetNextWanderTarget()
    {
        float targetDist = DistanceAlongPath + LookAhead;
        Vector3 pointOnLine = CurrentPath.GetPointAtDistance(targetDist, out Vector3 tangent);

        Vector3 perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;
        float noise = (Mathf.PerlinNoise(NoiseSeed, Time.time * WanderSpeed) - 0.5f) * 2f;
        Vector3 wanderPoint = pointOnLine + perpendicular * noise * WanderAmplitude;

        if (NavMesh.SamplePosition(wanderPoint, out NavMeshHit hit, WanderAmplitude + 1f, NavMesh.AllAreas))
            NMAgent.SetDestination(hit.position);
        else
            NMAgent.SetDestination(pointOnLine);
    }
}