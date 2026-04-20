using UnityEngine;
using UnityEngine.SceneManagement;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class SceneNavigator : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string entryScene = "Entry";
        [SerializeField] private string mainScene = "Main";
        [SerializeField] private string lobbyOkeyScene = "LobbyOkey";
        [SerializeField] private string gameOkeyScene = "GameOkey";
        [SerializeField] private string lobbyMahjongScene = "LobbyMahjong";
        [SerializeField] private string gameMahjongScene = "GameMahjong";

        [Header("Mahjong Door Transition")]
        [SerializeField] private bool useDoorFxForMahjong = true;
        [SerializeField] private bool useDoorFxForMainToMahjong = true;
        [SerializeField] private bool useDoorFxForMahjongBackToMain = true;
        [SerializeField] private bool useDoorFxForMahjongReload = true;

        private bool isLoading;

        public string CurrentSceneName => SceneManager.GetActiveScene().name;

        public bool IsCurrentScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && CurrentSceneName == sceneName;
        }

        public void LoadEntry()
        {
            LoadSceneByName(entryScene);
        }

        public void LoadMain()
        {
            bool fromMahjong = IsCurrentScene(lobbyMahjongScene) || IsCurrentScene(gameMahjongScene);

            if (fromMahjong && useDoorFxForMahjongBackToMain)
            {
                LoadSceneByName(mainScene, false, true);
                return;
            }

            LoadSceneByName(mainScene);
        }

        public void LoadLobbyOkey()
        {
            LoadSceneByName(lobbyOkeyScene);
        }

        public void LoadGameOkey()
        {
            LoadSceneByName(gameOkeyScene);
        }

        public void LoadLobbyMahjong()
        {
            bool fromMain = IsCurrentScene(mainScene);
            bool useDoor = useDoorFxForMahjong && (!fromMain || useDoorFxForMainToMahjong);
            LoadSceneByName(lobbyMahjongScene, false, useDoor);
        }

        public void LoadGameMahjong()
        {
            LoadSceneByName(gameMahjongScene, false, useDoorFxForMahjong);
        }

        public void BackFromMain()
        {
            LoadEntry();
        }

        public void BackFromLobbyOkey()
        {
            LoadMain();
        }

        public void BackFromGameOkey()
        {
            LoadLobbyOkey();
        }

        public void BackFromLobbyMahjong()
        {
            if (useDoorFxForMahjongBackToMain)
            {
                LoadSceneByName(mainScene, false, true);
                return;
            }

            LoadMain();
        }

        public void BackFromGameMahjong()
        {
            LoadSceneByName(lobbyMahjongScene, false, useDoorFxForMahjong);
        }

        public void ReloadCurrentScene()
        {
            if (isLoading)
                return;

            bool useDoor = useDoorFxForMahjongReload && IsMahjongScene(CurrentSceneName);
            LoadSceneByName(CurrentSceneName, true, useDoor);
        }

        public void QuitGame()
        {
            Debug.Log("[SceneNavigator] QuitGame");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool IsMahjongScene(string sceneName)
        {
            return sceneName == lobbyMahjongScene || sceneName == gameMahjongScene;
        }

        private void LoadSceneByName(string sceneName, bool allowReloadSameScene = false, bool useDoorFx = false)
        {
            if (isLoading)
                return;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[SceneNavigator] Scene name is empty.");
                return;
            }

            if (!allowReloadSameScene && IsCurrentScene(sceneName))
            {
                Debug.Log($"[SceneNavigator] Scene '{sceneName}' is already active.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneNavigator] Scene '{sceneName}' is not in Build Settings or name is wrong.");
                return;
            }

            isLoading = true;
            Debug.Log($"[SceneNavigator] Loading scene: {sceneName}");

            var doorFx = MahjongGame.DoorFx.I;

            if (useDoorFx && doorFx != null && doorFx.isActiveAndEnabled && doorFx.IsReady())
            {
                doorFx.LoadScene(sceneName);
                isLoading = false;
                return;
            }

            SceneManager.LoadScene(sceneName);
            isLoading = false;
        }
    }
}