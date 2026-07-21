using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneMusicController : MonoBehaviour
    {
        private const string MainSceneName = "Main";
        private const string MahjongLobbySceneName = "LobbyMahjong";
        private const string BattleLobbySceneName = "LobbyMahjongBattle";
        private const string BattleGameSceneName = "GameMahjongBattle";
        private const string MainMusicPath = "Mahjong/Music/MainMusic";
        private const string StoryModeMusicPath = "Mahjong/Music/StoryModeMusic";
        private const string BattleLobbyMusicPath = "Mahjong/Music/BattleLobbyMusic";

        private static SceneMusicController instance;

        private AudioSource source;
        private string currentResourcePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("SceneMusic");
            instance = go.AddComponent<SceneMusicController>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 1f;

            AppSettings.OnMusicChanged += OnMusicChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            ApplyForScene(SceneManager.GetActiveScene().name);
            ApplyMute();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            AppSettings.OnMusicChanged -= OnMusicChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForScene(scene.name);
            ApplyMute();
        }

        private void OnMusicChanged(bool enabled)
        {
            if (source != null)
                source.mute = !enabled;
        }

        private void ApplyForScene(string sceneName)
        {
            string resourcePath = ResolveResourcePath(sceneName);
            if (string.IsNullOrEmpty(resourcePath))
            {
                StopMusic();
                return;
            }

            if (currentResourcePath == resourcePath && source.clip != null)
            {
                if (!source.isPlaying)
                    source.Play();

                return;
            }

            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[SceneMusicController] Music clip not found: Resources/{resourcePath}");
                StopMusic();
                return;
            }

            currentResourcePath = resourcePath;
            source.clip = clip;
            source.Play();
        }

        private void StopMusic()
        {
            currentResourcePath = null;

            if (source == null)
                return;

            source.Stop();
            source.clip = null;
        }

        private void ApplyMute()
        {
            if (source == null)
                return;

            source.mute = AppSettings.I != null && !AppSettings.I.MusicEnabled;
        }

        private static string ResolveResourcePath(string sceneName)
        {
            return sceneName switch
            {
                MainSceneName => MainMusicPath,
                MahjongLobbySceneName => StoryModeMusicPath,
                BattleLobbySceneName => BattleLobbyMusicPath,
                BattleGameSceneName => BattleLobbyMusicPath,
                _ => null
            };
        }
    }
}
