using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class RankedLeagueVisuals
    {
        private const string RankedSpriteRoot = "Mahjong/Sprites/Ranked/";

        private static readonly Dictionary<RankedLeagueId, Sprite> CachedIcons = new Dictionary<RankedLeagueId, Sprite>();

        public static RankedLeagueId ResolveLeagueId(string rankTier, int rankPoints = 0)
        {
            if (!string.IsNullOrWhiteSpace(rankTier))
            {
                string value = rankTier.Trim().ToLowerInvariant();
                if (value.Contains("master")) return RankedLeagueId.Master;
                if (value.Contains("platinum") || value.Contains("platine")) return RankedLeagueId.Platinum;
                if (value.Contains("gold")) return RankedLeagueId.Gold;
                if (value.Contains("silver")) return RankedLeagueId.Silver;
                if (value.Contains("bronze") || value.Contains("bronz")) return RankedLeagueId.Bronze;
            }

            if (rankPoints >= 900) return RankedLeagueId.Master;
            if (rankPoints >= 500) return RankedLeagueId.Platinum;
            if (rankPoints >= 250) return RankedLeagueId.Gold;
            if (rankPoints >= 100) return RankedLeagueId.Silver;
            return RankedLeagueId.Bronze;
        }

        public static Sprite LoadLeagueIcon(RankedLeagueId leagueId)
        {
            Sprite cached;
            if (CachedIcons.TryGetValue(leagueId, out cached) && cached != null)
                return cached;

            string path = RankedSpriteRoot + GetSpriteName(leagueId);
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(path);
                if (sprites != null && sprites.Length > 0)
                    sprite = sprites[0];
            }

            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            CachedIcons[leagueId] = sprite;
            return sprite;
        }

        private static string GetSpriteName(RankedLeagueId leagueId)
        {
            switch (leagueId)
            {
                case RankedLeagueId.Silver:
                    return "SilverRank";
                case RankedLeagueId.Gold:
                    return "GoldRank";
                case RankedLeagueId.Platinum:
                    return "PlatinumRank";
                case RankedLeagueId.Master:
                    return "MasterRank";
                default:
                    return "BronzeRank";
            }
        }
    }
}
