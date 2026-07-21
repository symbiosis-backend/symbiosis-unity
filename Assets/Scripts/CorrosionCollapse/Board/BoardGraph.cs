using System.Collections.Generic;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class BoardGraph
    {
        private readonly List<BoardNode> nodes = new List<BoardNode>();

        public IReadOnlyList<BoardNode> Nodes => nodes;
        public BoardNode startNode { get; private set; }
        public BoardNode finishNode { get; private set; }

        public BoardNode CreateNode(Vector3 position, TileType type)
        {
            var node = new BoardNode(nodes.Count, position, type);
            nodes.Add(node);
            startNode ??= node;
            finishNode = node;
            return node;
        }

        public void SetStart(BoardNode node)
        {
            startNode = node;
        }

        public void SetFinish(BoardNode node)
        {
            finishNode = node;
        }

        public void Connect(BoardNode from, BoardNode to)
        {
            if (from == null || to == null || from.nextNodes.Contains(to))
            {
                return;
            }

            from.nextNodes.Add(to);
            to.previousNodes.Add(from);
        }

        public int GetProgress(BoardNode node)
        {
            return node == null ? 0 : node.progressIndex;
        }
    }
}
