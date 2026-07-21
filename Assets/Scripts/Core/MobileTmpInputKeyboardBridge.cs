using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_InputField))]
    public sealed class MobileTmpInputKeyboardBridge : MonoBehaviour, IPointerClickHandler, ISelectHandler
    {
        private const float FallbackDelay = 0.18f;

        private TMP_InputField input;
        private TouchScreenKeyboard keyboard;
        private float fallbackAt;
        private bool fallbackPending;

        private void Awake()
        {
            input = GetComponent<TMP_InputField>();
            input.shouldHideMobileInput = false;
            input.shouldHideSoftKeyboard = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RequestKeyboard();
        }

        public void OnSelect(BaseEventData eventData)
        {
            RequestKeyboard();
        }

        private void Update()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (fallbackPending && Time.unscaledTime >= fallbackAt)
            {
                fallbackPending = false;
                OpenFallbackKeyboardIfNeeded();
            }

            if (keyboard == null)
                return;

            if (input == null || !input.isFocused)
            {
                keyboard = null;
                return;
            }

            string keyboardText;
            TouchScreenKeyboard.Status keyboardStatus;
            try
            {
                keyboardText = keyboard.text;
                keyboardStatus = keyboard.status;
            }
            catch (System.NullReferenceException)
            {
                keyboard = null;
                return;
            }

            if (input.text != keyboardText)
            {
                input.text = keyboardText;
                input.MoveTextEnd(false);
            }

            if (keyboardStatus == TouchScreenKeyboard.Status.Done ||
                keyboardStatus == TouchScreenKeyboard.Status.Canceled ||
                keyboardStatus == TouchScreenKeyboard.Status.LostFocus)
            {
                input.DeactivateInputField();
                keyboard = null;
            }
#endif
        }

        private void RequestKeyboard()
        {
            if (input == null || !input.IsActive() || !input.interactable || input.readOnly)
                return;

            input.shouldHideMobileInput = false;
            input.shouldHideSoftKeyboard = false;
            input.ActivateInputField();

#if UNITY_ANDROID || UNITY_IOS
            fallbackPending = true;
            fallbackAt = Time.unscaledTime + FallbackDelay;
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        private void OpenFallbackKeyboardIfNeeded()
        {
            if (input == null || !input.isFocused || TouchScreenKeyboard.visible)
                return;

            bool multiline = input.lineType != TMP_InputField.LineType.SingleLine;
            bool secure = input.inputType == TMP_InputField.InputType.Password ||
                          input.contentType == TMP_InputField.ContentType.Password;
            bool autocorrection = input.contentType == TMP_InputField.ContentType.Autocorrected;
            string placeholder = input.placeholder == null ? string.Empty : input.placeholder.GetComponent<TMP_Text>()?.text ?? string.Empty;
            keyboard = TouchScreenKeyboard.Open(input.text, input.keyboardType, autocorrection, multiline, secure, false, placeholder, input.characterLimit);
        }
#endif
    }
}
