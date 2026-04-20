using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BattleTrayUI : MonoBehaviour
    {
        public event Action Changed;
        public event Action LoseTriggered;

        [Header("Roots")]
        [SerializeField] private RectTransform slotsRoot;
        [SerializeField] private RectTransform tilesRoot;

        [Header("Setup")]
        [SerializeField] private int maxSlots = 4;
        [SerializeField] private Vector2 tileSize = new Vector2(110f, 150f);
        [SerializeField] private float sidePadding = 20f;
        [SerializeField] private float spacing = 12f;
        [SerializeField] private bool useTrayHeightAsTileHeight = true;

        [Header("Slot Background")]
        [SerializeField] private Sprite slotBackgroundSprite;
        [SerializeField] private Color slotBackgroundColor = Color.white;
        [SerializeField] private Vector2 slotBackgroundExtraSize = new Vector2(14f, 14f);

        private readonly List<BattleTile> tiles = new();
        private readonly List<Vector2> slotPositions = new();
        private readonly List<Image> slotBackgrounds = new();
        private readonly HashSet<BattleTile> landedTiles = new();
        private readonly HashSet<BattleTile> breakingTiles = new();

        private RectTransform rect;
        private TrayFX trayFX;

        private int flyingCounter;
        private Coroutine matchQueueRoutine;
        private bool loseTriggered;

        public bool IsBusy => flyingCounter > 0;
        public int Count => tiles.Count;
        public bool IsFull => tiles.Count >= maxSlots;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();

            if (slotsRoot == null)
                slotsRoot = transform as RectTransform;

            if (tilesRoot == null)
                tilesRoot = transform as RectTransform;

            trayFX = GetComponent<TrayFX>();
            if (trayFX == null)
                trayFX = GetComponentInChildren<TrayFX>(true);

            RebuildSlots();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxSlots = Mathf.Max(1, maxSlots);
            tileSize.x = Mathf.Max(10f, tileSize.x);
            tileSize.y = Mathf.Max(10f, tileSize.y);
            sidePadding = Mathf.Max(0f, sidePadding);
            spacing = Mathf.Max(0f, spacing);
            slotBackgroundExtraSize.x = Mathf.Max(0f, slotBackgroundExtraSize.x);
            slotBackgroundExtraSize.y = Mathf.Max(0f, slotBackgroundExtraSize.y);
        }
#endif

        [ContextMenu("Rebuild Slots")]
        public void RebuildSlots()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            slotPositions.Clear();

            float trayWidth = rect.rect.width;
            float trayHeight = rect.rect.height;

            float tileWidth = tileSize.x;
            float tileHeight = useTrayHeightAsTileHeight
                ? Mathf.Max(10f, trayHeight - 10f)
                : tileSize.y;

            tileSize = new Vector2(tileWidth, tileHeight);

            float availableWidth = trayWidth - sidePadding * 2f;
            float totalTilesWidth = maxSlots * tileWidth + (maxSlots - 1) * spacing;

            if (totalTilesWidth > availableWidth && maxSlots > 0)
            {
                float correctedSpacing = (availableWidth - maxSlots * tileWidth) / Mathf.Max(1, maxSlots - 1);
                spacing = Mathf.Max(0f, correctedSpacing);
                totalTilesWidth = maxSlots * tileWidth + (maxSlots - 1) * spacing;
            }

            float startX = -totalTilesWidth * 0.5f + tileWidth * 0.5f;

            for (int i = 0; i < maxSlots; i++)
                slotPositions.Add(new Vector2(startX + i * (tileWidth + spacing), 0f));

            RefreshSlotBackgrounds();
            RefreshPositionsImmediate();
        }

        public bool TryAdd(BattleTile tile)
        {
            if (tile == null)
                return false;
            if (tiles.Count >= maxSlots)
                return false;
            if (tiles.Contains(tile))
                return false;
            if (breakingTiles.Contains(tile))
                return false;
            if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
                return false;
            if (loseTriggered)
                return false;

            if (slotPositions.Count != maxSlots)
                RebuildSlots();

            RectTransform tileRect = tile.Rect;
            if (tileRect == null)
                return false;

            Vector3 worldPos = tileRect.position;

            tiles.Add(tile);
            landedTiles.Remove(tile);

            tile.transform.SetParent(tilesRoot, true);
            tile.transform.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out Vector2 localPoint
            );

            tileRect.anchoredPosition = localPoint;

            int slotIndex = tiles.Count - 1;
            StartCoroutine(AnimateIntoTray(tile, slotIndex));

            Changed?.Invoke();
            return true;
        }

        private IEnumerator AnimateIntoTray(BattleTile tile, int slotIndex)
        {
            flyingCounter++;

            RectTransform tileRect = tile.Rect;
            if (tileRect == null)
            {
                flyingCounter = Mathf.Max(0, flyingCounter - 1);
                yield break;
            }

            Vector2 startPos = tileRect.anchoredPosition;
            Vector2 targetPos = slotPositions[Mathf.Clamp(slotIndex, 0, slotPositions.Count - 1)];

            if (trayFX != null)
                yield return StartCoroutine(trayFX.PlayFly(tileRect, startPos, targetPos, tileSize));
            else
            {
                tileRect.sizeDelta = tileSize;
                tileRect.anchoredPosition = targetPos;
                tileRect.localScale = Vector3.one;
                tileRect.localRotation = Quaternion.identity;
            }

            if (tile != null && tileRect != null)
            {
                tileRect.sizeDelta = tileSize;
                tileRect.anchoredPosition = targetPos;
                tileRect.localScale = Vector3.one;
                tileRect.localRotation = Quaternion.identity;
                tile.transform.SetAsLastSibling();
                landedTiles.Add(tile);
            }

            flyingCounter = Mathf.Max(0, flyingCounter - 1);

            RequestMatchQueue();
            Changed?.Invoke();
        }

        private void RequestMatchQueue()
        {
            if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
                return;

            if (matchQueueRoutine != null)
                return;

            matchQueueRoutine = StartCoroutine(ProcessMatchQueue());
        }

        private IEnumerator ProcessMatchQueue()
        {
            while (true)
            {
                if (flyingCounter > 0)
                {
                    yield return null;
                    continue;
                }

                if (!TryFindLandedMatch(out BattleTile a, out BattleTile b))
                    break;

                if (a == null || b == null)
                {
                    yield return null;
                    continue;
                }

                if (!tiles.Contains(a) || !tiles.Contains(b))
                {
                    landedTiles.Remove(a);
                    landedTiles.Remove(b);
                    yield return null;
                    continue;
                }

                tiles.Remove(a);
                tiles.Remove(b);
                landedTiles.Remove(a);
                landedTiles.Remove(b);

                breakingTiles.Add(a);
                breakingTiles.Add(b);

                RefreshPositionsImmediate();
                Changed?.Invoke();

                if (gameObject.activeInHierarchy && isActiveAndEnabled)
                    StartCoroutine(PlayMatchBreakAndDestroy(a, b));
                else
                {
                    breakingTiles.Remove(a);
                    breakingTiles.Remove(b);
                    DestroySafe(a != null ? a.gameObject : null);
                    DestroySafe(b != null ? b.gameObject : null);
                }

                yield return null;
            }

            EvaluateLoseCondition();
            matchQueueRoutine = null;
        }

        private void EvaluateLoseCondition()
        {
            if (loseTriggered)
                return;

            if (flyingCounter > 0)
                return;

            if (tiles.Count < maxSlots)
                return;

            if (HasAnyAvailablePair())
                return;

            loseTriggered = true;
            LoseTriggered?.Invoke();
        }

        private bool HasAnyAvailablePair()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile a = tiles[i];
                if (a == null || !landedTiles.Contains(a) || breakingTiles.Contains(a))
                    continue;

                for (int j = i + 1; j < tiles.Count; j++)
                {
                    BattleTile b = tiles[j];
                    if (b == null || !landedTiles.Contains(b) || breakingTiles.Contains(b))
                        continue;

                    if (a.Id == b.Id)
                        return true;
                }
            }

            return false;
        }

        private bool TryFindLandedMatch(out BattleTile first, out BattleTile second)
        {
            first = null;
            second = null;

            if (tiles.Count < 2)
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile a = tiles[i];
                if (a == null || !landedTiles.Contains(a) || breakingTiles.Contains(a))
                    continue;

                for (int j = i + 1; j < tiles.Count; j++)
                {
                    BattleTile b = tiles[j];
                    if (b == null || !landedTiles.Contains(b) || breakingTiles.Contains(b))
                        continue;

                    if (a.Id != b.Id)
                        continue;

                    first = a;
                    second = b;
                    return true;
                }
            }

            return false;
        }

        private IEnumerator PlayMatchBreakAndDestroy(BattleTile a, BattleTile b)
        {
            RectTransform ra = a != null ? a.Rect : null;
            RectTransform rb = b != null ? b.Rect : null;

            if (trayFX != null && ra != null && rb != null)
                yield return StartCoroutine(trayFX.PlayMatchBreak(ra, rb));
            else
                yield return null;

            breakingTiles.Remove(a);
            breakingTiles.Remove(b);

            DestroySafe(a != null ? a.gameObject : null);
            DestroySafe(b != null ? b.gameObject : null);

            RequestMatchQueue();
        }

        private void RefreshSlotBackgrounds()
        {
            if (slotBackgroundSprite == null || slotsRoot == null)
            {
                ClearSlotBackgrounds();
                return;
            }

            while (slotBackgrounds.Count < maxSlots)
            {
                GameObject go = new GameObject(
                    $"BattleTraySlotBg_{slotBackgrounds.Count}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                go.transform.SetParent(slotsRoot, false);

                Image img = go.GetComponent<Image>();
                img.sprite = slotBackgroundSprite;
                img.color = slotBackgroundColor;
                img.raycastTarget = false;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;

                slotBackgrounds.Add(img);
            }

            while (slotBackgrounds.Count > maxSlots)
            {
                int last = slotBackgrounds.Count - 1;
                if (slotBackgrounds[last] != null)
                    DestroySafe(slotBackgrounds[last].gameObject);
                slotBackgrounds.RemoveAt(last);
            }

            Vector2 bgSize = tileSize + slotBackgroundExtraSize;

            for (int i = 0; i < slotBackgrounds.Count; i++)
            {
                Image img = slotBackgrounds[i];
                if (img == null)
                    continue;

                RectTransform rt = img.rectTransform;
                rt.SetParent(slotsRoot, false);
                rt.anchoredPosition = i < slotPositions.Count ? slotPositions[i] : Vector2.zero;
                rt.sizeDelta = bgSize;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                img.sprite = slotBackgroundSprite;
                img.color = slotBackgroundColor;
                img.transform.SetAsLastSibling();
            }
        }

        private void ClearSlotBackgrounds()
        {
            for (int i = slotBackgrounds.Count - 1; i >= 0; i--)
            {
                if (slotBackgrounds[i] != null)
                    DestroySafe(slotBackgrounds[i].gameObject);
            }

            slotBackgrounds.Clear();
        }

        private void RefreshPositionsImmediate()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (tile == null)
                    continue;

                RectTransform tileRect = tile.Rect;
                if (tileRect == null)
                    continue;

                tileRect.SetParent(tilesRoot != null ? tilesRoot : transform, false);
                tileRect.sizeDelta = tileSize;
                tileRect.anchoredPosition = slotPositions[i];
                tileRect.localScale = Vector3.one;
                tileRect.localRotation = Quaternion.identity;

                CanvasGroup cg = tile.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.alpha = 1f;

                tile.transform.SetAsLastSibling();
            }
        }

        public void ClearImmediate()
        {
            StopAllCoroutines();
            flyingCounter = 0;
            matchQueueRoutine = null;
            loseTriggered = false;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null)
                    continue;

                DestroySafe(tiles[i].gameObject);
            }

            tiles.Clear();
            landedTiles.Clear();
            breakingTiles.Clear();
            ClearSlotBackgrounds();
        }

        private void DestroySafe(GameObject go)
        {
            if (go == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }
    }
}