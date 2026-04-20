using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public sealed class UIButtonFX : MonoBehaviour
{
    [Header("Breath")]
    [SerializeField] private bool useBreath = true;
    [SerializeField] private float breathSpeed = 2f;
    [SerializeField] private float breathAmount = 0.02f;

    [Header("Brightness")]
    [SerializeField] private bool useBrightnessPulse = true;
    [SerializeField] private float brightnessSpeed = 2f;
    [SerializeField] private float brightnessAmount = 0.05f;

    [Header("Sparkles")]
    [SerializeField] private bool useSparkles = true;
    [SerializeField] private int sparkleCount = 2;
    [SerializeField] private Vector2 sparkleSizeMinMax = new Vector2(22f, 40f);
    [SerializeField] private Vector2 sparkleLifetimeMinMax = new Vector2(0.45f, 0.8f);
    [SerializeField] private Vector2 sparkleDelayMinMax = new Vector2(0.8f, 1.8f);
    [SerializeField] private Vector2 sparkleScaleMinMax = new Vector2(0.7f, 1.15f);
    [SerializeField] private float sparkleAlpha = 0.85f;
    [SerializeField] private float sparkleInset = 18f;
    [SerializeField] private Color sparkleColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private float sparkleRotationSpeed = 18f;

    private RectTransform rectTransform;
    private Image mainImage;
    private Vector3 baseScale;
    private Color baseColor;

    private readonly List<SparkleData> sparkles = new List<SparkleData>();

    private static Sprite sparkleSprite;

    private sealed class SparkleData
    {
        public RectTransform rect;
        public Image image;
        public float timer;
        public float delay;
        public float life;
        public float maxScale;
        public float rotation;
        public float rotationSpeed;
        public bool active;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainImage = GetComponent<Image>();
        baseScale = rectTransform.localScale;
        baseColor = mainImage.color;

        if (useSparkles)
            BuildSparkles();
    }

    private void OnEnable()
    {
        rectTransform.localScale = baseScale;
        mainImage.color = baseColor;

        for (int i = 0; i < sparkles.Count; i++)
            ResetSparkle(sparkles[i], true);
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        if (useBreath)
            ApplyBreath(t);

        if (useBrightnessPulse)
            ApplyBrightnessPulse(t);

        if (useSparkles)
            UpdateSparkles(Time.unscaledDeltaTime);
    }

    private void ApplyBreath(float t)
    {
        float s = 1f + Mathf.Sin(t * breathSpeed) * breathAmount;
        rectTransform.localScale = baseScale * s;
    }

    private void ApplyBrightnessPulse(float t)
    {
        float wave = (Mathf.Sin(t * brightnessSpeed) + 1f) * 0.5f;
        float boost = Mathf.Lerp(1f, 1f + brightnessAmount, wave);

        Color c = baseColor;
        c.r = Mathf.Clamp01(baseColor.r * boost);
        c.g = Mathf.Clamp01(baseColor.g * boost);
        c.b = Mathf.Clamp01(baseColor.b * boost);
        c.a = baseColor.a;
        mainImage.color = c;
    }

    private void BuildSparkles()
    {
        RectMask2D mask = GetComponent<RectMask2D>();
        if (mask == null)
            mask = gameObject.AddComponent<RectMask2D>();

        ClearOldSparkles();

        int count = Mathf.Max(1, sparkleCount);
        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject($"AutoSparkle_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(transform, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.zero;

            Image img = obj.GetComponent<Image>();
            img.sprite = GetSparkleSprite();
            img.raycastTarget = false;
            img.maskable = true;
            img.color = new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, 0f);

            sparkles.Add(new SparkleData
            {
                rect = rt,
                image = img
            });
        }
    }

    private void UpdateSparkles(float dt)
    {
        for (int i = 0; i < sparkles.Count; i++)
        {
            SparkleData s = sparkles[i];
            s.timer += dt;

            if (!s.active)
            {
                if (s.timer >= s.delay)
                    ActivateSparkle(s);
                continue;
            }

            float p = Mathf.Clamp01(s.timer / s.life);
            float wave = Mathf.Sin(p * Mathf.PI);

            float alpha = wave * sparkleAlpha;
            float scale = Mathf.Lerp(0.1f, s.maxScale, wave);

            Color c = sparkleColor;
            c.a = alpha;
            s.image.color = c;

            s.rect.localScale = Vector3.one * scale;
            s.rotation += s.rotationSpeed * dt;
            s.rect.localRotation = Quaternion.Euler(0f, 0f, s.rotation);

            if (p >= 1f)
                ResetSparkle(s, false);
        }
    }

    private void ActivateSparkle(SparkleData s)
    {
        s.active = true;
        s.timer = 0f;
        s.life = Random.Range(sparkleLifetimeMinMax.x, sparkleLifetimeMinMax.y);
        s.maxScale = Random.Range(sparkleScaleMinMax.x, sparkleScaleMinMax.y);

        float size = Random.Range(sparkleSizeMinMax.x, sparkleSizeMinMax.y);
        s.rect.sizeDelta = new Vector2(size, size);

        float halfW = Mathf.Max(0f, rectTransform.rect.width * 0.5f - sparkleInset);
        float halfH = Mathf.Max(0f, rectTransform.rect.height * 0.5f - sparkleInset);

        float x = Random.Range(-halfW, halfW);
        float y = Random.Range(-halfH, halfH);
        s.rect.anchoredPosition = new Vector2(x, y);

        s.rotation = randomRotation ? Random.Range(0f, 360f) : 0f;
        s.rotationSpeed = randomRotation ? Random.Range(-sparkleRotationSpeed, sparkleRotationSpeed) : 0f;
        s.rect.localRotation = Quaternion.Euler(0f, 0f, s.rotation);

        Color c = sparkleColor;
        c.a = 0f;
        s.image.color = c;
        s.rect.localScale = Vector3.zero;
        s.rect.SetAsLastSibling();
    }

    private void ResetSparkle(SparkleData s, bool randomStartDelay)
    {
        s.active = false;
        s.timer = 0f;
        s.delay = randomStartDelay
            ? Random.Range(0.15f, sparkleDelayMinMax.y)
            : Random.Range(sparkleDelayMinMax.x, sparkleDelayMinMax.y);

        s.rect.localScale = Vector3.zero;

        Color c = sparkleColor;
        c.a = 0f;
        s.image.color = c;
    }

    private void ClearOldSparkles()
    {
        sparkles.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("AutoSparkle_"))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    private static Sprite GetSparkleSprite()
    {
        if (sparkleSprite != null)
            return sparkleSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radius;
                float dy = (y - center.y) / radius;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float radial = Mathf.Clamp01(1f - dist);

                float vertical = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx) * 2.8f), 3.5f);
                float horizontal = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dy) * 2.8f), 3.5f);
                float diagonalA = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx - dy) * 2.2f), 5f) * 0.45f;
                float diagonalB = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx + dy) * 2.2f), 5f) * 0.45f;
                float core = Mathf.Pow(Mathf.Clamp01(1f - dist * 2.4f), 4.5f);

                float a = Mathf.Clamp01(core + (vertical + horizontal) * 0.32f + diagonalA + diagonalB);
                a *= Mathf.Pow(radial, 0.85f);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();

        sparkleSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return sparkleSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sparkleCount = Mathf.Max(1, sparkleCount);
        sparkleAlpha = Mathf.Clamp01(sparkleAlpha);
        sparkleInset = Mathf.Max(0f, sparkleInset);
        sparkleSizeMinMax.x = Mathf.Max(8f, sparkleSizeMinMax.x);
        sparkleSizeMinMax.y = Mathf.Max(sparkleSizeMinMax.x, sparkleSizeMinMax.y);
        sparkleLifetimeMinMax.x = Mathf.Max(0.1f, sparkleLifetimeMinMax.x);
        sparkleLifetimeMinMax.y = Mathf.Max(sparkleLifetimeMinMax.x, sparkleLifetimeMinMax.y);
        sparkleDelayMinMax.x = Mathf.Max(0f, sparkleDelayMinMax.x);
        sparkleDelayMinMax.y = Mathf.Max(sparkleDelayMinMax.x, sparkleDelayMinMax.y);
        sparkleScaleMinMax.x = Mathf.Max(0.1f, sparkleScaleMinMax.x);
        sparkleScaleMinMax.y = Mathf.Max(sparkleScaleMinMax.x, sparkleScaleMinMax.y);
        breathAmount = Mathf.Max(0f, breathAmount);
        brightnessAmount = Mathf.Max(0f, brightnessAmount);
        sparkleRotationSpeed = Mathf.Max(0f, sparkleRotationSpeed);
    }
#endif
}