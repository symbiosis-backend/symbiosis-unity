using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public sealed class MainGameLaunchBootstrap : MonoBehaviour
    {
        private const string MainSceneName = "Main";
        private const string RootName = "BrainGamesPermanentLaunchCanvas";
        private const int SortingOrder = 29000;

        private const string MahjongSpritePath = "Mahjong/Sprites/MainSettings/MainMahjongWorldButton";
        private const string SymbiGridSpritePath = "SymbiGrid/SymbiGridTitleLogo";
        private const string OrbiosisSpritePath = "Orbiosis/OrbiosisLogo_Symmetric_ImageGen_02_Clean";
        private const string OrbiosisDevelopmentBadgeName = "OrbiosisDevelopmentBadge";
        private const string SymbiozSpritePath = "DynastyLegacy/DynastyLegacyButton";
        private static readonly bool SymbiozLaunchEnabled = false;

        private static MainGameLaunchBootstrap instance;
        private static Sprite mahjongSprite;
        private static Sprite symbiGridSprite;
        private static Sprite orbiosisSprite;
        private static Sprite symbiozSprite;
        private static bool leavingMainScene;

        private Canvas launchCanvas;
        private GraphicRaycaster launchRaycaster;
        private RectTransform launchRoot;
        private Button mahjongButton;
        private Button symbiGridButton;
        private Button orbiosisButton;
        private Button symbiozButton;
        private SceneNavigator sceneNavigator;
        private float nextGuardRealtime;
        private bool actionsConfigured;
        private bool eventSystemChecked;

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

            leavingMainScene = false;

            if (instance == null)
            {
                GameObject host = new GameObject("MainGameLaunchBootstrap");
                SceneManager.MoveGameObjectToScene(host, scene);
                instance = host.AddComponent<MainGameLaunchBootstrap>();
            }
            else if (instance.gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(instance.gameObject, scene);
            }

            instance.EnsureLaunchLayer();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            StartCoroutine(WarmMainLaunchLayer());
        }

        private IEnumerator WarmMainLaunchLayer()
        {
            for (int i = 0; i < 180; i++)
            {
                if (leavingMainScene)
                    yield break;

                EnsureLaunchLayer();
                yield return null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextGuardRealtime)
                return;

            nextGuardRealtime = Time.unscaledTime + 0.5f;
            EnsureLaunchLayer();
        }

        private void EnsureLaunchLayer()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (leavingMainScene || !isActiveAndEnabled || gameObject.scene != activeScene ||
                !string.Equals(activeScene.name, MainSceneName, System.StringComparison.Ordinal))
                return;

            if (!eventSystemChecked)
            {
                EventSystemInputModeGuard.EnsureCompatibleEventSystems();
                eventSystemChecked = true;
            }

            EnsureCanvas();
            EnsureButtons();
            LayoutButtons();

            RefreshVisibilityOnly();
        }

        private void RefreshVisibilityOnly()
        {
            if (launchCanvas == null || launchRoot == null)
                return;

            bool coveredByMainOverlay = IsAnyActive(
                "SymbiozLoginWindow",
                "OrbiosisHangarRoot",
                "MainDevelopmentNotice",
                "MainMahjongModeChoice",
                "DynastyVaultOverlay",
                "DynastyBankOverlay",
                "ExchangeMonitorOverlay",
                "MainWeeklyRewardOverlay",
                "ShopOverlay",
                "MainShopOverlay",
                "PanelRoot",
                "MailboxPanel",
                "ProfileModalOverlay",
                "FriendsPanel",
                "ChatPanel",
                "AlliancePanel",
                "AllianceBackdrop",
                "AdminPanelRoot",
                "ChangelogOverlay",
                "MainInfoHintOverlay");
            SetLaunchLayerVisible(!coveredByMainOverlay);
        }

        public static void RefreshVisibilityNow()
        {
            if (!leavingMainScene && instance != null && instance.isActiveAndEnabled)
                instance.RefreshVisibilityOnly();
        }

        public static void PrepareForSceneExit(string targetSceneName)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MainSceneName, System.StringComparison.Ordinal) ||
                string.Equals(targetSceneName, MainSceneName, System.StringComparison.Ordinal))
                return;

            leavingMainScene = true;
            if (instance == null)
                return;

            instance.StopAllCoroutines();
            instance.SetLaunchLayerVisible(false);
            if (instance.launchRaycaster != null)
                instance.launchRaycaster.enabled = false;
        }

        public static void HideForMainWindowNow()
        {
            if (instance != null)
                instance.SetLaunchLayerVisible(false);
        }

        private void EnsureCanvas()
        {
            if (launchCanvas != null && launchRoot != null)
            {
                ConfigureCanvas();
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate != null && candidate.gameObject.scene == activeScene &&
                    string.Equals(candidate.name, RootName, System.StringComparison.Ordinal))
                {
                    launchCanvas = candidate;
                    launchRoot = candidate.transform as RectTransform;
                    ConfigureCanvas();
                    return;
                }
            }

            GameObject rootObject = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (IsUsableScene(activeScene))
                SceneManager.MoveGameObjectToScene(rootObject, activeScene);
            launchCanvas = rootObject.GetComponent<Canvas>();
            launchRoot = rootObject.transform as RectTransform;
            ConfigureCanvas();
        }

        private void ConfigureCanvas()
        {
            if (launchCanvas == null)
                return;

            launchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            launchCanvas.overrideSorting = true;
            launchCanvas.sortingOrder = SortingOrder;
            launchCanvas.pixelPerfect = false;
            launchRaycaster = launchCanvas.GetComponent<GraphicRaycaster>();
            if (launchRaycaster == null)
                launchRaycaster = launchCanvas.gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = launchCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = launchCanvas.gameObject.AddComponent<CanvasScaler>();
            MainLobbyUiCoordinator.ConfigureOverlayScaler(scaler);

            if (launchRoot != null)
            {
                launchRoot.anchorMin = Vector2.zero;
                launchRoot.anchorMax = Vector2.one;
                launchRoot.pivot = new Vector2(0.5f, 0.5f);
                launchRoot.offsetMin = Vector2.zero;
                launchRoot.offsetMax = Vector2.zero;
                launchRoot.anchoredPosition = Vector2.zero;
                launchRoot.localScale = Vector3.one;
                launchRoot.localRotation = Quaternion.identity;
            }
        }

        private void EnsureButtons()
        {
            if (launchRoot == null)
                return;

            mahjongButton = EnsureButton(mahjongButton, "BrainGamesPermanentMahjongButton", "World of Mahjong", LoadSprite(ref mahjongSprite, MahjongSpritePath), true, true);
            symbiGridButton = EnsureButton(symbiGridButton, "BrainGamesPermanentSymbiGridButton", "SYMBIGRID", LoadSprite(ref symbiGridSprite, SymbiGridSpritePath), true, true);
            orbiosisButton = EnsureButton(orbiosisButton, "BrainGamesPermanentOrbiosisButton", "ORBIOSIS", LoadSprite(ref orbiosisSprite, OrbiosisSpritePath), false, true);
            EnsureOrbiosisDevelopmentBadge(orbiosisButton);
            symbiozButton = EnsureButton(symbiozButton, "BrainGamesPermanentSymbiozButton", "Dynasty Legacy - Symbiosis", LoadSprite(ref symbiozSprite, SymbiozSpritePath), true, SymbiozLaunchEnabled);

            if (!actionsConfigured)
            {
                ConfigureActions();
                actionsConfigured = true;
            }
        }

        private Button EnsureButton(Button cached, string objectName, string label, Sprite sprite, bool preserveAspect, bool active)
        {
            if (cached == null)
            {
                Transform existing = launchRoot.Find(objectName);
                cached = existing != null ? existing.GetComponent<Button>() : null;
            }

            if (cached == null)
            {
                GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(launchRoot, false);
                cached = buttonObject.GetComponent<Button>();

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 30f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                MainLobbyButtonStyle.ApplyFont(text);
            }

            Image image = cached.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = preserveAspect;
                image.color = sprite != null ? Color.white : new Color(0.015f, 0.052f, 0.095f, 0.92f);
                image.raycastTarget = true;
            }

            TMP_Text tmp = cached.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                if (tmp.font == null)
                    MainLobbyButtonStyle.ApplyFont(tmp);

                tmp.text = label;
                tmp.gameObject.SetActive(sprite == null || string.Equals(objectName, "BrainGamesPermanentSymbiozButton", System.StringComparison.Ordinal));
            }

            cached.interactable = true;
            if (cached.gameObject.activeSelf != active)
                cached.gameObject.SetActive(active);
            return cached;
        }

        private void LayoutButtons()
        {
            LayoutButton(mahjongButton, new Vector2(-230f, 108f), new Vector2(400f, 150f));
            LayoutButton(symbiGridButton, new Vector2(230f, 108f), new Vector2(400f, 150f));
            LayoutButton(orbiosisButton, new Vector2(0f, -72f), new Vector2(400f, 150f));
            if (SymbiozLaunchEnabled)
                LayoutButton(symbiozButton, new Vector2(230f, -72f), new Vector2(560f, 136f));
        }

        private static void LayoutButton(Button button, Vector2 position, Vector2 size)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null)
                return;

            MainLobbyUiCoordinator.LayoutMainCentered(rect, position, size);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();

            LayoutOrbiosisDevelopmentBadge(button);

            RectTransform labelRect = rect.Find("Label") as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelRect.localScale = Vector3.one;
                labelRect.localRotation = Quaternion.identity;
            }
        }

        private void ConfigureActions()
        {
            ConfigureButton(mahjongButton, OpenMahjong);
            ConfigureButton(symbiGridButton, OpenSymbiGrid);
            ConfigureButton(orbiosisButton, OpenOrbiosis);
            ConfigureButton(symbiozButton, SymbiozLaunchEnabled ? OpenSymbioz : null);
        }

        private static void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(action);
        }

        private void OpenMahjong()
        {
            Debug.Log("[MainGameLaunchBootstrap] Mahjong launch clicked.");
            SceneNavigator navigator = ResolveSceneNavigator();
            if (navigator != null)
                navigator.LoadLobbyMahjong();
        }

        private void OpenSymbiGrid()
        {
            Debug.Log("[MainGameLaunchBootstrap] SymbiGrid launch clicked.");
            SceneNavigator navigator = ResolveSceneNavigator();
            if (navigator != null)
                navigator.LoadSymbiGrid();
        }

        private void OpenOrbiosis()
        {
            if (MainSceneResponsiveLayout.IsOrbiosisAccessEnabled)
            {
                Debug.Log("[MainGameLaunchBootstrap] Orbiosis launch clicked.");
                SceneNavigator navigator = ResolveSceneNavigator();
                if (navigator != null)
                    navigator.LoadOrbiosis();
                return;
            }

            Debug.Log("[MainGameLaunchBootstrap] Orbiosis is under refinement; showing the Main notice.");
            MainSceneResponsiveLayout layout = FindAnyObjectByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            if (layout == null)
            {
                MainSceneResponsiveLayout.ForceRefreshCurrentScene();
                layout = FindAnyObjectByType<MainSceneResponsiveLayout>(FindObjectsInactive.Include);
            }

            if (layout != null)
                layout.OpenOrbiosisDevelopmentFromMainLaunch();
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

            if (text.font == null)
                MainLobbyButtonStyle.ApplyFont(text);

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

        private void OpenSymbioz()
        {
            if (!MainHubStateController.CanOpenMainWindow("SymbiozLogin"))
                return;

            SetLaunchLayerVisible(false);

            MainSceneResponsiveLayout layout = FindAnyObjectByType<MainSceneResponsiveLayout>();
            if (layout != null)
            {
                layout.OpenSymbiozLoginFromMainLaunch();
                return;
            }

            SceneNavigator navigator = ResolveSceneNavigator();
            if (navigator != null)
                navigator.LoadSymbiozFlagship();
        }

        private SceneNavigator ResolveSceneNavigator()
        {
            if (sceneNavigator != null)
                return sceneNavigator;

            sceneNavigator = FindAnyObjectByType<SceneNavigator>(FindObjectsInactive.Include);
            if (sceneNavigator != null)
                return sceneNavigator;

            GameObject host = new GameObject("SceneNavigator");
            Scene activeScene = SceneManager.GetActiveScene();
            if (IsUsableScene(activeScene))
                SceneManager.MoveGameObjectToScene(host, activeScene);
            sceneNavigator = host.AddComponent<SceneNavigator>();
            Debug.Log("[MainGameLaunchBootstrap] Created runtime SceneNavigator for Main launch buttons.");
            return sceneNavigator;
        }

        private static Sprite LoadSprite(ref Sprite cache, string path)
        {
            if (cache != null)
                return cache;

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                return cache;
            }

            cache = Resources.Load<Sprite>(path);
            if (cache != null)
                return cache;

            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
                cache = sprites[0];

            return cache;
        }

        private static bool IsAnyActive(params string[] names)
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

        private void SetLaunchLayerVisible(bool visible)
        {
            if (launchCanvas != null)
                launchCanvas.enabled = visible;

            if (launchRaycaster == null && launchCanvas != null)
                launchRaycaster = launchCanvas.GetComponent<GraphicRaycaster>();
            if (launchRaycaster != null)
                launchRaycaster.enabled = visible;

            SetButtonRaycast(mahjongButton, visible);
            SetButtonRaycast(symbiGridButton, visible);
            SetButtonRaycast(orbiosisButton, visible);
            SetButtonRaycast(symbiozButton, visible && SymbiozLaunchEnabled);
        }

        private static void SetButtonRaycast(Button button, bool enabled)
        {
            if (button == null)
                return;

            button.interactable = enabled;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = enabled;
        }

        private static bool IsUsableScene(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
