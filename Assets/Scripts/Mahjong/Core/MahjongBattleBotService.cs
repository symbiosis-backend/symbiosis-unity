using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongBattleBotService : MonoBehaviour
    {
        public static MahjongBattleBotService I { get; private set; }

        private static readonly string[] Names =
        {
            "Mira", "Sora", "Deniz", "Rin", "Aylin", "Kaan", "Luna", "Akio",
            "Nika", "Arda", "Mavi", "Kai", "Lale", "Yuki", "Mert", "Eda",
            "Nova", "Asel", "Taro", "Mina", "Bora", "Rei", "Selin", "Ken"
        };

        private static readonly string[] Handles =
        {
            "TileFox", "BambooMind", "QuietRiichi", "EastWind", "MoonPair", "JadeHand",
            "LuckyDiscard", "RedDragon", "SilentWall", "RiverRead", "LastTile", "TeaTable",
            "ClosedKan", "FastPair", "SoftShuffle", "NightRon", "TableSense", "CalmStorm"
        };

        private static readonly string[] Tags =
        {
            "x", "gg", "pro", "one", "zen", "mk", "jp", "tr", "88", "77", "24"
        };

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
            MahjongBattleOpponentData bot = new MahjongBattleOpponentData();

            int avatarId = Random.Range(0, 16);
            int rankPoints = GenerateRankPoints(mode, playerRankPoints);
            string rankTier = ResolveRankTier(rankPoints);
            int totalMatches = GenerateTotalMatches(rankPoints);
            int wins = GenerateWins(totalMatches, rankPoints, playerRankPoints);

            bot.Id = "bot_" + System.Guid.NewGuid().ToString("N")[..12];
            bot.DisplayName = GenerateName(rankTier);
            bot.AvatarId = avatarId;
            bot.RankPoints = rankPoints;
            bot.RankTier = rankTier;
            bot.IsBot = true;
            bot.TotalMatches = totalMatches;
            bot.Wins = wins;
            bot.Losses = Mathf.Max(0, totalMatches - wins);
            bot.DifficultyFactor = CalculateDifficulty(rankPoints, playerRankPoints);

            return bot;
        }

        private int GenerateRankPoints(MahjongBattleLobbyMode mode, int playerRankPoints)
        {
            int safePlayerPoints = Mathf.Max(0, playerRankPoints);

            if (mode == MahjongBattleLobbyMode.RankedMatch)
                return Mathf.Max(0, safePlayerPoints + Random.Range(-70, 91));

            if (mode == MahjongBattleLobbyMode.RandomMatch)
            {
                if (Random.value < 0.18f)
                    return Random.Range(0, 900);

                return Mathf.Max(0, safePlayerPoints + Random.Range(-160, 181));
            }

            return Mathf.Max(0, safePlayerPoints + Random.Range(-110, 131));
        }

        private string GenerateName(string rankTier)
        {
            string baseName = Names[Random.Range(0, Names.Length)];
            string handle = Handles[Random.Range(0, Handles.Length)];
            string tag = Tags[Random.Range(0, Tags.Length)];

            int pattern = Random.Range(0, 7);
            int suffix = Random.Range(7, 9999);

            if (rankTier == "Master" && Random.value < 0.35f)
                pattern = 4;

            return pattern switch
            {
                0 => baseName + suffix,
                1 => handle + Random.Range(10, 100),
                2 => baseName + "." + tag,
                3 => tag + "_" + baseName,
                4 => handle,
                5 => baseName + "_" + Random.Range(90, 100),
                _ => handle + "_" + tag
            };
        }

        private string ResolveRankTier(int points)
        {
            if (points >= 800) return RankTiers[4];
            if (points >= 500) return RankTiers[3];
            if (points >= 250) return RankTiers[2];
            if (points >= 100) return RankTiers[1];
            return RankTiers[0];
        }

        private float CalculateDifficulty(int botPoints, int playerPoints)
        {
            int diff = botPoints - playerPoints;

            if (diff >= 250) return 1.25f;
            if (diff >= 120) return 1.12f;
            if (diff >= 40) return 1.05f;
            if (diff <= -250) return 0.78f;
            if (diff <= -120) return 0.86f;
            if (diff <= -40) return 0.94f;

            return 1f;
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
