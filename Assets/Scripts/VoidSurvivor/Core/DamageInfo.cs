using UnityEngine;

namespace VoidSurvivor
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector2 Point;
        public readonly GameObject Source;

        public DamageInfo(float amount, Vector2 point, GameObject source)
        {
            Amount = amount;
            Point = point;
            Source = source;
        }
    }
}
