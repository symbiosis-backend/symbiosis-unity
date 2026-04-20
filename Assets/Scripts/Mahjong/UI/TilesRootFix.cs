using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TilesRootFix : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private bool stretchToParent = true;
        [SerializeField] private bool forceRootScaleOne = true;
        [SerializeField] private bool forceRootAnchoredPosZero = true;

        [Header("Tiles")]
        [SerializeField] private bool controlTileSize = true;
        [SerializeField] private Vector2 tileSize = new Vector2(72f, 96f);
        [SerializeField] private bool controlTileScale = true;
        [SerializeField] private Vector3 tileScale = Vector3.one;

        [Header("Live Refresh")]
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyEveryFrame = false;

        private RectTransform rect;
        private RectTransform parentRect;

        private void Awake()
        {
            Cache();
            if (applyOnAwake)
                ApplyNow();
        }

        private void OnEnable()
        {
            Cache();
            if (applyOnEnable)
                ApplyNow();
        }

        private void LateUpdate()
        {
            if (applyEveryFrame)
                ApplyNow();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyNow();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            tileSize.x = Mathf.Max(8f, tileSize.x);
            tileSize.y = Mathf.Max(8f, tileSize.y);

            Cache();
            ApplyNow();
        }
#endif

        [ContextMenu("Apply Now")]
        public void ApplyNow()
        {
            Cache();

            if (rect == null)
                return;

            if (stretchToParent)
                StretchRoot();

            if (forceRootScaleOne)
                rect.localScale = Vector3.one;

            if (forceRootAnchoredPosZero)
                rect.anchoredPosition = Vector2.zero;

            if (!controlTileSize && !controlTileScale)
                return;

            for (int i = 0; i < rect.childCount; i++)
            {
                Transform child = rect.GetChild(i);
                if (child == null)
                    continue;

                RectTransform childRect = child as RectTransform;
                if (childRect == null)
                    continue;

                if (controlTileSize)
                    childRect.sizeDelta = tileSize;

                if (controlTileScale)
                    childRect.localScale = tileScale;
            }
        }

        private void Cache()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            if (parentRect == null && transform.parent != null)
                parentRect = transform.parent as RectTransform;
        }

        private void StretchRoot()
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}