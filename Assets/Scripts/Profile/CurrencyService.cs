using System;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class CurrencyService : MonoBehaviour
    {
        public static CurrencyService I { get; private set; }

        public static event Action CurrencyChanged;
        private bool loggedMissingProfileService;

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

        public int GetOzAltin()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Currencies.OzAltin;
        }

        public int GetOzAmetist()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            return profile.Currencies.OzAmetist;
        }

        public void AddOzAltin(int amount)
        {
            if (amount <= 0)
                return;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.AddAltin(amount);
            SaveProfile();
            NotifyCurrencyChanged();
        }

        public bool SpendOzAltin(int amount)
        {
            if (amount < 0)
                return false;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();

            bool success = profile.Currencies.SpendAltin(amount);
            if (success)
            {
                SaveProfile();
                NotifyCurrencyChanged();
            }

            return success;
        }

        public bool CanSpendOzAltin(int amount)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            return profile.Currencies.CanSpendAltin(amount);
        }

        public void AddOzAmetist(int amount)
        {
            if (amount <= 0)
                return;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.AddAmetist(amount);
            SaveProfile();
            NotifyCurrencyChanged();
        }

        public bool SpendOzAmetist(int amount)
        {
            if (amount < 0)
                return false;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();

            bool success = profile.Currencies.SpendAmetist(amount);
            if (success)
            {
                SaveProfile();
                NotifyCurrencyChanged();
            }

            return success;
        }

        public bool CanSpendOzAmetist(int amount)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            return profile.Currencies.CanSpendAmetist(amount);
        }

        public void SetOzAltin(int value)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.OzAltin = Mathf.Max(0, value);
            SaveProfile();
            NotifyCurrencyChanged();
        }

        public void SetOzAmetist(int value)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.OzAmetist = Mathf.Max(0, value);
            SaveProfile();
            NotifyCurrencyChanged();
        }

        private PlayerProfile GetProfile()
        {
            if (ProfileService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            if (ProfileService.I == null)
            {
                if (!loggedMissingProfileService)
                {
                    Debug.LogWarning("[CurrencyService] ProfileService not found.");
                    loggedMissingProfileService = true;
                }

                return null;
            }

            PlayerProfile profile = ProfileService.I.Current;
            if (profile == null)
            {
                ProfileRuntimeBootstrap.TryLoadCachedProfile();
                profile = ProfileService.I.Current;
            }

            if (profile == null)
            {
                return null;
            }

            loggedMissingProfileService = false;
            return profile;
        }

        private void SaveProfile()
        {
            if (ProfileService.I != null)
            {
                ProfileService.I.Save();
                ProfileService.I.NotifyProfileChanged();
            }
        }

        private void NotifyCurrencyChanged()
        {
            CurrencyChanged?.Invoke();
        }
    }
}
