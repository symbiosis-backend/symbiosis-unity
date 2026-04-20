using UnityEngine;
using UnityEngine.SceneManagement;

public class AppSceneManager : MonoBehaviour
{
    public static AppSceneManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ENTRY → MAIN
    public void GoToMain()
    {
        SceneManager.LoadScene("Main");
    }

    // MAIN → LOBBY OKEY
    public void GoToLobbyOkey()
    {
        SceneManager.LoadScene("LobbyOkey");
    }

    // MAIN → LOBBY MAHJONG
    public void GoToLobbyMahjong()
    {
        SceneManager.LoadScene("LobbyMahjong");
    }

    // LOBBY → GAME
    public void GoToGameOkey()
    {
        SceneManager.LoadScene("GameOkey");
    }

    public void GoToGameMahjong()
    {
        SceneManager.LoadScene("GameMahjong");
    }

    // ОБРАТНЫЕ ПЕРЕХОДЫ
    public void BackToMain()
    {
        SceneManager.LoadScene("Main");
    }

    public void BackToLobbyOkey()
    {
        SceneManager.LoadScene("LobbyOkey");
    }

    public void BackToLobbyMahjong()
    {
        SceneManager.LoadScene("LobbyMahjong");
    }
}
