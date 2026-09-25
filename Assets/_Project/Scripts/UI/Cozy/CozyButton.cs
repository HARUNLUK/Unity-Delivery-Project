using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tactile press feel for Kurye Defteri buttons: the face sinks onto its lip while pressed.
/// The lip is a UI Shadow effect on the button face; the label/icon live in the "Inner" child.
/// </summary>
[DisallowMultipleComponent]
public class CozyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform inner;
    public Shadow lip;
    public float lipDepth = CozyTheme.LipButton;
    public float pressDepth = 4f;

    private Selectable selectable;
    private bool pressed;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (inner == null)
        {
            Transform t = transform.Find("Inner");
            if (t != null) inner = (RectTransform)t;
        }
        if (lip == null) lip = GetComponent<Shadow>();
    }

    private void OnDisable()
    {
        SetPressed(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (selectable != null && !selectable.IsInteractable()) return;
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool value)
    {
        if (pressed == value) return;
        pressed = value;

        float offset = value ? -pressDepth : 0f;
        if (inner != null) inner.anchoredPosition = new Vector2(inner.anchoredPosition.x, offset);
        if (lip != null) lip.effectDistance = new Vector2(0f, -(value ? Mathf.Max(1f, lipDepth - pressDepth) : lipDepth));
    }
}
