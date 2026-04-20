using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ProceduralFogUI : MonoBehaviour
    {
        private enum SpawnSide
        {
            Left,
            Right
        }

        [Header("Создание")]
        [SerializeField, Min(1)] private int fogCount = 4;
        [SerializeField, Min(4)] private int puffCountPerFog = 10;
        [SerializeField] private SpawnSide spawnFrom = SpawnSide.Right;
        [SerializeField] private bool buildOnAwake = true;

        [Header("Движение")]
        [SerializeField] private float moveSpeed = 36f;
        [SerializeField] private Vector2 randomSpeedRange = new Vector2(-6f, 8f);
        [SerializeField] private float spawnPadding = 500f;
        [SerializeField] private float extraSpawnOffset = 350f;

        [Header("Размер потока")]
        [SerializeField] private Vector2 baseFogSize = new Vector2(1200f, 190f);
        [SerializeField] private Vector2 randomFogWidthRange = new Vector2(-180f, 220f);
        [SerializeField] private Vector2 randomFogHeightRange = new Vector2(-35f, 55f);

        [Header("Положение по высоте")]
        [SerializeField] private float minY = -170f;
        [SerializeField] private float maxY = 170f;

        [Header("Прозрачность")]
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.04f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.12f;

        [Header("Плавное появление / исчезновение")]
        [SerializeField] private bool enableFadeIn = true;
        [SerializeField] private float fadeInDuration = 1.8f;
        [SerializeField] private bool enableFadeOut = true;
        [SerializeField, Range(0.01f, 0.45f)] private float fadeOutScreenPercent = 0.14f;

        [Header("Форма облаков")]
        [SerializeField] private int generatedTextureSize = 128;
        [SerializeField, Range(0f, 1f)] private float softness = 0.9f;
        [SerializeField] private float edgeFadePower = 2.2f;
        [SerializeField] private float noiseStrength = 0.55f;

        [Header("Растяжение по движению")]
        [SerializeField] private bool enableStretchX = true;
        [SerializeField] private float stretchBySpeed = 0.8f;
        [SerializeField] private float maxStretchX = 240f;

        [Header("Живость формы")]
        [SerializeField] private bool enableWave = true;
        [SerializeField] private Vector2 waveSpeedRange = new Vector2(0.45f, 0.95f);
        [SerializeField] private Vector2 waveAmplitudeRange = new Vector2(6f, 16f);

        [Header("Дыхание")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private Vector2 pulseSpeedRange = new Vector2(0.18f, 0.35f);
        [SerializeField] private Vector2 pulseAmountRange = new Vector2(0.02f, 0.05f);

        [Header("Облака внутри потока")]
        [SerializeField] private Vector2 puffWidthRange = new Vector2(140f, 260f);
        [SerializeField] private Vector2 puffHeightRange = new Vector2(60f, 130f);
        [SerializeField] private float puffVerticalSpread = 22f;
        [SerializeField] private float puffHorizontalJitter = 35f;
        [SerializeField] private float puffVerticalJitter = 16f;
        [SerializeField] private bool randomFlipY = true;

        [Header("Случайность")]
        [SerializeField] private int randomSeed = 0;

        private readonly List<FogGroup> groups = new();
        private readonly List<Texture2D> generatedTextures = new();
        private RectTransform hostRect;

        private sealed class FogGroup
        {
            public RectTransform Root;
            public float Speed;
            public float BaseWidth;
            public float BaseHeight;
            public float BaseY;

            public float TargetAlpha;
            public float CurrentAlpha;
            public float SpawnTime;

            public float PulseSpeed;
            public float PulseAmount;
            public float PulseOffset;

            public float WaveSpeed;
            public float WaveAmplitude;
            public float WaveOffset;

            public readonly List<Puff> Puffs = new();
        }

        private sealed class Puff
        {
            public RectTransform Rect;
            public RawImage Image;
            public float BaseX;
            public float BaseY;
            public float Width;
            public float Height;
            public float WavePhase;
            public float WaveStrength;
            public float PulsePhase;
            public float PulseStrength;
            public float AlphaMultiplier;
        }

        private void Awake()
        {
            hostRect = GetComponent<RectTransform>();

            if (buildOnAwake)
                Rebuild();
        }

        private void OnDestroy()
        {
            ReleaseGeneratedTextures();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (randomSpeedRange.x > randomSpeedRange.y)
                randomSpeedRange = new Vector2(randomSpeedRange.y, randomSpeedRange.x);

            if (randomFogWidthRange.x > randomFogWidthRange.y)
                randomFogWidthRange = new Vector2(randomFogWidthRange.y, randomFogWidthRange.x);

            if (randomFogHeightRange.x > randomFogHeightRange.y)
                randomFogHeightRange = new Vector2(randomFogHeightRange.y, randomFogHeightRange.x);

            if (waveSpeedRange.x > waveSpeedRange.y)
                waveSpeedRange = new Vector2(waveSpeedRange.y, waveSpeedRange.x);

            if (waveAmplitudeRange.x > waveAmplitudeRange.y)
                waveAmplitudeRange = new Vector2(waveAmplitudeRange.y, waveAmplitudeRange.x);

            if (pulseSpeedRange.x > pulseSpeedRange.y)
                pulseSpeedRange = new Vector2(pulseSpeedRange.y, pulseSpeedRange.x);

            if (pulseAmountRange.x > pulseAmountRange.y)
                pulseAmountRange = new Vector2(pulseAmountRange.y, pulseAmountRange.x);

            if (puffWidthRange.x > puffWidthRange.y)
                puffWidthRange = new Vector2(puffWidthRange.y, puffWidthRange.x);

            if (puffHeightRange.x > puffHeightRange.y)
                puffHeightRange = new Vector2(puffHeightRange.y, puffHeightRange.x);

            if (minY > maxY)
            {
                float t = minY;
                minY = maxY;
                maxY = t;
            }

            if (minAlpha > maxAlpha)
            {
                float t = minAlpha;
                minAlpha = maxAlpha;
                maxAlpha = t;
            }

            fogCount = Mathf.Max(1, fogCount);
            puffCountPerFog = Mathf.Max(4, puffCountPerFog);
            generatedTextureSize = Mathf.Max(64, generatedTextureSize);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            spawnPadding = Mathf.Max(0f, spawnPadding);
            extraSpawnOffset = Mathf.Max(0f, extraSpawnOffset);
            baseFogSize.x = Mathf.Max(64f, baseFogSize.x);
            baseFogSize.y = Mathf.Max(32f, baseFogSize.y);
            edgeFadePower = Mathf.Max(0.1f, edgeFadePower);
            noiseStrength = Mathf.Clamp01(noiseStrength);
            stretchBySpeed = Mathf.Max(0f, stretchBySpeed);
            maxStretchX = Mathf.Max(0f, maxStretchX);
            puffVerticalSpread = Mathf.Max(0f, puffVerticalSpread);
            puffHorizontalJitter = Mathf.Max(0f, puffHorizontalJitter);
            puffVerticalJitter = Mathf.Max(0f, puffVerticalJitter);
            fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
        }
#endif

        private void Update()
        {
            if (groups.Count == 0)
                return;

            float dt = Time.deltaTime;
            float time = Time.unscaledTime;
            float halfHostW = hostRect.rect.width * 0.5f;
            float dir = spawnFrom == SpawnSide.Right ? -1f : 1f;

            for (int i = 0; i < groups.Count; i++)
            {
                FogGroup group = groups[i];

                Vector2 rootPos = group.Root.anchoredPosition;
                rootPos.x += dir * group.Speed * dt;
                group.Root.anchoredPosition = rootPos;

                float widthPulse = 1f;
                if (enablePulse)
                    widthPulse += Mathf.Sin((time + group.PulseOffset) * group.PulseSpeed) * group.PulseAmount;

                float totalWidth = group.BaseWidth * widthPulse;
                float totalHeight = group.BaseHeight * widthPulse;

                if (enableStretchX)
                    totalWidth += Mathf.Min(group.Speed * stretchBySpeed, maxStretchX);

                float alphaFactor = 1f;

                if (enableFadeIn)
                {
                    float age = time - group.SpawnTime;
                    alphaFactor *= Mathf.Clamp01(age / fadeInDuration);
                }

                if (enableFadeOut)
                {
                    float fadeZone = hostRect.rect.width * fadeOutScreenPercent;
                    fadeZone = Mathf.Max(1f, fadeZone);

                    if (spawnFrom == SpawnSide.Right)
                    {
                        float leftEdge = rootPos.x + totalWidth * 0.5f;
                        float startFadeX = -halfHostW + fadeZone;
                        if (leftEdge < startFadeX)
                            alphaFactor *= Mathf.Clamp01((leftEdge + halfHostW) / fadeZone);
                    }
                    else
                    {
                        float rightEdge = rootPos.x - totalWidth * 0.5f;
                        float startFadeX = halfHostW - fadeZone;
                        if (rightEdge > startFadeX)
                            alphaFactor *= Mathf.Clamp01((halfHostW - rightEdge) / fadeZone);
                    }
                }

                group.CurrentAlpha = group.TargetAlpha * alphaFactor;

                UpdatePuffs(group, totalWidth, totalHeight, time);

                float leftBound = -halfHostW - totalWidth - spawnPadding;
                float rightBound = halfHostW + totalWidth + spawnPadding;

                if ((spawnFrom == SpawnSide.Right && rootPos.x < leftBound) ||
                    (spawnFrom == SpawnSide.Left && rootPos.x > rightBound))
                {
                    Respawn(group);
                }
            }
        }

        [ContextMenu("Rebuild Fog")]
        public void Rebuild()
        {
            ClearChildren();
            ReleaseGeneratedTextures();
            groups.Clear();

            if (randomSeed != 0)
                Random.InitState(randomSeed);

            for (int i = 0; i < fogCount; i++)
                CreateFogGroup(i);
        }

        [ContextMenu("Clear Fog")]
        public void ClearFog()
        {
            ClearChildren();
            ReleaseGeneratedTextures();
            groups.Clear();
        }

        private void CreateFogGroup(int index)
        {
            Texture2D tex = GeneratePuffTexture(index);
            generatedTextures.Add(tex);

            GameObject rootObj = new GameObject($"FogGroup_{index + 1}", typeof(RectTransform));
            rootObj.transform.SetParent(transform, false);

            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            FogGroup group = new FogGroup
            {
                Root = rootRect
            };

            for (int i = 0; i < puffCountPerFog; i++)
            {
                GameObject puffObj = new GameObject($"Puff_{i + 1}", typeof(RectTransform), typeof(RawImage));
                puffObj.transform.SetParent(rootRect, false);

                RectTransform puffRect = puffObj.GetComponent<RectTransform>();
                RawImage puffImage = puffObj.GetComponent<RawImage>();

                puffRect.anchorMin = new Vector2(0.5f, 0.5f);
                puffRect.anchorMax = new Vector2(0.5f, 0.5f);
                puffRect.pivot = new Vector2(0.5f, 0.5f);

                puffImage.texture = tex;
                puffImage.raycastTarget = false;

                group.Puffs.Add(new Puff
                {
                    Rect = puffRect,
                    Image = puffImage
                });
            }

            RandomizeGroup(group, true);
            groups.Add(group);
        }

        private void RandomizeGroup(FogGroup group, bool firstSpawn)
        {
            group.Speed = Mathf.Max(1f, moveSpeed + Random.Range(randomSpeedRange.x, randomSpeedRange.y));
            group.BaseWidth = Mathf.Max(100f, baseFogSize.x + Random.Range(randomFogWidthRange.x, randomFogWidthRange.y));
            group.BaseHeight = Mathf.Max(40f, baseFogSize.y + Random.Range(randomFogHeightRange.x, randomFogHeightRange.y));
            group.BaseY = Random.Range(minY, maxY);

            group.TargetAlpha = Random.Range(minAlpha, maxAlpha);
            group.CurrentAlpha = 0f;
            group.SpawnTime = Time.unscaledTime;

            group.PulseSpeed = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
            group.PulseAmount = Random.Range(pulseAmountRange.x, pulseAmountRange.y);
            group.PulseOffset = Random.Range(0f, 20f);

            group.WaveSpeed = Random.Range(waveSpeedRange.x, waveSpeedRange.y);
            group.WaveAmplitude = Random.Range(waveAmplitudeRange.x, waveAmplitudeRange.y);
            group.WaveOffset = Random.Range(0f, 20f);

            if (randomFlipY)
                group.Root.localScale = new Vector3(1f, Random.value > 0.5f ? -1f : 1f, 1f);
            else
                group.Root.localScale = Vector3.one;

            LayoutPuffs(group);

            float halfHostW = hostRect.rect.width * 0.5f;
            float outsideDistance = group.BaseWidth * 0.5f + spawnPadding + Random.Range(extraSpawnOffset * 0.4f, extraSpawnOffset);

            Vector2 pos = group.Root.anchoredPosition;
            pos.y = group.BaseY;

            if (spawnFrom == SpawnSide.Right)
            {
                pos.x = halfHostW + outsideDistance;
                if (firstSpawn)
                    pos.x += Random.Range(0f, extraSpawnOffset);
            }
            else
            {
                pos.x = -halfHostW - outsideDistance;
                if (firstSpawn)
                    pos.x -= Random.Range(0f, extraSpawnOffset);
            }

            group.Root.anchoredPosition = pos;
            UpdatePuffs(group, group.BaseWidth, group.BaseHeight, Time.unscaledTime);
        }

        private void Respawn(FogGroup group)
        {
            RandomizeGroup(group, false);
        }

        private void LayoutPuffs(FogGroup group)
        {
            int count = group.Puffs.Count;
            if (count == 0)
                return;

            float step = count > 1 ? group.BaseWidth / (count - 1) : group.BaseWidth;
            float startX = -group.BaseWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Puff puff = group.Puffs[i];

                float width = Random.Range(puffWidthRange.x, puffWidthRange.y);
                float height = Random.Range(puffHeightRange.x, puffHeightRange.y);

                float baseX = startX + i * step;
                baseX += Random.Range(-puffHorizontalJitter, puffHorizontalJitter);

                float normalized = count == 1 ? 0.5f : (float)i / (count - 1);
                float arch = Mathf.Sin(normalized * Mathf.PI) * puffVerticalSpread;
                float baseY = arch + Random.Range(-puffVerticalJitter, puffVerticalJitter);

                puff.Width = width;
                puff.Height = height;
                puff.BaseX = baseX;
                puff.BaseY = baseY;
                puff.WavePhase = Random.Range(0f, 10f);
                puff.WaveStrength = Random.Range(0.7f, 1.25f);
                puff.PulsePhase = Random.Range(0f, 10f);
                puff.PulseStrength = Random.Range(0.85f, 1.15f);
                puff.AlphaMultiplier = Random.Range(0.75f, 1f);
            }
        }

        private void UpdatePuffs(FogGroup group, float totalWidth, float totalHeight, float time)
        {
            int count = group.Puffs.Count;
            if (count == 0)
                return;

            float widthScale = group.BaseWidth > 0.001f ? totalWidth / group.BaseWidth : 1f;
            float heightScale = group.BaseHeight > 0.001f ? totalHeight / group.BaseHeight : 1f;

            for (int i = 0; i < count; i++)
            {
                Puff puff = group.Puffs[i];

                float x = puff.BaseX * widthScale;
                float y = puff.BaseY * heightScale;

                if (enableWave)
                    y += Mathf.Sin((time + group.WaveOffset + puff.WavePhase) * group.WaveSpeed) * group.WaveAmplitude * puff.WaveStrength;

                float pulse = 1f;
                if (enablePulse)
                    pulse += Mathf.Sin((time + puff.PulsePhase) * group.PulseSpeed * 0.9f) * (group.PulseAmount * 0.65f) * puff.PulseStrength;

                float width = puff.Width * widthScale * pulse;
                float height = puff.Height * heightScale * pulse;

                puff.Rect.sizeDelta = new Vector2(width, height);
                puff.Rect.anchoredPosition = new Vector2(x, y);

                Color c = puff.Image.color;
                c.r = 1f;
                c.g = 1f;
                c.b = 1f;
                c.a = group.CurrentAlpha * puff.AlphaMultiplier;
                puff.Image.color = c;
            }
        }

        private Texture2D GeneratePuffTexture(int index)
        {
            Texture2D tex = new Texture2D(generatedTextureSize, generatedTextureSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = $"GeneratedFogPuff_{index + 1}";

            float seedA = Random.Range(0f, 1000f);
            float seedB = Random.Range(0f, 1000f);

            for (int y = 0; y < generatedTextureSize; y++)
            {
                for (int x = 0; x < generatedTextureSize; x++)
                {
                    float u = (float)x / (generatedTextureSize - 1);
                    float v = (float)y / (generatedTextureSize - 1);

                    float dx = u - 0.5f;
                    float dy = v - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                    float radial = Mathf.Clamp01(1f - dist);
                    radial = Mathf.Pow(radial, edgeFadePower);

                    float n1 = Mathf.PerlinNoise(seedA + u * 3.2f, seedB + v * 3.1f);
                    float n2 = Mathf.PerlinNoise(seedB + u * 6.4f, seedA + v * 6.0f);
                    float noise = Mathf.Lerp(1f, (n1 * 0.65f + n2 * 0.35f), noiseStrength);

                    float density = radial * noise;
                    density = Mathf.Pow(Mathf.Clamp01(density), Mathf.Lerp(1.8f, 1.1f, softness));

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, density));
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        private void ReleaseGeneratedTextures()
        {
            for (int i = 0; i < generatedTextures.Count; i++)
            {
                if (generatedTextures[i] == null)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(generatedTextures[i]);
                else
                    Destroy(generatedTextures[i]);
#else
                Destroy(generatedTextures[i]);
#endif
            }

            generatedTextures.Clear();
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }
}