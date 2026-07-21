using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class MahjongIntroMovingBackground : MonoBehaviour
    {
        private const string MovingLayerName = "IntroMovingBackgroundLayer";

        [SerializeField] private float horizontalDrift = 48f;
        [SerializeField] private float verticalDrift = 18f;
        [SerializeField] private float period = 18f;
        [SerializeField] private float overscan = 1.12f;
        [SerializeField] private float tiltParallax = 34f;
        [SerializeField] private float tiltRotation = 1.8f;
        [SerializeField] private float tiltSmoothing = 7f;

        private Image sourceImage;
        private Image movingImage;
        private RectTransform movingRect;
        private float smoothedTilt;

        private void Awake()
        {
            EnsureLayer();
        }

        private void OnEnable()
        {
            EnsureLayer();
            ApplyFrame();
        }

        private void OnDisable()
        {
            if (sourceImage != null)
                sourceImage.enabled = true;
        }

        private void Update()
        {
            if (movingImage == null || movingRect == null)
                EnsureLayer();

            ApplyFrame();
        }

        public void RefreshFromSource()
        {
            EnsureLayer();
            ApplySourceSprite();
            ApplyFrame();
        }

        private void EnsureLayer()
        {
            if (sourceImage == null)
                sourceImage = GetComponent<Image>();

            if (sourceImage == null)
                return;

            sourceImage.raycastTarget = false;

            if (movingImage == null)
            {
                Transform existing = transform.Find(MovingLayerName);
                movingImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (movingImage == null)
            {
                GameObject layer = new GameObject(MovingLayerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                layer.transform.SetParent(transform, false);
                layer.layer = gameObject.layer;
                movingImage = layer.GetComponent<Image>();
            }

            movingRect = movingImage.rectTransform;
            movingImage.raycastTarget = false;
            movingImage.type = Image.Type.Simple;
            movingImage.preserveAspect = false;

            ApplySourceSprite();
            sourceImage.enabled = false;
        }

        private void ApplySourceSprite()
        {
            if (sourceImage == null || movingImage == null)
                return;

            movingImage.sprite = sourceImage.sprite;
            movingImage.color = sourceImage.color;
            movingImage.material = sourceImage.material;
            movingImage.enabled = sourceImage.sprite != null;
        }

        private void ApplyFrame()
        {
            if (movingRect == null)
                return;

            RectTransform rect = transform as RectTransform;
            if (rect == null)
                return;

            Vector2 parentSize = rect.rect.size;
            if (parentSize.x <= 1f || parentSize.y <= 1f)
                return;

            float safeOverscan = Mathf.Max(1f, overscan);
            movingRect.anchorMin = new Vector2(0.5f, 0.5f);
            movingRect.anchorMax = new Vector2(0.5f, 0.5f);
            movingRect.pivot = new Vector2(0.5f, 0.5f);
            movingRect.sizeDelta = new Vector2(parentSize.x * safeOverscan, parentSize.y * safeOverscan);
            movingRect.localScale = Vector3.one;
            movingRect.SetAsFirstSibling();

            float safePeriod = Mathf.Max(1f, period);
            float phase = (Time.unscaledTime / safePeriod) * Mathf.PI * 2f;
            float tilt = ResolveTilt();

            movingRect.anchoredPosition = new Vector2(
                Mathf.Sin(phase) * horizontalDrift + tilt * tiltParallax,
                Mathf.Cos(phase * 0.7f) * verticalDrift);
            movingRect.localRotation = Quaternion.Euler(0f, 0f, -tilt * tiltRotation);
        }

        private float ResolveTilt()
        {
            float rawTilt = 0f;

            if (SystemInfo.supportsAccelerometer)
                rawTilt = Mathf.Clamp(Input.acceleration.x, -1f, 1f);

            float smoothing = Mathf.Max(1f, tiltSmoothing);
            smoothedTilt = Mathf.Lerp(smoothedTilt, rawTilt, Time.unscaledDeltaTime * smoothing);
            return smoothedTilt;
        }
    }
}
