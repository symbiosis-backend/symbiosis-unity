using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainShopUI : MonoBehaviour
    {
        private enum ShopSection
        {
            Mahjong,
            Ametist,
            Abonelik
        }

        private static readonly Color BackdropColor = new Color(0.02f, 0.025f, 0.03f, 0.72f);
        private static readonly Color WindowColor = new Color(0.055f, 0.065f, 0.085f, 0.98f);
        private static readonly Color PanelColor = new Color(0.13f, 0.16f, 0.2f, 1f);
        private static readonly Color ButtonColor = new Color(0.17f, 0.21f, 0.27f, 1f);
        private static readonly Color AccentColor = new Color(0.49f, 0.28f, 0.9f, 1f);
        private static readonly Color RewardColor = new Color(0.12f, 0.45f, 0.36f, 1f);
        private static readonly Color DisabledColor = new Color(0.18f, 0.19f, 0.22f, 0.92f);
        private static readonly Color TextColor = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.74f, 0.8f, 0.89f, 1f);
        private const float StoreWindowSpriteWidth = 1513f;
        private const float StoreWindowSpriteHeight = 1024f;
        private const float StoreWindowInnerLeft = 114f / StoreWindowSpriteWidth;
        private const float StoreWindowInnerRight = 117f / StoreWindowSpriteWidth;
        private const float StoreWindowInnerTop = 170f / StoreWindowSpriteHeight;
        private const float StoreWindowInnerBottom = 208f / StoreWindowSpriteHeight;

        private GameObject overlay;
        private RectTransform contentRoot;
        private Button openShopButton;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI balanceText;
        private Image balanceIcon;
        private TextMeshProUGUI statusText;
        private ShopSection activeSection = ShopSection.Ametist;

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

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null)
                    continue;

                string objectName = candidate.gameObject.name;
                bool isDoorCanvas = objectName.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isDoorCanvas)
                    continue;

                if (fallback == null)
                    fallback = candidate;

                if (string.Equals(objectName, "Canvas", System.StringComparison.Ordinal))
                    return candidate;
            }

            return fallback;
        }

        private void OnEnable()
        {
            CurrencyService.CurrencyChanged += RefreshBalance;
            ProfileService.ProfileChanged += RefreshBalance;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            LayoutOpenButton();
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= RefreshBalance;
            ProfileService.ProfileChanged -= RefreshBalance;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Build()
        {
            CreateOpenButton();
            CreateOverlay();
            RefreshBalance();
            ShowSection(activeSection);
            SetOverlayVisible(false);
        }

        private void CreateOpenButton()
        {
            openShopButton = CreateButton(transform, "ButtonOpenShop", GameLocalization.Text("menu.shop"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 42f), new Vector2(450f, 117f), AccentColor, 42f);
            openShopButton.onClick.AddListener(Open);
            LayoutOpenButton();
        }

        private void LayoutOpenButton()
        {
            RectTransform rect = openShopButton != null ? openShopButton.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(36f, 42f);
            rect.sizeDelta = new Vector2(450f, 117f);

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
            overlay = new GameObject("ShopOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(SolidRuntimeGraphic));
            overlay.transform.SetParent(transform, false);
            overlay.layer = gameObject.layer;

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
            windowRect.anchorMin = new Vector2(0.1f, 0.08f);
            windowRect.anchorMax = new Vector2(0.93f, 0.9f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = Vector2.zero;

            Image windowGraphic = window.GetComponent<Image>();
            windowGraphic.color = WindowColor;
            windowGraphic.raycastTarget = true;
            MainLobbyButtonStyle.ApplyStoreBankWindow(windowGraphic);

            RectTransform surface = CreateRect(window.transform, "Surface", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetNormalizedInsetRect(surface, StoreWindowInnerLeft, StoreWindowInnerRight, StoreWindowInnerTop, StoreWindowInnerBottom);

            Button closeButton = CreateButton(window.transform, "ButtonCloseShop", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -48f), new Vector2(72f, 72f), new Color(0.22f, 0.1f, 0.13f, 1f), 30f);
            SetButtonLabelLayout(closeButton, 30f, 18f, false, new Vector4(8f, 0f, 8f, 0f));
            closeButton.onClick.AddListener(Close);

            RectTransform tabs = CreateRect(surface, "Tabs", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            SetTopStretchRect(tabs, 28f, 28f, 10f, 64f);
            HorizontalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 14f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = false;
            tabsLayout.childControlHeight = false;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.childForceExpandHeight = false;

            CreateTabButton(tabs, ShopSection.Mahjong, GetSectionLabel(ShopSection.Mahjong));
            CreateTabButton(tabs, ShopSection.Ametist, GetSectionLabel(ShopSection.Ametist));
            CreateTabButton(tabs, ShopSection.Abonelik, GetSectionLabel(ShopSection.Abonelik));

            contentRoot = CreateRect(surface, "Content", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetInsetRect(contentRoot, 6f, 6f, 72f, 54f);

            statusText = CreateLabel(surface, "Status", string.Empty, 26f, FontStyles.Bold, MutedTextColor, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            SetBottomStretchRect(statusText.rectTransform, 24f, 24f, 16f, 36f);
        }

        private void CreateTabButton(Transform parent, ShopSection section, string label)
        {
            Button button = CreateButton(parent, "Tab" + section, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(238f, 60f), section == activeSection ? AccentColor : ButtonColor, 24f);
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 238f;
            element.preferredHeight = 60f;
            SetButtonLabelLayout(button, 24f, 16f, false, new Vector4(12f, 1f, 12f, 3f));
            button.onClick.AddListener(() => ShowSection(section));
        }

        private void ShowSection(ShopSection section)
        {
            activeSection = section;
            ClearContent();
            RefreshTabColors();

            if (statusText != null)
                statusText.text = string.Empty;

            switch (section)
            {
                case ShopSection.Mahjong:
                    BuildPlaceholder(GetSectionLabel(ShopSection.Mahjong), GameLocalization.Text("shop.placeholder.mahjong"));
                    break;
                case ShopSection.Ametist:
                    BuildAmetistSection();
                    break;
                case ShopSection.Abonelik:
                    BuildPlaceholder(GetSectionLabel(ShopSection.Abonelik), GameLocalization.Text("shop.placeholder.subscription"));
                    break;
            }
        }

        private void BuildAmetistSection()
        {
            Canvas.ForceUpdateCanvases();

            float availableWidth = contentRoot != null && contentRoot.rect.width > 0f ? contentRoot.rect.width : 920f;
            float availableHeight = contentRoot != null && contentRoot.rect.height > 0f ? contentRoot.rect.height : 420f;
            float gapX = Mathf.Clamp(availableWidth * 0.035f, 22f, 34f);
            float gapY = Mathf.Clamp(availableHeight * 0.05f, 16f, 24f);
            float cellWidth = Mathf.Clamp((availableWidth - gapX) * 0.5f, 280f, 360f);
            float cellHeight = Mathf.Clamp((availableHeight - gapY * 2f) / 3f, 92f, 112f);
            float gridWidth = cellWidth * 2f + gapX;
            float gridHeight = cellHeight * 3f + gapY * 2f;

            RectTransform grid = CreateRect(contentRoot, "AmetistOfferGrid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(gridWidth, gridHeight));
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
            gridLayout.spacing = new Vector2(gapX, gapY);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            Button freeButton = CreateButton(grid, "ButtonFreeAmetist", GetFreeButtonText(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellWidth, cellHeight), OzAmetistShopService.CanClaimFree() ? AccentColor : DisabledColor, 26f);
            SetButtonLabelLayout(freeButton, 26f, 16f, true, new Vector4(16f, 4f, 16f, 6f));
            freeButton.interactable = OzAmetistShopService.CanClaimFree();
            freeButton.onClick.AddListener(ClaimFreeAmetist);

            Button adButton = CreateButton(grid, "ButtonAdAmetist", GetAdButtonText(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellWidth, cellHeight), OzAmetistShopService.CanClaimRewardedAd() ? RewardColor : DisabledColor, 26f);
            SetButtonLabelLayout(adButton, 26f, 16f, true, new Vector4(16f, 4f, 16f, 6f));
            adButton.interactable = OzAmetistShopService.CanClaimRewardedAd();
            adButton.onClick.AddListener(ClaimRewardedAdAmetist);

            CreatePackageButton(grid, "Small", FormatAmetistAmount(50), "$0.99");
            CreatePackageButton(grid, "Medium", FormatAmetistAmount(120), "$1.99");
            CreatePackageButton(grid, "Big", FormatAmetistAmount(300), "$4.99");
            CreatePackageButton(grid, "Legend", FormatAmetistAmount(700), "$9.99");
        }

        private void CreateRewardRow()
        {
            RectTransform row = CreateRect(contentRoot, "AmetistRewardRow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 112f;

            Button freeButton = CreateButton(row, "ButtonFreeAmetist", GetFreeButtonText(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 102f), OzAmetistShopService.CanClaimFree() ? AccentColor : DisabledColor, 26f);
            SetPreferredSize(freeButton, 320f, 102f);
            SetButtonLabelLayout(freeButton, 26f, 16f, true, new Vector4(16f, 4f, 16f, 6f));
            freeButton.interactable = OzAmetistShopService.CanClaimFree();
            freeButton.onClick.AddListener(ClaimFreeAmetist);

            Button adButton = CreateButton(row, "ButtonAdAmetist", GetAdButtonText(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 102f), OzAmetistShopService.CanClaimRewardedAd() ? RewardColor : DisabledColor, 26f);
            SetPreferredSize(adButton, 320f, 102f);
            SetButtonLabelLayout(adButton, 26f, 16f, true, new Vector4(16f, 4f, 16f, 6f));
            adButton.interactable = OzAmetistShopService.CanClaimRewardedAd();
            adButton.onClick.AddListener(ClaimRewardedAdAmetist);
        }

        private void CreatePackageButton(Transform parent, string id, string amount, string price)
        {
            Button button = CreateButton(parent, "Package" + id, amount + "\n" + price, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 104f), PanelColor, 26f);
            SetButtonLabelLayout(button, 26f, 17f, true, new Vector4(14f, 4f, 14f, 6f));
            button.onClick.AddListener(() =>
            {
                if (statusText != null)
                    statusText.text = GameLocalization.Format("shop.package_stub", amount);
            });
        }

        private void BuildPlaceholder(string title, string body)
        {
            RectTransform panel = CreateGraphicRect(contentRoot, "Placeholder" + title, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelColor);
            LayoutElement element = panel.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 340f;

            CreateLabel(panel, "PlaceholderTitle", title, 42f, FontStyles.Bold, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-90f, 76f), TextAlignmentOptions.Center);
            CreateLabel(panel, "PlaceholderBody", body, 30f, FontStyles.Normal, MutedTextColor, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-120f, 110f), TextAlignmentOptions.Center);
        }

        private void ClaimFreeAmetist()
        {
            bool success = OzAmetistShopService.TryClaimFree();
            RefreshBalance();
            ShowSection(ShopSection.Ametist);

            if (statusText != null)
                statusText.text = success ? "+" + FormatAmetistAmount(5) : GameLocalization.Text("shop.free_claimed");
        }

        private void ClaimRewardedAdAmetist()
        {
            bool success = OzAmetistShopService.TryClaimRewardedAd();
            RefreshBalance();
            ShowSection(ShopSection.Ametist);

            if (statusText != null)
                statusText.text = success ? "+" + FormatAmetistAmount(10) : GameLocalization.Text("shop.ad_limit");
        }

        private string GetFreeButtonText()
        {
            return OzAmetistShopService.HasClaimedFree()
                ? GameLocalization.Text("shop.free") + "\n" + GameLocalization.Text("shop.claimed")
                : GameLocalization.Text("shop.free") + "\n+5 " + GameLocalization.Text("common.oz_ametist");
        }

        private string GetAdButtonText()
        {
            int used = OzAmetistShopService.GetDailyAdClaims();
            return GameLocalization.Text("shop.ad") + "\n+10 " + GameLocalization.Text("common.oz_ametist") + "  " + used + "/" + OzAmetistShopService.DailyRewardedAdLimit;
        }

        private void RefreshBalance()
        {
            if (balanceText == null)
                return;

            int ametist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            balanceText.text = GameLocalization.Format("shop.balance_ametist", ametist);
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalizedText();
            ShowSection(activeSection);
            RefreshBalance();
        }

        private void RefreshLocalizedText()
        {
            SetButtonText(openShopButton, GameLocalization.Text("menu.shop"));
            if (titleText != null)
                titleText.text = GameLocalization.Text("shop.title");

            SetButtonText(FindButton("Tab" + ShopSection.Mahjong), GetSectionLabel(ShopSection.Mahjong));
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
                ShopSection.Mahjong => GameLocalization.Text("shop.tab.mahjong"),
                ShopSection.Abonelik => GameLocalization.Text("shop.tab.subscription"),
                _ => GameLocalization.Text("shop.tab.ametist")
            };
        }

        private static string FormatAmetistAmount(int amount)
        {
            return amount + " " + GameLocalization.Text("common.oz_ametist");
        }

        private void RefreshTabColors()
        {
            if (overlay == null)
                return;

            Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null || !buttons[i].name.StartsWith("Tab"))
                    continue;

                Image image = buttons[i].GetComponent<Image>();
                if (image == null)
                    continue;

                bool active = buttons[i].name == "Tab" + activeSection;
                image.color = active ? AccentColor : ButtonColor;
            }
        }

        private void Open()
        {
            RefreshBalance();
            ShowSection(activeSection);
            SetOverlayVisible(true);
        }

        private void Close()
        {
            SetOverlayVisible(false);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (overlay != null)
                overlay.SetActive(visible);
        }

        private void ClearContent()
        {
            if (contentRoot == null)
                return;

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
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

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
            label.enableWordWrapping = true;
            return button;
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
            label.overflowMode = TextOverflowModes.Ellipsis;
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
