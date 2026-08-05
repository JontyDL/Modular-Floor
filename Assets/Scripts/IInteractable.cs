using UnityEditor;
using UnityEngine;

public enum InteractionMode             // the different actions I'll be able to take
{
    None,
    Destroy,
    Build
}

public interface IInteractable
{
    void OnInteract(InteractionMode mode);
}
