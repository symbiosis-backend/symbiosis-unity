using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.Clusters
{
    public static class ClusterMainMenuBootstrap
    {
        private const string MainSceneName = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainSceneName)
                return;

            if (Object.FindAnyObjectByType<ClusterMainMenuButton>(FindObjectsInactive.Include) != null)
                return;

            ClusterMainMenuButton.CreateInScene();
        }
    }
}
