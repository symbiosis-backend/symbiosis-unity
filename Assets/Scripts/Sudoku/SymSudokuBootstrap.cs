using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame.Sudoku
{
    [DisallowMultipleComponent]
    public sealed class SymSudokuBootstrap : MonoBehaviour
    {
        private enum Difficulty
        {
            Easy,
            Medium,
            Hard
        }

        private const int Size = 9;
        private const int Box = 3;
        private const int CellCount = Size * Size;
        private const int LevelsPerDifficulty = 1000;
        private const int AdCardInterval = 20;
        private const int LevelCarouselWindow = 12;
        private const int GeneratorAttempts = 96;
        private const int HintsPerRewardedAd = 3;
        private const int FreeUndoCreditsPerLevel = 3;
        private const int UndoCreditsPerRewardedAd = 3;
        private const int InterstitialEveryCompletedGames = 3;
        private const float SudokuInterstitialWarmupTimeoutSeconds = 3f;
        private const float LevelCardWidth = 620f;
        private const float LevelCardSpacing = 34f;
        private const string ProgressPrefix = "SymSudoku.Progress.";
        private const string CompletedGamesKey = "SymSudoku.CompletedGames";
        private const string PhilosopherFontResourcePath = "Fonts/Philosopher-Regular";
        private static readonly Vector2 SudokuLandscapeReferenceResolution = new Vector2(1600f, 900f);
        private const float SudokuLandscapeMatchWidthOrHeight = 0.5f;
        private const float SudokuPortraitMatchWidthOrHeight = 0.5f;

        private struct DifficultyProfile
        {
            public int MinGivens;
            public int MaxGivens;
            public int MinScore;
            public int MaxScore;
        }

        private struct PuzzleRating
        {
            public int Givens;
            public int EmptyCells;
            public int Score;
            public int Singles;
            public int HiddenSingles;
            public int NakedPairs;
            public int SearchPressure;
            public string TechniqueLabel;
        }

        private struct MoveState
        {
            public int Row;
            public int Col;
            public int Value;
            public bool[] Notes;
        }

        private readonly int[,] puzzle = new int[Size, Size];
        private readonly int[,] solution = new int[Size, Size];
        private readonly int[,] entries = new int[Size, Size];
        private readonly bool[,] locked = new bool[Size, Size];
        private readonly bool[,,] notes = new bool[Size, Size, Size + 1];
        private readonly Stack<MoveState> undoStack = new Stack<MoveState>();
        private System.Random random = new System.Random();

        private Canvas canvas;
        private RectTransform lobbyRoot;
        private RectTransform lobbyHeader;
        private RectTransform compactMenuPanel;
        private RectTransform leaderboardPanel;
        private RectTransform levelSelectRoot;
        private RectTransform levelTopBar;
        private RectTransform carouselFrame;
        private RectTransform levelCarouselContent;
        private TextMeshProUGUI levelSelectTitle;
        private TextMeshProUGUI leaderboardText;
        private RectTransform gameRoot;
        private RectTransform gameTopBar;
        private RectTransform controlPanel;
        private RectTransform boardOuter;
        private RectTransform numberPad;
        private RectTransform gameActions;
        private readonly Button[,] cellButtons = new Button[Size, Size];
        private readonly TextMeshProUGUI[,] cellLabels = new TextMeshProUGUI[Size, Size];
        private readonly TextMeshProUGUI[,] noteLabels = new TextMeshProUGUI[Size, Size];
        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI difficultyLabel;
        private TextMeshProUGUI noteToggleLabel;
        private TextMeshProUGUI undoButtonLabel;
        private TextMeshProUGUI hintButtonLabel;
        private TextMeshProUGUI timerLabel;
        private Button undoButton;
        private Button hintButton;
        private PuzzleRating currentPuzzleRating;
        private Difficulty currentLevelSelectDifficulty = Difficulty.Easy;
        private int levelRangeStart = 1;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private static TMP_FontAsset philosopherFont;
        private static Sprite softPanelSprite;
        private static Sprite softButtonSprite;

        private Difficulty currentDifficulty = Difficulty.Easy;
        private int currentLevel = 1;
        private float levelStartTime;
        private int lastElapsedSeconds;
        private int selectedRow = -1;
        private int selectedCol = -1;
        private int mistakes;
        private int hintCredits;
        private int undoCredits;
        private bool noteMode;
        private bool completed;
        private bool rewardedHintRequestInProgress;
        private bool rewardedUndoRequestInProgress;
        private bool interstitialRequestInProgress;
        private ScreenOrientation lastScreenOrientation;

        private void OnEnable()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalizedRuntimeText();
        }

        private void Update()
        {
            EnsureSudokuAutoRotation();
            RefreshResponsiveLayoutIfNeeded();

            if (gameRoot == null || !gameRoot.gameObject.activeSelf || completed)
                return;

            int elapsed = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - levelStartTime));
            if (elapsed == lastElapsedSeconds)
                return;

            lastElapsedSeconds = elapsed;
            if (timerLabel != null)
                timerLabel.text = FormatTime(elapsed);
        }

        private static readonly Color Background = new Color(0.965f, 0.953f, 0.925f, 1f);
        private static readonly Color Panel = new Color(0.992f, 0.984f, 0.965f, 0.98f);
        private static readonly Color GridLine = new Color(0.18f, 0.22f, 0.23f, 1f);
        private static readonly Color Cell = new Color(0.995f, 0.989f, 0.974f, 1f);
        private static readonly Color CellAlt = new Color(0.954f, 0.969f, 0.958f, 1f);
        private static readonly Color CellSelected = new Color(0.64f, 0.78f, 0.76f, 1f);
        private static readonly Color CellRelated = new Color(0.885f, 0.927f, 0.914f, 1f);
        private static readonly Color CellConflict = new Color(0.965f, 0.755f, 0.705f, 1f);
        private static readonly Color Ink = new Color(0.105f, 0.135f, 0.14f, 1f);
        private static readonly Color GivenInk = new Color(0.07f, 0.09f, 0.095f, 1f);
        private static readonly Color PlayerInk = new Color(0.09f, 0.39f, 0.37f, 1f);
        private static readonly Color MutedInk = new Color(0.43f, 0.48f, 0.47f, 1f);
        private static readonly Color Accent = new Color(0.25f, 0.58f, 0.55f, 1f);
        private static readonly Color Gold = new Color(0.82f, 0.58f, 0.24f, 1f);

        private void Awake()
        {
            EnableSudokuAutoRotation();
            RemoveForeignMainEntryButtons();
            EnsureCamera();
            EnsureEventSystem();
            BuildInterface();
            ShowLobby();
        }

        private static void EnableSudokuAutoRotation()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private static void EnsureSudokuAutoRotation()
        {
            if (!Screen.autorotateToPortrait)
                Screen.autorotateToPortrait = true;
            if (!Screen.autorotateToPortraitUpsideDown)
                Screen.autorotateToPortraitUpsideDown = true;
            if (!Screen.autorotateToLandscapeLeft)
                Screen.autorotateToLandscapeLeft = true;
            if (!Screen.autorotateToLandscapeRight)
                Screen.autorotateToLandscapeRight = true;
            if (Screen.orientation != ScreenOrientation.AutoRotation)
                Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private static void RemoveForeignMainEntryButtons()
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null)
                    continue;

                string text = label.text != null ? label.text.Trim() : string.Empty;
                if (string.Equals(text, "OZ LOBBY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "ÖzGame", StringComparison.OrdinalIgnoreCase))
                {
                    Destroy(button.gameObject);
                }
            }
        }

        private void EnsureCamera()
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

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("SymSudokuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = SudokuLandscapeReferenceResolution;
            scaler.matchWidthOrHeight = SudokuLandscapeMatchWidthOrHeight;

            Image backdrop = CreateImage(canvasObject.transform, "Backdrop", Background);
            Stretch(backdrop.rectTransform);

            lobbyRoot = CreatePanel(canvasObject.transform, "LobbyRoot", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(lobbyRoot);
            levelSelectRoot = CreatePanel(canvasObject.transform, "LevelSelectRoot", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(levelSelectRoot);
            gameRoot = CreatePanel(canvasObject.transform, "GameRoot", new Color(0f, 0f, 0f, 0f)).rectTransform;
            Stretch(gameRoot);

            BuildLobby();
            BuildLevelSelect();
            BuildGame();
        }

        private void BuildLobby()
        {
            lobbyHeader = CreatePanel(lobbyRoot, "Header", Panel).rectTransform;
            LayoutTopCenter(lobbyHeader, new Vector2(0f, -34f), new Vector2(1120f, 92f));
            HorizontalLayoutGroup headerLayout = lobbyHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(22, 22, 14, 14);
            headerLayout.spacing = 14f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;

            TextMeshProUGUI title = CreateLocalizedText(lobbyHeader, "Title", "sudoku.title", 50f, Ink, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            AddLayout(title.gameObject, 360f, -1f, 1f);

            CreateActionButton(lobbyHeader, "sudoku.leaderboard", ShowLeaderboard);
            CreateActionButton(lobbyHeader, "sudoku.menu", ReturnToMainMenu);

            compactMenuPanel = CreateSoftPanel(lobbyRoot, "CompactMenu", Panel, 24f).rectTransform;
            LayoutCentered(compactMenuPanel, new Vector2(-405f, -30f), new Vector2(620f, 640f));
            VerticalLayoutGroup compactLayout = compactMenuPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            compactLayout.padding = new RectOffset(28, 28, 26, 26);
            compactLayout.spacing = 18f;
            compactLayout.childAlignment = TextAnchor.UpperCenter;
            compactLayout.childControlWidth = true;
            compactLayout.childControlHeight = false;
            compactLayout.childForceExpandWidth = true;

            TextMeshProUGUI subtitle = CreateLocalizedText(compactMenuPanel, "Subtitle", "sudoku.subtitle", 30f, MutedInk, TextAlignmentOptions.Center);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 18f;
            AddLayout(subtitle.gameObject, -1f, 104f);

            CreateDifficultyButton(compactMenuPanel, "sudoku.easy", "sudoku.easy.desc", Difficulty.Easy);
            CreateDifficultyButton(compactMenuPanel, "sudoku.medium", "sudoku.medium.desc", Difficulty.Medium);
            CreateDifficultyButton(compactMenuPanel, "sudoku.hard", "sudoku.hard.desc", Difficulty.Hard);

            leaderboardPanel = CreateSoftPanel(lobbyRoot, "LeaderboardPanel", Panel, 24f).rectTransform;
            LayoutCentered(leaderboardPanel, new Vector2(405f, -30f), new Vector2(620f, 640f));
            VerticalLayoutGroup leaderboardLayout = leaderboardPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            leaderboardLayout.padding = new RectOffset(30, 30, 28, 28);
            leaderboardLayout.spacing = 16f;
            leaderboardLayout.childAlignment = TextAnchor.UpperCenter;
            leaderboardLayout.childControlWidth = true;
            leaderboardLayout.childControlHeight = false;

            TextMeshProUGUI leaderboardTitle = CreateLocalizedText(leaderboardPanel, "LeaderboardTitle", "sudoku.leaderboard", 40f, Ink, TextAlignmentOptions.Center);
            leaderboardTitle.fontStyle = FontStyles.Bold;
            AddLayout(leaderboardTitle.gameObject, -1f, 60f);

            leaderboardText = CreateText(leaderboardPanel, "LeaderboardText", string.Empty, 28f, MutedInk, TextAlignmentOptions.TopLeft);
            leaderboardText.enableAutoSizing = true;
            leaderboardText.fontSizeMin = 18f;
            AddLayout(leaderboardText.gameObject, -1f, 300f);

            TextMeshProUGUI adText = CreateLocalizedText(leaderboardPanel, "AdSlot", "sudoku.status.ad", 25f, Gold, TextAlignmentOptions.Center);
            adText.enableAutoSizing = true;
            adText.fontSizeMin = 16f;
            AddLayout(adText.gameObject, -1f, 110f);

            RefreshLeaderboard();
        }

        private void BuildLevelSelect()
        {
            Image shade = levelSelectRoot.GetComponent<Image>();
            if (shade != null)
                shade.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);

            levelTopBar = CreateSoftPanel(levelSelectRoot, "LevelTopBar", Panel, 18f).rectTransform;
            LayoutTopCenter(levelTopBar, new Vector2(0f, -32f), new Vector2(1320f, 92f));
            HorizontalLayoutGroup topLayout = levelTopBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(20, 20, 14, 14);
            topLayout.spacing = 16f;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;

            CreateActionButton(levelTopBar, "sudoku.back", ShowLobby);
            CreateActionButton(levelTopBar, "< 12", PreviousLevelRange);
            levelSelectTitle = CreateText(levelTopBar, "Title", string.Empty, 34f, Ink, TextAlignmentOptions.Center);
            levelSelectTitle.fontStyle = FontStyles.Bold;
            AddLayout(levelSelectTitle.gameObject, 620f, -1f, 1f);
            CreateActionButton(levelTopBar, "12 >", NextLevelRange);
            CreateActionButton(levelTopBar, "sudoku.ad", ShowAdPlaceholder);

            carouselFrame = CreateSoftPanel(levelSelectRoot, "CarouselFrame", Panel, 28f).rectTransform;
            LayoutCentered(carouselFrame, new Vector2(0f, -38f), new Vector2(1440f, 650f));

            GameObject viewportObject = CreatePanel(carouselFrame, "Viewport", new Color(0.95f, 0.97f, 0.99f, 1f)).gameObject;
            RectTransform viewport = viewportObject.transform as RectTransform;
            Stretch(viewport, 24f);
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            levelCarouselContent = contentObject.transform as RectTransform;
            levelCarouselContent.anchorMin = new Vector2(0f, 0f);
            levelCarouselContent.anchorMax = new Vector2(0f, 1f);
            levelCarouselContent.pivot = new Vector2(0f, 0.5f);
            levelCarouselContent.anchoredPosition = Vector2.zero;
            levelCarouselContent.sizeDelta = new Vector2(2600f, 0f);

            HorizontalLayoutGroup contentLayout = contentObject.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(28, 28, 36, 36);
            contentLayout.spacing = LevelCardSpacing;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;

            ScrollRect scroll = carouselFrame.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = levelCarouselContent;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.scrollSensitivity = 38f;
        }

        private void BuildGame()
        {
            gameTopBar = CreateSoftPanel(gameRoot, "TopBar", Panel, 8f).rectTransform;
            LayoutTopCenter(gameTopBar, new Vector2(0f, -30f), new Vector2(1380f, 64f));
            HorizontalLayoutGroup topLayout = gameTopBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(14, 14, 10, 10);
            topLayout.spacing = 10f;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;

            CreateActionButton(gameTopBar, "sudoku.lobby", ShowLobby);
            CreateActionButton(gameTopBar, "sudoku.menu", ReturnToMainMenu);
            difficultyLabel = CreateText(gameTopBar, "DifficultyLabel", string.Empty, 28f, Ink, TextAlignmentOptions.Center);
            AddLayout(difficultyLabel.gameObject, 260f, -1f, 1f);
            timerLabel = CreateText(gameTopBar, "TimerLabel", "00:00", 28f, Gold, TextAlignmentOptions.Center);
            timerLabel.fontStyle = FontStyles.Bold;
            AddLayout(timerLabel.gameObject, 150f, -1f);
            CreateActionButton(gameTopBar, "sudoku.new", () => StartGame(currentDifficulty, currentLevel));

            controlPanel = CreateSoftPanel(gameRoot, "ControlPanel", Panel, 8f).rectTransform;
            LayoutCentered(controlPanel, new Vector2(514f, -38f), new Vector2(392f, 672f));
            VerticalLayoutGroup leftLayout = controlPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(22, 22, 20, 20);
            leftLayout.spacing = 12f;
            leftLayout.childAlignment = TextAnchor.UpperCenter;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = false;

            TextMeshProUGUI infoTitle = CreateLocalizedText(controlPanel, "InfoTitle", "sudoku.title", 34f, Ink, TextAlignmentOptions.Center);
            infoTitle.fontStyle = FontStyles.Bold;
            AddLayout(infoTitle.gameObject, -1f, 44f);

            TextMeshProUGUI rules = CreateLocalizedText(controlPanel, "Rules", "sudoku.rules", 24f, MutedInk, TextAlignmentOptions.Center);
            rules.enableAutoSizing = true;
            rules.fontSizeMin = 16f;
            AddLayout(rules.gameObject, -1f, 58f);

            statusLabel = CreateText(controlPanel, "Status", string.Empty, 23f, PlayerInk, TextAlignmentOptions.Center);
            statusLabel.enableAutoSizing = true;
            statusLabel.fontSizeMin = 15f;
            AddLayout(statusLabel.gameObject, -1f, 84f);

            gameActions = CreatePanel(controlPanel, "Actions", new Color(0f, 0f, 0f, 0f)).rectTransform;
            AddLayout(gameActions.gameObject, -1f, 138f);
            GridLayoutGroup actionLayout = gameActions.gameObject.AddComponent<GridLayoutGroup>();
            actionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            actionLayout.constraintCount = 2;
            actionLayout.spacing = new Vector2(10f, 10f);
            actionLayout.cellSize = new Vector2(164f, 58f);

            undoButton = CreateButton(gameActions, "SudokuUndoButton", T("sudoku.undo"), Accent, Color.white);
            undoButton.onClick.AddListener(OnUndoButtonPressed);
            undoButtonLabel = undoButton.GetComponentInChildren<TextMeshProUGUI>();
            CreateActionButton(gameActions, "sudoku.erase", EraseSelected);
            Button notesButton = CreateActionButton(gameActions, "sudoku.notes_off", ToggleNotes);
            noteToggleLabel = notesButton.GetComponentInChildren<TextMeshProUGUI>();
            hintButton = CreateButton(gameActions, "SudokuHintButton", T("sudoku.hint_ad"), Accent, Color.white);
            hintButton.onClick.AddListener(OnHintButtonPressed);
            hintButtonLabel = hintButton.GetComponentInChildren<TextMeshProUGUI>();

            boardOuter = CreateSoftPanel(gameRoot, "BoardOuter", GridLine, 8f).rectTransform;
            LayoutCentered(boardOuter, new Vector2(-252f, -38f), new Vector2(672f, 672f));
            GridLayoutGroup boardLayout = boardOuter.gameObject.AddComponent<GridLayoutGroup>();
            boardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boardLayout.constraintCount = Size;
            boardLayout.spacing = new Vector2(2f, 2f);
            boardLayout.padding = new RectOffset(7, 7, 7, 7);
            boardLayout.cellSize = new Vector2(72f, 72f);

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    int capturedRow = row;
                    int capturedCol = col;
                    Button button = CreateBoardCell(boardOuter, row, col);
                    button.onClick.AddListener(() => SelectCell(capturedRow, capturedCol));
                    cellButtons[row, col] = button;
                }
            }

            numberPad = CreatePanel(controlPanel, "NumberPad", new Color(0f, 0f, 0f, 0f)).rectTransform;
            AddLayout(numberPad.gameObject, -1f, 276f);
            GridLayoutGroup padLayout = numberPad.gameObject.AddComponent<GridLayoutGroup>();
            padLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            padLayout.constraintCount = 3;
            padLayout.spacing = new Vector2(10f, 10f);
            padLayout.cellSize = new Vector2(104f, 78f);

            for (int number = 1; number <= Size; number++)
            {
                int capturedNumber = number;
                Button button = CreateButton(numberPad, "Number" + number, number.ToString(), new Color(0.92f, 0.95f, 0.93f, 1f), PlayerInk);
                button.onClick.AddListener(() => ApplyNumber(capturedNumber));
            }

            ApplyGameResponsiveLayout(IsPortraitOrientation());
        }

        private void RefreshResponsiveLayoutIfNeeded()
        {
            if (lobbyRoot == null || levelSelectRoot == null || gameRoot == null)
                return;

            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight && Screen.orientation == lastScreenOrientation)
                return;

            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            bool portrait = IsPortraitOrientation();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastScreenOrientation = Screen.orientation;

            ApplyCanvasScale(portrait);

            if (lobbyRoot.gameObject.activeSelf)
                ApplyLobbyResponsiveLayout(portrait);
            else if (levelSelectRoot.gameObject.activeSelf)
                ApplyLevelSelectResponsiveLayout(portrait);
            else if (gameRoot.gameObject.activeSelf)
                ApplyGameResponsiveLayout(portrait);

            Canvas.ForceUpdateCanvases();
        }

        private void ApplyCanvasScale(bool portrait)
        {
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler == null)
                return;

            scaler.referenceResolution = portrait ? new Vector2(1080f, 1920f) : SudokuLandscapeReferenceResolution;
            scaler.matchWidthOrHeight = portrait ? SudokuPortraitMatchWidthOrHeight : SudokuLandscapeMatchWidthOrHeight;
        }

        private void ApplyLobbyResponsiveLayout(bool portrait)
        {
            if (lobbyHeader == null || compactMenuPanel == null || leaderboardPanel == null)
                return;

            if (portrait)
            {
                for (int i = 0; i < lobbyHeader.childCount; i++)
                {
                    Transform child = lobbyHeader.GetChild(i);
                    LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                    if (child.name == "Title")
                    {
                        layout.preferredWidth = 360f;
                        layout.flexibleWidth = 1f;
                    }
                    else if (child.GetComponent<Button>() != null)
                    {
                        layout.preferredWidth = 250f;
                        layout.flexibleWidth = 0f;
                    }
                }

                LayoutTopCenter(lobbyHeader, new Vector2(0f, -58f), new Vector2(1040f, 124f));
                LayoutCentered(compactMenuPanel, new Vector2(0f, 395f), new Vector2(1000f, 640f));
                LayoutCentered(leaderboardPanel, new Vector2(0f, -330f), new Vector2(1000f, 650f));
                TuneLobbyPanelText(true);
            }
            else
            {
                for (int i = 0; i < lobbyHeader.childCount; i++)
                {
                    Transform child = lobbyHeader.GetChild(i);
                    LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                    if (child.name == "Title")
                    {
                        layout.preferredWidth = 360f;
                        layout.flexibleWidth = 1f;
                    }
                    else if (child.GetComponent<Button>() != null)
                    {
                        layout.preferredWidth = 190f;
                        layout.flexibleWidth = 0f;
                    }
                }

                LayoutTopCenter(lobbyHeader, new Vector2(0f, -34f), new Vector2(1120f, 92f));
                LayoutCentered(compactMenuPanel, new Vector2(-405f, -30f), new Vector2(620f, 640f));
                LayoutCentered(leaderboardPanel, new Vector2(405f, -30f), new Vector2(620f, 640f));
                TuneLobbyPanelText(false);
            }
        }

        private void ApplyLevelSelectResponsiveLayout(bool portrait)
        {
            if (levelTopBar == null || carouselFrame == null)
                return;

            if (portrait)
            {
                for (int i = 0; i < levelTopBar.childCount; i++)
                {
                    Transform child = levelTopBar.GetChild(i);
                    LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                    if (child.name == "Title")
                    {
                        layout.preferredWidth = 340f;
                        layout.flexibleWidth = 1f;
                    }
                    else if (child.GetComponent<Button>() != null)
                    {
                        layout.preferredWidth = 135f;
                        layout.flexibleWidth = 0f;
                    }
                }

                LayoutTopCenter(levelTopBar, new Vector2(0f, -24f), new Vector2(1010f, 104f));
                LayoutCentered(carouselFrame, new Vector2(0f, -80f), new Vector2(1010f, 1560f));
            }
            else
            {
                for (int i = 0; i < levelTopBar.childCount; i++)
                {
                    Transform child = levelTopBar.GetChild(i);
                    LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                    if (child.name == "Title")
                    {
                        layout.preferredWidth = 620f;
                        layout.flexibleWidth = 1f;
                    }
                    else if (child.GetComponent<Button>() != null)
                    {
                        layout.preferredWidth = 190f;
                        layout.flexibleWidth = 0f;
                    }
                }

                LayoutTopCenter(levelTopBar, new Vector2(0f, -32f), new Vector2(1320f, 92f));
                LayoutCentered(carouselFrame, new Vector2(0f, -38f), new Vector2(1440f, 650f));
            }
        }

        private void TuneLobbyPanelText(bool portrait)
        {
            if (compactMenuPanel != null)
            {
                Transform subtitle = compactMenuPanel.Find("Subtitle");
                if (subtitle != null)
                {
                    TextMeshProUGUI label = subtitle.GetComponent<TextMeshProUGUI>();
                    LayoutElement layout = subtitle.GetComponent<LayoutElement>();
                    if (label != null)
                    {
                        label.enableAutoSizing = false;
                        label.fontSize = portrait ? 36f : 30f;
                        label.alignment = TextAlignmentOptions.Center;
                    }
                    if (layout != null)
                        layout.preferredHeight = portrait ? 96f : 104f;
                }

                for (int i = 0; i < compactMenuPanel.childCount; i++)
                {
                    Transform child = compactMenuPanel.GetChild(i);
                    Button button = child.GetComponent<Button>();
                    if (button == null)
                        continue;

                    LayoutElement layout = child.GetComponent<LayoutElement>();
                    if (layout != null)
                        layout.preferredHeight = portrait ? 136f : 104f;

                    TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.enableAutoSizing = true;
                        label.fontSize = portrait ? 44f : 28f;
                        label.fontSizeMin = portrait ? 34f : 18f;
                        label.fontSizeMax = portrait ? 50f : 32f;
                        label.lineSpacing = portrait ? 0f : 14f;
                    }
                }
            }

            if (leaderboardPanel != null)
            {
                Transform title = leaderboardPanel.Find("LeaderboardTitle");
                if (title != null)
                {
                    TextMeshProUGUI label = title.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                        label.fontSize = portrait ? 52f : 40f;
                }

                if (leaderboardText != null)
                {
                    leaderboardText.enableAutoSizing = false;
                    leaderboardText.fontSize = portrait ? 36f : 28f;
                    leaderboardText.alignment = portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
                    leaderboardText.lineSpacing = portrait ? 6f : 0f;
                }

                Transform text = leaderboardPanel.Find("LeaderboardText");
                if (text != null)
                {
                    LayoutElement layout = text.GetComponent<LayoutElement>();
                    if (layout != null)
                        layout.preferredHeight = portrait ? 330f : 300f;
                }

                Transform ad = leaderboardPanel.Find("AdSlot");
                if (ad != null)
                {
                    TextMeshProUGUI label = ad.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.enableAutoSizing = false;
                        label.fontSize = portrait ? 32f : 25f;
                    }
                }
            }
        }

        private void ApplyGameResponsiveLayout()
        {
            ApplyResponsiveLayout();
        }

        private void ApplyGameResponsiveLayout(bool portrait)
        {
            if (canvas == null || gameTopBar == null || controlPanel == null || boardOuter == null || numberPad == null || gameActions == null)
                return;

            if (portrait)
                ApplyPortraitGameLayout();
            else
                ApplyLandscapeGameLayout();
        }

        private static bool IsPortraitOrientation()
        {
            return Screen.orientation == ScreenOrientation.Portrait
                || Screen.orientation == ScreenOrientation.PortraitUpsideDown
                || Screen.height > Screen.width;
        }

        private void ApplyLandscapeGameLayout()
        {
            for (int i = 0; i < gameTopBar.childCount; i++)
            {
                Transform child = gameTopBar.GetChild(i);
                LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                if (child.name == "DifficultyLabel")
                {
                    layout.preferredWidth = 430f;
                    layout.flexibleWidth = 1f;
                }
                else if (child.name == "TimerLabel")
                {
                    layout.preferredWidth = 130f;
                    layout.flexibleWidth = 0f;
                }
                else if (child.GetComponent<Button>() != null)
                {
                    layout.preferredWidth = 142f;
                    layout.flexibleWidth = 0f;
                }
            }

            LayoutTopCenter(gameTopBar, new Vector2(0f, -30f), new Vector2(1380f, 64f));
            LayoutCentered(boardOuter, new Vector2(-252f, -38f), new Vector2(672f, 672f));
            LayoutCentered(controlPanel, new Vector2(514f, -38f), new Vector2(392f, 672f));
            TuneGameText(false);
            SetParentKeepLocal(numberPad, controlPanel);
            SetParentKeepLocal(gameActions, controlPanel);

            GridLayoutGroup actionsGrid = gameActions.GetComponent<GridLayoutGroup>();
            if (actionsGrid != null)
            {
                actionsGrid.constraintCount = 2;
                actionsGrid.cellSize = new Vector2(164f, 58f);
                actionsGrid.spacing = new Vector2(10f, 10f);
            }

            AddLayout(gameActions.gameObject, -1f, 138f);
            AddLayout(numberPad.gameObject, -1f, 276f);
            GridLayoutGroup padGrid = numberPad.GetComponent<GridLayoutGroup>();
            if (padGrid != null)
            {
                padGrid.constraintCount = 3;
                padGrid.cellSize = new Vector2(104f, 78f);
                padGrid.spacing = new Vector2(10f, 10f);
            }

            GridLayoutGroup boardGrid = boardOuter.GetComponent<GridLayoutGroup>();
            if (boardGrid != null)
                boardGrid.cellSize = new Vector2(72f, 72f);
        }

        private void ApplyPortraitGameLayout()
        {
            for (int i = 0; i < gameTopBar.childCount; i++)
            {
                Transform child = gameTopBar.GetChild(i);
                LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                if (child.name == "DifficultyLabel")
                {
                    layout.preferredWidth = 240f;
                    layout.flexibleWidth = 1f;
                }
                else if (child.name == "TimerLabel")
                {
                    layout.preferredWidth = 130f;
                    layout.flexibleWidth = 0f;
                }
                else if (child.GetComponent<Button>() != null)
                {
                    layout.preferredWidth = 165f;
                    layout.flexibleWidth = 0f;
                }
            }

            LayoutTopCenter(gameTopBar, new Vector2(0f, -34f), new Vector2(1030f, 116f));
            LayoutCentered(boardOuter, new Vector2(0f, 395f), new Vector2(900f, 900f));
            LayoutCentered(controlPanel, new Vector2(0f, -575f), new Vector2(1030f, 830f));
            TuneGameText(true);
            SetParentKeepLocal(numberPad, controlPanel);
            SetParentKeepLocal(gameActions, controlPanel);

            GridLayoutGroup actionsGrid = gameActions.GetComponent<GridLayoutGroup>();
            if (actionsGrid != null)
            {
                actionsGrid.constraintCount = 4;
                actionsGrid.cellSize = new Vector2(235f, 86f);
            }

            AddLayout(gameActions.gameObject, -1f, 102f);
            AddLayout(numberPad.gameObject, -1f, 350f);
            GridLayoutGroup padGrid = numberPad.GetComponent<GridLayoutGroup>();
            if (padGrid != null)
            {
                padGrid.constraintCount = 9;
                padGrid.cellSize = new Vector2(102f, 108f);
                padGrid.spacing = new Vector2(8f, 10f);
            }

            GridLayoutGroup boardGrid = boardOuter.GetComponent<GridLayoutGroup>();
            if (boardGrid != null)
                boardGrid.cellSize = new Vector2(95f, 95f);
        }

        private void TuneGameText(bool portrait)
        {
            if (difficultyLabel != null)
            {
                difficultyLabel.enableAutoSizing = true;
                difficultyLabel.fontSize = portrait ? 30f : 24f;
                difficultyLabel.fontSizeMin = portrait ? 22f : 18f;
                difficultyLabel.fontSizeMax = portrait ? 34f : 28f;
            }

            if (timerLabel != null)
            {
                timerLabel.enableAutoSizing = false;
                timerLabel.fontSize = portrait ? 32f : 26f;
            }

            if (statusLabel != null)
            {
                statusLabel.enableAutoSizing = false;
                statusLabel.fontSize = portrait ? 32f : 20f;
                statusLabel.alignment = TextAlignmentOptions.Center;
            }

            if (controlPanel != null)
            {
                Transform title = controlPanel.Find("InfoTitle");
                if (title != null)
                {
                    TextMeshProUGUI label = title.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                        label.fontSize = portrait ? 48f : 34f;
                }

                Transform rules = controlPanel.Find("Rules");
                if (rules != null)
                {
                    TextMeshProUGUI label = rules.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.enableAutoSizing = false;
                        label.fontSize = portrait ? 31f : 20f;
                    }
                }
            }

            TuneButtonLabels(gameTopBar, portrait ? 31f : 21f, portrait ? 24f : 15f, portrait ? 36f : 24f);
            TuneButtonLabels(gameActions, portrait ? 31f : 20f, portrait ? 24f : 14f, portrait ? 36f : 23f);
            TuneButtonLabels(numberPad, portrait ? 52f : 34f, portrait ? 44f : 26f, portrait ? 60f : 38f);
        }

        private static void TuneButtonLabels(RectTransform root, float size, float min, float max)
        {
            if (root == null)
                return;

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].enableAutoSizing = true;
                labels[i].fontSize = size;
                labels[i].fontSizeMin = min;
                labels[i].fontSizeMax = max;
            }
        }

        private static void SetParentKeepLocal(RectTransform child, RectTransform parent)
        {
            if (child != null && parent != null && child.parent != parent)
                child.SetParent(parent, false);
        }

        private void CreateDifficultyButton(Transform parent, string titleKey, string descriptionKey, Difficulty difficulty)
        {
            string text = T(titleKey) + "\n" + T(descriptionKey);
            Button button = CreateButton(parent, DifficultyKey(difficulty) + "Button", text, Accent, Color.white);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 34f;
            label.fontSizeMin = 30f;
            label.fontSizeMax = 50f;
            label.lineSpacing = 0f;
            AddLayout(button.gameObject, -1f, 136f, 1f);
            button.onClick.AddListener(() => ShowLevelSelect(difficulty));
        }

        private Button CreateBoardCell(Transform parent, int row, int col)
        {
            GameObject cellObject = new GameObject("Cell_" + row + "_" + col, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cellObject.transform.SetParent(parent, false);

            Image image = cellObject.GetComponent<Image>();
            image.color = ((row / Box) + (col / Box)) % 2 == 0 ? Cell : CellAlt;

            Button button = cellObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = CellRelated;
            colors.pressedColor = CellSelected;
            colors.selectedColor = CellSelected;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI value = CreateText(cellObject.transform, "Value", string.Empty, 42f, Ink, TextAlignmentOptions.Center);
            Stretch(value.rectTransform);
            value.fontStyle = FontStyles.Bold;
            value.raycastTarget = false;
            cellLabels[row, col] = value;

            TextMeshProUGUI note = CreateText(cellObject.transform, "Notes", string.Empty, 16f, MutedInk, TextAlignmentOptions.Center);
            Stretch(note.rectTransform);
            note.margin = new Vector4(8f, 8f, 8f, 8f);
            note.lineSpacing = -10f;
            note.raycastTarget = false;
            noteLabels[row, col] = note;

            return button;
        }

        private void ShowLobby()
        {
            lobbyRoot.gameObject.SetActive(true);
            levelSelectRoot.gameObject.SetActive(false);
            gameRoot.gameObject.SetActive(false);
            ApplyResponsiveLayout();
            RefreshLeaderboard();
        }

        private void ShowLevelSelect(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
            currentLevelSelectDifficulty = difficulty;
            levelRangeStart = ClampLevelRangeStart(((currentLevel - 1) / LevelCarouselWindow) * LevelCarouselWindow + 1);
            lobbyRoot.gameObject.SetActive(false);
            gameRoot.gameObject.SetActive(false);
            levelSelectRoot.gameObject.SetActive(true);
            RebuildLevelCarousel(difficulty);
            ApplyResponsiveLayout();
        }

        private void RebuildLevelCarousel(Difficulty difficulty)
        {
            for (int i = levelCarouselContent.childCount - 1; i >= 0; i--)
                Destroy(levelCarouselContent.GetChild(i).gameObject);

            int endLevel = Mathf.Min(LevelsPerDifficulty, levelRangeStart + LevelCarouselWindow - 1);
            levelSelectTitle.text = F("sudoku.levels_range", DifficultyName(difficulty), levelRangeStart, endLevel);

            int itemCount = 0;
            for (int level = levelRangeStart; level <= endLevel; level++)
            {
                CreateLevelCard(difficulty, level);
                itemCount++;
                if (level % AdCardInterval == 0 && level < LevelsPerDifficulty)
                {
                    CreateAdCard(level / AdCardInterval);
                    itemCount++;
                }
            }

            float contentWidth = 56f + itemCount * LevelCardWidth + Mathf.Max(0, itemCount - 1) * LevelCardSpacing;
            levelCarouselContent.sizeDelta = new Vector2(contentWidth, 0f);
            levelCarouselContent.anchoredPosition = Vector2.zero;
        }

        private void RefreshLocalizedRuntimeText()
        {
            if (leaderboardText != null)
                RefreshLeaderboard();

            if (levelSelectRoot != null && levelSelectRoot.gameObject.activeSelf && levelCarouselContent != null)
                RebuildLevelCarousel(currentLevelSelectDifficulty);

            if (difficultyLabel != null)
                difficultyLabel.text = DifficultyName(currentDifficulty) + " - " + F("sudoku.level_short", currentLevel);

            UpdateNoteToggle();
            RefreshUndoButtonState();
            RefreshHintButtonState();
        }

        private void PreviousLevelRange()
        {
            levelRangeStart = ClampLevelRangeStart(levelRangeStart - LevelCarouselWindow);
            RebuildLevelCarousel(currentLevelSelectDifficulty);
        }

        private void NextLevelRange()
        {
            levelRangeStart = ClampLevelRangeStart(levelRangeStart + LevelCarouselWindow);
            RebuildLevelCarousel(currentLevelSelectDifficulty);
        }

        private static int ClampLevelRangeStart(int start)
        {
            int maxStart = Mathf.Max(1, LevelsPerDifficulty - LevelCarouselWindow + 1);
            return Mathf.Clamp(start, 1, maxStart);
        }

        private void CreateLevelCard(Difficulty difficulty, int level)
        {
            RectTransform card = CreateSoftPanel(levelCarouselContent, "Level_" + DifficultyKey(difficulty) + "_" + level, Panel, 26f).rectTransform;
            card.sizeDelta = new Vector2(LevelCardWidth, 540f);
            AddLayout(card.gameObject, LevelCardWidth, 540f);

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 30, 30);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            TextMeshProUGUI levelTitle = CreateText(card, "LevelTitle", F("sudoku.level", level), 46f, Ink, TextAlignmentOptions.Center);
            levelTitle.fontStyle = FontStyles.Bold;
            AddLayout(levelTitle.gameObject, -1f, 62f);

            bool complete = IsLevelComplete(difficulty, level);
            TextMeshProUGUI stamp = CreateText(card, "Stamp", complete ? T("sudoku.complete") : T("sudoku.incomplete"), 30f, complete ? Accent : MutedInk, TextAlignmentOptions.Center);
            stamp.fontStyle = FontStyles.Bold;
            AddLayout(stamp.gameObject, -1f, 48f);

            int best = GetBestTime(difficulty, level);
            string bestText = best > 0 ? FormatTime(best) : "-";
            TextMeshProUGUI bestLabel = CreateText(card, "BestScore", F("sudoku.best_score", bestText), 40f, PlayerInk, TextAlignmentOptions.Center);
            bestLabel.fontStyle = FontStyles.Bold;
            bestLabel.enableAutoSizing = true;
            bestLabel.fontSizeMin = 24f;
            AddLayout(bestLabel.gameObject, -1f, 112f);

            TextMeshProUGUI meta = CreateText(card, "Meta", F("sudoku.seed", DifficultyName(difficulty), level), 26f, MutedInk, TextAlignmentOptions.Center);
            meta.enableAutoSizing = true;
            meta.fontSizeMin = 18f;
            AddLayout(meta.gameObject, -1f, 74f);

            Button startButton = CreateButton(card, "StartButton", T("sudoku.start"), Accent, Color.white);
            BindLocalizedText(startButton.GetComponentInChildren<TextMeshProUGUI>(), "sudoku.start");
            AddLayout(startButton.gameObject, -1f, 84f);
            startButton.onClick.AddListener(() => StartGame(difficulty, level));
        }

        private void CreateAdCard(int index)
        {
            Button button = CreateButton(levelCarouselContent, "AdBreak_" + index, T("sudoku.ad_card"), new Color(0.96f, 0.88f, 0.72f, 1f), Ink);
            BindLocalizedText(button.GetComponentInChildren<TextMeshProUGUI>(), "sudoku.ad_card");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(LevelCardWidth, 540f);
            AddLayout(button.gameObject, LevelCardWidth, 540f);
            button.onClick.AddListener(ShowAdPlaceholder);
        }

        private string BuildLevelTitle(Difficulty difficulty, int level)
        {
            bool complete = IsLevelComplete(difficulty, level);
            int best = GetBestTime(difficulty, level);
            string stamp = complete ? "\n" + T("sudoku.complete") : "\n" + T("sudoku.incomplete");
            string time = "\n" + F("sudoku.best_score", best > 0 ? FormatTime(best) : "-");
            return F("sudoku.level", level) + stamp + time;
        }

        private void ShowLeaderboard()
        {
            RefreshLeaderboard();
            UpdateStatus(T("sudoku.status.leaderboard"));
        }

        private void ShowAdPlaceholder()
        {
            UpdateStatus(T("sudoku.status.ad"));
        }

        private void ReturnToMainMenu()
        {
            SceneNavigator navigator = FindAnyObjectByType<SceneNavigator>();
            if (navigator != null)
            {
                navigator.BackFromSymSudoku();
                return;
            }

            MahjongGame.DoorFx doorFx = MahjongGame.DoorFx.EnsureRuntime();
            if (doorFx != null && doorFx.IsReady())
                doorFx.LoadScene("Main", "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

        private void StartGame(Difficulty difficulty)
        {
            StartGame(difficulty, 1);
        }

        private void StartGame(Difficulty difficulty, int level)
        {
            currentDifficulty = difficulty;
            currentLevel = Mathf.Clamp(level, 1, LevelsPerDifficulty);
            selectedRow = -1;
            selectedCol = -1;
            mistakes = 0;
            hintCredits = 0;
            undoCredits = FreeUndoCreditsPerLevel;
            completed = false;
            noteMode = false;
            rewardedHintRequestInProgress = false;
            rewardedUndoRequestInProgress = false;
            interstitialRequestInProgress = false;
            undoStack.Clear();
            ClearNotes();

            GeneratePuzzle(difficulty, currentLevel);
            lobbyRoot.gameObject.SetActive(false);
            levelSelectRoot.gameObject.SetActive(false);
            gameRoot.gameObject.SetActive(true);
            ApplyResponsiveLayout();

            levelStartTime = Time.unscaledTime;
            lastElapsedSeconds = -1;
            if (timerLabel != null)
                timerLabel.text = FormatTime(0);

            difficultyLabel.text = DifficultyName(difficulty) + " - " + F("sudoku.level_short", currentLevel);
            UpdateStatus(F("sudoku.status.start", currentPuzzleRating.Score, currentPuzzleRating.TechniqueLabel));
            UpdateNoteToggle();
            RefreshUndoButtonState();
            RefreshHintButtonState();
            RefreshBoard();
        }

        private void GeneratePuzzle(Difficulty difficulty, int level)
        {
            DifficultyProfile profile = GetDifficultyProfile(difficulty);
            int baseSeed = ResolvePuzzleSeed(difficulty, level);
            int bestDistance = int.MaxValue;
            int[,] bestPuzzle = null;
            int[,] bestSolution = null;
            PuzzleRating bestRating = default;

            for (int attempt = 0; attempt < GeneratorAttempts; attempt++)
            {
                random = new System.Random(baseSeed + attempt * 104729);
                int[,] full = new int[Size, Size];
                FillBoard(full, 0);

                int targetGivens = random.Next(profile.MinGivens, profile.MaxGivens + 1);
                int[,] candidate = BuildUniquePuzzle(full, targetGivens);
                PuzzleRating rating = RatePuzzle(candidate);
                int distance = RatingDistance(rating, profile);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPuzzle = CloneBoard(candidate);
                    bestSolution = CloneBoard(full);
                    bestRating = rating;
                }

                if (distance == 0)
                    break;
            }

            CopyBoard(bestSolution, solution);
            CopyBoard(bestPuzzle, puzzle);
            currentPuzzleRating = bestRating;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    entries[row, col] = puzzle[row, col];
                    locked[row, col] = puzzle[row, col] != 0;
                }
            }
        }

        private int[,] BuildUniquePuzzle(int[,] full, int targetGivens)
        {
            int[,] candidate = CloneBoard(full);
            List<int> cells = new List<int>(CellCount);
            for (int i = 0; i < CellCount; i++)
                cells.Add(i);
            Shuffle(cells);

            int givens = CellCount;
            foreach (int cell in cells)
            {
                if (givens <= targetGivens)
                    break;

                int row = cell / Size;
                int col = cell % Size;
                int old = candidate[row, col];
                candidate[row, col] = 0;

                int[,] test = CloneBoard(candidate);
                if (CountSolutions(test, 2) != 1)
                {
                    candidate[row, col] = old;
                    continue;
                }

                givens--;
            }

            return candidate;
        }

        private bool FillBoard(int[,] board, int index)
        {
            if (index >= CellCount)
                return true;

            int row = index / Size;
            int col = index % Size;
            if (board[row, col] != 0)
                return FillBoard(board, index + 1);

            int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Shuffle(numbers);
            for (int i = 0; i < numbers.Length; i++)
            {
                int number = numbers[i];
                if (!CanPlace(board, row, col, number))
                    continue;

                board[row, col] = number;
                if (FillBoard(board, index + 1))
                    return true;
                board[row, col] = 0;
            }

            return false;
        }

        private int CountSolutions(int[,] board, int limit)
        {
            int bestRow = -1;
            int bestCol = -1;
            int bestCount = 10;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (board[row, col] != 0)
                        continue;

                    int count = 0;
                    for (int number = 1; number <= Size; number++)
                    {
                        if (CanPlace(board, row, col, number))
                            count++;
                    }

                    if (count == 0)
                        return 0;

                    if (count < bestCount)
                    {
                        bestCount = count;
                        bestRow = row;
                        bestCol = col;
                    }
                }
            }

            if (bestRow < 0)
                return 1;

            int solutions = 0;
            for (int number = 1; number <= Size; number++)
            {
                if (!CanPlace(board, bestRow, bestCol, number))
                    continue;

                board[bestRow, bestCol] = number;
                solutions += CountSolutions(board, limit - solutions);
                board[bestRow, bestCol] = 0;

                if (solutions >= limit)
                    return solutions;
            }

            return solutions;
        }

        private static bool CanPlace(int[,] board, int row, int col, int number)
        {
            for (int i = 0; i < Size; i++)
            {
                if (board[row, i] == number || board[i, col] == number)
                    return false;
            }

            int boxRow = row / Box * Box;
            int boxCol = col / Box * Box;
            for (int r = boxRow; r < boxRow + Box; r++)
            {
                for (int c = boxCol; c < boxCol + Box; c++)
                {
                    if (board[r, c] == number)
                        return false;
                }
            }

            return true;
        }

        private static PuzzleRating RatePuzzle(int[,] board)
        {
            PuzzleRating rating = new PuzzleRating();
            int singles = 0;
            int hiddenSingles = 0;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (board[row, col] == 0)
                        rating.EmptyCells++;
                    else
                        rating.Givens++;
                }
            }

            int[,] working = CloneBoard(board);
            bool progress = true;
            while (progress)
            {
                progress = false;

                for (int row = 0; row < Size; row++)
                {
                    for (int col = 0; col < Size; col++)
                    {
                        if (working[row, col] != 0)
                            continue;

                        int mask = CandidateMask(working, row, col);
                        if (BitCount(mask) != 1)
                            continue;

                        working[row, col] = FirstCandidate(mask);
                        singles++;
                        progress = true;
                    }
                }

                if (progress)
                    continue;

                for (int unit = 0; unit < Size && !progress; unit++)
                    progress = ApplyHiddenSingleInRow(working, unit, ref hiddenSingles);
                for (int unit = 0; unit < Size && !progress; unit++)
                    progress = ApplyHiddenSingleInColumn(working, unit, ref hiddenSingles);
                for (int box = 0; box < Size && !progress; box++)
                    progress = ApplyHiddenSingleInBox(working, box, ref hiddenSingles);
            }

            rating.Singles = singles;
            rating.HiddenSingles = hiddenSingles;
            rating.NakedPairs = CountNakedPairs(board);
            rating.SearchPressure = EstimateSearchPressure(working, 0, 240);
            rating.Score =
                rating.EmptyCells * 3 +
                rating.HiddenSingles * 5 +
                rating.NakedPairs * 12 +
                rating.SearchPressure;
            rating.TechniqueLabel = rating.NakedPairs > 0
                ? "pairs"
                : rating.HiddenSingles > 0
                    ? "hidden"
                    : "singles";
            return rating;
        }

        private static int CandidateMask(int[,] board, int row, int col)
        {
            if (board[row, col] != 0)
                return 0;

            int mask = 0;
            for (int number = 1; number <= Size; number++)
            {
                if (CanPlace(board, row, col, number))
                    mask |= 1 << number;
            }

            return mask;
        }

        private static bool ApplyHiddenSingleInRow(int[,] board, int row, ref int hiddenSingles)
        {
            for (int number = 1; number <= Size; number++)
            {
                int targetCol = -1;
                int count = 0;
                for (int col = 0; col < Size; col++)
                {
                    if ((CandidateMask(board, row, col) & (1 << number)) == 0)
                        continue;
                    targetCol = col;
                    count++;
                }

                if (count == 1)
                {
                    board[row, targetCol] = number;
                    hiddenSingles++;
                    return true;
                }
            }

            return false;
        }

        private static bool ApplyHiddenSingleInColumn(int[,] board, int col, ref int hiddenSingles)
        {
            for (int number = 1; number <= Size; number++)
            {
                int targetRow = -1;
                int count = 0;
                for (int row = 0; row < Size; row++)
                {
                    if ((CandidateMask(board, row, col) & (1 << number)) == 0)
                        continue;
                    targetRow = row;
                    count++;
                }

                if (count == 1)
                {
                    board[targetRow, col] = number;
                    hiddenSingles++;
                    return true;
                }
            }

            return false;
        }

        private static bool ApplyHiddenSingleInBox(int[,] board, int box, ref int hiddenSingles)
        {
            int boxRow = box / Box * Box;
            int boxCol = box % Box * Box;
            for (int number = 1; number <= Size; number++)
            {
                int targetRow = -1;
                int targetCol = -1;
                int count = 0;
                for (int row = boxRow; row < boxRow + Box; row++)
                {
                    for (int col = boxCol; col < boxCol + Box; col++)
                    {
                        if ((CandidateMask(board, row, col) & (1 << number)) == 0)
                            continue;
                        targetRow = row;
                        targetCol = col;
                        count++;
                    }
                }

                if (count == 1)
                {
                    board[targetRow, targetCol] = number;
                    hiddenSingles++;
                    return true;
                }
            }

            return false;
        }

        private static int CountNakedPairs(int[,] board)
        {
            int pairs = 0;
            for (int unit = 0; unit < Size; unit++)
            {
                pairs += CountNakedPairsInUnit(board, unit, 0, 0, 1);
                pairs += CountNakedPairsInUnit(board, 0, unit, 1, 0);
            }

            for (int boxRow = 0; boxRow < Size; boxRow += Box)
            {
                for (int boxCol = 0; boxCol < Size; boxCol += Box)
                    pairs += CountNakedPairsInBox(board, boxRow, boxCol);
            }

            return pairs;
        }

        private static int CountNakedPairsInUnit(int[,] board, int startRow, int startCol, int rowStep, int colStep)
        {
            int pairs = 0;
            for (int i = 0; i < Size; i++)
            {
                int rowA = startRow + i * rowStep;
                int colA = startCol + i * colStep;
                int mask = CandidateMask(board, rowA, colA);
                if (BitCount(mask) != 2)
                    continue;

                int matches = 0;
                for (int j = 0; j < Size; j++)
                {
                    int rowB = startRow + j * rowStep;
                    int colB = startCol + j * colStep;
                    if (CandidateMask(board, rowB, colB) == mask)
                        matches++;
                }

                if (matches == 2)
                    pairs++;
            }

            return pairs / 2;
        }

        private static int CountNakedPairsInBox(int[,] board, int boxRow, int boxCol)
        {
            int pairs = 0;
            for (int rowA = boxRow; rowA < boxRow + Box; rowA++)
            {
                for (int colA = boxCol; colA < boxCol + Box; colA++)
                {
                    int mask = CandidateMask(board, rowA, colA);
                    if (BitCount(mask) != 2)
                        continue;

                    int matches = 0;
                    for (int rowB = boxRow; rowB < boxRow + Box; rowB++)
                    {
                        for (int colB = boxCol; colB < boxCol + Box; colB++)
                        {
                            if (CandidateMask(board, rowB, colB) == mask)
                                matches++;
                        }
                    }

                    if (matches == 2)
                        pairs++;
                }
            }

            return pairs / 2;
        }

        private static int EstimateSearchPressure(int[,] board, int depth, int cap)
        {
            if (cap <= 0)
                return 0;

            int bestRow = -1;
            int bestCol = -1;
            int bestMask = 0;
            int bestCount = 10;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (board[row, col] != 0)
                        continue;

                    int mask = CandidateMask(board, row, col);
                    int count = BitCount(mask);
                    if (count == 0)
                        return cap;
                    if (count < bestCount)
                    {
                        bestCount = count;
                        bestMask = mask;
                        bestRow = row;
                        bestCol = col;
                    }
                }
            }

            if (bestRow < 0)
                return 0;

            int pressure = bestCount * (depth + 1);
            for (int number = 1; number <= Size && pressure < cap; number++)
            {
                if ((bestMask & (1 << number)) == 0)
                    continue;

                board[bestRow, bestCol] = number;
                pressure += EstimateSearchPressure(board, depth + 1, cap - pressure);
                board[bestRow, bestCol] = 0;
            }

            return Mathf.Min(pressure, cap);
        }

        private static int BitCount(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                count++;
            }

            return count;
        }

        private static int FirstCandidate(int mask)
        {
            for (int number = 1; number <= Size; number++)
            {
                if ((mask & (1 << number)) != 0)
                    return number;
            }

            return 0;
        }

        private static DifficultyProfile GetDifficultyProfile(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return new DifficultyProfile { MinGivens = 39, MaxGivens = 45, MinScore = 120, MaxScore = 220 };
                case Difficulty.Medium:
                    return new DifficultyProfile { MinGivens = 32, MaxGivens = 38, MinScore = 200, MaxScore = 330 };
                default:
                    return new DifficultyProfile { MinGivens = 26, MaxGivens = 31, MinScore = 300, MaxScore = 520 };
            }
        }

        private static int RatingDistance(PuzzleRating rating, DifficultyProfile profile)
        {
            int distance = 0;
            if (rating.Givens < profile.MinGivens)
                distance += (profile.MinGivens - rating.Givens) * 8;
            if (rating.Givens > profile.MaxGivens)
                distance += (rating.Givens - profile.MaxGivens) * 8;
            if (rating.Score < profile.MinScore)
                distance += profile.MinScore - rating.Score;
            if (rating.Score > profile.MaxScore)
                distance += rating.Score - profile.MaxScore;
            return distance;
        }

        private void SelectCell(int row, int col)
        {
            selectedRow = row;
            selectedCol = col;
            RefreshBoard();
        }

        private void ApplyNumber(int number)
        {
            if (!CanEditSelected())
                return;

            if (noteMode)
            {
                PushUndo(selectedRow, selectedCol);
                notes[selectedRow, selectedCol, number] = !notes[selectedRow, selectedCol, number];
                RefreshBoard();
                RefreshUndoButtonState();
                return;
            }

            PushUndo(selectedRow, selectedCol);
            entries[selectedRow, selectedCol] = number;
            ClearNotesForCell(selectedRow, selectedCol);
            if (number != solution[selectedRow, selectedCol])
            {
                mistakes++;
                UpdateStatus(F("sudoku.status.conflict", mistakes));
            }
            else
            {
                RemoveNumberFromRelatedNotes(selectedRow, selectedCol, number);
                UpdateStatus(T("sudoku.status.good"));
            }

            RefreshBoard();
            CheckComplete();
            RefreshUndoButtonState();
        }

        private void EraseSelected()
        {
            if (!CanEditSelected())
                return;

            PushUndo(selectedRow, selectedCol);
            entries[selectedRow, selectedCol] = 0;
            ClearNotesForCell(selectedRow, selectedCol);
            UpdateStatus(T("sudoku.status.erased"));
            RefreshBoard();
            RefreshUndoButtonState();
        }

        private void ToggleNotes()
        {
            noteMode = !noteMode;
            UpdateNoteToggle();
            RefreshUndoButtonState();
            RefreshHintButtonState();
        }

        private void RefreshUndoButtonState()
        {
            if (undoButtonLabel != null)
                undoButtonLabel.text = rewardedUndoRequestInProgress
                    ? T("sudoku.undo_loading")
                    : undoCredits > 0
                        ? F("sudoku.undo_stock", undoCredits)
                        : T("sudoku.undo_ad");

            if (undoButton != null)
                undoButton.interactable = !completed && !rewardedUndoRequestInProgress && undoStack.Count > 0;
        }

        private void RefreshHintButtonState()
        {
            if (hintButtonLabel != null)
                hintButtonLabel.text = rewardedHintRequestInProgress
                    ? T("sudoku.hint_loading")
                    : hintCredits > 0
                        ? F("sudoku.hint_stock", hintCredits)
                        : T("sudoku.hint_ad");

            if (hintButton != null)
                hintButton.interactable = !completed && !rewardedHintRequestInProgress;
        }

        private void OnUndoButtonPressed()
        {
            if (completed || rewardedUndoRequestInProgress)
                return;

            if (undoStack.Count == 0)
            {
                UpdateStatus(T("sudoku.status.undo_empty"));
                RefreshUndoButtonState();
                return;
            }

            if (undoCredits > 0)
            {
                if (ApplyUndoMove())
                {
                    undoCredits = Mathf.Max(0, undoCredits - 1);
                    RefreshUndoButtonState();
                }

                return;
            }

            RequestRewardedUndoAd();
        }

        private void RequestRewardedUndoAd()
        {
            MonetizationService service = MonetizationService.Ensure();
            RewardedAdAvailability availability = service.GetRewardedAdAvailability(MonetizationService.SudokuUndoRewardedPlacementId);
            if (!availability.IsReady)
            {
                UpdateStatus(T("sudoku.status.undo_ad_not_ready"));
                return;
            }

            rewardedUndoRequestInProgress = true;
            RefreshUndoButtonState();
            UpdateStatus(T("sudoku.status.undo_ad_opening"));
            service.ShowRewardedAd(MonetizationService.SudokuUndoRewardedPlacementId, result =>
            {
                rewardedUndoRequestInProgress = false;
                if (result.IsCompleted)
                {
                    undoCredits += UndoCreditsPerRewardedAd;
                    if (ApplyUndoMove())
                    {
                        undoCredits = Mathf.Max(0, undoCredits - 1);
                        UpdateStatus(T("sudoku.status.undo_ad_rewarded"));
                    }
                }
                else
                {
                    UpdateStatus(T("sudoku.status.undo_ad_not_completed"));
                }

                RefreshUndoButtonState();
            });
        }

        private bool ApplyUndoMove()
        {
            if (undoStack.Count == 0 || completed)
                return false;

            MoveState state = undoStack.Pop();
            entries[state.Row, state.Col] = state.Value;
            for (int number = 1; number <= Size; number++)
                notes[state.Row, state.Col, number] = state.Notes[number];

            selectedRow = state.Row;
            selectedCol = state.Col;
            UpdateStatus(T("sudoku.status.undo"));
            RefreshBoard();
            RefreshUndoButtonState();
            return true;
        }

        private void OnHintButtonPressed()
        {
            if (completed || rewardedHintRequestInProgress)
                return;

            if (hintCredits > 0)
            {
                if (ApplyHintReward())
                {
                    hintCredits = Mathf.Max(0, hintCredits - 1);
                    RefreshHintButtonState();
                }

                return;
            }

            RequestRewardedHintAd();
        }

        private void RequestRewardedHintAd()
        {
            MonetizationService service = MonetizationService.Ensure();
            RewardedAdAvailability availability = service.GetRewardedAdAvailability(MonetizationService.SudokuHintRewardedPlacementId);
            if (!availability.IsReady)
            {
                UpdateStatus(T("sudoku.status.hint_ad_not_ready"));
                return;
            }

            rewardedHintRequestInProgress = true;
            RefreshHintButtonState();
            UpdateStatus(T("sudoku.status.hint_ad_opening"));
            service.ShowRewardedAd(MonetizationService.SudokuHintRewardedPlacementId, result =>
            {
                rewardedHintRequestInProgress = false;
                if (result.IsCompleted)
                {
                    hintCredits += HintsPerRewardedAd;
                    if (ApplyHintReward())
                    {
                        hintCredits = Mathf.Max(0, hintCredits - 1);
                        UpdateStatus(T("sudoku.status.hint_ad_rewarded"));
                    }
                }
                else
                {
                    UpdateStatus(T("sudoku.status.hint_ad_not_completed"));
                }

                RefreshHintButtonState();
            });
        }

        private bool ApplyHintReward()
        {
            if (completed)
                return false;

            if (selectedRow >= 0 && selectedCol >= 0 && !locked[selectedRow, selectedCol] && entries[selectedRow, selectedCol] != solution[selectedRow, selectedCol])
            {
                PushUndo(selectedRow, selectedCol);
                entries[selectedRow, selectedCol] = solution[selectedRow, selectedCol];
                ClearNotesForCell(selectedRow, selectedCol);
                RemoveNumberFromRelatedNotes(selectedRow, selectedCol, entries[selectedRow, selectedCol]);
                UpdateStatus(T("sudoku.status.hint_selected"));
                RefreshBoard();
                RefreshUndoButtonState();
                CheckComplete();
                return true;
            }

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (locked[row, col] || entries[row, col] == solution[row, col])
                        continue;

                    selectedRow = row;
                    selectedCol = col;
                    PushUndo(row, col);
                    entries[row, col] = solution[row, col];
                    ClearNotesForCell(row, col);
                    RemoveNumberFromRelatedNotes(row, col, entries[row, col]);
                    UpdateStatus(T("sudoku.status.hint_next"));
                    RefreshBoard();
                    RefreshUndoButtonState();
                    CheckComplete();
                    return true;
                }
            }

            UpdateStatus(T("sudoku.status.hint_empty"));
            return false;
        }

        private bool CanEditSelected()
        {
            if (completed)
                return false;

            if (selectedRow < 0 || selectedCol < 0)
            {
                UpdateStatus(T("sudoku.status.select_cell"));
                return false;
            }

            if (locked[selectedRow, selectedCol])
            {
                UpdateStatus(T("sudoku.status.locked"));
                return false;
            }

            return true;
        }

        private void CheckComplete()
        {
            if (completed)
                return;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (entries[row, col] != solution[row, col])
                        return;
                }
            }

            completed = true;
            int elapsed = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - levelStartTime));
            SaveProgress(currentDifficulty, currentLevel, elapsed);
            RefreshLeaderboard();
            UpdateStatus(F("sudoku.status.complete", currentLevel, FormatTime(elapsed), mistakes));
            RefreshUndoButtonState();
            RefreshHintButtonState();
            RegisterCompletedGameForInterstitial();
        }

        private void RegisterCompletedGameForInterstitial()
        {
            int completedGames = PlayerPrefs.GetInt(CompletedGamesKey, 0) + 1;
            PlayerPrefs.SetInt(CompletedGamesKey, completedGames);
            PlayerPrefs.Save();

            if (NoAdsService.HasActiveNoAds())
                return;

            if (completedGames % InterstitialEveryCompletedGames == 0)
                StartCoroutine(ShowSudokuInterstitialWhenReady());
        }

        private IEnumerator ShowSudokuInterstitialWhenReady()
        {
            if (interstitialRequestInProgress)
                yield break;

            if (NoAdsService.HasActiveNoAds())
                yield break;

            MonetizationService service = MonetizationService.Ensure();
            interstitialRequestInProgress = true;
            string placementId = MonetizationService.SudokuInterstitialPlacementId;
            float deadline = Time.unscaledTime + SudokuInterstitialWarmupTimeoutSeconds;
            while (Time.unscaledTime < deadline && !service.CanShowInterstitialAd(placementId))
                yield return null;

            if (service.CanShowInterstitialAd(placementId))
            {
                service.ShowInterstitialAd(placementId, result =>
                {
                    interstitialRequestInProgress = false;
                    Debug.Log("[SymSudoku] Interstitial result: " + result.State + " | placement=" + result.PlacementId);
                });
            }
            else
            {
                interstitialRequestInProgress = false;
            }
        }

        private void RefreshBoard()
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    int value = entries[row, col];
                    TextMeshProUGUI valueLabel = cellLabels[row, col];
                    TextMeshProUGUI noteLabel = noteLabels[row, col];

                    valueLabel.text = value == 0 ? string.Empty : value.ToString();
                    valueLabel.color = locked[row, col] ? GivenInk : PlayerInk;
                    noteLabel.text = value == 0 ? FormatNotes(row, col) : string.Empty;

                    Image image = cellButtons[row, col].GetComponent<Image>();
                    image.color = ResolveCellColor(row, col);
                }
            }
        }

        private Color ResolveCellColor(int row, int col)
        {
            if (HasConflict(row, col))
                return CellConflict;

            if (row == selectedRow && col == selectedCol)
                return CellSelected;

            if (selectedRow >= 0 && selectedCol >= 0)
            {
                bool sameBox = row / Box == selectedRow / Box && col / Box == selectedCol / Box;
                if (row == selectedRow || col == selectedCol || sameBox)
                    return CellRelated;

                int selectedValue = entries[selectedRow, selectedCol];
                if (selectedValue != 0 && entries[row, col] == selectedValue)
                    return new Color(0.95f, 0.9f, 0.72f, 1f);
            }

            return ((row / Box) + (col / Box)) % 2 == 0 ? Cell : CellAlt;
        }

        private bool HasConflict(int row, int col)
        {
            int value = entries[row, col];
            if (value == 0)
                return false;

            if (!locked[row, col] && value != solution[row, col])
                return true;

            for (int i = 0; i < Size; i++)
            {
                if (i != col && entries[row, i] == value)
                    return true;
                if (i != row && entries[i, col] == value)
                    return true;
            }

            int boxRow = row / Box * Box;
            int boxCol = col / Box * Box;
            for (int r = boxRow; r < boxRow + Box; r++)
            {
                for (int c = boxCol; c < boxCol + Box; c++)
                {
                    if ((r != row || c != col) && entries[r, c] == value)
                        return true;
                }
            }

            return false;
        }

        private string FormatNotes(int row, int col)
        {
            string text = string.Empty;
            for (int number = 1; number <= Size; number++)
            {
                text += notes[row, col, number] ? number.ToString() : " ";
                if (number % 3 == 0 && number < Size)
                    text += "\n";
                else if (number < Size)
                    text += " ";
            }

            return text;
        }

        private void ClearNotes()
        {
            Array.Clear(notes, 0, notes.Length);
        }

        private void ClearNotesForCell(int row, int col)
        {
            for (int number = 1; number <= Size; number++)
                notes[row, col, number] = false;
        }

        private void RemoveNumberFromRelatedNotes(int row, int col, int number)
        {
            for (int i = 0; i < Size; i++)
            {
                notes[row, i, number] = false;
                notes[i, col, number] = false;
            }

            int boxRow = row / Box * Box;
            int boxCol = col / Box * Box;
            for (int r = boxRow; r < boxRow + Box; r++)
            {
                for (int c = boxCol; c < boxCol + Box; c++)
                    notes[r, c, number] = false;
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }

        private void UpdateNoteToggle()
        {
            if (noteToggleLabel != null)
                noteToggleLabel.text = noteMode ? T("sudoku.notes_on") : T("sudoku.notes_off");
        }

        private void PushUndo(int row, int col)
        {
            bool[] noteCopy = new bool[Size + 1];
            for (int number = 1; number <= Size; number++)
                noteCopy[number] = notes[row, col, number];

            undoStack.Push(new MoveState
            {
                Row = row,
                Col = col,
                Value = entries[row, col],
                Notes = noteCopy
            });
        }

        private void RefreshLeaderboard()
        {
            if (leaderboardText == null)
                return;

            leaderboardText.text =
                BuildDifficultySummary(Difficulty.Easy) + "\n" +
                BuildDifficultySummary(Difficulty.Medium) + "\n" +
                BuildDifficultySummary(Difficulty.Hard);
        }

        private string BuildDifficultySummary(Difficulty difficulty)
        {
            int completedCount = 0;
            int bestTotal = 0;
            for (int level = 1; level <= LevelsPerDifficulty; level++)
            {
                if (!IsLevelComplete(difficulty, level))
                    continue;

                completedCount++;
                int best = GetBestTime(difficulty, level);
                if (best > 0)
                    bestTotal += best;
            }

            string total = bestTotal > 0 ? FormatTime(bestTotal) : "-";
            return F("sudoku.summary", DifficultyName(difficulty), completedCount, LevelsPerDifficulty, total);
        }

        private static void SaveProgress(Difficulty difficulty, int level, int elapsedSeconds)
        {
            string completeKey = ProgressKey(difficulty, level, "complete");
            string bestKey = ProgressKey(difficulty, level, "best");
            PlayerPrefs.SetInt(completeKey, 1);

            int oldBest = PlayerPrefs.GetInt(bestKey, 0);
            if (oldBest <= 0 || elapsedSeconds < oldBest)
                PlayerPrefs.SetInt(bestKey, Mathf.Max(1, elapsedSeconds));

            PlayerPrefs.Save();
        }

        private static bool IsLevelComplete(Difficulty difficulty, int level)
        {
            return PlayerPrefs.GetInt(ProgressKey(difficulty, level, "complete"), 0) == 1;
        }

        private static int GetBestTime(Difficulty difficulty, int level)
        {
            return PlayerPrefs.GetInt(ProgressKey(difficulty, level, "best"), 0);
        }

        private static string ProgressKey(Difficulty difficulty, int level, string suffix)
        {
            return ProgressPrefix + DifficultyKey(difficulty) + "." + level + "." + suffix;
        }

        private static int ResolvePuzzleSeed(Difficulty difficulty, int level)
        {
            return 17341 + (int)difficulty * 1009 + level * 7919;
        }

        private static string FormatTime(int seconds)
        {
            int minutes = Mathf.Max(0, seconds) / 60;
            int rest = Mathf.Max(0, seconds) % 60;
            return minutes.ToString("00") + ":" + rest.ToString("00");
        }

        private static string DifficultyName(Difficulty difficulty)
        {
            return difficulty == Difficulty.Easy ? T("sudoku.easy") : difficulty == Difficulty.Medium ? T("sudoku.medium") : T("sudoku.hard");
        }

        private static string DifficultyKey(Difficulty difficulty)
        {
            return difficulty == Difficulty.Easy ? "easy" : difficulty == Difficulty.Medium ? "medium" : "hard";
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        private void Shuffle(int[] items)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        private static int[,] CloneBoard(int[,] source)
        {
            int[,] clone = new int[Size, Size];
            CopyBoard(source, clone);
            return clone;
        }

        private static void CopyBoard(int[,] source, int[,] target)
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                    target[row, col] = source[row, col];
            }
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            return CreateImage(parent, name, color);
        }

        private static Image CreateSoftPanel(Transform parent, string name, Color color, float radius)
        {
            Image image = CreateImage(parent, name, color);
            image.sprite = GetRoundedSprite(ref softPanelSprite, "SymSudokuSoftPanel", Mathf.RoundToInt(radius));
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
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
            TMP_FontAsset font = LoadPhilosopherFont();
            if (font != null)
                label.font = font;
            return label;
        }

        private static TextMeshProUGUI CreateLocalizedText(Transform parent, string name, string key, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = CreateText(parent, name, T(key), fontSize, color, alignment);
            BindLocalizedText(label, key);
            return label;
        }

        private static void BindLocalizedText(TextMeshProUGUI label, string key)
        {
            if (label == null || string.IsNullOrWhiteSpace(key))
                return;

            LocalizedText localized = label.GetComponent<LocalizedText>();
            if (localized == null)
                localized = label.gameObject.AddComponent<LocalizedText>();
            localized.SetKey(key);
        }

        private static Button CreateButton(Transform parent, string name, string text, Color background, Color foreground)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = background;
            image.sprite = GetRoundedSprite(ref softButtonSprite, "SymSudokuSoftButton", 18);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;

            Button button = obj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.12f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI label = CreateText(obj.transform, "Label", text, 28f, foreground, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 36f;
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform, 12f);

            return button;
        }

        private static Button CreateActionButton(Transform parent, string textKey, UnityEngine.Events.UnityAction action)
        {
            string text = T(textKey);
            Button button = CreateButton(parent, textKey.Replace(" ", string.Empty).Replace(".", string.Empty) + "Button", text, Accent, Color.white);
            BindLocalizedText(button.GetComponentInChildren<TextMeshProUGUI>(), textKey);
            AddLayout(button.gameObject, 190f, -1f, 1f);
            button.onClick.AddListener(action);
            return button;
        }

        private static string T(string key)
        {
            return GameLocalization.Text(key);
        }

        private static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static TMP_FontAsset LoadPhilosopherFont()
        {
            if (philosopherFont != null)
                return philosopherFont;

            Font font = Resources.Load<Font>(PhilosopherFontResourcePath);
            if (font == null)
                return null;

            philosopherFont = TMP_FontAsset.CreateFontAsset(font);
            return philosopherFont;
        }

        private static Sprite GetRoundedSprite(ref Sprite cache, string name, int radius)
        {
            if (cache != null)
                return cache;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name + "Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, size, Mathf.Clamp(radius, 1, 30));
                    texture.SetPixel(x, y, inside ? solid : clear);
                }
            }

            texture.Apply(false, true);
            cache = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            cache.name = name;
            return cache;
        }

        private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
        {
            int min = radius;
            int max = size - radius - 1;
            int closestX = Mathf.Clamp(x, min, max);
            int closestY = Mathf.Clamp(y, min, max);
            int dx = x - closestX;
            int dy = y - closestY;
            return dx * dx + dy * dy <= radius * radius;
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

        private static void LayoutTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AddLayout(GameObject obj, float preferredWidth, float preferredHeight, float flexibleWidth = 0f)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f)
                layout.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f)
                layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = flexibleWidth;
        }
    }
}
