using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyDrawPile : MonoBehaviour
    {
        [Header("Links")]
        public OkeyTurnManager TurnManager;

        [Header("Pile Root")]
        [Tooltip("Контейнер, в котором лежат реальные оставшиеся камни колоды.")]
        public Transform DrawPileRoot;

        [Header("Hit Area")]
        [Tooltip("Image, который будет ловить raycast на объекте DrawPile.")]
        public Image HitAreaImage;

        [Header("Real Stack View")]
        [Min(1)] public int VisibleTopCount = 3;
        public Vector2 StackOffset = new Vector2(0f, -6f);

        [Header("Stable Visual Background")]
        [Range(0, 3)] public int BackgroundBackCount = 2;
        public Vector2 BackgroundBackOffset = new Vector2(0f, -6f);

        private RectTransform selfRect;
        private RectTransform backgroundRoot;
        private readonly List<Image> backgroundBacks = new();

        private void Awake()
        {
            selfRect = transform as RectTransform;
            AutoResolve();
            EnsureHitArea();
            EnsureBackgroundRoot();
            SyncHitAreaToPileRoot();
            RefreshView();
        }

        private void OnEnable()
        {
            AutoResolve();
            EnsureHitArea();
            EnsureBackgroundRoot();
            SyncHitAreaToPileRoot();
            RefreshView();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                selfRect = transform as RectTransform;
                AutoResolve();
                EnsureHitArea();
                SyncHitAreaToPileRoot();
            }
        }
#endif

        private void LateUpdate()
        {
            AutoResolve();

            if (DrawPileRoot == null)
                return;

            EnsureBackgroundRoot();
            SyncHitAreaToPileRoot();
            RefreshView();
        }

        public void ForceRefresh()
        {
            RefreshView();
        }

        private void AutoResolve()
        {
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            Transform foundRoot = transform.Find("DrawPileRoot");
            if (foundRoot != null)
                DrawPileRoot = foundRoot;

            Transform foundImage = transform.Find("Image");
            if (foundImage != null)
            {
                Image childImage = foundImage.GetComponent<Image>();
                if (childImage != null)
                    HitAreaImage = childImage;
            }

            if (HitAreaImage == null)
                HitAreaImage = GetComponent<Image>();

            if (DrawPileRoot == null)
                DrawPileRoot = transform;
        }

        public void RefreshView()
        {
            if (DrawPileRoot == null)
                return;

            EnsureBackgroundRoot();

            List<OkeyTileInstance> pileTiles = GetPileTiles();
            int count = pileTiles.Count;

            if (count <= 0)
            {
                SetBackgroundVisible(false);
                return;
            }

            OkeyTileInstance topTile = pileTiles[count - 1];
            DrawStableBackground(topTile, count);

            int visibleCount = Mathf.Clamp(VisibleTopCount, 1, count);
            int firstVisibleIndex = Mathf.Max(0, count - visibleCount);

            for (int i = 0; i < count; i++)
            {
                OkeyTileInstance tile = pileTiles[i];
                if (tile == null)
                    continue;

                tile.gameObject.SetActive(true);
                tile.ShowBack();
                tile.RefreshVisuals();

                Transform child = tile.transform;

                int depth = i - firstVisibleIndex;
                if (depth < 0)
                    depth = 0;
                if (depth > visibleCount - 1)
                    depth = visibleCount - 1;

                if (child is RectTransform rt)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = StackOffset * depth;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                else
                {
                    child.localPosition = new Vector3(StackOffset.x * depth, StackOffset.y * depth, 0f);
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;
                }

                child.SetSiblingIndex(i);
                MakeChildVisualOnly(tile.gameObject);
            }
        }

        private List<OkeyTileInstance> GetPileTiles()
        {
            List<OkeyTileInstance> result = new List<OkeyTileInstance>();

            if (DrawPileRoot == null)
                return result;

            for (int i = 0; i < DrawPileRoot.childCount; i++)
            {
                Transform child = DrawPileRoot.GetChild(i);
                if (child == null)
                    continue;

                OkeyTileInstance tile = child.GetComponent<OkeyTileInstance>();
                if (tile == null)
                    continue;

                result.Add(tile);
            }

            return result;
        }

        private Vector2 GetRootSize()
        {
            if (DrawPileRoot is RectTransform rootRt)
                return rootRt.sizeDelta;

            return Vector2.zero;
        }

        private void EnsureHitArea()
        {
            if (HitAreaImage == null)
                AutoResolve();

            if (HitAreaImage == null)
                HitAreaImage = gameObject.AddComponent<Image>();

            Color c = HitAreaImage.color;
            c.a = 0f;
            HitAreaImage.color = c;
            HitAreaImage.raycastTarget = true;
        }

        private void SyncHitAreaToPileRoot()
        {
            if (selfRect == null)
                selfRect = transform as RectTransform;

            if (selfRect == null || DrawPileRoot == null)
                return;

            RectTransform rootRt = DrawPileRoot as RectTransform;
            if (rootRt == null)
                return;

            selfRect.anchorMin = rootRt.anchorMin;
            selfRect.anchorMax = rootRt.anchorMax;
            selfRect.pivot = rootRt.pivot;
            selfRect.anchoredPosition = rootRt.anchoredPosition;
            selfRect.sizeDelta = rootRt.sizeDelta;
            selfRect.localScale = Vector3.one;
            selfRect.localRotation = Quaternion.identity;

            if (backgroundRoot != null)
            {
                backgroundRoot.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRoot.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRoot.pivot = new Vector2(0.5f, 0.5f);
                backgroundRoot.anchoredPosition = Vector2.zero;
                backgroundRoot.sizeDelta = Vector2.zero;
                backgroundRoot.localScale = Vector3.one;
                backgroundRoot.localRotation = Quaternion.identity;
                backgroundRoot.SetAsFirstSibling();
            }
        }

        private void MakeChildVisualOnly(GameObject go)
        {
            if (go == null)
                return;

            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = false;
            }

            CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = go.AddComponent<CanvasGroup>();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 1f;

            OkeyTileDrag tileDrag = go.GetComponent<OkeyTileDrag>();
            if (tileDrag != null)
                tileDrag.enabled = false;
        }

        private void EnsureBackgroundRoot()
        {
            if (!Application.isPlaying)
                return;

            Transform found = transform.Find("PileBackgroundRoot");
            if (found != null)
            {
                backgroundRoot = found as RectTransform;
                EnsureBackgroundRootCanvasGroup();
                return;
            }

            GameObject go = new GameObject("PileBackgroundRoot", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            backgroundRoot = go.transform as RectTransform;

            if (backgroundRoot != null)
            {
                backgroundRoot.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRoot.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRoot.pivot = new Vector2(0.5f, 0.5f);
                backgroundRoot.anchoredPosition = Vector2.zero;
                backgroundRoot.sizeDelta = Vector2.zero;
                backgroundRoot.localScale = Vector3.one;
                backgroundRoot.localRotation = Quaternion.identity;
                backgroundRoot.SetAsFirstSibling();
            }

            EnsureBackgroundRootCanvasGroup();
        }

        private void EnsureBackgroundRootCanvasGroup()
        {
            if (backgroundRoot == null)
                return;

            CanvasGroup cg = backgroundRoot.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = backgroundRoot.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        private void DrawStableBackground(OkeyTileInstance sourceTile, int pileCount)
        {
            if (backgroundRoot == null)
                return;

            int bgCount = Mathf.Clamp(BackgroundBackCount, 0, Mathf.Max(0, pileCount - 1));
            if (bgCount <= 0)
            {
                SetBackgroundVisible(false);
                return;
            }

            SetBackgroundVisible(true);
            EnsureBackgroundBackCount(bgCount);

            Sprite backSprite = ResolveBackSprite(sourceTile);
            Vector2 size = GetBackgroundTileSize(sourceTile);

            for (int i = 0; i < backgroundBacks.Count; i++)
            {
                Image img = backgroundBacks[i];
                if (img == null)
                    continue;

                bool visible = i < bgCount;
                img.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                RectTransform rt = img.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = BackgroundBackOffset * (i + 1);
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                    rt.sizeDelta = size;
                    rt.SetSiblingIndex(i);
                }

                img.sprite = backSprite;
                img.enabled = backSprite != null;
                img.raycastTarget = false;
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
                img.useSpriteMesh = false;

                CanvasGroup cg = img.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = img.gameObject.AddComponent<CanvasGroup>();

                cg.alpha = 1f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        private Sprite ResolveBackSprite(OkeyTileInstance sourceTile)
        {
            if (sourceTile != null && sourceTile.BackSprite != null)
                return sourceTile.BackSprite;

            if (DrawPileRoot != null)
            {
                for (int i = 0; i < DrawPileRoot.childCount; i++)
                {
                    Transform child = DrawPileRoot.GetChild(i);
                    if (child == null)
                        continue;

                    OkeyTileInstance tile = child.GetComponent<OkeyTileInstance>();
                    if (tile != null && tile.BackSprite != null)
                        return tile.BackSprite;
                }
            }

            return null;
        }

        private Vector2 GetBackgroundTileSize(OkeyTileInstance sourceTile)
        {
            if (sourceTile != null && sourceTile.transform is RectTransform tileRt && tileRt.sizeDelta != Vector2.zero)
                return tileRt.sizeDelta;

            Vector2 rootSize = GetRootSize();
            if (rootSize != Vector2.zero)
                return rootSize;

            return new Vector2(70f, 100f);
        }

        private void EnsureBackgroundBackCount(int count)
        {
            if (backgroundRoot == null)
                return;

            while (backgroundBacks.Count < count)
            {
                GameObject go = new GameObject(
                    $"PileBgBack_{backgroundBacks.Count}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup)
                );

                go.transform.SetParent(backgroundRoot, false);

                Image img = go.GetComponent<Image>();
                img.raycastTarget = false;

                CanvasGroup cg = go.GetComponent<CanvasGroup>();
                cg.alpha = 1f;
                cg.blocksRaycasts = false;
                cg.interactable = false;

                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }

                backgroundBacks.Add(img);
            }

            for (int i = 0; i < backgroundBacks.Count; i++)
            {
                if (backgroundBacks[i] != null)
                    backgroundBacks[i].gameObject.SetActive(i < count);
            }
        }

        private void SetBackgroundVisible(bool visible)
        {
            if (backgroundRoot != null)
                backgroundRoot.gameObject.SetActive(visible);
        }
    }
}