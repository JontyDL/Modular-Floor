using System.Collections.Generic;
using UnityEngine;

public static class PathManager
{
    private static readonly List<Pathway> Paths = new List<Pathway>();

    public static void RegisterPath(Pathway InPath)
    {
        if (!Paths.Contains(InPath))
        {
            Paths.Add(InPath);
        }
    }

    public static void UnregisterPath(Pathway InPath)
    {
        Paths.Remove(InPath);
    }

    public static bool FindClosestPath(Vector3 WorldPos, out Pathway ClosestPath, out Vector3 ClosestPointOnPath, out float DistanceAlongPath)
    {
        ClosestPath = null;
        ClosestPointOnPath = WorldPos;
        DistanceAlongPath = 0;
        float BestDistance = float.MaxValue;

        for (int i = 0; i < Paths.Count; i++)
        {
            Pathway P = Paths[i];
            if (P == null) continue;

            if (P.ClosestPoint(WorldPos, out Vector3 Point, out float AlongDistance, out float Dist))
            {
                if (Dist < BestDistance)
                {
                    BestDistance = Dist;
                    ClosestPath = P;
                    ClosestPointOnPath = Point;
                    DistanceAlongPath = AlongDistance;
                }
            }
        }

        return ClosestPath != null;
    }
}
