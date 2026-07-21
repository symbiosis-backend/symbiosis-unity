using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MainButtonSoundController : MonoBehaviour
    {
        private const string MainSceneName = "Main";
        private const string ClickClipResourcePath = "Orbiosis/Audio/PartPickup";
        private const string FallbackClickClipResourcePath = "Mahjong/Sounds/MainButtonSound";
        private const float ClickVolume = 0.52f;
        private const float ScanInterval = 0.25f;

        private static MainButtonSoundController instance;

        private readonly List<MainButtonSoundTarget> wiredTargets = new();

        private AudioSource source;
        private AudioClip clickClip;
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("MainButtonSound");
            instance = go.AddComponent<MainButtonSoundController>();
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

            clickClip = Resources.Load<AudioClip>(ClickClipResourcePath);
            if (clickClip == null)
                clickClip = Resources.Load<AudioClip>(FallbackClickClipResourcePath);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            WireScene(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MainSceneName, System.StringComparison.Ordinal))
                return;

            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanInterval;
            WireMainButtons(SceneManager.GetActiveScene());
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

            if (!string.Equals(scene.name, MainSceneName, System.StringComparison.Ordinal))
                return;

            WireMainButtons(scene);
        }

        private void WireMainButtons(Scene scene)
        {
            PruneDestroyedTargets();

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (!IsMainButtonCandidate(button, scene))
                    continue;

                WireButton(button);
            }
        }

        private static bool IsMainButtonCandidate(Button button, Scene scene)
        {
            if (button == null)
                return false;

            if (button.gameObject.scene == scene)
                return true;

            return button.GetComponentInParent<SettingsMenuUI>(true) != null;
        }

        private void WireButton(Button button)
        {
            if (button == null)
                return;

            MainButtonSoundTarget target = button.GetComponent<MainButtonSoundTarget>();
            if (target == null)
                target = button.gameObject.AddComponent<MainButtonSoundTarget>();

            target.Bind(this, button);

            if (!wiredTargets.Contains(target))
                wiredTargets.Add(target);

            // Remove the old listener-based hook if an earlier runtime instance added it.
            if (button != null)
                button.onClick.RemoveListener(PlayClickSound);
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
                MainButtonSoundTarget target = wiredTargets[i];
                if (target != null)
                    target.Unbind(this);
            }

            wiredTargets.Clear();
        }

        internal void PlayClickSound()
        {
            if (source == null || clickClip == null)
                return;

            if (AppSettings.I != null && !AppSettings.I.SoundEnabled)
                return;

            source.PlayOneShot(clickClip, ClickVolume);
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainButtonSoundTarget : MonoBehaviour, IPointerDownHandler
    {
        private MainButtonSoundController owner;
        private Button button;

        public void Bind(MainButtonSoundController controller, Button sourceButton)
        {
            owner = controller;
            button = sourceButton;
        }

        public void Unbind(MainButtonSoundController controller)
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
