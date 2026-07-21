using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame
{
    public sealed class MainInfoCard : MonoBehaviour, IPointerClickHandler
    {
        private const string ReadableFontResourcePath = "Fonts/Exo2";

        private static TMP_FontAsset cachedReadableFont;

        private string titleKey;
        private string bodyKey;
        private Sprite iconSprite;
        private CanvasGroup group;
        private Image iconImage;
        private TMP_Text titleText;
        private TMP_Text bodyText;

        public void Configure(string titleLocalizationKey, string bodyLocalizationKey, Sprite icon = null)
        {
            titleKey = titleLocalizationKey;
            bodyKey = bodyLocalizationKey;
            iconSprite = icon;
            group = GetComponent<CanvasGroup>();
            EnsureText();
            RefreshIcon();
            RefreshText();
        }

        public void SetVisible(bool visible)
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();

            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }

            gameObject.SetActive(visible);
        }

        public void RefreshText()
        {
            if (titleText != null)
                titleText.text = GameLocalization.Text(titleKey);

            if (bodyText != null)
                bodyText.text = GameLocalization.Text(bodyKey);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // The tutorial is advanced only by the understood button.
        }

        private void EnsureText()
        {
            if (iconImage == null && iconSprite != null)
                iconImage = CreateIcon();

            float textLeft = iconSprite != null ? 230f : 30f;
            if (titleText == null)
            {
                titleText = CreateText(
                    "Title",
                    new Vector2(textLeft, 108f),
                    new Vector2(-30f, -18f),
                    40f,
                    28f,
                    46f,
                    new Color(0.98f, 0.91f, 0.68f, 1f),
                    TextAlignmentOptions.TopLeft);
            }

            if (bodyText == null)
            {
                bodyText = CreateText(
                    "Body",
                    new Vector2(textLeft, 24f),
                    new Vector2(-30f, -70f),
                    34f,
                    26f,
                    40f,
                    new Color(0.88f, 0.94f, 1f, 0.96f),
                    TextAlignmentOptions.TopLeft);
            }
        }

        private Image CreateIcon()
        {
            GameObject holder = new GameObject("IconFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic));
            holder.transform.SetParent(transform, false);
            holder.transform.SetAsFirstSibling();

            RectTransform holderRect = holder.GetComponent<RectTransform>();
            holderRect.anchorMin = new Vector2(0f, 0.5f);
            holderRect.anchorMax = new Vector2(0f, 0.5f);
            holderRect.pivot = new Vector2(0.5f, 0.5f);
            holderRect.anchoredPosition = new Vector2(118f, 0f);
            holderRect.sizeDelta = new Vector2(178f, 112f);

            AllianceRoundedGraphic frame = holder.GetComponent<AllianceRoundedGraphic>();
            frame.color = new Color(0f, 0.012f, 0.025f, 0.56f);
            frame.CornerRadius = 26f;
            frame.CornerSegments = 12;
            frame.raycastTarget = false;

            GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(holder.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -12f);

            Image image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void RefreshIcon()
        {
            if (iconImage == null)
                return;

            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
        }

        private TMP_Text CreateText(string objectName, Vector2 offsetMin, Vector2 offsetMax, float fontSize, float fontSizeMin, float fontSizeMax, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            ApplyReadableFont(text);
            text.fontSize = fontSize;
            text.fontSizeMin = fontSizeMin;
            text.fontSizeMax = fontSizeMax;
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        public static void ApplyReadableFont(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = LoadReadableFont();
            if (font != null)
                text.font = font;
        }

        private static TMP_FontAsset LoadReadableFont()
        {
            if (cachedReadableFont != null)
                return cachedReadableFont;

            Font sourceFont = Resources.Load<Font>(ReadableFontResourcePath);
            if (sourceFont == null)
            {
                cachedReadableFont = Resources.Load<TMP_FontAsset>(ReadableFontResourcePath);
                if (cachedReadableFont != null)
                    cachedReadableFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                return cachedReadableFont;
            }

            cachedReadableFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (cachedReadableFont != null)
            {
                cachedReadableFont.name = "Exo2 Info Runtime SDF";
                cachedReadableFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            return cachedReadableFont;
        }
    }
}
