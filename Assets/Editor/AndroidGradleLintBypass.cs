#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class AndroidGradleLintBypass : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string unityLibraryPath = Path.GetFullPath(path);
        string gradleRoot = Directory.GetParent(unityLibraryPath)?.FullName;
        if (string.IsNullOrEmpty(gradleRoot))
            return;

        PatchGradleFile(Path.Combine(unityLibraryPath, "build.gradle"));
        PatchGradleFile(Path.Combine(gradleRoot, "launcher", "build.gradle"));
        PatchGradleFile(Path.Combine(gradleRoot, "build.gradle"));
    }

    private static void PatchGradleFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        string content = File.ReadAllText(filePath);
        if (content.Contains("Symbiosis Android lint bypass", StringComparison.Ordinal))
            return;

        content += @"

// Symbiosis Android lint bypass: test APK builds do not need release lint tasks,
// and Unity 6000/AGP 9 can spend several GB on temporary lint AARs.
tasks.configureEach { task ->
    if (task.name.toLowerCase().contains('lint')) {
        task.enabled = false
    }
}
";
        File.WriteAllText(filePath, content);
        Debug.Log("[AndroidGradleLintBypass] Disabled lint tasks in " + filePath);
    }
}
#endif
