using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ProductAnalytics
{
    [Serializable]
    private sealed class TrainingStarted
    {
        public int level;
        public int base_number;
    }

    [Serializable]
    private sealed class TrainingCompleted
    {
        public int level;
        public int duration_seconds;
    }

    public static void TrainingStart(int levelIndex, int baseNumber)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Track("training_start", JsonUtility.ToJson(new TrainingStarted
        {
            level = levelIndex + 1,
            base_number = baseNumber
        }));
#endif
    }

    public static void TrainingComplete(int levelIndex, double durationSeconds)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Track("training_complete", JsonUtility.ToJson(new TrainingCompleted
        {
            level = levelIndex + 1,
            duration_seconds = (int)Math.Min(int.MaxValue, Math.Max(0, durationSeconds))
        }));
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static void Track(string name, string json)
    {
        try { HealthAnalytics_Track(name, json); }
        catch (Exception) { /* Analytics must not interrupt training or saving. */ }
    }

    [DllImport("__Internal")]
    private static extern void HealthAnalytics_Track(string name, string json);
#endif
}
