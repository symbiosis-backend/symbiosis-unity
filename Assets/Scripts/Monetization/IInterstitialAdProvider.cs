using System;

namespace MahjongGame.Monetization
{
    public interface IInterstitialAdProvider
    {
        bool IsInitialized { get; }
        void Initialize();
        bool IsInterstitialReady(string placementId);
        void ShowInterstitial(string placementId, Action<InterstitialAdResult> onComplete);
    }
}
