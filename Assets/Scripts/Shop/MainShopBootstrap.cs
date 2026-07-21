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

        public static void EnsureForCurrentScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!ShouldCreateShop(scene.name))
                return;

            EnsureCurrencyService();
            Monetization.MonetizationService.Ensure();
            OzAmetistShopService.EnsureCatalogRegistered();

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

        private static bool ShouldCreateShop(string sceneName)
        {
            return sceneName == MainSceneName;
        }
    }
}
