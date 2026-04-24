using System;
using UnityEngine;

namespace MahjongGame
{
    public static class ProfileAvatarResources
    {
        private const string MalePath = "ProfileAvatars/AvatarsMale";
        private const string FemalePath = "ProfileAvatars/AvatarsFemale";

        private static Sprite[] maleSprites;
        private static Sprite[] femaleSprites;

        public static Sprite[] GetSprites(PlayerGender gender)
        {
            return gender == PlayerGender.Female ? GetFemaleSprites() : GetMaleSprites();
        }

        public static Sprite GetSprite(PlayerGender gender, int avatarId)
        {
            Sprite[] sprites = GetSprites(gender);
            if (sprites == null || sprites.Length == 0)
                return null;

            int index = Mathf.Clamp(avatarId, 0, sprites.Length - 1);
            return sprites[index];
        }

        public static Sprite[] GetMaleSprites()
        {
            if (maleSprites == null)
                maleSprites = LoadSorted(MalePath);

            return maleSprites;
        }

        public static Sprite[] GetFemaleSprites()
        {
            if (femaleSprites == null)
                femaleSprites = LoadSorted(FemalePath);

            return femaleSprites;
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
                    continue;

                found = true;
                result = result * 10 + (c - '0');
            }

            return found ? result : int.MaxValue;
        }
    }
}
