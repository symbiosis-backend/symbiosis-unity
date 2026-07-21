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
        }
    }
}
