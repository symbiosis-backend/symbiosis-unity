using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class RankedLeagueSelectUI : MonoBehaviour
    {
        private const string RootName = "RankedLeagueSelectOverlay";
        private const string OzTileIconResourcePath = "Mahjong/Sprites/BattleTiles/OzTile";
        private const string WindowSpritePath = "Mahjong/Sprites/BattleLobbyUI/TournamentWindow";
        private const string ButtonSpritePath = "Mahjong/Sprites/BattleLobbyUI/Battlebutton";
        private const string DividerSpritePath = "Mahjong/Sprites/BattleLobbyUI/Divider";
        private static readonly Vector2 FullscreenPanelSize = new Vector2(2140f, 980f);
        private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private RectTransform panelRect;
        private Image playerRankIcon;
        private TMP_Text playerSummaryText;
        private TMP_Text playerOzTileBalanceText;
        private TMP_Text leaderboardTitleText;
        private TMP_Text leaderboardText;
        private TMP_Text leaderboardRpText;
        private TMP_Text confirmTitleText;
        private TMP_Text confirmBodyText;
        private GameObject confirmRoot;
        private Button confirmButton;
        private RankedLeagueConfig selectedLeague;
        private RankedLeaderboardScope leaderboardScope = RankedLeaderboardScope.Global;
        private RankedLeagueId selectedLeaderboardLeague = RankedLeagueId.Bronze;
        private Coroutine leaderboardRoutine;
        private float nextLeaderboardRefreshAt;
        private const float LeaderboardRefreshSeconds = 6f;
        private readonly List<LeagueCardView> leagueCards = new List<LeagueCardView>();
        private static Sprite cachedOzTileIcon;
        private static Sprite windowSprite;
        private static Sprite buttonSprite;
        private static Sprite dividerSprite;

        private sealed class LeagueCardView
        {
            public RankedLeagueConfig Config;
            public Button Button;
            public Image IconImage;
            public TMP_Text TitleText;
            public TMP_Text StatusText;
            public TMP_Text EntryValueText;
            public TMP_Text WinValueText;
            public TMP_Text ActionText;
        }

        public static RankedLeagueSelectUI Show(string battleSceneName)
        {
            RankedLeagueSelectUI existing = FindAnyObjectByType<RankedLeagueSelectUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                existing.Open();
                return existing;
            }

            GameObject host = new GameObject("RankedLeagueSelectUI");
            RankedLeagueSelectUI ui = host.AddComponent<RankedLeagueSelectUI>();
            ui.Configure(battleSceneName);
            ui.Open();
            return ui;
        }

        private void Configure(string battleSceneName)
        {
            if (!string.IsNullOrWhiteSpace(battleSceneName))
                battleGameSceneName = battleSceneName;
        }

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            CurrencyService.CurrencyChanged += HandleCurrencyChanged;
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= HandleCurrencyChanged;
        }

        private void OnDestroy()
        {
            CurrencyService.CurrencyChanged -= HandleCurrencyChanged;
            if (leaderboardRoutine != null)
            {
                StopCoroutine(leaderboardRoutine);
                leaderboardRoutine = null;
            }
            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RankedMatch);
        }

        public void Open()
        {
            ProfileRuntimeBootstrap.EnsureServices();
            if (RankedBattleService.HasPendingMatch())
            {
                RankedPendingMatch pending = RankedBattleService.GetPendingMatch();
                if (pending != null && pending.MatchStarted)
                    RankedBattleService.ApplyRankedResult(false);
                else
                    RankedBattleService.CancelPendingMatch(refundEntryFee: true);
            }

            BuildUi();
            BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.RankedMatch);

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            if (confirmRoot != null)
                confirmRoot.SetActive(false);

            selectedLeague = RankedBattleService.GetLeague(RankedLeagueId.Bronze);
            selectedLeaderboardLeague = selectedLeague.Id;
            nextLeaderboardRefreshAt = 0f;
            Refresh();
        }

        private void Update()
        {
            if (root == null || !root.activeSelf)
                return;
            if (Time.unscaledTime < nextLeaderboardRefreshAt)
                return;
            if (leaderboardRoutine != null)
                return;

            RefreshPlayerSummary();
            RefreshLeaderboard(showLoading: false);
        }

        private void Close()
        {
            if (leaderboardRoutine != null)
            {
                StopCoroutine(leaderboardRoutine);
                leaderboardRoutine = null;
            }
            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.RankedMatch);
            if (root != null)
                root.SetActive(false);
        }

        private void Refresh()
        {
            RefreshPlayerSummary();
            RefreshLeagueButtons();
            RefreshLeaderboard();
        }

        private void HandleCurrencyChanged()
        {
            if (root == null || !root.activeSelf)
                return;

            RefreshPlayerSummary();
            RefreshLeagueButtons();
        }

        private void RefreshPlayerSummary()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            int ozTile = ResolveDisplayOzTileBalance(profile);
            int rankPoints = 0;
            string tier = GameLocalization.Text("battle.rank.bronze");

            if (profile != null)
            {
                profile.EnsureData();

                MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
                if (battle != null)
                {
                    rankPoints = Mathf.Max(0, battle.RankPoints);
                    tier = string.IsNullOrWhiteSpace(battle.RankTier)
                        ? LocalizeLeagueName(RankedLeagueVisuals.ResolveLeagueId(string.Empty, rankPoints))
                        : LocalizeRankTier(battle.RankTier);
                }
            }

            if (playerRankIcon != null)
            {
                RankedLeagueId leagueId = RankedLeagueVisuals.ResolveLeagueId(tier, rankPoints);
                playerRankIcon.sprite = RankedLeagueVisuals.LoadLeagueIcon(leagueId);
                playerRankIcon.enabled = playerRankIcon.sprite != null;
            }

            if (playerSummaryText != null)
                playerSummaryText.text = tier;

            if (playerOzTileBalanceText != null)
                playerOzTileBalanceText.text = ozTile.ToString();
        }

        private static int ResolveDisplayOzTileBalance(PlayerProfile profile)
        {
            if (CurrencyService.I != null)
                return Mathf.Max(0, CurrencyService.I.GetOzTile());

            if (profile == null)
                profile = ProfileService.I != null ? ProfileService.I.Current : null;

            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Currencies != null ? Mathf.Max(0, profile.Currencies.OzTile) : 0;
        }

        private void RefreshLeagueButtons()
        {
            for (int i = 0; i < leagueCards.Count; i++)
            {
                LeagueCardView card = leagueCards[i];
                RankedLeagueConfig config = card != null ? card.Config : null;
                if (card == null || config == null)
                    continue;

                string reason;
                bool available = RankedBattleService.CanEnterLeague(config, out reason);
                bool rankLocked = RankedBattleService.GetCurrentRankPoints() < config.MinRankPoints;

                if (card.Button != null)
                    card.Button.interactable = available;

                if (card.TitleText != null)
                    card.TitleText.text = UpperLeagueText(LocalizeLeagueName(config));

                if (card.StatusText != null)
                    card.StatusText.text = available ? GameLocalization.Text("battle.league.open") : ResolveLockedLabel(config, reason);

                if (card.ActionText != null)
                    card.ActionText.text = available ? GameLocalization.Text("battle.league.play") : (rankLocked ? GameLocalization.Text("battle.league.locked") : GameLocalization.Text("battle.league.need"));

                if (card.EntryValueText != null)
                    card.EntryValueText.text = config.EntryFeeOzTile.ToString();

                if (card.WinValueText != null)
                    card.WinValueText.text = config.WinRewardOzTile.ToString();
            }
        }

        private void RefreshLeaderboard(bool showLoading = true)
        {
            if (leaderboardTitleText != null)
            {
                leaderboardTitleText.text = leaderboardScope == RankedLeaderboardScope.Global
                    ? GameLocalization.Text("battle.league.global_leaderboard")
                    : GameLocalization.Format("battle.league.league_leaderboard", LocalizeLeagueName(RankedBattleService.GetLeague(selectedLeaderboardLeague)));
            }

            nextLeaderboardRefreshAt = Time.unscaledTime + LeaderboardRefreshSeconds;

            if (showLoading && leaderboardText != null)
            {
                leaderboardText.text = GameLocalization.Text("common.loading");
                if (leaderboardRpText != null)
                    leaderboardRpText.text = string.Empty;
            }

            if (leaderboardRoutine != null)
                StopCoroutine(leaderboardRoutine);
            leaderboardRoutine = StartCoroutine(RefreshLeaderboardRoutine(leaderboardScope, selectedLeaderboardLeague));
        }

        private IEnumerator RefreshLeaderboardRoutine(RankedLeaderboardScope scope, RankedLeagueId leagueId)
        {
            List<LeaderboardEntry> entries = null;
            string error = string.Empty;
            yield return RankedLeaderboardService.FetchLeaderboard(scope, leagueId, (result, requestError) =>
            {
                entries = result;
                error = requestError;
            });

            if (scope != leaderboardScope || leagueId != selectedLeaderboardLeague)
                yield break;

            RenderLeaderboardEntries(entries, error);
            leaderboardRoutine = null;
        }

        private void RenderLeaderboardEntries(List<LeaderboardEntry> entries, string error)
        {
            System.Text.StringBuilder namesBuilder = new System.Text.StringBuilder();
            System.Text.StringBuilder rpBuilder = new System.Text.StringBuilder();
            int count = entries != null ? Mathf.Min(entries.Count, 10) : 0;
            for (int i = 0; i < count; i++)
            {
                LeaderboardEntry entry = entries[i];
                string marker = entry.IsPlayer ? GameLocalization.Text("battle.league.you") : string.Empty;
                namesBuilder.Append(i + 1)
                    .Append(". ")
                    .Append(entry.DisplayName)
                    .Append(marker)
                    .AppendLine();
                rpBuilder
                    .Append(entry.RankPoints)
                    .Append(" RP")
                    .AppendLine();
            }

            if (count == 0)
            {
                namesBuilder.AppendLine(GameLocalization.Text("battle.league.no_real_leaderboard"));
                if (!string.IsNullOrWhiteSpace(error))
                    namesBuilder.AppendLine(error);
            }

            if (leaderboardText != null)
                leaderboardText.text = namesBuilder.ToString();

            if (leaderboardRpText != null)
                leaderboardRpText.text = rpBuilder.ToString();
        }

        private void SelectLeague(RankedLeagueConfig config)
        {
            selectedLeague = config;
            selectedLeaderboardLeague = config.Id;
            leaderboardScope = RankedLeaderboardScope.League;
            RefreshLeaderboard();

            string reason;
            if (!RankedBattleService.CanEnterLeague(config, out reason))
                return;

            ShowConfirm(config);
        }

        private void ShowConfirm(RankedLeagueConfig config)
        {
            if (confirmRoot == null || config == null)
                return;

            confirmRoot.SetActive(true);
            confirmRoot.transform.SetAsLastSibling();
            if (confirmTitleText != null)
                confirmTitleText.text = GameLocalization.Format("battle.league.confirm_title", LocalizeLeagueName(config));

            if (confirmBodyText != null)
            {
                confirmBodyText.text = GameLocalization.Format(
                    "battle.league.confirm_body",
                    config.EntryFeeOzTile,
                    config.WinRewardOzTile,
                    config.WinRankPoints,
                    config.LossRankPoints);
            }
        }

        private void HideConfirm()
        {
            if (confirmRoot != null)
                confirmRoot.SetActive(false);
        }

        private void ConfirmStart()
        {
            if (selectedLeague == null)
                return;

            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return;

            string reason;
            if (!RankedBattleService.TryStartRankedMatch(selectedLeague, out reason))
            {
                if (confirmBodyText != null)
                    confirmBodyText.text = reason;
                Refresh();
                return;
            }

            HideConfirm();
            Close();
            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            OnlineRankedBattleLobbyUI.Show(battleGameSceneName);
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            Canvas canvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();

            root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.GetComponent<Image>();
            dim.color = Color.black;
            dim.raycastTarget = true;

            GameObject panel = new GameObject("RankedLeaguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            Image panelImage = panel.GetComponent<Image>();
            BattlePopupStyle.ApplyWindow(panelImage);

            TMP_Text titleText = CreateText(panel.transform, "Title", GameLocalization.Text("battle.league.arena"), new Vector2(0f, 402f), new Vector2(1060f, 84f), 68f);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -38f);

            RectTransform topDivider = CreateDivider(panel.transform, "TopDivider", new Vector2(0f, 334f), new Vector2(1840f, 8f));
            topDivider.anchorMin = new Vector2(0.5f, 1f);
            topDivider.anchorMax = new Vector2(0.5f, 1f);
            topDivider.pivot = new Vector2(0.5f, 1f);
            topDivider.anchoredPosition = new Vector2(0f, -116f);

            Button closeButton = CreateButton(panel.transform, "CloseRankedLeagues", GameLocalization.Text("battle.common.close"), new Vector2(970f, 402f), new Vector2(112f, 112f), 30f);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-44f, -36f);
            closeButton.transform.SetAsLastSibling();
            closeButton.onClick.AddListener(Close);
            BuildLeagueCards(panel.transform);
            BuildLeaderboard(panel.transform);
            BuildBalanceBlock(panel.transform);
            BuildConfirm(root.transform);

            root.SetActive(false);
        }

        private void BuildLeagueCards(Transform parent)
        {
            RankedLeagueConfig[] leagues = RankedBattleService.GetLeagues();
            for (int i = 0; i < leagues.Length; i++)
            {
                RankedLeagueConfig config = leagues[i];
                Vector2 position = new Vector2(-545f, 326f - i * 132f);
                LeagueCardView card = CreateLeagueCard(parent, config, position);
                RankedLeagueConfig captured = config;
                card.Button.onClick.AddListener(() => SelectLeague(captured));
                leagueCards.Add(card);
            }
        }

        private LeagueCardView CreateLeagueCard(Transform parent, RankedLeagueConfig config, Vector2 position)
        {
            Button button = CreateButton(parent, "League" + config.Id, string.Empty, position, new Vector2(1060f, 118f), 34f);
            HideButtonLabel(button);

            LeagueCardView card = new LeagueCardView
            {
                Config = config,
                Button = button,
                IconImage = CreateLeagueIcon(button.transform, "LeagueIcon", config.Id, new Vector2(-470f, 0f), new Vector2(96f, 96f)),
                TitleText = CreateText(button.transform, "LeagueTitle", UpperLeagueText(LocalizeLeagueName(config)), new Vector2(-285f, 20f), new Vector2(270f, 42f), 35f),
                StatusText = CreateText(button.transform, "LeagueStatus", string.Empty, new Vector2(-285f, -25f), new Vector2(290f, 32f), 24f),
                EntryValueText = CreateText(button.transform, "EntryValue", config.EntryFeeOzTile.ToString(), new Vector2(-32f, -16f), new Vector2(110f, 42f), 38f),
                WinValueText = CreateText(button.transform, "WinValue", config.WinRewardOzTile.ToString(), new Vector2(222f, -16f), new Vector2(130f, 42f), 38f),
                ActionText = CreateText(button.transform, "LeagueAction", GameLocalization.Text("battle.league.play"), new Vector2(395f, 0f), new Vector2(220f, 66f), 42f)
            };

            card.TitleText.alignment = TextAlignmentOptions.MidlineLeft;
            card.StatusText.alignment = TextAlignmentOptions.MidlineLeft;
            card.EntryValueText.alignment = TextAlignmentOptions.MidlineLeft;
            card.WinValueText.alignment = TextAlignmentOptions.MidlineLeft;
            card.ActionText.alignment = TextAlignmentOptions.Center;

            TMP_Text entryCaption = CreateText(button.transform, "EntryCaption", GameLocalization.Text("battle.league.entry"), new Vector2(-64f, 24f), new Vector2(150f, 28f), 23f);
            entryCaption.alignment = TextAlignmentOptions.Center;
            CreateOzTileIcon(button.transform, "EntryIcon", new Vector2(-106f, -16f), new Vector2(34f, 34f));

            TMP_Text winCaption = CreateText(button.transform, "WinCaption", GameLocalization.Text("battle.league.win"), new Vector2(170f, 24f), new Vector2(170f, 28f), 23f);
            winCaption.alignment = TextAlignmentOptions.Center;
            CreateOzTileIcon(button.transform, "WinIcon", new Vector2(136f, -16f), new Vector2(34f, 34f));

            return card;
        }

        private Image CreateLeagueIcon(Transform parent, string objectName, RankedLeagueId leagueId, Vector2 position, Vector2 size)
        {
            GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = iconObject.GetComponent<Image>();
            image.sprite = RankedLeagueVisuals.LoadLeagueIcon(leagueId);
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private void BuildLeaderboard(Transform parent)
        {
            GameObject card = new GameObject("LeaderboardCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(585f, -25f);
            rect.sizeDelta = new Vector2(840f, 820f);
            Image cardImage = card.GetComponent<Image>();
            BattlePopupStyle.ApplyWindow(cardImage, false);

            leaderboardTitleText = CreateText(card.transform, "LeaderboardTitle", GameLocalization.Text("battle.league.global_leaderboard"), new Vector2(0f, 348f), new Vector2(720f, 58f), 44f);
            Button globalButton = CreateButton(card.transform, "GlobalTab", GameLocalization.Text("battle.league.global"), new Vector2(-190f, 280f), new Vector2(320f, 72f), 40f);
            globalButton.onClick.AddListener(() =>
            {
                leaderboardScope = RankedLeaderboardScope.Global;
                RefreshLeaderboard();
            });

            Button leagueButton = CreateButton(card.transform, "LeagueTab", GameLocalization.Text("battle.league.league"), new Vector2(190f, 280f), new Vector2(320f, 72f), 40f);
            leagueButton.onClick.AddListener(() =>
            {
                leaderboardScope = RankedLeaderboardScope.League;
                RefreshLeaderboard();
            });

            leaderboardText = CreateText(card.transform, "LeaderboardNames", string.Empty, new Vector2(-150f, -95f), new Vector2(430f, 680f), 52f);
            leaderboardText.alignment = TextAlignmentOptions.TopLeft;
            leaderboardRpText = CreateText(card.transform, "LeaderboardRp", string.Empty, new Vector2(250f, -95f), new Vector2(250f, 680f), 52f);
            leaderboardRpText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void BuildBalanceBlock(Transform parent)
        {
            GameObject card = new GameObject("RankedBalanceCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-545f, -390f);
            rect.sizeDelta = new Vector2(760f, 124f);

            BattlePopupStyle.ApplyWindow(card.GetComponent<Image>(), false);

            playerRankIcon = CreateLeagueIcon(card.transform, "PlayerRankIcon", RankedLeagueId.Bronze, new Vector2(-285f, 0f), new Vector2(86f, 86f));
            playerSummaryText = CreateText(card.transform, "PlayerSummary", string.Empty, new Vector2(-130f, 0f), new Vector2(200f, 72f), 50f);
            playerSummaryText.alignment = TextAlignmentOptions.MidlineLeft;

            CreateOzTileIcon(card.transform, "BalanceOzTileIcon", new Vector2(82f, 0f), new Vector2(54f, 54f));
            playerOzTileBalanceText = CreateText(card.transform, "PlayerOzTileBalance", "0", new Vector2(240f, 0f), new Vector2(230f, 60f), 50f);
            playerOzTileBalanceText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void BuildConfirm(Transform parent)
        {
            confirmRoot = new GameObject("RankedConfirmOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            confirmRoot.transform.SetParent(parent, false);

            RectTransform rootRect = confirmRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = confirmRoot.GetComponent<Image>();
            dim.color = Color.black;
            dim.raycastTarget = true;

            GameObject card = new GameObject("RankedConfirmCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(confirmRoot.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(1100f, 640f);
            BattlePopupStyle.ApplyFront(card.GetComponent<Image>());

            confirmTitleText = CreateText(card.transform, "ConfirmTitle", GameLocalization.Text("battle.league.league"), new Vector2(0f, 220f), new Vector2(820f, 74f), 58f);
            confirmBodyText = CreateText(card.transform, "ConfirmBody", string.Empty, new Vector2(0f, 38f), new Vector2(800f, 270f), 44f);
            confirmButton = CreateButton(card.transform, "ConfirmStart", GameLocalization.Text("battle.common.start"), new Vector2(-210f, -230f), new Vector2(360f, 94f), 38f);
            confirmButton.onClick.AddListener(ConfirmStart);

            Button cancel = CreateButton(card.transform, "ConfirmCancel", GameLocalization.Text("battle.common.cancel"), new Vector2(210f, -230f), new Vector2(360f, 94f), 38f);
            cancel.onClick.AddListener(HideConfirm);
            confirmRoot.SetActive(false);
        }

        private static void HideButtonLabel(Button button)
        {
            Transform label = button != null ? button.transform.Find("Label") : null;
            if (label != null)
                label.gameObject.SetActive(false);
        }

        private static RectTransform CreateDivider(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            GameObject dividerObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerObject.transform.SetParent(parent, false);

            RectTransform rect = dividerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            ApplySimpleSprite(dividerObject.GetComponent<Image>(), LoadDividerSprite(), false);
            return rect;
        }

        private static string ResolveLockedLabel(RankedLeagueConfig config, string reason)
        {
            if (config == null)
                return GameLocalization.Text("battle.league.locked");

            int rankPoints = RankedBattleService.GetCurrentRankPoints();
            if (rankPoints < config.MinRankPoints)
                return GameLocalization.Format("battle.league.need_rp", config.MinRankPoints);

            if (!string.IsNullOrWhiteSpace(reason) && reason.Contains("OzTile", System.StringComparison.OrdinalIgnoreCase))
                return $"{GameLocalization.Text("battle.league.need")} {config.EntryFeeOzTile}";

            return string.IsNullOrWhiteSpace(reason) ? GameLocalization.Text("battle.league.locked") : reason.ToUpperInvariant();
        }

        private void AddLeaguePriceRows(Transform parent, RankedLeagueConfig config)
        {
            if (parent == null || config == null)
                return;

            CreateText(parent, "EntryCaption", GameLocalization.Text("battle.league.entry"), new Vector2(-108f, -30f), new Vector2(105f, 34f), 22f);
            CreateOzTileIcon(parent, "EntryIcon", new Vector2(-18f, -30f), new Vector2(30f, 30f));
            TMP_Text entry = CreateText(parent, "EntryValue", config.EntryFeeOzTile.ToString(), new Vector2(55f, -30f), new Vector2(120f, 36f), 28f);
            entry.alignment = TextAlignmentOptions.MidlineLeft;

            CreateText(parent, "WinCaption", GameLocalization.Text("battle.league.win"), new Vector2(-108f, -68f), new Vector2(105f, 34f), 22f);
            CreateOzTileIcon(parent, "WinIcon", new Vector2(-18f, -68f), new Vector2(30f, 30f));
            TMP_Text win = CreateText(parent, "WinValue", config.WinRewardOzTile.ToString(), new Vector2(55f, -68f), new Vector2(120f, 36f), 28f);
            win.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void ConfigureLeagueLabelRect(Button button)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            RectTransform rect = label != null ? label.rectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(420f, 88f);
        }

        private static void SetLeaguePriceText(Button button, string childName, int value)
        {
            if (button == null || string.IsNullOrWhiteSpace(childName))
                return;

            Transform child = button.transform.Find(childName);
            TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text != null)
                text.text = value.ToString();
        }

        private Image CreateOzTileIcon(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = iconObject.GetComponent<Image>();
            image.sprite = LoadOzTileIcon();
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite LoadOzTileIcon()
        {
            if (cachedOzTileIcon != null)
                return cachedOzTileIcon;

            cachedOzTileIcon = Resources.Load<Sprite>(OzTileIconResourcePath);
            if (cachedOzTileIcon != null)
                return cachedOzTileIcon;

            Texture2D texture = Resources.Load<Texture2D>(OzTileIconResourcePath);
            if (texture != null)
            {
                cachedOzTileIcon = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return cachedOzTileIcon;
        }

        private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            BattlePopupStyle.ApplyText(text, true);
            return text;
        }

        private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(26f, 12f), fontSize);
            BattlePopupStyle.ApplyButton(button);
            BattlePopupStyle.ApplyButtonLabel(button, fontSize);
            text.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrWhiteSpace(objectName) && objectName.Contains("Close"))
                BattlePopupStyle.ApplyCloseIconButton(button);
            return button;
        }

        private static void ApplySimpleSprite(Image image, Sprite sprite, bool raycastTarget)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
        }

        private static Sprite LoadWindowSprite()
        {
            if (windowSprite == null)
                windowSprite = LoadBattleLobbySprite(WindowSpritePath);
            return windowSprite;
        }

        private static Sprite LoadButtonSprite()
        {
            if (buttonSprite == null)
                buttonSprite = LoadBattleLobbySprite(ButtonSpritePath);
            return buttonSprite;
        }

        private static Sprite LoadDividerSprite()
        {
            if (dividerSprite == null)
                dividerSprite = LoadBattleLobbySprite(DividerSpritePath);
            return dividerSprite;
        }

        private static Sprite LoadBattleLobbySprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private static RankedLeagueConfig ResolveNextLeague(int rankPoints)
        {
            RankedLeagueConfig[] leagues = RankedBattleService.GetLeagues();
            for (int i = 0; i < leagues.Length; i++)
            {
                if (rankPoints < leagues[i].MinRankPoints)
                    return leagues[i];
            }

            return null;
        }

        private static string LocalizeLeagueName(RankedLeagueConfig config)
        {
            return config == null ? GameLocalization.Text("battle.rank.bronze") : LocalizeLeagueName(config.Id);
        }

        private static string UpperLeagueText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return AppSettings.I != null && AppSettings.I.Language == GameLanguage.Turkish
                ? value.ToUpper(TurkishCulture)
                : value.ToUpperInvariant();
        }

        private static string LocalizeLeagueName(RankedLeagueId id)
        {
            return id switch
            {
                RankedLeagueId.Silver => GameLocalization.Text("battle.rank.silver"),
                RankedLeagueId.Gold => GameLocalization.Text("battle.rank.gold"),
                RankedLeagueId.Platinum => GameLocalization.Text("battle.rank.platinum"),
                RankedLeagueId.Master => GameLocalization.Text("battle.rank.master"),
                _ => GameLocalization.Text("battle.rank.bronze")
            };
        }

        private static string LocalizeRankTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return GameLocalization.Text("battle.rank.unranked");

            string value = tier.Trim().ToLowerInvariant();
            if (value.Contains("master")) return GameLocalization.Text("battle.rank.master");
            if (value.Contains("platinum")) return GameLocalization.Text("battle.rank.platinum");
            if (value.Contains("gold")) return GameLocalization.Text("battle.rank.gold");
            if (value.Contains("silver")) return GameLocalization.Text("battle.rank.silver");
            if (value.Contains("bronze")) return GameLocalization.Text("battle.rank.bronze");
            if (value.Contains("unranked")) return GameLocalization.Text("battle.rank.unranked");
            return tier.Trim();
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
        }
    }
}
