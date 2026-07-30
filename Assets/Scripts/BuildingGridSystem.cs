using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildingGridSystem : MonoBehaviour
{
    public static BuildingGridSystem Instance { get; private set; }

    public GameObject PlacingObject;
    public float GridSize;

    [Tooltip("Layers that count as obstacles (bushes, plants, etc). The preview will be blocked from placing if it overlaps anything on these layers.")]
    [SerializeField] private LayerMask NatureLayerMask;     // the layer that the tree's and rocks are on

    [Header("Ghost Preview")]
    [Tooltip("Shader used for the placement ghost. Pick something simple whose properties you control, e.g. 'Universal Render Pipeline/Unlit'. If left empty, URP Unlit is used automatically.")]
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

    public void Activate()
    {
        CreateGhostObject();
        isActive = true;
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

        // Rather than trying to figure out every building's shader property names (which is
        // what kept breaking with Toon_URP etc), every renderer on the preview gets swapped
        // onto one ghost material whose shader WE chose, so we always know it exposes
        // "_BaseColor". The building's real materials are untouched — this only ever runs on
        // the throwaway preview instance, never on the object actually placed in PlaceObject().
        Shader shader = ghostShader != null ? ghostShader : Shader.Find("Universal Render Pipeline/Unlit");
        ghostMaterialInstance = new Material(shader);
        ConfigureGhostTransparency(ghostMaterialInstance);
        ghostMaterialInstance.SetColor("_BaseColor", ValidColor);

        Renderer[] renderers = PreviewObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Every material slot becomes the same ghost material instance, so one SetColor
            // call later (in SetGhostColour) updates every mesh on the preview at once.
            Material[] ghostMats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = ghostMaterialInstance;
            }
            renderer.sharedMaterials = ghostMats;
        }
    }

    static void ConfigureGhostTransparency(Material mat)
    {
        // URP Lit/Unlit convention: _Surface 1 = Transparent, _Blend 0 = alpha blend.
        // Guarded with HasProperty in case someone points ghostShader at something unusual.
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Vector3 point = hitInfo.point;

            // Snap X/Z to the grid. Y is deliberately NOT derived from rounding the raw
            // raycast hit anymore — on sloped/uneven terrain that caused the preview to
            // shake, because a tiny sub-pixel mouse move could shift the raycast hit height
            // just enough to flip the rounded value to the next GridSize step and back,
            // every frame. Instead we snap X/Z first, then ask the terrain for the exact
            // height at that grid cell — which only changes when the snapped cell changes,
            // not on every mouse jitter.
            Vector3 snappedPosition = new Vector3(
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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())                   // so it doesn't fire when clicking on a ui button
            return;

        Vector3 placementPos = PreviewObject.transform.position;
        GameObject placed = Instantiate(PlacingObject, placementPos, PreviewObject.transform.rotation);
    }
}