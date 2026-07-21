using System;
using System.Globalization;

namespace MahjongGame
{
    public static class CompactNumberFormatter
    {
        private static readonly CompactUnit[] Units =
        {
            new CompactUnit(1_000_000_000_000L, "T"),
            new CompactUnit(1_000_000_000L, "B"),
            new CompactUnit(1_000_000L, "M"),
            new CompactUnit(1_000L, "K")
        };

        public static string FormatCurrency(int value)
        {
            return FormatCurrency((long)value);
        }

        public static string FormatCurrency(long value)
        {
            if (value == 0)
                return "0";

            long absolute = value < 0 ? -value : value;
            if (absolute < 10_000L)
                return value.ToString("#,0", CultureInfo.InvariantCulture);

            for (int i = 0; i < Units.Length; i++)
            {
                CompactUnit unit = Units[i];
                if (absolute < unit.Value)
                    continue;

                decimal scaled = decimal.Divide(value, unit.Value);
                decimal rounded = decimal.Round(scaled, scaled < 100m && scaled > -100m ? 1 : 0, MidpointRounding.AwayFromZero);
                string format = decimal.Truncate(rounded) == rounded ? "0" : "0.#";
                return rounded.ToString(format, CultureInfo.InvariantCulture) + unit.Suffix;
            }

            return value.ToString("#,0", CultureInfo.InvariantCulture);
        }

        private readonly struct CompactUnit
        {
            public CompactUnit(long value, string suffix)
            {
                Value = value;
                Suffix = suffix;
            }

            public long Value { get; }

            public string Suffix { get; }
        }
    }
}
