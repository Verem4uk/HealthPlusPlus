#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VikingThemeInstaller
{
    private const string Folder = "Assets/Images/Theme/";
    private static Sprite Frame;
    private static Sprite Glow;
    private static Texture2D Green;
    private static Font Font;

    public static void PrepareAssets()
    {
        AssetDatabase.Refresh();
        ConfigureTexture("forest-green.png", false);
        ConfigureTexture("forged-frame.png", true);
        string glowPath = Folder + "sun-glow.png";
        if (!File.Exists(glowPath))
        {
            // A soft radial UI primitive; no changes to the existing shield artwork.
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * size * .5f) / (size * .5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - distance), 1.4f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(glowPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(glowPath);
        }
        ConfigureTexture("sun-glow.png", true, false);
        Green = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "forest-green.png");
        Frame = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "forged-frame.png");
        Glow = AssetDatabase.LoadAssetAtPath<Sprite>(glowPath);
        Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void ConfigureTexture(string name, bool sprite, bool sliced = true)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + name);
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = sprite;
        importer.wrapMode = sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (sprite && sliced)
        {
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spriteBorder = new Vector4(width * .18f, height * .18f, width * .18f, height * .18f);
        }
        importer.SaveAndReimport();
    }

    public static void Configure(Canvas canvas, PortraitTrainingLayout layout)
    {
        var theme = canvas.GetComponent<VikingThemeView>() ?? canvas.gameObject.AddComponent<VikingThemeView>();
        var backdrop = Child(canvas.transform, "ThemeBackdrop");
        Stretch(backdrop);
        backdrop.SetAsFirstSibling();
        var raw = Component<RawImage>(backdrop);
        raw.texture = Green;
        raw.color = Color.white;
        raw.raycastTarget = false;
        raw.uvRect = new Rect(0, 0, 2, 2);
        theme.Backdrop = raw;
        for (int side = 0; side < 2; side++)
        {
            var rect = Child(backdrop, side == 0 ? "LeftOrnament" : "RightOrnament");
            rect.anchorMin = new Vector2(side, 0);
            rect.anchorMax = new Vector2(side, 1);
            rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(42, -60);
            rect.anchoredPosition = new Vector2(side == 0 ? 30 : -30, 0);
            var ornament = Component<VikingOrnament>(rect);
            ornament.Vertical = true;
            ornament.color = new Color(.61f, .57f, .33f, .18f);
            ornament.raycastTarget = false;
        }

        if (layout.IsMenu)
        {
            var menu = canvas.GetComponent<MainMenuTransitionView>();
            layout.MenuArtwork = Child(layout.Content, "MenuArtwork");
            layout.MenuArtwork.SetAsFirstSibling();
            layout.Background.transform.SetParent(layout.MenuArtwork, false);
            layout.Background.transform.SetAsFirstSibling();
            Stretch(layout.Background.rectTransform);
            layout.Background.color = Color.white;
            layout.Background.preserveAspect = true;
            if (layout.BackgroundAspect != null) layout.BackgroundAspect.enabled = false;
            var video = Field<RawImage>(menu, "VideoImage");
            video.transform.SetParent(layout.MenuArtwork, false);
            Stretch(video.rectTransform);
            video.raycastTarget = false;
            var aspect = video.GetComponent<AspectRatioFitter>();
            if (aspect != null) aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            layout.Title.SetParent(layout.MenuArtwork, false);
            layout.Title.GetComponent<Image>().raycastTarget = false;
            Border(layout.MenuArtwork, 13);
            var glow = Child(layout.Content, "MenuSunGlow");
            glow.SetSiblingIndex(layout.MenuButton.GetSiblingIndex());
            var glowImage = Component<Image>(glow);
            glowImage.sprite = Glow;
            glowImage.color = new Color(1, .73f, .23f, .15f);
            glowImage.raycastTarget = false;
            layout.MenuGlow = glow;
            theme.MenuGlow = glowImage;
            theme.Menu = menu;
            var play = layout.MenuButton.GetComponent<Button>();
            play.image.preserveAspect = true;
            Component<VikingButtonFeedback>(layout.MenuButton);
            layout.MenuButton.SetAsLastSibling();
        }
        else
        {
            layout.Background.color = Color.clear;
            layout.Background.raycastTarget = false;
            var presentation = canvas.GetComponent<ExercisePresentation>() ?? canvas.gameObject.AddComponent<ExercisePresentation>();
            var title = Label(layout.Content, "ExerciseTitle", 62, FontStyle.Bold);
            presentation.Title = title;
            title.text = "";
            layout.ExerciseTitle = title.rectTransform;
            var unit = Label(layout.Content, "ExerciseUnit", 36);
            unit.color = new Color(.76f, .73f, .59f);
            presentation.Unit = unit;
            layout.ExerciseUnit = unit.rectTransform;
            Border(layout.Media, 14);
            presentation.Fail = Action(layout.Fail, false);
            presentation.Success = Action(layout.Success, true);
            presentation.Start = Action(layout.TimerStart, true);
            var overlay = layout.Regression.GetComponent<Image>();
            overlay.sprite = null;
            overlay.color = Color.clear;
            var regressionBackground = Child(layout.Regression, "ThemeBackdrop");
            Stretch(regressionBackground);
            regressionBackground.SetAsFirstSibling();
            var regressionTexture = Component<RawImage>(regressionBackground);
            regressionTexture.texture = Green;
            regressionTexture.raycastTarget = false;
            regressionTexture.color = new Color(.72f, .79f, .70f);
            regressionTexture.uvRect = new Rect(0, 0, 1, 2);
            Border(layout.Regression, -20);
            foreach (var control in new[] { layout.RegressionMinus, layout.RegressionPlus })
            {
                var image = control.GetComponent<Image>();
                image.sprite = null;
                image.color = new Color(.09f, .15f, .10f);
                image.type = Image.Type.Simple;
                Border(control, 0).pixelsPerUnitMultiplier = 4;
            }
            layout.RegressionTitle.GetComponent<Image>().enabled = false;
            presentation.RegressionTitle = Label(layout.RegressionTitle, "ThemeTitle", 56, FontStyle.Bold);
            Stretch(presentation.RegressionTitle.rectTransform, 12);
            presentation.Confirm = Label(layout.RegressionConfirm, "ThemeCaption", 40);
            var confirm = presentation.Confirm.rectTransform;
            confirm.anchorMin = confirm.anchorMax = new Vector2(.5f, 0);
            confirm.anchoredPosition = new Vector2(0, -30);
            confirm.sizeDelta = new Vector2(650, 72);
            presentation.RefreshLabels();
            layout.Regression.SetAsLastSibling();
        }
    }

    private static Text Action(RectTransform rect, bool primary)
    {
        var button = rect.GetComponent<Button>();
        var icon = Child(rect, "ThemeIcon");
        var image = Component<Image>(icon);
        if (image.sprite == null) image.sprite = button.image.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        icon.anchorMin = icon.anchorMax = new Vector2(0, .5f);
        icon.anchoredPosition = new Vector2(80, 0);
        icon.sizeDelta = new Vector2(126, 126);
        foreach (var text in rect.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
        foreach (var text in rect.GetComponentsInChildren<Text>(true))
            if (text.name != "ThemeCaption") text.gameObject.SetActive(false);
        button.image.sprite = null;
        button.image.color = primary ? new Color(.13f, .23f, .13f, .98f) : new Color(.075f, .09f, .075f, .98f);
        button.image.type = Image.Type.Simple;
        button.image.raycastTarget = true;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.12f, .98f);
        colors.pressedColor = new Color(.72f, .72f, .63f);
        colors.disabledColor = new Color(.5f, .5f, .5f, .6f);
        button.colors = colors;
        var border = Border(rect, 0);
        border.pixelsPerUnitMultiplier = 4;
        border.color = primary ? new Color(1, .91f, .69f) : new Color(.8f, .8f, .74f);
        var label = Label(rect, "ThemeCaption", 39, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(153, 16);
        label.rectTransform.offsetMax = new Vector2(-22, -16);
        label.transform.SetAsLastSibling();
        return label;
    }

    public static Image Border(Transform parent, float padding)
    {
        var rect = Child(parent, "ForgedBorder");
        Stretch(rect, -padding);
        rect.SetAsLastSibling();
        var image = Component<Image>(rect);
        image.sprite = Frame;
        image.type = Image.Type.Sliced;
        image.fillCenter = false;
        image.pixelsPerUnitMultiplier = 2.7f;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(Transform parent, string name, int size, FontStyle style = FontStyle.Normal)
    {
        var text = Component<Text>(Child(parent, name));
        text.font = Font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = new Color(.95f, .90f, .78f);
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(22, size * 2 / 3);
        text.resizeTextMaxSize = size;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static T Field<T>(Object obj, string name) where T : Object =>
        (T)new SerializedObject(obj).FindProperty(name).objectReferenceValue;
    private static T Component<T>(RectTransform rect) where T : Component => rect.GetComponent<T>() ?? rect.gameObject.AddComponent<T>();
    private static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return (RectTransform)found;
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    private static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one * .5f;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
        rect.localScale = Vector3.one;
    }
}
#endif
