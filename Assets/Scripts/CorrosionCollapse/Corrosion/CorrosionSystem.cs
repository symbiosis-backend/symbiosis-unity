using System;
using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Networking;
using Dynasty.Legacy.CorrosionCollapse.Players;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Corrosion
{
    public sealed class CorrosionSystem
    {
        private readonly BoardGraph graph;
        private readonly IServerAuthority serverAuthority;
        private readonly HashSet<BoardNode> infectedNodes = new HashSet<BoardNode>();

        public event Action<BoardNode> NodeStateChanged;

        public IReadOnlyCollection<BoardNode> InfectedNodes => infectedNodes;

        public CorrosionSystem(BoardGraph graph, IServerAuthority serverAuthority)
        {
            this.graph = graph;
            this.serverAuthority = serverAuthority;
        }

        public void StartFrom(BoardNode startNode)
        {
            if (startNode == null || !serverAuthority.IsServer)
            {
                return;
            }

            infectedNodes.Add(startNode);
        }

        public void Step(IEnumerable<PlayerState> players)
        {
            if (!serverAuthority.IsServer || graph == null)
            {
                return;
            }

            var newInfections = new List<BoardNode>();
            var snapshot = new List<BoardNode>(infectedNodes);

            foreach (BoardNode node in snapshot)
            {
                if (node.state == NodeState.Active)
                {
                    node.state = NodeState.Corrupted;
                    NodeStateChanged?.Invoke(node);
                    serverAuthority.BroadcastNodeState(node.id, node.state.ToString());
                    continue;
                }

                if (node.state == NodeState.Corrupted)
                {
                    node.state = NodeState.Destroyed;
                    NodeStateChanged?.Invoke(node);
                    serverAuthority.BroadcastNodeState(node.id, node.state.ToString());
                }

                if (node.state != NodeState.Destroyed)
                {
                    continue;
                }

                foreach (BoardNode next in node.nextNodes)
                {
                    if (next.state == NodeState.Active && !newInfections.Contains(next))
                    {
                        newInfections.Add(next);
                    }
                }
            }

            foreach (BoardNode node in newInfections)
            {
                node.state = NodeState.Corrupted;
                infectedNodes.Add(node);
                NodeStateChanged?.Invoke(node);
                serverAuthority.BroadcastNodeState(node.id, node.state.ToString());
            }

            EliminatePlayersOnDestroyedNodes(players);
        }

        public void EliminatePlayersOnDestroyedNodes(IEnumerable<PlayerState> players)
        {
            if (players == null)
            {
                return;
            }

            foreach (PlayerState player in players)
            {
                if (player == null || player.eliminated || player.finished || player.currentNode?.state != NodeState.Destroyed)
                {
                    continue;
                }

                player.eliminated = true;
                Debug.Log("[Game] Player eliminated");
            }
        }
    }
}
