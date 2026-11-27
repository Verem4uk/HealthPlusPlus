using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class VikingThemeView : MonoBehaviour
{
    public RawImage Backdrop;
    public Image MenuGlow;
    public MainMenuTransitionView Menu;
    private Vector2 LastSize;
    private void LateUpdate()
    {
        if (Backdrop != null && Backdrop.texture != null)
        {
            var size = Backdrop.rectTransform.rect.size;
            if (size != LastSize)
            {
                LastSize = size;
                Backdrop.uvRect = new Rect(0, 0, size.x / 1300, size.y / 1300);
            }
        }
        if (MenuGlow == null) return;
        MenuGlow.enabled = !Application.isPlaying || (Menu != null && Menu.CanStartTraining);
        float pulse = Application.isPlaying ? Mathf.Sin(Time.unscaledTime * Mathf.PI) * .012f : 0;
        MenuGlow.color = new Color(1, .73f, .23f, .15f + pulse);
    }
}
