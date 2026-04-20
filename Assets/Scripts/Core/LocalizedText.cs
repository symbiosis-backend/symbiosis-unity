using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private Text legacyText;

        private void Reset()
        {
            AutoResolveText();
        }

        private void Awake()
        {
            AutoResolveText();
        }

        private void OnEnable()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            Refresh();
        }

        public void Refresh()
        {
            string value = GameLocalization.Text(localizationKey);

            if (tmpText != null)
                tmpText.text = value;

            if (legacyText != null)
                legacyText.text = value;
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();
        }

        private void AutoResolveText()
        {
            if (tmpText == null)
                tmpText = GetComponent<TMP_Text>();

            if (legacyText == null)
                legacyText = GetComponent<Text>();
        }
    }
}
