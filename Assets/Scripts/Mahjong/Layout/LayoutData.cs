using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [CreateAssetMenu(fileName = "LayoutData", menuName = "Mahjong/Layout Data")]
    public sealed class LayoutData : ScriptableObject
    {
        public List<LayoutSlot> Slots = new List<LayoutSlot>();
    }
}