using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyRackSlotDropTarget : MonoBehaviour, IDropHandler
    {
        public OkeyTurnManager TurnManager;
        public OkeyPlayerSeat Seat;
        public Transform SlotTransform;

        private OkeyRackBoard rackBoard;

        private void Awake()
        {
            AutoResolve();
        }

        private void OnEnable()
        {
            AutoResolve();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoResolve();
        }
#endif

        private void AutoResolve()
        {
            if (SlotTransform == null)
                SlotTransform = transform;

            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            if (rackBoard == null)
                rackBoard = GetComponentInParent<OkeyRackBoard>();

            if (Seat == null && rackBoard != null)
                Seat = rackBoard.OwnerSeat;

            if (Seat == null)
                Seat = GetComponentInParent<OkeyPlayerSeat>();
        }

        private void ApplyTileVisual(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (TurnManager != null && TurnManager.Table != null)
                TurnManager.Table.ApplyHandOrBoardVisual(tile);
            else
            {
                tile.ShowFront();
                tile.RefreshVisuals();
            }
        }

        private void ApplyGroupVisual(List<OkeyTileInstance> group)
        {
            if (group == null)
                return;

            for (int i = 0; i < group.Count; i++)
                ApplyTileVisual(group[i]);
        }

        public void OnDrop(PointerEventData eventData)
        {
            AutoResolve();

            if (Seat == null || TurnManager == null || SlotTransform == null || rackBoard == null)
                return;

            if (TryHandleDeckDrawDrop())
                return;

            if (TryHandleDiscardDrawDrop())
                return;

            if (eventData == null || eventData.pointerDrag == null)
                return;

            TryHandleHandTileDrop(eventData.pointerDrag.gameObject);
        }

        private bool TryHandleDeckDrawDrop()
        {
            OkeyDrawPileDragSource drawSource = OkeyDrawPileDragSource.Active;

            if (drawSource == null || !drawSource.IsDraggingTile())
                return false;

            OkeyTileInstance tile = drawSource.GetDraggedTile();
            if (tile == null)
                return false;

            if (!Seat.ContainsTile(tile))
                Seat.AddToHand(tile);

            bool placed = rackBoard.InsertTileIntoSlot(tile, SlotTransform);
            if (!placed)
                return false;

            ApplyTileVisual(tile);

            drawSource.MarkPlacedIntoRack();
            TurnManager.ConfirmLocalDrawPlacedIntoHand();

            Debug.Log($"[FIX] INSERT FROM DECK: {tile.name}");
            return true;
        }

        private bool TryHandleDiscardDrawDrop()
        {
            OkeyDiscardPileDragSource discardSource = OkeyDiscardPileDragSource.Active;

            if (discardSource == null || !discardSource.IsDraggingTile())
                return false;

            OkeyTileInstance tile = discardSource.GetDraggedTile();
            if (tile == null)
                return false;

            if (!Seat.ContainsTile(tile))
                Seat.AddToHand(tile);

            bool placed = rackBoard.InsertTileIntoSlot(tile, SlotTransform);
            if (!placed)
                return false;

            ApplyTileVisual(tile);

            discardSource.MarkPlacedIntoRack();
            TurnManager.ConfirmLocalDrawPlacedIntoHand();

            Debug.Log($"[FIX] INSERT FROM DISCARD: {tile.name}");
            return true;
        }

        private bool TryHandleHandTileDrop(GameObject draggedObject)
        {
            if (draggedObject == null)
                return false;

            OkeyTileInstance tile = draggedObject.GetComponent<OkeyTileInstance>();
            OkeyTileDrag drag = draggedObject.GetComponent<OkeyTileDrag>();

            if (tile == null || drag == null)
                return false;

            if (!Seat.ContainsTile(tile))
                return false;

            if (drag.IsGroupMode)
            {
                List<OkeyTileInstance> group = new List<OkeyTileInstance>(drag.CachedGroup);

                for (int i = group.Count - 1; i >= 0; i--)
                {
                    if (group[i] == null || !Seat.ContainsTile(group[i]))
                        group.RemoveAt(i);
                }

                if (group.Count <= 1)
                    return false;

                int anchorIndex = drag.GroupAnchorIndex;
                if (anchorIndex < 0)
                    anchorIndex = 0;
                if (anchorIndex >= group.Count)
                    anchorIndex = group.Count - 1;

                drag.RestoreToOriginalSlotForGroupDrop();

                bool groupPlaced = rackBoard.InsertGroupIntoSlot(group, SlotTransform, anchorIndex);
                if (!groupPlaced)
                    return false;

                ApplyGroupVisual(group);

                drag.MarkDropHandled();
                Debug.Log($"[FIX] REORDER GROUP: {group.Count} tiles, anchor={anchorIndex}");
                return true;
            }

            bool placed = rackBoard.InsertTileIntoSlot(tile, SlotTransform);
            if (!placed)
                return false;

            ApplyTileVisual(tile);

            drag.MarkDropHandled();
            Debug.Log($"[FIX] REORDER TILE: {tile.name}");
            return true;
        }
    }
}