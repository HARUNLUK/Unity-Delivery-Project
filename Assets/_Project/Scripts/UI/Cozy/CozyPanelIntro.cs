using System.Collections;
using UnityEngine;

/// <summary>
/// Soft open animation for cards and toasts: a slight grow (or slide) and fade.
/// Uses unscaled time so it also plays while the game is paused (tablet, menus).
/// </summary>
[DisallowMultipleComponent]
public class CozyPanelIntro : MonoBehaviour
{
    public enum Style { Grow, SlideDown }

    public Style style = Style.Grow;
    public float duration = 0.18f;
    public float slideDistance = 40f;

    private CanvasGroup group;
    private RectTransform rect;
    private Vector2 restPosition;
    private bool hasRestPosition;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (!hasRestPosition)
        {
            restPosition = rect.anchoredPosition;
            hasRestPosition = true;
        }
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void OnDisable()
    {
        Apply(1f);
    }

    private IEnumerator Play()
    {
        float t = 0f;
        Apply(0f);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            Apply(1f - (1f - k) * (1f - k) * (1f - k)); // ease-out cubic
            yield return null;
        }
        Apply(1f);
    }

    private void Apply(float k)
    {
        if (group != null) group.alpha = k;
        if (rect == null) return;

        if (style == Style.Grow)
        {
            float s = Mathf.Lerp(0.96f, 1f, k);
            rect.localScale = new Vector3(s, s, 1f);
        }
        else if (hasRestPosition)
        {
            rect.anchoredPosition = restPosition + new Vector2(0f, (1f - k) * slideDistance);
        }
    }
}
