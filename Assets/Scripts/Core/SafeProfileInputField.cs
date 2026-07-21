using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongGame
{
    public sealed class SafeProfileInputField : TMP_InputField
    {
        protected override void Awake()
        {
            base.Awake();

            shouldHideMobileInput = false;
            shouldHideSoftKeyboard = false;

#if UNITY_ANDROID || UNITY_IOS
            if (GetComponent<MobileTmpInputKeyboardBridge>() == null)
                gameObject.AddComponent<MobileTmpInputKeyboardBridge>();
#endif
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            FocusForMobileKeyboard();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            FocusForMobileKeyboard();
        }

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

        private void FocusForMobileKeyboard()
        {
            if (!IsActive() || !interactable || readOnly)
                return;

            shouldHideMobileInput = false;
            shouldHideSoftKeyboard = false;
            ActivateInputField();
        }
    }
}
