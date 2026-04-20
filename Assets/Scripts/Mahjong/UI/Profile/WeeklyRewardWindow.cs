using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class WeeklyRewardWindow : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private RectTransform panelRect;

        [Header("Slots")]
        [SerializeField] private WeeklyRewardSlotView[] slots;

        [Header("Buttons")]
        [SerializeField] private Button freeButton;
        [SerializeField] private Button adButton;
        [SerializeField] private Button closeButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text todayRewardText;
        [SerializeField] private TMP_Text freeButtonText;
        [SerializeField] private TMP_Text adButtonText;
        [SerializeField] private TMP_Text statusText;

        [Header("Button Colors")]
        [SerializeField] private Image freeButtonImage;
        [SerializeField] private Image adButtonImage;
        [SerializeField] private Color activeButtonColor = Color.white;
        [SerializeField] private Color disabledButtonColor = Color.gray;

        [Header("Visual Setup")]
        [SerializeField] private Image backdropImage;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image closeButtonImage;
        [SerializeField] private WeeklyRewardWindowVisual visual = new WeeklyRewardWindowVisual();

        [Header("Refresh")]
        [SerializeField] private MonoBehaviour[] externalRefreshTargets;

        private PlayerProfile profile;
        private GameObject generatedBackdropRoot;

        [Serializable]
        public sealed class WeeklyRewardWindowVisual
        {
            [Header("Panel")]
            public bool UseBackdrop = true;
            public Color BackdropColor = new Color(0f, 0f, 0f, 0.92f);
            public Sprite PanelSprite;
            public Color PanelColor = new Color(0.1f, 0.1f, 0.1f, 0.86f);
            public bool PanelPreserveAspect = true;

            [Header("Layout")]
            public bool ApplyLayout = true;
            public Vector2 WindowSize = new Vector2(1260f, 660f);
            public Vector2 TitlePosition = new Vector2(0f, 270f);
            public Vector2 TitleSize = new Vector2(720f, 74f);
            public Vector2 SlotsStartPosition = new Vector2(-510f, 75f);
            public Vector2 SlotSize = new Vector2(145f, 175f);
            public float SlotSpacing = 170f;
            public Vector2 StatusPosition = new Vector2(0f, -125f);
            public Vector2 StatusSize = new Vector2(760f, 46f);
            public Vector2 FreeButtonPosition = new Vector2(-280f, -240f);
            public Vector2 FreeButtonSize = new Vector2(280f, 92f);
            public Vector2 AdButtonPosition = new Vector2(280f, -240f);
            public Vector2 AdButtonSize = new Vector2(330f, 92f);
            public Vector2 CloseButtonPosition = new Vector2(545f, 275f);
            public Vector2 CloseButtonSize = new Vector2(120f, 60f);

            [Header("Claim Buttons")]
            public Sprite FreeButtonSprite;
            public Sprite AdButtonSprite;
            public Color ActiveButtonColor = Color.white;
            public Color DisabledButtonColor = Color.gray;
            public bool ButtonPreserveAspect = true;

            [Header("Close Button")]
            public Sprite CloseButtonSprite;
            public Color CloseButtonColor = Color.white;
            public bool CloseButtonPreserveAspect = true;

            [Header("Day Slots")]
            public Sprite LockedSlotSprite;
            public Sprite ReadySlotSprite;
            public Sprite CurrentSlotSprite;
            public Sprite ClaimedSlotSprite;
            public Color LockedSlotColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            public Color ReadySlotColor = Color.white;
            public Color CurrentSlotColor = new Color(1f, 0.95f, 0.7f, 1f);
            public Color ClaimedSlotColor = new Color(0.7f, 1f, 0.7f, 1f);
            public bool SlotPreserveAspect = true;
        }

        private void Awake()
        {
            AutoResolveVisualRefs();

            if (freeButton != null)
                freeButton.onClick.AddListener(OnFreeClicked);

            if (adButton != null)
                adButton.onClick.AddListener(OnAdClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                        slots[i].Setup(i);
                }
            }

            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            EnsureBackdrop();
            ApplyLayoutPreview();
            ApplyVisualPreview();
            ShowBackdrop(false);
        }

        private void OnValidate()
        {
            AutoResolveVisualRefs();
            ApplyVisualPreview();
        }

        public void Open(PlayerProfile targetProfile)
        {
            profile = targetProfile;
            if (profile == null)
                return;

            profile.EnsureData();
            WeeklyRewardService.EnsureInitialized(profile);

            if (windowRoot != null)
                windowRoot.SetActive(true);

            ShowBackdrop(true);
            transform.SetAsLastSibling();
            ApplyLayoutPreview();
            ApplyVisualPreview();
            RefreshUI();
        }

        public void Close()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);

            ShowBackdrop(false);
        }

        public void RefreshUI()
        {
            if (profile == null)
                return;

            profile.EnsureData();
            WeeklyRewardService.EnsureInitialized(profile);
            ApplySlotVisuals();

            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                        slots[i].Refresh(profile);
                }
            }

            int dayNumber = WeeklyRewardService.GetCurrentDayNumber(profile);
            int freeAltin = WeeklyRewardService.GetFreeAltin(profile);
            int adAltin = WeeklyRewardService.GetAdAltin(profile);
            int adAmetist = WeeklyRewardService.GetAdAmetist(profile);

            if (todayRewardText != null)
                todayRewardText.text = $"Day {dayNumber} Reward";

            if (freeButtonText != null)
                freeButtonText.text = $"{freeAltin} Altın";

            if (adButtonText != null)
                adButtonText.text = $"{adAltin} Altın + {adAmetist} Ametist";

            bool timeBlocked = WeeklyRewardService.IsTimeBlocked(profile);
            bool canClaim = WeeklyRewardService.CanClaimToday(profile);

            bool freeInteractable = !timeBlocked && canClaim;
            bool adInteractable = !timeBlocked && canClaim;

            if (freeButton != null)
                freeButton.interactable = freeInteractable;

            if (adButton != null)
                adButton.interactable = adInteractable;

            if (freeButtonImage != null)
                ApplyImage(freeButtonImage, visual != null ? visual.FreeButtonSprite : null, freeInteractable ? activeButtonColor : disabledButtonColor, visual == null || visual.ButtonPreserveAspect);

            if (adButtonImage != null)
                ApplyImage(adButtonImage, visual != null ? visual.AdButtonSprite : null, adInteractable ? activeButtonColor : disabledButtonColor, visual == null || visual.ButtonPreserveAspect);

            if (statusText != null)
            {
                if (timeBlocked)
                    statusText.text = "Time error detected";
                else if (canClaim)
                    statusText.text = "Reward available";
                else
                    statusText.text = "Reward already claimed today";
            }
        }

        private void OnFreeClicked()
        {
            if (profile == null)
                return;

            if (WeeklyRewardService.ClaimFree(profile))
            {
                SaveAndNotify();
                RefreshUI();
                RefreshExternalTargets();
            }
        }

        private void OnAdClicked()
        {
            if (profile == null)
                return;

            if (WeeklyRewardService.ClaimAd(profile))
            {
                SaveAndNotify();
                RefreshUI();
                RefreshExternalTargets();
            }
        }

        private void SaveAndNotify()
        {
            if (ProfileService.I == null)
                return;

            ProfileService.I.Save();
            ProfileService.I.NotifyProfileChanged();
        }

        private void RefreshExternalTargets()
        {
            if (externalRefreshTargets == null)
                return;

            for (int i = 0; i < externalRefreshTargets.Length; i++)
            {
                MonoBehaviour target = externalRefreshTargets[i];
                if (target == null)
                    continue;

                target.Invoke(nameof(WeeklyRewardButton.Refresh), 0f);
            }
        }

        private void AutoResolveVisualRefs()
        {
            if (windowRect == null)
                windowRect = GetComponent<RectTransform>();

            if (panelRect == null && windowRoot != null)
                panelRect = windowRoot.GetComponent<RectTransform>();

            if (panelImage == null && windowRoot != null)
                panelImage = windowRoot.GetComponent<Image>();

            if (freeButtonImage == null && freeButton != null)
                freeButtonImage = freeButton.GetComponent<Image>();

            if (adButtonImage == null && adButton != null)
                adButtonImage = adButton.GetComponent<Image>();

            if (closeButtonImage == null && closeButton != null)
                closeButtonImage = closeButton.GetComponent<Image>();
        }

        private void ApplyLayoutPreview()
        {
            if (visual == null || !visual.ApplyLayout)
                return;

            ApplyCenteredRect(windowRect, Vector2.zero, visual.WindowSize);
            ApplyFill(panelRect);

            ApplyTextRect(todayRewardText, visual.TitlePosition, visual.TitleSize, 46f, 30f, 54f, Color.white);
            ApplyTextRect(statusText, visual.StatusPosition, visual.StatusSize, 26f, 18f, 32f, new Color(1f, 0.92f, 0.68f, 1f));

            ApplyButtonRect(freeButton, visual.FreeButtonPosition, visual.FreeButtonSize);
            ApplyButtonRect(adButton, visual.AdButtonPosition, visual.AdButtonSize);
            ApplyButtonRect(closeButton, visual.CloseButtonPosition, visual.CloseButtonSize);

            ApplyButtonImageFill(freeButton, freeButtonImage);
            ApplyButtonImageFill(adButton, adButtonImage);
            ApplyButtonImageFill(closeButton, closeButtonImage);

            ApplyTextRect(freeButtonText, visual.FreeButtonPosition, visual.FreeButtonSize * 0.86f, 28f, 18f, 34f, Color.white);
            ApplyTextRect(adButtonText, visual.AdButtonPosition, visual.AdButtonSize * 0.86f, 26f, 16f, 32f, Color.white);

            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    WeeklyRewardSlotView slot = slots[i];
                    if (slot == null)
                        continue;

                    RectTransform slotRect = slot.GetComponent<RectTransform>();
                    ApplyCenteredRect(slotRect, visual.SlotsStartPosition + new Vector2(visual.SlotSpacing * i, 0f), visual.SlotSize);
                    slot.ApplyDefaultLayout();
                }
            }
        }

        [ContextMenu("Weekly Reward/Apply Layout Preview")]
        private void ApplyLayoutPreviewFromContext()
        {
            AutoResolveVisualRefs();
            EnsureBackdrop();
            ApplyLayoutPreview();
            ApplyVisualPreview();
        }

        private void ApplyVisualPreview()
        {
            if (visual == null)
                return;

            if (backdropImage != null)
                backdropImage.color = visual.BackdropColor;

            ApplyImage(panelImage, visual.PanelSprite, visual.PanelColor, visual.PanelPreserveAspect);
            ApplyImage(freeButtonImage, visual.FreeButtonSprite, visual.ActiveButtonColor, visual.ButtonPreserveAspect);
            ApplyImage(adButtonImage, visual.AdButtonSprite, visual.ActiveButtonColor, visual.ButtonPreserveAspect);
            ApplyImage(closeButtonImage, visual.CloseButtonSprite, visual.CloseButtonColor, visual.CloseButtonPreserveAspect);
            ApplySlotVisuals();

            activeButtonColor = visual.ActiveButtonColor;
            disabledButtonColor = visual.DisabledButtonColor;
        }

        private void ApplySlotVisuals()
        {
            if (visual == null || slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                WeeklyRewardSlotView slot = slots[i];
                if (slot == null)
                    continue;

                slot.ConfigureVisuals(
                    visual.LockedSlotSprite,
                    visual.ReadySlotSprite,
                    visual.CurrentSlotSprite,
                    visual.ClaimedSlotSprite,
                    visual.LockedSlotColor,
                    visual.ReadySlotColor,
                    visual.CurrentSlotColor,
                    visual.ClaimedSlotColor,
                    visual.SlotPreserveAspect);
            }
        }

        private void EnsureBackdrop()
        {
            if (backdropImage != null)
                return;

            Transform parent = transform.parent;
            if (parent == null)
                return;

            generatedBackdropRoot = new GameObject("WeeklyRewardBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            generatedBackdropRoot.transform.SetParent(parent, false);

            RectTransform rect = generatedBackdropRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            backdropImage = generatedBackdropRoot.GetComponent<Image>();
            backdropImage.raycastTarget = true;
            backdropImage.color = visual != null ? visual.BackdropColor : new Color(0f, 0f, 0f, 0.92f);

            generatedBackdropRoot.SetActive(false);
        }

        private void ShowBackdrop(bool show)
        {
            if (visual != null && !visual.UseBackdrop)
                show = false;

            EnsureBackdrop();

            if (backdropImage == null)
                return;

            GameObject backdropObject = backdropImage.gameObject;
            if (backdropObject.activeSelf != show)
                backdropObject.SetActive(show);

            if (show)
            {
                backdropObject.transform.SetAsLastSibling();
                transform.SetAsLastSibling();
            }
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color, bool preserveAspect)
        {
            if (image == null)
                return;

            if (sprite != null)
                image.sprite = sprite;

            image.color = color;
            image.preserveAspect = preserveAspect;
        }

        private static void ApplyCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
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

        private static void ApplyButtonRect(Button button, Vector2 position, Vector2 size)
        {
            if (button == null)
                return;

            ApplyCenteredRect(button.GetComponent<RectTransform>(), position, size);
        }

        private static void ApplyButtonImageFill(Button button, Image image)
        {
            if (button == null || image == null)
                return;

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            RectTransform imageRect = image.rectTransform;
            if (imageRect != null && imageRect != buttonRect)
                ApplyFill(imageRect);
        }

        private static void ApplyTextRect(TMP_Text text, Vector2 position, Vector2 size, float fontSize, float minSize, float maxSize, Color color)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            ApplyCenteredRect(rect, position, size);

            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSize = fontSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.color = color;
        }
    }
}
