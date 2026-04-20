using System;

namespace MahjongGame
{
    [Serializable]
    public sealed class MahjongBattleOpponentData
    {
        public string Id;
        public string DisplayName;
        public int AvatarId;
        public string RankTier;
        public int RankPoints;
        public int Wins;
        public int Losses;
        public int TotalMatches;
        public bool IsBot;
        public float DifficultyFactor;

        public MahjongBattleOpponentData()
        {
            Id = string.Empty;
            DisplayName = "Opponent";
            AvatarId = 0;
            RankTier = "Unranked";
            RankPoints = 0;
            Wins = 0;
            Losses = 0;
            TotalMatches = 0;
            IsBot = true;
            DifficultyFactor = 1f;
        }
    }
}