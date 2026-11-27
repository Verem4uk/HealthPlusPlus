#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class HealthGraphicsBuild
{
    // Separate output keeps the previous local build available for comparison.
    public static void Run()
    {
        try
        {
            HealthPortraitInstaller.Apply();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = "Build/GraphicsUpdate",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Graphics build failed: " + report.summary.result);
            Debug.Log("Graphics WebGL build: " + report.summary.totalSize + " bytes, " + report.summary.totalTime);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
