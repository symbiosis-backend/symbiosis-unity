using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class LocalizedTextStyle
    {
        private const string LocalizedFontResourcePath = "Fonts/Philosopher-Regular";

        private static TMP_FontAsset cachedTmpFont;
        private static Font cachedLegacyFont;

        public static TMP_FontAsset TmpFont => LoadTmpFont();

        public static void Apply(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = LoadTmpFont();
            if (font == null)
                return;

            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        public static void Apply(Text text)
        {
            if (text == null)
                return;

            Font font = LoadLegacyFont();
            if (font != null)
                text.font = font;
        }

        public static bool ApplyIfLocalized(TMP_Text text)
        {
            if (text == null || !GameLocalization.IsKnownLocalizedValue(text.text))
                return false;

            Apply(text);
            return true;
        }

        public static void EnsureRuntimeFallbacks(TMP_FontAsset font)
        {
            if (font == null)
                return;

            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback == null || fallback == font)
                return;

            List<TMP_FontAsset> fallbacks = font.fallbackFontAssetTable;
            if (fallbacks == null)
            {
                fallbacks = new List<TMP_FontAsset>();
                font.fallbackFontAssetTable = fallbacks;
            }

            if (!fallbacks.Contains(fallback))
                fallbacks.Add(fallback);
        }

        private static TMP_FontAsset LoadTmpFont()
        {
            if (cachedTmpFont != null)
            {
                EnsureRuntimeFallbacks(cachedTmpFont);
                return cachedTmpFont;
            }

            cachedTmpFont = Resources.Load<TMP_FontAsset>(LocalizedFontResourcePath);
            if (cachedTmpFont != null)
            {
                EnsureRuntimeFallbacks(cachedTmpFont);
                return cachedTmpFont;
            }

            Font sourceFont = LoadLegacyFont();
            if (sourceFont == null)
                return null;

            cachedTmpFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (cachedTmpFont != null)
            {
                cachedTmpFont.name = "Philosopher Localized Runtime SDF";
                cachedTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            EnsureRuntimeFallbacks(cachedTmpFont);
            return cachedTmpFont;
        }

        private static Font LoadLegacyFont()
        {
            if (cachedLegacyFont != null)
                return cachedLegacyFont;

            cachedLegacyFont = Resources.Load<Font>(LocalizedFontResourcePath);
            return cachedLegacyFont;
        }
    }
}
