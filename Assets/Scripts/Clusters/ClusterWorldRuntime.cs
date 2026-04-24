using System.Collections;
using MahjongGame.Multiplayer;
using UnityEngine;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class ClusterWorldRuntime : MonoBehaviour
    {
        [SerializeField] private string clusterId = ClusterService.ElysiumId;
        [SerializeField] private string exitClusterId = ClusterService.SlumsId;

        private Camera worldCamera;
        private Vector3 maleTriangle;
        private Vector3 femaleTriangle;
        private Vector3 portalPosition;

        private const float InteractableZ = -0.85f;
        private const float GroundZ = 1.2f;
        private const float ArchetypeRadius = 0.8f;
        private const float PortalRadius = 0.9f;

        public void Configure(string id, string exitId)
        {
            clusterId = id;
            exitClusterId = exitId;
        }

        private void Start()
        {
            EnsureCamera();
            BuildWorld();
            StartCoroutine(ConnectWhenBootstrapReady());
            StartCoroutine(RegisterLocalAvatarWhenReady());
        }

        private void Update()
        {
            MatrixNetworkAvatar avatar = MatrixNetworkAvatar.LocalAvatar;
            Transform avatarTransform = avatar != null ? avatar.transform : ClusterLocalAvatar.LocalAvatar != null ? ClusterLocalAvatar.LocalAvatar.transform : null;
            if (avatarTransform == null)
                return;

            Vector2 avatarPosition = avatarTransform.position;
            if (Vector2.Distance(avatarPosition, maleTriangle) <= ArchetypeRadius)
                ApplyArchetype(1);
            else if (Vector2.Distance(avatarPosition, femaleTriangle) <= ArchetypeRadius)
                ApplyArchetype(2);

            if (Vector2.Distance(avatarPosition, portalPosition) <= PortalRadius)
                ClusterService.LoadCluster(exitClusterId);
        }

        private IEnumerator ConnectWhenBootstrapReady()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (RealtimeNetworkBootstrap.I == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (RealtimeNetworkBootstrap.I == null)
                yield break;

#if UNITY_EDITOR
            yield break;
#else
            if (!RealtimeNetworkBootstrap.I.IsClientStarted)
                RealtimeNetworkBootstrap.I.StartClient();
#endif
        }

        private IEnumerator RegisterLocalAvatarWhenReady()
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (MatrixNetworkAvatar.LocalAvatar == null && Time.realtimeSinceStartup < deadline)
            {
#if UNITY_EDITOR
                if (Time.realtimeSinceStartup >= deadline - 10.5f)
                    break;
#endif
                yield return null;
            }

            if (MatrixNetworkAvatar.LocalAvatar != null)
            {
                MatrixNetworkAvatar.LocalAvatar.EnterCluster(clusterId);
                yield break;
            }

            if (ClusterLocalAvatar.LocalAvatar == null)
                ClusterLocalAvatar.Create(clusterId);
        }

        private static void ApplyArchetype(int archetype)
        {
            if (MatrixNetworkAvatar.LocalAvatar != null)
                MatrixNetworkAvatar.LocalAvatar.RequestArchetype(archetype);
            else if (ClusterLocalAvatar.LocalAvatar != null)
                ClusterLocalAvatar.LocalAvatar.SetArchetype(archetype);
        }

        private void EnsureCamera()
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.GetComponent<Camera>();
            }

            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = IsElysium ? new Color(0.64f, 0.84f, 0.78f, 1f) : new Color(0.72f, 0.66f, 0.58f, 1f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 6f;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void BuildWorld()
        {
            if (GameObject.Find("ClusterWorldRuntimeObjects") != null)
                return;

            GameObject root = new GameObject("ClusterWorldRuntimeObjects");
            BuildGround(root.transform);
            BuildAmbientObjects(root.transform);

            maleTriangle = new Vector3(-2.1f, 2.1f, InteractableZ);
            femaleTriangle = new Vector3(2.1f, 2.1f, InteractableZ);
            portalPosition = new Vector3(7.6f, -3.8f, InteractableZ);

            CreateTriangle(root.transform, "MaleTriangle", maleTriangle, new Color(0.05f, 0.55f, 1f, 1f), 1.05f);
            CreateTriangle(root.transform, "FemaleTriangle", femaleTriangle, new Color(1f, 0.18f, 0.62f, 1f), 1.05f);
            CreatePortal(root.transform, portalPosition);
        }

        private void BuildGround(Transform root)
        {
            Color groundColor = IsElysium ? new Color(0.55f, 0.78f, 0.50f, 1f) : new Color(0.62f, 0.54f, 0.45f, 1f);
            CreateQuad(root, "Ground", new Vector3(0f, 0f, GroundZ), new Vector3(18.5f, 10.5f, 0.08f), groundColor);

            if (IsElysium)
            {
                CreateQuad(root, "WaterRibbon", new Vector3(-5.2f, -1.3f, 0.85f), new Vector3(2.2f, 8.6f, 0.08f), new Color(0.24f, 0.70f, 0.82f, 0.95f));
                CreateQuad(root, "SoftPath", new Vector3(1.2f, -0.7f, 0.8f), new Vector3(12.5f, 1.15f, 0.08f), new Color(0.77f, 0.69f, 0.45f, 1f));
            }
            else
            {
                CreateQuad(root, "BrokenRoad", new Vector3(0f, -0.8f, 0.8f), new Vector3(16f, 1.35f, 0.08f), new Color(0.36f, 0.35f, 0.34f, 1f));
                CreateQuad(root, "RustPatch", new Vector3(-4.5f, 2.2f, 0.82f), new Vector3(3.4f, 2f, 0.08f), new Color(0.70f, 0.34f, 0.18f, 1f));
            }
        }

        private void BuildAmbientObjects(Transform root)
        {
            if (IsElysium)
            {
                CreateCircle(root, "TreeA", new Vector3(-7f, 3.3f, 0.35f), 0.65f, new Color(0.07f, 0.50f, 0.18f, 1f));
                CreateCircle(root, "TreeB", new Vector3(6.1f, 2.8f, 0.35f), 0.75f, new Color(0.08f, 0.58f, 0.22f, 1f));
                CreateCircle(root, "StoneA", new Vector3(-1f, -3f, 0.35f), 0.38f, new Color(0.72f, 0.76f, 0.73f, 1f));
                CreateCircle(root, "BushA", new Vector3(4.6f, -2.3f, 0.35f), 0.45f, new Color(0.09f, 0.66f, 0.25f, 1f));
            }
            else
            {
                CreateQuad(root, "ContainerA", new Vector3(-6.8f, 3.2f, 0.35f), new Vector3(1.8f, 0.9f, 0.08f), new Color(0.72f, 0.28f, 0.16f, 1f));
                CreateQuad(root, "WallA", new Vector3(5.6f, 3.1f, 0.35f), new Vector3(2.6f, 0.75f, 0.08f), new Color(0.52f, 0.50f, 0.47f, 1f));
                CreateQuad(root, "CrateA", new Vector3(-1.2f, -3.1f, 0.35f), new Vector3(0.9f, 0.9f, 0.08f), new Color(0.66f, 0.48f, 0.28f, 1f));
                CreateCircle(root, "LampGlow", new Vector3(3.6f, -2.9f, 0.2f), 0.34f, new Color(1f, 0.83f, 0.30f, 1f));
            }
        }

        private void CreatePortal(Transform root, Vector3 position)
        {
            GameObject portal = new GameObject(GetPortalObjectName());
            portal.transform.SetParent(root, false);
            portal.transform.position = position;

            SphereCollider trigger = portal.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = PortalRadius;

            CreateCircle(portal.transform, "Glow", Vector3.zero, 0.82f, new Color(0.20f, 1f, 0.78f, 1f));
            CreateTriangle(portal.transform, "Arrow", new Vector3(0f, 0.12f, -0.05f), new Color(0.02f, 0.22f, 0.16f, 1f), 0.52f);
        }

        private static void CreateQuad(Transform root, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = name;
            quad.transform.SetParent(root, false);
            quad.transform.localPosition = position;
            quad.transform.localScale = scale;
            Destroy(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().material = ClusterVisuals.CreateBrightMaterial(color);
        }

        private static void CreateCircle(Transform root, string name, Vector3 position, float radius, Color color)
        {
            GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            circle.name = name;
            circle.transform.SetParent(root, false);
            circle.transform.localPosition = position;
            circle.transform.localScale = new Vector3(radius, radius, 0.08f);
            Destroy(circle.GetComponent<Collider>());
            circle.GetComponent<Renderer>().material = ClusterVisuals.CreateBrightMaterial(color);
        }

        private static void CreateTriangle(Transform root, string name, Vector3 position, Color color, float size)
        {
            GameObject triangle = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            triangle.transform.SetParent(root, false);
            triangle.transform.localPosition = position;
            triangle.transform.localScale = Vector3.one * size;
            triangle.GetComponent<MeshFilter>().mesh = CreateTriangleMesh();
            triangle.GetComponent<MeshRenderer>().material = ClusterVisuals.CreateBrightMaterial(color);
        }

        private static Mesh CreateTriangleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(0f, 0.7f, 0f), new Vector3(-0.62f, -0.45f, 0f), new Vector3(0.62f, -0.45f, 0f) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private bool IsElysium => clusterId == ClusterService.ElysiumId;

        private string GetPortalObjectName()
        {
            return exitClusterId == ClusterService.SlumsId ? "PortaToSlums" : "PortaToElysium";
        }
    }
}
