using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongTileExchangeUI : MonoBehaviour
    {
        private const int OpenButtonSortingOrder = 30010;
        private const int OverlaySortingOrder = 30120;
        private const string RuntimeCanvasName = "BattleLobbyRuntimeExchangeCanvas";
        private const string MainLobbySettingsButtonResourcePath = "Mahjong/Sprites/MainSettings/MahjongLobbySettingsButtons";
        private const string MainLobbySettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MahjongLobbySettingsWindow";
        private const string OzTileResourcePath = "Mahjong/Sprites/BattleTiles/OzTile";
        private const string BattleLobbyButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleLobbyButtonsV2";
        private static readonly Rect BattleLobbyButtonUsefulRect = Rect.zero;
        private static readonly Vector4 BattleLobbyButtonBorder = new Vector4(150f, 78f, 150f, 78f);
        private static readonly Vector2 BattleExchangeFullscreenPanelSize = new Vector2(2140f, 980f);

        private static readonly string[] SupportedLobbySceneNames =
        {
            "LobbyMahjongBattle"
        };

        [Header("Button")]
        [SerializeField] private Vector2 buttonSize = new Vector2(430f, 112f);
        [SerializeField] private Vector2 buttonOffsetFromBottomRight = new Vector2(-360f, 30f);
        [SerializeField] private string buttonObjectName = "OzTileExchangeButton";

        [Header("Window")]
        [SerializeField] private string overlayObjectName = "OzTileExchangeOverlay";
        [SerializeField] private Vector2 panelSize = new Vector2(1180f, 680f);

        private Canvas rootCanvas;
        private Button openButton;
        private Image openButtonOzTileIcon;
        private Image openButtonGoldIcon;
        private TMP_Text openButtonEqualsText;
        private GameObject overlayRoot;
        private Image spendCardImage;
        private Image receiveCardImage;
        private Image rateCardImage;
        private TMP_Text balanceText;
        private TMP_Text goldBalanceText;
        private TMP_Text rateText;
        private TMP_Text previewText;
        private TMP_Text statusText;
        private Image balanceOzTileIcon;
        private Image balanceGoldIcon;
        private Image rateOzTileIcon;
        private Image rateGoldIcon;
        private Image previewOzTileIcon;
        private Image previewGoldIcon;
        private TMP_InputField amountInput;
        private Button swapDirectionButton;
        private Button exchangeButton;
        private Button closeButton;
        private bool exchangeOzTileToGold = true;
        private static Sprite cachedMainLobbySettingsWindowSprite;
        private static Sprite cachedMainLobbySettingsButtonSprite;
        private static Sprite cachedBattleLobbyButtonSprite;
        private static Sprite cachedOzTileIconSprite;

        private bool UseBattleLobbyStyle =>
            string.Equals(gameObject.scene.name, "LobbyMahjongBattle", System.StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CleanupUnsupportedSceneObjects(scene);
            EnsureForScene(scene);
        }

        public static void RefreshBattleLobbyOpenButtonLayout()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            MahjongTileExchangeUI[] exchangeUis =
                FindObjectsByType<MahjongTileExchangeUI>(FindObjectsInactive.Include);
            for (int i = 0; i < exchangeUis.Length; i++)
            {
                MahjongTileExchangeUI ui = exchangeUis[i];
                if (ui != null && ui.gameObject.scene == activeScene)
                    ui.RefreshOpenButtonLayout();
            }
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!IsSupportedScene(scene))
            {
                CleanupUnsupportedSceneObjects(scene);
                return;
            }

            MahjongTileExchangeUI[] existingUis =
                FindObjectsByType<MahjongTileExchangeUI>(FindObjectsInactive.Include);
            for (int i = 0; i < existingUis.Length; i++)
            {
                MahjongTileExchangeUI ui = existingUis[i];
                if (ui != null && ui.gameObject.scene == scene)
                    return;
            }

            Canvas targetCanvas = GetOrCreateRuntimeCanvas(scene);
            if (targetCanvas == null)
                return;

            GameObject host = new GameObject("MahjongTileExchangeUI", typeof(RectTransform), typeof(MahjongTileExchangeUI));
            host.transform.SetParent(targetCanvas.transform, false);
        }

        private void Awake()
        {
            EnsureProfileServices();
            rootCanvas = GetComponentInParent<Canvas>();
            EnsureUi();
            RefreshUi();
        }

        private void OnEnable()
        {
            EnsureProfileServices();
            CurrencyService.CurrencyChanged += RefreshUi;
            ProfileService.ProfileChanged += RefreshUi;
            EnsureUi();
            RefreshUi();
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= RefreshUi;
            ProfileService.ProfileChanged -= RefreshUi;
        }

        private void OnDestroy()
        {
            DestroyRuntimeObject(openButton != null ? openButton.gameObject : null);
            DestroyRuntimeObject(overlayRoot);

            openButton = null;
            overlayRoot = null;
        }

        private void EnsureUi()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            Canvas sceneCanvas = GetOrCreateRuntimeCanvas(gameObject.scene);
            if (rootCanvas == null)
                rootCanvas = sceneCanvas;

            if (!IsCanvasUsableForScene(rootCanvas, gameObject.scene) || (sceneCanvas != null && rootCanvas != sceneCanvas))
            {
                rootCanvas = sceneCanvas;
                if (rootCanvas != null)
                {
                    transform.SetParent(rootCanvas.transform, false);
                    RebuildUi();
                }
            }

            if (rootCanvas == null)
                return;

            EnsureOpenButton();
            EnsureOverlay();
            EnsureEventSystem();
            RaiseOpenButton();
        }

        private void RebuildUi()
        {
            DestroyRuntimeObject(openButton != null ? openButton.gameObject : null);
            DestroyRuntimeObject(overlayRoot);

            openButton = null;
            openButtonOzTileIcon = null;
            openButtonGoldIcon = null;
            openButtonEqualsText = null;
            overlayRoot = null;
            spendCardImage = null;
            receiveCardImage = null;
            rateCardImage = null;
            balanceText = null;
            goldBalanceText = null;
            rateText = null;
            previewText = null;
            statusText = null;
            balanceOzTileIcon = null;
            balanceGoldIcon = null;
            rateOzTileIcon = null;
            rateGoldIcon = null;
            previewOzTileIcon = null;
            previewGoldIcon = null;
            amountInput = null;
            swapDirectionButton = null;
            exchangeButton = null;
            closeButton = null;
        }

        private void EnsureOpenButton()
        {
            if (openButton != null)
                return;

            GameObject buttonObject = new GameObject(
                buttonObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(rootCanvas.transform, false);
            ConfigureChildCanvas(buttonObject, OpenButtonSortingOrder);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            ApplyOpenButtonRect(rect);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            openButton = buttonObject.GetComponent<Button>();
            openButton.targetGraphic = image;
            openButton.interactable = true;
            openButton.onClick.AddListener(Open);
            if (UseBattleLobbyStyle)
            {
                ApplyBattleLobbyExchangeButton(openButton);
            }
            else
            {
                ApplyMainLobbySettingsButton(openButton);
            }

            EnsureOpenButtonExchangeIcons(buttonObject.transform);

            RaiseOpenButton();
        }

        private void EnsureOverlay()
        {
            if (overlayRoot != null)
                return;

            overlayRoot = new GameObject(
                overlayObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            overlayRoot.transform.SetParent(rootCanvas.transform, false);
            ConfigureChildCanvas(overlayRoot, OverlaySortingOrder);

            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image dim = overlayRoot.GetComponent<Image>();
            dim.color = UseBattleLobbyStyle ? Color.black : new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            Button dimButton = overlayRoot.GetComponent<Button>();
            dimButton.targetGraphic = dim;
            dimButton.onClick.AddListener(Close);

            GameObject panel = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            panel.transform.SetParent(overlayRoot.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = UseBattleLobbyStyle ? BattleExchangeFullscreenPanelSize : new Vector2(900f, 560f);
            FitPanelInsideCanvas(panelRect, rootCanvas, 30f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = Color.white;
            if (UseBattleLobbyStyle)
                BattlePopupStyle.ApplyWindow(panelImage);
            else
                ApplyMainLobbySettingsWindow(panelImage);

            Button blocker = panel.GetComponent<Button>();
            blocker.targetGraphic = panelImage;

            TMP_Text titleText = CreateText(panel.transform, "Title", LocalizedTitleText(), 36f, FontStyles.Bold, Color.white);
            SetTopLeft(titleText.rectTransform, 96f, -66f, 560f, 48f);

            closeButton = CreateButton(panel.transform, "CloseButton", LocalizedCloseText(), 21f);
            ApplyCloseButtonStyle(closeButton);
            SetTopLeft(closeButton.transform as RectTransform, 680f, -58f, 142f, 54f);
            closeButton.onClick.AddListener(Close);

            if (UseBattleLobbyStyle)
            {
                spendCardImage = CreatePanelPlate(panel.transform, "SpendCard");
                receiveCardImage = CreatePanelPlate(panel.transform, "ReceiveCard");
                rateCardImage = CreatePanelPlate(panel.transform, "RateCard");
            }

            balanceOzTileIcon = CreatePanelIcon(panel.transform, "BalanceOzTileIcon", new Vector2(122f, -150f), new Vector2(34f, 34f));
            balanceGoldIcon = CreatePanelIcon(panel.transform, "BalanceGoldIcon", new Vector2(330f, -150f), new Vector2(34f, 34f));
            balanceText = CreateText(panel.transform, "Balance", string.Empty, 24f, FontStyles.Bold, new Color(0.92f, 0.96f, 1f, 1f));
            SetTopLeft(balanceText.rectTransform, 166f, -134f, 240f, 36f);
            if (UseBattleLobbyStyle)
                goldBalanceText = CreateText(panel.transform, "GoldBalance", string.Empty, 24f, FontStyles.Bold, new Color(0.92f, 0.96f, 1f, 1f));

            rateOzTileIcon = CreatePanelIcon(panel.transform, "RateOzTileIcon", new Vector2(122f, -212f), new Vector2(32f, 32f));
            rateGoldIcon = CreatePanelIcon(panel.transform, "RateGoldIcon", new Vector2(330f, -212f), new Vector2(32f, 32f));
            rateText = CreateText(panel.transform, "Rate", string.Empty, 22f, FontStyles.Bold, new Color(0.8f, 0.88f, 1f, 1f));
            SetTopLeft(rateText.rectTransform, 166f, -198f, 240f, 34f);

            TMP_Text inputLabel = CreateText(panel.transform, "AmountLabel", LocalizedAmountText(), 22f, FontStyles.Bold, Color.white);
            SetTopLeft(inputLabel.rectTransform, 96f, -292f, 260f, 32f);

            amountInput = CreateInput(panel.transform, "AmountInput", LocalizedInputPlaceholder());
            SetTopLeft(amountInput.transform as RectTransform, 96f, -344f, 300f, 64f);
            amountInput.onValueChanged.AddListener(_ => RefreshUi());

            swapDirectionButton = CreateButton(panel.transform, "SwapDirectionButton", string.Empty, 22f);
            SetTopLeft(swapDirectionButton.transform as RectTransform, 442f, -246f, 270f, 60f);
            swapDirectionButton.onClick.AddListener(ToggleDirection);

            exchangeButton = CreateButton(panel.transform, "ExchangeButton", LocalizedExchangeText(), 24f);
            SetTopLeft(exchangeButton.transform as RectTransform, 442f, -322f, 270f, 64f);
            exchangeButton.onClick.AddListener(Exchange);

            previewOzTileIcon = CreatePanelIcon(panel.transform, "PreviewOzTileIcon", new Vector2(122f, -456f), new Vector2(34f, 34f));
            previewGoldIcon = CreatePanelIcon(panel.transform, "PreviewGoldIcon", new Vector2(330f, -456f), new Vector2(34f, 34f));
            previewText = CreateText(panel.transform, "Preview", string.Empty, 24f, FontStyles.Bold, new Color(1f, 0.92f, 0.62f, 1f));
            SetTopLeft(previewText.rectTransform, 166f, -440f, 240f, 38f);

            statusText = CreateText(panel.transform, "Status", string.Empty, 21f, FontStyles.Bold, new Color(1f, 0.66f, 0.56f, 1f));
            SetTopLeft(statusText.rectTransform, 96f, -504f, 700f, 32f);

            if (UseBattleLobbyStyle)
                ApplyBattleExchangeCompactLayout(panelRect, titleText);

            overlayRoot.SetActive(false);
        }

        private void Open()
        {
            EnsureProfileServices();
            EnsureUi();

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
                ConfigureChildCanvas(overlayRoot, OverlaySortingOrder);
                overlayRoot.transform.SetAsLastSibling();
            }
            if (openButton != null)
                openButton.gameObject.SetActive(false);

            SetStatus(string.Empty);
            RefreshUi();
        }

        private void Close()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);

            RaiseOpenButton();
        }

        private void Exchange()
        {
            EnsureProfileServices();
            int amount = ReadAmount(amountInput);
            if (amount <= 0)
            {
                SetStatus(LocalizedEnterAmountText());
                RefreshUi();
                return;
            }

            ExchangeQuote quote = GetCurrentQuote(amount);
            if (CurrencyService.I == null || quote == null || !quote.Success)
            {
                SetStatus(LocalizedExchangeFailReason(quote));
                RefreshUi();
                return;
            }

            bool success = CurrencyService.I.TryExchangeCurrency(
                quote.FromCurrencyId,
                quote.ToCurrencyId,
                amount,
                out ExchangeQuote result);

            SetStatus(success
                ? string.Format(LocalizedSuccessFormat(), amount, result.AmountOut)
                : LocalizedExchangeFailReason(result));

            RefreshUi();
        }

        private void ToggleDirection()
        {
            exchangeOzTileToGold = !exchangeOzTileToGold;
            SetStatus(string.Empty);
            RefreshUi();
        }

        private void RefreshUi()
        {
            EnsureProfileServices();

            if (overlayRoot == null || !overlayRoot.activeSelf)
                RaiseOpenButton();

            RefreshOpenButtonExchangeIcons();

            int ozTile = CurrencyService.I != null ? CurrencyService.I.GetOzTile() : 0;
            int ozAltin = CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int amount = ReadAmount(amountInput);
            ExchangeQuote quote = GetCurrentQuote(amount);

            if (balanceText != null)
                balanceText.text = UseBattleLobbyStyle
                    ? ozTile.ToString()
                    : $"{ozTile}                 {ozAltin}";

            if (goldBalanceText != null)
                goldBalanceText.text = ozAltin.ToString();

            if (rateText != null)
                rateText.text = FormatRateText(quote);

            if (previewText != null)
                previewText.text = FormatPreviewText(quote);

            RefreshPanelExchangeIcons();
            if (amount <= 0)
            {
                SetIconVisible(previewOzTileIcon, false);
                SetIconVisible(previewGoldIcon, false);
            }

            if (exchangeButton != null)
            {
                exchangeButton.interactable = amount > 0 && CurrencyService.I != null && quote != null && quote.Success;
                SetButtonLabel(exchangeButton, LocalizedExchangeText());
            }

            SetInputPlaceholder(amountInput, LocalizedInputPlaceholder());

            if (swapDirectionButton != null)
                SetButtonLabel(swapDirectionButton, exchangeOzTileToGold ? "Tile > Gold" : "Gold > Tile");

            if (closeButton != null)
            {
                SetButtonLabel(closeButton, UseBattleLobbyStyle ? string.Empty : LocalizedCloseText());
                ApplyCloseButtonStyle(closeButton);
            }
        }

        private bool CanExchange(int amount)
        {
            ExchangeQuote quote = GetCurrentQuote(amount);
            return quote != null && quote.Success;
        }

        private ExchangeQuote GetCurrentQuote(int amount)
        {
            if (CurrencyService.I == null)
                return null;

            return exchangeOzTileToGold
                ? CurrencyService.I.GetExchangeQuote(CurrencyIds.OzTile, CurrencyIds.OzAltin, amount)
                : CurrencyService.I.GetExchangeQuote(CurrencyIds.OzAltin, CurrencyIds.OzTile, amount);
        }

        private string FormatRateText(ExchangeQuote quote)
        {
            if (quote == null)
                return string.Empty;

            string direction = exchangeOzTileToGold ? "Tile > Gold" : "Gold > Tile";
            string fee = $"{Mathf.RoundToInt(quote.FeePercent * 100f)}%";
            string limit = quote.DailyLimit > 0
                ? $"{Mathf.Max(0, quote.DailyLimit - quote.DailyUsed)}/{quote.DailyLimit}"
                : "No limit";
            string rate = quote.Rate > 0f ? quote.Rate.ToString("0.###") : "-";

            return UseBattleLobbyStyle
                ? $"{direction}  Rate {rate}  Fee {fee}"
                : $"{rate}   Fee {fee}   Limit {limit}";
        }

        private string FormatPreviewText(ExchangeQuote quote)
        {
            if (quote == null)
                return string.Empty;

            if (quote.AmountIn <= 0)
                return string.Empty;

            string receive = quote.Success ? quote.AmountOut.ToString() : "-";
            string fee = quote.FeeAmount > 0 ? $"  Fee: {quote.FeeAmount}" : string.Empty;
            string limit = quote.DailyLimit > 0
                ? $"  Limit: {Mathf.Max(0, quote.DailyLimit - quote.DailyUsed)}"
                : string.Empty;

            return UseBattleLobbyStyle
                ? $"{LocalizedReceiveText()}: {receive}{fee}{limit}"
                : $"{quote.AmountIn}                 {receive}";
        }

        private string LocalizedExchangeFailReason(ExchangeQuote quote)
        {
            if (quote == null || string.IsNullOrWhiteSpace(quote.FailReason))
                return LocalizedExchangeFailedText();

            string reason = quote.FailReason;
            if (reason.IndexOf("Ametist", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return UiText("OzAmetist не участвует в бирже.", "OzAmetist is not on the market.", "OzAmetist piyasada degil.");
            if (reason.IndexOf("limit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return UiText("Дневной лимит обмена достигнут.", "Daily exchange limit reached.", "Gunluk degisim limiti doldu.");
            if (reason.IndexOf("Treasury", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return UiText("Резерв обменника слишком низкий.", "Exchange reserve is too low.", "Değişim rezervi düşük.");
            if (reason.IndexOf("balance", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return exchangeOzTileToGold ? LocalizedNotEnoughOzTileText() : LocalizedNotEnoughGoldText();

            return LocalizedExchangeFailedText();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private void RaiseOpenButton()
        {
            if (openButton == null || openButton.gameObject == null)
                return;

            RectTransform rect = openButton.transform as RectTransform;
            if (rect != null)
                ApplyOpenButtonRect(rect);

            openButton.gameObject.SetActive(true);
            openButton.interactable = true;
            ConfigureChildCanvas(openButton.gameObject, OpenButtonSortingOrder);
            openButton.transform.SetAsLastSibling();
        }

        private void RefreshOpenButtonLayout()
        {
            if (!UseBattleLobbyStyle || (overlayRoot != null && overlayRoot.activeSelf))
                return;

            EnsureUi();
            RectTransform rect = openButton != null ? openButton.transform as RectTransform : null;
            if (rect != null)
                ApplyOpenButtonRect(rect);
        }

        private static void EnsureProfileServices()
        {
            ProfileRuntimeBootstrap.EnsureServices();
        }

        private void ApplyOpenButtonRect(RectTransform rect)
        {
            if (rect == null)
                return;

            if (UseBattleLobbyStyle)
            {
                Button button = rect.GetComponent<Button>();
                MainLobbyUiCoordinator.LayoutBattleLobbyBottomButton(
                    button,
                    BattleLobbyBottomButtonSlot.Exchange,
                    GetRuntimeCanvasSize(),
                    new Vector2(390f, 100f));
                return;
            }

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = buttonOffsetFromBottomRight;
            rect.sizeDelta = buttonSize;
        }

        private Vector2 ResolveBattleLobbyActionButtonSize()
        {
            Vector2 canvasSize = GetRuntimeCanvasSize();
            float availableWidth = Mathf.Max(1f, canvasSize.x);
            float targetWidth = Mathf.Min(390f, (availableWidth - 420f) / 5f);
            targetWidth = Mathf.Clamp(targetWidth, 220f, 390f);
            float targetHeight = Mathf.Clamp(targetWidth * (100f / 390f), 72f, 100f);
            return new Vector2(targetWidth, targetHeight);
        }

        private float ResolveBattleLobbyActionButtonY()
        {
            Vector2 canvasSize = GetRuntimeCanvasSize();
            float aspect = Mathf.Max(1f, canvasSize.x) / Mathf.Max(1f, canvasSize.y);
            bool tabletLike = aspect < 1.55f;
            return -canvasSize.y * 0.5f + Mathf.Clamp(canvasSize.y * (tabletLike ? 0.108f : 0.112f), tabletLike ? 96f : 104f, tabletLike ? 124f : 128f);
        }

        private Vector2 GetRuntimeCanvasSize()
        {
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return canvasRect.rect.size;

            return new Vector2(2400f, 1080f);
        }

        private static void ConfigureChildCanvas(GameObject target, int sortingOrder)
        {
            if (target == null)
                return;

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
                canvas = target.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (target.GetComponent<GraphicRaycaster>() == null)
                target.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            BattleLobbyUiCoordinator.EnsureInputReady();
            EventSystemInputModeGuard.EnsureCompatibleEventSystems();
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private static int ReadAmount(TMP_InputField input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.text))
                return 0;

            return int.TryParse(input.text.Trim(), out int value) ? Mathf.Max(0, value) : 0;
        }

        private string LocalizedButtonText() => UseBattleLobbyStyle
            ? "OzTile = OzGold"
            : UiText("\u041E\u0431\u043C\u0435\u043D OzTile", "OzTile Exchange", "OzTile Değişim", "OzTile Tausch");
        private string LocalizedTitleText() => UiText("\u041E\u0431\u043C\u0435\u043D", "Exchange", "Takas", "Tausch");
        private string LocalizedCloseText() => UiText("\u0417\u0430\u043A\u0440\u044B\u0442\u044C", "Close", "Kapat", "Schliessen");
        private string LocalizedExchangeText() => UiText("\u041E\u0431\u043C\u0435\u043D\u044F\u0442\u044C", "Exchange", "Değiştir", "Tauschen");
        private string LocalizedAmountText() => UiText("\u0421\u0443\u043C\u043C\u0430", "Amount", "Miktar", "Betrag");
        private string LocalizedBalanceText() => UiText("\u0411\u0430\u043B\u0430\u043D\u0441", "Balance", "Bakiye", "Guthaben");
        private string LocalizedReceiveText() => UiText("\u041F\u043E\u043B\u0443\u0447\u0438\u0442\u0435", "Receive", "Al", "Erhalten");
        private string LocalizedInputPlaceholder() => UiText("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u0441\u0443\u043C\u043C\u0443", "Enter amount", "Miktar gir", "Betrag eingeben");
        private string LocalizedEnterAmountText() => UiText("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u0441\u0443\u043C\u043C\u0443.", "Enter an amount.", "Bir miktar girin.", "Gib einen Betrag ein.");
        private string LocalizedMultipleOfRateText() => UiText("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043A\u0440\u0430\u0442\u043D\u043E \u043A\u0443\u0440\u0441\u0443", "Enter a multiple of rate", "Kur katini gir", "Gib ein Vielfaches des Kurses ein");
        private string LocalizedNotEnoughOzTileText() => UiText("\u041D\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 OzTile.", "Not enough OzTile.", "Yeterli OzTile yok.", "Nicht genug OzTile.");
        private string LocalizedNotEnoughGoldText() => UiText("\u041D\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 OzAltin.", "Not enough OzAltin.", "Yeterli OzAltin yok.", "Nicht genug OzAltin.");
        private string LocalizedExchangeFailedText() => UiText("\u041E\u0431\u043C\u0435\u043D \u043D\u0435 \u0443\u0434\u0430\u043B\u0441\u044F.", "Exchange failed.", "Değişim başarısız.", "Tausch fehlgeschlagen.");
        private string LocalizedSuccessFormat() => UiText("\u041E\u0431\u043C\u0435\u043D: {0} -> {1}", "Exchanged: {0} -> {1}", "Değişim: {0} -> {1}", "Getauscht: {0} -> {1}");
        private string LocalizedBalanceFormat() => UiText("OzTile: {0}   OzAltin: {1}", "OzTile: {0}   OzAltin: {1}", "OzTile: {0}   OzAltin: {1}");
        private string LocalizedRateFormat() => UiText("\u041A\u0443\u0440\u0441: 1 OzTile = {0} OzAltin", "Rate: 1 OzTile = {0} OzAltin", "Kur: 1 OzTile = {0} OzAltin", "Kurs: 1 OzTile = {0} OzAltin");
        private string LocalizedPreviewFormat() => UiText("\u041F\u043E\u043B\u0443\u0447\u0438\u0442\u0435: {0} OzTile -> {1} OzAltin", "You receive: {0} OzTile -> {1} OzAltin", "Alacaginiz: {0} OzTile -> {1} OzAltin", "Du erhaeltst: {0} OzTile -> {1} OzAltin");

        private string UiText(string russian, string english, string turkish, string german = null)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            return language switch
            {
                GameLanguage.English => english,
                GameLanguage.Turkish => turkish,
                GameLanguage.German => string.IsNullOrWhiteSpace(german) ? english : german,
                _ => russian
            };
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, string value, float fontSize, FontStyles style, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            MainLobbyButtonStyle.ApplyFont(text);
            MainLobbyButtonStyle.ApplySilverTextEffect(text);
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.65f);
            text.enableAutoSizing = true;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private void EnsureOpenButtonExchangeIcons(Transform buttonTransform)
        {
            if (buttonTransform == null)
                return;

            Vector2 leftAnchor = UseBattleLobbyStyle ? new Vector2(0.38f, 0.5f) : new Vector2(0.25f, 0.5f);
            Vector2 rightAnchor = UseBattleLobbyStyle ? new Vector2(0.62f, 0.5f) : new Vector2(0.75f, 0.5f);
            Vector2 ozTileSize = UseBattleLobbyStyle ? new Vector2(62f, 62f) : new Vector2(46f, 46f);
            Vector2 goldSize = UseBattleLobbyStyle ? new Vector2(58f, 58f) : new Vector2(42f, 42f);
            float equalsFontSize = UseBattleLobbyStyle ? 38f : 34f;

            openButtonOzTileIcon = CreateIconImage(buttonTransform, "OzTileIcon", leftAnchor, ozTileSize);
            openButtonEqualsText = CreateText(buttonTransform, "Equals", "=", equalsFontSize, FontStyles.Bold, Color.white);
            openButtonGoldIcon = CreateIconImage(buttonTransform, "GoldIcon", rightAnchor, goldSize);

            RectTransform equalsRect = openButtonEqualsText.rectTransform;
            equalsRect.anchorMin = new Vector2(0.5f, 0.5f);
            equalsRect.anchorMax = new Vector2(0.5f, 0.5f);
            equalsRect.pivot = new Vector2(0.5f, 0.5f);
            equalsRect.anchoredPosition = new Vector2(0f, 0f);
            equalsRect.sizeDelta = UseBattleLobbyStyle ? new Vector2(54f, 54f) : new Vector2(42f, 44f);
            openButtonEqualsText.alignment = TextAlignmentOptions.Center;
            openButtonEqualsText.textWrappingMode = TextWrappingModes.NoWrap;
            openButtonEqualsText.overflowMode = TextOverflowModes.Overflow;

            RefreshOpenButtonExchangeIcons();
        }

        private static Image CreateIconImage(Transform parent, string objectName, Vector2 anchor, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreatePanelIcon(Transform parent, string objectName, Vector2 topLeftPosition, Vector2 size)
        {
            Image image = CreateIconImage(parent, objectName, new Vector2(0f, 1f), size);
            RectTransform rect = image.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = topLeftPosition;
            return image;
        }

        private static Image CreatePanelPlate(Transform parent, string objectName)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.02f, 0.017f, 0.012f, 0.58f);
            image.raycastTarget = false;
            return image;
        }

        private void RefreshOpenButtonExchangeIcons()
        {
            if (openButtonOzTileIcon != null)
            {
                openButtonOzTileIcon.sprite = ResolveOzTileIconSprite();
                openButtonOzTileIcon.enabled = openButtonOzTileIcon.sprite != null;
            }

            if (openButtonGoldIcon != null)
                ApplyGoldIcon(openButtonGoldIcon);

            if (openButtonEqualsText != null)
                openButtonEqualsText.text = "=";
        }

        private void RefreshPanelExchangeIcons()
        {
            if (UseBattleLobbyStyle)
            {
                ApplyOzTileIcon(balanceOzTileIcon);
                ApplyGoldIcon(balanceGoldIcon);

            if (exchangeOzTileToGold)
            {
                ApplyOzTileIcon(rateOzTileIcon);
                ApplyGoldIcon(rateGoldIcon);
                SetIconVisible(previewOzTileIcon, false);
                ApplyGoldIcon(previewGoldIcon);
            }
                else
                {
                    ApplyGoldIcon(rateOzTileIcon);
                    ApplyOzTileIcon(rateGoldIcon);
                    SetIconVisible(previewGoldIcon, false);
                    ApplyOzTileIcon(previewOzTileIcon);
                }
                return;
            }

            ApplyOzTileIcon(balanceOzTileIcon);
            ApplyGoldIcon(balanceGoldIcon);

            if (exchangeOzTileToGold)
            {
                ApplyOzTileIcon(rateOzTileIcon);
                ApplyGoldIcon(rateGoldIcon);
                ApplyOzTileIcon(previewOzTileIcon);
                ApplyGoldIcon(previewGoldIcon);
            }
            else
            {
                ApplyGoldIcon(rateOzTileIcon);
                ApplyOzTileIcon(rateGoldIcon);
                ApplyGoldIcon(previewOzTileIcon);
                ApplyOzTileIcon(previewGoldIcon);
            }
        }

        private static void SetIconVisible(Image image, bool visible)
        {
            if (image != null)
                image.enabled = visible;
        }

        private static void ApplyOzTileIcon(Image image)
        {
            if (image == null)
                return;

            image.sprite = ResolveOzTileIconSprite();
            image.enabled = image.sprite != null;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void ApplyGoldIcon(Image image)
        {
            if (image == null)
                return;

            MainLobbyButtonStyle.ApplyGoldCurrencyIcon(image);
            image.enabled = image.sprite != null;
        }

        private Button CreateButton(Transform parent, string objectName, string label, float fontSize)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (UseBattleLobbyStyle)
                BattlePopupStyle.ApplyButton(button);
            else
                ApplyMainLobbySettingsButton(button);

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, FontStyles.Bold, Color.white);
            CenterStretch(text.rectTransform, UseBattleLobbyStyle ? 34f : 10f, UseBattleLobbyStyle ? 9f : 4f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (UseBattleLobbyStyle)
                BattlePopupStyle.ApplyButtonLabel(button, fontSize);
            return button;
        }

        private static TMP_InputField CreateInput(Transform parent, string objectName, string placeholder)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.045f, 0.06f, 0.1f, 1f);
            image.raycastTarget = true;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(18f, 6f);
            textAreaRect.offsetMax = new Vector2(-18f, -6f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.5f));
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(placeholderText.rectTransform);

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 24f, FontStyles.Bold, Color.white);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(text.rectTransform);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 9;
            return input;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
                label.text = value;
        }

        private static void SetInputPlaceholder(TMP_InputField input, string value)
        {
            TMP_Text placeholder = input != null ? input.placeholder as TMP_Text : null;
            if (placeholder != null)
                placeholder.text = value;
        }

        private static void ApplyMainLobbySettingsButton(Button button)
        {
            Image image = button != null ? button.image : null;
            if (image == null)
                return;

            Sprite sprite = LoadMainLobbySettingsButtonSprite();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                button.targetGraphic = image;
            }
            else
            {
                MainLobbyButtonStyle.Apply(button, false);
            }
        }

        private static void ApplyBattleLobbyExchangeButton(Button button)
        {
            Image image = button != null ? button.image : null;
            if (image == null)
                return;

            Sprite sprite = LoadBattleLobbyButtonSprite();
            if (sprite == null)
            {
                BattlePopupStyle.ApplyButton(button);
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.05f, 0.92f, 1f);
            colors.pressedColor = new Color(0.82f, 0.76f, 0.64f, 1f);
            colors.selectedColor = new Color(1.04f, 1.02f, 0.9f, 1f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.82f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static Sprite LoadBattleLobbyButtonSprite()
        {
            if (cachedBattleLobbyButtonSprite != null)
                return cachedBattleLobbyButtonSprite;

            Texture2D texture = Resources.Load<Texture2D>(BattleLobbyButtonResourcePath);
            if (texture != null)
            {
                cachedBattleLobbyButtonSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    BattleLobbyButtonBorder);
                return cachedBattleLobbyButtonSprite;
            }

            Sprite source = Resources.Load<Sprite>(BattleLobbyButtonResourcePath);
            if (source == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(BattleLobbyButtonResourcePath);
                if (sprites != null && sprites.Length > 0)
                    source = sprites[0];
            }

            if (source == null || source.texture == null)
                return null;

            Rect rect = BattleLobbyButtonUsefulRect.width <= 0.5f || BattleLobbyButtonUsefulRect.height <= 0.5f
                ? source.rect
                : ClampRectToBounds(BattleLobbyButtonUsefulRect, source.textureRect);
            cachedBattleLobbyButtonSprite = Sprite.Create(
                source.texture,
                rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                BattleLobbyButtonBorder);
            return cachedBattleLobbyButtonSprite;
        }

        private static Rect ClampRectToBounds(Rect targetRect, Rect bounds)
        {
            float x = Mathf.Clamp(targetRect.x, bounds.xMin, bounds.xMax - 1f);
            float y = Mathf.Clamp(targetRect.y, bounds.yMin, bounds.yMax - 1f);
            float width = Mathf.Clamp(targetRect.width, 1f, bounds.xMax - x);
            float height = Mathf.Clamp(targetRect.height, 1f, bounds.yMax - y);
            return new Rect(x, y, width, height);
        }

        private static void ApplyMainLobbySettingsWindow(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadMainLobbySettingsWindowSprite();
            if (sprite == null)
            {
                MainLobbyButtonStyle.ApplyStoreBankWindow(image);
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;
        }

        private void ApplyBattleExchangeCompactLayout(RectTransform panelRect, TMP_Text titleText)
        {
            if (panelRect == null)
                return;

            panelRect.sizeDelta = BattleExchangeFullscreenPanelSize;

            if (titleText != null)
            {
                titleText.text = LocalizedTitleText();
                SetTopLeft(titleText.rectTransform, 130f, -78f, 1160f, 82f);
                titleText.fontSize = 64f;
                titleText.fontSizeMax = 64f;
            }

            ApplyCloseButtonStyle(closeButton);
            SetButtonLabel(closeButton, string.Empty);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, 1978f, -44f, 118f, 118f);
            SetTopLeft(spendCardImage != null ? spendCardImage.rectTransform : null, 130f, -205f, 760f, 210f);
            SetTopLeft(rateCardImage != null ? rateCardImage.rectTransform : null, 960f, -205f, 860f, 210f);
            SetTopLeft(receiveCardImage != null ? receiveCardImage.rectTransform : null, 130f, -720f, 1690f, 132f);

            SetTopLeft(balanceOzTileIcon != null ? balanceOzTileIcon.rectTransform : null, 190f, -282f, 62f, 62f);
            SetTopLeft(balanceGoldIcon != null ? balanceGoldIcon.rectTransform : null, 500f, -282f, 62f, 62f);
            SetTopLeft(balanceText != null ? balanceText.rectTransform : null, 280f, -284f, 200f, 64f);
            SetTopLeft(goldBalanceText != null ? goldBalanceText.rectTransform : null, 590f, -284f, 220f, 64f);

            SetTopLeft(rateOzTileIcon != null ? rateOzTileIcon.rectTransform : null, 1040f, -282f, 58f, 58f);
            SetTopLeft(rateGoldIcon != null ? rateGoldIcon.rectTransform : null, 1440f, -282f, 58f, 58f);
            SetTopLeft(rateText != null ? rateText.rectTransform : null, 1122f, -280f, 300f, 68f);

            SetTopLeft(amountInput != null ? amountInput.transform as RectTransform : null, 130f, -530f, 760f, 112f);
            SetTopLeft(swapDirectionButton != null ? swapDirectionButton.transform as RectTransform : null, 1040f, -504f, 650f, 96f);
            SetTopLeft(exchangeButton != null ? exchangeButton.transform as RectTransform : null, 1040f, -632f, 650f, 106f);

            SetTopLeft(previewOzTileIcon != null ? previewOzTileIcon.rectTransform : null, 190f, -754f, 68f, 68f);
            SetTopLeft(previewGoldIcon != null ? previewGoldIcon.rectTransform : null, 190f, -754f, 68f, 68f);
            SetTopLeft(previewText != null ? previewText.rectTransform : null, 290f, -752f, 980f, 72f);
            SetTopLeft(statusText != null ? statusText.rectTransform : null, 140f, -870f, 1680f, 54f);

            TMP_Text amountLabel = amountInput != null
                ? amountInput.transform.parent.Find("AmountLabel")?.GetComponent<TMP_Text>()
                : null;
            if (amountLabel != null)
            {
                amountLabel.text = $"{LocalizedAmountText()} / {LocalizedBalanceText()}";
                SetTopLeft(amountLabel.rectTransform, 140f, -462f, 740f, 52f);
                ApplyBattleTextSize(amountLabel, 38f);
            }

            ApplyBattleInputVisual(amountInput);
            ApplyBattleTextSize(balanceText, 46f);
            ApplyBattleTextSize(goldBalanceText, 46f);
            ApplyBattleTextSize(rateText, 38f);
            ApplyBattleTextSize(previewText, 46f);
            ApplyBattleTextSize(statusText, 38f);
            ApplyBattleButtonLabelSize(swapDirectionButton, 42f);
            ApplyBattleButtonLabelSize(exchangeButton, 48f);
            ApplyBattleInputTextSize(amountInput, 48f);
        }

        private static void ApplyBattleTextSize(TMP_Text text, float fontSize)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(18f, fontSize * 0.65f);
            text.enableAutoSizing = true;
        }

        private static void ApplyBattleButtonLabelSize(Button button, float fontSize)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            ApplyBattleTextSize(label, fontSize);
        }

        private void ApplyCloseButtonStyle(Button button)
        {
            if (UseBattleLobbyStyle)
                BattlePopupStyle.ApplyCloseIconButton(button);
            else
                MainLobbyButtonStyle.ApplyCloseIconButton(button);
        }

        private static void ApplyBattleInputTextSize(TMP_InputField input, float fontSize)
        {
            if (input == null)
                return;

            ApplyBattleTextSize(input.textComponent, fontSize);
            ApplyBattleTextSize(input.placeholder as TMP_Text, fontSize * 0.82f);
        }

        private static void ApplyBattleInputVisual(TMP_InputField input)
        {
            if (input == null)
                return;

            Image image = input.targetGraphic as Image;
            if (image != null)
            {
                image.color = new Color(0.025f, 0.019f, 0.013f, 0.88f);
                image.raycastTarget = true;
            }

            RectTransform viewport = input.textViewport;
            if (viewport != null)
            {
                viewport.offsetMin = new Vector2(34f, 12f);
                viewport.offsetMax = new Vector2(-34f, -12f);
            }

            TMP_Text placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
                placeholder.color = new Color(1f, 0.88f, 0.62f, 0.62f);

            if (input.textComponent != null)
                input.textComponent.color = new Color(1f, 0.94f, 0.76f, 1f);
        }

        private static Sprite LoadMainLobbySettingsButtonSprite()
        {
            if (cachedMainLobbySettingsButtonSprite != null)
                return cachedMainLobbySettingsButtonSprite;

            Sprite sprite = Resources.Load<Sprite>(MainLobbySettingsButtonResourcePath);
            if (sprite != null)
            {
                cachedMainLobbySettingsButtonSprite = sprite;
                return cachedMainLobbySettingsButtonSprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(MainLobbySettingsButtonResourcePath);
            if (sprites != null && sprites.Length > 0)
                cachedMainLobbySettingsButtonSprite = sprites[0];

            return cachedMainLobbySettingsButtonSprite;
        }

        private static Sprite LoadMainLobbySettingsWindowSprite()
        {
            if (cachedMainLobbySettingsWindowSprite != null)
                return cachedMainLobbySettingsWindowSprite;

            cachedMainLobbySettingsWindowSprite = LoadLargestSprite(MainLobbySettingsWindowResourcePath);
            return cachedMainLobbySettingsWindowSprite;
        }

        private static Sprite LoadLargestSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);

            if (sprites == null || sprites.Length == 0)
                return sprite;

            Sprite best = sprite;
            float bestArea = best != null ? best.rect.width * best.rect.height : 0f;
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite candidate = sprites[i];
                if (candidate == null)
                    continue;

                float area = candidate.rect.width * candidate.rect.height;
                if (best == null || area > bestArea)
                {
                    best = candidate;
                    bestArea = area;
                }
            }

            return best;
        }

        private static Sprite ResolveOzTileIconSprite()
        {
            if (cachedOzTileIconSprite != null)
                return cachedOzTileIconSprite;

            CurrencyView[] currencyViews = FindObjectsByType<CurrencyView>(FindObjectsInactive.Include);
            for (int i = 0; i < currencyViews.Length; i++)
            {
                if (currencyViews[i] != null)
                    currencyViews[i].Refresh();
            }

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.sprite == null)
                    continue;

                string objectName = image.gameObject.name;
                if (string.Equals(objectName, "PlayerOzTileIcon", System.StringComparison.Ordinal)
                    || string.Equals(objectName, "OzTileIcon", System.StringComparison.Ordinal)
                    || string.Equals(objectName, "TileIcon", System.StringComparison.Ordinal))
                {
                    cachedOzTileIconSprite = image.sprite;
                    return cachedOzTileIconSprite;
                }
            }

            cachedOzTileIconSprite = Resources.Load<Sprite>(OzTileResourcePath);
            return cachedOzTileIconSprite;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void FitPanelInsideCanvas(RectTransform panel, Canvas canvas, float padding)
        {
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (panel == null || canvasRect == null)
                return;

            Vector2 available = canvasRect.rect.size - Vector2.one * Mathf.Max(0f, padding * 2f);
            if (available.x <= 1f || available.y <= 1f)
                return;

            Vector2 size = panel.sizeDelta;
            float scale = Mathf.Min(1f, available.x / Mathf.Max(1f, size.x), available.y / Mathf.Max(1f, size.y));
            panel.localScale = Vector3.one * scale;
            panel.anchoredPosition = Vector2.zero;
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

        private static void CenterStretch(RectTransform rect, float horizontalMargin, float verticalMargin)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(horizontalMargin, verticalMargin);
            rect.offsetMax = new Vector2(-horizontalMargin, -verticalMargin);
        }

        private static bool IsSupportedScene(Scene scene)
        {
            string sceneName = scene.name;
            for (int i = 0; i < SupportedLobbySceneNames.Length; i++)
            {
                if (string.Equals(sceneName, SupportedLobbySceneNames[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static Canvas GetOrCreateRuntimeCanvas(Scene scene)
        {
            Canvas existing = FindRuntimeCanvas(scene);
            if (existing != null)
                return existing;

            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject canvasObject = new GameObject(
                RuntimeCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            SceneManager.MoveGameObjectToScene(canvasObject, scene);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OpenButtonSortingOrder - 1;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return canvas;
        }

        private static Canvas FindRuntimeCanvas(Scene scene)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null &&
                    canvas.gameObject.scene == scene &&
                    string.Equals(canvas.gameObject.name, RuntimeCanvasName, System.StringComparison.Ordinal))
                {
                    return canvas;
                }
            }

            return null;
        }

        private static void CleanupUnsupportedSceneObjects(Scene activeScene)
        {
            bool activeSceneSupportsExchange = IsSupportedScene(activeScene);

            MahjongTileExchangeUI[] exchangeUis =
                FindObjectsByType<MahjongTileExchangeUI>(FindObjectsInactive.Include);
            for (int i = 0; i < exchangeUis.Length; i++)
            {
                MahjongTileExchangeUI ui = exchangeUis[i];
                if (ui == null)
                    continue;

                Scene uiScene = ui.gameObject.scene;
                if (activeSceneSupportsExchange && uiScene == activeScene)
                    continue;

                DestroyRuntimeObject(ui.openButton != null ? ui.openButton.gameObject : null);
                DestroyRuntimeObject(ui.overlayRoot);
                DestroyRuntimeObject(ui.gameObject);
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null ||
                    !string.Equals(canvas.gameObject.name, RuntimeCanvasName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (activeSceneSupportsExchange && canvas.gameObject.scene == activeScene)
                    continue;

                DestroyRuntimeObject(canvas.gameObject);
            }
        }

        private static Canvas FindCanvasForScene(Scene scene)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas bestCanvas = null;
            float bestArea = -1f;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (!IsCanvasUsableForScene(canvas, scene))
                    continue;

                RectTransform rect = canvas.transform as RectTransform;
                float area = rect != null ? Mathf.Abs(rect.rect.width * rect.rect.height) : 0f;
                if (canvas.isRootCanvas)
                    area += 10_000_000f;

                if (area > bestArea)
                {
                    bestArea = area;
                    bestCanvas = canvas;
                }
            }

            return bestCanvas;
        }

        private static bool IsCanvasUsableForScene(Canvas canvas, Scene scene)
        {
            return canvas != null && canvas.gameObject.scene == scene;
        }
    }
}
