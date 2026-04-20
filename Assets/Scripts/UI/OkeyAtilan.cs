using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class OkeyAtilan : MonoBehaviour, IPointerClickHandler
    {
        [Header("Setup")]
        public Button AtilanButton;
        public OkeyActionButtonsUI ActionButtonsUI;
        public RectTransform WindowRoot;
        public RectTransform ContentRoot;
        public OkeyDiscardPile[] DiscardPiles;

        [Header("Window")]
        public Vector2 WindowSize = new Vector2(1000f, 450f);
        public Vector2 ContentPadding = new Vector2(20f, 20f);
        [Range(0f, 1f)] public float OverlayAlpha = 0.45f;

        [Header("Layout")]
        [Min(1)] public int Columns = 10;
        public Vector2 CellSize = new Vector2(70f, 100f);
        public Vector2 Spacing = new Vector2(8f, 8f);

        [Header("Button Behaviour")]
        public bool HideButtonWhenUnavailable = false;

        private readonly List<GameObject> spawnedViews = new List<GameObject>();
        private Image overlayImage;
        private GridLayoutGroup grid;
        private ContentSizeFitter fitter;
        private bool overlayVisible;
        private bool initialized;

        private void Awake()
        {
            Initialize();
            SetOverlayVisible(false, true);
            RefreshButtonState();
        }

        private void OnEnable()
        {
            Initialize();
            RefreshButtonState();
        }

        private void Update()
        {
            RefreshButtonState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureOverlayImage();
                EnsureWindow();
                EnsureContentRoot();
                EnsureGrid();
            }
        }
#endif

        private void Initialize()
        {
            if (initialized)
                return;

            AutoResolve();
            EnsureOverlayImage();
            EnsureWindow();
            EnsureContentRoot();
            EnsureGrid();
            initialized = true;
        }

        private void AutoResolve()
        {
            if (AtilanButton == null)
                AtilanButton = FindButtonByName("AtilanTaslarBtn");

            if (ActionButtonsUI == null)
                ActionButtonsUI = FindAnyObjectByType<OkeyActionButtonsUI>();

            if (DiscardPiles == null || DiscardPiles.Length == 0)
                DiscardPiles = FindObjectsByType<OkeyDiscardPile>(FindObjectsInactive.Exclude);

            if (WindowRoot == null)
            {
                Transform t = transform.Find("Window");
                if (t != null)
                    WindowRoot = t as RectTransform;
            }

            if (ContentRoot == null && WindowRoot != null)
            {
                Transform t = WindowRoot.Find("ContentRoot");
                if (t != null)
                    ContentRoot = t as RectTransform;
            }
        }

        private void EnsureOverlayImage()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            overlayImage = GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = false;
        }

        private void EnsureWindow()
        {
            if (WindowRoot == null)
            {
                GameObject go = new GameObject("Window", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                WindowRoot = go.transform as RectTransform;
            }

            WindowRoot.SetParent(transform, false);
            WindowRoot.anchorMin = new Vector2(0.5f, 0.5f);
            WindowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            WindowRoot.pivot = new Vector2(0.5f, 0.5f);
            WindowRoot.sizeDelta = WindowSize;
            WindowRoot.anchoredPosition = Vector2.zero;
            WindowRoot.localScale = Vector3.one;
            WindowRoot.localRotation = Quaternion.identity;

            Image windowImage = WindowRoot.GetComponent<Image>();
            if (windowImage == null)
                windowImage = WindowRoot.gameObject.AddComponent<Image>();

            if (windowImage.color.a <= 0f || windowImage.color == Color.white)
                windowImage.color = new Color(0.08f, 0.18f, 0.12f, 0.92f);

            windowImage.raycastTarget = true;
        }

        private void EnsureContentRoot()
        {
            if (ContentRoot == null && WindowRoot != null)
            {
                Transform t = WindowRoot.Find("ContentRoot");
                if (t != null)
                    ContentRoot = t as RectTransform;
            }

            if (ContentRoot == null && WindowRoot != null)
            {
                GameObject go = new GameObject("ContentRoot", typeof(RectTransform));
                go.transform.SetParent(WindowRoot, false);
                ContentRoot = go.transform as RectTransform;
            }

            if (ContentRoot == null)
                return;

            ContentRoot.SetParent(WindowRoot, false);
            ContentRoot.anchorMin = Vector2.zero;
            ContentRoot.anchorMax = Vector2.one;
            ContentRoot.pivot = new Vector2(0.5f, 0.5f);
            ContentRoot.offsetMin = new Vector2(ContentPadding.x, ContentPadding.y);
            ContentRoot.offsetMax = new Vector2(-ContentPadding.x, -ContentPadding.y);
            ContentRoot.localScale = Vector3.one;
            ContentRoot.localRotation = Quaternion.identity;
        }

        private void EnsureGrid()
        {
            if (ContentRoot == null)
                return;

            grid = ContentRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = ContentRoot.gameObject.AddComponent<GridLayoutGroup>();

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, Columns);
            grid.cellSize = CellSize;
            grid.spacing = Spacing;

            fitter = ContentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = ContentRoot.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private Button FindButtonByName(string objectName)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
            }
            return null;
        }

        private bool CanUseAtilan()
        {
            if (ActionButtonsUI == null)
                return false;

            return ActionButtonsUI.IsCifteGitActive;
        }

        private void RefreshButtonState()
        {
            if (AtilanButton == null)
                return;

            bool active = CanUseAtilan();

            if (HideButtonWhenUnavailable)
            {
                if (AtilanButton.gameObject.activeSelf != active)
                    AtilanButton.gameObject.SetActive(active);
            }
            else
            {
                AtilanButton.interactable = active;
            }

            if (!active && overlayVisible)
                Close();
        }

        public void Toggle()
        {
            if (!CanUseAtilan())
                return;

            if (overlayVisible) Close();
            else Open();
        }

        public void Open()
        {
            if (!CanUseAtilan())
                return;

            Initialize();
            ClearViews();
            BuildViews();
            SetOverlayVisible(true, false);
        }

        public void Close()
        {
            ClearViews();
            SetOverlayVisible(false, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!overlayVisible)
                return;

            if (eventData == null)
                return;

            if (WindowRoot == null)
            {
                Close();
                return;
            }

            bool clickedInsideWindow = RectTransformUtility.RectangleContainsScreenPoint(
                WindowRoot,
                eventData.position,
                eventData.pressEventCamera
            );

            if (!clickedInsideWindow)
                Close();
        }

        private void SetOverlayVisible(bool visible, bool instant)
        {
            overlayVisible = visible;

            if (overlayImage == null)
                overlayImage = GetComponent<Image>();

            overlayImage.color = visible
                ? new Color(0f, 0f, 0f, OverlayAlpha)
                : new Color(0f, 0f, 0f, 0f);

            overlayImage.raycastTarget = visible;

            if (WindowRoot != null)
                WindowRoot.gameObject.SetActive(visible);

            if (instant && WindowRoot != null)
                WindowRoot.gameObject.SetActive(false);
        }

        private void BuildViews()
        {
            if (ContentRoot == null || DiscardPiles == null || DiscardPiles.Length == 0)
                return;

            for (int p = 0; p < DiscardPiles.Length; p++)
            {
                OkeyDiscardPile pile = DiscardPiles[p];
                if (pile == null)
                    continue;

                List<OkeyTileInstance> tiles = pile.GetDiscardedTilesSnapshot();
                for (int i = 0; i < tiles.Count; i++)
                {
                    OkeyTileInstance source = tiles[i];
                    if (source == null)
                        continue;

                    GameObject clone = Instantiate(source.gameObject, ContentRoot);
                    clone.name = source.gameObject.name + "_AtilanView";
                    clone.SetActive(true);

                    PrepareClone(clone);
                    spawnedViews.Add(clone);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRoot);
        }

        private void PrepareClone(GameObject clone)
        {
            if (clone == null)
                return;

            OkeyTileDrag[] drags = clone.GetComponentsInChildren<OkeyTileDrag>(true);
            for (int i = 0; i < drags.Length; i++)
                drags[i].enabled = false;

            CanvasGroup[] groups = clone.GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i].blocksRaycasts = false;
                groups[i].interactable = false;
                groups[i].alpha = 1f;
            }

            Graphic[] graphics = clone.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;

            OkeyTileInstance tile = clone.GetComponent<OkeyTileInstance>();
            if (tile != null)
            {
                tile.ShowFront();
                tile.RefreshVisuals();
            }

            RectTransform rt = clone.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchoredPosition = Vector2.zero;
            }
        }

        private void ClearViews()
        {
            for (int i = 0; i < spawnedViews.Count; i++)
            {
                if (spawnedViews[i] != null)
                    Destroy(spawnedViews[i]);
            }

            spawnedViews.Clear();
        }
    }
}