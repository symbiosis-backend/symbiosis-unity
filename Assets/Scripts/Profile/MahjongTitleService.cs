using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongTitleService : MonoBehaviour
    {
        public static MahjongTitleService I { get; private set; }

        public const string TitleNovice = "novice";
        public const string TitleStorySeeker = "story_seeker";
        public const string TitleStoryWalker = "story_walker";
        public const string TitleStoryKeeper = "story_keeper";
        public const string TitleFirstBattle = "battle_first";
        public const string TitleBattleVeteran = "battle_veteran";
        public const string TitleBattleCenturion = "battle_centurion";

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

        public void EvaluateStoryTitles(PlayerProfile profile)
        {
            if (profile == null)
                return;

            profile.EnsureData();

            MahjongProfileData mahjong = profile.Mahjong;
            if (mahjong == null || mahjong.Story == null)
                return;

            MahjongStoryData story = mahjong.Story;

            TryUnlockNovice(profile, story);
            TryUnlockStoryMilestone(profile, story, TitleStorySeeker, 1, autoSelectIfEmpty: true);
            TryUnlockStoryMilestone(profile, story, TitleStoryWalker, 10, autoSelectIfEmpty: false);
            TryUnlockStoryMilestone(profile, story, TitleStoryKeeper, 100, autoSelectIfEmpty: false);

        }

        public void EvaluateBattleTitles(PlayerProfile profile)
        {
            if (profile == null)
                return;

            profile.EnsureData();

            MahjongProfileData mahjong = profile.Mahjong;
            if (mahjong == null || mahjong.Battle == null)
                return;

            MahjongBattleData battle = mahjong.Battle;
            TryUnlockBattleMilestone(profile, battle, TitleFirstBattle, 1, autoSelectIfEmpty: true);
            TryUnlockBattleMilestone(profile, battle, TitleBattleVeteran, 10, autoSelectIfEmpty: false);
            TryUnlockBattleMilestone(profile, battle, TitleBattleCenturion, 100, autoSelectIfEmpty: false);
        }

        public bool SelectTitle(PlayerProfile profile, string titleId)
        {
            if (profile == null || string.IsNullOrWhiteSpace(titleId))
                return false;

            profile.EnsureData();

            MahjongProfileData mahjong = profile.Mahjong;
            if (mahjong == null)
                return false;

            if (!mahjong.HasUnlockedTitle(titleId))
                return false;

            mahjong.SetSelectedTitle(titleId);
            profile.SetGlobalTitle(titleId);

            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }

            return true;
        }

        public string GetTitleDisplayName(string titleId)
        {
            switch (titleId)
            {
                case TitleNovice:
                    return GameLocalization.Text("mahjong.title.novice");
                case TitleStorySeeker:
                    return GameLocalization.Text("mahjong.title.story_seeker");
                case TitleStoryWalker:
                    return GameLocalization.Text("mahjong.title.story_walker");
                case TitleStoryKeeper:
                    return GameLocalization.Text("mahjong.title.story_keeper");
                case TitleFirstBattle:
                    return GameLocalization.Text("mahjong.title.battle_first");
                case TitleBattleVeteran:
                    return GameLocalization.Text("mahjong.title.battle_veteran");
                case TitleBattleCenturion:
                    return GameLocalization.Text("mahjong.title.battle_centurion");

                default:
                    return titleId;
            }
        }

        public string GetProfileDisplayTitle(PlayerProfile profile)
        {
            if (profile == null)
                return string.Empty;

            profile.EnsureData();

            string selectedTitle = profile.Mahjong != null ? profile.Mahjong.SelectedTitleId : string.Empty;
            if (!string.IsNullOrWhiteSpace(selectedTitle))
                return GetTitleDisplayName(selectedTitle);

            return string.IsNullOrWhiteSpace(profile.GlobalTitleId)
                ? string.Empty
                : GetTitleDisplayName(profile.GlobalTitleId);
        }

        private void TryUnlockNovice(PlayerProfile profile, MahjongStoryData story)
        {
            bool unlockCondition =
                story.HighestUnlockedLevel > 1 ||
                (story.HighestUnlockedLevel == 1 && story.HighestUnlockedStage >= 10);

            if (!unlockCondition)
                return;

            UnlockTitle(profile, TitleNovice, autoSelectIfEmpty: true);
        }

        private void TryUnlockStoryMilestone(PlayerProfile profile, MahjongStoryData story, string titleId, int requiredStages, bool autoSelectIfEmpty)
        {
            if (story == null || story.StagesCompleted < Mathf.Max(1, requiredStages))
                return;

            UnlockTitle(profile, titleId, autoSelectIfEmpty);
        }

        private void TryUnlockBattleMilestone(PlayerProfile profile, MahjongBattleData battle, string titleId, int requiredMatches, bool autoSelectIfEmpty)
        {
            if (battle == null || battle.TotalMatches < Mathf.Max(1, requiredMatches))
                return;

            UnlockTitle(profile, titleId, autoSelectIfEmpty);
        }

        private void UnlockTitle(PlayerProfile profile, string titleId, bool autoSelectIfEmpty)
        {
            if (profile == null || string.IsNullOrWhiteSpace(titleId))
                return;

            profile.EnsureData();

            MahjongProfileData mahjong = profile.Mahjong;
            if (mahjong == null)
                return;

            bool alreadyUnlocked = mahjong.HasUnlockedTitle(titleId);
            if (alreadyUnlocked)
                return;

            mahjong.UnlockTitle(titleId);

            if (autoSelectIfEmpty && string.IsNullOrWhiteSpace(mahjong.SelectedTitleId))
            {
                mahjong.SetSelectedTitle(titleId);
                profile.SetGlobalTitle(titleId);
            }

            Debug.Log($"[MahjongTitleService] Title unlocked: {titleId}");

            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }
        }
    }
}
