namespace MahjongGame
{
    public enum MahjongLaunchMode
    {
        None = 0,
        Story = 1,
        Battle = 2,
        Endless = 3
    }

    public enum MahjongBattleSource
    {
        Local = 0,
        Ranked = 1,
        Random = 2,
        Duel = 3,
        Tournament = 4
    }

    public enum MahjongStoryDifficulty
    {
        Unset = 0,
        Easy = 1,
        Medium = 2,
        Hardcore = 3
    }

    public static class MahjongSession
    {
        public static MahjongLaunchMode LaunchMode { get; private set; } = MahjongLaunchMode.None;

        public static int StoryLevel { get; private set; } = 1;
        public static int StoryStage { get; private set; } = 1;
        public static MahjongStoryDifficulty StoryDifficulty { get; private set; } = MahjongStoryDifficulty.Medium;
        public static int EndlessLevel { get; private set; } = 1;

        public static string BattleOpponentId { get; private set; } = string.Empty;
        public static string BattleOpponentName { get; private set; } = string.Empty;
        public static string BattleOpponentAllianceTag { get; private set; } = string.Empty;
        public static int BattleOpponentAllianceLevel { get; private set; } = 0;
        public static int BattleOpponentAvatarId { get; private set; } = 0;
        public static PlayerGender BattleOpponentGender { get; private set; } = PlayerGender.NotSpecified;
        public static string BattleOpponentCharacterId { get; private set; } = string.Empty;
        public static string BattleOpponentRankTier { get; private set; } = "Unranked";
        public static int BattleOpponentRankPoints { get; private set; } = 0;
        public static int BattleOpponentLevel { get; private set; } = 1;
        public static int BattleOpponentWins { get; private set; } = 0;
        public static int BattleOpponentLosses { get; private set; } = 0;
        public static int BattleOpponentMvpCount { get; private set; } = 0;
        public static float BattleOpponentDifficultyFactor { get; private set; } = 1f;
        public static string BattleOpponentStatusLine { get; private set; } = string.Empty;
        public static bool BattleOpponentIsBot { get; private set; } = true;
        public static int BattleStakePot { get; private set; } = 0;
        public static int BattleMatchSeed { get; private set; } = 0;
        public static BattleLoadoutSnapshot LocalBattleLoadout { get; private set; }
        public static BattleLoadoutSnapshot OpponentBattleLoadout { get; private set; }
        public static MahjongBattleSource BattleSource { get; private set; } = MahjongBattleSource.Local;
        public static int TournamentId { get; private set; } = 0;
        public static int TournamentMatchId { get; private set; } = 0;
        public static int TournamentRoundIndex { get; private set; } = 0;

        public static void StartStory(int level, int stage = 1, MahjongStoryDifficulty difficulty = MahjongStoryDifficulty.Unset)
        {
            LaunchMode = MahjongLaunchMode.Story;

            StoryLevel = level < 1 ? 1 : level;
            StoryStage = stage < 1 ? 1 : stage;
            StoryDifficulty = difficulty == MahjongStoryDifficulty.Unset
                ? ResolveExistingStoryDifficulty()
                : difficulty;
            EndlessLevel = 1;

            ClearBattleRuntime();
        }

        public static void StartBattle(
            MahjongBattleOpponentData opponent,
            int stakePot = 0,
            int matchSeed = 0,
            MahjongBattleSource battleSource = MahjongBattleSource.Local)
        {
            if (!BattleCharacterSelectionService.HasInstance ||
                !BattleCharacterSelectionService.Instance.HasSelectedCharacter())
            {
                LaunchMode = MahjongLaunchMode.None;
                ClearBattleRuntime();
                UnityEngine.Debug.LogWarning("[MahjongSession] Battle start rejected: this profile has no owned and selected battle character.");
                return;
            }

            LaunchMode = MahjongLaunchMode.Battle;

            StoryLevel = 1;
            StoryStage = 1;
            StoryDifficulty = MahjongStoryDifficulty.Medium;
            EndlessLevel = 1;

            if (opponent == null)
            {
                BattleOpponentId = "bot_unknown";
                BattleOpponentName = "Opponent";
                BattleOpponentAllianceTag = string.Empty;
                BattleOpponentAllianceLevel = 0;
                BattleOpponentAvatarId = 0;
                BattleOpponentGender = PlayerGender.NotSpecified;
                BattleOpponentCharacterId = string.Empty;
                BattleOpponentRankTier = "Unranked";
                BattleOpponentRankPoints = 0;
                BattleOpponentLevel = 1;
                BattleOpponentWins = 0;
                BattleOpponentLosses = 0;
                BattleOpponentMvpCount = 0;
                BattleOpponentDifficultyFactor = 1f;
                BattleOpponentStatusLine = string.Empty;
                BattleOpponentIsBot = true;
            }
            else
            {
                BattleOpponentId = string.IsNullOrWhiteSpace(opponent.Id) ? "bot_unknown" : opponent.Id;
                BattleOpponentName = string.IsNullOrWhiteSpace(opponent.DisplayName) ? "Opponent" : opponent.DisplayName;
                BattleOpponentAllianceTag = string.IsNullOrWhiteSpace(opponent.AllianceTag) ? string.Empty : opponent.AllianceTag.Trim();
                BattleOpponentAllianceLevel = opponent.AllianceLevel < 0 ? 0 : opponent.AllianceLevel;
                BattleOpponentAvatarId = opponent.AvatarId < 0 ? 0 : opponent.AvatarId;
                BattleOpponentGender = opponent.Gender;
                BattleOpponentCharacterId = string.IsNullOrWhiteSpace(opponent.CharacterId) ? string.Empty : opponent.CharacterId.Trim();
                BattleOpponentRankTier = string.IsNullOrWhiteSpace(opponent.RankTier) ? "Unranked" : opponent.RankTier;
                BattleOpponentRankPoints = opponent.RankPoints < 0 ? 0 : opponent.RankPoints;
                BattleOpponentLevel = opponent.Level < 1 ? 1 : opponent.Level;
                BattleOpponentWins = opponent.Wins < 0 ? 0 : opponent.Wins;
                BattleOpponentLosses = opponent.Losses < 0 ? 0 : opponent.Losses;
                BattleOpponentMvpCount = opponent.MvpCount < 0 ? 0 : opponent.MvpCount;
                BattleOpponentDifficultyFactor = UnityEngine.Mathf.Clamp(opponent.DifficultyFactor <= 0f ? 1f : opponent.DifficultyFactor, 0.55f, 1.45f);
                BattleOpponentStatusLine = string.IsNullOrWhiteSpace(opponent.StatusLine) ? string.Empty : opponent.StatusLine;
                BattleOpponentIsBot = opponent.IsBot;
            }

            BattleStakePot = stakePot < 0 ? 0 : stakePot;
            BattleMatchSeed = matchSeed <= 0 ? UnityEngine.Random.Range(100000, 999999) : matchSeed;
            CaptureBattleLoadouts(opponent);
            BattleSource = battleSource;
            TournamentId = 0;
            TournamentMatchId = 0;
            TournamentRoundIndex = 0;
        }

        public static void StartTournamentBattle(
            MahjongBattleOpponentData opponent,
            int matchSeed,
            int tournamentId,
            int tournamentMatchId,
            int roundIndex)
        {
            StartBattle(opponent, 0, matchSeed, MahjongBattleSource.Tournament);
            if (LaunchMode != MahjongLaunchMode.Battle)
                return;

            TournamentId = tournamentId < 0 ? 0 : tournamentId;
            TournamentMatchId = tournamentMatchId < 0 ? 0 : tournamentMatchId;
            TournamentRoundIndex = roundIndex < 0 ? 0 : roundIndex;
        }

        public static void StartEndless(int level = 1)
        {
            LaunchMode = MahjongLaunchMode.Endless;

            EndlessLevel = level < 1 ? 1 : level;
            StoryLevel = EndlessLevel;
            StoryStage = 1;
            StoryDifficulty = MahjongStoryDifficulty.Medium;

            ClearBattleRuntime();
        }

        public static void SetStage(int stage)
        {
            StoryStage = stage < 1 ? 1 : stage;
        }

        public static void SetStoryDifficulty(MahjongStoryDifficulty difficulty)
        {
            StoryDifficulty = difficulty == MahjongStoryDifficulty.Unset
                ? MahjongStoryDifficulty.Medium
                : difficulty;
        }

        public static void SetEndlessLevel(int level)
        {
            EndlessLevel = level < 1 ? 1 : level;
            StoryLevel = EndlessLevel;
            StoryStage = 1;
        }

        public static bool EnsureLocalBattleLoadout()
        {
            BattleTileStore store = BattleTileStore.I != null
                ? BattleTileStore.I
                : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>(UnityEngine.FindObjectsInactive.Include);
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (LocalBattleLoadout != null && LocalBattleLoadout.IsCompleteForStore(store))
                return true;

            if (!BattleLoadoutSnapshot.TryCreateFromProfile(profile, store, out BattleLoadoutSnapshot snapshot))
                return false;

            LocalBattleLoadout = snapshot;
            return true;
        }

        public static BattleLoadoutSnapshot GetBattleLoadout(BattleBoardSide side)
        {
            return side == BattleBoardSide.Player ? LocalBattleLoadout : OpponentBattleLoadout;
        }

        public static int GetBattleTileUpgradeLevel(BattleBoardSide side, string tileId)
        {
            BattleLoadoutSnapshot loadout = GetBattleLoadout(side);
            return loadout != null ? loadout.GetUpgradeLevel(tileId) : 0;
        }

        public static void SetOpponentBattleLoadout(BattleLoadoutSnapshot loadout)
        {
            OpponentBattleLoadout = loadout?.Clone();
        }

        private static void CaptureBattleLoadouts(MahjongBattleOpponentData opponent)
        {
            LocalBattleLoadout = null;
            OpponentBattleLoadout = opponent?.Loadout?.Clone();
            EnsureLocalBattleLoadout();

            BattleTileStore store = BattleTileStore.I != null
                ? BattleTileStore.I
                : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>(UnityEngine.FindObjectsInactive.Include);
            if (OpponentBattleLoadout == null && opponent != null && opponent.IsBot)
            {
                int seed = BattleMatchSeed != 0 ? BattleMatchSeed : (opponent.Id ?? string.Empty).GetHashCode();
                OpponentBattleLoadout = BattleLoadoutSnapshot.CreateBot(store, seed ^ 0x5f3759df, opponent.DifficultyFactor);
            }
        }

        public static void Clear()
        {
            LaunchMode = MahjongLaunchMode.None;

            StoryLevel = 1;
            StoryStage = 1;
            StoryDifficulty = MahjongStoryDifficulty.Medium;
            EndlessLevel = 1;

            ClearBattleRuntime();
        }

        private static void ClearBattleRuntime()
        {
            BattleOpponentId = string.Empty;
            BattleOpponentName = string.Empty;
            BattleOpponentAllianceTag = string.Empty;
            BattleOpponentAllianceLevel = 0;
            BattleOpponentAvatarId = 0;
            BattleOpponentGender = PlayerGender.NotSpecified;
            BattleOpponentCharacterId = string.Empty;
            BattleOpponentRankTier = "Unranked";
            BattleOpponentRankPoints = 0;
            BattleOpponentLevel = 1;
            BattleOpponentWins = 0;
            BattleOpponentLosses = 0;
            BattleOpponentMvpCount = 0;
            BattleOpponentDifficultyFactor = 1f;
            BattleOpponentStatusLine = string.Empty;
            BattleOpponentIsBot = true;
            BattleStakePot = 0;
            BattleMatchSeed = 0;
            LocalBattleLoadout = null;
            OpponentBattleLoadout = null;
            BattleSource = MahjongBattleSource.Local;
            TournamentId = 0;
            TournamentMatchId = 0;
            TournamentRoundIndex = 0;
        }

        private static MahjongStoryDifficulty ResolveExistingStoryDifficulty()
        {
            return StoryDifficulty == MahjongStoryDifficulty.Unset
                ? MahjongStoryDifficulty.Medium
                : StoryDifficulty;
        }
    }
}
