using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleBoardSolvability : MonoBehaviour
    {
        public event Action<BattleBoardSolvability> ValidationStarted;
        public event Action<BattleBoardSolvability> ValidationFinished;
        public event Action<BattleBoardSolvability> SolvableFound;
        public event Action<BattleBoardSolvability> SolvableFailed;
        public event Action<BattleBoardSolvability> StateChanged;

        [Header("Search")]
        [SerializeField, Min(1)] private int maxBranchChecks = 512;
        [SerializeField, Min(1)] private int maxDepth = 512;
        [SerializeField] private bool debugLogs = true;

        private BattleBoard board;

        [Header("State")]
        [SerializeField] private bool lastCheckResult;
        [SerializeField] private int lastSolvedPairs;
        [SerializeField] private int lastRemainingTiles;
        [SerializeField] private int lastCheckedTileCount;
        [SerializeField] private int lastBranchChecksUsed;

        public BattleBoard Owner => board;
        public bool IsReady => board != null;
        public bool LastCheckResult => lastCheckResult;
        public int LastSolvedPairs => lastSolvedPairs;
        public int LastRemainingTiles => lastRemainingTiles;
        public int LastCheckedTileCount => lastCheckedTileCount;
        public int LastBranchChecksUsed => lastBranchChecksUsed;
        public int MaxBranchChecks => maxBranchChecks;
        public int MaxDepth => maxDepth;

        private void Awake()
        {
            board = GetComponent<BattleBoard>();
        }

        public void Bind(BattleBoard target)
        {
            board = target;
            NotifyStateChanged();
        }

        public void SetMaxBranchChecks(int value)
        {
            maxBranchChecks = Mathf.Max(1, value);
            NotifyStateChanged();
        }

        public void SetMaxDepth(int value)
        {
            maxDepth = Mathf.Max(1, value);
            NotifyStateChanged();
        }

        public bool IsCurrentBoardSolvable()
        {
            if (!IsReady)
                return false;

            return IsSolvable(GetCurrentSlotsSnapshot(), GetCurrentBoardIdsSnapshot());
        }

        public bool IsSolvable(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<BattleTileData> buildData)
        {
            return IsSolvable(slots, ConvertToIdList(buildData));
        }

        public bool IsSolvable(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<string> tileIds)
        {
            BeginValidation(tileIds != null ? tileIds.Count : 0);

            if (slots == null || tileIds == null || slots.Count == 0 || tileIds.Count == 0)
                return FinishValidation(false, 0, 0);

            int count = Mathf.Min(slots.Count, tileIds.Count);
            if ((count & 1) != 0)
                count -= 1;

            if (count <= 0)
                return FinishValidation(false, 0, 0);

            List<SimNode> nodes = BuildNodes(slots, tileIds, count);
            if (nodes.Count <= 0 || (nodes.Count & 1) != 0)
                return FinishValidation(false, 0, nodes.Count);

            int branchBudget = Mathf.Max(1, maxBranchChecks);
            int solvedPairs = 0;

            bool solvable = Search(nodes, 0, ref branchBudget, ref solvedPairs);

            int remaining = CountRemaining(nodes);
            lastSolvedPairs = solvedPairs;
            lastRemainingTiles = remaining;
            lastBranchChecksUsed = Mathf.Max(0, maxBranchChecks - branchBudget);

            return FinishValidation(solvable, solvedPairs, remaining);
        }

        public bool TryPrepareSolvableBuild(
            IReadOnlyList<LayoutSlot> slots,
            IReadOnlyList<BattleTileData> source,
            bool repeatPairsToFillSlots,
            int seed,
            int maxAttempts,
            out List<BattleTileData> result)
        {
            result = new List<BattleTileData>();

            if (slots == null || slots.Count == 0 || source == null || source.Count == 0)
                return false;

            int safeAttempts = Mathf.Max(1, maxAttempts);
            List<BattleTileData> baseList = BuildBaseList(source, slots.Count, repeatPairsToFillSlots);
            if (baseList.Count == 0)
                return false;

            for (int attempt = 0; attempt < safeAttempts; attempt++)
            {
                List<BattleTileData> candidate = new(baseList);
                Shuffle(candidate, seed + attempt * 9973);

                if (IsSolvable(slots, candidate))
                {
                    result = candidate;
                    if (debugLogs)
                        Debug.Log($"[BattleBoardSolvability] Solvable build found on attempt {attempt + 1}/{safeAttempts}.", this);
                    return true;
                }
            }

            if (debugLogs)
                Debug.LogWarning($"[BattleBoardSolvability] No solvable build found after {safeAttempts} attempts.", this);

            return false;
        }

        public List<string> ConvertToIdList(IReadOnlyList<BattleTileData> buildData)
        {
            List<string> ids = new();
            if (buildData == null)
                return ids;

            for (int i = 0; i < buildData.Count; i++)
            {
                BattleTileData data = buildData[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                ids.Add(data.Id);
            }

            return ids;
        }

        public List<LayoutSlot> GetCurrentSlotsSnapshot()
        {
            List<LayoutSlot> result = new();
            if (!IsReady || board.Layout == null || board.Layout.Slots == null)
                return result;

            IReadOnlyList<LayoutSlot> slots = board.Layout.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                LayoutSlot s = slots[i];
                if (s == null)
                    continue;

                result.Add(new LayoutSlot { X = s.X, Y = s.Y, Z = s.Z });
            }

            return result;
        }

        public List<string> GetCurrentBoardIdsSnapshot()
        {
            List<string> result = new();
            if (!IsReady)
                return result;

            IReadOnlyList<BattleTile> tiles = board.SpawnedTiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                BattleTile tile = tiles[i];
                if (tile == null)
                    continue;

                result.Add(tile.Id);
            }

            return result;
        }

        private void BeginValidation(int checkedTileCount)
        {
            lastCheckResult = false;
            lastSolvedPairs = 0;
            lastRemainingTiles = 0;
            lastCheckedTileCount = checkedTileCount;
            lastBranchChecksUsed = 0;

            ValidationStarted?.Invoke(this);
            NotifyStateChanged();
        }

        private bool FinishValidation(bool result, int solvedPairs, int remaining)
        {
            lastCheckResult = result;
            lastSolvedPairs = solvedPairs;
            lastRemainingTiles = remaining;

            ValidationFinished?.Invoke(this);

            if (result)
                SolvableFound?.Invoke(this);
            else
                SolvableFailed?.Invoke(this);

            NotifyStateChanged();

            if (debugLogs)
                Debug.Log($"[BattleBoardSolvability] Validation result={result} solvedPairs={solvedPairs} remaining={remaining} branchUsed={lastBranchChecksUsed}", this);

            return result;
        }

        private List<SimNode> BuildNodes(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<string> tileIds, int count)
        {
            List<SimNode> nodes = new(count);

            for (int i = 0; i < count; i++)
            {
                LayoutSlot slot = slots[i];
                if (slot == null)
                    continue;

                string id = tileIds[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                nodes.Add(new SimNode(slot.X, slot.Y, slot.Z, id));
            }

            return nodes;
        }

        private bool Search(List<SimNode> nodes, int depth, ref int branchBudget, ref int solvedPairs)
        {
            int remaining = CountRemaining(nodes);
            if (remaining == 0)
                return true;

            if (depth >= maxDepth || branchBudget <= 0)
                return false;

            List<(int a, int b, int score)> pairs = GetFreePairs(nodes);
            if (pairs.Count == 0)
                return false;

            pairs.Sort((p1, p2) => p2.score.CompareTo(p1.score));

            int solvedBefore = solvedPairs;

            for (int i = 0; i < pairs.Count; i++)
            {
                if (branchBudget <= 0)
                    break;

                branchBudget--;

                var pair = pairs[i];
                SimNode na = nodes[pair.a];
                SimNode nb = nodes[pair.b];

                na.Removed = true;
                nb.Removed = true;
                solvedPairs++;

                if (Search(nodes, depth + 1, ref branchBudget, ref solvedPairs))
                    return true;

                na.Removed = false;
                nb.Removed = false;
                solvedPairs = solvedBefore;
            }

            return false;
        }

        private List<(int a, int b, int score)> GetFreePairs(List<SimNode> nodes)
        {
            List<int> freeIndices = GetFreeNodeIndices(nodes);
            List<(int a, int b, int score)> pairs = new();

            for (int i = 0; i < freeIndices.Count; i++)
            {
                int ai = freeIndices[i];
                SimNode a = nodes[ai];

                for (int j = i + 1; j < freeIndices.Count; j++)
                {
                    int bi = freeIndices[j];
                    SimNode b = nodes[bi];

                    if (!string.Equals(a.Id, b.Id, StringComparison.Ordinal))
                        continue;

                    int score = ScorePair(nodes, ai, bi);
                    pairs.Add((ai, bi, score));
                }
            }

            return pairs;
        }

        private int ScorePair(List<SimNode> nodes, int aIndex, int bIndex)
        {
            SimNode a = nodes[aIndex];
            SimNode b = nodes[bIndex];

            int score = 0;
            score += CountIdAmongRemaining(nodes, a.Id) <= 2 ? 10 : 0;
            score += a.Z == b.Z ? 2 : 0;
            score += Mathf.Abs(a.X - b.X) > 1 ? 2 : 0;
            score += Mathf.Abs(a.Y - b.Y) > 0 ? 1 : 0;
            return score;
        }

        private int CountIdAmongRemaining(List<SimNode> nodes, string id)
        {
            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].Removed && string.Equals(nodes[i].Id, id, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private List<BattleTileData> BuildBaseList(IReadOnlyList<BattleTileData> source, int slotCount, bool repeatPairsToFillSlots)
        {
            List<BattleTileData> build = new();
            List<BattleTileData> pairPool = new();

            for (int i = 0; i < source.Count; i++)
            {
                BattleTileData data = source[i];
                if (data == null || data.Prefab == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                pairPool.Add(data);
                pairPool.Add(data);
            }

            if (pairPool.Count == 0)
                return build;

            if (repeatPairsToFillSlots)
            {
                while (build.Count < slotCount)
                {
                    for (int i = 0; i < pairPool.Count && build.Count < slotCount; i++)
                        build.Add(pairPool[i]);
                }
            }
            else
            {
                build.AddRange(pairPool);
            }

            if ((build.Count & 1) != 0)
                build.RemoveAt(build.Count - 1);

            return build;
        }

        private List<int> GetFreeNodeIndices(List<SimNode> nodes)
        {
            List<int> result = new();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].Removed && IsNodeFree(nodes, i))
                    result.Add(i);
            }

            return result;
        }

        private bool IsNodeFree(List<SimNode> nodes, int index)
        {
            SimNode slot = nodes[index];

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == index)
                    continue;

                SimNode other = nodes[i];
                if (other.Removed)
                    continue;

                if (other.Z == slot.Z + 1)
                {
                    int dxTop = Mathf.Abs(other.X - slot.X);
                    int dyTop = Mathf.Abs(other.Y - slot.Y);
                    if (dxTop <= 1 && dyTop <= 1)
                        return false;
                }
            }

            bool leftBlocked = false;
            bool rightBlocked = false;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == index)
                    continue;

                SimNode other = nodes[i];
                if (other.Removed || other.Z != slot.Z)
                    continue;

                int dx = other.X - slot.X;
                int dy = Mathf.Abs(other.Y - slot.Y);

                if (dy == 0)
                {
                    if (dx < 0 && Mathf.Abs(dx) <= 1)
                        leftBlocked = true;

                    if (dx > 0 && dx <= 1)
                        rightBlocked = true;
                }

                if (leftBlocked && rightBlocked)
                    return false;
            }

            return true;
        }

        private int CountRemaining(List<SimNode> nodes)
        {
            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].Removed)
                    count++;
            }
            return count;
        }

        private void Shuffle(List<BattleTileData> list, int seed)
        {
            System.Random rng = new(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        [Serializable]
        private sealed class SimNode
        {
            public int X;
            public int Y;
            public int Z;
            public string Id;
            public bool Removed;

            public SimNode(int x, int y, int z, string id)
            {
                X = x;
                Y = y;
                Z = z;
                Id = id;
                Removed = false;
            }
        }
    }
}