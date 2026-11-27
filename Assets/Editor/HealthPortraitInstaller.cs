#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HealthPortraitInstaller
{
    [MenuItem("Health++/Apply portrait layout")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Configure(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void Configure(Scene scene)
    {
        VikingThemeInstaller.PrepareAssets();
        foreach (var canvas in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
        {
            if (!canvas.isRootCanvas) continue;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var frame = canvas.GetComponent<PortraitTrainingLayout>() ?? canvas.gameObject.AddComponent<PortraitTrainingLayout>();
            frame.IsMenu = canvas.name == "StartCanvas";
            frame.SafeArea = Child(canvas.transform, "SafeArea");
            Stretch(frame.SafeArea);
            frame.Content = Child(frame.SafeArea, "PortraitContent");
            Stretch(frame.Content);
            var background = frame.Background != null ? frame.Background.rectTransform : canvas.transform.Find("Panel").GetComponent<RectTransform>();
            background.SetAsFirstSibling();
            var backgroundImage = background.GetComponent<Image>();
            frame.Background = backgroundImage;
            backgroundImage.raycastTarget = false;
            if (frame.IsMenu)
            {
                var menu = canvas.GetComponent<MainMenuTransitionView>();
                frame.Title = frame.Title != null ? frame.Title : background.Find("Title").GetComponent<RectTransform>();
                frame.MenuButton = Field<Button>(menu, "PlayButton").GetComponent<RectTransform>();
                frame.Title.SetParent(frame.Content, false);
                frame.MenuButton.SetParent(frame.Content, false);
                Stretch(background);
                frame.BackgroundAspect = background.GetComponent<AspectRatioFitter>() ?? background.gameObject.AddComponent<AspectRatioFitter>();
                frame.BackgroundAspect.aspectRatio = backgroundImage.sprite.rect.width / backgroundImage.sprite.rect.height;
                frame.BackgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                backgroundImage.preserveAspect = false; // The rect itself preserves the source ratio.
            }
            else
            {
                Stretch(background);
                backgroundImage.sprite = null;
                backgroundImage.color = new Color(.063f, .106f, .129f, 1);
                var media = canvas.GetComponentInChildren<ExerciseMediaView>(true);
                var timer = canvas.GetComponentInChildren<ExerciseTimerView>(true);
                var regression = canvas.GetComponentInChildren<RegressPanelView>(true);
                frame.Numbers = Field<NumbersView>(timer, "NumbersView").GetComponent<RectTransform>();
                frame.Success = Field<Button>(timer, "SuccessButton").GetComponent<RectTransform>();
                frame.Fail = Field<Button>(timer, "FailButton").GetComponent<RectTransform>();
                frame.TimerStart = Field<Button>(timer, "StartButton").GetComponent<RectTransform>();
                frame.Regression = regression.GetComponent<RectTransform>();
                foreach (var control in new[] { frame.Numbers, frame.Success, frame.Fail, frame.TimerStart, frame.Regression })
                    control.SetParent(frame.Content, false);
                Stretch(frame.Regression);
                var overlay = frame.Regression.GetComponent<Image>();
                overlay.sprite = null;
                overlay.color = new Color(.063f, .106f, .129f, 1);
                frame.RegressionTitle = frame.Regression.Find("Image").GetComponent<RectTransform>();
                frame.RegressionTitle.GetComponent<Image>().preserveAspect = true;
                frame.RegressionNumbers = Field<NumbersView>(regression, "NumbersView").GetComponent<RectTransform>();
                frame.RegressionMinus = Field<Button>(regression, "MinusButton").GetComponent<RectTransform>();
                frame.RegressionPlus = Field<Button>(regression, "PlusButton").GetComponent<RectTransform>();
                frame.RegressionConfirm = frame.Regression.Find("Button (2)").GetComponent<RectTransform>();
                frame.Media = Child(frame.Content, "ExerciseMediaFrame");
                frame.Media.SetAsFirstSibling();
                var fallback = Child(frame.Media, "Fallback");
                Stretch(fallback);
                var fallbackImage = fallback.GetComponent<Image>() ?? fallback.gameObject.AddComponent<Image>();
                fallbackImage.raycastTarget = false;
                fallbackImage.preserveAspect = true;
                var mediaSettings = new SerializedObject(media);
                mediaSettings.FindProperty("Background").objectReferenceValue = fallbackImage;
                mediaSettings.ApplyModifiedPropertiesWithoutUndo();
                var video = Field<RawImage>(media, "VideoImage");
                var aspect = video.GetComponent<AspectRatioFitter>();
                aspect.enabled = false;
                video.transform.SetParent(frame.Media, false);
                video.rectTransform.localScale = Vector3.one;
                Stretch(video.rectTransform);
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.enabled = true;
                frame.Regression.SetAsLastSibling();
            }
            VikingThemeInstaller.Configure(canvas, frame);
            frame.Apply(new Vector2(1080, 2400), new Rect(0, 0, 1080, 2400));
            frame.Content.sizeDelta = new Vector2(1080, 0);
            frame.Refresh(new Vector2(1080, 2400));
            EditorUtility.SetDirty(frame);
            EditorUtility.SetDirty(scaler);
        }
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static T Field<T>(Object instance, string name) where T : Object =>
        (T)new SerializedObject(instance).FindProperty(name).objectReferenceValue;

    private static RectTransform Child(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<RectTransform>();
        var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        child.SetParent(parent, false);
        return child;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one * .5f;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
