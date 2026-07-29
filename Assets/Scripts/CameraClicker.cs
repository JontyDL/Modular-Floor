using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CameraClicker : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private LayerMask interactableLayers = ~0; // everything by default
    [SerializeField] private float maxDistance = 100f;

    [SerializeField] private InputActionReference clickActionReference;

    private InputAction _clickAction;
    private bool _ownsAction;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (clickActionReference != null && clickActionReference.action != null)
        {
            _clickAction = clickActionReference.action;
        }
        else
        {
            _clickAction = new InputAction(name: "Click", type: InputActionType.Button, binding: "<Mouse>/leftButton");             // setting the default to left click if i don't make a mobile build...
            _ownsAction = true;
        }
    }

    private void OnEnable()         // using Unity's input system, so clicking is event driven, not poll'd every frame
    {
        _clickAction.performed += OnClickPerformed;
        _clickAction.Enable();
    }

    private void OnDisable()
    {
        _clickAction.performed -= OnClickPerformed;
        _clickAction.Disable();
    }

    private void OnDestroy()
    {
        if (_ownsAction)
        {
            _clickAction.Dispose();
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (InteractionModeManager.Instance == null || targetCamera == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())                   // so it doesn't fire when clicking on a ui button
            return;

        Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.OnInteract(InteractionModeManager.Instance.CurrentMode);
            }
        }
    }
}
