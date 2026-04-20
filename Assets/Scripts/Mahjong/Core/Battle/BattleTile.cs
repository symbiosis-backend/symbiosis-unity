using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public enum BattleTileRevealState
    {
        Closed = 0,
        Revealed = 1,
        Matched = 2
    }

    // API: Commands | State | Events
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    public sealed class BattleTile : MonoBehaviour
    {
        public event Action<BattleTile> Clicked;
        public event Action<BattleTile> BlockedClicked;
        public event Action<BattleTile> StateChanged;

        [Header("Data")]
        [SerializeField] private string id;

        [Header("Sprites")]
        [SerializeField] private Sprite backSprite;
        [SerializeField] private Sprite faceSprite;

        [Header("Reveal")]
        [SerializeField] private bool startClosed = true;
        [SerializeField] private BattleTileRevealState revealState = BattleTileRevealState.Closed;

        [Header("Face Mode")]
        [SerializeField] private bool faceIsFullTileArt = true;

        [Header("Base Size")]
        [SerializeField] private Vector2 size = new(56f, 76f);

        [Header("Face Layout")]
        [SerializeField, Range(0.2f, 1f)] private float faceWidthPercent = 0.72f;
        [SerializeField, Range(0.2f, 1f)] private float faceHeightPercent = 0.72f;
        [SerializeField] private Vector2 faceOffset = Vector2.zero;
        [SerializeField] private bool preserveFaceAspect = true;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color blockedColor = new(0.62f, 0.62f, 0.62f, 1f);
        [SerializeField] private Color selectedColor = Color.white;

        [Header("Blocked Shake")]
        [SerializeField] private bool enableBlockedShake = true;
        [SerializeField, Min(0.01f)] private float blockedShakeDuration = 0.12f;
        [SerializeField, Min(0.1f)] private float blockedShakeOffset = 4f;
        [SerializeField, Min(2)] private int blockedShakeSteps = 6;

        [Header("Runtime")]
        [SerializeField] private RectTransform rect;
        [SerializeField] private Button button;
        [SerializeField] private Image backImage;
        [SerializeField] private Image faceImage;
        [SerializeField] private bool selected;
        [SerializeField] private bool blocked;
        [SerializeField] private bool interactable = true;

        private BattleBoard owner;
        private Coroutine blockedShakeRoutine;

        public string Id => id;
        public bool Selected => selected;
        public bool IsHidden => !gameObject.activeSelf;
        public bool IsBlocked => blocked;
        public bool IsInteractable => interactable;
        public RectTransform Rect => rect;
        public Vector2 Size => size;
        public BattleBoard Owner => owner;

        public BattleTileRevealState RevealState => revealState;
        public bool IsClosed => revealState == BattleTileRevealState.Closed;
        public bool IsRevealed => revealState == BattleTileRevealState.Revealed;
        public bool IsMatched => revealState == BattleTileRevealState.Matched;

        private void Reset()
        {
            InitializeVisuals();
            ResetTileState();
            StopBlockedShakeImmediate();
        }

        private void Awake()
        {
            InitializeVisuals();
            StopBlockedShakeImmediate();
        }

        private void OnEnable()
        {
            InitializeVisuals();
            StopBlockedShakeImmediate();
        }

        private void OnDisable()
        {
            StopBlockedShakeImmediate();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            size.x = Mathf.Max(16f, size.x);
            size.y = Mathf.Max(16f, size.y);
            faceWidthPercent = Mathf.Clamp(faceWidthPercent, 0.2f, 1f);
            faceHeightPercent = Mathf.Clamp(faceHeightPercent, 0.2f, 1f);
            blockedShakeDuration = Mathf.Max(0.01f, blockedShakeDuration);
            blockedShakeOffset = Mathf.Max(0.1f, blockedShakeOffset);
            blockedShakeSteps = Mathf.Max(2, blockedShakeSteps);

            InitializeVisuals();
            StopBlockedShakeImmediate();
        }
#endif

        public void Setup(string newId, BattleBoard boardOwner)
        {
            id = string.IsNullOrWhiteSpace(newId) ? string.Empty : newId;
            owner = boardOwner;

            ResetTileState();
            InitializeVisuals();
            StopBlockedShakeImmediate();

            gameObject.SetActive(true);
            NotifyStateChanged();
        }

        public void SetOwner(BattleBoard boardOwner)
        {
            owner = boardOwner;
        }

        public void SetSelected(bool value)
        {
            if (selected == value)
                return;

            selected = value;
            RefreshColor();
            NotifyStateChanged();
        }

        public void SetBlocked(bool value)
        {
            if (blocked == value)
                return;

            blocked = value;
            RefreshColor();

            if (!blocked)
                StopBlockedShakeImmediate();

            NotifyStateChanged();
        }

        public void SetInteractable(bool value)
        {
            if (interactable == value)
                return;

            interactable = value;

            if (button != null)
                button.interactable = value;

            NotifyStateChanged();
        }

        public void SetId(string newId)
        {
            id = string.IsNullOrWhiteSpace(newId) ? string.Empty : newId;
            NotifyStateChanged();
        }

        public void SetSprites(Sprite newBack, Sprite newFace)
        {
            backSprite = newBack;
            faceSprite = newFace;
            InitializeVisuals();
            StopBlockedShakeImmediate();
            NotifyStateChanged();
        }

        public void ResetTileState()
        {
            selected = false;
            blocked = false;
            interactable = true;
            revealState = startClosed ? BattleTileRevealState.Closed : BattleTileRevealState.Revealed;

            if (button != null)
                button.interactable = true;

            RefreshVisualState();
            RefreshColor();
            NotifyStateChanged();
        }

        public void ResetRevealState()
        {
            revealState = startClosed ? BattleTileRevealState.Closed : BattleTileRevealState.Revealed;
            RefreshVisualState();
            NotifyStateChanged();
        }

        public void Reveal()
        {
            if (revealState == BattleTileRevealState.Matched)
                return;

            if (revealState == BattleTileRevealState.Revealed)
                return;

            revealState = BattleTileRevealState.Revealed;
            RefreshVisualState();
            NotifyStateChanged();
        }

        public void HideToBack()
        {
            if (revealState == BattleTileRevealState.Matched)
                return;

            if (revealState == BattleTileRevealState.Closed)
                return;

            revealState = BattleTileRevealState.Closed;
            RefreshVisualState();
            NotifyStateChanged();
        }

        public void SetMatched()
        {
            if (revealState == BattleTileRevealState.Matched)
                return;

            revealState = BattleTileRevealState.Matched;
            selected = false;
            blocked = false;
            interactable = false;

            if (button != null)
                button.interactable = false;

            RefreshVisualState();
            RefreshColor();
            NotifyStateChanged();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            NotifyStateChanged();
        }

        public void HideNow()
        {
            StopBlockedShakeImmediate();
            ResetTileState();
            gameObject.SetActive(false);
            NotifyStateChanged();
        }

        public bool CanBeClicked()
        {
            return gameObject.activeSelf && interactable && !IsMatched;
        }

        private void OnClick()
        {
            if (!CanBeClicked())
                return;

            if (blocked)
            {
                PlayBlockedShake();
                BlockedClicked?.Invoke(this);
                return;
            }

            Clicked?.Invoke(this);

            if (owner != null)
                owner.TrySelectTileProxy(this);
        }

        private void PlayBlockedShake()
        {
            if (!enableBlockedShake || !gameObject.activeInHierarchy)
                return;

            if (blockedShakeRoutine != null)
                StopCoroutine(blockedShakeRoutine);

            blockedShakeRoutine = StartCoroutine(BlockedShakeRoutine());
        }

        private IEnumerator BlockedShakeRoutine()
        {
            Vector2 backBasePos = backImage != null ? backImage.rectTransform.anchoredPosition : Vector2.zero;
            Vector2 faceBasePos = faceImage != null ? faceImage.rectTransform.anchoredPosition : faceIsFullTileArt ? Vector2.zero : faceOffset;
            float stepTime = blockedShakeDuration / blockedShakeSteps;

            for (int i = 0; i < blockedShakeSteps; i++)
            {
                float dir = (i % 2 == 0) ? -1f : 1f;
                Vector2 offset = new(blockedShakeOffset * dir, 0f);

                if (backImage != null)
                    backImage.rectTransform.anchoredPosition = backBasePos + offset;

                if (faceImage != null)
                    faceImage.rectTransform.anchoredPosition = faceBasePos + offset;

                yield return new WaitForSeconds(stepTime);
            }

            if (backImage != null)
                backImage.rectTransform.anchoredPosition = backBasePos;

            if (faceImage != null)
                faceImage.rectTransform.anchoredPosition = faceBasePos;

            blockedShakeRoutine = null;
        }

        private void StopBlockedShakeImmediate()
        {
            if (blockedShakeRoutine != null)
            {
                StopCoroutine(blockedShakeRoutine);
                blockedShakeRoutine = null;
            }

            if (backImage != null)
                backImage.rectTransform.anchoredPosition = Vector2.zero;

            if (faceImage != null)
                faceImage.rectTransform.anchoredPosition = faceIsFullTileArt ? Vector2.zero : faceOffset;
        }

        private void InitializeVisuals()
        {
            CacheRefs();
            EnsureLayers();
            ApplyVisual();
            RefreshVisualState();
            RefreshColor();
            BindButton();
        }

        private void CacheRefs()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            if (button == null)
                button = GetComponent<Button>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = size;
        }

        private void EnsureLayers()
        {
            backImage = GetOrCreateLayer("Back", backImage, 0);
            faceImage = GetOrCreateLayer("Face", faceImage, 1);

            ConfigureBackLayer();
            ConfigureFaceLayer();
        }

        private Image GetOrCreateLayer(string childName, Image cached, int siblingIndex)
        {
            GameObject go = null;

            if (cached != null)
            {
                go = cached.gameObject;
            }
            else
            {
                Transform found = transform.Find(childName);
                if (found != null)
                    go = found.gameObject;
            }

            if (go == null)
            {
                go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
            }

            RectTransform childRect = go.GetComponent<RectTransform>();
            if (childRect == null)
                childRect = go.AddComponent<RectTransform>();

            Image img = go.GetComponent<Image>();
            if (img == null)
                img = go.AddComponent<Image>();

            go.transform.SetSiblingIndex(siblingIndex);
            childRect.localScale = Vector3.one;
            childRect.localRotation = Quaternion.identity;

            return img;
        }

        private void ConfigureBackLayer()
        {
            if (backImage == null)
                return;

            RectTransform rt = backImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            backImage.type = Image.Type.Simple;
            backImage.preserveAspect = false;
            backImage.raycastTarget = true;
        }

        private void ConfigureFaceLayer()
        {
            if (faceImage == null)
                return;

            RectTransform rt = faceImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            faceImage.type = Image.Type.Simple;
            faceImage.preserveAspect = preserveFaceAspect;
            faceImage.raycastTarget = false;
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
            button.transition = Selectable.Transition.None;
            button.interactable = interactable;

            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            button.colors = cb;
        }

        private void ApplyVisual()
        {
            if (rect == null)
                return;

            rect.sizeDelta = size;

            bool hasBack = backSprite != null;
            bool hasFace = faceSprite != null;

            if (faceIsFullTileArt)
            {
                if (backImage != null)
                {
                    backImage.sprite = backSprite;
                    backImage.enabled = hasBack && !hasFace;
                    backImage.rectTransform.sizeDelta = size;
                    backImage.rectTransform.anchoredPosition = Vector2.zero;
                }

                if (faceImage != null)
                {
                    faceImage.sprite = faceSprite;
                    faceImage.enabled = hasFace;
                    faceImage.rectTransform.sizeDelta = size;
                    faceImage.rectTransform.anchoredPosition = Vector2.zero;
                    faceImage.preserveAspect = false;
                }
            }
            else
            {
                if (backImage != null)
                {
                    backImage.sprite = backSprite;
                    backImage.enabled = hasBack;
                    backImage.rectTransform.sizeDelta = size;
                    backImage.rectTransform.anchoredPosition = Vector2.zero;
                }

                if (faceImage != null)
                {
                    faceImage.sprite = faceSprite;
                    faceImage.enabled = hasFace;
                    faceImage.rectTransform.sizeDelta = new Vector2(size.x * faceWidthPercent, size.y * faceHeightPercent);
                    faceImage.rectTransform.anchoredPosition = faceOffset;
                    faceImage.preserveAspect = preserveFaceAspect;
                }
            }
        }

        private void RefreshVisualState()
        {
            if (backImage == null || faceImage == null)
                return;

            switch (revealState)
            {
                case BattleTileRevealState.Closed:
                    backImage.enabled = backSprite != null;
                    faceImage.enabled = false;
                    break;

                case BattleTileRevealState.Revealed:
                    backImage.enabled = backSprite != null && !faceIsFullTileArt && backSprite != null;
                    faceImage.enabled = faceSprite != null;
                    break;

                case BattleTileRevealState.Matched:
                    backImage.enabled = false;
                    faceImage.enabled = false;
                    break;
            }
        }

        private void RefreshColor()
        {
            Color tint = blocked ? blockedColor : selected ? selectedColor : normalColor;

            if (backImage != null)
                backImage.color = tint;

            if (faceImage != null)
                faceImage.color = tint;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }
    }
}