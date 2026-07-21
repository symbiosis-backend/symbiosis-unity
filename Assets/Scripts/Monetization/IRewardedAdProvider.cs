using System;

namespace MahjongGame.Monetization
{
    public interface IRewardedAdProvider
    {
        bool IsInitialized { get; }
        void Initialize();
        bool IsRewardedAdReady(string placementId);
        RewardedAdAvailability GetRewardedAdAvailability(string placementId);
        void ShowRewardedAd(string placementId, Action<RewardedAdResult> onComplete);
    }
}
