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
            if (avatar == null)
                return;

            if (Vector3.Distance(avatar.transform.position, maleTriangle) <= 0.8f)
                avatar.RequestArchetype(1);
            else if (Vector3.Distance(avatar.transform.position, femaleTriangle) <= 0.8f)
                avatar.RequestArchetype(2);

            if (Vector3.Distance(avatar.transform.position, portalPosition) <= 0.9f)
                ClusterService.LoadCluster(exitClusterId);
        }

        private IEnumerator ConnectWhenBootstrapReady()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (RealtimeNetworkBootstrap.I == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (RealtimeNetworkBootstrap.I != null && !RealtimeNetworkBootstrap.I.IsClientStarted)
                RealtimeNetworkBootstrap.I.StartClient();
        }

        private IEnumerator RegisterLocalAvatarWhenReady()
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (MatrixNetworkAvatar.LocalAvatar == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            MatrixNetworkAvatar.LocalAvatar?.EnterCluster(clusterId);
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
            worldCamera.backgroundColor = IsElysium ? new Color(0.05f, 0.11f, 0.10f, 1f) : new Color(0.12f, 0.08f, 0.07f, 1f);
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

            maleTriangle = new Vector3(-2.1f, 2.1f, 0f);
            femaleTriangle = new Vector3(2.1f, 2.1f, 0f);
            portalPosition = new Vector3(7.6f, -3.8f, 0f);

            CreateTriangle(root.transform, "MaleTriangle", maleTriangle, new Color(0.35f, 0.68f, 1f, 1f), 0.85f);
            CreateTriangle(root.transform, "FemaleTriangle", femaleTriangle, new Color(1f, 0.45f, 0.72f, 1f), 0.85f);
            CreatePortal(root.transform, portalPosition);
        }

        private void BuildGround(Transform root)
        {
            Color groundColor = IsElysium ? new Color(0.17f, 0.35f, 0.23f, 1f) : new Color(0.30f, 0.24f, 0.20f, 1f);
            CreateQuad(root, "Ground", Vector3.zero, new Vector3(18.5f, 10.5f, 1f), groundColor);

            if (IsElysium)
            {
                CreateQuad(root, "WaterRibbon", new Vector3(-5.2f, -1.3f, -0.05f), new Vector3(2.2f, 8.6f, 1f), new Color(0.10f, 0.39f, 0.45f, 0.9f));
                CreateQuad(root, "SoftPath", new Vector3(1.2f, -0.7f, -0.04f), new Vector3(12.5f, 1.15f, 1f), new Color(0.44f, 0.38f, 0.24f, 0.95f));
            }
            else
            {
                CreateQuad(root, "BrokenRoad", new Vector3(0f, -0.8f, -0.04f), new Vector3(16f, 1.35f, 1f), new Color(0.16f, 0.15f, 0.15f, 1f));
                CreateQuad(root, "RustPatch", new Vector3(-4.5f, 2.2f, -0.03f), new Vector3(3.4f, 2f, 1f), new Color(0.42f, 0.20f, 0.12f, 1f));
            }
        }

        private void BuildAmbientObjects(Transform root)
        {
            if (IsElysium)
            {
                CreateCircle(root, "TreeA", new Vector3(-7f, 3.3f, 0f), 0.65f, new Color(0.08f, 0.42f, 0.18f, 1f));
                CreateCircle(root, "TreeB", new Vector3(6.1f, 2.8f, 0f), 0.75f, new Color(0.10f, 0.48f, 0.22f, 1f));
                CreateCircle(root, "StoneA", new Vector3(-1f, -3f, 0f), 0.38f, new Color(0.48f, 0.52f, 0.50f, 1f));
                CreateCircle(root, "BushA", new Vector3(4.6f, -2.3f, 0f), 0.45f, new Color(0.13f, 0.55f, 0.25f, 1f));
            }
            else
            {
                CreateQuad(root, "ContainerA", new Vector3(-6.8f, 3.2f, 0f), new Vector3(1.8f, 0.9f, 1f), new Color(0.52f, 0.22f, 0.13f, 1f));
                CreateQuad(root, "WallA", new Vector3(5.6f, 3.1f, 0f), new Vector3(2.6f, 0.75f, 1f), new Color(0.27f, 0.25f, 0.24f, 1f));
                CreateQuad(root, "CrateA", new Vector3(-1.2f, -3.1f, 0f), new Vector3(0.9f, 0.9f, 1f), new Color(0.45f, 0.33f, 0.20f, 1f));
                CreateCircle(root, "LampGlow", new Vector3(3.6f, -2.9f, 0f), 0.34f, new Color(0.95f, 0.70f, 0.30f, 1f));
            }
        }

        private void CreatePortal(Transform root, Vector3 position)
        {
            CreateCircle(root, "ClusterPortalGlow", position, 0.72f, new Color(0.35f, 1f, 0.78f, 0.95f));
            CreateTriangle(root, "ClusterPortalArrow", position + new Vector3(0f, 0.12f, -0.05f), new Color(0.05f, 0.18f, 0.16f, 1f), 0.46f);
        }

        private static void CreateQuad(Transform root, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = name;
            quad.transform.SetParent(root, false);
            quad.transform.position = position;
            quad.transform.localScale = scale;
            Destroy(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().material.color = color;
        }

        private static void CreateCircle(Transform root, string name, Vector3 position, float radius, Color color)
        {
            GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            circle.name = name;
            circle.transform.SetParent(root, false);
            circle.transform.position = position;
            circle.transform.localScale = new Vector3(radius, radius, 0.08f);
            Destroy(circle.GetComponent<Collider>());
            circle.GetComponent<Renderer>().material.color = color;
        }

        private static void CreateTriangle(Transform root, string name, Vector3 position, Color color, float size)
        {
            GameObject triangle = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            triangle.transform.SetParent(root, false);
            triangle.transform.position = position;
            triangle.transform.localScale = Vector3.one * size;
            triangle.GetComponent<MeshFilter>().mesh = CreateTriangleMesh();
            triangle.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color };
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
    }
}
