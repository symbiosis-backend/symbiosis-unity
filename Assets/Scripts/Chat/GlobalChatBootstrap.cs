using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class GlobalChatBootstrap
    {
        private static readonly string[] ChatSceneNames =
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
            if (GlobalChatService.I != null)
                return;

            GameObject service = new GameObject("GlobalChatService");
            service.AddComponent<GlobalChatService>();
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!ShouldShowChatInScene(scene.name))
            {
                DestroySceneChatUi();
                return;
            }

            GlobalChatUI existing = Object.FindAnyObjectByType<GlobalChatUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.transform.SetAsLastSibling();
                existing.LayoutToggleButton();
                return;
            }

            GlobalChatUI.CreateInScene();
        }

        private static void DestroySceneChatUi()
        {
            GlobalChatUI ui = Object.FindAnyObjectByType<GlobalChatUI>(FindObjectsInactive.Include);
            if (ui != null)
                Object.Destroy(ui.gameObject);
        }

        private static bool ShouldShowChatInScene(string sceneName)
        {
            for (int i = 0; i < ChatSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, ChatSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
