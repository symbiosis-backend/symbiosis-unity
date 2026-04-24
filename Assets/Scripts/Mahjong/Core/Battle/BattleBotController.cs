using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleBotController : MonoBehaviour
    {
        public event Action<BattleBotController> BotStarted;
        public event Action<BattleBotController> BotStopped;
        public event Action<BattleBotController, BattleTile> BotPickedTile;
        public event Action<BattleBotController> BotStateChanged;

        [Header("Links")]
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleCombatSystem combatSystem;

        [Header("Timing")]
        [SerializeField] private float minThinkDelay = 0.45f;
        [SerializeField] private float maxThinkDelay = 1.10f;
        [SerializeField] private float minPickDelay = 0.08f;
        [SerializeField] private float maxPickDelay = 0.22f;
        [SerializeField] private float retryDelay = 0.25f;

        [Header("Difficulty")]
        [SerializeField] private float bronzeSpeed = 0.95f;
        [SerializeField] private float silverSpeed = 1.00f;
        [SerializeField] private float goldSpeed = 1.08f;
        [SerializeField] private float jadeSpeed = 1.16f;
        [SerializeField] private float masterSpeed = 1.24f;

        [Header("Adaptive Difficulty")]
        [SerializeField] private bool useAdaptiveDifficulty = true;
        [SerializeField, Range(0f, 1f)] private float baseSkill = 0.46f;
        [SerializeField, Range(0f, 1f)] private float minAdaptiveSkill = 0.22f;
        [SerializeField, Range(0f, 1f)] private float maxAdaptiveSkill = 0.76f;
        [SerializeField, Min(0.1f)] private float playerTargetPairSeconds = 6f;
        [SerializeField, Range(0.01f, 1f)] private float adaptiveSmoothing = 0.2f;
        [SerializeField, Min(0.1f)] private float minDelayMultiplier = 1.05f;
        [SerializeField, Min(0.1f)] private float maxDelayMultiplier = 2.25f;
        [SerializeField, Range(0f, 1f)] private float minMistakeChance = 0.16f;
        [SerializeField, Range(0f, 1f)] private float maxMistakeChance = 0.52f;
        [SerializeField, Range(0f, 1f)] private float minKnownPairUseChance = 0.24f;
        [SerializeField, Range(0f, 1f)] private float maxKnownPairUseChance = 0.76f;
        [SerializeField, Range(0f, 1f)] private float memoryDropChance = 0.18f;

        [Header("State")]
        [SerializeField] private bool autoStartOnEnable = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private bool verboseLoopLogs = false;

        private readonly Dictionary<BattleTile, string> memory = new();

        private Coroutine botRoutine;
        private bool activeRound;
        private float speedFactor = 1f;
        private float adaptiveSkill;
        private float playerPairSecondsEma = -1f;
        private float lastPlayerPairTime = -1f;
        private int playerMatchCount;
        private int playerMismatchCount;
        private int botMatchCount;
        private int botMismatchCount;

        public BattleBoard OpponentBoard => opponentBoard;
        public BattleBoard PlayerBoard => playerBoard;
        public bool IsActiveRound => activeRound;
        public bool IsRunning => botRoutine != null;
        public float SpeedFactor => speedFactor;
        public float AdaptiveSkill => adaptiveSkill;

        private void OnEnable()
        {
            Log("OnEnable");
            AutoResolveLinks();
            BindBoard();
            BindPlayerBoard();
            BindCombatSystem();

            if (IsLocalWifiBattleActive())
            {
                Log("Local Wi-Fi battle active; bot auto-start disabled");
                StopBotInternal(false);
                return;
            }

            if (autoStartOnEnable)
            {
                Log("AutoStartOnEnable = true, trying to start bot");
                TryStartBot();
            }
        }

        private void OnDisable()
        {
            Log("OnDisable");
            UnbindCombatSystem();
            UnbindPlayerBoard();
            UnbindBoard();
            StopBot();
        }

        private void Start()
        {
            Log("Start");
            if (IsLocalWifiBattleActive())
            {
                StopBotInternal(false);
                return;
            }

            if (autoStartOnEnable && !IsRunning)
            {
                Log("Start -> TryStartBot");
                TryStartBot();
            }
        }

        public void SetBoard(BattleBoard board)
        {
            if (board != null && board.Side != BattleBoardSide.Opponent)
            {
                Debug.LogWarning($"[BattleBotController] Ignored non-opponent board '{board.name}'.", this);
                return;
            }

            if (opponentBoard == board)
                return;

            Log($"SetBoard -> {(board != null ? board.name : "null")}");

            UnbindBoard();
            opponentBoard = board;
            BindBoard();
            ClearMemory();
            NotifyStateChanged();
        }

        public void SetPlayerBoard(BattleBoard board)
        {
            if (board != null && board.Side != BattleBoardSide.Player)
            {
                Debug.LogWarning($"[BattleBotController] Ignored non-player board '{board.name}'.", this);
                return;
            }

            if (playerBoard == board)
                return;

            UnbindPlayerBoard();
            playerBoard = board;
            BindPlayerBoard();
            NotifyStateChanged();
        }

        public void SetCombatSystem(BattleCombatSystem combat)
        {
            if (combatSystem == combat)
                return;

            UnbindCombatSystem();
            combatSystem = combat;
            BindCombatSystem();
            NotifyStateChanged();
        }

        public void RestartBot()
        {
            Log("RestartBot");
            if (IsLocalWifiBattleActive())
            {
                StopBotInternal(false);
                return;
            }

            StopBot();
            TryStartBot();
        }

        public void TryStartBot()
        {
            Log("TryStartBot called");
            if (IsLocalWifiBattleActive())
            {
                Log("TryStartBot skipped for local Wi-Fi battle");
                StopBotInternal(false);
                return;
            }

            if (opponentBoard != null && opponentBoard.Side != BattleBoardSide.Opponent)
            {
                Debug.LogWarning($"[BattleBotController] Assigned board '{opponentBoard.name}' is not opponent side. Resolving again.", this);
                UnbindBoard();
                opponentBoard = null;
            }

            if (opponentBoard == null)
            {
                Log("OpponentBoard is null, trying FindOpponentBoard()");
                opponentBoard = FindOpponentBoard();

                if (opponentBoard == null)
                {
                    Debug.LogWarning("[BattleBotController] Opponent BattleBoard not found.");
                    return;
                }

                Log($"OpponentBoard found automatically: {opponentBoard.name}");
                BindBoard();
            }

            speedFactor = ResolveSpeedFactor(
                MahjongSession.BattleOpponentRankTier,
                MahjongSession.BattleOpponentRankPoints);
            adaptiveSkill = Mathf.Clamp(baseSkill, minAdaptiveSkill, maxAdaptiveSkill);
            ResetAdaptiveRoundStats();

            Log(
                $"Resolved speedFactor={speedFactor:0.00} | " +
                $"RankTier='{MahjongSession.BattleOpponentRankTier}' | " +
                $"RankPoints={MahjongSession.BattleOpponentRankPoints}");

            StopBotInternal(false);
            activeRound = true;
            botRoutine = StartCoroutine(BotLoop());

            BotStarted?.Invoke(this);
            NotifyStateChanged();

            Log("Bot coroutine started");
        }

        public void StopBot()
        {
            Log("StopBot");
            StopBotInternal(true);
        }

        public void SetSpeedFactor(float value)
        {
            speedFactor = Mathf.Max(0.1f, value);
            Log($"SetSpeedFactor -> {speedFactor:0.00}");
            NotifyStateChanged();
        }

        public void SetAutoStartOnEnable(bool value)
        {
            autoStartOnEnable = value;
            Log($"SetAutoStartOnEnable -> {autoStartOnEnable}");
            NotifyStateChanged();
        }

        public bool TryPickSingleMove()
        {
            if (opponentBoard == null || opponentBoard.IsFinished || opponentBoard.IsResolvingPair)
                return false;

            UpdateAdaptiveSkill();

            BattleTile first = ChooseFirstTile();

            if (first == null)
            {
                Log("TryPickSingleMove failed: first tile not found");
                return false;
            }

            Log($"TryPickSingleMove -> first '{first.name}'");
            bool firstOk = opponentBoard.TrySelectTile(first);
            Log($"Select first tile result = {firstOk}");
            if (!firstOk)
                return false;

            BotPickedTile?.Invoke(this, first);

            BattleTile second = ChooseSecondTile(first);

            if (second == null)
            {
                Log("TryPickSingleMove failed: second tile not found");
                return false;
            }

            Log($"TryPickSingleMove -> second '{second.name}'");
            bool secondOk = opponentBoard.TrySelectTile(second);
            Log($"Select second tile result = {secondOk}");

            if (secondOk)
                BotPickedTile?.Invoke(this, second);

            NotifyStateChanged();
            return secondOk;
        }

        private void BindBoard()
        {
            if (opponentBoard == null)
                return;

            Log($"BindBoard -> {opponentBoard.name}");

            opponentBoard.Cleared -= HandleBoardFinished;
            opponentBoard.Failed -= HandleBoardFinished;
            opponentBoard.BuildStarted -= HandleBoardBuildStarted;
            opponentBoard.BuildCompleted -= HandleBoardBuildCompleted;
            opponentBoard.TileRevealed -= HandleTileRevealed;
            opponentBoard.PairMatched -= HandlePairMatched;
            opponentBoard.PairMismatched -= HandlePairMismatched;

            opponentBoard.Cleared += HandleBoardFinished;
            opponentBoard.Failed += HandleBoardFinished;
            opponentBoard.BuildStarted += HandleBoardBuildStarted;
            opponentBoard.BuildCompleted += HandleBoardBuildCompleted;
            opponentBoard.TileRevealed += HandleTileRevealed;
            opponentBoard.PairMatched += HandlePairMatched;
            opponentBoard.PairMismatched += HandlePairMismatched;
        }

        private void UnbindBoard()
        {
            if (opponentBoard == null)
                return;

            Log($"UnbindBoard -> {opponentBoard.name}");

            opponentBoard.Cleared -= HandleBoardFinished;
            opponentBoard.Failed -= HandleBoardFinished;
            opponentBoard.BuildStarted -= HandleBoardBuildStarted;
            opponentBoard.BuildCompleted -= HandleBoardBuildCompleted;
            opponentBoard.TileRevealed -= HandleTileRevealed;
            opponentBoard.PairMatched -= HandlePairMatched;
            opponentBoard.PairMismatched -= HandlePairMismatched;
        }

        private void BindPlayerBoard()
        {
            if (playerBoard == null)
                return;

            Log($"BindPlayerBoard -> {playerBoard.name}");

            playerBoard.PairMatched -= HandlePlayerPairMatched;
            playerBoard.PairMismatched -= HandlePlayerPairMismatched;
            playerBoard.BuildStarted -= HandlePlayerBuildStarted;

            playerBoard.PairMatched += HandlePlayerPairMatched;
            playerBoard.PairMismatched += HandlePlayerPairMismatched;
            playerBoard.BuildStarted += HandlePlayerBuildStarted;
        }

        private void UnbindPlayerBoard()
        {
            if (playerBoard == null)
                return;

            Log($"UnbindPlayerBoard -> {playerBoard.name}");

            playerBoard.PairMatched -= HandlePlayerPairMatched;
            playerBoard.PairMismatched -= HandlePlayerPairMismatched;
            playerBoard.BuildStarted -= HandlePlayerBuildStarted;
        }

        private void BindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.DamageApplied -= HandleDamageApplied;
            combatSystem.CombatStarted -= HandleCombatStarted;
            combatSystem.DamageApplied += HandleDamageApplied;
            combatSystem.CombatStarted += HandleCombatStarted;
        }

        private void UnbindCombatSystem()
        {
            if (combatSystem == null)
                return;

            combatSystem.DamageApplied -= HandleDamageApplied;
            combatSystem.CombatStarted -= HandleCombatStarted;
        }

        private void HandleBoardFinished(BattleBoard board)
        {
            Log($"HandleBoardFinished -> {(board != null ? board.name : "null")}");
            activeRound = false;
            StopBotInternal(true);
        }

        private void HandleBoardBuildStarted(BattleBoard board)
        {
            Log($"HandleBoardBuildStarted -> {(board != null ? board.name : "null")}");
            ClearMemory();
            ResetAdaptiveRoundStats();
            activeRound = false;
            StopBotInternal(false);
        }

        private void HandleBoardBuildCompleted(BattleBoard board)
        {
            Log($"HandleBoardBuildCompleted -> {(board != null ? board.name : "null")}");

            if (board != null)
            {
                Log(
                    $"Board built | " +
                    $"IsBuilt={board.IsBuilt} | " +
                    $"IsFinished={board.IsFinished} | " +
                    $"ActiveTileCount={board.ActiveTileCount}");
            }

            if (autoStartOnEnable && !IsLocalWifiBattleActive())
            {
                Log("AutoStartOnEnable after build -> RestartBot");
                RestartBot();
            }
        }

        private void HandleTileRevealed(BattleBoard board, BattleTile tile)
        {
            if (board != opponentBoard || tile == null)
                return;

            if (Roll(memoryDropChance))
            {
                ForgetTile(tile);
                Log($"Memory missed: {tile.name}");
                return;
            }

            memory[tile] = tile.Id;
            Log($"Memory learned: {tile.name} -> {tile.Id}");
        }

        private void HandlePairMatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != opponentBoard)
                return;

            ForgetTile(first);
            ForgetTile(second);
            botMatchCount++;
            UpdateAdaptiveSkill();
            Log($"PairMatched -> forget '{first?.name}' and '{second?.name}'");
        }

        private void HandlePairMismatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != opponentBoard)
                return;

            if (first != null)
            {
                if (Roll(memoryDropChance))
                    ForgetTile(first);
                else
                    memory[first] = first.Id;
            }

            if (second != null)
            {
                if (Roll(memoryDropChance))
                    ForgetTile(second);
                else
                    memory[second] = second.Id;
            }

            botMismatchCount++;
            UpdateAdaptiveSkill();
            Log($"PairMismatched -> keep memory '{first?.name}', '{second?.name}'");
        }

        private void HandlePlayerBuildStarted(BattleBoard board)
        {
            ResetAdaptiveRoundStats();
        }

        private void HandlePlayerPairMatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != playerBoard)
                return;

            float now = Time.time;
            if (lastPlayerPairTime > 0f)
            {
                float seconds = Mathf.Max(0.1f, now - lastPlayerPairTime);
                playerPairSecondsEma = playerPairSecondsEma < 0f
                    ? seconds
                    : Mathf.Lerp(playerPairSecondsEma, seconds, 0.35f);
            }

            lastPlayerPairTime = now;
            playerMatchCount++;
            UpdateAdaptiveSkill();
        }

        private void HandlePlayerPairMismatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != playerBoard)
                return;

            playerMismatchCount++;
            UpdateAdaptiveSkill();
        }

        private void HandleCombatStarted(BattleCombatSystem _)
        {
            ResetAdaptiveRoundStats();
            UpdateAdaptiveSkill();
        }

        private void HandleDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int damage, int hpAfter)
        {
            UpdateAdaptiveSkill();
        }

        private IEnumerator BotLoop()
        {
            float startDelay = UnityEngine.Random.Range(minThinkDelay, maxThinkDelay);
            Log($"BotLoop started | initial delay={startDelay:0.00}");
            yield return WaitScaled(startDelay);

            while (activeRound && opponentBoard != null && !opponentBoard.IsFinished)
            {
                if (opponentBoard.IsResolvingPair)
                {
                    if (verboseLoopLogs)
                        Log("Board is resolving pair, waiting...");
                    yield return WaitScaled(retryDelay);
                    continue;
                }

                if (verboseLoopLogs)
                {
                    Log(
                        $"BotLoop tick | " +
                        $"activeRound={activeRound} | " +
                        $"boardFinished={opponentBoard.IsFinished} | " +
                        $"activeTiles={opponentBoard.ActiveTileCount} | " +
                        $"memory={memory.Count}");
                }

                UpdateAdaptiveSkill();

                BattleTile first = ChooseFirstTile();

                if (first == null)
                {
                    Log("No clickable closed first tile found, retrying...");
                    yield return WaitScaled(retryDelay);
                    continue;
                }

                Log($"First choice: '{first.name}'");

                float pickDelay1 = UnityEngine.Random.Range(minPickDelay, maxPickDelay);
                yield return WaitScaled(pickDelay1);

                bool firstSelected = opponentBoard.TrySelectTile(first);
                Log($"TrySelectTile(first='{first.name}') -> {firstSelected}");

                if (!firstSelected)
                {
                    yield return WaitScaled(retryDelay);
                    continue;
                }

                BotPickedTile?.Invoke(this, first);
                NotifyStateChanged();

                BattleTile second = ChooseSecondTile(first);

                if (second == null)
                {
                    Log("No second tile available, retrying...");
                    yield return WaitScaled(retryDelay);
                    continue;
                }

                Log($"Second choice: '{second.name}'");

                float pickDelay2 = UnityEngine.Random.Range(minPickDelay, maxPickDelay);
                yield return WaitScaled(pickDelay2);

                bool secondSelected = opponentBoard.TrySelectTile(second);
                Log($"TrySelectTile(second='{second.name}') -> {secondSelected}");

                if (secondSelected)
                {
                    BotPickedTile?.Invoke(this, second);
                    NotifyStateChanged();
                }

                float thinkDelay = UnityEngine.Random.Range(minThinkDelay, maxThinkDelay);
                yield return WaitScaled(thinkDelay);
            }

            Log(
                $"BotLoop ended | " +
                $"activeRound={activeRound} | " +
                $"boardNull={opponentBoard == null} | " +
                $"boardFinished={(opponentBoard != null && opponentBoard.IsFinished)}");

            botRoutine = null;
            NotifyStateChanged();
        }

        private BattleTile FindRememberedPairFirst()
        {
            List<BattleTile> clickable = opponentBoard != null ? opponentBoard.GetClickableClosedTiles() : null;
            if (clickable == null || clickable.Count == 0)
                return null;

            for (int i = 0; i < clickable.Count; i++)
            {
                BattleTile a = clickable[i];
                if (a == null)
                    continue;

                if (!memory.TryGetValue(a, out string aId))
                    continue;

                for (int j = i + 1; j < clickable.Count; j++)
                {
                    BattleTile b = clickable[j];
                    if (b == null)
                        continue;

                    if (!memory.TryGetValue(b, out string bId))
                        continue;

                    if (string.Equals(aId, bId, StringComparison.Ordinal))
                        return a;
                }
            }

            return null;
        }

        private BattleTile ChooseFirstTile()
        {
            if (Roll(GetKnownPairUseChance()))
            {
                BattleTile remembered = FindRememberedPairFirst();
                if (remembered != null)
                    return remembered;
            }

            return GetRandomClosedClickableTile(null);
        }

        private BattleTile ChooseSecondTile(BattleTile first)
        {
            if (first == null)
                return null;

            if (!Roll(GetMistakeChance()) && Roll(GetKnownPairUseChance()))
            {
                BattleTile known = FindKnownMatchFor(first);
                if (known != null)
                    return known;
            }

            BattleTile exploratory = GetRandomUnknownClosedClickableTile(first);
            return exploratory != null ? exploratory : GetRandomClosedClickableTile(first);
        }

        private BattleTile FindKnownMatchFor(BattleTile revealedTile)
        {
            if (revealedTile == null || opponentBoard == null)
                return null;

            if (!memory.TryGetValue(revealedTile, out string id))
                id = revealedTile.Id;

            List<BattleTile> clickable = opponentBoard.GetClickableClosedTiles();
            if (clickable == null || clickable.Count == 0)
                return null;

            for (int i = 0; i < clickable.Count; i++)
            {
                BattleTile candidate = clickable[i];
                if (candidate == null || candidate == revealedTile)
                    continue;

                if (!memory.TryGetValue(candidate, out string candidateId))
                    continue;

                if (string.Equals(candidateId, id, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        private BattleTile GetRandomClosedClickableTile(BattleTile exclude)
        {
            if (opponentBoard == null)
                return null;

            List<BattleTile> clickable = opponentBoard.GetClickableClosedTiles();
            if (clickable == null || clickable.Count == 0)
                return null;

            if (exclude != null)
                clickable.Remove(exclude);

            if (clickable.Count == 0)
                return null;

            int index = UnityEngine.Random.Range(0, clickable.Count);
            return clickable[index];
        }

        private BattleTile GetRandomUnknownClosedClickableTile(BattleTile exclude)
        {
            if (opponentBoard == null)
                return null;

            List<BattleTile> clickable = opponentBoard.GetClickableClosedTiles();
            if (clickable == null || clickable.Count == 0)
                return null;

            for (int i = clickable.Count - 1; i >= 0; i--)
            {
                BattleTile tile = clickable[i];
                if (tile == null || tile == exclude || memory.ContainsKey(tile))
                    clickable.RemoveAt(i);
            }

            if (clickable.Count == 0)
                return null;

            int index = UnityEngine.Random.Range(0, clickable.Count);
            return clickable[index];
        }

        private void ForgetTile(BattleTile tile)
        {
            if (tile == null)
                return;

            memory.Remove(tile);
        }

        private void ClearMemory()
        {
            memory.Clear();
            Log("Memory cleared");
        }

        private void ResetAdaptiveRoundStats()
        {
            playerPairSecondsEma = -1f;
            lastPlayerPairTime = -1f;
            playerMatchCount = 0;
            playerMismatchCount = 0;
            botMatchCount = 0;
            botMismatchCount = 0;
            adaptiveSkill = Mathf.Clamp(baseSkill, minAdaptiveSkill, maxAdaptiveSkill);
        }

        private void UpdateAdaptiveSkill()
        {
            if (!useAdaptiveDifficulty)
            {
                adaptiveSkill = Mathf.Clamp(baseSkill, minAdaptiveSkill, maxAdaptiveSkill);
                return;
            }

            float target = baseSkill;

            if (combatSystem != null && combatSystem.IsCombatStarted)
            {
                float playerHp01 = combatSystem.MaxPlayerHp > 0
                    ? combatSystem.PlayerHp / (float)combatSystem.MaxPlayerHp
                    : 1f;
                float opponentHp01 = combatSystem.MaxOpponentHp > 0
                    ? combatSystem.OpponentHp / (float)combatSystem.MaxOpponentHp
                    : 1f;

                float botLead = opponentHp01 - playerHp01;
                target -= botLead * 0.36f;
            }

            if (playerPairSecondsEma > 0f)
            {
                float pace = Mathf.Clamp((playerTargetPairSeconds - playerPairSecondsEma) / playerTargetPairSeconds, -1f, 1f);
                target += pace * 0.2f;
            }

            int playerAttempts = playerMatchCount + playerMismatchCount;
            if (playerAttempts >= 2)
            {
                float playerAccuracy = playerMatchCount / (float)playerAttempts;
                target += (playerAccuracy - 0.5f) * 0.18f;
            }

            int botAttempts = botMatchCount + botMismatchCount;
            if (botAttempts >= 2)
            {
                float botAccuracy = botMatchCount / (float)botAttempts;
                target -= (botAccuracy - 0.5f) * 0.16f;
            }

            target = Mathf.Clamp(target, minAdaptiveSkill, maxAdaptiveSkill);
            adaptiveSkill = Mathf.Lerp(adaptiveSkill <= 0f ? baseSkill : adaptiveSkill, target, adaptiveSmoothing);
        }

        private BattleBoard FindOpponentBoard()
        {
            BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
            for (int i = 0; i < boards.Length; i++)
            {
                if (boards[i] != null && boards[i].Side == BattleBoardSide.Opponent)
                    return boards[i];
            }

            return null;
        }

        private void AutoResolveLinks()
        {
            if (opponentBoard == null || playerBoard == null)
            {
                BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
                for (int i = 0; i < boards.Length; i++)
                {
                    BattleBoard board = boards[i];
                    if (board == null)
                        continue;

                    if (board.Side == BattleBoardSide.Opponent && opponentBoard == null)
                        opponentBoard = board;
                    else if (board.Side == BattleBoardSide.Player && playerBoard == null)
                        playerBoard = board;
                }
            }

            if (combatSystem == null)
                combatSystem = FindAnyObjectByType<BattleCombatSystem>(FindObjectsInactive.Exclude);
        }

        private void StopBotInternal(bool invokeEvent)
        {
            Log($"StopBotInternal | invokeEvent={invokeEvent}");

            if (botRoutine != null)
            {
                StopCoroutine(botRoutine);
                botRoutine = null;
            }

            activeRound = false;

            if (invokeEvent)
                BotStopped?.Invoke(this);

            NotifyStateChanged();
        }

        private WaitForSeconds WaitScaled(float time)
        {
            float delayMultiplier = useAdaptiveDifficulty
                ? Mathf.Lerp(maxDelayMultiplier, minDelayMultiplier, Mathf.Clamp01(adaptiveSkill))
                : 1f;
            float scaled = Mathf.Max(0.02f, time * delayMultiplier / Mathf.Max(0.1f, speedFactor));
            return new WaitForSeconds(scaled);
        }

        private float GetMistakeChance()
        {
            if (!useAdaptiveDifficulty)
                return minMistakeChance;

            return Mathf.Lerp(maxMistakeChance, minMistakeChance, Mathf.Clamp01(adaptiveSkill));
        }

        private float GetKnownPairUseChance()
        {
            if (!useAdaptiveDifficulty)
                return maxKnownPairUseChance;

            return Mathf.Lerp(minKnownPairUseChance, maxKnownPairUseChance, Mathf.Clamp01(adaptiveSkill));
        }

        private bool Roll(float chance)
        {
            chance = Mathf.Clamp01(chance);
            if (chance <= 0f)
                return false;

            if (chance >= 1f)
                return true;

            return UnityEngine.Random.value <= chance;
        }

        private static bool IsLocalWifiBattleActive()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch;
        }

        private float ResolveSpeedFactor(string rankTier, int rankPoints)
        {
            string tier = string.IsNullOrWhiteSpace(rankTier) ? string.Empty : rankTier.Trim().ToLowerInvariant();

            if (tier == "master")
                return masterSpeed;
            if (tier == "jade")
                return jadeSpeed;
            if (tier == "gold")
                return goldSpeed;
            if (tier == "silver")
                return silverSpeed;
            if (tier == "bronze")
                return bronzeSpeed;

            if (rankPoints >= 800)
                return masterSpeed;
            if (rankPoints >= 500)
                return jadeSpeed;
            if (rankPoints >= 250)
                return goldSpeed;
            if (rankPoints >= 100)
                return silverSpeed;

            return bronzeSpeed;
        }

        private void NotifyStateChanged()
        {
            BotStateChanged?.Invoke(this);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[BattleBotController] {message}", this);
        }
    }
}
