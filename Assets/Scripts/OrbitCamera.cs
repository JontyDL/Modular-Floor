using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public static OrbitCamera Instance { get; private set; }
    [Header("Target")]
    [Tooltip("The point the camera orbits around.")]
    public Transform target;
    [Header("Orbit Settings")]
    [Tooltip("Degrees orbited per unit of mouse delta, per second.")]
    public float orbitSpeed = 20f;
    [Tooltip("If true, the camera keeps looking at the target while orbiting.")]
    public bool lookAtTarget = true;
    [Header("Cursor")]
    [Tooltip("Cursor shown while orbiting (right mouse button held). Reverts to whatever cursor was showing before when released.")]
    [SerializeField] private Texture2D orbitCursorTexture;
    [SerializeField] private Vector2 orbitCursorHotspot = Vector2.zero;
    // Offset from target to camera. Its length IS the orbit radius.
    // Calculated once in Start() and only ever rotated afterwards, never resized.
    private Vector3 offset;
    private bool isOrbiting;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(OrbitCamera)}: No target assigned on '{name}'. Disabling script.");
            enabled = false;
            return;
        }
        if (Mouse.current == null)
        {
            Debug.LogWarning($"{nameof(OrbitCamera)}: No mouse device detected. Disabling script.");
            enabled = false;
            return;
        }
        // Capture the initial offset (radius + height) right after Start.
        offset = transform.position - target.position;
    }
    void Update()
    {
        if (target == null || Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            StartOrbiting();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            StopOrbiting();
        }

        // Right mouse button held down.
        if (Mouse.current.rightButton.isPressed)
        {
            float mouseX = Mouse.current.delta.ReadValue().x;
            if (Mathf.Abs(mouseX) > Mathf.Epsilon)
            {
                float angleDelta = mouseX * orbitSpeed * Time.deltaTime;
                // Rotate the stored offset around the world up axis.
                // Rotation preserves the vector's length, so the radius
                // never needs to be recomputed.
                offset = Quaternion.AngleAxis(angleDelta, Vector3.up) * offset;
                transform.position = target.position + offset;
                if (lookAtTarget)
                {
                    transform.LookAt(target);
                }
            }
        }
    }

    private void StartOrbiting()
    {
        if (isOrbiting) return;
        isOrbiting = true;
        InteractionModeManager.Instance?.PushCursorOverride(orbitCursorTexture, orbitCursorHotspot);
    }

    private void StopOrbiting()
    {
        if (!isOrbiting) return;
        isOrbiting = false;
        InteractionModeManager.Instance?.PopCursorOverride();
    }

    private void OnDisable()
    {
        // Safety net: if this script is disabled (or the object destroyed) mid-orbit,
        // make sure the cursor override doesn't get stuck.
        StopOrbiting();
    }
}