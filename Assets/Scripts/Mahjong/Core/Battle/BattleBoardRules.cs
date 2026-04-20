using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleBoardRules : MonoBehaviour
    {
        public event Action RulesChanged;
        public event Action ViewRefreshed;

        private BattleBoard board;

        public bool IsReady => board != null;
        public BattleBoard Owner => board;

        private void Awake()
        {
            board = GetComponent<BattleBoard>();
        }

        public void Bind(BattleBoard target)
        {
            board = target;
            RulesChanged?.Invoke();
        }

        public void RefreshBlockedView()
        {
            if (!IsReady)
                return;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (tile == null || !tile.gameObject.activeSelf || tile.IsMatched)
                    continue;

                bool blocked = board.UseOpenRule && !IsTileFree(tile);
                tile.SetBlocked(blocked);
            }

            ViewRefreshed?.Invoke();
        }

        public bool IsTileFree(BattleTile tile)
        {
            if (!IsReady || tile == null)
                return false;

            BattleBoard.BattleTileNode node = GetNode(tile);
            if (node == null || node.Slot == null)
                return false;

            LayoutSlot slot = node.Slot;
            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile other = tiles[i];
                if (!IsValidBlocker(other, tile))
                    continue;

                BattleBoard.BattleTileNode n = GetNode(other);
                if (n == null || n.Slot == null)
                    continue;

                if (n.Slot.Z == slot.Z + 1)
                {
                    int dx = Mathf.Abs(n.Slot.X - slot.X);
                    int dy = Mathf.Abs(n.Slot.Y - slot.Y);

                    if (dx <= 1 && dy <= 1)
                        return false;
                }
            }

            bool left = false;
            bool right = false;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile other = tiles[i];
                if (!IsValidBlocker(other, tile))
                    continue;

                BattleBoard.BattleTileNode n = GetNode(other);
                if (n == null || n.Slot == null || n.Slot.Z != slot.Z)
                    continue;

                int dx = n.Slot.X - slot.X;
                int dy = Mathf.Abs(n.Slot.Y - slot.Y);

                if (dy == 0)
                {
                    if (dx < 0 && Mathf.Abs(dx) <= 1)
                        left = true;

                    if (dx > 0 && dx <= 1)
                        right = true;
                }

                if (left && right)
                    return false;
            }

            return true;
        }

        public List<BattleTile> GetFreeTiles()
        {
            List<BattleTile> result = new();
            if (!IsReady || board.IsFinished)
                return result;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (!IsUsable(tile))
                    continue;

                if (!board.UseOpenRule || IsTileFree(tile))
                    result.Add(tile);
            }

            return result;
        }

        public List<BattleTile> GetClickableClosedTiles()
        {
            List<BattleTile> result = new();
            if (!IsReady || board.IsFinished || board.IsResolvingPair)
                return result;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (!IsUsable(tile))
                    continue;

                if (tile.IsRevealed)
                    continue;

                if (!board.UseOpenRule || IsTileFree(tile))
                    result.Add(tile);
            }

            return result;
        }

        public List<BattleTile> GetActiveTiles()
        {
            List<BattleTile> result = new();
            if (!IsReady)
                return result;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (tile != null && tile.gameObject.activeSelf && !tile.IsMatched)
                    result.Add(tile);
            }

            return result;
        }

        public int CountActiveTiles()
        {
            int count = 0;
            if (!IsReady)
                return count;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (tile != null && tile.gameObject.activeSelf && !tile.IsMatched)
                    count++;
            }

            return count;
        }

        public bool HasAnyMove()
        {
            List<BattleTile> free = GetClickableClosedTiles();

            for (int i = 0; i < free.Count; i++)
            {
                for (int j = i + 1; j < free.Count; j++)
                {
                    if (free[i].Id == free[j].Id)
                        return true;
                }
            }

            return false;
        }

        private bool IsUsable(BattleTile tile)
        {
            return tile != null &&
                   tile.gameObject.activeSelf &&
                   !tile.IsMatched;
        }

        private bool IsValidBlocker(BattleTile tile, BattleTile self)
        {
            return tile != null &&
                   tile != self &&
                   tile.gameObject.activeSelf &&
                   !tile.IsMatched;
        }

        private BattleBoard.BattleTileNode GetNode(BattleTile tile)
        {
            return board != null ? board.GetNodePublic(tile) : null;
        }
    }
}