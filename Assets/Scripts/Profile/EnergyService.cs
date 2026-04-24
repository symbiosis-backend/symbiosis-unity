using System;
using UnityEngine;

namespace MahjongGame
{
    public static class EnergyService
    {
        public const int MatchEnergyCost = 10;
        public const int RewardedAdEnergyAmount = 20;
        public const int MaxEnergy = PlayerEnergyData.DefaultMaxEnergy;
        public const int RefillIntervalSeconds = PlayerEnergyData.DefaultRefillIntervalSeconds;
        public const string AdminInfiniteEnergyProfileName = "Blackyang";

        public static event Action EnergyChanged;

        public static bool HasProfile => ProfileService.I != null && ProfileService.I.Current != null;

        public static int CurrentEnergy
        {
            get
            {
                if (HasInfiniteEnergy())
                    return CurrentMaxEnergy;

                PlayerEnergyData energy = GetEnergy();
                return energy != null ? Mathf.Clamp(energy.CurrentEnergy, 0, energy.MaxEnergy) : 0;
            }
        }

        public static int CurrentMaxEnergy
        {
            get
            {
                PlayerEnergyData energy = GetEnergy();
                return energy != null ? Mathf.Max(1, energy.MaxEnergy) : MaxEnergy;
            }
        }

        public static bool CanStartMatch()
        {
            if (HasInfiniteEnergy())
                return true;

            return CanSpend(MatchEnergyCost);
        }

        public static bool CanSpend(int amount)
        {
            if (HasInfiniteEnergy())
                return true;

            PlayerEnergyData energy = GetEnergy();
            return energy != null && energy.CanSpend(Mathf.Max(0, amount));
        }

        public static bool TrySpendForMatch()
        {
            if (HasInfiniteEnergy())
                return true;

            return TrySpend(MatchEnergyCost);
        }

        public static bool TrySpend(int amount)
        {
            if (HasInfiniteEnergy())
                return true;

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return false;

            profile.EnsureData();
            bool spent = profile.Energy.Spend(Mathf.Max(0, amount), DateTime.UtcNow.Ticks);
            if (!spent)
                return false;

            SaveAndNotify();
            return true;
        }

        public static bool CanClaimRewardedAdEnergy()
        {
            return HasProfile && CurrentEnergy < CurrentMaxEnergy;
        }

        public static bool TryClaimRewardedAdEnergy()
        {
            if (!CanClaimRewardedAdEnergy())
                return false;

            return AddEnergy(RewardedAdEnergyAmount);
        }

        public static bool AddEnergy(int amount)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || amount <= 0)
                return false;

            profile.EnsureData();

            long nowTicks = DateTime.UtcNow.Ticks;
            PlayerEnergyData energy = profile.Energy;
            energy.Refill(nowTicks);

            int previous = energy.CurrentEnergy;
            energy.CurrentEnergy = Mathf.Min(energy.MaxEnergy, energy.CurrentEnergy + amount);
            if (energy.CurrentEnergy >= energy.MaxEnergy)
                energy.LastUpdatedUtcTicks = nowTicks;

            if (energy.CurrentEnergy == previous)
                return false;

            SaveAndNotify();
            return true;
        }

        public static int GetSecondsUntilNextEnergy()
        {
            PlayerEnergyData energy = GetEnergy();
            return energy != null ? energy.GetSecondsUntilNextEnergy(DateTime.UtcNow.Ticks) : 0;
        }

        public static string FormatTimeUntilNextEnergy()
        {
            int totalSeconds = Mathf.Max(0, GetSecondsUntilNextEnergy());
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        public static PlayerEnergyData GetEnergy()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return null;

            profile.EnsureData();
            bool refilled = profile.Energy.Refill(DateTime.UtcNow.Ticks);
            if (refilled)
                SaveAndNotify();

            return profile.Energy;
        }

        public static bool HasInfiniteEnergy()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return false;

            return string.Equals(
                profile.DisplayName != null ? profile.DisplayName.Trim() : string.Empty,
                AdminInfiniteEnergyProfileName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SaveAndNotify()
        {
            ProfileService service = ProfileService.I;
            if (service == null || service.Current == null)
                return;

            service.Save();
            service.NotifyProfileChanged();
            EnergyChanged?.Invoke();
        }
    }
}
