using System;
using System.Collections;
using MahjongGame.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MahjongGame.Monetization
{
    public static class AdTelemetryService
    {
        private const string SessionTokenKey = "symbiosis_server_session_token";
        private const string TelemetryPath = "/telemetry/ads";

        public static void Report(string eventName, string placementId, string adFormat, string state, string message = "")
        {
            Report(eventName, placementId, adFormat, state, message, null);
        }

        public static void Report(string eventName, string placementId, string adFormat, string state, string message, AdTelemetryNetworkInfo networkInfo)
        {
            MonetizationService service = MonetizationService.I;
            if (service == null || !service.isActiveAndEnabled)
                return;

            service.StartCoroutine(Send(eventName, placementId, adFormat, state, message, networkInfo));
        }

        private static IEnumerator Send(string eventName, string placementId, string adFormat, string state, string message, AdTelemetryNetworkInfo networkInfo)
        {
            AdTelemetryPayload payload = new AdTelemetryPayload
            {
                token = PlayerPrefs.GetString(SessionTokenKey, string.Empty),
                eventName = eventName ?? string.Empty,
                placementId = placementId ?? string.Empty,
                adFormat = adFormat ?? string.Empty,
                state = state ?? string.Empty,
                message = message ?? string.Empty,
                sdkName = "GoogleMobileAds",
                appVersion = Application.version ?? string.Empty,
                versionCode = BackendEndpoints.GetClientVersionCode(),
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel ?? string.Empty,
                osVersion = SystemInfo.operatingSystem ?? string.Empty,
                language = Application.systemLanguage.ToString(),
                country = string.Empty,
                adUnitId = networkInfo != null ? networkInfo.adUnitId : string.Empty,
                responseId = networkInfo != null ? networkInfo.responseId : string.Empty,
                mediationAdapterClassName = networkInfo != null ? networkInfo.mediationAdapterClassName : string.Empty,
                adSourceName = networkInfo != null ? networkInfo.adSourceName : string.Empty,
                adSourceId = networkInfo != null ? networkInfo.adSourceId : string.Empty,
                adSourceInstanceName = networkInfo != null ? networkInfo.adSourceInstanceName : string.Empty,
                adSourceInstanceId = networkInfo != null ? networkInfo.adSourceInstanceId : string.Empty,
                loadedAdapterClassName = networkInfo != null ? networkInfo.loadedAdapterClassName : string.Empty,
                adapterResponses = networkInfo != null ? networkInfo.adapterResponses : string.Empty
            };

            string json = JsonUtility.ToJson(payload);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            string url = BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, TelemetryPath);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            BackendEndpoints.ApplyClientVersionHeaders(request);
            yield return request.SendWebRequest();

            if (BackendEndpoints.RequestFailed(request))
                Debug.LogWarning("[AdTelemetryService] Failed to send ad telemetry: " + request.error);
        }

        [Serializable]
        private sealed class AdTelemetryPayload
        {
            public string token;
            public string eventName;
            public string placementId;
            public string adFormat;
            public string state;
            public string message;
            public string sdkName;
            public string appVersion;
            public int versionCode;
            public string platform;
            public string deviceModel;
            public string osVersion;
            public string language;
            public string country;
            public string adUnitId;
            public string responseId;
            public string mediationAdapterClassName;
            public string adSourceName;
            public string adSourceId;
            public string adSourceInstanceName;
            public string adSourceInstanceId;
            public string loadedAdapterClassName;
            public string adapterResponses;
        }
    }

    [Serializable]
    public sealed class AdTelemetryNetworkInfo
    {
        public string adUnitId;
        public string responseId;
        public string mediationAdapterClassName;
        public string adSourceName;
        public string adSourceId;
        public string adSourceInstanceName;
        public string adSourceInstanceId;
        public string loadedAdapterClassName;
        public string adapterResponses;
    }

    public static class AdWebViewRepairPrompt
    {
        public const string MessageKey = "shop.ad_webview_update_required";

        private const string WebViewPackage = "com.google.android.webview";
        private const string ChromePackage = "com.android.chrome";
        private const string PlayServicesPackage = "com.google.android.gms";

        private static GameObject currentPrompt;

        public static bool ShouldShowForMessage(string message)
        {
            return string.Equals(message, MessageKey, StringComparison.Ordinal);
        }

        public static void Show()
        {
            if (currentPrompt != null)
            {
                currentPrompt.transform.SetAsLastSibling();
                return;
            }

            Canvas canvas = CreateCanvas();
            currentPrompt = canvas.gameObject;

            GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            dim.transform.SetParent(canvas.transform, false);
            RectTransform dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.64f);
            dim.GetComponent<Button>().onClick.AddListener(Close);

            GameObject panel = new GameObject("AdWebViewRepairPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(880f, 620f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.055f, 0.065f, 0.085f, 0.98f);
            BattlePopupStyle.ApplyWindow(panelImage);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(58, 58, 52, 48);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text title = CreateText(panel.transform, "Title", GameLocalization.Text("shop.ad_webview_title"), 42f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.88f, 0.48f, 1f);

            TMP_Text body = CreateText(panel.transform, "Body", GameLocalization.Text(MessageKey), 28f, TextAlignmentOptions.Center);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.color = new Color(0.92f, 0.96f, 1f, 1f);
            LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
            bodyLayout.preferredHeight = 190f;

            CreateButton(panel.transform, GameLocalization.Text("shop.ad_webview_webview"), () => OpenPackage(WebViewPackage));
            CreateButton(panel.transform, GameLocalization.Text("shop.ad_webview_chrome"), () => OpenPackage(ChromePackage));
            CreateButton(panel.transform, GameLocalization.Text("shop.ad_webview_play_services"), () => OpenPackage(PlayServicesPackage));
            CreateButton(panel.transform, GameLocalization.Text("shop.ad_webview_later"), Close);
        }

        private static Canvas CreateCanvas()
        {
            GameObject root = new GameObject("AdWebViewRepairPrompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TMP_Text label = obj.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(14f, size * 0.56f);
            label.fontSizeMax = size;
            label.alignment = alignment;
            label.raycastTarget = false;
            BattlePopupStyle.ApplyText(label, true);
            return label;
        }

        private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);

            LayoutElement layout = obj.GetComponent<LayoutElement>();
            layout.preferredHeight = 82f;
            layout.minHeight = 72f;

            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            BattlePopupStyle.ApplyButton(button);

            TMP_Text text = CreateText(obj.transform, "Label", label, 30f, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 8f);
            textRect.offsetMax = new Vector2(-24f, -8f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void OpenPackage(string packageName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("market://details?id=" + packageName);
#else
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
#endif
        }

        private static void Close()
        {
            if (currentPrompt == null)
                return;

            UnityEngine.Object.Destroy(currentPrompt);
            currentPrompt = null;
        }
    }
}
