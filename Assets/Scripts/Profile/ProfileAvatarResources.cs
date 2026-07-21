using System;
using UnityEngine;

namespace MahjongGame
{
    public static class ProfileAvatarResources
    {
        private const string MalePath = "ProfileAvatars/AvatarsMale";
        private const string FemalePath = "ProfileAvatars/AvatarsFemale";
        private const int ReservedMaleAssetNumber = 14;
        private const int ReservedFemaleAssetNumber = 9;
        private const string BlackYangProfileKey = "blackyang";
        private const string WhiteYinProfileKey = "whiteyin";

        private static Sprite[] maleSprites;
        private static Sprite[] femaleSprites;
        private static Sprite[] selectableMaleSprites;
        private static Sprite[] selectableFemaleSprites;

        public static Sprite[] GetSprites(PlayerGender gender)
        {
            return gender == PlayerGender.Female ? GetFemaleSprites() : GetMaleSprites();
        }

        public static Sprite GetSprite(PlayerGender gender, int avatarId)
        {
            Sprite[] sprites = GetAllSprites(gender);
            if (sprites == null || sprites.Length == 0)
                return null;

            int index = Mathf.Clamp(avatarId, 0, sprites.Length - 1);
            return sprites[index];
        }

        public static Sprite GetDisplaySprite(PlayerProfile profile)
        {
            if (profile == null)
                return null;

            if (profile.IsDeveloper)
            {
                Sprite creatorSprite = GetCreatorSprite(profile.DisplayName);
                if (creatorSprite != null)
                    return creatorSprite;
            }

            return GetRegularSprite(profile.Gender, profile.AvatarId);
        }

        public static Sprite GetDeveloperSprite()
        {
            return GetReservedSprite(GetAllMaleSprites(), ReservedMaleAssetNumber);
        }

        public static Sprite GetWhiteYinSprite()
        {
            return GetReservedSprite(GetAllFemaleSprites(), ReservedFemaleAssetNumber);
        }

        public static Sprite GetCreatorSprite(string profileName)
        {
            string profileKey = NormalizeProfileKey(profileName);
            if (profileKey == BlackYangProfileKey)
                return GetDeveloperSprite();

            if (profileKey == WhiteYinProfileKey)
                return GetWhiteYinSprite();

            return null;
        }

        private static Sprite GetReservedSprite(Sprite[] sprites, int reservedAssetNumber)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null && ExtractNumber(sprite.name) == reservedAssetNumber)
                    return sprite;
            }

            return null;
        }

        private static string NormalizeProfileKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            char[] buffer = new char[value.Length];
            int length = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsLetterOrDigit(c))
                    continue;

                buffer[length++] = char.ToLowerInvariant(c);
            }

            return length > 0 ? new string(buffer, 0, length) : string.Empty;
        }

        public static Sprite GetRegularSprite(PlayerGender gender, int avatarId)
        {
            Sprite sprite = GetSprite(gender, avatarId);
            int reservedAssetNumber = gender == PlayerGender.Female
                ? ReservedFemaleAssetNumber
                : ReservedMaleAssetNumber;

            if (sprite == null || ExtractNumber(sprite.name) != reservedAssetNumber)
                return sprite;

            Sprite[] selectableSprites = GetSprites(gender);
            return selectableSprites != null && selectableSprites.Length > 0
                ? selectableSprites[0]
                : null;
        }

        public static int GetAvatarId(PlayerGender gender, int visibleIndex)
        {
            Sprite[] sprites = GetSprites(gender);
            if (sprites == null || sprites.Length == 0)
                return 0;

            int index = Mathf.Clamp(visibleIndex, 0, sprites.Length - 1);
            Sprite selected = sprites[index];
            Sprite[] allSprites = GetAllSprites(gender);
            for (int i = 0; i < allSprites.Length; i++)
            {
                if (ReferenceEquals(allSprites[i], selected))
                    return i;
            }

            return index;
        }

        public static Sprite[] GetMaleSprites()
        {
            if (selectableMaleSprites == null)
                selectableMaleSprites = FilterReserved(GetAllMaleSprites(), ReservedMaleAssetNumber);

            return selectableMaleSprites;
        }

        public static Sprite[] GetFemaleSprites()
        {
            if (selectableFemaleSprites == null)
                selectableFemaleSprites = FilterReserved(GetAllFemaleSprites(), ReservedFemaleAssetNumber);

            return selectableFemaleSprites;
        }

        private static Sprite[] GetAllSprites(PlayerGender gender)
        {
            return gender == PlayerGender.Female ? GetAllFemaleSprites() : GetAllMaleSprites();
        }

        private static Sprite[] GetAllMaleSprites()
        {
            if (maleSprites == null)
                maleSprites = LoadSorted(MalePath);

            return maleSprites;
        }

        private static Sprite[] GetAllFemaleSprites()
        {
            if (femaleSprites == null)
                femaleSprites = LoadSorted(FemalePath);

            return femaleSprites;
        }

        private static Sprite[] FilterReserved(Sprite[] sprites, int reservedAssetNumber)
        {
            if (sprites == null || sprites.Length == 0)
                return Array.Empty<Sprite>();

            int allowedCount = 0;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && ExtractNumber(sprites[i].name) != reservedAssetNumber)
                    allowedCount++;
            }

            Sprite[] allowed = new Sprite[allowedCount];
            int writeIndex = 0;
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null || ExtractNumber(sprite.name) == reservedAssetNumber)
                    continue;

                allowed[writeIndex++] = sprite;
            }

            return allowed;
        }

        private static Sprite[] LoadSorted(string resourcePath)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0)
                return Array.Empty<Sprite>();

            Array.Sort(sprites, CompareSprites);
            return sprites;
        }

        private static int CompareSprites(Sprite left, Sprite right)
        {
            int leftNumber = ExtractNumber(left != null ? left.name : string.Empty);
            int rightNumber = ExtractNumber(right != null ? right.name : string.Empty);

            if (leftNumber != rightNumber)
                return leftNumber.CompareTo(rightNumber);

            return string.Compare(
                left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return int.MaxValue;

            int result = 0;
            bool found = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                {
                    if (found)
                        break;

                    continue;
                }

                found = true;
                result = result * 10 + (c - '0');
            }

            return found ? result : int.MaxValue;
        }
    }
}
