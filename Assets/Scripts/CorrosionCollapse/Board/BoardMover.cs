using System;
using System.Collections;
using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Networking;
using Dynasty.Legacy.CorrosionCollapse.Players;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class BoardMover
    {
        private readonly IServerAuthority serverAuthority;
        private readonly System.Random random = new System.Random();

        public BoardMover(IServerAuthority serverAuthority)
        {
            this.serverAuthority = serverAuthority;
        }

        public IEnumerator MoveBySteps(PlayerState player, PlayerView view, int steps, Func<PlayerState, BoardNode, BoardNode> branchSelector)
        {
            if (!serverAuthority.IsServer || player == null || view == null)
            {
                yield break;
            }

            for (int i = 0; i < steps; i++)
            {
                BoardNode next = SelectNext(player, player.currentNode, branchSelector);
                if (next == null || !next.IsTraversable)
                {
                    break;
                }

                player.currentNode = next;
                player.score = Mathf.Max(player.score, next.progressIndex);
                serverAuthority.BroadcastPlayerNode(player.playerId, next.id);
                yield return view.MoveToNode(next);

                if (next.nextNodes.Count == 0)
                {
                    break;
                }
            }
        }

        public IEnumerator MoveBackBySteps(PlayerState player, PlayerView view, int steps)
        {
            if (!serverAuthority.IsServer || player == null || view == null)
            {
                yield break;
            }

            for (int i = 0; i < steps; i++)
            {
                BoardNode previous = SelectPrevious(player.currentNode);
                if (previous == null || !previous.IsTraversable)
                {
                    break;
                }

                player.currentNode = previous;
                serverAuthority.BroadcastPlayerNode(player.playerId, previous.id);
                yield return view.MoveToNode(previous);
            }
        }

        public IEnumerator MoveToNodeByPreviousPath(PlayerState player, PlayerView view, BoardNode target)
        {
            if (!serverAuthority.IsServer || player == null || view == null || target == null)
            {
                yield break;
            }

            int guard = 0;
            while (player.currentNode != target && guard++ < 256)
            {
                BoardNode previous = SelectPrevious(player.currentNode);
                if (previous == null || !previous.IsTraversable)
                {
                    break;
                }

                player.currentNode = previous;
                serverAuthority.BroadcastPlayerNode(player.playerId, previous.id);
                yield return view.MoveToNode(previous);
            }
        }

        private BoardNode SelectNext(PlayerState player, BoardNode current, Func<PlayerState, BoardNode, BoardNode> branchSelector)
        {
            if (current == null || current.nextNodes.Count == 0)
            {
                return null;
            }

            var activeNext = new List<BoardNode>();
            foreach (BoardNode next in current.nextNodes)
            {
                if (next.IsTraversable)
                {
                    activeNext.Add(next);
                }
            }

            if (activeNext.Count == 0)
            {
                return null;
            }

            if (activeNext.Count == 1)
            {
                return activeNext[0];
            }

            BoardNode selected = branchSelector?.Invoke(player, current);
            if (selected != null && activeNext.Contains(selected))
            {
                return selected;
            }

            return activeNext[random.Next(activeNext.Count)];
        }

        private static BoardNode SelectPrevious(BoardNode current)
        {
            if (current == null || current.previousNodes.Count == 0)
            {
                return null;
            }

            BoardNode best = null;
            foreach (BoardNode previous in current.previousNodes)
            {
                if (!previous.IsTraversable)
                {
                    continue;
                }

                if (best == null || previous.progressIndex < best.progressIndex)
                {
                    best = previous;
                }
            }

            return best;
        }
    }
}
