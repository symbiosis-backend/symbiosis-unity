using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class RuntimeFileLogger
    {
        private const string LogFileName = "runtime.log";
        private static bool initialized;
        private static string logPath;

        public static string LogPath
        {
            get
            {
                EnsureInitialized();
                return logPath;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInitialized();
        }

        public static void Write(string message)
        {
            EnsureInitialized();
            WriteLine("INFO", message);
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            logPath = Path.Combine(Application.persistentDataPath, LogFileName);
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.lowMemory -= OnLowMemory;
            Application.lowMemory += OnLowMemory;

            WriteLine("BOOT", "Runtime logger initialized. Version=" + Application.version);
            WriteLine("BOOT", "Package=" + Application.identifier + ", Platform=" + Application.platform + ", DataPath=" + Application.persistentDataPath);
            WriteLine("BOOT", "Device=" + SystemInfo.deviceModel + ", OS=" + SystemInfo.operatingSystem + ", CPU=" + SystemInfo.processorType);
            WriteLine("BOOT", "Graphics=" + SystemInfo.graphicsDeviceType + ", Device=" + SystemInfo.graphicsDeviceName + ", ShaderLevel=" + SystemInfo.graphicsShaderLevel);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            WriteLine("SCENE", "Loaded " + scene.name + " (" + mode + ")");
        }

        private static void OnLowMemory()
        {
            WriteLine("LOWMEM", "Application.lowMemory received.");
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                return;

            string message = condition;
            if (!string.IsNullOrWhiteSpace(stackTrace))
                message += Environment.NewLine + stackTrace;

            WriteLine(type.ToString().ToUpperInvariant(), message);
        }

        private static void WriteLine(string level, string message)
        {
            try
            {
                string line = DateTime.UtcNow.ToString("O") + " [" + level + "] " + message + Environment.NewLine;
                File.AppendAllText(logPath, line);
            }
            catch
            {
            }
        }
    }
}
