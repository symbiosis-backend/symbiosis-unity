using UnityEngine;

namespace MahjongGame
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BattleBoardsHeightFirstLayout : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private RectTransform leftBoard;
        [SerializeField] private RectTransform rightBoard;

        [Header("Placement")]
        [SerializeField] private float leftPadding = 0f;
        [SerializeField] private float rightPadding = 0f;
        [SerializeField] private float bottomPadding = 0f;
        [SerializeField] private float centerGap = 0f;

        [Header("Height Limit")]
        [SerializeField] private float topReserved = 180f;
        [SerializeField] private float targetHeightPercent = 0.52f;
        [SerializeField] private float minBoardHeight = 180f;
        [SerializeField] private float maxBoardHeight = 1200f;

        [Header("Shape")]
        [SerializeField] private float boardAspect = 1f; // 1 = square

        [Header("Runtime")]
        [SerializeField] private bool adaptContinuously = true;
        [SerializeField] private bool applyInEditMode = true;

        private RectTransform root;
        private Vector2 lastSize;

        private void Awake()
        {
            root = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()
        {
            root = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (!adaptContinuously)
                return;

            if (!Application.isPlaying && !applyInEditMode)
                return;

            if (root == null)
                root = GetComponent<RectTransform>();

            Vector2 size = root.rect.size;
            if (size != lastSize)
                Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            if (!Application.isPlaying && !applyInEditMode)
                return;

            Apply();
        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            if (root == null)
                root = GetComponent<RectTransform>();

            if (leftBoard == null || rightBoard == null)
                return;

            Rect r = root.rect;
            lastSize = r.size;

            float rootWidth = r.width;
            float rootHeight = r.height;

            if (rootWidth <= 0f || rootHeight <= 0f)
                return;

            float availableWidth = rootWidth - leftPadding - rightPadding - centerGap;
            float availableHeight = rootHeight - bottomPadding - topReserved;

            if (availableWidth <= 0f || availableHeight <= 0f)
                return;

            float targetHeight = Mathf.Clamp(rootHeight * targetHeightPercent, minBoardHeight, maxBoardHeight);
            float boardHeight = Mathf.Min(targetHeight, availableHeight);

            float safeAspect = Mathf.Max(0.01f, boardAspect);
            float boardWidthFromHeight = boardHeight * safeAspect;
            float maxBoardWidthToCenter = availableWidth * 0.5f;

            float boardWidth = Mathf.Min(boardWidthFromHeight, maxBoardWidthToCenter);
            boardHeight = boardWidth / safeAspect;

            ApplyLeft(boardWidth, boardHeight);
            ApplyRight(boardWidth, boardHeight);
        }

        private void ApplyLeft(float width, float height)
        {
            leftBoard.anchorMin = new Vector2(0f, 0f);
            leftBoard.anchorMax = new Vector2(0f, 0f);
            leftBoard.pivot = new Vector2(0f, 0f);
            leftBoard.anchoredPosition = new Vector2(leftPadding, bottomPadding);
            leftBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            leftBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void ApplyRight(float width, float height)
        {
            rightBoard.anchorMin = new Vector2(1f, 0f);
            rightBoard.anchorMax = new Vector2(1f, 0f);
            rightBoard.pivot = new Vector2(1f, 0f);
            rightBoard.anchoredPosition = new Vector2(-rightPadding, bottomPadding);
            rightBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rightBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}