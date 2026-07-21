#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Dynasty.Legacy.Symbioz.Editor
{
    public static class SymbiozClientBuild
    {
        private const string EntryScenePath = "Assets/Scenes/Entry.unity";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string SymbiozScenePath = "Assets/Scenes/SymbiozFlagship.unity";
        private const string DefaultWindowsOutput = "Builds/SymbiozTestClient/Windows/DynastyLegacySymbioz.exe";
        private const string DefaultAndroidOutput = "Builds/SymbiozTestClient/Android/dynasty-legacy-symbioz-test.apk";

        [MenuItem("Dynasty/Symbioz/Build Windows Test Client")]
        public static void BuildWindowsTestClient()
        {
            BuildClient(BuildTarget.StandaloneWindows64, ReadEnv("SYMBIOZ_CLIENT_WINDOWS_OUTPUT", DefaultWindowsOutput));
        }

        [MenuItem("Dynasty/Symbioz/Build Android Test Client")]
        public static void BuildAndroidTestClient()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ozkullar.dlsymbiosis.symbioztest");
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.buildAppBundle = false;

            BuildClient(BuildTarget.Android, ReadEnv("SYMBIOZ_CLIENT_ANDROID_OUTPUT", DefaultAndroidOutput));
        }

        public static void BuildAllTestClients()
        {
            BuildWindowsTestClient();
            BuildAndroidTestClient();
        }

        private static void BuildClient(BuildTarget target, string outputPath)
        {
            EnsureOutputDirectory(outputPath);

            int subtarget = target == BuildTarget.StandaloneWindows64 ||
                            target == BuildTarget.StandaloneLinux64 ||
                            target == BuildTarget.StandaloneOSX
                ? (int)StandaloneBuildSubtarget.Player
                : 0;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = ResolveScenes(),
                locationPathName = outputPath,
                target = target,
                subtarget = subtarget,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Symbioz test client build failed: {summary.result}");

            Debug.Log($"[SymbiozClientBuild] {target} test client built at {outputPath} size={summary.totalSize} bytes");
        }

        private static string[] ResolveScenes()
        {
            var scenes = new List<string>();
            AddSceneIfExists(scenes, EntryScenePath);
            AddSceneIfExists(scenes, MainScenePath);
            AddSceneIfExists(scenes, SymbiozScenePath);

            foreach (string path in EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path))
                AddSceneIfExists(scenes, path);

            if (scenes.Count == 0)
                throw new InvalidOperationException("No scenes found for Symbioz test client build.");

            return scenes.ToArray();
        }

        private static void AddSceneIfExists(List<string> scenes, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || scenes.Contains(path))
                return;

            if (!File.Exists(path))
            {
                Debug.LogWarning("[SymbiozClientBuild] Skipping missing scene: " + path);
                return;
            }

            scenes.Add(path);
        }

        private static void EnsureOutputDirectory(string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private static string ReadEnv(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
#endif
