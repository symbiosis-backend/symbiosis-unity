using UnityEngine;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyTileInstance : MonoBehaviour
    {
        [Header("Tile Meta")]
        public TileColor Color = TileColor.Black;
        [Range(1, 13)] public int Number = 1;
        public bool IsJoker;

        [Header("Sprites")]
        public Sprite FrontSprite;
        public Sprite BackSprite;

        [Header("Runtime")]
        [SerializeField] private int runtimeId = -1;
        [SerializeField] private bool faceVisible = true;

        private RectTransform rootRect;
        private Image frontImage;
        private Image backImage;
        private RectTransform frontRect;
        private RectTransform backRect;
        private CanvasGroup frontCanvasGroup;
        private CanvasGroup backCanvasGroup;

        public int RuntimeId => runtimeId;
        public bool FaceVisible => faceVisible;

        private void Awake()
        {
            rootRect = GetComponent<RectTransform>();
            EnsureVisuals();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();

            EnsureVisuals();
            RefreshVisuals();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (IsJoker)
                Color = TileColor.Joker;

            if (Color == TileColor.Joker)
                IsJoker = true;

            if (!IsJoker)
            {
                if (Number < 1) Number = 1;
                if (Number > 13) Number = 13;
            }
        }
#endif

        public void SetRuntimeId(int id)
        {
            runtimeId = id;
        }

        public void SetTileData(TileColor color, int number, bool isJoker, int id = -1)
        {
            Color = color;
            Number = Mathf.Clamp(number, 1, 13);
            IsJoker = isJoker;
            runtimeId = id;

            if (IsJoker)
                Color = TileColor.Joker;
        }

        public void SetFaceVisible(bool visible)
        {
            faceVisible = visible;
            ApplyFaceState();
        }

        public void SetFaceDown(bool value)
        {
            faceVisible = !value;
            ApplyFaceState();
        }

        public void ShowFront()
        {
            faceVisible = true;
            ApplyFaceState();
        }

        public void ShowBack()
        {
            faceVisible = false;
            ApplyFaceState();
        }

        public void SetSprites(Sprite front, Sprite back)
        {
            FrontSprite = front;
            BackSprite = back;

            EnsureVisuals();
            ApplySprites();
            NormalizeVisualGeometry();
            ApplyFaceState();
        }

        public void RefreshVisuals()
        {
            EnsureVisuals();
            NormalizeVisualGeometry();
            ApplySprites();
            ApplyFaceState();
        }

        private void EnsureVisuals()
        {
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();

            if (frontImage == null)
            {
                Transform front = transform.Find("Front");
                if (front == null)
                {
                    GameObject go = new GameObject("Front", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                    go.transform.SetParent(transform, false);
                    front = go.transform;
                }

                frontRect = front as RectTransform;
                if (frontRect == null)
                    frontRect = front.gameObject.GetComponent<RectTransform>();

                Stretch(frontRect);

                frontImage = front.GetComponent<Image>();
                if (frontImage == null)
                    frontImage = front.gameObject.AddComponent<Image>();
            }
            else if (frontRect == null)
            {
                frontRect = frontImage.transform as RectTransform;
            }

            if (backImage == null)
            {
                Transform back = transform.Find("Back");
                if (back == null)
                {
                    GameObject go = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                    go.transform.SetParent(transform, false);
                    back = go.transform;
                }

                backRect = back as RectTransform;
                if (backRect == null)
                    backRect = back.gameObject.GetComponent<RectTransform>();

                Stretch(backRect);

                backImage = back.GetComponent<Image>();
                if (backImage == null)
                    backImage = back.gameObject.AddComponent<Image>();
            }
            else if (backRect == null)
            {
                backRect = backImage.transform as RectTransform;
            }

            if (frontImage != null && frontCanvasGroup == null)
                frontCanvasGroup = frontImage.GetComponent<CanvasGroup>();

            if (backImage != null && backCanvasGroup == null)
                backCanvasGroup = backImage.GetComponent<CanvasGroup>();

            if (frontCanvasGroup == null && frontImage != null)
                frontCanvasGroup = frontImage.gameObject.AddComponent<CanvasGroup>();

            if (backCanvasGroup == null && backImage != null)
                backCanvasGroup = backImage.gameObject.AddComponent<CanvasGroup>();

            if (backImage != null && backImage.transform.GetSiblingIndex() != 0)
                backImage.transform.SetSiblingIndex(0);

            if (frontImage != null && frontImage.transform.GetSiblingIndex() != transform.childCount - 1)
                frontImage.transform.SetSiblingIndex(transform.childCount - 1);

            ConfigureImage(frontImage);
            ConfigureImage(backImage);
            ConfigureCanvasGroup(frontCanvasGroup);
            ConfigureCanvasGroup(backCanvasGroup);
        }

        private void ConfigureImage(Image img)
        {
            if (img == null)
                return;

            img.raycastTarget = false;
            img.preserveAspect = false;
            img.type = Image.Type.Simple;
            img.useSpriteMesh = false;
        }

        private void ConfigureCanvasGroup(CanvasGroup cg)
        {
            if (cg == null)
                return;

            cg.blocksRaycasts = false;
            cg.interactable = false;
            if (cg.alpha < 0f || cg.alpha > 1f)
                cg.alpha = 1f;
        }

        private void NormalizeVisualGeometry()
        {
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();

            if (frontRect != null)
                Stretch(frontRect);

            if (backRect != null)
                Stretch(backRect);

            if (frontImage != null)
            {
                ConfigureImage(frontImage);
                Stretch(frontRect);
            }

            if (backImage != null)
            {
                ConfigureImage(backImage);
                Stretch(backRect);
            }
        }

        private void ApplySprites()
        {
            if (frontImage != null)
                frontImage.sprite = FrontSprite;

            if (backImage != null)
                backImage.sprite = BackSprite;
        }

        private void ApplyFaceState()
        {
            if (frontImage == null || backImage == null)
                return;

            bool showFront = faceVisible && frontImage.sprite != null;
            bool showBack = !faceVisible && backImage.sprite != null;

            ApplyVisualState(frontImage, frontCanvasGroup, showFront);
            ApplyVisualState(backImage, backCanvasGroup, showBack);
        }

        private void ApplyVisualState(Image img, CanvasGroup cg, bool visible)
        {
            if (img == null)
                return;

            img.enabled = visible;

            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}