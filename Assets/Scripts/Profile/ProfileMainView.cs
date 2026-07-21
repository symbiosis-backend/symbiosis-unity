using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileMainView : MonoBehaviour
    {
        private const string ProfileAvatarFrameResourcePath = "ProfileAvatars/ProfileAvatarFrameGenerated";

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
        private Canvas modalOverlayCanvas;
        private GraphicRaycaster modalOverlayRaycaster;
        private Image modalOverlayImage;
        private RectTransform modalOverlayRect;
        private RectTransform modalWindowRect;
        private RectTransform modalBackgroundRect;
        private Image modalBackgroundImage;
        private RectTransform modalFrameRect;
        private Image modalFrameImage;
        private RectTransform modalAvatarFrameRect;
        private RectTransform identityCardRect;
        private Image identityCardImage;
        private RectTransform detailsCardRect;
        private Image detailsCardImage;
        private TextMeshProUGUI modalHeaderText;
        private RectTransform privacyToggleRect;
        private Image privacyToggleImage;
        private Button compactButton;
        private Button overlayCloseButton;
        private Button modalWindowClickBlocker;
        private Button closeButton;
        private Button privacyToggleButton;
        private TextMeshProUGUI privacyToggleText;
        private TextMeshProUGUI titleSelectHeaderText;
        private readonly List<Button> titleButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> titleButtonLabels = new List<TextMeshProUGUI>();
        private readonly List<string> titleButtonIds = new List<string>();
        private static Sprite cachedProfileAvatarFrameSprite;
        private Transform uiParent;
        private bool expanded;

        private void OnEnable()
        {
            ProfileRuntimeBootstrap.EnsureServices();
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();
            AllianceBootstrap.EnsureForCurrentScene();
            EnsureGeneratedProfileInfo();
            EnsureRuntimeUi();
            ProfileService.ProfileChanged += Refresh;
            CurrencyService.CurrencyChanged += Refresh;
            if (AllianceService.I != null)
                AllianceService.I.AllianceChanged += Refresh;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            if (AllianceService.I != null && AllianceService.I.Current == null)
                StartCoroutine(AllianceService.I.Refresh());
            Refresh();
        }

        private void OnDisable()
        {
            expanded = false;
            SetObjectActive(modalOverlayRect != null ? modalOverlayRect.gameObject : null, false);
            ProfileService.ProfileChanged -= Refresh;
            CurrencyService.CurrencyChanged -= Refresh;
            if (AllianceService.I != null)
                AllianceService.I.AllianceChanged -= Refresh;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            UnbindButtons();
            MainGameLaunchBootstrap.RefreshVisibilityNow();
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

            nameText.text = AllianceIdentityFormatter.FormatOwnName(profile, GetFallbackName());
        }

        private void ApplyAvatar(PlayerProfile profile)
        {
            if (avatarImage == null)
                return;

            Sprite spriteToUse = fallbackAvatar;
            Sprite resourceSprite = ProfileAvatarResources.GetDisplaySprite(profile);
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
            string dynastyLine = string.IsNullOrWhiteSpace(dynastyName)
                ? GameLocalization.Text("profile.dynasty_empty")
                : GameLocalization.Format("profile.dynasty", dynastyName.Trim());

            dynastyText.text = dynastyLine + "\n" + BuildAllianceProfileLine(profile);
        }

        private static string BuildAllianceProfileLine(PlayerProfile profile)
        {
            string allianceName = AllianceIdentityFormatter.ResolveOwnName(profile);
            string allianceTag = AllianceIdentityFormatter.ResolveOwnTag(profile);
            string label = GameLocalization.Text("alliance.title");
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "alliance.title", System.StringComparison.Ordinal))
                label = "Alliance";

            if (string.IsNullOrWhiteSpace(allianceName) && string.IsNullOrWhiteSpace(allianceTag))
                return label + ": -";

            if (string.IsNullOrWhiteSpace(allianceName))
                return label + ": [" + allianceTag + "]";

            return string.IsNullOrWhiteSpace(allianceTag)
                ? label + ": " + allianceName
                : label + ": " + allianceName + " [" + allianceTag + "]";
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
            if (!MainHubStateController.CanOpenMainWindow("Profile"))
                return;

            expanded = true;
            SettingsMenuUI.ForceCloseAllSettingsMenus();
            MainLobbyUiCoordinator.SetRightStackSuppressed(true);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            if (modalOverlayRect != null)
                modalOverlayRect.SetAsLastSibling();
            RaiseAuxiliaryMenuRoots();
            LayoutProfileInfo();
            MainGameLaunchBootstrap.RefreshVisibilityNow();

            ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(modalOverlayRect);
            if (intro != null)
            {
                intro.TryShowForCurrentProfile(
                    "profile",
                    "main.info.profile.title",
                    "main.info.profile.body",
                    "main.intro.profile.white");
            }
        }

        private void CloseProfile()
        {
            expanded = false;
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            LayoutProfileInfo();
            MainGameLaunchBootstrap.RefreshVisibilityNow();
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
                compactButton.image.preserveAspect = false;
                MainInfoHintTarget.Attach(compactButton, "main.info.profile.title", "main.info.profile.body");

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
                ApplyProfileAvatarFrame(frameImage);
            }
            else if (compactParent != null && compactAvatarFrameRect.parent != compactParent)
            {
                compactAvatarFrameRect.SetParent(compactParent, false);
                compactAvatarFrameRect.SetAsFirstSibling();
            }

            if (compactAvatarFrameRect != null)
                ApplyProfileAvatarFrame(compactAvatarFrameRect.GetComponent<Image>());

            if (modalOverlayRect == null)
            {
                GameObject overlay = new GameObject("ProfileModalOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GraphicRaycaster));
                overlay.transform.SetParent(overlayParent, false);
                overlay.transform.SetAsLastSibling();
                modalOverlayRect = overlay.GetComponent<RectTransform>();
                modalOverlayCanvas = overlay.GetComponent<Canvas>();
                modalOverlayRaycaster = overlay.GetComponent<GraphicRaycaster>();
                ConfigureModalOverlayCanvas();
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
                EnsureModalDecorations();

                GameObject modalAvatarFrame = new GameObject("ModalAvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                modalAvatarFrame.transform.SetParent(window.transform, false);
                modalAvatarFrameRect = modalAvatarFrame.GetComponent<RectTransform>();
                Image avatarFrameImage = modalAvatarFrame.GetComponent<Image>();
                avatarFrameImage.color = Color.white;
                avatarFrameImage.raycastTarget = false;
                ApplyProfileAvatarFrame(avatarFrameImage);

                closeButton = CreateTextButton(window.transform, "CloseProfileButton", GameLocalization.Text("settings.close"), 24f);
                MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
                privacyToggleButton = CreateTextButton(window.transform, "ProfilePrivacyToggle", GameLocalization.Text("profile.privacy.public"), 22f);
                privacyToggleRect = privacyToggleButton.transform as RectTransform;
                privacyToggleImage = privacyToggleButton.GetComponent<Image>();
                privacyToggleText = privacyToggleButton.GetComponentInChildren<TextMeshProUGUI>(true);

                titleSelectHeaderText = CreateGeneratedText(window.transform, "TitleSelectHeader", GameLocalization.Text("profile.titles"), 24f, FontStyles.Bold);
                SetObjectActive(titleSelectHeaderText.gameObject, false);
            }
            else if (overlayParent != null && modalOverlayRect.parent != overlayParent)
            {
                modalOverlayRect.SetParent(overlayParent, false);
            }

            ConfigureModalOverlayCanvas();

            if (modalAvatarFrameRect != null)
                ApplyProfileAvatarFrame(modalAvatarFrameRect.GetComponent<Image>());

            if (modalWindowRect != null)
            {
                ConfigureModalWindowRoot(modalWindowRect.GetComponent<Image>());
                EnsureModalWindowLayers();
                EnsureModalDecorations();
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

        private void ConfigureModalOverlayCanvas()
        {
            if (modalOverlayRect == null)
                return;

            if (modalOverlayCanvas == null)
                modalOverlayCanvas = modalOverlayRect.GetComponent<Canvas>();
            if (modalOverlayCanvas == null)
                modalOverlayCanvas = modalOverlayRect.gameObject.AddComponent<Canvas>();

            modalOverlayCanvas.overrideSorting = true;
            Canvas parentCanvas = modalOverlayRect.parent != null
                ? modalOverlayRect.parent.GetComponentInParent<Canvas>()
                : null;
            modalOverlayCanvas.sortingLayerID = parentCanvas != null ? parentCanvas.sortingLayerID : 0;
            modalOverlayCanvas.sortingOrder = 32760;

            if (modalOverlayRaycaster == null)
                modalOverlayRaycaster = modalOverlayRect.GetComponent<GraphicRaycaster>();
            if (modalOverlayRaycaster == null)
                modalOverlayRaycaster = modalOverlayRect.gameObject.AddComponent<GraphicRaycaster>();
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
                MainLobbyButtonStyle.ApplyDlsWindow(modalBackgroundImage);
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

        private void EnsureModalDecorations()
        {
            if (modalWindowRect == null)
                return;

            identityCardRect = EnsureSolidPanel(
                modalWindowRect,
                "ProfileIdentityCard",
                identityCardRect,
                ref identityCardImage,
                new Color(0.012f, 0.045f, 0.078f, 0.7f));
            detailsCardRect = EnsureSolidPanel(
                modalWindowRect,
                "ProfileDetailsCard",
                detailsCardRect,
                ref detailsCardImage,
                new Color(0.012f, 0.045f, 0.078f, 0.56f));

            if (modalHeaderText == null)
            {
                Transform existing = modalWindowRect.Find("ProfileModalHeader");
                modalHeaderText = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            }

            if (modalHeaderText == null)
                modalHeaderText = CreateGeneratedText(modalWindowRect, "ProfileModalHeader", GameLocalization.Text("menu.profile"), 38f, FontStyles.Bold);

            if (modalBackgroundRect != null)
                modalBackgroundRect.SetAsFirstSibling();
            if (identityCardRect != null)
                identityCardRect.SetSiblingIndex(Mathf.Min(1, modalWindowRect.childCount - 1));
            if (detailsCardRect != null)
                detailsCardRect.SetSiblingIndex(Mathf.Min(2, modalWindowRect.childCount - 1));
        }

        private static RectTransform EnsureSolidPanel(
            Transform parent,
            string objectName,
            RectTransform cachedRect,
            ref Image cachedImage,
            Color color)
        {
            if (cachedRect == null && parent != null)
            {
                Transform existing = parent.Find(objectName);
                cachedRect = existing as RectTransform;
            }

            if (cachedRect == null)
            {
                GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(parent, false);
                cachedRect = panel.transform as RectTransform;
            }

            if (cachedImage == null)
                cachedImage = cachedRect.GetComponent<Image>();
            if (cachedImage == null)
                cachedImage = cachedRect.gameObject.AddComponent<Image>();

            cachedImage.sprite = null;
            cachedImage.color = color;
            cachedImage.raycastTarget = false;
            return cachedRect;
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

            if (privacyToggleButton != null)
            {
                privacyToggleButton.onClick.RemoveListener(ToggleProfilePrivacy);
                privacyToggleButton.onClick.AddListener(ToggleProfilePrivacy);
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

            if (privacyToggleButton != null)
                privacyToggleButton.onClick.RemoveListener(ToggleProfilePrivacy);

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
            MainLobbyUiCoordinator.LayoutProfileAvatarFrame(compactAvatarFrameRect);
            SetObjectActive(compactAvatarFrameRect != null ? compactAvatarFrameRect.gameObject : null, !expanded);

            MainLobbyUiCoordinator.LayoutLeftMenuButton(compactButtonRect, MainLobbyLeftMenuSlot.Profile);

            if (avatarImage != null)
            {
                Transform avatarParent = compactAvatarFrameRect != null && compactAvatarFrameRect.parent != null
                    ? compactAvatarFrameRect.parent
                    : compactButtonRect;
                avatarImage.transform.SetParent(avatarParent, false);
                MainLobbyUiCoordinator.LayoutProfileAvatar(avatarImage.rectTransform);
                avatarImage.preserveAspect = false;
                PlaceBehindFrame(avatarImage.rectTransform, compactAvatarFrameRect);
            }

            ConfigureText(openProfileText, 38f, 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            if (openProfileText != null)
                openProfileText.text = GameLocalization.Text("menu.profile");
            if (openProfileText != null && compactButtonRect != null && openProfileText.transform.parent != compactButtonRect)
                openProfileText.transform.SetParent(compactButtonRect, false);
            MainLobbyButtonStyle.ApplyButtonLabelLayout(openProfileText);

            SetTextVisible(false);
        }

        private void LayoutModal()
        {
            Stretch(modalOverlayRect);

            float rootWidth = rootRect.rect.width > 0f ? rootRect.rect.width : Screen.width;
            float rootHeight = rootRect.rect.height > 0f ? rootRect.rect.height : Screen.height;
            float marginX = Mathf.Clamp(rootWidth * 0.025f, 18f, 56f);
            float marginY = Mathf.Clamp(rootHeight * 0.035f, 16f, 42f);
            float windowWidth = rootWidth - marginX * 2f;
            float windowHeight = rootHeight - marginY * 2f;
            if (windowWidth <= 0f || windowHeight <= 0f)
                return;

            bool compactModal = windowWidth < 1350f || windowHeight < 700f;
            SetTopLeftRect(modalWindowRect, marginX, -marginY, windowWidth, windowHeight);
            EnsureModalWindowLayers();
            EnsureModalDecorations();
            SetStretchRect(modalBackgroundRect, 0f, 0f, 0f, 0f);
            SetStretchRect(modalFrameRect, 0f, 0f, 0f, 0f);

            float closeSize = Mathf.Clamp(windowHeight * 0.085f, 64f, 86f);
            SetTextButtonLabel(closeButton, string.Empty);
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            SetTopLeftRect(closeButton != null ? closeButton.transform as RectTransform : null, windowWidth - closeSize - 26f, -28f, closeSize, closeSize);

            if (modalHeaderText != null)
            {
                modalHeaderText.text = GameLocalization.Text("menu.profile");
                ConfigureText(modalHeaderText, compactModal ? 34f : 42f, 24f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
                SetTopLeftRect(modalHeaderText.rectTransform, windowWidth * 0.5f - 240f, -34f, 480f, 58f);
                modalHeaderText.transform.SetAsLastSibling();
            }

            float cardTop = compactModal ? 104f : 122f;
            float cardBottom = compactModal ? 54f : 68f;
            float outerX = Mathf.Clamp(windowWidth * 0.038f, 34f, 74f);
            float cardGap = Mathf.Clamp(windowWidth * 0.022f, 22f, 40f);
            float contentWidth = windowWidth - outerX * 2f;
            float cardHeight = Mathf.Max(300f, windowHeight - cardTop - cardBottom);
            float minimumDetailsWidth = compactModal ? 430f : 620f;
            float preferredIdentityWidth = Mathf.Clamp(contentWidth * 0.32f, compactModal ? 300f : 390f, 590f);
            float identityWidth = Mathf.Min(preferredIdentityWidth, Mathf.Max(270f, contentWidth - cardGap - minimumDetailsWidth));
            float detailsX = outerX + identityWidth + cardGap;
            float detailsWidth = Mathf.Max(300f, windowWidth - detailsX - outerX);

            SetTopLeftRect(identityCardRect, outerX, -cardTop, identityWidth, cardHeight);
            SetTopLeftRect(detailsCardRect, detailsX, -cardTop, detailsWidth, cardHeight);

            float frameSize = Mathf.Clamp(
                Mathf.Min(identityWidth * 0.68f, cardHeight * 0.54f),
                compactModal ? 178f : 220f,
                350f);
            float avatarSize = frameSize * 0.79f;
            float avatarInset = (frameSize - avatarSize) * 0.5f;
            float avatarFrameX = outerX + (identityWidth - frameSize) * 0.5f;
            float avatarFrameY = -cardTop - Mathf.Clamp(cardHeight * 0.055f, 20f, 34f);

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

            float identityPadding = Mathf.Clamp(identityWidth * 0.07f, 20f, 34f);
            float nameY = avatarFrameY - frameSize - 18f;
            ConfigureText(nameText, compactModal ? 34f : 42f, compactModal ? 20f : 26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetTopLeftRect(nameText != null ? nameText.rectTransform : null, outerX + identityPadding, nameY, identityWidth - identityPadding * 2f, 58f);

            float detailsPadding = Mathf.Clamp(detailsWidth * 0.045f, 28f, 48f);
            float detailsTopY = -cardTop - Mathf.Clamp(cardHeight * 0.065f, 24f, 40f);
            float slotWidth = Mathf.Clamp(detailsWidth * 0.22f, 130f, 210f);
            float dynastyWidth = Mathf.Max(180f, detailsWidth - detailsPadding * 2f - slotWidth - 20f);

            ConfigureText(dynastyText, compactModal ? 25f : 31f, compactModal ? 17f : 21f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.88f, 0.95f, 1f, 1f), TextWrappingModes.Normal);
            SetTopLeftRect(dynastyText != null ? dynastyText.rectTransform : null, detailsX + detailsPadding, detailsTopY, dynastyWidth, compactModal ? 76f : 88f);

            ConfigureText(slotText, compactModal ? 22f : 27f, 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.88f, 1f, 1f));
            SetTopLeftRect(slotText != null ? slotText.rectTransform : null, detailsX + detailsWidth - detailsPadding - slotWidth, detailsTopY + 2f, slotWidth, 42f);

            float infoFontSize = compactModal ? 23f : 29f;
            float infoLineHeight = compactModal ? 42f : 50f;
            float infoGap = compactModal ? 8f : 12f;
            float infoStartY = detailsTopY - (compactModal ? 108f : 126f);
            float infoWidth = detailsWidth - detailsPadding * 2f;
            Color primaryInfoColor = new Color(0.8f, 0.89f, 1f, 1f);
            Color secondaryInfoColor = new Color(0.68f, 0.79f, 0.92f, 1f);

            ConfigureText(titleText, infoFontSize, compactModal ? 15f : 19f, FontStyles.Normal, TextAlignmentOptions.Left, primaryInfoColor);
            SetTopLeftRect(titleText != null ? titleText.rectTransform : null, detailsX + detailsPadding, infoStartY, infoWidth, infoLineHeight);

            ConfigureText(rankText, infoFontSize, compactModal ? 15f : 19f, FontStyles.Normal, TextAlignmentOptions.Left, primaryInfoColor);
            SetTopLeftRect(rankText != null ? rankText.rectTransform : null, detailsX + detailsPadding, infoStartY - (infoLineHeight + infoGap), infoWidth, infoLineHeight);

            ConfigureText(publicIdText, infoFontSize, compactModal ? 15f : 19f, FontStyles.Normal, TextAlignmentOptions.Left, secondaryInfoColor);
            SetTopLeftRect(publicIdText != null ? publicIdText.rectTransform : null, detailsX + detailsPadding, infoStartY - (infoLineHeight + infoGap) * 2f, infoWidth, infoLineHeight);

            ConfigureText(ageGenderText, infoFontSize, compactModal ? 15f : 19f, FontStyles.Normal, TextAlignmentOptions.Left, secondaryInfoColor);
            SetTopLeftRect(ageGenderText != null ? ageGenderText.rectTransform : null, detailsX + detailsPadding, infoStartY - (infoLineHeight + infoGap) * 3f, infoWidth, infoLineHeight);

            HideTitleSelector();
            bool profilePublic = ProfileService.I == null || ProfileService.I.Current == null || ProfileService.I.Current.IsProfilePublic;
            SetTextButtonLabel(privacyToggleButton, profilePublic ? GameLocalization.Text("profile.privacy.public") : GameLocalization.Text("profile.privacy.private"));
            ConfigureText(privacyToggleText, compactModal ? 20f : 24f, compactModal ? 13f : 15f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            if (privacyToggleImage != null)
                privacyToggleImage.color = profilePublic ? new Color(0.12f, 0.42f, 0.32f, 1f) : new Color(0.42f, 0.18f, 0.18f, 1f);
            float privacyWidth = Mathf.Max(190f, identityWidth - identityPadding * 2f);
            float privacyHeight = compactModal ? 54f : 62f;
            float privacyY = -cardTop - cardHeight + privacyHeight + Mathf.Clamp(cardHeight * 0.055f, 20f, 34f);
            SetTopLeftRect(privacyToggleRect, outerX + identityPadding, privacyY, privacyWidth, privacyHeight);
            if (privacyToggleButton != null)
                privacyToggleButton.transform.SetAsLastSibling();

            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        private void ToggleProfilePrivacy()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || ProfileService.I == null)
                return;

            profile.EnsureData();
            ProfileService.I.SetProfilePublic(!profile.IsProfilePublic);
            Refresh();
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
            MainLobbyButtonStyle.ApplyButtonLabelLayout(text);
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
            TextOverflowModes overflowMode = TextOverflowModes.Truncate)
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

        private static void ApplyProfileAvatarFrame(Image image)
        {
            if (image == null)
                return;

            if (cachedProfileAvatarFrameSprite == null)
            {
                cachedProfileAvatarFrameSprite = Resources.Load<Sprite>(ProfileAvatarFrameResourcePath);
                if (cachedProfileAvatarFrameSprite == null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(ProfileAvatarFrameResourcePath);
                    if (sprites != null && sprites.Length > 0)
                        cachedProfileAvatarFrameSprite = sprites[0];
                }
            }

            if (cachedProfileAvatarFrameSprite != null)
            {
                image.sprite = cachedProfileAvatarFrameSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                MainLobbyButtonStyle.ApplyAvatarCard(image);
            }

            image.raycastTarget = false;
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
