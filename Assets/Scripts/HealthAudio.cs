using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Scripting;

public sealed class HealthAudio : MonoBehaviour
{
    public static bool Muted { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var host = new GameObject("HealthAudio");
        DontDestroyOnLoad(host);
        host.AddComponent<HealthAudio>();
        Muted = PlayerPrefs.GetInt("HealthAudioMuted", 0) != 0;
        AudioListener.volume = Muted ? 0 : 1;
    }
    [Preserve]
    public void SetMuted(string value)
    {
        Muted = value == "1";
        PlayerPrefs.SetInt("HealthAudioMuted", Muted ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = Muted ? 0 : 1;
        foreach (var player in FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Apply(player);
    }
    public static void Apply(VideoPlayer player)
    {
        if (!player.isPrepared || player.audioOutputMode != VideoAudioOutputMode.Direct) return;
        for (ushort track = 0; track < player.audioTrackCount; track++) player.SetDirectAudioMute(track, Muted);
    }
}
