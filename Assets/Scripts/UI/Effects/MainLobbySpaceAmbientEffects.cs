using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MainLobbySpaceAmbientEffects : MonoBehaviour
    {
        [Header("Twinkle Stars")]
        [SerializeField] private bool twinkleEnabled = false;
        [SerializeField] private int twinkleStarCount = 10;
        [SerializeField] private Vector2 twinkleSizeRange = new Vector2(1.6f, 3.8f);
        [SerializeField] private Vector2 twinkleAlphaRange = new Vector2(0.08f, 0.42f);
        [SerializeField] private Vector2 twinkleSpeedRange = new Vector2(0.35f, 0.9f);

        [Header("Soft Haze")]
        [SerializeField] private bool hazeEnabled = true;
        [SerializeField] private float hazeAlpha = 0.055f;
        [SerializeField] private float hazeDriftSpeed = 1.25f;
        [SerializeField] private Color hazeColor = new Color(0.1f, 0.28f, 0.58f, 1f);

        [Header("Near Star Parallax")]
        [SerializeField] private bool nearStarsEnabled = false;
        [SerializeField] private int nearStarCount = 12;
        [SerializeField] private Vector2 nearStarSizeRange = new Vector2(0.9f, 2.2f);
        [SerializeField] private Vector2 nearStarAlphaRange = new Vector2(0.06f, 0.18f);
        [SerializeField] private float nearStarTiltStrength = 0.035f;
        [SerializeField] private float nearStarSmoothing = 5.5f;
        [SerializeField] private bool invertTilt = false;

        [Header("Background Sync")]
        [SerializeField] private bool syncVerticalDrift = true;
        [SerializeField] private bool moveDown = true;
        [SerializeField] private float verticalDriftSpeed = 22f;

        private readonly List<TwinkleStar> twinkleStars = new List<TwinkleStar>(16);
        private readonly List<ParallaxStar> nearStars = new List<ParallaxStar>(24);
        private RectTransform rectTransform;
        private Sprite dotSprite;
        private Sprite hazeSprite;
        private Texture2D dotTexture;
        private Texture2D hazeTexture;
        private RectTransform hazeA;
        private RectTransform hazeB;
        private float parallaxX;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateSharedSprites();
        }

        private void Start()
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            if (hazeEnabled)
            {
                CreateHaze(rect);
            }

            if (twinkleEnabled)
            {
                CreateTwinkles(rect);
            }

            if (nearStarsEnabled)
            {
                CreateNearStars(rect);
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            UpdateHaze();
            UpdateTwinkles(time);
            UpdateNearStars();
        }

        private void OnDestroy()
        {
            if (dotSprite != null)
            {
                Destroy(dotSprite);
            }

            if (hazeSprite != null)
            {
                Destroy(hazeSprite);
            }

            if (dotTexture != null)
            {
                Destroy(dotTexture);
            }

            if (hazeTexture != null)
            {
                Destroy(hazeTexture);
            }
        }

        private void CreateSharedSprites()
        {
            dotTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "MainLobbyAmbientDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            dotTexture.SetPixel(0, 0, Color.white);
            dotTexture.Apply();
            dotSprite = Sprite.Create(dotTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            const int size = 256;
            hazeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MainLobbyNaturalNebulaHaze",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float seedX = Random.Range(0f, 1000f);
            float seedY = Random.Range(0f, 1000f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float nx = u * 2f - 1f;
                    float ny = v * 2f - 1f;

                    float broad = Mathf.PerlinNoise(seedX + u * 1.35f, seedY + v * 1.15f);
                    float mid = Mathf.PerlinNoise(seedX + 30f + u * 3.2f + v * 0.65f, seedY + 40f + v * 2.6f);
                    float fine = Mathf.PerlinNoise(seedX + 80f + u * 7.5f, seedY + 90f + v * 5.4f);
                    float wisps = Mathf.PerlinNoise(seedX + 160f + (u + v * 0.42f) * 4.3f, seedY + 180f + (v - u * 0.18f) * 2.1f);

                    float noise = broad * 0.42f + mid * 0.34f + fine * 0.12f + wisps * 0.12f;
                    float verticalFade = Mathf.SmoothStep(0f, 0.42f, v) * (1f - Mathf.SmoothStep(0.7f, 1f, v));
                    float sideFade = Mathf.SmoothStep(0f, 0.28f, u) * (1f - Mathf.SmoothStep(0.76f, 1f, u));
                    float diagonalBand = Mathf.Exp(-Mathf.Pow((ny + nx * 0.55f) / 0.82f, 2f));
                    float edgeFade = Mathf.Clamp01(verticalFade * 0.75f + sideFade * 0.25f);
                    float alpha = Mathf.SmoothStep(0.52f, 0.92f, noise) * diagonalBand * edgeFade;
                    alpha = Mathf.Pow(alpha, 1.85f) * 0.62f;
                    hazeTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            hazeTexture.Apply();
            hazeSprite = Sprite.Create(hazeTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void CreateHaze(Rect rect)
        {
            hazeA = CreateHazeImage("DeepSpaceNebula_A", rect, new Vector2(-0.12f, -0.16f), 1.55f, -11f, hazeAlpha);
            hazeB = CreateHazeImage("DeepSpaceNebula_B", rect, new Vector2(0.24f, 0.22f), 1.25f, 17f, hazeAlpha * 0.55f);
        }

        private RectTransform CreateHazeImage(string objectName, Rect rect, Vector2 normalizedPosition, float scale, float rotation, float alpha)
        {
            GameObject hazeObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hazeObject.layer = gameObject.layer;
            hazeObject.transform.SetParent(transform, false);
            hazeObject.transform.SetAsLastSibling();

            Image image = hazeObject.GetComponent<Image>();
            image.sprite = hazeSprite;
            image.color = new Color(hazeColor.r, hazeColor.g, hazeColor.b, alpha);
            image.raycastTarget = false;

            RectTransform hazeRect = hazeObject.GetComponent<RectTransform>();
            hazeRect.anchorMin = new Vector2(0.5f, 0.5f);
            hazeRect.anchorMax = new Vector2(0.5f, 0.5f);
            hazeRect.pivot = new Vector2(0.5f, 0.5f);
            hazeRect.anchoredPosition = new Vector2(rect.width * normalizedPosition.x, rect.height * normalizedPosition.y);
            float size = Mathf.Max(rect.width, rect.height) * scale;
            hazeRect.sizeDelta = new Vector2(size, size);
            hazeRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return hazeRect;
        }

        private void CreateTwinkles(Rect rect)
        {
            for (int i = 0; i < twinkleStarCount; i++)
            {
                RectTransform starRect = CreateDot("TwinkleStar", rect, twinkleSizeRange, Random.Range(twinkleAlphaRange.x, twinkleAlphaRange.y));
                twinkleStars.Add(new TwinkleStar
                {
                    Image = starRect.GetComponent<Image>(),
                    RectTransform = starRect,
                    Speed = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    MinAlpha = twinkleAlphaRange.x,
                    MaxAlpha = twinkleAlphaRange.y
                });
            }
        }

        private void CreateNearStars(Rect rect)
        {
            for (int i = 0; i < nearStarCount; i++)
            {
                RectTransform starRect = CreateDot("NearParallaxStar", rect, nearStarSizeRange, Random.Range(nearStarAlphaRange.x, nearStarAlphaRange.y));
                nearStars.Add(new ParallaxStar
                {
                    RectTransform = starRect,
                    BasePosition = starRect.anchoredPosition,
                    Depth = Random.Range(0.35f, 1f)
                });
            }
        }

        private RectTransform CreateDot(string objectName, Rect rect, Vector2 sizeRange, float alpha)
        {
            GameObject dotObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dotObject.layer = gameObject.layer;
            dotObject.transform.SetParent(transform, false);
            dotObject.transform.SetAsLastSibling();

            Image image = dotObject.GetComponent<Image>();
            image.sprite = dotSprite;
            image.color = new Color(0.8f, 0.92f, 1f, alpha);
            image.raycastTarget = false;

            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
            float size = Random.Range(sizeRange.x, sizeRange.y);
            dotRect.sizeDelta = new Vector2(size, size);
            return dotRect;
        }

        private void UpdateHaze()
        {
            if (hazeA == null || hazeB == null)
            {
                return;
            }

            float t = Time.unscaledTime * hazeDriftSpeed;
            hazeA.anchoredPosition += new Vector2(Mathf.Sin(t * 0.13f), Mathf.Cos(t * 0.11f)) * Time.unscaledDeltaTime;
            hazeB.anchoredPosition += new Vector2(Mathf.Cos(t * 0.1f), Mathf.Sin(t * 0.14f)) * Time.unscaledDeltaTime;
        }

        private void UpdateTwinkles(float time)
        {
            for (int i = 0; i < twinkleStars.Count; i++)
            {
                TwinkleStar star = twinkleStars[i];
                MoveWithBackground(star.RectTransform);

                float pulse = (Mathf.Sin(time * star.Speed + star.Phase) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(star.MinAlpha, star.MaxAlpha, pulse * pulse);
                Color color = star.Image.color;
                color.a = alpha;
                star.Image.color = color;
            }
        }

        private void UpdateNearStars()
        {
            if (nearStars.Count == 0)
            {
                return;
            }

            float tilt = GetLandscapeTilt();
            if (invertTilt)
            {
                tilt = -tilt;
            }

            float target = Mathf.Clamp(tilt, -1f, 1f) * nearStarTiltStrength * rectTransform.rect.width;
            parallaxX = Mathf.Lerp(parallaxX, target, 1f - Mathf.Exp(-nearStarSmoothing * Time.unscaledDeltaTime));

            for (int i = 0; i < nearStars.Count; i++)
            {
                ParallaxStar star = nearStars[i];
                star.BasePosition = MoveBaseWithBackground(star.BasePosition);
                star.RectTransform.anchoredPosition = star.BasePosition + new Vector2(parallaxX * star.Depth, 0f);
                nearStars[i] = star;
            }
        }

        private void MoveWithBackground(RectTransform target)
        {
            if (!syncVerticalDrift || target == null)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float direction = moveDown ? -1f : 1f;
            Vector2 position = target.anchoredPosition;
            position.y += direction * verticalDriftSpeed * Time.unscaledDeltaTime;

            float padding = 24f;
            if (position.y < rect.yMin - padding)
            {
                position.y = rect.yMax + padding;
                position.x = Random.Range(rect.xMin, rect.xMax);
            }
            else if (position.y > rect.yMax + padding)
            {
                position.y = rect.yMin - padding;
                position.x = Random.Range(rect.xMin, rect.xMax);
            }

            target.anchoredPosition = position;
        }

        private Vector2 MoveBaseWithBackground(Vector2 position)
        {
            if (!syncVerticalDrift)
            {
                return position;
            }

            Rect rect = rectTransform.rect;
            float direction = moveDown ? -1f : 1f;
            position.y += direction * verticalDriftSpeed * Time.unscaledDeltaTime;

            float padding = 24f;
            if (position.y < rect.yMin - padding)
            {
                position.y = rect.yMax + padding;
                position.x = Random.Range(rect.xMin, rect.xMax);
            }
            else if (position.y > rect.yMax + padding)
            {
                position.y = rect.yMin - padding;
                position.x = Random.Range(rect.xMin, rect.xMax);
            }

            return position;
        }

        private float GetLandscapeTilt()
        {
            Vector3 acceleration = Input.acceleration;
            float tilt = acceleration.x;

            if (Screen.orientation == ScreenOrientation.LandscapeLeft)
            {
                tilt = acceleration.y;
            }
            else if (Screen.orientation == ScreenOrientation.LandscapeRight)
            {
                tilt = -acceleration.y;
            }

            return tilt;
        }

        private struct TwinkleStar
        {
            public Image Image;
            public RectTransform RectTransform;
            public float Speed;
            public float Phase;
            public float MinAlpha;
            public float MaxAlpha;
        }

        private struct ParallaxStar
        {
            public RectTransform RectTransform;
            public Vector2 BasePosition;
            public float Depth;
        }
    }
}
