using System.Collections.Generic;
using MahjongGame.Multiplayer;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class OnlineRankedBattleMatchSync : MonoBehaviour
    {
        [SerializeField] private BattleMatchController matchController;
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleBotController botController;
        [SerializeField] private BattleCombatSystem combatSystem;
        [SerializeField] private bool debugLogs = true;

        private bool applyingRemotePick;
        private bool applyingRemoteDamage;
        private bool boardManifestSent;
        private bool playerServerBoardApplied;
        private readonly Queue<PendingTilePick> pendingTilePicks = new Queue<PendingTilePick>();
        private readonly Queue<PendingDamage> pendingDamage = new Queue<PendingDamage>();

        private void Awake()
        {
            AutoResolveLinks();
        }

        private void OnEnable()
        {
            AutoResolveLinks();
            Bind();
            ConfigureOnlineRankedBattle();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Configure(BattleMatchController controller, BattleBoard player, BattleBoard opponent, BattleBotController bot, BattleCombatSystem combat)
        {
            Unbind();
            matchController = controller;
            playerBoard = player;
            opponentBoard = opponent;
            botController = bot;
            combatSystem = combat;
            Bind();
            ConfigureOnlineRankedBattle();
        }

        private void Bind()
        {
            if (playerBoard != null)
            {
                playerBoard.TileSelectionRequested -= HandleLocalTileSelectionRequested;
                playerBoard.BuildCompleted -= HandlePlayerBoardBuildCompleted;
                playerBoard.TileSelectionRequested += HandleLocalTileSelectionRequested;
                playerBoard.BuildCompleted += HandlePlayerBoardBuildCompleted;
            }

            if (opponentBoard != null)
            {
                opponentBoard.PairMatched -= HandleRemoteBoardPairResolved;
                opponentBoard.PairMismatched -= HandleRemoteBoardPairResolved;
                opponentBoard.BuildCompleted -= HandleOpponentBoardBuildCompleted;
                opponentBoard.PairMatched += HandleRemoteBoardPairResolved;
                opponentBoard.PairMismatched += HandleRemoteBoardPairResolved;
                opponentBoard.BuildCompleted += HandleOpponentBoardBuildCompleted;
            }

            if (combatSystem != null)
            {
                combatSystem.DamageApplied -= HandleLocalDamageApplied;
                combatSystem.CombatStarted -= HandleCombatStarted;
                combatSystem.DamageApplied += HandleLocalDamageApplied;
                combatSystem.CombatStarted += HandleCombatStarted;
            }

            OnlineRankedBattleNetwork network = OnlineRankedBattleNetwork.EnsureInstance();
            network.RemoteTilePicked -= HandleRemoteTilePicked;
            network.RemoteDamageApplied -= HandleRemoteDamageApplied;
            network.ServerEventReceived -= HandleServerEventReceived;
            network.RemoteTilePicked += HandleRemoteTilePicked;
            network.RemoteDamageApplied += HandleRemoteDamageApplied;
            network.ServerEventReceived += HandleServerEventReceived;
            network.StartMatchEventPolling();
        }

        private void Unbind()
        {
            if (playerBoard != null)
            {
                playerBoard.TileSelectionRequested -= HandleLocalTileSelectionRequested;
                playerBoard.BuildCompleted -= HandlePlayerBoardBuildCompleted;
                playerBoard.SetRequireExternalSelectionApproval(false);
            }

            if (opponentBoard != null)
            {
                opponentBoard.PairMatched -= HandleRemoteBoardPairResolved;
                opponentBoard.PairMismatched -= HandleRemoteBoardPairResolved;
                opponentBoard.BuildCompleted -= HandleOpponentBoardBuildCompleted;
            }

            if (combatSystem != null)
            {
                combatSystem.SetNetworkPairDamageSuppressedForBoth(false);
                combatSystem.DamageApplied -= HandleLocalDamageApplied;
                combatSystem.CombatStarted -= HandleCombatStarted;
            }

            if (OnlineRankedBattleNetwork.I != null)
            {
                OnlineRankedBattleNetwork.I.RemoteTilePicked -= HandleRemoteTilePicked;
                OnlineRankedBattleNetwork.I.RemoteDamageApplied -= HandleRemoteDamageApplied;
                OnlineRankedBattleNetwork.I.ServerEventReceived -= HandleServerEventReceived;
                OnlineRankedBattleNetwork.I.StopMatchEventPolling();
            }
        }

        private void ConfigureOnlineRankedBattle()
        {
            if (!IsOnlineRankedBattle())
                return;

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }

            if (opponentBoard != null)
                opponentBoard.SetAllowPlayerInput(false);

            if (playerBoard != null)
                playerBoard.SetRequireExternalSelectionApproval(true);

            if (playerBoard != null)
                playerBoard.SetInteractionLocked(!playerServerBoardApplied);

            if (combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressedForBoth(true);

            TrySendBoardManifest();
            Log("Online ranked battle sync enabled.");
        }

        private void HandleLocalTileSelectionRequested(BattleBoard board, BattleTile tile)
        {
            if (!IsOnlineRankedBattle() || applyingRemotePick || board != playerBoard || tile == null)
                return;

            int tileIndex = playerBoard.GetSpawnedTileIndex(tile);
            OnlineRankedBattleNetwork.EnsureInstance().SendTilePick(tileIndex, tile.Id);
            Log($"Sent tile pick: {tileIndex} / {tile.Id}");
        }

        private void HandleRemoteTilePicked(int tileIndex, string tileId)
        {
            if (!IsOnlineRankedBattle() || (tileIndex < 0 && string.IsNullOrWhiteSpace(tileId)))
                return;

            if (opponentBoard == null || !opponentBoard.IsBuilt)
            {
                pendingTilePicks.Enqueue(new PendingTilePick { TileIndex = tileIndex, TileId = tileId });
                return;
            }

            applyingRemotePick = true;
            if (combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressed(true);

            bool picked = opponentBoard.TryRevealServerApprovedTileBySpawnedIndex(tileIndex, tileId, out BattleTile _);
            applyingRemotePick = false;
            if (!picked && combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressed(false);

            Log($"Remote tile pick '{tileIndex} / {tileId}' -> {picked}");
        }

        private void HandleRemoteBoardPairResolved(BattleBoard board, BattleTile first, BattleTile second)
        {
            if (board == opponentBoard && combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressed(false);
        }

        private void HandleOpponentBoardBuildCompleted(BattleBoard board)
        {
            if (board == opponentBoard)
                DrainPendingEvents();
        }

        private void HandlePlayerBoardBuildCompleted(BattleBoard board)
        {
            if (board == playerBoard)
                TrySendBoardManifest();
        }

        private void HandleLocalDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int damage, int hpAfter)
        {
            if (!IsOnlineRankedBattle() || applyingRemoteDamage || damage <= 0)
                return;

            OnlineRankedBattleNetwork.EnsureInstance().SendDamage(targetSide, damage);
            Log($"Sent damage | Target={targetSide} Damage={damage}");
        }

        private void HandleRemoteDamageApplied(BattleBoardSide senderTargetSide, int amount)
        {
            if (!IsOnlineRankedBattle() || amount <= 0)
                return;

            if (combatSystem == null || !combatSystem.IsCombatStarted)
            {
                pendingDamage.Enqueue(new PendingDamage { TargetSide = senderTargetSide, Amount = amount });
                return;
            }

            applyingRemoteDamage = true;

            if (senderTargetSide == BattleBoardSide.Opponent)
                combatSystem.ApplyDamageToOpponent(amount);
            else
                combatSystem.ApplyDamageToPlayer(amount);

            applyingRemoteDamage = false;
            Log($"Applied remote damage | SenderTarget={senderTargetSide} Damage={amount}");
        }

        private void HandleCombatStarted(BattleCombatSystem _)
        {
            TrySendBoardManifest();
            DrainPendingEvents();
        }

        private void HandleServerEventReceived(OnlineRankedBattleNetwork.RankedServerEvent item)
        {
            if (item == null || !IsOnlineRankedBattle())
                return;

            if (string.Equals(item.type, "finish", System.StringComparison.OrdinalIgnoreCase))
            {
                bool playerWon = item.winnerIndex == OnlineRankedBattleNetwork.I.PlayerIndex;
                matchController?.ForceFinishMatch(playerWon);
                return;
            }

            if (string.Equals(item.type, "board", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyServerBoardManifest(item);
                return;
            }

            if (string.Equals(item.type, "reveal", System.StringComparison.OrdinalIgnoreCase))
            {
                BattleBoard targetBoard = item.actorIndex == OnlineRankedBattleNetwork.I.PlayerIndex
                    ? playerBoard
                    : opponentBoard;
                ApplyServerReveal(targetBoard, item.tileIndex, item.tileId);
            }
        }

        private void ApplyServerReveal(BattleBoard board, int tileIndex, string tileId)
        {
            if (board == null || !board.IsBuilt)
            {
                if (board == opponentBoard)
                    pendingTilePicks.Enqueue(new PendingTilePick { TileIndex = tileIndex, TileId = tileId });
                return;
            }

            applyingRemotePick = true;
            if (combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressedForBoth(true);

            bool picked = board.TryRevealServerApprovedTileBySpawnedIndex(tileIndex, tileId, out BattleTile _);
            applyingRemotePick = false;

            Log($"Server reveal '{tileIndex} / {tileId}' -> {picked}");
        }

        private void TrySendBoardManifest()
        {
            if (boardManifestSent ||
                !IsOnlineRankedBattle() ||
                playerBoard == null ||
                !playerBoard.IsBuilt ||
                combatSystem == null ||
                !combatSystem.IsCombatStarted)
            {
                return;
            }

            OnlineRankedBattleNetwork.RankedBoardSlot[] slots = BuildBoardSlots(playerBoard);
            string[] tilePool = BuildTilePool();
            if (slots == null || slots.Length == 0 || tilePool == null || tilePool.Length == 0)
                return;

            boardManifestSent = true;
            OnlineRankedBattleNetwork.EnsureInstance().RequestServerBoard(
                slots,
                tilePool,
                combatSystem.MaxPlayerHp,
                combatSystem.DamagePerPair);
            Log($"Requested authoritative board | Slots={slots.Length} TilePool={tilePool.Length}");
        }

        private void ApplyServerBoardManifest(OnlineRankedBattleNetwork.RankedServerEvent item)
        {
            if (item == null || item.tiles == null || item.tiles.Length == 0)
                return;

            BattleBoard targetBoard = item.actorIndex == OnlineRankedBattleNetwork.I.PlayerIndex
                ? playerBoard
                : opponentBoard;

            if (targetBoard == null || !targetBoard.IsBuilt)
                return;

            string[] ids = new string[item.tiles.Length];
            for (int i = 0; i < item.tiles.Length; i++)
                ids[i] = item.tiles[i] != null ? item.tiles[i].id : string.Empty;

            targetBoard.ApplyServerTileIds(ids);

            if (targetBoard == playerBoard)
            {
                playerServerBoardApplied = true;
                playerBoard.SetInteractionLocked(false);
            }

            Log($"Applied authoritative board | Actor={item.actorIndex} Tiles={ids.Length}");
        }

        private static OnlineRankedBattleNetwork.RankedBoardSlot[] BuildBoardSlots(BattleBoard board)
        {
            if (board == null || board.SpawnedTiles == null)
                return null;

            OnlineRankedBattleNetwork.RankedBoardSlot[] slots =
                new OnlineRankedBattleNetwork.RankedBoardSlot[board.SpawnedTiles.Count];

            for (int i = 0; i < board.SpawnedTiles.Count; i++)
            {
                BattleTile tile = board.SpawnedTiles[i];
                BattleBoard.BattleTileNode node = tile != null ? board.GetNodePublic(tile) : null;
                LayoutSlot slot = node != null ? node.Slot : null;

                slots[i] = new OnlineRankedBattleNetwork.RankedBoardSlot
                {
                    x = slot != null ? slot.X : 0,
                    y = slot != null ? slot.Y : 0,
                    z = slot != null ? slot.Z : 0
                };
            }

            return slots;
        }

        private static string[] BuildTilePool()
        {
            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
            if (store == null || store.BattleTiles == null)
                return null;

            List<string> ids = new List<string>();
            for (int i = 0; i < store.BattleTiles.Count; i++)
            {
                BattleTileData data = store.BattleTiles[i];
                if (data != null && data.Prefab != null && !string.IsNullOrWhiteSpace(data.Id))
                    ids.Add(data.Id);
            }

            return ids.ToArray();
        }

        private void DrainPendingEvents()
        {
            if (!IsOnlineRankedBattle())
                return;

            while (opponentBoard != null && opponentBoard.IsBuilt && pendingTilePicks.Count > 0)
            {
                PendingTilePick item = pendingTilePicks.Dequeue();
                HandleRemoteTilePicked(item.TileIndex, item.TileId);
            }

            while (combatSystem != null && combatSystem.IsCombatStarted && pendingDamage.Count > 0)
            {
                PendingDamage item = pendingDamage.Dequeue();
                HandleRemoteDamageApplied(item.TargetSide, item.Amount);
            }
        }

        private void AutoResolveLinks()
        {
            if (matchController == null)
                matchController = GetComponent<BattleMatchController>();
            if (botController == null)
                botController = GetComponent<BattleBotController>();
            if (combatSystem == null)
                combatSystem = GetComponent<BattleCombatSystem>();

            if (playerBoard == null || opponentBoard == null)
            {
                BattleBoard[] boards = FindObjectsByType<BattleBoard>(FindObjectsInactive.Exclude);
                for (int i = 0; i < boards.Length; i++)
                {
                    BattleBoard board = boards[i];
                    if (board == null)
                        continue;

                    if (board.Side == BattleBoardSide.Player && playerBoard == null)
                        playerBoard = board;
                    else if (board.Side == BattleBoardSide.Opponent && opponentBoard == null)
                        opponentBoard = board;
                }
            }
        }

        private static bool IsOnlineRankedBattle()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.RankedMatch &&
                   OnlineRankedBattleNetwork.I != null &&
                   OnlineRankedBattleNetwork.I.IsInMatch;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log("[OnlineRankedBattleMatchSync] " + message, this);
        }

        private struct PendingTilePick
        {
            public int TileIndex;
            public string TileId;
        }

        private struct PendingDamage
        {
            public BattleBoardSide TargetSide;
            public int Amount;
        }
    }
}
