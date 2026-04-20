using UnityEngine;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyRackSlotsAutoBuilder : MonoBehaviour
    {
        [Header("Grid")]
        [Min(1)] public int Columns = 16;
        [Min(1)] public int Rows = 2;

        [Header("Auto Build")]
        [Tooltip("Если включено — будет автоматически пересобирать слоты в Edit Mode при изменениях.")]
        public bool AutoBuildInEditMode = true;

        [Tooltip("Если включено — будет собирать слоты при запуске сцены.")]
        public bool AutoBuildInPlayMode = true;

        [Tooltip("Подогнать размер RectTransform SlotGrid под сетку (удобно, чтобы 2 ряда точно помещались).")]
        public bool AutoResizeGridRect = true;

        [Header("Layout")]
        public Vector2 CellSize = new Vector2(120, 160);
        public Vector2 Spacing = new Vector2(8, 8);
        public RectOffset Padding = new RectOffset(0, 0, 0, 0);

        [Header("Slot Visual (Optional)")]
        public bool DebugSlotBackground = false;

        public int TotalSlots => Mathf.Max(1, Columns * Rows);

        private bool _queuedRebuild;

        private void Awake()
        {
            if (Application.isPlaying && AutoBuildInPlayMode)
                RebuildSlotsNow();
        }

        private void OnEnable()
        {
            // В Edit Mode безопаснее ребилдить "позже", чтобы не ловить SendMessage warnings.
            if (!Application.isPlaying && AutoBuildInEditMode)
                QueueRebuild();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (!AutoBuildInEditMode) return;

            QueueRebuild();
        }
#endif

        private void Update()
        {
            if (!_queuedRebuild) return;
            _queuedRebuild = false;

            // В ExecuteAlways Update вызывается и в Edit Mode
            if (!Application.isPlaying && AutoBuildInEditMode)
                RebuildSlotsNow();
        }

        private void QueueRebuild()
        {
            _queuedRebuild = true;
        }

        [ContextMenu("Rebuild Slots Now")]
        public void RebuildSlotsNow()
        {
            EnsureGridConfigured();
            EnsureSlots();
            if (AutoResizeGridRect) ResizeGridRectToFit();
        }

        private void EnsureGridConfigured()
        {
            var grid = GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = gameObject.AddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;

            grid.cellSize = CellSize;
            grid.spacing = Spacing;
            grid.padding = Padding;

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        private void EnsureSlots()
        {
            int need = TotalSlots;

            // add
            while (transform.childCount < need)
                CreateSlot(transform.childCount);

            // remove
            while (transform.childCount > need)
            {
                var last = transform.GetChild(transform.childCount - 1);
                DestroySafe(last.gameObject);
            }

            // rename
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).name = $"Slot_{i:00}";
        }

        private void CreateSlot(int index)
        {
            var go = new GameObject($"Slot_{index:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = DebugSlotBackground ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0f);
        }

        private void ResizeGridRectToFit()
        {
            var rt = (RectTransform)transform;

            float width =
                Padding.left + Padding.right +
                (Columns * CellSize.x) +
                ((Columns - 1) * Spacing.x);

            float height =
                Padding.top + Padding.bottom +
                (Rows * CellSize.y) +
                ((Rows - 1) * Spacing.y);

            rt.sizeDelta = new Vector2(width, height);
        }

        private static void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(go);
            else Object.Destroy(go);
#else
            Object.Destroy(go);
#endif
        }
    }
}