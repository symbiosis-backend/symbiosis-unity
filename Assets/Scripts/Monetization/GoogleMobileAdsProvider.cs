using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace MahjongGame.Monetization
{
    public sealed class GoogleMobileAdsProvider : IRewardedAdProvider, IInterstitialAdProvider
    {
#if UNITY_IOS
        // The first iOS release intentionally shares one unit per format. Placement IDs
        // remain distinct in telemetry and can be split into dedicated AdMob units later.
        private const string DefaultRewardedAdUnitId = "ca-app-pub-7247804880123488/6753866751";
        private const string MainBonusRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string BattleEnergyRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string WeeklyRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string SymbiGridRerollRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string SymbiMineSecondChanceRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string DefaultInterstitialAdUnitId = "ca-app-pub-7247804880123488/3519143416";
        private const string SymbiGridInterstitialAdUnitId = DefaultInterstitialAdUnitId;
#else
        private const string DefaultRewardedAdUnitId = "ca-app-pub-7247804880123488/6285404022";
        // Project support reuses the proven shop rewarded unit while keeping a
        // separate placement ID for its own +1 reward, daily limit and telemetry.
        private const string MainBonusRewardedAdUnitId = DefaultRewardedAdUnitId;
        private const string BattleEnergyRewardedAdUnitId = "ca-app-pub-7247804880123488/1227500538";
        private const string WeeklyRewardedAdUnitId = "ca-app-pub-7247804880123488/5000744494";
        private const string SymbiGridRerollRewardedAdUnitId = "ca-app-pub-7247804880123488/2106186485";
        private const string SymbiMineSecondChanceRewardedAdUnitId = "ca-app-pub-7247804880123488/9738003187";
        private const string DefaultInterstitialAdUnitId = "ca-app-pub-7247804880123488/4369687124";
        private const string SymbiGridInterstitialAdUnitId = "ca-app-pub-7247804880123488/4979133542";
#endif

        private readonly Dictionary<string, RewardedAd> rewardedAds = new Dictionary<string, RewardedAd>();
        private readonly Dictionary<string, float> rewardedLoadedAtTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> rewardedLoading = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> rewardedLastErrors = new Dictionary<string, string>();
        private readonly Dictionary<string, float> rewardedRetryAfterTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, InterstitialAd> interstitialAds = new Dictionary<string, InterstitialAd>();
        private readonly Dictionary<string, float> interstitialLoadedAtTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> interstitialLoading = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> interstitialRetryAfterTimes = new Dictionary<string, float>();

        private bool initializeStarted;
        private bool mobileAdsInitializeStarted;
        private bool preloadStarted;
        private bool consentFlowFinished;
        private float consentFlowStartedAt;
        private float mobileAdsInitializeStartedAt;
        private const float ConsentFallbackDelaySeconds = 8f;
        private const float MobileAdsInitializeFallbackDelaySeconds = 5f;
        private const float RewardedLoadRetryDelaySeconds = 8f;
        private const float InterstitialLoadRetryDelaySeconds = 20f;
        private const float LoadCallbackTimeoutSeconds = 24f;
        private const float FullScreenOpenTimeoutSeconds = 10f;

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (initializeStarted)
                return;

            initializeStarted = true;
#pragma warning disable CS0618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore CS0618
            consentFlowStartedAt = Time.unscaledTime;
            RequestConsentAndInitializeAds();
        }

        private void RequestConsentAndInitializeAds()
        {
            ConsentInformation.Update(new ConsentRequestParameters(), error =>
            {
                if (error != null)
                {
                    Debug.LogWarning("[GoogleMobileAdsProvider] Consent information update failed: " + error.Message);
                    InitializeMobileAds();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    consentFlowFinished = true;

                    if (formError != null)
                    {
                        Debug.LogWarning("[GoogleMobileAdsProvider] Consent form failed: " + formError.Message);
                        InitializeMobileAds();
                        return;
                    }

                    if (ConsentInformation.CanRequestAds())
                    {
                        InitializeMobileAds();
                        return;
                    }

                    Debug.LogWarning("[GoogleMobileAdsProvider] Ads cannot be requested yet because consent is not available.");
                });

                if (ConsentInformation.CanRequestAds())
                    InitializeMobileAds();
            });
        }

        private void InitializeMobileAds()
        {
            if (mobileAdsInitializeStarted)
                return;

            consentFlowFinished = true;
            mobileAdsInitializeStarted = true;
            mobileAdsInitializeStartedAt = Time.unscaledTime;
            MobileAds.Initialize(_ =>
            {
                IsInitialized = true;
                PreloadKnownAdsOnce();
            });
        }

        private void EnsureConsentFallback()
        {
            if (!initializeStarted || mobileAdsInitializeStarted)
                return;

            if (Time.unscaledTime - consentFlowStartedAt < ConsentFallbackDelaySeconds)
                return;

            Debug.LogWarning("[GoogleMobileAdsProvider] Consent flow timed out. Initializing Google Mobile Ads fallback.");
            InitializeMobileAds();
        }

        private void EnsureMobileAdsInitializeFallback()
        {
            if (!mobileAdsInitializeStarted || IsInitialized)
                return;

            if (Time.unscaledTime - mobileAdsInitializeStartedAt < MobileAdsInitializeFallbackDelaySeconds)
                return;

            Debug.LogWarning("[GoogleMobileAdsProvider] MobileAds.Initialize timed out. Starting ad preload fallback.");
            IsInitialized = true;
            PreloadKnownAdsOnce();
        }

        public bool IsRewardedAdReady(string placementId)
        {
            string key = NormalizePlacementId(placementId);
            EnsureConsentFallback();
            EnsureMobileAdsInitializeFallback();
            DiscardExpiredRewarded(key);
            EnsureRewardedLoaded(key);
            return rewardedAds.TryGetValue(key, out RewardedAd ad) && ad != null && ad.CanShowAd();
        }

        public RewardedAdAvailability GetRewardedAdAvailability(string placementId)
        {
            string key = NormalizePlacementId(placementId);
            EnsureConsentFallback();
            EnsureMobileAdsInitializeFallback();
            DiscardExpiredRewarded(key);
            EnsureRewardedLoaded(key);

            if (!IsInitialized)
                return new RewardedAdAvailability(
                    consentFlowFinished ? RewardedAdAvailabilityState.Unavailable : RewardedAdAvailabilityState.NotInitialized,
                    key,
                    consentFlowFinished ? "shop.ad_consent_required" : "shop.ad_initializing");

            if (rewardedAds.TryGetValue(key, out RewardedAd ad) && ad != null && ad.CanShowAd())
                return new RewardedAdAvailability(RewardedAdAvailabilityState.Ready, key, "shop.ad_ready");

            if (IsLoading(rewardedLoading, key))
                return new RewardedAdAvailability(RewardedAdAvailabilityState.Loading, key, "shop.ad_loading");

            if (rewardedLastErrors.TryGetValue(key, out string error) && !string.IsNullOrWhiteSpace(error))
                return new RewardedAdAvailability(RewardedAdAvailabilityState.Unavailable, key, BuildUnavailableMessage(error));

            return new RewardedAdAvailability(RewardedAdAvailabilityState.Loading, key, "shop.ad_loading");
        }

        public void ShowRewardedAd(string placementId, Action<RewardedAdResult> onComplete)
        {
            string key = NormalizePlacementId(placementId);
            DiscardExpiredRewarded(key);
            if (!rewardedAds.TryGetValue(key, out RewardedAd ad) || ad == null || !ad.CanShowAd())
            {
                EnsureRewardedLoaded(key);
                RewardedAdAvailability availability = GetRewardedAdAvailability(key);
                AdTelemetryService.Report("ad_show_not_ready", key, "rewarded", availability.State.ToString(), availability.Message);
                if (AdWebViewRepairPrompt.ShouldShowForMessage(availability.Message))
                    AdWebViewRepairPrompt.Show();

                onComplete?.Invoke(new RewardedAdResult(RewardedAdState.NotReady, key, availability.Message));
                return;
            }

            string adUnitId = GetRewardedAdUnitId(key);
            AdTelemetryService.Report("ad_show_requested", key, "rewarded", "requested", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            bool rewardEarned = false;
            bool fullScreenOpened = false;
            bool completed = false;
            Action<RewardedAdResult> completeOnce = result =>
            {
                if (completed)
                    return;

                completed = true;
                onComplete?.Invoke(result);
            };
            Action finish = () =>
            {
                RewardedAdResult result = rewardEarned
                    ? new RewardedAdResult(RewardedAdState.Completed, key, string.Empty)
                    : new RewardedAdResult(RewardedAdState.Skipped, key, "shop.ad_not_ready");

                CleanupRewarded(key);
                EnsureRewardedLoaded(key);
                AdTelemetryService.Report(
                    rewardEarned ? "ad_reward_earned" : "ad_closed_without_reward",
                    key,
                    "rewarded",
                    result.State.ToString(),
                    result.Message);
                completeOnce(result);
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                fullScreenOpened = true;
                Debug.Log($"[GoogleMobileAdsProvider] Rewarded ad opened for {key}.");
                AdTelemetryService.Report("ad_opened", key, "rewarded", "opened", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            };
            ad.OnAdFullScreenContentClosed += finish;
            ad.OnAdFullScreenContentFailed += error =>
            {
                CleanupRewarded(key);
                EnsureRewardedLoaded(key);
                string message = error != null ? error.GetMessage() : "Rewarded ad failed.";
                AdTelemetryService.Report("ad_show_failed", key, "rewarded", "failed", message, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                completeOnce(new RewardedAdResult(RewardedAdState.Failed, key, message));
            };

            StartShowOpenWatchdog(
                key,
                () => completed,
                () => fullScreenOpened,
                () =>
                {
                    CleanupRewarded(key);
                    EnsureRewardedLoaded(key);
                    AdTelemetryService.Report("ad_show_timeout", key, "rewarded", "timeout", "Fullscreen did not open after Show().", BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                    completeOnce(new RewardedAdResult(RewardedAdState.NotReady, key, "shop.ad_not_ready"));
                });

            try
            {
                ad.Show(_ => { rewardEarned = true; });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleMobileAdsProvider] Rewarded ad show threw for {key}: {ex.Message}");
                CleanupRewarded(key);
                EnsureRewardedLoaded(key);
                AdTelemetryService.Report("ad_show_exception", key, "rewarded", "exception", ex.Message, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                completeOnce(new RewardedAdResult(RewardedAdState.Failed, key, ex.Message));
            }
        }

        public bool IsInterstitialReady(string placementId)
        {
            string key = NormalizePlacementId(placementId);
            EnsureConsentFallback();
            EnsureMobileAdsInitializeFallback();
            DiscardExpiredInterstitial(key);
            EnsureInterstitialLoaded(key);
            return interstitialAds.TryGetValue(key, out InterstitialAd ad) && ad != null && ad.CanShowAd();
        }

        public void ShowInterstitial(string placementId, Action<InterstitialAdResult> onComplete)
        {
            string key = NormalizePlacementId(placementId);
            EnsureConsentFallback();
            EnsureMobileAdsInitializeFallback();
            DiscardExpiredInterstitial(key);
            if (!interstitialAds.TryGetValue(key, out InterstitialAd ad) || ad == null || !ad.CanShowAd())
            {
                EnsureInterstitialLoaded(key);
                AdTelemetryService.Report("ad_show_not_ready", key, "interstitial", "not_ready", "shop.ad_not_ready");
                onComplete?.Invoke(new InterstitialAdResult(InterstitialAdState.NotReady, key, "shop.ad_not_ready"));
                return;
            }

            string adUnitId = GetInterstitialAdUnitId(key);
            AdTelemetryService.Report("ad_show_requested", key, "interstitial", "requested", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            bool fullScreenOpened = false;
            bool completed = false;
            Action<InterstitialAdResult> completeOnce = result =>
            {
                if (completed)
                    return;

                completed = true;
                onComplete?.Invoke(result);
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                fullScreenOpened = true;
                Debug.Log($"[GoogleMobileAdsProvider] Interstitial ad opened for {key}.");
                AdTelemetryService.Report("ad_opened", key, "interstitial", "opened", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                CleanupInterstitial(key);
                EnsureInterstitialLoaded(key);
                AdTelemetryService.Report("ad_closed", key, "interstitial", "closed", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                completeOnce(new InterstitialAdResult(InterstitialAdState.Closed, key, string.Empty));
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                CleanupInterstitial(key);
                EnsureInterstitialLoaded(key);
                string message = error != null ? error.GetMessage() : "Interstitial ad failed.";
                AdTelemetryService.Report("ad_show_failed", key, "interstitial", "failed", message, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                completeOnce(new InterstitialAdResult(InterstitialAdState.Failed, key, message));
            };

            StartShowOpenWatchdog(
                key,
                () => completed,
                () => fullScreenOpened,
                () =>
                {
                    CleanupInterstitial(key);
                    EnsureInterstitialLoaded(key);
                    AdTelemetryService.Report("ad_show_timeout", key, "interstitial", "timeout", "Fullscreen did not open after Show().", BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                    completeOnce(new InterstitialAdResult(InterstitialAdState.NotReady, key, "shop.ad_not_ready"));
                });

            try
            {
                ad.Show();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleMobileAdsProvider] Interstitial ad show threw for {key}: {ex.Message}");
                CleanupInterstitial(key);
                EnsureInterstitialLoaded(key);
                AdTelemetryService.Report("ad_show_exception", key, "interstitial", "exception", ex.Message, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
                completeOnce(new InterstitialAdResult(InterstitialAdState.Failed, key, ex.Message));
            }
        }

        private void PreloadKnownAds()
        {
            PreloadKnownAdsOnce();
        }

        private void PreloadKnownAdsOnce()
        {
            if (preloadStarted)
                return;

            preloadStarted = true;
            EnsureRewardedLoaded(MonetizationService.AmetistRewardedPlacementId);
            EnsureRewardedLoaded(MonetizationService.MainBonusRewardedPlacementId);
            EnsureRewardedLoaded(MonetizationService.EnergyRewardedPlacementId);
            EnsureRewardedLoaded(MonetizationService.SymbiGridRerollRewardedPlacementId);
            EnsureRewardedLoaded(MonetizationService.SymbiGridSecondChanceRewardedPlacementId);
            EnsureRewardedLoaded(MonetizationService.SymbiMineSecondChanceRewardedPlacementId);
            EnsureInterstitialLoaded(MonetizationService.SymbiGridInterstitialPlacementId);
            EnsureInterstitialLoaded(MonetizationService.MatchEndInterstitialPlacementId);
        }

        private void EnsureRewardedLoaded(string placementId)
        {
            string key = NormalizePlacementId(placementId);
            DiscardExpiredRewarded(key);
            if (!IsInitialized || rewardedAds.ContainsKey(key) || IsLoading(rewardedLoading, key))
                return;

            if (rewardedRetryAfterTimes.TryGetValue(key, out float retryAfter) && Time.unscaledTime < retryAfter)
                return;

            rewardedLoading[key] = true;
            string adUnitId = GetRewardedAdUnitId(key);
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                const string missingIdMessage = "Rewarded ad unit ID is not configured.";
                rewardedLoading[key] = false;
                rewardedLastErrors[key] = missingIdMessage;
                rewardedRetryAfterTimes[key] = Time.unscaledTime + RewardedLoadRetryDelaySeconds;
                Debug.LogWarning($"[GoogleMobileAdsProvider] {missingIdMessage} Placement: {key}");
                AdTelemetryService.Report("ad_load_failed", key, "rewarded", "failed", missingIdMessage);
                return;
            }

            AdTelemetryService.Report("ad_load_started", key, "rewarded", "loading", string.Empty, new AdTelemetryNetworkInfo { adUnitId = adUnitId });
            StartLoadCallbackWatchdog(
                key,
                "rewarded",
                rewardedLoading,
                rewardedRetryAfterTimes,
                rewardedLastErrors,
                RewardedLoadRetryDelaySeconds,
                adUnitId);
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                rewardedLoading[key] = false;
                if (error != null || ad == null)
                {
                    string message = error != null ? error.GetMessage() : "No ad returned";
                    rewardedLastErrors[key] = message;
                    rewardedRetryAfterTimes[key] = Time.unscaledTime + RewardedLoadRetryDelaySeconds;
                    Debug.LogWarning($"Rewarded ad failed to load for {key}: {message}");
                    AdTelemetryService.Report("ad_load_failed", key, "rewarded", "failed", message, new AdTelemetryNetworkInfo { adUnitId = adUnitId });
                    return;
                }

                rewardedLastErrors.Remove(key);
                rewardedRetryAfterTimes.Remove(key);
                rewardedAds[key] = ad;
                rewardedLoadedAtTimes[key] = Time.unscaledTime;
                AdTelemetryService.Report("ad_loaded", key, "rewarded", "ready", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            });
        }

        private void EnsureInterstitialLoaded(string placementId)
        {
            string key = NormalizePlacementId(placementId);
            DiscardExpiredInterstitial(key);
            if (!IsInitialized || interstitialAds.ContainsKey(key) || IsLoading(interstitialLoading, key))
                return;

            if (interstitialRetryAfterTimes.TryGetValue(key, out float retryAfter) && Time.unscaledTime < retryAfter)
                return;

            interstitialLoading[key] = true;
            string adUnitId = GetInterstitialAdUnitId(key);
            AdTelemetryService.Report("ad_load_started", key, "interstitial", "loading", string.Empty, new AdTelemetryNetworkInfo { adUnitId = adUnitId });
            StartLoadCallbackWatchdog(
                key,
                "interstitial",
                interstitialLoading,
                interstitialRetryAfterTimes,
                null,
                InterstitialLoadRetryDelaySeconds,
                adUnitId);
            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                interstitialLoading[key] = false;
                if (error != null || ad == null)
                {
                    string message = error != null ? error.GetMessage() : "No ad returned";
                    interstitialRetryAfterTimes[key] = Time.unscaledTime + InterstitialLoadRetryDelaySeconds;
                    Debug.LogWarning($"Interstitial ad failed to load for {key}: {message}");
                    AdTelemetryService.Report("ad_load_failed", key, "interstitial", "failed", message, new AdTelemetryNetworkInfo { adUnitId = adUnitId });
                    return;
                }

                interstitialRetryAfterTimes.Remove(key);
                interstitialAds[key] = ad;
                interstitialLoadedAtTimes[key] = Time.unscaledTime;
                AdTelemetryService.Report("ad_loaded", key, "interstitial", "ready", string.Empty, BuildNetworkInfo(ad.GetResponseInfo(), adUnitId));
            });
        }

        private static AdTelemetryNetworkInfo BuildNetworkInfo(ResponseInfo responseInfo, string adUnitId)
        {
            AdTelemetryNetworkInfo info = new AdTelemetryNetworkInfo
            {
                adUnitId = adUnitId ?? string.Empty
            };

            if (responseInfo == null)
                return info;

            info.responseId = responseInfo.GetResponseId() ?? string.Empty;
            info.mediationAdapterClassName = responseInfo.GetMediationAdapterClassName() ?? string.Empty;

            AdapterResponseInfo loaded = responseInfo.GetLoadedAdapterResponseInfo();
            if (loaded != null)
            {
                info.loadedAdapterClassName = loaded.AdapterClassName ?? string.Empty;
                info.adSourceName = loaded.AdSourceName ?? string.Empty;
                info.adSourceId = loaded.AdSourceId ?? string.Empty;
                info.adSourceInstanceName = loaded.AdSourceInstanceName ?? string.Empty;
                info.adSourceInstanceId = loaded.AdSourceInstanceId ?? string.Empty;
            }

            info.adapterResponses = BuildAdapterResponsesSummary(responseInfo);
            return info;
        }

        private static string BuildAdapterResponsesSummary(ResponseInfo responseInfo)
        {
            List<AdapterResponseInfo> responses = responseInfo.GetAdapterResponses();
            if (responses == null || responses.Count == 0)
                return string.Empty;

            List<string> parts = new List<string>();
            int count = Mathf.Min(responses.Count, 8);
            for (int i = 0; i < count; i++)
            {
                AdapterResponseInfo adapter = responses[i];
                if (adapter == null)
                    continue;

                string status = adapter.AdError == null ? "loaded" : adapter.AdError.GetCode().ToString();
                parts.Add($"{adapter.AdSourceName}|{adapter.AdapterClassName}|{status}|{adapter.LatencyMillis}ms");
            }

            return string.Join(";", parts);
        }

        private static bool IsLoading(Dictionary<string, bool> source, string key)
        {
            return source.TryGetValue(key, out bool loading) && loading;
        }

        private static void StartShowOpenWatchdog(string key, Func<bool> isCompleted, Func<bool> hasOpened, Action onTimeout)
        {
            MonetizationService service = MonetizationService.I;
            if (service == null || !service.isActiveAndEnabled)
                return;

            service.StartCoroutine(ShowOpenWatchdog(key, isCompleted, hasOpened, onTimeout));
        }

        private static void StartLoadCallbackWatchdog(
            string key,
            string adFormat,
            Dictionary<string, bool> loading,
            Dictionary<string, float> retryAfterTimes,
            Dictionary<string, string> lastErrors,
            float retryDelaySeconds,
            string adUnitId)
        {
            MonetizationService service = MonetizationService.I;
            if (service == null || !service.isActiveAndEnabled)
                return;

            service.StartCoroutine(LoadCallbackWatchdog(key, adFormat, loading, retryAfterTimes, lastErrors, retryDelaySeconds, adUnitId));
        }

        private static IEnumerator LoadCallbackWatchdog(
            string key,
            string adFormat,
            Dictionary<string, bool> loading,
            Dictionary<string, float> retryAfterTimes,
            Dictionary<string, string> lastErrors,
            float retryDelaySeconds,
            string adUnitId)
        {
            float deadline = Time.unscaledTime + LoadCallbackTimeoutSeconds;
            while (Time.unscaledTime < deadline)
            {
                if (!IsLoading(loading, key))
                    yield break;

                yield return null;
            }

            if (!IsLoading(loading, key))
                yield break;

            loading[key] = false;
            retryAfterTimes[key] = Time.unscaledTime + retryDelaySeconds;
            if (lastErrors != null)
                lastErrors[key] = "Ad load callback timeout.";
            Debug.LogWarning($"[GoogleMobileAdsProvider] {adFormat} ad load callback timed out for {key}.");
            AdTelemetryService.Report(
                "ad_load_timeout",
                key,
                adFormat,
                "timeout",
                "Ad load callback did not complete.",
                new AdTelemetryNetworkInfo { adUnitId = adUnitId });
        }

        private static IEnumerator ShowOpenWatchdog(string key, Func<bool> isCompleted, Func<bool> hasOpened, Action onTimeout)
        {
            float deadline = Time.unscaledTime + FullScreenOpenTimeoutSeconds;
            while (Time.unscaledTime < deadline)
            {
                if ((isCompleted != null && isCompleted()) || (hasOpened != null && hasOpened()))
                    yield break;

                yield return null;
            }

            if ((isCompleted != null && isCompleted()) || (hasOpened != null && hasOpened()))
                yield break;

            Debug.LogWarning($"[GoogleMobileAdsProvider] Ad show did not open fullscreen for {key} within {FullScreenOpenTimeoutSeconds:0.#} seconds.");
            onTimeout?.Invoke();
        }

        private void CleanupRewarded(string key)
        {
            if (rewardedAds.TryGetValue(key, out RewardedAd ad) && ad != null)
                ad.Destroy();

            rewardedAds.Remove(key);
            rewardedLoadedAtTimes.Remove(key);
        }

        private void CleanupInterstitial(string key)
        {
            if (interstitialAds.TryGetValue(key, out InterstitialAd ad) && ad != null)
                ad.Destroy();

            interstitialAds.Remove(key);
            interstitialLoadedAtTimes.Remove(key);
        }

        private void DiscardExpiredRewarded(string key)
        {
            if (!rewardedAds.ContainsKey(key) || !IsExpired(rewardedLoadedAtTimes, key, MonetizationAdSettings.RewardedAdMaxAgeSeconds))
                return;

            Debug.Log($"[GoogleMobileAdsProvider] Discarding expired rewarded ad for {key}.");
            CleanupRewarded(key);
        }

        private void DiscardExpiredInterstitial(string key)
        {
            if (!interstitialAds.ContainsKey(key) || !IsExpired(interstitialLoadedAtTimes, key, MonetizationAdSettings.InterstitialAdMaxAgeSeconds))
                return;

            Debug.Log($"[GoogleMobileAdsProvider] Discarding expired interstitial ad for {key}.");
            CleanupInterstitial(key);
        }

        private static bool IsExpired(Dictionary<string, float> loadedAtTimes, string key, int maxAgeSeconds)
        {
            if (!loadedAtTimes.TryGetValue(key, out float loadedAt))
                return true;

            return Time.unscaledTime - loadedAt >= maxAgeSeconds;
        }

        private static string NormalizePlacementId(string placementId)
        {
            return string.IsNullOrWhiteSpace(placementId) ? string.Empty : placementId;
        }

        private static string GetRewardedAdUnitId(string placementId)
        {
            switch (placementId)
            {
                case MonetizationService.MainBonusRewardedPlacementId:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    return string.IsNullOrWhiteSpace(MainBonusRewardedAdUnitId)
                        ? DefaultRewardedAdUnitId
                        : MainBonusRewardedAdUnitId;
#else
                    return MainBonusRewardedAdUnitId;
#endif
                case MonetizationService.EnergyRewardedPlacementId:
                    return BattleEnergyRewardedAdUnitId;
                case MonetizationService.WeeklyRewardedPlacementId:
                    return WeeklyRewardedAdUnitId;
                case MonetizationService.SymbiGridRerollRewardedPlacementId:
                    return SymbiGridRerollRewardedAdUnitId;
                case MonetizationService.SymbiMineSecondChanceRewardedPlacementId:
                    return SymbiMineSecondChanceRewardedAdUnitId;
                default:
                    return DefaultRewardedAdUnitId;
            }
        }

        private static string BuildUnavailableMessage(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return "shop.ad_not_ready";

            string normalized = error.ToLowerInvariant();
            if (normalized.Contains("javascriptengine") || normalized.Contains("javascript engine") || normalized.Contains("webview"))
                return AdWebViewRepairPrompt.MessageKey;

            if (normalized.Contains("no fill") || normalized.Contains("no ad") || normalized.Contains("no inventory"))
                return "shop.ad_no_fill";

            if (normalized.Contains("network") || normalized.Contains("timeout") || normalized.Contains("offline"))
                return "shop.ad_network_error";

            return "shop.ad_not_ready";
        }

        private static string GetInterstitialAdUnitId(string placementId)
        {
            return string.Equals(placementId, MonetizationService.SymbiGridInterstitialPlacementId, StringComparison.Ordinal)
                ? SymbiGridInterstitialAdUnitId
                : DefaultInterstitialAdUnitId;
        }
    }
}
