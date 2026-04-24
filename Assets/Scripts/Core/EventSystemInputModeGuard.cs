using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    public static class EventSystemInputModeGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureCompatibleEventSystems();
        }

        public static void EnsureCompatibleEventSystems()
        {
#if ENABLE_INPUT_SYSTEM
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null)
                    continue;

                StandaloneInputModule[] legacyModules = eventSystem.GetComponents<StandaloneInputModule>();
                for (int j = 0; j < legacyModules.Length; j++)
                {
                    if (legacyModules[j] == null)
                        continue;

                    legacyModules[j].enabled = false;
                    Object.Destroy(legacyModules[j]);
                }

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureCompatibleEventSystems();
        }
    }
}
