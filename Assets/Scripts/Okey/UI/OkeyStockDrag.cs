using UnityEngine;
using UnityEngine.EventSystems;

namespace OzGame.Okey
{
    public class OkeyStockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private OkeyTouchUI owner;

        public void Init(OkeyTouchUI owner)
        {
            this.owner = owner;
        }

        public void OnBeginDrag(PointerEventData eventData) => owner.NoteUiEvent("stock drag begin");
        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData)
        {
            owner.NoteUiEvent("stock drag draw");
            owner.DrawStockByDrag();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            owner.NoteUiEvent("stock drag required");
        }
    }
}
