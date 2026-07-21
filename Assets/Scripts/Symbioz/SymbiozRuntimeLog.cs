using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MahjongGame;
using UnityEngine;

namespace Dynasty.Legacy.Symbioz
{
    public static class SymbiozRuntimeLog
    {
        private const int MaxRecentLines = 10;
        private static readonly Queue<string> Recent = new Queue<string>();
        private static bool initialized;
        private static string logPath;

        public static string LogPath => logPath ?? string.Empty;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            string directory = Path.Combine(Application.persistentDataPath, "Symbioz");
            Directory.CreateDirectory(directory);
            string profileSuffix = ResolveProfileSuffix();
            logPath = Path.Combine(directory, string.IsNullOrWhiteSpace(profileSuffix)
                ? "symbioz-runtime.log"
                : $"symbioz-runtime_{profileSuffix}.log");

            Application.logMessageReceived -= HandleUnityLog;
            Application.logMessageReceived += HandleUnityLog;
            Write("BOOT", "Symbioz runtime log started. File: " + logPath);
        }

        private static string ResolveProfileSuffix()
        {
            return SanitizeFileSuffix(ClientProfileScope.Suffix);
        }

        private static string SanitizeFileSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    builder.Append(c);
            }

            return builder.ToString();
        }

        public static void Write(string category, string message)
        {
            Initialize();

            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";
            Recent.Enqueue(line);
            while (Recent.Count > MaxRecentLines)
                Recent.Dequeue();

            try
            {
                File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Logging must never break gameplay.
            }
        }

        public static string GetRecentText()
        {
            if (Recent.Count == 0)
                return "Log: waiting...";

            return string.Join("\n", Recent);
        }

        private static void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Warning)
                return;

            string compactStack = string.IsNullOrWhiteSpace(stackTrace)
                ? string.Empty
                : " | " + stackTrace.Split('\n')[0].Trim();
            Write(type.ToString().ToUpperInvariant(), condition + compactStack);
        }
    }
}
