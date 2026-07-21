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
        private const int OwnerCurrencyDisplayBalance = 2000000000;
        private const string OwnerAccountEmail = "mykhaylov.artem@gmail.com";

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
            if (HasInfiniteCurrency(profile))
                return OwnerCurrencyDisplayBalance;

            return profile.Currencies.OzAltin;
        }

        public int GetOzAmetist()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return OwnerCurrencyDisplayBalance;

            return profile.Currencies.OzAmetist;
        }

        public int GetOzTile()
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return OwnerCurrencyDisplayBalance;

            return profile.Currencies.OzTile;
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
            if (HasInfiniteCurrency(profile))
                return true;

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
            if (HasInfiniteCurrency(profile))
                return true;

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

        public void AddOzTile(int amount)
        {
            if (amount <= 0)
                return;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.AddTile(amount);
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
            if (HasInfiniteCurrency(profile))
                return true;

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
            if (HasInfiniteCurrency(profile))
                return true;

            return profile.Currencies.CanSpendAmetist(amount);
        }

        public bool SpendOzTile(int amount)
        {
            if (amount < 0)
                return false;

            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return true;

            bool success = profile.Currencies.SpendTile(amount);
            if (success)
            {
                SaveProfile();
                NotifyCurrencyChanged();
            }

            return success;
        }

		internal bool SpendOzTileWithoutSave(PlayerProfile expectedProfile, int amount)
		{
			if (amount < 0 || expectedProfile == null)
				return false;

			PlayerProfile currentProfile = GetProfile();
			if (!ReferenceEquals(currentProfile, expectedProfile))
				return false;

			expectedProfile.EnsureData();
			if (HasInfiniteCurrency(expectedProfile))
				return true;

			return expectedProfile.Currencies.SpendTile(amount);
		}

		internal void NotifyOzTileChangedAfterSave(PlayerProfile expectedProfile)
		{
			if (expectedProfile != null && ReferenceEquals(GetProfile(), expectedProfile))
				NotifyCurrencyChanged();
		}

        public bool CanSpendOzTile(int amount)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return true;

            return profile.Currencies.CanSpendTile(amount);
        }

        public int GetCurrency(string currencyId)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return 0;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return OwnerCurrencyDisplayBalance;

            return profile.Currencies.GetCurrency(currencyId);
        }

        public bool CanSpendCurrency(string currencyId, int amount)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return false;

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
                return true;

            return profile.Currencies.CanSpendCurrency(currencyId, amount);
        }

        public bool TryExchangeCurrency(string fromCurrencyId, string toCurrencyId, int amount, out ExchangeQuote quote)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
            {
                quote = new ExchangeQuote
                {
                    Success = false,
                    FromCurrencyId = fromCurrencyId,
                    ToCurrencyId = toCurrencyId,
                    AmountIn = Mathf.Max(0, amount),
                    FailReason = "Profile is not loaded."
                };
                return false;
            }

            profile.EnsureData();
            if (HasInfiniteCurrency(profile))
            {
                quote = ExchangeMarketService.GetExchangeQuote(profile, fromCurrencyId, toCurrencyId, amount);
                return quote.Success;
            }

            bool success = ExchangeMarketService.ExchangeCurrency(profile, fromCurrencyId, toCurrencyId, amount, out quote);
            SaveProfile();
            NotifyCurrencyChanged();
            return success;
        }

        public ExchangeQuote GetExchangeQuote(string fromCurrencyId, string toCurrencyId, int amount)
        {
            PlayerProfile profile = GetProfile();
            return ExchangeMarketService.GetExchangeQuote(profile, fromCurrencyId, toCurrencyId, amount);
        }

        public bool CanExchangeCurrency(string fromCurrencyId, string toCurrencyId, int amount)
        {
            PlayerProfile profile = GetProfile();
            return ExchangeMarketService.CanExchange(profile, fromCurrencyId, toCurrencyId, amount);
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

        public void SetOzTile(int value)
        {
            PlayerProfile profile = GetProfile();
            if (profile == null)
                return;

            profile.EnsureData();
            profile.Currencies.OzTile = Mathf.Max(0, value);
            SaveProfile();
            NotifyCurrencyChanged();
        }

        public bool TryExchangeOzTileToOzAltin(int ozTileAmount, int ozAltinPerOzTile)
        {
            return TryExchangeCurrency(CurrencyIds.OzTile, CurrencyIds.OzAltin, ozTileAmount, out _);
        }

        public bool TryExchangeOzAltinToOzTile(int ozAltinAmount, int ozAltinPerOzTile)
        {
            return TryExchangeCurrency(CurrencyIds.OzAltin, CurrencyIds.OzTile, ozAltinAmount, out _);
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

        private static bool HasInfiniteCurrency(PlayerProfile profile)
        {
            if (profile == null)
                return false;

            if (profile.HasInfiniteCurrency)
                return true;

            string email = profile.AccountEmail != null ? profile.AccountEmail.Trim() : string.Empty;
            string displayName = profile.DisplayName != null ? profile.DisplayName.Trim() : string.Empty;
            return string.Equals(email, OwnerAccountEmail, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(displayName, "Owner", StringComparison.OrdinalIgnoreCase);
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
