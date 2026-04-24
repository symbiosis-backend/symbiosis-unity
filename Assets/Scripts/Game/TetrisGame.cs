using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class TetrisGame : MonoBehaviour
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;
    private const int BoardCellCount = BoardWidth * BoardHeight;
    private const float MoveRepeatDelay = 0.18f;
    private const float MoveRepeatInterval = 0.07f;
    private const float SoftDropRepeatInterval = 0.045f;

    [Header("Scene UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private Image cellPrefab;
    [SerializeField] private Button startButton;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text linesText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text controlsText;

    [Header("Labels")]
    [SerializeField] private string startLabel = "Начать";
    [SerializeField] private string restartLabel = "Заново";
    [SerializeField] private string idleStatusLabel = "Нажми «Начать»";
    [SerializeField] private string playingStatusLabel = "Игра идёт";
    [SerializeField] private string gameOverStatusLabel = "Игра окончена";
    [SerializeField] private string controlsLabel = "← → движение     ↑ поворот     ↓ быстрее     Space сброс";

    [Header("Editor Helper")]
    [SerializeField] private bool createEditableUiAutomatically = true;

    private static readonly Vector2Int[][] Shapes =
    {
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) },
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) }
    };

    private readonly Color[] colors =
    {
        new Color(0.08f, 0.10f, 0.13f, 1f),
        new Color(0.00f, 0.80f, 0.95f, 1f),
        new Color(0.95f, 0.83f, 0.18f, 1f),
        new Color(0.75f, 0.30f, 0.95f, 1f),
        new Color(0.95f, 0.48f, 0.20f, 1f),
        new Color(0.25f, 0.45f, 0.95f, 1f),
        new Color(0.22f, 0.82f, 0.36f, 1f),
        new Color(0.95f, 0.22f, 0.24f, 1f)
    };

    private int[,] board;
    private Image[,] cells;
    private Vector2Int currentPosition;
    private Vector2Int[] currentShape;
    private int currentColorIndex;
    private float fallTimer;
    private float fallInterval;
    private float moveRepeatTimer;
    private float softDropRepeatTimer;
    private int heldMoveDirection;
    private int score;
    private int lines;
    private bool isPlaying;
    private bool isGameOver;

    private void Awake()
    {
        CreateEditableUiIfNeeded();
        BindUi();
    }

    private void Start()
    {
        EnsureEventSystem();
        EnsureBoardCells();
        ResetBoard();
        DrawBoard();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        HandleKeyboard();

        fallTimer += Time.deltaTime;
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            StepDown();
        }
    }

    public void StartGame()
    {
        ResetBoard();

        score = 0;
        lines = 0;
        fallInterval = 0.65f;
        isPlaying = true;
        isGameOver = false;

        SetStartButtonLabel(restartLabel);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SpawnPiece();
        UpdateTexts();
        DrawBoard();
    }

    [ContextMenu("Create Editable UI")]
    private void CreateEditableUiIfNeeded()
    {
        if (!createEditableUiAutomatically && canvas != null)
            return;

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvas == null)
            canvas = CreateCanvas();

        Transform root = canvas.transform;

        RectTransform title = FindChildRect(root, "Title");
        if (title == null)
        {
            Text titleText = CreateText("Title", root, "TETRIS", 72, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(520f, 90f));
        }

        if (boardRoot == null)
            boardRoot = FindChildRect(root, "Board Root");

        if (boardRoot == null)
        {
            GameObject boardObject = CreatePanel("Board Root", root, new Color(0.12f, 0.14f, 0.18f, 1f));
            boardRoot = boardObject.GetComponent<RectTransform>();
            SetRect(boardRoot, new Vector2(0.5f, 0.59f), new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(530f, 1040f));
            ConfigureBoardGrid(boardRoot);
        }
        else if (boardRoot.GetComponent<GridLayoutGroup>() == null)
        {
            ConfigureBoardGrid(boardRoot);
        }

        startButton = startButton != null ? startButton : FindChildComponent<Button>(root, "Start Button");
        if (startButton == null)
        {
            startButton = CreateButton("Start Button", root, startLabel, new Color(0.19f, 0.60f, 0.36f, 1f));
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(360f, 82f));
        }

        scoreText = scoreText != null ? scoreText : FindChildComponent<Text>(root, "Score Text");
        if (scoreText == null)
        {
            scoreText = CreateText("Score Text", root, "Счёт: 0", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 300f), new Vector2(300f, 52f));
        }

        linesText = linesText != null ? linesText : FindChildComponent<Text>(root, "Lines Text");
        if (linesText == null)
        {
            linesText = CreateText("Lines Text", root, "Линии: 0", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(linesText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 300f), new Vector2(300f, 52f));
        }

        statusText = statusText != null ? statusText : FindChildComponent<Text>(root, "Status Text");
        if (statusText == null)
        {
            statusText = CreateText("Status Text", root, idleStatusLabel, 32, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 195f), new Vector2(680f, 62f));
        }

        controlsText = controlsText != null ? controlsText : FindChildComponent<Text>(root, "Controls Text");
        if (controlsText == null)
        {
            controlsText = CreateText("Controls Text", root, controlsLabel, 25, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(controlsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(860f, 48f));
        }

        BindUi();
    }

    private void BindUi()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
            SetStartButtonLabel(isPlaying ? restartLabel : startLabel);
        }

        if (statusText != null && !isPlaying && !isGameOver)
            statusText.text = idleStatusLabel;

        if (controlsText != null)
            controlsText.text = controlsLabel;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Tetris Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    private void ConfigureBoardGrid(RectTransform root)
    {
        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = root.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = BoardWidth;
        grid.cellSize = new Vector2(48f, 48f);
        grid.spacing = new Vector2(3f, 3f);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private void EnsureBoardCells()
    {
        if (boardRoot == null)
        {
            Debug.LogError("[TetrisGame] Board Root is missing. Assign a RectTransform in the Inspector.", this);
            return;
        }

        ConfigureBoardGrid(boardRoot);

        int existing = CountDirectCellImages();

        for (int i = existing; i < BoardCellCount; i++)
            CreateCell(boardRoot);

        cells = new Image[BoardWidth, BoardHeight];

        int index = 0;
        for (int y = BoardHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                cells[x, y] = GetDirectCellImage(index);
                index++;
            }
        }
    }

    private int CountDirectCellImages()
    {
        if (boardRoot == null)
            return 0;

        int count = 0;
        for (int i = 0; i < boardRoot.childCount; i++)
        {
            if (boardRoot.GetChild(i).GetComponent<Image>() != null)
                count++;
        }

        return count;
    }

    private Image GetDirectCellImage(int index)
    {
        if (boardRoot == null)
            return null;

        int current = 0;
        for (int i = 0; i < boardRoot.childCount; i++)
        {
            Image image = boardRoot.GetChild(i).GetComponent<Image>();
            if (image == null)
                continue;

            if (current == index)
                return image;

            current++;
        }

        return null;
    }

    private void CreateCell(Transform parent)
    {
        Image image;
        if (cellPrefab != null)
        {
            image = Instantiate(cellPrefab, parent);
            image.gameObject.name = "Cell";
        }
        else
        {
            GameObject cell = CreatePanel("Cell", parent, colors[0]);
            image = cell.GetComponent<Image>();
        }

        image.color = colors[0];
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
        MahjongGame.EventSystemInputModeGuard.EnsureCompatibleEventSystems();
    }

    private void HandleKeyboard()
    {
        bool leftPressed = IsLeftPressed();
        bool rightPressed = IsRightPressed();
        int moveDirection = rightPressed == leftPressed ? 0 : rightPressed ? 1 : -1;

        if (moveDirection == 0)
        {
            heldMoveDirection = 0;
            moveRepeatTimer = 0f;
        }
        else if (moveDirection != heldMoveDirection)
        {
            heldMoveDirection = moveDirection;
            moveRepeatTimer = MoveRepeatDelay;
            TryMove(new Vector2Int(heldMoveDirection, 0));
        }
        else
        {
            moveRepeatTimer -= Time.deltaTime;
            if (moveRepeatTimer <= 0f)
            {
                moveRepeatTimer = MoveRepeatInterval;
                TryMove(new Vector2Int(heldMoveDirection, 0));
            }
        }

        if (WasRotatePressed())
            RotatePiece();

        if (IsSoftDropPressed())
        {
            softDropRepeatTimer -= Time.deltaTime;
            if (softDropRepeatTimer <= 0f)
            {
                softDropRepeatTimer = SoftDropRepeatInterval;
                StepDown();
            }
        }
        else
        {
            softDropRepeatTimer = 0f;
        }

        if (WasHardDropPressed())
            HardDrop();
    }

    private static bool IsLeftPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed);
#else
        return false;
#endif
    }

    private static bool IsRightPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed);
#else
        return false;
#endif
    }

    private static bool IsSoftDropPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed);
#else
        return false;
#endif
    }

    private static bool WasRotatePressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    private static bool WasHardDropPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private void RotatePiece()
    {
        if (!isPlaying || currentShape == null)
            return;

        Vector2Int[] rotated = new Vector2Int[currentShape.Length];
        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(currentShape[i].y, -currentShape[i].x);

        if (CanPlace(rotated, currentPosition))
        {
            currentShape = rotated;
        }
        else if (CanPlace(rotated, currentPosition + Vector2Int.left))
        {
            currentShape = rotated;
            currentPosition += Vector2Int.left;
        }
        else if (CanPlace(rotated, currentPosition + Vector2Int.right))
        {
            currentShape = rotated;
            currentPosition += Vector2Int.right;
        }

        DrawBoard();
    }

    private void HardDrop()
    {
        if (!isPlaying)
            return;

        while (TryMove(new Vector2Int(0, -1)))
            score += 1;

        LockPiece();
    }

    private void ResetBoard()
    {
        board = new int[BoardWidth, BoardHeight];
        currentShape = null;
        currentPosition = new Vector2Int(BoardWidth / 2, BoardHeight - 2);
        fallTimer = 0f;
        moveRepeatTimer = 0f;
        softDropRepeatTimer = 0f;
        heldMoveDirection = 0;
        UpdateTexts();
    }

    private void SpawnPiece()
    {
        int shapeIndex = UnityEngine.Random.Range(0, Shapes.Length);
        currentShape = CloneShape(Shapes[shapeIndex]);
        currentColorIndex = shapeIndex + 1;
        currentPosition = new Vector2Int(BoardWidth / 2, BoardHeight - 2);

        if (!CanPlace(currentShape, currentPosition))
        {
            isPlaying = false;
            isGameOver = true;
            if (statusText != null)
                statusText.text = gameOverStatusLabel;
        }
        else if (statusText != null)
        {
            statusText.text = playingStatusLabel;
        }
    }

    private void StepDown()
    {
        if (!TryMove(new Vector2Int(0, -1)))
            LockPiece();
    }

    private bool TryMove(Vector2Int delta)
    {
        if (!isPlaying || currentShape == null)
            return false;

        Vector2Int target = currentPosition + delta;
        if (!CanPlace(currentShape, target))
            return false;

        currentPosition = target;
        DrawBoard();
        return true;
    }

    private bool CanPlace(Vector2Int[] shape, Vector2Int position)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int cell = position + shape[i];
            if (cell.x < 0 || cell.x >= BoardWidth || cell.y < 0 || cell.y >= BoardHeight)
                return false;

            if (board[cell.x, cell.y] != 0)
                return false;
        }

        return true;
    }

    private void LockPiece()
    {
        if (currentShape == null)
            return;

        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int cell = currentPosition + currentShape[i];
            if (cell.x >= 0 && cell.x < BoardWidth && cell.y >= 0 && cell.y < BoardHeight)
                board[cell.x, cell.y] = currentColorIndex;
        }

        int cleared = ClearLines();
        if (cleared > 0)
        {
            lines += cleared;
            score += cleared * cleared * 100;
            fallInterval = Mathf.Max(0.12f, 0.65f - lines * 0.015f);
        }

        SpawnPiece();
        UpdateTexts();
        DrawBoard();
    }

    private int ClearLines()
    {
        int cleared = 0;

        for (int y = 0; y < BoardHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < BoardWidth; x++)
            {
                if (board[x, y] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (!full)
                continue;

            for (int row = y; row < BoardHeight - 1; row++)
            {
                for (int x = 0; x < BoardWidth; x++)
                    board[x, row] = board[x, row + 1];
            }

            for (int x = 0; x < BoardWidth; x++)
                board[x, BoardHeight - 1] = 0;

            cleared++;
            y--;
        }

        return cleared;
    }

    private void DrawBoard()
    {
        if (cells == null || board == null)
            return;

        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                if (cells[x, y] != null)
                    cells[x, y].color = colors[board[x, y]];
            }
        }

        if (currentShape == null || isGameOver)
            return;

        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int cell = currentPosition + currentShape[i];
            if (cell.x >= 0 && cell.x < BoardWidth && cell.y >= 0 && cell.y < BoardHeight && cells[cell.x, cell.y] != null)
                cells[cell.x, cell.y].color = colors[currentColorIndex];
        }
    }

    private void UpdateTexts()
    {
        if (scoreText != null)
            scoreText.text = $"Счёт: {score}";

        if (linesText != null)
            linesText.text = $"Линии: {lines}";
    }

    private void SetStartButtonLabel(string label)
    {
        if (startButton == null)
            return;

        Text labelText = startButton.GetComponentInChildren<Text>();
        if (labelText != null)
            labelText.text = label;
    }

    private static Vector2Int[] CloneShape(Vector2Int[] source)
    {
        Vector2Int[] clone = new Vector2Int[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static RectTransform FindChildRect(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child as RectTransform : null;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Color color)
    {
        GameObject go = CreatePanel(objectName, parent, color);
        Button button = go.AddComponent<Button>();

        ColorBlock block = button.colors;
        block.normalColor = color;
        block.highlightedColor = color + new Color(0.08f, 0.08f, 0.08f, 0f);
        block.pressedColor = color * 0.85f;
        block.selectedColor = block.highlightedColor;
        button.colors = block;

        Text text = CreateText("Text", go.transform, label, 38, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = Color.white;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
