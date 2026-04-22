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
        [SerializeField] private bool randomMatchUsesOnlineRanked = true;

        [Header("Wi-Fi Battle Button")]
        [SerializeField] private Button localWifiBattleButton;
        [SerializeField] private bool autoCreateLocalWifiBattleButton = true;
        [SerializeField] private string localWifiBattleButtonText = "Wi-Fi Battle";
        [SerializeField] private Vector2 localWifiBattleButtonPosition = new Vector2(0f, -420f);
        [SerializeField] private Vector2 localWifiBattleButtonSize = new Vector2(320f, 76f);

        [Header("Ranked Online Button")]
        [SerializeField] private Button rankedBattleButton;
        [SerializeField] private bool autoCreateRankedBattleButton = true;
        [SerializeField] private string rankedBattleButtonText = "Ranked Match";
        [SerializeField] private Vector2 rankedBattleButtonPosition = new Vector2(0f, -324f);
        [SerializeField] private Vector2 rankedBattleButtonSize = new Vector2(320f, 76f);

        [Header("Return Button")]
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private bool autoCreateReturnButton = true;
        [SerializeField] private string returnButtonText = "Lobby";
        [SerializeField] private Vector2 returnButtonPosition = new Vector2(42f, -42f);
        [SerializeField] private Vector2 returnButtonSize = new Vector2(220f, 72f);

        [Header("Battle Progress")]
        [SerializeField] private GameObject battleProgressRoot;
        [SerializeField] private TMP_Text battleLevelText;
        [SerializeField] private TMP_Text battleExpText;
        [SerializeField] private TMP_Text battleStatsText;
        [SerializeField] private bool autoCreateBattleProgressUi = true;
        [SerializeField] private Vector2 battleProgressPosition = new Vector2(-42f, -42f);
        [SerializeField] private Vector2 battleProgressSize = new Vector2(440f, 154f);
        [SerializeField] private string battleLevelFormat = "Level {0}";
        [SerializeField] private string battleExpFormat = "EXP {0}/{1}  Next: {2}";
        [SerializeField] private string battleStatsFormat = "Wins {0}  Losses {1}  MVP {2}%";

        [Header("Character Selection")]
        [SerializeField] private GameObject characterCarouselRoot;
        [SerializeField] private Button openCharacterCarouselButton;
        [SerializeField] private string characterCarouselObjectName = "CharasterCarousel";
        [SerializeField] private string openCharacterButtonObjectName = "LobbyCharacterImage";
        [SerializeField] private bool closeCharacterCarouselOnEnter = false;
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
            EnsureRankedBattleButton();
            BindRankedBattleButton();
            EnsureLocalWifiBattleButton();
            BindLocalWifiBattleButton();
            EnsureBattleProgressUi();
            RefreshBattleProgressUi();

            CloseCharacterCarousel();
        }

        private void OnEnable()
        {
            ProfileService.ProfileChanged += RefreshBattleProgressUi;
            RefreshBattleProgressUi();

            CloseCharacterCarousel();

            QueueAutoOpenCharacterCarouselIfNeeded();
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= RefreshBattleProgressUi;

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

            if (localWifiBattleButton != null)
                localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);

            if (rankedBattleButton != null)
                rankedBattleButton.onClick.RemoveListener(OnClickRankedMatch);
        }

        public void RefreshBattleProgressUi()
        {
            EnsureBattleProgressUi();

            if (!ShouldShowBattleProgressUi())
                return;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
            {
                ApplyBattleProgressFallback();
                return;
            }

            profile.EnsureData();

            MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
            int level = battle != null ? Mathf.Max(1, battle.Level) : 1;
            int currentExp = battle != null ? Mathf.Max(0, battle.Experience) : 0;
            int nextExp = battle != null ? Mathf.Max(1, battle.GetExperienceRequiredForNextLevel()) : 100;
            int remainingExp = Mathf.Max(0, nextExp - currentExp);
            int wins = battle != null ? Mathf.Max(0, battle.Wins) : 0;
            int losses = battle != null ? Mathf.Max(0, battle.Losses) : 0;
            int mvpPercent = battle != null ? Mathf.Clamp(battle.MvpRatePercent, 0, 100) : 0;

            if (battleLevelText != null)
                battleLevelText.text = string.Format(battleLevelFormat, level);

            if (battleExpText != null)
                battleExpText.text = string.Format(battleExpFormat, currentExp, nextExp, remainingExp);

            if (battleStatsText != null)
                battleStatsText.text = string.Format(battleStatsFormat, wins, losses, mvpPercent);
        }

        public void OnClickRandomMatch()
        {
            if (randomMatchUsesOnlineRanked)
            {
                OnClickRankedMatch();
                return;
            }

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
                characterCarouselRoot.transform.SetAsLastSibling();

                BattleCharacterCircularCarousel carousel =
                    characterCarouselRoot.GetComponentInChildren<BattleCharacterCircularCarousel>(true);
                if (carousel != null)
                    carousel.RefreshButtons();

                Log("Character carousel opened.");
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
            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            OnlineRankedBattleLobbyUI.Show(battleGameSceneName);
            Log("RankedMatch selected, online matchmaking opened.");
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

            LocalWifiBattleLobbyUI.Show(battleGameSceneName);
            Log("LocalWifiMatch selected, runtime Wi-Fi lobby opened.");
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

            GameObject buttonObject = ResolveOpenCharacterButtonObject();

            if (buttonObject == null)
            {
                if (createOpenCharacterButtonIfMissing)
                    openCharacterCarouselButton = CreateOpenCharacterButton();

                return;
            }

            openCharacterCarouselButton = EnsureOpenCharacterHitButton(buttonObject);
        }

        private GameObject ResolveOpenCharacterButtonObject()
        {
            GameObject buttonObject = !string.IsNullOrWhiteSpace(openCharacterButtonObjectName)
                ? FindObjectByName(openCharacterButtonObjectName)
                : null;

            if (buttonObject != null)
                return buttonObject;

            BattleLobbyChar lobbyChar = FindAnyObjectByType<BattleLobbyChar>(FindObjectsInactive.Include);
            if (lobbyChar != null)
                return lobbyChar.gameObject;

            Image previewLobbyImage = FindImageByName("PreviewLobbyImage");
            if (previewLobbyImage != null)
                return previewLobbyImage.gameObject;

            BattleCharacterModelView modelView = FindAnyObjectByType<BattleCharacterModelView>(FindObjectsInactive.Include);
            return modelView != null ? modelView.gameObject : null;
        }

        private Button EnsureOpenCharacterHitButton(GameObject targetObject)
        {
            if (targetObject == null)
                return null;

            RectTransform targetRect = targetObject.transform as RectTransform;
            if (targetRect == null)
            {
                Button directButton = targetObject.GetComponent<Button>();
                if (directButton == null)
                    directButton = targetObject.AddComponent<Button>();

                Graphic directGraphic = targetObject.GetComponent<Graphic>();
                if (directGraphic != null)
                {
                    directGraphic.raycastTarget = true;
                    directButton.targetGraphic = directGraphic;
                }

                return directButton;
            }

            Transform existing = targetRect.Find("OpenCharacterHitArea");
            GameObject hitObject = existing != null
                ? existing.gameObject
                : new GameObject("OpenCharacterHitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

            if (hitObject.transform.parent != targetRect)
                hitObject.transform.SetParent(targetRect, false);

            RectTransform hitRect = hitObject.transform as RectTransform;
            hitRect.anchorMin = Vector2.zero;
            hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            hitRect.pivot = new Vector2(0.5f, 0.5f);
            hitRect.localScale = Vector3.one;

            Image hitImage = hitObject.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            hitImage.raycastTarget = true;

            Button hitButton = hitObject.GetComponent<Button>();
            hitButton.transition = Selectable.Transition.None;
            hitButton.targetGraphic = hitImage;
            hitButton.interactable = true;

            hitObject.transform.SetAsLastSibling();

            return hitButton;
        }

        private static Image FindImageByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && string.Equals(image.name, objectName, System.StringComparison.Ordinal))
                    return image;
            }

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.name.IndexOf(objectName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return image;
            }

            return null;
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

        private void EnsureLocalWifiBattleButton()
        {
            if (localWifiBattleButton != null || !autoCreateLocalWifiBattleButton)
                return;

            localWifiBattleButton = FindButtonByName("ButtonLocalWifiBattle");
            if (localWifiBattleButton == null)
                localWifiBattleButton = CreateLocalWifiBattleButton();
        }

        private void EnsureRankedBattleButton()
        {
            if (rankedBattleButton != null || !autoCreateRankedBattleButton)
                return;

            rankedBattleButton = FindButtonByName("ButtonRankedBattle");
            if (rankedBattleButton == null)
                rankedBattleButton = CreateRankedBattleButton();
        }

        private Button CreateLocalWifiBattleButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "ButtonLocalWifiBattle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localWifiBattleButtonPosition;
            rect.sizeDelta = localWifiBattleButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.2f, 0.94f);
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
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = localWifiBattleButtonText;
            label.fontSize = 30f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private Button CreateRankedBattleButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "ButtonRankedBattle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = rankedBattleButtonPosition;
            rect.sizeDelta = rankedBattleButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.16f, 0.28f, 0.96f);
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
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = rankedBattleButtonText;
            label.fontSize = 30f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
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

        private void EnsureBattleProgressUi()
        {
            if (!ShouldShowBattleProgressUi())
            {
                if (battleProgressRoot == null)
                    battleProgressRoot = FindObjectByName("BattleProgressPanel");

                if (battleProgressRoot != null)
                    battleProgressRoot.SetActive(false);

                return;
            }

            if (!autoCreateBattleProgressUi)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            if (battleProgressRoot == null)
                battleProgressRoot = FindObjectByName("BattleProgressPanel");

            if (battleProgressRoot == null)
            {
                battleProgressRoot = new GameObject(
                    "BattleProgressPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                battleProgressRoot.transform.SetParent(canvas.transform, false);
            }

            battleProgressRoot.SetActive(true);

            RectTransform rootRect = battleProgressRoot.transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(1f, 1f);
                rootRect.anchorMax = new Vector2(1f, 1f);
                rootRect.pivot = new Vector2(1f, 1f);
                rootRect.anchoredPosition = battleProgressPosition;
                rootRect.sizeDelta = battleProgressSize;
                rootRect.localScale = Vector3.one;
            }

            Image rootImage = battleProgressRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.05f, 0.07f, 0.09f, 0.86f);
                rootImage.raycastTarget = false;
            }

            battleLevelText = EnsureProgressText(
                battleProgressRoot.transform,
                battleLevelText,
                "BattleLevelText",
                new Vector2(22f, -18f),
                new Vector2(-44f, 42f),
                32f,
                TextAlignmentOptions.Left);

            battleExpText = EnsureProgressText(
                battleProgressRoot.transform,
                battleExpText,
                "BattleExpText",
                new Vector2(22f, -62f),
                new Vector2(-44f, 34f),
                22f,
                TextAlignmentOptions.Left);

            battleStatsText = EnsureProgressText(
                battleProgressRoot.transform,
                battleStatsText,
                "BattleStatsText",
                new Vector2(22f, -102f),
                new Vector2(-44f, 34f),
                22f,
                TextAlignmentOptions.Left);

            battleProgressRoot.transform.SetAsLastSibling();
        }

        private bool ShouldShowBattleProgressUi()
        {
            return !string.Equals(
                SceneManager.GetActiveScene().name,
                battleGameSceneName,
                System.StringComparison.Ordinal);
        }

        private TMP_Text EnsureProgressText(
            Transform parent,
            TMP_Text current,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            if (current != null)
                return current;

            Transform existing = parent != null ? parent.Find(objectName) : null;
            if (existing != null)
            {
                TMP_Text existingText = existing.GetComponent<TMP_Text>();
                if (existingText != null)
                    return existingText;
            }

            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.9f, 0.96f, 1f, 1f);
            label.raycastTarget = false;

            return label;
        }

        private void ApplyBattleProgressFallback()
        {
            if (battleLevelText != null)
                battleLevelText.text = string.Format(battleLevelFormat, 1);

            if (battleExpText != null)
                battleExpText.text = string.Format(battleExpFormat, 0, 100, 100);

            if (battleStatsText != null)
                battleStatsText.text = string.Format(battleStatsFormat, 0, 0, 0);
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
            openCharacterCarouselButton.interactable = true;
        }

        private void BindReturnButton()
        {
            if (returnToLobbyButton == null)
                return;

            returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);
            returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
        }

        private void BindLocalWifiBattleButton()
        {
            if (localWifiBattleButton == null)
                return;

            localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);
            localWifiBattleButton.onClick.AddListener(OnClickLocalWifiMatch);
            localWifiBattleButton.interactable = true;
        }

        private void BindRankedBattleButton()
        {
            if (rankedBattleButton == null)
                return;

            rankedBattleButton.onClick.RemoveListener(OnClickRankedMatch);
            rankedBattleButton.onClick.AddListener(OnClickRankedMatch);
            rankedBattleButton.interactable = true;
        }

        private static Button FindButtonByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && string.Equals(button.gameObject.name, objectName, System.StringComparison.Ordinal))
                    return button;
            }

            return null;
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleLobbyUI] {message}", this);
        }
    }
}
