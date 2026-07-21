using System;

namespace MahjongGame
{
    [Serializable]
    public sealed class AllianceSummary
    {
        public int id;
        public string name;
        public string tag;
        public string description;
        public string language;
        public string visibility;
        public string status;
        public string specialization;
        public string weeklyFocus;
        public string announcement;
        public int emblemId;
        public int leaderUserId;
        public int level;
        public int xp;
        public int currentLevelXp;
        public int nextLevelXp;
        public int lifetimePoints;
        public int weeklyPoints;
        public int weeklyChestTier;
        public string weeklyPeriodKey;
        public int baseMaxMembers;
        public int maxMembers;
        public int memberCount;
        public int recruitmentMinRankPoints;
        public bool recruitmentNewPlayersWelcome;
        public bool recruitmentCompetitive;
        public string viewerRole;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public sealed class AllianceMember
    {
        public int userId;
        public string nickname;
        public string publicPlayerId;
        public int avatarId;
        public string battleRankTier;
        public int battleRankPoints;
        public string role;
        public int contributionPoints;
        public int weeklyContributionPoints;
        public bool online;
        public string lastSeenAt;
        public string joinedAt;
    }

    [Serializable]
    public sealed class AllianceInvite
    {
        public int id;
        public int allianceId;
        public string allianceName;
        public string allianceTag;
        public int allianceLevel;
        public string status;
        public string createdAt;
    }

    [Serializable]
    public sealed class AllianceJoinRequest
    {
        public int id;
        public int allianceId;
        public int userId;
        public string nickname;
        public string publicPlayerId;
        public string status;
        public string createdAt;
    }

    [Serializable]
    public sealed class AllianceActivity
    {
        public int id;
        public int allianceId;
        public int actorUserId;
        public int targetUserId;
        public string actorNickname;
        public string actorPublicPlayerId;
        public string targetNickname;
        public string targetPublicPlayerId;
        public string type;
        public string gameKey;
        public int points;
        public string metadataJson;
        public string createdAt;
    }

    [Serializable]
    public sealed class AllianceContributionBreakdown
    {
        public string gameKey;
        public int weeklyPoints;
        public int xp;
    }

    [Serializable]
    public sealed class AllianceChampionInfo
    {
        public int id;
        public int allianceId;
        public int userId;
        public string nickname;
        public string publicPlayerId;
        public int avatarId;
        public string battleRankTier;
        public int battleRankPoints;
        public string periodKey;
        public int selectedByUserId;
        public string status;
        public string createdAt;
    }

    [Serializable]
    public sealed class AllianceTournamentRules
    {
        public int maxChampions;
        public int rewardToAlliancePercent;
        public int rewardToChampionPercent;
        public string semifinalRefundTarget;
        public bool selectionLocked;
        public int minAllianceLevel;
    }

    [Serializable]
    public sealed class AllianceTournamentState
    {
        public string periodKey;
        public int maxChampions;
        public int minAllianceLevel;
        public int allianceLevel;
        public bool eligible;
        public string lockReason;
        public AllianceChampionInfo champion;
        public int fundOzTile;
        public int fundOzGold;
        public AllianceTournamentRules rules;
    }

    [Serializable]
    public sealed class AllianceRules
    {
        public int weeklyChestMinContribution;
        public AllianceChestTier[] chestTiers;
    }

    [Serializable]
    public sealed class AllianceChestTier
    {
        public int tier;
        public int points;
    }

    [Serializable]
    public sealed class AllianceChestState
    {
        public string periodKey;
        public int tier;
        public bool ready;
        public bool claimed;
        public int claimedTier;
        public string claimedAt;
        public int minContribution;
        public int playerContribution;
    }

    [Serializable]
    public sealed class AllianceChatMessage
    {
        public int id;
        public int allianceId;
        public int userId;
        public string nickname;
        public string publicPlayerId;
        public bool isDeveloper;
        public string role;
        public string allianceTag;
        public int allianceLevel;
        public string text;
        public string createdAt;
    }

    [Serializable]
    public sealed class AllianceStateResponse
    {
        public bool success;
        public string error;
        public AllianceSummary alliance;
        public AllianceMember[] members;
        public AllianceInvite[] incomingInvites;
        public AllianceJoinRequest[] pendingRequests;
        public AllianceActivity[] activity;
        public AllianceContributionBreakdown[] contributionBreakdown;
        public AllianceRules rules;
        public AllianceChestState chest;
        public AllianceTournamentState tournament;
    }

    [Serializable]
    public sealed class AllianceSearchResponse
    {
        public bool success;
        public string error;
        public AllianceSummary[] alliances;
    }

    [Serializable]
    public sealed class AllianceChatResponse
    {
        public bool success;
        public string error;
        public AllianceChatMessage[] messages;
    }

    [Serializable]
    public sealed class AllianceChatSendResponse
    {
        public bool success;
        public string error;
        public AllianceChatMessage message;
    }

    [Serializable]
    public sealed class AllianceLeaderboardResponse
    {
        public bool success;
        public string error;
        public AllianceSummary[] alliances;
    }
}
