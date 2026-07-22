using System;
using System.Collections.Generic;
using UnityEngine;
using MahjongGame.Monetization;

namespace MahjongGame
{
    public static class OzAmetistShopService
    {
        public const string ProductSmall = "oz_ametist_small";
        public const string ProductMedium = "oz_ametist_medium";
        public const string ProductBig = "oz_ametist_big";
        public const string ProductLegend = "oz_ametist_legend";
        public const string ProductWeeklyNoAds = "weekly_no_ads";

        public const int FreeAmetistAmount = 5;
        public const int RewardedAdAmetistAmount = 10;
        public const int DailyRewardedAdLimit = 6;

        private const string FreeClaimPrefix = "shop_ozametist_free_claimed_";
        private const string AdDatePrefix = "shop_ozametist_ad_date_";
        private const string AdCountPrefix = "shop_ozametist_ad_count_";
        private static readonly MonetizationProduct[] AmetistProducts =
        {
            new MonetizationProduct(ProductSmall, ProductSmall, 50, "$0.99"),
            new MonetizationProduct(ProductMedium, ProductMedium, 120, "$1.99"),
            new MonetizationProduct(ProductBig, ProductBig, 300, "$4.99"),
            new MonetizationProduct(ProductLegend, ProductLegend, 700, "$9.99"),
            new MonetizationProduct(ProductWeeklyNoAds, ProductWeeklyNoAds, 0, "$2.29")
        };
        private static MonetizationService catalogRegisteredForService;

        public static IReadOnlyList<MonetizationProduct> Products => AmetistProducts;

        public static void EnsureCatalogRegistered()
        {
            MonetizationService service = MonetizationService.Ensure();
            if (catalogRegisteredForService == service)
                return;

            service.RegisterProducts(AmetistProducts);
            catalogRegisteredForService = service;
        }

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

        public static bool CanStartRewardedAd()
        {
            MonetizationService service = MonetizationService.Ensure();
            return CanClaimRewardedAd() && service.CanShowRewardedAd(MonetizationService.AmetistRewardedPlacementId);
        }

        public static void TryClaimRewardedAd(Action<bool, string> onComplete)
        {
            if (!CanClaimRewardedAd())
            {
                onComplete?.Invoke(false, "shop.ad_limit");
                return;
            }

            MonetizationService service = MonetizationService.Ensure();
            service.ShowRewardedAd(MonetizationService.AmetistRewardedPlacementId, result =>
            {
                if (!result.IsCompleted)
                {
                    onComplete?.Invoke(false, string.IsNullOrWhiteSpace(result.Message) ? "shop.ad_not_ready" : result.Message);
                    return;
                }

                string key = GetProfileKey();
                int newCount = GetDailyAdClaims() + 1;

                CurrencyService.I.AddOzAmetist(RewardedAdAmetistAmount);
                PlayerPrefs.SetString(AdDatePrefix + key, GetTodayKey());
                PlayerPrefs.SetInt(AdCountPrefix + key, newCount);
                PlayerPrefs.Save();
                onComplete?.Invoke(true, string.Empty);
            });
        }

        public static bool CanPurchase(string productId)
        {
            if (!MonetizationService.ArePurchasesSupported)
                return false;

            EnsureCatalogRegistered();
            return HasCurrencyProfile() && MonetizationService.Ensure().CanPurchase(productId);
        }

        public static void TryPurchaseAmetistPackage(string productId, Action<bool, int, string> onComplete)
        {
            if (!MonetizationService.ArePurchasesSupported)
            {
                onComplete?.Invoke(false, 0, "shop.purchase_not_ready");
                return;
            }

            EnsureCatalogRegistered();

            MonetizationProduct product = MonetizationService.Ensure().GetProduct(productId);
            if (product == null)
            {
                onComplete?.Invoke(false, 0, "shop.purchase_unknown");
                return;
            }

            if (!HasCurrencyProfile())
            {
                onComplete?.Invoke(false, 0, "profile.error.setup_failed");
                return;
            }

            MonetizationService.Ensure().Purchase(productId, result =>
            {
                if (!result.IsPurchased)
                {
                    onComplete?.Invoke(false, 0, string.IsNullOrWhiteSpace(result.Message) ? "shop.purchase_not_ready" : result.Message);
                    return;
                }

                onComplete?.Invoke(true, product.OzAmetistAmount, string.Empty);
            });
        }

        public static void TryPurchaseWeeklyNoAds(Action<bool, int, string> onComplete)
        {
            if (!MonetizationService.ArePurchasesSupported)
            {
                onComplete?.Invoke(false, 0, "shop.purchase_not_ready");
                return;
            }

            EnsureCatalogRegistered();

            MonetizationProduct product = MonetizationService.Ensure().GetProduct(ProductWeeklyNoAds);
            if (product == null)
            {
                onComplete?.Invoke(false, 0, "shop.purchase_unknown");
                return;
            }

            if (!HasCurrencyProfile())
            {
                onComplete?.Invoke(false, 0, "profile.error.setup_failed");
                return;
            }

            MonetizationService.Ensure().Purchase(ProductWeeklyNoAds, result =>
            {
                if (!result.IsPurchased)
                {
                    onComplete?.Invoke(false, 0, string.IsNullOrWhiteSpace(result.Message) ? "shop.purchase_not_ready" : result.Message);
                    return;
                }

                onComplete?.Invoke(true, NoAdsService.WeeklyNoAdsDays, string.Empty);
            });
        }

        public static MonetizationProduct GetProduct(string productId)
        {
            EnsureCatalogRegistered();
            return MonetizationService.Ensure().GetProduct(productId);
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
