using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class BattlePick : MonoBehaviour
    {
        [SerializeField] private BattleCharacterCircularCarousel carousel;
        [SerializeField] private GameObject closeWindow;
        [SerializeField] private BattleLobbyChar lobbyChar;
        [SerializeField] private string selectTextKey = "battle.character.select_character";

        private Button btn;
        private TMP_Text label;
        private BattleCharacterButton lastCenteredButton;
        private string lastLabelText;

        private void Awake()
        {
            btn = GetComponent<Button>();
            ApplyBattleLobbyStyle();

            if (carousel == null)
                carousel = FindAnyObjectByType<BattleCharacterCircularCarousel>();

            if (lobbyChar == null)
                lobbyChar = FindAnyObjectByType<BattleLobbyChar>();

            btn.onClick.AddListener(Pick);
            AppSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnEnable()
        {
            ApplyBattleLobbyStyle();
        }

        private void Update()
        {
            ResolveReferencesIfNeeded();
            RefreshButtonState();
        }

        private void OnDestroy()
        {
            if (btn != null)
                btn.onClick.RemoveListener(Pick);

            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        public void Pick()
        {
            ResolveReferencesIfNeeded();

            BattleCharacterButton activeButton = GetActiveButton();
            if (activeButton == null)
                return;

            RefreshButtonState();

            bool picked = activeButton.SelectDirectly();
            if (!picked)
                return;

            if (lobbyChar != null)
                lobbyChar.ConfirmAndRefresh();

            if (closeWindow != null)
                closeWindow.SetActive(false);
        }

        private void OnLanguageChanged(GameLanguage _)
        {
            ApplyBattleLobbyStyle();
        }

        private void ResolveReferencesIfNeeded()
        {
            if (carousel == null)
                carousel = GetComponentInParent<BattleCharacterCircularCarousel>(true);

            if (carousel == null)
                carousel = FindAnyObjectByType<BattleCharacterCircularCarousel>(FindObjectsInactive.Include);

            if (lobbyChar == null)
                lobbyChar = FindAnyObjectByType<BattleLobbyChar>(FindObjectsInactive.Include);
        }

        private BattleCharacterButton GetActiveButton()
        {
            ResolveReferencesIfNeeded();
            return carousel != null ? carousel.ActivePreviewButton : null;
        }

        private void ApplyBattleLobbyStyle()
        {
            if (btn == null)
                btn = GetComponent<Button>();

            if (btn == null)
                return;

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(540f, 96f);
                rect.anchoredPosition = new Vector2(0f, -424f);
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
            }

            BattlePopupStyle.ApplyPremiumButton(btn);
            EnsureLabel();
            RefreshButtonState(true);
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                ConfigureLabelRect();
                return;
            }

            label = GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                ConfigureLabelRect();
                return;
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);

            label = labelObject.GetComponent<TMP_Text>();
            ConfigureLabelRect();
        }

        private void ConfigureLabelRect()
        {
            if (label == null)
                return;

            RectTransform labelRect = label.transform as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(40f, 8f);
                labelRect.offsetMax = new Vector2(-40f, -10f);
                labelRect.localScale = Vector3.one;
            }

            label.gameObject.SetActive(true);
            label.transform.SetAsLastSibling();
        }

        private void RefreshButtonState(bool force = false)
        {
            if (label == null)
                return;

            BattleCharacterButton centered = GetActiveButton();
            string text = centered != null
                ? centered.GetPickerButtonText()
                : GetLocalizedText(selectTextKey, "Select Character");

            bool interactable = centered == null || centered.CanUsePickerButton();
            if (!force && centered == lastCenteredButton && string.Equals(text, lastLabelText, System.StringComparison.Ordinal) && btn != null && btn.interactable == interactable)
                return;

            lastCenteredButton = centered;
            lastLabelText = text;
            label.text = text;

            if (btn != null)
                btn.interactable = interactable;

            BattlePopupStyle.ApplyText(label, false);
            label.color = new Color(1f, 0.82f, 0.28f, 1f);
            label.outlineColor = new Color(0f, 0f, 0f, 0.92f);
            label.outlineWidth = 0.18f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSize = 34f;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 42f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            label.gameObject.SetActive(true);
            label.transform.SetAsLastSibling();
        }

        private static string GetLocalizedText(string key, string fallback)
        {
            string value = GameLocalization.Text(key);
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, System.StringComparison.Ordinal)
                ? fallback
                : value;
        }
    }
}
