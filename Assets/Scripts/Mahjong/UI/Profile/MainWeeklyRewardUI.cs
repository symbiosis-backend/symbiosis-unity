using MahjongGame.Monetization;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class MainWeeklyRewardBootstrap
    {
        private const string MainSceneName = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        public static void EnsureForCurrentScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene != SceneManager.GetActiveScene() || scene.name != MainSceneName)
                return;

            Canvas canvas = CentralPointLayout.ResolveMainCanvas();
            if (canvas == null)
                return;

            ProfileRuntimeBootstrap.EnsureServices();
            MonetizationService.Ensure();

            MainWeeklyRewardUI keep = null;
            MainWeeklyRewardUI[] all = UnityEngine.Object.FindObjectsByType<MainWeeklyRewardUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MainWeeklyRewardUI candidate = all[i];
                if (candidate == null)
                    continue;

                if (keep == null && candidate.gameObject.scene == scene)
                {
                    keep = candidate;
                    continue;
                }

                candidate.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(candidate.gameObject);
            }

            if (keep != null)
            {
                if (keep.transform.parent != canvas.transform)
                    keep.transform.SetParent(canvas.transform, false);

                keep.gameObject.SetActive(true);
                keep.ForceMainMenuLayout();
                return;
            }

            // Parent the host before OnEnable builds its button. This prevents the
            // default centred RectTransform from flashing on narrow Android devices.
            GameObject host = new GameObject("MainWeeklyRewardUI", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            MainWeeklyRewardUI created = host.AddComponent<MainWeeklyRewardUI>();
            created.ForceMainMenuLayout();
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainWeeklyRewardUI : DynastyEconomyWindowBase
    {
        private const int DayCount = 7;
        private const string CelestialFrameResourcePath = "Mahjong/Sprites/Rewards/WeeklyMain/WeeklyRewardMainCinematicV4";

        private static Sprite cachedRewardIconBackdropSprite;
        private static Sprite cachedCelestialFrameSprite;
        private static Sprite cachedRoundedRectSprite;
        private static Sprite cachedLeftGlyphSprite;
        private static Sprite cachedRightGlyphSprite;
        private static Sprite cachedCloseGlyphSprite;
        private Image fullscreenBackgroundImage;
        private RectTransform headerDivider;
        private RectTransform featuredRewardGlow;
        private RectTransform featuredRewardPanel;
        private RectTransform featuredRewardInner;
        private Image featuredRewardPanelImage;
        private Image featuredRewardGlowImage;
        private Outline featuredRewardOutline;
        private Image featuredRewardIconBackdrop;
        private Image featuredRewardIcon;
        private RectTransform featuredStateBadge;
        private Image featuredStateBadgeImage;
        private TextMeshProUGUI featuredStateText;
        private TextMeshProUGUI subtitleText;
        private RectTransform weekPanel;
        private Image weekPanelImage;
        private TextMeshProUGUI weekTitleText;
        private TextMeshProUGUI weekSubtitleText;
        private TextMeshProUGUI carouselPageText;
        private Button previousDayButton;
        private Button nextDayButton;
        private RectTransform statusPanel;
        private Image statusPanelImage;
        private RectTransform progressTrack;
        private Image progressFill;
        private Button freeButton;
        private Button adButton;
        private TextMeshProUGUI todayText;
        private TextMeshProUGUI rewardTitleText;
        private TextMeshProUGUI rewardAmountText;
        private TextMeshProUGUI rewardBonusText;
        private TextMeshProUGUI progressText;
        private readonly RectTransform[] dayCards = new RectTransform[DayCount];
        private readonly Image[] dayCardImages = new Image[DayCount];
        private readonly Image[] dayArtFrames = new Image[DayCount];
        private readonly Image[] dayCardAccents = new Image[DayCount];
        private readonly Outline[] dayCardOutlines = new Outline[DayCount];
        private readonly CanvasGroup[] dayCardGroups = new CanvasGroup[DayCount];
        private readonly Button[] dayCardButtons = new Button[DayCount];
        private readonly Image[] dayIcons = new Image[DayCount];
        private readonly TextMeshProUGUI[] dayLabels = new TextMeshProUGUI[DayCount];
        private readonly TextMeshProUGUI[] dayStateLabels = new TextMeshProUGUI[DayCount];
        private Coroutine openAnimationRoutine;
        private Coroutine carouselAnimationRoutine;
        private Coroutine fullscreenRelayoutRoutine;
        private int selectedDayIndex = -1;
        private int currentDayIndex;
        private bool deferCarouselLayout;
        private bool rewardedAdRequestInProgress;

        protected override string ButtonObjectName => "MainWeeklyRewardButton";
        protected override string OverlayObjectName => "MainWeeklyRewardOverlay";
        protected override string ButtonText => T("Награды", "Rewards", "Ödüller");
        protected override string TitleText => T("Недельные награды", "Weekly Rewards", "Haftalık Ödüller");
        protected override Vector2 ButtonPosition => MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Weekly);
        protected override Color AccentColor => new Color(0.22f, 0.16f, 0.08f, 0.96f);
        protected override MainLobbyLeftMenuSlot? MainMenuSlot => MainLobbyLeftMenuSlot.Weekly;

        protected override void OnEnable()
        {
            ProfileRuntimeBootstrap.EnsureServices();
            MonetizationService.Ensure();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (openAnimationRoutine != null)
            {
                StopCoroutine(openAnimationRoutine);
                openAnimationRoutine = null;
            }

            if (carouselAnimationRoutine != null)
            {
                StopCoroutine(carouselAnimationRoutine);
                carouselAnimationRoutine = null;
            }

            if (fullscreenRelayoutRoutine != null)
            {
                StopCoroutine(fullscreenRelayoutRoutine);
                fullscreenRelayoutRoutine = null;
            }

            if (ShouldRestoreMainUiOnDisable())
                SetMainUiVisible(true, false);

            base.OnDisable();
        }

        private bool ShouldRestoreMainUiOnDisable()
        {
            if (!Application.isPlaying)
                return false;

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid()
                && gameObject.scene.IsValid()
                && activeScene == gameObject.scene
                && string.Equals(activeScene.name, "Main", StringComparison.Ordinal);
        }

        protected override void BuildContent(Transform window)
        {
            ConfigureDedicatedWindowChrome(window);
            subtitleText = CreateText(window, "WeeklySubtitle", string.Empty, 24f, FontStyles.Normal, new Color(0.78f, 0.76f, 0.91f, 1f));
            headerDivider = CreatePanel(window, "WeeklyHeaderDivider", Color.clear);

            featuredRewardGlow = CreatePanel(window, "WeeklyFeaturedGlow", new Color(0.22f, 0.58f, 1f, 0.07f));
            featuredRewardGlowImage = featuredRewardGlow != null ? featuredRewardGlow.GetComponent<Image>() : null;
            if (featuredRewardGlowImage != null)
            {
                ApplyRoundedSprite(featuredRewardGlowImage, new Color(0.22f, 0.58f, 1f, 0.09f));
                featuredRewardGlowImage.enabled = true;
                featuredRewardGlowImage.raycastTarget = false;
            }

            featuredRewardPanel = CreatePanel(window, "WeeklySelectedReward", new Color(0.012f, 0.035f, 0.075f, 0.74f));
            featuredRewardPanelImage = featuredRewardPanel != null ? featuredRewardPanel.GetComponent<Image>() : null;
            if (featuredRewardPanelImage != null)
            {
                ApplyRoundedSprite(featuredRewardPanelImage, new Color(0.010f, 0.040f, 0.082f, 0.98f));
                featuredRewardOutline = AddOutline(featuredRewardPanelImage.gameObject, new Color(0.42f, 0.72f, 1f, 0.42f), new Vector2(1.5f, -1.5f));
                AddShadow(featuredRewardPanelImage.gameObject, new Color(0f, 0f, 0f, 0.54f), new Vector2(0f, -8f));
                featuredRewardPanelImage.enabled = true;
            }

            featuredRewardInner = CreatePanel(featuredRewardPanel, "RewardInnerBacking", new Color(0.004f, 0.018f, 0.044f, 0.78f));
            Image featuredInnerImage = featuredRewardInner != null ? featuredRewardInner.GetComponent<Image>() : null;
            ApplyRoundedSprite(featuredInnerImage, new Color(0.004f, 0.020f, 0.046f, 0.94f));
            if (featuredRewardInner != null)
                featuredRewardInner.SetAsFirstSibling();

            featuredRewardIconBackdrop = CreateRewardIconBackdrop(featuredRewardPanel, "RewardIconGlow");
            featuredRewardIcon = CreateRewardIcon(featuredRewardPanel, "RewardIcon", 0);
            todayText = CreateText(featuredRewardPanel, "WeeklyTodayText", string.Empty, 24f, FontStyles.Bold, new Color(1f, 0.79f, 0.30f, 1f));
            rewardTitleText = CreateText(featuredRewardPanel, "RewardTitle", string.Empty, 36f, FontStyles.Bold, new Color(0.86f, 0.94f, 1f, 1f));
            rewardAmountText = CreateText(featuredRewardPanel, "RewardAmount", string.Empty, 54f, FontStyles.Bold, new Color(1f, 0.82f, 0.34f, 1f));
            rewardBonusText = CreateText(featuredRewardPanel, "RewardBonus", string.Empty, 29f, FontStyles.Bold, new Color(0.80f, 0.68f, 1f, 1f));
            progressText = CreateText(featuredRewardPanel, "RewardProgress", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.90f, 1f, 1f));
            ConfigureCinematicRewardText(todayText);
            ConfigureCinematicRewardText(rewardTitleText);
            ConfigureCinematicRewardText(rewardAmountText);
            ConfigureCinematicRewardText(rewardBonusText);
            progressTrack = CreatePanel(featuredRewardPanel, "ProgressTrack", new Color(0f, 0f, 0f, 0.42f));
            RectTransform fillRect = CreatePanel(progressTrack, "ProgressFill", new Color(1f, 0.74f, 0.22f, 0.95f));
            progressFill = fillRect != null ? fillRect.GetComponent<Image>() : null;
            ApplyRoundedSprite(progressTrack != null ? progressTrack.GetComponent<Image>() : null, new Color(0.015f, 0.035f, 0.070f, 0.94f));
            ApplyRoundedSprite(progressFill, new Color(1f, 0.74f, 0.22f, 0.98f));

            featuredStateBadge = CreatePanel(featuredRewardPanel, "RewardStateBadge", new Color(0.22f, 0.58f, 0.42f, 0.94f));
            featuredStateBadgeImage = featuredStateBadge != null ? featuredStateBadge.GetComponent<Image>() : null;
            featuredStateText = CreateText(featuredStateBadge, "Label", string.Empty, 18f, FontStyles.Bold, Color.white);
            CenterSlotText(featuredStateText);
            Stretch(featuredStateText != null ? featuredStateText.rectTransform : null);

            weekPanel = CreatePanel(window, "WeeklyCarousel", new Color(0f, 0f, 0f, 0.001f));
            weekPanelImage = weekPanel != null ? weekPanel.GetComponent<Image>() : null;
            if (weekPanelImage != null)
            {
                ApplyRoundedSprite(weekPanelImage, new Color(0.006f, 0.026f, 0.056f, 0.95f));
                AddOutline(weekPanelImage.gameObject, new Color(0.30f, 0.61f, 0.92f, 0.24f), new Vector2(1f, -1f));
                weekPanelImage.raycastTarget = true;
            }

            WeeklyCarouselDragHandler dragHandler = weekPanel.gameObject.AddComponent<WeeklyCarouselDragHandler>();
            dragHandler.Initialize(SelectPreviousDay, SelectNextDay);

            weekTitleText = CreateText(weekPanel, "WeeklyCarouselTitle", string.Empty, 30f, FontStyles.Bold, new Color(0.91f, 0.94f, 1f, 1f));
            weekSubtitleText = CreateText(weekPanel, "WeeklyCarouselHint", string.Empty, 24f, FontStyles.Bold, new Color(0.76f, 0.86f, 0.98f, 0.96f));
            carouselPageText = CreateText(weekPanel, "WeeklyCarouselPages", string.Empty, 20f, FontStyles.Bold, new Color(0.84f, 0.68f, 1f, 1f));
            BuildDayCards(weekPanel);
            previousDayButton = CreateCarouselArrow(weekPanel, "WeeklyPreviousDay", "<", SelectPreviousDay);
            nextDayButton = CreateCarouselArrow(weekPanel, "WeeklyNextDay", ">", SelectNextDay);

            statusPanel = CreatePanel(window, "WeeklyStatusPanel", new Color(0.04f, 0.10f, 0.16f, 0.92f));
            statusPanelImage = statusPanel != null ? statusPanel.GetComponent<Image>() : null;
            if (statusPanelImage != null)
            {
                ApplyRoundedSprite(statusPanelImage, new Color(0.006f, 0.026f, 0.060f, 0.78f));
                AddOutline(statusPanelImage.gameObject, new Color(0.31f, 0.67f, 1f, 0.18f), new Vector2(1f, -1f));
                statusPanelImage.enabled = true;
            }

            freeButton = CreateButton(window, "WeeklyFreeButton", string.Empty, 34f);
            adButton = CreateButton(window, "WeeklyAdButton", string.Empty, 34f);
            freeButton.onClick.AddListener(ClaimFree);
            adButton.onClick.AddListener(ClaimAd);

            ConfigureRewardButton(freeButton, new Color(1f, 0.72f, 0.18f, 1f));
            ConfigureRewardButton(adButton, new Color(0.66f, 0.43f, 1f, 1f));
            CreateButtonAccent(freeButton, "FreeAccent", new Color(1f, 0.70f, 0.18f, 0.92f));
            CreateButtonAccent(adButton, "AdAccent", new Color(0.70f, 0.42f, 1f, 0.92f));

            MainLobbyButtonStyle.ApplySilverTextEffect(titleText);
            MainLobbyButtonStyle.ApplySilverTextEffect(weekTitleText);
            MainLobbyButtonStyle.ApplySilverTextEffect(rewardTitleText);

            if (messageText != null)
                messageText.transform.SetAsLastSibling();

        }

        protected override void Layout()
        {
            SetMainMenuButton(buttonRect, ButtonPosition, MainMenuSlot, MainLobbyUiCoordinator.LeftMenuWidth, MainLobbyUiCoordinator.LeftMenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 34f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            RectTransform rootRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float measuredWidth = rootRect != null ? rootRect.rect.width : 0f;
            float measuredHeight = rootRect != null ? rootRect.rect.height : 0f;
            Canvas rootCanvas = overlayRect != null ? overlayRect.GetComponentInParent<Canvas>()?.rootCanvas : null;
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            float scaleFactor = rootCanvas != null ? Mathf.Max(0.01f, rootCanvas.scaleFactor) : 1f;
            float screenWidth = Screen.width > 0 ? Screen.width / scaleFactor : 0f;
            float screenHeight = Screen.height > 0 ? Screen.height / scaleFactor : 0f;
            float rootWidth = Mathf.Max(measuredWidth, canvasWidth, screenWidth);
            float rootHeight = Mathf.Max(measuredHeight, canvasHeight, screenHeight);
            if (rootWidth <= 8f)
                rootWidth = 1920f;
            if (rootHeight <= 8f)
                rootHeight = 1080f;
            Rect safeArea = Screen.safeArea;
            float safeLeft = safeArea.xMin / scaleFactor;
            float safeRight = Mathf.Max(0f, Screen.width - safeArea.xMax) / scaleFactor;
            float safeTop = Mathf.Max(0f, Screen.height - safeArea.yMax) / scaleFactor;
            float safeBottom = safeArea.yMin / scaleFactor;
            float outerMarginX = Mathf.Clamp(rootWidth * 0.026f, 24f, 60f);
            float outerMarginY = Mathf.Clamp(rootHeight * 0.040f, 20f, 48f);
            float windowLeft = outerMarginX + safeLeft;
            float windowRight = outerMarginX + safeRight;
            float windowTop = outerMarginY + safeTop;
            float windowBottom = outerMarginY + safeBottom;
            float width = Mathf.Max(840f, rootWidth - windowLeft - windowRight);
            float height = Mathf.Max(560f, rootHeight - windowTop - windowBottom);
            float pad = Mathf.Clamp(width * 0.060f, 62f, 128f);
            float closeWidth = Mathf.Clamp(height * 0.078f, 48f, 70f);
            float walletWidth = Mathf.Clamp(width * 0.105f, 138f, 190f);
            float walletGap = 12f;
            float walletStartX = width - pad - closeWidth - 34f - walletWidth * 2f - walletGap;

            SetTopLeft(windowRect, windowLeft, -windowTop, width, height);
            SetObjectActive(contentPanelRect != null ? contentPanelRect.gameObject : null, false);
            bool compactHeader = width < 1700f || height < 860f;
            float titleWidth = compactHeader
                ? Mathf.Max(220f, walletStartX - pad - 24f)
                : Mathf.Clamp(width * 0.34f, 420f, 660f);
            float titleX = compactHeader ? pad : (width - titleWidth) * 0.5f;
            float titleY = height < 700f ? -20f : compactHeader ? -26f : -34f;
            float walletY = height < 700f ? -42f : compactHeader ? -52f : -68f;
            float dividerY = height < 700f ? -88f : compactHeader ? -106f : -126f;
            SetTopLeft(titleText != null ? titleText.rectTransform : null, titleX, titleY, titleWidth, 48f);
            SetTopLeft(subtitleText != null ? subtitleText.rectTransform : null, titleX, compactHeader ? -66f : -78f, titleWidth, 28f);
            SetObjectActive(subtitleText != null ? subtitleText.gameObject : null, !compactHeader);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, width - closeWidth - 30f, height < 700f ? -16f : -24f, closeWidth, closeWidth);
            SetIconLabelRow(profileGoldIcon, profileGoldText, walletStartX, walletY, walletWidth, 38f, 30f, 9f);
            SetIconLabelRow(profileAmetistIcon, profileAmetistText, walletStartX + walletWidth + walletGap, walletY, walletWidth, 38f, 30f, 9f);
            SetTopLeft(headerDivider, pad, dividerY, width - pad * 2f, 1.5f);
            SetTopLeft(fullscreenBackgroundImage != null ? fullscreenBackgroundImage.rectTransform : null, pad - 14f, dividerY - 6f, width - (pad - 14f) * 2f, height + dividerY - 140f);
            ConfigureCloseButtonChrome();
            ConfigureHeaderText(titleText, 48f, 31f);
            ConfigureHeaderText(profileGoldText, 30f, 20f);
            ConfigureHeaderText(profileAmetistText, 30f, 20f);
            ConfigureSubtitleText(subtitleText);
            if (titleText != null)
                titleText.alignment = compactHeader ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (subtitleText != null)
                subtitleText.alignment = TextAlignmentOptions.Center;
            LayoutContent(width, height, pad);
            SetObjectActive(messageText != null ? messageText.gameObject : null, true);
            if (messageText != null)
                messageText.transform.SetAsLastSibling();
            ConfigureMessageText();
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            float innerWidth = width - pad * 2f;
            bool compactHeight = height < 860f;
            bool ultraCompactHeight = height < 700f;
            float carouselY = ultraCompactHeight ? -96f : compactHeight ? -116f : -142f;
            float carouselHeight = ultraCompactHeight
                ? Mathf.Clamp(height * 0.37f, 204f, 250f)
                : compactHeight
                    ? Mathf.Clamp(height * 0.36f, 252f, 310f)
                    : Mathf.Clamp(height * 0.39f, 326f, 390f);
            SetTopLeft(weekPanel, pad, carouselY, innerWidth, carouselHeight);
            LayoutWeekPanel(innerWidth, carouselHeight);

            float featuredWidth = Mathf.Clamp(innerWidth * 0.82f, Mathf.Min(720f, innerWidth), innerWidth);
            float featuredHeight = ultraCompactHeight
                ? Mathf.Clamp(height * 0.17f, 94f, 106f)
                : compactHeight
                    ? Mathf.Clamp(height * 0.15f, 110f, 126f)
                    : Mathf.Clamp(height * 0.155f, 140f, 162f);
            float featuredX = pad + (innerWidth - featuredWidth) * 0.5f;
            float featuredY = carouselY - carouselHeight - (compactHeight ? 12f : 20f);
            float cardInset = Mathf.Clamp(featuredWidth * 0.024f, 28f, 42f);
            float iconSize = Mathf.Clamp(featuredHeight * 0.79f, ultraCompactHeight ? 70f : 88f, 136f);
            float iconX = cardInset + 4f;
            float iconY = -(featuredHeight - iconSize) * 0.5f;
            float textX = iconX + iconSize + 28f;
            float textWidth = featuredWidth * 0.35f;

            SetTopLeft(featuredRewardGlow, featuredX - 6f, featuredY + 6f, featuredWidth + 12f, featuredHeight + 12f);
            SetTopLeft(featuredRewardPanel, featuredX, featuredY, featuredWidth, featuredHeight);
            SetTopLeft(featuredRewardInner, 10f, -10f, featuredWidth - 20f, featuredHeight - 20f);
            SetObjectActive(featuredRewardIconBackdrop != null ? featuredRewardIconBackdrop.gameObject : null, true);
            SetObjectActive(featuredRewardIcon != null ? featuredRewardIcon.gameObject : null, true);
            SetTopLeft(featuredRewardIconBackdrop != null ? featuredRewardIconBackdrop.rectTransform : null, iconX - 8f, iconY + 8f, iconSize + 16f, iconSize + 16f);
            SetTopLeft(featuredRewardIcon != null ? featuredRewardIcon.rectTransform : null, iconX, iconY, iconSize, iconSize);
            SetTopLeft(todayText != null ? todayText.rectTransform : null, textX, -16f, textWidth, 22f);
            SetTopLeft(rewardTitleText != null ? rewardTitleText.rectTransform : null, textX, -42f, textWidth, 34f);
            SetTopLeft(rewardAmountText != null ? rewardAmountText.rectTransform : null, textX, -78f, textWidth, 52f);
            float bonusX = featuredWidth * 0.61f;
            if (compactHeight)
            {
                SetTopLeft(todayText != null ? todayText.rectTransform : null, textX, -9f, textWidth, 20f);
                SetObjectActive(rewardTitleText != null ? rewardTitleText.gameObject : null, false);
                float compactAmountHeight = Mathf.Clamp(featuredHeight - 54f, 38f, 44f);
                SetTopLeft(rewardAmountText != null ? rewardAmountText.rectTransform : null, textX, -34f, textWidth, compactAmountHeight);
                SetTopLeft(rewardBonusText != null ? rewardBonusText.rectTransform : null, bonusX, -16f, featuredWidth - bonusX - cardInset, featuredHeight - 28f);
            }
            else
            {
                SetObjectActive(rewardTitleText != null ? rewardTitleText.gameObject : null, true);
                SetTopLeft(rewardBonusText != null ? rewardBonusText.rectTransform : null, bonusX, -30f, featuredWidth - bonusX - cardInset, 82f);
            }
            SetObjectActive(featuredStateBadge != null ? featuredStateBadge.gameObject : null, false);
            SetObjectActive(progressTrack != null ? progressTrack.gameObject : null, true);
            SetObjectActive(progressText != null ? progressText.gameObject : null, false);
            SetTopLeft(progressText != null ? progressText.rectTransform : null, textX, -featuredHeight + 50f, featuredWidth - textX - cardInset, 22f);
            SetTopLeft(progressTrack, textX, -featuredHeight + (compactHeight ? 13f : 18f), featuredWidth - textX - cardInset, compactHeight ? 7f : 11f);
            StretchProgressFill();
            ConfigureFeatureText();
            ConfigureTodayText();

            float buttonGap = Mathf.Clamp(width * 0.018f, compactHeight ? 18f : 28f, 36f);
            float buttonWidth = Mathf.Min(compactHeight ? 420f : 460f, (innerWidth - buttonGap) * 0.5f);
            float buttonHeight = ultraCompactHeight
                ? Mathf.Clamp(height * 0.132f, 72f, 82f)
                : compactHeight
                    ? Mathf.Clamp(height * 0.108f, 82f, 94f)
                    : Mathf.Clamp(height * 0.124f, 116f, 132f);
            float buttonsWidth = buttonWidth * 2f + buttonGap;
            float buttonStartX = pad + (innerWidth - buttonsWidth) * 0.5f;
            float bottomMargin = ultraCompactHeight ? 12f : compactHeight ? 22f : 34f;
            float buttonY = -height + bottomMargin + buttonHeight;
            SetTopLeft(freeButton != null ? freeButton.transform as RectTransform : null, buttonStartX, buttonY, buttonWidth, buttonHeight);
            SetTopLeft(adButton != null ? adButton.transform as RectTransform : null, buttonStartX + buttonWidth + buttonGap, buttonY, buttonWidth, buttonHeight);

            float statusHeight = ultraCompactHeight ? 36f : compactHeight ? 46f : 54f;
            float statusGap = compactHeight ? 10f : 16f;
            float statusY = buttonY + statusHeight + statusGap;
            float statusInset = compactHeight ? width * 0.09f : width * 0.12f;
            float statusX = pad + statusInset;
            float statusWidth = innerWidth - statusInset * 2f;
            SetTopLeft(statusPanel, statusX, statusY, statusWidth, statusHeight);
            SetTopLeft(messageText != null ? messageText.rectTransform : null, statusX + 24f, statusY - 3f, statusWidth - 48f, statusHeight - 6f);
        }

        protected override void RefreshContentText()
        {
            SetLabel(subtitleText, T("Не прерывайте серию — забирайте награду каждый день", "Keep the streak alive — claim a reward every day", "Seriyi bozma — her gün ödülünü al"));
            SetLabel(weekTitleText, string.Empty);
            SetLabel(weekSubtitleText, T("7 ДНЕЙ • НАГРАДА РАСТЁТ КАЖДЫЙ ДЕНЬ", "7 DAYS • A RICHER REWARD EVERY DAY", "7 GÜN • HER GÜN DAHA DEĞERLİ ÖDÜL"));
        }

        protected override void RefreshValues()
        {
            base.RefreshValues();

            int gold = CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int ametist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            SetLabel(profileGoldText, FormatAmount(gold));
            SetLabel(profileAmetistText, FormatAmount(ametist));
        }

        protected override void Open()
        {
            if (!MainHubStateController.CanOpenMainWindow("MainWeeklyReward"))
            {
                Close();
                return;
            }

            SetMainUiVisible(false);
            selectedDayIndex = -1;
            base.Open();
            ApplyOpaqueOverlay();
            PlayOpenAnimation();
        }

        protected override void Close()
        {
            if (openAnimationRoutine != null)
            {
                StopCoroutine(openAnimationRoutine);
                openAnimationRoutine = null;
            }

            base.Close();
            SetMainUiVisible(true);
            MainHubStateController.NotifyMainWindowClosed();
        }

        protected override void RefreshContentValues()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                SetMessage(GameLocalization.Text("battle.shop.profile_loading"));
                return;
            }

            WeeklyRewardService.EnsureInitialized(profile);
            bool timeBlocked = WeeklyRewardService.IsTimeBlocked(profile);
            bool canClaim = WeeklyRewardService.CanClaimToday(profile);
            int dayNumber = WeeklyRewardService.GetCurrentDayNumber(profile);
            currentDayIndex = Mathf.Clamp(dayNumber - 1, 0, DayCount - 1);
            if (selectedDayIndex < 0 || selectedDayIndex >= DayCount)
                selectedDayIndex = currentDayIndex;

            int freeAltin = WeeklyRewardService.GetFreeAltinForDay(selectedDayIndex);
            int adAltin = WeeklyRewardService.GetAdAltinForDay(selectedDayIndex);
            int adAmetist = WeeklyRewardService.GetAdAmetistForDay(selectedDayIndex);
            bool selectedIsCurrent = selectedDayIndex == currentDayIndex;
            RewardedAdAvailability adAvailability = MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.WeeklyRewardedPlacementId);

            if (selectedIsCurrent && canClaim && !timeBlocked)
            {
                SetButtonLabel(freeButton, T(
                    "ЗАБРАТЬ БЕСПЛАТНО",
                    "CLAIM FREE",
                    "ÜCRETSİZ AL"));
                SetButtonLabel(adButton, T(
                    "РЕКЛАМА ×2",
                    "WATCH AD ×2",
                    "REKLAMLA ×2"));
            }
            else if (!selectedIsCurrent)
            {
                SetButtonLabel(freeButton, selectedDayIndex < currentDayIndex
                    ? T("НАГРАДА ПОЛУЧЕНА", "REWARD CLAIMED", "ÖDÜL ALINDI")
                    : T("ОТКРОЕТСЯ ПОЗЖЕ", "UNLOCKS LATER", "DAHA SONRA AÇILIR"));
                SetButtonLabel(adButton, T("ВЕРНУТЬСЯ К СЕГОДНЯ", "RETURN TO TODAY", "BUGÜNE DÖN"));
            }
            else
            {
                SetButtonLabel(freeButton, T("ПОЛУЧЕНО СЕГОДНЯ", "CLAIMED TODAY", "BUGÜN ALINDI"));
                SetButtonLabel(adButton, T("ВОЗВРАЩАЙТЕСЬ ЗАВТРА", "COME BACK TOMORROW", "YARIN GERİ DÖN"));
            }

            RefreshFeaturedReward(profile, selectedDayIndex, freeAltin, adAltin, adAmetist, timeBlocked, canClaim);

            if (freeButton != null)
                freeButton.interactable = selectedIsCurrent && !timeBlocked && canClaim;
            if (adButton != null)
            {
                adButton.onClick.RemoveAllListeners();
                if (selectedIsCurrent)
                    adButton.onClick.AddListener(ClaimAd);
                else
                    adButton.onClick.AddListener(ReturnToCurrentDay);
                adButton.interactable = !selectedIsCurrent || (!timeBlocked && canClaim && adAvailability.IsReady && !rewardedAdRequestInProgress);
            }

            RefreshStatusPanel(timeBlocked, canClaim, adAvailability);
            SetMessage(selectedIsCurrent
                ? ResolveStatus(timeBlocked, canClaim, adAvailability)
                : selectedDayIndex < currentDayIndex
                    ? T("Эта награда уже в вашей коллекции.", "This reward is already in your collection.", "Bu ödül zaten koleksiyonunda.")
                    : T("Продолжайте серию, чтобы открыть эту награду.", "Keep the streak to unlock this reward.", "Bu ödülü açmak için seriyi sürdür."));
        }

        private void RefreshFeaturedReward(PlayerProfile profile, int dayIndex, int freeAltin, int adAltin, int adAmetist, bool timeBlocked, bool canClaim)
        {
            int dayNumber = dayIndex + 1;
            bool selectedIsCurrent = dayIndex == currentDayIndex;
            bool claimed = WeeklyRewardService.IsDayClaimed(profile, dayIndex);
            bool locked = WeeklyRewardService.IsDayLocked(profile, dayIndex);
            ApplyRewardIcon(featuredRewardIcon, dayIndex);
            SetLabel(todayText, selectedIsCurrent
                ? T("СЕГОДНЯ", "TODAY", "BUGÜN")
                : T($"ДЕНЬ {dayNumber}", $"DAY {dayNumber}", $"GÜN {dayNumber}"));
            SetLabel(rewardTitleText, selectedIsCurrent
                ? T("СЕГОДНЯШНЯЯ НАГРАДА", "TODAY'S REWARD", "BUGÜNÜN ÖDÜLÜ")
                : T($"НАГРАДА ДНЯ {dayNumber}", $"DAY {dayNumber} REWARD", $"{dayNumber}. GÜN ÖDÜLÜ"));
            SetLabel(rewardAmountText, $"{FormatAmount(freeAltin)} ALTIN");
            SetLabel(rewardBonusText, T(
                $"С рекламой\n{FormatAmount(adAltin)} ALTIN  +  {adAmetist} AMETIST",
                $"With an ad\n{FormatAmount(adAltin)} ALTIN  +  {adAmetist} AMETIST",
                $"Reklamla\n{FormatAmount(adAltin)} ALTIN  +  {adAmetist} AMETIST"));
            SetLabel(progressText, ResolveFeaturedState(dayNumber, selectedIsCurrent && timeBlocked, selectedIsCurrent && canClaim));
            SetLabel(featuredStateText, selectedIsCurrent && timeBlocked
                ? T("ПРОВЕРКА ВРЕМЕНИ", "TIME CHECK", "ZAMAN KONTROLÜ")
                : selectedIsCurrent && canClaim
                    ? T("ДОСТУПНО СЕЙЧАС", "AVAILABLE NOW", "ŞİMDİ HAZIR")
                    : claimed
                        ? T("ПОЛУЧЕНО", "CLAIMED", "ALINDI")
                        : locked
                            ? T("ПОКА ЗАКРЫТО", "LOCKED FOR NOW", "ŞİMDİLİK KİLİTLİ")
                            : T("СЛЕДУЮЩАЯ", "UP NEXT", "SIRADAKİ"));
            SetProgress(dayNumber / (float)DayCount);
            RefreshDayCards(profile);
            RefreshCarouselPageIndicator();

            Color stateColor = selectedIsCurrent && timeBlocked
                ? new Color(0.84f, 0.30f, 0.34f, 1f)
                : selectedIsCurrent && canClaim
                    ? new Color(1f, 0.72f, 0.20f, 1f)
                    : claimed
                        ? new Color(0.42f, 0.82f, 0.64f, 1f)
                        : new Color(0.62f, 0.42f, 0.90f, 1f);

            if (featuredRewardPanelImage != null)
                featuredRewardPanelImage.color = new Color(0.010f, 0.040f, 0.082f, 0.98f);
            if (featuredRewardGlowImage != null)
                featuredRewardGlowImage.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.10f);
            if (featuredRewardOutline != null)
                featuredRewardOutline.effectColor = new Color(stateColor.r, stateColor.g, stateColor.b, 0.38f);
            if (featuredStateBadgeImage != null)
                featuredStateBadgeImage.color = new Color(stateColor.r * 0.62f, stateColor.g * 0.62f, stateColor.b * 0.62f, 0.96f);
        }

        private void BuildDayCards(Transform parent)
        {
            for (int i = 0; i < DayCount; i++)
            {
                int dayIndex = i;
                RectTransform card = CreatePanel(parent, $"WeeklyCarouselDay_{i + 1}", new Color(0.020f, 0.055f, 0.105f, 0.96f));
                dayCards[i] = card;
                dayCardImages[i] = card != null ? card.GetComponent<Image>() : null;
                ApplyRoundedSprite(dayCardImages[i], new Color(0.020f, 0.055f, 0.105f, 0.96f));
                dayCardOutlines[i] = AddOutline(card.gameObject, new Color(0.58f, 0.74f, 0.96f, 0.26f), new Vector2(1.25f, -1.25f));
                AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.66f), new Vector2(0f, -10f));
                dayCardGroups[i] = card.gameObject.AddComponent<CanvasGroup>();
                dayCardButtons[i] = card.gameObject.AddComponent<Button>();
                dayCardButtons[i].targetGraphic = dayCardImages[i];
                dayCardButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                dayCardButtons[i].onClick.AddListener(() => SelectDay(dayIndex, true));

                RectTransform artFrame = CreatePanel(card, "RewardArtFrame", Color.white);
                dayArtFrames[i] = artFrame != null ? artFrame.GetComponent<Image>() : null;
                if (dayArtFrames[i] != null)
                {
                    dayArtFrames[i].sprite = GetRewardIconBackdropSprite();
                    dayArtFrames[i].type = Image.Type.Simple;
                    dayArtFrames[i].preserveAspect = false;
                    dayArtFrames[i].color = new Color(0.06f, 0.16f, 0.25f, 0.42f);
                    dayArtFrames[i].raycastTarget = false;
                }

                RectTransform accentRect = CreatePanel(card, "StateAccent", new Color(0.40f, 0.70f, 1f, 0.75f));
                dayCardAccents[i] = accentRect != null ? accentRect.GetComponent<Image>() : null;
                ApplyRoundedSprite(dayCardAccents[i], new Color(0.40f, 0.70f, 1f, 0.75f));
                if (dayCardAccents[i] != null)
                    dayCardAccents[i].raycastTarget = false;

                dayIcons[i] = CreateRewardIcon(card, "RewardIcon", i);
                dayLabels[i] = CreateText(card, "DayLabel", string.Empty, 20f, FontStyles.Bold, new Color(0.96f, 0.91f, 0.78f, 1f));
                dayStateLabels[i] = CreateText(card, "StateLabel", string.Empty, 15f, FontStyles.Bold, new Color(0.76f, 0.70f, 0.90f, 1f));
                CenterSlotText(dayLabels[i]);
                CenterSlotText(dayStateLabels[i]);
            }
        }

        private void LayoutWeekPanel(float panelWidth, float panelHeight)
        {
            bool shortPanel = panelHeight < 300f;
            SetTopLeft(weekTitleText != null ? weekTitleText.rectTransform : null, 0f, shortPanel ? -2f : -4f, panelWidth, 28f);
            SetObjectActive(weekTitleText != null ? weekTitleText.gameObject : null, false);
            SetTopLeft(weekSubtitleText != null ? weekSubtitleText.rectTransform : null, 0f, -10f, panelWidth, 28f);
            SetObjectActive(weekSubtitleText != null ? weekSubtitleText.gameObject : null, !shortPanel);
            SetTopLeft(carouselPageText != null ? carouselPageText.rectTransform : null, 0f, -panelHeight + 27f, panelWidth, 22f);
            SetObjectActive(carouselPageText != null ? carouselPageText.gameObject : null, !shortPanel);
            ConfigureCarouselHeader(weekTitleText, 30f, 20f);
            ConfigureCarouselHeader(weekSubtitleText, 24f, 17f);
            ConfigureCarouselHeader(carouselPageText, 20f, 15f);

            float arrowSize = Mathf.Clamp(panelHeight * 0.13f, 46f, 64f);
            SetTopLeft(previousDayButton != null ? previousDayButton.transform as RectTransform : null, 12f, -panelHeight * 0.51f + arrowSize * 0.5f, arrowSize, arrowSize);
            SetTopLeft(nextDayButton != null ? nextDayButton.transform as RectTransform : null, panelWidth - arrowSize - 12f, -panelHeight * 0.51f + arrowSize * 0.5f, arrowSize, arrowSize);

            ApplyCarouselLayout(false);
        }

        private void RefreshDayCards(PlayerProfile profile)
        {
            if (profile == null)
                return;

            for (int i = 0; i < DayCount; i++)
            {
                bool claimed = WeeklyRewardService.IsDayClaimed(profile, i);
                bool current = WeeklyRewardService.IsDayCurrent(profile, i);
                bool locked = WeeklyRewardService.IsDayLocked(profile, i);
                WeeklyRewardClaimType claimType = WeeklyRewardService.GetDayClaimType(profile, i);

                SetLabel(dayLabels[i], T($"ДЕНЬ {i + 1}", $"DAY {i + 1}", $"GÜN {i + 1}"));
                SetLabel(dayStateLabels[i], ResolveDayState(claimed, current, locked, claimType));
                ApplyRewardIcon(dayIcons[i], i);

                Color frameColor;
                Color iconColor;
                Color stateColor;
                if (claimed)
                {
                    frameColor = new Color(0.56f, 0.90f, 0.72f, 0.94f);
                    iconColor = new Color(0.82f, 1f, 0.90f, 0.9f);
                    stateColor = new Color(0.48f, 1f, 0.68f, 1f);
                }
                else if (current)
                {
                    frameColor = new Color(1f, 0.83f, 0.36f, 1f);
                    iconColor = Color.white;
                    stateColor = new Color(1f, 0.82f, 0.30f, 1f);
                }
                else if (locked)
                {
                    frameColor = new Color(0.42f, 0.54f, 0.68f, 0.68f);
                    iconColor = new Color(0.68f, 0.74f, 0.84f, 0.76f);
                    stateColor = new Color(0.82f, 0.87f, 0.94f, 0.98f);
                }
                else
                {
                    frameColor = i == DayCount - 1
                        ? new Color(0.78f, 0.55f, 1f, 0.94f)
                        : new Color(0.70f, 0.82f, 0.94f, 0.82f);
                    iconColor = i == DayCount - 1
                        ? new Color(0.92f, 0.82f, 1f, 0.92f)
                        : new Color(0.78f, 0.86f, 0.96f, 0.82f);
                    stateColor = frameColor;
                }

                if (dayCardImages[i] != null)
                    dayCardImages[i].color = i == selectedDayIndex
                        ? i == DayCount - 1
                            ? new Color(0.060f, 0.036f, 0.112f, 0.98f)
                            : new Color(0.018f, 0.052f, 0.098f, 0.99f)
                        : claimed
                            ? new Color(0.016f, 0.066f, 0.060f, 0.98f)
                            : i == DayCount - 1
                                ? new Color(0.044f, 0.026f, 0.082f, locked ? 0.90f : 0.98f)
                                : new Color(0.012f, 0.034f, 0.068f, locked ? 0.90f : 0.98f);
                if (dayArtFrames[i] != null)
                    dayArtFrames[i].color = locked
                        ? new Color(1f, 1f, 1f, 0.46f)
                        : new Color(1f, 1f, 1f, i == selectedDayIndex ? 0.74f : 0.58f);
                if (dayCardAccents[i] != null)
                    dayCardAccents[i].color = new Color(frameColor.r, frameColor.g, frameColor.b, i == selectedDayIndex ? 0.96f : 0.46f);
                if (dayCardOutlines[i] != null)
                    dayCardOutlines[i].effectColor = new Color(frameColor.r, frameColor.g, frameColor.b, i == selectedDayIndex ? 0.88f : 0.30f);
                if (dayIcons[i] != null)
                    dayIcons[i].color = iconColor;
                if (dayStateLabels[i] != null)
                    dayStateLabels[i].color = stateColor;
            }

            if (!deferCarouselLayout)
                ApplyCarouselLayout(false);
        }

        private void SelectPreviousDay()
        {
            SelectDay(selectedDayIndex - 1, true);
        }

        private void SelectNextDay()
        {
            SelectDay(selectedDayIndex + 1, true);
        }

        private void ReturnToCurrentDay()
        {
            SelectDay(currentDayIndex, true);
        }

        private void SelectDay(int dayIndex, bool animate)
        {
            int clamped = Mathf.Clamp(dayIndex, 0, DayCount - 1);
            if (selectedDayIndex == clamped)
                return;

            selectedDayIndex = clamped;
            deferCarouselLayout = animate;
            try
            {
                RefreshValues();
            }
            finally
            {
                deferCarouselLayout = false;
            }
            ApplyCarouselLayout(animate);
        }

        private void ApplyCarouselLayout(bool animate)
        {
            if (weekPanel == null || selectedDayIndex < 0)
                return;

            if (carouselAnimationRoutine != null)
            {
                StopCoroutine(carouselAnimationRoutine);
                carouselAnimationRoutine = null;
            }

            Vector2[] targetPositions = new Vector2[DayCount];
            Vector3[] targetScales = new Vector3[DayCount];
            float[] targetAlphas = new float[DayCount];
            bool[] targetInteractable = new bool[DayCount];
            float panelWidth = weekPanel.rect.width > 1f ? weekPanel.rect.width : 1800f;
            float panelHeight = weekPanel.rect.height > 1f ? weekPanel.rect.height : 390f;
            bool fullWeek = panelWidth >= 1500f && panelHeight >= 290f;
            bool shortPanel = panelHeight < 300f;
            float gap = Mathf.Clamp(panelWidth * 0.010f, 16f, 24f);
            float cardWidth;
            float cardHeight;
            float centerY;
            float step;
            float startX = 0f;
            if (fullWeek)
            {
                float availableWidth = panelWidth - Mathf.Clamp(panelWidth * 0.075f, 104f, 156f);
                cardWidth = Mathf.Clamp((availableWidth - gap * (DayCount - 1)) / DayCount, 172f, 224f);
                cardHeight = shortPanel
                    ? Mathf.Clamp(panelHeight - 90f, 142f, 184f)
                    : Mathf.Clamp(panelHeight - 104f, 228f, 270f);
                step = cardWidth + gap;
                float rowWidth = cardWidth * DayCount + gap * (DayCount - 1);
                startX = -rowWidth * 0.5f + cardWidth * 0.5f;
                centerY = -(shortPanel ? 42f : 64f) - cardHeight * 0.5f;
            }
            else
            {
                cardHeight = shortPanel
                    ? Mathf.Clamp(panelHeight - 90f, 142f, 190f)
                    : Mathf.Clamp(panelHeight * 0.68f, 204f, 286f);
                cardWidth = cardHeight * 0.76f;
                centerY = shortPanel ? -42f - cardHeight * 0.5f : -panelHeight * 0.55f;
                step = Mathf.Clamp(cardWidth * 1.06f, 166f, 232f);
            }

            SetObjectActive(previousDayButton != null ? previousDayButton.gameObject : null, !fullWeek);
            SetObjectActive(nextDayButton != null ? nextDayButton.gameObject : null, !fullWeek);

            for (int i = 0; i < DayCount; i++)
            {
                RectTransform card = dayCards[i];
                if (card == null)
                    continue;

                card.anchorMin = new Vector2(0.5f, 1f);
                card.anchorMax = new Vector2(0.5f, 1f);
                card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(cardWidth, cardHeight);

                int relative = i - selectedDayIndex;
                int distance = Mathf.Abs(relative);
                float scale = fullWeek
                    ? distance == 0 ? 1.07f : 1f
                    : distance == 0 ? 1f : distance == 1 ? 0.84f : distance == 2 ? 0.66f : 0.46f;
                float alpha = fullWeek
                    ? 1f
                    : distance == 0 ? 1f : distance == 1 ? 0.86f : distance == 2 ? 0.52f : 0f;
                targetPositions[i] = new Vector2(fullWeek ? startX + i * step : relative * step, centerY);
                targetScales[i] = new Vector3(scale, scale, 1f);
                targetAlphas[i] = alpha;
                targetInteractable[i] = fullWeek || distance <= 2;

                float labelHeight = cardHeight * 0.15f;
                float stateHeight = cardHeight * 0.13f;
                float artSize = Mathf.Min(cardWidth * 0.86f, cardHeight * 0.58f);
                float artX = (cardWidth - artSize) * 0.5f;
                float artY = -labelHeight - 12f;
                SetTopLeft(dayLabels[i] != null ? dayLabels[i].rectTransform : null, 10f, -10f, cardWidth - 20f, labelHeight);
                SetTopLeft(dayArtFrames[i] != null ? dayArtFrames[i].rectTransform : null, artX, artY, artSize, artSize);
                float iconSize = artSize * 0.92f;
                Vector2 opticalOffset = GetRewardArtOpticalOffset(i, iconSize);
                SetTopLeft(
                    dayIcons[i] != null ? dayIcons[i].rectTransform : null,
                    (cardWidth - iconSize) * 0.5f + opticalOffset.x,
                    artY - (artSize - iconSize) * 0.5f + opticalOffset.y,
                    iconSize,
                    iconSize);
                SetTopLeft(dayCardAccents[i] != null ? dayCardAccents[i].rectTransform : null, 18f, -4f, cardWidth - 36f, i == selectedDayIndex ? 5f : 3f);
                SetTopLeft(dayStateLabels[i] != null ? dayStateLabels[i].rectTransform : null, 8f, -cardHeight + stateHeight + 11f, cardWidth - 16f, stateHeight);
                ConfigureDayCardText(dayLabels[i], fullWeek ? 25f : 24f, 17f);
                ConfigureDayCardText(dayStateLabels[i], fullWeek ? 21f : 20f, 15f);
            }

            for (int distance = 3; distance >= 0; distance--)
            {
                for (int i = 0; i < DayCount; i++)
                {
                    if (Mathf.Abs(i - selectedDayIndex) == distance && dayCards[i] != null)
                        dayCards[i].SetAsLastSibling();
                }
            }

            if (previousDayButton != null)
                previousDayButton.interactable = selectedDayIndex > 0;
            if (nextDayButton != null)
                nextDayButton.interactable = selectedDayIndex < DayCount - 1;
            if (previousDayButton != null)
                previousDayButton.transform.SetAsLastSibling();
            if (nextDayButton != null)
                nextDayButton.transform.SetAsLastSibling();
            if (carouselPageText != null)
                carouselPageText.transform.SetAsLastSibling();

            if (animate && isActiveAndEnabled)
                carouselAnimationRoutine = StartCoroutine(AnimateCarousel(targetPositions, targetScales, targetAlphas, targetInteractable));
            else
                SetCarouselTargets(targetPositions, targetScales, targetAlphas, targetInteractable);

            RefreshCarouselPageIndicator();
        }

        private IEnumerator AnimateCarousel(Vector2[] positions, Vector3[] scales, float[] alphas, bool[] interactable)
        {
            const float duration = 0.24f;
            Vector2[] starts = new Vector2[DayCount];
            Vector3[] startScales = new Vector3[DayCount];
            float[] startAlphas = new float[DayCount];
            for (int i = 0; i < DayCount; i++)
            {
                starts[i] = dayCards[i] != null ? dayCards[i].anchoredPosition : Vector2.zero;
                startScales[i] = dayCards[i] != null ? dayCards[i].localScale : Vector3.one;
                startAlphas[i] = dayCardGroups[i] != null ? dayCardGroups[i].alpha : 1f;
                if (dayCardGroups[i] != null)
                    dayCardGroups[i].blocksRaycasts = false;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                for (int i = 0; i < DayCount; i++)
                {
                    if (dayCards[i] != null)
                    {
                        dayCards[i].anchoredPosition = Vector2.LerpUnclamped(starts[i], positions[i], eased);
                        dayCards[i].localScale = Vector3.LerpUnclamped(startScales[i], scales[i], eased);
                    }
                    if (dayCardGroups[i] != null)
                        dayCardGroups[i].alpha = Mathf.Lerp(startAlphas[i], alphas[i], eased);
                }
                yield return null;
            }

            SetCarouselTargets(positions, scales, alphas, interactable);
            carouselAnimationRoutine = null;
        }

        private void SetCarouselTargets(Vector2[] positions, Vector3[] scales, float[] alphas, bool[] interactable)
        {
            for (int i = 0; i < DayCount; i++)
            {
                if (dayCards[i] != null)
                {
                    dayCards[i].anchoredPosition = positions[i];
                    dayCards[i].localScale = scales[i];
                }
                if (dayCardGroups[i] != null)
                {
                    dayCardGroups[i].alpha = alphas[i];
                    dayCardGroups[i].interactable = interactable[i];
                    dayCardGroups[i].blocksRaycasts = interactable[i];
                }
            }
        }

        private void RefreshCarouselPageIndicator()
        {
            if (carouselPageText == null || selectedDayIndex < 0)
                return;

            SetLabel(carouselPageText, $"{selectedDayIndex + 1:00} / {DayCount:00}");
        }

        private void RefreshStatusPanel(bool timeBlocked, bool canClaim, RewardedAdAvailability adAvailability)
        {
            if (statusPanelImage != null)
                statusPanelImage.color = new Color(0.006f, 0.026f, 0.060f, 0.78f);
            if (messageText != null)
                messageText.color = timeBlocked
                    ? new Color(1f, 0.55f, 0.55f, 1f)
                    : canClaim
                        ? new Color(1f, 0.84f, 0.46f, 1f)
                        : new Color(0.62f, 0.88f, 1f, 1f);
        }

        private static void ConfigureRewardButton(Button button, Color accent)
        {
            if (button == null)
                return;

            MainLobbyButtonStyle.Apply(button);
            if (button.image != null)
                button.image.preserveAspect = false;

            AddOutline(button.gameObject, new Color(accent.r, accent.g, accent.b, 0.20f), new Vector2(1f, -1f));
            AddShadow(button.gameObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, -6f));
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.84f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.36f, 0.40f, 0.46f, 0.64f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.richText = true;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 34f;
                label.fontSizeMax = 34f;
                label.fontSizeMin = 23f;
                label.enableAutoSizing = true;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Truncate;
                label.lineSpacing = 0f;
                label.margin = new Vector4(42f, 8f, 42f, 10f);
                MainLobbyButtonStyle.ApplyFont(label);
                MainLobbyButtonStyle.ApplySilverTextEffect(label);
            }
        }

        private Button CreateCarouselArrow(Transform parent, string objectName, string glyph, Action callback)
        {
            Button arrow = CreateButton(parent, objectName, glyph, 42f);
            if (arrow == null)
                return null;

            if (arrow.image != null)
            {
                ApplyRoundedSprite(arrow.image, new Color(0.020f, 0.075f, 0.14f, 0.96f));
            }

            AddOutline(arrow.gameObject, new Color(0.34f, 0.72f, 1f, 0.18f), new Vector2(1f, -1f));
            arrow.navigation = new Navigation { mode = Navigation.Mode.None };
            arrow.onClick.AddListener(() => callback?.Invoke());
            TextMeshProUGUI label = arrow.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.gameObject.SetActive(false);

            NavigationGlyph glyphType = string.Equals(glyph, "<", StringComparison.Ordinal)
                ? NavigationGlyph.Left
                : NavigationGlyph.Right;
            EnsureNavigationGlyph(arrow.transform, "SciFiArrowGlyph", glyphType, new Color(0.76f, 0.91f, 1f, 1f), 0.28f);

            return arrow;
        }

        private static void ConfigureDayCardText(TextMeshProUGUI text, float maxSize, float minSize)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private static Vector2 GetRewardArtOpticalOffset(int dayIndex, float iconSize)
        {
            return dayIndex switch
            {
                0 => new Vector2(0f, -iconSize * 0.120f),
                3 => new Vector2(iconSize * 0.120f, 0f),
                6 => new Vector2(iconSize * 0.150f, 0f),
                _ => Vector2.zero
            };
        }

        private static void ConfigureSubtitleText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 26f;
            text.fontSizeMax = 26f;
            text.fontSizeMin = 18f;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private static void ConfigureCarouselHeader(TextMeshProUGUI text, float maxSize, float minSize)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private void ConfigureDedicatedWindowChrome(Transform window)
        {
            Image windowImage = window != null ? window.GetComponent<Image>() : null;
            if (windowImage != null)
            {
                MainLobbyButtonStyle.ApplyDlsWindow(windowImage);
                windowImage.color = Color.white;
                windowImage.raycastTarget = true;
            }

            if (window != null)
            {
                Transform existing = window.Find("WeeklyFullscreenBackground");
                if (existing == null)
                {
                    GameObject backgroundObject = new GameObject("WeeklyFullscreenBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
                    backgroundObject.transform.SetParent(window, false);
                    fullscreenBackgroundImage = backgroundObject.GetComponent<Image>();
                }
                else
                {
                    fullscreenBackgroundImage = existing.GetComponent<Image>();
                }

                if (fullscreenBackgroundImage != null)
                {
                    Sprite cinematicBackground = LoadCelestialFrameSprite();
                    fullscreenBackgroundImage.sprite = cinematicBackground;
                    fullscreenBackgroundImage.type = Image.Type.Simple;
                    fullscreenBackgroundImage.preserveAspect = true;
                    fullscreenBackgroundImage.color = new Color(0.72f, 0.82f, 1f, 0.16f);
                    fullscreenBackgroundImage.raycastTarget = false;

                    AspectRatioFitter fitter = fullscreenBackgroundImage.GetComponent<AspectRatioFitter>();
                    if (fitter != null)
                        fitter.aspectMode = AspectRatioFitter.AspectMode.None;

                    fullscreenBackgroundImage.transform.SetAsFirstSibling();
                }
            }

            ConfigureCloseButtonChrome();
        }

        private void ConfigureCloseButtonChrome()
        {
            if (closeButton != null && closeButton.image != null)
            {
                MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
                Transform proceduralGlyph = closeButton.transform.Find("SciFiCloseGlyph");
                if (proceduralGlyph != null)
                    proceduralGlyph.gameObject.SetActive(false);
            }
        }

        private static void CreateButtonAccent(Button button, string objectName, Color color)
        {
            if (button == null)
                return;

            Transform existing = button.transform.Find(objectName);
            Image accent = existing != null ? existing.GetComponent<Image>() : null;
            if (accent == null)
            {
                GameObject accentObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accentObject.transform.SetParent(button.transform, false);
                accent = accentObject.GetComponent<Image>();
            }

            accent.sprite = GetRoundedRectSprite();
            accent.type = Image.Type.Sliced;
            accent.color = color;
            accent.raycastTarget = false;
            RectTransform rect = accent.rectTransform;
            rect.anchorMin = new Vector2(0.23f, 0f);
            rect.anchorMax = new Vector2(0.77f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, 7f);
            rect.offsetMax = new Vector2(0f, 11f);
            rect.localScale = Vector3.one;
            accent.transform.SetAsLastSibling();
        }

        private static Outline AddOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
                return null;

            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static Shadow AddShadow(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
                return null;

            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
            return shadow;
        }

        private static Sprite LoadCelestialFrameSprite()
        {
            if (cachedCelestialFrameSprite != null)
                return cachedCelestialFrameSprite;

            cachedCelestialFrameSprite = Resources.Load<Sprite>(CelestialFrameResourcePath);
            if (cachedCelestialFrameSprite != null)
                return cachedCelestialFrameSprite;

            Texture2D texture = Resources.Load<Texture2D>(CelestialFrameResourcePath);
            if (texture != null)
                cachedCelestialFrameSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

            return cachedCelestialFrameSprite;
        }

        private static Sprite GetRoundedRectSprite()
        {
            if (cachedRoundedRectSprite != null)
                return cachedRoundedRectSprite;

            const int size = 64;
            const float radius = 15f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "WeeklyRewardRoundedRect";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius));
                    float dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            cachedRoundedRectSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18f, 18f, 18f, 18f));
            return cachedRoundedRectSprite;
        }

        private enum NavigationGlyph
        {
            Left,
            Right,
            Close
        }

        private static void EnsureNavigationGlyph(Transform parent, string objectName, NavigationGlyph glyph, Color color, float inset)
        {
            if (parent == null)
                return;

            Transform existing = parent.Find(objectName);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
            }
            else
            {
                GameObject glyphObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                glyphObject.transform.SetParent(parent, false);
                image = glyphObject.GetComponent<Image>();
            }

            if (image == null)
                return;

            image.sprite = GetNavigationGlyphSprite(glyph);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(inset, inset);
            rect.anchorMax = new Vector2(1f - inset, 1f - inset);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        private static Sprite GetNavigationGlyphSprite(NavigationGlyph glyph)
        {
            if (glyph == NavigationGlyph.Left && cachedLeftGlyphSprite != null)
                return cachedLeftGlyphSprite;
            if (glyph == NavigationGlyph.Right && cachedRightGlyphSprite != null)
                return cachedRightGlyphSprite;
            if (glyph == NavigationGlyph.Close && cachedCloseGlyphSprite != null)
                return cachedCloseGlyphSprite;

            const int size = 64;
            const float thickness = 4.5f;
            Vector2 a1;
            Vector2 b1;
            Vector2 a2;
            Vector2 b2;
            if (glyph == NavigationGlyph.Close)
            {
                a1 = new Vector2(15f, 15f);
                b1 = new Vector2(49f, 49f);
                a2 = new Vector2(49f, 15f);
                b2 = new Vector2(15f, 49f);
            }
            else if (glyph == NavigationGlyph.Left)
            {
                a1 = new Vector2(43f, 11f);
                b1 = new Vector2(20f, 32f);
                a2 = new Vector2(20f, 32f);
                b2 = new Vector2(43f, 53f);
            }
            else
            {
                a1 = new Vector2(21f, 11f);
                b1 = new Vector2(44f, 32f);
                a2 = new Vector2(44f, 32f);
                b2 = new Vector2(21f, 53f);
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = $"WeeklyReward{glyph}Glyph";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float distance = Mathf.Min(DistanceToSegment(point, a1, b1), DistanceToSegment(point, a2, b2));
                    float alpha = Mathf.Clamp01(thickness + 1f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            if (glyph == NavigationGlyph.Left)
                cachedLeftGlyphSprite = sprite;
            else if (glyph == NavigationGlyph.Right)
                cachedRightGlyphSprite = sprite;
            else
                cachedCloseGlyphSprite = sprite;
            return sprite;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return Vector2.Distance(point, start);

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
        }

        private static void ApplyRoundedSprite(Image image, Color color)
        {
            if (image == null)
                return;

            image.sprite = GetRoundedRectSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = color;
        }

        private static Sprite LoadNamedSprite(string resourcePath, string spriteName)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && string.Equals(sprites[i].name, spriteName, StringComparison.Ordinal))
                        return sprites[i];
                }

                if (sprites.Length > 0)
                    return sprites[0];
            }

            return Resources.Load<Sprite>(resourcePath);
        }

        private static string FormatAmount(int value)
        {
            string raw = Mathf.Max(0, value).ToString();
            for (int i = raw.Length - 3; i > 0; i -= 3)
                raw = raw.Insert(i, " ");
            return raw;
        }

        private static Image CreateRewardIconBackdrop(Transform parent, string objectName)
        {
            GameObject backdropObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropObject.transform.SetParent(parent, false);

            Image image = backdropObject.GetComponent<Image>();
            image.sprite = GetRewardIconBackdropSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(0f, 0f, 0f, 0.68f);
            image.raycastTarget = false;
            return image;
        }

        private static Sprite GetRewardIconBackdropSprite()
        {
            if (cachedRewardIconBackdropSprite != null)
                return cachedRewardIconBackdropSprite;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "WeeklyRewardIconBackdrop";
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center.x) / (size * 0.5f);
                    float ny = (y - center.y) / (size * 0.42f);
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * 0.95f;
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            cachedRewardIconBackdropSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedRewardIconBackdropSprite;
        }

        private void StretchProgressFill()
        {
            RectTransform fillRect = progressFill != null ? progressFill.rectTransform : null;
            if (fillRect == null)
                return;

            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fillRect.anchorMax.x <= 0f ? 0.01f : fillRect.anchorMax.x), 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.localScale = Vector3.one;
        }

        private void SetProgress(float progress)
        {
            RectTransform fillRect = progressFill != null ? progressFill.rectTransform : null;
            if (fillRect == null)
                return;

            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void ConfigureFeatureText()
        {
            ConfigureFeatureLabel(rewardTitleText, 36f, 24f, new Color(0.86f, 0.94f, 1f, 1f));
            ConfigureFeatureLabel(rewardAmountText, 54f, 34f, new Color(1f, 0.82f, 0.34f, 1f));
            ConfigureFeatureLabel(rewardBonusText, 29f, 20f, new Color(0.80f, 0.68f, 1f, 1f));
            ConfigureFeatureLabel(progressText, 22f, 16f, new Color(0.78f, 0.90f, 1f, 1f));
        }

        private static void ConfigureFeatureLabel(TextMeshProUGUI text, float maxSize, float minSize, Color color)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = color;
        }

        private static void ConfigureCinematicRewardText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.outlineColor = new Color32(0, 5, 18, 230);
            text.outlineWidth = 0.18f;
        }

        private static string ResolveFeaturedState(int dayNumber, bool timeBlocked, bool canClaim)
        {
            if (timeBlocked)
                return T("Награда временно заблокирована", "Reward temporarily locked", "Ödül geçici olarak kilitli");

            return canClaim
                ? T($"ПРОГРЕСС НЕДЕЛИ  {dayNumber}/{DayCount}", $"WEEKLY PROGRESS  {dayNumber}/{DayCount}", $"HAFTALIK İLERLEME  {dayNumber}/{DayCount}")
                : T("Следующая награда откроется завтра", "Next reward unlocks tomorrow", "Sonraki ödül yarın açılır");
        }

        private void ApplyOpaqueOverlay()
        {
            if (overlayRect == null)
                return;

            Canvas overlayCanvas = overlayRect.GetComponent<Canvas>();
            if (overlayCanvas != null)
            {
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = 32000;
            }

            Image overlayImage = overlayRect.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0f, 0f, 0f, 1f);
                overlayImage.raycastTarget = true;
            }

            Stretch(overlayRect);
            Canvas.ForceUpdateCanvases();
            Layout();
            if (fullscreenRelayoutRoutine != null)
                StopCoroutine(fullscreenRelayoutRoutine);
            fullscreenRelayoutRoutine = StartCoroutine(RelayoutFullscreenAfterCanvasReady());
            overlayRect.SetAsLastSibling();
            if (windowRect != null)
                windowRect.SetAsLastSibling();
        }

        private IEnumerator RelayoutFullscreenAfterCanvasReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Layout();
            if (overlayRect != null)
                overlayRect.SetAsLastSibling();
            if (windowRect != null)
                windowRect.SetAsLastSibling();
            fullscreenRelayoutRoutine = null;
        }

        private void PlayOpenAnimation()
        {
            if (windowRect == null)
                return;

            if (openAnimationRoutine != null)
                StopCoroutine(openAnimationRoutine);

            openAnimationRoutine = StartCoroutine(AnimateWindowOpen());
        }

        private IEnumerator AnimateWindowOpen()
        {
            const float duration = 0.22f;
            float elapsed = 0f;
            Vector3 startScale = new Vector3(0.965f, 0.965f, 1f);
            windowRect.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                windowRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                yield return null;
            }

            windowRect.localScale = Vector3.one;
            openAnimationRoutine = null;
        }

        private static void SetMainUiVisible(bool visible)
        {
            SetMainUiVisible(visible, true);
        }

        private static void SetMainUiVisible(bool visible, bool ensureMainWidgets)
        {
            if (visible)
                SetNamedObjectActive("CentralPointLeftMenu", true);

            SetNamedObjectActive("ButtonOpenShop", visible);
            SetComponentObjectsActive<MailboxUI>(visible);
            SetComponentObjectsActive<FriendsUI>(visible);
            SetComponentObjectsActive<GlobalChatUI>(visible);
            SetComponentObjectsActive<AllianceUI>(visible);
            MainLobbyUiCoordinator.SetRightStackSuppressed(!visible);

            if (visible && ensureMainWidgets && IsMainSceneReadyForWidgetCreation())
            {
                MailboxBootstrap.EnsureForCurrentScene();
                FriendsBootstrap.EnsureForCurrentScene();
                GlobalChatBootstrap.EnsureForCurrentScene();
                AllianceBootstrap.EnsureForCurrentScene();
                RelayoutWeeklyRewardButtons();
            }
        }

        private static bool IsMainSceneReadyForWidgetCreation()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid()
                && activeScene.isLoaded
                && string.Equals(activeScene.name, "Main", StringComparison.Ordinal);
        }

        private static void RelayoutWeeklyRewardButtons()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            MainWeeklyRewardUI[] rewards = UnityEngine.Object.FindObjectsByType<MainWeeklyRewardUI>(FindObjectsInactive.Include);
            for (int i = 0; i < rewards.Length; i++)
            {
                MainWeeklyRewardUI reward = rewards[i];
                if (reward == null || reward.gameObject.scene != activeScene)
                    continue;

                reward.ForceMainMenuLayout();
            }
        }

        private static void SetNamedObjectActive(string objectName, bool active)
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && string.Equals(obj.name, objectName, StringComparison.Ordinal))
                    obj.SetActive(active);
            }
        }

        private static void SetComponentObjectsActive<T>(bool active) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null)
                    component.gameObject.SetActive(active);
            }
        }

        private static Image CreateRewardIcon(Transform parent, string objectName, int dayIndex)
        {
            GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            Image image = iconObject.GetComponent<Image>();
            ApplyRewardIcon(image, dayIndex);
            return image;
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

            Transform parent = image.transform.parent;
            if (parent != null)
                image.transform.SetSiblingIndex(Mathf.Min(2, parent.childCount - 1));
        }

        private void ClaimFree()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            if (WeeklyRewardService.ClaimFree(profile))
                SaveClaim();

            RefreshValues();
        }

        private void ClaimAd()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null || rewardedAdRequestInProgress)
                return;

            WeeklyRewardService.EnsureInitialized(profile);
            if (WeeklyRewardService.IsTimeBlocked(profile) || !WeeklyRewardService.CanClaimToday(profile))
            {
                RefreshValues();
                return;
            }

            RewardedAdAvailability availability = MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.WeeklyRewardedPlacementId);
            if (!availability.IsReady)
            {
                SetMessage(ResolveMessage(availability.Message));
                RefreshValues();
                return;
            }

            rewardedAdRequestInProgress = true;
            SetMessage(GameLocalization.Text("shop.ad_loading"));
            RefreshValues();

            MonetizationService.Ensure().ShowRewardedAd(MonetizationService.WeeklyRewardedPlacementId, result =>
            {
                rewardedAdRequestInProgress = false;
                if (result.IsCompleted && WeeklyRewardService.ClaimAd(profile))
                    SaveClaim();
                else
                    SetMessage(ResolveMessage(string.IsNullOrWhiteSpace(result.Message) ? "shop.ad_not_ready" : result.Message));

                RefreshValues();
            });
        }

        private static PlayerProfile GetProfile()
        {
            if (ProfileService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            if (ProfileService.I == null)
                return null;

            PlayerProfile profile = ProfileService.I.Current;
            if (profile == null)
            {
                ProfileRuntimeBootstrap.TryLoadCachedProfile();
                profile = ProfileService.I.Current;
            }

            if (profile != null)
                profile.EnsureData();

            return profile;
        }

        private static void SaveClaim()
        {
            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }

            if (CurrencyService.I != null)
            {
                CurrencyService.I.SetOzAltin(CurrencyService.I.GetOzAltin());
                CurrencyService.I.SetOzAmetist(CurrencyService.I.GetOzAmetist());
            }
        }

        private static string ResolveStatus(bool timeBlocked, bool canClaim, RewardedAdAvailability adAvailability)
        {
            if (timeBlocked)
                return T("Ошибка времени.", "Time error detected.", "Zaman hatasi.");
            if (canClaim)
                return adAvailability.IsReady
                    ? T("Выберите вариант получения награды", "Choose how to claim your reward", "Ödülünü nasıl alacağını seç")
                    : T("Реклама пока загружается. Попробуйте чуть позже.", "The ad is still loading. Please try again shortly.", "Reklam hâlâ yükleniyor. Lütfen biraz sonra tekrar dene.");

            return T("Возвращайтесь завтра за новой наградой", "Come back tomorrow for a new reward", "Yeni ödül için yarın geri gel");
        }

        private static string ResolveDayState(bool claimed, bool current, bool locked, WeeklyRewardClaimType claimType)
        {
            if (claimed)
                return claimType == WeeklyRewardClaimType.Ad
                    ? T("×2 ПОЛУЧЕНО", "×2 CLAIMED", "×2 ALINDI")
                    : T("ПОЛУЧЕНО", "CLAIMED", "ALINDI");

            if (locked)
                return T("ЗАКРЫТО", "LOCKED", "KİLİTLİ");

            return current ? T("ГОТОВО", "READY", "HAZIR") : T("ДАЛЕЕ", "NEXT", "SONRAKİ");
        }

        private static string ResolveMessage(string messageOrKey)
        {
            if (string.IsNullOrWhiteSpace(messageOrKey))
                return string.Empty;

            string localized = GameLocalization.Text(messageOrKey);
            return localized == messageOrKey ? messageOrKey : localized;
        }

        private static void CenterSlotText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private static void ConfigureHeaderText(TextMeshProUGUI text, float maxSize, float minSize)
        {
            if (text == null)
                return;

            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private void ConfigureTodayText()
        {
            if (todayText == null)
                return;

            todayText.alignment = TextAlignmentOptions.MidlineLeft;
            todayText.fontSize = 25f;
            todayText.fontSizeMax = 25f;
            todayText.fontSizeMin = 17f;
            todayText.enableAutoSizing = true;
            todayText.textWrappingMode = TextWrappingModes.NoWrap;
            todayText.overflowMode = TextOverflowModes.Truncate;
        }

        private void ConfigureMessageText()
        {
            if (messageText == null)
                return;

            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 27f;
            messageText.fontSizeMax = 27f;
            messageText.fontSizeMin = 18f;
            messageText.enableAutoSizing = true;
            messageText.textWrappingMode = TextWrappingModes.NoWrap;
            messageText.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyFont(messageText);
        }

        private sealed class WeeklyCarouselDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private Action onPrevious;
            private Action onNext;
            private Vector2 dragStart;

            public void Initialize(Action previous, Action next)
            {
                onPrevious = previous;
                onNext = next;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                dragStart = eventData != null ? eventData.position : Vector2.zero;
            }

            public void OnDrag(PointerEventData eventData)
            {
                // Required by Unity's drag contract so begin/end drag are routed
                // to this parent even when the gesture starts on a day card.
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (eventData == null)
                    return;

                float delta = eventData.position.x - dragStart.x;
                float threshold = Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi * 0.22f, 54f, 140f) : 54f;
                if (Mathf.Abs(delta) < threshold)
                    return;

                if (delta < 0f)
                    onNext?.Invoke();
                else
                    onPrevious?.Invoke();
            }
        }

        private static string T(string ru, string en, string tr)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            return language == GameLanguage.Russian ? ru : language == GameLanguage.Turkish ? tr : en;
        }
    }
}
