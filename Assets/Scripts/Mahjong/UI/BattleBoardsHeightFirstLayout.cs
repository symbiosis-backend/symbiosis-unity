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
        [SerializeField] private float centerGap = 24f;
        [SerializeField] private float maxCenterGap = 36f;
        [SerializeField] private float centerGapPercent = 0.012f;
        [SerializeField] private float horizontalPaddingPercent = 0.025f;
        [SerializeField] private float maxHorizontalPadding = 48f;

        [Header("Height Limit")]
        [SerializeField] private float topReserved = 180f;
        [SerializeField] private float targetHeightPercent = 0.52f;
        [SerializeField] private float minBoardHeight = 180f;
        [SerializeField] private float maxBoardHeight = 1200f;

        [Header("Phone Stretch")]
        [SerializeField] private float phoneAspectStart = 1.75f;
        [SerializeField] private float phoneAspectFull = 2.15f;
        [SerializeField] private float phoneWidthFill = 0.985f;
        [SerializeField] private float tabletWidthFill = 0.88f;

        [Header("Shape")]
        [SerializeField] private float boardAspect = 1.4625f;

        [Header("Runtime")]
        [SerializeField] private bool adaptContinuously = true;
        [SerializeField] private bool applyInEditMode = true;

        private RectTransform root;
        private Vector2 lastSize;
        private Vector2 lastLeftSize;
        private Vector2 lastRightSize;

        public void Configure(RectTransform left, RectTransform right)
        {
            leftBoard = left;
            rightBoard = right;
            Apply();
        }

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
            Vector2 leftSize = leftBoard != null ? leftBoard.rect.size : Vector2.zero;
            Vector2 rightSize = rightBoard != null ? rightBoard.rect.size : Vector2.zero;

            if (size != lastSize || leftSize != lastLeftSize || rightSize != lastRightSize)
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

            float safeMaxGap = Mathf.Max(centerGap, maxCenterGap);
            float safeMaxPadding = Mathf.Max(0f, maxHorizontalPadding);
            float adaptiveGap = Mathf.Clamp(rootWidth * centerGapPercent, centerGap, safeMaxGap);
            float adaptiveSidePadding = Mathf.Min(rootWidth * horizontalPaddingPercent, safeMaxPadding);
            float safeLeftPadding = Mathf.Max(leftPadding, adaptiveSidePadding);
            float safeRightPadding = Mathf.Max(rightPadding, adaptiveSidePadding);

            float availableWidth = rootWidth - safeLeftPadding - safeRightPadding - adaptiveGap;
            float availableHeight = rootHeight - bottomPadding - topReserved;

            if (availableWidth <= 0f || availableHeight <= 0f)
                return;

            float safeAspect = Mathf.Max(0.01f, boardAspect);
            float screenAspect = rootWidth / rootHeight;
            float phoneBlend = Mathf.InverseLerp(phoneAspectStart, phoneAspectFull, screenAspect);
            float widthFill = Mathf.Lerp(tabletWidthFill, phoneWidthFill, phoneBlend);

            float targetHeight = Mathf.Clamp(rootHeight * targetHeightPercent, minBoardHeight, maxBoardHeight);
            float boardWidthFromHeight = Mathf.Min(targetHeight, availableHeight) * safeAspect;
            float maxBoardWidthToCenter = availableWidth * 0.5f;
            float boardWidthFromWidth = maxBoardWidthToCenter * Mathf.Clamp01(widthFill);
            float boardWidthTarget = Mathf.Lerp(boardWidthFromHeight, boardWidthFromWidth, phoneBlend);

            float boardWidth = Mathf.Min(boardWidthTarget, maxBoardWidthToCenter);
            float boardHeight = Mathf.Min(boardWidth / safeAspect, availableHeight);
            boardWidth = boardHeight * safeAspect;

            float totalBoardsWidth = boardWidth * 2f + adaptiveGap;
            float groupLeft = safeLeftPadding + Mathf.Max(0f, (availableWidth + adaptiveGap - totalBoardsWidth) * 0.5f);
            float rightX = groupLeft + boardWidth + adaptiveGap;

            ApplyLeft(groupLeft, boardWidth, boardHeight);
            ApplyRight(rightX, boardWidth, boardHeight);

            lastLeftSize = leftBoard.rect.size;
            lastRightSize = rightBoard.rect.size;
        }

        private void ApplyLeft(float x, float width, float height)
        {
            leftBoard.anchorMin = new Vector2(0f, 0f);
            leftBoard.anchorMax = new Vector2(0f, 0f);
            leftBoard.pivot = new Vector2(0f, 0f);
            leftBoard.anchoredPosition = new Vector2(x, bottomPadding);
            leftBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            leftBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void ApplyRight(float x, float width, float height)
        {
            rightBoard.anchorMin = new Vector2(0f, 0f);
            rightBoard.anchorMax = new Vector2(0f, 0f);
            rightBoard.pivot = new Vector2(0f, 0f);
            rightBoard.anchoredPosition = new Vector2(x, bottomPadding);
            rightBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rightBoard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
