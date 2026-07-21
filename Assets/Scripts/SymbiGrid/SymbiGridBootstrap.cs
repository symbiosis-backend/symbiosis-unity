using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame.SymbiGrid
{
    [DisallowMultipleComponent]
    public sealed class SymbiGridBootstrap : MonoBehaviour
    {
        private const string SceneName = "SymbiGrid";
        private const string MainSceneName = "Main";
        private const string DailyLastCompleteKey = "SymbiGrid.DailyLastComplete";
        private const string DailyStreakKey = "SymbiGrid.DailyStreak";
        private const string DailyBestScoreKey = "SymbiGrid.DailyBestScore";
        private const string InterstitialRunCountKey = "SymbiGrid.InterstitialRunCount";
        private const int InterstitialShowEveryRuns = 3;
        private const float InterstitialWarmupTimeoutSeconds = 5f;
        private const float ModeTransitionHoldSeconds = 1f;
        private static readonly bool SymbiMineAccessEnabled = false;
        private const string BackgroundResourcePath = "SymbiGrid/SymbiGridBackground";
        private const string MenuBackgroundResourcePath = "SymbiGrid/SymbiGridMenuBackground";
        private const string BlockAtlasResourcePath = "SymbiGrid/SymbiGridBlockAtlas";
        private const string UiAtlasResourcePath = "SymbiGrid/SymbiGridUiAtlas";
        private const string SymbiGridTitleLogoResourcePath = "SymbiGrid/SymbiGridTitleLogo";
        private const string RetroGridTitleLogoResourcePath = "SymbiGrid/RetroGridTitleLogo";
        private const string ClassicGridTitleLogoResourcePath = "SymbiGrid/ClassicGridTitleLogo";
        private const string SymbiMineTitleLogoResourcePath = "SymbiGrid/SymbiMineTitleLogo";
        private const string AudioResourceRoot = "SymbiGrid/Audio/";
        private const string SymbiMineMineResourcePath = "Orbiosis/EnemyMine_Level1";
        private const string SymbiMineExplosionSheetPath = "Orbiosis/MineExplosion_Sheet";
        private const string SymbiMineExplosionSoundPath = "Orbiosis/Audio/DroneBreak";
        private const string MusicClipResourcePath = "Orbiosis/Audio/SpaceDivineCreation";
        private const int ClassicSize = 8;
        private const int SymbiMineBeginnerRows = 9;
        private const int SymbiMineBeginnerCols = 9;
        private const int SymbiMineBeginnerMines = 10;
        private const int SymbiMineIntermediateRows = 18;
        private const int SymbiMineIntermediateCols = 9;
        private const int SymbiMineIntermediateMines = 40;
        private const int SymbiMineExpertRows = 30;
        private const int SymbiMineExpertCols = 16;
        private const int SymbiMineExpertMines = 99;
        private const int TetrisRows = 20;
        private const int TetrisCols = 10;
        private const int MaxRows = SymbiMineExpertRows;
        private const int MaxCols = SymbiMineExpertCols;
        private const float TetrisFrameRate = 60f;
        private const float TetrisHorizontalDragCellPixels = 54f;
        private const float TetrisHorizontalMoveInterval = 0.085f;
        private const float TetrisTapRadius = 42f;
        private const float TetrisSwipeThreshold = 48f;
        private const float TetrisSoftDropInterval = 0.065f;
        private const int PieceSlots = 3;
        private const int MiniGridSize = 5;
        private const float PlacementMagnetRadiusInCells = 0.86f;
        private static readonly Vector2 ModeLogoButtonSize = new Vector2(780f, 152f);
        private static readonly Vector2 DragVisualOffset = new Vector2(52f, 72f);
        private static readonly int[] TetrisGravityFrames =
        {
            48, 43, 38, 33, 28, 23, 18, 13, 8, 6,
            5, 5, 5,
            4, 4, 4,
            3, 3, 3,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            1
        };

        private static readonly Color Background = new Color(0.018f, 0.024f, 0.042f, 1f);
        private static readonly Color BoardPanel = new Color(0.035f, 0.052f, 0.084f, 0.96f);
        private static readonly Color CellEmpty = new Color(0.092f, 0.122f, 0.172f, 1f);
        private static readonly Color CellEmptyAlt = new Color(0.075f, 0.101f, 0.145f, 1f);
        private static readonly Color CellPreview = new Color(0.28f, 0.86f, 0.94f, 0.82f);
        private static readonly Color CellInvalid = new Color(0.88f, 0.18f, 0.26f, 0.74f);
        private static readonly Color Gold = new Color(1f, 0.79f, 0.28f, 1f);
        private static readonly Color Ink = new Color(0.92f, 0.96f, 1f, 1f);
        private static readonly Color MutedInk = new Color(0.62f, 0.72f, 0.84f, 1f);
        private static readonly Vector2Int[] SymbiMinePreviewMines =
        {
            new Vector2Int(0, 6),
            new Vector2Int(1, 1),
            new Vector2Int(2, 6),
            new Vector2Int(4, 7),
            new Vector2Int(6, 5),
            new Vector2Int(7, 2)
        };

        private readonly bool[,] board = new bool[MaxRows, MaxCols];
        private readonly Color[,] boardColors = new Color[MaxRows, MaxCols];
        private readonly bool[,] mineCells = new bool[MaxRows, MaxCols];
        private readonly bool[,] revealedMineCells = new bool[MaxRows, MaxCols];
        private readonly bool[,] flaggedMineCells = new bool[MaxRows, MaxCols];
        private readonly int[,] adjacentMineCounts = new int[MaxRows, MaxCols];
        private readonly Image[,] cellImages = new Image[MaxRows, MaxCols];
        private readonly Image[,] blockImages = new Image[MaxRows, MaxCols];
        private readonly TextMeshProUGUI[,] cellTexts = new TextMeshProUGUI[MaxRows, MaxCols];
        private readonly List<PieceView> pieces = new List<PieceView>(PieceSlots);
        private System.Random runRandom = new System.Random();
        private static Sprite backgroundSprite;
        private static Sprite menuBackgroundSprite;
        private static Sprite symbiGridTitleLogoSprite;
        private static Sprite retroGridTitleLogoSprite;
        private static Sprite classicGridTitleLogoSprite;
        private static Sprite symbiMineTitleLogoSprite;
        private static Sprite[] blockSprites;
        private static bool blockSpritesAreFallback;
        private static Sprite boardPanelSprite;
        private static Sprite cellSprite;
        private static Sprite cellAltSprite;
        private static Sprite symbiMineMineSprite;
        private static Sprite[] symbiMineExplosionSprites;
        private static readonly Dictionary<UiSprite, Sprite> uiSprites = new Dictionary<UiSprite, Sprite>();

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioClip musicClip;
        private AudioClip buttonClip;
        private AudioClip selectClip;
        private AudioClip placeClip;
        private AudioClip invalidClip;
        private AudioClip lineClearClip;
        private AudioClip comboClip;
        private AudioClip newPieceClip;
        private AudioClip openClip;
        private AudioClip closeClip;
        private AudioClip gameOverClip;
        private AudioClip completeClip;
        private AudioClip symbiMineExplosionClip;
        private Canvas canvas;
        private RectTransform boardFrame;
        private RectTransform boardRoot;
        private RectTransform trayRoot;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI scoreLabelText;
        private TextMeshProUGUI linesText;
        private TextMeshProUGUI comboText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI modeText;
        private Image topBarImage;
        private Image retroGridTitleLogoImage;
        private Image classicGridTitleLogoImage;
        private Image symbiMineTitleLogoImage;
        private TextMeshProUGUI goalText;
        private TextMeshProUGUI movesText;
        private Image goalProgressFill;
        private Image comboEnergyFill;
        private RectTransform piecePanel;
        private RectTransform tetrisControls;
        private RectTransform tetrisJoystickRoot;
        private RectTransform tetrisGestureLayer;
        private TextMeshProUGUI tetrisControlHint;
        private TextMeshProUGUI tetrisControlModeLabel;
        private Button tetrisRotateButton;
        private Button tetrisDropButton;
        private GameObject symbiMineFlagButtonRoot;
        private TextMeshProUGUI symbiMineFlagModeLabel;
        private Button settingsMenuButton;
        private Button rerollAdButton;
        private GameObject gameOverOverlay;
        private GameObject symbiMineResultTapLayer;
        private GameObject settingsOverlay;
        private TextMeshProUGUI settingsTitleText;
        private Button settingsModeButton;
        private Button settingsRestartButton;
        private Button settingsBackButton;
        private Button settingsCloseButton;
        private GameObject symbiMineDifficultyOverlay;
        private TextMeshProUGUI symbiMineDifficultyTitleText;
        private TextMeshProUGUI symbiMineDifficultySubtitleText;
        private Button symbiMineBeginnerButton;
        private Button symbiMineIntermediateButton;
        private Button symbiMineExpertButton;
        private Button symbiMineDifficultyBackButton;
        private RectTransform outcomeWindow;
        private RectTransform outcomeHeaderPanel;
        private RectTransform outcomeScorePanel;
        private RectTransform outcomeMetricsPanel;
        private TextMeshProUGUI outcomeTitleText;
        private TextMeshProUGUI outcomeBodyText;
        private TextMeshProUGUI outcomeScoreText;
        private TextMeshProUGUI outcomeBestText;
        private TextMeshProUGUI outcomeLastText;
        private TextMeshProUGUI outcomeLinesText;
        private TextMeshProUGUI outcomeActionLabel;
        private GameObject secondChanceButtonRoot;
        private GameObject outcomeMenuButtonRoot;
        private GameObject modeOverlay;
        private RectTransform symbiGridMenuLogo;
        private RectTransform modeGroup;
        private Button retroGridModeButton;
        private Button classicGridModeButton;
        private Button symbiMineModeButton;
        private TextMeshProUGUI symbiMineUnavailableBadgeText;
        private Button modePlatformBackButton;
        private TextMeshProUGUI modeSubtitle;
        private RectTransform modePreviewWindow;
        private Button modePreviewBackButton;
        private Button modePreviewStartButton;
        private Button modePreviewControlsButton;
        private RectTransform modePreviewDetailsPanel;
        private TextMeshProUGUI modePreviewDescriptionText;
        private TextMeshProUGUI modePreviewBestText;
        private TextMeshProUGUI modePreviewLastText;
        private GameObject modeControlsOverlay;
        private Button modeControlsCloseButton;
        private TextMeshProUGUI modeControlsTitleText;
        private TextMeshProUGUI modeControlsBodyText;
        private GameObject symbiMineUnavailableOverlay;
        private TextMeshProUGUI symbiMineUnavailableStatusText;
        private TextMeshProUGUI symbiMineUnavailableBodyText;
        private Button symbiMineUnavailableCloseButton;
        private RectTransform retroGridPreviewBoard;
        private readonly List<Image> retroGridPreviewCells = new List<Image>(112);
        private readonly List<TextMeshProUGUI> retroGridPreviewLabels = new List<TextMeshProUGUI>(112);
        private Coroutine modeSelectionRoutine;
        private Coroutine retroGridPreviewRoutine;
        private SymbiGridMode selectedModePreview;
        private bool modePreviewOpen;
        private bool modePreviewAnimating;
        private RectTransform dragGhost;
        private Image[] dragGhostCells;
        private int score;
        private int bestScore;
        private int lastScore;
        private int combo;
        private int linesCleared;
        private int targetScore;
        private int targetLines;
        private int movesLeft;
        private int currentSymbiMineLevel = 1;
        private int symbiMineTotalMines;
        private int symbiMineRows = SymbiMineBeginnerRows;
        private int symbiMineCols = SymbiMineBeginnerCols;
        private int symbiMineFlags;
        private int symbiMineRevealedSafe;
        private bool symbiMineGenerated;
        private bool symbiMineExploded;
        private bool symbiMineFlagMode;
        private int symbiMineDetonatedRow = -1;
        private int symbiMineDetonatedCol = -1;
        private bool symbiMineOutcomePending;
        private bool symbiMineSecondChanceOfferActive;
        private string pendingOutcomeTitle;
        private string pendingOutcomeBody;
        private string pendingOutcomeAction;
        private int dailyStreak;
        private int dailyBestScore;
        private int selectedPieceIndex = -1;
        private bool resolving;
        private bool gameOver;
        private bool exitingScene;
        private bool modeTransitionRunning;
        private bool draggingPiece;
        private bool levelComplete;
        private bool hasMoveLimit;
        private bool secondChanceUsed;
        private bool piecesSpawnPending;
        private bool rerollRewardedAdRequestInProgress;
        private bool symbiMineRewardedAdRequestInProgress;
        private Shape tetrisActiveShape;
        private int tetrisActiveRow;
        private int tetrisActiveCol;
        private int tetrisLevel;
        private float tetrisFallTimer;
        private float tetrisFallInterval = 1.25f;
        private float tetrisLastHorizontalMoveTime = -10f;
        private bool tetrisHasActivePiece;
        private bool tetrisSoftDropActive;
        private float tetrisLastTapTime = -10f;
        private TetrisControlMode tetrisControlMode = TetrisControlMode.Gesture;
        private SymbiGridMode currentMode = SymbiGridMode.Classic;

        private int ActiveRows => currentMode == SymbiGridMode.Tetris ? TetrisRows : currentMode == SymbiGridMode.SymbiMine ? symbiMineRows : ClassicSize;
        private int ActiveCols => currentMode == SymbiGridMode.Tetris ? TetrisCols : currentMode == SymbiGridMode.SymbiMine ? symbiMineCols : ClassicSize;

        private enum SymbiGridMode
        {
            Classic,
            SymbiMine,
            Tetris
        }

        private enum TetrisControlMode
        {
            Gesture,
            Joystick
        }

        private enum SymbiMineDifficulty
        {
            Beginner,
            Intermediate,
            Expert
        }

        private enum UiSprite
        {
            TopHud,
            StatGold,
            StatCyan,
            StatGreen,
            GoalBar,
            BoardFrame,
            TrayPanel,
            PieceCard,
            Button,
            Modal,
            BarTrack,
            BarFillCyan,
            BarFillGold
        }

        private struct Shape
        {
            public string Name;
            public Vector2Int[] Cells;
            public Color Color;

            public Shape(string name, Color color, params Vector2Int[] cells)
            {
                Name = name;
                Color = color;
                Cells = cells;
            }
        }

        private sealed class PieceView
        {
            public Button Button;
            public RectTransform Root;
            public RectTransform Grid;
            public Image[] MiniCells;
            public CanvasGroup Group;
            public Shape Shape;
            public bool Used;
        }

        private sealed class PieceDragInput : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
        {
            public SymbiGridBootstrap Owner;
            public int Index;

            public void OnPointerDown(PointerEventData eventData)
            {
                Owner?.ShowPiecePlacements(Index);
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                Owner?.BeginPieceDrag(Index, eventData.position, eventData.pressEventCamera);
            }

            public void OnDrag(PointerEventData eventData)
            {
                Owner?.DragPiece(eventData.position, eventData.pressEventCamera);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                Owner?.EndPieceDrag(eventData.position, eventData.pressEventCamera);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                Owner?.CancelPiecePress(Index);
            }
        }

        private sealed class CellInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            private const float FlagHoldSeconds = 0.42f;

            public SymbiGridBootstrap Owner;
            public int Row;
            public int Col;
            private float pointerDownTime;
            private bool pointerDownValid;
            private PointerEventData.InputButton pointerButton;

            public void OnPointerEnter(PointerEventData eventData)
            {
                Owner?.Preview(Row, Col);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                Owner?.RefreshPlacementHints();
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                pointerDownTime = Time.unscaledTime;
                pointerButton = eventData.button;
                pointerDownValid = true;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (!pointerDownValid)
                    return;

                pointerDownValid = false;
                bool flag = pointerButton == PointerEventData.InputButton.Right
                    || Time.unscaledTime - pointerDownTime >= FlagHoldSeconds;
                Owner?.HandleCellTap(Row, Col, flag);
            }
        }

        private static readonly Shape[] ShapeLibrary =
        {
            new Shape("Dot", new Color(0.42f, 0.88f, 1f, 1f), new Vector2Int(0, 0)),
            new Shape("Pair", new Color(0.46f, 0.74f, 1f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new Shape("Stack", new Color(0.40f, 0.68f, 1f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1)),
            new Shape("Line 3", new Color(0.44f, 0.92f, 0.62f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)),
            new Shape("Column 3", new Color(0.32f, 0.82f, 0.64f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)),
            new Shape("Line 4", new Color(0.78f, 0.88f, 0.35f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)),
            new Shape("Column 4", new Color(0.66f, 0.84f, 0.36f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)),
            new Shape("Square", new Color(1f, 0.71f, 0.34f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new Shape("L", new Color(1f, 0.55f, 0.42f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new Shape("Hook", new Color(1f, 0.48f, 0.52f, 1f), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1)),
            new Shape("T", new Color(0.86f, 0.58f, 1f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)),
            new Shape("Plus", new Color(0.76f, 0.56f, 1f, 1f), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2)),
            new Shape("S", new Color(0.38f, 0.96f, 0.76f, 1f), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new Shape("Z", new Color(0.36f, 0.78f, 0.92f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)),
            new Shape("Big L", new Color(1f, 0.64f, 0.30f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)),
            new Shape("Corner", new Color(0.98f, 0.44f, 0.64f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)),
            new Shape("Block 3x3", new Color(0.95f, 0.78f, 0.38f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)),
        };

        private static readonly Shape[] TetrisShapeLibrary =
        {
            new Shape("I", new Color(0.42f, 0.88f, 1f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)),
            new Shape("O", new Color(1f, 0.71f, 0.34f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new Shape("T", new Color(0.86f, 0.58f, 1f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)),
            new Shape("L", new Color(1f, 0.55f, 0.42f, 1f), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2)),
            new Shape("J", new Color(0.46f, 0.74f, 1f, 1f), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 2)),
            new Shape("S", new Color(0.38f, 0.96f, 0.76f, 1f), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new Shape("Z", new Color(1f, 0.48f, 0.52f, 1f), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
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
            if (!string.Equals(scene.name, SceneName, StringComparison.Ordinal))
                return;

            if (FindAnyObjectByType<SymbiGridBootstrap>() != null)
                return;

            GameObject root = new GameObject("SymbiGridRoot");
            root.AddComponent<SymbiGridBootstrap>();
        }

        private void Awake()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            AppSettings.OnMusicChanged += OnMusicChanged;
            EnsureSupportedOrientation();
            EnsureCamera();
            EnsureEventSystem();
            SetupAudio();
            BuildInterface();
            RefreshLocalizedText();
            NewGame();
            if (modeOverlay != null)
                modeOverlay.SetActive(true);
        }

        private void OnDestroy()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            AppSettings.OnMusicChanged -= OnMusicChanged;
            StopRuntimeForExit();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalizedText();
        }

        private void OnMusicChanged(bool enabled)
        {
            UpdateMusicPlayback();
        }

        private void StopRuntimeForExit()
        {
            exitingScene = true;
            StopAllCoroutines();
            resolving = false;
            draggingPiece = false;
            gameOver = true;
            tetrisHasActivePiece = false;
            tetrisSoftDropActive = false;
            symbiMineRewardedAdRequestInProgress = false;
            HideDragGhost();

            if (musicSource != null && musicSource.isPlaying)
                musicSource.Stop();
        }

        private void Update()
        {
            if (exitingScene)
                return;

            EnsureSupportedOrientation();

            if (currentMode == SymbiGridMode.Tetris)
                UpdateTetrisMode();
        }

        private static void EnsureSupportedOrientation()
        {
#if UNITY_IOS
            MahjongGame.SceneOrientationPolicy.ApplyLandscapeOnly();
#else
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
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
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventObject.GetComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void SetupAudio()
        {
            musicClip = Resources.Load<AudioClip>(MusicClipResourcePath);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.34f;
            musicSource.clip = musicClip;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.78f;

            buttonClip = LoadSound("sg_button");
            selectClip = LoadSound("sg_select");
            placeClip = LoadSound("sg_place");
            invalidClip = LoadSound("sg_invalid");
            lineClearClip = LoadSound("sg_line_clear");
            comboClip = LoadSound("sg_combo");
            newPieceClip = LoadSound("sg_new_piece");
            openClip = LoadSound("sg_open");
            closeClip = LoadSound("sg_close");
            gameOverClip = LoadSound("sg_game_over");
            completeClip = LoadSound("sg_complete");
            symbiMineExplosionClip = Resources.Load<AudioClip>(SymbiMineExplosionSoundPath);
            UpdateMusicPlayback();
        }

        private void UpdateMusicPlayback()
        {
            if (musicSource == null)
                return;

            if (musicClip == null)
                musicClip = Resources.Load<AudioClip>(MusicClipResourcePath);
            if (musicSource.clip == null)
                musicSource.clip = musicClip;

            bool musicEnabled = AppSettings.I == null || AppSettings.I.MusicEnabled;
            if (musicEnabled && musicSource.clip != null)
            {
                if (!musicSource.isPlaying)
                    musicSource.Play();
            }
            else if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        private static AudioClip LoadSound(string name)
        {
            return Resources.Load<AudioClip>(AudioResourceRoot + name);
        }

        private void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (sfxSource == null || clip == null)
                return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }

        private static Vector2 GetReferenceResolution()
        {
#if UNITY_IOS
            return new Vector2(1600f, 900f);
#else
            return new Vector2(900f, 1600f);
#endif
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GetReferenceResolution();
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            Stretch(root);

            Image background = CreateImage(root, "Background", Color.white);
            Stretch(background.rectTransform);
            Sprite bgSprite = GetMenuBackgroundSprite();
            if (bgSprite != null)
            {
                background.sprite = bgSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }
            else
            {
                background.color = Background;
            }

            RectTransform top = CreatePanel(root, "TopBar", new Color(0.014f, 0.022f, 0.038f, 0.94f));
            ApplyUiSprite(top, UiSprite.TopHud, 58f);
            topBarImage = top.GetComponent<Image>();
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0f, 150f);

            modeText = CreateText(top, "Title", "SYMBIGRID", 44f, FontStyles.Bold, Ink);
            modeText.rectTransform.anchorMin = new Vector2(0.20f, 0.12f);
            modeText.rectTransform.anchorMax = new Vector2(0.80f, 0.96f);
            modeText.rectTransform.offsetMin = Vector2.zero;
            modeText.rectTransform.offsetMax = Vector2.zero;

            retroGridTitleLogoImage = CreateTopTitleLogo(top, "RetroGridTitleLogo", GetRetroGridTitleLogoSprite());
            classicGridTitleLogoImage = CreateTopTitleLogo(top, "ClassicGridTitleLogo", GetClassicGridTitleLogoSprite());
            symbiMineTitleLogoImage = CreateImage(top, "SymbiMineTitleLogo", Color.white);
            symbiMineTitleLogoImage.sprite = GetSymbiMineTitleLogoSprite();
            symbiMineTitleLogoImage.preserveAspect = true;
            symbiMineTitleLogoImage.raycastTarget = false;
            symbiMineTitleLogoImage.rectTransform.anchorMin = new Vector2(0.14f, 0.08f);
            symbiMineTitleLogoImage.rectTransform.anchorMax = new Vector2(0.86f, 0.98f);
            symbiMineTitleLogoImage.rectTransform.offsetMin = Vector2.zero;
            symbiMineTitleLogoImage.rectTransform.offsetMax = Vector2.zero;
            symbiMineTitleLogoImage.gameObject.SetActive(false);

            scoreText = CreateScoreReadout(root);
            settingsMenuButton = CreateButton(top, "SettingsButton", GameLocalization.Text("symbigrid.menu"), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -40f), new Vector2(126f, 58f), 22f, OpenSettingsOverlay);
            Button flagModeButton = CreateButton(top, "SymbiMineFlagMode", GameLocalization.Text("symbigrid.flag"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -40f), new Vector2(126f, 58f), 22f, ToggleSymbiMineFlagMode);
            symbiMineFlagButtonRoot = flagModeButton.gameObject;
            symbiMineFlagModeLabel = flagModeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            symbiMineFlagButtonRoot.SetActive(false);

            boardFrame = CreatePanel(root, "BoardFrame", new Color(0.006f, 0.012f, 0.024f, 0.86f));
            ApplyUiSprite(boardFrame, UiSprite.BoardFrame, 76f);
            boardFrame.anchorMin = new Vector2(0.045f, 0.360f);
            boardFrame.anchorMax = new Vector2(0.955f, 0.790f);
            boardFrame.offsetMin = Vector2.zero;
            boardFrame.offsetMax = Vector2.zero;
            RectTransform boardGlow = CreatePanel(boardFrame, "BoardGlow", new Color(0.06f, 0.28f, 0.25f, 0.18f));
            Stretch(boardGlow);
            boardGlow.offsetMin = new Vector2(8f, 8f);
            boardGlow.offsetMax = new Vector2(-8f, -8f);

            boardRoot = CreatePanel(root, "Board", BoardPanel);
            Image boardImage = boardRoot.GetComponent<Image>();
            boardImage.sprite = GetBoardPanelSprite();
            boardImage.type = Image.Type.Sliced;
            boardImage.color = Color.white;
            boardRoot.anchorMin = new Vector2(0.075f, 0.382f);
            boardRoot.anchorMax = new Vector2(0.925f, 0.763f);
            boardRoot.offsetMin = Vector2.zero;
            boardRoot.offsetMax = Vector2.zero;

            GridLayoutGroup grid = boardRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ActiveCols;
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.MiddleCenter;
            boardRoot.gameObject.AddComponent<BoardCellSizer>();

            for (int row = 0; row < MaxRows; row++)
            {
                for (int col = 0; col < MaxCols; col++)
                {
                    Button cell = CreateCell(row, col);
                    cell.transform.SetParent(boardRoot, false);
                    cellImages[row, col] = cell.image;
                }
            }

            tetrisGestureLayer = BuildTetrisGestureLayer(root);
            tetrisGestureLayer.gameObject.SetActive(false);

            piecePanel = CreatePanel(root, "PiecePanel", new Color(0.006f, 0.014f, 0.024f, 0.90f));
            ApplyUiSprite(piecePanel, UiSprite.TrayPanel, 54f);
            piecePanel.anchorMin = new Vector2(0.055f, 0.045f);
            piecePanel.anchorMax = new Vector2(0.945f, 0.355f);
            piecePanel.offsetMin = Vector2.zero;
            piecePanel.offsetMax = Vector2.zero;

            RectTransform energyTrack = CreatePanel(piecePanel, "ComboEnergyTrack", new Color(0.08f, 0.12f, 0.15f, 0.95f));
            ApplyUiSprite(energyTrack, UiSprite.BarTrack, 22f);
            energyTrack.anchorMin = new Vector2(0.07f, 0.700f);
            energyTrack.anchorMax = new Vector2(0.93f, 0.740f);
            energyTrack.offsetMin = Vector2.zero;
            energyTrack.offsetMax = Vector2.zero;

            comboEnergyFill = CreateImage(energyTrack, "ComboEnergyFill", new Color(1f, 0.78f, 0.18f, 0.95f));
            ApplyUiSprite(comboEnergyFill, UiSprite.BarFillGold, 20f);
            comboEnergyFill.rectTransform.anchorMin = Vector2.zero;
            comboEnergyFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            comboEnergyFill.rectTransform.offsetMin = Vector2.zero;
            comboEnergyFill.rectTransform.offsetMax = Vector2.zero;

            rerollAdButton = CreateButton(piecePanel, "RerollRewardedAd", GameLocalization.Text("symbigrid.reroll_ad"), new Vector2(0.20f, 0.765f), new Vector2(0.80f, 0.940f), Vector2.zero, Vector2.zero, 27f, HandleRerollRewardedAd);

            RectTransform pieceShelf = CreatePanel(piecePanel, "PieceShelf", new Color(0.005f, 0.018f, 0.024f, 0.72f));
            ApplyUiSprite(pieceShelf, UiSprite.Button, 46f);
            pieceShelf.anchorMin = new Vector2(0.055f, 0.085f);
            pieceShelf.anchorMax = new Vector2(0.945f, 0.680f);
            pieceShelf.offsetMin = Vector2.zero;
            pieceShelf.offsetMax = Vector2.zero;

            trayRoot = new GameObject("PieceTray", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            trayRoot.SetParent(piecePanel, false);
            trayRoot.anchorMin = new Vector2(0.055f, 0.125f);
            trayRoot.anchorMax = new Vector2(0.945f, 0.645f);
            trayRoot.offsetMin = new Vector2(20f, 0f);
            trayRoot.offsetMax = new Vector2(-20f, 0f);
            HorizontalLayoutGroup trayLayout = trayRoot.GetComponent<HorizontalLayoutGroup>();
            trayLayout.spacing = 24f;
            trayLayout.padding = new RectOffset(0, 0, 0, 0);
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = true;

            for (int i = 0; i < PieceSlots; i++)
                pieces.Add(CreatePieceView(i));

            tetrisControls = BuildTetrisControls(root);
            tetrisControls.gameObject.SetActive(false);
            BuildDragGhost(root);
            gameOverOverlay = BuildGameOverOverlay(root);
            gameOverOverlay.SetActive(false);
            symbiMineResultTapLayer = BuildSymbiMineResultTapLayer(root);
            symbiMineResultTapLayer.SetActive(false);
            settingsOverlay = BuildSettingsOverlay(root);
            settingsOverlay.SetActive(false);
            symbiMineDifficultyOverlay = BuildSymbiMineDifficultyOverlay(root);
            symbiMineDifficultyOverlay.SetActive(false);
            modeOverlay = BuildModeOverlay(root);
            modeOverlay.SetActive(true);
            symbiMineUnavailableOverlay = BuildSymbiMineUnavailableOverlay(root);
            symbiMineUnavailableOverlay.SetActive(false);
        }

        private Button CreateCell(int row, int col)
        {
            GameObject go = new GameObject("Cell_" + row + "_" + col, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CellInput));
            Image image = go.GetComponent<Image>();
            image.sprite = GetCellSprite((row + col) % 2 != 0);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;

            Image block = CreateImage(go.transform, "Block", new Color(1f, 1f, 1f, 0f));
            block.raycastTarget = false;
            RectTransform blockRect = block.rectTransform;
            blockRect.anchorMin = new Vector2(0.5f, 0.5f);
            blockRect.anchorMax = new Vector2(0.5f, 0.5f);
            blockRect.pivot = new Vector2(0.5f, 0.5f);
            blockRect.anchoredPosition = Vector2.zero;
            blockRect.sizeDelta = new Vector2(28f, 28f);
            block.preserveAspect = false;
            go.AddComponent<CellBlockFitter>().Block = blockRect;
            blockImages[row, col] = block;

            TextMeshProUGUI label = CreateText(go.transform, "Label", "", 28f, FontStyles.Bold, Ink);
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 32f;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(2f, 0f);
            label.rectTransform.offsetMax = new Vector2(-2f, 0f);
            cellTexts[row, col] = label;

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            CellInput input = go.GetComponent<CellInput>();
            input.Owner = this;
            input.Row = row;
            input.Col = col;
            return button;
        }

        private PieceView CreatePieceView(int index)
        {
            Button button = CreateButton(trayRoot, "Piece_" + index, "", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 1f, () => SelectPiece(index));
            button.onClick.RemoveAllListeners();
            PieceDragInput dragInput = button.gameObject.AddComponent<PieceDragInput>();
            dragInput.Owner = this;
            dragInput.Index = index;
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 170f;
            layout.preferredWidth = 198f;
            layout.preferredHeight = 190f;
            CanvasGroup group = button.gameObject.AddComponent<CanvasGroup>();

            RectTransform root = button.transform as RectTransform;
            Image image = button.image;
            image.color = new Color(0.04f, 0.07f, 0.11f, 0.98f);
            ApplyUiSprite(image, UiSprite.PieceCard, 48f);

            RectTransform grid = new GameObject("MiniGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            grid.SetParent(root, false);
            grid.anchorMin = new Vector2(0.5f, 0.52f);
            grid.anchorMax = new Vector2(0.5f, 0.52f);
            grid.pivot = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2(144f, 144f);
            GridLayoutGroup miniGrid = grid.GetComponent<GridLayoutGroup>();
            miniGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            miniGrid.constraintCount = MiniGridSize;
            miniGrid.cellSize = new Vector2(27f, 27f);
            miniGrid.spacing = new Vector2(4f, 4f);
            miniGrid.childAlignment = TextAnchor.MiddleCenter;

            Image[] miniCells = new Image[MiniGridSize * MiniGridSize];
            for (int i = 0; i < miniCells.Length; i++)
            {
                Image mini = CreateImage(grid, "MiniCell_" + i, new Color(1f, 1f, 1f, 0f));
                mini.raycastTarget = false;
                mini.preserveAspect = false;
                miniCells[i] = mini;
            }

            return new PieceView
            {
                Button = button,
                Root = root,
                Grid = grid,
                MiniCells = miniCells,
                Group = group
            };
        }

        private RectTransform BuildTetrisControls(RectTransform parent)
        {
            RectTransform controls = CreatePanel(parent, "TetrisControls", new Color(0.006f, 0.014f, 0.024f, 0.92f));
            ApplyUiSprite(controls, UiSprite.TrayPanel, 54f);
            controls.anchorMin = new Vector2(0.055f, 0.045f);
            controls.anchorMax = new Vector2(0.945f, 0.355f);
            controls.offsetMin = Vector2.zero;
            controls.offsetMax = Vector2.zero;

            tetrisControlHint = CreateText(controls, "Hint", "", 26f, FontStyles.Bold, Gold);
            tetrisControlHint.textWrappingMode = TextWrappingModes.Normal;
            tetrisControlHint.rectTransform.anchorMin = new Vector2(0.06f, 0.62f);
            tetrisControlHint.rectTransform.anchorMax = new Vector2(0.94f, 0.92f);
            tetrisControlHint.rectTransform.offsetMin = Vector2.zero;
            tetrisControlHint.rectTransform.offsetMax = Vector2.zero;

            tetrisJoystickRoot = CreatePanel(controls, "JoystickButtons", new Color(0f, 0f, 0f, 0f));
            tetrisJoystickRoot.anchorMin = new Vector2(0.04f, 0.06f);
            tetrisJoystickRoot.anchorMax = new Vector2(0.96f, 0.56f);
            tetrisJoystickRoot.offsetMin = Vector2.zero;
            tetrisJoystickRoot.offsetMax = Vector2.zero;

            CreateButton(tetrisJoystickRoot, "Left", "<", new Vector2(0.00f, 0.05f), new Vector2(0.22f, 0.95f), Vector2.zero, Vector2.zero, 42f, MoveTetrisLeft);
            tetrisRotateButton = CreateButton(tetrisJoystickRoot, "Rotate", GameLocalization.Text("symbigrid.rotate"), new Vector2(0.25f, 0.05f), new Vector2(0.53f, 0.95f), Vector2.zero, Vector2.zero, 24f, RotateTetrisButton);
            tetrisDropButton = CreateButton(tetrisJoystickRoot, "Drop", GameLocalization.Text("symbigrid.drop"), new Vector2(0.56f, 0.05f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero, 24f, DropTetrisButton);
            CreateButton(tetrisJoystickRoot, "Right", ">", new Vector2(0.81f, 0.05f), new Vector2(1.00f, 0.95f), Vector2.zero, Vector2.zero, 42f, MoveTetrisRight);
            return controls;
        }

        private RectTransform BuildTetrisGestureLayer(RectTransform parent)
        {
            RectTransform layer = new GameObject("TetrisGestureLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TetrisGestureInput)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = new Vector2(0.045f, 0.360f);
            layer.anchorMax = new Vector2(0.955f, 0.790f);
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;

            Image image = layer.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            TetrisGestureInput input = layer.GetComponent<TetrisGestureInput>();
            input.Owner = this;
            return layer;
        }

        private void MoveTetrisLeft()
        {
            StepTetrisHorizontal(-1);
        }

        private void ToggleTetrisControlMode()
        {
            tetrisControlMode = TetrisControlMode.Gesture;
            tetrisLastTapTime = -10f;
            RefreshTetrisControlModeUi();
            SetStatus(BuildModeStatusText());
        }

        private void RefreshTetrisControlModeUi()
        {
            bool isTetris = currentMode == SymbiGridMode.Tetris;
            tetrisControlMode = TetrisControlMode.Gesture;

            if (tetrisControls != null)
                tetrisControls.gameObject.SetActive(false);
            if (tetrisGestureLayer != null)
                tetrisGestureLayer.gameObject.SetActive(isTetris);
            if (tetrisJoystickRoot != null)
                tetrisJoystickRoot.gameObject.SetActive(false);
            if (tetrisControlModeLabel != null)
                tetrisControlModeLabel.text = GameLocalization.Text("symbigrid.retro_control_gesture");
            if (tetrisControlHint != null)
                tetrisControlHint.text = GameLocalization.Text("symbigrid.retro_gesture_hint");
        }

        private void ToggleSymbiMineFlagMode()
        {
            if (currentMode != SymbiGridMode.SymbiMine || gameOver || resolving)
                return;

            symbiMineFlagMode = !symbiMineFlagMode;
            PlaySound(selectClip, 0.45f, symbiMineFlagMode ? 1.08f : 0.96f);
            RefreshSymbiMineFlagModeUi();
            SetStatus(symbiMineFlagMode ? "Flag mode: tap cells to mark mines." : "Open mode: tap cells to reveal.");
        }

        private void RefreshSymbiMineFlagModeUi()
        {
            bool show = currentMode == SymbiGridMode.SymbiMine;
            if (symbiMineFlagButtonRoot != null)
                symbiMineFlagButtonRoot.SetActive(show);
            if (symbiMineFlagModeLabel != null)
                symbiMineFlagModeLabel.text = symbiMineFlagMode ? GameLocalization.Text("symbigrid.flag_on") : GameLocalization.Text("symbigrid.flag");
        }

        private void ApplyBoardLayout()
        {
            bool isTetris = currentMode == SymbiGridMode.Tetris;
            bool isSymbiMine = currentMode == SymbiGridMode.SymbiMine;

            if (boardFrame != null)
            {
                boardFrame.anchorMin = isTetris ? new Vector2(0.055f, 0.025f) : isSymbiMine ? new Vector2(0.025f, 0.030f) : new Vector2(0.045f, 0.360f);
                boardFrame.anchorMax = isTetris ? new Vector2(0.945f, 0.805f) : isSymbiMine ? new Vector2(0.975f, 0.790f) : new Vector2(0.955f, 0.790f);
                boardFrame.offsetMin = Vector2.zero;
                boardFrame.offsetMax = Vector2.zero;
            }

            if (boardRoot != null)
            {
                boardRoot.anchorMin = isTetris ? new Vector2(0.085f, 0.050f) : isSymbiMine ? new Vector2(0.055f, 0.055f) : new Vector2(0.075f, 0.382f);
                boardRoot.anchorMax = isTetris ? new Vector2(0.915f, 0.780f) : isSymbiMine ? new Vector2(0.945f, 0.765f) : new Vector2(0.925f, 0.763f);
                boardRoot.offsetMin = Vector2.zero;
                boardRoot.offsetMax = Vector2.zero;

                GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.constraintCount = ActiveCols;
                    grid.spacing = isTetris ? new Vector2(3f, 3f) : isSymbiMine ? new Vector2(3f, 3f) : new Vector2(8f, 8f);
                    grid.padding = isTetris ? new RectOffset(10, 10, 10, 10) : isSymbiMine ? new RectOffset(8, 8, 8, 8) : new RectOffset(20, 20, 20, 20);
                }

                BoardCellSizer sizer = boardRoot.GetComponent<BoardCellSizer>();
                if (sizer != null)
                    sizer.Configure(ActiveRows, ActiveCols);
            }

            if (tetrisGestureLayer != null)
            {
                tetrisGestureLayer.anchorMin = isTetris ? new Vector2(0.085f, 0.050f) : new Vector2(0.045f, 0.360f);
                tetrisGestureLayer.anchorMax = isTetris ? new Vector2(0.915f, 0.780f) : new Vector2(0.955f, 0.790f);
                tetrisGestureLayer.offsetMin = Vector2.zero;
                tetrisGestureLayer.offsetMax = Vector2.zero;
            }

            RectTransform scoreRoot = scoreText != null ? scoreText.transform.parent as RectTransform : null;
            if (scoreRoot != null)
            {
                scoreRoot.anchorMin = isTetris ? new Vector2(0.165f, 0.812f) : isSymbiMine ? new Vector2(0.145f, 0.805f) : new Vector2(0.125f, 0.795f);
                scoreRoot.anchorMax = isTetris ? new Vector2(0.835f, 0.900f) : isSymbiMine ? new Vector2(0.855f, 0.905f) : new Vector2(0.875f, 0.905f);
                scoreRoot.offsetMin = Vector2.zero;
                scoreRoot.offsetMax = Vector2.zero;
            }

            if (tetrisControls != null)
            {
                tetrisControls.anchorMin = isTetris ? new Vector2(0.055f, 0.035f) : new Vector2(0.055f, 0.045f);
                tetrisControls.anchorMax = isTetris ? new Vector2(0.945f, 0.185f) : new Vector2(0.945f, 0.355f);
                tetrisControls.offsetMin = Vector2.zero;
                tetrisControls.offsetMax = Vector2.zero;
            }

            for (int row = 0; row < MaxRows; row++)
            {
                for (int col = 0; col < MaxCols; col++)
                {
                    if (cellImages[row, col] != null)
                        cellImages[row, col].gameObject.SetActive(row < ActiveRows && col < ActiveCols);
                }
            }
        }

        private bool CanHandleTetrisGestureInput()
        {
            if (currentMode != SymbiGridMode.Tetris || tetrisControlMode != TetrisControlMode.Gesture || gameOver || resolving || !tetrisHasActivePiece)
                return false;
            if (modeOverlay != null && modeOverlay.activeInHierarchy || settingsOverlay != null && settingsOverlay.activeInHierarchy || gameOverOverlay != null && gameOverOverlay.activeInHierarchy)
                return false;

            return true;
        }

        private int GetTetrisHorizontalDragStep(Vector2 start, Vector2 current)
        {
            float deltaX = current.x - start.x;
            if (Mathf.Abs(deltaX) < TetrisHorizontalDragCellPixels)
                return 0;

            return Mathf.RoundToInt(deltaX / TetrisHorizontalDragCellPixels);
        }

        private void HandleTetrisGestureDrag(Vector2 start, Vector2 current, ref int lastHorizontalStep)
        {
            if (!CanHandleTetrisGestureInput())
                return;

            Vector2 delta = current - start;
            tetrisSoftDropActive = delta.y <= -TetrisSwipeThreshold;

            if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y) * 0.45f)
                return;

            int currentStep = GetTetrisHorizontalDragStep(start, current);
            int stepDelta = currentStep - lastHorizontalStep;
            if (stepDelta == 0)
                return;

            int direction = stepDelta > 0 ? 1 : -1;
            if (!CanStepTetrisHorizontal())
                return;

            TryMoveTetris(0, direction);
            lastHorizontalStep += direction;
        }

        private void HandleTetrisGestureRelease(Vector2 start, Vector2 end, float duration, int lastHorizontalStep)
        {
            if (!CanHandleTetrisGestureInput())
                return;

            Vector2 delta = end - start;

            if (delta.magnitude < TetrisTapRadius)
            {
                float now = Time.unscaledTime;
                if (now - tetrisLastTapTime <= 0.32f)
                {
                    tetrisLastTapTime = -10f;
                    HardDropTetris();
                    return;
                }

                tetrisLastTapTime = now;
                return;
            }

            tetrisLastTapTime = -10f;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (lastHorizontalStep == 0 && Mathf.Abs(delta.x) >= TetrisSwipeThreshold)
                    StepTetrisHorizontal(delta.x > 0f ? 1 : -1);
                return;
            }

            if (Mathf.Abs(delta.y) < TetrisSwipeThreshold)
                return;

            if (delta.y > 0f)
                TryRotateTetris();
        }

        private void SetTetrisSoftDropActive(bool active)
        {
            tetrisSoftDropActive = active && CanHandleTetrisGestureInput();
        }

        private void MoveTetrisRight()
        {
            StepTetrisHorizontal(1);
        }

        private bool CanStepTetrisHorizontal()
        {
            float now = Time.unscaledTime;
            if (now - tetrisLastHorizontalMoveTime < TetrisHorizontalMoveInterval)
                return false;

            tetrisLastHorizontalMoveTime = now;
            return true;
        }

        private void ResetTetrisHorizontalMoveDelay()
        {
            tetrisLastHorizontalMoveTime = -10f;
        }

        private void StepTetrisHorizontal(int direction)
        {
            if (direction == 0 || !CanStepTetrisHorizontal())
                return;

            TryMoveTetris(0, direction > 0 ? 1 : -1);
        }

        private void RotateTetrisButton()
        {
            TryRotateTetris();
        }

        private void DropTetrisButton()
        {
            HardDropTetris();
        }

        private void BuildDragGhost(RectTransform parent)
        {
            dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(GridLayoutGroup), typeof(CanvasGroup)).GetComponent<RectTransform>();
            dragGhost.SetParent(parent, false);
            dragGhost.anchorMin = new Vector2(0.5f, 0.5f);
            dragGhost.anchorMax = new Vector2(0.5f, 0.5f);
            dragGhost.pivot = new Vector2(0.5f, 0.5f);
            dragGhost.sizeDelta = new Vector2(164f, 164f);
            dragGhost.gameObject.SetActive(false);

            CanvasGroup group = dragGhost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.86f;

            GridLayoutGroup grid = dragGhost.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(34f, 34f);
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            dragGhostCells = new Image[16];
            for (int i = 0; i < dragGhostCells.Length; i++)
            {
                Image image = CreateImage(dragGhost, "GhostCell_" + i, new Color(1f, 1f, 1f, 0f));
                image.raycastTarget = false;
                dragGhostCells[i] = image;
            }
        }

        private GameObject BuildGameOverOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "GameOverOverlay", Color.black);
            Stretch(overlay);

            RectTransform window = CreatePanel(overlay, "Window", new Color(0.018f, 0.030f, 0.048f, 1f));
            outcomeWindow = window;
            ApplyUiSprite(window, UiSprite.Modal, 72f);
            window.anchorMin = new Vector2(0.055f, 0.055f);
            window.anchorMax = new Vector2(0.945f, 0.945f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            AddSignatureFrame(window, new Color(0.07f, 0.77f, 0.82f, 0.88f), Gold);

            RectTransform headerPanel = CreatePanel(window, "ResultHeader", new Color(0.018f, 0.045f, 0.062f, 0.94f));
            outcomeHeaderPanel = headerPanel;
            ApplyUiSprite(headerPanel, UiSprite.StatCyan, 42f);
            headerPanel.anchorMin = new Vector2(0.07f, 0.73f);
            headerPanel.anchorMax = new Vector2(0.93f, 0.91f);
            headerPanel.offsetMin = Vector2.zero;
            headerPanel.offsetMax = Vector2.zero;

            outcomeTitleText = CreateText(headerPanel, "Title", "GAME OVER", 64f, FontStyles.Bold, Gold);
            outcomeTitleText.rectTransform.anchorMin = new Vector2(0f, 0.46f);
            outcomeTitleText.rectTransform.anchorMax = new Vector2(1f, 0.94f);
            outcomeTitleText.rectTransform.offsetMin = new Vector2(22f, 0f);
            outcomeTitleText.rectTransform.offsetMax = new Vector2(-22f, 0f);

            outcomeBodyText = CreateText(headerPanel, "Body", "No remaining piece fits the board.", 26f, FontStyles.Bold, Ink);
            outcomeBodyText.textWrappingMode = TextWrappingModes.Normal;
            outcomeBodyText.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            outcomeBodyText.rectTransform.anchorMax = new Vector2(1f, 0.48f);
            outcomeBodyText.rectTransform.offsetMin = new Vector2(24f, 0f);
            outcomeBodyText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            RectTransform scorePanel = CreatePanel(window, "ResultScorePanel", new Color(0.018f, 0.045f, 0.062f, 0.94f));
            outcomeScorePanel = scorePanel;
            ApplyUiSprite(scorePanel, UiSprite.StatGold, 48f);
            scorePanel.anchorMin = new Vector2(0.12f, 0.49f);
            scorePanel.anchorMax = new Vector2(0.88f, 0.68f);
            scorePanel.offsetMin = Vector2.zero;
            scorePanel.offsetMax = Vector2.zero;

            TextMeshProUGUI scoreLabel = CreateText(scorePanel, "ScoreLabel", "SCORE", 22f, FontStyles.Bold, MutedInk);
            scoreLabel.rectTransform.anchorMin = new Vector2(0f, 0.62f);
            scoreLabel.rectTransform.anchorMax = new Vector2(1f, 0.92f);
            scoreLabel.rectTransform.offsetMin = new Vector2(20f, 0f);
            scoreLabel.rectTransform.offsetMax = new Vector2(-20f, 0f);

            outcomeScoreText = CreateText(scorePanel, "ScoreValue", "0", 88f, FontStyles.Bold, Gold);
            outcomeScoreText.fontSizeMin = 58f;
            outcomeScoreText.rectTransform.anchorMin = new Vector2(0f, 0.06f);
            outcomeScoreText.rectTransform.anchorMax = new Vector2(1f, 0.66f);
            outcomeScoreText.rectTransform.offsetMin = new Vector2(20f, 0f);
            outcomeScoreText.rectTransform.offsetMax = new Vector2(-20f, 0f);

            RectTransform metricsPanel = CreatePanel(window, "ResultMetricsPanel", new Color(0.018f, 0.045f, 0.062f, 0.94f));
            outcomeMetricsPanel = metricsPanel;
            ApplyUiSprite(metricsPanel, UiSprite.TrayPanel, 50f);
            metricsPanel.anchorMin = new Vector2(0.07f, 0.31f);
            metricsPanel.anchorMax = new Vector2(0.93f, 0.45f);
            metricsPanel.offsetMin = Vector2.zero;
            metricsPanel.offsetMax = Vector2.zero;

            outcomeBestText = CreateOutcomeMetric(metricsPanel, "OutcomeBest", "BEST", new Vector2(0.03f, 0.16f), new Vector2(0.31f, 0.86f), Gold);
            outcomeLastText = CreateOutcomeMetric(metricsPanel, "OutcomeLast", "LAST", new Vector2(0.36f, 0.16f), new Vector2(0.64f, 0.86f), new Color(0.42f, 0.92f, 1f, 1f));
            outcomeLinesText = CreateOutcomeMetric(metricsPanel, "OutcomeLines", "FIELD", new Vector2(0.69f, 0.16f), new Vector2(0.97f, 0.86f), new Color(0.52f, 1f, 0.58f, 1f));

            Button secondChance = CreateButton(window, "SecondChance", "WATCH AD", new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.275f), Vector2.zero, Vector2.zero, 30f, HandleSecondChanceAdPlaceholder);
            secondChanceButtonRoot = secondChance.gameObject;
            secondChanceButtonRoot.SetActive(false);

            Button action = CreateButton(window, "Again", "PLAY AGAIN", new Vector2(0.10f, 0.115f), new Vector2(0.90f, 0.19f), Vector2.zero, Vector2.zero, 30f, HandleOutcomeAction);
            outcomeActionLabel = action.GetComponentInChildren<TextMeshProUGUI>(true);

            Button menu = CreateButton(window, "Menu", "MENU", new Vector2(0.10f, 0.030f), new Vector2(0.90f, 0.105f), Vector2.zero, Vector2.zero, 30f, ReturnToSymbiGridModeMenuFromOutcome);
            outcomeMenuButtonRoot = menu.gameObject;
            outcomeMenuButtonRoot.SetActive(false);
            return overlay.gameObject;
        }

        private GameObject BuildSymbiMineResultTapLayer(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "SymbiMineResultTapLayer", new Color(0f, 0f, 0f, 0.01f));
            Stretch(overlay);
            Button button = overlay.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ShowPendingSymbiMineOutcome);

            TextMeshProUGUI hint = CreateText(overlay, "Hint", "TAP FOR RESULTS", 24f, FontStyles.Bold, Gold);
            hint.rectTransform.anchorMin = new Vector2(0f, 0.035f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0.085f);
            hint.rectTransform.offsetMin = new Vector2(24f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-24f, 0f);
            return overlay.gameObject;
        }

        private static TextMeshProUGUI CreateOutcomeMetric(RectTransform parent, string name, string labelText, Vector2 anchorMin, Vector2 anchorMax, Color accent)
        {
            RectTransform card = CreatePanel(parent, name, new Color(0.018f, 0.045f, 0.062f, 0.94f));
            ApplyUiSprite(card, UiSprite.StatCyan, 34f);
            card.anchorMin = anchorMin;
            card.anchorMax = anchorMax;
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            TextMeshProUGUI label = CreateText(card, "Label", labelText, 16f, FontStyles.Bold, MutedInk);
            label.rectTransform.anchorMin = new Vector2(0f, 0.54f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.94f);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);

            TextMeshProUGUI value = CreateText(card, "Value", "0", 28f, FontStyles.Bold, accent);
            value.fontSizeMin = 18f;
            value.rectTransform.anchorMin = new Vector2(0f, 0.04f);
            value.rectTransform.anchorMax = new Vector2(1f, 0.58f);
            value.rectTransform.offsetMin = new Vector2(8f, 0f);
            value.rectTransform.offsetMax = new Vector2(-8f, 0f);
            return value;
        }

        private GameObject BuildSettingsOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "SettingsOverlay", new Color(0f, 0f, 0f, 0.84f));
            Stretch(overlay);

            RectTransform window = CreatePanel(overlay, "Window", new Color(0.035f, 0.052f, 0.084f, 0.98f));
            ApplyUiSprite(window, UiSprite.Modal, 72f);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            window.sizeDelta = new Vector2(780f, 1080f);

            settingsTitleText = CreateText(window, "Title", GameLocalization.Text("symbigrid.settings"), 64f, FontStyles.Bold, Gold);
            settingsTitleText.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            settingsTitleText.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            settingsTitleText.rectTransform.offsetMin = Vector2.zero;
            settingsTitleText.rectTransform.offsetMax = Vector2.zero;

            settingsModeButton = CreateButton(window, "Mode", GameLocalization.Text("symbigrid.mode"), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(560f, 96f), 36f, OpenModeFromSettings);
            settingsRestartButton = CreateButton(window, "Restart", GameLocalization.Text("symbigrid.new_run"), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(560f, 96f), 36f, RestartFromSettings);
            settingsBackButton = CreateButton(window, "Back", GameLocalization.Text("symbigrid.back_to_platform"), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(560f, 96f), 34f, BackToMain);
            settingsCloseButton = CreateButton(window, "Close", GameLocalization.Text("symbigrid.close"), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(430f, 84f), 32f, CloseSettingsOverlay);
            return overlay.gameObject;
        }

        private GameObject BuildSymbiMineDifficultyOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "SymbiMineDifficultyOverlay", Color.black);
            Stretch(overlay);

            RectTransform window = CreatePanel(overlay, "Window", new Color(0.035f, 0.052f, 0.084f, 0.98f));
            ApplyUiSprite(window, UiSprite.Modal, 72f);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            window.sizeDelta = new Vector2(760f, 720f);

            symbiMineDifficultyTitleText = CreateText(window, "Title", "SYMBIMINE", 58f, FontStyles.Bold, Gold);
            symbiMineDifficultyTitleText.rectTransform.anchorMin = new Vector2(0f, 0.82f);
            symbiMineDifficultyTitleText.rectTransform.anchorMax = new Vector2(1f, 0.97f);
            symbiMineDifficultyTitleText.rectTransform.offsetMin = Vector2.zero;
            symbiMineDifficultyTitleText.rectTransform.offsetMax = Vector2.zero;

            symbiMineDifficultySubtitleText = CreateText(window, "Subtitle", GameLocalization.Text("symbigrid.minefield_choose"), 30f, FontStyles.Bold, Ink);
            symbiMineDifficultySubtitleText.rectTransform.anchorMin = new Vector2(0f, 0.72f);
            symbiMineDifficultySubtitleText.rectTransform.anchorMax = new Vector2(1f, 0.82f);
            symbiMineDifficultySubtitleText.rectTransform.offsetMin = new Vector2(30f, 0f);
            symbiMineDifficultySubtitleText.rectTransform.offsetMax = new Vector2(-30f, 0f);

            symbiMineBeginnerButton = CreateButton(window, "Beginner", GameLocalization.Text("symbigrid.difficulty.beginner"), new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(620f, 92f), 30f, () => StartSymbiMineDifficulty(SymbiMineDifficulty.Beginner));
            symbiMineIntermediateButton = CreateButton(window, "Intermediate", GameLocalization.Text("symbigrid.difficulty.intermediate"), new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(620f, 92f), 28f, () => StartSymbiMineDifficulty(SymbiMineDifficulty.Intermediate));
            symbiMineExpertButton = CreateButton(window, "Expert", GameLocalization.Text("symbigrid.difficulty.expert"), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(620f, 92f), 30f, () => StartSymbiMineDifficulty(SymbiMineDifficulty.Expert));
            symbiMineDifficultyBackButton = CreateButton(window, "Back", GameLocalization.Text("symbigrid.back"), new Vector2(0.5f, 0.105f), new Vector2(0.5f, 0.105f), Vector2.zero, new Vector2(320f, 68f), 26f, BackToModeOverlayFromSymbiMineDifficulty);
            return overlay.gameObject;
        }

        private GameObject BuildModeOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "ModeOverlay", Color.black);
            Stretch(overlay);

            Image menuBackground = CreateImage(overlay, "MenuBackground", Background);
            Stretch(menuBackground.rectTransform);
            Sprite bgSprite = GetMenuBackgroundSprite();
            if (bgSprite != null)
            {
                menuBackground.sprite = bgSprite;
                menuBackground.type = Image.Type.Simple;
                menuBackground.preserveAspect = false;
                menuBackground.color = Color.white;
            }

            RectTransform dimmer = CreatePanel(overlay, "MenuBackgroundDimmer", new Color(0f, 0f, 0f, 0.34f));
            Stretch(dimmer);

            Sprite titleLogo = GetSymbiGridTitleLogoSprite();
            if (titleLogo != null)
            {
                Image titleImage = CreateImage(overlay, "SymbiGridLogo", Color.white);
                titleImage.sprite = titleLogo;
                titleImage.preserveAspect = true;
                titleImage.raycastTarget = false;
                titleImage.rectTransform.anchorMin = new Vector2(0.01f, 0.855f);
                titleImage.rectTransform.anchorMax = new Vector2(0.99f, 1.015f);
                titleImage.rectTransform.offsetMin = Vector2.zero;
                titleImage.rectTransform.offsetMax = Vector2.zero;
                symbiGridMenuLogo = titleImage.rectTransform;
            }
            else
            {
                TextMeshProUGUI fallbackTitle = CreateText(overlay, "SymbiGridLogoFallback", "SYMBIGRID", 72f, FontStyles.Bold, Gold);
                fallbackTitle.rectTransform.anchorMin = new Vector2(0f, 0.88f);
                fallbackTitle.rectTransform.anchorMax = new Vector2(1f, 1.01f);
                fallbackTitle.rectTransform.offsetMin = Vector2.zero;
                fallbackTitle.rectTransform.offsetMax = Vector2.zero;
                symbiGridMenuLogo = fallbackTitle.rectTransform;
            }

            modeGroup = new GameObject("ModeGroup", typeof(RectTransform)).GetComponent<RectTransform>();
            modeGroup.SetParent(overlay, false);
            Stretch(modeGroup);
            modeGroup.pivot = new Vector2(0.5f, 0.5f);

            retroGridModeButton = CreateLogoButton(modeGroup, "RetroGrid", "RETROGRID", GetRetroGridTitleLogoSprite(), new Vector2(0.5f, 0.555f), () => OpenModePreview(SymbiGridMode.Tetris));
            classicGridModeButton = CreateLogoButton(modeGroup, "ClassicGrid", "CLASSICGRID", GetClassicGridTitleLogoSprite(), new Vector2(0.5f, 0.445f), () => OpenModePreview(SymbiGridMode.Classic));
            symbiMineModeButton = CreateLogoButton(modeGroup, "SymbiMine", "SYMBIMINE", GetSymbiMineTitleLogoSprite(), new Vector2(0.5f, 0.335f), () => OpenModePreview(SymbiGridMode.SymbiMine));
            BuildSymbiMineUnavailableBadge(symbiMineModeButton.GetComponent<RectTransform>());
            modePlatformBackButton = CreateButton(modeGroup, "BackToPlatform", GameLocalization.Text("symbigrid.back_to_platform"), new Vector2(0.5f, 0.075f), new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(650f, 94f), 34f, BackToMain);

            modePreviewWindow = BuildRetroGridPreviewWindow(modeGroup);
            modePreviewDetailsPanel = BuildModePreviewDetails(modeGroup);
            modeControlsOverlay = BuildModeControlsOverlay(modeGroup);
            modePreviewBackButton = CreateButton(modeGroup, "PreviewBack", GameLocalization.Text("symbigrid.back"), new Vector2(0.25f, 0.035f), new Vector2(0.25f, 0.035f), Vector2.zero, new Vector2(330f, 92f), 31f, CloseModePreview);
            modePreviewStartButton = CreateButton(modeGroup, "PreviewStart", GameLocalization.Text("symbigrid.start"), new Vector2(0.75f, 0.035f), new Vector2(0.75f, 0.035f), Vector2.zero, new Vector2(330f, 92f), 31f, StartSelectedModePreview);
            modePreviewBackButton.gameObject.SetActive(false);
            modePreviewStartButton.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private void BuildSymbiMineUnavailableBadge(RectTransform parent)
        {
            if (parent == null)
                return;

            RectTransform badge = CreatePanel(parent, "UnavailableBadge", new Color(0.03f, 0.06f, 0.10f, 0.96f));
            ApplyUiSprite(badge, UiSprite.StatGold, 30f);
            badge.anchorMin = new Vector2(0.5f, 0.12f);
            badge.anchorMax = new Vector2(0.5f, 0.12f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = Vector2.zero;
            badge.sizeDelta = new Vector2(390f, 52f);
            badge.GetComponent<Image>().raycastTarget = false;

            symbiMineUnavailableBadgeText = CreateText(badge, "Label", GameLocalization.Text("symbigrid.mine_unavailable.status"), 25f, FontStyles.Bold, Ink);
            Stretch(symbiMineUnavailableBadgeText.rectTransform);
            symbiMineUnavailableBadgeText.rectTransform.offsetMin = new Vector2(18f, 3f);
            symbiMineUnavailableBadgeText.rectTransform.offsetMax = new Vector2(-18f, -3f);
            badge.gameObject.SetActive(!SymbiMineAccessEnabled);
        }

        private GameObject BuildSymbiMineUnavailableOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "SymbiMineUnavailableOverlay", new Color(0f, 0f, 0f, 0.98f));
            Stretch(overlay);

            Image background = CreateImage(overlay, "Background", Background);
            Stretch(background.rectTransform);
            Sprite menuSprite = GetMenuBackgroundSprite();
            if (menuSprite != null)
            {
                background.sprite = menuSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
                background.color = new Color(0.30f, 0.38f, 0.48f, 0.45f);
            }

            RectTransform dimmer = CreatePanel(overlay, "Dimmer", new Color(0f, 0f, 0f, 0.72f));
            Stretch(dimmer);

            RectTransform window = CreatePanel(overlay, "Window", new Color(0.018f, 0.030f, 0.048f, 0.99f));
            ApplyUiSprite(window, UiSprite.Modal, 72f);
            window.anchorMin = new Vector2(0.07f, 0.245f);
            window.anchorMax = new Vector2(0.93f, 0.755f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            AddSignatureFrame(window, new Color(0.07f, 0.77f, 0.82f, 0.92f), Gold);
            AddCornerBrackets(window, new Color(0.07f, 0.77f, 0.82f, 0.82f), Gold);

            TextMeshProUGUI title = CreateText(window, "Title", "SYMBIMINE", 64f, FontStyles.Bold, Gold);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.76f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.93f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            RectTransform statusPanel = CreatePanel(window, "StatusPanel", new Color(0.04f, 0.08f, 0.13f, 0.96f));
            ApplyUiSprite(statusPanel, UiSprite.StatGold, 36f);
            statusPanel.anchorMin = new Vector2(0.16f, 0.59f);
            statusPanel.anchorMax = new Vector2(0.84f, 0.72f);
            statusPanel.offsetMin = Vector2.zero;
            statusPanel.offsetMax = Vector2.zero;

            symbiMineUnavailableStatusText = CreateText(statusPanel, "Status", GameLocalization.Text("symbigrid.mine_unavailable.status"), 32f, FontStyles.Bold, Ink);
            Stretch(symbiMineUnavailableStatusText.rectTransform);
            symbiMineUnavailableStatusText.rectTransform.offsetMin = new Vector2(16f, 4f);
            symbiMineUnavailableStatusText.rectTransform.offsetMax = new Vector2(-16f, -4f);

            symbiMineUnavailableBodyText = CreateText(window, "Body", GameLocalization.Text("symbigrid.mine_unavailable.body"), 34f, FontStyles.Bold, Ink);
            symbiMineUnavailableBodyText.textWrappingMode = TextWrappingModes.Normal;
            symbiMineUnavailableBodyText.lineSpacing = 10f;
            symbiMineUnavailableBodyText.fontSizeMin = 27f;
            symbiMineUnavailableBodyText.rectTransform.anchorMin = new Vector2(0.10f, 0.27f);
            symbiMineUnavailableBodyText.rectTransform.anchorMax = new Vector2(0.90f, 0.55f);
            symbiMineUnavailableBodyText.rectTransform.offsetMin = Vector2.zero;
            symbiMineUnavailableBodyText.rectTransform.offsetMax = Vector2.zero;

            symbiMineUnavailableCloseButton = CreateButton(window, "Back", GameLocalization.Text("symbigrid.back"), new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(430f, 86f), 32f, CloseSymbiMineUnavailableNotice);
            return overlay.gameObject;
        }

        private static Image CreateTopTitleLogo(RectTransform parent, string name, Sprite sprite)
        {
            Image image = CreateImage(parent, name, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.14f, 0.08f);
            image.rectTransform.anchorMax = new Vector2(0.86f, 0.98f);
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.gameObject.SetActive(false);
            return image;
        }

        private Button CreateLogoButton(RectTransform parent, string name, string fallbackText, Sprite logo, Vector2 anchor, Action action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = ModeLogoButtonSize;

            Image image = go.GetComponent<Image>();
            image.raycastTarget = true;
            image.color = logo != null ? Color.white : new Color(0.09f, 0.16f, 0.25f, 0.12f);

            if (logo != null)
            {
                image.sprite = logo;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            else
            {
                ApplyUiSprite(image, UiSprite.Button, 42f);
                TextMeshProUGUI label = CreateText(go.transform, "Label", fallbackText, 38f, FontStyles.Bold, Ink);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(16f, 6f);
                label.rectTransform.offsetMax = new Vector2(-16f, -6f);
            }

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.66f, 0.88f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    PlaySound(buttonClip, 0.52f, 1f);
                    action();
                });
            }

            return button;
        }

        private RectTransform BuildModePreviewDetails(RectTransform parent)
        {
            RectTransform panel = new GameObject("ModePreviewDetails", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = new Vector2(0.5f, 0.425f);
            panel.anchorMax = new Vector2(0.5f, 0.425f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(730f, 440f);
            panel.gameObject.SetActive(false);

            RectTransform infoPanel = CreatePanel(panel, "InfoPanel", new Color(0.018f, 0.030f, 0.048f, 0.91f));
            ApplyUiSprite(infoPanel, UiSprite.TrayPanel, 52f);
            infoPanel.anchorMin = new Vector2(0f, -0.18f);
            infoPanel.anchorMax = new Vector2(1f, 0.66f);
            infoPanel.offsetMin = new Vector2(-28f, 0f);
            infoPanel.offsetMax = new Vector2(28f, 0f);

            modePreviewDescriptionText = CreateText(infoPanel, "Description", "", 42f, FontStyles.Bold, Ink);
            modePreviewDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            modePreviewDescriptionText.alignment = TextAlignmentOptions.Center;
            modePreviewDescriptionText.fontSizeMin = 32f;
            modePreviewDescriptionText.lineSpacing = 8f;
            modePreviewDescriptionText.rectTransform.anchorMin = new Vector2(0.07f, 0.10f);
            modePreviewDescriptionText.rectTransform.anchorMax = new Vector2(0.93f, 0.90f);
            modePreviewDescriptionText.rectTransform.offsetMin = Vector2.zero;
            modePreviewDescriptionText.rectTransform.offsetMax = Vector2.zero;

            modePreviewControlsButton = CreateButton(panel, "ControlsButton", GameLocalization.Text("symbigrid.controls"), new Vector2(0.5f, -0.27f), new Vector2(0.5f, -0.27f), Vector2.zero, new Vector2(420f, 78f), 34f, OpenModeControlsOverlay);

            RectTransform scorePanel = CreatePanel(panel, "ScorePanel", new Color(0.012f, 0.026f, 0.040f, 0.92f));
            ApplyUiSprite(scorePanel, UiSprite.StatCyan, 36f);
            scorePanel.anchorMin = new Vector2(0f, 0.66f);
            scorePanel.anchorMax = new Vector2(1f, 1f);
            scorePanel.offsetMin = new Vector2(-28f, 0f);
            scorePanel.offsetMax = new Vector2(28f, 54f);

            TextMeshProUGUI scoreLabel = CreateText(scorePanel, "ScoreLabel", "SCORE", 38f, FontStyles.Bold, MutedInk);
            scoreLabel.rectTransform.anchorMin = new Vector2(0.34f, 0.50f);
            scoreLabel.rectTransform.anchorMax = new Vector2(0.66f, 0.98f);
            scoreLabel.rectTransform.offsetMin = Vector2.zero;
            scoreLabel.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI bestLabel = CreateText(scorePanel, "BestLabel", "BEST", 33f, FontStyles.Bold, MutedInk);
            bestLabel.rectTransform.anchorMin = new Vector2(0.04f, 0.55f);
            bestLabel.rectTransform.anchorMax = new Vector2(0.32f, 0.98f);
            bestLabel.rectTransform.offsetMin = Vector2.zero;
            bestLabel.rectTransform.offsetMax = Vector2.zero;

            modePreviewBestText = CreateText(scorePanel, "BestValue", "0", 46f, FontStyles.Bold, Gold);
            modePreviewBestText.rectTransform.anchorMin = new Vector2(0.04f, 0.02f);
            modePreviewBestText.rectTransform.anchorMax = new Vector2(0.32f, 0.60f);
            modePreviewBestText.rectTransform.offsetMin = Vector2.zero;
            modePreviewBestText.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI lastLabel = CreateText(scorePanel, "LastLabel", "LAST", 33f, FontStyles.Bold, MutedInk);
            lastLabel.rectTransform.anchorMin = new Vector2(0.68f, 0.55f);
            lastLabel.rectTransform.anchorMax = new Vector2(0.96f, 0.98f);
            lastLabel.rectTransform.offsetMin = Vector2.zero;
            lastLabel.rectTransform.offsetMax = Vector2.zero;

            modePreviewLastText = CreateText(scorePanel, "LastValue", "0", 46f, FontStyles.Bold, new Color(0.42f, 0.92f, 1f, 1f));
            modePreviewLastText.rectTransform.anchorMin = new Vector2(0.68f, 0.02f);
            modePreviewLastText.rectTransform.anchorMax = new Vector2(0.96f, 0.60f);
            modePreviewLastText.rectTransform.offsetMin = Vector2.zero;
            modePreviewLastText.rectTransform.offsetMax = Vector2.zero;

            return panel;
        }

        private GameObject BuildModeControlsOverlay(RectTransform parent)
        {
            RectTransform overlay = CreatePanel(parent, "ModeControlsOverlay", Color.black);
            Stretch(overlay);
            overlay.gameObject.SetActive(false);

            RectTransform window = CreatePanel(overlay, "Window", new Color(0.018f, 0.030f, 0.048f, 0.98f));
            ApplyUiSprite(window, UiSprite.Modal, 64f);
            window.anchorMin = new Vector2(0.035f, 0.045f);
            window.anchorMax = new Vector2(0.965f, 0.955f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            AddSignatureFrame(window, new Color(0.07f, 0.77f, 0.82f, 0.88f), Gold);

            modeControlsTitleText = CreateText(window, "Title", GameLocalization.Text("symbigrid.controls"), 68f, FontStyles.Bold, Gold);
            modeControlsTitleText.rectTransform.anchorMin = new Vector2(0.06f, 0.79f);
            modeControlsTitleText.rectTransform.anchorMax = new Vector2(0.94f, 0.94f);
            modeControlsTitleText.rectTransform.offsetMin = Vector2.zero;
            modeControlsTitleText.rectTransform.offsetMax = Vector2.zero;

            modeControlsBodyText = CreateText(window, "Body", "", 44f, FontStyles.Bold, Ink);
            modeControlsBodyText.textWrappingMode = TextWrappingModes.Normal;
            modeControlsBodyText.alignment = TextAlignmentOptions.Center;
            modeControlsBodyText.lineSpacing = 28f;
            modeControlsBodyText.fontSizeMin = 34f;
            modeControlsBodyText.rectTransform.anchorMin = new Vector2(0.08f, 0.22f);
            modeControlsBodyText.rectTransform.anchorMax = new Vector2(0.92f, 0.74f);
            modeControlsBodyText.rectTransform.offsetMin = Vector2.zero;
            modeControlsBodyText.rectTransform.offsetMax = Vector2.zero;

            modeControlsCloseButton = CreateButton(window, "Close", GameLocalization.Text("symbigrid.close"), new Vector2(0.5f, 0.105f), new Vector2(0.5f, 0.105f), Vector2.zero, new Vector2(430f, 86f), 32f, CloseModeControlsOverlay);
            return overlay.gameObject;
        }

        private RectTransform BuildRetroGridPreviewWindow(RectTransform parent)
        {
            RectTransform window = CreatePanel(parent, "RetroGridPreview", new Color(0.018f, 0.030f, 0.048f, 0.94f));
            ApplyUiSprite(window, UiSprite.Modal, 64f);
            window.anchorMin = new Vector2(0.5f, 0.735f);
            window.anchorMax = new Vector2(0.5f, 0.735f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            window.sizeDelta = new Vector2(560f, 610f);
            window.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            window.gameObject.SetActive(false);

            retroGridPreviewBoard = CreatePanel(window, "Board", new Color(0.004f, 0.010f, 0.018f, 0.96f));
            retroGridPreviewBoard.anchorMin = new Vector2(0.24f, 0.09f);
            retroGridPreviewBoard.anchorMax = new Vector2(0.76f, 0.91f);
            retroGridPreviewBoard.offsetMin = Vector2.zero;
            retroGridPreviewBoard.offsetMax = Vector2.zero;

            GridLayoutGroup grid = retroGridPreviewBoard.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.cellSize = new Vector2(30f, 30f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.MiddleCenter;

            retroGridPreviewCells.Clear();
            retroGridPreviewLabels.Clear();
            for (int i = 0; i < 112; i++)
            {
                Image cell = CreateImage(retroGridPreviewBoard, "Cell_" + i, new Color(0.03f, 0.07f, 0.10f, 0.82f));
                cell.raycastTarget = false;
                retroGridPreviewCells.Add(cell);

                TextMeshProUGUI label = CreateText(cell.transform, "Label", "", 18f, FontStyles.Bold, Ink);
                Stretch(label.rectTransform);
                label.raycastTarget = false;
                retroGridPreviewLabels.Add(label);
            }

            return window;
        }

        private void OpenModePreview(SymbiGridMode mode)
        {
            if (modePreviewAnimating)
                return;

            if (mode == SymbiGridMode.SymbiMine && !SymbiMineAccessEnabled)
            {
                ShowSymbiMineUnavailableNotice();
                return;
            }

            selectedModePreview = mode;
            if (modeSelectionRoutine != null)
                StopCoroutine(modeSelectionRoutine);

            modeSelectionRoutine = StartCoroutine(AnimateModeSelection(mode));
        }

        private void ShowSymbiMineUnavailableNotice()
        {
            if (symbiMineUnavailableOverlay == null)
                return;

            RefreshLocalizedText();
            symbiMineUnavailableOverlay.transform.SetAsLastSibling();
            symbiMineUnavailableOverlay.SetActive(true);
        }

        private void CloseSymbiMineUnavailableNotice()
        {
            if (symbiMineUnavailableOverlay != null)
                symbiMineUnavailableOverlay.SetActive(false);
        }

        private IEnumerator AnimateModeSelection(SymbiGridMode mode)
        {
            modePreviewAnimating = true;
            modePreviewOpen = true;

            SetModeButtonsInteractable(false);
            if (modePreviewWindow != null)
                modePreviewWindow.gameObject.SetActive(false);
            if (modePreviewBackButton != null)
                modePreviewBackButton.gameObject.SetActive(false);
            if (modePreviewStartButton != null)
                modePreviewStartButton.gameObject.SetActive(false);
            if (modePlatformBackButton != null)
                modePlatformBackButton.gameObject.SetActive(false);
            if (modePreviewDetailsPanel != null)
                modePreviewDetailsPanel.gameObject.SetActive(false);
            if (modeControlsOverlay != null)
                modeControlsOverlay.SetActive(false);

            RectTransform retro = retroGridModeButton != null ? retroGridModeButton.GetComponent<RectTransform>() : null;
            RectTransform classic = classicGridModeButton != null ? classicGridModeButton.GetComponent<RectTransform>() : null;
            RectTransform mine = symbiMineModeButton != null ? symbiMineModeButton.GetComponent<RectTransform>() : null;
            RectTransform selected = GetModeButtonRect(mode);

            Vector2 retroStart = retro != null ? retro.anchorMin : new Vector2(0.5f, 0.555f);
            Vector2 classicStart = classic != null ? classic.anchorMin : new Vector2(0.5f, 0.445f);
            Vector2 mineStart = mine != null ? mine.anchorMin : new Vector2(0.5f, 0.335f);
            Vector2 retroMaxStart = retro != null ? retro.anchorMax : retroStart;
            Vector2 classicMaxStart = classic != null ? classic.anchorMax : classicStart;
            Vector2 mineMaxStart = mine != null ? mine.anchorMax : mineStart;
            Vector2 titleMinStart = symbiGridMenuLogo != null ? symbiGridMenuLogo.anchorMin : new Vector2(0.01f, 0.855f);
            Vector2 titleMaxStart = symbiGridMenuLogo != null ? symbiGridMenuLogo.anchorMax : new Vector2(0.99f, 1.015f);
            Vector2 selectedMinTarget = titleMinStart;
            Vector2 selectedMaxTarget = titleMaxStart;
            Color subtitleStart = modeSubtitle != null ? modeSubtitle.color : Color.white;

            const float duration = 0.48f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                if (mode == SymbiGridMode.Tetris)
                    MoveModeButton(retro, retroStart, retroMaxStart, selectedMinTarget, selectedMaxTarget, eased);
                else
                    MoveModeButton(retro, retroStart, new Vector2(-0.45f, retroStart.y), eased);

                if (mode == SymbiGridMode.Classic)
                    MoveModeButton(classic, classicStart, classicMaxStart, selectedMinTarget, selectedMaxTarget, eased);
                else
                    MoveModeButton(classic, classicStart, new Vector2(1.45f, classicStart.y), eased);

                if (mode == SymbiGridMode.SymbiMine)
                    MoveModeButton(mine, mineStart, mineMaxStart, selectedMinTarget, selectedMaxTarget, eased);
                else
                    MoveModeButton(mine, mineStart, new Vector2(-0.45f, mineStart.y), eased);
                if (symbiGridMenuLogo != null)
                {
                    symbiGridMenuLogo.anchorMin = Vector2.LerpUnclamped(titleMinStart, new Vector2(0.01f, 1.05f), eased);
                    symbiGridMenuLogo.anchorMax = Vector2.LerpUnclamped(titleMaxStart, new Vector2(0.99f, 1.21f), eased);
                    symbiGridMenuLogo.offsetMin = Vector2.zero;
                    symbiGridMenuLogo.offsetMax = Vector2.zero;
                }

                if (modeSubtitle != null)
                    modeSubtitle.color = new Color(subtitleStart.r, subtitleStart.g, subtitleStart.b, Mathf.Lerp(1f, 0f, eased));

                yield return null;
            }

            SetLogoAnchors(selected, selectedMinTarget, selectedMaxTarget);
            if (retro != selected)
                SetLogoAnchor(retro, new Vector2(-0.45f, retroStart.y));
            if (classic != selected)
                SetLogoAnchor(classic, new Vector2(1.45f, classicStart.y));
            if (mine != selected)
                SetLogoAnchor(mine, new Vector2(-0.45f, mineStart.y));
            if (symbiGridMenuLogo != null)
            {
                symbiGridMenuLogo.anchorMin = new Vector2(0.01f, 1.05f);
                symbiGridMenuLogo.anchorMax = new Vector2(0.99f, 1.21f);
                symbiGridMenuLogo.offsetMin = Vector2.zero;
                symbiGridMenuLogo.offsetMax = Vector2.zero;
            }
            if (modeSubtitle != null)
                modeSubtitle.gameObject.SetActive(false);

            yield return StartCoroutine(ShowRetroGridPreviewWindow());
            if (modePreviewBackButton != null)
                modePreviewBackButton.gameObject.SetActive(true);
            if (modePreviewStartButton != null)
                modePreviewStartButton.gameObject.SetActive(true);
            RefreshModePreviewDetails(mode);
            if (modePreviewDetailsPanel != null)
                modePreviewDetailsPanel.gameObject.SetActive(true);
            if (modePreviewWindow != null)
                modePreviewWindow.SetAsLastSibling();
            RectTransform previewLogo = GetModeButtonRect(mode);
            if (previewLogo != null)
                previewLogo.SetAsLastSibling();
            modePreviewAnimating = false;
        }

        private IEnumerator ShowRetroGridPreviewWindow()
        {
            if (modePreviewWindow == null)
                yield break;

            modePreviewWindow.gameObject.SetActive(true);
            RectTransform selectedLogo = GetModeButtonRect(selectedModePreview);
            if (selectedLogo != null)
                selectedLogo.SetAsLastSibling();

            CanvasGroup group = modePreviewWindow.GetComponent<CanvasGroup>();
            if (group == null)
                group = modePreviewWindow.gameObject.AddComponent<CanvasGroup>();

            if (retroGridPreviewRoutine != null)
                StopCoroutine(retroGridPreviewRoutine);
            retroGridPreviewRoutine = StartCoroutine(RetroGridPreviewLoop());

            Vector3 startScale = Vector3.one * 0.88f;
            Vector3 endScale = Vector3.one;
            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                group.alpha = Mathf.Lerp(0f, 1f, eased);
                modePreviewWindow.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                yield return null;
            }

            group.alpha = 1f;
            modePreviewWindow.localScale = Vector3.one;
        }

        private IEnumerator RetroGridPreviewLoop()
        {
            int frame = 0;
            while (modePreviewWindow != null && modePreviewWindow.gameObject.activeInHierarchy)
            {
                DrawModePreviewFrame(frame++);
                yield return new WaitForSecondsRealtime(0.105f);
            }
        }

        private void DrawModePreviewFrame(int frame)
        {
            ApplyModePreviewBoardLayout();
            if (selectedModePreview == SymbiGridMode.Classic)
                DrawClassicGridPreviewFrame(frame);
            else if (selectedModePreview == SymbiGridMode.SymbiMine)
                DrawSymbiMinePreviewFrame(frame);
            else
                DrawRetroGridPreviewFrame(frame);
        }

        private void ApplyModePreviewBoardLayout()
        {
            if (retroGridPreviewBoard == null)
                return;

            GridLayoutGroup grid = retroGridPreviewBoard.GetComponent<GridLayoutGroup>();
            Image boardImage = retroGridPreviewBoard.GetComponent<Image>();
            if (selectedModePreview == SymbiGridMode.Tetris)
            {
                SetPreviewActiveCells(14);
                retroGridPreviewBoard.anchorMin = new Vector2(0.24f, 0.09f);
                retroGridPreviewBoard.anchorMax = new Vector2(0.76f, 0.91f);
                if (grid != null)
                {
                    grid.cellSize = new Vector2(30f, 30f);
                    grid.spacing = new Vector2(4f, 4f);
                    grid.padding = new RectOffset(8, 8, 8, 8);
                }
                if (boardImage != null)
                    boardImage.color = new Color(0.004f, 0.010f, 0.018f, 0.96f);
            }
            else if (selectedModePreview == SymbiGridMode.Classic)
            {
                SetPreviewActiveCells(12);
                retroGridPreviewBoard.anchorMin = new Vector2(0.04f, 0.06f);
                retroGridPreviewBoard.anchorMax = new Vector2(0.96f, 0.90f);
                if (grid != null)
                {
                    grid.cellSize = new Vector2(34f, 34f);
                    grid.spacing = new Vector2(6f, 6f);
                    grid.padding = new RectOffset(8, 8, 8, 8);
                }
                if (boardImage != null)
                    boardImage.color = new Color(0.004f, 0.010f, 0.018f, 0.06f);
            }
            else
            {
                SetPreviewActiveCells(8);
                retroGridPreviewBoard.anchorMin = new Vector2(0.04f, 0.07f);
                retroGridPreviewBoard.anchorMax = new Vector2(0.96f, 0.89f);
                if (grid != null)
                {
                    grid.cellSize = new Vector2(48f, 48f);
                    grid.spacing = new Vector2(7f, 7f);
                    grid.padding = new RectOffset(8, 8, 8, 8);
                }
                if (boardImage != null)
                    boardImage.color = new Color(0.004f, 0.010f, 0.018f, 0.08f);
            }

            retroGridPreviewBoard.offsetMin = Vector2.zero;
            retroGridPreviewBoard.offsetMax = Vector2.zero;
        }

        private void SetPreviewActiveCells(int rows)
        {
            int activeCount = Mathf.Clamp(rows, 1, 14) * 8;
            for (int i = 0; i < retroGridPreviewCells.Count; i++)
            {
                bool active = i < activeCount;
                if (retroGridPreviewCells[i] != null)
                    retroGridPreviewCells[i].gameObject.SetActive(active);
            }
        }

        private void DrawRetroGridPreviewFrame(int frame)
        {
            const int cols = 8;
            const int rows = 14;
            if (retroGridPreviewCells.Count < cols * rows)
                return;

            ClearRetroPreviewCells();

            int phase = frame % 64;
            bool locked = phase >= 40;
            bool flashing = phase >= 48 && phase < 56;
            bool cleared = phase >= 56;
            Color cyanBlock = new Color(0.13f, 0.66f, 0.82f, 0.94f);
            Color blueBlock = new Color(0.20f, 0.32f, 0.78f, 0.92f);
            Color pinkBlock = new Color(0.92f, 0.24f, 0.72f, 0.95f);
            Color oBlock = new Color(1f, 0.76f, 0.22f, 0.98f);

            if (!cleared)
            {
                for (int col = 0; col < cols; col++)
                {
                    bool gap = col == 3 || col == 4;
                    if (!gap || locked)
                        PaintRetroPreviewCell(rows - 1, col, flashing && phase % 2 == 0 ? Gold : cyanBlock);
                }

                PaintRetroPreviewCell(rows - 2, 0, blueBlock);
                PaintRetroPreviewCell(rows - 2, 1, blueBlock);
                PaintRetroPreviewCell(rows - 2, 6, pinkBlock);
                PaintRetroPreviewCell(rows - 2, 7, pinkBlock);
                PaintRetroPreviewCell(rows - 3, 1, blueBlock);
                PaintRetroPreviewCell(rows - 3, 7, pinkBlock);
            }
            else
            {
                PaintRetroPreviewCell(rows - 1, 0, blueBlock);
                PaintRetroPreviewCell(rows - 1, 1, blueBlock);
                PaintRetroPreviewCell(rows - 1, 6, pinkBlock);
                PaintRetroPreviewCell(rows - 1, 7, pinkBlock);
            }

            if (!locked)
            {
                int fallRow = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0f, rows - 2f, phase / 39f)), 0, rows - 2);
                PaintRetroPreviewOBlock(fallRow, 3, oBlock);
            }
            else if (!cleared)
            {
                PaintRetroPreviewOBlock(rows - 2, 3, flashing && phase % 2 == 0 ? Color.white : oBlock);
            }
        }

        private void ClearRetroPreviewCells()
        {
            for (int i = 0; i < retroGridPreviewCells.Count; i++)
            {
                if (retroGridPreviewCells[i] != null)
                {
                    retroGridPreviewCells[i].sprite = null;
                    retroGridPreviewCells[i].color = new Color(0.025f, 0.055f, 0.078f, 0.88f);
                    retroGridPreviewCells[i].preserveAspect = false;
                }
                if (i < retroGridPreviewLabels.Count && retroGridPreviewLabels[i] != null)
                    retroGridPreviewLabels[i].text = "";
            }
        }

        private void DrawClassicGridPreviewFrame(int frame)
        {
            const int top = 0;
            const int size = 8;
            int phase = frame % 72;
            bool dragging = phase >= 14 && phase < 34;
            bool placed = phase >= 34;
            bool flashing = phase >= 42 && phase < 54;
            bool cleared = phase >= 54;
            Color empty = new Color(0.025f, 0.055f, 0.078f, 0.90f);
            Color traySlot = new Color(0.018f, 0.052f, 0.075f, 0.96f);
            Color cyan = new Color(0.13f, 0.70f, 0.82f, 0.95f);
            Color magenta = new Color(0.86f, 0.23f, 0.70f, 0.94f);
            Color blue = new Color(0.21f, 0.32f, 0.76f, 0.94f);
            Color ghost = new Color(0.72f, 0.95f, 1f, 0.70f);
            Color flash = phase % 2 == 0 ? Gold : cyan;

            ClearPreviewToTransparent();
            FillPreviewSquare(top, size, empty);
            DrawClassicPreviewSlots(traySlot, cyan, blue, magenta, !dragging && !placed);

            PaintPreviewCell(top + 1, 1, blue);
            PaintPreviewCell(top + 1, 2, blue);
            PaintPreviewCell(top + 2, 1, blue);
            PaintPreviewCell(top + 2, 5, magenta);
            PaintPreviewCell(top + 2, 6, magenta);
            PaintPreviewCell(top + 3, 6, magenta);

            if (!cleared)
            {
                for (int col = 0; col < size; col++)
                {
                    bool lShapeTarget = col == 3 || col == 4;
                    if (!lShapeTarget || placed)
                        PaintPreviewCell(top + 7, col, flashing ? flash : cyan);
                }

                if (placed && !cleared)
                    PaintClassicPreviewShape(top + 6, 3, flashing ? flash : cyan, 0);

                if (dragging)
                {
                    float travel = Mathf.Clamp01((phase - 14f) / 19f);
                    int dragRow = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(9f, 6f, travel)), 6, 9);
                    int dragCol = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 3f, travel)), 3, 3);
                    PaintClassicPreviewShape(dragRow, dragCol, ghost, 0);
                }
            }
            else
            {
                PaintPreviewCell(top + 6, 0, blue);
                PaintPreviewCell(top + 6, 1, blue);
                PaintPreviewCell(top + 6, 6, magenta);
                PaintPreviewCell(top + 6, 7, magenta);
            }
        }

        private void DrawClassicPreviewSlots(Color slot, Color cyan, Color blue, Color magenta, bool showMiddlePiece)
        {
            FillPreviewRect(9, 0, 3, 2, slot);
            FillPreviewRect(9, 3, 3, 2, slot);
            FillPreviewRect(9, 6, 3, 2, slot);

            PaintPreviewCell(9, 0, blue);
            PaintPreviewCell(10, 0, blue);
            PaintPreviewCell(10, 1, blue);

            if (showMiddlePiece)
                PaintClassicPreviewShape(9, 3, cyan, 0);

            PaintPreviewCell(9, 6, magenta);
            PaintPreviewCell(9, 7, magenta);
            PaintPreviewCell(10, 7, magenta);
        }

        private void PaintClassicPreviewBar(int row, int col, Color color)
        {
            PaintPreviewCell(row, col, color);
            PaintPreviewCell(row, col + 1, color);
            PaintPreviewCell(row, col + 2, color);
        }

        private void PaintClassicPreviewShape(int row, int col, Color color, int variant)
        {
            if (variant == 0)
            {
                PaintPreviewCell(row, col, color);
                PaintPreviewCell(row + 1, col, color);
                PaintPreviewCell(row + 1, col + 1, color);
                return;
            }

            if (variant == 1)
            {
                PaintPreviewCell(row, col, color);
                PaintPreviewCell(row, col + 1, color);
                PaintPreviewCell(row + 1, col + 1, color);
                return;
            }

            PaintPreviewCell(row, col, color);
            PaintPreviewCell(row, col + 1, color);
            PaintPreviewCell(row + 1, col, color);
        }

        private void DrawSymbiMinePreviewFrame(int frame)
        {
            const int top = 0;
            const int left = 0;
            const int size = 8;
            const int clickRow = 5;
            const int clickCol = 1;
            int phase = frame % 96;
            Color hidden = new Color(0.010f, 0.030f, 0.046f, 0.96f);
            Color opened = new Color(0.075f, 0.145f, 0.185f, 0.98f);
            Color zero = new Color(0.055f, 0.110f, 0.145f, 0.98f);
            Color flag = new Color(0.78f, 0.10f, 0.16f, 0.98f);
            Color tap = phase % 8 < 4 ? Gold : new Color(0.95f, 0.58f, 0.18f, 1f);
            Color detonated = phase % 2 == 0 ? new Color(1f, 0.30f, 0.13f, 1f) : Gold;

            ClearPreviewToTransparent();
            FillPreviewRect(top, left, size, size, hidden);

            if (phase < 18)
            {
                PaintPreviewCell(top + clickRow, left + clickCol, tap);
                return;
            }

            bool[,] revealMask = BuildSymbiMinePreviewRevealMask(clickRow, clickCol);
            int revealRadius = Mathf.Clamp((phase - 18) / 5, 0, 8);
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    int distance = Mathf.Abs(row - clickRow) + Mathf.Abs(col - clickCol);
                    if (revealMask[row, col] && distance <= revealRadius)
                    {
                        int count = CountSymbiMinePreviewAdjacentMines(row, col);
                        PaintPreviewCell(top + row, left + col, count > 0 ? opened : zero);
                        if (count > 0)
                            PaintPreviewLabel(top + row, left + col, count.ToString(), GetSymbiMinePreviewNumberColor(count));
                    }
                }
            }

            if (phase >= 50)
            {
                PaintPreviewCell(top + 1, left + 1, flag);
                PaintPreviewLabel(top + 1, left + 1, "F", Color.white);
            }
            if (phase >= 58)
            {
                PaintPreviewCell(top + 6, left + 5, flag);
                PaintPreviewLabel(top + 6, left + 5, "F", Color.white);
            }

            if (phase >= 70)
            {
                PaintPreviewCell(top + 4, left + 7, detonated);
                PaintPreviewMine(top + 4, left + 7);
            }

            if (phase >= 82)
            {
                for (int i = 0; i < SymbiMinePreviewMines.Length; i++)
                    PaintPreviewMine(top + SymbiMinePreviewMines[i].x, left + SymbiMinePreviewMines[i].y);
            }
        }

        private bool[,] BuildSymbiMinePreviewRevealMask(int startRow, int startCol)
        {
            bool[,] revealed = new bool[8, 8];
            if (IsSymbiMinePreviewMine(startRow, startCol))
                return revealed;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            revealed[startRow, startCol] = true;
            queue.Enqueue(new Vector2Int(startRow, startCol));

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (CountSymbiMinePreviewAdjacentMines(cell.x, cell.y) > 0)
                    continue;

                for (int row = cell.x - 1; row <= cell.x + 1; row++)
                {
                    for (int col = cell.y - 1; col <= cell.y + 1; col++)
                    {
                        if (row < 0 || row >= 8 || col < 0 || col >= 8 || revealed[row, col] || IsSymbiMinePreviewMine(row, col))
                            continue;

                        revealed[row, col] = true;
                        if (CountSymbiMinePreviewAdjacentMines(row, col) == 0)
                            queue.Enqueue(new Vector2Int(row, col));
                    }
                }
            }

            return revealed;
        }

        private static bool IsSymbiMinePreviewMine(int row, int col)
        {
            for (int i = 0; i < SymbiMinePreviewMines.Length; i++)
            {
                if (SymbiMinePreviewMines[i].x == row && SymbiMinePreviewMines[i].y == col)
                    return true;
            }

            return false;
        }

        private static int CountSymbiMinePreviewAdjacentMines(int row, int col)
        {
            int count = 0;
            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                {
                    if (r == row && c == col)
                        continue;
                    if (r >= 0 && r < 8 && c >= 0 && c < 8 && IsSymbiMinePreviewMine(r, c))
                        count++;
                }
            }

            return count;
        }

        private static Color GetSymbiMinePreviewNumberColor(int count)
        {
            switch (count)
            {
                case 1:
                    return new Color(0.36f, 0.74f, 1f, 1f);
                case 2:
                    return new Color(0.35f, 1f, 0.58f, 1f);
                case 3:
                    return new Color(1f, 0.36f, 0.34f, 1f);
                default:
                    return Gold;
            }
        }

        private void ClearPreviewToTransparent()
        {
            for (int i = 0; i < retroGridPreviewCells.Count; i++)
            {
                if (retroGridPreviewCells[i] != null)
                {
                    retroGridPreviewCells[i].sprite = null;
                    retroGridPreviewCells[i].color = new Color(0f, 0f, 0f, 0f);
                    retroGridPreviewCells[i].preserveAspect = false;
                }
                if (i < retroGridPreviewLabels.Count && retroGridPreviewLabels[i] != null)
                    retroGridPreviewLabels[i].text = "";
            }
        }

        private void FillPreviewSquare(int topRow, int size, Color color)
        {
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                    PaintPreviewCell(topRow + row, col, color);
            }
        }

        private void FillPreviewRect(int topRow, int leftCol, int rows, int cols, Color color)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                    PaintPreviewCell(topRow + row, leftCol + col, color);
            }
        }

        private void PaintRetroPreviewOBlock(int row, int col, Color color)
        {
            PaintPreviewCell(row, col, color);
            PaintPreviewCell(row, col + 1, color);
            PaintPreviewCell(row + 1, col, color);
            PaintPreviewCell(row + 1, col + 1, color);
        }

        private void PaintRetroPreviewCell(int row, int col, Color color)
        {
            PaintPreviewCell(row, col, color);
        }

        private void PaintPreviewCell(int row, int col, Color color)
        {
            const int cols = 8;
            const int rows = 14;
            if (row < 0 || row >= rows || col < 0 || col >= cols)
                return;

            int index = row * cols + col;
            if (index >= 0 && index < retroGridPreviewCells.Count && retroGridPreviewCells[index] != null)
            {
                retroGridPreviewCells[index].sprite = null;
                retroGridPreviewCells[index].color = color;
                retroGridPreviewCells[index].preserveAspect = false;
                if (index < retroGridPreviewLabels.Count && retroGridPreviewLabels[index] != null)
                    retroGridPreviewLabels[index].text = "";
            }
        }

        private void PaintPreviewLabel(int row, int col, string text, Color color)
        {
            const int cols = 8;
            const int rows = 14;
            if (row < 0 || row >= rows || col < 0 || col >= cols)
                return;

            int index = row * cols + col;
            if (index >= 0 && index < retroGridPreviewLabels.Count && retroGridPreviewLabels[index] != null)
            {
                retroGridPreviewLabels[index].text = text;
                retroGridPreviewLabels[index].color = color;
            }
        }

        private void PaintPreviewMine(int row, int col)
        {
            const int cols = 8;
            const int rows = 14;
            if (row < 0 || row >= rows || col < 0 || col >= cols)
                return;

            int index = row * cols + col;
            if (index < 0 || index >= retroGridPreviewCells.Count || retroGridPreviewCells[index] == null)
                return;

            Image cell = retroGridPreviewCells[index];
            Sprite mineSprite = GetSymbiMineMineSprite();
            if (mineSprite != null)
            {
                cell.sprite = mineSprite;
                cell.color = Color.white;
                cell.preserveAspect = true;
            }
            else
            {
                cell.sprite = null;
                cell.color = Gold;
            }

            if (index < retroGridPreviewLabels.Count && retroGridPreviewLabels[index] != null)
                retroGridPreviewLabels[index].text = "";
        }

        private void CloseModePreview()
        {
            if (!modePreviewOpen || modePreviewAnimating)
                return;

            if (modeSelectionRoutine != null)
                StopCoroutine(modeSelectionRoutine);
            if (modeControlsOverlay != null)
                modeControlsOverlay.SetActive(false);
            modeSelectionRoutine = StartCoroutine(AnimateModePreviewBack());
        }

        private IEnumerator AnimateModePreviewBack()
        {
            modePreviewAnimating = true;
            if (modePreviewBackButton != null)
                modePreviewBackButton.gameObject.SetActive(false);
            if (modePreviewStartButton != null)
                modePreviewStartButton.gameObject.SetActive(false);
            if (modePreviewDetailsPanel != null)
                modePreviewDetailsPanel.gameObject.SetActive(false);

            if (retroGridPreviewRoutine != null)
            {
                StopCoroutine(retroGridPreviewRoutine);
                retroGridPreviewRoutine = null;
            }

            CanvasGroup previewGroup = modePreviewWindow != null ? modePreviewWindow.GetComponent<CanvasGroup>() : null;
            RectTransform retro = retroGridModeButton != null ? retroGridModeButton.GetComponent<RectTransform>() : null;
            RectTransform classic = classicGridModeButton != null ? classicGridModeButton.GetComponent<RectTransform>() : null;
            RectTransform mine = symbiMineModeButton != null ? symbiMineModeButton.GetComponent<RectTransform>() : null;
            Vector2 retroStart = retro != null ? retro.anchorMin : new Vector2(0.5f, 0.555f);
            Vector2 classicStart = classic != null ? classic.anchorMin : new Vector2(0.5f, 0.445f);
            Vector2 mineStart = mine != null ? mine.anchorMin : new Vector2(0.5f, 0.335f);
            Vector2 retroMaxStart = retro != null ? retro.anchorMax : retroStart;
            Vector2 classicMaxStart = classic != null ? classic.anchorMax : classicStart;
            Vector2 mineMaxStart = mine != null ? mine.anchorMax : mineStart;
            Vector2 titleMinStart = symbiGridMenuLogo != null ? symbiGridMenuLogo.anchorMin : new Vector2(0.01f, 1.05f);
            Vector2 titleMaxStart = symbiGridMenuLogo != null ? symbiGridMenuLogo.anchorMax : new Vector2(0.99f, 1.21f);
            if (modeSubtitle != null)
            {
                modeSubtitle.gameObject.SetActive(true);
                Color color = modeSubtitle.color;
                modeSubtitle.color = new Color(color.r, color.g, color.b, 0f);
            }

            const float duration = 0.42f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                MoveModeButton(retro, retroStart, retroMaxStart, new Vector2(0.5f, 0.555f), new Vector2(0.5f, 0.555f), eased);
                MoveModeButton(classic, classicStart, classicMaxStart, new Vector2(0.5f, 0.445f), new Vector2(0.5f, 0.445f), eased);
                MoveModeButton(mine, mineStart, mineMaxStart, new Vector2(0.5f, 0.335f), new Vector2(0.5f, 0.335f), eased);
                if (symbiGridMenuLogo != null)
                {
                    symbiGridMenuLogo.anchorMin = Vector2.LerpUnclamped(titleMinStart, new Vector2(0.01f, 0.855f), eased);
                    symbiGridMenuLogo.anchorMax = Vector2.LerpUnclamped(titleMaxStart, new Vector2(0.99f, 1.015f), eased);
                    symbiGridMenuLogo.offsetMin = Vector2.zero;
                    symbiGridMenuLogo.offsetMax = Vector2.zero;
                }
                if (previewGroup != null)
                    previewGroup.alpha = Mathf.Lerp(1f, 0f, eased);
                if (modePreviewWindow != null)
                    modePreviewWindow.localScale = Vector3.one * Mathf.Lerp(1f, 0.90f, eased);
                if (modeSubtitle != null)
                {
                    Color color = modeSubtitle.color;
                    modeSubtitle.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, 1f, eased));
                }

                yield return null;
            }

            ResetModeOverlayState();
        }

        private void StartSelectedModePreview()
        {
            if (modePreviewAnimating || modeTransitionRunning)
                return;

            if (selectedModePreview == SymbiGridMode.SymbiMine && !SymbiMineAccessEnabled)
            {
                ShowSymbiMineUnavailableNotice();
                return;
            }

            switch (selectedModePreview)
            {
                case SymbiGridMode.Tetris:
                    StartTetrisMode();
                    break;
                case SymbiGridMode.SymbiMine:
                    StartSymbiMineMode();
                    break;
                default:
                    StartClassicMode();
                    break;
            }
        }

        private static void SetLogoAnchor(RectTransform rect, Vector2 anchor)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = ModeLogoButtonSize;
        }

        private static void SetLogoAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void MoveModeButton(RectTransform rect, Vector2 start, Vector2 end, float eased)
        {
            SetLogoAnchor(rect, Vector2.LerpUnclamped(start, end, eased));
        }

        private static void MoveModeButton(RectTransform rect, Vector2 startMin, Vector2 startMax, Vector2 endMin, Vector2 endMax, float eased)
        {
            SetLogoAnchors(
                rect,
                Vector2.LerpUnclamped(startMin, endMin, eased),
                Vector2.LerpUnclamped(startMax, endMax, eased));
        }

        private static Vector2 GetSelectedModeLogoAnchor()
        {
            return new Vector2(0.5f, 0.865f);
        }

        private RectTransform GetModeButtonRect(SymbiGridMode mode)
        {
            switch (mode)
            {
                case SymbiGridMode.Tetris:
                    return retroGridModeButton != null ? retroGridModeButton.GetComponent<RectTransform>() : null;
                case SymbiGridMode.SymbiMine:
                    return symbiMineModeButton != null ? symbiMineModeButton.GetComponent<RectTransform>() : null;
                default:
                    return classicGridModeButton != null ? classicGridModeButton.GetComponent<RectTransform>() : null;
            }
        }

        private void SetModeButtonsInteractable(bool interactable)
        {
            if (retroGridModeButton != null)
                retroGridModeButton.interactable = interactable;
            if (classicGridModeButton != null)
                classicGridModeButton.interactable = interactable;
            if (symbiMineModeButton != null)
                symbiMineModeButton.interactable = interactable;
        }

        private void RefreshModePreviewDetails(SymbiGridMode mode)
        {
            int modeBestScore = PlayerPrefs.GetInt(GetModeBestScoreKey(mode), 0);
            int modeLastScore = PlayerPrefs.GetInt(GetModeLastScoreKey(mode), 0);

            if (modePreviewDescriptionText != null)
                modePreviewDescriptionText.text = GetModePreviewDescription(mode);
            if (modePreviewBestText != null)
                modePreviewBestText.text = modeBestScore.ToString();
            if (modePreviewLastText != null)
                modePreviewLastText.text = modeLastScore.ToString();
        }

        private void RefreshLocalizedText()
        {
            if (scoreLabelText != null)
                scoreLabelText.text = GameLocalization.Text("symbigrid.score");
            if (settingsTitleText != null)
                settingsTitleText.text = GameLocalization.Text("symbigrid.settings");
            if (symbiMineDifficultySubtitleText != null)
                symbiMineDifficultySubtitleText.text = GameLocalization.Text("symbigrid.minefield_choose");

            SetButtonText(settingsMenuButton, GameLocalization.Text("symbigrid.menu"));
            SetButtonText(rerollAdButton, GameLocalization.Text("symbigrid.reroll_ad"));
            SetButtonText(tetrisRotateButton, GameLocalization.Text("symbigrid.rotate"));
            SetButtonText(tetrisDropButton, GameLocalization.Text("symbigrid.drop"));
            SetButtonText(settingsModeButton, GameLocalization.Text("symbigrid.mode"));
            SetButtonText(settingsRestartButton, GameLocalization.Text("symbigrid.new_run"));
            SetButtonText(settingsBackButton, GameLocalization.Text("symbigrid.back_to_platform"));
            SetButtonText(settingsCloseButton, GameLocalization.Text("symbigrid.close"));
            SetButtonText(symbiMineBeginnerButton, GameLocalization.Text("symbigrid.difficulty.beginner"));
            SetButtonText(symbiMineIntermediateButton, GameLocalization.Text("symbigrid.difficulty.intermediate"));
            SetButtonText(symbiMineExpertButton, GameLocalization.Text("symbigrid.difficulty.expert"));
            SetButtonText(symbiMineDifficultyBackButton, GameLocalization.Text("symbigrid.back"));
            SetButtonText(modePlatformBackButton, GameLocalization.Text("symbigrid.back_to_platform"));
            SetButtonText(modePreviewBackButton, GameLocalization.Text("symbigrid.back"));
            SetButtonText(modePreviewStartButton, GameLocalization.Text("symbigrid.start"));
            SetButtonText(modePreviewControlsButton, GameLocalization.Text("symbigrid.controls"));
            SetButtonText(modeControlsCloseButton, GameLocalization.Text("symbigrid.close"));
            SetButtonText(symbiMineUnavailableCloseButton, GameLocalization.Text("symbigrid.back"));
            if (symbiMineUnavailableBadgeText != null)
                symbiMineUnavailableBadgeText.text = GameLocalization.Text("symbigrid.mine_unavailable.status");
            if (symbiMineUnavailableStatusText != null)
                symbiMineUnavailableStatusText.text = GameLocalization.Text("symbigrid.mine_unavailable.status");
            if (symbiMineUnavailableBodyText != null)
                symbiMineUnavailableBodyText.text = GameLocalization.Text("symbigrid.mine_unavailable.body");
            RefreshSymbiMineFlagModeUi();
            RefreshTetrisControlModeUi();

            if (modeControlsTitleText != null)
                modeControlsTitleText.text = GameLocalization.Text("symbigrid.controls");
            if (modeControlsBodyText != null && modeControlsOverlay != null && modeControlsOverlay.activeSelf)
                modeControlsBodyText.text = GetModeControlsDescription(selectedModePreview);
            if (modePreviewDescriptionText != null)
                RefreshModePreviewDetails(selectedModePreview);
        }

        private void LoadModeScores()
        {
            bestScore = PlayerPrefs.GetInt(GetModeBestScoreKey(currentMode), 0);
            lastScore = PlayerPrefs.GetInt(GetModeLastScoreKey(currentMode), 0);
        }

        private void SaveModeBestScore()
        {
            PlayerPrefs.SetInt(GetModeBestScoreKey(currentMode), bestScore);
        }

        private void SaveModeLastScore()
        {
            PlayerPrefs.SetInt(GetModeLastScoreKey(currentMode), lastScore);
        }

        private static string GetModeBestScoreKey(SymbiGridMode mode)
        {
            return "SymbiGrid." + GetModeStorageName(mode) + ".BestScore";
        }

        private static string GetModeLastScoreKey(SymbiGridMode mode)
        {
            return "SymbiGrid." + GetModeStorageName(mode) + ".LastScore";
        }

        private static string GetModeStorageName(SymbiGridMode mode)
        {
            switch (mode)
            {
                case SymbiGridMode.Classic:
                    return "ClassicGrid";
                case SymbiGridMode.SymbiMine:
                    return "SymbiMine";
                default:
                    return "RetroGrid";
            }
        }

        private void OpenModeControlsOverlay()
        {
            if (modeControlsOverlay == null)
                return;

            if (modeControlsTitleText != null)
                modeControlsTitleText.text = GameLocalization.Text("symbigrid.controls");
            if (modeControlsBodyText != null)
                modeControlsBodyText.text = GetModeControlsDescription(selectedModePreview);

            modeControlsOverlay.SetActive(true);
            modeControlsOverlay.transform.SetAsLastSibling();
        }

        private void CloseModeControlsOverlay()
        {
            if (modeControlsOverlay != null)
                modeControlsOverlay.SetActive(false);
        }

        private static string GetModeTitle(SymbiGridMode mode)
        {
            switch (mode)
            {
                case SymbiGridMode.Classic:
                    return "CLASSICGRID";
                case SymbiGridMode.SymbiMine:
                    return "SYMBIMINE";
                default:
                    return "RETROGRID";
            }
        }

        private static string GetModePreviewDescription(SymbiGridMode mode)
        {
            switch (mode)
            {
                case SymbiGridMode.Classic:
                    return GameLocalization.Text("symbigrid.preview.classic");
                case SymbiGridMode.SymbiMine:
                    return GameLocalization.Text("symbigrid.preview.mine");
                default:
                    return GameLocalization.Text("symbigrid.preview.retro");
            }
        }

        private static string GetModeControlsDescription(SymbiGridMode mode)
        {
            switch (mode)
            {
                case SymbiGridMode.Classic:
                    return GameLocalization.Text("symbigrid.controls.classic");
                case SymbiGridMode.SymbiMine:
                    return GameLocalization.Text("symbigrid.controls.mine");
                default:
                    return GameLocalization.Text("symbigrid.controls.retro");
            }
        }

        private void ResetModeOverlayState()
        {
            modePreviewOpen = false;
            modePreviewAnimating = false;
            selectedModePreview = SymbiGridMode.Tetris;
            if (modeSelectionRoutine != null)
            {
                StopCoroutine(modeSelectionRoutine);
                modeSelectionRoutine = null;
            }
            if (retroGridPreviewRoutine != null)
            {
                StopCoroutine(retroGridPreviewRoutine);
                retroGridPreviewRoutine = null;
            }

            SetLogoAnchor(retroGridModeButton != null ? retroGridModeButton.GetComponent<RectTransform>() : null, new Vector2(0.5f, 0.555f));
            SetLogoAnchor(classicGridModeButton != null ? classicGridModeButton.GetComponent<RectTransform>() : null, new Vector2(0.5f, 0.445f));
            SetLogoAnchor(symbiMineModeButton != null ? symbiMineModeButton.GetComponent<RectTransform>() : null, new Vector2(0.5f, 0.335f));
            if (symbiGridMenuLogo != null)
            {
                symbiGridMenuLogo.anchorMin = new Vector2(0.01f, 0.855f);
                symbiGridMenuLogo.anchorMax = new Vector2(0.99f, 1.015f);
                symbiGridMenuLogo.offsetMin = Vector2.zero;
                symbiGridMenuLogo.offsetMax = Vector2.zero;
            }

            SetModeButtonsInteractable(true);
            if (modeSubtitle != null)
            {
                modeSubtitle.gameObject.SetActive(true);
                Color color = modeSubtitle.color;
                modeSubtitle.color = new Color(color.r, color.g, color.b, 1f);
            }
            if (modePreviewWindow != null)
            {
                modePreviewWindow.gameObject.SetActive(false);
                modePreviewWindow.localScale = Vector3.one;
                CanvasGroup group = modePreviewWindow.GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 0f;
            }
            if (modePreviewBackButton != null)
                modePreviewBackButton.gameObject.SetActive(false);
            if (modePreviewStartButton != null)
                modePreviewStartButton.gameObject.SetActive(false);
            if (modePlatformBackButton != null)
                modePlatformBackButton.gameObject.SetActive(true);
            if (modePreviewDetailsPanel != null)
                modePreviewDetailsPanel.gameObject.SetActive(false);
            if (modeControlsOverlay != null)
                modeControlsOverlay.SetActive(false);
        }

        private void NewGame()
        {
            StopAllCoroutines();
            Array.Clear(board, 0, board.Length);
            Array.Clear(boardColors, 0, boardColors.Length);
            ResetSymbiMineState();
            score = 0;
            combo = 0;
            linesCleared = 0;
            selectedPieceIndex = -1;
            draggingPiece = false;
            resolving = false;
            gameOver = false;
            levelComplete = false;
            secondChanceUsed = false;
            rerollRewardedAdRequestInProgress = false;
            symbiMineRewardedAdRequestInProgress = false;
            ConfigureModeTargets();
            ConfigureRunRandom();
            ApplyBoardLayout();
            ApplyModeBoardSetup();
            HideDragGhost();
            LoadModeScores();
            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(false);
            if (symbiMineResultTapLayer != null)
                symbiMineResultTapLayer.SetActive(false);
            if (settingsOverlay != null)
                settingsOverlay.SetActive(false);
            if (symbiMineDifficultyOverlay != null)
                symbiMineDifficultyOverlay.SetActive(false);
            if (modeOverlay != null)
                modeOverlay.SetActive(false);
            if (piecePanel != null)
                piecePanel.gameObject.SetActive(currentMode == SymbiGridMode.Classic);
            RefreshSymbiMineFlagModeUi();
            RefreshTetrisControlModeUi();

            if (currentMode == SymbiGridMode.Tetris)
                StartTetrisRun();
            else if (currentMode == SymbiGridMode.Classic)
                GeneratePieces();
            RefreshBoard();
            RefreshPieces();
            if (currentMode == SymbiGridMode.Classic)
                AnimatePendingPieceSpawn();
            RefreshScore();
            RefreshGoalHud();
            SetStatus(BuildModeStatusText());
        }

        private void ConfigureRunRandom()
        {
            int seed = Environment.TickCount;
            if (currentMode == SymbiGridMode.SymbiMine)
                seed = 7919 + currentSymbiMineLevel * 104729;

            runRandom = new System.Random(seed);
        }

        private void StartClassicMode()
        {
            if (modeTransitionRunning)
                return;

            PlaySound(closeClip, 0.50f, 1.04f);
            PlayLocalModeTransition(() =>
            {
                currentMode = SymbiGridMode.Classic;
                NewGame();
            });
        }

        private void StartSymbiMineMode()
        {
            if (!SymbiMineAccessEnabled)
            {
                ShowSymbiMineUnavailableNotice();
                return;
            }

            PlaySound(openClip, 0.54f, 1.02f);
            if (modeOverlay != null)
                modeOverlay.SetActive(false);
            if (settingsOverlay != null)
                settingsOverlay.SetActive(false);
            if (symbiMineDifficultyOverlay != null)
            {
                symbiMineDifficultyOverlay.transform.SetAsLastSibling();
                symbiMineDifficultyOverlay.SetActive(true);
            }
        }

        private void StartSymbiMineDifficulty(SymbiMineDifficulty difficulty)
        {
            if (!SymbiMineAccessEnabled)
            {
                ShowSymbiMineUnavailableNotice();
                return;
            }

            if (modeTransitionRunning)
                return;

            PlaySound(closeClip, 0.50f, 1.04f);
            PlayLocalModeTransition(() =>
            {
                currentMode = SymbiGridMode.SymbiMine;
                ConfigureSymbiMineDifficulty(difficulty);
                NewGame();
            });
        }

        private void ConfigureSymbiMineDifficulty(SymbiMineDifficulty difficulty)
        {
            switch (difficulty)
            {
                case SymbiMineDifficulty.Intermediate:
                    currentSymbiMineLevel = 2;
                    symbiMineRows = SymbiMineIntermediateRows;
                    symbiMineCols = SymbiMineIntermediateCols;
                    symbiMineTotalMines = SymbiMineIntermediateMines;
                    break;
                case SymbiMineDifficulty.Expert:
                    currentSymbiMineLevel = 3;
                    symbiMineRows = SymbiMineExpertRows;
                    symbiMineCols = SymbiMineExpertCols;
                    symbiMineTotalMines = SymbiMineExpertMines;
                    break;
                default:
                    currentSymbiMineLevel = 1;
                    symbiMineRows = SymbiMineBeginnerRows;
                    symbiMineCols = SymbiMineBeginnerCols;
                    symbiMineTotalMines = SymbiMineBeginnerMines;
                    break;
            }
        }

        private void BackToModeOverlayFromSymbiMineDifficulty()
        {
            PlaySound(closeClip, 0.50f, 1f);
            if (symbiMineDifficultyOverlay != null)
                symbiMineDifficultyOverlay.SetActive(false);
            if (modeOverlay != null)
            {
                if (symbiMineDifficultyOverlay != null)
                    symbiMineDifficultyOverlay.SetActive(false);
                ResetModeOverlayState();
                modeOverlay.transform.SetAsLastSibling();
                modeOverlay.SetActive(true);
            }
        }

        private void StartTetrisMode()
        {
            if (modeTransitionRunning)
                return;

            PlaySound(closeClip, 0.50f, 1.04f);
            PlayLocalModeTransition(() =>
            {
                currentMode = SymbiGridMode.Tetris;
                NewGame();
            });
        }

        private void OpenModeOverlay()
        {
            if (resolving)
                return;

            PlaySound(openClip, 0.62f, 1f);
            HideDragGhost();
            selectedPieceIndex = -1;
            draggingPiece = false;
            RefreshBoard();
            RefreshPieces();
            if (modeOverlay != null)
            {
                ResetModeOverlayState();
                modeOverlay.transform.SetAsLastSibling();
                modeOverlay.SetActive(true);
            }
        }

        private void OpenModeOverlayWithTransition()
        {
            if (modeTransitionRunning)
                return;

            PlayLocalModeTransition(OpenModeOverlay);
        }

        private void PlayLocalModeTransition(Action onCovered)
        {
            if (modeTransitionRunning || exitingScene)
                return;

            modeTransitionRunning = true;
            bool started = MahjongGame.SymbiGridSceneTransitionFx.PlayLocal(() =>
            {
                try
                {
                    onCovered?.Invoke();
                }
                finally
                {
                    modeTransitionRunning = false;
                }
            }, ModeTransitionHoldSeconds);

            if (started)
                return;

            try
            {
                onCovered?.Invoke();
            }
            finally
            {
                modeTransitionRunning = false;
            }
        }

        private sealed class TetrisGestureInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public SymbiGridBootstrap Owner;
            private Vector2 startPosition;
            private float startTime;
            private int lastHorizontalStep;

            public void OnPointerDown(PointerEventData eventData)
            {
                startPosition = eventData.position;
                startTime = Time.unscaledTime;
                lastHorizontalStep = 0;
                Owner?.ResetTetrisHorizontalMoveDelay();
                Owner?.SetTetrisSoftDropActive(false);
            }

            public void OnDrag(PointerEventData eventData)
            {
                Owner?.HandleTetrisGestureDrag(startPosition, eventData.position, ref lastHorizontalStep);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                Owner?.HandleTetrisGestureRelease(startPosition, eventData.position, Time.unscaledTime - startTime, lastHorizontalStep);
                Owner?.SetTetrisSoftDropActive(false);
            }
        }

        private void OpenSettingsOverlay()
        {
            if (resolving)
                return;

            PlaySound(openClip, 0.62f, 1f);
            HideDragGhost();
            selectedPieceIndex = -1;
            draggingPiece = false;
            RefreshBoard();
            RefreshPieces();
            if (symbiMineDifficultyOverlay != null)
                symbiMineDifficultyOverlay.SetActive(false);
            if (settingsOverlay != null)
                settingsOverlay.SetActive(true);
        }

        private void CloseSettingsOverlay()
        {
            PlaySound(closeClip, 0.58f, 1f);
            if (settingsOverlay != null)
                settingsOverlay.SetActive(false);
        }

        private void HandleRerollRewardedAd()
        {
            if (resolving || gameOver || currentMode != SymbiGridMode.Classic || rerollRewardedAdRequestInProgress)
                return;

            MonetizationService monetization = MonetizationService.Ensure();
            RewardedAdAvailability availability = monetization.GetRewardedAdAvailability(MonetizationService.SymbiGridRerollRewardedPlacementId);
            if (!availability.IsReady)
            {
                string messageKey = string.IsNullOrWhiteSpace(availability.Message) ? "shop.ad_not_ready" : availability.Message;
                SetStatus(GameLocalization.Text(messageKey));
                return;
            }

            rerollRewardedAdRequestInProgress = true;
            SetRerollButtonInteractable(false);
            SetStatus(GameLocalization.Text("shop.ad_loading"));

            monetization.ShowRewardedAd(MonetizationService.SymbiGridRerollRewardedPlacementId, result =>
            {
                rerollRewardedAdRequestInProgress = false;
                SetRerollButtonInteractable(true);

                if (!result.IsCompleted)
                {
                    SetStatus(result.State == RewardedAdState.Skipped
                        ? GameLocalization.Text("symbigrid.reroll_ad_incomplete")
                        : GameLocalization.Text("symbigrid.reroll_ad_failed"));
                    return;
                }

                if (resolving || gameOver || currentMode != SymbiGridMode.Classic)
                    return;

                CompleteRewardedReroll();
            });
        }

        private void CompleteRewardedReroll()
        {
            selectedPieceIndex = -1;
            draggingPiece = false;
            HideDragGhost();
            RerollAvailablePieces();
            RefreshBoard();
            RefreshPieces();
            AnimatePendingPieceSpawn();
            RefreshGoalHud();
            PlaySound(newPieceClip, 0.56f, 1.04f);

            if (!AnyRemainingPieceFits())
            {
                gameOver = true;
                ShowOutcome("GAME OVER", "No remaining piece fits the board.", "PLAY AGAIN");
                return;
            }

            SetStatus(GameLocalization.Text("symbigrid.reroll_complete"));
        }

        private void SetRerollButtonInteractable(bool interactable)
        {
            if (rerollAdButton != null)
                rerollAdButton.interactable = interactable;
        }

        private void OpenModeFromSettings()
        {
            CloseSettingsOverlay();
            OpenModeOverlayWithTransition();
        }

        private void RestartFromSettings()
        {
            CloseSettingsOverlay();
            NewGame();
        }

        private void ConfigureModeTargets()
        {
            hasMoveLimit = false;
            targetScore = 0;
            targetLines = 0;
            movesLeft = 0;

            if (currentMode == SymbiGridMode.SymbiMine)
            {
                targetScore = symbiMineRows * symbiMineCols - symbiMineTotalMines;
                targetLines = symbiMineTotalMines;
                return;
            }

            if (currentMode == SymbiGridMode.Tetris)
                targetLines = 40;
        }

        private void ApplyModeBoardSetup()
        {
        }

        private void ResetSymbiMineState()
        {
            Array.Clear(mineCells, 0, mineCells.Length);
            Array.Clear(revealedMineCells, 0, revealedMineCells.Length);
            Array.Clear(flaggedMineCells, 0, flaggedMineCells.Length);
            Array.Clear(adjacentMineCounts, 0, adjacentMineCounts.Length);
            symbiMineFlags = 0;
            symbiMineRevealedSafe = 0;
            symbiMineGenerated = false;
            symbiMineExploded = false;
            symbiMineFlagMode = false;
            symbiMineDetonatedRow = -1;
            symbiMineDetonatedCol = -1;
            symbiMineOutcomePending = false;
            symbiMineSecondChanceOfferActive = false;
            pendingOutcomeTitle = null;
            pendingOutcomeBody = null;
            pendingOutcomeAction = null;
            symbiMineRewardedAdRequestInProgress = false;
        }

        private void GenerateSymbiMineBoard(int safeRow, int safeCol)
        {
            Array.Clear(mineCells, 0, mineCells.Length);
            Array.Clear(adjacentMineCounts, 0, adjacentMineCounts.Length);

            int placed = 0;
            while (placed < symbiMineTotalMines)
            {
                int row = runRandom.Next(ActiveRows);
                int col = runRandom.Next(ActiveCols);
                if (mineCells[row, col] || IsProtectedSymbiMineStart(row, col, safeRow, safeCol))
                    continue;

                mineCells[row, col] = true;
                placed++;
            }

            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                    adjacentMineCounts[row, col] = CountAdjacentMines(row, col);
            }

            symbiMineGenerated = true;
        }

        private bool IsProtectedSymbiMineStart(int row, int col, int safeRow, int safeCol)
        {
            if (safeRow < 0 || safeCol < 0)
                return false;

            return Mathf.Abs(row - safeRow) <= 1 && Mathf.Abs(col - safeCol) <= 1;
        }

        private int CountAdjacentMines(int row, int col)
        {
            int count = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int r = row + y;
                    int c = col + x;
                    if (r >= 0 && r < ActiveRows && c >= 0 && c < ActiveCols && mineCells[r, c])
                        count++;
                }
            }

            return count;
        }

        private void HandleCellTap(int row, int col, bool flag)
        {
            if (symbiMineOutcomePending)
            {
                ShowPendingSymbiMineOutcome();
                return;
            }

            if (currentMode != SymbiGridMode.SymbiMine || resolving || gameOver)
                return;
            if (row < 0 || row >= ActiveRows || col < 0 || col >= ActiveCols)
                return;

            if (flag || symbiMineFlagMode)
            {
                ToggleSymbiMineFlag(row, col);
                return;
            }

            OpenSymbiMineCell(row, col);
        }

        private void ToggleSymbiMineFlag(int row, int col)
        {
            if (revealedMineCells[row, col])
                return;

            flaggedMineCells[row, col] = !flaggedMineCells[row, col];
            symbiMineFlags += flaggedMineCells[row, col] ? 1 : -1;
            PlaySound(selectClip, 0.45f, flaggedMineCells[row, col] ? 1.06f : 0.94f);
            RefreshBoard();
            RefreshScore();
            RefreshGoalHud();
            SetStatus(flaggedMineCells[row, col] ? "Flag placed." : "Flag removed.");
        }

        private void OpenSymbiMineCell(int row, int col)
        {
            if (flaggedMineCells[row, col])
                return;

            if (!symbiMineGenerated)
                GenerateSymbiMineBoard(row, col);

            if (revealedMineCells[row, col])
            {
                TryChordSymbiMineCell(row, col);
                return;
            }

            if (mineCells[row, col])
            {
                DetonateSymbiMine(row, col, "SymbiMine field exploded. Safe cells opened: " + symbiMineRevealedSafe + "/" + targetScore + ".");
                return;
            }

            int opened = FloodRevealSymbiMine(row, col);
            if (opened <= 0)
                return;

            int gained = opened * 18 + (adjacentMineCounts[row, col] == 0 ? 20 : 0);
            score += gained;
            linesCleared = symbiMineRevealedSafe;
            if (score > bestScore)
            {
                bestScore = score;
                SaveModeBestScore();
            }

            PlaySound(openClip, 0.48f, opened > 1 ? 1.08f : 1f);
            RefreshBoard();
            RefreshScore();
            RefreshGoalHud();
            ShowFloatingText("+" + gained, Ink);

            if (symbiMineRevealedSafe >= targetScore)
            {
                levelComplete = true;
                gameOver = true;
                ShowOutcome("COMPLETE", BuildCompletionBody(), "NEXT");
                return;
            }

            SetStatus(opened > 1 ? "Opened " + opened + " safe cells." : "Safe cell opened.");
        }

        private void TryChordSymbiMineCell(int row, int col)
        {
            int count = adjacentMineCounts[row, col];
            if (count <= 0)
                return;

            int flags = CountAdjacentFlags(row, col);
            if (flags != count)
            {
                SetStatus("Place " + count + " flag" + (count == 1 ? "" : "s") + " around this number to open neighbors.");
                PlaySound(invalidClip, 0.34f, 0.98f);
                return;
            }

            int opened = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int r = row + y;
                    int c = col + x;
                    if (r < 0 || r >= ActiveRows || c < 0 || c >= ActiveCols || flaggedMineCells[r, c] || revealedMineCells[r, c])
                        continue;

                    if (mineCells[r, c])
                    {
                        DetonateSymbiMine(r, c, "One of the surrounding flags was wrong.");
                        return;
                    }

                    opened += FloodRevealSymbiMine(r, c);
                }
            }

            if (opened <= 0)
                return;

            int gained = opened * 18;
            score += gained;
            linesCleared = symbiMineRevealedSafe;
            if (score > bestScore)
            {
                bestScore = score;
                SaveModeBestScore();
            }

            PlaySound(openClip, 0.48f, 1.08f);
            RefreshBoard();
            RefreshScore();
            RefreshGoalHud();
            ShowFloatingText("+" + gained, Ink);

            if (symbiMineRevealedSafe >= targetScore)
            {
                levelComplete = true;
                gameOver = true;
                ShowOutcome("COMPLETE", BuildCompletionBody(), "NEXT");
                return;
            }

            SetStatus("Number opened " + opened + " neighbor cells.");
        }

        private void DetonateSymbiMine(int row, int col, string body)
        {
            symbiMineDetonatedRow = row;
            symbiMineDetonatedCol = col;
            revealedMineCells[row, col] = true;
            pendingOutcomeTitle = "MINE HIT";
            pendingOutcomeBody = body;
            pendingOutcomeAction = "RETRY";
            gameOver = true;
            resolving = true;
            PlaySound(symbiMineExplosionClip != null ? symbiMineExplosionClip : gameOverClip, 0.78f, 0.96f);
            RefreshBoard();
            PlaySymbiMineExplosionEffect(row, col);
            StartCoroutine(ShowSymbiMineSecondChanceOfferAfterExplosion());
        }

        private IEnumerator ShowSymbiMineSecondChanceOfferAfterExplosion()
        {
            yield return new WaitForSecondsRealtime(0.42f);
            resolving = false;
            if (secondChanceUsed)
            {
                DeclineSymbiMineSecondChance();
                yield break;
            }

            symbiMineSecondChanceOfferActive = true;
            ShowOutcome("SECOND CHANCE", "Mine detonated. Do you want a second chance?", "RESULTS", false, false);
            SetStatus("Second chance available.");
        }

        private void ShowPendingSymbiMineOutcome()
        {
            if (!symbiMineOutcomePending)
                return;

            symbiMineOutcomePending = false;
            if (symbiMineResultTapLayer != null)
                symbiMineResultTapLayer.SetActive(false);

            ShowOutcome(
                string.IsNullOrEmpty(pendingOutcomeTitle) ? "MINE HIT" : pendingOutcomeTitle,
                string.IsNullOrEmpty(pendingOutcomeBody) ? "Minefield exploded." : pendingOutcomeBody,
                string.IsNullOrEmpty(pendingOutcomeAction) ? "RETRY" : pendingOutcomeAction);
            pendingOutcomeTitle = null;
            pendingOutcomeBody = null;
            pendingOutcomeAction = null;
        }

        private void GrantSymbiMineSecondChance()
        {
            symbiMineSecondChanceOfferActive = false;
            symbiMineOutcomePending = false;
            secondChanceUsed = true;
            gameOver = false;
            resolving = false;

            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(false);
            if (symbiMineResultTapLayer != null)
                symbiMineResultTapLayer.SetActive(false);

            if (symbiMineDetonatedRow >= 0 && symbiMineDetonatedRow < ActiveRows && symbiMineDetonatedCol >= 0 && symbiMineDetonatedCol < ActiveCols)
            {
                if (!flaggedMineCells[symbiMineDetonatedRow, symbiMineDetonatedCol])
                    symbiMineFlags++;

                flaggedMineCells[symbiMineDetonatedRow, symbiMineDetonatedCol] = true;
                revealedMineCells[symbiMineDetonatedRow, symbiMineDetonatedCol] = false;
            }

            pendingOutcomeTitle = null;
            pendingOutcomeBody = null;
            pendingOutcomeAction = null;
            RefreshBoard();
            RefreshScore();
            RefreshGoalHud();
            PlaySound(newPieceClip, 0.58f, 1.02f);
            SetStatus("Second chance granted. The detonated mine is marked.");
        }

        private void DeclineSymbiMineSecondChance()
        {
            symbiMineSecondChanceOfferActive = false;
            secondChanceUsed = true;
            symbiMineExploded = true;
            RevealAllSymbiMines();
            RefreshBoard();
            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(false);
            symbiMineOutcomePending = true;
            if (symbiMineResultTapLayer != null)
                symbiMineResultTapLayer.SetActive(true);
            SetStatus("Minefield revealed. Tap anywhere for results.");
        }

        private int CountAdjacentFlags(int row, int col)
        {
            int count = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int r = row + y;
                    int c = col + x;
                    if (r >= 0 && r < ActiveRows && c >= 0 && c < ActiveCols && flaggedMineCells[r, c])
                        count++;
                }
            }

            return count;
        }

        private int FloodRevealSymbiMine(int startRow, int startCol)
        {
            int opened = 0;
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startCol, startRow));

            while (queue.Count > 0)
            {
                Vector2Int point = queue.Dequeue();
                int row = point.y;
                int col = point.x;
                if (row < 0 || row >= ActiveRows || col < 0 || col >= ActiveCols)
                    continue;
                if (revealedMineCells[row, col] || flaggedMineCells[row, col] || mineCells[row, col])
                    continue;

                revealedMineCells[row, col] = true;
                symbiMineRevealedSafe++;
                opened++;

                if (adjacentMineCounts[row, col] != 0)
                    continue;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        queue.Enqueue(new Vector2Int(col + x, row + y));
                    }
                }
            }

            return opened;
        }

        private void RevealAllSymbiMines()
        {
            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    if (mineCells[row, col])
                        revealedMineCells[row, col] = true;
                }
            }
        }

        private void PlaySymbiMineExplosionEffect(int row, int col)
        {
            if (row < 0 || row >= ActiveRows || col < 0 || col >= ActiveCols)
                return;

            Image cell = cellImages[row, col];
            if (cell == null)
                return;

            Image fx = CreateImage(cell.transform, "SymbiMineExplosionFx", Color.white);
            fx.raycastTarget = false;
            fx.preserveAspect = true;
            RectTransform rect = fx.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = cell.rectTransform.rect.size * 1.75f;
            fx.transform.SetAsLastSibling();
            StartCoroutine(AnimateSymbiMineExplosion(fx));
        }

        private IEnumerator AnimateSymbiMineExplosion(Image fx)
        {
            Sprite[] frames = GetSymbiMineExplosionSprites();
            const float frameSeconds = 0.035f;
            if (frames != null && frames.Length > 0)
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (fx == null)
                        yield break;

                    fx.sprite = frames[i];
                    fx.color = Color.white;
                    fx.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.22f, i / Mathf.Max(1f, frames.Length - 1f));
                    yield return new WaitForSecondsRealtime(frameSeconds);
                }
            }

            if (fx == null)
                yield break;

            float started = Time.unscaledTime;
            const float fadeSeconds = 0.16f;
            while (Time.unscaledTime - started < fadeSeconds)
            {
                if (fx == null)
                    yield break;

                float t = (Time.unscaledTime - started) / fadeSeconds;
                fx.color = new Color(1f, 0.72f, 0.32f, 1f - t);
                fx.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.18f, 1.48f, t);
                yield return null;
            }

            if (fx != null)
                Destroy(fx.gameObject);
        }

        private string BuildModeStatusText()
        {
            if (currentMode == SymbiGridMode.SymbiMine)
                return "Tap to reveal. Use FLAG mode, hold, or right-click to mark mines.";
            if (currentMode == SymbiGridMode.Tetris)
                return tetrisControlMode == TetrisControlMode.Gesture
                    ? "RetroGrid gesture mode: swipe to move, rotate, or drop."
                    : "RetroGrid joystick mode: use the on-screen buttons.";

            return "Drag a puzzle shape onto a glowing place.";
        }

        private void GeneratePieces()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceView piece = pieces[i];
                piece.Shape = PickFairShape(i);
                piece.Used = false;
            }

            EnsureAtLeastOneGeneratedPieceFits();
            piecesSpawnPending = true;
        }

        private Shape PickFairShape(int slot)
        {
            List<Shape> fitting = new List<Shape>();
            for (int i = 0; i < ShapeLibrary.Length; i++)
            {
                Shape candidate = ShapeLibrary[i];
                if (AnyPlacementFits(candidate))
                    fitting.Add(candidate);
            }

            if (fitting.Count == 0)
                return ShapeLibrary[runRandom.Next(ShapeLibrary.Length)];

            int softLimit = currentMode == SymbiGridMode.Classic ? fitting.Count : Mathf.Max(1, Mathf.Min(fitting.Count, 12));
            return fitting[(runRandom.Next(softLimit) + slot) % fitting.Count];
        }

        private void EnsureAtLeastOneGeneratedPieceFits()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Used && AnyPlacementFits(pieces[i].Shape))
                    return;
            }

            for (int i = 0; i < ShapeLibrary.Length; i++)
            {
                Shape candidate = ShapeLibrary[(runRandom.Next(ShapeLibrary.Length) + i) % ShapeLibrary.Length];
                if (!AnyPlacementFits(candidate))
                    continue;

                pieces[0].Shape = candidate;
                pieces[0].Used = false;
                return;
            }
        }

        private void RerollAvailablePieces()
        {
            bool hasAvailablePiece = false;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Used)
                {
                    hasAvailablePiece = true;
                    pieces[i].Shape = PickFairShape(i);
                }
            }

            if (!hasAvailablePiece)
            {
                GeneratePieces();
                return;
            }

            EnsureAtLeastOneRemainingPieceFits();
            piecesSpawnPending = true;
        }

        private void EnsureAtLeastOneRemainingPieceFits()
        {
            if (AnyRemainingPieceFits())
                return;

            int targetSlot = -1;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Used)
                {
                    targetSlot = i;
                    break;
                }
            }

            if (targetSlot < 0)
                return;

            for (int i = 0; i < ShapeLibrary.Length; i++)
            {
                Shape candidate = ShapeLibrary[(runRandom.Next(ShapeLibrary.Length) + i) % ShapeLibrary.Length];
                if (!AnyPlacementFits(candidate))
                    continue;

                pieces[targetSlot].Shape = candidate;
                return;
            }
        }

        private void SelectPiece(int index)
        {
            if (currentMode == SymbiGridMode.Tetris || currentMode == SymbiGridMode.SymbiMine)
                return;

            ShowPiecePlacements(index);
        }

        private void TryPlace(int row, int col)
        {
            if (currentMode == SymbiGridMode.Tetris || currentMode == SymbiGridMode.SymbiMine)
                return;

            if (resolving || gameOver)
                return;

            if (selectedPieceIndex < 0 || selectedPieceIndex >= pieces.Count)
            {
                SetStatus("Drag one of the three pieces onto the board.");
                return;
            }

            PieceView piece = pieces[selectedPieceIndex];
            if (piece.Used)
                return;

            if (!CanPlace(piece.Shape, row, col))
            {
                PlaySound(invalidClip, 0.72f, 0.96f);
                SetStatus("That shape does not fit there.");
                Preview(row, col);
                return;
            }

            PlaySound(placeClip, 0.58f, UnityEngine.Random.Range(0.98f, 1.04f));
            List<Vector2Int> placedCells = new List<Vector2Int>(piece.Shape.Cells.Length);
            for (int i = 0; i < piece.Shape.Cells.Length; i++)
            {
                Vector2Int cell = piece.Shape.Cells[i];
                int r = row + cell.y;
                int c = col + cell.x;
                board[r, c] = true;
                boardColors[r, c] = piece.Shape.Color;
                placedCells.Add(new Vector2Int(r, c));
            }

            piece.Used = true;
            selectedPieceIndex = -1;
            draggingPiece = false;
            HideDragGhost();
            int placedCellCount = piece.Shape.Cells.Length;
            List<int> rows = FindFullRows();
            List<int> cols = FindFullCols();
            int cleared = rows.Count + cols.Count;
            linesCleared += cleared;
            int gained = placedCellCount * 12;
            if (cleared > 0)
            {
                combo++;
                gained += cleared * 100 + combo * 35;
            }
            else
            {
                combo = 0;
            }

            score += gained;
            if (score > bestScore)
            {
                bestScore = score;
                SaveModeBestScore();
            }

            RefreshScore();
            RefreshBoard();
            AnimatePlacedCells(placedCells);
            RefreshPieces();
            RefreshGoalHud();
            ShowFloatingText("+" + gained, cleared > 0 ? Gold : Ink);

            if (cleared > 0)
                StartCoroutine(ClearLinesRoutine(rows, cols, gained));
            else
                FinishTurn(gained);
        }

        private void ShowPiecePlacements(int index)
        {
            if (resolving || gameOver || index < 0 || index >= pieces.Count || pieces[index].Used)
                return;

            PlaySound(selectClip, 0.58f, 1.02f);
            selectedPieceIndex = index;
            RefreshBoard();
            HighlightAllFits(pieces[index].Shape);
            RefreshPieces();
            SetStatus("Drag this shape to one of the glowing positions.");
        }

        private void CancelPiecePress(int index)
        {
            if (draggingPiece || selectedPieceIndex != index)
                return;

            selectedPieceIndex = -1;
            RefreshBoard();
            RefreshPieces();
            SetStatus("Drag a puzzle shape onto a glowing place.");
        }

        private void BeginPieceDrag(int index, Vector2 screenPosition, Camera eventCamera)
        {
            if (resolving || gameOver || index < 0 || index >= pieces.Count || pieces[index].Used)
                return;

            selectedPieceIndex = index;
            draggingPiece = true;
            PlaySound(selectClip, 0.52f, 1.06f);
            if (pieces[index].Group != null)
                pieces[index].Group.alpha = 0.36f;

            ShowDragGhost(pieces[index].Shape);
            MoveDragGhost(pieces[index].Shape, screenPosition, eventCamera);
            RefreshBoard();
            HighlightAllFits(pieces[index].Shape);
            PreviewFromScreen(screenPosition, eventCamera);
            RefreshPieces();
        }

        private void DragPiece(Vector2 screenPosition, Camera eventCamera)
        {
            if (!draggingPiece || selectedPieceIndex < 0 || selectedPieceIndex >= pieces.Count)
                return;

            MoveDragGhost(pieces[selectedPieceIndex].Shape, screenPosition, eventCamera);
            RefreshBoard();
            HighlightAllFits(pieces[selectedPieceIndex].Shape);
            PreviewFromScreen(screenPosition, eventCamera);
        }

        private void EndPieceDrag(Vector2 screenPosition, Camera eventCamera)
        {
            if (!draggingPiece || selectedPieceIndex < 0 || selectedPieceIndex >= pieces.Count)
                return;

            int index = selectedPieceIndex;
            if (pieces[index].Group != null)
                pieces[index].Group.alpha = 1f;

            if (TryGetMagneticPlacementFromScreen(pieces[index].Shape, screenPosition, eventCamera, out int row, out int col))
            {
                TryPlace(row, col);
                return;
            }

            draggingPiece = false;
            selectedPieceIndex = -1;
            HideDragGhost();
            PlaySound(invalidClip, 0.58f, 0.92f);
            RefreshBoard();
            RefreshPieces();
            SetStatus("Drop the shape on a glowing valid position.");
        }

        private void ShowDragGhost(Shape shape)
        {
            if (dragGhost == null || dragGhostCells == null)
                return;

            dragGhost.gameObject.SetActive(true);
            ResizeDragGhostToBoard();
            for (int i = 0; i < dragGhostCells.Length; i++)
                dragGhostCells[i].color = new Color(1f, 1f, 1f, 0f);

            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                if (cell.x < 0 || cell.x > 3 || cell.y < 0 || cell.y > 3)
                    continue;

                Image image = dragGhostCells[cell.y * 4 + cell.x];
                ApplyBlockVisual(image, shape.Color);
                image.color = new Color(1f, 1f, 1f, 0.88f);
            }

            dragGhost.SetAsLastSibling();
        }

        private void ResizeDragGhostToBoard()
        {
            if (dragGhost == null || boardRoot == null)
                return;

            GridLayoutGroup boardGrid = boardRoot.GetComponent<GridLayoutGroup>();
            GridLayoutGroup ghostGrid = dragGhost.GetComponent<GridLayoutGroup>();
            if (boardGrid == null || ghostGrid == null)
                return;

            Vector2 boardCell = boardGrid.cellSize;
            Vector2 boardSpacing = boardGrid.spacing;
            ghostGrid.cellSize = boardCell;
            ghostGrid.spacing = boardSpacing;
            dragGhost.sizeDelta = new Vector2(boardCell.x * 4f + boardSpacing.x * 3f, boardCell.y * 4f + boardSpacing.y * 3f);
        }

        private void MoveDragGhost(Shape shape, Vector2 screenPosition, Camera eventCamera)
        {
            if (dragGhost == null || !dragGhost.gameObject.activeSelf || canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 localPoint);
            dragGhost.anchoredPosition = localPoint + DragVisualOffset - GetShapeGhostVisualOffset(shape);
        }

        private void HideDragGhost()
        {
            if (dragGhost != null)
                dragGhost.gameObject.SetActive(false);
        }

        private IEnumerator ClearLinesRoutine(List<int> rows, List<int> cols, int gained)
        {
            resolving = true;
            SetStatus("Clean sweep! +" + gained);
            PlaySound(combo > 1 ? comboClip : lineClearClip, combo > 1 ? 0.78f : 0.56f, combo > 1 ? 1.06f : 1f);
            StartCoroutine(BoardPulseRoutine());
            BurstLineParticles(rows, cols);
            for (int flash = 0; flash < 3; flash++)
            {
                SetLineColors(rows, cols, flash % 2 == 0 ? Gold : Color.white);
                yield return new WaitForSecondsRealtime(0.08f);
            }

            yield return StartCoroutine(LineClearVanishRoutine(rows, cols));

            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                for (int col = 0; col < ActiveCols; col++)
                {
                    board[row, col] = false;
                    boardColors[row, col] = default;
                }
            }

            for (int i = 0; i < cols.Count; i++)
            {
                int col = cols[i];
                for (int row = 0; row < ActiveRows; row++)
                {
                    board[row, col] = false;
                    boardColors[row, col] = default;
                }
            }

            resolving = false;
            FinishTurn(gained);
        }

        private void FinishTurn(int gained)
        {
            if (hasMoveLimit)
                movesLeft = Mathf.Max(0, movesLeft - 1);

            if (AllPiecesUsed())
                GeneratePieces();

            RefreshBoard();
            RefreshPieces();
            AnimatePendingPieceSpawn();
            RefreshScore();
            RefreshGoalHud();

            if (CheckLevelComplete())
                return;

            if (hasMoveLimit && movesLeft <= 0)
            {
                gameOver = true;
                ShowOutcome("TRY AGAIN", "Moves are out. Score " + score + " / " + targetScore + ", lines " + linesCleared + " / " + targetLines + ".", "RETRY");
                return;
            }

            if (!AnyRemainingPieceFits())
            {
                gameOver = true;
                ShowOutcome("GAME OVER", "No remaining piece fits the board.", "PLAY AGAIN");
                return;
            }

            SetStatus(combo > 1 ? "Combo x" + combo + "! +" + gained : "Nice placement. +" + gained);
        }

        private void StartTetrisRun()
        {
            for (int i = 0; i < pieces.Count; i++)
                pieces[i].Used = true;

            tetrisLevel = 0;
            tetrisFallTimer = 0f;
            tetrisSoftDropActive = false;
            RefreshTetrisDifficulty();
            tetrisHasActivePiece = false;
            tetrisLastTapTime = -10f;
            RefreshTetrisControlModeUi();
            SpawnTetrisPiece();
        }

        private void UpdateTetrisMode()
        {
            if (gameOver || resolving || modeOverlay == null || modeOverlay.activeInHierarchy || settingsOverlay != null && settingsOverlay.activeInHierarchy || gameOverOverlay != null && gameOverOverlay.activeInHierarchy)
                return;

            float activeFallInterval = tetrisSoftDropActive
                ? Mathf.Min(tetrisFallInterval, TetrisSoftDropInterval)
                : tetrisFallInterval;

            tetrisFallTimer += Time.deltaTime;
            if (tetrisFallTimer >= activeFallInterval)
            {
                tetrisFallTimer = 0f;
                StepTetrisDown();
            }
        }

        private void SpawnTetrisPiece()
        {
            tetrisActiveShape = TetrisShapeLibrary[runRandom.Next(TetrisShapeLibrary.Length)];
            tetrisActiveRow = 0;
            tetrisActiveCol = Mathf.Clamp((ActiveCols - GetShapeWidth(tetrisActiveShape)) / 2, 0, ActiveCols - 1);
            tetrisHasActivePiece = true;
            tetrisSoftDropActive = false;

            if (!CanPlace(tetrisActiveShape, tetrisActiveRow, tetrisActiveCol))
            {
                tetrisHasActivePiece = false;
                gameOver = true;
                ShowOutcome("GAME OVER", "RetroGrid stack reached the top. Lines: " + linesCleared + ".", "PLAY AGAIN");
                return;
            }

            PlaySound(newPieceClip, 0.48f, 1.02f);
            RefreshBoard();
        }

        private void StepTetrisDown()
        {
            if (!tetrisHasActivePiece)
                return;

            if (TryMoveTetris(1, 0))
                return;

            LockTetrisPiece();
        }

        private bool TryMoveTetris(int rowDelta, int colDelta)
        {
            if (!tetrisHasActivePiece)
                return false;

            int nextRow = tetrisActiveRow + rowDelta;
            int nextCol = tetrisActiveCol + colDelta;
            if (!CanPlace(tetrisActiveShape, nextRow, nextCol))
            {
                if (colDelta != 0)
                    PlaySound(invalidClip, 0.30f, 1.10f);
                return false;
            }

            tetrisActiveRow = nextRow;
            tetrisActiveCol = nextCol;
            RefreshBoard();
            return true;
        }

        private void TryRotateTetris()
        {
            if (!tetrisHasActivePiece)
                return;

            Shape rotated = RotateTetrisShape(tetrisActiveShape);
            int[] kicks = { 0, -1, 1, -2, 2 };
            for (int i = 0; i < kicks.Length; i++)
            {
                int nextCol = tetrisActiveCol + kicks[i];
                if (!CanPlace(rotated, tetrisActiveRow, nextCol))
                    continue;

                tetrisActiveShape = rotated;
                tetrisActiveCol = nextCol;
                PlaySound(selectClip, 0.34f, 1.08f);
                RefreshBoard();
                return;
            }

            PlaySound(invalidClip, 0.30f, 0.96f);
        }

        private void HardDropTetris()
        {
            if (!tetrisHasActivePiece)
                return;

            while (CanPlace(tetrisActiveShape, tetrisActiveRow + 1, tetrisActiveCol))
                tetrisActiveRow++;

            LockTetrisPiece();
        }

        private void LockTetrisPiece()
        {
            if (!tetrisHasActivePiece)
                return;

            List<Vector2Int> placedCells = new List<Vector2Int>(tetrisActiveShape.Cells.Length);
            for (int i = 0; i < tetrisActiveShape.Cells.Length; i++)
            {
                Vector2Int cell = tetrisActiveShape.Cells[i];
                int row = tetrisActiveRow + cell.y;
                int col = tetrisActiveCol + cell.x;
                if (row < 0 || row >= ActiveRows || col < 0 || col >= ActiveCols)
                    continue;

                board[row, col] = true;
                boardColors[row, col] = tetrisActiveShape.Color;
                placedCells.Add(new Vector2Int(row, col));
            }

            tetrisHasActivePiece = false;
            PlaySound(placeClip, 0.48f, UnityEngine.Random.Range(0.98f, 1.04f));
            int previousLevel = tetrisLevel;
            int cleared = ClearTetrisRows();
            if (cleared > 0)
                RefreshTetrisDifficulty();

            int gained = 28 + GetTetrisLineScore(cleared);
            score += gained;
            if (score > bestScore)
            {
                bestScore = score;
                SaveModeBestScore();
            }

            AnimatePlacedCells(placedCells);
            RefreshScore();
            RefreshGoalHud();
            ShowFloatingText("+" + gained, cleared > 0 ? Gold : Ink);
            if (tetrisLevel > previousLevel)
                SetStatus("Level " + tetrisLevel + "! Speed up.");
            else
                SetStatus(cleared > 0 ? "RetroGrid line clear x" + cleared + "!" : "Piece locked.");
            SpawnTetrisPiece();
        }

        private void RefreshTetrisDifficulty()
        {
            if (currentMode != SymbiGridMode.Tetris)
                return;

            tetrisLevel = Mathf.Max(0, linesCleared / 10);
            int frames = GetTetrisGravityFrames(tetrisLevel);
            tetrisFallInterval = Mathf.Max(1f / TetrisFrameRate, frames / TetrisFrameRate);
        }

        private int GetTetrisGravityFrames(int level)
        {
            if (level < TetrisGravityFrames.Length)
                return TetrisGravityFrames[Mathf.Max(0, level)];

            return 1;
        }

        private int GetTetrisLineScore(int cleared)
        {
            if (cleared <= 0)
                return 0;

            int baseScore;
            switch (cleared)
            {
                case 1:
                    baseScore = 40;
                    break;
                case 2:
                    baseScore = 100;
                    break;
                case 3:
                    baseScore = 300;
                    break;
                default:
                    baseScore = 1200;
                    break;
            }

            return baseScore * (tetrisLevel + 1);
        }

        private int ClearTetrisRows()
        {
            int cleared = 0;
            for (int row = ActiveRows - 1; row >= 0; row--)
            {
                bool full = true;
                for (int col = 0; col < ActiveCols; col++)
                    full &= board[row, col];

                if (!full)
                    continue;

                cleared++;
                for (int shiftRow = row; shiftRow > 0; shiftRow--)
                {
                    for (int col = 0; col < ActiveCols; col++)
                    {
                        board[shiftRow, col] = board[shiftRow - 1, col];
                        boardColors[shiftRow, col] = boardColors[shiftRow - 1, col];
                    }
                }

                for (int col = 0; col < ActiveCols; col++)
                {
                    board[0, col] = false;
                    boardColors[0, col] = default;
                }

                row++;
            }

            if (cleared > 0)
            {
                linesCleared += cleared;
                combo++;
                tetrisFallInterval = Mathf.Max(0.55f, 1.25f - linesCleared * 0.018f);
                PlaySound(cleared > 1 ? comboClip : lineClearClip, cleared > 1 ? 0.70f : 0.56f, 1f);
                StartCoroutine(BoardPulseRoutine());
            }
            else
            {
                combo = 0;
            }

            return cleared;
        }

        private static Shape RotateTetrisShape(Shape shape)
        {
            int height = GetShapeHeight(shape);
            Vector2Int[] cells = new Vector2Int[shape.Cells.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                Vector2Int rotated = new Vector2Int(height - 1 - cell.y, cell.x);
                cells[i] = rotated;
                minX = Mathf.Min(minX, rotated.x);
                minY = Mathf.Min(minY, rotated.y);
            }

            for (int i = 0; i < cells.Length; i++)
                cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);

            return new Shape(shape.Name, shape.Color, cells);
        }

        private static int GetShapeWidth(Shape shape)
        {
            int maxX = 0;
            for (int i = 0; i < shape.Cells.Length; i++)
                maxX = Mathf.Max(maxX, shape.Cells[i].x);
            return maxX + 1;
        }

        private static int GetShapeHeight(Shape shape)
        {
            int maxY = 0;
            for (int i = 0; i < shape.Cells.Length; i++)
                maxY = Mathf.Max(maxY, shape.Cells[i].y);
            return maxY + 1;
        }

        private bool CheckLevelComplete()
        {
            return false;
        }

        private string BuildCompletionBody()
        {
            if (currentMode == SymbiGridMode.SymbiMine)
                return "SymbiMine " + GetSymbiMineDifficultyName() + " field cleared.";

            return "Level cleared.";
        }

        private string GetSymbiMineDifficultyName()
        {
            if (symbiMineRows == SymbiMineExpertRows && symbiMineCols == SymbiMineExpertCols && symbiMineTotalMines == SymbiMineExpertMines)
                return "Expert";
            if (symbiMineRows == SymbiMineIntermediateRows && symbiMineCols == SymbiMineIntermediateCols && symbiMineTotalMines == SymbiMineIntermediateMines)
                return "Intermediate";
            return "Beginner";
        }

        private void LoadDailyProgress()
        {
            int today = GetTodayNumber();
            int lastComplete = PlayerPrefs.GetInt(DailyLastCompleteKey, -1);
            dailyStreak = PlayerPrefs.GetInt(DailyStreakKey, 0);
            dailyBestScore = PlayerPrefs.GetInt(DailyBestScoreKey, 0);
            if (lastComplete < today - 1)
            {
                dailyStreak = 0;
                PlayerPrefs.SetInt(DailyStreakKey, dailyStreak);
            }
        }

        private void MarkDailyComplete()
        {
            int today = GetTodayNumber();
            int lastComplete = PlayerPrefs.GetInt(DailyLastCompleteKey, -1);
            if (score > dailyBestScore)
            {
                dailyBestScore = score;
                PlayerPrefs.SetInt(DailyBestScoreKey, dailyBestScore);
            }

            if (lastComplete == today)
            {
                PlayerPrefs.Save();
                return;
            }

            dailyStreak = lastComplete == today - 1 ? PlayerPrefs.GetInt(DailyStreakKey, 0) + 1 : 1;
            PlayerPrefs.SetInt(DailyLastCompleteKey, today);
            PlayerPrefs.SetInt(DailyStreakKey, dailyStreak);
            PlayerPrefs.Save();
        }

        private static int GetTodayNumber()
        {
            return (int)(DateTime.UtcNow.Date - new DateTime(2020, 1, 1)).TotalDays;
        }

        private void ShowOutcome(string title, string body, string action, bool recordScore = true, bool playOutcomeSound = true)
        {
            int previousLast = lastScore;
            bool canUseSecondChance = symbiMineSecondChanceOfferActive || (currentMode != SymbiGridMode.SymbiMine && title != "COMPLETE" && !secondChanceUsed);
            ConfigureOutcomeLayout(symbiMineSecondChanceOfferActive);
            if (playOutcomeSound)
                PlaySound(title == "COMPLETE" ? completeClip : gameOverClip, title == "COMPLETE" ? 0.72f : 0.62f, 1f);
            if (outcomeTitleText != null)
                outcomeTitleText.text = title;
            if (outcomeBodyText != null)
                outcomeBodyText.text = body;
            if (outcomeScoreText != null)
                outcomeScoreText.text = score.ToString();
            if (outcomeBestText != null)
                outcomeBestText.text = bestScore.ToString();
            if (outcomeLastText != null)
                outcomeLastText.text = previousLast.ToString();
            if (outcomeLinesText != null)
                outcomeLinesText.text = currentMode == SymbiGridMode.Classic
                    ? linesCleared.ToString()
                    : currentMode == SymbiGridMode.SymbiMine
                        ? symbiMineRevealedSafe + "/" + targetScore
                        : linesCleared + "/" + targetLines;
            if (outcomeActionLabel != null)
                outcomeActionLabel.text = action;
            if (secondChanceButtonRoot != null)
                secondChanceButtonRoot.SetActive(canUseSecondChance);
            if (outcomeMenuButtonRoot != null)
                outcomeMenuButtonRoot.SetActive(currentMode == SymbiGridMode.SymbiMine && !symbiMineSecondChanceOfferActive && title != "COMPLETE");
            if (recordScore)
            {
                lastScore = score;
                SaveModeLastScore();
                PlayerPrefs.Save();
                TryShowInterstitialAfterCompletedRun();
            }
            RefreshScore();
            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(true);
        }

        private void ConfigureOutcomeLayout(bool secondChancePrompt)
        {
            if (outcomeWindow != null)
            {
                outcomeWindow.anchorMin = secondChancePrompt ? new Vector2(0.035f, 0.245f) : new Vector2(0.055f, 0.055f);
                outcomeWindow.anchorMax = secondChancePrompt ? new Vector2(0.965f, 0.755f) : new Vector2(0.945f, 0.945f);
                outcomeWindow.offsetMin = Vector2.zero;
                outcomeWindow.offsetMax = Vector2.zero;
            }

            if (outcomeHeaderPanel != null)
            {
                outcomeHeaderPanel.anchorMin = secondChancePrompt ? new Vector2(0.07f, 0.46f) : new Vector2(0.07f, 0.73f);
                outcomeHeaderPanel.anchorMax = secondChancePrompt ? new Vector2(0.93f, 0.89f) : new Vector2(0.93f, 0.91f);
                outcomeHeaderPanel.offsetMin = Vector2.zero;
                outcomeHeaderPanel.offsetMax = Vector2.zero;
            }

            if (outcomeTitleText != null)
            {
                outcomeTitleText.fontSize = secondChancePrompt ? 58f : 64f;
                outcomeTitleText.fontSizeMin = secondChancePrompt ? 38f : 44f;
            }

            if (outcomeBodyText != null)
            {
                outcomeBodyText.fontSize = secondChancePrompt ? 30f : 26f;
                outcomeBodyText.fontSizeMin = secondChancePrompt ? 20f : 18f;
            }

            if (outcomeScorePanel != null)
                outcomeScorePanel.gameObject.SetActive(!secondChancePrompt);
            if (outcomeMetricsPanel != null)
                outcomeMetricsPanel.gameObject.SetActive(!secondChancePrompt);

            RectTransform secondChanceButton = secondChanceButtonRoot != null ? secondChanceButtonRoot.GetComponent<RectTransform>() : null;
            if (secondChanceButton != null)
            {
                secondChanceButton.anchorMin = secondChancePrompt ? new Vector2(0.09f, 0.255f) : new Vector2(0.10f, 0.20f);
                secondChanceButton.anchorMax = secondChancePrompt ? new Vector2(0.91f, 0.385f) : new Vector2(0.90f, 0.275f);
                secondChanceButton.offsetMin = Vector2.zero;
                secondChanceButton.offsetMax = Vector2.zero;

                TextMeshProUGUI label = secondChanceButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.fontSize = secondChancePrompt ? 34f : 30f;
                    label.fontSizeMin = secondChancePrompt ? 24f : 18f;
                }
            }

            if (outcomeActionLabel != null && outcomeActionLabel.transform.parent is RectTransform actionButton)
            {
                actionButton.anchorMin = secondChancePrompt ? new Vector2(0.09f, 0.105f) : new Vector2(0.10f, 0.115f);
                actionButton.anchorMax = secondChancePrompt ? new Vector2(0.91f, 0.235f) : new Vector2(0.90f, 0.19f);
                actionButton.offsetMin = Vector2.zero;
                actionButton.offsetMax = Vector2.zero;

                outcomeActionLabel.fontSize = secondChancePrompt ? 34f : 30f;
                outcomeActionLabel.fontSizeMin = secondChancePrompt ? 24f : 18f;
            }
        }

        private void TryShowInterstitialAfterCompletedRun()
        {
            if (exitingScene || NoAdsService.HasActiveNoAds())
                return;

            int count = Mathf.Max(0, PlayerPrefs.GetInt(InterstitialRunCountKey, 0)) + 1;
            PlayerPrefs.SetInt(InterstitialRunCountKey, count);
            PlayerPrefs.Save();

            if (count % InterstitialShowEveryRuns != 0)
                return;

            MonetizationService service = MonetizationService.Ensure();
            StartCoroutine(ShowSymbiGridInterstitialWhenReady(service));
        }

        private IEnumerator ShowSymbiGridInterstitialWhenReady(MonetizationService service)
        {
            if (service == null)
                yield break;

            string placementId = MonetizationService.SymbiGridInterstitialPlacementId;
            float deadline = Time.unscaledTime + InterstitialWarmupTimeoutSeconds;
            while (!exitingScene && Time.unscaledTime < deadline && !service.CanShowInterstitialAd(placementId))
                yield return null;

            if (exitingScene)
                yield break;

            service.ShowInterstitialAd(placementId, result =>
            {
                Debug.Log($"[SymbiGrid] Interstitial completed | State={result.State} | Placement={result.PlacementId}");
            });
        }

        private void HandleOutcomeAction()
        {
            if (symbiMineSecondChanceOfferActive)
            {
                DeclineSymbiMineSecondChance();
                return;
            }

            if (levelComplete && currentMode == SymbiGridMode.SymbiMine)
            {
                levelComplete = false;
                NewGame();
                return;
            }

            NewGame();
        }

        private void ReturnToSymbiGridModeMenuFromOutcome()
        {
            if (modeTransitionRunning)
                return;

            PlayLocalModeTransition(() =>
            {
                symbiMineSecondChanceOfferActive = false;
                symbiMineOutcomePending = false;
                resolving = false;
                HideDragGhost();
                if (gameOverOverlay != null)
                    gameOverOverlay.SetActive(false);
                if (symbiMineResultTapLayer != null)
                    symbiMineResultTapLayer.SetActive(false);

                OpenModeOverlay();
            });
        }

        private void HandleSecondChanceAdPlaceholder()
        {
            if (secondChanceUsed || levelComplete || symbiMineRewardedAdRequestInProgress)
                return;

            if (symbiMineSecondChanceOfferActive)
            {
                RequestSymbiMineSecondChanceRewardedAd();
                return;
            }

            RequestSymbiGridSecondChanceRewardedAd();
        }

        private void RequestSymbiGridSecondChanceRewardedAd()
        {
            MonetizationService monetization = MonetizationService.Ensure();
            RewardedAdAvailability availability = monetization.GetRewardedAdAvailability(MonetizationService.SymbiGridSecondChanceRewardedPlacementId);
            if (!availability.IsReady)
            {
                SetStatus(ResolveSymbiMineAdStatus(availability));
                if (outcomeBodyText != null)
                    outcomeBodyText.text = "Rewarded ad is not ready yet. Try again in a moment.";
                return;
            }

            symbiMineRewardedAdRequestInProgress = true;
            SetSecondChanceButtonInteractable(false);
            SetStatus("Showing rewarded ad...");

            monetization.ShowRewardedAd(MonetizationService.SymbiGridSecondChanceRewardedPlacementId, result =>
            {
                symbiMineRewardedAdRequestInProgress = false;
                SetSecondChanceButtonInteractable(true);

                if (!result.IsCompleted)
                {
                    SetStatus("Rewarded ad was not completed.");
                    if (outcomeBodyText != null)
                        outcomeBodyText.text = "Ad was not completed. Watch the full ad to continue.";
                    return;
                }

                GrantSymbiGridSecondChance();
            });
        }

        private void GrantSymbiGridSecondChance()
        {
            secondChanceUsed = true;
            gameOver = false;
            resolving = false;
            selectedPieceIndex = -1;
            draggingPiece = false;
            HideDragGhost();
            if (gameOverOverlay != null)
                gameOverOverlay.SetActive(false);

            if (currentMode == SymbiGridMode.Tetris)
            {
                GrantTetrisSecondChance();
                return;
            }

            if (currentMode == SymbiGridMode.SymbiMine)
            {
                NewGame();
                return;
            }

            if (hasMoveLimit && movesLeft <= 0)
                movesLeft = 5;

            RerollAvailablePieces();
            RefreshBoard();
            RefreshPieces();
            AnimatePendingPieceSpawn();
            RefreshScore();
            RefreshGoalHud();
            PlaySound(newPieceClip, 0.58f, 1.02f);
            SetStatus(hasMoveLimit ? "Second chance granted. +5 moves." : "Second chance granted.");
        }

        private void RequestSymbiMineSecondChanceRewardedAd()
        {
            MonetizationService monetization = MonetizationService.Ensure();
            RewardedAdAvailability availability = monetization.GetRewardedAdAvailability(MonetizationService.SymbiMineSecondChanceRewardedPlacementId);
            if (!availability.IsReady)
            {
                SetStatus(ResolveSymbiMineAdStatus(availability));
                if (outcomeBodyText != null)
                    outcomeBodyText.text = "Rewarded ad is not ready yet. Try again in a moment, or show the full minefield.";
                return;
            }

            symbiMineRewardedAdRequestInProgress = true;
            SetSecondChanceButtonInteractable(false);
            SetStatus("Showing rewarded ad...");

            monetization.ShowRewardedAd(MonetizationService.SymbiMineSecondChanceRewardedPlacementId, result =>
            {
                symbiMineRewardedAdRequestInProgress = false;
                SetSecondChanceButtonInteractable(true);

                if (!result.IsCompleted)
                {
                    SetStatus("Rewarded ad was not completed.");
                    if (outcomeBodyText != null)
                        outcomeBodyText.text = "Ad was not completed. Watch the full ad to continue, or show the full minefield.";
                    return;
                }

                GrantSymbiMineSecondChance();
            });
        }

        private void SetSecondChanceButtonInteractable(bool interactable)
        {
            if (secondChanceButtonRoot == null)
                return;

            Button button = secondChanceButtonRoot.GetComponent<Button>();
            if (button != null)
                button.interactable = interactable;
        }

        private static string ResolveSymbiMineAdStatus(RewardedAdAvailability availability)
        {
            if (availability.State == RewardedAdAvailabilityState.NotInitialized)
                return "Rewarded ad is initializing.";
            if (availability.State == RewardedAdAvailabilityState.Loading)
                return "Rewarded ad is loading.";
            if (availability.State == RewardedAdAvailabilityState.Unavailable)
                return "Rewarded ad is unavailable.";

            return "Rewarded ad is not ready.";
        }

        private void GrantTetrisSecondChance()
        {
            tetrisHasActivePiece = false;
            tetrisFallTimer = 0f;
            tetrisSoftDropActive = false;

            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    board[row, col] = false;
                    boardColors[row, col] = default;
                }
            }

            RefreshBoard();
            RefreshGoalHud();
            PlaySound(newPieceClip, 0.58f, 1.02f);
            SetStatus("Second chance granted.");
            SpawnTetrisPiece();
        }

        private bool AllPiecesUsed()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Used)
                    return false;
            }

            return true;
        }

        private bool AnyRemainingPieceFits()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Used && AnyPlacementFits(pieces[i].Shape))
                    return true;
            }

            return false;
        }

        private bool AnyPlacementFits(Shape shape)
        {
            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    if (CanPlace(shape, row, col))
                        return true;
                }
            }

            return false;
        }

        private bool CanPlace(Shape shape, int row, int col)
        {
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                int r = row + cell.y;
                int c = col + cell.x;
                if (r < 0 || r >= ActiveRows || c < 0 || c >= ActiveCols || board[r, c])
                    return false;
            }

            return true;
        }

        private List<int> FindFullRows()
        {
            List<int> rows = new List<int>();
            for (int row = 0; row < ActiveRows; row++)
            {
                bool full = true;
                for (int col = 0; col < ActiveCols; col++)
                    full &= board[row, col];

                if (full)
                    rows.Add(row);
            }

            return rows;
        }

        private List<int> FindFullCols()
        {
            List<int> cols = new List<int>();
            for (int col = 0; col < ActiveCols; col++)
            {
                bool full = true;
                for (int row = 0; row < ActiveRows; row++)
                    full &= board[row, col];

                if (full)
                    cols.Add(col);
            }

            return cols;
        }

        private void Preview(int row, int col)
        {
            if (selectedPieceIndex < 0 || selectedPieceIndex >= pieces.Count || pieces[selectedPieceIndex].Used || resolving || gameOver)
                return;

            RefreshBoard();
            Shape shape = pieces[selectedPieceIndex].Shape;
            HighlightAllFits(shape);
            bool valid = CanPlace(shape, row, col);
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                int r = row + cell.y;
                int c = col + cell.x;
                if (r < 0 || r >= ActiveRows || c < 0 || c >= ActiveCols)
                    continue;

                cellImages[r, c].color = valid ? CellPreview : CellInvalid;
            }

            if (valid)
            {
                int impact = PreviewLineClearImpact(shape, row, col);
                if (impact > 0 && draggingPiece)
                    SetStatus("Drop here to clear " + impact + " line" + (impact == 1 ? "." : "s."));
            }
        }

        private int PreviewLineClearImpact(Shape shape, int row, int col)
        {
            List<int> previewRows = new List<int>();
            List<int> previewCols = new List<int>();

            for (int r = 0; r < ActiveRows; r++)
            {
                bool full = true;
                for (int c = 0; c < ActiveCols; c++)
                    full &= IsFilledAfterPreview(shape, row, col, r, c);

                if (full)
                    previewRows.Add(r);
            }

            for (int c = 0; c < ActiveCols; c++)
            {
                bool full = true;
                for (int r = 0; r < ActiveRows; r++)
                    full &= IsFilledAfterPreview(shape, row, col, r, c);

                if (full)
                    previewCols.Add(c);
            }

            SetLineColors(previewRows, previewCols, Gold);
            return previewRows.Count + previewCols.Count;
        }

        private bool IsFilledAfterPreview(Shape shape, int anchorRow, int anchorCol, int row, int col)
        {
            if (board[row, col])
                return true;

            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                if (anchorRow + cell.y == row && anchorCol + cell.x == col)
                    return true;
            }

            return false;
        }

        private void PreviewFromScreen(Vector2 screenPosition, Camera eventCamera)
        {
            if (selectedPieceIndex < 0 || selectedPieceIndex >= pieces.Count)
                return;

            if (TryGetMagneticPlacementFromScreen(pieces[selectedPieceIndex].Shape, screenPosition, eventCamera, out int row, out int col)
                || TryGetPlacementFromScreen(pieces[selectedPieceIndex].Shape, screenPosition, eventCamera, out row, out col))
            {
                Preview(row, col);
            }
        }

        private bool TryGetPlacementFromScreen(Shape shape, Vector2 screenPosition, Camera eventCamera, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (!TryGetPlacementFloatFromScreen(shape, screenPosition, eventCamera, out float rawRow, out float rawCol))
                return false;

            row = Mathf.RoundToInt(rawRow);
            col = Mathf.RoundToInt(rawCol);
            return row > -4 && row < ActiveRows && col > -4 && col < ActiveCols;
        }

        private bool TryGetMagneticPlacementFromScreen(Shape shape, Vector2 screenPosition, Camera eventCamera, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (!TryGetPlacementFloatFromScreen(shape, screenPosition, eventCamera, out float rawRow, out float rawCol))
                return false;

            int nearestRow = Mathf.RoundToInt(rawRow);
            int nearestCol = Mathf.RoundToInt(rawCol);
            if (CanPlace(shape, nearestRow, nearestCol))
            {
                row = nearestRow;
                col = nearestCol;
                return true;
            }

            float bestDistance = PlacementMagnetRadiusInCells * PlacementMagnetRadiusInCells;
            bool found = false;
            int searchRadius = Mathf.CeilToInt(PlacementMagnetRadiusInCells) + 1;
            int minRow = Mathf.Max(0, nearestRow - searchRadius);
            int maxRow = Mathf.Min(ActiveRows - 1, nearestRow + searchRadius);
            int minCol = Mathf.Max(0, nearestCol - searchRadius);
            int maxCol = Mathf.Min(ActiveCols - 1, nearestCol + searchRadius);

            for (int candidateRow = minRow; candidateRow <= maxRow; candidateRow++)
            {
                for (int candidateCol = minCol; candidateCol <= maxCol; candidateCol++)
                {
                    if (!CanPlace(shape, candidateRow, candidateCol))
                        continue;

                    float distance = (candidateRow - rawRow) * (candidateRow - rawRow)
                        + (candidateCol - rawCol) * (candidateCol - rawCol);
                    if (distance > bestDistance)
                        continue;

                    bestDistance = distance;
                    row = candidateRow;
                    col = candidateCol;
                    found = true;
                }
            }

            return found;
        }

        private bool TryGetPlacementFloatFromScreen(Shape shape, Vector2 screenPosition, Camera eventCamera, out float row, out float col)
        {
            row = -1f;
            col = -1f;

            if (canvas == null || boardRoot == null || shape.Cells == null || shape.Cells.Length == 0)
                return false;

            RectTransform canvasRect = canvas.transform as RectTransform;
            GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
            if (canvasRect == null || grid == null)
                return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 canvasLocal);
            Vector3 shapeCenterWorld = canvasRect.TransformPoint(canvasLocal + DragVisualOffset);
            Vector2 boardLocal = boardRoot.InverseTransformPoint(shapeCenterWorld);

            int minX = 3;
            int minY = 3;
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }

            Vector2 cellSize = grid.cellSize;
            Vector2 spacing = grid.spacing;
            float stepX = cellSize.x + spacing.x;
            float stepY = cellSize.y + spacing.y;
            Vector2 gridOrigin = new Vector2(
                boardRoot.rect.xMin + grid.padding.left + cellSize.x * 0.5f,
                boardRoot.rect.yMax - grid.padding.top - cellSize.y * 0.5f);

            float shapeCenterX = (minX + maxX) * 0.5f;
            float shapeCenterY = (minY + maxY) * 0.5f;
            col = (boardLocal.x - gridOrigin.x) / stepX - shapeCenterX;
            row = (gridOrigin.y - boardLocal.y) / stepY - shapeCenterY;

            return row > -4f && row < ActiveRows && col > -4f && col < ActiveCols;
        }

        private Vector2 GetShapeGhostVisualOffset(Shape shape)
        {
            if (shape.Cells == null || shape.Cells.Length == 0)
                return Vector2.zero;

            int minX = 3;
            int minY = 3;
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                Vector2Int cell = shape.Cells[i];
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            Vector2 step = GetBoardCellStep();
            return new Vector2((centerX - 1.5f) * step.x, -(centerY - 1.5f) * step.y);
        }

        private Vector2 GetBoardCellStep()
        {
            if (boardRoot == null)
                return new Vector2(40f, 40f);

            GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return new Vector2(40f, 40f);

            return grid.cellSize + grid.spacing;
        }

        private void RefreshPlacementHints()
        {
            RefreshBoard();
            if (selectedPieceIndex >= 0 && selectedPieceIndex < pieces.Count && !pieces[selectedPieceIndex].Used && !resolving && !gameOver)
                HighlightAllFits(pieces[selectedPieceIndex].Shape);
        }

        private void HighlightAllFits(Shape shape)
        {
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.5f;
            Color hintColor = Color.Lerp(
                new Color(0.08f, 0.28f, 0.32f, 1f),
                new Color(0.16f, 0.52f, 0.58f, 1f),
                pulse);
            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    if (!CanPlace(shape, row, col))
                        continue;

                    for (int i = 0; i < shape.Cells.Length; i++)
                    {
                        Vector2Int cell = shape.Cells[i];
                        int r = row + cell.y;
                        int c = col + cell.x;
                        if (r >= 0 && r < ActiveRows && c >= 0 && c < ActiveCols && !board[r, c])
                            cellImages[r, c].color = hintColor;
                    }
                }
            }
        }

        private void RefreshBoard()
        {
            if (currentMode == SymbiGridMode.SymbiMine)
            {
                RefreshSymbiMineBoard();
                return;
            }

            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    Image image = cellImages[row, col];
                    if (image == null)
                        continue;

                    image.sprite = GetCellSprite((row + col) % 2 != 0);
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                    if (cellTexts[row, col] != null)
                        cellTexts[row, col].text = "";

                    Image block = blockImages[row, col];
                    if (block == null)
                        continue;

                    if (board[row, col])
                    {
                        ApplyBlockVisual(block, boardColors[row, col]);
                    }
                    else
                    {
                        block.sprite = null;
                        block.color = new Color(1f, 1f, 1f, 0f);
                    }
                }
            }

            if (currentMode == SymbiGridMode.Tetris && tetrisHasActivePiece)
            {
                for (int i = 0; i < tetrisActiveShape.Cells.Length; i++)
                {
                    Vector2Int cell = tetrisActiveShape.Cells[i];
                    int row = tetrisActiveRow + cell.y;
                    int col = tetrisActiveCol + cell.x;
                    if (row < 0 || row >= ActiveRows || col < 0 || col >= ActiveCols)
                        continue;

                    Image block = blockImages[row, col];
                    if (block != null)
                        ApplyBlockVisual(block, tetrisActiveShape.Color);

                    if (cellImages[row, col] != null)
                        cellImages[row, col].color = CellPreview;
                }
            }
        }

        private void RefreshSymbiMineBoard()
        {
            for (int row = 0; row < ActiveRows; row++)
            {
                for (int col = 0; col < ActiveCols; col++)
                {
                    Image image = cellImages[row, col];
                    Image block = blockImages[row, col];
                    TextMeshProUGUI label = cellTexts[row, col];
                    if (image == null || block == null)
                        continue;

                    bool revealed = revealedMineCells[row, col];
                    bool flagged = flaggedMineCells[row, col];
                    image.sprite = GetCellSprite((row + col) % 2 != 0);
                    image.type = Image.Type.Sliced;
                    image.color = revealed
                        ? new Color(0.77f, 0.84f, 0.88f, 1f)
                        : flagged
                            ? new Color(0.12f, 0.22f, 0.30f, 1f)
                            : new Color(0.09f, 0.16f, 0.22f, 1f);

                    block.sprite = null;
                    block.type = Image.Type.Simple;
                    block.preserveAspect = false;
                    block.color = new Color(1f, 1f, 1f, 0f);

                    if (label == null)
                        continue;

                    label.text = "";
                    label.color = Ink;
                    label.fontSizeMax = 34f;

                    if (flagged && !revealed)
                    {
                        if (symbiMineExploded && !mineCells[row, col])
                        {
                            label.text = "X";
                            label.color = new Color(1f, 0.30f, 0.28f, 1f);
                            image.color = new Color(0.30f, 0.05f, 0.07f, 1f);
                            continue;
                        }

                        label.text = "!";
                        label.color = Gold;
                        continue;
                    }

                    if (!revealed)
                        continue;

                    if (mineCells[row, col])
                    {
                        bool detonated = row == symbiMineDetonatedRow && col == symbiMineDetonatedCol;
                        Sprite mineSprite = GetSymbiMineMineSprite();
                        if (mineSprite != null)
                        {
                            block.sprite = mineSprite;
                            block.preserveAspect = true;
                            block.color = detonated ? Color.white : new Color(1f, 1f, 1f, 0.82f);
                        }
                        else
                        {
                            label.text = "*";
                            label.color = symbiMineExploded ? new Color(1f, 0.34f, 0.32f, 1f) : Gold;
                        }

                        image.color = detonated
                            ? new Color(0.82f, 0.16f, 0.10f, 1f)
                            : new Color(0.32f, 0.06f, 0.08f, 1f);
                        if (detonated)
                        {
                            label.text = "!";
                            label.color = Color.white;
                            label.fontSizeMax = 28f;
                        }

                        continue;
                    }

                    int count = adjacentMineCounts[row, col];
                    if (count <= 0)
                        continue;

                    label.text = count.ToString();
                    label.color = GetSymbiMineNumberColor(count);
                }
            }
        }

        private static Color GetSymbiMineNumberColor(int count)
        {
            switch (count)
            {
                case 1:
                    return new Color(0.25f, 0.55f, 1f, 1f);
                case 2:
                    return new Color(0.18f, 0.72f, 0.38f, 1f);
                case 3:
                    return new Color(1f, 0.34f, 0.28f, 1f);
                case 4:
                    return new Color(0.56f, 0.42f, 1f, 1f);
                case 5:
                    return new Color(1f, 0.62f, 0.25f, 1f);
                default:
                    return new Color(0.22f, 0.92f, 0.92f, 1f);
            }
        }

        private void SetLineColors(List<int> rows, List<int> cols, Color color)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                for (int col = 0; col < ActiveCols; col++)
                    cellImages[row, col].color = color;
            }

            for (int i = 0; i < cols.Count; i++)
            {
                int col = cols[i];
                for (int row = 0; row < ActiveRows; row++)
                    cellImages[row, col].color = color;
            }
        }

        private void RefreshPieces()
        {
            if (currentMode == SymbiGridMode.Tetris || currentMode == SymbiGridMode.SymbiMine)
                return;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceView piece = pieces[i];
                bool selected = i == selectedPieceIndex;
                if (!draggingPiece && piece.Group != null)
                    piece.Group.alpha = 1f;

                piece.Button.interactable = !piece.Used && !gameOver;
                piece.Root.localScale = selected ? Vector3.one * 1.06f : Vector3.one;
                piece.Button.image.color = piece.Used
                    ? new Color(0.03f, 0.04f, 0.06f, 0.45f)
                    : selected
                        ? new Color(0.10f, 0.18f, 0.28f, 1f)
                        : new Color(0.04f, 0.07f, 0.11f, 0.98f);
                RefreshMiniCells(piece);
            }
        }

        private static void RefreshMiniCells(PieceView piece)
        {
            for (int i = 0; i < piece.MiniCells.Length; i++)
            {
                piece.MiniCells[i].sprite = null;
                piece.MiniCells[i].color = new Color(1f, 1f, 1f, 0f);
            }

            if (piece.Used)
                return;

            int minX = 4;
            int minY = 4;
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < piece.Shape.Cells.Length; i++)
            {
                Vector2Int cell = piece.Shape.Cells[i];
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }

            int shapeWidth = maxX - minX + 1;
            int shapeHeight = maxY - minY + 1;
            int offsetX = Mathf.FloorToInt((MiniGridSize - shapeWidth) * 0.5f) - minX;
            int offsetY = Mathf.FloorToInt((MiniGridSize - shapeHeight) * 0.5f) - minY;

            for (int i = 0; i < piece.Shape.Cells.Length; i++)
            {
                Vector2Int cell = piece.Shape.Cells[i];
                int x = cell.x + offsetX;
                int y = cell.y + offsetY;
                if (x < 0 || x >= MiniGridSize || y < 0 || y >= MiniGridSize)
                    continue;

                int index = y * MiniGridSize + x;
                ApplyBlockVisual(piece.MiniCells[index], piece.Shape.Color);
            }
        }

        private static void ApplyBlockVisual(Image image, Color sourceColor)
        {
            if (image == null)
                return;

            Sprite sprite = GetBlockSprite(sourceColor);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                return;
            }

            image.sprite = null;
            image.color = sourceColor;
        }

        private static Sprite GetBackgroundSprite()
        {
            if (backgroundSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(BackgroundResourcePath);
                if (texture != null)
                    backgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return backgroundSprite;
        }

        private static Sprite GetMenuBackgroundSprite()
        {
            if (menuBackgroundSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(MenuBackgroundResourcePath);
                if (texture != null)
                    menuBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return menuBackgroundSprite != null ? menuBackgroundSprite : GetBackgroundSprite();
        }

        private static Sprite GetSymbiMineMineSprite()
        {
            if (symbiMineMineSprite == null)
                symbiMineMineSprite = LoadResourceSprite(SymbiMineMineResourcePath);

            return symbiMineMineSprite;
        }

        private static Sprite GetSymbiGridTitleLogoSprite()
        {
            if (symbiGridTitleLogoSprite == null)
                symbiGridTitleLogoSprite = LoadResourceSprite(SymbiGridTitleLogoResourcePath);

            return symbiGridTitleLogoSprite;
        }

        private static Sprite GetRetroGridTitleLogoSprite()
        {
            if (retroGridTitleLogoSprite == null)
                retroGridTitleLogoSprite = LoadResourceSprite(RetroGridTitleLogoResourcePath);

            return retroGridTitleLogoSprite;
        }

        private static Sprite GetClassicGridTitleLogoSprite()
        {
            if (classicGridTitleLogoSprite == null)
                classicGridTitleLogoSprite = LoadResourceSprite(ClassicGridTitleLogoResourcePath);

            return classicGridTitleLogoSprite;
        }

        private static Sprite GetSymbiMineTitleLogoSprite()
        {
            if (symbiMineTitleLogoSprite == null)
                symbiMineTitleLogoSprite = LoadResourceSprite(SymbiMineTitleLogoResourcePath);

            return symbiMineTitleLogoSprite;
        }

        private static Sprite[] GetSymbiMineExplosionSprites()
        {
            if (symbiMineExplosionSprites != null)
                return symbiMineExplosionSprites;

            Sprite[] importedSprites = Resources.LoadAll<Sprite>(SymbiMineExplosionSheetPath);
            if (importedSprites != null && importedSprites.Length > 0)
            {
                Array.Sort(importedSprites, (left, right) => ExtractTrailingNumber(left.name).CompareTo(ExtractTrailingNumber(right.name)));
                symbiMineExplosionSprites = importedSprites;
                return symbiMineExplosionSprites;
            }

            Sprite fallback = LoadResourceSprite(SymbiMineExplosionSheetPath);
            symbiMineExplosionSprites = fallback != null ? new[] { fallback } : Array.Empty<Sprite>();
            return symbiMineExplosionSprites;
        }

        private static Sprite LoadResourceSprite(string path)
        {
            Sprite[] importedSprites = Resources.LoadAll<Sprite>(path);
            if (importedSprites != null && importedSprites.Length > 0 && importedSprites[0] != null)
                return importedSprites[0];

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int multiplier = 1;
            int result = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                    break;

                result += (c - '0') * multiplier;
                multiplier *= 10;
            }

            return result;
        }

        private static Sprite GetCellSprite(bool alternate)
        {
            if (alternate)
            {
                if (cellAltSprite == null)
                    cellAltSprite = CreateRoundedCellSprite(CellEmptyAlt, new Color(0.11f, 0.17f, 0.22f, 1f), new Color(0.03f, 0.08f, 0.10f, 1f));
                return cellAltSprite;
            }

            if (cellSprite == null)
                cellSprite = CreateRoundedCellSprite(CellEmpty, new Color(0.13f, 0.20f, 0.26f, 1f), new Color(0.04f, 0.10f, 0.12f, 1f));
            return cellSprite;
        }

        private static Sprite GetBoardPanelSprite()
        {
            if (boardPanelSprite != null)
                return boardPanelSprite;

            const int textureSize = 160;
            const float radius = 22f;
            const float border = 3f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = new Color(0.025f, 0.038f, 0.060f, 0.84f);
            Color shadow = new Color(0.004f, 0.010f, 0.018f, 0.92f);
            Color edge = new Color(0.08f, 0.25f, 0.28f, 0.70f);
            Vector2 center = new Vector2((textureSize - 1f) * 0.5f, (textureSize - 1f) * 0.5f);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float alpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    float inner = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius - border, border);
                    float edgeAmount = Mathf.Clamp01(alpha - inner);
                    float vertical = y / (textureSize - 1f);
                    float radial = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / center.x);
                    Color body = Color.Lerp(shadow, fill, 0.72f + vertical * 0.16f);
                    body = Color.Lerp(body, new Color(0.035f, 0.075f, 0.085f, 0.86f), radial * 0.16f);
                    Color color = Color.Lerp(body, edge, edgeAmount * 0.82f);
                    color.a *= alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, true);
            boardPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(26f, 26f, 26f, 26f));
            return boardPanelSprite;
        }

        private static Sprite CreateRoundedCellSprite(Color fill, Color edge, Color shadow)
        {
            const int textureSize = 64;
            const float radius = 9f;
            const float border = 2.2f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float alpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    float innerAlpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius - border, border);
                    float edgeAmount = Mathf.Clamp01(alpha - innerAlpha);
                    float vertical = y / (textureSize - 1f);
                    Color body = Color.Lerp(shadow, fill, 0.68f + vertical * 0.22f);
                    Color color = Color.Lerp(body, edge, edgeAmount * 0.85f);
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12f, 12f, 12f, 12f));
        }

        private static float RoundedRectCoverage(float x, float y, float width, float height, float radius, float inset = 0f)
        {
            float left = inset;
            float right = width - inset;
            float bottom = inset;
            float top = height - inset;
            float r = Mathf.Max(0f, radius);
            float cx = Mathf.Clamp(x, left + r, right - r);
            float cy = Mathf.Clamp(y, bottom + r, top - r);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(r + 0.75f - distance);
        }

        private static Sprite GetBlockSprite(Color sourceColor)
        {
            Sprite[] sprites = GetBlockSprites();
            if (sprites == null || sprites.Length == 0)
                return null;

            float hue;
            float saturation;
            float value;
            Color.RGBToHSV(sourceColor, out hue, out saturation, out value);
            if (hue < 0.08f || hue > 0.92f)
                return sprites[Mathf.Min(3, sprites.Length - 1)];
            if (hue < 0.17f)
                return sprites[Mathf.Min(2, sprites.Length - 1)];
            if (hue < 0.43f)
                return sprites[Mathf.Min(1, sprites.Length - 1)];

            return sprites[0];
        }

        private static Sprite[] GetBlockSprites()
        {
            if (blockSprites != null && !blockSpritesAreFallback)
                return blockSprites;

            Sprite[] importedSprites = Resources.LoadAll<Sprite>(BlockAtlasResourcePath);
            if (importedSprites != null && importedSprites.Length > 0 && importedSprites[0] != null)
            {
                Sprite source = importedSprites[0];
                Texture2D texture = source.texture;
                Rect rect = source.rect;
                if (texture != null && rect.width > 1f && rect.height > 1f)
                {
                    blockSprites = CreateBlockSpritesFromAtlas(texture, rect);
                    blockSpritesAreFallback = false;
                    return blockSprites;
                }
            }

            Texture2D atlas = Resources.Load<Texture2D>(BlockAtlasResourcePath);
            if (atlas != null)
            {
                blockSprites = CreateBlockSpritesFromAtlas(atlas, new Rect(0f, 0f, atlas.width, atlas.height));
                blockSpritesAreFallback = false;
                return blockSprites;
            }

            blockSprites = new[]
            {
                CreateBlockSprite(new Color(0.10f, 0.88f, 0.92f, 1f), new Color(0.78f, 1f, 1f, 1f), new Color(0.02f, 0.32f, 0.38f, 1f)),
                CreateBlockSprite(new Color(0.22f, 0.92f, 0.24f, 1f), new Color(0.83f, 1f, 0.66f, 1f), new Color(0.05f, 0.36f, 0.06f, 1f)),
                CreateBlockSprite(new Color(1f, 0.67f, 0.08f, 1f), new Color(1f, 0.92f, 0.50f, 1f), new Color(0.46f, 0.18f, 0.02f, 1f)),
                CreateBlockSprite(new Color(1f, 0.22f, 0.30f, 1f), new Color(1f, 0.76f, 0.80f, 1f), new Color(0.42f, 0.04f, 0.09f, 1f))
            };
            blockSpritesAreFallback = true;
            return blockSprites;
        }

        private static Sprite[] CreateBlockSpritesFromAtlas(Texture2D atlas, Rect rect)
        {
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;
            float left = rect.x;
            float bottom = rect.y;
            return new[]
            {
                Sprite.Create(atlas, new Rect(left, bottom + halfHeight, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(left + halfWidth, bottom + halfHeight, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(left, bottom, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(left + halfWidth, bottom, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f)
            };
        }

        private static Sprite CreateBlockSprite(Color fill, Color highlight, Color shadow)
        {
            const int textureSize = 96;
            const float radius = 13f;
            const float border = 3f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Vector2 center = new Vector2((textureSize - 1f) * 0.5f, (textureSize - 1f) * 0.5f);
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float alpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius, 4f);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    float inner = RoundedRectCoverage(x + 0.5f, y + 0.5f, textureSize, textureSize, radius - border, 4f + border);
                    float edge = Mathf.Clamp01(alpha - inner);
                    float vertical = y / (textureSize - 1f);
                    float horizontalGlow = 1f - Mathf.Clamp01(Mathf.Abs(x - center.x) / center.x);
                    float radial = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / center.x);

                    Color body = Color.Lerp(shadow, fill, 0.68f + vertical * 0.18f);
                    body = Color.Lerp(body, highlight, Mathf.Clamp01(radial * 0.34f + horizontalGlow * 0.10f));
                    Color color = Color.Lerp(body, highlight, edge * 0.65f);
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private void RefreshScore()
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
            if (linesText != null)
                linesText.text = currentMode == SymbiGridMode.Tetris
                    ? "Level: " + tetrisLevel + "  Lines: " + linesCleared
                    : currentMode == SymbiGridMode.Classic
                    ? "Lines: " + linesCleared
                    : "Safe: " + symbiMineRevealedSafe + "/" + targetScore;
            if (comboText != null)
                comboText.text = combo > 1 ? "COMBO x" + combo : "";
            if (comboEnergyFill != null)
            {
                float energy = combo > 1 ? Mathf.Clamp01(combo / 5f) : 0f;
                RectTransform rect = comboEnergyFill.rectTransform;
                rect.anchorMax = new Vector2(energy, 1f);
                rect.offsetMax = Vector2.zero;
            }
        }

        private void RefreshGoalHud()
        {
            bool isSymbiMine = currentMode == SymbiGridMode.SymbiMine;
            bool isRetroGrid = currentMode == SymbiGridMode.Tetris;
            bool isClassicGrid = currentMode == SymbiGridMode.Classic;
            bool showRetroGridLogo = isRetroGrid && retroGridTitleLogoImage != null && retroGridTitleLogoImage.sprite != null;
            bool showClassicGridLogo = isClassicGrid && classicGridTitleLogoImage != null && classicGridTitleLogoImage.sprite != null;
            bool showSymbiMineLogo = isSymbiMine && symbiMineTitleLogoImage != null && symbiMineTitleLogoImage.sprite != null;
            bool showAnyModeLogo = showRetroGridLogo || showClassicGridLogo || showSymbiMineLogo;
            if (topBarImage != null)
                topBarImage.enabled = !showAnyModeLogo;
            if (retroGridTitleLogoImage != null)
                retroGridTitleLogoImage.gameObject.SetActive(showRetroGridLogo);
            if (classicGridTitleLogoImage != null)
                classicGridTitleLogoImage.gameObject.SetActive(showClassicGridLogo);
            if (symbiMineTitleLogoImage != null)
                symbiMineTitleLogoImage.gameObject.SetActive(showSymbiMineLogo);

            if (modeText != null)
            {
                modeText.gameObject.SetActive(!showAnyModeLogo);
                if (isSymbiMine)
                    modeText.text = "SYMBIMINE";
                else if (isRetroGrid)
                    modeText.text = "RETROGRID L" + tetrisLevel;
                else
                    modeText.text = "CLASSICGRID";
            }

            if (goalText != null)
            {
                if (currentMode == SymbiGridMode.Classic)
                    goalText.text = "Goal: beat your best score";
                else if (currentMode == SymbiGridMode.Tetris)
                    goalText.text = "Goal: survive the speed curve";
                else
                    goalText.text = "Goal: open " + targetScore + " safe cells";
            }

            if (movesText != null)
                movesText.text = currentMode == SymbiGridMode.Tetris
                    ? "Fall: " + tetrisFallInterval.ToString("0.00") + "s  Next: " + (10 - linesCleared % 10)
                    : currentMode == SymbiGridMode.SymbiMine
                        ? "Mines: " + Mathf.Max(0, symbiMineTotalMines - symbiMineFlags) + "  Flags: " + symbiMineFlags
                        : hasMoveLimit ? "Moves: " + movesLeft : "";

            if (goalProgressFill != null)
            {
                float progress = currentMode == SymbiGridMode.Classic
                    ? Mathf.Clamp01(score / Mathf.Max(1f, bestScore > 0 ? bestScore : 1000f))
                    : currentMode == SymbiGridMode.Tetris
                        ? Mathf.Clamp01(linesCleared / 40f)
                        : currentMode == SymbiGridMode.SymbiMine
                            ? Mathf.Clamp01(symbiMineRevealedSafe / Mathf.Max(1f, targetScore))
                            : Mathf.Min(
                                Mathf.Clamp01(score / Mathf.Max(1f, targetScore)),
                                Mathf.Clamp01(linesCleared / Mathf.Max(1f, targetLines)));

                RectTransform rect = goalProgressFill.rectTransform;
                rect.anchorMax = new Vector2(progress, 1f);
                rect.offsetMax = Vector2.zero;
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }

        private void ShowFloatingText(string value, Color color)
        {
            if (canvas == null || string.IsNullOrWhiteSpace(value))
                return;

            TextMeshProUGUI text = CreateText(canvas.transform, "FloatingScore", value, 48f, FontStyles.Bold, color);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 215f);
            rect.sizeDelta = new Vector2(360f, 82f);
            rect.localScale = Vector3.one * 0.82f;
            text.raycastTarget = false;
            StartCoroutine(FloatingTextRoutine(text));
        }

        private IEnumerator FloatingTextRoutine(TextMeshProUGUI text)
        {
            if (text == null)
                yield break;

            RectTransform rect = text.rectTransform;
            Color startColor = text.color;
            Vector2 start = rect.anchoredPosition;
            Vector2 end = start + new Vector2(0f, 90f);
            float duration = 0.75f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                    float scale = p < 0.22f
                        ? Mathf.Lerp(0.82f, 1.12f, Mathf.SmoothStep(0f, 1f, p / 0.22f))
                        : Mathf.Lerp(1.12f, 0.92f, Mathf.SmoothStep(0f, 1f, (p - 0.22f) / 0.78f));
                    rect.localScale = Vector3.one * scale;
                }

                if (text != null)
                    text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, p));
                yield return null;
            }

            if (text != null)
                Destroy(text.gameObject);
        }

        private void AnimatePlacedCells(List<Vector2Int> cells)
        {
            if (cells == null)
                return;

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (cell.x < 0 || cell.x >= ActiveRows || cell.y < 0 || cell.y >= ActiveCols)
                    continue;

                Image block = blockImages[cell.x, cell.y];
                if (block != null)
                    StartCoroutine(BlockPopRoutine(block, i * 0.025f));
            }
        }

        private IEnumerator BlockPopRoutine(Image block, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            if (block == null)
                yield break;

            RectTransform rect = block.rectTransform;
            Color color = block.color;
            float duration = 0.20f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float scale = p < 0.58f
                    ? Mathf.Lerp(0.72f, 1.12f, Mathf.SmoothStep(0f, 1f, p / 0.58f))
                    : Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, (p - 0.58f) / 0.42f));
                if (rect != null)
                    rect.localScale = Vector3.one * scale;
                if (block != null)
                    block.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 1f, p)));
                yield return null;
            }

            if (rect != null)
                rect.localScale = Vector3.one;
            if (block != null)
                block.color = Color.white;
        }

        private IEnumerator LineClearVanishRoutine(List<int> rows, List<int> cols)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                for (int col = 0; col < ActiveCols; col++)
                    cells.Add(new Vector2Int(row, col));
            }

            for (int i = 0; i < cols.Count; i++)
            {
                int col = cols[i];
                for (int row = 0; row < ActiveRows; row++)
                    cells.Add(new Vector2Int(row, col));
            }

            float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                foreach (Vector2Int cell in cells)
                {
                    Image block = blockImages[cell.x, cell.y];
                    if (block == null)
                        continue;

                    RectTransform rect = block.rectTransform;
                    if (rect != null)
                        rect.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.12f, eased);
                    block.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, eased));
                    cellImages[cell.x, cell.y].color = Color.Lerp(Gold, Color.white, eased);
                }

                yield return null;
            }

            foreach (Vector2Int cell in cells)
            {
                Image block = blockImages[cell.x, cell.y];
                if (block == null)
                    continue;

                block.rectTransform.localScale = Vector3.one;
                block.color = Color.white;
            }
        }

        private void AnimatePendingPieceSpawn()
        {
            if (!piecesSpawnPending)
                return;

            piecesSpawnPending = false;
            PlaySound(newPieceClip, 0.50f, 1.02f);
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].Root != null && pieces[i].Group != null && !pieces[i].Used)
                    StartCoroutine(PieceSpawnRoutine(pieces[i], i * 0.055f));
            }
        }

        private IEnumerator PieceSpawnRoutine(PieceView piece, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            if (piece.Root == null || piece.Group == null || piece.Used)
                yield break;

            float duration = 0.24f;
            float t = 0f;
            piece.Root.localScale = Vector3.one * 0.82f;
            piece.Group.alpha = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                if (piece.Root != null)
                    piece.Root.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased + Mathf.Sin(p * Mathf.PI) * 0.08f);
                if (piece.Group != null)
                    piece.Group.alpha = Mathf.Lerp(0f, 1f, eased);
                yield return null;
            }

            if (piece.Root != null)
                piece.Root.localScale = Vector3.one;
            if (piece.Group != null)
                piece.Group.alpha = 1f;
        }

        private IEnumerator BoardPulseRoutine()
        {
            if (boardRoot == null)
                yield break;

            Vector3 start = Vector3.one;
            Vector3 peak = Vector3.one * 1.028f;
            float duration = 0.18f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float wave = Mathf.Sin(p * Mathf.PI);
                boardRoot.localScale = Vector3.LerpUnclamped(start, peak, wave);
                yield return null;
            }

            boardRoot.localScale = start;
        }

        private void BurstLineParticles(List<int> rows, List<int> cols)
        {
            if (canvas == null)
                return;

            int spawned = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                for (int col = 0; col < ActiveCols; col += 2)
                    SpawnSparkAtCell(row, col, spawned++);
            }

            for (int i = 0; i < cols.Count; i++)
            {
                int col = cols[i];
                for (int row = 0; row < ActiveRows; row += 2)
                    SpawnSparkAtCell(row, col, spawned++);
            }
        }

        private void SpawnSparkAtCell(int row, int col, int index)
        {
            Image source = cellImages[row, col];
            if (source == null || canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, source.rectTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            Image spark = CreateImage(canvasRect, "LineSpark", index % 2 == 0 ? Gold : Color.white);
            spark.raycastTarget = false;
            RectTransform rect = spark.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = local;
            rect.sizeDelta = new Vector2(18f, 18f);
            spark.transform.SetAsLastSibling();
            StartCoroutine(SparkRoutine(spark, index));
        }

        private IEnumerator SparkRoutine(Image spark, int index)
        {
            if (spark == null)
                yield break;

            RectTransform rect = spark.rectTransform;
            Color startColor = spark.color;
            Vector2 start = rect.anchoredPosition;
            float angle = index * 2.39996f;
            Vector2 end = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 62f;
            float duration = 0.42f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);
                rect.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, p);
                spark.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(0.9f, 0f, p));
                yield return null;
            }

            if (spark != null)
                Destroy(spark.gameObject);
        }

        private void BackToMain()
        {
            if (exitingScene)
                return;

            PlaySound(closeClip, 0.60f, 0.96f);
            StopRuntimeForExit();
            if (MahjongGame.SymbiGridSceneTransitionFx.PlayOrientationFade(
                MahjongGame.SceneOrientationPolicy.ApplyLandscapeOnly,
                () =>
                {
                    if (MahjongGame.SymbiGridSceneTransitionFx.Play(MainSceneName, () =>
                    {
                        SceneManager.LoadScene(MainSceneName);
                    }, ModeTransitionHoldSeconds))
                    {
                        return;
                    }

                    SceneManager.LoadScene(MainSceneName);
                }))
            {
                return;
            }

            MahjongGame.SceneOrientationPolicy.ApplyLandscapeOnly();
            SceneManager.LoadScene(MainSceneName);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            return CreateImage(parent, name, color).rectTransform;
        }

        private static RectTransform CreateDecor(Transform parent, string name, Color color)
        {
            RectTransform rect = CreatePanel(parent, name, color);
            Image image = rect.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI CreateStatCard(Transform parent, string name, string labelValue, Vector2 anchorMin, Vector2 anchorMax, Color accent)
        {
            RectTransform card = CreatePanel(parent, name, new Color(0.018f, 0.045f, 0.062f, 0.88f));
            ApplyUiSprite(card, UiSprite.StatCyan, 38f);
            card.anchorMin = anchorMin;
            card.anchorMax = anchorMax;
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            RectTransform accentBar = CreatePanel(card, "Accent", accent);
            accentBar.anchorMin = new Vector2(0f, 0f);
            accentBar.anchorMax = new Vector2(0.035f, 1f);
            accentBar.offsetMin = Vector2.zero;
            accentBar.offsetMax = Vector2.zero;

            TextMeshProUGUI label = CreateText(card, "Label", labelValue, 12f, FontStyles.Bold, MutedInk);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSizeMin = 12f;
            label.rectTransform.anchorMin = new Vector2(0f, 0.54f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            label.rectTransform.offsetMin = new Vector2(6f, 0f);
            label.rectTransform.offsetMax = new Vector2(-6f, 0f);

            TextMeshProUGUI value = CreateText(card, "Value", "0", 24f, FontStyles.Bold, accent);
            value.alignment = TextAlignmentOptions.Center;
            value.fontSizeMin = 22f;
            value.rectTransform.anchorMin = new Vector2(0f, 0.05f);
            value.rectTransform.anchorMax = new Vector2(1f, 0.58f);
            value.rectTransform.offsetMin = new Vector2(6f, 0f);
            value.rectTransform.offsetMax = new Vector2(-6f, 0f);
            return value;
        }

        private TextMeshProUGUI CreateScoreReadout(Transform parent)
        {
            RectTransform card = CreatePanel(parent, "ScoreReadout", new Color(0.018f, 0.045f, 0.062f, 0.88f));
            ApplyUiSprite(card, UiSprite.StatGold, 54f);
            card.anchorMin = new Vector2(0.125f, 0.795f);
            card.anchorMax = new Vector2(0.875f, 0.905f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            scoreLabelText = CreateText(card, "Label", GameLocalization.Text("symbigrid.score"), 21f, FontStyles.Bold, MutedInk);
            scoreLabelText.alignment = TextAlignmentOptions.Center;
            scoreLabelText.fontSizeMin = 18f;
            scoreLabelText.rectTransform.anchorMin = new Vector2(0f, 0.62f);
            scoreLabelText.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            scoreLabelText.rectTransform.offsetMin = new Vector2(12f, 0f);
            scoreLabelText.rectTransform.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI value = CreateText(card, "Value", "0", 76f, FontStyles.Bold, Gold);
            value.alignment = TextAlignmentOptions.Center;
            value.fontSizeMin = 52f;
            value.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            value.rectTransform.anchorMax = new Vector2(1f, 0.70f);
            value.rectTransform.offsetMin = new Vector2(14f, 0f);
            value.rectTransform.offsetMax = new Vector2(-14f, 0f);
            return value;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18f, size * 0.72f);
            text.fontSizeMax = size;
            text.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float fontSize, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMax.x >= 1f ? 1f : anchorMin.x <= 0f ? 0f : 0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Button button = go.GetComponent<Button>();
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.09f, 0.16f, 0.25f, 1f);
            ApplyUiSprite(image, UiSprite.Button, 42f);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.80f, 0.92f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.78f, 0.96f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, FontStyles.Bold, Ink);
                Stretch(text.rectTransform);
                text.fontSizeMin = Mathf.Max(20f, fontSize * 0.82f);
                text.rectTransform.offsetMin = new Vector2(14f, 4f);
                text.rectTransform.offsetMax = new Vector2(-14f, -4f);
            }

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    PlaySound(buttonClip, 0.52f, 1f);
                    action();
                });
            }

            return button;
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = value;
        }

        private static void AddSignatureFrame(RectTransform parent, Color primary, Color accent)
        {
            if (parent == null)
                return;

            RectTransform top = CreateDecor(parent, "SignatureTop", primary);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0f, 3f);

            RectTransform bottom = CreateDecor(parent, "SignatureBottom", new Color(primary.r, primary.g, primary.b, primary.a * 0.7f));
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(0f, 2f);

            RectTransform node = CreateDecor(parent, "SignatureNode", accent);
            node.anchorMin = new Vector2(0.5f, 1f);
            node.anchorMax = new Vector2(0.5f, 1f);
            node.pivot = new Vector2(0.5f, 0.5f);
            node.anchoredPosition = new Vector2(0f, -1f);
            node.sizeDelta = new Vector2(58f, 5f);
        }

        private static void AddCornerBrackets(RectTransform parent, Color primary, Color accent)
        {
            if (parent == null)
                return;

            AddBracket(parent, "TL", new Vector2(0f, 1f), new Vector2(1f, -1f), primary, accent);
            AddBracket(parent, "TR", new Vector2(1f, 1f), new Vector2(-1f, -1f), primary, accent);
            AddBracket(parent, "BL", new Vector2(0f, 0f), new Vector2(1f, 1f), primary, accent);
            AddBracket(parent, "BR", new Vector2(1f, 0f), new Vector2(-1f, 1f), primary, accent);
        }

        private static void AddBracket(RectTransform parent, string name, Vector2 anchor, Vector2 direction, Color primary, Color accent)
        {
            RectTransform horizontal = CreateDecor(parent, "Bracket" + name + "H", primary);
            horizontal.anchorMin = anchor;
            horizontal.anchorMax = anchor;
            horizontal.pivot = new Vector2(anchor.x, anchor.y);
            horizontal.anchoredPosition = new Vector2(direction.x * 10f, direction.y * 10f);
            horizontal.sizeDelta = new Vector2(74f, 4f);

            RectTransform vertical = CreateDecor(parent, "Bracket" + name + "V", primary);
            vertical.anchorMin = anchor;
            vertical.anchorMax = anchor;
            vertical.pivot = new Vector2(anchor.x, anchor.y);
            vertical.anchoredPosition = new Vector2(direction.x * 10f, direction.y * 10f);
            vertical.sizeDelta = new Vector2(4f, 74f);

            RectTransform dot = CreateDecor(parent, "Bracket" + name + "Dot", accent);
            dot.anchorMin = anchor;
            dot.anchorMax = anchor;
            dot.pivot = new Vector2(anchor.x, anchor.y);
            dot.anchoredPosition = new Vector2(direction.x * 8f, direction.y * 8f);
            dot.sizeDelta = new Vector2(12f, 12f);
        }

        private static void AddTitleRule(RectTransform parent, Color color)
        {
            if (parent == null)
                return;

            RectTransform rule = CreateDecor(parent, "TitleRule", color);
            rule.anchorMin = new Vector2(0.08f, 0.02f);
            rule.anchorMax = new Vector2(0.92f, 0.02f);
            rule.pivot = new Vector2(0.5f, 0f);
            rule.anchoredPosition = Vector2.zero;
            rule.sizeDelta = new Vector2(0f, 2f);
        }

        private static void AddButtonAccent(RectTransform parent)
        {
            if (parent == null)
                return;

            RectTransform strip = CreateDecor(parent, "ButtonAccent", new Color(0.14f, 0.88f, 0.76f, 0.78f));
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.anchoredPosition = Vector2.zero;
            strip.sizeDelta = new Vector2(5f, 0f);

            RectTransform shine = CreateDecor(parent, "ButtonShine", new Color(1f, 0.76f, 0.22f, 0.24f));
            shine.anchorMin = new Vector2(0.08f, 1f);
            shine.anchorMax = new Vector2(0.92f, 1f);
            shine.pivot = new Vector2(0.5f, 1f);
            shine.anchoredPosition = Vector2.zero;
            shine.sizeDelta = new Vector2(0f, 2f);
        }

        private static void AddOutline(RectTransform rect, Color color, Vector2 distance)
        {
            if (rect == null)
                return;

            Outline outline = rect.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = rect.gameObject.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void ApplyUiSprite(RectTransform rect, UiSprite sprite, float border = 42f)
        {
            if (rect == null)
                return;

            ApplyUiSprite(rect.GetComponent<Image>(), sprite, border);
        }

        private static void ApplyUiSprite(Image image, UiSprite sprite, float border = 42f)
        {
            if (image == null)
                return;

            Sprite visual = GetUiSprite(sprite, border);
            if (visual == null)
                return;

            image.sprite = visual;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        private static Sprite GetUiSprite(UiSprite sprite, float border)
        {
            if (uiSprites.TryGetValue(sprite, out Sprite cached) && cached != null)
                return cached;

            Texture2D atlas = Resources.Load<Texture2D>(UiAtlasResourcePath);
            if (atlas == null)
                return null;

            Rect rect = GetUiAtlasRect(sprite, atlas.height);
            Vector4 spriteBorder = new Vector4(border, border, border, border);
            Sprite created = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, spriteBorder);
            uiSprites[sprite] = created;
            return created;
        }

        private static Rect GetUiAtlasRect(UiSprite sprite, int atlasHeight)
        {
            switch (sprite)
            {
                case UiSprite.TopHud:
                    return FromAtlasTopLeft(48f, 86f, 698f, 170f, atlasHeight);
                case UiSprite.StatGold:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
                case UiSprite.StatCyan:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
                case UiSprite.StatGreen:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
                case UiSprite.GoalBar:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
                case UiSprite.BoardFrame:
                    return FromAtlasTopLeft(33f, 293f, 547f, 563f, atlasHeight);
                case UiSprite.TrayPanel:
                    return FromAtlasTopLeft(34f, 914f, 546f, 179f, atlasHeight);
                case UiSprite.PieceCard:
                    return FromAtlasTopLeft(607f, 304f, 124f, 341f, atlasHeight);
                case UiSprite.Button:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
                case UiSprite.Modal:
                    return FromAtlasTopLeft(620f, 987f, 378f, 368f, atlasHeight);
                case UiSprite.BarTrack:
                    return FromAtlasTopLeft(45f, 1129f, 530f, 58f, atlasHeight);
                case UiSprite.BarFillCyan:
                    return FromAtlasTopLeft(47f, 1301f, 527f, 52f, atlasHeight);
                case UiSprite.BarFillGold:
                    return FromAtlasTopLeft(47f, 1380f, 527f, 52f, atlasHeight);
                default:
                    return FromAtlasTopLeft(607f, 690f, 392f, 114f, atlasHeight);
            }
        }

        private static Rect FromAtlasTopLeft(float x, float y, float width, float height, int atlasHeight)
        {
            return new Rect(x, atlasHeight - y - height, width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }

    internal sealed class BoardCellSizer : MonoBehaviour
    {
        private GridLayoutGroup grid;
        private RectTransform rect;
        private Vector2 lastSize;
        private int rows = 8;
        private int cols = 8;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rect = transform as RectTransform;
            Apply();
        }

        private void Update()
        {
            if (rect == null || rect.rect.size == lastSize)
                return;

            Apply();
        }

        private void Apply()
        {
            if (grid == null || rect == null)
                return;

            lastSize = rect.rect.size;
            int safeRows = Mathf.Max(1, rows);
            int safeCols = Mathf.Max(1, cols);
            float width = Mathf.Max(1f, rect.rect.width - grid.padding.left - grid.padding.right - grid.spacing.x * Mathf.Max(0, safeCols - 1));
            float height = Mathf.Max(1f, rect.rect.height - grid.padding.top - grid.padding.bottom - grid.spacing.y * Mathf.Max(0, safeRows - 1));
            float cell = Mathf.Floor(Mathf.Min(width / safeCols, height / safeRows));
            grid.cellSize = new Vector2(cell, cell);
        }

        public void Configure(int activeRows, int activeCols)
        {
            rows = Mathf.Max(1, activeRows);
            cols = Mathf.Max(1, activeCols);
            lastSize = Vector2.zero;
            Apply();
        }
    }

    internal sealed class CellBlockFitter : MonoBehaviour
    {
        public RectTransform Block;

        private RectTransform rect;
        private Vector2 lastSize;

        private void Awake()
        {
            rect = transform as RectTransform;
            Apply();
        }

        private void Update()
        {
            if (rect == null || rect.rect.size == lastSize)
                return;

            Apply();
        }

        private void Apply()
        {
            if (rect == null || Block == null)
                return;

            lastSize = rect.rect.size;
            float blockSize = Mathf.Max(1f, Mathf.Floor(Mathf.Min(lastSize.x, lastSize.y)));
            Block.anchorMin = new Vector2(0.5f, 0.5f);
            Block.anchorMax = new Vector2(0.5f, 0.5f);
            Block.pivot = new Vector2(0.5f, 0.5f);
            Block.anchoredPosition = Vector2.zero;
            Block.sizeDelta = new Vector2(blockSize, blockSize);
        }
    }
}
