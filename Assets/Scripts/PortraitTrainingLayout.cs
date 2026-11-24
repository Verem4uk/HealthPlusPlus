using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, DefaultExecutionOrder(-100)]
public sealed class PortraitTrainingLayout : MonoBehaviour
{
    public RectTransform SafeArea;
    public RectTransform Content;
    public bool IsMenu;
    public RectTransform Title;
    public RectTransform MenuButton;
    public RectTransform Numbers;
    public RectTransform Media;
    public RectTransform Success;
    public RectTransform Fail;
    public RectTransform TimerStart;
    public RectTransform Regression;
    public RectTransform RegressionTitle;
    public RectTransform RegressionNumbers;
    public RectTransform RegressionMinus;
    public RectTransform RegressionPlus;
    public RectTransform RegressionConfirm;
    public Image Background;
    public AspectRatioFitter BackgroundAspect;

    private Vector2 LastViewport;
    private Rect LastSafeArea;
    private Vector2 LastContentSize;

    private void OnEnable() => Apply(new Vector2(Screen.width, Screen.height), Screen.safeArea);

    private void LateUpdate()
    {
        var viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != LastViewport || Screen.safeArea != LastSafeArea)
            Apply(viewport, Screen.safeArea);
        if (Content != null && SafeArea != null)
        {
            float width = Mathf.Min(1080, SafeArea.rect.width);
            if (Mathf.Abs(Content.rect.width - width) > .01f)
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
        if (Content != null && Content.rect.size != LastContentSize) Refresh();
        if (BackgroundAspect != null && Background != null && Background.sprite != null)
            BackgroundAspect.aspectRatio = Background.sprite.rect.width / Background.sprite.rect.height;
    }

    public void Apply(Vector2 viewport, Rect safeArea)
    {
        if (SafeArea == null || Content == null || viewport.x <= 0 || viewport.y <= 0) return;
        LastViewport = viewport;
        LastSafeArea = safeArea;
        if (safeArea.width <= 0 || safeArea.height <= 0) safeArea = new Rect(Vector2.zero, viewport);
        SafeArea.anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / viewport.x), Mathf.Clamp01(safeArea.yMin / viewport.y));
        SafeArea.anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / viewport.x), Mathf.Clamp01(safeArea.yMax / viewport.y));
        SafeArea.offsetMin = SafeArea.offsetMax = Vector2.zero;
        Content.anchorMin = new Vector2(.5f, 0);
        Content.anchorMax = new Vector2(.5f, 1);
        Content.pivot = Vector2.one * .5f;
        Content.anchoredPosition = Vector2.zero;
        Content.sizeDelta = new Vector2(Mathf.Min(1080, SafeArea.rect.width), 0);
        Refresh();
    }

    public void Refresh()
    {
        if (Content == null) return;
        Refresh(Content.rect.size);
    }

    public void Refresh(Vector2 size)
    {
        LastContentSize = size;
        if (size.x <= 0 || size.y <= 0) return;
        if (IsMenu)
        {
            var titleWidth = Mathf.Min(790.7f, size.x - 120);
            Set(Title, new Vector2(.5f, 1), new Vector2(0, -260), new Vector2(titleWidth, titleWidth * 342.6f / 790.7f));
            // Keep the authored button scale for its pulse and half-turn animation.
            var buttonWidth = Mathf.Min(1169, (size.x - 140) * 2);
            float radius = buttonWidth * .25f * Mathf.Sqrt(1 + Mathf.Pow(1203f / 1169, 2));
            Set(MenuButton, new Vector2(.5f, 0), new Vector2(0, Mathf.Max(344, radius * 1.06f + 48)), new Vector2(buttonWidth, buttonWidth * 1203 / 1169));
            return;
        }

        float header = Mathf.Min(420, size.y * .20f);
        float button = Mathf.Min(280, size.x * .26f);
        Set(Numbers, new Vector2(.5f, 1), new Vector2(0, -32 - header * .5f), new Vector2(Mathf.Min(760, size.x - 120), header));
        Media.anchorMin = new Vector2(0, 0);
        Media.anchorMax = Vector2.one;
        Media.offsetMin = new Vector2(40, 440);
        Media.offsetMax = new Vector2(-40, -header - 80);
        Set(Fail, new Vector2(.25f, 0), new Vector2(0, 220), new Vector2(button, button));
        Set(Success, new Vector2(.75f, 0), new Vector2(0, 220), new Vector2(button, button));
        Set(TimerStart, new Vector2(.5f, 0), new Vector2(0, 220), new Vector2(button, button));
        float bannerWidth = Mathf.Min(1000, size.x - 80);
        float bannerHeight = Mathf.Min(bannerWidth * 764.4f / 1109.8f, size.y * .38f);
        Set(RegressionTitle, new Vector2(.5f, 1), new Vector2(0, -60 - bannerHeight * .5f), new Vector2(bannerWidth, bannerHeight));
        float row = -size.y * .08f;
        Set(RegressionNumbers, new Vector2(.5f, .5f), new Vector2(0, row), new Vector2(Mathf.Min(480, size.x * .46f), 220));
        Set(RegressionMinus, new Vector2(.5f, .5f), new Vector2(-size.x * .36f, row), new Vector2(160, 160));
        Set(RegressionPlus, new Vector2(.5f, .5f), new Vector2(size.x * .36f, row), new Vector2(160, 160));
        Set(RegressionConfirm, new Vector2(.5f, 0), new Vector2(0, 240), new Vector2(360, 360));
    }

    private static void Set(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
