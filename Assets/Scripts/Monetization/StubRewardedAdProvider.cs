using System;

namespace MahjongGame.Monetization
{
    public sealed class StubRewardedAdProvider : IRewardedAdProvider
    {
        private readonly bool completeImmediately;

        public StubRewardedAdProvider(bool completeImmediately)
        {
            this.completeImmediately = completeImmediately;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public bool IsRewardedAdReady(string placementId)
        {
            return IsInitialized && completeImmediately;
        }

        public RewardedAdAvailability GetRewardedAdAvailability(string placementId)
        {
            if (!IsInitialized)
                return new RewardedAdAvailability(RewardedAdAvailabilityState.NotInitialized, placementId, "shop.ad_initializing");

            return completeImmediately
                ? new RewardedAdAvailability(RewardedAdAvailabilityState.Ready, placementId, "shop.ad_ready")
                : new RewardedAdAvailability(RewardedAdAvailabilityState.Unavailable, placementId, "shop.ad_not_ready");
        }

        public void ShowRewardedAd(string placementId, Action<RewardedAdResult> onComplete)
        {
            if (!IsRewardedAdReady(placementId))
            {
                onComplete?.Invoke(new RewardedAdResult(RewardedAdState.NotReady, placementId, "Rewarded ads provider is not connected."));
                return;
            }

            onComplete?.Invoke(new RewardedAdResult(RewardedAdState.Completed, placementId, "Simulated rewarded ad completed."));
        }
    }
}
