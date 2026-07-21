using System.Collections.Generic;
using System.Linq;

namespace OzGame.Okey
{
    public static class MeldSolver
    {
        public static bool CanCompleteHand(IReadOnlyList<OkeyTile> hand, OkeyTile realOkey, OkeyRulesConfig config)
        {
            if (hand == null || hand.Count != 14) return false;
            var tiles = hand.Select(t => Normalize(t, realOkey)).ToList();
            return Search(tiles, realOkey, config);
        }

        public static bool IsSevenPairs(IReadOnlyList<OkeyTile> hand, OkeyTile realOkey)
        {
            if (hand == null || hand.Count != 14) return false;
            var tiles = hand.Select(t => Normalize(t, realOkey)).ToList();
            var jokers = tiles.Count(IsWild);
            var groups = tiles.Where(t => !IsWild(t)).GroupBy(Key).Select(g => g.Count()).ToList();
            var need = groups.Sum(c => c % 2);
            var pairs = groups.Sum(c => c / 2);
            if (jokers < need) return false;
            pairs += need;
            pairs += (jokers - need) / 2;
            return pairs >= 7;
        }

        public static bool IsSet(IReadOnlyList<OkeyTile> meld, OkeyTile realOkey)
        {
            if (meld == null || meld.Count < 3 || meld.Count > 4) return false;
            var tiles = meld.Select(t => Normalize(t, realOkey)).ToList();
            var natural = tiles.Where(t => !IsWild(t)).ToList();
            if (natural.Count == 0) return true;
            var number = natural[0].number;
            if (natural.Any(t => t.number != number || t.color == OkeyColor.None)) return false;
            return natural.Select(t => t.color).Distinct().Count() == natural.Count;
        }

        public static bool IsRun(IReadOnlyList<OkeyTile> meld, OkeyTile realOkey, OkeyRulesConfig config)
        {
            if (meld == null || meld.Count < 3) return false;
            var tiles = meld.Select(t => Normalize(t, realOkey)).ToList();
            var natural = tiles.Where(t => !IsWild(t)).ToList();
            if (natural.Count == 0) return true;
            var color = natural[0].color;
            if (color == OkeyColor.None || natural.Any(t => t.color != color)) return false;
            return CanFormRun(tiles, color, config);
        }

        public static List<OkeyTile> SortByColorNumber(IEnumerable<OkeyTile> tiles, OkeyTile realOkey)
        {
            return tiles.OrderByDescending(t => IsWild(Normalize(t, realOkey)))
                .ThenBy(t => Normalize(t, realOkey).color)
                .ThenBy(t => Normalize(t, realOkey).number)
                .ThenBy(t => t.copyIndex)
                .ToList();
        }

        public static List<OkeyTile> SortByPairs(IEnumerable<OkeyTile> tiles, OkeyTile realOkey)
        {
            return tiles.OrderByDescending(t => IsWild(Normalize(t, realOkey)))
                .ThenByDescending(t => tiles.Count(x => SameFace(Normalize(x, realOkey), Normalize(t, realOkey))))
                .ThenBy(t => Normalize(t, realOkey).number)
                .ThenBy(t => Normalize(t, realOkey).color)
                .ThenBy(t => t.copyIndex)
                .ToList();
        }

        public static List<OkeyTile> SortByMeldHints(IEnumerable<OkeyTile> tiles, OkeyTile realOkey)
        {
            return tiles.OrderByDescending(t => IsWild(Normalize(t, realOkey)))
                .ThenByDescending(t => NeighborScore(Normalize(t, realOkey), tiles.Select(x => Normalize(x, realOkey))))
                .ThenBy(t => Normalize(t, realOkey).color)
                .ThenBy(t => Normalize(t, realOkey).number)
                .ToList();
        }

        private static bool Search(List<OkeyTile> tiles, OkeyTile realOkey, OkeyRulesConfig config)
        {
            if (tiles.Count == 0) return true;
            tiles = tiles.OrderBy(t => IsWild(t)).ThenBy(t => t.color).ThenBy(t => t.number).ToList();
            var first = tiles[0];

            foreach (var group in CandidateMelds(tiles, first, realOkey, config))
            {
                var rest = new List<OkeyTile>(tiles);
                foreach (var tile in group) rest.Remove(tile);
                if (Search(rest, realOkey, config)) return true;
            }
            return false;
        }

        private static IEnumerable<List<OkeyTile>> CandidateMelds(List<OkeyTile> tiles, OkeyTile first, OkeyTile realOkey, OkeyRulesConfig config)
        {
            for (var size = 3; size <= 5 && size <= tiles.Count; size++)
            {
                foreach (var combo in Combos(tiles, size))
                {
                    if (!combo.Contains(first)) continue;
                    if (IsSet(combo, realOkey) || IsRun(combo, realOkey, config)) yield return combo;
                }
            }
        }

        private static bool CanFormRun(List<OkeyTile> tiles, OkeyColor color, OkeyRulesConfig config)
        {
            var len = tiles.Count;
            var numbers = tiles.Where(t => !IsWild(t)).Select(t => t.number).ToList();
            var wilds = tiles.Count(IsWild);
            var starts = config.runWrap == OkeyRunWrap.Allow12_13_1 ? Enumerable.Range(1, 13) : Enumerable.Range(1, 14 - len);

            foreach (var start in starts)
            {
                var needed = new List<int>();
                for (var i = 0; i < len; i++) needed.Add(Wrap(start + i));
                if (config.runWrap == OkeyRunWrap.NoWrap && start + len - 1 > 13) continue;

                var pool = new List<int>(numbers);
                var missing = 0;
                foreach (var n in needed)
                {
                    if (pool.Remove(n)) continue;
                    missing++;
                }
                if (pool.Count == 0 && missing <= wilds) return true;
            }
            return false;
        }

        private static int Wrap(int number) => number > 13 ? number - 13 : number;

        private static OkeyTile Normalize(OkeyTile tile, OkeyTile realOkey)
        {
            var copy = tile.Clone();
            if (copy.type == OkeyTileType.FakeJoker && realOkey != null)
            {
                copy.color = realOkey.color;
                copy.number = realOkey.number;
                copy.isRealOkey = false;
            }
            return copy;
        }

        private static bool IsWild(OkeyTile tile) => tile != null && tile.isRealOkey && tile.type == OkeyTileType.Number;
        private static string Key(OkeyTile tile) => $"{tile.color}:{tile.number}";
        private static bool SameFace(OkeyTile a, OkeyTile b) => a.type == b.type && a.color == b.color && a.number == b.number;

        private static int NeighborScore(OkeyTile tile, IEnumerable<OkeyTile> all)
        {
            if (tile == null || tile.color == OkeyColor.None) return 0;
            var list = all.ToList();
            var score = 0;
            score += list.Count(t => t.id != tile.id && t.color == tile.color && (t.number == tile.number - 1 || t.number == tile.number + 1)) * 3;
            score += list.Count(t => t.id != tile.id && t.number == tile.number && t.color != tile.color) * 2;
            score += list.Count(t => t.id != tile.id && SameFace(t, tile));
            return score;
        }

        private static IEnumerable<List<T>> Combos<T>(IReadOnlyList<T> source, int count)
        {
            var result = new List<T>();
            foreach (var combo in Combos(source, count, 0, result)) yield return combo;
        }

        private static IEnumerable<List<T>> Combos<T>(IReadOnlyList<T> source, int count, int index, List<T> result)
        {
            if (result.Count == count)
            {
                yield return new List<T>(result);
                yield break;
            }

            for (var i = index; i < source.Count; i++)
            {
                result.Add(source[i]);
                foreach (var combo in Combos(source, count, i + 1, result)) yield return combo;
                result.RemoveAt(result.Count - 1);
            }
        }
    }
}
