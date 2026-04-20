using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class ClusterSceneController : MonoBehaviour
    {
        [SerializeField] private string clusterId = ClusterService.ElysiumId;
        [SerializeField] private string primaryConnectionId = ClusterService.SlumsId;

        private void Awake()
        {
            EnsureWorldRuntime();

            EnsureSceneUi();
        }

        public void TravelToPrimaryConnection()
        {
            ClusterService.LoadCluster(primaryConnectionId);
        }

        public void ReturnToMain()
        {
            ClusterService.ReturnToMain();
        }

        private void EnsureSceneUi()
        {
            if (!ClusterService.TryGet(clusterId, out ClusterDefinition cluster))
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                canvas = CreateCanvas();

            if (canvas.transform.Find("ClusterScenePanel") != null)
                return;

            GameObject panel = new GameObject("ClusterScenePanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            CreateText(panel.transform, cluster.displayName, 42f, FontStyles.Bold, new Vector2(0f, 466f), new Vector2(820f, 62f));
            CreateText(panel.transform, cluster.description, 20f, FontStyles.Normal, new Vector2(0f, 422f), new Vector2(920f, 44f));

            if (ClusterService.TryGet(primaryConnectionId, out ClusterDefinition connection))
            {
                Button travelButton = CreateButton(panel.transform, $"GO TO {connection.displayName.ToUpperInvariant()}", new Vector2(-190f, -466f));
                travelButton.onClick.AddListener(TravelToPrimaryConnection);
            }

            Button backButton = CreateButton(panel.transform, "MAIN", new Vector2(190f, -466f));
            backButton.onClick.AddListener(ReturnToMain);
        }

        private void EnsureWorldRuntime()
        {
            if (clusterId != ClusterService.ElysiumId && clusterId != ClusterService.SlumsId)
                return;

            ClusterWorldRuntime runtime = GetComponent<ClusterWorldRuntime>();
            if (runtime == null)
                runtime = gameObject.AddComponent<ClusterWorldRuntime>();

            runtime.Configure(clusterId, primaryConnectionId);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, FontStyles style, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Vector2 anchoredPosition)
        {
            GameObject root = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 58f);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.15f, 0.94f);

            Button button = root.GetComponent<Button>();
            TextMeshProUGUI label = CreateText(root.transform, text, 22f, FontStyles.Bold, Vector2.zero, rect.sizeDelta);
            label.color = new Color(0.78f, 1f, 0.91f, 1f);
            return button;
        }
    }

}
