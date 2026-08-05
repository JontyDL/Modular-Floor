using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingGridSystem : MonoBehaviour
{
    public static BuildingGridSystem Instance { get; private set; }

    public GameObject PlacingObject;
    public float GridSize;

    [SerializeField] private LayerMask NatureLayerMask;     // the layer that the tree's and rocks are on

    [SerializeField] private LayerMask GroundLayerMask;

    [Header("Ghost Preview")]
    [SerializeField] private Shader ghostShader;

    private GameObject PreviewObject;
    private Collider PreviewCollider;
    private Material ghostMaterialInstance;

    private bool isActive = false;
    private bool isPlacementValid = false;

    private static readonly Color ValidColor = new Color(0f, 1f, 0f, 0.5f);
    private static readonly Color InvalidColor = new Color(1f, 0f, 0f, 0.5f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Toggle()
    {
        if (isActive == false)      // we are turning it on
        {
            CreateGhostObject();
            isActive = true;
        }
        else
        {                           // we are turning it off
            Destroy(PreviewObject);
            PreviewObject = null;
            PreviewCollider = null;

            if (ghostMaterialInstance != null)
            {
                Destroy(ghostMaterialInstance);
                ghostMaterialInstance = null;
            }

            isActive = false;
        }
    }

    public void Deactivate()
    {
        Destroy(PreviewObject);
        PreviewObject = null;
        PreviewCollider = null;

        if (ghostMaterialInstance != null)
        {
            Destroy(ghostMaterialInstance);
            ghostMaterialInstance = null;
        }

        isActive = false;
    }

    private void Update()
    {
        if (isActive == false) return;

        UpdateGhostPosition();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceObject();
        }
    }

    void CreateGhostObject()
    {
        PreviewObject = Instantiate(PlacingObject);

        PreviewCollider = PreviewObject.GetComponentInChildren<Collider>();

        if (PreviewCollider != null)
        {
            PreviewCollider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"BuildingGridSystem: {PlacingObject.name} has no Collider, overlap checks will be skipped.");
        }

        Shader shader = ghostShader != null ? ghostShader : Shader.Find("Universal Render Pipeline/Unlit");
        ghostMaterialInstance = new Material(shader);
        ghostMaterialInstance.SetColor("_BaseColor", ValidColor);

        Renderer[] renderers = PreviewObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] ghostMats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = ghostMaterialInstance;
            }
            renderer.sharedMaterials = ghostMats;
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, GroundLayerMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 point = hitInfo.point;

            Vector3 snappedPosition = new Vector3(                      // snap X and Z, derive Y from the terrain
                Mathf.Round(point.x / GridSize) * GridSize,
                point.y,
                Mathf.Round(point.z / GridSize) * GridSize
            );

            if (ProceduralFloor.Instance != null)
            {
                snappedPosition.y = ProceduralFloor.Instance.SampleHeight(snappedPosition);
            }
            else
            {
                // Fallback if no floor is present, so this still behaves sensibly on a flat plane.
                snappedPosition.y = Mathf.Round(point.y / GridSize) * GridSize;
            }

            PreviewObject.transform.position = snappedPosition;

            isPlacementValid = !IsOverlappingObstacle();
            SetGhostColour(isPlacementValid ? ValidColor : InvalidColor);
        }
    }

    bool IsOverlappingObstacle()
    {
        if (PreviewCollider == null) return false;

        Bounds bounds = PreviewCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            PreviewObject.transform.rotation,
            NatureLayerMask,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in overlaps)
        {
            if (col.transform.IsChildOf(PreviewObject.transform)) continue; // Ignore the preview object's own collider(s)
            return true;
        }

        return false;
    }

    void SetGhostColour(Color color)
    {
        if (ghostMaterialInstance != null)
        {
            ghostMaterialInstance.SetColor("_BaseColor", color);
        }
    }

    void PlaceObject()
    {
        if (!isPlacementValid)
        {
            return; // its blocked by something, cancel the input
        }

        Vector3 placementPos = PreviewObject.transform.position;
        GameObject placed = Instantiate(PlacingObject, placementPos, PreviewObject.transform.rotation);
    }
}