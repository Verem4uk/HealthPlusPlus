using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

// The browser restores either guest or account progress before training.
public sealed class CloudProgress : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IsReady = false;
        var receiver = new GameObject("HealthCloudProgress");
        DontDestroyOnLoad(receiver);
        receiver.AddComponent<CloudProgress>();
#else
        IsReady = true;
#endif
    }

    [Preserve]
    public void Restore(string json)
    {
        try
        {
            // Restore is accepted only before the user can start training.
            if (IsReady) return;
            var controller = FindFirstObjectByType<Controller>();
            var snapshot = JsonUtility.FromJson<ProgressSnapshot>(json);
            if (controller == null || snapshot == null || !snapshot.IsValid(controller.TrainingLevels.Count))
                throw new InvalidOperationException("Unsupported or invalid cloud progress.");
            Saver.RestoreProgress(snapshot);
            FindFirstObjectByType<TrainingVideoBootstrap>()?.PrepareLevel(snapshot.level);
            FindFirstObjectByType<MainMenuTransitionView>()?.RefreshSavedProgress();
            IsReady = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            HealthCloud_Restored();
#endif
        }
        catch (Exception error)
        {
            Debug.LogError("Cloud progress could not be restored: " + error.Message);
#if UNITY_WEBGL && !UNITY_EDITOR
            HealthCloud_RestoreFailed();
#endif
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsReady) IsReady = HealthCloud_Save(JsonUtility.ToJson(Saver.CaptureProgress())) != 0;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int HealthCloud_Save(string json);
    [DllImport("__Internal")] private static extern void HealthCloud_Restored();
    [DllImport("__Internal")] private static extern void HealthCloud_RestoreFailed();
#endif
}
