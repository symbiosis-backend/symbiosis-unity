using System;
using System.Collections.Generic;

namespace MahjongGame
{
    [Serializable]
    public sealed class WeeklyRewardData
    {
        public List<WeeklyRewardDayData> Days = new();
        public int CurrentDayIndex;
        public long CycleStartUtcTicks;
        public long LastClaimUtcTicks;
        public long LastSeenUtcTicks;
        public bool TimeCheatDetected;

        public void EnsureValid()
        {
            if (Days == null)
                Days = new List<WeeklyRewardDayData>();

            if (Days.Count != 7)
            {
                Days.Clear();
                for (int i = 0; i < 7; i++)
                    Days.Add(new WeeklyRewardDayData(i + 1));
            }

            if (CurrentDayIndex < 0)
                CurrentDayIndex = 0;

            if (CurrentDayIndex > 6)
                CurrentDayIndex = 6;
        }

        public void ResetCycle(long utcTicks)
        {
            EnsureValid();

            for (int i = 0; i < 7; i++)
                Days[i].Reset(i + 1);

            CurrentDayIndex = 0;
            CycleStartUtcTicks = utcTicks;
            LastClaimUtcTicks = 0;
            LastSeenUtcTicks = utcTicks;
            TimeCheatDetected = false;
        }

        public WeeklyRewardDayData GetCurrentDay()
        {
            EnsureValid();
            return Days[CurrentDayIndex];
        }
    }
}