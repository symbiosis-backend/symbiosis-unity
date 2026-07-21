using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class SceneOrientationPolicy
    {
        private const string EntrySceneName = "Entry";
        private const string MainSceneName = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForScene(scene);
        }

        private static void ApplyForScene(Scene scene)
        {
#if UNITY_IOS
            ApplyLandscapeOnly();
            return;
#endif
            if (IsLandscapeOnlyScene(scene.name))
            {
                ApplyLandscapeOnly();
                return;
            }

            if (IsPortraitOnlyScene(scene.name))
                ApplyPortraitOnly();
        }

        public static void ApplyPortraitOnly()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        public static void ApplyLandscapeOnly()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            if (Screen.orientation == ScreenOrientation.Portrait
                || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }

            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private static bool IsLandscapeOnlyScene(string sceneName)
        {
            return string.Equals(sceneName, EntrySceneName, System.StringComparison.Ordinal)
                || string.Equals(sceneName, MainSceneName, System.StringComparison.Ordinal)
                || string.Equals(sceneName, "SymbiozFlagship", System.StringComparison.Ordinal);
        }

        private static bool IsPortraitOnlyScene(string sceneName)
        {
            return string.Equals(sceneName, "Orbiosis", System.StringComparison.Ordinal)
                || string.Equals(sceneName, "SymbiGrid", System.StringComparison.Ordinal);
        }
    }
}
