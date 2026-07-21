using System;

namespace MahjongGame
{
    [Serializable]
    public sealed class MahjongBattleOpponentData
    {
        public string Id;
        public string DisplayName;
        public string AllianceTag;
        public int AllianceLevel;
        public int AvatarId;
        public PlayerGender Gender;
        public string CharacterId;
        public string RankTier;
        public int RankPoints;
        public int Level;
        public int Wins;
        public int Losses;
        public int TotalMatches;
        public int MvpCount;
        public bool IsBot;
        public float DifficultyFactor;
        public string StatusLine;
        public BattleLoadoutSnapshot Loadout;

        public MahjongBattleOpponentData()
        {
            Id = string.Empty;
            DisplayName = "Opponent";
            AllianceTag = string.Empty;
            AllianceLevel = 0;
            AvatarId = 0;
            Gender = PlayerGender.NotSpecified;
            CharacterId = string.Empty;
            RankTier = "Unranked";
            RankPoints = 0;
            Level = 1;
            Wins = 0;
            Losses = 0;
            TotalMatches = 0;
            MvpCount = 0;
            IsBot = true;
            DifficultyFactor = 1f;
            StatusLine = string.Empty;
            Loadout = null;
        }

        public static PlayerGender ParseGender(string value)
        {
            if (string.Equals(value, "female", System.StringComparison.OrdinalIgnoreCase))
                return PlayerGender.Female;
            if (string.Equals(value, "male", System.StringComparison.OrdinalIgnoreCase))
                return PlayerGender.Male;
            if (string.Equals(value, "other", System.StringComparison.OrdinalIgnoreCase))
                return PlayerGender.Other;
            return PlayerGender.NotSpecified;
        }
    }
}
