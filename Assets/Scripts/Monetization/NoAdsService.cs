using System;
using UnityEngine;

namespace MahjongGame.Monetization
{
    public static class NoAdsService
    {
        public const int WeeklyNoAdsDays = 7;

        public static bool HasActiveNoAds()
        {
            PlayerProfile profile = GetProfile();
            return profile != null && profile.HasActiveNoAds;
        }

        public static int GetRemainingDays()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null || profile.Ads == null)
                return 0;

            return profile.Ads.GetRemainingDays();
        }

        public static void GrantWeeklyNoAds()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Ads.ExtendNoAds(TimeSpan.FromDays(WeeklyNoAdsDays));
            SaveAndNotify();
        }

        private static PlayerProfile GetProfile()
        {
            if (ProfileService.I == null || ProfileService.I.Current == null)
                ProfileRuntimeBootstrap.TryGetProfile();

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
                profile.EnsureData();

            return profile;
        }

        private static void SaveAndNotify()
        {
            if (ProfileService.I == null)
                return;

            ProfileService.I.Save();
            ProfileService.I.NotifyProfileChanged();
        }
    }
}
