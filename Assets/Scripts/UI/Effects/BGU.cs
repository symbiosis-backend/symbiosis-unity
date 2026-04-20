using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class BGU : MonoBehaviour
    {
        [SerializeField] private float speed = 0.02f;
        [SerializeField] private bool moveLeft = false;

        private RawImage rawImage;
        private Rect uv;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            uv = rawImage.uvRect;
        }

        private void Update()
        {
            float dir = moveLeft ? -1f : 1f;
            uv.x += dir * speed * Time.deltaTime;
            rawImage.uvRect = uv;
        }
    }
}