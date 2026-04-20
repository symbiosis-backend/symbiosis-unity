using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class MahjongRules
    {
        public static bool IsTileFree(LayoutSlot slot, IReadOnlyList<TileNode> nodes, HashSet<Tile> lifted)
        {
            if (slot == null || nodes == null || nodes.Count == 0)
                return false;

            if (HasTopBlock(slot, nodes, lifted))
                return false;

            bool leftBlocked = HasLeftBlock(slot, nodes, lifted);
            bool rightBlocked = HasRightBlock(slot, nodes, lifted);

            return !leftBlocked || !rightBlocked;
        }

        private static bool HasTopBlock(LayoutSlot slot, IReadOnlyList<TileNode> nodes, HashSet<Tile> lifted)
        {
            int topZ = slot.Z + 1;

            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || n.Slot == null)
                    continue;

                if (!n.Tile.gameObject.activeSelf)
                    continue;

                if (lifted != null && lifted.Contains(n.Tile))
                    continue;

                if (n.Slot.Z != topZ)
                    continue;

                int dx = Mathf.Abs(n.Slot.X - slot.X);
                int dy = Mathf.Abs(n.Slot.Y - slot.Y);

                if (dx <= 1 && dy <= 1)
                    return true;
            }

            return false;
        }

        private static bool HasLeftBlock(LayoutSlot slot, IReadOnlyList<TileNode> nodes, HashSet<Tile> lifted)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || n.Slot == null)
                    continue;

                if (!n.Tile.gameObject.activeSelf)
                    continue;

                if (lifted != null && lifted.Contains(n.Tile))
                    continue;

                if (n.Slot.Z != slot.Z)
                    continue;

                int dx = n.Slot.X - slot.X;
                int dy = Mathf.Abs(n.Slot.Y - slot.Y);

                if (dx < 0 && Mathf.Abs(dx) <= 1 && dy == 0)
                    return true;
            }

            return false;
        }

        private static bool HasRightBlock(LayoutSlot slot, IReadOnlyList<TileNode> nodes, HashSet<Tile> lifted)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || n.Slot == null)
                    continue;

                if (!n.Tile.gameObject.activeSelf)
                    continue;

                if (lifted != null && lifted.Contains(n.Tile))
                    continue;

                if (n.Slot.Z != slot.Z)
                    continue;

                int dx = n.Slot.X - slot.X;
                int dy = Mathf.Abs(n.Slot.Y - slot.Y);

                if (dx > 0 && dx <= 1 && dy == 0)
                    return true;
            }

            return false;
        }
    }
}