using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MahjongGame.Monetization;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainSceneResponsiveLayout : MonoBehaviour
    {
        private const string MainSceneName = "Main";
        private const string SudokuButtonName = "BrainGamesSudokuButton";
        private const string SudokuRuntimeButtonName = "Btn_SymSudoku_Runtime";
        private const string SudokuButtonSpriteResourcePath = "Mahjong/Sprites/MainSudoku_Selected";
        private const string SudokuDevelopmentBadgeName = "SudokuDevelopmentBadge";
        private const string SudokuDevelopmentBadgeText = "В разработке";
        private const string MahjongButtonName = "BrainGamesMahjongButton";
        private const string MahjongButtonSpriteResourcePath = "Mahjong/Sprites/MainSettings/MainMahjongWorldButton";
        private const string SymbiGridButtonName = "BrainGamesSymbiGridButton";
        private const string SymbiGridButtonSpriteResourcePath = "SymbiGrid/SymbiGridTitleLogo";
        private const string OrbiosisButtonName = "BrainGamesOrbiosisButton";
        private const string OrbiosisButtonSpriteResourcePath = "Orbiosis/OrbiosisLogo_Symmetric_ImageGen_02_Clean";
        private const string OrbiosisDevelopmentBadgeName = "OrbiosisDevelopmentBadge";
        private const string OrbiosisDevelopmentNoticeName = "MainDevelopmentNotice";
        private const string SymbiozButtonName = "BrainGamesSymbiozButton";
        private const string SymbiozButtonSpriteResourcePath = "DynastyLegacy/DynastyLegacyButton";
        private const string SymbiozLoginPanelSpriteResourcePath = "DynastyLegacy/DynastyLegacyLoginPanel";
        private const string LegacyBlockBustButtonName = "BrainGamesBlockBustButton";
        private const string BrainGamesTabButtonName = "Btn_BrainGamesTab";
        private const string BrainGamesRuntimeCanvasName = "BrainGamesRuntimeCanvas";
        private const string BrainGamesTabSpriteResourcePath = "Mahjong/Sprites/MainSettings/ZekaOyunlariButton";
        private const string SymbiozLoginValue = "1111";
        private const string SymbiozPasswordValue = "1111";
        private const string OrbiosisHangarBackgroundSpriteResourcePath = "Orbiosis/BaseHangarUnified_01";
        private const string OrbiosisHangarDoorSpriteResourcePath = "Orbiosis/MenuHangarDoorLeaf_01";
        private const float OrbiosisHangarDoorCloseSeconds = 0.54f;
        private const float OrbiosisHangarDoorOpenSeconds = 0.68f;
        private const float OrbiosisHangarDoorHoldSeconds = 0.14f;
        private const int BrainGamesRuntimeSortingOrder = 32750;
        private static readonly bool UseLegacyBrainGamesRuntimeLayer = false;

        private Canvas canvas;
        private CanvasScaler scaler;
        private static Sprite sudokuButtonSprite;
        private static Sprite mahjongButtonSprite;
        private static Sprite symbiGridButtonSprite;
        private static Sprite orbiosisButtonSprite;
        private static Sprite symbiozButtonSprite;
        private static Sprite symbiozLoginPanelSprite;
        private static Sprite brainGamesTabSprite;
        private static Sprite orbiosisHangarBackgroundSprite;
        private static Sprite orbiosisHangarDoorSprite;
        private Vector2 lastScreenSize = new Vector2(-1f, -1f);
        private bool lastPortrait;
        private int layoutWarmupFrames = 180;
        private bool brainGamesOpen;
        private bool orbiosisHangarOpen;
        private Coroutine orbiosisHangarRoutine;
        private SceneNavigator sceneNavigator;
        private Button brainGamesTabButton;
        private TMP_Text brainGamesTabLabel;
        private RectTransform brainGamesRuntimeRoot;
        private RectTransform brainGamesPanelRect;
        private Button brainGamesBackdropButton;
        private Button brainGamesCloseButton;
        private Button brainGamesMahjongButton;
        private Button brainGamesSudokuButton;
        private Button brainGamesSymbiGridButton;
        private Button brainGamesOrbiosisButton;
        private Button brainGamesSymbiozButton;
        private Button brainGamesOkeyButton;
        private RectTransform symbiozLoginRoot;
        private RectTransform symbiozLoginCard;
        private TMP_InputField symbiozLoginInput;
        private TMP_InputField symbiozPasswordInput;
        private TextMeshProUGUI symbiozLoginErrorText;
        private Button symbiozLoginSubmitButton;
        private Button symbiozLoginCancelButton;
        private RectTransform orbiosisHangarRoot;
        private CanvasGroup orbiosisHangarGroup;
        private RectTransform orbiosisHangarBackgroundRect;
        private RectTransform orbiosisHangarLogoRect;
        private RectTransform orbiosisHangarLeftDoor;
        private RectTransform orbiosisHangarRightDoor;
        private Button orbiosisHangarCloseButton;
        private Button orbiosisHangarStartButton;
        private RectTransform orbiosisDevelopmentRoot;
        private RectTransform orbiosisDevelopmentCard;
        private RectTransform orbiosisDevelopmentLogo;
        private TextMeshProUGUI mainDevelopmentTitleText;
        private TextMeshProUGUI orbiosisDevelopmentStatusText;
        private TextMeshProUGUI orbiosisDevelopmentBodyText;
        private Button orbiosisDevelopmentBackButton;
        private Coroutine mainReturnSanitizeRoutine;
        private float nextBrainGamesGuardRealtime;
        private int brainGamesGuardRestoreCount;
        private bool mainDevelopmentShowsLogo;

        private readonly LandscapeLayoutModule landscapeLayout = new();
        private readonly PortraitLayoutModule portraitLayout = new();

        public static bool IsOrbiosisAccessEnabled => false;

        public static bool ShowDevelopmentNotice(string titleKey, string bodyKey)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MainSceneName, System.StringComparison.Ordinal) ||
                !MainHubStateController.CanOpenMainWindow("MainDevelopmentNotice"))
            {
                return false;
            }

            MainSceneResponsiveLayout layout = FindAnyObjectByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            if (layout == null)
            {
                ForceRefreshCurrentScene();
                layout = FindAnyObjectByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            }

            if (layout == null)
                return false;

            layout.OpenMainDevelopmentNotice(titleKey, "main.feature_unavailable.status", bodyKey, false);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsUsableScene(scene))
                return;

            if (!string.Equals(scene.name, MainSceneName, System.StringComparison.Ordinal))
                return;

            Canvas targetCanvas = FindMainCanvas();
            if (targetCanvas == null || targetCanvas.GetComponent<MainSceneResponsiveLayout>() != null)
                return;

            targetCanvas.gameObject.AddComponent<MainSceneResponsiveLayout>();
        }

        public static void ForceRefreshCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!string.Equals(scene.name, MainSceneName, System.StringComparison.Ordinal))
                return;

            NormalizeMainRuntimeState(true);
            EnsureForScene(scene);

            MainSceneResponsiveLayout[] layouts = FindObjectsByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            for (int i = 0; i < layouts.Length; i++)
            {
                MainSceneResponsiveLayout layout = layouts[i];
                if (layout == null)
                    continue;

                layout.CloseTransientMainOverlays();
                layout.layoutWarmupFrames = 12;
                layout.ApplyLayout();
                layout.StartMainReturnSanitizer();
            }

            NormalizeMainRuntimeState(true);
            Canvas.ForceUpdateCanvases();
        }

        private static Canvas FindMainCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Scene activeScene = SceneManager.GetActiveScene();
            Canvas fallback = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null || !candidate.gameObject.scene.IsValid() || candidate.gameObject.scene != activeScene)
                    continue;

                if (string.Equals(candidate.name, "Canvas", System.StringComparison.Ordinal))
                    return candidate;

                if (fallback == null && IsUsableMainCanvas(candidate))
                    fallback = candidate;
            }

            return fallback;
        }

        private static bool IsUsableMainCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            string canvasName = canvas.name;
            return !CentralPointLayout.IsRuntimeOverlayCanvasName(canvasName);
        }

        private static bool IsUsableScene(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private void Awake()
        {
            NormalizeMainRuntimeState(true);
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            SceneOrientationPolicy.ApplyLandscapeOnly();
            ApplyLayout();
            StartMainReturnSanitizer();
        }

        private void OnEnable()
        {
            NormalizeMainRuntimeState(true);
            SceneOrientationPolicy.ApplyLandscapeOnly();
            layoutWarmupFrames = 180;
            ApplyLayout();
            StartMainReturnSanitizer();
        }

        private void OnDisable()
        {
            if (mainReturnSanitizeRoutine != null)
            {
                StopCoroutine(mainReturnSanitizeRoutine);
                mainReturnSanitizeRoutine = null;
            }
        }

        private void StartMainReturnSanitizer()
        {
            if (!isActiveAndEnabled)
                return;

            if (mainReturnSanitizeRoutine != null)
                StopCoroutine(mainReturnSanitizeRoutine);

            mainReturnSanitizeRoutine = StartCoroutine(MainReturnSanitizerRoutine());
        }

        public static void CancelMainReturnSanitizers()
        {
            MainSceneResponsiveLayout[] layouts = FindObjectsByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            for (int i = 0; i < layouts.Length; i++)
            {
                MainSceneResponsiveLayout layout = layouts[i];
                if (layout == null || layout.mainReturnSanitizeRoutine == null)
                    continue;

                layout.StopCoroutine(layout.mainReturnSanitizeRoutine);
                layout.mainReturnSanitizeRoutine = null;
            }
        }

        private IEnumerator MainReturnSanitizerRoutine()
        {
            for (int frame = 0; frame < 120; frame++)
            {
                if (EntryMainTransitionFx.IsTransitionActive)
                {
                    yield return null;
                    continue;
                }

                bool finalPass = frame == 0 || frame == 3 || frame == 12 || frame == 35 || frame == 119;
                NormalizeMainRuntimeState(finalPass);
                CloseTransientMainOverlays();
                ApplyLayout();
                Canvas.ForceUpdateCanvases();
                yield return null;
            }

            mainReturnSanitizeRoutine = null;
        }

        private static void NormalizeMainRuntimeState(bool strict)
        {
            Time.timeScale = 1f;
            SceneOrientationPolicy.ApplyLandscapeOnly();
            if (strict)
                MainHubStateController.BeginMainEntryStabilization();
            EnsureMainEventSystem();
            DoorFx.ForceOpenAll();
            SymbiGridSceneTransitionFx.ForceClearAll();
            SettingsMenuUI.ForceCloseAllSettingsMenus();
            MainShopUI.ForceResetAllToClosed(strict ? 2.5f : 0.25f);
            DynastyEconomyRuntime.EnsureServices();
            CloseMainRuntimePanels();
            if (strict)
                CleanupMainReturnResidue();

            SuppressMainChrome(false);
            if (strict)
                EnsureMainRuntimeWidgets();

            RelayoutMainRuntimeWidgets();
            MainShopUI.ForceResetAllToClosed(strict ? 2.5f : 0.25f);
            CloseMainRuntimePanels();
            DisableTransitionCanvases();
        }

        private void CloseTransientMainOverlays()
        {
            brainGamesOpen = false;
            orbiosisHangarOpen = false;

            if (orbiosisHangarRoutine != null)
            {
                StopCoroutine(orbiosisHangarRoutine);
                orbiosisHangarRoutine = null;
            }

            if (brainGamesPanelRect != null)
            {
                Image panelImage = brainGamesPanelRect.GetComponent<Image>();
                if (panelImage != null)
                    panelImage.raycastTarget = false;

                Button panelButton = brainGamesPanelRect.GetComponent<Button>();
                if (panelButton != null)
                    panelButton.enabled = false;
            }

            if (orbiosisHangarRoot != null)
                orbiosisHangarRoot.gameObject.SetActive(false);

            if (orbiosisDevelopmentRoot != null)
                orbiosisDevelopmentRoot.gameObject.SetActive(false);

            if (orbiosisHangarGroup != null)
            {
                orbiosisHangarGroup.interactable = false;
                orbiosisHangarGroup.blocksRaycasts = false;
            }

            if (symbiozLoginRoot != null)
                symbiozLoginRoot.gameObject.SetActive(false);
        }

        private static void EnsureMainEventSystem()
        {
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();

            EventSystem current = EventSystem.current;
            if (current != null)
            {
                current.gameObject.SetActive(true);
                current.enabled = true;
                current.sendNavigationEvents = true;
                current.SetSelectedGameObject(null);
                return;
            }

#if ENABLE_INPUT_SYSTEM
            GameObject obj = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
            GameObject obj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
            Scene activeScene = SceneManager.GetActiveScene();
            if (IsUsableScene(activeScene))
                SceneManager.MoveGameObjectToScene(obj, activeScene);
            EventSystem.current = obj.GetComponent<EventSystem>();
        }

        private static void CloseMainRuntimePanels()
        {
            SetNamedObjectActive("DynastyVaultOverlay", false);
            SetNamedObjectActive("DynastyBankOverlay", false);
            SetNamedObjectActive("ExchangeMonitorOverlay", false);
            SetNamedObjectActive("MainWeeklyRewardOverlay", false);
            SetNamedObjectActive("MainRewardedBonusOverlay", false);
            SetNamedObjectActive("ShopOverlay", false);
            SetNamedObjectActive("MainShopOverlay", false);
        }

        private static void EnsureMainRuntimeWidgets()
        {
            MainShopBootstrap.EnsureForCurrentScene();
            MailboxBootstrap.EnsureForCurrentScene();
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();
            AllianceBootstrap.EnsureForCurrentScene();
            DynastyCentralEconomyBootstrap.EnsureForCurrentScene();
            MainExchangeMonitorBootstrap.EnsureForCurrentScene();
            MainWeeklyRewardBootstrap.EnsureForCurrentScene();
            MainRewardedBonusBootstrap.EnsureForCurrentScene();
        }

        private static void RelayoutMainRuntimeWidgets()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            LayoutNamedMainButton("ButtonOpenShop", MainLobbyBottomButtonSlot.Shop);

            MailboxUI[] mailboxUis = FindObjectsByType<MailboxUI>(FindObjectsInactive.Include);
            for (int i = 0; i < mailboxUis.Length; i++)
            {
                MailboxUI ui = mailboxUis[i];
                if (ui != null && ui.gameObject.scene == activeScene)
                    ui.LayoutToggleButton();
            }

            FriendsUI[] friendsUis = FindObjectsByType<FriendsUI>(FindObjectsInactive.Include);
            for (int i = 0; i < friendsUis.Length; i++)
            {
                FriendsUI ui = friendsUis[i];
                if (ui != null && ui.gameObject.scene == activeScene)
                    ui.LayoutToggleButton();
            }

            GlobalChatUI[] chatUis = FindObjectsByType<GlobalChatUI>(FindObjectsInactive.Include);
            for (int i = 0; i < chatUis.Length; i++)
            {
                GlobalChatUI ui = chatUis[i];
                if (ui != null && ui.gameObject.scene == activeScene)
                    ui.LayoutToggleButton();
            }

            AllianceUI[] allianceUis = FindObjectsByType<AllianceUI>(FindObjectsInactive.Include);
            for (int i = 0; i < allianceUis.Length; i++)
            {
                AllianceUI ui = allianceUis[i];
                if (ui != null && ui.gameObject.scene == activeScene)
                    ui.LayoutToggleButton();
            }

            DynastyEconomyWindowBase[] economyWindows = FindObjectsByType<DynastyEconomyWindowBase>(FindObjectsInactive.Include);
            for (int i = 0; i < economyWindows.Length; i++)
            {
                DynastyEconomyWindowBase window = economyWindows[i];
                if (window == null || window.gameObject.scene != activeScene)
                    continue;

                window.ForceMainMenuLayout();
            }
        }

        private static void LayoutNamedMainButton(string objectName, MainLobbyBottomButtonSlot slot)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null || !string.Equals(obj.name, objectName, System.StringComparison.Ordinal))
                    continue;

                RectTransform rect = obj.transform as RectTransform;
                if (rect == null)
                    continue;

                MainLobbyUiCoordinator.LayoutBottomButton(rect, slot);
            }
        }

        private static void CleanupMainReturnResidue()
        {
            DestroyObjectsByExactName("SymbiGridRoot");
            DestroyObjectsByExactName("OrbiosisRoot");
            DestroyObjectsByExactName("SymbiGridSceneTransitionFx");
            DestroyObjectsByExactName("SymbiGridOrientationBlackout");
            CleanupFinishedEntryMainTransitions();
            DestroyObjectsByExactName("RuntimeDoorTransition", keepFirst: true);
            CleanupDuplicateComponents<MainShopUI>();
            CleanupDuplicateComponents<MailboxUI>();
            CleanupDuplicateComponents<FriendsUI>();
            CleanupDuplicateComponents<GlobalChatUI>();
            CleanupDuplicateComponents<AllianceUI>();
            CleanupDuplicateComponents<DynastyVaultUI>();
            CleanupDuplicateComponents<DynastyBankUI>();
            CleanupDuplicateComponents<MainExchangeMonitorUI>();
            CleanupDuplicateComponents<MainWeeklyRewardUI>();
            CleanupDuplicateComponents<MainRewardedBonusUI>();
            CleanupLooseMailboxButtons();
            CleanupDuplicateButtons("ButtonOpenShop");
            CleanupDuplicateButtons("MailboxButton");
            CleanupDuplicateButtons("FriendsButton");
            CleanupDuplicateButtons("GlobalChatButton");
            CleanupDuplicateButtons("AllianceButton");
            CleanupDuplicateButtons("DynastyVaultButton");
            CleanupDuplicateButtons("DynastyBankButton");
            CleanupDuplicateButtons("ExchangeMonitorButton");
            CleanupDuplicateButtons("MainWeeklyRewardButton");
            CleanupDuplicateButtons("ButtonOpenMainRewardedBonus");
        }

        private static void CleanupDuplicateComponents<T>() where T : Component
        {
            T[] all = FindObjectsByType<T>(FindObjectsInactive.Include);
            T keep = null;
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < all.Length; i++)
            {
                T item = all[i];
                if (item == null)
                    continue;

                if (keep == null && item.gameObject.scene == activeScene)
                {
                    keep = item;
                    continue;
                }
            }

            if (keep == null && all.Length > 0)
                keep = all[0];

            for (int i = 0; i < all.Length; i++)
            {
                T item = all[i];
                if (item == null || item == keep)
                    continue;

                Destroy(item.gameObject);
            }
        }

        private static void CleanupDuplicateButtons(string buttonName)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            Button keep = null;
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !string.Equals(button.name, buttonName, System.StringComparison.Ordinal))
                    continue;

                if (keep == null && button.gameObject.scene == activeScene)
                    keep = button;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button == keep || !string.Equals(button.name, buttonName, System.StringComparison.Ordinal))
                    continue;

                Destroy(button.gameObject);
            }
        }

        private static void DestroyObjectsByExactName(string objectName, bool keepFirst = false)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            bool kept = false;
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null || !string.Equals(obj.name, objectName, System.StringComparison.Ordinal))
                    continue;

                if (keepFirst && !kept)
                {
                    kept = true;
                    continue;
                }

                Destroy(obj);
            }
        }

        private static void CleanupFinishedEntryMainTransitions()
        {
            EntryMainTransitionFx[] transitions = FindObjectsByType<EntryMainTransitionFx>(FindObjectsInactive.Include);
            for (int i = 0; i < transitions.Length; i++)
            {
                EntryMainTransitionFx transition = transitions[i];
                if (transition == null || transition.IsRunning)
                    continue;

                Destroy(transition.gameObject);
            }
        }

        private static void DisableExtraEventSystemsInActiveScene()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            Scene activeScene = SceneManager.GetActiveScene();
            EventSystem current = EventSystem.current;
            for (int i = 0; i < systems.Length; i++)
            {
                EventSystem system = systems[i];
                if (system == null || system.gameObject.scene != activeScene)
                    continue;

                bool keep = current == null || system == current;
                system.gameObject.SetActive(keep);
                system.enabled = keep;
            }
        }

        private static void DisableTransitionCanvases()
        {
            DisableExtraEventSystemsInActiveScene();

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null)
                    continue;

                string name = candidate.gameObject.name;
                if (name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("Transition", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("Entry", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (candidate.TryGetComponent(out GraphicRaycaster raycaster))
                    raycaster.enabled = false;

                Image[] images = candidate.GetComponentsInChildren<Image>(true);
                for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
                    images[imageIndex].raycastTarget = false;
            }
        }

        private void Update()
        {
            if (EntryMainTransitionFx.IsTransitionActive)
                return;

            GuardBrainGamesLaunchLayer();

            Vector2 screenSize = MainLobbyUiCoordinator.ResolveScreenSize();
            bool portrait = false;
            if (layoutWarmupFrames <= 0 && screenSize == lastScreenSize && portrait == lastPortrait)
                return;

            layoutWarmupFrames--;
            ApplyLayout();
        }

        private void GuardBrainGamesLaunchLayer()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MainSceneName, System.StringComparison.Ordinal))
                return;

            if (Time.unscaledTime < nextBrainGamesGuardRealtime)
                return;

            nextBrainGamesGuardRealtime = Time.unscaledTime + 0.5f;

            if (!UseLegacyBrainGamesRuntimeLayer)
            {
                SetBrainGamesRuntimeLayerVisible(false);
                return;
            }

            if (IsMainOverlayActive())
            {
                SetBrainGamesRuntimeLayerVisible(false);
                return;
            }

            bool needsRestore =
                brainGamesRuntimeRoot == null ||
                !brainGamesRuntimeRoot.gameObject.activeInHierarchy ||
                brainGamesPanelRect == null ||
                !brainGamesPanelRect.gameObject.activeInHierarchy ||
                !IsLaunchButtonVisible(brainGamesMahjongButton) ||
                !IsLaunchButtonVisible(brainGamesSymbiGridButton) ||
                !IsLaunchButtonVisible(brainGamesOrbiosisButton) ||
                !IsLaunchButtonVisible(brainGamesSymbiozButton);

            if (!needsRestore)
                return;

            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (sceneNavigator == null)
                sceneNavigator = FindAnyObjectByType<SceneNavigator>();

            EnsureBrainGamesTab();
            LayoutBrainGamesTab(false);
            SetBrainGamesRuntimeLayerVisible(true);
            HideLegacyGameEntryButtons();
            Canvas.ForceUpdateCanvases();

            if (brainGamesGuardRestoreCount < 8)
            {
                brainGamesGuardRestoreCount++;
                Debug.Log("[MainSceneResponsiveLayout] Restored Main game launch layer for Android/runtime stability.");
            }
        }

        private void SetBrainGamesRuntimeLayerVisible(bool visible)
        {
            if (brainGamesRuntimeRoot == null)
                brainGamesRuntimeRoot = FindBrainGamesRuntimeRoot();

            if (brainGamesPanelRect == null && brainGamesRuntimeRoot != null)
            {
                Transform panel = brainGamesRuntimeRoot.Find("BrainGamesPanel");
                brainGamesPanelRect = panel as RectTransform;
            }

            Canvas runtimeCanvas = brainGamesRuntimeRoot != null ? brainGamesRuntimeRoot.GetComponent<Canvas>() : null;
            if (runtimeCanvas != null)
                runtimeCanvas.enabled = visible;

            GraphicRaycaster raycaster = brainGamesRuntimeRoot != null ? brainGamesRuntimeRoot.GetComponent<GraphicRaycaster>() : null;
            if (raycaster != null)
                raycaster.enabled = visible;

            if (brainGamesPanelRect != null && brainGamesPanelRect.gameObject.activeSelf != visible)
                brainGamesPanelRect.gameObject.SetActive(visible);
        }

        private static RectTransform FindBrainGamesRuntimeRoot()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate != null && string.Equals(candidate.name, BrainGamesRuntimeCanvasName, System.StringComparison.Ordinal))
                    return candidate.transform as RectTransform;
            }

            return null;
        }

        private static bool IsMainOverlayActive()
        {
            return IsAnyActiveByName(
                "SymbiozLoginWindow",
                "OrbiosisHangarRoot",
                OrbiosisDevelopmentNoticeName,
                "MainMahjongModeChoice",
                "DynastyVaultOverlay",
                "DynastyBankOverlay",
                "ExchangeMonitorOverlay",
                "MainWeeklyRewardOverlay",
                "MainRewardedBonusOverlay",
                "ShopOverlay",
                "MainShopOverlay",
                "PanelRoot",
                "MailboxPanel",
                "FriendsPanel",
                "ChatPanel",
                "AlliancePanel",
                "AllianceBackdrop",
                "AdminPanelRoot",
                "MainInfoHintOverlay");
        }

        private static bool IsAnyActiveByName(params string[] names)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null || !obj.activeInHierarchy)
                    continue;

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    if (string.Equals(obj.name, names[nameIndex], System.StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        private static bool IsLaunchButtonVisible(Button button)
        {
            if (button == null || button.gameObject == null)
                return false;

            if (!button.gameObject.activeInHierarchy)
                return false;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                return false;

            return rect.rect.width > 1f && rect.rect.height > 1f && rect.lossyScale.sqrMagnitude > 0.01f;
        }

        private void ApplyLayout()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (scaler == null)
                scaler = GetComponent<CanvasScaler>();

            if (sceneNavigator == null)
                sceneNavigator = FindAnyObjectByType<SceneNavigator>();

            Vector2 screenSize = MainLobbyUiCoordinator.ResolveScreenSize();
            bool portrait = false;
            lastScreenSize = screenSize;
            lastPortrait = portrait;

            IMainSceneLayoutModule module = portrait ? (IMainSceneLayoutModule)portraitLayout : landscapeLayout;
            module.ConfigureScaler(scaler);
            EnsureCanvasRectFillsViewport(canvas);
            module.Apply(this);
            if (UseLegacyBrainGamesRuntimeLayer)
            {
                EnsureBrainGamesTab();
                LayoutBrainGamesTab(portrait);
            }
            else
            {
                SetBrainGamesRuntimeLayerVisible(false);
            }
            LayoutOrbiosisHangarMenu();
            LayoutOrbiosisDevelopmentNotice(portrait);
            LayoutSymbiozLoginWindow(portrait);
            if (UseLegacyBrainGamesRuntimeLayer)
                SetBrainGamesRuntimeLayerVisible(!IsMainOverlayActive());
            HideLegacyGameEntryButtons();
            CleanupLooseMailboxButtons();
            RelayoutMainRuntimeWidgets();
            Canvas.ForceUpdateCanvases();
        }

        private RectTransform FindRect(string objectName)
        {
            if (canvas == null || string.IsNullOrWhiteSpace(objectName))
                return null;

            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && string.Equals(child.name, objectName, System.StringComparison.Ordinal))
                    return child as RectTransform;
            }

            return null;
        }

        private Button FindButtonByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && string.Equals(button.name, objectName, System.StringComparison.Ordinal))
                    return button;
            }

            return null;
        }

        private Button FindButtonByText(params string[] labels)
        {
            if (labels == null || labels.Length == 0)
                return null;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                string value = text != null && text.text != null ? text.text.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    string label = labels[labelIndex];
                    if (!string.IsNullOrWhiteSpace(label) && value.IndexOf(label, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return button;
                }
            }

            return null;
        }

        private static void StretchBackground(RectTransform rect, bool preserveAspect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(-4f, -4f);
            rect.offsetMax = new Vector2(4f, 4f);

            if (rect.TryGetComponent(out Image image))
                image.preserveAspect = preserveAspect;
        }

        private static void EnsureCanvasRectFillsViewport(Canvas targetCanvas)
        {
            RectTransform rect = targetCanvas != null ? targetCanvas.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void LayoutButton(Button button, Vector2 position, Vector2 size, Vector3 scale, bool preserveAspect, float fontSize = 0f)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = scale;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            if (button.image != null)
                button.image.preserveAspect = preserveAspect;

            NormalizeVisualChildren(rect);
            LayoutSudokuDevelopmentBadge(button);
            LayoutOrbiosisDevelopmentBadge(button);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null || fontSize <= 0f)
                return;

            label.enableAutoSizing = true;
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(16f, fontSize * 0.58f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
        }

        private void EnsureBrainGamesTab()
        {
            if (canvas == null)
                return;

            RectTransform brainGamesRoot = EnsureBrainGamesRuntimeRoot();
            Transform brainGamesParent = brainGamesRoot != null ? brainGamesRoot : canvas.transform;

            if (brainGamesTabButton == null)
            {
                Transform existingTab = canvas.transform.Find(BrainGamesTabButtonName);
                brainGamesTabButton = existingTab != null
                    ? existingTab.GetComponent<Button>()
                    : CreateRuntimeButton(BrainGamesTabButtonName, "Zeka Oyunları", canvas.transform, 38f);
            }

            if (brainGamesTabButton != null)
            {
                ApplyBrainGamesButtonStyle(brainGamesTabButton);
                brainGamesTabButton.onClick.RemoveListener(ToggleBrainGames);
                brainGamesTabButton.onClick.AddListener(ToggleBrainGames);
                brainGamesTabLabel = brainGamesTabButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (brainGamesPanelRect == null)
            {
                Transform existingPanel = brainGamesParent.Find("BrainGamesPanel");
                if (existingPanel != null)
                {
                    if (existingPanel.gameObject.activeSelf)
                        brainGamesOpen = true;

                    brainGamesPanelRect = existingPanel as RectTransform;
                    Image existingImage = existingPanel.GetComponent<Image>();
                    if (existingImage != null)
                    {
                        existingImage.sprite = null;
                        existingImage.type = Image.Type.Simple;
                        existingImage.color = Color.black;
                        existingImage.raycastTarget = true;
                    }
                    brainGamesBackdropButton = ConfigureBrainGamesBackdrop(existingPanel.gameObject, existingImage);

                    brainGamesCloseButton = existingPanel.Find("BrainGamesClose") != null
                        ? existingPanel.Find("BrainGamesClose").GetComponent<Button>()
                        : null;
                    brainGamesMahjongButton = existingPanel.Find(MahjongButtonName) != null
                        ? existingPanel.Find(MahjongButtonName).GetComponent<Button>()
                        : null;
                    brainGamesSudokuButton = existingPanel.Find(SudokuButtonName) != null
                        ? existingPanel.Find(SudokuButtonName).GetComponent<Button>()
                        : null;
                    brainGamesSymbiGridButton = FindChildButton(existingPanel, SymbiGridButtonName, LegacyBlockBustButtonName);
                    brainGamesOrbiosisButton = existingPanel.Find(OrbiosisButtonName) != null
                        ? existingPanel.Find(OrbiosisButtonName).GetComponent<Button>()
                        : null;
                    brainGamesSymbiozButton = existingPanel.Find(SymbiozButtonName) != null
                        ? existingPanel.Find(SymbiozButtonName).GetComponent<Button>()
                        : null;
                    brainGamesOkeyButton = existingPanel.Find("BrainGamesOkeyButton") != null
                        ? existingPanel.Find("BrainGamesOkeyButton").GetComponent<Button>()
                        : null;

                    if (brainGamesCloseButton != null)
                    {
                        brainGamesCloseButton.onClick.RemoveListener(CloseBrainGames);
                        brainGamesCloseButton.onClick.AddListener(CloseBrainGames);
                    }

                    if (brainGamesMahjongButton == null)
                        brainGamesMahjongButton = CreateRuntimeButton(MahjongButtonName, "Mahjong", existingPanel, 30f);
                    if (brainGamesSudokuButton == null)
                        brainGamesSudokuButton = CreateRuntimeButton(SudokuButtonName, "SUDOKU", existingPanel, 30f);
                    if (brainGamesSymbiGridButton == null)
                        brainGamesSymbiGridButton = CreateRuntimeButton(SymbiGridButtonName, "SYMBIGRID", existingPanel, 30f);
                    if (brainGamesOrbiosisButton == null)
                        brainGamesOrbiosisButton = CreateRuntimeButton(OrbiosisButtonName, "ORBIOSIS", existingPanel, 30f);
                    if (brainGamesSymbiozButton == null)
                        brainGamesSymbiozButton = CreateRuntimeButton(SymbiozButtonName, "Dynasty Legacy - Symbiosis", existingPanel, 28f);
                    SetButtonText(brainGamesSymbiGridButton, "SYMBIGRID");
                    SetButtonText(brainGamesOrbiosisButton, "ORBIOSIS");
                    SetButtonText(brainGamesSymbiozButton, "Dynasty Legacy - Symbiosis");
                    brainGamesSymbiGridButton.gameObject.name = SymbiGridButtonName;
                    brainGamesOrbiosisButton.gameObject.name = OrbiosisButtonName;
                    brainGamesSymbiozButton.gameObject.name = SymbiozButtonName;
                    ApplyBrainGamesButtonStyle(brainGamesCloseButton);
                    ApplyBrainGamesButtonStyle(brainGamesMahjongButton);
                    ApplyBrainGamesButtonStyle(brainGamesSudokuButton);
                    ApplyBrainGamesButtonStyle(brainGamesSymbiGridButton);
                    ApplyBrainGamesButtonStyle(brainGamesOrbiosisButton);
                    ApplyBrainGamesButtonStyle(brainGamesSymbiozButton);

                    CleanupBrainGamesPanelChildren(existingPanel);

                    return;
                }

                GameObject panel = new GameObject("BrainGamesPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                panel.transform.SetParent(brainGamesParent, false);
                brainGamesPanelRect = panel.GetComponent<RectTransform>();
                Image image = panel.GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;
                brainGamesBackdropButton = ConfigureBrainGamesBackdrop(panel, image);

                TextMeshProUGUI title = CreateRuntimeText(panel.transform, "Title", "Zeka Oyunları", 44f);
                title.gameObject.SetActive(false);
                RectTransform titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -36f);
                titleRect.sizeDelta = new Vector2(-180f, 72f);

                brainGamesCloseButton = CreateRuntimeButton("BrainGamesClose", "X", panel.transform, 32f);
                brainGamesCloseButton.onClick.AddListener(CloseBrainGames);
                brainGamesMahjongButton = CreateRuntimeButton(MahjongButtonName, "Mahjong", panel.transform, 30f);
                brainGamesSudokuButton = CreateRuntimeButton(SudokuButtonName, "SUDOKU", panel.transform, 30f);
                brainGamesSymbiGridButton = CreateRuntimeButton(SymbiGridButtonName, "SYMBIGRID", panel.transform, 30f);
                brainGamesOrbiosisButton = CreateRuntimeButton(OrbiosisButtonName, "ORBIOSIS", panel.transform, 30f);
                brainGamesSymbiozButton = CreateRuntimeButton(SymbiozButtonName, "Dynasty Legacy - Symbiosis", panel.transform, 28f);
            }
        }

        private RectTransform EnsureBrainGamesRuntimeRoot()
        {
            if (brainGamesRuntimeRoot != null)
                return brainGamesRuntimeRoot;

            Scene activeScene = SceneManager.GetActiveScene();
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null || !string.Equals(candidate.name, BrainGamesRuntimeCanvasName, System.StringComparison.Ordinal))
                    continue;

                if (candidate.gameObject.scene.IsValid() && candidate.gameObject.scene != activeScene)
                    continue;

                ConfigureBrainGamesRuntimeCanvas(candidate);
                brainGamesRuntimeRoot = candidate.transform as RectTransform;
                return brainGamesRuntimeRoot;
            }

            GameObject host = new GameObject(BrainGamesRuntimeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (IsUsableScene(activeScene))
                SceneManager.MoveGameObjectToScene(host, activeScene);

            Canvas runtimeCanvas = host.GetComponent<Canvas>();
            ConfigureBrainGamesRuntimeCanvas(runtimeCanvas);
            brainGamesRuntimeRoot = host.transform as RectTransform;
            return brainGamesRuntimeRoot;
        }

        private static void ConfigureBrainGamesRuntimeCanvas(Canvas runtimeCanvas)
        {
            if (runtimeCanvas == null)
                return;

            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.overrideSorting = true;
            runtimeCanvas.sortingOrder = BrainGamesRuntimeSortingOrder;
            runtimeCanvas.pixelPerfect = false;

            RectTransform rect = runtimeCanvas.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            CanvasScaler runtimeScaler = runtimeCanvas.GetComponent<CanvasScaler>();
            if (runtimeScaler == null)
                runtimeScaler = runtimeCanvas.gameObject.AddComponent<CanvasScaler>();
            MainLobbyUiCoordinator.ConfigureOverlayScaler(runtimeScaler);

            GraphicRaycaster raycaster = runtimeCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                runtimeCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static Button FindChildButton(Transform parent, params string[] names)
        {
            if (parent == null || names == null)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                Transform child = parent.Find(names[i]);
                if (child != null)
                    return child.GetComponent<Button>();
            }

            return null;
        }

        private Button ConfigureBrainGamesBackdrop(GameObject panel, Graphic targetGraphic)
        {
            if (panel == null)
                return null;

            Button backdrop = panel.GetComponent<Button>();
            if (backdrop == null)
                backdrop = panel.AddComponent<Button>();

            backdrop.transition = Selectable.Transition.None;
            backdrop.targetGraphic = targetGraphic;
            backdrop.onClick.RemoveAllListeners();
            backdrop.onClick.AddListener(CloseBrainGames);
            backdrop.interactable = true;
            return backdrop;
        }

        private Button CreateRuntimeButton(string objectName, string label, Transform parent, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            CreateRuntimeText(buttonObject.transform, "Label", label, fontSize);
            ApplyBrainGamesButtonStyle(button);
            return button;
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
                label.text = value;
        }

        private static void ApplyBrainGamesButtonStyle(Button button)
        {
            if (button == null)
                return;

            Image image = button.image != null ? button.image : button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = new Color(0.015f, 0.052f, 0.095f, 0.92f);
                image.raycastTarget = true;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.76f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.48f, 0.68f, 0.86f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.38f, 0.42f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                MainLobbyButtonStyle.ApplyFont(label);
                label.enableVertexGradient = false;
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.gameObject.SetActive(true);
            }

            if (IsSudokuLaunchButton(button))
                ApplySudokuButtonSprite(button, image, label);

            if (IsMahjongLaunchButton(button))
                ApplyMahjongButtonSprite(button, image, label);

            if (IsSymbiGridLaunchButton(button))
                ApplySymbiGridLaunchButtonSprite(button, image, label);

            if (IsOrbiosisLaunchButton(button))
            {
                ApplyGeneratedLaunchButtonSprite(button, image, label, GetOrbiosisButtonSprite());
                EnsureOrbiosisDevelopmentBadge(button);
            }

            if (IsSymbiozLaunchButton(button))
                ApplySymbiozLaunchButtonSprite(button, image, label);

            if (IsBrainGamesTabButton(button))
                ApplyBrainGamesTabSprite(button, image);
        }

        private static bool IsBrainGamesTabButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, BrainGamesTabButtonName, System.StringComparison.Ordinal);
        }

        private static bool IsMahjongLaunchButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, MahjongButtonName, System.StringComparison.Ordinal);
        }

        private static bool IsSudokuLaunchButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, SudokuButtonName, System.StringComparison.Ordinal)
                || string.Equals(name, SudokuRuntimeButtonName, System.StringComparison.Ordinal);
        }

        private static bool IsSymbiGridLaunchButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, SymbiGridButtonName, System.StringComparison.Ordinal)
                || string.Equals(name, LegacyBlockBustButtonName, System.StringComparison.Ordinal);
        }

        private static bool IsOrbiosisLaunchButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, OrbiosisButtonName, System.StringComparison.Ordinal);
        }

        private static bool IsSymbiozLaunchButton(Button button)
        {
            string name = button != null ? button.gameObject.name : string.Empty;
            return string.Equals(name, SymbiozButtonName, System.StringComparison.Ordinal);
        }

        private static void ApplyMahjongButtonSprite(Button button, Image image, TMP_Text label)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            Sprite sprite = GetMahjongButtonSprite();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            if (label != null)
                label.gameObject.SetActive(sprite == null);
        }

        private static void ApplySudokuButtonSprite(Button button, Image image, TMP_Text label)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            Sprite sprite = GetSudokuButtonSprite();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            if (label != null)
                label.gameObject.SetActive(false);

            EnsureSudokuDevelopmentBadge(button);
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        private static void EnsureSudokuDevelopmentBadge(Button button)
        {
            RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
            if (buttonRect == null)
                return;

            Transform existing = buttonRect.Find(SudokuDevelopmentBadgeName);
            TextMeshProUGUI text = existing != null ? existing.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (text == null)
            {
                GameObject badgeObject = new GameObject(SudokuDevelopmentBadgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObject.transform.SetParent(buttonRect, false);

                Image badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.color = new Color(0.02f, 0.02f, 0.025f, 0.82f);
                badgeImage.raycastTarget = false;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(badgeObject.transform, false);
                text = labelObject.GetComponent<TextMeshProUGUI>();
                text.text = SudokuDevelopmentBadgeText;
                text.color = new Color(1f, 0.88f, 0.42f, 1f);
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 28f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.raycastTarget = false;
                MainLobbyButtonStyle.ApplyFont(text);
            }

            text.text = SudokuDevelopmentBadgeText;
            text.transform.parent.gameObject.SetActive(true);
            LayoutSudokuDevelopmentBadge(button);
            text.transform.parent.SetAsLastSibling();
        }

        private static void LayoutSudokuDevelopmentBadge(Button button)
        {
            RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
            RectTransform badgeRect = buttonRect != null ? buttonRect.Find(SudokuDevelopmentBadgeName) as RectTransform : null;
            if (badgeRect == null)
                return;

            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 24f);
            badgeRect.sizeDelta = new Vector2(Mathf.Min(260f, buttonRect.rect.width * 0.72f), 42f);
            badgeRect.localScale = Vector3.one;
            badgeRect.localRotation = Quaternion.identity;

            RectTransform labelRect = badgeRect.Find("Label") as RectTransform;
            if (labelRect == null)
                return;

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 2f);
            labelRect.offsetMax = new Vector2(-12f, -2f);
            labelRect.localScale = Vector3.one;
            labelRect.localRotation = Quaternion.identity;
        }

        private static void EnsureOrbiosisDevelopmentBadge(Button button)
        {
            RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
            if (buttonRect == null)
                return;

            Transform existing = buttonRect.Find(OrbiosisDevelopmentBadgeName);
            TextMeshProUGUI text = existing != null ? existing.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (text == null)
            {
                GameObject badgeObject = new GameObject(OrbiosisDevelopmentBadgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
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

            text.text = GameLocalization.Text("main.orbiosis_unavailable.status");
            text.transform.parent.gameObject.SetActive(true);
            LayoutOrbiosisDevelopmentBadge(button);
            text.transform.parent.SetAsLastSibling();
        }

        private static void LayoutOrbiosisDevelopmentBadge(Button button)
        {
            RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
            RectTransform badgeRect = buttonRect != null ? buttonRect.Find(OrbiosisDevelopmentBadgeName) as RectTransform : null;
            if (badgeRect == null)
                return;

            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 8f);
            badgeRect.sizeDelta = new Vector2(Mathf.Min(360f, buttonRect.rect.width * 0.82f), 58f);
            badgeRect.localScale = Vector3.one;
            badgeRect.localRotation = Quaternion.identity;

            RectTransform labelRect = badgeRect.Find("Label") as RectTransform;
            if (labelRect == null)
                return;

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 2f);
            labelRect.offsetMax = new Vector2(-12f, -2f);
            labelRect.localScale = Vector3.one;
            labelRect.localRotation = Quaternion.identity;
        }

        private static void ApplyGeneratedLaunchButtonSprite(Button button, Image image, TMP_Text label, Sprite sprite)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            if (label != null)
                label.gameObject.SetActive(sprite == null);
        }

        private static void ApplySymbiozLaunchButtonSprite(Button button, Image image, TMP_Text label)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            Sprite sprite = GetSymbiozButtonSprite();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = "Dynasty Legacy - Symbiosis";
                label.color = new Color(0.93f, 0.98f, 1f, 1f);
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16f;
                label.fontSizeMax = 30f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.margin = new Vector4(48f, 0f, 48f, 0f);
                MainLobbyButtonStyle.ApplyFont(label);
            }
        }

        private static void ApplySymbiGridLaunchButtonSprite(Button button, Image image, TMP_Text label)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            Sprite sprite = GetSymbiGridButtonSprite();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            if (label != null)
                label.gameObject.SetActive(sprite == null);
        }

        private static void ApplyBrainGamesTabSprite(Button button, Image image)
        {
            if (button == null)
                return;

            if (image == null)
                image = button.GetComponent<Image>();

            Sprite sprite = GetBrainGamesTabSprite();
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Truncate;

                RectTransform labelRect = label.rectTransform;
                if (labelRect != null)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                    labelRect.anchoredPosition = Vector2.zero;
                    labelRect.localScale = Vector3.one;
                }
            }
        }

        private static Sprite GetSudokuButtonSprite()
        {
            if (sudokuButtonSprite == null)
            {
                sudokuButtonSprite = Resources.Load<Sprite>(SudokuButtonSpriteResourcePath);
                if (sudokuButtonSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(SudokuButtonSpriteResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        sudokuButtonSprite = sprites[0];
                }
            }

            return sudokuButtonSprite;
        }

        private static Sprite GetMahjongButtonSprite()
        {
            if (mahjongButtonSprite == null)
            {
                mahjongButtonSprite = Resources.Load<Sprite>(MahjongButtonSpriteResourcePath);
                if (mahjongButtonSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(MahjongButtonSpriteResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        mahjongButtonSprite = sprites[0];
                }
            }

            return mahjongButtonSprite;
        }

        private static Sprite GetSymbiGridButtonSprite()
        {
            if (symbiGridButtonSprite == null)
                symbiGridButtonSprite = LoadFullTextureSprite(SymbiGridButtonSpriteResourcePath);

            return symbiGridButtonSprite;
        }

        private static Sprite GetOrbiosisButtonSprite()
        {
            if (orbiosisButtonSprite == null)
                orbiosisButtonSprite = LoadFullTextureSprite(OrbiosisButtonSpriteResourcePath);

            return orbiosisButtonSprite;
        }

        private static Sprite GetSymbiozButtonSprite()
        {
            if (symbiozButtonSprite == null)
                symbiozButtonSprite = LoadFullTextureSprite(SymbiozButtonSpriteResourcePath);

            return symbiozButtonSprite;
        }

        private static Sprite GetSymbiozLoginPanelSprite()
        {
            if (symbiozLoginPanelSprite == null)
                symbiozLoginPanelSprite = LoadFullTextureSprite(SymbiozLoginPanelSpriteResourcePath);

            return symbiozLoginPanelSprite;
        }

        private static Sprite LoadFullTextureSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private static Sprite GetBrainGamesTabSprite()
        {
            if (brainGamesTabSprite == null)
            {
                brainGamesTabSprite = Resources.Load<Sprite>(BrainGamesTabSpriteResourcePath);
                if (brainGamesTabSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(BrainGamesTabSpriteResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        brainGamesTabSprite = sprites[0];
                }

                if (brainGamesTabSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(BrainGamesTabSpriteResourcePath);
                    if (texture != null)
                        brainGamesTabSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return brainGamesTabSprite;
        }

        private static Sprite GetOrbiosisHangarBackgroundSprite()
        {
            if (orbiosisHangarBackgroundSprite == null)
            {
                orbiosisHangarBackgroundSprite = Resources.Load<Sprite>(OrbiosisHangarBackgroundSpriteResourcePath);
                if (orbiosisHangarBackgroundSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(OrbiosisHangarBackgroundSpriteResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        orbiosisHangarBackgroundSprite = sprites[0];
                }

                if (orbiosisHangarBackgroundSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(OrbiosisHangarBackgroundSpriteResourcePath);
                    if (texture != null)
                        orbiosisHangarBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return orbiosisHangarBackgroundSprite;
        }

        private static Sprite GetOrbiosisHangarDoorSprite()
        {
            if (orbiosisHangarDoorSprite == null)
            {
                orbiosisHangarDoorSprite = Resources.Load<Sprite>(OrbiosisHangarDoorSpriteResourcePath);
                if (orbiosisHangarDoorSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(OrbiosisHangarDoorSpriteResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        orbiosisHangarDoorSprite = sprites[0];
                }

                if (orbiosisHangarDoorSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(OrbiosisHangarDoorSpriteResourcePath);
                    if (texture != null)
                        orbiosisHangarDoorSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return orbiosisHangarDoorSprite;
        }

        private TextMeshProUGUI CreateRuntimeText(Transform parent, string objectName, string value, float fontSize)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(16f, fontSize * 0.52f);
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(label);

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 4f);
            rect.offsetMax = new Vector2(-18f, -4f);
            return label;
        }

        private void EnsureOrbiosisHangarMenu()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvas == null || orbiosisHangarRoot != null)
                return;

            GameObject root = new GameObject("OrbiosisHangarMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            orbiosisHangarRoot = root.GetComponent<RectTransform>();
            orbiosisHangarGroup = root.GetComponent<CanvasGroup>();

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0.001f, 0.006f, 0.014f, 1f);
            blocker.raycastTarget = true;

            GameObject backgroundObject = new GameObject("HangarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(root.transform, false);
            orbiosisHangarBackgroundRect = backgroundObject.GetComponent<RectTransform>();
            Image background = backgroundObject.GetComponent<Image>();
            background.sprite = GetOrbiosisHangarBackgroundSprite();
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = Color.white;
            background.raycastTarget = false;

            GameObject shadeObject = new GameObject("HangarShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadeObject.transform.SetParent(root.transform, false);
            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            Image shade = shadeObject.GetComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.18f);
            shade.raycastTarget = false;

            orbiosisHangarLeftDoor = CreateHangarDoor(root.transform, "LeftDoor", false);
            orbiosisHangarRightDoor = CreateHangarDoor(root.transform, "RightDoor", true);

            GameObject logoObject = new GameObject("OrbiosisLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(root.transform, false);
            orbiosisHangarLogoRect = logoObject.GetComponent<RectTransform>();
            Image logo = logoObject.GetComponent<Image>();
            logo.sprite = GetOrbiosisButtonSprite();
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            logo.color = Color.white;
            logo.raycastTarget = false;

            orbiosisHangarCloseButton = CreateRuntimeButton("OrbiosisHangarClose", "X", root.transform, 28f);
            orbiosisHangarCloseButton.onClick.AddListener(CloseOrbiosisHangarMenu);
            orbiosisHangarStartButton = CreateRuntimeButton("OrbiosisHangarStart", "START", root.transform, 32f);
            orbiosisHangarStartButton.onClick.AddListener(LaunchOrbiosisFromHangar);

            root.SetActive(false);
        }

        private static RectTransform CreateHangarDoor(Transform parent, string objectName, bool mirrored)
        {
            GameObject doorObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            doorObject.transform.SetParent(parent, false);
            RectTransform rect = doorObject.GetComponent<RectTransform>();
            Image image = doorObject.GetComponent<Image>();
            image.sprite = GetOrbiosisHangarDoorSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            if (mirrored)
                rect.localScale = new Vector3(-1f, 1f, 1f);
            return rect;
        }

        private void ToggleBrainGames()
        {
            brainGamesOpen = !brainGamesOpen;
            ApplyLayout();
        }

        private void CloseBrainGames()
        {
            brainGamesOpen = false;
            ApplyLayout();
        }

        private void LayoutBrainGamesTab(bool portrait)
        {
            bool showGames = true;
            UnityEngine.Events.UnityAction mahjongAction = sceneNavigator != null ? sceneNavigator.LoadLobbyMahjong : (UnityEngine.Events.UnityAction)null;
            UnityEngine.Events.UnityAction symbiGridAction = sceneNavigator != null ? sceneNavigator.LoadSymbiGrid : (UnityEngine.Events.UnityAction)null;
            UnityEngine.Events.UnityAction orbiosisAction = IsOrbiosisAccessEnabled
                ? (sceneNavigator != null ? sceneNavigator.LoadOrbiosis : (UnityEngine.Events.UnityAction)null)
                : OpenOrbiosisDevelopmentNotice;
            UnityEngine.Events.UnityAction symbiozAction = sceneNavigator != null ? OpenSymbiozLoginWindow : (UnityEngine.Events.UnityAction)null;

            brainGamesOpen = false;
            ConfigureProxyButton(brainGamesMahjongButton, mahjongAction);
            ConfigureProxyButton(brainGamesSudokuButton, null);
            ConfigureProxyButton(brainGamesSymbiGridButton, symbiGridAction);
            ConfigureProxyButton(brainGamesOrbiosisButton, orbiosisAction);
            ConfigureProxyButton(brainGamesSymbiozButton, symbiozAction);
            ConfigureProxyButton(brainGamesOkeyButton, null);
            SetButtonActive(brainGamesMahjongButton, showGames);
            SetButtonActive(brainGamesSudokuButton, false);
            SetButtonActive(brainGamesSymbiGridButton, showGames);
            SetButtonActive(brainGamesOrbiosisButton, showGames);
            SetButtonActive(brainGamesSymbiozButton, showGames);
            SetButtonActive(brainGamesOkeyButton, false);

            LayoutBrainGamesChrome(portrait);
            SuppressMainChrome(IsOrbiosisHangarVisible());

            if (portrait)
            {
                Vector2 buttonSize = new Vector2(430f, 168f);
                LayoutButton(brainGamesMahjongButton, new Vector2(0f, 278f), buttonSize, Vector3.one, true, 23f);
                LayoutButton(brainGamesSymbiGridButton, new Vector2(0f, 96f), buttonSize, Vector3.one, true, 23f);
                LayoutButton(brainGamesOrbiosisButton, new Vector2(0f, -86f), buttonSize, Vector3.one, true, 23f);
                LayoutButton(brainGamesSymbiozButton, new Vector2(0f, -270f), new Vector2(600f, 142f), Vector3.one, false, 24f);
            }
            else
            {
                Vector2 logoButtonSize = new Vector2(400f, 150f);
                LayoutButton(brainGamesMahjongButton, new Vector2(-230f, 108f), logoButtonSize, Vector3.one, true, 24f);
                LayoutButton(brainGamesSymbiGridButton, new Vector2(230f, 108f), logoButtonSize, Vector3.one, true, 24f);
                LayoutButton(brainGamesOrbiosisButton, new Vector2(-230f, -72f), logoButtonSize, Vector3.one, true, 24f);
                LayoutButton(brainGamesSymbiozButton, new Vector2(230f, -72f), new Vector2(560f, 136f), Vector3.one, false, 25f);
            }

            RaiseButton(brainGamesMahjongButton);
            RaiseButton(brainGamesSymbiGridButton);
            RaiseButton(brainGamesOrbiosisButton);
            RaiseButton(brainGamesSymbiozButton);
            RaiseButton(brainGamesCloseButton);
        }

        private void EnsureSymbiozLoginWindow()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvas == null || symbiozLoginRoot != null)
                return;

            GameObject root = new GameObject("SymbiozLoginWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            symbiozLoginRoot = root.GetComponent<RectTransform>();
            Image blocker = root.GetComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;

            GameObject card = new GameObject("LoginCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(root.transform, false);
            symbiozLoginCard = card.GetComponent<RectTransform>();
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = GetSymbiozLoginPanelSprite();
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = false;
            cardImage.color = Color.white;
            cardImage.raycastTarget = true;

            TextMeshProUGUI title = CreateRuntimeText(card.transform, "Title", "Dynasty Legacy", 38f);
            title.color = new Color(0.92f, 0.98f, 1f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI subtitle = CreateRuntimeText(card.transform, "Subtitle", "Symbiosis flagship access", 22f);
            subtitle.color = new Color(0.58f, 0.9f, 1f, 0.88f);
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;

            symbiozLoginInput = CreateSymbiozSecretInput(card.transform, "LoginInput", "Логин", false);
            symbiozPasswordInput = CreateSymbiozSecretInput(card.transform, "PasswordInput", "Пароль", true);

            symbiozLoginErrorText = CreateRuntimeText(card.transform, "ErrorText", string.Empty, 22f);
            symbiozLoginErrorText.color = new Color(1f, 0.38f, 0.34f, 1f);
            symbiozLoginErrorText.textWrappingMode = TextWrappingModes.Normal;

            symbiozLoginSubmitButton = CreateRuntimeButton("SubmitButton", "Войти в игру", card.transform, 26f);
            symbiozLoginSubmitButton.onClick.AddListener(SubmitSymbiozLogin);
            symbiozLoginCancelButton = CreateRuntimeButton("CancelButton", "X", card.transform, 26f);
            symbiozLoginCancelButton.onClick.AddListener(CloseSymbiozLoginWindow);

            root.SetActive(false);
        }

        private TMP_InputField CreateSymbiozSecretInput(Transform parent, string objectName, string placeholder, bool secure)
        {
            GameObject inputObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField), typeof(MobileTmpInputKeyboardBridge));
            inputObject.transform.SetParent(parent, false);

            Image background = inputObject.GetComponent<Image>();
            background.color = new Color(0f, 0.012f, 0.028f, 0.98f);
            background.raycastTarget = true;
            Outline outline = inputObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.72f, 1f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(inputObject.transform, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.fontSize = 24f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 24f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(text);

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 5f);
            textRect.offsetMax = new Vector2(-24f, -5f);

            GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(inputObject.transform, false);
            TextMeshProUGUI placeholderText = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.color = new Color(1f, 1f, 1f, 0.46f);
            placeholderText.fontSize = 23f;
            placeholderText.enableAutoSizing = true;
            placeholderText.fontSizeMin = 15f;
            placeholderText.fontSizeMax = 23f;
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.textWrappingMode = TextWrappingModes.NoWrap;
            placeholderText.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(placeholderText);

            RectTransform placeholderRect = placeholderText.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(24f, 5f);
            placeholderRect.offsetMax = new Vector2(-24f, -5f);

            TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
            input.textViewport = inputObject.transform as RectTransform;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.contentType = secure ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.inputType = secure ? TMP_InputField.InputType.Password : TMP_InputField.InputType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.asteriskChar = '*';
            input.characterLimit = 32;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.18f, 0.55f, 1f, 0.36f);
            input.ForceLabelUpdate();
            return input;
        }

        private void OpenSymbiozLoginWindow()
        {
            EnsureSymbiozLoginWindow();
            if (symbiozLoginRoot == null)
                return;

            if (!MainHubStateController.CanOpenMainWindow("SymbiozLogin"))
                return;

            MainLobbyUiCoordinator.SetRightStackSuppressed(true);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(true);

            if (symbiozLoginInput != null)
                symbiozLoginInput.SetTextWithoutNotify(string.Empty);
            if (symbiozPasswordInput != null)
                symbiozPasswordInput.SetTextWithoutNotify(string.Empty);
            if (symbiozLoginErrorText != null)
                symbiozLoginErrorText.text = string.Empty;

            symbiozLoginRoot.gameObject.SetActive(true);
            LayoutSymbiozLoginWindow(false);
            if (symbiozLoginInput != null)
                symbiozLoginInput.ActivateInputField();
        }

        public void OpenSymbiozLoginFromMainLaunch()
        {
            OpenSymbiozLoginWindow();
        }

        private void CloseSymbiozLoginWindow()
        {
            if (symbiozLoginInput != null)
                symbiozLoginInput.DeactivateInputField();
            if (symbiozPasswordInput != null)
                symbiozPasswordInput.DeactivateInputField();
            if (symbiozLoginRoot != null)
                symbiozLoginRoot.gameObject.SetActive(false);
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void SubmitSymbiozLogin()
        {
            string login = symbiozLoginInput != null ? symbiozLoginInput.text : string.Empty;
            string password = symbiozPasswordInput != null ? symbiozPasswordInput.text : string.Empty;
            if (!string.Equals(login, SymbiozLoginValue, System.StringComparison.Ordinal) ||
                !string.Equals(password, SymbiozPasswordValue, System.StringComparison.Ordinal))
            {
                if (symbiozLoginErrorText != null)
                    symbiozLoginErrorText.text = "Неверный логин или пароль";
                if (symbiozPasswordInput != null)
                    symbiozPasswordInput.SetTextWithoutNotify(string.Empty);
                return;
            }

            CloseSymbiozLoginWindow();
            if (sceneNavigator == null)
                sceneNavigator = FindAnyObjectByType<SceneNavigator>();
            if (sceneNavigator != null)
                sceneNavigator.LoadSymbiozFlagship();
        }

        private void LayoutSymbiozLoginWindow(bool portrait)
        {
            EnsureSymbiozLoginWindow();
            if (symbiozLoginRoot == null || !symbiozLoginRoot.gameObject.activeSelf)
                return;

            symbiozLoginRoot.anchorMin = Vector2.zero;
            symbiozLoginRoot.anchorMax = Vector2.one;
            symbiozLoginRoot.offsetMin = Vector2.zero;
            symbiozLoginRoot.offsetMax = Vector2.zero;
            symbiozLoginRoot.anchoredPosition = Vector2.zero;
            symbiozLoginRoot.localScale = Vector3.one;
            symbiozLoginRoot.SetAsLastSibling();

            Vector2 cardSize = portrait ? new Vector2(700f, 600f) : new Vector2(860f, 560f);
            float innerWidth = portrait ? 460f : 530f;
            if (symbiozLoginCard != null)
            {
                symbiozLoginCard.anchorMin = new Vector2(0.5f, 0.5f);
                symbiozLoginCard.anchorMax = new Vector2(0.5f, 0.5f);
                symbiozLoginCard.pivot = new Vector2(0.5f, 0.5f);
                symbiozLoginCard.anchoredPosition = Vector2.zero;
                symbiozLoginCard.sizeDelta = cardSize;
                symbiozLoginCard.localScale = Vector3.one;
            }

            LayoutChildRect(symbiozLoginCard, "Title", new Vector2(0f, 136f), new Vector2(innerWidth + 80f, 54f));
            LayoutChildRect(symbiozLoginCard, "Subtitle", new Vector2(0f, 100f), new Vector2(innerWidth, 30f));
            LayoutInput(symbiozLoginInput, new Vector2(0f, 36f), new Vector2(innerWidth, 58f));
            LayoutInput(symbiozPasswordInput, new Vector2(0f, -34f), new Vector2(innerWidth, 58f));
            LayoutChildRect(symbiozLoginCard, "ErrorText", new Vector2(0f, -88f), new Vector2(innerWidth, 36f));
            LayoutButton(symbiozLoginSubmitButton, new Vector2(0f, -146f), new Vector2(300f, 64f), Vector3.one, false, 23f);
            LayoutButton(symbiozLoginCancelButton, new Vector2(cardSize.x * 0.5f - 78f, cardSize.y * 0.5f - 82f), new Vector2(52f, 52f), Vector3.one, true, 22f);
            RaiseButton(symbiozLoginCancelButton);
        }

        private static void LayoutInput(TMP_InputField input, Vector2 position, Vector2 size)
        {
            RectTransform rect = input != null ? input.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void LayoutChildRect(Transform parent, string childName, Vector2 position, Vector2 size)
        {
            RectTransform rect = parent != null ? parent.Find(childName) as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private bool IsOrbiosisHangarVisible()
        {
            return orbiosisHangarOpen || (orbiosisHangarRoot != null && orbiosisHangarRoot.gameObject.activeSelf);
        }

        private void OpenOrbiosisHangarMenu()
        {
            if (sceneNavigator == null)
                sceneNavigator = FindAnyObjectByType<SceneNavigator>();

            EnsureOrbiosisHangarMenu();
            if (orbiosisHangarRoot == null)
                return;

            brainGamesOpen = false;
            orbiosisHangarOpen = true;
            SuppressMainChrome(true);
            LayoutBrainGamesChrome(false);
            LayoutOrbiosisHangarMenu();

            if (orbiosisHangarRoutine != null)
                StopCoroutine(orbiosisHangarRoutine);

            orbiosisHangarRoutine = StartCoroutine(AnimateOrbiosisHangarDoors());
        }

        public void OpenOrbiosisHangarFromMainLaunch()
        {
            if (!IsOrbiosisAccessEnabled)
            {
                OpenOrbiosisDevelopmentNotice();
                return;
            }

            OpenOrbiosisHangarMenu();
        }

        public void OpenOrbiosisDevelopmentFromMainLaunch()
        {
            OpenOrbiosisDevelopmentNotice();
        }

        private void CloseOrbiosisHangarMenu()
        {
            if (orbiosisHangarRoutine != null)
            {
                StopCoroutine(orbiosisHangarRoutine);
                orbiosisHangarRoutine = null;
            }

            orbiosisHangarOpen = false;
            if (orbiosisHangarRoot != null)
                orbiosisHangarRoot.gameObject.SetActive(false);
            SuppressMainChrome(false);
            ApplyLayout();
        }

        private void LaunchOrbiosisFromHangar()
        {
            if (!IsOrbiosisAccessEnabled)
            {
                CloseOrbiosisHangarMenu();
                OpenOrbiosisDevelopmentNotice();
                return;
            }

            if (sceneNavigator == null)
                sceneNavigator = FindAnyObjectByType<SceneNavigator>();

            if (sceneNavigator != null)
                sceneNavigator.LoadOrbiosis();
        }

        private void EnsureOrbiosisDevelopmentNotice()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvas == null || orbiosisDevelopmentRoot != null)
                return;

            GameObject root = new GameObject(OrbiosisDevelopmentNoticeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            orbiosisDevelopmentRoot = root.GetComponent<RectTransform>();

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0.002f, 0.009f, 0.02f, 0.96f);
            blocker.raycastTarget = true;

            GameObject cardObject = new GameObject("NoticeCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            cardObject.transform.SetParent(root.transform, false);
            orbiosisDevelopmentCard = cardObject.GetComponent<RectTransform>();

            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = new Color(0.012f, 0.055f, 0.105f, 0.98f);
            cardImage.raycastTarget = true;

            Outline cardOutline = cardObject.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.24f, 0.82f, 1f, 0.96f);
            cardOutline.effectDistance = new Vector2(3f, -3f);

            GameObject topAccentObject = new GameObject("TopAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topAccentObject.transform.SetParent(cardObject.transform, false);
            Image topAccent = topAccentObject.GetComponent<Image>();
            topAccent.color = new Color(1f, 0.72f, 0.18f, 1f);
            topAccent.raycastTarget = false;

            GameObject bottomAccentObject = new GameObject("BottomAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bottomAccentObject.transform.SetParent(cardObject.transform, false);
            Image bottomAccent = bottomAccentObject.GetComponent<Image>();
            bottomAccent.color = new Color(0.18f, 0.76f, 1f, 0.86f);
            bottomAccent.raycastTarget = false;

            GameObject logoObject = new GameObject("OrbiosisLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(cardObject.transform, false);
            orbiosisDevelopmentLogo = logoObject.GetComponent<RectTransform>();
            Image logo = logoObject.GetComponent<Image>();
            logo.sprite = GetOrbiosisButtonSprite();
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            logo.color = Color.white;
            logo.raycastTarget = false;

            mainDevelopmentTitleText = CreateRuntimeText(cardObject.transform, "DevelopmentTitle", string.Empty, 52f);
            mainDevelopmentTitleText.color = new Color(0.82f, 0.94f, 1f, 1f);
            mainDevelopmentTitleText.fontStyle = FontStyles.Bold;
            mainDevelopmentTitleText.textWrappingMode = TextWrappingModes.Normal;
            mainDevelopmentTitleText.gameObject.SetActive(false);

            orbiosisDevelopmentStatusText = CreateRuntimeText(cardObject.transform, "Status", GameLocalization.Text("main.orbiosis_unavailable.status"), 46f);
            orbiosisDevelopmentStatusText.color = new Color(1f, 0.84f, 0.34f, 1f);
            orbiosisDevelopmentStatusText.fontStyle = FontStyles.Bold;
            orbiosisDevelopmentStatusText.textWrappingMode = TextWrappingModes.NoWrap;

            orbiosisDevelopmentBodyText = CreateRuntimeText(cardObject.transform, "Body", GameLocalization.Text("main.orbiosis_unavailable.body"), 32f);
            orbiosisDevelopmentBodyText.color = new Color(0.86f, 0.94f, 1f, 1f);
            orbiosisDevelopmentBodyText.fontStyle = FontStyles.Normal;
            orbiosisDevelopmentBodyText.textWrappingMode = TextWrappingModes.Normal;
            orbiosisDevelopmentBodyText.overflowMode = TextOverflowModes.Ellipsis;

            orbiosisDevelopmentBackButton = CreateRuntimeButton("BackButton", GameLocalization.Text("common.back").ToUpperInvariant(), cardObject.transform, 31f);
            orbiosisDevelopmentBackButton.onClick.AddListener(CloseOrbiosisDevelopmentNotice);
            Image backImage = orbiosisDevelopmentBackButton.GetComponent<Image>();
            if (backImage != null)
                backImage.color = new Color(0.025f, 0.16f, 0.26f, 0.98f);

            root.SetActive(false);
        }

        private void OpenOrbiosisDevelopmentNotice()
        {
            OpenMainDevelopmentNotice(null, "main.orbiosis_unavailable.status", "main.orbiosis_unavailable.body", true);
        }

        private void OpenMainDevelopmentNotice(string titleKey, string statusKey, string bodyKey, bool showOrbiosisLogo)
        {
            EnsureOrbiosisDevelopmentNotice();
            if (orbiosisDevelopmentRoot == null)
                return;

            brainGamesOpen = false;
            orbiosisHangarOpen = false;
            if (orbiosisHangarRoot != null)
                orbiosisHangarRoot.gameObject.SetActive(false);

            mainDevelopmentShowsLogo = showOrbiosisLogo;
            if (orbiosisDevelopmentLogo != null)
                orbiosisDevelopmentLogo.gameObject.SetActive(showOrbiosisLogo);
            if (mainDevelopmentTitleText != null)
            {
                mainDevelopmentTitleText.gameObject.SetActive(!showOrbiosisLogo);
                mainDevelopmentTitleText.text = string.IsNullOrWhiteSpace(titleKey)
                    ? string.Empty
                    : GameLocalization.Text(titleKey).ToUpperInvariant();
            }
            if (orbiosisDevelopmentStatusText != null)
                orbiosisDevelopmentStatusText.text = GameLocalization.Text(statusKey);
            if (orbiosisDevelopmentBodyText != null)
                orbiosisDevelopmentBodyText.text = GameLocalization.Text(bodyKey);
            SetButtonText(orbiosisDevelopmentBackButton, GameLocalization.Text("common.back").ToUpperInvariant());

            orbiosisDevelopmentRoot.gameObject.SetActive(true);
            MainGameLaunchBootstrap.HideForMainWindowNow();
            SuppressMainChrome(true);
            LayoutOrbiosisDevelopmentNotice(false);
        }

        private void CloseOrbiosisDevelopmentNotice()
        {
            if (orbiosisDevelopmentRoot != null)
                orbiosisDevelopmentRoot.gameObject.SetActive(false);

            SuppressMainChrome(false);
            MainHubStateController.NotifyMainWindowClosed();
            MainGameLaunchBootstrap.RefreshVisibilityNow();
            ApplyLayout();
        }

        private void LayoutOrbiosisDevelopmentNotice(bool portrait)
        {
            EnsureOrbiosisDevelopmentNotice();
            if (orbiosisDevelopmentRoot == null || !orbiosisDevelopmentRoot.gameObject.activeSelf)
                return;

            orbiosisDevelopmentRoot.anchorMin = Vector2.zero;
            orbiosisDevelopmentRoot.anchorMax = Vector2.one;
            orbiosisDevelopmentRoot.pivot = new Vector2(0.5f, 0.5f);
            orbiosisDevelopmentRoot.offsetMin = Vector2.zero;
            orbiosisDevelopmentRoot.offsetMax = Vector2.zero;
            orbiosisDevelopmentRoot.anchoredPosition = Vector2.zero;
            orbiosisDevelopmentRoot.localScale = Vector3.one;
            orbiosisDevelopmentRoot.SetAsLastSibling();

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            Vector2 viewportSize = canvasRect != null ? canvasRect.rect.size : MainLobbyUiCoordinator.ResolveScreenSize();
            if (viewportSize.x <= 1f || viewportSize.y <= 1f)
                viewportSize = new Vector2(1920f, 1080f);

            Vector2 cardSize = portrait
                ? new Vector2(Mathf.Min(860f, viewportSize.x - 60f), Mathf.Min(880f, viewportSize.y - 70f))
                : new Vector2(Mathf.Min(1500f, viewportSize.x - 120f), Mathf.Min(760f, viewportSize.y - 100f));
            orbiosisDevelopmentCard.anchorMin = new Vector2(0.5f, 0.5f);
            orbiosisDevelopmentCard.anchorMax = new Vector2(0.5f, 0.5f);
            orbiosisDevelopmentCard.pivot = new Vector2(0.5f, 0.5f);
            orbiosisDevelopmentCard.anchoredPosition = Vector2.zero;
            orbiosisDevelopmentCard.sizeDelta = cardSize;
            orbiosisDevelopmentCard.localScale = Vector3.one;

            RectTransform topAccent = orbiosisDevelopmentCard.Find("TopAccent") as RectTransform;
            if (topAccent != null)
            {
                topAccent.anchorMin = new Vector2(0f, 1f);
                topAccent.anchorMax = new Vector2(1f, 1f);
                topAccent.pivot = new Vector2(0.5f, 1f);
                topAccent.anchoredPosition = Vector2.zero;
                topAccent.sizeDelta = new Vector2(0f, 7f);
            }

            RectTransform bottomAccent = orbiosisDevelopmentCard.Find("BottomAccent") as RectTransform;
            if (bottomAccent != null)
            {
                bottomAccent.anchorMin = new Vector2(0f, 0f);
                bottomAccent.anchorMax = new Vector2(1f, 0f);
                bottomAccent.pivot = new Vector2(0.5f, 0f);
                bottomAccent.anchoredPosition = Vector2.zero;
                bottomAccent.sizeDelta = new Vector2(0f, 4f);
            }

            if (orbiosisDevelopmentLogo != null)
            {
                orbiosisDevelopmentLogo.gameObject.SetActive(mainDevelopmentShowsLogo);
                orbiosisDevelopmentLogo.anchorMin = new Vector2(0.5f, 0.5f);
                orbiosisDevelopmentLogo.anchorMax = new Vector2(0.5f, 0.5f);
                orbiosisDevelopmentLogo.pivot = new Vector2(0.5f, 0.5f);
                orbiosisDevelopmentLogo.anchoredPosition = portrait ? new Vector2(0f, 270f) : new Vector2(0f, 230f);
                orbiosisDevelopmentLogo.sizeDelta = portrait ? new Vector2(520f, 170f) : new Vector2(580f, 190f);
                orbiosisDevelopmentLogo.localScale = Vector3.one;
            }

            if (mainDevelopmentTitleText != null)
            {
                mainDevelopmentTitleText.gameObject.SetActive(!mainDevelopmentShowsLogo);
                LayoutChildRect(
                    orbiosisDevelopmentCard,
                    "DevelopmentTitle",
                    portrait ? new Vector2(0f, 270f) : new Vector2(0f, 230f),
                    new Vector2(cardSize.x - 180f, portrait ? 150f : 120f));
            }

            LayoutChildRect(orbiosisDevelopmentCard, "Status", portrait ? new Vector2(0f, 135f) : new Vector2(0f, 100f), new Vector2(cardSize.x - 140f, 78f));
            LayoutChildRect(orbiosisDevelopmentCard, "Body", portrait ? new Vector2(0f, -25f) : new Vector2(0f, -40f), new Vector2(cardSize.x - 230f, portrait ? 220f : 190f));
            LayoutButton(orbiosisDevelopmentBackButton, portrait ? new Vector2(0f, -315f) : new Vector2(0f, -275f), new Vector2(360f, 88f), Vector3.one, false, 31f);
            RaiseButton(orbiosisDevelopmentBackButton);
        }

        private IEnumerator AnimateOrbiosisHangarDoors()
        {
            SetHangarActionButtonsVisible(false);
            SetHangarDoorPose(0f);
            yield return AnimateHangarDoorPose(0f, 1f, OrbiosisHangarDoorCloseSeconds);
            yield return new WaitForSecondsRealtime(OrbiosisHangarDoorHoldSeconds);
            yield return AnimateHangarDoorPose(1f, 0f, OrbiosisHangarDoorOpenSeconds);
            SetHangarActionButtonsVisible(true);
            orbiosisHangarRoutine = null;
        }

        private IEnumerator AnimateHangarDoorPose(float from, float to, float seconds)
        {
            float duration = Mathf.Max(0.01f, seconds);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                t = t * t * (3f - 2f * t);
                SetHangarDoorPose(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetHangarDoorPose(to);
        }

        private void LayoutOrbiosisHangarMenu()
        {
            EnsureOrbiosisHangarMenu();
            if (orbiosisHangarRoot == null)
                return;

            orbiosisHangarRoot.gameObject.SetActive(orbiosisHangarOpen);
            if (!orbiosisHangarOpen)
                return;

            orbiosisHangarRoot.anchorMin = Vector2.zero;
            orbiosisHangarRoot.anchorMax = Vector2.one;
            orbiosisHangarRoot.pivot = new Vector2(0.5f, 0.5f);
            orbiosisHangarRoot.offsetMin = Vector2.zero;
            orbiosisHangarRoot.offsetMax = Vector2.zero;
            orbiosisHangarRoot.anchoredPosition = Vector2.zero;
            orbiosisHangarRoot.localScale = Vector3.one;
            orbiosisHangarRoot.SetAsLastSibling();

            if (orbiosisHangarGroup != null)
            {
                orbiosisHangarGroup.alpha = 1f;
                orbiosisHangarGroup.interactable = true;
                orbiosisHangarGroup.blocksRaycasts = true;
            }

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            Vector2 size = canvasRect != null ? canvasRect.rect.size : MainLobbyUiCoordinator.ResolveScreenSize();
            if (size.x <= 1f || size.y <= 1f)
                size = new Vector2(1920f, 1080f);

            if (orbiosisHangarBackgroundRect != null)
            {
                orbiosisHangarBackgroundRect.anchorMin = Vector2.zero;
                orbiosisHangarBackgroundRect.anchorMax = Vector2.one;
                orbiosisHangarBackgroundRect.offsetMin = Vector2.zero;
                orbiosisHangarBackgroundRect.offsetMax = Vector2.zero;
                orbiosisHangarBackgroundRect.anchoredPosition = Vector2.zero;
                orbiosisHangarBackgroundRect.localScale = Vector3.one;
            }

            float doorWidth = Mathf.Max(size.x * 0.58f, 420f);
            float doorHeight = Mathf.Max(size.y * 1.16f, 820f);
            ConfigureHangarDoorRect(orbiosisHangarLeftDoor, doorWidth, doorHeight);
            ConfigureHangarDoorRect(orbiosisHangarRightDoor, doorWidth, doorHeight);
            SetHangarDoorPose(CurrentHangarDoorClosedFraction());

            if (orbiosisHangarLogoRect != null)
            {
                bool narrow = size.x < size.y;
                orbiosisHangarLogoRect.anchorMin = new Vector2(0.5f, 0.5f);
                orbiosisHangarLogoRect.anchorMax = new Vector2(0.5f, 0.5f);
                orbiosisHangarLogoRect.pivot = new Vector2(0.5f, 0.5f);
                orbiosisHangarLogoRect.sizeDelta = narrow ? new Vector2(520f, 208f) : new Vector2(760f, 304f);
                orbiosisHangarLogoRect.anchoredPosition = narrow ? new Vector2(0f, 230f) : new Vector2(0f, 245f);
                orbiosisHangarLogoRect.localScale = Vector3.one;
                orbiosisHangarLogoRect.gameObject.SetActive(true);
                orbiosisHangarLogoRect.SetAsLastSibling();
            }

            LayoutButton(orbiosisHangarCloseButton, new Vector2(-34f, -30f), new Vector2(74f, 74f), Vector3.one, true, 26f);
            LayoutButton(orbiosisHangarStartButton, new Vector2(0f, 58f), new Vector2(270f, 82f), Vector3.one, false, 30f);
            RaiseButton(orbiosisHangarCloseButton);
            RaiseButton(orbiosisHangarStartButton);
        }

        private static void ConfigureHangarDoorRect(RectTransform rect, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private float CurrentHangarDoorClosedFraction()
        {
            if (orbiosisHangarLeftDoor == null)
                return 0f;

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            Vector2 size = canvasRect != null ? canvasRect.rect.size : MainLobbyUiCoordinator.ResolveScreenSize();
            float doorWidth = Mathf.Max(size.x * 0.58f, 420f);
            float closedX = -doorWidth * 0.5f;
            float openX = -size.x * 0.5f - doorWidth * 0.58f;
            if (Mathf.Abs(closedX - openX) < 0.001f)
                return 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(openX, closedX, orbiosisHangarLeftDoor.anchoredPosition.x));
        }

        private void SetHangarDoorPose(float closedFraction)
        {
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            Vector2 size = canvasRect != null ? canvasRect.rect.size : MainLobbyUiCoordinator.ResolveScreenSize();
            if (size.x <= 1f || size.y <= 1f)
                size = new Vector2(1920f, 1080f);

            float doorWidth = Mathf.Max(size.x * 0.58f, 420f);
            float leftClosedX = -doorWidth * 0.5f;
            float rightClosedX = doorWidth * 0.5f;
            float leftOpenX = -size.x * 0.5f - doorWidth * 0.58f;
            float rightOpenX = size.x * 0.5f + doorWidth * 0.58f;
            float t = Mathf.Clamp01(closedFraction);

            if (orbiosisHangarLeftDoor != null)
                orbiosisHangarLeftDoor.anchoredPosition = new Vector2(Mathf.Lerp(leftOpenX, leftClosedX, t), 0f);
            if (orbiosisHangarRightDoor != null)
                orbiosisHangarRightDoor.anchoredPosition = new Vector2(Mathf.Lerp(rightOpenX, rightClosedX, t), 0f);
        }

        private void SetHangarActionButtonsVisible(bool visible)
        {
            SetButtonActive(orbiosisHangarCloseButton, visible);
            SetButtonActive(orbiosisHangarStartButton, visible);
        }

        private void LayoutBrainGamesChrome(bool portrait)
        {
            if (brainGamesTabButton != null)
                brainGamesTabButton.gameObject.SetActive(false);

            if (brainGamesPanelRect != null)
            {
                brainGamesPanelRect.gameObject.SetActive(true);
                brainGamesPanelRect.anchorMin = Vector2.zero;
                brainGamesPanelRect.anchorMax = Vector2.one;
                brainGamesPanelRect.pivot = new Vector2(0.5f, 0.5f);
                brainGamesPanelRect.anchoredPosition = Vector2.zero;
                brainGamesPanelRect.sizeDelta = Vector2.zero;
                brainGamesPanelRect.offsetMin = Vector2.zero;
                brainGamesPanelRect.offsetMax = Vector2.zero;
                brainGamesPanelRect.localScale = Vector3.one;
                brainGamesPanelRect.SetAsLastSibling();

                Image panelImage = brainGamesPanelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.sprite = null;
                    panelImage.color = Color.clear;
                    panelImage.raycastTarget = false;
                }

                Button panelButton = brainGamesPanelRect.GetComponent<Button>();
                if (panelButton != null)
                    panelButton.enabled = false;

                Transform title = brainGamesPanelRect.Find("Title");
                if (title != null)
                {
                    title.gameObject.SetActive(false);
                    if (title is RectTransform titleRect)
                    {
                        titleRect.anchorMin = new Vector2(0f, 1f);
                        titleRect.anchorMax = new Vector2(1f, 1f);
                        titleRect.pivot = new Vector2(0.5f, 1f);
                        titleRect.anchoredPosition = new Vector2(0f, -34f);
                        titleRect.sizeDelta = new Vector2(-220f, 72f);
                    }
                }
            }

            if (brainGamesCloseButton != null)
                brainGamesCloseButton.gameObject.SetActive(false);
        }

        private static void SetButtonActive(Button button, bool active)
        {
            if (button != null && button.gameObject.activeSelf != active)
                button.gameObject.SetActive(active);
        }

        private static void ConfigureProxyButton(Button proxy, UnityEngine.Events.UnityAction action)
        {
            if (proxy == null)
                return;

            proxy.onClick.RemoveAllListeners();
            proxy.interactable = action != null;
            if (action != null)
                proxy.onClick.AddListener(action);
        }

        private static void SuppressMainChrome(bool suppressed)
        {
            MainLobbyUiCoordinator.SetRightStackSuppressed(suppressed);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(suppressed);
            SetNamedObjectActive("ButtonOpenShop", !suppressed);
            SetNamedObjectActive("ButtonOpenMainRewardedBonus", !suppressed);
            SetNamedObjectActive("DynastyVaultButton", !suppressed);
            SetNamedObjectActive("DynastyBankButton", !suppressed);
            SetNamedObjectActive("ExchangeMonitorButton", !suppressed);
            SetNamedObjectActive("MainWeeklyRewardButton", !suppressed);
            SetMainDynamicWindowsSuppressed(suppressed);
        }

        private static void SetMainDynamicWindowsSuppressed(bool suppressed)
        {
            if (suppressed)
            {
                SetComponentObjectsActive<MailboxUI>(false);
                SetComponentObjectsActive<FriendsUI>(false);
                SetComponentObjectsActive<GlobalChatUI>(false);
                SetComponentObjectsActive<AllianceUI>(false);
                SetButtonsWithTextActive(false, "İttifak", "Ittifak", "Posta", "Arkadaşlar", "Arkadaslar", "Genel Sohbet");
                return;
            }

            MailboxBootstrap.EnsureForCurrentScene();
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();
            AllianceBootstrap.EnsureForCurrentScene();
        }

        private static void SetComponentObjectsActive<T>(bool active) where T : Component
        {
            T[] components = FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null)
                    component.gameObject.SetActive(active);
            }
        }

        private static void SetNamedObjectActive(string objectName, bool active)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return;

            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && string.Equals(obj.name, objectName, System.StringComparison.Ordinal))
                    obj.SetActive(active);
            }
        }

        private static void HideLegacyGameEntryButtons()
        {
            SetNamedObjectActive("Btn_Mahjong", false);
            SetNamedObjectActive("Btn_Okey", false);
            SetNamedObjectActive(BrainGamesTabButtonName, false);
            SetNamedObjectActive("BrainGamesPanel", false);
            SetNamedObjectActive("Btn_SymSudoku_Runtime", false);
            SetNamedObjectActive("Btn_OzLobby_Runtime", false);
            SetButtonsWithTextActive(false, "Mahjong", "SUDOKU", "SYMBIGRID", "BLOCK BUST", "Block Bust", "ÖzOkey", "OzOkey", "OZ OKEY", "ÖZ OKEY");
        }

        private static void SetButtonsWithTextActive(bool active, params string[] labels)
        {
            if (labels == null || labels.Length == 0)
                return;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                string value = text != null && text.text != null ? text.text.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    string label = labels[labelIndex];
                    if (!string.IsNullOrWhiteSpace(label) && value.IndexOf(label, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (button.gameObject.name.StartsWith("BrainGames", System.StringComparison.Ordinal))
                            break;

                        button.gameObject.SetActive(active);
                        break;
                    }
                }
            }
        }

        private static void CleanupBrainGamesPanelChildren(Transform panel)
        {
            if (panel == null)
                return;

            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                Transform child = panel.GetChild(i);
                if (child == null)
                    continue;

                string name = child.name;
                bool keep =
                    string.Equals(name, "Title", System.StringComparison.Ordinal) ||
                    string.Equals(name, "BrainGamesClose", System.StringComparison.Ordinal) ||
                    string.Equals(name, MahjongButtonName, System.StringComparison.Ordinal) ||
                    string.Equals(name, "BrainGamesSudokuButton", System.StringComparison.Ordinal) ||
                    string.Equals(name, SymbiGridButtonName, System.StringComparison.Ordinal) ||
                    string.Equals(name, OrbiosisButtonName, System.StringComparison.Ordinal) ||
                    string.Equals(name, SymbiozButtonName, System.StringComparison.Ordinal) ||
                    string.Equals(name, LegacyBlockBustButtonName, System.StringComparison.Ordinal) ||
                    string.Equals(name, "BrainGamesOkeyButton", System.StringComparison.Ordinal);

                if (!keep)
                    Destroy(child.gameObject);
            }
        }

        private static void RaiseButton(Button button)
        {
            if (button != null)
                button.transform.SetAsLastSibling();
        }

        private static void CleanupLooseMailboxButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !string.Equals(button.name, "MailboxButton", System.StringComparison.Ordinal))
                    continue;

                if (button.GetComponentInParent<MailboxUI>(true) != null)
                    continue;

                Destroy(button.gameObject);
            }
        }

        private static void NormalizeVisualChildren(RectTransform root)
        {
            RectTransform[] children = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                RectTransform child = children[i];
                if (child == null || child == root)
                    continue;

                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }
        }

        private interface IMainSceneLayoutModule
        {
            void ConfigureScaler(CanvasScaler targetScaler);
            void Apply(MainSceneResponsiveLayout owner);
        }

        private sealed class LandscapeLayoutModule : IMainSceneLayoutModule
        {
            public void ConfigureScaler(CanvasScaler targetScaler)
            {
                MainLobbyUiCoordinator.ConfigureOverlayScaler(targetScaler);
            }

            public void Apply(MainSceneResponsiveLayout owner)
            {
                StretchBackground(owner.FindRect("BG"), MainLobbyUiCoordinator.UseTabletLandscapeComposition());
            }
        }

        private sealed class PortraitLayoutModule : IMainSceneLayoutModule
        {
            public void ConfigureScaler(CanvasScaler targetScaler)
            {
                MainLobbyUiCoordinator.ConfigureResponsiveLobbyScaler(targetScaler);
            }

            public void Apply(MainSceneResponsiveLayout owner)
            {
                StretchBackground(owner.FindRect("BG"), true);
            }
        }
    }

    public sealed class MainHubStateController : MonoBehaviour
    {
        private const string MainSceneName = "Main";
        private const float DefaultInputBlockSeconds = 2.75f;
        private static MainHubStateController instance;
        private static float blockWindowOpenUntilRealtime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsUsableScene(scene))
                return;

            if (!string.Equals(scene.name, MainSceneName, System.StringComparison.Ordinal))
                return;

            if (instance == null)
            {
                GameObject host = new GameObject("MainHubStateController");
                SceneManager.MoveGameObjectToScene(host, scene);
                instance = host.AddComponent<MainHubStateController>();
            }
            else if (instance.gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(instance.gameObject, scene);
            }

            BeginMainEntryStabilization(DefaultInputBlockSeconds);
        }

        public static void BeginMainEntryStabilization(float seconds = DefaultInputBlockSeconds)
        {
            if (!IsMainSceneActive())
                return;

            blockWindowOpenUntilRealtime = Mathf.Max(blockWindowOpenUntilRealtime, Time.unscaledTime + Mathf.Max(0.1f, seconds));
            ForceCloseTransientWindows();
            ClearSelectedUi();

            if (instance != null && instance.isActiveAndEnabled)
            {
                instance.StopAllCoroutines();
                instance.StartCoroutine(instance.StabilizeMainRoutine());
            }
        }

        public static void CancelMainEntryStabilization()
        {
            blockWindowOpenUntilRealtime = 0f;
            ClearSelectedUi();

            if (instance != null && instance.isActiveAndEnabled)
                instance.StopAllCoroutines();
        }

        public static bool CanOpenMainWindow(string owner)
        {
            if (!IsMainSceneActive())
                return true;

            MainSceneResponsiveLayout.CancelMainReturnSanitizers();
            CancelMainEntryStabilization();
            MainGameLaunchBootstrap.HideForMainWindowNow();
            Debug.Log($"[MainHubStateController] Main window accepted on first interaction: {owner}");
            return true;
        }

        public static void NotifyMainWindowClosed()
        {
            if (!IsMainSceneActive())
                return;

            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainGameLaunchBootstrap.RefreshVisibilityNow();
        }

        private IEnumerator StabilizeMainRoutine()
        {
            int frames = 0;
            while (IsMainSceneActive() && Time.unscaledTime < blockWindowOpenUntilRealtime)
            {
                ForceCloseTransientWindows();
                ClearSelectedUi();
                frames++;
                yield return null;
            }

            for (int i = 0; i < 8; i++)
            {
                if (!IsMainSceneActive())
                    yield break;

                ForceCloseTransientWindows();
                ClearSelectedUi();
                yield return null;
            }

            if (!IsMainSceneActive())
                yield break;

            NotifyMainWindowClosed();
            Debug.Log($"[MainHubStateController] Main stabilized after {frames} frames.");
        }

        private static bool IsMainSceneActive()
        {
            return string.Equals(SceneManager.GetActiveScene().name, MainSceneName, System.StringComparison.Ordinal);
        }

        private static bool IsUsableScene(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private static void ForceCloseTransientWindows()
        {
            SetObjectsActive("ShopOverlay", false);
            SetObjectsActive("MainShopOverlay", false);
            SetObjectsActive("DynastyVaultOverlay", false);
            SetObjectsActive("DynastyBankOverlay", false);
            SetObjectsActive("ExchangeMonitorOverlay", false);
            SetObjectsActive("MainWeeklyRewardOverlay", false);
            SetObjectsActive("MainRewardedBonusOverlay", false);
            SetObjectsActive("MailboxPanel", false);
            SetObjectsActive("FriendsPanel", false);
            SetObjectsActive("ChatPanel", false);
            SetObjectsActive("AlliancePanel", false);
            SetObjectsActive("AllianceBackdrop", false);
            SetObjectsActive("PanelRoot", false);
            SetObjectsActive("MainInfoHintOverlay", false);
            DestroyObjectsByName("Btn_SymSudoku_Runtime");
            DestroyObjectsByName("Btn_OzLobby_Runtime");

            MainShopUI.ForceResetAllToClosed(0.25f);
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        private static void SetObjectsActive(string objectName, bool active)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && string.Equals(obj.name, objectName, System.StringComparison.Ordinal))
                    obj.SetActive(active);
            }
        }

        private static void DestroyObjectsByName(string objectName)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && string.Equals(obj.name, objectName, System.StringComparison.Ordinal))
                    Destroy(obj);
            }
        }

        private static void ClearSelectedUi()
        {
            EventSystem current = EventSystem.current;
            if (current != null)
                current.SetSelectedGameObject(null);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
