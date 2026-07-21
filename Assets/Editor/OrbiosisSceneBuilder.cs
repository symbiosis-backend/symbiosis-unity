using MahjongGame.Orbiosis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MahjongGame.EditorTools
{
    public static class OrbiosisSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Orbiosis.unity";

        [MenuItem("Dynasty/Orbiosis/Rebuild Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Orbiosis";

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.010f, 0.028f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif

            GameObject root = new GameObject("OrbiosisRoot");
            root.AddComponent<OrbiosisBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[OrbiosisSceneBuilder] Rebuilt " + ScenePath);
        }

        [MenuItem("Dynasty/Orbiosis/Rebuild Editable UI Hierarchy")]
        public static void RebuildEditableUiHierarchy()
        {
            OrbiosisBootstrap bootstrap = Object.FindAnyObjectByType<OrbiosisBootstrap>();
            if (bootstrap == null)
            {
                GameObject root = GameObject.Find("OrbiosisRoot");
                if (root == null)
                    root = new GameObject("OrbiosisRoot");

                bootstrap = root.GetComponent<OrbiosisBootstrap>();
                if (bootstrap == null)
                    bootstrap = root.AddComponent<OrbiosisBootstrap>();
            }

            bootstrap.RebuildEditableUiHierarchy();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            Debug.Log("[OrbiosisSceneBuilder] Rebuilt editable UI hierarchy under OrbiosisRoot.");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                updated[i] = scenes[i];

            updated[updated.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
