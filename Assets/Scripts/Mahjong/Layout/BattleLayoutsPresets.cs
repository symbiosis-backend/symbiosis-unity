using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class BattleLayoutPresets
    {
        public static List<LayoutSlot> GetByLevel(int level)
        {
            level = NormalizeLevel(level);

            return level switch
            {
                1 => Fortress16(),
                2 => Gate20(),
                _ => WideArena24()
            };
        }

        public static string GetLevelName(int level)
        {
            level = NormalizeLevel(level);

            return level switch
            {
                1 => "Fortress 16",
                2 => "Gate 20",
                _ => "Wide Arena 24"
            };
        }

        public static int GetSlotCount(int level)
        {
            return GetByLevel(level).Count;
        }

        public static List<int> GetAllLevels()
        {
            return new List<int> { 1, 2, 3 };
        }

        private static int NormalizeLevel(int level)
        {
            int v = Mathf.Abs(level);
            if (v == 0)
                v = 1;

            return ((v - 1) % 3) + 1;
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

        // 16 tiles
        // XX   XX
        // XX   XX
        // XX   XX
        // XX   XX
        private static List<LayoutSlot> Fortress16()
        {
            return new List<LayoutSlot>
            {
                P(-3,  2), P(-2,  2), P( 2,  2), P( 3,  2),
                P(-3,  1), P(-2,  1), P( 2,  1), P( 3,  1),
                P(-3,  0), P(-2,  0), P( 2,  0), P( 3,  0),
                P(-3, -1), P(-2, -1), P( 2, -1), P( 3, -1)
            };
        }

        // 20 tiles
        // XX   XX
        // XX   XX
        //  X   X
        //  X   X
        // XX   XX
        // XX   XX
        private static List<LayoutSlot> Gate20()
        {
            return new List<LayoutSlot>
            {
                P(-3,  3), P(-2,  3), P( 2,  3), P( 3,  3),
                P(-3,  2), P(-2,  2), P( 2,  2), P( 3,  2),

                P(-1,  1), P( 1,  1),
                P(-1,  0), P( 1,  0),

                P(-3, -1), P(-2, -1), P( 2, -1), P( 3, -1),
                P(-3, -2), P(-2, -2), P( 2, -2), P( 3, -2)
            };
        }

        // 24 tiles - landscape friendly
        // XX  XX  XX
        // XX  XX  XX
        //
        // XX  XX  XX
        // XX  XX  XX
        private static List<LayoutSlot> WideArena24()
        {
            return new List<LayoutSlot>
            {
                P(-5,  2), P(-4,  2),
                P(-1,  2), P( 0,  2),
                P( 4,  2), P( 5,  2),

                P(-5,  1), P(-4,  1),
                P(-1,  1), P( 0,  1),
                P( 4,  1), P( 5,  1),

                P(-5, -1), P(-4, -1),
                P(-1, -1), P( 0, -1),
                P( 4, -1), P( 5, -1),

                P(-5, -2), P(-4, -2),
                P(-1, -2), P( 0, -2),
                P( 4, -2), P( 5, -2)
            };
        }
    }
}