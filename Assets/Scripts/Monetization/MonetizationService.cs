using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame.Monetization
{
    [DisallowMultipleComponent]
    public sealed class MonetizationService : MonoBehaviour
    {
        public const string AmetistRewardedPlacementId = "shop_oz_ametist_rewarded";
        // Voluntary Main-menu bonus, capped by MainRewardedBonusService.
        public const string MainBonusRewardedPlacementId = "main_bonus_rewarded";
        public const string EnergyRewardedPlacementId = "battle_energy_rewarded";
        public const string WeeklyRewardedPlacementId = "weekly_rewarded";
        public const string DailyHeroBoostRewardedPlacementId = "daily_hero_boost_rewarded";
        public const string BattleTilePackRewardedPlacementId = "battle_tile_pack_rewarded";
        public const string MahjongAssistRewardedPlacementId = "mahjong_assist_rewarded";
        public const string SudokuHintRewardedPlacementId = "sudoku_hint_rewarded";
        public const string SudokuUndoRewardedPlacementId = "sudoku_undo_rewarded";
        public const string SymbiGridRerollRewardedPlacementId = "symbigrid_reroll_rewarded";
        public const string SymbiGridSecondChanceRewardedPlacementId = "symbigrid_second_chance_rewarded";
        public const string SymbiMineSecondChanceRewardedPlacementId = "symbimine_second_chance_rewarded";
        public const string SymbiGridInterstitialPlacementId = "symbigrid_interstitial";
        public const string SudokuInterstitialPlacementId = "sudoku_interstitial";
        public const string MatchEndInterstitialPlacementId = "match_end_interstitial";
        public const string SurrenderInterstitialPlacementId = "battle_surrender_interstitial";

        public static MonetizationService I { get; private set; }

        [Header("Editor Simulation")]
        [SerializeField] private bool simulateRewardedAdsInEditor = true;
        [SerializeField] private bool simulateInterstitialAdsInEditor = true;
        [SerializeField] private bool simulatePurchasesInEditor = false;

        private IRewardedAdProvider rewardedAdProvider;
        private IInterstitialAdProvider interstitialAdProvider;
        private IPurchaseProvider purchaseProvider;
        private readonly List<MonetizationProduct> products = new List<MonetizationProduct>();

        public bool IsRewardedAdsReady => rewardedAdProvider != null && rewardedAdProvider.IsInitialized;
        public bool IsInterstitialAdsReady => interstitialAdProvider != null && interstitialAdProvider.IsInitialized;
        public bool IsPurchasesReady => ArePurchasesSupported && purchaseProvider != null && purchaseProvider.IsInitialized;

        public static bool ArePurchasesSupported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeRuntime()
        {
            if (Application.isBatchMode)
                return;

            Ensure();
        }

        public static MonetizationService Ensure()
        {
            if (I != null)
                return I;

            MonetizationService existing = FindAnyObjectByType<MonetizationService>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(true);

                return existing;
            }

            GameObject serviceObject = new GameObject("MonetizationService");
            return serviceObject.AddComponent<MonetizationService>();
        }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
            ConfigureDefaultProviders();
        }

        public void SetRewardedAdProvider(IRewardedAdProvider provider)
        {
            rewardedAdProvider = provider;
            rewardedAdProvider?.Initialize();
        }

        public void SetInterstitialAdProvider(IInterstitialAdProvider provider)
        {
            interstitialAdProvider = provider;
            interstitialAdProvider?.Initialize();
        }

        public void SetPurchaseProvider(IPurchaseProvider provider)
        {
            purchaseProvider = provider;
            purchaseProvider?.Initialize(products);
        }

        public void RegisterProducts(IEnumerable<MonetizationProduct> sourceProducts)
        {
            products.Clear();

            if (sourceProducts != null)
            {
                foreach (MonetizationProduct product in sourceProducts)
                {
                    if (product != null && !string.IsNullOrWhiteSpace(product.ProductId))
                        products.Add(product);
                }
            }

            if (purchaseProvider != null)
            {
                try
                {
                    purchaseProvider.Initialize(products);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[MonetizationService] Purchase provider initialization failed: {exception}");
                    purchaseProvider = new StubPurchaseProvider(false);
                    purchaseProvider.Initialize(products);
                }
            }
        }

        public MonetizationProduct GetProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return null;

            for (int i = 0; i < products.Count; i++)
            {
                if (products[i] != null && string.Equals(products[i].ProductId, productId, StringComparison.Ordinal))
                    return products[i];
            }

            return null;
        }

        public bool CanShowRewardedAd(string placementId)
        {
            return rewardedAdProvider != null && rewardedAdProvider.IsRewardedAdReady(placementId);
        }

        public RewardedAdAvailability GetRewardedAdAvailability(string placementId)
        {
            if (rewardedAdProvider == null)
                return new RewardedAdAvailability(RewardedAdAvailabilityState.Unavailable, placementId, "shop.ad_not_ready");

            return rewardedAdProvider.GetRewardedAdAvailability(placementId);
        }

        public void ShowRewardedAd(string placementId, Action<RewardedAdResult> onComplete)
        {
            if (rewardedAdProvider == null)
            {
                onComplete?.Invoke(new RewardedAdResult(RewardedAdState.NotReady, placementId, "Rewarded ads provider is missing."));
                return;
            }

            rewardedAdProvider.ShowRewardedAd(placementId, onComplete);
        }

        public bool CanShowInterstitialAd(string placementId)
        {
            return interstitialAdProvider != null && interstitialAdProvider.IsInterstitialReady(placementId);
        }

        public void ShowInterstitialAd(string placementId, Action<InterstitialAdResult> onComplete)
        {
            if (interstitialAdProvider == null)
            {
                onComplete?.Invoke(new InterstitialAdResult(InterstitialAdState.NotReady, placementId, "Interstitial provider is missing."));
                return;
            }

            interstitialAdProvider.ShowInterstitial(placementId, onComplete);
        }

        public bool CanPurchase(string productId)
        {
            return ArePurchasesSupported && purchaseProvider != null && purchaseProvider.CanPurchase(productId);
        }

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            if (!ArePurchasesSupported)
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseState.NotReady, productId, "shop.purchase_not_ready"));
                return;
            }

            if (purchaseProvider == null)
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseState.NotReady, productId, "Purchase provider is missing."));
                return;
            }

            purchaseProvider.Purchase(productId, onComplete);
        }

        private void ConfigureDefaultProviders()
        {
#if UNITY_EDITOR
            SetRewardedAdProvider(new StubRewardedAdProvider(simulateRewardedAdsInEditor));
            SetInterstitialAdProvider(new StubInterstitialAdProvider(simulateInterstitialAdsInEditor));
            SetPurchaseProvider(new StubPurchaseProvider(simulatePurchasesInEditor));
#else
            if (Application.isBatchMode)
            {
                SetRewardedAdProvider(new StubRewardedAdProvider(false));
                SetInterstitialAdProvider(new StubInterstitialAdProvider(false));
                SetPurchaseProvider(new StubPurchaseProvider(false));
                return;
            }

#if !(UNITY_ANDROID || UNITY_IOS)
            SetRewardedAdProvider(new StubRewardedAdProvider(false));
            SetInterstitialAdProvider(new StubInterstitialAdProvider(false));
            SetPurchaseProvider(new StubPurchaseProvider(false));
            return;
#endif

            try
            {
                GoogleMobileAdsProvider adsProvider = new GoogleMobileAdsProvider();
                SetRewardedAdProvider(adsProvider);
                SetInterstitialAdProvider(adsProvider);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MonetizationService] Google Mobile Ads initialization failed: {exception}");
                SetRewardedAdProvider(new StubRewardedAdProvider(false));
                SetInterstitialAdProvider(new StubInterstitialAdProvider(false));
            }

#if UNITY_ANDROID
            try
            {
                SetPurchaseProvider(new UnityIapPurchaseProvider());
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MonetizationService] Unity IAP initialization failed: {exception}");
                SetPurchaseProvider(new StubPurchaseProvider(false));
            }
#else
            SetPurchaseProvider(new StubPurchaseProvider(false));
#endif
#endif
        }
    }
}
