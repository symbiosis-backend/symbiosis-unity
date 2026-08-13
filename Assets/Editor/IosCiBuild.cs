#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class IosCiBuild
{
    private const string DefaultXcodeOutputPath = "Builds/iOS/Symbiosis-iOS";
    private const string VersionFilePath = "ProjectSettings/SymbiosisVersion.json";
    private const string DefaultBundleIdentifier = "com.ozkullar.dlsymbiosis";
    private const string DefaultAppleTeamId = "32VM68DZD8";
    private const string DefaultAppStoreVersion = "1.0.26";
    private const string AppIconPath = "Assets/Scripts/Mahjong/Sprites/DLSicon.png";

    public static void PrepareIosSettings()
    {
        AppVersion version = ReadVersionFile();
        string versionName = ReadEnv("BUILD_VERSION_NAME", DefaultAppStoreVersion);
        int buildNumber = GetBuildNumber(version);

        ConfigurePlayerSettings(versionName, buildNumber);
        ApplyAdMobSettingsFromEnvironment();
        ApplySigningFromEnvironment();
        ValidateAppIcon();
        AssetDatabase.SaveAssets();

        Debug.Log("[IosCiBuild] iOS settings prepared: bundle=" +
                  PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) +
                  " version=" + versionName +
                  " build=" + buildNumber +
                  " target=iPhone orientation=Landscape backend=IL2CPP");
    }

    public static void BuildXcodeProject()
    {
        AppVersion version = ReadVersionFile();
        string versionName = ReadEnv("BUILD_VERSION_NAME", DefaultAppStoreVersion);
        int buildNumber = GetBuildNumber(version);
        string outputPath = ResolveProjectRelativePath(ReadEnv("BUILD_OUTPUT_PATH", DefaultXcodeOutputPath));

        Directory.CreateDirectory(outputPath);

        PrepareIosSettings();

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path =>
            {
                bool exists = File.Exists(path);
                if (!exists)
                    Debug.LogWarning("[IosCiBuild] Skipping missing scene in Build Settings: " + path);

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
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("iOS Xcode project build failed: " + summary.result);
        }

        string xcodeProjectFile = Path.Combine(outputPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
        if (!File.Exists(xcodeProjectFile))
        {
            throw new FileNotFoundException("Unity reported success, but the Xcode project was not created.", xcodeProjectFile);
        }

        Debug.Log("[IosCiBuild] Xcode project built at " + outputPath +
                  " version=" + versionName +
                  " build=" + buildNumber +
                  " backend=IL2CPP target=iPhone");
    }

    private static void ConfigurePlayerSettings(string versionName, int buildNumber)
    {
        PlayerSettings.productName = "Symbiosis";
        PlayerSettings.bundleVersion = versionName;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ReadEnv("IOS_BUNDLE_ID", DefaultBundleIdentifier));
        PlayerSettings.iOS.applicationDisplayName = ReadEnv("IOS_DISPLAY_NAME", "Symbiosis");
        PlayerSettings.iOS.buildNumber = Math.Max(1, buildNumber).ToString();
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.targetOSVersionString = ReadEnv("IOS_MIN_TARGET", "15.0");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EnsureScriptingDefine("FISHNET");
    }

    private static void EnsureScriptingDefine(string requiredDefine)
    {
        string[] defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (defines.Contains(requiredDefine, StringComparer.Ordinal))
            return;

        PlayerSettings.SetScriptingDefineSymbols(
            NamedBuildTarget.iOS,
            string.Join(";", defines.Concat(new[] { requiredDefine })));
    }

    private static void ValidateAppIcon()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
        if (icon == null)
            throw new FileNotFoundException("Missing iOS app icon asset.", AppIconPath);

        if (icon.width != icon.height || icon.width < 1024)
        {
            throw new InvalidOperationException(
                "The iOS app icon must be square and at least 1024x1024. Current size: " +
                icon.width + "x" + icon.height + ".");
        }
    }

    private static int GetBuildNumber(AppVersion version)
    {
        int fallbackBuildNumber = ReadIntEnv("BUILD_VERSION_CODE_OFFSET", 0) +
                                  ReadIntEnv("GITHUB_RUN_NUMBER", version.versionCode);
        return Math.Max(1, ReadIntEnv("IOS_BUILD_NUMBER", ReadIntEnv("BUILD_VERSION_CODE", fallbackBuildNumber)));
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

    private static void ApplyAdMobSettingsFromEnvironment()
    {
        string appId = Environment.GetEnvironmentVariable("ADMOB_IOS_APP_ID");
        bool requireAppId = ReadBoolEnv("REQUIRE_IOS_ADMOB_ID", false);

        if (string.IsNullOrWhiteSpace(appId))
        {
            if (requireAppId)
            {
                throw new InvalidOperationException("REQUIRE_IOS_ADMOB_ID is true, but ADMOB_IOS_APP_ID is empty.");
            }

            Debug.LogWarning("[IosCiBuild] ADMOB_IOS_APP_ID is empty. iOS AdMob app id will not be changed.");
            return;
        }

        UnityEngine.Object settings = Resources.Load("GoogleMobileAdsSettings");
        if (settings == null)
        {
            if (requireAppId)
            {
                throw new InvalidOperationException("GoogleMobileAdsSettings asset was not found.");
            }

            Debug.LogWarning("[IosCiBuild] GoogleMobileAdsSettings asset was not found.");
            return;
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty iosAppId = serializedSettings.FindProperty("adMobIOSAppId");
        if (iosAppId == null)
        {
            if (requireAppId)
            {
                throw new InvalidOperationException("adMobIOSAppId property was not found in GoogleMobileAdsSettings.");
            }

            Debug.LogWarning("[IosCiBuild] adMobIOSAppId property was not found in GoogleMobileAdsSettings.");
            return;
        }

        iosAppId.stringValue = appId.Trim();
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[IosCiBuild] iOS AdMob app id configured from ADMOB_IOS_APP_ID.");
    }

    private static void ApplySigningFromEnvironment()
    {
        string teamId = ReadEnv("APPLE_DEVELOPER_TEAM_ID", DefaultAppleTeamId);
        string profileId = Environment.GetEnvironmentVariable("IOS_PROVISIONING_PROFILE_ID");
        bool automaticSigning = ReadBoolEnv("IOS_AUTOMATIC_SIGNING", true);

        PlayerSettings.iOS.appleEnableAutomaticSigning = automaticSigning;

        if (!string.IsNullOrWhiteSpace(teamId))
        {
            PlayerSettings.iOS.appleDeveloperTeamID = teamId.Trim();
        }

        if (automaticSigning)
        {
            PlayerSettings.iOS.iOSManualProvisioningProfileID = string.Empty;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
            Debug.Log("[IosCiBuild] iOS automatic signing enabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(profileId))
        {
            throw new InvalidOperationException("Manual iOS signing needs APPLE_DEVELOPER_TEAM_ID and IOS_PROVISIONING_PROFILE_ID.");
        }

        PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId.Trim();
        PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
        Debug.Log("[IosCiBuild] iOS manual distribution signing configured.");
    }

    private static string ReadEnv(string name, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool ReadBoolEnv(string name, bool fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
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
            throw new FileNotFoundException("Missing Symbiosis version file.", VersionFilePath);
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
