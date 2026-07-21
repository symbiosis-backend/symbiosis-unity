using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MainLobbyCometField : MonoBehaviour
    {
        [SerializeField] private bool play = true;
        [SerializeField] private int maxActiveComets = 3;
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(2.8f, 6.5f);
        [SerializeField] private Vector2 lifetimeRange = new Vector2(1.8f, 3.4f);
        [SerializeField] private Vector2 lengthRange = new Vector2(42f, 86f);
        [SerializeField] private Vector2 thicknessRange = new Vector2(5f, 9f);
        [SerializeField] private Vector2 alphaRange = new Vector2(0.24f, 0.46f);
        [SerializeField] private float offscreenPadding = 90f;
        [SerializeField] private Color cometColor = new Color(0.78f, 0.92f, 1f, 1f);

        private readonly List<Comet> comets = new List<Comet>(4);
        private RectTransform rectTransform;
        private Sprite cometSprite;
        private Texture2D cometTexture;
        private float nextSpawnTime;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateSprite();
            ScheduleNextSpawn();
        }

        private void Update()
        {
            UpdateComets();

            if (!play || comets.Count >= maxActiveComets || Time.unscaledTime < nextSpawnTime)
            {
                return;
            }

            SpawnComet();
            ScheduleNextSpawn();
        }

        private void OnDestroy()
        {
            for (int i = comets.Count - 1; i >= 0; i--)
            {
                if (comets[i].Image != null)
                {
                    Destroy(comets[i].Image.gameObject);
                }
            }

            if (cometSprite != null)
            {
                Destroy(cometSprite);
            }

            if (cometTexture != null)
            {
                Destroy(cometTexture);
            }
        }

        private void CreateSprite()
        {
            const int width = 96;
            const int height = 16;

            cometTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "MainLobbyCometTail",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float ny = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
                    float tail = Mathf.SmoothStep(0f, 1f, nx);
                    float taper = Mathf.Pow(Mathf.Clamp01(1f - ny), 2.8f);
                    float head = Mathf.Exp(-Mathf.Pow((nx - 0.93f) / 0.08f, 2f) - Mathf.Pow(ny / 0.55f, 2f));
                    float alpha = Mathf.Clamp01(tail * taper * 0.55f + head);
                    cometTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            cometTexture.Apply();
            cometSprite = Sprite.Create(cometTexture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 1f);
        }

        private void ScheduleNextSpawn()
        {
            float min = Mathf.Max(0.2f, spawnDelayRange.x);
            float max = Mathf.Max(min, spawnDelayRange.y);
            nextSpawnTime = Time.unscaledTime + Random.Range(min, max);
        }

        private void SpawnComet()
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            Vector2 start = GetStartPosition(rect);
            Vector2 end = GetEndPosition(rect, start);
            float lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
            Vector2 velocity = (end - start) / Mathf.Max(0.1f, lifetime);

            GameObject cometObject = new GameObject("TinyComet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cometObject.layer = gameObject.layer;
            cometObject.transform.SetParent(transform, false);

            Image image = cometObject.GetComponent<Image>();
            image.sprite = cometSprite;
            image.raycastTarget = false;
            image.color = WithAlpha(cometColor, 0f);

            RectTransform cometRect = cometObject.GetComponent<RectTransform>();
            cometRect.anchorMin = new Vector2(0.5f, 0.5f);
            cometRect.anchorMax = new Vector2(0.5f, 0.5f);
            cometRect.pivot = new Vector2(0.5f, 0.5f);
            cometRect.anchoredPosition = start;
            cometRect.sizeDelta = new Vector2(Random.Range(lengthRange.x, lengthRange.y), Random.Range(thicknessRange.x, thicknessRange.y));

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            cometRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            comets.Add(new Comet
            {
                Image = image,
                RectTransform = cometRect,
                Velocity = velocity,
                Lifetime = lifetime,
                MaxAlpha = Random.Range(alphaRange.x, alphaRange.y)
            });
        }

        private Vector2 GetStartPosition(Rect rect)
        {
            bool fromLeft = Random.value < 0.5f;
            float x = fromLeft ? rect.xMin - offscreenPadding : rect.xMax + offscreenPadding;
            float y = Random.Range(rect.yMin + rect.height * 0.12f, rect.yMax - rect.height * 0.12f);
            return new Vector2(x, y);
        }

        private Vector2 GetEndPosition(Rect rect, Vector2 start)
        {
            bool toRight = start.x < rect.center.x;
            float x = toRight ? rect.xMax + offscreenPadding : rect.xMin - offscreenPadding;
            float verticalTravel = Random.Range(rect.height * 0.18f, rect.height * 0.52f);
            verticalTravel *= Random.value < 0.5f ? -1f : 1f;
            float y = start.y + verticalTravel;
            y = Mathf.Clamp(y, rect.yMin - offscreenPadding, rect.yMax + offscreenPadding);
            return new Vector2(x, y);
        }

        private void UpdateComets()
        {
            for (int i = comets.Count - 1; i >= 0; i--)
            {
                Comet comet = comets[i];
                comet.Age += Time.unscaledDeltaTime;

                if (comet.Age >= comet.Lifetime)
                {
                    Destroy(comet.Image.gameObject);
                    comets.RemoveAt(i);
                    continue;
                }

                comet.RectTransform.anchoredPosition += comet.Velocity * Time.unscaledDeltaTime;

                float t = comet.Age / comet.Lifetime;
                float alpha = Mathf.Sin(t * Mathf.PI) * comet.MaxAlpha;
                comet.Image.color = WithAlpha(cometColor, alpha);
                comets[i] = comet;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private struct Comet
        {
            public Image Image;
            public RectTransform RectTransform;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public float MaxAlpha;
        }
    }
}
