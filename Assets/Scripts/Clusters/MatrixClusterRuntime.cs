using System.Collections;
using MahjongGame.Multiplayer;
using UnityEngine;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class MatrixClusterRuntime : MonoBehaviour
    {
        private void Start()
        {
            EnsureCamera();
            EnsureGrid();
            StartCoroutine(ConnectWhenBootstrapReady());
        }

        private static IEnumerator ConnectWhenBootstrapReady()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (RealtimeNetworkBootstrap.I == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (RealtimeNetworkBootstrap.I != null && !RealtimeNetworkBootstrap.I.IsClientStarted)
                RealtimeNetworkBootstrap.I.StartClient();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
                return;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.035f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureGrid()
        {
            if (GameObject.Find("MatrixRuntimeGrid") != null)
                return;

            GameObject gridRoot = new GameObject("MatrixRuntimeGrid");
            for (int i = -9; i <= 9; i++)
                CreateLine(gridRoot.transform, new Vector3(i, -5f, 0.5f), new Vector3(i, 5f, 0.5f));

            for (int i = -5; i <= 5; i++)
                CreateLine(gridRoot.transform, new Vector3(-9f, i, 0.5f), new Vector3(9f, i, 0.5f));
        }

        private static void CreateLine(Transform parent, Vector3 start, Vector3 end)
        {
            GameObject lineObject = new GameObject("GridLine", typeof(LineRenderer));
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.018f;
            line.endWidth = 0.018f;
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.12f, 0.9f, 0.66f, 0.35f);
            line.endColor = line.startColor;
        }
    }
}
