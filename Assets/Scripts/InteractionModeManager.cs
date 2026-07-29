using UnityEngine;
using System;

public class InteractionModeManager : MonoBehaviour
{
    public static InteractionModeManager Instance { get; private set; }

    [SerializeField] private InteractionMode startingMode = InteractionMode.None;

    public InteractionMode CurrentMode { get; private set; }

    public event Action<InteractionMode> OnModeChanged;              // subscribeable event, for later use. i.e the tutorial

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        CurrentMode = startingMode;
    }

    public void SetMode(InteractionMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnModeChanged?.Invoke(CurrentMode);
    }

    public void ToggleMode(InteractionMode mode)            // for toggling in and out of a mode using UI buttons
    {
        SetMode(CurrentMode == mode ? InteractionMode.None : mode);
    }

    public bool IsInMode(InteractionMode mode) => CurrentMode == mode;
}
