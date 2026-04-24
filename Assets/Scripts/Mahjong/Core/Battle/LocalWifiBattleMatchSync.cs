using MahjongGame.Multiplayer;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class LocalWifiBattleMatchSync : MonoBehaviour
    {
        [SerializeField] private BattleMatchController matchController;
        [SerializeField] private BattleBoard playerBoard;
        [SerializeField] private BattleBoard opponentBoard;
        [SerializeField] private BattleBotController botController;
        [SerializeField] private BattleCombatSystem combatSystem;
        [SerializeField] private bool debugLogs = true;

        private bool applyingRemotePick;
        private bool applyingRemoteDamage;

        private void Awake()
        {
            AutoResolveLinks();
        }

        private void OnEnable()
        {
            AutoResolveLinks();
            Bind();
            ConfigureLocalWifiBattle();
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
            ConfigureLocalWifiBattle();
        }

        private void Bind()
        {
            if (playerBoard != null)
            {
                playerBoard.TileSelected -= HandleLocalTileSelected;
                playerBoard.TileSelected += HandleLocalTileSelected;
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

            LocalWifiBattleNetwork network = LocalWifiBattleNetwork.EnsureInstance();
            network.RemoteTilePicked -= HandleRemoteTilePicked;
            network.RemoteDamageApplied -= HandleRemoteDamageApplied;
            network.RemoteRoundEnded -= HandleRemoteRoundEnded;
            network.ConnectionClosed -= HandleConnectionClosed;
            network.RemoteTilePicked += HandleRemoteTilePicked;
            network.RemoteDamageApplied += HandleRemoteDamageApplied;
            network.RemoteRoundEnded += HandleRemoteRoundEnded;
            network.ConnectionClosed += HandleConnectionClosed;

            DrainPendingNetworkMessages(network);
        }

        private void Unbind()
        {
            if (playerBoard != null)
                playerBoard.TileSelected -= HandleLocalTileSelected;

            if (opponentBoard != null)
            {
                opponentBoard.PairMatched -= HandleRemoteBoardPairResolved;
                opponentBoard.PairMismatched -= HandleRemoteBoardPairResolved;
                opponentBoard.BuildCompleted -= HandleOpponentBoardBuildCompleted;
            }

            if (combatSystem != null)
            {
                combatSystem.SetNetworkPairDamageSuppressed(false);
                combatSystem.DamageApplied -= HandleLocalDamageApplied;
                combatSystem.CombatStarted -= HandleCombatStarted;
            }

            if (LocalWifiBattleNetwork.I != null)
            {
                LocalWifiBattleNetwork.I.RemoteTilePicked -= HandleRemoteTilePicked;
                LocalWifiBattleNetwork.I.RemoteDamageApplied -= HandleRemoteDamageApplied;
                LocalWifiBattleNetwork.I.RemoteRoundEnded -= HandleRemoteRoundEnded;
                LocalWifiBattleNetwork.I.ConnectionClosed -= HandleConnectionClosed;
            }
        }

        private void ConfigureLocalWifiBattle()
        {
            if (!IsLocalWifiBattle())
                return;

            if (botController != null)
            {
                botController.SetAutoStartOnEnable(false);
                botController.StopBot();
            }

            if (opponentBoard != null)
                opponentBoard.SetAllowPlayerInput(false);

            Log("Local Wi-Fi battle sync enabled.");
        }

        private void HandleLocalTileSelected(BattleBoard board, BattleTile tile)
        {
            if (!IsLocalWifiBattle() || applyingRemotePick || board != playerBoard || tile == null)
                return;

            int tileIndex = playerBoard.GetSpawnedTileIndex(tile);
            LocalWifiBattleNetwork.EnsureInstance().SendLocalTilePick(tileIndex, tile.Id);
            Log($"Sent tile pick: {tileIndex} / {tile.Id}");
        }

        private void HandleRemoteTilePicked(int tileIndex, string tileId)
        {
            if (!IsLocalWifiBattle() || opponentBoard == null || (tileIndex < 0 && string.IsNullOrWhiteSpace(tileId)))
                return;

            applyingRemotePick = true;
            if (combatSystem != null)
                combatSystem.SetNetworkPairDamageSuppressed(true);

            bool picked = opponentBoard.TryRevealTileBySpawnedIndex(tileIndex, tileId, out BattleTile _);
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
                DrainPendingNetworkMessages(LocalWifiBattleNetwork.I);
        }

        private void HandleLocalDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int damage, int hpAfter)
        {
            if (!IsLocalWifiBattle() || applyingRemoteDamage || damage <= 0)
                return;

            LocalWifiBattleNetwork.EnsureInstance().SendDamage(targetSide, damage);
            Log($"Sent damage | Target={targetSide} Damage={damage}");
        }

        private void HandleCombatStarted(BattleCombatSystem _)
        {
            DrainPendingNetworkMessages(LocalWifiBattleNetwork.I);
        }

        private void HandleRemoteDamageApplied(BattleBoardSide senderTargetSide, int amount)
        {
            if (!IsLocalWifiBattle() || combatSystem == null || amount <= 0)
                return;

            applyingRemoteDamage = true;

            if (senderTargetSide == BattleBoardSide.Opponent)
                combatSystem.ApplyDamageToPlayer(amount);
            else
                combatSystem.ApplyDamageToOpponent(amount);

            applyingRemoteDamage = false;
            Log($"Applied remote damage | SenderTarget={senderTargetSide} Damage={amount}");
        }

        private void HandleRemoteRoundEnded(int senderRoundNumber, BattleBoardSide senderDeadSide)
        {
            if (!IsLocalWifiBattle() || matchController == null)
                return;

            matchController.HandleLocalWifiRemoteRoundEnded(senderRoundNumber, senderDeadSide);
            Log($"Applied remote round end | Round={senderRoundNumber} SenderDeadSide={senderDeadSide}");
        }

        private void HandleConnectionClosed()
        {
            if (!IsLocalWifiBattle())
                return;

            Debug.LogWarning("[LocalWifiBattleMatchSync] Wi-Fi opponent disconnected.");
        }

        private void DrainPendingNetworkMessages(LocalWifiBattleNetwork network)
        {
            if (network == null ||
                opponentBoard == null ||
                !opponentBoard.IsBuilt ||
                combatSystem == null ||
                !combatSystem.IsCombatStarted)
            {
                return;
            }

            while (network.TryDequeuePendingRemoteTilePick(out int tileIndex, out string tileId))
                HandleRemoteTilePicked(tileIndex, tileId);

            while (network.TryDequeuePendingRemoteDamage(out BattleBoardSide targetSide, out int amount))
                HandleRemoteDamageApplied(targetSide, amount);
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

        private static bool IsLocalWifiBattle()
        {
            return MahjongBattleLobbySession.SelectedMode == MahjongBattleLobbyMode.LocalWifiMatch &&
                   LocalWifiBattleNetwork.I != null &&
                   LocalWifiBattleNetwork.I.IsConnected;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log("[LocalWifiBattleMatchSync] " + message, this);
        }
    }
}
