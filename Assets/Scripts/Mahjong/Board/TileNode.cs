using UnityEngine;

namespace MahjongGame
{
    [System.Serializable]
    public sealed class TileNode
    {
        public Tile Tile;
        public LayoutSlot Slot;

        public TileNode(Tile tile, LayoutSlot slot)
        {
            Tile = tile;
            Slot = slot;
        }
    }
}