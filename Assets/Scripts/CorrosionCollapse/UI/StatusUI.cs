using System.Collections;
using TMPro;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class StatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI eventText;
        [SerializeField] private CanvasGroup eventGroup;

        private Coroutine eventRoutine;

        public void Initialize(TextMeshProUGUI status, TextMeshProUGUI centerEvent, CanvasGroup centerGroup)
        {
            statusText = status;
            eventText = centerEvent;
            eventGroup = centerGroup;
            ShowStatus("Ожидание хода...");
            if (eventGroup != null)
            {
                eventGroup.alpha = 0f;
                eventGroup.blocksRaycasts = false;
            }
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void ShowEvent(string message)
        {
            if (eventRoutine != null)
            {
                StopCoroutine(eventRoutine);
            }

            eventRoutine = StartCoroutine(EventRoutine(message));
        }

        private IEnumerator EventRoutine(string message)
        {
            if (eventText == null || eventGroup == null)
            {
                yield break;
            }

            eventText.text = message;
            eventGroup.alpha = 1f;
            yield return new WaitForSeconds(1.2f);

            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                eventGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.45f);
                yield return null;
            }

            eventGroup.alpha = 0f;
        }
    }
}
