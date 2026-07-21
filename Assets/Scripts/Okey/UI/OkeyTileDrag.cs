using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGame.Okey
{
    public class OkeyTileDrag : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private OkeyTouchUI owner;
        private int tileId;
        private bool topRow;
        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 startAnchored;
        private float pointerDownTime;
        private bool groupDragActivated;
        private bool movedBeforeHold;
        private Vector2 pointerDownPosition;

        private const float GroupHoldSeconds = 1f;
        private const float HoldMoveTolerance = 10f;

        public int TileId => tileId;

        public void Init(OkeyTouchUI owner, int tileId, bool topRow, int rowIndex)
        {
            this.owner = owner;
            this.tileId = tileId;
            this.topRow = topRow;
        }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownTime = Time.unscaledTime;
            pointerDownPosition = eventData.position;
            groupDragActivated = false;
            movedBeforeHold = false;
            owner.NoteUiEvent($"tile down {tileId}");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (pointerDownTime <= 0f) pointerDownTime = Time.unscaledTime;
            if (rect == null) rect = GetComponent<RectTransform>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            startAnchored = rect.anchoredPosition;
            group.blocksRaycasts = false;
            group.alpha = 0.38f;
            transform.SetAsLastSibling();
            owner.NoteUiEvent($"drag begin {tileId}");
            owner.BeginTileDrag(tileId, eventData.position);
            TryActivateGroup(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            TryActivateGroup(eventData.position);
            owner.NoteUiEvent($"dragging {tileId}");
            owner.UpdateTileDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            group.blocksRaycasts = true;
            group.alpha = 1f;
            rect.anchoredPosition = startAnchored;
            owner.NoteUiEvent($"drag end {tileId}");
            owner.EndTileDrag();
            owner.MoveTileByDrag(tileId, topRow, eventData.position);
        }

        private void TryActivateGroup(Vector2 position)
        {
            if (groupDragActivated) return;
            var heldLongEnough = Time.unscaledTime - pointerDownTime >= GroupHoldSeconds;
            var movedDistance = Vector2.Distance(position, pointerDownPosition);
            if (!heldLongEnough)
            {
                if (movedDistance > HoldMoveTolerance) movedBeforeHold = true;
                return;
            }
            if (movedBeforeHold) return;
            groupDragActivated = owner.ActivateTileGroupDrag(tileId, position);
        }
    }
}
