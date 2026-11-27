#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HealthPortraitLayoutTests
{
    private static int Checks;
    private static readonly Vector2Int[] Profiles = {
        new Vector2Int(320, 568), new Vector2Int(360, 780), new Vector2Int(390, 844),
        new Vector2Int(430, 932), new Vector2Int(1080, 2400),
        new Vector2Int(768, 1024), new Vector2Int(820, 1180), new Vector2Int(1024, 1366),
        new Vector2Int(1366, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440)
    };

    public static async void Run()
    {
        try
        {
            HealthPortraitInstaller.Apply();
            int installedObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
            HealthPortraitInstaller.Apply();
            Check(installedObjects == UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Sum(root => root.GetComponentsInChildren<Transform>(true).Length), "Theme installation duplicates objects.");
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var frames = roots.SelectMany(root => root.GetComponentsInChildren<PortraitTrainingLayout>(true)).ToArray();
            Check(frames.Length == 2, "Expected both menu and training layouts.");
            var camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.063f, .106f, .129f, 1);
            camera.allowHDR = false;
            camera.cullingMask = -1;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.transform.rotation = Quaternion.identity;
            var menu = frames.First(frame => frame.IsMenu);
            var training = frames.First(frame => !frame.IsMenu);
            foreach (var frame in frames)
            {
                frame.gameObject.SetActive(true);
                var canvas = frame.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                frame.GetComponent<CanvasScaler>().enabled = false;
                frame.transform.localScale = Vector3.one;
                frame.transform.position = Vector3.zero;
                frame.transform.rotation = Quaternion.identity;
            }
            var timer = training.GetComponentInChildren<ExerciseTimerView>(true);
            var regression = training.GetComponentInChildren<RegressPanelView>(true);
            var mediaView = training.GetComponentInChildren<ExerciseMediaView>(true);
            var settings = new SerializedObject(mediaView);
            var video = (RawImage)settings.FindProperty("VideoImage").objectReferenceValue;
            var fallback = (Image)settings.FindProperty("Background").objectReferenceValue;
            var exercise = AssetDatabase.LoadAssetAtPath<ExerciseSO>("Assets/BD/Push-ups/0Knee Push-ups.asset");
            fallback.sprite = exercise.Icon;
            training.SetMediaAspect(exercise.Icon.rect.width / exercise.Icon.rect.height);
            training.GetComponent<ExercisePresentation>().Show(exercise.GetText(), false);
            var output = Path.GetFullPath("Logs/PortraitLayout");
            Directory.CreateDirectory(output);

            foreach (var profile in Profiles)
            {
                float scale = Mathf.Min(profile.x / 1080f, profile.y / 1920f);
                var logical = new Vector2(profile.x / scale, profile.y / scale);
                camera.aspect = profile.x / (float)profile.y;
                camera.orthographicSize = logical.y * .5f;
                foreach (var frame in frames) ((RectTransform)frame.transform).sizeDelta = logical;
                foreach (bool inset in new[] { false, true })
                {
                    var safe = inset ? new Rect(profile.x * .02f, profile.y * .035f, profile.x * .96f, profile.y * .91f)
                        : new Rect(0, 0, profile.x, profile.y);
                    foreach (var frame in frames) { frame.Apply(profile, safe); }
                    Canvas.ForceUpdateCanvases();
                    foreach (var frame in frames) { frame.Apply(profile, safe); frame.Refresh(); }
                    Canvas.ForceUpdateCanvases();
                    foreach (var frame in frames)
                    {
                        Check(frame.Content.rect.width <= 1080.1f, "Content stretches on a tablet.");
                        Check(Contains(frame.SafeArea.rect, Bounds(frame.Content, frame.SafeArea)), "Content leaves safe area.");
                    }
                    Check(Contains(menu.Content.rect, Bounds(menu.Title, menu.Content)), $"Title clipped: {profile}.");
                    Check(Contains(menu.Content.rect, Bounds(menu.MenuButton, menu.Content)), $"Menu button clipped: {profile}.");
                    Check(!Bounds(menu.Title, menu.Content).Overlaps(Bounds(menu.MenuButton, menu.Content)), "Menu controls overlap.");
                    Check(Contains(menu.Content.rect, Bounds(menu.MenuArtwork, menu.Content)), "Menu artwork clipped.");
                    Check(Mathf.Abs(menu.MenuArtwork.rect.width / menu.MenuArtwork.rect.height - menu.Background.sprite.rect.width / menu.Background.sprite.rect.height) < .001f, "Menu artwork stretched.");
                    foreach (var graphic in frames.SelectMany(frame => frame.GetComponentsInChildren<Graphic>(true))
                        .Where(graphic => graphic.name == "ForgedBorder" || graphic.name == "MenuSunGlow" || graphic is VikingOrnament))
                        Check(!graphic.raycastTarget, "Theme decoration blocks pointer input.");

                    timer.Show(false, 999999);
                    Canvas.ForceUpdateCanvases();
                    foreach (var control in new[] { training.Numbers, training.Success, training.Fail, training.TimerStart })
                        Check(Contains(training.Content.rect, Bounds(control, training.Content)), $"Training control clipped: {control.name}, {profile}.");
                    Check(!Bounds(training.Fail, training.Content).Overlaps(Bounds(training.Success, training.Content)), "Training buttons overlap.");
                    Check(!Bounds(training.Media, training.Content).Overlaps(Bounds(training.Numbers, training.Content)), "Media overlaps counter.");
                    Check(!Bounds(training.Media, training.Content).Overlaps(Bounds(training.Fail, training.Content)), "Media overlaps controls.");
                    Check(training.Media.rect.height > 0, "No room for media.");
                    var aspect = video.GetComponent<AspectRatioFitter>();
                    foreach (float ratio in new[] { 9f / 16, 16f / 9, 1f, .44972068f })
                    {
                        aspect.aspectRatio = ratio;
                        Canvas.ForceUpdateCanvases();
                        Check(Contains(training.Media.rect, Bounds(video.rectTransform, training.Media)), "Exercise video is cropped.");
                        Check(Mathf.Abs(video.rectTransform.rect.width / video.rectTransform.rect.height - ratio) < .005f, "Exercise video is stretched.");
                    }
                    Check(fallback.preserveAspect, "Fallback image must preserve aspect.");
                    foreach (var image in training.Numbers.GetComponentsInChildren<Image>())
                        Check(Contains(training.Numbers.rect, Bounds(image.rectTransform, training.Numbers)), "Six-digit counter overflows.");
                    regression.Open(999999, 999999);
                    Canvas.ForceUpdateCanvases();
                    foreach (var control in new[] { training.RegressionTitle, training.RegressionNumbers, training.RegressionMinus, training.RegressionPlus, training.RegressionConfirm })
                        Check(Contains(training.Content.rect, Bounds(control, training.Content)), $"Regression control clipped: {control.name}, {profile}.");
                    Check(!Bounds(training.RegressionNumbers, training.Content).Overlaps(Bounds(training.RegressionPlus, training.Content)), "Regression row overlaps.");
                    Check(!Bounds(training.RegressionTitle, training.Content).Overlaps(Bounds(training.RegressionNumbers, training.Content)), "Regression title overlaps numbers.");
                    regression.Close();
                }

                if (profile == Profiles[0] || profile == Profiles[2] || profile == Profiles[4] || profile == Profiles[5] || profile.x == 1920)
                {
                    menu.gameObject.SetActive(true);
                    training.gameObject.SetActive(false);
                    await Task.Yield();
                    Save(camera, profile, Path.Combine(output, $"{profile.x}x{profile.y}_Menu.png"));
                    if (profile.x == 1920 || profile.x == 390)
                    {
                        var menuSettings = new SerializedObject(menu.GetComponent<MainMenuTransitionView>());
                        var night = menu.Background.sprite;
                        var rotation = menu.MenuButton.localRotation;
                        menu.Background.sprite = (Sprite)menuSettings.FindProperty("DayBackground").objectReferenceValue;
                        menu.MenuButton.localRotation = Quaternion.Euler(0, 0, -67.11f);
                        Canvas.ForceUpdateCanvases();
                        Save(camera, profile, Path.Combine(output, $"{profile.x}x{profile.y}_MenuDay.png"));
                        menu.Background.sprite = night;
                        menu.MenuButton.localRotation = rotation;
                    }
                    menu.gameObject.SetActive(false);
                    training.gameObject.SetActive(true);
                    timer.Show(false, 25);
                    video.enabled = false;
                    regression.Close();
                    Canvas.ForceUpdateCanvases();
                    await Task.Yield();
                    Save(camera, profile, Path.Combine(output, $"{profile.x}x{profile.y}_Training.png"));
                    regression.Open(24, 12);
                    Canvas.ForceUpdateCanvases();
                    await Task.Yield();
                    Save(camera, profile, Path.Combine(output, $"{profile.x}x{profile.y}_Regression.png"));
                    regression.Close();
                    menu.gameObject.SetActive(true);
                }
            }
            Check(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Native orientation is not portrait.");
            File.WriteAllText(Path.Combine(output, "result.txt"), $"PASS: {Checks} layout checks across {Profiles.Length} phone, tablet and desktop profiles, with and without safe-area insets. No player build was created.");
            Debug.Log($"PASS: {Checks} portrait layout checks.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static Rect Bounds(RectTransform rect, RectTransform parent)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var local = corners.Select(parent.InverseTransformPoint).ToArray();
        return Rect.MinMaxRect(local.Min(point => point.x), local.Min(point => point.y), local.Max(point => point.x), local.Max(point => point.y));
    }

    private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMin >= outer.yMin - 1 && inner.yMax <= outer.yMax + 1;
    private static void Check(bool condition, string message) { Checks++; if (!condition) throw new InvalidOperationException(message); }

    private static void Save(Camera camera, Vector2Int size, string path)
    {
        var render = new RenderTexture(size.x, size.y, 24);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = render;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = render;
            var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            camera.targetTexture = previous;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(render);
        }
    }
}
#endif
