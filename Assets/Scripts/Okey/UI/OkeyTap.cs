using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace OzGame.Okey
{
    public class OkeyTap : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        private OkeyTouchUI owner;
        private UnityAction action;
        private string label;
        private bool enabledTap;

        public void Init(OkeyTouchUI owner, string label, bool enabledTap, UnityAction action)
        {
            this.owner = owner;
            this.label = label;
            this.enabledTap = enabledTap;
            this.action = action;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            owner?.NoteUiEvent($"{label} down");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!enabledTap)
            {
                owner?.NoteUiEvent($"{label} disabled");
                return;
            }

            owner?.NoteUiEvent($"{label} tap");
            action?.Invoke();
        }
    }
}
