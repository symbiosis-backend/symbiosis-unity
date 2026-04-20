using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float followSpeed = 13f;
        [SerializeField] private float smoothing = 0.08f;
        [SerializeField] private float screenPadding = 0.55f;
        [SerializeField] private bool rotateTowardPointer = true;
        [SerializeField] private bool requirePressToFollow = true;

        private Vector2 targetPosition;
        private Vector2 velocity;

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            targetPosition = transform.position;
        }

        private void OnEnable()
        {
            targetPosition = transform.position;
            velocity = Vector2.zero;
        }

        private void Update()
        {
            if (TryGetPointerWorldPosition(out Vector2 pointer))
                targetPosition = ClampToCamera(pointer);

            Vector2 current = transform.position;
            Vector2 next = Vector2.SmoothDamp(current, targetPosition, ref velocity, smoothing, followSpeed);
            transform.position = next;

            if (rotateTowardPointer)
                RotateToward(targetPosition - next);
        }

        private bool TryGetPointerWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = targetPosition;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 screen = Touchscreen.current.primaryTouch.position.ReadValue();
                worldPosition = worldCamera.ScreenToWorldPoint(screen);
                return true;
            }

            if (requirePressToFollow && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
                return false;

            if (Mouse.current != null)
            {
                Vector2 screen = Mouse.current.position.ReadValue();
                worldPosition = worldCamera.ScreenToWorldPoint(screen);
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                worldPosition = worldCamera.ScreenToWorldPoint(Input.GetTouch(0).position);
                return true;
            }

            if (requirePressToFollow && !Input.GetMouseButton(0))
                return false;

            worldPosition = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            return true;
#else
            return false;
#endif
        }

        private Vector2 ClampToCamera(Vector2 position)
        {
            if (worldCamera == null)
                return position;

            float halfHeight = worldCamera.orthographicSize;
            float halfWidth = halfHeight * worldCamera.aspect;
            Vector3 cameraPosition = worldCamera.transform.position;

            return new Vector2(
                Mathf.Clamp(position.x, cameraPosition.x - halfWidth + screenPadding, cameraPosition.x + halfWidth - screenPadding),
                Mathf.Clamp(position.y, cameraPosition.y - halfHeight + screenPadding, cameraPosition.y + halfHeight - screenPadding)
            );
        }

        private void RotateToward(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, angle), Time.deltaTime * 12f);
        }
    }
}
