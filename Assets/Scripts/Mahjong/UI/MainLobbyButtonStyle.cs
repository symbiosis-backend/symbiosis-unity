using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class MainLobbyButtonStyle
    {
        private const string ButtonResourcePath = "Mahjong/Sprites/MainSettings/BtnMainStandart";
        private const string ButtonSpriteName = "BtnMainStandart_0";
        private const string AvatarCardResourcePath = "Mahjong/Sprites/MainSettings/AvatarCard";
        private const string AvatarCardSpriteName = "AvatarCard_0";
        private const string MainFrameResourcePath = "Mahjong/Sprites/MainSettings/RamkaMain";
        private const string MainFrameSpriteName = "RamkaMain_0";
        private const string ProfileWindowResourcePath = "Mahjong/Sprites/WindowProfile";
        private const string ProfileWindowSpriteName = "WindowProfile_0";
        private const string StoreBankWindowResourcePath = "Mahjong/Sprites/WindowStoreBank";
        private const string StoreBankWindowSpriteName = "WindowStoreBank_0";
        private const string GoldCurrencyResourcePath = "Mahjong/Sprites/Money/OzAlt\u0131n";
        private const string AmetistCurrencyResourcePath = "Mahjong/Sprites/Money/OzAmetist";
        private const string FontResourcePath = "Fonts/tilt-neon-regular SDF";
        private const string FallbackFontResourcePath = "Fonts/Trade SDF";

        private static Sprite cachedSprite;
        private static Sprite cachedAvatarCardSprite;
        private static Sprite cachedMainFrameSprite;
        private static Sprite cachedProfileWindowSprite;
        private static Sprite cachedStoreBankWindowSprite;
        private static Sprite cachedGoldCurrencySprite;
        private static Sprite cachedAmetistCurrencySprite;
        private static TMP_FontAsset cachedFont;

        public static Sprite ButtonSprite => LoadButtonSprite();
        public static Sprite AvatarCardSprite => LoadAvatarCardSprite();
        public static Sprite MainFrameSprite => LoadMainFrameSprite();
        public static Sprite ProfileWindowSprite => LoadProfileWindowSprite();
        public static Sprite StoreBankWindowSprite => LoadStoreBankWindowSprite();
        public static Sprite GoldCurrencySprite => LoadGoldCurrencySprite();
        public static Sprite AmetistCurrencySprite => LoadAmetistCurrencySprite();
        public static TMP_FontAsset Font => LoadFont();

        private static readonly VertexGradient SilverTextGradient = new VertexGradient(
            new Color32(255, 255, 255, 255),
            new Color32(235, 244, 255, 255),
            new Color32(122, 136, 154, 255),
            new Color32(190, 204, 224, 255));

        public static void Apply(Button button, bool keepLabelVisible = true)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = LoadButtonSprite();
            if (sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = true;
            button.image.color = Color.white;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                ApplyFont(label);
                ApplySilverTextEffect(label);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(10f, label.fontSize * 0.55f);
                label.fontSizeMax = label.fontSize;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.margin = new Vector4(8f, 1f, 8f, 3f);
                label.gameObject.SetActive(keepLabelVisible);
            }
        }

        public static void ApplyFont(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = LoadFont();
            if (font != null)
                text.font = font;
        }

        public static void ApplySilverTextEffect(TMP_Text text)
        {
            if (text == null)
                return;

            text.enableVertexGradient = true;
            text.colorGradient = SilverTextGradient;
            text.color = Color.white;
        }

        public static void ApplyAvatarCard(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadAvatarCardSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void ApplyMainFrame(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadMainFrameSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void ApplyProfileWindow(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadProfileWindowSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void ApplyStoreBankWindow(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadStoreBankWindowSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void ApplyGoldCurrencyIcon(Image image)
        {
            ApplySprite(image, LoadGoldCurrencySprite(), true);
        }

        public static void ApplyAmetistCurrencyIcon(Image image)
        {
            ApplySprite(image, LoadAmetistCurrencySprite(), true);
        }

        private static Sprite LoadButtonSprite()
        {
            if (cachedSprite != null)
                return cachedSprite;

            Sprite sprite = Resources.Load<Sprite>(ButtonResourcePath);
            if (sprite != null)
            {
                cachedSprite = sprite;
                return cachedSprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(ButtonResourcePath);
            if (sprites == null || sprites.Length == 0)
                return null;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == ButtonSpriteName)
                {
                    cachedSprite = sprites[i];
                    return cachedSprite;
                }
            }

            cachedSprite = sprites[0];
            return cachedSprite;
        }

        private static Sprite LoadAvatarCardSprite()
        {
            if (cachedAvatarCardSprite != null)
                return cachedAvatarCardSprite;

            cachedAvatarCardSprite = LoadNamedSprite(AvatarCardResourcePath, AvatarCardSpriteName);
            return cachedAvatarCardSprite;
        }

        private static Sprite LoadMainFrameSprite()
        {
            if (cachedMainFrameSprite != null)
                return cachedMainFrameSprite;

            cachedMainFrameSprite = LoadNamedSprite(MainFrameResourcePath, MainFrameSpriteName);
            return cachedMainFrameSprite;
        }

        private static Sprite LoadProfileWindowSprite()
        {
            if (cachedProfileWindowSprite != null)
                return cachedProfileWindowSprite;

            cachedProfileWindowSprite = LoadNamedSprite(ProfileWindowResourcePath, ProfileWindowSpriteName);
            return cachedProfileWindowSprite;
        }

        private static Sprite LoadStoreBankWindowSprite()
        {
            if (cachedStoreBankWindowSprite != null)
                return cachedStoreBankWindowSprite;

            cachedStoreBankWindowSprite = LoadNamedSprite(StoreBankWindowResourcePath, StoreBankWindowSpriteName);
            return cachedStoreBankWindowSprite;
        }

        private static Sprite LoadGoldCurrencySprite()
        {
            if (cachedGoldCurrencySprite != null)
                return cachedGoldCurrencySprite;

            cachedGoldCurrencySprite = LoadAnySprite(GoldCurrencyResourcePath);
            return cachedGoldCurrencySprite;
        }

        private static Sprite LoadAmetistCurrencySprite()
        {
            if (cachedAmetistCurrencySprite != null)
                return cachedAmetistCurrencySprite;

            cachedAmetistCurrencySprite = LoadAnySprite(AmetistCurrencyResourcePath);
            return cachedAmetistCurrencySprite;
        }

        private static Sprite LoadNamedSprite(string resourcePath, string spriteName)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null && sprites[i].name == spriteName)
                        return sprites[i];
                }

                return sprites[0];
            }

            return Resources.Load<Sprite>(resourcePath);
        }

        private static Sprite LoadAnySprite(string resourcePath)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];

            return Resources.Load<Sprite>(resourcePath);
        }

        private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static TMP_FontAsset LoadFont()
        {
            if (cachedFont != null)
                return cachedFont;

            cachedFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (cachedFont == null)
                cachedFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
            return cachedFont;
        }
    }
}
