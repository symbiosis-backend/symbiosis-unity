using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    public static class FishNetServerBuild
    {
        private const string DefaultLinuxOutput = "Builds/Server/Linux/SymbiosisServer.x86_64";
        private const string DefaultWindowsOutput = "Builds/Server/Windows/SymbiosisServer.exe";

        [MenuItem("Symbiosis/Build/FishNet Linux Server")]
        public static void BuildLinuxServer()
        {
            BuildServer(BuildTarget.StandaloneLinux64, ReadOutputPath(DefaultLinuxOutput));
        }

        [MenuItem("Symbiosis/Build/FishNet Windows Server")]
        public static void BuildWindowsServer()
        {
            BuildServer(BuildTarget.StandaloneWindows64, ReadOutputPath(DefaultWindowsOutput));
        }

        public static void BuildCiServer()
        {
            string target = Environment.GetEnvironmentVariable("FISHNET_SERVER_TARGET");
            if (string.Equals(target, "windows", StringComparison.OrdinalIgnoreCase))
                BuildWindowsServer();
            else
                BuildLinuxServer();
        }

        private static void BuildServer(BuildTarget target, string outputPath)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes found in Build Settings.");

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"FishNet server build failed: {report.summary.result}");

            Debug.Log($"[FishNetServerBuild] Server built at {outputPath}");
        }

        private static string ReadOutputPath(string fallback)
        {
            string value = Environment.GetEnvironmentVariable("FISHNET_SERVER_OUTPUT_PATH");
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
