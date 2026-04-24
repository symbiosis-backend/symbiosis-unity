using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class ClusterLocalAvatar : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float pointerStopDistance = 0.12f;

        public static ClusterLocalAvatar LocalAvatar { get; private set; }

        private const float AvatarZ = -1f;

        private Camera avatarCamera;
        private MeshFilter meshFilter;
        private Renderer cachedRenderer;
        private Vector3 moveTarget;
        private bool hasMoveTarget;
        private int archetype;

        public static ClusterLocalAvatar Create(string clusterId)
        {
            GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            avatar.name = "ClusterLocalAvatar";
            avatar.transform.position = GetClusterSpawn(clusterId);
            avatar.transform.localScale = new Vector3(0.72f, 0.72f, 0.12f);

            Collider collider = avatar.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            ClusterLocalAvatar component = avatar.AddComponent<ClusterLocalAvatar>();
            component.moveTarget = avatar.transform.position;
            component.ApplyMaterial(new Color(0.02f, 1f, 0.70f, 1f));
            return component;
        }

        private void Awake()
        {
            if (LocalAvatar != null && LocalAvatar != this)
                Destroy(LocalAvatar.gameObject);

            LocalAvatar = this;
            meshFilter = GetComponent<MeshFilter>();
            cachedRenderer = GetComponent<Renderer>();
            moveTarget = transform.position;
        }

        private void OnDestroy()
        {
            if (LocalAvatar == this)
                LocalAvatar = null;
        }

        private void Update()
        {
            UpdatePointerTarget();
            UpdateKeyboardMovement();
            UpdateCamera();
        }

        public void SetArchetype(int value)
        {
            if (archetype == value)
                return;

            archetype = Mathf.Clamp(value, 0, 2);
            if (archetype == 1 || archetype == 2)
            {
                if (meshFilter != null)
                    meshFilter.mesh = CreateTriangleMesh();

                transform.localScale = new Vector3(0.92f, 0.92f, 0.12f);
                ApplyMaterial(archetype == 1 ? new Color(0.05f, 0.55f, 1f, 1f) : new Color(1f, 0.18f, 0.62f, 1f));
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

            if (hasMoveTarget)
            {
                transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, moveTarget) <= pointerStopDistance)
                    hasMoveTarget = false;
            }
        }

        private void UpdateKeyboardMovement()
        {
            Vector2 input = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    input.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    input.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    input.y += 1f;
            }
#endif

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (input.sqrMagnitude <= 0.0001f)
                return;

            transform.position += new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
            transform.position = ClampToClusterBounds(transform.position);
            moveTarget = transform.position;
            hasMoveTarget = false;
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

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 world = avatarCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 9f));
            world.z = AvatarZ;
            return ClampToClusterBounds(world);
        }

        private void ApplyMaterial(Color color)
        {
            if (cachedRenderer == null)
                cachedRenderer = GetComponent<Renderer>();

            if (cachedRenderer != null)
                cachedRenderer.material = ClusterVisuals.CreateBrightMaterial(color);
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
