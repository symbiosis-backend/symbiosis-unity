using System;
using System.Collections;
using System.Collections.Generic;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    public static class RankedLeaderboardService
    {
        private const string KeySessionToken = "symbiosis_server_session_token";

        public static IEnumerator FetchLeaderboard(
            RankedLeaderboardScope scope,
            RankedLeagueId leagueId,
            Action<List<LeaderboardEntry>, string> completed)
        {
            int minRankPoints = scope == RankedLeaderboardScope.League
                ? RankedBattleService.GetLeague(leagueId).MinRankPoints
                : 0;
            string token = PlayerPrefs.GetString(KeySessionToken, string.Empty);
            string path = "/battle/ranked/leaderboard?limit=20&minRankPoints=" + minRankPoints;
            if (!string.IsNullOrWhiteSpace(token))
                path += "&token=" + UnityWebRequest.EscapeURL(token);

            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = UnityWebRequest.Get(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], path));
                BackendEndpoints.ApplyClientVersionHeaders(request);
                yield return request.SendWebRequest();

                if (!BackendEndpoints.RequestFailed(request))
                {
                    RankedLeaderboardResponse response = JsonUtility.FromJson<RankedLeaderboardResponse>(request.downloadHandler.text);
                    List<LeaderboardEntry> entries = ConvertEntries(response);
                    AddLocalPlayerIfMissing(entries, scope, leagueId);
                    entries.Sort((a, b) => b.RankPoints.CompareTo(a.RankPoints));
                    completed?.Invoke(entries, string.Empty);
                    yield break;
                }

                if (!BackendEndpoints.CanRetryWithFallback(request) || i == BackendEndpoints.BaseUrls.Length - 1)
                {
                    List<LeaderboardEntry> fallback = GetLocalOnlyLeaderboard(scope, leagueId);
                    completed?.Invoke(fallback, request.error ?? "Leaderboard unavailable");
                    yield break;
                }
            }
        }

        public static List<LeaderboardEntry> GetLocalOnlyLeaderboard(RankedLeaderboardScope scope, RankedLeagueId leagueId)
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            AddLocalPlayerIfMissing(entries, scope, leagueId);
            return entries;
        }

        private static List<LeaderboardEntry> ConvertEntries(RankedLeaderboardResponse response)
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            if (response == null || response.entries == null)
                return entries;

            for (int i = 0; i < response.entries.Length; i++)
            {
                RankedLeaderboardEntryDto dto = response.entries[i];
                if (dto == null)
                    continue;

                entries.Add(new LeaderboardEntry
                {
                    DisplayName = string.IsNullOrWhiteSpace(dto.displayName) ? "Player" : dto.displayName.Trim(),
                    RankTier = string.IsNullOrWhiteSpace(dto.rankTier) ? RankedBattleService.GetCurrentTier(dto.rankPoints) : dto.rankTier.Trim(),
                    RankPoints = Mathf.Max(0, dto.rankPoints),
                    Wins = Mathf.Max(0, dto.wins),
                    Losses = Mathf.Max(0, dto.losses),
                    IsPlayer = dto.isPlayer
                });
            }

            return entries;
        }

        private static void AddLocalPlayerIfMissing(List<LeaderboardEntry> entries, RankedLeaderboardScope scope, RankedLeagueId leagueId)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
            {
                ProfileRuntimeBootstrap.TryLoadCachedProfile();
                profile = ProfileService.I != null ? ProfileService.I.Current : null;
            }

            if (profile == null)
                return;

            profile.EnsureData();
            MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
            int points = battle != null ? Mathf.Max(0, battle.RankPoints) : 0;
            if (scope == RankedLeaderboardScope.League && points < RankedBattleService.GetLeague(leagueId).MinRankPoints)
                return;

            string displayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "You" : profile.DisplayName.Trim();
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry entry = entries[i];
                if (entry != null && entry.IsPlayer)
                    return;
                if (entry != null && string.Equals(entry.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    entry.IsPlayer = true;
                    return;
                }
            }

            entries.Add(new LeaderboardEntry
            {
                DisplayName = displayName,
                RankTier = battle != null && !string.IsNullOrWhiteSpace(battle.RankTier)
                    ? battle.RankTier
                    : RankedBattleService.GetCurrentTier(points),
                RankPoints = points,
                Wins = battle != null ? Mathf.Max(0, battle.Wins) : 0,
                Losses = battle != null ? Mathf.Max(0, battle.Losses) : 0,
                IsPlayer = true
            });
        }

        [Serializable]
        private sealed class RankedLeaderboardResponse
        {
            public bool success;
            public RankedLeaderboardEntryDto[] entries;
        }

        [Serializable]
        private sealed class RankedLeaderboardEntryDto
        {
            public string displayName;
            public string rankTier;
            public int rankPoints;
            public int wins;
            public int losses;
            public bool isPlayer;
        }
    }
}
