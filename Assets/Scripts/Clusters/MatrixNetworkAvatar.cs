using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class MatrixNetworkAvatar : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float sendInterval = 0.05f;
        [SerializeField] private float remoteLerp = 14f;

        private Camera avatarCamera;
        private Renderer cachedRenderer;
        private Vector3 targetPosition;
        private float nextSendTime;

        private bool IsInMatrixScene => SceneManager.GetActiveScene().name == ClusterService.MatrixSceneName;

        private void Awake()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
            targetPosition = transform.position;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            targetPosition = transform.position;
            ApplyOwnerVisuals();
        }

        private void Update()
        {
            bool inMatrix = IsInMatrixScene;
            SetVisible(inMatrix);

            if (!inMatrix)
                return;

            if (IsOwner)
            {
                UpdateLocalMovement();
                UpdateCamera();
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * remoteLerp);
            }
        }

        private void UpdateLocalMovement()
        {
            Vector2 input = ReadMovementInput();
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (input.sqrMagnitude > 0.0001f)
            {
                Vector3 delta = new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
                transform.position += delta;
            }

            transform.position = ClampToMatrixBounds(transform.position);

            if (Time.unscaledTime >= nextSendTime)
            {
                nextSendTime = Time.unscaledTime + sendInterval;
                SubmitPositionServerRpc(transform.position);
            }
        }

        private static Vector2 ReadMovementInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                y += 1f;

            return new Vector2(x, y);
        }

        private void UpdateCamera()
        {
            if (avatarCamera == null)
                avatarCamera = Camera.main;

            if (avatarCamera == null)
                return;

            Vector3 cameraPosition = transform.position + new Vector3(0f, 0f, -10f);
            avatarCamera.transform.position = Vector3.Lerp(avatarCamera.transform.position, cameraPosition, Time.deltaTime * 10f);
            avatarCamera.orthographic = true;
            avatarCamera.orthographicSize = 6f;
        }

        private void ApplyOwnerVisuals()
        {
            if (cachedRenderer == null)
                return;

            Color color = IsOwner ? new Color(0.16f, 1f, 0.72f, 1f) : new Color(0.25f, 0.56f, 1f, 1f);
            cachedRenderer.material.color = color;
        }

        private void SetVisible(bool visible)
        {
            if (cachedRenderer != null && cachedRenderer.enabled != visible)
                cachedRenderer.enabled = visible;
        }

        [ServerRpc]
        private void SubmitPositionServerRpc(Vector3 position)
        {
            Vector3 clamped = ClampToMatrixBounds(position);
            transform.position = clamped;
            ApplyPositionObserversRpc(clamped);
        }

        [ObserversRpc(IncludeOwner = false)]
        private void ApplyPositionObserversRpc(Vector3 position)
        {
            targetPosition = ClampToMatrixBounds(position);
        }

        private static Vector3 ClampToMatrixBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -9f, 9f);
            position.y = Mathf.Clamp(position.y, -4.8f, 4.8f);
            position.z = 0f;
            return position;
        }
    }
}
