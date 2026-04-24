using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class MainShopBootstrap
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

            EnsureCurrencyService();

            if (Object.FindAnyObjectByType<MainShopUI>(FindObjectsInactive.Include) != null)
                return;

            MainShopUI.CreateInScene();
        }

        private static void EnsureCurrencyService()
        {
            if (CurrencyService.I != null)
                return;

            GameObject serviceObject = new GameObject("CurrencyService");
            serviceObject.AddComponent<CurrencyService>();
        }
    }
}
