using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyDiscardPileDragSource : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public static OkeyDiscardPileDragSource Active { get; private set; }

        [Header("Auto Links")]
        public OkeyTurnManager TurnManager;
        public OkeyDiscardPile DiscardPile;
        public Canvas RootCanvas;

        private OkeyTileInstance draggedTile;
        private RectTransform draggedRect;
        private CanvasGroup draggedCanvasGroup;
        private Image hitAreaImage;
        private bool dragBusy;
        private bool placedIntoRack;

        private void Awake()
        {
            AutoResolve();
            EnsureHitArea();
        }

        private void OnEnable()
        {
            AutoResolve();
            EnsureHitArea();
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        private void AutoResolve()
        {
            if (DiscardPile == null)
                DiscardPile = GetComponent<OkeyDiscardPile>();

            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            if (RootCanvas == null)
                RootCanvas = GetComponentInParent<Canvas>();

            if (RootCanvas == null)
                RootCanvas = FindAnyObjectByType<Canvas>();
        }

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

            if (TurnManager == null || RootCanvas == null || DiscardPile == null)
                return;

            if (TurnManager.WinController != null && TurnManager.WinController.GameEnded)
                return;

            if (!TurnManager.IsLocalPlayersTurn())
                return;

            if (!TurnManager.CanStartLocalDrawFromDiscard(DiscardPile))
                return;

            draggedTile = TurnManager.BeginLocalDrawFromDiscard(DiscardPile);
            if (draggedTile == null)
                return;

            draggedTile.gameObject.SetActive(true);
            draggedTile.ShowFront();
            draggedTile.RefreshVisuals();

            draggedTile.transform.SetParent(RootCanvas.transform, false);
            draggedTile.transform.SetAsLastSibling();

            draggedRect = draggedTile.transform as RectTransform;
            if (draggedRect == null)
            {
                TurnManager.CancelPendingLocalDraw();
                draggedTile = null;
                return;
            }

            draggedCanvasGroup = draggedTile.GetComponent<CanvasGroup>();
            if (draggedCanvasGroup == null)
                draggedCanvasGroup = draggedTile.gameObject.AddComponent<CanvasGroup>();

            // КЛЮЧЕВОЙ ФИКС: не блокировать слот под курсором.
            draggedCanvasGroup.blocksRaycasts = false;
            draggedCanvasGroup.interactable = false;
            draggedCanvasGroup.alpha = 1f;

            draggedRect.anchorMin = new Vector2(0.5f, 0.5f);
            draggedRect.anchorMax = new Vector2(0.5f, 0.5f);
            draggedRect.pivot = new Vector2(0.5f, 0.5f);
            draggedRect.localScale = Vector3.one;
            draggedRect.localRotation = Quaternion.identity;
            SnapToPointer(eventData);

            Active = this;
            dragBusy = true;
            placedIntoRack = false;

            Debug.Log($"[OkeyDiscardPileDragSource] Begin drag from discard: {draggedTile.name}");
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
            }

            if (!placedIntoRack && draggedTile != null && TurnManager != null)
            {
                Debug.Log($"[OkeyDiscardPileDragSource] Draw from discard cancelled: {draggedTile.name}");
                TurnManager.CancelPendingLocalDraw();
            }
            else if (draggedTile != null)
            {
                Debug.Log($"[OkeyDiscardPileDragSource] Draw from discard placed: {draggedTile.name}");
            }

            draggedTile = null;
            draggedRect = null;
            draggedCanvasGroup = null;
            dragBusy = false;
            placedIntoRack = false;

            if (Active == this)
                Active = null;
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
    }
}