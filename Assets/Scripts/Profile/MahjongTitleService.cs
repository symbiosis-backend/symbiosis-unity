using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongTitleService : MonoBehaviour
    {
        public static MahjongTitleService I { get; private set; }

        public const string TitleNovice = "novice";

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

            // Add new story title unlock checks here.
            // TryUnlockSomething(profile, story);
            // TryUnlockMaster(profile, story);
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

                default:
                    return titleId;
            }
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
                mahjong.SetSelectedTitle(titleId);

            Debug.Log($"[MahjongTitleService] Title unlocked: {titleId}");

            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }
        }
    }
}
