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
        public int Level;
        public int Wins;
        public int Losses;
        public int TotalMatches;
        public int MvpCount;
        public bool IsBot;
        public float DifficultyFactor;

        public MahjongBattleOpponentData()
        {
            Id = string.Empty;
            DisplayName = "Opponent";
            AvatarId = 0;
            RankTier = "Unranked";
            RankPoints = 0;
            Level = 1;
            Wins = 0;
            Losses = 0;
            TotalMatches = 0;
            MvpCount = 0;
            IsBot = true;
            DifficultyFactor = 1f;
        }
    }
}
