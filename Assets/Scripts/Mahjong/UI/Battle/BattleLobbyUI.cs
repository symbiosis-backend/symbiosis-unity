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
        private static readonly Rect BattleLobbyMatchButtonSpriteRect = new Rect(67f, 373f, 1431f, 339f);
        private static readonly Rect BattleLobbyTopBarSpriteRect = new Rect(10f, 3f, 1432f, 343f);
        private static readonly Vector4 BattleLobbyMatchButtonLabelMargin = new Vector4(34f, 10f, 34f, 12f);
        private static readonly Vector4 BattleLobbyUtilityButtonLabelMargin = new Vector4(22f, 6f, 22f, 8f);
        private const string BattleLobbyTopBarObjectName = "TopBar";
        private const string BattleLobbyTopBarGraphicPath = "Bar/Image";
        private const string BattleLobbyTopBarGraphicAlternatePath = "Bar/UI";

        [Header("Scene Names")]
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string battleGameSceneName = "GameMahjongBattle";
        [SerializeField] private string mainLobbySceneName = "LobbyMahjong";
        [SerializeField] private string friendLobbySceneName = "";
        [SerializeField] private string localWifiSceneName = "";
        [SerializeField] private bool randomMatchUsesOnlineRanked = false;

        [Header("Random Match Button")]
        [SerializeField] private Button randomMatchButton;
        [SerializeField] private bool autoCreateRandomMatchButton = true;
        [SerializeField] private string randomMatchButtonText = "Random Match";
        [SerializeField] private Vector2 randomMatchButtonPosition = new Vector2(0f, -228f);
        [SerializeField] private Vector2 randomMatchButtonSize = new Vector2(320f, 76f);

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

        [Header("Battle Lobby Visuals")]
        [SerializeField] private Sprite battleLobbyButtonSprite;
        [SerializeField] private Sprite battleLobbyTopBarSprite;
        [SerializeField] private TMP_FontAsset battleLobbyMainFont;

        [Header("Return Button")]
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private bool autoCreateReturnButton = true;
        [SerializeField] private string returnButtonText = "Lobby";
        [SerializeField] private Vector2 returnButtonPosition = new Vector2(42f, -42f);
        [SerializeField] private Vector2 returnButtonSize = new Vector2(220f, 72f);

        [Header("Battle Shop")]
        [SerializeField] private Button battleShopButton;
        [SerializeField] private bool autoCreateBattleShopButton = true;
        [SerializeField] private string battleShopButtonText = "Shop";
        [SerializeField] private Vector2 battleShopButtonPosition = new Vector2(42f, -124f);
        [SerializeField] private Vector2 battleShopButtonSize = new Vector2(220f, 72f);
        [SerializeField, Min(1)] private int shopEnergyAmount = 20;
        [SerializeField, Min(1)] private int shopEnergyAmetistPrice = 5;

        [Header("Battle Progress")]
        [SerializeField] private GameObject battleProgressRoot;
        [SerializeField] private TMP_Text battleLevelText;
        [SerializeField] private TMP_Text battleExpText;
        [SerializeField] private TMP_Text battleStatsText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text energyHintText;
        [SerializeField] private Button energyAdButton;
        [SerializeField] private bool autoCreateBattleProgressUi = true;
        [SerializeField] private Vector2 battleProgressPosition = new Vector2(-42f, -42f);
        [SerializeField] private Vector2 battleProgressSize = new Vector2(440f, 278f);
        [SerializeField] private string battleLevelFormat = "Level {0}";
        [SerializeField] private string battleExpFormat = "EXP {0}/{1}  Next: {2}";
        [SerializeField] private string battleStatsFormat = "Wins {0}  Losses {1}  MVP {2}%";
        [SerializeField] private string energyFormat = "Energy {0}/{1}";
        [SerializeField] private string energyReadyFormat = "Match -{0} | Full";
        [SerializeField] private string energyRefillFormat = "Match -{0} | +1 in {1}";
        [SerializeField] private string energyAdButtonFormat = "REKLAMA +{0}";

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
        private Coroutine energyRefreshRoutine;
        private GameObject battleShopRoot;
        private TMP_Text battleShopBalanceText;
        private TMP_Text battleShopStatusText;
        private Button shopEnergyTabButton;
        private Button shopCharactersTabButton;
        private Button shopSkinsTabButton;
        private Button shopBuyEnergyButton;
        private GameObject shopEnergySection;
        private GameObject shopCharactersSection;
        private GameObject shopSkinsSection;
        private Sprite cachedBattleLobbyButtonSprite;
        private Sprite cachedBattleLobbyTopBarSprite;

        private void Awake()
        {
            AutoResolveCharacterSelectionLinks();
            BindCharacterSelectionButton();
            EnsureAndBindLobbyButtonsIfNeeded();
            EnsureBattleProgressUi();
            RefreshBattleProgressUi();
            ApplyBattleLobbyVisuals();

            CloseCharacterCarousel();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            ProfileService.ProfileChanged += RefreshBattleProgressUi;
            EnergyService.EnergyChanged += RefreshBattleProgressUi;
            CurrencyService.CurrencyChanged += RefreshBattleShopUi;
            EnsureAndBindLobbyButtonsIfNeeded();
            RefreshBattleProgressUi();
            RefreshBattleShopUi();
            ApplyBattleLobbyVisuals();
            StartEnergyRefreshRoutine();

            CloseCharacterCarousel();

            if (ShouldShowLobbyButtons())
                QueueAutoOpenCharacterCarouselIfNeeded();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            ProfileService.ProfileChanged -= RefreshBattleProgressUi;
            EnergyService.EnergyChanged -= RefreshBattleProgressUi;
            CurrencyService.CurrencyChanged -= RefreshBattleShopUi;

            if (autoOpenCharacterCarouselRoutine != null)
            {
                StopCoroutine(autoOpenCharacterCarouselRoutine);
                autoOpenCharacterCarouselRoutine = null;
            }

            if (energyRefreshRoutine != null)
            {
                StopCoroutine(energyRefreshRoutine);
                energyRefreshRoutine = null;
            }
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            if (openCharacterCarouselButton != null)
                openCharacterCarouselButton.onClick.RemoveListener(OnClickOpenCharacterCarousel);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);

            if (battleShopButton != null)
                battleShopButton.onClick.RemoveListener(OnClickOpenBattleShop);

            if (randomMatchButton != null)
                randomMatchButton.onClick.RemoveListener(OnClickRandomMatch);

            if (localWifiBattleButton != null)
                localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);

            if (rankedBattleButton != null)
                rankedBattleButton.onClick.RemoveListener(OnClickRankedMatch);

            if (energyAdButton != null)
                energyAdButton.onClick.RemoveListener(OnClickRewardedEnergyAd);

            if (shopEnergyTabButton != null)
                shopEnergyTabButton.onClick.RemoveListener(ShowBattleShopEnergy);

            if (shopCharactersTabButton != null)
                shopCharactersTabButton.onClick.RemoveListener(ShowBattleShopCharacters);

            if (shopSkinsTabButton != null)
                shopSkinsTabButton.onClick.RemoveListener(ShowBattleShopSkins);

            if (shopBuyEnergyButton != null)
                shopBuyEnergyButton.onClick.RemoveListener(OnClickBuyEnergyWithAmetist);

            EnergyService.EnergyChanged -= RefreshBattleProgressUi;
            CurrencyService.CurrencyChanged -= RefreshBattleShopUi;
        }

        private void HandleActiveSceneChanged(Scene previous, Scene current)
        {
            EnsureAndBindLobbyButtonsIfNeeded();
            EnsureBattleProgressUi();
            RefreshBattleProgressUi();
            ApplyBattleLobbyVisuals();
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

            RefreshEnergyUi();
        }

        public void OnClickRandomMatch()
        {
            if (!TrySpendMatchEnergy())
                return;

            RandomBattleLobbyUI.Show(battleGameSceneName);
            Log("RandomMatch selected, online search opened with bot fallback.");
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

        public void OnClickOpenBattleShop()
        {
            EnsureCurrencyService();
            EnsureBattleShopUi();
            ShowBattleShopEnergy();

            if (battleShopRoot != null)
            {
                battleShopRoot.SetActive(true);
                battleShopRoot.transform.SetAsLastSibling();
            }
        }

        private void CloseBattleShop()
        {
            if (battleShopRoot != null)
                battleShopRoot.SetActive(false);
        }

        public void OpenCharacterCarousel()
        {
            AutoResolveCharacterSelectionLinks();

            if (characterCarouselRoot != null)
            {
                SetLobbyHudVisibleWhileCarousel(false);
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

            SetLobbyHudVisibleWhileCarousel(true);
        }

        public void RestoreLobbyHudAfterCharacterCarouselClosed()
        {
            AutoResolveCharacterSelectionLinks();
            EnsureAndBindLobbyButtonsIfNeeded();
            SetLobbyHudVisibleWhileCarousel(true);
            RefreshBattleProgressUi();
            RefreshEnergyUi();
            ApplyBattleLobbyVisuals();
        }

        public void OnClickRankedMatch()
        {
            if (!TrySpendMatchEnergy())
                return;

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
            if (!TrySpendMatchEnergy())
                return;

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

        private void EnsureAndBindLobbyButtonsIfNeeded()
        {
            bool showLobbyButtons = ShouldShowLobbyButtons();

            if (showLobbyButtons)
            {
                EnsureReturnButton();
                BindReturnButton();
                EnsureBattleShopButton();
                BindBattleShopButton();
                EnsureRandomMatchButton();
                BindRandomMatchButton();
                EnsureRankedBattleButton();
                BindRankedBattleButton();
                EnsureLocalWifiBattleButton();
                BindLocalWifiBattleButton();
            }
            else
            {
                ResolveLobbyButtonReferences();
            }

            SetLobbyButtonsVisible(showLobbyButtons);
            SetCharacterEntryPointVisible(showLobbyButtons);

            if (showLobbyButtons)
                ApplyBattleLobbyVisuals();
        }

        private bool ShouldShowLobbyButtons()
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                battleLobbySceneName,
                System.StringComparison.Ordinal);
        }

        private void SetLobbyButtonsVisible(bool visible)
        {
            SetButtonVisible(returnToLobbyButton, visible);
            SetButtonVisible(battleShopButton, visible);
            SetButtonVisible(randomMatchButton, visible);
            SetButtonVisible(rankedBattleButton, visible);
            SetButtonVisible(localWifiBattleButton, visible);

            if (!visible && battleShopRoot != null)
                battleShopRoot.SetActive(false);
        }

        private void SetLobbyHudVisibleWhileCarousel(bool visible)
        {
            bool shouldShow = visible && ShouldShowLobbyButtons();

            SetLobbyButtonsVisible(shouldShow);
            SetCharacterEntryButtonVisible(shouldShow);

            if (battleProgressRoot != null)
                battleProgressRoot.SetActive(shouldShow);

            if (battleShopRoot != null && !shouldShow)
                battleShopRoot.SetActive(false);
        }

        private void ResolveLobbyButtonReferences()
        {
            if (returnToLobbyButton == null)
                returnToLobbyButton = FindButtonByName("ButtonReturnToLobby");
            if (battleShopButton == null)
                battleShopButton = FindButtonByName("ButtonBattleShop");
            if (randomMatchButton == null)
                randomMatchButton = FindButtonByName("ButtonRandomMatch");
            if (rankedBattleButton == null)
                rankedBattleButton = FindButtonByName("ButtonRankedBattle");
            if (localWifiBattleButton == null)
                localWifiBattleButton = FindButtonByName("ButtonLocalWifiBattle");
        }

        private void SetCharacterEntryPointVisible(bool visible)
        {
            SetCharacterEntryButtonVisible(visible);

            if (characterCarouselRoot != null)
                characterCarouselRoot.SetActive(false);
        }

        private void SetCharacterEntryButtonVisible(bool visible)
        {
            if (openCharacterCarouselButton != null && openCharacterCarouselButton.gameObject != null)
                SetButtonVisible(openCharacterCarouselButton, visible);

            GameObject entry = FindObjectByName(openCharacterButtonObjectName);
            if (entry != null && entry.activeSelf != visible)
                entry.SetActive(visible);

            GameObject generated = FindObjectByName("OpenCharacterCarouselButton");
            if (generated != null && generated.activeSelf != visible)
                generated.SetActive(visible);
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button == null || button.gameObject == null)
                return;

            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }

        private void ApplyBattleLobbyVisuals()
        {
            if (!ShouldShowLobbyButtons())
                return;

            ApplyBattleLobbyTopBarVisual();
            ApplyBattleLobbyTypography();
            ApplyBattleLobbyMatchButtonVisual(randomMatchButton);
            ApplyBattleLobbyMatchButtonVisual(rankedBattleButton);
            ApplyBattleLobbyMatchButtonVisual(localWifiBattleButton);
            ApplyBattleLobbyUtilityButtonVisual(returnToLobbyButton);
            ApplyBattleLobbyUtilityButtonVisual(battleShopButton);
        }

        private void ApplyBattleLobbyTopBarVisual()
        {
            GameObject topBarObject = FindObjectByName(BattleLobbyTopBarObjectName);
            if (topBarObject == null)
                return;

            ApplyBattleLobbyFontToDescendantTexts(topBarObject.transform);

            Image topBarImage = ResolveBattleLobbyTopBarImage(topBarObject.transform);
            Sprite sprite = GetBattleLobbyTopBarSprite();
            if (topBarImage == null || sprite == null)
                return;

            topBarImage.sprite = sprite;
            topBarImage.type = Image.Type.Simple;
            topBarImage.preserveAspect = false;
            topBarImage.color = Color.white;
            topBarImage.raycastTarget = false;
        }

        private void ApplyBattleLobbyTypography()
        {
            ApplyBattleLobbyFontToButton(returnToLobbyButton);
            ApplyBattleLobbyFontToButton(battleShopButton);
            ApplyBattleLobbyFontToButton(openCharacterCarouselButton);
            ApplyBattleLobbyFontToButton(energyAdButton);
            ApplyBattleLobbyFontToDescendantTexts(battleProgressRoot != null ? battleProgressRoot.transform : null);
            ApplyBattleLobbyFontToDescendantTexts(battleShopRoot != null ? battleShopRoot.transform : null);
        }

        private void ApplyBattleLobbyMatchButtonVisual(Button button)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = GetBattleLobbyMatchButtonSprite();
            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.type = Image.Type.Simple;
                button.image.preserveAspect = false;
                button.image.color = Color.white;
                button.targetGraphic = button.image;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            ApplyBattleLobbyFontToText(label);
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(16f, label.fontSize * 0.6f);
            label.fontSizeMax = Mathf.Max(label.fontSizeMax, label.fontSize);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = BattleLobbyMatchButtonLabelMargin;
        }

        private void ApplyBattleLobbyUtilityButtonVisual(Button button)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = GetBattleLobbyMatchButtonSprite();
            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.type = Image.Type.Simple;
                button.image.preserveAspect = false;
                button.image.color = Color.white;
                button.targetGraphic = button.image;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            ApplyBattleLobbyFontToText(label);
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(14f, label.fontSize * 0.58f);
            label.fontSizeMax = Mathf.Max(label.fontSizeMax, label.fontSize);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = BattleLobbyUtilityButtonLabelMargin;
        }

        private void ApplyBattleLobbyFontToButton(Button button)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            ApplyBattleLobbyFontToText(label);
        }

        private void ApplyBattleLobbyFontToDescendantTexts(Transform root)
        {
            if (root == null)
                return;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                ApplyBattleLobbyFontToText(texts[i]);
        }

        private void ApplyBattleLobbyFontToText(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = battleLobbyMainFont != null ? battleLobbyMainFont : TMP_Settings.defaultFontAsset;
            if (font == null)
                return;

            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        private Sprite GetBattleLobbyMatchButtonSprite()
        {
            if (battleLobbyButtonSprite == null)
                return null;

            if (cachedBattleLobbyButtonSprite != null)
                return cachedBattleLobbyButtonSprite;

            cachedBattleLobbyButtonSprite = CreateRuntimeSpriteVariant(battleLobbyButtonSprite, BattleLobbyMatchButtonSpriteRect);
            return cachedBattleLobbyButtonSprite;
        }

        private Sprite GetBattleLobbyTopBarSprite()
        {
            if (battleLobbyTopBarSprite == null)
                return null;

            if (cachedBattleLobbyTopBarSprite != null)
                return cachedBattleLobbyTopBarSprite;

            cachedBattleLobbyTopBarSprite = CreateRuntimeSpriteVariant(battleLobbyTopBarSprite, BattleLobbyTopBarSpriteRect);
            return cachedBattleLobbyTopBarSprite;
        }

        private static Sprite CreateRuntimeSpriteVariant(Sprite source, Rect targetRect)
        {
            if (source == null || source.texture == null)
                return source;

            Rect sourceRect = source.textureRect;
            Rect rect = ClampRectToBounds(targetRect, sourceRect);

            if (Mathf.Approximately(rect.x, sourceRect.x)
                && Mathf.Approximately(rect.y, sourceRect.y)
                && Mathf.Approximately(rect.width, sourceRect.width)
                && Mathf.Approximately(rect.height, sourceRect.height))
            {
                return source;
            }

            return Sprite.Create(
                source.texture,
                rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        private static Rect ClampRectToBounds(Rect targetRect, Rect bounds)
        {
            float x = Mathf.Clamp(targetRect.x, bounds.xMin, bounds.xMax - 1f);
            float y = Mathf.Clamp(targetRect.y, bounds.yMin, bounds.yMax - 1f);
            float width = Mathf.Clamp(targetRect.width, 1f, bounds.xMax - x);
            float height = Mathf.Clamp(targetRect.height, 1f, bounds.yMax - y);
            return new Rect(x, y, width, height);
        }

        private static Image ResolveBattleLobbyTopBarImage(Transform topBarTransform)
        {
            if (topBarTransform == null)
                return null;

            Transform direct = topBarTransform.Find(BattleLobbyTopBarGraphicPath);
            if (direct != null)
            {
                Image directImage = direct.GetComponent<Image>();
                if (directImage != null)
                    return directImage;
            }

            Transform alternate = topBarTransform.Find(BattleLobbyTopBarGraphicAlternatePath);
            if (alternate != null)
            {
                Image alternateImage = alternate.GetComponent<Image>();
                if (alternateImage != null)
                    return alternateImage;
            }

            Image[] images = topBarTransform.GetComponentsInChildren<Image>(true);
            Image bestImage = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                    continue;

                RectTransform rect = image.rectTransform;
                if (rect == null)
                    continue;

                float area = Mathf.Abs(rect.rect.width * rect.rect.height);
                float score = area;
                if (image.sprite != null)
                    score += 1_000_000f;
                if (image.color.a > 0.01f)
                    score += 100_000f;
                if (image.raycastTarget)
                    score -= 10_000f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestImage = image;
                }
            }

            return bestImage;
        }

        private void EnsureRandomMatchButton()
        {
            if (randomMatchButton != null || !autoCreateRandomMatchButton)
                return;

            randomMatchButton = FindButtonByName("ButtonRandomMatch");
            if (randomMatchButton == null)
                randomMatchButton = CreateRandomMatchButton();
        }

        private void EnsureBattleShopButton()
        {
            if (battleShopButton != null || !autoCreateBattleShopButton)
                return;

            battleShopButton = FindButtonByName("ButtonBattleShop");
            if (battleShopButton == null)
                battleShopButton = CreateBattleShopButton();
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

        private Button CreateRandomMatchButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "ButtonRandomMatch",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = randomMatchButtonPosition;
            rect.sizeDelta = randomMatchButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.24f, 0.19f, 0.96f);
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
            label.text = randomMatchButtonText;
            label.fontSize = 30f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
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

        private Button CreateBattleShopButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject buttonObject = new GameObject(
                "ButtonBattleShop",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = battleShopButtonPosition;
            rect.sizeDelta = battleShopButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.13f, 0.12f, 0.2f, 0.92f);
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
            label.text = battleShopButtonText;
            label.fontSize = 28f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 34f;
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

            energyText = EnsureProgressText(
                battleProgressRoot.transform,
                energyText,
                "BattleEnergyText",
                new Vector2(22f, -142f),
                new Vector2(-44f, 34f),
                24f,
                TextAlignmentOptions.Left);

            energyHintText = EnsureProgressText(
                battleProgressRoot.transform,
                energyHintText,
                "BattleEnergyHintText",
                new Vector2(22f, -176f),
                new Vector2(-44f, 30f),
                18f,
                TextAlignmentOptions.Left);

            energyAdButton = EnsureEnergyAdButton(
                battleProgressRoot.transform,
                energyAdButton,
                "BattleEnergyAdButton",
                new Vector2(22f, -218f),
                new Vector2(-44f, 46f));

            battleProgressRoot.transform.SetAsLastSibling();
            ApplyBattleLobbyTypography();
        }

        private bool ShouldShowBattleProgressUi()
        {
            if (IsCharacterCarouselOpen())
                return false;

            return !string.Equals(
                SceneManager.GetActiveScene().name,
                battleGameSceneName,
                System.StringComparison.Ordinal);
        }

        private bool IsCharacterCarouselOpen()
        {
            return characterCarouselRoot != null && characterCarouselRoot.activeInHierarchy;
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

        private Button EnsureEnergyAdButton(
            Transform parent,
            Button current,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (current != null)
            {
                current.onClick.RemoveListener(OnClickRewardedEnergyAd);
                current.onClick.AddListener(OnClickRewardedEnergyAd);
                return current;
            }

            Transform existing = parent != null ? parent.Find(objectName) : null;
            if (existing != null)
            {
                Button existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    existingButton.onClick.RemoveListener(OnClickRewardedEnergyAd);
                    existingButton.onClick.AddListener(OnClickRewardedEnergyAd);
                    return existingButton;
                }
            }

            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.16f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnClickRewardedEnergyAd);

            TMP_Text label = EnsureProgressText(
                buttonObject.transform,
                null,
                "Label",
                Vector2.zero,
                new Vector2(-20f, 36f),
                19f,
                TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10f, 4f);
            label.rectTransform.offsetMax = new Vector2(-10f, -4f);
            label.color = Color.white;

            return button;
        }

        private void EnsureBattleShopUi()
        {
            if (battleShopRoot != null)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            battleShopRoot = new GameObject("BattleShopOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            battleShopRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = battleShopRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = battleShopRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);
            dim.raycastTarget = true;

            GameObject panel = CreateShopPanel(battleShopRoot.transform, "BattleShopPanel", new Vector2(820f, 640f), Vector2.zero, new Color(0.045f, 0.052f, 0.07f, 0.98f));
            CreateShopText(panel.transform, "Title", "Battle Shop", new Vector2(0f, 270f), new Vector2(700f, 58f), 42f, TextAlignmentOptions.Center, Color.white);
            battleShopBalanceText = CreateShopText(panel.transform, "Balance", string.Empty, new Vector2(0f, 220f), new Vector2(700f, 38f), 22f, TextAlignmentOptions.Center, new Color(0.86f, 0.92f, 1f, 1f));
            battleShopStatusText = CreateShopText(panel.transform, "Status", string.Empty, new Vector2(0f, -270f), new Vector2(700f, 34f), 20f, TextAlignmentOptions.Center, new Color(0.82f, 0.88f, 0.95f, 1f));

            shopEnergyTabButton = CreateShopButton(panel.transform, "TabEnergy", "Energy", new Vector2(-230f, 164f), new Vector2(190f, 54f), new Color(0.15f, 0.2f, 0.26f, 0.96f), 22f);
            shopCharactersTabButton = CreateShopButton(panel.transform, "TabCharacters", "Characters", new Vector2(0f, 164f), new Vector2(220f, 54f), new Color(0.15f, 0.2f, 0.26f, 0.96f), 22f);
            shopSkinsTabButton = CreateShopButton(panel.transform, "TabSkins", "Skins", new Vector2(245f, 164f), new Vector2(190f, 54f), new Color(0.15f, 0.2f, 0.26f, 0.96f), 22f);

            shopEnergyTabButton.onClick.AddListener(ShowBattleShopEnergy);
            shopCharactersTabButton.onClick.AddListener(ShowBattleShopCharacters);
            shopSkinsTabButton.onClick.AddListener(ShowBattleShopSkins);

            shopEnergySection = CreateShopPanel(panel.transform, "EnergySection", new Vector2(700f, 300f), new Vector2(0f, -24f), new Color(0.075f, 0.092f, 0.12f, 0.95f));
            CreateShopText(shopEnergySection.transform, "EnergyTitle", "Energy Pack", new Vector2(0f, 92f), new Vector2(610f, 44f), 30f, TextAlignmentOptions.Center, Color.white);
            CreateShopText(shopEnergySection.transform, "EnergyBody", $"+{shopEnergyAmount} Energy", new Vector2(0f, 42f), new Vector2(610f, 36f), 24f, TextAlignmentOptions.Center, new Color(0.86f, 0.96f, 0.9f, 1f));
            CreateShopText(shopEnergySection.transform, "EnergyPrice", $"{shopEnergyAmetistPrice} Ametist", new Vector2(0f, 0f), new Vector2(610f, 34f), 22f, TextAlignmentOptions.Center, new Color(0.78f, 0.72f, 1f, 1f));
            shopBuyEnergyButton = CreateShopButton(shopEnergySection.transform, "ButtonBuyEnergy", "Buy Energy", new Vector2(0f, -78f), new Vector2(280f, 58f), new Color(0.18f, 0.24f, 0.16f, 0.96f), 24f);
            shopBuyEnergyButton.onClick.AddListener(OnClickBuyEnergyWithAmetist);

            shopCharactersSection = CreateShopPanel(panel.transform, "CharactersSection", new Vector2(700f, 300f), new Vector2(0f, -24f), new Color(0.075f, 0.092f, 0.12f, 0.95f));
            CreateShopText(shopCharactersSection.transform, "CharactersTitle", "Characters", new Vector2(0f, 58f), new Vector2(610f, 46f), 30f, TextAlignmentOptions.Center, Color.white);
            CreateShopText(shopCharactersSection.transform, "CharactersBody", "Coming soon", new Vector2(0f, 6f), new Vector2(610f, 40f), 24f, TextAlignmentOptions.Center, new Color(0.76f, 0.82f, 0.9f, 1f));

            shopSkinsSection = CreateShopPanel(panel.transform, "SkinsSection", new Vector2(700f, 300f), new Vector2(0f, -24f), new Color(0.075f, 0.092f, 0.12f, 0.95f));
            CreateShopText(shopSkinsSection.transform, "SkinsTitle", "Skins", new Vector2(0f, 58f), new Vector2(610f, 46f), 30f, TextAlignmentOptions.Center, Color.white);
            CreateShopText(shopSkinsSection.transform, "SkinsBody", "Coming soon", new Vector2(0f, 6f), new Vector2(610f, 40f), 24f, TextAlignmentOptions.Center, new Color(0.76f, 0.82f, 0.9f, 1f));

            Button closeButton = CreateShopButton(panel.transform, "ButtonCloseShop", "Close", new Vector2(0f, -218f), new Vector2(220f, 56f), new Color(0.13f, 0.15f, 0.19f, 0.96f), 22f);
            closeButton.onClick.AddListener(CloseBattleShop);

            battleShopRoot.SetActive(false);
            ApplyBattleLobbyTypography();
        }

        private void ShowBattleShopEnergy()
        {
            SetBattleShopSection(shopEnergySection);
            RefreshBattleShopUi();
        }

        private void ShowBattleShopCharacters()
        {
            SetBattleShopSection(shopCharactersSection);
            RefreshBattleShopUi();
        }

        private void ShowBattleShopSkins()
        {
            SetBattleShopSection(shopSkinsSection);
            RefreshBattleShopUi();
        }

        private void SetBattleShopSection(GameObject activeSection)
        {
            if (shopEnergySection != null)
                shopEnergySection.SetActive(shopEnergySection == activeSection);
            if (shopCharactersSection != null)
                shopCharactersSection.SetActive(shopCharactersSection == activeSection);
            if (shopSkinsSection != null)
                shopSkinsSection.SetActive(shopSkinsSection == activeSection);

            SetShopTabColor(shopEnergyTabButton, shopEnergySection == activeSection);
            SetShopTabColor(shopCharactersTabButton, shopCharactersSection == activeSection);
            SetShopTabColor(shopSkinsTabButton, shopSkinsSection == activeSection);
        }

        private void OnClickBuyEnergyWithAmetist()
        {
            EnsureCurrencyService();

            if (EnergyService.CurrentEnergy >= EnergyService.CurrentMaxEnergy)
            {
                SetBattleShopStatus("Energy is already full.");
                RefreshBattleShopUi();
                return;
            }

            if (CurrencyService.I == null || !CurrencyService.I.CanSpendOzAmetist(shopEnergyAmetistPrice))
            {
                SetBattleShopStatus("Not enough Ametist.");
                RefreshBattleShopUi();
                return;
            }

            if (!CurrencyService.I.SpendOzAmetist(shopEnergyAmetistPrice))
            {
                SetBattleShopStatus("Purchase failed.");
                RefreshBattleShopUi();
                return;
            }

            if (!EnergyService.AddEnergy(shopEnergyAmount))
            {
                CurrencyService.I.AddOzAmetist(shopEnergyAmetistPrice);
                SetBattleShopStatus("Energy is already full.");
                RefreshBattleShopUi();
                return;
            }

            SetBattleShopStatus($"+{shopEnergyAmount} Energy purchased.");
            RefreshEnergyUi();
            RefreshBattleShopUi();
        }

        private void RefreshBattleShopUi()
        {
            if (battleShopRoot == null)
                return;

            EnsureCurrencyService();

            int ametist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            int energy = EnergyService.CurrentEnergy;
            int maxEnergy = EnergyService.CurrentMaxEnergy;

            if (battleShopBalanceText != null)
                battleShopBalanceText.text = $"Ametist: {ametist}   Energy: {energy}/{maxEnergy}";

            if (shopBuyEnergyButton != null)
            {
                shopBuyEnergyButton.interactable = energy < maxEnergy && CurrencyService.I != null && CurrencyService.I.CanSpendOzAmetist(shopEnergyAmetistPrice);
                TMP_Text label = shopBuyEnergyButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = $"+{shopEnergyAmount} Energy / {shopEnergyAmetistPrice} Ametist";
            }

            if (battleShopStatusText != null && string.IsNullOrWhiteSpace(battleShopStatusText.text))
                battleShopStatusText.text = energy >= maxEnergy ? "Energy is full." : "Choose an item.";
        }

        private void SetBattleShopStatus(string message)
        {
            if (battleShopStatusText != null)
                battleShopStatusText.text = message ?? string.Empty;
        }

        private static void EnsureCurrencyService()
        {
            if (CurrencyService.I != null)
                return;

            GameObject serviceObject = new GameObject("CurrencyService");
            serviceObject.AddComponent<CurrencyService>();
        }

        private static GameObject CreateShopPanel(Transform parent, string objectName, Vector2 size, Vector2 position, Color color)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            return panel;
        }

        private static TMP_Text CreateShopText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
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
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateShopButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size, Color color, float fontSize)
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
            image.color = color;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateShopText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(22f, 10f), fontSize, TextAlignmentOptions.Center, Color.white);
            return button;
        }

        private static void SetShopTabColor(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? new Color(0.25f, 0.2f, 0.42f, 0.98f) : new Color(0.15f, 0.2f, 0.26f, 0.96f);
        }

        private void ApplyBattleProgressFallback()
        {
            if (battleLevelText != null)
                battleLevelText.text = string.Format(battleLevelFormat, 1);

            if (battleExpText != null)
                battleExpText.text = string.Format(battleExpFormat, 0, 100, 100);

            if (battleStatsText != null)
                battleStatsText.text = string.Format(battleStatsFormat, 0, 0, 0);

            RefreshEnergyUi();
        }

        private void StartEnergyRefreshRoutine()
        {
            if (energyRefreshRoutine != null)
                StopCoroutine(energyRefreshRoutine);

            energyRefreshRoutine = StartCoroutine(EnergyRefreshRoutine());
        }

        private IEnumerator EnergyRefreshRoutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
            while (isActiveAndEnabled)
            {
                RefreshEnergyUi();
                yield return wait;
            }
        }

        private bool TrySpendMatchEnergy()
        {
            if (EnergyService.TrySpendForMatch())
            {
                RefreshEnergyUi();
                return true;
            }

            RefreshEnergyUi();
            Log($"Not enough energy. Need {EnergyService.MatchEnergyCost}, have {EnergyService.CurrentEnergy}.");
            return false;
        }

        private void RefreshEnergyUi()
        {
            int current = EnergyService.CurrentEnergy;
            int max = EnergyService.CurrentMaxEnergy;
            bool canClaimAdEnergy = EnergyService.CanClaimRewardedAdEnergy();
            bool infiniteEnergy = EnergyService.HasInfiniteEnergy();

            if (energyText != null)
                energyText.text = infiniteEnergy ? "Energy ∞" : string.Format(energyFormat, current, max);

            if (energyHintText != null)
            {
                energyHintText.text = infiniteEnergy
                    ? $"Match -0 | Admin"
                    : current >= max
                    ? string.Format(energyReadyFormat, EnergyService.MatchEnergyCost)
                    : string.Format(energyRefillFormat, EnergyService.MatchEnergyCost, EnergyService.FormatTimeUntilNextEnergy());
            }

            SetMatchButtonInteractable(randomMatchButton, true);
            SetMatchButtonInteractable(rankedBattleButton, true);
            SetMatchButtonInteractable(localWifiBattleButton, true);

            if (energyAdButton != null)
            {
                energyAdButton.interactable = canClaimAdEnergy;
                TMP_Text label = energyAdButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = string.Format(energyAdButtonFormat, EnergyService.RewardedAdEnergyAmount);
            }

            RefreshBattleShopUi();
        }

        private void OnClickRewardedEnergyAd()
        {
            bool success = EnergyService.TryClaimRewardedAdEnergy();
            RefreshEnergyUi();
            Log(success
                ? $"Rewarded energy ad claimed: +{EnergyService.RewardedAdEnergyAmount}."
                : "Rewarded energy ad is unavailable.");
        }

        private static void SetMatchButtonInteractable(Button button, bool interactable)
        {
            if (button == null)
                return;

            button.interactable = interactable;
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

        private void BindBattleShopButton()
        {
            if (battleShopButton == null)
                return;

            battleShopButton.onClick.RemoveListener(OnClickOpenBattleShop);
            battleShopButton.onClick.AddListener(OnClickOpenBattleShop);
            battleShopButton.interactable = true;
        }

        private void BindLocalWifiBattleButton()
        {
            if (localWifiBattleButton == null)
                return;

            localWifiBattleButton.onClick.RemoveListener(OnClickLocalWifiMatch);
            localWifiBattleButton.onClick.AddListener(OnClickLocalWifiMatch);
            localWifiBattleButton.interactable = true;
        }

        private void BindRandomMatchButton()
        {
            if (randomMatchButton == null)
                return;

            randomMatchButton.onClick.RemoveListener(OnClickRandomMatch);
            randomMatchButton.onClick.AddListener(OnClickRandomMatch);
            randomMatchButton.interactable = true;
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
