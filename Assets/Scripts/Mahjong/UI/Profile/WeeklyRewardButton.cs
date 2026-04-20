using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class WeeklyRewardButton : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private WeeklyRewardWindow window;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Graphic statusDot;

        [Header("Colors")]
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color claimedColor = Color.gray;
        [SerializeField] private Color blockedColor = new Color(0.7f, 0.35f, 0.35f, 1f);

        [Header("Visual Sprites")]
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite claimedSprite;
        [SerializeField] private Sprite blockedSprite;
        [SerializeField] private bool preserveAspect = true;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            ApplyImage(buttonImage, availableSprite, availableColor, preserveAspect);
        }

        private void OnEnable()
        {
            ProfileService.ProfileChanged += OnProfileChanged;
            Refresh();
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            Refresh();
        }

        public void Refresh()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                ApplyState(false, blockedColor, blockedSprite, "No Profile");
                return;
            }

            profile.EnsureData();
            WeeklyRewardService.EnsureInitialized(profile);

            bool timeBlocked = WeeklyRewardService.IsTimeBlocked(profile);
            bool canClaim = WeeklyRewardService.CanClaimToday(profile);

            if (timeBlocked)
            {
                ApplyState(true, blockedColor, blockedSprite, "Time Error");
                return;
            }

            if (canClaim)
            {
                int dayNumber = WeeklyRewardService.GetCurrentDayNumber(profile);
                ApplyState(true, availableColor, availableSprite, $"Reward\nDay {dayNumber}");
                return;
            }

            ApplyState(true, claimedColor, claimedSprite, "Claimed");
        }

        private void OnClick()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null || window == null)
                return;

            window.Open(profile);
            Refresh();
        }

        private PlayerProfile GetProfile()
        {
            if (ProfileService.I == null)
                return null;

            PlayerProfile profile = ProfileService.I.Current;
            if (profile == null)
                return null;

            profile.EnsureData();
            return profile;
        }

        private void ApplyState(bool interactable, Color color, Sprite sprite, string label)
        {
            if (button != null)
                button.interactable = interactable;

            ApplyImage(buttonImage, sprite, color, preserveAspect);

            if (labelText != null)
                labelText.text = label;

            if (statusDot != null)
                statusDot.color = color;
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color, bool keepAspect)
        {
            if (image == null)
                return;

            if (sprite != null)
                image.sprite = sprite;

            image.color = color;
            image.preserveAspect = keepAspect;
        }
    }
}
