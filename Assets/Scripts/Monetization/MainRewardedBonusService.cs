using System;
using UnityEngine;

namespace MahjongGame.Monetization
{
    public static class MainRewardedBonusService
    {
        public const int RewardAmount = 1;
        public const int DailyLimit = 3;

        private const string ClaimDatePrefix = "main_rewarded_bonus_date_";
        private const string ClaimCountPrefix = "main_rewarded_bonus_count_";
        private static bool requestInProgress;

        public static bool IsRequestInProgress => requestInProgress;
        public static bool IsProfileReady => HasCurrencyProfile();

        public static int GetClaimsToday()
        {
            string profileKey = GetProfileKey();
            if (!string.Equals(PlayerPrefs.GetString(ClaimDatePrefix + profileKey, string.Empty), GetTodayKey(), StringComparison.Ordinal))
                return 0;

            return Mathf.Clamp(PlayerPrefs.GetInt(ClaimCountPrefix + profileKey, 0), 0, DailyLimit);
        }

        public static int GetRemainingClaimsToday()
        {
            return Mathf.Max(0, DailyLimit - GetClaimsToday());
        }

        public static bool CanClaim()
        {
            return HasCurrencyProfile() && GetRemainingClaimsToday() > 0;
        }

        public static RewardedAdAvailability GetAvailability()
        {
            return MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.MainBonusRewardedPlacementId);
        }

        public static void TryClaim(Action<bool, string> onComplete)
        {
            if (requestInProgress)
            {
                onComplete?.Invoke(false, "main.reward_bonus.opening");
                return;
            }

            if (!HasCurrencyProfile())
            {
                onComplete?.Invoke(false, "main.reward_bonus.profile_unavailable");
                return;
            }

            if (GetRemainingClaimsToday() <= 0)
            {
                onComplete?.Invoke(false, "main.reward_bonus.limit");
                return;
            }

            RewardedAdAvailability availability = GetAvailability();
            if (!availability.IsReady)
            {
                onComplete?.Invoke(false, string.IsNullOrWhiteSpace(availability.Message) ? "shop.ad_not_ready" : availability.Message);
                return;
            }

            PlayerProfile expectedProfile = ProfileService.I.Current;
            string expectedProfileKey = GetProfileKey(expectedProfile);
            requestInProgress = true;
            MonetizationService.Ensure().ShowRewardedAd(MonetizationService.MainBonusRewardedPlacementId, result =>
            {
                try
                {
                    if (!result.IsCompleted)
                    {
                        onComplete?.Invoke(false, string.IsNullOrWhiteSpace(result.Message) ? "main.reward_bonus.not_completed" : result.Message);
                        return;
                    }

                    if (!IsExpectedProfileActive(expectedProfile, expectedProfileKey))
                    {
                        onComplete?.Invoke(false, "main.reward_bonus.profile_unavailable");
                        return;
                    }

                    expectedProfile.EnsureData();
                    int balanceBefore = expectedProfile.Currencies.OzAmetist;
                    Exception rewardException = null;
                    try
                    {
                        CurrencyService.I.AddOzAmetist(RewardAmount);
                    }
                    catch (Exception exception)
                    {
                        rewardException = exception;
                        Debug.LogException(exception);
                    }

                    bool rewardApplied = expectedProfile.Currencies.OzAmetist > balanceBefore;
                    if (!rewardApplied)
                    {
                        onComplete?.Invoke(false, "main.reward_bonus.profile_unavailable");
                        return;
                    }

                    int newCount = Mathf.Clamp(GetClaimsToday() + 1, 1, DailyLimit);
                    try
                    {
                        PlayerPrefs.SetString(ClaimDatePrefix + expectedProfileKey, GetTodayKey());
                        PlayerPrefs.SetInt(ClaimCountPrefix + expectedProfileKey, newCount);
                        PlayerPrefs.Save();
                    }
                    finally
                    {
                        if (rewardException != null)
                            Debug.LogWarning("[MainRewardedBonusService] Reward was saved, but a currency notification listener failed.");
                    }

                    onComplete?.Invoke(true, "main.reward_bonus.received");
                }
                finally
                {
                    requestInProgress = false;
                }
            });
        }

        private static bool HasCurrencyProfile()
        {
            return CurrencyService.I != null && ProfileService.I != null && ProfileService.I.Current != null;
        }

        private static string GetProfileKey()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            return GetProfileKey(profile);
        }

        private static string GetProfileKey(PlayerProfile profile)
        {
            if (profile == null)
                return "default";

            profile.EnsureData();
            return string.IsNullOrWhiteSpace(profile.LocalProfileId) ? "default" : profile.LocalProfileId;
        }

        private static bool IsExpectedProfileActive(PlayerProfile expectedProfile, string expectedProfileKey)
        {
            PlayerProfile currentProfile = ProfileService.I != null ? ProfileService.I.Current : null;
            return expectedProfile != null &&
                   ReferenceEquals(currentProfile, expectedProfile) &&
                   string.Equals(GetProfileKey(currentProfile), expectedProfileKey, StringComparison.Ordinal);
        }

        private static string GetTodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
    }
}
