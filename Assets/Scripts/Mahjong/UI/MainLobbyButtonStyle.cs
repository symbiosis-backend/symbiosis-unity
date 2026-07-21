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
        private const string BankFullscreenBackgroundResourcePath = "Mahjong/Sprites/BankUI/BankSciFiBackground";
        private const string BankWindowFrameResourcePath = "Mahjong/Sprites/BankUI/BankSciFiWindowFrame";
        private const string BankModuleResourcePath = "Mahjong/Sprites/BankUI/BankSciFiModule";
        private const string BankButtonResourcePath = "Mahjong/Sprites/BankUI/BankSciFiButton";
        private const string DlsWindowResourcePath = "Mahjong/Sprites/Alliance/WindowDLS";
        private const string CloseIconResourcePath = "Mahjong/Sprites/MainSettings/MainCloseIcon";
        private const string GoldCurrencyResourcePath = "Mahjong/Sprites/Money/OzAlt\u0131n";
        private const string AmetistCurrencyResourcePath = "Mahjong/Sprites/Money/OzAmetist";
        private const string FontResourcePath = "Fonts/Philosopher-Regular";
        private const string FallbackFontResourcePath = "Fonts/Trade SDF";
        private static readonly Vector4 MainButtonTextInset = new Vector4(36f, 10f, 36f, 12f);

        private static Sprite cachedSprite;
        private static Sprite cachedAvatarCardSprite;
        private static Sprite cachedMainFrameSprite;
        private static Sprite cachedProfileWindowSprite;
        private static Sprite cachedStoreBankWindowSprite;
        private static Sprite cachedBankFullscreenBackgroundSprite;
        private static Sprite cachedBankWindowFrameSprite;
        private static Sprite cachedBankModuleSprite;
        private static Sprite cachedBankButtonSprite;
        private static Sprite cachedDlsWindowSprite;
        private static Sprite cachedCloseIconSprite;
        private static Sprite cachedGoldCurrencySprite;
        private static Sprite cachedAmetistCurrencySprite;
        private static TMP_FontAsset cachedMainFont;

        public static Sprite ButtonSprite => LoadButtonSprite();
        public static Sprite AvatarCardSprite => LoadAvatarCardSprite();
        public static Sprite MainFrameSprite => LoadMainFrameSprite();
        public static Sprite ProfileWindowSprite => LoadProfileWindowSprite();
        public static Sprite StoreBankWindowSprite => LoadStoreBankWindowSprite();
        public static Sprite BankFullscreenBackgroundSprite => LoadBankFullscreenBackgroundSprite();
        public static Sprite BankWindowFrameSprite => LoadBankWindowFrameSprite();
        public static Sprite BankModuleSprite => LoadBankModuleSprite();
        public static Sprite BankButtonSprite => LoadBankButtonSprite();
        public static Sprite DlsWindowSprite => LoadDlsWindowSprite();
        public static Sprite CloseIconSprite => LoadCloseIconSprite();
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
            button.image.preserveAspect = false;
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
                label.overflowMode = TextOverflowModes.Truncate;
                ApplyButtonLabelLayout(label);
                label.gameObject.SetActive(keepLabelVisible);
            }
        }

        public static void ApplyButtonLabelLayout(TMP_Text label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;

            RectTransform rect = label.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(MainButtonTextInset.x, MainButtonTextInset.y);
            rect.offsetMax = new Vector2(-MainButtonTextInset.z, -MainButtonTextInset.w);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public static void ApplyFont(TMP_Text text)
        {
            if (text == null)
                return;

            if (LocalizedTextStyle.ApplyIfLocalized(text))
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

        public static void ApplyDlsWindow(Image image)
        {
            if (image == null)
                return;

            Sprite sprite = LoadDlsWindowSprite();
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        public static void ApplyCloseIconButton(Button button)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = LoadCloseIconSprite();
            if (sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = true;
            button.image.color = Color.white;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.gameObject.SetActive(false);
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

        private static Sprite LoadBankFullscreenBackgroundSprite()
        {
            if (cachedBankFullscreenBackgroundSprite != null)
                return cachedBankFullscreenBackgroundSprite;

            cachedBankFullscreenBackgroundSprite = LoadAnySprite(BankFullscreenBackgroundResourcePath);
            return cachedBankFullscreenBackgroundSprite;
        }

        private static Sprite LoadBankWindowFrameSprite()
        {
            if (cachedBankWindowFrameSprite != null)
                return cachedBankWindowFrameSprite;

            cachedBankWindowFrameSprite = LoadAnySprite(BankWindowFrameResourcePath);
            return cachedBankWindowFrameSprite;
        }

        private static Sprite LoadBankModuleSprite()
        {
            if (cachedBankModuleSprite != null)
                return cachedBankModuleSprite;

            cachedBankModuleSprite = LoadAnySprite(BankModuleResourcePath);
            return cachedBankModuleSprite;
        }

        private static Sprite LoadBankButtonSprite()
        {
            if (cachedBankButtonSprite != null)
                return cachedBankButtonSprite;

            cachedBankButtonSprite = LoadAnySprite(BankButtonResourcePath);
            return cachedBankButtonSprite;
        }

        private static Sprite LoadDlsWindowSprite()
        {
            if (cachedDlsWindowSprite != null)
                return cachedDlsWindowSprite;

            cachedDlsWindowSprite = LoadAnySprite(DlsWindowResourcePath);
            return cachedDlsWindowSprite;
        }

        private static Sprite LoadCloseIconSprite()
        {
            if (cachedCloseIconSprite != null)
                return cachedCloseIconSprite;

            cachedCloseIconSprite = LoadAnySprite(CloseIconResourcePath);
            return cachedCloseIconSprite;
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
            if (cachedMainFont != null)
            {
                LocalizedTextStyle.EnsureRuntimeFallbacks(cachedMainFont);
                return cachedMainFont;
            }

            cachedMainFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (cachedMainFont == null)
            {
                Font sourceFont = Resources.Load<Font>(FontResourcePath);
                if (sourceFont != null)
                {
                    cachedMainFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                    if (cachedMainFont != null)
                        cachedMainFont.name = "Philosopher Runtime SDF";
                }
            }

            if (cachedMainFont == null)
                cachedMainFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);

            LocalizedTextStyle.EnsureRuntimeFallbacks(cachedMainFont);
            return cachedMainFont;
        }

    }
}
