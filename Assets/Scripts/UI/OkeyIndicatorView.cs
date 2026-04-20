using UnityEngine;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyIndicatorView : MonoBehaviour
    {
        [Header("UI")]
        public RectTransform VisualRoot;

        [Header("Runtime (Read Only)")]
        [SerializeField] private OkeyTileInstance currentIndicator;

        private void Awake()
        {
            if (VisualRoot == null)
                VisualRoot = transform as RectTransform;
        }

        public void ShowIndicator(OkeyTileInstance tile)
        {
            if (tile == null)
            {
                Debug.LogWarning("[OkeyIndicatorView] Indicator tile is NULL.");
                return;
            }

            if (VisualRoot == null)
                VisualRoot = transform as RectTransform;

            if (VisualRoot == null)
            {
                Debug.LogError("[OkeyIndicatorView] VisualRoot is NULL.");
                return;
            }

            if (currentIndicator != null && currentIndicator != tile)
            {
                currentIndicator.gameObject.SetActive(false);
                currentIndicator.transform.SetParent(null, false);
            }

            currentIndicator = tile;

            tile.transform.SetParent(VisualRoot, false);
            tile.transform.SetAsLastSibling();
            tile.gameObject.SetActive(true);
            tile.SetFaceVisible(true);

            if (tile.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }

            Debug.Log($"[OkeyIndicatorView] Show indicator: {tile.Color}-{tile.Number}");
        }

        public void ClearIndicator()
        {
            if (currentIndicator == null)
                return;

            currentIndicator.gameObject.SetActive(false);
            currentIndicator.transform.SetParent(null, false);
            currentIndicator = null;
        }
    }
}