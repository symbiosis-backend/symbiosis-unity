using System;

namespace MahjongGame.Tournaments
{
    [Serializable]
    public sealed class TournamentFeatureFlags
    {
        public bool enabled;
        public bool bronzeOnly;
        public bool testUsersOnly;
        public string mvpLeague;
        public int entryFeeOzTile;
        public int maxPlayers;
    }

    [Serializable]
    public sealed class TournamentInfo
    {
        public int id;
        public string type;
        public string league;
        public string status;
        public int entryFeeOzTile;
        public int maxPlayers;
        public int registeredCount;
        public int totalPoolOzTile;
        public int grandFundOzTile;
        public int rewardPoolOzTile;
        public int finalistPoolOzTile;
        public int firstRewardOzTile;
        public int secondRewardOzTile;
        public int semifinalRefundOzTile;
        public int currentRound;
        public string startsAt;
        public string registrationExpiresAt;
        public string completedAt;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public sealed class TournamentParticipantInfo
    {
        public int id;
        public int tournamentId;
        public int userId;
        public string nickname;
        public string publicPlayerId;
        public int avatarId;
        public string allianceTag;
        public int allianceLevel;
        public string leagueSnapshot;
        public int rankPointsSnapshot;
        public int bracketSlot;
        public string status;
        public int entryFeePaidOzTile;
        public int finalPlace;
        public string joinedAt;
        public string eliminatedAt;
    }

    [Serializable]
    public sealed class TournamentMatchInfo
    {
        public int id;
        public int tournamentId;
        public int roundIndex;
        public int matchIndex;
        public int playerAUserId;
        public int playerBUserId;
        public int winnerUserId;
        public string battleMatchId;
        public int battleSeed;
        public string status;
        public string startsAt;
        public string expiresAt;
        public string completedAt;
    }

    [Serializable]
    public sealed class TournamentRewardInfo
    {
        public int id;
        public int tournamentId;
        public int userId;
        public int place;
        public string currency;
        public int amount;
        public string claimedAt;
        public string createdAt;
    }

    [Serializable]
    public sealed class TournamentFundInfo
    {
        public string league;
        public string currency;
        public string allocation_bucket;
        public int amount;
    }

    [Serializable]
    public sealed class TournamentListResponse : TournamentBasicResponse
    {
        public TournamentFeatureFlags feature;
        public TournamentInfo[] tournaments;
    }

    [Serializable]
    public sealed class TournamentActiveResponse : TournamentBasicResponse
    {
        public TournamentFeatureFlags feature;
        public int userId;
        public TournamentInfo active;
        public string participantStatus;
        public TournamentInfo recentTournament;
        public string recentParticipantStatus;
        public int recentFinalPlace;
        public TournamentMatchInfo currentMatch;
        public string battleMatchId;
        public MahjongGame.Multiplayer.OnlineRankedBattleNetwork.RankedOpponentInfo opponent;
        public string startsAt;
        public string expiresAt;
        public string connectStatus;
        public TournamentRewardInfo[] pendingRewards;
        public int ozTileBalance;
    }

    [Serializable]
    public sealed class TournamentBracketResponse : TournamentBasicResponse
    {
        public TournamentInfo tournament;
        public TournamentParticipantInfo[] participants;
        public TournamentMatchInfo[] matches;
        public TournamentRewardInfo[] rewards;
    }

    [Serializable]
    public sealed class TournamentJoinResponse : TournamentBasicResponse
    {
        public bool alreadyJoined;
        public TournamentInfo tournament;
        public TournamentActiveResponse active;
    }

    [Serializable]
    public sealed class TournamentClaimResponse : TournamentBasicResponse
    {
        public bool claimed;
        public TournamentRewardInfo reward;
    }

    [Serializable]
    public sealed class TournamentFundsResponse : TournamentBasicResponse
    {
        public TournamentFundInfo[] funds;
    }

    [Serializable]
    public class TournamentBasicResponse
    {
        public bool success;
        public string error;
        public string lockReason;
    }
}
