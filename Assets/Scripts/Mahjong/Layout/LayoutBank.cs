using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [CreateAssetMenu(fileName = "LayoutBank", menuName = "Mahjong/Layout Bank")]
    public sealed class LayoutBank : ScriptableObject
    {
        [SerializeField] private List<LayoutData> layouts = new();

        public IReadOnlyList<LayoutData> Layouts => layouts;

        public int Count => layouts != null ? layouts.Count : 0;

        public LayoutData Get(int index)
        {
            if (layouts == null || layouts.Count == 0)
                return null;

            if (index < 0 || index >= layouts.Count)
                return null;

            return layouts[index];
        }

        public LayoutData GetRandom()
        {
            if (layouts == null || layouts.Count == 0)
                return null;

            int index = Random.Range(0, layouts.Count);
            return layouts[index];
        }
    }
}