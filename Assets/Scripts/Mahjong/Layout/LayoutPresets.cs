using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class LayoutPresets
    {
        public static List<LayoutSlot> GetTutorial()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -3, 3, 0, 0, 0);   // 7
            AddRect(list, -1, 1, 0, 0, 1);   // 3

            return list;
        }

        public static List<LayoutSlot> GetByLevel(int level)
        {
            int index = Mathf.Clamp(level, 1, 10);

            return index switch
            {
                1 => GetLevel01_Line(),
                2 => GetLevel02_Bridge(),
                3 => GetLevel03_Hill(),
                4 => GetLevel04_WideTurtle(),
                5 => GetLevel05_Arena(),
                6 => GetLevel06_Palace(),
                7 => GetLevel07_Wave(),
                8 => GetLevel08_LongHill(),
                9 => GetLevel09_Fortress(),
                _ => GetLevel10_Dragon()
            };
        }

        public static List<LayoutSlot> GetEndlessLandscapeByLevel(int level)
        {
            int index = Mathf.Clamp(level, 1, 10);

            return index switch
            {
                1 => GetEndless01_RiverLine(),
                2 => GetEndless02_WideBridge(),
                3 => GetEndless03_LongHill(),
                4 => GetEndless04_OpenTerrace(),
                5 => GetEndless05_TwinBanks(),
                6 => GetEndless06_BroadPalace(),
                7 => GetEndless07_WideWave(),
                8 => GetEndless08_LongGarden(),
                9 => GetEndless09_LandscapeFortress(),
                _ => GetEndless10_DragonRoad()
            };
        }

        public static string GetEndlessLandscapeName(int level)
        {
            int index = Mathf.Clamp(level, 1, 10);

            return index switch
            {
                1 => "Open Gate",
                2 => "Young Turtle",
                3 => "Crescent Bridge",
                4 => "Wind Terrace",
                5 => "Twin Temples",
                6 => "Lotus Terrace",
                7 => "Wave Serpent",
                8 => "Turtle Garden",
                9 => "Pagoda Bridge",
                _ => "Dragon Road"
            };
        }

        public static string GetLevelName(int level)
        {
            int index = Mathf.Clamp(level, 1, 10);

            return index switch
            {
                1 => "Line",
                2 => "Bridge",
                3 => "Hill",
                4 => "Wide Turtle",
                5 => "Arena",
                6 => "Palace",
                7 => "Wave",
                8 => "Long Hill",
                9 => "Fortress",
                _ => "Dragon"
            };
        }

        public static List<LayoutSlot> GetLevel01_Line()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -3, 2, -1, 0); // 6
            AddRowUnique(list, -4, 3, 0, 0);  // 8
            AddRowUnique(list, -3, 2, 1, 0);  // 6
            AddRowUnique(list, -2, 1, 0, 1);  // 4

            return list;
        }

        public static List<LayoutSlot> GetLevel02_Bridge()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -2, 1, -2, 0); // 4
            AddRowUnique(list, -4, 3, -1, 0); // 8
            AddRowUnique(list, -5, 4, 0, 0);  // 10
            AddRowUnique(list, -4, 3, 1, 0);  // 8
            AddRowUnique(list, -2, 1, -1, 1); // 4
            AddRowUnique(list, -1, 0, 0, 1);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel03_Hill()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -3, 2, -2, 0); // 6
            AddRowUnique(list, -4, 3, -1, 0); // 8
            AddRowUnique(list, -5, 4, 0, 0);  // 10
            AddRowUnique(list, -4, 3, 1, 0);  // 8
            AddRowUnique(list, -2, 1, -1, 1); // 4
            AddRowUnique(list, -3, 2, 0, 1);  // 6
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 1);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel04_WideTurtle()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -3, 2, -2, 0); // 6
            AddRowUnique(list, -5, 4, -1, 0); // 10
            AddRowUnique(list, -7, 6, 0, 0);  // 14
            AddRowUnique(list, -5, 4, 1, 0);  // 10
            AddRowUnique(list, -2, 1, 2, 0);  // 4
            AddRowUnique(list, -2, 1, -1, 1); // 4
            AddRowUnique(list, -4, 3, 0, 1);  // 8
            AddRowUnique(list, -2, 1, 1, 1);  // 4
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel05_Arena()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -4, 3, -2, 0); // 8
            AddRowUnique(list, -6, 5, -1, 0); // 12
            AddRowUnique(list, -7, 6, 0, 0);  // 14
            AddRowUnique(list, -5, 4, 1, 0);  // 10
            AddRowUnique(list, -4, 3, 2, 0);  // 8
            AddRowUnique(list, -3, 2, -1, 1); // 6
            AddRowUnique(list, -4, 3, 0, 1);  // 8
            AddRowUnique(list, -3, 2, 1, 1);  // 6
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel06_Palace()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -4, 3, -2, 0); // 8
            AddRowUnique(list, -6, 5, -1, 0); // 12
            AddRowUnique(list, -8, 7, 0, 0);  // 16
            AddRowUnique(list, -7, 6, 1, 0);  // 14
            AddRowUnique(list, -4, 3, 2, 0);  // 8
            AddRowUnique(list, -3, 2, -1, 1); // 6
            AddRowUnique(list, -5, 4, 0, 1);  // 10
            AddRowUnique(list, -3, 2, 1, 1);  // 6
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel07_Wave()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -5, 4, -2, 0); // 10
            AddRowUnique(list, -8, 7, -1, 0); // 16
            AddRowUnique(list, -9, 8, 0, 0);  // 18
            AddRowUnique(list, -6, 5, 1, 0);  // 12
            AddRowUnique(list, -5, 4, 2, 0);  // 10
            AddRowUnique(list, -4, 3, -1, 1); // 8
            AddRowUnique(list, -5, 4, 0, 1);  // 10
            AddRowUnique(list, -4, 3, 1, 1);  // 8
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel08_LongHill()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -4, 3, -3, 0);  // 8
            AddRowUnique(list, -7, 6, -2, 0);  // 14
            AddRowUnique(list, -9, 8, -1, 0);  // 18
            AddRowUnique(list, -9, 8, 0, 0);   // 18
            AddRowUnique(list, -7, 6, 1, 0);   // 14
            AddRowUnique(list, -4, 3, -1, 1);  // 8
            AddRowUnique(list, -7, 6, 0, 1);   // 14
            AddRowUnique(list, -4, 3, 1, 1);   // 8
            AddRowUnique(list, -2, 1, 0, 2);   // 4
            AddRowUnique(list, -1, 0, 1, 2);   // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel09_Fortress()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -5, 4, -3, 0);   // 10
            AddRowUnique(list, -8, 7, -2, 0);   // 16
            AddRowUnique(list, -10, 9, -1, 0);  // 20
            AddRowUnique(list, -10, 9, 0, 0);   // 20
            AddRowUnique(list, -8, 7, 1, 0);    // 16
            AddRowUnique(list, -5, 4, -2, 1);   // 10
            AddRowUnique(list, -8, 7, -1, 1);   // 16
            AddRowUnique(list, -5, 4, 0, 1);    // 10
            AddRowUnique(list, -2, 1, -1, 2);   // 4
            AddRowUnique(list, -1, 0, 0, 2);    // 2

            return list;
        }

        public static List<LayoutSlot> GetLevel10_Dragon()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -6, 5, -3, 0);    // 12
            AddRowUnique(list, -9, 8, -2, 0);    // 18
            AddRowUnique(list, -12, 11, -1, 0);  // 24
            AddRowUnique(list, -12, 11, 0, 0);   // 24
            AddRowUnique(list, -9, 8, 1, 0);     // 18
            AddRowUnique(list, -6, 7, -2, 1);    // 14
            AddRowUnique(list, -9, 8, -1, 1);    // 18
            AddRowUnique(list, -6, 7, 0, 1);     // 14
            AddRowUnique(list, -3, 2, -1, 2);    // 6
            AddRowUnique(list, -2, 1, 0, 2);     // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless01_RiverLine()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -4, 3, -1, 0); // 8
            AddRowUnique(list, -5, 4, 0, 0);  // 10
            AddRowUnique(list, -3, 2, 1, 0);  // 6
            AddRowUnique(list, -3, 2, 0, 1);  // 6
            AddRowUnique(list, -1, 0, 1, 1);  // 2

            return list;
        }

        public static List<LayoutSlot> GetEndless02_WideBridge()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -3, 2, -2, 0); // 6
            AddRowUnique(list, -5, 4, -1, 0); // 10
            AddRowUnique(list, -6, 5, 0, 0);  // 12
            AddRowUnique(list, -5, 4, 1, 0);  // 10
            AddRowUnique(list, -3, 2, -1, 1); // 6
            AddRowUnique(list, -2, 1, 0, 1);  // 4
            AddRowUnique(list, -2, 1, 1, 1);  // 4
            AddRowUnique(list, -1, 0, 0, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetEndless03_LongHill()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -3, 2, -2, 0); // 6
            AddRowUnique(list, -5, 4, -1, 0); // 10
            AddRowUnique(list, -7, 6, 0, 0);  // 14
            AddRowUnique(list, -5, 4, 1, 0);  // 10
            AddRowUnique(list, -2, 1, 2, 0);  // 4
            AddRowUnique(list, -2, 1, -1, 1); // 4
            AddRowUnique(list, -4, 3, 0, 1);  // 8
            AddRowUnique(list, -2, 1, 1, 1);  // 4
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetEndless04_OpenTerrace()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -4, 3, -2, 0); // 8
            AddRowUnique(list, -5, 4, -1, 0); // 10
            AddRowUnique(list, -7, 6, 0, 0);  // 14
            AddRowUnique(list, -6, 5, 1, 0);  // 12
            AddRowUnique(list, -4, 3, 2, 0);  // 8
            AddRowUnique(list, -3, 2, -1, 1); // 6
            AddRowUnique(list, -4, 3, 0, 1);  // 8
            AddRowUnique(list, -3, 2, 1, 1);  // 6
            AddRowUnique(list, -1, 0, 0, 2);  // 2
            AddRowUnique(list, -1, 0, 1, 2);  // 2

            return list;
        }

        public static List<LayoutSlot> GetEndless05_TwinBanks()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -5, 4, -2, 0); // 10
            AddRowUnique(list, -7, 6, -1, 0); // 14
            AddRowUnique(list, -9, 8, 0, 0);  // 18
            AddRowUnique(list, -7, 6, 1, 0);  // 14
            AddRowUnique(list, -5, 4, 2, 0);  // 10
            AddRowUnique(list, -4, 3, -1, 1); // 8
            AddRowUnique(list, -5, 4, 0, 1);  // 10
            AddRowUnique(list, -4, 3, 1, 1);  // 8
            AddRowUnique(list, -2, 1, -1, 2); // 4
            AddRowUnique(list, -2, 1, 0, 2);  // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless06_BroadPalace()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -5, 4, -2, 0);   // 10
            AddRowUnique(list, -8, 7, -1, 0);   // 16
            AddRowUnique(list, -11, 10, 0, 0);  // 22
            AddRowUnique(list, -10, 9, 1, 0);   // 20
            AddRowUnique(list, -6, 5, 2, 0);    // 12
            AddRowUnique(list, -5, 4, -1, 1);   // 10
            AddRowUnique(list, -6, 5, 0, 1);    // 12
            AddRowUnique(list, -5, 4, 1, 1);    // 10
            AddRowUnique(list, -2, 1, -1, 2);   // 4
            AddRowUnique(list, -2, 1, 0, 2);    // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless07_WideWave()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -5, 4, -2, 0);   // 10
            AddRowUnique(list, -9, 8, -1, 0);   // 18
            AddRowUnique(list, -12, 11, 0, 0);  // 24
            AddRowUnique(list, -11, 10, 1, 0);  // 22
            AddRowUnique(list, -6, 5, 2, 0);    // 12
            AddRowUnique(list, -5, 4, -1, 1);   // 10
            AddRowUnique(list, -8, 7, 0, 1);    // 16
            AddRowUnique(list, -5, 4, 1, 1);    // 10
            AddRowUnique(list, -3, 2, -1, 2);   // 6
            AddRowUnique(list, -2, 1, 0, 2);    // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless08_LongGarden()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -6, 5, -2, 0);    // 12
            AddRowUnique(list, -9, 8, -1, 0);    // 18
            AddRowUnique(list, -12, 11, 0, 0);   // 24
            AddRowUnique(list, -12, 11, 1, 0);   // 24
            AddRowUnique(list, -6, 5, 2, 0);     // 12
            AddRowUnique(list, -6, 5, -1, 1);    // 12
            AddRowUnique(list, -7, 6, 0, 1);     // 14
            AddRowUnique(list, -6, 5, 1, 1);     // 12
            AddRowUnique(list, -3, 2, -1, 2);    // 6
            AddRowUnique(list, -2, 1, 0, 2);     // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless09_LandscapeFortress()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -6, 5, -2, 0);    // 12
            AddRowUnique(list, -10, 9, -1, 0);   // 20
            AddRowUnique(list, -12, 11, 0, 0);   // 24
            AddRowUnique(list, -12, 11, 1, 0);   // 24
            AddRowUnique(list, -7, 6, 2, 0);     // 14
            AddRowUnique(list, -6, 5, -1, 1);    // 12
            AddRowUnique(list, -8, 7, 0, 1);     // 16
            AddRowUnique(list, -6, 5, 1, 1);     // 12
            AddRowUnique(list, -3, 2, -1, 2);    // 6
            AddRowUnique(list, -2, 1, 0, 2);     // 4

            return list;
        }

        public static List<LayoutSlot> GetEndless10_DragonRoad()
        {
            List<LayoutSlot> list = new();

            AddRowUnique(list, -6, 5, -2, 0);    // 12
            AddRowUnique(list, -10, 9, -1, 0);   // 20
            AddRowUnique(list, -12, 11, 0, 0);   // 24
            AddRowUnique(list, -12, 11, 1, 0);   // 24
            AddRowUnique(list, -7, 6, 2, 0);     // 14
            AddRowUnique(list, -6, 5, -1, 1);    // 12
            AddRowUnique(list, -8, 7, 0, 1);     // 16
            AddRowUnique(list, -6, 5, 1, 1);     // 12
            AddRowUnique(list, -3, 2, -1, 2);    // 6
            AddRowUnique(list, -2, 1, 0, 2);     // 4

            return list;
        }

        public static List<LayoutSlot> GetRandomStory()
        {
            int level = Random.Range(1, 11);
            return GetByLevel(level);
        }

        private static void AddRect(List<LayoutSlot> list, int minX, int maxX, int minY, int maxY, int z)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    list.Add(S(x, y, z));
                }
            }
        }

        private static void AddRowUnique(List<LayoutSlot> list, int minX, int maxX, int y, int z)
        {
            for (int x = minX; x <= maxX; x++)
                AddSlotUnique(list, x, y, z);
        }

        private static void AddSlotUnique(List<LayoutSlot> list, int x, int y, int z)
        {
            for (int i = 0; i < list.Count; i++)
            {
                LayoutSlot slot = list[i];
                if (slot == null)
                    continue;

                if (slot.X == x && slot.Y == y && slot.Z == z)
                    return;
            }

            list.Add(S(x, y, z));
        }

        private static LayoutSlot S(int x, int y, int z)
        {
            return new LayoutSlot
            {
                X = x,
                Y = y,
                Z = z
            };
        }
    }
}
