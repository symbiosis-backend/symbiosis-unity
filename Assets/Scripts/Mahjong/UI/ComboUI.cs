using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ComboUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private string comboPrefix = "x";
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-360f, -92f);
        [SerializeField] private Vector2 size = new Vector2(220f, 88f);
        [SerializeField] private string trayObjectName = "TrayArea";
        [SerializeField] private string trayVisualObjectName = "TraySlotsRoot";
        [SerializeField] private float trayGap = 18f;
        [SerializeField] private float screenEdgePadding = 18f;
        [SerializeField] private float baseFontSize = 60f;
        [SerializeField] private float punchScale = 1.22f;
        [SerializeField] private float punchSpeed = 9f;

        private static TMP_FontAsset cachedHudFont;
        private RectTransform trayRect;
        private int lastMultiplier = int.MinValue;
        private float punch;
        private Vector3 baseScale = Vector3.one;
        private readonly Vector3[] trayWorldCorners = new Vector3[4];
        private static readonly VertexGradient ComboGradient = new VertexGradient(
            new Color32(225, 255, 139, 255),
            new Color32(108, 255, 102, 255),
            new Color32(25, 176, 78, 255),
            new Color32(128, 255, 108, 255));
        private static readonly VertexGradient ComboHotGradient = new VertexGradient(
            new Color32(255, 250, 132, 255),
            new Color32(165, 255, 95, 255),
            new Color32(255, 174, 28, 255),
            new Color32(118, 255, 102, 255));

        private void Reset()
        {
            if (!comboText)
                comboText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (!comboText)
                comboText = GetComponent<TMP_Text>();

            ApplyLayout();
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyLayout();
            ApplyStyle();
            if (comboText != null)
                comboText.text = comboPrefix + "1";

            lastMultiplier = int.MinValue;
        }

        private void Update()
        {
            ApplyLayout();

            if (!comboText)
                return;

            int multiplier = ComboSystem.I != null ? Mathf.Max(1, ComboSystem.I.ComboLevel + 1) : 1;
            if (multiplier != lastMultiplier)
            {
                comboText.text = comboPrefix + multiplier;
                punch = 1f;
                lastMultiplier = multiplier;
            }

            AnimatePunch(multiplier);
        }

        private void ApplyLayout()
        {
            ApplyHudRootLayout();

            RectTransform rect = comboText != null ? comboText.rectTransform : transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = ResolveAnchoredPosition(rect, false);
            rect.localRotation = Quaternion.identity;
            baseScale = Vector3.one;
            rect.localScale = baseScale;
        }

        private void ApplyHudRootLayout()
        {
            RectTransform parent = transform.parent as RectTransform;
            if (parent == null)
                return;

            parent.anchorMin = Vector2.zero;
            parent.anchorMax = Vector2.one;
            parent.pivot = new Vector2(0.5f, 0.5f);
            parent.offsetMin = Vector2.zero;
            parent.offsetMax = Vector2.zero;
            parent.anchoredPosition = Vector2.zero;
            parent.localScale = Vector3.one;
            parent.localRotation = Quaternion.identity;
            parent.SetAsLastSibling();
        }

        private Vector2 ResolveAnchoredPosition(RectTransform rect, bool rightSide)
        {
            RectTransform parent = transform.parent as RectTransform;
            RectTransform tray = ResolveTrayRect();
            if (parent == null || tray == null)
                return ResolveFallbackPosition(rect, rightSide);

            if (parent.rect.width <= 1f || parent.rect.height <= 1f)
                return anchoredPosition;

            if (!TryGetTraySelfBounds(parent, tray, out Vector2 min, out Vector2 max))
                return ResolveFallbackPosition(rect, rightSide);

            float x = rightSide
                ? max.x + trayGap + rect.sizeDelta.x * 0.5f
                : min.x - trayGap - rect.sizeDelta.x * 0.5f;

            float parentHalfWidth = parent.rect.width * 0.5f;
            float halfWidth = rect.sizeDelta.x * 0.5f;
            if (parentHalfWidth <= halfWidth + screenEdgePadding)
                return ResolveFallbackPosition(rect, rightSide);

            x = Mathf.Clamp(x, -parentHalfWidth + halfWidth + screenEdgePadding, parentHalfWidth - halfWidth - screenEdgePadding);

            return new Vector2(x, (min.y + max.y) * 0.5f);
        }

        private bool TryGetTraySelfBounds(RectTransform parent, RectTransform tray, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;
            if (parent == null || tray == null || tray.rect.width <= 1f || tray.rect.height <= 1f)
                return false;

            tray.GetWorldCorners(trayWorldCorners);
            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < trayWorldCorners.Length; i++)
            {
                Vector3 local = parent.InverseTransformPoint(trayWorldCorners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            return max.x - min.x > 1f && max.y - min.y > 1f;
        }

        private Vector2 ResolveFallbackPosition(RectTransform rect, bool rightSide)
        {
            RectTransform parent = transform.parent as RectTransform;
            if (parent == null || parent.rect.width <= 1f || parent.rect.height <= 1f)
                return anchoredPosition;

            float halfWidth = rect != null ? rect.sizeDelta.x * 0.5f : size.x * 0.5f;
            float x = parent.rect.width * 0.18f + halfWidth;
            if (!rightSide)
                x = -x;

            float y = parent.rect.height * 0.5f - 95f;
            return new Vector2(x, y);
        }

        private RectTransform ResolveTrayRect()
        {
            if (trayRect != null && trayRect.gameObject.activeInHierarchy)
                return trayRect;

            GameObject found = GameObject.Find(trayObjectName);
            if (found == null)
            {
                trayRect = null;
                return trayRect;
            }

            Transform visual = found.transform.Find(trayVisualObjectName);
            trayRect = visual != null ? visual as RectTransform : found.transform as RectTransform;
            return trayRect;
        }

        private void ApplyStyle()
        {
            if (comboText == null)
                return;

            ApplyHudFont(comboText);
            comboText.gameObject.SetActive(true);
            comboText.color = Color.white;
            comboText.enableVertexGradient = true;
            comboText.colorGradient = ComboGradient;
            comboText.fontSize = baseFontSize;
            comboText.fontSizeMin = 36f;
            comboText.fontSizeMax = baseFontSize + 12f;
            comboText.enableAutoSizing = true;
            comboText.fontStyle = FontStyles.Bold;
            comboText.alignment = TextAlignmentOptions.Right;
            comboText.textWrappingMode = TextWrappingModes.NoWrap;
            comboText.overflowMode = TextOverflowModes.Ellipsis;
            comboText.raycastTarget = false;
            comboText.outlineWidth = 0.36f;
            comboText.outlineColor = new Color(0.005f, 0.035f, 0.008f, 1f);

            Shadow shadow = comboText.GetComponent<Shadow>();
            if (shadow == null)
                shadow = comboText.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            shadow.effectDistance = new Vector2(3f, -4f);
        }

        private static void ApplyHudFont(TMP_Text text)
        {
            if (text == null)
                return;

            if (cachedHudFont == null)
                cachedHudFont = Resources.Load<TMP_FontAsset>("Fonts/Trade SDF");

            if (cachedHudFont == null)
                cachedHudFont = MainLobbyButtonStyle.Font;

            if (cachedHudFont != null)
                text.font = cachedHudFont;
        }

        private void AnimatePunch(int multiplier)
        {
            RectTransform rect = comboText.rectTransform;
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * punchSpeed);
            float scale = Mathf.Lerp(1f, punchScale, punch);
            rect.localScale = baseScale * scale;

            comboText.colorGradient = multiplier > 1 && Mathf.PingPong(Time.unscaledTime * 3.5f, 1f) > 0.5f
                ? ComboHotGradient
                : ComboGradient;
        }
    }
}
