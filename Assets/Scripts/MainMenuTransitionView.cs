using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuTransitionView : MonoBehaviour
{
    [SerializeField]
    private Image Background;

    [SerializeField]
    private Sprite DayBackground;

    [SerializeField]
    private Sprite NightBackground;

    [SerializeField]
    private Button PlayButton;

    [SerializeField]
    private RawImage VideoImage;

    [SerializeField]
    private VideoPlayer Player;

    [SerializeField]
    private float DayButtonAngle = -67.11f;

    private RenderTexture VideoTexture;
    private double LastProgressTime;
    private double LastVideoTime;
    private bool PlaybackStarted;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        var colors = PlayButton.colors;
        colors.disabledColor = colors.normalColor;
        PlayButton.colors = colors;

        Player.prepareCompleted += OnPrepared;
        Player.frameReady += OnFrameReady;
        Player.loopPointReached += OnVideoCompleted;
        Player.errorReceived += OnVideoError;
        ShowDay();
    }

    public void PlayCompletionTransition()
    {
        if (IsTransitioning)
        {
            return;
        }

        ShowDay();
        IsTransitioning = true;
        PlayButton.interactable = false;
        PlaybackStarted = false;
        LastVideoTime = 0;
        LastProgressTime = Time.realtimeSinceStartupAsDouble;

        if (Player.clip == null)
        {
            FinishTransition();
            return;
        }

        VideoTexture = new RenderTexture((int)Player.clip.width, (int)Player.clip.height, 0);
        VideoTexture.Create();
        Player.targetTexture = VideoTexture;
        VideoImage.texture = VideoTexture;
        Player.Prepare();
    }

    private void ShowDay()
    {
        Background.sprite = DayBackground;
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, DayButtonAngle);
        PlayButton.interactable = true;
        VideoImage.enabled = false;
        VideoImage.color = Color.white;
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (!IsTransitioning)
        {
            return;
        }

        PlaybackStarted = true;
        LastProgressTime = Time.realtimeSinceStartupAsDouble;
        source.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        if (IsTransitioning && PlaybackStarted)
        {
            UpdateVideoDisplay();
            VideoImage.enabled = true;
        }
    }

    private void Update()
    {
        if (!IsTransitioning)
        {
            return;
        }

        if (PlaybackStarted)
        {
            UpdateVideoDisplay();
            if (Player.time > LastVideoTime)
            {
                LastVideoTime = Player.time;
                LastProgressTime = Time.realtimeSinceStartupAsDouble;
            }
        }

        if (Time.realtimeSinceStartupAsDouble - LastProgressTime > 10)
        {
            Debug.LogWarning("Menu transition video timed out; showing the night background.");
            FinishTransition();
        }
    }

    private void UpdateVideoDisplay()
    {
        var duration = Player.clip.length;
        var progress = duration > 0 ? Mathf.Clamp01((float)(Player.time / duration)) : 1;
        // Interpolate the angle directly to make the half-turn unambiguously clockwise.
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, DayButtonAngle - 180 * progress);

        var remaining = duration - Player.time;
        if (remaining < 0.2)
        {
            Background.sprite = NightBackground;
        }

        var opacity = Mathf.Min(Mathf.Clamp01((float)(Player.time / 0.15)),
            Mathf.Clamp01((float)(remaining / 0.2)));
        VideoImage.color = new Color(1, 1, 1, opacity);
    }

    private void OnVideoCompleted(VideoPlayer source)
    {
        FinishTransition();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("Menu transition video failed: " + message);
        FinishTransition();
    }

    private void FinishTransition()
    {
        if (!IsTransitioning)
        {
            return;
        }

        IsTransitioning = false;
        Background.sprite = NightBackground;
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, DayButtonAngle - 180);
        VideoImage.enabled = false;
        Player.Stop();
        ReleaseVideoTexture();
        PlayButton.interactable = true;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            FinishTransition();
        }
    }

    private void OnDisable()
    {
        FinishTransition();
    }

    private void OnDestroy()
    {
        if (Player != null)
        {
            Player.prepareCompleted -= OnPrepared;
            Player.frameReady -= OnFrameReady;
            Player.loopPointReached -= OnVideoCompleted;
            Player.errorReceived -= OnVideoError;
        }

        ReleaseVideoTexture();
    }

    private void ReleaseVideoTexture()
    {
        if (Player != null)
        {
            Player.targetTexture = null;
        }

        if (VideoImage != null)
        {
            VideoImage.texture = null;
        }
        if (VideoTexture != null)
        {
            VideoTexture.Release();
            Destroy(VideoTexture);
            VideoTexture = null;
        }
    }
}
