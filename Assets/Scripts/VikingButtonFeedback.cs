using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tint only: the menu owns the shield's rotation and pulsing scale.
[RequireComponent(typeof(Button))]
public sealed class VikingButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private Button Button;
    private bool Hovered;
    private bool Pressed;
    private void Awake() => Button = GetComponent<Button>();
    public void OnPointerEnter(PointerEventData data) { Hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { Hovered = Pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData data) { Pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData data) { Pressed = false; Refresh(); }
    private void OnDisable()
    {
        Hovered = Pressed = false;
        if (Button != null && Button.image != null) Button.image.color = Color.white;
    }
    private void Refresh()
    {
        if (Button == null || Button.image == null) return;
        Button.image.color = !Button.interactable ? Color.white : Pressed
            ? new Color(.83f, .78f, .66f) : Hovered ? new Color(1, .94f, .78f) : Color.white;
    }
}
