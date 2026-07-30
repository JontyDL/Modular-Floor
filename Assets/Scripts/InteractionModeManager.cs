using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ModeCursor
{
    public InteractionMode mode;
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;
}

public class InteractionModeManager : MonoBehaviour
{
    public static InteractionModeManager Instance { get; private set; }

    [SerializeField] private InteractionMode startingMode = InteractionMode.None;

    [Header("Cursors")]
    [SerializeField] private Texture2D defaultCursorTexture;
    [SerializeField] private Vector2 defaultCursorHotspot = Vector2.zero;
    [SerializeField] private List<ModeCursor> modeCursors = new List<ModeCursor>();

    private Dictionary<InteractionMode, ModeCursor> _cursorLookup;

    private Texture2D _activeCursorTexture;
    private Vector2 _activeCursorHotspot;
    private readonly Stack<(Texture2D texture, Vector2 hotspot)> _cursorOverrideStack = new();

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

        BuildCursorLookup();
        CurrentMode = startingMode;
        ApplyCursor(CurrentMode);
    }

    private void BuildCursorLookup()
    {
        _cursorLookup = new Dictionary<InteractionMode, ModeCursor>();
        foreach (var entry in modeCursors)
        {
            if (entry == null) continue;
            if (_cursorLookup.ContainsKey(entry.mode))
            {
                Debug.LogWarning($"InteractionModeManager: duplicate cursor entry for mode {entry.mode}, ignoring extra.");
                continue;
            }
            _cursorLookup.Add(entry.mode, entry);
        }
    }

    public void SetMode(InteractionMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        ApplyCursor(CurrentMode);
        OnModeChanged?.Invoke(CurrentMode);
    }

    public void ToggleMode(InteractionMode mode)            // for toggling in and out of a mode using UI buttons
    {
        SetMode(CurrentMode == mode ? InteractionMode.None : mode);
    }

    public bool IsInMode(InteractionMode mode) => CurrentMode == mode;

    private void ApplyCursor(InteractionMode mode)
    {
        if (mode != InteractionMode.None
            && _cursorLookup.TryGetValue(mode, out var entry)
            && entry.cursorTexture != null)
        {
            SetCursor(entry.cursorTexture, entry.hotspot);
        }
        else
        {
            // None, or a mode with no assigned texture, uses the default (null texture = OS cursor).
            SetCursor(defaultCursorTexture, defaultCursorHotspot);
        }
    }

    private void SetCursor(Texture2D texture, Vector2 hotspot)
    {
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _activeCursorTexture = texture;
        _activeCursorHotspot = hotspot;
    }

    public void PushCursorOverride(Texture2D texture, Vector2 hotspot = default)                // temporary mouse cursor override
    {
        _cursorOverrideStack.Push((_activeCursorTexture, _activeCursorHotspot));
        SetCursor(texture, hotspot);
    }

    public void PopCursorOverride()
    {
        if (_cursorOverrideStack.Count > 0)
        {
            var (texture, hotspot) = _cursorOverrideStack.Pop();
            SetCursor(texture, hotspot);
        }
        else
        {
            ApplyCursor(CurrentMode);
        }
    }
}