using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGame.Okey
{
    public enum OkeyDropKind { Discard }

    public class OkeyDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        private OkeyTouchUI owner;
        private OkeyDropKind kind;

        public void Init(OkeyTouchUI owner, OkeyDropKind kind)
        {
            this.owner = owner;
            this.kind = kind;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (kind != OkeyDropKind.Discard) return;
            var draggedTile = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<OkeyTileDrag>() : null;
            if (draggedTile != null)
            {
                owner.NoteUiEvent($"drop tile {draggedTile.TileId}");
                owner.DropTileToDiscard(draggedTile.TileId);
            }
            else
            {
                owner.NoteUiEvent("drop selected");
                owner.DropSelectedToDiscard();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (kind != OkeyDropKind.Discard) return;
            owner.NoteUiEvent("drop click");
            owner.DropSelectedToDiscard();
        }
    }
}
