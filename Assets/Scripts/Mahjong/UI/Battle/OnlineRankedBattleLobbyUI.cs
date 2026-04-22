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

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private TMP_Text statusText;
        private TMP_Text hintText;
        private Button cancelButton;

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
            BindNetwork();
        }

        private void OnEnable()
        {
            BindNetwork();
        }

        private void OnDestroy()
        {
            UnbindNetwork();
        }

        public void Open()
        {
            BuildUi();
            BindNetwork();
            root.SetActive(true);
            root.transform.SetAsLastSibling();

            RefreshStatus("Waiting for another player...");
            OnlineRankedBattleNetwork.EnsureInstance().StartRankedSearch();
        }

        private void Close()
        {
            OnlineRankedBattleNetwork.EnsureInstance().CancelRankedSearch();

            if (root != null)
                root.SetActive(false);
        }

        private void BindNetwork()
        {
            OnlineRankedBattleNetwork network = OnlineRankedBattleNetwork.EnsureInstance();
            network.StatusChanged -= RefreshStatus;
            network.ErrorChanged -= RefreshError;
            network.MatchFound -= HandleMatchFound;
            network.StatusChanged += RefreshStatus;
            network.ErrorChanged += RefreshError;
            network.MatchFound += HandleMatchFound;
        }

        private void UnbindNetwork()
        {
            if (OnlineRankedBattleNetwork.I == null)
                return;

            OnlineRankedBattleNetwork.I.StatusChanged -= RefreshStatus;
            OnlineRankedBattleNetwork.I.ErrorChanged -= RefreshError;
            OnlineRankedBattleNetwork.I.MatchFound -= HandleMatchFound;
        }

        private void RefreshStatus(string value)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(value) ? "Waiting for another player..." : value;
        }

        private void RefreshError(string value)
        {
            if (hintText != null)
                hintText.text = string.IsNullOrWhiteSpace(value)
                    ? "The window will close when a ranked opponent is found."
                    : value;
        }

        private void HandleMatchFound(OnlineRankedBattleNetwork.RankedMatchInfo match)
        {
            if (match == null || match.opponent == null)
                return;

            MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
            {
                Id = string.IsNullOrWhiteSpace(match.opponent.id) ? "ranked_online_peer" : match.opponent.id,
                DisplayName = string.IsNullOrWhiteSpace(match.opponent.displayName) ? "Ranked Player" : match.opponent.displayName,
                RankTier = string.IsNullOrWhiteSpace(match.opponent.rankTier) ? "Unranked" : match.opponent.rankTier,
                RankPoints = Mathf.Max(0, match.opponent.rankPoints),
                AvatarId = Mathf.Max(0, match.opponent.avatarId),
                IsBot = false
            };

            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            MahjongSession.StartBattle(opponent, 0, Mathf.Max(1, match.seed));

            UnbindNetwork();

            if (root != null)
                Destroy(root);

            Destroy(gameObject);
            SceneManager.LoadScene(battleGameSceneName);
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
            panelRect.sizeDelta = new Vector2(660f, 420f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.05f, 0.06f, 0.075f, 0.97f);
            panelImage.raycastTarget = true;

            CreateText(panel.transform, "Title", "Ranked Match", new Vector2(0f, 130f), new Vector2(580f, 64f), 44f);
            statusText = CreateText(panel.transform, "Status", "Waiting for another player...", new Vector2(0f, 54f), new Vector2(560f, 58f), 28f);
            hintText = CreateText(panel.transform, "Hint", "The window will close when a ranked opponent is found.", new Vector2(0f, -12f), new Vector2(560f, 74f), 20f);
            cancelButton = CreateButton(panel.transform, "CancelButton", "Cancel", new Vector2(0f, -146f), new Vector2(260f, 58f));
            cancelButton.onClick.AddListener(Close);
            root.SetActive(false);
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
            text.fontSizeMin = 14f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
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
            image.color = new Color(0.16f, 0.24f, 0.28f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(28f, 12f), 24f);
            return button;
        }
    }
}
