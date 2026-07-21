using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class GlobalCurrencyData
    {
        [Header("Platform Currency")]
        public int OzAltin;
        public int OzTile;
        public int OzAmetist;
        public List<CurrencyWalletEntry> ExtraCurrencies;

        public int Altin => OzAltin;
        public int Tile => OzTile;
        public int Ametist => OzAmetist;

        public event Action<int> AltinChanged;
        public event Action<int> TileChanged;
        public event Action<int> AmetistChanged;
        public event Action CurrencyChanged;

        public GlobalCurrencyData()
        {
            OzAltin = 0;
            OzTile = 0;
            OzAmetist = 0;
            ExtraCurrencies = new List<CurrencyWalletEntry>();
        }

        public void AddAltin(int amount)
        {
            if (amount <= 0) return;
            OzAltin = (int)Math.Min(int.MaxValue, (long)OzAltin + amount);
            NotifyAltinChanged();
        }

        public bool SpendAltin(int amount)
        {
            if (amount < 0 || OzAltin < amount)
                return false;

            OzAltin -= amount;
            NotifyAltinChanged();
            return true;
        }

        public bool CanSpendAltin(int amount)
        {
            return amount >= 0 && OzAltin >= amount;
        }

        public int GetCurrency(string currencyId)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (id == CurrencyIds.OzAltin)
                return OzAltin;
            if (id == CurrencyIds.OzTile)
                return OzTile;
            if (id == CurrencyIds.OzAmetist)
                return OzAmetist;

            CurrencyWalletEntry entry = FindExtraCurrency(id);
            return entry != null ? entry.Amount : 0;
        }

        public bool CanSpendCurrency(string currencyId, int amount)
        {
            return amount >= 0 && GetCurrency(currencyId) >= amount;
        }

        public bool TryChangeCurrency(string currencyId, int delta)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (delta == 0)
                return true;

            int current = GetCurrency(id);
            if (delta < 0 && current < -delta)
                return false;

            SetCurrency(id, current + delta);
            return true;
        }

        public void SetCurrency(string currencyId, int value)
        {
            string id = CurrencyWalletEntry.NormalizeCurrencyId(currencyId);
            int clamped = Mathf.Max(0, value);

            if (id == CurrencyIds.OzAltin)
            {
                OzAltin = clamped;
                NotifyAltinChanged();
                return;
            }

            if (id == CurrencyIds.OzTile)
            {
                OzTile = clamped;
                NotifyTileChanged();
                return;
            }

            if (id == CurrencyIds.OzAmetist)
            {
                OzAmetist = clamped;
                NotifyAmetistChanged();
                return;
            }

            if (string.IsNullOrWhiteSpace(id))
                return;

            CurrencyWalletEntry entry = EnsureExtraCurrency(id);
            if (entry.Amount == clamped)
                return;

            entry.Amount = clamped;
            CurrencyChanged?.Invoke();
        }

        public bool TryChangeAltin(int delta)
        {
            if (delta == 0) return true;
            if (delta > 0)
            {
                AddAltin(delta);
                return true;
            }
            return SpendAltin(-delta);
        }

        public void SetAltin(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (OzAltin == clamped) return;
            OzAltin = clamped;
            NotifyAltinChanged();
        }

        public void AddAmetist(int amount)
        {
            if (amount <= 0) return;
            OzAmetist = (int)Math.Min(int.MaxValue, (long)OzAmetist + amount);
            NotifyAmetistChanged();
        }

        public void AddTile(int amount)
        {
            if (amount <= 0) return;
            OzTile = (int)Math.Min(int.MaxValue, (long)OzTile + amount);
            NotifyTileChanged();
        }

        public bool SpendTile(int amount)
        {
            if (amount < 0 || OzTile < amount)
                return false;

            OzTile -= amount;
            NotifyTileChanged();
            return true;
        }

        public bool CanSpendTile(int amount)
        {
            return amount >= 0 && OzTile >= amount;
        }

        public bool TryChangeTile(int delta)
        {
            if (delta == 0) return true;
            if (delta > 0)
            {
                AddTile(delta);
                return true;
            }
            return SpendTile(-delta);
        }

        public void SetTile(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (OzTile == clamped) return;
            OzTile = clamped;
            NotifyTileChanged();
        }

        public bool SpendAmetist(int amount)
        {
            if (amount < 0 || OzAmetist < amount)
                return false;

            OzAmetist -= amount;
            NotifyAmetistChanged();
            return true;
        }

        public bool CanSpendAmetist(int amount)
        {
            return amount >= 0 && OzAmetist >= amount;
        }

        public bool TryChangeAmetist(int delta)
        {
            if (delta == 0) return true;
            if (delta > 0)
            {
                AddAmetist(delta);
                return true;
            }
            return SpendAmetist(-delta);
        }

        public void SetAmetist(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (OzAmetist == clamped) return;
            OzAmetist = clamped;
            NotifyAmetistChanged();
        }

        public void EnsureValid()
        {
            if (ExtraCurrencies == null)
                ExtraCurrencies = new List<CurrencyWalletEntry>();

            if (OzAltin < 0) OzAltin = 0;
            if (OzTile < 0) OzTile = 0;
            if (OzAmetist < 0) OzAmetist = 0;

            for (int i = ExtraCurrencies.Count - 1; i >= 0; i--)
            {
                CurrencyWalletEntry entry = ExtraCurrencies[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.CurrencyId))
                {
                    ExtraCurrencies.RemoveAt(i);
                    continue;
                }

                entry.CurrencyId = CurrencyWalletEntry.NormalizeCurrencyId(entry.CurrencyId);
                entry.Amount = Mathf.Max(0, entry.Amount);
            }
        }

        private CurrencyWalletEntry FindExtraCurrency(string currencyId)
        {
            if (ExtraCurrencies == null)
                ExtraCurrencies = new List<CurrencyWalletEntry>();

            for (int i = 0; i < ExtraCurrencies.Count; i++)
            {
                CurrencyWalletEntry entry = ExtraCurrencies[i];
                if (entry != null && entry.CurrencyId == currencyId)
                    return entry;
            }

            return null;
        }

        private CurrencyWalletEntry EnsureExtraCurrency(string currencyId)
        {
            CurrencyWalletEntry existing = FindExtraCurrency(currencyId);
            if (existing != null)
                return existing;

            CurrencyWalletEntry created = new CurrencyWalletEntry(currencyId, 0);
            ExtraCurrencies.Add(created);
            return created;
        }

        private void NotifyAltinChanged()
        {
            AltinChanged?.Invoke(OzAltin);
            CurrencyChanged?.Invoke();
        }

        private void NotifyAmetistChanged()
        {
            AmetistChanged?.Invoke(OzAmetist);
            CurrencyChanged?.Invoke();
        }

        private void NotifyTileChanged()
        {
            TileChanged?.Invoke(OzTile);
            CurrencyChanged?.Invoke();
        }
    }

    [Serializable]
    public sealed class PlayerEnergyData
    {
        public const int DefaultMaxEnergy = 100;
        public const int DefaultRefillIntervalSeconds = 60;

        [Header("Energy")]
        public int CurrentEnergy;
        public int MaxEnergy;
        public int RefillIntervalSeconds;
        public long LastUpdatedUtcTicks;

        public event Action EnergyChanged;

        public PlayerEnergyData()
        {
            MaxEnergy = DefaultMaxEnergy;
            CurrentEnergy = DefaultMaxEnergy;
            RefillIntervalSeconds = DefaultRefillIntervalSeconds;
            LastUpdatedUtcTicks = DateTime.UtcNow.Ticks;
        }

        public bool Refill(long nowUtcTicks)
        {
            EnsureValid();

            if (nowUtcTicks <= LastUpdatedUtcTicks)
                return false;

            if (CurrentEnergy >= MaxEnergy)
                return false;

            long ticksPerEnergy = TimeSpan.FromSeconds(RefillIntervalSeconds).Ticks;
            if (ticksPerEnergy <= 0)
                return false;

            long elapsedTicks = nowUtcTicks - LastUpdatedUtcTicks;
            int restored = (int)(elapsedTicks / ticksPerEnergy);
            if (restored <= 0)
                return false;

            int previous = CurrentEnergy;
            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + restored);
            LastUpdatedUtcTicks = CurrentEnergy >= MaxEnergy
                ? nowUtcTicks
                : LastUpdatedUtcTicks + restored * ticksPerEnergy;

            if (CurrentEnergy == previous)
                return false;

            EnergyChanged?.Invoke();
            return true;
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && CurrentEnergy >= amount;
        }

        public bool Spend(int amount, long nowUtcTicks)
        {
            if (amount < 0)
                return false;

            Refill(nowUtcTicks);

            if (!CanSpend(amount))
                return false;

            bool wasFull = CurrentEnergy >= MaxEnergy;
            CurrentEnergy -= amount;
            if (wasFull && CurrentEnergy < MaxEnergy)
                LastUpdatedUtcTicks = nowUtcTicks;

            EnergyChanged?.Invoke();
            return true;
        }

        public int GetSecondsUntilNextEnergy(long nowUtcTicks)
        {
            EnsureValid();

            if (CurrentEnergy >= MaxEnergy)
                return 0;

            long ticksPerEnergy = TimeSpan.FromSeconds(RefillIntervalSeconds).Ticks;
            long nextTicks = LastUpdatedUtcTicks + ticksPerEnergy;
            long remainingTicks = Math.Max(0L, nextTicks - nowUtcTicks);
            return Mathf.CeilToInt((float)TimeSpan.FromTicks(remainingTicks).TotalSeconds);
        }

        public void EnsureValid()
        {
            MaxEnergy = Mathf.Max(1, MaxEnergy <= 0 ? DefaultMaxEnergy : MaxEnergy);
            if (RefillIntervalSeconds <= 0 || RefillIntervalSeconds > DefaultRefillIntervalSeconds)
                RefillIntervalSeconds = DefaultRefillIntervalSeconds;
            CurrentEnergy = Mathf.Max(0, CurrentEnergy);

            if (LastUpdatedUtcTicks <= 0)
                LastUpdatedUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public sealed class MahjongStoryData
    {
        [Header("Story Progress")]
        public int CurrentLevel;
        public int CurrentStage;
        public int HighestUnlockedLevel;
        public int HighestUnlockedStage;
        public int LevelsCompleted;
        public int StagesCompleted;

        [Header("Story Score")]
        public int BestScore;
        public int TotalScore;

        public int Level => CurrentLevel;
        public int Stage => CurrentStage;
        public int UnlockedLevel => HighestUnlockedLevel;
        public int UnlockedStage => HighestUnlockedStage;
        public bool HasProgress => LevelsCompleted > 0 || StagesCompleted > 0 || TotalScore > 0;

        public event Action ProgressChanged;
        public event Action<int> BestScoreChanged;
        public event Action<int> TotalScoreChanged;

        public MahjongStoryData()
        {
            CurrentLevel = 1;
            CurrentStage = 1;
            HighestUnlockedLevel = 1;
            HighestUnlockedStage = 1;
            LevelsCompleted = 0;
            StagesCompleted = 0;
            BestScore = 0;
            TotalScore = 0;
        }

        public void SetCurrentProgress(int level, int stage)
        {
            int newLevel = Mathf.Max(1, level);
            int newStage = Mathf.Max(1, stage);

            bool changed = CurrentLevel != newLevel || CurrentStage != newStage;
            if (!changed) return;

            CurrentLevel = newLevel;
            CurrentStage = newStage;
            ProgressChanged?.Invoke();
        }

        public void UnlockProgress(int level, int stage)
        {
            int safeLevel = Mathf.Max(1, level);
            int safeStage = Mathf.Max(1, stage);

            bool changed = false;

            if (safeLevel > HighestUnlockedLevel)
            {
                HighestUnlockedLevel = safeLevel;
                HighestUnlockedStage = safeStage;
                changed = true;
            }
            else if (safeLevel == HighestUnlockedLevel && safeStage > HighestUnlockedStage)
            {
                HighestUnlockedStage = safeStage;
                changed = true;
            }

            if (changed)
                ProgressChanged?.Invoke();
        }

        public void AddCompletedStage(int score)
        {
            StagesCompleted++;
            if (score > 0)
            {
                TotalScore += score;
                TotalScoreChanged?.Invoke(TotalScore);
                if (score > BestScore)
                {
                    BestScore = score;
                    BestScoreChanged?.Invoke(BestScore);
                }
            }
            ProgressChanged?.Invoke();
        }

        public void AddCompletedLevel()
        {
            LevelsCompleted++;
            ProgressChanged?.Invoke();
        }

        public void SetBestScore(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (BestScore == clamped) return;
            BestScore = clamped;
            BestScoreChanged?.Invoke(BestScore);
        }

        public void AddTotalScore(int amount)
        {
            if (amount <= 0) return;
            TotalScore += amount;
            TotalScoreChanged?.Invoke(TotalScore);
        }

        public void EnsureValid()
        {
            CurrentLevel = Mathf.Max(1, CurrentLevel);
            CurrentStage = Mathf.Max(1, CurrentStage);
            HighestUnlockedLevel = Mathf.Max(1, HighestUnlockedLevel);
            HighestUnlockedStage = Mathf.Max(1, HighestUnlockedStage);
            LevelsCompleted = Mathf.Max(0, LevelsCompleted);
            StagesCompleted = Mathf.Max(0, StagesCompleted);
            BestScore = Mathf.Max(0, BestScore);
            TotalScore = Mathf.Max(0, TotalScore);
        }
    }

    [Serializable]
    public sealed class MahjongBattleTileStackData
    {
        public string TileId;
        public int Count;
        public int UpgradeLevel;

        public MahjongBattleTileStackData()
        {
            TileId = string.Empty;
            Count = 0;
            UpgradeLevel = 0;
        }

        public MahjongBattleTileStackData(string tileId, int count, int upgradeLevel = 0)
        {
            TileId = string.IsNullOrWhiteSpace(tileId) ? string.Empty : tileId.Trim();
            Count = Mathf.Max(0, count);
            UpgradeLevel = Mathf.Max(0, upgradeLevel);
        }

        public void EnsureValid()
        {
            TileId = string.IsNullOrWhiteSpace(TileId) ? string.Empty : TileId.Trim();
            Count = Mathf.Max(0, Count);
            UpgradeLevel = Mathf.Max(0, UpgradeLevel);
        }
    }

    [Serializable]
    public sealed class MahjongBattleTileInventoryData
    {
        public int SchemaVersion;
        public string TotemTileId;
        public List<string> ActiveTileIds;
        public List<string> ReserveTileIds;
        public List<MahjongBattleTileStackData> TileStacks;
		public int AscendLegendaryPityCount;
		public int AscendMythicPityCount;

        public MahjongBattleTileInventoryData()
        {
            SchemaVersion = 0;
            TotemTileId = string.Empty;
            ActiveTileIds = new List<string>();
            ReserveTileIds = new List<string>();
            TileStacks = new List<MahjongBattleTileStackData>();
			AscendLegendaryPityCount = 0;
			AscendMythicPityCount = 0;
        }

        public void EnsureValid()
        {
            SchemaVersion = Mathf.Max(0, SchemaVersion);

            if (ActiveTileIds == null)
                ActiveTileIds = new List<string>();

            if (ReserveTileIds == null)
                ReserveTileIds = new List<string>();

            if (TileStacks == null)
                TileStacks = new List<MahjongBattleTileStackData>();

			AscendLegendaryPityCount = Mathf.Max(0, AscendLegendaryPityCount);
			AscendMythicPityCount = Mathf.Max(0, AscendMythicPityCount);

            SanitizeList(ActiveTileIds);
            SanitizeList(ReserveTileIds);
            SanitizeStacks(TileStacks);
            TotemTileId = string.IsNullOrWhiteSpace(TotemTileId) ? string.Empty : TotemTileId.Trim();

            for (int i = ReserveTileIds.Count - 1; i >= 0; i--)
            {
                if (ActiveTileIds.Contains(ReserveTileIds[i]))
                    ReserveTileIds.RemoveAt(i);
            }

            // The totem is a role assigned to one of the 18 active tile types.
            // It is intentionally allowed to overlap ActiveTileIds.
        }

        private static void SanitizeList(List<string> ids)
        {
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    ids.RemoveAt(i);
                    continue;
                }

                string trimmed = id.Trim();
                int firstIndex = ids.FindIndex(value => string.Equals(value != null ? value.Trim() : string.Empty, trimmed, StringComparison.Ordinal));
                if (firstIndex >= 0 && firstIndex != i)
                {
                    ids.RemoveAt(i);
                    continue;
                }

                ids[i] = trimmed;
            }
        }

        private static void SanitizeStacks(List<MahjongBattleTileStackData> stacks)
        {
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                MahjongBattleTileStackData stack = stacks[i];
                if (stack == null)
                {
                    stacks.RemoveAt(i);
                    continue;
                }

                stack.EnsureValid();
                if (string.IsNullOrWhiteSpace(stack.TileId) || stack.Count <= 0)
                {
                    stacks.RemoveAt(i);
                    continue;
                }

                int firstIndex = stacks.FindIndex(value =>
                    value != null &&
                    value.UpgradeLevel == stack.UpgradeLevel &&
                    string.Equals(value.TileId != null ? value.TileId.Trim() : string.Empty, stack.TileId, StringComparison.Ordinal));
                if (firstIndex >= 0 && firstIndex != i)
                {
                    MahjongBattleTileStackData first = stacks[firstIndex];
                    long combinedCount = (long)Mathf.Max(0, first.Count) + Mathf.Max(0, stack.Count);
                    first.Count = combinedCount >= int.MaxValue ? int.MaxValue : (int)combinedCount;
                    stacks.RemoveAt(i);
                }
            }
        }
    }

    [Serializable]
    public sealed class MahjongBattleCharacterProgressData
    {
        public string CharacterId;
        public int Level;
        public int Experience;
        public int MaxHpUpgrades;
        public int AttackUpgrades;
        public int ArmorUpgrades;
        public int ParryUpgrades;
        public int CritUpgrades;
        public int CritDamageUpgrades;

        public MahjongBattleCharacterProgressData()
        {
            CharacterId = string.Empty;
            Level = 1;
            Experience = 0;
        }

        public void EnsureValid()
        {
            CharacterId = string.IsNullOrWhiteSpace(CharacterId) ? string.Empty : CharacterId.Trim();
            Level = Mathf.Max(1, Level);
            Experience = Mathf.Max(0, Experience);
            MaxHpUpgrades = Mathf.Max(0, MaxHpUpgrades);
            AttackUpgrades = Mathf.Max(0, AttackUpgrades);
            ArmorUpgrades = Mathf.Max(0, ArmorUpgrades);
            ParryUpgrades = Mathf.Max(0, ParryUpgrades);
            CritUpgrades = Mathf.Max(0, CritUpgrades);
            CritDamageUpgrades = Mathf.Max(0, CritDamageUpgrades);
        }
    }

    [Serializable]
    public sealed class MahjongBattleData
    {
        [Header("Battle Stats")]
        public int Wins;
        public int Losses;
        public int TotalMatches;
        public int MvpCount;

        [Header("Battle Level")]
        public int Level;
        public int Experience;

        [Header("Battle Streak")]
        public int WinStreak;
        public int BestWinStreak;

        [Header("Battle Rank")]
        public string RankTier;
        public int RankPoints;

        [Header("Battle Economy")]
        public int LastStakeUsed;
        public int TotalBattleRewardEarned;

        [Header("Battle Tile Inventory")]
        public MahjongBattleTileInventoryData TileInventory;

        [Header("Character Progression")]
        public List<MahjongBattleCharacterProgressData> CharacterProgression;

        public int TotalGames => TotalMatches;
        public bool HasMatches => TotalMatches > 0;
        public int WinRatePercent => TotalMatches > 0 ? Mathf.RoundToInt((float)Wins / TotalMatches * 100f) : 0;
        public int MvpRatePercent => TotalMatches > 0 ? Mathf.RoundToInt((float)MvpCount / TotalMatches * 100f) : 0;
        public int ExpToNextLevel => Mathf.Max(0, GetExperienceRequiredForNextLevel() - Experience);

        public event Action StatsChanged;
        public event Action<int> RankPointsChanged;
        public event Action<string> RankTierChanged;

        public MahjongBattleData()
        {
            Wins = 0;
            Losses = 0;
            TotalMatches = 0;
            MvpCount = 0;
            Level = 1;
            Experience = 0;
            WinStreak = 0;
            BestWinStreak = 0;
            RankTier = "Bronze";
            RankPoints = 0;
            LastStakeUsed = 0;
            TotalBattleRewardEarned = 0;
            TileInventory = new MahjongBattleTileInventoryData();
            CharacterProgression = new List<MahjongBattleCharacterProgressData>();
        }

        public void AddWin(bool mvp = true)
        {
            Wins++;
            TotalMatches++;
            if (mvp)
                MvpCount++;

            WinStreak++;
            if (WinStreak > BestWinStreak)
                BestWinStreak = WinStreak;

            StatsChanged?.Invoke();
        }

        public void AddLoss(bool mvp = false)
        {
            Losses++;
            TotalMatches++;
            if (mvp)
                MvpCount++;

            WinStreak = 0;
            StatsChanged?.Invoke();
        }

        public void AddMvp()
        {
            MvpCount++;
            StatsChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            Level = Mathf.Max(1, Level);
            Experience = Mathf.Max(0, Experience) + amount;

            int expToNextLevel = GetExperienceRequiredForNextLevel();
            while (Experience >= expToNextLevel)
            {
                Experience -= expToNextLevel;
                Level++;
                expToNextLevel = GetExperienceRequiredForNextLevel();
            }

            StatsChanged?.Invoke();
        }

        public int GetExperienceRequiredForNextLevel()
        {
            return 100 + Mathf.Max(0, Level - 1) * 50;
        }

        public void SetRank(string rankTier, int rankPoints)
        {
            int newPoints = Mathf.Max(0, rankPoints);
            string newTier = string.IsNullOrWhiteSpace(rankTier) || rankTier.Trim().Equals("Unranked", StringComparison.OrdinalIgnoreCase)
                ? RankedBattleService.GetCurrentTier(newPoints)
                : rankTier.Trim();

            bool tierChanged = RankTier != newTier;
            bool pointsChanged = RankPoints != newPoints;

            RankTier = newTier;
            RankPoints = newPoints;

            if (tierChanged) RankTierChanged?.Invoke(RankTier);
            if (pointsChanged) RankPointsChanged?.Invoke(RankPoints);
            if (tierChanged || pointsChanged) StatsChanged?.Invoke();
        }

        public void AddRankPoints(int amount)
        {
            if (amount == 0) return;
            RankPoints = Mathf.Max(0, RankPoints + amount);
            RankPointsChanged?.Invoke(RankPoints);
            StatsChanged?.Invoke();
        }

        public void SetLastStakeUsed(int stake)
        {
            int clamped = Mathf.Max(0, stake);
            if (LastStakeUsed == clamped) return;
            LastStakeUsed = clamped;
            StatsChanged?.Invoke();
        }

        public void AddBattleReward(int amount)
        {
            if (amount <= 0) return;
            TotalBattleRewardEarned += amount;
            StatsChanged?.Invoke();
        }

        public void EnsureValid()
        {
            Wins = Mathf.Max(0, Wins);
            Losses = Mathf.Max(0, Losses);
            TotalMatches = Mathf.Max(Mathf.Max(0, TotalMatches), Wins + Losses);
            MvpCount = Mathf.Clamp(MvpCount, 0, TotalMatches);
            Level = Mathf.Max(1, Level);
            Experience = Mathf.Max(0, Experience);
            WinStreak = Mathf.Max(0, WinStreak);
            BestWinStreak = Mathf.Max(0, BestWinStreak);
            RankPoints = Mathf.Max(0, RankPoints);
            RankTier = string.IsNullOrWhiteSpace(RankTier) || RankTier.Trim().Equals("Unranked", StringComparison.OrdinalIgnoreCase)
                ? RankedBattleService.GetCurrentTier(RankPoints)
                : RankTier.Trim();
            LastStakeUsed = Mathf.Max(0, LastStakeUsed);
            TotalBattleRewardEarned = Mathf.Max(0, TotalBattleRewardEarned);
            if (TileInventory == null)
                TileInventory = new MahjongBattleTileInventoryData();
            TileInventory.EnsureValid();
            if (CharacterProgression == null)
                CharacterProgression = new List<MahjongBattleCharacterProgressData>();
            for (int i = CharacterProgression.Count - 1; i >= 0; i--)
            {
                MahjongBattleCharacterProgressData progress = CharacterProgression[i];
                if (progress == null || string.IsNullOrWhiteSpace(progress.CharacterId))
                {
                    CharacterProgression.RemoveAt(i);
                    continue;
                }

                progress.EnsureValid();
                int firstIndex = CharacterProgression.FindIndex(item =>
                    item != null &&
                    string.Equals(item.CharacterId, progress.CharacterId, StringComparison.Ordinal));
                if (firstIndex >= 0 && firstIndex != i)
                    CharacterProgression.RemoveAt(i);
            }
        }
    }

    [Serializable]
    public sealed class MahjongEndlessData
    {
        [Header("Endless Progress")]
        public int BestReachedLevel;
        public int TotalRuns;

        [Header("Endless Score")]
        public int BestScore;
        public int TotalScore;

        [Header("Endless Records")]
        public int LongestCombo;
        public int HighestRewardCollected;

        public bool HasRuns => TotalRuns > 0;

        public event Action ProgressChanged;
        public event Action<int> BestScoreChanged;
        public event Action<int> TotalScoreChanged;

        public MahjongEndlessData()
        {
            BestReachedLevel = 0;
            TotalRuns = 0;
            BestScore = 0;
            TotalScore = 0;
            LongestCombo = 0;
            HighestRewardCollected = 0;
        }

        public void RegisterRun(int reachedLevel, int score, int combo, int reward)
        {
            TotalRuns++;

            if (reachedLevel > BestReachedLevel)
                BestReachedLevel = reachedLevel;

            if (score > BestScore)
            {
                BestScore = score;
                BestScoreChanged?.Invoke(BestScore);
            }

            if (combo > LongestCombo)
                LongestCombo = combo;

            if (reward > HighestRewardCollected)
                HighestRewardCollected = reward;

            if (score > 0)
            {
                TotalScore += score;
                TotalScoreChanged?.Invoke(TotalScore);
            }

            ProgressChanged?.Invoke();
        }

        public void EnsureValid()
        {
            BestReachedLevel = Mathf.Max(0, BestReachedLevel);
            TotalRuns = Mathf.Max(0, TotalRuns);
            BestScore = Mathf.Max(0, BestScore);
            TotalScore = Mathf.Max(0, TotalScore);
            LongestCombo = Mathf.Max(0, LongestCombo);
            HighestRewardCollected = Mathf.Max(0, HighestRewardCollected);
        }
    }

    [Serializable]
    public sealed class MahjongProfileData
    {
        [Header("Shared Title")]
        public string SelectedTitleId;

        [Header("Shared Unlocks")]
        public List<string> UnlockedTitleIds;

        [Header("Shared Stats")]
        public int TotalMatchesPlayed;
        public int TotalWins;
        public int TotalLosses;
        public int TotalScoreAllModes;

        [Header("Modes")]
        public MahjongStoryData Story;
        public MahjongBattleData Battle;
        public MahjongEndlessData Endless;

        public string CurrentTitleId => SelectedTitleId;
        public IReadOnlyList<string> Titles => UnlockedTitleIds;
        public bool HasAnyUnlockedTitle => UnlockedTitleIds != null && UnlockedTitleIds.Count > 0;

        public event Action<string> SelectedTitleChanged;
        public event Action<string> TitleUnlocked;
        public event Action StatsChanged;

        public MahjongProfileData()
        {
            SelectedTitleId = string.Empty;
            UnlockedTitleIds = new List<string>();
            TotalMatchesPlayed = 0;
            TotalWins = 0;
            TotalLosses = 0;
            TotalScoreAllModes = 0;

            Story = new MahjongStoryData();
            Battle = new MahjongBattleData();
            Endless = new MahjongEndlessData();
        }

        public bool HasTitle()
        {
            return !string.IsNullOrWhiteSpace(SelectedTitleId);
        }

        public void UnlockTitle(string titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId))
                return;

            if (UnlockedTitleIds == null)
                UnlockedTitleIds = new List<string>();

            string safeId = titleId.Trim();
            if (UnlockedTitleIds.Contains(safeId))
                return;

            UnlockedTitleIds.Add(safeId);
            TitleUnlocked?.Invoke(safeId);
        }

        public bool HasUnlockedTitle(string titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId) || UnlockedTitleIds == null)
                return false;

            return UnlockedTitleIds.Contains(titleId.Trim());
        }

        public bool TrySelectTitle(string titleId)
        {
            string safeId = string.IsNullOrWhiteSpace(titleId) ? string.Empty : titleId.Trim();

            if (!string.IsNullOrEmpty(safeId) && !HasUnlockedTitle(safeId))
                return false;

            if (SelectedTitleId == safeId)
                return true;

            SelectedTitleId = safeId;
            SelectedTitleChanged?.Invoke(SelectedTitleId);
            return true;
        }

        public void SetSelectedTitle(string titleId)
        {
            SelectedTitleId = string.IsNullOrWhiteSpace(titleId) ? string.Empty : titleId.Trim();
            SelectedTitleChanged?.Invoke(SelectedTitleId);
        }

        public void AddMatchResult(bool win, int score)
        {
            TotalMatchesPlayed++;
            if (win) TotalWins++;
            else TotalLosses++;

            if (score > 0)
                TotalScoreAllModes += score;

            StatsChanged?.Invoke();
        }

        public void EnsureData()
        {
            if (UnlockedTitleIds == null)
                UnlockedTitleIds = new List<string>();

            if (Story == null)
                Story = new MahjongStoryData();

            if (Battle == null)
                Battle = new MahjongBattleData();

            if (Endless == null)
                Endless = new MahjongEndlessData();

            Story.EnsureValid();
            Battle.EnsureValid();
            Endless.EnsureValid();

            TotalMatchesPlayed = Mathf.Max(0, TotalMatchesPlayed);
            TotalWins = Mathf.Max(0, TotalWins);
            TotalLosses = Mathf.Max(0, TotalLosses);
            TotalScoreAllModes = Mathf.Max(0, TotalScoreAllModes);
            SelectedTitleId = SelectedTitleId ?? string.Empty;
        }
    }

    [Serializable]
    public enum PlayerGender
    {
        NotSpecified = 0,
        Male = 1,
        Female = 2,
        Other = 3
    }

    [Serializable]
    public sealed class AdRemovalData
    {
        public long NoAdsUntilUtcTicks;

        public bool HasActiveNoAds()
        {
            return NoAdsUntilUtcTicks > DateTime.UtcNow.Ticks;
        }

        public int GetRemainingDays()
        {
            long remainingTicks = Math.Max(0L, NoAdsUntilUtcTicks - DateTime.UtcNow.Ticks);
            if (remainingTicks <= 0)
                return 0;

            return Mathf.CeilToInt((float)TimeSpan.FromTicks(remainingTicks).TotalDays);
        }

        public void ExtendNoAds(TimeSpan duration)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long startTicks = Math.Max(nowTicks, NoAdsUntilUtcTicks);
            long durationTicks = Math.Max(0L, duration.Ticks);
            NoAdsUntilUtcTicks = startTicks + durationTicks;
        }

        public void EnsureValid()
        {
            if (NoAdsUntilUtcTicks < 0)
                NoAdsUntilUtcTicks = 0;
        }
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentDataVersion = 12;

        [Header("Identity")]
        public string LocalProfileId;
        public string PublicPlayerId;
        public string OnlinePlayerId;
        public string AccountEmail;
        public string DynastyName;
        public string DynastyId;
        public string AllianceTag;
        public string AllianceName;
        public int AllianceLevel;
        public int ProfileSlotIndex;
        public string DisplayName;
        public int Age;
        public PlayerGender Gender;

        [Header("Friends")]
        public List<string> FriendPublicIds;

        [Header("Visual")]
        public int AvatarId;
        public string FrameId;

        [Header("Global Profile")]
        public string GlobalTitleId;
        public string GlobalRankTier;
        public int GlobalRankPoints;
        public bool IsProfilePublic;

        [Header("Global Progress")]
        public int AccountLevel;
        public int AccountExp;

        [Header("Currencies")]
        public GlobalCurrencyData Currencies;

        [Header("Energy")]
        public PlayerEnergyData Energy;

        [Header("Mahjong")]
        public MahjongProfileData Mahjong;

        [Header("Ranked Battle")]
        public RankedBattlePersistentData RankedBattle;

        [Header("Weekly Reward")]
        public WeeklyRewardData WeeklyReward;

        [Header("Exchange Market")]
        public ExchangeMarketData ExchangeMarket;

        [Header("Ads")]
        public AdRemovalData Ads;

        [Header("Mailbox")]
        public MailboxData Mailbox;

        [Header("Time")]
        public string CreatedAtUtc;
        public string LastLoginUtc;

        [Header("State")]
        public bool IsGuest;
        public bool IsProfileCompleted;
        [NonSerialized] public bool IsDeveloper;
        [NonSerialized] public bool HasInfiniteCurrency;
        public int DataVersion;

        public string Id => LocalProfileId;
        public string FriendId => PublicPlayerId;
        public string Name => DisplayName;
        public bool HasProfile => IsProfileCompleted;
        public bool Guest => IsGuest;
        public int Level => AccountLevel;
        public int Exp => AccountExp;
        public bool HasMahjongData => Mahjong != null;
        public bool HasCurrencies => Currencies != null;
        public bool HasEnergy => Energy != null;
        public bool HasActiveNoAds => Ads != null && Ads.HasActiveNoAds();

        public event Action ProfileChanged;
        public event Action<string> DisplayNameChanged;
        public event Action<int> AvatarChanged;
        public event Action<int> AccountExpChanged;
        public event Action<int> AccountLevelChanged;
        public event Action<string> GlobalTitleChanged;
        public event Action LoginTimeUpdated;

        public PlayerProfile()
        {
            LocalProfileId = Guid.NewGuid().ToString("N");
            PublicPlayerId = GeneratePublicPlayerId();
            OnlinePlayerId = string.Empty;
            AccountEmail = string.Empty;
            DynastyName = string.Empty;
            DynastyId = string.Empty;
            AllianceTag = string.Empty;
            AllianceName = string.Empty;
            AllianceLevel = 0;
            ProfileSlotIndex = 1;
            DisplayName = string.Empty;
            Age = 0;
            Gender = PlayerGender.NotSpecified;
            FriendPublicIds = new List<string>();

            AvatarId = 0;
            FrameId = string.Empty;

            GlobalTitleId = string.Empty;
            GlobalRankTier = "Bronze";
            GlobalRankPoints = 0;
            IsProfilePublic = true;

            AccountLevel = 1;
            AccountExp = 0;

            Currencies = new GlobalCurrencyData();
            Energy = new PlayerEnergyData();
            Mahjong = new MahjongProfileData();
            RankedBattle = new RankedBattlePersistentData();
            WeeklyReward = new WeeklyRewardData();
            ExchangeMarket = new ExchangeMarketData();
            Ads = new AdRemovalData();
            Mailbox = new MailboxData();

            string now = DateTime.UtcNow.ToString("O");
            CreatedAtUtc = now;
            LastLoginUtc = now;

            IsGuest = true;
            IsProfileCompleted = false;
            IsDeveloper = false;
            HasInfiniteCurrency = false;
            DataVersion = CurrentDataVersion;

            HookNestedSignals();
        }

        public void TouchLoginTime()
        {
            LastLoginUtc = DateTime.UtcNow.ToString("O");
            LoginTimeUpdated?.Invoke();
            ProfileChanged?.Invoke();
        }

        public void CompleteProfile(string displayName, int avatarId)
        {
            CompleteProfile(displayName, avatarId, Age, Gender, PublicPlayerId);
        }

        public void CompleteProfile(string displayName, int avatarId, int age, PlayerGender gender, string publicPlayerId)
        {
            string safeName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();
            int safeAvatar = Mathf.Max(0, avatarId);
            int safeAge = Mathf.Clamp(age, 0, 120);
            PlayerGender safeGender = Enum.IsDefined(typeof(PlayerGender), gender)
                ? gender
                : PlayerGender.NotSpecified;
            string safePublicId = NormalizePublicPlayerId(publicPlayerId);
            if (string.IsNullOrWhiteSpace(safePublicId))
                safePublicId = GeneratePublicPlayerId();

            bool changed = false;

            if (PublicPlayerId != safePublicId)
            {
                PublicPlayerId = safePublicId;
                changed = true;
            }

            if (DisplayName != safeName)
            {
                DisplayName = safeName;
                DisplayNameChanged?.Invoke(DisplayName);
                changed = true;
            }

            if (AvatarId != safeAvatar)
            {
                AvatarId = safeAvatar;
                AvatarChanged?.Invoke(AvatarId);
                changed = true;
            }

            if (Age != safeAge)
            {
                Age = safeAge;
                changed = true;
            }

            if (Gender != safeGender)
            {
                Gender = safeGender;
                changed = true;
            }

            if (!IsProfileCompleted)
            {
                IsProfileCompleted = true;
                changed = true;
            }

            TouchLoginTime();

            if (changed)
                ProfileChanged?.Invoke();
        }

        public bool HasOnlineAccount()
        {
            return !string.IsNullOrWhiteSpace(OnlinePlayerId);
        }

        public bool HasDisplayName()
        {
            return !string.IsNullOrWhiteSpace(DisplayName);
        }

        public bool HasGlobalTitle()
        {
            return !string.IsNullOrWhiteSpace(GlobalTitleId);
        }

        public bool HasFriend(string publicPlayerId)
        {
            string normalized = NormalizePublicPlayerId(publicPlayerId);
            return !string.IsNullOrWhiteSpace(normalized) &&
                   FriendPublicIds != null &&
                   FriendPublicIds.Contains(normalized);
        }

        public bool TryAddFriend(string publicPlayerId)
        {
            string normalized = NormalizePublicPlayerId(publicPlayerId);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            EnsureData();

            if (normalized == PublicPlayerId || FriendPublicIds.Contains(normalized))
                return false;

            FriendPublicIds.Add(normalized);
            ProfileChanged?.Invoke();
            return true;
        }

        public bool RemoveFriend(string publicPlayerId)
        {
            string normalized = NormalizePublicPlayerId(publicPlayerId);
            if (string.IsNullOrWhiteSpace(normalized) || FriendPublicIds == null)
                return false;

            bool removed = FriendPublicIds.Remove(normalized);
            if (removed)
                ProfileChanged?.Invoke();

            return removed;
        }

        public void SetGlobalTitle(string titleId)
        {
            string safeId = string.IsNullOrWhiteSpace(titleId) ? string.Empty : titleId.Trim();
            if (GlobalTitleId == safeId) return;

            GlobalTitleId = safeId;
            GlobalTitleChanged?.Invoke(GlobalTitleId);
            ProfileChanged?.Invoke();
        }

        public void SetFrame(string frameId)
        {
            string safeId = string.IsNullOrWhiteSpace(frameId) ? string.Empty : frameId.Trim();
            if (FrameId == safeId) return;

            FrameId = safeId;
            ProfileChanged?.Invoke();
        }

        public void SetOnlinePlayerId(string onlineId)
        {
            string safeId = string.IsNullOrWhiteSpace(onlineId) ? string.Empty : onlineId.Trim();
            if (OnlinePlayerId == safeId) return;

            OnlinePlayerId = safeId;
            ProfileChanged?.Invoke();
        }

        public void SetGuestState(bool isGuest)
        {
            if (IsGuest == isGuest) return;
            IsGuest = isGuest;
            ProfileChanged?.Invoke();
        }

        public void AddAccountExp(int exp)
        {
            if (exp <= 0) return;
            AccountExp += exp;
            bool levelChanged = false;
            int expToNextLevel = GetAccountExpRequiredForNextLevel();
            while (AccountExp >= expToNextLevel)
            {
                AccountExp -= expToNextLevel;
                AccountLevel++;
                levelChanged = true;
                expToNextLevel = GetAccountExpRequiredForNextLevel();
            }

            if (levelChanged)
                AccountLevelChanged?.Invoke(AccountLevel);

            AccountExpChanged?.Invoke(AccountExp);
            ProfileChanged?.Invoke();
        }

        public int GetAccountExpRequiredForNextLevel()
        {
            return 100 + Mathf.Max(0, AccountLevel - 1) * 50;
        }

        public void SetAccountExp(int exp)
        {
            int clamped = Mathf.Max(0, exp);
            if (AccountExp == clamped) return;
            AccountExp = clamped;
            AccountExpChanged?.Invoke(AccountExp);
            ProfileChanged?.Invoke();
        }

        public void SetAccountLevel(int level)
        {
            int clamped = Mathf.Max(1, level);
            if (AccountLevel == clamped) return;
            AccountLevel = clamped;
            AccountLevelChanged?.Invoke(AccountLevel);
            ProfileChanged?.Invoke();
        }

        public void AddAccountLevel(int amount)
        {
            if (amount <= 0) return;
            AccountLevel += amount;
            if (AccountLevel < 1) AccountLevel = 1;
            AccountLevelChanged?.Invoke(AccountLevel);
            ProfileChanged?.Invoke();
        }

        public bool TrySpendAltin(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            bool ok = Currencies.SpendAltin(amount);
            if (ok) ProfileChanged?.Invoke();
            return ok;
        }

        public void AddAltin(int amount)
        {
            EnsureData();
            Currencies.AddAltin(amount);
            if (amount > 0) ProfileChanged?.Invoke();
        }

        public bool TrySpendAmetist(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            bool ok = Currencies.SpendAmetist(amount);
            if (ok) ProfileChanged?.Invoke();
            return ok;
        }

        public bool TrySpendTile(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            bool ok = Currencies.SpendTile(amount);
            if (ok) ProfileChanged?.Invoke();
            return ok;
        }

        public void AddAmetist(int amount)
        {
            EnsureData();
            Currencies.AddAmetist(amount);
            if (amount > 0) ProfileChanged?.Invoke();
        }

        public void AddTile(int amount)
        {
            EnsureData();
            Currencies.AddTile(amount);
            if (amount > 0) ProfileChanged?.Invoke();
        }

        public bool CanSpendAltin(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            return Currencies.CanSpendAltin(amount);
        }

        public bool CanSpendAmetist(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            return Currencies.CanSpendAmetist(amount);
        }

        public bool CanSpendTile(int amount)
        {
            EnsureData();
            if (HasInfiniteCurrency)
                return amount >= 0;

            return Currencies.CanSpendTile(amount);
        }

        public void EnsureData()
        {
            if (string.IsNullOrWhiteSpace(LocalProfileId))
                LocalProfileId = Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(PublicPlayerId))
                PublicPlayerId = GeneratePublicPlayerId();
            else
                PublicPlayerId = NormalizePublicPlayerId(PublicPlayerId);

            if (string.IsNullOrWhiteSpace(PublicPlayerId))
                PublicPlayerId = GeneratePublicPlayerId();

            if (Currencies == null)
                Currencies = new GlobalCurrencyData();

            if (Energy == null)
                Energy = new PlayerEnergyData();

            if (FriendPublicIds == null)
                FriendPublicIds = new List<string>();

            if (Mahjong == null)
                Mahjong = new MahjongProfileData();

            if (RankedBattle == null)
                RankedBattle = new RankedBattlePersistentData();

            if (WeeklyReward == null)
                WeeklyReward = new WeeklyRewardData();

            if (ExchangeMarket == null)
                ExchangeMarket = new ExchangeMarketData();

            if (Ads == null)
                Ads = new AdRemovalData();

            if (Mailbox == null)
                Mailbox = new MailboxData();

            if (string.IsNullOrWhiteSpace(CreatedAtUtc))
                CreatedAtUtc = DateTime.UtcNow.ToString("O");

            if (string.IsNullOrWhiteSpace(LastLoginUtc))
                LastLoginUtc = DateTime.UtcNow.ToString("O");

            DisplayName = DisplayName ?? string.Empty;
            PublicPlayerId = PublicPlayerId ?? string.Empty;
            OnlinePlayerId = OnlinePlayerId ?? string.Empty;
            AccountEmail = string.IsNullOrWhiteSpace(AccountEmail) ? string.Empty : AccountEmail.Trim().ToLowerInvariant();
            DynastyName = DynastyName ?? string.Empty;
            DynastyId = DynastyId ?? string.Empty;
            AllianceTag = AllianceTag ?? string.Empty;
            AllianceName = AllianceName ?? string.Empty;
            AllianceLevel = Mathf.Max(0, AllianceLevel);
            ProfileSlotIndex = Mathf.Clamp(ProfileSlotIndex <= 0 ? 1 : ProfileSlotIndex, 1, 3);
            FrameId = FrameId ?? string.Empty;
            GlobalTitleId = GlobalTitleId ?? string.Empty;
            AccountLevel = Mathf.Max(1, AccountLevel);
            AccountExp = Mathf.Max(0, AccountExp);
            Age = Mathf.Clamp(Age, 0, 120);
            if (!Enum.IsDefined(typeof(PlayerGender), Gender))
                Gender = PlayerGender.NotSpecified;
            AvatarId = Mathf.Max(0, AvatarId);
            GlobalRankPoints = Mathf.Max(0, GlobalRankPoints);
            GlobalRankTier = string.IsNullOrWhiteSpace(GlobalRankTier) || GlobalRankTier.Trim().Equals("Unranked", StringComparison.OrdinalIgnoreCase)
                ? RankedBattleService.GetCurrentTier(GlobalRankPoints)
                : GlobalRankTier.Trim();
            if (DataVersion < 11)
                IsProfilePublic = true;
            DataVersion = Mathf.Max(CurrentDataVersion, DataVersion);

            Currencies.EnsureValid();
            Energy.EnsureValid();
            Mahjong.EnsureData();
            RankedBattle.EnsureValid();
            WeeklyReward.EnsureValid();
            ExchangeMarket.EnsureData(ExchangeMarketService.Config);
            Ads.EnsureValid();
            Mailbox.EnsureValid();
            SanitizeFriendIds();

            HookNestedSignals();
        }

        private void SanitizeFriendIds()
        {
            if (FriendPublicIds == null)
            {
                FriendPublicIds = new List<string>();
                return;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = FriendPublicIds.Count - 1; i >= 0; i--)
            {
                string normalized = NormalizePublicPlayerId(FriendPublicIds[i]);
                if (string.IsNullOrWhiteSpace(normalized) ||
                    normalized == PublicPlayerId ||
                    seen.Contains(normalized))
                {
                    FriendPublicIds.RemoveAt(i);
                    continue;
                }

                FriendPublicIds[i] = normalized;
                seen.Add(normalized);
            }
        }

        public static string GeneratePublicPlayerId()
        {
            string raw = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            return "MB-" + raw;
        }

        public static string NormalizePublicPlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim().ToUpperInvariant();
            char[] buffer = new char[trimmed.Length];
            int count = 0;

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if ((c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '-' ||
                    c == '_')
                {
                    buffer[count] = c;
                    count++;
                }
            }

            if (count == 0)
                return string.Empty;

            string normalized = new string(buffer, 0, count);
            return normalized.Length > 18 ? normalized.Substring(0, 18) : normalized;
        }

        private void HookNestedSignals()
        {
            if (Currencies != null)
            {
                Currencies.CurrencyChanged -= OnNestedDataChanged;
                Currencies.CurrencyChanged += OnNestedDataChanged;
            }

            if (Energy != null)
            {
                Energy.EnergyChanged -= OnNestedDataChanged;
                Energy.EnergyChanged += OnNestedDataChanged;
            }

            if (Mahjong != null)
            {
                Mahjong.StatsChanged -= OnNestedDataChanged;
                Mahjong.StatsChanged += OnNestedDataChanged;

                Mahjong.SelectedTitleChanged -= OnMahjongTitleChanged;
                Mahjong.SelectedTitleChanged += OnMahjongTitleChanged;

                Mahjong.TitleUnlocked -= OnNestedTitleUnlocked;
                Mahjong.TitleUnlocked += OnNestedTitleUnlocked;
            }
        }

        private void OnNestedDataChanged()
        {
            ProfileChanged?.Invoke();
        }

        private void OnMahjongTitleChanged(string titleId)
        {
            ProfileChanged?.Invoke();
        }

        private void OnNestedTitleUnlocked(string titleId)
        {
            ProfileChanged?.Invoke();
        }
    }
}
