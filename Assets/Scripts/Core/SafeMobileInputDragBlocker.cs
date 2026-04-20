using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class SafeMobileInputDragBlocker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }
    }
}
