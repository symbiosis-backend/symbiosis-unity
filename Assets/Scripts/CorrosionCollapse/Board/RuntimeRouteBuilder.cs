using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class RuntimeRouteBuilder : MonoBehaviour
    {
        private const int GridWidth = 50;
        private const int GridHeight = 50;
        private const int GridMin = -25;
        private const int GridMax = 24;
        private const float ReferenceMapWidth = 1920f;
        private const float ReferenceMapHeight = 1080f;
        private const float DefaultIsoStepX = 28f;
        private const float DefaultIsoStepY = 14f;
        private const float PanSpeed = 360f;
        private const float TuneSpeed = 12f;
        private const string ResourcePath = "CorrosionCollapse/BGCC";
        private const string MapFileName = "BGCC.png";
        private const string LayoutFileName = "route-layout.json";
        private static readonly Vector2 RouteTileSize = new Vector2(19f, 19f);

        private readonly List<Vector2Int> routeCells = new List<Vector2Int>(160);
        private readonly List<TileType> routeEffects = new List<TileType>(160);
        private readonly List<GameObject> previewTiles = new List<GameObject>(160);
        private readonly List<RawImage> previewImages = new List<RawImage>(160);
        private readonly List<Vector2Int> shortcutCells = new List<Vector2Int>(64);
        private readonly List<GameObject> shortcutPreviewTiles = new List<GameObject>(64);
        private readonly List<Vector2Int> portalInCells = new List<Vector2Int>(8);
        private readonly List<Vector2Int> portalOutCells = new List<Vector2Int>(8);
        private readonly List<GameObject> portalInPreviewTiles = new List<GameObject>(8);
        private readonly List<GameObject> portalOutPreviewTiles = new List<GameObject>(8);
        private readonly List<BuildAction> actionHistory = new List<BuildAction>(256);
        private readonly List<ButtonHitArea> buttonHitAreas = new List<ButtonHitArea>(20);

        private BoardBuilder boardBuilder;
        private Action onRouteApplied;
        private RectTransform builderRoot;
        private RectTransform inputSurface;
        private RectTransform gridRoot;
        private RectTransform routeRoot;
        private RectTransform mapBackgroundRect;
        private RectTransform hoverTile;
        private Text saveButtonText;
        private Text undoButtonText;
        private Text finishButtonText;
        private Text selectedEffectText;
        private Text selectedToolText;
        private Vector2 panOffset;
        private Vector2 lastMapRectSize;
        private float isoStepX = DefaultIsoStepX;
        private float isoStepY = DefaultIsoStepY;
        private BuilderTool selectedTool = BuilderTool.Road;
        private TileType selectedEffect = TileType.Normal;
        private bool active;

        public void Initialize(BoardBuilder builder, Action routeApplied)
        {
            boardBuilder = builder;
            onRouteApplied = routeApplied;
            active = true;
            boardBuilder.ClearRoute();
            EnsureCanvas();
            ClearGrid();
            TryLoadSavedLayout();
            Debug.Log("[Builder] 2D route builder enabled. First click sets START. Enter applies route. Backspace removes last point.");
        }

        private void Update()
        {
            if (!active || builderRoot == null)
            {
                return;
            }

            UpdatePan();
            UpdateGridTuning();
            UpdateMapRectSize();
            UpdateHover();
            UpdatePointerInput();

            if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                RemoveLastCell();
            }

            if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
            {
                SaveLayout();
            }

            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                ApplyRoute();
            }
        }

        private void UpdatePointerInput()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandlePointerClick(Mouse.current.position.ReadValue());
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                HandlePointerClick(Touchscreen.current.primaryTouch.position.ReadValue());
            }
        }

        private void HandlePointerClick(Vector2 screenPosition)
        {
            for (int i = buttonHitAreas.Count - 1; i >= 0; i--)
            {
                ButtonHitArea hitArea = buttonHitAreas[i];
                if (hitArea.rect != null && RectTransformUtility.RectangleContainsScreenPoint(hitArea.rect, screenPosition, null))
                {
                    hitArea.action?.Invoke();
                    return;
                }
            }

            TryAddCellFromScreenPosition(screenPosition);
        }

        private void UpdateGridTuning()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            bool changed = false;
            float tuneDelta = TuneSpeed * Time.unscaledDeltaTime;
            if (Keyboard.current.qKey.isPressed)
            {
                isoStepY = Mathf.Max(3f, isoStepY - tuneDelta);
                changed = true;
            }

            if (Keyboard.current.eKey.isPressed)
            {
                isoStepY = Mathf.Min(40f, isoStepY + tuneDelta);
                changed = true;
            }

            if (Keyboard.current.zKey.isPressed)
            {
                isoStepX = Mathf.Max(8f, isoStepX - tuneDelta);
                changed = true;
            }

            if (Keyboard.current.xKey.isPressed)
            {
                isoStepX = Mathf.Min(60f, isoStepX + tuneDelta);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            RefreshRoutePositions();
            Debug.Log($"[Builder] Grid tuning: stepX={isoStepX:0.0}, stepY={isoStepY:0.0}");
        }

        private void EnsureCanvas()
        {
            GameObject canvasObject = GameObject.Find("RouteBuilderCanvas") ?? new GameObject("RouteBuilderCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(null, false);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.SetActive(true);
            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 60;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            builderRoot = canvasObject.GetComponent<RectTransform>();
            builderRoot.anchorMin = Vector2.zero;
            builderRoot.anchorMax = Vector2.one;
            builderRoot.offsetMin = Vector2.zero;
            builderRoot.offsetMax = Vector2.zero;
            builderRoot.localRotation = Quaternion.identity;
            builderRoot.localScale = Vector3.one;

            mapBackgroundRect = EnsureBuilderMapBackground(builderRoot);
            inputSurface = EnsureInputSurface(builderRoot);
            gridRoot = EnsureRect("BuilderGrid", builderRoot);
            RemoveStaleBuilderRoute(builderRoot);
            routeRoot = EnsureRect("BuilderRoute", mapBackgroundRect);
            routeRoot.SetAsLastSibling();
            lastMapRectSize = mapBackgroundRect.rect.size;
            hoverTile = CreateDiamond(routeRoot, "HoverCell", Vector2.zero, new Vector2(32f, 32f), new Color(0.2f, 0.85f, 1f, 0.55f));
            hoverTile.gameObject.SetActive(false);
            buttonHitAreas.Clear();
            CreateSaveButton(builderRoot);
            CreateUndoButton(builderRoot);
            CreateFinishButton(builderRoot);
            CreateToolPanel(builderRoot);
            CreateEffectPanel(builderRoot);
            RefreshSelectionLabels();
            ApplyPanOffset();
        }

        private void UpdatePan()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            Vector2 direction = Vector2.zero;
            if (Keyboard.current.leftArrowKey.isPressed)
            {
                direction.x -= 1f;
            }

            if (Keyboard.current.rightArrowKey.isPressed)
            {
                direction.x += 1f;
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                direction.y += 1f;
            }

            if (Keyboard.current.downArrowKey.isPressed)
            {
                direction.y -= 1f;
            }

            if (direction == Vector2.zero)
            {
                return;
            }

            panOffset += direction.normalized * PanSpeed * Time.unscaledDeltaTime;
            ApplyPanOffset();
            RefreshRoutePositions();
        }

        private void UpdateMapRectSize()
        {
            if (mapBackgroundRect == null)
            {
                return;
            }

            Vector2 size = mapBackgroundRect.rect.size;
            if ((size - lastMapRectSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            lastMapRectSize = size;
            RefreshRoutePositions();
        }

        private RectTransform EnsureInputSurface(Transform parent)
        {
            Transform existing = parent.Find("BuilderInputSurface");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("BuilderInputSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsFirstSibling();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Image image = obj.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            return rect;
        }

        private RectTransform EnsureBuilderMapBackground(Transform parent)
        {
            Transform existing = parent.Find("BuilderMapBackground");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("BuilderMapBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsFirstSibling();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            RawImage image = obj.GetComponent<RawImage>();
            Texture texture = Resources.Load<Texture2D>(ResourcePath) ?? LoadTextureFromFile() ?? FindExistingMapTexture();
            image.texture = texture;
            image.color = texture != null ? Color.white : Color.clear;
            image.raycastTarget = false;

            AspectRatioFitter fitter = obj.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture != null ? texture.width / (float)texture.height : 16f / 9f;
            return rect;
        }

        private void CreateSaveButton(Transform parent)
        {
            Transform existing = parent.Find("SaveLayoutButton");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("SaveLayoutButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.035f);
            rect.anchorMax = new Vector2(0.16f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.05f, 0.045f, 0.035f, 0.88f);

            Button button = obj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            RegisterButton(rect, SaveLayout);

            Transform textTransform = obj.transform.Find("Text");
            GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            saveButtonText = textObject.GetComponent<Text>();
            saveButtonText.text = "SAVE LAYOUT";
            saveButtonText.alignment = TextAnchor.MiddleCenter;
            saveButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            saveButtonText.fontSize = 18;
            saveButtonText.color = new Color(1f, 0.82f, 0.2f, 1f);
        }

        private void CreateUndoButton(Transform parent)
        {
            Transform existing = parent.Find("UndoRouteButton");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("UndoRouteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.17f, 0.035f);
            rect.anchorMax = new Vector2(0.29f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.05f, 0.045f, 0.035f, 0.88f);

            Button button = obj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            RegisterButton(rect, RemoveLastCell);

            Transform textTransform = obj.transform.Find("Text");
            GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            undoButtonText = textObject.GetComponent<Text>();
            undoButtonText.text = "UNDO";
            undoButtonText.alignment = TextAnchor.MiddleCenter;
            undoButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            undoButtonText.fontSize = 18;
            undoButtonText.color = new Color(1f, 0.82f, 0.2f, 1f);
        }

        private void CreateFinishButton(Transform parent)
        {
            Transform existing = parent.Find("FinishRouteButton");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("FinishRouteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.30f, 0.035f);
            rect.anchorMax = new Vector2(0.43f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.08f, 0.055f, 0.02f, 0.9f);

            Button button = obj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            RegisterButton(rect, ApplyRoute);

            Transform textTransform = obj.transform.Find("Text");
            GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            finishButtonText = textObject.GetComponent<Text>();
            finishButtonText.text = "FINISH";
            finishButtonText.alignment = TextAnchor.MiddleCenter;
            finishButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            finishButtonText.fontSize = 18;
            finishButtonText.color = new Color(1f, 0.88f, 0.24f, 1f);
        }

        private void CreateToolPanel(Transform parent)
        {
            RectTransform panel = EnsureRect("ToolPanel", parent);
            panel.anchorMin = new Vector2(0.44f, 0.035f);
            panel.anchorMax = new Vector2(0.78f, 0.14f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Image background = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.03f, 0.025f, 0.78f);

            selectedToolText = CreateLabel(panel, "SelectedTool", new Vector2(0.02f, 0.58f), new Vector2(0.98f, 0.98f), "Tool: ROAD", 15, new Color(1f, 0.86f, 0.32f, 1f));
            CreateToolButton(panel, BuilderTool.Road, "ROAD", 0);
            CreateToolButton(panel, BuilderTool.Trail, "TRAIL", 1);
            CreateToolButton(panel, BuilderTool.Portal, "PORTAL", 2);
        }

        private void CreateToolButton(Transform parent, BuilderTool tool, string label, int index)
        {
            GameObject obj = new GameObject($"Tool_{tool}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            float x0 = 0.04f + index * 0.31f;
            rect.anchorMin = new Vector2(x0, 0.08f);
            rect.anchorMax = new Vector2(x0 + 0.27f, 0.48f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = GetToolColor(tool);

            Text text = CreateLabel(rect, "Text", Vector2.zero, Vector2.one, label, 14, Color.black);
            text.raycastTarget = false;

            Button button = obj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            RegisterButton(rect, () => SelectTool(tool));
        }

        private void CreateEffectPanel(Transform parent)
        {
            RectTransform panel = EnsureRect("EffectPanel", parent);
            panel.anchorMin = new Vector2(0.79f, 0.08f);
            panel.anchorMax = new Vector2(0.985f, 0.32f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Image background = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.035f, 0.03f, 0.78f);

            selectedEffectText = CreateLabel(panel, "SelectedEffect", new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f), "Effect: Normal", 16, new Color(1f, 0.86f, 0.32f, 1f));

            TileType[] effects =
            {
                TileType.Normal,
                TileType.Purple,
                TileType.Yellow,
                TileType.Green,
                TileType.Red,
                TileType.BlackRed,
                TileType.Safe
            };

            for (int i = 0; i < effects.Length; i++)
            {
                int index = i;
                int row = i / 4;
                int col = i % 4;
                RectTransform buttonRect = CreateEffectButton(panel, effects[i], col, row);
                Button button = buttonRect.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                RegisterButton(buttonRect, () => SelectEffect(effects[index]));
            }
        }

        private RectTransform CreateEffectButton(Transform parent, TileType effect, int col, int row)
        {
            GameObject obj = new GameObject($"Effect_{effect}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            float x0 = 0.05f + col * 0.235f;
            float x1 = x0 + 0.2f;
            float y1 = 0.72f - row * 0.3f;
            float y0 = y1 - 0.22f;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = GetEffectColor(effect);

            Text label = CreateLabel(rect, "Text", Vector2.zero, Vector2.one, ShortName(effect), 14, effect == TileType.BlackRed ? Color.red : Color.black);
            label.raycastTarget = false;
            return rect;
        }

        private Text CreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string value, int fontSize, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            return text;
        }

        private void SelectEffect(TileType effect)
        {
            selectedEffect = effect;
            RefreshSelectionLabels();

            Debug.Log($"[Builder] Selected effect: {effect}");
        }

        private void SelectTool(BuilderTool tool)
        {
            selectedTool = tool;
            RefreshSelectionLabels();
            Debug.Log($"[Builder] Selected tool: {tool}");
        }

        private void RefreshSelectionLabels()
        {
            if (selectedEffectText != null)
            {
                selectedEffectText.text = $"Effect: {selectedEffect}";
            }

            if (selectedToolText != null)
            {
                selectedToolText.text = $"Tool: {selectedTool}";
            }
        }

        private void RegisterButton(RectTransform rect, Action action)
        {
            buttonHitAreas.Add(new ButtonHitArea(rect, action));
        }

        private static RectTransform EnsureRect(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void RemoveStaleBuilderRoute(Transform oldParent)
        {
            Transform stale = oldParent.Find("BuilderRoute");
            if (stale != null)
            {
                Destroy(stale.gameObject);
            }
        }

        private void ClearGrid()
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }
        }

        private RectTransform FindMapBackgroundRect()
        {
            GameObject background = GameObject.Find("BuilderMapBackground") ?? GameObject.Find("CCMapUIBackground");
            return background != null ? background.GetComponent<RectTransform>() : null;
        }

        private void ApplyPanOffset()
        {
            if (mapBackgroundRect == null)
            {
                mapBackgroundRect = FindMapBackgroundRect();
            }

            if (mapBackgroundRect != null)
            {
                mapBackgroundRect.anchoredPosition = panOffset;
            }
        }

        private void RefreshRoutePositions()
        {
            for (int i = 0; i < routeCells.Count && i < previewTiles.Count; i++)
            {
                if (previewTiles[i] != null)
                {
                    RectTransform rect = previewTiles[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = GridToLocal(routeCells[i]);
                    rect.sizeDelta = ScaleReferenceSize(RouteTileSize);
                }
            }

            for (int i = 0; i < shortcutCells.Count && i < shortcutPreviewTiles.Count; i++)
            {
                if (shortcutPreviewTiles[i] != null)
                {
                    RectTransform rect = shortcutPreviewTiles[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = GridToLocal(shortcutCells[i]);
                    rect.sizeDelta = ScaleReferenceSize(new Vector2(36f, 36f));
                }
            }

            for (int i = 0; i < portalInCells.Count && i < portalInPreviewTiles.Count; i++)
            {
                if (portalInPreviewTiles[i] != null)
                {
                    RectTransform rect = portalInPreviewTiles[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = GridToLocal(portalInCells[i]);
                    rect.sizeDelta = ScaleReferenceSize(new Vector2(36f, 36f));
                }
            }

            for (int i = 0; i < portalOutCells.Count && i < portalOutPreviewTiles.Count; i++)
            {
                if (portalOutPreviewTiles[i] != null)
                {
                    RectTransform rect = portalOutPreviewTiles[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = GridToLocal(portalOutCells[i]);
                    rect.sizeDelta = ScaleReferenceSize(new Vector2(36f, 36f));
                }
            }
        }

        private void TryAddCellFromScreenPosition(Vector2 screenPosition)
        {
            if (!TryGetPointerCell(screenPosition, out Vector2Int cell))
            {
                Debug.Log("[Builder] Click was outside route grid.");
                return;
            }

            TryAddCell(cell);
        }

        private void TryAddCell(Vector2Int cell)
        {
            if (selectedTool == BuilderTool.Trail)
            {
                TryAddShortcutCell(cell);
                return;
            }

            if (selectedTool == BuilderTool.Portal)
            {
                AddPortalCell(cell);
                return;
            }

            int existingIndex = routeCells.IndexOf(cell);
            if (existingIndex >= 0)
            {
                SetCellEffect(existingIndex, selectedEffect);
                return;
            }

            if (routeCells.Count == 0)
            {
                AddCell(cell);
                Debug.Log($"[Builder] START set at {cell}");
                return;
            }

            Vector2Int last = routeCells[^1];
            if (cell == last)
            {
                return;
            }

            if (cell.x != last.x && cell.y != last.y)
            {
                Debug.Log("[Builder] Diagonal route segment rejected. Continue along an isometric row.");
                return;
            }

            AddLine(last, cell);
        }

        private void UpdateHover()
        {
            if (hoverTile == null)
            {
                return;
            }

            if (TryGetPointerCell(out Vector2Int cell))
            {
                hoverTile.gameObject.SetActive(true);
                hoverTile.anchoredPosition = GridToLocal(cell);
                hoverTile.sizeDelta = ScaleReferenceSize(new Vector2(32f, 32f));
                RawImage hoverImage = hoverTile.GetComponent<RawImage>();
                if (hoverImage != null)
                {
                    hoverImage.color = GetHoverColor();
                }
            }
            else
            {
                hoverTile.gameObject.SetActive(false);
            }
        }

        private Color GetHoverColor()
        {
            if (selectedTool == BuilderTool.Trail)
            {
                return new Color(0.48f, 0.2f, 1f, 0.58f);
            }

            if (selectedTool == BuilderTool.Portal)
            {
                return portalInCells.Count == portalOutCells.Count
                    ? new Color(1f, 0.12f, 0.08f, 0.62f)
                    : new Color(0.15f, 0.85f, 1f, 0.62f);
            }

            return new Color(1f, 0.78f, 0.1f, 0.58f);
        }

        private bool TryGetPointerCell(out Vector2Int cell)
        {
            cell = default;
            if (Mouse.current == null)
            {
                return false;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            return TryGetPointerCell(pointerPosition, out cell);
        }

        private bool TryGetPointerCell(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (mapBackgroundRect == null)
            {
                mapBackgroundRect = FindMapBackgroundRect();
            }

            if (mapBackgroundRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(mapBackgroundRect, screenPosition, null, out Vector2 local))
            {
                return false;
            }

            return LocalToGrid(local, out cell);
        }

        private void AddLine(Vector2Int from, Vector2Int to)
        {
            Vector2Int cursor = from;
            while (cursor != to)
            {
                if (cursor.x != to.x)
                {
                    cursor.x += cursor.x < to.x ? 1 : -1;
                }
                else if (cursor.y != to.y)
                {
                    cursor.y += cursor.y < to.y ? 1 : -1;
                }

                AddCell(cursor);
            }
        }

        private void AddShortcutLine(Vector2Int from, Vector2Int to)
        {
            Vector2Int cursor = from;
            while (cursor != to)
            {
                if (cursor.x != to.x)
                {
                    cursor.x += cursor.x < to.x ? 1 : -1;
                }
                else if (cursor.y != to.y)
                {
                    cursor.y += cursor.y < to.y ? 1 : -1;
                }

                AddShortcutCell(cursor);
            }
        }

        private void TryAddShortcutCell(Vector2Int cell)
        {
            if (shortcutCells.Count > 0)
            {
                Vector2Int last = shortcutCells[^1];
                if (cell == last)
                {
                    return;
                }

                if (cell.x != last.x && cell.y != last.y)
                {
                    Debug.Log("[Builder] Diagonal trail segment rejected. Continue along an isometric row.");
                    return;
                }

                AddShortcutLine(last, cell);
                return;
            }

            AddShortcutCell(cell);
        }

        private void AddCell(Vector2Int cell)
        {
            if (routeCells.Count > 0 && routeCells[^1] == cell)
            {
                return;
            }

            bool isStart = routeCells.Count == 0;
            routeCells.Add(cell);
            routeEffects.Add(isStart ? TileType.Safe : selectedEffect);
            RectTransform tile = CreateDiamond(
                routeRoot,
                isStart ? "Route_START" : $"Route_{routeCells.Count:000}",
                GridToLocal(cell),
                ScaleReferenceSize(RouteTileSize),
                isStart ? new Color(0.15f, 1f, 0.35f, 0.95f) : GetEffectColor(selectedEffect));
            previewTiles.Add(tile.gameObject);
            previewImages.Add(tile.GetComponent<RawImage>());
            actionHistory.Add(new BuildAction(BuildActionType.Road, routeCells.Count - 1));
        }

        private void AddShortcutCell(Vector2Int cell)
        {
            if (shortcutCells.Count > 0 && shortcutCells[^1] == cell)
            {
                return;
            }

            shortcutCells.Add(cell);
            RectTransform tile = CreateTextMarker(
                routeRoot,
                $"Trail_{shortcutCells.Count:000}",
                GridToLocal(cell),
                "●",
                Mathf.RoundToInt(26f * CurrentMapScale()),
                new Color(0.48f, 0.2f, 1f, 0.96f));
            shortcutPreviewTiles.Add(tile.gameObject);
            actionHistory.Add(new BuildAction(BuildActionType.Trail, shortcutCells.Count - 1));
        }

        private void AddPortalCell(Vector2Int cell)
        {
            bool isEntry = portalInCells.Count == portalOutCells.Count;
            if (isEntry)
            {
                portalInCells.Add(cell);
                RectTransform marker = CreateTextMarker(
                    routeRoot,
                    $"Portal_IN_{portalInCells.Count:000}",
                    GridToLocal(cell),
                    "▲",
                    Mathf.RoundToInt(34f * CurrentMapScale()),
                    new Color(1f, 0.12f, 0.08f, 0.96f));
                portalInPreviewTiles.Add(marker.gameObject);
                actionHistory.Add(new BuildAction(BuildActionType.PortalIn, portalInCells.Count - 1));
                Debug.Log($"[Builder] Portal entry placed at {cell}. Place linked exit portal.");
                return;
            }

            portalOutCells.Add(cell);
            RectTransform exitMarker = CreateTextMarker(
                routeRoot,
                $"Portal_OUT_{portalOutCells.Count:000}",
                GridToLocal(cell),
                "▼",
                Mathf.RoundToInt(34f * CurrentMapScale()),
                new Color(0.15f, 0.85f, 1f, 0.96f));
            portalOutPreviewTiles.Add(exitMarker.gameObject);
            actionHistory.Add(new BuildAction(BuildActionType.PortalOut, portalOutCells.Count - 1));
            Debug.Log($"[Builder] Portal exit placed at {cell}. Pair {portalOutCells.Count - 1} linked.");
        }

        private void SetCellEffect(int index, TileType effect)
        {
            if (index < 0 || index >= routeEffects.Count)
            {
                return;
            }

            routeEffects[index] = index == 0 ? TileType.Safe : effect;
            if (index < previewImages.Count && previewImages[index] != null)
            {
                previewImages[index].color = index == 0 ? new Color(0.15f, 1f, 0.35f, 0.95f) : GetEffectColor(effect);
            }

            Debug.Log($"[Builder] Cell {routeCells[index]} effect set to {routeEffects[index]}.");
        }

        private RectTransform CreateDiamond(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0f, 0f, 45f);
            rect.localScale = Vector3.one;

            RawImage image = obj.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private RectTransform CreateTextMarker(Transform parent, string name, Vector2 position, string glyph, int fontSize, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = ScaleReferenceSize(new Vector2(36f, 36f));
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Text text = obj.GetComponent<Text>();
            text.text = glyph;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(8, fontSize);
            text.color = color;
            text.raycastTarget = false;
            return rect;
        }

        private Vector2 GridToLocal(Vector2Int cell)
        {
            Rect rect = mapBackgroundRect != null ? mapBackgroundRect.rect : builderRoot.rect;
            Vector2 origin = GetIsoOrigin(rect);
            float originX = origin.x;
            float originY = origin.y;
            float x = originX + (cell.x - cell.y) * ScaledStepX(rect);
            float y = originY + (cell.x + cell.y) * ScaledStepY(rect);
            return new Vector2(x, y);
        }

        private bool LocalToGrid(Vector2 local, out Vector2Int cell)
        {
            Rect rect = mapBackgroundRect != null ? mapBackgroundRect.rect : builderRoot.rect;
            Vector2 origin = GetIsoOrigin(rect);
            float originX = origin.x;
            float originY = origin.y;
            float projectedX = (local.x - originX) / ScaledStepX(rect);
            float projectedY = (local.y - originY) / ScaledStepY(rect);
            int x = Mathf.RoundToInt((projectedX + projectedY) * 0.5f);
            int y = Mathf.RoundToInt((projectedY - projectedX) * 0.5f);
            cell = new Vector2Int(x, y);
            return x >= GridMin && x <= GridMax && y >= GridMin && y <= GridMax;
        }

        private Vector2 GetIsoOrigin(Rect rect)
        {
            return new Vector2(0f, -rect.height * 0.02f);
        }

        private float ScaledStepX(Rect rect)
        {
            return isoStepX * (rect.width / ReferenceMapWidth);
        }

        private float ScaledStepY(Rect rect)
        {
            return isoStepY * (rect.height / ReferenceMapHeight);
        }

        private Vector2 ScaleReferenceSize(Vector2 referenceSize)
        {
            Rect rect = mapBackgroundRect != null ? mapBackgroundRect.rect : builderRoot.rect;
            return referenceSize * CurrentMapScale();
        }

        private float CurrentMapScale()
        {
            Rect rect = mapBackgroundRect != null ? mapBackgroundRect.rect : builderRoot.rect;
            float scale = Mathf.Min(rect.width / ReferenceMapWidth, rect.height / ReferenceMapHeight);
            return Mathf.Max(0.01f, scale);
        }

        private void RemoveLastCell()
        {
            if (actionHistory.Count == 0)
            {
                return;
            }

            BuildAction action = actionHistory[^1];
            actionHistory.RemoveAt(actionHistory.Count - 1);
            switch (action.type)
            {
                case BuildActionType.Road:
                    RemoveAt(routeCells, action.index);
                    RemoveAt(routeEffects, action.index);
                    DestroyPreview(previewTiles, action.index);
                    RemoveAt(previewImages, action.index);
                    Debug.Log("[Builder] Last road point removed.");
                    break;
                case BuildActionType.Trail:
                    RemoveAt(shortcutCells, action.index);
                    DestroyPreview(shortcutPreviewTiles, action.index);
                    Debug.Log("[Builder] Last trail point removed.");
                    break;
                case BuildActionType.PortalIn:
                    RemoveAt(portalInCells, action.index);
                    DestroyPreview(portalInPreviewTiles, action.index);
                    Debug.Log("[Builder] Last portal entry removed.");
                    break;
                case BuildActionType.PortalOut:
                    RemoveAt(portalOutCells, action.index);
                    DestroyPreview(portalOutPreviewTiles, action.index);
                    Debug.Log("[Builder] Last portal exit removed.");
                    break;
            }

            if (undoButtonText != null)
            {
                undoButtonText.text = actionHistory.Count > 0 ? "UNDO" : "EMPTY";
            }
        }

        private static void RemoveAt<T>(List<T> list, int index)
        {
            if (index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
            }
        }

        private static void DestroyPreview(List<GameObject> list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            if (list[index] != null)
            {
                Destroy(list[index]);
            }

            list.RemoveAt(index);
        }

        private void ApplyRoute()
        {
            if (routeCells.Count < 2)
            {
                Debug.Log("[Builder] Route needs at least START and one more cell.");
                return;
            }

            SaveLayout();
            boardBuilder.BuildManualRoute(routeCells, routeEffects);
            active = false;
            Debug.Log($"[Builder] Route applied: {routeCells.Count} cells.");
            onRouteApplied?.Invoke();
        }

        private void SaveLayout()
        {
            if (routeCells.Count == 0)
            {
                Debug.Log("[Builder] Nothing to save yet.");
                return;
            }

            var layout = new SavedRouteLayout
            {
                gridWidth = GridWidth,
                gridHeight = GridHeight,
                gridMin = GridMin,
                gridMax = GridMax,
                stepX = isoStepX,
                stepY = isoStepY,
                map = MapFileName,
                cells = new SavedRouteCell[routeCells.Count],
                shortcuts = new SavedGridCell[shortcutCells.Count],
                portals = new SavedPortalPair[Mathf.Min(portalInCells.Count, portalOutCells.Count)]
            };

            for (int i = 0; i < routeCells.Count; i++)
            {
                layout.cells[i] = new SavedRouteCell
                {
                    x = routeCells[i].x,
                    y = routeCells[i].y,
                    effect = routeEffects[i].ToString()
                };
            }

            for (int i = 0; i < shortcutCells.Count; i++)
            {
                layout.shortcuts[i] = new SavedGridCell
                {
                    x = shortcutCells[i].x,
                    y = shortcutCells[i].y
                };
            }

            for (int i = 0; i < layout.portals.Length; i++)
            {
                layout.portals[i] = new SavedPortalPair
                {
                    inX = portalInCells[i].x,
                    inY = portalInCells[i].y,
                    outX = portalOutCells[i].x,
                    outY = portalOutCells[i].y
                };
            }

            string directory = Path.Combine(Application.persistentDataPath, "CorrosionCollapse");
            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(layout, true);
            string path = Path.Combine(directory, LayoutFileName);
            File.WriteAllText(path, json);

            string projectPath = GetProjectLayoutPath();
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
            File.WriteAllText(projectPath, json);

            Debug.Log($"[Builder] Route layout saved: {path}");
            Debug.Log($"[Builder] Project route layout saved: {projectPath}");
            if (saveButtonText != null)
            {
                saveButtonText.text = "SAVED";
            }
        }

        private void TryLoadSavedLayout()
        {
            string projectPath = GetProjectLayoutPath();
            string persistentPath = Path.Combine(Application.persistentDataPath, "CorrosionCollapse", LayoutFileName);
            string path = File.Exists(projectPath) ? projectPath : persistentPath;
            if (!File.Exists(path))
            {
                return;
            }

            SavedRouteLayout layout = JsonUtility.FromJson<SavedRouteLayout>(File.ReadAllText(path));
            if (layout == null || layout.cells == null || layout.cells.Length == 0)
            {
                return;
            }

            ClearRoutePreview();
            routeCells.Clear();
            routeEffects.Clear();
            shortcutCells.Clear();
            portalInCells.Clear();
            portalOutCells.Clear();
            actionHistory.Clear();
            isoStepX = layout.stepX > 0f ? layout.stepX : DefaultIsoStepX;
            isoStepY = layout.stepY > 0f ? layout.stepY : DefaultIsoStepY;

            for (int i = 0; i < layout.cells.Length; i++)
            {
                SavedRouteCell saved = layout.cells[i];
                var cell = new Vector2Int(saved.x, saved.y);
                if (cell.x < GridMin || cell.x > GridMax || cell.y < GridMin || cell.y > GridMax)
                {
                    continue;
                }

                routeCells.Add(cell);
                routeEffects.Add(ParseTileType(saved.effect, i == 0 ? TileType.Safe : TileType.Normal));
            }

            if (layout.shortcuts != null)
            {
                for (int i = 0; i < layout.shortcuts.Length; i++)
                {
                    var cell = new Vector2Int(layout.shortcuts[i].x, layout.shortcuts[i].y);
                    if (cell.x >= GridMin && cell.x <= GridMax && cell.y >= GridMin && cell.y <= GridMax)
                    {
                        shortcutCells.Add(cell);
                    }
                }
            }

            if (layout.portals != null)
            {
                for (int i = 0; i < layout.portals.Length; i++)
                {
                    var inCell = new Vector2Int(layout.portals[i].inX, layout.portals[i].inY);
                    var outCell = new Vector2Int(layout.portals[i].outX, layout.portals[i].outY);
                    if (inCell.x >= GridMin && inCell.x <= GridMax && inCell.y >= GridMin && inCell.y <= GridMax &&
                        outCell.x >= GridMin && outCell.x <= GridMax && outCell.y >= GridMin && outCell.y <= GridMax)
                    {
                        portalInCells.Add(inCell);
                        portalOutCells.Add(outCell);
                    }
                }
            }

            RebuildRoutePreview();
            Debug.Log($"[Builder] Loaded saved route layout: {path} ({routeCells.Count} cells).");
        }

        private void RebuildRoutePreview()
        {
            ClearRoutePreview();
            for (int i = 0; i < routeCells.Count; i++)
            {
                bool isStart = i == 0;
                TileType type = isStart ? TileType.Safe : routeEffects[i];
                RectTransform tile = CreateDiamond(
                    routeRoot,
                    isStart ? "Route_START" : $"Route_{i + 1:000}",
                    GridToLocal(routeCells[i]),
                    ScaleReferenceSize(RouteTileSize),
                    isStart ? new Color(0.15f, 1f, 0.35f, 0.95f) : GetEffectColor(type));
                previewTiles.Add(tile.gameObject);
                previewImages.Add(tile.GetComponent<RawImage>());
            }

            for (int i = 0; i < shortcutCells.Count; i++)
            {
                RectTransform tile = CreateTextMarker(routeRoot, $"Trail_{i + 1:000}", GridToLocal(shortcutCells[i]), "●", Mathf.RoundToInt(26f * CurrentMapScale()), new Color(0.48f, 0.2f, 1f, 0.96f));
                shortcutPreviewTiles.Add(tile.gameObject);
            }

            for (int i = 0; i < portalInCells.Count; i++)
            {
                RectTransform marker = CreateTextMarker(routeRoot, $"Portal_IN_{i + 1:000}", GridToLocal(portalInCells[i]), "▲", Mathf.RoundToInt(34f * CurrentMapScale()), new Color(1f, 0.12f, 0.08f, 0.96f));
                portalInPreviewTiles.Add(marker.gameObject);
            }

            for (int i = 0; i < portalOutCells.Count; i++)
            {
                RectTransform marker = CreateTextMarker(routeRoot, $"Portal_OUT_{i + 1:000}", GridToLocal(portalOutCells[i]), "▼", Mathf.RoundToInt(34f * CurrentMapScale()), new Color(0.15f, 0.85f, 1f, 0.96f));
                portalOutPreviewTiles.Add(marker.gameObject);
            }
        }

        private void ClearRoutePreview()
        {
            for (int i = previewTiles.Count - 1; i >= 0; i--)
            {
                if (previewTiles[i] != null)
                {
                    Destroy(previewTiles[i]);
                }
            }

            previewTiles.Clear();
            previewImages.Clear();
            ClearPreviewList(shortcutPreviewTiles);
            ClearPreviewList(portalInPreviewTiles);
            ClearPreviewList(portalOutPreviewTiles);
        }

        private static void ClearPreviewList(List<GameObject> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null)
                {
                    Destroy(list[i]);
                }
            }

            list.Clear();
        }

        private static TileType ParseTileType(string value, TileType fallback)
        {
            return Enum.TryParse(value, out TileType parsed) ? parsed : fallback;
        }

        private static string GetProjectLayoutPath()
        {
            return Path.Combine(Application.dataPath, "Resources", "CorrosionCollapse", LayoutFileName);
        }

        [Serializable]
        private sealed class SavedRouteLayout
        {
            public int gridWidth;
            public int gridHeight;
            public int gridMin;
            public int gridMax;
            public float stepX;
            public float stepY;
            public string map;
            public SavedRouteCell[] cells;
            public SavedGridCell[] shortcuts;
            public SavedPortalPair[] portals;
        }

        [Serializable]
        private sealed class SavedRouteCell
        {
            public int x;
            public int y;
            public string effect;
        }

        [Serializable]
        private sealed class SavedGridCell
        {
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class SavedPortalPair
        {
            public int inX;
            public int inY;
            public int outX;
            public int outY;
        }

        private static Color GetEffectColor(TileType effect)
        {
            return effect switch
            {
                TileType.Purple => new Color(0.62f, 0.17f, 0.95f, 0.96f),
                TileType.Yellow => new Color(1f, 0.78f, 0.08f, 0.96f),
                TileType.Green => new Color(0.18f, 0.9f, 0.28f, 0.96f),
                TileType.Red => new Color(0.95f, 0.12f, 0.08f, 0.96f),
                TileType.BlackRed => new Color(0.02f, 0.01f, 0.015f, 0.96f),
                TileType.Safe => new Color(0.15f, 1f, 0.35f, 0.96f),
                _ => new Color(1f, 0.78f, 0.1f, 0.96f)
            };
        }

        private static Color GetToolColor(BuilderTool tool)
        {
            return tool switch
            {
                BuilderTool.Trail => new Color(0.52f, 0.22f, 1f, 0.94f),
                BuilderTool.Portal => new Color(1f, 0.18f, 0.08f, 0.94f),
                _ => new Color(1f, 0.78f, 0.1f, 0.94f)
            };
        }

        private static string ShortName(TileType effect)
        {
            return effect switch
            {
                TileType.Purple => "P",
                TileType.Yellow => "Y",
                TileType.Green => "G",
                TileType.Red => "R",
                TileType.BlackRed => "BR",
                TileType.Safe => "S",
                _ => "N"
            };
        }

        private static Texture2D LoadTextureFromFile()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "CorrosionCollapse", MapFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }

        private static Texture FindExistingMapTexture()
        {
            GameObject background = GameObject.Find("CCMapUIBackground");
            if (background == null || !background.TryGetComponent(out RawImage image))
            {
                return null;
            }

            return image.texture;
        }

        private readonly struct ButtonHitArea
        {
            public readonly RectTransform rect;
            public readonly Action action;

            public ButtonHitArea(RectTransform rect, Action action)
            {
                this.rect = rect;
                this.action = action;
            }
        }

        private enum BuilderTool
        {
            Road,
            Trail,
            Portal
        }

        private enum BuildActionType
        {
            Road,
            Trail,
            PortalIn,
            PortalOut
        }

        private readonly struct BuildAction
        {
            public readonly BuildActionType type;
            public readonly int index;

            public BuildAction(BuildActionType type, int index)
            {
                this.type = type;
                this.index = index;
            }
        }
    }
}
