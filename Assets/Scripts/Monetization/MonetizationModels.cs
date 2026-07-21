using System;

namespace MahjongGame.Monetization
{
    public enum RewardedAdState
    {
        Completed,
        Skipped,
        Failed,
        NotReady
    }

    public enum PurchaseState
    {
        Purchased,
        Cancelled,
        Failed,
        NotReady
    }

    public enum InterstitialAdState
    {
        Closed,
        Failed,
        NotReady
    }

    public enum RewardedAdAvailabilityState
    {
        NotInitialized,
        Loading,
        Ready,
        Unavailable
    }

    public readonly struct RewardedAdResult
    {
        public RewardedAdResult(RewardedAdState state, string placementId, string message)
        {
            State = state;
            PlacementId = placementId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public RewardedAdState State { get; }
        public string PlacementId { get; }
        public string Message { get; }
        public bool IsCompleted => State == RewardedAdState.Completed;
    }

    public readonly struct PurchaseResult
    {
        public PurchaseResult(PurchaseState state, string productId, string message)
        {
            State = state;
            ProductId = productId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public PurchaseState State { get; }
        public string ProductId { get; }
        public string Message { get; }
        public bool IsPurchased => State == PurchaseState.Purchased;
    }

    public readonly struct InterstitialAdResult
    {
        public InterstitialAdResult(InterstitialAdState state, string placementId, string message)
        {
            State = state;
            PlacementId = placementId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public InterstitialAdState State { get; }
        public string PlacementId { get; }
        public string Message { get; }
        public bool WasShown => State == InterstitialAdState.Closed;
    }

    public readonly struct RewardedAdAvailability
    {
        public RewardedAdAvailability(RewardedAdAvailabilityState state, string placementId, string message)
        {
            State = state;
            PlacementId = placementId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public RewardedAdAvailabilityState State { get; }
        public string PlacementId { get; }
        public string Message { get; }
        public bool IsReady => State == RewardedAdAvailabilityState.Ready;
        public bool IsLoading => State == RewardedAdAvailabilityState.Loading || State == RewardedAdAvailabilityState.NotInitialized;
    }

    [Serializable]
    public sealed class MonetizationProduct
    {
        public string ProductId;
        public string StoreProductId;
        public int OzAmetistAmount;
        public string LocalPrice;

        public MonetizationProduct(string productId, string storeProductId, int ozAmetistAmount, string localPrice)
        {
            ProductId = productId ?? string.Empty;
            StoreProductId = storeProductId ?? string.Empty;
            OzAmetistAmount = ozAmetistAmount;
            LocalPrice = localPrice ?? string.Empty;
        }
    }
}
