using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class BattleLoadoutSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string[] ActiveTileIds = Array.Empty<string>();
        public int[] ActiveUpgradeLevels = Array.Empty<int>();
        public string TotemTileId = string.Empty;
        public int TotemUpgradeLevel;

        public bool IsCompleteForStore(BattleTileStore store)
        {
            return TryResolveActiveTiles(store, out _);
        }

        public bool TryResolveActiveTiles(BattleTileStore store, out List<BattleTileData> tiles)
        {
            tiles = new List<BattleTileData>(BattleTileInventoryService.RequiredActiveTiles);
            if (store == null || ActiveTileIds == null ||
                ActiveTileIds.Length != BattleTileInventoryService.RequiredActiveTiles ||
                ActiveUpgradeLevels == null || ActiveUpgradeLevels.Length != ActiveTileIds.Length ||
                string.IsNullOrWhiteSpace(TotemTileId))
                return false;

            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            int totemIndex = -1;
            string normalizedTotemId = TotemTileId.Trim();
            for (int i = 0; i < ActiveTileIds.Length; i++)
            {
                string id = string.IsNullOrWhiteSpace(ActiveTileIds[i]) ? string.Empty : ActiveTileIds[i].Trim();
                if (string.IsNullOrEmpty(id) || !used.Add(id) ||
                    ActiveUpgradeLevels[i] < 0 ||
                    !store.TryGetTileDataById(id, out BattleTileData data) || data?.Prefab == null)
                {
                    tiles.Clear();
                    return false;
                }

                tiles.Add(data);
                if (string.Equals(id, normalizedTotemId, StringComparison.Ordinal))
                    totemIndex = i;
            }

            if (totemIndex < 0 || TotemUpgradeLevel < 0 || TotemUpgradeLevel != ActiveUpgradeLevels[totemIndex])
            {
                tiles.Clear();
                return false;
            }

            return tiles.Count == BattleTileInventoryService.RequiredActiveTiles;
        }

        public int GetUpgradeLevel(string tileId)
        {
            if (string.IsNullOrWhiteSpace(tileId))
                return 0;

            string id = tileId.Trim();
            if (string.Equals(TotemTileId, id, StringComparison.Ordinal))
                return Mathf.Max(0, TotemUpgradeLevel);

            if (ActiveTileIds == null)
                return 0;

            for (int i = 0; i < ActiveTileIds.Length; i++)
            {
                if (!string.Equals(ActiveTileIds[i], id, StringComparison.Ordinal))
                    continue;

                return ActiveUpgradeLevels != null && i < ActiveUpgradeLevels.Length
                    ? Mathf.Max(0, ActiveUpgradeLevels[i])
                    : 0;
            }

            return 0;
        }

        public BattleLoadoutSnapshot Clone()
        {
            return new BattleLoadoutSnapshot
            {
                SchemaVersion = SchemaVersion,
                ActiveTileIds = ActiveTileIds != null ? (string[])ActiveTileIds.Clone() : Array.Empty<string>(),
                ActiveUpgradeLevels = ActiveUpgradeLevels != null ? (int[])ActiveUpgradeLevels.Clone() : Array.Empty<int>(),
                TotemTileId = TotemTileId ?? string.Empty,
                TotemUpgradeLevel = Mathf.Max(0, TotemUpgradeLevel)
            };
        }

        public static bool TryCreateFromProfile(PlayerProfile profile, BattleTileStore store, out BattleLoadoutSnapshot snapshot)
        {
            snapshot = null;
            if (profile == null || store == null)
                return false;

            IReadOnlyList<BattleTileData> active = BattleTileInventoryService.GetActiveTileData(profile, store);
            if (active == null || active.Count != BattleTileInventoryService.RequiredActiveTiles)
                return false;

            string[] ids = new string[active.Count];
            int[] levels = new int[active.Count];
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < active.Count; i++)
            {
                BattleTileData data = active[i];
                string id = data != null ? data.Id : string.Empty;
                if (data?.Prefab == null || string.IsNullOrWhiteSpace(id) || !used.Add(id))
                    return false;

                ids[i] = id;
                levels[i] = BattleTileInventoryService.GetUpgradeLevel(profile, id);
            }

            BattleTileData totem = BattleTileInventoryService.GetTotemTileData(profile, store);
            if (totem == null || !used.Contains(totem.Id))
                return false;

            snapshot = new BattleLoadoutSnapshot
            {
                ActiveTileIds = ids,
                ActiveUpgradeLevels = levels,
                TotemTileId = totem != null ? totem.Id : string.Empty,
                TotemUpgradeLevel = totem != null ? BattleTileInventoryService.GetUpgradeLevel(profile, totem.Id) : 0
            };
            return snapshot.IsCompleteForStore(store);
        }

        public static BattleLoadoutSnapshot CreateBot(BattleTileStore store, int seed, float difficultyFactor)
        {
            if (store == null || store.BattleTiles == null)
                return null;

            List<BattleTileData> pool = new List<BattleTileData>();
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < store.BattleTiles.Count; i++)
            {
                BattleTileData data = store.BattleTiles[i];
                if (data?.Prefab != null && !string.IsNullOrWhiteSpace(data.Id) && used.Add(data.Id))
                    pool.Add(data);
            }

            if (pool.Count < BattleTileInventoryService.RequiredActiveTiles)
                return null;

            System.Random random = new System.Random(seed == 0 ? 1 : seed);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }

            int count = BattleTileInventoryService.RequiredActiveTiles;
            string[] ids = new string[count];
            int[] levels = new int[count];
            int maxUpgrade = difficultyFactor >= 1.25f ? 2 : difficultyFactor >= 1.05f ? 1 : 0;
            for (int i = 0; i < count; i++)
            {
                ids[i] = pool[i].Id;
                levels[i] = maxUpgrade > 0 && random.NextDouble() < 0.28 ? random.Next(1, maxUpgrade + 1) : 0;
            }

            int totemIndex = random.Next(0, count);
            BattleTileData totem = pool[totemIndex];
            return new BattleLoadoutSnapshot
            {
                ActiveTileIds = ids,
                ActiveUpgradeLevels = levels,
                TotemTileId = totem.Id,
                TotemUpgradeLevel = levels[totemIndex]
            };
        }
    }
}
