using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LanguageButton : MonoBehaviour
    {
        private const string RussianLanguageButtonResourcePath = "Mahjong/Sprites/RuButton";
        private const string EnglishLanguageButtonResourcePath = "Mahjong/Sprites/EngButton";
        private const string TurkishLanguageButtonResourcePath = "Mahjong/Sprites/TrButton";

        [SerializeField] private GameLanguage language = GameLanguage.Russian;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Text legacyLabel;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.55f);

        private static Sprite russianLanguageButtonSprite;
        private static Sprite englishLanguageButtonSprite;
        private static Sprite turkishLanguageButtonSprite;
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

            Sprite sprite = GetLanguageButtonSprite(language);
            if (targetImage != null)
            {
                if (sprite != null)
                {
                    targetImage.sprite = sprite;
                    targetImage.type = Image.Type.Simple;
                    targetImage.preserveAspect = true;
                    targetImage.color = Color.white;
                    SetLabelVisible(false);
                    return;
                }

                GameLanguage current = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
                targetImage.color = current == language ? selectedColor : normalColor;
            }

            SetLabelVisible(sprite == null || targetImage == null);
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

        private Sprite GetLanguageButtonSprite(GameLanguage targetLanguage)
        {
            return targetLanguage switch
            {
                GameLanguage.English => LoadEnglishLanguageButtonSprite(),
                GameLanguage.Turkish => LoadTurkishLanguageButtonSprite(),
                _ => LoadRussianLanguageButtonSprite()
            };
        }

        private static Sprite LoadRussianLanguageButtonSprite()
        {
            if (russianLanguageButtonSprite != null)
                return russianLanguageButtonSprite;

            russianLanguageButtonSprite = LoadFirstSprite(RussianLanguageButtonResourcePath);
            return russianLanguageButtonSprite;
        }

        private static Sprite LoadEnglishLanguageButtonSprite()
        {
            if (englishLanguageButtonSprite != null)
                return englishLanguageButtonSprite;

            englishLanguageButtonSprite = LoadFirstSprite(EnglishLanguageButtonResourcePath);
            return englishLanguageButtonSprite;
        }

        private static Sprite LoadTurkishLanguageButtonSprite()
        {
            if (turkishLanguageButtonSprite != null)
                return turkishLanguageButtonSprite;

            turkishLanguageButtonSprite = LoadFirstSprite(TurkishLanguageButtonResourcePath);
            return turkishLanguageButtonSprite;
        }

        private static Sprite LoadFirstSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private void SetLabelVisible(bool visible)
        {
            if (label != null)
                label.gameObject.SetActive(visible);

            if (legacyLabel != null)
                legacyLabel.gameObject.SetActive(visible);
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
