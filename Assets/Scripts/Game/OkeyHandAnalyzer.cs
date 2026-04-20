using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OkeyGame
{
    public static class OkeyHandAnalyzer
    {
        public enum GroupType
        {
            Run = 0,
            Set = 1
        }

        public sealed class GroupResult
        {
            public GroupType Type;
            public List<OkeyTileInstance> Tiles = new List<OkeyTileInstance>();
            public bool UsesJoker;

            public override string ToString()
            {
                string typeText = Type == GroupType.Run ? "RUN" : "SET";
                string tilesText = string.Join(", ", Tiles.Select(DescribeTile));
                return $"{typeText}: {tilesText} | JokerUsed={UsesJoker}";
            }
        }

        public sealed class AnalysisResult
        {
            public List<GroupResult> Groups = new List<GroupResult>();
            public List<OkeyTileInstance> Leftovers = new List<OkeyTileInstance>();

            public bool HasAnyGroup => Groups.Count > 0;
        }

        public sealed class WinCheckResult
        {
            public bool IsWinningHand;
            public bool IsCifte;
            public List<GroupResult> WinningGroups = new List<GroupResult>();
            public List<OkeyTileInstance> Leftovers = new List<OkeyTileInstance>();
        }

        public static AnalysisResult AnalyzeWithOkey(IReadOnlyList<OkeyTileInstance> hand, OkeyTableModule table)
        {
            var result = new AnalysisResult();

            if (hand == null || hand.Count == 0 || table == null)
                return result;

            List<OkeyTileInstance> remaining = hand
                .Where(t => t != null)
                .ToList();

            bool foundSomething = true;
            while (foundSomething)
            {
                foundSomething = false;

                var run = FindFirstRunWithOkey(remaining, table);
                if (run != null)
                {
                    result.Groups.Add(run);
                    RemoveTiles(remaining, run.Tiles);
                    foundSomething = true;
                    continue;
                }

                var set = FindFirstSetWithOkey(remaining, table);
                if (set != null)
                {
                    result.Groups.Add(set);
                    RemoveTiles(remaining, set.Tiles);
                    foundSomething = true;
                }
            }

            result.Leftovers = remaining;
            return result;
        }

        public static WinCheckResult CheckWinningHand(IReadOnlyList<OkeyTileInstance> hand, OkeyTableModule table)
        {
            var result = new WinCheckResult();

            if (hand == null || table == null)
                return result;

            List<OkeyTileInstance> source = hand
                .Where(t => t != null)
                .ToList();

            if (source.Count != 14)
            {
                result.IsWinningHand = false;
                result.IsCifte = false;
                result.Leftovers = source;
                return result;
            }

            // 1. Проверка победы через пары.
            if (CheckCifteWinningHand(source, table))
            {
                result.IsWinningHand = true;
                result.IsCifte = true;
                result.WinningGroups = new List<GroupResult>();
                result.Leftovers = new List<OkeyTileInstance>();
                return result;
            }

            // 2. Обычная проверка руки.
            var groups = new List<GroupResult>();
            var working = new List<OkeyTileInstance>(source);

            bool ok = TrySolveAllGroups(working, table, groups);

            result.IsWinningHand = ok;
            result.IsCifte = false;
            result.WinningGroups = groups;

            if (!ok)
                result.Leftovers = working;

            return result;
        }

        public static bool CheckCifteWinningHand(IReadOnlyList<OkeyTileInstance> hand, OkeyTableModule table)
        {
            if (hand == null || table == null)
                return false;

            List<OkeyTileInstance> source = hand
                .Where(t => t != null)
                .ToList();

            if (source.Count != 14)
                return false;

            int wildCount = 0;
            Dictionary<string, int> normalCounts = new Dictionary<string, int>();

            for (int i = 0; i < source.Count; i++)
            {
                OkeyTileInstance tile = source[i];
                if (tile == null)
                    continue;

                if (IsWildTile(tile, table))
                {
                    wildCount++;
                    continue;
                }

                string key = $"{table.GetEffectiveColor(tile)}_{table.GetEffectiveNumber(tile)}";

                if (!normalCounts.ContainsKey(key))
                    normalCounts[key] = 0;

                normalCounts[key]++;
            }

            int pairCount = 0;
            int singleCount = 0;

            foreach (var kv in normalCounts)
            {
                pairCount += kv.Value / 2;

                if ((kv.Value % 2) == 1)
                    singleCount++;
            }

            // Сначала закрываем одиночные пары джокерами / настоящим okey.
            int singlesCovered = Mathf.Min(wildCount, singleCount);
            pairCount += singlesCovered;
            wildCount -= singlesCovered;

            // Оставшиеся wild можно составить в пары между собой.
            pairCount += wildCount / 2;

            return pairCount >= 7;
        }

        public static void PrintAnalysisToConsole(IReadOnlyList<OkeyTileInstance> hand, OkeyTableModule table)
        {
            var analysis = AnalyzeWithOkey(hand, table);

            Debug.Log($"[OkeyHandAnalyzer] Groups found: {analysis.Groups.Count}");

            for (int i = 0; i < analysis.Groups.Count; i++)
                Debug.Log($"[OkeyHandAnalyzer] {analysis.Groups[i]}");

            if (analysis.Leftovers.Count > 0)
            {
                string leftovers = string.Join(", ", analysis.Leftovers.Select(DescribeTile));
                Debug.Log($"[OkeyHandAnalyzer] Leftovers: {leftovers}");
            }
            else
            {
                Debug.Log("[OkeyHandAnalyzer] Leftovers: none");
            }
        }

        public static void PrintWinCheckToConsole(IReadOnlyList<OkeyTileInstance> hand, OkeyTableModule table)
        {
            var check = CheckWinningHand(hand, table);

            Debug.Log($"[OkeyHandAnalyzer] WIN CHECK = {check.IsWinningHand} | CIFTE = {check.IsCifte}");

            if (check.IsWinningHand)
            {
                if (check.IsCifte)
                {
                    Debug.Log("[OkeyHandAnalyzer] WIN TYPE = CIFTE");
                }
                else
                {
                    for (int i = 0; i < check.WinningGroups.Count; i++)
                        Debug.Log($"[OkeyHandAnalyzer] WIN GROUP {i + 1}: {check.WinningGroups[i]}");
                }
            }
            else
            {
                if (check.Leftovers != null && check.Leftovers.Count > 0)
                {
                    string leftovers = string.Join(", ", check.Leftovers.Select(DescribeTile));
                    Debug.Log($"[OkeyHandAnalyzer] Not winning. Leftovers: {leftovers}");
                }
                else
                {
                    Debug.Log("[OkeyHandAnalyzer] Not winning.");
                }
            }
        }

        private static bool TrySolveAllGroups(List<OkeyTileInstance> remaining, OkeyTableModule table, List<GroupResult> groups)
        {
            if (remaining.Count == 0)
                return true;

            var candidateGroups = FindAllPossibleGroups(remaining, table);
            if (candidateGroups.Count == 0)
                return false;

            for (int i = 0; i < candidateGroups.Count; i++)
            {
                var group = candidateGroups[i];

                var nextRemaining = new List<OkeyTileInstance>(remaining);
                RemoveTiles(nextRemaining, group.Tiles);

                var nextGroups = new List<GroupResult>(groups)
                {
                    group
                };

                if (TrySolveAllGroups(nextRemaining, table, nextGroups))
                {
                    groups.Clear();
                    groups.AddRange(nextGroups);

                    remaining.Clear();
                    remaining.AddRange(nextRemaining);

                    return true;
                }
            }

            return false;
        }

        private static List<GroupResult> FindAllPossibleGroups(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            var results = new List<GroupResult>();

            results.AddRange(FindAllRunsWithOkey(tiles, table));
            results.AddRange(FindAllSetsWithOkey(tiles, table));

            results = results
                .OrderByDescending(g => g.Tiles.Count)
                .ThenBy(g => g.Type)
                .ThenBy(g => MakeGroupKey(g))
                .ToList();

            return results;
        }

        private static List<GroupResult> FindAllRunsWithOkey(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            var results = new List<GroupResult>();

            var jokers = tiles.Where(t => IsWildTile(t, table)).ToList();
            var normals = tiles.Where(t => !IsWildTile(t, table)).ToList();

            var byColor = normals
                .GroupBy(t => table.GetEffectiveColor(t))
                .ToList();

            foreach (var colorGroup in byColor)
            {
                var ordered = colorGroup
                    .GroupBy(t => table.GetEffectiveNumber(t))
                    .Select(g => g.OrderBy(x => x.RuntimeId).First())
                    .OrderBy(t => table.GetEffectiveNumber(t))
                    .ThenBy(t => t.RuntimeId)
                    .ToList();

                if (ordered.Count == 0)
                    continue;

                for (int start = 0; start < ordered.Count; start++)
                {
                    List<OkeyTileInstance> run = new List<OkeyTileInstance>();
                    List<OkeyTileInstance> availableJokers = new List<OkeyTileInstance>(jokers);

                    run.Add(ordered[start]);
                    int expected = NextNumber(table.GetEffectiveNumber(ordered[start]));

                    for (int index = start + 1; index < ordered.Count; index++)
                    {
                        int currentNumber = table.GetEffectiveNumber(ordered[index]);

                        while (currentNumber != expected && availableJokers.Count > 0)
                        {
                            run.Add(availableJokers[0]);
                            availableJokers.RemoveAt(0);
                            expected = NextNumber(expected);
                        }

                        if (currentNumber == expected)
                        {
                            run.Add(ordered[index]);
                            expected = NextNumber(currentNumber);

                            if (run.Count >= 3)
                            {
                                AddIfUnique(results, new GroupResult
                                {
                                    Type = GroupType.Run,
                                    Tiles = new List<OkeyTileInstance>(run),
                                    UsesJoker = run.Any(t => IsWildTile(t, table))
                                });
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (run.Count >= 2 && availableJokers.Count > 0 && run.Count < 5)
                    {
                        run.Add(availableJokers[0]);
                        availableJokers.RemoveAt(0);

                        if (run.Count >= 3)
                        {
                            AddIfUnique(results, new GroupResult
                            {
                                Type = GroupType.Run,
                                Tiles = new List<OkeyTileInstance>(run),
                                UsesJoker = run.Any(t => IsWildTile(t, table))
                            });
                        }
                    }
                }
            }

            return results;
        }

        private static List<GroupResult> FindAllSetsWithOkey(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            var results = new List<GroupResult>();

            var jokers = tiles.Where(t => IsWildTile(t, table)).ToList();
            var normals = tiles.Where(t => !IsWildTile(t, table)).ToList();

            var byNumber = normals
                .GroupBy(t => table.GetEffectiveNumber(t))
                .ToList();

            foreach (var numberGroup in byNumber)
            {
                var distinctColors = numberGroup
                    .GroupBy(t => table.GetEffectiveColor(t))
                    .Select(g => g.OrderBy(x => x.RuntimeId).First())
                    .ToList();

                if (distinctColors.Count >= 3)
                {
                    AddIfUnique(results, new GroupResult
                    {
                        Type = GroupType.Set,
                        Tiles = distinctColors.Take(3).ToList(),
                        UsesJoker = false
                    });
                }

                if (distinctColors.Count >= 4)
                {
                    AddIfUnique(results, new GroupResult
                    {
                        Type = GroupType.Set,
                        Tiles = distinctColors.Take(4).ToList(),
                        UsesJoker = false
                    });
                }

                if (distinctColors.Count == 2 && jokers.Count >= 1)
                {
                    AddIfUnique(results, new GroupResult
                    {
                        Type = GroupType.Set,
                        Tiles = new List<OkeyTileInstance>
                        {
                            distinctColors[0],
                            distinctColors[1],
                            jokers[0]
                        },
                        UsesJoker = true
                    });
                }

                if (distinctColors.Count == 3 && jokers.Count >= 1)
                {
                    AddIfUnique(results, new GroupResult
                    {
                        Type = GroupType.Set,
                        Tiles = new List<OkeyTileInstance>
                        {
                            distinctColors[0],
                            distinctColors[1],
                            distinctColors[2],
                            jokers[0]
                        },
                        UsesJoker = true
                    });
                }

                if (distinctColors.Count == 1 && jokers.Count >= 2)
                {
                    AddIfUnique(results, new GroupResult
                    {
                        Type = GroupType.Set,
                        Tiles = new List<OkeyTileInstance>
                        {
                            distinctColors[0],
                            jokers[0],
                            jokers[1]
                        },
                        UsesJoker = true
                    });
                }
            }

            return results;
        }

        private static GroupResult FindFirstRunWithOkey(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            return FindAllRunsWithOkey(tiles, table).FirstOrDefault();
        }

        private static GroupResult FindFirstSetWithOkey(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            return FindAllSetsWithOkey(tiles, table).FirstOrDefault();
        }

        private static bool IsWildTile(OkeyTileInstance tile, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            if (table.IsFakeOkey(tile))
                return true;

            if (table.IsCurrentRoundOkey(tile))
                return true;

            return false;
        }

        private static int NextNumber(int number)
        {
            int next = number + 1;
            if (next > 13)
                next = 1;
            return next;
        }

        private static void RemoveTiles(List<OkeyTileInstance> source, List<OkeyTileInstance> toRemove)
        {
            for (int i = 0; i < toRemove.Count; i++)
                source.Remove(toRemove[i]);
        }

        private static void AddIfUnique(List<GroupResult> list, GroupResult candidate)
        {
            if (candidate == null || candidate.Tiles.Count < 3)
                return;

            string key = MakeGroupKey(candidate);
            for (int i = 0; i < list.Count; i++)
            {
                if (MakeGroupKey(list[i]) == key)
                    return;
            }

            list.Add(candidate);
        }

        private static string MakeGroupKey(GroupResult group)
        {
            var ids = group.Tiles
                .Where(t => t != null)
                .Select(t => t.RuntimeId)
                .OrderBy(id => id);

            return $"{group.Type}:{string.Join("-", ids)}";
        }

        private static string DescribeTile(OkeyTileInstance tile)
        {
            if (tile == null)
                return "NULL";

            if (tile.IsJoker || tile.Color == TileColor.Joker)
                return "SAHTE_OKEY";

            return $"{tile.Color}-{tile.Number}";
        }
    }
}