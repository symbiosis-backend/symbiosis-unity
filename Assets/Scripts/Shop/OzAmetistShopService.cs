using System;
using UnityEngine;

namespace MahjongGame
{
    public static class OzAmetistShopService
    {
        public const int FreeAmetistAmount = 5;
        public const int RewardedAdAmetistAmount = 10;
        public const int DailyRewardedAdLimit = 5;

        private const string FreeClaimPrefix = "shop_ozametist_free_claimed_";
        private const string AdDatePrefix = "shop_ozametist_ad_date_";
        private const string AdCountPrefix = "shop_ozametist_ad_count_";

        public static bool HasClaimedFree()
        {
            return PlayerPrefs.GetInt(FreeClaimPrefix + GetProfileKey(), 0) == 1;
        }

        public static bool CanClaimFree()
        {
            return !HasClaimedFree() && HasCurrencyProfile();
        }

        public static bool TryClaimFree()
        {
            if (!CanClaimFree())
                return false;

            CurrencyService.I.AddOzAmetist(FreeAmetistAmount);
            PlayerPrefs.SetInt(FreeClaimPrefix + GetProfileKey(), 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int GetDailyAdClaims()
        {
            string key = GetProfileKey();
            string today = GetTodayKey();
            string storedDate = PlayerPrefs.GetString(AdDatePrefix + key, string.Empty);

            if (!string.Equals(storedDate, today, StringComparison.Ordinal))
                return 0;

            return Mathf.Clamp(PlayerPrefs.GetInt(AdCountPrefix + key, 0), 0, DailyRewardedAdLimit);
        }

        public static int GetRemainingDailyAdClaims()
        {
            return Mathf.Max(0, DailyRewardedAdLimit - GetDailyAdClaims());
        }

        public static bool CanClaimRewardedAd()
        {
            return HasCurrencyProfile() && GetRemainingDailyAdClaims() > 0;
        }

        public static bool TryClaimRewardedAd()
        {
            if (!CanClaimRewardedAd())
                return false;

            string key = GetProfileKey();
            int newCount = GetDailyAdClaims() + 1;

            CurrencyService.I.AddOzAmetist(RewardedAdAmetistAmount);
            PlayerPrefs.SetString(AdDatePrefix + key, GetTodayKey());
            PlayerPrefs.SetInt(AdCountPrefix + key, newCount);
            PlayerPrefs.Save();
            return true;
        }

        private static string GetProfileKey()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return "default";

            profile.EnsureData();
            return string.IsNullOrWhiteSpace(profile.LocalProfileId) ? "default" : profile.LocalProfileId;
        }

        private static bool HasCurrencyProfile()
        {
            return CurrencyService.I != null && ProfileService.I != null && ProfileService.I.Current != null;
        }

        private static string GetTodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
    }
}
