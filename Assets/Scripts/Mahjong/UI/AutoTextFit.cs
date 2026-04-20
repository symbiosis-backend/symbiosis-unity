using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class AutoTextFit : MonoBehaviour
    {
        [SerializeField] private TMP_Text textTarget;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private float minSize = 16f;
        [SerializeField] private float maxSize = 42f;
        [SerializeField] private int maxIterations = 12;
        [SerializeField] private bool refitOnEnable = true;

        private string lastText = string.Empty;
        private Vector2 lastSize = Vector2.zero;

        private void Awake()
        {
            if (textTarget == null)
                textTarget = GetComponent<TMP_Text>();

            if (targetRect == null)
                targetRect = GetComponent<RectTransform>();

            SetupText();
        }

        private void OnEnable()
        {
            if (refitOnEnable)
                RefitNow();
        }

        private void LateUpdate()
        {
            if (textTarget == null || targetRect == null)
                return;

            string currentText = textTarget.text ?? string.Empty;
            Vector2 currentSize = targetRect.rect.size;

            if (currentText != lastText || currentSize != lastSize)
                RefitNow();
        }

        public void RefitNow()
        {
            if (textTarget == null || targetRect == null)
                return;

            SetupText();

            float low = minSize;
            float high = maxSize;
            float best = minSize;

            for (int i = 0; i < maxIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                textTarget.fontSize = mid;
                textTarget.ForceMeshUpdate();

                if (Fits())
                {
                    best = mid;
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            textTarget.fontSize = best;
            textTarget.ForceMeshUpdate();

            lastText = textTarget.text ?? string.Empty;
            lastSize = targetRect.rect.size;
        }

        private void SetupText()
        {
            textTarget.enableAutoSizing = false;
            textTarget.textWrappingMode = TextWrappingModes.Normal;
            textTarget.overflowMode = TextOverflowModes.Overflow;
        }

        private bool Fits()
        {
            Vector2 preferred = textTarget.GetPreferredValues(textTarget.text, targetRect.rect.width, 10000f);
            return preferred.x <= targetRect.rect.width + 0.5f &&
                   preferred.y <= targetRect.rect.height + 0.5f;
        }
    }
}