using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PathFollower : MonoBehaviour
{
    public enum State
    {
        SeekingLine,                // walking towards the closest path
        FollowingLine,              // following the path to the destination
        MovingToDestination,        // finished following the path, walking to the destination
        Arrived                     // at the destination, can start attacking the tower
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

    private NavMeshAgent NMAgent;
    private Pathway CurrentPath;
    public float DistanceAlongPath;                 // creating and exposing this now incase I want to do some cutscene scripting later in the project
    private float NoiseSeed;
    private Coroutine RunRoutine;
    public State CurrentState {  get; private set; }

    private void Awake()
    {
        NMAgent = GetComponent<NavMeshAgent>();
        NoiseSeed = Random.Range(0, 10000f);
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
