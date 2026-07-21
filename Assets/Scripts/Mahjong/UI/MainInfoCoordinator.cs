using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public enum MainInfoPlacement
    {
        Below,
        Above,
        Right,
        Left
    }

    [DisallowMultipleComponent]
    public sealed class MainInfoCoordinator : MonoBehaviour
    {
        private const float CardWidthFactor = 1.12f;
        private static readonly Color CardColor = new Color(0.025f, 0.033f, 0.052f, 0.94f);
        private static readonly Color CardOutlineColor = new Color(0.62f, 0.77f, 1f, 0.62f);
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color ButtonColor = new Color(0.075f, 0.145f, 0.235f, 0.98f);
        private static readonly Color ButtonHighlightColor = new Color(0.12f, 0.23f, 0.36f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.035f, 0.07f, 0.13f, 1f);
        private static readonly Color ButtonOutlineColor = new Color(0.78f, 0.9f, 1f, 0.88f);

        private static MainInfoCoordinator instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            AppSettings.OnInfoHintsChanged += OnInfoHintsChanged;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            AppSettings.OnInfoHintsChanged -= OnInfoHintsChanged;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        public static MainInfoCoordinator Ensure(Transform parent)
        {
            if (instance != null)
                return instance;

            GameObject go = new GameObject("MainInfoCoordinator");
            if (parent != null)
                go.transform.SetParent(parent, false);

            instance = go.AddComponent<MainInfoCoordinator>();
            return instance;
        }

        public static bool HintsEnabled => MainInfoHintTarget.FeatureEnabled &&
                                           (AppSettings.I == null || AppSettings.I.InfoHintsEnabled);

        public void CreateDimBackdrop(RectTransform parent)
        {
            if (parent == null)
                return;

            GameObject go = new GameObject("MainInfoDimBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button), typeof(MainInfoLayerElement));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = BackdropColor;
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { });

            go.GetComponent<MainInfoLayerElement>().SetVisible(HintsEnabled);
        }

        public MainInfoCard CreateCard(RectTransform parent, string objectName, Vector2 position, Vector2 size, string titleKey, string bodyKey, Sprite icon = null)
        {
            if (parent == null)
                return null;

            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(CanvasGroup), typeof(Shadow), typeof(Outline), typeof(MainInfoCard));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            AllianceRoundedGraphic graphic = go.GetComponent<AllianceRoundedGraphic>();
            graphic.color = CardColor;
            graphic.CornerRadius = Mathf.Min(54f, size.y * 0.28f);
            graphic.CornerSegments = 16;
            graphic.raycastTarget = true;

            Shadow shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = CardOutlineColor;
            outline.effectDistance = new Vector2(2.2f, -2.2f);

            MainInfoCard card = go.GetComponent<MainInfoCard>();
            card.Configure(titleKey, bodyKey, icon);
            card.SetVisible(HintsEnabled);
            return card;
        }

        public Button CreateUnderstoodButton(RectTransform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action = null)
        {
            if (parent == null)
                return null;

            GameObject go = new GameObject("BtnInfoUnderstood", typeof(RectTransform), typeof(CanvasRenderer), typeof(AllianceRoundedGraphic), typeof(CanvasGroup), typeof(Shadow), typeof(Outline), typeof(Button), typeof(MainInfoLayerElement));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            AllianceRoundedGraphic graphic = go.GetComponent<AllianceRoundedGraphic>();
            graphic.color = ButtonColor;
            graphic.CornerRadius = Mathf.Min(42f, size.y * 0.48f);
            graphic.CornerSegments = 16;
            graphic.raycastTarget = true;

            Shadow shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -6f);

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = ButtonOutlineColor;
            outline.effectDistance = new Vector2(2.6f, -2.6f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = ButtonHighlightColor,
                pressedColor = ButtonPressedColor,
                selectedColor = ButtonHighlightColor,
                disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEngine.Events.UnityAction clickAction = action ?? (() => ClearInfoLayer(parent));
            button.onClick.AddListener(clickAction);

            CreateButtonLabel(go.transform);
            go.GetComponent<MainInfoLayerElement>().SetVisible(HintsEnabled);
            return button;
        }

        public MainInfoCard Attach(Button button, string titleKey, string bodyKey, MainInfoPlacement placement = MainInfoPlacement.Below)
        {
            if (button == null)
                return null;

            RectTransform buttonRect = button.transform as RectTransform;
            RectTransform parent = buttonRect != null ? buttonRect.parent as RectTransform : null;
            if (buttonRect == null || parent == null)
                return null;

            Vector2 buttonSize = buttonRect.sizeDelta;
            Vector2 cardSize = new Vector2(Mathf.Max(520f, buttonSize.x * CardWidthFactor), 190f);
            Vector2 offset = placement switch
            {
                MainInfoPlacement.Above => new Vector2(0f, buttonSize.y * 0.5f + cardSize.y * 0.5f + 12f),
                MainInfoPlacement.Right => new Vector2(buttonSize.x * 0.5f + cardSize.x * 0.5f + 18f, 0f),
                MainInfoPlacement.Left => new Vector2(-buttonSize.x * 0.5f - cardSize.x * 0.5f - 18f, 0f),
                _ => new Vector2(0f, -buttonSize.y * 0.5f - cardSize.y * 0.5f - 12f)
            };

            MainInfoCard card = CreateCard(parent, button.name + "_Info", buttonRect.anchoredPosition + offset, cardSize, titleKey, bodyKey);
            if (card != null)
                card.transform.SetSiblingIndex(Mathf.Max(0, button.transform.GetSiblingIndex()));

            return card;
        }

        private void OnInfoHintsChanged(bool enabled)
        {
            MainInfoCard[] cards = FindObjectsByType<MainInfoCard>(FindObjectsInactive.Include);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                    cards[i].SetVisible(enabled);
            }

            MainInfoLayerElement[] elements = FindObjectsByType<MainInfoLayerElement>(FindObjectsInactive.Include);
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null)
                    elements[i].SetVisible(enabled);
            }
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            MainInfoCard[] cards = FindObjectsByType<MainInfoCard>(FindObjectsInactive.Include);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                    cards[i].RefreshText();
            }
        }

        private static void CreateButtonLabel(Transform parent)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -10f);

            TMPro.TextMeshProUGUI text = go.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = GameLocalization.Text("settings.info_understood");
            MainInfoCard.ApplyReadableFont(text);
            text.color = new Color(1f, 0.96f, 0.82f, 1f);
            text.enableVertexGradient = false;
            text.fontSize = 36f;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 42f;
            text.enableAutoSizing = true;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.raycastTarget = false;

            LocalizedText localized = go.AddComponent<LocalizedText>();
            localized.SetKey("settings.info_understood");
        }

        private static void ClearInfoLayer(RectTransform parent)
        {
            if (parent == null)
                return;

            MainInfoCard[] cards = parent.GetComponentsInChildren<MainInfoCard>(true);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                    Destroy(cards[i].gameObject);
            }

            MainInfoLayerElement[] elements = parent.GetComponentsInChildren<MainInfoLayerElement>(true);
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null)
                    Destroy(elements[i].gameObject);
            }
        }

    }
}
