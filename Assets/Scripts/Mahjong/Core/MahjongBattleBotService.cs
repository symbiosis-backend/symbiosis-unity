using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongBattleBotService : MonoBehaviour
    {
        public static MahjongBattleBotService I { get; private set; }

        private static readonly string[] RankTiers =
        {
            "Bronze", "Silver", "Gold", "Jade", "Master"
        };

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        public MahjongBattleOpponentData CreateRandomOpponent(int playerRankPoints = 0)
        {
            return CreateOpponent(MahjongBattleLobbyMode.RandomMatch, playerRankPoints);
        }

        public MahjongBattleOpponentData CreateOpponent(MahjongBattleLobbyMode mode, int playerRankPoints = 0)
        {
            MahjongBattleData playerBattle = ResolvePlayerBattleData();
            MahjongBattleOpponentData bot = new MahjongBattleOpponentData();

            PlayerGender avatarGender = Random.value < 0.5f ? PlayerGender.Male : PlayerGender.Female;
            int selectableAvatarCount = ProfileAvatarResources.GetSprites(avatarGender).Length;
            int avatarId = selectableAvatarCount > 0
                ? ProfileAvatarResources.GetAvatarId(avatarGender, Random.Range(0, selectableAvatarCount))
                : 0;
            int rankPoints = GenerateRankPoints(mode, playerRankPoints, playerBattle);
            string rankTier = ResolveRankTier(rankPoints);
            int totalMatches = GenerateTotalMatches(rankPoints);
            int wins = GenerateWins(totalMatches, rankPoints, playerRankPoints);
            NaturalBotProfile profile = NaturalBotProfileGenerator.CreateProfile(rankTier);

            bot.Id = "bot_" + System.Guid.NewGuid().ToString("N")[..12];
            bot.DisplayName = profile.Nickname;
            bot.AvatarId = avatarId;
            bot.Gender = avatarGender;
            bot.RankPoints = rankPoints;
            bot.RankTier = rankTier;
            bot.Level = Mathf.Max(1, 1 + rankPoints / 100);
            bot.IsBot = true;
            bot.TotalMatches = totalMatches;
            bot.Wins = wins;
            bot.Losses = Mathf.Max(0, totalMatches - wins);
            bot.MvpCount = Mathf.Clamp(Mathf.RoundToInt(wins * Random.Range(0.18f, 0.48f)), 0, totalMatches);
            bot.DifficultyFactor = CalculateDifficulty(mode, rankPoints, playerRankPoints, playerBattle);
            bot.StatusLine = profile.StatusLine;
            BattleTileStore store = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
            bot.Loadout = BattleLoadoutSnapshot.CreateBot(store, bot.Id.GetHashCode(), bot.DifficultyFactor);

            return bot;
        }

        private int GenerateRankPoints(MahjongBattleLobbyMode mode, int playerRankPoints, MahjongBattleData playerBattle)
        {
            int safePlayerPoints = Mathf.Max(0, playerRankPoints);
            float playerPressure = ResolvePlayerPressure(playerBattle);

            if (mode == MahjongBattleLobbyMode.RankedMatch)
            {
                int centerOffset = Mathf.RoundToInt(Mathf.Lerp(-10f, 165f, playerPressure));
                return Mathf.Max(0, safePlayerPoints + centerOffset + Random.Range(-50, 91));
            }

            if (mode == MahjongBattleLobbyMode.RandomMatch)
            {
                if (Random.value < 0.18f)
                    return Random.Range(0, 900);

                int centerOffset = Mathf.RoundToInt(Mathf.Lerp(-130f, 95f, playerPressure));
                return Mathf.Max(0, safePlayerPoints + centerOffset + Random.Range(-150, 171));
            }

            int fallbackOffset = Mathf.RoundToInt(Mathf.Lerp(-95f, 75f, playerPressure));
            return Mathf.Max(0, safePlayerPoints + fallbackOffset + Random.Range(-110, 131));
        }

        private string ResolveRankTier(int points)
        {
            if (points >= 800) return RankTiers[4];
            if (points >= 500) return RankTiers[3];
            if (points >= 250) return RankTiers[2];
            if (points >= 100) return RankTiers[1];
            return RankTiers[0];
        }

        private float CalculateDifficulty(MahjongBattleLobbyMode mode, int botPoints, int playerPoints, MahjongBattleData playerBattle)
        {
            int diff = botPoints - playerPoints;
            float difficulty = 1f;

            if (diff >= 250) difficulty = 1.20f;
            else if (diff >= 120) difficulty = 1.10f;
            else if (diff >= 40) difficulty = 1.04f;
            else if (diff <= -250) difficulty = 0.78f;
            else if (diff <= -120) difficulty = 0.86f;
            else if (diff <= -40) difficulty = 0.94f;

            float playerPressure = ResolvePlayerPressure(playerBattle);
            bool ranked = mode == MahjongBattleLobbyMode.RankedMatch;

            difficulty += ranked
                ? Mathf.Lerp(0.00f, 0.40f, playerPressure)
                : Mathf.Lerp(-0.22f, 0.28f, playerPressure);

            if (playerBattle != null)
            {
                difficulty += Mathf.Clamp(playerBattle.WinStreak, 0, 6) * (ranked ? 0.052f : 0.035f);
                difficulty += Mathf.Clamp(playerBattle.BestWinStreak - 3, 0, 7) * (ranked ? 0.018f : 0.01f);

                if (ranked && playerBattle.TotalMatches >= 10 && playerBattle.WinRatePercent >= 62)
                    difficulty += 0.07f;
            }

            return ranked
                ? Mathf.Clamp(difficulty, 0.84f, 1.43f)
                : Mathf.Clamp(difficulty, 0.62f, 1.38f);
        }

        private float ResolvePlayerPressure(MahjongBattleData playerBattle)
        {
            if (playerBattle == null)
                return 0f;

            playerBattle.EnsureValid();

            int totalMatches = Mathf.Max(0, playerBattle.TotalMatches);
            if (totalMatches <= 0)
                return 0f;

            float experience = Mathf.InverseLerp(0f, 18f, totalMatches);
            float winRate = playerBattle.Wins / (float)Mathf.Max(1, totalMatches);
            float winRatePressure = Mathf.InverseLerp(0.42f, 0.76f, winRate);
            float streakPressure = Mathf.InverseLerp(0f, 5f, Mathf.Max(0, playerBattle.WinStreak));
            float rankPressure = Mathf.InverseLerp(0f, 800f, Mathf.Max(0, playerBattle.RankPoints));

            return Mathf.Clamp01(
                experience * 0.28f +
                winRatePressure * 0.36f +
                streakPressure * 0.24f +
                rankPressure * 0.12f);
        }

        private MahjongBattleData ResolvePlayerBattleData()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return null;

            profile.EnsureData();
            return profile.Mahjong != null ? profile.Mahjong.Battle : null;
        }

        private int GenerateTotalMatches(int rankPoints)
        {
            if (rankPoints >= 800)
                return Random.Range(420, 1600);
            if (rankPoints >= 500)
                return Random.Range(220, 900);
            if (rankPoints >= 250)
                return Random.Range(90, 430);
            if (rankPoints >= 100)
                return Random.Range(35, 190);

            return Random.Range(8, 85);
        }

        private int GenerateWins(int totalMatches, int botPoints, int playerPoints)
        {
            float rankWinRate = Mathf.InverseLerp(0f, 900f, botPoints);
            float playerDelta = Mathf.Clamp((botPoints - playerPoints) / 700f, -0.12f, 0.12f);
            float winRate = Mathf.Clamp(Random.Range(0.42f, 0.57f) + rankWinRate * 0.12f + playerDelta, 0.32f, 0.72f);
            int wins = Mathf.RoundToInt(totalMatches * winRate) + Random.Range(-3, 4);
            return Mathf.Clamp(wins, 1, Mathf.Max(1, totalMatches - 1));
        }
    }
}
