using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RandomPatrol : MonoBehaviour
{
    [Header("Wander Settings")]
    [Tooltip("Max distance from this object's current position to search for a new point.")]
    [SerializeField] private float wanderRadius = 20f;

    [Tooltip("Optional: restrict the search to points near this transform instead of the agent. Leave null to wander from current position each time.")]
    [SerializeField] private Transform wanderOrigin;

    [Tooltip("How close (in NavMesh sample distance) a random point must land to be accepted.")]
    [SerializeField] private float sampleMaxDistance = 4f;

    [Tooltip("Seconds to wait at each point before picking a new one.")]
    [SerializeField] private float minWaitTime = 0f;
    [SerializeField] private float maxWaitTime = 2f;

    [Tooltip("How often (seconds) to poll 'have I arrived yet' while walking. Lower = more responsive, higher = cheaper.")]
    [SerializeField] private float arrivalCheckInterval = 0.25f;

    [Tooltip("NavMesh area mask to restrict sampling to (default: all areas).")]
    [SerializeField] private int areaMask = NavMesh.AllAreas;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        StartCoroutine(PatrolRoutine());
    }

    private System.Collections.IEnumerator PatrolRoutine()
    {
        WaitForSeconds checkWait = new WaitForSeconds(Mathf.Max(0.05f, arrivalCheckInterval));

        while (enabled)
        {
            // Pick and move to a new destination.
            if (TryGetRandomPoint(out Vector3 point))
            {
                agent.SetDestination(point);
            }

            // IMPORTANT: pathPending does not become true the instant SetDestination
            // is called - it only updates on Unity's next internal pass. Without this
            // single-frame wait, HasArrived() below can read stale remainingDistance
            // data from the *previous* destination and think we've already arrived,
            // so the agent never actually moves to the new point.
            yield return null;

            // Wait until path is computed, then poll arrival at low frequency
            // instead of every Update frame.
            while (agent.pathPending)
            {
                yield return null; // pathPending resolves quickly; fine to check each frame briefly.
            }

            while (!HasArrived())
            {
                yield return checkWait;
            }

            // Optional pause before choosing the next point.
            float wait = Random.Range(minWaitTime, maxWaitTime);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }
    }

    private bool HasArrived()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false; // no path yet - definitely not arrived
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true; // bail out, pick a new point

        return agent.remainingDistance <= agent.stoppingDistance;
    }

    private bool TryGetRandomPoint(out Vector3 result)
    {
        Vector3 origin = wanderOrigin != null ? wanderOrigin.position : transform.position;
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sampleMaxDistance, areaMask))
        {
            result = hit.position;
            return true;
        }

        result = origin;
        return false;
    }
}