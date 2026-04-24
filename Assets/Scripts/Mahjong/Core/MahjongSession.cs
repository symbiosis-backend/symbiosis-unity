namespace MahjongGame
{
    public enum MahjongLaunchMode
    {
        None = 0,
        Story = 1,
        Battle = 2,
        Endless = 3
    }

    public static class MahjongSession
    {
        public static MahjongLaunchMode LaunchMode { get; private set; } = MahjongLaunchMode.None;

        public static int StoryLevel { get; private set; } = 1;
        public static int StoryStage { get; private set; } = 1;

        public static string BattleOpponentId { get; private set; } = string.Empty;
        public static string BattleOpponentName { get; private set; } = string.Empty;
        public static int BattleOpponentAvatarId { get; private set; } = 0;
        public static string BattleOpponentRankTier { get; private set; } = "Unranked";
        public static int BattleOpponentRankPoints { get; private set; } = 0;
        public static int BattleOpponentLevel { get; private set; } = 1;
        public static int BattleOpponentWins { get; private set; } = 0;
        public static int BattleOpponentLosses { get; private set; } = 0;
        public static int BattleOpponentMvpCount { get; private set; } = 0;
        public static bool BattleOpponentIsBot { get; private set; } = true;
        public static int BattleStakePot { get; private set; } = 0;
        public static int BattleMatchSeed { get; private set; } = 0;

        public static void StartStory(int level, int stage = 1)
        {
            LaunchMode = MahjongLaunchMode.Story;

            StoryLevel = level < 1 ? 1 : level;
            StoryStage = stage < 1 ? 1 : stage;

            ClearBattleRuntime();
        }

        public static void StartBattle(MahjongBattleOpponentData opponent, int stakePot = 0, int matchSeed = 0)
        {
            LaunchMode = MahjongLaunchMode.Battle;

            StoryLevel = 1;
            StoryStage = 1;

            if (opponent == null)
            {
                BattleOpponentId = "bot_unknown";
                BattleOpponentName = "Opponent";
                BattleOpponentAvatarId = 0;
                BattleOpponentRankTier = "Unranked";
                BattleOpponentRankPoints = 0;
                BattleOpponentLevel = 1;
                BattleOpponentWins = 0;
                BattleOpponentLosses = 0;
                BattleOpponentMvpCount = 0;
                BattleOpponentIsBot = true;
            }
            else
            {
                BattleOpponentId = string.IsNullOrWhiteSpace(opponent.Id) ? "bot_unknown" : opponent.Id;
                BattleOpponentName = string.IsNullOrWhiteSpace(opponent.DisplayName) ? "Opponent" : opponent.DisplayName;
                BattleOpponentAvatarId = opponent.AvatarId < 0 ? 0 : opponent.AvatarId;
                BattleOpponentRankTier = string.IsNullOrWhiteSpace(opponent.RankTier) ? "Unranked" : opponent.RankTier;
                BattleOpponentRankPoints = opponent.RankPoints < 0 ? 0 : opponent.RankPoints;
                BattleOpponentLevel = opponent.Level < 1 ? 1 : opponent.Level;
                BattleOpponentWins = opponent.Wins < 0 ? 0 : opponent.Wins;
                BattleOpponentLosses = opponent.Losses < 0 ? 0 : opponent.Losses;
                BattleOpponentMvpCount = opponent.MvpCount < 0 ? 0 : opponent.MvpCount;
                BattleOpponentIsBot = opponent.IsBot;
            }

            BattleStakePot = stakePot < 0 ? 0 : stakePot;
            BattleMatchSeed = matchSeed <= 0 ? UnityEngine.Random.Range(100000, 999999) : matchSeed;
        }

        public static void StartEndless()
        {
            LaunchMode = MahjongLaunchMode.Endless;

            StoryLevel = 1;
            StoryStage = 1;

            ClearBattleRuntime();
        }

        public static void SetStage(int stage)
        {
            StoryStage = stage < 1 ? 1 : stage;
        }

        public static void Clear()
        {
            LaunchMode = MahjongLaunchMode.None;

            StoryLevel = 1;
            StoryStage = 1;

            ClearBattleRuntime();
        }

        private static void ClearBattleRuntime()
        {
            BattleOpponentId = string.Empty;
            BattleOpponentName = string.Empty;
            BattleOpponentAvatarId = 0;
            BattleOpponentRankTier = "Unranked";
            BattleOpponentRankPoints = 0;
            BattleOpponentLevel = 1;
            BattleOpponentWins = 0;
            BattleOpponentLosses = 0;
            BattleOpponentMvpCount = 0;
            BattleOpponentIsBot = true;
            BattleStakePot = 0;
            BattleMatchSeed = 0;
        }
    }
}
