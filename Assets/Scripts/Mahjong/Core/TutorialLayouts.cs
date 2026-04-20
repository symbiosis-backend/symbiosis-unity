using System.Collections.Generic;

namespace MahjongGame
{
    public static class TutorialLayouts
    {
        public static List<LayoutSlot> GetStage(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Stage01_4Tiles(),
                2 => Stage02_6Tiles(),
                3 => Stage03_8Tiles(),
                4 => Stage04_10Tiles(),
                5 => Stage05_12Tiles(),
                6 => Stage06_14Tiles(),
                7 => Stage07_16Tiles(),
                8 => Stage08_18Tiles(),
                9 => Stage09_20Tiles(),
                _ => Stage10_24Tiles()
            };
        }

        public static List<LayoutSlot> Stage01_4Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-1, 0, 0), S(0, 0, 0),
                S(1, 0, 0),  S(2, 0, 0)
            };
        }

        public static List<LayoutSlot> Stage02_6Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0),
                S(1, 0, 0),  S(2, 0, 0),  S(3, 0, 0)
            };
        }

        public static List<LayoutSlot> Stage03_8Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0),
                S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0)
            };
        }

        public static List<LayoutSlot> Stage04_10Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0), S(2, 0, 0),
                S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0), S(2, 1, 0)
            };
        }

        public static List<LayoutSlot> Stage05_12Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0), S(2, 0, 0), S(3, 0, 0),
                S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0), S(2, 1, 0), S(3, 1, 0)
            };
        }

        public static List<LayoutSlot> Stage06_14Tiles()
        {
            List<LayoutSlot> list = Stage05_12Tiles();
            list.Add(S(0, -1, 0));
            list.Add(S(1, -1, 0));
            return list;
        }

        public static List<LayoutSlot> Stage07_16Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0),
                S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0),
                S(-2, 2, 0), S(-1, 2, 0), S(0, 2, 0), S(1, 2, 0),
                S(-1, 3, 0), S(0, 3, 0),
                S(-1, 1, 1), S(0, 1, 1)
            };
        }

        public static List<LayoutSlot> Stage08_18Tiles()
        {
            List<LayoutSlot> list = Stage07_16Tiles();
            list.Add(S(-3, 1, 0));
            list.Add(S(2, 1, 0));
            return list;
        }

        public static List<LayoutSlot> Stage09_20Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-3, 0, 0), S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0),
                S(-3, 1, 0), S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0),
                S(-3, 2, 0), S(-2, 2, 0), S(-1, 2, 0), S(0, 2, 0), S(1, 2, 0),
                S(-2, 3, 0), S(-1, 3, 0), S(0, 3, 0), S(-1, 1, 1), S(0, 1, 1)
            };
        }

        public static List<LayoutSlot> Stage10_24Tiles()
        {
            return new List<LayoutSlot>
            {
                S(-3, 0, 0), S(-2, 0, 0), S(-1, 0, 0), S(0, 0, 0), S(1, 0, 0), S(2, 0, 0),
                S(-3, 1, 0), S(-2, 1, 0), S(-1, 1, 0), S(0, 1, 0), S(1, 1, 0), S(2, 1, 0),
                S(-3, 2, 0), S(-2, 2, 0), S(-1, 2, 0), S(0, 2, 0), S(1, 2, 0), S(2, 2, 0),
                S(-2, 3, 0), S(-1, 3, 0), S(0, 3, 0), S(1, 3, 0),
                S(-1, 1, 1), S(0, 1, 1)
            };
        }

        private static LayoutSlot S(int x, int y, int z)
        {
            return new LayoutSlot(x, y, z);
        }
    }
}