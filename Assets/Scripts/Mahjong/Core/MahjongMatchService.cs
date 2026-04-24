using System;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class MahjongMatchProcessResult
    {
        public bool Success;
        public MahjongGameMode Mode;
        public int GrantedAltin;
        public int Score;
        public int MaxCombo;
    }

    [DisallowMultipleComponent]
    public sealed class MahjongMatchService : MonoBehaviour
    {
        public static MahjongMatchService I { get; private set; }

        public MahjongMatchProcessResult LastProcessedResult { get; private set; }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        public void ClearLastProcessedResult()
        {
            LastProcessedResult = null;
        }

        public MahjongMatchProcessResult ProcessMatch(MahjongMatchResultData matchResult)
        {
            MahjongMatchProcessResult result = new MahjongMatchProcessResult();

            if (matchResult == null)
            {
                Debug.LogError("[MahjongMatchService] MatchResult is null.");
                LastProcessedResult = result;
                return result;
            }

            if (ProfileService.I == null || ProfileService.I.Current == null)
            {
                Debug.LogError("[MahjongMatchService] ProfileService or Current profile is null.");
                LastProcessedResult = result;
                return result;
            }

            PlayerProfile profile = ProfileService.I.Current;
            profile.EnsureData();

            MahjongRewardResult rewardResult = MahjongRewardService.I != null
                ? MahjongRewardService.I.GrantReward(matchResult)
                : new MahjongRewardResult();

            ApplyProfileProgress(profile, matchResult, rewardResult);
            EvaluateTitles(profile, matchResult);

            ProfileService.I.Save();
            ProfileService.I.NotifyProfileChanged();

            result.Success = true;
            result.Mode = matchResult.Mode;
            result.GrantedAltin = rewardResult != null ? rewardResult.TotalReward : 0;
            result.Score = Mathf.Max(0, matchResult.Score);
            result.MaxCombo = Mathf.Max(0, matchResult.MaxCombo);

            LastProcessedResult = result;
            return result;
        }

        private void EvaluateTitles(PlayerProfile profile, MahjongMatchResultData matchResult)
        {
            if (profile == null || matchResult == null)
                return;

            switch (matchResult.Mode)
            {
                case MahjongGameMode.Story:
                    if (MahjongTitleService.I != null)
                        MahjongTitleService.I.EvaluateStoryTitles(profile);
                    break;

                case MahjongGameMode.Battle:
                    if (MahjongTitleService.I != null)
                        MahjongTitleService.I.EvaluateBattleTitles(profile);
                    break;
            }
        }

        private void ApplyProfileProgress(PlayerProfile profile, MahjongMatchResultData matchResult, MahjongRewardResult rewardResult)
        {
            if (profile == null || matchResult == null)
                return;

            profile.EnsureData();

            switch (matchResult.Mode)
            {
                case MahjongGameMode.Story:
                    ApplyStoryProgress(profile, matchResult, rewardResult);
                    break;

                case MahjongGameMode.Battle:
                    ApplyBattleProgress(profile, matchResult, rewardResult);
                    break;

                case MahjongGameMode.Endless:
                    ApplyEndlessProgress(profile, matchResult, rewardResult);
                    break;
            }

            profile.TouchLoginTime();
        }

        private void ApplyStoryProgress(PlayerProfile profile, MahjongMatchResultData matchResult, MahjongRewardResult rewardResult)
        {
            MahjongStoryData story = profile.Mahjong.Story;
            if (story == null)
                return;

            if (!matchResult.IsWin)
                return;

            int levelNumber = Mathf.Max(1, matchResult.StoryLevelNumber);
            int stageIndex = Mathf.Max(1, matchResult.StoryStageIndex);
            int score = Mathf.Max(0, matchResult.Score);

            story.CurrentLevel = levelNumber;
            story.CurrentStage = stageIndex;

            story.StagesCompleted++;
            story.TotalScore += score;

            if (score > story.BestScore)
                story.BestScore = score;

            if (levelNumber > story.HighestUnlockedLevel)
            {
                story.HighestUnlockedLevel = levelNumber;
                story.HighestUnlockedStage = stageIndex;
            }
            else if (levelNumber == story.HighestUnlockedLevel && stageIndex > story.HighestUnlockedStage)
            {
                story.HighestUnlockedStage = stageIndex;
            }

            profile.Mahjong.TotalMatchesPlayed++;
            profile.Mahjong.TotalWins++;
            profile.Mahjong.TotalScoreAllModes += score;
        }

        private void ApplyBattleProgress(PlayerProfile profile, MahjongMatchResultData matchResult, MahjongRewardResult rewardResult)
        {
            MahjongBattleData battle = profile.Mahjong.Battle;
            if (battle == null)
                return;

            int score = Mathf.Max(0, matchResult.Score);

            switch (matchResult.BattleResult)
            {
                case MahjongBattleResult.Win:
                    battle.AddWin(matchResult.BattleMvp);
                    profile.Mahjong.TotalWins++;
                    break;

                case MahjongBattleResult.Lose:
                    battle.AddLoss(matchResult.BattleMvp);
                    profile.Mahjong.TotalLosses++;
                    break;

                case MahjongBattleResult.Draw:
                    battle.TotalMatches++;
                    break;
            }

            battle.LastStakeUsed = Mathf.Max(0, matchResult.BattleStakePot);
            battle.TotalBattleRewardEarned += rewardResult != null ? Mathf.Max(0, rewardResult.TotalReward) : 0;

            profile.Mahjong.TotalMatchesPlayed++;
            profile.Mahjong.TotalScoreAllModes += score;
        }

        private void ApplyEndlessProgress(PlayerProfile profile, MahjongMatchResultData matchResult, MahjongRewardResult rewardResult)
        {
            MahjongEndlessData endless = profile.Mahjong.Endless;
            if (endless == null)
                return;

            int reachedLevel = Mathf.Max(0, matchResult.EndlessReachedLevel);
            int score = Mathf.Max(0, matchResult.Score);
            int combo = Mathf.Max(0, matchResult.MaxCombo);
            int granted = rewardResult != null ? Mathf.Max(0, rewardResult.TotalReward) : 0;

            endless.TotalRuns++;
            endless.TotalScore += score;

            if (reachedLevel > endless.BestReachedLevel)
                endless.BestReachedLevel = reachedLevel;

            if (score > endless.BestScore)
                endless.BestScore = score;

            if (combo > endless.LongestCombo)
                endless.LongestCombo = combo;

            if (granted > endless.HighestRewardCollected)
                endless.HighestRewardCollected = granted;

            profile.Mahjong.TotalMatchesPlayed++;
            profile.Mahjong.TotalScoreAllModes += score;
        }
    }
}
