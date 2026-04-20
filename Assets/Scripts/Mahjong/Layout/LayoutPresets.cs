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

            AddRect(list, -5, 5, 0, 0, 0);   // 11
            AddRect(list, -1, 1, 0, 0, 1);   // 3

            return list;
        }

        public static List<LayoutSlot> GetLevel02_Bridge()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -7, 7, -1, 1, 0);  // 45
            AddRect(list, -2, 2, 0, 0, 1);   // 5

            return list;
        }

        public static List<LayoutSlot> GetLevel03_Hill()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -7, 7, -1, 1, 0);  // 45
            AddRect(list, -4, 4, 0, 0, 1);   // 9
            AddRect(list, -1, 1, 0, 0, 2);   // 3

            return list;
        }

        public static List<LayoutSlot> GetLevel04_WideTurtle()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -7, 7, -2, 2, 0);  // 75
            AddRect(list, -5, 5, -1, 1, 1);  // 33
            AddRect(list, -2, 2, 0, 0, 2);   // 5

            return list;
        }

        public static List<LayoutSlot> GetLevel05_Arena()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -8, 8, -2, 2, 0);  // 85
            AddRect(list, -5, 5, -1, 1, 1);  // 33
            AddRect(list, -3, 3, 0, 0, 2);   // 7
            AddRect(list, -1, 1, 0, 0, 3);   // 3

            return list;
        }

        public static List<LayoutSlot> GetLevel06_Palace()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -9, 9, -2, 2, 0);   // 95
            AddRect(list, -8, -5, -1, 1, 1);  // 12
            AddRect(list,  5,  8, -1, 1, 1);  // 12
            AddRect(list, -2,  2, -1, 1, 1);  // 15
            AddRect(list, -1,  1, 0, 0, 2);   // 3

            return list;
        }

        public static List<LayoutSlot> GetLevel07_Wave()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -9, 9, -1, 1, 0);   // 57
            AddRect(list, -7, -1, -1, -1, 1); // 7
            AddRect(list,  1,  7,  1,  1, 1); // 7
            AddRect(list, -4,  4,  0,  0, 2); // 9
            AddRect(list, -2,  2,  0,  0, 3); // 5

            return list;
        }

        public static List<LayoutSlot> GetLevel08_LongHill()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -10, 10, -1, 1, 0); // 63
            AddRect(list,  -7,  7, -1, 1, 1); // 45
            AddRect(list,  -4,  4,  0, 0, 2); // 9
            AddRect(list,  -2,  2,  0, 0, 3); // 5
            AddRect(list,   0,  0,  0, 0, 4); // 1

            return list;
        }

        public static List<LayoutSlot> GetLevel09_Fortress()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -10, 10, -2, 2, 0); // 105
            AddRect(list, -8, -5, -1, 1, 1);  // 12
            AddRect(list,  5,  8, -1, 1, 1);  // 12
            AddRect(list, -4,  4, -1, 1, 1);  // 27
            AddRect(list, -2,  2,  0, 0, 2);  // 5
            AddRect(list,  0,  0,  0, 0, 3);  // 1

            return list;
        }

        public static List<LayoutSlot> GetLevel10_Dragon()
        {
            List<LayoutSlot> list = new();

            AddRect(list, -11, 11, -2, 2, 0); // 115
            AddRect(list, -9, -5, -1, 1, 1);  // 15
            AddRect(list, -3,  3, -1, 1, 1);  // 21
            AddRect(list,  5,  9, -1, 1, 1);  // 15
            AddRect(list, -7, -5,  0, 0, 2);  // 3
            AddRect(list, -2,  2,  0, 0, 2);  // 5
            AddRect(list,  5,  7,  0, 0, 2);  // 3
            AddRect(list, -1,  1,  0, 0, 3);  // 3
            AddRect(list,  0,  0,  0, 0, 4);  // 1

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