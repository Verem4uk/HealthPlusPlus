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
    private VideoClip NightToDayClip;

    [SerializeField]
    private VideoClip DayToNightClip;

    [SerializeField]
    private float DayButtonAngle = -67.11f;

    private RenderTexture VideoTexture;
    private double LastProgressTime;
    private double LastVideoTime;
    private bool PlaybackStarted;
    private bool TargetDay;
    private bool ShowingDay;
    private bool Initialized;
    private bool LastAvailability;
    private float RotationStart;
    private Vector3 ButtonScale;
    private double PulseStartedAt;
    private double NextAvailabilityCheck;

    public bool IsTransitioning { get; private set; }
    public bool CanStartTraining => Initialized && ShowingDay && !IsTransitioning && Saver.IsTrainingAvailable();

    private void Awake()
    {
        var colors = PlayButton.colors;
        colors.disabledColor = colors.normalColor;
        PlayButton.colors = colors;

        Player.prepareCompleted += OnPrepared;
        Player.frameReady += OnFrameReady;
        Player.loopPointReached += OnVideoCompleted;
        Player.errorReceived += OnVideoError;
        ButtonScale = PlayButton.transform.localScale;
        ShowStatic(false);
    }

    private void Start()
    {
        Initialized = true;
        RefreshAvailability(true);
    }

    private void OnEnable()
    {
        if (Initialized)
        {
            RefreshAvailability(false);
        }
    }

    private void RefreshAvailability(bool force)
    {
        NextAvailabilityCheck = Time.realtimeSinceStartupAsDouble + 1;
        if (IsTransitioning) return;

        bool available = Saver.IsTrainingAvailable();
        if (!force && available == LastAvailability) return;
        LastAvailability = available;
        if (available)
        {
            BeginTransition(true);
        }
        else
        {
            ShowStatic(false);
        }
    }

    public void PlayCompletionTransition()
    {
        BeginTransition(false);
    }

    public void RefreshSavedProgress()
    {
        FinishTransition();
        RefreshAvailability(true);
    }

    private void BeginTransition(bool toDay)
    {
        if (IsTransitioning)
        {
            return;
        }

        TargetDay = toDay;
        LastAvailability = Saver.IsTrainingAvailable();
        ShowStatic(!toDay);
        RotationStart = toDay ? DayButtonAngle + 180 : DayButtonAngle;
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, RotationStart);
        IsTransitioning = true;
        PlayButton.interactable = false;
        PlaybackStarted = false;
        LastVideoTime = 0;
        LastProgressTime = Time.realtimeSinceStartupAsDouble;
        Player.clip = toDay ? NightToDayClip : DayToNightClip;

        if (Player.clip == null)
        {
            FinishTransition();
            return;
        }

        var aspect = VideoImage.GetComponent<AspectRatioFitter>();
        if (aspect != null)
        {
            aspect.aspectRatio = (float)Player.clip.width / Player.clip.height;
        }

        VideoTexture = new RenderTexture((int)Player.clip.width, (int)Player.clip.height, 0);
        VideoTexture.Create();
        Player.targetTexture = VideoTexture;
        VideoImage.texture = VideoTexture;
        Player.Prepare();
    }

    private void ShowStatic(bool day)
    {
        ShowingDay = day;
        Background.sprite = day ? DayBackground : NightBackground;
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, day ? DayButtonAngle : DayButtonAngle - 180);
        PlayButton.transform.localScale = ButtonScale;
        PlayButton.interactable = day && Saver.IsTrainingAvailable();
        VideoImage.enabled = false;
        VideoImage.color = Color.white;
        PulseStartedAt = Time.realtimeSinceStartupAsDouble;
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
        if (Time.realtimeSinceStartupAsDouble >= NextAvailabilityCheck)
        {
            RefreshAvailability(false);
        }

        if (!IsTransitioning)
        {
            if (CanStartTraining)
            {
                var phase = (float)((Time.realtimeSinceStartupAsDouble - PulseStartedAt) % 2) * Mathf.PI;
                var scale = 1 + 0.025f * (1 - Mathf.Cos(phase)) * 0.5f;
                PlayButton.transform.localScale = ButtonScale * scale;
            }
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
            Debug.LogWarning("Menu transition video timed out; showing its final background.");
            FinishTransition();
        }
    }

    private void UpdateVideoDisplay()
    {
        var duration = Player.clip.length;
        var progress = duration > 0 ? Mathf.Clamp01((float)(Player.time / duration)) : 1;
        // Interpolate the angle directly to make the half-turn unambiguously clockwise.
        PlayButton.transform.localRotation = Quaternion.Euler(0, 0, RotationStart - 180 * progress);

        var remaining = duration - Player.time;
        if (remaining < 0.2)
        {
            Background.sprite = TargetDay ? DayBackground : NightBackground;
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
        Player.Stop();
        ReleaseVideoTexture();
        bool day = TargetDay && Saver.IsTrainingAvailable();
        ShowStatic(day);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            FinishTransition();
        }
        else if (Initialized)
        {
            RefreshAvailability(false);
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && Initialized)
        {
            RefreshAvailability(false);
        }
    }

    private void OnDisable()
    {
        FinishTransition();
        PlayButton.transform.localScale = ButtonScale;
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
