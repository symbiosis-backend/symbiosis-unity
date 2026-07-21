using System.Collections.Generic;

namespace MahjongGame
{
    public static class BattleLayoutPresets
    {
        private const int BattleSlotCount = 36;

        public static List<LayoutSlot> GetByLevel(int level)
        {
            return StandardBattle36();
        }

        public static string GetLevelName(int level)
        {
            return "Battle 9x4 36";
        }

        public static int GetSlotCount(int level)
        {
            return GetByLevel(level).Count;
        }

        public static List<int> GetAllLevels()
        {
            return new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        }

        private static LayoutSlot P(int x, int y, int z = 0)
        {
            return new LayoutSlot
            {
                X = x,
                Y = y,
                Z = z
            };
        }

        // 36 visible tiles = 18 pairs. Battle is landscape: 9 columns by 4 rows.
        // Keep this flat for readable mobile tapping; variety comes from shuffling tile identities.
        private static List<LayoutSlot> StandardBattle36()
        {
            List<LayoutSlot> slots = new(BattleSlotCount);
            for (int y = 1; y >= -2; y--)
            {
                for (int x = -4; x <= 4; x++)
                    slots.Add(P(x, y));
            }

            return slots;
        }
    }
}
