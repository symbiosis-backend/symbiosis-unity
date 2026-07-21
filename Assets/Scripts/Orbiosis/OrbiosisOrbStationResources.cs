using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame.Orbiosis
{
    public static class OrbiosisOrbStationResources
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;
        public const string BattleEvolutionFolder = "Orbiosis/OrbStationBattleEvolution";

        private static readonly Sprite[] CachedSprites = new Sprite[MaxLevel - MinLevel + 1];

        public static string GetBattleEvolutionResourcePath(int level)
        {
            int clampedLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
            return BattleEvolutionFolder + "/OrbBattleStation_Level" + clampedLevel.ToString("00");
        }

        public static Sprite LoadBattleEvolutionSprite(int level)
        {
            int clampedLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
            int cacheIndex = clampedLevel - MinLevel;
            if (CachedSprites[cacheIndex] != null)
                return CachedSprites[cacheIndex];

            string resourcePath = GetBattleEvolutionResourcePath(clampedLevel);
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            Sprite sprite = null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = resourcePath.Replace("/", "_") + "_RuntimeSprite";
            }

            if (sprite == null)
                sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
                sprite = Resources.Load<Sprite>(resourcePath + "_0");
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
                if (sprites != null && sprites.Length > 0)
                    sprite = sprites[0];
            }

            CachedSprites[cacheIndex] = sprite;
            return sprite;
        }

        public static bool ApplyBattleEvolutionSprite(Image image, int level)
        {
            if (image == null)
                return false;

            Sprite sprite = LoadBattleEvolutionSprite(level);
            if (sprite == null)
                return false;

            image.enabled = true;
            image.sprite = sprite;
            image.overrideSprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return true;
        }
    }
}
