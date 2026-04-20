using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OkeyGame
{
    public sealed class OkeyPlayerSeat : MonoBehaviour
    {
        public enum SortMode
        {
            ColorThenNumber = 0,
            NumberThenColor = 1
        }

        [Header("Seat")]
        public int SeatIndex = 0;

        [Header("Rack")]
        public OkeyRackBoard RackBoard;

        [Header("Discard")]
        public OkeyDiscardPile DiscardPile;

        [Header("Sort")]
        public SortMode CurrentSortMode = SortMode.ColorThenNumber;
        public bool AutoSortOnAdd = false;

        [Header("Layout")]
        public bool PreserveManualLayout = true;

        [Header("Rules")]
        [Min(1)] public int MaxHandTiles = 15;

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<OkeyTileInstance> tiles = new List<OkeyTileInstance>();

        public IReadOnlyList<OkeyTileInstance> Tiles => tiles;
        public int HandCount => tiles.Count;

        public bool CanAcceptOneMoreTile()
        {
            return tiles.Count < MaxHandTiles;
        }

        public void ClearHand()
        {
            tiles.Clear();

            if (RackBoard != null)
                RackBoard.ClearBoard();

            if (DiscardPile != null)
                DiscardPile.ClearPile();
        }

        public void AddToHand(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (!RegisterTile(tile))
                return;

            ApplyTileSeatVisual(tile);

            if (AutoSortOnAdd)
            {
                SortHand();
                return;
            }

            if (!TryPlaceTileRobust(tile))
                Debug.LogWarning($"[OkeyPlayerSeat] P{SeatIndex} failed to place tile {tile.name} into rack.");

            SafeSyncTilesFromRackVisual();
        }

        public bool AddToHandAtSlot(OkeyTileInstance tile, Transform slotTransform)
        {
            if (tile == null || slotTransform == null || RackBoard == null)
                return false;

            if (!RegisterTile(tile))
                return false;

            ApplyTileSeatVisual(tile);

            if (AutoSortOnAdd)
            {
                SortHand();
                return true;
            }

            bool placed = TryPlaceTileIntoSlotRobust(tile, slotTransform, allowForeignTile: true);
            if (placed)
                SafeSyncTilesFromRackVisual();

            return placed;
        }

        public bool AcceptExternalTileToSlot(OkeyTileInstance tile, Transform slotTransform)
        {
            if (tile == null || slotTransform == null || RackBoard == null)
                return false;

            if (!RegisterTile(tile))
                return false;

            ApplyTileSeatVisual(tile);

            bool placed = TryPlaceTileIntoSlotRobust(tile, slotTransform, allowForeignTile: true);
            if (placed)
                SafeSyncTilesFromRackVisual();

            return placed;
        }

        public bool MoveTileToSlot(OkeyTileInstance tile, Transform slotTransform)
        {
            if (tile == null || slotTransform == null || RackBoard == null)
                return false;

            if (!tiles.Contains(tile))
            {
                Debug.LogWarning($"[OkeyPlayerSeat] P{SeatIndex} rejected MoveTileToSlot for foreign tile {tile.name}.");
                return false;
            }

            ApplyTileSeatVisual(tile);

            bool placed = TryPlaceTileIntoSlotRobust(tile, slotTransform, allowForeignTile: false);
            if (placed)
                SafeSyncTilesFromRackVisual();

            return placed;
        }

        public void RemoveFromHand(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            tiles.Remove(tile);

            if (RackBoard != null)
            {
                List<OkeyTileInstance> layout = RackBoard.GetSlotLayout();
                if (layout != null)
                {
                    bool changed = RemoveTileFromLayout(layout, tile);
                    if (changed)
                        RackBoard.RestoreSlotLayout(layout);
                }
            }

            if (ShouldUseManualLayout())
            {
                SafeSyncTilesFromRackVisual();
                return;
            }

            RebuildRackVisual();
        }

        public bool ContainsTile(OkeyTileInstance tile)
        {
            return tile != null && tiles.Contains(tile);
        }

        public bool Contains(OkeyTileInstance tile)
        {
            return ContainsTile(tile);
        }

        public void SortHand()
        {
            if (ShouldUseManualLayout())
                SafeSyncTilesFromRackVisual();

            if (tiles.Count <= 1)
            {
                RebuildRackVisual();
                return;
            }

            tiles.Sort(CompareTiles);
            RebuildRackVisual();
        }

        public void SortHandAsPairs(OkeyTableModule table)
        {
            if (table == null)
            {
                SortHand();
                return;
            }

            if (ShouldUseManualLayout())
                SafeSyncTilesFromRackVisual();

            if (tiles.Count <= 1)
            {
                RebuildRackVisual();
                return;
            }

            Dictionary<string, List<OkeyTileInstance>> grouped = new Dictionary<string, List<OkeyTileInstance>>();
            List<OkeyTileInstance> wilds = new List<OkeyTileInstance>();

            for (int i = 0; i < tiles.Count; i++)
            {
                OkeyTileInstance tile = tiles[i];
                if (tile == null)
                    continue;

                if (IsWildForPairs(tile, table))
                {
                    wilds.Add(tile);
                    continue;
                }

                string key = GetPairKey(tile);
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<OkeyTileInstance>();

                grouped[key].Add(tile);
            }

            List<List<OkeyTileInstance>> pairGroups = new List<List<OkeyTileInstance>>();
            List<List<OkeyTileInstance>> singleGroups = new List<List<OkeyTileInstance>>();

            foreach (var kv in grouped)
            {
                kv.Value.Sort((a, b) => a.RuntimeId.CompareTo(b.RuntimeId));

                if (kv.Value.Count >= 2)
                    pairGroups.Add(kv.Value);
                else
                    singleGroups.Add(kv.Value);
            }

            pairGroups = pairGroups
                .OrderBy(g => g[0].Number)
                .ThenBy(g => GetColorOrder(g[0].Color))
                .ThenBy(g => g[0].RuntimeId)
                .ToList();

            singleGroups = singleGroups
                .OrderBy(g => g[0].Number)
                .ThenBy(g => GetColorOrder(g[0].Color))
                .ThenBy(g => g[0].RuntimeId)
                .ToList();

            wilds = wilds
                .OrderBy(t => t.RuntimeId)
                .ToList();

            List<OkeyTileInstance> ordered = new List<OkeyTileInstance>(tiles.Count);

            for (int i = 0; i < pairGroups.Count; i++)
                ordered.AddRange(pairGroups[i]);

            for (int i = 0; i < singleGroups.Count; i++)
                ordered.AddRange(singleGroups[i]);

            ordered.AddRange(wilds);

            tiles.Clear();
            tiles.AddRange(ordered);

            RebuildRackVisual();
        }

        public void ToggleSortModeAndSort()
        {
            CurrentSortMode = CurrentSortMode == SortMode.ColorThenNumber
                ? SortMode.NumberThenColor
                : SortMode.ColorThenNumber;

            SortHand();
        }

        public void SetSortModeAndSort(SortMode mode)
        {
            CurrentSortMode = mode;
            SortHand();
        }

        public void SyncTilesFromRackVisual()
        {
            if (RackBoard == null)
                return;

            if (!RackBoard.gameObject.activeInHierarchy)
                return;

            List<OkeyTileInstance> layout = RackBoard.GetSlotLayout();
            if (layout == null)
                return;

            List<OkeyTileInstance> ordered = new List<OkeyTileInstance>(layout.Count);

            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i] != null)
                    ordered.Add(layout[i]);
            }

            if (ordered.Count == 0)
                return;

            tiles.Clear();
            tiles.AddRange(ordered);
        }

        private void SafeSyncTilesFromRackVisual()
        {
            if (RackBoard == null)
                return;

            if (!RackBoard.gameObject.activeInHierarchy)
                return;

            List<OkeyTileInstance> layout = RackBoard.GetSlotLayout();
            if (layout == null)
                return;

            List<OkeyTileInstance> ordered = new List<OkeyTileInstance>(layout.Count);

            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i] != null)
                    ordered.Add(layout[i]);
            }

            if (ordered.Count == 0)
                return;

            // КЛЮЧЕВОЙ ФИКС:
            // не даём временно неполному layout затирать реальную руку.
            if (tiles.Count > 0 && ordered.Count < tiles.Count)
                return;

            tiles.Clear();
            tiles.AddRange(ordered);
        }

        private bool RegisterTile(OkeyTileInstance tile)
        {
            if (tile == null)
                return false;

            if (tiles.Contains(tile))
            {
                tile.gameObject.SetActive(true);
                return true;
            }

            if (!CanRegisterTile(tile))
                return false;

            tiles.Add(tile);
            tile.gameObject.SetActive(true);
            return true;
        }

        private bool CanRegisterTile(OkeyTileInstance tile)
        {
            if (tile == null)
                return false;

            if (tiles.Contains(tile))
                return true;

            if (!CanAcceptOneMoreTile())
            {
                Debug.LogWarning($"[OkeyPlayerSeat] P{SeatIndex} cannot accept more than {MaxHandTiles} tiles.");
                return false;
            }

            return true;
        }

        private OkeyTableModule ResolveTable()
        {
            if (RackBoard != null && RackBoard.TurnManager != null && RackBoard.TurnManager.Table != null)
                return RackBoard.TurnManager.Table;

            OkeyTurnManager turnManager = FindAnyObjectByType<OkeyTurnManager>();
            if (turnManager != null && turnManager.Table != null)
                return turnManager.Table;

            return null;
        }

        private void ApplyTileSeatVisual(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            OkeyTableModule table = ResolveTable();
            if (table != null)
                table.ApplyHandOrBoardVisual(tile);
            else
            {
                tile.ShowFront();
                tile.RefreshVisuals();
            }
        }

        private bool TryPlaceTileRobust(OkeyTileInstance tile)
        {
            if (tile == null || RackBoard == null)
                return false;

            bool placed = RackBoard.PlaceTile(tile);
            if (placed)
            {
                ApplyTileSeatVisual(tile);
                return true;
            }

            RebuildRackVisual();
            return IsTileActuallyOnRack(tile);
        }

        private bool TryPlaceTileIntoSlotRobust(OkeyTileInstance tile, Transform slotTransform, bool allowForeignTile)
        {
            if (tile == null || slotTransform == null || RackBoard == null)
                return false;

            if (!allowForeignTile && !tiles.Contains(tile))
                return false;

            List<OkeyTileInstance> snapshot = null;
            bool useManual = ShouldUseManualLayout();

            if (useManual)
            {
                List<OkeyTileInstance> currentLayout = RackBoard.GetSlotLayout();
                if (currentLayout != null)
                {
                    snapshot = new List<OkeyTileInstance>(currentLayout);
                    bool changed = RemoveTileFromLayout(currentLayout, tile);
                    if (changed)
                        RackBoard.RestoreSlotLayout(currentLayout);
                }
            }

            bool placed = RackBoard.PlaceTileIntoSlot(tile, slotTransform);
            if (placed)
            {
                ApplyTileSeatVisual(tile);
                return true;
            }

            if (snapshot != null)
            {
                RackBoard.RestoreSlotLayout(snapshot);

                if (!tiles.Contains(tile))
                    tiles.Add(tile);
            }

            RebuildRackVisual();
            return IsTileActuallyOnRack(tile);
        }

        private bool RemoveTileFromLayout(List<OkeyTileInstance> layout, OkeyTileInstance tile)
        {
            if (layout == null || tile == null)
                return false;

            bool changed = false;

            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i] == tile)
                {
                    layout[i] = null;
                    changed = true;
                }
            }

            return changed;
        }

        private bool IsTileActuallyOnRack(OkeyTileInstance tile)
        {
            if (tile == null || RackBoard == null)
                return false;

            List<OkeyTileInstance> layout = RackBoard.GetSlotLayout();
            if (layout == null)
                return false;

            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i] == tile)
                    return true;
            }

            return false;
        }

        private bool ShouldUseManualLayout()
        {
            if (!PreserveManualLayout)
                return false;

            if (RackBoard == null)
                return false;

            return RackBoard.gameObject.activeInHierarchy;
        }

        private bool IsWildForPairs(OkeyTileInstance tile, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            return table.IsFakeOkey(tile) || table.IsCurrentRoundOkey(tile);
        }

        private string GetPairKey(OkeyTileInstance tile)
        {
            if (tile == null)
                return "NULL";

            return $"{tile.Color}_{tile.Number}";
        }

        private int CompareTiles(OkeyTileInstance a, OkeyTileInstance b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            bool aJoker = a.IsJoker || a.Color == TileColor.Joker;
            bool bJoker = b.IsJoker || b.Color == TileColor.Joker;

            if (aJoker && !bJoker) return 1;
            if (!aJoker && bJoker) return -1;

            if (CurrentSortMode == SortMode.ColorThenNumber)
            {
                int colorCompare = GetColorOrder(a.Color).CompareTo(GetColorOrder(b.Color));
                if (colorCompare != 0) return colorCompare;

                int numberCompare = a.Number.CompareTo(b.Number);
                if (numberCompare != 0) return numberCompare;
            }
            else
            {
                int numberCompare = a.Number.CompareTo(b.Number);
                if (numberCompare != 0) return numberCompare;

                int colorCompare = GetColorOrder(a.Color).CompareTo(GetColorOrder(b.Color));
                if (colorCompare != 0) return colorCompare;
            }

            return a.RuntimeId.CompareTo(b.RuntimeId);
        }

        private int GetColorOrder(TileColor color)
        {
            return color switch
            {
                TileColor.Black => 0,
                TileColor.Blue => 1,
                TileColor.Red => 2,
                TileColor.Yellow => 3,
                TileColor.Joker => 99,
                _ => 99
            };
        }

        private void RebuildRackVisual()
        {
            if (RackBoard == null)
                return;

            RackBoard.ClearBoard();

            for (int i = 0; i < tiles.Count; i++)
            {
                OkeyTileInstance tile = tiles[i];
                if (tile == null)
                    continue;

                ApplyTileSeatVisual(tile);

                bool placed = RackBoard.PlaceTile(tile);
                if (!placed)
                    Debug.LogWarning($"[OkeyPlayerSeat] P{SeatIndex} failed to place tile {tile.name} into rack.");
            }

            SafeSyncTilesFromRackVisual();
        }
    }
}