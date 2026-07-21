using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MahjongGame;
using MahjongGame.Multiplayer;

namespace Dynasty.Legacy.Symbioz
{
    public sealed class SymbiozFlagshipPrototype : MonoBehaviour
    {
        private const int GridSize = 275;
        private const float CellSize = 1f;
        private const float GroundPawnSpeedCellsPerSecond = 4.4f;
        private const float RoadPawnSpeedCellsPerSecond = 8f;
        private const float MinCameraZoom = 3.5f;
        private const float MaxCameraZoom = 95f;
        private const float IsoCameraPitch = 60f;
        private const float IsoCameraYaw = 45f;
        private const float IsoCameraDistance = 48f;
        private const float KeyboardZoomStep = 6f;
        private const float CameraMoveSmoothTime = 0.09f;
        private const float CameraZoomSmoothTime = 0.08f;
        private const float TouchPinchZoomScale = 0.035f;
        private const float TwoFingerMoveMaxDistancePixels = 120f;
        private const float DoubleToolPressSeconds = 0.65f;
        private const float PersistentSaveDebounceSeconds = 0.75f;
        private const int TileAtlasGrid = 4;
        private const int SimulatedOnlineTarget = 10000;
        private const int MaxNoteLength = 42;
        private const float SharedWorldDownloadIntervalSeconds = 5f;
        private const float SharedWorldLocalEditGraceSeconds = 9f;
        private const float SharedPlayerPresenceIntervalSeconds = 1f;
        private const float SharedPlayerMovingPresenceIntervalSeconds = 0.18f;
        private const float RemotePawnInterpolationDelaySeconds = 0.24f;
        private const float RemotePawnMaxExtrapolationSeconds = 0.22f;
        private const float RemotePawnVisualSmoothTime = 0.045f;
        private const float RemotePawnSnapDistance = 8f;
        private const float SunUpdateIntervalSeconds = 1f;
        private const int MateriaEdgeBandCells = 30;
        private const int MateriaBerryBushesPerPlayer = 3;
        private const int MateriaBerryClusterRadiusCells = 4;
        private const int MateriaTreeMinimum = 45;
        private const int MateriaTreesPerPlayer = 10;
        private const int MateriaVisibleTreeMinimum = 16;
        private const int MateriaVisibleTreeRadiusCells = 28;
        private const int MateriaTreeEdgeMarginCells = 5;
        private const float MateriaGeneratorIntervalSeconds = 5f;
        private const int BerryBushMaxBerries = 3;
        private const float BerryBushInteractDistanceCells = 1.65f;
        private const int TreeVariantCount = 5;
        private const int TreeWoodYield = 3;
        private const float TreeChopSeconds = 60f;
        private const float TreeInteractDistanceCells = 1.75f;
        private const float SatietyMax = 100f;
        private const float SatietyDrainPerSecond = 0.35f;
        private const float CarryWeightMax = 24f;
        private const float BerryCarryWeight = 0.35f;
        private const int LargeBuildingFootprintWidthCells = 6;
        private const int LargeBuildingFootprintHeightCells = 4;
        private const int EstateHouseWidthCells = 7;
        private const int EstateHouseHeightCells = 7;
        private const int EstateTowerWidthCells = 9;
        private const int EstateTowerHeightCells = 9;
        private const int EstateKeepWidthCells = 14;
        private const int EstateKeepHeightCells = 14;
        private const int EstateCastleWidthCells = 19;
        private const int EstateCastleHeightCells = 19;
        private const int ResourceWorksiteFootprintCells = 7;
        private const float ResourceWorkIntervalSeconds = 10f;
        private const float ResourceWorkInteractDistanceCells = 1.75f;
        private const string ResourceInventoryStonePrefsKey = "DynastySymbioz_ResourceInventory_Stone";
        private const string ResourceInventoryWoodPrefsKey = "DynastySymbioz_ResourceInventory_Wood";
        private const float PawnGroundYOffset = 0.18f;
        private const float PawnSpriteWidth = 1.65f;
        private const float PawnSpriteHeight = 3.05f;
        private const float PawnSpriteHorizontalCrop = 0f;
        private const float PawnSpriteBottomCrop = 0f;
        private const float PawnSpriteTopCrop = 0f;
        private const int PawnWalkColumns = 4;
        private const int PawnWalkRows = 8;
        private const float PawnWalkFrameSeconds = 0.115f;
        private const float DoorPromptCooldownSeconds = 0.7f;
        private const float CellDoubleClickSeconds = 0.42f;
        private const float TouchHoldInspectSeconds = 0.58f;
        private const float PointerHoldActionSeconds = 0.52f;
        private const float PointerHoldMoveTolerancePixels = 12f;
        private const string SleeperNotePrefix = "sleeper=1";
        private const string FirstLocationName = "Slums";
        private const string SecondLocationName = "Elysium";
        private const string ThirdLocationName = "City Gate";
        private const string FourthLocationName = "City Center";
        private const string SaveFileName = "dynasty_legacy_symbioz_world_v1.json";

        [Header("Scene")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Canvas canvas;

        [Header("Shared World")]
        [SerializeField] private bool enableSharedWorldSync = true;
        [SerializeField] private string sharedWorldEndpoint = "https://dlsymbiosis.com/dynasty/symbioz/world";
        [SerializeField] private string sharedPlayersEndpoint = "https://dlsymbiosis.com/dynasty/symbioz/players";

        private Transform worldRoot;
        private Transform gridRoot;
        private Transform objectsRoot;
        private Transform doorsRoot;
        private Transform playersRoot;
        private Transform pawnVisualRoot;
        private GameObject pawn;
        private MeshFilter pawnSpriteMeshFilter;
        private MeshRenderer pawnSpriteRenderer;
        private GameObject moveTargetMarker;
        private GameObject selectedObjectFrame;
        private LineRenderer[] selectedObjectFrameLines;
        private GameObject selectedTreeOutline;
        private Material selectedTreeOutlineMaterial;
        private LineRenderer[] cursorFrameLines;
        private int pawnViewIndex;
        private int pawnViewFrame;
        private bool pawnViewFlipX;
        private bool pawnViewUsesIdleMaterial = true;
        private int pawnLastFacingView;
        private bool pawnLastFacingFlipX;
        private int pawnAnimationFrame;
        private float pawnAnimationTimer;
        private GameObject cursor;
        private GameObject centerDoor;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI detailText;
        private TextMeshProUGUI commandText;
        private TextMeshProUGUI selectionText;
        private TextMeshProUGUI runtimeLogText;
        private TextMeshProUGUI transitionPromptText;
        private TextMeshProUGUI buildSelectionText;
        private TextMeshProUGUI materiaText;
        private RectTransform buildPalettePanel;
        private RectTransform buildConfirmPanel;
        private RectTransform cellActionPanel;
        private RectTransform cellBuildCarouselPanel;
        private RectTransform objectInteractionPanel;
        private TextMeshProUGUI objectUseButtonText;
        private RectTransform objectProfilePanel;
        private RectTransform materiaPanel;
        private RectTransform notePanel;
        private RectTransform transitionPanel;
        private RectTransform playerStatusPanel;
        private TMP_InputField noteInput;
        private TextMeshProUGUI playerStatusText;
        private TextMeshProUGUI objectProfileText;
        private Material roadTileMaterial;
        private Material wallTileMaterial;
        private Material barrierTileMaterial;
        private Material shelterTileMaterial;
        private Material communityStorageMaterial;
        private Material slumTradeCenterMaterial;
        private Material stoneQuarryMaterial;
        private Material sawmillMaterial;
        private Material smallHouseExteriorMaterial;
        private Material[] resourceTreeMaterials;
        private Material portalMaterial;
        private Material groundTileMaterial;
        private Material pawnSpriteMaterial;
        private Material pawnIdleMaterial;
        private Light worldSunLight;
        private float nextSunUpdateTime;
        private string worldTimeLabel = "00:00";
        private string worldDayPhase = "Night";
        private float pawnHp = 100f;
        private float pawnSatiety = 100f;
        private float carriedWeight;
        private int carriedBerries;
        private int carriedStone;
        private int carriedWood;
        private float nextMateriaGeneratorTime;
        private bool hasActiveTreeChop;
        private Vector2Int activeTreeChopCell;
        private float activeTreeChopFinishTime;
        private SymbiozFishNetWorldBridge fishNetWorldBridge;
        private readonly Dictionary<LocationId, Dictionary<Vector2Int, PlacedObject>> placedObjectsByLocation = new Dictionary<LocationId, Dictionary<Vector2Int, PlacedObject>>();
        private readonly List<GameObject> buildPaletteItems = new List<GameObject>(16);
        private readonly Dictionary<string, RemotePawn> remotePawns = new Dictionary<string, RemotePawn>();
        private Dictionary<Vector2Int, PlacedObject> placedObjects;
        private LocationId currentLocation = LocationId.FirstSoil;
        private Vector2Int pawnCell;
        private Vector2Int targetCell;
        private Vector2Int selectedCell;
        private Vector2Int selectedObjectAnchorCell;
        private Vector2 pawnMoveInput;
        private BuildTool selectedTool = BuildTool.Wall;
        private BuildCategory activeBuildCategory = BuildCategory.Houses;
        private bool hasMoveTarget;
        private bool hasAutoMoveTarget;
        private bool hasSelectedObject;
        private bool isInsideEstateInterior;
        private ObjectKind activeEstateInteriorKind;
        private Vector2Int activeEstateInteriorAnchor;
        private bool isPanning;
        private bool cameraWasManuallyMoved;
        private bool isEditingNote;
        private bool isConfirmingTransition;
        private bool isBuildPaletteOpen;
        private bool hasPendingBuildKind;
        private bool hasPendingBuildPlacement;
        private bool mustLeavePortalBeforeTransition;
        private bool isApplyingSharedWorld;
        private bool hasPendingPersistentSave;
        private Coroutine sharedWorldPollingRoutine;
        private Coroutine sharedPlayerPresenceRoutine;
        private int lastSharedWorldHash;
        private bool hasCompletedInitialSharedWorldCheck;
        private bool isPlayerPresenceRequestInFlight;
        private bool useFishNetRealtime;
        private float suppressSharedWorldDownloadUntil;
        private string localClientId;
        private string localDisplayName;
        private string localDynastyName;
        private int localPlayerAge;
        private bool hasRegisteredLocalProfile;
        private bool hasDroppedExitSleeper;
        private float nextPersistentSaveTime;
        private Vector2Int editingNoteCell;
        private Vector2Int pendingBuildCell;
        private PlacedObject editingNoteObject;
        private ObjectKind pendingBuildKind;
        private GameObject pendingBuildPreview;
        private BuildTool lastPressedTool = BuildTool.Wall;
        private float lastToolPressTime = -10f;
        private Vector3 panStartWorld;
        private Vector3 cameraStart;
        private Vector3 cameraTargetPosition;
        private Vector3 cameraMoveVelocity;
        private Vector3 lastPresencePosition;
        private float cameraTargetZoom;
        private float cameraZoomVelocity;
        private float lastPresenceRealtime;
        private float doorPromptCooldownUntil;
        private Vector2 lastPrimaryTouchPosition;
        private Vector2 mousePressStartPosition;
        private Vector2 lastPresenceFacing = Vector2.down;
        private float mousePressStartTime;
        private float lastPinchDistance;
        private bool hasPrimaryTouchStart;
        private bool isPinching;
        private bool twoFingerMoveIssued;
        private Vector2Int lastClickedCell;
        private float lastCellClickTime = -10f;
        private float touchPressStartTime;
        private Vector2 touchPressStartPosition;
        private bool touchHoldInspectTriggered;
        private bool isResourceWorking;
        private ObjectKind activeResourceKind;
        private Vector2Int activeResourceAnchorCell;
        private float nextResourceWorkTickTime;
        private long estimatedServerNowMs;
        private float estimatedServerRealtime;
        private int localPresenceSequence;

        private enum LocationId
        {
            FirstSoil,
            ReturnYard,
            CityGate,
            CityCenter
        }

        private enum BuildTool
        {
            Wall,
            Road,
            Sign,
            Repair,
            BuildPlot,
            SleepingBag,
            CommunityStorage,
            SlumTradeCenter,
            StoneQuarry,
            Sawmill,
            SmallHouse,
            StoneTower,
            StoneKeep,
            Castle
        }

        private enum ObjectKind
        {
            Wall,
            Road,
            Sign,
            Repair,
            BuildPlot,
            SleepingBag,
            Tent,
            BerryBush,
            CommunityStorage,
            SlumTradeCenter,
            StoneQuarry,
            Sawmill,
            Tree,
            SmallHouse,
            StoneTower,
            StoneKeep,
            Castle
        }

        private enum BuildCategory
        {
            Houses,
            Roads,
            Utility,
            Resources
        }

        private sealed class PlacedObject
        {
            public ObjectKind Kind;
            public GameObject Root;
            public TextMeshPro Label;
            public string Note;
        }

        private sealed class RemotePawn
        {
            public GameObject Root;
            public MeshFilter SpriteMeshFilter;
            public MeshRenderer SpriteRenderer;
            public TextMeshPro Label;
            public Vector3 TargetWorld;
            public Vector3 SmoothVelocity;
            public Vector3 LastRenderPosition;
            public Vector2Int LastCell;
            public float LastSeenRealtime;
            public int LastFacingView;
            public bool LastFacingFlipX;
            public int ViewIndex;
            public int ViewFrame;
            public bool ViewFlipX;
            public bool ViewUsesIdleMaterial = true;
            public int AnimationFrame;
            public float AnimationTimer;
            public bool LastNetworkMoving;
            public int LastNetworkSequence;
            public long LastNetworkServerSeenMs;
            public Vector3 LastNetworkVelocity;
            public Vector2 LastNetworkFacing;
            public readonly List<RemotePawnSample> Samples = new List<RemotePawnSample>(10);
        }

        private readonly struct RemotePawnSample
        {
            public readonly float ReceivedRealtime;
            public readonly Vector3 World;
            public readonly bool Moving;
            public readonly int Sequence;
            public readonly long ServerSeenMs;
            public readonly Vector3 Velocity;
            public readonly Vector2 Facing;

            public RemotePawnSample(
                float receivedRealtime,
                Vector3 world,
                bool moving,
                int sequence,
                long serverSeenMs,
                Vector3 velocity,
                Vector2 facing)
            {
                ReceivedRealtime = receivedRealtime;
                World = world;
                Moving = moving;
                Sequence = sequence;
                ServerSeenMs = serverSeenMs;
                Velocity = velocity;
                Facing = facing;
            }
        }

        private readonly struct ObjectSnapshot
        {
            public readonly Vector2Int Cell;
            public readonly ObjectKind Kind;
            public readonly string Note;

            public ObjectSnapshot(Vector2Int cell, ObjectKind kind, string note)
            {
                Cell = cell;
                Kind = kind;
                Note = note;
            }
        }

        [Serializable]
        private sealed class WorldSaveData
        {
            public int version = 1;
            public string currentLocation;
            public List<LocationSaveData> locations = new List<LocationSaveData>();
        }

        [Serializable]
        private sealed class LocationSaveData
        {
            public string id;
            public List<ObjectSaveData> objects = new List<ObjectSaveData>();
        }

        [Serializable]
        private sealed class ObjectSaveData
        {
            public int x;
            public int y;
            public string kind;
            public string note;
        }

        [Serializable]
        private sealed class PlayerPresencePostData
        {
            public string clientId;
            public string displayName;
            public string dynasty;
            public int age;
            public string location;
            public float x;
            public float z;
            public int cellX;
            public int cellY;
            public bool moving;
            public long sentAtMs;
            public int sequence;
            public float velocityX;
            public float velocityZ;
            public float facingX;
            public float facingZ;
            public float hp;
            public float satiety;
            public float carryWeight;
        }

        [Serializable]
        private sealed class PlayerPresenceResponse
        {
            public bool success;
            public string serverTime;
            public long serverNowMs;
            public int serverTick;
            public List<PlayerPresenceData> players = new List<PlayerPresenceData>();
        }

        [Serializable]
        private sealed class PlayerPresenceData
        {
            public string clientId;
            public string displayName;
            public string dynasty;
            public int age;
            public string location;
            public float x;
            public float z;
            public int cellX;
            public int cellY;
            public bool moving;
            public long sentAtMs;
            public int sequence;
            public float velocityX;
            public float velocityZ;
            public float facingX;
            public float facingZ;
            public long serverSeenMs;
            public int serverTick;
            public float hp;
            public float satiety;
            public float carryWeight;
            public string lastSeenAt;
        }

        private sealed class PlayerSleeperProfile
        {
            public string ClientId;
            public string Nick;
            public string Dynasty;
            public int Age;
            public string Location;
            public int CellX;
            public int CellY;
            public float Hp;
            public float Satiety;
            public float CarryWeight;
            public int Berries;
            public string LastSeen;
        }

        private void Awake()
        {
            Application.runInBackground = true;
            SymbiozRuntimeLog.Initialize();
            SymbiozRuntimeLog.Write("LIFECYCLE", "Prototype awake. batchMode=" + Application.isBatchMode);
            useFishNetRealtime = IsFishNetRealtimeRequested();
            InitializeLocations();

            if (Application.isBatchMode)
            {
                EnsureFishNetWorldBridge();
                SymbiozRuntimeLog.Write("LIFECYCLE", "Headless server mode initialized visuals disabled.");
                enabled = false;
                return;
            }

            LoadPersistentWorld();
            EnsureLocationDefaults(currentLocation);
            EnsureSceneObjects();
            BuildBareWorld();
            BuildHud();
            SpawnArchitect(GetSpawnCellForCurrentLocation());
            localClientId = ResolveLocalClientId();
            localDisplayName = ResolveLocalDisplayName();
            localDynastyName = ResolveLocalDynastyName();
            localPlayerAge = ResolveLocalPlayerAge();
            LoadResourceInventory();
            RegisterLocalProfile();
            if (RemoveExistingSleeperForClient(localClientId))
                SavePersistentWorld();
            SelectCell(pawnCell);
            EnsureFishNetWorldBridge();
            TryStartSharedWorldDownload();
            if (!useFishNetRealtime)
                TryStartSharedPlayerPresence();
            else
                SymbiozRuntimeLog.Write("NETWORK", "FishNet realtime mode active. HTTP player presence disabled.");
            SymbiozRuntimeLog.Write("LIFECYCLE", "Prototype ready. save=" + GetPersistentWorldPath());
        }

        private void Update()
        {
            HandleInput();
            MovePawn();
            UpdateEstateInteriorVisibility();
            UpdateResourceWork();
            UpdatePawnBillboards();
            UpdatePawnNeeds();
            FollowPawn();
            UpdateCameraSmoothing();
            UpdateSelectedCellCursorPulse();
            UpdateCellContextPanelsPosition();
            UpdateWorldTimeAndLighting();
            if (!useFishNetRealtime)
                UpdateRemotePawns();
            UpdateMateriaGenerator();
            UpdateTreeChopProgress();
            FlushPendingPersistentWorld();
            UpdateHud();
        }

        private void EnsureSceneObjects()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                worldCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 22f;
            worldCamera.transform.rotation = IsoCameraRotation();
            worldCamera.transform.position = CameraPositionForFocus(Vector3.zero);
            worldCamera.backgroundColor = new Color(0.07f, 0.08f, 0.075f, 1f);
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            if (FindAnyObjectByType<AudioListener>() == null)
                worldCamera.gameObject.AddComponent<AudioListener>();
            cameraTargetPosition = worldCamera.transform.position;
            cameraTargetZoom = worldCamera.orthographicSize;
            LoadTileMaterials();

            worldSunLight = FindAnyObjectByType<Light>();
            if (worldSunLight == null)
            {
                var lightObject = new GameObject("Directional Light");
                worldSunLight = lightObject.AddComponent<Light>();
                worldSunLight.type = LightType.Directional;
                lightObject.transform.rotation = Quaternion.Euler(70f, -25f, 0f);
            }
            worldSunLight.name = "Symbioz World Sun";
            worldSunLight.type = LightType.Directional;
            UpdateWorldTimeAndLighting(true);

            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 33000;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Destroy(legacyModule);
        }

        private void BuildBareWorld()
        {
            placedObjects = placedObjectsByLocation[currentLocation];
            worldRoot = ResetChild("BareSoilWorld_275x275");
            gridRoot = ResetChild("CoordinateGrid");
            objectsRoot = ResetChild("ArchitectObjects");
            doorsRoot = ResetChild("LocationDoors");
            playersRoot = ResetChild("OnlineArchitects");

            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "OneTypeSoilPlane";
            soil.transform.SetParent(worldRoot, false);
            soil.transform.position = new Vector3(0f, -0.055f, 0f);
            soil.transform.localScale = new Vector3(GridSize * CellSize, 0.1f, GridSize * CellSize);
            if (groundTileMaterial != null)
                soil.GetComponent<Renderer>().sharedMaterial = groundTileMaterial;
            else
                SetRendererColor(soil, new Color(0.28f, 0.3f, 0.22f, 1f));

            CreateGroundCoverPatches();
            CreateNorthWorldBoundary();
            CreateGridLines();
            CreateCenterDoor();
            RebuildPlacedObjectsForCurrentLocation();
        }

        private void CreateNorthWorldBoundary()
        {
            float half = GridSize * CellSize * 0.5f;
            float wallZ = half + 0.78f;
            float wallHeight = 2.65f;
            float wallDepth = 0.72f;
            float gateOpeningWidth = 8.25f;
            float sideWidth = (GridSize * CellSize - gateOpeningWidth) * 0.5f;
            float sideCenterOffset = (gateOpeningWidth + sideWidth) * 0.5f;

            CreateBoundaryWallBlock("NorthBoundaryWall_Left", new Vector3(-sideCenterOffset, 0.84f, wallZ), new Vector3(sideWidth, wallHeight, wallDepth));
            CreateBoundaryWallBlock("NorthBoundaryWall_Right", new Vector3(sideCenterOffset, 0.84f, wallZ), new Vector3(sideWidth, wallHeight, wallDepth));
            CreateBoundaryWallBlock("NorthBoundaryGateTower_Left", new Vector3(-4.55f, 1.02f, wallZ - 0.06f), new Vector3(0.92f, 3.05f, 1.18f));
            CreateBoundaryWallBlock("NorthBoundaryGateTower_Right", new Vector3(4.55f, 1.02f, wallZ - 0.06f), new Vector3(0.92f, 3.05f, 1.18f));
            CreateBoundaryWallBlock("NorthBoundaryGateLintel", new Vector3(0f, 2.46f, wallZ - 0.08f), new Vector3(8.2f, 0.52f, 1.02f));
            CreateBoundaryCap("NorthBoundaryWallCap_Left", new Vector3(-sideCenterOffset, 2.24f, wallZ - 0.01f), new Vector3(sideWidth, 0.18f, wallDepth + 0.12f));
            CreateBoundaryCap("NorthBoundaryWallCap_Right", new Vector3(sideCenterOffset, 2.24f, wallZ - 0.01f), new Vector3(sideWidth, 0.18f, wallDepth + 0.12f));
            CreateBoundaryShadow("NorthBoundaryShadow", new Vector3(0f, 0.02f, half + 0.16f), new Vector3(GridSize * CellSize, 0.03f, 0.34f));
        }

        private void CreateBoundaryWallBlock(string name, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(worldRoot, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            SetRendererColor(block, new Color(0.38f, 0.17f, 0.12f, 1f));

            int brickLines = Mathf.Max(3, Mathf.FloorToInt(scale.y / 0.34f));
            for (int i = 0; i < brickLines; i++)
            {
                GameObject mortar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mortar.name = $"{name}_Mortar_{i:00}";
                mortar.transform.SetParent(worldRoot, false);
                float y = position.y - scale.y * 0.5f + 0.24f + i * 0.34f;
                mortar.transform.position = new Vector3(position.x, y, position.z - scale.z * 0.51f);
                mortar.transform.localScale = new Vector3(scale.x + 0.02f, 0.025f, 0.035f);
                SetRendererColor(mortar, new Color(0.71f, 0.56f, 0.45f, 1f));
            }
        }

        private void CreateBoundaryCap(string name, Vector3 position, Vector3 scale)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = name;
            cap.transform.SetParent(worldRoot, false);
            cap.transform.position = position;
            cap.transform.localScale = scale;
            SetRendererColor(cap, new Color(0.62f, 0.55f, 0.43f, 1f));
        }

        private void CreateBoundaryShadow(string name, Vector3 position, Vector3 scale)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadow.name = name;
            shadow.transform.SetParent(worldRoot, false);
            shadow.transform.position = position;
            shadow.transform.localScale = scale;
            SetRendererColor(shadow, new Color(0.03f, 0.035f, 0.025f, 1f));
        }

        private void CreateGroundCoverPatches()
        {
            float half = GridSize * CellSize * 0.5f;
            Color[] patchColors =
            {
                new Color(0.22f, 0.25f, 0.18f, 1f),
                new Color(0.33f, 0.31f, 0.21f, 1f),
                new Color(0.24f, 0.21f, 0.15f, 1f),
                new Color(0.30f, 0.34f, 0.24f, 1f)
            };

            for (int i = 0; i < 96; i++)
            {
                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = $"SoilCoverPatch_{i:000}";
                patch.transform.SetParent(worldRoot, false);

                float x = -half + 6f + ((i * 37) % (GridSize - 12));
                float z = -half + 6f + ((i * 61) % (GridSize - 12));
                float width = 2.5f + (i % 5) * 0.55f;
                float depth = 1.6f + (i % 7) * 0.38f;
                patch.transform.position = new Vector3(x, 0.004f, z);
                patch.transform.localRotation = Quaternion.Euler(0f, (i * 23) % 180, 0f);
                patch.transform.localScale = new Vector3(width, 0.012f, depth);
                SetRendererColor(patch, patchColors[i % patchColors.Length]);
            }
        }

        private void InitializeLocations()
        {
            placedObjectsByLocation.Clear();
            placedObjectsByLocation[LocationId.FirstSoil] = new Dictionary<Vector2Int, PlacedObject>();
            placedObjectsByLocation[LocationId.ReturnYard] = new Dictionary<Vector2Int, PlacedObject>();
            placedObjectsByLocation[LocationId.CityGate] = new Dictionary<Vector2Int, PlacedObject>();
            placedObjectsByLocation[LocationId.CityCenter] = new Dictionary<Vector2Int, PlacedObject>();
            currentLocation = LocationId.FirstSoil;
            placedObjects = placedObjectsByLocation[currentLocation];
        }

        private void EnsureFishNetWorldBridge()
        {
            fishNetWorldBridge = GetComponent<SymbiozFishNetWorldBridge>();
            if (fishNetWorldBridge == null)
                fishNetWorldBridge = gameObject.AddComponent<SymbiozFishNetWorldBridge>();

            fishNetWorldBridge.Initialize(this);
        }

        private void LoadTileMaterials()
        {
            roadTileMaterial = CreateTransparentMaterial("SymbiozTiles/road-tiles-v2");
            wallTileMaterial = CreateTransparentMaterial("SymbiozTiles/wall-tiles-v2");
            barrierTileMaterial = CreateAtlasMaterial("SymbiozTiles/barrier-tiles-v1");
            shelterTileMaterial = CreateTransparentMaterial("SymbiozTiles/shelter-tiles-v1");
            communityStorageMaterial = CreateTransparentMaterial("SymbiozTiles/community-storage-green-roof-6x4");
            slumTradeCenterMaterial = CreateTransparentMaterial("SymbiozTiles/slum-trade-center-6x4");
            stoneQuarryMaterial = CreateTransparentMaterial("SymbiozTiles/stone-quarry-worksite-iso-7x7");
            sawmillMaterial = CreateTransparentMaterial("SymbiozTiles/sawmill-lumberyard-worksite-iso-7x7");
            smallHouseExteriorMaterial = CreateTransparentMaterial("SymbiozTiles/Buildings/small-house-exterior");
            resourceTreeMaterials = new Material[TreeVariantCount];
            for (int i = 0; i < resourceTreeMaterials.Length; i++)
                resourceTreeMaterials[i] = CreateTransparentMaterial($"SymbiozTiles/resource-tree-{i}");
            portalMaterial = CreateTransparentMaterial("SymbiozTiles/portal-technomagic-v1");
            pawnSpriteMaterial = CreateTransparentMaterial("SymbiozTiles/architect-iso-template-v2");
            pawnIdleMaterial = CreateTransparentMaterial("SymbiozTiles/architect-iso-idle-v1");
            if (roadTileMaterial != null)
                roadTileMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest - 25;
            if (wallTileMaterial != null)
                wallTileMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 40;
            if (portalMaterial != null)
                portalMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest + 30;
            if (pawnSpriteMaterial != null)
                pawnSpriteMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 40;
            if (pawnIdleMaterial != null)
                pawnIdleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 40;
            if (stoneQuarryMaterial != null)
                stoneQuarryMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 20;
            if (sawmillMaterial != null)
                sawmillMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 20;
            if (smallHouseExteriorMaterial != null)
                smallHouseExteriorMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 75;
            if (resourceTreeMaterials != null)
            {
                for (int i = 0; i < resourceTreeMaterials.Length; i++)
                {
                    if (resourceTreeMaterials[i] != null)
                        resourceTreeMaterials[i].renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 55;
                }
            }
            groundTileMaterial = CreateTiledMaterial("SymbiozTiles/ground-soil-living-v1");
        }

        private static Material CreateAtlasMaterial(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));
            material.mainTexture = texture;
            return material;
        }

        private static Material CreateTransparentMaterial(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Shader shader = Shader.Find("Unlit/Transparent Cutout") ??
                            Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Unlit/Texture");
            Material material = new Material(shader);
            material.mainTexture = texture;
            material.color = Color.white;
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", 0.08f);
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            return material;
        }

        private static Material CreateTiledMaterial(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));
            material.mainTexture = texture;
            material.mainTextureScale = new Vector2(GridSize / 8f, GridSize / 8f);
            return material;
        }

        private Transform ResetChild(string childName)
        {
            Transform existing = transform.Find(childName);
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            var obj = new GameObject(childName);
            obj.transform.SetParent(transform, false);
            return obj.transform;
        }

        private void CreateGridLines()
        {
            Color minor = new Color(0.09f, 0.12f, 0.095f, 0.55f);
            Color major = new Color(0.14f, 0.19f, 0.15f, 0.85f);
            float half = GridSize * CellSize * 0.5f;

            for (int i = 0; i <= GridSize; i++)
            {
                float p = -half + i * CellSize;
                bool isMajor = i % 25 == 0;
                CreateLine($"Grid_X_{i:000}", new Vector3(p, 0.015f, -half), new Vector3(p, 0.015f, half), isMajor ? major : minor, isMajor ? 0.035f : 0.012f);
                CreateLine($"Grid_Y_{i:000}", new Vector3(-half, 0.016f, p), new Vector3(half, 0.016f, p), isMajor ? major : minor, isMajor ? 0.035f : 0.012f);
            }
        }

        private void CreateLine(string lineName, Vector3 from, Vector3 to, Color color, float width)
        {
            var obj = new GameObject(lineName);
            obj.transform.SetParent(gridRoot, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = width;
            line.endWidth = width;
            line.useWorldSpace = false;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
        }

        private void SpawnArchitect(Vector2Int startCell)
        {
            pawnCell = ClampCell(startCell);
            targetCell = pawnCell;

            pawn = new GameObject("Architect_001_BlackYang");
            pawn.name = "Architect_001_BlackYang";
            pawn.transform.SetParent(worldRoot, false);
            pawn.transform.position = CellToWorld(pawnCell) + new Vector3(0f, PawnGroundYOffset, 0f);
            cameraTargetPosition = ClampCamera(CameraPositionForFocus(pawn.transform.position));
            cameraMoveVelocity = Vector3.zero;

            pawnVisualRoot = new GameObject("ArchitectVisual_2CellsTall").transform;
            pawnVisualRoot.SetParent(pawn.transform, false);
            CreatePawnSprite(pawnVisualRoot, "ArchitectSprite", 0, false);

            cursor = CreateSelectedCellCursor();
        }

        private GameObject CreateSelectedCellCursor()
        {
            GameObject root = new GameObject("SelectedCellCursor_Frame");
            root.transform.SetParent(worldRoot, false);
            cursorFrameLines = new LineRenderer[4];
            cursorFrameLines[0] = CreateCursorFrameLine(root.transform, "North", new Vector3(-0.47f, 0.044f, 0.47f), new Vector3(0.47f, 0.044f, 0.47f));
            cursorFrameLines[1] = CreateCursorFrameLine(root.transform, "South", new Vector3(-0.47f, 0.044f, -0.47f), new Vector3(0.47f, 0.044f, -0.47f));
            cursorFrameLines[2] = CreateCursorFrameLine(root.transform, "West", new Vector3(-0.47f, 0.044f, -0.47f), new Vector3(-0.47f, 0.044f, 0.47f));
            cursorFrameLines[3] = CreateCursorFrameLine(root.transform, "East", new Vector3(0.47f, 0.044f, -0.47f), new Vector3(0.47f, 0.044f, 0.47f));
            return root;
        }

        private static LineRenderer CreateCursorFrameLine(Transform parent, string name, Vector3 from, Vector3 to)
        {
            GameObject obj = new GameObject("CursorFrame_" + name);
            obj.transform.SetParent(parent, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.055f;
            line.endWidth = 0.055f;
            line.useWorldSpace = false;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.55f, 0.9f, 1f, 0.82f);
            line.endColor = new Color(0.55f, 0.9f, 1f, 0.82f);
            line.sortingOrder = 190;
            return line;
        }

        private void CreatePawnVisualPart(string partName, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(pawnVisualRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            SetRendererColor(part, color);
        }

        private void CreatePawnShadow(Transform parent, string name)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = name;
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, 0.012f, 0.02f);
            shadow.transform.localScale = new Vector3(0.34f, 0.004f, 0.18f);
            SetRendererColor(shadow, new Color(0.1f, 0.12f, 0.09f, 1f));
        }

        private GameObject CreatePawnSprite(Transform parent, string name, int viewIndex, bool flipX)
        {
            GameObject sprite = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            sprite.transform.SetParent(parent, false);
            sprite.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            Mesh mesh = CreatePawnQuadMesh(name + "_Mesh", ResolvePawnUv(viewIndex, 0, flipX));
            pawnSpriteMeshFilter = sprite.GetComponent<MeshFilter>();
            pawnSpriteMeshFilter.sharedMesh = mesh;
            pawnViewIndex = viewIndex;
            pawnViewFrame = 0;
            pawnViewFlipX = flipX;
            pawnViewUsesIdleMaterial = true;
            pawnLastFacingView = viewIndex;
            pawnLastFacingFlipX = flipX;
            pawnAnimationFrame = 0;
            pawnAnimationTimer = 0f;
            pawnSpriteRenderer = sprite.GetComponent<MeshRenderer>();
            if (pawnIdleMaterial != null)
                pawnSpriteRenderer.sharedMaterial = pawnIdleMaterial;
            else if (pawnSpriteMaterial != null)
                pawnSpriteRenderer.sharedMaterial = pawnSpriteMaterial;
            else
                pawnSpriteRenderer.sharedMaterial = CreateFallbackPawnMaterial();
            RefreshPawnRenderOrder();
            return sprite;
        }

        private void UpdatePawnFacing(Vector2 direction, bool moving)
        {
            if (pawnSpriteMeshFilter == null)
                return;

            int nextView = pawnViewIndex;
            bool nextFlip = false;
            int nextFrame = 0;
            bool nextUsesIdleMaterial = !(moving && direction.sqrMagnitude > 0.001f);

            if (!nextUsesIdleMaterial)
            {
                ResolveScreenFacingView(direction, out nextView, out nextFlip);

                pawnLastFacingView = nextView;
                pawnLastFacingFlipX = nextFlip;

                pawnAnimationTimer += Time.deltaTime;
                if (pawnAnimationTimer >= PawnWalkFrameSeconds)
                {
                    pawnAnimationTimer -= PawnWalkFrameSeconds;
                    pawnAnimationFrame = (pawnAnimationFrame + 1) % PawnWalkColumns;
                }

                nextFrame = pawnAnimationFrame;
            }
            else
            {
                nextView = ResolveIdleViewForFacing(pawnLastFacingView);
                nextFlip = false;
                pawnAnimationFrame = 0;
                pawnAnimationTimer = 0f;
            }

            ApplyPawnAnimationMaterial(nextUsesIdleMaterial);

            if (nextView == pawnViewIndex && nextFrame == pawnViewFrame && nextFlip == pawnViewFlipX && nextUsesIdleMaterial == pawnViewUsesIdleMaterial)
                return;

            pawnViewIndex = nextView;
            pawnViewFrame = nextFrame;
            pawnViewFlipX = nextFlip;
            pawnViewUsesIdleMaterial = nextUsesIdleMaterial;
            pawnSpriteMeshFilter.sharedMesh = CreatePawnQuadMesh("ArchitectSprite_Mesh_" + nextView + "_" + nextFrame + "_" + nextFlip, ResolvePawnUv(nextView, nextFrame, nextFlip));
        }

        private void ApplyPawnAnimationMaterial(bool idle)
        {
            if (pawnSpriteRenderer == null)
                return;

            Material material = idle && pawnIdleMaterial != null
                ? pawnIdleMaterial
                : pawnSpriteMaterial != null
                    ? pawnSpriteMaterial
                    : CreateFallbackPawnMaterial();
            if (pawnSpriteRenderer.sharedMaterial != material)
                pawnSpriteRenderer.sharedMaterial = material;
        }

        private static int ResolveIdleViewForFacing(int facingView)
        {
            return Mathf.Clamp(facingView, 0, PawnWalkRows - 1);
        }

        private static void ResolveScreenFacingView(Vector2 worldDirection, out int view, out bool flipX)
        {
            flipX = false;
            Vector3 cameraRight = IsoCameraRotation() * Vector3.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
            Vector3 cameraUp = IsoCameraRotation() * Vector3.up;
            cameraUp.y = 0f;
            cameraUp.Normalize();

            Vector3 direction = new Vector3(worldDirection.x, 0f, worldDirection.y);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                view = 0;
                return;
            }

            direction.Normalize();
            float screenX = Vector3.Dot(direction, cameraRight);
            float screenY = Vector3.Dot(direction, cameraUp);
            float angle = Mathf.Atan2(screenX, -screenY) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;
            int screenSector = Mathf.RoundToInt(angle / 45f) % PawnWalkRows;
            view = screenSector switch
            {
                0 => 0,
                1 => 7,
                2 => 6,
                3 => 5,
                4 => 4,
                5 => 3,
                6 => 2,
                7 => 1,
                _ => 0
            };
        }

        private static Material CreateFallbackPawnMaterial()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));
            material.color = new Color(0.24f, 0.5f, 0.82f, 1f);
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", 0);
            return material;
        }

        private void CreateCenterDoor()
        {
            List<Vector2Int> doorCells = GetDoorCellsForLocation(currentLocation);
            centerDoor = null;
            for (int i = 0; i < doorCells.Count; i++)
                CreateDoorVisual(doorCells[i], i);
        }

        private void CreateDoorVisual(Vector2Int doorCell, int index)
        {
            LocationId targetLocation = GetTransitionTargetForCell(currentLocation, doorCell);
            var root = new GameObject($"Portal_{GetLocationName(currentLocation)}_{GetLocationName(targetLocation)}_{index:00}");
            root.transform.SetParent(doorsRoot, false);
            root.transform.position = CellToWorld(doorCell);

            GameObject portal = new GameObject("TechnoMagicPortal_3x3", typeof(MeshFilter), typeof(MeshRenderer));
            portal.transform.SetParent(root.transform, false);
            portal.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            portal.GetComponent<MeshFilter>().sharedMesh = CreateTexturedQuadMesh(
                "TechnoMagicPortal_3x3_Mesh",
                IsCityGateLocation(currentLocation) ? 5.4f : 3.18f,
                IsCityGateLocation(currentLocation) ? 2.6f : 3.18f,
                RectToUv(new Rect(0f, 0f, 1f, 1f), false));
            MeshRenderer renderer = portal.GetComponent<MeshRenderer>();
            if (portalMaterial != null)
                renderer.sharedMaterial = portalMaterial;
            else
                renderer.sharedMaterial = CreateFallbackPortalMaterial();
            renderer.sortingOrder = 80;

            if (IsCityGateLocation(currentLocation))
                CreateCityGateFrame(root.transform);

            if (centerDoor == null)
                centerDoor = root;
        }

        private void CreateCityGateFrame(Transform parent)
        {
            CreateGatePiece(parent, "GateLeftTower", new Vector3(-2.35f, 0.16f, 0f), new Vector3(0.62f, 0.28f, 2.35f), new Color(0.33f, 0.31f, 0.28f, 1f));
            CreateGatePiece(parent, "GateRightTower", new Vector3(2.35f, 0.16f, 0f), new Vector3(0.62f, 0.28f, 2.35f), new Color(0.33f, 0.31f, 0.28f, 1f));
            CreateGatePiece(parent, "GateLintel", new Vector3(0f, 0.18f, 0.92f), new Vector3(5.4f, 0.22f, 0.38f), new Color(0.42f, 0.37f, 0.31f, 1f));
            CreateGatePiece(parent, "GateThreshold", new Vector3(0f, 0.075f, -0.78f), new Vector3(4.25f, 0.08f, 0.34f), new Color(0.62f, 0.55f, 0.42f, 1f));
        }

        private static void CreateGatePiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            SetRendererColor(piece, color);
        }

        private static Material CreateFallbackPortalMaterial()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));
            material.color = new Color(0.14f, 0.68f, 1f, 1f);
            return material;
        }

        private void BuildHud()
        {
            RectTransform root = canvas.GetComponent<RectTransform>();

            RectTransform topBar = CreatePanel(root, "HUD_TopBar", Anchor.TopStretch, new Vector2(24f, -22f), new Vector2(-24f, 112f), new Color(0.05f, 0.065f, 0.055f, 0.9f));
            TextMeshProUGUI title = CreateText(topBar, "Title", "Dynasty: Legacy - Symbioz", 36, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(0.45f, 1f);
            title.rectTransform.offsetMin = new Vector2(170f, 6f);
            title.rectTransform.offsetMax = new Vector2(0f, -6f);

            statsText = CreateText(topBar, "Stats", string.Empty, 23, FontStyles.Bold, TextAlignmentOptions.Right);
            statsText.rectTransform.anchorMin = new Vector2(0.42f, 0f);
            statsText.rectTransform.anchorMax = new Vector2(0.86f, 1f);
            statsText.rectTransform.offsetMin = new Vector2(12f, 6f);
            statsText.rectTransform.offsetMax = new Vector2(-24f, -6f);

            Button mainButton = CreateHudButton(topBar, "Btn_ReturnMain", "MAIN", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(128f, 58f), 22f, ReturnToMain);
            mainButton.transform.SetAsLastSibling();

            RectTransform side = CreatePanel(root, "HUD_PawnPanel", Anchor.RightMiddle, new Vector2(-24f, 0f), new Vector2(390f, 392f), new Color(0.05f, 0.065f, 0.055f, 0.9f));
            side.gameObject.SetActive(false);
            detailText = CreateText(side, "Detail", string.Empty, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            detailText.rectTransform.anchorMin = Vector2.zero;
            detailText.rectTransform.anchorMax = Vector2.one;
            detailText.rectTransform.offsetMin = new Vector2(22f, 18f);
            detailText.rectTransform.offsetMax = new Vector2(-22f, -18f);

            RectTransform bottom = CreatePanel(root, "HUD_CommandStrip", Anchor.BottomCenter, new Vector2(0f, 24f), new Vector2(980f, 78f), new Color(0.05f, 0.065f, 0.055f, 0.9f));
            bottom.gameObject.SetActive(false);
            commandText = CreateText(bottom, "Command", string.Empty, 21, FontStyles.Bold, TextAlignmentOptions.Center);
            commandText.rectTransform.anchorMin = Vector2.zero;
            commandText.rectTransform.anchorMax = Vector2.one;
            commandText.rectTransform.offsetMin = new Vector2(18f, 8f);
            commandText.rectTransform.offsetMax = new Vector2(-18f, -8f);

            RectTransform selectionHud = CreatePanel(root, "HUD_SelectionMini", Anchor.BottomLeft, new Vector2(24f, 24f), new Vector2(330f, 112f), new Color(0.045f, 0.055f, 0.05f, 0.92f));
            selectionText = CreateText(selectionHud, "SelectionText", string.Empty, 19, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            selectionText.rectTransform.anchorMin = Vector2.zero;
            selectionText.rectTransform.anchorMax = Vector2.one;
            selectionText.rectTransform.offsetMin = new Vector2(16f, 12f);
            selectionText.rectTransform.offsetMax = new Vector2(-16f, -12f);

            RectTransform logHud = CreatePanel(root, "HUD_RuntimeLog", Anchor.TopLeft, new Vector2(24f, -150f), new Vector2(520f, 184f), new Color(0.025f, 0.032f, 0.03f, 0.82f));
            logHud.gameObject.SetActive(false);
            runtimeLogText = CreateText(logHud, "RuntimeLogText", string.Empty, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            runtimeLogText.rectTransform.anchorMin = Vector2.zero;
            runtimeLogText.rectTransform.anchorMax = Vector2.one;
            runtimeLogText.rectTransform.offsetMin = new Vector2(14f, 10f);
            runtimeLogText.rectTransform.offsetMax = new Vector2(-14f, -10f);

            BuildNotePanel(root);
            BuildTransitionPanel(root);
            BuildConstructionPanel(root);
            BuildCellActionPanels(root);
            BuildMateriaPanel(root);
            BuildPlayerStatusPanel(root);
        }

        private RectTransform CreatePanel(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            obj.GetComponent<Image>().color = color;
            return rect;
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, int fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = new Color(0.93f, 0.96f, 0.91f, 1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private Button CreateHudButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float fontSize, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.12f, 0.19f, 0.15f, 0.95f);

            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);

            TextMeshProUGUI text = CreateText(rect, "Label", label, (int)fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private void BuildNotePanel(RectTransform root)
        {
            notePanel = CreatePanel(root, "HUD_NoteDialog", Anchor.Center, Vector2.zero, new Vector2(620f, 260f), new Color(0.045f, 0.052f, 0.047f, 0.96f));
            notePanel.gameObject.SetActive(false);

            TextMeshProUGUI title = CreateText(notePanel, "Title", "Sign", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 48f);

            GameObject inputObject = new GameObject("NoteInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.SetParent(notePanel, false);
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0f, 14f);
            inputRect.sizeDelta = new Vector2(540f, 76f);
            inputObject.GetComponent<Image>().color = new Color(0.86f, 0.78f, 0.61f, 1f);

            TextMeshProUGUI text = CreateText(inputRect, "Text", string.Empty, 25, FontStyles.Bold, TextAlignmentOptions.Left);
            text.color = new Color(0.1f, 0.08f, 0.045f, 1f);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(18f, 8f);
            text.rectTransform.offsetMax = new Vector2(-18f, -8f);

            TextMeshProUGUI placeholder = CreateText(inputRect, "Placeholder", "Note or command: wall, road, repair, plot, delete", 22, FontStyles.Italic, TextAlignmentOptions.Left);
            placeholder.color = new Color(0.33f, 0.27f, 0.16f, 0.72f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(18f, 8f);
            placeholder.rectTransform.offsetMax = new Vector2(-18f, -8f);

            noteInput = inputObject.GetComponent<TMP_InputField>();
            noteInput.textComponent = text;
            noteInput.placeholder = placeholder;
            noteInput.characterLimit = MaxNoteLength;
            noteInput.lineType = TMP_InputField.LineType.SingleLine;
            noteInput.onSubmit.AddListener(_ => SaveNoteDialog());

            CreateHudButton(notePanel, "Save", "OK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-78f, 44f), new Vector2(130f, 58f), 22f, SaveNoteDialog);
            CreateHudButton(notePanel, "Cancel", "X", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(86f, 44f), new Vector2(92f, 58f), 22f, CancelNoteDialog);
        }

        private void BuildTransitionPanel(RectTransform root)
        {
            transitionPanel = CreatePanel(root, "HUD_TransitionConfirm", Anchor.Center, Vector2.zero, new Vector2(700f, 260f), new Color(0.035f, 0.045f, 0.04f, 0.97f));
            transitionPanel.gameObject.SetActive(false);

            TextMeshProUGUI title = CreateText(transitionPanel, "Title", "Cluster transfer", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 48f);

            transitionPromptText = CreateText(transitionPanel, "Prompt", string.Empty, 26, FontStyles.Bold, TextAlignmentOptions.Center);
            transitionPromptText.rectTransform.anchorMin = new Vector2(0f, 0f);
            transitionPromptText.rectTransform.anchorMax = new Vector2(1f, 1f);
            transitionPromptText.rectTransform.offsetMin = new Vector2(48f, 88f);
            transitionPromptText.rectTransform.offsetMax = new Vector2(-48f, -72f);

            CreateHudButton(transitionPanel, "Yes", "Yes", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-92f, 44f), new Vector2(150f, 58f), 22f, ConfirmTransition);
            CreateHudButton(transitionPanel, "No", "No", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(92f, 44f), new Vector2(150f, 58f), 22f, CancelTransitionPrompt);
        }

        private void BuildConstructionPanel(RectTransform root)
        {
            Button buildButton = CreateHudButton(root, "HUD_BuildTab", "Build", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-108f, -88f), new Vector2(172f, 58f), 22f, ToggleBuildPalette);
            buildButton.transform.SetAsLastSibling();

            buildPalettePanel = CreatePanel(root, "HUD_BuildPalette", Anchor.TopRight, new Vector2(-24f, -154f), new Vector2(620f, 430f), new Color(0.035f, 0.045f, 0.04f, 0.96f));
            buildPalettePanel.gameObject.SetActive(false);

            TextMeshProUGUI title = CreateText(buildPalettePanel, "Title", "Build craft", 28, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            title.rectTransform.sizeDelta = new Vector2(-32f, 42f);
            title.rectTransform.offsetMin = new Vector2(20f, title.rectTransform.offsetMin.y);
            title.rectTransform.offsetMax = new Vector2(-20f, title.rectTransform.offsetMax.y);

            CreateBuildCategoryButton("Houses", BuildCategory.Houses, -60f);
            CreateBuildCategoryButton("Roads", BuildCategory.Roads, -112f);
            CreateBuildCategoryButton("Utility", BuildCategory.Utility, -164f);
            CreateBuildCategoryButton("Resources", BuildCategory.Resources, -216f);

            buildSelectionText = CreateText(buildPalettePanel, "BuildSelection", "Choose a piece, then click land to place.", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            buildSelectionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            buildSelectionText.rectTransform.anchorMax = new Vector2(1f, 0f);
            buildSelectionText.rectTransform.pivot = new Vector2(0.5f, 0f);
            buildSelectionText.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            buildSelectionText.rectTransform.sizeDelta = new Vector2(-32f, 34f);
            buildSelectionText.rectTransform.offsetMin = new Vector2(154f, buildSelectionText.rectTransform.offsetMin.y);

            RebuildBuildPaletteItems();

            buildConfirmPanel = CreatePanel(root, "HUD_BuildConfirm", Anchor.TopRight, new Vector2(-24f, -596f), new Vector2(360f, 76f), new Color(0.035f, 0.045f, 0.04f, 0.96f));
            buildConfirmPanel.gameObject.SetActive(false);
            CreateHudButton(buildConfirmPanel, "Confirm", "OK", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(110f, 52f), 22f, ConfirmPendingBuild);
            CreateHudButton(buildConfirmPanel, "Cancel", "X", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-198f, 0f), new Vector2(92f, 52f), 22f, CancelPendingBuild);
        }

        private void CreateBuildCategoryButton(string label, BuildCategory category, float y)
        {
            CreateHudButton(buildPalettePanel, "BuildCategory_" + category, label, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, y), new Vector2(124f, 42f), 17f, () => SelectBuildCategory(category));
        }

        private void SelectBuildCategory(BuildCategory category)
        {
            activeBuildCategory = category;
            RebuildBuildPaletteItems();
        }

        private void RebuildBuildPaletteItems()
        {
            for (int i = 0; i < buildPaletteItems.Count; i++)
            {
                if (buildPaletteItems[i] != null)
                    Destroy(buildPaletteItems[i]);
            }

            buildPaletteItems.Clear();

            switch (activeBuildCategory)
            {
                case BuildCategory.Houses:
                    AddBuildPaletteItem("House", ObjectKind.SmallHouse, new Vector2(70f, -82f), new Color(0.44f, 0.5f, 0.38f, 1f), "7x7");
                    AddBuildPaletteItem("Tower", ObjectKind.StoneTower, new Vector2(220f, -82f), new Color(0.42f, 0.43f, 0.44f, 1f), "9x9");
                    AddBuildPaletteItem("Keep", ObjectKind.StoneKeep, new Vector2(70f, -204f), new Color(0.32f, 0.34f, 0.36f, 1f), "14x14");
                    AddBuildPaletteItem("Castle", ObjectKind.Castle, new Vector2(220f, -204f), new Color(0.36f, 0.37f, 0.4f, 1f), "19x19");
                    break;
                case BuildCategory.Roads:
                    AddBuildPaletteItem("Road", ObjectKind.Road, new Vector2(70f, -82f), new Color(0.48f, 0.42f, 0.29f, 1f), "1x1");
                    AddBuildPaletteItem("Wall", ObjectKind.Wall, new Vector2(220f, -82f), new Color(0.52f, 0.2f, 0.14f, 1f), "1x1");
                    AddBuildPaletteItem("Repair", ObjectKind.Repair, new Vector2(70f, -204f), new Color(0.58f, 0.38f, 0.2f, 1f), "1x1");
                    AddBuildPaletteItem("Plot", ObjectKind.BuildPlot, new Vector2(220f, -204f), new Color(0.89f, 0.78f, 0.38f, 1f), "1x1");
                    break;
                case BuildCategory.Utility:
                    AddBuildPaletteItem("Sign", ObjectKind.Sign, new Vector2(70f, -82f), new Color(0.82f, 0.67f, 0.42f, 1f), "1x1");
                    AddBuildPaletteItem("Sleep", ObjectKind.SleepingBag, new Vector2(220f, -82f), new Color(0.34f, 0.42f, 0.22f, 1f), "1x2");
                    AddBuildPaletteItem("Storage", ObjectKind.CommunityStorage, new Vector2(70f, -204f), new Color(0.25f, 0.47f, 0.24f, 1f), "6x4");
                    AddBuildPaletteItem("Trade", ObjectKind.SlumTradeCenter, new Vector2(220f, -204f), new Color(0.72f, 0.56f, 0.34f, 1f), "6x4");
                    break;
                case BuildCategory.Resources:
                    AddBuildPaletteItem("Quarry", ObjectKind.StoneQuarry, new Vector2(70f, -82f), new Color(0.44f, 0.44f, 0.4f, 1f), "7x7");
                    AddBuildPaletteItem("Sawmill", ObjectKind.Sawmill, new Vector2(220f, -82f), new Color(0.48f, 0.31f, 0.14f, 1f), "7x7");
                    break;
            }

            if (buildSelectionText != null && !hasPendingBuildKind)
                buildSelectionText.text = $"{activeBuildCategory}: choose craft, then click land.";
        }

        private void AddBuildPaletteItem(string label, ObjectKind kind, Vector2 position, Color swatchColor, string sizeLabel)
        {
            Button button = CreateBuildItemButton(buildPalettePanel, label, kind, position, swatchColor, sizeLabel);
            if (button != null)
                buildPaletteItems.Add(button.gameObject);
        }

        private void BuildCellActionPanels(RectTransform root)
        {
            cellActionPanel = CreatePanel(root, "HUD_CellActionMenu", Anchor.Center, Vector2.zero, new Vector2(230f, 250f), new Color(0.028f, 0.038f, 0.034f, 0.96f));
            cellActionPanel.gameObject.SetActive(false);

            TextMeshProUGUI title = CreateText(cellActionPanel, "Title", "Action", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            title.rectTransform.sizeDelta = new Vector2(-24f, 34f);

            CreateHudButton(cellActionPanel, "ActionBuild", "Construct", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(184f, 42f), 18f, ShowBuildCarouselNearSelectedCell);
            CreateHudButton(cellActionPanel, "ActionMove", "Go here", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(184f, 42f), 18f, MoveToSelectedCellFromMenu);
            CreateHudButton(cellActionPanel, "ActionTent", "Deploy tent", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(184f, 42f), 17f, () => SelectContextBuildItem(ObjectKind.SleepingBag));
            CreateHudButton(cellActionPanel, "ActionStatus", "Status", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(184f, 42f), 18f, OpenSelectedStatusFromMenu);

            cellBuildCarouselPanel = CreatePanel(root, "HUD_CellBuildCarousel", Anchor.Center, Vector2.zero, new Vector2(210f, 670f), new Color(0.028f, 0.038f, 0.034f, 0.97f));
            cellBuildCarouselPanel.gameObject.SetActive(false);

            TextMeshProUGUI buildTitle = CreateText(cellBuildCarouselPanel, "Title", "Construct", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            buildTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            buildTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            buildTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            buildTitle.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            buildTitle.rectTransform.sizeDelta = new Vector2(-24f, 34f);

            CreateContextBuildButton("Road", ObjectKind.Road, -60f);
            CreateContextBuildButton("Wall", ObjectKind.Wall, -108f);
            CreateContextBuildButton("Sign", ObjectKind.Sign, -156f);
            CreateContextBuildButton("Repair", ObjectKind.Repair, -204f);
            CreateContextBuildButton("Plot", ObjectKind.BuildPlot, -252f);
            CreateContextBuildButton("Sleep", ObjectKind.SleepingBag, -300f);
            CreateContextBuildButton("Storage", ObjectKind.CommunityStorage, -348f);
            CreateContextBuildButton("Trade", ObjectKind.SlumTradeCenter, -396f);
            CreateContextBuildButton("House", ObjectKind.SmallHouse, -444f);
            CreateContextBuildButton("Tower", ObjectKind.StoneTower, -492f);
            CreateContextBuildButton("Keep", ObjectKind.StoneKeep, -540f);
            CreateContextBuildButton("Castle", ObjectKind.Castle, -588f);
            CreateHudButton(cellBuildCarouselPanel, "BuildClose", "X", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -636f), new Vector2(160f, 38f), 18f, HideCellContextMenus);

            objectInteractionPanel = CreatePanel(root, "HUD_ObjectInteractionMenu", Anchor.Center, Vector2.zero, new Vector2(246f, 252f), new Color(0.028f, 0.038f, 0.034f, 0.97f));
            objectInteractionPanel.gameObject.SetActive(false);
            TextMeshProUGUI objectTitle = CreateText(objectInteractionPanel, "Title", "Object", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            objectTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            objectTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            objectTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            objectTitle.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            objectTitle.rectTransform.sizeDelta = new Vector2(-24f, 34f);
            Button objectUseButton = CreateHudButton(objectInteractionPanel, "ObjectUse", "Use", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(190f, 42f), 18f, UseSelectedObjectFromMenu);
            objectUseButtonText = objectUseButton != null ? objectUseButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            CreateHudButton(objectInteractionPanel, "ObjectProfile", "Profile", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(190f, 42f), 18f, ShowObjectProfileCardForSelectedCell);
            CreateHudButton(objectInteractionPanel, "ObjectDelete", "Delete", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(190f, 42f), 18f, DeleteSelectedObjectFromMenu);
            CreateHudButton(objectInteractionPanel, "ObjectClose", "Close", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(190f, 42f), 18f, HideCellContextMenus);

            objectProfilePanel = CreatePanel(root, "HUD_ObjectProfileCard", Anchor.Center, Vector2.zero, new Vector2(350f, 228f), new Color(0.025f, 0.034f, 0.031f, 0.97f));
            objectProfilePanel.gameObject.SetActive(false);
            objectProfileText = CreateText(objectProfilePanel, "ObjectProfileText", string.Empty, 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            objectProfileText.rectTransform.anchorMin = Vector2.zero;
            objectProfileText.rectTransform.anchorMax = Vector2.one;
            objectProfileText.rectTransform.offsetMin = new Vector2(18f, 14f);
            objectProfileText.rectTransform.offsetMax = new Vector2(-18f, -14f);
        }

        private void CreateContextBuildButton(string label, ObjectKind kind, float y)
        {
            CreateHudButton(cellBuildCarouselPanel, "CellBuild_" + kind, label, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(166f, 40f), 17f, () => SelectContextBuildItem(kind));
        }

        private void BuildMateriaPanel(RectTransform root)
        {
            Button materiaButton = CreateHudButton(root, "HUD_MateriaGenerate", "Materia", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(438f, 84f), new Vector2(160f, 54f), 21f, GenerateMateriaForCurrentLocation);
            materiaButton.transform.SetAsLastSibling();

            materiaPanel = CreatePanel(root, "HUD_MateriaPanel", Anchor.BottomLeft, new Vector2(24f, 148f), new Vector2(430f, 150f), new Color(0.045f, 0.055f, 0.05f, 0.93f));
            materiaText = CreateText(materiaPanel, "MateriaText", string.Empty, 18, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            materiaText.rectTransform.anchorMin = Vector2.zero;
            materiaText.rectTransform.anchorMax = Vector2.one;
            materiaText.rectTransform.offsetMin = new Vector2(16f, 58f);
            materiaText.rectTransform.offsetMax = new Vector2(-16f, -10f);

            CreateHudButton(materiaPanel, "Eat", "Eat", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(78f, 28f), new Vector2(110f, 48f), 20f, EatSelectedBerryBush);
            CreateHudButton(materiaPanel, "Gather", "Gather", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(206f, 28f), new Vector2(126f, 48f), 20f, GatherSelectedBerryBush);
            CreateHudButton(materiaPanel, "Spawn", "Generate", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(350f, 28f), new Vector2(132f, 48f), 18f, GenerateMateriaForCurrentLocation);
        }

        private void BuildPlayerStatusPanel(RectTransform root)
        {
            playerStatusPanel = CreatePanel(root, "HUD_PlayerSleeperStatus", Anchor.Center, Vector2.zero, new Vector2(1420f, 820f), new Color(0.025f, 0.032f, 0.03f, 0.985f));
            playerStatusPanel.gameObject.SetActive(false);

            TextMeshProUGUI title = CreateText(playerStatusPanel, "Title", "Player status", 36, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            title.rectTransform.sizeDelta = new Vector2(-96f, 58f);
            title.rectTransform.offsetMin = new Vector2(48f, title.rectTransform.offsetMin.y);
            title.rectTransform.offsetMax = new Vector2(-160f, title.rectTransform.offsetMax.y);

            playerStatusText = CreateText(playerStatusPanel, "StatusText", string.Empty, 26, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            playerStatusText.rectTransform.anchorMin = Vector2.zero;
            playerStatusText.rectTransform.anchorMax = Vector2.one;
            playerStatusText.rectTransform.offsetMin = new Vector2(54f, 82f);
            playerStatusText.rectTransform.offsetMax = new Vector2(-54f, -104f);

            CreateHudButton(playerStatusPanel, "Close", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -52f), new Vector2(92f, 58f), 24f, ClosePlayerStatusPanel);
        }

        private Button CreateBuildItemButton(RectTransform parent, string label, ObjectKind kind, Vector2 position, Color swatchColor, string sizeLabel)
        {
            Button button = CreateHudButton(parent, "BuildItem_" + kind, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(118f, 92f), 18f, () => SelectBuildItem(kind));
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = new Color(0.09f, 0.13f, 0.105f, 0.98f);

            RectTransform rect = button.transform as RectTransform;
            GameObject swatch = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            RectTransform swatchRect = swatch.GetComponent<RectTransform>();
            swatchRect.SetParent(rect, false);
            swatchRect.anchorMin = new Vector2(0.5f, 1f);
            swatchRect.anchorMax = new Vector2(0.5f, 1f);
            swatchRect.pivot = new Vector2(0.5f, 1f);
            swatchRect.anchoredPosition = new Vector2(0f, -8f);
            swatchRect.sizeDelta = GetBuildSwatchSize(kind);
            swatch.GetComponent<Image>().color = swatchColor;

            TextMeshProUGUI size = CreateText(rect, "Size", sizeLabel, 13, FontStyles.Bold, TextAlignmentOptions.Right);
            size.rectTransform.anchorMin = new Vector2(0f, 0f);
            size.rectTransform.anchorMax = new Vector2(1f, 0f);
            size.rectTransform.pivot = new Vector2(0.5f, 0f);
            size.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            size.rectTransform.sizeDelta = new Vector2(-14f, 22f);
            return button;
        }

        private static Vector2 GetBuildSwatchSize(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.SleepingBag => new Vector2(32f, 46f),
                ObjectKind.CommunityStorage => new Vector2(58f, 38f),
                ObjectKind.SlumTradeCenter => new Vector2(58f, 38f),
                ObjectKind.StoneQuarry => new Vector2(66f, 66f),
                ObjectKind.Sawmill => new Vector2(66f, 66f),
                ObjectKind.SmallHouse => new Vector2(58f, 58f),
                ObjectKind.StoneTower => new Vector2(66f, 66f),
                ObjectKind.StoneKeep => new Vector2(78f, 78f),
                ObjectKind.Castle => new Vector2(86f, 86f),
                _ => new Vector2(42f, 34f)
            };
        }

        private enum Anchor
        {
            TopStretch,
            TopLeft,
            TopRight,
            BottomCenter,
            BottomLeft,
            BottomRight,
            RightMiddle,
            Center
        }

        private static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopStretch:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    break;
                case Anchor.TopLeft:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    break;
                case Anchor.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    break;
                case Anchor.BottomLeft:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    break;
                case Anchor.BottomRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case Anchor.RightMiddle:
                    rect.anchorMin = new Vector2(1f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    break;
                case Anchor.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private void HandleInput()
        {
            if (isResourceWorking)
            {
                pawnMoveInput = Vector2.zero;
                Keyboard resourceKeyboard = Keyboard.current;
                if (resourceKeyboard != null && WasPressed(resourceKeyboard.escapeKey))
                    StopResourceWork("cancelled");

                Mouse resourceMouse = Mouse.current;
                if (resourceMouse != null && resourceMouse.leftButton.wasPressedThisFrame)
                    StopResourceWork("clicked_out");

                Touchscreen resourceTouchscreen = Touchscreen.current;
                if (resourceTouchscreen != null && resourceTouchscreen.primaryTouch.press.wasPressedThisFrame)
                    StopResourceWork("tapped_out");

                return;
            }

            if (playerStatusPanel != null && playerStatusPanel.gameObject.activeInHierarchy)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && WasPressed(keyboard.escapeKey))
                    ClosePlayerStatusPanel();
            }

            if (isConfirmingTransition)
            {
                pawnMoveInput = Vector2.zero;
                HandleTransitionPromptInput();
                return;
            }

            if (isEditingNote)
            {
                pawnMoveInput = Vector2.zero;
                HandleNoteInput();
                return;
            }

            HandleArchitectKeyboard();

            if (TryHandleTouchInput())
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 mousePosition = mouse.position.ReadValue();

            if (mouse.rightButton.wasPressedThisFrame)
            {
                SelectCellFromScreen(mousePosition);
                OpenCellActionMenu();
            }

            if (mouse.middleButton.wasPressedThisFrame)
                BeginPan(mousePosition);

            if (mouse.middleButton.isPressed)
                ContinuePan(mousePosition);

            if (mouse.middleButton.wasReleasedThisFrame)
                isPanning = false;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                mousePressStartPosition = mousePosition;
                mousePressStartTime = Time.unscaledTime;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if ((mousePosition - mousePressStartPosition).sqrMagnitude <= PointerHoldMoveTolerancePixels * PointerHoldMoveTolerancePixels
                    && !hasPendingBuildKind
                    && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                {
                    ActivatePrimaryCellClick(mousePressStartPosition);
                }
            }

            float wheelZoom = ResolveMouseWheelZoom(mouse);
            if (Mathf.Abs(wheelZoom) > 0.05f)
                ZoomCamera(wheelZoom, mousePosition);
        }

        private void ZoomCamera(float zoomDelta, Vector2 screenPivot)
        {
            Vector3 beforeZoom = ScreenToGround(screenPivot);
            Vector3 currentFocus = CameraFocusFromPosition(cameraTargetPosition);
            float oldZoom = Mathf.Max(0.01f, cameraTargetZoom);
            cameraTargetZoom = Mathf.Clamp(cameraTargetZoom - zoomDelta, MinCameraZoom, MaxCameraZoom);
            float zoomRatio = cameraTargetZoom / oldZoom;
            Vector3 nextFocus = beforeZoom - (beforeZoom - currentFocus) * zoomRatio;
            cameraTargetPosition = ClampCamera(CameraPositionForFocus(nextFocus));
            cameraWasManuallyMoved = true;
        }

        private static float ResolveMouseWheelZoom(Mouse mouse)
        {
            if (mouse == null)
                return 0f;

            float raw = mouse.scroll.y.ReadValue();
            if (Mathf.Abs(raw) <= 0.01f)
                raw = mouse.scroll.ReadValue().y;

            float abs = Mathf.Abs(raw);
            if (abs <= 0.01f)
                return 0f;

            float normalized = abs >= 20f ? raw / 120f : raw;
            return normalized * 5.5f;
        }

        private bool TryHandleTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return false;

            TouchControl primary = touchscreen.primaryTouch;
            if (primary == null)
                return false;

            if (TryHandleTwoFingerMoveCommand(touchscreen))
                return true;

            if (TryHandlePinchZoom(touchscreen))
                return true;

            isPinching = false;
            Vector2 position = primary.position.ReadValue();

            if (primary.press.wasPressedThisFrame)
            {
                hasPrimaryTouchStart = true;
                lastPrimaryTouchPosition = position;
                touchPressStartPosition = position;
                touchPressStartTime = Time.unscaledTime;
                touchHoldInspectTriggered = false;
                BeginPan(position);
                return true;
            }

            if (primary.press.isPressed)
            {
                if (!touchHoldInspectTriggered
                    && Time.unscaledTime - touchPressStartTime >= TouchHoldInspectSeconds
                    && (position - touchPressStartPosition).sqrMagnitude < 36f)
                {
                    touchHoldInspectTriggered = true;
                    SelectCellFromScreen(position);
                    OpenCellActionMenu();
                    isPanning = false;
                    return true;
                }

                ContinuePan(position);
                return true;
            }

            if (primary.press.wasReleasedThisFrame)
            {
                if (!touchHoldInspectTriggered && hasPrimaryTouchStart && (position - lastPrimaryTouchPosition).sqrMagnitude < 16f)
                    ActivatePrimaryCellClick(position);

                hasPrimaryTouchStart = false;
                isPanning = false;
                isPinching = false;
                return true;
            }

            return false;
        }

        private bool TryHandleTwoFingerMoveCommand(Touchscreen touchscreen)
        {
            if (touchscreen == null || touchscreen.touches.Count < 2)
            {
                twoFingerMoveIssued = false;
                return false;
            }

            TouchControl first = touchscreen.touches[0];
            TouchControl second = touchscreen.touches[1];
            if (first == null || second == null || !first.press.isPressed || !second.press.isPressed)
            {
                twoFingerMoveIssued = false;
                return false;
            }

            Vector2 firstPosition = first.position.ReadValue();
            Vector2 secondPosition = second.position.ReadValue();
            if (Vector2.Distance(firstPosition, secondPosition) > TwoFingerMoveMaxDistancePixels)
            {
                twoFingerMoveIssued = false;
                return false;
            }

            Vector2Int firstCell = WorldToCell(ScreenToGround(firstPosition));
            Vector2Int secondCell = WorldToCell(ScreenToGround(secondPosition));
            if (firstCell != secondCell)
            {
                twoFingerMoveIssued = false;
                return false;
            }

            if (twoFingerMoveIssued)
                return true;

            twoFingerMoveIssued = true;
            isPinching = false;
            isPanning = false;
            SetMoveTarget(firstCell);
            return true;
        }

        private bool TryHandlePinchZoom(Touchscreen touchscreen)
        {
            if (touchscreen.touches.Count < 2)
                return false;

            TouchControl first = touchscreen.touches[0];
            TouchControl second = touchscreen.touches[1];
            if (first == null || second == null || !first.press.isPressed || !second.press.isPressed)
                return false;

            Vector2 firstPosition = first.position.ReadValue();
            Vector2 secondPosition = second.position.ReadValue();
            float distance = Vector2.Distance(firstPosition, secondPosition);
            Vector2 pivot = (firstPosition + secondPosition) * 0.5f;

            if (!isPinching)
            {
                isPinching = true;
                isPanning = false;
                lastPinchDistance = distance;
                return true;
            }

            float pinchDelta = (distance - lastPinchDistance) * TouchPinchZoomScale;
            if (Mathf.Abs(pinchDelta) > 0.01f)
                ZoomCamera(pinchDelta, pivot);

            lastPinchDistance = distance;
            return true;
        }

        private static bool WasPressed(ButtonControl control)
        {
            return control != null && control.wasPressedThisFrame;
        }

        private static bool IsPressed(ButtonControl control)
        {
            return control != null && control.isPressed;
        }

        private void HandleArchitectKeyboard()
        {
            pawnMoveInput = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (WasPressed(keyboard.eKey) && IsPortalTriggerCell(selectedCell))
                HandleCenterDoorReached();

            if (WasPressed(keyboard.fKey))
                CenterCameraOnPawn();

            if (WasPressed(keyboard.f11Key))
                ToggleFullscreen();

            if (WasPressed(keyboard.uKey))
                DeleteObject(hasSelectedObject ? selectedObjectAnchorCell : selectedCell);

            if (WasPressed(keyboard.equalsKey) || WasPressed(keyboard.numpadPlusKey))
                ZoomCamera(KeyboardZoomStep, worldCamera != null ? worldCamera.pixelRect.center : Vector2.zero);

            if (WasPressed(keyboard.minusKey) || WasPressed(keyboard.numpadMinusKey))
                ZoomCamera(-KeyboardZoomStep, worldCamera != null ? worldCamera.pixelRect.center : Vector2.zero);

            bool tentShortcut = IsPressed(keyboard.tKey);
            if (tentShortcut)
            {
                if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key))
                    PlaceObject(selectedCell, ObjectKind.CommunityStorage);

                if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key))
                    PlaceObject(selectedCell, ObjectKind.SlumTradeCenter);

                if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key))
                    PlaceObject(selectedCell, ObjectKind.SmallHouse);

                if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key))
                    PlaceObject(selectedCell, ObjectKind.StoneTower);

                if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key))
                    PlaceObject(selectedCell, ObjectKind.StoneKeep);

                if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key))
                    PlaceObject(selectedCell, ObjectKind.Castle);
            }
            else
            {
                if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key))
                    HandleToolPress(BuildTool.Wall, ObjectKind.Wall);

                if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key))
                    HandleToolPress(BuildTool.Road, ObjectKind.Road);

                if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key))
                    HandleToolPress(BuildTool.Sign, ObjectKind.Sign);

                if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key))
                    HandleToolPress(BuildTool.Repair, ObjectKind.Repair);

                if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key))
                    HandleToolPress(BuildTool.BuildPlot, ObjectKind.BuildPlot);

                if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key))
                    HandleToolPress(BuildTool.SleepingBag, ObjectKind.SleepingBag);
            }

            if (IsPressed(keyboard.wKey) || IsPressed(keyboard.upArrowKey))
                pawnMoveInput.y += 1f;
            if (IsPressed(keyboard.sKey) || IsPressed(keyboard.downArrowKey))
                pawnMoveInput.y -= 1f;
            if (IsPressed(keyboard.aKey) || IsPressed(keyboard.leftArrowKey))
                pawnMoveInput.x -= 1f;
            if (IsPressed(keyboard.dKey) || IsPressed(keyboard.rightArrowKey))
                pawnMoveInput.x += 1f;

            if (pawnMoveInput.sqrMagnitude > 1f)
                pawnMoveInput.Normalize();
        }

        private void HandleNoteInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (!placedObjects.TryGetValue(editingNoteCell, out PlacedObject placed) || placed.Kind != ObjectKind.Sign)
            {
                isEditingNote = false;
                return;
            }

            if (keyboard != null && WasPressed(keyboard.escapeKey))
            {
                CancelNoteDialog();
                return;
            }

            if (keyboard != null && (WasPressed(keyboard.enterKey) || WasPressed(keyboard.numpadEnterKey)))
            {
                SaveNoteDialog();
                return;
            }

            if (keyboard != null && WasPressed(keyboard.backspaceKey) && placed.Note.Length > 0)
                placed.Note = noteInput != null ? noteInput.text : placed.Note.Substring(0, placed.Note.Length - 1);
        }

        private void HandleTransitionPromptInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (WasPressed(keyboard.escapeKey))
                CancelTransitionPrompt();

            if (WasPressed(keyboard.enterKey) || WasPressed(keyboard.numpadEnterKey))
                ConfirmTransition();
        }

        private void BeginPan(Vector2 screenPosition)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isPanning = true;
            cameraWasManuallyMoved = true;
            panStartWorld = ScreenToGround(screenPosition);
            cameraStart = cameraTargetPosition;
        }

        private void ContinuePan(Vector2 screenPosition)
        {
            if (!isPanning)
                return;

            Vector3 current = ScreenToGround(screenPosition);
            Vector3 delta = panStartWorld - current;
            cameraTargetPosition = ClampCamera(cameraStart + delta);
        }

        private void SelectCellFromScreen(Vector2 screenPosition)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2Int cell = WorldToCell(ScreenToGround(screenPosition));
            if (!hasPendingBuildKind && TryResolveTreeAtScreenPoint(screenPosition, out Vector2Int treeCell))
                cell = treeCell;

            bool doubleClick = cell == lastClickedCell && Time.unscaledTime - lastCellClickTime <= CellDoubleClickSeconds;
            lastClickedCell = cell;
            lastCellClickTime = Time.unscaledTime;
            SelectCell(cell);
            HideCellContextMenus();
            if (!hasPendingBuildKind)
                HideObjectProfileCard();
            if (doubleClick && TryOpenSelectedPlayerSleeperStatus())
                return;

            if (hasPendingBuildKind)
                PreviewPendingBuild(cell);
        }

        private bool TryResolveTreeAtScreenPoint(Vector2 screenPosition, out Vector2Int anchorCell)
        {
            anchorCell = default;
            if (worldCamera == null || placedObjects == null)
                return false;

            if (TryResolveTreeAtPhysicsRay(screenPosition, out anchorCell))
                return true;

            float bestDistance = float.MaxValue;
            bool found = false;
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value == null || pair.Value.Kind != ObjectKind.Tree)
                    continue;

                int variant = ParseTreeVariant(pair.Value.Note);
                Vector3 baseWorld = CellToWorld(pair.Key);
                Vector3 centerWorld = baseWorld + new Vector3(0f, ResolveTreeSpriteHeight(variant) * 0.52f, 0.24f);
                Vector3 screenCenter = worldCamera.WorldToScreenPoint(centerWorld);
                if (screenCenter.z < 0f)
                    continue;

                float pixelsPerWorld = ResolvePixelsPerWorldAt(centerWorld);
                float halfWidth = Mathf.Max(24f, ResolveTreeSpriteHeight(variant) * ResolveTreeSpriteAspect(variant) * pixelsPerWorld * 0.58f);
                float halfHeight = Mathf.Max(30f, ResolveTreeSpriteHeight(variant) * pixelsPerWorld * 0.52f);
                Vector2 delta = screenPosition - new Vector2(screenCenter.x, screenCenter.y);
                if (Mathf.Abs(delta.x) > halfWidth || Mathf.Abs(delta.y) > halfHeight)
                    continue;

                float distance = delta.sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                anchorCell = pair.Key;
                found = true;
            }

            return found;
        }

        private bool TryResolveTreeAtPhysicsRay(Vector2 screenPosition, out Vector2Int anchorCell)
        {
            anchorCell = default;
            if (worldCamera == null || placedObjects == null)
                return false;

            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                Transform hitTransform = hits[hitIndex].transform;
                if (hitTransform == null)
                    continue;

                foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
                {
                    if (pair.Value == null || pair.Value.Kind != ObjectKind.Tree || pair.Value.Root == null)
                        continue;

                    Transform treeRoot = pair.Value.Root.transform;
                    if (hitTransform == treeRoot || hitTransform.IsChildOf(treeRoot))
                    {
                        anchorCell = pair.Key;
                        return true;
                    }
                }
            }

            return false;
        }

        private float ResolvePixelsPerWorldAt(Vector3 worldPoint)
        {
            if (worldCamera == null)
                return 32f;

            Vector3 a = worldCamera.WorldToScreenPoint(worldPoint);
            Vector3 b = worldCamera.WorldToScreenPoint(worldPoint + Vector3.up);
            return Mathf.Max(1f, Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y)));
        }

        private void ActivatePrimaryCellClick(Vector2 screenPosition)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            SelectCellFromScreen(screenPosition);
            if (hasPendingBuildKind)
                return;

            if (hasSelectedObject && placedObjects.TryGetValue(selectedObjectAnchorCell, out _))
            {
                OpenObjectInteractionMenu();
                return;
            }

            SetMoveTarget(selectedCell);
            HideCellContextMenus();
        }

        private void OpenCellActionMenu()
        {
            if (cellActionPanel == null)
                return;

            if (hasSelectedObject)
            {
                OpenObjectInteractionMenu();
                return;
            }

            if (cellBuildCarouselPanel != null)
                cellBuildCarouselPanel.gameObject.SetActive(false);
            if (objectInteractionPanel != null)
                objectInteractionPanel.gameObject.SetActive(false);

            cellActionPanel.gameObject.SetActive(true);
            ShowObjectProfileCardForSelectedCell();
            PositionPanelNearSelectedCell(cellActionPanel, new Vector2(172f, 92f));
        }

        private void OpenObjectInteractionMenu()
        {
            if (objectInteractionPanel == null)
                return;

            if (cellActionPanel != null)
                cellActionPanel.gameObject.SetActive(false);
            if (cellBuildCarouselPanel != null)
                cellBuildCarouselPanel.gameObject.SetActive(false);

            if (objectUseButtonText != null)
            {
                string label = "Use";
                if (hasSelectedObject
                    && placedObjects != null
                    && placedObjects.TryGetValue(selectedObjectAnchorCell, out PlacedObject placed))
                {
                    label = placed.Kind switch
                    {
                        ObjectKind.Tree => "Chop tree",
                        ObjectKind.BerryBush => "Gather",
                        ObjectKind.StoneQuarry => "Mine stone",
                        ObjectKind.Sawmill => "Gather wood",
                        ObjectKind.SmallHouse => IsActiveEstateInterior(selectedObjectAnchorCell, placed.Kind) ? "Exit" : "Enter",
                        ObjectKind.StoneTower => IsActiveEstateInterior(selectedObjectAnchorCell, placed.Kind) ? "Exit" : "Enter",
                        ObjectKind.StoneKeep => IsActiveEstateInterior(selectedObjectAnchorCell, placed.Kind) ? "Exit" : "Enter",
                        ObjectKind.Castle => IsActiveEstateInterior(selectedObjectAnchorCell, placed.Kind) ? "Exit" : "Enter",
                        _ => "Use"
                    };
                }

                objectUseButtonText.text = label;
            }

            objectInteractionPanel.gameObject.SetActive(true);
            ShowObjectProfileCardForSelectedCell();
            PositionPanelNearSelectedCell(objectInteractionPanel, new Vector2(176f, 92f));
        }

        private void ShowBuildCarouselNearSelectedCell()
        {
            if (cellBuildCarouselPanel == null)
                return;

            if (cellActionPanel != null)
                cellActionPanel.gameObject.SetActive(false);

            cellBuildCarouselPanel.gameObject.SetActive(true);
            ShowObjectProfileCardForSelectedCell();
            PositionPanelNearSelectedCell(cellBuildCarouselPanel, new Vector2(190f, 118f));
        }

        private void HideCellContextMenus()
        {
            if (cellActionPanel != null)
                cellActionPanel.gameObject.SetActive(false);
            if (cellBuildCarouselPanel != null)
                cellBuildCarouselPanel.gameObject.SetActive(false);
            if (objectInteractionPanel != null)
                objectInteractionPanel.gameObject.SetActive(false);
            HideObjectProfileCard();
        }

        private void UpdateCellContextPanelsPosition()
        {
            if (cellActionPanel != null && cellActionPanel.gameObject.activeInHierarchy)
                PositionPanelNearSelectedCell(cellActionPanel, new Vector2(172f, 92f));
            if (cellBuildCarouselPanel != null && cellBuildCarouselPanel.gameObject.activeInHierarchy)
                PositionPanelNearSelectedCell(cellBuildCarouselPanel, new Vector2(190f, 118f));
            if (objectInteractionPanel != null && objectInteractionPanel.gameObject.activeInHierarchy)
                PositionPanelNearSelectedCell(objectInteractionPanel, new Vector2(176f, 92f));
            if (objectProfilePanel != null && objectProfilePanel.gameObject.activeInHierarchy)
                PositionPanelNearSelectedCell(objectProfilePanel, new Vector2(-230f, 116f));
        }

        private void PositionPanelNearSelectedCell(RectTransform panel, Vector2 offset)
        {
            if (panel == null || canvas == null || worldCamera == null)
                return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(CellToWorld(selectedCell) + new Vector3(0f, 0.12f, 0f));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
                return;

            Vector2 size = panel.sizeDelta;
            Rect rect = canvasRect.rect;
            localPoint += offset;
            localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin + size.x * 0.5f + 16f, rect.xMax - size.x * 0.5f - 16f);
            localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin + size.y * 0.5f + 16f, rect.yMax - size.y * 0.5f - 16f);
            panel.anchoredPosition = localPoint;
        }

        private void ShowObjectProfileCardForSelectedCell()
        {
            if (objectProfilePanel == null || objectProfileText == null)
                return;

            objectProfileText.text = BuildSelectedCellProfileText();
            objectProfilePanel.gameObject.SetActive(true);
            PositionPanelNearSelectedCell(objectProfilePanel, new Vector2(-230f, 116f));
        }

        private void ShowObjectProfileCardForBuildKind(ObjectKind kind)
        {
            if (objectProfilePanel == null || objectProfileText == null)
                return;

            objectProfileText.text = BuildObjectProfileText(kind, true, pendingBuildCell);
            objectProfilePanel.gameObject.SetActive(true);
            PositionPanelNearSelectedCell(objectProfilePanel, new Vector2(-230f, 116f));
        }

        private void HideObjectProfileCard()
        {
            if (objectProfilePanel != null)
                objectProfilePanel.gameObject.SetActive(false);
        }

        private string BuildSelectedCellProfileText()
        {
            if (hasSelectedObject && placedObjects != null && placedObjects.TryGetValue(selectedObjectAnchorCell, out PlacedObject placed))
                return BuildObjectProfileText(placed.Kind, false, selectedObjectAnchorCell);

            if (IsPortalReservedCell(selectedCell))
                return $"Portal cell\nCell: {selectedCell.x:000}:{selectedCell.y:000}\nSize: 3x3 gate zone\nRole: cluster transfer trigger.\nStatus: reserved; building disabled.";

            return $"Empty land\nCell: {selectedCell.x:000}:{selectedCell.y:000}\nTerrain: living soil\nRole: free construction plot.\nStatus: ready for building or movement.";
        }

        private string BuildObjectProfileText(ObjectKind kind, bool blueprint, Vector2Int cell)
        {
            string state = blueprint ? "Blueprint preview" : "Placed object";
            return $"{GetObjectDisplayName(kind)}\n" +
                   $"Cell: {cell.x:000}:{cell.y:000}\n" +
                   $"State: {state}\n" +
                   $"Size: {GetObjectFootprintLabel(kind)}\n" +
                   $"Deed: {GetObjectCostLabel(kind)}\n" +
                   $"Role: {GetObjectRoleDescription(kind)}\n" +
                   $"Use: {GetObjectUseDescription(kind)}";
        }

        private static string GetObjectFootprintLabel(ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            return $"{size.x}x{size.y}";
        }

        private static string GetObjectRoleDescription(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.Wall => "solid barrier; blocks pathing and shapes rooms.",
                ObjectKind.Road => "travel surface; increases movement speed.",
                ObjectKind.Sign => "note marker; stores text or blueprint commands.",
                ObjectKind.Repair => "wooden work patch for repairs and temporary fixes.",
                ObjectKind.BuildPlot => "reserved square for future construction plans.",
                ObjectKind.SleepingBag => "personal rest marker; two nearby bags form a tent.",
                ObjectKind.Tent => "small shared camp created from paired sleeping bags.",
                ObjectKind.BerryBush => "Materia resource node with edible berries.",
                ObjectKind.Tree => "Materia wood node; chop for logs.",
                ObjectKind.CommunityStorage => "early shared depot for settlement materials.",
                ObjectKind.SlumTradeCenter => "trade platform for local exchange and services.",
                ObjectKind.StoneQuarry => "stone resource worksite; blocks movement through its working zone.",
                ObjectKind.Sawmill => "wood resource worksite; blocks movement through its working zone.",
                ObjectKind.SmallHouse => "claimed family house with walls, roof, front door, and walkable interior.",
                ObjectKind.StoneTower => "vertical estate tower with a gate and protected interior floor.",
                ObjectKind.StoneKeep => "fortified keep; large visible land claim with an enterable courtyard.",
                ObjectKind.Castle => "largest estate claim; status fortress with gate, halls, and courtyard space.",
                _ => "world object."
            };
        }

        private static string GetObjectCostLabel(ObjectKind kind)
        {
            return TryGetBuildDeedCost(kind, out int stoneCost, out int woodCost)
                ? $"{stoneCost} stone, {woodCost} wood"
                : "none";
        }

        private static string GetObjectUseDescription(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.BerryBush => "eat or gather when close enough.",
                ObjectKind.Tree => "chop for one minute to collect 3 logs.",
                ObjectKind.Sign => "double click or long press to inspect/edit.",
                ObjectKind.SleepingBag => "double click or long press to open player status.",
                ObjectKind.CommunityStorage => "future shared inventory anchor.",
                ObjectKind.SlumTradeCenter => "future market and barter anchor.",
                ObjectKind.StoneQuarry => "approach and use to mine stone every 10 seconds.",
                ObjectKind.Sawmill => "approach and use to gather wood every 10 seconds.",
                ObjectKind.SmallHouse => "press Enter/Use at the front door to step inside.",
                ObjectKind.StoneTower => "press Enter/Use to move through the gate into the tower estate.",
                ObjectKind.StoneKeep => "press Enter/Use to enter the keep courtyard.",
                ObjectKind.Castle => "press Enter/Use to enter the castle courtyard.",
                _ => "place with OK, cancel with X."
            };
        }

        private void MoveToSelectedCellFromMenu()
        {
            HideCellContextMenus();
            HideObjectProfileCard();
            SetMoveTarget(selectedCell);
        }

        private void OpenSelectedStatusFromMenu()
        {
            HideCellContextMenus();
            HideObjectProfileCard();
            TryOpenSelectedPlayerSleeperStatus();
        }

        private void UseSelectedObjectFromMenu()
        {
            if (!hasSelectedObject || placedObjects == null || !placedObjects.TryGetValue(selectedObjectAnchorCell, out PlacedObject placed))
                return;

            HideCellContextMenus();
            switch (placed.Kind)
            {
                case ObjectKind.Sign:
                    OpenNoteDialog(selectedObjectAnchorCell, placed);
                    break;
                case ObjectKind.SleepingBag:
                    TryOpenSelectedPlayerSleeperStatus();
                    break;
                case ObjectKind.BerryBush:
                    GatherSelectedBerryBush();
                    break;
                case ObjectKind.Tree:
                    StartChopSelectedTree();
                    break;
                case ObjectKind.StoneQuarry:
                case ObjectKind.Sawmill:
                    ToggleResourceWork(placed.Kind, selectedObjectAnchorCell);
                    break;
                case ObjectKind.SmallHouse:
                case ObjectKind.StoneTower:
                case ObjectKind.StoneKeep:
                case ObjectKind.Castle:
                    if (IsActiveEstateInterior(selectedObjectAnchorCell, placed.Kind))
                        ExitActiveEstateInterior();
                    else
                        EnterSelectedEstate(placed.Kind, selectedObjectAnchorCell);
                    break;
                default:
                    ShowObjectProfileCardForSelectedCell();
                    SymbiozRuntimeLog.Write("OBJECT", $"Interaction opened kind={placed.Kind} anchor={selectedObjectAnchorCell.x:000}:{selectedObjectAnchorCell.y:000}");
                    break;
            }
        }

        private void EnterSelectedEstate(ObjectKind kind, Vector2Int anchorCell)
        {
            if (!IsEstateKind(kind))
                return;

            Vector2Int doorCell = GetEstateDoorCell(anchorCell, kind);
            Vector2Int interiorCell = GetEstateInteriorEntryCell(anchorCell, kind);
            Vector2Int target = IsEstateWalkableCell(anchorCell, kind, interiorCell) ? interiorCell : doorCell;

            HideObjectProfileCard();
            ClearMoveTargetMarker();
            pawnMoveInput = Vector2.zero;
            hasMoveTarget = false;
            hasAutoMoveTarget = false;
            isInsideEstateInterior = true;
            activeEstateInteriorKind = kind;
            activeEstateInteriorAnchor = anchorCell;
            targetCell = target;
            selectedCell = target;

            if (pawn != null)
            {
                pawn.transform.position = CellToWorld(target) + new Vector3(0f, PawnGroundYOffset, 0f);
                pawnCell = target;
                cameraTargetPosition = ClampCamera(CameraPositionForFocus(pawn.transform.position));
                cameraTargetZoom = Mathf.Min(cameraTargetZoom, 8.5f);
                cameraWasManuallyMoved = false;
                RefreshPawnRenderOrder();
            }

            SelectCell(target);
            UpdateEstateInteriorVisibility();
            SymbiozRuntimeLog.Write("ESTATE", $"Entered kind={kind} anchor={anchorCell.x:000}:{anchorCell.y:000} cell={target.x:000}:{target.y:000}");
        }

        private void ExitActiveEstateInterior()
        {
            if (!isInsideEstateInterior)
                return;

            ObjectKind kind = activeEstateInteriorKind;
            Vector2Int anchorCell = activeEstateInteriorAnchor;
            Vector2Int doorCell = GetEstateDoorCell(anchorCell, kind);
            Vector2Int outsideCell = ClampCell(doorCell + Vector2Int.down);
            Vector2Int target = !IsPortalReservedCell(outsideCell) ? outsideCell : doorCell;

            HideCellContextMenus();
            HideObjectProfileCard();
            ClearMoveTargetMarker();
            pawnMoveInput = Vector2.zero;
            hasMoveTarget = false;
            hasAutoMoveTarget = false;
            isInsideEstateInterior = false;
            activeEstateInteriorKind = default;
            activeEstateInteriorAnchor = default;
            targetCell = target;
            selectedCell = target;

            if (pawn != null)
            {
                pawn.transform.position = CellToWorld(target) + new Vector3(0f, PawnGroundYOffset, 0f);
                pawnCell = target;
                cameraTargetPosition = ClampCamera(CameraPositionForFocus(pawn.transform.position));
                RefreshPawnRenderOrder();
            }

            SelectCell(target);
            UpdateEstateInteriorVisibility();
            SymbiozRuntimeLog.Write("ESTATE", $"Exited kind={kind} anchor={anchorCell.x:000}:{anchorCell.y:000} cell={target.x:000}:{target.y:000}");
        }

        private bool IsActiveEstateInterior(Vector2Int anchorCell, ObjectKind kind)
        {
            return isInsideEstateInterior
                && activeEstateInteriorAnchor == anchorCell
                && activeEstateInteriorKind == kind;
        }

        private void DeleteSelectedObjectFromMenu()
        {
            if (!hasSelectedObject)
                return;

            Vector2Int anchor = selectedObjectAnchorCell;
            HideCellContextMenus();
            HideObjectProfileCard();
            DeleteObject(anchor);
            SelectCell(selectedCell);
        }

        private void ToggleResourceWork(ObjectKind kind, Vector2Int anchorCell)
        {
            if (!IsResourceWorksiteKind(kind))
                return;

            if (isResourceWorking && activeResourceKind == kind && activeResourceAnchorCell == anchorCell)
            {
                StopResourceWork("manual_stop");
                return;
            }

            if (!IsObjectInInteractRange(anchorCell, kind))
            {
                ShowObjectProfileCardForSelectedCell();
                SymbiozRuntimeLog.Write("RESOURCE", $"Too far to work kind={kind} anchor={anchorCell.x:000}:{anchorCell.y:000} pawn={pawnCell.x:000}:{pawnCell.y:000}");
                return;
            }

            StartResourceWork(kind, anchorCell);
        }

        private void StartResourceWork(ObjectKind kind, Vector2Int anchorCell)
        {
            if (!IsResourceWorksiteKind(kind))
                return;

            HideCellContextMenus();
            HideObjectProfileCard();
            ClearMoveTargetMarker();
            pawnMoveInput = Vector2.zero;
            hasMoveTarget = false;
            hasAutoMoveTarget = false;
            isResourceWorking = true;
            activeResourceKind = kind;
            activeResourceAnchorCell = anchorCell;
            nextResourceWorkTickTime = Time.unscaledTime + ResourceWorkIntervalSeconds;
            if (pawnVisualRoot != null)
                pawnVisualRoot.gameObject.SetActive(false);

            SymbiozRuntimeLog.Write("RESOURCE", $"Started work kind={kind} anchor={anchorCell.x:000}:{anchorCell.y:000}");
        }

        private void StopResourceWork(string reason)
        {
            if (!isResourceWorking)
                return;

            SymbiozRuntimeLog.Write("RESOURCE", $"Stopped work kind={activeResourceKind} reason={reason} stone={carriedStone} wood={carriedWood}");
            isResourceWorking = false;
            activeResourceKind = default;
            activeResourceAnchorCell = default;
            nextResourceWorkTickTime = 0f;
            if (pawnVisualRoot != null)
                pawnVisualRoot.gameObject.SetActive(true);
        }

        private static bool IsResourceWorksiteKind(ObjectKind kind)
        {
            return kind == ObjectKind.StoneQuarry || kind == ObjectKind.Sawmill;
        }

        private bool IsObjectInInteractRange(Vector2Int anchorCell, ObjectKind kind)
        {
            float distanceCells = kind switch
            {
                ObjectKind.StoneQuarry => ResourceWorkInteractDistanceCells,
                ObjectKind.Sawmill => ResourceWorkInteractDistanceCells,
                ObjectKind.Tree => TreeInteractDistanceCells,
                ObjectKind.BerryBush => BerryBushInteractDistanceCells,
                _ => BerryBushInteractDistanceCells
            };

            return DistanceFromCellToObjectFootprint(pawnCell, anchorCell, kind) <= distanceCells;
        }

        private static float DistanceFromCellToObjectFootprint(Vector2Int fromCell, Vector2Int anchorCell, ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            int minX = anchorCell.x;
            int maxX = anchorCell.x + size.x - 1;
            int minY = anchorCell.y;
            int maxY = anchorCell.y + size.y - 1;
            int dx = fromCell.x < minX ? minX - fromCell.x : fromCell.x > maxX ? fromCell.x - maxX : 0;
            int dy = fromCell.y < minY ? minY - fromCell.y : fromCell.y > maxY ? fromCell.y - maxY : 0;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private void UpdateResourceWork()
        {
            if (!isResourceWorking)
                return;

            if (placedObjects == null
                || !placedObjects.TryGetValue(activeResourceAnchorCell, out PlacedObject placed)
                || placed == null
                || placed.Kind != activeResourceKind)
            {
                StopResourceWork("worksite_missing");
                return;
            }

            if (Time.unscaledTime < nextResourceWorkTickTime)
                return;

            if (activeResourceKind == ObjectKind.StoneQuarry)
                carriedStone++;
            else if (activeResourceKind == ObjectKind.Sawmill)
                carriedWood++;

            SaveResourceInventory();
            nextResourceWorkTickTime = Time.unscaledTime + ResourceWorkIntervalSeconds;
            SymbiozRuntimeLog.Write("RESOURCE", $"Gather tick kind={activeResourceKind} stone={carriedStone} wood={carriedWood}");
        }

        private void SelectContextBuildItem(ObjectKind kind)
        {
            SelectBuildItem(kind);
            PreviewPendingBuild(selectedCell);
            if (buildPalettePanel != null)
                buildPalettePanel.gameObject.SetActive(false);
            isBuildPaletteOpen = false;
            HideCellContextMenus();
            ShowObjectProfileCardForBuildKind(kind);
        }

        private void ToggleBuildPalette()
        {
            isBuildPaletteOpen = !isBuildPaletteOpen;
            if (buildPalettePanel != null)
                buildPalettePanel.gameObject.SetActive(isBuildPaletteOpen);

            if (!isBuildPaletteOpen)
                CancelPendingBuild();
        }

        private void SelectBuildItem(ObjectKind kind)
        {
            pendingBuildKind = kind;
            hasPendingBuildKind = true;
            hasPendingBuildPlacement = false;
            ClearPendingBuildPreview();
            selectedTool = KindToBuildTool(kind);

            if (buildSelectionText != null)
                buildSelectionText.text = $"{GetObjectDisplayName(kind)} selected. Click a cell.";

            if (buildConfirmPanel != null)
                buildConfirmPanel.gameObject.SetActive(false);

            pendingBuildCell = selectedCell;
            ShowObjectProfileCardForBuildKind(kind);
        }

        private static BuildTool KindToBuildTool(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.Road => BuildTool.Road,
                ObjectKind.Sign => BuildTool.Sign,
                ObjectKind.Repair => BuildTool.Repair,
                ObjectKind.BuildPlot => BuildTool.BuildPlot,
                ObjectKind.SleepingBag => BuildTool.SleepingBag,
                ObjectKind.CommunityStorage => BuildTool.CommunityStorage,
                ObjectKind.SlumTradeCenter => BuildTool.SlumTradeCenter,
                ObjectKind.StoneQuarry => BuildTool.StoneQuarry,
                ObjectKind.Sawmill => BuildTool.Sawmill,
                ObjectKind.SmallHouse => BuildTool.SmallHouse,
                ObjectKind.StoneTower => BuildTool.StoneTower,
                ObjectKind.StoneKeep => BuildTool.StoneKeep,
                ObjectKind.Castle => BuildTool.Castle,
                _ => BuildTool.Wall
            };
        }

        private static string GetObjectDisplayName(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.Wall => "Wall",
                ObjectKind.Road => "Road",
                ObjectKind.Sign => "Sign",
                ObjectKind.Repair => "Repair planks",
                ObjectKind.BuildPlot => "Build plot",
                ObjectKind.SleepingBag => "Sleeping bag",
                ObjectKind.Tent => "Small tent",
                ObjectKind.BerryBush => "Berry bush",
                ObjectKind.Tree => "Tree",
                ObjectKind.CommunityStorage => "Community storage",
                ObjectKind.SlumTradeCenter => "Slum trade center",
                ObjectKind.StoneQuarry => "Stone quarry",
                ObjectKind.Sawmill => "Sawmill",
                ObjectKind.SmallHouse => "Small house deed",
                ObjectKind.StoneTower => "Stone tower deed",
                ObjectKind.StoneKeep => "Stone keep deed",
                ObjectKind.Castle => "Castle deed",
                _ => kind.ToString()
            };
        }

        private void PreviewPendingBuild(Vector2Int cell)
        {
            if (!hasPendingBuildKind)
                return;

            cell = ClampCell(cell);
            if (cell == pawnCell || IsPortalReservedCell(cell))
                return;

            ClearPendingBuildPreview();
            pendingBuildCell = cell;
            pendingBuildPreview = pendingBuildKind == ObjectKind.Sign ? CreateSign(cell).Root : CreateBlockObject(cell, pendingBuildKind).Root;
            TintPreviewObject(pendingBuildPreview);
            hasPendingBuildPlacement = true;

            if (buildSelectionText != null)
                buildSelectionText.text = $"{GetObjectDisplayName(pendingBuildKind)} at {cell.x:000}:{cell.y:000}";

            if (buildConfirmPanel != null)
                buildConfirmPanel.gameObject.SetActive(true);

            ShowObjectProfileCardForBuildKind(pendingBuildKind);
        }

        private static void TintPreviewObject(GameObject root)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].material;
                if (material == null)
                    continue;

                Color color = material.color;
                color.a = Mathf.Min(color.a, 0.58f);
                material.color = new Color(color.r * 0.82f, Mathf.Min(1f, color.g * 1.15f), color.b * 0.82f, color.a);
            }
        }

        private void ConfirmPendingBuild()
        {
            if (!hasPendingBuildKind || !hasPendingBuildPlacement)
                return;

            ObjectKind kind = pendingBuildKind;
            Vector2Int cell = pendingBuildCell;
            ClearPendingBuildPreview();
            hasPendingBuildKind = false;
            hasPendingBuildPlacement = false;
            if (buildConfirmPanel != null)
                buildConfirmPanel.gameObject.SetActive(false);
            if (buildSelectionText != null)
                buildSelectionText.text = "Select an item, then click a cell.";
            HideObjectProfileCard();

            PlaceObject(cell, kind);
        }

        private void CancelPendingBuild()
        {
            ClearPendingBuildPreview();
            hasPendingBuildKind = false;
            hasPendingBuildPlacement = false;
            if (buildConfirmPanel != null)
                buildConfirmPanel.gameObject.SetActive(false);
            if (buildSelectionText != null)
                buildSelectionText.text = "Select an item, then click a cell.";
            HideObjectProfileCard();
        }

        private void ClearPendingBuildPreview()
        {
            if (pendingBuildPreview != null)
                Destroy(pendingBuildPreview);

            pendingBuildPreview = null;
        }

        private void HandleToolPress(BuildTool tool, ObjectKind kind)
        {
            float now = Time.unscaledTime;
            bool isDoublePress = selectedTool == tool && lastPressedTool == tool && now - lastToolPressTime <= DoubleToolPressSeconds;
            selectedTool = tool;
            lastPressedTool = tool;
            lastToolPressTime = now;

            if (isDoublePress)
                PlaceObject(selectedCell, kind);
        }

        private void PlaceObject(Vector2Int cell, ObjectKind kind)
        {
            cell = ClampCell(cell);
            if (!CanPlaceObjectFootprint(cell, kind, out string blockedReason))
            {
                SymbiozRuntimeLog.Write("BUILD", $"Place blocked kind={kind} cell={cell.x}:{cell.y} reason={blockedReason}");
                return;
            }

            SymbiozRuntimeLog.Write("BUILD", $"Place request kind={kind} cell={cell.x}:{cell.y} location={currentLocation}");
            bool submittedToServer = false;
            if (kind != ObjectKind.Sign && IsServerAuthoritativeWorldMode())
            {
                submittedToServer = fishNetWorldBridge != null
                    && fishNetWorldBridge.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)kind, string.Empty);
                if (!submittedToServer)
                {
                    SymbiozRuntimeLog.Write("BUILD", $"Place rejected because FishNet server is not connected kind={kind} cell={cell.x}:{cell.y}");
                    return;
                }
            }

            if (!TrySpendBuildDeedResources(kind))
            {
                SymbiozRuntimeLog.Write("BUILD", $"Place rejected because deed resources are missing kind={kind} stone={carriedStone} wood={carriedWood}");
                return;
            }

            if (placedObjects.TryGetValue(cell, out PlacedObject oldObject))
            {
                if (oldObject.Root != null)
                    Destroy(oldObject.Root);
                placedObjects.Remove(cell);
                if (isEditingNote && editingNoteCell == cell)
                    CancelNoteDialog();
            }

            PlacedObject placed = kind == ObjectKind.Sign ? CreateSign(cell) : CreateBlockObject(cell, kind);
            placedObjects[cell] = placed;

            if (kind == ObjectKind.Sign)
            {
                editingNoteCell = cell;
                OpenNoteDialog(cell, placed);
            }
            else
            {
                if (!submittedToServer)
                    fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)kind, placed.Note ?? string.Empty);
                if (kind == ObjectKind.SleepingBag)
                    TryMergeSleepingBags(cell, true);
                SavePersistentWorld();
            }

            RefreshBuildTilesAround(cell);
        }

        private void DeleteObject(Vector2Int cell)
        {
            cell = ClampCell(cell);
            if (!placedObjects.TryGetValue(cell, out PlacedObject oldObject))
            {
                if (!TryResolveObjectAtCell(cell, out Vector2Int anchorCell, out oldObject))
                    return;

                cell = anchorCell;
            }

            SymbiozRuntimeLog.Write("BUILD", $"Delete request cell={cell.x}:{cell.y} kind={oldObject.Kind} location={currentLocation}");
            bool submittedToServer = false;
            if (IsServerAuthoritativeWorldMode())
            {
                submittedToServer = fishNetWorldBridge != null
                    && fishNetWorldBridge.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
                if (!submittedToServer)
                {
                    SymbiozRuntimeLog.Write("BUILD", $"Delete rejected because FishNet server is not connected cell={cell.x}:{cell.y}");
                    return;
                }
            }

            if (oldObject.Root != null)
                Destroy(oldObject.Root);

            placedObjects.Remove(cell);
            if (isInsideEstateInterior && activeEstateInteriorAnchor == cell)
            {
                isInsideEstateInterior = false;
                activeEstateInteriorKind = default;
                activeEstateInteriorAnchor = default;
            }
            if (isEditingNote && editingNoteCell == cell)
                CancelNoteDialog();

            RefreshBuildTilesAround(cell);
            if (!submittedToServer)
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
            SavePersistentWorld();
        }

        private bool CanPlaceObjectFootprint(Vector2Int anchorCell, ObjectKind kind, out string blockedReason)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int cell = anchorCell + new Vector2Int(x, y);
                    if (cell.x < 0 || cell.x >= GridSize || cell.y < 0 || cell.y >= GridSize)
                    {
                        blockedReason = $"out_of_grid at {cell.x}:{cell.y}";
                        return false;
                    }

                    if (cell == pawnCell)
                    {
                        blockedReason = $"pawn at {cell.x}:{cell.y}";
                        return false;
                    }

                    if (IsPortalReservedCell(cell))
                    {
                        blockedReason = $"portal_reserved at {cell.x}:{cell.y}";
                        return false;
                    }
                }
            }

            if (placedObjects != null)
            {
                foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
                {
                    if (pair.Value == null || pair.Key == anchorCell)
                        continue;

                    if (ObjectFootprintsOverlap(anchorCell, kind, pair.Key, pair.Value.Kind))
                    {
                        blockedReason = $"occupied by {pair.Value.Kind} at {pair.Key.x}:{pair.Key.y}";
                        return false;
                    }
                }
            }

            if (IsEstateKind(kind) && !CanReserveEstateClearance(anchorCell, kind, out blockedReason))
                return false;

            blockedReason = string.Empty;
            return true;
        }

        private bool CanReserveEstateClearance(Vector2Int anchorCell, ObjectKind kind, out string blockedReason)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            int sideClearance = 1;
            int frontClearance = 3;
            int minX = anchorCell.x - sideClearance;
            int maxX = anchorCell.x + size.x + sideClearance - 1;
            int minY = anchorCell.y - frontClearance;
            int maxY = anchorCell.y + size.y + sideClearance - 1;

            if (minX < 0 || minY < 0 || maxX >= GridSize || maxY >= GridSize)
            {
                blockedReason = "estate_clearance_out_of_grid";
                return false;
            }

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    bool insideHouse = cell.x >= anchorCell.x
                        && cell.y >= anchorCell.y
                        && cell.x < anchorCell.x + size.x
                        && cell.y < anchorCell.y + size.y;

                    if (insideHouse)
                        continue;

                    if (IsPortalReservedCell(cell))
                    {
                        blockedReason = $"estate_clearance_portal at {cell.x}:{cell.y}";
                        return false;
                    }

                    if (placedObjects == null)
                        continue;

                    foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
                    {
                        if (pair.Value == null || pair.Key == anchorCell)
                            continue;

                        if (ContainsCellInObjectFootprint(pair.Key, pair.Value.Kind, cell))
                        {
                            blockedReason = $"estate_clearance_blocked by {pair.Value.Kind} at {pair.Key.x}:{pair.Key.y}";
                            return false;
                        }
                    }
                }
            }

            blockedReason = string.Empty;
            return true;
        }

        private bool TrySpendBuildDeedResources(ObjectKind kind)
        {
            if (!TryGetBuildDeedCost(kind, out int stoneCost, out int woodCost))
                return true;

            if (carriedStone < stoneCost || carriedWood < woodCost)
            {
                if (buildSelectionText != null)
                    buildSelectionText.text = $"Need {stoneCost} stone and {woodCost} wood. Bag: {carriedStone} stone, {carriedWood} wood.";
                return false;
            }

            carriedStone -= stoneCost;
            carriedWood -= woodCost;
            SaveResourceInventory();
            SymbiozRuntimeLog.Write("BUILD", $"Deed paid kind={kind} stone={stoneCost} wood={woodCost} leftStone={carriedStone} leftWood={carriedWood}");
            return true;
        }

        private static bool TryGetBuildDeedCost(ObjectKind kind, out int stoneCost, out int woodCost)
        {
            switch (kind)
            {
                case ObjectKind.SmallHouse:
                    stoneCost = 12;
                    woodCost = 25;
                    return true;
                case ObjectKind.StoneTower:
                    stoneCost = 80;
                    woodCost = 55;
                    return true;
                case ObjectKind.StoneKeep:
                    stoneCost = 200;
                    woodCost = 120;
                    return true;
                case ObjectKind.Castle:
                    stoneCost = 420;
                    woodCost = 260;
                    return true;
                default:
                    stoneCost = 0;
                    woodCost = 0;
                    return false;
            }
        }

        private void UpdateMateriaGenerator()
        {
            if (Time.unscaledTime < nextMateriaGeneratorTime)
                return;

            nextMateriaGeneratorTime = Time.unscaledTime + MateriaGeneratorIntervalSeconds;
            EnsureMateriaBerryBushes(false);
            EnsureMateriaResourceTrees(false);
        }

        private void GenerateMateriaForCurrentLocation()
        {
            EnsureMateriaBerryBushes(true);
            EnsureMateriaResourceTrees(true);
        }

        private void EnsureMateriaBerryBushes(bool forceLog)
        {
            if (placedObjects == null)
                return;

            int playersOnMap = CountPlayersOnCurrentLocation();
            int targetBushes = Mathf.Max(MateriaBerryBushesPerPlayer, playersOnMap * MateriaBerryBushesPerPlayer);
            int currentBushes = CountBerryBushesWithFood();
            int missing = Mathf.Max(0, targetBushes - currentBushes);
            if (missing <= 0)
            {
                if (forceLog)
                    SymbiozRuntimeLog.Write("MATERIA", $"Berry bushes stable. players={playersOnMap} bushes={currentBushes}/{targetBushes}");
                return;
            }

            int spawned = 0;
            while (spawned < missing)
            {
                int clusterSize = Mathf.Min(MateriaBerryBushesPerPlayer, missing - spawned);
                if (!TryFindMateriaSpawnCluster(clusterSize, out List<Vector2Int> cells))
                    break;

                for (int i = 0; i < cells.Count; i++)
                {
                    Vector2Int cell = cells[i];
                    PlacedObject bush = CreateBerryBushObject(cell, BerryBushMaxBerries);
                    placedObjects[cell] = bush;
                    spawned++;
                    fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)ObjectKind.BerryBush, bush.Note ?? string.Empty);
                }
            }

            if (spawned > 0)
            {
                SavePersistentWorld();
                SymbiozRuntimeLog.Write("MATERIA", $"Spawned berry bushes={spawned} players={playersOnMap} target={targetBushes} edgeBand={MateriaEdgeBandCells} clusterRadius={MateriaBerryClusterRadiusCells}");
            }
            else if (forceLog)
            {
                SymbiozRuntimeLog.Write("MATERIA", "No free edge cells for berry bushes.");
            }
        }

        private void EnsureMateriaResourceTrees(bool forceLog)
        {
            if (placedObjects == null)
                return;

            int removedInvalidTrees = CleanupInvalidResourceTrees();
            int playersOnMap = CountPlayersOnCurrentLocation();
            int targetTrees = Mathf.Max(MateriaTreeMinimum, playersOnMap * MateriaTreesPerPlayer);
            Vector2Int treeFocusCell = GetMateriaTreeFocusCell();
            int visibleTrees = CountResourceTreesNear(treeFocusCell, MateriaVisibleTreeRadiusCells);
            int missingVisible = Mathf.Max(0, MateriaVisibleTreeMinimum - visibleTrees);
            int currentTrees = CountResourceTrees();
            int missing = Mathf.Max(0, targetTrees - currentTrees);
            if (missing <= 0 && missingVisible <= 0)
            {
                if (forceLog)
                    SymbiozRuntimeLog.Write("MATERIA", $"Trees stable. players={playersOnMap} trees={currentTrees}/{targetTrees} visible={visibleTrees}/{MateriaVisibleTreeMinimum}");
                return;
            }

            int spawned = 0;
            while (spawned < missingVisible && TryFindTreeSpawnCellNear(treeFocusCell, MateriaVisibleTreeRadiusCells, out Vector2Int cell))
            {
                PlacedObject tree = CreateTreeObject(cell, UnityEngine.Random.Range(0, TreeVariantCount), TreeWoodYield);
                placedObjects[cell] = tree;
                spawned++;
                fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)ObjectKind.Tree, tree.Note ?? string.Empty);
            }

            while (spawned < missing && TryFindTreeSpawnCell(out Vector2Int cell))
            {
                PlacedObject tree = CreateTreeObject(cell, UnityEngine.Random.Range(0, TreeVariantCount), TreeWoodYield);
                placedObjects[cell] = tree;
                spawned++;
                fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)ObjectKind.Tree, tree.Note ?? string.Empty);
            }

            if (spawned > 0 || removedInvalidTrees > 0)
            {
                SavePersistentWorld();
                SymbiozRuntimeLog.Write("MATERIA", $"Spawned trees={spawned} removedInvalidTrees={removedInvalidTrees} players={playersOnMap} target={targetTrees} visibleTarget={MateriaVisibleTreeMinimum}; empty-cell-only rule enforced.");
            }
            else if (forceLog)
            {
                SymbiozRuntimeLog.Write("MATERIA", "No free empty cells for trees.");
            }
        }

        private int CleanupInvalidResourceTrees()
        {
            if (placedObjects == null)
                return 0;

            List<Vector2Int> invalidCells = null;
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value == null || pair.Value.Kind != ObjectKind.Tree)
                    continue;

                if (IsInsideTreeSpawnBounds(pair.Key))
                    continue;

                invalidCells ??= new List<Vector2Int>();
                invalidCells.Add(pair.Key);
            }

            if (invalidCells == null)
                return 0;

            for (int i = 0; i < invalidCells.Count; i++)
            {
                Vector2Int cell = invalidCells[i];
                if (!placedObjects.TryGetValue(cell, out PlacedObject tree))
                    continue;

                if (tree.Root != null)
                    Destroy(tree.Root);

                placedObjects.Remove(cell);
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
            }

            return invalidCells.Count;
        }

        private int CountPlayersOnCurrentLocation()
        {
            return Mathf.Max(1, 1 + remotePawns.Count);
        }

        private int CountBerryBushesWithFood()
        {
            int count = 0;
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value != null
                    && pair.Value.Kind == ObjectKind.BerryBush
                    && ParseBerryBushBerries(pair.Value.Note) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountResourceTrees()
        {
            int count = 0;
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value != null
                    && pair.Value.Kind == ObjectKind.Tree
                    && ParseTreeWood(pair.Value.Note) > 0
                    && IsInsideTreeSpawnBounds(pair.Key))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountResourceTreesNear(Vector2Int origin, int radiusCells)
        {
            int count = 0;
            int sqrRadius = radiusCells * radiusCells;
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value == null
                    || pair.Value.Kind != ObjectKind.Tree
                    || ParseTreeWood(pair.Value.Note) <= 0
                    || !IsInsideTreeSpawnBounds(pair.Key))
                {
                    continue;
                }

                Vector2Int delta = pair.Key - origin;
                if (delta.sqrMagnitude <= sqrRadius)
                    count++;
            }

            return count;
        }

        private bool TryFindMateriaSpawnCell(out Vector2Int cell)
        {
            for (int attempt = 0; attempt < 90; attempt++)
            {
                int side = UnityEngine.Random.Range(0, 4);
                int x;
                int y;
                switch (side)
                {
                    case 0:
                        x = UnityEngine.Random.Range(0, GridSize);
                        y = UnityEngine.Random.Range(0, MateriaEdgeBandCells);
                        break;
                    case 1:
                        x = UnityEngine.Random.Range(0, GridSize);
                        y = UnityEngine.Random.Range(GridSize - MateriaEdgeBandCells, GridSize);
                        break;
                    case 2:
                        x = UnityEngine.Random.Range(0, MateriaEdgeBandCells);
                        y = UnityEngine.Random.Range(0, GridSize);
                        break;
                    default:
                        x = UnityEngine.Random.Range(GridSize - MateriaEdgeBandCells, GridSize);
                        y = UnityEngine.Random.Range(0, GridSize);
                        break;
                }

                cell = ClampCell(new Vector2Int(x, y));
                if (!IsMateriaSpawnCellFree(cell))
                    continue;

                return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool TryFindTreeSpawnCell(out Vector2Int cell)
        {
            for (int attempt = 0; attempt < 180; attempt++)
            {
                cell = ClampCell(new Vector2Int(
                    UnityEngine.Random.Range(MateriaTreeEdgeMarginCells, GridSize - MateriaTreeEdgeMarginCells),
                    UnityEngine.Random.Range(MateriaTreeEdgeMarginCells, GridSize - MateriaTreeEdgeMarginCells)));
                if (IsMateriaTreeSpawnCellFree(cell))
                    return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool TryFindTreeSpawnCellNear(Vector2Int origin, int radiusCells, out Vector2Int cell)
        {
            for (int attempt = 0; attempt < 220; attempt++)
            {
                int radius = UnityEngine.Random.Range(5, radiusCells + 1);
                int x = origin.x + UnityEngine.Random.Range(-radius, radius + 1);
                int y = origin.y + UnityEngine.Random.Range(-radius, radius + 1);
                cell = new Vector2Int(x, y);
                if (!IsInsideTreeSpawnBounds(cell))
                    continue;

                if (Vector2Int.Distance(origin, cell) > radiusCells)
                    continue;

                if (IsMateriaTreeSpawnCellFree(cell))
                    return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool TryFindMateriaSpawnCluster(int requestedCount, out List<Vector2Int> cells)
        {
            cells = new List<Vector2Int>(Mathf.Max(1, requestedCount));
            requestedCount = Mathf.Clamp(requestedCount, 1, MateriaBerryBushesPerPlayer);

            for (int attempt = 0; attempt < 70; attempt++)
            {
                if (!TryFindMateriaSpawnCell(out Vector2Int anchor))
                    break;

                cells.Clear();
                cells.Add(anchor);

                for (int pass = 0; pass < 90 && cells.Count < requestedCount; pass++)
                {
                    Vector2Int offset = new Vector2Int(
                        UnityEngine.Random.Range(-MateriaBerryClusterRadiusCells, MateriaBerryClusterRadiusCells + 1),
                        UnityEngine.Random.Range(-MateriaBerryClusterRadiusCells, MateriaBerryClusterRadiusCells + 1));

                    Vector2Int candidate = ClampCell(anchor + offset);
                    if (!IsMateriaSpawnCellFree(candidate) || cells.Contains(candidate))
                        continue;

                    bool closeToCluster = true;
                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (Mathf.Abs(candidate.x - cells[i].x) > MateriaBerryClusterRadiusCells
                            || Mathf.Abs(candidate.y - cells[i].y) > MateriaBerryClusterRadiusCells)
                        {
                            closeToCluster = false;
                            break;
                        }
                    }

                    if (closeToCluster)
                        cells.Add(candidate);
                }

                if (cells.Count == requestedCount)
                    return true;
            }

            cells.Clear();
            return false;
        }

        private bool IsMateriaSpawnCellFree(Vector2Int cell)
        {
            return placedObjects != null
                && CanPlaceObjectFootprint(cell, ObjectKind.Tree, out _);
        }

        private bool IsMateriaTreeSpawnCellFree(Vector2Int cell)
        {
            return IsInsideTreeSpawnBounds(cell)
                && IsMateriaSpawnCellFree(cell);
        }

        private static bool IsInsideTreeSpawnBounds(Vector2Int cell)
        {
            return cell.x >= MateriaTreeEdgeMarginCells
                && cell.y >= MateriaTreeEdgeMarginCells
                && cell.x < GridSize - MateriaTreeEdgeMarginCells
                && cell.y < GridSize - MateriaTreeEdgeMarginCells;
        }

        private void EatSelectedBerryBush()
        {
            if (!TryGetSelectedBerryBush(out PlacedObject bush, out int berries))
                return;

            if (!IsSelectedObjectInInteractRange())
            {
                SymbiozRuntimeLog.Write("MATERIA", "Move closer to eat berries.");
                return;
            }

            berries--;
            pawnSatiety = Mathf.Min(SatietyMax, pawnSatiety + 24f);
            UpdateBerryBushAfterUse(selectedCell, bush, berries);
            SymbiozRuntimeLog.Write("MATERIA", $"Ate berries. satiety={pawnSatiety:0} bushBerries={Mathf.Max(0, berries)}");
        }

        private void GatherSelectedBerryBush()
        {
            if (!TryGetSelectedBerryBush(out PlacedObject bush, out int berries))
                return;

            if (!IsSelectedObjectInInteractRange())
            {
                SymbiozRuntimeLog.Write("MATERIA", "Move closer to gather berries.");
                return;
            }

            int canCarry = Mathf.FloorToInt((CarryWeightMax - carriedWeight) / BerryCarryWeight);
            int gathered = Mathf.Clamp(berries, 0, canCarry);
            if (gathered <= 0)
            {
                SymbiozRuntimeLog.Write("MATERIA", "Carry weight is full.");
                return;
            }

            carriedBerries += gathered;
            carriedWeight += gathered * BerryCarryWeight;
            berries -= gathered;
            UpdateBerryBushAfterUse(selectedCell, bush, berries);
            SymbiozRuntimeLog.Write("MATERIA", $"Gathered berries={gathered} carry={carriedWeight:0.0}/{CarryWeightMax:0}");
        }

        private bool TryGetSelectedBerryBush(out PlacedObject bush, out int berries)
        {
            bush = null;
            berries = 0;
            if (placedObjects == null || !placedObjects.TryGetValue(selectedCell, out bush) || bush.Kind != ObjectKind.BerryBush)
                return false;

            berries = ParseBerryBushBerries(bush.Note);
            return berries > 0;
        }

        private bool IsSelectedObjectInInteractRange()
        {
            return Vector2Int.Distance(pawnCell, selectedCell) <= BerryBushInteractDistanceCells;
        }

        private bool IsSelectedObjectInInteractRange(float distanceCells)
        {
            Vector2Int anchor = hasSelectedObject ? selectedObjectAnchorCell : selectedCell;
            ObjectKind kind = hasSelectedObject
                && placedObjects != null
                && placedObjects.TryGetValue(anchor, out PlacedObject placed)
                && placed != null
                    ? placed.Kind
                    : ObjectKind.BerryBush;
            return DistanceFromCellToObjectFootprint(pawnCell, anchor, kind) <= distanceCells;
        }

        private void UpdateBerryBushAfterUse(Vector2Int cell, PlacedObject bush, int berries)
        {
            berries = Mathf.Clamp(berries, 0, BerryBushMaxBerries);
            if (berries <= 0)
            {
                if (bush.Root != null)
                    Destroy(bush.Root);

                placedObjects.Remove(cell);
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
                SavePersistentWorld();
                return;
            }

            bush.Note = FormatBerryBushNote(berries);
            if (bush.Root != null)
                Destroy(bush.Root);

            bush.Root = CreateBerryBush(cell, berries);
            fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)ObjectKind.BerryBush, bush.Note);
            SavePersistentWorld();
        }

        private void StartChopSelectedTree()
        {
            Vector2Int cell = hasSelectedObject ? selectedObjectAnchorCell : selectedCell;
            if (placedObjects == null || !placedObjects.TryGetValue(cell, out PlacedObject tree) || tree.Kind != ObjectKind.Tree)
                return;

            if (!IsSelectedObjectInInteractRange(TreeInteractDistanceCells))
            {
                SymbiozRuntimeLog.Write("MATERIA", "Move closer to chop tree.");
                return;
            }

            if (hasActiveTreeChop && activeTreeChopCell == cell)
            {
                float remaining = Mathf.Max(0f, activeTreeChopFinishTime - Time.unscaledTime);
                SymbiozRuntimeLog.Write("MATERIA", $"Tree chopping already active. remaining={remaining:0}s cell={cell.x}:{cell.y}");
                return;
            }

            hasActiveTreeChop = true;
            activeTreeChopCell = cell;
            activeTreeChopFinishTime = Time.unscaledTime + TreeChopSeconds;
            SymbiozRuntimeLog.Write("MATERIA", $"Tree chopping started. cell={cell.x}:{cell.y} duration={TreeChopSeconds:0}s reward={TreeWoodYield} logs");
        }

        private void UpdateTreeChopProgress()
        {
            if (!hasActiveTreeChop || Time.unscaledTime < activeTreeChopFinishTime)
                return;

            Vector2Int cell = activeTreeChopCell;
            hasActiveTreeChop = false;
            if (placedObjects == null || !placedObjects.TryGetValue(cell, out PlacedObject tree) || tree.Kind != ObjectKind.Tree)
                return;

            int wood = Mathf.Max(TreeWoodYield, ParseTreeWood(tree.Note));
            bool submittedToServer = false;
            if (IsServerAuthoritativeWorldMode())
            {
                submittedToServer = fishNetWorldBridge != null
                    && fishNetWorldBridge.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
                if (!submittedToServer)
                {
                    SymbiozRuntimeLog.Write("MATERIA", $"Tree chop reward blocked because server is not connected cell={cell.x}:{cell.y}");
                    return;
                }
            }

            if (tree.Root != null)
                Destroy(tree.Root);

            placedObjects.Remove(cell);
            carriedWood += wood;
            carriedWeight = Mathf.Min(CarryWeightMax, carriedWeight + wood);
            SaveResourceInventory();
            if (!submittedToServer)
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
            SavePersistentWorld();
            SymbiozRuntimeLog.Write("MATERIA", $"Tree chopped. logs={wood} carriedWood={carriedWood} cell={cell.x}:{cell.y}");
        }

        internal void ApplyServerWorldDelta(int locationValue, int x, int y, int kindValue, string note, bool removed)
        {
            if (!Enum.IsDefined(typeof(LocationId), locationValue))
                return;

            if (!Enum.IsDefined(typeof(ObjectKind), kindValue) && !removed)
                return;

            LocationId location = (LocationId)locationValue;
            Vector2Int cell = ClampCell(new Vector2Int(x, y));
            if (IsPortalReservedCell(cell, location))
                return;

            Dictionary<Vector2Int, PlacedObject> locationObjects = placedObjectsByLocation[location];
            bool isCurrentLocation = location == currentLocation;

            if (locationObjects.TryGetValue(cell, out PlacedObject oldObject))
            {
                if (isCurrentLocation && oldObject.Root != null)
                    Destroy(oldObject.Root);

                locationObjects.Remove(cell);
            }

            if (!removed)
            {
                ObjectKind kind = (ObjectKind)kindValue;
                PlacedObject placed;
                if (isCurrentLocation)
                {
                    placed = kind == ObjectKind.Sign
                        ? CreateSign(cell)
                        : kind == ObjectKind.BerryBush
                            ? CreateBerryBushObject(cell, ParseBerryBushBerries(note))
                            : kind == ObjectKind.Tree
                                ? CreateTreeObject(cell, ParseTreeVariant(note), ParseTreeWood(note))
                                : CreateBlockObject(cell, kind);
                    placed.Note = note ?? string.Empty;
                    UpdateSignLabel(placed);
                    UpdateSleepingBagStatusLabel(placed);
                }
                else
                {
                    placed = new PlacedObject
                    {
                        Kind = kind,
                        Note = note ?? string.Empty
                    };
                }

                locationObjects[cell] = placed;
                if (isCurrentLocation && kind == ObjectKind.SleepingBag)
                    TryMergeSleepingBags(cell, false);
            }

            if (isCurrentLocation)
                RefreshBuildTilesAround(cell);

            SavePersistentWorld();
        }

        internal void ClearLocationFromServer(int locationValue)
        {
            if (!Enum.IsDefined(typeof(LocationId), locationValue))
                return;

            LocationId location = (LocationId)locationValue;
            Dictionary<Vector2Int, PlacedObject> locationObjects = placedObjectsByLocation[location];
            if (location == currentLocation)
            {
                foreach (PlacedObject placed in locationObjects.Values)
                {
                    if (placed != null && placed.Root != null)
                        Destroy(placed.Root);
                }
            }

            locationObjects.Clear();
        }

        internal void EnsureLocationDefaultsFromServer(int locationValue)
        {
            if (!Enum.IsDefined(typeof(LocationId), locationValue))
                return;

            LocationId location = (LocationId)locationValue;
            if (!placedObjectsByLocation.TryGetValue(location, out Dictionary<Vector2Int, PlacedObject> locationObjects))
                return;

            int countBefore = locationObjects.Count;
            EnsureLocationDefaults(location);
            if (locationObjects.Count == countBefore || location != currentLocation || objectsRoot == null)
                return;

            RebuildPlacedObjectsForCurrentLocation();
            SelectCell(selectedCell);
        }

        private PlacedObject CreateBlockObject(Vector2Int cell, ObjectKind kind)
        {
            GameObject obj = kind switch
            {
                ObjectKind.Wall => CreateBrickWall(cell),
                ObjectKind.Repair => CreateRepairPlanks(cell),
                ObjectKind.BuildPlot => CreateBuildPlotMarker(cell),
                ObjectKind.SleepingBag => CreateSleepingBag(cell),
                ObjectKind.Tent => CreateTent(cell),
                ObjectKind.BerryBush => CreateBerryBush(cell, BerryBushMaxBerries),
                ObjectKind.Tree => CreateResourceTree(cell, 0),
                ObjectKind.CommunityStorage => CreateCommunityStorage(cell),
                ObjectKind.SlumTradeCenter => CreateSlumTradeCenter(cell),
                ObjectKind.StoneQuarry => CreateResourceWorksite(cell, ObjectKind.StoneQuarry),
                ObjectKind.Sawmill => CreateResourceWorksite(cell, ObjectKind.Sawmill),
                ObjectKind.SmallHouse => CreateEstateBuilding(cell, ObjectKind.SmallHouse),
                ObjectKind.StoneTower => CreateEstateBuilding(cell, ObjectKind.StoneTower),
                ObjectKind.StoneKeep => CreateEstateBuilding(cell, ObjectKind.StoneKeep),
                ObjectKind.Castle => CreateEstateBuilding(cell, ObjectKind.Castle),
                _ => CreateRoadTile(cell)
            };

            return new PlacedObject
            {
                Kind = kind,
                Root = obj,
                Note = kind == ObjectKind.BerryBush
                    ? FormatBerryBushNote(BerryBushMaxBerries)
                    : kind == ObjectKind.Tree
                        ? FormatTreeNote(0, TreeWoodYield)
                        : string.Empty
            };
        }

        private PlacedObject CreateBerryBushObject(Vector2Int cell, int berries)
        {
            berries = Mathf.Clamp(berries, 0, BerryBushMaxBerries);
            return new PlacedObject
            {
                Kind = ObjectKind.BerryBush,
                Root = CreateBerryBush(cell, berries),
                Note = FormatBerryBushNote(berries)
            };
        }

        private GameObject CreateBerryBush(Vector2Int cell, int berries)
        {
            var root = new GameObject($"BerryBush_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadow.name = "BushShadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.024f, 0f);
            shadow.transform.localScale = new Vector3(0.78f, 0.028f, 0.7f);
            SetRendererColor(shadow, new Color(0.06f, 0.055f, 0.04f, 0.95f));

            Color[] greens =
            {
                new Color(0.15f, 0.32f, 0.12f, 1f),
                new Color(0.21f, 0.43f, 0.17f, 1f),
                new Color(0.11f, 0.25f, 0.1f, 1f),
                new Color(0.28f, 0.5f, 0.2f, 1f)
            };

            for (int i = 0; i < 9; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = $"LeafCluster_{i}";
                leaf.transform.SetParent(root.transform, false);
                float angle = i * 41f * Mathf.Deg2Rad;
                float radius = i == 0 ? 0f : 0.22f + (i % 3) * 0.04f;
                leaf.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.16f + (i % 3) * 0.035f, Mathf.Sin(angle) * radius);
                leaf.transform.localScale = new Vector3(0.34f, 0.18f, 0.28f);
                SetRendererColor(leaf, greens[i % greens.Length]);
            }

            for (int i = 0; i < berries; i++)
            {
                GameObject berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                berry.name = $"Berry_{i}";
                berry.transform.SetParent(root.transform, false);
                float angle = (70f + i * 128f) * Mathf.Deg2Rad;
                berry.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.18f, 0.31f, Mathf.Sin(angle) * 0.18f);
                berry.transform.localScale = new Vector3(0.085f, 0.085f, 0.085f);
                SetRendererColor(berry, new Color(0.72f, 0.08f, 0.09f, 1f));
            }

            return root;
        }

        private static string FormatBerryBushNote(int berries)
        {
            return $"berries={Mathf.Clamp(berries, 0, BerryBushMaxBerries)}";
        }

        private static int ParseBerryBushBerries(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return BerryBushMaxBerries;

            string[] parts = note.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (!part.StartsWith("berries=", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (int.TryParse(part.Substring("berries=".Length), out int berries))
                    return Mathf.Clamp(berries, 0, BerryBushMaxBerries);
            }

            return BerryBushMaxBerries;
        }

        private PlacedObject CreateTreeObject(Vector2Int cell, int variant, int wood)
        {
            variant = Mathf.Clamp(variant, 0, TreeVariantCount - 1);
            wood = Mathf.Max(0, wood);
            return new PlacedObject
            {
                Kind = ObjectKind.Tree,
                Root = CreateResourceTree(cell, variant),
                Note = FormatTreeNote(variant, wood)
            };
        }

        private GameObject CreateResourceTree(Vector2Int cell, int variant)
        {
            variant = Mathf.Clamp(variant, 0, TreeVariantCount - 1);
            var root = new GameObject($"Tree_{variant}_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            Material treeMaterial = ResolveResourceTreeMaterial(variant);
            if (treeMaterial == null)
            {
                CreateTreePhysicalLayer(root.transform, variant);
                return root;
            }

            GameObject sprite = new GameObject("TreeSprite", typeof(MeshFilter), typeof(MeshRenderer));
            sprite.transform.SetParent(root.transform, false);
            sprite.transform.localPosition = new Vector3(0f, 0.08f, 0.24f);
            sprite.transform.localRotation = ResolveCameraFacingBillboardRotation();
            float height = ResolveTreeSpriteHeight(variant);
            float width = height * ResolveTreeSpriteAspect(variant);
            sprite.GetComponent<MeshFilter>().sharedMesh = CreateVerticalQuadMesh($"Tree_{variant}_Mesh", width, height, ResolveTreeUv(variant));
            BoxCollider hitbox = sprite.AddComponent<BoxCollider>();
            hitbox.center = new Vector3(0f, height * 0.5f, 0f);
            hitbox.size = new Vector3(width, height, 0.35f);
            MeshRenderer renderer = sprite.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = treeMaterial;
            renderer.sortingOrder = ResolveWorldSortOrder(root.transform.position.z + 0.55f);
            return root;
        }

        private Material ResolveResourceTreeMaterial(int variant)
        {
            if (resourceTreeMaterials == null || resourceTreeMaterials.Length == 0)
                return null;

            int index = Mathf.Clamp(variant, 0, resourceTreeMaterials.Length - 1);
            return resourceTreeMaterials[index];
        }

        private GameObject CreateFallbackTree(Vector2Int cell, int variant)
        {
            var root = new GameObject($"TreeFallback_{variant}_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);
            CreateTreePhysicalLayer(root.transform, variant);
            return root;
        }

        private void CreateTreePhysicalLayer(Transform root, int variant)
        {
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            trunk.transform.localScale = new Vector3(0.16f, 0.48f, 0.16f);
            SetRendererColor(trunk, new Color(0.34f, 0.22f, 0.12f, 1f));

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(root, false);
            canopy.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            canopy.transform.localScale = new Vector3(0.72f, variant == 1 ? 1.2f : 0.58f, 0.72f);
            Color[] colors =
            {
                new Color(0.22f, 0.42f, 0.16f, 1f),
                new Color(0.12f, 0.28f, 0.12f, 1f),
                new Color(0.42f, 0.33f, 0.18f, 1f),
                new Color(0.13f, 0.25f, 0.11f, 1f),
                new Color(0.55f, 0.6f, 0.18f, 1f)
            };
            SetRendererColor(canopy, colors[Mathf.Clamp(variant, 0, colors.Length - 1)]);
        }

        private static string FormatTreeNote(int variant, int wood)
        {
            return $"tree={Mathf.Clamp(variant, 0, TreeVariantCount - 1)};wood={Mathf.Max(0, wood)}";
        }

        private static int ParseTreeVariant(string note)
        {
            Dictionary<string, string> values = ParseNoteKeyValues(note ?? string.Empty);
            return Mathf.Clamp(ParseNoteInt(values, "tree", 0), 0, TreeVariantCount - 1);
        }

        private static int ParseTreeWood(string note)
        {
            Dictionary<string, string> values = ParseNoteKeyValues(note ?? string.Empty);
            return Mathf.Max(0, ParseNoteInt(values, "wood", TreeWoodYield));
        }

        private static Vector2[] ResolveTreeUv(int variant)
        {
            return new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
        }

        private static float ResolveTreeSpriteAspect(int variant)
        {
            return Mathf.Clamp(variant, 0, TreeVariantCount - 1) switch
            {
                1 => 540f / 1165f,
                2 => 780f / 893f,
                3 => 1228f / 820f,
                4 => 990f / 1085f,
                _ => 1069f / 1021f
            };
        }

        private static float ResolveTreeSpriteHeight(int variant)
        {
            return Mathf.Clamp(variant, 0, TreeVariantCount - 1) switch
            {
                1 => 3.85f,
                2 => 3.05f,
                3 => 2.85f,
                4 => 3.55f,
                _ => 3.35f
            };
        }

        private GameObject CreateAtlasTile(string name, Vector2Int cell, Material material, int tileIndex, float y, float scale, Color fallbackColor)
        {
            if (material == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetParent(objectsRoot, false);
                fallback.transform.position = CellToWorld(cell) + new Vector3(0f, y, 0f);
                fallback.transform.localScale = new Vector3(scale, 0.035f, scale);
                SetRendererColor(fallback, fallbackColor);
                return fallback;
            }

            var root = new GameObject(name);
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject tile = new GameObject("AtlasQuad", typeof(MeshFilter), typeof(MeshRenderer));
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(0f, y, 0f);

            float half = scale * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";
            mesh.vertices = new[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(-half, 0f, half),
                new Vector3(half, 0f, half),
                new Vector3(half, 0f, -half)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = ResolveAtlasUv(tileIndex);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            tile.GetComponent<MeshFilter>().sharedMesh = mesh;
            tile.GetComponent<MeshRenderer>().sharedMaterial = material;
            return root;
        }

        private GameObject CreateWallAtlasTile(string name, Vector2Int cell, int tileIndex, bool east, bool west)
        {
            if (wallTileMaterial == null)
                return CreateLegacyBrickWall(cell);

            var root = new GameObject(name);
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject tile = new GameObject("WallAtlasQuad", typeof(MeshFilter), typeof(MeshRenderer));
            tile.transform.SetParent(root.transform, false);
            float layerLift = east || west ? 0.22f : 0f;
            tile.transform.localPosition = new Vector3(0f, 0.13f + layerLift, 0.5f);
            tile.GetComponent<MeshFilter>().sharedMesh = CreateTexturedQuadMesh($"{name}_Mesh", 1.12f, 2.08f, ResolveAtlasUv(tileIndex));
            MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = wallTileMaterial;
            renderer.sortingOrder = ResolveWorldSortOrder(CellToWorld(cell).z + 1.05f);
            return root;
        }

        private GameObject CreateShelterTile(string name, Vector2Int anchorCell, Vector3 worldOffset, Vector2 worldSize, Rect uvRect, Color fallbackColor)
        {
            if (shelterTileMaterial == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetParent(objectsRoot, false);
                fallback.transform.position = CellToWorld(anchorCell) + worldOffset + new Vector3(0f, 0.045f, 0f);
                fallback.transform.localScale = new Vector3(worldSize.x, 0.05f, worldSize.y);
                SetRendererColor(fallback, fallbackColor);
                return fallback;
            }

            var root = new GameObject(name);
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(anchorCell) + worldOffset;

            GameObject tile = new GameObject("ShelterQuad", typeof(MeshFilter), typeof(MeshRenderer));
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(0f, 0.09f, 0f);

            tile.GetComponent<MeshFilter>().sharedMesh = CreateTexturedQuadMesh($"{name}_Mesh", worldSize.x, worldSize.y, RectToUv(uvRect, false));
            tile.GetComponent<MeshRenderer>().sharedMaterial = shelterTileMaterial;
            return root;
        }

        private static Mesh CreateTexturedQuadMesh(string name, float width, float height, Vector2[] uv)
        {
            float halfX = width * 0.5f;
            float halfZ = height * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new[]
            {
                new Vector3(-halfX, 0f, -halfZ),
                new Vector3(-halfX, 0f, halfZ),
                new Vector3(halfX, 0f, halfZ),
                new Vector3(halfX, 0f, -halfZ)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreatePawnQuadMesh(string name, Vector2[] uv)
        {
            float halfX = PawnSpriteWidth * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new[]
            {
                new Vector3(-halfX, 0f, 0f),
                new Vector3(-halfX, PawnSpriteHeight, 0f),
                new Vector3(halfX, PawnSpriteHeight, 0f),
                new Vector3(halfX, 0f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateVerticalQuadMesh(string name, float width, float height, Vector2[] uv)
        {
            float halfX = width * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new[]
            {
                new Vector3(-halfX, 0f, 0f),
                new Vector3(-halfX, height, 0f),
                new Vector3(halfX, height, 0f),
                new Vector3(halfX, 0f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2[] RectToUv(Rect uvRect, bool flipX)
        {
            float left = flipX ? uvRect.xMax : uvRect.xMin;
            float right = flipX ? uvRect.xMin : uvRect.xMax;
            return new[]
            {
                new Vector2(left, uvRect.yMin),
                new Vector2(left, uvRect.yMax),
                new Vector2(right, uvRect.yMax),
                new Vector2(right, uvRect.yMin)
            };
        }

        private static Vector2[] ResolvePawnUv(int rowIndex, int frameIndex, bool flipX)
        {
            rowIndex = Mathf.Clamp(rowIndex, 0, PawnWalkRows - 1);
            frameIndex = Mathf.Clamp(frameIndex, 0, PawnWalkColumns - 1);
            float cellWidth = 1f / PawnWalkColumns;
            float cellHeight = 1f / PawnWalkRows;
            float uMin = frameIndex * cellWidth + cellWidth * PawnSpriteHorizontalCrop;
            float width = cellWidth * (1f - PawnSpriteHorizontalCrop * 2f);
            float vMin = 1f - (rowIndex + 1) * cellHeight + cellHeight * PawnSpriteBottomCrop;
            float height = cellHeight * (1f - PawnSpriteBottomCrop - PawnSpriteTopCrop);
            return RectToUv(new Rect(uMin, vMin, width, height), flipX);
        }

        private static Vector2[] ResolveAtlasUv(int tileIndex)
        {
            tileIndex = Mathf.Clamp(tileIndex, 0, TileAtlasGrid * TileAtlasGrid - 1);
            int col = tileIndex % TileAtlasGrid;
            int rowFromTop = tileIndex / TileAtlasGrid;
            float step = 1f / TileAtlasGrid;
            float u0 = col * step;
            float u1 = u0 + step;
            float v1 = 1f - rowFromTop * step;
            float v0 = v1 - step;

            return new[]
            {
                new Vector2(u0, v0),
                new Vector2(u0, v1),
                new Vector2(u1, v1),
                new Vector2(u1, v0)
            };
        }

        private static int ResolveConnectedTileIndex(bool north, bool east, bool south, bool west)
        {
            int connections = (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);
            if (connections == 0)
                return 0;
            if (north && south && !east && !west)
                return 1;
            if (east && west && !north && !south)
                return 2;
            if (north && east && !south && !west)
                return 3;
            if (east && south && !north && !west)
                return 4;
            if (south && west && !north && !east)
                return 5;
            if (west && north && !east && !south)
                return 6;
            if (north && east && south && west)
                return 11;
            if (connections == 3)
            {
                if (!north) return 7;
                if (!east) return 8;
                if (!south) return 9;
                return 10;
            }
            if (north) return 12;
            if (east) return 13;
            if (south) return 14;
            return 15;
        }

        private GameObject CreateBrickWall(Vector2Int cell)
        {
            bool north = IsWallAt(cell + Vector2Int.up);
            bool south = IsWallAt(cell + Vector2Int.down);
            bool east = IsWallAt(cell + Vector2Int.right);
            bool west = IsWallAt(cell + Vector2Int.left);
            bool hasConnections = north || south || east || west;

            if (!hasConnections)
            {
                north = true;
                south = true;
            }

            int tileIndex = ResolveConnectedTileIndex(north, east, south, west);
            return CreateWallAtlasTile($"ConnectedBrickWall_{cell.x:000}_{cell.y:000}", cell, tileIndex, east, west);
        }

        private GameObject CreateLegacyBrickWall(Vector2Int cell)
        {
            var root = new GameObject($"ConnectedBrickWall_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            bool north = IsWallAt(cell + Vector2Int.up);
            bool south = IsWallAt(cell + Vector2Int.down);
            bool east = IsWallAt(cell + Vector2Int.right);
            bool west = IsWallAt(cell + Vector2Int.left);
            bool hasConnections = north || south || east || west;

            if (!hasConnections)
            {
                north = true;
                south = true;
            }

            CreateWallPiece(root.transform, "WallFoundation", new Vector3(0f, 0.045f, 0f), new Vector3(0.86f, 0.08f, 0.86f), new Color(0.22f, 0.16f, 0.12f, 1f), false);
            CreateWallPiece(root.transform, "WallCore", new Vector3(0f, 0.43f, 0f), new Vector3(0.58f, 0.82f, 0.58f), new Color(0.52f, 0.2f, 0.14f, 1f), false);

            if (north)
                CreateWallPiece(root.transform, "WallArm_North", new Vector3(0f, 0.43f, 0.34f), new Vector3(0.58f, 0.82f, 0.7f), new Color(0.52f, 0.2f, 0.14f, 1f), false);
            if (south)
                CreateWallPiece(root.transform, "WallArm_South", new Vector3(0f, 0.43f, -0.34f), new Vector3(0.58f, 0.82f, 0.7f), new Color(0.52f, 0.2f, 0.14f, 1f), false);
            if (east)
                CreateWallPiece(root.transform, "WallArm_East", new Vector3(0.34f, 0.43f, 0f), new Vector3(0.7f, 0.82f, 0.58f), new Color(0.52f, 0.2f, 0.14f, 1f), true);
            if (west)
                CreateWallPiece(root.transform, "WallArm_West", new Vector3(-0.34f, 0.43f, 0f), new Vector3(0.7f, 0.82f, 0.58f), new Color(0.52f, 0.2f, 0.14f, 1f), true);

            CreateWallCap(root.transform, "WallTopDust", new Vector3(0f, 0.865f, 0f), new Vector3(0.54f, 0.035f, 0.54f));
            if (north)
                CreateWallCap(root.transform, "WallTop_North", new Vector3(0f, 0.865f, 0.34f), new Vector3(0.54f, 0.035f, 0.64f));
            if (south)
                CreateWallCap(root.transform, "WallTop_South", new Vector3(0f, 0.865f, -0.34f), new Vector3(0.54f, 0.035f, 0.64f));
            if (east)
                CreateWallCap(root.transform, "WallTop_East", new Vector3(0.34f, 0.865f, 0f), new Vector3(0.64f, 0.035f, 0.54f));
            if (west)
                CreateWallCap(root.transform, "WallTop_West", new Vector3(-0.34f, 0.865f, 0f), new Vector3(0.64f, 0.035f, 0.54f));

            return root;
        }

        private void CreateWallPiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, bool horizontal)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            SetRendererColor(piece, color);

            int stripeCount = 3;
            for (int i = 0; i < stripeCount; i++)
            {
                GameObject mortar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mortar.name = $"{name}_Mortar_{i}";
                mortar.transform.SetParent(parent, false);
                float offset = -0.22f + i * 0.22f;
                mortar.transform.localPosition = horizontal
                    ? localPosition + new Vector3(offset, 0.44f, 0f)
                    : localPosition + new Vector3(0f, 0.44f, offset);
                mortar.transform.localScale = horizontal
                    ? new Vector3(0.035f, 0.025f, Mathf.Min(localScale.z + 0.02f, 0.62f))
                    : new Vector3(Mathf.Min(localScale.x + 0.02f, 0.62f), 0.025f, 0.035f);
                SetRendererColor(mortar, new Color(0.72f, 0.58f, 0.48f, 1f));
            }
        }

        private void CreateWallCap(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = name;
            cap.transform.SetParent(parent, false);
            cap.transform.localPosition = localPosition;
            cap.transform.localScale = localScale;
            SetRendererColor(cap, new Color(0.66f, 0.29f, 0.2f, 1f));
        }

        private GameObject CreateRepairPlanks(Vector2Int cell)
        {
            return CreateLegacyRepairPlanks(cell);
        }

        private GameObject CreateLegacyRepairPlanks(Vector2Int cell)
        {
            var root = new GameObject($"RepairPlanks_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadow.name = "RepairPatchShadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.022f, 0f);
            shadow.transform.localScale = new Vector3(0.98f, 0.028f, 0.92f);
            SetRendererColor(shadow, new Color(0.13f, 0.1f, 0.075f, 1f));

            Color[] plankColors =
            {
                new Color(0.58f, 0.38f, 0.2f, 1f),
                new Color(0.66f, 0.44f, 0.23f, 1f),
                new Color(0.49f, 0.31f, 0.16f, 1f),
                new Color(0.62f, 0.41f, 0.22f, 1f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"RepairBoard_{i}";
                plank.transform.SetParent(root.transform, false);
                plank.transform.localPosition = new Vector3(-0.36f + i * 0.24f, 0.07f + i * 0.002f, i % 2 == 0 ? -0.02f : 0.035f);
                plank.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 2.5f : -2f, 0f);
                plank.transform.localScale = new Vector3(0.2f, 0.065f, 0.94f);
                SetRendererColor(plank, plankColors[i]);

                GameObject grain = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grain.name = $"BoardGrain_{i}";
                grain.transform.SetParent(plank.transform, false);
                grain.transform.localPosition = new Vector3(0f, 0.53f, 0.05f);
                grain.transform.localScale = new Vector3(0.72f, 0.08f, 0.035f);
                SetRendererColor(grain, new Color(0.26f, 0.16f, 0.08f, 1f));
            }

            for (int i = 0; i < 2; i++)
            {
                GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brace.name = $"CrossBrace_{i}";
                brace.transform.SetParent(root.transform, false);
                brace.transform.localPosition = new Vector3(0f, 0.13f, i == 0 ? -0.31f : 0.31f);
                brace.transform.localScale = new Vector3(0.92f, 0.045f, 0.09f);
                SetRendererColor(brace, new Color(0.38f, 0.24f, 0.12f, 1f));
            }

            for (int i = 0; i < 6; i++)
            {
                GameObject nail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nail.name = $"NailHead_{i}";
                nail.transform.SetParent(root.transform, false);
                nail.transform.localPosition = new Vector3(-0.36f + (i % 3) * 0.36f, 0.165f, i < 3 ? -0.31f : 0.31f);
                nail.transform.localScale = new Vector3(0.025f, 0.008f, 0.025f);
                SetRendererColor(nail, new Color(0.08f, 0.075f, 0.065f, 1f));
            }

            return root;
        }

        private GameObject CreateBuildPlotMarker(Vector2Int cell)
        {
            return CreateLegacyBuildPlotMarker(cell);
        }

        private GameObject CreateLegacyBuildPlotMarker(Vector2Int cell)
        {
            var root = new GameObject($"BuildPlot_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject baseShade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseShade.name = "ReservedSoilSquare";
            baseShade.transform.SetParent(root.transform, false);
            baseShade.transform.localPosition = new Vector3(0f, 0.026f, 0f);
            baseShade.transform.localScale = new Vector3(0.92f, 0.018f, 0.92f);
            SetRendererColor(baseShade, new Color(0.17f, 0.22f, 0.19f, 1f));

            CreateBuildPlotRail(root.transform, "PlotRail_North", new Vector3(0f, 0.07f, 0.46f), new Vector3(0.94f, 0.045f, 0.045f));
            CreateBuildPlotRail(root.transform, "PlotRail_South", new Vector3(0f, 0.07f, -0.46f), new Vector3(0.94f, 0.045f, 0.045f));
            CreateBuildPlotRail(root.transform, "PlotRail_East", new Vector3(0.46f, 0.07f, 0f), new Vector3(0.045f, 0.045f, 0.94f));
            CreateBuildPlotRail(root.transform, "PlotRail_West", new Vector3(-0.46f, 0.07f, 0f), new Vector3(0.045f, 0.045f, 0.94f));

            for (int i = 0; i < 4; i++)
            {
                GameObject stake = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stake.name = $"CornerStake_{i}";
                stake.transform.SetParent(root.transform, false);
                float x = i == 0 || i == 3 ? -0.46f : 0.46f;
                float z = i < 2 ? 0.46f : -0.46f;
                stake.transform.localPosition = new Vector3(x, 0.14f, z);
                stake.transform.localScale = new Vector3(0.08f, 0.24f, 0.08f);
                SetRendererColor(stake, new Color(0.72f, 0.57f, 0.28f, 1f));
            }

            GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cross.name = "PlanningString";
            cross.transform.SetParent(root.transform, false);
            cross.transform.localPosition = new Vector3(0f, 0.105f, 0f);
            cross.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            cross.transform.localScale = new Vector3(0.035f, 0.028f, 1.16f);
            SetRendererColor(cross, new Color(0.88f, 0.82f, 0.52f, 1f));

            return root;
        }

        private void CreateBuildPlotRail(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = localPosition;
            rail.transform.localScale = localScale;
            SetRendererColor(rail, new Color(0.89f, 0.78f, 0.38f, 1f));
        }

        private GameObject CreateSleepingBag(Vector2Int cell)
        {
            return CreateShelterTile(
                $"SleepingBag_{cell.x:000}_{cell.y:000}",
                cell,
                new Vector3(0f, 0f, 0.5f),
                new Vector2(0.94f, 1.92f),
                new Rect(0.02f, 0.08f, 0.32f, 0.82f),
                new Color(0.34f, 0.42f, 0.22f, 1f));
        }

        private void UpdateSleepingBagStatusLabel(PlacedObject placed)
        {
            if (placed == null || placed.Root == null || placed.Kind != ObjectKind.SleepingBag)
                return;

            if (!TryParseSleeperProfile(placed.Note, out PlayerSleeperProfile profile))
                return;

            if (placed.Label == null)
            {
                var labelObject = new GameObject("SleeperStatusLabel");
                labelObject.transform.SetParent(placed.Root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.18f, -0.42f);
                labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                placed.Label = labelObject.AddComponent<TextMeshPro>();
                placed.Label.alignment = TextAlignmentOptions.Center;
                placed.Label.fontSize = 0.18f;
                placed.Label.color = new Color(0.86f, 0.96f, 1f, 1f);
                placed.Label.rectTransform.sizeDelta = new Vector2(1.8f, 0.5f);
            }

            placed.Label.text = string.IsNullOrWhiteSpace(profile.Nick) ? "Offline player" : profile.Nick;
        }

        private GameObject CreateTent(Vector2Int cell)
        {
            return CreateShelterTile(
                $"PairedTent_{cell.x:000}_{cell.y:000}",
                cell,
                new Vector3(0.5f, 0f, 1f),
                new Vector2(2.15f, 3.05f),
                new Rect(0.36f, 0.06f, 0.62f, 0.88f),
                new Color(0.31f, 0.39f, 0.21f, 1f));
        }

        private GameObject CreateCommunityStorage(Vector2Int cell)
        {
            return CreateLargeBuildingTile(
                $"CommunityStorage_{cell.x:000}_{cell.y:000}",
                cell,
                communityStorageMaterial,
                new Color(0.24f, 0.42f, 0.22f, 1f));
        }

        private GameObject CreateSlumTradeCenter(Vector2Int cell)
        {
            return CreateLargeBuildingTile(
                $"SlumTradeCenter_{cell.x:000}_{cell.y:000}",
                cell,
                slumTradeCenterMaterial,
                new Color(0.55f, 0.36f, 0.18f, 1f));
        }

        private GameObject CreateResourceWorksite(Vector2Int cell, ObjectKind kind)
        {
            Material material = kind == ObjectKind.StoneQuarry ? stoneQuarryMaterial : sawmillMaterial;
            Color fallback = kind == ObjectKind.StoneQuarry
                ? new Color(0.38f, 0.37f, 0.34f, 1f)
                : new Color(0.48f, 0.31f, 0.14f, 1f);
            return CreateResourceWorksiteTile($"{kind}_{cell.x:000}_{cell.y:000}", cell, material, fallback);
        }

        private GameObject CreateResourceWorksiteTile(string name, Vector2Int anchorCell, Material material, Color fallbackColor)
        {
            Vector3 centerOffset = new Vector3((ResourceWorksiteFootprintCells - 1) * 0.5f, 0f, (ResourceWorksiteFootprintCells - 1) * 0.5f);
            Vector2 footprintSize = new Vector2(ResourceWorksiteFootprintCells, ResourceWorksiteFootprintCells);
            if (material == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetParent(objectsRoot, false);
                fallback.transform.position = CellToWorld(anchorCell) + centerOffset + new Vector3(0f, 0.06f, 0f);
                fallback.transform.localScale = new Vector3(footprintSize.x, 0.1f, footprintSize.y);
                SetRendererColor(fallback, fallbackColor);
                return fallback;
            }

            var root = new GameObject(name);
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(anchorCell) + centerOffset;

            float spriteWidth = 8.2f;
            float spriteAspect = ResolveMaterialAspect(material, 1.5f);
            float spriteHeight = spriteWidth / Mathf.Max(0.1f, spriteAspect);

            GameObject sprite = new GameObject("ResourceWorksiteSprite_7x7", typeof(MeshFilter), typeof(MeshRenderer));
            sprite.transform.SetParent(root.transform, false);
            sprite.transform.localPosition = new Vector3(0f, 0.06f, 0.15f);
            sprite.transform.localRotation = ResolveCameraFacingBillboardRotation();
            sprite.GetComponent<MeshFilter>().sharedMesh = CreateVerticalQuadMesh($"{name}_Mesh", spriteWidth, spriteHeight, RectToUv(new Rect(0f, 0f, 1f, 1f), false));
            MeshRenderer renderer = sprite.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = ResolveWorldSortOrder(root.transform.position.z + ResourceWorksiteFootprintCells * 0.5f);
            return root;
        }

        private static float ResolveMaterialAspect(Material material, float fallback)
        {
            Texture texture = material != null ? material.mainTexture : null;
            return texture != null && texture.height > 0
                ? texture.width / (float)texture.height
                : fallback;
        }

        private GameObject CreateLargeBuildingTile(string name, Vector2Int anchorCell, Material material, Color fallbackColor)
        {
            Vector3 centerOffset = new Vector3((LargeBuildingFootprintWidthCells - 1) * 0.5f, 0f, (LargeBuildingFootprintHeightCells - 1) * 0.5f);
            Vector2 worldSize = new Vector2(LargeBuildingFootprintWidthCells, LargeBuildingFootprintHeightCells);
            if (material == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetParent(objectsRoot, false);
                fallback.transform.position = CellToWorld(anchorCell) + centerOffset + new Vector3(0f, 0.052f, 0f);
                fallback.transform.localScale = new Vector3(worldSize.x, 0.08f, worldSize.y);
                SetRendererColor(fallback, fallbackColor);
                return fallback;
            }

            var root = new GameObject(name);
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(anchorCell) + centerOffset;

            GameObject tile = new GameObject("BuildingQuad_6x4", typeof(MeshFilter), typeof(MeshRenderer));
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            tile.GetComponent<MeshFilter>().sharedMesh = CreateTexturedQuadMesh($"{name}_Mesh", worldSize.x, worldSize.y, RectToUv(new Rect(0f, 0f, 1f, 1f), false));

            MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 80;
            return root;
        }

        private GameObject CreateEstateBuilding(Vector2Int anchorCell, ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            Vector3 centerOffset = new Vector3((size.x - 1) * 0.5f, 0f, (size.y - 1) * 0.5f);
            var root = new GameObject($"{kind}_{anchorCell.x:000}_{anchorCell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(anchorCell) + centerOffset;

            Color stone = kind == ObjectKind.SmallHouse
                ? new Color(0.62f, 0.56f, 0.43f, 1f)
                : new Color(0.34f, 0.35f, 0.36f, 1f);
            Color roof = kind == ObjectKind.SmallHouse
                ? new Color(0.55f, 0.22f, 0.12f, 1f)
                : new Color(0.19f, 0.2f, 0.22f, 1f);
            Color floor = kind == ObjectKind.Castle
                ? new Color(0.22f, 0.24f, 0.21f, 1f)
                : new Color(0.32f, 0.29f, 0.22f, 1f);

            if (kind != ObjectKind.SmallHouse)
                CreateEstateBox(root.transform, "ClaimedFootprint", new Vector3(0f, 0.006f, 0f), new Vector3(size.x, 0.012f, size.y), floor);

            if (kind == ObjectKind.SmallHouse)
            {
                CreateSmallHouseSpriteShell(root.transform, size);
                CreateSmallHouseInterior(root.transform, size);
                SetEstateInteriorVisible(root.transform, false);
                SetEstateExteriorCoverVisible(root.transform, true);
                return root;
            }

            float wallHeight = kind == ObjectKind.StoneTower ? 1.4f : kind == ObjectKind.StoneKeep ? 1.05f : 1.15f;
            CreateEstatePerimeter(root.transform, size, wallHeight, stone);
            CreateEstateCornerTower(root.transform, "NW_Tower", -size.x * 0.5f + 0.9f, size.y * 0.5f - 0.9f, kind, stone);
            CreateEstateCornerTower(root.transform, "NE_Tower", size.x * 0.5f - 0.9f, size.y * 0.5f - 0.9f, kind, stone);
            CreateEstateCornerTower(root.transform, "SW_Tower", -size.x * 0.5f + 0.9f, -size.y * 0.5f + 0.9f, kind, stone);
            CreateEstateCornerTower(root.transform, "SE_Tower", size.x * 0.5f - 0.9f, -size.y * 0.5f + 0.9f, kind, stone);

            if (kind == ObjectKind.StoneTower)
            {
                CreateEstateBox(root.transform, "CentralTower", new Vector3(0f, 1.4f, 0f), new Vector3(size.x - 2.6f, 2.25f, size.y - 2.6f), stone);
                CreateEstateBox(root.transform, "InteriorTowerFloor", new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 3.2f, 0.014f, size.y - 3.2f), new Color(0.25f, 0.24f, 0.2f, 1f));
                CreateEstateBox(root.transform, "InteriorTowerTable", new Vector3(-1.1f, 0.26f, 0.45f), new Vector3(1.35f, 0.34f, 1.0f), new Color(0.37f, 0.23f, 0.13f, 1f));
                CreateEstateBox(root.transform, "InteriorTowerChest", new Vector3(1.35f, 0.22f, -1.2f), new Vector3(1.2f, 0.38f, 0.8f), new Color(0.33f, 0.19f, 0.1f, 1f));
                CreateEstateBox(root.transform, "TowerRoof", new Vector3(0f, 2.65f, 0f), new Vector3(size.x - 1.8f, 0.32f, size.y - 1.8f), roof);
                CreateEstateLabel(root.transform, "TOWER", size);
            }
            else if (kind == ObjectKind.StoneKeep)
            {
                CreateEstateBox(root.transform, "KeepHall", new Vector3(0f, 0.92f, 0f), new Vector3(size.x - 5f, 1.45f, size.y - 5f), stone);
                CreateEstateBox(root.transform, "InteriorKeepFloor", new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 6f, 0.014f, size.y - 6f), new Color(0.25f, 0.23f, 0.19f, 1f));
                CreateEstateBox(root.transform, "InteriorKeepWarTable", new Vector3(0f, 0.28f, 0.2f), new Vector3(2.5f, 0.36f, 1.35f), new Color(0.39f, 0.24f, 0.13f, 1f));
                CreateEstateBox(root.transform, "InteriorKeepSupplyChest", new Vector3(size.x * 0.22f, 0.22f, -size.y * 0.18f), new Vector3(1.45f, 0.38f, 0.9f), new Color(0.33f, 0.19f, 0.1f, 1f));
                CreateEstateBox(root.transform, "KeepRoof", new Vector3(0f, 1.78f, 0f), new Vector3(size.x - 4.35f, 0.26f, size.y - 4.35f), roof);
                CreateEstateLabel(root.transform, "KEEP", size);
            }
            else
            {
                CreateEstateBox(root.transform, "CastleNorthHall", new Vector3(0f, 0.78f, size.y * 0.22f), new Vector3(size.x - 6.2f, 1.25f, 3.4f), stone);
                CreateEstateBox(root.transform, "CastleSouthHall", new Vector3(0f, 0.64f, -size.y * 0.24f), new Vector3(size.x - 7.4f, 1.0f, 2.8f), stone);
                CreateEstateBox(root.transform, "Courtyard", new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 8.8f, 0.014f, size.y - 9.2f), new Color(0.28f, 0.25f, 0.18f, 1f));
                CreateEstateBox(root.transform, "InteriorCastleWell", new Vector3(-size.x * 0.16f, 0.25f, 0f), new Vector3(1.5f, 0.5f, 1.5f), new Color(0.18f, 0.2f, 0.2f, 1f));
                CreateEstateBox(root.transform, "InteriorCastleTable", new Vector3(size.x * 0.16f, 0.26f, size.y * 0.05f), new Vector3(2.4f, 0.34f, 1.3f), new Color(0.39f, 0.24f, 0.13f, 1f));
                CreateEstateBox(root.transform, "InteriorCastleSupply", new Vector3(size.x * 0.22f, 0.2f, -size.y * 0.22f), new Vector3(1.8f, 0.36f, 1.0f), new Color(0.33f, 0.19f, 0.1f, 1f));
                CreateEstateLabel(root.transform, "CASTLE", size);
            }

            CreateEstateDoor(root.transform, size);
            return root;
        }

        private void CreateEstatePerimeter(Transform parent, Vector2Int size, float height, Color color)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            float gateGap = Mathf.Min(2.4f, Mathf.Max(1.6f, size.x * 0.16f));
            float southSegmentWidth = (size.x - gateGap) * 0.5f;
            CreateEstateBox(parent, "Wall_North", new Vector3(0f, height * 0.5f, halfY - 0.35f), new Vector3(size.x, height, 0.7f), color);
            CreateEstateBox(parent, "Wall_South_West", new Vector3(-gateGap * 0.5f - southSegmentWidth * 0.5f, height * 0.5f, -halfY + 0.35f), new Vector3(southSegmentWidth, height, 0.7f), color);
            CreateEstateBox(parent, "Wall_South_East", new Vector3(gateGap * 0.5f + southSegmentWidth * 0.5f, height * 0.5f, -halfY + 0.35f), new Vector3(southSegmentWidth, height, 0.7f), color);
            CreateEstateBox(parent, "Wall_West", new Vector3(-halfX + 0.35f, height * 0.5f, 0f), new Vector3(0.7f, height, size.y), color);
            CreateEstateBox(parent, "Wall_East", new Vector3(halfX - 0.35f, height * 0.5f, 0f), new Vector3(0.7f, height, size.y), color);
        }

        private void CreateSmallHouseSpriteShell(Transform parent, Vector2Int size)
        {
            float worldWidth = size.x + 2.2f;
            float aspect = ResolveMaterialAspect(smallHouseExteriorMaterial, 1f);
            float worldHeight = worldWidth / Mathf.Max(0.25f, aspect);

            if (smallHouseExteriorMaterial == null)
            {
                CreateSmallHouseShell(parent, size, new Color(0.62f, 0.56f, 0.43f, 1f), new Color(0.55f, 0.22f, 0.12f, 1f));
                return;
            }

            GameObject exterior = new GameObject("EstateExteriorSprite", typeof(MeshFilter), typeof(MeshRenderer));
            exterior.transform.SetParent(parent, false);
            exterior.transform.localPosition = new Vector3(0f, 0.02f, -0.9f);
            exterior.transform.localRotation = ResolveCameraFacingBillboardRotation();
            exterior.GetComponent<MeshFilter>().sharedMesh = CreateVerticalQuadMesh("SmallHouseExteriorSprite_Mesh", worldWidth, worldHeight, RectToUv(new Rect(0f, 0f, 1f, 1f), false));

            MeshRenderer renderer = exterior.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = smallHouseExteriorMaterial;
            renderer.sortingOrder = ResolveWorldSortOrder(parent.position.z - size.y * 0.5f) + 180;
        }

        private void CreateSmallHouseInterior(Transform parent, Vector2Int size)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            Color wallColor = new Color(0.58f, 0.52f, 0.42f, 1f);
            Color trimColor = new Color(0.22f, 0.13f, 0.07f, 1f);
            CreateSmallHouseInteriorFloorTiles(parent, size);
            CreateEstateBox(parent, "InteriorNorthWall", new Vector3(0f, 0.48f, halfY - 0.72f), new Vector3(size.x - 1.15f, 0.96f, 0.26f), wallColor);
            CreateEstateBox(parent, "InteriorWestWall", new Vector3(-halfX + 0.72f, 0.48f, 0f), new Vector3(0.26f, 0.96f, size.y - 1.15f), wallColor);
            CreateEstateBox(parent, "InteriorEastWall", new Vector3(halfX - 0.72f, 0.48f, 0f), new Vector3(0.26f, 0.96f, size.y - 1.15f), wallColor);
            CreateEstateBox(parent, "InteriorSouthWallLeft", new Vector3(-1.95f, 0.36f, -halfY + 0.72f), new Vector3(1.75f, 0.72f, 0.24f), wallColor);
            CreateEstateBox(parent, "InteriorSouthWallRight", new Vector3(1.95f, 0.36f, -halfY + 0.72f), new Vector3(1.75f, 0.72f, 0.24f), wallColor);
            CreateEstateBox(parent, "InteriorExitThreshold", new Vector3(0f, 0.03f, -halfY + 0.48f), new Vector3(1.9f, 0.04f, 0.52f), new Color(0.58f, 0.46f, 0.28f, 1f));
            CreateEstateBox(parent, "InteriorDoorFrameLeft", new Vector3(-1.04f, 0.48f, -halfY + 0.66f), new Vector3(0.18f, 0.96f, 0.28f), trimColor);
            CreateEstateBox(parent, "InteriorDoorFrameRight", new Vector3(1.04f, 0.48f, -halfY + 0.66f), new Vector3(0.18f, 0.96f, 0.28f), trimColor);
            CreateEstateBox(parent, "InteriorBed", new Vector3(-halfX + 1.65f, 0.18f, halfY - 1.65f), new Vector3(1.35f, 0.22f, 2.05f), new Color(0.31f, 0.43f, 0.52f, 1f));
            CreateEstateBox(parent, "InteriorTable", new Vector3(0.85f, 0.2f, 0.35f), new Vector3(1.15f, 0.28f, 1.15f), new Color(0.42f, 0.27f, 0.14f, 1f));
            CreateEstateBox(parent, "InteriorChest", new Vector3(halfX - 1.55f, 0.18f, -halfY + 1.85f), new Vector3(1.15f, 0.34f, 0.75f), new Color(0.36f, 0.21f, 0.11f, 1f));
        }

        private void CreateSmallHouseInteriorFloorTiles(Transform parent, Vector2Int size)
        {
            Color floorA = new Color(0.43f, 0.34f, 0.22f, 1f);
            Color floorB = new Color(0.36f, 0.28f, 0.18f, 1f);
            int minX = -size.x / 2 + 1;
            int maxX = size.x / 2 - 1;
            int minY = -size.y / 2 + 1;
            int maxY = size.y / 2 - 1;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    string name = $"InteriorFloorTile_{x + size.x:00}_{y + size.y:00}";
                    Color color = ((x + y) & 1) == 0 ? floorA : floorB;
                    CreateEstateBox(parent, name, new Vector3(x, 0.012f, y), new Vector3(0.94f, 0.014f, 0.94f), color);
                }
            }
        }

        private void CreateSmallHouseShell(Transform parent, Vector2Int size, Color wallColor, Color roofColor)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            float wallHeight = 1.08f;
            float wallThickness = 0.42f;
            float gateGap = 1.9f;
            float southSegmentWidth = (size.x - gateGap - 0.8f) * 0.5f;
            Color trimColor = new Color(0.18f, 0.2f, 0.18f, 1f);

            CreateEstateBox(parent, "InteriorFloor", new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 1.2f, 0.014f, size.y - 1.2f), new Color(0.28f, 0.25f, 0.19f, 1f));
            CreateEstateBox(parent, "BackWall", new Vector3(0f, wallHeight * 0.5f, halfY - 0.56f), new Vector3(size.x - 0.72f, wallHeight, wallThickness), wallColor);
            CreateEstateBox(parent, "LeftWall", new Vector3(-halfX + 0.56f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size.y - 0.72f), wallColor);
            CreateEstateBox(parent, "RightWall", new Vector3(halfX - 0.56f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size.y - 0.72f), wallColor);
            CreateEstateBox(parent, "FrontWall_West", new Vector3(-gateGap * 0.5f - southSegmentWidth * 0.5f, wallHeight * 0.5f, -halfY + 0.56f), new Vector3(southSegmentWidth, wallHeight, wallThickness), wallColor);
            CreateEstateBox(parent, "FrontWall_East", new Vector3(gateGap * 0.5f + southSegmentWidth * 0.5f, wallHeight * 0.5f, -halfY + 0.56f), new Vector3(southSegmentWidth, wallHeight, wallThickness), wallColor);
            CreateEstateBox(parent, "Window_Left", new Vector3(-halfX + 0.54f, 0.68f, -0.7f), new Vector3(0.08f, 0.38f, 0.62f), new Color(0.84f, 0.76f, 0.42f, 1f));
            CreateEstateBox(parent, "Window_Right", new Vector3(halfX - 0.54f, 0.68f, 0.75f), new Vector3(0.08f, 0.38f, 0.62f), new Color(0.84f, 0.76f, 0.42f, 1f));

            CreateEstateBox(parent, "Roof_Back", new Vector3(0f, wallHeight + 0.28f, halfY - 0.8f), new Vector3(size.x - 0.15f, 0.26f, 1.1f), roofColor);
            CreateEstateBox(parent, "Roof_Left", new Vector3(-halfX + 0.8f, wallHeight + 0.28f, 0f), new Vector3(1.1f, 0.26f, size.y - 0.25f), roofColor);
            CreateEstateBox(parent, "Roof_Right", new Vector3(halfX - 0.8f, wallHeight + 0.28f, 0f), new Vector3(1.1f, 0.26f, size.y - 0.25f), roofColor);
            CreateEstateBox(parent, "Roof_Front_Left", new Vector3(-gateGap * 0.5f - southSegmentWidth * 0.5f, wallHeight + 0.28f, -halfY + 0.8f), new Vector3(southSegmentWidth + 0.15f, 0.26f, 1.1f), roofColor);
            CreateEstateBox(parent, "Roof_Front_Right", new Vector3(gateGap * 0.5f + southSegmentWidth * 0.5f, wallHeight + 0.28f, -halfY + 0.8f), new Vector3(southSegmentWidth + 0.15f, 0.26f, 1.1f), roofColor);
            CreateEstateBox(parent, "DoorLintel", new Vector3(0f, wallHeight + 0.2f, -halfY + 0.72f), new Vector3(gateGap + 0.35f, 0.22f, 0.58f), roofColor);

            CreateEstateBox(parent, "Chimney", new Vector3(halfX - 1.75f, wallHeight + 0.76f, halfY - 1.55f), new Vector3(0.52f, 0.92f, 0.52f), trimColor);
            CreateEstateBox(parent, "DoorStep", new Vector3(0f, 0.018f, -halfY - 0.26f), new Vector3(1.9f, 0.018f, 0.75f), new Color(0.48f, 0.41f, 0.29f, 1f));
            CreateEstateBox(parent, "InteriorBed", new Vector3(-halfX + 1.65f, 0.18f, halfY - 1.65f), new Vector3(1.35f, 0.22f, 2.05f), new Color(0.31f, 0.43f, 0.52f, 1f));
            CreateEstateBox(parent, "InteriorTable", new Vector3(0.85f, 0.2f, 0.35f), new Vector3(1.15f, 0.28f, 1.15f), new Color(0.42f, 0.27f, 0.14f, 1f));
            CreateEstateBox(parent, "InteriorChest", new Vector3(halfX - 1.55f, 0.18f, -halfY + 1.85f), new Vector3(1.15f, 0.34f, 0.75f), new Color(0.36f, 0.21f, 0.11f, 1f));
        }

        private void CreateEstateCornerTower(Transform parent, string name, float x, float z, ObjectKind kind, Color color)
        {
            float height = kind == ObjectKind.StoneTower ? 2.9f : kind == ObjectKind.Castle ? 2.25f : 1.95f;
            CreateEstateBox(parent, name, new Vector3(x, height * 0.5f, z), new Vector3(1.8f, height, 1.8f), color);
            CreateEstateBox(parent, name + "_Cap", new Vector3(x, height + 0.18f, z), new Vector3(2.12f, 0.28f, 2.12f), new Color(0.2f, 0.21f, 0.23f, 1f));
        }

        private void CreateEstateDoor(Transform parent, Vector2Int size)
        {
            CreateEstateBox(parent, "FrontDoor", new Vector3(0f, 0.42f, -size.y * 0.5f + 0.08f), new Vector3(1.35f, 0.84f, 0.16f), new Color(0.31f, 0.16f, 0.07f, 1f));
            CreateEstateBox(parent, "DoorHandle", new Vector3(0.44f, 0.44f, -size.y * 0.5f - 0.02f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.92f, 0.72f, 0.32f, 1f));
        }

        private void CreateEstateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            SetRendererColor(box, color);
        }

        private void CreateEstateLabel(Transform parent, string value, Vector2Int size)
        {
            var labelObject = new GameObject("EstateLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.09f, size.y * 0.5f - 1.2f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = Mathf.Clamp(size.x * 0.18f, 0.9f, 2.6f);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.96f, 0.88f, 0.58f, 1f);
            label.text = value;
            label.rectTransform.sizeDelta = new Vector2(size.x, 2f);
        }

        private bool TryMergeSleepingBags(Vector2Int cell, bool submitNetwork)
        {
            if (placedObjects == null || !IsMergeableSleepingBagAt(cell))
                return false;

            Vector2Int otherCell = cell + Vector2Int.right;
            Vector2Int tentAnchor = cell;
            if (!IsMergeableSleepingBagAt(otherCell))
            {
                otherCell = cell + Vector2Int.left;
                tentAnchor = otherCell;
            }

            if (!IsMergeableSleepingBagAt(otherCell))
                return false;

            RemoveObjectLocal(cell);
            RemoveObjectLocal(otherCell);

            PlacedObject tent = CreateBlockObject(tentAnchor, ObjectKind.Tent);
            placedObjects[tentAnchor] = tent;
            SymbiozRuntimeLog.Write("BUILD", $"Sleeping bags merged into tent anchor={tentAnchor.x}:{tentAnchor.y} location={currentLocation}");

            if (submitNetwork)
            {
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
                fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, otherCell.x, otherCell.y);
                fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, tentAnchor.x, tentAnchor.y, (int)ObjectKind.Tent, string.Empty);
            }

            RefreshBuildTilesAround(cell);
            RefreshBuildTilesAround(otherCell);
            RefreshBuildTilesAround(tentAnchor);
            return true;
        }

        private bool IsSleepingBagAt(Vector2Int cell)
        {
            return placedObjects != null
                && placedObjects.TryGetValue(cell, out PlacedObject placed)
                && placed.Kind == ObjectKind.SleepingBag;
        }

        private bool IsMergeableSleepingBagAt(Vector2Int cell)
        {
            return placedObjects != null
                && placedObjects.TryGetValue(cell, out PlacedObject placed)
                && placed.Kind == ObjectKind.SleepingBag
                && !IsPlayerSleeperNote(placed.Note);
        }

        private void RemoveObjectLocal(Vector2Int cell)
        {
            if (!placedObjects.TryGetValue(cell, out PlacedObject placed))
                return;

            if (placed.Root != null)
                Destroy(placed.Root);

            placedObjects.Remove(cell);
        }

        private GameObject CreateRoadTile(Vector2Int cell)
        {
            bool north = IsRoadAt(cell + Vector2Int.up);
            bool south = IsRoadAt(cell + Vector2Int.down);
            bool east = IsRoadAt(cell + Vector2Int.right);
            bool west = IsRoadAt(cell + Vector2Int.left);
            bool hasConnections = north || south || east || west;

            if (!hasConnections)
            {
                north = true;
                south = true;
            }

            int tileIndex = ResolveConnectedTileIndex(north, east, south, west);
            return CreateAtlasTile($"Road_{cell.x:000}_{cell.y:000}", cell, roadTileMaterial, tileIndex, 0.065f, 1.08f, new Color(0.38f, 0.32f, 0.22f, 1f));
        }

        private GameObject CreateLegacyRoadTile(Vector2Int cell)
        {
            var root = new GameObject($"Road_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            bool north = IsRoadAt(cell + Vector2Int.up);
            bool south = IsRoadAt(cell + Vector2Int.down);
            bool east = IsRoadAt(cell + Vector2Int.right);
            bool west = IsRoadAt(cell + Vector2Int.left);
            bool hasConnections = north || south || east || west;

            if (!hasConnections)
            {
                north = true;
                south = true;
            }

            GameObject dirtBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirtBase.name = "CompactedDirtBase";
            dirtBase.transform.SetParent(root.transform, false);
            dirtBase.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            dirtBase.transform.localScale = new Vector3(1.02f, 0.035f, 1.02f);
            SetRendererColor(dirtBase, new Color(0.19f, 0.15f, 0.11f, 1f));

            CreateRoadPiece(root.transform, "RoadCenter", new Vector3(0f, 0.045f, 0f), new Vector3(0.52f, 0.026f, 0.52f), new Color(0.27f, 0.24f, 0.19f, 1f));

            if (north)
                CreateRoadPiece(root.transform, "RoadArm_North", new Vector3(0f, 0.045f, 0.34f), new Vector3(0.52f, 0.026f, 0.68f), new Color(0.27f, 0.24f, 0.19f, 1f));
            if (south)
                CreateRoadPiece(root.transform, "RoadArm_South", new Vector3(0f, 0.045f, -0.34f), new Vector3(0.52f, 0.026f, 0.68f), new Color(0.27f, 0.24f, 0.19f, 1f));
            if (east)
                CreateRoadPiece(root.transform, "RoadArm_East", new Vector3(0.34f, 0.045f, 0f), new Vector3(0.68f, 0.026f, 0.52f), new Color(0.27f, 0.24f, 0.19f, 1f));
            if (west)
                CreateRoadPiece(root.transform, "RoadArm_West", new Vector3(-0.34f, 0.045f, 0f), new Vector3(0.68f, 0.026f, 0.52f), new Color(0.27f, 0.24f, 0.19f, 1f));

            if (north || south)
            {
                CreateRoadPiece(root.transform, "GravelEdge_WestVertical", new Vector3(-0.34f, 0.055f, 0f), new Vector3(0.08f, 0.018f, 1f), new Color(0.35f, 0.33f, 0.28f, 1f));
                CreateRoadPiece(root.transform, "GravelEdge_EastVertical", new Vector3(0.34f, 0.055f, 0f), new Vector3(0.08f, 0.018f, 1f), new Color(0.35f, 0.33f, 0.28f, 1f));
            }

            if (east || west)
            {
                CreateRoadPiece(root.transform, "GravelEdge_NorthHorizontal", new Vector3(0f, 0.057f, 0.34f), new Vector3(1f, 0.018f, 0.08f), new Color(0.35f, 0.33f, 0.28f, 1f));
                CreateRoadPiece(root.transform, "GravelEdge_SouthHorizontal", new Vector3(0f, 0.057f, -0.34f), new Vector3(1f, 0.018f, 0.08f), new Color(0.35f, 0.33f, 0.28f, 1f));
            }

            for (int i = 0; i < 5; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = $"SmallStone_{i}";
                stone.transform.SetParent(root.transform, false);
                float x = -0.36f + i * 0.18f;
                float z = i % 2 == 0 ? -0.38f : 0.36f;
                stone.transform.localPosition = new Vector3(x, 0.064f, z);
                stone.transform.localScale = new Vector3(0.055f, 0.018f, 0.04f);
                SetRendererColor(stone, new Color(0.42f, 0.4f, 0.35f, 1f));
            }

            return root;
        }

        private void CreateRoadPiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            SetRendererColor(piece, color);
        }

        private bool IsRoadAt(Vector2Int cell)
        {
            return placedObjects != null
                && placedObjects.TryGetValue(cell, out PlacedObject placed)
                && placed.Kind == ObjectKind.Road;
        }

        private bool IsWallAt(Vector2Int cell)
        {
            return placedObjects != null
                && placedObjects.TryGetValue(cell, out PlacedObject placed)
                && placed.Kind == ObjectKind.Wall;
        }

        private void RefreshBuildTilesAround(Vector2Int cell)
        {
            RefreshRoadAt(cell);
            RefreshRoadAt(cell + Vector2Int.up);
            RefreshRoadAt(cell + Vector2Int.down);
            RefreshRoadAt(cell + Vector2Int.left);
            RefreshRoadAt(cell + Vector2Int.right);

            RefreshWallAt(cell);
            RefreshWallAt(cell + Vector2Int.up);
            RefreshWallAt(cell + Vector2Int.down);
            RefreshWallAt(cell + Vector2Int.left);
            RefreshWallAt(cell + Vector2Int.right);
        }

        private void RefreshRoadAt(Vector2Int cell)
        {
            if (!IsRoadAt(cell))
                return;

            PlacedObject placed = placedObjects[cell];
            if (placed.Root != null)
                Destroy(placed.Root);

            placed.Root = CreateRoadTile(cell);
        }

        private void RefreshWallAt(Vector2Int cell)
        {
            if (!IsWallAt(cell))
                return;

            PlacedObject placed = placedObjects[cell];
            if (placed.Root != null)
                Destroy(placed.Root);

            placed.Root = CreateBrickWall(cell);
        }

        private void RefreshAllConnectedBuildTiles()
        {
            var roadCells = new List<Vector2Int>();
            var wallCells = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value != null && pair.Value.Kind == ObjectKind.Road)
                    roadCells.Add(pair.Key);
                if (pair.Value != null && pair.Value.Kind == ObjectKind.Wall)
                    wallCells.Add(pair.Key);
            }

            for (int i = 0; i < roadCells.Count; i++)
                RefreshRoadAt(roadCells[i]);

            for (int i = 0; i < wallCells.Count; i++)
                RefreshWallAt(wallCells[i]);
        }

        private PlacedObject CreateSign(Vector2Int cell)
        {
            var root = new GameObject($"Sign_{cell.x:000}_{cell.y:000}");
            root.transform.SetParent(objectsRoot, false);
            root.transform.position = CellToWorld(cell);

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(root.transform, false);
            board.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            board.transform.localScale = new Vector3(0.86f, 0.05f, 0.72f);
            SetRendererColor(board, new Color(0.82f, 0.67f, 0.42f, 1f));

            var labelObject = new GameObject("NoteText");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 0.28f;
            label.color = new Color(0.12f, 0.085f, 0.045f, 1f);
            label.text = "...";
            label.rectTransform.sizeDelta = new Vector2(1.8f, 0.7f);

            return new PlacedObject
            {
                Kind = ObjectKind.Sign,
                Root = root,
                Label = label,
                Note = string.Empty
            };
        }

        private void FinishNoteEditing(PlacedObject placed)
        {
            if (string.IsNullOrWhiteSpace(placed.Note))
                placed.Note = "mark";

            if (TryApplySignBlueprint(placed))
                return;

            UpdateSignLabel(placed);
            bool submittedToServer = false;
            if (IsServerAuthoritativeWorldMode())
            {
                submittedToServer = fishNetWorldBridge != null
                    && fishNetWorldBridge.SubmitBuildCommand((int)currentLocation, editingNoteCell.x, editingNoteCell.y, (int)ObjectKind.Sign, placed.Note ?? string.Empty);
                if (!submittedToServer)
                {
                    SymbiozRuntimeLog.Write("SIGN", $"Sign save rejected because FishNet server is not connected cell={editingNoteCell.x}:{editingNoteCell.y}");
                    return;
                }
            }

            isEditingNote = false;
            editingNoteObject = null;
            if (notePanel != null)
                notePanel.gameObject.SetActive(false);

            if (!submittedToServer)
                fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, editingNoteCell.x, editingNoteCell.y, (int)ObjectKind.Sign, placed.Note ?? string.Empty);
            SavePersistentWorld();
        }

        private bool TryApplySignBlueprint(PlacedObject signObject)
        {
            if (!TryResolveSignBlueprint(signObject.Note, out ObjectKind blueprintKind, out bool deleteObject))
                return false;

            Vector2Int cell = editingNoteCell;
            SymbiozRuntimeLog.Write("SIGN", $"Blueprint command note='{signObject.Note}' cell={cell.x}:{cell.y} delete={deleteObject} kind={blueprintKind}");
            bool submittedToServer = false;
            if (IsServerAuthoritativeWorldMode())
            {
                submittedToServer = fishNetWorldBridge != null && (deleteObject
                    ? fishNetWorldBridge.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y)
                    : fishNetWorldBridge.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)blueprintKind, string.Empty));
                if (!submittedToServer)
                {
                    SymbiozRuntimeLog.Write("SIGN", $"Blueprint rejected because FishNet server is not connected cell={cell.x}:{cell.y}");
                    return true;
                }
            }

            if (signObject.Root != null)
                Destroy(signObject.Root);

            placedObjects.Remove(cell);
            isEditingNote = false;
            editingNoteObject = null;
            if (notePanel != null)
                notePanel.gameObject.SetActive(false);

            if (deleteObject)
            {
                RefreshBuildTilesAround(cell);
                if (!submittedToServer)
                    fishNetWorldBridge?.SubmitDeleteCommand((int)currentLocation, cell.x, cell.y);
                SavePersistentWorld();
                return true;
            }

            PlacedObject builtObject = CreateBlockObject(cell, blueprintKind);
            placedObjects[cell] = builtObject;

            RefreshBuildTilesAround(cell);
            if (!submittedToServer)
                fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, cell.x, cell.y, (int)blueprintKind, string.Empty);
            SavePersistentWorld();
            return true;
        }

        private static bool TryResolveSignBlueprint(string note, out ObjectKind kind, out bool deleteObject)
        {
            kind = ObjectKind.Sign;
            deleteObject = false;

            if (string.IsNullOrWhiteSpace(note))
                return false;

            string command = note.Trim().ToLowerInvariant();
            if (ContainsAny(command, "delete", "remove", "clear", "erase", "destroy"))
            {
                deleteObject = true;
                return true;
            }

            if (ContainsAny(command, "road", "path", "street", "track", "doroga"))
            {
                kind = ObjectKind.Road;
                return true;
            }

            if (ContainsAny(command, "wall", "brick", "stone wall", "stena"))
            {
                kind = ObjectKind.Wall;
                return true;
            }

            if (ContainsAny(command, "repair", "plank", "planks", "wood", "board", "boards", "barrier", "barricade"))
            {
                kind = ObjectKind.Repair;
                return true;
            }

            if (ContainsAny(command, "plot", "foundation", "reserve", "claim", "build plot", "construction"))
            {
                kind = ObjectKind.BuildPlot;
                return true;
            }

            if (ContainsAny(command, "t1", "storage", "storehouse", "resource", "hranilishe", "community storage"))
            {
                kind = ObjectKind.CommunityStorage;
                return true;
            }

            if (ContainsAny(command, "t2", "trade", "market", "slum trade", "trusheb", "truscheb"))
            {
                kind = ObjectKind.SlumTradeCenter;
                return true;
            }

            if (ContainsAny(command, "quarry", "mine", "stone quarry", "stone worksite", "kamen", "rudnik"))
            {
                kind = ObjectKind.StoneQuarry;
                return true;
            }

            if (ContainsAny(command, "sawmill", "lumber", "lumberyard", "wood worksite", "lesopilka"))
            {
                kind = ObjectKind.Sawmill;
                return true;
            }

            if (ContainsAny(command, "house", "home", "dom", "small house"))
            {
                kind = ObjectKind.SmallHouse;
                return true;
            }

            if (ContainsAny(command, "tower", "bashnya", "stone tower"))
            {
                kind = ObjectKind.StoneTower;
                return true;
            }

            if (ContainsAny(command, "keep", "fort", "krepost", "stone keep"))
            {
                kind = ObjectKind.StoneKeep;
                return true;
            }

            if (ContainsAny(command, "castle", "zamok", "fortress"))
            {
                kind = ObjectKind.Castle;
                return true;
            }

            return false;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (value.Contains(needles[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void OpenNoteDialog(Vector2Int cell, PlacedObject placed)
        {
            editingNoteCell = cell;
            editingNoteObject = placed;
            isEditingNote = true;
            SymbiozRuntimeLog.Write("SIGN", $"Open note dialog cell={cell.x}:{cell.y}");

            if (notePanel != null)
                notePanel.gameObject.SetActive(true);

            if (noteInput != null)
            {
                noteInput.text = placed.Note ?? string.Empty;
                noteInput.ActivateInputField();
                noteInput.Select();
            }
        }

        private void SaveNoteDialog()
        {
            if (editingNoteObject == null)
            {
                isEditingNote = false;
                if (notePanel != null)
                    notePanel.gameObject.SetActive(false);
                return;
            }

            editingNoteObject.Note = noteInput != null ? noteInput.text : editingNoteObject.Note;
            SymbiozRuntimeLog.Write("SIGN", $"Save note cell={editingNoteCell.x}:{editingNoteCell.y} text='{editingNoteObject.Note}'");
            FinishNoteEditing(editingNoteObject);
        }

        private void CancelNoteDialog()
        {
            if (editingNoteObject != null && string.IsNullOrWhiteSpace(editingNoteObject.Note))
            {
                if (editingNoteObject.Root != null)
                    Destroy(editingNoteObject.Root);

                placedObjects.Remove(editingNoteCell);
                SavePersistentWorld();
            }

            editingNoteObject = null;
            isEditingNote = false;
            if (notePanel != null)
                notePanel.gameObject.SetActive(false);
        }

        private static void UpdateSignLabel(PlacedObject placed)
        {
            if (placed.Label == null)
                return;

            placed.Label.text = string.IsNullOrEmpty(placed.Note) ? "..." : placed.Note;
        }

        private void MovePawn()
        {
            if (pawn == null)
                return;

            if (isResourceWorking)
            {
                pawnMoveInput = Vector2.zero;
                hasMoveTarget = false;
                hasAutoMoveTarget = false;
                ClearMoveTargetMarker();
                if (pawnVisualRoot != null && pawnVisualRoot.gameObject.activeSelf)
                    pawnVisualRoot.gameObject.SetActive(false);
                return;
            }

            Transform networkPawn = GetOwnedNetworkPawnTransform();
            if (networkPawn != null)
            {
                if (pawnVisualRoot != null && !pawnVisualRoot.gameObject.activeSelf)
                    pawnVisualRoot.gameObject.SetActive(true);

                Vector3 previous = pawn.transform.position;
                pawn.transform.position = networkPawn.position;
                pawnCell = WorldToCell(pawn.transform.position);
                Vector3 delta = pawn.transform.position - previous;
                Vector2 facingDirection = new Vector2(delta.x, delta.z);

                if (hasAutoMoveTarget)
                {
                    Vector3 targetWorld = CellToWorld(targetCell) + new Vector3(0f, PawnGroundYOffset, 0f);
                    Vector3 toTarget = targetWorld - pawn.transform.position;
                    toTarget.y = 0f;
                    if (toTarget.magnitude <= 0.14f)
                    {
                        hasAutoMoveTarget = false;
                        hasMoveTarget = false;
                        SymbiozNetworkPawn.SetLocalNavigationInput(Vector2.zero);
                        ClearMoveTargetMarker();
                    }
                    else
                    {
                        Vector2 navInput = new Vector2(toTarget.x, toTarget.z).normalized;
                        SymbiozNetworkPawn.SetLocalNavigationInput(navInput);
                        facingDirection = navInput;
                        hasMoveTarget = true;
                    }
                }
                else
                {
                    SymbiozNetworkPawn.SetLocalNavigationInput(Vector2.zero);
                    targetCell = pawnCell;
                    hasMoveTarget = false;
                    ClearMoveTargetMarker();
                }

                UpdatePawnFacing(facingDirection, facingDirection.sqrMagnitude > 0.00001f);
                return;
            }

            if (pawnVisualRoot != null && !pawnVisualRoot.gameObject.activeSelf)
                pawnVisualRoot.gameObject.SetActive(true);

            if (pawnMoveInput.sqrMagnitude > 0.001f)
            {
                ClearMoveTargetMarker();
                UpdatePawnFacing(pawnMoveInput, true);
                float speedCellsPerSecond = ResolvePawnSpeedCellsPerSecond(pawnMoveInput);
                Vector3 delta = new Vector3(pawnMoveInput.x, 0f, pawnMoveInput.y) * (speedCellsPerSecond * CellSize * Time.deltaTime);
                MovePawnWithCollision(delta);
                hasMoveTarget = true;
            }
            else if (hasAutoMoveTarget)
            {
                Vector3 targetWorld = CellToWorld(targetCell) + new Vector3(0f, PawnGroundYOffset, 0f);
                Vector3 toTarget = targetWorld - pawn.transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                if (distance <= 0.08f)
                {
                    pawn.transform.position = ClampPawnPosition(targetWorld);
                    pawnCell = WorldToCell(pawn.transform.position);
                    hasAutoMoveTarget = false;
                    hasMoveTarget = false;
                    ClearMoveTargetMarker();
                    UpdatePawnFacing(Vector2.zero, false);
                }
                else
                {
                    Vector3 direction = toTarget / Mathf.Max(0.001f, distance);
                    UpdatePawnFacing(new Vector2(direction.x, direction.z), true);
                    float speedCellsPerSecond = ResolvePawnSpeedCellsPerSecond(new Vector2(direction.x, direction.z));
                    Vector3 before = pawn.transform.position;
                    Vector3 delta = direction * Mathf.Min(distance, speedCellsPerSecond * CellSize * Time.deltaTime);
                    MovePawnWithCollision(delta);
                    if ((pawn.transform.position - before).sqrMagnitude < 0.00001f)
                    {
                        hasAutoMoveTarget = false;
                        hasMoveTarget = false;
                        ClearMoveTargetMarker();
                    }
                    else
                    {
                        hasMoveTarget = true;
                    }
                }
            }
            else
            {
                UpdatePawnFacing(Vector2.zero, false);
                hasMoveTarget = false;
            }

            pawnCell = WorldToCell(pawn.transform.position);
            if (!hasAutoMoveTarget)
                targetCell = pawnCell;

            if (!IsPortalTriggerCell(pawnCell))
                mustLeavePortalBeforeTransition = false;

            if (IsPortalTriggerCell(pawnCell))
                HandleCenterDoorReached();
        }

        private void MovePawnWithCollision(Vector3 delta)
        {
            Vector3 current = pawn.transform.position;
            Vector3 desired = ClampPawnPosition(current + delta);
            if (!IsPawnPositionBlocked(desired))
            {
                pawn.transform.position = desired;
                RefreshPawnRenderOrder();
                return;
            }

            Vector3 xOnly = ClampPawnPosition(current + new Vector3(delta.x, 0f, 0f));
            if (Mathf.Abs(delta.x) > 0.0001f && !IsPawnPositionBlocked(xOnly))
            {
                pawn.transform.position = xOnly;
                RefreshPawnRenderOrder();
                return;
            }

            Vector3 zOnly = ClampPawnPosition(current + new Vector3(0f, 0f, delta.z));
            if (Mathf.Abs(delta.z) > 0.0001f && !IsPawnPositionBlocked(zOnly))
            {
                pawn.transform.position = zOnly;
                RefreshPawnRenderOrder();
            }
        }

        private void RefreshPawnRenderOrder()
        {
            if (pawnSpriteRenderer == null || pawn == null)
                return;

            pawnSpriteRenderer.sortingOrder = ResolveWorldSortOrder(pawn.transform.position.z);
        }

        private void UpdatePawnBillboards()
        {
            Quaternion billboardRotation = ResolvePawnBillboardRotation();
            if (pawnVisualRoot != null)
                pawnVisualRoot.localRotation = billboardRotation;

            foreach (RemotePawn remotePawn in remotePawns.Values)
            {
                if (remotePawn?.SpriteMeshFilter != null)
                    remotePawn.SpriteMeshFilter.transform.localRotation = billboardRotation;
            }
        }

        private void UpdateEstateInteriorVisibility()
        {
            if (placedObjects == null)
                return;

            Vector2Int activeEstateAnchor = activeEstateInteriorAnchor;
            bool hasActiveEstate = isInsideEstateInterior
                && placedObjects.TryGetValue(activeEstateInteriorAnchor, out PlacedObject activeEstate)
                && activeEstate != null
                && activeEstate.Kind == activeEstateInteriorKind
                && IsEstateKind(activeEstate.Kind);

            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                PlacedObject placed = pair.Value;
                if (placed == null || placed.Root == null || !IsEstateKind(placed.Kind))
                    continue;

                bool revealInterior = hasActiveEstate && pair.Key == activeEstateAnchor;
                SetEstateExteriorCoverVisible(placed.Root.transform, !revealInterior);
                SetEstateInteriorVisible(placed.Root.transform, revealInterior);
                UpdateEstateExteriorSpriteBillboard(placed.Root.transform);
            }
        }

        private void UpdateEstateExteriorSpriteBillboard(Transform root)
        {
            if (root == null)
                return;

            Transform sprite = root.Find("EstateExteriorSprite");
            if (sprite != null)
                sprite.localRotation = ResolveCameraFacingBillboardRotation();
        }

        private static void SetEstateExteriorCoverVisible(Transform root, bool visible)
        {
            if (root == null)
                return;

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                string partName = renderer.gameObject.name;
                if (IsEstateExteriorCoverPart(partName))
                    renderer.enabled = visible;
            }
        }

        private static void SetEstateInteriorVisible(Transform root, bool visible)
        {
            if (root == null)
                return;

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (IsEstateInteriorPart(renderer.gameObject.name))
                    renderer.enabled = visible;
            }
        }

        private static bool IsEstateExteriorCoverPart(string partName)
        {
            if (string.IsNullOrWhiteSpace(partName))
                return false;

            return partName.Contains("Roof")
                || partName == "Chimney"
                || partName == "DoorLintel"
                || partName == "FrontDoor"
                || partName == "DoorHandle"
                || partName == "EstateExteriorSprite"
                || partName.StartsWith("FrontWall_", System.StringComparison.Ordinal)
                || partName.StartsWith("Wall_South_", System.StringComparison.Ordinal);
        }

        private static bool IsEstateInteriorPart(string partName)
        {
            return !string.IsNullOrWhiteSpace(partName)
                && partName.StartsWith("Interior", System.StringComparison.Ordinal);
        }

        private Quaternion ResolvePawnBillboardRotation()
        {
            if (worldCamera == null)
                return Quaternion.identity;

            Vector3 forward = worldCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(-forward.normalized, Vector3.up);
        }

        private Quaternion ResolveCameraFacingBillboardRotation()
        {
            if (worldCamera == null)
                return Quaternion.identity;

            return Quaternion.LookRotation(-worldCamera.transform.forward, worldCamera.transform.up);
        }

        private static int ResolveWorldSortOrder(float worldZ)
        {
            return -Mathf.RoundToInt(worldZ * 100f);
        }

        private bool IsPawnPositionBlocked(Vector3 worldPosition)
        {
            return IsPawnProbeBlocked(worldPosition)
                || IsPawnProbeBlocked(worldPosition + new Vector3(0.34f, 0f, 0f))
                || IsPawnProbeBlocked(worldPosition + new Vector3(-0.34f, 0f, 0f))
                || IsPawnProbeBlocked(worldPosition + new Vector3(0f, 0f, 0.34f))
                || IsPawnProbeBlocked(worldPosition + new Vector3(0f, 0f, -0.34f));
        }

        private bool IsPawnProbeBlocked(Vector3 worldPosition)
        {
            Vector2Int cell = WorldToCell(worldPosition);
            if (isInsideEstateInterior && !IsEstateWalkableCell(activeEstateInteriorAnchor, activeEstateInteriorKind, cell))
                return true;

            return TryResolveObjectAtCell(cell, out Vector2Int anchorCell, out PlacedObject placed)
                && placed != null
                && IsBlockingObjectKind(placed.Kind)
                && !IsEstateWalkableCell(anchorCell, placed.Kind, cell);
        }

        private static bool IsBlockingObjectKind(ObjectKind kind)
        {
            return kind == ObjectKind.Wall
                || kind == ObjectKind.Tent
                || kind == ObjectKind.CommunityStorage
                || kind == ObjectKind.SlumTradeCenter
                || kind == ObjectKind.StoneQuarry
                || kind == ObjectKind.Sawmill
                || kind == ObjectKind.Tree
                || IsEstateKind(kind);
        }

        private void SetMoveTarget(Vector2Int cell)
        {
            cell = ClampCell(cell);
            if (IsPawnPositionBlocked(CellToWorld(cell) + new Vector3(0f, PawnGroundYOffset, 0f)))
            {
                SymbiozRuntimeLog.Write("MOVE", $"Target blocked by object cell={cell.x:000}:{cell.y:000}");
                return;
            }

            targetCell = cell;
            hasAutoMoveTarget = true;
            hasMoveTarget = true;
            SelectCell(cell);
            CreateOrMoveTargetMarker(cell);
            SymbiozRuntimeLog.Write("MOVE", $"Two-finger move target cell={cell.x:000}:{cell.y:000}");
        }

        private void CreateOrMoveTargetMarker(Vector2Int cell)
        {
            if (moveTargetMarker == null)
                moveTargetMarker = CreateMoveTargetMarker();

            moveTargetMarker.transform.position = CellToWorld(cell) + new Vector3(0f, 0.18f, 0f);
            moveTargetMarker.SetActive(true);
        }

        private GameObject CreateMoveTargetMarker()
        {
            var root = new GameObject("TwoFingerMoveTargetMarker");
            root.transform.SetParent(worldRoot != null ? worldRoot : transform, false);

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            shaft.transform.localScale = new Vector3(0.13f, 0.025f, 0.58f);
            SetRendererColor(shaft, new Color(0.22f, 0.72f, 1f, 0.95f));

            GameObject head = new GameObject("ArrowHead", typeof(MeshFilter), typeof(MeshRenderer));
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.018f, 0.28f);
            Mesh headMesh = new Mesh();
            headMesh.name = "MoveTargetArrowHeadMesh";
            headMesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0.34f),
                new Vector3(-0.28f, 0f, -0.18f),
                new Vector3(0.28f, 0f, -0.18f)
            };
            headMesh.triangles = new[] { 0, 1, 2 };
            headMesh.RecalculateNormals();
            headMesh.RecalculateBounds();
            head.GetComponent<MeshFilter>().sharedMesh = headMesh;
            head.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial(new Color(0.22f, 0.72f, 1f, 0.95f));

            CreateFootprint(root.transform, "FootprintLeft", new Vector3(-0.16f, 0.028f, -0.3f), -18f);
            CreateFootprint(root.transform, "FootprintRight", new Vector3(0.17f, 0.032f, -0.5f), 18f);
            return root;
        }

        private static void CreateFootprint(Transform parent, string name, Vector3 localPosition, float rotation)
        {
            GameObject footprint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            footprint.name = name;
            footprint.transform.SetParent(parent, false);
            footprint.transform.localPosition = localPosition;
            footprint.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
            footprint.transform.localScale = new Vector3(0.1f, 0.016f, 0.18f);
            SetRendererColor(footprint, new Color(0.06f, 0.09f, 0.1f, 0.95f));
        }

        private void ClearMoveTargetMarker()
        {
            hasAutoMoveTarget = false;
            if (moveTargetMarker != null)
                moveTargetMarker.SetActive(false);
        }

        private float ResolvePawnSpeedCellsPerSecond(Vector2 moveInput)
        {
            if (pawn == null || moveInput.sqrMagnitude <= 0.001f)
                return GroundPawnSpeedCellsPerSecond;

            Vector2 direction = moveInput.normalized;
            Vector3 position = pawn.transform.position;
            Vector2Int currentCell = WorldToCell(position);
            Vector3 probePosition = position + new Vector3(direction.x, 0f, direction.y) * (CellSize * 0.35f);
            Vector2Int nextCell = WorldToCell(probePosition);

            return IsRoadAt(currentCell) || IsRoadAt(nextCell)
                ? RoadPawnSpeedCellsPerSecond
                : GroundPawnSpeedCellsPerSecond;
        }

        private void HandleCenterDoorReached()
        {
            if (mustLeavePortalBeforeTransition || Time.realtimeSinceStartup < doorPromptCooldownUntil)
                return;

            SymbiozRuntimeLog.Write("TRANSITION", $"Instant portal transfer from={GetLocationName(currentLocation)} to={GetOppositeLocationName(pawnCell)}.");
            if (transitionPanel != null)
                transitionPanel.gameObject.SetActive(false);

            isConfirmingTransition = false;
            pawnMoveInput = Vector2.zero;
            hasMoveTarget = false;
            TransitionThroughCenterDoor();
        }

        private void OpenTransitionPrompt()
        {
            if (Time.realtimeSinceStartup < doorPromptCooldownUntil)
                return;

            if (isConfirmingTransition && (transitionPanel == null || !transitionPanel.gameObject.activeInHierarchy))
            {
                SymbiozRuntimeLog.Write("TRANSITION", "Recovered stale transition confirmation flag.");
                isConfirmingTransition = false;
            }

            if (isConfirmingTransition)
                return;

            if (isEditingNote && placedObjects.TryGetValue(editingNoteCell, out PlacedObject noteObject))
                FinishNoteEditing(noteObject);

            isConfirmingTransition = true;
            pawnMoveInput = Vector2.zero;
            hasMoveTarget = false;

            Vector2Int safeCell = GetSpawnCellForCurrentLocation();
            pawnCell = ClampCell(safeCell);
            targetCell = pawnCell;
            if (pawn != null)
                pawn.transform.position = CellToWorld(pawnCell) + new Vector3(0f, PawnGroundYOffset, 0f);

            SelectCell(GetCenterDoorCell());

            if (transitionPromptText != null)
                transitionPromptText.text = $"Do you want to transfer to {GetOppositeLocationName()}?";

            if (transitionPanel != null)
                transitionPanel.gameObject.SetActive(true);

            SymbiozRuntimeLog.Write("TRANSITION", $"Prompt opened from={GetLocationName(currentLocation)} to={GetOppositeLocationName()}");
        }

        private void ConfirmTransition()
        {
            if (!isConfirmingTransition)
                return;

            isConfirmingTransition = false;
            if (transitionPanel != null)
                transitionPanel.gameObject.SetActive(false);

            TransitionThroughCenterDoor();
        }

        private void CancelTransitionPrompt()
        {
            isConfirmingTransition = false;
            doorPromptCooldownUntil = Time.realtimeSinceStartup + DoorPromptCooldownSeconds;
            pawnMoveInput = Vector2.zero;
            if (transitionPanel != null)
                transitionPanel.gameObject.SetActive(false);

            Vector2Int safeCell = GetSpawnCellForCurrentLocation();
            pawnCell = ClampCell(safeCell);
            targetCell = pawnCell;
            if (pawn != null)
                pawn.transform.position = CellToWorld(pawnCell) + new Vector3(0f, PawnGroundYOffset, 0f);
        }

        private void FollowPawn()
        {
            if (cameraWasManuallyMoved || pawn == null)
                return;

            Transform networkPawn = GetOwnedNetworkPawnTransform();
            Vector3 followPosition = networkPawn != null ? networkPawn.position : pawn.transform.position;
            Vector3 desired = CameraPositionForFocus(followPosition);
            cameraTargetPosition = ClampCamera(desired);
        }

        private void CenterCameraOnPawn()
        {
            if (pawn == null || worldCamera == null)
                return;

            Transform networkPawn = GetOwnedNetworkPawnTransform();
            Vector3 followPosition = networkPawn != null ? networkPawn.position : pawn.transform.position;
            cameraTargetPosition = ClampCamera(CameraPositionForFocus(followPosition));
            cameraWasManuallyMoved = true;
        }

        private Transform GetOwnedNetworkPawnTransform()
        {
            return IsServerAuthoritativeWorldMode() ? SymbiozNetworkPawn.LocalOwnedTransform : null;
        }

        private static void ToggleFullscreen()
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(1280, 720, false);
                return;
            }

            Resolution resolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(resolution.width, resolution.height, true);
        }

        private void UpdateCameraSmoothing()
        {
            if (worldCamera == null)
                return;

            cameraTargetPosition = ClampCamera(cameraTargetPosition);
            worldCamera.transform.position = Vector3.SmoothDamp(worldCamera.transform.position, cameraTargetPosition, ref cameraMoveVelocity, CameraMoveSmoothTime);
            worldCamera.orthographicSize = Mathf.SmoothDamp(worldCamera.orthographicSize, cameraTargetZoom, ref cameraZoomVelocity, CameraZoomSmoothTime);
        }

        private void UpdateWorldTimeAndLighting(bool force = false)
        {
            if (!force && Time.unscaledTime < nextSunUpdateTime)
                return;

            nextSunUpdateTime = Time.unscaledTime + SunUpdateIntervalSeconds;
            DateTime now = DateTime.Now;
            float hour = now.Hour + now.Minute / 60f + now.Second / 3600f;
            float daylight = Mathf.Clamp01(Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
            daylight = Mathf.SmoothStep(0f, 1f, daylight);
            float visibility = Mathf.Lerp(0.58f, 1f, daylight);
            float dawn = Mathf.Clamp01(1f - Mathf.Abs(hour - 6.5f) / 2.2f);
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(hour - 18.5f) / 2.4f);
            float warmLight = Mathf.Max(dawn, dusk);

            Color nightTint = new Color(0.76f, 0.8f, 0.92f, 1f);
            Color dayTint = Color.white;
            Color warmTint = new Color(1f, 0.82f, 0.56f, 1f);
            Color worldTint = Color.Lerp(nightTint, dayTint, visibility);
            worldTint = Color.Lerp(worldTint, warmTint, warmLight * 0.35f);

            if (worldSunLight != null)
            {
                worldSunLight.intensity = Mathf.Lerp(0.62f, 1.12f, daylight) + warmLight * 0.12f;
                worldSunLight.color = worldTint;
                worldSunLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(28f, 72f, daylight), hour / 24f * 360f - 90f, 0f);
            }

            RenderSettings.ambientLight = Color.Lerp(new Color(0.48f, 0.5f, 0.58f, 1f), new Color(0.72f, 0.72f, 0.68f, 1f), daylight);
            RenderSettings.fog = false;

            if (worldCamera != null)
                worldCamera.backgroundColor = Color.Lerp(new Color(0.055f, 0.062f, 0.065f, 1f), new Color(0.07f, 0.08f, 0.075f, 1f), daylight);

            ApplyWorldTintToMaterial(groundTileMaterial, worldTint);
            ApplyWorldTintToMaterial(roadTileMaterial, worldTint);
            ApplyWorldTintToMaterial(wallTileMaterial, worldTint);
            ApplyWorldTintToMaterial(barrierTileMaterial, worldTint);
            ApplyWorldTintToMaterial(shelterTileMaterial, worldTint);
            ApplyWorldTintToMaterial(communityStorageMaterial, worldTint);
            ApplyWorldTintToMaterial(slumTradeCenterMaterial, worldTint);
            if (resourceTreeMaterials != null)
            {
                for (int i = 0; i < resourceTreeMaterials.Length; i++)
                    ApplyWorldTintToMaterial(resourceTreeMaterials[i], worldTint);
            }
            ApplyWorldTintToMaterial(portalMaterial, worldTint);
            ApplyWorldTintToMaterial(pawnSpriteMaterial, worldTint);

            worldTimeLabel = now.ToString("HH:mm");
            worldDayPhase = ResolveWorldDayPhase(hour, daylight);
        }

        private void UpdatePawnNeeds()
        {
            pawnSatiety = Mathf.Max(0f, pawnSatiety - SatietyDrainPerSecond * Time.deltaTime);
            if (pawnSatiety <= 0.01f)
                pawnHp = Mathf.Max(0f, pawnHp - 1.2f * Time.deltaTime);
        }

        private static void ApplyWorldTintToMaterial(Material material, Color tint)
        {
            if (material != null && material.HasProperty("_Color"))
                material.color = tint;
        }

        private static string ResolveWorldDayPhase(float hour, float daylight)
        {
            if (hour >= 5f && hour < 8f)
                return "Dawn";
            if (hour >= 8f && hour < 17f)
                return "Day";
            if (hour >= 17f && hour < 21f)
                return "Evening";
            return daylight > 0.15f ? "Twilight" : "Night";
        }

        private void SelectCell(Vector2Int cell)
        {
            cell = ClampCell(cell);
            selectedCell = cell;
            hasSelectedObject = TryResolveObjectAtCell(cell, out selectedObjectAnchorCell, out PlacedObject selectedObject);
            if (cursor != null)
                cursor.transform.position = CellToWorld(cell) + new Vector3(0f, 0.035f, 0f);

            UpdateSelectedObjectFrame(selectedObjectAnchorCell, selectedObject);
        }

        private bool TryResolveObjectAtCell(Vector2Int cell, out Vector2Int anchorCell, out PlacedObject placed)
        {
            anchorCell = cell;
            placed = null;
            if (placedObjects == null)
                return false;

            if (placedObjects.TryGetValue(cell, out placed))
            {
                anchorCell = cell;
                return true;
            }

            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value == null)
                    continue;

                if (ContainsCellInObjectFootprint(pair.Key, pair.Value.Kind, cell))
                {
                    anchorCell = pair.Key;
                    placed = pair.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCellInObjectFootprint(Vector2Int anchorCell, ObjectKind kind, Vector2Int cell)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            int padding = GetObjectInteractionPaddingCells(kind);
            return cell.x >= anchorCell.x - padding
                && cell.x < anchorCell.x + size.x + padding
                && cell.y >= anchorCell.y - padding
                && cell.y < anchorCell.y + size.y + padding;
        }

        private static int GetObjectInteractionPaddingCells(ObjectKind kind)
        {
            return kind == ObjectKind.StoneQuarry || kind == ObjectKind.Sawmill
                ? 2
                : 0;
        }

        private static bool ObjectFootprintsOverlap(Vector2Int aAnchor, ObjectKind aKind, Vector2Int bAnchor, ObjectKind bKind)
        {
            Vector2Int aSize = GetObjectFootprintCells(aKind);
            Vector2Int bSize = GetObjectFootprintCells(bKind);
            return aAnchor.x < bAnchor.x + bSize.x
                && aAnchor.x + aSize.x > bAnchor.x
                && aAnchor.y < bAnchor.y + bSize.y
                && aAnchor.y + aSize.y > bAnchor.y;
        }

        private static Vector2Int GetObjectFootprintCells(ObjectKind kind)
        {
            return kind switch
            {
                ObjectKind.SleepingBag => new Vector2Int(1, 2),
                ObjectKind.Tent => new Vector2Int(2, 2),
                ObjectKind.CommunityStorage => new Vector2Int(LargeBuildingFootprintWidthCells, LargeBuildingFootprintHeightCells),
                ObjectKind.SlumTradeCenter => new Vector2Int(LargeBuildingFootprintWidthCells, LargeBuildingFootprintHeightCells),
                ObjectKind.StoneQuarry => new Vector2Int(ResourceWorksiteFootprintCells, ResourceWorksiteFootprintCells),
                ObjectKind.Sawmill => new Vector2Int(ResourceWorksiteFootprintCells, ResourceWorksiteFootprintCells),
                ObjectKind.SmallHouse => new Vector2Int(EstateHouseWidthCells, EstateHouseHeightCells),
                ObjectKind.StoneTower => new Vector2Int(EstateTowerWidthCells, EstateTowerHeightCells),
                ObjectKind.StoneKeep => new Vector2Int(EstateKeepWidthCells, EstateKeepHeightCells),
                ObjectKind.Castle => new Vector2Int(EstateCastleWidthCells, EstateCastleHeightCells),
                _ => new Vector2Int(1, 1)
            };
        }

        private static bool IsEstateKind(ObjectKind kind)
        {
            return kind == ObjectKind.SmallHouse
                || kind == ObjectKind.StoneTower
                || kind == ObjectKind.StoneKeep
                || kind == ObjectKind.Castle;
        }

        private static Vector2Int GetEstateDoorCell(Vector2Int anchorCell, ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            return anchorCell + new Vector2Int(size.x / 2, 0);
        }

        private static Vector2Int GetEstateInteriorEntryCell(Vector2Int anchorCell, ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            return anchorCell + new Vector2Int(size.x / 2, Mathf.Min(2, size.y - 2));
        }

        private static bool IsEstateWalkableCell(Vector2Int anchorCell, ObjectKind kind, Vector2Int cell)
        {
            if (!IsEstateKind(kind))
                return false;

            Vector2Int size = GetObjectFootprintCells(kind);
            int localX = cell.x - anchorCell.x;
            int localY = cell.y - anchorCell.y;
            if (localX < 0 || localX >= size.x || localY < 0 || localY >= size.y)
                return false;

            int doorX = size.x / 2;
            bool isDoorMouth = localY == 0 && Mathf.Abs(localX - doorX) <= 1;
            if (isDoorMouth)
                return true;

            return localX >= 1
                && localX < size.x - 1
                && localY >= 1
                && localY < size.y - 1;
        }

        private void UpdateSelectedObjectFrame(Vector2Int anchorCell, PlacedObject placed)
        {
            ClearSelectedTreeOutline();
            if (placed == null)
            {
                if (selectedObjectFrame != null)
                    selectedObjectFrame.SetActive(false);
                return;
            }

            if (placed.Kind == ObjectKind.Tree)
            {
                if (selectedObjectFrame != null)
                    selectedObjectFrame.SetActive(false);

                CreateSelectedTreeOutline(placed);
                return;
            }

            if (selectedObjectFrame == null)
                selectedObjectFrame = CreateSelectionFootprintFrame("SelectedObjectFootprintFrame", new Color(1f, 0.85f, 0.28f, 0.92f), out selectedObjectFrameLines);

            Vector2Int size = GetObjectFootprintCells(placed.Kind);
            Vector3 centerOffset = new Vector3((size.x - 1) * 0.5f, 0f, (size.y - 1) * 0.5f);
            selectedObjectFrame.transform.position = CellToWorld(anchorCell) + centerOffset + new Vector3(0f, 0.052f, 0f);
            ConfigureFootprintFrame(selectedObjectFrameLines, size, new Color(1f, 0.85f, 0.28f, 0.92f));
            selectedObjectFrame.SetActive(true);
        }

        private void CreateSelectedTreeOutline(PlacedObject placed)
        {
            if (placed == null || placed.Root == null)
                return;

            int variant = ParseTreeVariant(placed.Note);
            Material baseMaterial = ResolveResourceTreeMaterial(variant);
            if (baseMaterial == null)
                return;

            float height = ResolveTreeSpriteHeight(variant);
            float width = height * ResolveTreeSpriteAspect(variant);
            Mesh outlineMesh = CreateVerticalQuadMesh("SelectedTreeBlinkOutlineMesh", width, height, ResolveTreeUv(variant));
            selectedTreeOutline = new GameObject("SelectedTreeBlinkOutline");
            selectedTreeOutline.transform.SetParent(placed.Root.transform, false);
            selectedTreeOutline.transform.localRotation = ResolveCameraFacingBillboardRotation();

            Shader outlineShader = Shader.Find("Sprites/Default") ?? baseMaterial.shader;
            selectedTreeOutlineMaterial = new Material(outlineShader);
            selectedTreeOutlineMaterial.mainTexture = baseMaterial.mainTexture;
            selectedTreeOutlineMaterial.color = new Color(1f, 0.86f, 0.18f, 0.72f);
            selectedTreeOutlineMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 54;
            if (selectedTreeOutlineMaterial.HasProperty("_Cull"))
                selectedTreeOutlineMaterial.SetInt("_Cull", 0);

            float offset = Mathf.Clamp(width * 0.018f, 0.028f, 0.06f);
            Vector2[] offsets =
            {
                new Vector2(-offset, 0f),
                new Vector2(offset, 0f),
                new Vector2(0f, -offset),
                new Vector2(0f, offset),
                new Vector2(-offset, -offset),
                new Vector2(-offset, offset),
                new Vector2(offset, -offset),
                new Vector2(offset, offset)
            };

            int sortingOrder = ResolveWorldSortOrder(placed.Root.transform.position.z + 0.54f);
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject outlinePart = new GameObject($"Outline_{i:00}", typeof(MeshFilter), typeof(MeshRenderer));
                outlinePart.transform.SetParent(selectedTreeOutline.transform, false);
                outlinePart.transform.localPosition = new Vector3(offsets[i].x, 0.075f + offsets[i].y, 0.23f);
                outlinePart.GetComponent<MeshFilter>().sharedMesh = outlineMesh;
                MeshRenderer renderer = outlinePart.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = selectedTreeOutlineMaterial;
                renderer.sortingOrder = sortingOrder;
            }
        }

        private void ClearSelectedTreeOutline()
        {
            if (selectedTreeOutline != null)
            {
                Destroy(selectedTreeOutline);
                selectedTreeOutline = null;
            }

            if (selectedTreeOutlineMaterial != null)
            {
                Destroy(selectedTreeOutlineMaterial);
                selectedTreeOutlineMaterial = null;
            }

        }

        private GameObject CreateSelectionFootprintFrame(string name, Color color, out LineRenderer[] lines)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(worldRoot != null ? worldRoot : transform, false);
            lines = new LineRenderer[4];
            lines[0] = CreateCursorFrameLine(root.transform, "North", Vector3.zero, Vector3.zero);
            lines[1] = CreateCursorFrameLine(root.transform, "South", Vector3.zero, Vector3.zero);
            lines[2] = CreateCursorFrameLine(root.transform, "West", Vector3.zero, Vector3.zero);
            lines[3] = CreateCursorFrameLine(root.transform, "East", Vector3.zero, Vector3.zero);
            ConfigureFootprintFrame(lines, Vector2Int.one, color);
            return root;
        }

        private static void ConfigureFootprintFrame(LineRenderer[] lines, Vector2Int size, Color color)
        {
            if (lines == null || lines.Length < 4)
                return;

            float halfWidth = Mathf.Max(1, size.x) * 0.5f - 0.03f;
            float halfHeight = Mathf.Max(1, size.y) * 0.5f - 0.03f;
            Vector3 nw = new Vector3(-halfWidth, 0.052f, halfHeight);
            Vector3 ne = new Vector3(halfWidth, 0.052f, halfHeight);
            Vector3 sw = new Vector3(-halfWidth, 0.052f, -halfHeight);
            Vector3 se = new Vector3(halfWidth, 0.052f, -halfHeight);
            SetFrameLine(lines[0], nw, ne, color, 0.07f);
            SetFrameLine(lines[1], sw, se, color, 0.07f);
            SetFrameLine(lines[2], sw, nw, color, 0.07f);
            SetFrameLine(lines[3], se, ne, color, 0.07f);
        }

        private static void SetFrameLine(LineRenderer line, Vector3 from, Vector3 to, Color color, float width)
        {
            if (line == null)
                return;

            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
        }

        private void UpdateSelectedCellCursorPulse()
        {
            if (cursor == null || cursorFrameLines == null)
                return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.2f);
            float scale = Mathf.Lerp(0.985f, 1.035f, pulse);
            cursor.transform.localScale = new Vector3(scale, 1f, scale);
            Color color = new Color(0.45f, 0.86f, 1f, Mathf.Lerp(0.42f, 0.95f, pulse));
            for (int i = 0; i < cursorFrameLines.Length; i++)
            {
                LineRenderer line = cursorFrameLines[i];
                if (line == null)
                    continue;

                line.startColor = color;
                line.endColor = color;
            }

            if (selectedObjectFrame != null && selectedObjectFrame.activeInHierarchy && selectedObjectFrameLines != null)
            {
                Color objectColor = new Color(1f, 0.82f, 0.24f, Mathf.Lerp(0.48f, 0.98f, pulse));
                for (int i = 0; i < selectedObjectFrameLines.Length; i++)
                {
                    LineRenderer line = selectedObjectFrameLines[i];
                    if (line == null)
                        continue;

                    line.startColor = objectColor;
                    line.endColor = objectColor;
                }
            }

            if (selectedTreeOutline != null && selectedTreeOutlineMaterial != null)
            {
                selectedTreeOutlineMaterial.color = new Color(1f, 0.86f, 0.18f, Mathf.Lerp(0.18f, 0.78f, pulse));
            }
        }

        private void UpdateHud()
        {
            if (statsText != null)
            {
                float zoom = worldCamera != null ? worldCamera.orthographicSize : 0f;
                int objectCount = placedObjects != null ? placedObjects.Count : 0;
                statsText.text = $"{GetLocationName(currentLocation)}  |  TIME {worldTimeLabel} {worldDayPhase}  |  MAP {GridSize} x {GridSize}  |  ZOOM {zoom:0.0}  |  MODE {selectedTool}  |  OBJECTS {objectCount}";
            }

            if (commandText != null)
            {
                commandText.text = isEditingNote
                    ? "Sign: type a note or command: wall, road, repair, plot, delete."
                    : "Tap selects cell  |  Hold cell opens actions  |  Build carousel -> OK/X  |  Two fingers move  |  U delete";
            }

            if (selectionText != null)
            {
                string selectedObject = ResolveSelectedObjectLabel();
                selectionText.text =
                    $"Selected\n" +
                    $"Cell: {selectedCell.x:000}:{selectedCell.y:000}\n" +
                    $"Tool: {selectedTool}\n" +
                    $"Object: {selectedObject}" +
                    (hasPendingBuildKind ? $"\nPending: {GetObjectDisplayName(pendingBuildKind)}" : string.Empty);
            }

            if (detailText != null)
            {
                string state = hasMoveTarget ? "Moving" : "Idle";
                detailText.text =
                    $"Architect pawn\n" +
                    $"Name: {localDisplayName ?? "Architect-001"}\n" +
                    $"Hanedan: {localDynastyName ?? "BlackYang"}\n" +
                    $"Age: {Mathf.Max(0, localPlayerAge)}\n" +
                    $"State: {state}\n" +
                    $"Cell: {pawnCell.x:000}:{pawnCell.y:000}\n" +
                    $"Target: {targetCell.x:000}:{targetCell.y:000}\n" +
                    $"Online seen: {remotePawns.Count}\n" +
                    $"Selected: {selectedCell.x:000}:{selectedCell.y:000}\n\n" +
                    $"Survival\n" +
                    $"HP: {pawnHp:0}/{100}\n" +
                    $"Satiety: {pawnSatiety:0}/{SatietyMax:0}\n" +
                    $"Carry: {carriedWeight:0.0}/{CarryWeightMax:0} kg\n" +
                    $"Berries: {carriedBerries}\n\n" +
                    $"Location\n" +
                    $"{GetLocationName(currentLocation)}\n" +
                    $"World time: {worldTimeLabel} {worldDayPhase}\n" +
                    $"Door: {GetDoorLabel()}\n\n" +
                    $"Creator tools\n" +
                    $"11: wall on selected cell\n" +
                    $"22: road on selected cell\n" +
                    $"33: sign or blueprint command\n" +
                    $"44: wooden repair planks\n" +
                    $"55: reserve build square\n" +
                    $"66: sleeping bag; two side by side become tent\n" +
                    $"T+1: 6x4 community storage\n" +
                    $"T+2: 6x4 slum trade center\n" +
                    $"U: delete selected object";
            }

            if (runtimeLogText != null)
            {
                runtimeLogText.text = "Runtime log\n" + SymbiozRuntimeLog.GetRecentText();
            }

            if (materiaText != null)
            {
                int players = CountPlayersOnCurrentLocation();
                int targetBushes = Mathf.Max(MateriaBerryBushesPerPlayer, players * MateriaBerryBushesPerPlayer);
                int targetTrees = Mathf.Max(MateriaTreeMinimum, players * MateriaTreesPerPlayer);
                string selectedMateria = "Selected resource: none";
                string rangeStatus = "move closer";
                if (TryResolveObjectAtCell(selectedCell, out Vector2Int selectedAnchor, out PlacedObject selected) && selected != null)
                {
                    selectedMateria = selected.Kind switch
                    {
                        ObjectKind.BerryBush => $"Selected bush: {ParseBerryBushBerries(selected.Note)}/{BerryBushMaxBerries} berries",
                        ObjectKind.Tree => $"Selected tree: {ParseTreeWood(selected.Note)} logs",
                        ObjectKind.StoneQuarry => "Selected worksite: stone quarry",
                        ObjectKind.Sawmill => "Selected worksite: sawmill",
                        _ => "Selected resource: none"
                    };
                    rangeStatus = IsObjectInInteractRange(selectedAnchor, selected.Kind) ? "ready" : "move closer";
                }

                string chopStatus = hasActiveTreeChop
                    ? $"Chop: {Mathf.Max(0f, activeTreeChopFinishTime - Time.unscaledTime):0}s"
                    : "Chop: idle";
                string workStatus = isResourceWorking
                    ? $"Work: {GetObjectDisplayName(activeResourceKind)} next +1 in {Mathf.Max(0f, nextResourceWorkTickTime - Time.unscaledTime):0}s"
                    : "Work: idle";
                materiaText.text =
                    $"Materia generator\n" +
                    $"Players: {players}  Bushes: {CountBerryBushesWithFood()}/{targetBushes}  Trees: {CountResourceTrees()}/{targetTrees}\n" +
                    $"Bag: stone {carriedStone}  wood {carriedWood}  berries {carriedBerries}\n" +
                    $"{selectedMateria}\n" +
                    $"{chopStatus}  {workStatus}  Range: {rangeStatus}";
            }
        }

        private void TransitionThroughCenterDoor()
        {
            if (isEditingNote && placedObjects.TryGetValue(editingNoteCell, out PlacedObject noteObject))
                FinishNoteEditing(noteObject);

            LocationId fromLocation = currentLocation;
            Vector2Int triggerCell = pawnCell;
            currentLocation = GetTransitionTargetForCell(currentLocation, triggerCell);
            placedObjects = placedObjectsByLocation[currentLocation];

            DestroyChildren(objectsRoot);
            DestroyChildren(doorsRoot);
            CreateCenterDoor();
            EnsureLocationDefaults(currentLocation);
            RebuildPlacedObjectsForCurrentLocation();

            Vector2Int exitCell = GetCenterDoorCell() + GetArrivalOffsetFrom(fromLocation, currentLocation);
            pawnCell = ClampCell(exitCell);
            targetCell = pawnCell;
            selectedCell = pawnCell;
            hasMoveTarget = false;
            cameraWasManuallyMoved = true;

            if (pawn != null)
            {
                pawn.transform.position = CellToWorld(pawnCell) + new Vector3(0f, PawnGroundYOffset, 0f);
                cameraTargetPosition = ClampCamera(CameraPositionForFocus(pawn.transform.position));
            }

            pawnMoveInput = Vector2.zero;
            cameraMoveVelocity = Vector3.zero;
            doorPromptCooldownUntil = Time.realtimeSinceStartup + DoorPromptCooldownSeconds;
            mustLeavePortalBeforeTransition = true;
            SelectCell(pawnCell);
            SavePersistentWorld();
            SymbiozRuntimeLog.Write("TRANSITION", $"Transition complete location={GetLocationName(currentLocation)} cell={pawnCell.x}:{pawnCell.y}");
        }

        private void EnsureLocationDefaults(LocationId location)
        {
            if (!placedObjectsByLocation.TryGetValue(location, out Dictionary<Vector2Int, PlacedObject> targetObjects))
                return;

            if (targetObjects.Count > 0 && location != LocationId.FirstSoil)
                return;

            int countBeforeDefaults = targetObjects.Count;
            if (location == LocationId.FirstSoil)
            {
                int cx = GridSize / 2;
                int entranceY = GridSize - 14;
                AddDefaultObjectNear(targetObjects, location, new Vector2Int(cx - 18, entranceY), ObjectKind.StoneQuarry, "Slums visible stone quarry: mine stone.", true);
                AddDefaultObjectNear(targetObjects, location, new Vector2Int(cx + 12, entranceY), ObjectKind.Sawmill, "Slums visible sawmill: gather wood.", false);
                AddDefaultObjectNear(targetObjects, location, new Vector2Int(cx - 18, entranceY - 9), ObjectKind.Sign, "Stone quarry: use to mine stone.", true);
                AddDefaultObjectNear(targetObjects, location, new Vector2Int(cx + 12, entranceY - 9), ObjectKind.Sign, "Sawmill: use to gather wood.", false);
            }
            else if (location == LocationId.CityGate)
            {
                int cx = GridSize / 2;
                for (int y = 2; y <= 32; y++)
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx, y), ObjectKind.Road);

                for (int dx = -7; dx <= 7; dx++)
                {
                    if (Mathf.Abs(dx) <= 2)
                        continue;

                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, 8), ObjectKind.Wall);
                }

                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 9, 12), ObjectKind.Wall);
                AddDefaultObject(targetObjects, location, new Vector2Int(cx + 9, 12), ObjectKind.Wall);
                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 9, 13), ObjectKind.Wall);
                AddDefaultObject(targetObjects, location, new Vector2Int(cx + 9, 13), ObjectKind.Wall);
                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 5, 18), ObjectKind.Sign, "City Gate: approach the lower gate to enter the city center.");
                AddDefaultObject(targetObjects, location, new Vector2Int(cx + 4, 20), ObjectKind.BuildPlot);
                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 12, 21), ObjectKind.CommunityStorage, "Gate supply cache");
            }
            else if (location == LocationId.CityCenter)
            {
                int cx = GridSize / 2;
                int cy = GridSize / 2;
                for (int dx = -22; dx <= 22; dx++)
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, cy), ObjectKind.Road);

                for (int dy = -18; dy <= 18; dy++)
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx, cy + dy), ObjectKind.Road);

                for (int dx = -8; dx <= 8; dx++)
                {
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, cy + 6), ObjectKind.Road);
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, cy - 6), ObjectKind.Road);
                }

                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 13, cy + 8), ObjectKind.SlumTradeCenter, "Trade quarter west hall");
                AddDefaultObject(targetObjects, location, new Vector2Int(cx + 8, cy + 8), ObjectKind.SlumTradeCenter, "Trade quarter east hall");
                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 13, cy - 11), ObjectKind.CommunityStorage, "Market warehouse");
                AddDefaultObject(targetObjects, location, new Vector2Int(cx + 8, cy - 11), ObjectKind.CommunityStorage, "Craft supply depot");
                AddDefaultObject(targetObjects, location, new Vector2Int(cx - 2, cy + 3), ObjectKind.Sign, "City Center: trade quarter");

                for (int dx = -18; dx <= 18; dx++)
                {
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, cy + 15), ObjectKind.Wall);
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + dx, cy - 15), ObjectKind.Wall);
                }

                for (int dy = -12; dy <= 12; dy++)
                {
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx - 18, cy + dy), ObjectKind.Wall);
                    AddDefaultObject(targetObjects, location, new Vector2Int(cx + 18, cy + dy), ObjectKind.Wall);
                }
            }

            if (targetObjects.Count != countBeforeDefaults)
                SavePersistentWorld();
        }

        private void EnsureAllLocationDefaults()
        {
            foreach (LocationId location in Enum.GetValues(typeof(LocationId)))
                EnsureLocationDefaults(location);
        }

        private void AddDefaultObjectNear(
            Dictionary<Vector2Int, PlacedObject> targetObjects,
            LocationId location,
            Vector2Int preferredCell,
            ObjectKind kind,
            string note,
            bool searchLeft)
        {
            if (HasDefaultObject(targetObjects, kind, note))
                return;

            preferredCell = ClampCell(preferredCell);
            if (CanAddDefaultObject(targetObjects, location, preferredCell, kind))
            {
                AddDefaultObject(targetObjects, location, preferredCell, kind, note);
                return;
            }

            int direction = searchLeft ? -1 : 1;
            for (int radius = 1; radius <= 36; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int candidate = ClampCell(preferredCell + new Vector2Int(direction * radius, y));
                    if (CanAddDefaultObject(targetObjects, location, candidate, kind))
                    {
                        AddDefaultObject(targetObjects, location, candidate, kind, note);
                        return;
                    }
                }
            }
        }

        private static bool HasDefaultObject(Dictionary<Vector2Int, PlacedObject> targetObjects, ObjectKind kind, string note)
        {
            foreach (PlacedObject value in targetObjects.Values)
            {
                if (value == null || value.Kind != kind)
                    continue;

                bool exactNoteDefault = kind == ObjectKind.Sign
                    || kind == ObjectKind.StoneQuarry
                    || kind == ObjectKind.Sawmill;
                if (!exactNoteDefault || string.Equals(value.Note ?? string.Empty, note ?? string.Empty, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void AddDefaultObject(
            Dictionary<Vector2Int, PlacedObject> targetObjects,
            LocationId location,
            Vector2Int cell,
            ObjectKind kind,
            string note = "")
        {
            cell = ClampCell(cell);
            if (!CanAddDefaultObject(targetObjects, location, cell, kind))
                return;

            targetObjects[cell] = new PlacedObject
            {
                Kind = kind,
                Note = string.IsNullOrWhiteSpace(note)
                    ? kind == ObjectKind.BerryBush ? FormatBerryBushNote(BerryBushMaxBerries) : string.Empty
                    : note
            };
        }

        private bool CanAddDefaultObject(
            Dictionary<Vector2Int, PlacedObject> targetObjects,
            LocationId location,
            Vector2Int cell,
            ObjectKind kind)
        {
            Vector2Int size = GetObjectFootprintCells(kind);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int footprintCell = new Vector2Int(cell.x + x, cell.y + y);
                    if (footprintCell.x < 0
                        || footprintCell.x >= GridSize
                        || footprintCell.y < 0
                        || footprintCell.y >= GridSize
                        || IsPortalReservedCell(footprintCell, location))
                    {
                        return false;
                    }
                }
            }

            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in targetObjects)
            {
                if (pair.Value != null && ObjectFootprintsOverlap(pair.Key, pair.Value.Kind, cell, kind))
                    return false;
            }

            return true;
        }

        private string ResolveSelectedObjectLabel()
        {
            if (IsPortalReservedCell(selectedCell))
                return GetDoorLabel();

            if (placedObjects != null && placedObjects.TryGetValue(selectedCell, out PlacedObject placed))
            {
                if (placed.Kind == ObjectKind.Sign && !string.IsNullOrWhiteSpace(placed.Note))
                    return $"Sign: {placed.Note}";

                if (placed.Kind == ObjectKind.BerryBush)
                    return $"Berry bush: {ParseBerryBushBerries(placed.Note)}/{BerryBushMaxBerries}";

                if (placed.Kind == ObjectKind.Tree)
                    return $"Tree: {ParseTreeWood(placed.Note)} logs";

                if (placed.Kind == ObjectKind.SleepingBag && TryParseSleeperProfile(placed.Note, out PlayerSleeperProfile profile))
                    return $"Sleeper: {SafeStatusValue(profile.Nick)} / {SafeStatusValue(profile.Dynasty)}";

                return placed.Kind.ToString();
            }

            return "empty soil";
        }

        private void ReturnToMain()
        {
            SceneManager.LoadScene("Main");
        }

        private void RebuildPlacedObjectsForCurrentLocation()
        {
            var snapshots = new List<ObjectSnapshot>(placedObjects.Count);
            foreach (KeyValuePair<Vector2Int, PlacedObject> pair in placedObjects)
            {
                if (pair.Value != null)
                    snapshots.Add(new ObjectSnapshot(pair.Key, pair.Value.Kind, pair.Value.Note));
            }

            placedObjects = new Dictionary<Vector2Int, PlacedObject>();
            placedObjectsByLocation[currentLocation] = placedObjects;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ObjectSnapshot snapshot = snapshots[i];
                PlacedObject placed = snapshot.Kind == ObjectKind.Sign
                    ? CreateSign(snapshot.Cell)
                    : snapshot.Kind == ObjectKind.BerryBush
                        ? CreateBerryBushObject(snapshot.Cell, ParseBerryBushBerries(snapshot.Note))
                        : snapshot.Kind == ObjectKind.Tree
                            ? CreateTreeObject(snapshot.Cell, ParseTreeVariant(snapshot.Note), ParseTreeWood(snapshot.Note))
                            : CreateBlockObject(snapshot.Cell, snapshot.Kind);
                placed.Note = snapshot.Note ?? string.Empty;
                UpdateSignLabel(placed);
                UpdateSleepingBagStatusLabel(placed);
                placedObjects[snapshot.Cell] = placed;
            }

            RefreshAllConnectedBuildTiles();
        }

        private void SavePersistentWorld()
        {
            if (IsServerAuthoritativeWorldMode())
            {
                SymbiozRuntimeLog.Write("SAVE", "Local world save suppressed. FishNet dedicated server is authoritative.");
                return;
            }

            hasPendingPersistentSave = true;
            nextPersistentSaveTime = Time.unscaledTime + PersistentSaveDebounceSeconds;
            suppressSharedWorldDownloadUntil = Mathf.Max(suppressSharedWorldDownloadUntil, Time.unscaledTime + SharedWorldLocalEditGraceSeconds);
            SymbiozRuntimeLog.Write("SAVE", "Save scheduled. shared download paused for local edit.");
        }

        private void FlushPendingPersistentWorld()
        {
            if (IsServerAuthoritativeWorldMode())
            {
                hasPendingPersistentSave = false;
                return;
            }

            if (!hasPendingPersistentSave || Time.unscaledTime < nextPersistentSaveTime)
                return;

            hasPendingPersistentSave = false;
            try
            {
                string json = JsonUtility.ToJson(BuildWorldSaveData(), true);
                File.WriteAllText(GetPersistentWorldPath(), json);
                SymbiozRuntimeLog.Write("SAVE", $"World saved. objects={CountPlacedObjectsForLog()} path={GetPersistentWorldPath()}");

                if (enableSharedWorldSync && !isApplyingSharedWorld && Application.isPlaying && !string.IsNullOrWhiteSpace(sharedWorldEndpoint))
                    StartCoroutine(UploadSharedWorld(json));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not save Symbioz world: {exception.Message}");
            }
        }

        private WorldSaveData BuildWorldSaveData()
        {
            var save = new WorldSaveData
            {
                version = 1,
                currentLocation = currentLocation.ToString()
            };

            foreach (KeyValuePair<LocationId, Dictionary<Vector2Int, PlacedObject>> location in placedObjectsByLocation)
            {
                var locationSave = new LocationSaveData
                {
                    id = location.Key.ToString()
                };

                foreach (KeyValuePair<Vector2Int, PlacedObject> pair in location.Value)
                {
                    if (pair.Value == null)
                        continue;

                    locationSave.objects.Add(new ObjectSaveData
                    {
                        x = pair.Key.x,
                        y = pair.Key.y,
                        kind = pair.Value.Kind.ToString(),
                        note = pair.Value.Note ?? string.Empty
                    });
                }

                save.locations.Add(locationSave);
            }

            return save;
        }

        private void LoadPersistentWorld()
        {
            if (IsServerAuthoritativeWorldMode())
            {
                SymbiozRuntimeLog.Write("SAVE", "Local world load skipped. Waiting for FishNet dedicated snapshot.");
                return;
            }

            string path = GetPersistentWorldPath();
            if (!File.Exists(path))
            {
                SymbiozRuntimeLog.Write("SAVE", "No local world save found. path=" + path);
                return;
            }

            try
            {
                WorldSaveData save = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(path));
                ApplyWorldSaveData(save, false);
                SymbiozRuntimeLog.Write("SAVE", $"World loaded. objects={CountPlacedObjectsForLog()} path={path}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load Symbioz world: {exception.Message}");
            }
        }

        private int CountPlacedObjectsForLog()
        {
            int count = 0;
            foreach (Dictionary<Vector2Int, PlacedObject> locationObjects in placedObjectsByLocation.Values)
            {
                if (locationObjects != null)
                    count += locationObjects.Count;
            }

            return count;
        }

        private static int CountObjectsInSave(WorldSaveData save)
        {
            if (save == null || save.locations == null)
                return 0;

            int count = 0;
            foreach (LocationSaveData location in save.locations)
            {
                if (location != null && location.objects != null)
                    count += location.objects.Count;
            }

            return count;
        }

        private void ApplyWorldSaveData(WorldSaveData save, bool rebuildScene)
        {
            if (save == null || save.locations == null)
                return;

            isApplyingSharedWorld = true;
            LocationId previousLocation = currentLocation;
            Vector3 previousPawnPosition = pawn != null ? pawn.transform.position : Vector3.zero;
            Vector2Int previousPawnCell = pawnCell;
            Vector2Int previousTargetCell = targetCell;
            Vector2Int previousSelectedCell = selectedCell;
            bool previousHasMoveTarget = hasMoveTarget;

            foreach (Dictionary<Vector2Int, PlacedObject> locationObjects in placedObjectsByLocation.Values)
                locationObjects.Clear();

            foreach (LocationSaveData locationSave in save.locations)
            {
                if (locationSave == null || !Enum.TryParse(locationSave.id, out LocationId locationId))
                    continue;

                if (!placedObjectsByLocation.TryGetValue(locationId, out Dictionary<Vector2Int, PlacedObject> locationObjects))
                    continue;

                if (locationSave.objects == null)
                    continue;

                foreach (ObjectSaveData objectSave in locationSave.objects)
                {
                    if (objectSave == null || !Enum.TryParse(objectSave.kind, out ObjectKind kind))
                        continue;

                    Vector2Int cell = ClampCell(new Vector2Int(objectSave.x, objectSave.y));
                    if (IsPortalReservedCell(cell, locationId))
                        continue;

                    locationObjects[cell] = new PlacedObject
                    {
                        Kind = kind,
                        Note = objectSave.note ?? string.Empty
                    };
                }
            }

            if (!rebuildScene
                && Enum.TryParse(save.currentLocation, out LocationId loadedLocation)
                && placedObjectsByLocation.ContainsKey(loadedLocation))
            {
                currentLocation = loadedLocation;
            }
            else if (rebuildScene)
            {
                currentLocation = previousLocation;
            }

            EnsureAllLocationDefaults();
            placedObjects = placedObjectsByLocation[currentLocation];

            if (rebuildScene && objectsRoot != null && doorsRoot != null)
            {
                DestroyChildren(objectsRoot);
                DestroyChildren(doorsRoot);
                CreateCenterDoor();
                RebuildPlacedObjectsForCurrentLocation();

                pawnCell = previousPawnCell;
                targetCell = previousTargetCell;
                selectedCell = previousSelectedCell;
                hasMoveTarget = previousHasMoveTarget;
                if (pawn != null)
                {
                    pawn.transform.position = ClampPawnPosition(previousPawnPosition);
                    RefreshPawnRenderOrder();
                    if (!cameraWasManuallyMoved)
                        cameraTargetPosition = ClampCamera(CameraPositionForFocus(pawn.transform.position));
                }
                SelectCell(previousSelectedCell);
            }

            if (!IsServerAuthoritativeWorldMode())
            {
                try
                {
                    File.WriteAllText(GetPersistentWorldPath(), JsonUtility.ToJson(BuildWorldSaveData(), true));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not cache Symbioz world locally: {exception.Message}");
                }
            }

            isApplyingSharedWorld = false;
        }

        private void TryStartSharedWorldDownload()
        {
            if (IsServerAuthoritativeWorldMode())
            {
                SymbiozRuntimeLog.Write("SHARED", "HTTP shared world sync disabled. FishNet dedicated server is authoritative.");
                return;
            }

            if (!enableSharedWorldSync || !Application.isPlaying || string.IsNullOrWhiteSpace(sharedWorldEndpoint))
                return;

            if (sharedWorldPollingRoutine == null)
                sharedWorldPollingRoutine = StartCoroutine(PollSharedWorld());
        }

        private IEnumerator PollSharedWorld()
        {
            while (enabled && enableSharedWorldSync && !string.IsNullOrWhiteSpace(sharedWorldEndpoint))
            {
                if (!hasPendingPersistentSave && !isApplyingSharedWorld && Time.unscaledTime >= suppressSharedWorldDownloadUntil)
                {
                    yield return DownloadSharedWorld();
                }
                else if (Time.unscaledTime < suppressSharedWorldDownloadUntil)
                {
                    SymbiozRuntimeLog.Write("SHARED", "World download skipped while local edit is settling.");
                }

                yield return new WaitForSecondsRealtime(SharedWorldDownloadIntervalSeconds);
            }

            sharedWorldPollingRoutine = null;
        }

        private IEnumerator DownloadSharedWorld()
        {
            if (IsServerAuthoritativeWorldMode())
                yield break;

            UnityWebRequest request = UnityWebRequest.Get(sharedWorldEndpoint);
            request.timeout = 8;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download shared Symbioz world: {request.error}");
                request.Dispose();
                yield break;
            }

            string json = request.downloadHandler.text;
            int worldHash = string.IsNullOrEmpty(json) ? 0 : json.GetHashCode();
            if (worldHash == lastSharedWorldHash)
            {
                request.Dispose();
                yield break;
            }

            WorldSaveData save = JsonUtility.FromJson<WorldSaveData>(json);
            request.Dispose();
            int remoteObjectCount = CountObjectsInSave(save);
            int localObjectCount = CountPlacedObjectsForLog();
            if (!hasCompletedInitialSharedWorldCheck && remoteObjectCount == 0 && localObjectCount > 0)
            {
                hasCompletedInitialSharedWorldCheck = true;
                string localJson = JsonUtility.ToJson(BuildWorldSaveData(), true);
                SymbiozRuntimeLog.Write("SHARED", $"Remote world is empty. Uploading local world instead. localObjects={localObjectCount}");
                yield return UploadSharedWorld(localJson);
                yield break;
            }

            if (remoteObjectCount < localObjectCount)
            {
                hasCompletedInitialSharedWorldCheck = true;
                string localJson = JsonUtility.ToJson(BuildWorldSaveData(), true);
                SymbiozRuntimeLog.Write("SHARED", $"Remote world is older/smaller. Keeping local world. localObjects={localObjectCount} remoteObjects={remoteObjectCount}");
                yield return UploadSharedWorld(localJson);
                yield break;
            }

            hasCompletedInitialSharedWorldCheck = true;
            lastSharedWorldHash = worldHash;
            ApplyWorldSaveData(save, true);
            SymbiozRuntimeLog.Write("SHARED", $"World downloaded. objects={remoteObjectCount}");
        }

        private IEnumerator UploadSharedWorld(string json)
        {
            if (IsServerAuthoritativeWorldMode())
                yield break;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest request = new UnityWebRequest(sharedWorldEndpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 8
            };
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not upload shared Symbioz world: {request.error}");
            }
            else
            {
                lastSharedWorldHash = string.IsNullOrEmpty(json) ? 0 : json.GetHashCode();
                suppressSharedWorldDownloadUntil = Mathf.Max(suppressSharedWorldDownloadUntil, Time.unscaledTime + SharedWorldDownloadIntervalSeconds);
                SymbiozRuntimeLog.Write("SHARED", $"World uploaded. bytes={bytes.Length} objects={CountPlacedObjectsForLog()}");
            }

            request.Dispose();
        }

        private void TryStartSharedPlayerPresence()
        {
            if (useFishNetRealtime)
                return;

            if (!enableSharedWorldSync || !Application.isPlaying || string.IsNullOrWhiteSpace(sharedPlayersEndpoint))
                return;

            if (sharedPlayerPresenceRoutine == null)
                sharedPlayerPresenceRoutine = StartCoroutine(PollSharedPlayerPresence());
        }

        private IEnumerator PollSharedPlayerPresence()
        {
            while (enabled && enableSharedWorldSync && !string.IsNullOrWhiteSpace(sharedPlayersEndpoint))
            {
                if (!isPlayerPresenceRequestInFlight && pawn != null)
                    yield return SyncPlayerPresence();

                yield return new WaitForSecondsRealtime(IsLocalPawnMovingForNetwork()
                    ? SharedPlayerMovingPresenceIntervalSeconds
                    : SharedPlayerPresenceIntervalSeconds);
            }

            sharedPlayerPresenceRoutine = null;
        }

        private static bool IsFishNetRealtimeRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "-fishnet-client", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-fishnet-host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-fishnet-server", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-symbioz-direct", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return RealtimeNetworkBootstrap.I != null && RealtimeNetworkBootstrap.I.IsClientStarted;
        }

        private bool IsServerAuthoritativeWorldMode()
        {
            return useFishNetRealtime || IsFishNetRealtimeRequested();
        }

        private IEnumerator SyncPlayerPresence()
        {
            isPlayerPresenceRequestInFlight = true;
            float nowRealtime = Time.realtimeSinceStartup;
            Vector3 currentPosition = pawn != null ? pawn.transform.position : Vector3.zero;
            float presenceDeltaSeconds = lastPresenceRealtime > 0f
                ? Mathf.Max(0.001f, nowRealtime - lastPresenceRealtime)
                : SharedPlayerMovingPresenceIntervalSeconds;
            Vector3 networkVelocity = lastPresenceRealtime > 0f
                ? (currentPosition - lastPresencePosition) / presenceDeltaSeconds
                : Vector3.zero;
            Vector2 networkFacing = ResolveLocalNetworkFacing(networkVelocity);
            lastPresencePosition = currentPosition;
            lastPresenceRealtime = nowRealtime;
            localPresenceSequence++;

            var post = new PlayerPresencePostData
            {
                clientId = string.IsNullOrWhiteSpace(localClientId) ? ResolveLocalClientId() : localClientId,
                displayName = string.IsNullOrWhiteSpace(localDisplayName) ? ResolveLocalDisplayName() : localDisplayName,
                dynasty = string.IsNullOrWhiteSpace(localDynastyName) ? ResolveLocalDynastyName() : localDynastyName,
                age = localPlayerAge <= 0 ? ResolveLocalPlayerAge() : localPlayerAge,
                location = currentLocation.ToString(),
                x = currentPosition.x,
                z = currentPosition.z,
                cellX = pawnCell.x,
                cellY = pawnCell.y,
                moving = IsLocalPawnMovingForNetwork(),
                sentAtMs = UnixNowMs(),
                sequence = localPresenceSequence,
                velocityX = networkVelocity.x,
                velocityZ = networkVelocity.z,
                facingX = networkFacing.x,
                facingZ = networkFacing.y,
                hp = pawnHp,
                satiety = pawnSatiety,
                carryWeight = carriedWeight
            };

            string json = JsonUtility.ToJson(post);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            UnityWebRequest request = new UnityWebRequest(sharedPlayersEndpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 4
            };
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    PlayerPresenceResponse response = JsonUtility.FromJson<PlayerPresenceResponse>(request.downloadHandler.text);
                    if (response != null && response.serverNowMs > 0)
                    {
                        estimatedServerNowMs = response.serverNowMs;
                        estimatedServerRealtime = Time.realtimeSinceStartup;
                    }

                    ApplyPlayerPresence(response);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not parse shared Symbioz players: {exception.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not sync shared Symbioz players: {request.error}");
            }

            request.Dispose();
            isPlayerPresenceRequestInFlight = false;
        }

        private bool IsLocalPawnMovingForNetwork()
        {
            if (pawn == null)
                return false;

            return hasMoveTarget
                || hasAutoMoveTarget
                || pawnMoveInput.sqrMagnitude > 0.001f;
        }

        private Vector2 ResolveLocalNetworkFacing(Vector3 velocity)
        {
            Vector2 facing = new Vector2(velocity.x, velocity.z);
            if (facing.sqrMagnitude > 0.0001f)
            {
                lastPresenceFacing = facing.normalized;
                return lastPresenceFacing;
            }

            if (pawnMoveInput.sqrMagnitude > 0.001f)
            {
                lastPresenceFacing = pawnMoveInput.normalized;
                return lastPresenceFacing;
            }

            return lastPresenceFacing.sqrMagnitude > 0.001f ? lastPresenceFacing : Vector2.down;
        }

        private long EstimateServerNowMs(float realtime)
        {
            if (estimatedServerNowMs <= 0 || estimatedServerRealtime <= 0f)
                return UnixNowMs();

            return estimatedServerNowMs + (long)((realtime - estimatedServerRealtime) * 1000f);
        }

        private void ApplyPlayerPresence(PlayerPresenceResponse response)
        {
            if (response == null || response.players == null)
                return;

            var seen = new HashSet<string>();
            foreach (PlayerPresenceData playerData in response.players)
            {
                if (playerData == null || string.IsNullOrWhiteSpace(playerData.clientId))
                    continue;

                if (string.Equals(playerData.clientId, localClientId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(playerData.location, currentLocation.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;

                seen.Add(playerData.clientId);
                if (!remotePawns.TryGetValue(playerData.clientId, out RemotePawn remotePawn))
                {
                    remotePawn = CreateRemotePawn(playerData);
                    remotePawns[playerData.clientId] = remotePawn;
                    SymbiozRuntimeLog.Write("PLAYERS", $"Remote player joined view id={playerData.clientId} name={playerData.displayName}");
                }

                Vector3 nextTarget = ClampPawnPosition(new Vector3(playerData.x, 0.08f, playerData.z));
                Vector3 networkVelocity = new Vector3(playerData.velocityX, 0f, playerData.velocityZ);
                Vector2 networkFacing = new Vector2(playerData.facingX, playerData.facingZ);
                if (networkFacing.sqrMagnitude <= 0.0001f && networkVelocity.sqrMagnitude > 0.0001f)
                    networkFacing = new Vector2(networkVelocity.x, networkVelocity.z).normalized;
                if (networkFacing.sqrMagnitude <= 0.0001f)
                    networkFacing = remotePawn.LastNetworkFacing.sqrMagnitude > 0.0001f ? remotePawn.LastNetworkFacing : Vector2.down;
                Vector2Int nextCell = new Vector2Int(playerData.cellX, playerData.cellY);
                if (nextCell != remotePawn.LastCell)
                    SymbiozRuntimeLog.Write("PLAYERS", $"Remote player moved id={playerData.clientId} cell={nextCell.x:000}:{nextCell.y:000}");

                long sampleServerSeenMs = playerData.serverSeenMs > 0
                    ? playerData.serverSeenMs
                    : EstimateServerNowMs(Time.realtimeSinceStartup);
                AddRemotePawnSample(
                    remotePawn,
                    nextTarget,
                    Time.realtimeSinceStartup,
                    playerData.moving,
                    playerData.sequence,
                    sampleServerSeenMs,
                    networkVelocity,
                    networkFacing);
                remotePawn.LastCell = nextCell;
                remotePawn.LastSeenRealtime = Time.realtimeSinceStartup;
                remotePawn.LastNetworkMoving = playerData.moving;
                remotePawn.LastNetworkVelocity = networkVelocity;
                remotePawn.LastNetworkFacing = networkFacing;
                if (remotePawn.Label != null)
                    remotePawn.Label.text = FormatRemotePawnLabel(playerData);
                if (remotePawn.Root != null && !remotePawn.Root.activeSelf)
                    remotePawn.Root.SetActive(true);
            }

            var remove = new List<string>();
            foreach (KeyValuePair<string, RemotePawn> pair in remotePawns)
            {
                if (!seen.Contains(pair.Key) && Time.realtimeSinceStartup - pair.Value.LastSeenRealtime > 5f)
                    remove.Add(pair.Key);
            }

            foreach (string key in remove)
            {
                if (remotePawns.TryGetValue(key, out RemotePawn remotePawn) && remotePawn.Root != null)
                    Destroy(remotePawn.Root);
                remotePawns.Remove(key);
            }
        }

        private RemotePawn CreateRemotePawn(PlayerPresenceData playerData)
        {
            var root = new GameObject("RemoteArchitect_" + SanitizeFileSuffix(playerData.clientId));
            root.transform.SetParent(playersRoot != null ? playersRoot : worldRoot, false);
            root.transform.position = ClampPawnPosition(new Vector3(playerData.x, 0.08f, playerData.z));

            GameObject sprite = new GameObject("RemoteArchitectSprite", typeof(MeshFilter), typeof(MeshRenderer));
            sprite.transform.SetParent(root.transform, false);
            sprite.transform.localPosition = new Vector3(0f, 0.13f, 0.07f);
            MeshFilter spriteMeshFilter = sprite.GetComponent<MeshFilter>();
            spriteMeshFilter.sharedMesh = CreatePawnQuadMesh("RemoteArchitectSprite_Mesh", ResolvePawnUv(3, 0, false));
            MeshRenderer spriteRenderer = sprite.GetComponent<MeshRenderer>();
            spriteRenderer.sharedMaterial = pawnIdleMaterial != null
                ? pawnIdleMaterial
                : pawnSpriteMaterial != null
                    ? pawnSpriteMaterial
                    : CreateFallbackPawnMaterial();
            spriteRenderer.sortingOrder = ResolveWorldSortOrder(root.transform.position.z);

            var labelObject = new GameObject("RemoteName");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.14f, -0.96f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.text = FormatRemotePawnLabel(playerData);
            label.fontSize = 2.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.72f, 1f, 0.82f, 1f);

            return new RemotePawn
            {
                Root = root,
                SpriteMeshFilter = spriteMeshFilter,
                SpriteRenderer = spriteRenderer,
                Label = label,
                TargetWorld = root.transform.position,
                LastRenderPosition = root.transform.position,
                LastCell = new Vector2Int(playerData.cellX, playerData.cellY),
                LastSeenRealtime = Time.realtimeSinceStartup,
                LastFacingView = 0,
                LastFacingFlipX = false,
                ViewIndex = 3,
                ViewFrame = 0,
                ViewFlipX = false,
                LastNetworkMoving = playerData.moving,
                LastNetworkSequence = Math.Max(0, playerData.sequence),
                LastNetworkServerSeenMs = Math.Max(0, playerData.serverSeenMs),
                LastNetworkVelocity = new Vector3(playerData.velocityX, 0f, playerData.velocityZ),
                LastNetworkFacing = new Vector2(playerData.facingX, playerData.facingZ)
            };
        }

        private static string FormatRemotePawnLabel(PlayerPresenceData playerData)
        {
            string name = string.IsNullOrWhiteSpace(playerData.displayName) ? "Architect" : playerData.displayName;
            return string.IsNullOrWhiteSpace(playerData.dynasty) ? name : $"{name}\n{playerData.dynasty}";
        }

        private static void AddRemotePawnSample(
            RemotePawn remotePawn,
            Vector3 world,
            float receivedRealtime,
            bool moving,
            int sequence,
            long serverSeenMs,
            Vector3 velocity,
            Vector2 facing)
        {
            if (remotePawn == null)
                return;

            if (sequence > 0 && remotePawn.LastNetworkSequence > 0 && sequence < remotePawn.LastNetworkSequence)
                return;

            if (sequence > 0)
                remotePawn.LastNetworkSequence = sequence;
            if (serverSeenMs > 0)
                remotePawn.LastNetworkServerSeenMs = serverSeenMs;

            remotePawn.TargetWorld = world;
            List<RemotePawnSample> samples = remotePawn.Samples;
            if (samples.Count == 0 || Vector3.SqrMagnitude(samples[samples.Count - 1].World - world) > 0.0001f)
                samples.Add(new RemotePawnSample(receivedRealtime, world, moving, sequence, serverSeenMs, velocity, facing));
            else
                samples[samples.Count - 1] = new RemotePawnSample(receivedRealtime, world, moving, sequence, serverSeenMs, velocity, facing);

            while (samples.Count > 10)
                samples.RemoveAt(0);

            float cutoff = receivedRealtime - 2f;
            while (samples.Count > 2 && samples[1].ReceivedRealtime < cutoff)
                samples.RemoveAt(0);
        }

        private void CreateRemotePawnPart(Transform parent, string partName, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            SetRendererColor(part, color);
        }

        private void UpdateRemotePawns()
        {
            foreach (RemotePawn remotePawn in remotePawns.Values)
            {
                if (remotePawn?.Root == null)
                    continue;

                Vector3 desired = ResolveRemotePawnRenderPosition(remotePawn, Time.realtimeSinceStartup);
                float distance = Vector3.Distance(remotePawn.Root.transform.position, desired);
                Vector3 before = remotePawn.Root.transform.position;
                if (distance > RemotePawnSnapDistance)
                {
                    remotePawn.Root.transform.position = desired;
                    remotePawn.SmoothVelocity = Vector3.zero;
                    Vector3 snapDelta = desired - remotePawn.LastRenderPosition;
                    if (snapDelta.sqrMagnitude <= 0.000015f && remotePawn.LastNetworkVelocity.sqrMagnitude > 0.0001f)
                        snapDelta = remotePawn.LastNetworkVelocity * Time.deltaTime;
                    UpdateRemotePawnFacing(remotePawn, snapDelta, false);
                    remotePawn.LastRenderPosition = desired;
                    RefreshRemotePawnRenderOrder(remotePawn);
                    continue;
                }

                remotePawn.Root.transform.position = Vector3.SmoothDamp(
                    remotePawn.Root.transform.position,
                    desired,
                    ref remotePawn.SmoothVelocity,
                    RemotePawnVisualSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime);
                Vector3 after = remotePawn.Root.transform.position;
                Vector3 delta = after - before;
                bool visuallyMoving = delta.sqrMagnitude > 0.000015f
                    || remotePawn.LastNetworkMoving
                    || remotePawn.LastNetworkVelocity.sqrMagnitude > 0.0001f;
                Vector3 animationDelta = delta.sqrMagnitude > 0.000015f
                    ? delta
                    : remotePawn.LastNetworkVelocity * Time.deltaTime;
                UpdateRemotePawnFacing(remotePawn, animationDelta, visuallyMoving);
                remotePawn.LastRenderPosition = after;
                RefreshRemotePawnRenderOrder(remotePawn);
            }
        }

        private void UpdateRemotePawnFacing(RemotePawn remotePawn, Vector3 delta, bool moving)
        {
            if (remotePawn?.SpriteMeshFilter == null)
                return;

            int nextView = remotePawn.ViewIndex;
            bool nextFlip = false;
            int nextFrame = 0;
            bool nextUsesIdleMaterial = !moving;
            Vector2 direction = new Vector2(delta.x, delta.z);

            if (!nextUsesIdleMaterial)
            {
                if (direction.sqrMagnitude <= 0.00001f && remotePawn.LastNetworkFacing.sqrMagnitude > 0.0001f)
                    direction = remotePawn.LastNetworkFacing;

                if (direction.sqrMagnitude > 0.00001f)
                {
                    ResolveScreenFacingView(direction, out nextView, out nextFlip);

                    remotePawn.LastFacingView = nextView;
                    remotePawn.LastFacingFlipX = nextFlip;
                }
                else
                {
                    nextView = remotePawn.LastFacingView;
                    nextFlip = remotePawn.LastFacingFlipX && nextView == 2;
                }

                remotePawn.AnimationTimer += Time.deltaTime;
                if (remotePawn.AnimationTimer >= PawnWalkFrameSeconds)
                {
                    remotePawn.AnimationTimer -= PawnWalkFrameSeconds;
                    remotePawn.AnimationFrame = (remotePawn.AnimationFrame + 1) % PawnWalkColumns;
                }

                nextFrame = remotePawn.AnimationFrame;
            }
            else
            {
                nextView = ResolveIdleViewForFacing(remotePawn.LastFacingView);
                nextFlip = false;
                remotePawn.AnimationFrame = 0;
                remotePawn.AnimationTimer = 0f;
            }

            ApplyRemotePawnAnimationMaterial(remotePawn, nextUsesIdleMaterial);

            if (nextView == remotePawn.ViewIndex && nextFrame == remotePawn.ViewFrame && nextFlip == remotePawn.ViewFlipX && nextUsesIdleMaterial == remotePawn.ViewUsesIdleMaterial)
                return;

            remotePawn.ViewIndex = nextView;
            remotePawn.ViewFrame = nextFrame;
            remotePawn.ViewFlipX = nextFlip;
            remotePawn.ViewUsesIdleMaterial = nextUsesIdleMaterial;
            remotePawn.SpriteMeshFilter.sharedMesh = CreatePawnQuadMesh(
                $"RemoteArchitectSprite_Mesh_{nextView}_{nextFrame}_{nextFlip}",
                ResolvePawnUv(nextView, nextFrame, nextFlip));
        }

        private void ApplyRemotePawnAnimationMaterial(RemotePawn remotePawn, bool idle)
        {
            if (remotePawn?.SpriteRenderer == null)
                return;

            Material material = idle && pawnIdleMaterial != null
                ? pawnIdleMaterial
                : pawnSpriteMaterial != null
                    ? pawnSpriteMaterial
                    : CreateFallbackPawnMaterial();
            if (remotePawn.SpriteRenderer.sharedMaterial != material)
                remotePawn.SpriteRenderer.sharedMaterial = material;
        }

        private static void RefreshRemotePawnRenderOrder(RemotePawn remotePawn)
        {
            if (remotePawn?.SpriteRenderer == null || remotePawn.Root == null)
                return;

            remotePawn.SpriteRenderer.sortingOrder = ResolveWorldSortOrder(remotePawn.Root.transform.position.z);
        }

        private static Vector3 ResolveRemotePawnRenderPosition(RemotePawn remotePawn, float now)
        {
            List<RemotePawnSample> samples = remotePawn.Samples;
            if (samples == null || samples.Count == 0)
                return remotePawn.TargetWorld;

            if (samples.Count == 1)
                return samples[0].World;

            float renderTime = now - RemotePawnInterpolationDelaySeconds;
            while (samples.Count > 2 && samples[1].ReceivedRealtime <= renderTime)
                samples.RemoveAt(0);

            RemotePawnSample first = samples[0];
            RemotePawnSample second = samples[1];
            if (renderTime <= second.ReceivedRealtime)
            {
                float duration = Mathf.Max(0.001f, second.ReceivedRealtime - first.ReceivedRealtime);
                float t = Mathf.Clamp01((renderTime - first.ReceivedRealtime) / duration);
                return Vector3.Lerp(first.World, second.World, t);
            }

            float sampleDelta = Mathf.Max(0.001f, second.ReceivedRealtime - first.ReceivedRealtime);
            float extrapolateSeconds = Mathf.Min(renderTime - second.ReceivedRealtime, RemotePawnMaxExtrapolationSeconds);
            Vector3 velocity = second.Velocity.sqrMagnitude > 0.0001f
                ? second.Velocity
                : (second.World - first.World) / sampleDelta;
            velocity = Vector3.ClampMagnitude(velocity, 16f);
            return second.World + velocity * Mathf.Max(0f, extrapolateSeconds);
        }

        private static long UnixNowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void RegisterLocalProfile()
        {
            if (hasRegisteredLocalProfile)
                return;

            hasRegisteredLocalProfile = true;
            SymbiozRuntimeLog.Write("PROFILE", $"Registered local profile id={localClientId} nick={localDisplayName} dynasty={localDynastyName} age={localPlayerAge}");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                DropExitSleepingBagAndUpload("pause");
        }

        private void OnApplicationQuit()
        {
            DropExitSleepingBagAndUpload("quit");
        }

        private void DropExitSleepingBagAndUpload(string reason)
        {
            if (hasDroppedExitSleeper || Application.isBatchMode || pawn == null || placedObjects == null)
                return;

            hasDroppedExitSleeper = true;
            localClientId = string.IsNullOrWhiteSpace(localClientId) ? ResolveLocalClientId() : localClientId;
            localDisplayName = string.IsNullOrWhiteSpace(localDisplayName) ? ResolveLocalDisplayName() : localDisplayName;
            localDynastyName = string.IsNullOrWhiteSpace(localDynastyName) ? ResolveLocalDynastyName() : localDynastyName;
            if (localPlayerAge <= 0)
                localPlayerAge = ResolveLocalPlayerAge();

            RemoveExistingSleeperForClient(localClientId);
            Vector2Int sleeperCell = FindExitSleeperCell();
            string note = BuildSleeperProfileNote(sleeperCell);

            if (placedObjects.TryGetValue(sleeperCell, out PlacedObject oldObject))
            {
                if (oldObject.Root != null)
                    Destroy(oldObject.Root);
                placedObjects.Remove(sleeperCell);
            }

            PlacedObject sleeper = CreateBlockObject(sleeperCell, ObjectKind.SleepingBag);
            sleeper.Note = note;
            UpdateSleepingBagStatusLabel(sleeper);
            placedObjects[sleeperCell] = sleeper;
            fishNetWorldBridge?.SubmitBuildCommand((int)currentLocation, sleeperCell.x, sleeperCell.y, (int)ObjectKind.SleepingBag, note);

            if (IsServerAuthoritativeWorldMode())
            {
                SymbiozRuntimeLog.Write("PROFILE", $"Exit sleeper submitted to FishNet server reason={reason} cell={sleeperCell.x}:{sleeperCell.y} nick={localDisplayName}");
                return;
            }

            string json = JsonUtility.ToJson(BuildWorldSaveData(), true);
            try
            {
                File.WriteAllText(GetPersistentWorldPath(), json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write exit sleeper save: {exception.Message}");
            }

            if (enableSharedWorldSync && !string.IsNullOrWhiteSpace(sharedWorldEndpoint))
                UploadSharedWorldBlocking(json, string.Equals(reason, "quit", StringComparison.OrdinalIgnoreCase) ? 2200 : 650);

            SymbiozRuntimeLog.Write("PROFILE", $"Exit sleeper dropped reason={reason} cell={sleeperCell.x}:{sleeperCell.y} nick={localDisplayName}");
        }

        private Vector2Int FindExitSleeperCell()
        {
            Vector2Int baseCell = ClampCell(pawnCell);
            if (CanPlaceExitSleeperAt(baseCell))
                return baseCell;

            for (int radius = 1; radius <= 5; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                            continue;

                        Vector2Int candidate = ClampCell(baseCell + new Vector2Int(x, y));
                        if (CanPlaceExitSleeperAt(candidate))
                            return candidate;
                    }
                }
            }

            return baseCell;
        }

        private bool CanPlaceExitSleeperAt(Vector2Int cell)
        {
            if (IsPortalReservedCell(cell))
                return false;

            if (placedObjects == null || !placedObjects.TryGetValue(cell, out PlacedObject placed))
                return true;

            return placed.Kind == ObjectKind.SleepingBag && IsSleeperForClient(placed.Note, localClientId);
        }

        private bool RemoveExistingSleeperForClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return false;

            bool removedAny = false;
            foreach (Dictionary<Vector2Int, PlacedObject> locationObjects in placedObjectsByLocation.Values)
            {
                if (locationObjects == null)
                    continue;

                var remove = new List<Vector2Int>();
                foreach (KeyValuePair<Vector2Int, PlacedObject> pair in locationObjects)
                {
                    if (pair.Value != null && pair.Value.Kind == ObjectKind.SleepingBag && IsSleeperForClient(pair.Value.Note, clientId))
                        remove.Add(pair.Key);
                }

                for (int i = 0; i < remove.Count; i++)
                {
                    Vector2Int cell = remove[i];
                    if (locationObjects.TryGetValue(cell, out PlacedObject placed) && placed.Root != null)
                        Destroy(placed.Root);
                    locationObjects.Remove(cell);
                    removedAny = true;
                }
            }

            return removedAny;
        }

        private bool TryOpenSelectedPlayerSleeperStatus()
        {
            Vector2Int cell = hasSelectedObject ? selectedObjectAnchorCell : selectedCell;
            if (placedObjects == null
                || !placedObjects.TryGetValue(cell, out PlacedObject placed)
                || placed.Kind != ObjectKind.SleepingBag
                || !TryParseSleeperProfile(placed.Note, out PlayerSleeperProfile profile))
            {
                return false;
            }

            OpenPlayerStatusPanel(profile);
            return true;
        }

        private void OpenPlayerStatusPanel(PlayerSleeperProfile profile)
        {
            if (playerStatusPanel == null || playerStatusText == null)
                return;

            playerStatusText.text =
                $"Nick: {SafeStatusValue(profile.Nick)}\n" +
                $"Hanedan: {SafeStatusValue(profile.Dynasty)}\n" +
                $"Age: {Mathf.Max(0, profile.Age)}\n" +
                $"Client ID: {SafeStatusValue(profile.ClientId)}\n\n" +
                $"Status\n" +
                $"State: Offline / resting\n" +
                $"Last seen: {SafeStatusValue(profile.LastSeen)}\n" +
                $"Location: {SafeStatusValue(profile.Location)}\n" +
                $"Cell: {profile.CellX:000}:{profile.CellY:000}\n\n" +
                $"Survival\n" +
                $"HP: {profile.Hp:0}/{100}\n" +
                $"Satiety: {profile.Satiety:0}/{SatietyMax:0}\n" +
                $"Carry weight: {profile.CarryWeight:0.0}/{CarryWeightMax:0} kg\n" +
                $"Berries: {profile.Berries}\n\n" +
                $"This sleeping bag remains in the shared server world after the player leaves.";

            playerStatusPanel.gameObject.SetActive(true);
            SymbiozRuntimeLog.Write("PROFILE", $"Opened sleeper status nick={profile.Nick} client={profile.ClientId}");
        }

        private void ClosePlayerStatusPanel()
        {
            if (playerStatusPanel != null)
                playerStatusPanel.gameObject.SetActive(false);
        }

        private string BuildSleeperProfileNote(Vector2Int cell)
        {
            return string.Join(";",
                SleeperNotePrefix,
                "client=" + EncodeNoteValue(localClientId),
                "nick=" + EncodeNoteValue(localDisplayName),
                "dynasty=" + EncodeNoteValue(localDynastyName),
                "age=" + Mathf.Max(1, localPlayerAge),
                "location=" + EncodeNoteValue(GetLocationName(currentLocation)),
                "x=" + cell.x,
                "y=" + cell.y,
                "hp=" + Mathf.RoundToInt(pawnHp),
                "satiety=" + Mathf.RoundToInt(pawnSatiety),
                "carry=" + carriedWeight.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                "berries=" + carriedBerries,
                "last=" + EncodeNoteValue(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")));
        }

        private static bool IsPlayerSleeperNote(string note)
        {
            return !string.IsNullOrWhiteSpace(note) && note.Contains(SleeperNotePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSleeperForClient(string note, string clientId)
        {
            return TryParseSleeperProfile(note, out PlayerSleeperProfile profile)
                && string.Equals(profile.ClientId, clientId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseSleeperProfile(string note, out PlayerSleeperProfile profile)
        {
            profile = null;
            if (!IsPlayerSleeperNote(note))
                return false;

            Dictionary<string, string> values = ParseNoteKeyValues(note);
            profile = new PlayerSleeperProfile
            {
                ClientId = GetNoteValue(values, "client"),
                Nick = GetNoteValue(values, "nick"),
                Dynasty = GetNoteValue(values, "dynasty"),
                Age = ParseNoteInt(values, "age", 18),
                Location = GetNoteValue(values, "location"),
                CellX = ParseNoteInt(values, "x", 0),
                CellY = ParseNoteInt(values, "y", 0),
                Hp = ParseNoteFloat(values, "hp", 100f),
                Satiety = ParseNoteFloat(values, "satiety", SatietyMax),
                CarryWeight = ParseNoteFloat(values, "carry", 0f),
                Berries = ParseNoteInt(values, "berries", 0),
                LastSeen = GetNoteValue(values, "last")
            };

            return true;
        }

        private static Dictionary<string, string> ParseNoteKeyValues(string note)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] parts = note.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                int index = part.IndexOf('=');
                if (index <= 0)
                    continue;

                values[part.Substring(0, index)] = DecodeNoteValue(part.Substring(index + 1));
            }

            return values;
        }

        private static string GetNoteValue(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private static int ParseNoteInt(Dictionary<string, string> values, string key, int fallback)
        {
            return values.TryGetValue(key, out string value) && int.TryParse(value, out int result) ? result : fallback;
        }

        private static float ParseNoteFloat(Dictionary<string, string> values, string key, float fallback)
        {
            return values.TryGetValue(key, out string value)
                && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result)
                    ? result
                    : fallback;
        }

        private static string EncodeNoteValue(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string DecodeNoteValue(string value)
        {
            return Uri.UnescapeDataString(value ?? string.Empty);
        }

        private static string SafeStatusValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }

        private void LoadResourceInventory()
        {
            carriedStone = Mathf.Max(0, PlayerPrefs.GetInt(ResourceInventoryStonePrefsKey, 0));
            carriedWood = Mathf.Max(0, PlayerPrefs.GetInt(ResourceInventoryWoodPrefsKey, 0));
        }

        private void SaveResourceInventory()
        {
            PlayerPrefs.SetInt(ResourceInventoryStonePrefsKey, Mathf.Max(0, carriedStone));
            PlayerPrefs.SetInt(ResourceInventoryWoodPrefsKey, Mathf.Max(0, carriedWood));
            PlayerPrefs.Save();
        }

        private void UploadSharedWorldBlocking(string json, int timeoutMs)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json ?? string.Empty);
            using UnityWebRequest request = new UnityWebRequest(sharedWorldEndpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutMs / 1000f))
            };
            request.SetRequestHeader("Content-Type", "application/json");
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!operation.isDone && DateTime.UtcNow < deadline)
            {
            }

            if (request.result == UnityWebRequest.Result.Success)
                SymbiozRuntimeLog.Write("SHARED", $"Exit sleeper uploaded. bytes={bytes.Length}");
            else
                SymbiozRuntimeLog.Write("SHARED", $"Exit sleeper upload not confirmed: {request.error}");
        }

        private static string ResolveLocalClientId()
        {
            string profileSuffix = ResolveProfileSuffix();
            string profileKey = string.IsNullOrWhiteSpace(profileSuffix) ? "default" : profileSuffix;
            string prefsKey = "DynastyLegacySymbiozClientId_" + profileKey;
            string stored = PlayerPrefs.GetString(prefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
                return stored;

            string id = "architect-" + profileKey + "-" + Guid.NewGuid().ToString("N").Substring(0, 10);
            PlayerPrefs.SetString(prefsKey, id);
            PlayerPrefs.Save();
            return id;
        }

        private static string ResolveLocalDisplayName()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                return profile.DisplayName.Trim();

            string profileSuffix = ResolveProfileSuffix();
            if (string.Equals(profileSuffix, "client2", StringComparison.OrdinalIgnoreCase))
                return "Architect-002";

            if (!string.IsNullOrWhiteSpace(profileSuffix))
                return "Architect-" + profileSuffix;

            return "Architect-001";
        }

        private static string ResolveLocalDynastyName()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DynastyName))
                return profile.DynastyName.Trim();

            string profileSuffix = ResolveProfileSuffix();
            string profileKey = string.IsNullOrWhiteSpace(profileSuffix) ? "default" : profileSuffix;
            string prefsKey = "DynastyLegacySymbiozDynasty_" + profileKey;
            string stored = PlayerPrefs.GetString(prefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
                return stored;

            string dynasty = string.Equals(profileSuffix, "client2", StringComparison.OrdinalIgnoreCase)
                ? "Second Dawn"
                : string.IsNullOrWhiteSpace(profileSuffix)
                    ? "BlackYang"
                    : "House " + profileSuffix;
            PlayerPrefs.SetString(prefsKey, dynasty);
            PlayerPrefs.Save();
            return dynasty;
        }

        private static int ResolveLocalPlayerAge()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null && profile.Age > 0)
                return profile.Age;

            string profileSuffix = ResolveProfileSuffix();
            string profileKey = string.IsNullOrWhiteSpace(profileSuffix) ? "default" : profileSuffix;
            string prefsKey = "DynastyLegacySymbiozAge_" + profileKey;
            int stored = PlayerPrefs.GetInt(prefsKey, 0);
            if (stored > 0)
                return stored;

            int age = 19 + Mathf.Abs(profileKey.GetHashCode()) % 24;
            PlayerPrefs.SetInt(prefsKey, age);
            PlayerPrefs.Save();
            return age;
        }

        private static string GetPersistentWorldPath()
        {
            string profileSuffix = ResolveProfileSuffix();
            if (string.IsNullOrWhiteSpace(profileSuffix))
                return Path.Combine(Application.persistentDataPath, SaveFileName);

            return Path.Combine(Application.persistentDataPath, $"dynasty_legacy_symbioz_world_v1_{profileSuffix}.json");
        }

        private static string ResolveProfileSuffix()
        {
            return SanitizeFileSuffix(ClientProfileScope.Suffix);
        }

        private static string SanitizeFileSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new System.Text.StringBuilder(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    builder.Append(c);
            }

            return builder.ToString();
        }

        private void DestroyChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private Vector2Int GetCenterDoorCell()
        {
            return GetCenterDoorCell(currentLocation);
        }

        private static Vector2Int GetCenterDoorCell(LocationId location)
        {
            List<Vector2Int> doorCells = GetDoorCellsForLocation(location);
            return doorCells.Count > 0 ? doorCells[0] : GetNorthDoorCell();
        }

        private static List<Vector2Int> GetDoorCellsForLocation(LocationId location)
        {
            var cells = new List<Vector2Int>(2);
            switch (location)
            {
                case LocationId.FirstSoil:
                case LocationId.ReturnYard:
                    cells.Add(GetNorthDoorCell());
                    break;
                case LocationId.CityGate:
                    cells.Add(GetSouthDoorCell());
                    cells.Add(GetNorthDoorCell());
                    break;
                case LocationId.CityCenter:
                    cells.Add(GetSouthDoorCell());
                    break;
                default:
                    cells.Add(GetNorthDoorCell());
                    break;
            }

            return cells;
        }

        private static Vector2Int GetNorthDoorCell()
        {
            return new Vector2Int(GridSize / 2, GridSize - 1);
        }

        private static Vector2Int GetSouthDoorCell()
        {
            return new Vector2Int(GridSize / 2, 0);
        }

        private bool IsPortalTriggerCell(Vector2Int cell)
        {
            return IsPortalTriggerCell(cell, currentLocation);
        }

        private static bool IsPortalTriggerCell(Vector2Int cell, LocationId location)
        {
            List<Vector2Int> doorCells = GetDoorCellsForLocation(location);
            for (int i = 0; i < doorCells.Count; i++)
            {
                Vector2Int center = doorCells[i];
                if (Mathf.Abs(cell.y - center.y) <= 1 && Mathf.Abs(cell.x - center.x) <= 2)
                    return true;
            }

            return false;
        }

        private bool IsPortalReservedCell(Vector2Int cell)
        {
            return IsPortalReservedCell(cell, currentLocation);
        }

        private static bool IsPortalReservedCell(Vector2Int cell, LocationId location)
        {
            List<Vector2Int> doorCells = GetDoorCellsForLocation(location);
            for (int i = 0; i < doorCells.Count; i++)
            {
                Vector2Int center = doorCells[i];
                if (Mathf.Abs(cell.x - center.x) <= 2 && Mathf.Abs(cell.y - center.y) <= 1)
                    return true;
            }

            return false;
        }

        private Vector2Int GetSpawnCellForCurrentLocation()
        {
            return GetCenterDoorCell() + GetSpawnOffsetForLocation(currentLocation);
        }

        private Vector2Int GetMateriaTreeFocusCell()
        {
            Vector2Int doorCell = GetCenterDoorCell();
            Vector2Int inward = currentLocation == LocationId.FirstSoil || currentLocation == LocationId.ReturnYard
                ? Vector2Int.down
                : Vector2Int.up;
            return ClampCell(doorCell + inward * 18);
        }

        private static string GetLocationName(LocationId location)
        {
            return location switch
            {
                LocationId.FirstSoil => FirstLocationName,
                LocationId.ReturnYard => SecondLocationName,
                LocationId.CityGate => ThirdLocationName,
                LocationId.CityCenter => FourthLocationName,
                _ => location.ToString()
            };
        }

        private string GetDoorLabel()
        {
            return IsCityGateLocation(currentLocation)
                ? $"Gate to {GetOppositeLocationName(selectedCell)}"
                : $"Portal to {GetOppositeLocationName(selectedCell)}";
        }

        private string GetOppositeLocationName()
        {
            return GetOppositeLocationName(pawnCell);
        }

        private string GetOppositeLocationName(Vector2Int triggerCell)
        {
            return GetLocationName(GetTransitionTargetForCell(currentLocation, triggerCell));
        }

        private static bool IsCityGateLocation(LocationId location)
        {
            return location == LocationId.FirstSoil || location == LocationId.ReturnYard || location == LocationId.CityGate || location == LocationId.CityCenter;
        }

        private static LocationId GetNextLocation(LocationId location)
        {
            return location switch
            {
                LocationId.FirstSoil => LocationId.CityGate,
                LocationId.ReturnYard => LocationId.CityGate,
                LocationId.CityGate => LocationId.CityCenter,
                LocationId.CityCenter => LocationId.CityGate,
                _ => LocationId.FirstSoil
            };
        }

        private static LocationId GetTransitionTargetForCell(LocationId location, Vector2Int triggerCell)
        {
            if (location == LocationId.CityGate)
            {
                Vector2Int south = GetSouthDoorCell();
                Vector2Int north = GetNorthDoorCell();
                if (Mathf.Abs(triggerCell.y - south.y) <= Mathf.Abs(triggerCell.y - north.y))
                    return LocationId.FirstSoil;

                return LocationId.CityCenter;
            }

            if (location == LocationId.CityCenter)
                return LocationId.CityGate;

            return GetNextLocation(location);
        }

        private static LocationId GetPreviousLocation(LocationId location)
        {
            return location switch
            {
                LocationId.ReturnYard => LocationId.FirstSoil,
                LocationId.CityGate => LocationId.ReturnYard,
                LocationId.CityCenter => LocationId.CityGate,
                _ => LocationId.FirstSoil
            };
        }

        private static Vector2Int GetSpawnOffsetForLocation(LocationId location)
        {
            return location == LocationId.FirstSoil || location == LocationId.ReturnYard
                ? Vector2Int.down * 2
                : Vector2Int.up * 2;
        }

        private static Vector2Int GetArrivalOffsetFrom(LocationId from, LocationId to)
        {
            return GetSpawnOffsetForLocation(to);
        }

        private Vector3 ScreenToGround(Vector2 screen)
        {
            Ray ray = worldCamera.ScreenPointToRay(screen);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return Vector3.zero;
        }

        private static Quaternion IsoCameraRotation()
        {
            return Quaternion.Euler(IsoCameraPitch, IsoCameraYaw, 0f);
        }

        private static Vector3 CameraPositionForFocus(Vector3 focus)
        {
            Vector3 flatFocus = new Vector3(focus.x, 0f, focus.z);
            return flatFocus - IsoCameraRotation() * Vector3.forward * IsoCameraDistance;
        }

        private static Vector3 CameraFocusFromPosition(Vector3 cameraPosition)
        {
            Ray ray = new Ray(cameraPosition, IsoCameraRotation() * Vector3.forward);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return new Vector3(cameraPosition.x, 0f, cameraPosition.z);
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            float half = GridSize * CellSize * 0.5f;
            return new Vector3(-half + (cell.x + 0.5f) * CellSize, 0f, -half + (cell.y + 0.5f) * CellSize);
        }

        private Vector2Int WorldToCell(Vector3 world)
        {
            float half = GridSize * CellSize * 0.5f;
            int x = Mathf.FloorToInt((world.x + half) / CellSize);
            int y = Mathf.FloorToInt((world.z + half) / CellSize);
            return ClampCell(new Vector2Int(x, y));
        }

        private Vector2Int ClampCell(Vector2Int cell)
        {
            return new Vector2Int(Mathf.Clamp(cell.x, 0, GridSize - 1), Mathf.Clamp(cell.y, 0, GridSize - 1));
        }

        private Vector3 ClampPawnPosition(Vector3 position)
        {
            float half = GridSize * CellSize * 0.5f;
            position.x = Mathf.Clamp(position.x, -half + 0.5f, half - 0.5f);
            position.z = Mathf.Clamp(position.z, -half + 0.5f, half - 0.5f);
            position.y = 0.08f;
            return position;
        }

        private Vector3 ClampCamera(Vector3 position)
        {
            float half = GridSize * CellSize * 0.5f;
            Vector3 focus = CameraFocusFromPosition(position);
            focus.x = Mathf.Clamp(focus.x, -half + 8f, half - 8f);
            focus.z = Mathf.Clamp(focus.z, -half + 8f, half - 8f);
            return CameraPositionForFocus(focus);
        }

        private static void SetRendererColor(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterial = CreateColorMaterial(color);
        }

        private static Material CreateColorMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }
}

