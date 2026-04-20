using System;

namespace MahjongGame
{
    public static class TimeService
    {
        private static readonly TimeSpan BackwardTolerance = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan ForwardSuspiciousJump = TimeSpan.FromDays(30);

        public static DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        public static long GetUtcNowTicks()
        {
            return GetUtcNow().Ticks;
        }

        public static bool ValidateAndUpdate(WeeklyRewardData data)
        {
            if (data == null)
                return false;

            data.EnsureValid();

            DateTime now = GetUtcNow();

            if (data.LastSeenUtcTicks > 0)
            {
                DateTime lastSeen = new DateTime(data.LastSeenUtcTicks, DateTimeKind.Utc);

                if (now < lastSeen - BackwardTolerance)
                {
                    data.TimeCheatDetected = true;
                    return false;
                }

                if (now > lastSeen + ForwardSuspiciousJump)
                {
                    data.TimeCheatDetected = true;
                    return false;
                }
            }

            data.LastSeenUtcTicks = now.Ticks;
            return true;
        }

        public static bool IsTimeCheatDetected(WeeklyRewardData data)
        {
            return data != null && data.TimeCheatDetected;
        }

        public static void ClearTimeCheatFlag(WeeklyRewardData data)
        {
            if (data == null)
                return;

            data.TimeCheatDetected = false;
            data.LastSeenUtcTicks = GetUtcNowTicks();
        }

        public static DateTime TicksToUtc(long ticks)
        {
            if (ticks <= 0)
                return DateTime.MinValue;

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        public static int GetUtcDayNumber(DateTime utcTime)
        {
            return (int)(utcTime.Date - DateTime.UnixEpoch.Date).TotalDays;
        }

        public static int GetUtcDayNumberNow()
        {
            return GetUtcDayNumber(GetUtcNow());
        }
    }
}