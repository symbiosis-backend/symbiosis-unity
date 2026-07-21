using System.Collections;
using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public sealed class CorrosionWaveSystem
    {
        private readonly BoardGraph graph;
        private readonly IReadOnlyList<PlayerState> players;
        private readonly IReadOnlyDictionary<int, PlayerView> playerViews;
        private readonly BoardMover mover;

        public CorrosionWaveSystem(
            BoardGraph graph,
            IReadOnlyList<PlayerState> players,
            IReadOnlyDictionary<int, PlayerView> playerViews,
            BoardMover mover)
        {
            this.graph = graph;
            this.players = players;
            this.playerViews = playerViews;
            this.mover = mover;
        }

        public IEnumerator Trigger(BoardNode sourceNode)
        {
            if (sourceNode == null)
            {
                yield break;
            }

            int triggeredLineId = sourceNode.routeLineId;
            Debug.Log($"[Tile] BlackRed Corrosion Wave on line {triggeredLineId}");
            Debug.Log($"[Tile] BlackRed: Corrosion Wave triggered on line {triggeredLineId}.");

            foreach (PlayerState player in players)
            {
                if (player == null || player.finished || player.currentNode == null || player.currentNode.routeLineId != triggeredLineId)
                {
                    continue;
                }

                BoardNode safeZone = FindNearestSafeZoneBehind(player.currentNode) ?? graph.startNode;
                if (!playerViews.TryGetValue(player.playerId, out PlayerView view))
                {
                    player.currentNode = safeZone;
                    continue;
                }

                yield return mover.MoveToNodeByPreviousPath(player, view, safeZone);
                player.currentNode = safeZone;
                Debug.Log($"[Corrosion] {player.nickname} returned to Safe Zone {safeZone.id}");
                Debug.Log($"[Corrosion] Player returned to Safe Zone node {safeZone.id}.");
            }
        }

        private static BoardNode FindNearestSafeZoneBehind(BoardNode start)
        {
            var visited = new HashSet<BoardNode>();
            var queue = new Queue<BoardNode>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                BoardNode node = queue.Dequeue();
                if (node != start && (node.type == TileType.Safe || node.isSafeZone))
                {
                    return node;
                }

                foreach (BoardNode previous in node.previousNodes)
                {
                    if (previous != null && visited.Add(previous))
                    {
                        queue.Enqueue(previous);
                    }
                }
            }

            return null;
        }
    }
}
