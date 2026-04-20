using UnityEngine;
using UnityEngine.EventSystems;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyRackSlot : MonoBehaviour, IDropHandler
    {
        private OkeyRackBoard rackBoard;

        private void Awake()
        {
            rackBoard = GetComponentInParent<OkeyRackBoard>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            var turnManager = Object.FindAnyObjectByType<OkeyTurnManager>();
            if (turnManager != null && turnManager.WinController != null && turnManager.WinController.GameEnded)
                return;

            if (rackBoard == null)
                rackBoard = GetComponentInParent<OkeyRackBoard>();

            if (rackBoard == null)
                return;

            if (HasTileInThisSlot())
                return;

            var drawSource = Object.FindAnyObjectByType<OkeyDrawPileDragSource>();
            OkeyTileInstance tile = null;

            if (drawSource != null)
                tile = drawSource.GetDraggedTile();

            // 1. Внешний добор из общей стопки.
            if (tile != null)
            {
                if (turnManager == null)
                    return;

                if (!turnManager.IsLocalPlayersTurn())
                    return;

                if (turnManager.CurrentPhase != OkeyTurnManager.TurnPhase.MustDraw)
                    return;

                if (turnManager.LocalSeat == null)
                    return;

                bool placed = turnManager.LocalSeat.AcceptExternalTileToSlot(tile, transform);
                if (!placed)
                    return;

                tile.ShowFront();
                tile.RefreshVisuals();
                NormalizeTileRect(tile);

                drawSource.MarkPlacedIntoRack();

                bool confirmed = turnManager.ConfirmLocalDrawPlacedIntoHand();
                if (!confirmed)
                {
                    turnManager.LocalSeat.RemoveFromHand(tile);
                    turnManager.CancelPendingLocalDraw();
                }

                return;
            }

            // 2. Обычное перемещение внутри руки.
            if (eventData.pointerDrag == null)
                return;

            tile = eventData.pointerDrag.GetComponent<OkeyTileInstance>();
            if (tile == null)
                tile = eventData.pointerDrag.GetComponentInParent<OkeyTileInstance>();

            if (tile == null)
                return;

            var tileDrag = eventData.pointerDrag.GetComponent<OkeyTileDrag>();
            if (tileDrag == null)
                tileDrag = eventData.pointerDrag.GetComponentInParent<OkeyTileDrag>();

            if (tileDrag == null)
                return;

            if (turnManager == null || turnManager.LocalSeat == null)
                return;

            if (!turnManager.LocalSeat.ContainsTile(tile))
                return;

            bool moved = turnManager.LocalSeat.MoveTileToSlot(tile, transform);
            if (!moved)
                return;

            tile.ShowFront();
            tile.RefreshVisuals();
            NormalizeTileRect(tile);
            turnManager.LocalSeat.SyncTilesFromRackVisual();
        }

        private bool HasTileInThisSlot()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (child.GetComponent<OkeyTileInstance>() != null)
                    return true;
            }

            return false;
        }

        private void NormalizeTileRect(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (tile.transform is not RectTransform rt)
                return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
    }
} 