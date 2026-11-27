#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// WebGL cannot play imported VideoClips; publish their original MP4 bytes as URL assets.
public sealed class HealthMenuVideoBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL) return;
        string output = report.summary.outputPath;
        if (Path.HasExtension(output)) output = Path.GetDirectoryName(output);
        string destination = Path.Combine(output, "StreamingAssets", "Menu");
        Directory.CreateDirectory(destination);
        foreach (string name in new[] { "FromNightToDay.mp4", "FromDayToNight.mp4" })
            File.Copy(Path.Combine("Assets", "Images", name), Path.Combine(destination, name), true);
    }
}
#endif
