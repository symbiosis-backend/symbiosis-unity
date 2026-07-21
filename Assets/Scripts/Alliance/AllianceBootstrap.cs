using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class AllianceBootstrap
    {
        private static readonly string[] AllianceSceneNames =
        {
            "Main",
            "Alliance"
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
            if (!ShouldShowAllianceInScene(nextScene.name))
                DestroySceneAllianceUi();
        }

        private static void OnSceneTransitionStarted(string targetSceneName)
        {
            if (ShouldShowAllianceInScene(targetSceneName))
                return;

            HideSceneAllianceUi();
        }

        public static void EnsureForCurrentScene()
        {
            EnsureService();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureService()
        {
            if (AllianceService.I != null)
            {
                PersistentObjectUtility.DontDestroyOnLoad(AllianceService.I.gameObject);
                return;
            }

            GameObject service = new GameObject("AllianceService");
            service.AddComponent<AllianceService>();
            PersistentObjectUtility.DontDestroyOnLoad(service);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsSceneReady(scene))
                return;

            if (!ShouldShowAllianceInScene(scene.name))
            {
                DestroySceneAllianceUi();
                return;
            }

            AllianceUI existing = Object.FindAnyObjectByType<AllianceUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.transform.SetAsLastSibling();
                existing.LayoutToggleButton();
                return;
            }

            AllianceUI.CreateInScene();
        }

        private static bool IsSceneReady(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private static void DestroySceneAllianceUi()
        {
            AllianceUI[] all = Object.FindObjectsByType<AllianceUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                AllianceUI ui = all[i];
                if (ui == null)
                    continue;

                SafeDestroyRuntimeUi(ui.gameObject);
            }
        }

        private static void HideSceneAllianceUi()
        {
            AllianceUI[] all = Object.FindObjectsByType<AllianceUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                AllianceUI ui = all[i];
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

        private static bool ShouldShowAllianceInScene(string sceneName)
        {
            for (int i = 0; i < AllianceSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, AllianceSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
