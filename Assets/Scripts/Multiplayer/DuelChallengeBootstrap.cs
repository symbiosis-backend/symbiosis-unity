using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class DuelChallengeBootstrap
    {
        private const string BattleGameSceneName = "GameMahjongBattle";

        private static readonly string[] VisibleSceneNames =
        {
            "Main",
            "LobbyMahjong",
            "LobbyMahjongBattle"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            DoorFx.SceneTransitionStarted -= OnSceneTransitionStarted;
            DoorFx.SceneTransitionStarted += OnSceneTransitionStarted;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void OnSceneTransitionStarted(string targetSceneName)
        {
            if (ShouldShowInScene(targetSceneName))
                return;

            DuelChallengeLobbyUI ui = Object.FindAnyObjectByType<DuelChallengeLobbyUI>(FindObjectsInactive.Include);
            if (ui != null)
                Object.Destroy(ui.gameObject);
        }

        private static void EnsureForScene(Scene scene)
        {
            Multiplayer.DuelChallengeService.EnsureInstance();

            if (!ShouldShowInScene(scene.name))
            {
                DuelChallengeLobbyUI ui = Object.FindAnyObjectByType<DuelChallengeLobbyUI>(FindObjectsInactive.Include);
                if (ui != null)
                    Object.Destroy(ui.gameObject);
                return;
            }

            ProfileRuntimeBootstrap.EnsureServices();
            DuelChallengeLobbyUI.Ensure(BattleGameSceneName);
        }

        private static bool ShouldShowInScene(string sceneName)
        {
            for (int i = 0; i < VisibleSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, VisibleSceneNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
