using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    public enum BattleBoardSide
    {
        Player = 0,
        Opponent = 1
    }

    [DisallowMultipleComponent]
    public sealed class BattleBoard : MonoBehaviour
    {
        public event Action<BattleBoard> BuildStarted;
        public event Action<BattleBoard> BuildCompleted;
        public event Action<BattleBoard> Cleared;
        public event Action<BattleBoard> Failed;
        public event Action<BattleBoard> BoardStateChanged;
        public event Action<BattleBoard, BattleTile> TileSelected;
        public event Action<BattleBoard, BattleTile> TileSelectionRequested;
        public event Action<BattleBoard, BattleTile> TileBlockedClicked;
        public event Action<BattleBoard, BattleTile> TileRevealed;
        public event Action<BattleBoard, BattleTile, BattleTile> PairMatched;
        public event Action<BattleBoard, BattleTile, BattleTile> PairMismatched;

        [Header("Identity")]
        [SerializeField] private BattleBoardSide side = BattleBoardSide.Player;
        [SerializeField] private bool allowPlayerInput = true;
        [SerializeField] private bool interactionLocked;
        [SerializeField] private bool requireExternalSelectionApproval;

        [Header("Links")]
        [SerializeField] private BattleTileStore battleStore;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private RectTransform root;
        [SerializeField] private LayoutBuilder layout;

        [Header("Passive Modules")]
        [SerializeField] private BattleBoardRules rules;
        [SerializeField] private BattleBoardBuilder builder;

        [Header("Legacy / Optional")]
        [SerializeField] private BattleTrayUI tray;

        [Header("Build")]
        [SerializeField] private bool buildOnStart;
        [SerializeField] private bool shuffleOnBuild = true;
        [SerializeField] private bool repeatPairsToFillSlots = true;
        [SerializeField] private bool useOpenRule = true;

        [Header("Reveal Logic")]
        [SerializeField] private float compareDelay = 0.35f;
        [SerializeField] private float mismatchHideDelay = 0.75f;
        [SerializeField] private float matchedHideDelay = 0.15f;

        [Header("Fit")]
        [SerializeField] private float paddingX = 20f;
        [SerializeField] private float paddingY = 24f;
        [SerializeField] private float fitPaddingXPercent = 0.045f;
        [SerializeField] private float fitPaddingYPercent = 0.075f;
        [SerializeField] private float minBattleFitPaddingX = 18f;
        [SerializeField] private float minBattleFitPaddingY = 24f;
        [SerializeField] private float maxBattleFitPaddingX = 44f;
        [SerializeField] private float maxBattleFitPaddingY = 64f;
        [SerializeField] private float minFitScale = 0.2f;
        [SerializeField] private float maxFitScale = 1f;

        [Header("Fallback")]
        [SerializeField, Min(1)] private int fallbackRoundIndex = 1;
        [SerializeField] private int fallbackSeed = 12345;

        private readonly List<BattleTile> spawned = new();
        private readonly List<BattleTileNode> nodes = new();

        private bool clearedTriggered;
        private bool failedTriggered;
        private bool isBuilt;
        private bool isResolvingPair;

        private int roundIndex = 1;
        private int roundSeed;
        private bool hasRoundSeed;
        private IReadOnlyList<BattleTileData> customTileSource;
        private List<LayoutSlot> customSlots;

        private BattleTile firstRevealed;
        private BattleTile secondRevealed;
        private Coroutine resolveRoutine;

        public BattleBoardSide Side => side;
        public bool AllowPlayerInput => allowPlayerInput;
        public bool IsInteractionLocked => interactionLocked;
        public bool RequireExternalSelectionApproval => requireExternalSelectionApproval;
        public bool IsFinished => clearedTriggered || failedTriggered;
        public bool IsBuilt => isBuilt;
        public bool UseOpenRule => useOpenRule;
        public bool IsResolvingPair => isResolvingPair;
        public int RoundIndex => roundIndex;
        public int RoundSeed => hasRoundSeed ? roundSeed : fallbackSeed;
        public int ActiveTileCount => rules != null ? rules.CountActiveTiles() : CountActiveTilesFallback();
        public int TotalSpawnedCount => spawned.Count;

        public BattleTrayUI Tray => tray;
        public RectTransform Root => root;
        public RectTransform BoardArea => boardArea;
        public LayoutBuilder Layout => layout;
        public BattleTileStore BattleStore => battleStore;
        public BattleBoardRules Rules => rules;
        public BattleBoardBuilder Builder => builder;

        public int FallbackRoundIndex => fallbackRoundIndex;
        public bool ShuffleOnBuild => shuffleOnBuild;
        public bool RepeatPairsToFillSlots => repeatPairsToFillSlots;
        public float PaddingX => paddingX;
        public float PaddingY => paddingY;
        public float MinFitScale => minFitScale;
        public float MaxFitScale => maxFitScale;

        public IReadOnlyList<BattleTile> SpawnedTiles => spawned;
        public IReadOnlyList<BattleTileData> CustomTileSource => customTileSource;
        public List<LayoutSlot> CustomSlots => customSlots;

        public BattleTile FirstRevealedTile => firstRevealed;
        public BattleTile SecondRevealedTile => secondRevealed;

        private void Awake()
        {
            EnsureBattleStore();
            EnsureModules();
        }

        private void OnDestroy()
        {
            UnbindTiles();
        }

        private void Start()
        {
            if (!buildOnStart)
                return;

            Canvas.ForceUpdateCanvases();
            Build();
        }

        public void SetAllowPlayerInput(bool value)
        {
            if (allowPlayerInput == value)
                return;

            allowPlayerInput = value;
            NotifyBoardStateChanged();
        }

        public void SetInteractionLocked(bool value)
        {
            if (interactionLocked == value)
                return;

            interactionLocked = value;
            NotifyBoardStateChanged();
        }

        public void SetRequireExternalSelectionApproval(bool value)
        {
            if (requireExternalSelectionApproval == value)
                return;

            requireExternalSelectionApproval = value;
            NotifyBoardStateChanged();
        }

        public void SetUseOpenRule(bool value)
        {
            if (useOpenRule == value)
                return;

            useOpenRule = value;
            rules?.RefreshBlockedView();
            NotifyBoardStateChanged();
        }

        public void SetBattleStore(BattleTileStore store)
        {
            battleStore = store;
            NotifyBoardStateChanged();
        }

        public void SetRoundIndex(int value)
        {
            roundIndex = Mathf.Max(1, value);
            NotifyBoardStateChanged();
        }

        public void SetRoundData(int newRoundIndex, List<LayoutSlot> slots, int seed, IReadOnlyList<BattleTileData> tileSource = null)
        {
            roundIndex = Mathf.Max(1, newRoundIndex);
            customSlots = slots != null ? new List<LayoutSlot>(slots) : null;
            roundSeed = seed;
            hasRoundSeed = true;
            customTileSource = tileSource;
            NotifyBoardStateChanged();
        }

        public void SetCustomTileSource(IReadOnlyList<BattleTileData> tileSource)
        {
            customTileSource = tileSource;
            NotifyBoardStateChanged();
        }

        public void SetCustomSlots(List<LayoutSlot> slots)
        {
            customSlots = slots != null ? new List<LayoutSlot>(slots) : null;
            NotifyBoardStateChanged();
        }

        public void ResetRoundOverrides()
        {
            customTileSource = null;
            customSlots = null;
            hasRoundSeed = false;
            roundSeed = 0;
            NotifyBoardStateChanged();
        }

        [ContextMenu("Build")]
        public void Build()
        {
            BuildStarted?.Invoke(this);

            ResetBuildFlags();
            StopResolveRoutine();
            Clear();

            EnsureBattleStore();
            EnsureModules();

            builder.EnsureBattleStore();
            if (!builder.HasBuildLinks())
            {
                Debug.LogError($"[BattleBoard:{side}] Missing links.");
                return;
            }

            IReadOnlyList<BattleTileData> source = builder.ResolveTileSource();
            if (source == null || source.Count == 0)
            {
                Debug.LogError($"[BattleBoard:{side}] Tile source is empty.");
                return;
            }

            builder.ApplyLayoutSource();

            IReadOnlyList<LayoutSlot> slots = layout != null ? layout.Slots : null;
            if (slots == null || slots.Count == 0)
            {
                Debug.LogError($"[BattleBoard:{side}] Layout slots are empty.");
                return;
            }

            layout.SetTileSize(builder.GetTileSizeFromSource(source));

            bool prepared = builder.TryPrepareSolvableBuild(
                source,
                slots.Count,
                repeatPairsToFillSlots,
                RoundSeed
            );

            if (!prepared || builder.BuildCount == 0)
            {
                Debug.LogError($"[BattleBoard:{side}] Failed to prepare solvable build.");
                return;
            }

            builder.PrepareRoot();
            builder.SpawnTiles(slots);

            ApplySorting();
            FitAndCenterIntoBoardArea();
            rules?.RefreshBlockedView();

            isBuilt = true;
            BuildCompleted?.Invoke(this);
            NotifyBoardStateChanged();

            Debug.Log($"[BattleBoard:{side}] Build complete | Round={roundIndex} | Tiles={spawned.Count} | Seed={RoundSeed}");
        }

        public void Select(BattleTile tile)
        {
            if (!allowPlayerInput || interactionLocked)
                return;

            TrySelectTile(tile);
        }

        public void TrySelectTileProxy(BattleTile tile)
        {
            if (!CanHandleTilePointerClick(tile))
                return;

            TrySelectTile(tile);
        }

        public bool CanHandleTilePointerClick(BattleTile tile)
        {
            return allowPlayerInput &&
                   !interactionLocked &&
                   tile != null &&
                   tile.Owner == this &&
                   !IsFinished;
        }

        public bool TrySelectTile(BattleTile tile)
        {
            if (interactionLocked || tile == null || IsFinished)
                return false;

            if (isResolvingPair && !TryDismissPendingMismatchForNextSelection(tile))
                return false;

            if (!IsSelectableClosedTile(tile))
                return false;

            if (useOpenRule && rules != null && !rules.IsTileFree(tile))
                return false;

            if (requireExternalSelectionApproval)
            {
                TileSelectionRequested?.Invoke(this, tile);
                return true;
            }

            RevealTile(tile);
            return true;
        }

        public bool TryRevealServerApprovedTileBySpawnedIndex(int tileIndex, string fallbackTileId, out BattleTile revealedTile)
        {
            bool previous = requireExternalSelectionApproval;
            requireExternalSelectionApproval = false;
            bool revealed = TryRevealTileBySpawnedIndex(tileIndex, fallbackTileId, out revealedTile);
            requireExternalSelectionApproval = previous;
            NotifyBoardStateChanged();
            return revealed;
        }

        public bool ApplyServerTileIds(IReadOnlyList<string> tileIds)
        {
            if (tileIds == null || tileIds.Count == 0)
                return false;

            EnsureBattleStore();
            int count = Mathf.Min(tileIds.Count, spawned.Count);
            for (int i = 0; i < count; i++)
            {
                BattleTile tile = spawned[i];
                if (tile == null)
                    continue;

                string id = tileIds[i];
                if (battleStore != null && battleStore.TryGetTileDataById(id, out BattleTileData data))
                    tile.ApplyData(data);
                else
                    tile.SetId(id);
            }

            rules?.RefreshBlockedView();
            NotifyBoardStateChanged();
            return count > 0;
        }

        public bool TryRevealTileById(string tileId, out BattleTile revealedTile)
        {
            revealedTile = null;
            if (string.IsNullOrWhiteSpace(tileId))
                return false;

            for (int i = 0; i < spawned.Count; i++)
            {
                BattleTile tile = spawned[i];
                if (!IsSelectableClosedTile(tile))
                    continue;

                if (!string.Equals(tile.Id, tileId, StringComparison.Ordinal))
                    continue;

                if (!TrySelectTile(tile))
                    return false;

                revealedTile = tile;
                return true;
            }

            return false;
        }

        public int GetSpawnedTileIndex(BattleTile tile)
        {
            return tile != null ? spawned.IndexOf(tile) : -1;
        }

        public bool TryRevealTileBySpawnedIndex(int tileIndex, string fallbackTileId, out BattleTile revealedTile)
        {
            revealedTile = null;

            if (tileIndex >= 0 && tileIndex < spawned.Count)
            {
                BattleTile tile = spawned[tileIndex];
                if (IsSelectableClosedTile(tile) && TrySelectTile(tile))
                {
                    revealedTile = tile;
                    return true;
                }
            }

            return TryRevealTileById(fallbackTileId, out revealedTile);
        }

        public List<BattleTile> GetFreeTiles()
        {
            return rules != null ? rules.GetFreeTiles() : new List<BattleTile>();
        }

        public List<BattleTile> GetClickableClosedTiles()
        {
            return rules != null ? rules.GetClickableClosedTiles() : new List<BattleTile>();
        }

        public List<BattleTile> GetActiveTiles()
        {
            return rules != null ? rules.GetActiveTiles() : GetActiveTilesFallback();
        }

        public bool TryGetFirstKnownPair(out BattleTile first, out BattleTile second)
        {
            first = null;
            second = null;

            List<BattleTile> clickable = GetClickableClosedTiles();
            for (int i = 0; i < clickable.Count; i++)
            {
                BattleTile a = clickable[i];
                if (a == null)
                    continue;

                for (int j = i + 1; j < clickable.Count; j++)
                {
                    BattleTile b = clickable[j];
                    if (b == null)
                        continue;

                    if (string.Equals(a.Id, b.Id, StringComparison.Ordinal))
                    {
                        first = a;
                        second = b;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsTile(BattleTile tile)
        {
            return tile != null && spawned.Contains(tile);
        }

        public bool IsTileFreePublic(BattleTile tile)
        {
            return rules != null && rules.IsTileFree(tile);
        }

        public void RefreshBoard()
        {
            FitAndCenterIntoBoardArea();
            rules?.RefreshBlockedView();
            CheckClear();
            NotifyBoardStateChanged();
        }

        public void RefitIntoBoardArea()
        {
            FitAndCenterIntoBoardArea();
            rules?.RefreshBlockedView();
            NotifyBoardStateChanged();
        }

        public void ForceFail()
        {
            if (IsFinished)
                return;

            failedTriggered = true;
            Failed?.Invoke(this);
            NotifyBoardStateChanged();
        }

        public void ForceClear()
        {
            if (IsFinished)
                return;

            clearedTriggered = true;
            Cleared?.Invoke(this);
            NotifyBoardStateChanged();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            StopResolveRoutine();
            UnbindTiles();
            DestroySpawnedTiles();

            if (root != null)
            {
                root.anchoredPosition = Vector2.zero;
                root.localScale = Vector3.one;
            }

            spawned.Clear();
            nodes.Clear();

            firstRevealed = null;
            secondRevealed = null;

            clearedTriggered = false;
            failedTriggered = false;
            isBuilt = false;
            isResolvingPair = false;

            builder?.ClearBuildList();
            NotifyBoardStateChanged();
        }

        public void BindSpawnedTilePublic(BattleTile tile, LayoutSlot slot)
        {
            if (tile == null || slot == null)
                return;

            BindTile(tile);
            spawned.Add(tile);
            nodes.Add(new BattleTileNode(tile, slot));
        }

        public BattleTileNode GetNodePublic(BattleTile tile)
        {
            return GetNode(tile);
        }

        private void RevealTile(BattleTile tile)
        {
            if (tile == null)
                return;

            tile.Reveal();
            tile.SetSelected(true);

            TileSelected?.Invoke(this, tile);
            TileRevealed?.Invoke(this, tile);

            if (firstRevealed == null)
            {
                firstRevealed = tile;
                NotifyBoardStateChanged();
                return;
            }

            if (firstRevealed == tile)
            {
                NotifyBoardStateChanged();
                return;
            }

            if (secondRevealed == null)
            {
                secondRevealed = tile;
                StopResolveRoutine();
                resolveRoutine = StartCoroutine(ResolveRevealedPairRoutine());
            }

            NotifyBoardStateChanged();
        }

        private IEnumerator ResolveRevealedPairRoutine()
        {
            isResolvingPair = true;
            NotifyBoardStateChanged();

            yield return new WaitForSeconds(Mathf.Max(0.01f, compareDelay));

            BattleTile first = firstRevealed;
            BattleTile second = secondRevealed;

            if (first == null || second == null)
            {
                ResetRevealSelection();
                yield break;
            }

            bool matched = string.Equals(first.Id, second.Id, StringComparison.Ordinal);

            if (matched)
            {
                first.SetMatched();
                second.SetMatched();
                PairMatched?.Invoke(this, first, second);

                yield return new WaitForSeconds(Mathf.Max(0.01f, matchedHideDelay));

                if (first != null)
                    first.gameObject.SetActive(false);

                if (second != null)
                    second.gameObject.SetActive(false);
            }
            else
            {
                PairMismatched?.Invoke(this, first, second);

                yield return new WaitForSeconds(Mathf.Max(0.01f, mismatchHideDelay));

                HideTileToBack(first);
                HideTileToBack(second);
            }

            ResetRevealSelection();
            rules?.RefreshBlockedView();
            CheckClear();
            NotifyBoardStateChanged();
        }

        private void ResetRevealSelection()
        {
            firstRevealed = null;
            secondRevealed = null;
            isResolvingPair = false;
            resolveRoutine = null;
        }

        private bool TryDismissPendingMismatchForNextSelection(BattleTile nextTile)
        {
            if (nextTile == null || firstRevealed == null || secondRevealed == null)
                return false;

            if (nextTile == firstRevealed || nextTile == secondRevealed)
                return false;

            if (string.Equals(firstRevealed.Id, secondRevealed.Id, StringComparison.Ordinal))
                return false;

            BattleTile first = firstRevealed;
            BattleTile second = secondRevealed;

            StopResolveRoutine();
            HideTileToBack(first);
            HideTileToBack(second);
            ResetRevealSelection();
            rules?.RefreshBlockedView();
            NotifyBoardStateChanged();
            return true;
        }

        private void StopResolveRoutine()
        {
            if (resolveRoutine == null)
                return;

            StopCoroutine(resolveRoutine);
            resolveRoutine = null;
        }

        public void BindTile(BattleTile tile)
        {
            if (tile == null)
                return;

            tile.BlockedClicked -= HandleTileBlockedClicked;
            tile.BlockedClicked += HandleTileBlockedClicked;
        }

        private void UnbindTiles()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null)
                    continue;

                spawned[i].BlockedClicked -= HandleTileBlockedClicked;
            }
        }

        private void HandleTileBlockedClicked(BattleTile tile)
        {
            TileBlockedClicked?.Invoke(this, tile);
        }

        private void CheckClear()
        {
            if (IsFinished)
                return;

            for (int i = 0; i < spawned.Count; i++)
            {
                BattleTile tile = spawned[i];
                if (tile != null && tile.gameObject.activeSelf && !tile.IsMatched)
                    return;
            }

            clearedTriggered = true;
            Cleared?.Invoke(this);
            NotifyBoardStateChanged();
            Debug.Log($"[BattleBoard:{side}] Cleared");
        }

        private void ApplySorting()
        {
            nodes.Sort((a, b) =>
            {
                int z = a.Slot.Z.CompareTo(b.Slot.Z);
                if (z != 0)
                    return z;

                int y = a.Slot.Y.CompareTo(b.Slot.Y);
                if (y != 0)
                    return y;

                return a.Slot.X.CompareTo(b.Slot.X);
            });

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i]?.Tile != null)
                    nodes[i].Tile.transform.SetSiblingIndex(i);
            }

            if (root != null)
                root.SetAsLastSibling();
        }

        private void FitAndCenterIntoBoardArea()
        {
            if (boardArea == null || root == null || spawned.Count == 0)
                return;

            if (!TryGetSpawnedBounds(out Vector2 min, out Vector2 max))
                return;

            Vector2 contentSize = max - min;
            contentSize.x = Mathf.Max(1f, contentSize.x);
            contentSize.y = Mathf.Max(1f, contentSize.y);

            float adaptivePaddingX = Mathf.Clamp(boardArea.rect.width * fitPaddingXPercent, minBattleFitPaddingX, maxBattleFitPaddingX);
            float adaptivePaddingY = Mathf.Clamp(boardArea.rect.height * fitPaddingYPercent, minBattleFitPaddingY, maxBattleFitPaddingY);
            float safePaddingX = Mathf.Max(paddingX, adaptivePaddingX);
            float safePaddingY = Mathf.Max(paddingY, adaptivePaddingY);
            float availableWidth = Mathf.Max(1f, boardArea.rect.width - safePaddingX * 2f);
            float availableHeight = Mathf.Max(1f, boardArea.rect.height - safePaddingY * 2f);

            float scaleX = availableWidth / contentSize.x;
            float scaleY = availableHeight / contentSize.y;
            float fitScale = Mathf.Clamp(Mathf.Min(scaleX, scaleY), minFitScale, maxFitScale);

            Vector2 center = (min + max) * 0.5f;
            root.localScale = Vector3.one * fitScale;
            root.anchoredPosition = -center * fitScale;
        }

        private bool TryGetSpawnedBounds(out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;
            bool found = false;

            for (int i = 0; i < spawned.Count; i++)
            {
                BattleTile tile = spawned[i];
                if (tile == null || !tile.gameObject.activeSelf)
                    continue;

                RectTransform rt = tile.Rect;
                if (rt == null)
                    continue;

                Vector2 half = rt.sizeDelta * 0.5f;
                Vector2 pos = rt.anchoredPosition;
                Vector2 localMin = pos - half;
                Vector2 localMax = pos + half;

                if (!found)
                {
                    min = localMin;
                    max = localMax;
                    found = true;
                }
                else
                {
                    min = Vector2.Min(min, localMin);
                    max = Vector2.Max(max, localMax);
                }
            }

            return found;
        }

        private BattleTileNode GetNode(BattleTile tile)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                BattleTileNode node = nodes[i];
                if (node != null && node.Tile == tile)
                    return node;
            }

            return null;
        }

        private int CountActiveTilesFallback()
        {
            int count = 0;
            for (int i = 0; i < spawned.Count; i++)
            {
                BattleTile tile = spawned[i];
                if (tile != null && tile.gameObject.activeSelf && !tile.IsMatched)
                    count++;
            }

            return count;
        }

        private List<BattleTile> GetActiveTilesFallback()
        {
            List<BattleTile> result = new();
            for (int i = 0; i < spawned.Count; i++)
            {
                BattleTile tile = spawned[i];
                if (tile != null && tile.gameObject.activeSelf && !tile.IsMatched)
                    result.Add(tile);
            }

            return result;
        }

        private void HideTileToBack(BattleTile tile)
        {
            if (tile == null)
                return;

            tile.SetSelected(false);
            tile.HideToBack();
        }

        private void EnsureBattleStore()
        {
            if (battleStore == null)
                battleStore = BattleTileStore.I != null ? BattleTileStore.I : FindAnyObjectByType<BattleTileStore>();
        }

        private void EnsureModules()
        {
            if (rules == null)
                rules = GetComponent<BattleBoardRules>();
            if (rules == null)
                rules = gameObject.AddComponent<BattleBoardRules>();
            rules.Bind(this);

            if (builder == null)
                builder = GetComponent<BattleBoardBuilder>();
            if (builder == null)
                builder = gameObject.AddComponent<BattleBoardBuilder>();
            builder.Bind(this);
        }

        private void ResetBuildFlags()
        {
            clearedTriggered = false;
            failedTriggered = false;
            isBuilt = false;
            isResolvingPair = false;
        }

        private void DestroySpawnedTiles()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    DestroySafe(spawned[i].gameObject);
            }
        }

        private bool IsSelectableClosedTile(BattleTile tile)
        {
            return tile != null &&
                   tile.gameObject.activeSelf &&
                   !tile.IsMatched &&
                   !tile.IsRevealed;
        }

        private void NotifyBoardStateChanged()
        {
            BoardStateChanged?.Invoke(this);
        }

        private void DestroySafe(GameObject go)
        {
            if (go == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }

        [Serializable]
        public sealed class BattleTileNode
        {
            public BattleTile Tile;
            public LayoutSlot Slot;

            public BattleTileNode(BattleTile tile, LayoutSlot slot)
            {
                Tile = tile;
                Slot = slot;
            }
        }
    }
}
