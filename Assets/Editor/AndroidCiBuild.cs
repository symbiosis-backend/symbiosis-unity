#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidCiBuild
{
    private const string DefaultOutputPath = "Builds/Android/symbiosis-latest.apk";

    public static void BuildApk()
    {
        string outputPath = ReadEnv("BUILD_OUTPUT_PATH", DefaultOutputPath);
        string versionName = ReadEnv("BUILD_VERSION_NAME", PlayerSettings.bundleVersion);
        int fallbackVersionCode = ReadIntEnv("BUILD_VERSION_CODE_OFFSET", 0) +
                                  ReadIntEnv("GITHUB_RUN_NUMBER", PlayerSettings.Android.bundleVersionCode + 1);
        int versionCode = ReadIntEnv("BUILD_VERSION_CODE", fallbackVersionCode);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds/Android");

        PlayerSettings.bundleVersion = versionName;
        PlayerSettings.Android.bundleVersionCode = Math.Max(1, versionCode);
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.Android.forceInternetPermission = true;
        ApplyKeystoreFromEnvironment();

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Android build failed: " + summary.result);
        }

        Debug.Log("[AndroidCiBuild] APK built at " + outputPath + " version=" + versionName + " code=" + versionCode);
    }

    private static void ApplyKeystoreFromEnvironment()
    {
        string keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
        string keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        string keyAliasName = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS_NAME");
        string keyAliasPass = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS_PASS");

        if (string.IsNullOrWhiteSpace(keystorePath) ||
            string.IsNullOrWhiteSpace(keystorePass) ||
            string.IsNullOrWhiteSpace(keyAliasName))
        {
            PlayerSettings.Android.useCustomKeystore = false;
            Debug.Log("[AndroidCiBuild] Custom keystore not configured. Unity default signing will be used.");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAliasName;
        PlayerSettings.Android.keyaliasPass = string.IsNullOrWhiteSpace(keyAliasPass) ? keystorePass : keyAliasPass;
        Debug.Log("[AndroidCiBuild] Custom Android keystore configured.");
    }

    private static string ReadEnv(string name, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int ReadIntEnv(string name, int fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out int result) ? result : fallback;
    }
}
#endif
