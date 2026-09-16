using UnityEngine;
using System.Collections;

public class UISlide : MonoBehaviour
{
    [Header("Settings")]
    public RectTransform panel;
    public Vector2 hiddenPosition;
    public Vector2 shownPosition;
    public float duration = 0.3f;

    private Coroutine slideCoroutine;

    private void OnEnable()
    {
        // Start at the hidden position
        panel.anchoredPosition = hiddenPosition;

        // Slide into view
        slideCoroutine = StartCoroutine(Slide(shownPosition));
    }

    public void Deactivate()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideOutAndDisable());
    }

    private IEnumerator SlideOutAndDisable()
    {
        yield return Slide(hiddenPosition);

        // Disable the GameObject after sliding out
        gameObject.SetActive(false);
    }

    private IEnumerator Slide(Vector2 targetPosition)
    {
        Vector2 startPosition = panel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            panel.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        panel.anchoredPosition = targetPosition;
    }
}
