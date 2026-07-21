#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidCiBuild
{
    private const string DefaultApkOutputPath = "Builds/Android/symbiosis-latest.apk";
    private const string DefaultAabOutputPath = "Builds/Android/symbiosis-play-release.aab";
    private const string VersionFilePath = "ProjectSettings/SymbiosisVersion.json";

    public static void BuildApk()
    {
        BuildAndroid(ReadEnv("BUILD_OUTPUT_PATH", DefaultApkOutputPath), false, "APK");
    }

    public static void BuildAab()
    {
        BuildAndroid(ReadEnv("BUILD_OUTPUT_PATH", DefaultAabOutputPath), true, "AAB");
    }

    private static void BuildAndroid(string outputPath, bool buildAppBundle, string artifactName)
    {
        string absoluteOutputPath = ResolveProjectRelativePath(outputPath);
        AppVersion version = ReadVersionFile();
        string versionName = ReadEnv("BUILD_VERSION_NAME", version.versionName);
        string productName = ReadEnv("BUILD_PRODUCT_NAME", PlayerSettings.productName);
        int fallbackVersionCode = ReadIntEnv("BUILD_VERSION_CODE_OFFSET", 0) +
                                  ReadIntEnv("GITHUB_RUN_NUMBER", version.versionCode);
        int versionCode = ReadIntEnv("BUILD_VERSION_CODE", fallbackVersionCode);

        Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath) ?? "Builds/Android");
        if (File.Exists(absoluteOutputPath))
            File.Delete(absoluteOutputPath);

        PlayerSettings.productName = productName;
        PlayerSettings.bundleVersion = versionName;
        PlayerSettings.Android.bundleVersionCode = Math.Max(1, versionCode);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ozkullar.dlsymbiosis");
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = true;
        ApplyKeystoreFromEnvironment();
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path =>
            {
                bool exists = File.Exists(path);
                if (!exists)
                    Debug.LogWarning("[AndroidCiBuild] Skipping missing scene in Build Settings: " + path);

                return exists;
            })
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = absoluteOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Android build failed: " + summary.result);
        }

        FileInfo artifact = new FileInfo(absoluteOutputPath);
        if (!artifact.Exists || artifact.Length <= 0)
        {
            throw new FileNotFoundException("Android build reported success, but the artifact was not created.", absoluteOutputPath);
        }

        Debug.Log("[AndroidCiBuild] " + artifactName + " built at " + artifact.FullName + " bytes=" + artifact.Length + " product=" + productName + " version=" + versionName + " code=" + versionCode + " backend=IL2CPP architectures=ARMv7|ARM64");
    }

    private static string ResolveProjectRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Build output path is empty.", nameof(path));

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(projectRoot, path));
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
            if (!string.Equals(Environment.GetEnvironmentVariable("ALLOW_ANDROID_DEBUG_SIGNING"), "true", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Android release signing is not configured. Set ANDROID_KEYSTORE_PATH, ANDROID_KEYSTORE_PASS, and ANDROID_KEY_ALIAS_NAME before building a deployable APK.");
            }

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

    private static AppVersion ReadVersionFile()
    {
        if (!File.Exists(VersionFilePath))
        {
            throw new FileNotFoundException("Missing Android version file.", VersionFilePath);
        }

        AppVersion version = JsonUtility.FromJson<AppVersion>(File.ReadAllText(VersionFilePath));
        if (version == null || string.IsNullOrWhiteSpace(version.versionName) || version.versionCode <= 0)
        {
            throw new InvalidOperationException("Invalid version data in " + VersionFilePath);
        }

        return version;
    }

    [Serializable]
    private sealed class AppVersion
    {
        public string versionName;
        public int versionCode;
    }
}
#endif
