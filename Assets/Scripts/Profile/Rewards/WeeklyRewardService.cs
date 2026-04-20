using System;

namespace MahjongGame
{
    public static class WeeklyRewardService
    {
        private static readonly int[] FreeAltinRewards = { 1000, 1200, 1400, 1600, 1800, 2000, 2500 };
        private static readonly int[] AdAmetistRewards = { 5, 5, 6, 6, 7, 8, 10 };

        public static void EnsureInitialized(PlayerProfile profile)
        {
            if (profile == null)
                return;

            profile.EnsureData();
            profile.WeeklyReward.EnsureValid();

            if (profile.WeeklyReward.CycleStartUtcTicks <= 0)
            {
                long nowTicks = TimeService.GetUtcNowTicks();
                profile.WeeklyReward.ResetCycle(nowTicks);
            }

            TimeService.ValidateAndUpdate(profile.WeeklyReward);
        }

        public static bool CanClaimToday(PlayerProfile profile)
        {
            if (profile == null)
                return false;

            EnsureInitialized(profile);

            WeeklyRewardData data = profile.WeeklyReward;
            if (data.TimeCheatDetected)
                return false;

            if (!TimeService.ValidateAndUpdate(data))
                return false;

            if (data.LastClaimUtcTicks <= 0)
                return true;

            int lastClaimDay = TimeService.GetUtcDayNumber(TimeService.TicksToUtc(data.LastClaimUtcTicks));
            int nowDay = TimeService.GetUtcDayNumberNow();

            return nowDay > lastClaimDay;
        }

        public static WeeklyRewardClaimType GetTodayClaimType(PlayerProfile profile)
        {
            if (profile == null)
                return WeeklyRewardClaimType.None;

            EnsureInitialized(profile);

            WeeklyRewardData data = profile.WeeklyReward;
            WeeklyRewardDayData currentDay = data.GetCurrentDay();
            return currentDay != null ? currentDay.ClaimType : WeeklyRewardClaimType.None;
        }

        public static int GetCurrentDayNumber(PlayerProfile profile)
        {
            if (profile == null)
                return 1;

            EnsureInitialized(profile);
            return profile.WeeklyReward.CurrentDayIndex + 1;
        }

        public static bool IsTimeBlocked(PlayerProfile profile)
        {
            if (profile == null)
                return true;

            EnsureInitialized(profile);
            return profile.WeeklyReward.TimeCheatDetected;
        }

        public static int GetFreeAltin(PlayerProfile profile)
        {
            int index = GetSafeDayIndex(profile);
            return FreeAltinRewards[index];
        }

        public static int GetAdAltin(PlayerProfile profile)
        {
            int index = GetSafeDayIndex(profile);
            return FreeAltinRewards[index] * 2;
        }

        public static int GetAdAmetist(PlayerProfile profile)
        {
            int index = GetSafeDayIndex(profile);
            return AdAmetistRewards[index];
        }

        public static bool ClaimFree(PlayerProfile profile)
        {
            if (!CanClaimToday(profile))
                return false;

            WeeklyRewardData data = profile.WeeklyReward;
            WeeklyRewardDayData day = data.GetCurrentDay();
            if (day == null || day.Claimed)
                return false;

            int altin = GetFreeAltin(profile);
            profile.Currencies.AddAltin(altin);

            day.Claimed = true;
            day.ClaimType = WeeklyRewardClaimType.Free;

            FinalizeClaim(data);
            return true;
        }

        public static bool ClaimAd(PlayerProfile profile)
        {
            if (!CanClaimToday(profile))
                return false;

            WeeklyRewardData data = profile.WeeklyReward;
            WeeklyRewardDayData day = data.GetCurrentDay();
            if (day == null || day.Claimed)
                return false;

            int altin = GetAdAltin(profile);
            int ametist = GetAdAmetist(profile);

            profile.Currencies.AddAltin(altin);
            profile.Currencies.AddAmetist(ametist);

            day.Claimed = true;
            day.ClaimType = WeeklyRewardClaimType.Ad;

            FinalizeClaim(data);
            return true;
        }

        public static bool IsDayClaimed(PlayerProfile profile, int dayIndex)
        {
            if (profile == null)
                return false;

            EnsureInitialized(profile);

            if (dayIndex < 0 || dayIndex >= 7)
                return false;

            return profile.WeeklyReward.Days[dayIndex].Claimed;
        }

        public static WeeklyRewardClaimType GetDayClaimType(PlayerProfile profile, int dayIndex)
        {
            if (profile == null)
                return WeeklyRewardClaimType.None;

            EnsureInitialized(profile);

            if (dayIndex < 0 || dayIndex >= 7)
                return WeeklyRewardClaimType.None;

            return profile.WeeklyReward.Days[dayIndex].ClaimType;
        }

        public static bool IsDayCurrent(PlayerProfile profile, int dayIndex)
        {
            if (profile == null)
                return false;

            EnsureInitialized(profile);
            return profile.WeeklyReward.CurrentDayIndex == dayIndex;
        }

        public static bool IsDayLocked(PlayerProfile profile, int dayIndex)
        {
            if (profile == null)
                return true;

            EnsureInitialized(profile);

            if (dayIndex < 0 || dayIndex >= 7)
                return true;

            return dayIndex > profile.WeeklyReward.CurrentDayIndex;
        }

        public static void ForceResetCycle(PlayerProfile profile)
        {
            if (profile == null)
                return;

            profile.EnsureData();
            profile.WeeklyReward.ResetCycle(TimeService.GetUtcNowTicks());
        }

        private static int GetSafeDayIndex(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            EnsureInitialized(profile);
            int index = profile.WeeklyReward.CurrentDayIndex;

            if (index < 0)
                index = 0;
            if (index > 6)
                index = 6;

            return index;
        }

        private static void FinalizeClaim(WeeklyRewardData data)
        {
            DateTime now = TimeService.GetUtcNow();
            data.LastClaimUtcTicks = now.Ticks;
            data.LastSeenUtcTicks = now.Ticks;

            if (data.CurrentDayIndex >= 6)
            {
                data.ResetCycle(now.Ticks);
                return;
            }

            data.CurrentDayIndex++;
        }
    }
}