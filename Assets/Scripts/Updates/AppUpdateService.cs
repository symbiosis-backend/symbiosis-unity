using System;
using System.Collections;
using MahjongGame.Networking;
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
        private const string UpdateManifestPath = "/updates/android";
        private const string DesktopUpdateManifestPath = "/updates/desktop";

        private static AppUpdateService instance;

        [SerializeField] private float initialDelaySeconds = 1.2f;
        [SerializeField] private int fallbackAndroidVersionCode = 1;

        private AppUpdateManifest lastManifest;
        private bool checking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_IOS
            // The current backend manifest points to an Android APK. iOS updates are delivered by the App Store.
            Debug.Log("[AppUpdateService] Native iOS update prompts are disabled; updates are managed by the App Store.");
            return;
#endif
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

            string responseText = string.Empty;
            string requestError = string.Empty;
            bool failed = true;

            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = UnityWebRequest.Get(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], GetUpdateManifestPath()));
                request.timeout = 10;

                yield return request.SendWebRequest();

                responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                requestError = request.error;
                failed = BackendEndpoints.RequestFailed(request);
                if (!failed || !BackendEndpoints.CanRetryWithFallback(request) || i == BackendEndpoints.BaseUrls.Length - 1)
                    break;
            }

            checking = false;

            if (failed)
            {
                Debug.LogWarning("[AppUpdateService] Update check failed: " + requestError);
                yield break;
            }

            AppUpdateManifest manifest = null;
            try
            {
                manifest = JsonUtility.FromJson<AppUpdateManifest>(responseText);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AppUpdateService] Invalid update manifest: " + ex.Message);
            }

            if (manifest == null || !manifest.success)
                yield break;

            lastManifest = manifest;

            if (ShouldShowUpdate(manifest))
            {
                manifest.forceUpdate = IsUpdateRequired(manifest);
                AppUpdateUI.Show(manifest);
            }
        }

        private bool ShouldShowUpdate(AppUpdateManifest manifest)
        {
            if (manifest == null)
                return false;

            if (IsStandaloneDesktopClient() && !manifest.updateAvailable)
            {
                Debug.Log("[AppUpdateService] Desktop patch channel checked. No desktop package is available yet.");
                return false;
            }

            if (manifest == null || manifest.latestVersionCode <= 0)
            {
                Debug.LogWarning("[AppUpdateService] Update manifest has no valid latestVersionCode.");
                return false;
            }

            int currentCode = GetCurrentClientVersionCode();
            if (currentCode <= 0)
            {
                Debug.LogWarning("[AppUpdateService] Current client version code is unknown. Update prompt will be skipped.");
                return false;
            }

            Debug.Log($"[AppUpdateService] Version check. Current={currentCode}, Latest={manifest.latestVersionCode}, Minimum={manifest.minimumVersionCode}, Force={manifest.forceUpdate}");

            if (manifest.minimumVersionCode > currentCode)
                return true;

            if (manifest.forceUpdate && manifest.latestVersionCode > currentCode)
                return true;

            return manifest.latestVersionCode > currentCode;
        }

        private bool IsUpdateRequired(AppUpdateManifest manifest)
        {
            int currentCode = GetCurrentClientVersionCode();
            return manifest != null &&
                   currentCode > 0 &&
                   (manifest.minimumVersionCode > currentCode ||
                    (manifest.forceUpdate && manifest.latestVersionCode > currentCode));
        }

        private int GetCurrentClientVersionCode()
        {
            int versionCode = BackendEndpoints.GetClientVersionCode();
            return versionCode > 0 ? versionCode : ResolveFallbackAndroidVersionCode();
        }

        private int ResolveFallbackAndroidVersionCode()
        {
            return fallbackAndroidVersionCode > 0 ? fallbackAndroidVersionCode : -1;
        }

        private static string GetUpdateManifestPath()
        {
            return IsStandaloneDesktopClient() ? DesktopUpdateManifestPath : UpdateManifestPath;
        }

        private static bool IsStandaloneDesktopClient()
        {
#if UNITY_STANDALONE
            return !Application.isEditor;
#else
            return false;
#endif
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
            public bool updateAvailable;
            public string updateUrl;
            public string packageUrl;
            public string releaseNotes;
            public string checkedAt;
        }

        private sealed class AppUpdateUI : MonoBehaviour
        {
            private AppUpdateManifest manifest;

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
                EventSystemInputModeGuard.EnsureCompatibleEventSystems();

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
                overlay.GetComponent<Image>().color = new Color(0.015f, 0.018f, 0.025f, 0.96f);

                TMP_Text title = CreateText(overlay.transform, "Title", GameLocalization.Text("update.title"), 58f, FontStyles.Bold, TextAlignmentOptions.Center);
                title.enableAutoSizing = true;
                title.fontSizeMin = 36f;
                title.fontSizeMax = 58f;
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(80f, -164f), new Vector2(-80f, -62f));

                TMP_Text subtitle = CreateText(overlay.transform, "Subtitle", GameLocalization.Text("update.subtitle"), 28f, FontStyles.Normal, TextAlignmentOptions.Center);
                subtitle.enableAutoSizing = true;
                subtitle.fontSizeMin = 20f;
                subtitle.fontSizeMax = 28f;
                SetRect(subtitle.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -272f), new Vector2(0f, -172f));

                TMP_Text version = CreateText(overlay.transform, "Version", BuildVersionLine(updateManifest), 26f, FontStyles.Bold, TextAlignmentOptions.Center);
                version.enableAutoSizing = true;
                version.fontSizeMin = 19f;
                version.fontSizeMax = 26f;
                SetRect(version.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -340f), new Vector2(0f, -284f));

                TMP_Text required = CreateText(overlay.transform, "Required", updateManifest.forceUpdate ? GameLocalization.Text("update.required") : GameLocalization.Text("update.body_older"), 24f, FontStyles.Bold, TextAlignmentOptions.Center);
                required.color = updateManifest.forceUpdate ? new Color(1f, 0.78f, 0.28f, 1f) : new Color(0.78f, 0.9f, 1f, 1f);
                required.enableAutoSizing = true;
                required.fontSizeMin = 18f;
                required.fontSizeMax = 24f;
                SetRect(required.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -402f), new Vector2(0f, -348f));

                TMP_Text notesTitle = CreateText(overlay.transform, "NotesTitle", GameLocalization.Text("update.notes_title"), 30f, FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(notesTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(80f, -478f), new Vector2(-80f, -424f));

                ScrollRect notesScroll = CreateNotesScroll(overlay.transform, BuildReleaseNotes(updateManifest));
                SetRect(notesScroll.GetComponent<RectTransform>(), new Vector2(0.08f, 0f), new Vector2(0.92f, 1f), new Vector2(0f, 176f), new Vector2(0f, -488f));

                Button updateButton = CreateButton(overlay.transform, "UpdateButton", GameLocalization.Text("update.button"), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(420f, 76f), new Color(0.11f, 0.56f, 0.72f, 1f));
                updateButton.onClick.AddListener(OpenUpdate);

                if (!updateManifest.forceUpdate)
                {
                    Button laterButton = CreateButton(overlay.transform, "LaterButton", GameLocalization.Text("update.later"), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(260f, 54f), new Color(0.18f, 0.2f, 0.25f, 1f));
                    laterButton.onClick.AddListener(Close);
                }
            }

            private static string BuildVersionLine(AppUpdateManifest manifest)
            {
                string currentVersion = Application.version;
                int currentCode = BackendEndpoints.GetClientVersionCode();
                if (currentCode > 0)
                    currentVersion += " (" + currentCode + ")";

                string latestVersion = string.IsNullOrWhiteSpace(manifest.latestVersion) ? manifest.latestVersionCode.ToString() : manifest.latestVersion;
                if (manifest.latestVersionCode > 0)
                    latestVersion += " (" + manifest.latestVersionCode + ")";

                return GameLocalization.Format("update.current_version", currentVersion) + "\n" +
                       GameLocalization.Format("update.latest_version", latestVersion);
            }

            private static string BuildReleaseNotes(AppUpdateManifest manifest)
            {
                if (!string.IsNullOrWhiteSpace(manifest.releaseNotes))
                    return manifest.releaseNotes.Trim();

                return GameLocalization.Text("update.body_older");
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

            private static ScrollRect CreateNotesScroll(Transform parent, string notes)
            {
                GameObject scrollObject = new GameObject("NotesScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
                scrollObject.transform.SetParent(parent, false);
                Image background = scrollObject.GetComponent<Image>();
                background.color = new Color(0.05f, 0.06f, 0.08f, 0.82f);

                GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(scrollObject.transform, false);
                RectTransform viewportRect = viewport.transform as RectTransform;
                SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(28f, 22f), new Vector2(-28f, -22f));
                Image viewportImage = viewport.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
                viewport.GetComponent<Mask>().showMaskGraphic = false;

                GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
                content.transform.SetParent(viewport.transform, false);
                RectTransform contentRect = content.transform as RectTransform;
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;

                ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                TMP_Text notesText = CreateText(content.transform, "Notes", notes, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                notesText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
                notesText.lineSpacing = 12f;
                notesText.enableAutoSizing = true;
                notesText.fontSizeMin = 18f;
                notesText.fontSizeMax = 24f;

                ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
                scroll.viewport = viewportRect;
                scroll.content = contentRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 32f;
                return scroll;
            }

            private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, Color color)
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
                image.color = color;

                TMP_Text text = CreateText(buttonObject.transform, "Label", label, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = 30f;
                SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));

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
