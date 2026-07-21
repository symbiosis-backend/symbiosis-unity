using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class SceneNavigator : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string entryScene = "Entry";
        [SerializeField] private string mainScene = "Main";
        [SerializeField] private string lobbyMahjongScene = "LobbyMahjong";
        [SerializeField] private string battleLobbyMahjongScene = "LobbyMahjongBattle";
        [SerializeField] private string gameMahjongScene = "GameMahjong";
        [SerializeField] private string symSudokuScene = "SymSudoku";
        [SerializeField] private string symbiGridScene = "SymbiGrid";
        [SerializeField] private string orbiosisScene = "Orbiosis";
        [SerializeField] private string symbiozFlagshipScene = "SymbiozFlagship";
        [SerializeField] private string ozLobbyScene = "OzLobby";
        [SerializeField] private string ozGameScene = "OzGame";

        [Header("Mahjong Door Transition")]
        [SerializeField] private bool useDoorFxForMahjong = true;
        [SerializeField] private bool useDoorFxForMainToMahjong = true;
        [SerializeField] private bool useDoorFxForMahjongBackToMain = true;
        [SerializeField] private bool useDoorFxForMahjongReload = true;
        [SerializeField] private string battleDoorSpriteResourcePath = "Mahjong/Sprites/BattleUI/BattleLobbyDoorLeaf";
        [SerializeField] private string endlessDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        [SerializeField] private string sharedDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        [SerializeField] private bool orbiosisDoorVerticalSplit = false;
        [SerializeField] private bool endlessDoorReverseMirroring = false;

        [Header("Main To Mahjong Moon Effect")]
        [SerializeField] private bool useMoonFxForMainToMahjong = true;
        [SerializeField] private string moonFxSpriteResourcePath = "Mahjong/Sprites/MainSettings/MoonMainEffect";
        [SerializeField] private Vector2 moonFxStartSize = new Vector2(260f, 260f);
        [SerializeField] private Vector2 moonFxAbsorbSize = new Vector2(420f, 420f);
        [SerializeField] private float moonFxStartOffsetX = 220f;
        [SerializeField] private float moonFxExitOffsetX = 900f;
        [SerializeField] private float moonFxYOffset = 0f;
        [SerializeField, Min(0.01f)] private float moonFxFlyInTime = 0.63f;
        [SerializeField, Min(0f)] private float moonFxLandHoldTime = 0f;
        [SerializeField, Min(0.01f)] private float moonFxAbsorbTime = 0.41f;
        [SerializeField, Min(0f)] private float moonFxHoldTime = 0.09f;
        [SerializeField, Min(0.01f)] private float moonFxFlyOutTime = 1.25f;
        [SerializeField] private bool moonFxUseUnscaledTime = true;
        [SerializeField] private bool moonFxRotateWhileMoving = true;
        [SerializeField] private float moonFxRotateSpeed = 720f;
        [SerializeField] private int moonFxSortingOrder = 10050;

        [Header("Main Mahjong Mode Choice")]
        [SerializeField] private string battleModeButtonSpriteResourcePath = "Mahjong/Sprites/MainSettings/MainMahjongBattleButton";
        [SerializeField] private string endlessModeButtonSpriteResourcePath = "Mahjong/Sprites/MainSettings/MainMahjongEndlessButton";
        [SerializeField] private float mahjongModeButtonMaxWidth = 720f;
        [SerializeField] private float mahjongModeButtonAspect = 0.36f;
        [SerializeField] private float mahjongModeButtonGap = 30f;
        [SerializeField, Min(0.01f)] private float mahjongModeSpitTime = 0.32f;
        [SerializeField] private Vector2 mahjongModeSelectionMoonSize = new Vector2(360f, 360f);
        [SerializeField] private Vector2 mahjongModeSelectionMoonAbsorbSize = new Vector2(620f, 620f);
        [SerializeField, Min(0f)] private float mahjongModeSelectionMoonHoldTime = 1f;
        [SerializeField, Min(0.01f)] private float mahjongModeSelectionAbsorbTime = 0.42f;
        [SerializeField, Min(0.01f)] private float mahjongModeSelectionMoonFlyInTime = 0.55f;
        [SerializeField, Min(0.01f)] private float mahjongModeSelectionMoonFlyOutTime = 0.75f;
        [SerializeField] private int mahjongModeSortingOrder = 32758;

        private const string MahjongEndlessDevelopmentBadgeName = "MahjongEndlessDevelopmentBadge";

        private bool isLoading;
        private float loadingStartedRealtime = -1f;
        private Sprite moonFxSprite;
        private GameObject mahjongModeOverlay;
        private Sprite battleSelectionMoonSprite;
        private Sprite battleModeButtonSprite;
        private Sprite endlessModeButtonSprite;
        private Button mahjongModeSourceButton;
        private Vector3 mahjongModeSourceScale = Vector3.one;
        private MonoBehaviour[] mahjongModePausedVisualFx = System.Array.Empty<MonoBehaviour>();
        private bool mahjongModeChoiceAnimating;
        private RectTransform mahjongEndlessDevelopmentCard;
        private const float LoadingStaleSeconds = 6f;

        public static bool IsMahjongEndlessAccessEnabled => false;

        public string CurrentSceneName => SceneManager.GetActiveScene().name;

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isLoading = false;
            loadingStartedRealtime = -1f;
        }

        public bool IsCurrentScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && CurrentSceneName == sceneName;
        }

        public void LoadEntry()
        {
            LoadSceneByName(entryScene);
        }

        public void LoadMain()
        {
            bool fromMahjong = IsCurrentScene(lobbyMahjongScene) || IsCurrentScene(battleLobbyMahjongScene) || IsCurrentScene(gameMahjongScene);

            if (fromMahjong && useDoorFxForMahjongBackToMain)
            {
                LoadSceneByName(mainScene, false, true, false, ResolveCurrentMahjongDoorSpriteResourcePath(), IsCurrentMahjongDoorReversed());
                return;
            }

            LoadSceneByName(mainScene);
        }

        public void LoadLobbyMahjong()
        {
            bool fromMain = IsCurrentScene(mainScene);
            if (fromMain && ShowMainMahjongModeChoice())
                return;

            bool useDoor = useDoorFxForMahjong && (!fromMain || useDoorFxForMainToMahjong);
            string targetScene = fromMain && !string.IsNullOrWhiteSpace(battleLobbyMahjongScene)
                ? battleLobbyMahjongScene
                : lobbyMahjongScene;
            LoadSceneByName(targetScene, false, useDoor, fromMain && useMoonFxForMainToMahjong);
        }

        public void LoadBattleLobbyMahjong()
        {
            bool useDoor = useDoorFxForMahjong && (!IsCurrentScene(mainScene) || useDoorFxForMainToMahjong);
            LoadSceneByName(battleLobbyMahjongScene, false, useDoor, IsCurrentScene(mainScene) && useMoonFxForMainToMahjong, battleDoorSpriteResourcePath);
        }

        public void LoadEndlessLobbyMahjong()
        {
            if (IsCurrentScene(mainScene) && !IsMahjongEndlessAccessEnabled)
            {
                if (mahjongModeOverlay == null)
                    ShowMainMahjongModeChoice();

                ShowMainMahjongEndlessDevelopmentNotice();
                return;
            }

            bool useDoor = useDoorFxForMahjong && (!IsCurrentScene(mainScene) || useDoorFxForMainToMahjong);
            LoadSceneByName(lobbyMahjongScene, false, useDoor, IsCurrentScene(mainScene) && useMoonFxForMainToMahjong, endlessDoorSpriteResourcePath, endlessDoorReverseMirroring);
        }

        public void LoadGameMahjong()
        {
            LoadSceneByName(gameMahjongScene, false, useDoorFxForMahjong, false, endlessDoorSpriteResourcePath, endlessDoorReverseMirroring);
        }

        public void LoadSymSudoku()
        {
            SceneOrientationPolicy.ApplyLandscapeOnly();
            LoadSceneByName(symSudokuScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void LoadSymbiGrid()
        {
            LoadSceneByName(symbiGridScene, false, false, false);
        }

        public void LoadOrbiosis()
        {
            LoadSceneByName(orbiosisScene, false, false, false);
        }

        public void LoadSymbiozFlagship()
        {
            if (DlsDesktopClientBootstrap.ShouldBlockEarlyDirectGameEntry())
            {
                Debug.Log("[SceneNavigator] Early desktop shell Symbioz entry click ignored. Platform entry is still warming up.");
                return;
            }

            SceneOrientationPolicy.ApplyLandscapeOnly();
            LoadSceneByName(symbiozFlagshipScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void LoadBlockBust()
        {
            LoadSymbiGrid();
        }

        public void LoadOzLobby()
        {
            LoadSceneByName(ozLobbyScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void LoadOzGame()
        {
            LoadSceneByName(ozGameScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void BackFromMain()
        {
            LoadEntry();
        }

        public void BackFromLobbyMahjong()
        {
            if (useDoorFxForMahjongBackToMain)
            {
                LoadSceneByName(mainScene, false, true, false, ResolveCurrentMahjongDoorSpriteResourcePath(), IsCurrentMahjongDoorReversed());
                return;
            }

            LoadMain();
        }

        public void BackFromGameMahjong()
        {
            LoadSceneByName(lobbyMahjongScene, false, useDoorFxForMahjong, false, endlessDoorSpriteResourcePath, endlessDoorReverseMirroring);
        }

        public void BackFromSymSudoku()
        {
            LoadSceneByName(mainScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void BackFromSymbiGrid()
        {
            LoadSceneByName(mainScene, false, false, false);
        }

        public void BackFromOrbiosis()
        {
            LoadSceneByName(mainScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void BackFromSymbiozFlagship()
        {
            LoadSceneByName(mainScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void BackFromBlockBust()
        {
            BackFromSymbiGrid();
        }

        public void BackFromOzLobby()
        {
            LoadSceneByName(mainScene, false, true, false, sharedDoorSpriteResourcePath);
        }

        public void ReloadCurrentScene()
        {
            if (isLoading)
                return;

            bool useDoor = useDoorFxForMahjongReload && IsMahjongScene(CurrentSceneName);
            LoadSceneByName(CurrentSceneName, true, useDoor, false, ResolveCurrentMahjongDoorSpriteResourcePath(), IsCurrentMahjongDoorReversed());
        }

        public void QuitGame()
        {
            Debug.Log("[SceneNavigator] QuitGame");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool IsMahjongScene(string sceneName)
        {
            return sceneName == lobbyMahjongScene || sceneName == battleLobbyMahjongScene || sceneName == gameMahjongScene;
        }

        private string ResolveCurrentMahjongDoorSpriteResourcePath()
        {
            return IsCurrentScene(lobbyMahjongScene) || IsCurrentScene(gameMahjongScene)
                ? endlessDoorSpriteResourcePath
                : battleDoorSpriteResourcePath;
        }

        private bool IsCurrentMahjongDoorReversed()
        {
            return (IsCurrentScene(lobbyMahjongScene) || IsCurrentScene(gameMahjongScene)) && endlessDoorReverseMirroring;
        }

        private bool ShowMainMahjongModeChoice()
        {
            if (mahjongModeChoiceAnimating)
                return true;

            if (mahjongModeOverlay != null)
            {
                CloseMainMahjongModeChoice();
                return true;
            }

            Button sourceButton = GetEventButton();
            Canvas sourceCanvas = sourceButton != null ? sourceButton.GetComponentInParent<Canvas>() : null;
            Canvas rootCanvas = CentralPointLayout.ResolveMainCanvas();
            if (rootCanvas == null)
                rootCanvas = sourceCanvas != null ? sourceCanvas : FindAnyObjectByType<Canvas>();
            if (rootCanvas == null)
                return false;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return false;

            mahjongModeOverlay = new GameObject("MainMahjongModeChoice", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            mahjongModeOverlay.transform.SetParent(rootCanvas.transform, false);
            mahjongModeOverlay.transform.SetAsLastSibling();

            RectTransform overlayRoot = mahjongModeOverlay.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;

            Canvas overlayCanvas = mahjongModeOverlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingLayerName = rootCanvas.sortingLayerName;
            overlayCanvas.sortingOrder = Mathf.Max(mahjongModeSortingOrder, 32758);

            Image blocker = mahjongModeOverlay.GetComponent<Image>();
            blocker.color = Color.black;

            Button closeButton = mahjongModeOverlay.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.interactable = true;
            closeButton.onClick.AddListener(CloseMainMahjongModeChoice);

            Canvas.ForceUpdateCanvases();
            RectTransform sourceRect = sourceButton != null ? sourceButton.transform as RectTransform : null;
            Vector2 center = Vector2.zero;

            Vector2 buttonSize = ResolveMainModeButtonSize(canvasRect.rect.size);
            float stackHeight = buttonSize.y * 2f + mahjongModeButtonGap;
            Vector2 battlePosition = new Vector2(center.x, center.y + stackHeight * 0.5f - buttonSize.y * 0.5f);
            Vector2 endlessPosition = new Vector2(center.x, center.y - stackHeight * 0.5f + buttonSize.y * 0.5f);

            StartMainMahjongModeSplit(rootCanvas, overlayRoot, sourceButton, sourceRect, center, buttonSize, battlePosition, endlessPosition, closeButton);
            return true;
        }

        private void StartMainMahjongModeSplit(Canvas rootCanvas, RectTransform overlayRoot, Button sourceButton, RectTransform sourceRect, Vector2 center, Vector2 buttonSize, Vector2 battlePosition, Vector2 endlessPosition, Button closeButton)
        {
            mahjongModeSourceButton = sourceButton;
            mahjongModeSourceScale = sourceRect != null ? sourceRect.localScale : Vector3.one;
            mahjongModePausedVisualFx = sourceButton != null ? DisableButtonVisualFx(sourceButton) : System.Array.Empty<MonoBehaviour>();

            Button battleButton = CreateMainModeButton(overlayRoot, "Btn_MahjongBattle", "Battle", battlePosition, buttonSize, battleModeButtonSpriteResourcePath, ref battleModeButtonSprite, () => SelectMainMahjongMode(battleLobbyMahjongScene, battleDoorSpriteResourcePath, false, null));
            UnityEngine.Events.UnityAction endlessAction = IsMahjongEndlessAccessEnabled
                ? () => SelectMainMahjongMode(lobbyMahjongScene, endlessDoorSpriteResourcePath, endlessDoorReverseMirroring, null)
                : ShowMainMahjongEndlessDevelopmentNotice;
            Button endlessButton = CreateMainModeButton(overlayRoot, "Btn_MahjongEndless", "Endless", endlessPosition, buttonSize, endlessModeButtonSpriteResourcePath, ref endlessModeButtonSprite, endlessAction);
            if (!IsMahjongEndlessAccessEnabled)
                EnsureMahjongEndlessDevelopmentBadge(endlessButton);

            if (sourceButton != null)
                sourceButton.gameObject.SetActive(false);

            RevealMainModeButtons(battleButton, endlessButton, closeButton);
            mahjongModeChoiceAnimating = false;
        }

        private void CloseMainMahjongModeChoice()
        {
            if (isLoading)
                return;

            RestoreMainMahjongSourceButton();

            if (mahjongModeOverlay == null)
                return;

            Destroy(mahjongModeOverlay);
            mahjongModeOverlay = null;
            mahjongEndlessDevelopmentCard = null;
            mahjongModeChoiceAnimating = false;
        }

        private void SelectMainMahjongMode(string sceneName, string doorSpriteResourcePath)
        {
            SelectMainMahjongMode(sceneName, doorSpriteResourcePath, false, null);
        }

        private void SelectMainMahjongMode(string sceneName, string doorSpriteResourcePath, bool reverseDoorMirroring)
        {
            SelectMainMahjongMode(sceneName, doorSpriteResourcePath, reverseDoorMirroring, null);
        }

        private void SelectMainMahjongMode(string sceneName, string doorSpriteResourcePath, bool reverseDoorMirroring, string selectionMoonSpriteResourcePath)
        {
            if (mahjongModeChoiceAnimating || isLoading || mahjongModeOverlay == null)
                return;

            RectTransform overlayRoot = mahjongModeOverlay.transform as RectTransform;
            if (overlayRoot == null)
            {
                LoadSceneByName(sceneName, false, useDoorFxForMahjong, false, doorSpriteResourcePath, reverseDoorMirroring);
                return;
            }

            Button[] buttons = mahjongModeOverlay.GetComponentsInChildren<Button>(true);
            Button battleButton = null;
            Button endlessButton = null;
            Button closeButton = mahjongModeOverlay.GetComponent<Button>();

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                if (button.name == "Btn_MahjongBattle")
                    battleButton = button;
                else if (button.name == "Btn_MahjongEndless")
                    endlessButton = button;
            }

            MainMahjongModeSelected(sceneName, doorSpriteResourcePath, reverseDoorMirroring, overlayRoot, battleButton, endlessButton, closeButton);
        }

        private Button CreateMainModeButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, string spriteResourcePath, ref Sprite cachedSprite, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(UIButtonFX));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.zero;

            Image image = go.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = true;
            image.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            button.interactable = true;
            MainLobbyButtonStyle.Apply(button);
            ApplyMainModeButtonSprite(button, spriteResourcePath, ref cachedSprite);

            UIButtonFX fx = go.GetComponent<UIButtonFX>();
            fx.ApplyMainMahjongButtonPreset();
            fx.enabled = true;
            return button;
        }

        private void ApplyMainModeButtonSprite(Button button, string spriteResourcePath, ref Sprite cachedSprite)
        {
            if (button == null || button.image == null)
                return;

            cachedSprite = LoadResourceSprite(cachedSprite, spriteResourcePath);
            if (cachedSprite == null)
                return;

            button.image.sprite = cachedSprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
            button.image.color = Color.white;
        }

        private static void EnsureMahjongEndlessDevelopmentBadge(Button button)
        {
            RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
            if (buttonRect == null)
                return;

            Transform existing = buttonRect.Find(MahjongEndlessDevelopmentBadgeName);
            TextMeshProUGUI text = existing != null ? existing.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (text == null)
            {
                GameObject badgeObject = new GameObject(MahjongEndlessDevelopmentBadgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                badgeObject.transform.SetParent(buttonRect, false);

                Image badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.color = new Color(0.008f, 0.035f, 0.07f, 0.94f);
                badgeImage.raycastTarget = false;

                Outline outline = badgeObject.GetComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.82f, 1f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(badgeObject.transform, false);
                text = labelObject.GetComponent<TextMeshProUGUI>();
                text.color = new Color(1f, 0.84f, 0.34f, 1f);
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 17f;
                text.fontSizeMax = 30f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.raycastTarget = false;
                MainLobbyButtonStyle.ApplyFont(text);
            }

            text.text = GameLocalization.Text("main.mahjong_endless_unavailable.status");
            text.transform.parent.gameObject.SetActive(true);

            RectTransform badgeRect = text.transform.parent as RectTransform;
            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 36f);
            badgeRect.sizeDelta = new Vector2(Mathf.Min(380f, buttonRect.rect.width * 0.78f), 60f);
            badgeRect.localScale = Vector3.one;
            badgeRect.localRotation = Quaternion.identity;

            RectTransform labelRect = badgeRect.Find("Label") as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 2f);
                labelRect.offsetMax = new Vector2(-12f, -2f);
                labelRect.localScale = Vector3.one;
                labelRect.localRotation = Quaternion.identity;
            }

            badgeRect.SetAsLastSibling();
        }

        private void ShowMainMahjongEndlessDevelopmentNotice()
        {
            if (mahjongModeOverlay == null)
                return;

            RectTransform overlayRoot = mahjongModeOverlay.transform as RectTransform;
            if (overlayRoot == null)
                return;

            Button battleButton = overlayRoot.Find("Btn_MahjongBattle")?.GetComponent<Button>();
            Button endlessButton = overlayRoot.Find("Btn_MahjongEndless")?.GetComponent<Button>();
            if (battleButton != null)
                battleButton.gameObject.SetActive(false);
            if (endlessButton != null)
                endlessButton.gameObject.SetActive(false);

            EnsureMainMahjongEndlessDevelopmentNotice(overlayRoot);
            if (mahjongEndlessDevelopmentCard == null)
                return;

            TMP_Text status = mahjongEndlessDevelopmentCard.Find("Status")?.GetComponent<TMP_Text>();
            TMP_Text body = mahjongEndlessDevelopmentCard.Find("Body")?.GetComponent<TMP_Text>();
            if (status != null)
                status.text = GameLocalization.Text("main.mahjong_endless_unavailable.status");
            if (body != null)
                body.text = GameLocalization.Text("main.mahjong_endless_unavailable.body");

            mahjongEndlessDevelopmentCard.gameObject.SetActive(true);
            mahjongEndlessDevelopmentCard.SetAsLastSibling();
            mahjongModeChoiceAnimating = false;
        }

        private void EnsureMainMahjongEndlessDevelopmentNotice(RectTransform overlayRoot)
        {
            if (mahjongEndlessDevelopmentCard != null || overlayRoot == null)
                return;

            GameObject cardObject = new GameObject("MahjongEndlessDevelopmentNotice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            cardObject.transform.SetParent(overlayRoot, false);
            mahjongEndlessDevelopmentCard = cardObject.GetComponent<RectTransform>();

            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = new Color(0.012f, 0.055f, 0.105f, 0.99f);
            cardImage.raycastTarget = true;

            Outline cardOutline = cardObject.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.24f, 0.82f, 1f, 0.96f);
            cardOutline.effectDistance = new Vector2(3f, -3f);

            Button cardBlocker = cardObject.GetComponent<Button>();
            cardBlocker.targetGraphic = cardImage;
            cardBlocker.transition = Selectable.Transition.None;

            Vector2 overlaySize = overlayRoot.rect.size;
            if (overlaySize.x <= 1f || overlaySize.y <= 1f)
                overlaySize = new Vector2(1920f, 1080f);
            Vector2 cardSize = new Vector2(Mathf.Min(1500f, overlaySize.x - 120f), Mathf.Min(760f, overlaySize.y - 100f));

            mahjongEndlessDevelopmentCard.anchorMin = new Vector2(0.5f, 0.5f);
            mahjongEndlessDevelopmentCard.anchorMax = new Vector2(0.5f, 0.5f);
            mahjongEndlessDevelopmentCard.pivot = new Vector2(0.5f, 0.5f);
            mahjongEndlessDevelopmentCard.anchoredPosition = Vector2.zero;
            mahjongEndlessDevelopmentCard.sizeDelta = cardSize;
            mahjongEndlessDevelopmentCard.localScale = Vector3.one;

            CreateDevelopmentAccent(cardObject.transform, "TopAccent", new Color(1f, 0.72f, 0.18f, 1f), true, 7f);
            CreateDevelopmentAccent(cardObject.transform, "BottomAccent", new Color(0.18f, 0.76f, 1f, 0.86f), false, 4f);

            GameObject logoObject = new GameObject("MahjongEndlessLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(cardObject.transform, false);
            RectTransform logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = new Vector2(0f, 230f);
            logoRect.sizeDelta = new Vector2(580f, 200f);
            Image logo = logoObject.GetComponent<Image>();
            logo.sprite = LoadResourceSprite(endlessModeButtonSprite, endlessModeButtonSpriteResourcePath);
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            logo.color = Color.white;
            logo.raycastTarget = false;

            TextMeshProUGUI status = CreateMainModeButtonText(cardObject.transform, GameLocalization.Text("main.mahjong_endless_unavailable.status"), 46f);
            status.gameObject.name = "Status";
            status.color = new Color(1f, 0.84f, 0.34f, 1f);
            LayoutDevelopmentRect(status.rectTransform, new Vector2(0f, 100f), new Vector2(cardSize.x - 140f, 78f));

            TextMeshProUGUI body = CreateMainModeButtonText(cardObject.transform, GameLocalization.Text("main.mahjong_endless_unavailable.body"), 32f);
            body.gameObject.name = "Body";
            body.color = new Color(0.86f, 0.94f, 1f, 1f);
            body.fontStyle = FontStyles.Normal;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;
            LayoutDevelopmentRect(body.rectTransform, new Vector2(0f, -40f), new Vector2(cardSize.x - 230f, 190f));

            Button backButton = CreateDevelopmentBackButton(cardObject.transform);
            RectTransform backRect = backButton.transform as RectTransform;
            LayoutDevelopmentRect(backRect, new Vector2(0f, -275f), new Vector2(360f, 88f));
        }

        private static void CreateDevelopmentAccent(Transform parent, string name, Color color, bool top, float height)
        {
            GameObject accentObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accentObject.transform.SetParent(parent, false);
            RectTransform rect = accentObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            Image image = accentObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private Button CreateDevelopmentBackButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("BackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.025f, 0.16f, 0.26f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            MainLobbyButtonStyle.Apply(button);
            button.onClick.AddListener(CloseMainMahjongEndlessDevelopmentNotice);

            TextMeshProUGUI label = CreateMainModeButtonText(buttonObject.transform, GameLocalization.Text("common.back").ToUpperInvariant(), 31f);
            label.gameObject.name = "Label";
            return button;
        }

        private void CloseMainMahjongEndlessDevelopmentNotice()
        {
            if (mahjongEndlessDevelopmentCard != null)
            {
                Destroy(mahjongEndlessDevelopmentCard.gameObject);
                mahjongEndlessDevelopmentCard = null;
            }

            if (mahjongModeOverlay == null)
                return;

            RectTransform overlayRoot = mahjongModeOverlay.transform as RectTransform;
            Button battleButton = overlayRoot != null ? overlayRoot.Find("Btn_MahjongBattle")?.GetComponent<Button>() : null;
            Button endlessButton = overlayRoot != null ? overlayRoot.Find("Btn_MahjongEndless")?.GetComponent<Button>() : null;
            if (battleButton != null)
            {
                battleButton.gameObject.SetActive(true);
                SetMainModeButtonReady(battleButton);
            }
            if (endlessButton != null)
            {
                endlessButton.gameObject.SetActive(true);
                SetMainModeButtonReady(endlessButton);
                EnsureMahjongEndlessDevelopmentBadge(endlessButton);
            }
        }

        private static void LayoutDevelopmentRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static TextMeshProUGUI CreateMainModeButtonText(Transform parent, string label, float fontSize)
        {
            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(text);
            text.alignment = TextAlignmentOptions.Center;
            text.margin = Vector4.zero;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            return text;
        }

        private Vector2 ResolveMainModeButtonSize(Vector2 canvasSize)
        {
            float width = Mathf.Min(mahjongModeButtonMaxWidth, Mathf.Max(1f, canvasSize.x) * 0.42f);
            width = Mathf.Max(420f, width);
            float height = width * Mathf.Max(0.1f, mahjongModeButtonAspect);
            return new Vector2(width, height);
        }

        private IEnumerator MainMahjongModeChoiceRoutine(
            Canvas rootCanvas,
            RectTransform overlayRoot,
            Button sourceButton,
            RectTransform sourceRect,
            Vector2 center,
            Button battleButton,
            Vector2 battlePosition,
            Button endlessButton,
            Vector2 endlessPosition,
            Button closeButton)
        {
            mahjongModeChoiceAnimating = true;
            mahjongModeSourceButton = sourceButton;
            mahjongModeSourceScale = sourceRect != null ? sourceRect.localScale : Vector3.one;

            RectTransform moon = CreateMoonFxImage(overlayRoot);
            if (moon == null || sourceButton == null || sourceRect == null)
            {
                RevealMainModeButtons(battleButton, endlessButton, closeButton);
                mahjongModeChoiceAnimating = false;
                yield break;
            }

            CanvasGroup sourceGroup = sourceButton.GetComponent<CanvasGroup>();
            if (sourceGroup == null)
                sourceGroup = sourceButton.gameObject.AddComponent<CanvasGroup>();

            mahjongModePausedVisualFx = DisableButtonVisualFx(sourceButton);
            sourceGroup.alpha = 1f;
            sourceGroup.blocksRaycasts = false;
            sourceGroup.interactable = false;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(-halfWidth - moonFxStartSize.x - moonFxStartOffsetX, center.y + moonFxYOffset);
            Vector2 absorbPos = new Vector2(center.x, center.y + moonFxYOffset);
            Vector2 exitPos = new Vector2(halfWidth + moonFxAbsorbSize.x + moonFxExitOffsetX, center.y + moonFxYOffset);

            moon.anchoredPosition = startPos;
            moon.sizeDelta = moonFxStartSize;
            moon.localScale = Vector3.one;
            PlaceMoonUnderInfoLayer(moon, overlayRoot);

            yield return AnimateMoonMove(moon, startPos, absorbPos, moonFxFlyInTime);

            if (moonFxLandHoldTime > 0f)
                yield return WaitMoon(moonFxLandHoldTime);

            yield return AnimateMoonAbsorb(moon, sourceRect, mahjongModeSourceScale);

            if (moonFxHoldTime > 0f)
                yield return WaitMoon(moonFxHoldTime);

            sourceButton.gameObject.SetActive(false);
            yield return AnimateMainModeButtonsSpit(battleButton, battlePosition, endlessButton, endlessPosition, center);
            yield return AnimateMoonMove(moon, absorbPos, exitPos, moonFxFlyOutTime * 0.72f);

            if (moon != null)
                Destroy(moon.gameObject);

            RevealMainModeButtons(battleButton, endlessButton, closeButton);
            mahjongModeChoiceAnimating = false;
        }

        private IEnumerator AnimateMainModeButtonsSpit(Button battleButton, Vector2 battlePosition, Button endlessButton, Vector2 endlessPosition, Vector2 center)
        {
            RectTransform battleRect = battleButton != null ? battleButton.transform as RectTransform : null;
            RectTransform endlessRect = endlessButton != null ? endlessButton.transform as RectTransform : null;
            float duration = Mathf.Max(0.0001f, mahjongModeSpitTime);
            float t = 0f;

            while (t < duration)
            {
                t += MoonDeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                float scale = Mathf.LerpUnclamped(0.25f, 1f, eased);

                if (battleRect != null)
                {
                    battleRect.anchoredPosition = Vector2.LerpUnclamped(center, battlePosition, eased);
                    battleRect.localScale = Vector3.one * scale;
                }

                if (endlessRect != null)
                {
                    endlessRect.anchoredPosition = Vector2.LerpUnclamped(center, endlessPosition, eased);
                    endlessRect.localScale = Vector3.one * scale;
                }

                yield return null;
            }
        }

        private void RevealMainModeButtons(Button battleButton, Button endlessButton, Button closeButton)
        {
            SetMainModeButtonReady(battleButton);
            SetMainModeButtonReady(endlessButton);

            if (closeButton != null)
                closeButton.interactable = true;
        }

        private void SetMainModeButtonReady(Button button)
        {
            if (button == null)
                return;

            button.interactable = true;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
                rect.localScale = Vector3.one;

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            UIButtonFX fx = button.GetComponent<UIButtonFX>();
            if (fx != null)
                fx.enabled = true;
        }

        private IEnumerator MainMahjongModeSelectedRoutine(string sceneName, string doorSpriteResourcePath, bool reverseDoorMirroring, string selectionMoonSpriteResourcePath, RectTransform overlayRoot, Button battleButton, Button endlessButton, Button closeButton)
        {
            mahjongModeChoiceAnimating = true;

            SetModeChoiceInteractive(battleButton, false);
            SetModeChoiceInteractive(endlessButton, false);
            if (closeButton != null)
                closeButton.interactable = false;

            RectTransform battleRect = battleButton != null ? battleButton.transform as RectTransform : null;
            RectTransform endlessRect = endlessButton != null ? endlessButton.transform as RectTransform : null;
            Vector2 center = ResolveModeButtonsCenter(battleRect, endlessRect);
            float stackHeight = ResolveModeButtonsStackHeight(battleRect, endlessRect);
            Vector2 moonSize = new Vector2(
                Mathf.Max(mahjongModeSelectionMoonSize.x, stackHeight * 0.62f),
                Mathf.Max(mahjongModeSelectionMoonSize.y, stackHeight * 0.62f));
            Vector2 absorbMoonSize = new Vector2(
                Mathf.Max(mahjongModeSelectionMoonAbsorbSize.x, stackHeight),
                Mathf.Max(mahjongModeSelectionMoonAbsorbSize.y, stackHeight));

            RectTransform moon = CreateMoonFxImage(overlayRoot, ResolveSelectionMoonSprite(selectionMoonSpriteResourcePath));
            if (moon == null)
            {
                HideModeChoiceButton(battleButton);
                HideModeChoiceButton(endlessButton);
                mahjongModeOverlay = null;
                LoadSceneByName(sceneName, false, useDoorFxForMahjong, false, doorSpriteResourcePath, reverseDoorMirroring);
                yield break;
            }

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(-halfWidth - moonSize.x - moonFxStartOffsetX, center.y);
            Vector2 holdPos = center;
            Vector2 exitPos = new Vector2(halfWidth + moonSize.x + moonFxExitOffsetX, center.y);

            moon.sizeDelta = moonSize;
            moon.anchoredPosition = startPos;
            moon.localScale = Vector3.one;
            PlaceMoonUnderInfoLayer(moon, overlayRoot);

            yield return AnimateMoonMove(moon, startPos, holdPos, mahjongModeSelectionMoonFlyInTime);

            float holdBeforeAbsorb = Mathf.Max(0f, mahjongModeSelectionMoonHoldTime - mahjongModeSelectionAbsorbTime);
            if (holdBeforeAbsorb > 0f)
                yield return WaitMoon(holdBeforeAbsorb);

            yield return AnimateModeButtonsAbsorb(battleButton, endlessButton, moon, moonSize, absorbMoonSize, holdPos);

            yield return AnimateMoonMove(moon, holdPos, exitPos, mahjongModeSelectionMoonFlyOutTime);

            if (moon != null)
                Destroy(moon.gameObject);

            if (mahjongModeOverlay != null)
            {
                mahjongModeOverlay.SetActive(false);
                Destroy(mahjongModeOverlay);
                mahjongModeOverlay = null;
            }

            mahjongModeSourceButton = null;
            mahjongModePausedVisualFx = System.Array.Empty<MonoBehaviour>();
            mahjongModeChoiceAnimating = false;
            LoadSceneByName(sceneName, false, useDoorFxForMahjong, false, doorSpriteResourcePath, reverseDoorMirroring);
        }

        private void MainMahjongModeSelected(string sceneName, string doorSpriteResourcePath, bool reverseDoorMirroring, RectTransform overlayRoot, Button battleButton, Button endlessButton, Button closeButton)
        {
            if (isLoading)
                return;

            mahjongModeChoiceAnimating = true;
            SetModeChoiceInteractive(battleButton, false);
            SetModeChoiceInteractive(endlessButton, false);
            if (closeButton != null)
                closeButton.interactable = false;

            if (mahjongModeOverlay != null)
            {
                mahjongModeOverlay.SetActive(false);
                Destroy(mahjongModeOverlay);
                mahjongModeOverlay = null;
            }

            mahjongModeSourceButton = null;
            mahjongModePausedVisualFx = System.Array.Empty<MonoBehaviour>();
            mahjongModeChoiceAnimating = false;
            LoadSceneByName(sceneName, false, useDoorFxForMahjong, false, doorSpriteResourcePath, reverseDoorMirroring);
        }

        private void SetModeChoiceInteractive(Button button, bool interactive)
        {
            if (button == null)
                return;

            button.interactable = interactive;

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = interactive;
                group.blocksRaycasts = interactive;
            }
        }

        private void HideModeChoiceButton(Button button)
        {
            if (button == null)
                return;

            UIButtonFX fx = button.GetComponent<UIButtonFX>();
            if (fx != null)
                fx.enabled = false;

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                return;
            }

            button.gameObject.SetActive(false);
        }

        private Sprite ResolveSelectionMoonSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return moonFxSprite;

            battleSelectionMoonSprite = LoadResourceSprite(battleSelectionMoonSprite, resourcePath);
            return battleSelectionMoonSprite != null ? battleSelectionMoonSprite : moonFxSprite;
        }

        private IEnumerator AnimateModeButtonsAbsorb(Button battleButton, Button endlessButton, RectTransform moon, Vector2 moonStartSize, Vector2 moonAbsorbSize, Vector2 center)
        {
            RectTransform battleRect = battleButton != null ? battleButton.transform as RectTransform : null;
            RectTransform endlessRect = endlessButton != null ? endlessButton.transform as RectTransform : null;
            Vector2 battleStart = battleRect != null ? battleRect.anchoredPosition : center;
            Vector2 endlessStart = endlessRect != null ? endlessRect.anchoredPosition : center;
            Vector3 battleScale = battleRect != null ? battleRect.localScale : Vector3.one;
            Vector3 endlessScale = endlessRect != null ? endlessRect.localScale : Vector3.one;

            float duration = Mathf.Max(0.0001f, mahjongModeSelectionAbsorbTime);
            float t = 0f;

            while (t < duration)
            {
                t += MoonDeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                float scale = Mathf.LerpUnclamped(1f, 0.08f, eased);

                if (battleRect != null)
                {
                    battleRect.anchoredPosition = Vector2.LerpUnclamped(battleStart, center, eased);
                    battleRect.localScale = battleScale * scale;
                }

                if (endlessRect != null)
                {
                    endlessRect.anchoredPosition = Vector2.LerpUnclamped(endlessStart, center, eased);
                    endlessRect.localScale = endlessScale * scale;
                }

                if (moon != null)
                    moon.sizeDelta = Vector2.LerpUnclamped(moonStartSize, moonAbsorbSize, eased);

                yield return null;
            }

            if (moon != null)
                moon.sizeDelta = moonAbsorbSize;

            HideModeChoiceButton(battleButton);
            HideModeChoiceButton(endlessButton);
        }

        private Vector2 ResolveModeButtonsCenter(RectTransform first, RectTransform second)
        {
            if (first != null && second != null)
                return (first.anchoredPosition + second.anchoredPosition) * 0.5f;

            if (first != null)
                return first.anchoredPosition;

            if (second != null)
                return second.anchoredPosition;

            return Vector2.zero;
        }

        private float ResolveModeButtonsStackHeight(RectTransform first, RectTransform second)
        {
            if (first == null && second == null)
                return mahjongModeSelectionMoonSize.y;

            float top = float.MinValue;
            float bottom = float.MaxValue;
            AddRectVerticalBounds(first, ref top, ref bottom);
            AddRectVerticalBounds(second, ref top, ref bottom);
            return Mathf.Max(mahjongModeSelectionMoonSize.y, top - bottom);
        }

        private static void AddRectVerticalBounds(RectTransform rect, ref float top, ref float bottom)
        {
            if (rect == null)
                return;

            float halfHeight = rect.sizeDelta.y * 0.5f * Mathf.Abs(rect.localScale.y);
            top = Mathf.Max(top, rect.anchoredPosition.y + halfHeight);
            bottom = Mathf.Min(bottom, rect.anchoredPosition.y - halfHeight);
        }

        private void RestoreMainMahjongSourceButton()
        {
            if (mahjongModeSourceButton == null)
                return;

            mahjongModeSourceButton.gameObject.SetActive(true);
            mahjongModeSourceButton.interactable = true;

            RectTransform rect = mahjongModeSourceButton.transform as RectTransform;
            if (rect != null)
                rect.localScale = mahjongModeSourceScale;

            CanvasGroup group = mahjongModeSourceButton.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            RestoreButtonVisualFx(mahjongModePausedVisualFx);
            mahjongModePausedVisualFx = System.Array.Empty<MonoBehaviour>();
            mahjongModeSourceButton = null;
        }

        private Sprite LoadResourceSprite(Sprite cachedSprite, string resourcePath)
        {
            if (cachedSprite != null || string.IsNullOrWhiteSpace(resourcePath))
                return cachedSprite;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void LoadSceneByName(string sceneName, bool allowReloadSameScene = false, bool useDoorFx = false, bool useMainMoonFx = false, string doorSpriteResourcePath = null, bool reverseDoorMirroring = false)
        {
            if (isLoading)
            {
                if (loadingStartedRealtime > 0f && Time.realtimeSinceStartup - loadingStartedRealtime > LoadingStaleSeconds)
                {
                    Debug.LogWarning($"[SceneNavigator] Loading state was stale for {Time.realtimeSinceStartup - loadingStartedRealtime:0.0}s. Resetting before loading '{sceneName}'.");
                    isLoading = false;
                    loadingStartedRealtime = -1f;
                }
                else
                {
                    Debug.LogWarning($"[SceneNavigator] Ignored scene load '{sceneName}' because another load is active.");
                    return;
                }
            }

            if (isLoading)
                return;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[SceneNavigator] Scene name is empty.");
                return;
            }

            if (!allowReloadSameScene && IsCurrentScene(sceneName))
            {
                Debug.Log($"[SceneNavigator] Scene '{sceneName}' is already active.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneNavigator] Scene '{sceneName}' is not in Build Settings or name is wrong.");
                return;
            }

            isLoading = true;
            loadingStartedRealtime = Time.realtimeSinceStartup;
            Debug.Log($"[SceneNavigator] Loading scene: {sceneName}");
            MainGameLaunchBootstrap.PrepareForSceneExit(sceneName);
            BattleLobbyUiCoordinator.PrepareForSceneExit(sceneName);
            MainSceneResponsiveLayout.CancelMainReturnSanitizers();
            MainHubStateController.CancelMainEntryStabilization();
            CleanupMainSceneRuntimeWindowsBeforeLeaving(sceneName);

            bool isPortraitOnlyScene = IsPortraitOnlyScene(sceneName);
            if (isPortraitOnlyScene)
            {
                if (!useDoorFx)
                {
                    SceneOrientationPolicy.ApplyPortraitOnly();
                    SceneManager.LoadScene(sceneName);
                    isLoading = false;
                    loadingStartedRealtime = -1f;
                    return;
                }
            }

            if (useMainMoonFx && TryStartMoonFxThenLoad(sceneName, useDoorFx, doorSpriteResourcePath, reverseDoorMirroring))
                return;

            var doorFx = useDoorFx ? MahjongGame.DoorFx.EnsureRuntime() : MahjongGame.DoorFx.I;

            if (useDoorFx && doorFx != null && doorFx.isActiveAndEnabled && doorFx.IsReady() && !doorFx.IsBusy)
            {
                doorFx.LoadScene(
                    sceneName,
                    doorSpriteResourcePath,
                    reverseDoorMirroring,
                    isPortraitOnlyScene ? SceneOrientationPolicy.ApplyPortraitOnly : null,
                    string.Equals(sceneName, orbiosisScene, System.StringComparison.Ordinal) && orbiosisDoorVerticalSplit);
                isLoading = false;
                loadingStartedRealtime = -1f;
                return;
            }

            if (useDoorFx && doorFx != null && doorFx.IsBusy)
            {
                Debug.LogWarning($"[SceneNavigator] DoorFx is busy while loading '{sceneName}'. Falling back to direct scene load.");
                doorFx.ForceOpenNow();
            }

            if (isPortraitOnlyScene)
                SceneOrientationPolicy.ApplyPortraitOnly();

            SceneManager.LoadScene(sceneName);
            isLoading = false;
            loadingStartedRealtime = -1f;
        }

        private bool IsPortraitOnlyScene(string sceneName)
        {
#if UNITY_IOS
            return false;
#else
            return sceneName == symbiGridScene
                || sceneName == orbiosisScene;
#endif
        }

        private bool TryStartMoonFxThenLoad(string sceneName, bool useDoorFx, string doorSpriteResourcePath, bool reverseDoorMirroring)
        {
            Button targetButton = GetEventButton();
            RectTransform targetRect = targetButton != null ? targetButton.transform as RectTransform : null;
            if (targetRect == null)
                return false;

            Canvas sourceCanvas = targetButton.GetComponentInParent<Canvas>();
            Canvas rootCanvas = CentralPointLayout.ResolveMainCanvas();
            if (rootCanvas == null)
                rootCanvas = sourceCanvas != null ? sourceCanvas : FindAnyObjectByType<Canvas>();

            if (rootCanvas == null)
                return false;

            if (moonFxSprite == null && !string.IsNullOrWhiteSpace(moonFxSpriteResourcePath))
                moonFxSprite = Resources.Load<Sprite>(moonFxSpriteResourcePath);

            if (moonFxSprite == null)
            {
                Debug.LogWarning($"[SceneNavigator] Moon FX sprite not found in Resources: {moonFxSpriteResourcePath}");
                return false;
            }

            StartCoroutine(MoonFxThenLoadRoutine(sceneName, useDoorFx, doorSpriteResourcePath, reverseDoorMirroring, rootCanvas, targetButton, targetRect));
            return true;
        }

        private IEnumerator MoonFxThenLoadRoutine(string sceneName, bool useDoorFx, string doorSpriteResourcePath, bool reverseDoorMirroring, Canvas rootCanvas, Button targetButton, RectTransform targetRect)
        {
            GameObject overlay = new GameObject("MainMoonEffectOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            overlay.transform.SetParent(rootCanvas.transform, false);
            overlay.transform.SetAsLastSibling();

            RectTransform overlayRoot = overlay.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;

            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingLayerName = rootCanvas.sortingLayerName;
            overlayCanvas.sortingOrder = moonFxSortingOrder;

            GraphicRaycaster raycaster = overlay.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            Canvas.ForceUpdateCanvases();

            RectTransform moon = CreateMoonFxImage(overlayRoot);
            if (moon == null)
            {
                Destroy(overlay);
                LoadSceneAfterMoon(sceneName, useDoorFx, doorSpriteResourcePath, reverseDoorMirroring);
                yield break;
            }

            CanvasGroup targetGroup = targetButton.GetComponent<CanvasGroup>();
            if (targetGroup == null)
                targetGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

            MonoBehaviour[] pausedVisualFx = DisableButtonVisualFx(targetButton);
            targetGroup.alpha = 1f;
            targetGroup.blocksRaycasts = false;
            targetGroup.interactable = false;

            Vector3 startButtonScale = targetRect.localScale;
            Vector2 center = GetTargetLocalPosition(rootCanvas, overlayRoot, targetRect);
            center.y += moonFxYOffset;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(-halfWidth - moonFxStartSize.x - moonFxStartOffsetX, center.y);
            Vector2 absorbPos = center;
            Vector2 exitPos = new Vector2(halfWidth + moonFxAbsorbSize.x + moonFxExitOffsetX, center.y);

            moon.anchoredPosition = startPos;
            moon.sizeDelta = moonFxStartSize;
            moon.localScale = Vector3.one;
            moon.SetAsLastSibling();

            yield return AnimateMoonMove(moon, startPos, absorbPos, moonFxFlyInTime);

            if (moonFxLandHoldTime > 0f)
                yield return WaitMoon(moonFxLandHoldTime);

            yield return AnimateMoonAbsorb(moon, targetRect, startButtonScale);

            if (moonFxHoldTime > 0f)
                yield return WaitMoon(moonFxHoldTime);

            targetButton.gameObject.SetActive(false);
            yield return AnimateMoonMove(moon, absorbPos, exitPos, moonFxFlyOutTime);

            RestoreButtonVisualFx(pausedVisualFx);
            Destroy(overlay);
            LoadSceneAfterMoon(sceneName, useDoorFx, doorSpriteResourcePath, reverseDoorMirroring);
        }

        private RectTransform CreateMoonFxImage(RectTransform overlayRoot)
        {
            return CreateMoonFxImage(overlayRoot, moonFxSprite);
        }

        private RectTransform CreateMoonFxImage(RectTransform overlayRoot, Sprite sprite)
        {
            if (overlayRoot == null || sprite == null)
                return null;

            GameObject go = new GameObject("MoonMainEffect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(overlayRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = moonFxStartSize;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            return rt;
        }

        private static void PlaceMoonUnderInfoLayer(RectTransform moon, RectTransform overlayRoot)
        {
            if (moon == null || overlayRoot == null)
                return;

            int index = overlayRoot.childCount - 1;
            for (int i = 0; i < overlayRoot.childCount; i++)
            {
                Transform child = overlayRoot.GetChild(i);
                if (child == null || child == moon)
                    continue;

                if (child.GetComponent<MainInfoCard>() != null || child.GetComponent<MainInfoLayerElement>() != null)
                {
                    index = Mathf.Max(0, i - 1);
                    break;
                }
            }

            moon.SetSiblingIndex(index);
        }

        private IEnumerator AnimateMoonAbsorb(RectTransform moon, RectTransform targetRect, Vector3 startButtonScale)
        {
            Vector2 startSize = moon.sizeDelta;
            float duration = Mathf.Max(0.0001f, moonFxAbsorbTime);
            float t = 0f;

            while (t < duration)
            {
                t += MoonDeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                if (moon != null)
                    moon.sizeDelta = Vector2.LerpUnclamped(startSize, moonFxAbsorbSize, eased);

                if (targetRect != null)
                    targetRect.localScale = Vector3.LerpUnclamped(startButtonScale, Vector3.zero, eased);

                yield return null;
            }

            if (moon != null)
                moon.sizeDelta = moonFxAbsorbSize;

            if (targetRect != null)
                targetRect.localScale = Vector3.zero;
        }

        private IEnumerator AnimateMoonMove(RectTransform moon, Vector2 from, Vector2 to, float duration)
        {
            if (moon == null)
                yield break;

            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);
            float startZ = moon.localEulerAngles.z;

            while (t < duration)
            {
                if (moon == null)
                    yield break;

                t += MoonDeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                moon.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);

                if (moonFxRotateWhileMoving)
                {
                    float z = startZ - moonFxRotateSpeed * t;
                    moon.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (moon != null)
                moon.anchoredPosition = to;
        }

        private void LoadSceneAfterMoon(string sceneName, bool useDoorFx, string doorSpriteResourcePath, bool reverseDoorMirroring)
        {
            var doorFx = MahjongGame.DoorFx.I;
            if (useDoorFx && doorFx != null && doorFx.isActiveAndEnabled && doorFx.IsReady())
            {
                doorFx.LoadScene(sceneName, doorSpriteResourcePath, reverseDoorMirroring);
                isLoading = false;
                loadingStartedRealtime = -1f;
                return;
            }

            SceneManager.LoadScene(sceneName);
            isLoading = false;
            loadingStartedRealtime = -1f;
        }

        private Button GetEventButton()
        {
            GameObject selected = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
                : null;

            return selected != null ? selected.GetComponentInParent<Button>() : null;
        }

        private Vector2 GetTargetLocalPosition(Canvas rootCanvas, RectTransform overlayRoot, RectTransform target)
        {
            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, cam, out Vector2 localPoint);
            return localPoint;
        }

        private MonoBehaviour[] DisableButtonVisualFx(Button targetButton)
        {
            if (targetButton == null)
                return System.Array.Empty<MonoBehaviour>();

            MonoBehaviour[] behaviours = targetButton.GetComponentsInChildren<MonoBehaviour>(true);
            var paused = new System.Collections.Generic.List<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.enabled)
                    continue;

                if (behaviour.GetType().Name != "UIButtonFX")
                    continue;

                behaviour.enabled = false;
                paused.Add(behaviour);
            }

            return paused.ToArray();
        }

        private void RestoreButtonVisualFx(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                    behaviours[i].enabled = true;
            }
        }

        private float MoonDeltaTime()
        {
            return moonFxUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private object WaitMoon(float seconds)
        {
            if (moonFxUseUnscaledTime)
                return new WaitForSecondsRealtime(seconds);

            return new WaitForSeconds(seconds);
        }

        private void CleanupMainSceneRuntimeWindowsBeforeLeaving(string targetSceneName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.name, mainScene, System.StringComparison.Ordinal))
                return;

            if (string.Equals(targetSceneName, mainScene, System.StringComparison.Ordinal))
                return;

            PersistSceneServices<ProfileService>(activeScene);
            PersistSceneServices<CurrencyService>(activeScene);
            PersistSceneServices<MailboxService>(activeScene);
            PersistSceneServices<Monetization.MonetizationService>(activeScene);
            PersistSceneServices<MahjongTitleService>(activeScene);
            PersistSceneServices<MahjongRewardService>(activeScene);
            PersistSceneServices<FriendsService>(activeScene);
            PersistSceneServices<AllianceService>(activeScene);
            PersistSceneServices<GlobalChatService>(activeScene);

            DestroySceneRuntimeWindows<MailboxUI>(activeScene);
            DestroySceneRuntimeWindows<FriendsUI>(activeScene);
            DestroySceneRuntimeWindows<AllianceUI>(activeScene);
            DestroySceneRuntimeWindows<GlobalChatUI>(activeScene);
        }

        private static void PersistSceneServices<T>(Scene scene) where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || component.gameObject.scene != scene)
                    continue;

                PersistentObjectUtility.DontDestroyOnLoad(component.gameObject);
            }
        }

        private static void DestroySceneRuntimeWindows<T>(Scene scene) where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || component.gameObject.scene != scene)
                    continue;

                component.gameObject.SetActive(false);
            }
        }
    }
}
