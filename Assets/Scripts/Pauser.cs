using UnityEngine;
using UnityEngine.InputSystem;

public class Pauser : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private InputActionReference pauseActionReference;

    private void OnEnable()
    {
        if (pauseActionReference != null)
        {
            pauseActionReference.action.started += OnPauseActionTriggered;
            pauseActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseActionReference != null)
        {               
            pauseActionReference.action.started -= OnPauseActionTriggered;
            pauseActionReference.action.Disable();
        }
    }

    private void OnPauseActionTriggered(InputAction.CallbackContext context)
    {
        TriggerPause();
    }

    private void TriggerPause()
    {
        GameStateManager.Instance.ToggleGamePause();
        BuildingGridSystem.Instance.Deactivate();
    }
}
