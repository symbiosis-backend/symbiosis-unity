using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class MahjongRewardLevel
    {
        [Min(1)] public int LevelNumber = 1;
        [Min(0)] public int BaseClearReward = 10;
    }

    [Serializable]
    public sealed class MahjongRewardResult
    {
        public MahjongGameMode Mode;
        public int BaseReward;
        public int ComboReward;
        public int BattleReward;
        public int StakeReward;
        public int EndlessReward;
        public int TotalReward;
    }

    [DisallowMultipleComponent]
    public sealed class MahjongRewardService : MonoBehaviour
    {
        public static MahjongRewardService I { get; private set; }

        [Header("Story Reward Table")]
        [SerializeField] private List<MahjongRewardLevel> storyLevelRewards = new()
        {
            new MahjongRewardLevel { LevelNumber = 1, BaseClearReward = 10 },
            new MahjongRewardLevel { LevelNumber = 2, BaseClearReward = 15 },
            new MahjongRewardLevel { LevelNumber = 3, BaseClearReward = 20 }
        };

        [Header("Combo Reward")]
        [SerializeField] private bool rewardByCombo = true;
        [SerializeField] private int rewardPerComboStep = 1;

        [Header("Battle Reward")]
        [SerializeField] private int battleWinReward = 100;
        [SerializeField] private bool allowStakeReward = true;

        [Header("Endless Reward")]
        [SerializeField] private int endlessRewardPerReachedLevel = 3;
        [SerializeField] private int endlessFlatCompletionReward = 0;

        [Header("Fallback")]
        [SerializeField] private int fallbackStoryBaseReward = 10;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            NormalizeRewardTable();
            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        private void OnValidate()
        {
            NormalizeRewardTable();
        }

        public MahjongRewardResult CalculateReward(MahjongMatchResultData matchResult)
        {
            MahjongRewardResult result = new MahjongRewardResult();

            if (matchResult == null)
                return result;

            result.Mode = matchResult.Mode;

            switch (matchResult.Mode)
            {
                case MahjongGameMode.Story:
                    CalculateStoryReward(matchResult, result);
                    break;

                case MahjongGameMode.Battle:
                    CalculateBattleReward(matchResult, result);
                    break;

                case MahjongGameMode.Endless:
                    CalculateEndlessReward(matchResult, result);
                    break;

                default:
                    break;
            }

            result.TotalReward =
                result.BaseReward +
                result.ComboReward +
                result.BattleReward +
                result.StakeReward +
                result.EndlessReward;

            return result;
        }

        public MahjongRewardResult GrantReward(MahjongMatchResultData matchResult)
        {
            MahjongRewardResult reward = CalculateReward(matchResult);

            if (reward.TotalReward > 0 && CurrencyService.I != null)
                CurrencyService.I.AddOzAltin(reward.TotalReward);

            if (matchResult != null)
                matchResult.RewardGranted = reward.TotalReward;

            return reward;
        }

        private void CalculateStoryReward(MahjongMatchResultData matchResult, MahjongRewardResult reward)
        {
            if (!matchResult.IsWin)
                return;

            reward.BaseReward = GetStoryBaseReward(matchResult.StoryLevelNumber);
            reward.ComboReward = CalculateComboReward(matchResult.MaxCombo);
        }

        private void CalculateBattleReward(MahjongMatchResultData matchResult, MahjongRewardResult reward)
        {
            if (matchResult.BattleResult != MahjongBattleResult.Win)
                return;

            reward.BattleReward = Mathf.Max(0, battleWinReward);
            reward.ComboReward = CalculateComboReward(matchResult.MaxCombo);

            if (allowStakeReward)
                reward.StakeReward = Mathf.Max(0, matchResult.BattleStakePot);
        }

        private void CalculateEndlessReward(MahjongMatchResultData matchResult, MahjongRewardResult reward)
        {
            int reachedLevel = Mathf.Max(0, matchResult.EndlessReachedLevel);

            reward.EndlessReward =
                Mathf.Max(0, endlessFlatCompletionReward) +
                reachedLevel * Mathf.Max(0, endlessRewardPerReachedLevel);

            reward.ComboReward = CalculateComboReward(matchResult.MaxCombo);
        }

        private int GetStoryBaseReward(int levelNumber)
        {
            if (storyLevelRewards != null)
            {
                for (int i = 0; i < storyLevelRewards.Count; i++)
                {
                    MahjongRewardLevel item = storyLevelRewards[i];
                    if (item != null && item.LevelNumber == levelNumber)
                        return Mathf.Max(0, item.BaseClearReward);
                }
            }

            return Mathf.Max(0, fallbackStoryBaseReward);
        }

        private void NormalizeRewardTable()
        {
            if (storyLevelRewards == null)
                return;

            HashSet<int> seenLevels = new HashSet<int>();

            for (int i = storyLevelRewards.Count - 1; i >= 0; i--)
            {
                MahjongRewardLevel item = storyLevelRewards[i];
                if (item == null)
                {
                    storyLevelRewards.RemoveAt(i);
                    continue;
                }

                item.LevelNumber = Mathf.Max(1, item.LevelNumber);
                item.BaseClearReward = Mathf.Max(0, item.BaseClearReward);

                if (!seenLevels.Add(item.LevelNumber))
                    storyLevelRewards.RemoveAt(i);
            }

            storyLevelRewards.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
        }

        private int CalculateComboReward(int maxCombo)
        {
            if (!rewardByCombo)
                return 0;

            return Mathf.Max(0, maxCombo) * Mathf.Max(0, rewardPerComboStep);
        }
    }
}
