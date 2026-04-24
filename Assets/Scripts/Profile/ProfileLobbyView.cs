using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileLobbyView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI publicIdText;
        [SerializeField] private TextMeshProUGUI ageGenderText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Button backButton;

        [Header("Avatar Data")]
        [SerializeField] private Sprite[] avatarSprites;
        [SerializeField] private Sprite fallbackAvatar;

        [Header("Fallback Text")]
        [SerializeField] private string fallbackName = "Player";

        [Header("Scene Names")]
        [SerializeField] private string mainSceneName = "Main";

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnClickBack);
        }

        private void OnEnable()
        {
            ProfileRuntimeBootstrap.EnsureServices();
            EnsureGeneratedProfileInfo();
            ProfileService.ProfileChanged += Refresh;
            CurrencyService.CurrencyChanged += Refresh;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= Refresh;
            CurrencyService.CurrencyChanged -= Refresh;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnClickBack);
        }

        public void Refresh()
        {
            PlayerProfile profile = ProfileRuntimeBootstrap.TryGetProfile();

            if (profile == null)
            {
                ApplyFallback();
                return;
            }

            profile.EnsureData();

            ApplyName(profile);
            ApplyPublicId(profile);
            ApplyAgeGender(profile);
            ApplyAvatar(profile);
            ApplyMahjongTitle(profile);
            ApplyMahjongRank(profile);
        }

        private void ApplyName(PlayerProfile profile)
        {
            if (nameText == null)
                return;

            nameText.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? GetFallbackName()
                : profile.DisplayName;
        }

        private void ApplyAvatar(PlayerProfile profile)
        {
            if (avatarImage == null)
                return;

            Sprite spriteToUse = fallbackAvatar;
            Sprite resourceSprite = ProfileAvatarResources.GetSprite(profile.Gender, profile.AvatarId);
            if (resourceSprite != null)
            {
                spriteToUse = resourceSprite;
            }

            if (resourceSprite == null &&
                avatarSprites != null &&
                avatarSprites.Length > 0 &&
                profile.AvatarId >= 0 &&
                profile.AvatarId < avatarSprites.Length)
            {
                spriteToUse = avatarSprites[profile.AvatarId];
            }

            avatarImage.sprite = spriteToUse;
            avatarImage.enabled = spriteToUse != null;
        }

        private void ApplyPublicId(PlayerProfile profile)
        {
            if (publicIdText == null)
                return;

            string publicId = profile != null ? profile.PublicPlayerId : string.Empty;
            publicIdText.text = string.IsNullOrWhiteSpace(publicId)
                ? "ID: -"
                : "ID: " + publicId;
        }

        private void ApplyAgeGender(PlayerProfile profile)
        {
            if (ageGenderText == null)
                return;

            string age = profile != null && profile.Age > 0 ? profile.Age.ToString() : "-";
            string gender = profile != null ? GetGenderDisplayName(profile.Gender) : "-";
            ageGenderText.text = $"Age: {age}  Gender: {gender}";
        }

        private void ApplyMahjongTitle(PlayerProfile profile)
        {
            if (titleText == null)
                return;

            string selectedTitle = profile.Mahjong != null ? profile.Mahjong.SelectedTitleId : string.Empty;

            titleText.text = string.IsNullOrWhiteSpace(selectedTitle)
                ? GameLocalization.Text("common.title_empty")
                : GameLocalization.Format("profile.mahjong_title", GetTitleDisplayName(selectedTitle));
        }

        private void ApplyMahjongRank(PlayerProfile profile)
        {
            if (rankText == null)
                return;

            string rankValue = GameLocalization.Text("common.unranked");

            if (profile.Mahjong != null &&
                profile.Mahjong.Battle != null &&
                !string.IsNullOrWhiteSpace(profile.Mahjong.Battle.RankTier))
            {
                rankValue = profile.Mahjong.Battle.RankTier;
            }

            rankText.text = GameLocalization.Format("profile.mahjong_rank", rankValue);
        }

        private string GetTitleDisplayName(string titleId)
        {
            if (MahjongTitleService.I != null)
                return MahjongTitleService.I.GetTitleDisplayName(titleId);

            return titleId;
        }

        private void ApplyFallback()
        {
            if (nameText != null)
                nameText.text = GetFallbackName();

            if (titleText != null)
                titleText.text = GameLocalization.Text("common.title_empty");

            if (rankText != null)
                rankText.text = GameLocalization.Text("common.rank_unranked");

            if (publicIdText != null)
                publicIdText.text = "ID: -";

            if (ageGenderText != null)
                ageGenderText.text = "Age: -  Gender: -";

            if (avatarImage != null)
            {
                avatarImage.sprite = fallbackAvatar;
                avatarImage.enabled = fallbackAvatar != null;
            }
        }

        private void OnClickBack()
        {
            SceneManager.LoadScene(mainSceneName);
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();
        }

        private string GetFallbackName()
        {
            return string.IsNullOrWhiteSpace(fallbackName)
                ? GameLocalization.Text("common.player")
                : fallbackName;
        }

        private string GetGenderDisplayName(PlayerGender gender)
        {
            return gender switch
            {
                PlayerGender.Male => "Male",
                PlayerGender.Female => "Female",
                PlayerGender.Other => "Other",
                _ => "-"
            };
        }

        private void EnsureGeneratedProfileInfo()
        {
            if (publicIdText != null && ageGenderText != null)
                return;

            Transform parent = nameText != null && nameText.transform.parent != null
                ? nameText.transform.parent
                : transform;

            if (publicIdText == null)
                publicIdText = CreateGeneratedText(parent, "PublicIdText", new Vector2(168f, -82f), "ID: -", 18f);

            if (ageGenderText == null)
                ageGenderText = CreateGeneratedText(parent, "AgeGenderText", new Vector2(168f, -108f), "Age: -  Gender: -", 16f);
        }

        private TextMeshProUGUI CreateGeneratedText(Transform parent, string objectName, Vector2 anchoredPosition, string text, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 34f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.88f, 0.94f, 1f, 1f);
            label.raycastTarget = false;
            return label;
        }
    }
}
