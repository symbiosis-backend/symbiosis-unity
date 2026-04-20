using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class WeeklyRewardSlotView : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text claimTypeText;

        [Header("Colors")]
        [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color readyColor = Color.white;
        [SerializeField] private Color claimedColor = new Color(0.7f, 1f, 0.7f, 1f);
        [SerializeField] private Color currentColor = new Color(1f, 0.95f, 0.7f, 1f);

        [Header("Visual Sprites")]
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite readySprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite claimedSprite;
        [SerializeField] private bool preserveAspect = true;

        private int dayIndex;

        private void OnValidate()
        {
            if (background == null)
                background = GetComponentInChildren<Image>(true);

            ApplyBackground(readySprite, readyColor);
        }

        public void Setup(int index)
        {
            dayIndex = Mathf.Clamp(index, 0, 6);

            if (dayText != null)
                dayText.text = $"Day {dayIndex + 1}";
        }

        public void ConfigureVisuals(
            Sprite locked,
            Sprite ready,
            Sprite current,
            Sprite claimed,
            Color lockedTint,
            Color readyTint,
            Color currentTint,
            Color claimedTint,
            bool keepAspect)
        {
            lockedSprite = locked;
            readySprite = ready;
            currentSprite = current;
            claimedSprite = claimed;
            lockedColor = lockedTint;
            readyColor = readyTint;
            currentColor = currentTint;
            claimedColor = claimedTint;
            preserveAspect = keepAspect;

            ApplyBackground(readySprite, readyColor);
        }

        public void ApplyDefaultLayout()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = Vector3.one;

            ApplyFill(background != null ? background.rectTransform : null);
            ApplyTextRect(dayText, new Vector2(0f, 54f), new Vector2(140f, 34f), 24f, 18f, 28f);
            ApplyTextRect(stateText, new Vector2(0f, -28f), new Vector2(140f, 34f), 22f, 16f, 26f);
            ApplyTextRect(claimTypeText, new Vector2(0f, -62f), new Vector2(140f, 30f), 18f, 14f, 22f);
        }

        public void Refresh(PlayerProfile profile)
        {
            if (profile == null)
                return;

            bool claimed = WeeklyRewardService.IsDayClaimed(profile, dayIndex);
            bool isCurrent = WeeklyRewardService.IsDayCurrent(profile, dayIndex);
            bool isLocked = WeeklyRewardService.IsDayLocked(profile, dayIndex);
            WeeklyRewardClaimType claimType = WeeklyRewardService.GetDayClaimType(profile, dayIndex);

            if (claimed)
            {
                SetVisual(claimedSprite, claimedColor, "Claimed", GetClaimTypeLabel(claimType));
                return;
            }

            if (isLocked)
            {
                SetVisual(lockedSprite, lockedColor, "Locked", string.Empty);
                return;
            }

            if (isCurrent)
            {
                bool canClaim = WeeklyRewardService.CanClaimToday(profile);
                SetVisual(canClaim ? currentSprite : readySprite, canClaim ? currentColor : readyColor, canClaim ? "Ready" : "Waiting", string.Empty);
                return;
            }

            SetVisual(readySprite, readyColor, string.Empty, string.Empty);
        }

        private void SetVisual(Sprite sprite, Color bgColor, string state, string claimType)
        {
            ApplyBackground(sprite, bgColor);

            if (stateText != null)
                stateText.text = state;

            if (claimTypeText != null)
                claimTypeText.text = claimType;
        }

        private string GetClaimTypeLabel(WeeklyRewardClaimType claimType)
        {
            return claimType switch
            {
                WeeklyRewardClaimType.Free => "Free",
                WeeklyRewardClaimType.Ad => "Ad",
                _ => string.Empty
            };
        }

        private void ApplyBackground(Sprite sprite, Color bgColor)
        {
            if (background == null)
                return;

            if (sprite != null)
                background.sprite = sprite;

            background.color = bgColor;
            background.preserveAspect = preserveAspect;
        }

        private static void ApplyFill(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ApplyTextRect(TMP_Text text, Vector2 position, Vector2 size, float fontSize, float minSize, float maxSize)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSize = fontSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.color = Color.white;
        }
    }
}
