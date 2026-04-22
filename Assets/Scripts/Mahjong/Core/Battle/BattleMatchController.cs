using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleMatchController : MonoBehaviour
    {
        public event Action<BattleMatchController> MatchStarted;
        public event Action<BattleMatchController, int> RoundStarted;
        public event Action<BattleMatchController, int> RoundFinished;
        public event Action<BattleMatchController, bool> MatchFinished;
        public event Action<BattleMatchController> MatchStateChanged;

        [Header("Boards")]
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleBotController botController;
        [SerializeField] private BattleCombatSystem combatSystem;

        [Header("Round UI")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text stateText;

        [Header("Player Profile UI")]
        [SerializeField] private Image playerBattleSpriteImage;
        [SerializeField] private BattleCharacterModelView playerBattleModelView;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerTitleText;
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private string playerRankFormat = "{0} {1} RP";
        [SerializeField] private string fallbackPlayerName = "Player";
        [SerializeField] private string fallbackPlayerRankTier = "Unranked";
        [SerializeField] private bool createPlayerProfileUiIfMissing = true;
        [SerializeField] private Vector2 playerProfileUiSize = new Vector2(420f, 172f);
        [SerializeField] private Vector2 playerProfileUiOffset = new Vector2(-120f, -50f);
        [SerializeField] private Vector2 playerBattleSpriteSize = new Vector2(156f, 156f);
        [SerializeField] private Vector2 playerBattleSpriteOffset = new Vector2(-12f, -8f);
        [SerializeField] private Image playerHpBarFill;
        [SerializeField] private TMP_Text playerHpBarText;
        [SerializeField] private Vector2 playerHpBarOffset = new Vector2(16f, -122f);
        [SerializeField] private Vector2 playerHpBarSize = new Vector2(230f, 18f);
        [SerializeField] private Color playerHpBarBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color playerHpBarFillColor = new Color(0.3f, 0.9f, 0.35f, 1f);
        [SerializeField] private Image opponentHpBarFill;
        [SerializeField] private TMP_Text opponentHpBarText;
        [SerializeField] private Color opponentHpBarFillColor = new Color(1f, 0.32f, 0.24f, 1f);
        [SerializeField, Min(0f)] private float boardHpBarInnerInset = 18f;
        [SerializeField, Min(6f)] private float verticalBoardHpBarWidth = 18f;
        [SerializeField, Range(0.2f, 1f)] private float verticalBoardHpBarHeightFactor = 0.72f;
        [SerializeField] private bool hideLegacyPlayerHpText = true;

        [Header("Opponent Profile UI")]
        [SerializeField] private Image opponentBattleSpriteImage;
        [SerializeField] private BattleCharacterModelView opponentBattleModelView;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text opponentRankText;
        [SerializeField] private string opponentRankFormat = "{0} {1} RP";
        [SerializeField] private bool createOpponentProfileUiIfMissing = true;
        [SerializeField] private Vector2 opponentProfileUiSize = new Vector2(420f, 172f);
        [SerializeField] private Vector2 opponentProfileUiOffset = new Vector2(120f, -50f);
        [SerializeField] private Vector2 opponentBattleSpriteSize = new Vector2(156f, 156f);
        [SerializeField] private Vector2 opponentBattleSpriteOffset = new Vector2(12f, -8f);
        [SerializeField] private bool flipOpponentBattleSpriteX = true;

        [Header("Character Action Feedback")]
        [SerializeField, Min(0.01f)] private float characterActionPulseDuration = 0.18f;
        [SerializeField, Min(0f)] private float characterAttackPulseDistance = 18f;
        [SerializeField, Min(0f)] private float characterHitPulseDistance = 12f;

        [Header("Parry Choice UI")]
        [SerializeField] private BattleParryChoiceUI parryChoiceUi;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanelRoot;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultRewardText;
        [SerializeField] private TMP_Text resultExperienceText;
        [SerializeField] private Button resultBattleLobbyButton;
        [SerializeField] private Button resultNewMatchButton;
        [SerializeField] private bool createResultPanelIfMissing = true;
        [SerializeField] private string winResultText = "VICTORY";
        [SerializeField] private string failedResultText = "DEFEAT";
        [SerializeField] private string resultGoldFormat = "+{0} Gold";
        [SerializeField] private string resultNoGoldText = "+0 Gold";
        [SerializeField] private string resultExperienceFormat = "+{0} XP  Level {1}";
        [SerializeField] private string returnToBattleLobbyText = "Menu";
        [SerializeField] private string newMatchText = "New Match";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string battleGameSceneName = "GameMahjongBattle";
        [SerializeField, Min(0)] private int battleWinExperienceReward = 100;
        [SerializeField, Min(0)] private int battleLoseExperienceReward = 35;
        [SerializeField] private Vector2 resultPanelSize = new Vector2(620f, 380f);
        [SerializeField] private Color resultPanelBackgroundColor = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private Color winResultColor = new Color(0.3f, 1f, 0.38f, 1f);
        [SerializeField] private Color failedResultColor = new Color(1f, 0.25f, 0.2f, 1f);

        [Header("Flow")]
        [SerializeField, Min(1)] private int totalCombatRounds = 3;
        [SerializeField] private float nextRoundDelay = 1.25f;
        [SerializeField] private string roundPrefix = "Round ";
        [SerializeField] private string stateFight = "Fight";
        [SerializeField] private string statePlayerBoardCleared = "Player board cleared";
        [SerializeField] private string stateOpponentBoardCleared = "Opponent board cleared";
        [SerializeField] private string statePlayerBoardFailed = "Player board failed";
        [SerializeField] private string stateOpponentBoardFailed = "Opponent board failed";
        [SerializeField] private string stateRoundWin = "Round won";
        [SerializeField] private string stateRoundLose = "Round lost";
        [SerializeField] private string stateMatchWin = "You won the match";
        [SerializeField] private string stateMatchLose = "You lost the match";

        [Header("Countdown")]
        [SerializeField] private bool useStartCountdown = true;
        [SerializeField, Min(1)] private int countdownSeconds = 3;
        [SerializeField, Min(0.05f)] private float countdownInterval = 1f;
        [SerializeField, Min(0.05f)] private float startTextDuration = 0.45f;
        [SerializeField] private string countdownStartText = "Start";

        [Header("Layout")]
        [SerializeField] private BattleLayoutPresetService battleLayoutPresetService;
        [SerializeField] private bool loopLayouts = true;

        [Header("Tiles")]
        [SerializeField] private BattleTileStore battleStore;

        [Header("Fallback")]
        [SerializeField, Min(1)] private int fallbackTileRound = 1;
        [SerializeField, Min(1)] private int fallbackLayoutLevel = 1;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private int currentRoundIndex;
        private int playerLayoutIndex;
        private int opponentLayoutIndex;
        private int playerRoundWins;
        private int opponentRoundWins;

        private bool matchFinished;
        private bool matchRunning;
        private bool roundEnding;

        private bool playerBoardRebuilding;
        private bool opponentBoardRebuilding;
        private int lastResultGoldReward;
        private int lastResultExperienceReward;
        private int lastResultAccountLevel;

        private Coroutine matchStartRoutine;
        private Coroutine playerBoardRoutine;
        private Coroutine opponentBoardRoutine;
        private Coroutine roundTransitionRoutine;
        private Coroutine playerCharacterActionRoutine;
        private Coroutine opponentCharacterActionRoutine;
        private string opponentBattleCharacterId;
        private BattleBoardsHeightFirstLayout boardsHeightFirstLayout;
        private Vector2 lastPlayerBoardAreaSize;
        private Vector2 lastOpponentBoardAreaSize;
        private Vector2 lastBattleHudCanvasSize;

        public BattleBoard PlayerBoard => playerBoard;
        public BattleBoard OpponentBoard => opponentBoard;
        public BattleBotController BotController => botController;
        public BattleCombatSystem CombatSystem => combatSystem;
        public BattleTileStore BattleStore => battleStore;
        public BattleLayoutPresetService BattleLayoutPresetService => battleLayoutPresetService;

        public int CurrentRoundIndex => currentRoundIndex;
        public int CurrentRoundNumber => Mathf.Max(1, currentRoundIndex + 1);

        public int PlayerLayoutIndex => playerLayoutIndex;
        public int OpponentLayoutIndex => opponentLayoutIndex;

        public int PlayerLayoutNumber => Mathf.Max(1, playerLayoutIndex + 1);
        public int OpponentLayoutNumber => Mathf.Max(1, opponentLayoutIndex + 1);

        public bool IsMatchFinished => matchFinished;
        public bool IsMatchRunning => matchRunning;
        public bool IsPlayerBoardRebuilding => playerBoardRebuilding;
        public bool IsOpponentBoardRebuilding => opponentBoardRebuilding;

        private void Awake()
        {
            AutoResolveLinks();
            EnsureBattleBoardsLayout();
        }

        private void OnEnable()
        {
            AutoResolveLinks();
            EnsureBattleBoardsLayout();
            BindBoards();
            ProfileService.ProfileChanged += ApplyPlayerProfileUi;
            BindCharacterSelectionService();
        }

        private void OnDisable()
        {
            ProfileService.ProfileChanged -= ApplyPlayerProfileUi;
            UnbindCharacterSelectionService();
            UnbindBoards();
            UnbindCombatSystem();
            StopMatchStartRoutine();
            StopBoardRoutines();
            StopRoundTransitionRoutine();
            StopCharacterActionRoutines();
        }

        private void Start()
        {
            EnsureBattleBoardsLayout();
            EnsureBattleOpponentSession();
            EnsureOpponentBattleCharacter();
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            ApplyBattleProfileLayout();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshBattleHpBars();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
            SyncParryChoiceCharacterSprites();
            ApplyBattleProfileLayout();
            RefreshHud();
            StartMatch();
        }

        private void Update()
        {
            RefitBoardsWhenAreaSizeChanges();
            RefitBattleHudWhenCanvasSizeChanges();
        }

        public void StartMatch()
        {
            StopAllCoroutines();
            matchStartRoutine = null;
            playerBoardRoutine = null;
            opponentBoardRoutine = null;
            roundTransitionRoutine = null;
            playerCharacterActionRoutine = null;
            opponentCharacterActionRoutine = null;
            opponentBattleCharacterId = string.Empty;

            currentRoundIndex = 0;
            playerLayoutIndex = 0;
            opponentLayoutIndex = 0;
            playerRoundWins = 0;
            opponentRoundWins = 0;

            matchFinished = false;
            matchRunning = false;
            roundEnding = false;

            playerBoardRebuilding = false;
            opponentBoardRebuilding = false;

            EnsureBattleOpponentSession();
            EnsureOpponentBattleCharacter();
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();
            EnsureBattleBoardsLayout();
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            ApplyBattleProfileLayout();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshBattleHpBars();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
            SyncParryChoiceCharacterSprites();
            ApplyBattleProfileLayout();
            RefreshHud();
            NotifyStateChanged();

            matchStartRoutine = StartCoroutine(StartMatchRoutine());
        }

        public void RestartMatch()
        {
            StartMatch();
        }

        public void ForceFinishMatch(bool playerWon)
        {
            if (matchFinished)
                return;

            StopBoardRoutines();
            StopMatchStartRoutine();
            StopRoundTransitionRoutine();
            StopCharacterActionRoutines();

            matchFinished = true;
            matchRunning = false;
            playerBoardRebuilding = false;
            opponentBoardRebuilding = false;
            roundEnding = false;

            if (stateText != null)
                stateText.text = playerWon ? stateMatchWin : stateMatchLose;

            if (botController != null)
                botController.StopBot();

            if (IsOnlineRankedBattleActive())
                OnlineRankedBattleNetwork.I.SendMatchFinished();

            ApplyBattleMatchResult(playerWon);
            ShowResultPanel(playerWon);
            MatchFinished?.Invoke(this, playerWon);
            NotifyStateChanged();

            Log($"Match finished | PlayerWon={playerWon}");
        }

        private IEnumerator StartMatchRoutine()
        {
            if (useStartCountdown)
            {
                for (int i = Mathf.Max(1, countdownSeconds); i > 0; i--)
                {
                    if (stateText != null)
                        stateText.text = i.ToString();

                    NotifyStateChanged();
                    yield return new WaitForSeconds(Mathf.Max(0.05f, countdownInterval));
                }

                if (stateText != null)
                    stateText.text = countdownStartText;

                NotifyStateChanged();
                yield return new WaitForSeconds(Mathf.Max(0.05f, startTextDuration));
            }

            matchStartRoutine = null;

            if (matchFinished)
                yield break;

            BeginMatchAfterCountdown();
        }

        private void BeginMatchAfterCountdown()
        {
            matchRunning = true;
            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();

            if (combatSystem != null)
                combatSystem.StartCombat();

            RefreshHud();
            MatchStarted?.Invoke(this);
            NotifyStateChanged();

            Log("Match started");
            BuildInitialBoards();
        }

        public void SetBoards(BattleBoard player, BattleBoard opponent)
        {
            UnbindBoards();

            playerBoard = player;
            opponentBoard = opponent;
            ConfigureBoardInputOwnership();

            if (combatSystem != null)
                combatSystem.SetBoards(playerBoard, opponentBoard);

            BindBoards();
            NotifyStateChanged();
        }

        public void SetBotController(BattleBotController controller)
        {
            botController = controller;
            NotifyStateChanged();
        }

        public void SetCombatSystem(BattleCombatSystem combat)
        {
            UnbindCombatSystem();
            combatSystem = combat;

            if (combatSystem != null)
            {
                combatSystem.SetMatchController(this);
                combatSystem.SetBoards(playerBoard, opponentBoard);
                BindCombatSystem();
            }

            ConfigureBoardInputOwnership();
            NotifyStateChanged();
        }

        public void SetBattleStore(BattleTileStore store)
        {
            battleStore = store;
            NotifyStateChanged();
        }

        public void SetBattleLayoutPresetService(BattleLayoutPresetService service)
        {
            battleLayoutPresetService = service;
            NotifyStateChanged();
        }

        public void SetRoundUi(TMP_Text round, TMP_Text score, TMP_Text state)
        {
            roundText = round;
            scoreText = score;
            stateText = state;
            RefreshHud();
            NotifyStateChanged();
        }

        public string GetRoundText()
        {
            return $"{roundPrefix}{CurrentRoundNumber}/{TotalCombatRounds}";
        }

        public string GetScoreText()
        {
            if (combatSystem != null)
                return $"{combatSystem.PlayerHp} : {combatSystem.OpponentHp}";

            return "- : -";
        }

        public void BuildNextPlayerBoard()
        {
            if (!CanRebuildPlayerBoard())
                return;

            playerLayoutIndex = currentRoundIndex;

            BuildBoardForSide(playerBoard, playerLayoutIndex, restartBotAfterBuild: false);

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            NotifyStateChanged();

            Log($"Player board rebuilt in current round | LayoutIndex={playerLayoutIndex}");
        }

        public void BuildNextOpponentBoard()
        {
            if (!CanRebuildOpponentBoard())
                return;

            opponentLayoutIndex = currentRoundIndex;

            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: !IsRealtimeOpponentBattleActive());

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            NotifyStateChanged();

            Log($"Opponent board rebuilt in current round | LayoutIndex={opponentLayoutIndex}");
        }

        private void BuildInitialBoards()
        {
            playerLayoutIndex = currentRoundIndex;
            opponentLayoutIndex = currentRoundIndex;

            BuildBoardForSide(playerBoard, playerLayoutIndex, restartBotAfterBuild: false);
            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: !IsRealtimeOpponentBattleActive());

            if (stateText != null)
                stateText.text = stateFight;

            RefreshHud();
            RoundStarted?.Invoke(this, CurrentRoundNumber);
            NotifyStateChanged();

            Log($"Initial boards built | PlayerLayout={playerLayoutIndex} OpponentLayout={opponentLayoutIndex}");
        }

        private void BuildBoardForSide(BattleBoard board, int layoutIndex, bool restartBotAfterBuild)
        {
            if (board == null)
            {
                Debug.LogError("[BattleMatchController] Target board is null.");
                return;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log($"BuildBoardForSide skipped: combat finished | Side={board.Side}");
                return;
            }

            int requestedRoundNumber = Mathf.Max(1, layoutIndex + 1);
            int resolvedRoundNumber = requestedRoundNumber;
            int layoutLevel = ResolveLayoutLevel(layoutIndex);
            List<LayoutSlot> slots = ResolveBattleLayoutSlots(layoutLevel);

            if (slots == null || slots.Count == 0)
            {
                Debug.LogError($"[BattleMatchController] Battle layout is empty for level {layoutLevel}.");
                return;
            }

            IReadOnlyList<BattleTileData> source = ResolveBestTileSource(requestedRoundNumber, out resolvedRoundNumber);
            if (source == null || source.Count == 0)
            {
                Debug.LogError($"[BattleMatchController] Tile source is empty. RequestedRound={requestedRoundNumber}");
                return;
            }

            int seed = ResolveBattleBoardSeed(requestedRoundNumber);

            board.Clear();
            board.SetRoundData(resolvedRoundNumber, slots, seed, source);
            EnsureBattleBoardsLayout();
            board.Build();
            RefitBattleBoards();

            if (!board.IsBuilt)
            {
                Debug.LogError($"[BattleMatchController] Board build failed | Side={board.Side} | Round={resolvedRoundNumber}");
                return;
            }

            EnsureLocalWifiMatchSync();
            EnsureOnlineRankedMatchSync();

            if (restartBotAfterBuild && botController != null && !IsRealtimeOpponentBattleActive())
                botController.RestartBot();

            Log(
                $"Board built | Side={board.Side} | RequestedRound={requestedRoundNumber} | " +
                $"ResolvedRound={resolvedRoundNumber} | Layout={layoutLevel} | Seed={seed}");
        }

        private bool CanRebuildPlayerBoard()
        {
            return matchRunning &&
                   !matchFinished &&
                   !roundEnding &&
                   playerBoard != null &&
                   !playerBoardRebuilding &&
                   (combatSystem == null || !combatSystem.IsCombatFinished);
        }

        private int ResolveBattleBoardSeed(int roundNumber)
        {
            int safeRound = Mathf.Max(1, roundNumber);
            if ((MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch ||
                 MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch) &&
                MahjongSession.BattleMatchSeed > 0)
            {
                return MahjongSession.BattleMatchSeed + safeRound * 997;
            }

            return UnityEngine.Random.Range(100000, 999999);
        }

        private bool CanRebuildOpponentBoard()
        {
            return matchRunning &&
                   !matchFinished &&
                   !roundEnding &&
                   opponentBoard != null &&
                   !opponentBoardRebuilding &&
                   (combatSystem == null || !combatSystem.IsCombatFinished);
        }

        private void BindBoards()
        {
            if (playerBoard != null)
            {
                playerBoard.Cleared -= HandleBoardCleared;
                playerBoard.Failed -= HandleBoardFailed;
                playerBoard.Cleared += HandleBoardCleared;
                playerBoard.Failed += HandleBoardFailed;
            }

            if (opponentBoard != null)
            {
                opponentBoard.Cleared -= HandleBoardCleared;
                opponentBoard.Failed -= HandleBoardFailed;
                opponentBoard.Cleared += HandleBoardCleared;
                opponentBoard.Failed += HandleBoardFailed;
            }
        }

        private void UnbindBoards()
        {
            if (playerBoard != null)
            {
                playerBoard.Cleared -= HandleBoardCleared;
                playerBoard.Failed -= HandleBoardFailed;
            }

            if (opponentBoard != null)
            {
                opponentBoard.Cleared -= HandleBoardCleared;
                opponentBoard.Failed -= HandleBoardFailed;
            }
        }

        private List<LayoutSlot> ResolveBattleLayoutSlots(int layoutLevel)
        {
            if (battleLayoutPresetService == null)
            {
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();
            }

            List<LayoutSlot> slots = null;

            if (battleLayoutPresetService != null)
                slots = battleLayoutPresetService.GetLevel(layoutLevel);

            if (slots == null || slots.Count == 0)
                slots = BattleLayoutPresets.GetByLevel(layoutLevel);

            return slots != null ? new List<LayoutSlot>(slots) : null;
        }

        private IReadOnlyList<BattleTileData> ResolveBestTileSource(int requestedRoundNumber, out int resolvedRoundNumber)
        {
            resolvedRoundNumber = Mathf.Max(1, requestedRoundNumber);

            if (battleStore == null)
                return null;

            IReadOnlyList<BattleTileData> source = battleStore.GetTilesForRound(resolvedRoundNumber);
            if (HasTileSource(source))
                return source;

            for (int round = resolvedRoundNumber - 1; round >= 1; round--)
            {
                source = battleStore.GetTilesForRound(round);
                if (HasTileSource(source))
                {
                    resolvedRoundNumber = round;
                    Log($"Fallback tile source used | RequestedRound={requestedRoundNumber} -> Round={round}");
                    return source;
                }
            }

            resolvedRoundNumber = Mathf.Max(1, fallbackTileRound);
            source = battleStore.GetTilesForRound(resolvedRoundNumber);
            return HasTileSource(source) ? source : null;
        }

        private bool HasTileSource(IReadOnlyList<BattleTileData> source)
        {
            return source != null && source.Count > 0;
        }

        private int ResolveLayoutLevel(int layoutIndex)
        {
            if (battleLayoutPresetService != null)
            {
                int min = battleLayoutPresetService.MinLevel;
                int max = battleLayoutPresetService.MaxLevel;
                int count = Mathf.Max(1, max - min + 1);

                if (loopLayouts)
                    return min + (Mathf.Abs(layoutIndex) % count);

                return Mathf.Clamp(min + layoutIndex, min, max);
            }

            return Mathf.Max(1, fallbackLayoutLevel);
        }

        private void HandleBoardCleared(BattleBoard board)
        {
            if (matchFinished || board == null)
                return;

            if (stateText != null)
            {
                if (board == playerBoard)
                    stateText.text = statePlayerBoardCleared;
                else if (board == opponentBoard)
                    stateText.text = stateOpponentBoardCleared;
            }

            RefreshHud();
            NotifyStateChanged();

            Log($"Board cleared inside round | Side={board.Side}");

            if (board == playerBoard)
            {
                if (playerBoardRoutine != null)
                    StopCoroutine(playerBoardRoutine);

                playerBoardRoutine = StartCoroutine(RebuildPlayerBoardRoutine());
            }
            else if (board == opponentBoard)
            {
                if (opponentBoardRoutine != null)
                    StopCoroutine(opponentBoardRoutine);

                opponentBoardRoutine = StartCoroutine(RebuildOpponentBoardRoutine());
            }
        }

        private void HandleDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int damage, int hpAfter)
        {
            if (matchFinished || damage <= 0)
                return;

            if (targetSide == BattleBoardSide.Opponent)
            {
                PlayBattleCharacterAction(playerBattleModelView, playerBattleSpriteImage, true, true);
            }
            else if (targetSide == BattleBoardSide.Player)
            {
                PlayBattleCharacterAction(opponentBattleModelView, opponentBattleSpriteImage, true, false);
            }

            RefreshHud();
            NotifyStateChanged();
        }

        private void PlayBattleCharacterAction(
            BattleCharacterModelView modelView,
            Image spriteImage,
            bool isAttack,
            bool isPlayerSide)
        {
            bool playedModelAnimation = false;
            if (modelView != null)
            {
                playedModelAnimation = isAttack
                    ? modelView.PlayAttackAnimation()
                    : modelView.PlayHitAnimation();
            }

            if (!playedModelAnimation)
                PlaySpriteActionPulse(spriteImage, isAttack, isPlayerSide);
        }

        private void PlaySpriteActionPulse(Image spriteImage, bool isAttack, bool isPlayerSide)
        {
            if (spriteImage == null || spriteImage.rectTransform == null || !spriteImage.gameObject.activeInHierarchy)
                return;

            Coroutine routine = isPlayerSide ? playerCharacterActionRoutine : opponentCharacterActionRoutine;
            if (routine != null)
                StopCoroutine(routine);

            float distance = isAttack ? characterAttackPulseDistance : characterHitPulseDistance;
            float direction = isPlayerSide
                ? (isAttack ? 1f : -1f)
                : (isAttack ? -1f : 1f);

            routine = StartCoroutine(SpriteActionPulseRoutine(spriteImage.rectTransform, direction * distance, isPlayerSide));

            if (isPlayerSide)
                playerCharacterActionRoutine = routine;
            else
                opponentCharacterActionRoutine = routine;
        }

        private IEnumerator SpriteActionPulseRoutine(RectTransform rect, float localX, bool isPlayerSide)
        {
            Vector2 start = rect.anchoredPosition;
            Vector2 peak = start + new Vector2(localX, 0f);
            float halfDuration = Mathf.Max(0.01f, characterActionPulseDuration * 0.5f);

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (rect == null)
                    yield break;

                rect.anchoredPosition = Vector2.Lerp(start, peak, t / halfDuration);
                yield return null;
            }

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (rect == null)
                    yield break;

                rect.anchoredPosition = Vector2.Lerp(peak, start, t / halfDuration);
                yield return null;
            }

            if (rect != null)
                rect.anchoredPosition = start;

            if (isPlayerSide)
                playerCharacterActionRoutine = null;
            else
                opponentCharacterActionRoutine = null;
        }

        private void HandleBoardFailed(BattleBoard board)
        {
            if (matchFinished || board == null)
                return;

            if (stateText != null)
            {
                if (board == playerBoard)
                    stateText.text = statePlayerBoardFailed;
                else if (board == opponentBoard)
                    stateText.text = stateOpponentBoardFailed;
            }

            RefreshHud();
            NotifyStateChanged();

            Log($"Board failed inside round | Side={board.Side}");

            if (board == playerBoard)
            {
                if (playerBoardRoutine != null)
                    StopCoroutine(playerBoardRoutine);

                playerBoardRoutine = StartCoroutine(RebuildPlayerBoardRoutine());
            }
            else if (board == opponentBoard)
            {
                if (opponentBoardRoutine != null)
                    StopCoroutine(opponentBoardRoutine);

                opponentBoardRoutine = StartCoroutine(RebuildOpponentBoardRoutine());
            }
        }

        private IEnumerator RebuildPlayerBoardRoutine()
        {
            playerBoardRebuilding = true;
            NotifyStateChanged();

            yield return new WaitForSeconds(Mathf.Max(0.05f, nextRoundDelay));

            playerBoardRoutine = null;

            if (matchFinished || !matchRunning || roundEnding)
            {
                playerBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log("RebuildPlayerBoardRoutine stopped: combat finished");
                playerBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            playerBoardRebuilding = false;
            NotifyStateChanged();

            BuildNextPlayerBoard();
        }

        private IEnumerator RebuildOpponentBoardRoutine()
        {
            opponentBoardRebuilding = true;
            NotifyStateChanged();

            yield return new WaitForSeconds(Mathf.Max(0.05f, nextRoundDelay));

            opponentBoardRoutine = null;

            if (matchFinished || !matchRunning || roundEnding)
            {
                opponentBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            if (combatSystem != null && combatSystem.IsCombatFinished)
            {
                Log("RebuildOpponentBoardRoutine stopped: combat finished");
                opponentBoardRebuilding = false;
                NotifyStateChanged();
                yield break;
            }

            opponentBoardRebuilding = false;
            NotifyStateChanged();

            BuildNextOpponentBoard();
        }

        private void StopBoardRoutines()
        {
            if (playerBoardRoutine != null)
            {
                StopCoroutine(playerBoardRoutine);
                playerBoardRoutine = null;
            }

            if (opponentBoardRoutine != null)
            {
                StopCoroutine(opponentBoardRoutine);
                opponentBoardRoutine = null;
            }
        }

        private void StopRoundTransitionRoutine()
        {
            if (roundTransitionRoutine == null)
                return;

            StopCoroutine(roundTransitionRoutine);
            roundTransitionRoutine = null;
        }

        private void StopCharacterActionRoutines()
        {
            if (playerCharacterActionRoutine != null)
            {
                StopCoroutine(playerCharacterActionRoutine);
                playerCharacterActionRoutine = null;
            }

            if (opponentCharacterActionRoutine != null)
            {
                StopCoroutine(opponentCharacterActionRoutine);
                opponentCharacterActionRoutine = null;
            }
        }

        private void StopMatchStartRoutine()
        {
            if (matchStartRoutine == null)
                return;

            StopCoroutine(matchStartRoutine);
            matchStartRoutine = null;
        }

        private void RefreshHud()
        {
            if (roundText != null)
                roundText.text = GetRoundText();

            if (scoreText != null)
                scoreText.text = GetScoreText();

            RefreshBattleHpBars();
        }

        private void EnsureBattleOpponentSession()
        {
            bool needsOpponent = MahjongSession.LaunchMode != MahjongLaunchMode.Battle ||
                                 string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName);

            if (!needsOpponent)
                return;

            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            MahjongBattleLobbyMode mode = MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.None
                ? MahjongBattleLobbyMode.RandomMatch
                : MahjongBattleLobbySession.SelectedMode;

            MahjongBattleOpponentData opponent = botService.CreateOpponent(mode, ResolvePlayerBattleRankPoints());
            MahjongSession.StartBattle(opponent);

            Log($"Generated fallback bot profile | {opponent.DisplayName} | {opponent.RankTier} {opponent.RankPoints}");
        }

        private void EnsureOpponentBattleCharacter()
        {
            if (!string.IsNullOrWhiteSpace(opponentBattleCharacterId))
                return;

            if (TryApplyLocalWifiOpponentCharacter())
                return;

            BattleCharacterDatabase database = BattleCharacterDatabase.HasInstance
                ? BattleCharacterDatabase.Instance
                : FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);

            if (database == null)
                return;

            database.RebuildCache();
            List<BattleCharacterDatabase.BattleCharacterData> characters = database.GetEnabledCharacters();
            if (characters == null || characters.Count == 0)
                return;

            int index = Mathf.Abs(MahjongSession.BattleMatchSeed);
            if (index <= 0)
                index = UnityEngine.Random.Range(0, int.MaxValue);

            BattleCharacterDatabase.BattleCharacterData selected = characters[index % characters.Count];
            if (selected == null || string.IsNullOrWhiteSpace(selected.Id))
                return;

            opponentBattleCharacterId = selected.Id;
            Log($"Opponent battle character selected | {opponentBattleCharacterId}");
        }

        private bool TryApplyLocalWifiOpponentCharacter()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.LocalWifiMatch ||
                LocalWifiBattleNetwork.I == null ||
                LocalWifiBattleNetwork.I.RemotePlayer == null)
            {
                return false;
            }

            string characterId = LocalWifiBattleNetwork.I.RemotePlayer.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (ResolveBattleCharacter(characterId) == null)
                return false;

            opponentBattleCharacterId = characterId.Trim();
            Log($"Local Wi-Fi opponent battle character applied | {opponentBattleCharacterId}");
            return true;
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

        private void AutoResolveOpponentProfileUi()
        {
            if (hideLegacyPlayerHpText)
                HideLegacyHpTexts();

            if (opponentBattleSpriteImage == null)
            {
                opponentBattleSpriteImage = FindImageByObjectName("OpponentBattleSprite");
                if (opponentBattleSpriteImage == null)
                    opponentBattleSpriteImage = FindImageByObjectName("OpponentCharacterBattleSprite");
                if (opponentBattleSpriteImage == null)
                    opponentBattleSpriteImage = FindImageByObjectName("OpponentProfileSprite");
            }

            if (opponentNameText == null)
                opponentNameText = FindTextByObjectName("OpponentName");

            if (opponentRankText == null)
            {
                opponentRankText = FindTextByObjectName("OpponentRank");
                if (opponentRankText == null)
                    opponentRankText = FindTextByObjectName("OpponentRankText");
                if (opponentRankText == null)
                    opponentRankText = FindTextByObjectName("OpponentRankTier");
            }

            if (opponentHpBarFill == null)
            {
                opponentHpBarFill = FindImageByObjectName("OpponentHpBarFill");
                if (opponentHpBarFill == null)
                    opponentHpBarFill = FindImageByObjectName("OpponentHPBarFill");
            }

            if (opponentHpBarText == null)
            {
                opponentHpBarText = FindTextByObjectName("OpponentHpBarText");
                if (opponentHpBarText == null)
                    opponentHpBarText = FindTextByObjectName("OpponentHPBarText");
            }

            if (opponentHpBarFill == null || opponentHpBarText == null)
            {
                Transform hpParent = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText);
                if (hpParent != null)
                    CreateOpponentHpBar(hpParent);
            }

            if (createOpponentProfileUiIfMissing &&
                (opponentBattleSpriteImage == null ||
                 opponentNameText == null ||
                 opponentRankText == null))
            {
                CreateOpponentProfileUi();
            }
        }

        private void AutoResolvePlayerProfileUi()
        {
            if (hideLegacyPlayerHpText)
                HideLegacyHpTexts();

            if (playerBattleSpriteImage == null)
            {
                playerBattleSpriteImage = FindImageByObjectName("PlayerBattleSprite");
                if (playerBattleSpriteImage == null)
                    playerBattleSpriteImage = FindImageByObjectName("PlayerCharacterBattleSprite");
                if (playerBattleSpriteImage == null)
                    playerBattleSpriteImage = FindImageByObjectName("PlayerProfileSprite");
            }

            if (playerNameText == null)
            {
                playerNameText = FindTextByObjectName("PlayerName");
                if (playerNameText == null)
                    playerNameText = FindTextByObjectName("PlayerNameText");
            }

            if (playerRankText == null)
            {
                playerRankText = FindTextByObjectName("PlayerRank");
                if (playerRankText == null)
                    playerRankText = FindTextByObjectName("PlayerRankText");
                if (playerRankText == null)
                    playerRankText = FindTextByObjectName("PlayerRankTier");
            }

            if (playerTitleText == null)
            {
                playerTitleText = FindTextByObjectName("PlayerTitle");
                if (playerTitleText == null)
                    playerTitleText = FindTextByObjectName("PlayerTitleText");
            }

            if (playerHpBarFill == null)
            {
                playerHpBarFill = FindImageByObjectName("PlayerHpBarFill");
                if (playerHpBarFill == null)
                    playerHpBarFill = FindImageByObjectName("PlayerHPBarFill");
            }

            if (playerHpBarText == null)
            {
                playerHpBarText = FindTextByObjectName("PlayerHpBarText");
                if (playerHpBarText == null)
                    playerHpBarText = FindTextByObjectName("PlayerHPBarText");
            }

            if (playerHpBarFill == null || playerHpBarText == null)
            {
                Transform hpParent = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerRankText);
                if (hpParent != null)
                    CreatePlayerHpBar(hpParent);
            }

            if (createPlayerProfileUiIfMissing &&
                (playerBattleSpriteImage == null ||
                 playerNameText == null ||
                 playerRankText == null ||
                 playerTitleText == null))
            {
                CreatePlayerProfileUi();
            }
        }

        private void AutoResolveResultPanelUi()
        {
            if (resultPanelRoot == null)
            {
                GameObject foundRoot = GameObject.Find("BattleResultPanel");
                if (foundRoot == null)
                    foundRoot = GameObject.Find("ResultPanel");
                if (foundRoot == null)
                    foundRoot = GameObject.Find("BattleEndPanel");

                resultPanelRoot = foundRoot;
            }

            if (resultTitleText == null)
            {
                resultTitleText = FindTextByObjectName("BattleResultTitle");
                if (resultTitleText == null)
                    resultTitleText = FindTextByObjectName("ResultTitle");
                if (resultTitleText == null)
                    resultTitleText = FindTextByObjectName("ResultTitleText");
            }

            if (resultRewardText == null)
            {
                resultRewardText = FindTextByObjectName("BattleResultReward");
                if (resultRewardText == null)
                    resultRewardText = FindTextByObjectName("ResultRewardText");
            }

            if (resultExperienceText == null)
            {
                resultExperienceText = FindTextByObjectName("BattleResultExperience");
                if (resultExperienceText == null)
                    resultExperienceText = FindTextByObjectName("ResultExperienceText");
            }

            if (resultBattleLobbyButton == null)
            {
                resultBattleLobbyButton = FindButtonByObjectName("BattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("ReturnBattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("BackToBattleLobbyButton");
            }

            if (resultNewMatchButton == null)
            {
                resultNewMatchButton = FindButtonByObjectName("BattleNewMatchButton");
                if (resultNewMatchButton == null)
                    resultNewMatchButton = FindButtonByObjectName("NewMatchButton");
                if (resultNewMatchButton == null)
                    resultNewMatchButton = FindButtonByObjectName("FindNewMatchButton");
            }

            if (createResultPanelIfMissing &&
                (resultPanelRoot == null ||
                 resultTitleText == null ||
                 resultRewardText == null ||
                 resultExperienceText == null ||
                 resultBattleLobbyButton == null ||
                 resultNewMatchButton == null))
            {
                CreateResultPanelUi();
            }

            BindResultPanelButton();
        }

        private TMP_Text FindTextByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.gameObject.name == objectName)
                    return text;
            }

            return null;
        }

        private Image FindImageByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.gameObject.name == objectName)
                    return image;
            }

            return null;
        }

        private Button FindButtonByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.gameObject.name == objectName)
                    return button;
            }

            return null;
        }

        private void ApplyOpponentProfileUi()
        {
            string opponentName = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName)
                ? "Opponent"
                : MahjongSession.BattleOpponentName;

            string rankTier = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentRankTier)
                ? "Unranked"
                : MahjongSession.BattleOpponentRankTier;

            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            string rankText = string.Format(opponentRankFormat, rankTier, rankPoints);

            if (opponentNameText != null)
                opponentNameText.text = opponentRankText == null ? $"{opponentName} [{rankText}]" : opponentName;

            if (opponentRankText != null)
                opponentRankText.text = rankText;

            BattleHudUI hud = FindAnyObjectByType<BattleHudUI>(FindObjectsInactive.Include);
            if (hud != null)
                hud.Refresh();
        }

        private void ApplyOpponentBattleSpriteUi()
        {
            if (opponentBattleSpriteImage == null)
                return;

            BattleCharacterDatabase.BattleCharacterData data = ResolveBattleCharacter(opponentBattleCharacterId);
            if (ApplyBattleModel(data, ref opponentBattleModelView, opponentBattleSpriteImage, true))
            {
                SyncParryChoiceCharacterSprites();
                return;
            }

            Sprite battleSprite = ResolveBattleSprite(opponentBattleCharacterId);
            opponentBattleSpriteImage.sprite = battleSprite;
            opponentBattleSpriteImage.enabled = battleSprite != null;
            opponentBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                opponentBattleSpriteImage,
                opponentBattleSpriteOffset,
                opponentBattleSpriteSize,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));
            ApplyImageFlip(opponentBattleSpriteImage, flipOpponentBattleSpriteX);
            SyncParryChoiceCharacterSprites();
        }

        private void ApplyPlayerProfileUi()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string playerName = fallbackPlayerName;
            string title = string.Empty;
            string rankTier = fallbackPlayerRankTier;
            int rankPoints = 0;

            if (profile != null)
            {
                profile.EnsureData();

                if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                    playerName = profile.DisplayName.Trim();

                title = MahjongTitleService.I != null
                    ? MahjongTitleService.I.GetProfileDisplayTitle(profile)
                    : ResolveSelectedMahjongTitleFallback(profile);

                if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                {
                    if (!string.IsNullOrWhiteSpace(profile.Mahjong.Battle.RankTier))
                        rankTier = profile.Mahjong.Battle.RankTier.Trim();

                    rankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
                }
            }

            if (playerNameText != null)
                playerNameText.text = playerRankText == null && playerTitleText == null
                    ? $"{playerName} [{string.Format(playerRankFormat, rankTier, rankPoints)}]"
                    : playerName;

            if (playerTitleText != null)
            {
                playerTitleText.text = string.IsNullOrWhiteSpace(title)
                    ? GameLocalization.Text("common.title_empty")
                    : GameLocalization.Format("profile.title", title);
            }

            if (playerRankText != null)
                playerRankText.text = string.Format(playerRankFormat, rankTier, rankPoints);
        }

        private static string ResolveSelectedMahjongTitleFallback(PlayerProfile profile)
        {
            if (profile == null || profile.Mahjong == null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(profile.Mahjong.SelectedTitleId)
                ? string.Empty
                : profile.Mahjong.SelectedTitleId.Trim();
        }

        private void RefreshPlayerHpBar()
        {
            int hp = ResolveDisplayPlayerHp();
            int maxHp = combatSystem != null ? combatSystem.MaxPlayerHp : 0;
            RefreshPlayerHpBar(hp, maxHp);
        }

        private void RefreshOpponentHpBar()
        {
            int hp = ResolveDisplayOpponentHp();
            int maxHp = combatSystem != null ? combatSystem.MaxOpponentHp : 0;
            RefreshOpponentHpBar(hp, maxHp);
        }

        private void RefreshBattleHpBars()
        {
            RefreshPlayerHpBar();
            RefreshOpponentHpBar();
        }

        private float ResolvePlayerHpNormalized()
        {
            return combatSystem != null && combatSystem.MaxPlayerHp > 0
                ? Mathf.Clamp01((float)Mathf.Max(0, ResolveDisplayPlayerHp()) / combatSystem.MaxPlayerHp)
                : 0f;
        }

        private float ResolveOpponentHpNormalized()
        {
            return combatSystem != null && combatSystem.MaxOpponentHp > 0
                ? Mathf.Clamp01((float)Mathf.Max(0, ResolveDisplayOpponentHp()) / combatSystem.MaxOpponentHp)
                : 0f;
        }

        private int ResolveDisplayPlayerHp()
        {
            if (combatSystem == null)
                return 0;

            return combatSystem.IsCombatStarted
                ? combatSystem.PlayerHp
                : combatSystem.MaxPlayerHp;
        }

        private int ResolveDisplayOpponentHp()
        {
            if (combatSystem == null)
                return 0;

            return combatSystem.IsCombatStarted
                ? combatSystem.OpponentHp
                : combatSystem.MaxOpponentHp;
        }

        private void RefreshPlayerHpBar(int hp, int maxHp)
        {
            if (playerHpBarFill != null)
            {
                float value = maxHp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, hp) / maxHp) : 0f;
                ApplyHpBarFillValue(playerHpBarFill, playerHpBarFillColor, value);
            }

            if (playerHpBarText != null)
                playerHpBarText.text = maxHp > 0 ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP";
        }

        private void RefreshOpponentHpBar(int hp, int maxHp)
        {
            if (opponentHpBarFill != null)
            {
                float value = maxHp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, hp) / maxHp) : 0f;
                ApplyHpBarFillValue(opponentHpBarFill, opponentHpBarFillColor, value);
            }

            if (opponentHpBarText != null)
                opponentHpBarText.text = maxHp > 0 ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP";
        }

        private static void ApplyHpBarFillValue(Image fill, Color color, float value)
        {
            if (fill == null)
                return;

            fill.enabled = true;
            fill.gameObject.SetActive(true);
            fill.raycastTarget = false;
            fill.color = color;
            fill.type = Image.Type.Simple;

            RectTransform rect = fill.rectTransform;
            RectTransform parent = fill.transform.parent as RectTransform;
            bool vertical = parent != null && parent.rect.height > parent.rect.width * 1.2f;

            if (rect == null)
                return;

            value = Mathf.Clamp01(value);
            if (vertical)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(1f, value);
                rect.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(value, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
            }

            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HideLegacyHpTexts()
        {
            TMP_Text legacy = FindTextByObjectName("PlayerHPText");
            if (legacy == null)
                legacy = FindTextByObjectName("PlayerHpText");

            if (legacy != null)
                legacy.gameObject.SetActive(false);

            legacy = FindTextByObjectName("OpponentHPText");
            if (legacy == null)
                legacy = FindTextByObjectName("OpponentHpText");

            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }

        private void ApplyPlayerBattleSpriteUi()
        {
            if (playerBattleSpriteImage == null)
                return;

            BattleCharacterDatabase.BattleCharacterData data = ResolveSelectedBattleCharacter();
            if (ApplyBattleModel(data, ref playerBattleModelView, playerBattleSpriteImage, false))
            {
                SyncParryChoiceCharacterSprites();
                return;
            }

            Sprite battleSprite = ResolveSelectedBattleSprite();
            playerBattleSpriteImage.sprite = battleSprite;
            playerBattleSpriteImage.enabled = battleSprite != null;
            playerBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                playerBattleSpriteImage,
                playerBattleSpriteOffset,
                playerBattleSpriteSize,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));
            ApplyImageFlip(playerBattleSpriteImage, false);
            SyncParryChoiceCharacterSprites();
        }

        private void SyncParryChoiceCharacterSprites()
        {
            if (parryChoiceUi == null)
                parryChoiceUi = FindAnyObjectByType<BattleParryChoiceUI>(FindObjectsInactive.Include);

            if (parryChoiceUi == null)
                return;

            parryChoiceUi.SetCharacters(
                ResolveSelectedBattleCharacter(),
                ResolveBattleCharacter(opponentBattleCharacterId),
                flipOpponentBattleSpriteX);
        }

        private bool ApplyBattleModel(
            BattleCharacterDatabase.BattleCharacterData data,
            ref BattleCharacterModelView modelView,
            Image anchorImage,
            bool flipX)
        {
            if (anchorImage == null || data == null)
            {
                if (modelView != null)
                    modelView.Hide();

                return false;
            }

            if (modelView == null)
                modelView = anchorImage.GetComponent<BattleCharacterModelView>();

            if (modelView == null)
                modelView = anchorImage.gameObject.AddComponent<BattleCharacterModelView>();

            bool shown = modelView.Show(data, BattleCharacterModelView.ModelContext.Battle, flipX);
            if (shown)
            {
                anchorImage.enabled = false;
                anchorImage.raycastTarget = false;
            }

            return shown;
        }

        private BattleCharacterDatabase.BattleCharacterData ResolveSelectedBattleCharacter()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            return BattleCharacterSelectionService.Instance.GetSelectedCharacter();
        }

        private BattleCharacterDatabase.BattleCharacterData ResolveBattleCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !BattleCharacterDatabase.HasInstance)
                return null;

            return BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId);
        }

        private Sprite ResolveSelectedBattleSprite()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            BattleCharacterDatabase.BattleCharacterData selected =
                BattleCharacterSelectionService.Instance.GetSelectedCharacter();

            if (selected == null)
                return null;

            if (selected.BattleSprite != null)
                return selected.BattleSprite;

            if (selected.LobbySprite != null)
                return selected.LobbySprite;

            return selected.SelectSprite;
        }

        private Sprite ResolveBattleSprite(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !BattleCharacterDatabase.HasInstance)
                return null;

            BattleCharacterDatabase.BattleCharacterData data =
                BattleCharacterDatabase.Instance.GetCharacterOrNull(characterId);

            if (data == null)
                return null;

            if (data.BattleSprite != null)
                return data.BattleSprite;

            if (data.LobbySprite != null)
                return data.LobbySprite;

            return data.SelectSprite;
        }

        private void CreatePlayerProfileUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            Transform rootTransform = playerNameText != null && playerNameText.transform.parent != null
                ? playerNameText.transform.parent
                : null;

            GameObject root = rootTransform != null
                ? rootTransform.gameObject
                : new GameObject("PlayerProfileHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            root.layer = canvas.gameObject.layer;

            if (rootTransform == null)
                root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect != null && rootTransform == null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = playerProfileUiOffset;
                rect.sizeDelta = playerProfileUiSize;
            }

            Image background = root.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0f, 0f, 0f, 0.42f);
                background.raycastTarget = false;
            }

            if (playerBattleSpriteImage == null)
            {
                playerBattleSpriteImage = CreateProfileImage(
                    root.transform,
                    "PlayerBattleSprite",
                    playerBattleSpriteOffset,
                    playerBattleSpriteSize,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
            }

            if (playerNameText == null)
                playerNameText = CreateProfileText(root.transform, "PlayerName", 30f, new Vector2(20f, -18f), 1);

            if (playerTitleText == null)
                playerTitleText = CreateProfileText(root.transform, "PlayerTitle", 18f, new Vector2(20f, -54f), 2);

            if (playerRankText == null)
                playerRankText = CreateProfileText(root.transform, "PlayerRank", 20f, new Vector2(20f, -82f), 3);

            if (playerHpBarFill == null || playerHpBarText == null)
                CreatePlayerHpBar(root.transform);
        }

        private void CreateOpponentProfileUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            GameObject root = new GameObject("OpponentProfileHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.layer = canvas.gameObject.layer;
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = opponentProfileUiOffset;
            rect.sizeDelta = opponentProfileUiSize;

            Image background = root.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.42f);
            background.raycastTarget = false;

            if (opponentBattleSpriteImage == null)
            {
                opponentBattleSpriteImage = CreateProfileImage(
                    root.transform,
                    "OpponentBattleSprite",
                    opponentBattleSpriteOffset,
                    opponentBattleSpriteSize,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));
            }

            if (opponentNameText == null)
                opponentNameText = CreateProfileText(root.transform, "OpponentName", 30f, new Vector2(20f, -18f), 1);

            if (opponentRankText == null)
                opponentRankText = CreateProfileText(root.transform, "OpponentRank", 22f, new Vector2(20f, -58f), 2);

            if (opponentHpBarFill == null || opponentHpBarText == null)
                CreateOpponentHpBar(root.transform);
        }

        private Image CreateProfileImage(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            imageObject.transform.SetSiblingIndex(0);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;

            return image;
        }

        private void CreatePlayerHpBar(Transform parent)
        {
            if (playerHpBarFill != null)
            {
                if (playerHpBarText == null)
                {
                    playerHpBarText = CreateProfileText(parent, "PlayerHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                    playerHpBarText.alignment = TextAlignmentOptions.Center;
                    playerHpBarText.rectTransform.sizeDelta = playerHpBarSize;
                }

                RefreshPlayerHpBar();
                return;
            }

            GameObject barRoot = new GameObject("PlayerHpBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barRoot.layer = parent.gameObject.layer;
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.SetSiblingIndex(3);

            RectTransform rootRect = barRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = playerHpBarOffset;
            rootRect.sizeDelta = playerHpBarSize;

            Image background = barRoot.GetComponent<Image>();
            background.color = playerHpBarBackgroundColor;
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("PlayerHpBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = parent.gameObject.layer;
            fillObject.transform.SetParent(barRoot.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            playerHpBarFill = fillObject.GetComponent<Image>();
            playerHpBarFill.color = playerHpBarFillColor;
            playerHpBarFill.raycastTarget = false;
            playerHpBarFill.type = Image.Type.Simple;

            if (playerHpBarText == null)
            {
                playerHpBarText = CreateProfileText(parent, "PlayerHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                playerHpBarText.alignment = TextAlignmentOptions.Center;
                playerHpBarText.rectTransform.sizeDelta = playerHpBarSize;
            }

            RefreshPlayerHpBar();
        }

        private void CreateOpponentHpBar(Transform parent)
        {
            if (opponentHpBarFill != null)
            {
                if (opponentHpBarText == null)
                {
                    opponentHpBarText = CreateProfileText(parent, "OpponentHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                    opponentHpBarText.alignment = TextAlignmentOptions.Center;
                    opponentHpBarText.rectTransform.sizeDelta = playerHpBarSize;
                }

                RefreshOpponentHpBar();
                return;
            }

            GameObject barRoot = new GameObject("OpponentHpBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barRoot.layer = parent.gameObject.layer;
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.SetSiblingIndex(3);

            RectTransform rootRect = barRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = playerHpBarOffset;
            rootRect.sizeDelta = playerHpBarSize;

            Image background = barRoot.GetComponent<Image>();
            background.color = playerHpBarBackgroundColor;
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("OpponentHpBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = parent.gameObject.layer;
            fillObject.transform.SetParent(barRoot.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            opponentHpBarFill = fillObject.GetComponent<Image>();
            opponentHpBarFill.color = opponentHpBarFillColor;
            opponentHpBarFill.raycastTarget = false;
            opponentHpBarFill.type = Image.Type.Simple;

            if (opponentHpBarText == null)
            {
                opponentHpBarText = CreateProfileText(parent, "OpponentHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                opponentHpBarText.alignment = TextAlignmentOptions.Center;
                opponentHpBarText.rectTransform.sizeDelta = playerHpBarSize;
            }

            RefreshOpponentHpBar();
        }

        private void ApplyBattleProfileLayout()
        {
            Transform playerRoot = ResolveProfileRoot(playerBattleSpriteImage, playerNameText, playerTitleText, playerRankText, playerHpBarFill, playerHpBarText);
            Transform opponentRoot = ResolveProfileRoot(opponentBattleSpriteImage, opponentNameText, opponentRankText);
            Vector2 canvasSize = ResolveBattleHudCanvasSize();
            lastBattleHudCanvasSize = canvasSize;

            float width = Mathf.Max(1f, canvasSize.x);
            float height = Mathf.Max(1f, canvasSize.y);
            bool compact = width < 900f || height < 720f;
            bool narrow = width < 640f;

            float topSpaceHeight = ResolveFreeHeightAboveBoards(canvasSize);
            float panelGap = compact ? 22f : 30f;
            float horizontalMargin = compact ? 12f : 28f;
            float portraitMaxByWidth = Mathf.Clamp(width * (narrow ? 0.24f : 0.16f), compact ? 150f : 178f, compact ? 210f : 250f);
            float portraitSize = Mathf.Clamp(topSpaceHeight * 0.68f, compact ? 122f : 148f, portraitMaxByWidth);
            float fighterGap = Mathf.Clamp(portraitSize * 0.82f, compact ? 92f : 112f, compact ? 150f : 178f);
            float availablePanelWidth = (width - fighterGap - portraitSize - panelGap * 2f) * 0.5f - horizontalMargin;
            float profileWidth = Mathf.Clamp(availablePanelWidth, narrow ? 150f : 220f, compact ? 300f : 360f);
            float profileHeight = Mathf.Max(portraitSize + 18f, compact ? 132f : 158f);
            float rootTop = -Mathf.Clamp(topSpaceHeight * 0.12f, compact ? 18f : 22f, compact ? 34f : 42f);
            float rootWidth = profileWidth + panelGap + portraitSize * 0.5f;
            Vector2 profileSize = new Vector2(rootWidth, profileHeight);
            Vector2 portraitSizeDelta = new Vector2(portraitSize, portraitSize);
            Vector2 playerRootOffset = new Vector2(-fighterGap * 0.5f, rootTop);
            Vector2 opponentRootOffset = new Vector2(fighterGap * 0.5f, rootTop);
            Vector2 playerPortraitOffset = Vector2.zero;
            Vector2 opponentPortraitOffset = Vector2.zero;
            float textTop = compact ? -12f : -16f;
            float rankTop = compact ? -42f : -50f;
            float textReservedWidth = portraitSize * 0.5f + panelGap + 32f;
            Vector2 profileTextSize = new Vector2(-textReservedWidth, compact ? 28f : 32f);
            Vector2 rankTextSize = new Vector2(-textReservedWidth, compact ? 24f : 28f);

            ApplyProfileRootLayout(
                playerRoot,
                new Vector2(0.5f, 1f),
                new Vector2(1f, 1f),
                playerRootOffset,
                profileSize);

            ApplyProfileRootLayout(
                opponentRoot,
                new Vector2(0.5f, 1f),
                new Vector2(0f, 1f),
                opponentRootOffset,
                profileSize);

            ApplyImageLayout(playerBattleSpriteImage, playerPortraitOffset, portraitSizeDelta, new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            ApplyImageLayout(opponentBattleSpriteImage, opponentPortraitOffset, portraitSizeDelta, new Vector2(0f, 1f), new Vector2(0.5f, 1f));

            ApplyProfileTextLayout(playerNameText, new Vector2(16f, textTop), profileTextSize, TextAlignmentOptions.Left);
            ApplyProfileTextLayout(playerTitleText, new Vector2(16f, textTop - 32f), rankTextSize, TextAlignmentOptions.Left);
            ApplyProfileTextLayout(playerRankText, new Vector2(16f, rankTop - 14f), rankTextSize, TextAlignmentOptions.Left);
            ApplyProfileTextLayout(opponentNameText, new Vector2(portraitSize * 0.5f + panelGap, textTop), profileTextSize, TextAlignmentOptions.Left);
            ApplyProfileTextLayout(opponentRankText, new Vector2(portraitSize * 0.5f + panelGap, rankTop), rankTextSize, TextAlignmentOptions.Left);

            ApplyBoardHpBarLayouts(canvasSize, compact);
            ApplyRoundHudLayout(canvasSize, compact, fighterGap, rootTop, portraitSize);
        }

        private float ResolveFreeHeightAboveBoards(Vector2 canvasSize)
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvas == null || canvasRect == null)
                return Mathf.Clamp(canvasSize.y * 0.26f, 150f, 260f);

            bool resolvedAny = false;
            float boardTop = float.PositiveInfinity;
            resolvedAny |= TryResolveBoardTopInCanvas(playerBoard, canvas, canvasRect, out float playerTop);
            if (resolvedAny)
                boardTop = Mathf.Min(boardTop, playerTop);

            bool opponentResolved = TryResolveBoardTopInCanvas(opponentBoard, canvas, canvasRect, out float opponentTop);
            if (opponentResolved)
            {
                resolvedAny = true;
                boardTop = Mathf.Min(boardTop, opponentTop);
            }

            if (!resolvedAny)
                return Mathf.Clamp(canvasSize.y * 0.26f, 150f, 260f);

            float canvasTop = canvasRect.rect.height * 0.5f;
            return Mathf.Clamp(canvasTop - boardTop, 150f, Mathf.Max(170f, canvasSize.y * 0.34f));
        }

        private bool TryResolveBoardTopInCanvas(BattleBoard board, Canvas canvas, RectTransform canvasRect, out float top)
        {
            top = 0f;

            RectTransform boardArea = board != null ? board.BoardArea : null;
            if (boardArea == null || canvas == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[1]), camera, out Vector2 topLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[2]), camera, out Vector2 topRight))
            {
                return false;
            }

            top = Mathf.Max(topLeft.y, topRight.y);
            return true;
        }

        private Transform ResolveProfileRoot(params Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.transform == null)
                    continue;

                if (component.transform.parent != null)
                    return component.transform.parent;
            }

            return null;
        }

        private void ApplyProfileRootLayout(
            Transform root,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 offset,
            Vector2 size)
        {
            if (root == null)
                return;

            RectTransform rect = root as RectTransform;
            if (rect == null)
                rect = root.GetComponent<RectTransform>();

            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        private void ApplyProfileTextLayout(
            TMP_Text text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TextAlignmentOptions alignment)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyPlayerHpBarLayout()
        {
            ApplyPlayerHpBarLayout(playerHpBarOffset, playerHpBarSize);
        }

        private void ApplyPlayerHpBarLayout(Vector2 offset, Vector2 size)
        {
            RectTransform barRoot = playerHpBarFill != null && playerHpBarFill.transform.parent != null
                ? playerHpBarFill.transform.parent as RectTransform
                : null;

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(true);
                barRoot.anchorMin = new Vector2(0.5f, 0.5f);
                barRoot.anchorMax = new Vector2(0.5f, 0.5f);
                barRoot.pivot = new Vector2(0.5f, 0.5f);
                barRoot.anchoredPosition = offset;
                barRoot.sizeDelta = size;
                barRoot.SetAsLastSibling();

                Image background = barRoot.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;
            }

            if (playerHpBarFill != null)
            {
                ApplyHpBarFillValue(playerHpBarFill, playerHpBarFillColor, ResolvePlayerHpNormalized());
            }

            if (playerHpBarText != null && playerHpBarText.rectTransform != null)
                ApplyHpBarTextLayout(playerHpBarText, offset, size);
        }

        private void ApplyOpponentHpBarLayout(Vector2 offset, Vector2 size)
        {
            RectTransform barRoot = opponentHpBarFill != null && opponentHpBarFill.transform.parent != null
                ? opponentHpBarFill.transform.parent as RectTransform
                : null;

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(true);
                barRoot.anchorMin = new Vector2(0.5f, 0.5f);
                barRoot.anchorMax = new Vector2(0.5f, 0.5f);
                barRoot.pivot = new Vector2(0.5f, 0.5f);
                barRoot.anchoredPosition = offset;
                barRoot.sizeDelta = size;
                barRoot.SetAsLastSibling();

                Image background = barRoot.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;
            }

            if (opponentHpBarFill != null)
            {
                ApplyHpBarFillValue(opponentHpBarFill, opponentHpBarFillColor, ResolveOpponentHpNormalized());
            }

            if (opponentHpBarText != null && opponentHpBarText.rectTransform != null)
                ApplyHpBarTextLayout(opponentHpBarText, offset, size);
        }

        private void ApplyBoardHpBarLayouts(Vector2 canvasSize, bool compact)
        {
            Canvas canvas = ResolveBattleHudCanvas();
            if (canvas == null)
                return;

            Vector2 fallbackSize = new Vector2(compact ? 16f : 18f, Mathf.Clamp(canvasSize.y * 0.32f, 160f, compact ? 300f : 380f));
            float fallbackY = -canvasSize.y * 0.24f;

            Vector2 playerOffset;
            Vector2 playerSize;
            if (!TryResolveBoardHpBarLayout(playerBoard, canvas, compact, true, out playerOffset, out playerSize))
            {
                playerOffset = new Vector2(-canvasSize.x * 0.03f, fallbackY);
                playerSize = fallbackSize;
            }

            Vector2 opponentOffset;
            Vector2 opponentSize;
            if (!TryResolveBoardHpBarLayout(opponentBoard, canvas, compact, false, out opponentOffset, out opponentSize))
            {
                opponentOffset = new Vector2(canvasSize.x * 0.03f, fallbackY);
                opponentSize = fallbackSize;
            }

            ReparentHpBarToCanvas(playerHpBarFill, playerHpBarText, canvas);
            ReparentHpBarToCanvas(opponentHpBarFill, opponentHpBarText, canvas);
            ApplyPlayerHpBarLayout(playerOffset, playerSize);
            ApplyOpponentHpBarLayout(opponentOffset, opponentSize);
        }

        private bool TryResolveBoardHpBarLayout(BattleBoard board, Canvas canvas, bool compact, bool playerSide, out Vector2 offset, out Vector2 size)
        {
            offset = Vector2.zero;
            size = Vector2.zero;

            RectTransform boardArea = board != null ? board.BoardArea : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (boardArea == null || canvasRect == null)
                return false;

            Vector3[] corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 bottomLeft;
            Vector2 topLeft;
            Vector2 topRight;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[0]), camera, out bottomLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[1]), camera, out topLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, corners[2]), camera, out topRight))
            {
                return false;
            }

            float boardHeight = Mathf.Abs(topLeft.y - bottomLeft.y);
            float barHeight = Mathf.Clamp(boardHeight * verticalBoardHpBarHeightFactor, 120f, compact ? 300f : 420f);
            float barWidth = compact ? Mathf.Max(12f, verticalBoardHpBarWidth - 2f) : verticalBoardHpBarWidth;
            float innerX = playerSide
                ? Mathf.Max(topLeft.x, topRight.x) - boardHpBarInnerInset
                : Mathf.Min(topLeft.x, topRight.x) + boardHpBarInnerInset;
            offset = new Vector2(innerX, (topLeft.y + bottomLeft.y) * 0.5f);
            size = new Vector2(barWidth, barHeight);
            return true;
        }

        private void ReparentHpBarToCanvas(Image fill, TMP_Text text, Canvas canvas)
        {
            if (canvas == null)
                return;

            Transform canvasTransform = canvas.transform;
            RectTransform barRoot = fill != null && fill.transform.parent != null
                ? fill.transform.parent as RectTransform
                : null;

            if (barRoot != null && barRoot.parent != canvasTransform)
                barRoot.SetParent(canvasTransform, false);

            if (text != null && text.transform.parent != canvasTransform)
                text.transform.SetParent(canvasTransform, false);

            if (barRoot != null)
                barRoot.SetAsLastSibling();
            if (text != null)
                text.transform.SetAsLastSibling();
        }

        private void ApplyHpBarTextLayout(TMP_Text text, Vector2 offset, Vector2 size)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform textRect = text.rectTransform;
            text.gameObject.SetActive(true);
            if (size.y > size.x * 2f)
            {
                text.gameObject.SetActive(false);
                return;
            }

            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = offset;
            textRect.sizeDelta = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyRoundHudLayout(Vector2 canvasSize, bool compact, float fighterGap, float rootTop, float portraitSize)
        {
            HideRoundHudBackgroundImages();

            float centerWidth = Mathf.Clamp(fighterGap + portraitSize * 1.35f, 174f, compact ? 250f : 310f);
            float top = rootTop - Mathf.Clamp(portraitSize * 0.58f, compact ? 62f : 72f, compact ? 86f : 104f);

            ApplyCenteredHudText(roundText, new Vector2(0f, top), new Vector2(centerWidth, compact ? 24f : 28f), compact ? 18f : 22f);
            ApplyCenteredHudText(scoreText, new Vector2(0f, top - (compact ? 24f : 28f)), new Vector2(centerWidth, compact ? 28f : 34f), compact ? 23f : 28f);
            ApplyCenteredHudText(stateText, new Vector2(0f, top - (compact ? 56f : 66f)), new Vector2(Mathf.Min(canvasSize.x - 32f, centerWidth + 110f), compact ? 24f : 28f), compact ? 16f : 19f);
        }

        private void HideRoundHudBackgroundImages()
        {
            Transform root = roundText != null && roundText.transform.parent != null
                ? roundText.transform.parent
                : null;

            if (root == null && scoreText != null && scoreText.transform.parent != null)
                root = scoreText.transform.parent;
            if (root == null && stateText != null && stateText.transform.parent != null)
                root = stateText.transform.parent;
            if (root == null)
                return;
            if (root.GetComponent<Canvas>() != null)
                return;
            if (!root.name.Contains("Round") && !root.name.Contains("Score") && !root.name.Contains("HUD"))
                return;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                    continue;

                image.enabled = false;
                image.raycastTarget = false;
            }
        }

        private void ApplyCenteredHudText(TMP_Text text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize)
        {
            if (text == null || text.rectTransform == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontSize = fontSize;
        }

        private Vector2 ResolveBattleHudCanvasSize()
        {
            Canvas canvas = ResolveBattleHudCanvas();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return canvasRect.rect.size;

            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        private Canvas ResolveBattleHudCanvas()
        {
            Canvas canvas = roundText != null ? roundText.GetComponentInParent<Canvas>() : null;
            if (canvas == null && playerBattleSpriteImage != null)
                canvas = playerBattleSpriteImage.GetComponentInParent<Canvas>();
            if (canvas == null && opponentBattleSpriteImage != null)
                canvas = opponentBattleSpriteImage.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

            return canvas;
        }

        private void CreateResultPanelUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            GameObject root = resultPanelRoot != null
                ? resultPanelRoot
                : new GameObject("BattleResultPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            root.layer = canvas.gameObject.layer;
            if (root.transform.parent != canvas.transform)
                root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                rect = root.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = root.GetComponent<Image>();
            if (background == null)
                background = root.AddComponent<Image>();

            background.color = new Color(0f, 0f, 0f, 0.9f);
            background.raycastTarget = true;

            resultPanelRoot = root;

            RectTransform window = EnsureResultWindow(root.transform);

            ReparentResultElement(resultTitleText, window);
            ReparentResultElement(resultRewardText, window);
            ReparentResultElement(resultExperienceText, window);
            ReparentResultElement(resultBattleLobbyButton, window);
            ReparentResultElement(resultNewMatchButton, window);

            if (resultTitleText == null)
                resultTitleText = CreateResultText(window);

            if (resultRewardText == null)
                resultRewardText = CreateResultInfoText(window, "BattleResultReward", new Vector2(0f, 34f), 32f);

            if (resultExperienceText == null)
                resultExperienceText = CreateResultInfoText(window, "BattleResultExperience", new Vector2(0f, -8f), 26f);

            if (resultBattleLobbyButton == null)
                resultBattleLobbyButton = CreateResultButton(window, "BattleLobbyButton", new Vector2(-142f, -126f), returnToBattleLobbyText);

            if (resultNewMatchButton == null)
                resultNewMatchButton = CreateResultButton(window, "BattleNewMatchButton", new Vector2(142f, -126f), newMatchText);

            ApplyResultTextLayout(resultTitleText, new Vector2(0f, 108f), new Vector2(resultPanelSize.x - 88f, 74f), 56f);
            ApplyResultTextLayout(resultRewardText, new Vector2(0f, 34f), new Vector2(resultPanelSize.x - 108f, 40f), 30f);
            ApplyResultTextLayout(resultExperienceText, new Vector2(0f, -8f), new Vector2(resultPanelSize.x - 108f, 34f), 23f);
            ApplyResultButtonLayout(resultBattleLobbyButton, new Vector2(-142f, -126f), new Vector2(252f, 58f), returnToBattleLobbyText);
            ApplyResultButtonLayout(resultNewMatchButton, new Vector2(142f, -126f), new Vector2(252f, 58f), newMatchText);

            root.transform.SetAsLastSibling();
            window.SetAsLastSibling();
        }

        private RectTransform EnsureResultWindow(Transform overlayRoot)
        {
            Transform existing = overlayRoot.Find("BattleResultWindow");
            GameObject windowObject = existing != null
                ? existing.gameObject
                : new GameObject("BattleResultWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            windowObject.layer = overlayRoot.gameObject.layer;
            windowObject.transform.SetParent(overlayRoot, false);

            RectTransform rect = windowObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = windowObject.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = resultPanelSize;

            Image image = windowObject.GetComponent<Image>();
            if (image == null)
                image = windowObject.AddComponent<Image>();

            image.color = new Color(0.055f, 0.058f, 0.064f, 0.96f);
            image.raycastTarget = true;

            return rect;
        }

        private static void ReparentResultElement(Component component, RectTransform window)
        {
            if (component == null || window == null)
                return;

            component.transform.SetParent(window, false);
            component.transform.SetAsLastSibling();
        }

        private TMP_Text CreateResultText(Transform parent)
        {
            GameObject textObject = new GameObject("BattleResultTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 112f);
            rect.sizeDelta = new Vector2(resultPanelSize.x - 80f, 96f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = 68f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = ResolveResultTitle(true);

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private void ApplyResultTextLayout(TMP_Text text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize)
        {
            if (text == null)
                return;

            RectTransform rect = text.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyResultButtonLayout(Button button, Vector2 anchoredPosition, Vector2 sizeDelta, string labelText)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = labelText;
                label.fontSize = 22f;
                label.color = new Color(0.06f, 0.06f, 0.07f, 1f);
                label.alignment = TextAlignmentOptions.Center;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
            {
                image.enabled = true;
                image.color = new Color(0.94f, 0.92f, 0.86f, 1f);
                image.raycastTarget = true;
            }
        }

        private TMP_Text CreateResultInfoText(Transform parent, string objectName, Vector2 anchoredPosition, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(resultPanelSize.x - 90f, 40f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private Button CreateResultButton(Transform parent, string objectName, Vector2 anchoredPosition, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(280f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateResultButtonLabel(buttonObject.transform);
            label.text = labelText;

            return button;
        }

        private TMP_Text CreateResultButtonLabel(Transform parent)
        {
            GameObject textObject = new GameObject("BattleLobbyButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = new Color(0.06f, 0.06f, 0.07f, 1f);
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private void ApplyImageLayout(
            Image image,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void ApplyImageFlip(Image image, bool flipX)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            Vector3 scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
            rect.localScale = scale;
        }

        private void ApplyBattleMatchResult(bool playerWon)
        {
            lastResultGoldReward = 0;
            lastResultExperienceReward = playerWon
                ? Mathf.Max(0, battleWinExperienceReward)
                : Mathf.Max(0, battleLoseExperienceReward);
            lastResultAccountLevel = 1;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
                profile.EnsureData();

            int altinBefore = GetProfileAltin(profile);

            MahjongBattleResult battleResult = playerWon ? MahjongBattleResult.Win : MahjongBattleResult.Lose;
            int score = Mathf.Max(0, playerRoundWins * 100 - opponentRoundWins * 25);
            MahjongMatchResultData result = MahjongMatchResultData.CreateBattleResult(
                battleResult,
                score,
                maxCombo: 0,
                stakePot: MahjongSession.BattleStakePot,
                battleMvp: playerWon);

            if (MahjongMatchService.I != null)
            {
                MahjongMatchProcessResult processed = MahjongMatchService.I.ProcessMatch(result);
                lastResultGoldReward = processed != null ? Mathf.Max(0, processed.GrantedAltin) : 0;

                if (playerWon && lastResultGoldReward <= 0 && MahjongRewardService.I == null)
                {
                    lastResultGoldReward = 100 + Mathf.Max(0, result.BattleStakePot);
                    GrantDirectBattleGold(lastResultGoldReward);
                }
            }
            else
            {
                ApplyBattleResultFallback(result);
                profile = ProfileService.I != null ? ProfileService.I.Current : profile;
            }

            if (profile == null)
                return;

            profile.EnsureData();
            EnsureBattleGoldRewardPersisted(profile, playerWon, result, altinBefore);
            profile.AddAccountExp(lastResultExperienceReward);
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
            {
                profile.Mahjong.Battle.AddExperience(lastResultExperienceReward);
                lastResultAccountLevel = profile.Mahjong.Battle.Level;
                if (MahjongTitleService.I != null)
                    MahjongTitleService.I.EvaluateBattleTitles(profile);
            }
            else
            {
                lastResultAccountLevel = profile.AccountLevel;
            }

            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }
        }

        private void EnsureBattleGoldRewardPersisted(
            PlayerProfile profile,
            bool playerWon,
            MahjongMatchResultData result,
            int altinBefore)
        {
            if (profile == null || !playerWon)
                return;

            int expectedReward = Mathf.Max(0, lastResultGoldReward);
            if (expectedReward <= 0)
            {
                expectedReward = 100 + (result != null ? Mathf.Max(0, result.BattleStakePot) : 0);
                lastResultGoldReward = expectedReward;
            }

            int actualGain = Mathf.Max(0, GetProfileAltin(profile) - Mathf.Max(0, altinBefore));
            int missingReward = Mathf.Max(0, expectedReward - actualGain);
            if (missingReward <= 0)
                return;

            profile.AddAltin(missingReward);
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                profile.Mahjong.Battle.TotalBattleRewardEarned += missingReward;
        }

        private static int GetProfileAltin(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Currencies != null ? Mathf.Max(0, profile.Currencies.OzAltin) : 0;
        }

        private void ApplyBattleResultFallback(MahjongMatchResultData result)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || result == null)
                return;

            profile.EnsureData();

            if (result.BattleResult == MahjongBattleResult.Win)
            {
                profile.Mahjong.Battle.AddWin(result.BattleMvp);
                profile.Mahjong.TotalWins++;
                lastResultGoldReward = 100 + Mathf.Max(0, result.BattleStakePot);
                profile.AddAltin(lastResultGoldReward);
            }
            else if (result.BattleResult == MahjongBattleResult.Lose)
            {
                profile.Mahjong.Battle.AddLoss(result.BattleMvp);
                profile.Mahjong.TotalLosses++;
            }

            profile.Mahjong.Battle.LastStakeUsed = Mathf.Max(0, result.BattleStakePot);
            profile.Mahjong.Battle.TotalBattleRewardEarned += lastResultGoldReward;
            profile.Mahjong.TotalMatchesPlayed++;
            profile.Mahjong.TotalScoreAllModes += Mathf.Max(0, result.Score);

            if (MahjongTitleService.I != null)
                MahjongTitleService.I.EvaluateBattleTitles(profile);
        }

        private void GrantDirectBattleGold(int amount)
        {
            if (amount <= 0)
                return;

            if (CurrencyService.I != null)
                CurrencyService.I.AddOzAltin(amount);
            else if (ProfileService.I != null && ProfileService.I.Current != null)
                ProfileService.I.Current.AddAltin(amount);

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return;

            profile.EnsureData();
            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                profile.Mahjong.Battle.TotalBattleRewardEarned += amount;
        }

        private void ShowResultPanel(bool playerWon)
        {
            AutoResolveResultPanelUi();
            CreateResultPanelUi();

            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(true);
                resultPanelRoot.transform.SetAsLastSibling();
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = ResolveResultTitle(playerWon);
                resultTitleText.color = playerWon ? winResultColor : failedResultColor;
            }

            if (resultRewardText != null)
                resultRewardText.text = lastResultGoldReward > 0
                    ? string.Format(resultGoldFormat, lastResultGoldReward)
                    : resultNoGoldText;

            if (resultExperienceText != null)
                resultExperienceText.text = string.Format(
                    resultExperienceFormat,
                    Mathf.Max(0, lastResultExperienceReward),
                    Mathf.Max(1, lastResultAccountLevel));
        }

        private void HideResultPanel()
        {
            if (resultPanelRoot != null)
                resultPanelRoot.SetActive(false);
        }

        private string ResolveResultTitle(bool playerWon)
        {
            string value = playerWon ? winResultText : failedResultText;
            if (string.IsNullOrWhiteSpace(value))
                return playerWon ? "VICTORY" : "DEFEAT";

            value = value.Trim();
            if (playerWon && string.Equals(value, "WIN", StringComparison.OrdinalIgnoreCase))
                return "VICTORY";
            if (!playerWon && string.Equals(value, "FAILED", StringComparison.OrdinalIgnoreCase))
                return "DEFEAT";

            return value;
        }

        private void BindResultPanelButton()
        {
            if (resultBattleLobbyButton != null)
            {
                resultBattleLobbyButton.onClick.RemoveListener(OnClickReturnToBattleLobby);
                resultBattleLobbyButton.onClick.AddListener(OnClickReturnToBattleLobby);
            }

            if (resultNewMatchButton != null)
            {
                resultNewMatchButton.onClick.RemoveListener(OnClickFindNewMatch);
                resultNewMatchButton.onClick.AddListener(OnClickFindNewMatch);
            }
        }

        private void OnClickReturnToBattleLobby()
        {
            if (botController != null)
                botController.StopBot();

            SceneManager.LoadScene(battleLobbySceneName);
        }

        private void OnClickFindNewMatch()
        {
            if (botController != null)
                botController.StopBot();

            MahjongBattleLobbyMode mode = MahjongBattleLobbySession.SelectedMode;
            if (mode == MahjongBattleLobbyMode.LocalWifiMatch)
            {
                SceneManager.LoadScene(battleLobbySceneName);
                return;
            }

            if (mode == MahjongBattleLobbyMode.None)
            {
                mode = MahjongBattleLobbyMode.RandomMatch;
                MahjongBattleLobbySession.SetMode(mode);
            }

            MahjongBattleBotService botService = MahjongBattleBotService.I;
            if (botService == null)
            {
                GameObject serviceObject = new GameObject("MahjongBattleBotService");
                botService = serviceObject.AddComponent<MahjongBattleBotService>();
            }

            int playerRankPoints = 0;
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
            {
                profile.EnsureData();
                if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                    playerRankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
            }

            MahjongSession.StartBattle(botService.CreateOpponent(mode, playerRankPoints));
            SceneManager.LoadScene(battleGameSceneName);
        }

        private TMP_Text CreateProfileText(
            Transform parent,
            string objectName,
            float fontSize,
            Vector2 anchoredPosition,
            int siblingIndex)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            textObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(-40f, 34f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = objectName;

            TMP_Text styleSource = playerNameText != null ? playerNameText : opponentNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private void BindCharacterSelectionService()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged -= ApplyPlayerBattleSpriteUi;
            BattleCharacterSelectionService.Instance.SelectedCharacterChanged += HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged += ApplyPlayerBattleSpriteUi;
        }

        private void UnbindCharacterSelectionService()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= HandleSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged -= ApplyPlayerBattleSpriteUi;
        }

        private void HandleSelectedCharacterChanged(string _)
        {
            ApplyPlayerBattleSpriteUi();
            ApplyBattleProfileLayout();
        }

        private void AutoResolveLinks()
        {
            if (battleStore == null)
                battleStore = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();

            if (battleLayoutPresetService == null)
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();

            if (combatSystem == null)
                combatSystem = GetComponent<BattleCombatSystem>();

            if (playerBoard == null || opponentBoard == null)
            {
                BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
                for (int i = 0; i < boards.Length; i++)
                {
                    BattleBoard foundBoard = boards[i];
                    if (foundBoard == null)
                        continue;

                    if (foundBoard.Side == BattleBoardSide.Player && playerBoard == null)
                        playerBoard = foundBoard;
                    else if (foundBoard.Side == BattleBoardSide.Opponent && opponentBoard == null)
                        opponentBoard = foundBoard;
                }
            }

            if (combatSystem != null)
            {
                combatSystem.SetMatchController(this);
                combatSystem.SetBoards(playerBoard, opponentBoard);
                BindCombatSystem();
            }

            ConfigureBoardInputOwnership();
        }

        private void ConfigureBoardInputOwnership()
        {
            if (playerBoard != null)
                playerBoard.SetAllowPlayerInput(true);

            if (opponentBoard != null)
                opponentBoard.SetAllowPlayerInput(false);
        }

        private void EnsureLocalWifiMatchSync()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.LocalWifiMatch)
                return;

            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.I;
            if (network == null || !network.IsConnected)
                return;

            LocalWifiBattleMatchSync sync = GetComponent<LocalWifiBattleMatchSync>();
            if (sync == null)
                sync = gameObject.AddComponent<LocalWifiBattleMatchSync>();

            sync.Configure(this, playerBoard, opponentBoard, botController, combatSystem);

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }
        }

        private void EnsureOnlineRankedMatchSync()
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.RankedMatch)
                return;

            OnlineRankedBattleNetwork network = OnlineRankedBattleNetwork.I;
            if (network == null || !network.IsInMatch)
                return;

            OnlineRankedBattleMatchSync sync = GetComponent<OnlineRankedBattleMatchSync>();
            if (sync == null)
                sync = gameObject.AddComponent<OnlineRankedBattleMatchSync>();

            sync.Configure(this, playerBoard, opponentBoard, botController, combatSystem);

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }
        }

        private static bool IsLocalWifiBattleActive()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch &&
                   LocalWifiBattleNetwork.I != null &&
                   LocalWifiBattleNetwork.I.IsConnected;
        }

        private static bool IsOnlineRankedBattleActive()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch &&
                   OnlineRankedBattleNetwork.I != null &&
                   OnlineRankedBattleNetwork.I.IsInMatch;
        }

        private static bool IsRealtimeOpponentBattleActive()
        {
            return IsLocalWifiBattleActive() || IsOnlineRankedBattleActive();
        }

        private void EnsureBattleBoardsLayout()
        {
            if (playerBoard == null || opponentBoard == null)
                return;

            RectTransform playerArea = playerBoard.BoardArea;
            RectTransform opponentArea = opponentBoard.BoardArea;
            if (playerArea == null || opponentArea == null)
                return;

            RectTransform root = playerArea.parent as RectTransform;
            if (root == null)
                root = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include)?.transform as RectTransform;

            if (root == null)
                return;

            if (boardsHeightFirstLayout == null || boardsHeightFirstLayout.transform != root)
                boardsHeightFirstLayout = root.GetComponent<BattleBoardsHeightFirstLayout>();

            if (boardsHeightFirstLayout == null)
                boardsHeightFirstLayout = root.gameObject.AddComponent<BattleBoardsHeightFirstLayout>();

            boardsHeightFirstLayout.Configure(playerArea, opponentArea);
            RefitBattleBoards();
        }

        private void RefitBattleBoards()
        {
            playerBoard?.RefitIntoBoardArea();
            opponentBoard?.RefitIntoBoardArea();
            lastPlayerBoardAreaSize = playerBoard != null && playerBoard.BoardArea != null
                ? playerBoard.BoardArea.rect.size
                : Vector2.zero;
            lastOpponentBoardAreaSize = opponentBoard != null && opponentBoard.BoardArea != null
                ? opponentBoard.BoardArea.rect.size
                : Vector2.zero;
        }

        private void RefitBoardsWhenAreaSizeChanges()
        {
            Vector2 playerSize = playerBoard != null && playerBoard.BoardArea != null
                ? playerBoard.BoardArea.rect.size
                : Vector2.zero;
            Vector2 opponentSize = opponentBoard != null && opponentBoard.BoardArea != null
                ? opponentBoard.BoardArea.rect.size
                : Vector2.zero;

            if (playerSize == lastPlayerBoardAreaSize && opponentSize == lastOpponentBoardAreaSize)
                return;

            RefitBattleBoards();
        }

        private void RefitBattleHudWhenCanvasSizeChanges()
        {
            Vector2 canvasSize = ResolveBattleHudCanvasSize();
            if ((canvasSize - lastBattleHudCanvasSize).sqrMagnitude < 1f)
                return;

            ApplyBattleProfileLayout();
        }

        private int TotalCombatRounds => Mathf.Max(1, totalCombatRounds);

        private void BindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged -= HandleOpponentHpChanged;
            combatSystem.DamageApplied -= HandleDamageApplied;
            combatSystem.CombatFinished += HandleCombatFinished;
            combatSystem.PlayerHpChanged += HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged += HandleOpponentHpChanged;
            combatSystem.DamageApplied += HandleDamageApplied;
        }

        private void UnbindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
            combatSystem.OpponentHpChanged -= HandleOpponentHpChanged;
            combatSystem.DamageApplied -= HandleDamageApplied;
        }

        private void HandlePlayerHpChanged(BattleCombatSystem _, int hp, int maxHp)
        {
            RefreshPlayerHpBar(hp, maxHp);
            if (scoreText != null)
                scoreText.text = GetScoreText();
        }

        private void HandleOpponentHpChanged(BattleCombatSystem _, int hp, int maxHp)
        {
            RefreshOpponentHpBar(hp, maxHp);
            if (scoreText != null)
                scoreText.text = GetScoreText();
        }

        private void HandleCombatFinished(BattleCombatSystem _, BattleBoardSide deadSide)
        {
            FinishRoundByDeadSide(deadSide, notifyLocalWifiPeer: true);
        }

        public void HandleLocalWifiRemoteRoundEnded(int senderRoundNumber, BattleBoardSide senderDeadSide)
        {
            if (senderRoundNumber != CurrentRoundNumber)
            {
                Log($"Ignored remote Wi-Fi round end for round {senderRoundNumber}; current round is {CurrentRoundNumber}");
                return;
            }

            FinishRoundByDeadSide(MapRemoteBattleSide(senderDeadSide), notifyLocalWifiPeer: false);
        }

        private void FinishRoundByDeadSide(BattleBoardSide deadSide, bool notifyLocalWifiPeer)
        {
            if (matchFinished || !matchRunning || roundEnding)
                return;

            roundEnding = true;
            StopBoardRoutines();

            if (botController != null)
                botController.StopBot();

            if (notifyLocalWifiPeer && IsLocalWifiBattleActive())
                LocalWifiBattleNetwork.I.SendRoundEnded(CurrentRoundNumber, deadSide);

            bool playerWonRound = deadSide == BattleBoardSide.Opponent;
            if (playerWonRound)
                playerRoundWins++;
            else
                opponentRoundWins++;

            string resultText = playerWonRound ? stateRoundWin : stateRoundLose;
            if (stateText != null)
                stateText.text = CurrentRoundNumber >= TotalCombatRounds
                    ? resultText
                    : $"{resultText}. Next round...";

            RefreshHud();
            RoundFinished?.Invoke(this, CurrentRoundNumber);
            NotifyStateChanged();

            Log(
                $"Round finished by HP | Round={CurrentRoundNumber}/{TotalCombatRounds} | " +
                $"PlayerWonRound={playerWonRound} | Score={playerRoundWins}:{opponentRoundWins}");

            if (CurrentRoundNumber >= TotalCombatRounds)
            {
                ForceFinishMatch(playerRoundWins >= opponentRoundWins);
                return;
            }

            StopRoundTransitionRoutine();
            roundTransitionRoutine = StartCoroutine(StartNextHpRoundRoutine());
        }

        private static BattleBoardSide MapRemoteBattleSide(BattleBoardSide remoteSide)
        {
            return remoteSide == BattleBoardSide.Player
                ? BattleBoardSide.Opponent
                : BattleBoardSide.Player;
        }

        private IEnumerator StartNextHpRoundRoutine()
        {
            if (botController != null)
                botController.StopBot();

            yield return new WaitForSeconds(ResolveRoundTransitionDelay());

            roundTransitionRoutine = null;

            if (matchFinished || !matchRunning)
            {
                roundEnding = false;
                NotifyStateChanged();
                yield break;
            }

            currentRoundIndex++;
            roundEnding = false;

            if (combatSystem != null)
                combatSystem.StartCombat();

            RefreshHud();
            BuildInitialBoards();

            Log($"Next HP round started | Round={CurrentRoundNumber}/{TotalCombatRounds}");
        }

        private float ResolveRoundTransitionDelay()
        {
            float delay = Mathf.Max(0.05f, nextRoundDelay);
            if (IsRealtimeOpponentBattleActive())
                delay = Mathf.Max(delay, 2.2f);

            return delay;
        }

        private void NotifyStateChanged()
        {
            MatchStateChanged?.Invoke(this);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleMatchController] {message}", this);
        }
    }
}
