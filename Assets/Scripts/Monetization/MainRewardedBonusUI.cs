using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame.Monetization
{
    public static class MainRewardedBonusBootstrap
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

            Canvas canvas = FindMainCanvas(scene);
            if (canvas == null)
                return;

            MainRewardedBonusUI[] existing = UnityEngine.Object.FindObjectsByType<MainRewardedBonusUI>(FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                MainRewardedBonusUI candidate = existing[i];
                if (candidate != null && candidate.gameObject.scene == scene)
                {
                    candidate.RefreshLayout();
                    return;
                }
            }

            EnsureCurrencyService();
            MonetizationService.Ensure();

            GameObject host = new GameObject("MainRewardedBonusUI", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            host.AddComponent<MainRewardedBonusUI>();
        }

        private static Canvas FindMainCanvas(Scene scene)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas fallback = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null || candidate.gameObject.scene != scene)
                    continue;

                if (candidate.name == "Canvas")
                    return candidate;

                if (fallback == null && !CentralPointLayout.IsRuntimeOverlayCanvasName(candidate.name))
                    fallback = candidate;
            }

            return fallback;
        }

        private static void EnsureCurrencyService()
        {
            if (CurrencyService.I != null)
                return;

            ProfileRuntimeBootstrap.EnsureServices();
            if (CurrencyService.I != null)
                return;

            new GameObject("CurrencyService").AddComponent<CurrencyService>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainRewardedBonusUI : MonoBehaviour
    {
        private const string BlackYangTexturePath = "Monetization/MainRewardedBonus/BlackYang";
        private const string WhiteYinTexturePath = "Monetization/MainRewardedBonus/WhiteYin";
        private const string ProfileAvatarFrameResourcePath = "ProfileAvatars/ProfileAvatarFrameGenerated";

        private static readonly Color FullscreenBlack = new Color(0.003f, 0.004f, 0.01f, 1f);
        private static readonly Color CyanAccent = new Color(0.19f, 0.78f, 0.96f, 1f);
        private static readonly Color VioletAccent = new Color(0.72f, 0.36f, 0.96f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.7f, 0.76f, 0.88f, 1f);
        private static Sprite cachedProfileAvatarFrameSprite;
        private readonly WaitForSecondsRealtime refreshDelay = new WaitForSecondsRealtime(1f);

        private RectTransform root;
        private Button openButton;
        private TextMeshProUGUI openButtonLabel;
        private GameObject overlay;
        private RectTransform safeAreaRoot;
        private RectTransform dialogueStage;
        private RectTransform stageBackground;
        private RectTransform stageFrame;
        private RectTransform titlePlate;
        private RectTransform leftPortraitGroup;
        private RectTransform rightPortraitGroup;
        private RectTransform leftBubble;
        private RectTransform rightBubble;
        private RectTransform offerCard;
        private RectTransform offerIcon;
        private Button closeButton;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI blackYangNameText;
        private TextMeshProUGUI whiteYinNameText;
        private TextMeshProUGUI blackYangLineText;
        private TextMeshProUGUI whiteYinLineText;
        private TextMeshProUGUI offerTitleText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI watchButtonLabel;
        private TextMeshProUGUI notNowButtonLabel;
        private Button watchButton;
        private Button notNowButton;
        private Coroutine refreshRoutine;
        private Vector2 lastCanvasSize = new Vector2(-1f, -1f);
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private string lastStatusKey = string.Empty;
        private RewardedAdAvailabilityState lastAvailabilityState = (RewardedAdAvailabilityState)(-1);
        private string lastAvailabilityMessage = string.Empty;
        private int lastRemaining = -1;
        private bool lastRequestInProgress;
        private bool lastCanWatch;
        private bool preserveOutcomeStatus;
        private float nextSafeAreaCheckTime;

        private void Awake()
        {
            root = transform as RectTransform;
            Stretch(root);
            BuildUi();
            RefreshLayout();
        }

        private void OnEnable()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            RefreshTexts();
            RefreshLayout();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            StopRefreshing();
            bool wasOpen = overlay != null && overlay.activeSelf;
            CloseInternal(wasOpen && SceneManager.GetActiveScene().name == "Main");
        }

        private void OnDestroy()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            if (openButton != null)
                openButton.onClick.RemoveListener(Open);
            if (watchButton != null)
                watchButton.onClick.RemoveListener(WatchAd);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (notNowButton != null)
                notNowButton.onClick.RemoveListener(Close);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (root == null || root.rect.size == lastCanvasSize)
                return;

            RefreshLayout();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSafeAreaCheckTime)
                return;

            nextSafeAreaCheckTime = Time.unscaledTime + 0.5f;
            if (lastSafeArea != Screen.safeArea)
                RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (root == null)
                root = transform as RectTransform;

            Stretch(root);
            lastCanvasSize = root != null ? root.rect.size : Vector2.zero;
            lastSafeArea = Screen.safeArea;

            if (openButton != null)
                MainLobbyUiCoordinator.LayoutBottomButton(openButton.transform as RectTransform, MainLobbyBottomButtonSlot.RewardBonus);

            if (overlay != null)
                Stretch(overlay.transform as RectTransform);

            ApplySafeArea(safeAreaRoot, lastCanvasSize);
            if (dialogueStage == null || safeAreaRoot == null)
                return;

            Vector2 available = safeAreaRoot.rect.size;
            bool portrait = MainLobbyUiCoordinator.IsPortraitLayout(MainLobbyUiCoordinator.ResolveScreenSize());
            Vector2 reference = portrait ? new Vector2(1080f, 1920f) : new Vector2(2400f, 1080f);
            float scale = Mathf.Min(
                Mathf.Max(1f, available.x) / reference.x,
                Mathf.Max(1f, available.y) / reference.y);

            SetRect(dialogueStage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, reference);
            dialogueStage.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            if (portrait)
                LayoutPortrait();
            else
                LayoutLandscape();
        }

        public static void ForceCloseAll(bool notifyHub = false)
        {
            MainRewardedBonusUI[] all = FindObjectsByType<MainRewardedBonusUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].CloseInternal(notifyHub);
            }
        }

        private void BuildUi()
        {
            openButton = CreateButton(transform, "ButtonOpenMainRewardedBonus", GameLocalization.Text("main.reward_bonus.menu"), 32f);
            openButton.onClick.AddListener(Open);
            MainLobbyButtonStyle.Apply(openButton);
            openButtonLabel = openButton.GetComponentInChildren<TextMeshProUGUI>(true);

            overlay = new GameObject("MainRewardedBonusOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            Stretch(overlay.transform as RectTransform);
            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            // Main UI stays below the reserved scene-transition layer (32767).
            overlayCanvas.sortingOrder = 32766;

            Image backdrop = CreateImage(overlay.transform, "BlackBackdrop", FullscreenBlack);
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;

            safeAreaRoot = CreateRect(overlay.transform, "SafeArea");
            Stretch(safeAreaRoot);
            dialogueStage = CreateRect(safeAreaRoot, "DialogueStage");

            Image backgroundImage = CreateImage(dialogueStage, "MainBankBackground", Color.white);
            stageBackground = backgroundImage.rectTransform;
            ApplySprite(backgroundImage, MainLobbyButtonStyle.BankFullscreenBackgroundSprite, Image.Type.Simple, new Color(0.33f, 0.38f, 0.43f, 0.9f));

            Image frameImage = CreateImage(dialogueStage, "MainBankWindowFrame", Color.white);
            stageFrame = frameImage.rectTransform;
            ApplySprite(frameImage, MainLobbyButtonStyle.BankWindowFrameSprite, Image.Type.Sliced, Color.white);

            Image titlePlateImage = CreateImage(dialogueStage, "CreatorTitlePlate", Color.white);
            titlePlate = titlePlateImage.rectTransform;
            ApplySprite(titlePlateImage, MainLobbyButtonStyle.BankModuleSprite, Image.Type.Sliced, Color.white);

            titleText = CreateLabel(dialogueStage, "Title", GameLocalization.Text("main.reward_bonus.creator_title"), 56f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplySilverTextEffect(titleText);

            leftPortraitGroup = CreatePortrait(dialogueStage, "BlackYangPortrait", BlackYangTexturePath, new Rect(0.069f, 0.002f, 0.862f, 0.958f), CyanAccent, out blackYangNameText);
            rightPortraitGroup = CreatePortrait(dialogueStage, "WhiteYinPortrait", WhiteYinTexturePath, new Rect(0.11f, 0.05f, 0.776f, 0.905f), VioletAccent, out whiteYinNameText);

            leftBubble = CreateSpeechBubble(dialogueStage, "BlackYangSpeech", CyanAccent, true, out blackYangLineText);
            rightBubble = CreateSpeechBubble(dialogueStage, "WhiteYinSpeech", VioletAccent, false, out whiteYinLineText);

            offerCard = CreateRect(dialogueStage, "RewardOffer");
            Image offerImage = offerCard.gameObject.AddComponent<Image>();
            offerImage.raycastTarget = true;
            ApplySprite(offerImage, MainLobbyButtonStyle.BankModuleSprite, Image.Type.Sliced, Color.white);

            Image ametistIcon = CreateImage(offerCard, "AmetistIcon", Color.white);
            offerIcon = ametistIcon.rectTransform;
            MainLobbyButtonStyle.ApplyAmetistCurrencyIcon(ametistIcon);

            offerTitleText = CreateLabel(offerCard, "OfferTitle", GameLocalization.Text("main.reward_bonus.offer_title"), 40f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplySilverTextEffect(offerTitleText);
            bodyText = CreateLabel(offerCard, "OfferDescription", string.Empty, 34f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
            statusText = CreateLabel(offerCard, "Status", string.Empty, 30f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);

            watchButton = CreateButton(offerCard, "ButtonWatchRewardedAd", string.Empty, 36f);
            ApplyBankButton(watchButton);
            watchButton.onClick.AddListener(WatchAd);
            watchButtonLabel = watchButton.GetComponentInChildren<TextMeshProUGUI>(true);

            notNowButton = CreateButton(offerCard, "ButtonNotNow", GameLocalization.Text("main.reward_bonus.not_now"), 30f);
            ApplyBankButton(notNowButton);
            notNowButton.onClick.AddListener(Close);
            notNowButtonLabel = notNowButton.GetComponentInChildren<TextMeshProUGUI>(true);

            closeButton = CreateButton(dialogueStage, "ButtonCloseRewardBonus", "X", 26f);
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            closeButton.onClick.AddListener(Close);

            overlay.SetActive(false);
        }

        private void LayoutLandscape()
        {
            SetRect(stageBackground, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2350f, 1035f));
            SetRect(stageFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2370f, 1050f));
            SetRect(titlePlate, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1040f, 136f));
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(880f, 74f));
            SetRect(closeButton.transform as RectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-46f, -40f), new Vector2(86f, 86f));

            SetRect(leftPortraitGroup, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 75f), new Vector2(500f, 590f));
            SetRect(rightPortraitGroup, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-82f, 75f), new Vector2(500f, 590f));

            SetRect(leftBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 220f), new Vector2(1120f, 280f));
            SetRect(rightBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -40f), new Vector2(1120f, 280f));

            SetRect(offerCard, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1500f, 320f));
            LayoutOffer(false);
        }

        private void LayoutPortrait()
        {
            SetRect(stageBackground, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 1880f));
            SetRect(stageFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 1900f));
            SetRect(titlePlate, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(760f, 130f));
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(620f, 70f));
            SetRect(closeButton.transform as RectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-44f, -42f), new Vector2(74f, 74f));

            SetRect(leftPortraitGroup, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -188f), new Vector2(460f, 542f));
            SetRect(rightPortraitGroup, Vector2.one, Vector2.one, Vector2.one, new Vector2(-58f, -188f), new Vector2(460f, 542f));

            SetRect(leftBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 262f), new Vector2(900f, 250f));
            SetRect(rightBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(900f, 250f));

            SetRect(offerCard, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 66f), new Vector2(920f, 360f));
            LayoutOffer(true);
        }

        private void LayoutOffer(bool portrait)
        {
            SetRect(offerIcon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(portrait ? 80f : 96f, portrait ? -46f : -66f), new Vector2(portrait ? 78f : 120f, portrait ? 78f : 120f));
            SetRect(offerTitleText.rectTransform, new Vector2(portrait ? 0.17f : 0.18f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, portrait ? -24f : -48f), new Vector2(0f, portrait ? 42f : 48f));
            SetRect(bodyText.rectTransform, new Vector2(portrait ? 0.17f : 0.18f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, portrait ? -68f : -100f), new Vector2(0f, portrait ? 76f : 46f));
            SetRect(statusText.rectTransform, new Vector2(portrait ? 0.08f : 0.18f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, portrait ? -145f : -150f), new Vector2(0f, portrait ? 36f : 40f));

            if (portrait)
            {
                SetRect(watchButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(740f, 78f));
                SetRect(notNowButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(280f, 54f));
            }
            else
            {
                SetRect(watchButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 30f), new Vector2(900f, 96f));
                SetRect(notNowButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(490f, 40f), new Vector2(360f, 76f));
            }
        }

        private void Open()
        {
            if (overlay == null || !MainHubStateController.CanOpenMainWindow("MainRewardedBonus"))
                return;

            MainLobbyUiCoordinator.SetRightStackSuppressed(true);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            preserveOutcomeStatus = false;
            ResetStateSnapshot();
            RefreshTexts();
            RefreshLayout();
            RefreshState(true);
            StopRefreshing();
            refreshRoutine = StartCoroutine(RefreshRoutine());
        }

        private void Close()
        {
            CloseInternal(true);
        }

        private void CloseInternal(bool notifyHub)
        {
            StopRefreshing();
            if (overlay != null)
                overlay.SetActive(false);

            if (!notifyHub)
                return;

            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void WatchAd()
        {
            if (MainRewardedBonusService.IsRequestInProgress)
                return;

            preserveOutcomeStatus = false;
            SetStatus("main.reward_bonus.opening");
            if (watchButton != null)
                watchButton.interactable = false;

            MainRewardedBonusService.TryClaim((success, messageKey) =>
            {
                if (this == null)
                    return;

                SetStatus(string.IsNullOrWhiteSpace(messageKey)
                    ? (success ? "main.reward_bonus.received" : "shop.ad_not_ready")
                    : messageKey);
                preserveOutcomeStatus = true;
                RefreshState(false);
            });
        }

        private IEnumerator RefreshRoutine()
        {
            while (overlay != null && overlay.activeInHierarchy)
            {
                RefreshState(false);
                yield return refreshDelay;
            }

            refreshRoutine = null;
        }

        private void StopRefreshing()
        {
            if (refreshRoutine == null)
                return;

            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        private void RefreshState(bool forceStatus)
        {
            if (watchButton == null)
                return;

            bool profileReady = MainRewardedBonusService.IsProfileReady;
            int remaining = profileReady ? MainRewardedBonusService.GetRemainingClaimsToday() : 0;
            bool hasClaims = profileReady && remaining > 0;
            RewardedAdAvailability availability = hasClaims
                ? MainRewardedBonusService.GetAvailability()
                : new RewardedAdAvailability(RewardedAdAvailabilityState.Unavailable, MonetizationService.MainBonusRewardedPlacementId, string.Empty);
            bool requestInProgress = MainRewardedBonusService.IsRequestInProgress;
            bool canWatch = hasClaims && availability.IsReady && !requestInProgress;
            if (watchButton.interactable != canWatch)
                watchButton.interactable = canWatch;

            bool stateChanged = forceStatus ||
                                remaining != lastRemaining ||
                                requestInProgress != lastRequestInProgress ||
                                canWatch != lastCanWatch ||
                                availability.State != lastAvailabilityState ||
                                !string.Equals(availability.Message, lastAvailabilityMessage, StringComparison.Ordinal);

            lastRemaining = remaining;
            lastRequestInProgress = requestInProgress;
            lastCanWatch = canWatch;
            lastAvailabilityState = availability.State;
            lastAvailabilityMessage = availability.Message ?? string.Empty;

            if (!stateChanged || preserveOutcomeStatus)
                return;

            if (!profileReady)
                SetStatus("main.reward_bonus.profile_unavailable");
            else if (remaining <= 0)
                SetStatus("main.reward_bonus.limit");
            else if (requestInProgress)
                SetStatus("main.reward_bonus.opening");
            else if (availability.IsReady)
            {
                lastStatusKey = "main.reward_bonus.remaining";
                SetTextIfChanged(statusText, GameLocalization.Format("main.reward_bonus.remaining", remaining, MainRewardedBonusService.DailyLimit));
            }
            else
                SetStatus(string.IsNullOrWhiteSpace(availability.Message) ? "shop.ad_not_ready" : availability.Message);
        }

        private void SetStatus(string key)
        {
            lastStatusKey = key ?? string.Empty;
            if (statusText != null)
                SetTextIfChanged(statusText, GameLocalization.Text(lastStatusKey));
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshTexts();
            ResetStateSnapshot();
            RefreshState(true);
        }

        private void RefreshTexts()
        {
            string currencyName = GameLocalization.Text("common.oz_ametist");
            SetTextIfChanged(openButtonLabel, GameLocalization.Text("main.reward_bonus.menu"));
            SetTextIfChanged(titleText, GameLocalization.Text("main.reward_bonus.creator_title"));
            SetTextIfChanged(blackYangNameText, GameLocalization.Text("main.reward_bonus.black_yang"));
            SetTextIfChanged(whiteYinNameText, GameLocalization.Text("main.reward_bonus.white_yin"));
            SetTextIfChanged(blackYangLineText, GameLocalization.Text("main.reward_bonus.black_line"));
            SetTextIfChanged(whiteYinLineText, GameLocalization.Text("main.reward_bonus.white_line"));
            SetTextIfChanged(offerTitleText, GameLocalization.Text("main.reward_bonus.offer_title"));
            SetTextIfChanged(bodyText, GameLocalization.Format("main.reward_bonus.body", MainRewardedBonusService.RewardAmount, currencyName));
            SetTextIfChanged(watchButtonLabel, GameLocalization.Format("main.reward_bonus.watch", MainRewardedBonusService.RewardAmount, currencyName));
            SetTextIfChanged(notNowButtonLabel, GameLocalization.Text("main.reward_bonus.not_now"));

            if (preserveOutcomeStatus && !string.IsNullOrWhiteSpace(lastStatusKey))
                SetTextIfChanged(statusText, GameLocalization.Text(lastStatusKey));
        }

        private void ResetStateSnapshot()
        {
            lastAvailabilityState = (RewardedAdAvailabilityState)(-1);
            lastAvailabilityMessage = string.Empty;
            lastRemaining = -1;
            lastRequestInProgress = false;
            lastCanWatch = false;
        }

        private static RectTransform CreatePortrait(Transform parent, string objectName, string texturePath, Rect uvRect, Color accent, out TextMeshProUGUI nameLabel)
        {
            RectTransform group = CreateRect(parent, objectName);

            RectTransform portraitHolder = CreateRect(group, "PortraitViewport");
            // Let the portrait continue beneath the inner decorative lip so the frame hugs the image.
            SetRect(portraitHolder, new Vector2(0.1f, 0.21f), new Vector2(0.9f, 0.89f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            portraitObject.transform.SetParent(portraitHolder, false);
            RawImage portrait = portraitObject.GetComponent<RawImage>();
            Texture2D portraitTexture = Resources.Load<Texture2D>(texturePath);
            portrait.texture = portraitTexture;
            portrait.uvRect = uvRect;
            portrait.color = Color.white;
            portrait.raycastTarget = false;
            Stretch(portrait.rectTransform);
            AspectRatioFitter portraitAspect = portraitObject.GetComponent<AspectRatioFitter>();
            portraitAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            portraitAspect.aspectRatio = portraitTexture != null && portraitTexture.height > 0
                ? portraitTexture.width * uvRect.width / (portraitTexture.height * uvRect.height)
                : 1f;

            Image frame = CreateImage(group, "ProfileAvatarFrame", Color.white);
            SetRect(frame.rectTransform, new Vector2(0f, 0.15f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ApplySprite(frame, LoadProfileAvatarFrameSprite(), Image.Type.Simple, Color.white);

            Image namePlate = CreateImage(group, "NamePlate", Color.white);
            SetRect(namePlate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(440f, 96f));
            ApplySprite(namePlate, MainLobbyButtonStyle.BankButtonSprite, Image.Type.Sliced, Color.white);

            nameLabel = CreateLabel(namePlate.transform, "Name", string.Empty, 38f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
            Stretch(nameLabel.rectTransform, 48f, 14f);
            Shadow nameShadow = nameLabel.gameObject.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            nameShadow.effectDistance = new Vector2(2f, -2f);
            return group;
        }

        private static RectTransform CreateSpeechBubble(Transform parent, string objectName, Color accent, bool pointsLeft, out TextMeshProUGUI speechLabel)
        {
            RectTransform bubble = CreateRect(parent, objectName);
            Image image = bubble.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            ApplySprite(image, MainLobbyButtonStyle.BankModuleSprite, Image.Type.Sliced, Color.white);

            Image directionAccent = CreateImage(bubble, "SpeakerAccent", accent);
            SetRect(directionAccent.rectTransform,
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 48f : -48f, 0f),
                new Vector2(5f, 104f));
            directionAccent.raycastTarget = false;

            speechLabel = CreateLabel(bubble, "Speech", string.Empty, 52f, FontStyles.Normal, TextColor,
                pointsLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);
            speechLabel.fontSizeMin = 32f;
            Stretch(speechLabel.rectTransform, 72f, 42f);
            return bubble;
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void ApplySprite(Image image, Sprite sprite, Image.Type type, Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void ApplyBankButton(Button button)
        {
            if (button == null || button.image == null)
                return;

            ApplySprite(button.image, MainLobbyButtonStyle.BankButtonSprite, Image.Type.Sliced, Color.white);
            button.image.raycastTarget = true;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
                return;

            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            Stretch(label.rectTransform, 48f, 14f);
        }

        private static Sprite LoadProfileAvatarFrameSprite()
        {
            if (cachedProfileAvatarFrameSprite != null)
                return cachedProfileAvatarFrameSprite;

            cachedProfileAvatarFrameSprite = Resources.Load<Sprite>(ProfileAvatarFrameResourcePath);
            if (cachedProfileAvatarFrameSprite != null)
                return cachedProfileAvatarFrameSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(ProfileAvatarFrameResourcePath);
            if (sprites != null && sprites.Length > 0)
                cachedProfileAvatarFrameSprite = sprites[0];

            return cachedProfileAvatarFrameSprite;
        }

        private static Button CreateButton(Transform parent, string objectName, string text, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.8f, 0.88f, 1f);
            colors.disabledColor = new Color(0.45f, 0.48f, 0.56f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TextMeshProUGUI label = CreateLabel(buttonObject.transform, "Label", text, fontSize, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 24f, 16f);
            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string objectName, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(15f, fontSize * 0.58f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(label);
            return label;
        }

        private static void ApplySafeArea(RectTransform rect, Vector2 canvasSize)
        {
            if (rect == null)
                return;

            Stretch(rect);
            Rect safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0 || safe.width <= 0f || safe.height <= 0f)
                return;

            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;
            rect.offsetMin = new Vector2(Mathf.Max(0f, safe.xMin) * scaleX, Mathf.Max(0f, safe.yMin) * scaleY);
            rect.offsetMax = new Vector2(-Mathf.Max(0f, Screen.width - safe.xMax) * scaleX, -Mathf.Max(0f, Screen.height - safe.yMax) * scaleY);
        }

        private static void SetTextIfChanged(TMP_Text target, string value)
        {
            if (target != null && !string.Equals(target.text, value, StringComparison.Ordinal))
                target.text = value;
        }

        private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
