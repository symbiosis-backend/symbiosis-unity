using Dynasty.Legacy.CorrosionCollapse.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.Editor
{
    public static class CorrosionCollapseSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/CorrosionCollapse.unity";

        [MenuItem("Dynasty/Corrosion Collapse/Setup Scene")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find("CorrosionCollapseRoot") ?? new GameObject("CorrosionCollapseRoot");
            if (root.GetComponent<CorrosionCollapseBootstrap>() == null)
            {
                root.AddComponent<CorrosionCollapseBootstrap>();
            }

            EnsureChild(root.transform, "Systems");
            EnsureChild(root.transform, "Board");
            EnsureChild(root.transform, "Players");
            Transform uiRoot = EnsureChild(root.transform, "UI");

            if (Object.FindAnyObjectByType<Canvas>() == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(uiRoot, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(root.transform, false);
            }

            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 18f, -18f);
                camera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 17f;
                camera.backgroundColor = new Color(0.22f, 0.17f, 0.13f, 1f);
            }
            else
            {
                Debug.LogWarning("[Game] Corrosion Collapse scene setup did not create a camera. Add a scene camera first.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }
    }
}
