using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class ClusterMainMenuButton : MonoBehaviour
    {
        private Button button;

        public static ClusterMainMenuButton CreateInScene()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                canvas = CreateCanvas();

            GameObject root = new GameObject("MatrixClusterButton", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas.transform, false);
            root.layer = canvas.gameObject.layer;

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 112f);
            rect.sizeDelta = new Vector2(260f, 58f);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.15f, 0.94f);

            Button createdButton = root.GetComponent<Button>();
            ColorBlock colors = createdButton.colors;
            colors.normalColor = new Color(0.08f, 0.12f, 0.15f, 0.94f);
            colors.highlightedColor = new Color(0.12f, 0.22f, 0.27f, 1f);
            colors.pressedColor = new Color(0.04f, 0.28f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            createdButton.colors = colors;

            TextMeshProUGUI label = CreateLabel(root.transform);
            label.text = "MATRIX";

            ClusterMainMenuButton component = root.AddComponent<ClusterMainMenuButton>();
            component.button = createdButton;
            component.Bind();
            return component;
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            Bind();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(EnterMatrix);
        }

        private void Bind()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(EnterMatrix);
            button.onClick.AddListener(EnterMatrix);
        }

        private static void EnterMatrix()
        {
            ClusterService.EnterMatrix();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent)
        {
            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.78f, 1f, 0.91f, 1f);
            label.raycastTarget = false;
            return label;
        }
    }
}
