using System.Collections;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class OnlineRankedBattleLobbyUI : MonoBehaviour
    {
        private const string RootName = "OnlineRankedBattleLobbyOverlay";

        [SerializeField, Min(1f)] private float maxOnlineSearchSeconds = 30f;
        [SerializeField, Min(1f)] private float botFallbackSeconds = 10f;
        [SerializeField, Min(0.1f)] private float foundRevealSeconds = 1.4f;

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private TMP_Text titleText;
        private TMP_Text statusText;
        private TMP_Text playerNameText;
        private TMP_Text playerInfoText;
        private TMP_Text opponentNameText;
        private TMP_Text opponentInfoText;
        private Button cancelButton;
        private Coroutine searchRoutine;
        private bool waitingForRankedMatch;
        private bool launching;

        public static OnlineRankedBattleLobbyUI Show(string battleSceneName)
        {
            OnlineRankedBattleLobbyUI existing = FindAnyObjectByType<OnlineRankedBattleLobbyUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                existing.Open();
                return existing;
            }

            GameObject host = new GameObject("OnlineRankedBattleLobbyUI");
            OnlineRankedBattleLobbyUI ui = host.AddComponent<OnlineRankedBattleLobbyUI>();
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

        private void OnDestroy()
        {
            UnbindNetwork();

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            BuildUi();
            BindNetwork();

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            ApplyPlayerProfile();
            ApplyOpponentPlaceholder();
            StartRankedSearch();
        }

        private void Close()
        {
            waitingForRankedMatch = false;

            if (searchRoutine != null)
            {
                StopCoroutine(searchRoutine);
                searchRoutine = null;
            }

            OnlineRankedBattleNetwork.I?.CancelRankedSearch();

            if (root != null)
                root.SetActive(false);
        }

        private void StartRankedSearch()
        {
            if (searchRoutine != null)
                StopCoroutine(searchRoutine);

            waitingForRankedMatch = true;
            launching = false;

            if (titleText != null)
                titleText.text = "Ranked Match";
            if (statusText != null)
                statusText.text = "Searching ranked opponent...";
            if (cancelButton != null)
                cancelButton.interactable = true;

            ApplyOpponentPlaceholder();

            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            OnlineRankedBattleNetwork.EnsureInstance().StartRankedSearch();
            searchRoutine = StartCoroutine(RankedSearchFallbackRoutine());
        }

        private IEnumerator RankedSearchFallbackRoutine()
        {
            float elapsed = 0f;
            float fallbackAt = Mathf.Clamp(botFallbackSeconds, 1f, Mathf.Max(1f, maxOnlineSearchSeconds));
            float maxSearch = Mathf.Max(fallbackAt, maxOnlineSearchSeconds);

            while (elapsed < maxSearch && waitingForRankedMatch && !launching)
            {
                float fallbackRemaining = Mathf.Max(0f, fallbackAt - elapsed);
                float maxRemaining = Mathf.Max(0f, maxSearch - elapsed);

                if (statusText != null)
                {
                    if (elapsed < fallbackAt)
                        statusText.text = $"Searching ranked opponent... {Mathf.CeilToInt(fallbackRemaining)}";
                    else
                        statusText.text = $"Extending search... {Mathf.CeilToInt(maxRemaining)}";
                }

                yield return null;
                elapsed += Time.unscaledDeltaTime;

                if (elapsed >= fallbackAt && waitingForRankedMatch && !launching)
                    break;
            }

            searchRoutine = null;

            if (!waitingForRankedMatch || launching)
                yield break;

            OnlineRankedBattleNetwork.I?.CancelRankedSearch();
            MahjongBattleOpponentData opponent = CreateRankedBotOpponent();
            yield return LaunchFoundMatch(opponent, 0);
        }

        private void HandleMatchFound(OnlineRankedBattleNetwork.RankedMatchInfo match)
        {
            if (!waitingForRankedMatch || launching)
                return;

            if (match == null || match.opponent == null)
                return;

            if (searchRoutine != null)
            {
                StopCoroutine(searchRoutine);
                searchRoutine = null;
            }

            MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
            {
                Id = string.IsNullOrWhiteSpace(match.opponent.id) ? "ranked_online_peer" : match.opponent.id,
                DisplayName = string.IsNullOrWhiteSpace(match.opponent.displayName) ? "Ranked Player" : match.opponent.displayName,
                RankTier = string.IsNullOrWhiteSpace(match.opponent.rankTier) ? "Unranked" : match.opponent.rankTier,
                RankPoints = Mathf.Max(0, match.opponent.rankPoints),
                Level = Mathf.Max(1, 1 + Mathf.Max(0, match.opponent.rankPoints) / 100),
                AvatarId = Mathf.Max(0, match.opponent.avatarId),
                IsBot = false
            };

            StartCoroutine(LaunchFoundMatch(opponent, Mathf.Max(1, match.seed)));
        }

        private IEnumerator LaunchFoundMatch(MahjongBattleOpponentData opponent, int matchSeed)
        {
            waitingForRankedMatch = false;
            launching = true;

            if (cancelButton != null)
                cancelButton.interactable = false;

            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            MahjongSession.StartBattle(opponent, 0, matchSeed);

            ApplyOpponentProfile(opponent);

            if (titleText != null)
                titleText.text = "Player Found";
            if (statusText != null)
                statusText.text = "Starting match...";

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, foundRevealSeconds));

            UnbindNetwork();

            if (root != null)
                root.SetActive(false);

            SceneManager.LoadScene(battleGameSceneName);
        }

        private MahjongBattleOpponentData CreateRankedBotOpponent()
        {
            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            return botService.CreateOpponent(MahjongBattleLobbyMode.RankedMatch, ResolvePlayerBattleRankPoints());
        }

        private int ResolvePlayerBattleRankPoints()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Mahjong != null && profile.Mahjong.Battle != null
                ? Mathf.Max(0, profile.Mahjong.Battle.RankPoints)
                : 0;
        }

        private void BindNetwork()
        {
            OnlineRankedBattleNetwork network = OnlineRankedBattleNetwork.EnsureInstance();
            network.MatchFound -= HandleMatchFound;
            network.MatchFound += HandleMatchFound;
        }

        private void UnbindNetwork()
        {
            if (OnlineRankedBattleNetwork.I == null)
                return;

            OnlineRankedBattleNetwork.I.MatchFound -= HandleMatchFound;
        }

        private void ApplyPlayerProfile()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string displayName = "Player";
            string rankTier = "Unranked";
            int rankPoints = 0;
            int level = 1;
            int wins = 0;
            int losses = 0;

            if (profile != null)
            {
                profile.EnsureData();

                if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                    displayName = profile.DisplayName.Trim();

                level = Mathf.Max(1, profile.AccountLevel);

                MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
                if (battle != null)
                {
                    rankTier = string.IsNullOrWhiteSpace(battle.RankTier) ? "Unranked" : battle.RankTier;
                    rankPoints = Mathf.Max(0, battle.RankPoints);
                    wins = Mathf.Max(0, battle.Wins);
                    losses = Mathf.Max(0, battle.Losses);
                }
            }

            if (playerNameText != null)
                playerNameText.text = displayName;
            if (playerInfoText != null)
                playerInfoText.text = $"Level {level}\n{rankTier} {rankPoints} RP\nWins {wins}  Losses {losses}";
        }

        private void ApplyOpponentPlaceholder()
        {
            if (opponentNameText != null)
                opponentNameText.text = "Searching...";
            if (opponentInfoText != null)
                opponentInfoText.text = "Ranked opponent slot";
        }

        private void ApplyOpponentProfile(MahjongBattleOpponentData opponent)
        {
            if (opponent == null)
                return;

            if (opponentNameText != null)
                opponentNameText.text = string.IsNullOrWhiteSpace(opponent.DisplayName) ? "Opponent" : opponent.DisplayName;

            if (opponentInfoText != null)
                opponentInfoText.text =
                    $"{(string.IsNullOrWhiteSpace(opponent.RankTier) ? "Unranked" : opponent.RankTier)} {Mathf.Max(0, opponent.RankPoints)} RP\n" +
                    $"Wins {Mathf.Max(0, opponent.Wins)}  Losses {Mathf.Max(0, opponent.Losses)}";
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                canvas = CreateCanvas();

            root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            GameObject panel = new GameObject("OnlineRankedBattlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 520f);

            Image panelImage = panel.GetComponent<Image>();
            BattlePopupStyle.ApplyWindow(panelImage);

            titleText = CreateText(panel.transform, "Title", "Ranked Match", new Vector2(0f, 188f), new Vector2(660f, 60f), 42f);
            statusText = CreateText(panel.transform, "Status", "Searching ranked opponent...", new Vector2(0f, 130f), new Vector2(650f, 46f), 24f);

            Transform playerCard = CreateProfileCard(panel.transform, "PlayerCard", new Vector2(-190f, -28f));
            playerNameText = CreateText(playerCard, "Name", "Player", new Vector2(0f, 64f), new Vector2(260f, 44f), 28f);
            playerInfoText = CreateText(playerCard, "Info", string.Empty, new Vector2(0f, -24f), new Vector2(260f, 116f), 20f);

            Transform opponentCard = CreateProfileCard(panel.transform, "OpponentCard", new Vector2(190f, -28f));
            opponentNameText = CreateText(opponentCard, "Name", "Searching...", new Vector2(0f, 64f), new Vector2(260f, 44f), 28f);
            opponentInfoText = CreateText(opponentCard, "Info", "Ranked opponent slot", new Vector2(0f, -24f), new Vector2(260f, 116f), 20f);

            CreateText(panel.transform, "Versus", "VS", new Vector2(0f, -10f), new Vector2(80f, 58f), 32f);

            cancelButton = CreateButton(panel.transform, "CancelButton", "Cancel", new Vector2(0f, -205f), new Vector2(240f, 58f));
            cancelButton.onClick.AddListener(Close);

            root.SetActive(false);
        }

        private Transform CreateProfileCard(Transform parent, string objectName, Vector2 position)
        {
            GameObject card = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 220f);

            Image image = card.GetComponent<Image>();
            BattlePopupStyle.ApplyFront(image);

            return card.transform;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("OnlineRankedCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
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
            text.fontSizeMin = 13f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            BattlePopupStyle.ApplyText(text);
            return text;
        }

        private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
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
            image.color = new Color(0.16f, 0.23f, 0.22f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(28f, 12f), 24f);
            BattlePopupStyle.ApplyButton(button);
            return button;
        }
    }
}
