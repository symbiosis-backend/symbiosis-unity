using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class Tray : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private Camera cam;

        [Header("Auto Slots")]
        [SerializeField] private int slotCount = 4;
        [SerializeField] private Vector2 slotSize = new Vector2(1.15f, 1.55f);
        [SerializeField] private float gap = 0.12f;
        [SerializeField] private float topMargin = 0.9f;
        [SerializeField] private bool rebuildOnStart = true;

        [Header("Slot Visual")]
        [SerializeField] private bool showSlotVisuals = true;
        [SerializeField] private Color slotColor = new Color(0.28f, 0.12f, 0.04f, 0.92f);
        [SerializeField] private Color slotBorderColor = new Color(0.72f, 0.38f, 0.12f, 1f);
        [SerializeField] private Vector2 borderExtra = new Vector2(0.12f, 0.12f);
        [SerializeField] private int borderOrder = -11;
        [SerializeField] private int fillOrder = -10;

        [Header("Anim")]
        [SerializeField] private float moveTime = 0.18f;
        [SerializeField] private float vanishTime = 0.12f;

        private readonly List<Transform> slots = new();
        private readonly List<Tile> tiles = new();
        private readonly List<GameObject> visuals = new();

        private bool busy;
        private Sprite whiteSprite;

        public int Capacity => Mathf.Max(1, slotCount);
        public int Count => tiles.Count;
        public bool IsBusy => busy;
        public bool IsFull => Count >= Capacity;

        private void Awake()
        {
            if (cam == null)
                cam = Camera.main;

            EnsureWhiteSprite();
        }

        private void Start()
        {
            if (rebuildOnStart)
                RebuildSlots();
        }

        private void OnValidate()
        {
            slotCount = Mathf.Max(1, slotCount);
            slotSize.x = Mathf.Max(0.2f, slotSize.x);
            slotSize.y = Mathf.Max(0.2f, slotSize.y);
            gap = Mathf.Max(0f, gap);
        }

        [ContextMenu("Rebuild Slots")]
        public void RebuildSlots()
        {
            if (cam == null)
                cam = Camera.main;

            EnsureWhiteSprite();
            ClearSlotObjects();

            float totalWidth = slotCount * slotSize.x + (slotCount - 1) * gap;
            float startX = -totalWidth * 0.5f + slotSize.x * 0.5f;

            float camTop = cam != null ? cam.transform.position.y + cam.orthographicSize : 5f;
            float y = camTop - topMargin - slotSize.y * 0.5f;

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotRoot = new GameObject($"Slot_{i}");
                slotRoot.transform.SetParent(transform, false);
                slotRoot.transform.position = new Vector3(startX + i * (slotSize.x + gap), y, 0f);
                slotRoot.transform.localScale = Vector3.one;

                slots.Add(slotRoot.transform);

                if (showSlotVisuals)
                    CreateSlotVisual(slotRoot.transform, i);
            }

            StopAllCoroutines();
            CompactImmediate();
        }

        public bool TryAdd(Tile tile)
        {
            if (tile == null || busy || IsFull || slots.Count == 0)
                return false;

            if (tiles.Contains(tile))
                return false;

            tiles.Add(tile);

            BoxCollider2D box = tile.GetComponent<BoxCollider2D>();
            if (box != null)
                box.enabled = false;

            StartCoroutine(AddRoutine(tile));
            return true;
        }

        public void ClearImmediate()
        {
            StopAllCoroutines();
            busy = false;
            tiles.Clear();
        }

        private IEnumerator AddRoutine(Tile tile)
        {
            busy = true;

            int slotIndex = tiles.IndexOf(tile);
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                busy = false;
                yield break;
            }

            Transform target = slots[slotIndex];
            Vector3 start = tile.transform.position;
            Vector3 end = target.position;

            float t = 0f;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / moveTime);
                tile.transform.position = Vector3.Lerp(start, end, k);
                yield return null;
            }

            tile.transform.position = end;

            yield return ResolvePairs();

            busy = false;
        }

        private IEnumerator ResolvePairs()
        {
            bool removedAny = true;

            while (removedAny)
            {
                removedAny = false;

                for (int i = 0; i < tiles.Count; i++)
                {
                    Tile a = tiles[i];
                    if (a == null || !a.gameObject.activeSelf)
                        continue;

                    for (int j = i + 1; j < tiles.Count; j++)
                    {
                        Tile b = tiles[j];
                        if (b == null || !b.gameObject.activeSelf)
                            continue;

                        if (a.Id != b.Id)
                            continue;

                        yield return RemovePair(a, b);
                        removedAny = true;
                        goto NEXT_PASS;
                    }
                }

            NEXT_PASS:
                yield return null;
            }

            yield return CompactRoutine();
        }

        private IEnumerator RemovePair(Tile a, Tile b)
        {
            Vector3 startA = a.transform.position;
            Vector3 startB = b.transform.position;
            Vector3 mid = (startA + startB) * 0.5f;

            float t = 0f;
            while (t < vanishTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / vanishTime);

                if (a != null)
                {
                    a.transform.position = Vector3.Lerp(startA, mid, k);
                    a.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, k);
                }

                if (b != null)
                {
                    b.transform.position = Vector3.Lerp(startB, mid, k);
                    b.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, k);
                }

                yield return null;
            }

            if (a != null)
                a.HideNow();

            if (b != null)
                b.HideNow();

            tiles.Remove(a);
            tiles.Remove(b);
        }

        private IEnumerator CompactRoutine()
        {
            if (tiles.Count == 0 || slots.Count == 0)
                yield break;

            List<Vector3> startPos = new List<Vector3>(tiles.Count);

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                if (tile == null)
                    continue;

                tile.transform.localScale = Vector3.one;
                startPos.Add(tile.transform.position);
            }

            float t = 0f;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / moveTime);

                int safe = Mathf.Min(tiles.Count, startPos.Count, slots.Count);
                for (int i = 0; i < safe; i++)
                {
                    Tile tile = tiles[i];
                    if (tile == null)
                        continue;

                    tile.transform.position = Vector3.Lerp(startPos[i], slots[i].position, k);
                }

                yield return null;
            }

            CompactImmediate();
        }

        private void CompactImmediate()
        {
            int safe = Mathf.Min(tiles.Count, slots.Count);
            for (int i = 0; i < safe; i++)
            {
                if (tiles[i] == null)
                    continue;

                tiles[i].transform.position = slots[i].position;
                tiles[i].transform.localScale = Vector3.one;
            }
        }

        private void CreateSlotVisual(Transform parent, int index)
        {
            GameObject border = new GameObject($"SlotBorder_{index}");
            border.transform.SetParent(parent, false);
            border.transform.localPosition = Vector3.zero;
            border.transform.localScale = Vector3.one;

            SpriteRenderer borderSr = border.AddComponent<SpriteRenderer>();
            borderSr.sprite = whiteSprite;
            borderSr.color = slotBorderColor;
            borderSr.sortingOrder = borderOrder;
            border.transform.localScale = new Vector3(slotSize.x + borderExtra.x, slotSize.y + borderExtra.y, 1f);

            GameObject fill = new GameObject($"SlotFill_{index}");
            fill.transform.SetParent(parent, false);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localScale = Vector3.one;

            SpriteRenderer fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = whiteSprite;
            fillSr.color = slotColor;
            fillSr.sortingOrder = fillOrder;
            fill.transform.localScale = new Vector3(slotSize.x, slotSize.y, 1f);

            visuals.Add(border);
            visuals.Add(fill);
        }

        private void ClearSlotObjects()
        {
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                if (visuals[i] != null)
                    DestroyImmediateSafe(visuals[i]);
            }
            visuals.Clear();

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i] != null)
                    DestroyImmediateSafe(slots[i].gameObject);
            }
            slots.Clear();
        }

        private void EnsureWhiteSprite()
        {
            if (whiteSprite != null)
                return;

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void DestroyImmediateSafe(GameObject go)
        {
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