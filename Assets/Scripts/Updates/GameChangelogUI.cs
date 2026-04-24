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
        private const string VisibleSceneName = "Main";

        private static GameChangelogUI instance;

        private Canvas canvas;
        private Button openButton;
        private GameObject overlayRoot;
        private TMP_Text statusText;
        private RectTransform contentRoot;
        private bool loading;

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

            openButton = CreateButton(transform, "BtnOpenChangelog", ChronicleButtonText(), new Vector2(0f, 0f), new Vector2(126f, 46f), new Vector2(210f, 70f), ButtonTone.Secondary);
            openButton.onClick.AddListener(Open);
        }

        private void RefreshVisibility(string sceneName)
        {
            if (openButton == null)
                return;

            bool visible = IsVisibleScene(sceneName);
            openButton.gameObject.SetActive(visible);

            if (!visible && overlayRoot != null)
                Close();
        }

        private static bool IsVisibleScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            return string.Equals(sceneName, VisibleSceneName, StringComparison.Ordinal);
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

            TMP_Text title = CreateText(window.transform, "Title", ChroniclesTitleText(), 42f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(42f, -92f), new Vector2(-260f, -28f));

            Button closeButton = CreateButton(window.transform, "BtnCloseChangelog", CloseText(), new Vector2(1f, 1f), new Vector2(-98f, -56f), new Vector2(150f, 58f), ButtonTone.Close);
            closeButton.onClick.AddListener(Close);

            Button webButton = CreateButton(window.transform, "BtnOpenChangelogWeb", WebsiteText(), new Vector2(1f, 1f), new Vector2(-272f, -56f), new Vector2(160f, 58f), ButtonTone.Secondary);
            webButton.onClick.AddListener(() => Application.OpenURL(ChangelogPageUrl));

            statusText = CreateText(window.transform, "Status", LoadingText(), 24f, FontStyles.Normal, TextAlignmentOptions.Center);
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
            builder.Append("<b>").Append(VersionText()).Append(" ").Append(entry.version).Append("</b>");
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
                latestVersion = "1.0.9",
                latestVersionCode = 100009,
                entries = new[]
                {
                    new ChangelogEntry
                    {
                        version = "1.0.9",
                        versionCode = 100009,
                        date = "2026-04-24",
                        title = ChronicleBattleLobbyTitle(),
                        summary = ChronicleBattleLobbySummary(),
                        changes = new[]
                        {
                            ChronicleBattleLobbyChangeOne(),
                            ChronicleBattleLobbyChangeTwo(),
                            ChronicleBattleLobbyChangeThree()
                        }
                    },
                    new ChangelogEntry
                    {
                        version = "1.0.8",
                        versionCode = 100008,
                        date = "2026-04-23",
                        title = ChronicleLatestTitle(),
                        summary = ChronicleLatestSummary(),
                        changes = new[]
                        {
                            ChronicleLatestChangeOne(),
                            ChronicleLatestChangeTwo(),
                            ChronicleLatestChangeThree()
                        }
                    },
                    new ChangelogEntry
                    {
                        version = "1.0.7",
                        versionCode = 100007,
                        date = "2026-04-22",
                        title = ChronicleProjectTitle(),
                        summary = ChronicleProjectSummary(),
                        changes = new[]
                        {
                            ChronicleProjectChangeOne(),
                            ChronicleProjectChangeTwo(),
                            ChronicleProjectChangeThree()
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
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();
        }

        private enum ButtonTone
        {
            Secondary,
            Close
        }

        private static string ChronicleButtonText() => T("Хроники", "Chronicles", "Kronikler");
        private static string ChroniclesTitleText() => T("Хроники проекта", "Project Chronicles", "Proje Kronikleri");
        private static string CloseText() => T("Закрыть", "Close", "Kapat");
        private static string WebsiteText() => T("Сайт", "Website", "Site");
        private static string LoadingText() => T("Открываем хроники...", "Opening chronicles...", "Kronikler aciliyor...");
        private static string VersionText() => T("Версия", "Version", "Surum");
        private static string ChronicleBattleLobbyTitle() => T("Глава о боевом лобби", "The Battle Lobby Chapter", "Savas Lobisi Bolumu");
        private static string ChronicleBattleLobbySummary() => T(
            "Мы переодели battle lobby в собственный интерфейсный слой: верхняя панель, окно настроек и окна поиска матча получили боевые спрайты, а типографика стала общей с Main. Лобби перестало выглядеть как смесь экранов и стало цельной боевой точкой входа.",
            "We rebuilt the battle lobby with its own interface layer: the top bar, settings window, and matchmaking windows now use battle art, while typography matches Main. The lobby no longer feels like a mix of screens and now reads as one coherent battle entry point.",
            "Battle lobby icin ayri bir arayuz katmani kurduk: top bar, ayarlar penceresi ve eslesme pencereleri artik battle art kullaniyor, tipografi ise Main ile ayni. Lobi artik farkli ekranlarin karisimi gibi degil, tek parca bir savas girisi gibi gorunuyor.");
        private static string ChronicleBattleLobbyChangeOne() => T(
            "Top bar, окно настроек и фронтальные панели поиска матча теперь используют отдельные battle-спрайты.",
            "The top bar, settings window, and matchmaking front panels now use dedicated battle sprites.",
            "Top bar, ayarlar penceresi ve eslesme on panelleri artik ozel battle sprite'lari kullaniyor.");
        private static string ChronicleBattleLobbyChangeTwo() => T(
            "Кнопки lobby, shop, settings и действия в боевых окнах переведены на BattleLobbyButton, а шрифт синхронизирован с Main.",
            "Lobby, shop, settings, and battle action buttons now use BattleLobbyButton, with the same font as Main.",
            "Lobby, shop, settings ve battle aksiyon dugmeleri artik BattleLobbyButton kullaniyor; yazi tipi de Main ile ayni.");
        private static string ChronicleBattleLobbyChangeThree() => T(
            "Из battle lobby убраны чат и друзья, а закрытие карусели больше не падает на поиске неактивных объектов.",
            "Chat and friends were removed from the battle lobby, and carousel shutdown no longer trips over inactive object lookups.",
            "Battle lobby'den chat ve arkadaslar kaldirildi; carousel kapanisi da artik pasif nesne aramasinda hata vermiyor.");
        private static string ChronicleLatestTitle() => T("Глава о династии и центральном пункте", "The Dynasty and Central Point Chapter", "Hanedan ve Merkez Bolum");
        private static string ChronicleLatestSummary() => T(
            "Мы собрали в центральном пункте первые династические инструменты: хранилище, куда можно отложить золото и аметисты, и отдельный банк для обмена аметистов на золото. Это уже не просто кнопки в меню, а маленькая глава о том, как у аккаунта появляется общая память и общий запас.",
            "We gathered the first dynasty tools into the central point: a vault for setting aside gold and amethysts, and a separate bank for exchanging amethysts into gold. These are no longer just menu buttons, but a small chapter about an account gaining shared memory and shared reserves.",
            "Merkez bolume ilk hanedan araclarini yerlestirdik: altin ve ametistleri ayirmak icin depo, ametistleri altina cevirmek icin ayri banka. Bunlar artik sadece menu dugmeleri degil, hesabin ortak hafiza ve ortak birikim kazanmasinin kucuk bir bolumu.");
        private static string ChronicleLatestChangeOne() => T(
            "Династическое хранилище стало общим для профилей одного аккаунта, но его запас нельзя тратить напрямую на покупки.",
            "The dynasty vault is shared by the account profiles, but its reserves cannot be spent directly on purchases.",
            "Hanedan deposu hesap profilleri arasinda ortaktir, ancak buradaki birikim alisveriste dogrudan harcanamaz.");
        private static string ChronicleLatestChangeTwo() => T(
            "Банк отделён от хранилища и работает только как обменник аметистов на золото.",
            "The bank is separate from the vault and works only as an amethyst-to-gold exchange.",
            "Banka depodan ayridir ve yalnizca ametistten altina takas icin calisir.");
        private static string ChronicleLatestChangeThree() => T(
            "Левый верхний блок центрального пункта получил общий якорный слой, чтобы профиль, хранилище и банк держались вместе.",
            "The central point's upper-left block now has one shared anchor layer, keeping profile, vault, and bank together.",
            "Merkez bolumun sol ust blogu tek ortak anchor katmani kullaniyor; profil, depo ve banka birlikte duruyor.");
        private static string ChronicleProjectTitle() => T("Хроники проекта", "Project Chronicles", "Proje Kronikleri");
        private static string ChronicleProjectSummary() => T(
            "Мы добавили место, где игра рассказывает о собственном пути: от первых сборок до новых систем, которые постепенно складывают Symbiosis в живой проект.",
            "We added a place where the game tells the story of its own path: from the first builds to the new systems that slowly shape Symbiosis into a living project.",
            "Oyunun kendi yolunu anlattigi bir yer ekledik: ilk surumlerden Symbiosis'i yavas yavas yasayan bir projeye donusturen sistemlere kadar.");
        private static string ChronicleProjectChangeOne() => T("В игре появилась кнопка хроник.", "The game now has a Chronicles button.", "Oyuna Kronikler dugmesi eklendi.");
        private static string ChronicleProjectChangeTwo() => T("На сайте появилась страница хроник.", "The website now has a Chronicles page.", "Web sitesine Kronikler sayfasi eklendi.");
        private static string ChronicleProjectChangeThree() => T("Если сервер недоступен, игра всё равно показывает локальную главу истории.", "If the server is unavailable, the game still shows a local chapter of the story.", "Sunucuya ulasilamazsa oyun yine de yerel bir hikaye bolumu gosterir.");

        private static string T(string ru, string en, string tr)
        {
            if (AppSettings.I == null)
                return en;

            return AppSettings.I.Language switch
            {
                GameLanguage.Russian => ru,
                GameLanguage.English => en,
                GameLanguage.Turkish => tr,
                _ => en
            };
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
