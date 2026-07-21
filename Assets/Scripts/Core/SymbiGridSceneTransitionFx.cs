using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public sealed class SymbiGridSceneTransitionFx : MonoBehaviour
    {
        private const int SortingOrder = 32767;
        private const int Columns = 18;
        private const int PaddingCells = 2;
        private const float CellOverlap = 3f;
        private const float CloseSeconds = 0.62f;
        private const float CoveredHoldSeconds = 0.16f;
        private const float OrientationFadeOverscan = 4096f;
        private const float OpenSeconds = 0.58f;
        private const float MaxViewportSettleSeconds = 0.85f;
        private const int StableViewportFrames = 6;
        private const int BlackFramesBeforeOrientation = 6;
        private const string BlockAtlasResourcePath = "SymbiGrid/SymbiGridBlockAtlas";

        private static readonly Color CoverColor = new Color(0.004f, 0.012f, 0.016f, 1f);
        private static readonly Color GapGuardColor = new Color(0.006f, 0.018f, 0.022f, 1f);

        private static SymbiGridSceneTransitionFx active;
        private static bool orientationBlackoutActive;
        private static Sprite[] blockSprites;

        private readonly List<BlockPiece> pieces = new List<BlockPiece>(260);
        private RectTransform root;
        private RectTransform blockRoot;
        private CanvasGroup rootGroup;
        private Image coverImage;
        private Action loadAction;
        private string targetSceneName;
        private Vector2 fieldSize;
        private bool loadStarted;
        private bool localOnly;
        private float coveredHoldSeconds = CoveredHoldSeconds;

        private sealed class BlockPiece
        {
            public RectTransform Rect;
            public Vector2 Start;
            public Vector2 Target;
            public Vector2 Exit;
            public float Delay;
            public float StartRotation;
            public float ExitRotation;
        }

        private sealed class OrientationBlackout : MonoBehaviour
        {
            private RectTransform rect;
            private Image image;

            public void Begin(Action onBlack, Action onComplete)
            {
                rect = transform as RectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-OrientationFadeOverscan, -OrientationFadeOverscan);
                rect.offsetMax = new Vector2(OrientationFadeOverscan, OrientationFadeOverscan);

                Canvas canvas = GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = SortingOrder + 1;

                CanvasScaler scaler = GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;

                image = GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;

                CanvasGroup group = GetComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = false;

                Canvas.ForceUpdateCanvases();
                StartCoroutine(Routine(onBlack, onComplete));
            }

            private void OnDestroy()
            {
                orientationBlackoutActive = false;
            }

            private IEnumerator Routine(Action onBlack, Action onComplete)
            {
                if (image != null)
                    image.color = Color.black;

                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < BlackFramesBeforeOrientation; i++)
                    yield return new WaitForEndOfFrame();

                onBlack?.Invoke();
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return WaitForViewportStability();

                onComplete.Invoke();
                yield return null;
                Canvas.ForceUpdateCanvases();
                Destroy(gameObject);
            }

            private IEnumerator WaitForViewportStability()
            {
                int stableFrames = 0;
                float waited = 0f;
                Vector2 previousSize = ResolveViewportSize();

                while (stableFrames < StableViewportFrames && waited < MaxViewportSettleSeconds)
                {
                    yield return null;
                    waited += Time.unscaledDeltaTime;

                    Vector2 currentSize = ResolveViewportSize();
                    if (Mathf.Abs(currentSize.x - previousSize.x) <= 1f && Mathf.Abs(currentSize.y - previousSize.y) <= 1f)
                        stableFrames++;
                    else
                        stableFrames = 0;

                    previousSize = currentSize;
                }
            }

            private Vector2 ResolveViewportSize()
            {
                Vector2 rectSize = rect != null ? rect.rect.size : Vector2.zero;
                if (rectSize.x > 1f && rectSize.y > 1f)
                    return rectSize;

                return new Vector2(Mathf.Max(Screen.width, 1f), Mathf.Max(Screen.height, 1f));
            }
        }

        public static bool Play(string sceneName, Action onCovered)
        {
            return Play(sceneName, onCovered, CoveredHoldSeconds);
        }

        public static bool Play(string sceneName, Action onCovered, float coveredHoldSeconds)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || onCovered == null)
                return false;

            if (active != null)
                return true;

            GameObject go = new GameObject("SymbiGridSceneTransitionFx", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup), typeof(SymbiGridSceneTransitionFx));
            DontDestroyOnLoad(go);
            active = go.GetComponent<SymbiGridSceneTransitionFx>();
            active.coveredHoldSeconds = Mathf.Max(0f, coveredHoldSeconds);
            active.Begin(sceneName, onCovered);
            return true;
        }

        public static bool PlayLocal(Action onCovered, float coveredHoldSeconds)
        {
            if (onCovered == null)
                return false;

            if (active != null)
                return false;

            GameObject go = new GameObject("SymbiGridSceneTransitionFx", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup), typeof(SymbiGridSceneTransitionFx));
            active = go.GetComponent<SymbiGridSceneTransitionFx>();
            active.localOnly = true;
            active.coveredHoldSeconds = Mathf.Max(0f, coveredHoldSeconds);
            active.Begin(string.Empty, onCovered);
            return true;
        }

        public static bool PlayOrientationFade(Action onBlack, Action onComplete)
        {
            if (orientationBlackoutActive || active != null || onComplete == null)
                return false;

            GameObject go = new GameObject("SymbiGridOrientationBlackout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup), typeof(OrientationBlackout));
            DontDestroyOnLoad(go);
            orientationBlackoutActive = true;
            OrientationBlackout blackout = go.GetComponent<OrientationBlackout>();
            blackout.Begin(onBlack, onComplete);
            return true;
        }

        public static void ForceClearAll()
        {
            active = null;
            orientationBlackoutActive = false;

            SymbiGridSceneTransitionFx[] transitions = FindObjectsByType<SymbiGridSceneTransitionFx>(FindObjectsInactive.Include);
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null)
                    Destroy(transitions[i].gameObject);
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.gameObject == null)
                    continue;

                string name = canvas.gameObject.name;
                if (name.IndexOf("SymbiGridSceneTransitionFx", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("SymbiGridOrientationBlackout", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Destroy(canvas.gameObject);
            }
        }

        private void Begin(string sceneName, Action onCovered)
        {
            targetSceneName = sceneName;
            loadAction = onCovered;

            root = transform as RectTransform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            coverImage = GetComponent<Image>();
            coverImage.color = Color.clear;
            coverImage.raycastTarget = true;

            rootGroup = GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = false;

            Canvas.ForceUpdateCanvases();
            BuildBlocks();
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(PlayRoutine());
        }

        private void OnDestroy()
        {
            if (active == this)
                active = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!loadStarted || !string.Equals(scene.name, targetSceneName, StringComparison.Ordinal))
                return;

            StartCoroutine(OpenAfterSceneLoad());
        }

        private IEnumerator PlayRoutine()
        {
            yield return null;
            yield return AnimateBlocks(true, CloseSeconds);
            SetCoveredState();

            if (coveredHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(coveredHoldSeconds);

            loadStarted = true;
            loadAction?.Invoke();

            if (localOnly)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                RebuildBlocks();
                SetCoveredState();
                yield return AnimateBlocks(false, OpenSeconds);
                Destroy(gameObject);
            }
        }

        private IEnumerator OpenAfterSceneLoad()
        {
            yield return WaitForViewportStability();
            Canvas.ForceUpdateCanvases();
            RebuildBlocks();
            SetCoveredState();
            yield return null;

            yield return AnimateBlocks(false, OpenSeconds);
            Destroy(gameObject);
        }

        private IEnumerator WaitForViewportStability()
        {
            int stableFrames = 0;
            float waited = 0f;
            Vector2 previousSize = ResolveViewportSize();

            while (stableFrames < StableViewportFrames && waited < MaxViewportSettleSeconds)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;

                Vector2 currentSize = ResolveViewportSize();
                if (Mathf.Abs(currentSize.x - previousSize.x) <= 1f && Mathf.Abs(currentSize.y - previousSize.y) <= 1f)
                    stableFrames++;
                else
                    stableFrames = 0;

                previousSize = currentSize;
            }
        }

        private void RebuildBlocks()
        {
            if (blockRoot != null)
                Destroy(blockRoot.gameObject);

            pieces.Clear();
            BuildBlocks();
        }

        private void BuildBlocks()
        {
            Vector2 viewport = ResolveViewportSize();
            float width = Mathf.Max(viewport.x, viewport.y * 16f / 9f, 1f);
            float height = Mathf.Max(viewport.y, viewport.x * 9f / 16f, 1f);
            fieldSize = new Vector2(width, height);

            blockRoot = new GameObject("CubeField", typeof(RectTransform)).GetComponent<RectTransform>();
            blockRoot.SetParent(root, false);
            blockRoot.anchorMin = new Vector2(0.5f, 0.5f);
            blockRoot.anchorMax = new Vector2(0.5f, 0.5f);
            blockRoot.pivot = new Vector2(0.5f, 0.5f);
            blockRoot.anchoredPosition = Vector2.zero;
            blockRoot.sizeDelta = fieldSize;

            float cell = Mathf.Ceil(width / Columns);
            int columns = Mathf.CeilToInt(width / cell) + PaddingCells * 2;
            int rows = Mathf.CeilToInt(height / cell) + PaddingCells * 2;
            System.Random random = new System.Random(Environment.TickCount);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                    CreateBlock(x, y, columns, rows, cell, random);
            }
        }

        private void CreateBlock(int gridX, int gridY, int columns, int rows, float cell, System.Random random)
        {
            GameObject go = new GameObject("SymbiGridTransitionCube", typeof(RectTransform));
            go.transform.SetParent(blockRoot, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cell + CellOverlap, cell + CellOverlap);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(rect, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = GetRandomBlockSprite(random);
            fillImage.type = Image.Type.Simple;
            fillImage.preserveAspect = false;
            fillImage.color = fillImage.sprite != null ? Color.white : new Color(0.02f, 0.22f, 0.26f, 1f);
            fillImage.raycastTarget = false;

            float left = -fieldSize.x * 0.5f - PaddingCells * cell;
            float top = fieldSize.y * 0.5f + PaddingCells * cell;
            Vector2 target = new Vector2(left + (gridX + 0.5f) * cell, top - (gridY + 0.5f) * cell);
            Vector2 direction = DirectionFromEdge(gridX, gridY, columns, rows, random);
            float travel = Mathf.Max(fieldSize.x, fieldSize.y) * (0.64f + (float)random.NextDouble() * 0.32f);

            float centerDistance = Vector2.Distance(target, Vector2.zero) / Mathf.Max(fieldSize.x, fieldSize.y);
            float delay = centerDistance * 0.18f + (float)random.NextDouble() * 0.09f;
            float startRotation = (float)(random.NextDouble() * 90.0 - 45.0);

            rect.anchoredPosition = target + direction * travel;
            rect.localEulerAngles = new Vector3(0f, 0f, startRotation);

            pieces.Add(new BlockPiece
            {
                Rect = rect,
                Start = rect.anchoredPosition,
                Target = target,
                Exit = target - direction * (travel * 1.08f),
                Delay = delay,
                StartRotation = startRotation,
                ExitRotation = -startRotation + (float)(random.NextDouble() * 80.0 - 40.0)
            });
        }

        private IEnumerator AnimateBlocks(bool closing, float seconds)
        {
            coverImage.color = CoverColor;
            float duration = Mathf.Max(0.01f, seconds);
            float maxDelay = 0f;
            for (int i = 0; i < pieces.Count; i++)
                maxDelay = Mathf.Max(maxDelay, pieces[i].Delay);

            float time = 0f;
            while (time < duration + maxDelay)
            {
                time += Time.unscaledDeltaTime;
                float coverAlpha = closing ? 0f : 1f;

                for (int i = 0; i < pieces.Count; i++)
                {
                    BlockPiece piece = pieces[i];
                    if (piece.Rect == null)
                        continue;

                    float t = Ease(Mathf.Clamp01((time - piece.Delay) / duration));
                    if (closing)
                    {
                        piece.Rect.anchoredPosition = Vector2.LerpUnclamped(piece.Start, piece.Target, t);
                        piece.Rect.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(piece.StartRotation, 0f, t));
                        coverAlpha = Mathf.Max(coverAlpha, t * 0.55f);
                    }
                    else
                    {
                        piece.Rect.anchoredPosition = Vector2.LerpUnclamped(piece.Target, piece.Exit, t);
                        piece.Rect.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(0f, piece.ExitRotation, t));
                        coverAlpha = Mathf.Min(coverAlpha, 1f - t * 0.75f);
                    }
                }

                Color cover = CoverColor;
                cover.a = Mathf.Clamp01(coverAlpha);
                coverImage.color = cover;
                yield return null;
            }

            if (closing)
                SetCoveredState();
            else
                coverImage.color = Color.clear;
        }

        private void SetCoveredState()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                BlockPiece piece = pieces[i];
                if (piece.Rect == null)
                    continue;

                piece.Rect.anchoredPosition = piece.Target;
                piece.Rect.localEulerAngles = Vector3.zero;
            }

            coverImage.color = CoverColor;
        }

        private Vector2 ResolveViewportSize()
        {
            Vector2 rectSize = root != null ? root.rect.size : Vector2.zero;
            if (rectSize.x > 1f && rectSize.y > 1f)
                return rectSize;

            return new Vector2(Mathf.Max(Screen.width, 1f), Mathf.Max(Screen.height, 1f));
        }

        private static Vector2 DirectionFromEdge(int x, int y, int columns, int rows, System.Random random)
        {
            float left = x;
            float right = columns - 1 - x;
            float top = y;
            float bottom = rows - 1 - y;
            float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
            float drift = (float)(random.NextDouble() * 0.75 - 0.375);

            if (Mathf.Approximately(min, left))
                return new Vector2(-1f, drift).normalized;
            if (Mathf.Approximately(min, right))
                return new Vector2(1f, drift).normalized;
            if (Mathf.Approximately(min, top))
                return new Vector2(drift, 1f).normalized;

            return new Vector2(drift, -1f).normalized;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Sprite GetRandomBlockSprite(System.Random random)
        {
            Sprite[] sprites = GetBlockSprites();
            if (sprites == null || sprites.Length == 0)
                return null;

            return sprites[random.Next(sprites.Length)];
        }

        private static Sprite[] GetBlockSprites()
        {
            if (blockSprites != null)
                return blockSprites;

            Texture2D atlas = Resources.Load<Texture2D>(BlockAtlasResourcePath);
            if (atlas != null)
            {
                float halfWidth = atlas.width * 0.5f;
                float halfHeight = atlas.height * 0.5f;
                blockSprites = new[]
                {
                    Sprite.Create(atlas, new Rect(0f, halfHeight, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(halfWidth, halfHeight, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(0f, 0f, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(halfWidth, 0f, halfWidth, halfHeight), new Vector2(0.5f, 0.5f), 100f)
                };
                return blockSprites;
            }

            blockSprites = new[]
            {
                CreateBlockSprite(new Color(0.10f, 0.88f, 0.92f, 1f), new Color(0.78f, 1f, 1f, 1f), new Color(0.02f, 0.32f, 0.38f, 1f)),
                CreateBlockSprite(new Color(0.22f, 0.92f, 0.24f, 1f), new Color(0.83f, 1f, 0.66f, 1f), new Color(0.05f, 0.36f, 0.06f, 1f)),
                CreateBlockSprite(new Color(1f, 0.67f, 0.08f, 1f), new Color(1f, 0.92f, 0.50f, 1f), new Color(0.46f, 0.18f, 0.02f)),
                CreateBlockSprite(new Color(1f, 0.22f, 0.30f, 1f), new Color(1f, 0.76f, 0.80f, 1f), new Color(0.42f, 0.04f, 0.09f, 1f))
            };
            return blockSprites;
        }

        private static Sprite CreateBlockSprite(Color fill, Color highlight, Color shadow)
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, size, size, 10f, 2f);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float vertical = y / (size - 1f);
                    float radial = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / center.x);
                    float edge = 1f - RoundedRectCoverage(x + 0.5f, y + 0.5f, size, size, 7f, 7f);
                    Color body = Color.Lerp(shadow, fill, 0.66f + vertical * 0.16f);
                    body = Color.Lerp(body, highlight, Mathf.Clamp01(radial * 0.32f + edge * 0.16f));
                    body.a = alpha;
                    texture.SetPixel(x, y, body);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static float RoundedRectCoverage(float x, float y, float width, float height, float radius, float inset)
        {
            float left = inset;
            float right = width - inset;
            float bottom = inset;
            float top = height - inset;
            float cx = Mathf.Clamp(x, left + radius, right - radius);
            float cy = Mathf.Clamp(y, bottom + radius, top - radius);
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius + 0.5f - dist);
        }
    }
}
