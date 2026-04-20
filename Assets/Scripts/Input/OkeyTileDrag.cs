using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyTileDrag : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Group Drag")]
        [SerializeField] private float groupHoldTime = 0.45f;
        [SerializeField] private float mouseMaxMoveBeforeCancelGroup = 18f;
        [SerializeField] private float touchMaxMoveBeforeCancelGroup = 42f;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.7f, 1f);

        private RectTransform rectTransform;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private OkeyRackBoard rackBoard;
        private OkeyTileInstance tileInstance;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalLocalScale;
        private Quaternion originalLocalRotation;
        private Vector2 originalAnchorMin;
        private Vector2 originalAnchorMax;
        private Vector2 originalPivot;

        private bool pointerHeld;
        private bool dragStarted;
        private bool dropHandled;
        private bool groupMode;
        private bool groupModeLockedOut;
        private bool pointerIsTouch;

        private Vector2 pointerDownScreenPosition;
        private Coroutine groupHoldCoroutine;

        private readonly List<Graphic> cachedGraphics = new();
        private readonly List<bool> cachedRaycastStates = new();

        private readonly List<OkeyTileInstance> cachedGroup = new();
        private readonly Dictionary<Image, Color> originalColors = new();
        private readonly List<RectTransform> groupGhosts = new();
        private readonly Dictionary<OkeyTileInstance, float> hiddenOriginalAlpha = new();

        public bool IsGroupMode => groupMode;
        public IReadOnlyList<OkeyTileInstance> CachedGroup => cachedGroup;
        public OkeyTileInstance TileInstance => tileInstance;
        public OkeyRackBoard RackBoard => rackBoard;
        public Transform OriginalParent => originalParent;

        public int GroupAnchorIndex
        {
            get
            {
                if (cachedGroup.Count == 0 || tileInstance == null)
                    return 0;

                int idx = cachedGroup.IndexOf(tileInstance);
                return idx < 0 ? 0 : idx;
            }
        }

        private void Awake()
        {
            ResolveRefs();
            ForceRestoreSelfVisualState();
        }

        private void OnEnable()
        {
            ResolveRefs();
            ClearGroupHighlight();
            DestroyGroupGhosts();
            RestoreOriginalGroupVisuals();
            ForceRestoreSelfVisualState();
        }

        private void OnDisable()
        {
            ClearGroupHighlight();
            DestroyGroupGhosts();
            RestoreOriginalGroupVisuals();
            StopHoldRoutine();
            ClearCachedGroup();

            groupMode = false;
            groupModeLockedOut = false;
            pointerHeld = false;
            dragStarted = false;
            dropHandled = false;
        }

        private void OnDestroy()
        {
            ClearGroupHighlight();
            DestroyGroupGhosts();
            RestoreOriginalGroupVisuals();
        }

        private void ResolveRefs()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (tileInstance == null)
                tileInstance = GetComponent<OkeyTileInstance>();

            if (rackBoard == null)
                rackBoard = GetComponentInParent<OkeyRackBoard>();

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindAnyObjectByType<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ResolveRefs();

            pointerHeld = true;
            dragStarted = false;
            dropHandled = false;
            groupMode = false;
            groupModeLockedOut = false;

            ClearGroupHighlight();
            DestroyGroupGhosts();
            RestoreOriginalGroupVisuals();
            ForceRestoreSelfVisualState();
            ClearCachedGroup();

            if (eventData != null)
            {
                pointerDownScreenPosition = eventData.position;
                pointerIsTouch = eventData.pointerId >= 0;
            }
            else
            {
                pointerDownScreenPosition = Vector2.zero;
                pointerIsTouch = false;
            }

            if (rackBoard != null && tileInstance != null)
            {
                List<OkeyTileInstance> group = rackBoard.GetConnectedGroup(tileInstance);
                if (group != null)
                    cachedGroup.AddRange(group);
            }

            StopHoldRoutine();
            groupHoldCoroutine = StartCoroutine(HoldToEnableGroupMode());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerHeld = false;
            StopHoldRoutine();

            if (!dragStarted)
            {
                groupMode = false;
                ClearGroupHighlight();
                DestroyGroupGhosts();
                RestoreOriginalGroupVisuals();
                ForceRestoreSelfVisualState();
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            bool isTouch = eventData.pointerId >= 0;
            eventData.useDragThreshold = isTouch;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ResolveRefs();

            if (rectTransform == null || rootCanvas == null)
                return;

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalLocalScale = rectTransform.localScale;
            originalLocalRotation = rectTransform.localRotation;
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            originalPivot = rectTransform.pivot;

            if (eventData != null)
            {
                float moved = Vector2.Distance(pointerDownScreenPosition, eventData.position);
                float limit = GetCurrentHoldCancelDistance();
                if (moved > limit && !groupMode)
                {
                    groupModeLockedOut = true;
                    StopHoldRoutine();
                    ClearGroupHighlight();
                }
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.9f;

            SetGraphicsRaycastEnabled(false);

            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            SnapToPointer(eventData);

            dragStarted = true;

            if (groupMode)
            {
                HideOriginalGroupVisuals();
                EnsureGroupGhosts();
                UpdateGroupGhosts();
            }

            Debug.Log($"[OkeyTileDrag] Begin drag: {name}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragStarted)
                return;

            if (eventData != null && !groupMode)
            {
                float moved = Vector2.Distance(pointerDownScreenPosition, eventData.position);
                float limit = GetCurrentHoldCancelDistance();

                if (moved > limit && !groupModeLockedOut)
                {
                    groupModeLockedOut = true;
                    StopHoldRoutine();
                    ClearGroupHighlight();
                }
            }

            SnapToPointer(eventData);

            if (groupMode)
            {
                EnsureGroupGhosts();
                UpdateGroupGhosts();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragStarted)
                return;

            StopHoldRoutine();

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1f;

            SetGraphicsRaycastEnabled(true);

            StartCoroutine(DelayedDropCheck());

            dragStarted = false;
            pointerHeld = false;
        }

        private IEnumerator HoldToEnableGroupMode()
        {
            yield return new WaitForSeconds(groupHoldTime);

            if (!pointerHeld || dropHandled || groupModeLockedOut)
                yield break;

            if (cachedGroup.Count <= 1)
                yield break;

            groupMode = true;
            ApplyGroupHighlight();

            if (dragStarted)
            {
                HideOriginalGroupVisuals();
                EnsureGroupGhosts();
                UpdateGroupGhosts();
            }

            Debug.Log($"[OkeyTileDrag] Group mode enabled: {name}, count={cachedGroup.Count}");
        }

        private IEnumerator DelayedDropCheck()
        {
            yield return null;

            if (!dropHandled)
            {
                ReturnToOriginalSlot();
                Debug.Log($"[OkeyTileDrag] Drop rejected, returned: {name}");
            }
            else
            {
                Debug.Log($"[OkeyTileDrag] Drop accepted: {name}");
            }

            groupMode = false;
            groupModeLockedOut = false;
            ClearGroupHighlight();
            DestroyGroupGhosts();
            RestoreOriginalGroupVisuals();
            ForceRestoreSelfVisualState();
            ForceRebuildAroundTile();
        }

        public void MarkDropHandled()
        {
            dropHandled = true;
        }

        public void ReturnToOriginalSlot()
        {
            if (originalParent == null || rectTransform == null)
                return;

            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);

            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
            rectTransform.pivot = originalPivot;
            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localScale = originalLocalScale;
            rectTransform.localRotation = originalLocalRotation;

            ResolveRefs();
            ForceRestoreSelfVisualState();
            ForceRebuildAroundTile();
        }

        public void RestoreToOriginalSlotForGroupDrop()
        {
            ReturnToOriginalSlot();
        }

        private float GetCurrentHoldCancelDistance()
        {
            return pointerIsTouch ? touchMaxMoveBeforeCancelGroup : mouseMaxMoveBeforeCancelGroup;
        }

        private void ApplyGroupHighlight()
        {
            ClearGroupHighlight();

            for (int i = 0; i < cachedGroup.Count; i++)
            {
                OkeyTileInstance tile = cachedGroup[i];
                if (tile == null)
                    continue;

                Image[] images = tile.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < images.Length; j++)
                {
                    Image img = images[j];
                    if (img == null)
                        continue;

                    if (!originalColors.ContainsKey(img))
                        originalColors.Add(img, img.color);

                    img.color = MultiplyColor(img.color, highlightColor);
                }
            }
        }

        private void ClearGroupHighlight()
        {
            foreach (var kv in originalColors)
            {
                if (kv.Key != null)
                    kv.Key.color = kv.Value;
            }

            originalColors.Clear();
        }

        private void HideOriginalGroupVisuals()
        {
            hiddenOriginalAlpha.Clear();

            for (int i = 0; i < cachedGroup.Count; i++)
            {
                OkeyTileInstance tile = cachedGroup[i];
                if (tile == null || tile == tileInstance)
                    continue;

                CanvasGroup cg = tile.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = tile.gameObject.AddComponent<CanvasGroup>();

                if (!hiddenOriginalAlpha.ContainsKey(tile))
                    hiddenOriginalAlpha.Add(tile, cg.alpha);

                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        private void RestoreOriginalGroupVisuals()
        {
            foreach (var kv in hiddenOriginalAlpha)
            {
                if (kv.Key == null)
                    continue;

                CanvasGroup cg = kv.Key.GetComponent<CanvasGroup>();
                if (cg == null)
                    continue;

                cg.alpha = kv.Value;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            hiddenOriginalAlpha.Clear();
        }

        private Color MultiplyColor(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
        }

        private void ClearCachedGroup()
        {
            cachedGroup.Clear();
        }

        private void StopHoldRoutine()
        {
            if (groupHoldCoroutine != null)
            {
                StopCoroutine(groupHoldCoroutine);
                groupHoldCoroutine = null;
            }
        }

        private void EnsureGroupGhosts()
        {
            if (!groupMode || rootCanvas == null || cachedGroup.Count <= 1)
                return;

            if (groupGhosts.Count > 0)
                return;

            for (int i = 0; i < cachedGroup.Count; i++)
            {
                OkeyTileInstance sourceTile = cachedGroup[i];
                if (sourceTile == null || sourceTile == tileInstance)
                    continue;

                GameObject ghostRoot = new GameObject(
                    $"Ghost_{sourceTile.name}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(CanvasGroup)
                );

                ghostRoot.transform.SetParent(rootCanvas.transform, false);
                ghostRoot.transform.SetAsLastSibling();

                RectTransform ghostRt = ghostRoot.GetComponent<RectTransform>();
                RectTransform srcRt = sourceTile.transform as RectTransform;

                ghostRt.anchorMin = new Vector2(0.5f, 0.5f);
                ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
                ghostRt.pivot = new Vector2(0.5f, 0.5f);
                ghostRt.localScale = Vector3.one;
                ghostRt.localRotation = Quaternion.identity;
                ghostRt.sizeDelta = srcRt != null ? srcRt.sizeDelta : rectTransform.sizeDelta;

                CanvasGroup ghostCg = ghostRoot.GetComponent<CanvasGroup>();
                ghostCg.blocksRaycasts = false;
                ghostCg.interactable = false;
                ghostCg.alpha = 0.9f;

                Image[] sourceImages = sourceTile.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < sourceImages.Length; j++)
                {
                    Image srcImg = sourceImages[j];
                    if (srcImg == null)
                        continue;

                    GameObject child = new GameObject(
                        srcImg.gameObject.name,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );

                    child.transform.SetParent(ghostRoot.transform, false);

                    RectTransform childRt = child.GetComponent<RectTransform>();
                    RectTransform srcChildRt = srcImg.transform as RectTransform;

                    if (srcChildRt != null)
                    {
                        childRt.anchorMin = srcChildRt.anchorMin;
                        childRt.anchorMax = srcChildRt.anchorMax;
                        childRt.pivot = srcChildRt.pivot;
                        childRt.anchoredPosition = srcChildRt.anchoredPosition;
                        childRt.sizeDelta = srcChildRt.sizeDelta;
                        childRt.localScale = srcChildRt.localScale;
                        childRt.localRotation = srcChildRt.localRotation;
                    }
                    else
                    {
                        childRt.anchorMin = Vector2.zero;
                        childRt.anchorMax = Vector2.one;
                        childRt.pivot = new Vector2(0.5f, 0.5f);
                        childRt.offsetMin = Vector2.zero;
                        childRt.offsetMax = Vector2.zero;
                    }

                    Image childImg = child.GetComponent<Image>();
                    childImg.sprite = srcImg.sprite;
                    childImg.color = srcImg.color;
                    childImg.material = srcImg.material;
                    childImg.type = srcImg.type;
                    childImg.preserveAspect = srcImg.preserveAspect;
                    childImg.useSpriteMesh = srcImg.useSpriteMesh;
                    childImg.raycastTarget = false;
                    child.SetActive(srcImg.gameObject.activeSelf);
                }

                groupGhosts.Add(ghostRt);
            }
        }

        private void UpdateGroupGhosts()
        {
            if (!groupMode || groupGhosts.Count == 0 || rackBoard == null || rectTransform == null)
                return;

            int leaderIndex = cachedGroup.IndexOf(tileInstance);
            if (leaderIndex < 0)
                leaderIndex = 0;

            float stepX = rackBoard.CellSize.x + rackBoard.Spacing.x;

            int ghostCursor = 0;
            for (int i = 0; i < cachedGroup.Count; i++)
            {
                OkeyTileInstance sourceTile = cachedGroup[i];
                if (sourceTile == null || sourceTile == tileInstance)
                    continue;

                if (ghostCursor >= groupGhosts.Count)
                    break;

                RectTransform ghostRt = groupGhosts[ghostCursor];
                if (ghostRt == null)
                {
                    ghostCursor++;
                    continue;
                }

                int relative = i - leaderIndex;

                ghostRt.anchorMin = new Vector2(0.5f, 0.5f);
                ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
                ghostRt.pivot = new Vector2(0.5f, 0.5f);
                ghostRt.anchoredPosition = rectTransform.anchoredPosition + new Vector2(relative * stepX, 0f);
                ghostRt.localScale = Vector3.one;
                ghostRt.localRotation = Quaternion.identity;

                ghostCursor++;
            }
        }

        private void DestroyGroupGhosts()
        {
            for (int i = 0; i < groupGhosts.Count; i++)
            {
                if (groupGhosts[i] != null)
                    Destroy(groupGhosts[i].gameObject);
            }

            groupGhosts.Clear();
        }

        private void SetGraphicsRaycastEnabled(bool enabled)
        {
            if (!enabled)
            {
                cachedGraphics.Clear();
                cachedRaycastStates.Clear();

                Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] == null)
                        continue;

                    cachedGraphics.Add(graphics[i]);
                    cachedRaycastStates.Add(graphics[i].raycastTarget);
                    graphics[i].raycastTarget = false;
                }
            }
            else
            {
                int count = Mathf.Min(cachedGraphics.Count, cachedRaycastStates.Count);
                for (int i = 0; i < count; i++)
                {
                    if (cachedGraphics[i] != null)
                        cachedGraphics[i].raycastTarget = cachedRaycastStates[i];
                }

                cachedGraphics.Clear();
                cachedRaycastStates.Clear();
            }
        }

        private void SnapToPointer(PointerEventData eventData)
        {
            if (rectTransform == null || rootCanvas == null || eventData == null)
                return;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = localPoint;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }
        }

        private void ForceRestoreSelfVisualState()
        {
            ResolveRefs();

            gameObject.SetActive(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            SetGraphicsRaycastEnabled(true);

            if (tileInstance != null)
                tileInstance.RefreshVisuals();

            Canvas.ForceUpdateCanvases();
        }

        private void ForceRebuildAroundTile()
        {
            if (rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            RectTransform parentRt = transform.parent as RectTransform;
            if (parentRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);

            OkeyRackBoard board = GetComponentInParent<OkeyRackBoard>();
            if (board != null && board.transform is RectTransform boardRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(boardRt);

            Canvas.ForceUpdateCanvases();
        }
    }
}
