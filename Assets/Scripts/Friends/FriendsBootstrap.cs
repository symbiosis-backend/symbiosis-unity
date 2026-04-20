using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class FriendsBootstrap
    {
        private static readonly string[] FriendsSceneNames =
        {
            "Main",
            "LobbyMahjong",
            "LobbyMahjongBattle"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            EnsureService();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureService();
            EnsureForScene(scene);
        }

        public static void EnsureForCurrentScene()
        {
            EnsureService();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureService()
        {
            if (FriendsService.I != null)
                return;

            GameObject service = new GameObject("FriendsService");
            service.AddComponent<FriendsService>();
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!ShouldShowFriendsInScene(scene.name))
                return;

            if (Object.FindAnyObjectByType<FriendsUI>(FindObjectsInactive.Include) != null)
                return;

            FriendsUI.CreateInScene();
        }

        private static bool ShouldShowFriendsInScene(string sceneName)
        {
            for (int i = 0; i < FriendsSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, FriendsSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
