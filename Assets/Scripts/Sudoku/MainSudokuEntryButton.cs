using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.Sudoku
{
    public static class MainSudokuEntryButton
    {
        private const string ButtonName = "Btn_SymSudoku_Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureButton(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureButton(scene);
        }

        private static void EnsureButton(Scene scene)
        {
            DestroyButton();
        }

        private static void DestroyButton()
        {
            GameObject existing = GameObject.Find(ButtonName);
            if (existing != null)
                Object.Destroy(existing);
        }
    }
}
