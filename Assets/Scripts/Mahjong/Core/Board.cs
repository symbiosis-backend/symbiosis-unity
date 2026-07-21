using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class Board : MonoBehaviour
    {
        private const string DefaultGameplayBackgroundResourcePath = "Mahjong/Sprites/Gameplay/Mahjong_Jade_Seamless_Gameplay_Background";

        public event Action WinTriggered;
        public event Action LoseTriggered;

        [Header("Links")]
        [SerializeField] private TileStore store;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private RectTransform root;
        [SerializeField] private LayoutBuilder layout;
        [SerializeField] private TrayUI tray;
        [SerializeField] private LevelResultUI levelResultUI;

        [Header("HUD")]
        [SerializeField] private GameObject gameplayHudRoot;

        [Header("Gameplay Background")]
        [SerializeField] private Image gameplayBackgroundImage;
        [SerializeField] private Sprite endlessGameplayBackground;
        [SerializeField] private bool useDefaultGameplayBackgroundForAllMahjongLevels = true;

        [Header("Flow")]
        [SerializeField, Min(1)] private int levelIndex = 1;

        [Header("Build")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool shuffleOnBuild = true;
        [SerializeField] private bool repeatPairsToFillSlots = true;

        [Header("Rules")]
        [SerializeField] private bool useOpenRule = true;

        [Header("Fit To BArea")]
        [SerializeField] private float paddingX = 20f;
        [SerializeField] private float paddingY = 20f;
        [SerializeField] private float minFitScale = 0.2f;
        [SerializeField] private float maxFitScale = 1f;
        [SerializeField] private float maxUpscaleFitScale = 2.35f;
        [SerializeField] private float maxLandscapeFitScale = 2.45f;
        [SerializeField] private int smallLayoutSlotThreshold = 32;
        [SerializeField, Range(0.2f, 1f)] private float smallLayoutWidthFill = 0.72f;
        [SerializeField, Range(0.2f, 1f)] private float smallLayoutHeightFill = 0.82f;
        [SerializeField, Range(0.2f, 1f)] private float normalLayoutWidthFill = 0.92f;
        [SerializeField, Range(0.2f, 1f)] private float normalLayoutHeightFill = 0.86f;
        [SerializeField, Range(0.2f, 1f)] private float landscapeWidthFill = 0.98f;
        [SerializeField, Range(0.2f, 1f)] private float landscapeHeightFill = 0.96f;
        [SerializeField] private float landscapePaddingX = 10f;
        [SerializeField] private float landscapePaddingY = 10f;

        [Header("Story")]
        [SerializeField, Min(1)] private int maxStoryLevel = 10;

        [Header("Assist Hints")]
        [SerializeField, Min(1f)] private float easyAutoHintDelaySeconds = 10f;
        [SerializeField, Min(0.05f)] private float shuffleGatherDuration = 0.28f;
        [SerializeField, Min(0.05f)] private float shuffleSpreadDuration = 0.36f;
        [SerializeField, Min(0f)] private float shuffleStackHoldSeconds = 0.12f;
        [SerializeField] private Vector2 shuffleStackStep = new Vector2(4f, -3f);

        [Header("Tutorial Layout")]
        [SerializeField] private float tutorialGapX = -14f;
        [SerializeField] private float tutorialGapY = -30f;
        [SerializeField] private float storyGapX = -14f;
        [SerializeField] private float storyGapY = -30f;
        [SerializeField] private float endlessGapX = -14f;
        [SerializeField] private float endlessGapY = -30f;
        [SerializeField] private float layerShiftX = 48f;
        [SerializeField] private float layerShiftY = 66f;

        private readonly List<TileData> buildList = new();
        private readonly List<Tile> spawned = new();
        private readonly List<TileNode> nodes = new();
        private readonly HashSet<Tile> lifted = new();
        private readonly Stack<SelectedMoveRecord> selectedMoveHistory = new();
        private readonly Stack<MatchedPairRecord> matchedPairHistory = new();

        private bool levelCompleteTriggered;
        private bool levelLoseTriggered;
        private bool matchRewardProcessed;
        private Coroutine hintRoutine;
        private Coroutine easyAutoHintRoutine;
        private Coroutine shuffleRoutine;

        private int storyLevelNumber = 1;
        private int storyStageIndex = 0;
        private int endlessLevelNumber = 1;
        private Sprite activeBackground;
        private Sprite defaultGameplayBackground;
        private bool useStoryStageRuntime;
        private LevelStageContent currentStageContent;
        private MahjongGameMode currentMode = MahjongGameMode.None;
        private MahjongStoryDifficulty storyDifficulty = MahjongStoryDifficulty.Medium;
        private bool hasDefaultLayoutGap;
        private Vector2 defaultLayoutGap;
        private Vector2 lastBoardAreaSize;

        private void Awake()
        {
            if (levelResultUI == null)
                levelResultUI = FindAnyObjectByType<LevelResultUI>();

            BindTray();
        }

        private void OnEnable()
        {
            BindTray();
        }

        private void OnDestroy()
        {
            UnbindTray();
        }

        private void BindTray()
        {
            if (tray == null)
                return;

            tray.Changed -= HandleTrayChanged;
            tray.LoseTriggered -= HandleTrayLoseTriggered;

            tray.Changed += HandleTrayChanged;
            tray.LoseTriggered += HandleTrayLoseTriggered;
            tray.PairMatched += HandleTrayPairMatched;
        }

        private void UnbindTray()
        {
            if (tray == null)
                return;

            tray.Changed -= HandleTrayChanged;
            tray.LoseTriggered -= HandleTrayLoseTriggered;
            tray.PairMatched -= HandleTrayPairMatched;
        }

        private IEnumerator Start()
        {
            if (!buildOnStart)
                yield break;

            yield return null;
            Canvas.ForceUpdateCanvases();
            Build();
        }

        private void Update()
        {
            if (boardArea == null || spawned.Count == 0)
                return;

            Vector2 currentSize = boardArea.rect.size;
            if ((currentSize - lastBoardAreaSize).sqrMagnitude <= 1f)
                return;

            FitAndCenterIntoBoardArea();
        }

        [ContextMenu("Build")]
        public void Build()
        {
            levelCompleteTriggered = false;
            levelLoseTriggered = false;
            matchRewardProcessed = false;
            currentStageContent = null;
            currentMode = MahjongGameMode.None;
            storyDifficulty = MahjongStoryDifficulty.Medium;

            if (MahjongMatchService.I != null)
                MahjongMatchService.I.ClearLastProcessedResult();

            if (gameplayHudRoot != null)
                gameplayHudRoot.SetActive(false);

            if (ScoreSystem.I != null)
                ScoreSystem.I.ResetLevelScore();

            if (ComboSystem.I != null)
                ComboSystem.I.ResetCombo();

            if (levelResultUI == null)
                levelResultUI = FindAnyObjectByType<LevelResultUI>();

            if (levelResultUI != null)
                levelResultUI.ResetState();

            Clear();
            selectedMoveHistory.Clear();
            matchedPairHistory.Clear();

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (store == null || boardArea == null || root == null || layout == null)
            {
                Debug.LogError("[Board] Не назначены ссылки.");
                return;
            }

            ResolveFlowMode();

            IReadOnlyList<TileData> src = GetTileSourceForCurrentFlow();

            if (src == null || src.Count == 0)
            {
                Debug.LogError("[Board] В TileStore нет камней.");
                return;
            }

            if (useStoryStageRuntime)
            {
                bool foundStage = store.TryGetStageContent(storyLevelNumber, storyStageIndex + 1, out currentStageContent);
                if (!foundStage)
                    Debug.LogWarning($"[Board] Stage content not found | Level={storyLevelNumber} | Stage={storyStageIndex + 1}");

                activeBackground = ResolveGameplayBackground(currentStageContent != null ? currentStageContent.Background : null);
            }
            else if (currentMode == MahjongGameMode.Endless)
            {
                activeBackground = ResolveGameplayBackground(endlessGameplayBackground);
            }
            else
            {
                activeBackground = ResolveGameplayBackground(null);
            }

            ApplyGameplayBackground();

            Vector2 tileSize = GetTileSizeFromStore(src);
            layout.SetTileSize(tileSize);
            ApplyLayoutSpacingForCurrentFlow();

            ApplyLayoutByFlow();

            IReadOnlyList<LayoutSlot> slots = layout.Slots;
            if (slots == null || slots.Count == 0)
            {
                Debug.LogError("[Board] Нет слотов раскладки.");
                return;
            }

            buildList.Clear();

            List<TileData> pairPool = new();
            for (int i = 0; i < src.Count; i++)
            {
                TileData data = src[i];
                if (data == null || data.Prefab == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                pairPool.Add(data);
                pairPool.Add(data);
            }

            if (pairPool.Count == 0)
            {
                Debug.LogError("[Board] Нет валидных префабов в TileStore.");
                return;
            }

            bool solvablePairPlacementApplied = ShouldUseSolvablePairPlacement() && TryBuildSolvablePairList(src, slots, buildList, ShouldRandomizeSolvablePairs());

            if (solvablePairPlacementApplied)
            {
                Debug.Log($"[Board] Solvable pair placement applied | Mode={currentMode} | StoryLevel={storyLevelNumber} | Stage={storyStageIndex + 1} | EndlessLevel={endlessLevelNumber}");
            }
            else if (repeatPairsToFillSlots)
            {
                while (buildList.Count < slots.Count)
                {
                    for (int i = 0; i < pairPool.Count && buildList.Count < slots.Count; i++)
                        buildList.Add(pairPool[i]);
                }
            }
            else
            {
                buildList.AddRange(pairPool);
            }

            if ((buildList.Count & 1) != 0)
                buildList.RemoveAt(buildList.Count - 1);

            if (shuffleOnBuild && !solvablePairPlacementApplied)
                Shuffle(buildList);

            PrepareRoot();

            int count = Mathf.Min(buildList.Count, slots.Count);
            for (int i = 0; i < count; i++)
                CreateTile(buildList[i], slots[i], i);

            ApplySorting();
            FitAndCenterIntoBoardArea();
            RefreshBlockedView();
            RestartEasyAutoHintTimer();
            BringBackgroundBackAndTilesFront();

            if (gameplayHudRoot != null)
                gameplayHudRoot.SetActive(true);

            MahjongAssistUI.Ensure(this);

            Debug.Log($"[Board] Build complete | Mode={currentMode} | StoryLevel={storyLevelNumber} | Stage={storyStageIndex + 1} | LaunchMode={MahjongSession.LaunchMode} | Slots={slots.Count}");
        }

        public void SetStoryStage(int levelNumber, int stageIndex)
        {
            storyLevelNumber = Mathf.Max(1, levelNumber);
            storyStageIndex = Mathf.Max(0, stageIndex);
            endlessLevelNumber = 1;
            useStoryStageRuntime = true;
            currentMode = MahjongGameMode.Story;
            storyDifficulty = MahjongSession.StoryDifficulty == MahjongStoryDifficulty.Unset
                ? MahjongStoryDifficulty.Medium
                : MahjongSession.StoryDifficulty;

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (store != null)
            {
                bool foundStage = store.TryGetStageContent(storyLevelNumber, storyStageIndex + 1, out currentStageContent);
                if (!foundStage)
                    Debug.LogWarning($"[Board] SetStoryStage: stage not found | Level={storyLevelNumber} | Stage={storyStageIndex + 1}");
            }

            activeBackground = ResolveGameplayBackground(currentStageContent != null ? currentStageContent.Background : null);
            ApplyGameplayBackground();
        }

        public void SetEndlessLevel(int levelNumber)
        {
            endlessLevelNumber = Mathf.Max(1, levelNumber);
            storyLevelNumber = endlessLevelNumber;
            storyStageIndex = 0;
            useStoryStageRuntime = false;
            currentStageContent = null;
            currentMode = MahjongGameMode.Endless;
            storyDifficulty = MahjongStoryDifficulty.Medium;
            activeBackground = ResolveGameplayBackground(endlessGameplayBackground);
            ApplyGameplayBackground();
        }

        public int GetCurrentStoryLevel()
        {
            return Mathf.Max(1, storyLevelNumber);
        }

        public int GetCurrentStageNumber()
        {
            return storyStageIndex + 1;
        }

        public int GetCurrentStageCount()
        {
            if (storyLevelNumber == 1)
                return 5;

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (store == null)
                return 0;

            return store.GetStageCount(storyLevelNumber);
        }

        public bool HasNextStage()
        {
            int count = GetCurrentStageCount();
            return count > 0 && (storyStageIndex + 1) < count;
        }

        public bool TryGetNextPlayableStoryLevel(out int nextLevel)
        {
            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            nextLevel = -1;

            if (store == null)
                return false;

            nextLevel = store.GetNextLevelNumber(storyLevelNumber);
            return nextLevel > 0;
        }

        private void ResolveFlowMode()
        {
            useStoryStageRuntime = false;
            currentMode = MahjongGameMode.None;

            if (MahjongSession.LaunchMode == MahjongLaunchMode.Battle)
            {
                currentMode = MahjongGameMode.Battle;
                storyLevelNumber = Mathf.Clamp(levelIndex, 1, maxStoryLevel);
                storyStageIndex = 0;
                levelIndex = storyLevelNumber;
                useStoryStageRuntime = false;

                Debug.Log($"[Board] Battle mode | Opponent={MahjongSession.BattleOpponentName} | Rank={MahjongSession.BattleOpponentRankTier} {MahjongSession.BattleOpponentRankPoints}");
                return;
            }

            if (useStoryStageRuntime)
            {
                currentMode = MahjongGameMode.Story;
                return;
            }

            if (MahjongSession.LaunchMode == MahjongLaunchMode.Story)
            {
                currentMode = MahjongGameMode.Story;
                storyLevelNumber = Mathf.Clamp(MahjongSession.StoryLevel, 1, maxStoryLevel);
                storyStageIndex = Mathf.Max(0, MahjongSession.StoryStage - 1);
                storyDifficulty = MahjongSession.StoryDifficulty == MahjongStoryDifficulty.Unset
                    ? MahjongStoryDifficulty.Medium
                    : MahjongSession.StoryDifficulty;
                levelIndex = storyLevelNumber;
                useStoryStageRuntime = true;

                Debug.Log($"[Board] Story mode | Level={storyLevelNumber} | Stage={storyStageIndex + 1} | Difficulty={storyDifficulty}");
                return;
            }

            if (MahjongSession.LaunchMode == MahjongLaunchMode.Endless)
            {
                currentMode = MahjongGameMode.Endless;
                endlessLevelNumber = Mathf.Max(1, MahjongSession.EndlessLevel);
                storyLevelNumber = endlessLevelNumber;
                storyStageIndex = 0;
                levelIndex = ResolveEndlessLayoutLevel(endlessLevelNumber);
                useStoryStageRuntime = false;

                Debug.Log($"[Board] Endless mode | EndlessLevel={endlessLevelNumber} | LayoutLevel={levelIndex} | TileLevel={ResolveEndlessTileLevel(endlessLevelNumber)}");
                return;
            }

            currentMode = MahjongGameMode.Story;
            storyDifficulty = MahjongStoryDifficulty.Medium;
            storyLevelNumber = Mathf.Clamp(levelIndex, 1, maxStoryLevel);
            storyStageIndex = 0;
            levelIndex = storyLevelNumber;
            useStoryStageRuntime = true;
            MahjongSession.StartStory(storyLevelNumber, 1, storyDifficulty);

            Debug.Log($"[Board] Fallback story mode | Level={storyLevelNumber} | Stage=1");
        }

        private IReadOnlyList<TileData> GetTileSourceForCurrentFlow()
        {
            if (store == null)
                return null;

            if (currentMode == MahjongGameMode.Endless)
                return store.GetTilesForLevel(ResolveEndlessTileLevel(endlessLevelNumber));

            if (useStoryStageRuntime)
                return store.GetTilesForLevel(storyLevelNumber);

            return store.BaseTiles;
        }

        private int ResolveEndlessLayoutLevel(int endlessLevel)
        {
            return Mathf.Clamp(1 + ((Mathf.Max(1, endlessLevel) - 1) / 2), 1, maxStoryLevel);
        }

        private int ResolveEndlessTileLevel(int endlessLevel)
        {
            if (store == null)
                return Mathf.Clamp(endlessLevel, 1, maxStoryLevel);

            int maxLevel = store.GetMaxLevelNumber();
            if (maxLevel <= 0)
                return Mathf.Clamp(endlessLevel, 1, maxStoryLevel);

            return 1 + ((Mathf.Max(1, endlessLevel) - 1) % maxLevel);
        }

        private void ApplyGameplayBackground()
        {
            if (gameplayBackgroundImage == null)
                return;

            gameplayBackgroundImage.sprite = activeBackground;
            gameplayBackgroundImage.raycastTarget = false;

            MahjongIntroMovingBackground movingBackground = gameplayBackgroundImage.GetComponent<MahjongIntroMovingBackground>();
            if (movingBackground == null)
                movingBackground = gameplayBackgroundImage.gameObject.AddComponent<MahjongIntroMovingBackground>();

            movingBackground.RefreshFromSource();
            gameplayBackgroundImage.transform.SetAsFirstSibling();
        }

        private Sprite ResolveGameplayBackground(Sprite preferredBackground)
        {
            if (useDefaultGameplayBackgroundForAllMahjongLevels)
                return LoadDefaultGameplayBackground() ?? preferredBackground;

            return preferredBackground != null ? preferredBackground : LoadDefaultGameplayBackground();
        }

        private Sprite LoadDefaultGameplayBackground()
        {
            if (defaultGameplayBackground != null)
                return defaultGameplayBackground;

            defaultGameplayBackground = Resources.Load<Sprite>(DefaultGameplayBackgroundResourcePath);
            if (defaultGameplayBackground != null)
                return defaultGameplayBackground;

            Texture2D texture = Resources.Load<Texture2D>(DefaultGameplayBackgroundResourcePath);
            if (texture == null)
                return null;

            defaultGameplayBackground = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            return defaultGameplayBackground;
        }

        private void ApplyLayoutByFlow()
        {
            if (currentMode == MahjongGameMode.Endless)
            {
                List<LayoutSlot> endlessSlots = LayoutPresets.GetEndlessLandscapeByLevel(levelIndex);
                if (endlessSlots == null || endlessSlots.Count == 0)
                {
                    Debug.LogError($"[Board] LayoutPresets.GetEndlessLandscapeByLevel({levelIndex}) вернул пусто.");
                    return;
                }

                layout.SetSlots(endlessSlots);
                Debug.Log($"[Board] Endless landscape layout applied | Level={levelIndex} | Name={LayoutPresets.GetEndlessLandscapeName(levelIndex)} | Slots={endlessSlots.Count}");
                return;
            }

            if (!useStoryStageRuntime)
            {
                List<LayoutSlot> normalSlots = LayoutPresets.GetByLevel(levelIndex);
                if (normalSlots == null || normalSlots.Count == 0)
                {
                    Debug.LogError($"[Board] LayoutPresets.GetByLevel({levelIndex}) вернул пусто.");
                    return;
                }

                layout.SetSlots(normalSlots);
                return;
            }

            if (currentStageContent != null &&
                currentStageContent.UseCustomLayout &&
                currentStageContent.CustomSlots != null &&
                currentStageContent.CustomSlots.Count > 0)
            {
                layout.SetSlots(currentStageContent.CustomSlots);
                return;
            }

            if (storyLevelNumber == 1)
            {
                List<LayoutSlot> tutorialSlots = TutorialLayouts.GetStage(storyStageIndex + 1);
                if (tutorialSlots != null && tutorialSlots.Count > 0)
                {
                    layout.SetSlots(tutorialSlots);
                    return;
                }

                Debug.LogError($"[Board] TutorialLayouts.GetStage({storyStageIndex + 1}) вернул пусто.");
                return;
            }

            int layoutLevel = currentStageContent != null
                ? Mathf.Max(1, currentStageContent.LayoutLevel)
                : Mathf.Max(1, storyStageIndex + 1);

            List<LayoutSlot> presetSlots = LayoutPresets.GetByLevel(layoutLevel);
            if (presetSlots != null && presetSlots.Count > 0)
            {
                layout.SetSlots(presetSlots);
                return;
            }

            Debug.LogError($"[Board] LayoutPresets.GetByLevel({layoutLevel}) вернул пусто.");
        }

        private bool ShouldUseSolvablePairPlacement()
        {
            return currentMode == MahjongGameMode.Endless ||
                   currentMode == MahjongGameMode.Story;
        }

        private bool ShouldRandomizeSolvablePairs()
        {
            return currentMode == MahjongGameMode.Endless;
        }

        private void ApplyLayoutSpacingForCurrentFlow()
        {
            if (layout == null)
                return;

            if (!hasDefaultLayoutGap)
            {
                defaultLayoutGap = layout.GetGap();
                hasDefaultLayoutGap = true;
            }

            if (currentMode == MahjongGameMode.Story && storyLevelNumber == 1)
            {
                layout.SetGap(tutorialGapX, tutorialGapY);
                layout.SetLayerShift(layerShiftX, layerShiftY);
                return;
            }

            if (currentMode == MahjongGameMode.Endless)
            {
                layout.SetGap(endlessGapX, endlessGapY);
                layout.SetLayerShift(layerShiftX, layerShiftY);
                return;
            }

            if (currentMode == MahjongGameMode.Story)
            {
                layout.SetGap(storyGapX, storyGapY);
                layout.SetLayerShift(layerShiftX, layerShiftY);
                return;
            }

            layout.SetGap(defaultLayoutGap.x, defaultLayoutGap.y);
            layout.SetLayerShift(layerShiftX, layerShiftY);
        }

        private bool TryBuildSolvablePairList(IReadOnlyList<TileData> src, IReadOnlyList<LayoutSlot> slots, List<TileData> output, bool randomizePairs)
        {
            output.Clear();

            if (src == null || slots == null || slots.Count == 0 || (slots.Count & 1) != 0)
                return false;

            List<TileData> validTiles = new();
            for (int i = 0; i < src.Count; i++)
            {
                TileData data = src[i];
                if (data == null || data.Prefab == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                validTiles.Add(data);
            }

            if (validTiles.Count == 0)
                return false;

            if (randomizePairs && validTiles.Count > 1)
                Shuffle(validTiles);

            bool[] active = new bool[slots.Count];
            TileData[] bySlot = new TileData[slots.Count];

            for (int i = 0; i < active.Length; i++)
                active[i] = true;

            int remaining = slots.Count;
            int pairIndex = 0;

            while (remaining > 0)
            {
                List<int> free = CollectFreeSlotIndices(slots, active);
                if (free.Count < 2)
                    return false;

                int a = free[0];
                int b = free[free.Count - 1];
                TileData pairTile = validTiles[pairIndex % validTiles.Count];

                bySlot[a] = pairTile;
                bySlot[b] = pairTile;

                active[a] = false;
                active[b] = false;
                remaining -= 2;
                pairIndex++;
            }

            for (int i = 0; i < bySlot.Length; i++)
            {
                if (bySlot[i] == null)
                    return false;

                output.Add(bySlot[i]);
            }

            return output.Count == slots.Count;
        }

        private List<int> CollectFreeSlotIndices(IReadOnlyList<LayoutSlot> slots, bool[] active)
        {
            List<int> free = new();

            for (int i = 0; i < slots.Count; i++)
            {
                if (!active[i] || slots[i] == null)
                    continue;

                if (IsSlotFreeForPairPlacement(i, slots, active))
                    free.Add(i);
            }

            return free;
        }

        private bool IsSlotFreeForPairPlacement(int index, IReadOnlyList<LayoutSlot> slots, bool[] active)
        {
            LayoutSlot slot = slots[index];
            if (slot == null)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!active[i] || i == index || slots[i] == null)
                    continue;

                LayoutSlot other = slots[i];
                if (DoesUpperSlotCoverLowerSlot(other, slot))
                    return false;
            }

            bool leftBlocked = false;
            bool rightBlocked = false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!active[i] || i == index || slots[i] == null)
                    continue;

                LayoutSlot other = slots[i];
                if (other.Z != slot.Z)
                    continue;

                int dx = other.X - slot.X;
                int dy = Mathf.Abs(other.Y - slot.Y);

                if (dy != 0)
                    continue;

                if (dx < 0 && Mathf.Abs(dx) <= 1)
                    leftBlocked = true;

                if (dx > 0 && dx <= 1)
                    rightBlocked = true;

                if (leftBlocked && rightBlocked)
                    return false;
            }

            return true;
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

        private void PrepareRoot()
        {
            root.SetParent(boardArea, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
            root.SetAsLastSibling();
        }

        private void BringBackgroundBackAndTilesFront()
        {
            if (gameplayBackgroundImage != null)
                gameplayBackgroundImage.transform.SetAsFirstSibling();

            if (root != null)
                root.SetAsLastSibling();
        }

        private Vector2 GetTileSizeFromStore(IReadOnlyList<TileData> src)
        {
            for (int i = 0; i < src.Count; i++)
            {
                TileData data = src[i];
                if (data == null || data.Prefab == null)
                    continue;

                Tile tile = data.Prefab.GetComponent<Tile>();
                if (tile != null)
                    return tile.Size;
            }

            return new Vector2(56f, 76f);
        }

        private void CreateTile(TileData data, LayoutSlot slot, int index)
        {
            Tile tile = Instantiate(data.Prefab, root);
            tile.name = $"{data.Id}_{index}";
            tile.Setup(data.Id, this);
            tile.Rect.anchoredPosition = layout.GetUiPos(slot);
            tile.Rect.localScale = Vector3.one;
            tile.gameObject.SetActive(true);

            spawned.Add(tile);
            nodes.Add(new TileNode(tile, slot));
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

            bool smallLayout = spawned.Count <= smallLayoutSlotThreshold;
            bool landscape = boardArea.rect.width > boardArea.rect.height;
            float widthFill = smallLayout ? smallLayoutWidthFill : normalLayoutWidthFill;
            float heightFill = smallLayout ? smallLayoutHeightFill : normalLayoutHeightFill;
            float effectivePaddingX = paddingX;
            float effectivePaddingY = paddingY;

            if (landscape)
            {
                widthFill = Mathf.Max(widthFill, landscapeWidthFill, 0.98f);
                heightFill = Mathf.Max(heightFill, landscapeHeightFill, 0.96f);
                effectivePaddingX = Mathf.Min(effectivePaddingX, Mathf.Max(0f, landscapePaddingX));
                effectivePaddingY = Mathf.Min(effectivePaddingY, Mathf.Max(0f, landscapePaddingY));
            }

            float availableWidth = Mathf.Max(1f, (boardArea.rect.width - effectivePaddingX * 2f) * widthFill);
            float availableHeight = Mathf.Max(1f, (boardArea.rect.height - effectivePaddingY * 2f) * heightFill);

            float scaleX = availableWidth / contentSize.x;
            float scaleY = availableHeight / contentSize.y;

            float fitScale = Mathf.Min(scaleX, scaleY);
            float upperScale = smallLayout ? maxUpscaleFitScale : maxFitScale;
            if (landscape)
                upperScale = Mathf.Max(upperScale, maxLandscapeFitScale, 2.45f);

            upperScale = Mathf.Max(maxFitScale, upperScale);
            fitScale = Mathf.Clamp(fitScale, minFitScale, upperScale);

            Vector2 center = (min + max) * 0.5f;

            root.localScale = Vector3.one * fitScale;
            root.anchoredPosition = -center * fitScale;
            lastBoardAreaSize = boardArea.rect.size;
        }

        private bool TryGetSpawnedBounds(out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;
            bool found = false;

            for (int i = 0; i < spawned.Count; i++)
            {
                Tile t = spawned[i];
                if (t == null || !t.gameObject.activeSelf)
                    continue;

                RectTransform rt = t.Rect;
                if (rt == null)
                    continue;

                Vector2 size = rt.sizeDelta;
                Vector2 pos = rt.anchoredPosition;

                Vector2 localMin = pos - size * 0.5f;
                Vector2 localMax = pos + size * 0.5f;

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

        public void Select(Tile tile)
        {
            if (tile == null || tray == null || tray.IsBusy || shuffleRoutine != null || lifted.Contains(tile) || levelCompleteTriggered || levelLoseTriggered)
                return;

            if (useOpenRule && !IsTileFree(tile))
                return;

            if (!tray.TryAdd(tile))
                return;

            TileNode node = GetNode(tile);
            if (node != null && node.Slot != null)
                selectedMoveHistory.Push(new SelectedMoveRecord(tile, CloneSlot(node.Slot)));

            ClearAssistHints();
            lifted.Add(tile);
            tile.SetSelected(false);
            tile.SetBlocked(false);

            RefreshBlockedView();
            CheckWin();
            RestartEasyAutoHintTimer();
        }

        public bool TryShowHintPair()
        {
            if (levelCompleteTriggered || levelLoseTriggered || shuffleRoutine != null)
                return false;

            if (!TryFindFreePair(out Tile first, out Tile second))
                return false;

            if (hintRoutine != null)
                StopCoroutine(hintRoutine);

            hintRoutine = StartCoroutine(ShowHintPairRoutine(first, second));
            return true;
        }

        public bool TryShuffleActiveTiles()
        {
            if (levelCompleteTriggered || levelLoseTriggered || shuffleRoutine != null)
                return false;

            ClearAssistHints();

            List<Tile> activeTiles = GetActiveBoardTiles();
            if (activeTiles.Count < 2)
                return false;

            List<TileData> values = new();
            for (int i = 0; i < activeTiles.Count; i++)
            {
                TileData data = FindTileDataForId(activeTiles[i].Id);
                if (data != null && data.Prefab != null)
                    values.Add(data);
            }

            if (values.Count != activeTiles.Count)
                return false;

            Shuffle(values);

            StopEasyAutoHintTimer();
            shuffleRoutine = StartCoroutine(ShuffleActiveTilesRoutine(activeTiles, values));
            return true;
        }

        private IEnumerator ShuffleActiveTilesRoutine(List<Tile> activeTiles, List<TileData> values)
        {
            int count = Mathf.Min(activeTiles.Count, values.Count);
            Vector2[] startPositions = new Vector2[count];
            Vector3[] startScales = new Vector3[count];
            Quaternion[] startRotations = new Quaternion[count];
            Vector2[] stackPositions = new Vector2[count];
            bool[] valid = new bool[count];

            Vector2 stackCenter = Vector2.zero;
            int validCount = 0;

            for (int i = 0; i < count; i++)
            {
                Tile tile = activeTiles[i];
                RectTransform rect = tile != null ? tile.Rect : null;
                if (rect == null || values[i] == null || values[i].Prefab == null)
                    continue;

                valid[i] = true;
                startPositions[i] = rect.anchoredPosition;
                startScales[i] = rect.localScale;
                startRotations[i] = rect.localRotation;
                stackCenter += startPositions[i];
                validCount++;
            }

            if (validCount == 0)
            {
                shuffleRoutine = null;
                RestartEasyAutoHintTimer();
                yield break;
            }

            stackCenter /= validCount;

            int stackIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (!valid[i])
                    continue;

                float centeredIndex = stackIndex - (validCount - 1) * 0.5f;
                stackPositions[i] = stackCenter + shuffleStackStep * centeredIndex;
                activeTiles[i].transform.SetAsLastSibling();
                stackIndex++;
            }

            float gatherDuration = Mathf.Max(0.05f, shuffleGatherDuration);
            for (float elapsed = 0f; elapsed < gatherDuration; elapsed += Time.deltaTime)
            {
                float t = EaseInOut(elapsed / gatherDuration);
                for (int i = 0; i < count; i++)
                {
                    if (!valid[i] || activeTiles[i] == null || activeTiles[i].Rect == null)
                        continue;

                    RectTransform rect = activeTiles[i].Rect;
                    rect.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], stackPositions[i], t);
                    rect.localScale = Vector3.LerpUnclamped(startScales[i], Vector3.one * 0.92f, t);
                    rect.localRotation = Quaternion.LerpUnclamped(startRotations[i], Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * 3.5f), t);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (!valid[i] || activeTiles[i] == null || activeTiles[i].Rect == null)
                    continue;

                RectTransform rect = activeTiles[i].Rect;
                rect.anchoredPosition = stackPositions[i];
                rect.localScale = Vector3.one * 0.92f;
                rect.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * 3.5f);
                activeTiles[i].CopyAppearanceFrom(values[i].Prefab, values[i].Id);
                if (layout != null)
                    activeTiles[i].SetRuntimeSize(layout.TileSize);
            }

            if (shuffleStackHoldSeconds > 0f)
                yield return new WaitForSeconds(shuffleStackHoldSeconds);

            float spreadDuration = Mathf.Max(0.05f, shuffleSpreadDuration);
            for (float elapsed = 0f; elapsed < spreadDuration; elapsed += Time.deltaTime)
            {
                float t = EaseInOut(elapsed / spreadDuration);
                for (int i = 0; i < count; i++)
                {
                    if (!valid[i] || activeTiles[i] == null || activeTiles[i].Rect == null)
                        continue;

                    RectTransform rect = activeTiles[i].Rect;
                    rect.anchoredPosition = Vector2.LerpUnclamped(stackPositions[i], startPositions[i], t);
                    rect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, startScales[i], t);
                    rect.localRotation = Quaternion.LerpUnclamped(Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * 3.5f), startRotations[i], t);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (!valid[i] || activeTiles[i] == null || activeTiles[i].Rect == null)
                    continue;

                RectTransform rect = activeTiles[i].Rect;
                rect.anchoredPosition = startPositions[i];
                rect.localScale = startScales[i];
                rect.localRotation = startRotations[i];
            }

            shuffleRoutine = null;
            ApplySorting();
            RefreshBlockedView();
            RestartEasyAutoHintTimer();
        }

        public bool TryUndoLastMove()
        {
            if (levelCompleteTriggered || levelLoseTriggered || tray == null || shuffleRoutine != null)
                return false;

            ClearAssistHints();

            if (tray.IsResolvingMatches)
                return false;

            if (tray.TryPopLastTile(out Tile trayTile) && RestoreSelectedTile(trayTile))
                return true;

            while (matchedPairHistory.Count > 0)
            {
                MatchedPairRecord record = matchedPairHistory.Pop();
                if (RestoreMatchedPair(record))
                    return true;
            }

            return false;
        }

        public bool CanRescueAfterLoseUndo()
        {
            if (!levelLoseTriggered || levelCompleteTriggered || tray == null)
                return false;

            if (currentMode == MahjongGameMode.Battle)
                return false;

            if (currentMode == MahjongGameMode.Story && storyDifficulty == MahjongStoryDifficulty.Hardcore)
                return false;

            return tray.Count > 0 || matchedPairHistory.Count > 0;
        }

        public bool TryRescueAfterLoseUndo()
        {
            if (!CanRescueAfterLoseUndo())
                return false;

            levelLoseTriggered = false;
            tray.ClearLoseStateForRescue();

            bool restored = TryUndoLastMove();
            if (!restored)
            {
                levelLoseTriggered = true;
                return false;
            }

            RefreshBlockedView();
            RestartEasyAutoHintTimer();
            return true;
        }

        private bool TryFindFreePair(out Tile first, out Tile second)
        {
            first = null;
            second = null;

            List<Tile> freeTiles = GetFreeBoardTiles();
            for (int i = 0; i < freeTiles.Count; i++)
            {
                Tile a = freeTiles[i];
                if (a == null)
                    continue;

                for (int j = i + 1; j < freeTiles.Count; j++)
                {
                    Tile b = freeTiles[j];
                    if (b == null || a.Id != b.Id)
                        continue;

                    first = a;
                    second = b;
                    return true;
                }
            }

            return false;
        }

        private IEnumerator ShowHintPairRoutine(Tile first, Tile second)
        {
            if (first == null || second == null)
                yield break;

            first.SetAssistHint(true);
            second.SetAssistHint(true);
            first.PlayAssistHintMotion();
            second.PlayAssistHintMotion();

            yield return new WaitForSeconds(1.25f);

            if (first != null)
                first.SetAssistHint(false);

            if (second != null)
                second.SetAssistHint(false);

            hintRoutine = null;
        }

        private void ClearAssistHints()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    spawned[i].SetAssistHint(false);
            }
        }

        private void RestartEasyAutoHintTimer()
        {
            StopEasyAutoHintTimer();

            if (!ShouldUseEasyAutoHint() || !gameObject.activeInHierarchy)
                return;

            easyAutoHintRoutine = StartCoroutine(EasyAutoHintRoutine());
        }

        private void StopEasyAutoHintTimer()
        {
            if (easyAutoHintRoutine == null)
                return;

            StopCoroutine(easyAutoHintRoutine);
            easyAutoHintRoutine = null;
        }

        private IEnumerator EasyAutoHintRoutine()
        {
            yield return new WaitForSeconds(easyAutoHintDelaySeconds);

            if (ShouldUseEasyAutoHint())
                TryShowHintPair();

            easyAutoHintRoutine = null;

            if (ShouldUseEasyAutoHint())
                RestartEasyAutoHintTimer();
        }

        private bool ShouldUseEasyAutoHint()
        {
            return currentMode == MahjongGameMode.Story &&
                   storyDifficulty == MahjongStoryDifficulty.Easy &&
                   !levelCompleteTriggered &&
                   !levelLoseTriggered;
        }

        private bool ShouldShowBlockedFeedback()
        {
            return currentMode != MahjongGameMode.Story ||
                   storyDifficulty == MahjongStoryDifficulty.Easy;
        }

        private List<Tile> GetActiveBoardTiles()
        {
            List<Tile> result = new();
            for (int i = 0; i < spawned.Count; i++)
            {
                Tile tile = spawned[i];
                if (tile != null && tile.gameObject.activeSelf && !lifted.Contains(tile))
                    result.Add(tile);
            }

            return result;
        }

        private List<Tile> GetFreeBoardTiles()
        {
            List<Tile> result = new();
            List<Tile> activeTiles = GetActiveBoardTiles();
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Tile tile = activeTiles[i];
                if (tile != null && (!useOpenRule || IsTileFree(tile)))
                    result.Add(tile);
            }

            return result;
        }

        private void HandleTrayPairMatched(Tile first, Tile second)
        {
            if (first == null || second == null)
                return;

            TileNode firstNode = GetNode(first);
            TileNode secondNode = GetNode(second);
            if (firstNode == null || secondNode == null || firstNode.Slot == null || secondNode.Slot == null)
                return;

            matchedPairHistory.Push(new MatchedPairRecord(
                first.Id,
                second.Id,
                CloneSlot(firstNode.Slot),
                CloneSlot(secondNode.Slot),
                FindTileDataForId(first.Id),
                FindTileDataForId(second.Id)));
        }

        private bool RestoreSelectedTile(Tile tile)
        {
            if (tile == null || root == null || layout == null)
                return false;

            TileNode node = GetNode(tile);
            LayoutSlot slot = node != null ? node.Slot : null;

            while (selectedMoveHistory.Count > 0)
            {
                SelectedMoveRecord record = selectedMoveHistory.Pop();
                if (record.Tile == tile)
                {
                    slot = record.Slot;
                    break;
                }
            }

            if (slot == null)
                return false;

            lifted.Remove(tile);
            tile.transform.SetParent(root, false);
            tile.Rect.anchoredPosition = layout.GetUiPos(slot);
            tile.Rect.localScale = Vector3.one;
            tile.Rect.localRotation = Quaternion.identity;
            tile.SetRuntimeSize(layout.TileSize);
            tile.SetSelected(false);
            tile.SetBlocked(false);
            tile.gameObject.SetActive(true);

            ApplySorting();
            RefreshBlockedView();
            return true;
        }

        private bool RestoreMatchedPair(MatchedPairRecord record)
        {
            if (record == null || root == null || layout == null)
                return false;

            Tile first = CreateRestoredTile(record.FirstData, record.FirstId, record.FirstSlot);
            Tile second = CreateRestoredTile(record.SecondData, record.SecondId, record.SecondSlot);

            if (first == null || second == null)
            {
                if (first != null)
                    DestroySafe(first.gameObject);

                if (second != null)
                    DestroySafe(second.gameObject);

                return false;
            }

            ApplySorting();
            FitAndCenterIntoBoardArea();
            RefreshBlockedView();
            return true;
        }

        private Tile CreateRestoredTile(TileData data, string id, LayoutSlot slot)
        {
            if (data == null || data.Prefab == null || slot == null)
                return null;

            Tile tile = Instantiate(data.Prefab, root);
            tile.name = $"{id}_undo_{spawned.Count}";
            tile.Setup(id, this);
            tile.SetRuntimeSize(layout.TileSize);
            tile.Rect.anchoredPosition = layout.GetUiPos(slot);
            tile.Rect.localScale = Vector3.one;
            tile.Rect.localRotation = Quaternion.identity;
            tile.gameObject.SetActive(true);

            spawned.Add(tile);
            nodes.Add(new TileNode(tile, CloneSlot(slot)));
            return tile;
        }

        private TileData FindTileDataForId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            IReadOnlyList<TileData> source = GetTileSourceForCurrentFlow();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    TileData data = source[i];
                    if (data != null && data.Id == id)
                        return data;
                }
            }

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            IReadOnlyList<TileData> baseTiles = store != null ? store.BaseTiles : null;
            if (baseTiles != null)
            {
                for (int i = 0; i < baseTiles.Count; i++)
                {
                    TileData data = baseTiles[i];
                    if (data != null && data.Id == id)
                        return data;
                }
            }

            return null;
        }

        private LayoutSlot CloneSlot(LayoutSlot slot)
        {
            return slot == null ? null : new LayoutSlot(slot.X, slot.Y, slot.Z);
        }

        private void HandleTrayChanged()
        {
            if (levelCompleteTriggered || levelLoseTriggered)
                return;

            RefreshBlockedView();
            CheckWin();
            RestartEasyAutoHintTimer();
        }

        private void HandleTrayLoseTriggered()
        {
            if (levelCompleteTriggered || levelLoseTriggered)
                return;

            levelLoseTriggered = true;
            StopEasyAutoHintTimer();

            if (ComboSystem.I != null)
                ComboSystem.I.ResetCombo();

            ProcessLoseRewardAndProgress();

            Debug.Log($"[Board] Lose | Mode={currentMode} | Level={storyLevelNumber} | Stage={storyStageIndex + 1}");

            if (levelResultUI != null)
                levelResultUI.ShowLose();

            LoseTriggered?.Invoke();
        }

        private void RefreshBlockedView()
        {
            bool showBlockedFeedback = ShouldShowBlockedFeedback();
            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || !n.Tile.gameObject.activeSelf || lifted.Contains(n.Tile))
                    continue;

                bool blocked = useOpenRule && !IsTileFree(n.Tile);
                n.Tile.SetBlocked(blocked, showBlockedFeedback, showBlockedFeedback);
            }
        }

        private bool IsTileFree(Tile tile)
        {
            TileNode node = GetNode(tile);
            if (node == null || node.Slot == null)
                return false;

            LayoutSlot slot = node.Slot;

            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || n.Slot == null || !n.Tile.gameObject.activeSelf || lifted.Contains(n.Tile) || n.Tile == tile)
                    continue;

                if (DoesUpperSlotCoverLowerSlot(n.Slot, slot))
                    return false;
            }

            bool leftBlocked = false;
            bool rightBlocked = false;

            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || n.Slot == null || !n.Tile.gameObject.activeSelf || lifted.Contains(n.Tile) || n.Tile == tile)
                    continue;

                if (n.Slot.Z != slot.Z)
                    continue;

                int dx = n.Slot.X - slot.X;
                int dy = Mathf.Abs(n.Slot.Y - slot.Y);

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

        private bool DoesUpperSlotCoverLowerSlot(LayoutSlot upper, LayoutSlot lower)
        {
            if (upper == null || lower == null || layout == null)
                return false;

            if (upper.Z != lower.Z + 1)
                return false;

            Vector2 upperPos = layout.GetUiPos(upper);
            Vector2 lowerPos = layout.GetUiPos(lower);
            Vector2 tileSize = layout.TileSize;

            float overlapX = Mathf.Max(0f, tileSize.x - Mathf.Abs(upperPos.x - lowerPos.x));
            float overlapY = Mathf.Max(0f, tileSize.y - Mathf.Abs(upperPos.y - lowerPos.y));

            return overlapX >= tileSize.x * 0.15f &&
                   overlapY >= tileSize.y * 0.15f;
        }

        private TileNode GetNode(Tile tile)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n != null && n.Tile == tile)
                    return n;
            }

            return null;
        }

        private void CheckWin()
        {
            if (levelCompleteTriggered || levelLoseTriggered)
                return;

            for (int i = 0; i < spawned.Count; i++)
            {
                Tile t = spawned[i];
                if (t != null && t.gameObject.activeSelf && !lifted.Contains(t))
                    return;
            }

            if (tray != null && tray.Count > 0)
                return;

            levelCompleteTriggered = true;
            StopEasyAutoHintTimer();

            ProcessWinRewardAndProgress();

            if (ScoreSystem.I != null)
                ScoreSystem.I.CommitLevelScoreToTotal();

            if (ComboSystem.I != null)
                ComboSystem.I.ResetCombo();

            Debug.Log($"[Board] Win | Mode={currentMode} | Level={storyLevelNumber} | Stage={storyStageIndex + 1} | LaunchMode={MahjongSession.LaunchMode}");

            if (levelResultUI != null)
                levelResultUI.ShowWin();

            WinTriggered?.Invoke();
        }

        private void ProcessWinRewardAndProgress()
        {
            if (matchRewardProcessed)
                return;

            switch (currentMode)
            {
                case MahjongGameMode.Battle:
                    ProcessBattleResultAndProgress(MahjongBattleResult.Win);
                    break;

                case MahjongGameMode.Story:
                    int completedStage = storyStageIndex + 1;
                    int stageCount = GetCurrentStageCount();
                    ProcessStoryWinRewardAndProgress();
                    if (storyDifficulty == MahjongStoryDifficulty.Hardcore)
                        MahjongProgress.AdvanceHardcoreStage(storyLevelNumber, completedStage, stageCount);

                    if (stageCount > 0 && completedStage >= stageCount)
                    {
                        if (storyLevelNumber == 1)
                            MahjongProgress.CompleteTutorial();
                        else
                            MahjongProgress.UnlockNextLevel(storyLevelNumber);
                    }
                    break;

                case MahjongGameMode.Endless:
                    ProcessEndlessWinRewardAndProgress();
                    break;

                default:
                    break;
            }
        }

        private void ProcessLoseRewardAndProgress()
        {
            if (matchRewardProcessed)
                return;

            switch (currentMode)
            {
                case MahjongGameMode.Battle:
                    ProcessBattleResultAndProgress(MahjongBattleResult.Lose);
                    break;

                case MahjongGameMode.Story:
                    if (storyDifficulty == MahjongStoryDifficulty.Hardcore)
                    {
                        MahjongProgress.ResetHardcoreRun(storyLevelNumber);
                        MahjongSession.StartStory(storyLevelNumber, 1, MahjongStoryDifficulty.Hardcore);
                    }
                    break;
                case MahjongGameMode.Endless:
                default:
                    break;
            }
        }

        private void ProcessStoryWinRewardAndProgress()
        {
            if (matchRewardProcessed)
                return;

            matchRewardProcessed = true;

            int score = GetCurrentLevelScoreSafe();
            int maxCombo = GetMaxComboSafe();

            MahjongProgress.RecordStoryStageResult(storyDifficulty, storyLevelNumber, storyStageIndex + 1, score);

            MahjongMatchResultData matchResult =
                MahjongMatchResultData.CreateStoryWin(storyLevelNumber, storyStageIndex + 1, score, maxCombo);

            MahjongMatchProcessResult processed =
                MahjongMatchService.I != null
                    ? MahjongMatchService.I.ProcessMatch(matchResult)
                    : null;

            int granted = processed != null ? processed.GrantedOzTile : 0;

            Debug.Log($"[Board] Story reward processed | Level={storyLevelNumber} | Stage={storyStageIndex + 1} | Score={score} | MaxCombo={maxCombo} | OzTile={granted}");
        }

        private void ProcessEndlessWinRewardAndProgress()
        {
            if (matchRewardProcessed)
                return;

            matchRewardProcessed = true;

            int score = GetCurrentLevelScoreSafe();
            int maxCombo = GetMaxComboSafe();

            MahjongMatchResultData matchResult =
                MahjongMatchResultData.CreateEndlessResult(endlessLevelNumber, score, maxCombo, true);

            MahjongMatchProcessResult processed =
                MahjongMatchService.I != null
                    ? MahjongMatchService.I.ProcessMatch(matchResult)
                    : null;

            int granted = processed != null ? processed.GrantedOzTile : 0;

            Debug.Log($"[Board] Endless reward processed | EndlessLevel={endlessLevelNumber} | Score={score} | MaxCombo={maxCombo} | OzTile={granted}");
        }

        private void ProcessBattleResultAndProgress(MahjongBattleResult battleResult)
        {
            if (matchRewardProcessed)
                return;

            matchRewardProcessed = true;

            int score = GetCurrentLevelScoreSafe();
            int maxCombo = GetMaxComboSafe();
            int stakePot = Mathf.Max(0, MahjongSession.BattleStakePot);

            MahjongMatchResultData matchResult =
                MahjongMatchResultData.CreateBattleResult(battleResult, score, maxCombo, stakePot);

            MahjongMatchProcessResult processed =
                MahjongMatchService.I != null
                    ? MahjongMatchService.I.ProcessMatch(matchResult)
                    : null;

            int granted = processed != null ? processed.GrantedOzTile : 0;

            Debug.Log($"[Board] Battle result processed | Result={battleResult} | Opponent={MahjongSession.BattleOpponentName} | Score={score} | MaxCombo={maxCombo} | Stake={stakePot} | OzTile={granted}");
        }

        private int GetCurrentLevelScoreSafe()
        {
            if (ScoreSystem.I == null)
                return 0;

            return Mathf.Max(0, ScoreSystem.I.CurrentLevelScore);
        }

        private int GetMaxComboSafe()
        {
            if (ComboSystem.I == null)
                return 0;

            object comboObject = ComboSystem.I;

            return Mathf.Max(
                0,
                TryReadIntMember(comboObject, "MaxCombo",
                TryReadIntMember(comboObject, "BestCombo",
                TryReadIntMember(comboObject, "HighestCombo",
                TryReadIntMember(comboObject, "PeakCombo",
                TryReadIntMember(comboObject, "CurrentCombo", 0)))))
            );
        }

        private int TryReadIntMember(object target, string memberName, int fallback)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
                return fallback;

            Type type = target.GetType();

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(int))
            {
                object value = property.GetValue(target);
                if (value is int intValue)
                    return intValue;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(int))
            {
                object value = field.GetValue(target);
                if (value is int intValue)
                    return intValue;
            }

            return fallback;
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            StopShuffleAnimation();
            StopEasyAutoHintTimer();
            ClearAssistHints();

            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    DestroySafe(spawned[i].gameObject);
            }

            if (tray != null)
                tray.ClearImmediate();

            if (root != null)
            {
                root.anchoredPosition = Vector2.zero;
                root.localScale = Vector3.one;
            }

            spawned.Clear();
            nodes.Clear();
            buildList.Clear();
            lifted.Clear();
            selectedMoveHistory.Clear();
            matchedPairHistory.Clear();
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

        private void StopShuffleAnimation()
        {
            if (shuffleRoutine == null)
                return;

            StopCoroutine(shuffleRoutine);
            shuffleRoutine = null;
        }

        private static float EaseInOut(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void Shuffle(List<TileData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private sealed class SelectedMoveRecord
        {
            public readonly Tile Tile;
            public readonly LayoutSlot Slot;

            public SelectedMoveRecord(Tile tile, LayoutSlot slot)
            {
                Tile = tile;
                Slot = slot;
            }
        }

        private sealed class MatchedPairRecord
        {
            public readonly string FirstId;
            public readonly string SecondId;
            public readonly LayoutSlot FirstSlot;
            public readonly LayoutSlot SecondSlot;
            public readonly TileData FirstData;
            public readonly TileData SecondData;

            public MatchedPairRecord(string firstId, string secondId, LayoutSlot firstSlot, LayoutSlot secondSlot, TileData firstData, TileData secondData)
            {
                FirstId = firstId;
                SecondId = secondId;
                FirstSlot = firstSlot;
                SecondSlot = secondSlot;
                FirstData = firstData;
                SecondData = secondData;
            }
        }
    }
}
