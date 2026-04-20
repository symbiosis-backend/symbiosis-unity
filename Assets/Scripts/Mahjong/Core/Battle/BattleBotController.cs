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

        [Header("State")]
        [SerializeField] private bool autoStartOnEnable = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private bool verboseLoopLogs = false;

        private readonly Dictionary<BattleTile, string> memory = new();

        private Coroutine botRoutine;
        private bool activeRound;
        private float speedFactor = 1f;

        public BattleBoard OpponentBoard => opponentBoard;
        public bool IsActiveRound => activeRound;
        public bool IsRunning => botRoutine != null;
        public float SpeedFactor => speedFactor;

        private void OnEnable()
        {
            Log("OnEnable");
            BindBoard();

            if (autoStartOnEnable)
            {
                Log("AutoStartOnEnable = true, trying to start bot");
                TryStartBot();
            }
        }

        private void OnDisable()
        {
            Log("OnDisable");
            UnbindBoard();
            StopBot();
        }

        private void Start()
        {
            Log("Start");
            if (autoStartOnEnable && !IsRunning)
            {
                Log("Start -> TryStartBot");
                TryStartBot();
            }
        }

        public void SetBoard(BattleBoard board)
        {
            if (opponentBoard == board)
                return;

            Log($"SetBoard -> {(board != null ? board.name : "null")}");

            UnbindBoard();
            opponentBoard = board;
            BindBoard();
            ClearMemory();
            NotifyStateChanged();
        }

        public void RestartBot()
        {
            Log("RestartBot");
            StopBot();
            TryStartBot();
        }

        public void TryStartBot()
        {
            Log("TryStartBot called");

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

            BattleTile first = FindRememberedPairFirst();
            if (first == null)
                first = GetRandomClosedClickableTile(null);

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

            BattleTile second = FindKnownMatchFor(first);
            if (second == null)
                second = GetRandomClosedClickableTile(first);

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

            if (autoStartOnEnable)
            {
                Log("AutoStartOnEnable after build -> RestartBot");
                RestartBot();
            }
        }

        private void HandleTileRevealed(BattleBoard board, BattleTile tile)
        {
            if (board != opponentBoard || tile == null)
                return;

            memory[tile] = tile.Id;
            Log($"Memory learned: {tile.name} -> {tile.Id}");
        }

        private void HandlePairMatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != opponentBoard)
                return;

            ForgetTile(first);
            ForgetTile(second);
            Log($"PairMatched -> forget '{first?.name}' and '{second?.name}'");
        }

        private void HandlePairMismatched(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board != opponentBoard)
                return;

            if (first != null)
                memory[first] = first.Id;

            if (second != null)
                memory[second] = second.Id;

            Log($"PairMismatched -> keep memory '{first?.name}', '{second?.name}'");
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

                BattleTile first = FindRememberedPairFirst();
                if (first == null)
                    first = GetRandomClosedClickableTile(null);

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

                BattleTile second = FindKnownMatchFor(first);
                if (second == null)
                    second = GetRandomClosedClickableTile(first);

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
            float scaled = Mathf.Max(0.02f, time / Mathf.Max(0.1f, speedFactor));
            return new WaitForSeconds(scaled);
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