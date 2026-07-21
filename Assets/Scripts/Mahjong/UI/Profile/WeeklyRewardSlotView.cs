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
        [SerializeField] private Image rewardIcon;
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

            FindExistingRewardIcon();
            ApplyBackground(readySprite, readyColor);
            ApplyRewardIconWithoutReorder();
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

            EnsureRewardIcon();
            ApplyFill(background != null ? background.rectTransform : null);
            ApplyIconRect(rewardIcon, new Vector2(0f, 0f), new Vector2(118f, 118f));
            ApplyTextRect(dayText, new Vector2(0f, 58f), new Vector2(140f, 34f), 24f, 18f, 28f);
            ApplyTextRect(stateText, new Vector2(0f, -96f), new Vector2(154f, 34f), 22f, 16f, 26f);
            ApplyTextRect(claimTypeText, new Vector2(0f, -124f), new Vector2(154f, 30f), 18f, 14f, 22f);
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
            EnsureRewardIcon();
            ApplyBackground(sprite, bgColor);
            ApplyRewardIcon();

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

        private void EnsureRewardIcon()
        {
            FindExistingRewardIcon();

            if (rewardIcon == null)
            {
                GameObject iconObject = new GameObject("RewardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(transform, false);
                rewardIcon = iconObject.GetComponent<Image>();
            }

            ApplyRewardIcon();
            rewardIcon.transform.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
        }

        private void FindExistingRewardIcon()
        {
            if (rewardIcon != null)
                return;

            Transform existing = transform.Find("RewardIcon");
            rewardIcon = existing != null ? existing.GetComponent<Image>() : null;
        }

        private void ApplyRewardIcon()
        {
            ApplyRewardIcon(rewardIcon, dayIndex);
        }

        private void ApplyRewardIconWithoutReorder()
        {
            if (rewardIcon == null)
                return;

            rewardIcon.sprite = WeeklyRewardIconProvider.GetDaySprite(dayIndex);
            rewardIcon.enabled = rewardIcon.sprite != null;
            rewardIcon.color = Color.white;
            rewardIcon.preserveAspect = true;
            rewardIcon.raycastTarget = false;
        }

        private static void ApplyRewardIcon(Image image, int dayIndex)
        {
            if (image == null)
                return;

            image.sprite = WeeklyRewardIconProvider.GetDaySprite(dayIndex);
            image.enabled = image.sprite != null;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.transform.SetSiblingIndex(Mathf.Min(1, image.transform.parent != null ? image.transform.parent.childCount - 1 : 0));
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

        private static void ApplyIconRect(Image image, Vector2 position, Vector2 size)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
