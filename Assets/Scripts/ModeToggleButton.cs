using UnityEngine;
using UnityEngine.UI;
public class ModeToggleButton : MonoBehaviour
{
    [SerializeField] private InteractionMode mode;
    [SerializeField] private GameObject activeIndicator;        // probably going to add a green outline when the mode is active

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
        if (InteractionModeManager.Instance != null)
        {
            InteractionModeManager.Instance.OnModeChanged += RefreshVisual;
            RefreshVisual(InteractionModeManager.Instance.CurrentMode);
        }
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
        if (InteractionModeManager.Instance != null)
            InteractionModeManager.Instance.OnModeChanged -= RefreshVisual;
    }

    private void HandleClick()
    {
        InteractionModeManager.Instance.ToggleMode(mode);
    }

    private void RefreshVisual(InteractionMode current)
    {
        if (activeIndicator != null)
            activeIndicator.SetActive(current == mode);
    }
}