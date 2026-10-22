using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningWeatherEvent : WeatherEventBase
{
    [Header("Strike Settings")]
    [SerializeField, Min(0.1f)] private float StrikeInterval = 15f;
    [SerializeField, Min(0.1f)] private float StrikeRadius = 15f;
    [SerializeField, Min(0f)] private float StrikeDamage = 25f;
    [SerializeField] private LayerMask DamageLayers = ~0;

    [Header("Lightning Mesh")]
    [SerializeField] private Material LightningMaterial;
    [SerializeField, Min(1f)] private float BoltHeight = 12f;
    [SerializeField, Min(0.01f)] private float BoltWidth = 0.3f;
    [SerializeField, Range(3, 20)] private int BoltSegments = 8;
    [SerializeField, Min(0f)] private float BoltJitter = 1f;

    [Header("Light")]
    [SerializeField] private Light FlashLight;
    [SerializeField, Min(0.01f)] private float FlashDuration = 0.15f;
    [SerializeField, Min(0f)] private float FlashIntensity = 5f;

    private GameObject BoltObject;
    private Mesh BoltMesh;
    private Coroutine StrikeRoutine;
    private Coroutine FlashRoutine;

    private readonly HashSet<Health> HitTargets = new HashSet<Health>();
    private readonly List<Health> TargetsToDamage = new List<Health>();

    private float OriginalLightIntensity;
    private bool HasOriginalLightIntensity;

    private void Awake()
    {
        BuildLightningMesh();

        if (FlashLight != null)
        {
            OriginalLightIntensity = FlashLight.intensity;
            HasOriginalLightIntensity = true;
            FlashLight.intensity = 0f;
        }
    }

    public override void BeginEvent(int WaveNumber)
    {
        EndEvent();

        if (BoltObject == null) BuildLightningMesh();

        Debug.Log("Lightning Event Coming!");

        StrikeRoutine = StartCoroutine(StrikeLoop());
    }

    public override void EndEvent()
    {
        if (StrikeRoutine != null)
        {
            StopCoroutine(StrikeRoutine);
            StrikeRoutine = null;
        }

        if (FlashRoutine != null)
        {
            StopCoroutine(FlashRoutine);
            FlashRoutine = null;
        }

        if (BoltObject != null)
        {
            BoltObject.SetActive(false);
        }

        if (FlashLight != null)
        {
            FlashLight.intensity = 0f;
        }

        HitTargets.Clear();
        TargetsToDamage.Clear();
    }

    private IEnumerator StrikeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(StrikeInterval);
            Strike();
            Debug.LogWarning("LIGHTNING STRIKE");
        }
    }

    private void Strike()
    {
        if (ProceduralFloor.Instance == null)
        {
            Debug.LogWarning("Lightning: Floor.Instance is missing.", this);
            return;
        }

        Vector3 StrikePosition = GetRandomStrikePosition();

        Collider[] Hits = Physics.OverlapSphere(StrikePosition, StrikeRadius, DamageLayers, QueryTriggerInteraction.Ignore);

        HitTargets.Clear();
        TargetsToDamage.Clear();

        foreach (Collider hit in Hits)          // get all valid targets
        {
            if (hit == null) continue;

            Health health = hit.GetComponentInParent<Health>();

            if (health == null || !health.gameObject.activeInHierarchy || health.IsDead) continue;

            if (HitTargets.Add(health))
                TargetsToDamage.Add(health);
        }

        foreach (Health health in TargetsToDamage)
        {
            if (health == null || !health.gameObject.activeInHierarchy || health.IsDead) continue;

            health.TakeDamage(StrikeDamage);
        }

        TriggerFlash(StrikePosition);
    }

    private Vector3 GetRandomStrikePosition()
    {
        ProceduralFloor Floor = ProceduralFloor.Instance;

        float Extent = Floor.MapExtent;

        float LocalX = Random.Range(0f, Extent);
        float LocalZ = Random.Range(0f, Extent);

        Vector3 LocalPosition = new Vector3(LocalX, 0, LocalZ);
        Vector3 WorldPosition = Floor.FloorTransform.TransformPoint(LocalPosition);

        WorldPosition.y = Floor.SampleHeight(WorldPosition);

        return WorldPosition;
    }

    private void BuildLightningMesh()
    {
        if (BoltObject != null) return;

        BoltObject = new GameObject("ProceduralLightningBolt");
        BoltObject.transform.SetParent(transform, false);
        BoltObject.SetActive(false);

        MeshFilter filter = BoltObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = BoltObject.AddComponent<MeshRenderer>();

        BoltMesh = GenerateBoltMesh();
        filter.sharedMesh = BoltMesh;

        if (LightningMaterial != null)
        {
            renderer.sharedMaterial = LightningMaterial;
        }
        else
        {
            Debug.LogWarning("No Bolt Material Applied", this);
        }
        ;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Mesh GenerateBoltMesh()
    {
        int segments = Mathf.Max(3, BoltSegments);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Create two crossed ribbons so the bolt is visible
        // from different viewing directions.
        for (int plane = 0; plane < 2; plane++)
        {
            int startVertex = vertices.Count;

            Vector3[] centers = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                // t = 0 is the top; t = 1 is the ground.
                float y = BoltHeight * (1f - t);

                float x = 0f;
                float z = 0f;

                if (i > 0 && i < segments)
                {
                    x = Random.Range(-BoltJitter, BoltJitter);
                    z = Random.Range(-BoltJitter, BoltJitter);
                }

                centers[i] = new Vector3(x, y, z);
            }

            // Two vertices per centerline point form a ribbon.
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                float taper = Mathf.Lerp(0.15f, 1f, t);
                float halfWidth = BoltWidth * 0.5f * taper;

                Vector3 offset = plane == 0
                    ? new Vector3(halfWidth, 0f, 0f)
                    : new Vector3(0f, 0f, halfWidth);

                vertices.Add(centers[i] - offset);
                vertices.Add(centers[i] + offset);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = startVertex + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);

                // Back faces, so the bolt remains visible
                // when viewed from the opposite side.
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(a);

                triangles.Add(d);
                triangles.Add(c);
                triangles.Add(b);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Lightning Bolt";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void TriggerFlash(Vector3 position)
    {
        if (BoltObject != null)
        {
            BoltObject.transform.position = position;
            BoltObject.transform.rotation = Quaternion.identity;
            BoltObject.SetActive(true);
        }

        if (FlashLight != null)
        {
            FlashLight.transform.position = position + Vector3.up * (BoltHeight * 0.5f);
            FlashLight.intensity = FlashIntensity;
        }

        if (FlashRoutine != null)
            StopCoroutine(FlashRoutine);

        FlashRoutine = StartCoroutine(FlashLoop());
    }

    private IEnumerator FlashLoop()
    {
        yield return new WaitForSeconds(FlashDuration);

        if (BoltObject != null)
            BoltObject.SetActive(false);

        if (FlashLight != null)
            FlashLight.intensity = 0f;

        FlashRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        // Visual aid for inspecting the configured strike radius.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, StrikeRadius);
    }

    private void OnDestroy()
    {
        if (BoltMesh != null)
            Destroy(BoltMesh);
    }
}
