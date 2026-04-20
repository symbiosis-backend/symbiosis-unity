using System.Collections.Generic;
using System.IO;
using System.Linq;
using MahjongGame.Clusters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.EditorTools
{
    public static class ClusterSceneBuilder
    {
        private const string MatrixScenePath = "Assets/Scenes/ClusterMatrix.unity";
        private const string SlumsScenePath = "Assets/Scenes/ClusterSlums.unity";

        [MenuItem("Symbiosis/Clusters/Rebuild Cluster Scenes")]
        public static void RebuildClusterScenes()
        {
            EnsureScenesFolder();
            CreateClusterScene(MatrixScenePath, ClusterService.MatrixId, ClusterService.SlumsId, new Color(0.02f, 0.06f, 0.07f, 1f));
            CreateClusterScene(SlumsScenePath, ClusterService.SlumsId, ClusterService.MatrixId, new Color(0.10f, 0.07f, 0.055f, 1f));
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClusterSceneBuilder] Cluster scenes rebuilt.");
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static void CreateClusterScene(string scenePath, string clusterId, string connectedClusterId, Color backgroundColor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            GameObject controllerObject = new GameObject("ClusterSceneController");
            ClusterSceneController controller = controllerObject.AddComponent<ClusterSceneController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("clusterId").stringValue = clusterId;
            serialized.FindProperty("primaryConnectionId").stringValue = connectedClusterId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            AddSceneIfMissing(scenes, MatrixScenePath);
            AddSceneIfMissing(scenes, SlumsScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            if (scenes.Any(scene => scene.path == scenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
    }
}
