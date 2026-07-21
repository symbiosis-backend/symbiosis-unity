using System;
using System.Collections.Generic;

namespace MahjongGame.Monetization
{
    public interface IPurchaseProvider
    {
        bool IsInitialized { get; }
        void Initialize(IReadOnlyList<MonetizationProduct> products);
        bool CanPurchase(string productId);
        void Purchase(string productId, Action<PurchaseResult> onComplete);
    }
}
