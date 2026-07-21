using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Networking;
using Dynasty.Legacy.CorrosionCollapse.Players;
using Dynasty.Legacy.CorrosionCollapse.Pooling;
using Dynasty.Legacy.CorrosionCollapse.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public sealed class CorrosionCollapseBootstrap : MonoBehaviour
    {
        private const string SceneName = "CorrosionCollapse";

        [SerializeField] private Transform systemsRoot;
        [SerializeField] private Transform boardRoot;
        [SerializeField] private Transform playersRoot;
        [SerializeField] private Transform uiRoot;
        [SerializeField] private bool routeBuilderMode = true;

        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapScene()
        {
            SceneManager.sceneLoaded += (_, _) => EnsureBootstrapForActiveScene();
            EnsureBootstrapForActiveScene();
        }

        private static void EnsureBootstrapForActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != SceneName && GameObject.Find("CorrosionCollapseRoot") == null)
            {
                return;
            }

            if (FindAnyObjectByType<CorrosionCollapseBootstrap>() != null)
            {
                return;
            }

            GameObject root = GameObject.Find("CorrosionCollapseRoot") ?? new GameObject("CorrosionCollapseRoot");
            root.AddComponent<CorrosionCollapseBootstrap>();
        }

        private void Awake()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            EnsureHierarchy();
            EnsureCamera();

            ObjectPoolManager pool = systemsRoot.gameObject.GetComponent<ObjectPoolManager>() ?? systemsRoot.gameObject.AddComponent<ObjectPoolManager>();
            LocalServerAuthority authority = systemsRoot.gameObject.GetComponent<LocalServerAuthority>() ?? systemsRoot.gameObject.AddComponent<LocalServerAuthority>();
            BoardBuilder builder = boardRoot.gameObject.GetComponent<BoardBuilder>() ?? boardRoot.gameObject.AddComponent<BoardBuilder>();
            TurnManager turnManager = systemsRoot.gameObject.GetComponent<TurnManager>() ?? systemsRoot.gameObject.AddComponent<TurnManager>();
            MapBackgroundView mapBackground = boardRoot.gameObject.GetComponent<MapBackgroundView>() ?? boardRoot.gameObject.AddComponent<MapBackgroundView>();

            Transform templatesRoot = EnsureChild(systemsRoot, "PoolTemplates");
            templatesRoot.gameObject.SetActive(false);
            RegisterPools(pool, templatesRoot);
            builder.Initialize(boardRoot, pool);

            Canvas canvas = EnsureCanvas();
            mapBackground.BuildUI(canvas);
            EnsureEventSystem();
            if (routeBuilderMode)
            {
                Transform existingHud = canvas.transform.Find("CorrosionHUD");
                if (existingHud != null)
                {
                    existingHud.gameObject.SetActive(false);
                }
            }

            bool matchStarted = false;
            void StartConfiguredMatch()
            {
                if (matchStarted)
                {
                    return;
                }

                matchStarted = true;
                CorrosionCollapseHud hud = CorrosionCollapseHud.Create(canvas);
                hud.transform.SetParent(canvas.transform, false);
                hud.gameObject.SetActive(true);
                IReadOnlyList<PlayerView> playerViews = CreatePlayers(pool, builder.Graph);
                turnManager.Initialize(builder, playerViews, hud, authority);
                turnManager.StartMatch();
            }

            if (routeBuilderMode)
            {
                RuntimeRouteBuilder routeBuilder = systemsRoot.gameObject.GetComponent<RuntimeRouteBuilder>() ?? systemsRoot.gameObject.AddComponent<RuntimeRouteBuilder>();
                routeBuilder.Initialize(builder, StartConfiguredMatch);
            }
            else
            {
                StartConfiguredMatch();
            }
        }

        private void EnsureHierarchy()
        {
            gameObject.name = "CorrosionCollapseRoot";
            systemsRoot = EnsureChild(transform, "Systems");
            boardRoot = EnsureChild(transform, "Board");
            playersRoot = EnsureChild(transform, "Players");
            uiRoot = EnsureChild(transform, "UI");
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private void RegisterPools(ObjectPoolManager pool, Transform templatesRoot)
        {
            GameObject gridPrefab = CreatePrimitiveTemplate("GridTileTemplate", PrimitiveType.Cube, templatesRoot, new Color(0.66f, 0.56f, 0.39f, 1f));
            DiamondTileMesh.Ensure(gridPrefab);
            gridPrefab.AddComponent<GridCellView>();
            gridPrefab.transform.localScale = new Vector3(0.69f, 1f, 0.69f);

            GameObject nodePrefab = CreatePrimitiveTemplate("BoardNodeTemplate", PrimitiveType.Cube, templatesRoot, new Color(0.74f, 0.58f, 0.36f, 1f));
            DiamondTileMesh.Ensure(nodePrefab);
            nodePrefab.AddComponent<BoardNodeView>();
            nodePrefab.transform.localScale = new Vector3(0.72f, 1f, 0.72f);

            GameObject playerPrefab = CreatePrimitiveTemplate("PlayerTemplate", PrimitiveType.Sphere, templatesRoot, Color.white);
            playerPrefab.AddComponent<PlayerView>();
            playerPrefab.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);

            GameObject effectPrefab = CreatePrimitiveTemplate("CorrosionEffectTemplate", PrimitiveType.Sphere, templatesRoot, new Color(0.23f, 0f, 0.35f, 0.8f));
            effectPrefab.transform.localScale = Vector3.one * 0.6f;

            pool.RegisterPool("GridTile", gridPrefab, 2025, boardRoot);
            pool.RegisterPool("BoardNode", nodePrefab, 512, boardRoot);
            pool.RegisterPool("Player", playerPrefab, 4, playersRoot);
            pool.RegisterPool("CorrosionEffect", effectPrefab, 24, systemsRoot);
        }

        private static GameObject CreatePrimitiveTemplate(string name, PrimitiveType type, Transform parent, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            if (obj.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
            }

            var renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
            obj.SetActive(false);
            return obj;
        }

        private IReadOnlyList<PlayerView> CreatePlayers(ObjectPoolManager pool, BoardGraph graph)
        {
            var views = new List<PlayerView>(4);
            string[] names = { "Anima", "Chronos Bot", "Aether Bot", "Lumen Bot" };
            Color[] colors =
            {
                new Color(0.95f, 0.82f, 0.28f, 1f),
                new Color(0.18f, 0.62f, 0.95f, 1f),
                new Color(0.58f, 0.32f, 0.92f, 1f),
                new Color(0.25f, 0.9f, 0.48f, 1f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject playerObject = pool.Get("Player", playersRoot);
                if (playerObject == null)
                {
                    continue;
                }

                var state = new PlayerState
                {
                    playerId = i,
                    nickname = names[i],
                    currentNode = graph.startNode,
                    score = 0,
                    isBot = i != 0
                };

                PlayerView view = playerObject.GetComponent<PlayerView>() ?? playerObject.AddComponent<PlayerView>();
                view.Bind(state, colors[i]);
                views.Add(view);
            }

            return views;
        }

        private Canvas EnsureCanvas()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                CanvasScaler existingScaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
                existingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                existingScaler.referenceResolution = new Vector2(1920f, 1080f);
                existingScaler.matchWidthOrHeight = 0.5f;
                return canvas;
            }

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(uiRoot, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(FindAnyObjectByType<CorrosionCollapseBootstrap>().transform, false);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void EnsureCamera()
        {
            Camera camera = Camera.main ?? FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("[Game] Corrosion Collapse requires an existing scene camera.");
                return;
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.22f, 0.17f, 0.13f, 1f);
        }
    }
}
