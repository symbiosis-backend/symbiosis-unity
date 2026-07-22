using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MahjongGame.Monetization;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainShopUI : MonoBehaviour
    {
        private enum ShopSection
        {
            Ametist,
            Abonelik
        }

        private static readonly Color BackdropColor = Color.black;
        private static readonly Color WindowColor = Color.black;
        private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.07f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.075f, 0.09f, 0.14f, 1f);
        private static readonly Color AccentColor = new Color(0.55f, 0.25f, 0.96f, 1f);
        private static readonly Color AccentSoftColor = new Color(0.29f, 0.12f, 0.5f, 0.9f);
        private static readonly Color CardColor = new Color(0.045f, 0.035f, 0.07f, 1f);
        private static readonly Color CardOutlineColor = new Color(0.46f, 0.3f, 0.76f, 0.95f);
        private static readonly Color LegendaryCardOutlineColor = new Color(0.95f, 0.69f, 0.2f, 1f);
        private static readonly Color RewardCardColor = new Color(0.075f, 0.05f, 0.12f, 1f);
        private static readonly Color PremiumGoldColor = new Color(1f, 0.74f, 0.22f, 1f);
        private static readonly Color RewardColor = new Color(0.065f, 0.3f, 0.24f, 1f);
        private static readonly Color DisabledColor = new Color(0.18f, 0.19f, 0.22f, 0.92f);
        private static readonly Color TextColor = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.74f, 0.8f, 0.89f, 1f);
        private static readonly Color ActiveTabColor = new Color(0.9f, 0.93f, 1f, 0.9f);
        private static readonly Color InactiveTabColor = new Color(0.52f, 0.6f, 0.74f, 0.56f);
        private const string FreeOfferSpritePath = "Mahjong/Sprites/Money/OzAmetistOffers/OzAmetistOffer_Free";
        private const string AdOfferSpritePath = "Mahjong/Sprites/Money/OzAmetistOffers/OzAmetistOffer_Ad";
        private const string SmallPackSpritePath = "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_50";
        private const string MediumPackSpritePath = "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_120";
        private const string BigPackSpritePath = "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_300";
        private const string LegendPackSpritePath = "Mahjong/Sprites/Money/OzAmetistPacks/OzAmetistPack_700";
        private const string ProductDockSpritePath = "Mahjong/Sprites/ShopUI/MainShopProductDockV2_Cropped";
        private const string RewardPanelSpritePath = "Mahjong/Sprites/ShopUI/MainShopRewardPanelV2_Cropped";
        private const string HeaderDividerSpritePath = "Mahjong/Sprites/ShopUI/MainShopHeaderDividerV2_Cropped";

        private GameObject overlay;
        private RectTransform safeAreaRoot;
        private RectTransform contentRoot;
        private Button openShopButton;
        private Button freeAmetistButton;
        private Button rewardedAdButton;
        private readonly Button[] purchaseButtons = new Button[4];
        private int purchaseButtonCount;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI balanceText;
        private Image balanceIcon;
        private TextMeshProUGUI statusText;
        private ShopSection activeSection = ShopSection.Ametist;
        private Coroutine adRefreshRoutine;
        private Coroutine liveAdStatusRoutine;
        private bool rewardedAdRequestInProgress;
        private bool purchaseRequestInProgress;
        private bool layoutReady;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;

        private static Sprite cachedFreeOfferSprite;
        private static Sprite cachedAdOfferSprite;
        private static Sprite cachedSmallPackSprite;
        private static Sprite cachedMediumPackSprite;
        private static Sprite cachedBigPackSprite;
        private static Sprite cachedLegendPackSprite;
        private static Sprite cachedProductDockSprite;
        private static Sprite cachedRewardPanelSprite;
        private static Sprite cachedHeaderDividerSprite;

        public static MainShopUI CreateInScene()
        {
            Canvas targetCanvas = ResolveMainCanvas();
            if (targetCanvas == null)
                targetCanvas = CreateCanvas();

            GameObject host = new GameObject("MainShopUI", typeof(RectTransform));
            host.transform.SetParent(targetCanvas.transform, false);
            host.layer = targetCanvas.gameObject.layer;

            RectTransform hostRect = host.GetComponent<RectTransform>();
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;

            MainShopUI ui = host.AddComponent<MainShopUI>();
            ui.Build();
            return ui;
        }

        private static Canvas ResolveMainCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas fallback = null;
            Scene activeScene = SceneManager.GetActiveScene();

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null)
                    continue;

                if (!candidate.gameObject.scene.IsValid() || candidate.gameObject.scene != activeScene)
                    continue;

                if (string.Equals(candidate.gameObject.name, "Canvas", System.StringComparison.Ordinal))
                    return candidate;

                if (fallback == null && IsUsableMainCanvas(candidate))
                    fallback = candidate;
            }

            if (fallback != null)
                return fallback;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (IsUsableMainCanvas(candidate))
                    return candidate;
            }

            return null;
        }

        public static void ForceCloseAll()
        {
            MainShopUI[] shops = FindObjectsByType<MainShopUI>(FindObjectsInactive.Include);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null)
                    shops[i].Close();
            }
        }

        public static void ForceResetAllToClosed(float blockOpenSeconds = 0f)
        {
            MainShopUI[] shops = FindObjectsByType<MainShopUI>(FindObjectsInactive.Include);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null)
                    shops[i].ForceClosedState();
            }

            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        private static bool IsUsableMainCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            string name = canvas.gameObject.name;
            return !CentralPointLayout.IsRuntimeOverlayCanvasName(name);
        }

        private void OnEnable()
        {
            CurrencyService.CurrencyChanged += RefreshBalance;
            ProfileService.ProfileChanged += RefreshBalance;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            LayoutOpenButton();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!layoutReady || safeAreaRoot == null)
                return;

            Rect safeArea = Screen.safeArea;
            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height && lastSafeArea == safeArea)
                return;

            ApplySafeArea();
            if (overlay != null && overlay.activeInHierarchy)
                ShowSection(activeSection);
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= RefreshBalance;
            ProfileService.ProfileChanged -= RefreshBalance;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            StopLiveAdStatusRefresh();
        }

        private void Build()
        {
            CreateOpenButton();
            CreateOverlay();
            RefreshBalance();
            RefreshTabColors();
            SetOverlayVisible(false);
            layoutReady = true;
            RememberCurrentScreenLayout();
        }

        private void CreateOpenButton()
        {
            openShopButton = CreateButton(transform, "ButtonOpenShop", GameLocalization.Text("menu.shop"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), MainLobbyUiCoordinator.ShopButtonPosition, MainLobbyUiCoordinator.ShopButtonSize, AccentColor, 36f);
            openShopButton.onClick.AddListener(Open);
            MainInfoHintTarget.Attach(openShopButton, "main.info.shop.title", "main.info.shop.body");
            LayoutOpenButton();
        }

        private void LayoutOpenButton()
        {
            RectTransform rect = openShopButton != null ? openShopButton.transform as RectTransform : null;
            if (rect == null)
                return;

            MainLobbyUiCoordinator.LayoutBottomButton(rect, MainLobbyBottomButtonSlot.Shop);

            TMP_Text label = openShopButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 42f;
                label.fontSizeMax = 42f;
                label.fontSizeMin = 24f;
                label.enableAutoSizing = true;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateOverlay()
        {
            overlay = new GameObject("ShopOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic));
            overlay.transform.SetParent(transform, false);
            overlay.layer = gameObject.layer;

            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 30000;

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            SolidRuntimeGraphic backdrop = overlay.GetComponent<SolidRuntimeGraphic>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            GameObject window = new GameObject("ShopWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(overlay.transform, false);
            window.layer = gameObject.layer;

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = Vector2.zero;

            Image windowGraphic = window.GetComponent<Image>();
            Sprite bankBackground = MainLobbyButtonStyle.BankFullscreenBackgroundSprite;
            windowGraphic.sprite = bankBackground;
            windowGraphic.type = Image.Type.Simple;
            windowGraphic.preserveAspect = false;
            windowGraphic.color = bankBackground != null
                ? new Color(0.58f, 0.62f, 0.68f, 1f)
                : WindowColor;
            windowGraphic.raycastTarget = true;

            safeAreaRoot = CreateRect(window.transform, "SafeAreaRoot", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ApplySafeArea();

            RectTransform header = CreateImageRect(safeAreaRoot, "ShopHeader", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 124f), Color.clear);
            Image headerImage = header.GetComponent<Image>();
            headerImage.raycastTarget = false;

            Image headerDivider = CreateIcon(header, "HeaderDivider", LoadResourceSprite(ref cachedHeaderDividerSprite, HeaderDividerSpritePath), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 9f), new Vector2(-30f, 38f));
            headerDivider.preserveAspect = true;
            headerDivider.color = new Color(0.88f, 0.94f, 1f, 0.82f);

            titleText = CreateLabel(header, "ShopTitle", GameLocalization.Text("shop.title"), 56f, FontStyles.Bold, TextColor, new Vector2(0.25f, 0f), new Vector2(0.75f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-40f, -20f), TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplySilverTextEffect(titleText);

            RectTransform balancePill = CreateImageRect(header, "AmetistBalancePill", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-108f, 5f), new Vector2(390f, 78f), new Color(0.91f, 0.93f, 1f, 0.96f));
            Image balancePillImage = balancePill.GetComponent<Image>();
            balancePillImage.sprite = LoadResourceSprite(ref cachedRewardPanelSprite, RewardPanelSpritePath);
            balancePillImage.type = Image.Type.Sliced;
            balancePillImage.preserveAspect = false;
            balancePillImage.raycastTarget = false;
            balanceIcon = CreateIcon(balancePill, "AmetistBalanceIcon", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(52f, 52f));
            MainLobbyButtonStyle.ApplyAmetistCurrencyIcon(balanceIcon);
            balanceText = CreateLabel(balancePill, "AmetistBalanceValue", string.Empty, 31f, FontStyles.Bold, PremiumGoldColor, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(156f, 0f), new Vector2(140f, -10f), TextAlignmentOptions.Center);
            balanceText.textWrappingMode = TextWrappingModes.NoWrap;

            Button closeButton = CreateButton(header, "ButtonCloseShop", "X", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 5f), new Vector2(72f, 72f), new Color(0.22f, 0.1f, 0.13f, 1f), 34f);
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            closeButton.onClick.AddListener(Close);

            RectTransform tabs = CreateRect(safeAreaRoot, "Tabs", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            SetTopStretchRect(tabs, 42f, 42f, 132f, 64f);
            HorizontalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 12f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = false;
            tabsLayout.childControlHeight = false;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.childForceExpandHeight = false;

            CreateTabButton(tabs, ShopSection.Ametist, GetSectionLabel(ShopSection.Ametist));
            if (MonetizationService.ArePurchasesSupported)
                CreateTabButton(tabs, ShopSection.Abonelik, GetSectionLabel(ShopSection.Abonelik));

            RectTransform contentPanel = CreateImageRect(safeAreaRoot, "ShopContentPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
            SetInsetRect(contentPanel, 48f, 48f, 214f, 58f);
            contentPanel.GetComponent<Image>().raycastTarget = false;

            contentRoot = CreateRect(contentPanel, "Content", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetInsetRect(contentRoot, 12f, 12f, 14f, 14f);

            statusText = CreateLabel(safeAreaRoot, "Status", string.Empty, 22f, FontStyles.Normal, MutedTextColor, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            SetBottomStretchRect(statusText.rectTransform, 48f, 48f, 14f, 30f);
            statusText.gameObject.SetActive(false);
        }

        private void CreateTabButton(Transform parent, ShopSection section, string label)
        {
            bool active = section == activeSection;
            Vector2 tabSize = active ? new Vector2(320f, 64f) : new Vector2(285f, 58f);
            float tabFontSize = active ? 30f : 27f;
            Button button = CreateButton(parent, "Tab" + section, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, tabSize, active ? ActiveTabColor : InactiveTabColor, tabFontSize);
            Image tabImage = button.image;
            if (tabImage != null)
            {
                tabImage.sprite = LoadResourceSprite(ref cachedRewardPanelSprite, RewardPanelSpritePath);
                tabImage.type = Image.Type.Sliced;
                tabImage.preserveAspect = false;
                tabImage.color = active ? ActiveTabColor : InactiveTabColor;
            }

            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = tabSize.x;
            element.preferredHeight = tabSize.y;
            SetButtonLabelLayout(button, tabFontSize, 20f, false, new Vector4(20f, 2f, 20f, 4f));
            button.onClick.AddListener(() => ShowSection(section));
        }

        private void ShowSection(ShopSection section)
        {
            if (!MonetizationService.ArePurchasesSupported && section == ShopSection.Abonelik)
                section = ShopSection.Ametist;

            activeSection = section;
            ClearContent();
            RefreshTabColors();
            SetStatus(string.Empty);

            switch (section)
            {
                case ShopSection.Ametist:
                    BuildAmetistSection();
                    break;
                case ShopSection.Abonelik:
                    BuildSubscriptionSection();
                    break;
            }
        }

        private void BuildAmetistSection()
        {
            Canvas.ForceUpdateCanvases();

            purchaseButtonCount = 0;
            freeAmetistButton = null;
            rewardedAdButton = null;

            float availableWidth = contentRoot != null && contentRoot.rect.width > 0f ? contentRoot.rect.width : 2100f;
            float availableHeight = contentRoot != null && contentRoot.rect.height > 0f ? contentRoot.rect.height : 700f;
            float packageGap = Mathf.Clamp(availableWidth * 0.012f, 16f, 28f);
            float rewardGap = Mathf.Clamp(availableWidth * 0.018f, 20f, 38f);
            float verticalGap = Mathf.Clamp(availableHeight * 0.03f, 16f, 26f);
            float packageWidth = Mathf.Max(250f, (availableWidth - packageGap * 3f) * 0.25f);
            float rewardHeight = Mathf.Clamp(availableHeight * 0.23f, 168f, 196f);
            float desiredPackageHeight = availableHeight - rewardHeight - verticalGap;
            float maxPackageHeight = Mathf.Clamp(packageWidth * 1.12f, 430f, 570f);
            float packageHeight = Mathf.Clamp(desiredPackageHeight, 380f, maxPackageHeight);
            float rewardWidth = Mathf.Max(380f, (availableWidth - rewardGap) * 0.5f);
            bool showPaidPackages = MonetizationService.ArePurchasesSupported;
            float stackHeight = showPaidPackages ? packageHeight + verticalGap + rewardHeight : rewardHeight;
            float stackTop = Mathf.Max(0f, (availableHeight - stackHeight) * 0.5f);

            if (showPaidPackages)
            {
                RectTransform packageGrid = CreateRect(contentRoot, "AmetistPackageGrid", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -stackTop), new Vector2(availableWidth, packageHeight));
                GridLayoutGroup packageLayout = packageGrid.gameObject.AddComponent<GridLayoutGroup>();
                packageLayout.cellSize = new Vector2(packageWidth, packageHeight);
                packageLayout.spacing = new Vector2(packageGap, 0f);
                packageLayout.childAlignment = TextAnchor.UpperCenter;
                packageLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                packageLayout.constraintCount = 4;

                CreatePackageButton(packageGrid, OzAmetistShopService.ProductSmall, packageWidth, packageHeight);
                CreatePackageButton(packageGrid, OzAmetistShopService.ProductMedium, packageWidth, packageHeight);
                CreatePackageButton(packageGrid, OzAmetistShopService.ProductBig, packageWidth, packageHeight);
                CreatePackageButton(packageGrid, OzAmetistShopService.ProductLegend, packageWidth, packageHeight);
            }

            float rewardTop = stackTop + (showPaidPackages ? packageHeight + verticalGap : 0f);
            RectTransform rewardGrid = CreateRect(contentRoot, "AmetistRewardGrid", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -rewardTop), new Vector2(availableWidth, rewardHeight));
            GridLayoutGroup rewardLayout = rewardGrid.gameObject.AddComponent<GridLayoutGroup>();
            rewardLayout.cellSize = new Vector2(rewardWidth, rewardHeight);
            rewardLayout.spacing = new Vector2(rewardGap, 0f);
            rewardLayout.childAlignment = TextAnchor.LowerCenter;
            rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rewardLayout.constraintCount = 2;

            bool canClaimFree = OzAmetistShopService.CanClaimFree();
            freeAmetistButton = CreateRewardOfferCard(rewardGrid, "ButtonFreeAmetist", GetFreeButtonText(), LoadFreeOfferSprite(), rewardWidth, rewardHeight, canClaimFree, RewardCardColor, CardOutlineColor);
            freeAmetistButton.onClick.AddListener(ClaimFreeAmetist);

            RewardedAdAvailability adAvailability = GetAmetistAdAvailability();
            bool canClickAd = CanClickAmetistAd(adAvailability) && !rewardedAdRequestInProgress;
            rewardedAdButton = CreateRewardOfferCard(rewardGrid, "ButtonAdAmetist", GetAdButtonText(adAvailability), LoadAdOfferSprite(), rewardWidth, rewardHeight, canClickAd, canClickAd ? RewardColor : DisabledColor, new Color(0.24f, 0.68f, 0.52f, 0.9f));
            rewardedAdButton.onClick.AddListener(ClaimRewardedAdAmetist);
        }

        private Button CreateRewardOfferCard(Transform parent, string objectName, string text, Sprite artwork, float width, float height, bool interactable, Color color, Color outlineColor)
        {
            Button button = CreateCardButton(parent, objectName, new Vector2(width, height), color, outlineColor);
            button.interactable = interactable;
            ApplyRewardPanelStyle(button, objectName.IndexOf("Ad", System.StringComparison.Ordinal) >= 0, interactable);

            float artWidth = Mathf.Clamp(height * 1.28f, 220f, 256f);
            float artInset = -48f;
            Image artworkImage = CreateIcon(button.transform, "Artwork", artwork, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(artInset, 0f), new Vector2(artWidth, 60f));
            artworkImage.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.72f);

            float labelLeft = artInset + artWidth + 14f;
            float labelRight = 38f;
            TextMeshProUGUI label = CreateLabel(button.transform, "OfferLabel", text, 29f, FontStyles.Bold, TextColor, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2((labelLeft - labelRight) * 0.5f, 0f), new Vector2(-(labelLeft + labelRight), -34f), TextAlignmentOptions.Center);
            label.fontSizeMin = 19f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            return button;
        }

        private void CreatePackageButton(Transform parent, string productId, float width, float height)
        {
            Monetization.MonetizationProduct product = OzAmetistShopService.GetProduct(productId);
            if (product == null)
                return;

            string amount = FormatAmetistAmount(product.OzAmetistAmount);
            bool legendary = productId == OzAmetistShopService.ProductLegend;
            Color outlineColor = legendary ? LegendaryCardOutlineColor : CardOutlineColor;
            Button button = CreateCardButton(parent, "Package" + productId, new Vector2(width, height), CardColor, outlineColor);
            ApplyMainCardStyle(button, legendary, height);
            button.interactable = !purchaseRequestInProgress;
            button.onClick.AddListener(() => PurchaseAmetistPackage(productId));

            if (purchaseButtonCount < purchaseButtons.Length)
                purchaseButtons[purchaseButtonCount++] = button;

            Sprite packageSprite = ResolvePackageSprite(productId);
            float artworkBottom = Mathf.Clamp(height * 0.245f, 136f, 148f);
            Image artwork = CreateIcon(button.transform, "PackageArtwork", packageSprite, new Vector2(0.08f, 0f), new Vector2(0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, (artworkBottom - 18f) * 0.5f), new Vector2(0f, -(artworkBottom + 18f)));
            artwork.color = purchaseRequestInProgress ? new Color(0.7f, 0.7f, 0.75f, 0.72f) : Color.white;

            TextMeshProUGUI amountLabel = CreateLabel(button.transform, "Amount", amount, 34f, FontStyles.Bold, legendary ? PremiumGoldColor : TextColor, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(-30f, 44f), TextAlignmentOptions.Center);
            amountLabel.fontSizeMin = 22f;
            amountLabel.textWrappingMode = TextWrappingModes.NoWrap;
            amountLabel.overflowMode = TextOverflowModes.Truncate;
            if (!legendary)
                MainLobbyButtonStyle.ApplySilverTextEffect(amountLabel);

            TextMeshProUGUI priceLabel = CreateLabel(button.transform, "Price", product.LocalPrice, 27f, FontStyles.Bold, PremiumGoldColor, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-30f, 34f), TextAlignmentOptions.Center);
            priceLabel.fontSizeMin = 20f;
        }

        private static void ApplyMainCardStyle(Button button, bool legendary, float cardHeight)
        {
            if (button == null || button.image == null)
                return;

            Outline outline = button.image.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

            button.image.color = Color.clear;

            Sprite dockSprite = LoadResourceSprite(ref cachedProductDockSprite, ProductDockSpritePath);
            if (dockSprite == null)
                return;

            float dockY = Mathf.Clamp(cardHeight * 0.28f, 148f, 160f);
            Image dock = CreateIcon(button.transform, "MainProductDock", dockSprite, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, dockY), new Vector2(-52f, 76f));
            dock.type = Image.Type.Sliced;
            dock.preserveAspect = false;
            dock.color = legendary
                ? new Color(0.93f, 0.82f, 1f, 0.98f)
                : new Color(0.86f, 0.92f, 1f, 0.84f);
            button.targetGraphic = dock;
        }

        private static void ApplyRewardPanelStyle(Button button, bool adStyle, bool interactable)
        {
            if (button == null || button.image == null)
                return;

            Outline outline = button.image.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

            Sprite panelSprite = LoadResourceSprite(ref cachedRewardPanelSprite, RewardPanelSpritePath);
            if (panelSprite == null)
                return;

            button.image.sprite = panelSprite;
            button.image.type = Image.Type.Sliced;
            button.image.preserveAspect = false;
            button.image.color = GetRewardPanelTint(adStyle, interactable);
        }

        private static Color GetRewardPanelTint(bool adStyle, bool interactable)
        {
            if (!interactable)
                return new Color(0.55f, 0.59f, 0.66f, 0.68f);

            return adStyle
                ? new Color(0.84f, 0.98f, 1f, 0.98f)
                : new Color(0.94f, 0.9f, 1f, 0.98f);
        }

        private static Sprite LoadFreeOfferSprite()
        {
            return LoadResourceSprite(ref cachedFreeOfferSprite, FreeOfferSpritePath);
        }

        private static Sprite LoadAdOfferSprite()
        {
            return LoadResourceSprite(ref cachedAdOfferSprite, AdOfferSpritePath);
        }

        private static Sprite ResolvePackageSprite(string productId)
        {
            return productId switch
            {
                OzAmetistShopService.ProductSmall => LoadResourceSprite(ref cachedSmallPackSprite, SmallPackSpritePath),
                OzAmetistShopService.ProductMedium => LoadResourceSprite(ref cachedMediumPackSprite, MediumPackSpritePath),
                OzAmetistShopService.ProductBig => LoadResourceSprite(ref cachedBigPackSprite, BigPackSpritePath),
                OzAmetistShopService.ProductLegend => LoadResourceSprite(ref cachedLegendPackSprite, LegendPackSpritePath),
                _ => null
            };
        }

        private static Sprite LoadResourceSprite(ref Sprite cache, string resourcePath)
        {
            if (cache != null)
                return cache;

            cache = Resources.Load<Sprite>(resourcePath);
            if (cache != null)
                return cache;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                cache = sprites[0];
                return cache;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            cache.name = texture.name + "_RuntimeSprite";
            return cache;
        }

        private void BuildPlaceholder(string title, string body)
        {
            RectTransform panel = CreateGraphicRect(contentRoot, "Placeholder" + title, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelColor);
            LayoutElement element = panel.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 340f;

            CreateLabel(panel, "PlaceholderTitle", title, 42f, FontStyles.Bold, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-90f, 76f), TextAlignmentOptions.Center);
            CreateLabel(panel, "PlaceholderBody", body, 30f, FontStyles.Normal, MutedTextColor, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-120f, 110f), TextAlignmentOptions.Center);
        }

        private void BuildSubscriptionSection()
        {
            Monetization.MonetizationProduct product = OzAmetistShopService.GetProduct(OzAmetistShopService.ProductWeeklyNoAds);
            string price = product != null ? product.LocalPrice : "$2.29";
            bool active = Monetization.NoAdsService.HasActiveNoAds();
            int remainingDays = Monetization.NoAdsService.GetRemainingDays();
            Canvas.ForceUpdateCanvases();
            float availableWidth = contentRoot != null && contentRoot.rect.width > 0f ? contentRoot.rect.width : 1280f;
            float availableHeight = contentRoot != null && contentRoot.rect.height > 0f ? contentRoot.rect.height : 420f;
            bool compactOffer = availableHeight < 390f || availableWidth < 1120f;
            float badgeSize = compactOffer ? 38f : 46f;
            float titleSize = compactOffer ? 72f : 88f;
            float bodySize = compactOffer ? 40f : 50f;
            float featureSize = compactOffer ? 34f : 43f;
            float priceSize = compactOffer ? 96f : 118f;
            float ctaSize = compactOffer ? 37f : 46f;
            float badgeY = compactOffer ? -4f : -10f;
            float titleY = compactOffer ? -58f : -76f;
            float bodyY = compactOffer ? -148f : -188f;
            float featureOneY = compactOffer ? -212f : -278f;
            float featureTwoY = compactOffer ? -270f : -352f;
            float featureThreeY = compactOffer ? -328f : -426f;

            RectTransform leftColumn = CreateRect(contentRoot, "PremiumTextColumn", new Vector2(0.02f, 0f), new Vector2(0.7f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            SetOffsetRect(leftColumn, compactOffer ? 8f : 14f, 14f, compactOffer ? 2f : 8f, compactOffer ? 2f : 8f);

            RectTransform buyColumn = CreateRect(contentRoot, "PremiumBuyColumn", new Vector2(0.71f, 0f), new Vector2(0.985f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetOffsetRect(buyColumn, 4f, 4f, compactOffer ? 8f : 16f, compactOffer ? 8f : 16f);

            CreateLabel(
                leftColumn,
                "PremiumBadge",
                GameLocalization.Text("shop.no_ads_week_badge"),
                badgeSize,
                FontStyles.Bold,
                PremiumGoldColor,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, badgeY),
                new Vector2(compactOffer ? 420f : 520f, compactOffer ? 52f : 66f),
                TextAlignmentOptions.Left);
            TextMeshProUGUI titleLabel = CreateLabel(
                leftColumn,
                "WeeklyNoAdsTitle",
                GameLocalization.Text("shop.no_ads_week_title"),
                titleSize,
                FontStyles.Bold,
                TextColor,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, titleY),
                new Vector2(0f, compactOffer ? 86f : 108f),
                TextAlignmentOptions.Left);
            MainLobbyButtonStyle.ApplySilverTextEffect(titleLabel);

            string body = active
                ? GameLocalization.Format("shop.no_ads_week_active", remainingDays)
                : GameLocalization.Text("shop.no_ads_week_body");

            CreateLabel(
                leftColumn,
                "WeeklyNoAdsBody",
                body,
                bodySize,
                FontStyles.Normal,
                MutedTextColor,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, bodyY),
                new Vector2(0f, compactOffer ? 58f : 72f),
                TextAlignmentOptions.Left);

            CreateSubscriptionBenefit(leftColumn, "FeatureWin", GameLocalization.Text("shop.no_ads_week_feature_1"), featureOneY, featureSize, compactOffer);
            CreateSubscriptionBenefit(leftColumn, "FeatureRanked", GameLocalization.Text("shop.no_ads_week_feature_2"), featureTwoY, featureSize, compactOffer);
            CreateSubscriptionBenefit(leftColumn, "FeatureAuto", GameLocalization.Text("shop.no_ads_week_feature_3"), featureThreeY, featureSize, compactOffer);

            CreateLabel(
                buyColumn,
                "PriceCaption",
                GameLocalization.Text("shop.no_ads_week_price_caption"),
                compactOffer ? 32f : 40f,
                FontStyles.Bold,
                MutedTextColor,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, compactOffer ? -22f : -32f),
                new Vector2(-14f, compactOffer ? 50f : 64f),
                TextAlignmentOptions.Center);

            CreateLabel(
                buyColumn,
                "PriceValue",
                price,
                priceSize,
                FontStyles.Bold,
                PremiumGoldColor,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, compactOffer ? 20f : 30f),
                new Vector2(-12f, compactOffer ? 106f : 132f),
                TextAlignmentOptions.Center);
            string ctaText = active
                ? GameLocalization.Text("shop.no_ads_week_active_cta")
                : GameLocalization.Format("shop.no_ads_week_cta", price);

            Button buyButton = CreateButton(
                buyColumn,
                "ButtonWeeklyNoAds",
                ctaText,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, compactOffer ? 30f : 42f),
                new Vector2(0f, compactOffer ? 104f : 130f),
                AccentColor,
                ctaSize);

            SetButtonLabelLayout(buyButton, ctaSize, 24f, false, new Vector4(28f, 3f, 28f, 5f));
            buyButton.interactable = !active && !purchaseRequestInProgress;
            buyButton.onClick.AddListener(PurchaseWeeklyNoAds);
        }

        private void CreateSubscriptionBenefit(Transform parent, string objectName, string text, float y, float fontSize, bool compactOffer)
        {
            CreateLabel(
                parent,
                objectName + "Marker",
                ">",
                fontSize,
                FontStyles.Bold,
                PremiumGoldColor,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(compactOffer ? 42f : 54f, y),
                new Vector2(compactOffer ? 42f : 52f, compactOffer ? 48f : 62f),
                TextAlignmentOptions.Left);
            CreateLabel(
                parent,
                objectName,
                text,
                fontSize,
                FontStyles.Bold,
                TextColor,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(compactOffer ? 96f : 122f, y),
                new Vector2(compactOffer ? -112f : -138f, compactOffer ? 48f : 62f),
                TextAlignmentOptions.Left);
        }

        private void ClaimFreeAmetist()
        {
            bool success = OzAmetistShopService.TryClaimFree();
            RefreshBalance();
            ShowSection(ShopSection.Ametist);
            SetStatus(success ? "+" + FormatAmetistAmount(OzAmetistShopService.FreeAmetistAmount) : GameLocalization.Text("shop.free_claimed"));
        }

        private void ClaimRewardedAdAmetist()
        {
            if (rewardedAdRequestInProgress)
                return;

            if (!OzAmetistShopService.CanClaimRewardedAd())
            {
                ShowSection(ShopSection.Ametist);
                SetStatus(GameLocalization.Text("shop.ad_limit"));
                return;
            }

            RewardedAdAvailability availability = GetAmetistAdAvailability();
            if (!availability.IsReady)
            {
                RefreshAmetistAdButtonState(availability);
                SetStatus(ResolveStatusMessage(availability.Message));
                return;
            }

            rewardedAdRequestInProgress = true;
            RefreshAmetistAdButtonState(availability);
            SetStatus(GameLocalization.Text("shop.ad_loading"));

            OzAmetistShopService.TryClaimRewardedAd((success, message) =>
            {
                if (this == null)
                    return;

                rewardedAdRequestInProgress = false;
                RefreshBalance();
                bool visible = overlay != null && overlay.activeInHierarchy;
                if (visible)
                    ShowSection(ShopSection.Ametist);
                SetStatus(success ? "+" + FormatAmetistAmount(OzAmetistShopService.RewardedAdAmetistAmount) : ResolveStatusMessage(message));
                if (visible && (success || string.Equals(message, "shop.ad_not_ready", System.StringComparison.Ordinal)))
                    ScheduleAdButtonRefresh();
            });
        }

        private void ScheduleAdButtonRefresh()
        {
            if (adRefreshRoutine != null)
                StopCoroutine(adRefreshRoutine);

            adRefreshRoutine = StartCoroutine(RefreshAdButtonAfterDelay());
        }

        private IEnumerator RefreshAdButtonAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (overlay != null && overlay.activeInHierarchy && activeSection == ShopSection.Ametist)
                ShowSection(ShopSection.Ametist);

            adRefreshRoutine = null;
        }

        private void PurchaseAmetistPackage(string productId)
        {
            if (!MonetizationService.ArePurchasesSupported || purchaseRequestInProgress)
                return;

            purchaseRequestInProgress = true;
            SetPurchaseButtonsInteractable(false);
            SetStatus(GameLocalization.Text("shop.purchase_loading"));

            OzAmetistShopService.TryPurchaseAmetistPackage(productId, (success, grantedAmount, message) =>
            {
                if (this == null)
                    return;

                purchaseRequestInProgress = false;
                RefreshBalance();
                if (overlay != null && overlay.activeInHierarchy)
                    ShowSection(ShopSection.Ametist);
                SetStatus(success ? "+" + FormatAmetistAmount(grantedAmount) : ResolveStatusMessage(message));
            });
        }

        private void PurchaseWeeklyNoAds()
        {
            if (!MonetizationService.ArePurchasesSupported || purchaseRequestInProgress)
                return;

            purchaseRequestInProgress = true;
            Button weeklyButton = FindButton("ButtonWeeklyNoAds");
            if (weeklyButton != null)
                weeklyButton.interactable = false;
            SetStatus(GameLocalization.Text("shop.purchase_loading"));

            OzAmetistShopService.TryPurchaseWeeklyNoAds((success, days, message) =>
            {
                if (this == null)
                    return;

                purchaseRequestInProgress = false;
                RefreshBalance();
                if (overlay != null && overlay.activeInHierarchy)
                    ShowSection(ShopSection.Abonelik);
                SetStatus(success ? GameLocalization.Format("shop.no_ads_week_purchased", days) : ResolveStatusMessage(message));
            });
        }

        private string GetFreeButtonText()
        {
            string title = GameLocalization.Text("shop.free");
            string value = OzAmetistShopService.HasClaimedFree()
                ? GameLocalization.Text("shop.claimed")
                : "+5 " + GameLocalization.Text("common.oz_ametist");
            return FormatRewardCardText(title, value, string.Empty);
        }

        private string GetAdButtonText(RewardedAdAvailability availability)
        {
            int used = OzAmetistShopService.GetDailyAdClaims();
            string state = OzAmetistShopService.CanClaimRewardedAd()
                ? ResolveStatusMessage(availability.Message)
                : GameLocalization.Text("shop.ad_limit");
            string value = "+10 " + GameLocalization.Text("common.oz_ametist") + "  " + used + "/" + OzAmetistShopService.DailyRewardedAdLimit;
            return FormatRewardCardText(GameLocalization.Text("shop.ad"), value, state);
        }

        private static string FormatRewardCardText(string title, string value, string state)
        {
            string result = "<size=82%><color=#C9D7EA>" + (title ?? string.Empty) + "</color></size>";
            if (!string.IsNullOrWhiteSpace(value))
                result += "\n<size=108%><color=#FFFFFF>" + value + "</color></size>";
            if (!string.IsNullOrWhiteSpace(state))
                result += "\n<size=76%><color=#91A8C2>" + state + "</color></size>";
            return result;
        }

        private static RewardedAdAvailability GetAmetistAdAvailability()
        {
            return MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.AmetistRewardedPlacementId);
        }

        private static bool CanClickAmetistAd(RewardedAdAvailability availability)
        {
            return OzAmetistShopService.CanClaimRewardedAd() && availability.IsReady;
        }

        private void RefreshAmetistAdButtonState()
        {
            if (activeSection != ShopSection.Ametist || overlay == null || !overlay.activeInHierarchy)
                return;

            RefreshAmetistAdButtonState(GetAmetistAdAvailability());
        }

        private void RefreshAmetistAdButtonState(RewardedAdAvailability availability)
        {
            if (overlay == null || activeSection != ShopSection.Ametist)
                return;

            Button adButton = rewardedAdButton != null ? rewardedAdButton : FindButton("ButtonAdAmetist");
            if (adButton == null)
                return;

            bool canClick = CanClickAmetistAd(availability) && !rewardedAdRequestInProgress;
            adButton.interactable = canClick;

            Image image = adButton.GetComponent<Image>();
            if (image != null)
                image.color = GetRewardPanelTint(true, canClick);

            Transform artworkTransform = adButton.transform.Find("Artwork");
            Image artwork = artworkTransform != null ? artworkTransform.GetComponent<Image>() : null;
            if (artwork != null)
                artwork.color = canClick ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.72f);

            SetButtonText(adButton, GetAdButtonText(availability));

        }

        private void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text ?? string.Empty;
                statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusText.text));
            }
        }

        private static string ResolveStatusMessage(string messageOrKey)
        {
            if (string.IsNullOrWhiteSpace(messageOrKey))
                return string.Empty;

            string localized = GameLocalization.Text(messageOrKey);
            return localized == messageOrKey ? messageOrKey : localized;
        }

        private void RefreshBalance()
        {
            if (balanceText == null)
                return;

            int ametist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            balanceText.text = CompactNumberFormatter.FormatCurrency(ametist);
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalizedText();
            if (overlay != null && overlay.activeInHierarchy)
                ShowSection(activeSection);
            RefreshBalance();
        }

        private void RefreshLocalizedText()
        {
            SetButtonText(openShopButton, GameLocalization.Text("menu.shop"));
            if (titleText != null)
                titleText.text = GameLocalization.Text("shop.title");

            SetButtonText(FindButton("Tab" + ShopSection.Ametist), GetSectionLabel(ShopSection.Ametist));
            SetButtonText(FindButton("Tab" + ShopSection.Abonelik), GetSectionLabel(ShopSection.Abonelik));
        }

        private Button FindButton(string buttonName)
        {
            if (overlay == null)
                return null;

            Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == buttonName)
                    return buttons[i];
            }

            return null;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }

        private static string GetSectionLabel(ShopSection section)
        {
            return section switch
            {
                ShopSection.Abonelik => GameLocalization.Text("shop.tab.subscription"),
                _ => GameLocalization.Text("shop.tab.ametist")
            };
        }

        private static string FormatAmetistAmount(int amount)
        {
            return CompactNumberFormatter.FormatCurrency(amount) + " " + GameLocalization.Text("common.oz_ametist");
        }

        private void RefreshTabColors()
        {
            if (overlay == null)
                return;

            Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
            RectTransform tabsRect = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null || !buttons[i].name.StartsWith("Tab"))
                    continue;

                Image image = buttons[i].GetComponent<Image>();
                if (image == null)
                    continue;

                bool active = buttons[i].name == "Tab" + activeSection;
                image.color = active ? ActiveTabColor : InactiveTabColor;

                Vector2 targetSize = active ? new Vector2(320f, 64f) : new Vector2(285f, 58f);
                RectTransform tabRect = buttons[i].transform as RectTransform;
                if (tabRect != null)
                    tabRect.sizeDelta = targetSize;

                LayoutElement layoutElement = buttons[i].GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredWidth = targetSize.x;
                    layoutElement.preferredHeight = targetSize.y;
                }

                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = active ? TextColor : MutedTextColor;
                    label.fontSizeMax = active ? 30f : 27f;
                    label.fontSize = label.fontSizeMax;
                }

                if (tabsRect == null)
                    tabsRect = buttons[i].transform.parent as RectTransform;
            }

            if (tabsRect != null)
                LayoutRebuilder.MarkLayoutForRebuild(tabsRect);
        }

        private void Open()
        {
            if (!MainHubStateController.CanOpenMainWindow("Shop"))
            {
                ForceClosedState();
                return;
            }

            SettingsMenuUI.ForceCloseAllSettingsMenus();
            SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            transform.SetAsLastSibling();
            if (overlay != null)
                overlay.transform.SetAsLastSibling();
            RefreshBalance();
            ShowSection(activeSection);
            SetOverlayVisible(true);
            StartLiveAdStatusRefresh();

            Transform introParent = overlay != null ? overlay.transform : transform;
            ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(introParent);
            if (intro != null)
            {
                intro.TryShowForCurrentProfile(
                    "shop",
                    "main.info.shop.title",
                    "main.info.shop.body",
                    "main.intro.shop.white");
            }
        }

        private void Close()
        {
            SetOverlayVisible(false);
            StopLiveAdStatusRefresh();
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void ForceClosedState()
        {
            StopLiveAdStatusRefresh();
            StopAdRefreshRoutine();
            if (overlay != null)
                overlay.SetActive(false);

            SetOverlayInputEnabled(false);
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (overlay != null)
                overlay.SetActive(visible);

            SetOverlayInputEnabled(visible);

            MainLobbyUiCoordinator.SetRightStackSuppressed(visible);
            SettingsMenuUI.SetMainSettingsButtonSuppressed(visible);

            if (visible)
                StartLiveAdStatusRefresh();
            else
                StopLiveAdStatusRefresh();
        }

        private void SetOverlayInputEnabled(bool enabled)
        {
            if (overlay == null)
                return;

            Canvas canvas = overlay.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = enabled;

            GraphicRaycaster raycaster = overlay.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = enabled;
        }

        private void StopAdRefreshRoutine()
        {
            if (adRefreshRoutine == null)
                return;

            StopCoroutine(adRefreshRoutine);
            adRefreshRoutine = null;
        }

        private void StartLiveAdStatusRefresh()
        {
            if (liveAdStatusRoutine == null)
                liveAdStatusRoutine = StartCoroutine(LiveAdStatusRefreshRoutine());
        }

        private void StopLiveAdStatusRefresh()
        {
            if (liveAdStatusRoutine == null)
                return;

            StopCoroutine(liveAdStatusRoutine);
            liveAdStatusRoutine = null;
        }

        private IEnumerator LiveAdStatusRefreshRoutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
            while (overlay != null && overlay.activeInHierarchy)
            {
                RefreshAmetistAdButtonState();
                yield return wait;
            }

            liveAdStatusRoutine = null;
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null)
                return;

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, screenWidth, screenHeight);

            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            safeAreaRoot.pivot = new Vector2(0.5f, 0.5f);
            safeAreaRoot.anchoredPosition = Vector2.zero;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            RememberCurrentScreenLayout();
        }

        private void RememberCurrentScreenLayout()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }

        private void SetPurchaseButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < purchaseButtonCount; i++)
            {
                Button purchaseButton = purchaseButtons[i];
                if (purchaseButton == null)
                    continue;

                purchaseButton.interactable = interactable;
                Transform artworkTransform = purchaseButton.transform.Find("PackageArtwork");
                Image artwork = artworkTransform != null ? artworkTransform.GetComponent<Image>() : null;
                if (artwork != null)
                    artwork.color = interactable ? Color.white : new Color(0.66f, 0.68f, 0.74f, 0.7f);
            }
        }

        private void ClearContent()
        {
            if (contentRoot == null)
                return;

            freeAmetistButton = null;
            rewardedAdButton = null;
            purchaseButtonCount = 0;
            for (int i = 0; i < purchaseButtons.Length; i++)
                purchaseButtons[i] = null;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            MainLobbyUiCoordinator.ConfigureOverlayScaler(scaler);

            return createdCanvas;
        }

        private static RectTransform CreateRect(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform CreateGraphicRect(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic));
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            SolidRuntimeGraphic graphic = rectObject.GetComponent<SolidRuntimeGraphic>();
            graphic.color = color;
            graphic.raycastTarget = true;
            return rect;
        }

        private static RectTransform CreateImageRect(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = rectObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            image.preserveAspect = false;
            return rect;
        }

        private static Image CreateIcon(Transform parent, string objectName, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;
            return icon;
        }

        private static Button CreateButton(Transform parent, string objectName, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            MainLobbyButtonStyle.Apply(button);
            image.preserveAspect = false;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.48f, 0.75f);
            button.colors = colors;

            TextMeshProUGUI label = CreateLabel(buttonObject.transform, "Label", text, fontSize, FontStyles.Bold, TextColor, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -16f), TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
            label.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        private static Button CreateCardButton(Transform parent, string objectName, Vector2 size, Color color, Color outlineColor)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = true;
            AddOutline(image, outlineColor, 2f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.75f, 0.84f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private static void AddOutline(Graphic graphic, Color color, float distance)
        {
            if (graphic == null)
                return;

            Outline outline = graphic.GetComponent<Outline>();
            if (outline == null)
                outline = graphic.gameObject.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void SetPreferredSize(Button button, float width, float height)
        {
            if (button == null)
                return;

            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();

            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private static void SetInsetRect(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetOffsetRect(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetNormalizedInsetRect(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(1f - right, 1f - top);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetTopStretchRect(RectTransform rect, float left, float right, float top, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void SetBottomStretchRect(RectTransform rect, float left, float right, float bottom, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void SetButtonLabelLayout(Button button, float maxSize, float minSize, bool multiline, Vector4 margin)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;

            label.fontSize = maxSize;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = minSize;
            label.enableAutoSizing = true;
            label.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.margin = margin;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string objectName, string text, float fontSize, FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            MainLobbyButtonStyle.ApplyFont(label);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            return label;
        }
    }
}
