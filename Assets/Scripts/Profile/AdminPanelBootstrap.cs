using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class AdminPanelBootstrap
    {
        private static readonly string[] AdminSceneNames =
        {
            "Main"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ProfileService.ProfileChanged -= OnProfileChanged;
            ProfileService.ProfileChanged += OnProfileChanged;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void OnProfileChanged()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!ShouldShowInScene(scene.name) || !AdminPanelUI.IsOwnerProfile())
            {
                DestroySceneUi();
                return;
            }

            AdminPanelUI existing = Object.FindAnyObjectByType<AdminPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.transform.SetAsLastSibling();
                existing.RefreshOwnerVisibility();
                return;
            }

            AdminPanelUI.CreateInScene();
        }

        private static void DestroySceneUi()
        {
            AdminPanelUI ui = Object.FindAnyObjectByType<AdminPanelUI>(FindObjectsInactive.Include);
            if (ui != null)
                Object.Destroy(ui.gameObject);
        }

        private static bool ShouldShowInScene(string sceneName)
        {
            for (int i = 0; i < AdminSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, AdminSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
