using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class MainExchangeMonitorBootstrap
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

            MainExchangeMonitorUI keep = null;
            MainExchangeMonitorUI[] all = Object.FindObjectsByType<MainExchangeMonitorUI>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                MainExchangeMonitorUI candidate = all[i];
                if (candidate == null)
                    continue;

                if (keep == null && candidate.gameObject.scene == scene)
                {
                    keep = candidate;
                    continue;
                }

                candidate.gameObject.SetActive(false);
                Object.Destroy(candidate.gameObject);
            }

            if (keep != null)
            {
                if (keep.transform.parent != canvas.transform)
                    keep.transform.SetParent(canvas.transform, false);

                keep.gameObject.SetActive(true);
                keep.ForceMainMenuLayout();
                MainLobbyUiCoordinator.SetRightStackSuppressed(keep.IsWindowOpen);
                SettingsMenuUI.SetMainSettingsButtonSuppressed(keep.IsWindowOpen);
                return;
            }

            GameObject host = new GameObject("MainExchangeMonitorUI", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            MainExchangeMonitorUI created = host.AddComponent<MainExchangeMonitorUI>();
            created.ForceMainMenuLayout();
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainExchangeMonitorUI : DynastyEconomyWindowBase
    {
        private const int ColumnCount = 6;
        private const int MaxRows = 6;
        private const int MaxDataRows = MaxRows - 1;

        private static Sprite cachedRoundedRectSprite;

        private TextMeshProUGUI infoText;
        private TextMeshProUGUI footerText;
        private RectTransform headerDividerRect;
        private RectTransform tableBackdropRect;
        private readonly RectTransform[] rowBackgroundRects = new RectTransform[MaxRows];
        private readonly RectTransform[] rowDividerRects = new RectTransform[MaxRows];
        private readonly TextMeshProUGUI[] rowCells = new TextMeshProUGUI[MaxRows * ColumnCount];
        private Coroutine fullscreenRelayoutRoutine;
        private int visibleDataRows;

        internal bool IsWindowOpen => overlayRect != null && overlayRect.gameObject.activeSelf;

        protected override string ButtonObjectName => "ExchangeMonitorButton";
        protected override string OverlayObjectName => "ExchangeMonitorOverlay";
        protected override string ButtonText => DynastyEconomyLoc.T("\u0411\u0438\u0440\u0436\u0430", "Market", "Piyasa");
        protected override string TitleText => DynastyEconomyLoc.T("\u041C\u043E\u043D\u0438\u0442\u043E\u0440 \u0431\u0438\u0440\u0436\u0438", "Exchange Monitor", "Piyasa Monitörü");
        protected override Vector2 ButtonPosition => MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Exchange);
        protected override Color AccentColor => new Color(0.08f, 0.22f, 0.26f, 0.96f);
        protected override MainLobbyLeftMenuSlot? MainMenuSlot => MainLobbyLeftMenuSlot.Exchange;

        protected override void Layout()
        {
            SetMainMenuButton(buttonRect, ButtonPosition, MainMenuSlot, MainLobbyUiCoordinator.LeftMenuWidth, MainLobbyUiCoordinator.LeftMenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 34f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            ResolveRootMetrics(out float rootWidth, out float rootHeight, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);

            float frameInsetX = Mathf.Clamp(rootWidth * 0.052f, 48f, 126f);
            float frameInsetY = Mathf.Clamp(rootHeight * 0.050f, 32f, 64f);
            float contentLeft = Mathf.Max(frameInsetX, safeLeft + 24f);
            float contentRight = Mathf.Max(frameInsetX, safeRight + 24f);
            float contentTop = Mathf.Max(frameInsetY, safeTop + 18f);
            float contentBottom = Mathf.Max(frameInsetY, safeBottom + 18f);
            float contentWidth = Mathf.Max(320f, rootWidth - contentLeft - contentRight);
            float closeSize = Mathf.Clamp(rootHeight * 0.090f, 92f, 108f);
            float closeRightInset = Mathf.Max(safeRight + 10f, Mathf.Clamp(rootWidth * 0.008f, 8f, 20f));
            float closeTopInset = Mathf.Max(safeTop + 8f, Mathf.Clamp(rootHeight * 0.008f, 6f, 14f));
            float titleHeight = Mathf.Clamp(rootHeight * 0.072f, 54f, 78f);
            float titleSideInset = closeSize + 18f;
            float titleWidth = Mathf.Max(220f, contentWidth - titleSideInset * 2f);
            float titleX = contentLeft + (contentWidth - titleWidth) * 0.5f;

            SetTopLeft(windowRect, 0f, 0f, rootWidth, rootHeight);
            DarkenBackdrop();
            ApplyContentPanelBackdrop();
            SetTopLeft(titleText != null ? titleText.rectTransform : null, titleX, -contentTop, titleWidth, titleHeight);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, rootWidth - closeRightInset - closeSize, -closeTopInset, closeSize, closeSize);

            SetObjectActive(profileGoldIcon != null ? profileGoldIcon.gameObject : null, false);
            SetObjectActive(profileAmetistIcon != null ? profileAmetistIcon.gameObject : null, false);
            SetObjectActive(profileGoldText != null ? profileGoldText.gameObject : null, false);
            SetObjectActive(profileAmetistText != null ? profileAmetistText.gameObject : null, false);

            ApplyLargeText(titleText, 48f, 30f);
            if (titleText != null)
            {
                titleText.alignment = TextAlignmentOptions.Center;
                MainLobbyButtonStyle.ApplySilverTextEffect(titleText);
            }
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);

            float dividerY = contentTop + titleHeight + 2f;
            SetTopLeft(headerDividerRect, contentLeft, -dividerY, contentWidth, 2f);

            LayoutContent(rootWidth, rootHeight, contentLeft);
            SetTopLeft(messageText != null ? messageText.rectTransform : null, contentLeft, -rootHeight + contentBottom + 4f, contentWidth, 34f);

            if (titleText != null)
                titleText.transform.SetAsLastSibling();
            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
            if (messageText != null)
                messageText.transform.SetAsLastSibling();
        }

        protected override void OnDisable()
        {
            if (fullscreenRelayoutRoutine != null)
            {
                StopCoroutine(fullscreenRelayoutRoutine);
                fullscreenRelayoutRoutine = null;
            }

            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            base.OnDisable();
        }

        protected override void Open()
        {
            base.Open();
            if (overlayRect == null || !overlayRect.gameObject.activeSelf)
                return;

            SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            Canvas.ForceUpdateCanvases();
            Layout();
            if (fullscreenRelayoutRoutine != null)
                StopCoroutine(fullscreenRelayoutRoutine);
            fullscreenRelayoutRoutine = StartCoroutine(RelayoutFullscreenAfterCanvasReady());
        }

        protected override void Close()
        {
            if (fullscreenRelayoutRoutine != null)
            {
                StopCoroutine(fullscreenRelayoutRoutine);
                fullscreenRelayoutRoutine = null;
            }

            base.Close();
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        protected override void BuildContent(Transform window)
        {
            headerDividerRect = CreatePanel(window, "MarketHeaderDivider", new Color(0.22f, 0.68f, 1f, 0.72f));
            infoText = CreateText(window, "MarketInfo", string.Empty, 20f, FontStyles.Normal, new Color(0.76f, 0.88f, 0.94f, 1f));
            tableBackdropRect = CreatePanel(window, "MarketTableBackdrop", new Color(0.008f, 0.025f, 0.050f, 0.88f));
            ApplyRoundedSprite(tableBackdropRect != null ? tableBackdropRect.GetComponent<Image>() : null, new Color(0.008f, 0.025f, 0.050f, 0.88f));
            ApplySoftOutline(tableBackdropRect, new Color(0.18f, 0.58f, 0.88f, 0.22f));

            for (int row = 0; row < MaxRows; row++)
            {
                Color rowColor = row == 0
                    ? new Color(0.055f, 0.20f, 0.30f, 0.96f)
                    : row % 2 == 0
                        ? new Color(0.028f, 0.075f, 0.125f, 0.76f)
                        : new Color(0.018f, 0.055f, 0.105f, 0.66f);
                rowBackgroundRects[row] = CreatePanel(window, $"MarketRowBackground_{row}", rowColor);
                ApplyRoundedSprite(rowBackgroundRects[row] != null ? rowBackgroundRects[row].GetComponent<Image>() : null, rowColor);
                rowDividerRects[row] = CreatePanel(window, $"MarketRowDivider_{row}", new Color(0.20f, 0.55f, 0.80f, row == 0 ? 0.55f : 0.22f));
            }

            for (int row = 0; row < MaxRows; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    int index = row * ColumnCount + column;
                    bool isHeader = row == 0;
                    rowCells[index] = CreateText(
                        window,
                        $"MarketCell_{row}_{column}",
                        string.Empty,
                        isHeader ? 32f : 30f,
                        FontStyles.Bold,
                        isHeader ? new Color(1f, 0.86f, 0.45f, 1f) : new Color(0.92f, 0.96f, 1f, 1f));
                    rowCells[index].alignment = ResolveCellAlignment(column);
                    rowCells[index].margin = ResolveCellMargin(column);
                    rowCells[index].textWrappingMode = TextWrappingModes.NoWrap;
                    rowCells[index].fontSizeMin = 18f;
                    rowCells[index].overflowMode = TextOverflowModes.Truncate;
                }
            }

            footerText = CreateText(window, "MarketLegend", string.Empty, 22f, FontStyles.Normal, new Color(0.60f, 0.76f, 0.88f, 0.94f));
            footerText.alignment = TextAlignmentOptions.MidlineLeft;
            footerText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            ResolveRootMetrics(out _, out _, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);
            float frameInsetX = Mathf.Clamp(width * 0.052f, 48f, 126f);
            float frameInsetY = Mathf.Clamp(height * 0.050f, 32f, 64f);
            float innerLeft = Mathf.Max(frameInsetX, safeLeft + 24f);
            float innerRight = Mathf.Max(frameInsetX, safeRight + 24f);
            float innerTop = Mathf.Max(frameInsetY, safeTop + 18f);
            float innerBottom = Mathf.Max(frameInsetY, safeBottom + 18f);
            float innerWidth = Mathf.Max(320f, width - innerLeft - innerRight);
            float titleHeight = Mathf.Clamp(height * 0.072f, 54f, 78f);
            float dividerY = innerTop + titleHeight + 2f;
            float infoTop = dividerY + Mathf.Clamp(height * 0.018f, 10f, 20f);
            float infoHeight = Mathf.Clamp(height * 0.052f, 38f, 58f);
            float tableTop = infoTop + infoHeight + Mathf.Clamp(height * 0.014f, 8f, 16f);
            float footerHeight = Mathf.Clamp(height * 0.040f, 30f, 42f);
            float tableAvailableHeight = Mathf.Max(0f, height - tableTop - innerBottom - footerHeight - 22f);
            float rowHeight = Mathf.Min(96f, tableAvailableHeight / MaxRows);
            if (rowHeight < 18f)
                rowHeight = 18f;
            int displayedRows = Mathf.Clamp(visibleDataRows, 0, MaxDataRows);
            float tableHeight = rowHeight * (1 + displayedRows);
            float footerTop = tableTop + tableHeight + 10f;
            float panelTop = infoTop - 12f;
            float panelBottom = height - innerBottom;
            float rowGap = Mathf.Clamp(rowHeight * 0.08f, 3f, 6f);
            float[] weights = { 1.34f, 0.72f, 0.78f, 0.64f, 0.64f, 1.54f };
            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];

            SetObjectActive(contentPanelRect != null ? contentPanelRect.gameObject : null, true);
            SetTopLeft(contentPanelRect, innerLeft - 18f, -panelTop, innerWidth + 36f, Mathf.Max(80f, panelBottom - panelTop));
            SetTopLeft(infoText != null ? infoText.rectTransform : null, innerLeft, -infoTop, innerWidth, infoHeight);
            ApplyLargeText(infoText, 32f, 20f);
            if (infoText != null)
                infoText.alignment = TextAlignmentOptions.Center;

            SetTopLeft(tableBackdropRect, innerLeft, -tableTop, innerWidth, tableHeight);

            for (int row = 0; row < MaxRows; row++)
            {
                bool rowVisible = row == 0 || row <= displayedRows;
                float x = innerLeft;
                float y = -tableTop - row * rowHeight;
                SetObjectActive(rowBackgroundRects[row] != null ? rowBackgroundRects[row].gameObject : null, rowVisible);
                SetObjectActive(rowDividerRects[row] != null ? rowDividerRects[row].gameObject : null, rowVisible && row == 0);
                SetTopLeft(rowBackgroundRects[row], innerLeft + 3f, y - rowGap * 0.5f, innerWidth - 6f, rowHeight - rowGap);
                SetTopLeft(rowDividerRects[row], innerLeft + 18f, y - rowHeight + 1.5f, innerWidth - 36f, 1.5f);

                for (int column = 0; column < ColumnCount; column++)
                {
                    float columnWidth = innerWidth * (weights[column] / totalWeight);
                    TextMeshProUGUI cell = rowCells[row * ColumnCount + column];
                    SetObjectActive(cell != null ? cell.gameObject : null, rowVisible);
                    SetTopLeft(cell != null ? cell.rectTransform : null, x, y, columnWidth, rowHeight);
                    ApplyCellText(cell, row, column, rowHeight);
                    x += columnWidth;
                }
            }

            SetTopLeft(footerText != null ? footerText.rectTransform : null, innerLeft + 8f, -footerTop, innerWidth - 16f, footerHeight);
            ApplyLargeText(footerText, 23f, 17f);
        }

        protected override void RefreshContentText()
        {
            for (int column = 0; column < ColumnCount; column++)
                SetCell(0, column, HeaderText(column));

            SetLabel(infoText, DynastyEconomyLoc.T(
                "\u041E\u0444\u0438\u0446\u0438\u0430\u043B\u044C\u043D\u044B\u0439 \u043A\u0443\u0440\u0441 Symbiosis. OzAmetist \u043D\u0435 \u0442\u043E\u0440\u0433\u0443\u0435\u0442\u0441\u044F.",
                "Official Symbiosis rates. OzAmetist is not traded.",
                "Resm\u00EE Symbiosis kurlar\u0131. OzAmetist piyasada de\u011Fil."));

            UpdateFooterText(0);
        }

        protected override void RefreshContentValues()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
                profile.EnsureData();

            ExchangeMarketConfig config = ExchangeMarketService.Config;
            config.EnsureDefaults();

            ClearDataRows();
            if (profile != null && profile.ExchangeMarket != null)
                profile.ExchangeMarket.EnsureData(config);

            int displayRow = 1;
            int hiddenPairCount = 0;
            for (int i = 0; i < config.Pairs.Count; i++)
            {
                ExchangePairConfig pair = config.Pairs[i];
                if (pair == null || !pair.Enabled)
                    continue;

                if (displayRow >= MaxRows)
                {
                    hiddenPairCount++;
                    continue;
                }

                ExchangePairRuntimeState state = null;
                ExchangeTreasuryState treasury = null;
                ExchangeDailyCounter counter = null;
                if (profile != null && profile.ExchangeMarket != null)
                {
                    state = profile.ExchangeMarket.EnsurePairState(pair);
                    treasury = profile.ExchangeMarket.EnsureTreasury(pair.TreasuryId);
                    counter = profile.ExchangeMarket.GetDailyCounter(pair);
                }

                float currentRate = state != null ? state.CurrentRate : Mathf.Max(0.01f, pair.CurrentRate);
                float move = pair.BaseRate > 0f ? (currentRate / pair.BaseRate - 1f) * 100f : 0f;
                int outputUsed = counter != null ? counter.OutputAmount : 0;
                int reserve = Mathf.Max(0, treasury != null ? treasury.ReserveOzAltin : 0);
                int dailyLimit = Mathf.Max(0, pair.DailyOutputLimit);
                int remainingLimit = Mathf.Max(0, dailyLimit - Mathf.Max(0, outputUsed));

                SetCell(displayRow, 0, $"{CurrencyName(config, pair.BaseCurrencyId)}/{CurrencyName(config, pair.GameCurrencyId)}");
                SetCell(displayRow, 1, currentRate.ToString("0.###"));
                SetCell(displayRow, 2, FormatMove(move));
                SetCell(displayRow, 3, FormatPercent(pair.InputFeePercent));
                SetCell(displayRow, 4, FormatPercent(pair.OutputFeePercent));
                SetCell(displayRow, 5, $"{remainingLimit}/{dailyLimit}   {DynastyEconomyLoc.T("\u0420", "R", "R")}:{reserve}");
                SetCellColor(displayRow, 2, MoveColor(move));
                displayRow++;
            }

            int previousVisibleRows = visibleDataRows;
            visibleDataRows = displayRow - 1;
            UpdateFooterText(hiddenPairCount);
            if (previousVisibleRows != visibleDataRows)
                Layout();
        }

        private void ClearDataRows()
        {
            for (int row = 1; row < MaxRows; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    SetCell(row, column, string.Empty);
                    SetCellColor(row, column, new Color(0.92f, 0.96f, 1f, 1f));
                }
            }
        }

        private void SetCell(int row, int column, string value)
        {
            int index = row * ColumnCount + column;
            if (index >= 0 && index < rowCells.Length && rowCells[index] != null)
                rowCells[index].text = value;
        }

        private void SetCellColor(int row, int column, Color color)
        {
            int index = row * ColumnCount + column;
            if (index >= 0 && index < rowCells.Length && rowCells[index] != null)
                rowCells[index].color = color;
        }

        private void UpdateFooterText(int hiddenPairCount)
        {
            string legend = visibleDataRows == 0
                ? DynastyEconomyLoc.T("\u0410\u043A\u0442\u0438\u0432\u043D\u044B\u0445 \u0432\u0430\u043B\u044E\u0442\u043D\u044B\u0445 \u043F\u0430\u0440 \u043F\u043E\u043A\u0430 \u043D\u0435\u0442.", "There are no active currency pairs yet.", "Hen\u00FCz aktif d\u00F6viz paritesi yok.")
                : DynastyEconomyLoc.T("\u041B\u0438\u043C\u0438\u0442: \u043E\u0441\u0442\u0430\u0442\u043E\u043A/\u0432\u0441\u0435\u0433\u043E  \u00B7  \u0420 \u2014 \u0440\u0435\u0437\u0435\u0440\u0432 \u043A\u0430\u0437\u043D\u044B", "Limit: remaining/total  \u00B7  R is the treasury reserve", "Limit: kalan/toplam  \u00B7  R, hazine rezervidir");

            if (hiddenPairCount > 0)
            {
                legend += DynastyEconomyLoc.T(
                    $"  \u00B7  \u0415\u0449\u0451 \u043F\u0430\u0440: {hiddenPairCount}",
                    $"  \u00B7  More pairs: {hiddenPairCount}",
                    $"  \u00B7  Di\u011Fer pariteler: {hiddenPairCount}");
            }

            SetLabel(footerText, legend);
        }

        private static string HeaderText(int column)
        {
            switch (column)
            {
                case 0: return DynastyEconomyLoc.T("\u041F\u0430\u0440\u0430", "Pair", "Parite");
                case 1: return DynastyEconomyLoc.T("\u041A\u0443\u0440\u0441", "Rate", "Kur");
                case 2: return DynastyEconomyLoc.T("\u0414\u0432\u0438\u0436.", "Move", "De\u011F.");
                case 3: return DynastyEconomyLoc.T("\u0412\u0432\u043E\u0434", "In", "Giri\u015F");
                case 4: return DynastyEconomyLoc.T("\u0412\u044B\u0432\u043E\u0434", "Out", "\u00C7\u0131k\u0131\u015F");
                default: return DynastyEconomyLoc.T("\u041B\u0438\u043C\u0438\u0442", "Limit", "Limit");
            }
        }

        private static string CurrencyName(ExchangeMarketConfig config, string currencyId)
        {
            string normalizedId = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (normalizedId == CurrencyIds.OzAltin)
                return "OzAlt\u0131n";
            if (normalizedId == CurrencyIds.OzTile)
                return "OzTile";
            if (normalizedId == CurrencyIds.OzAmetist)
                return "OzAmetist";

            if (config != null && config.Currencies != null)
            {
                for (int i = 0; i < config.Currencies.Count; i++)
                {
                    ExchangeCurrencyDefinition currency = config.Currencies[i];
                    if (currency != null && CurrencyWalletEntry.NormalizeCurrencyId(currency.CurrencyId) == normalizedId && !string.IsNullOrWhiteSpace(currency.DisplayName))
                        return currency.DisplayName.Trim();
                }
            }

            return string.IsNullOrWhiteSpace(currencyId) ? "\u2014" : currencyId.Trim();
        }

        private static string FormatMove(float move)
        {
            if (Mathf.Abs(move) < 0.01f)
                return "0%";

            return (move > 0f ? "+" : string.Empty) + move.ToString("0.##") + "%";
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private static void ApplyLargeText(TextMeshProUGUI text, float maxSize, float minSize)
        {
            if (text == null)
                return;

            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private static void ApplyCellText(TextMeshProUGUI text, int row, int column, float rowHeight)
        {
            if (text == null)
                return;

            float max = Mathf.Clamp(rowHeight * (row == 0 ? 0.42f : 0.39f), 23f, row == 0 ? 42f : 38f);
            float min = Mathf.Max(17f, max * 0.68f);
            if (column == 5)
            {
                max = Mathf.Min(max, row == 0 ? 36f : 32f);
                min = Mathf.Max(16f, max * 0.68f);
            }
            else if (column == 0)
            {
                max = Mathf.Min(max, row == 0 ? 38f : 34f);
                min = Mathf.Max(17f, max * 0.68f);
            }

            ApplyLargeText(text, max, min);
        }

        private static TextAlignmentOptions ResolveCellAlignment(int column)
        {
            if (column == 0)
                return TextAlignmentOptions.MidlineLeft;
            if (column == ColumnCount - 1)
                return TextAlignmentOptions.MidlineRight;
            return TextAlignmentOptions.Midline;
        }

        private static Vector4 ResolveCellMargin(int column)
        {
            if (column == 0)
                return new Vector4(20f, 0f, 8f, 0f);
            if (column == ColumnCount - 1)
                return new Vector4(8f, 0f, 20f, 0f);
            return new Vector4(8f, 0f, 8f, 0f);
        }

        private static Color MoveColor(float move)
        {
            if (move > 0.01f)
                return new Color(0.44f, 0.94f, 0.67f, 1f);
            if (move < -0.01f)
                return new Color(1f, 0.48f, 0.45f, 1f);
            return new Color(0.76f, 0.88f, 0.96f, 1f);
        }

        private void ResolveRootMetrics(out float rootWidth, out float rootHeight, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom)
        {
            RectTransform rootRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float measuredWidth = rootRect != null ? rootRect.rect.width : 0f;
            float measuredHeight = rootRect != null ? rootRect.rect.height : 0f;
            Canvas root = overlayRect != null ? overlayRect.GetComponentInParent<Canvas>()?.rootCanvas : rootCanvas != null ? rootCanvas.rootCanvas : null;
            RectTransform canvasRect = root != null ? root.transform as RectTransform : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            float scaleFactor = root != null ? Mathf.Max(0.01f, root.scaleFactor) : 1f;
            float screenWidth = Screen.width > 0 ? Screen.width / scaleFactor : 0f;
            float screenHeight = Screen.height > 0 ? Screen.height / scaleFactor : 0f;

            rootWidth = Mathf.Max(measuredWidth, canvasWidth, screenWidth);
            rootHeight = Mathf.Max(measuredHeight, canvasHeight, screenHeight);
            if (rootWidth <= 8f)
                rootWidth = 1920f;
            if (rootHeight <= 8f)
                rootHeight = 1080f;

            Rect safeArea = Screen.safeArea;
            bool hasSafeArea = Screen.width > 0 && Screen.height > 0 && safeArea.width > 0f && safeArea.height > 0f;
            safeLeft = hasSafeArea ? safeArea.xMin / scaleFactor : 0f;
            safeRight = hasSafeArea ? Mathf.Max(0f, Screen.width - safeArea.xMax) / scaleFactor : 0f;
            safeTop = hasSafeArea ? Mathf.Max(0f, Screen.height - safeArea.yMax) / scaleFactor : 0f;
            safeBottom = hasSafeArea ? safeArea.yMin / scaleFactor : 0f;
            safeLeft = Mathf.Clamp(safeLeft, 0f, rootWidth * 0.25f);
            safeRight = Mathf.Clamp(safeRight, 0f, rootWidth * 0.25f);
            safeTop = Mathf.Clamp(safeTop, 0f, rootHeight * 0.20f);
            safeBottom = Mathf.Clamp(safeBottom, 0f, rootHeight * 0.20f);
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

        private void DarkenBackdrop()
        {
            Image overlayImage = overlayRect != null ? overlayRect.GetComponent<Image>() : null;
            if (overlayImage != null)
                overlayImage.color = new Color(0f, 0f, 0f, 0.52f);

            Image windowImage = windowRect != null ? windowRect.GetComponent<Image>() : null;
            if (windowImage != null)
                windowImage.color = Color.white;
        }

        private void ApplyContentPanelBackdrop()
        {
            Image image = contentPanelRect != null ? contentPanelRect.GetComponent<Image>() : null;
            if (image == null)
                return;

            ApplyRoundedSprite(image, new Color(0.012f, 0.035f, 0.064f, 0.82f));
            ApplySoftOutline(contentPanelRect, new Color(0.16f, 0.48f, 0.74f, 0.18f));
            image.raycastTarget = false;
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

        private static void ApplySoftOutline(RectTransform rect, Color color)
        {
            if (rect == null)
                return;

            Outline outline = rect.GetComponent<Outline>();
            if (outline == null)
                outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }

        private static Sprite GetRoundedRectSprite()
        {
            if (cachedRoundedRectSprite != null)
                return cachedRoundedRectSprite;

            const int size = 64;
            const float radius = 15f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ExchangeMonitorRoundedRect";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
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
            cachedRoundedRectSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(18f, 18f, 18f, 18f));
            return cachedRoundedRectSprite;
        }
    }
}
