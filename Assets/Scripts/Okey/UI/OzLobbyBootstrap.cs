using MahjongGame;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OzGame.Okey
{
    [DisallowMultipleComponent]
    public sealed class OzLobbyBootstrap : MonoBehaviour
    {
        private const string OzLobbySceneName = "OzLobby";
        private const string OzGameSceneName = "OzGame";
        private const string MainSceneName = "Main";
        private const string SharedDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const string BootstrapName = "OzLobbyBootstrap";

        private static readonly Color Background = new Color(0.04f, 0.075f, 0.06f, 1f);
        private static readonly Color Panel = new Color(0.08f, 0.13f, 0.11f, 0.95f);
        private static readonly Color PanelSoft = new Color(0.1f, 0.17f, 0.14f, 0.9f);
        private static readonly Color Accent = new Color(0.92f, 0.66f, 0.28f, 1f);
        private static readonly Color ButtonColor = new Color(0.14f, 0.22f, 0.18f, 1f);
        private static readonly Color DisabledColor = new Color(0.18f, 0.19f, 0.18f, 0.86f);
        private static readonly Color MutedText = new Color(0.72f, 0.8f, 0.75f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureBootstrap(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrap(scene);
        }

        private static void EnsureBootstrap(Scene scene)
        {
            if (!string.Equals(scene.name, OzLobbySceneName, System.StringComparison.Ordinal))
                return;

            if (GameObject.Find(BootstrapName) != null)
                return;

            new GameObject(BootstrapName).AddComponent<OzLobbyBootstrap>();
        }

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            BuildInterface();
        }

        private static void EnsureCamera()
        {
            Camera camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void BuildInterface()
        {
            GameObject canvasObject = new GameObject("OzLobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            MainLobbyUiCoordinator.ConfigureOverlayScaler(scaler);

            Image backdrop = CreateImage(canvasObject.transform, "Backdrop", Background);
            Stretch(backdrop.rectTransform);

            RectTransform root = CreateImage(canvasObject.transform, "Root", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(root);

            TextMeshProUGUI title = CreateText(root, "Title", "ÖzOkey", 92f, Color.white, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplyFont(title);
            LayoutCentered(title.rectTransform, new Vector2(0f, 316f), new Vector2(1040f, 118f));

            TextMeshProUGUI subtitle = CreateText(root, "Subtitle", "Выберите режим игры.", 32f, MutedText, TextAlignmentOptions.Center);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 18f;
            LayoutCentered(subtitle.rectTransform, new Vector2(0f, 226f), new Vector2(1180f, 74f));

            RectTransform mainRoot = CreateImage(root, "MainModeRoot", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(mainRoot);

            RectTransform choiceRoot = CreateImage(root, "ModeChoiceRoot", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(choiceRoot);
            choiceRoot.gameObject.SetActive(false);

            BuildMainMode(mainRoot, choiceRoot);
            BuildModeChoice(choiceRoot, mainRoot, root);

            Button backButton = CreateButton(root, "BackToMainButton", "Назад", ButtonColor, Color.white);
            backButton.onClick.AddListener(() => LoadScene(MainSceneName));
            LayoutBottomLeft(backButton.transform as RectTransform, new Vector2(42f, 42f), new Vector2(360f, 82f));
        }

        private static void BuildMainMode(RectTransform parent, RectTransform choiceRoot)
        {
            RectTransform panel = CreateImage(parent, "MainModePanel", Panel).rectTransform;
            LayoutCentered(panel, new Vector2(0f, -44f), new Vector2(1180f, 360f));

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(54, 54, 42, 42);
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateText(panel, "Header", "ÖzOkey", 56f, Color.white, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplyFont(header);
            AddLayout(header.gameObject, 920f, 72f);

            Button okeyButton = CreateButton(panel, "OpenOkeyModesButton", "ÖzOkey", Accent, new Color(0.08f, 0.06f, 0.03f, 1f));
            okeyButton.onClick.AddListener(() =>
            {
                parent.gameObject.SetActive(false);
                choiceRoot.gameObject.SetActive(true);
            });
            AddLayout(okeyButton.gameObject, 720f, 112f);

            TextMeshProUGUI hint = CreateText(panel, "Hint", "Откройте выбор режима: Solo или Online.", 26f, MutedText, TextAlignmentOptions.Center);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 15f;
            AddLayout(hint.gameObject, 920f, 56f);
        }

        private static void BuildModeChoice(RectTransform choiceRoot, RectTransform mainRoot, RectTransform windowParent)
        {
            RectTransform panel = CreateImage(choiceRoot, "ModeChoicePanel", Panel).rectTransform;
            LayoutCentered(panel, new Vector2(0f, -44f), new Vector2(1240f, 390f));

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(54, 54, 40, 40);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateText(panel, "Header", "Режим ÖzOkey", 48f, Color.white, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplyFont(header);
            AddLayout(header.gameObject, 980f, 66f);

            RectTransform buttons = CreateImage(panel, "ModeButtons", new Color(0f, 0f, 0f, 0f)).rectTransform;
            AddLayout(buttons.gameObject, 980f, 128f);
            HorizontalLayoutGroup buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 28f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = true;

            Button soloButton = CreateButton(buttons, "SoloButton", "Solo", Accent, new Color(0.08f, 0.06f, 0.03f, 1f));
            soloButton.onClick.AddListener(() => LoadScene(OzGameSceneName));

            Button onlineButton = CreateButton(buttons, "OnlineButton", "Online", ButtonColor, Color.white);
            onlineButton.onClick.AddListener(() => OpenOnlineWindow(windowParent));

            TextMeshProUGUI description = CreateText(panel, "Description", "Solo запускает игру с ботами. Online открывает комнаты и столы.", 27f, MutedText, TextAlignmentOptions.Center);
            description.enableAutoSizing = true;
            description.fontSizeMin = 15f;
            AddLayout(description.gameObject, 980f, 66f);

            Button backToMainChoice = CreateButton(panel, "BackToOkeyButton", "К выбору", ButtonColor, Color.white);
            backToMainChoice.onClick.AddListener(() =>
            {
                choiceRoot.gameObject.SetActive(false);
                mainRoot.gameObject.SetActive(true);
            });
            AddLayout(backToMainChoice.gameObject, 360f, 74f);
        }

        private static void OpenOnlineWindow(RectTransform parent)
        {
            if (parent == null)
                return;

            Transform old = parent.Find("OnlineRoomsWindow");
            if (old != null)
                Object.Destroy(old.gameObject);

            RectTransform window = CreateImage(parent, "OnlineRoomsWindow", new Color(0.015f, 0.025f, 0.02f, 0.98f)).rectTransform;
            Stretch(window);
            window.SetAsLastSibling();

            TextMeshProUGUI title = CreateText(window, "Title", "Online tables", 64f, Color.white, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplyFont(title);
            LayoutCentered(title.rectTransform, new Vector2(0f, 370f), new Vector2(1100f, 90f));

            TextMeshProUGUI subtitle = CreateText(window, "Subtitle", "Здесь будут отображаться комнаты с активными играми.", 30f, MutedText, TextAlignmentOptions.Center);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 16f;
            LayoutCentered(subtitle.rectTransform, new Vector2(0f, 300f), new Vector2(1320f, 64f));

            RectTransform tablePanel = CreateImage(window, "TablesPanel", Panel).rectTransform;
            LayoutCentered(tablePanel, new Vector2(0f, -30f), new Vector2(1680f, 610f));

            VerticalLayoutGroup panelLayout = tablePanel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(42, 42, 36, 36);
            panelLayout.spacing = 14f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            CreateRoomHeader(tablePanel);
            CreateRoomRow(tablePanel, "Стол 1", "Duz Okey", "0 ставка", "3/4", "Открыт", false);
            CreateRoomRow(tablePanel, "Стол 2", "Okey 101", "100 Oz Altin", "1/4", "Открыт", false);
            CreateRoomRow(tablePanel, "Приватный стол", "Duz Okey", "по приглашению", "0/4", "Закрыт", true);
            CreateRoomRow(tablePanel, "Быстрый стол", "Duz Okey", "0 ставка", "2/4", "Ожидание", false);

            Button closeButton = CreateButton(window, "CloseOnlineWindowButton", "Назад", ButtonColor, Color.white);
            closeButton.onClick.AddListener(() => Object.Destroy(window.gameObject));
            LayoutBottomLeft(closeButton.transform as RectTransform, new Vector2(42f, 42f), new Vector2(360f, 82f));
        }

        private static void CreateRoomHeader(Transform parent)
        {
            RectTransform row = CreateImage(parent, "RoomsHeader", new Color(0f, 0f, 0f, 0f)).rectTransform;
            AddLayout(row.gameObject, -1f, 54f);
            AddRowLayout(row, 18, 12);

            AddRowText(row, "Комната", 30f, Color.white, 330f, true);
            AddRowText(row, "Режим", 30f, Color.white, 260f, true);
            AddRowText(row, "Ставка", 30f, Color.white, 300f, true);
            AddRowText(row, "Места", 30f, Color.white, 150f, true);
            AddRowText(row, "Статус", 30f, Color.white, 200f, true);
            AddLayout(CreateImage(row, "ActionSpacer", new Color(0f, 0f, 0f, 0f)).gameObject, 210f, -1f);
        }

        private static void CreateRoomRow(Transform parent, string title, string mode, string stake, string seats, string status, bool locked)
        {
            RectTransform row = CreateImage(parent, title.Replace(" ", string.Empty) + "Row", PanelSoft).rectTransform;
            AddLayout(row.gameObject, -1f, 82f);
            AddRowLayout(row, 18, 12);

            AddRowText(row, title, 27f, Color.white, 330f, true);
            AddRowText(row, mode, 25f, MutedText, 260f, false);
            AddRowText(row, stake, 25f, MutedText, 300f, false);
            AddRowText(row, seats, 25f, MutedText, 150f, false);
            AddRowText(row, status, 25f, locked ? DisabledColor : MutedText, 200f, false);

            Button joinButton = CreateButton(row, "JoinButton", locked ? "Закрыт" : "Войти", locked ? DisabledColor : ButtonColor, Color.white);
            joinButton.interactable = !locked;
            joinButton.onClick.AddListener(() => Debug.Log("[OzLobby] Online table selected: " + title));
            AddLayout(joinButton.gameObject, 210f, -1f);
        }

        private static void AddRowLayout(RectTransform row, int horizontalPadding, int verticalPadding)
        {
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void AddRowText(Transform parent, string text, float fontSize, Color color, float width, bool bold)
        {
            TextMeshProUGUI label = CreateText(parent, text + "Text", text, fontSize, color, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            MainLobbyButtonStyle.ApplyFont(label);
            AddLayout(label.gameObject, width, -1f);
        }

        private static void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[OzLobby] Scene is not in Build Settings: " + sceneName);
                return;
            }

            DoorFx doorFx = DoorFx.EnsureRuntime();
            if (doorFx != null && doorFx.IsReady())
                doorFx.LoadScene(sceneName, SharedDoorSpriteResourcePath);
            else
                SceneManager.LoadScene(sceneName);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, Color background, Color foreground)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            Image image = obj.GetComponent<Image>();
            image.color = background;

            Button button = obj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI label = CreateText(obj.transform, "Label", text, 34f, foreground, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 36f;
            label.fontStyle = FontStyles.Bold;
            MainLobbyButtonStyle.ApplyFont(label);
            Stretch(label.rectTransform, 14f);

            return button;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void LayoutCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void LayoutBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AddLayout(GameObject obj, float preferredWidth, float preferredHeight)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f)
                layout.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f)
                layout.preferredHeight = preferredHeight;
        }
    }
}
