using System;
using System.Collections.Generic;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class AppSettings : MonoBehaviour
    {
        public static AppSettings I { get; private set; }

        public static event Action<bool> OnSoundChanged;
        public static event Action<bool> OnMusicChanged;
        public static event Action<bool> OnVibrationChanged;
        public static event Action<bool> OnInfoHintsChanged;
        public static event Action<bool> OnChatAutoTranslateChanged;
        public static event Action<GameLanguage> OnLanguageChanged;

        private const string KEY_SOUND = "mahjong_sound_enabled";
        private const string KEY_MUSIC = "mahjong_music_enabled";
        private const string KEY_VIBRATION = "mahjong_vibration_enabled";
        private const string KEY_INFO_HINTS = "mahjong_info_hints_enabled";
        private const string KEY_CHAT_AUTO_TRANSLATE = "mahjong_chat_auto_translate_enabled";
        private const string KEY_LANGUAGE = "mahjong_language";
        private const string KEY_LANGUAGE_SELECTED = "mahjong_language_selected";

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "LobbyMahjong";

        [Header("Auto Audio Detect")]
        [SerializeField] private bool autoScanOnSceneLoaded = true;
        [SerializeField] private bool includeInactiveObjects = true;
        [SerializeField] private string[] musicKeywords =
        {
            "music",
            "bgm",
            "theme",
            "ambient",
            "menu_music",
            "game_music"
        };

        private readonly List<AudioSource> cachedSources = new();
        private float nextHapticRealtime;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SymbiosisHapticImpact(int style);
#endif

        public bool SoundEnabled { get; private set; } = true;
        public bool MusicEnabled { get; private set; } = true;
        public bool VibrationEnabled { get; private set; } = true;
        public bool InfoHintsEnabled { get; private set; } = true;
        public bool ChatAutoTranslateEnabled { get; private set; } = true;
        public GameLanguage Language { get; private set; } = GameLanguage.Turkish;
        public bool HasLanguagePreference { get; private set; }

        public string MainMenuSceneName => mainMenuSceneName;

        private void Awake()
        {
            RuntimeFileLogger.Write("[Startup] AppSettings Awake begin");

            if (I != null && I != this)
            {
                RuntimeFileLogger.Write("[Startup] Duplicate AppSettings destroyed");
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
            RuntimeFileLogger.Write("[Startup] AppSettings persistent");

            LoadSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
            RuntimeFileLogger.Write("[Startup] AppSettings Awake done");
        }

        private void Start()
        {
            RuntimeFileLogger.Write("[Startup] AppSettings Start begin");
            RefreshAudioSources();
            ApplyAudioStates();

            OnSoundChanged?.Invoke(SoundEnabled);
            OnMusicChanged?.Invoke(MusicEnabled);
            OnVibrationChanged?.Invoke(VibrationEnabled);
            OnInfoHintsChanged?.Invoke(InfoHintsEnabled);
            OnChatAutoTranslateChanged?.Invoke(ChatAutoTranslateEnabled);
            OnLanguageChanged?.Invoke(Language);
            RuntimeFileLogger.Write("[Startup] AppSettings Start done");
        }

        private void OnDestroy()
        {
            if (I == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!autoScanOnSceneLoaded)
                return;

            RefreshAudioSources();
            ApplyAudioStates();
        }

        private void LoadSettings()
        {
            SoundEnabled = PlayerPrefs.GetInt(KEY_SOUND, 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
            VibrationEnabled = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
            InfoHintsEnabled = PlayerPrefs.GetInt(KEY_INFO_HINTS, 1) == 1;
            ChatAutoTranslateEnabled = PlayerPrefs.GetInt(KEY_CHAT_AUTO_TRANSLATE, 1) == 1;
            Language = ReadLanguage();
            HasLanguagePreference = PlayerPrefs.GetInt(KEY_LANGUAGE_SELECTED, 0) == 1;
        }

        public void SetSoundEnabled(bool value)
        {
            if (SoundEnabled == value)
                return;

            SoundEnabled = value;
            PlayerPrefs.SetInt(KEY_SOUND, value ? 1 : 0);
            PlayerPrefs.Save();

            RefreshAudioSources();
            ApplyAudioStates();
            OnSoundChanged?.Invoke(SoundEnabled);
        }

        public void SetMusicEnabled(bool value)
        {
            if (MusicEnabled == value)
                return;

            MusicEnabled = value;
            PlayerPrefs.SetInt(KEY_MUSIC, value ? 1 : 0);
            PlayerPrefs.Save();

            RefreshAudioSources();
            ApplyAudioStates();
            OnMusicChanged?.Invoke(MusicEnabled);
        }

        public void SetVibrationEnabled(bool value)
        {
            if (VibrationEnabled == value)
                return;

            VibrationEnabled = value;
            PlayerPrefs.SetInt(KEY_VIBRATION, value ? 1 : 0);
            PlayerPrefs.Save();

            OnVibrationChanged?.Invoke(VibrationEnabled);
        }

        public void SetInfoHintsEnabled(bool value)
        {
            if (InfoHintsEnabled == value)
                return;

            InfoHintsEnabled = value;
            PlayerPrefs.SetInt(KEY_INFO_HINTS, value ? 1 : 0);
            PlayerPrefs.Save();

            OnInfoHintsChanged?.Invoke(InfoHintsEnabled);
        }

        public void SetChatAutoTranslateEnabled(bool value)
        {
            if (ChatAutoTranslateEnabled == value)
                return;

            ChatAutoTranslateEnabled = value;
            PlayerPrefs.SetInt(KEY_CHAT_AUTO_TRANSLATE, value ? 1 : 0);
            PlayerPrefs.Save();

            OnChatAutoTranslateChanged?.Invoke(ChatAutoTranslateEnabled);
        }

        public void SetLanguage(GameLanguage language)
        {
            if (!Enum.IsDefined(typeof(GameLanguage), language))
                language = GameLanguage.Turkish;

            if (Language == language && HasLanguagePreference)
                return;

            Language = language;
            HasLanguagePreference = true;
            PlayerPrefs.SetInt(KEY_LANGUAGE, (int)language);
            PlayerPrefs.SetInt(KEY_LANGUAGE_SELECTED, 1);
            PlayerPrefs.Save();

            OnLanguageChanged?.Invoke(Language);
        }

        public void SetRussianLanguage()
        {
            SetLanguage(GameLanguage.Russian);
        }

        public void SetEnglishLanguage()
        {
            SetLanguage(GameLanguage.English);
        }

        public void SetTurkishLanguage()
        {
            SetLanguage(GameLanguage.Turkish);
        }

        public void SetGermanLanguage()
        {
            SetLanguage(GameLanguage.German);
        }

        public void ClearLanguagePreference()
        {
            HasLanguagePreference = false;
            PlayerPrefs.DeleteKey(KEY_LANGUAGE_SELECTED);
            PlayerPrefs.Save();
        }

        public void RefreshAudioSources()
        {
            cachedSources.Clear();

            AudioSource[] allSources = FindObjectsByType<AudioSource>(
                includeInactiveObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude
            );

            for (int i = 0; i < allSources.Length; i++)
            {
                AudioSource source = allSources[i];

                if (source == null)
                    continue;

                if (source.gameObject == gameObject)
                    continue;

                cachedSources.Add(source);
            }
        }

        public void ApplyAudioStates()
        {
            for (int i = cachedSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = cachedSources[i];

                if (source == null)
                {
                    cachedSources.RemoveAt(i);
                    continue;
                }

                bool isMusic = IsMusicSource(source);
                bool enabled = isMusic ? MusicEnabled : SoundEnabled;

                source.mute = !enabled;
            }
        }

        public void RefreshAndApplyAudio()
        {
            RefreshAudioSources();
            ApplyAudioStates();
        }

        public void Vibrate()
        {
            if (!VibrationEnabled)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public void VibrateLight()
        {
            VibrateImpact(18L, 45, 0);
        }

        public void VibrateMedium()
        {
            VibrateImpact(42L, 115, 1);
        }

        private void VibrateImpact(long durationMilliseconds, int amplitude, int iosStyle)
        {
            if (!VibrationEnabled || Time.unscaledTime < nextHapticRealtime)
                return;

            nextHapticRealtime = Time.unscaledTime + (iosStyle == 0 ? 0.04f : 0.08f);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject vibrator = activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    return;

                using AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 26)
                {
                    using AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    using AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        durationMilliseconds,
                        Mathf.Clamp(amplitude, 1, 255));
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMilliseconds);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AppSettings] Haptic impact failed: {exception.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            SymbiosisHapticImpact(iosStyle);
#endif
        }

        private bool IsMusicSource(AudioSource source)
        {
            if (source == null)
                return false;

            string objectName = source.gameObject.name.ToLowerInvariant();

            for (int i = 0; i < musicKeywords.Length; i++)
            {
                string keyword = musicKeywords[i];
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                if (objectName.Contains(keyword.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private static GameLanguage ReadLanguage()
        {
            int value = PlayerPrefs.GetInt(KEY_LANGUAGE, (int)GameLanguage.Turkish);
            GameLanguage language = (GameLanguage)value;
            return Enum.IsDefined(typeof(GameLanguage), language) ? language : GameLanguage.Turkish;
        }
    }
}
