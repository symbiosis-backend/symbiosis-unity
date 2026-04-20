using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class GlobalChatBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            EnsureService();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureMainMenuUi(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureService();
            EnsureMainMenuUi(scene);
        }

        private static void EnsureService()
        {
            if (GlobalChatService.I != null)
                return;

            GameObject service = new GameObject("GlobalChatService");
            service.AddComponent<GlobalChatService>();
        }

        private static void EnsureMainMenuUi(Scene scene)
        {
            if (!string.Equals(scene.name, "Main", System.StringComparison.Ordinal))
                return;

            if (Object.FindAnyObjectByType<GlobalChatUI>(FindObjectsInactive.Include) != null)
                return;

            GlobalChatUI.CreateInScene();
        }
    }
}
