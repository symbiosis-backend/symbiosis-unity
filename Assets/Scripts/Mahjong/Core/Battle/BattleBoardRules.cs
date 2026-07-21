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
            return IsReady && IsUsable(tile);
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
