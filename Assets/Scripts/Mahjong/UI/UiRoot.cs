using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class UiRoot : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private RectTransform safeRoot;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private RectTransform trayArea;
        [SerializeField] private RectTransform tilesRoot;

        [Header("Landscape Layout")]
        [SerializeField] private float sidePadding = 40f;
        [SerializeField] private float topPadding = 24f;
        [SerializeField] private float bottomPadding = 24f;
        [SerializeField] private float trayTopOffset = 30f;
        [SerializeField] private float gapBetweenTrayAndBoard = 20f;

        [Header("Board Margins")]
        [SerializeField] private float boardTopPadding = 0f;
        [SerializeField] private float boardBottomPadding = 0f;

        private bool applying;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Start()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            Apply();
        }

        private void OnEnable()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            Apply();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                Apply();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            KeepTransformsStable();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            sidePadding = Mathf.Max(0f, sidePadding);
            topPadding = Mathf.Max(0f, topPadding);
            bottomPadding = Mathf.Max(0f, bottomPadding);
            trayTopOffset = Mathf.Max(0f, trayTopOffset);
            gapBetweenTrayAndBoard = Mathf.Max(0f, gapBetweenTrayAndBoard);
            boardTopPadding = Mathf.Max(0f, boardTopPadding);
            boardBottomPadding = Mathf.Max(0f, boardBottomPadding);
        }
#endif

        [ContextMenu("Apply")]
        public void Apply()
        {
            if (applying)
                return;

            if (safeRoot == null || boardArea == null || trayArea == null)
                return;

            applying = true;

            ApplySafeArea();
            SetTray();
            SetBoard();
            SetTilesRoot();
            KeepTransformsStable();

            applying = false;
        }

        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            safeRoot.anchorMin = min;
            safeRoot.anchorMax = max;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
            safeRoot.pivot = new Vector2(0.5f, 0.5f);
            safeRoot.localScale = Vector3.one;
            safeRoot.localRotation = Quaternion.identity;
        }

        private void SetTray()
        {
            trayArea.anchorMin = new Vector2(0.5f, 1f);
            trayArea.anchorMax = new Vector2(0.5f, 1f);
            trayArea.pivot = new Vector2(0.5f, 1f);
            trayArea.localScale = Vector3.one;
            trayArea.localRotation = Quaternion.identity;
            trayArea.anchoredPosition = new Vector2(0f, -trayTopOffset);
        }

        private void SetBoard()
        {
            boardArea.anchorMin = new Vector2(0f, 0f);
            boardArea.anchorMax = new Vector2(1f, 1f);
            boardArea.pivot = new Vector2(0.5f, 0.5f);
            boardArea.localRotation = Quaternion.identity;
            boardArea.localScale = Vector3.one;

            float trayHeight = trayArea.rect.height;
            float top = trayTopOffset + trayHeight + gapBetweenTrayAndBoard + boardTopPadding + topPadding;
            float bottom = bottomPadding + boardBottomPadding;

            boardArea.offsetMin = new Vector2(sidePadding, bottom);
            boardArea.offsetMax = new Vector2(-sidePadding, -top);
        }

        private void SetTilesRoot()
        {
            if (tilesRoot == null)
                return;

            if (tilesRoot.parent != boardArea)
                tilesRoot.SetParent(boardArea, false);

            tilesRoot.anchorMin = new Vector2(0.5f, 0.5f);
            tilesRoot.anchorMax = new Vector2(0.5f, 0.5f);
            tilesRoot.pivot = new Vector2(0.5f, 0.5f);
            tilesRoot.anchoredPosition = Vector2.zero;
            tilesRoot.localScale = Vector3.one;
            tilesRoot.localRotation = Quaternion.identity;
        }

        private void KeepTransformsStable()
        {
            if (safeRoot != null)
            {
                safeRoot.localScale = Vector3.one;
                safeRoot.localRotation = Quaternion.identity;
            }

            if (boardArea != null)
            {
                boardArea.localScale = Vector3.one;
                boardArea.localRotation = Quaternion.identity;
            }

            if (trayArea != null)
            {
                trayArea.localScale = Vector3.one;
                trayArea.localRotation = Quaternion.identity;
            }

            if (tilesRoot != null)
            {
                tilesRoot.localScale = Vector3.one;
                tilesRoot.localRotation = Quaternion.identity;
            }
        }
    }
}