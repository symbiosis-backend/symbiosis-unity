using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class FriendsBootstrap
    {
        private static readonly string[] FriendsSceneNames =
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
            if (!ShouldShowFriendsInScene(nextScene.name))
                DestroySceneFriendsUi();
        }

        private static void OnSceneTransitionStarted(string targetSceneName)
        {
            if (ShouldShowFriendsInScene(targetSceneName))
                return;

            HideSceneFriendsUi();
        }

        public static void EnsureForCurrentScene()
        {
            EnsureService();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureService()
        {
            if (FriendsService.I != null)
            {
                PersistentObjectUtility.DontDestroyOnLoad(FriendsService.I.gameObject);
                return;
            }

            GameObject service = new GameObject("FriendsService");
            service.AddComponent<FriendsService>();
            PersistentObjectUtility.DontDestroyOnLoad(service);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsSceneReady(scene))
                return;

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

        private static bool IsSceneReady(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private static void DestroySceneFriendsUi()
        {
            FriendsUI[] all = Object.FindObjectsByType<FriendsUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                FriendsUI ui = all[i];
                if (ui == null)
                    continue;

                SafeDestroyRuntimeUi(ui.gameObject);
            }
        }

        private static void HideSceneFriendsUi()
        {
            FriendsUI[] all = Object.FindObjectsByType<FriendsUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                FriendsUI ui = all[i];
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
