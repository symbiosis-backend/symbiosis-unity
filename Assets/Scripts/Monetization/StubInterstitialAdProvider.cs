using System;

namespace MahjongGame.Monetization
{
    public sealed class StubInterstitialAdProvider : IInterstitialAdProvider
    {
        private readonly bool completeImmediately;

        public StubInterstitialAdProvider(bool completeImmediately)
        {
            this.completeImmediately = completeImmediately;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public bool IsInterstitialReady(string placementId)
        {
            return IsInitialized && completeImmediately;
        }

        public void ShowInterstitial(string placementId, Action<InterstitialAdResult> onComplete)
        {
            if (!IsInterstitialReady(placementId))
            {
                onComplete?.Invoke(new InterstitialAdResult(InterstitialAdState.NotReady, placementId, "Interstitial provider is not connected."));
                return;
            }

            onComplete?.Invoke(new InterstitialAdResult(InterstitialAdState.Closed, placementId, "Simulated interstitial closed."));
        }
    }
}
