using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public enum RankedLeagueId
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Master = 4
    }

    [Serializable]
    public enum RankedLeaderboardScope
    {
        Global = 0,
        League = 1
    }

    [Serializable]
    public sealed class RankedLeagueConfig
    {
        public RankedLeagueId Id;
        public string DisplayName;
        public int EntryFeeOzTile;
        public int WinRewardOzTile;
        public int MinRankPoints;
        public int WinRpBonus;

        public int WinRankPoints => 25 + Mathf.Max(0, WinRpBonus);
        public int LossRankPoints => -10;

        public RankedLeagueConfig(
            RankedLeagueId id,
            string displayName,
            int entryFeeOzTile,
            int winRewardOzTile,
            int minRankPoints,
            int winRpBonus)
        {
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.ToString() : displayName.Trim();
            EntryFeeOzTile = Mathf.Max(0, entryFeeOzTile);
            WinRewardOzTile = Mathf.Max(0, winRewardOzTile);
            MinRankPoints = Mathf.Max(0, minRankPoints);
            WinRpBonus = Mathf.Max(0, winRpBonus);
        }
    }

    [Serializable]
    public sealed class RankedPendingMatch
    {
        public bool Active;
        public RankedLeagueId LeagueId;
        public int EntryFeeOzTile;
        public int WinRewardOzTile;
        public long StartedUtcTicks;
        public bool MatchStarted;

        public void Clear()
        {
            Active = false;
            LeagueId = RankedLeagueId.Bronze;
            EntryFeeOzTile = 0;
            WinRewardOzTile = 0;
            StartedUtcTicks = 0;
            MatchStarted = false;
        }

        public void EnsureValid()
        {
            EntryFeeOzTile = Mathf.Max(0, EntryFeeOzTile);
            WinRewardOzTile = Mathf.Max(0, WinRewardOzTile);
            if (!Active)
            {
                StartedUtcTicks = 0;
                MatchStarted = false;
            }
        }
    }

    [Serializable]
    public sealed class RankedMatchHistoryEntry
    {
        public RankedLeagueId LeagueId;
        public bool Won;
        public int OzTileDelta;
        public int RankPointDelta;
        public long EndedUtcTicks;
    }

    [Serializable]
    public sealed class RankedBattlePersistentData
    {
        public string SeasonId;
        public RankedPendingMatch PendingMatch;
        public List<RankedMatchHistoryEntry> MatchHistory;

        public RankedBattlePersistentData()
        {
            SeasonId = RankedBattleService.DefaultSeasonId;
            PendingMatch = new RankedPendingMatch();
            MatchHistory = new List<RankedMatchHistoryEntry>();
        }

        public void EnsureValid()
        {
            SeasonId = string.IsNullOrWhiteSpace(SeasonId)
                ? RankedBattleService.DefaultSeasonId
                : SeasonId.Trim();

            if (PendingMatch == null)
                PendingMatch = new RankedPendingMatch();

            if (MatchHistory == null)
                MatchHistory = new List<RankedMatchHistoryEntry>();

            PendingMatch.EnsureValid();

            for (int i = MatchHistory.Count - 1; i >= 0; i--)
            {
                if (MatchHistory[i] == null)
                    MatchHistory.RemoveAt(i);
            }

            const int maxHistory = 20;
            while (MatchHistory.Count > maxHistory)
                MatchHistory.RemoveAt(MatchHistory.Count - 1);
        }
    }

    public sealed class RankedBattleResult
    {
        public bool Applied;
        public bool Won;
        public RankedLeagueId LeagueId;
        public int EntryFeeOzTile;
        public int WinRewardOzTile;
        public int OzTileDelta;
        public int RankPointDelta;
        public string RankTier;
        public int RankPoints;
    }

    public sealed class LeaderboardEntry
    {
        public string DisplayName;
        public string RankTier;
        public int RankPoints;
        public int Wins;
        public int Losses;
        public bool IsPlayer;
    }
}
