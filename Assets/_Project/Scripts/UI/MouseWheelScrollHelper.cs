using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(ScrollRect))]
public class MouseWheelScrollHelper : MonoBehaviour, IScrollHandler
{
    private ScrollRect scrollRect;
    [Tooltip("Scroll sensitivity multiplier for mouse wheel")]
    public float scrollSpeed = 0.25f;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        if (scrollRect == null || !scrollRect.gameObject.activeInHierarchy) return;

        float scrollDelta = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 s = Mouse.current.scroll.ReadValue();
            scrollDelta = s.y;
            // Normalizing New Input System scroll delta (which is usually +-120 per notch)
            if (Mathf.Abs(scrollDelta) > 1f)
            {
                scrollDelta /= 120f;
            }
        }
#else
        scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) < 0.001f)
        {
            scrollDelta = Input.GetAxis("Mouse ScrollWheel") * 10f;
        }
#endif

        if (Mathf.Abs(scrollDelta) > 0.001f)
        {
            if (scrollRect.content != null && scrollRect.viewport != null)
            {
                float contentH = scrollRect.content.rect.height;
                float viewportH = scrollRect.viewport.rect.height;
                float maxScroll = Mathf.Max(0f, contentH - viewportH);

                if (maxScroll > 1f)
                {
                    Vector2 curPos = scrollRect.content.anchoredPosition;
                    // Wheel Down (negative delta) moves content UP (increasing y)
                    curPos.y = Mathf.Clamp(curPos.y - scrollDelta * (scrollSpeed * 60f), 0f, maxScroll);
                    scrollRect.content.anchoredPosition = curPos;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollDelta * scrollSpeed);
                }
            }
            else
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollDelta * scrollSpeed);
            }
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect == null) return;
        float deltaY = eventData.scrollDelta.y;
        if (Mathf.Abs(deltaY) > 0.001f)
        {
            if (scrollRect.content != null && scrollRect.viewport != null)
            {
                float maxScroll = Mathf.Max(0f, scrollRect.content.rect.height - scrollRect.viewport.rect.height);
                if (maxScroll > 1f)
                {
                    Vector2 curPos = scrollRect.content.anchoredPosition;
                    curPos.y = Mathf.Clamp(curPos.y - deltaY * (scrollSpeed * 60f), 0f, maxScroll);
                    scrollRect.content.anchoredPosition = curPos;
                }
            }
        }
    }
}
