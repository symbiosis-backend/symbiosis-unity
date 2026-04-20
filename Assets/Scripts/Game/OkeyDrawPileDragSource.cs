using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyDrawPileDragSource : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public static OkeyDrawPileDragSource Active { get; private set; }

        [Header("Auto Links")]
        public OkeyTurnManager TurnManager;
        public Canvas RootCanvas;
        public OkeyDrawPile DrawPileView;

        private OkeyTileInstance draggedTile;
        private RectTransform draggedRect;
        private CanvasGroup draggedCanvasGroup;
        private Image hitAreaImage;
        private bool dragBusy;
        private bool placedIntoRack;
        private Coroutine refreshRoutine;

        private void Awake()
        {
            AutoResolve();
            EnsureHitArea();
            MakePileVisualsNonInteractive();
            ForceRefreshPileVisuals();
        }

        private void OnEnable()
        {
            AutoResolve();
            EnsureHitArea();
            MakePileVisualsNonInteractive();
            ForceRefreshPileVisuals();
        }

        private void OnDisable()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }

            if (Active == this)
                Active = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoResolveEditorSafe();
            EnsureHitAreaEditorSafe();
        }
#endif

        private void AutoResolve()
        {
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            if (RootCanvas == null)
                RootCanvas = GetComponentInParent<Canvas>();

            if (RootCanvas == null)
                RootCanvas = FindAnyObjectByType<Canvas>();

            if (DrawPileView == null)
                DrawPileView = GetComponent<OkeyDrawPile>();

            if (DrawPileView == null)
                DrawPileView = GetComponentInParent<OkeyDrawPile>();
        }

#if UNITY_EDITOR
        private void AutoResolveEditorSafe()
        {
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            if (RootCanvas == null)
                RootCanvas = GetComponentInParent<Canvas>();

            if (RootCanvas == null)
                RootCanvas = FindAnyObjectByType<Canvas>();

            if (DrawPileView == null)
                DrawPileView = GetComponent<OkeyDrawPile>();

            if (DrawPileView == null)
                DrawPileView = GetComponentInParent<OkeyDrawPile>();
        }
#endif

        private void EnsureHitArea()
        {
            hitAreaImage = GetComponent<Image>();
            if (hitAreaImage == null)
                hitAreaImage = gameObject.AddComponent<Image>();

            Color c = hitAreaImage.color;
            c.a = 0f;
            hitAreaImage.color = c;
            hitAreaImage.raycastTarget = true;
        }

#if UNITY_EDITOR
        private void EnsureHitAreaEditorSafe()
        {
            Image img = GetComponent<Image>();
            if (img == null)
                return;

            Color c = img.color;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = true;
        }
#endif

        private void MakePileVisualsNonInteractive()
        {
            if (DrawPileView == null)
                return;

            Transform root = DrawPileView.DrawPileRoot != null
                ? DrawPileView.DrawPileRoot
                : DrawPileView.transform;

            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                Graphic[] graphics = child.GetComponentsInChildren<Graphic>(true);
                for (int g = 0; g < graphics.Length; g++)
                {
                    if (graphics[g] != null)
                        graphics[g].raycastTarget = false;
                }

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = child.gameObject.AddComponent<CanvasGroup>();

                cg.blocksRaycasts = false;
                cg.interactable = false;
                cg.alpha = 1f;

                OkeyTileDrag tileDrag = child.GetComponent<OkeyTileDrag>();
                if (tileDrag != null)
                    tileDrag.enabled = false;
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragBusy)
                return;

            AutoResolve();
            MakePileVisualsNonInteractive();

            if (TurnManager == null || RootCanvas == null)
                return;
            if (TurnManager.WinController != null && TurnManager.WinController.GameEnded)
                return;
            if (!TurnManager.IsLocalPlayersTurn())
                return;
            if (TurnManager.LocalSeat == null)
                return;
            if (!TurnManager.CanStartLocalDrawFromDeck())
                return;

            draggedTile = TurnManager.BeginLocalDrawFromDeck();
            if (draggedTile == null)
                return;

            Active = this;
            dragBusy = true;
            placedIntoRack = false;

            draggedTile.gameObject.SetActive(true);
            draggedTile.ShowBack();
            draggedTile.RefreshVisuals();

            draggedTile.transform.SetParent(RootCanvas.transform, false);
            draggedTile.transform.SetAsLastSibling();

            draggedRect = draggedTile.transform as RectTransform;
            if (draggedRect == null)
            {
                TurnManager.CancelPendingLocalDraw();
                draggedTile = null;
                dragBusy = false;
                placedIntoRack = false;

                if (Active == this)
                    Active = null;

                ScheduleDeferredPileRefresh();
                return;
            }

            draggedCanvasGroup = draggedTile.GetComponent<CanvasGroup>();
            if (draggedCanvasGroup == null)
                draggedCanvasGroup = draggedTile.gameObject.AddComponent<CanvasGroup>();

            draggedCanvasGroup.blocksRaycasts = false;
            draggedCanvasGroup.interactable = false;
            draggedCanvasGroup.alpha = 1f;

            draggedRect.anchorMin = new Vector2(0.5f, 0.5f);
            draggedRect.anchorMax = new Vector2(0.5f, 0.5f);
            draggedRect.pivot = new Vector2(0.5f, 0.5f);
            draggedRect.localScale = Vector3.one;
            draggedRect.localRotation = Quaternion.identity;

            SnapToPointer(eventData);
            Canvas.ForceUpdateCanvases();

            ForceRefreshPileVisuals();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragBusy || draggedRect == null || RootCanvas == null)
                return;

            SnapToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragBusy)
                return;

            if (draggedCanvasGroup != null)
            {
                draggedCanvasGroup.blocksRaycasts = true;
                draggedCanvasGroup.interactable = true;
                draggedCanvasGroup.alpha = 1f;
            }

            bool cancelled = !placedIntoRack && draggedTile != null && TurnManager != null;

            if (cancelled)
            {
                TurnManager.CancelPendingLocalDraw();
            }

            draggedTile = null;
            draggedRect = null;
            draggedCanvasGroup = null;
            dragBusy = false;
            placedIntoRack = false;

            if (Active == this)
                Active = null;

            Canvas.ForceUpdateCanvases();

            if (cancelled)
                ScheduleDeferredPileRefresh();
            else
                ForceRefreshPileVisuals();
        }

        public void MarkPlacedIntoRack()
        {
            placedIntoRack = true;
        }

        public OkeyTileInstance GetDraggedTile()
        {
            return draggedTile;
        }

        public bool IsDraggingTile()
        {
            return dragBusy && draggedTile != null;
        }

        private void SnapToPointer(PointerEventData eventData)
        {
            if (draggedRect == null || RootCanvas == null || eventData == null)
                return;

            RectTransform canvasRect = RootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                draggedRect.anchorMin = new Vector2(0.5f, 0.5f);
                draggedRect.anchorMax = new Vector2(0.5f, 0.5f);
                draggedRect.pivot = new Vector2(0.5f, 0.5f);
                draggedRect.anchoredPosition = localPoint;
                draggedRect.localScale = Vector3.one;
                draggedRect.localRotation = Quaternion.identity;
            }
        }

        private void ForceRefreshPileVisuals()
        {
            AutoResolve();
            MakePileVisualsNonInteractive();

            if (DrawPileView != null)
                DrawPileView.ForceRefresh();

            Canvas.ForceUpdateCanvases();
        }

        private void ScheduleDeferredPileRefresh()
        {
            if (!isActiveAndEnabled)
                return;

            if (refreshRoutine != null)
                StopCoroutine(refreshRoutine);

            refreshRoutine = StartCoroutine(DeferredPileRefresh());
        }

        private IEnumerator DeferredPileRefresh()
        {
            yield return null;
            ForceRefreshPileVisuals();

            yield return new WaitForEndOfFrame();
            ForceRefreshPileVisuals();

            refreshRoutine = null;
        }
    }
}