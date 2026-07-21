using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Networking;
using Dynasty.Legacy.CorrosionCollapse.Players;
using Dynasty.Legacy.CorrosionCollapse.UI;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public sealed class TurnManager : MonoBehaviour
    {
        [SerializeField] private float botDelay = 1f;

        private readonly List<PlayerState> players = new List<PlayerState>();
        private readonly Dictionary<int, PlayerView> playerViews = new Dictionary<int, PlayerView>();

        private BoardGraph graph;
        private BoardBuilder boardBuilder;
        private DiceSystem diceSystem;
        private BoardMover boardMover;
        private TileEffectResolver tileEffectResolver;
        private CorrosionWaveSystem corrosionWaveSystem;
        private CorrosionCollapseHud hud;
        private IServerAuthority serverAuthority;
        private int currentPlayerIndex;
        private bool awaitingLocalRoll;
        private bool turnRunning;
        private bool matchEnded;

        public IReadOnlyList<PlayerState> Players => players;

        public void Initialize(
            BoardBuilder builder,
            IReadOnlyList<PlayerView> views,
            CorrosionCollapseHud gameHud,
            IServerAuthority authority)
        {
            boardBuilder = builder;
            graph = builder.Graph;
            hud = gameHud;
            serverAuthority = authority;
            diceSystem = new DiceSystem(serverAuthority);
            boardMover = new BoardMover(serverAuthority);
            tileEffectResolver = new TileEffectResolver();

            playerViews.Clear();
            foreach (PlayerView view in views)
            {
                playerViews[view.State.playerId] = view;
            }

            corrosionWaveSystem = new CorrosionWaveSystem(graph, players, playerViews, boardMover);

            hud.DiceButton.onClick.RemoveAllListeners();
            hud.DiceButton.onClick.AddListener(RequestLocalRoll);
        }

        public void StartMatch()
        {
            if (!serverAuthority.IsServer || graph == null)
            {
                return;
            }

            players.Clear();
            players.AddRange(playerViews.Values
                .Select(view => view.State)
                .OrderBy(state => state.playerId));
            foreach (PlayerState player in players)
            {
                player.extraRollAvailable = false;
                player.extraRollUsedThisTurn = false;
                player.hasShortcutPass = false;
                player.skipNextTurn = false;
            }

            currentPlayerIndex = 0;
            matchEnded = false;
            Debug.Log("[Game] Match started");
            StartTurn();
        }

        public void StartTurn()
        {
            if (matchEnded)
            {
                return;
            }

            PlayerState current = GetCurrentPlayer();
            if (current == null)
            {
                EndMatch(null);
                return;
            }

            if (!current.CanAct)
            {
                EndTurn(false);
                return;
            }

            if (current.skipNextTurn)
            {
                current.skipNextTurn = false;
                hud.ShowStatus($"{current.nickname}: пропуск хода");
                EndTurn(false);
                return;
            }

            awaitingLocalRoll = !current.isBot && serverAuthority.IsLocalPlayerTurn(current.playerId);
            RefreshHud();

            if (current.isBot)
            {
                StartCoroutine(BotTurnRoutine(current));
            }
        }

        public void RequestLocalRoll()
        {
            PlayerState current = GetCurrentPlayer();
            if (!awaitingLocalRoll || turnRunning || matchEnded || current == null || !current.CanAct)
            {
                return;
            }

            awaitingLocalRoll = false;
            StartCoroutine(ExecuteTurnRoutine(current));
        }

        public void EndTurn(bool extraTurn)
        {
            if (matchEnded)
            {
                return;
            }

            CheckMatchEnd();
            if (matchEnded)
            {
                return;
            }

            if (!extraTurn)
            {
                NextPlayer();
            }

            StartTurn();
        }

        public void NextPlayer()
        {
            for (int i = 0; i < players.Count; i++)
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                if (players[currentPlayerIndex].CanAct)
                {
                    players[currentPlayerIndex].extraRollAvailable = false;
                    players[currentPlayerIndex].extraRollUsedThisTurn = false;
                    break;
                }
            }
        }

        private IEnumerator BotTurnRoutine(PlayerState bot)
        {
            yield return new WaitForSeconds(botDelay);
            yield return ExecuteTurnRoutine(bot);
        }

        private IEnumerator ExecuteTurnRoutine(PlayerState player)
        {
            if (player == null || turnRunning || !serverAuthority.IsServer)
            {
                yield break;
            }

            turnRunning = true;
            RefreshHud();
            DiceRoll roll = diceSystem.RollDice(player.playerId);
            hud.SetDiceText(roll.dice1, roll.dice2);

            if (playerViews.TryGetValue(player.playerId, out PlayerView view))
            {
                yield return boardMover.MoveBySteps(player, view, roll.Sum, ChooseBranch);
                yield return tileEffectResolver.Resolve(player, view, boardMover, corrosionWaveSystem);
                hud.ShowEvent(TileEventLabel(player.currentNode.type));

                if (player.currentNode == graph.finishNode)
                {
                    player.finished = true;
                }

                bool extraTurn = player.extraRollAvailable && !player.extraRollUsedThisTurn && player.CanAct && !player.finished;
                if (extraTurn)
                {
                    player.extraRollAvailable = false;
                    player.extraRollUsedThisTurn = true;
                }

                turnRunning = false;
                RefreshHud();
                EndTurn(extraTurn);
            }
            else
            {
                turnRunning = false;
                EndTurn(false);
            }
        }

        private BoardNode ChooseBranch(PlayerState player, BoardNode current)
        {
            if (current == null || current.nextNodes.Count <= 1)
            {
                return current?.nextNodes.FirstOrDefault();
            }

            List<BoardNode> traversable = current.nextNodes.Where(node => node.IsTraversable).ToList();
            if (traversable.Count == 0)
            {
                return null;
            }

            if (player.isBot)
            {
                BoardNode botShortcut = traversable.FirstOrDefault(node => node.isShortcut);
                if (botShortcut != null && player.hasShortcutPass)
                {
                    player.hasShortcutPass = false;
                    hud.ShowStatus("Shortcut Pass used");
                    return botShortcut;
                }

                return traversable.FirstOrDefault(node => !node.isShortcut) ?? traversable[Random.Range(0, traversable.Count)];
            }

            BoardNode shortcut = traversable.FirstOrDefault(node => node.isShortcut);
            if (shortcut != null && player.hasShortcutPass)
            {
                player.hasShortcutPass = false;
                hud.ShowStatus("Shortcut Pass used");
                return shortcut;
            }

            return traversable
                .Where(node => !node.isShortcut)
                .OrderBy(node => node.progressIndex)
                .FirstOrDefault() ?? traversable.OrderBy(node => node.progressIndex).FirstOrDefault();
        }

        private PlayerState GetCurrentPlayer()
        {
            if (players.Count == 0)
            {
                return null;
            }

            return players[Mathf.Clamp(currentPlayerIndex, 0, players.Count - 1)];
        }

        private void CheckMatchEnd()
        {
            PlayerState winner = players.FirstOrDefault(player => player.finished);
            if (winner != null)
            {
                EndMatch(winner);
                return;
            }

            List<PlayerState> active = players.Where(player => player.CanAct).ToList();
            if (active.Count <= 1)
            {
                EndMatch(active.FirstOrDefault());
            }
        }

        private void EndMatch(PlayerState winner)
        {
            matchEnded = true;
            awaitingLocalRoll = false;
            turnRunning = false;
            int winnerId = winner?.playerId ?? -1;
            serverAuthority.BroadcastMatchResult(winnerId);
            hud.ShowResult(winner == null ? "Collapse complete" : $"{winner.nickname} crossed before collapse", players);
            RefreshHud();
        }

        private void RefreshHud()
        {
            PlayerState current = GetCurrentPlayer();
            bool canRoll = awaitingLocalRoll && current != null && current.CanAct && !turnRunning;
            hud.Refresh(players, current, graph, canRoll);
        }

        private static string TileEventLabel(TileType tileType)
        {
            return tileType switch
            {
                TileType.Purple => "Purple",
                TileType.Yellow => "Shortcut Pass",
                TileType.Green => "Extra Turn",
                TileType.Red => "Trap",
                TileType.BlackRed => "Corrosion Alert",
                TileType.Safe => "Safe Zone reached",
                _ => "Move Complete"
            };
        }
    }
}
