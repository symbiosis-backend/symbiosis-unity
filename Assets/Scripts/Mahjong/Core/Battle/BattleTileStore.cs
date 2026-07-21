using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class BattleRoundConfig
    {
        [Min(1)] public int RoundIndex = 1;
        [Min(2)] public int TilesToUse = 16;
        [Min(1)] public int LayoutLevel = 1;
    }

    [DisallowMultipleComponent]
    public sealed class BattleTileStore : MonoBehaviour
    {
        public static BattleTileStore I { get; private set; }
        public const int AscendLegendaryEpicCopies = 5;
        public const int AscendLegendaryOzTileCost = 1200;
        public const float AscendLegendaryChance = 0.20f;
        public const float AscendChanceBonusPerUpgradeLevel = 0.02f;
        public const float AscendLegendaryFailureChanceBonus = 0.03f;
        public const float AscendMythicFailureChanceBonus = 0.02f;
        public const float AscendMaximumChance = 0.75f;
        public const int AscendLegendaryPityLimit = 4;
        public const int AscendMythicLegendaryCopies = 4;
        public const int AscendMythicOzTileCost = 3600;
        public const float AscendMythicChance = 0.10f;
        public const int AscendMythicPityLimit = 6;

        [Header("Battle Tile Pool")]
        [SerializeField] private List<BattleTileData> battleTiles = new();

        [Header("Battle Match Config")]
        [SerializeField, Min(1)] private int totalRounds = 1;
        [SerializeField] private List<BattleRoundConfig> roundConfigs = new()
        {
            new BattleRoundConfig { RoundIndex = 1, TilesToUse = 56, LayoutLevel = 3 }
        };

        public IReadOnlyList<BattleTileData> BattleTiles => battleTiles;
        public int TotalRounds => Mathf.Max(1, totalRounds);
        public IReadOnlyList<BattleRoundConfig> RoundConfigs => roundConfigs;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            MailboxService.ApplyPendingBattleTileGrants(this);
        }

        public BattleRoundConfig GetRoundConfig(int roundIndex)
        {
            if (BattleLoreTutorialSession.IsActive)
                return BattleLoreTutorialSession.GetBattleRoundConfigForActiveStage(roundIndex);

            if (roundConfigs != null)
            {
                for (int i = 0; i < roundConfigs.Count; i++)
                {
                    BattleRoundConfig cfg = roundConfigs[i];
                    if (cfg != null && cfg.RoundIndex == roundIndex)
                        return cfg;
                }
            }

            int fallbackLayoutLevel = Mathf.Max(1, roundIndex);
            return new BattleRoundConfig
            {
                RoundIndex = Mathf.Max(1, roundIndex),
                TilesToUse = GetRecommendedTilesToUseForLayout(fallbackLayoutLevel),
                LayoutLevel = fallbackLayoutLevel
            };
        }

        public IReadOnlyList<BattleTileData> GetTilesForRound(int roundIndex)
        {
            return GetTilesForRound(roundIndex, ProfileService.I != null ? ProfileService.I.Current : null, useProfileActiveDeck: true);
        }

        public IReadOnlyList<BattleTileData> GetDefaultTilesForRound(int roundIndex)
        {
            return GetTilesForRound(roundIndex, null, useProfileActiveDeck: false);
        }

        public IReadOnlyList<BattleTileData> GetProfileTilesForRound(int roundIndex, PlayerProfile profile)
        {
            return GetTilesForRound(roundIndex, profile, useProfileActiveDeck: true);
        }

        private IReadOnlyList<BattleTileData> GetTilesForRound(int roundIndex, PlayerProfile profile, bool useProfileActiveDeck)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            int recommendedCount = GetRecommendedTilesToUseForLayout(cfg.LayoutLevel);
            int targetCount = Mathf.Max(2, cfg.TilesToUse);
            if (recommendedCount > 0)
                targetCount = Mathf.Min(targetCount, recommendedCount);

            List<BattleTileData> result = new();
            if (battleTiles == null || battleTiles.Count == 0)
                return result;

            List<BattleTileData> validPool = new();
            for (int i = 0; i < battleTiles.Count; i++)
            {
                BattleTileData t = battleTiles[i];
                if (t != null && t.Prefab != null && !string.IsNullOrWhiteSpace(t.Id))
                    validPool.Add(t);
            }

            if (validPool.Count == 0)
                return result;

            if (BattleLoreTutorialSession.IsActive)
                return BattleLoreTutorialSession.GetTrialDeckTiles(this, targetCount);

            IReadOnlyList<BattleTileData> activeDeck = useProfileActiveDeck && profile != null
                ? BattleTileInventoryService.GetActiveTileData(profile, this)
                : null;

            if (activeDeck != null && activeDeck.Count >= BattleTileInventoryService.MinActiveTiles)
                return new List<BattleTileData>(activeDeck);

            while (result.Count < targetCount)
            {
                for (int i = 0; i < validPool.Count && result.Count < targetCount; i++)
                    result.Add(validPool[i]);
            }

            if ((result.Count & 1) != 0)
                result.RemoveAt(result.Count - 1);

            return result;
        }

        public int GetLayoutLevelForRound(int roundIndex)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            return Mathf.Max(1, cfg.LayoutLevel);
        }

        public int GetTilesToUseForRound(int roundIndex)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            int recommendedCount = GetRecommendedTilesToUseForLayout(cfg.LayoutLevel);
            int targetCount = Mathf.Max(2, cfg.TilesToUse);
            return recommendedCount > 0 ? Mathf.Min(targetCount, recommendedCount) : targetCount;
        }

        public bool TryGetTileDataById(string id, out BattleTileData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(id) || battleTiles == null)
                return false;

            for (int i = 0; i < battleTiles.Count; i++)
            {
                BattleTileData item = battleTiles[i];
                if (item == null)
                    continue;

                if (string.Equals(item.Id, id, StringComparison.Ordinal))
                {
                    data = item;
                    return true;
                }
            }

            return false;
        }

        private static int GetRecommendedTilesToUseForLayout(int layoutLevel)
        {
            int slotCount = BattleLayoutPresets.GetSlotCount(layoutLevel);
            if (slotCount <= 0)
                return 0;

            if ((slotCount & 1) != 0)
                slotCount -= 1;

            return Mathf.Max(2, slotCount);
        }
    }

    public static class BattleTileInventoryService
    {
        private const string AscendLegendaryPityKey = "MahjongGame.Battle.ForgeAscend.LegendaryPity.";
        private const string AscendMythicPityKey = "MahjongGame.Battle.ForgeAscend.MythicPity.";
        public const int MinActiveTiles = 2;
        public const int MaxActiveTiles = 18;
        public const int RequiredActiveTiles = MaxActiveTiles;
        public const int ForgeRequiredCopies = 3;
        public const float ForgeBonusGrowthPerLevel = 0.10f;
        public const int ForgeRareOzTileCost = 30;
        public const int ForgeEpicOzTileCost = 120;
        public const int ForgeLegendaryOzTileCost = 500;
        public const int ForgeMythicOzTileCost = 1600;
        private const float RankedMaxHpBonusMultiplier = 1.25f;
        private const float RankedAttackBonusMultiplier = 1.25f;
        private const float RankedMaxArmorBonus = 0.10f;
        private const float RankedMaxParryBonus = 0.08f;
        private const float RankedMaxCritChanceBonus = 0.08f;
        private const float RankedMaxCritDamageBonus = 0.35f;
        private const float RankedMaxSelfHealMultiplier = 0.15f;
        private const int CurrentInventorySchemaVersion = 7;
        private static readonly string[] StandardActiveTileIds =
        {
            "battle_tile_01",
            "battle_tile_02",
            "battle_tile_03",
            "battle_tile_04",
            "battle_tile_09",
            "battle_tile_11",
            "battle_tile_12",
            "battle_tile_14",
            "battle_tile_15",
            "battle_tile_16",
            "battle_tile_19",
            "battle_tile_20",
            "battle_tile_21",
            "battle_tile_22",
            "battle_tile_24",
            "battle_tile_25",
            "battle_tile_27",
            "battle_tile_28"
        };

        public static MahjongBattleTileInventoryData GetOrCreateInventory(PlayerProfile profile)
        {
            if (profile == null)
                return null;

            profile.EnsureData();
            if (profile.Mahjong.Battle.TileInventory == null)
                profile.Mahjong.Battle.TileInventory = new MahjongBattleTileInventoryData();

            profile.Mahjong.Battle.TileInventory.EnsureValid();
            return profile.Mahjong.Battle.TileInventory;
        }

        public static bool EnsureDefaultInventory(PlayerProfile profile)
        {
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null)
                return false;

            bool changed = false;
            bool emptyInventory = inventory.ActiveTileIds.Count == 0 && inventory.ReserveTileIds.Count == 0;
			if (inventory.SchemaVersion < 6)
			{
				MigrateLegacyAscendPity(profile, inventory);
				changed = true;
			}
            if (emptyInventory || inventory.SchemaVersion < 1)
            {
                RemoveStandardTileIds(inventory.ActiveTileIds);
                RemoveStandardTileIds(inventory.ReserveTileIds);

                for (int i = 0; i < StandardActiveTileIds.Length && inventory.ActiveTileIds.Count < MaxActiveTiles; i++)
                {
                    inventory.ActiveTileIds.Add(StandardActiveTileIds[i]);
                    EnsureTileStack(inventory, StandardActiveTileIds[i], 1);
                }

                inventory.SchemaVersion = CurrentInventorySchemaVersion;
                changed = true;
            }
            else if (inventory.SchemaVersion < CurrentInventorySchemaVersion)
            {
                inventory.SchemaVersion = CurrentInventorySchemaVersion;
                changed = true;
            }

            changed |= EnsureStacksForOwnedTiles(inventory);

            while (inventory.ActiveTileIds.Count < MinActiveTiles && inventory.ReserveTileIds.Count > 0)
            {
                string id = inventory.ReserveTileIds[0];
                inventory.ReserveTileIds.RemoveAt(0);
                if (!inventory.ActiveTileIds.Contains(id))
                {
                    inventory.ActiveTileIds.Add(id);
                    changed = true;
                }
            }

            while (inventory.ActiveTileIds.Count > MaxActiveTiles)
            {
                int index = inventory.ActiveTileIds.Count - 1;
                string id = inventory.ActiveTileIds[index];
                inventory.ActiveTileIds.RemoveAt(index);
                if (!inventory.ReserveTileIds.Contains(id))
                    inventory.ReserveTileIds.Insert(0, id);
                changed = true;
            }

            inventory.EnsureValid();
            return changed;
        }

        public static bool IsBaseBattleTile(string tileId)
        {
            if (string.IsNullOrWhiteSpace(tileId))
                return false;

            string id = tileId.Trim();
            for (int i = 0; i < StandardActiveTileIds.Length; i++)
            {
                if (string.Equals(StandardActiveTileIds[i], id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static void EnsureInventoryForStore(PlayerProfile profile, BattleTileStore store)
        {
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null || store == null)
                return;

            EnsureDefaultInventory(profile);

            List<string> allIds = GetValidStoreIds(store);
            if (allIds.Count == 0)
                return;

            RemoveMissingIds(inventory.ActiveTileIds, allIds);
            RemoveMissingIds(inventory.ReserveTileIds, allIds);
            if (!string.IsNullOrWhiteSpace(inventory.TotemTileId) && !allIds.Contains(inventory.TotemTileId))
                inventory.TotemTileId = string.Empty;

            for (int i = 0; i < allIds.Count; i++)
            {
                string id = allIds[i];
                if (inventory.ActiveTileIds.Contains(id) || inventory.ReserveTileIds.Contains(id))
                {
                    EnsureTileStackChanged(inventory, id, 1);
                    continue;
                }

                if (!IsBaseBattleTile(id))
                    continue;

                if (inventory.ActiveTileIds.Count < MaxActiveTiles)
                    inventory.ActiveTileIds.Add(id);
                else if (!inventory.ReserveTileIds.Contains(id))
                    inventory.ReserveTileIds.Add(id);

                EnsureTileStack(inventory, id, 1);
            }

            // Migration from the former separate totem slot: the selected totem
            // now occupies one of the 18 active loadout positions.
            if (!string.IsNullOrWhiteSpace(inventory.TotemTileId) &&
                allIds.Contains(inventory.TotemTileId) &&
                !inventory.ActiveTileIds.Contains(inventory.TotemTileId))
            {
                inventory.ReserveTileIds.Remove(inventory.TotemTileId);
                if (inventory.ActiveTileIds.Count >= MaxActiveTiles)
                {
                    int replacementIndex = FindBaseTileIndex(inventory.ActiveTileIds, inventory.TotemTileId);
                    if (replacementIndex < 0)
                        replacementIndex = inventory.ActiveTileIds.Count - 1;

                    if (replacementIndex >= 0)
                    {
                        string replacementId = inventory.ActiveTileIds[replacementIndex];
                        inventory.ActiveTileIds.RemoveAt(replacementIndex);
                        if (!inventory.ReserveTileIds.Contains(replacementId))
                            inventory.ReserveTileIds.Insert(0, replacementId);
                    }
                }

                inventory.ActiveTileIds.Add(inventory.TotemTileId);
            }

            while (inventory.ActiveTileIds.Count < MinActiveTiles && inventory.ReserveTileIds.Count > 0)
            {
                string id = inventory.ReserveTileIds[0];
                inventory.ReserveTileIds.RemoveAt(0);
                if (!inventory.ActiveTileIds.Contains(id))
                    inventory.ActiveTileIds.Add(id);
            }

            while (inventory.ActiveTileIds.Count > MaxActiveTiles)
            {
                int index = inventory.ActiveTileIds.Count - 1;
                string id = inventory.ActiveTileIds[index];
                inventory.ActiveTileIds.RemoveAt(index);
                if (!inventory.ReserveTileIds.Contains(id))
                    inventory.ReserveTileIds.Insert(0, id);
            }

            inventory.EnsureValid();
            EnsureStacksForOwnedTiles(inventory);
        }

        public static BattleTileData GetTotemTileData(PlayerProfile profile, BattleTileStore store)
        {
            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null || store == null || string.IsNullOrWhiteSpace(inventory.TotemTileId) ||
                !inventory.ActiveTileIds.Contains(inventory.TotemTileId))
                return null;

            return store.TryGetTileDataById(inventory.TotemTileId, out BattleTileData data) ? data : null;
        }

        public static IReadOnlyList<BattleTileData> GetActiveTileData(PlayerProfile profile, BattleTileStore store)
        {
            EnsureInventoryForStore(profile, store);

            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null || store == null)
                return Array.Empty<BattleTileData>();

            List<BattleTileData> result = new();
            for (int i = 0; i < inventory.ActiveTileIds.Count; i++)
            {
                if (store.TryGetTileDataById(inventory.ActiveTileIds[i], out BattleTileData data) && data?.Prefab != null)
                    result.Add(data);
            }

            return result;
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyActiveTileBonuses(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            PlayerProfile profile,
            BattleTileStore store,
            BattleCharacterDatabase.BattleCharacterData selectedCharacter)
        {
            bool useTrialDeck = BattleLoreTutorialSession.IsActive && store != null;
            BattleLoadoutSnapshot battleLoadout = !useTrialDeck && MahjongSession.LaunchMode == MahjongLaunchMode.Battle
                ? MahjongSession.LocalBattleLoadout
                : null;
            IReadOnlyList<BattleTileData> activeTiles;
            if (battleLoadout != null && battleLoadout.TryResolveActiveTiles(store, out List<BattleTileData> snapshotTiles))
                activeTiles = snapshotTiles;
            else
                activeTiles = useTrialDeck ? BattleLoreTutorialSession.GetTrialDeckTiles(store, MaxActiveTiles) : GetActiveTileData(profile, store);
            if (activeTiles == null)
                return baseStats;

            int maxHp = baseStats.MaxHp;
            int attack = baseStats.Attack;
            float armor = baseStats.Armor;
            float parryChance = baseStats.ParryChance;
            float critChance = baseStats.CritChance;
            float critDamageMultiplier = baseStats.CritDamageMultiplier;
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);

            for (int i = 0; i < activeTiles.Count; i++)
            {
                BattleTileData data = activeTiles[i];
                int upgradeLevel = useTrialDeck
                    ? ResolveTrialForgeLevel(data)
                    : battleLoadout != null ? battleLoadout.GetUpgradeLevel(data?.Id) : GetUpgradeLevel(inventory, data?.Id);
                ApplyPassiveBonus(data?.PassiveBonus, data?.Rarity ?? BattleTileRarity.Standard, upgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);

                if (IsSymbiosisActive(data, selectedCharacter))
                    ApplyPassiveBonus(data.SymbiosisBonus, data.Rarity, upgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
            }

            BattleTileData totemTile = useTrialDeck && activeTiles.Count > 0
                ? activeTiles[0]
                : ResolveSnapshotTotem(battleLoadout, store) ?? GetTotemTileData(profile, store);
            if (!ContainsTileId(activeTiles, totemTile?.Id))
            {
                int totemUpgradeLevel = useTrialDeck
                    ? ResolveTrialForgeLevel(totemTile)
                    : battleLoadout != null ? battleLoadout.GetUpgradeLevel(totemTile?.Id) : GetUpgradeLevel(inventory, totemTile?.Id);
                ApplyPassiveBonus(totemTile?.PassiveBonus, totemTile?.Rarity ?? BattleTileRarity.Standard, totemUpgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
                if (IsSymbiosisActive(totemTile, selectedCharacter))
                    ApplyPassiveBonus(totemTile.SymbiosisBonus, totemTile.Rarity, totemUpgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
            }

            return ClampRankedTileBonuses(baseStats, new BattleStatsHub.BattleStatsSnapshot(
                maxHp,
                attack,
                armor,
                parryChance,
                critChance,
                critDamageMultiplier));
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyTileDataBonuses(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            IReadOnlyList<BattleTileData> activeTiles,
            BattleTileData totemTile,
            BattleCharacterDatabase.BattleCharacterData selectedCharacter,
            BattleLoadoutSnapshot battleLoadout = null)
        {
            int maxHp = baseStats.MaxHp;
            int attack = baseStats.Attack;
            float armor = baseStats.Armor;
            float parryChance = baseStats.ParryChance;
            float critChance = baseStats.CritChance;
            float critDamageMultiplier = baseStats.CritDamageMultiplier;

            if (activeTiles != null)
            {
                for (int i = 0; i < activeTiles.Count; i++)
                {
                    BattleTileData data = activeTiles[i];
                    int upgradeLevel = battleLoadout != null
                        ? battleLoadout.GetUpgradeLevel(data?.Id)
                        : GetUpgradeLevel(ProfileService.I?.Current, data?.Id);
                    ApplyPassiveBonus(data?.PassiveBonus, data?.Rarity ?? BattleTileRarity.Standard, upgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);

                    if (IsSymbiosisActive(data, selectedCharacter))
                        ApplyPassiveBonus(data.SymbiosisBonus, data.Rarity, upgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
                }
            }

            if (!ContainsTileId(activeTiles, totemTile?.Id))
            {
                int totemUpgradeLevel = battleLoadout != null
                    ? battleLoadout.GetUpgradeLevel(totemTile?.Id)
                    : GetUpgradeLevel(ProfileService.I?.Current, totemTile?.Id);
                ApplyPassiveBonus(totemTile?.PassiveBonus, totemTile?.Rarity ?? BattleTileRarity.Standard, totemUpgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
                if (IsSymbiosisActive(totemTile, selectedCharacter))
                    ApplyPassiveBonus(totemTile.SymbiosisBonus, totemTile.Rarity, totemUpgradeLevel, ref maxHp, ref attack, ref armor, ref parryChance, ref critChance, ref critDamageMultiplier);
            }

            return ClampRankedTileBonuses(baseStats, new BattleStatsHub.BattleStatsSnapshot(
                maxHp,
                attack,
                armor,
                parryChance,
                critChance,
                critDamageMultiplier));
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyMatchedTileActiveBonuses(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            BattleTileStore store,
            BattleTile firstTile,
            BattleTile secondTile,
            out int selfHeal,
            BattleLoadoutSnapshot battleLoadout = null)
        {
            selfHeal = 0;
            if (store == null)
                return baseStats;

            int attack = baseStats.Attack;
            float critChance = baseStats.CritChance;
            float critDamageMultiplier = baseStats.CritDamageMultiplier;

            ApplyMatchedTileActiveBonus(store, firstTile, battleLoadout, ref attack, ref critChance, ref critDamageMultiplier, ref selfHeal);

            if (secondTile != null && firstTile != null && string.Equals(secondTile.Id, firstTile.Id, StringComparison.Ordinal))
            {
                ClampRankedMatchedTileBonuses(baseStats, ref attack, ref critChance, ref critDamageMultiplier, ref selfHeal);
                return new BattleStatsHub.BattleStatsSnapshot(
                    baseStats.MaxHp,
                    attack,
                    baseStats.Armor,
                    baseStats.ParryChance,
                    critChance,
                    critDamageMultiplier);
            }

            ApplyMatchedTileActiveBonus(store, secondTile, battleLoadout, ref attack, ref critChance, ref critDamageMultiplier, ref selfHeal);
            ClampRankedMatchedTileBonuses(baseStats, ref attack, ref critChance, ref critDamageMultiplier, ref selfHeal);

            return new BattleStatsHub.BattleStatsSnapshot(
                baseStats.MaxHp,
                attack,
                baseStats.Armor,
                baseStats.ParryChance,
                critChance,
                critDamageMultiplier);
        }

        public static bool TryActivateTile(PlayerProfile profile, BattleTileStore store, string tileId, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(tileId))
                return false;

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null)
                return false;

            string id = tileId.Trim();
            if (inventory.ActiveTileIds.Contains(id))
            {
                reason = "Tile type is already active";
                return false;
            }

            if (string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal))
            {
                reason = "Tile type is already assigned to the totem";
                return false;
            }

            if (GetReserveCopyCount(inventory, id) <= 0)
            {
                reason = "No reserve copies available";
                return false;
            }

            if (inventory.ActiveTileIds.Count >= MaxActiveTiles)
            {
                int replacementIndex = FindBaseTileIndex(inventory.ActiveTileIds, id, inventory.TotemTileId);
                if (replacementIndex < 0)
                {
                    reason = $"Active deck is full: {MaxActiveTiles}";
                    return false;
                }

                string replacementId = inventory.ActiveTileIds[replacementIndex];
                inventory.ActiveTileIds.RemoveAt(replacementIndex);
                if (!inventory.ReserveTileIds.Contains(replacementId))
                    inventory.ReserveTileIds.Add(replacementId);
            }

            if (!inventory.ReserveTileIds.Remove(id))
            {
                reason = "Tile is not in reserve";
                return false;
            }

            inventory.ActiveTileIds.Add(id);
            inventory.EnsureValid();
            return true;
        }

        public static bool TrySetTotemTile(PlayerProfile profile, BattleTileStore store, string tileId, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(tileId))
                return false;

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null || store == null)
                return false;

            string id = tileId.Trim();
            if (!store.TryGetTileDataById(id, out BattleTileData data) || data?.Prefab == null)
            {
                reason = "Tile is missing";
                return false;
            }

            if (string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal))
                return true;

            if (!inventory.ActiveTileIds.Contains(id))
            {
                reason = "Totem must be selected from the active deck";
                return false;
            }

            inventory.TotemTileId = id;
            inventory.EnsureValid();
            return true;
        }

        public static bool TryClearTotemTile(PlayerProfile profile, BattleTileStore store, out string reason)
        {
            reason = string.Empty;
            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null || string.IsNullOrWhiteSpace(inventory.TotemTileId))
                return false;

            inventory.TotemTileId = string.Empty;
            inventory.EnsureValid();
            return true;
        }

        public static bool TryReserveTile(PlayerProfile profile, BattleTileStore store, string tileId, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(tileId))
                return false;

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null)
                return false;

            string id = tileId.Trim();
            if (inventory.ReserveTileIds.Contains(id))
                return true;

            if (string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal))
            {
                reason = "Select another active totem before moving this tile to reserve";
                return false;
            }

            if (inventory.ActiveTileIds.Count <= MinActiveTiles)
            {
                reason = $"Keep at least {MinActiveTiles} active tiles";
                return false;
            }

            if (!inventory.ActiveTileIds.Remove(id))
            {
                reason = "Tile is not active";
                return false;
            }

            inventory.ReserveTileIds.Add(id);
            inventory.EnsureValid();
            EnsureTileStackChanged(inventory, id, 1);
            return true;
        }

        public static int GetOwnedCount(PlayerProfile profile, string tileId)
        {
            return GetOwnedCount(GetOrCreateInventory(profile), tileId);
        }

        public static int GetOwnedCount(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return 0;

            inventory.EnsureValid();
            string id = tileId.Trim();
            long total = 0L;
            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                MahjongBattleTileStackData stack = inventory.TileStacks[i];
                if (stack != null && string.Equals(stack.TileId, id, StringComparison.Ordinal))
                    total += Mathf.Max(0, stack.Count);
            }

            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        public static int GetOwnedCount(PlayerProfile profile, string tileId, int upgradeLevel)
        {
            return GetOwnedCount(GetOrCreateInventory(profile), tileId, upgradeLevel);
        }

        public static int GetOwnedCount(MahjongBattleTileInventoryData inventory, string tileId, int upgradeLevel)
        {
            MahjongBattleTileStackData stack = FindTileStack(inventory, tileId, upgradeLevel);
            return stack != null ? Mathf.Max(0, stack.Count) : 0;
        }

        public static int GetReserveCopyCount(PlayerProfile profile, string tileId)
        {
            return GetReserveCopyCount(GetOrCreateInventory(profile), tileId);
        }

        public static int GetReserveCopyCount(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return 0;

            inventory.EnsureValid();
            string id = tileId.Trim();
            bool assignedToBattle = inventory.ActiveTileIds.Contains(id)
                                    || string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal);
            return Mathf.Max(0, GetOwnedCount(inventory, id) - (assignedToBattle ? 1 : 0));
        }

        public static int GetReserveCopyCount(MahjongBattleTileInventoryData inventory, string tileId, int upgradeLevel)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return 0;

            inventory.EnsureValid();
            string id = tileId.Trim();
            int count = GetOwnedCount(inventory, id, upgradeLevel);
            bool assignedToBattle = inventory.ActiveTileIds.Contains(id)
                                    || string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal);
            if (assignedToBattle && GetUpgradeLevel(inventory, id) == Mathf.Max(0, upgradeLevel))
                count--;
            return Mathf.Max(0, count);
        }

        public static List<MahjongBattleTileStackData> GetReserveTileStacks(MahjongBattleTileInventoryData inventory)
        {
            List<MahjongBattleTileStackData> result = new();
            if (inventory == null)
                return result;

            inventory.EnsureValid();
            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                MahjongBattleTileStackData stack = inventory.TileStacks[i];
                if (stack == null || string.IsNullOrWhiteSpace(stack.TileId))
                    continue;

                int reserveCount = GetReserveCopyCount(inventory, stack.TileId, stack.UpgradeLevel);
                if (reserveCount > 0)
                    result.Add(new MahjongBattleTileStackData(stack.TileId, reserveCount, stack.UpgradeLevel));
            }

            result.Sort((left, right) =>
            {
                int idCompare = string.Compare(left?.TileId, right?.TileId, StringComparison.Ordinal);
                return idCompare != 0 ? idCompare : (left?.UpgradeLevel ?? 0).CompareTo(right?.UpgradeLevel ?? 0);
            });
            return result;
        }

        public static List<string> GetReserveTileIds(MahjongBattleTileInventoryData inventory)
        {
            List<string> result = new();
            if (inventory == null)
                return result;

            inventory.EnsureValid();
            HashSet<string> added = new(StringComparer.Ordinal);
            for (int i = 0; i < inventory.ReserveTileIds.Count; i++)
            {
                string id = inventory.ReserveTileIds[i];
                if (GetReserveCopyCount(inventory, id) > 0 && added.Add(id))
                    result.Add(id);
            }

            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                string id = inventory.TileStacks[i]?.TileId;
                if (GetReserveCopyCount(inventory, id) > 0 && added.Add(id))
                    result.Add(id);
            }

            return result;
        }

        public static int GetUpgradeLevel(PlayerProfile profile, string tileId)
        {
            return GetUpgradeLevel(GetOrCreateInventory(profile), tileId);
        }

        public static int GetUpgradeLevel(MahjongBattleTileInventoryData inventory, string tileId)
        {
            MahjongBattleTileStackData stack = FindHighestTileStack(inventory, tileId);
            return stack != null ? Mathf.Max(0, stack.UpgradeLevel) : 0;
        }

        public static bool GrantTileCopy(PlayerProfile profile, BattleTileStore store, string tileId, out bool isNew)
        {
            isNew = false;
            if (profile == null || store == null || string.IsNullOrWhiteSpace(tileId))
                return false;

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (inventory == null)
                return false;

            string id = tileId.Trim();
            bool owned = OwnsTile(inventory, id);
            MahjongBattleTileStackData baseStack = EnsureTileStack(inventory, id, 0, 0);
            if (baseStack.Count < int.MaxValue)
                baseStack.Count++;
            if (!owned)
            {
                inventory.ReserveTileIds.Insert(0, id);
                isNew = true;
            }

            inventory.EnsureValid();
            return true;
        }

        public static bool TryForgeTile(PlayerProfile profile, BattleTileStore store, string tileId, out int newUpgradeLevel, out int remainingCopies, out string reason, bool waiveOzTileCost = false)
        {
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            int sourceLevel = FindHighestForgeableLevel(inventory, tileId);
            return TryForgeTile(profile, store, tileId, sourceLevel, out newUpgradeLevel, out remainingCopies, out reason, waiveOzTileCost);
        }

        public static bool TryForgeTile(PlayerProfile profile, BattleTileStore store, string tileId, int sourceUpgradeLevel, out int newUpgradeLevel, out int remainingCopies, out string reason, bool waiveOzTileCost = false)
        {
            newUpgradeLevel = 0;
            remainingCopies = 0;
            reason = string.Empty;

            if (profile == null || store == null || string.IsNullOrWhiteSpace(tileId))
            {
                reason = "Forge is not ready";
                return false;
            }

            string id = tileId.Trim();
            if (!store.TryGetTileDataById(id, out BattleTileData data) || data?.Prefab == null)
            {
                reason = "Tile is missing";
                return false;
            }

            if (data.Rarity < BattleTileRarity.Rare)
            {
                reason = "Forge requires Rare or higher tiles";
                return false;
            }

			EnsureInventoryForStore(profile, store);
			MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            int normalizedSourceLevel = Mathf.Max(0, sourceUpgradeLevel);
            MahjongBattleTileStackData stack = FindTileStack(inventory, id, normalizedSourceLevel);
            if (stack == null)
            {
                reason = "Selected Forge stack is missing";
                return false;
            }
			if (stack.UpgradeLevel == int.MaxValue)
			{
				reason = "Forge level storage limit reached";
				remainingCopies = stack.Count;
				newUpgradeLevel = stack.UpgradeLevel;
				return false;
			}

			if (stack.Count < ForgeRequiredCopies)
            {
                reason = $"Need {ForgeRequiredCopies} identical copies";
                remainingCopies = stack.Count;
                newUpgradeLevel = stack.UpgradeLevel;
                return false;
            }

            int cost = waiveOzTileCost ? 0 : GetForgeOzTileCost(data, stack.UpgradeLevel);
            if (cost > 0 && !CanSpendForgeOzTile(profile, cost))
            {
                reason = $"Need {cost} OzTile";
                remainingCopies = stack.Count;
                newUpgradeLevel = stack.UpgradeLevel;
                return false;
            }

            if (cost > 0 && !SpendForgeOzTile(profile, cost))
            {
                reason = $"Need {cost} OzTile";
                remainingCopies = stack.Count;
                newUpgradeLevel = stack.UpgradeLevel;
                return false;
            }

            stack.Count -= ForgeRequiredCopies;
            int targetUpgradeLevel = normalizedSourceLevel + 1;
            MahjongBattleTileStackData targetStack = EnsureTileStack(inventory, id, targetUpgradeLevel, 0);
            if (targetStack.Count < int.MaxValue)
                targetStack.Count++;
            remainingCopies = targetStack.Count;
            newUpgradeLevel = targetUpgradeLevel;

            if (!OwnsTile(inventory, id))
                inventory.ReserveTileIds.Insert(0, id);

            inventory.EnsureValid();
            return true;
        }

        [Serializable]
        public sealed class ForgeAscendSacrifice
        {
            public string TileId;
            public int UpgradeLevel;

            public ForgeAscendSacrifice(string tileId, int upgradeLevel)
            {
                TileId = tileId?.Trim() ?? string.Empty;
                UpgradeLevel = Mathf.Max(0, upgradeLevel);
            }
        }

        public sealed class ForgeAscendResult
        {
            public bool Success;
            public bool Hit;
            public bool Pity;
            public string Message;
            public BattleTileRarity SourceRarity;
            public BattleTileRarity TargetRarity;
            public BattleTileData RewardTile;
            public int ConsumedCopies;
            public int OzTileCost;
            public int PityCount;
            public int PityLimit;
            public float Chance;
        }

        [Obsolete("Use explicit sacrifice selection overload.")]
        public static bool CanForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, out string reason)
        {
			reason = "Sacrifice selection required";
			return false;
        }

        public static bool TryGetForgeAscendRequirements(BattleTileRarity targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out float chance)
        {
            return TryGetAscendConfig(targetRarity, out sourceRarity, out requiredCopies, out ozTileCost, out chance, out _, out _);
        }

		public static int GetForgeAscendSelectableCount(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, string tileId)
		{
			return GetForgeAscendSelectableCount(profile, store, targetRarity, tileId, 0);
		}

		public static int GetForgeAscendSelectableCount(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, string tileId, int upgradeLevel)
		{
			if (profile == null || store == null || string.IsNullOrWhiteSpace(tileId) || !TryGetAscendConfig(targetRarity, out BattleTileRarity sourceRarity, out _, out _, out _, out _, out _))
				return 0;

			string id = tileId.Trim();
			if (!store.TryGetTileDataById(id, out BattleTileData data) || data == null || data.Rarity != sourceRarity || IsBaseBattleTile(id))
				return 0;

			EnsureInventoryForStore(profile, store);
			return GetReserveCopyCount(GetOrCreateInventory(profile), id, Mathf.Max(0, upgradeLevel));
		}

		[Obsolete("Use GetForgeAscendPreviewChance with a profile to include failure streak and guarantee.")]
		public static float GetForgeAscendEffectiveChance(BattleTileRarity targetRarity, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices)
		{
			return GetForgeAscendPreviewChance(null, targetRarity, selectedSacrifices, out _, out _, out _);
		}

		public static float GetForgeAscendPreviewChance(PlayerProfile profile, BattleTileRarity targetRarity, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices, out float sacrificeBonus, out float failureBonus, out bool guaranteed)
		{
			sacrificeBonus = 0f;
			failureBonus = 0f;
			guaranteed = false;
			if (!TryGetAscendConfig(targetRarity, out _, out _, out _, out float baseChance, out int pityLimit, out _))
				return 0f;

			long totalUpgradeLevels = 0L;
			if (selectedSacrifices != null)
			{
				for (int i = 0; i < selectedSacrifices.Count; i++)
					totalUpgradeLevels += Mathf.Max(0, selectedSacrifices[i]?.UpgradeLevel ?? 0);
			}
			sacrificeBonus = (float)Math.Min(BattleTileStore.AscendMaximumChance, totalUpgradeLevels * (double)BattleTileStore.AscendChanceBonusPerUpgradeLevel);

			int failureCount = 0;
			if (profile != null)
			{
				MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
				failureCount = GetAscendPityCount(inventory, targetRarity);
			}
			float failureBonusPerAttempt = targetRarity == BattleTileRarity.Mythic
				? BattleTileStore.AscendMythicFailureChanceBonus
				: BattleTileStore.AscendLegendaryFailureChanceBonus;
			failureBonus = Mathf.Max(0, failureCount) * failureBonusPerAttempt;
			guaranteed = pityLimit > 0 && failureCount >= Mathf.Max(0, pityLimit - 1);
			if (guaranteed)
				return 1f;

			double effectiveChance = baseChance + sacrificeBonus + failureBonus;
			return Mathf.Clamp((float)Math.Min(BattleTileStore.AscendMaximumChance, effectiveChance), 0f, BattleTileStore.AscendMaximumChance);
		}

		[Obsolete("Use ForgeAscendSacrifice overload to preserve upgrade levels.")]
        public static bool CanForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, IReadOnlyList<string> selectedTileIds, out string reason)
		{
			List<ForgeAscendSacrifice> sacrifices = new();
			if (selectedTileIds != null)
			{
				for (int i = 0; i < selectedTileIds.Count; i++)
					sacrifices.Add(new ForgeAscendSacrifice(selectedTileIds[i], 0));
			}
			return CanForgeAscend(profile, store, targetRarity, sacrifices, out reason);
		}

		public static bool CanForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices, out string reason)
        {
            reason = string.Empty;
            if (!TryGetAscendConfig(targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out _, out _, out _))
            {
                reason = "Unsupported ascension";
                return false;
            }

            if (profile == null || store == null)
            {
                reason = "Forge is not ready";
                return false;
            }

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (!ValidateAscendSelection(inventory, store, sourceRarity, requiredCopies, selectedSacrifices, out reason))
                return false;

            if (ozTileCost > 0 && !CanSpendForgeOzTile(profile, ozTileCost))
            {
                reason = $"Need {ozTileCost} OzTile";
                return false;
            }

            if (GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: true).Count == 0 &&
                GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: false).Count == 0)
            {
                reason = $"No {targetRarity} rewards configured";
                return false;
            }

            return true;
        }

        [Obsolete("Explicit sacrifice selection is required for ascension.")]
        public static ForgeAscendResult TryForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity)
        {
            return new ForgeAscendResult
            {
                Success = false,
                TargetRarity = targetRarity,
                Message = "Sacrifice selection required"
            };
        }

		[Obsolete("Use ForgeAscendSacrifice overload to preserve upgrade levels.")]
        public static ForgeAscendResult TryForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, IReadOnlyList<string> selectedTileIds)
		{
			List<ForgeAscendSacrifice> sacrifices = new();
			if (selectedTileIds != null)
			{
				for (int i = 0; i < selectedTileIds.Count; i++)
					sacrifices.Add(new ForgeAscendSacrifice(selectedTileIds[i], 0));
			}
			return TryForgeAscend(profile, store, targetRarity, sacrifices);
		}

		public static ForgeAscendResult TryForgeAscend(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices)
        {
            ForgeAscendResult result = new ForgeAscendResult
            {
                Success = false,
                TargetRarity = targetRarity,
                Message = string.Empty
            };

            if (!TryGetAscendConfig(targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out float chance, out int pityLimit, out _))
            {
                result.Message = "Unsupported ascension";
                return result;
            }

            result.SourceRarity = sourceRarity;
            result.ConsumedCopies = 0;
            result.OzTileCost = ozTileCost;
            result.Chance = chance;
            result.PityLimit = pityLimit;

            if (profile == null || store == null)
            {
                result.Message = "Forge is not ready";
                return result;
            }
			if (ProfileService.I != null && ProfileService.I.Current != null && !ReferenceEquals(ProfileService.I.Current, profile))
			{
				result.Message = "Profile changed";
				return result;
			}

            EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = GetOrCreateInventory(profile);
            if (!ValidateAscendSelection(inventory, store, sourceRarity, requiredCopies, selectedSacrifices, out string selectionReason))
            {
                result.Message = selectionReason;
                return result;
            }

            if (ozTileCost > 0 && !CanSpendForgeOzTile(profile, ozTileCost))
            {
                result.Message = $"Need {ozTileCost} OzTile";
                return result;
            }

            if (GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: true).Count == 0 &&
                GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: false).Count == 0)
            {
                result.Message = $"No {targetRarity} rewards configured";
                return result;
            }

			chance = GetForgeAscendPreviewChance(profile, targetRarity, selectedSacrifices, out _, out _, out bool guaranteed);
			result.Chance = chance;
			int pityCount = GetAscendPityCount(inventory, targetRarity);
			bool pityHit = guaranteed || pityLimit > 0 && pityCount >= Mathf.Max(0, pityLimit - 1);
            bool chanceHit = UnityEngine.Random.value < chance;
            bool hit = pityHit || chanceHit;
			BattleTileData pendingReward = hit ? PickAscendReward(store, inventory, targetRarity) : null;
			if (hit && pendingReward == null)
			{
				result.Message = $"No {targetRarity} rewards configured";
				return result;
			}

			if (ozTileCost > 0 && !SpendForgeOzTileWithoutSave(profile, ozTileCost))
			{
				result.Message = $"Need {ozTileCost} OzTile";
				return result;
			}

            ConsumeSelectedSacrificeCopies(inventory, selectedSacrifices);
			result.ConsumedCopies = requiredCopies;
            if (hit)
            {
				GrantTileCopy(profile, store, pendingReward.Id, out _);
				SetAscendPityCount(inventory, targetRarity, 0);
				result.RewardTile = pendingReward;
                result.Hit = true;
                result.Pity = pityHit;
                result.PityCount = 0;
                result.Success = true;
                result.Message = "Ascension succeeded";
            }
            else
            {
                int nextPity = Mathf.Max(0, pityCount) + 1;
				SetAscendPityCount(inventory, targetRarity, nextPity);
                result.PityCount = nextPity;
                result.Success = true;
                result.Message = "Ascension failed";
            }

            inventory.EnsureValid();
			ProfileService.I?.Save();
			CurrencyService.I?.NotifyOzTileChangedAfterSave(profile);
            return result;
        }

		private static int GetAscendPityCount(MahjongBattleTileInventoryData inventory, BattleTileRarity targetRarity)
		{
			if (inventory == null)
				return 0;
			return targetRarity == BattleTileRarity.Mythic ? Mathf.Max(0, inventory.AscendMythicPityCount) : Mathf.Max(0, inventory.AscendLegendaryPityCount);
		}

		private static void SetAscendPityCount(MahjongBattleTileInventoryData inventory, BattleTileRarity targetRarity, int value)
		{
			if (inventory == null)
				return;
			if (targetRarity == BattleTileRarity.Mythic)
				inventory.AscendMythicPityCount = Mathf.Max(0, value);
			else
				inventory.AscendLegendaryPityCount = Mathf.Max(0, value);
		}

		private static void MigrateLegacyAscendPity(PlayerProfile profile, MahjongBattleTileInventoryData inventory)
		{
			if (profile == null || inventory == null)
				return;
			string profileKey = GetForgeProfileKey(profile);
			inventory.AscendLegendaryPityCount = Mathf.Max(inventory.AscendLegendaryPityCount, PlayerPrefs.GetInt(AscendLegendaryPityKey + profileKey, 0));
			inventory.AscendMythicPityCount = Mathf.Max(inventory.AscendMythicPityCount, PlayerPrefs.GetInt(AscendMythicPityKey + profileKey, 0));
		}

        private static bool ValidateAscendSelection(MahjongBattleTileInventoryData inventory, BattleTileStore store, BattleTileRarity sourceRarity, int requiredCopies, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices, out string reason)
        {
            reason = string.Empty;
            if (selectedSacrifices == null || selectedSacrifices.Count != requiredCopies)
            {
                reason = $"Select exactly {requiredCopies} {sourceRarity} copies";
                return false;
            }

            Dictionary<(string TileId, int UpgradeLevel), int> selectedCounts = new();
            for (int i = 0; i < selectedSacrifices.Count; i++)
            {
                ForgeAscendSacrifice sacrifice = selectedSacrifices[i];
                string id = sacrifice?.TileId?.Trim();
                if (string.IsNullOrWhiteSpace(id) || !store.TryGetTileDataById(id, out BattleTileData data) || data == null || data.Rarity != sourceRarity || IsBaseBattleTile(id))
                {
                    reason = "Invalid sacrifice selection";
                    return false;
                }

				var key = (id, Mathf.Max(0, sacrifice.UpgradeLevel));
                selectedCounts.TryGetValue(key, out int selectedCount);
                selectedCounts[key] = selectedCount + 1;
            }

            foreach (KeyValuePair<(string TileId, int UpgradeLevel), int> pair in selectedCounts)
            {
				int selectableCount = GetReserveCopyCount(inventory, pair.Key.TileId, pair.Key.UpgradeLevel);
				if (selectableCount < pair.Value)
                {
					reason = "Selected reserve copies are no longer available";
                    return false;
                }
            }

            return true;
        }

        private static void ConsumeSelectedSacrificeCopies(MahjongBattleTileInventoryData inventory, IReadOnlyList<ForgeAscendSacrifice> selectedSacrifices)
        {
            if (inventory?.TileStacks == null || selectedSacrifices == null)
                return;

			HashSet<string> affectedTileIds = new(StringComparer.Ordinal);
            for (int i = 0; i < selectedSacrifices.Count; i++)
            {
				ForgeAscendSacrifice sacrifice = selectedSacrifices[i];
                string id = sacrifice?.TileId?.Trim();
                if (string.IsNullOrWhiteSpace(id))
                    continue;
				MahjongBattleTileStackData stack = FindTileStack(inventory, id, Mathf.Max(0, sacrifice.UpgradeLevel));
                if (stack == null)
                    continue;
				stack.Count = Mathf.Max(0, stack.Count - 1);
				affectedTileIds.Add(id);
            }

			foreach (string tileId in affectedTileIds)
			{
				long total = 0L;
				for (int i = 0; i < inventory.TileStacks.Count; i++)
				{
					MahjongBattleTileStackData stack = inventory.TileStacks[i];
					if (stack != null && string.Equals(stack.TileId, tileId, StringComparison.Ordinal))
						total += Mathf.Max(0, stack.Count);
				}
				if (total <= 0L)
					RemoveTileFromInventoryLists(inventory, tileId);
			}

            inventory.EnsureValid();
        }

        private static bool TryGetAscendConfig(BattleTileRarity targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out float chance, out int pityLimit, out string pityKeyPrefix)
        {
            if (targetRarity == BattleTileRarity.Legendary)
            {
                sourceRarity = BattleTileRarity.Epic;
                requiredCopies = BattleTileStore.AscendLegendaryEpicCopies;
                ozTileCost = BattleTileStore.AscendLegendaryOzTileCost;
                chance = BattleTileStore.AscendLegendaryChance;
                pityLimit = BattleTileStore.AscendLegendaryPityLimit;
                pityKeyPrefix = AscendLegendaryPityKey;
                return true;
            }

            if (targetRarity == BattleTileRarity.Mythic)
            {
                sourceRarity = BattleTileRarity.Legendary;
                requiredCopies = BattleTileStore.AscendMythicLegendaryCopies;
                ozTileCost = BattleTileStore.AscendMythicOzTileCost;
                chance = BattleTileStore.AscendMythicChance;
                pityLimit = BattleTileStore.AscendMythicPityLimit;
                pityKeyPrefix = AscendMythicPityKey;
                return true;
            }

            sourceRarity = BattleTileRarity.Standard;
            requiredCopies = 0;
            ozTileCost = 0;
            chance = 0f;
            pityLimit = 0;
            pityKeyPrefix = string.Empty;
            return false;
        }

        public static int GetForgeOzTileCost(BattleTileData data, int currentUpgradeLevel)
        {
            if (data == null)
                return 0;

            int baseCost = data.Rarity switch
            {
                BattleTileRarity.Mythic => ForgeMythicOzTileCost,
                BattleTileRarity.Legendary => ForgeLegendaryOzTileCost,
                BattleTileRarity.Epic => ForgeEpicOzTileCost,
                BattleTileRarity.Rare => ForgeRareOzTileCost,
                _ => 0
            };

			long multiplier = Math.Max(1L, (long)Mathf.Max(0, currentUpgradeLevel) + 1L);
			long cost = (long)baseCost * multiplier;
			return cost >= int.MaxValue ? int.MaxValue : (int)cost;
		}

		[Obsolete("Forge upgrades no longer have a rarity maximum.")]
		public static int GetTotalForgeOzTileCostToMax(BattleTileData data)
		{
			if (data == null || data.Rarity < BattleTileRarity.Rare)
				return 0;
			return int.MaxValue;
		}

		[Obsolete("Forge upgrades no longer have a rarity maximum.")]
		public static int GetTotalCopiesRequiredForMaxForge(BattleTileRarity rarity)
		{
			return rarity >= BattleTileRarity.Rare ? int.MaxValue : 0;
		}

        private static int ResolveTrialForgeLevel(BattleTileData data)
        {
            if (data == null)
                return 0;

            return data.Rarity switch
            {
                BattleTileRarity.Mythic => 3,
                BattleTileRarity.Legendary => 3,
                BattleTileRarity.Epic => 2,
                BattleTileRarity.Rare => 1,
                _ => 0
            };
        }

		[Obsolete("Forge upgrades no longer have a rarity maximum.")]
		public static int GetMaxForgeUpgradeLevel(BattleTileRarity rarity)
		{
			return rarity >= BattleTileRarity.Rare ? int.MaxValue : 0;
		}

        private static bool CanSpendForgeOzTile(PlayerProfile profile, int cost)
        {
            if (cost <= 0)
                return true;

            if (CurrencyService.I != null)
                return CurrencyService.I.CanSpendOzTile(cost);

            profile?.EnsureData();
            return profile?.Currencies != null && profile.Currencies.CanSpendTile(cost);
        }

        private static bool SpendForgeOzTile(PlayerProfile profile, int cost)
        {
            if (cost <= 0)
                return true;

            if (CurrencyService.I != null)
                return CurrencyService.I.SpendOzTile(cost);

            profile?.EnsureData();
            return profile?.Currencies != null && profile.Currencies.SpendTile(cost);
        }

		private static bool SpendForgeOzTileWithoutSave(PlayerProfile profile, int cost)
		{
			if (cost <= 0)
				return true;

			if (CurrencyService.I != null)
				return CurrencyService.I.SpendOzTileWithoutSave(profile, cost);

			profile?.EnsureData();
			return profile?.Currencies != null && profile.Currencies.SpendTile(cost);
		}

        private static void RemoveTileFromInventoryLists(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return;

            string id = tileId.Trim();
            inventory.ActiveTileIds?.RemoveAll(value => string.Equals(value, id, StringComparison.Ordinal));
            inventory.ReserveTileIds?.RemoveAll(value => string.Equals(value, id, StringComparison.Ordinal));
            if (string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal))
                inventory.TotemTileId = string.Empty;
        }

        private static BattleTileData PickAscendReward(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTileRarity targetRarity)
        {
            List<BattleTileData> candidates = GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: true);
            if (candidates.Count == 0)
                candidates = GetCandidateTilesByRarity(store, inventory, targetRarity, onlyNew: false);

            if (candidates.Count == 0)
                return null;

            candidates.Sort((left, right) => string.Compare(left?.Id, right?.Id, StringComparison.Ordinal));
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static List<BattleTileData> GetCandidateTilesByRarity(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTileRarity rarity, bool onlyNew)
        {
            List<BattleTileData> result = new();
            IReadOnlyList<BattleTileData> tiles = store != null ? store.BattleTiles : null;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData tile = tiles[i];
                if (tile == null ||
                    tile.Prefab == null ||
                    string.IsNullOrWhiteSpace(tile.Id) ||
                    tile.Rarity != rarity ||
                    IsBaseBattleTile(tile.Id))
                {
                    continue;
                }

                if (onlyNew && OwnsTile(inventory, tile.Id))
                    continue;

                result.Add(tile);
            }

            return result;
        }

        private static string GetForgeProfileKey(PlayerProfile profile)
        {
            profile?.EnsureData();
            return string.IsNullOrWhiteSpace(profile?.LocalProfileId) ? "default" : profile.LocalProfileId.Trim();
        }

        private static List<string> GetValidStoreIds(BattleTileStore store)
        {
            List<string> ids = new();
            IReadOnlyList<BattleTileData> tiles = store.BattleTiles;
            if (tiles == null)
                return ids;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData data = tiles[i];
                string id = data != null ? data.Id : string.Empty;
                if (data?.Prefab == null || string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (!ids.Contains(id))
                    ids.Add(id);
            }

            return ids;
        }

        private static void ApplyMatchedTileActiveBonus(
            BattleTileStore store,
            BattleTile tile,
            BattleLoadoutSnapshot battleLoadout,
            ref int attack,
            ref float critChance,
            ref float critDamageMultiplier,
            ref int selfHeal)
        {
            if (tile == null || string.IsNullOrWhiteSpace(tile.Id))
                return;

            if (!store.TryGetTileDataById(tile.Id, out BattleTileData data))
                return;

            BattleTileActiveBonusData bonus = data?.ActiveBonus;
            if (bonus == null || !bonus.HasAnyBonus())
                return;

            int upgradeLevel = BattleLoreTutorialSession.IsActive
                ? ResolveTrialForgeLevel(data)
                : battleLoadout != null ? battleLoadout.GetUpgradeLevel(tile.Id) : GetUpgradeLevel(ProfileService.I?.Current, tile.Id);
            float multiplier = GetRarityPowerMultiplier(data.Rarity) * GetForgeMultiplier(upgradeLevel);
            attack += Mathf.Max(0, Mathf.RoundToInt(bonus.Attack * multiplier));
            critChance += Mathf.Max(0f, bonus.CritChance * multiplier);
            if (bonus.CritDamageMultiplier > 1f)
                critDamageMultiplier += (bonus.CritDamageMultiplier - 1f) * multiplier;
            selfHeal += Mathf.Max(0, Mathf.RoundToInt(bonus.HealSelf * multiplier));
        }

        private static BattleTileData ResolveSnapshotTotem(BattleLoadoutSnapshot loadout, BattleTileStore store)
        {
            if (loadout == null || store == null || string.IsNullOrWhiteSpace(loadout.TotemTileId))
                return null;

            return store.TryGetTileDataById(loadout.TotemTileId, out BattleTileData tile) ? tile : null;
        }

        private static bool ContainsTileId(IReadOnlyList<BattleTileData> tiles, string tileId)
        {
            if (tiles == null || string.IsNullOrWhiteSpace(tileId))
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (string.Equals(tiles[i]?.Id, tileId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static BattleStatsHub.BattleStatsSnapshot ClampRankedTileBonuses(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            BattleStatsHub.BattleStatsSnapshot modifiedStats)
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.RankedMatch)
                return modifiedStats;

            int maxHp = Mathf.Min(modifiedStats.MaxHp, Mathf.RoundToInt(baseStats.MaxHp * RankedMaxHpBonusMultiplier));
            int attack = Mathf.Min(modifiedStats.Attack, Mathf.RoundToInt(baseStats.Attack * RankedAttackBonusMultiplier));
            float armor = Mathf.Min(modifiedStats.Armor, baseStats.Armor + RankedMaxArmorBonus);
            float parryChance = Mathf.Min(modifiedStats.ParryChance, baseStats.ParryChance + RankedMaxParryBonus);
            float critChance = Mathf.Min(modifiedStats.CritChance, baseStats.CritChance + RankedMaxCritChanceBonus);
            float critDamageMultiplier = Mathf.Min(modifiedStats.CritDamageMultiplier, baseStats.CritDamageMultiplier + RankedMaxCritDamageBonus);

            return new BattleStatsHub.BattleStatsSnapshot(
                maxHp,
                attack,
                armor,
                parryChance,
                critChance,
                critDamageMultiplier);
        }

        private static void ClampRankedMatchedTileBonuses(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            ref int attack,
            ref float critChance,
            ref float critDamageMultiplier,
            ref int selfHeal)
        {
            if (MahjongBattleLobbySession.SelectedMode != MahjongBattleLobbyMode.RankedMatch)
                return;

            attack = Mathf.Min(attack, Mathf.RoundToInt(baseStats.Attack * RankedAttackBonusMultiplier));
            critChance = Mathf.Min(critChance, baseStats.CritChance + RankedMaxCritChanceBonus);
            critDamageMultiplier = Mathf.Min(critDamageMultiplier, baseStats.CritDamageMultiplier + RankedMaxCritDamageBonus);
            selfHeal = Mathf.Min(selfHeal, Mathf.RoundToInt(baseStats.MaxHp * RankedMaxSelfHealMultiplier));
        }

        private static bool IsSymbiosisActive(BattleTileData data, BattleCharacterDatabase.BattleCharacterData selectedCharacter)
        {
            if (data == null || selectedCharacter == null || data.SymbiosisAnimalTypes == null || data.SymbiosisAnimalTypes.Count == 0)
                return false;

            for (int i = 0; i < data.SymbiosisAnimalTypes.Count; i++)
            {
                if (data.SymbiosisAnimalTypes[i] == selectedCharacter.AnimalType)
                    return data.SymbiosisBonus != null && data.SymbiosisBonus.HasAnyBonus();
            }

            return false;
        }

        private static void ApplyPassiveBonus(
            BattleTileBonusData bonus,
            BattleTileRarity rarity,
            int upgradeLevel,
            ref int maxHp,
            ref int attack,
            ref float armor,
            ref float parryChance,
            ref float critChance,
            ref float critDamageMultiplier)
        {
            if (bonus == null || !bonus.HasAnyBonus())
                return;

            float multiplier = GetRarityPowerMultiplier(rarity) * GetForgeMultiplier(upgradeLevel);
            maxHp += Mathf.Max(0, Mathf.RoundToInt(bonus.MaxHp * multiplier));
            attack += Mathf.Max(0, Mathf.RoundToInt(bonus.Attack * multiplier));
            armor += Mathf.Max(0f, bonus.Armor * multiplier);
            parryChance += Mathf.Max(0f, bonus.ParryChance * multiplier);
            critChance += Mathf.Max(0f, bonus.CritChance * multiplier);
            if (bonus.CritDamageMultiplier > 1f)
                critDamageMultiplier += (bonus.CritDamageMultiplier - 1f) * multiplier;
        }

        private static float GetForgeMultiplier(int upgradeLevel)
        {
            return 1f + Mathf.Max(0, upgradeLevel) * ForgeBonusGrowthPerLevel;
        }

        public static float GetRarityPowerMultiplier(BattleTileRarity rarity)
        {
            return rarity switch
            {
                BattleTileRarity.Mythic => 1.85f,
                BattleTileRarity.Legendary => 1.45f,
                BattleTileRarity.Epic => 1.18f,
                BattleTileRarity.Rare => 1.08f,
                _ => 1f
            };
        }

        private static bool OwnsTile(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return false;

            string id = tileId.Trim();
            return inventory.ActiveTileIds.Contains(id)
                   || inventory.ReserveTileIds.Contains(id)
                   || string.Equals(inventory.TotemTileId, id, StringComparison.Ordinal);
        }

        private static bool EnsureStacksForOwnedTiles(MahjongBattleTileInventoryData inventory)
        {
            if (inventory == null)
                return false;

            bool changed = false;
            for (int i = 0; i < inventory.ActiveTileIds.Count; i++)
                changed |= EnsureTileStackChanged(inventory, inventory.ActiveTileIds[i], 1);

            for (int i = 0; i < inventory.ReserveTileIds.Count; i++)
                changed |= EnsureTileStackChanged(inventory, inventory.ReserveTileIds[i], 1);

            if (!string.IsNullOrWhiteSpace(inventory.TotemTileId))
                changed |= EnsureTileStackChanged(inventory, inventory.TotemTileId, 1);

            return changed;
        }

        private static bool EnsureTileStackChanged(MahjongBattleTileInventoryData inventory, string tileId, int minimumCount)
        {
            int beforeCount = GetOwnedCount(inventory, tileId);
            if (beforeCount >= minimumCount)
                return false;

            MahjongBattleTileStackData stack = EnsureTileStack(inventory, tileId, 0, 0);
            stack.Count += minimumCount - beforeCount;
            return true;
        }

        private static MahjongBattleTileStackData EnsureTileStack(MahjongBattleTileInventoryData inventory, string tileId, int minimumCount)
        {
            return EnsureTileStack(inventory, tileId, 0, minimumCount);
        }

        private static MahjongBattleTileStackData EnsureTileStack(MahjongBattleTileInventoryData inventory, string tileId, int upgradeLevel, int minimumCount)
        {
            int normalizedUpgradeLevel = Mathf.Max(0, upgradeLevel);
            MahjongBattleTileStackData stack = FindTileStack(inventory, tileId, normalizedUpgradeLevel);
            if (stack != null)
            {
                stack.Count = Mathf.Max(stack.Count, minimumCount);
                return stack;
            }

            if (inventory.TileStacks == null)
                inventory.TileStacks = new List<MahjongBattleTileStackData>();

            stack = new MahjongBattleTileStackData(tileId, Mathf.Max(0, minimumCount), normalizedUpgradeLevel);
            inventory.TileStacks.Add(stack);
            return stack;
        }

        private static MahjongBattleTileStackData FindTileStack(MahjongBattleTileInventoryData inventory, string tileId, int upgradeLevel)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return null;

            inventory.EnsureValid();
            string id = tileId.Trim();
            int normalizedUpgradeLevel = Mathf.Max(0, upgradeLevel);
            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                MahjongBattleTileStackData stack = inventory.TileStacks[i];
                if (stack != null && stack.UpgradeLevel == normalizedUpgradeLevel && string.Equals(stack.TileId, id, StringComparison.Ordinal))
                    return stack;
            }

            return null;
        }

        private static MahjongBattleTileStackData FindHighestTileStack(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return null;

            inventory.EnsureValid();
            string id = tileId.Trim();
            MahjongBattleTileStackData result = null;
            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                MahjongBattleTileStackData stack = inventory.TileStacks[i];
                if (stack != null && stack.Count > 0 && string.Equals(stack.TileId, id, StringComparison.Ordinal) && (result == null || stack.UpgradeLevel > result.UpgradeLevel))
                    result = stack;
            }

            return result;
        }

        private static int FindHighestForgeableLevel(MahjongBattleTileInventoryData inventory, string tileId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(tileId))
                return 0;

            inventory.EnsureValid();
            string id = tileId.Trim();
            int highestOwnedLevel = 0;
            int highestForgeableLevel = -1;
            for (int i = 0; i < inventory.TileStacks.Count; i++)
            {
                MahjongBattleTileStackData stack = inventory.TileStacks[i];
                if (stack == null || stack.Count <= 0 || !string.Equals(stack.TileId, id, StringComparison.Ordinal))
                    continue;

                highestOwnedLevel = Mathf.Max(highestOwnedLevel, stack.UpgradeLevel);
                if (stack.Count >= ForgeRequiredCopies)
                    highestForgeableLevel = Mathf.Max(highestForgeableLevel, stack.UpgradeLevel);
            }

            return highestForgeableLevel >= 0 ? highestForgeableLevel : highestOwnedLevel;
        }

        private static int FindBaseTileIndex(List<string> ids, string exceptId, string protectedId = null)
        {
            if (ids == null)
                return -1;

            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string id = ids[i];
                if (string.Equals(id, exceptId, StringComparison.Ordinal) ||
                    string.Equals(id, protectedId, StringComparison.Ordinal))
                    continue;

                if (IsBaseBattleTile(id))
                    return i;
            }

            return -1;
        }

        private static void RemoveStandardTileIds(List<string> ids)
        {
            if (ids == null)
                return;

            for (int i = ids.Count - 1; i >= 0; i--)
            {
                if (IsBaseBattleTile(ids[i]))
                    ids.RemoveAt(i);
            }
        }

        private static void RemoveMissingIds(List<string> ids, List<string> allIds)
        {
            if (ids == null)
                return;

            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id) || !allIds.Contains(id.Trim()))
                    ids.RemoveAt(i);
                else
                    ids[i] = id.Trim();
            }
        }
    }
}
