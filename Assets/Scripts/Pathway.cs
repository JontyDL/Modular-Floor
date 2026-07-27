using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Pathway : MonoBehaviour
{
    public Transform[] Waypoints;       // ordered positions which will act as "the path"

    public Transform Destination;       // point, when after reaching the pathway, the ai walks towards

    private float[] CumulativeDistances;
    public float TotalLength { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        RebuildDistanceTable();
        PathManager.RegisterPath(this);
    }

    private void OnDisable()
    {
        PathManager.UnregisterPath(this);
    }

    void OnValidate()
    {
        RebuildDistanceTable();
    }

    public void RebuildDistanceTable()
    {
        if (Waypoints == null || Waypoints.Length < 2)
        {
            CumulativeDistances = new float[0];
            TotalLength = 0f;
            return;
        }

        CumulativeDistances = new float[Waypoints.Length];
        CumulativeDistances[0] = 0f;
        for (int i = 1; i < Waypoints.Length; i++)
        {
            if (Waypoints[i - 1] == null || Waypoints[i] == null) continue;
            float segLength = Vector3.Distance(Waypoints[i - 1].position, Waypoints[i].position);
            CumulativeDistances[i] = CumulativeDistances[i - 1] + segLength;
        }
        TotalLength = CumulativeDistances[CumulativeDistances.Length - 1];
    }

    public Transform EffectiveDestination =>
        Destination != null ? Destination :
        (Waypoints != null && Waypoints.Length > 0 ? Waypoints[Waypoints.Length - 1] : null);

    public bool ClosestPoint(Vector3 WorldPos, out Vector3 Point, out float DistanceAlongPath, out float DistanceToLine)        // gets the closest point along the whole line to WorldPos
    {
        Point = WorldPos;
        DistanceAlongPath = 0f;
        DistanceToLine = float.MaxValue;

        if (Waypoints == null || Waypoints.Length < 2 || CumulativeDistances == null) return false;

        for (int i = 0; i < Waypoints.Length - 1; i++)
        {
            if (Waypoints[i] == null || Waypoints[i + 1] == null) continue;

            Vector3 a = Waypoints[i].position;
            Vector3 b = Waypoints[i + 1].position;
            Vector3 closest = ClosestPointOnSegment(WorldPos, a, b, out float t);
            float dist = Vector3.Distance(WorldPos, closest);

            if (dist < DistanceToLine)
            {
                DistanceToLine = dist;
                Point = closest;
                DistanceAlongPath = CumulativeDistances[i] + t * Vector3.Distance(a, b);
            }
        }
        return DistanceToLine < float.MaxValue;
    }

    public Vector3 GetPointAtDistance(float Distance, out Vector3 Tangent)
    {
        Tangent = Vector3.forward;
        if (Waypoints == null || Waypoints.Length < 2) return transform.position;

        Distance = Mathf.Clamp(Distance, 0f, TotalLength);

        for (int i = 0; i < Waypoints.Length - 1; i++)
        {
            if (Waypoints[i] == null || Waypoints[i + 1] == null) continue;

            float segStart = CumulativeDistances[i];
            float segEnd = CumulativeDistances[i + 1];
            if (Distance <= segEnd || i == Waypoints.Length - 2)
            {
                float segLength = segEnd - segStart;
                float t = segLength > 0.0001f ? Mathf.Clamp01((Distance - segStart) / segLength) : 0f;
                Vector3 a = Waypoints[i].position;
                Vector3 b = Waypoints[i + 1].position;
                Tangent = (b - a).normalized;
                return Vector3.Lerp(a, b, t);
            }
        }
        return Waypoints[Waypoints.Length - 1].position;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b, out float t)
    {
        Vector3 ab = b - a;
        float sqrLen = ab.sqrMagnitude;
        if (sqrLen < 0.000001f)
        {
            t = 0f;
            return a;
        }
        t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sqrLen);
        return a + ab * t;
    }
}
