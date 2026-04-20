using System;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class MahjongMatchResultData
    {
        [Header("Mode")]
        public MahjongGameMode Mode;

        [Header("Common")]
        public bool IsWin;
        public int Score;
        public int MaxCombo;
        public int RewardGranted;

        [Header("Story")]
        public int StoryLevelNumber;
        public int StoryStageIndex;

        [Header("Battle")]
        public MahjongBattleResult BattleResult;
        public int BattleStakePot;

        [Header("Endless")]
        public int EndlessReachedLevel;

        public MahjongMatchResultData()
        {
            Mode = MahjongGameMode.None;

            IsWin = false;
            Score = 0;
            MaxCombo = 0;
            RewardGranted = 0;

            StoryLevelNumber = 0;
            StoryStageIndex = 0;

            BattleResult = MahjongBattleResult.None;
            BattleStakePot = 0;

            EndlessReachedLevel = 0;
        }

        public static MahjongMatchResultData CreateStoryWin(int levelNumber, int stageIndex, int score, int maxCombo)
        {
            return new MahjongMatchResultData
            {
                Mode = MahjongGameMode.Story,
                IsWin = true,
                Score = Mathf.Max(0, score),
                MaxCombo = Mathf.Max(0, maxCombo),
                StoryLevelNumber = Mathf.Max(1, levelNumber),
                StoryStageIndex = Mathf.Max(1, stageIndex)
            };
        }

        public static MahjongMatchResultData CreateBattleResult(MahjongBattleResult battleResult, int score, int maxCombo, int stakePot)
        {
            return new MahjongMatchResultData
            {
                Mode = MahjongGameMode.Battle,
                IsWin = battleResult == MahjongBattleResult.Win,
                Score = Mathf.Max(0, score),
                MaxCombo = Mathf.Max(0, maxCombo),
                BattleResult = battleResult,
                BattleStakePot = Mathf.Max(0, stakePot)
            };
        }

        public static MahjongMatchResultData CreateEndlessResult(int reachedLevel, int score, int maxCombo, bool isWinLikeCompletion = true)
        {
            return new MahjongMatchResultData
            {
                Mode = MahjongGameMode.Endless,
                IsWin = isWinLikeCompletion,
                Score = Mathf.Max(0, score),
                MaxCombo = Mathf.Max(0, maxCombo),
                EndlessReachedLevel = Mathf.Max(0, reachedLevel)
            };
        }
    }
}