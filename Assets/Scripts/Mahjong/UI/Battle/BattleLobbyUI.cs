using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleLobbyUI : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string battleGameSceneName = "GameMahjongBattle";
        [SerializeField] private string mainLobbySceneName = "LobbyMahjong";
        [SerializeField] private string friendLobbySceneName = "";
        [SerializeField] private string localWifiSceneName = "";

        [Header("Return Button")]
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private bool autoCreateReturnButton = true;
        [SerializeField] private string returnButtonText = "Lobby";
        [SerializeField] private Vector2 returnButtonPosition = new Vector2(42f, -42f);
        [SerializeField] private Vector2 returnButtonSize = new Vector2(220f, 72f);

        [Header("Character Selection")]
        [SerializeField] private GameObject characterCarouselRoot;
        [SerializeField] private Button openCharacterCarouselButton;
        [SerializeField] private string characterCarouselObjectName = "CharasterCarousel";
        [SerializeField] private string openCharacterButtonObjectName = "LobbyCharacterImage";
        [SerializeField] private bool closeCharacterCarouselOnEnter = true;
        [SerializeField] private bool autoBindOpenCharacterButton = true;
        [SerializeField] private bool createOpenCharacterButtonIfMissing = true;
        [SerializeField] private string openCharacterButtonText = "Character";
        [SerializeField] private bool openCharacterCarouselWhenNoCharacterSelected = true;
        [SerializeField] private int autoOpenCharacterCarouselDelayFrames = 3;
        [SerializeField] private float autoOpenCharacterCarouselMaxWaitSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private Coroutine autoOpenCharacterCarouselRoutine;

        private void Awake()
        {
            AutoResolveCharacterSelectionLinks();
            BindCharacterSelectionButton();
            EnsureReturnButton();
            BindReturnButton();

            if (closeCharacterCarouselOnEnter)
                CloseCharacterCarousel();
        }

        private void OnEnable()
        {
            if (closeCharacterCarouselOnEnter)
                CloseCharacterCarousel();

            QueueAutoOpenCharacterCarouselIfNeeded();
        }

        private void OnDisable()
        {
            if (autoOpenCharacterCarouselRoutine != null)
            {
                StopCoroutine(autoOpenCharacterCarouselRoutine);
                autoOpenCharacterCarouselRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (openCharacterCarouselButton != null)
                openCharacterCarouselButton.onClick.RemoveListener(OnClickOpenCharacterCarousel);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);
        }

        public void OnClickRandomMatch()
        {
            OpenBattleMode(MahjongBattleLobbyMode.RandomMatch);
        }

        public void OnClickStart()
        {
            OnClickRandomMatch();
        }

        public void OnClickOpenCharacterCarousel()
        {
            OpenCharacterCarousel();
        }

        public void OnClickReturnToLobby()
        {
            if (string.IsNullOrWhiteSpace(mainLobbySceneName))
            {
                Log("mainLobbySceneName is empty.");
                return;
            }

            LoadScene(mainLobbySceneName);
        }

        public void OpenCharacterCarousel()
        {
            AutoResolveCharacterSelectionLinks();

            if (characterCarouselRoot != null)
            {
                characterCarouselRoot.SetActive(true);

                BattleCharacterCircularCarousel carousel =
                    characterCarouselRoot.GetComponentInChildren<BattleCharacterCircularCarousel>(true);
                if (carousel != null)
                    carousel.RefreshButtons();
            }
        }

        private void QueueAutoOpenCharacterCarouselIfNeeded()
        {
            if (!openCharacterCarouselWhenNoCharacterSelected)
                return;

            if (autoOpenCharacterCarouselRoutine != null)
                StopCoroutine(autoOpenCharacterCarouselRoutine);

            autoOpenCharacterCarouselRoutine = StartCoroutine(AutoOpenCharacterCarouselIfNoSelectionRoutine());
        }

        private IEnumerator AutoOpenCharacterCarouselIfNoSelectionRoutine()
        {
            int delayFrames = Mathf.Max(0, autoOpenCharacterCarouselDelayFrames);
            for (int i = 0; i < delayFrames; i++)
                yield return null;

            float deadline = Time.unscaledTime + Mathf.Max(0f, autoOpenCharacterCarouselMaxWaitSeconds);
            while (!BattleCharacterSelectionService.HasInstance && Time.unscaledTime < deadline)
                yield return null;

            autoOpenCharacterCarouselRoutine = null;

            if (HasSelectedBattleCharacter())
                yield break;

            OpenCharacterCarousel();
            Log("Auto-opened character carousel because no battle character is selected.");
        }

        private static bool HasSelectedBattleCharacter()
        {
            return BattleCharacterSelectionService.HasInstance
                && BattleCharacterSelectionService.Instance.HasSelectedCharacter();
        }

        public void CloseCharacterCarousel()
        {
            AutoResolveCharacterSelectionLinks();

            if (characterCarouselRoot != null)
                characterCarouselRoot.SetActive(false);
        }

        public void OnClickRankedMatch()
        {
            OpenBattleMode(MahjongBattleLobbyMode.RankedMatch);
        }

        public void OnClickFriendMatch()
        {
            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.FriendMatch);

            if (!string.IsNullOrWhiteSpace(friendLobbySceneName))
            {
                LoadScene(friendLobbySceneName);
                return;
            }

            Log("FriendMatch selected, but friendLobbySceneName is empty.");
        }

        public void OnClickLocalWifiMatch()
        {
            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.LocalWifiMatch);

            if (!string.IsNullOrWhiteSpace(localWifiSceneName))
            {
                LoadScene(localWifiSceneName);
                return;
            }

            Log("LocalWifiMatch selected, but localWifiSceneName is empty.");
        }

        private void OpenBattleMode(MahjongBattleLobbyMode mode)
        {
            MahjongBattleLobbySession.SetMode(mode);
            PrepareBotOpponent(mode);

            if (string.IsNullOrWhiteSpace(battleGameSceneName))
            {
                Log("battleGameSceneName is empty.");
                return;
            }

            LoadScene(battleGameSceneName);
        }

        private void LoadScene(string sceneName)
        {
            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);

            Log($"LoadScene -> {sceneName} | Mode={MahjongBattleLobbySession.SelectedMode}");
        }

        private void PrepareBotOpponent(MahjongBattleLobbyMode mode)
        {
            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            int playerRankPoints = ResolvePlayerBattleRankPoints();
            MahjongBattleOpponentData opponent = botService.CreateOpponent(mode, playerRankPoints);
            MahjongSession.StartBattle(opponent);

            Log(
                $"Bot opponent prepared | " +
                $"Name={opponent.DisplayName} | " +
                $"Rank={opponent.RankTier} {opponent.RankPoints} | " +
                $"W/L={opponent.Wins}/{opponent.Losses}");
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

        private void AutoResolveCharacterSelectionLinks()
        {
            if (characterCarouselRoot == null && !string.IsNullOrWhiteSpace(characterCarouselObjectName))
            {
                GameObject found = FindObjectByName(characterCarouselObjectName);
                if (found != null)
                    characterCarouselRoot = found;
            }

            if (!autoBindOpenCharacterButton || openCharacterCarouselButton != null)
                return;

            GameObject buttonObject = !string.IsNullOrWhiteSpace(openCharacterButtonObjectName)
                ? FindObjectByName(openCharacterButtonObjectName)
                : null;

            if (buttonObject == null)
            {
                if (createOpenCharacterButtonIfMissing)
                    openCharacterCarouselButton = CreateOpenCharacterButton();

                return;
            }

            openCharacterCarouselButton = buttonObject.GetComponent<Button>();
            if (openCharacterCarouselButton == null)
                openCharacterCarouselButton = buttonObject.AddComponent<Button>();

            Image image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                openCharacterCarouselButton.targetGraphic = image;
            }
        }

        private static GameObject FindObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            GameObject active = GameObject.Find(objectName);
            if (active != null)
                return active;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform item = transforms[i];
                if (item != null && string.Equals(item.name, objectName, System.StringComparison.Ordinal))
                    return item.gameObject;
            }

            return null;
        }

        private Button CreateOpenCharacterButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "OpenCharacterCarouselButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(32f, 42f);
            rect.sizeDelta = new Vector2(220f, 64f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.09f, 0.1f, 0.12f, 0.88f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = openCharacterButtonText;
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private void EnsureReturnButton()
        {
            if (returnToLobbyButton != null || !autoCreateReturnButton)
                return;

            returnToLobbyButton = CreateReturnToLobbyButton();
        }

        private Button CreateReturnToLobbyButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "ButtonReturnToLobby",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = returnButtonPosition;
            rect.sizeDelta = returnButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.13f, 0.9f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = returnButtonText;
            label.fontSize = 28f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 34f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private void BindCharacterSelectionButton()
        {
            if (!autoBindOpenCharacterButton)
                return;

            AutoResolveCharacterSelectionLinks();

            if (openCharacterCarouselButton == null)
                return;

            openCharacterCarouselButton.onClick.RemoveListener(OnClickOpenCharacterCarousel);
            openCharacterCarouselButton.onClick.AddListener(OnClickOpenCharacterCarousel);
        }

        private void BindReturnButton()
        {
            if (returnToLobbyButton == null)
                return;

            returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);
            returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleLobbyUI] {message}", this);
        }
    }
}
