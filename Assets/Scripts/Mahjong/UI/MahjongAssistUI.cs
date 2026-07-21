using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MahjongGame.Monetization;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongAssistUI : MonoBehaviour
    {
        private const string ObjectName = "MahjongAssistUI";
        private const string HintIconResourcePath = "Mahjong/Sprites/Assist/NaytiParuIcon";
        private const string ShuffleIconResourcePath = "Mahjong/Sprites/Assist/PeremeshatiIcon";
        private const string UndoIconResourcePath = "Mahjong/Sprites/Assist/HodnazadIcon";
        private static readonly Vector2 RootPosition = new Vector2(34f, -18f);
        private static readonly Vector2 RootSize = new Vector2(500f, 158f);
        private static readonly Vector2 ButtonSize = new Vector2(142f, 142f);
        private const float ButtonStep = 158f;

        private static readonly Color PanelColor = new Color(0.02f, 0.12f, 0.09f, 0.78f);
        private static readonly Color ButtonColor = new Color(0.04f, 0.24f, 0.17f, 0.95f);
        private static readonly Color ButtonPressedColor = new Color(0.01f, 0.09f, 0.07f, 0.98f);
        private static readonly Color TextColor = new Color(1f, 0.88f, 0.46f, 1f);

        private Board board;
        private TMP_Text statusText;
        private TMP_Text hintCountText;
        private TMP_Text shuffleCountText;
        private TMP_Text undoCountText;
        private Button hintButton;
        private Button shuffleButton;
        private Button undoButton;
        private Sprite hintIcon;
        private Sprite shuffleIcon;
        private Sprite undoIcon;
        private bool rewardedAdRequestInProgress;

        private static MahjongAssistUI current;
        private static bool visibleRequested;

        public static MahjongAssistUI Ensure(Board targetBoard)
        {
            if (targetBoard == null)
                return null;

            Canvas canvas = targetBoard.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null)
                return null;

            MahjongAssistUI ui = ResolveSingleInstance(canvas);
            if (ui == null)
            {
                GameObject go = new GameObject(ObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MahjongAssistUI));
                go.transform.SetParent(canvas.transform, false);
                ui = go.GetComponent<MahjongAssistUI>();
                ui.Build();
            }
            else
            {
                ui.Rebuild();
            }

            current = ui;
            ui.SetBoard(targetBoard);
            ui.gameObject.SetActive(visibleRequested && MahjongGameRuntime.AssistUiAllowed);
            ui.transform.SetAsLastSibling();
            return ui;
        }

        public static void SetVisible(bool visible)
        {
            visibleRequested = visible;

            MahjongAssistUI[] all = FindObjectsByType<MahjongAssistUI>(FindObjectsInactive.Include);
            MahjongAssistUI keep = null;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;

                if (keep == null)
                {
                    keep = all[i];
                    current = keep;
                    keep.gameObject.SetActive(visible && MahjongGameRuntime.AssistUiAllowed);
                    continue;
                }

                all[i].gameObject.SetActive(false);
                Destroy(all[i].gameObject);
            }

            if (current == null)
                current = FindAnyObjectByType<MahjongAssistUI>();
        }

        private static MahjongAssistUI ResolveSingleInstance(Canvas preferredCanvas)
        {
            MahjongAssistUI[] all = FindObjectsByType<MahjongAssistUI>(FindObjectsInactive.Include);
            MahjongAssistUI keep = null;

            for (int i = 0; i < all.Length; i++)
            {
                MahjongAssistUI ui = all[i];
                if (ui == null)
                    continue;

                Canvas uiCanvas = ui.GetComponentInParent<Canvas>();
                if (keep == null || uiCanvas == preferredCanvas)
                    keep = ui;
            }

            for (int i = 0; i < all.Length; i++)
            {
                MahjongAssistUI ui = all[i];
                if (ui == null || ui == keep)
                    continue;

                ui.gameObject.SetActive(false);
                Destroy(ui.gameObject);
            }

            return keep;
        }

        private void SetBoard(Board targetBoard)
        {
            board = targetBoard;
            MahjongAssistInventoryService.EnsureInitialized();
            RefreshCounts();
            SetStatus("");
        }

        private void OnEnable()
        {
            if (!MahjongGameRuntime.AssistUiAllowed)
            {
                gameObject.SetActive(false);
                return;
            }

            MahjongAssistInventoryService.EnsureInitialized();
            RefreshCounts();
        }

        private void Update()
        {
            if (!MahjongGameRuntime.AssistUiAllowed)
                gameObject.SetActive(false);
        }

        private void Build()
        {
            hintIcon = Resources.Load<Sprite>(HintIconResourcePath);
            shuffleIcon = Resources.Load<Sprite>(ShuffleIconResourcePath);
            undoIcon = Resources.Load<Sprite>(UndoIconResourcePath);

            RectTransform rect = GetComponent<RectTransform>();
            ApplyRootLayout(rect);

            Image panel = GetComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0f);
            panel.raycastTarget = false;

            hintButton = CreateButton("HintButton", hintIcon, "?", "ПАРА", new Vector2(-ButtonStep, 0f), OnHintClicked, out hintCountText);
            shuffleButton = CreateButton("ShuffleButton", shuffleIcon, "MIX", "МЕШАТЬ", new Vector2(0f, 0f), OnShuffleClicked, out shuffleCountText);
            undoButton = CreateButton("UndoButton", undoIcon, "<", "ОТМЕНА", new Vector2(ButtonStep, 0f), OnUndoClicked, out undoCountText);

            statusText = CreateText("AssistStatus", "", new Vector2(0f, -86f), new Vector2(430f, 26f), 18f);
            statusText.alignment = TextAlignmentOptions.Center;
            RefreshCounts();
        }

        private void Rebuild()
        {
            hintButton = null;
            shuffleButton = null;
            undoButton = null;
            hintCountText = null;
            shuffleCountText = null;
            undoCountText = null;
            statusText = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            Build();
        }

        private void EnsureBuilt()
        {
            if (transform.Find("HintButton") != null &&
                transform.Find("ShuffleButton") != null &&
                transform.Find("UndoButton") != null)
            {
                hintCountText = transform.Find("HintButton/HintButton_Count")?.GetComponent<TMP_Text>();
                shuffleCountText = transform.Find("ShuffleButton/ShuffleButton_Count")?.GetComponent<TMP_Text>();
                undoCountText = transform.Find("UndoButton/UndoButton_Count")?.GetComponent<TMP_Text>();
                statusText = transform.Find("AssistStatus")?.GetComponent<TMP_Text>();
                hintButton = transform.Find("HintButton")?.GetComponent<Button>();
                shuffleButton = transform.Find("ShuffleButton")?.GetComponent<Button>();
                undoButton = transform.Find("UndoButton")?.GetComponent<Button>();
                ApplyRootLayout(transform as RectTransform);
                ApplyButtonLayout(hintButton, new Vector2(-ButtonStep, 0f));
                ApplyButtonLayout(shuffleButton, Vector2.zero);
                ApplyButtonLayout(undoButton, new Vector2(ButtonStep, 0f));
                ApplyTextLayout(statusText, new Vector2(0f, -86f), new Vector2(430f, 26f));
                RefreshCounts();
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            Build();
        }

        private Button CreateButton(string name, Sprite sprite, string icon, string label, Vector2 position, UnityEngine.Events.UnityAction onClick, out TMP_Text countText)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = ButtonSize;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : ButtonColor;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = ButtonPressedColor;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            countText = CreateCountBadge(go.transform, $"{name}_Count");

            if (sprite == null)
            {
                TMP_Text iconText = CreateText($"{name}_Icon", icon, new Vector2(-46f, 2f), new Vector2(54f, 54f), 30f);
                iconText.transform.SetParent(go.transform, false);
                iconText.fontStyle = FontStyles.Bold;

                TMP_Text labelText = CreateText($"{name}_Label", label, new Vector2(28f, 0f), new Vector2(92f, 42f), 18f);
                labelText.transform.SetParent(go.transform, false);
                labelText.fontStyle = FontStyles.Bold;
            }

            return button;
        }

        private static void ApplyRootLayout(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = RootPosition;
            rect.sizeDelta = RootSize;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ApplyButtonLayout(Button button, Vector2 position)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = ButtonSize;
            rect.localScale = Vector3.one;
        }

        private static void ApplyTextLayout(TMP_Text text, Vector2 position, Vector2 size)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private TMP_Text CreateText(string name, string text, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TMP_Text tmp = go.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }

        private TMP_Text CreateCountBadge(Transform parent, string name)
        {
            GameObject badge = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(parent, false);

            RectTransform rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-6f, 8f);
            rect.sizeDelta = new Vector2(44f, 32f);

            Image bg = badge.GetComponent<Image>();
            bg.color = new Color(0.02f, 0.10f, 0.07f, 0.88f);
            bg.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(badge.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.color = TextColor;
            tmp.fontSize = 22f;
            tmp.fontSizeMin = 12f;
            tmp.fontSizeMax = 24f;
            tmp.enableAutoSizing = true;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void OnHintClicked()
        {
            TryUseBooster(MahjongAssistBooster.HintPair, () => board != null && board.TryShowHintPair(), "НЕТ ПАР");
        }

        private void OnShuffleClicked()
        {
            TryUseBooster(MahjongAssistBooster.Shuffle, () => board != null && board.TryShuffleActiveTiles(), "НЕЛЬЗЯ");
        }

        private void OnUndoClicked()
        {
            TryUseBooster(MahjongAssistBooster.Undo, () => board != null && board.TryUndoLastMove(), "НЕТ ХОДА");
        }

        private void TryUseBooster(MahjongAssistBooster booster, System.Func<bool> action, string failStatus)
        {
            if (rewardedAdRequestInProgress)
                return;

            if (MahjongAssistInventoryService.GetCount(booster) > 0)
            {
                TryExecuteAndConsume(booster, action, failStatus);
                return;
            }

            RewardedAdAvailability availability = MonetizationService.Ensure().GetRewardedAdAvailability(MonetizationService.MahjongAssistRewardedPlacementId);
            if (!availability.IsReady)
            {
                SetStatus(ResolveAdStatus(availability));
                return;
            }

            rewardedAdRequestInProgress = true;
            SetButtonsInteractable(false);
            SetStatus("РЕКЛАМА");

            MonetizationService.Ensure().ShowRewardedAd(MonetizationService.MahjongAssistRewardedPlacementId, result =>
            {
                rewardedAdRequestInProgress = false;
                SetButtonsInteractable(true);

                if (!result.IsCompleted)
                {
                    SetStatus("НЕТ РЕКЛАМЫ");
                    RefreshCounts();
                    return;
                }

                MahjongAssistInventoryService.Grant(booster, MahjongAssistInventoryService.RewardedGrantAmount);
                RefreshCounts();
                TryExecuteAndConsume(booster, action, failStatus);
            });
        }

        private void TryExecuteAndConsume(MahjongAssistBooster booster, System.Func<bool> action, string failStatus)
        {
            if (action == null || !action())
            {
                SetStatus(failStatus);
                RefreshCounts();
                return;
            }

            MahjongAssistInventoryService.TryConsume(booster);
            RefreshCounts();
            SetStatus("");
        }

        private void RefreshCounts()
        {
            SetCountText(hintCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.HintPair));
            SetCountText(shuffleCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.Shuffle));
            SetCountText(undoCountText, MahjongAssistInventoryService.GetCount(MahjongAssistBooster.Undo));
        }

        private void SetCountText(TMP_Text text, int count)
        {
            if (text != null)
                text.text = count > 0 ? count.ToString() : "AD";
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (hintButton != null)
                hintButton.interactable = interactable;

            if (shuffleButton != null)
                shuffleButton.interactable = interactable;

            if (undoButton != null)
                undoButton.interactable = interactable;
        }

        private string ResolveAdStatus(RewardedAdAvailability availability)
        {
            if (availability.IsLoading)
                return "ЗАГРУЗКА";

            return "НЕТ РЕКЛАМЫ";
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }
    }
}
