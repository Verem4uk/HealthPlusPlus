using System;

public sealed class VideoSourceResult : IDisposable
{
    public string PlaybackUrl { get; }

    private Action Release;

    public VideoSourceResult(string playbackUrl, Action release = null)
    {
        PlaybackUrl = playbackUrl;
        Release = release;
    }

    public void Dispose()
    {
        var release = Release;
        Release = null;
        release?.Invoke();
    }
}
