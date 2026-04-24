using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class ProfileRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureServices();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureServices();
        }

        public static void EnsureServices()
        {
            EnsureComponent<ProfileService>("ProfileService");
            EnsureComponent<CurrencyService>("CurrencyService");
            EnsureComponent<MahjongTitleService>("MahjongTitleService");
            EnsureComponent<MahjongRewardService>("MahjongRewardService");
            TryLoadCachedProfile();
        }

        public static PlayerProfile TryGetProfile()
        {
            EnsureServices();
            return ProfileService.I != null ? ProfileService.I.Current : null;
        }

        public static void TryLoadCachedProfile()
        {
            if (ProfileService.I == null || ProfileService.I.Current != null)
                return;

            if (ProfileService.I.HasProfile())
                ProfileService.I.LoadProfile();
        }

        private static T EnsureComponent<T>(string objectName) where T : Component
        {
            T instance = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (instance != null)
            {
                if (!instance.gameObject.activeSelf)
                    instance.gameObject.SetActive(true);

                return instance;
            }

            GameObject serviceObject = new GameObject(objectName);
            return serviceObject.AddComponent<T>();
        }
    }
}
