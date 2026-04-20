using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleCharacterCircularCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Links")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform buttonsRoot;

        [Header("Buttons")]
        [SerializeField] private bool autoCollectButtons = true;
        [SerializeField] private List<BattleCharacterButton> buttons = new List<BattleCharacterButton>();

        [Header("Layout")]
        [SerializeField] private float spacing = 250f;
        [SerializeField] private int visibleSideCount = 1;

        [Header("Swipe")]
        [SerializeField] private float swipeThresholdPixels = 40f;
        [SerializeField] private float dragPreviewFactor = 0.18f;
        [SerializeField] private bool allowPreviewWhileDragging = true;

        [Header("Snap")]
        [SerializeField] private float snapSpeed = 10f;
        [SerializeField] private float snapFinishThreshold = 0.001f;

        [Header("Auto Scroll")]
        [SerializeField] private bool autoScroll = true;
        [SerializeField] private float autoScrollDelay = 2f;
        [SerializeField] private float autoScrollStepDelay = 2.5f;
        [SerializeField] private bool autoScrollToLeft = true;

        [Header("Visual")]
        [SerializeField] private bool animateScale = true;
        [SerializeField] private float centerScale = 1.32f;
        [SerializeField] private float sideScale = 0.72f;
        [SerializeField] private float scaleLerpSpeed = 8f;

        [Header("Alpha")]
        [SerializeField] private bool animateAlpha = true;
        [SerializeField] private float centerAlpha = 1f;
        [SerializeField] private float sideAlpha = 0.45f;
        [SerializeField] private float hiddenAlpha = 0f;
        [SerializeField] private float alphaLerpSpeed = 8f;

        [Header("Selection")]
        [SerializeField] private bool autoSelectCenteredCharacter = true;
        [SerializeField] private float selectDelayAfterSnap = 0.1f;

        private readonly Dictionary<BattleCharacterButton, CanvasGroup> canvasGroups = new Dictionary<BattleCharacterButton, CanvasGroup>();

        // ВАЖНО:
        // Эти позиции больше НЕ ограничиваются диапазоном 0..count-1.
        // Они могут расти бесконечно: 0,1,2,3,4,5,6,7,8,9...
        // Благодаря этому нет прыжка "в начало".
        private float currentVirtualIndex;
        private float targetVirtualIndex;
        private float visualVirtualIndex;

        private bool isDragging;
        private bool isSnapping;

        private float dragStartX;
        private float dragCurrentOffsetPixels;

        private float idleTimer;
        private float autoStepTimer;
        private float snappedTime;

        private BattleCharacterButton centeredButton;
        private string lastSelectedCenteredId = string.Empty;

        public BattleCharacterButton CenteredButton => centeredButton;

        private void Reset()
        {
            if (viewport == null)
                viewport = transform as RectTransform;

            if (buttonsRoot == null && transform.childCount > 0)
                buttonsRoot = transform.GetChild(0) as RectTransform;
        }

        private void Awake()
        {
            if (viewport == null)
                viewport = transform as RectTransform;

            if (buttonsRoot == null)
                buttonsRoot = transform as RectTransform;

            CollectButtonsIfNeeded();
            BindButtons();
            EnsureCanvasGroups();

            currentVirtualIndex = 0f;
            targetVirtualIndex = 0f;
            visualVirtualIndex = 0f;
        }

        private void OnEnable()
        {
            CollectButtonsIfNeeded();
            BindButtons();
            EnsureCanvasGroups();

            if (BattleCharacterSelectionService.HasInstance)
                BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;

            SnapToSelectedOrFirst(true);
            RefreshButtons();
        }

        private void OnDisable()
        {
            if (BattleCharacterSelectionService.HasInstance)
                BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
        }

        private void Update()
        {
            if (buttons.Count == 0)
                return;

            float dt = Time.unscaledDeltaTime;

            if (!isDragging)
                idleTimer += dt;
            else
                idleTimer = 0f;

            if (!isDragging && !isSnapping && autoScroll && idleTimer >= autoScrollDelay)
            {
                autoStepTimer += dt;

                if (autoStepTimer >= autoScrollStepDelay)
                {
                    autoStepTimer = 0f;
                    Step(autoScrollToLeft ? 1 : -1);
                }
            }
            else if (isDragging || isSnapping || idleTimer < autoScrollDelay)
            {
                autoStepTimer = 0f;
            }

            if (isSnapping)
            {
                visualVirtualIndex = Mathf.Lerp(visualVirtualIndex, targetVirtualIndex, snapSpeed * dt);

                if (Mathf.Abs(visualVirtualIndex - targetVirtualIndex) <= snapFinishThreshold)
                {
                    visualVirtualIndex = targetVirtualIndex;
                    currentVirtualIndex = targetVirtualIndex;
                    isSnapping = false;
                    snappedTime = Time.unscaledTime;
                }
            }
            else
            {
                if (allowPreviewWhileDragging && isDragging)
                {
                    float previewOffset = (dragCurrentOffsetPixels / Mathf.Max(1f, spacing)) * dragPreviewFactor;
                    visualVirtualIndex = currentVirtualIndex - previewOffset;
                }
                else
                {
                    visualVirtualIndex = Mathf.Lerp(visualVirtualIndex, currentVirtualIndex, snapSpeed * dt);
                }
            }

            UpdateButtonPositions();
            UpdateCenteredButton();
            UpdateVisualStates();
            TryAutoSelectCentered();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (buttons.Count == 0)
                return;

            isDragging = true;
            isSnapping = false;
            dragStartX = eventData.position.x;
            dragCurrentOffsetPixels = 0f;
            idleTimer = 0f;
            autoStepTimer = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            dragCurrentOffsetPixels = eventData.position.x - dragStartX;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;

            float delta = dragCurrentOffsetPixels;

            if (Mathf.Abs(delta) >= swipeThresholdPixels)
            {
                if (delta < 0f)
                    Step(1);
                else
                    Step(-1);
            }
            else
            {
                SnapToCurrent();
            }

            dragCurrentOffsetPixels = 0f;
            idleTimer = 0f;
            autoStepTimer = 0f;
        }

        public void FocusButton(BattleCharacterButton button, bool selectCharacter)
        {
            if (button == null || buttons.Count == 0)
                return;

            int baseIndex = buttons.IndexOf(button);
            if (baseIndex < 0)
                return;

            // Ищем ближайшее "вхождение" этой кнопки относительно текущей бесконечной позиции
            float delta = WrapDelta(baseIndex - currentVirtualIndex, buttons.Count);

            currentVirtualIndex += delta;
            targetVirtualIndex = currentVirtualIndex;
            isDragging = false;
            isSnapping = true;
            idleTimer = 0f;
            autoStepTimer = 0f;

            if (selectCharacter)
                button.SelectDirectly();
        }

        public void RefreshButtons()
        {
            CollectButtonsIfNeeded();
            BindButtons();
            EnsureCanvasGroups();

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                    buttons[i].Refresh();
            }

            UpdateButtonPositions();
            UpdateCenteredButton();
            UpdateVisualStates();
        }

        public void SnapToSelectedOrFirst(bool instant)
        {
            BattleCharacterButton target = FindButtonForCurrentSelection();
            if (target == null && buttons.Count > 0)
                target = buttons[0];

            if (target == null)
                return;

            int baseIndex = buttons.IndexOf(target);
            if (baseIndex < 0)
                return;

            float delta = WrapDelta(baseIndex - currentVirtualIndex, buttons.Count);

            if (instant)
            {
                currentVirtualIndex += delta;
                targetVirtualIndex = currentVirtualIndex;
                visualVirtualIndex = currentVirtualIndex;
                isSnapping = false;
            }
            else
            {
                currentVirtualIndex += delta;
                targetVirtualIndex = currentVirtualIndex;
                isSnapping = true;
            }

            UpdateButtonPositions();
            UpdateCenteredButton();
            UpdateVisualStates();
        }

        private void Step(int direction)
        {
            if (buttons.Count == 0)
                return;

            // ВАЖНО:
            // здесь больше нет WrapIndex -> 0..count-1
            // просто двигаемся дальше по бесконечной оси
            currentVirtualIndex += direction;
            targetVirtualIndex = currentVirtualIndex;
            isSnapping = true;
        }

        private void SnapToCurrent()
        {
            targetVirtualIndex = currentVirtualIndex;
            isSnapping = true;
        }

        private void CollectButtonsIfNeeded()
        {
            if (!autoCollectButtons || buttonsRoot == null)
                return;

            buttons.Clear();
            BattleCharacterButton[] found = buttonsRoot.GetComponentsInChildren<BattleCharacterButton>(true);

            for (int i = 0; i < found.Length; i++)
                buttons.Add(found[i]);
        }

        private void BindButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == null)
                    continue;

                buttons[i].SetOwnerCarousel(this);
            }
        }

        private void EnsureCanvasGroups()
        {
            canvasGroups.Clear();

            for (int i = 0; i < buttons.Count; i++)
            {
                BattleCharacterButton button = buttons[i];
                if (button == null)
                    continue;

                CanvasGroup group = button.GetComponent<CanvasGroup>();
                if (group == null)
                    group = button.gameObject.AddComponent<CanvasGroup>();

                canvasGroups[button] = group;
            }
        }

        private void UpdateButtonPositions()
        {
            int count = buttons.Count;
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                BattleCharacterButton button = buttons[i];
                if (button == null || button.RectTransform == null)
                    continue;

                float relative = WrapDelta(i - visualVirtualIndex, count);
                float x = relative * spacing;

                Vector2 anchored = button.RectTransform.anchoredPosition;
                anchored.x = x;
                button.RectTransform.anchoredPosition = anchored;
            }
        }

        private void UpdateCenteredButton()
        {
            centeredButton = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < buttons.Count; i++)
            {
                BattleCharacterButton button = buttons[i];
                if (button == null || button.RectTransform == null)
                    continue;

                float dist = Mathf.Abs(button.RectTransform.anchoredPosition.x);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    centeredButton = button;
                }
            }
        }

        private void UpdateVisualStates()
        {
            float visibleRange = (visibleSideCount + 0.5f) * spacing;

            for (int i = 0; i < buttons.Count; i++)
            {
                BattleCharacterButton button = buttons[i];
                if (button == null || button.RectTransform == null)
                    continue;

                float absX = Mathf.Abs(button.RectTransform.anchoredPosition.x);
                bool visible = absX <= visibleRange;
                float t = Mathf.Clamp01(absX / Mathf.Max(1f, spacing));

                if (animateScale)
                {
                    float scale = visible ? Mathf.Lerp(centerScale, sideScale, t) : sideScale;
                    Vector3 targetScale = new Vector3(scale, scale, 1f);
                    button.RectTransform.localScale = Vector3.Lerp(
                        button.RectTransform.localScale,
                        targetScale,
                        Time.unscaledDeltaTime * scaleLerpSpeed);
                }

                if (canvasGroups.TryGetValue(button, out CanvasGroup group))
                {
                    float targetAlpha = visible
                        ? Mathf.Lerp(centerAlpha, sideAlpha, t)
                        : hiddenAlpha;

                    if (!animateAlpha)
                        targetAlpha = visible ? 1f : 0f;

                    group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.unscaledDeltaTime * alphaLerpSpeed);
                    group.blocksRaycasts = visible;
                    group.interactable = visible;
                }

                button.SetHighlighted(button == centeredButton);
            }
        }

        private void TryAutoSelectCentered()
        {
            if (!autoSelectCenteredCharacter)
                return;

            if (centeredButton == null)
                return;

            if (isDragging || isSnapping)
                return;

            if (Time.unscaledTime - snappedTime < selectDelayAfterSnap)
                return;

            string id = centeredButton.CharacterId;
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (string.Equals(lastSelectedCenteredId, id, StringComparison.Ordinal))
                return;

            if (!BattleCharacterSelectionService.HasInstance)
                return;

            if (!BattleCharacterSelectionService.Instance.IsUnlocked(id))
                return;

            BattleCharacterSelectionService.Instance.SelectCharacter(id, true, true);
            lastSelectedCenteredId = id;
        }

        private BattleCharacterButton FindButtonForCurrentSelection()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            string selectedId = BattleCharacterSelectionService.Instance.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(selectedId))
                return null;

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == null)
                    continue;

                if (string.Equals(buttons[i].CharacterId, selectedId, StringComparison.Ordinal))
                    return buttons[i];
            }

            return null;
        }

        private static float WrapDelta(float value, int count)
        {
            if (count <= 0)
                return 0f;

            float half = count * 0.5f;
            return Mathf.Repeat(value + half, count) - half;
        }

        private void OnSelectedCharacterChanged(string _)
        {
            if (isDragging)
                return;

            SnapToSelectedOrFirst(false);
        }
    }
}
