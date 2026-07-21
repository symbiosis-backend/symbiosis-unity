using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGame.Okey
{
    public class OkeyDiscardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private OkeyTouchUI owner;
        private int seat;

        public void Init(OkeyTouchUI owner, int seat)
        {
            this.owner = owner;
            this.seat = seat;
        }

        public void OnBeginDrag(PointerEventData eventData) => owner.NoteUiEvent($"discard drag {seat}");
        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData)
        {
            owner.NoteUiEvent($"discard take drag {seat}");
            owner.TakeDiscardByDrag(seat);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            owner.NoteUiEvent($"discard drag required {seat}");
        }
    }
}
