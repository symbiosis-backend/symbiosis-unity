using System.Collections.Generic;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    [System.Serializable]
    public sealed class BoardNode
    {
        public int id;
        public Vector3 position;
        public TileType type;
        public NodeState state;
        public bool isShortcut;
        public int progressIndex;
        public int routeLineId;
        public bool isSafeZone;
        public readonly List<BoardNode> nextNodes = new List<BoardNode>();
        public readonly List<BoardNode> previousNodes = new List<BoardNode>();

        public BoardNode(int id, Vector3 position, TileType type)
        {
            this.id = id;
            this.position = position;
            this.type = type;
            state = NodeState.Active;
        }

        public bool IsTraversable => state != NodeState.Destroyed;
    }
}
