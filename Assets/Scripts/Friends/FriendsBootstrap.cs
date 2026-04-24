using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class FriendsBootstrap
    {
        private static readonly string[] FriendsSceneNames =
        {
            "Main",
            "LobbyMahjong"
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
            {
                DestroySceneFriendsUi();
                return;
            }

            FriendsUI existing = Object.FindAnyObjectByType<FriendsUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.transform.SetAsLastSibling();
                existing.LayoutToggleButton();
                return;
            }

            FriendsUI.CreateInScene();
        }

        private static void DestroySceneFriendsUi()
        {
            FriendsUI ui = Object.FindAnyObjectByType<FriendsUI>(FindObjectsInactive.Include);
            if (ui != null)
                Object.Destroy(ui.gameObject);
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
