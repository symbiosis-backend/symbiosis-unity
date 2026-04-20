using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class CarouselBackgroundOverlay : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private RectTransform carouselRoot;

        [Header("Visual")]
        [SerializeField] private Sprite backgroundImage;
        [SerializeField, Range(0f, 1f)] private float darkness = 0.9f;
        [SerializeField] private bool preserveAspect = false;

        [Header("Behavior")]
        [SerializeField] private bool createOnAwake = true;
        [SerializeField] private bool showOnEnable = true;
        [SerializeField] private bool closeOnBackgroundClick = false;
        [SerializeField] private bool destroyOnDisable = false;

        private GameObject overlayObject;
        private RectTransform overlayRect;
        private Image overlayImage;
        private Button overlayButton;
        private Canvas parentCanvas;

        public bool IsCreated => overlayObject != null;
        public bool IsVisible => overlayObject != null && overlayObject.activeSelf;

        private void Reset()
        {
            if (carouselRoot == null)
                carouselRoot = transform as RectTransform;
        }

        private void Awake()
        {
            if (carouselRoot == null)
                carouselRoot = transform as RectTransform;

            if (createOnAwake)
                EnsureOverlay();
        }

        private void OnEnable()
        {
            if (showOnEnable)
                Show();
        }

        private void OnDisable()
        {
            if (destroyOnDisable)
                DestroyOverlay();
            else
                Hide();
        }

        private void OnDestroy()
        {
            if (overlayObject != null)
                DestroyOverlay();
        }

        public void Show()
        {
            EnsureOverlay();
            ApplyVisual();

            if (overlayObject != null)
                overlayObject.SetActive(true);

            MoveOverlayBehindCarousel();
        }

        public void Hide()
        {
            if (overlayObject != null)
                overlayObject.SetActive(false);
        }

        public void Toggle()
        {
            if (!IsCreated || !IsVisible)
                Show();
            else
                Hide();
        }

        public void SetBackground(Sprite sprite)
        {
            backgroundImage = sprite;
            ApplyVisual();
        }

        public void SetDarkness(float value)
        {
            darkness = Mathf.Clamp01(value);
            ApplyVisual();
        }

        private void EnsureOverlay()
        {
            if (overlayObject != null)
            {
                CacheReferences();
                return;
            }

            if (carouselRoot == null)
                carouselRoot = transform as RectTransform;

            parentCanvas = GetComponentInParent<Canvas>(true);
            if (parentCanvas == null)
            {
                Debug.LogWarning("[CarouselBackgroundOverlay] Parent Canvas not found.", this);
                return;
            }

            Transform existing = parentCanvas.transform.Find("CarouselBackgroundOverlay_Auto");
            if (existing != null)
            {
                overlayObject = existing.gameObject;
                CacheReferences();
                ApplyVisual();
                MoveOverlayBehindCarousel();
                return;
            }

            overlayObject = new GameObject(
                "CarouselBackgroundOverlay_Auto",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            overlayObject.transform.SetParent(parentCanvas.transform, false);

            CacheReferences();

            if (overlayRect != null)
            {
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                overlayRect.localScale = Vector3.one;
                overlayRect.anchoredPosition3D = Vector3.zero;
            }

            if (overlayButton != null)
            {
                overlayButton.onClick.RemoveAllListeners();
                overlayButton.onClick.AddListener(OnOverlayClicked);
            }

            ApplyVisual();
            MoveOverlayBehindCarousel();
        }

        private void CacheReferences()
        {
            if (overlayObject == null)
                return;

            if (overlayRect == null)
                overlayRect = overlayObject.GetComponent<RectTransform>();

            if (overlayImage == null)
                overlayImage = overlayObject.GetComponent<Image>();

            if (overlayButton == null)
                overlayButton = overlayObject.GetComponent<Button>();
        }

        private void ApplyVisual()
        {
            if (overlayImage == null)
                return;

            overlayImage.sprite = backgroundImage;
            overlayImage.type = Image.Type.Simple;
            overlayImage.preserveAspect = preserveAspect;
            overlayImage.raycastTarget = true;

            // Если есть картинка — показываем её как есть, с прозрачностью.
            // Если картинки нет — просто чёрное затемнение.
            if (backgroundImage != null)
                overlayImage.color = new Color(1f, 1f, 1f, darkness);
            else
                overlayImage.color = new Color(0f, 0f, 0f, darkness);

            if (overlayButton != null)
                overlayButton.enabled = true;
        }

        private void MoveOverlayBehindCarousel()
        {
            if (overlayObject == null || carouselRoot == null)
                return;

            Transform overlayTransform = overlayObject.transform;
            Transform carouselCanvasChild = GetCanvasDirectChild(carouselRoot);

            if (overlayTransform.parent == null || carouselCanvasChild == null)
                return;

            if (overlayTransform.parent != carouselCanvasChild.parent)
                return;

            int carouselIndex = carouselCanvasChild.GetSiblingIndex();
            int targetIndex = Mathf.Max(0, carouselIndex - 1);

            overlayTransform.SetSiblingIndex(targetIndex);
        }

        private Transform GetCanvasDirectChild(Transform target)
        {
            if (target == null || parentCanvas == null)
                return null;

            Transform current = target;
            Transform canvasTransform = parentCanvas.transform;

            while (current != null)
            {
                if (current.parent == canvasTransform)
                    return current;

                current = current.parent;
            }

            return null;
        }

        private void OnOverlayClicked()
        {
            if (closeOnBackgroundClick && carouselRoot != null)
                carouselRoot.gameObject.SetActive(false);
        }

        private void DestroyOverlay()
        {
            if (overlayObject == null)
                return;

            Destroy(overlayObject);
            overlayObject = null;
            overlayRect = null;
            overlayImage = null;
            overlayButton = null;
        }
    }
}
