using UnityEngine;

namespace MahjongGame
{
    [System.Serializable]
    public sealed class LayoutSlot
    {
        public int X;
        public int Y;
        public int Z;

        public LayoutSlot() { }

        public LayoutSlot(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}