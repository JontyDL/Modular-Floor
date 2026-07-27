using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ProceduralFloor : MonoBehaviour
{
    [Header("Grid")]
    public int size = 20;
    public float spacing = 2f;

    [Header("Terrain")]
    public float maxStep = 0.5f;
    public float minHeight = -5f;
    public float maxHeight = 5f;

    [Header("Vegetation")]
    public GameObject[] treePrefabs;
    public GameObject[] vegetationPrefabs;

    [Range(0f, 1f)]
    public float treeChance = 0.05f;

    [Range(0f, 1f)]
    public float vegetationChance = 0.15f;

    public float minTreeSpacing = 6f;

    [Header("Vegetation Distribution")]
    public float forestNoiseScale = 0.05f;
    public float forestThreshold = 0.55f;
    public float maxSlope = 30f;
    public int edgeBuffer = 2;
    public float placementJitter = 0.35f;

    [Header("Vegetation Exclusion Zone")]
    [Tooltip("Width, in grid cells, of the cross-shaped no-spawn band running through the center row and center column (e.g. 4 excludes cells 18-21 on a 40x40 grid).")]
    public float crossExclusionWidth = 4f;

    [Header("Seed")]
    public int seed;
    private System.Random terrainRng;
    private System.Random vegetationRng;

    [Header("Camera Target")]
    public bool setOrbitCameraTarget = true;
    private Transform centerTarget;

    [Header("Debug")]
    public bool showDebugPoints = true;
    public float sphereSize = 0.2f;

    private float[,] heights;

    /*void Start()
    {
        terrainRng = new System.Random(seed);
        vegetationRng = new System.Random(seed ^ 0x5A5A5A5A);
 
        Generate();
        BuildMesh();
        ClearVegetation();
        GenerateVegetation();
 
        if (showDebugPoints)
            SpawnDebugPoints();
    }*/

    private void Awake()
    {
        terrainRng = new System.Random(seed);
        vegetationRng = new System.Random(seed ^ 0x5A5A5A5A);

        Generate();
        BuildMesh();
        ClearVegetation();
        GenerateVegetation();

        if (showDebugPoints)
            SpawnDebugPoints();

        UpdateCenterTarget();
    }

    private void OnValidate()
    {
        terrainRng = new System.Random(seed);
        vegetationRng = new System.Random(seed ^ 0x5A5A5A5A);

        Generate();
        BuildMesh();
    }

    void ClearVegetation()
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Veg_"))
                children.Add(child);
        }

        foreach (Transform child in children)
        {
#if UNITY_EDITOR

            DestroyImmediate(child.gameObject);
#else
                    Destroy(child.gameObject);
#endif

        }
    }

    bool IsInCenterCross(int x, int z)
    {
        if (crossExclusionWidth <= 0f)
            return false;

        float centerIndex = (size - 1) / 2f;
        float half = crossExclusionWidth / 2f;

        return Mathf.Abs(x - centerIndex) < half ||
               Mathf.Abs(z - centerIndex) < half;
    }

    void GenerateVegetation()
    {
        if ((treePrefabs == null || treePrefabs.Length == 0) &&
            (vegetationPrefabs == null || vegetationPrefabs.Length == 0))
            return;

        float cellSize = minTreeSpacing;

        Dictionary<Vector2Int, List<Vector3>> treeGrid =
            new Dictionary<Vector2Int, List<Vector3>>();

        float noiseOffsetX = vegetationRng.Next(-100000, 100000);
        float noiseOffsetZ = vegetationRng.Next(-100000, 100000);

        for (int x = edgeBuffer; x < size - edgeBuffer; x++)
        {
            for (int z = edgeBuffer; z < size - edgeBuffer; z++)
            {
                // Keep the center cross clear of vegetation
                if (IsInCenterCross(x, z))
                    continue;

                Vector3 position = new Vector3(
                    x * spacing,
                    heights[x, z],
                    z * spacing);

                // Small deterministic jitter
                position.x += (((float)vegetationRng.NextDouble() * 2f) - 1f) * placementJitter;
                position.z += (((float)vegetationRng.NextDouble() * 2f) - 1f) * placementJitter;

                // Forest patches
                float forest =
                    Mathf.PerlinNoise(
                        noiseOffsetX + x * forestNoiseScale,
                        noiseOffsetZ + z * forestNoiseScale);

                if (forest < forestThreshold)
                    continue;

                // Slope
                Vector3 normal = CalculateNormal(x, z);
                float slope = Vector3.Angle(normal, Vector3.up);

                if (slope > maxSlope)
                    continue;

                // Trees
                if (treePrefabs.Length > 0 &&
                    (float)vegetationRng.NextDouble() < treeChance)
                {
                    Vector2Int cell = new Vector2Int(
                        Mathf.FloorToInt(position.x / cellSize),
                        Mathf.FloorToInt(position.z / cellSize));

                    bool tooClose = false;

                    // Only search neighbouring cells.
                    for (int dx = -1; dx <= 1 && !tooClose; dx++)
                    {
                        for (int dz = -1; dz <= 1 && !tooClose; dz++)
                        {
                            Vector2Int neighbour = new Vector2Int(
                                cell.x + dx,
                                cell.y + dz);

                            if (!treeGrid.TryGetValue(neighbour, out List<Vector3> list))
                                continue;

                            foreach (Vector3 other in list)
                            {
                                if ((other - position).sqrMagnitude <
                                    minTreeSpacing * minTreeSpacing)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!tooClose)
                    {
                        if (!treeGrid.TryGetValue(cell, out List<Vector3> list))
                        {
                            list = new List<Vector3>();
                            treeGrid[cell] = list;
                        }

                        list.Add(position);

                        GameObject prefab =
                            treePrefabs[
                                vegetationRng.Next(treePrefabs.Length)];

                        GameObject tree = Instantiate(
                            prefab,
                            position,
                            Quaternion.Euler(
                                0,
                                vegetationRng.Next(360),
                                0),
                            transform);

                        tree.name = "Veg_Tree";
                    }
                }

                // Grass / rocks / shrubs
                if (vegetationPrefabs.Length > 0 &&
                    (float)vegetationRng.NextDouble() < vegetationChance)
                {
                    GameObject prefab =
                        vegetationPrefabs[
                            vegetationRng.Next(vegetationPrefabs.Length)];

                    GameObject veg = Instantiate(
                        prefab,
                        position,
                        Quaternion.Euler(
                            0,
                            vegetationRng.Next(360),
                            0),
                        transform);

                    veg.name = "Veg_Object";
                }
            }
        }
    }

    void Generate()
    {
        heights = new float[size, size];
        heights[0, 0] = 0f;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (x == 0 && z == 0)
                    continue;

                float total = 0f;
                int count = 0;

                if (x > 0)
                {
                    total += heights[x - 1, z];
                    count++;
                }

                if (z > 0)
                {
                    total += heights[x, z - 1];
                    count++;
                }

                if (x > 0 && z > 0)
                {
                    total += heights[x - 1, z - 1];
                    count++;
                }

                float average = total / count;
                float offset = ((float)terrainRng.NextDouble() * 2f - 1f) * maxStep;

                heights[x, z] = Mathf.Clamp(
                    average + offset,
                    minHeight,
                    maxHeight);
            }
        }
    }

    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Floor";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Vertices & UVs
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                vertices.Add(new Vector3(
                    x * spacing,
                    heights[x, z],
                    z * spacing));

                uvs.Add(new Vector2(
                    (float)x / (size - 1),
                    (float)z / (size - 1)));
            }
        }

        // Triangles
        for (int z = 0; z < size - 1; z++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                int a = z * size + x;
                int b = a + 1;
                int c = a + size;
                int d = c + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        meshFilter.sharedMesh = mesh;

        // MeshCollider needs to be reset before assigning a new mesh
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    void SpawnDebugPoints()
    {
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                point.transform.SetParent(transform);
                point.transform.localScale = Vector3.one * sphereSize;
                point.transform.localPosition = new Vector3(
                    x * spacing,
                    heights[x, z],
                    z * spacing);

                point.name = $"Point ({x}, {z})";

                Destroy(point.GetComponent<Collider>());
            }
        }
    }

    void UpdateCenterTarget()
    {
        if (heights == null || size < 1)
            return;

        int cx = (size - 1) / 2;
        int cz = (size - 1) / 2;

        Vector3 localCenter = new Vector3(
            cx * spacing,
            heights[cx, cz],
            cz * spacing);

        Vector3 worldCenter = transform.TransformPoint(localCenter);

        if (centerTarget == null)
        {
            GameObject go = new GameObject("TerrainCenterTarget");
            centerTarget = go.transform;
            centerTarget.SetParent(transform, worldPositionStays: false);
        }

        centerTarget.position = worldCenter;

        if (setOrbitCameraTarget && OrbitCamera.Instance != null)
        {
            OrbitCamera.Instance.target = centerTarget;
            Debug.Log("trying to set center target at: " + centerTarget.position);
        }
    }

    Vector3 CalculateNormal(int x, int z)
    {
        int x0 = Mathf.Max(0, x - 1);
        int x1 = Mathf.Min(size - 1, x + 1);

        int z0 = Mathf.Max(0, z - 1);
        int z1 = Mathf.Min(size - 1, z + 1);

        Vector3 dx = new Vector3(
            (x1 - x0) * spacing,
            heights[x1, z] - heights[x0, z],
            0);

        Vector3 dz = new Vector3(
            0,
            heights[x, z1] - heights[x, z0],
            (z1 - z0) * spacing);

        return Vector3.Cross(dz, dx).normalized;
    }
}