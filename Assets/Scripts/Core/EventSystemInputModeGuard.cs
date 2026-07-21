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
        private const string RuntimeEventSystemName = "RuntimeEventSystem";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureCompatibleEventSystems();
        }

        public static void EnsureCompatibleEventSystems()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem primary = ResolvePrimaryEventSystem(eventSystems);
            if (primary == null)
                primary = CreateRuntimeEventSystem();

            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null || eventSystem.gameObject == null)
                    continue;

                if (eventSystem == primary)
                {
                    ActivatePrimary(eventSystem);
                    continue;
                }

                DeactivateDuplicate(eventSystem);
            }

            ActivatePrimary(primary);
        }

        private static EventSystem ResolvePrimaryEventSystem(EventSystem[] eventSystems)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            EventSystem current = EventSystem.current;
            EventSystem firstEnabled = null;
            EventSystem firstAny = null;

            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null || eventSystem.gameObject == null)
                    continue;

                if (eventSystem.gameObject.scene == activeScene)
                    return eventSystem;

                if (firstEnabled == null && eventSystem.isActiveAndEnabled)
                    firstEnabled = eventSystem;

                if (firstAny == null)
                    firstAny = eventSystem;
            }

            if (current != null)
                return current;

            return firstEnabled != null ? firstEnabled : firstAny;
        }

        private static EventSystem CreateRuntimeEventSystem()
        {
            GameObject eventSystemObject = new GameObject(RuntimeEventSystemName, typeof(EventSystem));
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                try
                {
                    SceneManager.MoveGameObjectToScene(eventSystemObject, activeScene);
                }
                catch (System.ArgumentException)
                {
                    Object.Destroy(eventSystemObject);
                    return null;
                }
            }
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            return eventSystemObject.GetComponent<EventSystem>();
        }

        private static void ActivatePrimary(EventSystem eventSystem)
        {
            if (eventSystem == null || eventSystem.gameObject == null)
                return;

            eventSystem.gameObject.SetActive(true);
            eventSystem.enabled = true;
            eventSystem.sendNavigationEvents = true;
            EventSystem.current = eventSystem;
            EnsureCompatibleInputModule(eventSystem);
        }

        private static void DeactivateDuplicate(EventSystem eventSystem)
        {
            if (eventSystem == null || eventSystem.gameObject == null)
                return;

            if (eventSystem.currentSelectedGameObject != null)
                eventSystem.SetSelectedGameObject(null);

            eventSystem.enabled = false;
            eventSystem.gameObject.SetActive(false);
        }

        private static void EnsureCompatibleInputModule(EventSystem eventSystem)
        {
            if (eventSystem == null)
                return;

#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule[] legacyModules = eventSystem.GetComponents<StandaloneInputModule>();
            for (int j = 0; j < legacyModules.Length; j++)
            {
                if (legacyModules[j] == null)
                    continue;

                legacyModules[j].enabled = false;
                Object.Destroy(legacyModules[j]);
            }

            InputSystemUIInputModule input = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (input == null)
                input = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            input.enabled = true;
#else
            StandaloneInputModule input = eventSystem.GetComponent<StandaloneInputModule>();
            if (input == null)
                input = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

            input.enabled = true;
#endif
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureCompatibleEventSystems();
        }
    }
}
