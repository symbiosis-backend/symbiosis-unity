using System;
using System.Collections;
using UnityEngine;

namespace MahjongGame.Monetization
{
    public static class MatchEndAdService
    {
        private const string MatchEndCountKey = "monetization_match_end_interstitial_count";
        private const string LastShownTicksKey = "monetization_match_end_interstitial_last_ticks";
        private const float InterstitialShowTimeoutSeconds = 45f;

        public static bool TryShowAfterMatchResult(string source, Action<InterstitialAdResult> onComplete = null)
        {
            if (NoAdsService.HasActiveNoAds())
                return false;

            MonetizationService service = MonetizationService.Ensure();

            int count = Mathf.Max(0, PlayerPrefs.GetInt(MatchEndCountKey, 0)) + 1;
            PlayerPrefs.SetInt(MatchEndCountKey, count);
            PlayerPrefs.Save();

            int showEveryCount = MonetizationAdSettings.MatchEndShowEveryCount;
            if (showEveryCount > 1 && count % showEveryCount != 0)
                return false;

            if (!HasCooldownElapsed())
                return false;

            string placementId = MonetizationService.MatchEndInterstitialPlacementId;

            // Navigation must never wait for an ad to warm up. On Android the old
            // coroutine disabled the result button for five seconds even when no ad
            // was available, which looked like a broken "Return to Lobby" button.
            if (!service.CanShowInterstitialAd(placementId))
                return false;

            PlayerPrefs.SetString(LastShownTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();

            service.StartCoroutine(ShowReadyInterstitial(service, placementId, source, onComplete));
            return true;
        }

        private static IEnumerator ShowReadyInterstitial(MonetizationService service, string placementId, string source, Action<InterstitialAdResult> onComplete)
        {
            bool completed = false;

            void CompleteOnce(InterstitialAdResult result)
            {
                if (completed)
                    return;

                completed = true;
                Debug.Log($"[MatchEndAdService] Match-end interstitial completed | Source={source} | State={result.State}");
                onComplete?.Invoke(result);
            }

            try
            {
                service.ShowInterstitialAd(placementId, CompleteOnce);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MatchEndAdService] Interstitial show failed | Source={source} | {exception.Message}");
                CompleteOnce(new InterstitialAdResult(InterstitialAdState.Failed, placementId, exception.Message));
            }

            float deadline = Time.unscaledTime + InterstitialShowTimeoutSeconds;
            while (!completed && Time.unscaledTime < deadline)
                yield return null;

            if (!completed)
            {
                const string timeoutMessage = "Interstitial completion callback timed out.";
                Debug.LogWarning($"[MatchEndAdService] {timeoutMessage} Source={source}");
                CompleteOnce(new InterstitialAdResult(InterstitialAdState.Failed, placementId, timeoutMessage));
            }
        }

        private static bool HasCooldownElapsed()
        {
            string storedTicks = PlayerPrefs.GetString(LastShownTicksKey, string.Empty);
            if (!long.TryParse(storedTicks, out long lastTicks) || lastTicks <= 0)
                return true;

            TimeSpan elapsed = TimeSpan.FromTicks(Math.Max(0L, DateTime.UtcNow.Ticks - lastTicks));
            return elapsed.TotalSeconds >= MonetizationAdSettings.MatchEndCooldownSeconds;
        }
    }
}
