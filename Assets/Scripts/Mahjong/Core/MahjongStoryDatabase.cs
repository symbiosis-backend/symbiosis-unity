using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class StoryStage
    {
        [Min(1)] public int layoutLevel = 1;
        public string themeId = "mahjong_history";
        public Sprite background;
        public string title;
        [TextArea(2, 8)] public string description;
    }

    [Serializable]
    public sealed class StoryLevel
    {
        [Min(1)] public int levelNumber = 1;
        public string title;
        public AudioClip music;
        public List<StoryStage> stages = new();
    }

    [DisallowMultipleComponent]
    public sealed class MahjongStoryDatabase : MonoBehaviour
    {
        public static MahjongStoryDatabase I { get; private set; }

        [Header("Story Levels")]
        [SerializeField] private List<StoryLevel> levels = new();

        public IReadOnlyList<StoryLevel> Levels => levels;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        public StoryLevel GetLevel(int levelNumber)
        {
            if (levels == null || levels.Count == 0)
                return null;

            for (int i = 0; i < levels.Count; i++)
            {
                StoryLevel level = levels[i];
                if (level == null)
                    continue;

                if (level.levelNumber == levelNumber)
                    return level;
            }

            return null;
        }

        public StoryStage GetStage(int levelNumber, int stageIndex)
        {
            StoryLevel level = GetLevel(levelNumber);
            if (level == null || level.stages == null || level.stages.Count == 0)
                return null;

            if (stageIndex < 0 || stageIndex >= level.stages.Count)
                return null;

            return level.stages[stageIndex];
        }

        public int GetStageCount(int levelNumber)
        {
            StoryLevel level = GetLevel(levelNumber);
            return level != null && level.stages != null ? level.stages.Count : 0;
        }

        public int GetMaxLevelNumber()
        {
            int max = 0;

            if (levels == null)
                return 0;

            for (int i = 0; i < levels.Count; i++)
            {
                StoryLevel level = levels[i];
                if (level == null)
                    continue;

                if (level.levelNumber > max)
                    max = level.levelNumber;
            }

            return max;
        }
    }
}