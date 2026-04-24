using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileMainView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI publicIdText;
        [SerializeField] private TextMeshProUGUI ageGenderText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI dynastyText;
        [SerializeField] private TextMeshProUGUI slotText;

        [Header("Avatar Data")]
        [SerializeField] private Sprite[] avatarSprites;
        [SerializeField] private Sprite fallbackAvatar;

        [Header("Fallback Text")]
        [SerializeField] private string fallbackName = "Player";

        private RectTransform rootRect;
        private RectTransform compactButtonRect;
        private RectTransform compactAvatarFrameRect;
        private TextMeshProUGUI openProfileText;
        private Image modalOverlayImage;
        private RectTransform modalOverlayRect;
        private RectTransform modalWindowRect;
        private RectTransform modalBackgroundRect;
        private Image modalBackgroundImage;
        private RectTransform modalFrameRect;
        private Image modalFrameImage;
        private RectTransform modalAvatarFrameRect;
        private Button compactButton;
        private Button overlayCloseButton;
        private Button modalWindowClickBlocker;
        private Button closeButton;
        private TextMeshProUGUI titleSelectHeaderText;
        private readonly List<Button> titleButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> titleButtonLabels = new List<TextMeshProUGUI>();
        private readonly List<string> titleButtonIds = new List<string>();
        private Transform uiParent;
        private bool expanded;

        private void OnEnable()
        {
            ProfileRuntimeBootstrap.EnsureServices();
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();
            EnsureGeneratedProfileInfo();
            EnsureRuntimeUi();
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
            UnbindButtons();
        }

        private void OnRectTransformDimensionsChange()
        {
            LayoutProfileInfo();
        }

        public void Refresh()
        {
            PlayerProfile profile = ProfileRuntimeBootstrap.TryGetProfile();

            if (profile == null)
            {
                ApplyFallback();
                LayoutProfileInfo();
                return;
            }

            profile.EnsureData();

            ApplyName(profile);
            ApplyDynasty(profile);
            ApplySlot(profile);
            ApplyPublicId(profile);
            ApplyAgeGender(profile);
            ApplyAvatar(profile);
            ApplyGlobalTitle(profile);
            ApplyGlobalRank(profile);
            RefreshTitleButtons();
            LayoutProfileInfo();
        }

        private void ApplyName(PlayerProfile profile)
        {
            if (nameText == null)
                return;

            nameText.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? GetFallbackName()
                : profile.DisplayName.Trim();
        }

        private void ApplyAvatar(PlayerProfile profile)
        {
            if (avatarImage == null)
                return;

            Sprite spriteToUse = fallbackAvatar;
            Sprite resourceSprite = ProfileAvatarResources.GetSprite(profile.Gender, profile.AvatarId);
            if (resourceSprite != null)
                spriteToUse = resourceSprite;

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
            avatarImage.preserveAspect = true;
        }

        private void ApplyGlobalTitle(PlayerProfile profile)
        {
            if (titleText == null)
                return;

            string title = MahjongTitleService.I != null
                ? MahjongTitleService.I.GetProfileDisplayTitle(profile)
                : ResolveSelectedTitleFallback(profile);

            titleText.text = string.IsNullOrWhiteSpace(title)
                ? GameLocalization.Text("common.title_empty")
                : GameLocalization.Format("profile.title", title);
        }

        private void ApplyGlobalRank(PlayerProfile profile)
        {
            if (rankText == null)
                return;

            string rankValue = string.IsNullOrWhiteSpace(profile.GlobalRankTier)
                ? GameLocalization.Text("common.unranked")
                : profile.GlobalRankTier.Trim();

            rankText.text = GameLocalization.Format("profile.rank", rankValue);
        }

        private void ApplyDynasty(PlayerProfile profile)
        {
            if (dynastyText == null)
                return;

            string dynastyName = profile != null ? profile.DynastyName : string.Empty;
            dynastyText.text = string.IsNullOrWhiteSpace(dynastyName)
                ? GameLocalization.Text("profile.dynasty_empty")
                : GameLocalization.Format("profile.dynasty", dynastyName.Trim());
        }

        private void ApplySlot(PlayerProfile profile)
        {
            if (slotText == null)
                return;

            int slot = profile != null ? Mathf.Clamp(profile.ProfileSlotIndex <= 0 ? 1 : profile.ProfileSlotIndex, 1, 3) : 1;
            slotText.text = GameLocalization.Format("profile.slot", slot);
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
            ageGenderText.text = GameLocalization.Format("profile.age_gender", age, gender);
        }

        private void ApplyFallback()
        {
            if (nameText != null)
                nameText.text = GetFallbackName();

            if (titleText != null)
                titleText.text = GameLocalization.Text("common.title_empty");

            if (rankText != null)
                rankText.text = GameLocalization.Text("common.rank_unranked");

            if (dynastyText != null)
                dynastyText.text = GameLocalization.Text("profile.dynasty_empty");

            if (slotText != null)
                slotText.text = GameLocalization.Format("profile.slot", 1);

            if (publicIdText != null)
                publicIdText.text = "ID: -";

            if (ageGenderText != null)
                ageGenderText.text = GameLocalization.Format("profile.age_gender", "-", "-");

            if (avatarImage != null)
            {
                avatarImage.sprite = fallbackAvatar;
                avatarImage.enabled = fallbackAvatar != null;
                avatarImage.preserveAspect = true;
            }

            RefreshTitleButtons();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();
        }

        private void OpenProfile()
        {
            expanded = true;
            if (modalOverlayRect != null)
                modalOverlayRect.SetAsLastSibling();
            RaiseAuxiliaryMenuRoots();
            LayoutProfileInfo();
        }

        private void CloseProfile()
        {
            expanded = false;
            LayoutProfileInfo();
        }

        private void SelectTitle(string titleId)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || string.IsNullOrWhiteSpace(titleId))
                return;

            if (MahjongTitleService.I != null)
            {
                MahjongTitleService.I.SelectTitle(profile, titleId);
            }
            else if (profile.Mahjong != null && profile.Mahjong.HasUnlockedTitle(titleId))
            {
                profile.Mahjong.SetSelectedTitle(titleId);
                profile.SetGlobalTitle(titleId);
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }

            RefreshTitleButtons();
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
                PlayerGender.Male => GameLocalization.Text("profile.gender.male"),
                PlayerGender.Female => GameLocalization.Text("profile.gender.female"),
                PlayerGender.Other => GameLocalization.Text("profile.gender.other"),
                _ => "-"
            };
        }

        private void EnsureGeneratedProfileInfo()
        {
            Transform parent = ResolveUiParent();

            if (publicIdText == null)
                publicIdText = CreateGeneratedText(parent, "PublicIdText", "ID: -", 24f, FontStyles.Normal);

            if (ageGenderText == null)
                ageGenderText = CreateGeneratedText(parent, "AgeGenderText", GameLocalization.Format("profile.age_gender", "-", "-"), 24f, FontStyles.Normal);

            if (dynastyText == null)
                dynastyText = CreateGeneratedText(parent, "DynastyText", GameLocalization.Text("profile.dynasty_empty"), 28f, FontStyles.Bold);

            if (slotText == null)
                slotText = CreateGeneratedText(parent, "SlotText", GameLocalization.Format("profile.slot", 1), 22f, FontStyles.Bold);
        }

        private void EnsureRuntimeUi()
        {
            Canvas canvas = CentralPointLayout.ResolveMainCanvas();
            RectTransform leftMenuRoot = CentralPointLayout.ResolveLeftMenuRoot(canvas);
            Transform compactParent = leftMenuRoot != null ? leftMenuRoot : ResolveUiParent();
            Transform overlayParent = canvas != null ? canvas.transform : compactParent;
            rootRect = overlayParent as RectTransform;
            if (rootRect == null)
                return;

            if (compactButtonRect == null)
            {
                GameObject compact = new GameObject("ProfileOpenButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                compact.transform.SetParent(compactParent, false);
                compact.transform.SetAsFirstSibling();
                compactButtonRect = compact.GetComponent<RectTransform>();

                Image image = compact.GetComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = true;

                compactButton = compact.GetComponent<Button>();
                compactButton.targetGraphic = image;
                MainLobbyButtonStyle.Apply(compactButton);

                openProfileText = CreateGeneratedText(compact.transform, "OpenProfileText", GameLocalization.Text("menu.profile"), 24f, FontStyles.Bold);
            }
            else if (compactParent != null && compactButtonRect.parent != compactParent)
            {
                compactButtonRect.SetParent(compactParent, false);
                compactButtonRect.SetAsFirstSibling();
            }

            if (compactAvatarFrameRect == null)
            {
                GameObject frame = new GameObject("CompactAvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frame.transform.SetParent(compactParent, false);
                frame.transform.SetAsFirstSibling();
                compactAvatarFrameRect = frame.GetComponent<RectTransform>();

                Image frameImage = frame.GetComponent<Image>();
                frameImage.color = Color.white;
                frameImage.raycastTarget = false;
                MainLobbyButtonStyle.ApplyAvatarCard(frameImage);
            }
            else if (compactParent != null && compactAvatarFrameRect.parent != compactParent)
            {
                compactAvatarFrameRect.SetParent(compactParent, false);
                compactAvatarFrameRect.SetAsFirstSibling();
            }

            if (modalOverlayRect == null)
            {
                GameObject overlay = new GameObject("ProfileModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                overlay.transform.SetParent(overlayParent, false);
                overlay.transform.SetAsLastSibling();
                modalOverlayRect = overlay.GetComponent<RectTransform>();
                modalOverlayImage = overlay.GetComponent<Image>();
                modalOverlayImage.color = new Color(0f, 0f, 0f, 0.68f);
                modalOverlayImage.raycastTarget = true;
                overlayCloseButton = overlay.GetComponent<Button>();
                overlayCloseButton.targetGraphic = modalOverlayImage;

                GameObject window = new GameObject("ProfileModalWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                window.transform.SetParent(overlay.transform, false);
                modalWindowRect = window.GetComponent<RectTransform>();
                Image windowImage = window.GetComponent<Image>();
                ConfigureModalWindowRoot(windowImage);
                modalWindowClickBlocker = window.GetComponent<Button>();
                modalWindowClickBlocker.targetGraphic = windowImage;
                EnsureModalWindowLayers();

                GameObject modalAvatarFrame = new GameObject("ModalAvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                modalAvatarFrame.transform.SetParent(window.transform, false);
                modalAvatarFrameRect = modalAvatarFrame.GetComponent<RectTransform>();
                Image avatarFrameImage = modalAvatarFrame.GetComponent<Image>();
                avatarFrameImage.color = Color.white;
                avatarFrameImage.raycastTarget = false;
                MainLobbyButtonStyle.ApplyAvatarCard(avatarFrameImage);

                closeButton = CreateTextButton(window.transform, "CloseProfileButton", GameLocalization.Text("settings.close"), 24f);

                titleSelectHeaderText = CreateGeneratedText(window.transform, "TitleSelectHeader", GameLocalization.Text("profile.titles"), 24f, FontStyles.Bold);
                SetObjectActive(titleSelectHeaderText.gameObject, false);
            }
            else if (overlayParent != null && modalOverlayRect.parent != overlayParent)
            {
                modalOverlayRect.SetParent(overlayParent, false);
            }

            if (modalWindowRect != null)
            {
                ConfigureModalWindowRoot(modalWindowRect.GetComponent<Image>());
                EnsureModalWindowLayers();
            }

            BindButtons();
        }

        private Transform ResolveUiParent()
        {
            if (uiParent != null)
                return uiParent;

            RectTransform leftMenuRoot = CentralPointLayout.ResolveLeftMenuRoot();
            if (leftMenuRoot != null)
                uiParent = leftMenuRoot;
            else if (avatarImage != null && avatarImage.transform.parent != null)
                uiParent = avatarImage.transform.parent;
            else if (nameText != null && nameText.transform.parent != null)
                uiParent = nameText.transform.parent;
            else
                uiParent = transform;

            return uiParent;
        }

        private void ConfigureModalWindowRoot(Image image)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        private void EnsureModalWindowLayers()
        {
            if (modalWindowRect == null)
                return;

            if (modalBackgroundRect == null)
            {
                Transform existing = modalWindowRect.Find("ProfileModalBackground");
                if (existing != null)
                {
                    modalBackgroundRect = existing as RectTransform;
                    modalBackgroundImage = existing.GetComponent<Image>();
                }
            }

            if (modalBackgroundRect == null)
            {
                GameObject background = new GameObject("ProfileModalBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.transform.SetParent(modalWindowRect, false);
                modalBackgroundRect = background.transform as RectTransform;
                modalBackgroundImage = background.GetComponent<Image>();
            }

            if (modalBackgroundImage == null && modalBackgroundRect != null)
                modalBackgroundImage = modalBackgroundRect.GetComponent<Image>();

            if (modalBackgroundImage != null)
            {
                MainLobbyButtonStyle.ApplyProfileWindow(modalBackgroundImage);
                modalBackgroundImage.raycastTarget = false;
            }

            if (modalBackgroundRect != null)
            {
                SetObjectActive(modalBackgroundRect.gameObject, true);
                modalBackgroundRect.SetAsFirstSibling();
            }

            if (modalFrameRect == null)
            {
                Transform existing = modalWindowRect.Find("ProfileModalFrame");
                if (existing != null)
                {
                    modalFrameRect = existing as RectTransform;
                    modalFrameImage = existing.GetComponent<Image>();
                }
            }

            if (modalFrameRect != null)
                SetObjectActive(modalFrameRect.gameObject, false);
        }

        private void BindButtons()
        {
            if (compactButton != null)
            {
                compactButton.onClick.RemoveListener(OpenProfile);
                compactButton.onClick.AddListener(OpenProfile);
            }

            if (overlayCloseButton != null)
            {
                overlayCloseButton.onClick.RemoveListener(CloseProfile);
                overlayCloseButton.onClick.AddListener(CloseProfile);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseProfile);
                closeButton.onClick.AddListener(CloseProfile);
            }

            if (modalWindowClickBlocker != null)
                modalWindowClickBlocker.onClick.RemoveAllListeners();

            BindTitleButtons();
        }

        private void UnbindButtons()
        {
            if (compactButton != null)
                compactButton.onClick.RemoveListener(OpenProfile);

            if (overlayCloseButton != null)
                overlayCloseButton.onClick.RemoveListener(CloseProfile);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseProfile);

            if (modalWindowClickBlocker != null)
                modalWindowClickBlocker.onClick.RemoveAllListeners();

            for (int i = 0; i < titleButtons.Count; i++)
            {
                if (titleButtons[i] != null)
                    titleButtons[i].onClick.RemoveAllListeners();
            }
        }

        private void LayoutProfileInfo()
        {
            if (rootRect == null)
                rootRect = ResolveUiParent() as RectTransform;

            if (rootRect == null)
                return;

            EnsureRuntimeUi();

            SetObjectActive(compactButtonRect != null ? compactButtonRect.gameObject : null, !expanded);
            SetObjectActive(modalOverlayRect != null ? modalOverlayRect.gameObject : null, expanded);

            LayoutCompact();

            if (expanded)
            {
                LayoutModal();
                RaiseAuxiliaryMenuRoots();
            }
        }

        private void LayoutCompact()
        {
            const float x = CentralPointLayout.LeftX;
            const float y = CentralPointLayout.TopY;
            const float width = CentralPointLayout.MenuWidth;
            const float frameSize = 243f;
            const float avatarFillRatio = 0.78f;
            float avatarSize = frameSize * avatarFillRatio;
            float avatarInset = (frameSize - avatarSize) * 0.5f;
            const float buttonWidth = 231f;
            const float buttonHeight = 72f;
            const float buttonGap = 15f;

            float frameX = x + (width - frameSize) * 0.5f;
            float avatarX = frameX + avatarInset;
            float avatarY = y - avatarInset;

            SetTopLeftRect(compactAvatarFrameRect, frameX, y, frameSize, frameSize);
            SetObjectActive(compactAvatarFrameRect != null ? compactAvatarFrameRect.gameObject : null, !expanded);

            SetTopLeftRect(compactButtonRect, x + (width - buttonWidth) * 0.5f, y - frameSize - buttonGap, buttonWidth, buttonHeight);

            if (avatarImage != null)
            {
                Transform avatarParent = compactAvatarFrameRect != null && compactAvatarFrameRect.parent != null
                    ? compactAvatarFrameRect.parent
                    : compactButtonRect;
                avatarImage.transform.SetParent(avatarParent, false);
                SetTopLeftRect(avatarImage.rectTransform, avatarX, avatarY, avatarSize, avatarSize);
                avatarImage.preserveAspect = false;
                PlaceBehindFrame(avatarImage.rectTransform, compactAvatarFrameRect);
            }

            ConfigureText(openProfileText, 30f, 18f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            if (openProfileText != null)
                openProfileText.text = GameLocalization.Text("menu.profile");
            if (openProfileText != null && compactButtonRect != null && openProfileText.transform.parent != compactButtonRect)
                openProfileText.transform.SetParent(compactButtonRect, false);
            SetTopLeftRect(openProfileText != null ? openProfileText.rectTransform : null, 14f, -6f, buttonWidth - 28f, buttonHeight - 12f);

            SetTextVisible(false);
        }

        private void LayoutModal()
        {
            Stretch(modalOverlayRect);

            float rootWidth = rootRect.rect.width > 0f ? rootRect.rect.width : Screen.width;
            float rootHeight = rootRect.rect.height > 0f ? rootRect.rect.height : Screen.height;
            const float frameAspect = 1513f / 1024f;
            float marginX = Mathf.Clamp(rootWidth * 0.045f, 18f, 72f);
            float marginY = Mathf.Clamp(rootHeight * 0.055f, 18f, 96f);
            float maxWidth = rootWidth - marginX * 2f;
            float maxHeight = rootHeight - marginY * 2f;
            if (maxWidth <= 0f || maxHeight <= 0f)
                return;
            float windowWidth = Mathf.Min(maxWidth, maxHeight * frameAspect);
            float windowHeight = windowWidth / frameAspect;

            if (windowHeight > maxHeight)
            {
                windowHeight = maxHeight;
                windowWidth = windowHeight * frameAspect;
            }

            float minimumWidth = Mathf.Min(720f, maxWidth);
            if (windowWidth < minimumWidth)
            {
                windowWidth = minimumWidth;
                windowHeight = Mathf.Min(maxHeight, windowWidth / frameAspect);

                if (windowHeight >= maxHeight - 0.01f)
                    windowWidth = Mathf.Min(maxWidth, windowHeight * frameAspect);
            }

            bool compactModal = windowWidth < 900f || windowHeight < 620f;
            float windowX = (rootWidth - windowWidth) * 0.5f;
            float windowY = -(rootHeight - windowHeight) * 0.5f;

            SetTopLeftRect(modalWindowRect, windowX, windowY, windowWidth, windowHeight);
            EnsureModalWindowLayers();

            float closeButtonWidth = Mathf.Clamp(windowWidth * (compactModal ? 0.34f : 0.23f), 180f, 248f);
            float closeButtonHeight = Mathf.Clamp(windowHeight * 0.11f, 60f, 78f);
            float closeButtonBottomGap = Mathf.Clamp(windowHeight * 0.035f, 14f, 28f);
            float footerClearance = closeButtonBottomGap + closeButtonHeight + Mathf.Clamp(windowHeight * 0.045f, 18f, 36f);

            float insetX = Mathf.Clamp(windowWidth * (compactModal ? 0.075f : 0.085f), 34f, 96f);
            float insetTop = Mathf.Clamp(windowHeight * 0.11f, 34f, 92f);
            float insetBottom = Mathf.Max(footerClearance, Mathf.Clamp(windowHeight * 0.17f, 86f, 128f));
            SetStretchRect(modalBackgroundRect, 0f, 0f, 0f, 0f);
            SetStretchRect(modalFrameRect, 0f, 0f, 0f, 0f);

            float contentLeft = insetX + Mathf.Clamp(windowWidth * 0.03f, 16f, 36f);
            float contentTop = insetTop + Mathf.Clamp(windowHeight * 0.045f, 16f, 34f);
            float contentRight = insetX + Mathf.Clamp(windowWidth * 0.03f, 16f, 36f);
            float frameSize = Mathf.Clamp(windowHeight * (compactModal ? 0.39f : 0.36f), compactModal ? 188f : 220f, 340f);
            float avatarFillRatio = 0.78f;
            float avatarSize = frameSize * avatarFillRatio;
            float avatarInset = (frameSize - avatarSize) * 0.5f;
            float profileShiftLeft = 10f;
            float profileShiftDown = windowHeight * 0.10f;
            float avatarX = contentLeft - profileShiftLeft;
            float avatarY = -contentTop - profileShiftDown;
            float avatarOffsetRight = 12f;
            float avatarOffsetUp = 12f;
            float avatarFrameX = avatarX + avatarOffsetRight;
            float avatarFrameY = avatarY + avatarOffsetUp;
            float textX = avatarX + frameSize + Mathf.Clamp(windowWidth * 0.04f, 22f, 54f);
            float fullTextWidth = Mathf.Max(compactModal ? 180f : 220f, windowWidth - textX - contentRight);
            float sideWidth = Mathf.Clamp(windowWidth * (compactModal ? 0.16f : 0.13f), 84f, 144f);
            float nameGap = 16f;
            float nameWidth = Mathf.Max(140f, fullTextWidth - sideWidth - nameGap);

            SetTopLeftRect(modalAvatarFrameRect, avatarFrameX, avatarFrameY, frameSize, frameSize);

            if (avatarImage != null)
            {
                avatarImage.transform.SetParent(modalWindowRect, false);
                SetTopLeftRect(avatarImage.rectTransform, avatarFrameX + avatarInset, avatarFrameY - avatarInset, avatarSize, avatarSize);
                avatarImage.preserveAspect = false;
                PlaceBehindFrame(avatarImage.rectTransform, modalAvatarFrameRect);
            }

            SetTextVisible(true);
            MoveTextToModalWindow();

            float nameFontSize = compactModal ? 40f : 52f;
            float dynastyFontSize = compactModal ? 28f : 34f;
            float infoFontSize = compactModal ? 22f : 28f;
            float nameHeight = compactModal ? 60f : 60f;
            float dynastyHeight = compactModal ? 40f : 42f;
            float infoLineHeight = compactModal ? 28f : 36f;
            float infoGap = compactModal ? 8f : 10f;
            float nameY = avatarY + (compactModal ? 2f : 4f);
            float dynastyY = nameY - (nameHeight + 10f);
            float infoStartY = dynastyY - (dynastyHeight + 10f);

            ConfigureText(nameText, nameFontSize, compactModal ? 22f : 30f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, TextWrappingModes.Normal);
            SetTopLeftRect(nameText != null ? nameText.rectTransform : null, textX, nameY, nameWidth, nameHeight);

            ConfigureText(slotText, compactModal ? 22f : 28f, compactModal ? 14f : 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.78f, 0.9f, 1f, 1f));
            SetTopLeftRect(slotText != null ? slotText.rectTransform : null, textX + nameWidth + nameGap, nameY + 4f, sideWidth, compactModal ? 34f : 40f);

            ConfigureText(dynastyText, dynastyFontSize, compactModal ? 18f : 21f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.86f, 0.93f, 1f, 1f), TextWrappingModes.Normal);
            SetTopLeftRect(dynastyText != null ? dynastyText.rectTransform : null, textX, dynastyY, fullTextWidth, dynastyHeight);

            ConfigureText(titleText, infoFontSize, compactModal ? 14f : 18f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.78f, 0.86f, 0.98f, 1f));
            SetTopLeftRect(titleText != null ? titleText.rectTransform : null, textX, infoStartY, fullTextWidth, infoLineHeight);

            ConfigureText(rankText, infoFontSize, compactModal ? 14f : 18f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.78f, 0.86f, 0.98f, 1f));
            SetTopLeftRect(rankText != null ? rankText.rectTransform : null, textX, infoStartY - (infoLineHeight + infoGap), fullTextWidth, infoLineHeight);

            ConfigureText(publicIdText, infoFontSize, compactModal ? 14f : 18f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.73f, 0.82f, 0.95f, 1f));
            SetTopLeftRect(publicIdText != null ? publicIdText.rectTransform : null, textX, infoStartY - (infoLineHeight + infoGap) * 2f, fullTextWidth, infoLineHeight);

            ConfigureText(ageGenderText, infoFontSize, compactModal ? 14f : 18f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.73f, 0.82f, 0.95f, 1f));
            SetTopLeftRect(ageGenderText != null ? ageGenderText.rectTransform : null, textX, infoStartY - (infoLineHeight + infoGap) * 3f, fullTextWidth, infoLineHeight);

            HideTitleSelector();
            SetTextButtonLabel(closeButton, GameLocalization.Text("settings.close"));
            SetTopLeftRect(closeButton != null ? closeButton.transform as RectTransform : null, (windowWidth - closeButtonWidth) * 0.5f, -windowHeight + closeButtonBottomGap + closeButtonHeight, closeButtonWidth, closeButtonHeight);
            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        private void MoveTextToModalWindow()
        {
            if (modalWindowRect == null)
                return;

            MoveTextToParent(nameText, modalWindowRect);
            MoveTextToParent(publicIdText, modalWindowRect);
            MoveTextToParent(ageGenderText, modalWindowRect);
            MoveTextToParent(titleText, modalWindowRect);
            MoveTextToParent(rankText, modalWindowRect);
            MoveTextToParent(dynastyText, modalWindowRect);
            MoveTextToParent(slotText, modalWindowRect);
            MoveTextToParent(titleSelectHeaderText, modalWindowRect);
        }

        private static void MoveTextToParent(TextMeshProUGUI text, Transform parent)
        {
            if (text != null && text.transform.parent != parent)
                text.transform.SetParent(parent, false);
        }

        private void SetTextVisible(bool visible)
        {
            SetObjectActive(nameText != null ? nameText.gameObject : null, visible);
            SetObjectActive(publicIdText != null ? publicIdText.gameObject : null, visible);
            SetObjectActive(ageGenderText != null ? ageGenderText.gameObject : null, visible);
            SetObjectActive(titleText != null ? titleText.gameObject : null, visible);
            SetObjectActive(rankText != null ? rankText.gameObject : null, visible);
            SetObjectActive(dynastyText != null ? dynastyText.gameObject : null, visible);
            SetObjectActive(slotText != null ? slotText.gameObject : null, visible);
            SetObjectActive(titleSelectHeaderText != null ? titleSelectHeaderText.gameObject : null, false);

            for (int i = 0; i < titleButtons.Count; i++)
                SetObjectActive(titleButtons[i] != null ? titleButtons[i].gameObject : null, false);
        }

        private void HideTitleSelector()
        {
            SetObjectActive(titleSelectHeaderText != null ? titleSelectHeaderText.gameObject : null, false);
            for (int i = 0; i < titleButtons.Count; i++)
                SetObjectActive(titleButtons[i] != null ? titleButtons[i].gameObject : null, false);
        }

        private void CreateTitleButtons(Transform parent, int count)
        {
            while (titleButtons.Count < count)
            {
                int index = titleButtons.Count;
                Button button = CreateTextButton(parent, "TitleSelectButton" + index, "-", 18f);
                TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                titleButtons.Add(button);
                titleButtonLabels.Add(label);
                titleButtonIds.Add(string.Empty);
            }
        }

        private void BindTitleButtons()
        {
            for (int i = 0; i < titleButtons.Count; i++)
            {
                Button button = titleButtons[i];
                if (button == null)
                    continue;

                int capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (capturedIndex >= 0 && capturedIndex < titleButtonIds.Count)
                        SelectTitle(titleButtonIds[capturedIndex]);
                });
            }
        }

        private void RefreshTitleButtons()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string selectedTitleId = string.Empty;
            IReadOnlyList<string> unlockedTitles = null;

            if (profile != null)
            {
                profile.EnsureData();
                if (profile.Mahjong != null)
                {
                    selectedTitleId = profile.Mahjong.SelectedTitleId ?? string.Empty;
                    unlockedTitles = profile.Mahjong.Titles;
                }
            }

            for (int i = 0; i < titleButtons.Count; i++)
            {
                bool hasTitle = unlockedTitles != null && i < unlockedTitles.Count && !string.IsNullOrWhiteSpace(unlockedTitles[i]);
                string titleId = hasTitle ? unlockedTitles[i].Trim() : string.Empty;
                titleButtonIds[i] = titleId;

                Button button = titleButtons[i];
                TextMeshProUGUI label = i < titleButtonLabels.Count ? titleButtonLabels[i] : null;

                if (button != null)
                    button.interactable = hasTitle && titleId != selectedTitleId;

                if (label == null)
                    continue;

                if (!hasTitle)
                {
                    label.text = "-";
                    label.color = new Color(0.55f, 0.62f, 0.72f, 1f);
                    continue;
                }

                bool selected = titleId == selectedTitleId;
                label.text = selected
                    ? GetTitleDisplayName(titleId) + "  " + GameLocalization.Text("profile.title_selected")
                    : GetTitleDisplayName(titleId);
                label.color = selected ? new Color(0.95f, 0.86f, 0.42f, 1f) : Color.white;
            }
        }

        private void LayoutTitleSelector(float x, float y, float width)
        {
            ConfigureText(titleSelectHeaderText, 22f, 14f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.86f, 0.93f, 1f, 1f));
            SetTopLeftRect(titleSelectHeaderText != null ? titleSelectHeaderText.rectTransform : null, x, y, width, 30f);

            const int columns = 2;
            float buttonHeight = 36f;
            float gap = 10f;
            float buttonWidth = Mathf.Max(120f, (width - gap * (columns - 1)) / columns);
            float startY = y - 36f;

            for (int i = 0; i < titleButtons.Count; i++)
            {
                RectTransform rect = titleButtons[i] != null ? titleButtons[i].transform as RectTransform : null;
                int row = i / columns;
                int column = i % columns;
                float rowY = startY - row * (buttonHeight + gap);
                float columnX = x + column * (buttonWidth + gap);

                SetObjectActive(titleButtons[i] != null ? titleButtons[i].gameObject : null, expanded);
                SetTopLeftRect(rect, columnX, rowY, buttonWidth, buttonHeight);

                if (i < titleButtonLabels.Count && titleButtonLabels[i] != null)
                    ConfigureText(titleButtonLabels[i], 17f, 11f, FontStyles.Bold, TextAlignmentOptions.Center, titleButtonLabels[i].color);
            }
        }

        private string GetTitleDisplayName(string titleId)
        {
            if (MahjongTitleService.I != null)
                return MahjongTitleService.I.GetTitleDisplayName(titleId);

            return string.IsNullOrWhiteSpace(titleId) ? string.Empty : titleId.Trim();
        }

        private static string ResolveSelectedTitleFallback(PlayerProfile profile)
        {
            if (profile == null)
                return string.Empty;

            if (profile.Mahjong != null && !string.IsNullOrWhiteSpace(profile.Mahjong.SelectedTitleId))
                return profile.Mahjong.SelectedTitleId.Trim();

            return string.IsNullOrWhiteSpace(profile.GlobalTitleId) ? string.Empty : profile.GlobalTitleId.Trim();
        }

        private TextMeshProUGUI CreateGeneratedText(Transform parent, string objectName, string text, float fontSize, FontStyles style)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetTopLeftRect(rect, 0f, 0f, 220f, 32f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            ConfigureText(label, fontSize, Mathf.Max(10f, fontSize * 0.58f), style, TextAlignmentOptions.Left, new Color(0.88f, 0.94f, 1f, 1f));
            return label;
        }

        private Button CreateTextButton(Transform parent, string objectName, string label, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.32f, 1f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            MainLobbyButtonStyle.Apply(button);

            TextMeshProUGUI text = CreateGeneratedText(buttonObject.transform, "Label", label, fontSize, FontStyles.Bold);
            ConfigureText(text, fontSize, 16f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private static void SetTextButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
        }

        private static void ConfigureText(
            TextMeshProUGUI label,
            float maxSize,
            float minSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color,
            TextWrappingModes wrappingMode = TextWrappingModes.NoWrap,
            TextOverflowModes overflowMode = TextOverflowModes.Ellipsis)
        {
            if (label == null)
                return;

            label.fontSize = maxSize;
            MainLobbyButtonStyle.ApplyFont(label);
            label.fontSizeMax = maxSize;
            label.fontSizeMin = minSize;
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = wrappingMode;
            label.overflowMode = overflowMode;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
        }

        private static void SetTopLeftRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetStretchRect(RectTransform rect, float left, float bottom, float right, float top)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void PlaceBehindFrame(RectTransform content, RectTransform frame)
        {
            if (content == null || frame == null || content.parent != frame.parent)
                return;

            int frameIndex = frame.GetSiblingIndex();
            content.SetSiblingIndex(frameIndex);
            frame.SetSiblingIndex(Mathf.Min(content.GetSiblingIndex() + 1, frame.parent.childCount - 1));
        }

        private static void RaiseAuxiliaryMenuRoots()
        {
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();

            FriendsUI friends = FindAnyObjectByType<FriendsUI>(FindObjectsInactive.Include);
            if (friends != null)
            {
                friends.LayoutToggleButton();
                friends.transform.SetAsLastSibling();
            }

            GlobalChatUI chat = FindAnyObjectByType<GlobalChatUI>(FindObjectsInactive.Include);
            if (chat != null)
            {
                chat.LayoutToggleButton();
                chat.transform.SetAsLastSibling();
            }
        }

        private static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
