using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class BGU_Vertical : MonoBehaviour
    {
        [SerializeField] private float speed = 0.02f;
        [SerializeField] private bool moveDown = false;

        private RawImage rawImage;
        private Rect uv;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            uv = rawImage.uvRect;
        }

        private void Update()
        {
            float dir = moveDown ? -1f : 1f;
            uv.y += dir * speed * Time.deltaTime;
            rawImage.uvRect = uv;
        }
    }
}