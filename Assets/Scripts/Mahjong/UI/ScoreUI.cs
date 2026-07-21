using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ScoreUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string prefix = "Puan: ";
        [SerializeField] private Vector2 anchoredPosition = new Vector2(410f, -92f);
        [SerializeField] private Vector2 size = new Vector2(330f, 88f);
        [SerializeField] private string trayObjectName = "TrayArea";
        [SerializeField] private string trayVisualObjectName = "TraySlotsRoot";
        [SerializeField] private float trayGap = 18f;
        [SerializeField] private float screenEdgePadding = 18f;
        [SerializeField] private float baseFontSize = 58f;
        [SerializeField] private float punchScale = 1.18f;
        [SerializeField] private float punchSpeed = 9f;

        private static TMP_FontAsset cachedHudFont;
        private RectTransform trayRect;
        private int lastScore = int.MinValue;
        private float punch;
        private Vector3 baseScale = Vector3.one;
        private readonly Vector3[] trayWorldCorners = new Vector3[4];
        private static readonly VertexGradient ScoreGradient = new VertexGradient(
            new Color32(255, 252, 165, 255),
            new Color32(255, 225, 78, 255),
            new Color32(255, 157, 24, 255),
            new Color32(255, 205, 54, 255));

        private void Reset()
        {
            if (!scoreText)
                scoreText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (!scoreText)
                scoreText = GetComponent<TMP_Text>();

            ApplyLayout();
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyLayout();
            ApplyStyle();
            if (scoreText != null)
                scoreText.text = prefix + "0";

            lastScore = int.MinValue;
        }

        private void Update()
        {
            ApplyLayout();

            if (!scoreText)
                return;

            int score = ScoreSystem.I != null ? ScoreSystem.I.CurrentLevelScore : 0;
            if (score != lastScore)
            {
                scoreText.text = prefix + score;
                punch = 1f;
                lastScore = score;
            }

            AnimatePunch();
        }

        private void ApplyLayout()
        {
            ApplyHudRootLayout();

            RectTransform rect = scoreText != null ? scoreText.rectTransform : transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = ResolveAnchoredPosition(rect, true);
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
                return ResolveFallbackPosition(rect, true);

            if (parent.rect.width <= 1f || parent.rect.height <= 1f)
                return anchoredPosition;

            if (!TryGetTraySelfBounds(parent, tray, out Vector2 min, out Vector2 max))
                return ResolveFallbackPosition(rect, true);

            float x = rightSide
                ? max.x + trayGap + rect.sizeDelta.x * 0.5f
                : min.x - trayGap - rect.sizeDelta.x * 0.5f;

            float parentHalfWidth = parent.rect.width * 0.5f;
            float halfWidth = rect.sizeDelta.x * 0.5f;
            if (parentHalfWidth <= halfWidth + screenEdgePadding)
                return ResolveFallbackPosition(rect, true);

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
            if (scoreText == null)
                return;

            ApplyHudFont(scoreText);
            scoreText.gameObject.SetActive(true);
            scoreText.color = Color.white;
            scoreText.enableVertexGradient = true;
            scoreText.colorGradient = ScoreGradient;
            scoreText.fontSize = baseFontSize;
            scoreText.fontSizeMin = 34f;
            scoreText.fontSizeMax = baseFontSize + 10f;
            scoreText.enableAutoSizing = true;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.alignment = TextAlignmentOptions.Left;
            scoreText.textWrappingMode = TextWrappingModes.NoWrap;
            scoreText.overflowMode = TextOverflowModes.Ellipsis;
            scoreText.raycastTarget = false;
            scoreText.outlineWidth = 0.34f;
            scoreText.outlineColor = new Color(0.02f, 0.025f, 0.005f, 1f);

            Shadow shadow = scoreText.GetComponent<Shadow>();
            if (shadow == null)
                shadow = scoreText.gameObject.AddComponent<Shadow>();

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

        private void AnimatePunch()
        {
            RectTransform rect = scoreText.rectTransform;
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * punchSpeed);
            float scale = Mathf.Lerp(1f, punchScale, punch);
            rect.localScale = baseScale * scale;
        }
    }
}
