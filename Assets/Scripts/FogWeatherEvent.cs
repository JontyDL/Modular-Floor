using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FogWeatherEvent : WeatherEventBase
{
    [Header("Fog Mesh")]
    [SerializeField] private Material FogMaterial;
    [SerializeField, Range(1, 4)] private int SubdivisionsPerCell = 2;
    [SerializeField, Min(0f)] private float GroundOffset = 0.15f;

    [Header("Fade In")]
    [SerializeField, Min(0f)] private float FadeInDuration = 2.5f;

    [Header("Gameplay")]
    [SerializeField, Range(0.1f, 1f)] private float RangeMultiplier = 0.75f;
    [SerializeField, Min(0.1f)] private float RefreshInterval = 0.5f;

    private GameObject FogObject;
    private Mesh FogMesh;
    private Coroutine RefreshRoutine;
    private Coroutine FadeRoutine;
    private readonly HashSet<Building> AffectedBuildings = new HashSet<Building>();

    private MaterialPropertyBlock FogPropertyBlock;
    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");

    public override void BeginEvent(int WaveNumber)
    {
        EndEvent();

        if (ProceduralFloor.Instance == null) return;

        BuildFogMesh();
        SetFogVisible(true);
        if (FogPropertyBlock == null)
            FogPropertyBlock = new MaterialPropertyBlock();

        if (FadeRoutine != null)
            StopCoroutine(FadeRoutine);

        FadeRoutine = StartCoroutine(FadeFogIn());

        ApplyFogToBuildings();

        RefreshRoutine = StartCoroutine(RefreshBuildingsLoop());
    }

    public override void EndEvent()
    {
        if (RefreshRoutine != null)
        {
            StopCoroutine(RefreshRoutine);
            RefreshRoutine = null;
        }

        foreach (Building building in AffectedBuildings)
        {
            if (building != null)
            {
                building.SetWeatherRangeMultiplier(1f);
            }
        }

        AffectedBuildings.Clear();
        SetFogVisible(false);
    }

    private void BuildFogMesh()
    {
        ProceduralFloor Floor = ProceduralFloor.Instance;

        if (FogObject == null)
        {
            FogObject = new GameObject("ProceduralFogMesh");
            FogObject.transform.SetParent(Floor.transform, false);

            MeshFilter Filter = FogObject.AddComponent<MeshFilter>();
            MeshRenderer Renderer = FogObject.AddComponent<MeshRenderer>();

            FogMesh = GenerateFogMesh(Floor);
            Filter.sharedMesh = FogMesh;

            if (FogMaterial != null)
                Renderer.sharedMaterial = FogMaterial;

            Renderer.shadowCastingMode = ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
        }
        else
        {
            FogObject.transform.SetParent(Floor.transform, false);
        }

        FogObject.transform.localPosition = Vector3.zero;
        FogObject.transform.localRotation = Quaternion.identity;
        FogObject.transform.localScale = Vector3.one;
    }

    private Mesh GenerateFogMesh(ProceduralFloor Floor)
    {
        int Cells = (Floor.GridSize - 1) * SubdivisionsPerCell;
        Cells = Convert.ToInt32(MathF.Max(1, Cells));

        float Extent = Floor.MapExtent;
        float Step = Extent / Cells;

        List<Vector3> Vertices = new List<Vector3>();
        List<Vector2> Uvs = new List<Vector2>();
        List<int> Triangles = new List<int>();

        for (int z = 0; z <= Cells; z++)
        {
            for (int x = 0; x <= Cells; x++)
            {
                float localX = x * Step;
                float localZ = z * Step;

                Vector3 worldPoint = Floor.transform.TransformPoint(
                    new Vector3(localX, 0f, localZ));

                float worldHeight = Floor.SampleHeight(worldPoint);

                Vector3 localSurface = Floor.transform.InverseTransformPoint(
                    new Vector3(worldPoint.x, worldHeight, worldPoint.z));

                localSurface.y += GroundOffset;

                Vertices.Add(localSurface);
                Uvs.Add(new Vector2(
                    localX / Mathf.Max(Extent, 0.001f),
                    localZ / Mathf.Max(Extent, 0.001f)));
            }
        }

        for (int z = 0; z < Cells; z++)
        {
            for (int x = 0; x < Cells; x++)
            {
                int a = z * (Cells + 1) + x;
                int b = a + 1;
                int c = a + Cells + 1;
                int d = c + 1;

                // Both sides are rendered by the fog shader.
                Triangles.Add(a);
                Triangles.Add(b);
                Triangles.Add(c);

                Triangles.Add(b);
                Triangles.Add(d);
                Triangles.Add(c);
            }
        }

        Mesh Mesh = new Mesh();
        Mesh.name = "Procedural Fog Ground";
        Mesh.indexFormat = Vertices.Count > 65535
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;

        Mesh.SetVertices(Vertices);
        Mesh.SetUVs(0, Uvs);
        Mesh.SetTriangles(Triangles, 0);
        Mesh.RecalculateBounds();

        return Mesh;
    }

    private void SetFogVisible(bool Visible)
    {
        if (FogObject != null)
            FogObject.SetActive(Visible);
    }

    private void ApplyFogToBuildings()
    {
        Building[] Buildings = UnityEngine.Object.FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Building building in Buildings)
        {
            if (building == null || !building.isActiveAndEnabled)
                continue;

            building.SetWeatherRangeMultiplier(RangeMultiplier);
            AffectedBuildings.Add(building);
        }
    }

    private IEnumerator RefreshBuildingsLoop()
    {
        var wait = new WaitForSeconds(RefreshInterval);

        while (true)
        {
            ApplyFogToBuildings();
            yield return wait;
        }
    }

    private IEnumerator FadeFogIn()
    {
        if (FogObject == null)
            yield break;

        MeshRenderer Renderer = FogObject.GetComponent<MeshRenderer>();

        if (Renderer == null || FogMaterial == null)
            yield break;

        // Read the chosen opacity from the material.
        float targetOpacity = FogMaterial.GetFloat(OpacityID);

        Renderer.GetPropertyBlock(FogPropertyBlock);
        FogPropertyBlock.SetFloat(OpacityID, 0f);
        Renderer.SetPropertyBlock(FogPropertyBlock);

        if (FadeInDuration <= 0f)
        {
            FogPropertyBlock.SetFloat(OpacityID, targetOpacity);
            Renderer.SetPropertyBlock(FogPropertyBlock);
            FadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < FadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / FadeInDuration);

            // Smooth start and finish.
            t = t * t * (3f - 2f * t);

            FogPropertyBlock.SetFloat(OpacityID, Mathf.Lerp(0f, targetOpacity, t));
            Renderer.SetPropertyBlock(FogPropertyBlock);

            yield return null;
        }

        FogPropertyBlock.SetFloat(OpacityID, targetOpacity);
        Renderer.SetPropertyBlock(FogPropertyBlock);

        FadeRoutine = null;
    }

    private void OnDestroy()
    {
        if (FadeRoutine != null)
        {
            StopCoroutine(FadeRoutine);
            FadeRoutine = null;
        }

        if (FogMesh != null)
        {
            Destroy(FogMesh);
        }
    }
}
