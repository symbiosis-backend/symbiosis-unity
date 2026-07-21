using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OzGame.Okey
{
    public static class MainOzLobbyEntryButton
    {
        private const string ButtonName = "Btn_OzLobby_Runtime";

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
            DestroyEntryButtons();
        }

        private static void DestroyEntryButtons()
        {
            GameObject oldButton = GameObject.Find(ButtonName);
            if (oldButton != null)
                Object.Destroy(oldButton);

            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null)
                    continue;

                if (obj.name == ButtonName)
                {
                    Object.Destroy(obj);
                    continue;
                }

                Button button = obj.GetComponent<Button>();
                TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null)
                    continue;

                string text = label.text != null ? label.text.Trim() : string.Empty;
                if (string.Equals(text, "OZ LOBBY", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "ÖzGame", System.StringComparison.OrdinalIgnoreCase))
                {
                    Object.Destroy(obj);
                }
            }
        }
    }
}
