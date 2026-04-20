using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private string playerRankFormat = "{0} {1} RP";
        [SerializeField] private string fallbackPlayerName = "Player";
        [SerializeField] private string fallbackPlayerRankTier = "Unranked";
        [SerializeField] private bool createPlayerProfileUiIfMissing = true;
        [SerializeField] private Vector2 playerProfileUiSize = new Vector2(440f, 112f);
        [SerializeField] private Vector2 playerProfileUiOffset = new Vector2(72f, -54f);
        [SerializeField] private Vector2 playerBattleSpriteSize = new Vector2(86f, 86f);
        [SerializeField] private Vector2 playerBattleSpriteOffset = new Vector2(18f, -13f);
        [SerializeField] private Image playerHpBarFill;
        [SerializeField] private TMP_Text playerHpBarText;
        [SerializeField] private Vector2 playerHpBarOffset = new Vector2(120f, -92f);
        [SerializeField] private Vector2 playerHpBarSize = new Vector2(280f, 16f);
        [SerializeField] private Color playerHpBarBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color playerHpBarFillColor = new Color(0.3f, 0.9f, 0.35f, 1f);
        [SerializeField] private bool hideLegacyPlayerHpText = true;

        [Header("Opponent Profile UI")]
        [SerializeField] private Image opponentBattleSpriteImage;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text opponentRankText;
        [SerializeField] private string opponentRankFormat = "{0} {1} RP";
        [SerializeField] private bool createOpponentProfileUiIfMissing = true;
        [SerializeField] private Vector2 opponentProfileUiSize = new Vector2(440f, 112f);
        [SerializeField] private Vector2 opponentProfileUiOffset = new Vector2(-72f, -54f);
        [SerializeField] private Vector2 opponentBattleSpriteSize = new Vector2(86f, 86f);
        [SerializeField] private Vector2 opponentBattleSpriteOffset = new Vector2(-104f, -13f);
        [SerializeField] private bool flipOpponentBattleSpriteX = true;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanelRoot;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private Button resultBattleLobbyButton;
        [SerializeField] private bool createResultPanelIfMissing = true;
        [SerializeField] private string winResultText = "WIN";
        [SerializeField] private string failedResultText = "FAILED";
        [SerializeField] private string returnToBattleLobbyText = "Battle Lobby";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private Vector2 resultPanelSize = new Vector2(560f, 320f);
        [SerializeField] private Color resultPanelBackgroundColor = new Color(0f, 0f, 0f, 0.72f);
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

        private Coroutine matchStartRoutine;
        private Coroutine playerBoardRoutine;
        private Coroutine opponentBoardRoutine;
        private Coroutine roundTransitionRoutine;
        private string opponentBattleCharacterId;

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
        }

        private void OnEnable()
        {
            AutoResolveLinks();
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
        }

        private void Start()
        {
            EnsureBattleOpponentSession();
            EnsureOpponentBattleCharacter();
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshPlayerHpBar();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
            RefreshHud();
            StartMatch();
        }

        public void StartMatch()
        {
            StopAllCoroutines();
            matchStartRoutine = null;
            playerBoardRoutine = null;
            opponentBoardRoutine = null;
            roundTransitionRoutine = null;
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
            AutoResolvePlayerProfileUi();
            AutoResolveOpponentProfileUi();
            AutoResolveResultPanelUi();
            HideResultPanel();
            ApplyPlayerProfileUi();
            ApplyPlayerBattleSpriteUi();
            RefreshPlayerHpBar();
            ApplyOpponentProfileUi();
            ApplyOpponentBattleSpriteUi();
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

            matchFinished = true;
            matchRunning = false;
            playerBoardRebuilding = false;
            opponentBoardRebuilding = false;
            roundEnding = false;

            if (stateText != null)
                stateText.text = playerWon ? stateMatchWin : stateMatchLose;

            if (botController != null)
                botController.StopBot();

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

            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: true);

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
            BuildBoardForSide(opponentBoard, opponentLayoutIndex, restartBotAfterBuild: true);

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

            int seed = UnityEngine.Random.Range(100000, 999999);

            board.Clear();
            board.SetRoundData(resolvedRoundNumber, slots, seed, source);
            board.Build();

            if (!board.IsBuilt)
            {
                Debug.LogError($"[BattleMatchController] Board build failed | Side={board.Side} | Round={resolvedRoundNumber}");
                return;
            }

            if (restartBotAfterBuild && botController != null)
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

            if (createOpponentProfileUiIfMissing &&
                (opponentBattleSpriteImage == null || opponentNameText == null || opponentRankText == null))
            {
                CreateOpponentProfileUi();
            }
        }

        private void AutoResolvePlayerProfileUi()
        {
            if (hideLegacyPlayerHpText)
                HideLegacyPlayerHpText();

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

            if (createPlayerProfileUiIfMissing &&
                (playerBattleSpriteImage == null ||
                 playerNameText == null ||
                 playerRankText == null ||
                 playerHpBarFill == null ||
                 playerHpBarText == null))
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

            if (resultBattleLobbyButton == null)
            {
                resultBattleLobbyButton = FindButtonByObjectName("BattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("ReturnBattleLobbyButton");
                if (resultBattleLobbyButton == null)
                    resultBattleLobbyButton = FindButtonByObjectName("BackToBattleLobbyButton");
            }

            if (createResultPanelIfMissing &&
                (resultPanelRoot == null || resultTitleText == null || resultBattleLobbyButton == null))
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

            Sprite battleSprite = ResolveBattleSprite(opponentBattleCharacterId);
            opponentBattleSpriteImage.sprite = battleSprite;
            opponentBattleSpriteImage.enabled = battleSprite != null;
            opponentBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                opponentBattleSpriteImage,
                opponentBattleSpriteOffset,
                opponentBattleSpriteSize,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));
            ApplyImageFlip(opponentBattleSpriteImage, flipOpponentBattleSpriteX);
        }

        private void ApplyPlayerProfileUi()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string playerName = fallbackPlayerName;
            string rankTier = fallbackPlayerRankTier;
            int rankPoints = 0;

            if (profile != null)
            {
                profile.EnsureData();

                if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                    playerName = profile.DisplayName.Trim();

                if (profile.Mahjong != null && profile.Mahjong.Battle != null)
                {
                    if (!string.IsNullOrWhiteSpace(profile.Mahjong.Battle.RankTier))
                        rankTier = profile.Mahjong.Battle.RankTier.Trim();

                    rankPoints = Mathf.Max(0, profile.Mahjong.Battle.RankPoints);
                }
            }

            if (playerNameText != null)
                playerNameText.text = playerRankText == null
                    ? $"{playerName} [{string.Format(playerRankFormat, rankTier, rankPoints)}]"
                    : playerName;

            if (playerRankText != null)
                playerRankText.text = string.Format(playerRankFormat, rankTier, rankPoints);
        }

        private void RefreshPlayerHpBar()
        {
            int hp = combatSystem != null ? combatSystem.PlayerHp : 0;
            int maxHp = combatSystem != null ? combatSystem.MaxPlayerHp : 0;
            RefreshPlayerHpBar(hp, maxHp);
        }

        private void RefreshPlayerHpBar(int hp, int maxHp)
        {
            if (playerHpBarFill != null)
            {
                float value = maxHp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, hp) / maxHp) : 0f;
                playerHpBarFill.fillAmount = value;
            }

            if (playerHpBarText != null)
                playerHpBarText.text = maxHp > 0 ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP";
        }

        private void HideLegacyPlayerHpText()
        {
            TMP_Text legacy = FindTextByObjectName("PlayerHPText");
            if (legacy == null)
                legacy = FindTextByObjectName("PlayerHpText");

            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }

        private void ApplyPlayerBattleSpriteUi()
        {
            if (playerBattleSpriteImage == null)
                return;

            Sprite battleSprite = ResolveSelectedBattleSprite();
            playerBattleSpriteImage.sprite = battleSprite;
            playerBattleSpriteImage.enabled = battleSprite != null;
            playerBattleSpriteImage.preserveAspect = true;
            ApplyImageLayout(
                playerBattleSpriteImage,
                playerBattleSpriteOffset,
                playerBattleSpriteSize,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));
            ApplyImageFlip(playerBattleSpriteImage, false);
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
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
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
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));
            }

            if (playerNameText == null)
                playerNameText = CreateProfileText(root.transform, "PlayerName", 30f, new Vector2(120f, -18f), 1);

            if (playerRankText == null)
                playerRankText = CreateProfileText(root.transform, "PlayerRank", 22f, new Vector2(120f, -58f), 2);

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
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
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
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
            }

            if (opponentNameText == null)
                opponentNameText = CreateProfileText(root.transform, "OpponentName", 30f, new Vector2(20f, -18f), 1);

            if (opponentRankText == null)
                opponentRankText = CreateProfileText(root.transform, "OpponentRank", 22f, new Vector2(20f, -58f), 2);
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
            playerHpBarFill.type = Image.Type.Filled;
            playerHpBarFill.fillMethod = Image.FillMethod.Horizontal;
            playerHpBarFill.fillOrigin = 0;
            playerHpBarFill.fillAmount = 1f;

            if (playerHpBarText == null)
            {
                playerHpBarText = CreateProfileText(parent, "PlayerHpBarText", 16f, playerHpBarOffset + new Vector2(0f, 18f), 4);
                playerHpBarText.alignment = TextAlignmentOptions.Center;
                playerHpBarText.rectTransform.sizeDelta = playerHpBarSize;
            }

            RefreshPlayerHpBar();
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
            if (root.transform.parent == null)
                root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                rect = root.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = resultPanelSize;

            Image background = root.GetComponent<Image>();
            if (background == null)
                background = root.AddComponent<Image>();

            background.color = resultPanelBackgroundColor;
            background.raycastTarget = true;

            resultPanelRoot = root;

            if (resultTitleText == null)
                resultTitleText = CreateResultText(root.transform);

            if (resultBattleLobbyButton == null)
                resultBattleLobbyButton = CreateResultButton(root.transform);
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
            rect.anchoredPosition = new Vector2(0f, 70f);
            rect.sizeDelta = new Vector2(resultPanelSize.x - 80f, 96f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontSize = 68f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = winResultText;

            TMP_Text styleSource = stateText != null ? stateText : playerNameText;
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private Button CreateResultButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("BattleLobbyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -70f);
            rect.sizeDelta = new Vector2(300f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateResultButtonLabel(buttonObject.transform);
            label.text = returnToBattleLobbyText;

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
            text.color = Color.black;
            text.fontSize = 28f;
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

        private void ShowResultPanel(bool playerWon)
        {
            AutoResolveResultPanelUi();

            if (resultPanelRoot != null)
                resultPanelRoot.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = playerWon ? winResultText : failedResultText;
                resultTitleText.color = playerWon ? winResultColor : failedResultColor;
            }
        }

        private void HideResultPanel()
        {
            if (resultPanelRoot != null)
                resultPanelRoot.SetActive(false);
        }

        private void BindResultPanelButton()
        {
            if (resultBattleLobbyButton == null)
                return;

            resultBattleLobbyButton.onClick.RemoveListener(OnClickReturnToBattleLobby);
            resultBattleLobbyButton.onClick.AddListener(OnClickReturnToBattleLobby);
        }

        private void OnClickReturnToBattleLobby()
        {
            if (botController != null)
                botController.StopBot();

            SceneManager.LoadScene(battleLobbySceneName);
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
        }

        private int TotalCombatRounds => Mathf.Max(1, totalCombatRounds);

        private void BindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
            combatSystem.CombatFinished += HandleCombatFinished;
            combatSystem.PlayerHpChanged += HandlePlayerHpChanged;
        }

        private void UnbindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.CombatFinished -= HandleCombatFinished;
            combatSystem.PlayerHpChanged -= HandlePlayerHpChanged;
        }

        private void HandlePlayerHpChanged(BattleCombatSystem _, int hp, int maxHp)
        {
            RefreshPlayerHpBar(hp, maxHp);
        }

        private void HandleCombatFinished(BattleCombatSystem _, BattleBoardSide deadSide)
        {
            if (matchFinished || !matchRunning || roundEnding)
                return;

            roundEnding = true;
            StopBoardRoutines();

            bool playerWonRound = deadSide == BattleBoardSide.Opponent;
            if (playerWonRound)
                playerRoundWins++;
            else
                opponentRoundWins++;

            if (stateText != null)
                stateText.text = playerWonRound ? stateRoundWin : stateRoundLose;

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

        private IEnumerator StartNextHpRoundRoutine()
        {
            if (botController != null)
                botController.StopBot();

            yield return new WaitForSeconds(Mathf.Max(0.05f, nextRoundDelay));

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
