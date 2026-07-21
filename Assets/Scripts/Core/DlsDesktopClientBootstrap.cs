using System;
using System.Collections;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    public sealed class DlsDesktopClientBootstrap : MonoBehaviour
    {
        private const string BootstrapPath = "/dls/client/bootstrap";
        private const string DirectSymbiozArg = "-symbioz-direct";
        private const string PlatformEntryArg = "-dls-platform-entry";
        private const string LastBootstrapJsonKey = "dls_desktop_client_bootstrap_json";
        private const string LastBootstrapAtKey = "dls_desktop_client_bootstrap_at";
        private const float DirectGameEntryBlockSeconds = 12f;

        private static float installedAtRealtime = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!ShouldInstall())
                return;

            Application.runInBackground = true;
            Screen.SetResolution(1280, 720, false);
            installedAtRealtime = Time.realtimeSinceStartup;

            GameObject root = new GameObject("DLSDesktopClientBootstrap");
            DontDestroyOnLoad(root);
            root.AddComponent<DlsDesktopClientBootstrap>();
        }

        private static bool ShouldInstall()
        {
            if (Application.isBatchMode)
                return false;

#if UNITY_STANDALONE
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], DirectSymbiozArg, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
#else
            return false;
#endif
        }

        private void Start()
        {
            StartCoroutine(FetchBootstrap());
        }

        private IEnumerator FetchBootstrap()
        {
            string url = BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, BootstrapPath);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 8;
            BackendEndpoints.ApplyClientVersionHeaders(request);
            request.SetRequestHeader("X-DLS-Client-Experience", "mobile");
            request.SetRequestHeader("X-DLS-Client-Shell", "desktop");

            yield return request.SendWebRequest();

            if (BackendEndpoints.RequestFailed(request))
            {
                RuntimeFileLogger.Write("[DLSDesktopClientBootstrap] Server bootstrap failed: " + request.error);
                yield break;
            }

            string json = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                RuntimeFileLogger.Write("[DLSDesktopClientBootstrap] Server bootstrap was empty.");
                yield break;
            }

            PlayerPrefs.SetString(LastBootstrapJsonKey, json);
            PlayerPrefs.SetString(LastBootstrapAtKey, DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();
            RuntimeFileLogger.Write("[DLSDesktopClientBootstrap] Mobile DLS bootstrap received. bytes=" + json.Length);
        }

        public static bool ShouldBlockEarlyDirectGameEntry()
        {
            if (!ShouldInstall())
                return false;

            if (!HasCommandLineArg(PlatformEntryArg))
                return false;

            return installedAtRealtime >= 0f &&
                   Time.realtimeSinceStartup - installedAtRealtime < DirectGameEntryBlockSeconds;
        }

        private static bool HasCommandLineArg(string arg)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], arg, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
