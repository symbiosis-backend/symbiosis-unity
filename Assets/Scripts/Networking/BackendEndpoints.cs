using System;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongGame.Networking
{
    public static class BackendEndpoints
    {
        public const string PrimaryBaseUrl = "https://dlsymbiosis.com";
        public const string FallbackBaseUrl = PrimaryBaseUrl;

        public static readonly string[] BaseUrls =
        {
            PrimaryBaseUrl
        };

        public static string BuildUrl(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return baseUrl;

            return path[0] == '/'
                ? baseUrl.TrimEnd('/') + path
                : baseUrl.TrimEnd('/') + "/" + path;
        }

        public static void ApplyClientVersionHeaders(UnityWebRequest request)
        {
            if (request == null)
                return;

            request.SetRequestHeader("X-Client-Platform", GetClientPlatform());
            request.SetRequestHeader("X-Client-Version", Application.version ?? string.Empty);
            int versionCode = GetClientVersionCode();
            if (versionCode > 0)
                request.SetRequestHeader("X-Client-Version-Code", versionCode.ToString());
        }

        public static int GetClientVersionCode()
        {
#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS &&
                !Application.isPlaying)
            {
                return -1;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                int editorVersionCode = PlayerSettings.Android.bundleVersionCode;
                if (editorVersionCode > 0)
                    return editorVersionCode;
            }
#elif UNITY_ANDROID
            try
            {
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
                string packageName = activity.Call<string>("getPackageName");
                using AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
                return packageInfo.Get<int>("versionCode");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[BackendEndpoints] Could not read Android versionCode: " + ex.Message);
            }
#elif UNITY_IOS
            // The backend version gate currently represents the Android APK channel only.
            return -1;
#endif
            return EncodeSemanticVersion(Application.version);
        }

        // Kept for compatibility with older callers. New cross-platform code should use GetClientVersionCode().
        public static int GetAndroidVersionCode()
        {
            return GetClientVersionCode();
        }

        public static string GetClientPlatform()
        {
#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                return "ios";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return "android";
            return "desktop";
#elif UNITY_IOS
            return "ios";
#elif UNITY_ANDROID
            return "android";
#elif UNITY_STANDALONE
            return "desktop";
#else
            return Application.platform.ToString().ToLowerInvariant();
#endif
        }

        private static int EncodeSemanticVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return 1;

            string[] parts = version.Split('.');
            int major = ParseVersionPart(parts, 0);
            int minor = ParseVersionPart(parts, 1);
            int patch = ParseVersionPart(parts, 2);
            int encoded = major * 100000 + minor * 1000 + patch;
            return encoded > 0 ? encoded : 1;
        }

        private static int ParseVersionPart(string[] parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Length)
                return 0;

            string value = parts[index];
            int length = 0;
            while (length < value.Length && char.IsDigit(value[length]))
                length++;

            return length > 0 && int.TryParse(value.Substring(0, length), out int result)
                ? Mathf.Max(0, result)
                : 0;
        }

        public static bool RequestFailed(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
        }

        public static bool CanRetryWithFallback(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
        }
    }
}
