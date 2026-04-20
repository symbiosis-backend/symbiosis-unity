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

        [Header("Story")]
        [SerializeField, Min(1)] private int maxStoryLevel = 10;

        private readonly List<TileData> buildList = new();
        private readonly List<Tile> spawned = new();
        private readonly List<TileNode> nodes = new();
        private readonly HashSet<Tile> lifted = new();

        private bool levelCompleteTriggered;
        private bool levelLoseTriggered;
        private bool matchRewardProcessed;

        private int storyLevelNumber = 1;
        private int storyStageIndex = 0;
        private Sprite activeBackground;
        private bool useStoryStageRuntime;
        private LevelStageContent currentStageContent;
        private MahjongGameMode currentMode = MahjongGameMode.None;

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
        }

        private void UnbindTray()
        {
            if (tray == null)
                return;

            tray.Changed -= HandleTrayChanged;
            tray.LoseTriggered -= HandleTrayLoseTriggered;
        }

        private IEnumerator Start()
        {
            if (!buildOnStart)
                yield break;

            yield return null;
            Canvas.ForceUpdateCanvases();
            Build();
        }

        [ContextMenu("Build")]
        public void Build()
        {
            levelCompleteTriggered = false;
            levelLoseTriggered = false;
            matchRewardProcessed = false;
            currentStageContent = null;
            currentMode = MahjongGameMode.None;

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

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (store == null || boardArea == null || root == null || layout == null)
            {
                Debug.LogError("[Board] Не назначены ссылки.");
                return;
            }

            ResolveFlowMode();

            IReadOnlyList<TileData> src = useStoryStageRuntime
                ? store.GetTilesForLevel(storyLevelNumber)
                : store.BaseTiles;

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

                activeBackground = currentStageContent != null ? currentStageContent.Background : null;
            }
            else
            {
                activeBackground = null;
            }

            ApplyGameplayBackground();

            Vector2 tileSize = GetTileSizeFromStore(src);
            layout.SetTileSize(tileSize);

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

            if (repeatPairsToFillSlots)
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

            if (shuffleOnBuild)
                Shuffle(buildList);

            PrepareRoot();

            int count = Mathf.Min(buildList.Count, slots.Count);
            for (int i = 0; i < count; i++)
                CreateTile(buildList[i], slots[i], i);

            ApplySorting();
            FitAndCenterIntoBoardArea();
            RefreshBlockedView();
            BringBackgroundBackAndTilesFront();

            if (gameplayHudRoot != null)
                gameplayHudRoot.SetActive(true);

            Debug.Log($"[Board] Build complete | Mode={currentMode} | StoryLevel={storyLevelNumber} | Stage={storyStageIndex + 1} | LaunchMode={MahjongSession.LaunchMode} | Slots={slots.Count}");
        }

        public void SetStoryStage(int levelNumber, int stageIndex)
        {
            storyLevelNumber = Mathf.Max(1, levelNumber);
            storyStageIndex = Mathf.Max(0, stageIndex);
            useStoryStageRuntime = true;
            currentMode = MahjongGameMode.Story;

            if (store == null)
                store = TileStore.I != null ? TileStore.I : FindAnyObjectByType<TileStore>();

            if (store != null)
            {
                bool foundStage = store.TryGetStageContent(storyLevelNumber, storyStageIndex + 1, out currentStageContent);
                if (!foundStage)
                    Debug.LogWarning($"[Board] SetStoryStage: stage not found | Level={storyLevelNumber} | Stage={storyStageIndex + 1}");
            }

            activeBackground = currentStageContent != null ? currentStageContent.Background : null;
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
                levelIndex = storyLevelNumber;
                useStoryStageRuntime = true;

                Debug.Log($"[Board] Story mode | Level={storyLevelNumber} | Stage={storyStageIndex + 1}");
                return;
            }

            currentMode = MahjongGameMode.Story;
            storyLevelNumber = Mathf.Clamp(levelIndex, 1, maxStoryLevel);
            storyStageIndex = 0;
            levelIndex = storyLevelNumber;
            useStoryStageRuntime = true;
            MahjongSession.StartStory(storyLevelNumber, 1);

            Debug.Log($"[Board] Fallback story mode | Level={storyLevelNumber} | Stage=1");
        }

        private void ApplyGameplayBackground()
        {
            if (gameplayBackgroundImage == null)
                return;

            gameplayBackgroundImage.sprite = activeBackground;
            gameplayBackgroundImage.transform.SetAsFirstSibling();
        }

        private void ApplyLayoutByFlow()
        {
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

            float availableWidth = Mathf.Max(1f, boardArea.rect.width - paddingX * 2f);
            float availableHeight = Mathf.Max(1f, boardArea.rect.height - paddingY * 2f);

            float scaleX = availableWidth / contentSize.x;
            float scaleY = availableHeight / contentSize.y;

            float fitScale = Mathf.Min(scaleX, scaleY);
            fitScale = Mathf.Clamp(fitScale, minFitScale, maxFitScale);

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
            if (tile == null || tray == null || tray.IsBusy || lifted.Contains(tile) || levelCompleteTriggered || levelLoseTriggered)
                return;

            if (useOpenRule && !IsTileFree(tile))
                return;

            if (!tray.TryAdd(tile))
                return;

            lifted.Add(tile);
            tile.SetSelected(false);
            tile.SetBlocked(false);

            RefreshBlockedView();
            CheckWin();
        }

        private void HandleTrayChanged()
        {
            if (levelCompleteTriggered || levelLoseTriggered)
                return;

            RefreshBlockedView();
            CheckWin();
        }

        private void HandleTrayLoseTriggered()
        {
            if (levelCompleteTriggered || levelLoseTriggered)
                return;

            levelLoseTriggered = true;

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
            for (int i = 0; i < nodes.Count; i++)
            {
                TileNode n = nodes[i];
                if (n == null || n.Tile == null || !n.Tile.gameObject.activeSelf || lifted.Contains(n.Tile))
                    continue;

                bool blocked = useOpenRule && !IsTileFree(n.Tile);
                n.Tile.SetBlocked(blocked);
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

                if (n.Slot.Z == slot.Z + 1)
                {
                    int dxTop = Mathf.Abs(n.Slot.X - slot.X);
                    int dyTop = Mathf.Abs(n.Slot.Y - slot.Y);

                    if (dxTop <= 1 && dyTop <= 1)
                        return false;
                }
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
                default:
                    ProcessStoryWinRewardAndProgress();
                    MahjongProgress.UnlockNextLevel(storyLevelNumber);
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

            MahjongMatchResultData matchResult =
                MahjongMatchResultData.CreateStoryWin(storyLevelNumber, storyStageIndex + 1, score, maxCombo);

            MahjongMatchProcessResult processed =
                MahjongMatchService.I != null
                    ? MahjongMatchService.I.ProcessMatch(matchResult)
                    : null;

            int granted = processed != null ? processed.GrantedAltin : 0;

            Debug.Log($"[Board] Story reward processed | Level={storyLevelNumber} | Stage={storyStageIndex + 1} | Score={score} | MaxCombo={maxCombo} | Altin={granted}");
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

            int granted = processed != null ? processed.GrantedAltin : 0;

            Debug.Log($"[Board] Battle result processed | Result={battleResult} | Opponent={MahjongSession.BattleOpponentName} | Score={score} | MaxCombo={maxCombo} | Stake={stakePot} | Altin={granted}");
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

        private void Shuffle(List<TileData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}