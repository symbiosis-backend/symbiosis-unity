using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MahjongGame
{
    public static class ClientProfileScope
    {
        private const string ProfileArg = "-symbioz-profile";
        private const string ProfileEnvironmentVariable = "SYMBIOZ_PROFILE";
        private static string cachedSuffix;
        private static bool resolved;

        public static string Suffix
        {
            get
            {
                if (!resolved)
                {
                    cachedSuffix = ResolveSuffix();
                    resolved = true;
                }

                return cachedSuffix;
            }
        }

        public static string AppendToFileName(string fileName)
        {
            string suffix = Suffix;
            if (string.IsNullOrWhiteSpace(suffix))
                return fileName;

            int dot = fileName.LastIndexOf('.');
            if (dot <= 0)
                return fileName + "_" + suffix;

            return fileName.Substring(0, dot) + "_" + suffix + fileName.Substring(dot);
        }

        public static string AppendToKey(string key)
        {
            string suffix = Suffix;
            return string.IsNullOrWhiteSpace(suffix) ? key : key + "_" + suffix;
        }

        private static string ResolveSuffix()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], ProfileArg, StringComparison.OrdinalIgnoreCase))
                    continue;

                return Sanitize(args[i + 1]);
            }

            string environmentSuffix = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentSuffix))
                return Sanitize(environmentSuffix);

#if UNITY_EDITOR
            string editorCloneSuffix = ResolveEditorCloneSuffix();
            if (!string.IsNullOrWhiteSpace(editorCloneSuffix))
                return editorCloneSuffix;
#endif

            return string.Empty;
        }

#if UNITY_EDITOR
        private static string ResolveEditorCloneSuffix()
        {
            string projectRoot = ResolveProjectRoot();
            if (string.IsNullOrWhiteSpace(projectRoot))
                return string.Empty;

            string folderName = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(folderName))
                return string.Empty;

            string normalized = folderName.ToLowerInvariant();
            if (!normalized.Contains("clone"))
                return string.Empty;

            return "editor-" + ShortStableHash(projectRoot);
        }

        private static string ResolveProjectRoot()
        {
            string dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
                return string.Empty;

            DirectoryInfo parent = Directory.GetParent(dataPath);
            return parent != null ? parent.FullName : string.Empty;
        }

        private static string ShortStableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                string normalized = (value ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
                for (int i = 0; i < normalized.Length; i++)
                {
                    hash ^= normalized[i];
                    hash *= 16777619;
                }

                return hash.ToString("x8");
            }
        }
#endif

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }
    }
}
