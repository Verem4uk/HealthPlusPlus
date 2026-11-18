using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ExerciseMediaView : MonoBehaviour
{
    [SerializeField]
    private Image Background;

    [SerializeField]
    private RawImage VideoImage;

    [SerializeField]
    private AspectRatioFitter VideoAspect;

    [SerializeField]
    private VideoPlayer Player;

    private VideoSourceResult CurrentVideo;
    private RenderTexture VideoTexture;
    private bool Preparing;
    private bool ApplicationPaused;
    private bool ResumePlayback;
    private double PrepareStartedAt;

    private void Awake()
    {
        Player.prepareCompleted += OnPrepared;
        Player.frameReady += OnFrameReady;
        Player.errorReceived += OnVideoError;
    }

    // The source is a snapshot of cache readiness at exercise entry, never a live download.
    public void Show(Sprite fallback, VideoSourceResult video)
    {
        StopVideo();
        Background.sprite = fallback;
        if (video == null) return;

        CurrentVideo = video;
        Preparing = true;
        PrepareStartedAt = Time.realtimeSinceStartupAsDouble;
        Player.url = video.PlaybackUrl;
        Player.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (CurrentVideo == null || !Preparing) return;
        if (source.width == 0 || source.height == 0)
        {
            StopVideo();
            return;
        }

        VideoTexture = new RenderTexture((int)source.width, (int)source.height, 0);
        VideoTexture.Create();
        source.targetTexture = VideoTexture;
        VideoImage.texture = VideoTexture;
        VideoAspect.aspectRatio = (float)source.width / source.height;
        if (ApplicationPaused)
        {
            ResumePlayback = true;
        }
        else
        {
            source.Play();
        }
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        if (CurrentVideo == null) return;
        Preparing = false;
        VideoImage.enabled = true;
    }

    private void Update()
    {
        if (Preparing && !ApplicationPaused && Time.realtimeSinceStartupAsDouble - PrepareStartedAt > 15)
        {
            Debug.LogWarning("Exercise video preparation timed out; using its image.");
            StopVideo();
        }
    }

    private void OnVideoError(VideoPlayer source, string error)
    {
        Debug.LogWarning("Exercise video playback failed; using its image: " + error);
        StopVideo();
    }

    private void OnApplicationPause(bool paused)
    {
        ApplicationPaused = paused;
        if (paused)
        {
            ResumePlayback = Player.isPlaying;
            if (ResumePlayback) Player.Pause();
        }
        else
        {
            PrepareStartedAt = Time.realtimeSinceStartupAsDouble;
            if (ResumePlayback && CurrentVideo != null) Player.Play();
            ResumePlayback = false;
        }
    }

    private void OnDisable()
    {
        StopVideo();
    }

    private void StopVideo()
    {
        Preparing = false;
        ResumePlayback = false;
        if (VideoImage != null)
        {
            VideoImage.enabled = false;
            VideoImage.texture = null;
        }

        if (Player != null)
        {
            Player.Stop();
            Player.url = string.Empty;
            Player.targetTexture = null;
        }

        if (VideoTexture != null)
        {
            VideoTexture.Release();
            Destroy(VideoTexture);
            VideoTexture = null;
        }

        CurrentVideo?.Dispose();
        CurrentVideo = null;
    }

    private void OnDestroy()
    {
        if (Player != null)
        {
            Player.prepareCompleted -= OnPrepared;
            Player.frameReady -= OnFrameReady;
            Player.errorReceived -= OnVideoError;
        }

        StopVideo();
    }
}
