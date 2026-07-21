using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class GlobalChatBootstrap
    {
        private static readonly string[] ChatSceneNames =
        {
            "Main"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            EnsureService();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            DoorFx.SceneTransitionStarted -= OnSceneTransitionStarted;
            DoorFx.SceneTransitionStarted += OnSceneTransitionStarted;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureService();
            EnsureForScene(scene);
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (!ShouldShowChatInScene(nextScene.name))
                DestroySceneChatUi();
        }

        private static void OnSceneTransitionStarted(string targetSceneName)
        {
            if (ShouldShowChatInScene(targetSceneName))
                return;

            HideSceneChatUi();
        }

        public static void EnsureForCurrentScene()
        {
            EnsureService();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureService()
        {
            if (GlobalChatService.I != null)
            {
                PersistentObjectUtility.DontDestroyOnLoad(GlobalChatService.I.gameObject);
                return;
            }

            GameObject service = new GameObject("GlobalChatService");
            service.AddComponent<GlobalChatService>();
            PersistentObjectUtility.DontDestroyOnLoad(service);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsSceneReady(scene))
                return;

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

        private static bool IsSceneReady(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private static void DestroySceneChatUi()
        {
            GlobalChatUI[] all = Object.FindObjectsByType<GlobalChatUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                GlobalChatUI ui = all[i];
                if (ui == null)
                    continue;

                SafeDestroyRuntimeUi(ui.gameObject);
            }
        }

        private static void HideSceneChatUi()
        {
            GlobalChatUI[] all = Object.FindObjectsByType<GlobalChatUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                GlobalChatUI ui = all[i];
                if (ui != null)
                    ui.gameObject.SetActive(false);
            }
        }

        private static void SafeDestroyRuntimeUi(GameObject obj)
        {
            if (obj == null)
                return;

            obj.SetActive(false);
            Object.Destroy(obj);
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
