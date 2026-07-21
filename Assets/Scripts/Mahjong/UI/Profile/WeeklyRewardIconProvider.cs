using UnityEngine;

namespace MahjongGame
{
    public static class WeeklyRewardIconProvider
    {
        private const int WeeklyDayCount = 7;
        private const string GeneratedIconResourceFolder = "Mahjong/Sprites/Rewards/WeeklyPremiumV2/";
        private const string RewardSpriteSheetResourcePath = "Mahjong/Sprites/Rewards/Rewards";
        private static readonly string[] PreferredImportedSpriteNames =
        {
            "Rewards_1",
            "Rewards_22",
            "Rewards_8",
            "Rewards_15",
            "Rewards_3",
            "Rewards_33",
            "Rewards_37"
        };

        private static Sprite[] cachedSprites;

        public static Sprite GetDaySprite(int dayIndex)
        {
            if (cachedSprites == null)
                cachedSprites = LoadRewardSprites();

            if (cachedSprites == null || cachedSprites.Length == 0)
                return MainLobbyButtonStyle.GoldCurrencySprite;

            return cachedSprites[Mathf.Clamp(GetSpriteIndexForDay(dayIndex), 0, cachedSprites.Length - 1)];
        }

        private static int GetSpriteIndexForDay(int dayIndex)
        {
            return Mathf.Clamp(dayIndex, 0, 6);
        }

        private static Sprite[] LoadRewardSprites()
        {
            Sprite[] generatedSprites = LoadGeneratedSprites();
            if (generatedSprites.Length == WeeklyDayCount)
                return generatedSprites;

            Sprite[] importedSprites = LoadImportedSprites();
            if (importedSprites.Length > 0)
                return importedSprites;

            Texture2D texture = Resources.Load<Texture2D>(RewardSpriteSheetResourcePath);
            if (texture == null)
                return new Sprite[0];

            const int columns = 4;
            const int rows = 2;
            int spriteCount = columns * rows;
            float cellWidth = texture.width / (float)columns;
            float cellHeight = texture.height / (float)rows;

            Sprite[] sprites = new Sprite[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                Rect rect = GetSpriteRect(texture.height, cellWidth, cellHeight, column, row, i);
                sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            }

            return sprites;
        }

        private static Sprite[] LoadGeneratedSprites()
        {
            Sprite[] sprites = new Sprite[WeeklyDayCount];
            for (int i = 0; i < WeeklyDayCount; i++)
            {
                string resourcePath = GeneratedIconResourceFolder + "WeeklyRewardDay" + (i + 1);
                Sprite sprite = LoadSingleSprite(resourcePath);
                if (sprite == null)
                    return new Sprite[0];

                sprites[i] = sprite;
            }

            return sprites;
        }

        private static Sprite LoadSingleSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            for (int i = 0; sprites != null && i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    return sprites[i];
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return texture != null
                ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
        }

        private static Sprite[] LoadImportedSprites()
        {
            Sprite[] source = Resources.LoadAll<Sprite>(RewardSpriteSheetResourcePath);
            if (source == null || source.Length == 0)
                return new Sprite[0];

            Sprite[] result = new Sprite[PreferredImportedSpriteNames.Length];
            int count = 0;
            for (int i = 0; i < PreferredImportedSpriteNames.Length; i++)
            {
                Sprite sprite = FindSpriteByName(source, PreferredImportedSpriteNames[i]);
                if (sprite == null)
                    continue;

                result[count] = sprite;
                count++;
            }

            if (count == 0)
                return new Sprite[0];

            if (count == result.Length)
                return result;

            Sprite[] compact = new Sprite[count];
            for (int i = 0; i < count; i++)
                compact[i] = result[i];

            return compact;
        }

        private static Sprite FindSpriteByName(Sprite[] sprites, string spriteName)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null && string.Equals(sprite.name, spriteName, System.StringComparison.Ordinal))
                    return sprite;
            }

            return null;
        }

        private static Rect GetSpriteRect(float textureHeight, float cellWidth, float cellHeight, int column, int row, int spriteIndex)
        {
            Vector4 crop = spriteIndex switch
            {
                0 => new Vector4(76f, 218f, 308f, 250f),
                1 => new Vector4(30f, 180f, 348f, 292f),
                2 => new Vector4(0f, 150f, 384f, 326f),
                3 => new Vector4(0f, 150f, 352f, 342f),
                4 => new Vector4(42f, 44f, 342f, 276f),
                5 => new Vector4(14f, 0f, 370f, 334f),
                6 => new Vector4(0f, 0f, 384f, 338f),
                7 => new Vector4(0f, 0f, 358f, 342f),
                _ => new Vector4(cellWidth * 0.05f, cellHeight * 0.1f, cellWidth * 0.9f, cellHeight * 0.7f)
            };

            float cropX = column * cellWidth + crop.x;
            float cellBottomY = textureHeight - (row + 1) * cellHeight;
            float cropY = cellBottomY + cellHeight - crop.y - crop.w;
            return new Rect(cropX, cropY, crop.z, crop.w);
        }
    }
}
