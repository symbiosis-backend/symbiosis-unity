using System;
using UnityEngine;

namespace MahjongGame
{
    public static class RankedBattleService
    {
        public const string DefaultSeasonId = "season_001";

        private static readonly RankedLeagueConfig[] LeagueConfigs =
        {
            new RankedLeagueConfig(RankedLeagueId.Bronze, "Bronze", 10, 16, 0, 0),
            new RankedLeagueConfig(RankedLeagueId.Silver, "Silver", 18, 28, 100, 5),
            new RankedLeagueConfig(RankedLeagueId.Gold, "Gold", 30, 45, 250, 10),
            new RankedLeagueConfig(RankedLeagueId.Platinum, "Platinum", 45, 66, 500, 15),
            new RankedLeagueConfig(RankedLeagueId.Master, "Master", 65, 95, 900, 25)
        };

        public static RankedBattleResult LastAppliedResult { get; private set; }

        public static RankedLeagueConfig[] GetLeagues()
        {
            RankedLeagueConfig[] copy = new RankedLeagueConfig[LeagueConfigs.Length];
            Array.Copy(LeagueConfigs, copy, LeagueConfigs.Length);
            return copy;
        }

        public static RankedLeagueConfig GetLeague(RankedLeagueId id)
        {
            for (int i = 0; i < LeagueConfigs.Length; i++)
            {
                if (LeagueConfigs[i].Id == id)
                    return LeagueConfigs[i];
            }

            return LeagueConfigs[0];
        }

        public static bool HasPendingMatch()
        {
            RankedPendingMatch pending = GetPendingMatch();
            return pending != null && pending.Active;
        }

        public static RankedPendingMatch GetPendingMatch()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return null;

            profile.EnsureData();
            return profile.RankedBattle != null ? profile.RankedBattle.PendingMatch : null;
        }

        public static string GetCurrentTier(int rankPoints)
        {
            int points = Mathf.Max(0, rankPoints);
            if (points >= 900) return "Master";
            if (points >= 500) return "Platinum";
            if (points >= 250) return "Gold";
            if (points >= 100) return "Silver";
            return "Bronze";
        }

        public static int GetCurrentRankPoints()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Mahjong != null && profile.Mahjong.Battle != null
                ? Mathf.Max(0, profile.Mahjong.Battle.RankPoints)
                : 0;
        }

        public static bool CanEnterLeague(RankedLeagueConfig config, out string reason)
        {
            reason = string.Empty;

            if (config == null)
            {
                reason = "League unavailable";
                return false;
            }

            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                reason = "Profile unavailable";
                return false;
            }

            profile.EnsureData();
            int rankPoints = profile.Mahjong != null && profile.Mahjong.Battle != null
                ? Mathf.Max(0, profile.Mahjong.Battle.RankPoints)
                : 0;

            if (rankPoints < config.MinRankPoints)
            {
                reason = $"Need {GetCurrentTier(config.MinRankPoints)} ({config.MinRankPoints} RP)";
                return false;
            }

            int ozTile = profile.Currencies != null ? Mathf.Max(0, profile.Currencies.OzTile) : 0;
            if (ozTile < config.EntryFeeOzTile)
            {
                reason = $"Need {config.EntryFeeOzTile} OzTile";
                return false;
            }

            reason = "Ready";
            return true;
        }

        public static bool TryStartRankedMatch(RankedLeagueConfig config, out string reason)
        {
            if (!CanEnterLeague(config, out reason))
                return false;

            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                reason = "Profile unavailable";
                return false;
            }

            profile.EnsureData();

            RankedPendingMatch pending = profile.RankedBattle.PendingMatch;
            if (pending != null && pending.Active)
            {
                reason = "Ranked match already pending";
                return false;
            }

            if (CurrencyService.I != null)
            {
                if (!CurrencyService.I.SpendOzTile(config.EntryFeeOzTile))
                {
                    reason = $"Need {config.EntryFeeOzTile} OzTile";
                    return false;
                }
            }
            else if (!profile.TrySpendTile(config.EntryFeeOzTile))
            {
                reason = $"Need {config.EntryFeeOzTile} OzTile";
                return false;
            }

            profile.EnsureData();
            pending = profile.RankedBattle.PendingMatch;
            pending.Active = true;
            pending.LeagueId = config.Id;
            pending.EntryFeeOzTile = config.EntryFeeOzTile;
            pending.WinRewardOzTile = config.WinRewardOzTile;
            pending.StartedUtcTicks = DateTime.UtcNow.Ticks;
            pending.MatchStarted = false;

            SaveProfile();
            reason = "Ready";
            LastAppliedResult = null;
            return true;
        }

        public static bool TryStartDuelMatch(int stakeOzTile, out string reason)
        {
            int stake = Mathf.Max(1, stakeOzTile);
            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                reason = "Profile unavailable";
                return false;
            }

            profile.EnsureData();

            RankedPendingMatch pending = profile.RankedBattle.PendingMatch;
            if (pending != null && pending.Active)
            {
                reason = "Ranked match already pending";
                return false;
            }

            if (CurrencyService.I != null)
            {
                if (!CurrencyService.I.SpendOzTile(stake))
                {
                    reason = $"Need {stake} OzTile";
                    return false;
                }
            }
            else if (!profile.TrySpendTile(stake))
            {
                reason = $"Need {stake} OzTile";
                return false;
            }

            RankedLeagueId leagueId = ResolveLeagueId(GetCurrentRankPoints());
            profile.EnsureData();
            pending = profile.RankedBattle.PendingMatch;
            pending.Active = true;
            pending.LeagueId = leagueId;
            pending.EntryFeeOzTile = stake;
            pending.WinRewardOzTile = stake * 2;
            pending.StartedUtcTicks = DateTime.UtcNow.Ticks;
            pending.MatchStarted = true;

            SaveProfile();
            reason = "Ready";
            LastAppliedResult = null;
            return true;
        }

        public static RankedBattleResult ApplyRankedResult(bool playerWon, bool syncRankToServer = true)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return new RankedBattleResult();

            profile.EnsureData();
            RankedPendingMatch pending = profile.RankedBattle != null ? profile.RankedBattle.PendingMatch : null;
            if (pending == null || !pending.Active)
                return new RankedBattleResult();

            RankedLeagueConfig config = GetLeague(pending.LeagueId);
            int winReward = Mathf.Max(0, pending.WinRewardOzTile);
            int entryFee = Mathf.Max(0, pending.EntryFeeOzTile);
            int rankDelta = playerWon ? config.WinRankPoints : config.LossRankPoints;
            int ozTileDelta = playerWon ? winReward : -entryFee;

            if (playerWon)
            {
                if (CurrencyService.I != null)
                    CurrencyService.I.AddOzTile(winReward);
                else
                    profile.AddTile(winReward);
            }

            if (profile.Mahjong != null && profile.Mahjong.Battle != null)
            {
                MahjongBattleData battle = profile.Mahjong.Battle;
                if (playerWon)
                {
                    battle.AddWin(true);
                    profile.Mahjong.TotalWins++;
                }
                else
                {
                    battle.AddLoss(false);
                    profile.Mahjong.TotalLosses++;
                }

                int newRankPoints = Mathf.Max(0, battle.RankPoints + rankDelta);
                string newTier = GetCurrentTier(newRankPoints);
                battle.SetRank(newTier, newRankPoints);
                battle.LastStakeUsed = entryFee;
                if (playerWon)
                    battle.TotalBattleRewardEarned += winReward;

                profile.GlobalRankTier = newTier;
                profile.GlobalRankPoints = newRankPoints;
            }

            if (profile.Mahjong != null)
                profile.Mahjong.TotalMatchesPlayed++;

            RankedMatchHistoryEntry history = new RankedMatchHistoryEntry
            {
                LeagueId = pending.LeagueId,
                Won = playerWon,
                OzTileDelta = ozTileDelta,
                RankPointDelta = rankDelta,
                EndedUtcTicks = DateTime.UtcNow.Ticks
            };

            if (profile.RankedBattle.MatchHistory == null)
                profile.RankedBattle.MatchHistory = new System.Collections.Generic.List<RankedMatchHistoryEntry>();

            profile.RankedBattle.MatchHistory.Insert(0, history);
            while (profile.RankedBattle.MatchHistory.Count > 20)
                profile.RankedBattle.MatchHistory.RemoveAt(profile.RankedBattle.MatchHistory.Count - 1);

            RankedLeagueId leagueId = pending.LeagueId;
            pending.Clear();

            RankedBattleResult result = new RankedBattleResult
            {
                Applied = true,
                Won = playerWon,
                LeagueId = leagueId,
                EntryFeeOzTile = entryFee,
                WinRewardOzTile = winReward,
                OzTileDelta = ozTileDelta,
                RankPointDelta = rankDelta,
                RankTier = profile.Mahjong != null && profile.Mahjong.Battle != null ? profile.Mahjong.Battle.RankTier : "Bronze",
                RankPoints = profile.Mahjong != null && profile.Mahjong.Battle != null ? profile.Mahjong.Battle.RankPoints : 0
            };

            LastAppliedResult = result;
            SaveProfile();
            if (syncRankToServer)
                ProfileService.I?.SyncBattleRankToServer();
            return result;
        }

        public static void ClearPendingMatch()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.RankedBattle.PendingMatch.Clear();
            SaveProfile();
        }

        public static bool MarkPendingMatchStarted()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            RankedPendingMatch pending = profile.RankedBattle != null ? profile.RankedBattle.PendingMatch : null;
            if (pending == null || !pending.Active)
                return false;

            pending.MatchStarted = true;
            SaveProfile();
            return true;
        }

        public static bool CancelPendingMatch(bool refundEntryFee)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            RankedPendingMatch pending = profile.RankedBattle != null ? profile.RankedBattle.PendingMatch : null;
            if (pending == null || !pending.Active)
                return false;

            int entryFee = Mathf.Max(0, pending.EntryFeeOzTile);
            bool canRefund = refundEntryFee && !pending.MatchStarted;
            pending.Clear();
            LastAppliedResult = null;

            if (canRefund && entryFee > 0)
            {
                if (CurrencyService.I != null)
                    CurrencyService.I.AddOzTile(entryFee);
                else
                    profile.AddTile(entryFee);
            }

            SaveProfile();
            return true;
        }

        private static PlayerProfile GetProfile()
        {
            if (ProfileService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            if (ProfileService.I == null)
                return null;

            PlayerProfile profile = ProfileService.I.Current;
            if (profile == null)
            {
                ProfileRuntimeBootstrap.TryLoadCachedProfile();
                profile = ProfileService.I.Current;
            }

            return profile;
        }

        private static RankedLeagueId ResolveLeagueId(int rankPoints)
        {
            int points = Mathf.Max(0, rankPoints);
            if (points >= 900) return RankedLeagueId.Master;
            if (points >= 500) return RankedLeagueId.Platinum;
            if (points >= 250) return RankedLeagueId.Gold;
            if (points >= 100) return RankedLeagueId.Silver;
            return RankedLeagueId.Bronze;
        }

        private static void SaveProfile()
        {
            if (ProfileService.I == null)
                return;

            ProfileService.I.Save();
            ProfileService.I.NotifyProfileChanged();
        }
    }
}
