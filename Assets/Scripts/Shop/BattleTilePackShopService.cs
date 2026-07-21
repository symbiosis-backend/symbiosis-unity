using System;
using System.Collections.Generic;
using UnityEngine;
using MahjongGame.Monetization;

namespace MahjongGame
{
    public enum BattleTilePackId
    {
        DailyAd,
        OzTileMedium,
        OzTileHigh,
        AmetistPremium
    }

    public sealed class BattleTilePackRoll
    {
        public BattleTileData Tile;
        public BattleTileRarity Rarity;
        public bool IsNew;
        public bool AutoSold;
        public int AutoSoldOzTile;
        public bool Guaranteed;
        public bool Pity;
    }

    public sealed class BattleTilePackResult
    {
        public bool Success;
        public string Message;
        public List<BattleTilePackRoll> Rolls = new();
        public int AutoSoldOzTile;
        public int GuaranteedUpgrades;
        public bool PityTriggered;
        public bool FixedBundle;
    }

    public sealed class BattleTileFixedReward
    {
        public BattleTileRarity Rarity;
        public int Count;
    }

    public sealed class BattleTilePackDefinition
    {
        public BattleTilePackId PackId;
        public int OzTileCost;
        public int AmetistCost;
        public int RollCount;
        public bool RequiresRewardedAd;
        public BattleTileRarity GuaranteedMinimum = BattleTileRarity.Rare;
        public int GuaranteedMinimumCount;
        public BattleTileRarity PityRarity = BattleTileRarity.Legendary;
        public int PityPackLimit;
        public (BattleTileRarity rarity, int weight)[] Weights;
        public BattleTileFixedReward[] FixedRewards;

        public bool IsRandomized => FixedRewards == null || FixedRewards.Length == 0;
    }

    public static class BattleTilePackShopService
    {
        public const int MediumOzTileCost = 500;
        public const int HighOzTileCost = 1500;
        public const int PremiumAmetistCost = 120;

        private const string DailyAdDatePrefix = "battle_tile_pack_daily_ad_";
        private const string DailyAdRarePityPrefix = "battle_tile_pack_daily_rare_pity_";

        private static readonly BattleTilePackDefinition DailyAdPack = new()
        {
            PackId = BattleTilePackId.DailyAd,
            RequiresRewardedAd = true,
            RollCount = 10,
            GuaranteedMinimum = BattleTileRarity.Rare,
            GuaranteedMinimumCount = 0,
            PityRarity = BattleTileRarity.Rare,
            PityPackLimit = 3,
            Weights = new[]
            {
                (BattleTileRarity.Standard, 82),
                (BattleTileRarity.Rare, 15),
                (BattleTileRarity.Epic, 3)
            }
        };

        private static readonly BattleTilePackDefinition MediumPack = new()
        {
            PackId = BattleTilePackId.OzTileMedium,
            OzTileCost = MediumOzTileCost,
            RollCount = 30,
            GuaranteedMinimum = BattleTileRarity.Rare,
            GuaranteedMinimumCount = 1,
            Weights = new[]
            {
                (BattleTileRarity.Standard, 700),
                (BattleTileRarity.Rare, 220),
                (BattleTileRarity.Epic, 80)
            }
        };

        private static readonly BattleTilePackDefinition HighPack = new()
        {
            PackId = BattleTilePackId.OzTileHigh,
            OzTileCost = HighOzTileCost,
            RollCount = 55,
            GuaranteedMinimum = BattleTileRarity.Epic,
            GuaranteedMinimumCount = 1,
            Weights = new[]
            {
                (BattleTileRarity.Standard, 570),
                (BattleTileRarity.Rare, 290),
                (BattleTileRarity.Epic, 140)
            }
        };

        private static readonly BattleTilePackDefinition PremiumPack = new()
        {
            PackId = BattleTilePackId.AmetistPremium,
            AmetistCost = PremiumAmetistCost,
            RollCount = 3,
            GuaranteedMinimum = BattleTileRarity.Rare,
            FixedRewards = new[]
            {
                new BattleTileFixedReward { Rarity = BattleTileRarity.Rare, Count = 2 },
                new BattleTileFixedReward { Rarity = BattleTileRarity.Epic, Count = 1 }
            }
        };

        public static BattleTilePackDefinition GetDefinition(BattleTilePackId packId)
        {
            return packId switch
            {
                BattleTilePackId.DailyAd => DailyAdPack,
                BattleTilePackId.OzTileMedium => MediumPack,
                BattleTilePackId.OzTileHigh => HighPack,
                BattleTilePackId.AmetistPremium => PremiumPack,
                _ => MediumPack
            };
        }

        public static bool HasClaimedDailyAd(PlayerProfile profile)
        {
            return string.Equals(PlayerPrefs.GetString(DailyAdDatePrefix + GetProfileKey(profile), string.Empty), GetTodayKey(), StringComparison.Ordinal);
        }

        public static bool CanClaimDailyAd(PlayerProfile profile)
        {
            return profile != null && !HasClaimedDailyAd(profile);
        }

        public static void TryOpenRewardedDailyPack(PlayerProfile profile, BattleTileStore store, Action<BattleTilePackResult> onComplete)
        {
            if (!CanClaimDailyAd(profile))
            {
                onComplete?.Invoke(Fail("Daily ad pack already claimed."));
                return;
            }

            if (store == null || !CanResolveRandomizedPack(store, DailyAdPack))
            {
                onComplete?.Invoke(Fail("Battle tile pack configuration is incomplete."));
                return;
            }

            MonetizationService service = MonetizationService.Ensure();
            service.ShowRewardedAd(MonetizationService.BattleTilePackRewardedPlacementId, result =>
            {
                if (!result.IsCompleted)
                {
                    onComplete?.Invoke(Fail(string.IsNullOrWhiteSpace(result.Message) ? "Rewarded ad is not ready." : result.Message));
                    return;
                }

                BattleTilePackResult packResult = OpenPack(profile, store, BattleTilePackId.DailyAd, chargeCost: false);
                if (packResult.Success)
                {
                    PlayerPrefs.SetString(DailyAdDatePrefix + GetProfileKey(profile), GetTodayKey());
                    PlayerPrefs.Save();
                }

                onComplete?.Invoke(packResult);
            });
        }

        public static BattleTilePackResult TryOpenPaidPack(PlayerProfile profile, BattleTileStore store, BattleTilePackId packId)
        {
            BattleTilePackDefinition definition = GetDefinition(packId);
            if (definition.RequiresRewardedAd)
                return Fail("This pack requires rewarded ad.");

            return OpenPack(profile, store, packId, chargeCost: true);
        }

        public static string FormatOdds(BattleTilePackId packId)
        {
            BattleTilePackDefinition definition = GetDefinition(packId);
            if (definition == null || !definition.IsRandomized)
                return string.Empty;

            if (definition.Weights == null || definition.Weights.Length == 0)
                return string.Empty;

            int total = 0;
            for (int i = 0; i < definition.Weights.Length; i++)
                total += Mathf.Max(0, definition.Weights[i].weight);

            if (total <= 0)
                return string.Empty;

            List<string> parts = new();
            for (int i = 0; i < definition.Weights.Length; i++)
            {
                int weight = Mathf.Max(0, definition.Weights[i].weight);
                if (weight <= 0)
                    continue;

                float percent = weight * 100f / total;
                string percentText = FormatPercent(percent);
                parts.Add($"{definition.Weights[i].rarity} {percentText}");
            }

            return string.Join(" / ", parts);
        }

        public static string FormatFixedRewards(BattleTilePackId packId)
        {
            BattleTilePackDefinition definition = GetDefinition(packId);
            if (definition?.FixedRewards == null || definition.FixedRewards.Length == 0)
                return string.Empty;

            List<string> parts = new();
            for (int i = 0; i < definition.FixedRewards.Length; i++)
            {
                BattleTileFixedReward reward = definition.FixedRewards[i];
                if (reward == null || reward.Count <= 0)
                    continue;

                parts.Add($"{reward.Count}x {reward.Rarity}");
            }

            return string.Join(" / ", parts);
        }

        private static BattleTilePackResult OpenPack(PlayerProfile profile, BattleTileStore store, BattleTilePackId packId, bool chargeCost)
        {
            if (profile == null || store == null)
                return Fail("Battle tile store is not ready.");

            BattleTilePackDefinition definition = GetDefinition(packId);
            if (!HasAnyPackTile(store))
                return Fail("No battle tiles available.");

            BattleTileInventoryService.EnsureInventoryForStore(profile, store);
            MahjongBattleTileInventoryData inventory = BattleTileInventoryService.GetOrCreateInventory(profile);
            if (inventory == null)
                return Fail("Battle tile inventory is not ready.");

            if (!definition.IsRandomized)
                return OpenFixedPack(profile, store, inventory, definition, chargeCost);

            if (!CanResolveRandomizedPack(store, definition))
                return Fail("Battle tile pack configuration is incomplete.");

            if (chargeCost && !SpendCost(definition))
                return Fail("Not enough currency.");

            int rollCount = Mathf.Max(1, definition.RollCount);
            List<BattleTileRarity> rolledRarities = new(rollCount);
            for (int i = 0; i < rollCount; i++)
                rolledRarities.Add(RollRarity(definition));

            HashSet<int> guaranteedRollIndices = ApplyGuaranteedRarities(definition, rolledRarities);
            BattleTilePackResult result = new()
            {
                Success = true,
                GuaranteedUpgrades = guaranteedRollIndices.Count
            };
            for (int i = 0; i < rolledRarities.Count; i++)
            {
                BattleTileRarity rarity = rolledRarities[i];
                BattleTileData tile = PickTile(store, inventory, rarity);
                AddRollResult(profile, store, result, tile, guaranteedRollIndices.Contains(i), pity: false);
            }

            if (result.Rolls.Count == 0)
                return Fail("No battle tiles available.");

            ApplyPity(profile, store, packId, definition, result);
            UpdatePityCounters(profile, packId, result);

            if (result.AutoSoldOzTile > 0)
                CurrencyService.I?.AddOzTile(result.AutoSoldOzTile);

            inventory.EnsureValid();
            ProfileService.I?.Save();
            ProfileService.I?.NotifyProfileChanged();
            return result;
        }

        private static BattleTilePackResult OpenFixedPack(PlayerProfile profile, BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTilePackDefinition definition, bool chargeCost)
        {
            List<BattleTileData> selectedTiles = BuildFixedRewardTiles(store, inventory, definition);
            if (selectedTiles.Count <= 0)
                return Fail("Fixed bundle is not available.");

            int expectedCount = GetFixedRewardCount(definition);
            if (selectedTiles.Count < expectedCount)
                return Fail("Fixed bundle does not have enough configured tiles.");

            if (chargeCost && !SpendCost(definition))
                return Fail("Not enough currency.");

            BattleTilePackResult result = new() { Success = true, FixedBundle = true };
            for (int i = 0; i < selectedTiles.Count; i++)
                AddRollResult(profile, store, result, selectedTiles[i], guaranteed: true, pity: false);

            inventory.EnsureValid();
            ProfileService.I?.Save();
            ProfileService.I?.NotifyProfileChanged();
            return result;
        }

        private static int GetFixedRewardCount(BattleTilePackDefinition definition)
        {
            int count = 0;
            if (definition?.FixedRewards == null)
                return count;

            for (int i = 0; i < definition.FixedRewards.Length; i++)
            {
                BattleTileFixedReward reward = definition.FixedRewards[i];
                if (reward != null && reward.Count > 0)
                    count += reward.Count;
            }

            return count;
        }

        private static List<BattleTileData> BuildFixedRewardTiles(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTilePackDefinition definition)
        {
            List<BattleTileData> selected = new();
            if (definition?.FixedRewards == null)
                return selected;

            HashSet<string> selectedIds = new(StringComparer.Ordinal);
            for (int rewardIndex = 0; rewardIndex < definition.FixedRewards.Length; rewardIndex++)
            {
                BattleTileFixedReward reward = definition.FixedRewards[rewardIndex];
                if (reward == null || reward.Count <= 0)
                    continue;

                for (int i = 0; i < reward.Count; i++)
                {
                    BattleTileData tile = PickFixedTile(store, inventory, reward.Rarity, selectedIds);
                    if (tile == null)
                        return selected;

                    selected.Add(tile);
                    selectedIds.Add(tile.Id.Trim());
                }
            }

            return selected;
        }

        private static BattleTileData PickFixedTile(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTileRarity rarity, HashSet<string> selectedIds)
        {
            List<BattleTileData> candidates = GetCandidateTiles(store, inventory, rarity, onlyNew: true);
            SortCandidatesById(candidates);
            for (int i = 0; i < candidates.Count; i++)
            {
                BattleTileData tile = candidates[i];
                if (tile != null && !selectedIds.Contains(tile.Id.Trim()))
                    return tile;
            }

            candidates = GetCandidateTiles(store, inventory, rarity, onlyNew: false);
            SortCandidatesById(candidates);
            for (int i = 0; i < candidates.Count; i++)
            {
                BattleTileData tile = candidates[i];
                if (tile != null && !selectedIds.Contains(tile.Id.Trim()))
                    return tile;
            }

            return null;
        }

        private static void SortCandidatesById(List<BattleTileData> candidates)
        {
            candidates?.Sort((left, right) => string.Compare(left?.Id, right?.Id, StringComparison.Ordinal));
        }

        private static void AddRollResult(PlayerProfile profile, BattleTileStore store, BattleTilePackResult result, BattleTileData tile, bool guaranteed, bool pity)
        {
            if (profile == null || store == null || result == null || tile == null)
                return;

            bool autoSold = IsAutoSoldStandardTile(tile);
            int autoSoldOzTile = autoSold ? GetAutoSellOzTile(tile) : 0;
            bool isNew = false;
            if (autoSold)
            {
                if (autoSoldOzTile > 0)
                    result.AutoSoldOzTile += autoSoldOzTile;
            }
            else
            {
                BattleTileInventoryService.GrantTileCopy(profile, store, tile.Id, out isNew);
            }

            result.Rolls.Add(new BattleTilePackRoll
            {
                Tile = tile,
                Rarity = tile.Rarity,
                IsNew = isNew,
                AutoSold = autoSold,
                AutoSoldOzTile = autoSoldOzTile,
                Guaranteed = guaranteed,
                Pity = pity
            });
        }

        private static HashSet<int> ApplyGuaranteedRarities(BattleTilePackDefinition definition, List<BattleTileRarity> rarities)
        {
            HashSet<int> upgradedIndices = new();
            int targetCount = Mathf.Max(0, definition.GuaranteedMinimumCount);
            if (targetCount <= 0 || rarities == null || rarities.Count == 0)
                return upgradedIndices;

            int currentCount = 0;
            for (int i = 0; i < rarities.Count; i++)
            {
                if (rarities[i] >= definition.GuaranteedMinimum)
                    currentCount++;
            }

            while (currentCount < targetCount)
            {
                int replaceIndex = -1;
                BattleTileRarity lowestRarity = BattleTileRarity.Mythic;
                for (int i = 0; i < rarities.Count; i++)
                {
                    BattleTileRarity rarity = rarities[i];
                    if (rarity >= definition.GuaranteedMinimum || (replaceIndex >= 0 && rarity >= lowestRarity))
                        continue;

                    replaceIndex = i;
                    lowestRarity = rarity;
                }

                if (replaceIndex < 0)
                    break;

                rarities[replaceIndex] = definition.GuaranteedMinimum;
                upgradedIndices.Add(replaceIndex);
                currentCount++;
            }

            return upgradedIndices;
        }

        private static void ApplyPity(PlayerProfile profile, BattleTileStore store, BattleTilePackId packId, BattleTilePackDefinition definition, BattleTilePackResult result)
        {
            if (definition.PityPackLimit <= 0)
                return;

            ApplySpecificPity(profile, store, packId, definition.PityRarity, definition.PityPackLimit, result);
        }

        private static void ApplySpecificPity(PlayerProfile profile, BattleTileStore store, BattleTilePackId packId, BattleTileRarity pityRarity, int pityPackLimit, BattleTilePackResult result)
        {
            if (pityPackLimit <= 0)
                return;

            string key = GetPityKey(packId, pityRarity);
            if (string.IsNullOrEmpty(key))
                return;

            int countSinceHit = PlayerPrefs.GetInt(key + GetProfileKey(profile), 0);
            if (countSinceHit + 1 < pityPackLimit || HasStoredAtLeast(result, pityRarity))
                return;

            BattleTileData pityTile = PickTile(store, BattleTileInventoryService.GetOrCreateInventory(profile), pityRarity);
            if (pityTile == null || IsAutoSoldStandardTile(pityTile))
                return;

            int replaceIndex = FindAutoSoldReplaceableRollIndex(result);
            if (replaceIndex >= 0)
            {
                RemoveAutoSoldValue(result, result.Rolls[replaceIndex]);
                result.Rolls.RemoveAt(replaceIndex);
            }

            AddRollResult(profile, store, result, pityTile, guaranteed: false, pity: true);
            result.PityTriggered = true;
        }

        private static void UpdatePityCounters(PlayerProfile profile, BattleTilePackId packId, BattleTilePackResult result)
        {
            string profileKey = GetProfileKey(profile);
            if (packId == BattleTilePackId.DailyAd)
                SetPityCount(DailyAdRarePityPrefix + profileKey, HasStoredAtLeast(result, BattleTileRarity.Rare));
            PlayerPrefs.Save();
        }

        private static void SetPityCount(string key, bool hit)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            PlayerPrefs.SetInt(key, hit ? 0 : Mathf.Max(0, PlayerPrefs.GetInt(key, 0)) + 1);
        }

        private static bool SpendCost(BattleTilePackDefinition definition)
        {
            if (CurrencyService.I == null)
                return false;

            if (definition.OzTileCost > 0)
                return CurrencyService.I.SpendOzTile(definition.OzTileCost);

            if (definition.AmetistCost > 0)
                return CurrencyService.I.SpendOzAmetist(definition.AmetistCost);

            return true;
        }

        private static BattleTileRarity RollRarity(BattleTilePackDefinition definition)
        {
            if (definition.Weights == null || definition.Weights.Length == 0)
                return definition.GuaranteedMinimum;

            int total = 0;
            for (int i = 0; i < definition.Weights.Length; i++)
                total += Mathf.Max(0, definition.Weights[i].weight);

            if (total <= 0)
                return definition.GuaranteedMinimum;

            int roll = UnityEngine.Random.Range(0, total);
            int cursor = 0;
            for (int i = 0; i < definition.Weights.Length; i++)
            {
                cursor += Mathf.Max(0, definition.Weights[i].weight);
                if (roll < cursor)
                    return definition.Weights[i].rarity;
            }

            return definition.GuaranteedMinimum;
        }

        private static BattleTileData PickTile(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTileRarity rolledRarity)
        {
            if (rolledRarity == BattleTileRarity.Standard)
            {
                List<BattleTileData> standards = GetStandardTiles(store);
                return standards.Count > 0
                    ? standards[UnityEngine.Random.Range(0, standards.Count)]
                    : null;
            }

            List<BattleTileData> candidates = GetCandidateTiles(store, inventory, rolledRarity, onlyNew: true);
            if (candidates.Count > 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            candidates = GetCandidateTiles(store, inventory, rolledRarity, onlyNew: false);
            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : null;
        }

        private static string FormatPercent(float percent)
        {
            float rounded = Mathf.Round(percent);
            return Mathf.Approximately(percent, rounded)
                ? Mathf.RoundToInt(percent) + "%"
                : percent.ToString("0.#") + "%";
        }

        private static List<BattleTileData> GetStandardTiles(BattleTileStore store)
        {
            List<BattleTileData> result = new();
            IReadOnlyList<BattleTileData> tiles = store != null ? store.BattleTiles : null;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData tile = tiles[i];
                if (IsAutoSoldStandardTile(tile))
                    result.Add(tile);
            }

            return result;
        }

        private static bool CanResolveRandomizedPack(BattleTileStore store, BattleTilePackDefinition definition)
        {
            if (store == null || definition?.Weights == null || definition.Weights.Length == 0)
                return false;

            for (int i = 0; i < definition.Weights.Length; i++)
            {
                (BattleTileRarity rarity, int weight) = definition.Weights[i];
                if (weight <= 0)
                    continue;

                if (rarity == BattleTileRarity.Standard)
                {
                    if (GetStandardTiles(store).Count == 0)
                        return false;

                    continue;
                }

                if (GetCandidateTiles(store, null, rarity, onlyNew: false).Count == 0)
                    return false;
            }

            if (definition.GuaranteedMinimumCount > 0
                && GetCandidateTiles(store, null, definition.GuaranteedMinimum, onlyNew: false).Count == 0)
                return false;

            if (definition.PityPackLimit > 0
                && GetCandidateTiles(store, null, definition.PityRarity, onlyNew: false).Count == 0)
                return false;

            return true;
        }

        private static List<BattleTileData> GetCandidateTiles(BattleTileStore store, MahjongBattleTileInventoryData inventory, BattleTileRarity rarity, bool onlyNew)
        {
            List<BattleTileData> result = new();
            IReadOnlyList<BattleTileData> tiles = store.BattleTiles;
            if (tiles == null)
                return result;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData tile = tiles[i];
                if (!IsPackTile(tile) || tile.Rarity != rarity)
                    continue;

                if (onlyNew && OwnsTile(inventory, tile.Id))
                    continue;

                result.Add(tile);
            }

            return result;
        }

        private static bool IsPackTile(BattleTileData tile)
        {
            return tile != null
                   && tile.Prefab != null
                   && !string.IsNullOrWhiteSpace(tile.Id)
                   && !BattleTileInventoryService.IsBaseBattleTile(tile.Id);
        }

        private static bool IsAutoSoldStandardTile(BattleTileData tile)
        {
            return tile != null
                   && tile.Prefab != null
                   && !string.IsNullOrWhiteSpace(tile.Id)
                   && BattleTileInventoryService.IsBaseBattleTile(tile.Id);
        }

        private static int GetAutoSellOzTile(BattleTileData tile)
        {
            if (tile == null)
                return 0;

            return tile.Rarity switch
            {
                BattleTileRarity.Common => 2,
                _ => 1
            };
        }

        private static bool HasAnyPackTile(BattleTileStore store)
        {
            IReadOnlyList<BattleTileData> tiles = store != null ? store.BattleTiles : null;
            if (tiles == null)
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTileData tile = tiles[i];
                if (IsPackTile(tile) || IsAutoSoldStandardTile(tile))
                    return true;
            }

            return false;
        }

        private static bool HasStoredAtLeast(BattleTilePackResult result, BattleTileRarity rarity)
        {
            return CountStoredAtLeast(result, rarity) > 0;
        }

        private static int CountStoredAtLeast(BattleTilePackResult result, BattleTileRarity rarity)
        {
            if (result?.Rolls == null)
                return 0;

            int count = 0;
            for (int i = 0; i < result.Rolls.Count; i++)
            {
                BattleTilePackRoll roll = result.Rolls[i];
                if (roll != null && !roll.AutoSold && roll.Rarity >= rarity)
                    count++;
            }

            return count;
        }

        private static int FindAutoSoldReplaceableRollIndex(BattleTilePackResult result)
        {
            if (result?.Rolls == null || result.Rolls.Count == 0)
                return -1;

            for (int i = 0; i < result.Rolls.Count; i++)
            {
                BattleTilePackRoll roll = result.Rolls[i];
                if (roll != null && roll.AutoSold && !roll.Guaranteed && !roll.Pity)
                    return i;
            }

            return -1;
        }

        private static void RemoveAutoSoldValue(BattleTilePackResult result, BattleTilePackRoll roll)
        {
            if (result == null || roll == null || !roll.AutoSold || roll.AutoSoldOzTile <= 0)
                return;

            result.AutoSoldOzTile = Mathf.Max(0, result.AutoSoldOzTile - roll.AutoSoldOzTile);
        }

        private static string GetPityKey(BattleTilePackId packId, BattleTileRarity rarity)
        {
            if (packId == BattleTilePackId.DailyAd && rarity == BattleTileRarity.Rare)
                return DailyAdRarePityPrefix;

            return string.Empty;
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

        private static BattleTilePackResult Fail(string message)
        {
            return new BattleTilePackResult
            {
                Success = false,
                Message = message ?? string.Empty
            };
        }

        private static string GetProfileKey(PlayerProfile profile)
        {
            if (profile == null)
                return "default";

            profile.EnsureData();
            return string.IsNullOrWhiteSpace(profile.LocalProfileId) ? "default" : profile.LocalProfileId;
        }

        private static string GetTodayKey()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd");
        }
    }
}
