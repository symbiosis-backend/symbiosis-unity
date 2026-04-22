using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class GameChangelogUI : MonoBehaviour
    {
        private const string ChangelogUrl = "https://dlsymbiosis.com/updates/changelog";
        private const string ChangelogPageUrl = "https://dlsymbiosis.com/changelog";

        private static GameChangelogUI instance;

        private Canvas canvas;
        private Button openButton;
        private GameObject overlayRoot;
        private TMP_Text statusText;
        private RectTransform contentRoot;
        private bool loading;

        private static readonly string[] HiddenSceneNames =
        {
            "Entry",
            "GameMahjong",
            "GameMahjongBattle",
            "GameOkey",
            "Voider",
            "Tetris"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject root = new GameObject("GameChangelogUI", typeof(RectTransform));
            instance = root.AddComponent<GameChangelogUI>();
            PersistentObjectUtility.DontDestroyOnLoad(root);
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
            EnsureUi();
            RefreshVisibility(SceneManager.GetActiveScene().name);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshVisibility(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshVisibility(scene.name);
        }

        private void EnsureUi()
        {
            EnsureEventSystem();

            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9997;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            openButton = CreateButton(transform, "BtnOpenChangelog", "Updates", new Vector2(0f, 0f), new Vector2(126f, 46f), new Vector2(210f, 70f), ButtonTone.Secondary);
            openButton.onClick.AddListener(Open);
        }

        private void RefreshVisibility(string sceneName)
        {
            if (openButton == null)
                return;

            bool visible = !IsHiddenScene(sceneName);
            openButton.gameObject.SetActive(visible);

            if (!visible && overlayRoot != null)
                Close();
        }

        private static bool IsHiddenScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            for (int i = 0; i < HiddenSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, HiddenSceneNames[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void Open()
        {
            if (overlayRoot != null)
                return;

            BuildOverlay();
            StartCoroutine(LoadChangelog());
        }

        private void Close()
        {
            if (overlayRoot != null)
                Destroy(overlayRoot);

            overlayRoot = null;
            statusText = null;
            contentRoot = null;
            loading = false;
        }

        private void BuildOverlay()
        {
            overlayRoot = CreatePanel(transform, "ChangelogOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.76f));
            RectTransform overlayRect = overlayRoot.transform as RectTransform;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject window = CreatePanel(overlayRoot.transform, "ChangelogWindow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 760f), new Color(0.055f, 0.07f, 0.085f, 0.98f));

            TMP_Text title = CreateText(window.transform, "Title", "Update History", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(42f, -92f), new Vector2(-260f, -28f));

            Button closeButton = CreateButton(window.transform, "BtnCloseChangelog", "Close", new Vector2(1f, 1f), new Vector2(-98f, -56f), new Vector2(150f, 58f), ButtonTone.Close);
            closeButton.onClick.AddListener(Close);

            Button webButton = CreateButton(window.transform, "BtnOpenChangelogWeb", "Website", new Vector2(1f, 1f), new Vector2(-272f, -56f), new Vector2(160f, 58f), ButtonTone.Secondary);
            webButton.onClick.AddListener(() => Application.OpenURL(ChangelogPageUrl));

            statusText = CreateText(window.transform, "Status", "Loading updates...", 24f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 96f), new Vector2(-48f, -116f));

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(window.transform, false);
            RectTransform scrollRect = scrollObject.transform as RectTransform;
            SetRect(scrollRect, Vector2.zero, Vector2.one, new Vector2(42f, 96f), new Vector2(-42f, -116f));

            GameObject viewport = CreatePanel(scrollObject.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.raycastTarget = true;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            contentRoot = content.transform as RectTransform;
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.transform as RectTransform;
            scroll.content = contentRoot;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
        }

        private IEnumerator LoadChangelog()
        {
            if (loading)
                yield break;

            loading = true;

            using UnityWebRequest request = UnityWebRequest.Get(ChangelogUrl);
            request.timeout = 10;
            yield return request.SendWebRequest();

            loading = false;

            ChangelogResponse response = null;
            bool failed = request.result == UnityWebRequest.Result.ConnectionError ||
                          request.result == UnityWebRequest.Result.ProtocolError ||
                          request.result == UnityWebRequest.Result.DataProcessingError;

            if (!failed)
            {
                try
                {
                    response = JsonUtility.FromJson<ChangelogResponse>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GameChangelogUI] Invalid changelog response: " + ex.Message);
                }
            }

            if (response == null || !response.success || response.entries == null || response.entries.Length == 0)
                response = BuildFallbackResponse();

            RenderEntries(response.entries);
        }

        private void RenderEntries(ChangelogEntry[] entries)
        {
            if (contentRoot == null)
                return;

            if (statusText != null)
                statusText.gameObject.SetActive(false);

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            for (int i = 0; i < entries.Length; i++)
                CreateEntry(contentRoot, entries[i]);
        }

        private static void CreateEntry(Transform parent, ChangelogEntry entry)
        {
            if (entry == null)
                return;

            GameObject card = CreatePanel(parent, "ChangelogEntry", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 220f), new Color(0.095f, 0.12f, 0.14f, 0.96f));
            LayoutElement layoutElement = card.AddComponent<LayoutElement>();
            layoutElement.minHeight = 180f;
            layoutElement.preferredHeight = 260f;

            StringBuilder builder = new StringBuilder();
            builder.Append("<b>Version ").Append(entry.version).Append("</b>");
            if (!string.IsNullOrWhiteSpace(entry.date))
                builder.Append("  <color=#B9C4CA>").Append(entry.date).Append("</color>");

            builder.AppendLine();
            builder.Append("<size=30><b>").Append(entry.title).Append("</b></size>");
            builder.AppendLine();
            builder.Append(entry.summary);

            if (entry.changes != null && entry.changes.Length > 0)
            {
                builder.AppendLine().AppendLine();
                for (int i = 0; i < entry.changes.Length; i++)
                    builder.Append("- ").Append(entry.changes[i]).AppendLine();
            }

            TMP_Text text = CreateText(card.transform, "EntryText", builder.ToString(), 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            text.richText = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 24f;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(26f, 24f), new Vector2(-26f, -24f));
        }

        private static ChangelogResponse BuildFallbackResponse()
        {
            return new ChangelogResponse
            {
                success = true,
                latestVersion = "1.0.7",
                latestVersionCode = 100007,
                entries = new[]
                {
                    new ChangelogEntry
                    {
                        version = "1.0.7",
                        versionCode = 100007,
                        date = "2026-04-22",
                        title = "Project Chronicle",
                        summary = "Added a changelog window in the game and a public update history page on the website.",
                        changes = new[]
                        {
                            "Players can open update history from the game menus.",
                            "The website has a public changelog page.",
                            "The game can show a fallback history when the server is unavailable."
                        }
                    },
                    new ChangelogEntry
                    {
                        version = "1.0.6",
                        versionCode = 100006,
                        date = "2026-04-22",
                        title = "Reliable Online Connection",
                        summary = "Online services now use HTTPS dlsymbiosis.com instead of the direct server IP.",
                        changes = new[] { "Improved connection reliability for different networks.", "Updated Android APK distribution." }
                    },
                    new ChangelogEntry
                    {
                        version = "1.0.5",
                        versionCode = 100005,
                        date = "2026-04-22",
                        title = "Online Ranked Matchmaking",
                        summary = "Added online ranked battle search through the public server.",
                        changes = new[] { "Matchmaking queue.", "Server-generated battle board.", "Authoritative battle events." }
                    },
                    new ChangelogEntry
                    {
                        version = "1.0.4",
                        versionCode = 100004,
                        date = "2026-04-22",
                        title = "Local Wi-Fi Battle",
                        summary = "Added local Wi-Fi battles for two devices on the same network.",
                        changes = new[] { "Local room flow.", "Host and join support.", "Battle sync for nearby devices." }
                    }
                }
            };
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Exclude) != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private enum ButtonTone
        {
            Secondary,
            Close
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 position, Vector2 size, ButtonTone tone)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = tone == ButtonTone.Close
                ? new Color(0.68f, 0.38f, 0.13f, 0.96f)
                : new Color(0.13f, 0.25f, 0.28f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateText(buttonObject.transform, "Label", label, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 26f;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));

            return button;
        }

        private static GameObject CreatePanel(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            return go;
        }

        private static TMP_Text CreateText(Transform parent, string objectName, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.color = new Color(0.96f, 0.92f, 0.84f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        [Serializable]
        private sealed class ChangelogResponse
        {
            public bool success;
            public string latestVersion;
            public int latestVersionCode;
            public ChangelogEntry[] entries;
            public string checkedAt;
        }

        [Serializable]
        private sealed class ChangelogEntry
        {
            public string version;
            public int versionCode;
            public string date;
            public string title;
            public string summary;
            public string[] changes;
        }
    }
}
