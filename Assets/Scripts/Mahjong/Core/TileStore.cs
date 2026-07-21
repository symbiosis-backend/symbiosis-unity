using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class LevelStageContent
    {
        [Min(1)] public int StageIndex = 1;
        [Min(1)] public int LayoutLevel = 1;
        public bool UseCustomLayout = false;
        public List<LayoutSlot> CustomSlots = new();
        public Sprite Background;
        public string Title = "Stage Title";
        [TextArea(2, 8)] public string Description;
    }

    [Serializable]
    public sealed class LevelPack
    {
        [Min(1)] public int LevelNumber = 1;
        public string LevelId = "level_01";
        public string DisplayName = "Level 1";
        public AudioClip Music;
        public List<TileData> Tiles = new();
        public List<LevelStageContent> Stages = new();
    }

    [DisallowMultipleComponent]
    public sealed class TileStore : MonoBehaviour
    {
        public static TileStore I { get; private set; }

        [Header("Fallback Tiles")]
        [SerializeField] private List<TileData> baseTiles = new();

        [Header("Level Packs")]
        [SerializeField] private List<LevelPack> levelPacks = new();

        public IReadOnlyList<TileData> BaseTiles => baseTiles;
        public IReadOnlyList<LevelPack> LevelPacks => levelPacks;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        public LevelPack GetLevelPack(int levelNumber)
        {
            if (levelPacks == null || levelPacks.Count == 0)
                return null;

            for (int i = 0; i < levelPacks.Count; i++)
            {
                LevelPack pack = levelPacks[i];
                if (pack == null)
                    continue;

                if (pack.LevelNumber == levelNumber)
                    return pack;
            }

            return null;
        }

        public bool HasLevelPack(int levelNumber)
        {
            return GetLevelPack(levelNumber) != null;
        }

        public IReadOnlyList<TileData> GetTilesForLevel(int levelNumber)
        {
            LevelPack pack = GetLevelPack(levelNumber);
            if (pack != null && pack.Tiles != null && pack.Tiles.Count > 0)
                return pack.Tiles;

            if (StoryChinaContentLibrary.HasLevel(levelNumber))
            {
                IReadOnlyList<TileData> chinaTiles = StoryChinaTileLibrary.GetTiles(transform);
                if (chinaTiles != null && chinaTiles.Count > 0)
                    return chinaTiles;
            }

            if (StoryThemedContentLibrary.HasLevel(levelNumber))
            {
                IReadOnlyList<TileData> themedTiles = StoryThemedTileLibrary.GetTiles(levelNumber, transform);
                if (themedTiles != null && themedTiles.Count > 0)
                    return themedTiles;
            }

            return baseTiles;
        }

        public AudioClip GetMusicForLevel(int levelNumber)
        {
            LevelPack pack = GetLevelPack(levelNumber);
            return pack != null ? pack.Music : null;
        }

        public int GetStageCount(int levelNumber)
        {
            LevelPack pack = GetLevelPack(levelNumber);
            if (pack == null || pack.Stages == null || pack.Stages.Count == 0)
            {
                if (StoryChinaContentLibrary.HasLevel(levelNumber))
                    return StoryChinaContentLibrary.StageCount;

                return StoryThemedContentLibrary.GetStageCount(levelNumber);
            }

            int count = 0;

            for (int i = 0; i < pack.Stages.Count; i++)
            {
                if (pack.Stages[i] != null)
                    count++;
            }

            return count;
        }

        public bool TryGetStageContent(int levelNumber, int stageIndex, out LevelStageContent content)
        {
            content = null;

            if (TryGetSerializedStageContent(levelNumber, stageIndex, out content))
                return true;

            if (StoryChinaContentLibrary.TryCreateStage(levelNumber, stageIndex, ResolveLanguage(), out content))
            {
                ApplyFallbackBackground(content, stageIndex);
                return true;
            }

            if (StoryThemedContentLibrary.TryCreateStage(levelNumber, stageIndex, ResolveLanguage(), out content))
            {
                ApplyFallbackBackground(content, stageIndex);
                return true;
            }

            return false;
        }

        public string GetLevelDisplayName(int levelNumber)
        {
            LevelPack pack = GetLevelPack(levelNumber);
            if (pack == null)
            {
                if (StoryChinaContentLibrary.HasLevel(levelNumber))
                    return StoryChinaContentLibrary.GetLevelDisplayName(ResolveLanguage());

                if (StoryThemedContentLibrary.HasLevel(levelNumber))
                    return StoryThemedContentLibrary.GetLevelDisplayName(levelNumber, ResolveLanguage());

                return $"Level {levelNumber}";
            }

            return string.IsNullOrWhiteSpace(pack.DisplayName) ? $"Level {levelNumber}" : pack.DisplayName;
        }

        public int GetMaxLevelNumber()
        {
            int max = Mathf.Max(StoryChinaContentLibrary.LevelNumber, StoryThemedContentLibrary.GetMaxLevelNumber());

            if (levelPacks == null || levelPacks.Count == 0)
                return max;

            for (int i = 0; i < levelPacks.Count; i++)
            {
                LevelPack pack = levelPacks[i];
                if (pack == null)
                    continue;

                if (pack.LevelNumber > max)
                    max = pack.LevelNumber;
            }

            return max;
        }

        public int GetFirstLevelNumber()
        {
            int first = StoryChinaContentLibrary.LevelNumber;
            bool found = StoryChinaContentLibrary.HasLevel(first);
            int themedFirst = StoryThemedContentLibrary.GetFirstLevelNumber();
            if (themedFirst > 0 && (!found || themedFirst < first))
            {
                first = themedFirst;
                found = true;
            }

            if (levelPacks == null || levelPacks.Count == 0)
                return found ? first : 0;

            for (int i = 0; i < levelPacks.Count; i++)
            {
                LevelPack pack = levelPacks[i];
                if (pack == null)
                    continue;

                if (!found || pack.LevelNumber < first)
                {
                    first = pack.LevelNumber;
                    found = true;
                }
            }

            return found ? first : 0;
        }

        public bool HasNextLevel(int currentLevel)
        {
            return GetNextLevelNumber(currentLevel) > 0;
        }

        public int GetNextLevelNumber(int currentLevel)
        {
            int next = int.MaxValue;
            bool found = false;

            TryRegisterNextLevelCandidate(StoryChinaContentLibrary.LevelNumber, currentLevel, ref next, ref found);
            if (StoryThemedContentLibrary.TryGetNextLevelNumber(currentLevel, out int themedNext))
                TryRegisterNextLevelCandidate(themedNext, currentLevel, ref next, ref found);

            if (levelPacks != null)
            {
                for (int i = 0; i < levelPacks.Count; i++)
                {
                    LevelPack pack = levelPacks[i];
                    if (pack == null)
                        continue;

                    TryRegisterNextLevelCandidate(pack.LevelNumber, currentLevel, ref next, ref found);
                }
            }

            return found ? next : 0;
        }

        private bool TryGetSerializedStageContent(int levelNumber, int stageIndex, out LevelStageContent content)
        {
            content = null;

            LevelPack pack = GetLevelPack(levelNumber);
            if (pack == null || pack.Stages == null || pack.Stages.Count == 0)
                return false;

            for (int i = 0; i < pack.Stages.Count; i++)
            {
                LevelStageContent stage = pack.Stages[i];
                if (stage == null)
                    continue;

                if (stage.StageIndex == stageIndex)
                {
                    content = stage;
                    return true;
                }
            }

            return false;
        }

        private void ApplyFallbackBackground(LevelStageContent content, int stageIndex)
        {
            if (content == null || content.Background != null)
                return;

            if (!TryGetSerializedStageContent(1, stageIndex, out LevelStageContent fallback))
                return;

            content.Background = fallback.Background;
        }

        private static void TryRegisterNextLevelCandidate(int candidate, int currentLevel, ref int next, ref bool found)
        {
            if (candidate <= currentLevel || candidate >= next)
                return;

            next = candidate;
            found = true;
        }

        private static GameLanguage ResolveLanguage()
        {
            return AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
        }
    }
}
