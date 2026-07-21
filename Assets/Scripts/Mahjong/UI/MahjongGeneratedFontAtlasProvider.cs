using UnityEngine;

namespace MahjongGame
{
    public static class MahjongGeneratedFontAtlasProvider
    {
        private const string Root = "Mahjong/Fonts/Generated/";
        private const string EnglishAtlas = Root + "Mahjong_Font_English_Atlas";
        private const string TurkishAtlas = Root + "Mahjong_Font_Turkish_Atlas";
        private const string GermanAtlas = Root + "Mahjong_Font_German_Atlas";
        private const string RussianAtlas = Root + "Mahjong_Font_Russian_Atlas";
        private const string NumbersSymbolsAtlas = Root + "Mahjong_Font_Numbers_Symbols_Atlas";

        private static Texture2D english;
        private static Texture2D turkish;
        private static Texture2D german;
        private static Texture2D russian;
        private static Texture2D numbersSymbols;

        public static Texture2D GetAlphabetAtlas(GameLanguage language)
        {
            switch (language)
            {
                case GameLanguage.Turkish:
                    return turkish != null ? turkish : turkish = Resources.Load<Texture2D>(TurkishAtlas);
                case GameLanguage.German:
                    return german != null ? german : german = Resources.Load<Texture2D>(GermanAtlas);
                case GameLanguage.Russian:
                    return russian != null ? russian : russian = Resources.Load<Texture2D>(RussianAtlas);
                default:
                    return english != null ? english : english = Resources.Load<Texture2D>(EnglishAtlas);
            }
        }

        public static Texture2D GetNumbersSymbolsAtlas()
        {
            return numbersSymbols != null ? numbersSymbols : numbersSymbols = Resources.Load<Texture2D>(NumbersSymbolsAtlas);
        }
    }
}
