using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MahjongButtonSoundController : MonoBehaviour
    {
        private const string MahjongLobbySceneName = "LobbyMahjong";
        private const string BattleLobbySceneName = "LobbyMahjongBattle";
        private const string BattleGameSceneName = "GameMahjongBattle";

        private const string MahjongLobbyClipResourcePath = "Mahjong/Sounds/MahLobby";
        private const string BattleClipResourcePath = "Mahjong/Sounds/MahjongBattleButtonSound";
        private const float ScanInterval = 0.25f;

        private static MahjongButtonSoundController instance;

        private readonly List<MahjongButtonSoundTarget> wiredTargets = new();

        private AudioSource source;
        private AudioClip mahjongLobbyClip;
        private AudioClip battleClip;
        private AudioClip activeClip;
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("MahjongButtonSound");
            instance = go.AddComponent<MahjongButtonSoundController>();
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
            source.loop = false;
            source.spatialBlend = 0f;

            mahjongLobbyClip = Resources.Load<AudioClip>(MahjongLobbyClipResourcePath);
            battleClip = Resources.Load<AudioClip>(BattleClipResourcePath);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            WireScene(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsHandledScene(scene.name))
                return;

            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanInterval;
            WireSceneButtons(scene);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            UnwireButtons();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            WireScene(scene);
        }

        private void WireScene(Scene scene)
        {
            UnwireButtons();
            nextScanTime = 0f;
            activeClip = ResolveClip(scene.name);

            if (activeClip == null)
                return;

            WireSceneButtons(scene);
        }

        private void WireSceneButtons(Scene scene)
        {
            PruneDestroyedTargets();

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (!IsButtonCandidate(button, scene))
                {
                    UnwireButton(button);
                    continue;
                }

                WireButton(button);
            }
        }

        private static bool IsButtonCandidate(Button button, Scene scene)
        {
            if (button == null)
                return false;

            GameObject buttonObject = button.gameObject;
            if (buttonObject == null)
                return false;

            try
            {
                if (button.GetComponentInParent<BattleTile>(true) != null)
                    return false;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
            catch (System.NullReferenceException)
            {
                return false;
            }

            Scene buttonScene = buttonObject.scene;
            if (buttonScene == scene)
                return true;

            return string.Equals(buttonScene.name, "DontDestroyOnLoad", System.StringComparison.Ordinal);
        }

        private void WireButton(Button button)
        {
            if (button == null)
                return;

            MahjongButtonSoundTarget target = button.GetComponent<MahjongButtonSoundTarget>();
            if (target == null)
                target = button.gameObject.AddComponent<MahjongButtonSoundTarget>();

            target.Bind(this, button);

            if (!wiredTargets.Contains(target))
                wiredTargets.Add(target);
        }

        private void UnwireButton(Button button)
        {
            if (button == null)
                return;

            MahjongButtonSoundTarget target = button.GetComponent<MahjongButtonSoundTarget>();
            if (target == null)
                return;

            target.Unbind(this);
            wiredTargets.Remove(target);
        }

        private void PruneDestroyedTargets()
        {
            for (int i = wiredTargets.Count - 1; i >= 0; i--)
            {
                if (wiredTargets[i] == null)
                    wiredTargets.RemoveAt(i);
            }
        }

        private void UnwireButtons()
        {
            for (int i = wiredTargets.Count - 1; i >= 0; i--)
            {
                MahjongButtonSoundTarget target = wiredTargets[i];
                if (target != null)
                    target.Unbind(this);
            }

            wiredTargets.Clear();
            activeClip = null;
        }

        internal void PlayClickSound()
        {
            if (source == null || activeClip == null)
                return;

            if (AppSettings.I != null && !AppSettings.I.SoundEnabled)
                return;

            source.PlayOneShot(activeClip);
        }

        private AudioClip ResolveClip(string sceneName)
        {
            return sceneName switch
            {
                MahjongLobbySceneName => mahjongLobbyClip,
                BattleLobbySceneName => battleClip,
                BattleGameSceneName => battleClip,
                _ => null
            };
        }

        private static bool IsHandledScene(string sceneName)
        {
            return sceneName == MahjongLobbySceneName
                || sceneName == BattleLobbySceneName
                || sceneName == BattleGameSceneName;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MahjongButtonSoundTarget : MonoBehaviour, IPointerDownHandler
    {
        private MahjongButtonSoundController owner;
        private Button button;

        public void Bind(MahjongButtonSoundController controller, Button sourceButton)
        {
            owner = controller;
            button = sourceButton;
        }

        public void Unbind(MahjongButtonSoundController controller)
        {
            if (owner == controller)
                owner = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            if (button == null || !button.IsActive() || !button.interactable)
                return;

            owner?.PlayClickSound();
        }
    }
}
