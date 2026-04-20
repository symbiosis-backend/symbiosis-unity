using TMPro;
using UnityEngine.EventSystems;

namespace MahjongGame
{
    public sealed class SafeProfileInputField : TMP_InputField
    {
        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.Use();
        }
    }
}
