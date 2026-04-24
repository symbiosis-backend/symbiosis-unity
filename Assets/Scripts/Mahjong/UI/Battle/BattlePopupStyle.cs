using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class BattlePopupStyle
    {
        private const string WindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/SettingsBattleWindow";
        private const string ButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/BattleLobbyButton";
        private const string MainFontResourcePath = "Fonts & Materials/LiberationSans SDF";

        private static readonly Rect BattleButtonSpriteRect = new Rect(67f, 373f, 1431f, 339f);
        private static readonly Vector4 BattleWindowBorder = new Vector4(84f, 76f, 84f, 76f);
        private static readonly Vector4 BattleFrontBorder = new Vector4(52f, 44f, 52f, 44f);
        private static readonly Vector4 CompactButtonMargin = new Vector4(22f, 6f, 22f, 8f);
        private static readonly Vector4 LargeButtonMargin = new Vector4(34f, 10f, 34f, 12f);

        private static Sprite cachedWindowSourceSprite;
        private static Sprite cachedWindowSprite;
        private static Sprite cachedFrontSprite;
        private static Sprite cachedButtonSourceSprite;
        private static Sprite cachedButtonSprite;
        private static TMP_FontAsset cachedFont;

        public static Sprite WindowSprite => LoadWindowSprite();
        public static Sprite FrontSprite => LoadFrontSprite();
        public static Sprite ButtonSprite => LoadButtonSprite();

        public static bool ApplyWindow(Image image, bool raycastTarget = true)
        {
            return ApplyFrameImage(image, LoadWindowSprite(), raycastTarget);
        }

        public static bool ApplyFront(Image image, bool raycastTarget = false)
        {
            return ApplyFrameImage(image, LoadFrontSprite(), raycastTarget);
        }

        public static bool ApplyButton(Button button, bool preserveCurrentColor = false, bool keepLabelVisible = true)
        {
            if (button == null || button.image == null)
                return false;

            Color color = preserveCurrentColor ? button.image.color : Color.white;
            Sprite sprite = LoadButtonSprite();
            if (sprite == null)
                return false;

            button.image.sprite = sprite;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
            button.image.color = color;
            button.image.raycastTarget = true;
            button.targetGraphic = button.image;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                ApplyText(label, true);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(12f, label.fontSize * 0.58f);
                label.fontSizeMax = Mathf.Max(label.fontSize, label.fontSizeMax);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.margin = IsLargeButton(button) ? LargeButtonMargin : CompactButtonMargin;
                label.gameObject.SetActive(keepLabelVisible);
            }

            return true;
        }

        public static void ApplyText(TMP_Text text, bool silver = false)
        {
            if (text == null)
                return;

            TMP_FontAsset font = LoadFont();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            if (silver)
            {
                MainLobbyButtonStyle.ApplySilverTextEffect(text);
            }
            else
            {
                text.enableVertexGradient = false;
                text.color = Color.white;
            }
        }

        private static bool ApplyFrameImage(Image image, Sprite sprite, bool raycastTarget)
        {
            if (image == null || sprite == null)
                return false;

            image.enabled = true;
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
            return true;
        }

        private static bool IsLargeButton(Button button)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
                return false;

            return rect.rect.width >= 250f || rect.rect.height >= 70f;
        }

        private static Sprite LoadWindowSprite()
        {
            if (cachedWindowSprite != null)
                return cachedWindowSprite;

            Sprite source = LoadWindowSourceSprite();
            if (source == null)
                return null;

            cachedWindowSprite = CreateSlicedSprite(source, BattleWindowBorder);
            return cachedWindowSprite;
        }

        private static Sprite LoadFrontSprite()
        {
            if (cachedFrontSprite != null)
                return cachedFrontSprite;

            Sprite source = LoadWindowSourceSprite();
            if (source == null)
                return null;

            cachedFrontSprite = CreateSlicedSprite(source, BattleFrontBorder);
            return cachedFrontSprite;
        }

        private static Sprite LoadButtonSprite()
        {
            if (cachedButtonSprite != null)
                return cachedButtonSprite;

            cachedButtonSourceSprite ??= LoadLargestSprite(ButtonResourcePath, "BattleLobbyButton");
            if (cachedButtonSourceSprite == null)
                return null;

            cachedButtonSprite = CreateRuntimeSpriteVariant(cachedButtonSourceSprite, BattleButtonSpriteRect);
            return cachedButtonSprite;
        }

        private static TMP_FontAsset LoadFont()
        {
            if (cachedFont != null)
                return cachedFont;

            cachedFont = Resources.Load<TMP_FontAsset>(MainFontResourcePath);
            if (cachedFont == null)
                cachedFont = TMP_Settings.defaultFontAsset;

            return cachedFont;
        }

        private static Sprite LoadWindowSourceSprite()
        {
            if (cachedWindowSourceSprite != null)
                return cachedWindowSourceSprite;

            cachedWindowSourceSprite = LoadLargestSprite(WindowResourcePath, "SettingsBattleWindow");
            return cachedWindowSourceSprite;
        }

        private static Sprite LoadLargestSprite(string resourcePath, string preferredSpriteName)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(preferredSpriteName))
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] != null && string.Equals(sprites[i].name, preferredSpriteName, System.StringComparison.Ordinal))
                            return sprites[i];
                    }
                }

                Sprite largest = null;
                float largestArea = 0f;
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    if (sprite == null)
                        continue;

                    float area = sprite.rect.width * sprite.rect.height;
                    if (largest == null || area > largestArea)
                    {
                        largest = sprite;
                        largestArea = area;
                    }
                }

                if (largest != null)
                    return largest;
            }

            return Resources.Load<Sprite>(resourcePath);
        }

        private static Sprite CreateRuntimeSpriteVariant(Sprite source, Rect targetRect)
        {
            if (source == null || source.texture == null)
                return source;

            Rect sourceRect = source.textureRect;
            Rect rect = ClampRectToBounds(targetRect, sourceRect);
            if (Mathf.Approximately(rect.x, sourceRect.x)
                && Mathf.Approximately(rect.y, sourceRect.y)
                && Mathf.Approximately(rect.width, sourceRect.width)
                && Mathf.Approximately(rect.height, sourceRect.height))
            {
                return source;
            }

            return Sprite.Create(
                source.texture,
                rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        private static Sprite CreateSlicedSprite(Sprite source, Vector4 border)
        {
            if (source == null || source.texture == null)
                return source;

            return Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
        }

        private static Rect ClampRectToBounds(Rect targetRect, Rect bounds)
        {
            float x = Mathf.Clamp(targetRect.x, bounds.xMin, bounds.xMax - 1f);
            float y = Mathf.Clamp(targetRect.y, bounds.yMin, bounds.yMax - 1f);
            float width = Mathf.Clamp(targetRect.width, 1f, bounds.xMax - x);
            float height = Mathf.Clamp(targetRect.height, 1f, bounds.yMax - y);
            return new Rect(x, y, width, height);
        }
    }
}
