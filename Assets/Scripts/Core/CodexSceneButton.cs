using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class CodexSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "Codex";
    [SerializeField] private bool allowReloadCurrentScene;

    private Button button;
    private bool isLoading;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadScene);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(LoadScene);
        }
    }

    public void LoadScene()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[CodexSceneButton] Scene name is empty.", this);
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!allowReloadCurrentScene && activeSceneName == sceneName)
        {
            Debug.Log($"[CodexSceneButton] Scene '{sceneName}' is already active.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[CodexSceneButton] Scene '{sceneName}' is not in Build Settings or the name is wrong.", this);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }
}
