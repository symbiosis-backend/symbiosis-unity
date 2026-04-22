using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    public sealed class AppUpdateService : MonoBehaviour
    {
        private const string UpdateManifestUrl = "http://91.99.176.77:8080/updates/android";

        private static AppUpdateService instance;

        [SerializeField] private float initialDelaySeconds = 1.2f;
        [SerializeField] private int fallbackAndroidVersionCode = 1;

        private AppUpdateManifest lastManifest;
        private bool checking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject serviceObject = new GameObject("AppUpdateService");
            instance = serviceObject.AddComponent<AppUpdateService>();
            PersistentObjectUtility.DontDestroyOnLoad(serviceObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(CheckAfterDelay());
        }

        private IEnumerator CheckAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, initialDelaySeconds));
            yield return CheckForUpdates();
        }

        public IEnumerator CheckForUpdates()
        {
            if (checking)
                yield break;

            checking = true;

            using UnityWebRequest request = UnityWebRequest.Get(UpdateManifestUrl);
            request.timeout = 10;

            yield return request.SendWebRequest();

            checking = false;

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning("[AppUpdateService] Update check failed: " + request.error);
                yield break;
            }

            AppUpdateManifest manifest = null;
            try
            {
                manifest = JsonUtility.FromJson<AppUpdateManifest>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AppUpdateService] Invalid update manifest: " + ex.Message);
            }

            if (manifest == null || !manifest.success)
                yield break;

            lastManifest = manifest;

            if (ShouldShowUpdate(manifest))
                AppUpdateUI.Show(manifest);
        }

        private bool ShouldShowUpdate(AppUpdateManifest manifest)
        {
            if (manifest == null || manifest.latestVersionCode <= 0)
            {
                Debug.LogWarning("[AppUpdateService] Update manifest has no valid latestVersionCode.");
                return false;
            }

            int currentCode = GetCurrentAndroidVersionCode();
            if (currentCode <= 0)
            {
                Debug.LogWarning("[AppUpdateService] Current Android versionCode is unknown. Update prompt will be skipped.");
                return false;
            }

            Debug.Log($"[AppUpdateService] Version check. Current={currentCode}, Latest={manifest.latestVersionCode}, Minimum={manifest.minimumVersionCode}, Force={manifest.forceUpdate}");

            if (manifest.minimumVersionCode > currentCode)
                return true;

            if (manifest.forceUpdate && manifest.latestVersionCode > currentCode)
                return true;

            return manifest.latestVersionCode > currentCode;
        }

        private int GetCurrentAndroidVersionCode()
        {
#if UNITY_EDITOR
            int editorVersionCode = PlayerSettings.Android.bundleVersionCode;
            return editorVersionCode > 0 ? editorVersionCode : ResolveFallbackAndroidVersionCode();
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
                Debug.LogWarning("[AppUpdateService] Could not read Android versionCode: " + ex.Message);
            }
            return ResolveFallbackAndroidVersionCode();
#else
            return ResolveFallbackAndroidVersionCode();
#endif
        }

        private int ResolveFallbackAndroidVersionCode()
        {
            return fallbackAndroidVersionCode > 0 ? fallbackAndroidVersionCode : -1;
        }

        [Serializable]
        public sealed class AppUpdateManifest
        {
            public bool success;
            public string platform;
            public string latestVersion;
            public int latestVersionCode;
            public int minimumVersionCode;
            public bool forceUpdate;
            public string updateUrl;
            public string releaseNotes;
            public string checkedAt;
        }

        private sealed class AppUpdateUI : MonoBehaviour
        {
            private AppUpdateManifest manifest;
            private GameObject panelRoot;

            public static void Show(AppUpdateManifest manifest)
            {
                if (FindAnyObjectByType<AppUpdateUI>(FindObjectsInactive.Include) != null)
                    return;

                Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);
                if (canvas == null)
                {
                    GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Exclude) == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                    eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                    eventSystem.AddComponent<StandaloneInputModule>();
#endif
                }

                GameObject root = new GameObject("AppUpdateUI", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                AppUpdateUI ui = root.AddComponent<AppUpdateUI>();
                ui.Build(manifest);
            }

            private void Build(AppUpdateManifest updateManifest)
            {
                manifest = updateManifest;

                RectTransform rootRect = transform as RectTransform;
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                GameObject overlay = new GameObject("UpdateOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlay.transform.SetParent(transform, false);
                RectTransform overlayRect = overlay.transform as RectTransform;
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

                panelRoot = new GameObject("UpdatePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelRoot.transform.SetParent(overlay.transform, false);
                RectTransform panelRect = panelRoot.transform as RectTransform;
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(620f, 430f);
                panelRoot.GetComponent<Image>().color = new Color(0.06f, 0.075f, 0.1f, 0.98f);

                TMP_Text title = CreateText(panelRoot.transform, "Title", "Update available", 38f, FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -86f), new Vector2(-32f, -24f));

                string body = BuildMessage(updateManifest);
                TMP_Text message = CreateText(panelRoot.transform, "Message", body, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
                message.enableAutoSizing = true;
                message.fontSizeMin = 15f;
                message.fontSizeMax = 22f;
                SetRect(message.rectTransform, Vector2.zero, Vector2.one, new Vector2(42f, 118f), new Vector2(-42f, -104f));

                Button updateButton = CreateButton(panelRoot.transform, "UpdateButton", "Update", new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(220f, 58f));
                updateButton.onClick.AddListener(OpenUpdate);

                if (!updateManifest.forceUpdate)
                {
                    Button laterButton = CreateButton(panelRoot.transform, "LaterButton", "Later", new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(160f, 48f));
                    laterButton.onClick.AddListener(Close);
                }
            }

            private static string BuildMessage(AppUpdateManifest manifest)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("Installed build is older than the server build.");

                if (!string.IsNullOrWhiteSpace(manifest.latestVersion))
                    builder.AppendLine().Append("Latest version: ").Append(manifest.latestVersion);

                if (!string.IsNullOrWhiteSpace(manifest.releaseNotes))
                    builder.AppendLine().AppendLine().Append(manifest.releaseNotes);

                if (manifest.forceUpdate)
                    builder.AppendLine().AppendLine().Append("This update is required.");

                return builder.ToString();
            }

            private void OpenUpdate()
            {
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.updateUrl))
                    return;

                Application.OpenURL(manifest.updateUrl);
            }

            private void Close()
            {
                Destroy(gameObject);
            }

            private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);

                TMP_Text text = textObject.GetComponent<TMP_Text>();
                text.text = value;
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.alignment = alignment;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.color = Color.white;
                text.raycastTarget = false;
                return text;
            }

            private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
            {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);

                RectTransform rect = buttonObject.transform as RectTransform;
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;

                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.12f, 0.48f, 0.62f, 1f);

                TMP_Text text = CreateText(buttonObject.transform, "Label", label, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                return buttonObject.GetComponent<Button>();
            }

            private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
            }
        }
    }
}
