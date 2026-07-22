using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Monetization
{
    internal static class GooglePlayPurchaseValidationService
    {
        private const string VerifyPath = "/iap/google/verify";
        private const float AuthenticationWaitSeconds = 30f;

        [Serializable]
        private sealed class VerifyRequest
        {
            public string token;
            public string productId;
            public string purchaseToken;
        }

        [Serializable]
        internal sealed class VerifyResponse
        {
            public bool success;
            public bool duplicate;
            public string productId;
            public string grantType;
            public int grantAmount;
            public int amethystBalance;
            public string noAdsUntil;
            public string error;
        }

        public static IEnumerator Verify(
            string productId,
            string purchaseToken,
            Action<VerifyResponse> onSuccess,
            Action<string> onFailure)
        {
#if !UNITY_ANDROID
            onFailure?.Invoke("shop.purchase_not_ready");
            yield break;
#else
            float waitUntil = Time.realtimeSinceStartup + AuthenticationWaitSeconds;
            string sessionToken = GetSessionToken();
            while (string.IsNullOrWhiteSpace(sessionToken) && Time.realtimeSinceStartup < waitUntil)
            {
                yield return null;
                sessionToken = GetSessionToken();
            }

            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                onFailure?.Invoke("shop.purchase_auth_required");
                yield break;
            }

            VerifyRequest payload = new VerifyRequest
            {
                token = sessionToken,
                productId = productId ?? string.Empty,
                purchaseToken = purchaseToken ?? string.Empty
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            string url = BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, VerifyPath);

            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 30
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Token", sessionToken);
            BackendEndpoints.ApplyClientVersionHeaders(request);
            yield return request.SendWebRequest();

            VerifyResponse response = ParseResponse(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
            if (request.result != UnityWebRequest.Result.Success || response == null || !response.success)
            {
                string error = response != null && !string.IsNullOrWhiteSpace(response.error) && response.error.StartsWith("shop.", StringComparison.Ordinal)
                    ? response.error
                    : "shop.purchase_verification_failed";
                onFailure?.Invoke(error);
                yield break;
            }

            ApplyAuthoritativeEntitlements(response);
            onSuccess?.Invoke(response);
#endif
        }

        private static VerifyResponse ParseResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonUtility.FromJson<VerifyResponse>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Purchase verification response could not be parsed: {exception.Message}");
                return null;
            }
        }

        private static void ApplyAuthoritativeEntitlements(VerifyResponse response)
        {
            if (CurrencyService.I != null)
                CurrencyService.I.SetOzAmetist(Mathf.Max(0, response.amethystBalance));

            NoAdsService.ApplyServerNoAdsUntil(response.noAdsUntil);
        }

        private static string GetSessionToken()
        {
            if (ProfileService.I != null && !string.IsNullOrWhiteSpace(ProfileService.I.CurrentSessionToken))
                return ProfileService.I.CurrentSessionToken;

            return PlayerPrefs.GetString("symbiosis_server_session_token", string.Empty);
        }
    }
}
