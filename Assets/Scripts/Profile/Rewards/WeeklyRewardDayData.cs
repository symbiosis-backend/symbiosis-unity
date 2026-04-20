using System;

namespace MahjongGame
{
    [Serializable]
    public sealed class WeeklyRewardDayData
    {
        public int DayNumber;
        public bool Claimed;
        public WeeklyRewardClaimType ClaimType;

        public WeeklyRewardDayData()
        {
            DayNumber = 1;
            Claimed = false;
            ClaimType = WeeklyRewardClaimType.None;
        }

        public WeeklyRewardDayData(int dayNumber)
        {
            DayNumber = dayNumber;
            Claimed = false;
            ClaimType = WeeklyRewardClaimType.None;
        }

        public void Reset(int dayNumber)
        {
            DayNumber = dayNumber;
            Claimed = false;
            ClaimType = WeeklyRewardClaimType.None;
        }
    }
}