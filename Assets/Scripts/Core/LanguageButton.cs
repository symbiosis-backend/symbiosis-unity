using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LanguageButton : MonoBehaviour
    {
        [SerializeField] private GameLanguage language = GameLanguage.Russian;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Text legacyLabel;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.55f);

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            AutoResolve();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(SelectLanguage);
                button.onClick.AddListener(SelectLanguage);
            }

            AppSettings.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(SelectLanguage);

            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        public void SelectLanguage()
        {
            EnsureSettingsInstance();
            AppSettings.I?.SetLanguage(language);
        }

        public void Configure(GameLanguage targetLanguage)
        {
            language = targetLanguage;
            Refresh();
        }

        private void OnLanguageChanged(GameLanguage currentLanguage)
        {
            Refresh();
        }

        private void Refresh()
        {
            string key = language switch
            {
                GameLanguage.English => "settings.language_en",
                GameLanguage.Turkish => "settings.language_tr",
                _ => "settings.language_ru"
            };

            string text = GameLocalization.Text(key);

            if (label != null)
                label.text = text;

            if (legacyLabel != null)
                legacyLabel.text = text;

            if (targetImage != null)
            {
                GameLanguage current = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
                targetImage.color = current == language ? selectedColor : normalColor;
            }
        }

        private void AutoResolve()
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);

            if (legacyLabel == null)
                legacyLabel = GetComponentInChildren<Text>(true);

            if (targetImage == null)
                targetImage = GetComponent<Image>();
        }

        private static void EnsureSettingsInstance()
        {
            if (AppSettings.I != null)
                return;

            GameObject go = new GameObject("AppSettings");
            go.AddComponent<AppSettings>();
        }
    }
}
