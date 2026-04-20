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
        public int OzAmetist;

        public int Altin => OzAltin;
        public int Ametist => OzAmetist;

        public event Action<int> AltinChanged;
        public event Action<int> AmetistChanged;
        public event Action CurrencyChanged;

        public GlobalCurrencyData()
        {
            OzAltin = 0;
            OzAmetist = 0;
        }

        public void AddAltin(int amount)
        {
            if (amount <= 0) return;
            OzAltin += amount;
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
            OzAmetist += amount;
            NotifyAmetistChanged();
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
            if (OzAltin < 0) OzAltin = 0;
            if (OzAmetist < 0) OzAmetist = 0;
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
    public sealed class MahjongBattleData
    {
        [Header("Battle Stats")]
        public int Wins;
        public int Losses;
        public int TotalMatches;

        [Header("Battle Streak")]
        public int WinStreak;
        public int BestWinStreak;

        [Header("Battle Rank")]
        public string RankTier;
        public int RankPoints;

        [Header("Battle Economy")]
        public int LastStakeUsed;
        public int TotalBattleRewardEarned;

        public int TotalGames => TotalMatches;
        public bool HasMatches => TotalMatches > 0;

        public event Action StatsChanged;
        public event Action<int> RankPointsChanged;
        public event Action<string> RankTierChanged;

        public MahjongBattleData()
        {
            Wins = 0;
            Losses = 0;
            TotalMatches = 0;
            WinStreak = 0;
            BestWinStreak = 0;
            RankTier = "Unranked";
            RankPoints = 0;
            LastStakeUsed = 0;
            TotalBattleRewardEarned = 0;
        }

        public void AddWin()
        {
            Wins++;
            TotalMatches++;
            WinStreak++;
            if (WinStreak > BestWinStreak)
                BestWinStreak = WinStreak;

            StatsChanged?.Invoke();
        }

        public void AddLoss()
        {
            Losses++;
            TotalMatches++;
            WinStreak = 0;
            StatsChanged?.Invoke();
        }

        public void SetRank(string rankTier, int rankPoints)
        {
            string newTier = string.IsNullOrWhiteSpace(rankTier) ? "Unranked" : rankTier.Trim();
            int newPoints = Mathf.Max(0, rankPoints);

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
            TotalMatches = Mathf.Max(0, TotalMatches);
            WinStreak = Mathf.Max(0, WinStreak);
            BestWinStreak = Mathf.Max(0, BestWinStreak);
            RankTier = string.IsNullOrWhiteSpace(RankTier) ? "Unranked" : RankTier.Trim();
            RankPoints = Mathf.Max(0, RankPoints);
            LastStakeUsed = Mathf.Max(0, LastStakeUsed);
            TotalBattleRewardEarned = Mathf.Max(0, TotalBattleRewardEarned);
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
    public sealed class PlayerProfile
    {
        [Header("Identity")]
        public string LocalProfileId;
        public string PublicPlayerId;
        public string OnlinePlayerId;
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

        [Header("Global Progress")]
        public int AccountLevel;
        public int AccountExp;

        [Header("Currencies")]
        public GlobalCurrencyData Currencies;

        [Header("Mahjong")]
        public MahjongProfileData Mahjong;

        [Header("Weekly Reward")]
        public WeeklyRewardData WeeklyReward;

        [Header("Time")]
        public string CreatedAtUtc;
        public string LastLoginUtc;

        [Header("State")]
        public bool IsGuest;
        public bool IsProfileCompleted;
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
            DisplayName = string.Empty;
            Age = 0;
            Gender = PlayerGender.NotSpecified;
            FriendPublicIds = new List<string>();

            AvatarId = 0;
            FrameId = string.Empty;

            GlobalTitleId = string.Empty;
            GlobalRankTier = "Unranked";
            GlobalRankPoints = 0;

            AccountLevel = 1;
            AccountExp = 0;

            Currencies = new GlobalCurrencyData();
            Mahjong = new MahjongProfileData();
            WeeklyReward = new WeeklyRewardData();

            string now = DateTime.UtcNow.ToString("O");
            CreatedAtUtc = now;
            LastLoginUtc = now;

            IsGuest = true;
            IsProfileCompleted = false;
            DataVersion = 6;

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
            AccountExpChanged?.Invoke(AccountExp);
            ProfileChanged?.Invoke();
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
            bool ok = Currencies.SpendAmetist(amount);
            if (ok) ProfileChanged?.Invoke();
            return ok;
        }

        public void AddAmetist(int amount)
        {
            EnsureData();
            Currencies.AddAmetist(amount);
            if (amount > 0) ProfileChanged?.Invoke();
        }

        public bool CanSpendAltin(int amount)
        {
            EnsureData();
            return Currencies.CanSpendAltin(amount);
        }

        public bool CanSpendAmetist(int amount)
        {
            EnsureData();
            return Currencies.CanSpendAmetist(amount);
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

            if (FriendPublicIds == null)
                FriendPublicIds = new List<string>();

            if (Mahjong == null)
                Mahjong = new MahjongProfileData();

            if (WeeklyReward == null)
                WeeklyReward = new WeeklyRewardData();

            if (string.IsNullOrWhiteSpace(CreatedAtUtc))
                CreatedAtUtc = DateTime.UtcNow.ToString("O");

            if (string.IsNullOrWhiteSpace(LastLoginUtc))
                LastLoginUtc = DateTime.UtcNow.ToString("O");

            DisplayName = DisplayName ?? string.Empty;
            PublicPlayerId = PublicPlayerId ?? string.Empty;
            OnlinePlayerId = OnlinePlayerId ?? string.Empty;
            FrameId = FrameId ?? string.Empty;
            GlobalTitleId = GlobalTitleId ?? string.Empty;
            GlobalRankTier = string.IsNullOrWhiteSpace(GlobalRankTier) ? "Unranked" : GlobalRankTier.Trim();

            AccountLevel = Mathf.Max(1, AccountLevel);
            AccountExp = Mathf.Max(0, AccountExp);
            Age = Mathf.Clamp(Age, 0, 120);
            if (!Enum.IsDefined(typeof(PlayerGender), Gender))
                Gender = PlayerGender.NotSpecified;
            AvatarId = Mathf.Max(0, AvatarId);
            GlobalRankPoints = Mathf.Max(0, GlobalRankPoints);
            DataVersion = Mathf.Max(1, DataVersion);

            Currencies.EnsureValid();
            Mahjong.EnsureData();
            WeeklyReward.EnsureValid();
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
