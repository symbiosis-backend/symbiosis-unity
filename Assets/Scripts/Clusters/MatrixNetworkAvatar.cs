using FishNet.Object;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class MatrixNetworkAvatar : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float sendInterval = 0.05f;
        [SerializeField] private float remoteLerp = 14f;
        [SerializeField] private float pointerStopDistance = 0.12f;

        public static MatrixNetworkAvatar LocalAvatar { get; private set; }

        private Camera avatarCamera;
        private Renderer cachedRenderer;
        private MeshFilter cachedMeshFilter;
        private Vector3 targetPosition;
        private Vector3 moveTarget;
        private string currentClusterId = ClusterService.ElysiumId;
        private float nextSendTime;
        private int archetype;
        private bool hasMoveTarget;

        private const float AvatarZ = -1f;

        private string ActiveClusterId => ClusterService.TryGetSceneClusterId(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        private bool IsInActiveCluster => currentClusterId == ActiveClusterId;

        private void Awake()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
            cachedMeshFilter = GetComponentInChildren<MeshFilter>();
            targetPosition = transform.position;
            moveTarget = transform.position;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            targetPosition = transform.position;
            moveTarget = transform.position;
            if (IsOwner)
                LocalAvatar = this;

            ApplyOwnerVisuals();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (LocalAvatar == this)
                LocalAvatar = null;
        }

        private void Update()
        {
            bool visible = IsInActiveCluster;
            SetVisible(visible);

            if (!visible)
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

        public void EnterCluster(string clusterId)
        {
            if (!IsOwner || string.IsNullOrWhiteSpace(clusterId))
                return;

            currentClusterId = clusterId;
            Vector3 spawn = GetClusterSpawn(clusterId);
            transform.position = spawn;
            targetPosition = spawn;
            moveTarget = spawn;
            hasMoveTarget = false;
            SubmitStateServerRpc(currentClusterId, spawn, archetype);
        }

        public void RequestArchetype(int value)
        {
            if (!IsOwner || archetype == value)
                return;

            archetype = Mathf.Clamp(value, 0, 2);
            ApplyArchetype(archetype);
            SubmitStateServerRpc(currentClusterId, transform.position, archetype);
        }

        private void UpdateLocalMovement()
        {
            UpdatePointerTarget();

            Vector2 input = ReadKeyboardMovement();
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (input.sqrMagnitude > 0.0001f)
            {
                Vector3 delta = new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
                transform.position += delta;
                moveTarget = transform.position;
                hasMoveTarget = false;
            }
            else if (hasMoveTarget)
            {
                transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, moveTarget) <= pointerStopDistance)
                    hasMoveTarget = false;
            }

            transform.position = ClampToClusterBounds(transform.position);

            if (Time.unscaledTime >= nextSendTime)
            {
                nextSendTime = Time.unscaledTime + sendInterval;
                SubmitStateServerRpc(currentClusterId, transform.position, archetype);
            }
        }

        private void UpdatePointerTarget()
        {
            if (avatarCamera == null)
                avatarCamera = Camera.main;

            if (avatarCamera == null)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                moveTarget = ScreenToWorld(Mouse.current.position.ReadValue());
                hasMoveTarget = true;
            }

            if (Touchscreen.current != null)
            {
                foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed)
                        continue;

                    moveTarget = ScreenToWorld(touch.position.ReadValue());
                    hasMoveTarget = true;
                    break;
                }
            }
#endif
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 world = avatarCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 9f));
            world.z = AvatarZ;
            return ClampToClusterBounds(world);
        }

        private static Vector2 ReadKeyboardMovement()
        {
            float x = 0f;
            float y = 0f;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    y += 1f;
            }
#endif

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

            Color color = IsOwner ? new Color(0.02f, 1f, 0.70f, 1f) : new Color(0.18f, 0.58f, 1f, 1f);
            cachedRenderer.material = ClusterVisuals.CreateBrightMaterial(color);
            ApplyArchetype(archetype);
        }

        private void SetVisible(bool visible)
        {
            if (cachedRenderer != null && cachedRenderer.enabled != visible)
                cachedRenderer.enabled = visible;
        }

        [ServerRpc]
        private void SubmitStateServerRpc(string clusterId, Vector3 position, int avatarArchetype)
        {
            Vector3 clamped = ClampToClusterBounds(position);
            currentClusterId = string.IsNullOrWhiteSpace(clusterId) ? ClusterService.ElysiumId : clusterId;
            archetype = Mathf.Clamp(avatarArchetype, 0, 2);
            transform.position = clamped;
            ApplyStateObserversRpc(currentClusterId, clamped, archetype);
        }

        [ObserversRpc(IncludeOwner = false, BufferLast = true)]
        private void ApplyStateObserversRpc(string clusterId, Vector3 position, int avatarArchetype)
        {
            currentClusterId = string.IsNullOrWhiteSpace(clusterId) ? ClusterService.ElysiumId : clusterId;
            targetPosition = ClampToClusterBounds(position);
            archetype = Mathf.Clamp(avatarArchetype, 0, 2);
            ApplyArchetype(archetype);
        }

        private void ApplyArchetype(int value)
        {
            if (cachedMeshFilter == null)
                return;

            if (value == 1 || value == 2)
            {
                cachedMeshFilter.mesh = CreateTriangleMesh();
                transform.localScale = new Vector3(0.92f, 0.92f, 0.12f);
                if (cachedRenderer != null)
                    cachedRenderer.material = ClusterVisuals.CreateBrightMaterial(value == 1 ? new Color(0.05f, 0.55f, 1f, 1f) : new Color(1f, 0.18f, 0.62f, 1f));
            }
        }

        private static Mesh CreateTriangleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(0f, 0.75f, 0f), new Vector3(-0.64f, -0.48f, 0f), new Vector3(0.64f, -0.48f, 0f) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 ClampToClusterBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -9f, 9f);
            position.y = Mathf.Clamp(position.y, -4.8f, 4.8f);
            position.z = AvatarZ;
            return position;
        }

        private static Vector3 GetClusterSpawn(string clusterId)
        {
            return clusterId == ClusterService.SlumsId ? new Vector3(-7.4f, -3.5f, AvatarZ) : new Vector3(-7.4f, 3.4f, AvatarZ);
        }
    }
}
