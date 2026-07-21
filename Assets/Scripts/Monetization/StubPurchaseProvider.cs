using System;
using System.Collections.Generic;

namespace MahjongGame.Monetization
{
    public sealed class StubPurchaseProvider : IPurchaseProvider
    {
        private readonly bool completeImmediately;
        private HashSet<string> productIds = new HashSet<string>(StringComparer.Ordinal);

        public StubPurchaseProvider(bool completeImmediately)
        {
            this.completeImmediately = completeImmediately;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(IReadOnlyList<MonetizationProduct> products)
        {
            productIds.Clear();

            if (products != null)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    if (products[i] != null && !string.IsNullOrWhiteSpace(products[i].ProductId))
                        productIds.Add(products[i].ProductId);
                }
            }

            IsInitialized = true;
        }

        public bool CanPurchase(string productId)
        {
            return IsInitialized && completeImmediately && productIds.Contains(productId);
        }

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            if (!productIds.Contains(productId))
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseState.Failed, productId, "Unknown product id."));
                return;
            }

            if (!CanPurchase(productId))
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseState.NotReady, productId, "Purchase provider is not connected."));
                return;
            }

            onComplete?.Invoke(new PurchaseResult(PurchaseState.Purchased, productId, "Simulated purchase completed."));
        }
    }
}
