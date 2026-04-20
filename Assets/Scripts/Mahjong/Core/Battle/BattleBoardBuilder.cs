using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    public enum BattleBuildMode
    {
        ConstructiveGuaranteed = 0,
        ValidateAfterShuffle = 1
    }

    [DisallowMultipleComponent]
    public sealed class BattleBoardBuilder : MonoBehaviour
    {
        public event Action<BattleBoardBuilder> BuildPrepared;
        public event Action<BattleBoardBuilder> TilesSpawned;
        public event Action<BattleBoardBuilder> StateChanged;
        public event Action<BattleBoardBuilder> ConstructiveBuildStarted;
        public event Action<BattleBoardBuilder> ConstructiveBuildFinished;
        public event Action<BattleBoardBuilder> ConstructiveBuildFailed;

        [Header("Battle Layout")]
        [SerializeField] private BattleLayoutPresetService battleLayoutPresetService;

        [Header("Build Strategy")]
        [SerializeField] private BattleBuildMode buildMode = BattleBuildMode.ConstructiveGuaranteed;
        [SerializeField, Min(1)] private int constructiveAttempts = 96;
        [SerializeField, Min(1)] private int solvabilityAttempts = 200;
        [SerializeField] private bool requireSolvableBuild = true;
        [SerializeField] private bool fallbackToSolverIfConstructiveFails = true;
        [SerializeField] private bool debugLogs = true;

        [Header("Constructive Stability")]
        [SerializeField] private bool validateConstructiveWithSolver = true;
        [SerializeField] private bool requireStepwiseStability = true;
        [SerializeField, Min(1)] private int localPairProbeLimit = 12;

        [Header("Solvability")]
        [SerializeField] private BattleBoardSolvability solvability;

        private BattleBoard board;
        private readonly List<BattleTileData> buildList = new();

        public BattleBoard Owner => board;
        public bool IsReady => board != null;
        public IReadOnlyList<BattleTileData> BuildList => buildList;
        public int BuildCount => buildList.Count;
        public BattleLayoutPresetService BattleLayoutPresetService => battleLayoutPresetService;
        public BattleBoardSolvability Solvability => solvability;
        public BattleBuildMode BuildMode => buildMode;
        public int ConstructiveAttempts => constructiveAttempts;
        public int SolvabilityAttempts => solvabilityAttempts;
        public bool RequireSolvableBuild => requireSolvableBuild;

        private void Awake()
        {
            board = GetComponent<BattleBoard>();

            if (battleLayoutPresetService == null)
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();

            if (solvability == null)
                solvability = GetComponent<BattleBoardSolvability>();
        }

        public void Bind(BattleBoard target)
        {
            board = target;

            if (battleLayoutPresetService == null)
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();

            if (solvability == null)
                solvability = GetComponent<BattleBoardSolvability>();

            if (solvability != null)
                solvability.Bind(board);

            NotifyStateChanged();
        }

        public void SetBattleLayoutPresetService(BattleLayoutPresetService service)
        {
            battleLayoutPresetService = service;
            NotifyStateChanged();
        }

        public void SetSolvability(BattleBoardSolvability value)
        {
            solvability = value;
            if (solvability != null && board != null)
                solvability.Bind(board);

            NotifyStateChanged();
        }

        public void SetBuildMode(BattleBuildMode mode)
        {
            buildMode = mode;
            NotifyStateChanged();
        }

        public void SetConstructiveAttempts(int value)
        {
            constructiveAttempts = Mathf.Max(1, value);
            NotifyStateChanged();
        }

        public void SetSolvabilityAttempts(int value)
        {
            solvabilityAttempts = Mathf.Max(1, value);
            NotifyStateChanged();
        }

        public void SetRequireSolvableBuild(bool value)
        {
            requireSolvableBuild = value;
            NotifyStateChanged();
        }

        public void EnsureBattleStore()
        {
            if (!IsReady)
                return;

            if (board.BattleStore == null)
                board.SetBattleStore(BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>());

            NotifyStateChanged();
        }

        public bool HasBuildLinks()
        {
            return IsReady &&
                   board.BattleStore != null &&
                   board.BoardArea != null &&
                   board.Root != null &&
                   board.Layout != null;
        }

        public IReadOnlyList<BattleTileData> ResolveTileSource()
        {
            if (!IsReady)
                return null;

            IReadOnlyList<BattleTileData> custom = board.CustomTileSource;
            if (custom != null && custom.Count > 0)
                return custom;

            return board.BattleStore != null
                ? board.BattleStore.GetTilesForRound(board.RoundIndex)
                : null;
        }

        public void ApplyLayoutSource()
        {
            if (!IsReady || board.Layout == null)
                return;

            List<LayoutSlot> customSlots = board.CustomSlots;
            if (customSlots != null && customSlots.Count > 0)
            {
                board.Layout.SetSlots(CloneSlots(customSlots));
                NotifyStateChanged();
                return;
            }

            int layoutLevel = board.BattleStore != null
                ? board.BattleStore.GetLayoutLevelForRound(board.RoundIndex)
                : Mathf.Max(1, board.FallbackRoundIndex);

            List<LayoutSlot> preset = ResolveBattleLayout(layoutLevel);
            if (preset != null && preset.Count > 0)
            {
                board.Layout.SetSlots(preset);
                NotifyStateChanged();
                return;
            }

            Debug.LogError($"[BattleBoardBuilder] No battle layout source for round {board.RoundIndex}.", this);
        }

        public Vector2 GetTileSizeFromSource(IReadOnlyList<BattleTileData> source)
        {
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    BattleTileData data = source[i];
                    if (data?.Prefab != null)
                        return data.Prefab.Size;
                }
            }

            return new Vector2(56f, 76f);
        }

        public void BuildTilePool(IReadOnlyList<BattleTileData> source, int slotCount, bool repeatPairsToFillSlots)
        {
            buildList.Clear();

            if (source == null || source.Count == 0)
            {
                NotifyStateChanged();
                return;
            }

            List<BattleTileData> baseList = BuildBaseList(source, slotCount, repeatPairsToFillSlots);
            buildList.AddRange(baseList);

            BuildPrepared?.Invoke(this);
            NotifyStateChanged();
        }

        public bool TryPrepareSolvableBuild(IReadOnlyList<BattleTileData> source, int slotCount, bool repeatPairsToFillSlots, int seed)
        {
            buildList.Clear();

            if (source == null || source.Count == 0 || slotCount <= 0)
            {
                NotifyStateChanged();
                return false;
            }

            IReadOnlyList<LayoutSlot> slots = board != null && board.Layout != null ? board.Layout.Slots : null;
            if (slots == null || slots.Count == 0)
            {
                LogWarning("Layout slots are missing during build preparation.");
                NotifyStateChanged();
                return false;
            }

            if (solvability == null)
                solvability = GetComponent<BattleBoardSolvability>();

            if (solvability != null && board != null)
                solvability.Bind(board);

            bool prepared = false;

            if (buildMode == BattleBuildMode.ConstructiveGuaranteed)
            {
                prepared = TryPrepareConstructiveBuild(
                    slots,
                    source,
                    slotCount,
                    repeatPairsToFillSlots,
                    seed,
                    out List<BattleTileData> constructiveResult);

                if (prepared)
                {
                    buildList.AddRange(constructiveResult);

                    if (debugLogs)
                        Debug.Log($"[BattleBoardBuilder] Constructive build prepared. Count={buildList.Count}", this);

                    BuildPrepared?.Invoke(this);
                    NotifyStateChanged();
                    return true;
                }

                ConstructiveBuildFailed?.Invoke(this);

                if (!fallbackToSolverIfConstructiveFails)
                {
                    NotifyStateChanged();
                    return false;
                }
            }

            if (solvability == null)
            {
                LogWarning("BattleBoardSolvability not found.");

                if (requireSolvableBuild)
                {
                    NotifyStateChanged();
                    return false;
                }

                BuildTilePool(source, slotCount, repeatPairsToFillSlots);
                ShuffleBuildList(seed);
                return buildList.Count > 0;
            }

            prepared = solvability.TryPrepareSolvableBuild(
                slots,
                source,
                repeatPairsToFillSlots,
                seed,
                solvabilityAttempts,
                out List<BattleTileData> solverResult);

            if (prepared && solverResult != null && solverResult.Count > 0)
            {
                buildList.AddRange(solverResult);

                if (debugLogs)
                    Debug.Log($"[BattleBoardBuilder] Solver build prepared. Count={buildList.Count}, Attempts={solvabilityAttempts}", this);

                BuildPrepared?.Invoke(this);
                NotifyStateChanged();
                return true;
            }

            if (requireSolvableBuild)
            {
                LogWarning("Failed to prepare solvable build after constructive/solver attempts.");
                NotifyStateChanged();
                return false;
            }

            BuildTilePool(source, slotCount, repeatPairsToFillSlots);
            ShuffleBuildList(seed);
            return buildList.Count > 0;
        }

        public void ShuffleBuildList(int seed)
        {
            System.Random rng = new(seed);
            for (int i = buildList.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (buildList[i], buildList[j]) = (buildList[j], buildList[i]);
            }

            NotifyStateChanged();
        }

        public void PrepareRoot()
        {
            if (!IsReady || board.Root == null || board.BoardArea == null)
                return;

            RectTransform rect = board.Root;
            rect.SetParent(board.BoardArea, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();

            NotifyStateChanged();
        }

        public void SpawnTiles(IReadOnlyList<LayoutSlot> slots)
        {
            if (!IsReady || slots == null || board.Root == null || board.Layout == null)
                return;

            int count = Mathf.Min(buildList.Count, slots.Count);
            for (int i = 0; i < count; i++)
                CreateTile(buildList[i], slots[i], i);

            TilesSpawned?.Invoke(this);
            NotifyStateChanged();
        }

        public void ClearBuildList()
        {
            if (buildList.Count == 0)
                return;

            buildList.Clear();
            NotifyStateChanged();
        }

        private bool TryPrepareConstructiveBuild(
            IReadOnlyList<LayoutSlot> slots,
            IReadOnlyList<BattleTileData> source,
            int slotCount,
            bool repeatPairsToFillSlots,
            int seed,
            out List<BattleTileData> result)
        {
            result = new List<BattleTileData>();

            if (slots == null || source == null || slotCount <= 0)
                return false;

            ConstructiveBuildStarted?.Invoke(this);

            List<BattleTileData> baseList = BuildBaseList(source, slotCount, repeatPairsToFillSlots);
            if (baseList.Count == 0)
                return false;

            int usableCount = Mathf.Min(baseList.Count, slots.Count);
            if ((usableCount & 1) != 0)
                usableCount -= 1;

            if (usableCount <= 0)
                return false;

            List<PairData> pairs = BuildPairsFromFlatList(baseList, usableCount);
            if (pairs.Count == 0)
                return false;

            for (int attempt = 0; attempt < constructiveAttempts; attempt++)
            {
                List<LayoutSlot> slotCopy = CloneSlots(slots, usableCount);
                List<PairData> pairCopy = ClonePairs(pairs);

                ShufflePairs(pairCopy, seed + attempt * 13007);

                if (!TrySortPairsForStability(pairCopy))
                    continue;

                List<PairPlacement> placements = new();
                if (!TryPlacePairsBackwards(slotCopy, pairCopy, placements))
                    continue;

                placements.Reverse();

                result.Clear();
                for (int i = 0; i < placements.Count; i++)
                {
                    result.Add(placements[i].First);
                    result.Add(placements[i].Second);
                }

                if (result.Count != usableCount)
                    continue;

                if (validateConstructiveWithSolver && solvability != null && !solvability.IsSolvable(slotCopy, result))
                    continue;

                ConstructiveBuildFinished?.Invoke(this);
                return true;
            }

            return false;
        }

        private bool TrySortPairsForStability(List<PairData> pairs)
        {
            if (pairs == null || pairs.Count == 0)
                return false;

            pairs.Sort((a, b) =>
            {
                int ac = SafeIdComplexity(a);
                int bc = SafeIdComplexity(b);
                return ac.CompareTo(bc);
            });

            return true;
        }

        private int SafeIdComplexity(PairData pair)
        {
            if (pair == null || pair.First == null || string.IsNullOrWhiteSpace(pair.First.Id))
                return int.MaxValue;

            return pair.First.Id.Length;
        }

        private bool TryPlacePairsBackwards(
            List<LayoutSlot> allSlots,
            List<PairData> pairs,
            List<PairPlacement> placements)
        {
            List<ConstructiveNode> nodes = new();
            for (int i = 0; i < allSlots.Count; i++)
            {
                LayoutSlot s = allSlots[i];
                if (s == null)
                    continue;

                nodes.Add(new ConstructiveNode(s.X, s.Y, s.Z));
            }

            for (int i = pairs.Count - 1; i >= 0; i--)
            {
                List<int> freeIndices = GetFreeIndicesForConstructive(nodes);
                if (freeIndices.Count < 2)
                    return false;

                if (!TryPickConstructivePair(nodes, freeIndices, i, pairs.Count, out int aIndex, out int bIndex))
                    return false;

                nodes[aIndex].Occupied = true;
                nodes[bIndex].Occupied = true;

                if (requireStepwiseStability && !HasFutureMoveAfterPlacement(nodes))
                {
                    nodes[aIndex].Occupied = false;
                    nodes[bIndex].Occupied = false;
                    return false;
                }

                placements.Add(new PairPlacement(
                    pairs[i].First,
                    pairs[i].Second,
                    new LayoutSlot { X = nodes[aIndex].X, Y = nodes[aIndex].Y, Z = nodes[aIndex].Z },
                    new LayoutSlot { X = nodes[bIndex].X, Y = nodes[bIndex].Y, Z = nodes[bIndex].Z }
                ));
            }

            placements.Sort((p1, p2) =>
            {
                int z = p1.FirstSlot.Z.CompareTo(p2.FirstSlot.Z);
                if (z != 0) return z;

                int y = p1.FirstSlot.Y.CompareTo(p2.FirstSlot.Y);
                if (y != 0) return y;

                return p1.FirstSlot.X.CompareTo(p2.FirstSlot.X);
            });

            return true;
        }

        private bool TryPickConstructivePair(
            List<ConstructiveNode> nodes,
            List<int> freeIndices,
            int pairIndex,
            int totalPairs,
            out int firstIndex,
            out int secondIndex)
        {
            firstIndex = -1;
            secondIndex = -1;

            int checkedCount = 0;

            for (int i = 0; i < freeIndices.Count; i++)
            {
                int a = freeIndices[i];
                for (int j = i + 1; j < freeIndices.Count; j++)
                {
                    int b = freeIndices[j];
                    if (!CanUseTogether(nodes[a], nodes[b]))
                        continue;

                    if (!IsGoodPairPlacement(nodes, a, b, pairIndex, totalPairs))
                        continue;

                    firstIndex = a;
                    secondIndex = b;
                    return true;
                }

                checkedCount++;
                if (checkedCount >= localPairProbeLimit)
                    break;
            }

            for (int i = 0; i < freeIndices.Count; i++)
            {
                int a = freeIndices[i];
                for (int j = i + 1; j < freeIndices.Count; j++)
                {
                    int b = freeIndices[j];
                    if (CanUseTogether(nodes[a], nodes[b]))
                    {
                        firstIndex = a;
                        secondIndex = b;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsGoodPairPlacement(List<ConstructiveNode> nodes, int aIndex, int bIndex, int pairIndex, int totalPairs)
        {
            ConstructiveNode a = nodes[aIndex];
            ConstructiveNode b = nodes[bIndex];
            if (a == null || b == null)
                return false;

            int stage = totalPairs - pairIndex;
            bool lateGame = stage <= 3;

            if (lateGame && a.Z == b.Z && Mathf.Abs(a.Y - b.Y) == 0 && Mathf.Abs(a.X - b.X) <= 2)
                return false;

            return true;
        }

        private bool CanUseTogether(ConstructiveNode a, ConstructiveNode b)
        {
            if (a == null || b == null)
                return false;

            if (a.Z != b.Z)
                return true;

            if (Mathf.Abs(a.Y - b.Y) > 0)
                return true;

            return Mathf.Abs(a.X - b.X) > 1;
        }

        private bool HasFutureMoveAfterPlacement(List<ConstructiveNode> nodes)
        {
            List<int> free = GetFreeIndicesForConstructive(nodes);
            return free.Count >= 2;
        }

        private List<int> GetFreeIndicesForConstructive(List<ConstructiveNode> nodes)
        {
            List<int> result = new();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].Occupied && IsConstructiveNodeFree(nodes, i))
                    result.Add(i);
            }

            return result;
        }

        private bool IsConstructiveNodeFree(List<ConstructiveNode> nodes, int index)
        {
            ConstructiveNode slot = nodes[index];

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == index)
                    continue;

                ConstructiveNode other = nodes[i];
                if (!other.Occupied)
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

                ConstructiveNode other = nodes[i];
                if (!other.Occupied || other.Z != slot.Z)
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

        private List<PairData> BuildPairsFromFlatList(List<BattleTileData> list, int usableCount)
        {
            List<PairData> result = new();

            for (int i = 0; i < usableCount - 1; i += 2)
            {
                BattleTileData a = list[i];
                BattleTileData b = list[i + 1];
                if (a == null || b == null)
                    continue;

                result.Add(new PairData(a, b));
            }

            return result;
        }

        private void ShufflePairs(List<PairData> pairs, int seed)
        {
            System.Random rng = new(seed);
            for (int i = pairs.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
            }
        }

        private List<PairData> ClonePairs(List<PairData> source)
        {
            List<PairData> copy = new();
            if (source == null)
                return copy;

            for (int i = 0; i < source.Count; i++)
                copy.Add(new PairData(source[i].First, source[i].Second));

            return copy;
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

        private List<LayoutSlot> ResolveBattleLayout(int layoutLevel)
        {
            if (battleLayoutPresetService == null)
            {
                battleLayoutPresetService = BattleLayoutPresetService.I != null
                    ? BattleLayoutPresetService.I
                    : FindAnyObjectByType<BattleLayoutPresetService>();
            }

            List<LayoutSlot> preset = null;

            if (battleLayoutPresetService != null)
                preset = battleLayoutPresetService.GetLevel(layoutLevel);

            if (preset == null || preset.Count == 0)
                preset = BattleLayoutPresets.GetByLevel(layoutLevel);

            return preset != null ? CloneSlots(preset) : null;
        }

        private List<LayoutSlot> CloneSlots(IReadOnlyList<LayoutSlot> source, int maxCount = int.MaxValue)
        {
            List<LayoutSlot> copy = new();
            if (source == null)
                return copy;

            int count = Mathf.Min(source.Count, maxCount);
            for (int i = 0; i < count; i++)
            {
                LayoutSlot s = source[i];
                if (s == null)
                    continue;

                copy.Add(new LayoutSlot
                {
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z
                });
            }

            return copy;
        }

        private void CreateTile(BattleTileData data, LayoutSlot slot, int index)
        {
            BattleTile prefab = data?.Prefab;
            if (prefab == null)
            {
                Debug.LogError($"[BattleBoardBuilder] BattleTile prefab is null for '{data?.Id}'.", this);
                return;
            }

            BattleTile tile = Instantiate(prefab, board.Root);
            if (tile == null)
            {
                Debug.LogError($"[BattleBoardBuilder] Failed to instantiate tile '{data.Id}'.", this);
                return;
            }

            tile.name = $"{board.Side}_{data.Id}_{index}";
            tile.Setup(data.Id, board);
            tile.Rect.anchoredPosition = board.Layout.GetUiPos(slot);
            tile.Rect.localScale = Vector3.one;
            tile.gameObject.SetActive(true);

            board.BindSpawnedTilePublic(tile, slot);
        }

        private void LogWarning(string message)
        {
            if (!debugLogs)
                return;

            Debug.LogWarning($"[BattleBoardBuilder] {message}", this);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        [Serializable]
        private sealed class PairData
        {
            public BattleTileData First;
            public BattleTileData Second;

            public PairData(BattleTileData first, BattleTileData second)
            {
                First = first;
                Second = second;
            }
        }

        [Serializable]
        private sealed class PairPlacement
        {
            public BattleTileData First;
            public BattleTileData Second;
            public LayoutSlot FirstSlot;
            public LayoutSlot SecondSlot;

            public PairPlacement(BattleTileData first, BattleTileData second, LayoutSlot firstSlot, LayoutSlot secondSlot)
            {
                First = first;
                Second = second;
                FirstSlot = firstSlot;
                SecondSlot = secondSlot;
            }
        }

        [Serializable]
        private sealed class ConstructiveNode
        {
            public int X;
            public int Y;
            public int Z;
            public bool Occupied;

            public ConstructiveNode(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
                Occupied = false;
            }
        }
    }
}