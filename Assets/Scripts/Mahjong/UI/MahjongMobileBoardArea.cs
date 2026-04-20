using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MahjongMobileBoardArea : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        private float lastAspect = -1f;

        private void Awake()
        {
            if (target == null)
                target = GetComponent<RectTransform>();

            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            float aspect = Screen.width / (float)Screen.height;
            if (!Mathf.Approximately(lastAspect, aspect))
                Apply();
        }

        private void Apply()
        {
            if (target == null)
                return;

            float aspect = Screen.width / (float)Screen.height;
            lastAspect = aspect;

            if (aspect >= 1.7f)
            {
                target.anchorMin = new Vector2(0.03f, 0.10f);
                target.anchorMax = new Vector2(0.97f, 0.88f);
            }
            else
            {
                target.anchorMin = new Vector2(0.04f, 0.10f);
                target.anchorMax = new Vector2(0.96f, 0.86f);
            }

            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}