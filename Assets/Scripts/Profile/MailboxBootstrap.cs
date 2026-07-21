using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class MailboxBootstrap
    {
        private static readonly string[] MailboxSceneNames =
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
            if (!ShouldShowInScene(nextScene.name))
                DestroySceneUi();
        }

        private static void OnSceneTransitionStarted(string targetSceneName)
        {
            if (ShouldShowInScene(targetSceneName))
                return;

            HideSceneUi();
        }

        public static void EnsureForCurrentScene()
        {
            EnsureService();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureService()
        {
            if (MailboxService.I != null)
            {
                PersistentObjectUtility.DontDestroyOnLoad(MailboxService.I.gameObject);
                return;
            }

            GameObject service = new GameObject("MailboxService");
            service.AddComponent<MailboxService>();
            PersistentObjectUtility.DontDestroyOnLoad(service);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsSceneReady(scene))
                return;

            if (!ShouldShowInScene(scene.name))
            {
                DestroySceneUi();
                return;
            }

            MailboxUI existing = FindSceneUi(scene);
            if (existing != null)
            {
                DestroyDuplicateSceneUi(existing);
                existing.gameObject.SetActive(true);
                existing.RepairVisualHierarchy();
                existing.transform.SetAsLastSibling();
                existing.LayoutToggleButton();
                return;
            }

            MailboxUI created = MailboxUI.CreateInScene();
            if (created != null && created.gameObject.scene != scene)
                SceneManager.MoveGameObjectToScene(created.gameObject, scene);
            created?.RepairVisualHierarchy();
            DestroyDuplicateSceneUi(created);
        }

        private static MailboxUI FindSceneUi(Scene scene)
        {
            MailboxUI[] all = Object.FindObjectsByType<MailboxUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MailboxUI ui = all[i];
                if (ui != null && ui.gameObject.scene == scene)
                    return ui;
            }

            return null;
        }

        private static bool IsSceneReady(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && scene == SceneManager.GetActiveScene();
        }

        private static void DestroySceneUi()
        {
            MailboxUI[] all = Object.FindObjectsByType<MailboxUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MailboxUI ui = all[i];
                if (ui == null)
                    continue;

                SafeDestroyRuntimeUi(ui.gameObject);
            }
        }

        private static void HideSceneUi()
        {
            MailboxUI[] all = Object.FindObjectsByType<MailboxUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MailboxUI ui = all[i];
                if (ui != null)
                    ui.gameObject.SetActive(false);
            }
        }

        private static void DestroyDuplicateSceneUi(MailboxUI keep)
        {
            MailboxUI[] all = Object.FindObjectsByType<MailboxUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MailboxUI ui = all[i];
                if (ui != null && ui != keep)
                    SafeDestroyRuntimeUi(ui.gameObject);
            }

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !string.Equals(button.name, "MailboxButton", System.StringComparison.Ordinal))
                    continue;

                if (keep != null && button.transform.IsChildOf(keep.transform))
                    continue;

                SafeDestroyRuntimeUi(button.gameObject);
            }
        }

        private static void SafeDestroyRuntimeUi(GameObject obj)
        {
            if (obj == null)
                return;

            obj.SetActive(false);
            Object.Destroy(obj);
        }

        private static bool ShouldShowInScene(string sceneName)
        {
            for (int i = 0; i < MailboxSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, MailboxSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
