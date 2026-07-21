using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace MahjongGame.Monetization
{
    public sealed class UnityIapPurchaseProvider : IPurchaseProvider
    {
        private StoreController storeController;
        private readonly Dictionary<string, MonetizationProduct> catalog = new Dictionary<string, MonetizationProduct>();
        private readonly Dictionary<string, Action<PurchaseResult>> pendingPurchases = new Dictionary<string, Action<PurchaseResult>>();
        private bool connectStarted;
        private bool productsFetched;

        public bool IsInitialized => storeController != null && productsFetched;

        public void Initialize(IReadOnlyList<MonetizationProduct> products)
        {
            catalog.Clear();
            if (products != null)
            {
                foreach (MonetizationProduct product in products)
                {
                    if (product == null || string.IsNullOrWhiteSpace(product.ProductId))
                        continue;

                    catalog[product.ProductId] = product;
                }
            }

            if (storeController == null)
                ConfigureStoreController();

            if (!connectStarted)
                Connect();
            else
                FetchProducts();
        }

        public bool CanPurchase(string productId)
        {
            Product product = GetStoreProduct(productId);
            return product != null && product.availableToPurchase;
        }

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            Product product = GetStoreProduct(productId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseState.NotReady, productId, "shop.purchase_not_ready"));
                return;
            }

            pendingPurchases[product.definition.id] = onComplete;
            storeController.PurchaseProduct(product);
        }

        private void ConfigureStoreController()
        {
            storeController = UnityIAPServices.StoreController();
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnStoreDisconnected += failure =>
            {
                productsFetched = false;
                Debug.LogWarning($"Unity IAP disconnected: {(failure != null ? failure.message : "Unknown store disconnect")}");
            };
        }

        private async void Connect()
        {
            connectStarted = true;
            try
            {
                await storeController.Connect();
                FetchProducts();
            }
            catch (Exception exception)
            {
                productsFetched = false;
                Debug.LogWarning($"Unity IAP connect failed: {exception.Message}");
            }
        }

        private void FetchProducts()
        {
            if (storeController == null || catalog.Count == 0)
                return;

            List<ProductDefinition> definitions = new List<ProductDefinition>();
            foreach (MonetizationProduct product in catalog.Values)
            {
                string storeProductId = string.IsNullOrWhiteSpace(product.StoreProductId) ? product.ProductId : product.StoreProductId;
                definitions.Add(new ProductDefinition(product.ProductId, storeProductId, ProductType.Consumable));
            }

            storeController.FetchProducts(definitions);
        }

        private void OnProductsFetched(List<Product> products)
        {
            productsFetched = products != null && products.Count > 0;
            if (products == null)
                return;

            foreach (Product product in products)
            {
                if (product == null || product.definition == null)
                    continue;

                if (catalog.TryGetValue(product.definition.id, out MonetizationProduct monetizationProduct) &&
                    product.metadata != null &&
                    !string.IsNullOrWhiteSpace(product.metadata.localizedPriceString))
                {
                    monetizationProduct.LocalPrice = product.metadata.localizedPriceString;
                }
            }
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            productsFetched = false;
            Debug.LogWarning($"Unity IAP products fetch failed: {(failure != null ? failure.FailureReason : "Unknown error")}");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string productId = GetProductId(order);
            if (string.IsNullOrWhiteSpace(productId))
                return;

            storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failedOrder)
            {
                CompletePurchase(GetProductId(failedOrder), new PurchaseResult(PurchaseState.Failed, GetProductId(failedOrder), failedOrder.Details));
                return;
            }

            string productId = GetProductId(order);
            CompletePurchase(productId, new PurchaseResult(PurchaseState.Purchased, productId, string.Empty));
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            string productId = GetProductId(failedOrder);
            PurchaseState state = failedOrder != null && failedOrder.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseState.Cancelled
                : PurchaseState.Failed;

            string message = failedOrder != null && !string.IsNullOrWhiteSpace(failedOrder.Details)
                ? failedOrder.Details
                : "shop.purchase_not_ready";

            CompletePurchase(productId, new PurchaseResult(state, productId, message));
        }

        private Product GetStoreProduct(string productId)
        {
            if (storeController == null || string.IsNullOrWhiteSpace(productId))
                return null;

            return storeController.GetProductById(productId);
        }

        private void CompletePurchase(string productId, PurchaseResult result)
        {
            if (!string.IsNullOrWhiteSpace(productId) && pendingPurchases.TryGetValue(productId, out Action<PurchaseResult> callback))
            {
                pendingPurchases.Remove(productId);
                callback?.Invoke(result);
            }
        }

        private static string GetProductId(Order order)
        {
            Product product = order?.CartOrdered?.Items()?.FirstOrDefault()?.Product;
            return product?.definition?.id ?? string.Empty;
        }
    }
}
